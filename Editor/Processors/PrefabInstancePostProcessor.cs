using GameCreator.Editor.Common;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.SaveablePrefabs;
using UnityEditor;
using UnityEngine;

namespace GameCreator.Editor.SaveablePrefabs
{
    public class PrefabInstancePostProcessor : AssetPostprocessor
    {
        static SaveablePrefabsSettings _settings;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Length == 0 && deletedAssets.Length == 0) return;
            RefreshPrefabs();
        }

        // PROCESSORS: ----------------------------------------------------------------------------

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            SettingsWindow.InitRunners.Add(new InitRunner(SettingsWindow.INIT_PRIORITY_LOW,
                                                          CanRefreshPrefabs,
                                                          RefreshPrefabs
                                                         ));
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        static bool CanRefreshPrefabs()
        {
            return true;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        static void RefreshPrefabs()
        {
            if (_settings == null)
            {
                var result = AssetDatabase.FindAssets($"t:{typeof(SaveablePrefabsSettings).FullName}");

                if (result.Length == 0) return;

                var assetPath = AssetDatabase.GUIDToAssetPath(result[0]);
                _settings = AssetDatabase.LoadAssetAtPath<SaveablePrefabsSettings>(assetPath);
            }
            RunPrefabSearch();
        }

        static void RunPrefabSearch()
        {
            if (AssemblyUtils.IsReloading) return;

            var guids = AssetDatabase.FindAssets("t:Prefab");
            var settingsSerializedObject = new SerializedObject(_settings);

            var prefabsProperty = settingsSerializedObject.FindProperty(TAssetRepositoryEditor.NAMEOF_MEMBER)
                .FindPropertyRelative("_catalogue").FindPropertyRelative("_prefabs");
            prefabsProperty.arraySize = guids.Length;

            var index = 0;
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(assetPath)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab.GetComponent<PrefabGuid>() is { } prefabGuid)
                {
                    if (prefabGuid.Guid.Get.String != guid)
                    {
                        prefabGuid.Guid.Set = new IdString(guid);
                    }
                    prefabsProperty.GetArrayElementAtIndex(index++).objectReferenceValue = prefab;
                }
            }
            prefabsProperty.arraySize = index;

            settingsSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
