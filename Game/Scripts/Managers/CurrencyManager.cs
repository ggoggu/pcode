using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("재화 설정")]
    [SerializeField] private int currentGold = 100; // 초기 보유 골드

    public int CurrentGold => currentGold;

    // 골드 변경 시 UI 등에 알릴 이벤트
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 시작 시 초기 골드 수치 방송
        OnGoldChanged?.Invoke(currentGold);
    }

    // 골드가 충분한지 확인
    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    // 골드 소비
    public bool TrySpendGold(int amount)
    {
        if (HasEnoughGold(amount))
        {
            currentGold -= amount;
            Debug.Log($"[재화] {amount} 골드 소비! 남은 골드: {currentGold}");
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        Debug.Log("[재화] 골드가 부족합니다!");
        return false;
    }

    // 골드 획득/환불
    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log($"[재화] {amount} 골드 획득/환불! 현재 골드: {currentGold}");
        OnGoldChanged?.Invoke(currentGold);
    }
}