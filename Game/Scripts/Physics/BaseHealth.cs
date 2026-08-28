using UnityEngine;

namespace PenguinPinball.Core
{
    public class BaseHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 10;
        private int currentHealth;

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            GameEvents.RaiseBaseHealthChanged(currentHealth, maxHealth); //초기체력 알림
        }

        public void TakeDamage(int damage)
        {
            if (IsDestroyed) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            GameEvents.RaiseBaseHealthChanged(currentHealth, maxHealth);
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            IsDestroyed = true;
            GameEvents.RaiseBaseDestroyed();
            Debug.Log("[BaseHealth] 베이스가 파괴되었습니다. 게임 오버!");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                TakeDamage(1);
                if (other.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.ReachBase();
                }
                else
                {
                    Destroy(other.gameObject);
                }
            }
        }

        // 마지막 웨이포인트 도달 시 호출할 경우
        //if (reachedFinalWaypoint)
        //{
        //    baseHealth.TakeDamage(1); // 미리 찾아둔 BaseHealth 참조 이용
        //    Destroy(gameObject);
        //}
    }
}