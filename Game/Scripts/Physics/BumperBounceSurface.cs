using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class BumperBounceSurface : PinballBounceSurface
    {
        [Header("Bumper Bounce Settings")]
        [Tooltip("펭귄의 진입 속도에 곱해지는 반발 배율 (1.0 = 속도 100% 보존, >1.0 = 가속 튕김)")]
        [Min(0f)]
        [SerializeField] private float bounceMultiplier = 1.15f;

        [Tooltip("최소 튕김 속도 (너무 느리게 충돌했을 때 멈추지 않고 튕겨 나가도록 보장)")]
        [Min(0f)]
        [SerializeField] private float minBumperSpeed = 6.0f;

        [Tooltip("충돌 시 고정적으로 더해지는 추가 추진력 (기본값: 0)")]
        [Min(0f)]
        [SerializeField] private float additiveForce = 0f;

        protected override float CalcFinalSpeed(Vector3 vWorldOut, PenguinController p)
        {
            // 1. 충돌 직전 펭귄의 보드 평면 상 진입 속력 계산
            Vector3 boardNormal = GetBoardNormal();
            Vector3 inPlaneVelocity = Vector3.ProjectOnPlane(p.Body.linearVelocity, boardNormal);
            float incomingSpeed = inPlaneVelocity.magnitude;

            // 2. 펭귄 진입 속도 기반 반발 속도 산출 (기본 반발력 + 진입 속도 * 배율)
            float calculatedSpeed = additiveForce + (incomingSpeed * bounceMultiplier);

            // 3. 최소 튕김 속도 보장 (너무 느리게 박아도 범퍼에 끼이거나 멈추지 않음)
            calculatedSpeed = Mathf.Max(calculatedSpeed, minBumperSpeed);

            // 4. 펭귄의 최대 속도 제한 적용 (무한 가속 및 맵 이탈 방지)
            return Mathf.Clamp(calculatedSpeed, minBumperSpeed, p.CurrentMaxSpeed);
        }

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(c);
        }
    }
}