using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using PenguinPinball.Core;
using PenguinPinball.UI.Views;
using PenguinPinball.UI.ViewModels;
using PenguinPinball.UI.Util;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class BenchSlotUITests
    {
        private GameObject canvasObj;
        private GameObject slotObj;
        private BenchSlotUI benchSlotUI;
        private GameObject deckManagerObj;
        private GameObject inventoryManagerObj;

        private void CallAwake(MonoBehaviour mb)
        {
            var method = mb.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) method.Invoke(mb, null);
        }

        [SetUp]
        public void SetUp()
        {
            canvasObj = new GameObject("TestCanvas", typeof(Canvas));
            slotObj = new GameObject("TestBenchSlot", typeof(RectTransform), typeof(Button), typeof(Image));
            slotObj.transform.SetParent(canvasObj.transform, false);

            benchSlotUI = slotObj.AddComponent<BenchSlotUI>();
            benchSlotUI.EnsureHierarchy();

            inventoryManagerObj = new GameObject("ItemInventoryManager");
            var inv = inventoryManagerObj.AddComponent<ItemInventoryManager>();
            CallAwake(inv);

            deckManagerObj = new GameObject("PenguinDeckManager");
            var deck = deckManagerObj.AddComponent<PenguinDeckManager>();
            CallAwake(deck);
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasObj != null) Object.DestroyImmediate(canvasObj);
            if (deckManagerObj != null) Object.DestroyImmediate(deckManagerObj);
            if (inventoryManagerObj != null) Object.DestroyImmediate(inventoryManagerObj);
        }

        [Test]
        public void BenchSlotUI_EnsureHierarchy_CreatesAllRequiredElements()
        {
            Assert.IsNotNull(benchSlotUI.BorderImage, "BorderImage should be created");
            Assert.IsNotNull(benchSlotUI.ModelPreviewRawImage, "ModelPreviewRawImage should be created");
            Assert.IsFalse(benchSlotUI.ModelPreviewRawImage.raycastTarget, "ModelPreview RawImage must NOT block raycasts for button clicks");
            Assert.IsFalse(benchSlotUI.BorderImage.raycastTarget, "BorderImage must NOT block raycasts for button clicks");
            Assert.Less(benchSlotUI.ModelPreviewRawImage.transform.GetSiblingIndex(), benchSlotUI.BorderImage.transform.GetSiblingIndex(), "Model preview must be rendered behind the border frame");
            Assert.AreEqual(5, benchSlotUI.StarImages.Count, "5 StarImages should be created");
            Assert.AreEqual(6, benchSlotUI.CharmCircleSlots.Count, "6 CharmCircleSlots should be created");
            Assert.AreEqual(6, benchSlotUI.CharmIconImages.Count, "6 CharmIconImages should be created");
        }

        [Test]
        public void BenchSlotUI_BorderColor_ChangesAccordingToTier()
        {
            // Empty slot (tier 0) -> defaultBorderColor
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.DefaultBorderColor, benchSlotUI.BorderImage.color);

            // Tier 1 -> 회색 (Gray)
            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.Level1Color, benchSlotUI.BorderImage.color);

            // Tier 2 -> 연두 (Light Green)
            benchSlotUI.UpdateVisuals(tier: 2, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.Level2Color, benchSlotUI.BorderImage.color);

            // Tier 3 -> 하늘 (Sky Blue)
            benchSlotUI.UpdateVisuals(tier: 3, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.Level3Color, benchSlotUI.BorderImage.color);

            // Tier 4 -> 노랑 (Yellow)
            benchSlotUI.UpdateVisuals(tier: 4, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.Level4Color, benchSlotUI.BorderImage.color);

            // Tier 5 -> 빨강 (Red)
            benchSlotUI.UpdateVisuals(tier: 5, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.AreEqual(benchSlotUI.Level5Color, benchSlotUI.BorderImage.color);

            // Locked slot -> lockedBorderColor
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: true, isDisabled: true);
            Assert.AreEqual(benchSlotUI.LockedBorderColor, benchSlotUI.BorderImage.color);
        }

        [Test]
        public void BenchSlotUI_Stars_MatchPenguinTier()
        {
            // Tier 0 (Empty) -> All stars inactive
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: false);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(benchSlotUI.StarImages[i].gameObject.activeSelf, $"Star {i} should be inactive for tier 0");
            }

            // Tier 1 -> 1 star active
            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: null, isLocked: false, isDisabled: false);
            Assert.IsTrue(benchSlotUI.StarImages[0].gameObject.activeSelf);
            Assert.IsFalse(benchSlotUI.StarImages[1].gameObject.activeSelf);

            // Tier 4 -> 4 stars active
            benchSlotUI.UpdateVisuals(tier: 4, activeCharms: null, isLocked: false, isDisabled: false);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(benchSlotUI.StarImages[i].gameObject.activeSelf, $"Star {i} should be active for tier 4");
            }
            Assert.IsFalse(benchSlotUI.StarImages[4].gameObject.activeSelf);

            // Tier 5 -> 5 stars active
            benchSlotUI.UpdateVisuals(tier: 5, activeCharms: null, isLocked: false, isDisabled: false);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(benchSlotUI.StarImages[i].gameObject.activeSelf, $"Star {i} should be active for tier 5");
            }

            // Locked slot -> Stars inactive
            benchSlotUI.UpdateVisuals(tier: 3, activeCharms: null, isLocked: true, isDisabled: true);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(benchSlotUI.StarImages[i].gameObject.activeSelf);
            }
        }

        [Test]
        public void BenchSlotUI_ActiveCharms_DisplaysUpTo6CirclesWithIcons()
        {
            var dummyTexture = new Texture2D(16, 16);
            var testSprite = Sprite.Create(dummyTexture, new Rect(0, 0, 16, 16), Vector2.zero);

            var charms = new List<ShopItemBase>();
            for (int i = 0; i < 6; i++)
            {
                var charm = ScriptableObject.CreateInstance<ShopItemBase>();
                charm.ItemID = $"Charm_{i}";
                charm.ItemName = $"Charm {i}";
                charm.Icon = testSprite;
                charms.Add(charm);
            }

            // 0 charms -> All circles inactive
            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: null, isLocked: false, isDisabled: false);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsFalse(benchSlotUI.CharmCircleSlots[i].activeSelf, $"Circle {i} should be inactive for 0 charms");
            }

            // 2 charms (Example 1 in user sketch)
            var twoCharms = new List<ShopItemBase> { charms[0], charms[1] };
            benchSlotUI.UpdateVisuals(tier: 5, activeCharms: twoCharms, isLocked: false, isDisabled: false);
            Assert.IsTrue(benchSlotUI.CharmCircleSlots[0].activeSelf);
            Assert.IsTrue(benchSlotUI.CharmCircleSlots[1].activeSelf);
            Assert.IsFalse(benchSlotUI.CharmCircleSlots[2].activeSelf);
            Assert.AreEqual(testSprite, benchSlotUI.CharmIconImages[0].sprite);
            Assert.AreEqual(testSprite, benchSlotUI.CharmIconImages[1].sprite);

            // 6 charms (Example 2 in user sketch: max capacity)
            benchSlotUI.UpdateVisuals(tier: 4, activeCharms: charms, isLocked: false, isDisabled: false);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(benchSlotUI.CharmCircleSlots[i].activeSelf, $"Circle {i} should be active for 6 charms");
                Assert.AreEqual(testSprite, benchSlotUI.CharmIconImages[i].sprite);
            }

            // Fallback placeholder test: charm with null icon should use placeholder sprite
            var nullIconCharm = ScriptableObject.CreateInstance<ShopItemBase>();
            nullIconCharm.ItemID = "NullIconCharm";
            nullIconCharm.Icon = null;
            var nullCharms = new List<ShopItemBase> { nullIconCharm };

            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: nullCharms, isLocked: false, isDisabled: false);
            Assert.IsTrue(benchSlotUI.CharmCircleSlots[0].activeSelf);
            Assert.IsNotNull(benchSlotUI.CharmIconImages[0].sprite, "Placeholder sprite must be assigned when charm icon is null");
            Assert.IsTrue(benchSlotUI.CharmIconImages[0].enabled, "Charm icon Image component must be enabled with placeholder sprite");

            // Empty bench slot (tier: 0, isLocked: false) should also display active charms
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: nullCharms, isLocked: false, isDisabled: false);
            Assert.IsTrue(benchSlotUI.CharmCircleSlots[0].activeSelf, "Empty bench slot should display active charms");

            // Locked slot (isLocked: true) should hide charms
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: nullCharms, isLocked: true, isDisabled: true);
            Assert.IsFalse(benchSlotUI.CharmCircleSlots[0].activeSelf, "Locked slot should hide charms");

            Object.DestroyImmediate(nullIconCharm);
            Object.DestroyImmediate(dummyTexture);
            foreach (var c in charms) Object.DestroyImmediate(c);
        }

        [Test]
        public void BenchSlotCharmUtility_DetectsCharmTargetingBenchSlot()
        {
            var inv = ItemInventoryManager.Instance;
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "BenchBuffCharm";
            charm.ItemName = "1st Bench Attack Buff";

            var effect = ScriptableObject.CreateInstance<Effect_BenchLaunchStatBuff>();
            effect.targetBenchSlots = new List<int> { 1 }; // 1번 벤치 (0-based index 0)
            charm.Effects.Add(effect);

            inv.AddCharm(charm);

            // 0번 슬롯 (1번 벤치)에는 효과 적용
            var charmsForSlot0 = BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(0);
            Assert.AreEqual(1, charmsForSlot0.Count);
            Assert.AreEqual(charm, charmsForSlot0[0]);

            // 1번 슬롯 (2번 벤치)에는 효과 미적용
            var charmsForSlot1 = BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(1);
            Assert.AreEqual(0, charmsForSlot1.Count);

            // 전역 효과 부적 (특정 슬롯 제한 없음) -> 전체 벤치 슬롯에 표시
            var generalCharm = ScriptableObject.CreateInstance<ShopItemBase>();
            generalCharm.ItemID = "GeneralCharm";
            inv.AddCharm(generalCharm);

            var charmsForSlot0After = BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(0);
            Assert.IsTrue(charmsForSlot0After.Contains(generalCharm), "Global charm should affect slot 0");
            var charmsForSlot1After = BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(1);
            Assert.IsTrue(charmsForSlot1After.Contains(generalCharm), "Global charm should affect slot 1");

            Object.DestroyImmediate(generalCharm);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(charm);
        }

        [Test]
        public void BenchViewModel_PopulatesTierAndActiveCharms()
        {
            var inv = ItemInventoryManager.Instance;
            var charm = ScriptableObject.CreateInstance<ShopItemBase>();
            charm.ItemID = "TestCharm";
            var effect = ScriptableObject.CreateInstance<Effect_BenchLaunchStatBuff>();
            effect.targetBenchSlots = new List<int> { 1 }; // slot 0
            charm.Effects.Add(effect);
            inv.AddCharm(charm);

            var deck = PenguinDeckManager.Instance;
            var pObj = new GameObject("PenguinTier5", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController), typeof(PenguinLocationData));
            var ctrl = pObj.GetComponent<PenguinController>();
            ctrl.SetTier(5);
            var loc = pObj.GetComponent<PenguinLocationData>();
            loc.Location = PenguinLocation.Bench;
            deck.RegisterPenguin(pObj);

            var vm = new BenchViewModel();
            vm.Initialize();

            Assert.AreEqual(6, vm.Slots.Count);
            Assert.AreEqual(5, vm.Slots[0].Tier);
            Assert.AreEqual(1, vm.Slots[0].ActiveCharms.Count);
            Assert.AreEqual(charm, vm.Slots[0].ActiveCharms[0]);

            // Slot 1 is empty, tier 0, no charms for slot 1
            Assert.AreEqual(0, vm.Slots[1].Tier);
            Assert.AreEqual(0, vm.Slots[1].ActiveCharms.Count);

            vm.Dispose();
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(charm);
            Object.DestroyImmediate(pObj);
        }

        [Test]
        public void BenchView_InitializesSlotUIs_AndUpdatesVisuals()
        {
            var benchViewObj = new GameObject("BenchView", typeof(BenchView));
            benchViewObj.transform.SetParent(canvasObj.transform, false);

            var buttons = new List<Button>();
            var icons = new List<Image>();
            var texts = new List<TMPro.TextMeshProUGUI>();

            for (int i = 0; i < 6; i++)
            {
                var s = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Button), typeof(Image));
                s.transform.SetParent(benchViewObj.transform, false);
                buttons.Add(s.GetComponent<Button>());

                var iconChild = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconChild.transform.SetParent(s.transform, false);
                icons.Add(iconChild.GetComponent<Image>());

                var textChild = new GameObject("Name", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                textChild.transform.SetParent(s.transform, false);
                texts.Add(textChild.GetComponent<TMPro.TextMeshProUGUI>());
            }

            var bv = benchViewObj.GetComponent<BenchView>();
            typeof(BenchView).GetField("slotButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bv, buttons);
            typeof(BenchView).GetField("slotIconImages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bv, icons);
            typeof(BenchView).GetField("slotNameTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bv, texts);

            CallAwake(bv);

            Assert.IsNotNull(bv.SlotUIs);
            Assert.AreEqual(6, bv.SlotUIs.Count);

            // Each slot has BenchSlotUI attached and initialized
            for (int i = 0; i < 6; i++)
            {
                Assert.IsNotNull(bv.SlotUIs[i]);
                Assert.IsNotNull(bv.SlotUIs[i].BorderImage);
            }

            Object.DestroyImmediate(benchViewObj);
        }

        [Test]
        public void BenchSlotUI_UpdateVisuals_Controls3DModelPreview()
        {
            var penguinObj = new GameObject("TestPenguin3D", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController));
            var visualRoot = new GameObject("visualRoot");
            visualRoot.transform.SetParent(penguinObj.transform, false);
            var modelMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelMesh.name = "DummyModel";
            modelMesh.transform.SetParent(visualRoot.transform, false);

            var ctrl = penguinObj.GetComponent<PenguinController>();

            // 1. With PenguinController -> 3D model preview should be created and active
            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: null, isLocked: false, isDisabled: false, ctrl);
            Assert.IsTrue(benchSlotUI.Preview3D.HasActive3DModel);
            Assert.IsNotNull(benchSlotUI.Preview3D.CurrentModelInstance);
            Assert.IsTrue(benchSlotUI.ModelPreviewRawImage.gameObject.activeSelf);
            Assert.IsFalse(benchSlotUI.ModelPreviewRawImage.raycastTarget);
            Assert.IsNotNull(benchSlotUI.Preview3D.PreviewLight);
            Assert.AreEqual(LightType.Point, benchSlotUI.Preview3D.PreviewLight.type, "PreviewLight must be Point Light to avoid global scene overexposure");
            Assert.LessOrEqual(benchSlotUI.Preview3D.PreviewLight.range, 10f, "PreviewLight range must be localized");
            Assert.IsTrue(benchSlotUI.Preview3D.PreviewLight.enabled);

            // 2. Clear model (when slot is empty or null controller passed)
            benchSlotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: false, null);
            Assert.IsFalse(benchSlotUI.Preview3D.HasActive3DModel);
            Assert.IsNull(benchSlotUI.Preview3D.CurrentModelInstance);
            Assert.IsFalse(benchSlotUI.ModelPreviewRawImage.gameObject.activeSelf);
            Assert.IsFalse(benchSlotUI.Preview3D.PreviewLight.enabled, "PreviewLight should be disabled when model is cleared");

            // 3. When slot is locked -> 3D model should be cleared even if controller provided
            benchSlotUI.UpdateVisuals(tier: 1, activeCharms: null, isLocked: true, isDisabled: true, ctrl);
            Assert.IsFalse(benchSlotUI.Preview3D.HasActive3DModel);
            Assert.IsFalse(benchSlotUI.Preview3D.PreviewLight.enabled);

            Object.DestroyImmediate(penguinObj);
        }

        [Test]
        public void BenchSlotUI_BasePenguinVisual_AppliesLoweredPositionOffset_ToPreventHeadCutoff()
        {
            var basicVisualObj = new GameObject("BasicPenguinVisual");
            
            benchSlotUI.Preview3D.SetModel(basicVisualObj);

            Assert.IsTrue(benchSlotUI.Preview3D.HasActive3DModel, "3D preview model should be active");
            Assert.IsNotNull(benchSlotUI.Preview3D.CurrentModelInstance);
            Assert.AreEqual(benchSlotUI.Preview3D.MeshPenguinPositionOffset.y, benchSlotUI.Preview3D.CurrentModelInstance.transform.localPosition.y, 0.001f, 
                "Base penguin visual model should be lowered by meshPenguinPositionOffset to prevent head cutoff");
            Assert.GreaterOrEqual(benchSlotUI.Preview3D.OrthographicSize, 0.55f, "OrthographicSize should be expanded to comfortably fit penguin height");

            Object.DestroyImmediate(basicVisualObj);
        }

        [Test]
        public void BenchSlotUI_SpherePlaceholder_DoesNotApplyLoweredOffset()
        {
            var tempObj = new GameObject("tempVisualRoot");
            
            benchSlotUI.Preview3D.SetModel(tempObj);

            Assert.IsTrue(benchSlotUI.Preview3D.HasActive3DModel);
            Assert.IsNotNull(benchSlotUI.Preview3D.CurrentModelInstance);
            Assert.AreEqual(0f, benchSlotUI.Preview3D.CurrentModelInstance.transform.localPosition.y, 0.001f, 
                "Placeholder sphere model (tempVisualRoot) should remain at uprightPositionOffset (0,0,0)");

            Object.DestroyImmediate(tempObj);
        }
    }
}
