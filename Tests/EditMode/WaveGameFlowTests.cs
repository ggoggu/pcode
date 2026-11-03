using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class WaveGameFlowTests
    {
        private GameObject gameManagerObj;
        private GameObject mapManagerObj;
        private GameObject deckManagerObj;
        private GameObject shopManagerObj;

        [SetUp]
        public void SetUp()
        {
            gameManagerObj = new GameObject("GameManager", typeof(GameManager));
            mapManagerObj = new GameObject("MapManager", typeof(MapManager));
            deckManagerObj = new GameObject("PenguinDeckManager", typeof(PenguinDeckManager));
        }

        [TearDown]
        public void TearDown()
        {
            if (shopManagerObj != null) Object.DestroyImmediate(shopManagerObj);
            if (deckManagerObj != null) Object.DestroyImmediate(deckManagerObj);
            if (mapManagerObj != null) Object.DestroyImmediate(mapManagerObj);
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);

            // 남아있는 임시 펭귄 및 코인 정리
            var controllers = Object.FindObjectsByType<PenguinController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (c != null && c.gameObject != null) Object.DestroyImmediate(c.gameObject);
            }

            var coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
            foreach (var coin in coins)
            {
                if (coin != null && coin.gameObject != null) Object.DestroyImmediate(coin.gameObject);
            }
        }

        [Test]
        public void SellPenguin_WhenOnlyOnePenguinRemains_CannotBeSold()
        {
            var deck = PenguinDeckManager.Instance;
            Assert.IsNotNull(deck);

            // 펭귄 1마리 생성 및 등록
            var p1 = new GameObject("Penguin_1", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            deck.RegisterPenguin(p1);
            Assert.AreEqual(1, deck.OwnedPenguins.Count, "1마리가 등록되어야 합니다.");

            // 마지막 1마리 판매 시도
            deck.SellPenguin(p1);

            // 검증: 판매되지 않고 여전히 1마리 유지
            Assert.AreEqual(1, deck.OwnedPenguins.Count, "마지막 1마리는 판매되지 않고 유지되어야 합니다.");
            Assert.IsNotNull(p1, "오브젝트가 파괴되지 않아야 합니다.");
        }

        [Test]
        public void SellPenguin_WhenMoreThanOnePenguinRemains_CanBeSold()
        {
            var deck = PenguinDeckManager.Instance;

            var p1 = new GameObject("Penguin_1", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            var p2 = new GameObject("Penguin_2", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            deck.RegisterPenguin(p1);
            deck.RegisterPenguin(p2);
            Assert.AreEqual(2, deck.OwnedPenguins.Count);

            // 2마리 중 1마리 판매
            deck.SellPenguin(p1);

            // 검증: 1마리만 남아있어야 함
            Assert.AreEqual(1, deck.OwnedPenguins.Count, "2마리 중 1마리는 정상적으로 판매되어야 합니다.");
        }

        [Test]
        public void DrainedPenguin_ResetToHeld_RestoresToAvailableState()
        {
            var pObj = new GameObject("Penguin_Drained", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            var controller = pObj.GetComponent<PenguinController>();

            // 가상의 펭귄 정의 주입 또는 초기화
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.DisplayName = "TestPenguin";
            controller.Init(def);

            // 드레인된 상태 흉내: IsInField = false, IsAvailable = false
            controller.Drain(1f);
            Assert.IsFalse(controller.IsAvailable, "드레인 후에는 IsAvailable이 false여야 합니다.");

            // 웨이브 종료 시 회수 호출
            controller.ResetToHeld();

            // 검증: IsAvailable이 true로 복구되고 필드에서 벗어나야 함
            Assert.IsTrue(controller.IsAvailable, "ResetToHeld 호출 후에는 전원 즉시 발사 가능(IsAvailable = true) 상태로 복귀해야 합니다.");
            Assert.IsFalse(controller.IsInField, "ResetToHeld 호출 후에는 IsInField가 false여야 합니다.");
        }

        [Test]
        public void ClearAllFieldCoins_DestroysAllCoinsInField()
        {
            var c1 = new GameObject("Coin_1", typeof(SphereCollider), typeof(Coin));
            var c2 = new GameObject("Coin_2", typeof(SphereCollider), typeof(Coin));

            Coin.ClearAllFieldCoins();

            // 오브젝트가 파괴 대기 상태(Unity fake null)인지 확인
            Assert.IsTrue(c1 == null, "Coin_1이 파괴되어야 합니다.");
            Assert.IsTrue(c2 == null, "Coin_2가 파괴되어야 합니다.");
        }

        [Test]
        public void StartNextWave_WhenZeroPenguins_BlockedByGate()
        {
            var map = MapManager.Instance;
            var deck = PenguinDeckManager.Instance;
            deck.OwnedPenguins.Clear();

            // 펭귄 0마리 상태에서 웨이브 시작 시도
            bool started = map.StartNextWave();

            // 검증: 시작 실패 (차단)
            Assert.IsFalse(started, "보유 펭귄이 0마리일 때는 웨이브가 시작되지 않아야 합니다.");
            Assert.AreEqual(GameState.MaintenanceTime, GameManager.Instance.CurrentState, "상태가 여전히 정비시간이어야 합니다.");
        }

        [Test]
        public void ShopManager_OnFirstMaintenance_GeneratesCurrentShopItems()
        {
            shopManagerObj = new GameObject("ShopManager", typeof(ShopManager));
            var shop = shopManagerObj.GetComponent<ShopManager>();

            // ItemPool과 HatcheryPool에 더미 데이터 주입
            var dummyItem = ScriptableObject.CreateInstance<ShopItemBase>();
            dummyItem.ItemID = "TEST_ITEM_1";
            dummyItem.ItemName = "테스트 아이템";
            dummyItem.Type = ShopItemType.Charm;
            dummyItem.Price = 10;
            shop.ItemPool.Add(dummyItem);

            var dummyPenguin = ScriptableObject.CreateInstance<PenguinDefinition>();
            dummyPenguin.DisplayName = "테스트 펭귄";
            shop.HatcheryPool.Add(dummyPenguin);

            // 최초 정비시간 상태 확인 (Map 1 Wave 1 Maintenance)
            Assert.IsTrue(shop.IsFirstMaintenance, "최초 정비시간 상태여야 합니다.");

            // 상점 및 부화장 리롤 실행 (게임 시작/정비시간 진입 동작)
            shop.RerollShop(freeReroll: true);
            shop.RerollHatchery(freeReroll: true);

            // 검증: CurrentShopItems가 3개 채워져 있어야 함
            Assert.AreEqual(3, shop.CurrentShopItems.Count, "게임 시작 시 상점 아이템(CurrentShopItems)이 3개 생성되어야 합니다.");
            Assert.IsNotNull(shop.CurrentShopItems[0], "생성된 상점 아이템 슬롯이 null이 아니어야 합니다.");

            // 검증: CurrentHatcheryItems도 3개 채워져 있어야 함
            Assert.AreEqual(3, shop.CurrentHatcheryItems.Count, "부화장 아이템도 3개 생성되어야 합니다.");
        }
    }
}
