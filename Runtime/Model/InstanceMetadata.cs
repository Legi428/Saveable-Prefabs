﻿using GameCreator.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    internal abstract class InstanceMetadata
    {
        [SerializeField]
        protected string _scenePath;

        [SerializeField]
        protected UniqueID _guid;

        [SerializeField]
        protected string _pathToParent;

        [SerializeField]
        protected SaveIdMap[] _saveIds;

        [SerializeField]
        protected Vector3 _position;

        [SerializeField]
        protected Quaternion _rotation;

        public string ScenePath => _scenePath;
        public UniqueID Guid => _guid;
        public string PathToParent => _pathToParent;
        public SaveIdMap[] SaveIds => _saveIds;
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;

        public GameObject Instance { get; set; }

        protected InstanceMetadata(GameObject instance, IEnumerable<SaveIdMap> saveIdMaps = null)
        {
            Instance = instance;
            _saveIds = saveIdMaps?.ToArray() ?? Array.Empty<SaveIdMap>();
        }

        public void UpdateInstancedData()
        {
            _scenePath = Instance.scene.path;
            _position = Instance.transform.position;
            _rotation = Instance.transform.rotation;

            _pathToParent = "";
            var parent = Instance.transform.parent;
            if (parent != null)
            {
                _pathToParent = parent.name;
            }
            var traversePoint = parent;
            while (traversePoint?.parent != null)
            {
                _pathToParent = $"{traversePoint.name}/{_pathToParent}";
                traversePoint = traversePoint.parent;
            }
        }
    }
}