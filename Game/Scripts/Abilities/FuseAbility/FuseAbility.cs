using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class FuseAbility : PenguinAbilityBase
    {
        private readonly float explosionRadius;
        private readonly float comboDamageBonus;

        private bool armed = false;
        private readonly Collider[] _hitBuffer = new Collider[32];

        public FuseAbility(float explosionRadius, float comboDamageBonus)
        {
            this.explosionRadius = explosionRadius;
            this.comboDamageBonus = comboDamageBonus;
        }

        public override void OnOverloadStarted()
        {
            armed = true;
        }

        public override void OnOverloadEnded()
        {
            armed = false;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool _)
        {
            if (!armed) return;

            armed = false;
            float damage = d * (1f + Owner.Combo * comboDamageBonus);

            int hitCount = Physics.OverlapSphereNonAlloc(Owner.transform.position, explosionRadius, _hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<Enemy>(out var targetEnemy) && !targetEnemy.IsDead)
                {
                    CombatResolver.DealDamage(Owner, targetEnemy, damage);
                }
            }

            Owner.ResetCombo();
        }

        public override void Dispose()
        {
            armed = false;
            base.Dispose();
        }
    }
}
