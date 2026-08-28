using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShrinkEnemyAI : NormalEnemyAI
{
    [Header("축소 효과 설정")]
    [Tooltip("충돌 누적 횟수가 이 값에 도달하면 발동")]
    [SerializeField] private int hitCountToTrigger = 3;

    [Tooltip("축소 지속 시간")]
    [SerializeField] private float shrinkDuration = 5.0f;

    [Tooltip("원래 크기의 몇 배로 축소 (0.5 = 절반)")]
    [SerializeField] private float shrinkScaleMultiplier = 0.5f;

    [Tooltip("축소 중 외부 충돌 시 즉시 해제 여부")]
    [SerializeField] private bool cancelOnHitDuringShrink = true;

    // 내부 카운터
    private int hitCounter = 0;
    private bool isShrinking = false;
    private float shrinkTimer = 0f;

    // 현재 축소를 적용하고 있는 펭귄들 (해제용)
    private List<IPenguinEffectable> affectedPenguins = new List<IPenguinEffectable>();

    protected override void Update()
    {
        base.Update(); // NormalEnemyAI의 이동 로직 유지

        // 축소 중 타이머 처리
        if (isShrinking)
        {
            shrinkTimer += Time.deltaTime;
            if (shrinkTimer >= shrinkDuration)
            {
                EndShrink();
            }
        }
    }

    // ── 충돌 처리 오버라이드 ───────────────────────────────────────
    protected override void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        // 축소 중 외부 충돌 → 즉시 해제 (기획: 해제 조건)
        if (isShrinking && cancelOnHitDuringShrink)
        {
            EndShrink();
            base.OnCollisionEnter(collision); // 반발력 + 넉백은 정상 처리 (카운터 증가 X — 해제가 우선)
            return;
        }

        // 1) 카운터 증가 (주체: 적 개체, 펭귄 구분 없음, Vcut 무관)
        hitCounter++;

        // 2) 반발력 + 넉백 (부모 로직 재사용)
        base.OnCollisionEnter(collision);

        // 3) 카운터 도달 시 발동 후 초기화
        if (hitCounter >= hitCountToTrigger)
        {
            ActivateShrink(collision.gameObject);
            hitCounter = 0; // 발동 후 0으로 초기화
        }
    }

    private void ActivateShrink(GameObject penguinObj)
    {
        IPenguinEffectable penguin = penguinObj.GetComponent<IPenguinEffectable>();
        if (penguin == null) return;

        penguin.ApplyShrink(shrinkDuration, shrinkScaleMultiplier);
        affectedPenguins.Clear();
        affectedPenguins.Add(penguin);

        isShrinking = true;
        shrinkTimer = 0f;
    }

    private void EndShrink()
    {
        foreach (var p in affectedPenguins)
        {
            if (p != null) p.EndShrink();
        }
        affectedPenguins.Clear();

        isShrinking = false;
        shrinkTimer = 0f;
    }
}
