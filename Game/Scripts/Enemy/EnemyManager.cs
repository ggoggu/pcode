using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("드랍 재화 설정 (글로벌 관리)")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int defaultCoinSplitCount = 3; // 기획서: 기본 3개 분할 생성
    [SerializeField] private LayerMask obstacleLayerMask;   // 벽·오브젝트 겹침 판정용 레이어 마스크
    public GameObject CoinPrefab => coinPrefab;

    [Header("적 프리팹 목록")]
    [SerializeField] private Enemy normalPrefab;
    [SerializeField] private Enemy seagullPrefab;
    [SerializeField] private Enemy shrinkPrefab;
    [SerializeField] private Enemy comboResetPrefab;
    [SerializeField] private Enemy bossPrefab;

    [Header("경로 목록 연동 (ID = 배열 인덱스 순서)")]
    [Tooltip("pathId 0번은 이 배열의 0번, 1번은 1번... 순서대로 등록하세요")]
    [SerializeField] private CatmullRomPath[] paths;

    [Header("사전 경로 가늠선 표시 설정")]
    [Tooltip("정비 시간에 다음 웨이브 경로를 미리 표시할지 여부")]
    [SerializeField] private bool showPathPreviewDuringMaintenance = true;
    [SerializeField] private float pathPreviewAlpha = 0.4f;
    [SerializeField] private Color pathPreviewStartColor = new Color(1f, 1f, 0f, 0.4f);
    [SerializeField] private Color pathPreviewEndColor = new Color(1f, 0.5f, 0f, 0.4f);
    [SerializeField] private float pathPreviewWidth = 0.15f;
    [SerializeField] private int pathPreviewStepsPerSegment = 20;

    [Header("웨이브 스케일링 설정")]
    [SerializeField] private float hpScaleFactorPerWave = 1.15f;

    [Header("스폰 겹침 방지 설정")]
    [Tooltip("스폰 시작 지점 최소 확보 반경 (이 반경 내에 적이 머물러 있으면 다음 적 스폰 일시 대기)")]
    [SerializeField] private float spawnClearanceRadius = 1.4f;
    [Tooltip("스폰 대기 최대 제한 시간 (초 단위, 스폰 영구 지연 방지)")]
    [SerializeField] private float maxSpawnWaitDuration = 2.0f;

    public enum EnemyType { Normal, Seagull, Shrink, ComboReset, Boss }

    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("웨이브 시작 후 스폰될 경과 시간 (초)")]
        public float spawnTime;

        [Tooltip("스폰할 적 종류")]
        public EnemyType enemyType;

        [Tooltip("스폰 수량")]
        public int count = 1;

        [Tooltip("연속 스폰 시 적 간의 생성 간격 (초)")]
        public float interval = 0.5f;

        [Tooltip("이 적이 따라갈 경로 ID (paths 배열의 인덱스)")]
        public int pathId;
    }

    [Serializable]
    public class WaveData
    {
        public string waveName = "Wave";

        [Tooltip("이 웨이브에서 적 전체가 드랍할 총 골드 양")]
        public int waveTotalGold = 100;

        [Tooltip("이 웨이브에서 적 처치로 획득 가능한 목표 오브젝트 수 (기획 §2 & 워크북 04_오브젝트)")]
        public int targetObjectDropCount = 3;

        public List<EnemySpawnEntry> spawnEntries = new List<EnemySpawnEntry>();

        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (var entry in spawnEntries) total += entry.count;
            return total;
        }

        public HashSet<int> GetUsedPathIds()
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (var entry in spawnEntries) ids.Add(entry.pathId);
            return ids;
        }
    }

    [Header("웨이브 목록 설정")]
    [SerializeField] private List<WaveData> waveDataList = new List<WaveData>();

    private readonly List<Enemy> activeEnemies = new List<Enemy>();
    public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;

    private readonly List<NormalEnemyAI> activeGroundEnemies = new List<NormalEnemyAI>();
    public IReadOnlyList<NormalEnemyAI> ActiveGroundEnemies => activeGroundEnemies;

    private void Update()
    {
        UpdateGroundEnemiesTracking();
    }

    /// <summary>
    /// 시간적 일관성(Temporal Coherence) 기반 1D Sweep and Prune 정렬 (Cohen et al., 1995).
    /// 거의 정렬된 배열에 삽입 정렬을 적용하여 O(N)으로 유지하며,
    /// 각 지상 적에게 바로 앞선 적(LeadingEnemy)을 O(1)로 매핑합니다.
    /// Zero GC-allocation hot-path 메서드입니다.
    /// </summary>
    private void UpdateGroundEnemiesTracking()
    {
        int count = activeGroundEnemies.Count;
        if (count <= 1)
        {
            if (count == 1 && activeGroundEnemies[0] != null)
            {
                activeGroundEnemies[0].LeadingEnemy = null;
            }
            return;
        }

        // 1. null / 비활성 / 사망 적 정리 (in-place 역방향 순회)
        for (int i = count - 1; i >= 0; i--)
        {
            var e = activeGroundEnemies[i];
            if (e == null || e.IsDead || !e.gameObject.activeInHierarchy)
            {
                activeGroundEnemies.RemoveAt(i);
            }
        }

        count = activeGroundEnemies.Count;
        if (count <= 1)
        {
            if (count == 1 && activeGroundEnemies[0] != null)
            {
                activeGroundEnemies[0].LeadingEnemy = null;
            }
            return;
        }

        // 2. 1D Insertion Sort: PathProgress 내림차순 (경로 앞선 적이 낮은 인덱스로)
        for (int i = 1; i < count; i++)
        {
            NormalEnemyAI current = activeGroundEnemies[i];
            float currentProgress = current.PathProgress;
            int j = i - 1;

            while (j >= 0 && activeGroundEnemies[j].PathProgress < currentProgress)
            {
                activeGroundEnemies[j + 1] = activeGroundEnemies[j];
                j--;
            }
            activeGroundEnemies[j + 1] = current;
        }

        // 3. 앞선 적(LeadingEnemy) 매핑: 동일 경로(CurrentPath) 상의 바로 앞 적 연결
        for (int i = 0; i < count; i++)
        {
            NormalEnemyAI self = activeGroundEnemies[i];
            NormalEnemyAI lead = null;

            for (int k = i - 1; k >= 0; k--)
            {
                NormalEnemyAI candidate = activeGroundEnemies[k];
                if (candidate != null && candidate.CurrentPath == self.CurrentPath)
                {
                    lead = candidate;
                    break;
                }
            }

            self.LeadingEnemy = lead;
        }
    }

    private int globalWaveIndex = 0;
    private int currentWaveIndex = 0;

    private bool isSpawning = false;
    private int totalWaveEnemies = 0;
    private int processedEnemyCount = 0;

    private readonly Dictionary<int, LineRenderer> previewLines = new Dictionary<int, LineRenderer>();
    private Material previewMaterial;

    public event Action OnWaveComplete;
    public event Action OnAllWavesComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 장애물 감지 레이어 미할당 시 기본 레이어(Default, Bumper) 자동 구성
        if (obstacleLayerMask.value == 0)
        {
            obstacleLayerMask = LayerMask.GetMask("Default", "Bumper");
            if (obstacleLayerMask.value == 0)
            {
                obstacleLayerMask = 1; // Fallback: Default layer
            }
        }

        // URP 및 Built-in 동시 대응 셰이더 자동 검색
        Shader targetShader = GetUniversalShader();
        previewMaterial = new Material(targetShader);
    }

    private Shader GetUniversalShader()
    {
        // 1. URP 2D Unlit
        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s != null) return s;

        // 2. URP Unlit
        s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s != null) return s;

        // 3. Sprites Default
        s = Shader.Find("Sprites/Default");
        if (s != null) return s;

        // 4. Fallback Standard
        return Shader.Find("Unlit/Color");
    }

    private void OnDestroy()
    {
        if (previewMaterial != null) Destroy(previewMaterial);
    }

    private void OnEnable()  => GameManager.OnStateChanged += HandleStateChanged;
    private void OnDisable() => GameManager.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.WaveInProgress || newState == GameState.Wave)
        {
            int targetIndex = MapManager.Instance != null ? MapManager.Instance.GlobalWaveIndex : currentWaveIndex;
            StartWave(targetIndex);
        }
        else if (newState == GameState.MaintenanceTime)
        {
            PrepareMaintenancePhase();
        }
        else if (newState == GameState.GameOver || newState == GameState.GameClear)
        {
            StopSpawning();
            ClearAllEnemies();
        }
    }

    private void PrepareMaintenancePhase()
    {
        StopSpawning();
        HideAllPathPreviews();

        int nextIndex = MapManager.Instance != null ? MapManager.Instance.GlobalWaveIndex : currentWaveIndex;
        if (nextIndex < waveDataList.Count)
        {
            WaveData nextWave = waveDataList[nextIndex];
            HashSet<int> usedPathIds = nextWave.GetUsedPathIds();
            ShowPathPreviews(usedPathIds);
        }
    }

    public void StartWave(int waveIndex)
    {
        if (isSpawning) return;

        if (waveIndex >= waveDataList.Count)
        {
            Debug.Log("[EnemyManager] 모든 웨이브가 완료되었습니다!");
            OnAllWavesComplete?.Invoke();
            return;
        }

        HideAllPathPreviews();

        currentWaveIndex = waveIndex;
        WaveData wave = waveDataList[waveIndex];

        // 웨이브별 목표 습득량 동적 초기화 (기획 §2)
        if (ObjectDropManager.Instance != null)
        {
            ObjectDropManager.Instance.InitializeWaveGoals(wave.GetTotalEnemyCount(), wave.targetObjectDropCount);
        }

        StartCoroutine(SpawnWaveTimeScheduleRoutine(wave));
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        isSpawning = false;
    }

    private IEnumerator SpawnWaveTimeScheduleRoutine(WaveData wave)
    {
        isSpawning = true;
        processedEnemyCount = 0;
        totalWaveEnemies = wave.GetTotalEnemyCount();

        float goldPerEnemy = totalWaveEnemies > 0 ? (float)wave.waveTotalGold / totalWaveEnemies : 0f;
        float hpMultiplier = Mathf.Pow(hpScaleFactorPerWave, globalWaveIndex);

        float waveTimer = 0f;
        List<EnemySpawnEntry> remainingEntries = new List<EnemySpawnEntry>(wave.spawnEntries);
        remainingEntries.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        int nextIndex = 0;

        while (nextIndex < remainingEntries.Count)
        {
            waveTimer += Time.deltaTime;

            while (nextIndex < remainingEntries.Count &&
                   waveTimer >= remainingEntries[nextIndex].spawnTime)
            {
                EnemySpawnEntry entry = remainingEntries[nextIndex];
                nextIndex++;

                StartCoroutine(ExecuteSpawnEntry(entry, goldPerEnemy, hpMultiplier));
            }

            yield return null;
        }

        isSpawning = false;

        yield return new WaitUntil(() => processedEnemyCount >= totalWaveEnemies);

        Debug.Log($"[EnemyManager] {wave.waveName} 완료!");
        OnWaveComplete?.Invoke();

        globalWaveIndex++;
        currentWaveIndex++;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnWaveEnemiesDefeated();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MaintenanceTime);
        }
    }

    private IEnumerator ExecuteSpawnEntry(EnemySpawnEntry entry, float goldPerEnemy, float hpMultiplier)
    {
        for (int i = 0; i < entry.count; i++)
        {
            if (spawnClearanceRadius > 0f)
            {
                float waitedTime = 0f;
                const float checkStep = 0.15f;
                while (IsSpawnPointBlocked(entry.pathId, spawnClearanceRadius) && waitedTime < maxSpawnWaitDuration)
                {
                    yield return new WaitForSeconds(checkStep);
                    waitedTime += checkStep;
                }
            }

            SpawnEnemy(entry.enemyType, entry.pathId, goldPerEnemy, hpMultiplier);

            if (entry.interval > 0f)
                yield return new WaitForSeconds(entry.interval);
        }
    }

    /// <summary>
    /// 지정된 경로의 스폰 지점 반경 내에 이미 다른 적이 존재하는지 검사합니다.
    /// </summary>
    public bool IsSpawnPointBlocked(int pathId, float clearanceRadius)
    {
        CatmullRomPath path = GetPathById(pathId);
        if (path == null) return false;

        Vector3 spawnPos = path.EvaluateAtProgress(0f);
        float radiusSqr = clearanceRadius * clearanceRadius;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            Vector3 diff = enemy.transform.position - spawnPos;
            diff.y = 0f;
            if (diff.sqrMagnitude < radiusSqr)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnEnemy(EnemyType type, int pathId, float goldPerEnemy, float hpMultiplier)
    {
        Enemy prefab = type switch
        {
            EnemyType.Normal     => normalPrefab,
            EnemyType.Seagull    => seagullPrefab,
            EnemyType.Shrink     => shrinkPrefab,
            EnemyType.ComboReset => comboResetPrefab,
            EnemyType.Boss       => bossPrefab,
            _                    => null
        };

        if (prefab == null)
        {
            Debug.LogWarning($"[EnemyManager] {type} 타입의 프리팹이 등록되지 않았습니다.");
            return;
        }

        CatmullRomPath path = GetPathById(pathId);
        if (path == null)
        {
            Debug.LogWarning($"[EnemyManager] ID {pathId}에 해당하는 경로를 찾을 수 없습니다.");
            return;
        }

        Vector3 spawnPos = path.EvaluateAtProgress(0f);
        Enemy instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        instance.SetBaseGold(goldPerEnemy);
        instance.ApplyHpMultiplier(hpMultiplier);

        instance.SendMessage("SetPath", path, SendMessageOptions.DontRequireReceiver);

        instance.OnDeath += HandleEnemyDeath;
        instance.OnBaseReached += HandleEnemyBaseReached;

        activeEnemies.Add(instance);
        if (instance is NormalEnemyAI groundAI)
        {
            activeGroundEnemies.Add(groundAI);
        }
    }

    /// <summary>
    /// 단일 좌표 기준 레거시 코인 드랍 (외부/부적 호환용)
    /// </summary>
    public void DropCoin(Vector3 position)
    {
        DropCoinsAtPosition(position, 0.5f, 1, 10);
    }

    /// <summary>
    /// 적 처치 시 기획서(§6)에 따른 기본 3개 분할 코인 드랍
    /// </summary>
    public void DropCoins(Enemy enemy)
    {
        if (enemy == null) return;

        float radius = GetEnemyCollisionRadius(enemy);
        int totalGold = Mathf.RoundToInt(enemy.BaseGold);
        int splitCount = Mathf.Max(1, defaultCoinSplitCount);

        DropCoinsAtPosition(enemy.transform.position, radius, splitCount, totalGold);
    }

    /// <summary>
    /// 지정된 반경 내에 코인을 분할 생성하고 골드를 배분합니다.
    /// </summary>
    public void DropCoinsAtPosition(Vector3 center, float radius, int splitCount, int totalGold)
    {
        if (coinPrefab == null || splitCount <= 0) return;

        int baseGoldPerCoin = totalGold / splitCount;
        int remainder = totalGold % splitCount;

        for (int i = 0; i < splitCount; i++)
        {
            int goldForThisCoin = baseGoldPerCoin + (i < remainder ? 1 : 0);
            Vector3 spawnPos = FindSafeCoinSpawnPosition(center, radius);

            GameObject coinInstance = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            if (coinInstance.TryGetComponent<Coin>(out var coinComp))
            {
                coinComp.SetGoldValue(goldForThisCoin);
            }
        }
    }

    /// <summary>
    /// 적의 충돌 반경 내에서 벽 및 오브젝트와 겹치지 않는 안전한 코인 스폰 지점을 탐색합니다.
    /// [미확정 처리]: 최대 10회 재추첨 후 미확보 시 중심 지점을 폴백으로 사용합니다.
    /// </summary>
    private Vector3 FindSafeCoinSpawnPosition(Vector3 center, float radius)
    {
        const int maxAttempts = 10;
        const float coinCheckRadius = 0.2f;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = new Vector3(center.x + randomCircle.x, center.y + 0.2f, center.z + randomCircle.y);

            // 벽·오브젝트 레이어와 충돌 여부 검사 (Trigger는 무시)
            if (!Physics.CheckSphere(candidate, coinCheckRadius, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
        }

        // 상한 도달 시 fallback: 적 중심 높이 보정 지점
        return new Vector3(center.x, center.y + 0.2f, center.z);
    }

    /// <summary>
    /// 적의 콜라이더 형태에 따른 실제 X-Z 평면 충돌 반경을 계산합니다.
    /// </summary>
    private float GetEnemyCollisionRadius(Enemy enemy)
    {
        if (enemy == null) return 0.5f;

        Collider col = enemy.MainCollider;
        if (col is CapsuleCollider cc)
        {
            float maxScale = Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.z);
            return Mathf.Max(0.3f, cc.radius * maxScale);
        }
        if (col is SphereCollider sc)
        {
            float maxScale = Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.z);
            return Mathf.Max(0.3f, sc.radius * maxScale);
        }
        if (col != null)
        {
            return Mathf.Max(0.3f, Mathf.Max(col.bounds.extents.x, col.bounds.extents.z));
        }

        return 0.5f;
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        if (enemy != null)
        {
            UnregisterEnemy(enemy);
            DropCoins(enemy);
            OnEnemyProcessed();
        }
    }

    private void HandleEnemyBaseReached(Enemy enemy)
    {
        if (enemy != null)
        {
            UnregisterEnemy(enemy);
            OnEnemyProcessed();
        }
    }

    private void UnregisterEnemy(Enemy enemy)
    {
        enemy.OnDeath -= HandleEnemyDeath;
        enemy.OnBaseReached -= HandleEnemyBaseReached;
        activeEnemies.Remove(enemy);
        if (enemy is NormalEnemyAI groundAI)
        {
            activeGroundEnemies.Remove(groundAI);
        }
    }

    private void OnEnemyProcessed()
    {
        processedEnemyCount++;
    }

    public void ClearAllEnemies()
    {
        StopSpawning();
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                Destroy(activeEnemies[i].gameObject);
            }
        }
        activeEnemies.Clear();
        activeGroundEnemies.Clear();
    }

    public int GetActiveEnemyCount() => activeEnemies.Count;

    private CatmullRomPath GetPathById(int pathId)
    {
        if (paths == null || pathId < 0 || pathId >= paths.Length)
            return null;
        return paths[pathId];
    }

    private void ShowPathPreviews(HashSet<int> pathIds)
    {
        if (!showPathPreviewDuringMaintenance) return;
        if (previewMaterial == null) return;

        Color startColor = pathPreviewStartColor;
        startColor.a = pathPreviewAlpha;
        Color endColor = pathPreviewEndColor;
        endColor.a = pathPreviewAlpha;

        foreach (int id in pathIds)
        {
            CatmullRomPath path = GetPathById(id);
            if (path == null || !path.IsValid) continue;

            if (previewLines.ContainsKey(id)) continue;

            GameObject lineObj = new GameObject($"PathPreview_{id}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = pathPreviewWidth;
            lr.endWidth = pathPreviewWidth;
            lr.startColor = startColor;
            lr.endColor = endColor;
            lr.material = previewMaterial;
            lr.sortingOrder = 10;

            List<Vector3> points = new List<Vector3>();
            int segments = path.SegmentCount;
            int stepsPerSegment = Mathf.Max(1, pathPreviewStepsPerSegment);

            for (int seg = 0; seg < segments; seg++)
            {
                for (int i = 0; i < stepsPerSegment; i++)
                {
                    float t = (float)i / stepsPerSegment;
                    points.Add(path.Evaluate(seg, t));
                }
            }
            if (segments > 0)
            {
                points.Add(path.Evaluate(segments - 1, 1f));
            }

            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());

            previewLines[id] = lr;
        }
    }

    private void HideAllPathPreviews()
    {
        foreach (var kv in previewLines)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        previewLines.Clear();
    }
}