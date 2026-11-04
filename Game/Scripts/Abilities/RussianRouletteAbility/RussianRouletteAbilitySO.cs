using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/RussianRoulette")]
    public sealed class RussianRouletteAbilitySO : PenguinAbilitySO
    {
        [Header("Russian Roulette Settings")]
        [SerializeField, Min(1)] private int targetHitCount = 6;
        [Tooltip("공격력 대비 데미지 비율 (6.0 = 600%)")]
        [SerializeField, Min(0f)] private float damageRatio = 6.0f;

        public override IPenguinAbility CreateAbility()
            => new RussianRouletteAbility(targetHitCount, damageRatio);
    }
}