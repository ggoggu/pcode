using UnityEngine;
namespace PenguinPinball.Core
{
    [RequireComponent(typeof(HingeJoint))]
    public sealed class FlipperController:MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        [SerializeField] bool leftFlipper;
        HingeJoint hinge;
        private bool canControl = false;

        void Awake()
        {
            hinge=GetComponent<HingeJoint>();
            hinge.useMotor=true;
        }

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
            canControl = (newState == GameState.WaveInProgress);
            ApplyMotor(false);

            if (!canControl)
            {
                SetPressed(false);
            }
        }

        public void SetPressed(bool pressed)
        {
            if (!canControl) return;

            ApplyMotor(pressed);
        }

        private void ApplyMotor(bool pressed)
        {
            var motor = hinge.motor;
            float sign = leftFlipper ? -1f : 1f;

            motor.targetVelocity = (pressed ? physicsConfig.flipperPressedVelocity : - physicsConfig.flipperRestVelocity) * sign;
            motor.force = physicsConfig.flipperMaxMotorForce;
            motor.freeSpin = false;
            hinge.motor = motor;
        }
    }
}