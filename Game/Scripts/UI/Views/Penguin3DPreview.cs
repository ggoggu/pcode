using UnityEngine;
using UnityEngine.UI;
using PenguinPinball.Core;
using UnityEngine.Rendering.Universal;

namespace PenguinPinball.UI.Views
{
    /// <summary>
    /// UI 슬롯(Bench, Inventory, Hatchery 등)에서 펭귄 3D 모델을 독립된 RenderTexture 및 오프스크린 카메라로
    /// 렌더링하기 위한 통합 3D 프리뷰 컴포넌트입니다.
    /// 배를 대고 누워있는 인게임 펭귄 메쉬를 똑바로 서 있는 자세(Upright)로 보정하며,
    /// 뷰 가시성에 따른 온디맨드 카메라/라이트 게이팅 및 VRAM 최적화를 제공합니다.
    /// </summary>
    public class Penguin3DPreview : MonoBehaviour
    {
        [Header("Preview Settings")]
        [SerializeField] protected int slotIndex = 0;
        [SerializeField] protected float baseXOffset = 2500f;
        [SerializeField] protected string rigPrefix = "PreviewRig";
        [SerializeField] protected RawImage previewRawImage;
        [SerializeField] protected bool rotateModel = true;
        [SerializeField] protected float rotationSpeed = 25f;
        [SerializeField] protected Vector3 cameraOffset = new Vector3(0f, 0.32f, -1.35f);
        [SerializeField] protected Vector3 cameraRotation = new Vector3(14f, 0f, 0f);
        [SerializeField] protected Vector3 uprightRotationOffset = new Vector3(-90f, 0f, 0f);
        [SerializeField] protected Vector3 uprightPositionOffset = Vector3.zero;
        [SerializeField] protected Vector3 meshPenguinPositionOffset = new Vector3(0f, -0.48f, 0f);
        [SerializeField] protected float orthographicSize = 0.56f;
        [SerializeField] protected int textureResolution = 128;
        [SerializeField] protected int antiAliasing = 1;
        [SerializeField] protected int depthBits = 16;

        protected RenderTexture renderTexture;
        protected GameObject previewRig;
        protected Camera previewCamera;
        protected Transform modelAnchor;
        protected Light previewLight;
        protected GameObject currentModelInstance;
        protected GameObject currentSourcePrefab;

        public bool HasActive3DModel => currentModelInstance != null && previewRawImage != null && previewRawImage.gameObject.activeSelf;
        public RawImage PreviewRawImage => previewRawImage;
        public Camera PreviewCamera => previewCamera;
        public Light PreviewLight => previewLight;
        public GameObject CurrentModelInstance => currentModelInstance;
        public int SlotIndex => slotIndex;
        public float BaseXOffset => baseXOffset;
        public Vector3 UprightRotationOffset { get => uprightRotationOffset; set => uprightRotationOffset = value; }
        public Vector3 UprightPositionOffset
        {
            get => uprightPositionOffset;
            set
            {
                uprightPositionOffset = value;
                if (currentModelInstance != null)
                {
                    Vector3 pos = uprightPositionOffset;
                    if (IsMeshPenguin(currentSourcePrefab))
                    {
                        pos += meshPenguinPositionOffset;
                    }
                    currentModelInstance.transform.localPosition = pos;
                }
            }
        }
        public Vector3 MeshPenguinPositionOffset
        {
            get => meshPenguinPositionOffset;
            set
            {
                meshPenguinPositionOffset = value;
                if (currentModelInstance != null && IsMeshPenguin(currentSourcePrefab))
                {
                    currentModelInstance.transform.localPosition = uprightPositionOffset + meshPenguinPositionOffset;
                }
            }
        }
        public float OrthographicSize
        {
            get => orthographicSize;
            set
            {
                orthographicSize = value;
                if (previewCamera != null)
                {
                    previewCamera.orthographicSize = orthographicSize;
                }
            }
        }

        public virtual void Initialize(int index, RawImage rawImage, float baseX = 2500f, string prefix = "PreviewRig")
        {
            slotIndex = index;
            baseXOffset = baseX;
            rigPrefix = prefix;
            previewRawImage = rawImage;
            if (previewRawImage != null)
            {
                previewRawImage.raycastTarget = false;
            }
        }

        public virtual void SetSlotIndex(int index)
        {
            if (slotIndex == index) return;
            slotIndex = index;
            UpdateRigPosition();
        }

        public virtual void SetBaseXOffset(float baseX)
        {
            if (Mathf.Approximately(baseXOffset, baseX)) return;
            baseXOffset = baseX;
            UpdateRigPosition();
        }

        protected virtual void UpdateRigPosition()
        {
            if (previewRig != null)
            {
                previewRig.transform.position = new Vector3(baseXOffset + slotIndex * 25f, -2500f, 0f);
                previewRig.name = $"{rigPrefix}_Slot_{slotIndex}";
            }
        }

        public virtual void SetPreviewActive(bool active)
        {
            bool shouldRender = active && currentModelInstance != null;
            if (previewCamera != null) previewCamera.enabled = shouldRender;
            if (previewLight != null) previewLight.enabled = shouldRender;
        }

        protected virtual void EnsureRig()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(textureResolution, textureResolution, depthBits, RenderTextureFormat.ARGB32)
                {
                    name = $"{rigPrefix}_RT_{slotIndex}",
                    filterMode = FilterMode.Bilinear,
                    antiAliasing = antiAliasing,
                    useMipMap = false
                };
                renderTexture.Create();
            }

            if (previewRawImage != null)
            {
                previewRawImage.texture = renderTexture;
                previewRawImage.raycastTarget = false;
            }

            if (previewRig == null)
            {
                // UI 뷰별 고유 X 대역 및 오프스크린 좌표에 전용 릭 생성
                Vector3 rigPos = new Vector3(baseXOffset + slotIndex * 25f, -2500f, 0f);
                previewRig = new GameObject($"{rigPrefix}_Slot_{slotIndex}");
                previewRig.transform.position = rigPos;

                // 3D 모델을 배치할 앵커 트랜스폼
                var anchorObj = new GameObject("ModelAnchor");
                anchorObj.transform.SetParent(previewRig.transform, false);
                anchorObj.transform.localPosition = Vector3.zero;
                anchorObj.transform.localRotation = Quaternion.identity;
                modelAnchor = anchorObj.transform;

                // 전용 프리뷰 카메라 생성
                var camObj = new GameObject("PreviewCamera", typeof(Camera));
                camObj.transform.SetParent(previewRig.transform, false);
                camObj.transform.localPosition = cameraOffset;
                camObj.transform.localRotation = Quaternion.Euler(cameraRotation);

                previewCamera = camObj.GetComponent<Camera>();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // 투명 배경
                previewCamera.orthographic = true;
                previewCamera.orthographicSize = orthographicSize;
                previewCamera.nearClipPlane = 0.1f;
                previewCamera.farClipPlane = 10f;
                previewCamera.targetTexture = renderTexture;
                previewCamera.depth = -100;
                previewCamera.enabled = false;

                // URP 카메라 최적화
                var cameraData = camObj.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.renderShadows = false;
                }

                // 프리뷰 전용 조명 생성
                var lightObj = new GameObject("PreviewLight", typeof(Light));
                lightObj.transform.SetParent(previewRig.transform, false);
                lightObj.transform.localPosition = new Vector3(1f, 2f, -1.5f);
                lightObj.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);

                previewLight = lightObj.GetComponent<Light>();
                previewLight.type = LightType.Point;
                previewLight.range = 5f;
                previewLight.intensity = 2.5f;
                previewLight.color = Color.white;
                previewLight.enabled = false;
            }
        }

        public virtual void SetModel(PenguinController controller)
        {
            if (controller == null)
            {
                ClearModel();
                return;
            }

            GameObject visualPrefab = (controller.Definition != null) ? controller.Definition.VisualPrefab : null;
            if (visualPrefab == null)
            {
                Transform vr = controller.transform.Find("visualRoot");
                if (vr != null && vr.childCount > 0)
                {
                    visualPrefab = vr.GetChild(0).gameObject;
                }
            }

            SetModel(visualPrefab);
        }

        public virtual void SetModel(PenguinDefinition definition)
        {
            if (definition == null)
            {
                ClearModel();
                return;
            }

            SetModel(definition.VisualPrefab);
        }

        public virtual void SetModel(GameObject visualPrefab)
        {
            if (visualPrefab == null)
            {
                ClearModel();
                return;
            }

            EnsureRig();

            if (currentSourcePrefab == visualPrefab && currentModelInstance != null)
            {
                if (previewCamera != null) previewCamera.enabled = true;
                if (previewLight != null) previewLight.enabled = true;
                if (previewRawImage != null)
                {
                    previewRawImage.gameObject.SetActive(true);
                    previewRawImage.texture = renderTexture;
                }
                return;
            }

            ClearModelInstance();
            currentSourcePrefab = visualPrefab;

            currentModelInstance = Instantiate(visualPrefab, modelAnchor);

            // 배를 대고 누워있는 인게임 자세를 직립 자세(Upright)로 보정
            currentModelInstance.transform.localRotation = Quaternion.Euler(uprightRotationOffset);

            // 펭귄 메쉬(BasicPenguinVisual 등)가 적용된 모델은 피벗(발) 위치로 인해 상단 머리가 잘리지 않도록 위치 하향 보정
            Vector3 targetPosition = uprightPositionOffset;
            if (IsMeshPenguin(visualPrefab))
            {
                targetPosition += meshPenguinPositionOffset;
            }
            currentModelInstance.transform.localPosition = targetPosition;

            // 프리뷰 모델에 불필요한 물리 및 비주얼 제어 컴포넌트 비활성화
            foreach (var col in currentModelInstance.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }
            foreach (var rb in currentModelInstance.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
            }
            foreach (var handler in currentModelInstance.GetComponentsInChildren<PenguinVisualHandler>(true))
            {
                handler.enabled = false;
            }

            if (previewCamera != null) previewCamera.enabled = true;
            if (previewLight != null) previewLight.enabled = true;
            if (previewRawImage != null)
            {
                previewRawImage.gameObject.SetActive(true);
                previewRawImage.texture = renderTexture;
            }
        }

        public virtual void ClearModel()
        {
            ClearModelInstance();
            if (previewLight != null) previewLight.enabled = false;
            if (previewCamera != null) previewCamera.enabled = false;
            if (previewRawImage != null) previewRawImage.gameObject.SetActive(false);
        }

        protected virtual void ClearModelInstance()
        {
            if (currentModelInstance != null)
            {
                if (Application.isPlaying) Destroy(currentModelInstance);
                else DestroyImmediate(currentModelInstance);
                currentModelInstance = null;
            }
            currentSourcePrefab = null;
            if (modelAnchor != null)
            {
                modelAnchor.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 비주얼 프리팹이 실제 펭귄 메쉬(BasicPenguinVisual 등)인지 판별합니다.
        /// 임시 구체(Sphere) 메쉬와 달리 실제 펭귄 메쉬는 피벗이 발 아래에 위치하므로
        /// 프리뷰에서 머리가 상단 테두리에 잘리지 않도록 위치 하향 보정(meshPenguinPositionOffset)을 적용합니다.
        /// </summary>
        protected virtual bool IsMeshPenguin(GameObject visualPrefab)
        {
            if (visualPrefab == null) return false;

            string prefabName = visualPrefab.name;
            if (prefabName.IndexOf("BasicPenguin", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefabName.IndexOf("PenguinVisual", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefabName.IndexOf("Adelie", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (prefabName.IndexOf("tempVisualRoot", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            var meshFilters = visualPrefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.sharedMesh != null && !mf.sharedMesh.name.Equals("Sphere", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var skinnedMeshes = visualPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshes != null && skinnedMeshes.Length > 0)
            {
                return true;
            }

            return false;
        }

        protected virtual void Update()
        {
            // 카메라가 비활성화되어 있거나 모델이 없으면 불필요한 Transform 회전 연산 조기 탈출
            if (!rotateModel || modelAnchor == null || currentModelInstance == null || previewCamera == null || !previewCamera.enabled)
                return;

            modelAnchor.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        protected virtual void OnDestroy()
        {
            ClearModelInstance();
            if (previewRig != null)
            {
                if (Application.isPlaying) Destroy(previewRig);
                else DestroyImmediate(previewRig);
                previewRig = null;
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                if (Application.isPlaying) Destroy(renderTexture);
                else DestroyImmediate(renderTexture);
                renderTexture = null;
            }
        }
    }
}
