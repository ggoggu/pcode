using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class OverloadSettlementCharmTests
    {
        private class TestEnemy : Enemy
        {
            public void SetHp(float hp)
            {
                var maxHpProp = typeof(Enemy).GetProperty("MaxHealth");
                if (maxHpProp != null) maxHpProp.SetValue(this, hp, null);

                var curHpProp = typeof(Enemy).GetProperty("CurrentHealth");
                if (curHpProp != null) curHpProp.SetValue(this, hp, null);

                var isDeadProp = typeof(Enemy).GetProperty("IsDead");
                if (isDeadProp != null) isDeadProp.SetValue(this, false, null);

                gameObject.SetActive(true);
            }
        }

        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private Effect_OverloadSettlement effect;
        private ShopItemBase charmItem;
        private PenguinDeckManager deckManager;
        private GameManager gameManager;
        private EconomyManager economyManager;
        private ItemInventoryManager inventoryManager;

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

            // 1. GameManager
            var gmGo = new GameObject("GameManager", typeof(GameManager));
            gameManager = gmGo.GetComponent<GameManager>();
            SetInstance(typeof(GameManager), gameManager);
            createdGameObjects.Add(gmGo);

            // 2. EconomyManager
            var ecoGo = new GameObject("EconomyManager", typeof(EconomyManager));
            economyManager = ecoGo.GetComponent<EconomyManager>();
            SetInstance(typeof(EconomyManager), economyManager);
            createdGameObjects.Add(ecoGo);

            // 3. ItemInventoryManager
            var invGo = new GameObject("ItemInventoryManager", typeof(ItemInventoryManager));
            inventoryManager = invGo.GetComponent<ItemInventoryManager>();
            SetInstance(typeof(ItemInventoryManager), inventoryManager);
            createdGameObjects.Add(invGo);

            // 4. PenguinDeckManager
            var deckGo = new GameObject("PenguinDeckManager", typeof(PenguinDeckManager));
            deckManager = deckGo.GetComponent<PenguinDeckManager>();
            SetInstance(typeof(PenguinDeckManager), deckManager);
            createdGameObjects.Add(deckGo);

            // 5. Effect & Charm
            effect = ScriptableObject.CreateInstance<Effect_OverloadSettlement>();
            effect.extraCoinDropCount = 1;
            createdScriptableObjects.Add(effect);

            charmItem = ScriptableObject.CreateInstance<ShopItemBase>();
            charmItem.ItemID = "E-10";
            charmItem.ItemName = "과부하 정산 T2";
            charmItem.Type = ShopItemType.Charm;
            charmItem.Price = 15;
            charmItem.Description = "과부하 상태에서 적을 처치할 때 코인을 1개 추가 드랍합니다.";
            charmItem.IsConsumable = false;
            charmItem.MaxUses = 1;
            charmItem.CurrentUses = 1;
            charmItem.Effects = new List<EffectBase> { effect };
            createdScriptableObjects.Add(charmItem);
        }

        [TearDown]
        public void TearDown()
        {
            if (effect != null)
            {
                effect.RemoveEffect();
            }

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

            SetInstance(typeof(PenguinDeckManager), null);
            SetInstance(typeof(ItemInventoryManager), null);
            SetInstance(typeof(EconomyManager), null);
            SetInstance(typeof(GameManager), null);
        }

        private PenguinController CreateMockPenguin(string name, int overloadThreshold = 5)
        {
            var go = new GameObject(name, typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinLocationData), typeof(PenguinController));
            createdGameObjects.Add(go);

            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.overloadThreshold = overloadThreshold;
            createdScriptableObjects.Add(def);

            var controller = go.GetComponent<PenguinController>();
            controller.Init(def);

            var locData = go.GetComponent<PenguinLocationData>();
            locData.Location = PenguinLocation.Bench;

            deckManager.OwnedPenguins.Add(go);
            return controller;
        }

        private TestEnemy CreateMockEnemy(string name, float hp = 100f)
        {
            var go = new GameObject(name, typeof(SphereCollider), typeof(TestEnemy));
            createdGameObjects.Add(go);
            var enemy = go.GetComponent<TestEnemy>();
            enemy.SetHp(hp);
            return enemy;
        }

        [Test]
        public void DefaultValues_MatchT2Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_OverloadSettlement>();
            try
            {
                Assert.AreEqual(1, freshEffect.extraCoinDropCount, "Default extraCoinDropCount should be 1");
                Assert.AreEqual("E-10", charmItem.ItemID);
                Assert.AreEqual("과부하 정산 T2", charmItem.ItemName);
                Assert.AreEqual(15, charmItem.Price, "T2 charm price should be 15G");
                Assert.IsFalse(charmItem.IsConsumable, "Charm should be permanent (non-consumable)");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void ApplyAndRemoveEffect_SubscribesAndUnsubscribesProperly()
        {
            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(5);
            Assert.IsTrue(penguin.IsOverloaded);

            var enemy = CreateMockEnemy("Enemy");

            int initialGold = gameManager.CurrentGold;

            // Apply effect
            effect.ApplyEffect();
            GameEvents.RaiseEnemyKilled(penguin, enemy);
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Extra coin drop should trigger gold addition when applied");

            // Remove effect
            effect.RemoveEffect();
            GameEvents.RaiseEnemyKilled(penguin, enemy);
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "No extra drop should occur after effect removed");
        }

        [Test]
        public void EnemyKilled_WhenOverloaded_TriggersCoinDrop()
        {
            effect.ApplyEffect();

            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(5);
            Assert.IsTrue(penguin.IsOverloaded);

            var enemy = CreateMockEnemy("Enemy");
            int initialGold = gameManager.CurrentGold;

            GameEvents.RaiseEnemyKilled(penguin, enemy);

            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Overloaded kill must trigger coin drop (fallback to gold)");
        }

        [Test]
        public void EnemyKilled_WhenNotOverloaded_DoesNotTriggerCoinDrop()
        {
            effect.ApplyEffect();

            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(3); // Less than threshold
            Assert.IsFalse(penguin.IsOverloaded);

            var enemy = CreateMockEnemy("Enemy");
            int initialGold = gameManager.CurrentGold;

            GameEvents.RaiseEnemyKilled(penguin, enemy);

            Assert.AreEqual(initialGold, gameManager.CurrentGold, "Non-overloaded kill must NOT trigger extra coin drop");
        }

        [Test]
        public void EnemyKilled_NullSource_DoesNotTriggerCoinDrop()
        {
            effect.ApplyEffect();

            var enemy = CreateMockEnemy("Enemy");
            int initialGold = gameManager.CurrentGold;

            GameEvents.RaiseEnemyKilled(null, enemy);

            Assert.AreEqual(initialGold, gameManager.CurrentGold, "Null source kill must NOT trigger extra coin drop");
        }

        [Test]
        public void EnemyKilled_SpawnsCoinPrefab_WhenPrefabAssigned()
        {
            var mockCoinPrefab = new GameObject("MockCoinPrefab");
            createdGameObjects.Add(mockCoinPrefab);

            effect.coinPrefab = mockCoinPrefab;
            effect.ApplyEffect();

            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(5);

            var enemy = CreateMockEnemy("Enemy");
            enemy.transform.position = new Vector3(3f, 0f, 5f);

            int countBefore = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

            GameEvents.RaiseEnemyKilled(penguin, enemy);

            int countAfter = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(countBefore + 1, countAfter, "Mock coin prefab instance should be spawned in scene");
        }

        [Test]
        public void InventoryManager_EquipAndUnequip_ControlsEffectLifecycle()
        {
            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(5);
            Assert.IsTrue(penguin.IsOverloaded);

            var enemy = CreateMockEnemy("Enemy");
            int initialGold = gameManager.CurrentGold;

            // Equip charm via inventory
            inventoryManager.AddCharm(charmItem);
            Assert.IsTrue(inventoryManager.OwnedCharms.Contains(charmItem));

            GameEvents.RaiseEnemyKilled(penguin, enemy);
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Equipping charm activates effect");

            // Unequip charm
            inventoryManager.RemoveCharm(charmItem);
            Assert.IsFalse(inventoryManager.OwnedCharms.Contains(charmItem));

            GameEvents.RaiseEnemyKilled(penguin, enemy);
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Unequipping charm deactivates effect");
        }

        [Test]
        public void PermanentCharm_DoesNotConsumeUsesOnTrigger()
        {
            inventoryManager.AddCharm(charmItem);

            var penguin = CreateMockPenguin("Penguin", 5);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            penguin.AddCombo(5);

            var enemy = CreateMockEnemy("Enemy");

            for (int i = 0; i < 10; i++)
            {
                GameEvents.RaiseEnemyKilled(penguin, enemy);
            }

            Assert.AreEqual(1, charmItem.CurrentUses, "Permanent charm current uses must remain unchanged");
            Assert.IsTrue(inventoryManager.OwnedCharms.Contains(charmItem), "Permanent charm must remain in inventory");
        }
    }
}
