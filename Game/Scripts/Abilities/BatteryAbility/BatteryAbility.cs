using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class BatteryAbility : PenguinAbilityBase
    {
        private readonly int maxChargeCount;
        private readonly float bonusDamagePerCharge;

        private int _chargeCount = 0;

        private readonly HashSet<int> _visitedObjectIds = new HashSet<int>();

        public BatteryAbility(int maxChargeCount, float bonusDamagePerCharge)
        {
            this.maxChargeCount = maxChargeCount;
            this.bonusDamagePerCharge = bonusDamagePerCharge;
        }

        public override void OnEnvironmentCollision(Collision c)
        {
            if (c == null || c.gameObject == null) return;

            TryCharge(c.gameObject.GetInstanceID());
        }

        public override void OnPenguinCollision(PenguinController other)
        {
            if (other == null) return;

            TryCharge(other.gameObject.GetInstanceID());
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float damage, ref bool shouldDrain)
        {
            if (e == null || e.IsDead) return;

            if (c != null && c.gameObject != null)
            {
                TryCharge(c.gameObject.GetInstanceID());
            }

            if (_chargeCount > 0)
            {
                float multiplier = 1f + (_chargeCount * bonusDamagePerCharge);
                damage *= multiplier;

                ResetBattery();
            }
        }

        private void TryCharge(int instanceId)
        {
            if (_chargeCount >= maxChargeCount) return;

            if (_visitedObjectIds.Add(instanceId))
            {
                _chargeCount++;
                Debug.Log("trycharge success");
            }
        }

        private void ResetBattery()
        {
            _chargeCount = 0;
            _visitedObjectIds.Clear();
        }

        public override void Dispose()
        {
            ResetBattery();
            base.Dispose();
        }
    }
}