using GameCreator.Runtime.Common;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameCreator.Runtime.SaveablePrefabs
{
    [Serializable]
    public class SaveablePrefabsRepository : TRepository<SaveablePrefabsRepository>
    {
        public const string REPOSITORY_ID = "saveable_prefabs.general";

        [SerializeField]
        PrefabsCatalogue _catalogue = new();

        // REPOSITORY PROPERTIES: -----------------------------------------------------------------
        public override string RepositoryID => REPOSITORY_ID;
        public PrefabsCatalogue Prefabs => _catalogue;

        // EDITOR ENTER PLAYMODE: -----------------------------------------------------------------

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        public static void InitializeOnEnterPlayMode()
        {
            Instance = null;
        }

#endif
    }
}
