using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 하단 벽 및 플리퍼 상단 등에서 펭귄이 강하게 튕기지 않고 
    /// 벽/표면을 따라 부드럽게 아래(플리퍼/드레인 방향)로 미끄러져 내려오도록 유도하는 표면 스크립트입니다.
    /// PinballBounceSurface를 상속받아 공통 충돌/게임 상태 수명주기를 완벽하게 공유합니다.
    /// </summary>
    public sealed class GuideWallSurface : PinballBounceSurface
    {
        [Header("Sliding Trigger Settings (속도 & 각도 조건)")]
        [Tooltip("이 속도(m/s) 이하로 충돌 시 정면 충돌이라도 튕기지 않고 아래로 매끄럽게 미끄러집니다.")]
        [Min(0f)]
        [SerializeField] private float slideSpeedThreshold = 5.0f;

        [Tooltip("벽면과 스치듯 들어왔다고 판정할 진입각 (법선 기준. 예: 55도 이상 = 스침)")]
        [Range(30f, 89f)]
        [SerializeField] private float glanceAngleThreshold = 55f;

        [Header("Sliding Speed Settings")]
        [Tooltip("슬라이딩 시 이전 속도 유지 비율 (1.0 = 감속 없음, 0.5 = 50% 감속)")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float speedRetention = 0.85f;

        [Tooltip("하향 슬라이딩 최소 속력")]
        [Min(0f)]
        [SerializeField] private float minSlideSpeed = 3.5f;

        [Tooltip("하향 슬라이딩 최대 속력 제한")]
        [Min(0f)]
        [SerializeField] private float maxSlideSpeed = 12.0f;

        [Header("Surface Type (벽 vs 플리퍼)")]
        [Tooltip("플리퍼 표면일 경우 체크하면 벽보다 더 부드럽고 느리게 미끄러지도록 추가 감속을 적용합니다.")]
        [SerializeField] private bool isFlipperSurface = false;

        [Tooltip("플리퍼 표면일 때 추가로 곱해지는 감속 계수")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float flipperSlowdownFactor = 0.65f;

        [Header("Head-on Collision (정면 고속 충돌 시)")]
        [Tooltip("고속/정면 충돌 등 슬라이딩 조건이 아닐 때 벽 밖으로 튕겨 나오는 반발력 계수")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float headOnRestitution = 0.25f;

        [Header("Impact Effect Options")]
        [Tooltip("하단 벽 충돌 시 콤보 및 어빌리티 환경 충돌을 트리거할지 여부")]
        [SerializeField] private bool triggerComboAndAbility = false;

        public float SlideSpeedThreshold => slideSpeedThreshold;
        public float GlanceAngleThreshold => glanceAngleThreshold;
        public float MinSlideSpeed => minSlideSpeed;
        public float MaxSlideSpeed => maxSlideSpeed;
        public float HeadOnRestitution => headOnRestitution;

        private FlipperController cachedFlipper;

        protected override bool UseContactPointAsOrigin => true;

        private void Awake()
        {
            cachedFlipper = GetComponent<FlipperController>();
        }

        protected override bool ShouldIgnoreCollision(PenguinController p)
        {
            // 플리퍼가 작동(스윙) 중일 때는 GuideWall 슬라이딩을 건너뛰고 FlipperController가 직접 타격을 처리합니다.
            if (cachedFlipper != null && cachedFlipper.IsPressed)
            {
                return true;
            }
            return false;
        }

        protected override void OnCollisionStay(Collision c)
        {
            // 플리퍼 스윙 중에는 지속 슬라이딩 무시
            if (cachedFlipper != null && cachedFlipper.IsPressed) return;
            if (!c.collider.TryGetComponent<PenguinController>(out var p)) return;

            // 펭귄이 벽 바깥쪽(법선 방향)으로 이미 튕겨 나가고 있다면 Stay 슬라이딩을 강제 적용하지 않음
            if (c.contactCount > 0)
            {
                Vector3 boardNormal = GetBoardNormal();
                Vector3 outwardN = -Vector3.ProjectOnPlane(c.GetContact(0).normal, boardNormal).normalized;
                Vector3 vInPlane = Vector3.ProjectOnPlane(p.Body.linearVelocity, boardNormal);
                if (Vector3.Dot(vInPlane, outwardN) > 0.1f) return; // 바깥으로 나가는 중이면 슬라이딩 유도 안 함
            }

            // 벽을 타고 내려가는 동안 지속적으로 하향 유도
            HandleCollisionResponse(c, isContinuous: true);
        }

        protected override Vector3 CalculateResponseVelocity(
            PenguinController p,
            Collision c,
            ContactPoint contact,
            Vector3 boardNormal,
            Vector3 originPos,
            bool isContinuous)
        {
            float retention = isFlipperSurface ? (speedRetention * flipperSlowdownFactor) : speedRetention;
            float maxSpeed = isFlipperSurface ? (maxSlideSpeed * flipperSlowdownFactor) : maxSlideSpeed;
            float minSpeed = isFlipperSurface ? Mathf.Min(minSlideSpeed, 2.0f) : minSlideSpeed;

            if (!isContinuous)
            {
                Vector3 pre = p.PreStepVelocity;
                Vector3 post = p.Body.linearVelocity;
                Vector3 rawN = contact.normal;
                Debug.Log(
                    $"[Enter] pre={pre} ({pre.magnitude:F2}) post={post} ({post.magnitude:F2}) " +
                    $"relV={c.relativeVelocity} rawN={rawN} " +
                    $"pre·rawN={Vector3.Dot(pre, rawN):F2} relV·rawN={Vector3.Dot(c.relativeVelocity, rawN):F2} " +
                    $"frame={Time.frameCount}");
            }

            return PinballPhysicsCalculator.CalculateSlideVelocity(
                p.PreStepVelocity,
                contact.normal,
                p.transform.position,
                originPos,
                boardNormal,
                GetBoardDown(),
                glanceAngleThreshold,
                slideSpeedThreshold,
                retention,
                minSpeed,
                maxSpeed,
                headOnRestitution,
                isContinuous
            );
        }

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            if (triggerComboAndAbility)
            {
                penguin.AddCombo();
                penguin.Ability.OnEnvironmentCollision(c);
            }
        }
    }
}
