using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class RepeatCustomerStoreCharmTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private Effect_RepeatCustomerStore effect;
        private ShopItemBase charmItem;
        private GameManager gameManager;
        private EconomyManager economyManager;
        private ShopManager shopManager;
        private PenguinDeckManager deckManager;
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
            gameManager.ChangeState(GameState.MaintenanceTime);

            // 2. EconomyManager
            var ecoGo = new GameObject("EconomyManager", typeof(EconomyManager));
            economyManager = ecoGo.GetComponent<EconomyManager>();
            SetInstance(typeof(EconomyManager), economyManager);
            createdGameObjects.Add(ecoGo);
            economyManager.AddGold(100);

            // 3. PenguinDeckManager
            var deckGo = new GameObject("PenguinDeckManager", typeof(PenguinDeckManager));
            deckManager = deckGo.GetComponent<PenguinDeckManager>();
            SetInstance(typeof(PenguinDeckManager), deckManager);
            createdGameObjects.Add(deckGo);

            // 4. ItemInventoryManager
            var invGo = new GameObject("ItemInventoryManager", typeof(ItemInventoryManager));
            inventoryManager = invGo.GetComponent<ItemInventoryManager>();
            SetInstance(typeof(ItemInventoryManager), inventoryManager);
            createdGameObjects.Add(invGo);

            // 5. ShopManager
            var shopGo = new GameObject("ShopManager", typeof(ShopManager));
            shopManager = shopGo.GetComponent<ShopManager>();
            SetInstance(typeof(ShopManager), shopManager);
            createdGameObjects.Add(shopGo);

            // 6. Effect and CharmItem
            effect = ScriptableObject.CreateInstance<Effect_RepeatCustomerStore>();
            effect.priceMultiplier = 2.0f;
            effect.additionalPurchasesPerSlot = 1;
            createdScriptableObjects.Add(effect);

            charmItem = ScriptableObject.CreateInstance<ShopItemBase>();
            charmItem.ItemID = "E-06";
            charmItem.ItemName = "단골 상점 T2";
            charmItem.Type = ShopItemType.Charm;
            charmItem.Price = 15;
            charmItem.IsConsumable = true;
            charmItem.MaxUses = 5;
            charmItem.CurrentUses = 5;
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

            SetInstance(typeof(ShopManager), null);
            SetInstance(typeof(ItemInventoryManager), null);
            SetInstance(typeof(PenguinDeckManager), null);
            SetInstance(typeof(EconomyManager), null);
            SetInstance(typeof(GameManager), null);
        }

        private PenguinDefinition CreateDefinition(string name)
        {
            var def = ScriptableObject.CreateInstance<PenguinDefinition>();
            def.name = name;
            createdScriptableObjects.Add(def);
            return def;
        }

        [Test]
        public void DefaultValues_MatchT2Specification()
        {
            var freshEffect = ScriptableObject.CreateInstance<Effect_RepeatCustomerStore>();
            try
            {
                Assert.AreEqual(2.0f, freshEffect.priceMultiplier, 0.01f, "Price multiplier should be 2.0 (2배)");
                Assert.AreEqual(1, freshEffect.additionalPurchasesPerSlot, "Additional purchases per slot should be 1 (1회 추가 구매)");
            }
            finally
            {
                Object.DestroyImmediate(freshEffect);
            }
        }

        [Test]
        public void ApplyAndRemoveEffect_RegistersAndUnregistersWithShopManager()
        {
            Assert.IsFalse(shopManager.IsRepeatStoreActive, "Repeat store should be inactive initially");

            inventoryManager.AddCharm(charmItem);
            Assert.IsTrue(shopManager.IsRepeatStoreActive, "Repeat store should be active after adding charm");

            inventoryManager.RemoveCharm(charmItem);
            Assert.IsFalse(shopManager.IsRepeatStoreActive, "Repeat store should be inactive after removing charm");
        }

        [Test]
        public void FirstPurchase_RegularPrice_TurnsSlotIntoRepurchase()
        {
            inventoryManager.AddCharm(charmItem);

            var pDef = CreateDefinition("TestPenguin");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { pDef, null, null };

            int initialGold = economyManager.CurrentGold;
            int basePrice = shopManager.GetHatcherySlotPrice(0);
            Assert.AreEqual(10, basePrice, "Base price should be 10G");

            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(initialGold - 10, economyManager.CurrentGold, "10G should be spent for first purchase");
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0), "Slot 0 should now be marked as repurchase");
            Assert.AreEqual(pDef, shopManager.CurrentHatcheryItems[0], "Same penguin definition should remain in slot");
            Assert.AreEqual(20, shopManager.GetHatcherySlotPrice(0), "Repurchase price should be 2x (20G)");
            Assert.AreEqual(5, charmItem.CurrentUses, "Charm uses should not be consumed yet");
        }

        [Test]
        public void Repurchase_DeductsDoublePrice_SpawnsPenguin_ConsumesCharmUse_MakesSlotSoldOut()
        {
            inventoryManager.AddCharm(charmItem);

            var pDef = CreateDefinition("TestPenguin");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { pDef, null, null };

            // 1st purchase (regular)
            shopManager.BuyHatcheryItem(0);
            Assert.AreEqual(90, economyManager.CurrentGold);

            // 2nd purchase (repurchase at 2x price)
            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(70, economyManager.CurrentGold, "20G should be spent for repurchase (90 - 20 = 70)");
            Assert.AreEqual(4, charmItem.CurrentUses, "Charm usage should be decremented from 5 to 4");
            Assert.IsNull(shopManager.CurrentHatcheryItems[0], "Slot should be sold out (null) after 1 additional repurchase");
            Assert.IsFalse(shopManager.IsHatcherySlotRepurchase(0), "Slot should no longer be marked as repurchase");
        }

        [Test]
        public void CannotRepurchaseSecondTime_SlotIsSoldOut()
        {
            inventoryManager.AddCharm(charmItem);

            var pDef = CreateDefinition("TestPenguin");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { pDef, null, null };

            shopManager.BuyHatcheryItem(0); // 1st purchase
            shopManager.BuyHatcheryItem(0); // 2nd purchase (repurchase)

            // Try to buy 3rd time
            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(70, economyManager.CurrentGold, "No additional gold should be deducted");
            Assert.AreEqual(4, charmItem.CurrentUses, "Uses should remain 4");
        }

        [Test]
        public void CharmExhaustsAfter5Repurchases_AndCleansUpInventory()
        {
            inventoryManager.AddCharm(charmItem);

            for (int i = 0; i < 5; i++)
            {
                var pDef = CreateDefinition($"Penguin_{i}");
                shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { pDef, null, null };

                shopManager.BuyHatcheryItem(0); // Regular purchase
                Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0));

                shopManager.BuyHatcheryItem(0); // Repurchase
                Assert.IsNull(shopManager.CurrentHatcheryItems[0]);
            }

            Assert.AreEqual(0, charmItem.CurrentUses, "CurrentUses should be 0");
            Assert.IsFalse(inventoryManager.OwnedCharms.Contains(charmItem), "Charm should be removed from inventory when uses reach 0");
            Assert.IsFalse(shopManager.IsRepeatStoreActive, "Repeat store should become inactive after exhaustion");
        }

        [Test]
        public void WhenCharmExhausts_RemainingRepurchaseSlotsAreCleared()
        {
            // Set charm to 1 use remaining
            charmItem.CurrentUses = 1;
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("PenguinA");
            var defB = CreateDefinition("PenguinB");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, defB, null };

            // Buy both regular penguins
            shopManager.BuyHatcheryItem(0); // Slot 0 becomes repurchase
            shopManager.BuyHatcheryItem(1); // Slot 1 becomes repurchase

            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0));
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(1));

            // Repurchase Slot 0 -> this exhausts the last remaining use (1 -> 0)
            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(0, charmItem.CurrentUses);
            Assert.IsNull(shopManager.CurrentHatcheryItems[0]);
            // Slot 1 should be reverted to null (품절) because the charm has expired
            Assert.IsNull(shopManager.CurrentHatcheryItems[1], "Remaining repurchase slots should become null when charm expires");
            Assert.IsFalse(shopManager.IsHatcherySlotRepurchase(1));
        }

        [Test]
        public void RerollHatchery_ClearsRepurchaseSlots_WithoutConsumingCharmUses()
        {
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("PenguinA");
            var defB = CreateDefinition("PenguinB");
            shopManager.HatcheryPool = new List<PenguinDefinition> { defA, defB };
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, null, null };

            // Buy regular
            shopManager.BuyHatcheryItem(0);
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0));

            // Reroll hatchery (free)
            shopManager.RerollHatchery(freeReroll: true);

            Assert.IsFalse(shopManager.IsHatcherySlotRepurchase(0), "Repurchase slots should be reset after reroll");
            Assert.AreEqual(5, charmItem.CurrentUses, "Charm uses should remain untouched after reroll");
        }

        [Test]
        public void RemoveCharm_ClearsActiveRepurchaseSlots()
        {
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("PenguinA");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, null, null };

            shopManager.BuyHatcheryItem(0);
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0));
            Assert.AreEqual(defA, shopManager.CurrentHatcheryItems[0]);

            // Unequip / remove charm
            inventoryManager.RemoveCharm(charmItem);

            Assert.IsFalse(shopManager.IsHatcherySlotRepurchase(0), "Slot should no longer be marked as repurchase");
            Assert.IsNull(shopManager.CurrentHatcheryItems[0], "Slot should revert to null (품절) when charm is removed");
        }

        [Test]
        public void InsufficientGold_BlocksRepurchase()
        {
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("PenguinA");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, null, null };

            shopManager.BuyHatcheryItem(0); // Spends 10G, leaving 90G
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0));

            // Reduce gold to 15G (less than 20G needed)
            economyManager.SpendGold(75);
            Assert.AreEqual(15, economyManager.CurrentGold);

            // Attempt repurchase
            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(15, economyManager.CurrentGold, "Gold should not be spent when insufficient");
            Assert.AreEqual(5, charmItem.CurrentUses, "Charm use should not be consumed");
            Assert.IsTrue(shopManager.IsHatcherySlotRepurchase(0), "Slot should still remain available as repurchase");
            Assert.AreEqual(defA, shopManager.CurrentHatcheryItems[0]);
        }

        [Test]
        public void NonMaintenanceTime_BlocksPurchase()
        {
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("PenguinA");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, null, null };

            gameManager.ChangeState(GameState.WaveInProgress);

            shopManager.BuyHatcheryItem(0);

            Assert.AreEqual(100, economyManager.CurrentGold, "Purchase must be blocked outside maintenance time");
            Assert.AreEqual(5, charmItem.CurrentUses);
        }

        [Test]
        public void HatcheryViewModel_CorrectlyReflectsRepurchaseStateAndPrice()
        {
            inventoryManager.AddCharm(charmItem);

            var defA = CreateDefinition("EngineerPenguin");
            shopManager.CurrentHatcheryItems = new List<PenguinDefinition> { defA, null, null };

            var viewModel = new HatcheryViewModel();
            viewModel.Initialize();

            try
            {
                // Before purchase: base price 10G
                Assert.AreEqual(10, viewModel.HatcherySlots[0].Price);
                Assert.AreEqual("EngineerPenguin", viewModel.HatcherySlots[0].ItemName);
                Assert.AreEqual("Penguin", viewModel.HatcherySlots[0].ItemTypeStr);
                Assert.IsFalse(viewModel.HatcherySlots[0].IsSoldOut);

                // Buy regular -> triggers repurchase
                viewModel.RequestBuyHatcheryItem(0);

                // After purchase: price 20G, name has (재구매), type is Repurchase
                Assert.AreEqual(20, viewModel.HatcherySlots[0].Price);
                Assert.IsTrue(viewModel.HatcherySlots[0].ItemName.Contains("(재구매)"), $"Expected (재구매) in name, got: {viewModel.HatcherySlots[0].ItemName}");
                Assert.AreEqual("Repurchase", viewModel.HatcherySlots[0].ItemTypeStr);
                Assert.IsFalse(viewModel.HatcherySlots[0].IsSoldOut);

                // Buy repurchase
                viewModel.RequestBuyHatcheryItem(0);

                // After repurchase: sold out
                Assert.IsTrue(viewModel.HatcherySlots[0].IsSoldOut);
                Assert.AreEqual("품절", viewModel.HatcherySlots[0].ItemName);
            }
            finally
            {
                viewModel.Dispose();
            }
        }
    }
}
