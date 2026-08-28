using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PenguinPinball.UI.ViewModels;
using PenguinPinball.Core;

namespace PenguinPinball.UI.Views
{
    public class PenguinDetailView : MonoBehaviour
    {
        [Header("Penguin Profile UI")]
        [SerializeField] private TextMeshProUGUI penguinNameText;
        [SerializeField] private Image penguinIconImage;

        [Header("Equipment Slots UI")]
        [SerializeField] private GameObject equipmentSlotPrefab;
        [SerializeField] private Transform equipmentSlotsContainer;

        private PenguinDetailViewModel viewModel;
        private List<GameObject> activeSlots = new List<GameObject>();

        private void Awake()
        {
            viewModel = new PenguinDetailViewModel();
            // 뷰는 기본적으로 숨겨진 상태에서 시작
            gameObject.SetActive(false);
        }

        private void Start()
        {
            viewModel.Initialize();
        }

        private void OnEnable()
        {
            viewModel.OnDetailUpdated += RefreshUI;
        }

        private void OnDisable()
        {
            viewModel.OnDetailUpdated -= RefreshUI;
        }

        private void OnDestroy()
        {
            viewModel.Dispose();
        }

        public void ShowPenguin(PenguinController penguin)
        {
            gameObject.SetActive(true);
            viewModel.SetTargetPenguin(penguin);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            viewModel.SetTargetPenguin(null);
        }

        private void RefreshUI()
        {
            if (penguinNameText != null) 
                penguinNameText.text = string.IsNullOrEmpty(viewModel.PenguinName) ? "None" : viewModel.PenguinName;

            if (penguinIconImage != null)
            {
                if (viewModel.PenguinIcon != null)
                {
                    penguinIconImage.sprite = viewModel.PenguinIcon;
                    penguinIconImage.enabled = true;
                }
                else
                {
                    penguinIconImage.enabled = false;
                }
            }

            // Clear existing slots
            foreach (var slot in activeSlots)
            {
                Destroy(slot);
            }
            activeSlots.Clear();

            // Create new equipment slots
            foreach (var equip in viewModel.EquippedItems)
            {
                var slotObj = Instantiate(equipmentSlotPrefab, equipmentSlotsContainer);
                activeSlots.Add(slotObj);
                
                var nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                var iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();

                if (nameText != null) nameText.text = equip.EquipmentName;
                if (iconImage != null && equip.Icon != null)
                {
                    iconImage.sprite = equip.Icon;
                    iconImage.enabled = true;
                }
            }
        }
    }
}
