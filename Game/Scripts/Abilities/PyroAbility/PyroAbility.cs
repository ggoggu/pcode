using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PyroAbility : PenguinAbilityBase
    {
        private readonly float burnDuration;
        private readonly float burnDamageRatio;
        private readonly float tickInterval;
        private readonly float aoeRadius;

        private readonly List<BurningMark> markBuffer = new();
        private static readonly int EnemyLayerMask = LayerMask.GetMask("Enemy");

        public PyroAbility(float burnDuration, float burnDamageRatio, float tickInterval, float aoeRadius)
        {
            this.burnDuration = burnDuration;
            this.burnDamageRatio = burnDamageRatio;
            this.tickInterval = tickInterval;
            this.aoeRadius = aoeRadius;
        }

        public override void OnEnemyCollision(Enemy enemy, Collision collision, ref float baseDamage, ref bool shouldDrain)
        {
            if (enemy == null || enemy.IsDead) return;

            float totalBurnDamage = baseDamage * burnDamageRatio;
            int totalTicks = Mathf.Max(1, Mathf.FloorToInt(burnDuration / tickInterval));
            float damagePerTick = totalBurnDamage / totalTicks;

            BurningMark existingMark = GetMyMark(enemy);

            if (existingMark != null && existingMark.IsBurning)
            {
                // 이미 불타는 적: 지속시간 갱신
                existingMark.ApplyOrRefresh(Owner, burnDuration, tickInterval, damagePerTick);

                // 과부하 상태에서 이미 불타는 적과 재충돌 시 화염 범위 공격
                if (Owner != null && Owner.IsOverloaded)
                {
                    TriggerFlameAOE(enemy.transform.position, damagePerTick);
                }
            }
            else
            {
                // 불타지 않는 적: 마크 Component 추가 및 발화
                if (existingMark == null)
                {
                    existingMark = enemy.gameObject.AddComponent<BurningMark>();
                }
                existingMark.ApplyOrRefresh(Owner, burnDuration, tickInterval, damagePerTick);
            }
        }

        private BurningMark GetMyMark(Enemy enemy)
        {
            markBuffer.Clear();
            enemy.GetComponents(markBuffer);

            for (int i = 0; i < markBuffer.Count; i++)
            {
                if (markBuffer[i].Source == Owner)
                {
                    return markBuffer[i];
                }
            }

            return null;
        }

        private void TriggerFlameAOE(Vector3 centerPosition, float damagePerTick)
        {
            Collider[] hitColliders = Physics.OverlapSphere(centerPosition, aoeRadius, EnemyLayerMask);

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<Enemy>(out var target) && !target.IsDead)
                {
                    BurningMark mark = GetMyMark(target);
                    if (mark == null)
                    {
                        mark = target.gameObject.AddComponent<BurningMark>();
                    }
                    mark.ApplyOrRefresh(Owner, burnDuration, tickInterval, damagePerTick);
                }
            }
        }
    }
}