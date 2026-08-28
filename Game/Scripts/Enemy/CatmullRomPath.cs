using System.Collections.Generic;
using UnityEngine;

public class CatmullRomPath : MonoBehaviour
{
    [System.Serializable]
    public class Waypoint
    {
        public Transform point;

        [Tooltip("0 = 날카로운 곡선 / 1 = 부드러운 곡선 (기본 0.5)")]
        [Range(0f, 1f)]
        public float tension = 0.5f;

        [Tooltip("해당 구간 진입 시 Y축 오프셋 속도 배율")]
        [Range(-2f, 2f)]
        public float verticalBias = 0f;
    }

    [Header("경로 설정")]
    public List<Waypoint> waypoints = new List<Waypoint>();

    [Header("경로 콜라이더 자동 생성 설정")]
    [Tooltip("게임 시작 시 자동으로 경로 콜라이더를 생성할지 여부")]
    [SerializeField] private bool autoGenerateColliders = true;
    [Tooltip("적 이동 경로의 좌우 폭 (PlacementManager 차단 범위)")]
    [SerializeField] private float pathWidth = 0.8f;
    [Tooltip("경로 콜라이더 세그먼트 정밀도 (값이 높을수록 곡선에 정교하게 밀착)")]
    [SerializeField] private int colliderStepsPerSegment = 10;

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private int gizmoSteps = 20;
    [SerializeField] private Color pathColor = Color.cyan;

    public bool IsValid => waypoints != null && waypoints.Count >= 2;
    public int SegmentCount => Mathf.Max(0, waypoints.Count - 1);

    private void Start()
    {
        if (autoGenerateColliders)
        {
            GeneratePathColliders();
        }
    }

    /// <summary>
    /// 곡선 경로를 따라 Trigger BoxCollider들을 자동 생성합니다.
    /// 에디터에서 컴포넌트 헤더 우클릭 -> [Generate Path Colliders] 클릭으로도 실행 가능합니다.
    /// </summary>
    [ContextMenu("Generate Path Colliders")]
    public void GeneratePathColliders()
    {
        if (!IsValid) return;

        // 기존에 생성되어 있던 PathColliders 그룹 제거
        Transform oldGroup = transform.Find("PathColliders");
        if (oldGroup != null)
        {
            if (Application.isPlaying) Destroy(oldGroup.gameObject);
            else DestroyImmediate(oldGroup.gameObject);
        }

        // 콜라이더들을 담을 부모 빈 오브젝트 생성
        GameObject colliderGroup = new GameObject("PathColliders");
        colliderGroup.transform.SetParent(transform);
        colliderGroup.transform.localPosition = Vector3.zero;
        colliderGroup.transform.localRotation = Quaternion.identity;
        colliderGroup.layer = gameObject.layer; // CatmullRomPath의 Layer(EnemyPath) 상속

        int stepCount = Mathf.Max(1, colliderStepsPerSegment);

        for (int seg = 0; seg < SegmentCount; seg++)
        {
            for (int i = 0; i < stepCount; i++)
            {
                float t0 = (float)i / stepCount;
                float t1 = (float)(i + 1) / stepCount;

                Vector3 startPos = Evaluate(seg, t0);
                Vector3 endPos = Evaluate(seg, t1);
                Vector3 midPos = (startPos + endPos) * 0.5f;

                float distance = Vector3.Distance(startPos, endPos);
                if (distance <= 0.001f) continue;

                // 세그먼트 조각 오브젝트 생성
                GameObject colObj = new GameObject($"PathCol_{seg}_{i}");
                colObj.transform.SetParent(colliderGroup.transform);
                colObj.transform.position = midPos;
                
                // 진행 방향 바라보기
                Vector3 direction = (endPos - startPos).normalized;
                if (direction != Vector3.zero)
                {
                    colObj.transform.rotation = Quaternion.LookRotation(direction);
                }
                
                colObj.layer = gameObject.layer; // EnemyPath Layer 복사

                // BoxCollider 추가 및 크기 조정
                BoxCollider box = colObj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(pathWidth, 1.0f, distance); // X: 경로폭, Y: 높이, Z: 세그먼트 길이
            }
        }
    }

    public Vector3 GetPosition(int index)
    {
        if (!IsValid) return transform.position;
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].point != null ? waypoints[index].point.position : transform.position;
    }

    public float GetTension(int index)
    {
        if (!IsValid) return 0.5f;
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].tension;
    }

    public float GetVerticalBias(int index)
    {
        if (!IsValid) return 0f;
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].verticalBias;
    }

    public Vector3 Evaluate(int seg, float t)
    {
        if (!IsValid) return transform.position;

        // 양 끝점 안전 보정 (p0, p3 인덱스 범위 클램프)
        Vector3 p0 = GetPosition(seg - 1);
        Vector3 p1 = GetPosition(seg);
        Vector3 p2 = GetPosition(seg + 1);
        Vector3 p3 = GetPosition(seg + 2);

        float tension = (GetTension(seg) + GetTension(seg + 1)) * 0.5f;
        float vBias = GetVerticalBias(seg);
        Vector3 biasOffset = new Vector3(0f, vBias * t, 0f);

        return CatmullRom(p0, p1, p2, p3, t, tension) + biasOffset;
    }

    public Vector3 EvaluateAtProgress(float pathProgress)
    {
        if (!IsValid) return transform.position;

        pathProgress = Mathf.Clamp(pathProgress, 0f, SegmentCount);
        int seg = Mathf.Min(Mathf.FloorToInt(pathProgress), SegmentCount - 1);
        float t = pathProgress - seg;

        return Evaluate(seg, t);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
    {
        float alpha = tension;
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 m1 = alpha * (p2 - p0);
        Vector3 m2 = alpha * (p3 - p1);

        return (2f * t3 - 3f * t2 + 1f) * p1
             + (t3 - 2f * t2 + t) * m1
             + (-2f * t3 + 3f * t2) * p2
             + (t3 - t2) * m2;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || !IsValid) return;

        Gizmos.color = pathColor;
        for (int seg = 0; seg < SegmentCount; seg++)
        {
            for (int i = 0; i < gizmoSteps; i++)
            {
                float t0 = (float)i / gizmoSteps;
                float t1 = (float)(i + 1) / gizmoSteps;

                Vector3 from = Evaluate(seg, t0);
                Vector3 to = Evaluate(seg, t1);
                Gizmos.DrawLine(from, to);
            }
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].point == null) continue;
            Gizmos.color = (i == 0 || i == waypoints.Count - 1) ? Color.red : Color.yellow;
            Gizmos.DrawSphere(waypoints[i].point.position, 0.15f);
        }
    }
}