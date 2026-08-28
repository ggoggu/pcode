using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ItemShopView : MonoBehaviour, IToggleableView
{
    [Header("상단 재화 UI")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("일반 상점 컨트롤 UI")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    
    [Header("상점 3칸 슬롯 UI")]
    [SerializeField] private List<Button> itemButtons;
    [SerializeField] private List<TextMeshProUGUI> itemNameTexts;
    [SerializeField] private List<TextMeshProUGUI> itemPriceTexts;
    [SerializeField] private List<Image> itemIconImages;
    
    [Header("토글용 캔버스 그룹")]
    [SerializeField] private CanvasGroup shopCanvasGroup;
    
    private ItemShopViewModel viewModel;
    private bool isOpen = false;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        viewModel = new ItemShopViewModel();
        viewModel.OnDataChanged += UpdateUI; // 뷰모델 이벤트 구독
        
        // 리롤 버튼 연결
        if (rerollButton != null)
            rerollButton.onClick.AddListener(() => viewModel.RequestRerollShop());

        // 각 슬롯 구매 버튼 연결
        if (itemButtons != null)
        {
            for (int i = 0; i < itemButtons.Count; i++)
            {
                int index = i; // 클로저(Closure) 인덱스 문제 방지
                itemButtons[i].onClick.AddListener(() => viewModel.RequestBuyShopItem(index));
            }
        }
    }
    
    private void Start()
    {
        viewModel.Initialize();
        UpdateVisibility();
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleGameStateChanged;
    }
    
    private void OnDisable()
    {
        viewModel.Dispose();
        GameManager.OnStateChanged -= HandleGameStateChanged;
    }

    // 토글 기능: 버튼 등에 연결하여 창을 열고 닫을 수 있음
    public void ToggleVisibility()
    {
        isOpen = !isOpen;
        UpdateVisibility();
    }

    // 창 강제 닫기 (정비 시간이 아닐 때 등)
    public void Close()
    {
        isOpen = false;
        UpdateVisibility();
    }
    
    // 창 열기
    public void Open()
    {
        isOpen = true;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (shopCanvasGroup == null) return;
        
        bool isMaintenance = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MaintenanceTime);
        bool shouldShow = isMaintenance && isOpen;
        
        shopCanvasGroup.alpha = shouldShow ? 1f : 0f;
        shopCanvasGroup.interactable = shouldShow;
        shopCanvasGroup.blocksRaycasts = shouldShow;
    }

    private void HandleGameStateChanged(GameState state)
    {
        // 정비 시간이 아니면 무조건 창을 닫는다.
        if (state != GameState.MaintenanceTime)
        {
            isOpen = false;
        }
        UpdateVisibility();
    }

    private void UpdateUI()
    {
        if (goldText != null) goldText.text = $"Gold: {viewModel.CurrentGold}";
        if (rerollCostText != null) rerollCostText.text = $"리롤 ({viewModel.ShopRerollCost}G)";

        if (rerollButton != null)
        {
            rerollButton.interactable = viewModel.CurrentGold >= viewModel.ShopRerollCost;
        }

        if (itemButtons != null)
        {
            for (int i = 0; i < viewModel.ShopSlots.Count && i < itemButtons.Count; i++)
            {
                var slotData = viewModel.ShopSlots[i];
                
                itemButtons[i].interactable = !slotData.IsSoldOut && viewModel.CurrentGold >= slotData.Price;
                
                if (itemNameTexts != null && itemNameTexts.Count > i)
                    itemNameTexts[i].text = slotData.IsSoldOut ? "품절" : $"[{slotData.ItemTypeStr}] {slotData.ItemName}";
                
                if (itemPriceTexts != null && itemPriceTexts.Count > i)
                    itemPriceTexts[i].text = slotData.IsSoldOut ? "-" : $"{slotData.Price} G";

                if (itemIconImages != null && itemIconImages.Count > i && itemIconImages[i] != null)
                {
                    if (slotData.Icon != null && !slotData.IsSoldOut)
                    {
                        itemIconImages[i].sprite = slotData.Icon;
                        itemIconImages[i].enabled = true;
                    }
                    else
                    {
                        itemIconImages[i].enabled = false;
                    }
                }
            }
        }
    }
}
