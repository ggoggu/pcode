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

    [Header("펭귄 구매 설정")]
    [SerializeField] private int basePenguinPrice = 10;
    public int BasePenguinPrice => basePenguinPrice;

    [Header("단골 상점(재구매) 상태")]
    private Effect_RepeatCustomerStore activeRepeatStoreEffect;
    private readonly HashSet<int> repurchaseSlots = new HashSet<int>();

    [Header("파도타기(무료 리롤) 상태")]
    private Effect_Surfing activeSurfingEffect;

    public bool HasFreeRerolls => activeSurfingEffect != null && activeSurfingEffect.HasFreeRerolls;
    public int RemainingFreeRerolls => activeSurfingEffect != null ? activeSurfingEffect.RemainingFreeRerolls : 0;
    public Effect_Surfing ActiveSurfingEffect => activeSurfingEffect;

    public void RegisterSurfingEffect(Effect_Surfing effect)
    {
        activeSurfingEffect = effect;
        Debug.Log("[ShopManager] 파도타기 효과 등록됨");
        OnShopUpdated?.Invoke();
    }

    public void UnregisterSurfingEffect(Effect_Surfing effect)
    {
        if (activeSurfingEffect == effect)
        {
            activeSurfingEffect = null;
            Debug.Log("[ShopManager] 파도타기 효과 해제됨");
            OnShopUpdated?.Invoke();
        }
    }

    public void NotifyShopUpdated()
    {
        OnShopUpdated?.Invoke();
    }

    public bool IsRepeatStoreActive => activeRepeatStoreEffect != null 
        && (activeRepeatStoreEffect.OwnerItem == null || !activeRepeatStoreEffect.OwnerItem.IsConsumable || activeRepeatStoreEffect.OwnerItem.CurrentUses > 0);

    public void RegisterRepeatStoreEffect(Effect_RepeatCustomerStore effect)
    {
        activeRepeatStoreEffect = effect;
        Debug.Log("[ShopManager] 단골 상점 효과 등록됨");
    }

    public void UnregisterRepeatStoreEffect(Effect_RepeatCustomerStore effect)
    {
        if (activeRepeatStoreEffect == effect)
        {
            activeRepeatStoreEffect = null;
            ClearAllRepurchaseSlots(revertToSoldOut: true);
            Debug.Log("[ShopManager] 단골 상점 효과 해제됨");
            OnShopUpdated?.Invoke();
        }
    }

    public bool IsHatcherySlotRepurchase(int slotIndex)
    {
        return repurchaseSlots.Contains(slotIndex);
    }

    public int GetHatcherySlotPrice(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CurrentHatcheryItems.Count) return basePenguinPrice;
        if (IsHatcherySlotRepurchase(slotIndex))
        {
            int multiplier = activeRepeatStoreEffect != null ? Mathf.RoundToInt(activeRepeatStoreEffect.PriceMultiplier) : 2;
            return basePenguinPrice * multiplier;
        }
        return basePenguinPrice;
    }

    public void ClearRepurchaseSlot(int slotIndex)
    {
        repurchaseSlots.Remove(slotIndex);
    }

    public void ClearAllRepurchaseSlots(bool revertToSoldOut = false)
    {
        if (revertToSoldOut)
        {
            foreach (int slot in repurchaseSlots)
            {
                if (slot >= 0 && slot < CurrentHatcheryItems.Count)
                {
                    CurrentHatcheryItems[slot] = null;
                }
            }
        }
        repurchaseSlots.Clear();
    }

    [Header("펭귄 소환 설정")]
    [Tooltip("펭귄이 생성될 기본 위치/트랜스폼입니다.")]
    [SerializeField] private Transform defaultPenguinSpawnPoint;
    [SerializeField] private PenguinLauncher penguinLauncher;

    public event Action OnShopUpdated;
    public event Action<InstantItem, int> OnInstantTargetSelectionRequested;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CheckExistingRepeatStoreCharm();
        CheckExistingSurfingCharm();
        InitializeShopItems();
    }

    private void InitializeShopItems()
    {
        if (CurrentShopItems == null)
        {
            CurrentShopItems = new List<ShopItemBase>(3);
        }

        // 게임 시작 시 상점 아이템이 비어있다면 풀에서 생성
        if (CurrentShopItems.Count == 0 && ItemPool != null && ItemPool.Count > 0)
        {
            RerollShop(freeReroll: true);
        }

        if (CurrentHatcheryItems == null)
        {
            CurrentHatcheryItems = new List<PenguinPinball.Core.PenguinDefinition>(3);
        }

        // 게임 시작 시 부화장 아이템이 비어있다면 풀에서 생성
        if (CurrentHatcheryItems.Count == 0 && HatcheryPool != null && HatcheryPool.Count > 0)
        {
            RerollHatchery(freeReroll: true);
        }
    }

    private void CheckExistingRepeatStoreCharm()
    {
        if (ItemInventoryManager.Instance == null) return;
        foreach (var charm in ItemInventoryManager.Instance.OwnedCharms)
        {
            if (charm == null || charm.Effects == null) continue;
            foreach (var eff in charm.Effects)
            {
                if (eff is Effect_RepeatCustomerStore repeatEff)
                {
                    RegisterRepeatStoreEffect(repeatEff);
                    return;
                }
            }
        }
    }

    private void CheckExistingSurfingCharm()
    {
        if (ItemInventoryManager.Instance == null) return;
        foreach (var charm in ItemInventoryManager.Instance.OwnedCharms)
        {
            if (charm == null || charm.Effects == null) continue;
            foreach (var eff in charm.Effects)
            {
                if (eff is Effect_Surfing surfingEff)
                {
                    RegisterSurfingEffect(surfingEff);
                    return;
                }
            }
        }
    }
    
    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }
    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    public bool IsFirstMaintenance => MapManager.Instance != null && MapManager.Instance.IsFirstMaintenance;

    private void HandleStateChanged(GameState state)
    {
        // 웨이브 종료(정비시간 진입) 시 리롤 비용 초기화 및 진열 자동 갱신
        if (state == GameState.MaintenanceTime)
        {
            ShopRerollCount = 0;
            HatcheryRerollCount = 0;
            ClearAllRepurchaseSlots(revertToSoldOut: false);

            RerollShop(freeReroll: true);
            RerollHatchery(freeReroll: true); 
        }
        else
        {
            ClearAllRepurchaseSlots(revertToSoldOut: true);
        }
    }

    public int GetCurrentRerollCost(int count) => baseRerollCost * (int)Mathf.Pow(2, count);

    public int GetShopRerollCost()
    {
        if (HasFreeRerolls) return 0;
        return GetCurrentRerollCost(ShopRerollCount);
    }

    public int GetHatcheryRerollCost()
    {
        if (IsFirstMaintenance) return -1;
        if (HasFreeRerolls) return 0;
        return GetCurrentRerollCost(HatcheryRerollCount);
    }

    // ★ 리롤 시 새로운 아이템을 뽑아오는 로직 추가 ★
    public void RerollShop(bool freeReroll = false)
    {

        if (!freeReroll)
        {
            if (HasFreeRerolls)
            {
                activeSurfingEffect.ConsumeFreeReroll();
            }
            else
            {
                int cost = GetShopRerollCost();
                if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendGold(cost)) return;
                ShopRerollCount++;
            }
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

        Debug.Log($"상점 리롤 완료! 다음 비용: {GetShopRerollCost()} (무료 여부: {HasFreeRerolls})");
        OnShopUpdated?.Invoke();
    }

    public void RerollHatchery(bool freeReroll = false)
    {
        if (!freeReroll && IsFirstMaintenance)
        {
            Debug.LogWarning("[ShopManager] 최초 정비시간에는 부화장 리롤을 이용할 수 없습니다.");
            return;
        }

        if (!freeReroll)
        {
            if (HasFreeRerolls)
            {
                activeSurfingEffect.ConsumeFreeReroll();
            }
            else
            {
                int cost = GetHatcheryRerollCost(); // 별도 리롤 카운트 사용
                if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendGold(cost)) return;
                HatcheryRerollCount++;
            }
        }

        if (HatcheryPool.Count > 0)
        {
            ClearAllRepurchaseSlots(revertToSoldOut: false);
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

        Debug.Log($"부화장 리롤 완료! 다음 비용: {GetHatcheryRerollCost()} (무료 여부: {HasFreeRerolls})");
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
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;
        if (slotIndex < 0 || slotIndex >= CurrentShopItems.Count) return;

        ShopItemBase item = CurrentShopItems[slotIndex];
        
        if (item == null) return;
        if (item.Type == ShopItemType.Equipment)
        {
            Debug.LogWarning("[ShopManager] 장비는 클릭하여 구매할 수 없습니다. 펭귄에게 드래그 앤 드랍하세요.");
            return;
        }

        // 골드 소지 여부 선확인
        if (EconomyManager.Instance != null && EconomyManager.Instance.CurrentGold < item.Price)
        {
            Debug.LogWarning("[ShopManager] 골드가 부족합니다.");
            return;
        }

        // 즉시사용 아이템 사용 가능 여부 선확인 (대상 선택 필요 여부 불문)
        if (item is InstantItem instantItem)
        {
            if (!instantItem.CanUse(out string failReason))
            {
                Debug.LogWarning($"[ShopManager] {instantItem.ItemName} 구매 불가: {failReason}");
                return;
            }

            if (instantItem.RequiresTargetSelection)
            {
                // 대상 선택 UI 오픈 요청 (골드는 대상 선택 완료 시 차감)
                OnInstantTargetSelectionRequested?.Invoke(instantItem, slotIndex);
                return;
            }
        }

        // 일반 구매 (부적 및 대상 없는 즉시사용): 골드 차감 후 발동
        if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendGold(item.Price)) return;

        switch (item.Type)
        {
            case ShopItemType.Charm:
                var charmInstance = item.CreateRuntimeInstance();
                if (ItemInventoryManager.Instance != null && !ItemInventoryManager.Instance.AddCharm(charmInstance))
                {
                    Destroy(charmInstance);
                    EconomyManager.Instance.AddGold(item.Price); // 인벤토리 꽉 찼으면 환불
                    return; 
                }
                break;
            case ShopItemType.Instant:
                var instant = item as InstantItem;
                if (instant != null) instant.ExecuteInstantEffect();
                break;
        }

        CurrentShopItems[slotIndex] = null;
        OnShopUpdated?.Invoke();
    }

    /// <summary>
    /// 대상 선택이 필요한 즉시사용 아이템의 구매 확정 및 효과 발동
    /// </summary>
    public bool CompleteTargetedInstantPurchase(int slotIndex, InstantItem item, params object[] targets)
    {
        if (slotIndex < 0 || slotIndex >= CurrentShopItems.Count) return false;
        if (CurrentShopItems[slotIndex] != item) return false;

        // 아이템 사용 가능 여부 최종 검증
        if (item != null && !item.CanUse(out string failReason))
        {
            Debug.LogWarning($"[ShopManager] {item.ItemName} 구매 불가: {failReason}");
            return false;
        }

        // 골드 차감
        if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendGold(item.Price))
        {
            Debug.LogWarning("[ShopManager] 골드가 부족하여 아이템 사용을 완료할 수 없습니다.");
            return false;
        }

        // 효과 실행
        bool success = item.ExecuteTargetedEffect(targets);
        if (!success)
        {
            // 효과 적용 실패 시 골드 환불
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(item.Price);
            Debug.LogWarning("[ShopManager] 즉시사용 효과 적용에 실패하여 골드가 환불되었습니다.");
            return false;
        }

        // 슬롯 품절 처리 및 상점 갱신
        CurrentShopItems[slotIndex] = null;
        OnShopUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// 대상 선택이 필요한 즉시사용 아이템의 구매 취소 처리
    /// </summary>
    public void CancelTargetedInstantPurchase(int slotIndex)
    {
        Debug.Log($"[ShopManager] 슬롯 {slotIndex} 즉시사용 아이템 대상 선택 취소됨 (골드 차감 없음)");
    }

    // 부화장 펭귄 지정 구매
    public void BuyHatcheryItem(int slotIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;
        if (slotIndex < 0 || slotIndex >= CurrentHatcheryItems.Count) return;

        var penguinDef = CurrentHatcheryItems[slotIndex];
        if (penguinDef == null) return;

        int penguinPrice = GetHatcherySlotPrice(slotIndex);
        if (EconomyManager.Instance != null && EconomyManager.Instance.CurrentGold < penguinPrice) return;

        // 펭귄 덱 남은 자리 확인 (벤치와 인벤토리 모두 검사)
        if (PenguinDeckManager.Instance != null)
        {
            int currentBenchCount = PenguinDeckManager.Instance.OwnedPenguins.Count(p => p != null && p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench);
            int currentInventoryCount = PenguinDeckManager.Instance.GetTotalInventoryCount();
            if (currentBenchCount >= PenguinDeckManager.Instance.EffectiveBenchCapacity && currentInventoryCount >= PenguinDeckManager.Instance.MaxInventoryCapacity)
            {
                Debug.LogWarning("[ShopManager] 펭귄 슬롯이 가득 찼습니다.");
                return;
            }
        }

        bool spendSuccess = EconomyManager.Instance == null || EconomyManager.Instance.SpendGold(penguinPrice);
        if (spendSuccess)
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

            bool wasRepurchase = IsHatcherySlotRepurchase(slotIndex);
            if (wasRepurchase)
            {
                // 재구매 성공 (2배 가격 지불 완료)
                ClearRepurchaseSlot(slotIndex);
                CurrentHatcheryItems[slotIndex] = null; // 1회 추가 구매 완료 -> 품절 처리

                if (activeRepeatStoreEffect != null)
                {
                    activeRepeatStoreEffect.TriggerUse();
                    // 부적 소멸 시 잔여 재구매 슬롯 품절 처리
                    if (activeRepeatStoreEffect.OwnerItem != null && activeRepeatStoreEffect.OwnerItem.CurrentUses <= 0)
                    {
                        ClearAllRepurchaseSlots(revertToSoldOut: true);
                    }
                }
            }
            else
            {
                // 일반 구매 성공
                if (IsRepeatStoreActive)
                {
                    // 단골 상점 부적 효과: 해당 영역(슬롯)에서 동일 펭귄을 2배 가격에 1회 추가 구매 가능하도록 설정
                    repurchaseSlots.Add(slotIndex);
                }
                else
                {
                    CurrentHatcheryItems[slotIndex] = null; // 품절 처리
                }
            }

            OnShopUpdated?.Invoke();
        }
    }

    // 장비 드래그 앤 드랍 장착 구매
    public bool BuyEquipmentWithTarget(int slotIndex, PenguinController targetPenguin)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime) return false;
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
            if (PenguinDeckManager.Instance != null)
            {
                PenguinDeckManager.Instance.SellPenguin(penguinObj, 10);
            }
            else
            {
                var controller = penguinObj.GetComponent<PenguinController>();
                controller?.EquipmentHandler?.ClearAllEquipments(false);
                Destroy(penguinObj);
                EconomyManager.Instance?.AddGold(10);
            }
            Debug.Log("[ShopManager] 펭귄 판매 완료");
        }
        else if (itemToSell is ShopItemBase item && item.Type == ShopItemType.Charm)
        {
            // 부적 판매 시
            if (ItemInventoryManager.Instance != null)
                ItemInventoryManager.Instance.RemoveCharm(item);
            
            int refund = item.GetSellPrice();
            if (item.Effects != null && item.Effects.Any(e => e is Effect_Surfing surfingEff && surfingEff.preventSellRefund))
            {
                refund = 0;
            }

            if (refund > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(refund);
            }
            Debug.Log($"[ShopManager] 부적 판매 완료 (정산금: {refund}G)");
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