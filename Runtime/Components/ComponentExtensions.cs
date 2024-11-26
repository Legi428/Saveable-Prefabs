using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.Variables;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreator.Runtime.SaveablePrefabs
{
    internal static class SceneExtensions
    {
        // Replace dynamic method with direct reflection call
        public static string GetGuid(this Scene scene)
        {
            var sceneType = typeof(Scene);
            var methodInfo = sceneType.GetMethod("GetGUIDInternal", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (methodInfo == null)
                throw new InvalidOperationException("GetGUIDInternal method not found in Scene.");
                
            return (string)methodInfo.Invoke(null, new object[] { scene.GetHashCode() });
        }
    }

    internal static class ItemExtensions
    {
        // Replace dynamic access with cached field info
        readonly static System.Reflection.FieldInfo PrefabField = typeof(Item)
            .GetField("m_Prefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static GameObject GetPrefab(this Item item)
        {
            if (PrefabField == null)
                throw new InvalidOperationException("m_Prefab field not found in Item.");
                
            return (GameObject)PrefabField.GetValue(item);
        }
    }

    internal static class MarkerExtensions
    {
        // Cache reflection info
        readonly static System.Reflection.FieldInfo UniqueIdField = typeof(Marker)
            .GetField("m_UniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        readonly static System.Reflection.PropertyInfo MarkersProperty = typeof(Marker)
            .GetProperty("Markers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        public static IdString GetIdString(this Marker marker)
        {
            if (UniqueIdField == null)
                throw new InvalidOperationException("m_UniqueID not found in Marker.");
                
            var uniqueId = (UniqueID)UniqueIdField.GetValue(marker);
            return uniqueId.Get;
        }

        public static Dictionary<IdString, Marker> GetMarkersDictionary(this Marker _)
        {
            if (MarkersProperty == null)
                throw new InvalidOperationException("Markers property not found in Marker.");
                
            return (Dictionary<IdString, Marker>)MarkersProperty.GetValue(null);
        }

        public static void SetIdString(this Marker marker, IdString newIdString)
        {
            if (UniqueIdField == null)
                throw new InvalidOperationException("m_UniqueID not found in Marker.");
                
            UniqueIdField.SetValue(marker, new UniqueID(newIdString.String));
        }
    }

    internal static class RememberExtensions
    {
        // Cache reflection fields
        readonly static System.Reflection.FieldInfo SaveUniqueIdField = typeof(Remember)
            .GetField("m_SaveUniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        readonly static System.Reflection.FieldInfo UniqueIdField = SaveUniqueIdField?.FieldType
            .GetField("m_UniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static IdString GetIdString(this Remember remember)
        {
            if (SaveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in Remember.");
                
            var saveUniqueId = (SaveUniqueID)SaveUniqueIdField.GetValue(remember);
            return saveUniqueId.Get;
        }

        public static void SetIdString(this Remember remember, IdString newIdString)
        {
            if (SaveUniqueIdField == null || UniqueIdField == null)
                throw new InvalidOperationException("Required fields not found in Remember.");
                
            var saveUniqueId = SaveUniqueIdField.GetValue(remember);
            UniqueIdField.SetValue(saveUniqueId, new UniqueID(newIdString.String));
        }
    }

    internal static class LocalVariablesExtensions
    {
        // Cache reflection fields
        readonly static System.Reflection.FieldInfo SaveUniqueIdField = typeof(TLocalVariables)
            .GetField("m_SaveUniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        readonly static System.Reflection.FieldInfo UniqueIdField = SaveUniqueIdField?.FieldType
            .GetField("m_UniqueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static IdString GetIdString(this TLocalVariables localVariables)
        {
            if (SaveUniqueIdField == null)
                throw new InvalidOperationException("m_SaveUniqueID not found in TLocalVariables.");
                
            var saveUniqueId = (SaveUniqueID)SaveUniqueIdField.GetValue(localVariables);
            return saveUniqueId.Get;
        }

        public static void SetIdString(this TLocalVariables localVariables, IdString newIdString)
        {
            if (SaveUniqueIdField == null || UniqueIdField == null)
                throw new InvalidOperationException("Required fields not found in TLocalVariables.");
                
            var saveUniqueId = SaveUniqueIdField.GetValue(localVariables);
            UniqueIdField.SetValue(saveUniqueId, new UniqueID(newIdString.String));
        }
    }
}
