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

    public HUDViewModel()
    {
        // GameManager 정적 이벤트 구독
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnGoldChanged  += HandleGoldChanged;
        GameManager.OnLifeChanged  += HandleLifeChanged;
        GameManager.OnStateChanged += HandleStateChanged;
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
    }

    /// <summary>
    /// View에서 버튼 클릭 시 호출
    /// </summary>
    public void TogglePhase()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePhase();
        }
        else
        {
            Debug.LogWarning("[HUDViewModel] GameManager.Instance가 Null입니다!");
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

    private void HandleStateChanged(GameState state)
    {
        string phaseName = state switch
        {
            GameState.MaintenanceTime => "SETUP PHASE",
            GameState.WaveInProgress  => "WAVE PHASE",
            GameState.GameOver        => "GAME OVER",
            GameState.GameClear       => "GAME CLEAR",
            _                         => state.ToString()
        };

        OnPhaseTextChanged?.Invoke(phaseName);
    }
}