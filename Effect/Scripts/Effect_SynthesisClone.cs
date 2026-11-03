using UnityEngine;
using PenguinPinball.Core;

[CreateAssetMenu(fileName = "New Synthesis Clone Effect", menuName = "Charms/Effects/Synthesis Clone")]
public class Effect_SynthesisClone : EffectBase
{
    [Header("합성 복제 설정")]
    [Tooltip("추가 생성될 펭귄의 성급 (기본 1성)")]
    public int cloneTier = 1;

    [Tooltip("합성 시 추가 생성될 펭귄 수 (기본 1마리)")]
    public int cloneCount = 1;

    public override void ApplyEffect(GameObject origin = null)
    {
        GameEvents.PenguinSynthesized += HandlePenguinSynthesized;
        Debug.Log($"[Effect_SynthesisClone] 합성 복제 효과 활성화 (합성 시 {cloneCount}마리 1성 추가 생성)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        GameEvents.PenguinSynthesized -= HandlePenguinSynthesized;
        Debug.Log("[Effect_SynthesisClone] 합성 복제 효과 해제");
    }

    private void HandlePenguinSynthesized(PenguinController synthesizedPenguin)
    {
        if (synthesizedPenguin == null || synthesizedPenguin.Definition == null) return;
        if (OwnerItem != null && OwnerItem.IsConsumable && OwnerItem.CurrentUses <= 0) return;

        // 소모형 부적 사용 횟수 차감 (마지막 횟수 소진 시 인벤토리에서 자동 제거 및 구독 해제)
        TriggerUse();

        // 합성된 펭귄 복제 생성
        CloneSynthesizedPenguin(synthesizedPenguin);
    }

    public void CloneSynthesizedPenguin(PenguinController synthesizedPenguin)
    {
        if (synthesizedPenguin == null || synthesizedPenguin.Definition == null) return;
        if (PenguinDeckManager.Instance == null)
        {
            Debug.LogWarning("[Effect_SynthesisClone] PenguinDeckManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Vector3 spawnPos = synthesizedPenguin.transform != null
            ? synthesizedPenguin.transform.position + new Vector3(0.5f, 0f, 0.5f)
            : Vector3.zero;

        for (int i = 0; i < cloneCount; i++)
        {
            var cloned = PenguinDeckManager.Instance.CreateAndRegisterPenguin(
                synthesizedPenguin.Definition,
                cloneTier,
                spawnPos
            );

            if (cloned != null)
            {
                Debug.Log($"[Effect_SynthesisClone] 합성 복제 완료! {synthesizedPenguin.Definition.DisplayName} (Tier {cloneTier}) 추가 생성됨 (남은 횟수: {OwnerItem?.CurrentUses})");
            }
            else
            {
                Debug.LogWarning($"[Effect_SynthesisClone] {synthesizedPenguin.Definition.DisplayName} 복제 실패 (덱 공간 부족)");
            }
        }
    }
}
