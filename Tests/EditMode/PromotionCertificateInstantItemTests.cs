using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class PromotionCertificateInstantItemTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private PenguinDeckManager deckManager;
        private GameManager gameManager;
        private EconomyManager economyManager;
        private ShopManager shopManager;
        private InstantItem_PromotionCertificate instantItem;

        private void SetInstance(System.Type type, object value)
        {
            var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            prop?.GetSetMethod(true)?.Invoke(null, new object[] { value });
        }

        [SetUp]
        public void SetUp()
        {
            createdGameObjects = new List<GameObject>();
            createdScriptableObjects = new List<ScriptableObject>();

            // GameManager
            var gmGo = new GameObject("GameManager", typeof(GameManager));
            gameManager = gmGo.GetComponent<GameManager>();
            SetInstance(typeof(GameManager), gameManager);
            createdGameObjects.Add(gmGo);

            // EconomyManager
            var ecoGo = new GameObject("EconomyManager", typeof(EconomyManager));
            economyManager = ecoGo.GetComponent<EconomyManager>();
            SetInstance(typeof(EconomyManager), economyManager);
            economyManager.AddGold(100);
            createdGameObjects.Add(ecoGo);

            // PenguinDeckManager
            var deckGo = new GameObject("PenguinDeckManager", typeof(PenguinDeckManager));
            deckManager = deckGo.GetComponent<PenguinDeckManager>();
            SetInstance(typeof(PenguinDeckManager), deckManager);
            createdGameObjects.Add(deckGo);

            // ShopManager
            var shopGo = new GameObject("ShopManager", typeof(ShopManager));
            shopManager = shopGo.GetComponent<ShopManager>();
            SetInstance(typeof(ShopManager), shopManager);
            createdGameObjects.Add(shopGo);

            // InstantItem_PromotionCertificate
            instantItem = ScriptableObject.CreateInstance<InstantItem_PromotionCertificate>();
            instantItem.ItemID = "I-02";
            instantItem.ItemName = "승급 증서 T2";
            instantItem.Price = 15;
            createdScriptableObjects.Add(instantItem);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdScriptableObjects.Count; i++)
            {
                if (createdScriptableObjects[i] != null)
                {
                    Object.DestroyImmediate(createdScriptableObjects[i]);
                }
            }
            createdScriptableObjects.Clear();

            for (int i = 0; i < createdGameObjects.Count; i++)
            {
                if (createdGameObjects[i] != null)
                {
                    Object.DestroyImmediate(createdGameObjects[i]);
                }
            }
            createdGameObjects.Clear();

            SetInstance(typeof(ShopManager), null);
            SetInstance(typeof(PenguinDeckManager), null);
            SetInstance(typeof(EconomyManager), null);
            SetInstance(typeof(GameManager), null);
        }

        private PenguinDefinition CreateDefinition(string name)
        {
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = name;
            def.DisplayName = name;
            def.attack = 10f;
            def.attackGrowthPerTier = 0.5f;
            def.maxSpeed = 15f;
            def.maxSpeedGrowthPerTier = 0.2f;
            def.mass = 1f;
            def.massGrowthPerTier = 0.1f;
            def.respawnSeconds = 3f;
            def.respawnTimeGrowthPerTier = 0.1f;
            createdScriptableObjects.Add(def);
            return def;
        }

        private PenguinController CreateMockPenguin(PenguinDefinition def, int tier = 1)
        {
            var go = new GameObject($"Penguin_{def.name}_T{tier}", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinLocationData), typeof(PenguinController));
            createdGameObjects.Add(go);

            var controller = go.GetComponent<PenguinController>();
            controller.Init(def);
            controller.SetTier(tier);

            var loc = go.GetComponent<PenguinLocationData>();
            loc.Location = PenguinLocation.Bench;

            deckManager.OwnedPenguins.Add(go);
            return controller;
        }

        [Test]
        public void PromotionCertificate_HasCorrectInitialValues()
        {
            Assert.AreEqual("I-02", instantItem.ItemID);
            Assert.AreEqual("승급 증서 T2", instantItem.ItemName);
            Assert.AreEqual(ShopItemType.Instant, instantItem.Type);
            Assert.AreEqual(InstantTargetType.SinglePenguin, instantItem.TargetType);
            Assert.AreEqual(15, instantItem.Price);
            StringAssert.Contains("5성 초과는 불가", instantItem.Description);
            StringAssert.Contains("1성 영구 업그레이드", instantItem.Description);
        }

        [Test]
        public void CanUse_ReturnsFalse_WhenNoAvailablePenguins()
        {
            bool canUse = instantItem.CanUse(out string failReason);
            Assert.IsFalse(canUse);
            StringAssert.Contains("보유한 펭귄이 없어", failReason);
        }

        [Test]
        public void CanUse_ReturnsFalse_WhenAllPenguinsAreTier5()
        {
            var def = CreateDefinition("Adelie");
            var p1 = CreateMockPenguin(def, 5);
            var p2 = CreateMockPenguin(def, 5);

            bool canUse = instantItem.CanUse(out string failReason);
            Assert.IsFalse(canUse);
            StringAssert.Contains("최대 성(5성)", failReason);
        }

        [Test]
        public void CanUse_ReturnsTrue_WhenPenguinUnderTier5Exists()
        {
            var def = CreateDefinition("Adelie");
            CreateMockPenguin(def, 1);

            bool canUse = instantItem.CanUse(out string failReason);
            Assert.IsTrue(canUse);
            Assert.IsEmpty(failReason);
        }

        [Test]
        public void ExecuteTargetedEffect_UpgradesPenguinTierBy1()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 1);

            float initialAttack = penguin.Stats[PenguinPinball.Core.StatType.Attack].Value;
            bool success = instantItem.ExecuteTargetedEffect(penguin);

            Assert.IsTrue(success);
            Assert.AreEqual(2, penguin.Tier, "Tier should be upgraded from 1 to 2");
            Assert.Greater(penguin.Stats[PenguinPinball.Core.StatType.Attack].Value, initialAttack, "Attack should increase after tier upgrade");
        }

        [Test]
        public void ExecuteTargetedEffect_Fails_WhenTargetIsTier5()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 5);

            bool success = instantItem.ExecuteTargetedEffect(penguin);

            Assert.IsFalse(success, "Should fail when penguin is already Tier 5");
            Assert.AreEqual(5, penguin.Tier, "Tier should remain 5");
        }

        [Test]
        public void ExecuteTargetedEffect_Fails_WhenTargetIsInField()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 2);
            typeof(PenguinController).GetProperty("IsInField")?.GetSetMethod(true)?.Invoke(penguin, new object[] { true });

            bool success = instantItem.ExecuteTargetedEffect(penguin);

            Assert.IsFalse(success, "Should fail when penguin is currently in field");
            Assert.AreEqual(2, penguin.Tier, "Tier should remain 2");
        }

        [Test]
        public void ExecuteTargetedEffect_TriggersSynthesis_WhenMatchingTierExists()
        {
            var def = CreateDefinition("Adelie");
            var p1 = CreateMockPenguin(def, 1);
            var p2 = CreateMockPenguin(def, 2);

            // p1(T1)을 승급하면 T2가 되어 p2(T2)와 자동 합성 -> T3 펭귄 1마리가 되어야 함!
            bool success = instantItem.ExecuteTargetedEffect(p1);

            Assert.IsTrue(success);
            Assert.AreEqual(1, deckManager.OwnedPenguins.Count, "Two T2 penguins should merge into one T3 penguin");

            var mergedPenguin = deckManager.OwnedPenguins[0].GetComponent<PenguinController>();
            Assert.AreEqual(3, mergedPenguin.Tier, "Merged penguin should be Tier 3");
            Assert.AreEqual(def, mergedPenguin.Definition);
        }

        [Test]
        public void PredictMergesAfterPromotion_AccuratelyPredictsSynthesis()
        {
            var def = CreateDefinition("Adelie");
            var p1 = CreateMockPenguin(def, 1);
            var p2 = CreateMockPenguin(def, 2);

            // p1(T1) 승급 시 T2가 되어 p2(T2)와 합성될 것이 예측되어야 함
            bool willMerge = deckManager.PredictMergesAfterPromotion(p1, out var predictions);
            Assert.IsTrue(willMerge);
            Assert.AreEqual(1, predictions.Count);
            StringAssert.Contains("Adelie", predictions[0]);
            StringAssert.Contains("합성", predictions[0]);

            // 반면 p2(T2) 승급 시 T3이 되는데, T3 펭귄이 없으므로 합성이 예측되지 않아야 함
            bool noMerge = deckManager.PredictMergesAfterPromotion(p2, out var noPredictions);
            Assert.IsFalse(noMerge);
            Assert.AreEqual(0, noPredictions.Count);
        }

        [Test]
        public void PredictMergesAfterPromotion_ReturnsFalse_WhenTier5()
        {
            var def = CreateDefinition("Adelie");
            var p5 = CreateMockPenguin(def, 5);

            bool willMerge = deckManager.PredictMergesAfterPromotion(p5, out var predictions);
            Assert.IsFalse(willMerge);
            Assert.AreEqual(0, predictions.Count);
        }

        [Test]
        public void ShopManager_CompleteTargetedInstantPurchase_DeductsGoldAndConsumesSlot()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 1);

            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            int initialGold = economyManager.CurrentGold; // 100
            bool success = shopManager.CompleteTargetedInstantPurchase(0, instantItem, penguin);

            Assert.IsTrue(success);
            Assert.AreEqual(initialGold - 15, economyManager.CurrentGold, "Should deduct 15 gold");
            Assert.IsNull(shopManager.CurrentShopItems[0], "Shop slot should be cleared (sold out)");
            Assert.AreEqual(2, penguin.Tier, "Penguin should be promoted to Tier 2");
        }

        [Test]
        public void ShopManager_CompleteTargetedInstantPurchase_FailsAndRefunds_WhenUpgradeFails()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 5); // 5성은 승급 불가

            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            int initialGold = economyManager.CurrentGold;
            bool success = shopManager.CompleteTargetedInstantPurchase(0, instantItem, penguin);

            Assert.IsFalse(success);
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold should be refunded/unchanged on failure");
            Assert.IsNotNull(shopManager.CurrentShopItems[0], "Slot should not be consumed on failure");
            Assert.AreEqual(5, penguin.Tier, "Penguin tier should remain 5");
        }

        [Test]
        public void ShopPurchaseFlow_CannotBuy_WhenNoPenguinsOwned()
        {
            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            gameManager.ChangeState(GameState.MaintenanceTime);
            int initialGold = economyManager.CurrentGold;

            bool eventFired = false;
            shopManager.OnInstantTargetSelectionRequested += (item, slot) => eventFired = true;

            // 펭귄 0마리일 때 구매 시도 -> 구매 불가
            shopManager.BuyShopItem(0);

            Assert.IsFalse(eventFired, "OnInstantTargetSelectionRequested must NOT be triggered when no penguins are owned");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold must remain untouched");
            Assert.AreEqual(instantItem, shopManager.CurrentShopItems[0], "Shop slot must remain available");
        }

        [Test]
        public void ShopPurchaseFlow_CanBuy_WhenPenguinExists()
        {
            var def = CreateDefinition("Adelie");
            var penguin = CreateMockPenguin(def, 1);

            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            gameManager.ChangeState(GameState.MaintenanceTime);
            int initialGold = economyManager.CurrentGold;

            bool eventFired = false;
            shopManager.OnInstantTargetSelectionRequested += (item, slot) => eventFired = true;

            // 펭귄 1마리(1성) 존재 시 구매 시도 -> 대상 선택 모드 정상 진입
            shopManager.BuyShopItem(0);

            Assert.IsTrue(eventFired, "OnInstantTargetSelectionRequested must be triggered when upgradable penguin exists");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold must not be deducted until confirmed");
        }
    }
}
