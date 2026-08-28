using System;
using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>Domain events only: no UI/economy calls are allowed from physics or combat.</summary>
    public static class GameEvents
    {
        public static event Action<PenguinController, Collision> PenguinHit;
        public static event Action<PenguinController, Enemy, float> EnemyDamaged;
        public static event Action<PenguinController, Enemy> EnemyKilled;
        public static event Action<PenguinController, int> ComboChanged;
        public static event Action<PenguinController> OverloadStarted;
        public static event Action<PenguinController> OverloadEnded;
        public static event Action<PenguinController, float> PenguinDrained;
        public static event Action<PenguinController> PenguinRespawned;
        public static event Action<PenguinController, PenguinController> PenguinsCollided;
        public static event Action<PenguinController> OnPenguinLaunched;
        public static event Action<int, int> BaseHealthChanged; // (currentHealth, maxHealth)
        public static event Action BaseDestroyed;
        public static event Action<PenguinController> EquipmentChanged;

        public static void RaisePenguinHit(PenguinController p, Collision c) => PenguinHit?.Invoke(p, c);
        public static void RaiseEnemyDamaged(PenguinController p, Enemy e, float damage) => EnemyDamaged?.Invoke(p, e, damage);
        public static void RaiseEnemyKilled(PenguinController p, Enemy e) => EnemyKilled?.Invoke(p, e);
        public static void RaiseComboChanged(PenguinController p, int value) => ComboChanged?.Invoke(p, value);
        public static void RaiseOverloadStarted(PenguinController p) => OverloadStarted?.Invoke(p);
        public static void RaiseOverloadEnded(PenguinController p) => OverloadEnded?.Invoke(p);
        public static void RaisePenguinDrained(PenguinController p, float seconds) => PenguinDrained?.Invoke(p, seconds);
        public static void RaisePenguinRespawned(PenguinController p) => PenguinRespawned?.Invoke(p);
        public static void RaisePenguinsCollided(PenguinController a, PenguinController b) => PenguinsCollided?.Invoke(a, b);
        public static void RaiseOnPenguinLaunched(PenguinController a) => OnPenguinLaunched?.Invoke(a);
        public static void RaiseBaseHealthChanged(int current, int max) => BaseHealthChanged?.Invoke(current, max);
        public static void RaiseBaseDestroyed() => BaseDestroyed?.Invoke();
        public static void RaiseEquipmentChanged(PenguinController p) => EquipmentChanged?.Invoke(p);
    }
}
