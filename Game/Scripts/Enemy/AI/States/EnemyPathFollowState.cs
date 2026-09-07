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

        // 1. 진행도(pathProgress) 갱신
        enemy.PathProgress += (enemy.MoveSpeed / segmentCount) * Time.deltaTime;

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

        // 5. 위치 동기화
        enemy.transform.position = pathPos;
        enemy.UpdateAnimation(true);
    }

    public override void OnCollisionEnter(Collision collision)
    {
        enemy.HandlePenguinCollision(collision);
    }

    public override void Exit()
    {
        enemy.UpdateAnimation(false);
    }
}
