using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HatcheryView : MonoBehaviour, IToggleableView
{
    [Header("상단 재화 UI")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("부화장 컨트롤 UI")]
    [SerializeField] private Button hatcheryRerollButton;
    [SerializeField] private TextMeshProUGUI hatcheryRerollCostText;
    
    [Header("부화장 3칸 슬롯 UI")]
    [SerializeField] private List<Button> hatcheryItemButtons;
    [SerializeField] private List<TextMeshProUGUI> hatcheryItemNameTexts;
    [SerializeField] private List<TextMeshProUGUI> hatcheryItemPriceTexts;
    [SerializeField] private List<Image> hatcheryItemIconImages;
    
    [Header("토글용 캔버스 그룹")]
    [SerializeField] private CanvasGroup hatcheryCanvasGroup;
    
    private HatcheryViewModel viewModel;
    private bool isOpen = false;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        viewModel = new HatcheryViewModel();
        viewModel.OnDataChanged += UpdateUI;
        
        if (hatcheryRerollButton != null)
            hatcheryRerollButton.onClick.AddListener(() => viewModel.RequestRerollHatchery());

        if (hatcheryItemButtons != null)
        {
            for (int i = 0; i < hatcheryItemButtons.Count; i++)
            {
                int index = i; 
                hatcheryItemButtons[i].onClick.AddListener(() => viewModel.RequestBuyHatcheryItem(index));
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

    // 토글 기능
    public void ToggleVisibility()
    {
        isOpen = !isOpen;
        UpdateVisibility();
    }

    public void Close()
    {
        isOpen = false;
        UpdateVisibility();
    }

    public void Open()
    {
        isOpen = true;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (hatcheryCanvasGroup == null) return;
        
        bool isMaintenance = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MaintenanceTime);
        bool shouldShow = isMaintenance && isOpen;
        
        hatcheryCanvasGroup.alpha = shouldShow ? 1f : 0f;
        hatcheryCanvasGroup.interactable = shouldShow;
        hatcheryCanvasGroup.blocksRaycasts = shouldShow;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.MaintenanceTime)
        {
            isOpen = false;
        }
        UpdateVisibility();
    }

    private void UpdateUI()
    {
        if (goldText != null) goldText.text = $"Gold: {viewModel.CurrentGold}";
        if (hatcheryRerollCostText != null) hatcheryRerollCostText.text = $"부화장 리롤 ({viewModel.HatcheryRerollCost}G)";

        if (hatcheryRerollButton != null)
        {
            hatcheryRerollButton.interactable = viewModel.CurrentGold >= viewModel.HatcheryRerollCost;
        }

        if (hatcheryItemButtons != null)
        {
            for (int i = 0; i < viewModel.HatcherySlots.Count && i < hatcheryItemButtons.Count; i++)
            {
                var slotData = viewModel.HatcherySlots[i];
                
                hatcheryItemButtons[i].interactable = !slotData.IsSoldOut && viewModel.CurrentGold >= slotData.Price;
                
                if (hatcheryItemNameTexts != null && hatcheryItemNameTexts.Count > i)
                    hatcheryItemNameTexts[i].text = slotData.IsSoldOut ? "품절" : $"[펭귄] {slotData.ItemName}";
                
                if (hatcheryItemPriceTexts != null && hatcheryItemPriceTexts.Count > i)
                    hatcheryItemPriceTexts[i].text = slotData.IsSoldOut ? "-" : $"{slotData.Price} G";

                if (hatcheryItemIconImages != null && hatcheryItemIconImages.Count > i && hatcheryItemIconImages[i] != null)
                {
                    if (slotData.Icon != null && !slotData.IsSoldOut)
                    {
                        hatcheryItemIconImages[i].sprite = slotData.Icon;
                        hatcheryItemIconImages[i].enabled = true;
                    }
                    else
                    {
                        hatcheryItemIconImages[i].enabled = false;
                    }
                }
            }
        }
    }
}
