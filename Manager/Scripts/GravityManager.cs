using UnityEngine;

public enum ZGravityDirection
{
    Negative, // -Z 방향으로 굴러 내려감 (기본 핀볼 방식: 아래쪽)
    Positive  // +Z 방향으로 굴러 올라감 (반대 중력 기믹: 위쪽)
}

public class GravityManager : MonoBehaviour
{
    [Header("핀볼 경사각 설정")]
    [Tooltip("핀볼 경사면의 기울기 각도 (도 단위)")]
    [Range(0f, 90f)]
    [SerializeField] private float tiltAngle = 7f;

    [Header("중력 크기")]
    [Tooltip("중력 가속도 기본값 (9.81)")]
    [SerializeField] private float gravityMagnitude = 9.81f;

    [Header("Z축 중력 방향")]
    [Tooltip("Negative: -Z(아래) 방향으로 끌어당김\nPositive: +Z(위) 방향으로 끌어당김")]
    [SerializeField] private ZGravityDirection zDirection = ZGravityDirection.Negative;

    [Header("핀볼 판 배치 방향")]
    [Tooltip("true: 맵이 바닥(X-Z 평면, Top-Down)에 있는 경우\nfalse: 맵이 정면(X-Y 평면)에 서 있는 경우")]
    [SerializeField] private bool isTopDownView = true;

    private void Awake()
    {
        ApplyPinballGravity();
    }

    public void ApplyPinballGravity()
    {
        Vector3 newGravity = Vector3.zero;

        if (isTopDownView)
        {
            // 맵이 바닥(X-Z 평면)에 배치된 경우
            // 공이 Z축 마이너스 방향(아래)으로 굴러 내려가도록 Y축 중력을 Z축으로 분산
            float gravityY = -gravityMagnitude * Mathf.Cos(tiltAngle * Mathf.Deg2Rad);
            float gravityZMagnitude = gravityMagnitude * Mathf.Sin(tiltAngle * Mathf.Deg2Rad);

            // 선택한 방향 옵션(Negative / Positive)에 따라 부호 처리
            float zSign = (zDirection == ZGravityDirection.Negative) ? -1f : 1f;
            float gravityZ = gravityZMagnitude * zSign;

            newGravity = new Vector3(0f, gravityY, gravityZ);
        }
        else
        {
            // 맵이 정면(X-Y 평면)에 배치된 경우
            float gravityY = -gravityMagnitude * Mathf.Sin(tiltAngle * Mathf.Deg2Rad);
            newGravity = new Vector3(0f, gravityY, 0f);
        }

        Physics.gravity = newGravity;
        Debug.Log($"[GravityManager] 중력 설정 완료: {Physics.gravity}");
    }

    public void SetZDirection(ZGravityDirection newDirection)
    {
        zDirection = newDirection;
        ApplyPinballGravity();
    }

    public void ToggleZDirection()
    {
        zDirection = (zDirection == ZGravityDirection.Negative)
            ? ZGravityDirection.Positive
            : ZGravityDirection.Negative;

        ApplyPinballGravity();
    }

    // 에디터 실행 중 인스펙터에서 값을 조절할 때 실시간 반영
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyPinballGravity();
        }
    }
}