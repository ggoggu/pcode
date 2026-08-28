using UnityEngine;
using UnityEngine.EventSystems;

public class ShopItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int slotIndex; // 인스펙터에서 상점 슬롯 인덱스 할당
    
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 장비 아이템인지 검사 (장비만 드래그 가능하게 할 수도 있음)
        if (ShopManager.Instance.CurrentShopItems.Count > slotIndex)
        {
            var item = ShopManager.Instance.CurrentShopItems[slotIndex];
            if (item == null || item.Type != ShopItemType.Equipment)
            {
                eventData.pointerDrag = null; // 드래그 취소
                return;
            }
        }

        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // 드랍 이벤트를 받기 위해 자신의 레이캐스트 차단 해제
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 드랍에 성공하지 않았다면 제자리로
        if (eventData.pointerEnter == null || eventData.pointerEnter.GetComponent<InventorySlotDropHandler>() == null)
        {
            rectTransform.position = originalPosition;
        }
        else
        {
            // 드랍 타겟에서 성공적으로 처리되었다면 제자리로 (UI 상) 돌아감 (혹은 파괴/비활성화)
            rectTransform.position = originalPosition;
        }
    }
}
