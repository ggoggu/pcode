using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MaxLife,            // 최대 생명력
    EnemyMaxHealth,     // 적 최대 체력
    FieldPenguinLimit,  // 필드 펭귄 제한 수
    OverloadThreshold,  // 과부하 진입 문턱
    GoldGainMultiplier  // 골드 획득 배율
}

public class StatModifierRegistry : MonoBehaviour
{
    public static StatModifierRegistry Instance { get; private set; }

    private Dictionary<StatType, float> flatModifiers = new Dictionary<StatType, float>();
    private Dictionary<StatType, float> multModifiers = new Dictionary<StatType, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddModifier(StatType type, float flatAmount, float multAmount = 0f)
    {
        if (!flatModifiers.ContainsKey(type)) flatModifiers[type] = 0f;
        if (!multModifiers.ContainsKey(type)) multModifiers[type] = 1f;

        flatModifiers[type] += flatAmount;
        multModifiers[type] += multAmount; 
    }

    public void RemoveModifier(StatType type, float flatAmount, float multAmount = 0f)
    {
        if (flatModifiers.ContainsKey(type)) flatModifiers[type] -= flatAmount;
        if (multModifiers.ContainsKey(type)) multModifiers[type] -= multAmount;
    }

    public float GetModifiedValue(StatType type, float baseValue)
    {
        float flat = flatModifiers.ContainsKey(type) ? flatModifiers[type] : 0f;
        float mult = multModifiers.ContainsKey(type) ? multModifiers[type] : 1f;

        return (baseValue + flat) * mult;
    }
}
