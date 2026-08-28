using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PenguinPinball.Core;

namespace PenguinPinball.Editor
{
    public class PenguinMatrixWindow : EditorWindow
    {
        private ScrollView matrixScrollView;

        // 컬럼 너비 및 간격 규격화 (헤더 & 입력 칸 공통 사용)
        private const float COL_ASSET = 160f;
        private const float COL_ATK = 70f;
        private const float COL_SPD = 70f;
        private const float COL_MASS = 70f;
        private const float COL_DRAG = 70f;
        private const float COL_RESPAWN = 70f;
        private const float COL_COMBO = 75f;
        private const float COL_ABILITY = 160f;
        private const float COL_SELECT = 45f;
        private const float COL_GAP = 8f; // 컬럼 간 공통 우측 간격

        [MenuItem("Tools/Penguin Pinball/Penguin Matrix Editor")]
        public static void ShowWindow()
        {
            PenguinMatrixWindow wnd = GetWindow<PenguinMatrixWindow>();
            wnd.titleContent = new GUIContent("Penguin Matrix Editor", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            wnd.minSize = new Vector2(920, 400);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // 1. Toolbar
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.SpaceBetween;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginBottom = 10;

            Label title = new Label("🐧 Penguin Definition Data Matrix");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            Button refreshBtn = new Button(LoadDefinitions) { text = "🔄 새로고침" };
            refreshBtn.style.height = 24;
            refreshBtn.style.paddingLeft = 10;
            refreshBtn.style.paddingRight = 10;

            toolbar.Add(title);
            toolbar.Add(refreshBtn);
            root.Add(toolbar);

            // 2. ScrollView
            matrixScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            matrixScrollView.style.flexGrow = 1;
            root.Add(matrixScrollView);

            LoadDefinitions();
        }

        private void LoadDefinitions()
        {
            matrixScrollView.Clear();

            string[] guids = AssetDatabase.FindAssets("t:PenguinDefinition");
            List<PenguinDefinition> definitions = new List<PenguinDefinition>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PenguinDefinition def = AssetDatabase.LoadAssetAtPath<PenguinDefinition>(path);
                if (def != null) definitions.Add(def);
            }

            if (definitions.Count == 0)
            {
                Label emptyLabel = new Label("프로젝트 내에 생성된 PenguinDefinition 에셋이 없습니다.");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                matrixScrollView.Add(emptyLabel);
                return;
            }

            // 헤더 행 생성
            VisualElement headerRow = CreateRow(true);
            headerRow.Add(CreateHeaderCell("Asset Reference", COL_ASSET));
            headerRow.Add(CreateHeaderCell("공격력", COL_ATK));
            headerRow.Add(CreateHeaderCell("최대 속도", COL_SPD));
            headerRow.Add(CreateHeaderCell("질량(Mass)", COL_MASS));
            headerRow.Add(CreateHeaderCell("마찰력", COL_DRAG));
            headerRow.Add(CreateHeaderCell("부활(초)", COL_RESPAWN));
            headerRow.Add(CreateHeaderCell("과부화 콤보", COL_COMBO));
            headerRow.Add(CreateHeaderCell("Assigned Ability", COL_ABILITY));
            headerRow.Add(CreateHeaderCell("Select", COL_SELECT));
            matrixScrollView.Add(headerRow);

            // 데이터 행 생성
            foreach (var def in definitions)
            {
                matrixScrollView.Add(CreateDefinitionRow(def));
            }
        }

        private VisualElement CreateDefinitionRow(PenguinDefinition def)
        {
            SerializedObject serializedDef = new SerializedObject(def);
            VisualElement row = CreateRow(false);

            row.Add(SetupCell(new ObjectField { value = def, objectType = typeof(PenguinDefinition) }, COL_ASSET, true));
            row.Add(SetupCell(new FloatField { bindingPath = "attack" }, COL_ATK));
            row.Add(SetupCell(new FloatField { bindingPath = "maxSpeed" }, COL_SPD));
            row.Add(SetupCell(new FloatField { bindingPath = "mass" }, COL_MASS));
            row.Add(SetupCell(new FloatField { bindingPath = "linearDrag" }, COL_DRAG));
            row.Add(SetupCell(new FloatField { bindingPath = "respawnSeconds" }, COL_RESPAWN));
            row.Add(SetupCell(new IntegerField { bindingPath = "overloadThreshold" }, COL_COMBO));
            row.Add(SetupCell(new ObjectField { bindingPath = "ability", objectType = typeof(PenguinAbilitySO) }, COL_ABILITY));

            Button selectBtn = new Button(() => {
                Selection.activeObject = def;
                EditorGUIUtility.PingObject(def);
            })
            { text = "🔍" };
            selectBtn.style.height = 20;
            row.Add(SetupCell(selectBtn, COL_SELECT));

            row.Bind(serializedDef);
            return row;
        }

        // 셀 공통 규격 적용 헬퍼 (기본 margin 제거 및 너비/간격 통일)
        private T SetupCell<T>(T element, float width, bool disabled = false) where T : VisualElement
        {
            element.style.width = width;
            element.style.marginLeft = 0;
            element.style.marginRight = COL_GAP;
            element.style.marginTop = 0;
            element.style.marginBottom = 0;
            if (disabled) element.SetEnabled(false);
            return element;
        }

        private VisualElement CreateRow(bool isHeader)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.marginBottom = 3;

            row.style.backgroundColor = isHeader ? new Color(0.22f, 0.22f, 0.22f, 0.95f) : new Color(0.16f, 0.16f, 0.16f, 0.6f);

            row.style.borderTopLeftRadius = 4;
            row.style.borderTopRightRadius = 4;
            row.style.borderBottomLeftRadius = 4;
            row.style.borderBottomRightRadius = 4;

            if (isHeader)
            {
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            }

            return row;
        }

        private Label CreateHeaderCell(string title, float width)
        {
            Label label = new Label(title);
            label.style.width = width;
            label.style.marginLeft = 0;
            label.style.marginRight = COL_GAP;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter; // 헤더 중앙 정렬
            return label;
        }
    }
}