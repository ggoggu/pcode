using UnityEngine;
using UnityEngine.UI;

public class UIToggleManager : MonoBehaviour
{
    [Header("패널 Views (IToggleableView)")]
    [SerializeField] private ItemShopView shopView;
    [SerializeField] private HatcheryView hatcheryView;

    [Header("토글 버튼")]
    [SerializeField] private Button shopToggleButton;
    [SerializeField] private Button hatcheryToggleButton;

    private void Start()
    {
        if (shopToggleButton != null)
            shopToggleButton.onClick.AddListener(() => TogglePanel(shopView, hatcheryView));

        if (hatcheryToggleButton != null)
            hatcheryToggleButton.onClick.AddListener(() => TogglePanel(hatcheryView, shopView));
    }

    /// <summary>
    /// IToggleableView 인터페이스를 활용하여 특정 패널을 토글하고 다른 패널을 닫습니다.
    /// </summary>
    public void TogglePanel(IToggleableView targetPanel, IToggleableView otherPanel)
    {
        // 정비 상태가 아니면 작동하지 않음
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MaintenanceTime)
            return;

        if (targetPanel == null) return;

        targetPanel.ToggleVisibility();
        otherPanel?.Close();
    }
}
