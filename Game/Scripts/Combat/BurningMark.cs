using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class BurningMark : MonoBehaviour
    {
        public PenguinController Source { get; private set; }
        public bool IsBurning => _remainingDuration > 0f;

        private float _remainingDuration;
        private float _tickInterval;
        private float _damagePerTick;
        private float _timer;

        public void ApplyOrRefresh(PenguinController source, float duration, float tickInterval, float damagePerTick)
        {
            Source = source;
            _remainingDuration = duration;
            _tickInterval = tickInterval;
            _damagePerTick = damagePerTick;
            _timer = 0f;
        }

        private void Update()
        {
            if (!TryGetComponent<Enemy>(out var enemy) || enemy.IsDead)
            {
                Destroy(this);
                return;
            }

            _remainingDuration -= Time.deltaTime;
            if (_remainingDuration <= 0f)
            {
                Destroy(this);
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= _tickInterval)
            {
                _timer -= _tickInterval;

                if (Source != null && _damagePerTick > 0f)
                {
                    CombatResolver.DealDamage(Source, enemy, _damagePerTick);
                }
            }
        }
    }
}