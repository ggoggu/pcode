using System;
using System.Collections.Generic;

public class ItemShopViewModel
{
    public int CurrentGold { get; private set; }
    public int ShopRerollCost { get; private set; }
    
    public List<ShopItemViewModel> ShopSlots { get; private set; } = new List<ShopItemViewModel>();

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
        ShopRerollCost = ShopManager.Instance.GetCurrentRerollCost(ShopManager.Instance.ShopRerollCount);
        
        // 상점 데이터 갱신
        ShopSlots.Clear();
        for (int i = 0; i < ShopManager.Instance.CurrentShopItems.Count; i++)
        {
            var item = ShopManager.Instance.CurrentShopItems[i];
            ShopSlots.Add(new ShopItemViewModel
            {
                SlotIndex = i,
                ItemName = item != null ? item.ItemName : "품절",
                Price = item != null ? item.Price : 0,
                ItemTypeStr = item != null ? item.Type.ToString() : "-",
                Icon = item != null ? item.Icon : null,
                IsSoldOut = item == null
            });
        }
        
        // 뷰에 UI 갱신 지시
        OnDataChanged?.Invoke();
    }

    // View에서 버튼 클릭 시 호출할 명령(Command)
    public void RequestBuyShopItem(int index) => ShopManager.Instance.BuyShopItem(index);
    public void RequestRerollShop() => ShopManager.Instance.RerollShop(false);
}
