namespace PenguinPinball.Core
{
    /// <summary>
    /// 적에게 부착되는 표식(Mark) 디버프의 공통 인터페이스입니다.
    /// 동일 종류 표식 중복 판별 및 활성 상태를 확인하는 데 사용됩니다.
    /// </summary>
    public interface ICombatMark
    {
        /// <summary>
        /// 표식이 현재 유효하게 활성화되어 있는지 여부
        /// </summary>
        bool IsMarked { get; }

        /// <summary>
        /// 표식의 고유 식별자 또는 타입 이름 (동일 종류 표식 중복 판별용)
        /// </summary>
        string MarkType { get; }
    }
}
