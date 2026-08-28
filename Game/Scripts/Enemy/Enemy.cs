using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("적 스탯")]
    [SerializeField, Min(1f)] float baseMaxHealth = 30f;

    public float MaxHealth { get; private set; }
    public float BaseGold { get; private set; } = 10f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<Enemy> OnDeath;
    public event Action<Enemy> OnBaseReached;

    protected virtual void Awake()
    {
        float modified = StatModifierRegistry.Instance != null
            ? StatModifierRegistry.Instance.GetModifiedValue(StatType.EnemyMaxHealth, baseMaxHealth)
            : baseMaxHealth;

        MaxHealth = Mathf.Max(1f, modified);
        CurrentHealth = MaxHealth;
    }

    protected virtual void Start()
    {
    }

    public void SetBaseGold(float gold)
    {
        BaseGold = Mathf.Max(0f, gold);
    }

    public virtual void ApplyHpMultiplier(float multiplier)
    {
        CurrentHealth = Mathf.Max(1f, CurrentHealth * Mathf.Max(0f, multiplier));
    }

    public virtual void TakeDamage(float value)
    {
        if (IsDead || value <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - value);
        Debug.Log($"[HP Debug] {gameObject.name} 이(가) {value} 피해를 입음! (남은 HP: {CurrentHealth} / {MaxHealth})", gameObject);

        if (CurrentHealth > 0f) return;
        Die();
    }

    public void ReachBase()
    {
        if (IsDead) return;
        IsDead = true;

        OnBaseReached?.Invoke(this);
        gameObject.SetActive(false);
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath?.Invoke(this);
        gameObject.SetActive(false);
    }

    public virtual void ResetHealth()
    {
        float modified = StatModifierRegistry.Instance != null
            ? StatModifierRegistry.Instance.GetModifiedValue(StatType.EnemyMaxHealth, baseMaxHealth)
            : baseMaxHealth;

        MaxHealth = Mathf.Max(1f, modified);
        CurrentHealth = MaxHealth;
        IsDead = false;
        gameObject.SetActive(true);
    }
}