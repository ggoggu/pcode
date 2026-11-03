using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class SurfingCharmTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private Effect_Surfing effect;
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

            // Setup dummy item pool so RerollShop has items to pick
            var dummyItem = ScriptableObject.CreateInstance<ShopItemBase>();
            dummyItem.ItemID = "TEST_ITEM";
            dummyItem.ItemName = "테스트 아이템";
            dummyItem.Type = ShopItemType.Charm;
            dummyItem.Price = 10;
            createdScriptableObjects.Add(dummyItem);
            shopManager.ItemPool.Add(dummyItem);

            // Setup dummy hatchery pool so RerollHatchery has items to pick
            var dummyPenguin = ScriptableObject.CreateInstance<PenguinDefinition>();
            dummyPenguin.name = "TestPenguin";
            createdScriptableObjects.Add(dummyPenguin);
            shopManager.HatcheryPool.Add(dummyPenguin);

            // 6. Effect and CharmItem
            effect = ScriptableObject.CreateInstance<Effect_Surfing>();
            effect.freeRerollsPerMaintenance = 5;
            effect.maxWaveDuration = 5;
            effect.shareFreeRerollsBetweenShops = true;
            effect.preventSellRefund = true;
            createdScriptableObjects.Add(effect);

            charmItem = ScriptableObject.CreateInstance<ShopItemBase>();
            charmItem.ItemID = "E-08";
            charmItem.ItemName = "파도타기 T1";
            charmItem.Type = ShopItemType.Charm;
            charmItem.Price = 10;
            charmItem.IsConsumable = true;
            charmItem.MaxUses = 5;
            charmItem.CurrentUses = 5;
            charmItem.NoSellRefund = true;
            charmItem.Description = "정비 시간에 무료 리롤을 5회 제공합니다. 5번의 웨이브 진행 이후 파괴됩니다. 판매해도 정산 없이 파괴됩니다.";
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

        [Test]
        public void DefaultValues_MatchT1Specification()
        {
            Assert.AreEqual("E-08", charmItem.ItemID, "ItemID should be E-08");
            Assert.AreEqual("파도타기 T1", charmItem.ItemName, "ItemName should be 파도타기 T1");
            Assert.AreEqual(10, charmItem.Price, "T1 Price should be 10G");
            Assert.IsTrue(charmItem.IsConsumable, "Should be consumable");
            Assert.AreEqual(5, charmItem.MaxUses, "Max uses should be 5");
            Assert.AreEqual(5, charmItem.CurrentUses, "Current uses should be 5");
            Assert.IsTrue(charmItem.NoSellRefund, "NoSellRefund should be true");
            Assert.AreEqual(0, charmItem.GetSellPrice(), "Sell price should be 0G");
            Assert.AreEqual(5, effect.freeRerollsPerMaintenance, "Free rerolls per maintenance should be 5");
            Assert.AreEqual(5, effect.maxWaveDuration, "Wave duration should be 5");
        }

        [Test]
        public void ApplyAndRemoveEffect_RegistersAndUnregistersWithShopManager()
        {
            Assert.IsFalse(shopManager.HasFreeRerolls, "Should have no free rerolls initially");

            inventoryManager.AddCharm(charmItem);
            Assert.IsTrue(shopManager.HasFreeRerolls, "Should have free rerolls after charm added");
            Assert.AreEqual(5, shopManager.RemainingFreeRerolls, "Should have 5 free rerolls initially");

            inventoryManager.RemoveCharm(charmItem);
            Assert.IsFalse(shopManager.HasFreeRerolls, "Should have no free rerolls after charm removed");
        }

        [Test]
        public void RerollShop_ConsumesFreeReroll_WithoutSpendingGoldOrIncrementingPaidCost()
        {
            inventoryManager.AddCharm(charmItem);

            int initialGold = economyManager.CurrentGold;
            Assert.AreEqual(0, shopManager.GetShopRerollCost(), "Reroll cost should be 0G when free rerolls available");

            shopManager.RerollShop();

            Assert.AreEqual(4, shopManager.RemainingFreeRerolls, "Remaining free rerolls should decrease to 4");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold should not be spent during free reroll");
            Assert.AreEqual(0, shopManager.ShopRerollCount, "Paid reroll count should not increment");
            Assert.AreEqual(0, shopManager.GetShopRerollCost(), "Next reroll cost should still be 0G while free rerolls remain");
        }

        [Test]
        public void RerollHatchery_ConsumesFreeReroll_SharedPool()
        {
            inventoryManager.AddCharm(charmItem);

            int initialGold = economyManager.CurrentGold;
            Assert.AreEqual(0, shopManager.GetHatcheryRerollCost(), "Hatchery reroll cost should be 0G when free rerolls available");

            shopManager.RerollShop();
            Assert.AreEqual(4, shopManager.RemainingFreeRerolls);

            shopManager.RerollHatchery();
            Assert.AreEqual(3, shopManager.RemainingFreeRerolls, "Shared pool should decrease to 3 after hatchery reroll");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold should not be spent");
            Assert.AreEqual(0, shopManager.HatcheryRerollCount, "Hatchery paid reroll count should not increment");
        }

        [Test]
        public void FreeReroll_WorksEvenWhenGoldIsZero()
        {
            inventoryManager.AddCharm(charmItem);

            // Drain all gold
            economyManager.SpendGold(economyManager.CurrentGold);
            Assert.AreEqual(0, economyManager.CurrentGold);

            // Free reroll should still execute successfully
            shopManager.RerollShop();

            Assert.AreEqual(4, shopManager.RemainingFreeRerolls);
            Assert.AreEqual(0, economyManager.CurrentGold);
        }

        [Test]
        public void ExhaustingFreeRerolls_TransitionsBackToPaidReroll()
        {
            inventoryManager.AddCharm(charmItem);

            // Consume all 5 free rerolls
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(shopManager.HasFreeRerolls);
                shopManager.RerollShop();
            }

            Assert.IsFalse(shopManager.HasFreeRerolls, "All free rerolls should be consumed");
            Assert.AreEqual(0, shopManager.RemainingFreeRerolls);

            // Next reroll should now cost 5G (base cost)
            Assert.AreEqual(5, shopManager.GetShopRerollCost(), "Reroll cost should revert to base cost (5G)");

            int goldBeforePaid = economyManager.CurrentGold;
            shopManager.RerollShop();

            Assert.AreEqual(goldBeforePaid - 5, economyManager.CurrentGold, "Paid reroll should spend 5G");
            Assert.AreEqual(1, shopManager.ShopRerollCount, "Paid reroll count should increment to 1");
            Assert.AreEqual(10, shopManager.GetShopRerollCost(), "Next reroll cost should double to 10G");
        }

        [Test]
        public void WaveProgression_ConsumesCharmUses_AndRefreshesFreeRerollsEachMaintenance()
        {
            inventoryManager.AddCharm(charmItem);
            Assert.AreEqual(5, charmItem.CurrentUses);
            Assert.AreEqual(5, shopManager.RemainingFreeRerolls);

            // Consume 2 rerolls in wave 1 prep
            shopManager.RerollShop();
            shopManager.RerollShop();
            Assert.AreEqual(3, shopManager.RemainingFreeRerolls);

            // Wave 1 progresses: Maintenance -> WaveInProgress -> Maintenance
            gameManager.ChangeState(GameState.WaveInProgress);
            gameManager.ChangeState(GameState.MaintenanceTime);

            // 1 wave completed: CurrentUses should be 4, free rerolls refreshed to 5
            Assert.AreEqual(4, charmItem.CurrentUses, "CurrentUses should decrement to 4 after 1 wave");
            Assert.AreEqual(5, shopManager.RemainingFreeRerolls, "Free rerolls should refresh to 5 in next maintenance");

            // Wave 2
            gameManager.ChangeState(GameState.WaveInProgress);
            gameManager.ChangeState(GameState.MaintenanceTime);
            Assert.AreEqual(3, charmItem.CurrentUses, "CurrentUses should decrement to 3 after 2 waves");
            Assert.AreEqual(5, shopManager.RemainingFreeRerolls);

            // Wave 3
            gameManager.ChangeState(GameState.WaveInProgress);
            gameManager.ChangeState(GameState.MaintenanceTime);
            Assert.AreEqual(2, charmItem.CurrentUses, "CurrentUses should decrement to 2 after 3 waves");

            // Wave 4
            gameManager.ChangeState(GameState.WaveInProgress);
            gameManager.ChangeState(GameState.MaintenanceTime);
            Assert.AreEqual(1, charmItem.CurrentUses, "CurrentUses should decrement to 1 after 4 waves");

            // Wave 5
            gameManager.ChangeState(GameState.WaveInProgress);
            gameManager.ChangeState(GameState.MaintenanceTime);

            // After 5 waves: charm should be destroyed and removed from inventory!
            Assert.IsFalse(inventoryManager.OwnedCharms.Contains(charmItem), "Charm should be destroyed and removed after 5 waves");
            Assert.IsFalse(shopManager.HasFreeRerolls, "ShopManager should no longer have free rerolls");
        }

        [Test]
        public void SellCharm_YieldsZeroGoldRefund_AndDestroysCharm()
        {
            inventoryManager.AddCharm(charmItem);
            Assert.IsTrue(inventoryManager.OwnedCharms.Contains(charmItem));

            int goldBeforeSell = economyManager.CurrentGold;

            // Sell the charm
            shopManager.SellItem(charmItem);

            Assert.IsFalse(inventoryManager.OwnedCharms.Contains(charmItem), "Charm should be removed from inventory on sell");
            Assert.AreEqual(goldBeforeSell, economyManager.CurrentGold, "Selling E-08 should yield 0 gold refund (정산 없이 파괴)");
            Assert.IsFalse(shopManager.HasFreeRerolls, "Free rerolls should be deactivated after sale");
        }

        [Test]
        public void ItemShopViewModel_ReflectsFreeRerollState()
        {
            inventoryManager.AddCharm(charmItem);

            var viewModel = new ItemShopViewModel();
            viewModel.Initialize();

            try
            {
                Assert.AreEqual(0, viewModel.ShopRerollCost, "ViewModel ShopRerollCost should be 0 when free reroll active");
                Assert.IsTrue(viewModel.HasFreeReroll, "ViewModel HasFreeReroll should be true");
                Assert.AreEqual(5, viewModel.RemainingFreeRerolls, "ViewModel RemainingFreeRerolls should be 5");

                viewModel.RequestRerollShop();

                Assert.AreEqual(4, viewModel.RemainingFreeRerolls, "ViewModel RemainingFreeRerolls should be 4 after reroll");
            }
            finally
            {
                viewModel.Dispose();
            }
        }

        [Test]
        public void HatcheryViewModel_ReflectsFreeRerollState()
        {
            inventoryManager.AddCharm(charmItem);

            var viewModel = new HatcheryViewModel();
            viewModel.Initialize();

            try
            {
                Assert.AreEqual(0, viewModel.HatcheryRerollCost, "ViewModel HatcheryRerollCost should be 0 when free reroll active");
                Assert.IsTrue(viewModel.HasFreeReroll, "ViewModel HasFreeReroll should be true");
                Assert.AreEqual(5, viewModel.RemainingFreeRerolls, "ViewModel RemainingFreeRerolls should be 5");

                viewModel.RequestRerollHatchery();

                Assert.AreEqual(4, viewModel.RemainingFreeRerolls, "ViewModel RemainingFreeRerolls should be 4 after hatchery reroll");
            }
            finally
            {
                viewModel.Dispose();
            }
        }
    }
}
