using UnityEngine;
using PenguinPinball.Core;

[CreateAssetMenu(fileName = "New Overload Settlement Effect", menuName = "Charms/Effects/Overload Settlement")]
public class Effect_OverloadSettlement : EffectBase
{
    [Header("코인 드랍 설정")]
    [Tooltip("적 처치 시 추가 드랍할 코인 수 (기본 1개)")]
    public int extraCoinDropCount = 1;

    [Tooltip("추가 드랍할 코인 프리팹 (미지정 시 EnemyManager.CoinPrefab 사용)")]
    public GameObject coinPrefab;

    public override void ApplyEffect(GameObject origin = null)
    {
        GameEvents.EnemyKilled += HandleEnemyKilled;
        Debug.Log($"[Effect_OverloadSettlement] 과부하 정산 효과 활성화 (과부하 상태 처치 시 코인 {extraCoinDropCount}개 추가 드랍)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        GameEvents.EnemyKilled -= HandleEnemyKilled;
        Debug.Log("[Effect_OverloadSettlement] 과부하 정산 효과 해제");
    }

    private void HandleEnemyKilled(PenguinController source, Enemy enemy)
    {
        // 펭귄 정보가 없거나, 과부하 상태가 아니라면 발동하지 않음
        if (source == null || !source.IsOverloaded)
        {
            return;
        }

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
                Debug.Log($"[Effect_OverloadSettlement] 추가 코인 드랍 완료 at {spawnPos}");
            }
            else
            {
                // 프리팹이 없는 환경(단위 테스트 등)에서는 골드 매니저에 직접 지급 fallback
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddGold(10);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(10);
                }
                Debug.Log($"[Effect_OverloadSettlement] 코인 프리팹 없음 -> 골드 매니저에 +10G 지급 fallback");
            }
        }
    }
}
