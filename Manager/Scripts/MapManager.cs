using System;
using System.Collections;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // 현재 맵 및 웨이브 정보
    public int CurrentMapIndex { get; private set; } = 1;
    public int CurrentWave { get; private set; } = 1;

    // 맵당 배정된 총 웨이브 수 (기획상 3~5 웨이브)
    public int MaxWavesPerMap { get; private set; } = 3; 

    // 적 생성 시 참고할 스케일링 계수 (프로그래머 C가 참조)
    public float CurrentDifficultyMultiplier => 1f + (CurrentMapIndex * 0.2f) + (CurrentWave * 0.1f);

    // 맵 전환 연출 관련 이벤트
    public event Action OnMapTransitionStarted;
    public event Action OnMapTransitionEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- 웨이브 흐름 제어 ---
    
    // UI의 '웨이브 시작' 버튼을 누르거나 정비시간이 끝났을 때 호출
    public void StartNextWave()
    {
        Debug.Log($"[MapManager] 맵 {CurrentMapIndex} - 웨이브 {CurrentWave} 시작!");
        
        // GameManager 상태를 '전투 중'으로 변경
        GameManager.Instance.ChangeState(GameState.WaveInProgress);
    }

    // 프로그래머 C(적 스포너)가 '모든 적 처치'를 감지하면 이 함수를 호출해 줍니다.
    public void OnWaveCleared()
    {
        Debug.Log($"[MapManager] 웨이브 {CurrentWave} 클리어!");

        if (CurrentWave >= MaxWavesPerMap)
        {
            // 해당 맵의 마지막 웨이브를 깼다면 맵 전환 시작
            StartCoroutine(MapTransitionRoutine());
        }
        else
        {
            // 아직 웨이브가 남았다면 다음 웨이브 준비 및 정비시간 진입
            CurrentWave++;
            GameManager.Instance.ChangeState(GameState.MaintenanceTime);
        }
    }

    // --- 맵 전환 연출 (코루틴) ---
    private IEnumerator MapTransitionRoutine()
    {
        // 1. 상태를 '맵 전환 중'으로 변경 (이때 나무 재화는 EconomyManager가 자동으로 0으로 초기화함)[cite: 1]
        GameManager.Instance.ChangeState(GameState.MapTransition);
        
        // 2. 맵 전환 이벤트 발생 (이때 설치된 핀볼 오브젝트들을 파괴하도록 연결)
        OnMapTransitionStarted?.Invoke();

        Debug.Log("[MapManager] 다음 맵이 아래에서 올라오는 연출 시작...");

        // 3. 실제 연출 대기 시간 (예: 2초 동안 카메라 이동 또는 맵 프리팹 이동)
        // 프로그래머 C나 연출 담당자가 이 시간 동안 y축 트윈 애니메이션을 실행합니다[cite: 1].
        yield return new WaitForSeconds(2.0f);

        // 4. 다음 맵 세팅
        CurrentMapIndex++;
        CurrentWave = 1;
        // MaxWavesPerMap = Random.Range(3, 6); // 다음 맵의 웨이브 수를 3~5로 재설정 가능[cite: 1]

        Debug.Log($"[MapManager] 맵 {CurrentMapIndex} 도착. 정비시간 시작.");

        OnMapTransitionEnded?.Invoke();

        // 5. 전환 완료 후 정비시간으로 돌입[cite: 1]
        GameManager.Instance.ChangeState(GameState.MaintenanceTime);
    }
}
