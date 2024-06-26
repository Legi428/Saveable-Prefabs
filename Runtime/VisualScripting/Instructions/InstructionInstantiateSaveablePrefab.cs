using GameCreator.Runtime.Common;
using GameCreator.Runtime.SaveablePrefabs;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Search;
using Object = UnityEngine.Object;

namespace GameCreator.Runtime.VisualScripting
{
    [Title("Instantiate Saveable Prefab")]
    [Description("Creates a new instance of a referenced game object")]
    [Category("Game Objects/Instantiate Saveable Prefab")]
    [Parameter("Prefab", "Game Object reference that is instantiated")]
    [Parameter("Position", "The position of the new game object instance")]
    [Parameter("Rotation", "The rotation of the new game object instance")]
    [Parameter("Save", "Optional value where the newly instantiated game object is stored")]
    [Image(typeof(IconCubeSolid), ColorTheme.Type.Blue, typeof(OverlayPlus))]
    [Keywords("Create", "New", "Game Object")]
    [Serializable]
    public class InstructionInstantiateSaveablePrefab : Instruction
    {
        // MEMBERS: -------------------------------------------------------------------------------

        [SerializeField]
        [SearchContext("p: t:Prefab t:PrefabGuid",
                       SearchViewFlags.DisableQueryHelpers
                       | SearchViewFlags.DisableSavedSearchQuery
                       | SearchViewFlags.HideSearchBar)]
        GameObject _prefab;

        [SerializeField]
        PropertyGetPosition _position = GetPositionCharactersPlayer.Create;

        [SerializeField]
        PropertyGetRotation _rotation = GetRotationCharactersPlayer.Create;

        [SerializeField]
        PropertyGetGameObject _parent = GetGameObjectNone.Create();

        [SerializeField]
        PropertySetGameObject _save = SetGameObjectNone.Create;

        // PROPERTIES: ----------------------------------------------------------------------------

        public override string Title => $"Instantiate Saveable Prefab {_prefab}";

        // RUN METHOD: ----------------------------------------------------------------------------

        protected override Task Run(Args args)
        {
            var position = _position.Get(args);
            var rotation = _rotation.Get(args);

            var parent = _parent?.Get<Transform>(args);

            if (_prefab != null && SaveLoadManager.Instance.IsLoading == false)
            {
                GameObject instance;
                if (_prefab.GetComponent<PrefabGuid>() == null)
                {
                    instance = Object.Instantiate(_prefab, position, rotation, parent);
                }
                else
                {
                    instance = SaveablePrefabInstanceManager.Instance.InstantiatePrefab(_prefab, parent, position, rotation);
                }
                _save.Set(instance.gameObject, args);
            }

            return DefaultResult;
        }
    }
}
