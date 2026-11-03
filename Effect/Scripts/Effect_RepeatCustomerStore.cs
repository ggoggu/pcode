using UnityEngine;

[CreateAssetMenu(fileName = "New Repeat Customer Store Effect", menuName = "Charms/Effects/Repeat Customer Store")]
public class Effect_RepeatCustomerStore : EffectBase
{
    [Header("단골 상점 설정")]
    [Tooltip("추가 구매 가격 배율 (기본 2배)")]
    public float priceMultiplier = 2.0f;

    [Tooltip("슬롯당 추가 구매 가능 횟수 (기본 1회)")]
    public int additionalPurchasesPerSlot = 1;

    public float PriceMultiplier => priceMultiplier;
    public int AdditionalPurchasesPerSlot => additionalPurchasesPerSlot;

    public override void ApplyEffect(GameObject origin = null)
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterRepeatStoreEffect(this);
        }
        Debug.Log($"[Effect_RepeatCustomerStore] 단골 상점 효과 활성화 (재구매 배율: {priceMultiplier}배, 슬롯당 {additionalPurchasesPerSlot}회)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UnregisterRepeatStoreEffect(this);
        }
        Debug.Log("[Effect_RepeatCustomerStore] 단골 상점 효과 해제");
    }
}
