using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class RussianRouletteAbility : PenguinAbilityBase
    {
        private readonly int maxHitCount;
        private readonly float fixedDamage;
        private readonly float[] triggerProbabilities;

        private int _hitCount = 0;

        public RussianRouletteAbility(int maxHitCount, float fixedDamage, float[] triggerProbabilities)
        {
            this.maxHitCount = maxHitCount;
            this.fixedDamage = fixedDamage;
            this.triggerProbabilities = triggerProbabilities;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool shouldDrain)
        {
            if (e == null || e.IsDead) return;

            _hitCount++;

            int index = Mathf.Clamp(_hitCount - 1, 0, maxHitCount - 1);
            float chance = triggerProbabilities[Mathf.Clamp(index, 0, triggerProbabilities.Length - 1)];

            if (Random.value <= chance)
            {
                d = fixedDamage;
                shouldDrain = true;
                _hitCount = 0;
            }
        }

        public override void Dispose()
        {
            _hitCount = 0;
            base.Dispose();
        }
    }
}
