using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class ShavingAbility : PenguinAbilityBase
    {
        private readonly float healthThresholdRatio;
        private readonly float normalDamageMultiplier;
        private readonly float overloadedDamageMultiplier;

        public ShavingAbility(float healthThresholdRatio, float normalDamageMultiplier, float overloadedDamageMultiplier)
        {
            this.healthThresholdRatio = healthThresholdRatio;
            this.normalDamageMultiplier = normalDamageMultiplier;
            this.overloadedDamageMultiplier = overloadedDamageMultiplier;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float damage, ref bool _)
        {
            if (e == null || e.IsDead) return;

            float healthRatio = e.CurrentHealth / e.MaxHealth;

            if (healthRatio >= healthThresholdRatio)
            {
                float multiplier = Owner.IsOverloaded ? overloadedDamageMultiplier : normalDamageMultiplier;
                damage *= multiplier;
            }
        }
    }
}
