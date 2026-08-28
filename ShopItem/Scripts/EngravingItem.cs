using UnityEngine;

[CreateAssetMenu(fileName = "NewEngraving", menuName = "Shop/Engraving")]
public class EngravingItem : ShopItemBase
{
    public EngravingItem() { Type = ShopItemType.Engraving; }
}