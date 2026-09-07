using System;
using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

public class ItemInventoryManager : MonoBehaviour
{
    public static ItemInventoryManager Instance { get; private set; }

    // 펭귄 장착용 장비
    public List<EquipmentSO> UnusedEquipments { get; private set; } = new List<EquipmentSO>();
    
    // 범퍼 등 필드 오브젝트 장착용 각인
    public List<EngravingItem> UnusedEngravings { get; private set; } = new List<EngravingItem>();
    
    // 보유 부적
    public List<ShopItemBase> OwnedCharms { get; private set; } = new List<ShopItemBase>();
    [SerializeField] private int maxCharmCount = 9;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddCharm(ShopItemBase charm)
    {
        if (charm == null) return false;

        // 1. 이미 같은 종류의 부적이 인벤토리에 있는지 검사
        ShopItemBase existingCharm = OwnedCharms.Find(c => IsSameCharm(c, charm));
        if (existingCharm != null)
        {
            // 기존 부적 효과 해제 및 인벤토리에서 제거 후 교체
            existingCharm.RemoveEffects(gameObject);
            int existingIndex = OwnedCharms.IndexOf(existingCharm);
            OwnedCharms[existingIndex] = charm;
            Destroy(existingCharm);

            // 새 부적 효과 적용
            charm.ApplyEffects(gameObject);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[ItemInventoryManager] 기존 부적({charm.ItemName}) 교체 완료 (남은 횟수: {charm.CurrentUses}/{charm.MaxUses})");
            return true;
        }

        // 2. 신규 부적인 경우 슬롯 한도 체크
        if (OwnedCharms.Count >= maxCharmCount) return false;
        
        OwnedCharms.Add(charm);
        charm.ApplyEffects(gameObject);
        
        OnInventoryChanged?.Invoke();
        Debug.Log($"[ItemInventoryManager] 새 부적({charm.ItemName}) 추가 완료");
        return true;
    }

    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void RemoveCharm(ShopItemBase charm)
    {
        if (charm != null && OwnedCharms.Contains(charm))
        {
            charm.RemoveEffects(gameObject);
            OwnedCharms.Remove(charm);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[ItemInventoryManager] 부적 제거: {charm.ItemName}");
            Destroy(charm);
        }
    }

    public static bool IsSameCharm(ShopItemBase a, ShopItemBase b)
    {
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.ItemID) && !string.IsNullOrEmpty(b.ItemID))
        {
            return a.ItemID == b.ItemID;
        }
        return a.ItemName == b.ItemName;
    }

    public void UseCharm(ShopItemBase charm)
    {
        if (charm != null && OwnedCharms.Contains(charm))
        {
            charm.UseItem();
        }
    }

    public void AddEquipment(EquipmentSO item)
    {
        UnusedEquipments.Add(item);
        OnInventoryChanged?.Invoke();
    }

    public bool EquipToPenguin(EquipmentSO item, PenguinController target)
    {
        if (!UnusedEquipments.Contains(item)) return false;

        if (target != null && target.EquipmentHandler != null && target.EquipmentHandler.Equip(item))
        {
            UnusedEquipments.Remove(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    // 각인(범퍼 등 오브젝트용) 추가 복구
    public void AddEngraving(EngravingItem item)
    {
        UnusedEngravings.Add(item);
        OnInventoryChanged?.Invoke();
    }

    // 오브젝트에 각인 장착 복구
    public bool EngraveToObject(EngravingItem item, IEngravable target)
    {
        if (!UnusedEngravings.Contains(item)) return false;
        
        if (target != null && target.TryEngrave(item))
        {
            UnusedEngravings.Remove(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }
}