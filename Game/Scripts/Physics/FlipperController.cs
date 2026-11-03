using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(HingeJoint))]
    public sealed class FlipperController : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;
        [SerializeField] private Transform movementPlane;

        [Header("Board Direction Settings")]
        [Tooltip("보드의 하단(Down/Drain) 방향 기준 축을 설정합니다. (기본값: +Z)")]
        [SerializeField] private BoardDownAxis boardDownAxis = BoardDownAxis.PositiveZ;

        [Header("Flipper Settings")]
        [SerializeField] private bool leftFlipper;

        [Tooltip("플리퍼로 타격 시 부여할 기본 상향 발사 속력")]
        [Min(0f)]
        [SerializeField] private float flipperHitSpeed = 18.0f;

        [Tooltip("펭귄의 진입 속도를 타격에 반영할 배율")]
        [Min(0f)]
        [SerializeField] private float incomingSpeedBonus = 0.5f;

        private HingeJoint hinge;
        private bool canControl = false;
        private bool isPressed = false;

        public bool IsPressed => isPressed;

        private void Awake()
        {
            hinge = GetComponent<HingeJoint>();
            hinge.useMotor = true;
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

            isPressed = pressed;
            ApplyMotor(pressed);
        }

        private void ApplyMotor(bool pressed)
        {
            var motor = hinge.motor;
            float sign = leftFlipper ? -1f : 1f;

            motor.targetVelocity = (pressed ? physicsConfig.flipperPressedVelocity : -physicsConfig.flipperRestVelocity) * sign;
            motor.force = physicsConfig.flipperMaxMotorForce;
            motor.freeSpin = false;
            hinge.motor = motor;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 플리퍼가 작동(스윙) 중일 때만 직접 상향 타격을 가합니다.
            // 플리퍼가 정지 상태일 때는 동일 오브젝트의 GuideWallSurface가 부드러운 슬라이딩을 처리합니다.
            if (!isPressed) return;
            if (!collision.collider.TryGetComponent<PenguinController>(out var penguin)) return;

            HandleFlipperHit(penguin, collision);
        }

        private void HandleFlipperHit(PenguinController penguin, Collision collision)
        {
            if (collision.contactCount == 0) return;

            Rigidbody penguinRb = penguin.Body;
            if (penguinRb == null) return;

            Vector3 boardNormal = GetBoardNormal();
            Vector3 boardUp = GetBoardUp();

            // 플리퍼의 윗면 방향(타격 상방) 계산
            ContactPoint contact = collision.GetContact(0);
            Vector3 contactNormal = Vector3.ProjectOnPlane(contact.normal, boardNormal).normalized;

            // 법선이 보드 하향(+Z)을 향하는 모서리 충돌인 경우 상향으로 강제 보정하여 하향 튕김을 원천 차단
            if (Vector3.Dot(contactNormal, boardUp) < 0.2f)
            {
                contactNormal = boardUp;
            }

            // 상향 방향과 접점 법선을 혼합하여 자연스러운 상향 발사 각도 형성
            Vector3 launchDir = (boardUp * 0.7f + contactNormal * 0.3f).normalized;

            // 펭귄 진입 속도 반영하여 최종 타격 속도 계산
            Vector3 inPlaneVelocity = Vector3.ProjectOnPlane(penguinRb.linearVelocity, boardNormal);
            float incomingSpeed = inPlaneVelocity.magnitude;
            float finalSpeed = flipperHitSpeed + (incomingSpeed * incomingSpeedBonus);
            finalSpeed = Mathf.Clamp(finalSpeed, flipperHitSpeed, penguin.CurrentMaxSpeed);

            penguinRb.linearVelocity = launchDir * finalSpeed;

            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(collision);
        }

        public Vector3 GetBoardNormal()
        {
            return movementPlane != null ? movementPlane.up : Vector3.up;
        }

        public Vector3 GetBoardDown()
        {
            Vector3 worldDown = (boardDownAxis == BoardDownAxis.PositiveZ) ? Vector3.forward : -Vector3.forward;
            if (movementPlane != null)
            {
                worldDown = (boardDownAxis == BoardDownAxis.PositiveZ) ? movementPlane.forward : -movementPlane.forward;
            }
            return Vector3.ProjectOnPlane(worldDown, GetBoardNormal()).normalized;
        }

        public Vector3 GetBoardUp()
        {
            return -GetBoardDown();
        }
    }
}