using UnityEngine;

public class Bumper : MonoBehaviour
{
    [Header("범퍼 물리 설정")]
    [SerializeField] private float bounceForce = 15.0f; // 튕겨내는 힘의 크기
    
    private EngravingItem currentEngraving; // 신규: 각인 상태

    // --- 신규 추가 영역 ---
    public bool TryEngrave(EngravingItem item)
    {
        if (currentEngraving != null) RemoveEngraving(true); // 기존 각인 해제 반환

        currentEngraving = item;
        foreach (var effect in currentEngraving.Effects)
            effect.ApplyEffect(gameObject); // 각인 효과 발동
            
        return true;
    }

    public void RemoveEngraving(bool returnToInventory)
    {
        if (currentEngraving == null) return;

        foreach (var effect in currentEngraving.Effects)
            effect.RemoveEffect(gameObject);

        if (returnToInventory)
            ItemInventoryManager.Instance.AddEngraving(currentEngraving);
            
        currentEngraving = null;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 충돌한 대상의 Rigidbody 가져오기 (한원석 님의 펭귄 객체)
        Rigidbody rb = collision.rigidbody;

        if (rb != null)
        {
            // 충돌 지점의 법선(Normal) 벡터를 이용해 튕겨나갈 방향 계산
            Vector3 bounceDirection = collision.contacts[0].normal;

            // 입사각 반사 방식이 아닌, 범퍼 중심에서 바깥쪽으로 밀어내려면 아래 주석 해제:
            // Vector3 bounceDirection = (collision.transform.position - transform.position).normalized;

            // 펭귄의 기존 속도를 일정 부분 초기화 후 강한 충격량(Impulse) 가하기
            rb.linearVelocity = Vector3.zero; // Unity 최신버전 (구버전은 rb.velocity)
            rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);

            Debug.Log($"[범퍼 충돌] {collision.gameObject.name}을(를) {bounceDirection} 방향으로 튕겨냄!");

            // TODO: 한원석 님(A)의 콤보 시스템이나 효과음 연동 시 여기서 이벤트/사운드 호출
        }
    }
}