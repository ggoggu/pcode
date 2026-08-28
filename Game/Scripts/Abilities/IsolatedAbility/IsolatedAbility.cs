using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class IsolatedAbility : PenguinAbilityBase
    {
        private readonly float isolationCheckRadius;
        private readonly float damageMultiplier;

        private static readonly Collider[] EnemyBuffer = new Collider[16];

        public IsolatedAbility(float isolationCheckRadius, float damageMultiplier)
        {
            this.isolationCheckRadius = isolationCheckRadius;
            this.damageMultiplier = damageMultiplier;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float damage, ref bool _)
        {
            if (e == null || e.IsDead) return;

            if (IsEnemyIsolated(e))
            {
                if (Owner.IsOverloaded) damage = damage * 2;
                else damage = damage * damageMultiplier;
            }
        }

        private bool IsEnemyIsolated(Enemy targetEnemy)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                targetEnemy.transform.position,
                isolationCheckRadius,
                EnemyBuffer
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = EnemyBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<Enemy>(out var otherEnemy))
                {
                    if (otherEnemy != targetEnemy && !otherEnemy.IsDead)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
