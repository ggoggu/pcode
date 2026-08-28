using UnityEngine;

namespace PenguinPinball.Core
{
    public sealed class HarpoonAbility : PenguinAbilityBase
    {
        public override void Initialize(PenguinController o)
        {
            base.Initialize(o);
            GameEvents.EnemyKilled += OnKill;
        }

        public override void Dispose()
        {
            GameEvents.EnemyKilled -= OnKill;
        }

        void OnKill(PenguinController source, Enemy dead)
        {
            if (source != Owner || !Owner.IsOverloaded) return;

            var e = FindNearest(dead.transform.position);
            if (e) Owner.LaunchTowards(e.transform.position, Owner.Body.linearVelocity.magnitude);
        }

        Enemy FindNearest(Vector3 p)
        {
            var all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            Enemy best = null;
            float ds = float.MaxValue;
            foreach (var x in all)
            {
                if (x.IsDead) continue;

                float sqrDist = (x.transform.position - p).sqrMagnitude;

                if (sqrDist < ds)
                {
                    best = x;
                    ds = sqrDist;
                }
            }

            return best;
        }
    }
}
