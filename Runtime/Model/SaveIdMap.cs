using GameCreator.Runtime.Common;
using System;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal struct SaveIdMap
    {
        [SerializeField]
        IdString _originalId;

        [SerializeField]
        IdString _newId;

        public SaveIdMap(IdString originalId, IdString newId)
        {
            _originalId = originalId;
            _newId = newId;
        }

        public IdString OriginalId => _originalId;

        public IdString NewId => _newId;
    }
}
