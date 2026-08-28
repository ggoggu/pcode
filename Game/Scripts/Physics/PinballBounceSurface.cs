using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(Collider))]
    public sealed class PinballBounceSurface : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        [SerializeField] private Transform movementPlane;

        private bool isWaveActive = false;
        private int ghostLayer;
        private bool isEnemy;
        private Enemy cachedEnemy;

        private void Awake()
        {
            ghostLayer = LayerMask.NameToLayer("GhostPenguin");
            // Enemy 컴포넌트 사전 캐싱
            isEnemy = TryGetComponent<Enemy>(out cachedEnemy);
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;

            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
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
            if (isEnemy && p.IsOverloaded && ghostLayer != -1 && p.gameObject.layer == ghostLayer) return;
            if (c.contactCount == 0) return;

            Rigidbody rb = p.Body;
            ContactPoint contact = c.GetContact(0);

            Vector3 boardNormal = movementPlane != null ? movementPlane.up : Vector3.up;
            Vector3 surfaceVel = GetSurfaceVelocity();

            Vector3 currentRelativeVel = c.relativeVelocity;
            Vector3 vInPlane = Vector3.ProjectOnPlane(currentRelativeVel, boardNormal);
            Vector3 nInPlane = Vector3.ProjectOnPlane(contact.normal, boardNormal);

            if (vInPlane.sqrMagnitude < 0.0001f || nInPlane.sqrMagnitude < 0.0001f)
            {
                Vector3 fallback = Vector3.ProjectOnPlane(p.transform.position - transform.position, boardNormal);
                if (fallback.sqrMagnitude < 0.0001f) fallback = nInPlane;
                if (fallback.sqrMagnitude < 0.0001f) fallback = transform.forward; // 최후의 보루

                Vector3 fallbackDir = fallback.normalized;
                float fallbackSpeed = Mathf.Clamp(physicsConfig.bounceMinimumSpeed * physicsConfig.bounceSpeedMultiplier, physicsConfig.bounceMinimumSpeed, p.CurrentMaxSpeed);

                ApplyBounce(rb, fallbackDir, fallbackSpeed);
                HandleHitImpact(p, c);
                return;
            }

            // 예외 검사 통과 후 안전하게 정규화
            nInPlane = nInPlane.normalized;

            Vector3 posDiff = p.transform.position - transform.position;
            Vector3 dirToPenguin = Vector3.ProjectOnPlane(posDiff, boardNormal);

            // 두 오브젝트 중심 위치가 완벽히 겹치지 않았을 때만 위치 기반 방향 검사 수행
            if (dirToPenguin.sqrMagnitude > 0.0001f)
            {
                dirToPenguin = dirToPenguin.normalized;

                // 적에서 펭귄을 향하는 방향(dirToPenguin)과 법선이 마주보지 않는다면 강제 뒤집기 (후방 추돌 방지)
                if (Vector3.Dot(nInPlane, dirToPenguin) < 0f)
                {
                    nInPlane = -nInPlane;
                }
            }

            float approachSpeed = Mathf.Abs(Vector3.Dot(vInPlane, nInPlane));
            Vector3 vNormalVec = -nInPlane * approachSpeed;

            Vector3 vTangentVec = Vector3.ProjectOnPlane(rb.linearVelocity - surfaceVel, nInPlane);
            vTangentVec = Vector3.ProjectOnPlane(vTangentVec, boardNormal);

            Vector3 vRelOut = (-vNormalVec * physicsConfig.bounceRestitution) + (vTangentVec * (1f - physicsConfig.bounceFriction));

            if (Vector3.Dot(vRelOut, nInPlane) < 1f)
            {
                vRelOut += nInPlane * 2f;
            }

            Vector3 vWorldOut = surfaceVel + vRelOut;

            float outSpeed = vWorldOut.magnitude;
            float calculatedSpeed = Mathf.Max(outSpeed, physicsConfig.bounceMinimumSpeed) * physicsConfig.bounceSpeedMultiplier;
            float finalSpeed = Mathf.Clamp(calculatedSpeed, physicsConfig.bounceMinimumSpeed, p.CurrentMaxSpeed);

            ApplyBounce(rb, vWorldOut, finalSpeed);
            HandleHitImpact(p, c);
        }

        private void ApplyBounce(Rigidbody rb, Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            rb.linearVelocity = direction.normalized * speed;
        }

        private void HandleHitImpact(PenguinController penguin, Collision c)
        {
            if (isEnemy && cachedEnemy != null)
            {
                penguin.AddCombo();

                float damage = CombatResolver.CalculateDamage(penguin);
                bool shouldDrain = false;

                penguin.Ability.OnEnemyCollision(cachedEnemy, c, ref damage, ref shouldDrain);
                CombatResolver.DealDamage(penguin, cachedEnemy, damage);

                if (shouldDrain) penguin.Drain();
            }
            else
            {
                penguin.AddCombo();
                penguin.Ability.OnEnvironmentCollision(c);
            }
        }
    }
}