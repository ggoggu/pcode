using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Conductor")]
    public sealed class ConductorAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Range(0f, 1f)] private float comboTakeRatio = 0.4f;
        [SerializeField, Range(0f, 1f)] private float comboGiveRatio = 0.5f;
        [SerializeField, Min(0f)] private float cooldown = 0.5f;

        public override IPenguinAbility CreateAbility()
            => new ConductorAbility(comboTakeRatio, comboGiveRatio, cooldown);
    }
}