using UnityEngine;

public class BumperPhysics : MonoBehaviour
{
    [Header("범퍼 반발력 & 보상 설정")]
    [SerializeField] private float bounceForce = 15f;    // 공을 튕겨내는 힘
    [SerializeField] private int scoreReward = 100;     // 충돌 시 획득 점수
    [SerializeField] private int goldReward = 5;         // 충돌 시 획득 골드

    [Header("피드백 연출")]
    [SerializeField] private float punchScaleAmount = 0.2f; // 충돌 시 찌그러졌다 펴지는 연출 크기
    [SerializeField] private float restoreSpeed = 5f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // 충돌 피드백 후 원래 스케일로 부드럽게 복귀
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * restoreSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 핀볼 공과 충돌했을 때
        if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // 1. 충돌 지점 반대 방향으로 반발력 적용
                Vector3 hitNormal = collision.contacts[0].normal;
                ballRb.AddForce(-hitNormal * bounceForce, ForceMode.Impulse);

                // 2. 점수 및 재화 추가 (GameManager 연동)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(scoreReward);
                    GameManager.Instance.AddGold(goldReward);
                }

                // 3. 충돌 바운스 피드백 (Scale Punch)
                transform.localScale = originalScale * (1f + punchScaleAmount);
            }
        }
    }
}