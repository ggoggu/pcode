using UnityEngine;

public class EnemyReturnToSplineState : EnemyStateBase
{
    private const float RejoinDistanceThreshold = 0.15f; // 스플라인 선과의 복귀 완료 판정 거리 (m)
    private const float MaxReturnTimeout = 1.5f;        // 복귀 상태 최대 지속 시간 (안전 타이머)
    private const float LookAheadProgress = 0.35f;      // 복귀 시 전방 조향 거리 (완만한 합류 곡선)
    private const float BlendDistance = 1.5f;           // 회전 블렌딩 기준 거리 (스플라인까지의 거리)

    private float stateTimer = 0f;

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
            stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
            return;
        }

        int segmentCount = path.SegmentCount;
        if (segmentCount <= 0)
        {
            stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
            return;
        }

        Vector3 currentPos = enemy.transform.position;

        // 1. 튕겨난 현재 위치에서 전체 스플라인 상의 최근접점 및 진행도 계산 (전체 경로 탐색: searchHintProgress = -1f)
        if (!path.GetClosestPointAndProgress(currentPos, out Vector3 closestSplinePoint, out float closestProgress, -1f))
        {
            stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
            return;
        }

        float distToSplineLine = Vector3.Distance(currentPos, closestSplinePoint);

        // 2. 복귀 완료 판정: 스플라인 선(최근접점)과의 거리가 임계치 이내이거나 타임아웃 도달 시
        if (distToSplineLine <= RejoinDistanceThreshold || stateTimer >= MaxReturnTimeout)
        {
            enemy.PathProgress = closestProgress;
            stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
            return;
        }

        // 3. 스플라인 합류(Steering)를 위한 전방 타겟 지점 계산 (완만한 합류 곡선)
        float steerProgress = Mathf.Min(closestProgress + LookAheadProgress, (float)segmentCount);
        Vector3 steerTargetPos = path.EvaluateAtProgress(steerProgress);

        // 거리 기반 적응형 복귀 타겟 블렌딩:
        // 스플라인에서 멀리 튕겨났을 때는 튕겨난 위치에서 가장 가까운 스플라인 지점(closestSplinePoint)으로 최단거리 이동하고,
        // 스플라인에 가까워질수록 전방 합류 지점(steerTargetPos)으로 부드럽게 전환
        float blendFactor = Mathf.Clamp01(1f - (distToSplineLine / BlendDistance));
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
            enemy.PathProgress = closestProgress;
            stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
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
                stateMachine.ChangeState(new EnemyPathFollowState(enemy, stateMachine));
            }
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        // 복귀 도중 다시 펭귄과 충돌한 경우: 동적 넉백 처리
        enemy.HandlePenguinCollision(collision);
    }
}
