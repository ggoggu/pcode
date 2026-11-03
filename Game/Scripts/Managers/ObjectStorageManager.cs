using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 맵(씬) 전환 자동 감지용 추가

namespace Game.ObjectSystem
{
    /// <summary>
    /// 오브젝트 종류 (§7)
    /// </summary>
    public enum ObjectType
    {
        Bumper,         // 범퍼
        Slingshot,      // 슬링샷
        CloneGate       // 유니크: 복제 게이트
    }

    /// <summary>
    /// 보관소 및 필드에 등록되는 개별 오브젝트 인스턴스 데이터 (§3)
    /// </summary>
    [System.Serializable]
    public class ObjectItem
    {
        public string InstanceId { get; private set; } // 개별 객체 식별용 고유 ID
        public ObjectType Type { get; private set; }

        public ObjectItem(ObjectType type)
        {
            InstanceId = Guid.NewGuid().ToString(); // 중복 습득 시에도 별도 인스턴스로 생성 (§3)
            Type = type;
        }
    }

    /// <summary>
    /// Phase 1: 오브젝트 보관소 매니저 (§3, §5)
    /// </summary>
    public class ObjectStorageManager : MonoBehaviour
    {
        public static ObjectStorageManager Instance { get; private set; }

        // §3. 수량 누적이 아닌 개별 인스턴스 리스트로 관리 (Non-stackable)
        private readonly List<ObjectItem> _storedObjects = new List<ObjectItem>();

        [Header("테스트 설정 (Test Setup)")]
        [Tooltip("게임 시작 시 테스트용으로 종류별 오브젝트를 1개씩 보관소에 자동 등록합니다.")]
        [SerializeField] private bool initializeTestObjects = true;
        [SerializeField] private List<ObjectType> testObjectTypes = new List<ObjectType>
        {
            ObjectType.Bumper,
            ObjectType.Slingshot
        };

        // 이벤트: UI 갱신 및 타 시스템 연동용
        public event Action<ObjectItem> OnObjectAdded;
        public event Action<ObjectItem> OnObjectRemoved;
        public event Action OnStorageCleared;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    if (transform.parent != null) transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            // 맵(씬)이 전환될 때 자동으로 감지하여 초기화 콜백 실행
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnMapTransitionStarted -= ClearOnMapTransition;
                MapManager.Instance.OnMapTransitionStarted += ClearOnMapTransition;
            }
        }

        private void Start()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnMapTransitionStarted -= ClearOnMapTransition;
                MapManager.Instance.OnMapTransitionStarted += ClearOnMapTransition;
            }

            if (initializeTestObjects && _storedObjects.Count == 0)
            {
                InitializeTestObjects();
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnMapTransitionStarted -= ClearOnMapTransition;
            }
        }

        /// <summary>
        /// 씬(맵) 전환 시 유니티에 의해 자동 호출되는 이벤트
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearOnMapTransition();
        }

        /// <summary>
        /// 보관 중인 전체 오브젝트 목록 조회
        /// </summary>
        public IReadOnlyList<ObjectItem> GetStoredObjects()
        {
            return _storedObjects.AsReadOnly();
        }

        /// <summary>
        /// §2 & §4-3. 습득 또는 필드 회수 시 보관소 진입
        /// </summary>
        public void AddObject(ObjectItem item)
        {
            if (item == null) return;

            // §3. 보관 한도 없음 (Capacity 제한 검사 제거)
            _storedObjects.Add(item);
            OnObjectAdded?.Invoke(item);
        }

        /// <summary>
        /// §4-4. 정비시간 중 필드 설치 확정 시 보관소에서 차감
        /// </summary>
        public bool RemoveObject(ObjectItem item)
        {
            if (item == null) return false;

            bool isRemoved = _storedObjects.Remove(item);
            if (isRemoved)
            {
                OnObjectRemoved?.Invoke(item);
            }
            return isRemoved;
        }

        /// <summary>
        /// §5. 맵 전환 시 소멸
        /// 필드 설치물과 함께 보관소 잔여분 전량 삭제 (골드 환산 없음)
        /// </summary>
        public void ClearOnMapTransition()
        {
            // 1. 보관소 잔여분 전량 삭제
            _storedObjects.Clear();
            OnStorageCleared?.Invoke();

            // 2. 필드에 이미 설치된 오브젝트 및 프리뷰 전량 삭제 (PlacementManager 연동)
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.ClearAllPlacedObjects();
            }

            Debug.Log("[ObjectStorageManager] 맵 전환 완료: 보관소 잔여분 및 필드 설치분이 예외 없이 전량 삭제되었습니다.");
        }

        /// <summary>
        /// ClearOnMapTransition의 호환용 별칭 메서드
        /// </summary>
        public void ClearAllStorage()
        {
            ClearOnMapTransition();
        }

        /// <summary>
        /// 테스트용: 종류별 오브젝트(기본: 범퍼, 슬링샷)를 1개씩 보관소에 등록합니다.
        /// </summary>
        public void InitializeTestObjects()
        {
            if (testObjectTypes == null) return;

            for (int i = 0; i < testObjectTypes.Count; i++)
            {
                AddObject(new ObjectItem(testObjectTypes[i]));
            }
        }
    }
}