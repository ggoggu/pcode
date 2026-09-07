using UnityEngine;

public class EnemyStunState : EnemyStateBase
{
    private float stunDuration;
    private float timer = 0f;

    public EnemyStunState(NormalEnemyAI enemy, EnemyStateMachine stateMachine, float duration)
        : base(enemy, stateMachine)
    {
        this.stunDuration = Mathf.Max(0.1f, duration);
    }

    public override void Enter()
    {
        enemy.UpdateAnimation(false);
        timer = 0f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= stunDuration)
        {
            // 스턴 종료 시 스플라인 복귀 상태 또는 경로 이동 상태로 전환
            stateMachine.ChangeState(new EnemyReturnToSplineState(enemy, stateMachine));
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        // 스턴 중 추가 충돌 처리 (동적 넉백 적용)
        enemy.HandlePenguinCollision(collision, 0.7f);
    }
}
