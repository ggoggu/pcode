using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class StormAbility : PenguinAbilityBase
    {
        private readonly float normalRadius;
        private readonly float overloadRadius;
        private readonly float normalTickDamageRatio;
        private readonly float overloadTickDamageRatio;

        private readonly Collider[] _hitBuffer = new Collider[32];

        public StormAbility(float normalRadius, float overloadRadius,
            float normalTickDamageRatio, float overloadTickDamageRatio)
        {
            this.normalRadius = normalRadius;
            this.overloadRadius = overloadRadius;
            this.normalTickDamageRatio = normalTickDamageRatio;
            this.overloadTickDamageRatio = overloadTickDamageRatio;
        }

        public override void OnEnemyCollision(Enemy e, Collision c, ref float d, ref bool _)
        {
            float currentRadius = Owner.IsOverloaded ? overloadRadius : normalRadius;

            var hits = Physics.OverlapSphere(Owner.transform.position, currentRadius);
            var list = new List<Enemy>();

            foreach (var h in hits)
                if (h.TryGetComponent<Enemy>(out var x) && !x.IsDead) list.Add(x);

            foreach (var x in list)
                CombatResolver.DealDamage(Owner, x, d / Mathf.Max(1, list.Count));
        }

        public override void Tick()
        {
            bool isOverloaded = Owner.IsOverloaded;
            float currentRadius = isOverloaded ? overloadRadius : normalRadius;
            float currentRatio = isOverloaded ? overloadTickDamageRatio : normalTickDamageRatio;

            int hitCount = Physics.OverlapSphereNonAlloc(Owner.transform.position, currentRadius, _hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    float damage = Owner.Definition.attack * currentRatio * Time.deltaTime;
                    CombatResolver.DealDamage(Owner, enemy, damage);
                }
            }
        }
    }
}
