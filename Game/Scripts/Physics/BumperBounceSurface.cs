using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class BumperBounceSurface : PinballBounceSurface
    {
        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(c);
        }
    }
}