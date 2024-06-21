using System;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal struct SaveIdMap
    {
        [SerializeField]
        string _originalId;

        [SerializeField]
        string _newId;

        public SaveIdMap(string originalId, string newId)
        {
            _originalId = originalId;
            _newId = newId;
        }

        public string OriginalId => _originalId;

        public string NewId => _newId;
    }
}
