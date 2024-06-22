using GameCreator.Runtime.Common;
using System;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal struct SaveIdMap
    {
        [SerializeField]
        SaveUniqueID _originalId;

        [SerializeField]
        SaveUniqueID _newId;

        public SaveIdMap(SaveUniqueID originalId, SaveUniqueID newId)
        {
            _originalId = originalId;
            _newId = newId;
        }

        public SaveUniqueID OriginalId => _originalId;

        public SaveUniqueID NewId => _newId;
    }
}
