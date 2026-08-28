using System.Collections;
using UnityEngine;
using PenguinPinball.Core;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class SlingshotRubber : MonoBehaviour
{
    [Header("발사 및 탄성 옵션")]
    [SerializeField] private float launchForce = 25.0f;       // 범퍼형 고정 임펄스 속도

    [Header("시각적 고무줄 연출 옵션")]
    [SerializeField] private float shakeDistance = 0.6f;      // 순간적으로 뒤로 늘어나는 시각적 거리
    [SerializeField] private float shakeDuration = 0.25f;     // 고무줄 탄성 진동 유지 시간

    [Header("콜라이더 범위 설정")]
    [SerializeField] private float colliderThickness = 2.0f;
    [SerializeField] private float colliderHeight = 3.0f;

    [Header("평면 설정")]
    [SerializeField] private Transform movementPlane;

    private LineRenderer lineRenderer;
    private BoxCollider boxCollider;
    private Rigidbody rb;

    private Transform bumperLeft;
    private Transform bumperRight;

    private bool isWaveActive = false;
    private bool isShaking = false;

    private void Awake()
    {
        EnsureComponents();
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
    }

    private void HandleStateChanged(GameState newState)
    {
        isWaveActive = (newState == GameState.WaveInProgress);
    }

    private void EnsureComponents()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null) boxCollider.isTrigger = true;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public void Setup(Transform left, Transform right)
    {
        bumperLeft = left;
        bumperRight = right;
        UpdateRubberAndCollider();
    }

    private void Update()
    {
        // 고무줄 진동 연출 중이 아닐 때만 평소 위치 동기화
        if (!isShaking && bumperLeft != null && bumperRight != null)
        {
            UpdateRubberAndCollider();
        }
    }

    public void UpdateRubberAndCollider()
    {
        if (bumperLeft == null || bumperRight == null) return;

        EnsureComponents();
        if (lineRenderer == null || boxCollider == null) return;

        lineRenderer.positionCount = 3;
        Vector3 centerPos = (bumperLeft.position + bumperRight.position) * 0.5f;

        lineRenderer.SetPosition(0, bumperLeft.position);
        lineRenderer.SetPosition(1, centerPos);
        lineRenderer.SetPosition(2, bumperRight.position);

        if (transform.parent != null)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(centerPos);
        }
        else
        {
            transform.position = centerPos;
        }

        Vector3 dir = (bumperRight.position - bumperLeft.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetWorldRotation = Quaternion.LookRotation(dir);
            transform.localRotation = transform.parent != null 
                ? Quaternion.Inverse(transform.parent.rotation) * targetWorldRotation 
                : targetWorldRotation;
        }

        float distance = Vector3.Distance(bumperLeft.position, bumperRight.position);
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(colliderThickness, colliderHeight, distance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isWaveActive) return;

        // 펭귄 충돌 시 즉시 범퍼처럼 반발
        if (other.TryGetComponent<PenguinController>(out var penguin))
        {
            Vector3 boardNormal = movementPlane != null ? movementPlane.up : Vector3.up;
            Vector3 forwardDir = transform.parent != null ? transform.parent.forward : transform.forward;
            Vector3 launchDirection = Vector3.ProjectOnPlane(forwardDir, boardNormal).normalized;

            // 1. 딜레이 없이 즉시 고정 임펄스 속도 부여
#if UNITY_6000_0_OR_NEWER
            penguin.Body.linearVelocity = launchDirection * launchForce;
#else
            penguin.Body.velocity = launchDirection * launchForce;
#endif

            // 2. 콤보 및 부적/능력 발동
            penguin.AddCombo();
            penguin.Ability.OnEnvironmentCollision(null);

            // 3. 고무줄 시각 진동 연출 재생 (펭귄 물리에 영향 없음)
            StopAllCoroutines();
            StartCoroutine(RubberShakeVisualRoutine());
        }
    }

    // ★ 시각 전용 고무줄 감쇄 진동 연출
    private IEnumerator RubberShakeVisualRoutine()
    {
        isShaking = true;

        Vector3 forwardDir = transform.parent != null ? transform.parent.forward : transform.forward;
        Vector3 originalCenterPos = (bumperLeft.position + bumperRight.position) * 0.5f;
        Vector3 pulledCenterPos = originalCenterPos - (forwardDir * shakeDistance);

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;

            // 감쇄 파형(Damped Wave)으로 탄성 있게 흔들리는 효과 계산
            float offsetCoeff = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
            Vector3 currentCenter = Vector3.Lerp(originalCenterPos, pulledCenterPos, offsetCoeff);

            lineRenderer.SetPosition(0, bumperLeft.position);
            lineRenderer.SetPosition(1, currentCenter);
            lineRenderer.SetPosition(2, bumperRight.position);

            yield return null;
        }

        // 연출 완료 후 원래 위치로 원복
        lineRenderer.SetPosition(0, bumperLeft.position);
        lineRenderer.SetPosition(1, originalCenterPos);
        lineRenderer.SetPosition(2, bumperRight.position);

        isShaking = false;
    }
}