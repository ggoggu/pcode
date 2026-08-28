using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // New Input System
using Game.ObjectSystem;       // ObjectStorageManager 및 ObjectItem 참조

/// <summary>
/// UI 및 슬롯에서 참조하는 배치 오브젝트 타입 열거형
/// </summary>
public enum PlacementType
{
    None,
    Bumper,
    Slingshot
}

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("오브젝트 프리팹 프리셋")]
    [SerializeField] private GameObject bumperPrefab;
    [SerializeField] private GameObject slingshotPrefab;

    [Header("설치 조작 UI (버튼 3개 포함)")]
    [SerializeField] private GameObject placementControlUI;            // 확정/회전/회수 버튼이 포함된 UI Panel
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 2f, 0f); // 오브젝트 기준 UI 표시 위치 오프셋

    [Header("레이어 및 장애물 설정")]
    [SerializeField] private LayerMask groundMask;                      // 지형(바닥) 레이어
    [SerializeField] private LayerMask obstacleMask;                    // 장애물(기존 설치물, 지형 구조물, 적 경로)
    [SerializeField] private float bufferRadius = 0.2f;                 // 설치 여유 범위 (Buffer)

    [Header("청사진 머티리얼 색상 설정")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);   // 설치 가능 (초록)
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f); // 설치 불가 (빨강)

    // 필드에 실제 설치된 오브젝트 및 데이터 추적
    private readonly Dictionary<GameObject, ObjectItem> placedObjects = new Dictionary<GameObject, ObjectItem>();
    private readonly Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

    // 청사진 상태 변수
    private GameObject currentBlueprint;
    private Collider blueprintCollider;
    private Renderer[] blueprintRenderers;
    private Quaternion currentRotation = Quaternion.identity;
    private RectTransform uiRectTransform;

    private bool isFixedPosition = false;      // 바닥에 임시 고정되었는지 여부
    private bool isModifyingMode = false;
    private GameObject editingTargetOriginal; // 수정 모드 시 기존 원본 오브젝트
    private ObjectItem currentItem;            // 현재 배치 중인 ObjectItem 데이터

    public bool IsPlacing => currentBlueprint != null;
    public bool IsFixedPosition => isFixedPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (placementControlUI != null)
        {
            uiRectTransform = placementControlUI.GetComponent<RectTransform>();
            placementControlUI.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        // 정비시간에만 설치 및 조작 가능
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.MaintenanceTime)
            return;

        if (!IsPlacing) return;

        // 1. 위치가 고정되지 않은 동안 마우스 커서를 따라 이동
        if (!isFixedPosition)
        {
            UpdateBlueprintPositionAndRotation();
        }
        else
        {
            // 2. 위치가 고정된 상태에서는 3버튼 UI를 청사진 머리 위 화면 좌표로 이동
            UpdateUIPosition();
        }

        UpdateValidationVisuals();
        HandlePlacementInput();
    }

    #region UI 연동 메서드 (PlacementType & PlacementControlUI)

    /// <summary>
    /// [UI 연동] ObjectStorageSlotUI에서 클릭 시 PlacementType에 맞는 프리팹을 찾아 배치 시작
    /// </summary>
    public void SelectPlacementType(PlacementType targetType, ObjectItem item)
    {
        GameObject prefabToPlace = null;

        switch (targetType)
        {
            case PlacementType.Bumper:
                prefabToPlace = bumperPrefab;
                break;
            case PlacementType.Slingshot:
                prefabToPlace = slingshotPrefab;
                break;
        }

        if (prefabToPlace != null)
        {
            StartPlacement(prefabToPlace, item);
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] {targetType} 타입에 할당된 프리팹이 없습니다.");
        }
    }

    /// <summary>
    /// [UI 버튼 2] 지정한 각도(angle)만큼 청사진 회전
    /// </summary>
    public void RotateCurrentPreview(float angle = 90f)
    {
        if (!IsPlacing) return;

        currentRotation *= Quaternion.Euler(0f, angle, 0f);
        if (currentBlueprint != null)
        {
            currentBlueprint.transform.rotation = currentRotation;
        }
    }

    /// <summary>
    /// [UI 버튼 3] PlacementControlUI의 회수/취소 버튼 호출용
    /// </summary>
    public void RecallPlacement()
    {
        RecallToStorage();
    }

    #endregion

    #region 신규 설치 및 수정 모드 진입

    /// <summary>
    /// [신규 설치] 보관소에서 인스턴스(ObjectItem)를 선택하여 청사진 배치 시작
    /// </summary>
    public void StartPlacement(GameObject prefab, ObjectItem item)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;

        CancelCurrentBlueprint(); // 기존 배치 중이던 청사진 취소/회수

        isModifyingMode = false;
        editingTargetOriginal = null;
        currentItem = item;

        CreateBlueprint(prefab);
    }

    /// <summary>
    /// [수정 모드] 필드의 기존 설치물을 선택하여 재배치/회수 상태로 진입
    /// </summary>
    public void StartModify(GameObject existingObject)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.MaintenanceTime) return;
        if (!placedObjects.ContainsKey(existingObject)) return;

        CancelCurrentBlueprint();

        isModifyingMode = true;
        editingTargetOriginal = existingObject;
        currentItem = placedObjects[existingObject];

        // 자기 자신의 기존 위치 충돌 제외 처리 (원본 비활성화)
        editingTargetOriginal.SetActive(false);

        CreateBlueprint(existingObject);
        currentRotation = existingObject.transform.rotation;
    }

    #endregion

    #region 청사진 생성 및 비활성화 처리 (카메라 충돌 완벽 방지)

    private void CreateBlueprint(GameObject targetPrefab)
    {
        currentBlueprint = Instantiate(targetPrefab);

        // 1. 레이캐스트 충돌 방지: 레이어를 Ignore Raycast로 임시 변경
        originalLayers.Clear();
        SaveAndSetLayerRecursively(currentBlueprint.transform, LayerMask.NameToLayer("Ignore Raycast"));

        // 2. 물리 및 콜라이더 완벽 제어 (카메라로 튕겨 나가는 현상 차단)
        foreach (var rb in currentBlueprint.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in currentBlueprint.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        blueprintCollider = currentBlueprint.GetComponent<Collider>();
        blueprintRenderers = currentBlueprint.GetComponentsInChildren<Renderer>();

        // 3. 청사진 상태에서는 스크립트 비활성화
        MonoBehaviour[] scripts = currentBlueprint.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this) script.enabled = false;
        }

        SetFixedState(false);
    }

    private void SaveAndSetLayerRecursively(Transform trans, int newLayer)
    {
        originalLayers[trans] = trans.gameObject.layer;
        trans.gameObject.layer = newLayer;
        foreach (Transform child in trans)
        {
            SaveAndSetLayerRecursively(child, newLayer);
        }
    }

    private void RestoreOriginalLayers(GameObject obj)
    {
        foreach (var kvp in originalLayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.gameObject.layer = kvp.Value;
            }
        }
        originalLayers.Clear();
    }

    #endregion

    #region 실시간 이동 · 고정 처리 · 입력 관리

    private void UpdateBlueprintPositionAndRotation()
    {
        if (Mouse.current == null || Camera.main == null || currentBlueprint == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        // 자기 자신에 레이저가 맞아 카메라 쪽으로 당겨지는 현상 필터링
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, groundMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.transform == currentBlueprint.transform || hit.transform.IsChildOf(currentBlueprint.transform))
                continue;

            currentBlueprint.transform.position = hit.point;
            currentBlueprint.transform.rotation = currentRotation;
            break;
        }
    }

    private void UpdateUIPosition()
    {
        if (placementControlUI == null || currentBlueprint == null || !isFixedPosition) return;

        if (Camera.main != null && uiRectTransform != null)
        {
            Vector3 worldPos = currentBlueprint.transform.position + uiOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 카메라 뒤편에 위치한 경우 UI 숨김
            if (screenPos.z < 0)
            {
                placementControlUI.SetActive(false);
                return;
            }

            if (!placementControlUI.activeSelf)
            {
                placementControlUI.SetActive(true);
            }

            uiRectTransform.position = screenPos;
        }
    }

    private void HandlePlacementInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // UI 버튼 클릭 중에는 바닥 클릭 레이캐스트 무시
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // [조작 1: 바닥 클릭 시 위치 고정 또는 이동]
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !isPointerOverUI)
        {
            if (!isFixedPosition)
            {
                // 위치 고정 시도
                if (IsPlacementValid())
                {
                    SetFixedState(true);
                }
                else
                {
                    Debug.LogWarning("[PlacementManager] 설치할 수 없는 위치입니다.");
                }
            }
            else
            {
                // 이미 고정된 상태에서 바닥의 다른 곳을 클릭하면 그 위치로 재이동 및 고정
                Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                RaycastHit[] hits = Physics.RaycastAll(ray, 100f, groundMask);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.transform == currentBlueprint.transform || hit.transform.IsChildOf(currentBlueprint.transform))
                        continue;

                    currentBlueprint.transform.position = hit.point;
                    break;
                }
            }
        }

        // [단축키 1: R키 회전]
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            RotateCurrentPreview(90f);
        }

        // [단축키 2: 우클릭/X키 회수]
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame && !isPointerOverUI) ||
            (keyboard != null && keyboard.xKey.wasPressedThisFrame))
        {
            RecallPlacement();
        }
    }

    private void SetFixedState(bool isFixed)
    {
        isFixedPosition = isFixed;

        if (placementControlUI != null)
        {
            placementControlUI.SetActive(isFixed);
            if (isFixed)
            {
                UpdateUIPosition();
            }
        }
    }

    /// <summary>
    /// 설치 가능 판정 (Validation)
    /// </summary>
    public bool IsPlacementValid()
    {
        if (currentBlueprint == null) return false;

        Vector3 center = blueprintCollider != null ? blueprintCollider.bounds.center : currentBlueprint.transform.position;
        Vector3 extents = blueprintCollider != null ? blueprintCollider.bounds.extents : Vector3.one * 0.5f;

        Vector3 bufferExtents = extents + new Vector3(bufferRadius, bufferRadius, bufferRadius);
        Collider[] overlaps = Physics.OverlapBox(center, bufferExtents, currentRotation, obstacleMask);

        foreach (var col in overlaps)
        {
            if (col.gameObject == currentBlueprint || col.transform.IsChildOf(currentBlueprint.transform))
                continue;

            if (isModifyingMode && editingTargetOriginal != null && col.gameObject == editingTargetOriginal)
                continue;

            return false;
        }

        return true;
    }

    private void UpdateValidationVisuals()
    {
        bool isValid = IsPlacementValid();
        Color targetColor = isValid ? validColor : invalidColor;

        if (blueprintRenderers == null) return;
        foreach (var rend in blueprintRenderers)
        {
            if (rend != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = targetColor;
            }
        }
    }

    #endregion

    #region 설치 확정 · 회수 · 맵 전환 초기화

    /// <summary>
    /// [UI 버튼 1] 설치 확정 (실물화 및 보관소 차감)
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!IsPlacementValid())
        {
            Debug.LogWarning("[PlacementManager] 유효하지 않은 위치에서는 설치 확정을 할 수 없습니다.");
            return;
        }

        // 수정 모드였을 경우 기존 원본 오브젝트 파괴
        if (isModifyingMode && editingTargetOriginal != null)
        {
            placedObjects.Remove(editingTargetOriginal);
            Destroy(editingTargetOriginal);
        }
        else
        {
            // 신규 설치 확정 시 ObjectStorageManager에서 아이템 제거
            if (ObjectStorageManager.Instance != null && currentItem != null)
            {
                ObjectStorageManager.Instance.RemoveObject(currentItem);
            }
        }

        // 1. 원본 레이어 복원
        RestoreOriginalLayers(currentBlueprint);

        // 2. 실물 컴포넌트, 물리, 콜라이더 복원
        foreach (var rb in currentBlueprint.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
        }

        foreach (var col in currentBlueprint.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = false;
        }

        MonoBehaviour[] scripts = currentBlueprint.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = true;
        }

        // 3. 머티리얼 색상 원복
        foreach (var rend in blueprintRenderers)
        {
            if (rend != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = Color.white;
            }
        }

        // 4. 필드 설치물 등록
        if (currentItem != null)
        {
            placedObjects[currentBlueprint] = currentItem;
        }

        ResetState();
    }

    /// <summary>
    /// 회수 (보관소 복귀)
    /// </summary>
    public void RecallToStorage()
    {
        // 수정 모드였던 원본 파괴
        if (isModifyingMode && editingTargetOriginal != null)
        {
            placedObjects.Remove(editingTargetOriginal);
            Destroy(editingTargetOriginal);
        }

        // 배치 중이던 청사진 파괴
        if (currentBlueprint != null)
        {
            Destroy(currentBlueprint);
        }

        // 보관소로 아이템 복원
        if (ObjectStorageManager.Instance != null && currentItem != null)
        {
            ObjectStorageManager.Instance.AddObject(currentItem);
        }

        ResetState();
    }

    /// <summary>
    /// 맵 전환 시 필드 내 모든 설치물 및 청사진 전량 파괴 (ObjectStorageManager 연동용)
    /// </summary>
    public void ClearAllPlacedObjects()
    {
        if (currentBlueprint != null)
        {
            Destroy(currentBlueprint);
        }

        if (editingTargetOriginal != null)
        {
            Destroy(editingTargetOriginal);
        }

        foreach (var kvp in placedObjects)
        {
            if (kvp.Key != null)
            {
                Destroy(kvp.Key);
            }
        }

        placedObjects.Clear();
        ResetState();
        Debug.Log("[PlacementManager] 필드의 모든 설치물이 제거되었습니다.");
    }

    /// <summary>
    /// 정비시간 종료 시 미확정 청사진 자동 회수
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (newState != GameState.MaintenanceTime && IsPlacing)
        {
            RecallToStorage();
            Debug.Log("[PlacementManager] 정비시간 종료로 인해 미확정 청사진이 자동 회수되었습니다.");
        }
    }

    private void CancelCurrentBlueprint()
    {
        if (IsPlacing)
        {
            RecallToStorage();
        }
    }

    private void ResetState()
    {
        SetFixedState(false);
        currentBlueprint = null;
        blueprintCollider = null;
        blueprintRenderers = null;
        isModifyingMode = false;
        editingTargetOriginal = null;
        currentItem = null;
    }

    #endregion
}