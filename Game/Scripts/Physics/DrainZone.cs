using UnityEngine;
namespace PenguinPinball.Core
{
    [RequireComponent(typeof(Collider))] 
    public sealed class DrainZone:MonoBehaviour 
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        private bool canDrain = false;

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;

            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState newState)
        {
            canDrain = (newState == GameState.WaveInProgress);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!canDrain) return;

            if(other.TryGetComponent<PenguinController>(out var p) && p.IsInField)
                p.Drain(physicsConfig.drainRespawnMultiplier);
        } 
    } 
}