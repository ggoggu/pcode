using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalEnemyAI : Enemy
{
    [Header("지상 적 기본 스탯")]
    [SerializeField] protected float mass = 1.0f;        // 무게 (넉백 거리 계산용)
    [SerializeField] protected int baseDamage = 1;       // 베이스 도달 시 GameManager 체력 차감량

    [Header("이동 및 경로 설정")]
    [SerializeField] protected float moveSpeed = 3.0f;
    [SerializeField] protected CatmullRomPath currentPath;

    [Header("적 넉백 옵션")]
    [SerializeField] protected float knockbackForce = 12.0f;     // 충돌 시 적이 받는 기본 넉백 힘 (하위 호환)
    [SerializeField] protected float knockbackDuration = 0.35f;  // 넉백 전체 시간 (하위 호환)
    [SerializeField] protected float maxKnockbackDistance = 3.0f;// 최대 밀려날 거리 (하위 호환)
    [SerializeField] protected float knockbackScale = 0.5f;      // 넉백 거리 보정 배율 (하위 호환)
    [SerializeField] protected float knockbackImpulseMultiplier = 1.5f; // 펭귄 충돌 속도에 비례한 충격량 배율
    [SerializeField] protected float minKnockbackImpulse = 4.0f;         // 보장되는 최소 넉백 충격량
    [SerializeField] protected float knockbackDamping = 3.2f;            // 넉백 중 미끄러짐 감속 마찰계수 (linearDamping)
    [SerializeField] protected float knockbackStunDuration = 0.15f;      // 정지 후 스플라인 복귀 전 피격 경직 시간
    [SerializeField] protected float maxKnockbackDuration = 1.2f;        // 넉백 물리 최대 안전 지속시간

    [Header("스플라인 복귀 및 회전 설정")]
    [SerializeField] protected float returnToSplineSpeedMultiplier = 1.5f; // 스플라인 복귀 시 이동 속도 배율
    [SerializeField] protected float rotationSpeed = 6f;                   // 회전 보간 속도 (초당 최대 회전각 = rotationSpeed * 30도)

    [Header("피격 연출 (색상 변경)")]
    [SerializeField] protected Color hitColor = Color.red;       // 피격 시 변경될 색상
    [SerializeField] protected float hitFlashDuration = 0.15f;   // 색상이 유지될 시간

    [Header("애니메이션 설정")]
    [SerializeField] protected Animator animator;

    // ── 내부 상태 및 FSM ──
    public EnemyStateMachine StateMachine { get; private set; }

    protected float pathProgress = 0f;
    protected Rigidbody rb;
    protected Collider enemyCollider;

    // 넉백 상태 제어 변수 (하위 호환성 유지)
    protected bool isKnockbacked = false;

    // 피격 색상 연출용 변수
    private Renderer[] enemyRenderers;
    private List<Color[]> originalColors = new List<Color[]>();
    private Coroutine hitFlashCoroutine;

    // 중복 처치 / 중복 베이스 도달 방지 플래그
    protected bool isProcessed = false;

    // ── 프로퍼티 ──
    public float Mass { get => mass; set => mass = Mathf.Max(0.1f, value); }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public CatmullRomPath CurrentPath { get => currentPath; set => currentPath = value; }
    public float PathProgress { get => pathProgress; set => pathProgress = value; }
    public float KnockbackForce => knockbackForce;
    public float KnockbackDuration => knockbackDuration;
    public float MaxKnockbackDistance => maxKnockbackDistance;
    public float KnockbackScale => knockbackScale;
    public float KnockbackImpulseMultiplier => knockbackImpulseMultiplier;
    public float MinKnockbackImpulse => minKnockbackImpulse;
    public float KnockbackDamping => knockbackDamping;
    public float KnockbackStunDuration => knockbackStunDuration;
    public float MaxKnockbackDuration => maxKnockbackDuration;
    public float ReturnToSplineSpeedMultiplier => returnToSplineSpeedMultiplier;
    public float RotationSpeed => rotationSpeed;
    public bool IsKnockbacked => isKnockbacked;

    protected static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        enemyCollider = GetComponent<Collider>();
        CacheRenderersAndColors();

        // FSM 상태 머신 초기화
        StateMachine = new EnemyStateMachine();
        StateMachine.Initialize(new EnemyPathFollowState(this, StateMachine));
    }

    private void CacheRenderersAndColors()
    {
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors.Clear();

        foreach (var rend in enemyRenderers)
        {
            Material[] mats = rend.materials;
            Color[] colors = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                {
                    colors[i] = mats[i].color;
                }
                else if (mats[i].HasProperty("_BaseColor"))
                {
                    colors[i] = mats[i].GetColor("_BaseColor");
                }
                else
                {
                    colors[i] = Color.white;
                }
            }
            originalColors.Add(colors);
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    protected virtual void OnDisable()
    {
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    public virtual void SetPath(CatmullRomPath path)
    {
        currentPath = path;
        pathProgress = 0f;
        StateMachine?.ChangeState(new EnemyPathFollowState(this, StateMachine));
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Wave && GameManager.Instance.CurrentState != GameState.WaveInProgress)
        {
            UpdateAnimation(false);
            return;
        }

        StateMachine?.Update();
    }

    protected virtual void FixedUpdate()
    {
        StateMachine?.FixedUpdate();
    }

    /// <summary>
    /// 하위 호환성 및 수동 경로 이동 호출용 메서드
    /// </summary>
    public virtual void MoveAlongPath()
    {
        if (StateMachine?.CurrentState is EnemyPathFollowState)
        {
            StateMachine.Update();
        }
    }

    // ── 충돌 처리 ───────────────────────────────────────────────
    protected virtual void OnCollisionEnter(Collision collision)
    {
        StateMachine?.OnCollisionEnter(collision);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        StateMachine?.OnTriggerEnter(other);
    }

    /// <summary>
    /// 펭귄과의 충돌 시 호출하여 상대 속도 및 질량을 반영한 동적 물리 넉백을 가합니다.
    /// </summary>
    public virtual void HandlePenguinCollision(Collision collision, float impulseScale = 1.0f)
    {
        bool isPenguin = collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Player")
            || (collision.collider != null && (collision.collider.CompareTag("Ball") || collision.collider.CompareTag("Player")));

        if (!isPenguin) return;

        // X-Z 평면 기준 넉백 방향 계산 (펭귄 중심 -> 적 중심 밀어내는 방향 우선)
        Vector3 pushDir = Vector3.ProjectOnPlane(transform.position - collision.transform.position, Vector3.up);
        if (pushDir.sqrMagnitude < 0.001f)
        {
            pushDir = Vector3.ProjectOnPlane(collision.relativeVelocity, Vector3.up);
        }
        if (pushDir.sqrMagnitude < 0.001f)
        {
            pushDir = -transform.forward;
        }
        pushDir.Normalize();

        // 충돌 속도(relativeVelocity) 기반 동적 충격량 산출 (현실 물리 비례)
        float impactSpeed = collision.relativeVelocity.magnitude;
        float impulse = Mathf.Max(minKnockbackImpulse, impactSpeed * knockbackImpulseMultiplier) * impulseScale;

        FlashHitColor();
        TriggerKnockback(pushDir, impulse);
    }

    // ── 넉백 실행 로직 ───────────────────────────────────────────
    public virtual void ApplyKnockback(Vector3 knockbackDir, float impulse)
    {
        TriggerKnockback(knockbackDir, impulse);
    }

    public virtual void TriggerKnockback(Vector3 knockbackDir, float impulse)
    {
        StateMachine?.ChangeState(new EnemyKnockbackState(this, StateMachine, knockbackDir, impulse));
    }

    public void ReturnToSpline()
    {
        StateMachine?.ChangeState(new EnemyReturnToSplineState(this, StateMachine));
    }

    public void ApplyStun(float duration)
    {
        StateMachine?.ChangeState(new EnemyStunState(this, StateMachine, duration));
    }

    public void SetKnockbackedFlag(bool value)
    {
        isKnockbacked = value;
    }

    // ── 피격 색상 연출 로직 ────────────────────────────────────────
    public void FlashHitColor()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }
        hitFlashCoroutine = StartCoroutine(RoutineHitFlash());
    }

    private IEnumerator RoutineHitFlash()
    {
        SetRenderersColor(hitColor);
        yield return new WaitForSeconds(hitFlashDuration);
        ResetRenderersColor();
        hitFlashCoroutine = null;
    }

    private void SetRenderersColor(Color color)
    {
        if (enemyRenderers == null) return;

        foreach (var rend in enemyRenderers)
        {
            if (rend == null) continue;

            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    mat.color = color;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
            }
        }
    }

    private void ResetRenderersColor()
    {
        if (enemyRenderers == null) return;

        for (int r = 0; r < enemyRenderers.Length; r++)
        {
            if (enemyRenderers[r] == null) continue;

            Material[] mats = enemyRenderers[r].materials;
            Color[] origColors = originalColors[r];

            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m].HasProperty("_Color"))
                {
                    mats[m].color = origColors[m];
                }
                else if (mats[m].HasProperty("_BaseColor"))
                {
                    mats[m].SetColor("_BaseColor", origColors[m]);
                }
            }
        }
    }

    // ── 처치 / 베이스 도달 처리 ───────────────────────────────────

    /// <summary>
    /// 플레이어 공격에 의해 처치될 때 호출
    /// </summary>
    protected override void Die()
    {
        if (isProcessed) return;
        isProcessed = true;

        base.Die();

        // 1. 보관소 아이템 드랍 확률 계산 및 습득 연동
        if (ObjectDropManager.Instance != null)
        {
            ObjectDropManager.Instance.OnEnemyKilledByPlayer(transform.position);
        }
    }

    /// <summary>
    /// 경로 끝(베이스)에 도달했을 때 호출 (지상 적 전용)
    /// </summary>
    public virtual void OnReachPathEnd()
    {
        if (isProcessed) return;
        isProcessed = true;

        UpdateAnimation(false);

        // 1. 보관소 드랍 기회 소멸 처리 (남은 적 카운트 갱신)
        if (ObjectDropManager.Instance != null)
        {
            ObjectDropManager.Instance.OnEnemyReachedBaseOrDespawned();
        }

        // 2. GameManager 생명(Life) 차감 연동
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DecreaseLife(baseDamage);
        }

        // 3. 적 파괴
        Destroy(gameObject);
    }

    public virtual void UpdateAnimation(bool isMoving)
    {
        if (animator == null) return;
        animator.SetBool(IsMovingHash, isMoving);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (currentPath == null || !currentPath.IsValid) return;

        // 현재 스플라인 목표 지점 및 복귀 라인 시각화
        Vector3 splinePos = currentPath.EvaluateAtProgress(pathProgress);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(splinePos, 0.2f);
        Gizmos.DrawLine(transform.position, splinePos);
    }
}