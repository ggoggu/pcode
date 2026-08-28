using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Stack")]
    public sealed class StackAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Range(0f, 1f)] private float damageBonusPerStack = 0.1f;
        [SerializeField, Min(1)] private int maxStackCount = 99;

        public override IPenguinAbility CreateAbility()
            => new StackAbility(damageBonusPerStack, maxStackCount);
    }
}