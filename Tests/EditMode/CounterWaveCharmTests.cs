using System.Collections.Generic;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class CounterWaveCharmTests
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
        private Effect_CounterWave effect;

        [SetUp]
        public void SetUp()
        {
            createdGameObjects = new List<GameObject>();
            effect = ScriptableObject.CreateInstance<Effect_CounterWave>();
            effect.WaveDamage = 40f;
            effect.WaveRadius = 6f;
            effect.Cooldown = 5f;
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
        }

        private TestEnemy CreateEnemy(string name, Vector3 position, float hp = 100f)
        {
            var go = new GameObject(name, typeof(SphereCollider), typeof(TestEnemy));
            go.transform.position = position;
            var enemy = go.GetComponent<TestEnemy>();
            enemy.SetHp(hp);
            createdGameObjects.Add(go);
            return enemy;
        }

        [Test]
        public void DefaultValues_MatchT2Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_CounterWave>();
            try
            {
                Assert.AreEqual(40f, freshEffect.WaveDamage, "Default damage should be 40f for T2");
                Assert.AreEqual(6.0f, freshEffect.WaveRadius, "Default radius should be 6.0f");
                Assert.AreEqual(5.0f, freshEffect.Cooldown, "Default cooldown should be 5.0f");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void ApplyEffect_SubscribesToBaseAttacked_AndDamagesNearbyEnemy()
        {
            effect.ApplyEffect();

            var nearbyEnemy = CreateEnemy("NearbyEnemy", new Vector3(3f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            GameEvents.RaiseBaseAttacked(Vector3.zero);

            Assert.AreEqual(60f, nearbyEnemy.CurrentHealth, "Enemy within 3m should take 40 damage (100 -> 60)");
        }

        [Test]
        public void RemoveEffect_UnsubscribesFromBaseAttacked()
        {
            effect.ApplyEffect();
            effect.RemoveEffect();

            var nearbyEnemy = CreateEnemy("NearbyEnemy", new Vector3(3f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            GameEvents.RaiseBaseAttacked(Vector3.zero);

            Assert.AreEqual(100f, nearbyEnemy.CurrentHealth, "Enemy should take no damage when effect is removed");
        }

        [Test]
        public void EnemiesOutsideRadius_TakeNoDamage()
        {
            effect.ApplyEffect();

            var insideEnemy = CreateEnemy("InsideEnemy", new Vector3(4f, 0f, 0f), 100f);
            var outsideEnemy = CreateEnemy("OutsideEnemy", new Vector3(10f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            GameEvents.RaiseBaseAttacked(Vector3.zero);

            Assert.AreEqual(60f, insideEnemy.CurrentHealth, "Inside enemy (4m <= 6m) should take 40 damage");
            Assert.AreEqual(100f, outsideEnemy.CurrentHealth, "Outside enemy (10m > 6m) should take 0 damage");
        }

        [Test]
        public void Cooldown_PreventsImmediateSubsequentTrigger()
        {
            effect.ApplyEffect();

            var enemy = CreateEnemy("TestEnemy", new Vector3(2f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            // First attack -> triggers wave
            GameEvents.RaiseBaseAttacked(Vector3.zero);
            Assert.AreEqual(60f, enemy.CurrentHealth, "First attack should deal 40 damage");
            Assert.IsTrue(effect.IsOnCooldown, "Effect should be on cooldown after trigger");

            // Second attack immediately -> ignored due to cooldown
            GameEvents.RaiseBaseAttacked(Vector3.zero);
            Assert.AreEqual(60f, enemy.CurrentHealth, "Second attack during cooldown should deal no damage");
        }

        [Test]
        public void Cooldown_AllowsTriggerAfterCooldownTimeElapsed()
        {
            effect.ApplyEffect();

            var enemy = CreateEnemy("TestEnemy", new Vector3(2f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            // First attack
            GameEvents.RaiseBaseAttacked(Vector3.zero);
            Assert.AreEqual(60f, enemy.CurrentHealth);

            // Simulate cooldown elapsed
            effect.LastTriggerTime = Time.time - (effect.Cooldown + 0.1f);
            Assert.IsFalse(effect.IsOnCooldown, "Effect should no longer be on cooldown");

            // Subsequent attack after cooldown expires
            GameEvents.RaiseBaseAttacked(Vector3.zero);
            Assert.AreEqual(20f, enemy.CurrentHealth, "Subsequent attack after cooldown should deal another 40 damage (60 -> 20)");
        }

        [Test]
        public void DeadEnemy_IsNotDamagedAgain()
        {
            effect.ApplyEffect();

            var enemy = CreateEnemy("WeakEnemy", new Vector3(2f, 0f, 0f), 30f);
            Physics.SyncTransforms();

            // First wave kills the enemy (40 damage >= 30 hp)
            GameEvents.RaiseBaseAttacked(Vector3.zero);
            Assert.IsTrue(enemy.IsDead, "Enemy should be dead");

            // Reset cooldown and attack again
            effect.LastTriggerTime = Time.time - 10f;
            GameEvents.RaiseBaseAttacked(Vector3.zero);

            // Still dead, no error or extra health reduction
            Assert.AreEqual(0f, enemy.CurrentHealth);
        }

        [Test]
        public void MultiColliderEnemy_DamagedOnlyOncePerWave()
        {
            effect.ApplyEffect();

            var enemy = CreateEnemy("MultiColliderEnemy", new Vector3(2f, 0f, 0f), 100f);
            // Add a secondary collider on a child object
            var childGo = new GameObject("ChildCollider", typeof(BoxCollider));
            childGo.transform.SetParent(enemy.transform, false);
            createdGameObjects.Add(childGo);
            Physics.SyncTransforms();

            GameEvents.RaiseBaseAttacked(Vector3.zero);

            Assert.AreEqual(60f, enemy.CurrentHealth, "Enemy with multiple colliders must only be damaged once per wave");
        }

        [Test]
        public void BaseHealth_TakeDamage_TriggersCounterWaveAroundBase()
        {
            effect.ApplyEffect();

            // Create BaseHealth object at (10, 0, 10)
            var baseGo = new GameObject("BaseObject", typeof(BaseHealth));
            baseGo.transform.position = new Vector3(10f, 0f, 10f);
            createdGameObjects.Add(baseGo);
            var baseHealth = baseGo.GetComponent<BaseHealth>();

            // Enemy near BaseHealth (dist = 2m from base)
            var nearEnemy = CreateEnemy("NearBaseEnemy", new Vector3(12f, 0f, 10f), 100f);
            // Enemy far from BaseHealth
            var farEnemy = CreateEnemy("FarEnemy", new Vector3(0f, 0f, 0f), 100f);
            Physics.SyncTransforms();

            // Attack the base
            baseHealth.TakeDamage(1);

            Assert.AreEqual(60f, nearEnemy.CurrentHealth, "Enemy near BaseHealth should be damaged by counter wave");
            Assert.AreEqual(100f, farEnemy.CurrentHealth, "Enemy far from BaseHealth should take no damage");
        }

        [Test]
        public void ItemInventoryManager_EquipCharm_ActivatesCounterWave()
        {
            var invGo = new GameObject("InventoryManager", typeof(ItemInventoryManager));
            createdGameObjects.Add(invGo);
            var inv = invGo.GetComponent<ItemInventoryManager>();

            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "A-03";
            charm.ItemName = "반격 파동 T2";
            charm.Type = ShopItemType.Charm;
            charm.IsConsumable = false;
            charm.Effects = new List<EffectBase> { effect };

            try
            {
                inv.AddCharm(charm);

                var enemy = CreateEnemy("TestEnemy", new Vector3(2f, 0f, 0f), 100f);
                Physics.SyncTransforms();

                GameEvents.RaiseBaseAttacked(Vector3.zero);
                Assert.AreEqual(60f, enemy.CurrentHealth, "Equipped charm effect should damage enemy");

                // Unequip
                inv.RemoveCharm(charm);

                effect.LastTriggerTime = Time.time - 10f;
                GameEvents.RaiseBaseAttacked(Vector3.zero);
                Assert.AreEqual(60f, enemy.CurrentHealth, "Unequipped charm effect should not trigger");
            }
            finally
            {
                if (charm != null) Object.DestroyImmediate(charm);
            }
        }
    }
}
