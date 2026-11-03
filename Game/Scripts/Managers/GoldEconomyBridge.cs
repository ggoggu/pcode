using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the existing coin/HUD balance and shop balance in sync in GameScene.
/// </summary>
public sealed class GoldEconomyBridge : MonoBehaviour
{
    private EconomyManager economy;
    private bool syncing;

    private IEnumerator Start()
    {
        // EconomyManager initializes its balance in Start; wait for that first.
        yield return null;

        economy = EconomyManager.Instance;
        if (economy == null || GameManager.Instance == null)
        {
            Debug.LogError("[GoldEconomyBridge] GameManager or EconomyManager is missing.");
            enabled = false;
            yield break;
        }

        economy.OnEconomyChanged += HandleEconomyChanged;
        GameManager.OnGoldChanged += HandleGameGoldChanged;
        HandleEconomyChanged();
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnEconomyChanged -= HandleEconomyChanged;
        GameManager.OnGoldChanged -= HandleGameGoldChanged;
    }

    private void HandleGameGoldChanged(int gold)
    {
        if (syncing || economy == null) return;

        syncing = true;
        try
        {
            int difference = gold - economy.CurrentGold;
            if (difference > 0) economy.AddGold(difference);
            else if (difference < 0) economy.SpendGold(-difference);

            // Respect the shop balance cap if a coin would exceed it.
            int correction = economy.CurrentGold - GameManager.Instance.CurrentGold;
            if (correction != 0) GameManager.Instance.AddGold(correction);
        }
        finally
        {
            syncing = false;
        }
    }

    private void HandleEconomyChanged()
    {
        if (syncing || GameManager.Instance == null) return;

        int difference = economy.CurrentGold - GameManager.Instance.CurrentGold;
        if (difference == 0) return;

        syncing = true;
        try
        {
            GameManager.Instance.AddGold(difference);
        }
        finally
        {
            syncing = false;
        }
    }
}
