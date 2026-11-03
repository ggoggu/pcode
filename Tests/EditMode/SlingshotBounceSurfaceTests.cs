using NUnit.Framework;
using PenguinPinball.Core;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class SlingshotBounceSurfaceTests
    {
        private GameObject slingshotGo;
        private SlingshotBounceSurface slingshot;
        private PinballPhysicsConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<PinballPhysicsConfig>();
            config.slingshotBarLength = 4.0f;
            config.slingshotRepulsionRatio = 0.6f; // 중앙 60% (±1.2m)
            config.slingshotLaunchForce = 25.0f;
            config.bounceMinimumSpeed = 7.0f;
            config.bounceRestitution = 0.95f;
            config.bounceFriction = 0.1f;

            slingshotGo = new GameObject("SlingshotTest");
            var col = slingshotGo.AddComponent<BoxCollider>();

            // 좌우 범퍼 생성 (z = -2, z = 2)
            var leftGo = new GameObject("LeftBumper");
            leftGo.transform.SetParent(slingshotGo.transform);
            leftGo.transform.localPosition = new Vector3(0f, 0f, -2f);

            var rightGo = new GameObject("RightBumper");
            rightGo.transform.SetParent(slingshotGo.transform);
            rightGo.transform.localPosition = new Vector3(0f, 0f, 2f);

            slingshot = slingshotGo.AddComponent<SlingshotBounceSurface>();

            // 리플렉션을 통해 private 필드(bumperLeft, bumperRight, physicsConfig) 바인딩
            var leftField = typeof(SlingshotBounceSurface).GetField("bumperLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            leftField.SetValue(slingshot, leftGo.transform);

            var rightField = typeof(SlingshotBounceSurface).GetField("bumperRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rightField.SetValue(slingshot, rightGo.transform);

            var configField = typeof(PinballBounceSurface).GetField("physicsConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(slingshot, config);
        }

        [TearDown]
        public void TearDown()
        {
            if (slingshotGo != null)
            {
                Object.DestroyImmediate(slingshotGo);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Slingshot_CenterPoint_IsRepulsionZone()
        {
            // 중앙점 (0, 0, 0)은 당연히 반발 영역
            Vector3 centerPoint = slingshotGo.transform.position;
            Assert.IsTrue(slingshot.IsRepulsionZone(centerPoint));
        }

        [Test]
        public void Slingshot_WithinRatioThreshold_IsRepulsionZone()
        {
            // barLength = 4.0, ratio = 0.6 => 반경 1.2m
            // 1.0m 위치 (중앙 60% 내부)
            Vector3 insidePoint = slingshotGo.transform.position + new Vector3(0f, 0f, 1.0f);
            Assert.IsTrue(slingshot.IsRepulsionZone(insidePoint));

            Vector3 insideNegative = slingshotGo.transform.position + new Vector3(0f, 0f, -1.0f);
            Assert.IsTrue(slingshot.IsRepulsionZone(insideNegative));
        }

        [Test]
        public void Slingshot_OutsideRatioThreshold_IsNonRepulsionZone()
        {
            // barLength = 4.0, ratio = 0.6 => 반경 1.2m
            // 1.5m, 1.9m 위치 (양 끝 20% 비반발 영역)
            Vector3 outsideRight = slingshotGo.transform.position + new Vector3(0f, 0f, 1.5f);
            Assert.IsFalse(slingshot.IsRepulsionZone(outsideRight));

            Vector3 outsideLeft = slingshotGo.transform.position + new Vector3(0f, 0f, -1.8f);
            Assert.IsFalse(slingshot.IsRepulsionZone(outsideLeft));
        }

        [Test]
        public void Slingshot_LaunchDirection_FollowsLocalLaunchDirection()
        {
            slingshot.LocalLaunchDirection = Vector3.right;
            Vector3 launchDir = slingshot.GetLaunchDirection();

            // 회전이 identity일 때 transform.right는 (1, 0, 0)
            Assert.AreEqual(1f, launchDir.x, 0.001f);
            Assert.AreEqual(0f, launchDir.y, 0.001f);
            Assert.AreEqual(0f, launchDir.z, 0.001f);
        }

        [Test]
        public void SlingshotViewModel_CalculateSpeed_ClampsBetweenMinAndMax()
        {
            var vm = new SlingshotViewModel(config);

            // 펭귄 최대 속도가 30일 때, launchForce 25 반환
            float speedNormal = vm.CalculateSpeed(30f);
            Assert.AreEqual(25.0f, speedNormal, 0.001f);

            // 펭귄 최대 속도가 20일 때, 20으로 클램프
            float speedClamped = vm.CalculateSpeed(20f);
            Assert.AreEqual(20.0f, speedClamped, 0.001f);
        }

        [Test]
        public void SlingshotViewModel_ColliderSize_MatchesTotalBarLength()
        {
            var vm = new SlingshotViewModel(config);
            Vector3 reportedSize = Vector3.zero;

            vm.OnColliderUpdated += (center, rot, size) => reportedSize = size;
            vm.UpdateTransforms(new Vector3(0, 0, -2), new Vector3(0, 0, 2), 0.5f, Vector3.right);

            // 박스 콜라이더 길이가 양 끝 범퍼를 포괄하는 totalBarLength(4.0f)와 일치해야 함
            Assert.AreEqual(config.slingshotBarLength, reportedSize.z, 0.001f);
        }
    }
}
