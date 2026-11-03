using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("초기 재화 설정")]
    [SerializeField] private int initialGold = 100;
    [SerializeField] private int maxGold = 100;

    public int CurrentGold { get; private set; } = 100;
    public int MaxGold { get; private set; } = 100;

    // 재화량이 변동될 때마다 UI 업데이트 등을 위해 호출할 이벤트 (MVVM 바인딩용)
    public event Action OnEconomyChanged;
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MaxGold = maxGold;
            CurrentGold = Mathf.Clamp(initialGold, 0, MaxGold);

            if (Application.isPlaying)
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }

    private void Start()
    {
        // GameManager의 상태 변경 이벤트를 구독합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // 게임 시작 시 모든 구독자(상점, 부화장, HUD 등)에게 초기 재화 상태 브로드캐스트 보장
        NotifyEconomyChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        // 맵 전환 시 이벤트 브로드캐스트
        if (state == GameState.MapTransition)
        {
            NotifyEconomyChanged();
        }
    }

    public void NotifyEconomyChanged()
    {
        OnEconomyChanged?.Invoke();
        OnGoldChanged?.Invoke(CurrentGold);
    }

    // --- 골드(Gold) 관련 로직 ---
    public void AddGold(int amount)
    {
        // 획득 시 상한선을 초과하지 않도록 Clamp 처리합니다.
        CurrentGold = Mathf.Clamp(CurrentGold + amount, 0, MaxGold);
        NotifyEconomyChanged();
    }

    public bool SpendGold(int amount)
    {
        // 부적 뽑기, 펭귄 뽑기, 리롤 시 호출됩니다.
        if (CurrentGold >= amount)
        {
            CurrentGold -= amount;
            NotifyEconomyChanged();
            return true; // 소비 성공
        }
        return false; // 잔액 부족
    }

    public bool TrySpendGold(int amount) => SpendGold(amount);
    public bool HasEnoughGold(int amount) => CurrentGold >= amount;

    // --- 상한선(Cap) 관리 로직 ---
    // 진행도에 따른 선형 증가 또는 경제 부적 획득 시 이 함수를 호출해 상한을 조작합니다.
    public void ModifyCaps(int addedGoldCap)
    {
        MaxGold += addedGoldCap;
        NotifyEconomyChanged();
    }

    public void SetGold(int amount)
    {
        CurrentGold = Mathf.Clamp(amount, 0, MaxGold);
        NotifyEconomyChanged();
    }
}
