using UnityEngine;

public interface IEnemyState
{
    string StateName { get; }
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
    void OnCollisionEnter(Collision collision);
    void OnTriggerEnter(Collider other);
}
