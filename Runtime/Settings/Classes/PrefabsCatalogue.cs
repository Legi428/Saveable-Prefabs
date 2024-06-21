using GameCreator.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    public class PrefabsCatalogue
    {
        // EXPOSED MEMBERS: -----------------------------------------------------------------------

        [SerializeField]
        GameObject[] _prefabs = Array.Empty<GameObject>();


        // MEMBERS: -------------------------------------------------------------------------------

        [NonSerialized]
        Dictionary<int, GameObject> _map;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public GameObject Get(UniqueID prefabGuid)
        {
            RequireInitialize();
            return _map.GetValueOrDefault(prefabGuid.Get.Hash);
        }

        public bool TryGet(UniqueID prefabGuid, out GameObject gameObject)
        {
            RequireInitialize();
            return _map.TryGetValue(prefabGuid.Get.Hash, out gameObject);
        }

        public bool Contains(UniqueID prefabGuid)
        {
            RequireInitialize();
            return _map.ContainsKey(prefabGuid.Get.Hash);
        }

        public GameObject[] GetAll()
        {
            return _prefabs;
        }

        void RequireInitialize()
        {
            if (_map != null) return;

            _map = new Dictionary<int, GameObject>();
            foreach (var prefab in _prefabs)
            {
                _map[prefab.GetComponent<PrefabGuid>().Guid.Get.Hash] = prefab;
            }
        }
    }
}
