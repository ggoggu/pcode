using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PenguinPinball.Core;

public class PenguinDeckUI : MonoBehaviour
{
    [Header("UI Text (Status Display)")]
    [SerializeField] private TextMeshProUGUI statusText; // 일반 Text 사용 시 Text로 변경

    [Header("Action Buttons")]
    [SerializeField] private Button selectFirstButton;  // 1. Select First Penguin
    [SerializeField] private Button clearSelectButton;   // 2. Clear Selection

    private void OnEnable()
    {
        // DeckManager 이벤트 구독
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.OnDeckChanged += RefreshUI;
            PenguinDeckManager.Instance.OnSelectedPenguinChanged += HandleSelectedChanged;
        }

        if (selectFirstButton != null) selectFirstButton.onClick.AddListener(OnClickSelectFirst);
        if (clearSelectButton != null) clearSelectButton.onClick.AddListener(OnClickClearSelection);

        RefreshUI();
    }

    private void Start()
    {
        // Manager 초기화 시점 차이 방지
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.OnDeckChanged -= RefreshUI;
            PenguinDeckManager.Instance.OnDeckChanged += RefreshUI;

            PenguinDeckManager.Instance.OnSelectedPenguinChanged -= HandleSelectedChanged;
            PenguinDeckManager.Instance.OnSelectedPenguinChanged += HandleSelectedChanged;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.OnDeckChanged -= RefreshUI;
            PenguinDeckManager.Instance.OnSelectedPenguinChanged -= HandleSelectedChanged;
        }

        if (selectFirstButton != null) selectFirstButton.onClick.RemoveListener(OnClickSelectFirst);
        if (clearSelectButton != null) clearSelectButton.onClick.RemoveListener(OnClickClearSelection);
    }

    public void RefreshUI()
    {
        var deckManager = PenguinDeckManager.Instance;
        if (deckManager == null) return;

        // 텍스트 정보 영어로 업데이트
        if (statusText != null)
        {
            string selectedName = deckManager.SelectedPenguin != null ? deckManager.SelectedPenguin.name : "None";

            statusText.text = $"Field: {deckManager.GetFieldActiveCount()}/{deckManager.MaxFieldCount}\n" +
                              $"Ready: {deckManager.ReadyPenguins.Count}\n" +
                              $"Selected: {selectedName}";
        }
        else
        {
            Debug.LogWarning("[PenguinDeckUI] statusText is not assigned in the Inspector!");
        }

        // 버튼 활성화 상태 업데이트
        if (selectFirstButton != null)
        {
            selectFirstButton.interactable = deckManager.ReadyPenguins.Count > 0 && deckManager.CanLaunchToField();
        }
    }

    private void HandleSelectedChanged(GameObject selectedObj)
    {
        RefreshUI();
    }

    #region Button Click Events
    private void OnClickSelectFirst()
    {
        var deckManager = PenguinDeckManager.Instance;
        if (deckManager == null || deckManager.ReadyPenguins.Count == 0) return;

        var first = deckManager.ReadyPenguins[0];
        if (first != null)
        {
            deckManager.TrySelectPenguin(first);
        }
    }

    private void OnClickClearSelection()
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.ClearSelection();
        }
    }
    #endregion
}