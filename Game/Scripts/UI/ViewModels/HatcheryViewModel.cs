using System;
using System.Collections.Generic;

public class HatcheryViewModel
{
    public int CurrentGold { get; private set; }
    public int HatcheryRerollCost { get; private set; }
    
    // 부화장(펭귄 상점) 진열 아이템 상태 리스트
    public List<ShopItemViewModel> HatcherySlots { get; private set; } = new List<ShopItemViewModel>();

    // 데이터 갱신 알림 이벤트
    public event Action OnDataChanged;

    public void Initialize()
    {
        // Manager들의 이벤트 구독
        if (ShopManager.Instance != null) ShopManager.Instance.OnShopUpdated += RefreshData;
        if (EconomyManager.Instance != null) EconomyManager.Instance.OnEconomyChanged += RefreshData;
        RefreshData();
    }

    public void Dispose()
    {
        // 메모리 누수 방지용 구독 해제
        if (ShopManager.Instance != null) ShopManager.Instance.OnShopUpdated -= RefreshData;
        if (EconomyManager.Instance != null) EconomyManager.Instance.OnEconomyChanged -= RefreshData;
    }

    private void RefreshData()
    {
        if (EconomyManager.Instance == null || ShopManager.Instance == null) return;

        CurrentGold = EconomyManager.Instance.CurrentGold;
        
        // 부화장 데이터 갱신
        HatcheryRerollCost = ShopManager.Instance.GetCurrentRerollCost(ShopManager.Instance.HatcheryRerollCount);
        HatcherySlots.Clear();
        for (int i = 0; i < ShopManager.Instance.CurrentHatcheryItems.Count; i++)
        {
            var penguinDef = ShopManager.Instance.CurrentHatcheryItems[i];
            HatcherySlots.Add(new ShopItemViewModel
            {
                SlotIndex = i,
                ItemName = penguinDef != null ? penguinDef.name : "품절",
                Price = 10, // 임시 고정가
                ItemTypeStr = "Penguin",
                Icon = penguinDef != null ? penguinDef.Icon : null,
                IsSoldOut = penguinDef == null
            });
        }

        // 뷰에 UI 갱신 지시
        OnDataChanged?.Invoke();
    }

    // View에서 버튼 클릭 시 호출할 명령(Command)
    public void RequestBuyHatcheryItem(int index) => ShopManager.Instance.BuyHatcheryItem(index);
    public void RequestRerollHatchery() => ShopManager.Instance.RerollHatchery(false);
}
