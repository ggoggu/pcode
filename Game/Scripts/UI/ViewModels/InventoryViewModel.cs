using System;
using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

namespace PenguinPinball.UI.ViewModels
{
    public class InventoryViewModel : IDisposable
    {
        public List<InventorySlotData> Slots { get; private set; } = new List<InventorySlotData>();
        
        public event Action OnInventoryUpdated;
        public event Action<InventorySlotData> OnSlotSelected;

        public void Initialize()
        {
            if (ItemInventoryManager.Instance != null)
                ItemInventoryManager.Instance.OnInventoryChanged += RefreshData;

            if (PenguinDeckManager.Instance != null)
                PenguinDeckManager.Instance.OnDeckChanged += RefreshData;

            RefreshData();
        }

        public void Dispose()
        {
            if (ItemInventoryManager.Instance != null)
                ItemInventoryManager.Instance.OnInventoryChanged -= RefreshData;
                
            if (PenguinDeckManager.Instance != null)
                PenguinDeckManager.Instance.OnDeckChanged -= RefreshData;
        }

        private void RefreshData()
        {
            Slots.Clear();
            
            int capacity = ItemInventoryManager.MaxSlotCount;
            for (int i = 0; i < capacity; i++)
            {
                object item = ItemInventoryManager.Instance != null 
                    ? ItemInventoryManager.Instance.GetItemAtSlot(i) 
                    : null;

                if (item is ShopItemBase charm)
                {
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Charm,
                        Name = charm.ItemName,
                        Icon = charm.Icon,
                        Reference = charm
                    });
                }
                else if (item is EquipmentSO equip)
                {
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Equipment,
                        Name = equip.EquipmentName,
                        Icon = equip.Icon,
                        Reference = equip
                    });
                }
                else if (item is GameObject penguinObj)
                {
                    var controller = penguinObj.GetComponent<PenguinController>();
                    var def = controller != null ? controller.Definition : null;
                    int tier = controller != null ? controller.Tier : 1;
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Penguin,
                        Name = def != null ? def.DisplayName : "Unknown",
                        Icon = def != null ? def.Icon : null,
                        Reference = penguinObj,
                        IsAvailable = controller != null ? controller.IsAvailable : true,
                        IsInField = controller != null ? controller.IsInField : false,
                        Tier = tier
                    });
                }
                else if (item is EngravingItem eng)
                {
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Charm,
                        Name = eng.ItemName,
                        Icon = eng.Icon,
                        Reference = eng
                    });
                }
                else
                {
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.None,
                        Name = string.Empty,
                        Icon = null,
                        Reference = null
                    });
                }
            }
            
            OnInventoryUpdated?.Invoke();
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < Slots.Count)
            {
                var slot = Slots[index];
                if (slot != null && !slot.IsEmpty)
                {
                    OnSlotSelected?.Invoke(slot);
                }
            }
        }
    }
}
