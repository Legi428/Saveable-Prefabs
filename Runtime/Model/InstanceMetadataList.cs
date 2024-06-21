using System;
using System.Collections.Generic;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal class InstanceMetadataList
    {
        List<InstanceMetadata> _list = new();
        public InstanceMetadata[] List => _list.ToArray();

        public void Add(InstanceMetadata metadata)
        {
            _list.Add(metadata);
        }

        public void UpdateInstances()
        {
            _list.RemoveAll(metadata => metadata.Instance == null);
            foreach (var metadata in _list)
            {
                metadata.UpdateInstancedData();
            }
        }
    }
}
