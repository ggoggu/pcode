using UnityEngine;

namespace PenguinPinball.Core
{
    public interface IPenguinAbility
    {
        void Initialize(PenguinController owner);
        void OnOverloadStarted();
        void OnOverloadEnded();
        void OnEnemyCollision(Enemy enemy, Collision collision, ref float baseDamage, ref bool shouldDrain);
        void OnEnemyTriggerEnter(Enemy enemy, Collider other);
        void OnPenguinCollision(PenguinController other);
        void OnEnvironmentCollision(Collision c);
        void Tick();
        void Dispose();
    }

    public abstract class PenguinAbilityBase : IPenguinAbility
    {
        protected PenguinController Owner;
        public virtual void Initialize(PenguinController owner) => Owner = owner;

        public virtual void OnOverloadStarted() { }
        public virtual void OnOverloadEnded() { }
        public virtual void OnEnemyCollision(Enemy enemy, Collision collision, ref float baseDamage, ref bool shouldDrain) { }
        public virtual void OnEnemyTriggerEnter(Enemy enemy, Collider other) { }
        public virtual void OnPenguinCollision(PenguinController other) { }
        public virtual void OnEnvironmentCollision(Collision c) { }
        public virtual void Tick() { }
        public virtual void Dispose() { }
    }
}