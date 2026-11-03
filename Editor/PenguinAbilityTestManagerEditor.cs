using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PenguinPinball.Core.Editor
{
    [CustomEditor(typeof(PenguinAbilityTestManager))]
    public class PenguinAbilityTestManagerEditor : UnityEditor.Editor
    {
        private PenguinAbilityTestManager manager;
        private string[] definitionNames;
        private int selectedIndex = -1;

        private void OnEnable()
        {
            manager = (PenguinAbilityTestManager)target;
            RefreshDefinitionsList();
        }

        private void RefreshDefinitionsList()
        {
            if (manager == null) return;

            string[] guids = AssetDatabase.FindAssets("t:PenguinDefinition");
            var defs = new List<PenguinDefinition>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<PenguinDefinition>(path);
                if (def != null && !defs.Contains(def))
                {
                    defs.Add(def);
                }
            }

            defs = defs.OrderBy(d => d.name).ToList();

            Undo.RecordObject(manager, "Refresh Definitions List");
            manager.RegisteredDefinitions.Clear();
            manager.RegisteredDefinitions.AddRange(defs);

            definitionNames = manager.RegisteredDefinitions
                .Select(d => $"{d.name} ({d.DisplayName})")
                .ToArray();

            UpdateSelectedIndex();
        }

        private void UpdateSelectedIndex()
        {
            if (manager.SelectedDefinition == null || manager.RegisteredDefinitions.Count == 0)
            {
                selectedIndex = -1;
                return;
            }

            selectedIndex = manager.RegisteredDefinitions.IndexOf(manager.SelectedDefinition);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            manager = (PenguinAbilityTestManager)target;

            EditorGUILayout.Space(15);
            DrawHeader("🎮 Penguin Ability Quick Control", new Color(0.2f, 0.6f, 1f));

            DrawGameStateSection();
            EditorGUILayout.Space(10);

            DrawPenguinSelectionSection();
            EditorGUILayout.Space(10);

            DrawLaunchSection();
            EditorGUILayout.Space(10);

            DrawOverloadComboSection();
            EditorGUILayout.Space(10);

            DrawTargetAndEnvironmentSection();
            EditorGUILayout.Space(10);

            DrawTimeScaleSection();
            EditorGUILayout.Space(10);

            DrawMonitoringSection();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawHeader(string title, Color color)
        {
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prevColor;
        }

        private void DrawGameStateSection()
        {
            EditorGUILayout.LabelField("0. 게임 상태 제어 (Game State)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            string stateName = GameManager.Instance != null ? GameManager.Instance.CurrentState.ToString() : "GameManager 없음";
            bool isWave = GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.WaveInProgress || GameManager.Instance.CurrentState == GameState.Wave);

            EditorGUILayout.LabelField($"현재 상태: {(isWave ? "🌊 WaveInProgress (전투 중 - 발사 가능)" : "🔧 " + stateName)}");

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = isWave ? new Color(0.3f, 0.9f, 0.4f) : Color.white;
            if (GUILayout.Button("🌊 Wave 시작 (전투 전환)", GUILayout.Height(30)))
            {
                manager.StartWaveState();
            }

            GUI.backgroundColor = !isWave ? new Color(1f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Button("🔧 Maintenance (정비 전환)", GUILayout.Height(30)))
            {
                manager.StartMaintenanceState();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawPenguinSelectionSection()
        {
            EditorGUILayout.LabelField("1. 펭귄 선택 및 스폰 (Penguin Setup)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (definitionNames != null && definitionNames.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup("Penguin Definition", selectedIndex, definitionNames);
                if (newIndex != selectedIndex && newIndex >= 0 && newIndex < manager.RegisteredDefinitions.Count)
                {
                    selectedIndex = newIndex;
                    manager.SelectedDefinition = manager.RegisteredDefinitions[selectedIndex];
                    EditorUtility.SetDirty(manager);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("등록된 PenguinDefinition이 없습니다.", MessageType.Warning);
            }

            if (GUILayout.Button("🔄 새로고침", GUILayout.Width(75)))
            {
                RefreshDefinitionsList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Tier 설정");
            for (int t = 1; t <= 3; t++)
            {
                GUI.backgroundColor = (manager.SelectedTier == t) ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
                if (GUILayout.Button($"Tier {t}"))
                {
                    manager.SelectedTier = t;
                    if (Application.isPlaying)
                    {
                        manager.SetPenguinTier(t);
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("🐧 펭귄 스폰 / 변경 (Spawn & Equip)", GUILayout.Height(30)))
            {
                manager.SpawnOrReplacePenguin();
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if (GUILayout.Button("↩️ 발사대로 회수 (Recall)", GUILayout.Height(30)))
            {
                manager.RecallPenguin();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 펭귄 스폰 및 테스트가 가능합니다.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLaunchSection()
        {
            EditorGUILayout.LabelField("2. 발사 제어 (Launch Control)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            manager.TestLaunchAngle = EditorGUILayout.Slider("Launch Angle (각도)", manager.TestLaunchAngle, -45f, 45f);
            manager.TestLaunchSpeed = EditorGUILayout.Slider("Launch Speed (속도)", manager.TestLaunchSpeed, 1f, 30f);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("🚀 테스트 발사 (Launch)", GUILayout.Height(32)))
            {
                manager.LaunchPenguin();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawOverloadComboSection()
        {
            EditorGUILayout.LabelField("3. 콤보 & 과부하 치트 (Overload Control)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+1 Combo"))
            {
                manager.AddCombo(1);
            }

            if (GUILayout.Button("+5 Combo"))
            {
                manager.AddCombo(5);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("⚡ Trigger Overload (즉시 과부하)"))
            {
                manager.TriggerOverload();
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Reset Combo"))
            {
                manager.ResetCombo();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetAndEnvironmentSection()
        {
            EditorGUILayout.LabelField("4. 타겟 & 환경 관리 (Target & Partner)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            // 더미 1 & 2 개별 및 전체 스폰 버튼
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.8f, 0.5f, 1f);
            if (GUILayout.Button("🎯 더미 1 스폰/리셋"))
            {
                manager.SpawnOrResetDummy1();
            }
            if (GUILayout.Button("🎯 더미 2 스폰/리셋"))
            {
                manager.SpawnOrResetDummy2();
            }
            if (GUILayout.Button("🎯 더미 1+2 동시"))
            {
                manager.SpawnOrResetAllDummies();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.9f);
            if (GUILayout.Button("🐧 고정 보조 펭귄 배치"))
            {
                manager.SpawnPartnerPenguin();
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("🧹 필드 전체 정리 (Clear All)"))
            {
                manager.ClearAllEntities();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawTimeScaleSection()
        {
            EditorGUILayout.LabelField("5. 시뮬레이션 속도 제어 (Time Scale)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("0.2x (Slow)"))
            {
                manager.SetTimeScale(0.2f);
            }

            if (GUILayout.Button("0.5x"))
            {
                manager.SetTimeScale(0.5f);
            }

            if (GUILayout.Button("1.0x (Normal)"))
            {
                manager.SetTimeScale(1.0f);
            }

            if (GUILayout.Button("2.0x (Fast)"))
            {
                manager.SetTimeScale(2.0f);
            }

            bool isPaused = Mathf.Approximately(Time.timeScale, 0f);
            GUI.backgroundColor = isPaused ? Color.yellow : Color.white;
            if (GUILayout.Button(isPaused ? "▶️ Resume" : "⏸️ Pause"))
            {
                manager.SetTimeScale(isPaused ? 1.0f : 0f);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawMonitoringSection()
        {
            EditorGUILayout.LabelField("6. 실시간 상태 모니터링 (Live Monitor)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 실시간 스탯 및 피격 상태가 표시됩니다.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            var penguin = manager.ActivePenguin;
            if (penguin != null)
            {
                EditorGUILayout.LabelField($"[활성 펭귄] {penguin.Definition?.DisplayName} (Tier {penguin.Tier})", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"능력: {penguin.Definition?.Ability?.AbilityName}");
                if (!string.IsNullOrEmpty(penguin.Definition?.Ability?.Description))
                {
                    EditorGUILayout.LabelField($"설명: {penguin.Definition.Ability.Description}", EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Combo: {penguin.Combo} / {penguin.OverloadThreshold} | Overloaded: {(penguin.IsOverloaded ? "⚡ TRUE" : "FALSE")}");
                EditorGUILayout.LabelField($"In Field: {penguin.IsInField} | Available: {penguin.IsAvailable}");

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("[스탯 & 물리]", EditorStyles.miniBoldLabel);
                DrawStatRow("Attack", penguin, StatType.Attack);
                DrawStatRow("Max Speed", penguin, StatType.MaxSpeed);
                DrawStatRow("Linear Drag", penguin, StatType.LinearDrag);
                DrawStatRow("Mass", penguin, StatType.Mass);
                DrawStatRow("Respawn Time", penguin, StatType.RespawnTime);

                if (penguin.Body != null)
                {
                    EditorGUILayout.LabelField($"Current Speed: {penguin.Body.linearVelocity.magnitude:F2} | Drag: {penguin.Body.linearDamping:F2}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("활성화된 펭귄이 없습니다.", MessageType.Info);
            }

            // 더미 1 모니터링
            if (manager.Dummy1 != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("[샌드백 더미 1]", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HP: {manager.Dummy1.CurrentHealth:F0} / {manager.Dummy1.MaxHealth:F0} | Hits: {manager.Dummy1.HitCount} | Last: {manager.Dummy1.LastDamageReceived:F1}");
                EditorGUILayout.LabelField($"Total Damage: {manager.Dummy1.TotalDamageReceived:F1} | DPS: {manager.Dummy1.DPS:F1}");
            }

            // 더미 2 모니터링
            if (manager.Dummy2 != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("[샌드백 더미 2]", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HP: {manager.Dummy2.CurrentHealth:F0} / {manager.Dummy2.MaxHealth:F0} | Hits: {manager.Dummy2.HitCount} | Last: {manager.Dummy2.LastDamageReceived:F1}");
                EditorGUILayout.LabelField($"Total Damage: {manager.Dummy2.TotalDamageReceived:F1} | DPS: {manager.Dummy2.DPS:F1}");
            }

            // 보조 펭귄 모니터링
            if (manager.PartnerPenguin != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"[보조 펭귄 (고정)] {manager.PartnerPenguin.Definition?.DisplayName}", EditorStyles.boldLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatRow(string label, PenguinController penguin, StatType type)
        {
            if (penguin.Stats.TryGetValue(type, out var stat))
            {
                EditorGUILayout.LabelField($" • {label}: {stat.Value:F2}");
            }
            else
            {
                EditorGUILayout.LabelField($" • {label}: N/A");
            }
        }
    }
}

