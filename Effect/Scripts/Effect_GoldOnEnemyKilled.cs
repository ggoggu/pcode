using PenguinPinball.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gold On Kill Effect", menuName = "Charms/Effects/Gold On Enemy Killed")]
public class Effect_GoldOnEnemyKilled : EffectBase
{
    [Header("보너스 지급 설정")]
    [Tooltip("적을 처치할 때마다 추가로 지급할 골드량")]
    public int bonusGoldAmount = 1;

    [Tooltip("True일 경우, 펭귄이 '과부하' 상태일 때 처치한 적에게만 골드를 지급합니다.")]
    public bool requireOverload = false;

    public override void ApplyEffect(GameObject origin = null)
    {
        // 부적이 장착되면 적 처치 이벤트를 감지하기 시작합니다.
        GameEvents.EnemyKilled += OnEnemyKilled;
        Debug.Log("[Effect] 적 처치 이벤트 구독 시작");
    }

    public override void RemoveEffect(GameObject origin = null)
    {
        // 부적을 팔거나 버리면 감지를 중단합니다.
        GameEvents.EnemyKilled -= OnEnemyKilled;
        Debug.Log("[Effect] 적 처치 이벤트 구독 해제");
    }

    // 적이 죽을 때마다 GameEvents가 이 함수를 호출해 줍니다.
    private void OnEnemyKilled(PenguinController source, Enemy enemy)
    {
        // 펭귄 정보가 없거나, '과부하 요구' 조건이 켜져있는데 과부하 상태가 아니라면 무시합니다.
        if (requireOverload && source != null && !source.IsOverloaded)
        {
            return;
        }

        // 조건이 맞으면 CurrencyManager를 통해 골드를 지급합니다.
        if (CurrencyManager.Instance != null)
        {
            // 필요하다면 StatModifierRegistry의 골드 배율을 여기서 한 번 더 곱해줄 수도 있습니다.
            CurrencyManager.Instance.AddGold(bonusGoldAmount);
            
            // 디버그용 (실제 빌드 시 주석 처리 권장)
            Debug.Log($"[Charm Effect] 적 처치 트리거 발동! 보너스 골드 {bonusGoldAmount} 지급");

            // 소모형 부적인 경우 횟수 차감 및 소진 시 자동 제거
            TriggerUse();
        }
    }
}