using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using IGameSave = GameCreator.Runtime.Common.IGameSave;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [DefaultExecutionOrder(ApplicationManager.EXECUTION_ORDER_FIRST_EARLIER)]
    public class SaveablePrefabInstanceManager : Singleton<SaveablePrefabInstanceManager>, IGameSave
    {
        readonly static ConcurrentDictionary<Type, Action<object, string>> SaveIdSetterCache = new();
        readonly static ConcurrentDictionary<Type, Func<object, string>> SaveIdGetterCache = new();
        static Func<object, GameObject> _prefabGetterMethod;

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

        void RespawnSavedPrefabInstances()
        {
            foreach (var metadata in _instances.List)
            {
                if (metadata.ScenePath != SceneManager.GetActiveScene().path) continue;
                GameObject prefab = null;

                switch (metadata)
                {
                    case ItemPrefabInstanceMetadata:
                    {
                        var item = InventoryRepository.Get.Items.Get(metadata.Guid.Get);
                        if (item?.HasPrefab == false) continue;
                        prefab = GetPrefab(item);
                        break;
                    }
                    case not null when !SaveablePrefabsRepository.Get.Prefabs.TryGet(metadata.Guid, out prefab):
                        continue;
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

        static GameObject GetPrefab(Item item)
        {
            var getter = _prefabGetterMethod;
            if (getter == null)
            {
                getter = CreateGetPrefabDelegate();
                _prefabGetterMethod = getter;
            }
            return getter(item);
        }

        static void SetSaveId(Component component, string newId)
        {
            try
            {
                var type = component.GetType();
                var setter = SaveIdSetterCache.GetOrAdd(type, CreateSetSaveIdDelegate);
                setter(component, newId);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        static string GetSaveId(Component component)
        {
            try
            {
                var type = component.GetType();
                var getter = SaveIdGetterCache.GetOrAdd(type, CreateGetSaveIdDelegate);
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

        static Func<object, GameObject> CreateGetPrefabDelegate()
        {
            var fieldInfo = typeof(Item).GetField("m_Prefab", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) throw new InvalidOperationException("Field 'm_Prefab' not found");

            var dynamicMethod = new DynamicMethod("", typeof(GameObject), new[] { typeof(object) }, typeof(Item), true);
            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, typeof(Item));
            il.Emit(OpCodes.Ldfld, fieldInfo);
            il.Emit(OpCodes.Ret);

            return (Func<object, GameObject>)dynamicMethod.CreateDelegate(typeof(Func<object, GameObject>));
        }

        static Action<object, string> CreateSetSaveIdDelegate(Type componentType)
        {
            var fieldInfo = componentType.GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) throw new InvalidOperationException("Field 'm_SaveUniqueID' not found");

            var dynamicMethod = new DynamicMethod("", null, new[] { typeof(object), typeof(string) }, componentType, true);
            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, componentType);

            il.Emit(OpCodes.Ldfld, fieldInfo);

            il.Emit(OpCodes.Ldarg_1);

            var idStringCtor = typeof(IdString).GetConstructor(new[] { typeof(string) });
            il.Emit(OpCodes.Newobj, idStringCtor);

            var setterProperty = typeof(SaveUniqueID).GetProperty("Set");
            if (setterProperty == null) throw new InvalidOperationException("Type 'SaveUniqueID' does not have a 'Set' property");
            var setMethod = setterProperty.GetSetMethod();
            il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            return (Action<object, string>)dynamicMethod.CreateDelegate(typeof(Action<object, string>));
        }

        static Func<object, string> CreateGetSaveIdDelegate(Type componentType)
        {
            var fieldInfo = componentType.GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) throw new InvalidOperationException("Field 'm_SaveUniqueID' not found");

            var dynamicMethod = new DynamicMethod("", typeof(string), new[] { typeof(object) }, componentType, true);
            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, componentType);

            il.Emit(OpCodes.Ldfld, fieldInfo);

            var getUniqueIdProperty = typeof(SaveUniqueID).GetProperty("Get");
            if (getUniqueIdProperty == null)
                throw new InvalidOperationException("Type 'SaveUniqueID' does not have a 'Get' property");
            il.Emit(OpCodes.Callvirt, getUniqueIdProperty.GetGetMethod());

            var idStringGetProperty = typeof(IdString).GetProperty("String");
            if (idStringGetProperty == null)
                throw new InvalidOperationException("Type 'IdString' does not have a 'String' property");
            il.Emit(OpCodes.Callvirt, idStringGetProperty.GetGetMethod());

            il.Emit(OpCodes.Ret);

            return (Func<object, string>)dynamicMethod.CreateDelegate(typeof(Func<object, string>));
        }

        #endregion
    }
}
