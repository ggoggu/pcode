using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(Collider))]
    public abstract class PinballBounceSurface : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] protected PinballPhysicsConfig physicsConfig;
        [SerializeField] private Transform movementPlane;

        private bool isWaveActive = false;

        protected virtual bool UseContactPointAsOrigin => false; // 스플라인 벽처럼 길거나 피벗이 멀리 있는 오브젝트는 true로 오버라이드

        protected virtual void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance != null)
                HandleStateChanged(GameManager.Instance.CurrentState);
        }

        protected virtual void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState newState)
        {
            isWaveActive = (newState == GameState.WaveInProgress);
        }

        private Vector3 GetSurfaceVelocity()
        {
            if (TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
                return rb.linearVelocity;
            if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                return agent.velocity;
            return Vector3.zero;
        }

        private void OnCollisionEnter(Collision c)
        {
            if (!isWaveActive) return;
            if (!c.collider.TryGetComponent<PenguinController>(out var p)) return;
            if (ShouldIgnoreCollision(p)) return;
            if (c.contactCount == 0) return;

            Rigidbody rb = p.Body;
            ContactPoint contact = c.GetContact(0);
            Vector3 boardNormal = movementPlane != null ? movementPlane.up : Vector3.up;
            Vector3 surfaceVel = GetSurfaceVelocity();

            Vector3 currentRelativeVel = c.relativeVelocity;
            Vector3 vInPlane = Vector3.ProjectOnPlane(currentRelativeVel, boardNormal);
            Vector3 nInPlane = Vector3.ProjectOnPlane(contact.normal, boardNormal);

            // 기준 위치 선택 (스플라인 등은 contact.point, 일반 오브젝트는 transform.position)
            Vector3 originPos = UseContactPointAsOrigin ? contact.point : transform.position;

            if (vInPlane.sqrMagnitude < 0.0001f || nInPlane.sqrMagnitude < 0.0001f)
            {
                Vector3 fallback = Vector3.ProjectOnPlane(p.transform.position - originPos, boardNormal);
                if (fallback.sqrMagnitude < 0.0001f) fallback = contact.normal;

                float fallbackSpeed = CalcFallbackSpeed(p);
                ApplyBounce(rb, fallback.normalized, fallbackSpeed);
                OnHitImpact(p, c);
                return;
            }

            nInPlane = nInPlane.normalized;
            Vector3 posDiff = p.transform.position - originPos;
            Vector3 dirToPenguin = Vector3.ProjectOnPlane(posDiff, boardNormal);

            if (dirToPenguin.sqrMagnitude > 0.0001f && Vector3.Dot(nInPlane, dirToPenguin.normalized) < 0f)
            {
                nInPlane = -nInPlane;
            }

            float approachSpeed = Mathf.Abs(Vector3.Dot(vInPlane, nInPlane));
            Vector3 vNormalVec = -nInPlane * approachSpeed;
            Vector3 vTangentVec = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(rb.linearVelocity - surfaceVel, nInPlane), boardNormal);
            Vector3 vRelOut = (-vNormalVec * physicsConfig.bounceRestitution) + (vTangentVec * (1f - physicsConfig.bounceFriction));

            if (Vector3.Dot(vRelOut, nInPlane) < 1f) vRelOut += nInPlane * 2f;

            Vector3 vWorldOut = surfaceVel + vRelOut;

            float finalSpeed = CalcFinalSpeed(vWorldOut, p);

            ApplyBounce(rb, vWorldOut, finalSpeed);
            OnHitImpact(p, c);
        }

        protected virtual float CalcFinalSpeed(Vector3 vWorldOut, PenguinController p)
        {
            float calculatedSpeed = Mathf.Max(vWorldOut.magnitude, physicsConfig.bounceMinimumSpeed) * physicsConfig.bounceSpeedMultiplier;
            return Mathf.Clamp(calculatedSpeed, physicsConfig.bounceMinimumSpeed, p.CurrentMaxSpeed);
        }

        protected virtual float CalcFallbackSpeed(PenguinController p)
        {
            float fallbackSpeed = Mathf.Clamp(
                physicsConfig.bounceMinimumSpeed * physicsConfig.bounceSpeedMultiplier,
                physicsConfig.bounceMinimumSpeed,
                p.CurrentMaxSpeed);
            return fallbackSpeed;
        }


        private void ApplyBounce(Rigidbody rb, Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            rb.linearVelocity = direction.normalized * speed;
        }

        protected virtual bool ShouldIgnoreCollision(PenguinController p) => false;
        protected abstract void OnHitImpact(PenguinController penguin, Collision c);
    }
}