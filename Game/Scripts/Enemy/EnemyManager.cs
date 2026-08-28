using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("드랍 재화 설정 (글로벌 관리)")]
    [SerializeField] private GameObject coinPrefab;

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
            StartWave(currentWaveIndex);
        }
        else if (newState == GameState.MaintenanceTime)
        {
            PrepareMaintenancePhase();
        }
        else if (newState == GameState.GameOver)
        {
            StopSpawning();
        }
    }

    private void PrepareMaintenancePhase()
    {
        StopSpawning();
        HideAllPathPreviews();

        if (currentWaveIndex < waveDataList.Count)
        {
            WaveData nextWave = waveDataList[currentWaveIndex];
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
        StartCoroutine(SpawnWaveTimeScheduleRoutine(waveDataList[waveIndex]));
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MaintenanceTime);
        }
    }

    private IEnumerator ExecuteSpawnEntry(EnemySpawnEntry entry, float goldPerEnemy, float hpMultiplier)
    {
        for (int i = 0; i < entry.count; i++)
        {
            SpawnEnemy(entry.enemyType, entry.pathId, goldPerEnemy, hpMultiplier);

            if (entry.interval > 0f)
                yield return new WaitForSeconds(entry.interval);
        }
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
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        if (enemy != null)
        {
            UnregisterEnemy(enemy);

            if (coinPrefab != null)
            {
                Vector3 spawnPos = enemy.transform.position + new Vector3(
                    UnityEngine.Random.Range(-0.2f, 0.2f),
                    0.2f,
                    UnityEngine.Random.Range(-0.2f, 0.2f)
                );
                Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            }

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