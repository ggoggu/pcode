using System;
using UnityEngine;

public class EnemyStateMachine
{
    public IEnemyState CurrentState { get; private set; }
    public IEnemyState PreviousState { get; private set; }

    public event Action<IEnemyState, IEnemyState> OnStateChanged;

    public void Initialize(IEnemyState startingState)
    {
        CurrentState = startingState;
        CurrentState?.Enter();
    }

    public void ChangeState(IEnemyState newState)
    {
        if (newState == null || CurrentState == newState) return;

        CurrentState?.Exit();
        PreviousState = CurrentState;
        CurrentState = newState;
        CurrentState?.Enter();

        OnStateChanged?.Invoke(PreviousState, CurrentState);
    }

    public void Update()
    {
        CurrentState?.Update();
    }

    public void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
    }

    public void OnCollisionEnter(Collision collision)
    {
        CurrentState?.OnCollisionEnter(collision);
    }

    public void OnTriggerEnter(Collider other)
    {
        CurrentState?.OnTriggerEnter(other);
    }
}
