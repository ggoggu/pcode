using UnityEngine;

[CreateAssetMenu(fileName = "New Modify Stat Effect", menuName = "Charms/Effects/Modify Stat")]
public class ModifyStatEffect : EffectBase
{
    [Header("변경할 스탯 설정")]
    [Tooltip("어떤 스탯을 조작할지 선택합니다.")]
    public StatType targetStat;

    [Tooltip("합연산 수치 (예: 체력 +2면 2, 제한 인원 -1이면 -1)")]
    public float flatAmount = 0f;

    [Tooltip("곱연산 배율 수치 (예: 데미지 20% 증가면 0.2, 50% 감소면 -0.5)")]
    public float multAmount = 0f;

    public override void ApplyEffect(GameObject origin = null)
    {
        if (StatModifierRegistry.Instance != null)
        {
            StatModifierRegistry.Instance.AddModifier(targetStat, flatAmount, multAmount);
            Debug.Log($"[Effect] {targetStat} 스탯 조작 적용 (Flat: {flatAmount}, Mult: {multAmount})");
        }
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        if (StatModifierRegistry.Instance != null)
        {
            StatModifierRegistry.Instance.RemoveModifier(targetStat, flatAmount, multAmount);
            Debug.Log($"[Effect] {targetStat} 스탯 조작 해제 (Flat 원복: {-flatAmount}, Mult 원복: {-multAmount})");
        }
    }
}