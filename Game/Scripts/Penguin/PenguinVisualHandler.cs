using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// visualRoot 또는 Visual 객체에 부착되어 물리 구르기 회전을 상쇄하고 
    /// 진행 방향 주시 및 엎드린 자세(Posture)를 제어하는 시각 전용 컴포넌트입니다.
    /// </summary>
    public sealed class PenguinVisualHandler : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("진행 방향으로 몸을 돌리는 보간 속도")]
        [SerializeField] private float turnSpeed = 12f;

        [Tooltip("방향을 전환하기 위한 최소 물리 속도 임계값")]
        [SerializeField] private float minSpeedThreshold = 0.1f;

        private Rigidbody targetRigidbody;
        private Quaternion targetWorldRotation;

        /// <summary>
        /// PenguinController에서 생성 직후 호출하여 참조 및 초기 자세를 설정합니다.
        /// </summary>
        public void Initialize(Rigidbody rigidbody, GameObject visualInstance)
        {
            targetRigidbody = rigidbody;

            // 초기 월드 회전값 보존
            targetWorldRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (targetRigidbody == null) return;

            // 2. 부모 Rigidbody의 이동 속도 벡터 추출 (Unity 6: linearVelocity)
            Vector3 velocity = targetRigidbody.linearVelocity;

            // 3. XZ 평면화 (높이 Y축 속도 무시)
            velocity.y = 0f;

            // 4. 속도가 임계값 이상일 때만 목표 회전각 계산
            if (velocity.sqrMagnitude > minSpeedThreshold * minSpeedThreshold)
            {
                targetWorldRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            // 5. 부모의 물리 구르기 회전을 상쇄하며, visualRoot의 월드 회전을 진행 방향으로 부드럽게 보간
            transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRotation, Time.deltaTime * turnSpeed);
        }
    }
}