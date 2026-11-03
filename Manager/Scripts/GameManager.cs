using System;
using UnityEngine;

public enum GameState
{
    None,
    MaintenanceTime, 
    WaveInProgress,  
    MapTransition,   
    GameOver,
    GameClear,
    Setup = MaintenanceTime,   
    Wave  = WaveInProgress     
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 상태")]
    [SerializeField] private GameState currentState = GameState.MaintenanceTime;

    [Header("점수 시스템")]
    [SerializeField] private int currentScore = 0;

    [Header("재화 시스템")]
    [Tooltip("골드: EconomyManager를 단일 진실의 원천(SSOT)으로 사용합니다.")]
    [SerializeField] private int currentGold = 100;

    [Header("플레이어 생명")]
    [SerializeField] private int maxLife = 5;
    [SerializeField] private int currentLife = 5;
    [SerializeField] private float lifeRegenInterval = 10f;
    private float regenTimer = 0f;
    
    #region ContextMenu 테스트 도구 (에디터 전용)

    // 인스펙터의 GameManager 컴포넌트 우클릭 ➔ 메뉴에서 바로 실행 가능!
    [ContextMenu("Test: Start Wave")]
    public void TestStartWave()
    {
        ChangeState(GameState.WaveInProgress);
        Debug.Log("[Test] 게임 상태 변경: WaveInProgress");
    }

    [ContextMenu("Test: Start Maintenance")]
    public void TestStartMaintenance()
    {
        ChangeState(GameState.MaintenanceTime);
        Debug.Log("[Test] 게임 상태 변경: MaintenanceTime");
    }

    [ContextMenu("Test: Game Over")]
    public void TestGameOver()
    {
        ChangeState(GameState.GameOver);
        Debug.Log("[Test] 게임 상태 변경: GameOver");
    }

    #endregion

    public event Action<GameState> OnGameStateChanged; 
    public static event Action<GameState> OnStateChanged;  
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnLifeChanged;

    public GameState CurrentState => currentState;
    public int CurrentScore => currentScore;
    public int CurrentGold => EconomyManager.Instance != null ? EconomyManager.Instance.CurrentGold : currentGold;
    public int CurrentLife => currentLife;
    public int MaxLife 
    {
        get 
        {
            if (StatModifierRegistry.Instance != null)
            {
                float modifiedLife = StatModifierRegistry.Instance.GetModifiedValue(StatType.MaxLife, maxLife);
                return Mathf.RoundToInt(modifiedLife);
            }
            return maxLife;
        }
    }

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
        else if (Instance != this)
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
        currentGold = newGold;
        OnGoldChanged?.Invoke(newGold);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            currentGold = EconomyManager.Instance.CurrentGold;
        }

        OnStateChanged?.Invoke(currentState);
        OnScoreChanged?.Invoke(currentScore);
        OnGoldChanged?.Invoke(currentGold);
        OnLifeChanged?.Invoke(currentLife);
    }

    private void Update() => HandleLifeRegenerate();

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        
        HandleStateChange(newState);
        OnGameStateChanged?.Invoke(newState);
        OnStateChanged?.Invoke(newState);
    }

    public void SetGameState(GameState newState) => ChangeState(newState);

    public void TogglePhase()
    {
        if (currentState == GameState.MaintenanceTime) ChangeState(GameState.WaveInProgress);
        else if (currentState == GameState.WaveInProgress) ChangeState(GameState.MaintenanceTime);
    }

    private void HandleStateChange(GameState state)
    {
        Time.timeScale = (state == GameState.GameOver || state == GameState.GameClear) ? 0f : 1f;
    }

    public void PauseGameForUI(bool isPaused) => Time.timeScale = isPaused ? 0f : 1f;
    
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    public void DecreaseLife(int amount = 1)
    {
        currentLife = Mathf.Max(0, currentLife - amount);
        OnLifeChanged?.Invoke(currentLife);
        if (currentLife <= 0) ChangeState(GameState.GameOver);
    }

    public void HealLife(int amount = 1)
    {
        if (currentLife >= maxLife) return;
        currentLife = Mathf.Min(maxLife, currentLife + amount);
        OnLifeChanged?.Invoke(currentLife);
    }

    public void HealFullLife()
    {
        HealLife(MaxLife);
    }

    private void HandleLifeRegenerate()
    {
        // 기획서 §4-E: 자연 회복은 웨이브 전투 중에만 진행된다 (정비시간 대기 회복 차단)
        if (currentState != GameState.WaveInProgress || currentLife >= maxLife)
        {
            regenTimer = 0f;
            return;
        }
        regenTimer += Time.deltaTime;
        if (regenTimer >= lifeRegenInterval)
        {
            regenTimer = 0f;
            HealLife(1);
        }
    }

    public void AddGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(amount);
        }
        else
        {
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }
    }

    public bool TrySpendGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            return EconomyManager.Instance.SpendGold(amount);
        }

        if (currentGold < amount) return false;
        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold);
        return true;
    }
}
