using UnityEngine;
using Game.ObjectSystem;

public class StorageTester : MonoBehaviour
{
    private void Start()
    {
        // 씬 시작 시 보관소에 범퍼 2개, 슬링샷 1개 강제 투입
        if (ObjectStorageManager.Instance != null)
        {
            ObjectStorageManager.Instance.AddObject(new ObjectItem(ObjectType.Bumper));
            ObjectStorageManager.Instance.AddObject(new ObjectItem(ObjectType.Bumper));
            ObjectStorageManager.Instance.AddObject(new ObjectItem(ObjectType.Slingshot));
        }
        else
        {
            Debug.LogError("ObjectStorageManager 가 씬에 존재하지 않습니다!");
        }
    }
}