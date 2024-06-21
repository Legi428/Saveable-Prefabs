using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal class PrefabInstanceMetadata : InstanceMetadata
    {
        public PrefabInstanceMetadata(GameObject instance, IEnumerable<SaveIdMap> saveIdMaps = null) : base(instance, saveIdMaps)
        {
            _guid = instance.GetComponent<PrefabGuid>().Guid;
        }
    }
}
