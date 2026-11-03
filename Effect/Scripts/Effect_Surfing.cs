using UnityEngine;

[CreateAssetMenu(fileName = "New Surfing Effect", menuName = "Charms/Effects/Surfing")]
public class Effect_Surfing : EffectBase
{
    [Header("파도타기 설정")]
    [Tooltip("정비 시간마다 제공할 무료 리롤 횟수")]
    public int freeRerollsPerMaintenance = 5;

    [Tooltip("파괴까지 필요한 웨이브 진행 횟수")]
    public int maxWaveDuration = 5;

    [Tooltip("무료 리롤을 상점과 부화장이 공유하는지 여부 (기본 true)")]
    public bool shareFreeRerollsBetweenShops = true;

    [Tooltip("판매 시 골드 정산 없이 파괴 여부")]
    public bool preventSellRefund = true;

    // 런타임 상태
    [SerializeField] private int remainingFreeRerolls;
    [SerializeField] private int completedWavesCount = 0;
    private bool wasInWave = false;

    public int RemainingFreeRerolls => remainingFreeRerolls;
    public int CompletedWavesCount => completedWavesCount;
    public bool HasFreeRerolls => remainingFreeRerolls > 0;

    public override void ApplyEffect(GameObject origin = null)
    {
        remainingFreeRerolls = freeRerollsPerMaintenance;
        wasInWave = false;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterSurfingEffect(this);
        }

        GameManager.OnStateChanged += HandleGameStateChanged;
        Debug.Log($"[Effect_Surfing] 파도타기 효과 활성화 (정비시간 무료 리롤: {freeRerollsPerMaintenance}회, 지속: {maxWaveDuration}웨이브)");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        GameManager.OnStateChanged -= HandleGameStateChanged;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UnregisterSurfingEffect(this);
        }

        Debug.Log("[Effect_Surfing] 파도타기 효과 해제");
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.WaveInProgress || newState == GameState.Wave)
        {
            wasInWave = true;
        }
        else if (wasInWave && (newState == GameState.MaintenanceTime || newState == GameState.MapTransition))
        {
            wasInWave = false;
            completedWavesCount++;
            Debug.Log($"[Effect_Surfing] 웨이브 진행 완료 ({completedWavesCount}/{maxWaveDuration})");

            TriggerUse();

            // 부적이 아직 파괴되지 않고 정비 시간에 진입한 경우 무료 리롤 5회 재충전
            if (newState == GameState.MaintenanceTime && (OwnerItem == null || OwnerItem.CurrentUses > 0))
            {
                ResetFreeRerolls();
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.NotifyShopUpdated();
                }
            }
        }
        else if (!wasInWave && newState == GameState.MaintenanceTime)
        {
            ResetFreeRerolls();
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.NotifyShopUpdated();
            }
        }
    }

    /// <summary>
    /// 무료 리롤 1회를 소모합니다. 잔여 무료 리롤이 있으면 true, 없으면 false 반환.
    /// </summary>
    public bool ConsumeFreeReroll()
    {
        if (remainingFreeRerolls > 0)
        {
            remainingFreeRerolls--;
            Debug.Log($"[Effect_Surfing] 무료 리롤 1회 사용 (남은 무료 리롤: {remainingFreeRerolls}회)");
            return true;
        }
        return false;
    }

    public void ResetFreeRerolls()
    {
        remainingFreeRerolls = freeRerollsPerMaintenance;
    }
}
