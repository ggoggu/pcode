using UnityEngine;

namespace PenguinPinball.Core
{
    // 부적 기능 구현 필요
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Engineer")]
    public sealed class EngineerAbilitySO : PenguinAbilitySO
    {
        public override IPenguinAbility CreateAbility() => new EngineerAbility();
    }
}