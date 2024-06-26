using System;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal struct ParentStructure
    {
        [SerializeField]
        string _path;

        [SerializeField]
        int _instanceGuidHash;

        public ParentStructure(int instanceGuidHash = 0, string path = "")
        {
            _instanceGuidHash = instanceGuidHash;
            _path = path;
        }

        public string Path => _path;

        public int InstanceGuidHash => _instanceGuidHash;
    }
}
