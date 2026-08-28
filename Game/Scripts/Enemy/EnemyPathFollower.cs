using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3.0f; // 이동 속도
    [SerializeField] private Transform[] waypoints;  // 거쳐갈 경로 지점 배열

    private int currentWaypointIndex = 0; // 현재 이동 중인 목표 지점의 인덱스 번호

    private void Update()
    {
        // 이동할 웨이포인트가 없거나, 모든 웨이포인트를 다 지난 경우 작동 중지
        if (waypoints == null || waypoints.Length == 0 || currentWaypointIndex >= waypoints.Length) 
            return;

        Transform targetPoint = waypoints[currentWaypointIndex];

        // 현재 타겟 지점 방향으로 이동
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPoint.position, 
            moveSpeed * Time.deltaTime
        );

        // 해당 웨이포인트 도착 감지 (거리 0.1미만)
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            currentWaypointIndex++; // 다음 웨이포인트 번호로 넘어감

            // 마지막 웨이포인트(베이스)까지 다 도착했다면
            if (currentWaypointIndex >= waypoints.Length)
            {
                OnReachDestination();
            }
        }
    }

    // 외부(스폰 매니저)에서 웨이포인트 목록을 주입해 줄 수 있는 함수
    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
    }

    private void OnReachDestination()
    {
        Debug.Log("적이 최종 베이스에 도달했습니다!");
        
        // 김민재 님의 GameFlowManager에 생명력 차감 신호를 날리는 지점입니다.
        
        Destroy(gameObject);
    }
}