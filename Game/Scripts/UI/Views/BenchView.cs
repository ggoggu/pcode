using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PenguinPinball.UI.ViewModels;
using PenguinPinball.Core;

namespace PenguinPinball.UI.Views
{
    public class BenchView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private List<Button> slotButtons;
        [SerializeField] private List<Image> slotIconImages;
        [SerializeField] private List<TextMeshProUGUI> slotNameTexts;
        
        [Header("Detail View Reference")]
        [SerializeField] private PenguinDetailView detailView;

        [Header("Slot Visual Controllers")]
        [SerializeField] private List<BenchSlotUI> slotUIs;

        [Header("Slot Colors")]
        [SerializeField] private Color normalSlotColor = Color.white;
        [SerializeField] private Color disabledSlotColor = new Color(1f, 0.35f, 0.35f, 1f);

        public IReadOnlyList<BenchSlotUI> SlotUIs => slotUIs;

        private BenchViewModel viewModel;
        private void Awake()
        {
            viewModel = new BenchViewModel();
            InitializeSlotUIs();
            
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
        }

        private void InitializeSlotUIs()
        {
            if (slotButtons == null) return;

            if (slotUIs == null || slotUIs.Count != slotButtons.Count)
            {
                slotUIs = new List<BenchSlotUI>();
                for (int i = 0; i < slotButtons.Count; i++)
                {
                    if (slotButtons[i] != null)
                    {
                        var slotUI = slotButtons[i].GetComponent<BenchSlotUI>();
                        if (slotUI == null)
                        {
                            slotUI = slotButtons[i].gameObject.AddComponent<BenchSlotUI>();
                        }
                        slotUI.SetSlotIndex(i);
                        slotUI.EnsureHierarchy();
                        slotUIs.Add(slotUI);
                    }
                }
            }
            else
            {
                for (int i = 0; i < slotUIs.Count; i++)
                {
                    if (slotUIs[i] != null)
                    {
                        slotUIs[i].SetSlotIndex(i);
                        slotUIs[i].EnsureHierarchy();
                    }
                }
            }

            // 슬롯 텍스트 및 2D 아이콘이 버튼 클릭/드래그 이벤트를 가리지 않도록 raycastTarget 비활성화
            if (slotNameTexts != null)
            {
                for (int i = 0; i < slotNameTexts.Count; i++)
                {
                    if (slotNameTexts[i] != null)
                    {
                        slotNameTexts[i].raycastTarget = false;
                        slotNameTexts[i].text = "";
                    }
                }
            }
            if (slotIconImages != null)
            {
                for (int i = 0; i < slotIconImages.Count; i++)
                {
                    if (slotIconImages[i] != null) slotIconImages[i].raycastTarget = false;
                }
            }
        }

        private void Start()
        {
            viewModel.Initialize();
        }

        private void OnEnable()
        {
            viewModel.OnBenchUpdated += RefreshUI;
            viewModel.OnPenguinSelected += HandlePenguinSelected;
            if (ItemInventoryManager.Instance != null)
            {
                ItemInventoryManager.Instance.OnInventoryChanged += RefreshUI;
            }
        }

        private void OnDisable()
        {
            viewModel.OnBenchUpdated -= RefreshUI;
            viewModel.OnPenguinSelected -= HandlePenguinSelected;
            if (ItemInventoryManager.Instance != null)
            {
                ItemInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            }

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
                
                if (hasData)
                {
                    var slotData = viewModel.Slots[i];
                    bool isSlotDisabled = slotData.IsDisabled;
                    
                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                    {
                        slotNameTexts[i].text = slotData.Name;
                        slotNameTexts[i].raycastTarget = false;
                    }

                    GameObject penguinObj = slotData.Reference as GameObject;
                    PenguinController penguinController = penguinObj != null ? penguinObj.GetComponent<PenguinController>() : null;

                    if (slotUIs != null && slotUIs.Count > i && slotUIs[i] != null)
                    {
                        slotUIs[i].UpdateVisuals(slotData.Tier, slotData.ActiveCharms, isLocked: false, isDisabled: isSlotDisabled, penguinController);
                    }

                    // 3D 모델 프리뷰가 표시되는 경우 2D 아이콘은 숨기고, 그렇지 않은 경우 2D 아이콘 표시
                    bool has3DPreview = (slotUIs != null && slotUIs.Count > i && slotUIs[i] != null && slotUIs[i].Preview3D != null && slotUIs[i].Preview3D.HasActive3DModel);

                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                    {
                        slotIconImages[i].raycastTarget = false;
                        if (has3DPreview)
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
                            slotIconImages[i].enabled = false;
                        }
                    }
                    
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(true);
                        slotButtons[i].interactable = !isSlotDisabled;

                        // 배경 이미지 색상 변경 (발사 중 또는 리스폰 대기 중이면 빨간색)
                        var buttonImage = slotButtons[i].image;
                        if (buttonImage != null)
                        {
                            buttonImage.color = isSlotDisabled ? disabledSlotColor : normalSlotColor;
                        }

                        // Unity UI Button ColorBlock 연동
                        var colors = slotButtons[i].colors;
                        colors.normalColor = isSlotDisabled ? disabledSlotColor : normalSlotColor;
                        colors.disabledColor = disabledSlotColor;
                        slotButtons[i].colors = colors;
                        
                        // 드래그 앤 드랍 데이터 연동 (비활성화 상태일 때는 드래그 방지)
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler == null)
                        {
                            dragHandler = slotButtons[i].gameObject.AddComponent<InventoryItemDragHandler>();
                        }
                        dragHandler.sourceLocation = PenguinLocation.Bench;
                        dragHandler.sourceIndex = i;
                        dragHandler.itemReference = isSlotDisabled ? null : slotData.Reference;

                        // 드롭 핸들러 연동
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.enabled = true;
                        dropHandler.slotLocation = PenguinLocation.Bench;
                        dropHandler.slotIndex = i;
                        dropHandler.TargetPenguinObj = slotData.Reference as GameObject;
                        dropHandler.targetItemReference = slotData.Reference;
                    }
                }
                else
                {
                    bool isSlotLocked = PenguinDeckManager.Instance != null && PenguinDeckManager.Instance.IsBenchSlotLocked(i);
                    var activeCharms = (i < viewModel.Slots.Count && viewModel.Slots[i] != null)
                        ? viewModel.Slots[i].ActiveCharms
                        : Util.BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(i);

                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                    {
                        slotNameTexts[i].text = isSlotLocked ? "잠김" : "";
                        slotNameTexts[i].raycastTarget = false;
                    }
                        
                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                    {
                        slotIconImages[i].enabled = false;
                        slotIconImages[i].raycastTarget = false;
                    }
                        
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(true); // 데이터가 없어도 슬롯은 켜두어 드롭을 받을 수 있게 함
                        slotButtons[i].interactable = !isSlotLocked;

                        var buttonImage = slotButtons[i].image;
                        Color slotColor = isSlotLocked ? disabledSlotColor : normalSlotColor;
                        if (buttonImage != null)
                        {
                            buttonImage.color = slotColor;
                        }

                        var colors = slotButtons[i].colors;
                        colors.normalColor = slotColor;
                        colors.disabledColor = disabledSlotColor;
                        slotButtons[i].colors = colors;
                        
                        // 드래그 핸들러 초기화
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler != null)
                        {
                            dragHandler.sourceLocation = PenguinLocation.Bench;
                            dragHandler.sourceIndex = i;
                            dragHandler.itemReference = null;
                        }
                        
                        // 드롭 핸들러 연동 (잠긴 슬롯은 드롭 차단)
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.enabled = !isSlotLocked;
                        dropHandler.slotLocation = PenguinLocation.Bench;
                        dropHandler.slotIndex = i;
                        dropHandler.TargetPenguinObj = null;
                        dropHandler.targetItemReference = null;
                    }

                    if (slotUIs != null && slotUIs.Count > i && slotUIs[i] != null)
                    {
                        slotUIs[i].UpdateVisuals(tier: 0, activeCharms, isLocked: isSlotLocked, isDisabled: isSlotLocked, penguinController: null);
                    }
                }
            }
        }

        private void HandlePenguinSelected(GameObject penguinObj)
        {
            if (InstantItemSelectionController.Instance != null && InstantItemSelectionController.Instance.IsActive)
            {
                int slotIndex = viewModel.Slots.FindIndex(s => s.Reference == (object)penguinObj);
                RectTransform slotRect = (slotIndex >= 0 && slotIndex < slotButtons.Count && slotButtons[slotIndex] != null)
                    ? slotButtons[slotIndex].GetComponent<RectTransform>()
                    : null;

                var controller = penguinObj.GetComponent<PenguinController>();
                if (controller != null)
                {
                    InstantItemSelectionController.Instance.SelectPenguin(controller, slotRect);
                    return;
                }
            }

            if (detailView != null)
            {
                var controller = penguinObj.GetComponent<PenguinController>();
                if (controller != null)
                {
                    detailView.ShowPenguin(controller);
                }
            }
            else
            {
                Debug.LogWarning("PenguinDetailView is not assigned in BenchView!");
            }
        }
    }
}
