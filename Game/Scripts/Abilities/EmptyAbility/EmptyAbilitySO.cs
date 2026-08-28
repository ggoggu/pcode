using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Empty")]
    public sealed class EmptyAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new EmptyAbility();
    }
}