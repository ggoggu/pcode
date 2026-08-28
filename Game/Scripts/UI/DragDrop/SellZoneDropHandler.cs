using UnityEngine;
using UnityEngine.EventSystems;

public class SellZoneDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventoryItemDragHandler dragHandler = eventData.pointerDrag.GetComponent<InventoryItemDragHandler>();
            if (dragHandler != null && dragHandler.itemReference != null)
            {
                ShopManager.Instance.SellItem(dragHandler.itemReference);
                Debug.Log($"아이템 판매 성공: {dragHandler.itemReference}");
                
                // 해당 UI 삭제는 UI 매니저나 이벤트에서 처리
            }
        }
    }
}
