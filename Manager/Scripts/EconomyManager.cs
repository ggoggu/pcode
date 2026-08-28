using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }
    public int CurrentGold { get; private set; }


    public int MaxGold { get; private set; } = 100; // 초기 임시값

    // 재화량이 변동될 때마다 UI 업데이트 등을 위해 호출할 이벤트 (MVVM 바인딩용)
    public event Action OnEconomyChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // GameManager와 동일한 게임 오브젝트에 붙이거나 별도 관리
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // GameManager의 상태 변경 이벤트를 구독합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            CurrentGold = 100;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        // 맵 전환 시 임시 재화(나무)는 소멸시키고, 영구 재화(골드)는 유지합니다[cite: 1].
        if (state == GameState.MapTransition)
        {
            OnEconomyChanged?.Invoke();
        }
    }
    

    // --- 골드(Gold) 관련 로직 ---
    public void AddGold(int amount)
    {
        // 획득 시 상한선을 초과하지 않도록 Clamp 처리합니다[cite: 1].
        CurrentGold = Mathf.Clamp(CurrentGold + amount, 0, MaxGold);
        OnEconomyChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        // 부적 뽑기, 펭귄 뽑기, 리롤 시 호출됩니다[cite: 1].
        if (CurrentGold >= amount)
        {
            CurrentGold -= amount;
            OnEconomyChanged?.Invoke();
            return true; // 소비 성공
        }
        return false; // 잔액 부족
    }

    // --- 상한선(Cap) 관리 로직 ---
    // 진행도에 따른 선형 증가 또는 경제 부적 획득 시 이 함수를 호출해 상한을 조작합니다[cite: 1].
    public void ModifyCaps(int addedGoldCap)
    {
        MaxGold += addedGoldCap;
        OnEconomyChanged?.Invoke();
    }
}
