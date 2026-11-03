using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Pyro")]
    public sealed class PyroAbilitySO : PenguinAbilitySO
    {
        [Header("Burn Settings")]
        [SerializeField, Min(0f)] private float burnDuration = 3.0f;
        [SerializeField, Range(0f, 1f)] private float burnDamageRatio = 0.5f;
        [SerializeField, Min(0.1f)] private float tickInterval = 0.5f;
        [SerializeField, Min(0f)] private float aoeRadius = 2.5f;

        public override IPenguinAbility CreateAbility()
            => new PyroAbility(burnDuration, burnDamageRatio, tickInterval, aoeRadius);
    }
}