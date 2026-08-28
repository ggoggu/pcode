using UnityEngine;

[CreateAssetMenu(fileName = "NewInstant", menuName = "Shop/Instant")]
public class InstantItem : ShopItemBase
{
    public InstantItem() { Type = ShopItemType.Instant; }
    
    // 획득 즉시 실행될 로직
    public void ExecuteInstantEffect() 
    {
        Debug.Log($"[Instant] {ItemName} 즉시 효과 발동!");
        // TODO: 즉시사용 효과 세부 구현
    }
}