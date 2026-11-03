using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PenguinPinball.UI.ViewModels;
using PenguinPinball.Core;

namespace PenguinPinball.UI.Views
{
    /// <summary>
    /// 개별 벤치 슬롯의 시각적 요소(합성 등급별 테두리 색상, 레벨별 별 개수, 상단 활성 부적 원형 아이콘)를 제어하는 컴포넌트입니다.
    /// </summary>
    public class BenchSlotUI : MonoBehaviour
    {
        [Header("Border Settings")]
        [SerializeField] private Image borderImage;
        [SerializeField] private Sprite borderSprite;
        [SerializeField] private Color level1Color = new Color(127f / 255f, 127f / 255f, 127f / 255f, 1f); // 회색 (#7F7F7F)
        [SerializeField] private Color level2Color = new Color(181f / 255f, 230f / 255f, 29f / 255f, 1f);  // 연두 (#B5E61D)
        [SerializeField] private Color level3Color = new Color(0f / 255f, 162f / 255f, 232f / 255f, 1f);   // 하늘 (#00A2E8)
        [SerializeField] private Color level4Color = new Color(255f / 255f, 242f / 255f, 0f / 255f, 1f);   // 노랑 (#FFF200)
        [SerializeField] private Color level5Color = new Color(237f / 255f, 28f / 255f, 36f / 255f, 1f);   // 빨강 (#ED1C24)
        [SerializeField] private Color defaultBorderColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color lockedBorderColor = new Color(1f, 0.35f, 0.35f, 0.5f);

        [Header("3D Model Preview Settings")]
        [SerializeField] private RawImage modelPreviewRawImage;
        [SerializeField] private Penguin3DPreview preview3D;
        [SerializeField] private Vector2 modelPreviewSize = new Vector2(70f, 70f);
        [SerializeField] private int slotIndex = 0;
        [SerializeField] private float previewBaseX = 2500f;
        [SerializeField] private string previewRigPrefix = "BenchPreviewRig";

        [Header("Star Settings")]
        [SerializeField] private RectTransform starContainer;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private List<Image> starImages = new List<Image>();
        [SerializeField] private Color starColor = Color.white;
        [SerializeField] private float starSize = 16f;
        [SerializeField] private float starSpacing = 1f;

        [Header("Charm Settings (Up to 6)")]
        [SerializeField] private RectTransform charmContainer;
        [SerializeField] private RectTransform charmRow1;
        [SerializeField] private RectTransform charmRow2;
        [SerializeField] private Sprite charmCircleSprite;
        [SerializeField] private List<GameObject> charmCircleSlots = new List<GameObject>();
        [SerializeField] private List<Image> charmIconImages = new List<Image>();
        [SerializeField] private float charmCircleSize = 22f;

        [Header("Default Charm Placeholder Sprites")]
        [SerializeField] private Sprite defaultCharmIconSprite;
        [SerializeField] private List<Sprite> placeholderCharmSprites = new List<Sprite>();

        public Color Level1Color => level1Color;
        public Color Level2Color => level2Color;
        public Color Level3Color => level3Color;
        public Color Level4Color => level4Color;
        public Color Level5Color => level5Color;
        public Color DefaultBorderColor => defaultBorderColor;
        public Color LockedBorderColor => lockedBorderColor;
        public Image BorderImage => borderImage;
        public RawImage ModelPreviewRawImage => modelPreviewRawImage;
        public Penguin3DPreview Preview3D => preview3D;
        public int SlotIndex => slotIndex;
        public IReadOnlyList<Image> StarImages => starImages;
        public IReadOnlyList<GameObject> CharmCircleSlots => charmCircleSlots;
        public IReadOnlyList<Image> CharmIconImages => charmIconImages;
        public Sprite DefaultCharmIconSprite => defaultCharmIconSprite;
        public IReadOnlyList<Sprite> PlaceholderCharmSprites => placeholderCharmSprites;

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void SetSlotIndex(int index)
        {
            SetSlotIndex(index, previewBaseX, previewRigPrefix);
        }

        public void SetSlotIndex(int index, float baseX, string rigPrefix)
        {
            slotIndex = index;
            previewBaseX = baseX;
            previewRigPrefix = rigPrefix;
            if (preview3D != null)
            {
                preview3D.SetSlotIndex(index);
                preview3D.SetBaseXOffset(baseX);
            }
        }

        /// <summary>
        /// 필요한 자식 UI 계층 구조(Border, ModelPreview, StarContainer, CharmContainer)가 없으면 동적으로 구성합니다.
        /// </summary>
        public void EnsureHierarchy()
        {
            LoadDefaultSpritesIfNull();
            EnsureBorder();
            EnsureModelPreview();
            EnsureStars();
            EnsureCharms();
        }

        private void LoadDefaultSpritesIfNull()
        {
            if (borderSprite == null)
            {
                borderSprite = Resources.Load<Sprite>("BenchSlotBorder");
#if UNITY_EDITOR
                if (borderSprite == null)
                    borderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/BenchSlotBorder.png");
#endif
            }

            if (starSprite == null)
            {
                starSprite = Resources.Load<Sprite>("BenchStar");
#if UNITY_EDITOR
                if (starSprite == null)
                    starSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/BenchStar.png");
#endif
            }

            if (charmCircleSprite == null)
            {
                charmCircleSprite = Resources.Load<Sprite>("BenchCharmCircle");
#if UNITY_EDITOR
                if (charmCircleSprite == null)
                    charmCircleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/BenchCharmCircle.png");
#endif
            }

            if (defaultCharmIconSprite == null)
            {
                defaultCharmIconSprite = Resources.Load<Sprite>("BenchCharmDefaultIcon");
#if UNITY_EDITOR
                if (defaultCharmIconSprite == null)
                    defaultCharmIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/BenchCharmDefaultIcon.png");
#endif
            }

            if (placeholderCharmSprites == null)
            {
                placeholderCharmSprites = new List<Sprite>();
            }

            if (placeholderCharmSprites.Count == 0)
            {
                string[] spriteNames = new string[]
                {
                    "BenchCharm_Flag",
                    "BenchCharm_Cake",
                    "BenchCharm_Star",
                    "BenchCharm_Bolt",
                    "BenchCharm_Heart",
                    "BenchCharm_Shield"
                };

                foreach (var sName in spriteNames)
                {
                    Sprite s = Resources.Load<Sprite>(sName);
#if UNITY_EDITOR
                    if (s == null)
                    {
                        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/Sprites/{sName}.png");
                    }
#endif
                    if (s != null)
                    {
                        placeholderCharmSprites.Add(s);
                    }
                }

                if (placeholderCharmSprites.Count == 0 && defaultCharmIconSprite != null)
                {
                    placeholderCharmSprites.Add(defaultCharmIconSprite);
                }
            }
        }

        private void EnsureBorder()
        {
            if (borderImage == null)
            {
                var borderTransform = transform.Find("Border");
                if (borderTransform != null)
                {
                    borderImage = borderTransform.GetComponent<Image>();
                }

                if (borderImage == null)
                {
                    var borderObj = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    borderObj.transform.SetParent(transform, false);
                    borderTransform = borderObj.transform;

                    var rt = borderObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    borderImage = borderObj.GetComponent<Image>();
                    borderImage.raycastTarget = false;
                    borderImage.type = Image.Type.Sliced;
                    if (borderSprite != null) borderImage.sprite = borderSprite;
                    borderImage.color = defaultBorderColor;
                }
            }

            if (borderImage != null && borderImage.sprite == null && borderSprite != null)
            {
                borderImage.sprite = borderSprite;
                borderImage.type = Image.Type.Sliced;
            }
        }

        private void EnsureModelPreview()
        {
            if (modelPreviewRawImage == null)
            {
                var previewTransform = transform.Find("ModelPreview");
                if (previewTransform != null)
                {
                    modelPreviewRawImage = previewTransform.GetComponent<RawImage>();
                }

                if (modelPreviewRawImage == null)
                {
                    var previewObj = new GameObject("ModelPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                    previewObj.transform.SetParent(transform, false);

                    var rt = previewObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = modelPreviewSize;
                    rt.anchoredPosition = Vector2.zero;

                    modelPreviewRawImage = previewObj.GetComponent<RawImage>();
                    modelPreviewRawImage.raycastTarget = false;
                }
            }

            if (modelPreviewRawImage != null)
            {
                modelPreviewRawImage.raycastTarget = false;

                // 프레임(Border) 뒤로 보이도록 Sibling Index를 Border 바로 앞(아래)으로 설정
                if (borderImage != null)
                {
                    int borderIndex = borderImage.transform.GetSiblingIndex();
                    int previewIndex = modelPreviewRawImage.transform.GetSiblingIndex();
                    if (previewIndex > borderIndex)
                    {
                        modelPreviewRawImage.transform.SetSiblingIndex(Mathf.Max(0, borderIndex));
                    }
                }
            }

            if (preview3D == null)
            {
                preview3D = GetComponent<Penguin3DPreview>();
                if (preview3D == null)
                {
                    preview3D = gameObject.AddComponent<BenchSlot3DPreview>();
                }
            }

            if (preview3D != null)
            {
                preview3D.Initialize(slotIndex, modelPreviewRawImage, previewBaseX, previewRigPrefix);
            }
        }

        private void EnsureStars()
        {
            if (starContainer == null)
            {
                var containerTransform = transform.Find("StarContainer");
                if (containerTransform != null)
                {
                    starContainer = containerTransform.GetComponent<RectTransform>();
                }

                if (starContainer == null)
                {
                    var containerObj = new GameObject("StarContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    containerObj.transform.SetParent(transform, false);
                    starContainer = containerObj.GetComponent<RectTransform>();

                    // 하단 중앙에 배치 (테두리 하단선에 걸치도록)
                    starContainer.anchorMin = new Vector2(0.5f, 0f);
                    starContainer.anchorMax = new Vector2(0.5f, 0f);
                    starContainer.pivot = new Vector2(0.5f, 0.5f);
                    starContainer.anchoredPosition = new Vector2(0f, 0f);
                    starContainer.sizeDelta = new Vector2(80f, starSize + 4f);

                    var hlg = containerObj.GetComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = starSpacing;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }
            }

            // 5개의 별 이미지 확인/생성
            if (starImages == null) starImages = new List<Image>();
            while (starImages.Count < 5)
            {
                int index = starImages.Count + 1;
                string starName = $"Star_{index}";
                Transform starTransform = starContainer.Find(starName);
                Image starImg = starTransform != null ? starTransform.GetComponent<Image>() : null;

                if (starImg == null)
                {
                    var starObj = new GameObject(starName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    starObj.transform.SetParent(starContainer, false);

                    var rt = starObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(starSize, starSize);

                    starImg = starObj.GetComponent<Image>();
                    starImg.raycastTarget = false;
                    starImg.sprite = starSprite;
                    starImg.color = starColor;
                    starImg.preserveAspect = true;
                }

                starImages.Add(starImg);
            }
        }

        private void EnsureCharms()
        {
            if (charmContainer == null)
            {
                var containerTransform = transform.Find("CharmContainer");
                if (containerTransform != null)
                {
                    charmContainer = containerTransform.GetComponent<RectTransform>();
                }

                if (charmContainer == null)
                {
                    var containerObj = new GameObject("CharmContainer", typeof(RectTransform));
                    containerObj.transform.SetParent(transform, false);
                    charmContainer = containerObj.GetComponent<RectTransform>();

                    // 상단 중앙에 배치
                    charmContainer.anchorMin = new Vector2(0.5f, 1f);
                    charmContainer.anchorMax = new Vector2(0.5f, 1f);
                    charmContainer.pivot = new Vector2(0.5f, 0.5f);
                    charmContainer.anchoredPosition = new Vector2(0f, 0f);
                    charmContainer.sizeDelta = new Vector2(76f, charmCircleSize * 2f);
                }
            }

            // 2개의 행(Row 1, Row 2) 구성
            if (charmRow1 == null)
            {
                var r1Transform = charmContainer.Find("CharmRow1");
                if (r1Transform != null) charmRow1 = r1Transform.GetComponent<RectTransform>();
                if (charmRow1 == null)
                {
                    var r1Obj = new GameObject("CharmRow1", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    r1Obj.transform.SetParent(charmContainer, false);
                    charmRow1 = r1Obj.GetComponent<RectTransform>();
                    charmRow1.anchorMin = new Vector2(0.5f, 0.5f);
                    charmRow1.anchorMax = new Vector2(0.5f, 0.5f);
                    charmRow1.pivot = new Vector2(0.5f, 0.5f);
                    charmRow1.anchoredPosition = new Vector2(0f, 0f);
                    charmRow1.sizeDelta = new Vector2(76f, charmCircleSize);

                    var hlg = r1Obj.GetComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 2f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }
            }

            if (charmRow2 == null)
            {
                var r2Transform = charmContainer.Find("CharmRow2");
                if (r2Transform != null) charmRow2 = r2Transform.GetComponent<RectTransform>();
                if (charmRow2 == null)
                {
                    var r2Obj = new GameObject("CharmRow2", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    r2Obj.transform.SetParent(charmContainer, false);
                    charmRow2 = r2Obj.GetComponent<RectTransform>();
                    charmRow2.anchorMin = new Vector2(0.5f, 0.5f);
                    charmRow2.anchorMax = new Vector2(0.5f, 0.5f);
                    charmRow2.pivot = new Vector2(0.5f, 0.5f);
                    charmRow2.anchoredPosition = new Vector2(0f, charmCircleSize * 0.95f);
                    charmRow2.sizeDelta = new Vector2(76f, charmCircleSize);

                    var hlg = r2Obj.GetComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 2f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }
            }

            if (charmCircleSlots == null) charmCircleSlots = new List<GameObject>();
            if (charmIconImages == null) charmIconImages = new List<Image>();

            // 6개의 부적 원형 슬롯 생성 (0,1,2는 Row1, 3,4,5는 Row2)
            while (charmCircleSlots.Count < 6)
            {
                int index = charmCircleSlots.Count;
                RectTransform parentRow = (index < 3) ? charmRow1 : charmRow2;
                string slotName = $"CharmCircle_{index}";

                Transform slotTransform = parentRow.Find(slotName);
                GameObject slotObj = slotTransform != null ? slotTransform.gameObject : null;
                Image iconImg = null;

                if (slotObj == null)
                {
                    slotObj = new GameObject(slotName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    slotObj.transform.SetParent(parentRow, false);

                    var rt = slotObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(charmCircleSize, charmCircleSize);

                    var bgImg = slotObj.GetComponent<Image>();
                    bgImg.raycastTarget = false;
                    bgImg.sprite = charmCircleSprite;
                    bgImg.color = Color.white;

                    // 내부 부적 아이콘 Image
                    var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObj.transform.SetParent(slotObj.transform, false);

                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRt.pivot = new Vector2(0.5f, 0.5f);
                    iconRt.sizeDelta = new Vector2(charmCircleSize * 0.72f, charmCircleSize * 0.72f);
                    iconRt.anchoredPosition = Vector2.zero;

                    iconImg = iconObj.GetComponent<Image>();
                    iconImg.raycastTarget = false;
                    iconImg.preserveAspect = true;
                }
                else
                {
                    var iconChild = slotObj.transform.Find("Icon");
                    if (iconChild != null) iconImg = iconChild.GetComponent<Image>();
                }

                charmCircleSlots.Add(slotObj);
                charmIconImages.Add(iconImg);
            }
        }

        /// <summary>
        /// 벤치 슬롯 시각적 요소(테두리 색상, 별 개수, 부적 아이콘, 3D 모델 프리뷰)를 갱신합니다.
        /// </summary>
        /// <param name="tier">펭귄 합성 등급 (1~5, 0이면 비어있음)</param>
        /// <param name="activeCharms">해당 슬롯에 영향을 미치는 활성 부적 목록</param>
        /// <param name="isLocked">슬롯 잠김 여부</param>
        /// <param name="isDisabled">비활성화 상태 여부</param>
        /// <param name="penguinController">슬롯에 배치된 펭귄 컨트롤러 (null이면 프리뷰 해제)</param>
        public void UpdateVisuals(int tier, IReadOnlyList<ShopItemBase> activeCharms, bool isLocked, bool isDisabled, PenguinPinball.Core.PenguinController penguinController = null, PenguinDefinition penguinDefinition = null)
        {
            EnsureHierarchy();

            // 1. 테두리 색상 (레벨 1부터 회색, 연두, 하늘, 노랑, 빨강)
            UpdateBorderColor(tier, isLocked, isDisabled);

            // 2. 하단 별 개수 (평귄 레벨에 따라 별의 갯수가 늘어남)
            UpdateStars(tier, isLocked);

            // 3. 네모 상단 활성화 부적 동그라미 (최대 6개)
            // 슬롯이 잠긴 경우에는 부적 아이콘을 숨기고, 비어있거나 펭귄이 있는 일반 벤치 슬롯은 활성 부적 표시
            if (isLocked)
            {
                UpdateCharms(null);
            }
            else
            {
                UpdateCharms(activeCharms);
            }

            // 4. 슬롯 중앙 3D 모델 프리뷰 (테두리 프레임 뒤로 렌더링)
            Update3DModel(penguinController, penguinDefinition, isLocked);
        }

        private void Update3DModel(PenguinPinball.Core.PenguinController penguinController, PenguinDefinition penguinDefinition, bool isLocked)
        {
            if (preview3D == null) return;

            if (isLocked || (penguinController == null && penguinDefinition == null))
            {
                preview3D.ClearModel();
                if (modelPreviewRawImage != null)
                {
                    modelPreviewRawImage.gameObject.SetActive(false);
                }
            }
            else if (penguinController != null)
            {
                preview3D.SetModel(penguinController);
            }
            else if (penguinDefinition != null)
            {
                preview3D.SetModel(penguinDefinition);
            }
        }

        private void UpdateBorderColor(int tier, bool isLocked, bool isDisabled)
        {
            if (borderImage == null) return;

            if (isLocked)
            {
                borderImage.color = lockedBorderColor;
            }
            else if (tier <= 0)
            {
                borderImage.color = defaultBorderColor;
            }
            else
            {
                Color targetColor;
                switch (tier)
                {
                    case 1:
                        targetColor = level1Color; // 회색
                        break;
                    case 2:
                        targetColor = level2Color; // 연두
                        break;
                    case 3:
                        targetColor = level3Color; // 하늘
                        break;
                    case 4:
                        targetColor = level4Color; // 노랑
                        break;
                    case 5:
                    default:
                        targetColor = level5Color; // 빨강 (최대 등급)
                        break;
                }

                if (isDisabled)
                {
                    // 비활성화 상태(발사 중 등)일 때 약간의 반투명 또는 명도 조절
                    targetColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.6f);
                }

                borderImage.color = targetColor;
            }
        }

        private void UpdateStars(int tier, bool isLocked)
        {
            if (starContainer == null) return;

            if (isLocked || tier <= 0)
            {
                starContainer.gameObject.SetActive(false);
                for (int i = 0; i < starImages.Count; i++)
                {
                    if (starImages[i] != null) starImages[i].gameObject.SetActive(false);
                }
                return;
            }

            starContainer.gameObject.SetActive(true);
            int clampedTier = Mathf.Clamp(tier, 1, 5);

            for (int i = 0; i < starImages.Count; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(i < clampedTier);
                }
            }
        }

        private void UpdateCharms(IReadOnlyList<ShopItemBase> activeCharms)
        {
            if (charmContainer == null) return;

            int count = (activeCharms != null) ? Mathf.Min(activeCharms.Count, 6) : 0;
            if (count == 0)
            {
                charmContainer.gameObject.SetActive(false);
                for (int i = 0; i < charmCircleSlots.Count; i++)
                {
                    if (charmCircleSlots[i] != null) charmCircleSlots[i].SetActive(false);
                }
                return;
            }

            charmContainer.gameObject.SetActive(true);

            // 예시 디자인:
            // 3개 이하일 때: 1열만 상단선 바로 위에 중앙 정렬 배치 (시안의 '부적 2개 활성화' 형태)
            // 4개 이상일 때: 1열(상단선 걸침), 2열(그 위) 2행 배치 (시안의 '부적 6개 활성화' 형태)
            if (count <= 3)
            {
                if (charmRow1 != null)
                {
                    charmRow1.gameObject.SetActive(true);
                    charmRow1.anchoredPosition = new Vector2(0f, 0f);
                }
                if (charmRow2 != null)
                {
                    charmRow2.gameObject.SetActive(false);
                }

                for (int i = 0; i < 3; i++)
                {
                    bool active = (i < count);
                    SetCharmSlot(i, active, active ? activeCharms[i] : null);
                }
                for (int i = 3; i < 6; i++)
                {
                    SetCharmSlot(i, false, null);
                }
            }
            else
            {
                // 4~6개 활성화: 아랫줄(Row1)은 상단 테두리 선(Y=0), 윗줄(Row2)은 바로 위(Y=+charmCircleSize * 0.95f)
                if (charmRow1 != null)
                {
                    charmRow1.gameObject.SetActive(true);
                    charmRow1.anchoredPosition = new Vector2(0f, 0f);
                }
                if (charmRow2 != null)
                {
                    charmRow2.gameObject.SetActive(true);
                    charmRow2.anchoredPosition = new Vector2(0f, charmCircleSize * 0.95f);
                }

                // Row 1 (하단 3개): 슬롯 0, 1, 2
                for (int i = 0; i < 3; i++)
                {
                    bool active = (i < count);
                    SetCharmSlot(i, active, active ? activeCharms[i] : null);
                }

                // Row 2 (상단 3개): 슬롯 3, 4, 5
                for (int i = 3; i < 6; i++)
                {
                    bool active = (i < count);
                    SetCharmSlot(i, active, active ? activeCharms[i] : null);
                }
            }
        }

        public Sprite GetPlaceholderCharmSprite(int index, ShopItemBase charm)
        {
            if (placeholderCharmSprites != null && placeholderCharmSprites.Count > 0)
            {
                int iconIndex = Mathf.Abs(index) % placeholderCharmSprites.Count;
                if (placeholderCharmSprites[iconIndex] != null)
                {
                    return placeholderCharmSprites[iconIndex];
                }
            }

            return defaultCharmIconSprite;
        }

        private void SetCharmSlot(int index, bool active, ShopItemBase charm)
        {
            if (index < 0 || index >= charmCircleSlots.Count) return;

            var slot = charmCircleSlots[index];
            if (slot != null) slot.SetActive(active);

            if (active && index < charmIconImages.Count && charmIconImages[index] != null)
            {
                var iconImg = charmIconImages[index];
                Sprite targetSprite = null;

                if (charm != null && charm.Icon != null)
                {
                    targetSprite = charm.Icon;
                }
                else
                {
                    // 부적 이미지가 없을 경우 대체 임시 이미지(Placeholder Sprite) 제공
                    targetSprite = GetPlaceholderCharmSprite(index, charm);
                }

                if (targetSprite != null)
                {
                    iconImg.sprite = targetSprite;
                    iconImg.enabled = true;
                }
                else
                {
                    iconImg.enabled = false;
                }
            }
        }
    }
}
