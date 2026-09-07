using UnityEngine;

public abstract class EnemyStateBase : IEnemyState
{
    protected readonly NormalEnemyAI enemy;
    protected readonly EnemyStateMachine stateMachine;

    public virtual string StateName => GetType().Name;

    protected EnemyStateBase(NormalEnemyAI enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
    public virtual void OnCollisionEnter(Collision collision) { }
    public virtual void OnTriggerEnter(Collider other) { }
}
