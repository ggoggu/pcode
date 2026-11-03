using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace PenguinShot.Editor
{
    public static class SetupKoreanFont
    {
        [MenuItem("Tools/PenguinShot/Setup Korean Font")]
        public static void Execute()
        {
            string regularFontPath = "Assets/Fonts/Pretendard-Regular.otf";
            string boldFontPath = "Assets/Fonts/Pretendard-Bold.otf";

            AssetDatabase.ImportAsset(regularFontPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(boldFontPath, ImportAssetOptions.ForceUpdate);

            Font regularFont = AssetDatabase.LoadAssetAtPath<Font>(regularFontPath);
            if (regularFont == null)
            {
                Debug.LogError($"[SetupKoreanFont] Could not load font at {regularFontPath}");
                return;
            }

            string regularAssetPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
            TMP_FontAsset regularFontAsset = CreateOrLoadFontAsset(regularFont, regularAssetPath);
            if (regularFontAsset == null)
            {
                Debug.LogError("[SetupKoreanFont] Failed to create Pretendard-Regular SDF.");
                return;
            }

            // 1. Update LiberationSans SDF Fallback table
            string liberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(liberationPath);
            if (liberation != null)
            {
                if (liberation.fallbackFontAssetTable == null)
                {
                    liberation.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                liberation.fallbackFontAssetTable.RemoveAll(f => f == null || f.name.Contains("LiberationSans SDF - Fallback"));

                if (!liberation.fallbackFontAssetTable.Contains(regularFontAsset))
                {
                    liberation.fallbackFontAssetTable.Add(regularFontAsset);
                }
                EditorUtility.SetDirty(liberation);
                Debug.Log($"[SetupKoreanFont] Added {regularFontAsset.name} to LiberationSans SDF fallback table.");
            }

            // 2. Update TMP Settings global fallback list
            if (TMP_Settings.instance != null)
            {
                var fallbackList = TMP_Settings.fallbackFontAssets;
                if (fallbackList == null)
                {
                    fallbackList = new List<TMP_FontAsset>();
                    TMP_Settings.fallbackFontAssets = fallbackList;
                }

                fallbackList.RemoveAll(f => f == null || f.name.Contains("LiberationSans SDF - Fallback"));

                if (!fallbackList.Contains(regularFontAsset))
                {
                    fallbackList.Add(regularFontAsset);
                }
                EditorUtility.SetDirty(TMP_Settings.instance);
                Debug.Log($"[SetupKoreanFont] Added {regularFontAsset.name} to TMP Settings fallback list.");
            }

            // 3. Clean up the faulty LiberationSans SDF - Fallback.asset if it exists
            string brokenFallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
            if (File.Exists(brokenFallbackPath))
            {
                AssetDatabase.DeleteAsset(brokenFallbackPath);
                Debug.Log("[SetupKoreanFont] Removed broken LiberationSans SDF - Fallback.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetupKoreanFont] Korean font setup completed successfully!");
        }

        private static TMP_FontAsset CreateOrLoadFontAsset(Font font, string targetAssetPath)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
            if (existing != null)
            {
                return existing;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true
            );

            if (fontAsset == null)
            {
                return null;
            }

            AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }
    }
}
