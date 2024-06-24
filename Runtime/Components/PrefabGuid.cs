using GameCreator.Runtime.Common;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [DisallowMultipleComponent]
    public class PrefabGuid : MonoBehaviour
    {
        [SerializeField]
        UniqueID _guid = new("not-yet-indexed");

        public UniqueID Guid => _guid;
    }
}
