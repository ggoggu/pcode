using System.Collections.Generic;
using UnityEngine;

public enum ShopItemType { Charm, Equipment, Engraving, FieldObject, Instant }

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/ShopItem")]
public class ShopItemBase : ScriptableObject
{
    public string ItemID;
    public string ItemName;
    public ShopItemType Type;
    public int Price;
    public Sprite Icon;
    [TextArea] public string Description;
    
    [Header("Consumable")]
    public bool IsConsumable = false;
    public int MaxUses = 1;
    [HideInInspector] public int CurrentUses;

    [Header("Effects")]
    public List<EffectBase> Effects = new List<EffectBase>();

    private void OnEnable()
    {
        CurrentUses = MaxUses;
    }

    public void UseItem()
    {
        if (!IsConsumable) return;
        
        CurrentUses--;
        Debug.Log($"[ShopItemBase] {ItemName} 사용됨 (남은 횟수: {CurrentUses}/{MaxUses})");
        
        if (CurrentUses <= 0)
        {
            Debug.Log($"[ShopItemBase] {ItemName} 횟수 소진됨 -> 인벤토리에서 제거");
            if (ItemInventoryManager.Instance != null)
            {
                ItemInventoryManager.Instance.RemoveCharm(this);
            }
        }
    }

    public void ApplyEffects(GameObject target)
    {
        foreach (var effect in Effects)
        {
            if (effect != null)
            {
                effect.SetOwner(this);
                effect.ApplyEffect(target);
            }
        }
    }

    public void RemoveEffects(GameObject target)
    {
        foreach (var effect in Effects)
        {
            if (effect != null)
            {
                effect.RemoveEffect(target);
                effect.SetOwner(null);
            }
        }
    }
}