using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(MeshCollider))]
    public sealed class SplineBounceWall : PinballBounceSurface
    {
        // 스플라인 벽은 접점(contact.point) 기준으로 물리 방향을 계산하도록 설정
        protected override bool UseContactPointAsOrigin => true;

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(c);
        }
    }
}