using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PaydayAbility : PenguinAbilityBase
    {
        private readonly float damageMultiplier;
        private readonly List<Enemy> markedEnemies = new();

        public PaydayAbility(float damageMultiplier)
        {
            this.damageMultiplier = damageMultiplier;
        }

        public override void OnEnemyCollision(Enemy enemy, Collision collision, ref float baseDamage, ref bool shouldDrain)
        {
            if (!Owner.IsOverloaded) return;
            TryApplyMark(enemy);
        }

        private void TryApplyMark(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;

            var mark = enemy.GetComponent<PaydayMark>();
            if (mark == null)
            {
                mark = enemy.gameObject.AddComponent<PaydayMark>();
                mark.Initialize(Owner);
            }

            if (!markedEnemies.Contains(enemy))
            {
                markedEnemies.Add(enemy);
            }
        }

        public override void OnOverloadEnded()
        {
            PopAllMarks();
        }

        private void PopAllMarks()
        {
            for (int i = markedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = markedEnemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    if (enemy.TryGetComponent<PaydayMark>(out var mark) && mark.Source == Owner)
                    {
                        mark.PopMark(damageMultiplier);
                    }
                }
            }

            markedEnemies.Clear();
        }

        public override void Dispose()
        {
            PopAllMarks();
            base.Dispose();
        }
    }
}