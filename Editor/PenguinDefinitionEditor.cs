using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PenguinPinball.Core;

namespace PenguinPinball.Editor
{
    [CustomEditor(typeof(PenguinDefinition))]
    public class PenguinDefinitionEditor : UnityEditor.Editor
    {
        private VisualElement validationContainer;
        private VisualElement previewContainer;
        private VisualElement abilityContainer;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            // 1. 수치 유효성 검사 (HelpBox 영역)
            validationContainer = new VisualElement();
            validationContainer.style.marginBottom = 10;
            root.Add(validationContainer);

            // 2. 티어별 스탯 성장 미리보기 (Tier Growth Previewer)
            CreateHeader(root, "📊 Tier Growth Preview (Tier 1 ~ 5)");
            previewContainer = new VisualElement();
            previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            previewContainer.style.paddingTop = 6;
            previewContainer.style.paddingBottom = 6;
            previewContainer.style.paddingLeft = 8;
            previewContainer.style.paddingRight = 8;
            previewContainer.style.marginBottom = 10;
            SetBorderRadius(previewContainer, 5);
            SetBorder(previewContainer, 1, new Color(0.25f, 0.25f, 0.25f));
            root.Add(previewContainer);

            // 3. 기본 프로퍼티 그룹
            CreateHeader(root, "🐧 General & Visual");
            root.Add(CreatePropertyField("displayName", "몬스터/펭귄 이름"));
            root.Add(CreatePropertyField("visualPrefab", "비주얼 프리팹"));

            CreateHeader(root, "⚙️ Physics Settings");
            root.Add(CreatePropertyField("mass", "질량 (Mass)"));
            root.Add(CreatePropertyField("linearDrag", "선형 마찰력 (Linear Drag)"));
            root.Add(CreatePropertyField("maxSpeed", "최대 속도 (Max Speed)"));

            CreateHeader(root, "⚔️ Combat Settings");
            root.Add(CreatePropertyField("attack", "기본 공격력"));
            root.Add(CreatePropertyField("damageSpeedScale", "속도 비례 데미지 계수"));
            root.Add(CreatePropertyField("lowSpeedCutoff", "최저 속도 컷오프"));

            CreateHeader(root, "🔥 Combo & Overload");
            root.Add(CreatePropertyField("overloadThreshold", "과부화 필요 콤보 수"));
            root.Add(CreatePropertyField("comboBaseDuration", "콤보 기본 유지 시간"));
            root.Add(CreatePropertyField("durationLossPerCombo", "콤보당 감소 시간"));
            root.Add(CreatePropertyField("comboDurationMultiplier", "콤보 유지시간 배율"));
            root.Add(CreatePropertyField("overloadMaxSpeedMultiplier", "과부화 속도 배율"));
            root.Add(CreatePropertyField("overloadDragMultiplier", "과부화 마찰력 배율"));

            CreateHeader(root, "📈 Tier Growth & Respawn");
            root.Add(CreatePropertyField("respawnSeconds", "부활 소요 시간(초)"));
            root.Add(CreatePropertyField("attackGrowthPerTier", "티어당 공격력 성장률"));
            root.Add(CreatePropertyField("maxSpeedGrowthPerTier", "티어당 속도 성장률"));
            root.Add(CreatePropertyField("massGrowthPerTier", "티어당 질량 증가량"));
            root.Add(CreatePropertyField("respawnTimeGrowthPerTier", "티어당 부활시간 감소율"));

            // 4. Ability Configuration (Inline Inspector)
            CreateHeader(root, "⚡ Ability Configuration");
            SerializedProperty abilityProp = serializedObject.FindProperty("ability");
            PropertyField abilityField = new PropertyField(abilityProp, "할당된 능력 (Ability SO)");
            root.Add(abilityField);

            abilityContainer = new VisualElement();
            abilityContainer.style.marginTop = 8;
            abilityContainer.style.paddingTop = 8;
            abilityContainer.style.paddingBottom = 8;
            abilityContainer.style.paddingLeft = 8;
            abilityContainer.style.paddingRight = 8;
            abilityContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.6f);
            SetBorderRadius(abilityContainer, 5);
            SetBorder(abilityContainer, 1, new Color(0.3f, 0.3f, 0.3f));
            root.Add(abilityContainer);

            // 이벤트 바인딩: 프로퍼티 수치 변경 시 실시간으로 유효성 검사 및 표/Inline Inspector 갱신
            abilityField.RegisterValueChangeCallback(evt => RefreshAbilityInspector(abilityProp));
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt => RefreshAll());

            // 초기 로드 시 실행
            RefreshAll();
            RefreshAbilityInspector(abilityProp);

            return root;
        }

        private void RefreshAll()
        {
            serializedObject.Update();
            RefreshValidation();
            RefreshTierPreview();
        }

        #region Validation (HelpBox)
        private void RefreshValidation()
        {
            validationContainer.Clear();
            PenguinDefinition def = (PenguinDefinition)target;

            if (def == null) return;

            // 1. 최저 속도 컷오프가 최대 속도보다 높은 경우
            if (def.lowSpeedCutoff >= def.maxSpeed)
            {
                validationContainer.Add(new HelpBox("최저 속도 컷오프(lowSpeedCutoff)가 최대 속도(maxSpeed)보다 크거나 같습니다.", HelpBoxMessageType.Warning));
            }

            // 2. 부활 시간이 0 이하인 경우
            if (def.respawnSeconds <= 0f)
            {
                validationContainer.Add(new HelpBox("부활 시간(respawnSeconds)은 0보다 커야 합니다.", HelpBoxMessageType.Error));
            }

            // 3. 과부화 마찰력 배율이 음수인 경우
            if (def.overloadDragMultiplier < 0f)
            {
                validationContainer.Add(new HelpBox("과부화 마찰력 배율(overloadDragMultiplier)은 음수일 수 없습니다.", HelpBoxMessageType.Error));
            }

            // 4. 티어당 부활시간 감소율로 인해 5티어에서 부활시간이 0 이하가 되는지 검사
            float tier5Respawn = def.respawnSeconds * (1f - 4 * def.respawnTimeGrowthPerTier);
            if (tier5Respawn <= 0f)
            {
                validationContainer.Add(new HelpBox("티어당 부활시간 감소율이 높아 고티어(5티어)에서 부활시간이 0초 이하가 됩니다.", HelpBoxMessageType.Warning));
            }
        }
        #endregion

        #region Tier Growth Previewer
        private void RefreshTierPreview()
        {
            previewContainer.Clear();
            PenguinDefinition def = (PenguinDefinition)target;

            if (def == null) return;

            // 테이블 헤더 생성
            VisualElement headerRow = CreateTableRow(true);
            headerRow.Add(CreateTableCell("Tier", true));
            headerRow.Add(CreateTableCell("공격력", true));
            headerRow.Add(CreateTableCell("최대 속도", true));
            headerRow.Add(CreateTableCell("질량", true));
            headerRow.Add(CreateTableCell("부활 시간", true));
            previewContainer.Add(headerRow);

            // Tier 1 ~ 5 계산 행 생성
            for (int t = 1; t <= 5; t++)
            {
                int offset = t - 1;
                float atk = def.attack * (1f + offset * def.attackGrowthPerTier);
                float spd = def.maxSpeed * (1f + offset * def.maxSpeedGrowthPerTier);
                float mass = def.mass + (offset * def.massGrowthPerTier);
                float respawn = Mathf.Max(0.1f, def.respawnSeconds * (1f - offset * def.respawnTimeGrowthPerTier));

                VisualElement row = CreateTableRow(false);
                row.Add(CreateTableCell($"{t} Tier", false, true));
                row.Add(CreateTableCell($"{atk:F1}", false));
                row.Add(CreateTableCell($"{spd:F1}", false));
                row.Add(CreateTableCell($"{mass:F2}", false));
                row.Add(CreateTableCell($"{respawn:F2}s", false));
                previewContainer.Add(row);
            }
        }

        private VisualElement CreateTableRow(bool isHeader)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;

            if (isHeader)
            {
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
                row.style.marginBottom = 4;
            }

            return row;
        }

        private Label CreateTableCell(string text, bool isHeader, bool isBold = false)
        {
            Label cell = new Label(text);
            cell.style.flexGrow = 1;
            cell.style.width = Length.Percent(20);
            cell.style.unityTextAlign = TextAnchor.MiddleCenter;

            if (isHeader)
            {
                cell.style.unityFontStyleAndWeight = FontStyle.Bold;
                cell.style.color = new Color(0.8f, 0.8f, 0.8f);
            }
            else if (isBold)
            {
                cell.style.unityFontStyleAndWeight = FontStyle.Bold;
                cell.style.color = new Color(0.4f, 0.8f, 1.0f);
            }

            return cell;
        }
        #endregion

        #region Helpers
        private void RefreshAbilityInspector(SerializedProperty abilityProp)
        {
            abilityContainer.Clear();

            Object targetSO = abilityProp.objectReferenceValue;
            if (targetSO == null)
            {
                Label emptyLabel = new Label("할당된 PenguinAbilitySO가 없습니다.");
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                abilityContainer.Add(emptyLabel);
                return;
            }

            Label title = new Label($"⚡ {targetSO.name} (Inline Settings)");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            title.style.color = new Color(0.4f, 0.8f, 1.0f);
            abilityContainer.Add(title);

            SerializedObject abilitySO = new SerializedObject(targetSO);
            InspectorElement inlineInspector = new InspectorElement(abilitySO);
            abilityContainer.Add(inlineInspector);
        }

        private PropertyField CreatePropertyField(string propertyName, string label)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            return new PropertyField(prop, label);
        }

        private void CreateHeader(VisualElement root, string titleText)
        {
            Label header = new Label(titleText);
            header.style.fontSize = 13;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 12;
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