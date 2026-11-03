using UnityEngine;
using Game.ObjectSystem;

public class StorageTester : MonoBehaviour
{
    private void Start()
    {
        // 씬 시작 시 보관소에 종류별로 1개씩만 투입 (테스트용)
        if (ObjectStorageManager.Instance != null)
        {
            if (ObjectStorageManager.Instance.GetStoredObjects().Count == 0)
            {
                ObjectStorageManager.Instance.InitializeTestObjects();
            }
        }
        else
        {
            Debug.LogError("ObjectStorageManager 가 씬에 존재하지 않습니다!");
        }
    }
}
