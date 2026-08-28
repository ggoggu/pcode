using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SeagullState
{
    Flying,
    Chasing,
    Resting,
    Catching,
    Exiting
}

public class SeagullAI : Enemy
{
    [Header("시각적 표현 및 지면 마커 분리")]
    [SerializeField] private Transform visualModel;        // 자식 3D 모델
    [SerializeField] private Transform groundMarker;       // 지면에 표시할 투영 마커

    [Header("이동 및 경로 설정")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private CatmullRomPath currentPath;

    [Header("갈매기 상태 설정")]
    [SerializeField] private SeagullState currentState = SeagullState.Flying;
    [SerializeField] private float flyHeight           = 3.5f;   // 비행 높이
    [SerializeField] private float diveHeight          = 0.3f;   // 다이빙 높이
    [SerializeField] private float heightLerpSpeed     = 8.0f;

    [Header("휴식 및 퇴장 설정")]
    [SerializeField] private float flyDuration         = 6.0f;
    [SerializeField] private float restDuration        = 4.0f;
    [SerializeField] private float restY               = 0.2f;   
    
    [Tooltip("N번째 휴식이 끝나는 순간 맵 밖으로 퇴장하도록 설정")]
    [SerializeField] private int exitRestCount         = 2;
    private int currentRestCount                    = 0;

    [Header("휴식 랜덤 위치 범위")]
    [SerializeField] private float restXMin = -4f;
    [SerializeField] private float restXMax =  4f;
    [SerializeField] private float restZMin = -2f;
    [SerializeField] private float restZMax =  2f;

    [Header("추적 및 감지 설정")]
    [SerializeField] private float detectionRadius   = 8.0f;   // 공 감지 범위
    [SerializeField] private float catchRadius       = 1.2f;   // 잡기 판정 거리
    [SerializeField] private float maxChaseDuration  = 5.0f;
    [SerializeField] private float chaseSpeedMult    = 1.3f;

    [Header("Catch & Throw 설정")]
    [SerializeField] private Transform flipperCenterTarget;
    [SerializeField] private float throwForce          = 15.0f;

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;

    // ── 갈매기 불필요 스탯 무력화 ──────────────────────────────────────
    public new float CurrentHealth => 999999f;
    public new bool IsDead => false;

    public override void TakeDamage(float value) { }
    protected override void Die() { }
    public override void ResetHealth() { }

    // ── 내부 상태 변수 ─────────────────────────────────────────────
    private float     stateTimer          = 0f;
    private float     targetVisualY       = 0f;
    private float     currentVisualY      = 0f;
    private float     pathProgress        = 0f;

    private Transform chasingTarget       = null;
    private Transform caughtPenguin       = null;
    private Rigidbody caughtPenguinRb     = null;
    private Collider  caughtPenguinCol    = null;

    private Vector3   restPosition;
    private bool      useAssignedRestPosition = false;
    private Vector3   assignedRestPosition;

    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public CatmullRomPath CurrentPath { get => currentPath; set => currentPath = value; }

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    protected override void Awake()
    {
        base.Awake();
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualModel == null && transform.childCount > 0)
            visualModel = transform.GetChild(0);

        targetVisualY = flyHeight;
        currentVisualY = flyHeight;
        if (visualModel != null)
        {
            visualModel.localPosition = new Vector3(0f, flyHeight, 0f);
        }
    }

    protected override void Start()
    {
        base.Start();

        if (flipperCenterTarget == null)
        {
            GameObject obj = GameObject.Find("FlipperCenterTarget");
            if (obj != null)
                flipperCenterTarget = obj.transform;
            else
                Debug.LogWarning("[SeagullAI] FlipperCenterTarget을 찾을 수 없습니다!");
        }
    }

    public void SetPath(CatmullRomPath path)
    {
        currentPath = path;
        pathProgress = 0f;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameState.Wave) return;

        stateTimer += Time.deltaTime;

        UpdateVisualHeight();

        switch (currentState)
        {
            case SeagullState.Flying:   UpdateFlying();   break;
            case SeagullState.Chasing:  UpdateChasing();  break;
            case SeagullState.Resting:  UpdateResting();  break;
            case SeagullState.Catching: UpdateCatching(); break;
            case SeagullState.Exiting:  UpdateExiting();  break;
        }
    }

    private void SetState(SeagullState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case SeagullState.Flying:
            case SeagullState.Catching:
            case SeagullState.Exiting:
                targetVisualY = flyHeight;
                break;

            case SeagullState.Chasing:
                targetVisualY = flyHeight;
                break;

            case SeagullState.Resting:
                targetVisualY = restY;
                if (useAssignedRestPosition)
                {
                    restPosition = assignedRestPosition;
                }
                else
                {
                    restPosition = new Vector3(
                        Random.Range(restXMin, restXMax),
                        0f,
                        Random.Range(restZMin, restZMax)
                    );
                }
                break;
        }
    }

    private void UpdateVisualHeight()
    {
        currentVisualY = Mathf.Lerp(currentVisualY, targetVisualY, heightLerpSpeed * Time.deltaTime);

        if (visualModel != null)
        {
            visualModel.localPosition = new Vector3(0f, currentVisualY, 0f);
        }

        if (groundMarker != null)
        {
            groundMarker.localPosition = Vector3.zero;
        }
    }

    private void MoveAlongPath(float speedMultiplier = 1.0f)
    {
        if (currentPath == null || !currentPath.IsValid)
        {
            UpdateAnimation(false);
            return;
        }

        int segmentCount = currentPath.SegmentCount;
        if (segmentCount <= 0) return;

        Vector3 targetPos = currentPath.EvaluateAtProgress(pathProgress);
        targetPos.y = 0f;

        float distToNode = Vector3.Distance(transform.position, targetPos);
        if (distToNode > 0.2f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPos, 
                moveSpeed * speedMultiplier * Time.deltaTime
            );
        }
        else
        {
            pathProgress += (moveSpeed * speedMultiplier / segmentCount) * Time.deltaTime;
            transform.position = targetPos;
        }

        if (pathProgress >= segmentCount)
        {
            pathProgress = segmentCount;
            OnReachPathEnd();
            return;
        }

        Vector3 moveDir = targetPos - transform.position;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        UpdateAnimation(true);
    }

    private void UpdateFlying()
    {
        MoveAlongPath();

        Transform nearest = FindNearestBall(detectionRadius);
        if (nearest != null)
        {
            chasingTarget = nearest;
            SetState(SeagullState.Chasing);
            return;
        }

        if (stateTimer >= flyDuration)
            SetState(SeagullState.Resting);
    }

    private void UpdateChasing()
    {
        if (chasingTarget == null)
        {
            SetState(SeagullState.Flying);
            return;
        }

        Vector3 targetPos = chasingTarget.position;
        targetPos.y = 0f;

        float distToTarget = Vector3.Distance(transform.position, targetPos);

        if (distToTarget > detectionRadius * 1.5f || stateTimer >= maxChaseDuration)
        {
            chasingTarget = null;
            SetState(SeagullState.Flying);
            return;
        }

        float diveProgress = Mathf.Clamp01(1f - (distToTarget / 3.0f));
        targetVisualY = Mathf.Lerp(flyHeight, diveHeight, diveProgress);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * chaseSpeedMult * Time.deltaTime);

        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                10f * Time.deltaTime);
        }

        if (distToTarget <= catchRadius)
        {
            // 타겟 본인 또는 자식/부모의 Rigidbody 탐색
            Rigidbody ballRb = chasingTarget.GetComponent<Rigidbody>();
            if (ballRb == null) ballRb = chasingTarget.GetComponentInChildren<Rigidbody>();

            if (ballRb != null)
            {
                CatchPenguin(ballRb.transform, ballRb);
            }
        }
    }

    private void UpdateResting()
    {
        Vector3 targetPos = restPosition;
        targetPos.y = 0f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime);

        if (stateTimer >= restDuration)
        {
            currentRestCount++;

            if (currentRestCount >= exitRestCount)
                SetState(SeagullState.Exiting);
            else
                SetState(SeagullState.Flying);
        }
    }

    private void CatchPenguin(Transform target, Rigidbody rb)
    {
        caughtPenguin = target;
        caughtPenguinRb = rb;
        
        // 블록이나 다른 부모 오브젝트의 콜라이더가 아니라, 공의 콜라이더만 탐색
        caughtPenguinCol = target.GetComponent<Collider>();
        if (caughtPenguinCol == null)
            caughtPenguinCol = target.GetComponentInChildren<Collider>();

        if (caughtPenguinRb != null)
        {
            if (!caughtPenguinRb.isKinematic)
            {
                caughtPenguinRb.linearVelocity = Vector3.zero;
                caughtPenguinRb.angularVelocity = Vector3.zero;
            }
            caughtPenguinRb.isKinematic = true;
        }

        if (caughtPenguinCol != null)
        {
            caughtPenguinCol.enabled = false;
        }

        chasingTarget = null;
        SetState(SeagullState.Catching);
    }

    private void UpdateCatching()
    {
        if (caughtPenguin == null || flipperCenterTarget == null)
        {
            ReleasePenguin();
            SetState(SeagullState.Flying);
            return;
        }

        Vector3 holdPos = (visualModel != null) 
            ? visualModel.position + Vector3.down * 0.5f 
            : transform.position + Vector3.up * (currentVisualY - 0.5f);

        caughtPenguin.position = holdPos;

        Vector3 destination = flipperCenterTarget.position;
        destination.y = 0f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * chaseSpeedMult * Time.deltaTime);

        Vector3 dir = destination - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                10f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, destination) <= 1.0f)
        {
            ThrowPenguin();
            SetState(SeagullState.Flying);
        }
    }

    private void UpdateExiting()
    {
        MoveAlongPath(1.5f);
    }

    private void ThrowPenguin()
    {
        if (caughtPenguinRb != null && flipperCenterTarget != null)
        {
            if (caughtPenguinCol != null)
            {
                caughtPenguinCol.enabled = true;
            }
            caughtPenguinRb.isKinematic = false;

            Vector3 throwDir = (flipperCenterTarget.position - caughtPenguin.position).normalized;
            caughtPenguinRb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        }

        caughtPenguin = null;
        caughtPenguinRb = null;
        caughtPenguinCol = null;
    }

    private void ReleasePenguin()
    {
        if (caughtPenguinCol != null)
        {
            caughtPenguinCol.enabled = true;
        }
        if (caughtPenguinRb != null)
        {
            caughtPenguinRb.isKinematic = false;
        }
        caughtPenguin = null;
        caughtPenguinRb = null;
        caughtPenguinCol = null;
    }

    // ★ [핵심 수정] transform.root를 사용하지 않고 Rigidbody가 붙은 공만 잡도록 수정
    private Transform FindNearestBall(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;

            // 콜라이더 자체 또는 리짓바디 오브젝트의 태그가 "Ball"인지 검사
            bool isBall = hit.CompareTag("Ball") || (rb != null && rb.CompareTag("Ball"));

            if (isBall)
            {
                // root(최상위 부모)가 아니라, Rigidbody가 붙어있는 실제 공 Transform을 타겟으로 지정
                Transform targetTransform = (rb != null) ? rb.transform : hit.transform;
                
                float dist = Vector3.Distance(transform.position, targetTransform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = targetTransform;
                }
            }
        }
        return nearest;
    }

    private void OnReachPathEnd()
    {
        UpdateAnimation(false);
        Destroy(gameObject);
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (animator == null) return;
        animator.SetBool(IsMovingHash, isMoving);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}