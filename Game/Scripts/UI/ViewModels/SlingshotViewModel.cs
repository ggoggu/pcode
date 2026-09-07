using System;
using UnityEngine;

namespace PenguinPinball.Core
{
    public class SlingshotViewModel
    {
        private readonly PinballPhysicsConfig config;

        private Vector3 leftPos;
        private Vector3 rightPos;
        private Vector3 forwardDir;

        private bool isShaking;
        private float shakeTime;

        public event Action<Vector3, Quaternion, Vector3> OnColliderUpdated;
        public event Action<Vector3, Vector3, Vector3> OnLineUpdated;

        public SlingshotViewModel(PinballPhysicsConfig config)
        {
            this.config = config;
        }

        public void UpdateTransforms(Vector3 leftLocal, Vector3 rightLocal, float bumperRadius, Vector3 forwardLocal)
        {
            leftPos = leftLocal;
            rightPos = rightLocal;
            forwardDir = forwardLocal;

            Vector3 center = (leftPos + rightPos) * 0.5f;
            Vector3 dir = (rightPos - leftPos).normalized;
            Quaternion rot = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;

            float dist = Vector3.Distance(leftPos, rightPos);
            float rubberLength = Mathf.Max(0.1f, dist - bumperRadius * 2f);
            Vector3 size = new Vector3(config.slingshotColliderThickness, config.slingshotColliderHeight, rubberLength);

            OnColliderUpdated?.Invoke(center, rot, size);

            if (!isShaking)
            {
                OnLineUpdated?.Invoke(leftPos, center, rightPos);
            }
        }

        public float CalculateSpeed(float penguinMaxSpeed)
        {
            return Mathf.Clamp(config.slingshotLaunchForce, config.bounceMinimumSpeed, penguinMaxSpeed);
        }

        public void TriggerShake()
        {
            isShaking = true;
            shakeTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!isShaking) return;

            shakeTime += deltaTime;
            Vector3 originalCenter = (leftPos + rightPos) * 0.5f;

            if (shakeTime >= config.slingshotShakeDuration)
            {
                isShaking = false;
                OnLineUpdated?.Invoke(leftPos, originalCenter, rightPos);
                return;
            }

            float progress = shakeTime / config.slingshotShakeDuration;
            Vector3 pulledCenter = originalCenter - (forwardDir * config.slingshotShakeDistance);
            float offsetCoeff = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
            Vector3 currentCenter = Vector3.Lerp(originalCenter, pulledCenter, offsetCoeff);

            OnLineUpdated?.Invoke(leftPos, currentCenter, rightPos);
        }
    }
}