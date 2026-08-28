using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(PenguinController))]
    public sealed class PenguinCollisionHandler : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        [Header("Plane Reference")]
        [SerializeField] private Transform movementPlane;

        private PenguinController selfPenguin;

        private void Awake()
        {
            // 동일 오브젝트의 PenguinController 참조
            selfPenguin = GetComponent<PenguinController>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 상대방이 'PenguinController'를 가지고 있는지 확인
            if (!collision.gameObject.TryGetComponent<PenguinController>(out var otherPenguin))
            {
                return;
            }

            // 펭귄 vs 펭귄 충돌 반발 처리
            ApplyPenguinBounce(otherPenguin, collision);
        }

        private void ApplyPenguinBounce(PenguinController other, Collision c)
        {
            if (c.contactCount == 0) return;

            Rigidbody rb = selfPenguin.Body;
            Rigidbody otherRb = other.Body;
            if (rb == null) return;

            Vector3 boardNormal = movementPlane != null ? movementPlane.up : Vector3.up;

            // 1. 밀려날 방향 계산 (상대방 위치 -> 내 위치)
            ContactPoint contact = c.GetContact(0);
            Vector3 pushDirection = Vector3.ProjectOnPlane(contact.normal, boardNormal);

            // 두 펭귄의 위치가 완전히 겹친 경우 충돌 법선(Normal)을 대체 사용
            if (pushDirection.sqrMagnitude < 0.0001f)
            {
                pushDirection = Vector3.ProjectOnPlane(transform.position - other.transform.position, boardNormal);
            }

            if (pushDirection.sqrMagnitude < 0.0001f) return;

            pushDirection.Normalize();

            Vector3 relativeVelocity = rb.linearVelocity - otherRb.linearVelocity;
            float impactSpeed = Vector3.Dot(-relativeVelocity, pushDirection);

            // 2. 속도 계산 (현재 내 속도 기반으로 보장 및 증폭)
            float baseSpeed = Mathf.Max(rb.linearVelocity.magnitude, impactSpeed);
            float calculatedSpeed = Mathf.Max(baseSpeed * physicsConfig.penguinSpeedMultiplier, physicsConfig.penguinMinimumSpeed);
            float finalSpeed = Mathf.Clamp(calculatedSpeed, physicsConfig.penguinMinimumSpeed, selfPenguin.CurrentMaxSpeed);


            // 3. 내 Rigidbody 속도 적용 (상대방은 상대방 자신의 OnCollisionEnter에서 처리함)
            rb.linearVelocity = pushDirection * finalSpeed;
        }
    }
}