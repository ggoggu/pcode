using UnityEngine;

namespace PenguinPinball.Core
{
    public enum BoardDownAxis
    {
        PositiveZ, // +Z 축이 보드 하단(Down/Drain) 방향 (기본값)
        NegativeZ  // -Z 축이 보드 하단(Down/Drain) 방향
    }

    [RequireComponent(typeof(Collider))]
    public abstract class PinballBounceSurface : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] protected PinballPhysicsConfig physicsConfig;
        [SerializeField] protected Transform movementPlane;

        [Header("Board Direction Settings")]
        [Tooltip("보드의 하단(Down/Drain) 방향 기준 축을 설정합니다. (기본값: +Z)")]
        [SerializeField] protected BoardDownAxis boardDownAxis = BoardDownAxis.PositiveZ;

        private bool isWaveActive = false;

        protected virtual bool UseContactPointAsOrigin => false; // 스플라인 벽처럼 길거나 피벗이 멀리 있는 오브젝트는 true로 오버라이드
        public Transform MovementPlane => movementPlane;
        public PinballPhysicsConfig PhysicsConfig => physicsConfig;

        protected virtual void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance != null)
                HandleStateChanged(GameManager.Instance.CurrentState);
        }

        protected virtual void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState newState)
        {
            isWaveActive = (newState == GameState.WaveInProgress);
        }

        protected virtual void OnCollisionEnter(Collision c)
        {
            HandleCollisionResponse(c, isContinuous: false);
        }

        protected virtual void OnCollisionStay(Collision c)
        {
            // 기본 바운스 표면은 Stay 시 무시 (하위 클래스에서 필요 시 오버라이드)
        }

        protected void HandleCollisionResponse(Collision c, bool isContinuous)
        {
            if (!isWaveActive) return;
            if (!c.collider.TryGetComponent<PenguinController>(out var p)) return;
            if (ShouldIgnoreCollision(p)) return;
            if (c.contactCount == 0) return;

            Rigidbody rb = p.Body;
            ContactPoint contact = c.GetContact(0);
            Vector3 boardNormal = GetBoardNormal();
            Vector3 originPos = UseContactPointAsOrigin ? contact.point : transform.position;

            Vector3 finalVelocity = CalculateResponseVelocity(p, c, contact, boardNormal, originPos, isContinuous);

            ApplyVelocity(rb, finalVelocity);
            OnHitImpact(p, c);
        }

        /// <summary>
        /// 충돌 시 펭귄에 부여할 최종 속도 벡터를 계산합니다.
        /// 하위 클래스(Bumper, Slingshot, GuideWall 등)에서 목적에 맞게 오버라이드할 수 있습니다.
        /// </summary>
        protected virtual Vector3 CalculateResponseVelocity(
            PenguinController p,
            Collision c,
            ContactPoint contact,
            Vector3 boardNormal,
            Vector3 originPos,
            bool isContinuous)
        {
            Rigidbody rb = p.Body;
            Vector3 rawBounceVec = PinballPhysicsCalculator.CalculateBounceVelocity(
                rb.linearVelocity,
                contact.normal,
                p.transform.position,
                originPos,
                boardNormal,
                physicsConfig.bounceRestitution,
                physicsConfig.bounceFriction
            );

            float speed = CalcFinalSpeed(rawBounceVec, p);
            return rawBounceVec.normalized * speed;
        }

        protected virtual float CalcFinalSpeed(Vector3 vWorldOut, PenguinController p)
        {
            float calculatedSpeed = Mathf.Max(vWorldOut.magnitude, physicsConfig.bounceMinimumSpeed) * physicsConfig.bounceSpeedMultiplier;
            return Mathf.Clamp(calculatedSpeed, physicsConfig.bounceMinimumSpeed, p.CurrentMaxSpeed);
        }

        protected virtual float CalcFallbackSpeed(PenguinController p)
        {
            float fallbackSpeed = Mathf.Clamp(
                physicsConfig.bounceMinimumSpeed * physicsConfig.bounceSpeedMultiplier,
                physicsConfig.bounceMinimumSpeed,
                p.CurrentMaxSpeed);
            return fallbackSpeed;
        }

        protected void ApplyVelocity(Rigidbody rb, Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.0001f) return;
            rb.linearVelocity = velocity;
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

        protected virtual bool ShouldIgnoreCollision(PenguinController p) => false;
        protected abstract void OnHitImpact(PenguinController penguin, Collision c);
    }
}