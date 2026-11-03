using UnityEngine;
using UnityEngine.EventSystems;
using PenguinPinball.Core;

public class InventorySlotDropHandler : MonoBehaviour, IDropHandler
{
    public PenguinLocation slotLocation; 
    public int slotIndex = -1;
    public GameObject TargetPenguinObj; // 빈 슬롯이면 null
    public object targetItemReference; // 현재 슬롯에 들어있는 데이터 참조

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
                if (success) Debug.Log($"장착 성공: 슬롯 {shopDrag.slotIndex} -> 펭귄 {TargetPenguinObj.name}");
            }
            return;
        }
        
        // 2. 인벤토리/벤치 아이템을 드래그해서 올려놓았을 때
        InventoryItemDragHandler invenDrag = eventData.pointerDrag.GetComponent<InventoryItemDragHandler>();
        if (invenDrag != null && invenDrag.itemReference != null)
        {
            // A. 펭귄을 드래그한 경우
            if (invenDrag.itemReference is GameObject draggedPenguinObj)
            {
                if (invenDrag.sourceLocation == PenguinLocation.Bench && slotLocation == PenguinLocation.Bench)
                {
                    // 벤치 -> 벤치 칸 이동 / 교환
                    bool success = PenguinDeckManager.Instance.SwapOrMoveBenchSlot(invenDrag.sourceIndex, slotIndex);
                    if (success) Debug.Log($"[Bench] {invenDrag.sourceIndex}번 -> {slotIndex}번 슬롯 이동 완료");
                }
                else if (invenDrag.sourceLocation == PenguinLocation.Inventory && slotLocation == PenguinLocation.Inventory)
                {
                    // 인벤토리 -> 인벤토리 칸 이동 / 교환
                    bool success = ItemInventoryManager.Instance != null && ItemInventoryManager.Instance.SwapOrMoveInventorySlot(invenDrag.sourceIndex, slotIndex);
                    if (success) Debug.Log($"[Inventory] {invenDrag.sourceIndex}번 -> {slotIndex}번 슬롯 이동 완료");
                }
                else if (invenDrag.sourceLocation == PenguinLocation.Bench && slotLocation == PenguinLocation.Inventory)
                {
                    // 벤치 -> 인벤토리 슬롯 이동
                    bool success = PenguinDeckManager.Instance.ChangePenguinLocation(draggedPenguinObj, PenguinLocation.Inventory, slotIndex);
                    if (success) Debug.Log($"[Bench -> Inventory] {slotIndex}번 슬롯 이동 완료");
                }
                else if (invenDrag.sourceLocation == PenguinLocation.Inventory && slotLocation == PenguinLocation.Bench)
                {
                    // 인벤토리 -> 벤치 슬롯 이동
                    bool success = PenguinDeckManager.Instance.MovePenguinToBenchSlot(draggedPenguinObj, slotIndex);
                    if (success) Debug.Log($"[Inventory -> Bench] {slotIndex}번 슬롯 이동 완료");
                }
            }
            // B. 장비를 드래그한 경우
            else if (invenDrag.itemReference is EquipmentSO equipData)
            {
                // 타겟에 펭귄이 있으면 장착 시도 (벤치 또는 인벤토리 펭귄)
                if (TargetPenguinObj != null)
                {
                    var controller = TargetPenguinObj.GetComponent<PenguinController>();
                    if (controller != null)
                    {
                        bool success = ItemInventoryManager.Instance.EquipToPenguin(equipData, controller);
                        if (success)
                        {
                            Debug.Log($"[{equipData.EquipmentName}] 장착 성공!");
                            return;
                        }
                    }
                }

                // 타겟이 인벤토리 슬롯인 경우 칸 이동 / 교환
                if (slotLocation == PenguinLocation.Inventory && ItemInventoryManager.Instance != null)
                {
                    bool success = ItemInventoryManager.Instance.SwapOrMoveInventorySlot(invenDrag.sourceIndex, slotIndex);
                    if (success) Debug.Log($"[Inventory] 장비 {invenDrag.sourceIndex}번 -> {slotIndex}번 슬롯 이동 완료");
                }
            }
            // C. 부적 등 기타 아이템을 드래그한 경우
            else
            {
                if (slotLocation == PenguinLocation.Inventory && ItemInventoryManager.Instance != null)
                {
                    bool success = ItemInventoryManager.Instance.SwapOrMoveInventorySlot(invenDrag.sourceIndex, slotIndex);
                    if (success) Debug.Log($"[Inventory] 아이템 {invenDrag.sourceIndex}번 -> {slotIndex}번 슬롯 이동 완료");
                }
            }
        }
    }
}
