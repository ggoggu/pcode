using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public class PenguinEquipmentHandler : MonoBehaviour
    {
        private readonly List<EquipmentSO> equippedList = new();
        private PenguinController penguin;

        public IReadOnlyList<EquipmentSO> EquippedList => equippedList;
        public int MaxCapacity => GetMaxCapacity(penguin != null ? penguin.Tier : 1);
        public bool CanEquip => equippedList.Count < MaxCapacity;

      
        public void Initialize(PenguinController controller)
        {
            penguin = controller;
        }
        public int GetMaxCapacity(int tier)
        {
            if (tier <= 2) return 1;
            return 1 << (tier - 2);
        }

        /// <summary>
        /// 장비를 장착합니다.
        /// </summary>
        public bool Equip(EquipmentSO equipment)
        {
            if (equipment == null) return false;

            if (!CanEquip)
            {
                Debug.LogWarning($"[{penguin.name}] 장비 슬롯이 가득 차서 상점 구매가 불가능합니다. ({equippedList.Count}/{MaxCapacity})", this);
                return false;
            }

            return AddEquipmentInternal(equipment);
        }

        public void TransferEquipmentsTo(PenguinEquipmentHandler targetHandler)
        {
            if (targetHandler == null || equippedList.Count == 0 || targetHandler == this) return;

            // 현재 장비들을 타겟 펭귄에게 모두 강제 이관
            targetHandler.AddEquipmentsFromTransfer(equippedList);

            ClearAllEquipments(notifyStats: false);
        }

        public bool AddFromTransfer(EquipmentSO equipment)
        {
            if (equipment == null) return false;

            return AddEquipmentInternal(equipment, notifyStats: true);
        }

        public void ClearAllEquipments(bool notifyStats = true)
        {
            if (equippedList.Count == 0) return;

            foreach (var equipment in equippedList)
            {
                foreach (var stat in penguin.Stats.Values)
                {
                    stat.RemoveAllModifiersFromSource(equipment);
                }
            }

            equippedList.Clear();
            if (notifyStats)
            {
                penguin.OnStatsChanged();
                GameEvents.RaiseEquipmentChanged(penguin);
            }
        }

        private bool AddEquipmentInternal(EquipmentSO equipment, bool notifyStats = true)
        {
            equippedList.Add(equipment);

            foreach (var entry in equipment.StatModifiers)
            {
                if (penguin.Stats.TryGetValue(entry.statType, out var stat))
                {
                    stat.AddModifier(new StatModifier(entry.value, entry.modType, equipment));
                }
            }

            if (notifyStats)
            {
                penguin.OnStatsChanged();
                GameEvents.RaiseEquipmentChanged(penguin);
            }
            return true;
        }
        public void AddEquipmentsFromTransfer(IReadOnlyList<EquipmentSO> equipments)
        {
            if (equipments == null || equipments.Count == 0) return;

            for (int i = 0; i < equipments.Count; i++)
            {
                if (equipments[i] == null) continue;
                AddEquipmentInternal(equipments[i], notifyStats: false);
            }
        }
    }
}