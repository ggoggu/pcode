using System.Collections.Generic;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class PenguinLauncher : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private PinballPhysicsConfig physicsConfig;

        [Header("Launch Settings")]
        [SerializeField] private Transform launchPoint;
        [SerializeField] private Camera worldCamera;


        [Header("Aim Trajectory Settings")]
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private Vector3 localAimDirection = new Vector3(0f, 0f, -1f);

        private PenguinController selected;
        private Vector3 start;
        private bool aiming;
        private bool canLaunch = false;
        private float nextAllowedLaunchTime = 0f; // 공통 쿨타임 타이머

        public Transform LaunchPoint => launchPoint;
        public bool HasSelectedPenguin => selected != null;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
            if (aimLine) aimLine.enabled = false;
        }

        private void Start()
        {
            if (PenguinDeckManager.Instance != null)
                PenguinDeckManager.Instance.OnSelectedPenguinChanged += HandlePenguinSelected;
        }


        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;

            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            CancelLaunch();
        }

        private void OnDestroy()
        {
            if (PenguinDeckManager.Instance != null)
            {
                PenguinDeckManager.Instance.OnSelectedPenguinChanged -= HandlePenguinSelected;
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            canLaunch = (newState == GameState.WaveInProgress);

            if (!canLaunch && aiming)
            {
                CancelLaunch();
            }
        }

        private void HandlePenguinSelected(GameObject penguinObj)
        {
            selected = penguinObj != null ? penguinObj.GetComponent<PenguinController>() : null;

            if (selected == null && aiming)
            {
                CancelLaunch();
            }
        }

        public bool TryBeginLaunch(Vector2 screenPos)
        {
            if (!canLaunch || selected == null) return false;

            if (Time.time < nextAllowedLaunchTime) return false;

            start = ScreenToBoard(screenPos);
            aiming = true;

            if (aimLine)
            {
                aimLine.enabled = true;
                aimLine.positionCount = 2;
                UpdateAimLine(Vector3.zero);
            }

            return true;
        }

        public void OnAiming(Vector2 currentScreenPos)
        {
            if (!aiming || !selected) return;

            Vector3 currentWorldPos = ScreenToBoard(currentScreenPos);
            Vector3 rawDrag = Vector3.ProjectOnPlane(start - currentWorldPos, launchPoint.up);

            Vector3 clampedDrag = GetClampedDragVector(rawDrag);
            UpdateAimLine(clampedDrag);
        }

        public void ReleaseLaunch(Vector2 releaseScreenPos)
        {
            if (!aiming || !selected) return;

            Vector3 rawDrag = Vector3.ProjectOnPlane(start - ScreenToBoard(releaseScreenPos), launchPoint.up);

            if (rawDrag.magnitude < physicsConfig.launcherMinDragDistance)
            {
                CancelAim();
                return;
            }

            aiming = false;
            if (aimLine) aimLine.enabled = false;

            Vector3 clampedDrag = GetClampedDragVector(rawDrag);

            float dragDistance = clampedDrag.magnitude;
            float dragRatio = Mathf.Clamp01(dragDistance / physicsConfig.launcherMaxDragDistance);

            float calculatedSpeed = dragRatio * selected.CurrentMaxSpeed;
            float finalSpeed = Mathf.Max(calculatedSpeed, physicsConfig.minLaunchSpeed);

            PenguinController launchedPenguin = selected;
            launchedPenguin.PrepareAndLaunch(launchPoint.position, clampedDrag.normalized, finalSpeed);

            nextAllowedLaunchTime = Time.time + physicsConfig.launchCooldown;

            GameEvents.RaiseOnPenguinLaunched(launchedPenguin);
            selected = null;
        }

        private void CancelLaunch()
        {
            aiming = false;
            selected = null;
            if (aimLine) aimLine.enabled = false;
        }

        private void CancelAim()
        {
            aiming = false;
            if (aimLine) aimLine.enabled = false;
        }

        private void UpdateAimLine(Vector3 drag)
        {
            if (!aiming || !aimLine) return;

            Vector3 startPos = launchPoint.position;
            Vector3 limitedDrag = Vector3.ClampMagnitude(drag, physicsConfig.launcherMaxDragDistance);
            Vector3 endPos = startPos + limitedDrag;

            aimLine.SetPosition(0, startPos);
            aimLine.SetPosition(1, endPos);
        }

        private Vector3 GetClampedDragVector(Vector3 rawDrag)
        {
            if (rawDrag.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forwardDir = launchPoint.TransformDirection(localAimDirection.normalized);
            float angle = Vector3.SignedAngle(forwardDir, rawDrag, launchPoint.up);
            float maxAngle = physicsConfig.maxAimAngle;

            // 허용 각도 범위를 벗어나면 가장 가까운 경계 각도로 보정
            float clampedAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);
            Vector3 clampedDir = Quaternion.AngleAxis(clampedAngle, launchPoint.up) * forwardDir;

            return clampedDir * rawDrag.magnitude;
        }

        private Vector3 ScreenToBoard(Vector2 screen)
        {
            var ray = worldCamera.ScreenPointToRay(screen);
            var plane = new Plane(launchPoint.up, launchPoint.position);
            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : launchPoint.position;
        }

        #region Test Launch API
        public bool LaunchCustom(float angle, float speed)
        {
            if (selected == null)
            {
                Debug.LogWarning("[PenguinLauncher] 선택된 펭귄이 없어 발사할 수 없습니다.");
                return false;
            }

            if (Time.time < nextAllowedLaunchTime)
            {
                Debug.LogWarning("[PenguinLauncher] 발사 쿨타임 중입니다.");
                return false;
            }

            CancelAim();

            Vector3 forwardDir = launchPoint.TransformDirection(localAimDirection.normalized);
            Vector3 launchDirection = Quaternion.AngleAxis(angle, launchPoint.up) * forwardDir;

            ExecuteLaunchInternal(launchDirection.normalized, speed);
            return true;
        }

        private void ExecuteLaunchInternal(Vector3 direction, float speed)
        {
            PenguinController launchedPenguin = selected;
            launchedPenguin.PrepareAndLaunch(launchPoint.position, direction, speed);

            nextAllowedLaunchTime = Time.time + physicsConfig.launchCooldown;

            GameEvents.RaiseOnPenguinLaunched(launchedPenguin);
            selected = null;
        }
        #endregion
    }
}