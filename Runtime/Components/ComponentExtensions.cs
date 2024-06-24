using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    internal static class ItemExtensions
    {
        static Func<Item, GameObject> _prefabGetterMethod;

        public static GameObject GetPrefab(this Item item)
        {
            _prefabGetterMethod ??= CreateItemPrefabGetter();
            return _prefabGetterMethod(item);
        }

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
    }

    internal static class MarkerExtensions
    {
        static Func<Marker, UniqueID> _markerUniqueIdGetterMethod;
        static Action<Marker, UniqueID> _markerUniqueIdSetterMethod;
        static Func<Dictionary<IdString, Marker>> _markerDictionaryGetterMethod;

        public static IdString GetIdString(this Marker marker)
        {
            _markerUniqueIdGetterMethod ??= CreateMarkerUniqueIdGetter();
            return _markerUniqueIdGetterMethod(marker).Get;
        }

        public static Dictionary<IdString, Marker> GetMarkersDictionary(this Marker marker)
        {
            _markerDictionaryGetterMethod ??= CreateMarkerDictionaryGetter();
            return _markerDictionaryGetterMethod();
        }

        public static void SetIdString(this Marker marker, IdString newIdString)
        {
            _markerUniqueIdSetterMethod ??= CreateMarkerUniqueIdSetter();
            _markerUniqueIdSetterMethod(marker, new UniqueID(newIdString.String));
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
    }

    internal static class RememberExtensions
    {
        static Func<Remember, SaveUniqueID> _rememberSaveUniqueIdGetterCache;
        static Action<Remember, UniqueID> _rememberUniqueIdSetterMethod;

        public static IdString GetIdString(this Remember remember)
        {
            _rememberSaveUniqueIdGetterCache ??= CreateSaveUniqueIdGetter();
            return _rememberSaveUniqueIdGetterCache(remember).Get;
        }

        public static void SetIdString(this Remember remember, IdString newIdString)
        {
            _rememberUniqueIdSetterMethod ??= CreateSaveUniqueIdUniqueIdSetter();
            _rememberUniqueIdSetterMethod(remember, new UniqueID(newIdString.String));
        }

        static Func<Remember, SaveUniqueID> CreateSaveUniqueIdGetter()
        {
            var method = new DynamicMethod("GetRememberSaveUniqueId",
                                           typeof(SaveUniqueID),
                                           new[] { typeof(Remember) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField = typeof(Remember).GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in Remember or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, saveUniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Func<Remember, SaveUniqueID>)method.CreateDelegate(typeof(Func<Remember, SaveUniqueID>));
        }

        static Action<Remember, UniqueID> CreateSaveUniqueIdUniqueIdSetter()
        {
            var method = new DynamicMethod("SetRememberSaveUniqueIdUniqueId",
                                           typeof(void),
                                           new[] { typeof(Remember), typeof(UniqueID) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField = typeof(Remember).GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in Remember or its base classes.");

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

            return (Action<Remember, UniqueID>)method.CreateDelegate(typeof(Action<Remember, UniqueID>));
        }
    }

    internal static class LocalVariablesExtensions
    {
        static Func<TLocalVariables, SaveUniqueID> _localVariablesSaveUniqueIdGetterCache;
        static Action<TLocalVariables, UniqueID> _rememberUniqueIdSetterMethod;

        public static IdString GetIdString(this TLocalVariables localVariables)
        {
            _localVariablesSaveUniqueIdGetterCache ??= CreateSaveUniqueIdGetter();
            return _localVariablesSaveUniqueIdGetterCache(localVariables).Get;
        }

        public static void SetIdString(this TLocalVariables localVariables, IdString newIdString)
        {
            _rememberUniqueIdSetterMethod ??= CreateSaveUniqueIdUniqueIdSetter();
            _rememberUniqueIdSetterMethod(localVariables, new UniqueID(newIdString.String));
        }

        static Func<TLocalVariables, SaveUniqueID> CreateSaveUniqueIdGetter()
        {
            var method = new DynamicMethod("GetTLocalVariablesSaveUniqueId",
                                           typeof(SaveUniqueID),
                                           new[] { typeof(TLocalVariables) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField =
                typeof(TLocalVariables).GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in Remember or its base classes.");

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, saveUniqueIdField);
            il.Emit(OpCodes.Ret);

            return (Func<TLocalVariables, SaveUniqueID>)method.CreateDelegate(typeof(Func<TLocalVariables, SaveUniqueID>));
        }

        static Action<TLocalVariables, UniqueID> CreateSaveUniqueIdUniqueIdSetter()
        {
            var method = new DynamicMethod("SetTLocalVariablesSaveUniqueIdUniqueId",
                                           typeof(void),
                                           new[] { typeof(TLocalVariables), typeof(UniqueID) },
                                           typeof(SaveablePrefabInstanceManager),
                                           true);

            var saveUniqueIdField =
                typeof(TLocalVariables).GetField("m_SaveUniqueID", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in Remember or its base classes.");

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

            return (Action<TLocalVariables, UniqueID>)method.CreateDelegate(typeof(Action<TLocalVariables, UniqueID>));
        }
    }
}
