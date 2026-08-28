using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class RageAbility : PenguinAbilityBase
    {
        private readonly int normalStackGain;
        private readonly int overloadStackGain;
        private readonly float damageBonusPerStack;

        private int _rageStacks = 0;

        public RageAbility(int normalStackGain, int overloadStackGain, float damageBonusPerStack)
        {
            this.normalStackGain = normalStackGain;
            this.overloadStackGain = overloadStackGain;
            this.damageBonusPerStack = damageBonusPerStack;
        }

        public override void Initialize(PenguinController owner)
        {
            base.Initialize(owner);
            GameEvents.PenguinDrained += HandlePenguinDrained;
        }

        private void HandlePenguinDrained(PenguinController drainedPenguin, float _)
        {
            if (drainedPenguin != null && drainedPenguin != Owner)
            {
                int stackGain = Owner.IsOverloaded ? overloadStackGain : normalStackGain;
                _rageStacks += stackGain;
            }
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float damage, ref bool shouldDrain)
        {
            if (e == null || e.IsDead || _rageStacks <= 0) return;

            damage = damage * (1f + (_rageStacks * damageBonusPerStack));
            return;
        }

        public override void Dispose()
        {
            GameEvents.PenguinDrained -= HandlePenguinDrained;
            _rageStacks = 0;
            base.Dispose();
        }
    }
}
