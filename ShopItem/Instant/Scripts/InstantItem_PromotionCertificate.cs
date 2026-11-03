using System.Linq;
using PenguinPinball.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "Instant_I02_PromotionCertificate_T2", menuName = "Shop/Instant/PromotionCertificate")]
public class InstantItem_PromotionCertificate : InstantItem
{
    public InstantItem_PromotionCertificate()
    {
        ItemID = "I-02";
        ItemName = "승급 증서 T2";
        Type = ShopItemType.Instant;
        TargetType = InstantTargetType.SinglePenguin;
        InstructionText = "승급할 펭귄을 선택해 주세요.";
        Description = "선택한 펭귄을 1성 영구 업그레이드합니다. 5성 초과는 불가합니다.";
        Price = 15;
    }

    public override bool CanUse(out string failReason)
    {
        if (PenguinDeckManager.Instance == null || PenguinDeckManager.Instance.OwnedPenguins == null)
        {
            failReason = "펭귄 덱 매니저가 활성화되지 않았습니다.";
            return false;
        }

        // 펭귄이 없는 경우(0마리) 구매/사용 불가
        if (PenguinDeckManager.Instance.OwnedPenguins.Count == 0)
        {
            failReason = "보유한 펭귄이 없어 승급 증서를 구매하거나 사용할 수 없습니다.";
            return false;
        }

        // 출격 중이 아니며 5성 미만인 승급 가능 펭귄 존재 여부 확인
        var availablePenguins = PenguinDeckManager.Instance.OwnedPenguins.Where(p =>
        {
            if (p == null) return false;
            var controller = p.GetComponent<PenguinController>();
            return controller != null && !controller.IsInField;
        }).ToList();

        if (availablePenguins.Count == 0)
        {
            failReason = "승급할 수 있는 펭귄이 덱에 없습니다.";
            return false;
        }

        bool hasUpgradablePenguin = availablePenguins.Any(p =>
        {
            var controller = p.GetComponent<PenguinController>();
            return controller != null && controller.Tier < 5;
        });

        if (!hasUpgradablePenguin)
        {
            failReason = "모든 펭귄이 이미 최대 성(5성)에 도달하여 승급할 수 없습니다.";
            return false;
        }

        failReason = string.Empty;
        return true;
    }

    public override bool ExecuteTargetedEffect(params object[] targets)
    {
        if (targets == null || targets.Length < 1)
        {
            Debug.LogWarning("[InstantItem_PromotionCertificate] 승급할 대상이 지정되지 않았습니다.");
            return false;
        }

        PenguinController targetPenguin = targets[0] as PenguinController;
        if (targetPenguin == null && targets[0] is GameObject go)
        {
            targetPenguin = go.GetComponent<PenguinController>();
        }

        if (targetPenguin == null)
        {
            Debug.LogWarning("[InstantItem_PromotionCertificate] 승급 대상 펭귄이 올바르지 않습니다.");
            return false;
        }

        if (targetPenguin.IsInField)
        {
            Debug.LogWarning("[InstantItem_PromotionCertificate] 출격 중인 펭귄은 승급할 수 없습니다.");
            return false;
        }

        if (targetPenguin.Tier >= 5)
        {
            Debug.LogWarning($"[InstantItem_PromotionCertificate] 5성 초과는 불가합니다. (현재 Tier: {targetPenguin.Tier})");
            return false;
        }

        if (PenguinDeckManager.Instance != null)
        {
            return PenguinDeckManager.Instance.PromotePenguinTier(targetPenguin);
        }

        return false;
    }
}
