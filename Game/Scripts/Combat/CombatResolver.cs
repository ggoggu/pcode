using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace PenguinPinball.Core
{
    public static class CombatResolver
    {
        public static float CalculateDamage(PenguinController penguin)
        {
            var d = penguin.Definition;
            float speed = penguin.Body.linearVelocity.magnitude;
            // Smooth cutoff: exactly zero at zero velocity; ramps naturally after cutoff.
            float factor = speed * (1f - Mathf.Exp(-speed / Mathf.Max(.01f, d.lowSpeedCutoff)));
            return d.attack * factor * d.damageSpeedScale;
        }
        public static void DealDamage(PenguinController source, Enemy enemy, float rawDamage)
        {
            if (enemy == null || enemy.IsDead || rawDamage <= 0f) return;

            float actualDamage = rawDamage;

            if (enemy.TryGetComponent<VulnerabilityMark>(out var mark))
            {
                actualDamage = mark.ConsumeAndModifyDamage(source, rawDamage, 1.5f);
            }

            enemy.TakeDamage(actualDamage);
            if (enemy.TryGetComponent<PaydayMark>(out var Pmark))
            {
                Pmark.AddDamage(actualDamage);
            }
            
            GameEvents.RaiseEnemyDamaged(source, enemy, actualDamage);
            if (enemy.IsDead) GameEvents.RaiseEnemyKilled(source, enemy);
        }
    }
}
