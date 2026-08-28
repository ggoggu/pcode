using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PenguinPinball.Core;

namespace PenguinPinball.Editor
{
    [CustomEditor(typeof(PenguinController))]
    public class PenguinControllerEditor : UnityEditor.Editor
    {
        private VisualElement validationContainer;
        private VisualElement statsPreviewContainer;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            // 1. Definition 누락 검사 (HelpBox)
            validationContainer = new VisualElement();
            validationContainer.style.marginBottom = 8;
            root.Add(validationContainer);

            // 2. 현재 티어 기준 실시간 연산 스탯 프리뷰 (Stats Preview)
            CreateHeader(root, "⚡ Live Tier Stats Preview");
            statsPreviewContainer = new VisualElement();
            statsPreviewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            statsPreviewContainer.style.paddingTop = 8;
            statsPreviewContainer.style.paddingBottom = 8;
            statsPreviewContainer.style.paddingLeft = 10;
            statsPreviewContainer.style.paddingRight = 10;
            statsPreviewContainer.style.marginBottom = 10;
            SetBorderRadius(statsPreviewContainer, 5);
            SetBorder(statsPreviewContainer, 1, new Color(0.25f, 0.25f, 0.25f));
            root.Add(statsPreviewContainer);

            // 3. Serialized Fields 배치
            CreateHeader(root, "🐧 Controller Settings");

            // ✨ [추가됨] Physics Config 필드 노출
            // (PenguinController에 선언한 변수명에 따라 "physicsConfig" 또는 "config"로 맞춰주세요)
            SerializedProperty configProp = serializedObject.FindProperty("physicsConfig") ?? serializedObject.FindProperty("config");
            if (configProp != null)
            {
                root.Add(new PropertyField(configProp, "Physics Config"));
            }

            SerializedProperty defProp = serializedObject.FindProperty("definition");
            SerializedProperty tierProp = serializedObject.FindProperty("tier");

            PropertyField defField = new PropertyField(defProp, "Penguin Definition");
            PropertyField visualRootField = new PropertyField(serializedObject.FindProperty("visualRoot"), "Visual Root");

            root.Add(defField);
            root.Add(visualRootField);

            // stuckCheckInterval 등의 필드가 PenguinController에 남아있을 때만 출력하도록 안전하게 처리
            SerializedProperty stuckCheckIntervalProp = serializedObject.FindProperty("stuckCheckInterval");
            if (stuckCheckIntervalProp != null)
            {
                CreateHeader(root, "⏱️ Stuck Detection Settings");
                root.Add(new PropertyField(stuckCheckIntervalProp, "검사 간격 (초)"));
                root.Add(new PropertyField(serializedObject.FindProperty("minMoveThreshold"), "최소 이동 거리 (m)"));
                root.Add(new PropertyField(serializedObject.FindProperty("launchGracePeriod"), "발사 유예 시간 (초)"));
            }

            CreateHeader(root, "📈 Tier Settings");
            PropertyField tierField = new PropertyField(tierProp, "현재 설정 티어 (Tier)");
            root.Add(tierField);

            // 수치 변경 시 실시간 프리뷰 표 갱신 바인딩
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt => RefreshAll());

            RefreshAll();

            return root;
        }

        private void RefreshAll()
        {
            serializedObject.Update();
            PenguinController controller = (PenguinController)target;

            RefreshValidation(controller);
            RefreshLiveStats(controller);
        }

        #region Validation
        private void RefreshValidation(PenguinController controller)
        {
            validationContainer.Clear();

            if (controller == null) return;

            if (controller.Definition == null)
            {
                validationContainer.Add(new HelpBox("Penguin Definition이 할당되지 않았습니다. 수치를 연산할 수 없습니다.", HelpBoxMessageType.Warning));
            }
        }
        #endregion

        #region Live Stats Preview
        private void RefreshLiveStats(PenguinController controller)
        {
            statsPreviewContainer.Clear();

            if (controller == null || controller.Definition == null)
            {
                Label emptyLabel = new Label("Definition을 할당하면 최종 스탯이 여기에 표시됩니다.");
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                statsPreviewContainer.Add(emptyLabel);
                return;
            }

            PenguinDefinition def = controller.Definition;
            int currentTier = controller.Tier;
            int tierOffset = Mathf.Max(0, currentTier - 1);

            // 최종 스탯 연산
            float finalAttack = def.attack * (1f + tierOffset * def.attackGrowthPerTier);
            float finalMaxSpeed = def.maxSpeed * (1f + tierOffset * def.maxSpeedGrowthPerTier);
            float finalMass = def.mass + (tierOffset * def.massGrowthPerTier);
            float finalRespawn = Mathf.Max(0.1f, def.respawnSeconds * (1f - tierOffset * def.respawnTimeGrowthPerTier));

            // 과부화 스탯 연산
            float overloadSpeed = finalMaxSpeed * def.overloadMaxSpeedMultiplier;
            float overloadDrag = def.linearDrag * def.overloadDragMultiplier;

            // 스탯 요약 카드 레이아웃
            Label title = new Label($"[{currentTier} Tier] {def.DisplayName} 최종 적용 스탯");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            title.style.color = new Color(0.4f, 0.8f, 1.0f);
            statsPreviewContainer.Add(title);

            VisualElement grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Column;

            grid.Add(CreateStatRow("⚔️ 공격력 (Attack)", $"{finalAttack:F1} (기본: {def.attack})"));
            grid.Add(CreateStatRow("🏃 최대 속도 (Max Speed)", $"{finalMaxSpeed:F1} m/s"));
            grid.Add(CreateStatRow("🔥 과부화 속도 (Overload Speed)", $"{overloadSpeed:F1} m/s", true));
            grid.Add(CreateStatRow("🏋️ 질량 (Mass)", $"{finalMass:F2} kg"));
            grid.Add(CreateStatRow("💧 마찰력 (Linear Drag)", $"{def.linearDrag:F2} (과부화: {overloadDrag:F2})"));
            grid.Add(CreateStatRow("⏳ 부활 시간 (Respawn Time)", $"{finalRespawn:F2} 초"));

            if (def.Ability != null)
            {
                grid.Add(CreateStatRow("⚡ 연동 능력 (Ability)", $"{def.Ability.AbilityName}"));
            }

            statsPreviewContainer.Add(grid);
        }

        private VisualElement CreateStatRow(string labelText, string valueText, bool isHighlight = false)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 1;
            row.style.marginBottom = 1;

            Label label = new Label(labelText);
            label.style.color = new Color(0.8f, 0.8f, 0.8f);

            Label value = new Label(valueText);
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.color = isHighlight ? new Color(1.0f, 0.6f, 0.2f) : Color.white;

            row.Add(label);
            row.Add(value);
            return row;
        }
        #endregion

        #region Scene Gizmos (OnSceneGUI)
        private void OnSceneGUI()
        {
            PenguinController controller = (PenguinController)target;
            if (controller == null) return;

            // SerializedObject에서 minMoveThreshold, stuckCheckInterval 가져오기
            SerializedProperty minMoveProp = serializedObject.FindProperty("minMoveThreshold");
            SerializedProperty intervalProp = serializedObject.FindProperty("stuckCheckInterval");

            if (minMoveProp == null || intervalProp == null) return;

            float minMoveThreshold = minMoveProp.floatValue;
            float stuckCheckInterval = intervalProp.floatValue;

            Vector3 pos = controller.transform.position;

            // 1. Scene View 상에 최소 이동 거리 기준 구체(Sphere) 와이어프레임 렌더링
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Handles.DrawWireDisc(pos, Vector3.up, minMoveThreshold); // 바닥 원형 가이드
            Handles.DrawWireDisc(pos, Vector3.forward, minMoveThreshold); // 정면 원형 가이드

            // 2. Scene View 상에 디버그 안내 텍스트 노출
            GUIStyle textStyle = new GUIStyle();
            textStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.fontSize = 11;

            Handles.Label(pos + Vector3.up * (minMoveThreshold + 0.3f),
                $"⚠️ Stuck Check Range\nThreshold: {minMoveThreshold}m / {stuckCheckInterval}s", textStyle);
        }
        #endregion

        #region UI Helpers
        private void CreateHeader(VisualElement root, string titleText)
        {
            Label header = new Label(titleText);
            header.style.fontSize = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 10;
            header.style.marginBottom = 4;
            header.style.paddingBottom = 2;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
            root.Add(header);
        }

        private void SetBorderRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;

            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }
        #endregion
    }
}