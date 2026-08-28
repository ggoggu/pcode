using UnityEditor;
using UnityEngine;

namespace PenguinPinball.Core.Editor
{
    [CustomEditor(typeof(OverloadTestController))]
    public class OverloadTestControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var testController = (OverloadTestController)target;
            var penguin = testController.Penguin;

            GUILayout.Space(15);

            EditorGUILayout.LabelField(
                "Overload Test",
                EditorStyles.boldLabel
            );

            EditorGUILayout.BeginVertical("box");

            if (penguin == null)
            {
                EditorGUILayout.HelpBox(
                    "PenguinController를 연결해주세요.",
                    MessageType.Warning
                );
            }
            else if (Application.isPlaying)
            {
                DrawCurrentState(penguin);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Play Mode에서 런타임 상태를 확인할 수 있습니다.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.LabelField(
                "Test Actions",
                EditorStyles.boldLabel
            );

            if (GUILayout.Button("Add Combo"))
            {
                testController.AddCombo();
            }

            if (GUILayout.Button("Trigger Overload"))
            {
                testController.AddComboUntilOverload();
            }

            if (GUILayout.Button("Reset Combo / End Overload"))
            {
                testController.ResetCombo();
            }

            if (GUILayout.Button("Print Debug Log"))
            {
                testController.PrintState();
            }

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawCurrentState(PenguinController penguin)
        {
            EditorGUILayout.LabelField(
                "Current State",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                "Combo",
                penguin.Combo.ToString()
            );

            EditorGUILayout.LabelField(
                "Overloaded",
                penguin.IsOverloaded ? "TRUE" : "FALSE"
            );

            EditorGUILayout.LabelField(
                "Available",
                penguin.IsAvailable ? "TRUE" : "FALSE"
            );

            EditorGUILayout.LabelField(
                "In Field",
                penguin.IsInField ? "TRUE" : "FALSE"
            );

            GUILayout.Space(8);

            EditorGUILayout.LabelField(
                "Stats",
                EditorStyles.boldLabel
            );

            DrawStat(penguin, StatType.Attack, "Attack");
            DrawStat(penguin, StatType.MaxSpeed, "Max Speed");
            DrawStat(penguin, StatType.LinearDrag, "Linear Drag");
            DrawStat(penguin, StatType.Mass, "Mass");
            DrawStat(penguin, StatType.RespawnTime, "Respawn Time");

            GUILayout.Space(8);

            EditorGUILayout.LabelField(
                "Applied Physics",
                EditorStyles.boldLabel
            );

            if (penguin.Body != null)
            {
                EditorGUILayout.LabelField(
                    "Rigidbody Mass",
                    penguin.Body.mass.ToString("F2")
                );

                EditorGUILayout.LabelField(
                    "Linear Damping",
                    penguin.Body.linearDamping.ToString("F2")
                );

                EditorGUILayout.LabelField(
                    "Current Speed",
                    penguin.Body.linearVelocity.magnitude.ToString("F2")
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Rigidbody가 아직 초기화되지 않았습니다.",
                    MessageType.Warning
                );
            }
        }

        private void DrawStat(
            PenguinController penguin,
            StatType type,
            string label)
        {
            if (penguin.Stats.TryGetValue(type, out Stat stat))
            {
                EditorGUILayout.LabelField(
                    label,
                    stat.Value.ToString("F2")
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    label,
                    "N/A"
                );
            }
        }
    }
}