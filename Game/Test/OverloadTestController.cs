using UnityEngine;

namespace PenguinPinball.Core
{
    public class OverloadTestController : MonoBehaviour
    {
        [Header("Test Target")]
        [SerializeField] private PenguinController penguin;

        [Header("Test Settings")]
        [SerializeField] private int comboAddAmount = 1;

        public PenguinController Penguin => penguin;
        public int ComboAddAmount => comboAddAmount;

        public void AddCombo()
        {
            if (!ValidatePenguin())
                return;

            penguin.AddCombo(comboAddAmount);

            PrintState("Combo Added");
        }

        public void AddComboUntilOverload()
        {
            if (!ValidatePenguin())
                return;

            int safetyCount = 1000;

            while (!penguin.IsOverloaded && safetyCount-- > 0)
            {
                penguin.AddCombo(comboAddAmount);
            }

            PrintState("Overload Trigger Test");
        }

        public void ResetCombo()
        {
            if (!ValidatePenguin())
                return;

            penguin.ResetCombo();

            PrintState("Combo Reset");
        }

        public void PrintState(string title = "Current State")
        {
            if (!ValidatePenguin())
                return;

            Debug.Log(
                $"========== {title} ==========\n" +
                $"Combo        : {penguin.Combo}\n" +
                $"Overloaded   : {penguin.IsOverloaded}\n" +
                $"Available    : {penguin.IsAvailable}\n" +
                $"In Field     : {penguin.IsInField}\n" +
                $"Tier         : {penguin.Tier}\n" +
                $"\n" +
                $"[Stats]\n" +
                $"Attack       : {GetStatValue(StatType.Attack)}\n" +
                $"Max Speed    : {GetStatValue(StatType.MaxSpeed)}\n" +
                $"Linear Drag  : {GetStatValue(StatType.LinearDrag)}\n" +
                $"Mass         : {GetStatValue(StatType.Mass)}\n" +
                $"Respawn Time : {GetStatValue(StatType.RespawnTime)}\n" +
                $"\n" +
                $"[Physics]\n" +
                $"Mass         : {penguin.Body.mass:F2}\n" +
                $"Damping      : {penguin.Body.linearDamping:F2}\n" +
                $"Speed        : {penguin.Body.linearVelocity.magnitude:F2}\n" +
                $"==============================",
                penguin
            );
        }

        private float GetStatValue(StatType type)
        {
            if (penguin.Stats.TryGetValue(type, out Stat stat))
                return stat.Value;

            return 0f;
        }

        private bool ValidatePenguin()
        {
            if (penguin != null)
                return true;

            Debug.LogWarning("Penguin is not assigned.", this);
            return false;
        }
    }
}