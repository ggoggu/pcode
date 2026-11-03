using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PaydayAbility : PenguinAbilityBase
    {
        private readonly float damageMultiplier;
        private readonly List<Enemy> markedEnemies = new();

        private readonly List<PaydayMark> markBuffer = new();

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

            markBuffer.Clear();
            enemy.GetComponents(markBuffer);

            bool hasMyMark = false;

            for (int i = 0; i < markBuffer.Count; i++)
            {
                if (markBuffer[i].Source == Owner)
                {
                    hasMyMark = true;
                    break;
                }
            }

            if (!hasMyMark)
            {
                var newMark = enemy.gameObject.AddComponent<PaydayMark>();
                newMark.Initialize(Owner);
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
                    markBuffer.Clear();
                    enemy.GetComponents(markBuffer);

                    var existingMarks = enemy.GetComponents<PaydayMark>();

                    for (int j = 0; j < markBuffer.Count; j++)
                    {
                        var mark = markBuffer[j];
                        if (mark.Source == Owner)
                        {
                            mark.PopMark(damageMultiplier);
                            break;
                        }
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