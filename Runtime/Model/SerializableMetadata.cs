using GameCreator.Runtime.SaveablePrefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
internal class SerializableMetadata
{
    public string Type;
    public string Data;

    public SerializableMetadata(PrefabInstanceMetadata metadata)
    {
        Type = metadata.GetType().FullName;
        Data = JsonUtility.ToJson(metadata);
    }
}

[Serializable]
internal class SerializableMetadataList
{
    public List<SerializableMetadata> List;

    public SerializableMetadataList()
    {
    }

    public SerializableMetadataList(InstanceMetadataList metadataList)
    {
        List = metadataList.List.Select(m => new SerializableMetadata(m)).ToList();
    }

    public static List<PrefabInstanceMetadata> Deserialize(object serializedData)
    {
        if (serializedData is not SerializableMetadataList serializableList)
        {
            Debug.LogError($"Invalid serialized data type. Expected SerializableMetadataList. Got {serializedData?.GetType().FullName ?? "null"}");
            return new List<PrefabInstanceMetadata>();
        }

        return serializableList.List.Select(DeserializeMetadata).Where(m => m != null).ToList();
    }

    static PrefabInstanceMetadata DeserializeMetadata(SerializableMetadata serializableMetadata)
    {
        var type = serializableMetadata.Type;

        if (type == typeof(PrefabInstanceMetadata).FullName)
        {
            return JsonUtility.FromJson<PrefabInstanceMetadata>(serializableMetadata.Data);
        }
        if (type == typeof(ItemPrefabInstanceMetadata).FullName)
        {
            return JsonUtility.FromJson<ItemPrefabInstanceMetadata>(serializableMetadata.Data);
        }
        Debug.LogError($"Unknown type: {type}");
        return null;
    }
}
