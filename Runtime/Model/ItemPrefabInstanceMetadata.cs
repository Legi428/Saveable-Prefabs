using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal class ItemPrefabInstanceMetadata : InstanceMetadata
    {
        public ItemPrefabInstanceMetadata(Item item, GameObject instance, IEnumerable<SaveIdMap> saveIdMaps = null) :
            base(instance, saveIdMaps)
        {
            _guid = new UniqueID(item.ID.String);
        }
    }
}
