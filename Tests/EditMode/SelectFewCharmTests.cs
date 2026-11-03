using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class SelectFewCharmTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private Effect_SelectFew effect;
        private PenguinDeckManager deckManager;
        private GameManager gameManager;
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

            Effect_SelectFew.ResetStaticData();

            // GameManager
            var gmGo = new GameObject("GameManager", typeof(GameManager));
            gameManager = gmGo.GetComponent<GameManager>();
            SetInstance(typeof(GameManager), gameManager);
            createdGameObjects.Add(gmGo);

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

            // Effect_SelectFew
            effect = ScriptableObject.CreateInstance<Effect_SelectFew>();
            effect.lockedFieldSlots = 1;
            effect.overloadThresholdReduction = 2;
            createdScriptableObjects.Add(effect);
        }

        [TearDown]
        public void TearDown()
        {
            if (effect != null)
            {
                effect.RemoveEffect();
            }

            Effect_SelectFew.ResetStaticData();

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
            SetInstance(typeof(GameManager), null);
            SetInstance(typeof(ItemInventoryManager), null);
        }

        private PenguinController CreateMockPenguin(string name, int overloadThreshold = 10)
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

        [Test]
        public void DefaultValues_MatchT4Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_SelectFew>();
            try
            {
                Assert.AreEqual(1, freshEffect.lockedFieldSlots, "Default locked field slots should be 1 (limits 3 to 2)");
                Assert.AreEqual(2, freshEffect.overloadThresholdReduction, "Default overload threshold reduction should be 2");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void ApplyEffect_ReducesMaxFieldCount_From3To2()
        {
            Assert.AreEqual(3, deckManager.MaxFieldCount, "Default field capacity should be 3");
            Assert.AreEqual(0, deckManager.LockedFieldSlots);

            effect.ApplyEffect();

            Assert.AreEqual(1, deckManager.LockedFieldSlots, "Locked field slots should be 1");
            Assert.AreEqual(2, deckManager.MaxFieldCount, "Max field count should be 2 after applying effect");
        }

        [Test]
        public void RemoveEffect_RestoresMaxFieldCount_To3()
        {
            effect.ApplyEffect();
            Assert.AreEqual(2, deckManager.MaxFieldCount);

            effect.RemoveEffect();

            Assert.AreEqual(0, deckManager.LockedFieldSlots);
            Assert.AreEqual(3, deckManager.MaxFieldCount, "Max field count should be restored to 3");
        }

        [Test]
        public void CanLaunchToField_BlocksLaunch_WhenTwoPenguinsActive()
        {
            var p1 = CreateMockPenguin("Penguin1");
            var p2 = CreateMockPenguin("Penguin2");

            effect.ApplyEffect();
            Assert.AreEqual(2, deckManager.MaxFieldCount);

            // Initially 0 on field
            Assert.IsTrue(deckManager.CanLaunchToField(), "Should allow launch when field count is 0 < 2");

            // Launch 1 penguin
            p1.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            Assert.AreEqual(1, deckManager.GetFieldActiveCount());
            Assert.IsTrue(deckManager.CanLaunchToField(), "Should allow launch when field count is 1 < 2");

            // Launch 2nd penguin
            p2.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            Assert.AreEqual(2, deckManager.GetFieldActiveCount());
            Assert.IsFalse(deckManager.CanLaunchToField(), "Should block launch when field count reaches 2 (cap is 2)");

            // TrySelectPenguin should also fail
            var p3 = CreateMockPenguin("Penguin3");
            bool selected = deckManager.TrySelectPenguin(p3.gameObject);
            Assert.IsFalse(selected, "TrySelectPenguin should fail when field limit is reached");
        }

        [Test]
        public void ApplyEffect_ReducesOverloadThreshold()
        {
            var penguin = CreateMockPenguin("Penguin", overloadThreshold: 10);
            Assert.AreEqual(10, penguin.OverloadThreshold, "Base overload threshold should be 10");

            effect.ApplyEffect();

            Assert.AreEqual(8, penguin.OverloadThreshold, "Overload threshold should be 8 (10 - 2)");

            effect.RemoveEffect();

            Assert.AreEqual(10, penguin.OverloadThreshold, "Overload threshold should be restored to 10");
        }

        [Test]
        public void OverloadTriggersAtReducedComboThreshold()
        {
            var penguin = CreateMockPenguin("Penguin", overloadThreshold: 10);
            penguin.PrepareAndLaunch(Vector3.zero, Vector3.forward, 5f);
            Assert.IsTrue(penguin.IsInField);

            effect.ApplyEffect();
            Assert.AreEqual(8, penguin.OverloadThreshold);

            // Add 7 combos: should not overload yet
            penguin.AddCombo(7);
            Assert.AreEqual(7, penguin.Combo);
            Assert.IsFalse(penguin.IsOverloaded, "Should not be overloaded at 7 combos (threshold is 8)");

            // Add 1 more combo: reaches 8, triggers overload!
            penguin.AddCombo(1);
            Assert.AreEqual(8, penguin.Combo);
            Assert.IsTrue(penguin.IsOverloaded, "Should trigger overload at 8 combos!");
        }

        [Test]
        public void OverloadThreshold_NeverDropsBelowOne()
        {
            var penguin = CreateMockPenguin("Penguin", overloadThreshold: 3);
            effect.overloadThresholdReduction = 10; // Greater than base threshold

            effect.ApplyEffect();

            Assert.AreEqual(1, penguin.OverloadThreshold, "Overload threshold must be clamped to at least 1");
        }

        [Test]
        public void InventoryManager_AddAndRemoveCharm_IntegratesProperly()
        {
            var penguin = CreateMockPenguin("Penguin", overloadThreshold: 10);

            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "S-02";
            charm.ItemName = "소수정예 T4";
            charm.Type = ShopItemType.Charm;
            charm.Price = 25;
            charm.IsConsumable = false;
            charm.Effects.Add(effect);
            createdScriptableObjects.Add(charm);

            Assert.AreEqual(3, deckManager.MaxFieldCount);
            Assert.AreEqual(10, penguin.OverloadThreshold);

            // Equip charm via InventoryManager
            bool added = inventoryManager.AddCharm(charm);
            Assert.IsTrue(added);
            Assert.AreEqual(2, deckManager.MaxFieldCount, "Equipping charm should limit field slots to 2");
            Assert.AreEqual(8, penguin.OverloadThreshold, "Equipping charm should reduce overload threshold to 8");

            // Remove charm via InventoryManager
            inventoryManager.RemoveCharm(charm);
            Assert.AreEqual(3, deckManager.MaxFieldCount, "Removing charm should restore field slots to 3");
            Assert.AreEqual(10, penguin.OverloadThreshold, "Removing charm should restore overload threshold to 10");
        }
    }
}
