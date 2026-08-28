using UnityEngine;

namespace PenguinPinball.Core
{
    // 구현 필요
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Couple")]
    public sealed class CoupleAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new CoupleAbility();
    }
}