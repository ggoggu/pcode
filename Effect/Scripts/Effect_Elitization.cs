using System.Linq;
using UnityEngine;
using PenguinPinball.Core;

[CreateAssetMenu(fileName = "New Elitization Effect", menuName = "Charms/Effects/Elitization")]
public class Effect_Elitization : EffectBase
{
    [Header("벤치 잠금 설정")]
    [Tooltip("잠글 벤치 맨 뒤 슬롯 개수 (기본 2칸)")]
    public int lockedBenchSlots = 2;

    [Tooltip("인벤토리 공간 부족 시 자동 판매 단가 (기본 10골드)")]
    public int penguinSellPrice = 10;

    [Header("코인 드랍 설정")]
    [Tooltip("적 처치 시 추가 드랍할 코인 수 (기본 1개)")]
    public int extraCoinDropCount = 1;

    [Tooltip("추가 드랍할 코인 프리팹 (미지정 시 EnemyManager.CoinPrefab 사용)")]
    public GameObject coinPrefab;

    public override void ApplyEffect(GameObject origin = null)
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.LockedBenchSlots += lockedBenchSlots;
        }

        GameManager.OnStateChanged += HandleStateChanged;
        GameEvents.EnemyKilled += HandleEnemyKilled;

        Debug.Log($"[Effect_Elitization] 정예화 효과 활성화 (벤치 뒤 {lockedBenchSlots}칸 잠금, 적 처치 시 추가 코인 {extraCoinDropCount}개)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        if (PenguinDeckManager.Instance != null)
        {
            PenguinDeckManager.Instance.LockedBenchSlots = Mathf.Max(0, PenguinDeckManager.Instance.LockedBenchSlots - lockedBenchSlots);
        }

        GameManager.OnStateChanged -= HandleStateChanged;
        GameEvents.EnemyKilled -= HandleEnemyKilled;

        Debug.Log("[Effect_Elitization] 정예화 효과 해제 (벤치 잠금 해제)");
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.WaveInProgress || newState == GameState.Wave)
        {
            ProcessLockedBenchPenguins();
        }
    }

    /// <summary>
    /// 웨이브 시작 시 잠긴 칸 위의 펭귄들을 인벤토리로 이동시키거나(빈 자리 있을 시),
    /// 인벤토리가 가득 찼으면 자동으로 판매합니다.
    /// </summary>
    public void ProcessLockedBenchPenguins()
    {
        if (PenguinDeckManager.Instance == null) return;

        var benchPenguins = PenguinDeckManager.Instance.OwnedPenguins
            .Where(p => p != null && p.GetComponent<PenguinLocationData>()?.Location == PenguinLocation.Bench)
            .ToList();

        int effectiveCapacity = PenguinDeckManager.Instance.EffectiveBenchCapacity;

        if (benchPenguins.Count > effectiveCapacity)
        {
            // 잠긴 슬롯(effectiveCapacity 이상)에 위치한 펭귄들을 뒤에서부터 처리
            var lockedPenguins = benchPenguins.Skip(effectiveCapacity).Reverse().ToList();

            foreach (var penguinObj in lockedPenguins)
            {
                if (penguinObj == null) continue;

                int currentInvCount = PenguinDeckManager.Instance.GetTotalInventoryCount();
                if (currentInvCount < PenguinDeckManager.Instance.MaxInventoryCapacity)
                {
                    bool moved = PenguinDeckManager.Instance.ChangePenguinLocation(penguinObj, PenguinLocation.Inventory);
                    if (!moved)
                    {
                        // 이동 실패 시(동시성 등) 안전하게 판매로 폴백
                        Debug.LogWarning($"[Effect_Elitization] 인벤토리 이동 실패 -> 자동 판매 진행: {penguinObj.name}");
                        PenguinDeckManager.Instance.SellPenguin(penguinObj, penguinSellPrice);
                    }
                    else
                    {
                        Debug.Log($"[Effect_Elitization] 잠긴 칸 위의 펭귄({penguinObj.name}) 인벤토리로 자동 이동 완료");
                    }
                }
                else
                {
                    Debug.Log($"[Effect_Elitization] 인벤토리 공간 부족 -> 잠긴 칸 위의 펭귄({penguinObj.name}) 자동 판매 완료 (+{penguinSellPrice}G)");
                    PenguinDeckManager.Instance.SellPenguin(penguinObj, penguinSellPrice);
                }
            }
        }
    }

    private void HandleEnemyKilled(PenguinController source, Enemy enemy)
    {
        Vector3 dropPos = enemy != null ? enemy.transform.position : Vector3.zero;
        DropExtraCoin(dropPos);
        TriggerUse();
    }

    /// <summary>
    /// 지정된 위치에 추가 코인을 드랍합니다.
    /// </summary>
    public void DropExtraCoin(Vector3 position)
    {
        for (int i = 0; i < extraCoinDropCount; i++)
        {
            GameObject prefabToSpawn = coinPrefab;
            if (prefabToSpawn == null && EnemyManager.Instance != null)
            {
                prefabToSpawn = EnemyManager.Instance.CoinPrefab;
            }

            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = position + new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    0.2f,
                    Random.Range(-0.3f, 0.3f)
                );
                Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                Debug.Log($"[Effect_Elitization] 추가 코인 드랍 완료 at {spawnPos}");
            }
            else
            {
                // 프리팹이 없는 환경(단위 테스트 등)에서는 골드 매니저에 직접 지급
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddGold(10);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(10);
                }
                Debug.Log($"[Effect_Elitization] 코인 프리팹 없음 -> 골드 매니저에 +10G 지급 fallback");
            }
        }
    }
}
