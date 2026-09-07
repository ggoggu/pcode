using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(fileName = "PinballPhysicsConfig", menuName = "PenguinPinball/Physics Config")]
    public class PinballPhysicsConfig : ScriptableObject
    {
        [Header("펭귄 간 충돌 (Penguin Collision)")]
        [Min(0f)] public float penguinMinimumSpeed = 8f;
        [Min(1f)] public float penguinSpeedMultiplier = 1.15f;

        [Header("펭귄 이동 및 끼임 방지 (Stuck Detection)")]
        [Tooltip("위치 변동을 검사할 시간 간격 (초)")]
        public float stuckCheckInterval = 1.0f;
        [Tooltip("지정 간격 동안 최소한 이동해야 하는 거리 (미터)")]
        public float minMoveThreshold = 0.1f;
        [Tooltip("발사 직후 끼임 감지를 유예할 시간 (초)")]
        public float launchGracePeriod = 1f;

        [Header("바운스 표면 (Bounce Surface)")]
        [Tooltip("반발계수 (법선 성분)")]
        public float bounceRestitution = 0.95f;
        [Tooltip("마찰계수 (접선 감쇄)")]
        public float bounceFriction = 0.1f;
        [Min(0f)] public float bounceMinimumSpeed = 7f;
        [Min(0f)] public float bounceSpeedMultiplier = 1.1f;

        [Header("슬링샷 (Slingshot)")]
        [Tooltip("충돌 시 고정적으로 부여할 부스트 속력")]
        [Min(0f)] public float slingshotLaunchForce = 25.0f;
        [Tooltip("박스 콜라이더 두께")]
        [Min(0.01f)] public float slingshotColliderThickness = 0.1f;
        [Tooltip("박스 콜라이더 높이")]
        [Min(0.1f)] public float slingshotColliderHeight = 2.0f;
        [Tooltip("충돌 시 순간적으로 뒤로 늘어나는 시각적 거리")]
        [Min(0f)] public float slingshotShakeDistance = 0.6f;
        [Tooltip("고무줄 탄성 진동 유지 시간 (초)")]
        [Min(0.01f)] public float slingshotShakeDuration = 0.25f;

        [Header("플리퍼 (Flipper)")]
        public float flipperPressedVelocity = 1500f;
        public float flipperRestVelocity = 900f;
        public float flipperMaxMotorForce = 100000f;

        [Header("발사대 (Launcher)")]
        [Tooltip("최소 드래그 거리 (이 미만은 발사 취소)")]
        [Min(0f)] public float launcherMinDragDistance = 0.5f;
        [Min(0.1f)] public float launcherMaxDragDistance = 4f;
        public float launcherMaxImpulse = 18f;
        public float minLaunchSpeed = 5f;                       // 모든 펭귄 공통 최소 발사 속도
        public float maxAimAngle = 45f;                         // 상방 기준 최대 허용 각도 (±45도)
        public float launchCooldown = 0.5f;                     // 발사 창구 공통 쿨타임 (초)

        [Header("드레인 존 (Drain Zone)")]
        [Range(1f, 3f)] public float drainRespawnMultiplier = 1f;

        private void OnValidate()
        {
            // 드래그 최대 거리가 최소 거리보다 작아지지 않도록 검증
            if (launcherMaxDragDistance < launcherMinDragDistance)
            {
                launcherMaxDragDistance = launcherMinDragDistance + 0.1f;
            }
        }
    }
}