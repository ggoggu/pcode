using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PenguinPinball.Core;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("상점 진열")]
    public List<ShopItemBase> CurrentShopItems = new List<ShopItemBase>(3);

    [Header("부화장(펭귄 상점) 진열")]
    public List<PenguinPinball.Core.PenguinDefinition> CurrentHatcheryItems = new List<PenguinPinball.Core.PenguinDefinition>(3);

    [Header("테스트용 아이템 풀 (데이터 연동)")]
    [Tooltip("상점 갱신 시 등장할 모든 아이템(ScriptableObject)들을 여기에 넣어주세요.")]
    public List<ShopItemBase> ItemPool = new List<ShopItemBase>();

    [Header("테스트용 부화장 풀")]
    [Tooltip("부화장 갱신 시 등장할 모든 펭귄 스탯 데이터(PenguinDefinition)를 여기에 넣어주세요.")]
    public List<PenguinPinball.Core.PenguinDefinition> HatcheryPool = new List<PenguinPinball.Core.PenguinDefinition>();

    [Header("리롤 상태")]
    public int ShopRerollCount { get; private set; } = 0;
    public int HatcheryRerollCount { get; private set; } = 0;
    private int baseRerollCost = 5;

    [Header("펭귄 소환 설정")]
    [Tooltip("펭귄이 생성될 기본 위치/트랜스폼입니다.")]
    [SerializeField] private Transform defaultPenguinSpawnPoint;
    [SerializeField] private PenguinLauncher penguinLauncher;

    public event Action OnShopUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }
    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        // 웨이브 종료(정비시간 진입) 시 리롤 비용 초기화 및 진열 자동 갱신
        if (state == GameState.MaintenanceTime)
        {
            ShopRerollCount = 0;
            HatcheryRerollCount = 0;
            RerollShop(freeReroll: true);
            RerollHatchery(freeReroll: true); 
        }
    }

    public int GetCurrentRerollCost(int count) => baseRerollCost * (int)Mathf.Pow(2, count);

    // ★ 리롤 시 새로운 아이템을 뽑아오는 로직 추가 ★
    public void RerollShop(bool freeReroll = false)
    {
        if (!freeReroll)
        {
            int cost = GetCurrentRerollCost(ShopRerollCount);
            if (!EconomyManager.Instance.SpendGold(cost)) return;
            ShopRerollCount++;
        }
        
        if (ItemPool.Count > 0)
        {
            CurrentShopItems.Clear();
            for (int i = 0; i < 3; i++) // 3칸으로 축소
            {
                CurrentShopItems.Add(GetWeightedRandomItem());
            }
        }
        else
        {
            Debug.LogWarning("[ShopManager] ItemPool이 비어있어 상점을 갱신할 수 없습니다.");
        }

        Debug.Log($"상점 리롤 완료! 다음 비용: {GetCurrentRerollCost(ShopRerollCount)}");
        OnShopUpdated?.Invoke();
    }

    public void RerollHatchery(bool freeReroll = false)
    {
        if (!freeReroll)
        {
            int cost = GetCurrentRerollCost(HatcheryRerollCount); // 별도 리롤 카운트 사용
            if (!EconomyManager.Instance.SpendGold(cost)) return;
            HatcheryRerollCount++;
        }

        if (HatcheryPool.Count > 0)
        {
            CurrentHatcheryItems.Clear();
            for (int i = 0; i < 3; i++) // 3칸
            {
                int randomIndex = UnityEngine.Random.Range(0, HatcheryPool.Count);
                CurrentHatcheryItems.Add(HatcheryPool[randomIndex]);
            }
        }
        else
        {
            Debug.LogWarning("[ShopManager] HatcheryPool이 비어있어 부화장을 갱신할 수 없습니다.");
        }

        Debug.Log($"부화장 리롤 완료! 다음 비용: {GetCurrentRerollCost(HatcheryRerollCount)}");
        OnShopUpdated?.Invoke();
    }

    private ShopItemBase GetWeightedRandomItem()
    {
        // 부적 53.3%, 장비 40.0%, 즉시사용 6.7% (8 : 6 : 1 비율)
        float totalWeight = 15f; 
        float randomVal = UnityEngine.Random.Range(0, totalWeight);

        ShopItemType targetType;
        if (randomVal < 8f) targetType = ShopItemType.Charm;
        else if (randomVal < 14f) targetType = ShopItemType.Equipment;
        else targetType = ShopItemType.Instant;

        var typePool = ItemPool.FindAll(item => item.Type == targetType);
        
        if (typePool.Count == 0)
        {
            // 해당 타입이 없으면 전체 풀에서 무작위 선택 (안전장치)
            return ItemPool[UnityEngine.Random.Range(0, ItemPool.Count)];
        }
        
        return typePool[UnityEngine.Random.Range(0, typePool.Count)];
    }

    // 아이템 지정 구매 (부적, 즉시사용 등 클릭 구매)
    public void BuyShopItem(int slotIndex)
    {
        if (GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;
        if (slotIndex < 0 || slotIndex >= CurrentShopItems.Count) return;

        ShopItemBase item = CurrentShopItems[slotIndex];
        
        if (item == null) return;
        if (item.Type == ShopItemType.Equipment)
        {
            Debug.LogWarning("[ShopManager] 장비는 클릭하여 구매할 수 없습니다. 펭귄에게 드래그 앤 드랍하세요.");
            return;
        }

        if (!EconomyManager.Instance.SpendGold(item.Price)) return;

        switch (item.Type)
        {
            case ShopItemType.Charm:
                var charmInstance = Instantiate(item);
                charmInstance.CurrentUses = charmInstance.MaxUses;
                if (ItemInventoryManager.Instance != null && !ItemInventoryManager.Instance.AddCharm(charmInstance))
                {
                    Destroy(charmInstance);
                    EconomyManager.Instance.AddGold(item.Price); // 인벤토리 꽉 찼으면 환불
                    return; 
                }
                break;
            case ShopItemType.Instant:
                var instantItem = item as InstantItem;
                if (instantItem != null) instantItem.ExecuteInstantEffect();
                break;
        }

        CurrentShopItems[slotIndex] = null;
        OnShopUpdated?.Invoke();
    }

    // 부화장 펭귄 지정 구매
    public void BuyHatcheryItem(int slotIndex)
    {
        if (GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;
        if (slotIndex < 0 || slotIndex >= CurrentHatcheryItems.Count) return;

        var penguinDef = CurrentHatcheryItems[slotIndex];
        if (penguinDef == null) return;

        // 골드 차감 검사 
        // 펭귄 가격을 기획에 따라 여기서 설정하거나 PenguinDefinition에서 가져와야 함 (임시 10골드)
        int penguinPrice = 10; 
        if (EconomyManager.Instance.CurrentGold < penguinPrice) return;

        // 펭귄 덱 남은 자리 확인 (벤치와 인벤토리 모두 검사)
        if (PenguinDeckManager.Instance != null)
        {
            int currentBenchCount = PenguinDeckManager.Instance.OwnedPenguins.Count(p => p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
            int currentInventoryCount = PenguinDeckManager.Instance.GetTotalInventoryCount();
            if (currentBenchCount >= PenguinDeckManager.Instance.MaxBenchCapacity && currentInventoryCount >= PenguinDeckManager.Instance.MaxInventoryCapacity)
            {
                Debug.LogWarning("[ShopManager] 펭귄 슬롯이 가득 찼습니다.");
                return;
            }
        }

        if (EconomyManager.Instance.SpendGold(penguinPrice))
        {
            // 펭귄 생성
            if (PenguinDeckManager.Instance != null)
            {
                // 생성 위치는 보통 벤치 혹은 특정 스폰 지점
                Vector3 spawnPos = defaultPenguinSpawnPoint != null ? defaultPenguinSpawnPoint.position : Vector3.zero;
                var newPenguin = PenguinDeckManager.Instance.SpawnPenguin(penguinDef, spawnPos);
                if (newPenguin != null)
                {
                    PenguinDeckManager.Instance.RegisterPenguin(newPenguin.gameObject);
                }
            }

            CurrentHatcheryItems[slotIndex] = null; // 품절 처리
            OnShopUpdated?.Invoke();
        }
    }

    // 장비 드래그 앤 드랍 장착 구매
    public bool BuyEquipmentWithTarget(int slotIndex, PenguinController targetPenguin)
    {
        if (GameManager.Instance.CurrentState != GameState.MaintenanceTime) return false;
        if (slotIndex < 0 || slotIndex >= CurrentShopItems.Count) return false;

        ShopItemBase item = CurrentShopItems[slotIndex];
        if (item == null || item.Type != ShopItemType.Equipment) return false;

        var equipmentItem = item as EquipmentItem;
        if (equipmentItem == null) return false;

        if (targetPenguin == null || targetPenguin.EquipmentHandler == null) return false;

        // 골드가 충분한지 먼저 확인 (차감은 장착 성공 시에 함)
        if (EconomyManager.Instance.CurrentGold < item.Price) return false;

        // 상점 장착은 장비가 0개일 때만 가능 (기본 슬롯 1)
        if (targetPenguin.EquipmentHandler.EquippedList.Count > 0)
        {
            Debug.LogWarning("장비가 이미 장착되어 있습니다.");
            return false;
        }

        // 장착 시도
        if (equipmentItem.EquipmentData != null && targetPenguin.EquipmentHandler.Equip(equipmentItem.EquipmentData))
        {
            EconomyManager.Instance.SpendGold(item.Price);
            CurrentShopItems[slotIndex] = null;
            OnShopUpdated?.Invoke();
            return true;
        }
        else if (equipmentItem.EquipmentData == null)
        {
            Debug.LogError($"[ShopManager] {equipmentItem.ItemName}에 EquipmentData가 할당되지 않았습니다.");
        }

        return false;
    }

    // 판매
    public void SellItem(object itemToSell)
    {
        if (GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;

        if (itemToSell is GameObject penguinObj)
        {
            var controller = penguinObj.GetComponent<PenguinController>();
            // 펭귄 판매 시
            controller?.EquipmentHandler?.ClearAllEquipments(false);
            if (PenguinDeckManager.Instance != null)
                PenguinDeckManager.Instance.UnregisterPenguin(penguinObj);
            
            Destroy(penguinObj);
            EconomyManager.Instance.AddGold(10); // TODO: 판매가 산정식 적용
            Debug.Log("[ShopManager] 펭귄 판매 완료");
        }
        else if (itemToSell is ShopItemBase item && item.Type == ShopItemType.Charm)
        {
            // 부적 판매 시
            if (ItemInventoryManager.Instance != null)
                ItemInventoryManager.Instance.RemoveCharm(item);
            
            EconomyManager.Instance.AddGold(10); // TODO: 판매가 산정식 적용
            Debug.Log("[ShopManager] 부적 판매 완료");
        }
    }

    #region Penguin Spawn Logic
    public PenguinAbilityBase SpawnPenguin(GameObject penguinPrefab, Vector3? spawnPosition = null, Transform parent = null)
    {
        if (penguinPrefab == null)
        {
            Debug.LogError("[ShopManager] 소환할 펭귄 프리팹이 null입니다.");
            return null;
        }

        // 스폰 위치 결정 (전달된 위치가 없으면 Inspector에 지정된 기본 스폰 지점 또는 ShopManager 위치)
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (spawnPosition.HasValue)
        {
            targetPosition = spawnPosition.Value;
            targetRotation = Quaternion.identity;
        }
        else if (penguinLauncher != null && penguinLauncher.LaunchPoint != null)
        {
            targetPosition = penguinLauncher.LaunchPoint.position;
            targetRotation = penguinLauncher.LaunchPoint.rotation;
        }
        else
        {
            targetPosition = transform.position;
            targetRotation = Quaternion.identity;
        }
        // 프리팹 생성
        GameObject spawnedObj = Instantiate(penguinPrefab, targetPosition, targetRotation, parent);

        // 공통 베이스 컴포넌트 추출
        if (spawnedObj.TryGetComponent<PenguinAbilityBase>(out var penguinAbility))
        {
            // 필요 시 초기화 로직 (예: penguinAbility.Initialize();) 추가 가능
            Debug.Log($"[ShopManager] {spawnedObj.name} 펭귄이 성공적으로 소환되었습니다.");
            return penguinAbility;
        }

        Debug.LogWarning($"[ShopManager] {spawnedObj.name} 게임오브젝트에서 PenguinAbilityBase를 찾을 수 없습니다.");
        return null;
    }

    #endregion
}