using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 모든 펭귄 능력의 ScriptableObject 기반 정의.
    /// 인스펙터에서 설정값을 튜닝하고, CreateAbility()로 런타임 인스턴스를 생성합니다.
    /// PenguinDefinition의 ability 필드에 외부에서 할당하는 방식으로 사용합니다.
    /// </summary>
    public abstract class PenguinAbilitySO : ScriptableObject
    {
        /// <summary>
        /// 능력 이름 (표시용).
        /// </summary>
        [SerializeField] private string abilityName = "Unnamed Ability";
        public string AbilityName => abilityName;

        /// <summary>
        /// 능력 설명 (표시용).
        /// </summary>
        [TextArea, SerializeField] private string description = "";
        public string Description => description;

        /// <summary>
        /// 런타임 능력 인스턴스를 생성합니다.
        /// 호출할 때마다 새 인스턴스를 반환해야 합니다 (상태 공유 방지).
        /// </summary>
        public abstract IPenguinAbility CreateAbility();
    }
}