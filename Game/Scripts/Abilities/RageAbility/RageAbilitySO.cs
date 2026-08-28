using UnityEngine;

namespace PenguinPinball.Core
{
    // rage 구체화 필요
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Rage")]
    public sealed class RageAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(1)] private int normalStackGain = 1;
        [SerializeField, Min(1)] private int overloadStackGain = 2;

        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageBonusPerStack = 0.1f;

        public override IPenguinAbility CreateAbility()
            => new RageAbility(normalStackGain, overloadStackGain, damageBonusPerStack);
    }
}