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
            RefreshData();
        }

        public void Dispose()
        {
            if (PenguinDeckManager.Instance != null)
            {
                PenguinDeckManager.Instance.OnDeckChanged -= RefreshData;
            }
        }

        private void RefreshData()
        {
            Slots.Clear();
            
            if (PenguinDeckManager.Instance != null)
            {
                foreach (var penguinObj in PenguinDeckManager.Instance.OwnedPenguins)
                {
                    if (penguinObj == null) continue;
                    var locData = penguinObj.GetComponent<PenguinLocationData>();
                    if (locData == null || locData.Location != PenguinLocation.Bench) continue;
                    
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
            
            OnBenchUpdated?.Invoke();
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < Slots.Count)
            {
                var slot = Slots[index];
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
