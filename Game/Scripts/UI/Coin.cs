using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("코인 설정")]
    [SerializeField] private int goldValue = 10;   // 코인 획득 시 증가할 기본 골드 양
    [SerializeField] private float lifetime = 10f; // 코인 자연 소멸 지속시간 (10초)
    [SerializeField] private float pickupDelay = 0.35f; // 스폰 직후 즉시 획득 방지를 위한 유예 시간

    private static readonly System.Collections.Generic.HashSet<Coin> activeCoins = new System.Collections.Generic.HashSet<Coin>();

    private bool isCollected = false;
    private float spawnTime;

    public int GoldValue => goldValue;
    public float PickupDelay { get => pickupDelay; set => pickupDelay = value; }

    /// <summary>
    /// 분할 드랍 등으로 인해 코인당 골드 액수를 동적으로 설정할 때 사용합니다.
    /// </summary>
    public void SetGoldValue(int value)
    {
        goldValue = Mathf.Max(0, value);
    }

    private void Awake()
    {
        spawnTime = Time.time;
    }

    private void OnEnable()
    {
        activeCoins.Add(this);
    }

    private void OnDisable()
    {
        activeCoins.Remove(this);
    }

    /// <summary>
    /// 웨이브 종료 시 필드에 남아있는 미회수 코인 전량 소멸 (무정산)
    /// </summary>
    public static void ClearAllFieldCoins()
    {
        var coinsToDestroy = new System.Collections.Generic.List<Coin>(activeCoins);
        foreach (var coin in coinsToDestroy)
        {
            if (coin != null && coin.gameObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(coin.gameObject);
                }
                else
                {
                    DestroyImmediate(coin.gameObject);
                }
            }
        }
        activeCoins.Clear();
        Debug.Log("[Coin] 필드 잔여 코인이 전량 소멸되었습니다 (무정산).");
    }

    private void Start()
    {
        // 10초 뒤에 먹지 않아도 자동으로 씬에서 삭제
        Destroy(gameObject, lifetime);
    }

    private bool CanCollect()
    {
        return !isCollected && (Time.time >= spawnTime + pickupDelay);
    }

    // 3D Collider 환경
    private void OnTriggerEnter(Collider other)
    {
        if (!CanCollect()) return;

        if (IsPenguin(other.gameObject))
        {
            CollectCoin();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!CanCollect()) return;

        if (IsPenguin(other.gameObject))
        {
            CollectCoin();
        }
    }

    // 2D Collider 환경
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!CanCollect()) return;

        if (IsPenguin(collision.gameObject))
        {
            CollectCoin();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!CanCollect()) return;

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