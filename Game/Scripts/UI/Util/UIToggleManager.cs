using UnityEngine;
using UnityEngine.UI;

public class UIToggleManager : MonoBehaviour
{
    public static UIToggleManager Instance { get; private set; }

    [Header("패널 Views (IToggleableView)")]
    [SerializeField] private ItemShopView shopView;
    [SerializeField] private HatcheryView hatcheryView;

    [Header("토글 버튼")]
    [SerializeField] private Button shopToggleButton;
    [SerializeField] private Button hatcheryToggleButton;
    [SerializeField] private Button storageToggleButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        FindReferencesIfNull();
    }

    private void OnEnable()
    {
        PlacementManager.OnPlacementStarted += HandlePlacementStarted;
        PlacementManager.OnPlacementEnded += HandlePlacementEnded;

        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing)
        {
            HandlePlacementStarted();
        }
    }

    private void OnDisable()
    {
        PlacementManager.OnPlacementStarted -= HandlePlacementStarted;
        PlacementManager.OnPlacementEnded -= HandlePlacementEnded;
    }

    private void Start()
    {
        if (shopToggleButton != null)
            shopToggleButton.onClick.AddListener(() => TogglePanel(shopView, hatcheryView));

        if (hatcheryToggleButton != null)
            hatcheryToggleButton.onClick.AddListener(() => TogglePanel(hatcheryView, shopView));
    }

    private void FindReferencesIfNull()
    {
        if (shopView == null) shopView = FindAnyObjectByType<ItemShopView>(FindObjectsInactive.Include);
        if (hatcheryView == null) hatcheryView = FindAnyObjectByType<HatcheryView>(FindObjectsInactive.Include);

        if (storageToggleButton == null)
        {
            var btnObj = GameObject.Find("ToggleStorageButton");
            if (btnObj != null) storageToggleButton = btnObj.GetComponent<Button>();
        }
        if (shopToggleButton == null)
        {
            var btnObj = GameObject.Find("Btn_Shop");
            if (btnObj != null) shopToggleButton = btnObj.GetComponent<Button>();
        }
        if (hatcheryToggleButton == null)
        {
            var btnObj = GameObject.Find("Btn_Hat");
            if (btnObj != null) hatcheryToggleButton = btnObj.GetComponent<Button>();
        }
    }

    private void HandlePlacementStarted()
    {
        SetButtonsVisibility(false);
    }

    private void HandlePlacementEnded()
    {
        SetButtonsVisibility(true);
    }

    private void SetButtonsVisibility(bool isVisible)
    {
        if (shopToggleButton != null) shopToggleButton.gameObject.SetActive(isVisible);
        if (hatcheryToggleButton != null) hatcheryToggleButton.gameObject.SetActive(isVisible);
        if (storageToggleButton != null) storageToggleButton.gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// IToggleableView 인터페이스를 활용하여 특정 패널을 토글하고 다른 패널을 닫습니다.
    /// </summary>
    public void TogglePanel(IToggleableView targetPanel, IToggleableView otherPanel)
    {
        // 정비 상태가 아니면 작동하지 않음
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime)
            return;

        // 배치 중이면 패널을 열지 않음
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing)
            return;

        if (targetPanel == null) return;

        targetPanel.ToggleVisibility();
        otherPanel?.Close();
    }
}
