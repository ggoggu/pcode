using PenguinPinball.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mark Amplification Effect", menuName = "Charms/Effects/Mark Amplification")]
public class Effect_MarkAmplification : EffectBase
{
    [Header("데미지 증폭 설정")]
    [Tooltip("표식 1종류당 충돌 데미지 증가율 (예: 0.25면 표식 1종류당 +25% 증가)")]
    [SerializeField] private float damageMultiplierPerMark = 0.25f;

    [Tooltip("True일 경우 충돌(직접 타격) 데미지만 증폭합니다. False면 모든 피해를 증폭합니다.")]
    [SerializeField] private bool collisionOnly = true;

    public float DamageMultiplierPerMark
    {
        get => damageMultiplierPerMark;
        set => damageMultiplierPerMark = value;
    }

    public bool CollisionOnly
    {
        get => collisionOnly;
        set => collisionOnly = value;
    }

    public override void ApplyEffect(GameObject origin = null)
    {
        CombatResolver.RegisterDamageModifier(OnModifyDamage);
        Debug.Log($"[Effect_MarkAmplification] 표식 증폭 효과 활성화 (표식 종류당 +{damageMultiplierPerMark * 100f:F0}%)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        CombatResolver.UnregisterDamageModifier(OnModifyDamage);
        Debug.Log("[Effect_MarkAmplification] 표식 증폭 효과 해제");
    }

    private float OnModifyDamage(PenguinController source, Enemy enemy, float currentDamage, bool isCollision)
    {
        // 충돌 전용 옵션이 켜져 있고 충돌 피해가 아니면 증폭하지 않음
        if (collisionOnly && !isCollision) return currentDamage;
        if (enemy == null || enemy.IsDead || currentDamage <= 0f) return currentDamage;

        // 적에게 부착된 활성 표식의 고유 종류 수 조회 (동일 종류 중복은 1개로 취급)
        int markCount = CombatResolver.GetDistinctMarkCount(enemy);
        if (markCount <= 0) return currentDamage;

        // 표식 종류 수에 비례하여 충돌 데미지 증폭 (예: 1개 -> +25%, 2개 -> +50%)
        float bonusMultiplier = 1f + (markCount * damageMultiplierPerMark);
        float finalDamage = currentDamage * bonusMultiplier;

        Debug.Log($"[Effect_MarkAmplification] 표식이 붙은 적({enemy.name}) 타격! 표식 {markCount}종류 감지 -> 충돌 데미지 {currentDamage:F1} -> {finalDamage:F1} (x{bonusMultiplier:F2})");

        // 소모형 부적인 경우 사용 횟수 차감 (영구 부적은 IsConsumable=false이므로 영향 없음)
        TriggerUse();

        return finalDamage;
    }
}
