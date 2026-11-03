using System;
using UnityEngine;

/// <summary>
/// GameManager의 데이터를 받아 HUDView가 사용할 포맷으로 가공하고 이벤트를 전달하는 ViewModel
/// </summary>
public class HUDViewModel
{
    // View(HUDView)에서 구독할 UI 바인딩용 이벤트
    public event Action<string> OnScoreTextChanged;
    public event Action<string> OnGoldTextChanged;
    public event Action<string> OnLifeTextChanged;
    public event Action<string> OnPhaseTextChanged;
    public event Action<bool> OnPhaseButtonInteractableChanged;
    public event Action<string> OnPhaseButtonTextChanged;

    public HUDViewModel()
    {
        // GameManager 정적 이벤트 구독
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnGoldChanged  += HandleGoldChanged;
        GameManager.OnLifeChanged  += HandleLifeChanged;
        GameManager.OnStateChanged += HandleStateChanged;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnWaveChanged += HandleWaveChanged;
        }
    }

    /// <summary>
    /// 객체 해제 시 이벤트 구독 해제 (메모리 누수 방지)
    /// </summary>
    public void Dispose()
    {
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnGoldChanged  -= HandleGoldChanged;
        GameManager.OnLifeChanged  -= HandleLifeChanged;
        GameManager.OnStateChanged -= HandleStateChanged;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnWaveChanged -= HandleWaveChanged;
        }
    }

    /// <summary>
    /// 초기 씬 진입 시 현재 GameManager 데이터를 기반으로 UI 갱신 요청
    /// </summary>
    public void RefreshInitialValues()
    {
        if (GameManager.Instance == null) return;

        HandleScoreChanged(GameManager.Instance.CurrentScore);
        HandleGoldChanged(GameManager.Instance.CurrentGold);
        HandleLifeChanged(GameManager.Instance.CurrentLife);
        HandleStateChanged(GameManager.Instance.CurrentState); // 초기 상태 반영

        if (MapManager.Instance != null)
        {
            HandleWaveChanged(MapManager.Instance.CurrentMapIndex, MapManager.Instance.CurrentWave);
        }
    }

    /// <summary>
    /// View에서 버튼 클릭 시 호출 (정비시간 -> 웨이브 시작)
    /// </summary>
    public void TogglePhase()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.StartNextWave();
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MaintenanceTime)
        {
            GameManager.Instance.ChangeState(GameState.WaveInProgress);
        }
        else
        {
            Debug.LogWarning("[HUDViewModel] 현재 상태에서는 웨이브를 시작할 수 없습니다.");
        }
    }

    // ==========================================
    // GameManager 이벤트 핸들러 및 데이터 가공
    // ==========================================

    private void HandleScoreChanged(int score)
    {
        OnScoreTextChanged?.Invoke($"SCORE: {score:N0}");
    }

    private void HandleGoldChanged(int gold)
    {
        OnGoldTextChanged?.Invoke($"{gold}");
    }

    private void HandleLifeChanged(int life)
    {
        int maxLife = GameManager.Instance != null ? GameManager.Instance.MaxLife : 5;
        OnLifeTextChanged?.Invoke($"{life} / {maxLife}");
    }

    private void HandleWaveChanged(int mapIndex, int waveIndex)
    {
        int maxWave = MapManager.Instance != null ? MapManager.Instance.MaxWavesPerMap : 5;
        string info = $"MAP {mapIndex} - WAVE {waveIndex}/{maxWave}";
        Debug.Log($"[HUDViewModel] {info}");
    }

    private void HandleStateChanged(GameState state)
    {
        string phaseName = state switch
        {
            GameState.MaintenanceTime => "SETUP PHASE",
            GameState.WaveInProgress  => "WAVE IN PROGRESS",
            GameState.MapTransition   => "MAP TRANSITION",
            GameState.GameOver        => "GAME OVER",
            GameState.GameClear       => "VICTORY! ALL WAVES CLEARED",
            _                         => state.ToString()
        };

        OnPhaseTextChanged?.Invoke(phaseName);

        // 기획서 §4-B: 정비시간에만 버튼 활성화 (웨이브 중에는 임의 탈출 불가)
        bool isMaintenance = (state == GameState.MaintenanceTime);
        OnPhaseButtonInteractableChanged?.Invoke(isMaintenance);

        string buttonLabel = state switch
        {
            GameState.MaintenanceTime => "START WAVE",
            GameState.WaveInProgress  => "IN BATTLE...",
            GameState.MapTransition   => "LOADING MAP...",
            _                         => "---"
        };
        OnPhaseButtonTextChanged?.Invoke(buttonLabel);
    }
}