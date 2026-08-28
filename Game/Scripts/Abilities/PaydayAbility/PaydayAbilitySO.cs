using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Payday")]
    public sealed class PaydayAbilitySO : PenguinAbilitySO
    {
        [Header("Payday Settings")]
        [Tooltip("과부화 종료 시 누적 데미지의 몇 %를 폭발 데미지로 입힐지 설정합니다. (0.4 = 40%)")]
        [SerializeField, Range(0f, 1f)] private float damageMultiplier = 0.4f;

        public float DamageMultiplier => damageMultiplier;

        public override IPenguinAbility CreateAbility()
        {
            return new PaydayAbility(damageMultiplier);
        }
    }
}