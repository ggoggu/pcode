using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class ConductorAbility : PenguinAbilityBase
    {
        private readonly float comboTakeRatio;
        private readonly float comboGiveRatio;
        private readonly float cooldown;

        private float _lastCollisionTime = -999f;

        public ConductorAbility(float comboTakeRatio, float comboGiveRatio, float cooldown)
        {
            this.comboTakeRatio = comboTakeRatio;
            this.comboGiveRatio = comboGiveRatio;
            this.cooldown = cooldown;
        }

        public override void OnPenguinCollision(PenguinController other)
        {
            if (other == null) return;

            if (Time.time - _lastCollisionTime < cooldown) return;
            _lastCollisionTime = Time.time;

            if (!Owner.IsOverloaded)
            {
                int comboToTake = Mathf.FloorToInt(other.Combo * comboTakeRatio);
                if (comboToTake > 0)
                    Owner.AddCombo(comboToTake);
            }
            else
            {
                int comboToGive = Mathf.FloorToInt(Owner.Combo * comboGiveRatio);
                if (comboToGive > 0)
                    other.AddCombo(comboToGive);
            }
        }
    }
}
