using UnityEngine;

public enum InstantTargetType
{
    None,           // 대상 없음 (구매 즉시 발동)
    SinglePenguin,  // 펭귄 1마리 선택
    TwoPenguins     // 펭귄 2마리 선택 (교환 등)
}

[CreateAssetMenu(fileName = "NewInstant", menuName = "Shop/Instant")]
public class InstantItem : ShopItemBase
{
    [Header("Instant Target Settings")]
    public InstantTargetType TargetType = InstantTargetType.None;
    [TextArea] public string InstructionText = "효과를 적용할 대상을 선택하세요.";

    public InstantItem() 
    { 
        Type = ShopItemType.Instant; 
    }
    
    public virtual bool RequiresTargetSelection => TargetType != InstantTargetType.None;

    /// <summary>
    /// 아이템을 현재 사용할 수 있는 상태인지 검증합니다.
    /// (예: 교환 대상 펭귄이 2마리 이상 존재하는지 등)
    /// </summary>
    public virtual bool CanUse(out string failReason)
    {
        failReason = string.Empty;
        return true;
    }

    /// <summary>
    /// 대상 선택이 필요 없는 즉발 아이템 효과 실행
    /// </summary>
    public virtual void ExecuteInstantEffect() 
    {
        Debug.Log($"[Instant] {ItemName} 즉시 효과 발동!");
    }

    /// <summary>
    /// 대상 선택이 필요한 즉발 아이템 효과 실행
    /// </summary>
    public virtual bool ExecuteTargetedEffect(params object[] targets)
    {
        Debug.Log($"[Instant] {ItemName} 대상 효과 발동!");
        return true;
    }
}