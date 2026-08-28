using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class HeavyballAbility : PenguinAbilityBase
    {
        private readonly HashSet<Enemy> hitEnemiesInOverload = new();

        private int ghostLayer;
        public HeavyballAbility() { }

        public override void Initialize(PenguinController owner)
        {
            base.Initialize(owner);
            ghostLayer = LayerMask.NameToLayer("GhostPenguin");

            if (ghostLayer == -1)
            {
                Debug.LogWarning("[HeavyballAbility] 'GhostPenguin' 레이어가 존재하지 않습니다. Project Settings에서 레이어를 추가해 주세요.");
            }
        }

        public override void OnOverloadStarted()
        {
            hitEnemiesInOverload.Clear();

            // 과부화 진입 시 GhostPenguin 레이어로 전환 (Solid Collider 통과)
            if (ghostLayer != -1)
            {
                Owner.SetLayerRecursively(ghostLayer);
                Debug.Log($"[Heavyball] 과부화 시작! 현재 레이어: {LayerMask.LayerToName(Owner.gameObject.layer)}");
            }
            else
                Debug.LogError("[Heavyball] ghostLayer가 -1이라 레이어를 변경하지 못했습니다! Project Settings의 레이어 이름을 확인하세요.");
        }

        public override void OnOverloadEnded()
        {
            hitEnemiesInOverload.Clear();

            // 안전 레이어 복구 요청 (적 내부에 갇혀있으면 완전히 탈출할 때까지 유예)
            Owner.RestoreDefaultLayer();
        }

        public override void OnEnemyTriggerEnter(Enemy enemy, Collider other)
        {
            // 과부화 상태일 때 Trigger 충돌 처리
            if (!Owner.IsOverloaded || enemy == null) return;

            // 한 번의 관통 동안 동일한 적 중복 타격 방지
            if (hitEnemiesInOverload.Add(enemy))
            {
                Owner.AddCombo();

                float damage = CombatResolver.CalculateDamage(Owner);
                bool shouldDrain = false;

                // 능력 특수 효과 피드백 필요 시 호출
                OnEnemyCollision(enemy, null, ref damage, ref shouldDrain);

                CombatResolver.DealDamage(Owner, enemy, damage);

                if (shouldDrain)
                {
                    Owner.Drain();
                }
            }
        }
    }
}
