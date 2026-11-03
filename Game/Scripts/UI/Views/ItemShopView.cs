using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PenguinPinball.UI.Views;

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
    [SerializeField] private List<TextMeshProUGUI> itemDescriptionTexts;
    
    [Header("토글용 캔버스 그룹")]
    [SerializeField] private CanvasGroup shopCanvasGroup;

    [Header("슬롯 선택 강조 색상")]
    [SerializeField] private Color selectedSlotHighlightColor = new Color(1f, 0.88f, 0.25f, 1f);
    private int highlightedSlotIndex = -1;
    
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

        EnsureDescriptionTexts();

        // 각 슬롯 구매 버튼 연결
        if (itemButtons != null)
        {
            for (int i = 0; i < itemButtons.Count; i++)
            {
                if (itemButtons[i] == null) continue;
                int index = i; // 클로저(Closure) 인덱스 문제 방지
                itemButtons[i].onClick = new Button.ButtonClickedEvent();
                itemButtons[i].onClick.AddListener(() => viewModel.RequestBuyShopItem(index));
            }
        }
    }
    
    private void Start()
    {
        viewModel.Initialize();
        UpdateVisibility();

        EnsureSelectionController();
        SubscribeShopEvents();
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleGameStateChanged;
        SubscribeShopEvents();
    }
    
    private void OnDisable()
    {
        viewModel.Dispose();
        GameManager.OnStateChanged -= HandleGameStateChanged;
        UnsubscribeShopEvents();
    }

    private void SubscribeShopEvents()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnInstantTargetSelectionRequested -= HandleInstantTargetSelectionRequested;
            ShopManager.Instance.OnInstantTargetSelectionRequested += HandleInstantTargetSelectionRequested;
        }

        if (InstantItemSelectionController.Instance != null)
        {
            InstantItemSelectionController.Instance.OnSelectionStateChanged -= HandleSelectionStateChanged;
            InstantItemSelectionController.Instance.OnSelectionStateChanged += HandleSelectionStateChanged;
        }

        PlacementManager.OnPlacementStarted -= HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementStarted += HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementEnded -= HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementEnded += HandlePlacementVisibilityChanged;
    }

    private void UnsubscribeShopEvents()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnInstantTargetSelectionRequested -= HandleInstantTargetSelectionRequested;
        }

        if (InstantItemSelectionController.Instance != null)
        {
            InstantItemSelectionController.Instance.OnSelectionStateChanged -= HandleSelectionStateChanged;
        }

        PlacementManager.OnPlacementStarted -= HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementEnded -= HandlePlacementVisibilityChanged;
    }

    private void HandlePlacementVisibilityChanged()
    {
        UpdateVisibility();
    }

    private void HandleInstantTargetSelectionRequested(InstantItem item, int slotIndex)
    {
        EnsureSelectionController();
        HighlightSlot(slotIndex, true);

        if (InstantItemSelectionController.Instance != null)
        {
            RectTransform slotRect = GetItemSlotRect(slotIndex);
            InstantItemSelectionController.Instance.StartSelection(item, slotIndex, slotRect);
        }
    }

    private void HandleSelectionStateChanged(bool isActive)
    {
        if (!isActive)
        {
            HighlightSlot(highlightedSlotIndex, false);
        }
    }

    public void HighlightSlot(int slotIndex, bool highlight)
    {
        if (slotIndex >= 0 && slotIndex < itemButtons.Count && itemButtons[slotIndex] != null)
        {
            var btnImg = itemButtons[slotIndex].image;
            if (btnImg != null)
            {
                btnImg.color = highlight ? selectedSlotHighlightColor : Color.white;
            }

            if (itemIconImages != null && slotIndex < itemIconImages.Count && itemIconImages[slotIndex] != null)
            {
                itemIconImages[slotIndex].color = highlight ? selectedSlotHighlightColor : Color.white;
            }

            highlightedSlotIndex = highlight ? slotIndex : -1;
        }
        else if (!highlight && highlightedSlotIndex >= 0)
        {
            if (highlightedSlotIndex < itemButtons.Count && itemButtons[highlightedSlotIndex] != null)
            {
                var btnImg = itemButtons[highlightedSlotIndex].image;
                if (btnImg != null) btnImg.color = Color.white;
            }
            if (itemIconImages != null && highlightedSlotIndex < itemIconImages.Count && itemIconImages[highlightedSlotIndex] != null)
            {
                itemIconImages[highlightedSlotIndex].color = Color.white;
            }
            highlightedSlotIndex = -1;
        }
    }

    public RectTransform GetItemSlotRect(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < itemButtons.Count && itemButtons[slotIndex] != null)
        {
            return itemButtons[slotIndex].GetComponent<RectTransform>();
        }
        return GetComponent<RectTransform>();
    }

    private void EnsureSelectionController()
    {
        if (InstantItemSelectionController.Instance == null)
        {
            var controller = FindAnyObjectByType<InstantItemSelectionController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                var canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
                var ctrlObj = new GameObject("InstantItemSelectionController", typeof(InstantItemSelectionController));
                if (canvas != null)
                {
                    ctrlObj.transform.SetParent(canvas.transform, false);
                    ctrlObj.transform.SetAsLastSibling();
                }
            }
        }
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
        if (InstantItemSelectionController.Instance != null && InstantItemSelectionController.Instance.IsActive)
        {
            InstantItemSelectionController.Instance.CancelSelection();
        }
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
        bool isPlacing = (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing);
        bool shouldShow = isMaintenance && isOpen && !isPlacing;
        
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
            if (InstantItemSelectionController.Instance != null && InstantItemSelectionController.Instance.IsActive)
            {
                InstantItemSelectionController.Instance.CancelSelection();
            }
        }
        UpdateVisibility();
    }

    private void UpdateUI()
    {
        EnsureDescriptionTexts();
        if (goldText != null) goldText.text = $"Gold: {viewModel.CurrentGold}";
        if (rerollCostText != null)
        {
            rerollCostText.text = viewModel.HasFreeReroll 
                ? $"리롤 (무료 {viewModel.RemainingFreeRerolls}회)" 
                : $"리롤 ({viewModel.ShopRerollCost}G)";
        }

        if (rerollButton != null)
        {
            rerollButton.interactable = viewModel.HasFreeReroll || viewModel.CurrentGold >= viewModel.ShopRerollCost;
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

                if (itemDescriptionTexts != null && itemDescriptionTexts.Count > i && itemDescriptionTexts[i] != null)
                    itemDescriptionTexts[i].text = slotData.IsSoldOut ? "" : slotData.Description;

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

    private void EnsureDescriptionTexts()
    {
        if (itemButtons == null) return;

        if (itemDescriptionTexts == null)
            itemDescriptionTexts = new List<TextMeshProUGUI>();

        for (int i = 0; i < itemButtons.Count; i++)
        {
            if (itemButtons[i] == null) continue;

            if (i < itemDescriptionTexts.Count && itemDescriptionTexts[i] != null)
                continue;

            TextMeshProUGUI descTmp = null;
            Transform descTf = itemButtons[i].transform.Find("Description");
            if (descTf != null)
            {
                descTmp = descTf.GetComponent<TextMeshProUGUI>();
            }

            if (descTmp == null)
            {
                var allTmps = itemButtons[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTmps)
                {
                    if (t.gameObject.name.Equals("Description", System.StringComparison.OrdinalIgnoreCase))
                    {
                        descTmp = t;
                        break;
                    }
                }
            }

            if (i < itemDescriptionTexts.Count)
                itemDescriptionTexts[i] = descTmp;
            else
                itemDescriptionTexts.Add(descTmp);
        }
    }
}
