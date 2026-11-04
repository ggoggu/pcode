using UnityEngine;

public class EnemyReturnToSplineState : EnemyStateBase
{
    private const float RejoinDistanceThreshold = 0.15f; // 스플라인 선과의 복귀 완료 판정 거리 (m)
    private const float BlendDistance = 1.5f;           // 회전/조향 블렌딩 기준 거리 (스플라인까지의 거리)

    private float stateTimer = 0f;
    private float maxReturnTimeout = 3.5f;             // 복귀 상태 안전 타이머 (거리/속도 비례 동적 산출)

    public EnemyReturnToSplineState(NormalEnemyAI enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        enemy.UpdateAnimation(true);
        stateTimer = 0f;

        CatmullRomPath path = enemy.CurrentPath;
        if (path != null && path.IsValid)
        {
            // 튕겨난 현재 위치에서 전체 스플라인 중 가장 가까운 점과 진행도를 즉시 탐색하여 동기화
            if (path.GetClosestPointAndProgress(enemy.transform.position, out Vector3 closestPoint, out float closestProgress, -1f))
            {
                enemy.PathProgress = closestProgress;

                // 적 이동 속도와 튕겨난 거리를 고려한 동적 타임아웃 계산 (저속 적의 조기 타임아웃 강제 스냅 방지)
                float initDist = Vector3.Distance(enemy.transform.position, closestPoint);
                float returnSpeed = enemy.MoveSpeed * Mathf.Max(1.2f, enemy.ReturnToSplineSpeedMultiplier);
                maxReturnTimeout = Mathf.Clamp((initDist / Mathf.Max(0.4f, returnSpeed)) * 1.8f + 1.2f, 2.5f, 6.0f);
            }
        }

        // 진입 시 스플라인과 이미 거의 겹쳐있는 경우에만 즉시 복귀 (강제 스냅 없이 부드럽게 전이)
        CheckAndRejoin(enemy.transform.position);
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        CatmullRomPath path = enemy.CurrentPath;
        if (path == null || !path.IsValid)
        {
            stateMachine.ChangeState(enemy.PathFollowState);
            return;
        }

        int segmentCount = path.SegmentCount;
        if (segmentCount <= 0)
        {
            stateMachine.ChangeState(enemy.PathFollowState);
            return;
        }

        Vector3 currentPos = enemy.transform.position;

        // 1. 튕겨난 현재 위치에서 전체 스플라인 상의 최근접점 및 진행도 계산 (직전 진행도 기반 국소 세그먼트 탐색: Wang et al., 2002)
        if (!path.GetClosestPointAndProgress(currentPos, out Vector3 closestSplinePoint, out float closestProgress, enemy.PathProgress))
        {
            stateMachine.ChangeState(enemy.PathFollowState);
            return;
        }

        enemy.PathProgress = closestProgress;
        float distToSplineLine = Vector3.Distance(currentPos, closestSplinePoint);

        // 2. 복귀 완료 판정: 스플라인 선(최근접점)과의 거리가 임계치 이내이거나 타임아웃 도달 시
        if (distToSplineLine <= RejoinDistanceThreshold || stateTimer >= maxReturnTimeout)
        {
            enemy.PathProgress = Mathf.Min(closestProgress, (float)segmentCount - 0.001f);
            stateMachine.ChangeState(enemy.PathFollowState);
            return;
        }

        // 3. 스플라인 합류(Steering)를 위한 전방 타겟 지점 계산
        // 스플라인에 가까워질수록 전방 탐색 거리를 줄여(0.08~0.25) 스플라인과 평행하게 달리지 않고 자연스럽게 합류하도록 유도
        float normalizedDist = Mathf.Clamp01(distToSplineLine / BlendDistance);
        float adaptiveLookAhead = Mathf.Lerp(0.08f, 0.25f, normalizedDist);
        float steerProgress = Mathf.Min(closestProgress + adaptiveLookAhead, (float)segmentCount);
        Vector3 steerTargetPos = path.EvaluateAtProgress(steerProgress);

        // 거리 기반 적응형 복귀 타겟 블렌딩:
        // 스플라인에서 멀 때는 최단거리 지점(closestSplinePoint)으로 빠르게 접근하고,
        // 가까워질수록 전방 합류 지점(steerTargetPos)으로 부드럽게 전환
        float blendFactor = Mathf.Clamp01(1f - normalizedDist);
        Vector3 returnTarget = Vector3.Lerp(closestSplinePoint, steerTargetPos, blendFactor);

        // 4. 위치 이동: 산출된 복귀 타겟 지점을 향해 이동
        float returnSpeed = enemy.MoveSpeed * Mathf.Max(1.2f, enemy.ReturnToSplineSpeedMultiplier);
        Vector3 newPos = Vector3.MoveTowards(currentPos, returnTarget, returnSpeed * Time.deltaTime);
        enemy.transform.position = newPos;

        // 5. 회전 보간: 거리 비례 적응형 접선 블렌딩 (Distance-based Adaptive Tangent Blend)
        // 스플라인에서 멀 때는 이동 방향(moveDir), 스플라인에 가까워질수록 스플라인 진행 방향(splineTangent)으로 매끄럽게 수렴
        Vector3 moveDir = returnTarget - currentPos;
        moveDir.y = 0f;

        Vector3 splineTangent = path.EvaluateTangentAtProgress(steerProgress);
        splineTangent.y = 0f;

        // 비선형 스무딩 적용 (점진적인 각도 전환)
        float tangentWeight = Mathf.SmoothStep(0f, 1f, blendFactor);

        Vector3 finalLookDir = Vector3.Slerp(
            moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : splineTangent.normalized,
            splineTangent.normalized,
            tangentWeight
        );

        if (finalLookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(finalLookDir.normalized);
            // 일정 각속도(초당 maxDegrees)로 부드럽게 회전
            float maxTurnDegrees = enemy.RotationSpeed * 30f * Time.deltaTime;
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRot, maxTurnDegrees);
        }

        // 이동 후 다시 복귀 완료 여부 검사
        if (Vector3.Distance(newPos, closestSplinePoint) <= RejoinDistanceThreshold)
        {
            enemy.PathProgress = Mathf.Min(closestProgress, (float)segmentCount - 0.001f);
            stateMachine.ChangeState(enemy.PathFollowState);
        }
    }

    private void CheckAndRejoin(Vector3 pos)
    {
        CatmullRomPath path = enemy.CurrentPath;
        if (path == null || !path.IsValid) return;

        if (path.GetClosestPointAndProgress(pos, out Vector3 closestPoint, out float closestProgress, -1f))
        {
            if (Vector3.Distance(pos, closestPoint) <= RejoinDistanceThreshold)
            {
                enemy.PathProgress = closestProgress;
                stateMachine.ChangeState(enemy.PathFollowState);
            }
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        // 복귀 도중 다시 펭귄과 충돌한 경우: 동적 넉백 처리
        enemy.HandlePenguinCollision(collision);
    }
}
