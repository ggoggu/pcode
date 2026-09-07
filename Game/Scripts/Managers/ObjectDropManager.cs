using UnityEngine;
using Game.ObjectSystem;

public class ObjectDropManager : MonoBehaviour
{
    public static ObjectDropManager Instance { get; private set; }

    [Header("현재 웨이브/맵 습득 목표 설정")]
    [SerializeField] private int totalEnemiesInWave = 20; // 해당 웨이브의 총 적 수
    [SerializeField] private int targetDropCount = 3;     // 해당 웨이브에서 얻어야 할 목표 아이템 수

    [Header("드랍 가능 오브젝트 타입 비중")]
    [SerializeField] private ObjectType[] availableTypes = new ObjectType[] 
    { 
        ObjectType.Bumper, 
        ObjectType.Slingshot 
    };

    // 웨이브 진행 상태 변수
    private int killedEnemyCount = 0;
    private int currentDroppedCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 새로운 웨이브 시작 시 목표 및 상태 초기화
    /// </summary>
    public void InitializeWaveGoals(int totalEnemies, int targetDrops)
    {
        totalEnemiesInWave = Mathf.Max(1, totalEnemies);
        targetDropCount = Mathf.Clamp(targetDrops, 0, totalEnemiesInWave);
        
        killedEnemyCount = 0;
        currentDroppedCount = 0;

        Debug.Log($"[ObjectDropManager] 새 웨이브 시작 - 총 적: {totalEnemiesInWave}명 / 목표 드랍: {targetDropCount}개");
    }

    /// <summary>
    /// 적이 '플레이어 공격/기믹에 의해 처치'되었을 때만 호출
    /// </summary>
    public void OnEnemyKilledByPlayer(Vector3 enemyPosition)
    {
        killedEnemyCount++;

        // 1회 처치당 최대 1개 제한 및 목표 달성 여부 확인
        if (currentDroppedCount < targetDropCount)
        {
            float dropProbability = CalculateCurrentDropProbability();
            float randomRoll = Random.value; // 0.0f ~ 1.0f

            if (randomRoll <= dropProbability)
            {
                ExecuteDirectAcquisition();
            }
        }
    }

    /// <summary>
    /// 적이 '베이스 도달' 또는 '자연 퇴장'하여 소멸했을 때 호출 (습득 판정 제외)
    /// </summary>
    public void OnEnemyReachedBaseOrDespawned()
    {
        // 처치된 적 수에는 포함하여 남은 적 대비 확률 계산이 틀어지지 않도록 함
        killedEnemyCount++;
        // 드랍 시도는 하지 않고 무시
    }

    /// <summary>
    /// 목표 습득량 기반 동적 드랍 확률 계산
    /// </summary>
    private float CalculateCurrentDropProbability()
    {
        int remainingEnemies = totalEnemiesInWave - (killedEnemyCount - 1); // 현재 적 포함 남은 수
        int remainingDrops = targetDropCount - currentDroppedCount;

        if (remainingEnemies <= 0 || remainingDrops <= 0) return 0f;

        // 남은 적 수보다 남은 목표 드랍 수가 많거나 같으면 100% 드랍
        if (remainingEnemies <= remainingDrops) return 1f;

        return (float)remainingDrops / remainingEnemies;
    }

    /// <summary>
    /// 필드 드랍(아이템 줍기)을 경유하지 않고 보관소로 즉시 진입 (§2, §3)
    /// </summary>
    private void ExecuteDirectAcquisition()
    {
        if (availableTypes == null || availableTypes.Length == 0) return;

        // 드랍할 오브젝트 타입 무작위 선택 (필요 시 가중치 적용 가능)
        ObjectType selectedType = availableTypes[Random.Range(0, availableTypes.Length)];

        // 보관소 데이터 생성 및 즉시 진입 (1회당 정확히 1개)
        ObjectItem newItem = new ObjectItem(selectedType);
        
        if (ObjectStorageManager.Instance != null)
        {
            ObjectStorageManager.Instance.AddObject(newItem);
            currentDroppedCount++;
            
            Debug.Log($"[ObjectDropManager] 습득 성공! 타입: {selectedType} (현재 웨이브 습득량: {currentDroppedCount}/{targetDropCount})");
        }
    }
}