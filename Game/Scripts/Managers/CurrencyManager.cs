using System;
using UnityEngine;

/// <summary>
/// 레거시 호환성을 위한 래퍼(Wrapper). 실제 재화의 진실의 원천(SSOT)은 EconomyManager입니다.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("재화 설정 (폴백용)")]
    [SerializeField] private int fallbackGold = 100;

    public int CurrentGold => EconomyManager.Instance != null ? EconomyManager.Instance.CurrentGold : fallbackGold;

    // 골드 변경 시 UI 등에 알릴 이벤트
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }

    private void OnEnable()
    {
        EconomyManager.OnGoldChanged += HandleEconomyGoldChanged;
    }

    private void OnDisable()
    {
        EconomyManager.OnGoldChanged -= HandleEconomyGoldChanged;
    }

    private void HandleEconomyGoldChanged(int newGold)
    {
        fallbackGold = newGold;
        OnGoldChanged?.Invoke(newGold);
    }

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            fallbackGold = EconomyManager.Instance.CurrentGold;
        }
        OnGoldChanged?.Invoke(CurrentGold);
    }

    // 골드가 충분한지 확인
    public bool HasEnoughGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            return EconomyManager.Instance.HasEnoughGold(amount);
        }
        return fallbackGold >= amount;
    }

    // 골드 소비
    public bool TrySpendGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            bool success = EconomyManager.Instance.SpendGold(amount);
            if (success)
            {
                Debug.Log($"[재화] {amount} 골드 소비! 남은 골드: {EconomyManager.Instance.CurrentGold}");
            }
            else
            {
                Debug.Log("[재화] 골드가 부족합니다!");
            }
            return success;
        }

        if (fallbackGold >= amount)
        {
            fallbackGold -= amount;
            Debug.Log($"[재화] {amount} 골드 소비! 남은 골드: {fallbackGold}");
            OnGoldChanged?.Invoke(fallbackGold);
            return true;
        }

        Debug.Log("[재화] 골드가 부족합니다!");
        return false;
    }

    // 골드 획득/환불
    public void AddGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(amount);
            Debug.Log($"[재화] {amount} 골드 획득/환불! 현재 골드: {EconomyManager.Instance.CurrentGold}");
        }
        else
        {
            fallbackGold += amount;
            Debug.Log($"[재화] {amount} 골드 획득/환불! 현재 골드: {fallbackGold}");
            OnGoldChanged?.Invoke(fallbackGold);
        }
    }
}
