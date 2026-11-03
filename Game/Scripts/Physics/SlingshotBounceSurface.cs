using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SlingshotBounceSurface : PinballBounceSurface
    {
        [Header("참조")]
        [SerializeField] private Transform bumperLeft;
        [SerializeField] private Transform bumperRight;
        [SerializeField] private SlingshotRubberView rubberView;

        [Header("설치 방향 (발사 방향)")]
        [Tooltip("슬링샷이 펭귄을 튕겨낼 로컬 발사 방향 (기본: Vector3.right)")]
        [SerializeField] private Vector3 localLaunchDirection = Vector3.right;

        private BoxCollider boxCollider;
        public SlingshotViewModel ViewModel { get; private set; }

        private bool lastHitWasRepulsion = false;

        protected override bool UseContactPointAsOrigin => true;

        public Transform BumperLeft => bumperLeft;
        public Transform BumperRight => bumperRight;
        public Vector3 LocalLaunchDirection
        {
            get => localLaunchDirection;
            set => localLaunchDirection = value;
        }

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            EnsureIntegratedCollider();
            InitViewModel();
        }

        private void EnsureIntegratedCollider()
        {
            // 자식 오브젝트의 독립 BumperBounceSurface 비활성화 (슬링샷 통합 물리 적용)
            var childBumpers = GetComponentsInChildren<BumperBounceSurface>(true);
            foreach (var b in childBumpers)
            {
                b.enabled = false;
            }

            // 자식 오브젝트의 개별 콜라이더 비활성화하여 루트 BoxCollider가 전체(반발+비반발)를 단일 관할
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
            }

            if (boxCollider != null)
            {
                var allColliders = GetComponentsInChildren<Collider>(true);
                foreach (var col in allColliders)
                {
                    if (col != boxCollider)
                    {
                        col.enabled = false;
                    }
                }
            }
        }

        private void InitViewModel()
        {
            if (ViewModel != null) return;

            // physicsConfig(부모 클래스의 Model)를 ViewModel로 전달
            ViewModel = new SlingshotViewModel(physicsConfig);

            if (rubberView != null)
            {
                rubberView.BindViewModel(ViewModel);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureIntegratedCollider();
            InitViewModel();
            ViewModel.OnColliderUpdated += ApplyColliderTransform;

            if (rubberView != null)
            {
                RefreshCollider();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (ViewModel != null)
            {
                ViewModel.OnColliderUpdated -= ApplyColliderTransform;
            }
        }

        private void Update()
        {
            RefreshCollider();
            ViewModel.Tick(Time.deltaTime);
        }

        /// <summary>
        /// [7-2 기획] 충돌 위치가 막대 중앙의 반발 영역인지 판정합니다.
        /// </summary>
        public bool IsRepulsionZone(Vector3 contactPoint)
        {
            float barLength = GetBarLength();
            float ratio = physicsConfig != null ? physicsConfig.slingshotRepulsionRatio : 0.6f;
            float halfRepulsion = (barLength * ratio) * 0.5f;

            Vector3 center;
            Vector3 barAxis;

            if (bumperLeft != null && bumperRight != null)
            {
                center = (bumperLeft.position + bumperRight.position) * 0.5f;
                Vector3 diff = bumperRight.position - bumperLeft.position;
                barAxis = diff.sqrMagnitude > 0.0001f ? diff.normalized : transform.forward;
            }
            else
            {
                center = transform.position;
                barAxis = GetBarAxis();
            }

            // 막대 중심으로부터의 길이 축 상 오프셋
            float offsetFromCenter = Mathf.Abs(Vector3.Dot(contactPoint - center, barAxis));
            return offsetFromCenter <= halfRepulsion;
        }

        public float GetBarLength()
        {
            if (physicsConfig != null && physicsConfig.slingshotBarLength > 0.01f)
            {
                return physicsConfig.slingshotBarLength;
            }

            if (bumperLeft != null && bumperRight != null)
            {
                return Vector3.Distance(bumperLeft.position, bumperRight.position);
            }

            return 4.0f;
        }

        public Vector3 GetLaunchDirection()
        {
            Vector3 worldLaunch = transform.TransformDirection(
                localLaunchDirection != Vector3.zero ? localLaunchDirection.normalized : Vector3.right
            );
            Vector3 boardNormal = GetBoardNormal();
            Vector3 projected = Vector3.ProjectOnPlane(worldLaunch, boardNormal);
            if (projected.sqrMagnitude < 0.0001f)
            {
                projected = transform.right;
            }
            return projected.normalized;
        }

        public Vector3 GetBarAxis()
        {
            if (bumperLeft != null && bumperRight != null)
            {
                Vector3 diff = bumperRight.position - bumperLeft.position;
                if (diff.sqrMagnitude > 0.0001f) return diff.normalized;
            }

            Vector3 boardNormal = GetBoardNormal();
            Vector3 launchDir = GetLaunchDirection();
            return Vector3.Cross(boardNormal, launchDir).normalized;
        }

        protected override Vector3 CalculateResponseVelocity(
            PenguinController p,
            Collision c,
            ContactPoint contact,
            Vector3 boardNormal,
            Vector3 originPos,
            bool isContinuous)
        {
            lastHitWasRepulsion = IsRepulsionZone(contact.point);

            if (lastHitWasRepulsion)
            {
                // [반발 영역]
                // 1. 튕겨내는 방향은 설치 방향이 결정한다. (접촉면/진입각/충돌위치 무관)
                Vector3 launchDir = GetLaunchDirection();

                // 2. 부여하는 임펄스 크기는 펭귄의 진입 속도와 무관한 고정값이다.
                float fixedSpeed = ViewModel != null 
                    ? ViewModel.CalculateSpeed(p.CurrentMaxSpeed) 
                    : (physicsConfig != null ? physicsConfig.slingshotLaunchForce : 25.0f);

                return launchDir * fixedSpeed;
            }
            else
            {
                // [비반발 영역]
                // 펭귄을 튕겨내지 않는다. 벽과 동일한 물리 효과만 적용한다 (반발 계수로 반사되어 감속, 부스트 없음)
                Vector3 rawBounceVec = PinballPhysicsCalculator.CalculateBounceVelocity(
                    p.Body.linearVelocity,
                    contact.normal,
                    p.transform.position,
                    originPos,
                    boardNormal,
                    physicsConfig != null ? physicsConfig.bounceRestitution : 0.95f,
                    physicsConfig != null ? physicsConfig.bounceFriction : 0.1f
                );

                return rawBounceVec;
            }
        }

        protected override float CalcFinalSpeed(Vector3 vWorldOut, PenguinController p)
        {
            return ViewModel != null 
                ? ViewModel.CalculateSpeed(p.CurrentMaxSpeed) 
                : (physicsConfig != null ? physicsConfig.slingshotLaunchForce : 25.0f);
        }

        protected override float CalcFallbackSpeed(PenguinController p)
        {
            return ViewModel != null 
                ? ViewModel.CalculateSpeed(p.CurrentMaxSpeed) 
                : (physicsConfig != null ? physicsConfig.slingshotLaunchForce : 25.0f);
        }

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            if (lastHitWasRepulsion)
            {
                // [반발 영역 충돌]
                // 1. 콤보 누적
                penguin.AddCombo();

                // 2. 오브젝트 강화 부적 및 환경 충돌 발동
                penguin.Ability.OnEnvironmentCollision(c);

                // 3. 고무줄/막대 탄성 진동 연출
                if (ViewModel != null)
                {
                    ViewModel.TriggerShake();
                }
            }
            else
            {
                // [비반발 영역 충돌]
                // 비반발 영역 충돌은 벽 충돌로 취급한다. 오브젝트 충돌이 아니므로 아래가 발생하지 않는다:
                // - 콤보가 누적되지 않는다.
                // - 오브젝트 강화 부적이 발동하지 않으며, 발동 조건의 충돌 횟수에도 카운트되지 않는다.
                // - 고무줄 탄성 진동 연출도 발생하지 않는다.
            }
        }

        private void RefreshCollider()
        {
            if (bumperLeft == null || bumperRight == null) return;

            float bumperRadius = 0f;
            Collider leftCol = bumperLeft.GetComponent<Collider>();
            if (leftCol != null) bumperRadius = leftCol.bounds.extents.x;

            Vector3 leftLocal = bumperLeft.localPosition;
            Vector3 rightLocal = bumperRight.localPosition;
            Vector3 forwardLocal = localLaunchDirection != Vector3.zero ? localLaunchDirection.normalized : Vector3.right;

            ViewModel.UpdateTransforms(leftLocal, rightLocal, bumperRadius, forwardLocal);
        }

        private void ApplyColliderTransform(Vector3 localCenter, Quaternion localRot, Vector3 size)
        {
            // 루트 오브젝트의 position/rotation은 절대 건드리지 않음
            if (boxCollider != null)
            {
                boxCollider.center = localCenter;
                boxCollider.size = size;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 launchDir = GetLaunchDirection();
            Vector3 barAxis = GetBarAxis();

            Vector3 center = (bumperLeft != null && bumperRight != null)
                ? (bumperLeft.position + bumperRight.position) * 0.5f
                : transform.position;

            float barLen = GetBarLength();
            float ratio = physicsConfig != null ? physicsConfig.slingshotRepulsionRatio : 0.6f;
            float halfRepulse = (barLen * ratio) * 0.5f;
            float halfTotal = barLen * 0.5f;

            // 1. 발사 방향 (청록색 화살표)
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(center, launchDir * 2f);

            // 2. 중앙 반발 영역 (초록색 선)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center - barAxis * halfRepulse, center + barAxis * halfRepulse);

            // 3. 양 끝 비반발 영역 (빨간색 선)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center - barAxis * halfTotal, center - barAxis * halfRepulse);
            Gizmos.DrawLine(center + barAxis * halfRepulse, center + barAxis * halfTotal);
        }
#endif
    }
}