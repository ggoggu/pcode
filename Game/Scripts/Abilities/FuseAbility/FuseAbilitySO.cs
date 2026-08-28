using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Fuse")]
    public sealed class FuseAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(0.1f)] private float explosionRadius = 3f;
        [SerializeField, Min(0f)] private float comboDamageBonus = 0.1f;

        public override IPenguinAbility CreateAbility()
            => new FuseAbility(explosionRadius, comboDamageBonus);
    }
}