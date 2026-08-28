using UnityEngine;
using TMPro; // TextMeshPro 사용 시 필요 (Legacy Text 사용 시 UnityEngine.UI 참조)

public class GoldUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI goldText; // 골드를 표시할 TMP 텍스트 컴포넌트
    [SerializeField] private string formatString = "{0} G"; // 표시 포맷 (예: "100 G")

    private void Awake()
    {
        // Inspector에서 직접 연결하지 않았다면 자신에게 붙은 TMP를 찾음
        if (goldText == null)
        {
            goldText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        // GameManager의 골드 변경 이벤트 구독
        GameManager.OnGoldChanged += UpdateGoldUI;
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 이벤트 구독 해제 (메모리 누수 방지)
        GameManager.OnGoldChanged -= UpdateGoldUI;
    }

    private void Start()
    {
        // 게임 시작 시 초기 골드 값으로 UI 세팅
        if (GameManager.Instance != null)
        {
            UpdateGoldUI(GameManager.Instance.CurrentGold);
        }
    }

    /// <summary>
    /// 골드 값이 변경될 때 호출되어 텍스트를 갱신합니다.
    /// </summary>
    private void UpdateGoldUI(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = string.Format(formatString, currentGold);
        }
    }
}