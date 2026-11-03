using PenguinPinball.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[Serializable]
//public class PenguinData
//{
//    public string PenguinID;
//    public int Tier;

//    public PenguinData(string id, int tier)
//    {
//        PenguinID = id;
//        Tier = tier;
//    }
//}


public class PenguinDeckManager : MonoBehaviour
{
    public static PenguinDeckManager Instance { get; private set; }

    [Header("Deck Options")]
    [SerializeField] private int maxBenchCapacity = 6;  // 펭귄 벤치 전용 6칸
    [SerializeField] private int maxInventoryCapacity = 9; // 인벤토리 9칸 (부적, 장비와 공유)
    [SerializeField] private int maxFieldCount = 3;  // 필드 동시 존재 상한선 (기본 3마리)
    [SerializeField] private int mergeRequiredCount = 2; // 합성 필요 수량 (기본 2마리)

    [Header("Prefabs & Setup")]
    [SerializeField] private GameObject basicPenguinPrefab;
    [SerializeField] private Transform penguinContainer;
    [SerializeField] private bool autoRegisterScenePenguinsOnStart = false;

    public GameObject BasicPenguinPrefab
    {
        get => basicPenguinPrefab;
        set => basicPenguinPrefab = value;
    }

    public int MaxBenchCapacity => maxBenchCapacity;
    public int MaxInventoryCapacity => maxInventoryCapacity;
    public int MaxFieldCount
    {
        get
        {
            float baseCount = Mathf.Max(0, maxFieldCount - lockedFieldSlots);
            if (StatModifierRegistry.Instance != null)
            {
                float modified = StatModifierRegistry.Instance.GetModifiedValue(StatType.FieldPenguinLimit, baseCount);
                return Mathf.Max(1, Mathf.RoundToInt(modified));
            }
            return Mathf.Max(1, Mathf.RoundToInt(baseCount));
        }
    }

    private int lockedFieldSlots = 0;
    public int LockedFieldSlots
    {
        get => lockedFieldSlots;
        set
        {
            lockedFieldSlots = Mathf.Max(0, value);
            OnDeckChanged?.Invoke();
        }
    }

    private int lockedBenchSlots = 0;
    public int LockedBenchSlots
    {
        get => lockedBenchSlots;
        set
        {
            lockedBenchSlots = Mathf.Max(0, value);
            OnDeckChanged?.Invoke();
        }
    }

    public int EffectiveBenchCapacity => Mathf.Max(0, maxBenchCapacity - lockedBenchSlots);

    public bool IsBenchSlotLocked(int slotIndex)
    {
        return slotIndex >= EffectiveBenchCapacity && slotIndex < maxBenchCapacity;
    }

    private GameObject selectedPenguin;
    public GameObject SelectedPenguin
    {
        get => selectedPenguin;
        private set
        {
            if (selectedPenguin != value)
            {
                selectedPenguin = value;
                OnSelectedPenguinChanged?.Invoke(selectedPenguin);
            }
        }
    }

    public readonly List<GameObject> OwnedPenguins = new List<GameObject>();
    private readonly List<GameObject> readyPenguins = new List<GameObject>();
    private readonly GameObject[] benchSlots = new GameObject[6]; // maxBenchCapacity = 6
    
    public IReadOnlyList<GameObject> ReadyPenguins => readyPenguins;

    public event Action OnDeckChanged;
    public event Action<GameObject> OnSelectedPenguinChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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

    private void OnEnable()
    {
        GameEvents.PenguinRespawned += HandlePenguinRespawned;
        GameEvents.OnPenguinLaunched += HandlePenguinLaunched;
        GameEvents.PenguinDrained += HandlePenguinDrained;
    }

    private void OnDisable()
    {
        GameEvents.PenguinRespawned -= HandlePenguinRespawned;
        GameEvents.OnPenguinLaunched -= HandlePenguinLaunched;
        GameEvents.PenguinDrained -= HandlePenguinDrained;
    }

    private void Start()
    {
        InitializeDeck();
    }

    // 런 시작 시 초기 덱 셋업
    private void InitializeDeck()
    {
        OwnedPenguins.Clear();

        if (autoRegisterScenePenguinsOnStart)
        {
            var existingPenguins = FindObjectsByType<PenguinController>(FindObjectsSortMode.None);
            foreach (var penguin in existingPenguins)
            {
                RegisterPenguin(penguin.gameObject);
            }
        }
    }

    #region Event Handlers
    private void HandlePenguinRespawned(PenguinController penguin)
    {
        if (penguin == null) return;
        if (!readyPenguins.Contains(penguin.gameObject))
            readyPenguins.Add(penguin.gameObject);
            
        Debug.Log($"[DeckManager] {penguin.name} 발사 준비 완료");
        OnDeckChanged?.Invoke();
    }

    private void HandlePenguinLaunched(PenguinController penguin)
    {
        if (penguin != null && readyPenguins.Contains(penguin.gameObject))
        {
            readyPenguins.Remove(penguin.gameObject);

            if (SelectedPenguin == penguin.gameObject) SelectedPenguin = null;

            OnDeckChanged?.Invoke();
        }
    }

    private void HandlePenguinDrained(PenguinController penguin, float seconds)
    {
        if (penguin != null)
        {
            readyPenguins.Remove(penguin.gameObject);

            if (SelectedPenguin == penguin.gameObject) SelectedPenguin = null;

            Debug.Log($"[DeckManager] {penguin.name} 드레인됨 (리스폰 대기: {seconds}초)");
            OnDeckChanged?.Invoke();
        }
    }
    #endregion

    #region Registration & Merge Logic
    public bool RegisterPenguin(GameObject penguinObj)
    {
        if (penguinObj == null) return false;

        if (!OwnedPenguins.Contains(penguinObj))
        {
            var locData = penguinObj.GetComponent<PenguinLocationData>();
            if (locData == null) locData = penguinObj.AddComponent<PenguinLocationData>();
            
            var controller = penguinObj.GetComponent<PenguinController>();
            if (controller == null) return false;

            int currentBenchCount = OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
            int currentInventoryCount = GetTotalInventoryCount();

            if (locData.Location == PenguinLocation.Inventory)
            {
                if (currentInventoryCount < maxInventoryCapacity)
                {
                    ItemInventoryManager.Instance?.RegisterInventoryPenguin(penguinObj);
                }
                else
                {
                    Debug.LogWarning($"[DeckManager] 인벤토리({maxInventoryCapacity})가 가득 찼습니다.");
                    return false;
                }
            }
            else if (currentBenchCount < EffectiveBenchCapacity)
            {
                locData.Location = PenguinLocation.Bench;
                AssignToFirstEmptyBenchSlot(penguinObj);
            }
            else if (currentInventoryCount < maxInventoryCapacity)
            {
                locData.Location = PenguinLocation.Inventory;
                ItemInventoryManager.Instance?.RegisterInventoryPenguin(penguinObj);
            }
            else
            {
                Debug.LogWarning($"[DeckManager] 벤치({EffectiveBenchCapacity})와 인벤토리({maxInventoryCapacity})가 모두 가득 찼습니다.");
                return false;
            }

            OwnedPenguins.Add(penguinObj);

            // 초기 등록 시 발사 가능한 상태(벤치)라면 준비 리스트에 바로 추가
            if (locData.Location == PenguinLocation.Bench && controller.IsAvailable && !controller.IsInField && !readyPenguins.Contains(penguinObj))
            {
                readyPenguins.Add(penguinObj);
            }

            CheckAndExecuteMerge(controller.Definition, controller.Tier);

            OnDeckChanged?.Invoke();
        }
        return true;
    }

    // 특정 펭귄 버리기 (덱 교체 시 사용)[cite: 1]
    public void UnregisterPenguin(GameObject penguinObj)
    {
        if (penguinObj == null) return;

        int benchIdx = Array.IndexOf(benchSlots, penguinObj);
        if (benchIdx >= 0) benchSlots[benchIdx] = null;
        ItemInventoryManager.Instance?.UnregisterInventoryPenguin(penguinObj);

        OwnedPenguins.Remove(penguinObj);
        readyPenguins.Remove(penguinObj);
        OnDeckChanged?.Invoke();
    }

    // 펭귄 판매 처리 (장비 해제, 덱 등록 해제, 골드 지급, 오브젝트 파괴)
    public void SellPenguin(GameObject penguinObj, int goldReward = 10)
    {
        if (penguinObj == null) return;

        // 보유 펭귄이 1마리 이하인 경우 판매 차단 (기획서 §4-B: 마지막 1마리는 판매할 수 없음)
        if (OwnedPenguins.Count <= 1)
        {
            Debug.LogWarning("[DeckManager] 마지막 1마리의 펭귄은 판매할 수 없습니다.");
            return;
        }

        var controller = penguinObj.GetComponent<PenguinController>();
        controller?.EquipmentHandler?.ClearAllEquipments(false);
        UnregisterPenguin(penguinObj);

        if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(goldReward);
        else if (GameManager.Instance != null) GameManager.Instance.AddGold(goldReward);

        if (Application.isPlaying) Destroy(penguinObj);
        else DestroyImmediate(penguinObj);
    }

    /// <summary>
    /// 웨이브 종료 시 모든 펭귄 상태 초기화 (콤보/과부하/리스폰 타이머 초기화 및 전원 즉시 발사 대기 복귀)
    /// </summary>
    public void ResetAllPenguinsForWaveEnd()
    {
        ClearSelection();

        foreach (var obj in OwnedPenguins)
        {
            if (obj == null) continue;
            var controller = obj.GetComponent<PenguinController>();
            if (controller != null)
            {
                controller.ResetToHeld();
            }

            var locData = obj.GetComponent<PenguinLocationData>();
            if (locData != null && locData.Location == PenguinLocation.Bench)
            {
                if (!readyPenguins.Contains(obj))
                {
                    readyPenguins.Add(obj);
                }
            }
        }

        OnDeckChanged?.Invoke();
        Debug.Log("[DeckManager] 웨이브 종료 처리: 모든 펭귄 리셋 및 발사 가능 풀 복귀 완료");
    }

    public PenguinController SpawnPenguin(PenguinDefinition definition, Vector3 spawnPosition)
    {
        GameObject obj;
        if (basicPenguinPrefab != null)
        {
            obj = Instantiate(basicPenguinPrefab, spawnPosition, Quaternion.identity, penguinContainer);
        }
        else
        {
            // 테스트 또는 프리팹 미할당 환경을 위한 폴백 인스턴스 생성
            obj = new GameObject("Penguin", typeof(Rigidbody), typeof(SphereCollider), typeof(PenguinLocationData), typeof(PenguinController));
            if (penguinContainer != null) obj.transform.SetParent(penguinContainer);
            obj.transform.position = spawnPosition;
        }

        PenguinController controller = obj.GetComponent<PenguinController>();
        controller.Init(definition);

        return controller;
    }

    /// <summary>
    /// 지정된 정의의 펭귄을 특정 티어로 생성하여 벤치 또는 인벤토리에 등록합니다.
    /// 슬롯 공간이 없으면 파괴하고 null을 반환합니다.
    /// </summary>
    public PenguinController CreateAndRegisterPenguin(PenguinDefinition definition, int tier = 1, Vector3? position = null)
    {
        if (definition == null) return null;

        Vector3 spawnPos = position ?? Vector3.zero;
        PenguinController controller = SpawnPenguin(definition, spawnPos);
        if (controller == null) return null;

        controller.SetTier(tier);

        bool registered = RegisterPenguin(controller.gameObject);
        if (!registered)
        {
            if (Application.isPlaying) Destroy(controller.gameObject);
            else DestroyImmediate(controller.gameObject);
            return null;
        }

        return controller;
    }
    
    // 현재 인벤토리 전체 사용량 계산 (부적 + 장비 + 인벤토리 펭귄)
    public int GetTotalInventoryCount()
    {
        int count = 0;
        if (ItemInventoryManager.Instance != null) {
            count += ItemInventoryManager.Instance.OwnedCharms.Count;
            count += ItemInventoryManager.Instance.UnusedEquipments.Count;
        }
        count += OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Inventory);
        return count;
    }
    
    // 벤치 <-> 인벤토리 이동
    public bool ChangePenguinLocation(GameObject penguinObj, PenguinLocation newLocation, int targetSlotIndex = -1)
    {
        if (penguinObj == null || !OwnedPenguins.Contains(penguinObj)) return false;
        
        var locData = penguinObj.GetComponent<PenguinLocationData>();
        if (locData == null) locData = penguinObj.AddComponent<PenguinLocationData>();
        
        var controller = penguinObj.GetComponent<PenguinController>();
        
        if (newLocation == PenguinLocation.Bench)
        {
            if (locData.Location == PenguinLocation.Bench)
            {
                if (targetSlotIndex >= 0)
                {
                    int curIdx = Array.IndexOf(benchSlots, penguinObj);
                    if (curIdx >= 0 && curIdx != targetSlotIndex)
                    {
                        return SwapOrMoveBenchSlot(curIdx, targetSlotIndex);
                    }
                }
                return true;
            }

            if (targetSlotIndex >= 0 && targetSlotIndex < benchSlots.Length && IsBenchSlotLocked(targetSlotIndex))
            {
                Debug.LogWarning("[DeckManager] 잠긴 벤치 슬롯에는 펭귄을 배치할 수 없습니다.");
                return false;
            }

            int sourceInvenIdx = (ItemInventoryManager.Instance != null)
                ? Array.IndexOf(ItemInventoryManager.Instance.InventorySlots.ToArray(), penguinObj)
                : -1;

            var existingAtTarget = (targetSlotIndex >= 0 && targetSlotIndex < benchSlots.Length)
                ? benchSlots[targetSlotIndex]
                : null;

            // 타깃 슬롯에 이미 다른 펭귄이 있는 경우: 1:1 맞교환(스왑)
            if (existingAtTarget != null && existingAtTarget != penguinObj)
            {
                ItemInventoryManager.Instance?.UnregisterInventoryPenguin(penguinObj);

                var targetLocData = existingAtTarget.GetComponent<PenguinLocationData>();
                if (targetLocData != null) targetLocData.Location = PenguinLocation.Inventory;
                readyPenguins.Remove(existingAtTarget);
                if (SelectedPenguin == existingAtTarget) SelectedPenguin = null;

                if (sourceInvenIdx >= 0 && ItemInventoryManager.Instance != null)
                {
                    ItemInventoryManager.Instance.SetSlotItem(sourceInvenIdx, existingAtTarget);
                }
                else
                {
                    ItemInventoryManager.Instance?.RegisterInventoryPenguin(existingAtTarget);
                }

                locData.Location = PenguinLocation.Bench;
                benchSlots[targetSlotIndex] = penguinObj;
            }
            else
            {
                int currentBenchCount = OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
                if (currentBenchCount >= EffectiveBenchCapacity)
                {
                    Debug.LogWarning($"[DeckManager] 벤치({EffectiveBenchCapacity})가 가득 찼습니다.");
                    return false;
                }

                ItemInventoryManager.Instance?.UnregisterInventoryPenguin(penguinObj);
                locData.Location = PenguinLocation.Bench;

                if (targetSlotIndex >= 0 && targetSlotIndex < benchSlots.Length && !IsBenchSlotLocked(targetSlotIndex))
                {
                    benchSlots[targetSlotIndex] = penguinObj;
                }
                else
                {
                    AssignToFirstEmptyBenchSlot(penguinObj);
                }
            }

            if (controller != null && controller.IsAvailable && !controller.IsInField && !readyPenguins.Contains(penguinObj))
            {
                readyPenguins.Add(penguinObj);
            }
        }
        else
        {
            if (locData.Location == PenguinLocation.Inventory)
            {
                if (targetSlotIndex >= 0 && ItemInventoryManager.Instance != null)
                {
                    int curIdx = Array.IndexOf(ItemInventoryManager.Instance.InventorySlots.ToArray(), penguinObj);
                    if (curIdx >= 0 && curIdx != targetSlotIndex)
                    {
                        return ItemInventoryManager.Instance.SwapOrMoveInventorySlot(curIdx, targetSlotIndex);
                    }
                }
                return true;
            }

            int sourceBenchIdx = Array.IndexOf(benchSlots, penguinObj);

            object existingInvenItem = (targetSlotIndex >= 0 && ItemInventoryManager.Instance != null)
                ? ItemInventoryManager.Instance.GetItemAtSlot(targetSlotIndex)
                : null;

            // 인벤토리 타깃 슬롯에 이미 펭귄이 있는 경우: 1:1 맞교환(스왑)
            if (existingInvenItem is GameObject existingPenguinObj && existingPenguinObj != penguinObj)
            {
                var existingLocData = existingPenguinObj.GetComponent<PenguinLocationData>();
                var existingController = existingPenguinObj.GetComponent<PenguinController>();

                locData.Location = PenguinLocation.Inventory;
                readyPenguins.Remove(penguinObj);
                if (SelectedPenguin == penguinObj) SelectedPenguin = null;
                ItemInventoryManager.Instance.SetSlotItem(targetSlotIndex, penguinObj);

                if (sourceBenchIdx >= 0 && sourceBenchIdx < benchSlots.Length)
                {
                    if (existingLocData != null) existingLocData.Location = PenguinLocation.Bench;
                    benchSlots[sourceBenchIdx] = existingPenguinObj;
                    if (existingController != null && existingController.IsAvailable && !existingController.IsInField && !readyPenguins.Contains(existingPenguinObj))
                    {
                        readyPenguins.Add(existingPenguinObj);
                    }
                }
                else
                {
                    AssignToFirstEmptyBenchSlot(existingPenguinObj);
                }
            }
            else
            {
                int currentInventoryCount = GetTotalInventoryCount();
                if (currentInventoryCount >= maxInventoryCapacity)
                {
                    Debug.LogWarning($"[DeckManager] 인벤토리({maxInventoryCapacity})가 가득 찼습니다.");
                    return false;
                }
                
                if (sourceBenchIdx >= 0) benchSlots[sourceBenchIdx] = null;

                locData.Location = PenguinLocation.Inventory;
                readyPenguins.Remove(penguinObj);
                if (SelectedPenguin == penguinObj) SelectedPenguin = null;

                ItemInventoryManager.Instance?.RegisterInventoryPenguin(penguinObj, targetSlotIndex);
            }
        }
        
        OnDeckChanged?.Invoke();
        return true;
    }
    
    public bool CheckAndExecuteMerge(PenguinDefinition definition, int currentTier)
    {
        if (definition == null) return false;

        SyncBenchSlots();

        var candidates = OwnedPenguins
            .Where(p => {
                if (p == null) return false;
                var c = p.GetComponent<PenguinController>();
                return c != null && !c.IsInField && c.Definition == definition && c.Tier == currentTier;
            })
            .OrderBy(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench ? 0 : 1)
            .ToList();

        if (candidates.Count < mergeRequiredCount) return false;

        var materials = candidates.Take(mergeRequiredCount).ToList();
        Vector3 spawnPosition = materials[0].transform.position;

        // 재료 펭귄 중 벤치 슬롯 및 인벤토리 슬롯 위치를 재료 파괴/등록 해제 이전에 미리 파악합니다.
        int targetBenchSlotIndex = -1;
        int targetInventorySlotIndex = -1;
        bool anyInBench = false;

        foreach (var mat in materials)
        {
            if (mat == null) continue;
            var locData = mat.GetComponent<PenguinLocationData>();
            var loc = locData != null ? locData.Location : PenguinLocation.Bench;

            if (loc == PenguinLocation.Bench)
            {
                anyInBench = true;
                if (targetBenchSlotIndex < 0)
                {
                    int bIdx = Array.IndexOf(benchSlots, mat);
                    if (bIdx >= 0)
                    {
                        targetBenchSlotIndex = bIdx;
                    }
                }
            }
            else if (loc == PenguinLocation.Inventory)
            {
                if (targetInventorySlotIndex < 0 && ItemInventoryManager.Instance != null)
                {
                    int iIdx = Array.IndexOf(ItemInventoryManager.Instance.InventorySlots.ToArray(), mat);
                    if (iIdx >= 0)
                    {
                        targetInventorySlotIndex = iIdx;
                    }
                }
            }
        }

        PenguinController upgradedPenguin = SpawnPenguin(definition, spawnPosition);
        if (upgradedPenguin == null) return false;

        upgradedPenguin.SetTier(currentTier + 1);  // 티어 증가 설정

        bool wasSelected = false;
        foreach (var materialObj in materials)
        {
            if (SelectedPenguin == materialObj) wasSelected = true;

            var matController = materialObj.GetComponent<PenguinController>();
            // 스탯/UI 알림 없이 장비 데이터만 강제 이관
            if (matController != null)
                matController.EquipmentHandler.TransferEquipmentsTo(upgradedPenguin.EquipmentHandler);

            // 덱 등록 해제 및 게임오브젝트 파괴
            UnregisterPenguin(materialObj);
            if (Application.isPlaying) Destroy(materialObj);
            else DestroyImmediate(materialObj);
        }

        OwnedPenguins.Add(upgradedPenguin.gameObject);
        
        var upgradedLoc = upgradedPenguin.gameObject.GetComponent<PenguinLocationData>();
        if (upgradedLoc == null) upgradedLoc = upgradedPenguin.gameObject.AddComponent<PenguinLocationData>();
        
        // 업그레이드된 펭귄의 위치: 재료 펭귄 중 하나라도 벤치에 있었다면 벤치로, 아니면 인벤토리로
        upgradedLoc.Location = anyInBench ? PenguinLocation.Bench : PenguinLocation.Inventory;
        
        if (upgradedLoc.Location == PenguinLocation.Bench)
        {
            if (targetBenchSlotIndex >= 0 && targetBenchSlotIndex < benchSlots.Length && !IsBenchSlotLocked(targetBenchSlotIndex) && benchSlots[targetBenchSlotIndex] == null)
            {
                benchSlots[targetBenchSlotIndex] = upgradedPenguin.gameObject;
            }
            else
            {
                AssignToFirstEmptyBenchSlot(upgradedPenguin.gameObject);
            }

            if (upgradedPenguin.IsAvailable && !upgradedPenguin.IsInField)
            {
                if (!readyPenguins.Contains(upgradedPenguin.gameObject))
                {
                    readyPenguins.Add(upgradedPenguin.gameObject);
                }
            }
        }
        else
        {
            ItemInventoryManager.Instance?.RegisterInventoryPenguin(upgradedPenguin.gameObject, targetInventorySlotIndex);
        }

        upgradedPenguin.OnStatsChanged();
        GameEvents.RaiseEquipmentChanged(upgradedPenguin);

        if (wasSelected && upgradedLoc.Location == PenguinLocation.Bench)
        {
            SelectedPenguin = upgradedPenguin.gameObject;
        }

        Debug.Log($"[DeckManager] {definition.name} (Tier {currentTier} -> {currentTier + 1}) 합성 완료! (위치: {upgradedLoc.Location})");

        GameEvents.RaisePenguinSynthesized(upgradedPenguin);

        CheckAndExecuteMerge(definition, currentTier + 1);

        OnDeckChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 현재 덱 내의 모든 펭귄을 검사하여 합성 가능한 조합이 있으면 연쇄적으로 모두 합성합니다.
    /// 실제로 새로 합성된 상위 티어 펭귄 목록을 반환합니다.
    /// </summary>
    public List<PenguinController> CheckAllMerges()
    {
        return CheckAllMerges(out _);
    }

    /// <summary>
    /// 현재 덱 내의 모든 펭귄을 검사하여 합성 가능한 조합이 있으면 연쇄적으로 모두 합성합니다.
    /// 합성된 펭귄 목록을 out 파라미터로 반환합니다.
    /// </summary>
    public List<PenguinController> CheckAllMerges(out List<PenguinController> synthesizedPenguins)
    {
        synthesizedPenguins = new List<PenguinController>();
        bool mergedAny;
        int maxLoopCount = 100;
        int loop = 0;
        do
        {
            mergedAny = false;
            loop++;
            if (loop > maxLoopCount)
            {
                Debug.LogWarning("[DeckManager] CheckAllMerges 최대 반복 횟수를 초과했습니다.");
                break;
            }

            var groups = OwnedPenguins
                .Where(p => p != null)
                .Select(p => p.GetComponent<PenguinController>())
                .Where(c => c != null && !c.IsInField && c.Definition != null)
                .GroupBy(c => (c.Definition, c.Tier))
                .Where(g => g.Count() >= mergeRequiredCount)
                .ToList();

            foreach (var group in groups)
            {
                int countBefore = OwnedPenguins.Count;
                if (CheckAndExecuteMerge(group.Key.Definition, group.Key.Tier))
                {
                    // 새로 생성된 합성 펭귄(마지막으로 추가된 펭귄) 추적
                    var newlySynthesized = OwnedPenguins.LastOrDefault()?.GetComponent<PenguinController>();
                    if (newlySynthesized != null && !synthesizedPenguins.Contains(newlySynthesized))
                    {
                        synthesizedPenguins.Add(newlySynthesized);
                    }
                    mergedAny = true;
                    break;
                }
            }
        } while (mergedAny);

        return synthesizedPenguins;
    }

    /// <summary>
    /// 두 펭귄의 등급을 교환했을 때 합성이 발생할 것인지 미리 예측/확인합니다.
    /// </summary>
    public bool PredictMergesAfterSwap(PenguinController p1, PenguinController p2, out List<string> predictedMergeDescriptions)
    {
        predictedMergeDescriptions = new List<string>();
        if (p1 == null || p2 == null || p1 == p2) return false;

        int newTier1 = p2.Tier;
        int newTier2 = p1.Tier;

        // p1의 변화 예측: p1이 newTier1이 되었을 때 같은 definition/tier를 가진 다른 펭귄이 있는지
        int matchCount1 = OwnedPenguins.Count(p => {
            if (p == null || p == p1.gameObject || p == p2.gameObject) return false;
            var c = p.GetComponent<PenguinController>();
            return c != null && !c.IsInField && c.Definition == p1.Definition && c.Tier == newTier1;
        });

        // 만약 p2도 같은 Definition이고 newTier2 == newTier1인 경우 (p1.Tier == p2.Tier인 경우는 제외)
        if (p1.Definition == p2.Definition && newTier2 == newTier1 && p1.Tier != p2.Tier)
        {
            matchCount1++;
        }

        if (matchCount1 >= 1)
        {
            string name1 = p1.Definition != null ? p1.Definition.DisplayName : p1.name;
            predictedMergeDescriptions.Add($"[{name1}] Tier {newTier1} ➔ Tier {newTier1 + 1} 합성");
        }

        // p2의 변화 예측: p2가 newTier2가 되었을 때 같은 definition/tier를 가진 다른 펭귄이 있는지
        int matchCount2 = OwnedPenguins.Count(p => {
            if (p == null || p == p1.gameObject || p == p2.gameObject) return false;
            var c = p.GetComponent<PenguinController>();
            return c != null && !c.IsInField && c.Definition == p2.Definition && c.Tier == newTier2;
        });

        if (p1.Definition == p2.Definition && newTier1 == newTier2 && p1.Tier != p2.Tier)
        {
            matchCount2++;
        }

        if (matchCount2 >= 1 && p1.Definition != p2.Definition) // 서로 다른 종류일 때만 별도 추가
        {
            string name2 = p2.Definition != null ? p2.Definition.DisplayName : p2.name;
            predictedMergeDescriptions.Add($"[{name2}] Tier {newTier2} ➔ Tier {newTier2 + 1} 합성");
        }

        return predictedMergeDescriptions.Count > 0;
    }

    /// <summary>
    /// 선택한 두 펭귄의 등급(Tier)을 서로 교환하고, 교환 후 합성 가능한 조합이 있으면 자동으로 합성합니다.
    /// </summary>
    public bool SwapPenguinTiers(PenguinController penguinA, PenguinController penguinB)
    {
        return SwapPenguinTiers(penguinA, penguinB, out _);
    }

    /// <summary>
    /// 선택한 두 펭귄의 등급(Tier)을 서로 교환하고, 교환 후 합성 가능한 조합이 있으면 자동으로 합성합니다.
    /// 실제로 합성된 펭귄 목록을 out 파라미터로 반환합니다.
    /// </summary>
    public bool SwapPenguinTiers(PenguinController penguinA, PenguinController penguinB, out List<PenguinController> synthesizedPenguins)
    {
        synthesizedPenguins = new List<PenguinController>();

        if (penguinA == null || penguinB == null || penguinA == penguinB)
        {
            Debug.LogWarning("[DeckManager] 교환 대상 펭귄이 유효하지 않습니다.");
            return false;
        }

        if (penguinA.IsInField || penguinB.IsInField)
        {
            Debug.LogWarning("[DeckManager] 필드에 출격 중인 펭귄은 등급을 교환할 수 없습니다.");
            return false;
        }

        int tierA = penguinA.Tier;
        int tierB = penguinB.Tier;

        // 등급 교환
        penguinA.SetTier(tierB);
        penguinB.SetTier(tierA);

        Debug.Log($"[DeckManager] 등급 교환 완료: {penguinA.name}(Tier {tierA}->{tierB}), {penguinB.name}(Tier {tierB}->{tierA})");

        // 교환 후 합성 가능한 조합이 있으면 자동으로 합성 및 합성 여부 확인!
        synthesizedPenguins = CheckAllMerges();

        if (synthesizedPenguins != null && synthesizedPenguins.Count > 0)
        {
            Debug.Log($"[DeckManager] ★ 등급 교환 후 {synthesizedPenguins.Count}회의 합성이 자동으로 발생하여 완료되었습니다!");
            foreach (var sp in synthesizedPenguins)
            {
                Debug.Log($"[DeckManager]   • 합성 결과: {sp.name} (Tier {sp.Tier})");
            }
        }
        else
        {
            Debug.Log("[DeckManager] 등급 교환 후 즉시 합성 가능한 조합은 없습니다.");
        }

        OnDeckChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 선택한 펭귄을 1성 영구 업그레이드합니다. 5성 초과는 불가합니다.
    /// </summary>
    public bool PromotePenguinTier(PenguinController penguin)
    {
        return PromotePenguinTier(penguin, out _);
    }

    /// <summary>
    /// 선택한 펭귄을 1성 영구 업그레이드하고, 업그레이드 후 합성 가능한 조합이 있으면 자동으로 합성합니다.
    /// 실제로 합성된 펭귄 목록을 out 파라미터로 반환합니다.
    /// </summary>
    public bool PromotePenguinTier(PenguinController penguin, out List<PenguinController> synthesizedPenguins)
    {
        synthesizedPenguins = new List<PenguinController>();

        if (penguin == null)
        {
            Debug.LogWarning("[DeckManager] 승급 대상 펭귄이 null입니다.");
            return false;
        }

        if (penguin.IsInField)
        {
            Debug.LogWarning("[DeckManager] 필드에 출격 중인 펭귄은 승급할 수 없습니다.");
            return false;
        }

        if (penguin.Tier >= 5)
        {
            Debug.LogWarning($"[DeckManager] 펭귄이 이미 최대 성(Tier {penguin.Tier})에 도달하여 5성 초과는 불가합니다.");
            return false;
        }

        int oldTier = penguin.Tier;
        int newTier = oldTier + 1;

        penguin.SetTier(newTier);
        if (penguin.Definition != null)
        {
            penguin.gameObject.name = $"{penguin.Definition.DisplayName}_Tier{newTier}";
        }

        Debug.Log($"[DeckManager] 펭귄 승급 완료: {penguin.name} (Tier {oldTier} ➔ {newTier})");

        // 승급 후 합성 가능한 조합이 있으면 자동으로 합성
        synthesizedPenguins = CheckAllMerges();

        if (synthesizedPenguins != null && synthesizedPenguins.Count > 0)
        {
            Debug.Log($"[DeckManager] ★ 승급 후 {synthesizedPenguins.Count}회의 합성이 자동으로 발생하여 완료되었습니다!");
            foreach (var sp in synthesizedPenguins)
            {
                Debug.Log($"[DeckManager]   • 합성 결과: {sp.name} (Tier {sp.Tier})");
            }
        }

        OnDeckChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 펭귄을 1성 승급했을 때 합성이 발생할 것인지 미리 예측/확인합니다.
    /// </summary>
    public bool PredictMergesAfterPromotion(PenguinController penguin, out List<string> predictedMergeDescriptions)
    {
        predictedMergeDescriptions = new List<string>();
        if (penguin == null || penguin.Tier >= 5) return false;

        int newTier = penguin.Tier + 1;

        // penguin이 newTier가 되었을 때 동일 Definition과 newTier를 가진 다른 펭귄이 있는지 확인
        int matchCount = OwnedPenguins.Count(p => {
            if (p == null || p == penguin.gameObject) return false;
            var c = p.GetComponent<PenguinController>();
            return c != null && !c.IsInField && c.Definition == penguin.Definition && c.Tier == newTier;
        });

        if (matchCount >= 1)
        {
            string name = penguin.Definition != null ? penguin.Definition.DisplayName : penguin.name;
            predictedMergeDescriptions.Add($"[{name}] Tier {newTier} ➔ Tier {newTier + 1} 합성");
        }

        return predictedMergeDescriptions.Count > 0;
    }
    #endregion

    #region Query & Selection Logic
    // 현재 필드 위에서 움직이고 있는 펭귄 수
    public int GetFieldActiveCount()
    {
        return OwnedPenguins.Count(p => {
            var c = p.GetComponent<PenguinController>();
            return c != null && c.IsInField;
        });
    }

    public bool CanLaunchToField()
    {
        return GetFieldActiveCount() < MaxFieldCount;
    }

    public bool TrySelectPenguin(GameObject targetPenguinObj)
    {
        if (targetPenguinObj == null) return false;

        if (!CanLaunchToField())
        {
            Debug.LogWarning("[DeckManager] 필드 출격 상한선에 도달했습니다.");
            return false;
        }

        var locData = targetPenguinObj.GetComponent<PenguinLocationData>();
        if (locData != null && locData.Location == PenguinLocation.Bench && readyPenguins.Contains(targetPenguinObj))
        {
            SelectedPenguin = targetPenguinObj;
            OnDeckChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void ClearSelection()
    {
        SelectedPenguin = null;
        OnDeckChanged?.Invoke();
    }

    // 부적 획득 등으로 펭귄 보유 상한/필드 상한을 조작하는 함수[cite: 1]
    public void ModifyCapacities(int addedDeckCap, int addedFieldCap)
    {
        maxBenchCapacity += addedDeckCap;
        maxFieldCount += addedFieldCap;
        OnDeckChanged?.Invoke();
    }

    public GameObject GetOrSelectAvailablePenguin()
    {
        if (SelectedPenguin != null && readyPenguins.Contains(SelectedPenguin))
        {
            return SelectedPenguin;
        }

        if (!CanLaunchToField() || readyPenguins.Count == 0)
        {
            return null;
        }

        var nextPenguin = readyPenguins.FirstOrDefault(p => p != null);
        if (nextPenguin != null)
        {
            TrySelectPenguin(nextPenguin);
            return SelectedPenguin;
        }

        return null;
    }
    #endregion

    #region Bench Slot Management
    public GameObject GetPenguinAtBenchSlot(int index)
    {
        SyncBenchSlots();
        if (index < 0 || index >= benchSlots.Length) return null;
        return benchSlots[index];
    }

    public bool SwapOrMoveBenchSlot(int fromIndex, int toIndex)
    {
        SyncBenchSlots();
        if (fromIndex < 0 || fromIndex >= benchSlots.Length) return false;
        if (toIndex < 0 || toIndex >= benchSlots.Length) return false;
        if (IsBenchSlotLocked(toIndex)) return false;
        if (fromIndex == toIndex) return true;

        var fromObj = benchSlots[fromIndex];
        var toObj = benchSlots[toIndex];

        if (fromObj == null) return false;

        benchSlots[toIndex] = fromObj;
        benchSlots[fromIndex] = toObj;

        OnDeckChanged?.Invoke();
        return true;
    }

    public bool MovePenguinToBenchSlot(GameObject penguinObj, int targetIndex)
    {
        if (penguinObj == null || targetIndex < 0 || targetIndex >= benchSlots.Length) return false;
        if (IsBenchSlotLocked(targetIndex)) return false;

        var locData = penguinObj.GetComponent<PenguinLocationData>();
        if (locData != null && locData.Location == PenguinLocation.Bench)
        {
            int fromIndex = Array.IndexOf(benchSlots, penguinObj);
            if (fromIndex >= 0)
            {
                return SwapOrMoveBenchSlot(fromIndex, targetIndex);
            }
        }

        return ChangePenguinLocation(penguinObj, PenguinLocation.Bench, targetIndex);
    }

    private bool AssignToFirstEmptyBenchSlot(GameObject penguinObj)
    {
        if (penguinObj == null) return false;
        int existingIdx = Array.IndexOf(benchSlots, penguinObj);
        if (existingIdx >= 0) return true;

        for (int i = 0; i < EffectiveBenchCapacity && i < benchSlots.Length; i++)
        {
            if (benchSlots[i] == null)
            {
                benchSlots[i] = penguinObj;
                return true;
            }
        }
        return false;
    }

    public void SyncBenchSlots()
    {
        // 1. 유효하지 않은 항목 제거
        for (int i = 0; i < benchSlots.Length; i++)
        {
            var obj = benchSlots[i];
            if (obj == null) continue;
            if (!OwnedPenguins.Contains(obj) || obj.GetComponent<PenguinLocationData>()?.Location != PenguinLocation.Bench)
            {
                benchSlots[i] = null;
            }
        }

        // 2. 누락된 벤치 펭귄 빈 슬롯에 배치
        foreach (var obj in OwnedPenguins)
        {
            if (obj != null && obj.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench)
            {
                if (Array.IndexOf(benchSlots, obj) < 0)
                {
                    AssignToFirstEmptyBenchSlot(obj);
                }
            }
        }
    }

    /// <summary>
    /// 대상 펭귄이 위치한 벤치 슬롯 인덱스 (0부터 시작: 0, 1, 2...). 벤치에 없으면 -1 반환
    /// </summary>
    public int GetBenchSlotIndex(PenguinController penguin)
    {
        if (penguin == null) return -1;
        SyncBenchSlots();
        return Array.IndexOf(benchSlots, penguin.gameObject);
    }

    /// <summary>
    /// 대상 펭귄이 위치한 벤치 슬롯 번호 (1부터 시작: 1번 벤치 = 1, 2번 벤치 = 2...). 벤치에 없으면 -1 반환
    /// </summary>
    public int GetBenchSlotNumber(PenguinController penguin)
    {
        int index = GetBenchSlotIndex(penguin);
        return index >= 0 ? index + 1 : -1;
    }

    #endregion


#if UNITY_EDITOR
    [Header("Debug / Test Settings")]
    [SerializeField] private GameObject debugTargetPenguin;

    [ContextMenu("Test: 1. 선택 가능한 첫 번째 펭귄 선택")]
    private void TestSelectFirstPenguin()
    {
        var first = ReadyPenguins.FirstOrDefault();
        if (first != null)
        {
            TrySelectPenguin(first);
            Debug.Log($"[Test] 첫 번째 대기 펭귄({first.name}) 선택 완료");
        }
        else
        {
            Debug.LogWarning("[Test] 대기 중인 펭귄이 없습니다.");
        }
    }

    [ContextMenu("Test: 2. 지정된 Debug 펭귄 선택")]
    private void TestSelectDebugTarget()
    {
        var first = ReadyPenguins.FirstOrDefault();
        if (first != null)
        {
            TrySelectPenguin(first);
            Debug.Log($"[Test] 첫 번째 대기 펭귄({first.name}) 선택 완료");
        }
        else
        {
            Debug.LogWarning("[Test] 대기 중인 펭귄이 없습니다.");
        }
    }

    [ContextMenu("Test: 3. 선택 해제 (Clear Selection)")]
    private void TestClearSelection()
    {
        ClearSelection();
        Debug.Log("[Test] 펭귄 선택 해제 완료");
    }

    [ContextMenu("Test: 4. 선택된 펭귄 발사 이벤트 강제 발생")]
    private void TestLaunchSelected()
    {
        if (SelectedPenguin != null)
        {
            var controller = SelectedPenguin.GetComponent<PenguinController>();
            if (controller != null)
            {
                GameEvents.RaiseOnPenguinLaunched(controller);
                Debug.Log($"[Test] {SelectedPenguin.name} 발사 이벤트 신호 전달 완료");
            }
        }
        else
        {
            Debug.LogWarning("[Test] 현재 선택된 펭귄이 없습니다.");
        }
    }

    [ContextMenu("Test: 5. 현재 덱 상태 출력")]
    private void TestPrintStatus()
    {
        Debug.Log($"=== [PenguinDeckManager Status] ===");
        int benchCount = OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
        int invCount = OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Inventory);
        Debug.Log($"• 벤치 등록 수: {benchCount} / {MaxBenchCapacity}");
        Debug.Log($"• 인벤 등록 수: {invCount} / (총 인벤토리: {GetTotalInventoryCount()} / {maxInventoryCapacity})");
        Debug.Log($"• 전체 소유 수: {OwnedPenguins.Count}");
        Debug.Log($"• 발사 가능 수: {readyPenguins.Count}");
        Debug.Log($"• 필드 출격 수: {GetFieldActiveCount()} / {MaxFieldCount}");
        Debug.Log($"• 현재 선택된 펭귄: {(SelectedPenguin != null ? SelectedPenguin.name : "없음")}");
    }
#endif
}
