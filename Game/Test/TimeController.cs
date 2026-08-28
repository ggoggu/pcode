using UnityEngine;
using TMPro; // TextMeshPro를 사용하는 경우 필요

public class TimeController : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI timeScaleText; // 현재 배속을 표시할 텍스트 UI

    private float initialFixedDeltaTime;

    private void Awake()
    {
        initialFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void OnTimeScaleChanged(float value)
    {
        // 1. 전체 게임 속도 변경
        Time.timeScale = value;

        // 2. 물리 연산 주기 동기화 (물리 튐 및 충돌 감지 누락 방지)
        Time.fixedDeltaTime = initialFixedDeltaTime * value;

        // 3. UI 업데이트
        if (timeScaleText != null)
        {
            timeScaleText.text = $"Time Scale: {value:F1}x";
        }
    }
}