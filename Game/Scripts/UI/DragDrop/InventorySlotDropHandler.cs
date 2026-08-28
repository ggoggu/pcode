using UnityEngine;
using UnityEngine.EventSystems;
using PenguinPinball.Core;

public class InventorySlotDropHandler : MonoBehaviour, IDropHandler
{
    public PenguinLocation slotLocation; 
    public GameObject TargetPenguinObj; // 빈 슬롯이면 null

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        
        // 1. 상점 아이템(장비)을 드래그해서 올려놓았을 때
        ShopItemDragHandler shopDrag = eventData.pointerDrag.GetComponent<ShopItemDragHandler>();
        if (shopDrag != null && TargetPenguinObj != null)
        {
            var controller = TargetPenguinObj.GetComponent<PenguinController>();
            if (controller != null)
            {
                bool success = ShopManager.Instance.BuyEquipmentWithTarget(shopDrag.slotIndex, controller);
                if(success) Debug.Log($"장착 성공: 슬롯 {shopDrag.slotIndex} -> 펭귄 {TargetPenguinObj.name}");
            }
            return;
        }
        
        // 2. 인벤토리/벤치 아이템을 드래그해서 올려놓았을 때
        InventoryItemDragHandler invenDrag = eventData.pointerDrag.GetComponent<InventoryItemDragHandler>();
        if (invenDrag != null && invenDrag.itemReference != null)
        {
            // 펭귄을 드래그한 경우 -> 위치 이동
            if (invenDrag.itemReference is GameObject draggedPenguinObj)
            {
                bool success = PenguinDeckManager.Instance.ChangePenguinLocation(draggedPenguinObj, slotLocation);
                if (success)
                {
                    Debug.Log($"[{draggedPenguinObj.name}] -> {slotLocation} 이동 완료");
                }
                else
                {
                    Debug.LogWarning($"{slotLocation}이(가) 가득 차서 이동할 수 없습니다.");
                }
            }
            // 장비를 드래그한 경우 -> 장착 시도 (펭귄이 있는 슬롯에만)
            else if (invenDrag.itemReference is EquipmentSO equipData)
            {
                if (TargetPenguinObj != null)
                {
                    var controller = TargetPenguinObj.GetComponent<PenguinController>();
                    if (controller != null)
                    {
                        bool success = ItemInventoryManager.Instance.EquipToPenguin(equipData, controller);
                        if (success)
                        {
                            Debug.Log($"[{equipData.EquipmentName}] 장착 성공!");
                        }
                        else
                        {
                            Debug.LogWarning("장착 실패: 장비 슬롯이 가득 찼습니다.");
                        }
                    }
                }
            }
        }
    }
}
