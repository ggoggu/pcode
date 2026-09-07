using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Game.ObjectSystem.UI
{
    public class ObjectStorageSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Component References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;

        public ObjectItem ItemData { get; private set; }

        public void Init(ObjectItem item, Sprite icon, string displayName)
        {
            ItemData = item;
            if (_iconImage != null) _iconImage.sprite = icon;
            if (_nameText != null) _nameText.text = displayName;
        }

        /// <summary>
        /// 인벤토리 슬롯 클릭 시 PlacementManager를 호출하여 배치 모드 진입
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (ItemData == null || PlacementManager.Instance == null) return;

            // 1. ObjectType -> PlacementType 변환
            PlacementType targetType = PlacementType.None;
            if (ItemData.Type == ObjectType.Bumper) targetType = PlacementType.Bumper;
            else if (ItemData.Type == ObjectType.Slingshot) targetType = PlacementType.Slingshot;

            // 2. 배치 시스템에 아이템 데이터와 함께 배치 시작 요청
            PlacementManager.Instance.SelectPlacementType(targetType, ItemData);

            // 3. 필드가 보이도록 인벤토리 창 닫기
            ObjectStorageUI storageUI = GetComponentInParent<ObjectStorageUI>();
            if (storageUI != null)
            {
                storageUI.ClosePanel();
            }
        }
    }
}