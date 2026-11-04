using PenguinPinball.Visuals;
using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class RussianRouletteVisualHandler : MonoBehaviour, IAbilityVisualHandler
    {
        [Header("Mark FX Settings")]
        [Tooltip("러시안 룰렛 발동 시 적에게 부착할 흔적 프리팹")]
        [SerializeField] private GameObject markFxPrefab;

        [Tooltip("흔적 이펙트가 유지될 시간 (초)")]
        [SerializeField] private float markDuration = 0.4f;

        [Tooltip("적 위치 기준 흔적 생성 높이 오프셋 (로컬 좌표계)")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);

        private RussianRouletteAbility _rouletteAbility;

        public void Initialize(IPenguinAbility ability)
        {
            if (ability is RussianRouletteAbility roulette)
            {
                _rouletteAbility = roulette;
                _rouletteAbility.OnRouletteTriggered += HandleRouletteTriggered;
            }
        }

        private void OnDestroy()
        {
            if (_rouletteAbility != null)
            {
                _rouletteAbility.OnRouletteTriggered -= HandleRouletteTriggered;
            }
        }

        private void HandleRouletteTriggered(Enemy target, Vector3 hitPoint)
        {
            if (markFxPrefab == null || target == null || target.IsDead) return;

            // 1. 적의 Transform을 부모로 지정하여 스폰 (적을 따라 움직이도록 설정)
            GameObject markFx = Instantiate(markFxPrefab, target.transform);

            // 2. 적 기준 로컬 오프셋 및 프리팹 회전값 설정
            markFx.transform.localPosition = spawnOffset;
            markFx.transform.rotation = markFxPrefab.transform.rotation;

            // 3. 지정 시간 후 파괴 (적이 먼저 죽어서 파괴되면 자식인 이펙트도 자동으로 함께 삭제됨)
            Destroy(markFx, markDuration);
        }
    }
}