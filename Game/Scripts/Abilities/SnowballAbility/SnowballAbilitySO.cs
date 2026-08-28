using UnityEngine;

namespace PenguinPinball.Core
{
    // 구현 필요
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Snowball")]
    public sealed class SnowballAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new SnowballAbility();
    }
}