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
    [SerializeField] protected float knockbackForce = 12.0f;     // 충돌 시 적이 받는 넉백 힘 (수치 증가)
    [SerializeField] protected float knockbackDuration = 0.35f;  // 넉백 전체 시간
    [SerializeField] protected float maxKnockbackDistance = 3.0f;// 최대 밀려날 거리
    [SerializeField] protected float knockbackScale = 0.5f;     // 넉백 거리 보정 배율 (수치 증가)

    [Header("피격 연출 (색상 변경)")]
    [SerializeField] protected Color hitColor = Color.red;       // 피격 시 변경될 색상
    [SerializeField] protected float hitFlashDuration = 0.15f;   // 색상이 유지될 시간

    [Header("애니메이션 설정")]
    [SerializeField] protected Animator animator;

    // ── 내부 상태 변수 ──
    protected float pathProgress = 0f;
    protected Rigidbody rb;
    protected Collider enemyCollider;

    // 넉백 상태 제어 변수
    protected bool isKnockbacked = false;
    private Vector3 currentKnockbackOffset = Vector3.zero;
    private Coroutine knockbackCoroutine;

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

    public virtual void SetPath(CatmullRomPath path)
    {
        currentPath = path;
        pathProgress = 0f;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Wave)
        {
            UpdateAnimation(false);
            return;
        }

        MoveAlongPath();
    }

    // ── CatmullRomPath 연동 경로 이동 로직 ───────────────────────
    public virtual void MoveAlongPath()
    {
        if (currentPath == null || !currentPath.IsValid)
        {
            UpdateAnimation(false);
            return;
        }

        int segmentCount = currentPath.SegmentCount;
        if (segmentCount <= 0) return;

        // 1. 시간에 따른 경로 진행도(pathProgress) 증가
        pathProgress += (moveSpeed / segmentCount) * Time.deltaTime;

        // 2. 경로 끝 도착 처리 (베이스 도달)
        if (pathProgress >= segmentCount)
        {
            pathProgress = segmentCount;
            OnReachPathEnd();
            return;
        }

        // 3. 현재 위치 계산 (순수 경로 좌표)
        Vector3 basePathPos = currentPath.EvaluateAtProgress(pathProgress);

        // 4. 회전 처리
        float lookAheadProgress = Mathf.Min(pathProgress + 0.1f, segmentCount);
        Vector3 lookAheadPos = currentPath.EvaluateAtProgress(lookAheadProgress);
        Vector3 moveDirection = lookAheadPos - basePathPos;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // 5. 위치 최종 이동 (경로 위치 + 넉백 오프셋)
        transform.position = basePathPos + currentKnockbackOffset;

        UpdateAnimation(true);
    }

    // ── 충돌 처리 ───────────────────────────────────────────────
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Player"))
        {
            Vector3 knockbackDir = collision.contacts.Length > 0
                ? -collision.contacts[0].normal
                : (transform.position - collision.transform.position).normalized;

            knockbackDir.y = 0f;
            if (knockbackDir.sqrMagnitude < 0.001f)
            {
                knockbackDir = transform.forward * -1f;
            }
            knockbackDir.Normalize();

            // 1. 넉백 연출 실행
            ApplyKnockback(knockbackDir, knockbackForce);

            // 2. 피격 색상 깜빡임 연출 실행
            FlashHitColor();
        }
    }

    // ── 넉백 실행 로직 ───────────────────────────────────────────
    public virtual void ApplyKnockback(Vector3 knockbackDir, float impulse)
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        float rawDistance = (impulse / Mathf.Max(0.1f, mass)) * knockbackScale;
        float calculatedDistance = Mathf.Clamp(rawDistance, 0.5f, maxKnockbackDistance);

        if (currentPath != null && currentPath.SegmentCount > 0)
        {
            float progressLoss = (calculatedDistance * 0.1f) / currentPath.SegmentCount;
            pathProgress = Mathf.Max(0f, pathProgress - progressLoss);
        }

        knockbackCoroutine = StartCoroutine(RoutineKnockback(knockbackDir, calculatedDistance));
    }

    private IEnumerator RoutineKnockback(Vector3 dir, float distance)
    {
        isKnockbacked = true;

        float elapsed = 0f;
        Vector3 pushDirection = dir.normalized;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;

            float curve = Mathf.Sin(t * Mathf.PI);
            currentKnockbackOffset = pushDirection * (distance * curve);

            yield return null;
        }

        currentKnockbackOffset = Vector3.zero;
        isKnockbacked = false;
        knockbackCoroutine = null;
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
    /// <summary>
    /// 경로 끝(베이스)에 도달했을 때 호출 (지상 적 전용)
    /// </summary>
    protected virtual void OnReachPathEnd()
    {
        if (isProcessed) return;
        isProcessed = true;

        UpdateAnimation(false);

        // 1. 보관소 드랍 기회 소멸 처리 (남은 적 카운트 갱신)
        if (ObjectDropManager.Instance != null)
        {
            ObjectDropManager.Instance.OnEnemyReachedBaseOrDespawned();
        }

        // 2. GameManager 생명(Life) 차감 연동 (DecreaseLife 메서드 호출)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DecreaseLife(baseDamage);
        }

        // 3. 적 파괴
        Destroy(gameObject);
    }

    protected virtual void UpdateAnimation(bool isMoving)
    {
        if (animator == null) return;
        animator.SetBool(IsMovingHash, isMoving);
    }
}