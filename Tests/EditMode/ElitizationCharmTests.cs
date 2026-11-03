using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class ElitizationCharmTests
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
        private Effect_Elitization effect;
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

            // GameManager
            var gmGo = new GameObject("GameManager", typeof(GameManager));
            gameManager = gmGo.GetComponent<GameManager>();
            SetInstance(typeof(GameManager), gameManager);
            createdGameObjects.Add(gmGo);

            // EconomyManager
            var ecoGo = new GameObject("EconomyManager", typeof(EconomyManager));
            economyManager = ecoGo.GetComponent<EconomyManager>();
            SetInstance(typeof(EconomyManager), economyManager);
            createdGameObjects.Add(ecoGo);

            // ItemInventoryManager
            var invGo = new GameObject("ItemInventoryManager", typeof(ItemInventoryManager));
            inventoryManager = invGo.GetComponent<ItemInventoryManager>();
            SetInstance(typeof(ItemInventoryManager), inventoryManager);
            createdGameObjects.Add(invGo);

            // PenguinDeckManager
            var deckGo = new GameObject("PenguinDeckManager", typeof(PenguinDeckManager));
            deckManager = deckGo.GetComponent<PenguinDeckManager>();
            SetInstance(typeof(PenguinDeckManager), deckManager);
            createdGameObjects.Add(deckGo);

            // Effect_Elitization
            effect = ScriptableObject.CreateInstance<Effect_Elitization>();
            effect.lockedBenchSlots = 2;
            effect.extraCoinDropCount = 1;
            effect.penguinSellPrice = 10;
        }

        [TearDown]
        public void TearDown()
        {
            if (effect != null)
            {
                effect.RemoveEffect();
                Object.DestroyImmediate(effect);
            }

            for (int i = 0; i < createdGameObjects.Count; i++)
            {
                if (createdGameObjects[i] != null)
                {
                    Object.DestroyImmediate(createdGameObjects[i]);
                }
            }
            createdGameObjects.Clear();

            SetInstance(typeof(PenguinDeckManager), null);
            SetInstance(typeof(GameManager), null);
            SetInstance(typeof(EconomyManager), null);
            SetInstance(typeof(ItemInventoryManager), null);
        }

        private GameObject CreateMockPenguin(string name, PenguinLocation location = PenguinLocation.Bench)
        {
            var go = new GameObject(name, typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            var locData = go.GetComponent<PenguinLocationData>();
            locData.Location = location;
            createdGameObjects.Add(go);

            deckManager.OwnedPenguins.Add(go);
            return go;
        }

        [Test]
        public void DefaultValues_MatchT4Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_Elitization>();
            try
            {
                Assert.AreEqual(2, freshEffect.lockedBenchSlots, "Default locked bench slots should be 2 for T4");
                Assert.AreEqual(1, freshEffect.extraCoinDropCount, "Default extra coin drop count should be 1");
                Assert.AreEqual(10, freshEffect.penguinSellPrice, "Default penguin sell price should be 10");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void ApplyEffect_LocksLastTwoBenchSlots()
        {
            Assert.AreEqual(6, deckManager.EffectiveBenchCapacity);
            Assert.IsFalse(deckManager.IsBenchSlotLocked(4));
            Assert.IsFalse(deckManager.IsBenchSlotLocked(5));

            effect.ApplyEffect();

            Assert.AreEqual(2, deckManager.LockedBenchSlots);
            Assert.AreEqual(4, deckManager.EffectiveBenchCapacity, "Effective bench capacity should be 4 (6 - 2)");
            Assert.IsFalse(deckManager.IsBenchSlotLocked(0));
            Assert.IsFalse(deckManager.IsBenchSlotLocked(3));
            Assert.IsTrue(deckManager.IsBenchSlotLocked(4), "Slot 4 (5th slot) should be locked");
            Assert.IsTrue(deckManager.IsBenchSlotLocked(5), "Slot 5 (6th slot) should be locked");
        }

        [Test]
        public void RemoveEffect_UnlocksBenchSlots()
        {
            effect.ApplyEffect();
            Assert.AreEqual(4, deckManager.EffectiveBenchCapacity);

            effect.RemoveEffect();

            Assert.AreEqual(0, deckManager.LockedBenchSlots);
            Assert.AreEqual(6, deckManager.EffectiveBenchCapacity, "Effective bench capacity should be restored to 6");
            Assert.IsFalse(deckManager.IsBenchSlotLocked(4));
            Assert.IsFalse(deckManager.IsBenchSlotLocked(5));
        }

        [Test]
        public void WaveStart_MovesPenguinsOnLockedSlotsToInventory_WhenInventoryHasSpace()
        {
            // Create 6 penguins on bench
            var penguins = new List<GameObject>();
            for (int i = 0; i < 6; i++)
            {
                penguins.Add(CreateMockPenguin($"Penguin_{i}", PenguinLocation.Bench));
            }

            effect.ApplyEffect();
            Assert.AreEqual(4, deckManager.EffectiveBenchCapacity);

            // Trigger wave start
            gameManager.ChangeState(GameState.WaveInProgress);

            // First 4 penguins should still be on bench
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(PenguinLocation.Bench, penguins[i].GetComponent<PenguinLocationData>().Location, $"Penguin {i} should remain on bench");
            }

            // 5th and 6th penguins (index 4 and 5) should be moved to inventory
            Assert.AreEqual(PenguinLocation.Inventory, penguins[4].GetComponent<PenguinLocationData>().Location, "Penguin 4 should have moved to inventory");
            Assert.AreEqual(PenguinLocation.Inventory, penguins[5].GetComponent<PenguinLocationData>().Location, "Penguin 5 should have moved to inventory");
        }

        [Test]
        public void WaveStart_SellsPenguinsOnLockedSlots_WhenInventoryIsFull()
        {
            // Fill inventory to max (9 penguins)
            for (int i = 0; i < 9; i++)
            {
                CreateMockPenguin($"InvPenguin_{i}", PenguinLocation.Inventory);
            }
            Assert.AreEqual(9, deckManager.GetTotalInventoryCount());

            // Create 5 penguins on bench (index 4 is on locked slot when locked slots = 2)
            var benchPenguins = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                benchPenguins.Add(CreateMockPenguin($"BenchPenguin_{i}", PenguinLocation.Bench));
            }

            int initialGold = gameManager.CurrentGold;

            effect.ApplyEffect();
            Assert.AreEqual(4, deckManager.EffectiveBenchCapacity);

            // Trigger wave start
            gameManager.ChangeState(GameState.WaveInProgress);

            // The 5th penguin (index 4) should have been sold because inventory is full
            Assert.IsFalse(deckManager.OwnedPenguins.Contains(benchPenguins[4]), "Penguin on locked slot should have been unregistered from OwnedPenguins");
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Player should receive 10 gold for automatically sold penguin");

            // First 4 penguins should still be alive on bench
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(deckManager.OwnedPenguins.Contains(benchPenguins[i]));
                Assert.AreEqual(PenguinLocation.Bench, benchPenguins[i].GetComponent<PenguinLocationData>().Location);
            }
        }

        [Test]
        public void WaveStart_LeavesPenguinsOnUnlockedSlotsUntouched()
        {
            // Only 3 penguins on bench (all within effective capacity 4)
            var penguins = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                penguins.Add(CreateMockPenguin($"Penguin_{i}", PenguinLocation.Bench));
            }

            effect.ApplyEffect();

            gameManager.ChangeState(GameState.WaveInProgress);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PenguinLocation.Bench, penguins[i].GetComponent<PenguinLocationData>().Location);
                Assert.IsTrue(deckManager.OwnedPenguins.Contains(penguins[i]));
            }
        }

        [Test]
        public void EnemyKilled_TriggersExtraCoinDrop()
        {
            effect.ApplyEffect();

            int initialGold = gameManager.CurrentGold;

            var enemyGo = new GameObject("TestEnemy", typeof(SphereCollider), typeof(TestEnemy));
            createdGameObjects.Add(enemyGo);
            var enemy = enemyGo.GetComponent<TestEnemy>();
            enemy.SetHp(100f);

            // Raise EnemyKilled event
            GameEvents.RaiseEnemyKilled(null, enemy);

            // Without prefab assigned, fallback adds gold
            Assert.AreEqual(initialGold + 10, gameManager.CurrentGold, "Extra coin drop should give gold when no prefab is assigned");
        }

        [Test]
        public void ItemInventoryManager_EquipAndUnequip_TogglesBenchLock()
        {
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "S-01";
            charm.ItemName = "정예화 T4";
            charm.Type = ShopItemType.Charm;
            charm.IsConsumable = false;
            charm.Effects = new List<EffectBase> { effect };

            try
            {
                Assert.AreEqual(6, deckManager.EffectiveBenchCapacity);

                // Equip charm
                inventoryManager.AddCharm(charm);
                Assert.AreEqual(4, deckManager.EffectiveBenchCapacity, "Equipping charm should lock 2 bench slots");

                // Unequip charm
                inventoryManager.RemoveCharm(charm);
                Assert.AreEqual(6, deckManager.EffectiveBenchCapacity, "Removing charm should unlock bench slots");
            }
            finally
            {
                if (charm != null) Object.DestroyImmediate(charm);
            }
        }
    }
}
