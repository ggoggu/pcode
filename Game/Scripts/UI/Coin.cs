using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("코인 설정")]
    [SerializeField] private int goldValue = 10;   // 코인 획득 시 증가할 기본 골드 양
    [SerializeField] private float lifetime = 10f; // 코인 자연 소멸 지속시간 (10초)

    private bool isCollected = false;

    private void Start()
    {
        // 10초 뒤에 먹지 않아도 자동으로 씬에서 삭제
        Destroy(gameObject, lifetime);
    }

    // 3D Collider 환경
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (IsPenguin(other.gameObject))
        {
            CollectCoin();
        }
    }

    // 2D Collider 환경
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (IsPenguin(collision.gameObject))
        {
            CollectCoin();
        }
    }

    // ★ 펭귄(공/플레이어) 감지 판정
    private bool IsPenguin(GameObject obj)
    {
        // 1. 태그로 판정 ("Player" 또는 "Ball" 태그를 가진 경우)
        if (obj.CompareTag("Player") || obj.CompareTag("Ball"))
        {
            return true;
        }

        // 2. 자식/부모 오브젝트에 태그가 붙어있는 경우 감지
        if (obj.transform.root.CompareTag("Player") || obj.transform.root.CompareTag("Ball"))
        {
            return true;
        }

        return false;
    }

    private void CollectCoin()
    {
        isCollected = true;

        // 1. GameManager를 통해 골드 추가 및 UI 반영
        if (GameManager.Instance != null)
        {
            float finalGold = StatModifierRegistry.Instance != null
                ? StatModifierRegistry.Instance.GetModifiedValue(StatType.GoldGainMultiplier, goldValue)
                : goldValue;

            GameManager.Instance.AddGold(Mathf.RoundToInt(finalGold));
        }

        // 2. 획득 시 즉시 코인 오브젝트 삭제
        Destroy(gameObject);
    }
}