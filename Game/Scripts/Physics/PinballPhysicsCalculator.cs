using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 핀볼 물리(바운스, 슬라이딩 유도, 접선/법선 평면 투영 등)의 순수 수학/물리 계산을 전담하는 유틸리티 클래스입니다.
    /// </summary>
    public static class PinballPhysicsCalculator
    {
        /// <summary>
        /// 일반 범퍼 / 벽 / 슬링샷 반발 속도 벡터를 계산합니다.
        /// </summary>
        public static Vector3 CalculateBounceVelocity(
            Vector3 currentVelocity,
            Vector3 contactNormal,
            Vector3 penguinPos,
            Vector3 surfaceOriginPos,
            Vector3 boardNormal,
            float restitution,
            float friction)
        {
            Vector3 vInPlane = Vector3.ProjectOnPlane(currentVelocity, boardNormal);
            Vector3 nInPlane = -Vector3.ProjectOnPlane(contactNormal, boardNormal);

            if (vInPlane.sqrMagnitude < 0.0001f || nInPlane.sqrMagnitude < 0.0001f)
            {
                Vector3 fallback = Vector3.ProjectOnPlane(penguinPos - surfaceOriginPos, boardNormal);
                if (fallback.sqrMagnitude < 0.0001f) fallback = -contactNormal; // 부호 주의
                return fallback.normalized;
            }

            nInPlane = nInPlane.normalized;

            // 입사 속도의 법선 성분과 접선 성분 분리 후 반발/마찰 적용
            float approachSpeed = Mathf.Abs(Vector3.Dot(vInPlane, nInPlane));
            Vector3 vNormalVec = -nInPlane * approachSpeed;
            Vector3 vTangentVec = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(currentVelocity, nInPlane), boardNormal);
            Vector3 vRelOut = (-vNormalVec * restitution) + (vTangentVec * (1f - friction));

            if (Vector3.Dot(vRelOut, nInPlane) < 1f)
            {
                vRelOut += nInPlane * 2f;
            }

            return vRelOut;
        }

        /// <summary>
        /// 하단 벽 및 플리퍼 표면을 따라 부드럽게 아래로 흘러내려가도록 유도하는 속도 벡터를 계산합니다.
        /// (1) 공이 하향으로 진행 중이거나 정지/저속 상태이고,
        /// (2) 진입 속도가 slideSpeedThreshold 이하이거나 진입각이 glanceAngleThreshold 이내(스치는 각도)인 경우 매끄러운 하향 슬라이딩을 적용합니다.
        /// (3) 고속 정면 충돌 또는 상향 이동 중 충돌인 경우, 벽을 따라 강제로 흐르지 않고 자연스럽게 반사(Bounce)됩니다.
        /// </summary>
        public static Vector3 CalculateSlideVelocity(
            Vector3 currentVelocity,
            Vector3 contactNormal,
            Vector3 penguinPos,
            Vector3 surfaceOriginPos,
            Vector3 boardNormal,
            Vector3 boardDown,
            float glanceAngleThreshold,
            float slideSpeedThreshold,
            float speedRetention,
            float minSlideSpeed,
            float maxSlideSpeed,
            float headOnRestitution,
            bool isContinuous)
        {
            Vector3 nInPlane = -Vector3.ProjectOnPlane(contactNormal, boardNormal);
            if (nInPlane.sqrMagnitude < 0.0001f)
            {
                nInPlane = Vector3.ProjectOnPlane(penguinPos - surfaceOriginPos, boardNormal);
            }
            if (nInPlane.sqrMagnitude < 0.0001f)
            {
                nInPlane = -boardDown;
            }
            nInPlane = nInPlane.normalized;

            Vector3 vInPlane = Vector3.ProjectOnPlane(currentVelocity, boardNormal);
            float currentSpeed = vInPlane.magnitude;

            // 보드 아래쪽을 향하는 벽면 접선(Downward Tangent) 계산
            Vector3 tangent = Vector3.Cross(boardNormal, nInPlane).normalized;
            Vector3 downwardTangent = Vector3.Dot(tangent, boardDown) >= 0 ? tangent : -tangent;
            float tangentDownSpeed = Vector3.Dot(vInPlane, downwardTangent);

            // 벽으로 접근하는 속도 (양수 = 접근, 음수 = 이탈)
            float normalSpeed = -Vector3.Dot(vInPlane, nInPlane);

            float normalImpactRatio = currentSpeed > 0.001f
                ? Mathf.Clamp01(normalSpeed / currentSpeed)
                : 0f;

            bool isMovingDownwardOrSlow = tangentDownSpeed >= -0.2f || currentSpeed <= slideSpeedThreshold;
            bool isLowSpeed = normalSpeed <= slideSpeedThreshold;   // 전체 속력 대신 법선 속도
            float glanceDotThreshold = Mathf.Cos(glanceAngleThreshold * Mathf.Deg2Rad);
            bool isGlancingAngle = normalImpactRatio < glanceDotThreshold;

            bool shouldSlide = isMovingDownwardOrSlow && (isLowSpeed || isGlancingAngle || isContinuous);

            Debug.Log($"[{(isContinuous ? "Stay" : "Enter")}] speed={currentSpeed:F2} normalSpeed={normalSpeed:F2} ratio={normalImpactRatio:F2} " +
                      $"tanDown={tangentDownSpeed:F2} low={isLowSpeed} glance={isGlancingAngle} slide={shouldSlide}");


            if (shouldSlide)
            {
                // [스치는 충돌 / 저속 충돌 / 하향 슬라이딩]
                float baseSpeed = (tangentDownSpeed > 0f) ? (tangentDownSpeed * speedRetention) : minSlideSpeed;
                float targetSpeed = Mathf.Clamp(Mathf.Max(baseSpeed, minSlideSpeed), minSlideSpeed, maxSlideSpeed);

                // 벽에 끼이거나 파고들지 않도록 미세한 외측 법선 성분 추가 (5%)
                Vector3 slideDir = (downwardTangent * 0.95f + nInPlane * 0.05f).normalized;
                return slideDir * targetSpeed;
            }
            else
            {
                // [고속 정면 충돌 / 상향 진입 / 비슬라이딩 충돌]
                // 벽을 따라 강제로 흐르지 않고 물리적 반사(Bounce)를 수행하여 벽 바깥으로 튕겨 나옵니다.
                return CalculateBounceVelocity(
                    currentVelocity,
                    contactNormal,
                    penguinPos,
                    surfaceOriginPos,
                    boardNormal,
                    restitution: headOnRestitution,
                    friction: 0.15f
                );
            }
        }
    }
}
