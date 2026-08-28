using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Shop/Equipment")]
public class EquipmentItem : ShopItemBase
{
    public PenguinPinball.Core.EquipmentSO EquipmentData;
    public EquipmentItem() { Type = ShopItemType.Equipment; }
}
