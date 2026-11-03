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

    protected Collider mainCollider;
    public Collider MainCollider => mainCollider != null ? mainCollider : (mainCollider = GetComponent<Collider>());

    protected virtual void Awake()
    {
        mainCollider = GetComponent<Collider>();

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

        if (mainCollider != null) mainCollider.enabled = false;

        OnBaseReached?.Invoke(this);
        gameObject.SetActive(false);
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 1. 체력 0 도달 즉시 충돌체 제거 (충돌·데미지·콤보·낚아채기 판정 즉시 배제)
        if (mainCollider == null) mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        // 2. 코인을 사망 즉시 드랍 (사망 애니메이션 종료를 기다리지 않음)
        OnDeath?.Invoke(this);

        // 3. 사망 애니메이션 잔존 처리 (애니메이션 재생 후 정리, 없을 시 즉시 비활성화)
        HandleDeathAnimationOrDeactivate();
    }

    protected virtual void HandleDeathAnimationOrDeactivate()
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.gameObject.activeInHierarchy && HasAnimatorParameter(anim, "Die"))
        {
            anim.SetTrigger("Die");
            StartCoroutine(DeactivateAfterDelay(1.0f));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    private static bool HasAnimatorParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    public virtual void ResetHealth()
    {
        float modified = StatModifierRegistry.Instance != null
            ? StatModifierRegistry.Instance.GetModifiedValue(StatType.EnemyMaxHealth, baseMaxHealth)
            : baseMaxHealth;

        MaxHealth = Mathf.Max(1f, modified);
        CurrentHealth = MaxHealth;
        IsDead = false;

        if (mainCollider != null) mainCollider.enabled = true;
        gameObject.SetActive(true);
    }
}