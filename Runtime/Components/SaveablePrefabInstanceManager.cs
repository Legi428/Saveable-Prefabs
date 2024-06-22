using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
        readonly static ConcurrentDictionary<Type, Action<Component, SaveUniqueID>> SaveIdSetterCache = new();
        readonly static ConcurrentDictionary<Type, Func<Component, SaveUniqueID>> SaveIdGetterCache = new();
        static Func<Item, GameObject> _prefabGetterMethod;

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
            var scenePath = SceneManager.GetActiveScene().path.GetHashCode();

            List<GameObject> prefabsToReactivate = new();
            SortedList<int, Dictionary<GameObject, List<PrefabInstanceMetadata>>> prefabsToSpawn = new();
            for (var i = 0; i < prefabInstances.Count; i++)
            {
                var metadata = prefabInstances[i];
                if (metadata.ScenePathHash != scenePath) continue;

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

        static async Task RespawnPrefabList(GameObject prefab,
            IReadOnlyList<PrefabInstanceMetadata> metadataList)
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
                    instanceMetadata.Instance = instance;

                    Transform parentTransform = null;
                    if (!string.IsNullOrEmpty(instanceMetadata.PathToParent))
                    {
                        var foundGameObject = GameObject.Find(instanceMetadata.PathToParent);
                        if (foundGameObject != null)
                        {
                            parentTransform = foundGameObject.transform;
                        }
                    }
                    if (parentTransform != null)
                    {
                        instance.transform.SetParent(parentTransform);
                    }
                    RestoreSaveIds(instance, instanceMetadata.SaveIds, typeof(Remember), typeof(TLocalVariables));
                    instance.SetActive(true);
                }
            };
            while (!asyncResult.isDone)
            {
                await Task.Yield();
            }
        }

        static GameObject GetPrefab(PrefabInstanceMetadata metadata)
        {
            switch (metadata)
            {
                case ItemPrefabInstanceMetadata:
                {
                    var item = InventoryRepository.Get.Items.Get(metadata.Guid.Get);
                    return item?.HasPrefab == false ? null : GetPrefab(item);
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
        public Type SaveType => typeof(InstanceMetadataList);

        public object GetSaveData(bool includeNonSavable)
        {
            _instances.PrepareInstances();
            return _instances;
        }

        public LoadMode LoadMode => LoadMode.Greedy;

        public async Task OnLoad(object value)
        {
            if (value is InstanceMetadataList list && _instances != list)
            {
                _instances = list;
                await RespawnSavedPrefabInstances(_instances.List);
            }
        }

        #endregion

        #region SaveUniqueId Manipulation

        static void RestoreSaveIds(GameObject gameObject, SaveIdMap[] saveIds, params Type[] types)
        {
            foreach (var type in types)
            {
                foreach (var component in gameObject.GetComponentsInChildren(type, true))
                {
                    foreach (var idMap in saveIds)
                    {
                        if (idMap.OriginalId.Get.Hash == GetSaveUniqueId(component).Get.Hash)
                        {
                            SetSaveId(component, idMap.NewId);
                        }
                    }
                }
            }
        }

        static IEnumerable<SaveIdMap> GetSaveIdMaps(GameObject gameObject, params Type[] types)
        {
            var saveIds = new List<SaveIdMap>();
            foreach (var type in types)
            {
                foreach (var component in gameObject.GetComponentsInChildren(type, true))
                {
                    var originalSaveUniqueId = GetSaveUniqueId(component);
                    var newSaveUniqueId = new SaveUniqueID(originalSaveUniqueId.SaveValue, UniqueID.GenerateID());
                    saveIds.Add(new SaveIdMap(originalSaveUniqueId, newSaveUniqueId));
                    SetSaveId(component, newSaveUniqueId);
                }
            }
            return saveIds;
        }

        static GameObject GetPrefab(Item item)
        {
            _prefabGetterMethod ??= CreateItemPrefabGetter();
            return _prefabGetterMethod(item);
        }

        static void SetSaveId(Component component, SaveUniqueID saveUniqueID)
        {
            try
            {
                var type = component.GetType();
                var setter = SaveIdSetterCache.GetOrAdd(type, CreateIdStringSetter);
                setter(component, saveUniqueID);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        static SaveUniqueID GetSaveUniqueId(Component component)
        {
            try
            {
                var type = component.GetType();
                var getter = SaveIdGetterCache.GetOrAdd(type, CreateIdStringGetter);
                return getter(component);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        #endregion

        #region Dynamic Method Creation

        static Func<Item, GameObject> CreateItemPrefabGetter()
        {
            var method = new DynamicMethod("GetPrefab",
                                           typeof(GameObject),
                                           new[] { typeof(Item) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var prefabField = typeof(Item).GetField("m_Prefab", BindingFlags.NonPublic | BindingFlags.Instance);
            if (prefabField == null)
                throw new InvalidOperationException("m_Prefab not found in Item.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, prefabField);
            il.Emit(OpCodes.Ret);

            return (Func<Item, GameObject>)method.CreateDelegate(typeof(Func<Item, GameObject>));
        }

        static Action<Component, SaveUniqueID> CreateIdStringSetter(Type componentType)
        {
            var method = new DynamicMethod("SetSaveId",
                                           typeof(void),
                                           new[] { typeof(Component), typeof(SaveUniqueID) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField = componentType.GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException($"m_SaveUniqueID not found in {componentType.Name} or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, saveUniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Action<Component, SaveUniqueID>)method.CreateDelegate(typeof(Action<Component, SaveUniqueID>));
        }

        static Func<Component, SaveUniqueID> CreateIdStringGetter(Type componentType)
        {
            var method = new DynamicMethod("GetSaveId",
                                           typeof(SaveUniqueID),
                                           new[] { typeof(Component) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField = componentType.GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException($"m_SaveUniqueID not found in {componentType.Name} or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, saveUniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Func<Component, SaveUniqueID>)method.CreateDelegate(typeof(Func<Component, SaveUniqueID>));
        }

        #endregion
    }
}
