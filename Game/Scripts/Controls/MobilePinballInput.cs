using UnityEngine;
using UnityEngine.InputSystem;

namespace PenguinPinball.Core
{
    /// <summary>
    /// Input Action Asset(FlipperLeft / FlipperRight)을 통해
    /// PC 키보드 및 UI On-Screen Button 입력을 통합 받아 플리퍼를 제어합니다.
    /// </summary>
    public sealed class MobilePinballInput : MonoBehaviour
    {
        [Header("Flipper References")]
        [SerializeField] private FlipperController left;
        [SerializeField] private FlipperController right;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference flipperLeft;
        [SerializeField] private InputActionReference flipperRight;

        private void OnEnable()
        {
            flipperLeft.action.Enable();
            flipperRight.action.Enable();

            flipperLeft.action.started += OnLeftFlipperStarted;
            flipperLeft.action.canceled += OnLeftFlipperCanceled;

            flipperRight.action.started += OnRightFlipperStarted;
            flipperRight.action.canceled += OnRightFlipperCanceled;
        }

        private void OnDisable()
        {
            flipperLeft.action.started -= OnLeftFlipperStarted;
            flipperLeft.action.canceled -= OnLeftFlipperCanceled;

            flipperRight.action.started -= OnRightFlipperStarted;
            flipperRight.action.canceled -= OnRightFlipperCanceled;

            flipperLeft.action.Disable();
            flipperRight.action.Disable();
        }

        private void OnLeftFlipperStarted(InputAction.CallbackContext ctx) => SetFlipperState(left, true);
        private void OnLeftFlipperCanceled(InputAction.CallbackContext ctx) => SetFlipperState(left, false);

        private void OnRightFlipperStarted(InputAction.CallbackContext ctx) => SetFlipperState(right, true); // OnRightFlipperStarted/Canceled 처리
        private void OnRightFlipperCanceled(InputAction.CallbackContext ctx) => SetFlipperState(right, false);

        private void SetFlipperState(FlipperController flipper, bool isPressed)
        {
            if (flipper != null)
            {
                flipper.SetPressed(isPressed);
            }
        }
    }
}