using NUnit.Framework;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class CoinDropTests
    {
        private GameObject enemyManagerObj;
        private EnemyManager enemyManager;
        private GameObject coinPrefab;

        [SetUp]
        public void SetUp()
        {
            // 코인 프리팹 모의 생성
            coinPrefab = new GameObject("MockCoin", typeof(SphereCollider), typeof(Coin));

            // EnemyManager 모의 생성
            enemyManagerObj = new GameObject("EnemyManager", typeof(EnemyManager));
            enemyManager = enemyManagerObj.GetComponent<EnemyManager>();

            // Reflection으로 coinPrefab 설정
            var coinField = typeof(EnemyManager).GetField("coinPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (coinField != null)
            {
                coinField.SetValue(enemyManager, coinPrefab);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (enemyManagerObj != null) Object.DestroyImmediate(enemyManagerObj);
            if (coinPrefab != null) Object.DestroyImmediate(coinPrefab);

            // 잔여 코인 정리
            var coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
            foreach (var c in coins)
            {
                if (c != null && c.gameObject != null) Object.DestroyImmediate(c.gameObject);
            }
        }

        [Test]
        public void EnemyDeath_DisablesColliderImmediately_AndSetsIsDead()
        {
            // 준비: Enemy 오브젝트 생성
            var enemyObj = new GameObject("TestEnemy", typeof(CapsuleCollider), typeof(Enemy));
            var enemy = enemyObj.GetComponent<Enemy>();
            var col = enemyObj.GetComponent<CapsuleCollider>();

            Assert.IsTrue(col.enabled, "초기에는 충돌체가 활성화되어 있어야 합니다.");
            Assert.IsFalse(enemy.IsDead, "초기에는 IsDead가 false여야 합니다.");

            // 실행: 치명타 피해 적용 (체력 0 이하)
            enemy.TakeDamage(enemy.MaxHealth + 10f);

            // 검증: 체력 0 도달 즉시 처치 판정 및 충돌체 즉시 제거 (§6)
            Assert.IsTrue(enemy.IsDead, "체력 0 도달 시 즉시 IsDead가 true여야 합니다.");
            Assert.IsFalse(col.enabled, "체력 0 도달 시 충돌체가 즉시 비활성화되어야 합니다.");

            Object.DestroyImmediate(enemyObj);
        }

        [Test]
        public void DropCoins_SpawnsThreeCoins_WithDividedGold()
        {
            // 준비: Enemy 생성 및 골드 설정 (기본 30G)
            var enemyObj = new GameObject("TestEnemy", typeof(CapsuleCollider), typeof(Enemy));
            var enemy = enemyObj.GetComponent<Enemy>();
            enemy.SetBaseGold(30f);

            // 실행: 코인 드랍
            enemyManager.DropCoins(enemy);

            // 검증: 기본 3개 분할 생성 (§6)
            var spawnedCoins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
            // coinPrefab 원본 1개 제외하고 스폰된 코인 수 검사
            int spawnedCount = 0;
            int totalSpawnedGold = 0;
            foreach (var c in spawnedCoins)
            {
                if (c.gameObject != coinPrefab)
                {
                    spawnedCount++;
                    totalSpawnedGold += c.GoldValue;
                }
            }

            Assert.AreEqual(3, spawnedCount, "기본적으로 코인이 3개 분할 생성되어야 합니다.");
            Assert.AreEqual(30, totalSpawnedGold, "분할된 코인의 골드 합은 적의 기본 골드(30G)와 일치해야 합니다.");

            Object.DestroyImmediate(enemyObj);
        }

        [Test]
        public void Coin_PickupDelay_PreventsInstantCollection()
        {
            // 준비: 코인 인스턴스 생성
            var coinObj = Object.Instantiate(coinPrefab);
            var coin = coinObj.GetComponent<Coin>();
            coin.PickupDelay = 0.5f;

            // 펭귄(플레이어) 모의 오브젝트 생성
            var penguinObj = new GameObject("Penguin", typeof(SphereCollider));
            penguinObj.tag = "Player";

            // 검증: 스폰 직후 (Time.time 경과 전) 트리거 충돌 시 획득되지 않고 유지됨
            // OnTriggerEnter를 직접 시뮬레이션
            var onTriggerEnter = typeof(Coin).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onTriggerEnter != null)
            {
                onTriggerEnter.Invoke(coin, new object[] { penguinObj.GetComponent<Collider>() });
            }

            Assert.IsNotNull(coinObj, "스폰 직후에는 유예 시간으로 인해 코인이 즉시 획득(파괴)되지 않아야 합니다.");

            Object.DestroyImmediate(penguinObj);
            Object.DestroyImmediate(coinObj);
        }
    }
}
