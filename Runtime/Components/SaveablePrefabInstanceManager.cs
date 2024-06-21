using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using IGameSave = GameCreator.Runtime.Common.IGameSave;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [DefaultExecutionOrder(ApplicationManager.EXECUTION_ORDER_FIRST_EARLIER)]
    public class SaveablePrefabInstanceManager : Singleton<SaveablePrefabInstanceManager>, IGameSave
    {
        InstanceMetadataList _instances;

        void OnDestroy()
        {
            Item.EventInstantiate -= OnInstantiateItem;
            SaveLoadManager.Unsubscribe(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void OnSubsystemsInit()
        {
            Instance.WakeUp();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _instances = new InstanceMetadataList();
            Item.EventInstantiate += OnInstantiateItem;
            _ = SaveLoadManager.Subscribe(this);
        }

        #region Instantiation

        void OnInstantiateItem()
        {
            var item = Item.LastItemInstantiated;
            var gameObjectInstance = Item.LastItemInstanceInstantiated;
            var saveIdMaps = GetSaveIdMaps(gameObjectInstance, typeof(Remember), typeof(TLocalVariables));
            var metadata = new ItemPrefabInstanceMetadata(item.Item, gameObjectInstance, saveIdMaps);

            _instances.Add(metadata);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var wasActive = prefab.activeSelf;
            prefab.SetActive(false);
            var instance = Instantiate(prefab, position, rotation, parent);

            var saveIdMaps = GetSaveIdMaps(instance, typeof(Remember), typeof(TLocalVariables));
            var metadata = new PrefabInstanceMetadata(instance, saveIdMaps);

            _instances.Add(metadata);

            instance.SetActive(true);
            prefab.SetActive(wasActive);
            return instance;
        }

        #endregion

        #region IGameSave Implementation

        public string SaveID => "saveable-prefab-system";
        public bool IsShared => false;
        public Type SaveType => typeof(List<PrefabInstanceMetadata>);

        public object GetSaveData(bool includeNonSavable)
        {
            _instances.UpdateInstances();
            return _instances;
        }

        public LoadMode LoadMode => LoadMode.Greedy;

        public Task OnLoad(object value)
        {
            if (value is InstanceMetadataList list)
            {
                _instances = list;
                RespawnSavedPrefabInstances();
            }

            return Task.FromResult(true);
        }

        #endregion

        #region Respawn

        void RespawnSavedPrefabInstances()
        {
            foreach (var metadata in _instances.List)
            {
                if (metadata.ScenePath != SceneManager.GetActiveScene().path) continue;
                GameObject prefab = null;

                switch (metadata)
                {
                    case PrefabInstanceMetadata prefabInstanceMetadata
                        when !SaveablePrefabsRepository.Get.Prefabs.TryGet(prefabInstanceMetadata.Guid, out prefab):
                        continue;
                    case ItemPrefabInstanceMetadata itemPrefabInstanceMetadata:
                    {
                        var item = InventoryRepository.Get.Items.Get(itemPrefabInstanceMetadata.Guid.Get);
                        if (item?.HasPrefab == false) continue;
                        var fieldInfo = typeof(Item).GetField("m_Prefab",
                                                              BindingFlags.Instance
                                                              | BindingFlags.NonPublic
                                                              | BindingFlags.GetField);
                        prefab = fieldInfo?.GetValue(item) as GameObject;
                        break;
                    }
                }

                if (prefab == null) continue;

                var wasActive = prefab.activeSelf;
                prefab.SetActive(false);
                var foundGameObject = GameObject.Find(metadata.PathToParent);
                Transform parentTransform = null;
                if (foundGameObject != null)
                {
                    parentTransform = foundGameObject.transform;
                }
                var instance = Instantiate(prefab, metadata.Position, metadata.Rotation, parentTransform);
                metadata.Instance = instance;
                RestoreSaveIds(instance, metadata.SaveIds, typeof(Remember), typeof(TLocalVariables));
                instance.SetActive(true);
                prefab.SetActive(wasActive);
            }
        }

        #endregion

        #region SaveUniqueId Manipulation

        static void RestoreSaveIds(GameObject gameObject, SaveIdMap[] saveIds, params Type[] types)
        {
            var allComponents = types.SelectMany(type => gameObject.GetComponentsInChildren(type, true));
            foreach (var component in allComponents)
            {
                foreach (var idMap in saveIds.Where(idMap => idMap.OriginalId == GetSaveId(component)))
                {
                    SetSaveId(component, idMap.NewId);
                }
            }
        }

        static IEnumerable<SaveIdMap> GetSaveIdMaps(GameObject gameObject, params Type[] types)
        {
            var saveIds = new List<SaveIdMap>();
            var allComponents = types.SelectMany(type => gameObject.GetComponentsInChildren(type, true));
            foreach (var component in allComponents)
            {
                var originalId = GetSaveId(component);
                if (originalId == null)
                {
                    continue;
                }
                var newId = UniqueID.GenerateID();
                saveIds.Add(new SaveIdMap(originalId, newId));
                SetSaveId(component, newId);
            }
            return saveIds;
        }

        static void SetSaveId(Component component, string newId)
        {
            var fieldInfo = component.GetType().GetField("m_SaveUniqueID",
                                                         BindingFlags.Instance
                                                         | BindingFlags.NonPublic
                                                         | BindingFlags.GetField);
            var save = fieldInfo?.GetValue(component) as SaveUniqueID;
            var saveUniqueID = new SaveUniqueID(save?.SaveValue ?? true, new UniqueID(newId).Get.String);
            fieldInfo?.SetValue(component, saveUniqueID);
        }

        static string GetSaveId(Component component)
        {
            var fieldInfo = component.GetType().GetField("m_SaveUniqueID",
                                                         BindingFlags.Instance
                                                         | BindingFlags.NonPublic
                                                         | BindingFlags.GetField);
            var save = fieldInfo?.GetValue(component) as SaveUniqueID;
            return save?.Get.String;
        }

        #endregion
    }
}
