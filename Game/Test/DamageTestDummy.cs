using System;
using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 기획자 및 개발자용 샌드백/허수아비 적 더미.
    /// NormalEnemyAI 및 Enemy를 상속받아 모든 적 관련 시스템(피격, 콤보, 능력 충돌, 색상 플래시)과 호환되면서도
    /// 경로 이동 및 넉백(밀려남) 없이 제자리에 완벽히 고정됩니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageTestDummy : NormalEnemyAI
    {
        [Header("Dummy Settings")]
        [Tooltip("체력이 0이 되어도 죽지 않고 유지할지 여부")]
        [SerializeField] private bool isInvincible = true;

        [Tooltip("대미지 입을 때 콘솔에 로그 출력 여부")]
        [SerializeField] private bool logDamageToConsole = false;

        [Tooltip("Scene/Game 뷰에 대미지 정보 오버레이 표시 여부")]
        [SerializeField] private bool showOverlayGUI = true;

        [Header("Statistics (Read Only)")]
        [SerializeField] private int hitCount = 0;
        [SerializeField] private float totalDamageReceived = 0f;
        [SerializeField] private float lastDamageReceived = 0f;
        [SerializeField] private float calculatedDPS = 0f;

        public int HitCount => hitCount;
        public float TotalDamageReceived => totalDamageReceived;
        public float LastDamageReceived => lastDamageReceived;
        public float DPS => calculatedDPS;
        public bool IsInvincible
        {
            get => isInvincible;
            set => isInvincible = value;
        }

        private float firstHitTime = -1f;
        private float lastHitTime = -1f;

        public event Action<DamageTestDummy, float> OnDummyDamaged;

        protected override void Awake()
        {
            base.Awake();

            // 1. 물리 넉백 방지를 위해 Kinematic 고정 및 위치/회전 제약
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            // 2. 펭귄 튕김 및 능력 충돌 처리를 위한 EnemyBounceSurface 확보
            if (!TryGetComponent<EnemyBounceSurface>(out _))
            {
                gameObject.AddComponent<EnemyBounceSurface>();
            }
        }

        protected override void Start()
        {
            // 부모의 Start 및 경로 탐색/이동 시작 비활성화
        }

        protected override void Update()
        {
            // 부모의 FSM 상태 머신 업데이트 및 경로 이동 차단
        }

        protected override void FixedUpdate()
        {
            // 부모의 물리 업데이트 차단
        }

        #region Knockback & Movement Overrides (제자리 고정)
        /// <summary>
        /// 펭귄 충돌 시 피격 색상 플래시는 발생시키되 넉백은 가하지 않습니다.
        /// </summary>
        public override void HandlePenguinCollision(Collision collision, float impulseScale = 1.0f)
        {
            FlashHitColor();
        }

        public override void ApplyKnockback(Vector3 knockbackDir, float impulse)
        {
            FlashHitColor();
        }

        public override void TriggerKnockback(Vector3 knockbackDir, float impulse)
        {
            FlashHitColor();
        }

        public override void MoveAlongPath() { }
        public override void SetPath(CatmullRomPath path) { }
        #endregion

        #region Damage & Statistics Logic
        public override void TakeDamage(float value)
        {
            if (value <= 0f) return;

            hitCount++;
            totalDamageReceived += value;
            lastDamageReceived = value;

            float now = Time.time;
            if (firstHitTime < 0f)
            {
                firstHitTime = now;
            }
            lastHitTime = now;

            float duration = Mathf.Max(0.1f, lastHitTime - firstHitTime);
            calculatedDPS = totalDamageReceived / duration;

            FlashHitColor();

            if (logDamageToConsole)
            {
                Debug.Log($"[DamageTestDummy] Hit #{hitCount} | Damage: {value:F1} | Total: {totalDamageReceived:F1} | DPS: {calculatedDPS:F1}", this);
            }

            OnDummyDamaged?.Invoke(this, value);

            if (isInvincible)
            {
                base.TakeDamage(value);
                if (IsDead || CurrentHealth <= 0f)
                {
                    ResetHealth();
                }
            }
            else
            {
                base.TakeDamage(value);
            }
        }

        protected override void Die()
        {
            if (isInvincible)
            {
                ResetHealth();
                return;
            }

            base.Die();
        }

        public void ResetStats()
        {
            hitCount = 0;
            totalDamageReceived = 0f;
            lastDamageReceived = 0f;
            calculatedDPS = 0f;
            firstHitTime = -1f;
            lastHitTime = -1f;
            ResetHealth();
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showOverlayGUI) return;

            Vector3 labelPos = transform.position + Vector3.up * 1.5f;
            string text = $"[Dummy: {gameObject.name}]\n" +
                          $"HP: {CurrentHealth:F0}/{MaxHealth:F0}\n" +
                          $"Hits: {hitCount} | Last: {lastDamageReceived:F1}\n" +
                          $"Total: {totalDamageReceived:F1} | DPS: {calculatedDPS:F1}";

            UnityEditor.Handles.Label(labelPos, text, new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.yellow }
            });
        }
#endif
    }
}

