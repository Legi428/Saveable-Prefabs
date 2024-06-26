using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal class InstanceMetadataList
    {
        [SerializeField]
        List<PrefabInstanceMetadata> _list = new();

        public List<PrefabInstanceMetadata> List => _list;

        public void Add(PrefabInstanceMetadata metadata)
        {
            _list.Add(metadata);
        }

        public void PrepareInstances()
        {
            var result = new List<PrefabInstanceMetadata>(_list);
            result.RemoveAll(metadata => metadata.Instance == null);
            foreach (var metadata in result)
            {
                metadata.UpdateInstancedData();
            }
            _list = result.OrderBy(metadata => metadata.HierarchyDepth)
                .ThenBy(metadata => metadata.Instance.transform.GetSiblingIndex()).ToList();
        }
    }
}
