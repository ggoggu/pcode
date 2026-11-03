using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public static class CombatResolver
    {
        private static readonly List<PaydayMark> paydayMarkBuffer = new List<PaydayMark>();
        public delegate float DamageModifierDelegate(PenguinController source, Enemy enemy, float currentDamage, bool isCollision);

        private static readonly List<DamageModifierDelegate> damageModifiers = new();

        public static void RegisterDamageModifier(DamageModifierDelegate modifier)
        {
            if (modifier != null && !damageModifiers.Contains(modifier))
            {
                damageModifiers.Add(modifier);
            }
        }

        public static void UnregisterDamageModifier(DamageModifierDelegate modifier)
        {
            if (modifier != null)
            {
                damageModifiers.Remove(modifier);
            }
        }

        public static void ClearDamageModifiers()
        {
            damageModifiers.Clear();
        }

        /// <summary>
        /// 대상 적에게 부착된 활성 표식의 고유 종류 수를 반환합니다.
        /// 동일 종류의 표식이 여러 개 부착되어 있어도 1개로 취급합니다.
        /// </summary>
        public static int GetDistinctMarkCount(Enemy enemy)
        {
            if (enemy == null) return 0;

            var distinctTypes = new HashSet<string>();

            // 1. ICombatMark 구현 컴포넌트들 검사
            var marks = enemy.GetComponents<ICombatMark>();
            if (marks != null)
            {
                for (int i = 0; i < marks.Length; i++)
                {
                    var m = marks[i];
                    if (m != null && m.IsMarked)
                    {
                        distinctTypes.Add(m.MarkType ?? m.GetType().Name);
                    }
                }
            }

            // 2. Fallback: VulnerabilityMark 검사
            if (enemy.TryGetComponent<VulnerabilityMark>(out var vulnMark) && vulnMark.IsMarked)
            {
                distinctTypes.Add(nameof(VulnerabilityMark));
            }

            // 3. Fallback: PaydayMark 검사
            if (enemy.TryGetComponent<PaydayMark>(out var paydayMark) && paydayMark.IsMarked)
            {
                distinctTypes.Add(nameof(PaydayMark));
            }

            return distinctTypes.Count;
        }

        public static float CalculateDamage(PenguinController penguin)
        {
            var d = penguin.Definition;
            float speed = penguin.Body.linearVelocity.magnitude;
            float factor = speed * (1f - Mathf.Exp(-speed / Mathf.Max(.01f, d.lowSpeedCutoff)));
            return d.attack * factor * d.damageSpeedScale;
        }

        public static void DealDamage(PenguinController source, Enemy enemy, float rawDamage, bool isCollision = true)
        {
            if (enemy == null || enemy.IsDead || rawDamage <= 0f) return;

            float actualDamage = rawDamage;

            // 1. 등록된 데미지 모디파이어(표식 증폭 부적 등)를 표식 소모 전에 순차 적용
            for (int i = 0; i < damageModifiers.Count; i++)
            {
                if (damageModifiers[i] != null)
                {
                    actualDamage = damageModifiers[i](source, enemy, actualDamage, isCollision);
                }
            }

            // 2. 1회성 소모 표식(취약 등) 처리
            if (enemy.TryGetComponent<VulnerabilityMark>(out var mark))
            {
                actualDamage = mark.ConsumeAndModifyDamage(source, actualDamage, 1.5f);
            }

            enemy.TakeDamage(actualDamage);

            paydayMarkBuffer.Clear();
            enemy.GetComponents(paydayMarkBuffer);

            for (int i = 0; i < paydayMarkBuffer.Count; i++)
            {
                paydayMarkBuffer[i].AddDamage(source, actualDamage);
            }

            GameEvents.RaiseEnemyDamaged(source, enemy, actualDamage);
            if (enemy.IsDead) GameEvents.RaiseEnemyKilled(source, enemy);
        }
    }
}
