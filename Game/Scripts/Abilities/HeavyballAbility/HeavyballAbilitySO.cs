using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Heavyball")]
    public sealed class HeavyballAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new HeavyballAbility();
    }
}