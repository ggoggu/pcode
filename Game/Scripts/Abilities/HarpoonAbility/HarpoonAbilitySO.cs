using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Harpoon")]
    public sealed class HarpoonAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new HarpoonAbility();
    }
}