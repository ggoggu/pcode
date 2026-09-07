using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.ObjectSystem.UI
{
    /// <summary>
    /// Phase 1: 오브젝트 보관소 UI 매니저 (§3, §5)
    /// </summary>
    public class ObjectStorageUI : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] private GameObject _panelRoot;          // 보관소 창 전체 루트 오브젝트

        [Header("UI Layout References")]
        [SerializeField] private Transform _contentParent;          // ScrollView의 Grid Content Transform
        [SerializeField] private GameObject _slotPrefab;            // ObjectStorageSlotUI 프리팹

        [Header("Object Resource Data")]
        [SerializeField] private List<ObjectIconData> _iconDataList; // 오브젝트 타입별 아이콘/이름 매핑

        [System.Serializable]
        public struct ObjectIconData
        {
            public ObjectType Type;
            public Sprite Icon;
            public string DisplayName;
        }

        private readonly Dictionary<string, ObjectStorageSlotUI> _activeSlots = new Dictionary<string, ObjectStorageSlotUI>();

        private void Awake()
        {
            // PanelRoot 비어있을 경우 자기 자신으로 자동 지정
            if (_panelRoot == null) _panelRoot = gameObject;
        }

        private void OnEnable()
        {
            if (ObjectStorageManager.Instance != null)
            {
                ObjectStorageManager.Instance.OnObjectAdded += HandleObjectAdded;
                ObjectStorageManager.Instance.OnObjectRemoved += HandleObjectRemoved;
                ObjectStorageManager.Instance.OnStorageCleared += HandleStorageCleared;

                RefreshAllSlots();
            }
        }

        private void OnDisable()
        {
            if (ObjectStorageManager.Instance != null)
            {
                ObjectStorageManager.Instance.OnObjectAdded -= HandleObjectAdded;
                ObjectStorageManager.Instance.OnObjectRemoved -= HandleObjectRemoved;
                ObjectStorageManager.Instance.OnStorageCleared -= HandleStorageCleared;
            }
        }

        /// <summary>
        /// 버튼 연동용: 보관소 창 열기/닫기 토글
        /// </summary>
        public void TogglePanel()
        {
            bool isActive = !_panelRoot.activeSelf;
            _panelRoot.SetActive(isActive);

            if (isActive)
            {
                RefreshAllSlots();
            }
        }

        public void OpenPanel()
        {
            _panelRoot.SetActive(true);
            RefreshAllSlots();
        }

        public void ClosePanel()
        {
            _panelRoot.SetActive(false);
        }

        /// <summary>
        /// 현재 보관소 전체 데이터 기반 UI 동기화
        /// </summary>
        public void RefreshAllSlots()
        {
            ClearAllSlotGameObjects();

            if (ObjectStorageManager.Instance == null) return;

            var items = ObjectStorageManager.Instance.GetStoredObjects();
            foreach (var item in items)
            {
                CreateSlotUI(item);
            }
        }

        private void HandleObjectAdded(ObjectItem item)
        {
            if (_panelRoot.activeSelf) CreateSlotUI(item);
        }

        private void HandleObjectRemoved(ObjectItem item)
        {
            if (_activeSlots.TryGetValue(item.InstanceId, out var slot))
            {
                Destroy(slot.gameObject);
                _activeSlots.Remove(item.InstanceId);
            }
        }

        private void HandleStorageCleared()
        {
            ClearAllSlotGameObjects();
        }

        private void CreateSlotUI(ObjectItem item)
        {
            if (_activeSlots.ContainsKey(item.InstanceId)) return;

            GameObject slotObj = Instantiate(_slotPrefab, _contentParent);
            ObjectStorageSlotUI slotUI = slotObj.GetComponent<ObjectStorageSlotUI>();

            var (sprite, displayName) = GetMetaData(item.Type);
            slotUI.Init(item, sprite, displayName);

            _activeSlots.Add(item.InstanceId, slotUI);
        }

        private void ClearAllSlotGameObjects()
        {
            foreach (var slot in _activeSlots.Values)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _activeSlots.Clear();
        }

        private (Sprite sprite, string name) GetMetaData(ObjectType type)
        {
            foreach (var data in _iconDataList)
            {
                if (data.Type == type) return (data.Icon, data.DisplayName);
            }
            return (null, type.ToString());
        }
    }
}