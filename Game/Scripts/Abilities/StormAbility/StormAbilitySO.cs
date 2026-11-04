using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Storm")]
    public sealed class StormAbilitySO : PenguinAbilitySO
    {
        [SerializeField] private float overloadRadius = 3.5f;

        public override IPenguinAbility CreateAbility()
            => new StormAbility(overloadRadius);
    }
}