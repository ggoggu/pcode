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

    private HUDViewModel viewModel;

    private void Awake()
    {
        viewModel = new HUDViewModel();
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

        // 버튼 클릭 이벤트 바인딩
        if (phaseToggleButton != null)
        {
            phaseToggleButton.onClick.RemoveListener(OnPhaseButtonClicked);
            phaseToggleButton.onClick.AddListener(OnPhaseButtonClicked);
        }
    }

    private void Start()
    {
        // 씬 시작 후 GameManager의 상태값을 받아와 UI 초기화
        viewModel?.RefreshInitialValues();
    }

    private void OnDisable()
    {
        if (viewModel != null)
        {
            viewModel.OnScoreTextChanged -= UpdateScoreUI;
            viewModel.OnGoldTextChanged  -= UpdateGoldUI;
            viewModel.OnLifeTextChanged  -= UpdateLifeUI;
            viewModel.OnPhaseTextChanged -= UpdatePhaseUI;
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

    private void OnPhaseButtonClicked()
    {
        viewModel?.TogglePhase();
    }
}