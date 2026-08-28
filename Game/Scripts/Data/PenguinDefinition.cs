using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 펭귄의 기본 스탯과 능력을 정의하는 ScriptableObject.
    /// 능력은 외부에서 PenguinAbilitySO를 할당하는 방식으로 주입됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "PenguinPinball/Penguin Definition")]
    public class PenguinDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Penguin";
        [SerializeField] private Sprite icon;

        [Header("Physics")]
        public float mass = 1f;
        public float linearDrag = 0.5f;
        public float maxSpeed = 15f;

        [Header("Combat")]
        public float attack = 10f;
        public float damageSpeedScale = 1f;
        public float lowSpeedCutoff = 5f;

        [Header("Combo / Overload")]
        public int overloadThreshold = 10;
        public float comboBaseDuration = 3f;
        public float durationLossPerCombo = 0.1f;
        public float comboDurationMultiplier = 1f;
        public float overloadMaxSpeedMultiplier = 1.5f;
        public float overloadDragMultiplier = 0.5f;

        [Header("Respawn")]
        public float respawnSeconds = 3f;

        [Header("Tier Growth Settings")]
        [Tooltip("티어당 공격력 증가율 (0.15 = 티어당 +15%)")]
        public float attackGrowthPerTier = 0.15f;

        [Tooltip("티어당 최대 속도 증가율 (0.05 = 티어당 +5%)")]
        public float maxSpeedGrowthPerTier = 0.05f;

        [Tooltip("티어당 질량 증가량 (고정치)")]
        public float massGrowthPerTier = 0.1f;

        [Tooltip("티어당 부활 시간 감소율 (0.05 = 티어당 -5%)")]
        public float respawnTimeGrowthPerTier = 0.05f;

        [Header("Visual")]
        [Tooltip("해당 펭귄 종류의 모델/스프라이트 프리팹")]
        [SerializeField] private GameObject visualPrefab;

        [Header("Ability")]
        [Tooltip("이 펭귄이 사용할 능력 ScriptableObject. 할당하지 않으면 빈 능력(EmptyAbility)이 사용됩니다.")]
        [SerializeField] private PenguinAbilitySO ability;

        public PenguinAbilitySO Ability => ability;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject VisualPrefab => visualPrefab;


        public IPenguinAbility CreateAbility()
        {
            return ability != null ? ability.CreateAbility() : new EmptyAbility();
        }
    }
}