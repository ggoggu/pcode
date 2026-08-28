using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class SpeedsterAbility : PenguinAbilityBase
    {
        private readonly int maxHits;

        public SpeedsterAbility(int maxHits)
        {
            this.maxHits = maxHits;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool _)
        {
            if (Owner.IsOverloaded)
            {
                if (e == null || e.IsDead) return;

                int hitCount = 0;

                for (int i = 0; i < maxHits; i++)
                {
                    if (e.IsDead) break;

                    CombatResolver.DealDamage(Owner, e, d);
                    hitCount++;
                }

                if (hitCount > 0)
                    Owner.AddCombo(hitCount);
            }
        }
    }
}