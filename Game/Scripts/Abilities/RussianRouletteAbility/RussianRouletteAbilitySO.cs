using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/RussianRoulette")]
    public sealed class RussianRouletteAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(1)] private int maxHitCount = 6;
        [SerializeField, Min(0f)] private float fixedDamage = 600f;

        [Tooltip("충돌 횟수별 발동 확률 (0~1). 배열 길이는 maxHitCount와 일치해야 합니다.")]
        [SerializeField]
        private float[] triggerProbabilities = new float[]
        {
            1f / 6f,
            2f / 6f,
            3f / 6f,
            4f / 6f,
            5f / 6f,
            1f
        };

        public override IPenguinAbility CreateAbility()
            => new RussianRouletteAbility(maxHitCount, fixedDamage, triggerProbabilities);
    }
}