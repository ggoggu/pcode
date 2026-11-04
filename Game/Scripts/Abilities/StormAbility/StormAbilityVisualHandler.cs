using PenguinPinball.Visuals;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class StormVisualHandler : MonoBehaviour, IAbilityVisualHandler
    {
        [Header("Impact Visual Prefab")]
        [Tooltip("충돌 시 연출될 푸른색 원 프리팹 (지름 1 unit 기준 메쉬/파티클)")]
        [SerializeField] private GameObject impactRingPrefab;

        [Header("Effect Settings")]
        [Tooltip("원 효과가 유지된 후 파괴될 시간 (초)")]
        [SerializeField] private float effectDuration = 0.5f;

        private StormAbility _stormAbility;

        public void Initialize(IPenguinAbility ability)
        {
            if (ability is StormAbility storm)
            {
                _stormAbility = storm;
                _stormAbility.OnOverloadImpact += HandleOverloadImpact;
            }
        }

        private void OnDestroy()
        {
            if (_stormAbility != null)
            {
                _stormAbility.OnOverloadImpact -= HandleOverloadImpact;
            }
        }

        private void HandleOverloadImpact(Vector3 position, float radius)
        {
            if (impactRingPrefab == null) return;

            // 1. 과부하 충돌 위치에 원 이펙트 생성
            GameObject ringFx = Instantiate(impactRingPrefab, position, Quaternion.identity);

            // 2. 반지름(radius)을 기준으로 정확한 직경(Diameter = radius * 2) 스케일 설정
            // X, Z 축으로 정확히 반지름만큼 퍼진 원 범위를 보여줍니다.
            float diameter = radius * 2f;
            ringFx.transform.localScale = new Vector3(diameter, 1f, diameter);

            // 3. 연출 시간 후 자동 파괴 (또는 Object Pool 반환 가능)
            Destroy(ringFx, effectDuration);
        }
    }
}