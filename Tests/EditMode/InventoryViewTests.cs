using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PenguinPinball.UI.Views;
using PenguinPinball.Core;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class InventoryViewTests
    {
        private GameObject panelObj;
        private InventoryView inventoryView;
        private List<GameObject> slotObjects;

        private void CallMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (method != null) method.Invoke(target, null);
        }

        [SetUp]
        public void SetUp()
        {
            panelObj = new GameObject("InvnetoryPanel", typeof(RectTransform));
            inventoryView = panelObj.AddComponent<InventoryView>();

            var containerObj = new GameObject("SlotContainer", typeof(RectTransform));
            containerObj.transform.SetParent(panelObj.transform, false);

            slotObjects = new List<GameObject>();
            for (int i = 0; i < 9; i++)
            {
                var slot = new GameObject($"InventorySlotPrefab ({i})", typeof(RectTransform), typeof(Button), typeof(Image));
                slot.transform.SetParent(containerObj.transform, false);

                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(slot.transform, false);
                var iconImg = iconObj.GetComponent<Image>();
                iconImg.enabled = true; // Simulating prefab with enabled icon

                var textObj = new GameObject("name", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(slot.transform, false);
                var textComp = textObj.GetComponent<TextMeshProUGUI>();
                textComp.text = i == 3 ? "New Text" : "Dummy"; // Simulating 4th slot with default "New Text"

                slotObjects.Add(slot);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (panelObj != null) Object.DestroyImmediate(panelObj);
        }

        [Test]
        public void RebindSlotReferences_CorrectlyBindsAllSlots_AndClearsDefaultTextAndDisablesIcons()
        {
            // Act
            inventoryView.RebindSlotReferences();

            // Assert
            for (int i = 0; i < 9; i++)
            {
                var textComp = slotObjects[i].GetComponentInChildren<TextMeshProUGUI>(true);
                Assert.AreEqual(string.Empty, textComp.text, $"Slot {i} text should be cleared to empty string");

                var iconTf = slotObjects[i].transform.Find("Icon");
                var iconImg = iconTf.GetComponent<Image>();
                Assert.IsFalse(iconImg.enabled, $"Slot {i} icon should be disabled");
                Assert.IsNull(iconImg.sprite, $"Slot {i} icon sprite should be null");
            }
        }

        [Test]
        public void RebindSlotReferences_InitializesBenchSlotUI_OnAllSlots()
        {
            // Act
            inventoryView.RebindSlotReferences();

            // Assert
            Assert.IsNotNull(inventoryView.SlotUIs, "SlotUIs should be initialized");
            Assert.AreEqual(9, inventoryView.SlotUIs.Count, "There should be 9 SlotUIs");
            for (int i = 0; i < 9; i++)
            {
                Assert.IsNotNull(inventoryView.SlotUIs[i], $"SlotUI {i} should not be null");
                Assert.IsNotNull(inventoryView.SlotUIs[i].BorderImage, $"SlotUI {i} BorderImage should exist");
                Assert.AreEqual(5, inventoryView.SlotUIs[i].StarImages.Count, $"SlotUI {i} should have 5 star images");
            }
        }

        [Test]
        public void InventorySlot_PenguinVisuals_MatchesBenchDesign_WithoutCharms()
        {
            inventoryView.RebindSlotReferences();

            var slotUI = inventoryView.SlotUIs[0];
            Assert.IsNotNull(slotUI);

            var pObj = new GameObject("TestPenguin_Tier3", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinController));
            var visualRoot = new GameObject("visualRoot");
            visualRoot.transform.SetParent(pObj.transform, false);
            var modelMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelMesh.name = "DummyModel";
            modelMesh.transform.SetParent(visualRoot.transform, false);

            var ctrl = pObj.GetComponent<PenguinController>();
            ctrl.SetTier(3);

            // Act: Update visuals with Tier 3 penguin (no charms)
            slotUI.UpdateVisuals(tier: 3, activeCharms: null, isLocked: false, isDisabled: false, ctrl);

            // Assert
            // 1. Tier 3 border color (Sky blue)
            Assert.AreEqual(slotUI.Level3Color, slotUI.BorderImage.color, "Border color should match Tier 3 color");

            // 2. 3 stars active
            Assert.IsTrue(slotUI.StarImages[0].gameObject.activeSelf, "Star 0 should be active");
            Assert.IsTrue(slotUI.StarImages[1].gameObject.activeSelf, "Star 1 should be active");
            Assert.IsTrue(slotUI.StarImages[2].gameObject.activeSelf, "Star 2 should be active");
            Assert.IsFalse(slotUI.StarImages[3].gameObject.activeSelf, "Star 3 should be inactive");
            Assert.IsFalse(slotUI.StarImages[4].gameObject.activeSelf, "Star 4 should be inactive");

            // 3. Charm effect is NOT applied (all charm circles inactive)
            for (int c = 0; c < 6; c++)
            {
                Assert.IsFalse(slotUI.CharmCircleSlots[c].activeSelf, $"Charm circle {c} should be inactive for inventory penguin");
            }

            // 4. 3D model active
            Assert.IsTrue(slotUI.Preview3D.HasActive3DModel, "3D model preview should be active for penguin");

            // Cleanup
            Object.DestroyImmediate(pObj);
        }

        [Test]
        public void InventorySlot_EmptyOrItem_HidesStarsAndCharms_AndClears3DModel()
        {
            inventoryView.RebindSlotReferences();

            var slotUI = inventoryView.SlotUIs[1];
            Assert.IsNotNull(slotUI);

            // Act: empty slot / item slot (tier: 0)
            slotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: false, null);

            // Assert
            Assert.AreEqual(slotUI.DefaultBorderColor, slotUI.BorderImage.color, "Empty/Item slot should have default border color");
            for (int s = 0; s < 5; s++)
            {
                Assert.IsFalse(slotUI.StarImages[s].gameObject.activeSelf, $"Star {s} should be inactive");
            }
            for (int c = 0; c < 6; c++)
            {
                Assert.IsFalse(slotUI.CharmCircleSlots[c].activeSelf, $"Charm circle {c} should be inactive");
            }
            Assert.IsFalse(slotUI.Preview3D.HasActive3DModel, "3D model should be inactive for empty/item slot");
        }
    }
}
