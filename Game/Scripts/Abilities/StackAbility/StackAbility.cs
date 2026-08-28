using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class StackAbility : PenguinAbilityBase
    {
        private readonly float damageBonusPerStack;
        private readonly int maxStackCount;

        private int _stackCount = 0;

        public StackAbility(float damageBonusPerStack, int maxStackCount)
        {
            this.damageBonusPerStack = damageBonusPerStack;
            this.maxStackCount = maxStackCount;
        }

        public override void Initialize(PenguinController owner)
        {
            base.Initialize(owner);
            GameEvents.EnemyKilled += HandleEnemyKilled;
        }

        private void HandleEnemyKilled(PenguinController killer, Enemy victim)
        {
            if (killer == Owner)
            {
                _stackCount = Mathf.Min(_stackCount + 1, maxStackCount);
            }
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float damage, ref bool _)
        {
            if (e == null || e.IsDead || _stackCount <= 0) return;

            float multiplier = 1f + (_stackCount * damageBonusPerStack);
            damage *= multiplier;
        }

        public override void Dispose()
        {
            GameEvents.EnemyKilled -= HandleEnemyKilled;
            _stackCount = 0;
            base.Dispose();
        }
    }
}
