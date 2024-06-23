using GameCreator.Runtime.SaveablePrefabs;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GameCreator.Editor.Installs
{
    public static class UninstallSaveablePrefabs
    {
        const string ModuleName = "Saveable Prefabs";
        const string UninstallTitle = "Are you sure you want to uninstall Saveable Prefabs";

        const string UninstallMsg =
            "** MAKE SURE YOU HAVE A BACKUP **"
            + "\n\rThis operation cannot be undone. This will also remove all PrefabGuid components from your prefabs.";

        const string UninstallPrefabGuidTitle = "Do you want to remove the PrefabGuid from your prefabs?";

        const string UninstallPrefabGuidMsg =
            "This is a shorthand to save you from removing it manually. If you plan on reinstalling this package, click NO.";

        [MenuItem("Game Creator/Uninstall/Saveable Prefabs", false, UninstallManager.PRIORITY)]
        static void Uninstall()
        {
            if (PackageInfo.GetAllRegisteredPackages().Any(x => x.name == "com.legi.saveable_prefabs"))
            {
                if (EditorUtility.DisplayDialog(UninstallTitle, UninstallMsg, "Yes", "Cancel"))
                {
                    if (EditorUtility.DisplayDialog(UninstallPrefabGuidTitle, UninstallPrefabGuidMsg, "Yes", "Cancel"))
                    {
                        RemoveGuidComponents();
                    }
                    RemoveRepository();
                    RemovePackage();
                }
            }
            else
            {
                UninstallManager.EventBeforeUninstall -= WillUninstall;
                UninstallManager.EventBeforeUninstall += WillUninstall;
                UninstallManager.Uninstall(ModuleName);
            }
        }

        static void WillUninstall(string name)
        {
            UninstallManager.EventBeforeUninstall -= WillUninstall;
            if (name != ModuleName) return;

            if (EditorUtility.DisplayDialog(UninstallPrefabGuidTitle, UninstallPrefabGuidMsg, "Yes", "Cancel"))
            {
                RemoveGuidComponents();
            }
            RemoveRepository();
        }

        static void RemovePackage()
        {
            Client.Remove("com.legi.saveable_prefabs");
        }

        static void RemoveRepository()
        {
            var file = $"{SaveablePrefabsRepository.DIRECTORY_ASSETS}{SaveablePrefabsRepository.REPOSITORY_ID}.asset";
            AssetDatabase.MoveAssetToTrash(file);
        }

        static void RemoveGuidComponents()
        {
            var prefabs = SaveablePrefabsRepository.Get.Prefabs.GetAll();
            foreach (var prefab in prefabs)
            {
                if (prefab.GetComponent<PrefabGuid>() is { } prefabGuid)
                {
                    Object.DestroyImmediate(prefabGuid, true);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
