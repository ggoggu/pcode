namespace PenguinPinball.Core
{
    /// <summary>
    /// 특정 벤치 슬롯에 영향을 미치는 부적/효과 인터페이스입니다.
    /// </summary>
    public interface IBenchSlotEffect
    {
        /// <summary>
        /// 지정된 벤치 슬롯 번호(0-based: 0 ~ MaxBenchCapacity-1)에 효과가 적용되는지 여부를 반환합니다.
        /// </summary>
        /// <param name="benchSlotIndex">0부터 시작하는 벤치 슬롯 인덱스</param>
        /// <returns>해당 슬롯에 적용되는 효과이면 true</returns>
        bool AffectsBenchSlot(int benchSlotIndex);
    }
}
