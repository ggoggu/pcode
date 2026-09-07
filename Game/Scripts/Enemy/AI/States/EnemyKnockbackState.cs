using UnityEngine;

public class EnemyKnockbackState : EnemyStateBase
{
    private Vector3 knockbackDirection;
    private float impulse;
    private Rigidbody rb;
    private float timer = 0f;

    private const float MinKnockbackDuration = 0.12f;   // 최소 물리 반응 시간 (너무 일찍 멈추는 것 방지)
    private const float StopSpeedThresholdSqr = 0.04f;  // 멈춤 판정 속도 제곱 (0.2m/s 이하)

    /// <summary>
    /// 동적 물리(Dynamic Rigidbody) 기반 넉백 생성자
    /// </summary>
    public EnemyKnockbackState(NormalEnemyAI enemy, EnemyStateMachine stateMachine, Vector3 direction, float impulse)
        : base(enemy, stateMachine)
    {
        this.knockbackDirection = direction.normalized;
        this.impulse = impulse;
        this.rb = enemy.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 하위 호환성 유지용 생성자
    /// </summary>
    public EnemyKnockbackState(NormalEnemyAI enemy, EnemyStateMachine stateMachine, Vector3 direction, float distance, float duration)
        : this(enemy, stateMachine, direction, distance * 5f)
    {
    }

    public override void Enter()
    {
        enemy.SetKnockbackedFlag(true);
        timer = 0f;

        if (rb != null)
        {
            // 1. Dynamic Rigidbody로 물리 활성화
            rb.isKinematic = false;
            rb.linearDamping = enemy.KnockbackDamping;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            // 2. 적의 질량(mass)을 반영한 충격량 인가
            float finalForce = impulse / Mathf.Max(0.1f, enemy.Mass);
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockbackDirection * finalForce, ForceMode.Impulse);
        }

        // 3. 경로 진행도: 인위적 차감 없이 넉백 종료 후 실제 튕겨난 현재 위치 기준으로 최근접 경로를 재계산합니다.
    }

    public override void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        // 최소 물리 반응 시간 경과 후 정지 여부 검사
        if (timer >= MinKnockbackDuration && rb != null)
        {
            float maxDuration = enemy.MaxKnockbackDuration > 0f ? enemy.MaxKnockbackDuration : 1.2f;
            if (rb.linearVelocity.sqrMagnitude <= StopSpeedThresholdSqr || timer >= maxDuration)
            {
                EndKnockback();
            }
        }
    }

    private void EndKnockback()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 감속 후 짧은 피격 경직(Stun) 후 스플라인 복귀
        if (enemy.KnockbackStunDuration > 0f)
        {
            stateMachine.ChangeState(new EnemyStunState(enemy, stateMachine, enemy.KnockbackStunDuration));
        }
        else
        {
            stateMachine.ChangeState(new EnemyReturnToSplineState(enemy, stateMachine));
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        // 넉백 중 추가 피격 시: 연속 충격량 가산
        bool isPenguin = collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Player")
            || (collision.collider != null && (collision.collider.CompareTag("Ball") || collision.collider.CompareTag("Player")));

        if (!isPenguin) return;

        Vector3 pushDir = Vector3.ProjectOnPlane(enemy.transform.position - collision.transform.position, Vector3.up);
        if (pushDir.sqrMagnitude < 0.001f)
        {
            pushDir = Vector3.ProjectOnPlane(collision.relativeVelocity, Vector3.up);
        }
        if (pushDir.sqrMagnitude < 0.001f)
        {
            pushDir = knockbackDirection;
        }
        pushDir.Normalize();

        float impactSpeed = collision.relativeVelocity.magnitude;
        float newImpulse = Mathf.Max(enemy.MinKnockbackImpulse * 0.8f, impactSpeed * enemy.KnockbackImpulseMultiplier);

        enemy.FlashHitColor();

        if (rb != null && !rb.isKinematic)
        {
            float force = newImpulse / Mathf.Max(0.1f, enemy.Mass);
            rb.AddForce(pushDir * force, ForceMode.Impulse);
        }
        timer = 0f; // 추가 충돌로 인한 넉백 시간 연장
    }

    public override void Exit()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        enemy.SetKnockbackedFlag(false);
    }
}
