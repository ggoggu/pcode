using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Shaving")]
    public sealed class ShavingAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Range(0f, 1f)] private float healthThresholdRatio = 0.8f;
        [SerializeField, Min(1f)] private float normalDamageMultiplier = 1.4f;
        [SerializeField, Min(1f)] private float overloadedDamageMultiplier = 1.8f;

        public override IPenguinAbility CreateAbility()
            => new ShavingAbility(healthThresholdRatio, normalDamageMultiplier, overloadedDamageMultiplier);
    }
}