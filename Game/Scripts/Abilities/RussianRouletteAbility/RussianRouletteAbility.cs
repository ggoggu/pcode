using System;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class RussianRouletteAbility : PenguinAbilityBase
    {
        private readonly int targetHitCount;
        private readonly float damageRatio;

        private int _hitCount = 0;

        public event Action<Enemy, Vector3> OnRouletteTriggered;

        public RussianRouletteAbility(int targetHitCount, float damageRatio)
        {
            this.targetHitCount = targetHitCount;
            this.damageRatio = damageRatio;
        }

        public override void OnOverloadStarted()
        {
            base.OnOverloadStarted();
            _hitCount = 0;
        }

        public override void OnOverloadEnded()
        {
            base.OnOverloadEnded();
            _hitCount = 0;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool shouldDrain)
        {
            if (!Owner.IsOverloaded) return;
            if (e == null || e.IsDead) return;

            _hitCount++;

            if (_hitCount >= targetHitCount)
            {
                // 공격력의 600% 데미지 설정
                float calculatedDamage = Owner.Definition.attack * damageRatio;
                CombatResolver.DealDamage(Owner, e, calculatedDamage);

                Debug.Log($"damange: {calculatedDamage}");

                _hitCount = 0;

                Vector3 hitPoint = (c != null && c.contactCount > 0) ? c.GetContact(0).point : e.transform.position;
                OnRouletteTriggered?.Invoke(e, hitPoint);

                // 즉시 필드 이탈 처리
                shouldDrain = true;
            }
        }

        public override void Dispose()
        {
            _hitCount = 0;
            base.Dispose();
        }
    }
}
