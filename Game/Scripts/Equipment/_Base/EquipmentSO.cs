using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Equipment Definition")]
    public class EquipmentSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string equipmentName = "New Equipment";
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Stat Modifiers")]
        [SerializeField] private List<EquipmentStatEntry> statModifiers = new();

        public string EquipmentName => equipmentName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<EquipmentStatEntry> StatModifiers => statModifiers;
    }
}