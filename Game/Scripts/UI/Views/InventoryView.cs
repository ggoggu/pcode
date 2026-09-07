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
        
        [Header("State Settings")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        private InventoryViewModel viewModel;
        private void Awake()
        {
            viewModel = new InventoryViewModel();
            
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

        private void Start()
        {
            viewModel.Initialize();
        }

        private void OnEnable()
        {
            viewModel.OnInventoryUpdated += RefreshUI;
            GameManager.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance != null) HandleStateChanged(GameManager.Instance.CurrentState);
        }

        private void OnDisable()
        {
            viewModel.OnInventoryUpdated -= RefreshUI;
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (canvasGroup != null)
            {
                bool isVisible = (state == GameState.MaintenanceTime);
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;

                if (isVisible)
                {
                    RefreshUI();
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
                bool hasData = i < viewModel.Slots.Count;
                
                if (hasData)
                {
                    var slotData = viewModel.Slots[i];
                    
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
                        
                        // 드래그 앤 드랍 데이터 연동
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler == null)
                        {
                            dragHandler = slotButtons[i].gameObject.AddComponent<InventoryItemDragHandler>();
                        }
                        dragHandler.itemReference = slotData.Reference;

                        // 드롭 핸들러 연동
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.slotLocation = PenguinLocation.Inventory;
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
                        
                        // 드래그 핸들러 초기화
                        var dragHandler = slotButtons[i].gameObject.GetComponent<InventoryItemDragHandler>();
                        if (dragHandler != null) dragHandler.itemReference = null;
                        
                        // 드롭 핸들러 연동 (빈 슬롯)
                        var dropHandler = slotButtons[i].gameObject.GetComponent<InventorySlotDropHandler>();
                        if (dropHandler == null)
                        {
                            dropHandler = slotButtons[i].gameObject.AddComponent<InventorySlotDropHandler>();
                        }
                        dropHandler.slotLocation = PenguinLocation.Inventory;
                        dropHandler.TargetPenguinObj = null;
                    }
                }
            }
        }
    }
}
