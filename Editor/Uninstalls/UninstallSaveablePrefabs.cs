using GameCreator.Runtime.Common;
using GameCreator.Runtime.SaveablePrefabs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

namespace GameCreator.Editor.Installs
{
    public static class UninstallSaveablePrefabs
    {
        static string ModuleName = "Saveable Prefabs";

        [MenuItem(itemName: "Game Creator/Uninstall/Saveable Prefabs",
                  isValidateFunction: false,
                  priority: UninstallManager.PRIORITY
                 )]
        static void Uninstall()
        {
            UninstallManager.EventBeforeUninstall -= WillUninstall;
            UninstallManager.EventBeforeUninstall += WillUninstall;
            UninstallManager.Uninstall(ModuleName);
        }

        static void WillUninstall(string name)
        {
            UninstallManager.EventBeforeUninstall -= WillUninstall;
            if (name != ModuleName) return;

            RemoveGuidComponents();
            RemoveRepository();
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
                if (prefab.GetComponent<PrefabGuid>() is PrefabGuid prefabGuid)
                {
                    Object.Destroy(prefabGuid);
                }
            }
        }
    }
}
