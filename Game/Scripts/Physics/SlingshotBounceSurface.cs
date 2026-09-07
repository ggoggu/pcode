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

        private BoxCollider boxCollider;
        public SlingshotViewModel ViewModel { get; private set; }

        protected override bool UseContactPointAsOrigin => true;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            InitViewModel();
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

        protected override float CalcFinalSpeed(Vector3 vWorldOut, PenguinController p)
        {
            return ViewModel.CalculateSpeed(p.CurrentMaxSpeed);
        }

        protected override float CalcFallbackSpeed(PenguinController p)
        {
            return ViewModel.CalculateSpeed(p.CurrentMaxSpeed);
        }

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(c);

            ViewModel.TriggerShake();
        }

        private void RefreshCollider()
        {
            if (bumperLeft == null || bumperRight == null) return;

            float bumperRadius = 0f;
            Collider leftCol = bumperLeft.GetComponent<Collider>();
            if (leftCol != null) bumperRadius = leftCol.bounds.extents.x;

            // 범퍼의 localPosition 사용 (부모 Slingshot 기준)
            Vector3 leftLocal = bumperLeft.localPosition;
            Vector3 rightLocal = bumperRight.localPosition;
            Vector3 forwardLocal = Vector3.forward;

            ViewModel.UpdateTransforms(leftLocal, rightLocal, bumperRadius, forwardLocal);
        }

        private void ApplyColliderTransform(Vector3 localCenter, Quaternion localRot, Vector3 size)
        {
            // 루트 오브젝트의 position/rotation은 절대 건드리지 않음
            boxCollider.center = localCenter;
            boxCollider.size = size;
        }
    }
}