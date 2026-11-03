using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class GradeTransplantInstantItemTests
    {
        private List<GameObject> createdGameObjects;
        private List<ScriptableObject> createdScriptableObjects;
        private PenguinDeckManager deckManager;
        private GameManager gameManager;
        private EconomyManager economyManager;
        private ShopManager shopManager;
        private InstantItem_TierTransplant instantItem;

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

            // InstantItem_TierTransplant
            instantItem = ScriptableObject.CreateInstance<InstantItem_TierTransplant>();
            instantItem.ItemID = "I-01";
            instantItem.ItemName = "등급 이식 T3";
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

            var locData = go.GetComponent<PenguinLocationData>();
            locData.Location = PenguinLocation.Bench;

            deckManager.RegisterPenguin(go);
            return controller;
        }

        [Test]
        public void ItemProperties_MatchT3Specification()
        {
            Assert.AreEqual("I-01", instantItem.ItemID);
            Assert.AreEqual("등급 이식 T3", instantItem.ItemName);
            Assert.AreEqual(ShopItemType.Instant, instantItem.Type);
            Assert.AreEqual(InstantTargetType.TwoPenguins, instantItem.TargetType);
            Assert.IsTrue(instantItem.RequiresTargetSelection);
        }

        [Test]
        public void SwapPenguinTiers_CorrectlySwapsTiersAndRecalculatesStats()
        {
            var defA = CreateDefinition("Adelie");
            var defB = CreateDefinition("Emperor");

            var penguinA = CreateMockPenguin(defA, 1);
            var penguinB = CreateMockPenguin(defB, 3);

            Assert.AreEqual(1, penguinA.Tier);
            Assert.AreEqual(3, penguinB.Tier);

            float attackABefore = penguinA.Stats[PenguinPinball.Core.StatType.Attack].Value;
            float attackBBefore = penguinB.Stats[PenguinPinball.Core.StatType.Attack].Value;

            bool success = deckManager.SwapPenguinTiers(penguinA, penguinB);
            Assert.IsTrue(success);

            Assert.AreEqual(3, penguinA.Tier, "Penguin A should now be Tier 3");
            Assert.AreEqual(1, penguinB.Tier, "Penguin B should now be Tier 1");

            // Penguin A's attack stat should have increased due to higher tier
            Assert.Greater(penguinA.Stats[PenguinPinball.Core.StatType.Attack].Value, attackABefore);
            // Penguin B's attack stat should have decreased due to lower tier
            Assert.Less(penguinB.Stats[PenguinPinball.Core.StatType.Attack].Value, attackBBefore);
        }

        [Test]
        public void SwapPenguinTiers_AutomaticallyMergesMatchingTiers()
        {
            // Scenario:
            // Adelie 1 (Tier 1)
            // Adelie 2 (Tier 2)
            // Emperor (Tier 2)
            // Swap Adelie 1 (T1) and Emperor (T2) -> Adelie 1 becomes T2!
            // Now two Adelie T2 penguins exist -> Auto-merge into Adelie T3!
            var defAdelie = CreateDefinition("Adelie");
            var defEmperor = CreateDefinition("Emperor");

            var adelie1 = CreateMockPenguin(defAdelie, 1);
            var adelie2 = CreateMockPenguin(defAdelie, 2);
            var emperor = CreateMockPenguin(defEmperor, 2);

            Assert.AreEqual(3, deckManager.OwnedPenguins.Count);

            bool swapResult = deckManager.SwapPenguinTiers(adelie1, emperor);
            Assert.IsTrue(swapResult);

            // Emperor should be Tier 1
            var remainingEmperor = deckManager.OwnedPenguins
                .Select(p => p.GetComponent<PenguinController>())
                .FirstOrDefault(c => c.Definition == defEmperor);

            Assert.IsNotNull(remainingEmperor);
            Assert.AreEqual(1, remainingEmperor.Tier, "Emperor should now be Tier 1");

            // Two Adelie T2s merged into one Adelie T3!
            var adelies = deckManager.OwnedPenguins
                .Select(p => p.GetComponent<PenguinController>())
                .Where(c => c.Definition == defAdelie)
                .ToList();

            Assert.AreEqual(1, adelies.Count, "Two Adelie Tier 2 penguins should have merged into 1 penguin");
            Assert.AreEqual(3, adelies[0].Tier, "Merged Adelie should be Tier 3");
            Assert.AreEqual(2, deckManager.OwnedPenguins.Count, "Deck should now have 2 penguins total (Adelie T3 + Emperor T1)");
        }

        [Test]
        public void SwapPenguinTiers_CascadesMultipleMerges()
        {
            // Cascading merge test:
            // Adelie 1 (T1)
            // Adelie 2 (T2)
            // Adelie 3 (T3)
            // Emperor (T2)
            // Swap Adelie 1 (T1) and Emperor (T2)
            // -> Adelie 1 becomes T2
            // -> Adelie 1 (T2) + Adelie 2 (T2) merge into Adelie (T3)
            // -> That new Adelie (T3) + Adelie 3 (T3) merge into Adelie (T4)!
            var defAdelie = CreateDefinition("Adelie");
            var defEmperor = CreateDefinition("Emperor");

            var a1 = CreateMockPenguin(defAdelie, 1);
            var a2 = CreateMockPenguin(defAdelie, 2);
            var a3 = CreateMockPenguin(defAdelie, 3);
            var emp = CreateMockPenguin(defEmperor, 2);

            Assert.AreEqual(4, deckManager.OwnedPenguins.Count);

            bool success = deckManager.SwapPenguinTiers(a1, emp);
            Assert.IsTrue(success);

            var adelieResult = deckManager.OwnedPenguins
                .Select(p => p.GetComponent<PenguinController>())
                .FirstOrDefault(c => c.Definition == defAdelie);

            Assert.IsNotNull(adelieResult);
            Assert.AreEqual(4, adelieResult.Tier, "Cascading merge should result in Adelie Tier 4");
            Assert.AreEqual(2, deckManager.OwnedPenguins.Count, "Deck should have 2 penguins (Adelie T4 + Emperor T1)");
        }

        [Test]
        public void PredictMergesAfterSwap_AccuratelyPredictsSynthesis()
        {
            var defAdelie = CreateDefinition("Adelie");
            var defEmperor = CreateDefinition("Emperor");

            var adelie1 = CreateMockPenguin(defAdelie, 1);
            var adelie2 = CreateMockPenguin(defAdelie, 2);
            var emperor = CreateMockPenguin(defEmperor, 2);

            // Adelie1(T1)과 Emperor(T2) 교환 시, Adelie1이 T2가 되어 Adelie2(T2)와 합성될 것이 예측되어야 함
            bool willMerge = deckManager.PredictMergesAfterSwap(adelie1, emperor, out var predictions);
            Assert.IsTrue(willMerge, "PredictMergesAfterSwap should return true when swap causes a merge");
            Assert.AreEqual(1, predictions.Count);
            StringAssert.Contains("Adelie", predictions[0]);
            StringAssert.Contains("합성", predictions[0]);

            // 반대로 합성이 일어나지 않는 조합 테스트:
            // Emperor(T2)와 Adelie2(T2)를 바꾸면? (둘 다 T2이므로 티어 변화 없음 -> 추가 합성 없음)
            bool noMerge = deckManager.PredictMergesAfterSwap(adelie2, emperor, out var noPredictions);
            Assert.IsFalse(noMerge, "PredictMergesAfterSwap should return false when no merge will occur");
            Assert.AreEqual(0, noPredictions.Count);
        }

        [Test]
        public void SwapPenguinTiers_OutputsSynthesizedPenguinsList()
        {
            var defAdelie = CreateDefinition("Adelie");
            var defEmperor = CreateDefinition("Emperor");

            var adelie1 = CreateMockPenguin(defAdelie, 1);
            var adelie2 = CreateMockPenguin(defAdelie, 2);
            var emperor = CreateMockPenguin(defEmperor, 2);

            bool success = deckManager.SwapPenguinTiers(adelie1, emperor, out var synthesizedList);
            Assert.IsTrue(success);
            Assert.IsNotNull(synthesizedList);
            Assert.AreEqual(1, synthesizedList.Count, "Should output exactly 1 synthesized penguin");
            Assert.AreEqual(defAdelie, synthesizedList[0].Definition);
            Assert.AreEqual(3, synthesizedList[0].Tier, "Synthesized penguin should be Tier 3");
        }

        [Test]
        public void CanUse_ReturnsFalse_WhenLessThanTwoPenguinsOwned()
        {
            var def = CreateDefinition("Adelie");
            CreateMockPenguin(def, 1);

            Assert.AreEqual(1, deckManager.OwnedPenguins.Count);

            bool canUse = instantItem.CanUse(out string failReason);
            Assert.IsFalse(canUse);
            Assert.IsNotEmpty(failReason);
            StringAssert.Contains("1마리 이하", failReason);
        }

        [Test]
        public void CanUse_ReturnsFalse_WhenZeroPenguinsOwned()
        {
            Assert.AreEqual(0, deckManager.OwnedPenguins.Count);

            bool canUse = instantItem.CanUse(out string failReason);
            Assert.IsFalse(canUse);
            Assert.IsNotEmpty(failReason);
            StringAssert.Contains("1마리 이하", failReason);
        }

        [Test]
        public void ShopPurchaseFlow_RequiresTargetSelection_AndCompletesCorrectly()
        {
            // Setup shop state
            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            var defA = CreateDefinition("Adelie");
            var defB = CreateDefinition("Emperor");
            var p1 = CreateMockPenguin(defA, 1);
            var p2 = CreateMockPenguin(defB, 2);

            bool eventFired = false;
            InstantItem requestedItem = null;
            int requestedSlot = -1;

            shopManager.OnInstantTargetSelectionRequested += (item, slot) =>
            {
                eventFired = true;
                requestedItem = item;
                requestedSlot = slot;
            };

            int initialGold = economyManager.CurrentGold;

            // Step 1: Click buy in shop
            // Ensure Maintenance state
            gameManager.ChangeState(GameState.MaintenanceTime);
            shopManager.BuyShopItem(0);

            // Step 2: Target selection requested event should fire, gold NOT deducted yet, slot NOT cleared
            Assert.IsTrue(eventFired, "OnInstantTargetSelectionRequested should be triggered");
            Assert.AreEqual(instantItem, requestedItem);
            Assert.AreEqual(0, requestedSlot);
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold should NOT be deducted upon opening selection UI");
            Assert.AreEqual(instantItem, shopManager.CurrentShopItems[0], "Shop slot should NOT be cleared yet");

            // Step 3: Complete purchase with selected targets
            bool completeSuccess = shopManager.CompleteTargetedInstantPurchase(0, instantItem, p1, p2);
            Assert.IsTrue(completeSuccess);

            // Step 4: Gold deducted, effect applied, slot cleared
            Assert.AreEqual(initialGold - instantItem.Price, economyManager.CurrentGold, "Gold should be deducted upon confirmation");
            Assert.AreEqual(2, p1.Tier, "P1 should now be Tier 2");
            Assert.AreEqual(1, p2.Tier, "P2 should now be Tier 1");
            Assert.IsNull(shopManager.CurrentShopItems[0], "Shop slot should be cleared after purchase completion");
        }

        [Test]
        public void ShopPurchaseFlow_Cancel_DoesNotDeductGoldOrClearSlot()
        {
            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            var defA = CreateDefinition("Adelie");
            var defB = CreateDefinition("Emperor");
            CreateMockPenguin(defA, 1);
            CreateMockPenguin(defB, 2);

            gameManager.ChangeState(GameState.MaintenanceTime);
            int initialGold = economyManager.CurrentGold;

            shopManager.BuyShopItem(0);

            // Simulate user clicking Cancel in Modal UI
            shopManager.CancelTargetedInstantPurchase(0);

            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold should remain untouched after cancel");
            Assert.AreEqual(instantItem, shopManager.CurrentShopItems[0], "Shop slot should remain available after cancel");
        }

        [Test]
        public void ShopPurchaseFlow_CannotBuy_WhenOneOrZeroPenguinsOwned()
        {
            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            var defA = CreateDefinition("Adelie");
            var p1 = CreateMockPenguin(defA, 1);
            // Only 1 penguin owned!

            gameManager.ChangeState(GameState.MaintenanceTime);
            int initialGold = economyManager.CurrentGold;

            bool eventFired = false;
            shopManager.OnInstantTargetSelectionRequested += (item, slot) => eventFired = true;

            // 펭귄 1마리일 때 구매 시도 -> 구매 불가 (OnInstantTargetSelectionRequested 미발생)
            shopManager.BuyShopItem(0);

            Assert.IsFalse(eventFired, "OnInstantTargetSelectionRequested must NOT be triggered when 1 or fewer penguins are owned");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold must not be deducted");
            Assert.AreEqual(instantItem, shopManager.CurrentShopItems[0], "Shop slot must remain available");

            // 직접 완료를 시도하더라도 CanUse 실패로 거부되어야 함
            bool success = shopManager.CompleteTargetedInstantPurchase(0, instantItem, p1);
            Assert.IsFalse(success, "Purchase must fail when CanUse conditions are not met");
            Assert.AreEqual(initialGold, economyManager.CurrentGold, "Gold must not be deducted");
            Assert.AreEqual(instantItem, shopManager.CurrentShopItems[0], "Shop slot must remain available");
        }

        [Test]
        public void InstantItemSelectionController_SelectionFlow_WorksStepByStep()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            createdGameObjects.Add(canvasGo);

            var ctrlGo = new GameObject("SelectionController", typeof(RectTransform), typeof(PenguinPinball.UI.Views.InstantItemSelectionController));
            ctrlGo.transform.SetParent(canvasGo.transform, false);
            createdGameObjects.Add(ctrlGo);

            var controller = ctrlGo.GetComponent<PenguinPinball.UI.Views.InstantItemSelectionController>();
            Assert.IsNotNull(controller);

            var defA = CreateDefinition("Adelie");
            var defB = CreateDefinition("Emperor");
            var p1 = CreateMockPenguin(defA, 1);
            var p2 = CreateMockPenguin(defB, 3);

            shopManager.CurrentShopItems.Clear();
            shopManager.CurrentShopItems.Add(instantItem);

            var shopSlotGo = new GameObject("ShopSlot", typeof(RectTransform));
            createdGameObjects.Add(shopSlotGo);
            var p1SlotGo = new GameObject("P1Slot", typeof(RectTransform));
            createdGameObjects.Add(p1SlotGo);
            var p2SlotGo = new GameObject("P2Slot", typeof(RectTransform));
            createdGameObjects.Add(p2SlotGo);

            int initialGold = economyManager.CurrentGold;

            // Step 1: Start selection
            controller.StartSelection(instantItem, 0, shopSlotGo.GetComponent<RectTransform>());
            Assert.IsTrue(controller.IsActive, "Controller should be active");
            Assert.AreEqual(0, controller.SelectedPenguins.Count);

            // Step 2: Select first penguin
            controller.SelectPenguin(p1, p1SlotGo.GetComponent<RectTransform>());
            Assert.AreEqual(1, controller.SelectedPenguins.Count);
            Assert.AreEqual(p1, controller.SelectedPenguins[0]);

            // Step 3: Select second penguin
            controller.SelectPenguin(p2, p2SlotGo.GetComponent<RectTransform>());
            Assert.AreEqual(2, controller.SelectedPenguins.Count);
            Assert.AreEqual(p2, controller.SelectedPenguins[1]);

            // Step 4: Confirm exchange
            controller.ConfirmExchange();

            Assert.IsFalse(controller.IsActive, "Controller should be inactive after confirmation");
            Assert.AreEqual(3, p1.Tier, "P1 should now be Tier 3");
            Assert.AreEqual(1, p2.Tier, "P2 should now be Tier 1");
            Assert.AreEqual(initialGold - instantItem.Price, economyManager.CurrentGold, "Gold should be deducted");
            Assert.IsNull(shopManager.CurrentShopItems[0], "Shop slot should be cleared");
        }
    }
}
