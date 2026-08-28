using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PaydayMark : MonoBehaviour
    {
        public PenguinController Source { get; private set; }
        public float AccumulatedDamage { get; private set; }

        public void Initialize(PenguinController source)
        {
            Source = source;
            AccumulatedDamage = 0f;
        }

        public void AddDamage(float damage)
        {
            if (damage <= 0f) return;
            AccumulatedDamage += damage;
        }

        public void PopMark(float multiplier)
        {
            if (Source == null || !TryGetComponent<Enemy>(out var enemy) || enemy.IsDead)
            {
                Destroy(this);
                return;
            }

            float finalDamage = AccumulatedDamage * multiplier;

            Destroy(this);

            if (finalDamage > 0f)
            {
                CombatResolver.DealDamage(Source, enemy, finalDamage);
            }
        }
    }
}