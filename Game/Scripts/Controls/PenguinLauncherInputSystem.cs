using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen; // OnScreenButton 감지를 위해 추가

namespace PenguinPinball.Core
{
    public sealed class PenguinLauncherInputSystem : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PenguinLauncher launcher;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference aimPosition;
        [SerializeField] private InputActionReference launch;

        private bool isAiming;
        private bool canLaunch = false;
        private Vector2 pressScreenPosition;
        private Vector2 currentScreenPosition;
        private PointerEventData pointerEventData;
        private List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            launch.action.started += OnLaunchStarted;
            launch.action.canceled += OnLaunchCanceled;

            aimPosition.action.Enable();
            launch.action.Enable();

            GameManager.OnStateChanged += HandleStateChanged;
            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            launch.action.started -= OnLaunchStarted;
            launch.action.canceled -= OnLaunchCanceled;
            GameManager.OnStateChanged -= HandleStateChanged;

            aimPosition.action.Disable();
            launch.action.Disable();
        }

        private void HandleStateChanged(GameState newState)
        {
            canLaunch = (newState == GameState.WaveInProgress);

            if (!canLaunch && isAiming)
            {
                isAiming = false;
            }
        }

        private void Update()
        {
            if (canLaunch && isAiming)
            {
                // 드래그 중인 실시간 좌표 갱신 (Value 타입으로 설정되어 있어야 함)
                currentScreenPosition = GetCurrentDevicePosition();
                launcher.OnAiming(currentScreenPosition);
            }
        }

        private void OnLaunchStarted(InputAction.CallbackContext context)
        {
            if (!canLaunch) return;

            // 1. 현재 물리적인 기기의 터치/마우스 좌표를 즉시 가져옴
            pressScreenPosition = GetCurrentDevicePosition();

            // 2. 해당 좌표가 플리퍼(On-Screen Button) 등 UI 위인지 검사
            if (IsPointerOverUI(pressScreenPosition)) return;

            currentScreenPosition = pressScreenPosition;

            if (launcher.TryBeginLaunch(pressScreenPosition))
            {
                isAiming = true;
            }
        }
            
        private void OnLaunchCanceled(InputAction.CallbackContext context)
        {
            if (!canLaunch || !isAiming)
            {
                isAiming = false;
                return;
            }

            isAiming = false;
            launcher.ReleaseLaunch(currentScreenPosition);
        }

        // InputAction의 딜레이를 무시하고 하드웨어 장치에서 좌표를 즉시 추출하는 헬퍼 메서드
        private Vector2 GetCurrentDevicePosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            // 폴백: Input Action 값 사용 (이전 입력 캐시)
            return aimPosition.action.ReadValue<Vector2>();
        }

        // 매개변수를 직접 좌표(Vector2)로 받도록 수정
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            if (pointerEventData == null)
            {
                pointerEventData = new PointerEventData(EventSystem.current);
            }
            pointerEventData.position = screenPosition;

            raycastResults.Clear();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hitObj = raycastResults[i].gameObject;

                // 단순 배경(Raycast Target 켜진 패널)은 무시하고, 
                // 실제 Button 컴포넌트나 OnScreenButton 컴포넌트가 있는 경우에만 조준 차단
                if (hitObj.GetComponent<Button>() != null ||
                    hitObj.GetComponent<OnScreenButton>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}