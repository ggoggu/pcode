using System;
using System.Collections.Generic;
using PenguinPinball.Core;

namespace PenguinPinball.UI.ViewModels
{
    public class PenguinDetailViewModel : IDisposable
    {
        private PenguinController currentPenguin;
        
        public string PenguinName { get; private set; }
        public UnityEngine.Sprite PenguinIcon { get; private set; }
        public List<EquipmentSO> EquippedItems { get; private set; } = new List<EquipmentSO>();

        public event Action OnDetailUpdated;

        public void Initialize()
        {
            GameEvents.EquipmentChanged += HandleEquipmentChanged;
        }

        public void Dispose()
        {
            GameEvents.EquipmentChanged -= HandleEquipmentChanged;
        }

        public void SetTargetPenguin(PenguinController penguin)
        {
            currentPenguin = penguin;
            RefreshData();
        }

        private void HandleEquipmentChanged(PenguinController penguin)
        {
            if (currentPenguin == penguin)
            {
                RefreshData();
            }
        }

        private void RefreshData()
        {
            if (currentPenguin == null)
            {
                PenguinName = string.Empty;
                PenguinIcon = null;
                EquippedItems.Clear();
            }
            else
            {
                var def = currentPenguin.Definition;
                PenguinName = def != null ? def.DisplayName : "Unknown";
                PenguinIcon = def != null ? def.Icon : null;
                
                EquippedItems.Clear();
                if (currentPenguin.EquipmentHandler != null)
                {
                    EquippedItems.AddRange(currentPenguin.EquipmentHandler.EquippedList);
                }
            }
            
            OnDetailUpdated?.Invoke();
        }
    }
}
