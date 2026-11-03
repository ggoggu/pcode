using System;
using System.Collections.Generic;
using PenguinPinball.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinPinball.UI.Views
{
    /// <summary>
    /// UI 상에서 두 점(RectTransform)을 잇는 애니메이션 점선 컴포넌트
    /// </summary>
    public class UIDottedLine : MonoBehaviour
    {
        [SerializeField] private Color dotColor = new Color(1f, 0.88f, 0.25f, 1f);
        [SerializeField] private float dotSize = 9f;
        [SerializeField] private float dotSpacing = 18f;

        private readonly List<RectTransform> activeDots = new List<RectTransform>();
        private readonly List<RectTransform> dotPool = new List<RectTransform>();

        private RectTransform startTarget;
        private RectTransform endTarget;
        private Canvas parentCanvas;
        private Sprite circleSprite;

        public void Initialize(Canvas canvas)
        {
            parentCanvas = canvas;
            CreateCircleSprite();
        }

        private void CreateCircleSprite()
        {
            if (circleSprite != null) return;
            // 부드러운 원형 스프라이트 생성
            int res = 32;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float radius = res * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        }

        public void SetLine(RectTransform start, RectTransform end)
        {
            startTarget = start;
            endTarget = end;
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            UpdateLineGeometry();
        }

        public void Clear()
        {
            startTarget = null;
            endTarget = null;
            foreach (var dot in activeDots)
            {
                dot.gameObject.SetActive(false);
                dotPool.Add(dot);
            }
            activeDots.Clear();
        }

        private void LateUpdate()
        {
            if (startTarget != null && endTarget != null)
            {
                UpdateLineGeometry();
            }
        }

        private void UpdateLineGeometry()
        {
            if (startTarget == null || endTarget == null) return;
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransform canvasRect = parentCanvas.transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, startTarget.position), cam, out Vector2 p1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(cam, endTarget.position), cam, out Vector2 p2);

            float distance = Vector2.Distance(p1, p2);
            if (distance < 2f)
            {
                ClearDots();
                return;
            }

            Vector2 dir = (p2 - p1).normalized;
            int neededDots = Mathf.Max(2, Mathf.FloorToInt(distance / dotSpacing));

            while (activeDots.Count < neededDots)
            {
                RectTransform dot = GetOrCreateDot();
                activeDots.Add(dot);
            }
            while (activeDots.Count > neededDots)
            {
                int last = activeDots.Count - 1;
                activeDots[last].gameObject.SetActive(false);
                dotPool.Add(activeDots[last]);
                activeDots.RemoveAt(last);
            }

            // 부드러운 흐르는 점선 애니메이션
            float animOffset = (Time.unscaledTime * 35f) % dotSpacing;

            for (int i = 0; i < activeDots.Count; i++)
            {
                float t = ((i * dotSpacing) + animOffset);
                if (t > distance) t -= distance;
                Vector2 pos = p1 + dir * t;
                activeDots[i].localPosition = pos;
                activeDots[i].gameObject.SetActive(true);
            }
        }

        private void ClearDots()
        {
            foreach (var dot in activeDots)
            {
                dot.gameObject.SetActive(false);
                dotPool.Add(dot);
            }
            activeDots.Clear();
        }

        private RectTransform GetOrCreateDot()
        {
            if (dotPool.Count > 0)
            {
                int lastIdx = dotPool.Count - 1;
                var dot = dotPool[lastIdx];
                dotPool.RemoveAt(lastIdx);
                dot.gameObject.SetActive(true);
                return dot;
            }

            var dotObj = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(transform, false);
            var rect = dotObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(dotSize, dotSize);

            var img = dotObj.GetComponent<Image>();
            img.color = dotColor;
            img.raycastTarget = false;
            if (circleSprite != null) img.sprite = circleSprite;

            return rect;
        }
    }

    /// <summary>
    /// 대상 선택형 즉시사용 아이템(등급 이식 등) 구매 시 점선 연결 및 확인 다이얼로그를 총괄하는 컨트롤러
    /// </summary>
    public class InstantItemSelectionController : MonoBehaviour
    {
        public static InstantItemSelectionController Instance { get; private set; }

        public bool IsActive { get; private set; } = false;
        public InstantItem CurrentItem { get; private set; }
        public int CurrentSlotIndex { get; private set; } = -1;
        public RectTransform ShopSlotRect { get; private set; }

        public readonly List<PenguinController> SelectedPenguins = new List<PenguinController>();
        public readonly List<RectTransform> SelectedSlotRects = new List<RectTransform>();

        // 이벤트
        public event Action<bool> OnSelectionStateChanged; // 활성화/비활성화 시 슬롯 색상 복원용

        [Header("UI References (자동 생성 지원)")]
        private Canvas mainCanvas;
        private GameObject overlayRoot;

        // 상단 안내 배너
        private GameObject topBannerObj;
        private TextMeshProUGUI topBannerText;

        // 점선 렌더러
        private UIDottedLine lineToPenguin1;
        private UIDottedLine lineToPenguin2;

        // 뒷배경 흐림 (Dimmed Backdrop) 및 확인 다이얼로그
        private GameObject dimmedBackdropObj;
        private CanvasGroup dimmedCanvasGroup;
        private GameObject confirmDialogObj;
        private TextMeshProUGUI confirmTitleText;
        private TextMeshProUGUI confirmDescText;
        private Button confirmExchangeButton;
        private TextMeshProUGUI confirmExchangeButtonText;
        private Button cancelExchangeButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            mainCanvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            BuildUI();
            HideAll();
        }

        private void OnEnable()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnInstantTargetSelectionRequested -= HandleSelectionRequested;
                ShopManager.Instance.OnInstantTargetSelectionRequested += HandleSelectionRequested;
            }
        }

        private void OnDisable()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnInstantTargetSelectionRequested -= HandleSelectionRequested;
            }
            CancelSelection();
        }

        private void HandleSelectionRequested(InstantItem item, int slotIndex)
        {
            // 상점 슬롯 RectTransform 찾기
            RectTransform slotRect = null;
            var shopView = FindAnyObjectByType<ItemShopView>();
            if (shopView != null)
            {
                slotRect = shopView.GetItemSlotRect(slotIndex);
            }

            StartSelection(item, slotIndex, slotRect);
        }

        /// <summary>
        /// 대상 선택 모드 시작
        /// </summary>
        public void StartSelection(InstantItem item, int slotIndex, RectTransform shopSlotRect)
        {
            if (item != null && !item.CanUse(out string failReason))
            {
                Debug.LogWarning($"[InstantItemSelectionController] {item.ItemName} 대상 선택 진입 불가: {failReason}");
                return;
            }

            EnsureUI();

            IsActive = true;
            CurrentItem = item;
            CurrentSlotIndex = slotIndex;
            ShopSlotRect = shopSlotRect;
            SelectedPenguins.Clear();
            SelectedSlotRects.Clear();

            // 점선 초기화
            if (lineToPenguin1 != null) lineToPenguin1.Clear();
            if (lineToPenguin2 != null) lineToPenguin2.Clear();

            // 상단 안내 문구 표시
            if (item != null && item.TargetType == InstantTargetType.SinglePenguin)
            {
                ShowTopBanner(string.IsNullOrEmpty(item.InstructionText) ? "승급할 펭귄을 선택해 주세요" : item.InstructionText);
            }
            else
            {
                ShowTopBanner(item != null && !string.IsNullOrEmpty(item.InstructionText) ? item.InstructionText : "펭귄 두마리를 선택해 주세요");
            }

            // 뒷배경 흐림 및 확인창은 아직 숨김
            HideConfirmDialog();

            overlayRoot.SetActive(true);
            OnSelectionStateChanged?.Invoke(true);

            Debug.Log($"[InstantItemSelectionController] 대상 선택 모드 시작: {item.ItemName} (슬롯 {slotIndex})");
        }

        /// <summary>
        /// 펭귄 슬롯 클릭 시 호출
        /// </summary>
        public void SelectPenguin(PenguinController penguin, RectTransform penguinSlotRect)
        {
            if (!IsActive || penguin == null) return;

            if (penguin.IsInField)
            {
                ShowTopBanner("<color=#FF6666>출격 중인 펭귄은 선택할 수 없습니다.</color>");
                return;
            }

            // 이미 선택된 펭귄을 다시 클릭한 경우 -> 선택 해제
            if (SelectedPenguins.Contains(penguin))
            {
                int index = SelectedPenguins.IndexOf(penguin);
                SelectedPenguins.RemoveAt(index);
                if (index < SelectedSlotRects.Count) SelectedSlotRects.RemoveAt(index);

                UpdateVisualsAfterSelection();
                return;
            }

            // SinglePenguin 모드인 경우
            if (CurrentItem != null && CurrentItem.TargetType == InstantTargetType.SinglePenguin)
            {
                if (penguin.Tier >= 5)
                {
                    ShowTopBanner("<color=#FF6666>5성 펭귄은 더 이상 승급할 수 없습니다. (최대 5성)</color>");
                    return;
                }

                SelectedPenguins.Clear();
                SelectedSlotRects.Clear();
                SelectedPenguins.Add(penguin);
                SelectedSlotRects.Add(penguinSlotRect);

                UpdateVisualsAfterSelection();
                return;
            }

            // 신규 펭귄 추가 (최대 2마리) - TwoPenguins 모드
            if (SelectedPenguins.Count >= 2)
            {
                // 이미 2마리 선택되어 있으면 2번째 것을 교체
                SelectedPenguins[1] = penguin;
                if (SelectedSlotRects.Count >= 2) SelectedSlotRects[1] = penguinSlotRect;
                else SelectedSlotRects.Add(penguinSlotRect);
            }
            else
            {
                SelectedPenguins.Add(penguin);
                SelectedSlotRects.Add(penguinSlotRect);
            }

            UpdateVisualsAfterSelection();
        }

        private void UpdateVisualsAfterSelection()
        {
            if (CurrentItem != null && CurrentItem.TargetType == InstantTargetType.SinglePenguin)
            {
                if (SelectedPenguins.Count == 0)
                {
                    if (lineToPenguin1 != null) lineToPenguin1.Clear();
                    if (lineToPenguin2 != null) lineToPenguin2.Clear();
                    ShowTopBanner(string.IsNullOrEmpty(CurrentItem.InstructionText) ? "승급할 펭귄을 선택해 주세요" : CurrentItem.InstructionText);
                    HideConfirmDialog();
                }
                else if (SelectedPenguins.Count >= 1)
                {
                    if (ShopSlotRect != null && SelectedSlotRects.Count > 0 && SelectedSlotRects[0] != null)
                    {
                        lineToPenguin1.SetLine(ShopSlotRect, SelectedSlotRects[0]);
                    }
                    if (lineToPenguin2 != null) lineToPenguin2.Clear();

                    ShowTopBanner("승급 대상 선택 완료");
                    ShowConfirmDialog();
                }
                return;
            }

            if (SelectedPenguins.Count == 0)
            {
                // 0마리 선택됨
                if (lineToPenguin1 != null) lineToPenguin1.Clear();
                if (lineToPenguin2 != null) lineToPenguin2.Clear();
                ShowTopBanner(CurrentItem != null && !string.IsNullOrEmpty(CurrentItem.InstructionText) ? CurrentItem.InstructionText : "펭귄 두마리를 선택해 주세요");
                HideConfirmDialog();
            }
            else if (SelectedPenguins.Count == 1)
            {
                // 1마리 선택됨: ShopItem 위치에서 1번째 펭귄 위치로 점선 연결!
                if (ShopSlotRect != null && SelectedSlotRects.Count > 0 && SelectedSlotRects[0] != null)
                {
                    lineToPenguin1.SetLine(ShopSlotRect, SelectedSlotRects[0]);
                }
                if (lineToPenguin2 != null) lineToPenguin2.Clear();

                ShowTopBanner("교환할 두 번째 펭귄을 선택해 주세요");
                HideConfirmDialog();
            }
            else if (SelectedPenguins.Count == 2)
            {
                // 2마리 선택됨:
                // 1. 상점 -> 펭귄1 점선 유지
                if (ShopSlotRect != null && SelectedSlotRects.Count > 0 && SelectedSlotRects[0] != null)
                {
                    lineToPenguin1.SetLine(ShopSlotRect, SelectedSlotRects[0]);
                }
                // 2. 펭귄1 -> 펭귄2 점선 연결!
                if (SelectedSlotRects.Count > 1 && SelectedSlotRects[0] != null && SelectedSlotRects[1] != null)
                {
                    lineToPenguin2.SetLine(SelectedSlotRects[0], SelectedSlotRects[1]);
                }

                ShowTopBanner("교환 대상 선택 완료");

                // 3. 뒷배경이 흐려지며 확인 다이얼로그 표시!
                ShowConfirmDialog();
            }
        }

        private void ShowTopBanner(string message)
        {
            if (topBannerObj != null) topBannerObj.SetActive(true);
            if (topBannerText != null) topBannerText.text = message;
        }

        private void HideTopBanner()
        {
            if (topBannerObj != null) topBannerObj.SetActive(false);
        }

        private void ShowConfirmDialog()
        {
            if (dimmedBackdropObj != null) dimmedBackdropObj.SetActive(true);
            if (dimmedCanvasGroup != null)
            {
                dimmedCanvasGroup.alpha = 1f;
                dimmedCanvasGroup.interactable = true;
                dimmedCanvasGroup.blocksRaycasts = true;
            }
            if (confirmDialogObj != null) confirmDialogObj.SetActive(true);

            if (CurrentItem != null && CurrentItem.TargetType == InstantTargetType.SinglePenguin && SelectedPenguins.Count >= 1)
            {
                if (confirmTitleText != null)
                    confirmTitleText.text = "승급 하시겠습니까?";

                var p = SelectedPenguins[0];
                string name = p.Definition != null ? p.Definition.DisplayName : p.name;
                int targetTier = p.Tier + 1;

                bool willMerge = false;
                List<string> mergePredictions = new List<string>();
                if (PenguinDeckManager.Instance != null)
                {
                    willMerge = PenguinDeckManager.Instance.PredictMergesAfterPromotion(p, out mergePredictions);
                }

                string mergeNotice = willMerge
                    ? $"<color=#FFD700><b>[합성 감지]</b> {string.Join(", ", mergePredictions)}</color>"
                    : $"<color=#AAAAAA>※ 5성 초과는 불가합니다.</color>";

                if (confirmDescText != null)
                {
                    confirmDescText.text =
                        $"선택한 펭귄을 1성 영구 업그레이드합니다.\n\n" +
                        $"• <b>{name}</b>: Tier {p.Tier}  ➜  <color=#00FFAA>Tier {targetTier}</color>\n\n" +
                        $"{mergeNotice}";
                }

                if (confirmExchangeButtonText != null)
                {
                    int price = CurrentItem != null ? CurrentItem.Price : 15;
                    confirmExchangeButtonText.text = $"승급하기 ({price}G)";
                }
            }
            else if (SelectedPenguins.Count >= 2)
            {
                if (confirmTitleText != null)
                    confirmTitleText.text = "교환 하시겠습니까?";

                var p1 = SelectedPenguins[0];
                var p2 = SelectedPenguins[1];
                string name1 = p1.Definition != null ? p1.Definition.DisplayName : p1.name;
                string name2 = p2.Definition != null ? p2.Definition.DisplayName : p2.name;

                // 교환 후 합성 가능 여부 사전 확인/예측
                bool willMerge = false;
                List<string> mergePredictions = new List<string>();
                if (PenguinDeckManager.Instance != null)
                {
                    willMerge = PenguinDeckManager.Instance.PredictMergesAfterSwap(p1, p2, out mergePredictions);
                }

                string mergeNotice = willMerge
                    ? $"<color=#FFD700><b>[합성 감지]</b> {string.Join(", ", mergePredictions)}</color>"
                    : $"<color=#AAAAAA>※ 교환 후 즉시 합성되는 조합은 없습니다.</color>";

                if (confirmDescText != null)
                {
                    confirmDescText.text =
                        $"선택한 두 펭귄의 등급을 서로 맞교환합니다.\n\n" +
                        $"• <b>{name1}</b>: Tier {p1.Tier}  ➜  <color=#00FFAA>Tier {p2.Tier}</color>\n" +
                        $"• <b>{name2}</b>: Tier {p2.Tier}  ➜  <color=#00FFAA>Tier {p1.Tier}</color>\n\n" +
                        $"{mergeNotice}";
                }

                if (confirmExchangeButtonText != null)
                {
                    int price = CurrentItem != null ? CurrentItem.Price : 15;
                    confirmExchangeButtonText.text = $"교환하기 ({price}G)";
                }
            }
        }

        private void HideConfirmDialog()
        {
            if (dimmedCanvasGroup != null)
            {
                dimmedCanvasGroup.alpha = 0f;
                dimmedCanvasGroup.interactable = false;
                dimmedCanvasGroup.blocksRaycasts = false;
            }
            if (dimmedBackdropObj != null) dimmedBackdropObj.SetActive(false);
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);
        }

        /// <summary>
        /// 확인 버튼 클릭 시 실행
        /// </summary>
        public void ConfirmExchange()
        {
            if (!IsActive || CurrentItem == null) return;

            bool success = false;
            if (CurrentItem.TargetType == InstantTargetType.SinglePenguin && SelectedPenguins.Count >= 1)
            {
                success = ShopManager.Instance != null && ShopManager.Instance.CompleteTargetedInstantPurchase(
                    CurrentSlotIndex,
                    CurrentItem,
                    SelectedPenguins[0]
                );
            }
            else if (CurrentItem.TargetType == InstantTargetType.TwoPenguins && SelectedPenguins.Count >= 2)
            {
                success = ShopManager.Instance != null && ShopManager.Instance.CompleteTargetedInstantPurchase(
                    CurrentSlotIndex,
                    CurrentItem,
                    SelectedPenguins[0],
                    SelectedPenguins[1]
                );
            }

            if (success)
            {
                Debug.Log($"[InstantItemSelectionController] {CurrentItem.ItemName} 실행 완료!");
            }
            else
            {
                Debug.LogWarning($"[InstantItemSelectionController] {CurrentItem.ItemName} 실행 실패");
            }

            EndSelection();
        }

        /// <summary>
        /// 취소 버튼 클릭 시 실행
        /// </summary>
        public void CancelSelection()
        {
            if (!IsActive) return;

            if (ShopManager.Instance != null && CurrentItem != null && CurrentSlotIndex >= 0)
            {
                ShopManager.Instance.CancelTargetedInstantPurchase(CurrentSlotIndex);
            }

            EndSelection();
        }

        private void EndSelection()
        {
            IsActive = false;
            HideAll();
            OnSelectionStateChanged?.Invoke(false);

            SelectedPenguins.Clear();
            SelectedSlotRects.Clear();
            CurrentItem = null;
            CurrentSlotIndex = -1;
            ShopSlotRect = null;
        }

        private void HideAll()
        {
            if (lineToPenguin1 != null) lineToPenguin1.Clear();
            if (lineToPenguin2 != null) lineToPenguin2.Clear();
            HideTopBanner();
            HideConfirmDialog();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        #region Dynamic UI Generation
        private void EnsureUI()
        {
            if (overlayRoot == null || lineToPenguin1 == null)
            {
                BuildUI();
            }
        }

        private void BuildUI()
        {
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            }
            if (mainCanvas == null) return;

            // 1. Overlay Root
            overlayRoot = new GameObject("TargetSelectionOverlay", typeof(RectTransform));
            overlayRoot.transform.SetParent(mainCanvas.transform, false);
            var rootRect = overlayRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 2. 점선 컴포넌트 2개 (상점->펭귄1, 펭귄1->펭귄2)
            var lineObj1 = new GameObject("DottedLine_ShopToP1", typeof(RectTransform), typeof(UIDottedLine));
            lineObj1.transform.SetParent(overlayRoot.transform, false);
            lineToPenguin1 = lineObj1.GetComponent<UIDottedLine>();
            lineToPenguin1.Initialize(mainCanvas);

            var lineObj2 = new GameObject("DottedLine_P1ToP2", typeof(RectTransform), typeof(UIDottedLine));
            lineObj2.transform.SetParent(overlayRoot.transform, false);
            lineToPenguin2 = lineObj2.GetComponent<UIDottedLine>();
            lineToPenguin2.Initialize(mainCanvas);

            // 3. 상단 안내 배너 (Top Prompt Banner)
            topBannerObj = new GameObject("TopPromptBanner", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            topBannerObj.transform.SetParent(overlayRoot.transform, false);
            var tbRect = topBannerObj.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 1f);
            tbRect.anchorMax = new Vector2(0.5f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0f, -25f);
            tbRect.sizeDelta = new Vector2(420f, 52f);

            var tbImg = topBannerObj.GetComponent<Image>();
            tbImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            var textObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(topBannerObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 0f);
            textRect.offsetMax = new Vector2(-20f, 0f);

            topBannerText = textObj.GetComponent<TextMeshProUGUI>();
            topBannerText.text = "펭귄 두마리를 선택해 주세요";
            topBannerText.fontSize = 20f;
            topBannerText.fontStyle = FontStyles.Bold;
            topBannerText.alignment = TextAlignmentOptions.Center;
            topBannerText.color = new Color(1f, 0.88f, 0.35f, 1f);

            // 4. 뒷배경 흐림 (Dimmed Backdrop)
            dimmedBackdropObj = new GameObject("DimmedBackdrop", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            dimmedBackdropObj.transform.SetParent(overlayRoot.transform, false);
            var dimRect = dimmedBackdropObj.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            var dimImg = dimmedBackdropObj.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.7f); // 어두운 흐림 효과
            dimImg.raycastTarget = true; // 배경 터치 가로채기

            dimmedCanvasGroup = dimmedBackdropObj.GetComponent<CanvasGroup>();

            // 5. 중앙 확인 다이얼로그 (Confirm Modal)
            confirmDialogObj = new GameObject("ConfirmModal", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            confirmDialogObj.transform.SetParent(dimmedBackdropObj.transform, false);
            var diaRect = confirmDialogObj.GetComponent<RectTransform>();
            diaRect.anchorMin = new Vector2(0.5f, 0.5f);
            diaRect.anchorMax = new Vector2(0.5f, 0.5f);
            diaRect.pivot = new Vector2(0.5f, 0.5f);
            diaRect.sizeDelta = new Vector2(420f, 320f);

            var diaImg = confirmDialogObj.GetComponent<Image>();
            diaImg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            var vlg = confirmDialogObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 16f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 타이틀: "교환 하시겠습니까?"
            var tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            tObj.transform.SetParent(confirmDialogObj.transform, false);
            confirmTitleText = tObj.GetComponent<TextMeshProUGUI>();
            confirmTitleText.text = "교환 하시겠습니까?";
            confirmTitleText.fontSize = 24f;
            confirmTitleText.fontStyle = FontStyles.Bold;
            confirmTitleText.alignment = TextAlignmentOptions.Center;
            confirmTitleText.color = new Color(1f, 0.88f, 0.3f);
            tObj.GetComponent<LayoutElement>().preferredHeight = 35f;

            // 설명 및 프리뷰
            var dObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            dObj.transform.SetParent(confirmDialogObj.transform, false);
            confirmDescText = dObj.GetComponent<TextMeshProUGUI>();
            confirmDescText.fontSize = 16f;
            confirmDescText.alignment = TextAlignmentOptions.Center;
            confirmDescText.color = Color.white;
            dObj.GetComponent<LayoutElement>().preferredHeight = 140f;

            // 버튼 바
            var btnBar = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            btnBar.transform.SetParent(confirmDialogObj.transform, false);
            btnBar.GetComponent<LayoutElement>().preferredHeight = 52f;
            var hlg = btnBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // 취소 버튼
            var cancelBtnObj = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelBtnObj.transform.SetParent(btnBar.transform, false);
            cancelBtnObj.GetComponent<Image>().color = new Color(0.4f, 0.45f, 0.5f, 1f);
            cancelExchangeButton = cancelBtnObj.GetComponent<Button>();
            cancelExchangeButton.onClick.AddListener(CancelSelection);

            var cancelTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
            var cText = cancelTextObj.GetComponent<TextMeshProUGUI>();
            cText.text = "취소";
            cText.fontSize = 18f;
            cText.alignment = TextAlignmentOptions.Center;
            cText.color = Color.white;
            var cRect = cancelTextObj.GetComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            // 교환하기 버튼
            var confirmBtnObj = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmBtnObj.transform.SetParent(btnBar.transform, false);
            confirmBtnObj.GetComponent<Image>().color = new Color(0.15f, 0.7f, 0.35f, 1f);
            confirmExchangeButton = confirmBtnObj.GetComponent<Button>();
            confirmExchangeButton.onClick.AddListener(ConfirmExchange);

            var confirmBtnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            confirmBtnTextObj.transform.SetParent(confirmBtnObj.transform, false);
            confirmExchangeButtonText = confirmBtnTextObj.GetComponent<TextMeshProUGUI>();
            confirmExchangeButtonText.text = "교환하기";
            confirmExchangeButtonText.fontSize = 18f;
            confirmExchangeButtonText.fontStyle = FontStyles.Bold;
            confirmExchangeButtonText.alignment = TextAlignmentOptions.Center;
            confirmExchangeButtonText.color = Color.white;
            var cfRect = confirmBtnTextObj.GetComponent<RectTransform>();
            cfRect.anchorMin = Vector2.zero;
            cfRect.anchorMax = Vector2.one;
            cfRect.offsetMin = Vector2.zero;
            cfRect.offsetMax = Vector2.zero;
        }
        #endregion
    }
}
