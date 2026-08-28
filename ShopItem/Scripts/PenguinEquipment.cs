using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public class PenguinEquipment : MonoBehaviour
    {
        public List<EquipmentItem> EquippedItems { get; private set; } = new List<EquipmentItem>();

        public int GetMaxEquipmentLimit(int tier)
        {
            if (tier <= 2) return 1;
            return Mathf.Max(1, (int)Mathf.Pow(2, tier - 2));
        }

        // 상점에서 드래그 앤 드랍으로 직접 장착할 때 사용
        public bool TryEquipFromShop(EquipmentItem item)
        {
            // 상점에서 구매하여 장착할 때는 기존 장비가 없어야만 가능 (기본 1개 제한)
            if (EquippedItems.Count > 0) return false;

            EquippedItems.Add(item);
            
            // 전역/개체 효과 발동 (origin으로 자기 자신 전달)
            foreach (var effect in item.Effects)
                effect.ApplyEffect(gameObject);

            Debug.Log($"[Equipment] {gameObject.name}에 {item.ItemName} 장착됨.");
            return true;
        }

        // 합성 등으로 승계할 때 사용
        public void InheritEquipment(EquipmentItem item, int currentTier)
        {
            if (EquippedItems.Count >= GetMaxEquipmentLimit(currentTier))
            {
                Debug.LogWarning($"[Equipment] {gameObject.name}의 장비 한도를 초과하여 승계할 수 없습니다.");
                return;
            }

            EquippedItems.Add(item);
            foreach (var effect in item.Effects)
                effect.ApplyEffect(gameObject);
        }

        // 펭귄 판매 시 호출 (합성 승계 시에는 TransferEquipmentsTo를 사용하므로 다름)
        public void UnequipAllAndDestroy()
        {
            foreach (var item in EquippedItems)
            {
                foreach (var effect in item.Effects)
                    effect.RemoveEffect(gameObject);
            }
            EquippedItems.Clear();
        }

        public void TransferEquipmentsTo(PenguinEquipment targetEquipment, int targetTier)
        {
            foreach (var item in EquippedItems)
            {
                foreach (var effect in item.Effects)
                    effect.RemoveEffect(gameObject);
                
                targetEquipment.InheritEquipment(item, targetTier);
            }
            EquippedItems.Clear();
        }
    }
}