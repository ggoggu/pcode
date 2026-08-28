using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Battery")]
    public sealed class BatteryAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(1)] private int maxChargeCount = 10;
        [SerializeField, Min(0f)] private float bonusDamagePerCharge = 0.2f;

        public override IPenguinAbility CreateAbility()
            => new BatteryAbility(maxChargeCount, bonusDamagePerCharge);
    }
}