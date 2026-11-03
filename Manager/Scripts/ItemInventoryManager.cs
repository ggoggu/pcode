using System;
using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

public class ItemInventoryManager : MonoBehaviour
{
    public static ItemInventoryManager Instance { get; private set; }

    // 펭귄 장착용 장비
    public List<EquipmentSO> UnusedEquipments { get; private set; } = new List<EquipmentSO>();
    
    // 범퍼 등 필드 오브젝트 장착용 각인
    public List<EngravingItem> UnusedEngravings { get; private set; } = new List<EngravingItem>();
    
    // 보유 부적
    public List<ShopItemBase> OwnedCharms { get; private set; } = new List<ShopItemBase>();
    [SerializeField] private int maxCharmCount = 9;

    [Header("초기 보유 부적 (테스트 및 시작 지급용)")]
    [SerializeField] private List<ShopItemBase> initialCharms = new List<ShopItemBase>();
    public List<ShopItemBase> InitialCharms => initialCharms;

    // 인벤토리 9칸 고정 슬롯 (부적, 장비, 펭귄 공유)
    public const int MaxSlotCount = 9;
    private readonly object[] inventorySlots = new object[MaxSlotCount];
    public IReadOnlyList<object> InventorySlots => inventorySlots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (initialCharms != null && initialCharms.Count > 0)
        {
            foreach (var charm in initialCharms)
            {
                if (charm != null)
                {
                    var charmInstance = Instantiate(charm);
                    AddCharm(charmInstance);
                }
            }
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        // F6: 테스트용 부적 1개 추가, F7: 부적 1개 제거 (벤치 UI 실시간 테스트 편의)
        if (keyboard.f6Key.wasPressedThisFrame)
        {
            DebugAddTestCharm();
        }
        else if (keyboard.f7Key.wasPressedThisFrame)
        {
            if (OwnedCharms.Count > 0)
            {
                RemoveCharm(OwnedCharms[OwnedCharms.Count - 1]);
            }
        }
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Debug: Add Test Charm (F6)")]
    public void DebugAddTestCharm()
    {
        string[] testPaths = new string[]
        {
            "Assets/ShopItem/Charm/Charm_B01_FirstBenchAttack_T1.asset",
            "Assets/ShopItem/Charm/Charm_B01_FirstBenchAttack_T1.asset",
            "Assets/ShopItem/Charm/Charm_A01_MarkAmplification_T2.asset",
            "Assets/ShopItem/Charm/Charm_A03_CounterWave_T2.asset",
            "Assets/ShopItem/Charm/Charm_E02_SynthesisClone_T3.asset",
            "Assets/ShopItem/Charm/Charm_E06_RepeatCustomerStore_T2.asset"
        };

#if UNITY_EDITOR
        int currentCount = OwnedCharms.Count;
        int nextIdx = currentCount % testPaths.Length;
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ShopItemBase>(testPaths[nextIdx]);
        if (asset != null)
        {
            var inst = Instantiate(asset);
            AddCharm(inst);
        }
#endif
    }

    [ContextMenu("Debug: Clear All Charms")]
    public void DebugClearCharms()
    {
        while (OwnedCharms.Count > 0)
        {
            RemoveCharm(OwnedCharms[0]);
        }
    }
#endif

    public bool AddCharm(ShopItemBase charm)
    {
        if (charm == null) return false;

        // 1. 이미 같은 종류의 부적이 인벤토리에 있는지 검사
        ShopItemBase existingCharm = OwnedCharms.Find(c => IsSameCharm(c, charm));
        if (existingCharm != null)
        {
            // 기존 부적 효과 해제 및 인벤토리에서 제거 후 교체
            existingCharm.RemoveEffects(gameObject);
            int existingIndex = OwnedCharms.IndexOf(existingCharm);
            OwnedCharms[existingIndex] = charm;

            int slotIdx = Array.IndexOf(inventorySlots, existingCharm);
            if (slotIdx >= 0) inventorySlots[slotIdx] = charm;
            else AssignToFirstEmptySlot(charm);

            if (Application.isPlaying) Destroy(existingCharm);
            else DestroyImmediate(existingCharm);

            // 새 부적 효과 적용
            charm.ApplyEffects(gameObject);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[ItemInventoryManager] 기존 부적({charm.ItemName}) 교체 완료 (남은 횟수: {charm.CurrentUses}/{charm.MaxUses})");
            return true;
        }

        // 2. 신규 부적인 경우 슬롯 한도 체크
        if (OwnedCharms.Count >= maxCharmCount) return false;
        
        OwnedCharms.Add(charm);
        AssignToFirstEmptySlot(charm);
        charm.ApplyEffects(gameObject);
        
        OnInventoryChanged?.Invoke();
        Debug.Log($"[ItemInventoryManager] 새 부적({charm.ItemName}) 추가 완료");
        return true;
    }

    public void RemoveCharm(ShopItemBase charm)
    {
        if (charm != null && OwnedCharms.Contains(charm))
        {
            charm.RemoveEffects(gameObject);
            OwnedCharms.Remove(charm);

            int slotIdx = Array.IndexOf(inventorySlots, charm);
            if (slotIdx >= 0) inventorySlots[slotIdx] = null;

            OnInventoryChanged?.Invoke();
            Debug.Log($"[ItemInventoryManager] 부적 제거: {charm.ItemName}");
            if (Application.isPlaying) Destroy(charm);
            else DestroyImmediate(charm);
        }
    }

    public static bool IsSameCharm(ShopItemBase a, ShopItemBase b)
    {
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.ItemID) && !string.IsNullOrEmpty(b.ItemID))
        {
            return a.ItemID == b.ItemID;
        }
        return a.ItemName == b.ItemName;
    }

    public void UseCharm(ShopItemBase charm)
    {
        if (charm != null && OwnedCharms.Contains(charm))
        {
            charm.UseItem();
        }
    }

    public void AddEquipment(EquipmentSO item)
    {
        UnusedEquipments.Add(item);
        AssignToFirstEmptySlot(item);
        OnInventoryChanged?.Invoke();
    }

    public bool EquipToPenguin(EquipmentSO item, PenguinController target)
    {
        if (!UnusedEquipments.Contains(item)) return false;

        if (target != null && target.EquipmentHandler != null && target.EquipmentHandler.Equip(item))
        {
            UnusedEquipments.Remove(item);
            int slotIdx = Array.IndexOf(inventorySlots, item);
            if (slotIdx >= 0) inventorySlots[slotIdx] = null;

            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    // 각인(범퍼 등 오브젝트용) 추가 복구
    public void AddEngraving(EngravingItem item)
    {
        UnusedEngravings.Add(item);
        AssignToFirstEmptySlot(item);
        OnInventoryChanged?.Invoke();
    }

    // 오브젝트에 각인 장착 복구
    public bool EngraveToObject(EngravingItem item, IEngravable target)
    {
        if (!UnusedEngravings.Contains(item)) return false;
        
        if (target != null && target.TryEngrave(item))
        {
            UnusedEngravings.Remove(item);
            int slotIdx = Array.IndexOf(inventorySlots, item);
            if (slotIdx >= 0) inventorySlots[slotIdx] = null;

            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    #region Inventory Slot Management
    public object GetItemAtSlot(int index)
    {
        SyncSlotsWithSources();
        if (index < 0 || index >= inventorySlots.Length) return null;
        return inventorySlots[index];
    }

    public bool RegisterInventoryPenguin(GameObject penguinObj, int preferredSlot = -1)
    {
        if (penguinObj == null) return false;
        int existingIdx = Array.IndexOf(inventorySlots, penguinObj);
        if (existingIdx >= 0) return true;

        if (preferredSlot >= 0 && preferredSlot < inventorySlots.Length && inventorySlots[preferredSlot] == null)
        {
            inventorySlots[preferredSlot] = penguinObj;
            OnInventoryChanged?.Invoke();
            return true;
        }

        bool assigned = AssignToFirstEmptySlot(penguinObj);
        if (assigned) OnInventoryChanged?.Invoke();
        return assigned;
    }

    public void UnregisterInventoryPenguin(GameObject penguinObj)
    {
        if (penguinObj == null) return;
        int idx = Array.IndexOf(inventorySlots, penguinObj);
        if (idx >= 0)
        {
            inventorySlots[idx] = null;
            OnInventoryChanged?.Invoke();
        }
    }

    public bool SwapOrMoveInventorySlot(int fromIndex, int toIndex)
    {
        SyncSlotsWithSources();
        if (fromIndex < 0 || fromIndex >= inventorySlots.Length) return false;
        if (toIndex < 0 || toIndex >= inventorySlots.Length) return false;
        if (fromIndex == toIndex) return true;

        object fromItem = inventorySlots[fromIndex];
        object toItem = inventorySlots[toIndex];

        if (fromItem == null) return false;

        // 특수 케이스: 장비를 펭귄 슬롯으로 드래그했을 때 장착 시도
        if (fromItem is EquipmentSO equipData && toItem is GameObject targetPenguinObj)
        {
            var controller = targetPenguinObj.GetComponent<PenguinController>();
            if (controller != null && EquipToPenguin(equipData, controller))
            {
                return true;
            }
        }

        // 일반 이동 또는 스왑
        inventorySlots[toIndex] = fromItem;
        inventorySlots[fromIndex] = toItem;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void SetSlotItem(int index, object item)
    {
        if (index >= 0 && index < inventorySlots.Length)
        {
            inventorySlots[index] = item;
            OnInventoryChanged?.Invoke();
        }
    }

    public bool AssignToFirstEmptySlot(object item)
    {
        if (item == null) return false;
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = item;
                return true;
            }
        }
        return false;
    }

    public void SyncSlotsWithSources()
    {
        // 1. 유효하지 않은 항목 정리
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            var item = inventorySlots[i];
            if (item == null) continue;

            if (item is ShopItemBase charm && !OwnedCharms.Contains(charm))
                inventorySlots[i] = null;
            else if (item is EquipmentSO equip && !UnusedEquipments.Contains(equip))
                inventorySlots[i] = null;
            else if (item is EngravingItem eng && !UnusedEngravings.Contains(eng))
                inventorySlots[i] = null;
            else if (item is GameObject peng)
            {
                if (PenguinDeckManager.Instance == null ||
                    !PenguinDeckManager.Instance.OwnedPenguins.Contains(peng) ||
                    peng.GetComponent<PenguinLocationData>()?.Location != PenguinLocation.Inventory)
                {
                    inventorySlots[i] = null;
                }
            }
        }

        // 2. 누락된 항목 빈 슬롯에 배치
        foreach (var charm in OwnedCharms)
        {
            if (charm != null && Array.IndexOf(inventorySlots, charm) < 0)
                AssignToFirstEmptySlot(charm);
        }

        foreach (var equip in UnusedEquipments)
        {
            if (equip != null && Array.IndexOf(inventorySlots, equip) < 0)
                AssignToFirstEmptySlot(equip);
        }

        foreach (var eng in UnusedEngravings)
        {
            if (eng != null && Array.IndexOf(inventorySlots, eng) < 0)
                AssignToFirstEmptySlot(eng);
        }

        if (PenguinDeckManager.Instance != null)
        {
            foreach (var peng in PenguinDeckManager.Instance.OwnedPenguins)
            {
                if (peng != null && peng.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Inventory)
                {
                    if (Array.IndexOf(inventorySlots, peng) < 0)
                        AssignToFirstEmptySlot(peng);
                }
            }
        }
    }
    #endregion
}