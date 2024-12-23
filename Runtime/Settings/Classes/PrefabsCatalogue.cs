using GameCreator.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    public class PrefabsCatalogue : ISerializationCallbackReceiver
    {
        // EXPOSED MEMBERS: -----------------------------------------------------------------------

        [SerializeField]
        GameObject[] _prefabs = new GameObject[0];

        // MEMBERS: -------------------------------------------------------------------------------

        [NonSerialized]
        Dictionary<int, GameObject> _map;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public GameObject Get(UniqueID prefabGuid)
        {
            EnsureInitialized();
            return _map.GetValueOrDefault(prefabGuid.Get.Hash);
        }

        public bool TryGet(UniqueID prefabGuid, out GameObject gameObject)
        {
            EnsureInitialized();
            return _map.TryGetValue(prefabGuid.Get.Hash, out gameObject);
        }

        public bool Contains(UniqueID prefabGuid)
        {
            EnsureInitialized();
            return _map.ContainsKey(prefabGuid.Get.Hash);
        }

        public GameObject[] GetAll()
        {
            return _prefabs;
        }

        // SERIALIZATION CALLBACK: ---------------------------------------------------------------

        public void OnBeforeSerialize()
        {
            // Nothing needed before serialization
        }

        public void OnAfterDeserialize()
        {
            // Reset the map to force reinitialization
            _map = null;
        }

        void EnsureInitialized()
        {
            if (_map != null) return;

            _map = new Dictionary<int, GameObject>();

            foreach (var prefab in _prefabs)
            {
                if (prefab == null) continue;

                var prefabGuid = prefab.GetComponent<PrefabGuid>();
                if (prefabGuid == null) continue;

                var hash = prefabGuid.Guid.Get.Hash;
                
                _map.TryAdd(hash, prefab);
            }

            // Update _prefabs to remove any null or duplicate references
            _prefabs = _map.Values.ToArray();
        }
    }
}
