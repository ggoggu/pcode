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
            
            // Add Charms
            if (ItemInventoryManager.Instance != null)
            {
                foreach (var charm in ItemInventoryManager.Instance.OwnedCharms)
                {
                    if (charm == null) continue;
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Charm,
                        Name = charm.ItemName, // Changed from CharmName
                        Icon = charm.Icon,
                        Reference = charm
                    });
                }
            }
            
            // Add Equipments
            if (ItemInventoryManager.Instance != null)
            {
                foreach (var equip in ItemInventoryManager.Instance.UnusedEquipments)
                {
                    if (equip == null) continue;
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Equipment,
                        Name = equip.EquipmentName,
                        Icon = equip.Icon,
                        Reference = equip
                    });
                }
            }
            
            // Add Inventory Penguins
            if (PenguinDeckManager.Instance != null)
            {
                foreach (var penguinObj in PenguinDeckManager.Instance.OwnedPenguins)
                {
                    if (penguinObj == null) continue;
                    var locData = penguinObj.GetComponent<PenguinLocationData>();
                    if (locData == null || locData.Location != PenguinLocation.Inventory) continue;
                    
                    var controller = penguinObj.GetComponent<PenguinController>();
                    var def = controller != null ? controller.Definition : null;
                    
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.Penguin,
                        Name = def != null ? def.DisplayName : "Unknown",
                        Icon = def != null ? def.Icon : null,
                        Reference = penguinObj
                    });
                }
            }
            
            OnInventoryUpdated?.Invoke();
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < Slots.Count)
            {
                OnSlotSelected?.Invoke(Slots[index]);
            }
        }
    }
}
