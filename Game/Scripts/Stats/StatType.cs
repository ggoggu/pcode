namespace PenguinPinball.Core
{
    public enum StatType
    {
        Attack,
        MaxSpeed,
        LinearDrag,
        Mass,
        RespawnTime
    }

    public enum StatModType
    {
        BaseFlat = 0,        // 티어 상승 등으로 인한 '기본 스탯' 고정치 증가
        BasePercentAdd = 10, // 티어 상승 등으로 인한 '기본 스탯' 비율 증가
        Flat = 100,      // 고정 증가량 (예: 공격력 +10)
        PercentAdd = 200 // 비율 증가량 (예: +0.10f = +10%)
    }
}