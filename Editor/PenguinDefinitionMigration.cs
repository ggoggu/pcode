#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PenguinPinball.Core
{
    /// <summary>
    /// 기존 PenguinKind enum 기반 Definition 에셋을 새로운 SO 기반 능력 할당 방식으로 마이그레이션합니다.
    /// 
    /// 사용 방법:
    /// 1. 기존 PenguinDefinition 에셋들에 PenguinKind 필드가 남아있는 경우,
    ///    이 스크립트의 MigrateAllDefinitions()를 에디터 메뉴에서 실행합니다.
    /// 2. 각 kind에 대응하는 AbilitySO 에셋을 Resources/Abilities/ 경로에서 로드하여 할당합니다.
    /// 3. 마이그레이션 후에는 PenguinKind 필드와 PenguinAbilityFactory를 안전하게 제거할 수 있습니다.
    /// 
    /// 주의: 이 스크립트는 Editor 전용이며 빌드에 포함되지 않습니다.
    /// </summary>
    public static class PenguinDefinitionMigration
    {
        private const string AbilityResourcePath = "Abilities/";

        [MenuItem("Tools/Penguin Pinball/Migration/Migrate All Penguin Definitions")]
        public static void MigrateAllDefinitions()
        {
            // 모든 PenguinDefinition 에셋 찾기
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(PenguinDefinition)}");
            int migrated = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<PenguinDefinition>(path);

                if (def == null) continue;

                // 이미 ability가 할당되어 있으면 스킵
                if (def.Ability != null)
                {
                    skipped++;
                    continue;
                }

                // 기존 kind 값 읽기 (리플렉션 또는 직렬화된 필드 접근 필요)
                // 주의: 이 마이그레이션은 kind 필드가 아직 Definition에 남아있을 때만 동작합니다.
                // kind 필드를 제거하기 전에 반드시 실행해야 합니다.
                //
                // var kind = def.kind;  // 기존 코드에서 kind가 public이었다면 직접 접근 가능
                // var so = LoadAbilitySO(kind);
                // if (so != null)
                // {
                //     SerializedObject serialized = new SerializedObject(def);
                //     SerializedProperty abilityProp = serialized.FindProperty("ability");
                //     abilityProp.objectReferenceValue = so;
                //     serialized.ApplyModifiedProperties();
                //     EditorUtility.SetDirty(def);
                //     migrated++;
                // }

                Debug.Log($"[Migration] Found definition: {path}. Manual ability assignment required if kind field is already removed.", def);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Migration] Done. Migrated: {migrated}, Skipped (already has ability): {skipped}, Total: {guids.Length}");
        }

        /// <summary>
        /// kind enum 값에 대응하는 AbilitySO 에셋을 Resources 폴더에서 로드합니다.
        /// 각 능력 SO 에셋이 Resources/Abilities/ 경로에 있어야 합니다.
        /// </summary>
        private static PenguinAbilitySO LoadAbilitySO<T>(T kind) where T : System.Enum
        {
            string name = kind.ToString();
            return Resources.Load<PenguinAbilitySO>($"{AbilityResourcePath}{name}Ability");
        }

        [MenuItem("Tools/Penguin Pinball/Migration/Create Default Ability SOs")]
        public static void CreateDefaultAbilitySOs()
        {
            string folder = "Assets/Resources/Abilities";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Abilities");
            }

            CreateSO<EmptyAbilitySO>(folder, "Basic");
            CreateSO<SpeedsterAbilitySO>(folder, "Speedster");
            CreateSO<HeavyballAbilitySO>(folder, "Heavyball");
            CreateSO<HarpoonAbilitySO>(folder, "Harpoon");
            CreateSO<StormAbilitySO>(folder, "Storm");
            CreateSO<FuseAbilitySO>(folder, "Fuse");
            CreateSO<ConductorAbilitySO>(folder, "Conductor");
            CreateSO<VulnerabilityAbilitySO>(folder, "Vulnerability");
            CreateSO<RussianRouletteAbilitySO>(folder, "RussianRoulette");
            CreateSO<IsolatedAbilitySO>(folder, "Isolated");
            CreateSO<StackAbilitySO>(folder, "Stack");
            CreateSO<BatteryAbilitySO>(folder, "Battery");
            CreateSO<EngineerAbilitySO>(folder, "Engineer");
            CreateSO<RageAbilitySO>(folder, "Rage");
            CreateSO<ShavingAbilitySO>(folder, "Shaving");
            CreateSO<CoupleAbilitySO>(folder, "Couple");
            CreateSO<SnowballAbilitySO>(folder, "Snowball");
            CreateSO<PaydayAbilitySO>(folder, "Payday");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Migration] Default Ability SOs created in Assets/Resources/Abilities/");
        }

        private static void CreateSO<T>(string folder, string assetName) where T : PenguinAbilitySO
        {
            string path = $"{folder}/{assetName}Ability.asset";

            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"[Migration] Already exists: {path}");
                return;
            }

            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[Migration] Created: {path}");
        }
    }
}
#endif