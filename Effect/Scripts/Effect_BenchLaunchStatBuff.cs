using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

[CreateAssetMenu(fileName = "New Bench Launch Stat Effect", menuName = "Charms/Effects/Bench Launch Stat Buff")]
public class Effect_BenchLaunchStatBuff : EffectBase, IBenchSlotEffect
{
    [Header("발동 조건 (대상 벤치 슬롯)")]
    [Tooltip("효과를 적용할 벤치 슬롯 번호 목록 (1번 벤치 = 1, 3번 벤치 = 3). 비어있으면 모든 벤치 슬롯에서 발동합니다.")]
    public List<int> targetBenchSlots = new List<int> { 1 };

    /// <summary>
    /// 대상 벤치 슬롯(0-based index)에 효과가 적용되는지 여부를 반환합니다.
    /// </summary>
    public bool AffectsBenchSlot(int benchSlotIndex)
    {
        int slotNumber = benchSlotIndex + 1; // 1-based (1번 벤치 = 1)
        return targetBenchSlots == null || targetBenchSlots.Count == 0 || targetBenchSlots.Contains(slotNumber);
    }

    [Header("스탯 변경 설정")]
    [Tooltip("부여할 스탯 종류 (예: Attack, MaxSpeed 등)")]
    public PenguinPinball.Core.StatType targetStat = PenguinPinball.Core.StatType.Attack;

    [Tooltip("모디파이어 연산 방식 (Flat: 고정치 증가, PercentAdd: 비율 증가)")]
    public StatModType modType = StatModType.Flat;

    [Tooltip("증가 수치 (예: 공격력 +1이면 1, 이동속도 +20%면 0.2)")]
    public float statValue = 1f;

    // 현재 필드에 나간 펭귄별 적용된 모디파이어 추적
    private readonly Dictionary<PenguinController, StatModifier> activeModifiers = new Dictionary<PenguinController, StatModifier>();

    public override void ApplyEffect(GameObject origin = null)
    {
        GameEvents.OnPenguinLaunched += HandlePenguinLaunched;
        GameEvents.PenguinDrained += HandlePenguinDrained;
        GameEvents.PenguinRespawned += HandlePenguinRespawned;
        GameManager.OnStateChanged += HandleStateChanged;
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        GameEvents.OnPenguinLaunched -= HandlePenguinLaunched;
        GameEvents.PenguinDrained -= HandlePenguinDrained;
        GameEvents.PenguinRespawned -= HandlePenguinRespawned;
        GameManager.OnStateChanged -= HandleStateChanged;

        RemoveAllActiveModifiers();
    }

    private void HandlePenguinLaunched(PenguinController launchedPenguin)
    {
        if (launchedPenguin == null) return;

        // 펭귄의 벤치 슬롯 번호(1-based) 확인
        int benchSlotNumber = -1;
        if (PenguinDeckManager.Instance != null)
        {
            benchSlotNumber = PenguinDeckManager.Instance.GetBenchSlotNumber(launchedPenguin);
        }

        // 대상 벤치 슬롯 매칭 검사
        bool isMatchingSlot = (targetBenchSlots == null || targetBenchSlots.Count == 0 || targetBenchSlots.Contains(benchSlotNumber));

        if (!isMatchingSlot) return;

        // 기존에 등록된 모디파이어가 있다면 중복 방지 정리
        RemoveModifierFromPenguin(launchedPenguin);

        // 스탯 모디파이어 생성 및 적용
        var modifier = new StatModifier(statValue, modType, this);
        if (launchedPenguin.Stats != null && launchedPenguin.Stats.TryGetValue(targetStat, out var stat))
        {
            stat.AddModifier(modifier);
            launchedPenguin.OnStatsChanged();
            activeModifiers[launchedPenguin] = modifier;

            Debug.Log($"[Effect_BenchLaunchStatBuff] {benchSlotNumber}번 벤치 발사 버프 발동! {launchedPenguin.name}의 {targetStat} +{statValue} ({modType})");

            // 부적 횟수 차감 및 소진 시 자동 제거
            TriggerUse();
        }
    }

    private void HandlePenguinDrained(PenguinController penguin, float seconds)
    {
        RemoveModifierFromPenguin(penguin);
    }

    private void HandlePenguinRespawned(PenguinController penguin)
    {
        RemoveModifierFromPenguin(penguin);
    }

    private void HandleStateChanged(GameState newState)
    {
        // 웨이브 종료 또는 비전투 진입 시 잔여 필드 버프 일괄 제거
        if (newState != GameState.WaveInProgress)
        {
            RemoveAllActiveModifiers();
        }
    }

    private void RemoveModifierFromPenguin(PenguinController penguin)
    {
        if (penguin == null) return;

        if (activeModifiers.TryGetValue(penguin, out var modifier))
        {
            if (penguin.Stats != null && penguin.Stats.TryGetValue(targetStat, out var stat))
            {
                stat.RemoveAllModifiersFromSource(this);
                penguin.OnStatsChanged();
            }
            activeModifiers.Remove(penguin);
            Debug.Log($"[Effect_BenchLaunchStatBuff] {penguin.name}의 필드 버프 해제 완료");
        }
    }

    private void RemoveAllActiveModifiers()
    {
        foreach (var kvp in activeModifiers)
        {
            var penguin = kvp.Key;
            if (penguin != null && penguin.Stats != null && penguin.Stats.TryGetValue(targetStat, out var stat))
            {
                stat.RemoveAllModifiersFromSource(this);
                penguin.OnStatsChanged();
            }
        }
        activeModifiers.Clear();
    }
}
