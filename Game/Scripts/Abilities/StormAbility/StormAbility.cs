using System;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class StormAbility : PenguinAbilityBase
    {
        private readonly float overloadRadius;

        public event Action<Vector3, float> OnOverloadImpact;

        private readonly Collider[] _hitBuffer = new Collider[32];

        public StormAbility(float overloadRadius)
        {
            this.overloadRadius = overloadRadius;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool _)
        {
            if (!Owner.IsOverloaded) return;

            int hitCount = Physics.OverlapSphereNonAlloc(Owner.transform.position, overloadRadius, _hitBuffer);
            if (hitCount == 0) return;

            // 1차 순회: 유효한 적(살아있는 적) 개수 측정
            int validEnemyCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    validEnemyCount++;
                }
            }

            if (validEnemyCount == 0) return;

            // 2차 순회: 대미지 균등 분배 적용
            float splitDamage = d / validEnemyCount;

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    Debug.Log($"[Split Damage] Target: {enemy.name}, Damage: {splitDamage}, validEnemyCount: {validEnemyCount}");
                    CombatResolver.DealDamage(Owner, enemy, splitDamage);
                }
            }

            // 과부하 폭발 이펙트/사운드용 이벤트 호출
            OnOverloadImpact?.Invoke(Owner.transform.position, overloadRadius);
        }
    }
}
