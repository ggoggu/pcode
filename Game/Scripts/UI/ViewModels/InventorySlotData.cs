using System;
using UnityEngine;

namespace PenguinPinball.UI.ViewModels
{
    public enum InventorySlotType
    {
        None,
        Penguin,
        Charm,
        Equipment
    }

    public class InventorySlotData
    {
        public InventorySlotType SlotType { get; set; }
        public string Name { get; set; }
        public Sprite Icon { get; set; }
        public object Reference { get; set; }
        public bool IsEmpty => SlotType == InventorySlotType.None;
    }
}
