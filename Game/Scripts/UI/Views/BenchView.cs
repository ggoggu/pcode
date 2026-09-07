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

        [Header("Slot Colors")]
        [SerializeField] private Color normalSlotColor = Color.white;
        [SerializeField] private Color disabledSlotColor = new Color(1f, 0.35f, 0.35f, 1f);

        private BenchViewModel viewModel;
        private void Awake()
        {
            viewModel = new BenchViewModel();
            
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

        private void Start()
        {
            viewModel.Initialize();
        }

        private void OnEnable()
        {
            viewModel.OnBenchUpdated += RefreshUI;
            viewModel.OnPenguinSelected += HandlePenguinSelected;
        }

        private void OnDisable()
        {
            viewModel.OnBenchUpdated -= RefreshUI;
            viewModel.OnPenguinSelected -= HandlePenguinSelected;
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
                bool hasData = i < viewModel.Slots.Count;
                
                if (hasData)
                {
                    var slotData = viewModel.Slots[i];
                    bool isSlotDisabled = slotData.IsDisabled;
                    
                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                        slotNameTexts[i].text = slotData.Name;
                    
                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                    {
                        if (slotData.Icon != null)
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
                        dragHandler.itemReference = isSlotDisabled ? null : slotData.Reference;

                        // 드롭 핸들러 연동
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.slotLocation = PenguinLocation.Bench;
                        dropHandler.TargetPenguinObj = slotData.Reference as GameObject;
                    }
                }
                else
                {
                    if (slotNameTexts != null && slotNameTexts.Count > i && slotNameTexts[i] != null)
                        slotNameTexts[i].text = "";
                        
                    if (slotIconImages != null && slotIconImages.Count > i && slotIconImages[i] != null)
                        slotIconImages[i].enabled = false;
                        
                    if (slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(true); // 데이터가 없어도 슬롯은 켜두어 드롭을 받을 수 있게 함
                        slotButtons[i].interactable = true;

                        var buttonImage = slotButtons[i].image;
                        if (buttonImage != null)
                        {
                            buttonImage.color = normalSlotColor;
                        }

                        var colors = slotButtons[i].colors;
                        colors.normalColor = normalSlotColor;
                        slotButtons[i].colors = colors;
                        
                        // 드래그 핸들러 초기화
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler != null) dragHandler.itemReference = null;
                        
                        // 드롭 핸들러 연동 (빈 슬롯)
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.slotLocation = PenguinLocation.Bench;
                        dropHandler.TargetPenguinObj = null;
                    }
                }
            }
        }

        private void HandlePenguinSelected(GameObject penguinObj)
        {
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
