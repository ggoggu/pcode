using System.Collections.Generic;
using UnityEngine;
using PenguinPinball.Core;

namespace PenguinPinball.UI.Util
{
    /// <summary>
    /// 특정 벤치 슬롯에 영향을 미치는 활성 부적을 조회하는 유틸리티 클래스입니다.
    /// </summary>
    public static class BenchSlotCharmUtility
    {
        /// <summary>
        /// 지정된 벤치 슬롯(0-based index)에 적용 중인 보유 부적 목록을 반환합니다.
        /// </summary>
        public static List<ShopItemBase> GetActiveCharmsForBenchSlot(int benchSlotIndex)
        {
            var result = new List<ShopItemBase>();
            if (ItemInventoryManager.Instance == null || ItemInventoryManager.Instance.OwnedCharms == null)
            {
                return result;
            }

            foreach (var charm in ItemInventoryManager.Instance.OwnedCharms)
            {
                if (charm == null) continue;
                if (IsCharmAffectingBenchSlot(charm, benchSlotIndex))
                {
                    if (!result.Contains(charm))
                    {
                        result.Add(charm);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 해당 부적이 특정 벤치 슬롯(0-based index)에 효과를 활성화하고 있는지 검사합니다.
        /// 1) 부적(ShopItemBase) 자체에 특정 벤치 슬롯(targetBenchSlots)이 지정되어 있으면 해당 슬롯에만 적용
        /// 2) 부적의 Effect 중 IBenchSlotEffect나 Effect_BenchLaunchStatBuff가 있으면 해당 슬롯에만 적용
        /// 3) 특정 슬롯 제약이 없는 모든 일반 부적(상점의 모든 부적들)은 벤치에 효과가 적용되어 표시됩니다.
        /// </summary>
        public static bool IsCharmAffectingBenchSlot(ShopItemBase charm, int benchSlotIndex)
        {
            if (charm == null) return false;

            int slotNumber = benchSlotIndex + 1; // 1-based (1번 벤치 = 1)

            // 1. ShopItemBase 자체에 특정 벤치 슬롯이 명시적으로 지정되어 있는 경우
            if (charm.targetBenchSlots != null && charm.targetBenchSlots.Count > 0)
            {
                return charm.targetBenchSlots.Contains(slotNumber);
            }

            // 2. 부적의 Effects 중 특정 슬롯 한정 효과(IBenchSlotEffect 등)가 있는 경우
            if (charm.Effects != null && charm.Effects.Count > 0)
            {
                bool hasSlotSpecificEffect = false;
                bool slotMatches = false;

                foreach (var effect in charm.Effects)
                {
                    if (effect == null) continue;

                    if (effect is IBenchSlotEffect benchSlotEffect)
                    {
                        hasSlotSpecificEffect = true;
                        if (benchSlotEffect.AffectsBenchSlot(benchSlotIndex))
                        {
                            slotMatches = true;
                        }
                    }
                    else if (effect is Effect_BenchLaunchStatBuff buffEffect)
                    {
                        hasSlotSpecificEffect = true;
                        if (buffEffect.AffectsBenchSlot(benchSlotIndex))
                        {
                            slotMatches = true;
                        }
                    }
                }

                if (hasSlotSpecificEffect)
                {
                    return slotMatches;
                }
            }

            // 3. 특정 슬롯 한정이 없는 전역 효과 부적: 전체 벤치 슬롯에 표시
            return true;
        }
    }
}
