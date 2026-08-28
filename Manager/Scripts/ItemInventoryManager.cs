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
        if (OwnedCharms.Count >= maxCharmCount) return false;
        
        OwnedCharms.Add(charm);
        charm.ApplyEffects(gameObject);
        
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveCharm(ShopItemBase charm)
    {
        if (charm != null && OwnedCharms.Contains(charm))
        {
            charm.RemoveEffects(gameObject);
            OwnedCharms.Remove(charm);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[ItemInventoryManager] 부적 제거: {charm.ItemName}");
        }
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