using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PenguinPinball.UI.ViewModels;
using PenguinPinball.Core;

namespace PenguinPinball.UI.Views
{
    public class InventoryView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private List<Button> slotButtons;
        [SerializeField] private List<Image> slotIconImages;
        [SerializeField] private List<TextMeshProUGUI> slotNameTexts;
        [SerializeField] private List<Penguin3DPreview> slotPreviews;

        [Header("Slot Visual Controllers")]
        [SerializeField] private List<BenchSlotUI> slotUIs;

        public IReadOnlyList<BenchSlotUI> SlotUIs => slotUIs;
        
        [Header("State Settings")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        private InventoryViewModel viewModel;
        private void Awake()
        {
            viewModel = new InventoryViewModel();
            RebindSlotReferences();
            
            // 버튼 클릭 이벤트 미리 연결
            if (slotButtons != null)
            {
                for (int i = 0; i < slotButtons.Count; i++)
                {
                    int index = i;
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].onClick.AddListener(() => viewModel.SelectSlot(index));
                    }
                }
            }
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 슬롯 버튼, 텍스트, 아이콘 컴포넌트를 계층 구조에서 1:1로 정확하게 자동 재바인딩하고 초기화합니다.
        /// 인스펙터 직렬화 누락이나 인덱스 어긋남 문제를 원천 방지합니다.
        /// </summary>
        public void RebindSlotReferences()
        {
            Transform container = transform.Find("SlotContainer");
            if (container != null && container.childCount > 0)
            {
                var foundButtons = new List<Button>();
                for (int i = 0; i < container.childCount; i++)
                {
                    var btn = container.GetChild(i).GetComponent<Button>();
                    if (btn != null)
                    {
                        foundButtons.Add(btn);
                    }
                }
                if (foundButtons.Count > 0)
                {
                    slotButtons = foundButtons;
                }
            }

            if (slotButtons != null)
            {
                slotNameTexts = new List<TextMeshProUGUI>(slotButtons.Count);
                slotIconImages = new List<Image>(slotButtons.Count);

                for (int i = 0; i < slotButtons.Count; i++)
                {
                    var btn = slotButtons[i];
                    if (btn != null)
                    {
                        // 1. 해당 슬롯 버튼 하위의 TextMeshProUGUI 탐색
                        var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (txt != null)
                        {
                            txt.raycastTarget = false;
                            txt.text = string.Empty;
                        }
                        slotNameTexts.Add(txt);

                        // 2. 해당 슬롯 버튼 하위의 Icon Image 탐색
                        Transform iconTf = btn.transform.Find("Icon");
                        Image iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
                        if (iconImg == null)
                        {
                            var images = btn.GetComponentsInChildren<Image>(true);
                            foreach (var img in images)
                            {
                                if (img != null && img != btn.image)
                                {
                                    iconImg = img;
                                    break;
                                }
                            }
                        }

                        if (iconImg != null)
                        {
                            iconImg.raycastTarget = false;
                            iconImg.sprite = null;
                            iconImg.enabled = false;
                        }
                        slotIconImages.Add(iconImg);
                    }
                    else
                    {
                        slotNameTexts.Add(null);
                        slotIconImages.Add(null);
                    }
                }

                slotUIs = new List<BenchSlotUI>(slotButtons.Count);
                slotPreviews = new List<Penguin3DPreview>(slotButtons.Count);

                for (int i = 0; i < slotButtons.Count; i++)
                {
                    var btn = slotButtons[i];
                    if (btn != null)
                    {
                        var slotUI = btn.GetComponent<BenchSlotUI>();
                        if (slotUI == null)
                        {
                            slotUI = btn.gameObject.AddComponent<BenchSlotUI>();
                        }

                        slotUI.SetSlotIndex(i, 2750f, "InventoryPreviewRig");
                        slotUI.EnsureHierarchy();
                        slotUIs.Add(slotUI);
                        slotPreviews.Add(slotUI.Preview3D);
                    }
                    else
                    {
                        slotUIs.Add(null);
                        slotPreviews.Add(null);
                    }
                }
            }

            // 혹시 슬롯 계층 내부에 남아있는 모든 TextMeshProUGUI를 안전하게 공백 초기화
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                if (t != null && t.transform != transform)
                {
                    t.raycastTarget = false;
                    t.text = string.Empty;
                }
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            RebindSlotReferences();
        }

        [ContextMenu("Rebind Slot References")]
        public void RebindSlotsFromContext()
        {
            RebindSlotReferences();
        }
#endif

        private void Start()
        {
            viewModel.Initialize();
        }

        private void OnEnable()
        {
            viewModel.OnInventoryUpdated += RefreshUI;
            viewModel.OnSlotSelected += HandleSlotSelected;
            GameManager.OnStateChanged += HandleStateChanged;
            PlacementManager.OnPlacementStarted += HandlePlacementVisibilityChanged;
            PlacementManager.OnPlacementEnded += HandlePlacementVisibilityChanged;
            UpdateVisibility();
        }

        private void OnDisable()
        {
            viewModel.OnInventoryUpdated -= RefreshUI;
            viewModel.OnSlotSelected -= HandleSlotSelected;
            GameManager.OnStateChanged -= HandleStateChanged;
            PlacementManager.OnPlacementStarted -= HandlePlacementVisibilityChanged;
            PlacementManager.OnPlacementEnded -= HandlePlacementVisibilityChanged;

            if (slotUIs != null)
            {
                for (int i = 0; i < slotUIs.Count; i++)
                {
                    if (slotUIs[i] != null && slotUIs[i].Preview3D != null)
                    {
                        slotUIs[i].Preview3D.SetPreviewActive(false);
                    }
                }
            }
        }

        private void HandlePlacementVisibilityChanged()
        {
            UpdateVisibility();
        }

        private void HandleSlotSelected(InventorySlotData slotData)
        {
            if (InstantItemSelectionController.Instance != null && InstantItemSelectionController.Instance.IsActive)
            {
                if (slotData != null && slotData.SlotType == InventorySlotType.Penguin)
                {
                    var penguinObj = slotData.Reference as GameObject;
                    if (penguinObj != null)
                    {
                        int index = viewModel.Slots.IndexOf(slotData);
                        RectTransform slotRect = (index >= 0 && index < slotButtons.Count && slotButtons[index] != null)
                            ? slotButtons[index].GetComponent<RectTransform>()
                            : null;

                        var controller = penguinObj.GetComponent<PenguinController>();
                        if (controller != null)
                        {
                            InstantItemSelectionController.Instance.SelectPenguin(controller, slotRect);
                        }
                    }
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (canvasGroup != null)
            {
                bool isMaintenance = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MaintenanceTime);
                bool isPlacing = (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing);
                bool isVisible = isMaintenance && !isPlacing;
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;

                if (slotUIs != null)
                {
                    for (int i = 0; i < slotUIs.Count; i++)
                    {
                        if (slotUIs[i] != null && slotUIs[i].Preview3D != null)
                        {
                            slotUIs[i].Preview3D.SetPreviewActive(isVisible);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            viewModel.Dispose();
        }

        private void RefreshUI()
        {
            if (slotButtons == null) return;
            
            for (int i = 0; i < slotButtons.Count; i++)
            {
                bool hasData = i < viewModel.Slots.Count && !viewModel.Slots[i].IsEmpty;
                var slotUI = (slotUIs != null && slotUIs.Count > i) ? slotUIs[i] : null;
                
                if (hasData)
                {
                    var slotData = viewModel.Slots[i];
                    bool isPenguin = slotData.SlotType == InventorySlotType.Penguin;
                    bool has3D = false;

                    if (isPenguin)
                    {
                        PenguinController controller = null;
                        PenguinDefinition definition = null;

                        if (slotData.Reference is GameObject pObj)
                        {
                            controller = pObj.GetComponent<PenguinController>();
                            if (controller != null) definition = controller.Definition;
                        }
                        else if (slotData.Reference is PenguinDefinition pDef)
                        {
                            definition = pDef;
                        }

                        int tier = controller != null ? controller.Tier : slotData.Tier;

                        if (slotUI != null)
                        {
                            slotUI.UpdateVisuals(tier, activeCharms: null, isLocked: false, isDisabled: slotData.IsDisabled, controller, definition);
                            has3D = slotUI.Preview3D != null && slotUI.Preview3D.HasActive3DModel;
                        }
                    }
                    else
                    {
                        if (slotUI != null)
                        {
                            slotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: slotData.IsDisabled, null, null);
                        }
                    }

                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                    {
                        slotNameTexts[i].text = isPenguin ? string.Empty : slotData.Name;
                        slotNameTexts[i].raycastTarget = false;
                    }
                    
                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                    {
                        slotIconImages[i].raycastTarget = false;
                        if (has3D)
                        {
                            slotIconImages[i].enabled = false;
                        }
                        else if (slotData.Icon != null)
                        {
                            slotIconImages[i].sprite = slotData.Icon;
                            slotIconImages[i].enabled = true;
                        }
                        else
                        {
                            slotIconImages[i].sprite = null;
                            slotIconImages[i].enabled = false;
                        }
                    }
                    
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(true);
                        
                        // 드래그 앤 드랍 데이터 연동
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler == null)
                        {
                            dragHandler = slotButtons[i].gameObject.AddComponent<InventoryItemDragHandler>();
                        }
                        dragHandler.sourceLocation = PenguinLocation.Inventory;
                        dragHandler.sourceIndex = i;
                        dragHandler.itemReference = slotData.Reference;

                        // 드롭 핸들러 연동
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.enabled = true;
                        dropHandler.slotLocation = PenguinLocation.Inventory;
                        dropHandler.slotIndex = i;
                        dropHandler.TargetPenguinObj = slotData.Reference as GameObject;
                        dropHandler.targetItemReference = slotData.Reference;
                    }
                }
                else
                {
                    if (slotUI != null)
                    {
                        slotUI.UpdateVisuals(tier: 0, activeCharms: null, isLocked: false, isDisabled: false, null, null);
                    }

                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                    {
                        slotNameTexts[i].text = string.Empty;
                        slotNameTexts[i].raycastTarget = false;
                    }
                        
                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                    {
                        slotIconImages[i].sprite = null;
                        slotIconImages[i].enabled = false;
                        slotIconImages[i].raycastTarget = false;
                    }
                        
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(true); // 데이터가 없어도 슬롯은 켜두어 드롭을 받을 수 있게 함
                        
                        // 드래그 핸들러 초기화
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler != null)
                        {
                            dragHandler.sourceLocation = PenguinLocation.Inventory;
                            dragHandler.sourceIndex = i;
                            dragHandler.itemReference = null;
                        }
                        
                        // 드롭 핸들러 연동 (빈 슬롯)
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.enabled = true;
                        dropHandler.slotLocation = PenguinLocation.Inventory;
                        dropHandler.slotIndex = i;
                        dropHandler.TargetPenguinObj = null;
                        dropHandler.targetItemReference = null;
                    }
                }
            }
        }
    }
}
