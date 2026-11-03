using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class PinballPhysicsTests
    {
        [Test]
        public void CalculateSlideVelocity_LowSpeedHeadOnImpact_SlidesDownSmoothly()
        {
            // +Z가 아래(Down) 방향인 보드 평면 설정
            Vector3 boardNormal = Vector3.up;
            Vector3 boardDown = Vector3.forward; // +Z
            Vector3 wallNormal = -Vector3.right; // 벽 법선이 -X 방향

            // 1. 시속 3.0 m/s의 저속으로 벽에 정면 충돌 (X방향)
            Vector3 velocity = new Vector3(3.0f, 0f, 0f);
            Vector3 penguinPos = new Vector3(0f, 0f, 0f);
            Vector3 surfaceOriginPos = new Vector3(1f, 0f, 0f);

            float glanceAngleThreshold = 55f;
            float slideSpeedThreshold = 5.0f; // 5.0 이하이므로 저속 판정
            float speedRetention = 0.85f;
            float minSlideSpeed = 3.5f;
            float maxSlideSpeed = 12.0f;
            float headOnRestitution = 0.15f;

            Vector3 result = PinballPhysicsCalculator.CalculateSlideVelocity(
                velocity,
                wallNormal,
                penguinPos,
                surfaceOriginPos,
                boardNormal,
                boardDown,
                glanceAngleThreshold,
                slideSpeedThreshold,
                speedRetention,
                minSlideSpeed,
                maxSlideSpeed,
                headOnRestitution,
                isContinuous: false
            );

            // 결과 속도가 +Z(보드 하향) 방향 성분을 가지며, 최소 슬라이드 속도 이상이어야 함
            Assert.Greater(result.z, 0f, "저속 정면 충돌 슬라이딩 속도의 Z 성분은 +Z(하향)여야 합니다.");
            Assert.GreaterOrEqual(result.magnitude, minSlideSpeed, "슬라이딩 속력은 최소 슬라이드 속력 이상이어야 합니다.");
        }

        [Test]
        public void CalculateSlideVelocity_GlancingAngle_FollowsDownwardTangent()
        {
            Vector3 boardNormal = Vector3.up;
            Vector3 boardDown = Vector3.forward; // +Z
            Vector3 wallNormal = -Vector3.right; // 벽 법선이 -X 방향

            // 2. 얕은 각도(스치듯)로 +Z를 향해 진입 (속도: X=1, Z=8 -> 빗겨맞음)
            Vector3 velocity = new Vector3(1.0f, 0f, 8.0f);
            Vector3 penguinPos = Vector3.zero;
            Vector3 surfaceOriginPos = Vector3.right;

            float glanceAngleThreshold = 55f;
            float slideSpeedThreshold = 3.0f;
            float speedRetention = 0.85f;
            float minSlideSpeed = 3.5f;
            float maxSlideSpeed = 12.0f;
            float headOnRestitution = 0.15f;

            Vector3 result = PinballPhysicsCalculator.CalculateSlideVelocity(
                velocity,
                wallNormal,
                penguinPos,
                surfaceOriginPos,
                boardNormal,
                boardDown,
                glanceAngleThreshold,
                slideSpeedThreshold,
                speedRetention,
                minSlideSpeed,
                maxSlideSpeed,
                headOnRestitution,
                isContinuous: false
            );

            Assert.Greater(result.z, 0f, "빗겨맞은 충돌은 +Z(하향) 방향으로 슬라이딩해야 합니다.");
            Assert.AreEqual(8.0f * speedRetention, result.magnitude, 0.5f, "슬라이딩 속도는 보존율이 곱해져야 합니다.");
        }

        [Test]
        public void CalculateSlideVelocity_HighSpeedHeadOnImpact_BouncesAwayFromWall()
        {
            Vector3 boardNormal = Vector3.up;
            Vector3 boardDown = Vector3.forward; // +Z
            Vector3 wallNormal = -Vector3.right; // 벽 법선이 -X 방향 (벽 밖으로 튕겨나가는 방향)

            // 시속 10.0 m/s의 고속으로 벽에 수직 정면 충돌 (+X 방향)
            Vector3 velocity = new Vector3(10.0f, 0f, 0f);
            Vector3 penguinPos = Vector3.zero;
            Vector3 surfaceOriginPos = Vector3.right;

            float glanceAngleThreshold = 55f;
            float slideSpeedThreshold = 4.0f;
            float speedRetention = 0.85f;
            float minSlideSpeed = 3.5f;
            float maxSlideSpeed = 12.0f;
            float headOnRestitution = 0.3f;

            Vector3 result = PinballPhysicsCalculator.CalculateSlideVelocity(
                velocity,
                wallNormal,
                penguinPos,
                surfaceOriginPos,
                boardNormal,
                boardDown,
                glanceAngleThreshold,
                slideSpeedThreshold,
                speedRetention,
                minSlideSpeed,
                maxSlideSpeed,
                headOnRestitution,
                isContinuous: false
            );

            // 고속 정면 충돌은 벽을 따라 아래로 흐르지 않고, 벽 법선 방향(-X)으로 튕겨 나와야 함
            Assert.Less(result.x, 0f, "고속 정면 충돌 시 벽 바깥쪽(-X)으로 튕겨 나와야 합니다.");
            Assert.AreEqual(0f, result.z, 0.1f, "정면 충돌 시 벽 하향(+Z)으로 쏠리지 않아야 합니다.");
        }

        [Test]
        public void CalculateSlideVelocity_UpwardMovingPenguin_DoesNotForceDownwardSlide()
        {
            Vector3 boardNormal = Vector3.up;
            Vector3 boardDown = Vector3.forward; // +Z (하향)
            Vector3 wallNormal = -Vector3.right; // 벽 법선이 -X

            // 상향(-Z)으로 쏘아 올려져 벽에 충돌한 경우 (속도: X=3, Z=-7)
            Vector3 velocity = new Vector3(3.0f, 0f, -7.0f);
            Vector3 penguinPos = Vector3.zero;
            Vector3 surfaceOriginPos = Vector3.right;

            float glanceAngleThreshold = 55f;
            float slideSpeedThreshold = 3.0f;
            float speedRetention = 0.85f;
            float minSlideSpeed = 3.5f;
            float maxSlideSpeed = 12.0f;
            float headOnRestitution = 0.25f;

            Vector3 result = PinballPhysicsCalculator.CalculateSlideVelocity(
                velocity,
                wallNormal,
                penguinPos,
                surfaceOriginPos,
                boardNormal,
                boardDown,
                glanceAngleThreshold,
                slideSpeedThreshold,
                speedRetention,
                minSlideSpeed,
                maxSlideSpeed,
                headOnRestitution,
                isContinuous: false
            );

            // 위로 올라가던 공은 아래로 강제 역주행하지 않고, 계속 상향(-Z) 성분을 유지하며 반사되어야 함
            Assert.Less(result.z, 0f, "상향 이동 중 충돌 시 반사 속도의 Z 성분은 계속 상향(-Z)이어야 합니다.");
            Assert.Less(result.x, 0f, "벽 법선(-X)으로 반사되어야 합니다.");
        }
    }
}
