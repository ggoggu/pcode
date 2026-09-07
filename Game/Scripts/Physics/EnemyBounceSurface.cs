using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class EnemyBounceSurface : PinballBounceSurface
    {
        private Enemy cachedEnemy;
        private int ghostLayer;

        private void Awake()
        {
            cachedEnemy = GetComponent<Enemy>();
            ghostLayer = LayerMask.NameToLayer("GhostPenguin");
        }

        protected override bool ShouldIgnoreCollision(PenguinController p)
        {
            // 과부화 및 고스트 레이어 예외 처리
            return p.IsOverloaded && ghostLayer != -1 && p.gameObject.layer == ghostLayer;
        }

        protected override void OnHitImpact(PenguinController penguin, Collision c)
        {
            penguin.AddCombo();

            if (cachedEnemy == null) return;

            float damage = CombatResolver.CalculateDamage(penguin);
            bool shouldDrain = false;

            penguin.Ability.OnEnemyCollision(cachedEnemy, c, ref damage, ref shouldDrain);
            CombatResolver.DealDamage(penguin, cachedEnemy, damage);

            if (shouldDrain) penguin.Drain();
        }
    }
}