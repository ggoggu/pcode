using System.Collections.Generic;

namespace PenguinPinball.Core
{
    [System.Serializable]
    public class Stat
    {
        public float BaseValue { get; set; }
        private readonly List<StatModifier> modifiers = new();

        public Stat(float baseValue)
        {
            BaseValue = baseValue;
        }

        // BaseValue에 고정치(Flat)와 비율(Percent)을 계산해 최종값 반환
        public float Value
        {
            get
            {
                float baseFlatSum = 0f;
                float basePercentSum = 0f;
                float flatSum = 0f;
                float percentSum = 0f;

                // 1. 모디파이어 타입별 합산
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var mod = modifiers[i];
                    if (mod.Type == StatModType.BaseFlat) baseFlatSum += mod.Value;
                    else if (mod.Type == StatModType.BasePercentAdd) basePercentSum += mod.Value;
                    else if (mod.Type == StatModType.Flat) flatSum += mod.Value;
                    else if (mod.Type == StatModType.PercentAdd) percentSum += mod.Value;
                }

                // 2. 최종 계산 (기본 스탯 성장 -> 아이템/버프 장착 순서)
                float finalBase = (BaseValue + baseFlatSum) * (1f + basePercentSum);
                float finalValue = (finalBase + flatSum) * (1f + percentSum);

                return finalValue;
            }
        }

        public void AddModifier(StatModifier mod)
        {
            modifiers.Add(mod);
        }

        public void RemoveAllModifiersFromSource(object source)
        {
            modifiers.RemoveAll(mod => mod.Source == source);
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
        }
    }
}