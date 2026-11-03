using System;
using System.Collections;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("런 계층 구조 설정 (기획서 §4-A: 3스테이지 × 5웨이브 = 15웨이브)")]
    [SerializeField] private int maxMaps = 3;
    [SerializeField] private int maxWavesPerMap = 5;

    // 현재 맵 및 웨이브 정보
    public int CurrentMapIndex { get; private set; } = 1;
    public int CurrentWave { get; private set; } = 1;

    public int MaxMaps => maxMaps;
    public int MaxWavesPerMap => maxWavesPerMap;

    /// <summary>
    /// 런 전체 기준 웨이브 인덱스 (0 ~ 14)
    /// </summary>
    public int GlobalWaveIndex => (CurrentMapIndex - 1) * maxWavesPerMap + (CurrentWave - 1);

    /// <summary>
    /// 기획서 §4-B: 런 최초 정비시간 (1맵 시작 전) 여부
    /// </summary>
    public bool IsFirstMaintenance => CurrentMapIndex == 1 && CurrentWave == 1 && 
                                     GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MaintenanceTime;

    // 적 생성 시 참고할 스케일링 계수
    public float CurrentDifficultyMultiplier => 1f + (CurrentMapIndex * 0.2f) + (CurrentWave * 0.1f);

    // 이벤트
    public event Action OnMapTransitionStarted;
    public event Action OnMapTransitionEnded;
    public event Action<int, int> OnWaveChanged;       // (mapIndex, waveIndex)
    public event Action<string> OnWaveGateFailed;       // 시작 불가 안내

    private Coroutine waveClearRoutine;

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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        OnWaveChanged?.Invoke(CurrentMapIndex, CurrentWave);
    }

    // --- 웨이브 흐름 제어 ---

    /// <summary>
    /// 정비시간 종료 후 웨이브 시작 (플레이어의 명시적 버튼 입력으로만 전이)
    /// </summary>
    public bool StartNextWave()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime)
        {
            Debug.LogWarning("[MapManager] 정비시간 중에만 웨이브를 시작할 수 있습니다.");
            return false;
        }

        // 기획서 §4-B 종료 게이트: 보유 펭귄 1마리 이상 제약
        if (PenguinDeckManager.Instance != null && PenguinDeckManager.Instance.OwnedPenguins.Count < 1)
        {
            string msg = "부화장에서 최소 1마리의 펭귄을 영입해야 웨이브를 시작할 수 있습니다!";
            Debug.LogWarning($"[MapManager] {msg}");
            OnWaveGateFailed?.Invoke(msg);
            return false;
        }

        Debug.Log($"[MapManager] 맵 {CurrentMapIndex} - 웨이브 {CurrentWave} (글로벌 #{GlobalWaveIndex + 1}) 전투 시작!");

        // GameManager 상태를 '전투 중'으로 변경
        GameManager.Instance.ChangeState(GameState.WaveInProgress);

        // 적 스포너 시작
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.StartWave(GlobalWaveIndex);
        }

        return true;
    }

    /// <summary>
    /// 적 스포너(EnemyManager)가 '필드의 모든 적 소멸'을 감지했을 때 호출
    /// </summary>
    public void OnWaveEnemiesDefeated()
    {
        if (waveClearRoutine != null) return;
        waveClearRoutine = StartCoroutine(WaveClearSequenceRoutine());
    }

    /// <summary>
    /// 기획서 §4-B: 회수 유예(2초) 및 웨이브 종료 일괄 처리 시퀀스
    /// </summary>
    private IEnumerator WaveClearSequenceRoutine()
    {
        Debug.Log($"[MapManager] 적 전멸! 회수 유예 2초 시작 (물리 및 발사/회수 유지)...");

        // ④ 회수 유예 (2초): 전투와 동일 (발사·회수 계속, 물리 진행)
        yield return new WaitForSeconds(2.0f);

        Debug.Log($"[MapManager] 회수 유예 종료 - 웨이브 종료 처리 일괄 실행");

        // --- 웨이브 종료 처리 (일괄) [확정] ---
        // 1. 필드 잔여 코인 전량 소멸 (미회수분 무정산)
        Coin.ClearAllFieldCoins();

        // 2 & 3. 모든 펭귄의 콤보·과부하 초기화 및 모든 리스폰 타이머 초기화 (전원 즉시 발사 가능 풀 복귀)
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.ResetAllPenguinsForWaveEnd();
        }

        // 4. 상점·부화장 자동 리롤 + 비용 초기화
        // (GameState.MaintenanceTime 진입 시 ShopManager가 자동으로 처리함)

        waveClearRoutine = null;

        // 5. 5웨이브였다면 맵 클리어, 아니면 정비시간 진입
        if (CurrentWave >= maxWavesPerMap)
        {
            if (CurrentMapIndex >= maxMaps)
            {
                // 기획서 §4-A, §4-F: 3맵 5웨이브 (총 15웨이브) 모두 클리어 시 게임 승리!
                Debug.Log("[MapManager] ★ 런 전체 15웨이브 클리어 달성! 게임 승리! ★");
                GameManager.Instance.ChangeState(GameState.GameClear);
            }
            else
            {
                // 다음 맵으로 전환 (5웨이브 뒤에는 정비시간이 붙지 않고 맵 전환 후의 시작 전 정비시간이 대신함)
                StartCoroutine(MapTransitionRoutine());
            }
        }
        else
        {
            // 아직 웨이브가 남았다면 다음 웨이브 번호 갱신 및 정비시간 진입
            CurrentWave++;
            OnWaveChanged?.Invoke(CurrentMapIndex, CurrentWave);
            GameManager.Instance.ChangeState(GameState.MaintenanceTime);
        }
    }

    /// <summary>
    /// 기획서 §4-B ① 맵 로드 / ⑤ 맵 클리어 연출
    /// </summary>
    private IEnumerator MapTransitionRoutine()
    {
        // 1. 상태를 '맵 전환 중'으로 변경
        GameManager.Instance.ChangeState(GameState.MapTransition);

        // 2. 맵 전환 이벤트 발생 (오브젝트 보관소 및 필드 설치물 전량 소멸)
        OnMapTransitionStarted?.Invoke();

        Debug.Log($"[MapManager] 맵 {CurrentMapIndex} 클리어! 다음 맵으로 전환 중...");

        // 3. 연출 대기 시간 (2초)
        yield return new WaitForSeconds(2.0f);

        // 4. 다음 맵 세팅
        CurrentMapIndex++;
        CurrentWave = 1;
        OnWaveChanged?.Invoke(CurrentMapIndex, CurrentWave);

        // 기획서 §4-B ①: 연출 종료 시 생명 전량 회복
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HealFullLife();
            Debug.Log("[MapManager] 새 맵 도착: 플레이어 생명 전량 회복 완료");
        }

        Debug.Log($"[MapManager] 맵 {CurrentMapIndex} 도착. 정비시간 시작.");

        OnMapTransitionEnded?.Invoke();

        // 5. 전환 완료 후 정비시간으로 돌입
        GameManager.Instance.ChangeState(GameState.MaintenanceTime);
    }
}
