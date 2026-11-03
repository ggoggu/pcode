using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class SynthesisCloneCharmTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private Effect_SynthesisClone effect;
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

            // Effect_SynthesisClone
            effect = ScriptableObject.CreateInstance<Effect_SynthesisClone>();
            effect.cloneTier = 1;
            effect.cloneCount = 1;
            createdScriptableObjects.Add(effect);
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
            SetInstance(typeof(GameManager), null);
            SetInstance(typeof(ItemInventoryManager), null);
        }

        private PenguinDefinition CreateDefinition(string name)
        {
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = name;
            createdScriptableObjects.Add(def);
            return def;
        }

        private PenguinController CreateMockPenguin(PenguinDefinition def, int tier = 1, PenguinLocation location = PenguinLocation.Bench)
        {
            var go = new GameObject($"Penguin_{def.name}_T{tier}", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinLocationData), typeof(PenguinController));
            createdGameObjects.Add(go);

            var controller = go.GetComponent<PenguinController>();
            controller.Init(def);
            controller.SetTier(tier);

            var locData = go.GetComponent<PenguinLocationData>();
            locData.Location = location;

            deckManager.RegisterPenguin(go);
            return controller;
        }

        [Test]
        public void DefaultValues_MatchT3Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_SynthesisClone>();
            try
            {
                Assert.AreEqual(1, freshEffect.cloneTier, "Default clone tier should be 1 (1성)");
                Assert.AreEqual(1, freshEffect.cloneCount, "Default clone count should be 1 (1마리)");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void Synthesis_CreatesBonusPenguinAtTier1()
        {
            effect.ApplyEffect();

            var def = CreateDefinition("Adelie");

            // Register first 1-star penguin (does not merge)
            var p1 = CreateMockPenguin(def, 1);
            Assert.AreEqual(1, deckManager.OwnedPenguins.Count);

            // Register second 1-star penguin -> triggers merge!
            // 2 materials merge into 1 2-star penguin, AND effect adds 1 bonus 1-star penguin!
            var p2 = CreateMockPenguin(def, 1);

            // Total penguins should now be 2: (1 2-star + 1 bonus 1-star)
            Assert.AreEqual(2, deckManager.OwnedPenguins.Count, "After merge and clone, deck should have 2 penguins (2-star + bonus 1-star)");

            var controllers = deckManager.OwnedPenguins
                .Select(p => p.GetComponent<PenguinController>())
                .ToList();

            var tier2Count = controllers.Count(c => c.Tier == 2);
            var tier1Count = controllers.Count(c => c.Tier == 1);

            Assert.AreEqual(1, tier2Count, "Should have 1 2-star penguin");
            Assert.AreEqual(1, tier1Count, "Should have 1 bonus 1-star penguin");
            Assert.IsTrue(controllers.All(c => c.Definition == def), "All penguins should have the merged definition");
        }

        [Test]
        public void RemoveEffect_UnsubscribesFromSynthesis()
        {
            effect.ApplyEffect();
            effect.RemoveEffect();

            var def = CreateDefinition("Adelie");

            CreateMockPenguin(def, 1);
            CreateMockPenguin(def, 1);

            // Merge happened, but effect was removed, so NO bonus penguin created
            // 2 1-star penguins became 1 2-star penguin
            Assert.AreEqual(1, deckManager.OwnedPenguins.Count, "Without active effect, merge should result in exactly 1 2-star penguin");
            var c = deckManager.OwnedPenguins[0].GetComponent<PenguinController>();
            Assert.AreEqual(2, c.Tier);
        }

        [Test]
        public void ConsumableCharm_DecreasesUsesEachSynthesis()
        {
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "E-02";
            charm.ItemName = "합성 복제 T3";
            charm.Type = ShopItemType.Charm;
            charm.Price = 20;
            charm.IsConsumable = true;
            charm.MaxUses = 5;
            charm.CurrentUses = 5;
            charm.Effects.Add(effect);
            createdScriptableObjects.Add(charm);

            inventoryManager.AddCharm(charm);
            Assert.AreEqual(5, charm.CurrentUses);

            var def = CreateDefinition("Emperor");

            // 1st synthesis
            CreateMockPenguin(def, 1);
            CreateMockPenguin(def, 1);
            Assert.AreEqual(4, charm.CurrentUses, "First synthesis should decrease uses to 4");

            // Remove the cloned 1-star so it doesn't immediately merge with the next one
            var cloned = deckManager.OwnedPenguins.FirstOrDefault(p => p.GetComponent<PenguinController>().Tier == 1);
            if (cloned != null) deckManager.UnregisterPenguin(cloned);

            // 2nd synthesis
            CreateMockPenguin(def, 1);
            CreateMockPenguin(def, 1);
            Assert.AreEqual(3, charm.CurrentUses, "Second synthesis should decrease uses to 3");
        }

        [Test]
        public void ConsumableCharm_DepletesAndRemovesAfter5Uses()
        {
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "E-02";
            charm.ItemName = "합성 복제 T3";
            charm.Type = ShopItemType.Charm;
            charm.Price = 20;
            charm.IsConsumable = true;
            charm.MaxUses = 5;
            charm.CurrentUses = 5;
            charm.Effects.Add(effect);
            createdScriptableObjects.Add(charm);

            inventoryManager.AddCharm(charm);
            Assert.IsTrue(inventoryManager.OwnedCharms.Contains(charm));

            var def = CreateDefinition("Gentoo");

            // Perform 5 syntheses sequentially
            for (int i = 0; i < 5; i++)
            {
                CreateMockPenguin(def, 1);
                CreateMockPenguin(def, 1);

                // Clean up 1-star clones to prevent unwanted automatic chain merges
                var clones = deckManager.OwnedPenguins
                    .Where(p => p.GetComponent<PenguinController>().Tier == 1)
                    .ToList();
                foreach (var cl in clones) deckManager.UnregisterPenguin(cl);
            }

            // Charm should now be depleted and removed from ItemInventoryManager
            Assert.IsFalse(inventoryManager.OwnedCharms.Contains(charm), "Charm should be removed from inventory after 5 uses");

            int penguinCountBefore = deckManager.OwnedPenguins.Count;

            // 6th synthesis should NOT produce any bonus clone
            CreateMockPenguin(def, 1);
            CreateMockPenguin(def, 1);

            // 2 added and merged into 1, so count increases by 1 (no bonus clone)
            Assert.AreEqual(penguinCountBefore + 1, deckManager.OwnedPenguins.Count, "6th synthesis should not create a bonus clone");
            var remaining1Stars = deckManager.OwnedPenguins.Count(p => p.GetComponent<PenguinController>().Tier == 1);
            Assert.AreEqual(0, remaining1Stars, "No 1-star clone should exist after depleted charm");
        }

        [Test]
        public void CascadingSynthesis_TriggersMultipleTimesSafely()
        {
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "E-02";
            charm.ItemName = "합성 복제 T3";
            charm.Type = ShopItemType.Charm;
            charm.Price = 20;
            charm.IsConsumable = true;
            charm.MaxUses = 5;
            charm.CurrentUses = 5;
            charm.Effects.Add(effect);
            createdScriptableObjects.Add(charm);

            inventoryManager.AddCharm(charm);

            var def = CreateDefinition("Chinstrap");

            // Setup: player has 1 1-star penguin already waiting
            CreateMockPenguin(def, 1);
            Assert.AreEqual(1, deckManager.OwnedPenguins.Count);

            // Now, introduce 2 more 1-star penguins one by one
            // First one adds to deck (now 2 1-star penguins) -> triggers Merge 1!
            // Merge 1 produces a 2-star, and charm spawns a bonus 1-star!
            // Charm uses: 5 -> 4.
            CreateMockPenguin(def, 1);

            // After Merge 1:
            // 1 2-star penguin exists
            // 1 bonus 1-star penguin exists
            // Total = 2 penguins
            Assert.AreEqual(4, charm.CurrentUses, "Should have used 1 charge for Merge 1");

            // Now add another 1-star penguin -> merges with the bonus 1-star penguin! (Merge 2)
            // Merge 2 produces a second 2-star penguin, and charm spawns another bonus 1-star!
            // Then the two 2-star penguins merge into a 3-star penguin! (Merge 3)
            // Merge 3 produces a 3-star, and charm spawns another bonus 1-star!
            CreateMockPenguin(def, 1);

            // Total 3 merges happened!
            // Initial charge: 5. 3 merges consumed 3 charges -> 2 charges remaining!
            Assert.AreEqual(2, charm.CurrentUses, "Cascading merges should consume charges sequentially");
        }
    }
}
