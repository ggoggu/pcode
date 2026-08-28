using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PenguinPinball.Core
{
    public class PenguinLaunchTester : MonoBehaviour
    {
        [Header("Target Ref")]
        [SerializeField] private PenguinLauncher launcher;

        [Header("Test Launch Options")]
        [Tooltip("발사 오프셋 각도 (-45 ~ 45도)")]
        [SerializeField] private float testAngle = 0f;

        [Tooltip("발사 속도")]
        [SerializeField] private float testSpeed = 12f;

        public PenguinLauncher Launcher => launcher;
        public float TestAngle => testAngle;
        public float TestSpeed => testSpeed;

        private void Reset()
        {
            if (launcher == null)
            {
                launcher = FindFirstObjectByType<PenguinLauncher>();
            }
        }

        public void ExecuteTestLaunch()
        {
            if (launcher == null)
            {
                Debug.LogError("[PenguinLaunchTester] PenguinLauncher 참조가 비어 있습니다.");
                return;
            }

            launcher.LaunchCustom(testAngle, testSpeed);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PenguinLaunchTester))]
    public class PenguinLaunchTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PenguinLaunchTester tester = (PenguinLaunchTester)target;

            EditorGUILayout.Space(10);

            // 플레이 모드에서만 버튼 활성화
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("테스트 발사 (Launch Test)", GUILayout.Height(35)))
            {
                tester.ExecuteTestLaunch();
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("테스트 발사 버튼은 플레이 모드에서만 작동합니다.", MessageType.Info);
            }
            else if (tester.Launcher != null && !tester.Launcher.HasSelectedPenguin)
            {
                EditorGUILayout.HelpBox("덱에서 펭귄을 선택해야 발사가 가능합니다.", MessageType.Warning);
            }
        }
    }
#endif
}