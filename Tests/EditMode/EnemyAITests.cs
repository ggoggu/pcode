using NUnit.Framework;
using UnityEngine;

namespace PenguinShot.Tests
{
    [TestFixture]
    public class EnemyAITests
    {
        private class DummyStateA : IEnemyState
        {
            public string StateName => "DummyA";
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }
            public int UpdateCount { get; private set; }

            public void Enter() => Entered = true;
            public void Update() => UpdateCount++;
            public void FixedUpdate() { }
            public void Exit() => Exited = true;
            public void OnCollisionEnter(Collision collision) { }
            public void OnTriggerEnter(Collider other) { }
        }

        private class DummyStateB : IEnemyState
        {
            public string StateName => "DummyB";
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }

            public void Enter() => Entered = true;
            public void Update() { }
            public void FixedUpdate() { }
            public void Exit() => Exited = true;
            public void OnCollisionEnter(Collision collision) { }
            public void OnTriggerEnter(Collider other) { }
        }

        [Test]
        public void EnemyStateMachine_TransitionsState_Correctly()
        {
            var sm = new EnemyStateMachine();
            var stateA = new DummyStateA();
            var stateB = new DummyStateB();

            IEnemyState prevStateReported = null;
            IEnemyState nextStateReported = null;
            sm.OnStateChanged += (prev, next) =>
            {
                prevStateReported = prev;
                nextStateReported = next;
            };

            sm.Initialize(stateA);
            Assert.AreEqual(stateA, sm.CurrentState);
            Assert.IsTrue(stateA.Entered);

            sm.Update();
            Assert.AreEqual(1, stateA.UpdateCount);

            sm.ChangeState(stateB);
            Assert.IsTrue(stateA.Exited);
            Assert.IsTrue(stateB.Entered);
            Assert.AreEqual(stateB, sm.CurrentState);
            Assert.AreEqual(stateA, sm.PreviousState);
            Assert.AreEqual(stateA, prevStateReported);
            Assert.AreEqual(stateB, nextStateReported);
        }

        [Test]
        public void CatmullRomPath_ClosestPointAndProgress_FindsAccuratePoint()
        {
            var go = new GameObject("TestPath");
            var path = go.AddComponent<CatmullRomPath>();

            var p0 = new GameObject("P0").transform;
            var p1 = new GameObject("P1").transform;
            var p2 = new GameObject("P2").transform;
            var p3 = new GameObject("P3").transform;

            p0.position = new Vector3(0f, 0f, 0f);
            p1.position = new Vector3(0f, 0f, 10f);
            p2.position = new Vector3(0f, 0f, 20f);
            p3.position = new Vector3(0f, 0f, 30f);

            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p0 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p1 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p2 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p3 });

            // 스플라인에서 X축으로 +2m 만큼 넉백되어 이탈한 위치 테스트
            Vector3 offSplinePos = new Vector3(2f, 0f, 15f);
            bool found = path.GetClosestPointAndProgress(offSplinePos, out Vector3 closestPoint, out float closestProgress);

            Assert.IsTrue(found);
            // X는 0에 가까워야 하고, Z는 15에 가까워야 함
            Assert.AreEqual(0f, closestPoint.x, 0.5f, "Closest point X should be near spline X (0)");
            Assert.AreEqual(15f, closestPoint.z, 1.0f, "Closest point Z should be near target Z (15)");
            Assert.IsTrue(closestProgress >= 0.8f && closestProgress <= 2.2f, "Closest progress should be near middle segment");

            // 접선 계산 검증
            Vector3 tangent = path.EvaluateTangentAtProgress(1.0f);
            Assert.AreEqual(1f, tangent.z, 0.1f, "Path tangent should point forward along +Z axis");

            Object.DestroyImmediate(p0.gameObject);
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
            Object.DestroyImmediate(p3.gameObject);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NormalEnemyAI_TriggerKnockback_EntersKnockbackState_AndUnfreezesRigidbody()
        {
            var go = new GameObject("TestEnemy");
            go.AddComponent<BoxCollider>();
            var rb = go.AddComponent<Rigidbody>();
            var enemy = go.AddComponent<NormalEnemyAI>();

            // 넉백 발동 시 Dynamic Rigidbody 전환 검증
            enemy.TriggerKnockback(Vector3.forward, 10f);

            Assert.IsInstanceOf<EnemyKnockbackState>(enemy.StateMachine.CurrentState);
            Assert.IsFalse(rb.isKinematic, "넉백 중에는 물리 시뮬레이션을 위해 isKinematic이 false여야 합니다.");
            Assert.IsTrue(enemy.IsKnockbacked);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemyKnockbackState_FixedUpdate_TransitionsToStun_WhenStopped()
        {
            var go = new GameObject("TestEnemy");
            go.AddComponent<BoxCollider>();
            var rb = go.AddComponent<Rigidbody>();
            var enemy = go.AddComponent<NormalEnemyAI>();

            enemy.TriggerKnockback(Vector3.forward, 5f);
            Assert.IsInstanceOf<EnemyKnockbackState>(enemy.StateMachine.CurrentState);

            // 속도가 0에 도달하고 최소 시간 경과 시 스턴 상태로 전이 및 Kinematic 복구 검증
            rb.linearVelocity = Vector3.zero;
            for (int i = 0; i < 15; i++)
            {
                enemy.StateMachine.FixedUpdate();
            }

            Assert.IsInstanceOf<EnemyStunState>(enemy.StateMachine.CurrentState);
            Assert.IsTrue(rb.isKinematic, "넉백 정지 후에는 스플라인/스턴 제어를 위해 isKinematic이 true로 복구되어야 합니다.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemyReturnToSplineState_RejoinsClosestPathPoint_FromKnockedPosition_NotHitPosition()
        {
            var pathGo = new GameObject("TestPath");
            var path = pathGo.AddComponent<CatmullRomPath>();

            var p0 = new GameObject("P0").transform;
            var p1 = new GameObject("P1").transform;
            var p2 = new GameObject("P2").transform;
            var p3 = new GameObject("P3").transform;

            p0.position = new Vector3(0f, 0f, 0f);
            p1.position = new Vector3(0f, 0f, 10f);
            p2.position = new Vector3(0f, 0f, 20f);
            p3.position = new Vector3(0f, 0f, 30f);

            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p0 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p1 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p2 });
            path.waypoints.Add(new CatmullRomPath.Waypoint { point = p3 });

            var enemyGo = new GameObject("TestEnemy");
            enemyGo.AddComponent<BoxCollider>();
            enemyGo.AddComponent<Rigidbody>();
            var enemy = enemyGo.AddComponent<NormalEnemyAI>();
            enemy.SetPath(path);

            // 1. 피격 당시 경로 진행도 0.5f (Z 약 5m 위치) 가정
            enemy.PathProgress = 0.5f;

            // 2. 넉백으로 멀리 튕겨나가 Z=22m (진행도 약 2.2 구간)로 이동
            enemy.transform.position = new Vector3(3f, 0f, 22f);

            // 3. 복귀 상태 진입
            enemy.StateMachine.ChangeState(new EnemyReturnToSplineState(enemy, enemy.StateMachine));

            // 검증: 복귀 시 이전 맞은 자리(0.5f)가 아닌 튕겨난 현재 위치(Z=22m)에서 가장 가까운 경로 진행도(~2.2f)로 갱신되어야 함
            Assert.IsTrue(enemy.PathProgress >= 2.0f && enemy.PathProgress <= 2.5f,
                $"PathProgress should be near knocked position (~2.2), but was {enemy.PathProgress}");

            Object.DestroyImmediate(p0.gameObject);
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
            Object.DestroyImmediate(p3.gameObject);
            Object.DestroyImmediate(pathGo);
            Object.DestroyImmediate(enemyGo);
        }
    }
}
