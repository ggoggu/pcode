namespace PenguinPinball.Core
{
    [System.Serializable]
    public class StatModifier
    {
        public float Value;
        public StatModType Type;
        public object Source; // 어떤 장비에서 적용된 수치인지 식별용

        public StatModifier(float value, StatModType type, object source = null)
        {
            Value = value;
            Type = type;
            Source = source;
        }
    }
}