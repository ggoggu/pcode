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

    public int MaxBenchCapacity => maxBenchCapacity;
    public int MaxInventoryCapacity => maxInventoryCapacity;
    public int MaxFieldCount => maxFieldCount;

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
    
    public IReadOnlyList<GameObject> ReadyPenguins => readyPenguins;

    public event Action OnDeckChanged;
    public event Action<GameObject> OnSelectedPenguinChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

        var existingPenguins = FindObjectsByType<PenguinController>(FindObjectsSortMode.None);
        foreach (var penguin in existingPenguins)
        {
            RegisterPenguin(penguin.gameObject);
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

            if (currentBenchCount < maxBenchCapacity)
            {
                locData.Location = PenguinLocation.Bench;
            }
            else if (currentInventoryCount < maxInventoryCapacity)
            {
                locData.Location = PenguinLocation.Inventory;
            }
            else
            {
                Debug.LogWarning($"[DeckManager] 벤치({maxBenchCapacity})와 인벤토리({maxInventoryCapacity})가 모두 가득 찼습니다.");
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

        OwnedPenguins.Remove(penguinObj);
        readyPenguins.Remove(penguinObj);
        OnDeckChanged?.Invoke();
    }

    public PenguinController SpawnPenguin(PenguinDefinition definition, Vector3 spawnPosition)
    {
        if (basicPenguinPrefab == null)
        {
            Debug.LogError("[DeckManager] basePenguinPrefab이 할당되지 않았습니다!", this);
            return null;
        }

        GameObject obj = Instantiate(basicPenguinPrefab, spawnPosition, Quaternion.identity, penguinContainer);
        PenguinController controller = obj.GetComponent<PenguinController>();
        controller.Init(definition);

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
    public bool ChangePenguinLocation(GameObject penguinObj, PenguinLocation newLocation)
    {
        if (penguinObj == null || !OwnedPenguins.Contains(penguinObj)) return false;
        
        var locData = penguinObj.GetComponent<PenguinLocationData>();
        if (locData == null) locData = penguinObj.AddComponent<PenguinLocationData>();
        
        if (locData.Location == newLocation) return true;
        
        var controller = penguinObj.GetComponent<PenguinController>();
        
        if (newLocation == PenguinLocation.Bench)
        {
            int currentBenchCount = OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
            if (currentBenchCount >= maxBenchCapacity) return false;
            
            locData.Location = PenguinLocation.Bench;
            if (controller != null && controller.IsAvailable && !controller.IsInField && !readyPenguins.Contains(penguinObj))
            {
                readyPenguins.Add(penguinObj);
            }
        }
        else
        {
            int currentInventoryCount = GetTotalInventoryCount();
            if (currentInventoryCount >= maxInventoryCapacity) return false;
            
            locData.Location = PenguinLocation.Inventory;
            readyPenguins.Remove(penguinObj);
            if (SelectedPenguin == penguinObj) SelectedPenguin = null;
        }
        
        OnDeckChanged?.Invoke();
        return true;
    }
    
    private void CheckAndExecuteMerge(PenguinDefinition definition, int currentTier)
    {
        if (definition == null) return;

        var candidates = OwnedPenguins
            .Where(p => {
                var c = p.GetComponent<PenguinController>();
                return c != null && !c.IsInField && c.Definition == definition && c.Tier == currentTier;
            })
            .ToList();

        if (candidates.Count < mergeRequiredCount) return;

        var materials = candidates.Take(mergeRequiredCount).ToList();
        Vector3 spawnPosition = materials[0].transform.position;

        PenguinController upgradedPenguin = SpawnPenguin(definition, spawnPosition);
        if (upgradedPenguin == null) return;

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
            Destroy(materialObj);
        }

        OwnedPenguins.Add(upgradedPenguin.gameObject);
        
        var upgradedLoc = upgradedPenguin.gameObject.GetComponent<PenguinLocationData>();
        if (upgradedLoc == null) upgradedLoc = upgradedPenguin.gameObject.AddComponent<PenguinLocationData>();
        
        // 업그레이드된 펭귄의 위치는 재료 펭귄 중 하나라도 벤치에 있었다면 벤치로, 아니면 인벤토리로
        bool anyInBench = materials.Any(m => m.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
        upgradedLoc.Location = anyInBench ? PenguinLocation.Bench : PenguinLocation.Inventory;
        
        if (upgradedLoc.Location == PenguinLocation.Bench && upgradedPenguin.IsAvailable && !upgradedPenguin.IsInField)
        {
            readyPenguins.Add(upgradedPenguin.gameObject);
        }

        upgradedPenguin.OnStatsChanged();
        GameEvents.RaiseEquipmentChanged(upgradedPenguin);

        if (wasSelected)
        {
            SelectedPenguin = upgradedPenguin.gameObject;
        }

        Debug.Log($"[DeckManager] {definition.name} (Tier {currentTier} -> {currentTier + 1}) 합성 완료!");

        CheckAndExecuteMerge(definition, currentTier + 1);
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
        return GetFieldActiveCount() < maxFieldCount;
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

    /// <summary>
    /// 대상 펭귄이 위치한 벤치 슬롯 인덱스 (0부터 시작: 0, 1, 2...). 벤치에 없으면 -1 반환
    /// </summary>
    public int GetBenchSlotIndex(PenguinController penguin)
    {
        if (penguin == null) return -1;

        int benchIndex = 0;
        foreach (var obj in OwnedPenguins)
        {
            if (obj == null) continue;
            var locData = obj.GetComponent<PenguinLocationData>();
            if (locData != null && locData.Location == PenguinLocation.Bench)
            {
                if (obj == penguin.gameObject) return benchIndex;
                benchIndex++;
            }
        }

        return -1;
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
