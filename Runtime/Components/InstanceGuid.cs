using GameCreator.Runtime.Common;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [DisallowMultipleComponent]
    public class InstanceGuid : MonoBehaviour
    {
        [SerializeField]
        UniqueID _guid = new();

        public IdString GuidIdString => _guid.Get;

        void Awake()
        {
            SaveablePrefabInstanceManager.RegisterInstanceGuid(this);
        }

        void OnDestroy()
        {
            SaveablePrefabInstanceManager.UnregisterInstanceGuid(this);
        }

        public void SetGuid(IdString newIdString)
        {
            SaveablePrefabInstanceManager.UnregisterInstanceGuid(this);
            _guid.Set = newIdString;
            SaveablePrefabInstanceManager.RegisterInstanceGuid(this);
        }
    }
}
