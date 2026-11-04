using UnityEngine;

public class EnemyPathFollowState : EnemyStateBase
{
    public EnemyPathFollowState(NormalEnemyAI enemy, EnemyStateMachine stateMachine) 
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        enemy.UpdateAnimation(true);
    }

    public override void Update()
    {
        CatmullRomPath path = enemy.CurrentPath;
        if (path == null || !path.IsValid)
        {
            enemy.UpdateAnimation(false);
            return;
        }

        int segmentCount = path.SegmentCount;
        if (segmentCount <= 0)
        {
            enemy.UpdateAnimation(false);
            return;
        }

        // 0. 전방 차간 거리 기반 속도 배율 계산 (0.0: 완전 정지 ~ 1.0: 정상 속도)
        float speedFactor = CalculateSpeedFactor();

        // 1. 진행도(pathProgress) 갱신 (전방 여유 공간이 있을 때만 이동)
        if (speedFactor > 0.001f)
        {
            enemy.PathProgress += (enemy.MoveSpeed * speedFactor / segmentCount) * Time.deltaTime;
        }

        // 2. 경로 끝(베이스) 도착 검사
        if (enemy.PathProgress >= segmentCount)
        {
            enemy.PathProgress = segmentCount;
            enemy.OnReachPathEnd();
            return;
        }

        // 3. 현재 스플라인 위치 계산
        Vector3 pathPos = path.EvaluateAtProgress(enemy.PathProgress);

        // 4. 진행 방향 바라보기 (스플라인 접선 벡터 기반 부드러운 회전)
        Vector3 tangent = path.EvaluateTangentAtProgress(enemy.PathProgress);
        tangent.y = 0f;
        if (tangent.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(tangent.normalized);
            float maxTurnDegrees = enemy.RotationSpeed * 30f * Time.deltaTime;
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, targetRot, maxTurnDegrees);
        }

        // 5. 위치 동기화 및 애니메이션 갱신 (복귀 직후 1프레임 텔레포트 방지를 위한 부드러운 수렴)
        Vector3 currentPos = enemy.transform.position;
        float snapDist = Vector3.Distance(currentPos, pathPos);
        if (snapDist > 0.005f)
        {
            // 잔여 오차를 0.12초 이내에 매끄럽게 흡수 (초당 최소 MoveSpeed * 2.5f 배율)
            float convergenceSpeed = Mathf.Max(enemy.MoveSpeed * 2.5f, snapDist / 0.12f);
            enemy.transform.position = Vector3.MoveTowards(currentPos, pathPos, convergenceSpeed * Time.deltaTime);
        }
        else
        {
            enemy.transform.position = pathPos;
        }

        enemy.UpdateAnimation(speedFactor > 0.05f);
    }

    public override void OnCollisionEnter(Collision collision)
    {
        enemy.HandlePenguinCollision(collision);
    }

    public override void Exit()
    {
        enemy.UpdateAnimation(false);
    }

    /// <summary>
    /// 전방에 위치한 적과의 거리를 계산하여 물리 충돌(튕김 카오스) 없이 부드러운 차간 거리 유지를 위한 속도 배율(0.0 ~ 1.0)을 반환합니다.
    /// Zero GC-allocations hot-path 메서드입니다.
    /// </summary>
    public float CalculateSpeedFactor(System.Collections.Generic.IReadOnlyList<Enemy> customEnemyList = null)
    {
        // ── 런타임 O(1) 앞선 적(LeadingEnemy) 고속 경로 ──────────────────
        // EnemyManager의 1D Sweep and Prune 정렬을 통해 동일 경로상의 직전 적만 단일 O(1) 검사합니다.
        if (customEnemyList == null && enemy.CurrentPath != null && enemy.CurrentPath.IsValid)
        {
            NormalEnemyAI lead = enemy.LeadingEnemy;
            if (lead == null || lead.IsDead || !lead.gameObject.activeInHierarchy)
            {
                return 1f;
            }

            Vector3 myPosition = enemy.transform.position;
            if (Mathf.Abs(lead.transform.position.y - myPosition.y) > 1.5f)
            {
                return 1f;
            }

            Vector3 toLead = lead.transform.position - myPosition;
            toLead.y = 0f;
            float leadDist = toLead.magnitude;

            if (leadDist < 0.05f)
            {
                return lead.GetInstanceID() < enemy.GetInstanceID() ? 0f : 1f;
            }

            float minFollow = enemy.MinFollowDistance;
            float slowFollow = enemy.SlowFollowDistance <= minFollow ? minFollow + 0.6f : enemy.SlowFollowDistance;

            if (leadDist <= minFollow) return 0f;
            if (leadDist >= slowFollow) return 1f;

            return (leadDist - minFollow) / (slowFollow - minFollow);
        }

        // ── Fallback 및 단위 테스트용 O(N) 순회 경로 ─────────────────────
        var allEnemies = customEnemyList ?? (EnemyManager.Instance != null ? EnemyManager.Instance.ActiveEnemies : null);
        if (allEnemies == null || allEnemies.Count <= 1) return 1f;

        Vector3 myPos = enemy.transform.position;

        // 이동 기준 진행 방향 벡터 산출 (스플라인 접선 우선, 없을 시 transform.forward)
        Vector3 forwardDir;
        if (enemy.CurrentPath != null && enemy.CurrentPath.IsValid)
        {
            forwardDir = enemy.CurrentPath.EvaluateTangentAtProgress(enemy.PathProgress);
            forwardDir.y = 0f;
        }
        else
        {
            forwardDir = enemy.transform.forward;
            forwardDir.y = 0f;
        }

        if (forwardDir.sqrMagnitude > 0.001f)
        {
            forwardDir.Normalize();
        }
        else
        {
            forwardDir = Vector3.forward;
        }

        float minFollowDist = enemy.MinFollowDistance;
        float slowFollowDist = enemy.SlowFollowDistance;
        if (slowFollowDist <= minFollowDist)
        {
            slowFollowDist = minFollowDist + 0.6f;
        }
        float slowDistSqr = slowFollowDist * slowFollowDist;

        float lowestSpeedFactor = 1f;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            Enemy other = allEnemies[i];
            if (other == null || other == enemy || other.IsDead || !other.gameObject.activeInHierarchy)
                continue;

            // 공중 적 또는 Y축 높이 차이가 큰 적은 차간 거리 판정에서 제외
            if (Mathf.Abs(other.transform.position.y - myPos.y) > 1.5f)
                continue;

            Vector3 toOther = other.transform.position - myPos;
            toOther.y = 0f; // 2D X-Z 평면

            float sqrDist = toOther.sqrMagnitude;
            if (sqrDist > slowDistSqr)
                continue;

            float dist = Mathf.Sqrt(sqrDist);

            // 동일 위치에 완전히 겹쳐 스폰된 경우의 우선순위 판정 (ID 기반 결정론적 분리)
            if (dist < 0.05f)
            {
                if (other.GetInstanceID() < enemy.GetInstanceID())
                {
                    return 0f; // 상대방에게 우선권 양보하고 정지
                }
                continue; // 내가 우선권 가짐
            }

            // 전방 부채꼴 (각도 약 140도: Dot > 0.25f) 안에 있는 적만 감지
            float dot = Vector3.Dot(forwardDir, toOther / dist);
            if (dot <= 0.25f)
                continue; // 내 뒤에 있거나 옆을 지나가는 적은 무시

            // 안전거리 이내면 즉시 완전 정지
            if (dist <= minFollowDist)
            {
                return 0f;
            }

            // minFollowDist ~ slowFollowDist 사이 부드러운 감속 보간
            float factor = (dist - minFollowDist) / (slowFollowDist - minFollowDist);
            if (factor < lowestSpeedFactor)
            {
                lowestSpeedFactor = factor;
            }
        }

        return lowestSpeedFactor;
    }
}
