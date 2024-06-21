using System;
using System.Collections.Generic;
using System.Linq;

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
            var result = new List<InstanceMetadata>(_list);
            result.RemoveAll(metadata => metadata.Instance == null);
            foreach (var metadata in result)
            {
                metadata.UpdateInstancedData();
            }
            _list = result.OrderBy(metadata => metadata.HierarchyDepth).ToList();
        }
    }
}
