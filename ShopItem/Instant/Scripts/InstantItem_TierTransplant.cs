using System.Linq;
using PenguinPinball.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "Instant_I01_GradeTransplant_T3", menuName = "Shop/Instant/TierTransplant")]
public class InstantItem_TierTransplant : InstantItem
{
    public InstantItem_TierTransplant()
    {
        ItemID = "I-01";
        ItemName = "등급 이식 T3";
        Type = ShopItemType.Instant;
        TargetType = InstantTargetType.TwoPenguins;
        InstructionText = "등급을 서로 교환할 두 펭귄을 선택하세요.";
        Description = "선택한 두 펭귄의 등급을 서로 교환합니다. 교환 후 합성 가능한 조합이 있으면 자동으로 합성됩니다.";
        Price = 15;
    }

    public override bool CanUse(out string failReason)
    {
        if (PenguinDeckManager.Instance == null || PenguinDeckManager.Instance.OwnedPenguins == null)
        {
            failReason = "펭귄 덱 매니저가 활성화되지 않았습니다.";
            return false;
        }

        // 펭귄이 1마리 이하인 경우 교환 불가
        if (PenguinDeckManager.Instance.OwnedPenguins.Count <= 1)
        {
            failReason = "펭귄이 1마리 이하일 때는 등급 이식을 구매하거나 사용할 수 없습니다.";
            return false;
        }

        // 출격 중이 아닌 교환 가능한 펭귄 수 확인
        int availableCount = PenguinDeckManager.Instance.OwnedPenguins.Count(p =>
        {
            if (p == null) return false;
            var controller = p.GetComponent<PenguinController>();
            return controller != null && !controller.IsInField;
        });

        if (availableCount < 2)
        {
            failReason = "등급을 교환하려면 출격 중이 아닌 펭귄이 최소 2마리 이상 필요합니다.";
            return false;
        }

        failReason = string.Empty;
        return true;
    }

    public override bool ExecuteTargetedEffect(params object[] targets)
    {
        if (targets == null || targets.Length < 2)
        {
            Debug.LogWarning("[InstantItem_TierTransplant] 교환할 두 대상이 지정되지 않았습니다.");
            return false;
        }

        PenguinController p1 = targets[0] as PenguinController;
        PenguinController p2 = targets[1] as PenguinController;

        if (p1 == null && targets[0] is GameObject go1) p1 = go1.GetComponent<PenguinController>();
        if (p2 == null && targets[1] is GameObject go2) p2 = go2.GetComponent<PenguinController>();

        if (p1 == null || p2 == null || p1 == p2)
        {
            Debug.LogWarning("[InstantItem_TierTransplant] 교환 대상 펭귄이 올바르지 않습니다.");
            return false;
        }

        if (PenguinDeckManager.Instance != null)
        {
            return PenguinDeckManager.Instance.SwapPenguinTiers(p1, p2);
        }

        return false;
    }
}
