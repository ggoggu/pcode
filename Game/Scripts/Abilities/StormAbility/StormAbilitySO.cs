using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Storm")]
    public sealed class StormAbilitySO : PenguinAbilitySO
    {
        [SerializeField] private float normalRadius = 1.8f;
        [SerializeField] private float overloadRadius = 3.5f;
        [SerializeField] private float normalTickDamageRatio = 0.03f;
        [SerializeField] private float overloadTickDamageRatio = 0.06f;

        public override IPenguinAbility CreateAbility()
            => new StormAbility(normalRadius, overloadRadius, normalTickDamageRatio, overloadTickDamageRatio);
    }
}