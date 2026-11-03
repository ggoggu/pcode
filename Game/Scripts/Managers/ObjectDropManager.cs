using System.Collections.Generic;
using UnityEngine;
using Game.ObjectSystem;

public class ObjectDropManager : MonoBehaviour
{
    public static ObjectDropManager Instance { get; private set; }

    [System.Serializable]
    public struct ObjectDropWeightEntry
    {
        public ObjectType type;
        [Range(0, 100)] public int weight;
        public bool isUnique; // 유니크 오브젝트 (예: CloneGate)

        public ObjectDropWeightEntry(ObjectType type, int weight, bool isUnique = false)
        {
            this.type = type;
            this.weight = weight;
            this.isUnique = isUnique;
        }
    }

    [Header("현재 웨이브/맵 습득 목표 설정")]
    [SerializeField] private int totalEnemiesInWave = 20; // 해당 웨이브의 총 적 수
    [SerializeField] private int targetDropCount = 3;     // 해당 웨이브에서 얻어야 할 목표 아이템 수

    [Header("오브젝트 종류별 가중치 드랍 테이블 (기획 §2 & 워크북 04_오브젝트)")]
    [SerializeField] private List<ObjectDropWeightEntry> dropWeights = new List<ObjectDropWeightEntry>()
    {
        new ObjectDropWeightEntry(ObjectType.Bumper, 60, false),
        new ObjectDropWeightEntry(ObjectType.Slingshot, 40, false),
        new ObjectDropWeightEntry(ObjectType.CloneGate, 5, true)
    };

    [Header("호환용 기본 타입 배열 (dropWeights가 비었을 때 Fallback)")]
    [SerializeField] private ObjectType[] availableTypes = new ObjectType[] 
    { 
        ObjectType.Bumper, 
        ObjectType.Slingshot 
    };

    // 웨이브 진행 상태 변수
    private int killedEnemyCount = 0;
    private int currentDroppedCount = 0;

    public int TotalEnemiesInWave => totalEnemiesInWave;
    public int TargetDropCount => targetDropCount;
    public int CurrentDroppedCount => currentDroppedCount;
    public int KilledEnemyCount => killedEnemyCount;
    public float CurrentDropProbability => CalculateCurrentDropProbability();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 새로운 웨이브 시작 시 목표 및 상태 초기화 (§2 웨이브·맵당 습득량)
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
    /// 적이 '베이스 도달' 또는 '자연 퇴장'하여 소멸했을 때 호출
    /// 기획서 §2: "베이스 도달·퇴장으로 소멸한 개체의 몫은 다른 개체에 재배분하지 않으며,
    /// 그만큼 실제 습득량은 미달로 끝난다."
    /// </summary>
    public void OnEnemyReachedBaseOrDespawned()
    {
        // 소멸 개체의 카운트는 누적하되, 기회를 다른 적에 재배분하지 않음
        killedEnemyCount++;
    }

    /// <summary>
    /// 목표 습득량 기반 확률 산출식 (§2)
    /// 총 적 수 대비 목표 습득량의 고유 기댓값 비율을 산출합니다.
    /// 목표치를 모두 달성했거나 남은 목표가 없으면 0을 반환합니다.
    /// </summary>
    private float CalculateCurrentDropProbability()
    {
        if (currentDroppedCount >= targetDropCount) return 0f;
        if (totalEnemiesInWave <= 0) return 0f;

        // 적 전량 처치 시 정확히 targetDropCount 기댓값이 되도록 기본 확률 유지
        // 베이스 도달 개체의 몫을 남은 적에게 몰아주지 않음 (기획 §2 준수)
        return Mathf.Clamp01((float)targetDropCount / totalEnemiesInWave);
    }

    /// <summary>
    /// 필드 드랍(아이템 줍기)을 경유하지 않고 보관소로 즉시 진입 (§1, §2, §3)
    /// </summary>
    private void ExecuteDirectAcquisition()
    {
        ObjectType selectedType = SelectObjectType();

        // 보관소 데이터 생성 및 즉시 진입 (1회당 정확히 1개)
        ObjectItem newItem = new ObjectItem(selectedType);
        
        if (ObjectStorageManager.Instance != null)
        {
            ObjectStorageManager.Instance.AddObject(newItem);
            currentDroppedCount++;
            
            Debug.Log($"[ObjectDropManager] 습득 성공! 타입: {selectedType} (현재 웨이브 습득량: {currentDroppedCount}/{targetDropCount})");
        }
    }

    /// <summary>
    /// 가중치 테이블 또는 기본 타입 배열 기반 오브젝트 종류 추첨
    /// </summary>
    private ObjectType SelectObjectType()
    {
        if (dropWeights != null && dropWeights.Count > 0)
        {
            int totalWeight = 0;
            foreach (var entry in dropWeights)
            {
                totalWeight += Mathf.Max(0, entry.weight);
            }

            if (totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int cumulative = 0;
                foreach (var entry in dropWeights)
                {
                    cumulative += Mathf.Max(0, entry.weight);
                    if (roll < cumulative)
                    {
                        return entry.type;
                    }
                }
            }
        }

        // Fallback: availableTypes 균등 추첨
        if (availableTypes != null && availableTypes.Length > 0)
        {
            return availableTypes[Random.Range(0, availableTypes.Length)];
        }

        return ObjectType.Bumper;
    }
}