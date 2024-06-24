using GameCreator.Runtime.Characters;
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
        readonly static ConcurrentDictionary<Type, Action<Component, UniqueID>> SaveUniqueIdIdStringSetterCache = new();
        static Func<Component, SaveUniqueID> _rememberSaveUniqueIdGetterCache;
        static Func<Component, SaveUniqueID> _localVariablesSaveUniqueIdGetterCache;
        static Func<Item, GameObject> _prefabGetterMethod;
        static Func<Marker, UniqueID> _markerUniqueIdGetterMethod;
        static Action<Marker, UniqueID> _markerUniqueIdSetterMethod;
        static Func<Dictionary<IdString, Marker>> _markerDictionaryGetterMethod;

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
                    instance.name = instanceMetadata.Name;
                    instanceMetadata.Instance = instance;

                    if (!string.IsNullOrEmpty(instanceMetadata.PathToParent))
                    {
                        var foundGameObject = GameObject.Find(instanceMetadata.PathToParent);
                        if (foundGameObject != null)
                        {
                            instance.transform.SetParent(foundGameObject.transform);
                        }
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
            var saveIdMaps = GenerateNewIds(gameObjectInstance, typeof(Remember), typeof(Character), typeof(Marker));
            var metadata = new ItemPrefabInstanceMetadata(item.Item, gameObjectInstance, saveIdMaps);

            _instances.Add(metadata);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var wasActive = prefab.activeSelf;
            prefab.SetActive(false);
            var instance = Instantiate(prefab, position, rotation, parent);

            var saveIdMaps = GenerateNewIds(instance,
                                            typeof(Remember),
                                            typeof(TLocalVariables),
                                            typeof(Character),
                                            typeof(Marker));
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
                        if (idMap.OriginalId.Hash == GetIdString(component)?.Hash)
                        {
                            SetIdString(component, idMap.NewId);
                        }
                    }
                }
            }
        }

        static IEnumerable<SaveIdMap> GenerateNewIds(GameObject gameObject, params Type[] types)
        {
            var saveIds = new List<SaveIdMap>();
            foreach (var type in types)
            {
                foreach (var component in gameObject.GetComponentsInChildren(type, true))
                {
                    if (GetIdString(component) is not { } originalIdString) continue;
                    var newIdString = new IdString(UniqueID.GenerateID());
                    saveIds.Add(new SaveIdMap(originalIdString, newIdString));
                    SetIdString(component, newIdString);
                }
            }
            return saveIds;
        }

        static GameObject GetPrefab(Item item)
        {
            _prefabGetterMethod ??= CreateItemPrefabGetter();
            return _prefabGetterMethod(item);
        }

        static void SetIdString(Component component, IdString idString)
        {
            try
            {
                switch (component)
                {
                    case Remember or TLocalVariables:
                        var type = component.GetType();
                        var setter = SaveUniqueIdIdStringSetterCache.GetOrAdd(type, CreateSaveUniqueIdUniqueIdSetter);
                        setter(component, new UniqueID(idString.String));
                        break;
                    case Character character:
                        character.ChangeId(idString);
                        break;
                    case Marker marker:
                        _markerUniqueIdGetterMethod ??= CreateMarkerUniqueIdGetter();
                        var currentIdString = _markerUniqueIdGetterMethod(marker).Get;

                        _markerUniqueIdSetterMethod ??= CreateMarkerUniqueIdSetter();
                        _markerUniqueIdSetterMethod(marker, new UniqueID(idString.String));

                        _markerDictionaryGetterMethod ??= CreateMarkerDictionaryGetter();
                        var dict = _markerDictionaryGetterMethod();
                        dict.Remove(currentIdString);
                        dict[idString] = marker;

                        break;
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.Log(component.GetType().Name + " on " + component.name);
                Debug.LogError(e);
                throw;
            }
        }

        static IdString? GetIdString(Component component)
        {
            try
            {
                switch (component)
                {
                    case Remember:
                        _rememberSaveUniqueIdGetterCache ??= CreateSaveUniqueIdGetter(typeof(Remember));
                        return _rememberSaveUniqueIdGetterCache(component).Get;
                    case TLocalVariables:
                        _localVariablesSaveUniqueIdGetterCache ??= CreateSaveUniqueIdGetter(typeof(TLocalVariables));
                        return _localVariablesSaveUniqueIdGetterCache(component).Get;
                    case Character character:
                        return character.ID;
                    case Marker marker:
                        _markerUniqueIdGetterMethod ??= CreateMarkerUniqueIdGetter();
                        return _markerUniqueIdGetterMethod(marker).Get;
                    default:
                        Debug.LogWarning($"Encountered incompatible Component {component.GetType().Name} on {component.name}");
                        return null;
                }
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
            var method = new DynamicMethod("GetItemPrefab",
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

        static Func<Marker, UniqueID> CreateMarkerUniqueIdGetter()
        {
            var method = new DynamicMethod("GetMarkerUniqueId",
                                           typeof(UniqueID),
                                           new[] { typeof(Marker) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var uniqueIdField = typeof(Marker).GetField("m_UniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (uniqueIdField == null)
                throw new InvalidOperationException("m_UniqueID not found in Marker or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, uniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Func<Marker, UniqueID>)method.CreateDelegate(typeof(Func<Marker, UniqueID>));
        }

        static Func<Dictionary<IdString, Marker>> CreateMarkerDictionaryGetter()
        {
            var method = new DynamicMethod("GetMarkerDictionary",
                                           typeof(Dictionary<IdString, Marker>),
                                           new[] { typeof(Marker) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var markerDictionaryProperty = typeof(Marker).GetProperty("Markers", BindingFlags.NonPublic | BindingFlags.Static);
            if (markerDictionaryProperty == null)
                throw new InvalidOperationException("Markers property not found in Marker or its base classes.");

            var getMarkerDictionaryMethod = markerDictionaryProperty.GetGetMethod(true);
            if (getMarkerDictionaryMethod == null)
                throw new InvalidOperationException("GetMethod for Markers property not found.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Call, getMarkerDictionaryMethod);
            il.Emit(OpCodes.Ret);

            return (Func<Dictionary<IdString, Marker>>)method.CreateDelegate(typeof(Func<Dictionary<IdString, Marker>>));
        }

        static Action<Component, UniqueID> CreateSaveUniqueIdUniqueIdSetter(Type componentType)
        {
            var method = new DynamicMethod("SetSaveUniqueIdUniqueId",
                                           typeof(void),
                                           new[] { typeof(Component), typeof(UniqueID) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField = componentType.GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException($"m_SaveUniqueID not found in {componentType.Name} or its base classes.");

            var uniqueIdField =
                saveUniqueIdField.FieldType.GetField("m_UniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (uniqueIdField == null)
                throw new InvalidOperationException("m_UniqueID not found in SaveUniqueID or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, saveUniqueIdField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, uniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Action<Component, UniqueID>)method.CreateDelegate(typeof(Action<Component, UniqueID>));
        }

        static Action<Marker, UniqueID> CreateMarkerUniqueIdSetter()
        {
            var method = new DynamicMethod("SetMarkerUniqueId",
                                           typeof(void),
                                           new[] { typeof(Marker), typeof(UniqueID) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var uniqueIdField = typeof(Marker).GetField("m_UniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (uniqueIdField == null)
                throw new InvalidOperationException("m_UniqueID not found in Marker or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, uniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Action<Marker, UniqueID>)method.CreateDelegate(typeof(Action<Marker, UniqueID>));
        }

        static Func<Component, SaveUniqueID> CreateSaveUniqueIdGetter(Type componentType)
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
