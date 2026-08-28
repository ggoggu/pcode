using UnityEngine;

public abstract class EffectBase : ScriptableObject
{
    public ShopItemBase OwnerItem { get; private set; }

    public void SetOwner(ShopItemBase owner)
    {
        OwnerItem = owner;
    }

    // 소모형 부적인 경우 횟수를 차감하고 모두 소진 시 인벤토리에서 자동 제거
    public void TriggerUse()
    {
        if (OwnerItem != null)
        {
            OwnerItem.UseItem();
        }
    }

    public abstract void ApplyEffect(GameObject origin = null);

    // 효과를 해제할 때 호출됩니다.
    public abstract void RemoveEffect(GameObject origin = null);
}
