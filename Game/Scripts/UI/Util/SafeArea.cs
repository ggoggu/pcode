using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;
    
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }
    
        void Update()
        {
            // 화면 회전이나 해상도 변경 실시간 감지
            if (lastSafeArea != Screen.safeArea || 
                lastScreenSize.x != Screen.width || 
                lastScreenSize.y != Screen.height || 
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }
    
        void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
    
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;
    
            // 화면 픽셀 좌표를 0~1 정규화 앵커 좌표로 변환
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
    
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
    
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
}
