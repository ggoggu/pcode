using System;
using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

namespace PenguinPinball.UI.ViewModels
{
    public class BenchViewModel : IDisposable
    {
        public List<InventorySlotData> Slots { get; private set; } = new List<InventorySlotData>();
        
        public event Action OnBenchUpdated;
        public event Action<GameObject> OnPenguinSelected;

        public void Initialize()
        {
            if (PenguinDeckManager.Instance != null)
            {
                PenguinDeckManager.Instance.OnDeckChanged += RefreshData;
            }
            if (ItemInventoryManager.Instance != null)
            {
                ItemInventoryManager.Instance.OnInventoryChanged += RefreshData;
            }
            RefreshData();
        }

        public void Dispose()
        {
            if (PenguinDeckManager.Instance != null)
            {
                PenguinDeckManager.Instance.OnDeckChanged -= RefreshData;
            }
            if (ItemInventoryManager.Instance != null)
            {
                ItemInventoryManager.Instance.OnInventoryChanged -= RefreshData;
            }
        }

        public void RefreshData()
        {
            Slots.Clear();
            
            int capacity = PenguinDeckManager.Instance != null ? PenguinDeckManager.Instance.MaxBenchCapacity : 6;
            for (int i = 0; i < capacity; i++)
            {
                GameObject penguinObj = PenguinDeckManager.Instance != null 
                    ? PenguinDeckManager.Instance.GetPenguinAtBenchSlot(i) 
                    : null;
                bool isSlotLocked = PenguinDeckManager.Instance != null && PenguinDeckManager.Instance.IsBenchSlotLocked(i);
                var activeCharms = Util.BenchSlotCharmUtility.GetActiveCharmsForBenchSlot(i);

                if (penguinObj != null)
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
                        Tier = tier,
                        ActiveCharms = activeCharms,
                        IsLocked = isSlotLocked
                    });
                }
                else
                {
                    Slots.Add(new InventorySlotData
                    {
                        SlotType = InventorySlotType.None,
                        Name = string.Empty,
                        Icon = null,
                        Reference = null,
                        IsAvailable = true,
                        IsInField = false,
                        Tier = 0,
                        ActiveCharms = activeCharms,
                        IsLocked = isSlotLocked
                    });
                }
            }
            
            OnBenchUpdated?.Invoke();
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < Slots.Count)
            {
                var slot = Slots[index];
                if (slot.IsEmpty || slot.IsDisabled) return;

                if (slot.SlotType == InventorySlotType.Penguin)
                {
                    var penguinObj = slot.Reference as GameObject;
                    if (penguinObj != null)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.WaveInProgress)
                        {
                            PenguinDeckManager.Instance.TrySelectPenguin(penguinObj);
                        }
                        else
                        {
                            OnPenguinSelected?.Invoke(penguinObj);
                        }
                    }
                }
            }
        }
    }
}
