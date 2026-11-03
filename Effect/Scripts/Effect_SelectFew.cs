using UnityEngine;

[CreateAssetMenu(fileName = "New SelectFew Effect", menuName = "Charms/Effects/SelectFew")]
public class Effect_SelectFew : EffectBase
{
    [Header("필드 슬롯 제한 설정")]
    [Tooltip("제한할 필드 슬롯 개수 (기본 1칸 제한 -> 필드 상한 3에서 2로)")]
    public int lockedFieldSlots = 1;

    [Header("과부하 설정")]
    [Tooltip("과부하 발동에 필요한 콤보 수치 감소량 (기본 2 감소)")]
    public int overloadThresholdReduction = 2;

    public static int ActiveThresholdReduction { get; private set; } = 0;

    public static void ResetStaticData()
    {
        ActiveThresholdReduction = 0;
    }

    public override void ApplyEffect(GameObject origin = null)
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.LockedFieldSlots += lockedFieldSlots;
        }

        if (StatModifierRegistry.Instance != null)
        {
            StatModifierRegistry.Instance.AddModifier(StatType.OverloadThreshold, -overloadThresholdReduction);
        }
        ActiveThresholdReduction += overloadThresholdReduction;

        Debug.Log($"[Effect_SelectFew] 소수정예 효과 활성화 (필드 슬롯 -{lockedFieldSlots}, 과부하 필요 콤보 -{overloadThresholdReduction})");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.LockedFieldSlots = Mathf.Max(0, PenguinDeckManager.Instance.LockedFieldSlots - lockedFieldSlots);
        }

        if (StatModifierRegistry.Instance != null)
        {
            StatModifierRegistry.Instance.RemoveModifier(StatType.OverloadThreshold, -overloadThresholdReduction);
        }
        ActiveThresholdReduction = Mathf.Max(0, ActiveThresholdReduction - overloadThresholdReduction);

        Debug.Log("[Effect_SelectFew] 소수정예 효과 해제");
    }
}
