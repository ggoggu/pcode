using UnityEngine;

namespace PenguinPinball.Core
{
    [CreateAssetMenu(menuName = "PenguinPinball/Abilities/Speedster")]
    public sealed class SpeedsterAbilitySO : PenguinAbilitySO
    {
        [SerializeField, Min(1)] private int maxHits = 3;

        public override IPenguinAbility CreateAbility() => new SpeedsterAbility(maxHits);
    }
}