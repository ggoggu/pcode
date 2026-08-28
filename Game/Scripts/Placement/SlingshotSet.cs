using UnityEngine;

public class SlingshotSet : MonoBehaviour
{
    [Header("슬링샷 구성요소")]
    [SerializeField] private Transform bumperLeft;
    [SerializeField] private Transform bumperRight;
    [SerializeField] private SlingshotRubber rubberScript; // 자식 고무줄 스크립트

    private void Start()
    {
        InitSlingshot();
    }

    private void Update()
    {
        // 실시간으로 범퍼 위치가 바뀌거나 부모가 움직일 때 고무줄 위치 재계산
        if (rubberScript != null && bumperLeft != null && bumperRight != null)
        {
            rubberScript.UpdateRubberAndCollider();
        }
    }

    private void OnValidate()
    {
        // 에디터 상에서 배치/이동할 때도 실시간 반영
        InitSlingshot();
    }

    private void InitSlingshot()
    {
        if (bumperLeft != null && bumperRight != null && rubberScript != null)
        {
            rubberScript.Setup(bumperLeft, bumperRight);
        }
    }
}