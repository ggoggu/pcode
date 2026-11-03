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
    [SerializeField] private List<TextMeshProUGUI> hatcheryItemDescriptionTexts;
    [SerializeField] private List<PenguinPinball.UI.Views.Penguin3DPreview> slotPreviews;
    
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

        EnsureDescriptionTexts();
        EnsureSlotPreviews();

        if (hatcheryItemButtons != null)
        {
            for (int i = 0; i < hatcheryItemButtons.Count; i++)
            {
                if (hatcheryItemButtons[i] == null) continue;
                int index = i; 
                hatcheryItemButtons[i].onClick = new Button.ButtonClickedEvent();
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
        PlacementManager.OnPlacementStarted += HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementEnded += HandlePlacementVisibilityChanged;
        UpdateVisibility();
    }
    
    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleGameStateChanged;
        PlacementManager.OnPlacementStarted -= HandlePlacementVisibilityChanged;
        PlacementManager.OnPlacementEnded -= HandlePlacementVisibilityChanged;

        if (slotPreviews != null)
        {
            for (int i = 0; i < slotPreviews.Count; i++)
            {
                if (slotPreviews[i] != null)
                {
                    slotPreviews[i].SetPreviewActive(false);
                }
            }
        }
    }

    private void OnDestroy()
    {
        viewModel.Dispose();
    }

    private void HandlePlacementVisibilityChanged()
    {
        UpdateVisibility();
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
        bool isPlacing = (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing);
        bool shouldShow = isMaintenance && isOpen && !isPlacing;
        
        hatcheryCanvasGroup.alpha = shouldShow ? 1f : 0f;
        hatcheryCanvasGroup.interactable = shouldShow;
        hatcheryCanvasGroup.blocksRaycasts = shouldShow;

        if (slotPreviews != null)
        {
            for (int i = 0; i < slotPreviews.Count; i++)
            {
                if (slotPreviews[i] != null)
                {
                    slotPreviews[i].SetPreviewActive(shouldShow);
                }
            }
        }
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
        EnsureDescriptionTexts();
        EnsureSlotPreviews();
        if (goldText != null) goldText.text = $"Gold: {viewModel.CurrentGold}";
        if (hatcheryRerollCostText != null)
        {
            hatcheryRerollCostText.text = viewModel.HasFreeReroll 
                ? $"부화장 리롤 (무료 {viewModel.RemainingFreeRerolls}회)" 
                : $"부화장 리롤 ({viewModel.HatcheryRerollCost}G)";
        }

        if (hatcheryRerollButton != null)
        {
            hatcheryRerollButton.interactable = viewModel.HasFreeReroll || viewModel.CurrentGold >= viewModel.HatcheryRerollCost;
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

                if (hatcheryItemDescriptionTexts != null && hatcheryItemDescriptionTexts.Count > i && hatcheryItemDescriptionTexts[i] != null)
                    hatcheryItemDescriptionTexts[i].text = slotData.IsSoldOut ? "" : slotData.Description;

                bool has3D = slotData.PenguinDef != null && slotData.PenguinDef.VisualPrefab != null && !slotData.IsSoldOut;

                if (slotPreviews != null && slotPreviews.Count > i && slotPreviews[i] != null)
                {
                    if (has3D)
                    {
                        slotPreviews[i].SetModel(slotData.PenguinDef);
                        slotPreviews[i].SetPreviewActive(isOpen);
                    }
                    else
                    {
                        slotPreviews[i].ClearModel();
                    }
                }

                if (hatcheryItemIconImages != null && hatcheryItemIconImages.Count > i && hatcheryItemIconImages[i] != null)
                {
                    if (has3D)
                    {
                        hatcheryItemIconImages[i].enabled = false;
                    }
                    else if (slotData.Icon != null && !slotData.IsSoldOut)
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

    private void EnsureSlotPreviews()
    {
        if (hatcheryItemButtons == null) return;
        if (slotPreviews == null) slotPreviews = new List<PenguinPinball.UI.Views.Penguin3DPreview>();

        for (int i = 0; i < hatcheryItemButtons.Count; i++)
        {
            var btn = hatcheryItemButtons[i];
            if (btn == null)
            {
                if (i >= slotPreviews.Count) slotPreviews.Add(null);
                continue;
            }

            var preview = btn.GetComponent<PenguinPinball.UI.Views.Penguin3DPreview>();
            if (preview == null)
            {
                preview = btn.gameObject.AddComponent<PenguinPinball.UI.Views.Penguin3DPreview>();
            }

            Transform previewTf = btn.transform.Find("ModelPreview");
            RawImage rawImg = previewTf != null ? previewTf.GetComponent<RawImage>() : null;
            if (rawImg == null)
            {
                var previewObj = new GameObject("ModelPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                previewObj.transform.SetParent(btn.transform, false);

                var rt = previewObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(80f, 80f);
                rt.anchoredPosition = new Vector2(0f, 15f);

                rawImg = previewObj.GetComponent<RawImage>();
                rawImg.raycastTarget = false;
                previewObj.transform.SetSiblingIndex(1);
            }

            rawImg.raycastTarget = false;
            preview.Initialize(i, rawImg, 3050f, "HatcheryPreviewRig");

            if (i < slotPreviews.Count) slotPreviews[i] = preview;
            else slotPreviews.Add(preview);
        }
    }

    private void EnsureDescriptionTexts()
    {
        if (hatcheryItemButtons == null) return;

        if (hatcheryItemDescriptionTexts == null)
            hatcheryItemDescriptionTexts = new List<TextMeshProUGUI>();

        for (int i = 0; i < hatcheryItemButtons.Count; i++)
        {
            if (hatcheryItemButtons[i] == null) continue;

            if (i < hatcheryItemDescriptionTexts.Count && hatcheryItemDescriptionTexts[i] != null)
                continue;

            TextMeshProUGUI descTmp = null;
            Transform descTf = hatcheryItemButtons[i].transform.Find("Description");
            if (descTf != null)
            {
                descTmp = descTf.GetComponent<TextMeshProUGUI>();
            }

            if (descTmp == null)
            {
                var allTmps = hatcheryItemButtons[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTmps)
                {
                    if (t.gameObject.name.Equals("Description", System.StringComparison.OrdinalIgnoreCase))
                    {
                        descTmp = t;
                        break;
                    }
                }
            }

            if (i < hatcheryItemDescriptionTexts.Count)
                hatcheryItemDescriptionTexts[i] = descTmp;
            else
                hatcheryItemDescriptionTexts.Add(descTmp);
        }
    }
}
