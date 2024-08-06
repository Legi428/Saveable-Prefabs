using GameCreator.Runtime.Characters;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.UniqueGameObjects;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
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

        #region Respawn

        static async Task RespawnSavedPrefabInstances(IReadOnlyList<PrefabInstanceMetadata> prefabInstances)
        {
            var sceneGuidHash = SceneManager.GetActiveScene().GetGuid().GetHashCode();

            List<GameObject> prefabsToReactivate = new();
            SortedList<int, Dictionary<GameObject, List<PrefabInstanceMetadata>>> prefabsToSpawn = new();
            for (var i = 0; i < prefabInstances.Count; i++)
            {
                var metadata = prefabInstances[i];
                if (metadata.SceneGuidHash != sceneGuidHash) continue;

                if (GetPrefab(metadata) is not { } prefab) continue;

                if (prefab.activeSelf)
                {
                    prefab.SetActive(false);
                    prefabsToReactivate.Add(prefab);
                }
                if (prefabsToSpawn.TryGetValue(metadata.HierarchyDepth, out var dictionary))
                {
                    if (dictionary.TryGetValue(prefab, out var metadataList))
                    {
                        metadataList.Add(metadata);
                    }
                    else
                    {
                        dictionary.Add(prefab, new List<PrefabInstanceMetadata> { metadata });
                    }
                }
                else
                {
                    dictionary = new Dictionary<GameObject, List<PrefabInstanceMetadata>>
                    {
                        { prefab, new List<PrefabInstanceMetadata> { metadata } }
                    };
                    prefabsToSpawn.Add(metadata.HierarchyDepth, dictionary);
                }
            }
            foreach (var pair in prefabsToSpawn)
            {
                foreach (var (prefab, metadataList) in pair.Value)
                {
                    await RespawnPrefabList(prefab, metadataList);
                }
            }
            foreach (var prefab in prefabsToReactivate)
            {
                prefab.SetActive(true);
            }
        }

        static async Task RespawnPrefabList(GameObject prefab, IReadOnlyList<PrefabInstanceMetadata> metadataList)
        {
            var count = metadataList.Count;
            var positions = new Vector3[count];
            var rotations = new Quaternion[count];
            for (var i = 0; i < count; i++)
            {
                positions[i] = metadataList[i].Position;
                rotations[i] = metadataList[i].Rotation;
            }
            var asyncResult = InstantiateAsync(prefab, count, positions, rotations);
            asyncResult.completed += _ =>
            {
                for (var i = 0; i < asyncResult.Result.Length; i++)
                {
                    var instance = asyncResult.Result[i];
                    var instanceMetadata = metadataList[i];
                    instance.name = instanceMetadata.Name;
                    instanceMetadata.Instance = instance;

                    ReparentNewInstance(instanceMetadata, instance);
                    RestoreSaveIds(instance, instanceMetadata.SaveIds);
                    instance.SetActive(true);
                }
                Physics.SyncTransforms();
            };
            while (!asyncResult.isDone)
            {
                await Task.Yield();
            }
        }

        static void ReparentNewInstance(PrefabInstanceMetadata instanceMetadata, GameObject instance)
        {
            var parentStructure = instanceMetadata.ParentStructure;
            if (UniqueGameObjectsManager.Instance.GetByID(parentStructure.InstanceGuidHash) is { } foundUniqueGameObject)
            {
                var foundTransform = foundUniqueGameObject.transform;
                var newParent = foundTransform.Find(parentStructure.Path) ?? foundTransform;
                instance.transform.SetParent(newParent);
            }
            else if (parentStructure.Path != string.Empty && GameObject.Find(parentStructure.Path) is { } foundGameObject)
            {
                instance.transform.SetParent(foundGameObject.transform);
            }
        }

        static GameObject GetPrefab(PrefabInstanceMetadata metadata)
        {
            switch (metadata)
            {
                case ItemPrefabInstanceMetadata:
                {
                    var item = InventoryRepository.Get.Items.Get(metadata.Guid.Get);
                    return item?.m_Prefab;
                }
                case not null when SaveablePrefabsRepository.Get.Prefabs.TryGet(metadata.Guid, out var prefab):
                    return prefab;
                default:
                    return null;
            }
        }

        #endregion

        #region Instantiation

        void OnInstantiateItem()
        {
            var item = Item.LastItemInstantiated;
            var gameObjectInstance = Item.LastItemInstanceInstantiated;
            var saveIdMaps = GenerateNewIds(gameObjectInstance);
            var metadata = new ItemPrefabInstanceMetadata(item.Item, gameObjectInstance, saveIdMaps);

            _instances.Add(metadata);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var wasActive = prefab.activeSelf;
            prefab.SetActive(false);
            var instance = Instantiate(prefab, position, rotation, parent);

            var saveIdMaps = GenerateNewIds(instance);
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
        public Type SaveType => typeof(SerializableMetadataList);

        public object GetSaveData(bool includeNonSavable)
        {
            _instances.PrepareInstances();
            return new SerializableMetadataList(_instances);
        }

        public LoadMode LoadMode => LoadMode.Greedy;

        public async Task OnLoad(object value)
        {
            var deserialized = SerializableMetadataList.Deserialize(value);
            _instances = new InstanceMetadataList(deserialized);
            await RespawnSavedPrefabInstances(_instances.List);
        }

        #endregion

        #region SaveUniqueId Manipulation

        static void RestoreSaveIds(GameObject gameObject, SaveIdMap[] saveIds)
        {
            foreach (var component in gameObject.GetComponentsInChildren<Remember>(true))
            {
                ProcessComponent(component);
            }
            foreach (var component in gameObject.GetComponentsInChildren<TLocalVariables>(true))
            {
                ProcessComponent(component);
            }
            foreach (var component in gameObject.GetComponentsInChildren<Character>(true))
            {
                ProcessComponent(component);
            }
            foreach (var component in gameObject.GetComponentsInChildren<Marker>(true))
            {
                ProcessComponent(component);
            }
            foreach (var component in gameObject.GetComponentsInChildren<InstanceGuid>(true))
            {
                ProcessComponent(component);
            }
            return;

            void ProcessComponent(Component component)
            {
                foreach (var idMap in saveIds)
                {
                    if (idMap.OriginalId.Hash == GetIdString(component).Hash)
                    {
                        SetIdString(component, idMap.NewId);
                    }
                }
            }
        }

        static IEnumerable<SaveIdMap> GenerateNewIds(GameObject gameObject)
        {
            var saveIds = new List<SaveIdMap>();
            foreach (var component in gameObject.GetComponentsInChildren<Remember>(true))
            {
                ProcessComponent(component, saveIds);
            }
            foreach (var component in gameObject.GetComponentsInChildren<TLocalVariables>(true))
            {
                ProcessComponent(component, saveIds);
            }
            foreach (var component in gameObject.GetComponentsInChildren<Character>(true))
            {
                ProcessComponent(component, saveIds);
            }
            foreach (var component in gameObject.GetComponentsInChildren<Marker>(true))
            {
                ProcessComponent(component, saveIds);
            }
            return saveIds;

            void ProcessComponent(Component component, ICollection<SaveIdMap> saveIdMaps)
            {
                var idString = GetIdString(component);
                var newIdString = new IdString(UniqueID.GenerateID());
                saveIdMaps.Add(new SaveIdMap(idString, newIdString));
                SetIdString(component, newIdString);
            }
        }

        static void SetIdString(Component component, IdString newIdString)
        {
            try
            {
                switch (component)
                {
                    case Remember remember:
                        remember.SetIdString(newIdString);
                        break;
                    case TLocalVariables localVariables:
                        localVariables.SetIdString(newIdString);
                        break;
                    case Character character:
                        character.ChangeId(newIdString);
                        break;
                    case Marker marker:
                        var currentIdString = marker.GetIdString();
                        marker.SetIdString(newIdString);

                        var dict = marker.GetMarkersDictionary();
                        dict.Remove(currentIdString);
                        dict[newIdString] = marker;
                        break;
                    case InstanceGuid instanceGuid:
                        instanceGuid.SetGuid(newIdString);
                        break;
                    default:
                        Debug.LogWarning($"Encountered incompatible Component {component.GetType().Name} on {component.name}");
                        break;
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        static IdString GetIdString(Component component)
        {
            try
            {
                switch (component)
                {
                    case Remember remember:
                        return remember.GetIdString();
                    case TLocalVariables localVariables:
                        return localVariables.GetIdString();
                    case Character character:
                        return character.ID;
                    case Marker marker:
                        return marker.GetIdString();
                    case InstanceGuid instanceGuid:
                        return instanceGuid.GuidIdString;
                    default:
                        Debug.LogWarning($"Encountered incompatible Component {component.GetType().Name} on {component.name}");
                        return new IdString();
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        #endregion
    }
}
