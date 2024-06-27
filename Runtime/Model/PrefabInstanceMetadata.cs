using GameCreator.Runtime.Common;
using GameCreator.Runtime.UniqueGameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal class PrefabInstanceMetadata
    {
        [SerializeField]
        protected int _sceneGuidHash;

        [SerializeField]
        protected UniqueID _guid;

        [SerializeField]
        protected ParentStructure _parentStructure;

        [SerializeField]
        protected int _hierarchyDepth;

        [SerializeField]
        protected SaveIdMap[] _saveIds;

        [SerializeField]
        protected Vector3 _position;

        [SerializeField]
        protected Quaternion _rotation;

        [SerializeField]
        protected string _name;

        public PrefabInstanceMetadata(GameObject instance, IEnumerable<SaveIdMap> saveIdMaps = null)
        {
            Instance = instance;
            if (instance.GetComponent<PrefabGuid>() is { } prefabGuid)
            {
                _guid = prefabGuid.Guid;
            }
            _saveIds = saveIdMaps?.ToArray() ?? Array.Empty<SaveIdMap>();
        }

        public int SceneGuidHash => _sceneGuidHash;
        public UniqueID Guid => _guid;
        public ParentStructure ParentStructure => _parentStructure;
        public SaveIdMap[] SaveIds => _saveIds;
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
        public string Name => _name;
        public int HierarchyDepth => _hierarchyDepth;
        public GameObject Instance { get; set; }

        public void UpdateInstancedData()
        {
            _sceneGuidHash = Instance.scene.GetGuid().GetHashCode();
            _position = Instance.transform.position;
            _rotation = Instance.transform.rotation;
            _name = Instance.name;

            _parentStructure = GetParentStructure(Instance);
            _hierarchyDepth = GetHierarchyDepth(Instance);
        }

        ParentStructure GetParentStructure(GameObject startPoint)
        {
            var path = new StringBuilder();
            var current = startPoint.transform.parent;

            var instanceGuidHash = 0;

            while (current != null)
            {
                if (path.Length > 0)
                    path.Insert(0, "/");

                if (current.GetComponent<InstanceGuid>() is { } instanceGuid)
                {
                    instanceGuidHash = instanceGuid.GuidIdString.Hash;
                    break;
                }

                path.Insert(0, current.name);

                current = current.parent;
            }

            return new ParentStructure(instanceGuidHash, path.ToString());
        }

        int GetHierarchyDepth(GameObject gameObject)
        {
            var depth = 0;
            var current = gameObject.transform;

            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
    }
}
