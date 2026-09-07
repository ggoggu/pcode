using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ComboResetEnemyAI : NormalEnemyAI
{
    [Header("콤보 파괴 주기 발동 설정")]
    [Tooltip("주기적 자동 발동 간격 (초)")]
    [SerializeField] private float autoEffectInterval = 5.0f;

    [Tooltip("효과 적용 반경")]
    [SerializeField] private float effectRadius = 5.0f;

    [Tooltip("반경 내 대상 최대 수 (기획: 최대 3마리 전원 대상 가능)")]
    [SerializeField] private int maxTargets = 3;

    [Header("정지 효과 설정")]
    [Tooltip("중력 미적용 + 속도 0 수렴 지속 시간")]
    [SerializeField] private float freezeDuration = 0.5f;

    // 내부 상태
    private float effectTimer = 0f;
    private bool isFreezing = false;
    private float freezeTimer = 0f;

    // 현재 정지시키고 있는 펭귄들 (해제용)
    private List<IPenguinEffectable> frozenPenguins = new List<IPenguinEffectable>();

    protected override void Update()
    {
        base.Update(); // NormalEnemyAI의 이동 로직 유지

        // 주기적 자동 발동 타이머
        if (!isFreezing)
        {
            effectTimer += Time.deltaTime;
            if (effectTimer >= autoEffectInterval)
            {
                TriggerAreaEffect();
                effectTimer = 0f;
            }
        }

        // 정지 효과 타이머
        if (isFreezing)
        {
            freezeTimer += Time.deltaTime;
            if (freezeTimer >= freezeDuration)
            {
                EndFreeze();
            }
        }
    }

    // ── 충돌 처리 오버라이드 ───────────────────────────────────────
    protected override void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball") && !collision.gameObject.CompareTag("Player")) return;

        // 정지 중 외부 충돌 → 즉시 해제 (기획: 해제 조건)
        if (isFreezing)
        {
            EndFreeze();
        }

        // 반발력 + 넉백 (부모 로직 재사용)
        base.OnCollisionEnter(collision);
    }

    private void TriggerAreaEffect()
    {
        // 반경 내 "Ball" 태그 오브젝트 찾기
        Collider[] hits = Physics.OverlapSphere(transform.position, effectRadius);
        List<IPenguinEffectable> targets = new List<IPenguinEffectable>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Ball")) continue;

            IPenguinEffectable p = hit.GetComponent<IPenguinEffectable>();
            if (p != null)
            {
                targets.Add(p);
                if (targets.Count >= maxTargets) break; // 최대 3마리
            }
        }

        if (targets.Count == 0) return;

        // 효과 순서: 1) 콤보 삭제 + 과부하 해제 → 2) 정지
        foreach (var p in targets)
        {
            p.ApplyComboReset();    // 콤보 0 + 과부하 해제
            p.ApplyFreeze(freezeDuration); // 0.5초 중력 미적용 + 속도 0 수렴
        }

        frozenPenguins.Clear();
        frozenPenguins.AddRange(targets);

        isFreezing = true;
        freezeTimer = 0f;
        effectTimer = 0f;
    }

    private void EndFreeze()
    {
        foreach (var p in frozenPenguins)
        {
            if (p != null) p.EndFreeze();
        }
        frozenPenguins.Clear();

        isFreezing = false;
        freezeTimer = 0f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        // 에디터에서 효과 반경 시각화
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
