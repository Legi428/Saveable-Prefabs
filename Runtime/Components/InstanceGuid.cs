using GameCreator.Runtime.Common;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [DisallowMultipleComponent]
    public class InstanceGuid : MonoBehaviour
    {
        [SerializeField]
        UniqueID _guid = new();

        public int GuidHash => _guid.Get.Hash;

        void Awake()
        {
            SaveablePrefabInstanceManager.RegisterInstanceGuid(this);
        }

        void OnDestroy()
        {
            SaveablePrefabInstanceManager.UnregisterInstanceGuid(this);
        }
    }
}
