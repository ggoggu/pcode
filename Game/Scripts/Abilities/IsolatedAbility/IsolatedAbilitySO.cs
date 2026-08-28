using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Isolated")]
    public sealed class IsolatedAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(0.1f)] private float isolationCheckRadius = 5f;
        [SerializeField, Min(1f)] private float damageMultiplier = 2f;

        public override IPenguinAbility CreateAbility()
            => new IsolatedAbility(isolationCheckRadius, damageMultiplier);
    }
}