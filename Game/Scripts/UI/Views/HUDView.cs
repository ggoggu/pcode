using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 실제 유니티 UI 컴포넌트를 제어하는 View 스크립트
/// </summary>
public class HUDView : MonoBehaviour
{
    [Header("UI Text References (TextMeshPro)")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text phaseText;

    [Header("UI Interaction")]
    [SerializeField] private Button phaseToggleButton;

    [Header("Panel / CanvasGroup (Optional)")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private GameObject hudRoot;

    private HUDViewModel viewModel;

    private void Awake()
    {
        viewModel = new HUDViewModel();
        FindReferencesIfNull();
    }

    private void FindReferencesIfNull()
    {
        if (hudCanvasGroup == null)
            hudCanvasGroup = GetComponent<CanvasGroup>();
        if (hudRoot == null && hudCanvasGroup != null)
            hudRoot = hudCanvasGroup.gameObject;
    }

    private void OnEnable()
    {
        if (viewModel == null)
            viewModel = new HUDViewModel();

        // ViewModel 이벤트 바인딩
        viewModel.OnScoreTextChanged += UpdateScoreUI;
        viewModel.OnGoldTextChanged  += UpdateGoldUI;
        viewModel.OnLifeTextChanged  += UpdateLifeUI;
        viewModel.OnPhaseTextChanged += UpdatePhaseUI;
        viewModel.OnPhaseButtonInteractableChanged += UpdatePhaseButtonInteractable;
        viewModel.OnPhaseButtonTextChanged += UpdatePhaseButtonText;

        // 버튼 클릭 이벤트 바인딩
        if (phaseToggleButton != null)
        {
            phaseToggleButton.onClick.RemoveListener(OnPhaseButtonClicked);
            phaseToggleButton.onClick.AddListener(OnPhaseButtonClicked);
        }

        // 오브젝트 배치 이벤트 바인딩 (배치 중에도 HUD 상시 표시 유지)
        PlacementManager.OnPlacementStarted += HandlePlacementStarted;
        PlacementManager.OnPlacementEnded   += HandlePlacementEnded;

        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing)
        {
            EnsureHUDVisible();
        }
    }

    private void Start()
    {
        // 씬 시작 후 GameManager의 상태값을 받아와 UI 초기화
        viewModel?.RefreshInitialValues();
        EnsureHUDVisible();
    }

    private void OnDisable()
    {
        PlacementManager.OnPlacementStarted -= HandlePlacementStarted;
        PlacementManager.OnPlacementEnded   -= HandlePlacementEnded;

        if (viewModel != null)
        {
            viewModel.OnScoreTextChanged -= UpdateScoreUI;
            viewModel.OnGoldTextChanged  -= UpdateGoldUI;
            viewModel.OnLifeTextChanged  -= UpdateLifeUI;
            viewModel.OnPhaseTextChanged -= UpdatePhaseUI;
            viewModel.OnPhaseButtonInteractableChanged -= UpdatePhaseButtonInteractable;
            viewModel.OnPhaseButtonTextChanged -= UpdatePhaseButtonText;
        }

        if (phaseToggleButton != null)
        {
            phaseToggleButton.onClick.RemoveListener(OnPhaseButtonClicked);
        }
    }

    private void OnDestroy()
    {
        viewModel?.Dispose();
        viewModel = null;
    }

    private void HandlePlacementStarted()
    {
        EnsureHUDVisible();
    }

    private void HandlePlacementEnded()
    {
        EnsureHUDVisible();
    }

    /// <summary>
    /// 오브젝트 배치 시 및 일반 게임 상태에서 골드 등을 표시하는 HUD가 가려지거나 비활성화되지 않고
    /// 항상 화면 최상위에 온전히 표시되도록 보장합니다.
    /// </summary>
    public void EnsureHUDVisible()
    {
        if (hudRoot != null && !hudRoot.activeSelf)
        {
            hudRoot.SetActive(true);
        }
        else if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 1f;
            hudCanvasGroup.interactable = true;
            hudCanvasGroup.blocksRaycasts = true;
        }

        // 개별 HUD 텍스트 요소 활성화 및 표시 보장
        EnsureElementVisible(goldText);
        EnsureElementVisible(scoreText);
        EnsureElementVisible(lifeText);
        EnsureElementVisible(phaseText);

        // 오버레이 패널(PlacementControlPanel 등)보다 위에 렌더링되도록 최상단 계층 보장
        if (hudRoot != null && hudRoot.transform.parent != null)
        {
            hudRoot.SetActive(true);
            hudRoot.transform.SetAsLastSibling();
        }
        else if (transform.parent != null && (transform.parent.name == "Canvas" || transform.parent.GetComponent<Canvas>() != null))
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
        else if (goldText != null && goldText.transform.parent != null)
        {
            var parent = goldText.transform.parent;
            // Case 1: goldText의 부모가 Canvas의 직속 자식인 컨테이너(Area 등)인 경우
            if (parent.parent != null && (parent.parent.GetComponent<Canvas>() != null || parent.parent.name == "Canvas"))
            {
                parent.gameObject.SetActive(true);
                parent.SetAsLastSibling();
            }
            // Case 2: goldText의 부모 자체가 Canvas인 경우 (PlacementScene 등)
            else if (parent.GetComponent<Canvas>() != null || parent.name == "Canvas")
            {
                goldText.transform.SetAsLastSibling();
                if (scoreText != null) scoreText.transform.SetAsLastSibling();
                if (lifeText != null) lifeText.transform.SetAsLastSibling();
                if (phaseText != null) phaseText.transform.SetAsLastSibling();
            }
            else if (parent.name == "Area" || parent.name.Contains("HUD"))
            {
                parent.gameObject.SetActive(true);
                parent.SetAsLastSibling();
            }
        }
    }

    private void EnsureElementVisible(TMP_Text element)
    {
        if (element == null) return;

        if (!element.gameObject.activeSelf)
        {
            element.gameObject.SetActive(true);
        }

        if (element.transform.parent != null && !element.transform.parent.gameObject.activeSelf)
        {
            element.transform.parent.gameObject.SetActive(true);
        }

        if (!element.enabled)
        {
            element.enabled = true;
        }

        if (element.canvasRenderer != null)
        {
            element.canvasRenderer.SetAlpha(1f);
        }

        if (element.color.a < 0.1f)
        {
            Color c = element.color;
            c.a = 1f;
            element.color = c;
        }

        var cg = element.GetComponentInParent<CanvasGroup>();
        if (cg != null && cg.alpha < 0.1f)
        {
            cg.alpha = 1f;
        }
    }

    // ==========================================
    // UI 콜백 메서드
    // ==========================================

    private void UpdateScoreUI(string text)
    {
        if (scoreText != null) scoreText.text = text;
    }

    private void UpdateGoldUI(string text)
    {
        if (goldText != null) goldText.text = text;
    }

    private void UpdateLifeUI(string text)
    {
        if (lifeText != null) lifeText.text = text;
    }

    private void UpdatePhaseUI(string text)
    {
        if (phaseText != null) phaseText.text = text;
    }

    private void UpdatePhaseButtonInteractable(bool interactable)
    {
        if (phaseToggleButton != null) phaseToggleButton.interactable = interactable;
    }

    private void UpdatePhaseButtonText(string text)
    {
        if (phaseToggleButton != null)
        {
            var btnText = phaseToggleButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = text;
        }
    }

    private void OnPhaseButtonClicked()
    {
        viewModel?.TogglePhase();
    }
}
