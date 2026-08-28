using UnityEngine;
using UnityEngine.UI;

namespace Game.ObjectSystem.UI
{
    /// <summary>
    /// 오브젝트 관리 상태 3종 조작 버튼 UI (설치 확정 / 회전 / 회수)
    /// </summary>
    public class PlacementControlUI : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button _confirmButton;  // 설치 확정
        [SerializeField] private Button _rotateButton;   // 회전
        [SerializeField] private Button _recallButton;   // 회수

        private void Start()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(() => 
                {
                    if (PlacementManager.Instance != null)
                        PlacementManager.Instance.ConfirmPlacement();
                });
            }

            if (_rotateButton != null)
            {
                _rotateButton.onClick.AddListener(() => 
                {
                    if (PlacementManager.Instance != null)
                        PlacementManager.Instance.RotateCurrentPreview(45f);
                });
            }

            if (_recallButton != null)
            {
                _recallButton.onClick.AddListener(() => 
                {
                    if (PlacementManager.Instance != null)
                        PlacementManager.Instance.RecallPlacement();
                });
            }

            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetConfirmButtonInteractable(bool isInteractable)
        {
            if (_confirmButton != null)
                _confirmButton.interactable = isInteractable;
        }
    }
}