using UnityEngine;
using UnityEditor;
using PenguinPinball.Core;

namespace PenguinPinball.Editor
{
    public class PinballPhysicsDashboard : EditorWindow
    {
        private PinballPhysicsConfig currentConfig;
        private SerializedObject serializedConfig;
        private Vector2 scrollPosition;

        // 유니티 상단 메뉴에 대시보드 열기 버튼 추가
        [MenuItem("Tools/Penguin Pinball/Physics Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<PinballPhysicsDashboard>("Physics Dashboard");
        }

        private void OnEnable()
        {
            // 창이 열릴 때 프로젝트 내에 있는 PinballPhysicsConfig 에셋을 자동으로 찾기
            string[] guids = AssetDatabase.FindAssets("t:PinballPhysicsConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                currentConfig = AssetDatabase.LoadAssetAtPath<PinballPhysicsConfig>(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("🐧 핀볼 밸런싱 대시보드", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 1. Config 에셋 할당 필드
            EditorGUI.BeginChangeCheck();
            currentConfig = (PinballPhysicsConfig)EditorGUILayout.ObjectField(
                "설정 파일 (Config)",
                currentConfig,
                typeof(PinballPhysicsConfig),
                false
            );

            if (EditorGUI.EndChangeCheck() || serializedConfig == null || (currentConfig != null && serializedConfig.targetObject != currentConfig))
            {
                if (currentConfig != null)
                {
                    serializedConfig = new SerializedObject(currentConfig);
                }
            }

            // 에셋이 없을 경우 안내 문구 및 생성 버튼 표시
            if (currentConfig == null)
            {
                EditorGUILayout.HelpBox("할당된 물리 설정 파일(Config)이 없습니다.\n아래 버튼을 눌러 에셋을 생성하거나 할당해주세요.", MessageType.Warning);
                if (GUILayout.Button("새 Physics Config 생성", GUILayout.Height(30)))
                {
                    CreateNewConfig();
                }
                return;
            }

            GUILayout.Space(10);
            DrawUILine(Color.gray);
            GUILayout.Space(10);

            // 2. 수치 조절 UI 그리기 (스크롤 지원)
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            serializedConfig.Update();

            // Config에 있는 변수들을 순회하며 자동으로 그려줌 (Header, Range 등 속성 완벽 지원)
            SerializedProperty prop = serializedConfig.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script") continue; // C# 스크립트 참조 필드 숨기기

                EditorGUILayout.PropertyField(prop, true);
                GUILayout.Space(2);
            }

            serializedConfig.ApplyModifiedProperties();

            GUILayout.EndScrollView();
        }

        // 새로운 Config 에셋을 지정된 경로에 자동 생성하는 함수
        private void CreateNewConfig()
        {
            PinballPhysicsConfig newConfig = ScriptableObject.CreateInstance<PinballPhysicsConfig>();

            // Resources 폴더가 없다면 생성 (추후 코드로 로드하기 편하도록)
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string path = "Assets/Resources/PinballPhysicsConfig.asset";
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();

            currentConfig = newConfig;
            Debug.Log($"새 물리 설정 파일이 생성되었습니다: {path}");
        }

        // 시각적 구분을 위한 가로줄 그리기 유틸리티
        private void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}