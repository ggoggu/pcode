using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PaydayMark : MonoBehaviour, ICombatMark
    {
        public PenguinController Source { get; private set; }
        public float AccumulatedDamage { get; private set; }
        public bool IsMarked => Source != null;
        public string MarkType => nameof(PaydayMark);

        private bool isPopping = false;

        public void Initialize(PenguinController source)
        {
            Source = source;
            AccumulatedDamage = 0f;
        }

        public void AddDamage(PenguinController damageSource, float damage)
        {
            if (isPopping || damage <= 0f || damageSource != Source) return;

            AccumulatedDamage += damage;
        }

        public void PopMark(float multiplier)
        {
            if (Source == null || !TryGetComponent<Enemy>(out var enemy) || enemy.IsDead)
            {
                Destroy(this);
                return;
            }

            isPopping = true;

            float finalDamage = AccumulatedDamage * multiplier;

            Destroy(this);

            if (finalDamage > 0f)
            {
                CombatResolver.DealDamage(Source, enemy, finalDamage, isCollision: false);
            }
        }
    }
}