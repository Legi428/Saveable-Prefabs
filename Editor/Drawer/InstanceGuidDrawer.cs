using GameCreator.Runtime.SaveablePrefabs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCreator.Editor.SaveablePrefabs
{
    [CustomEditor(typeof(InstanceGuid))]
    public class InstanceGuidDrawer : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (Selection.count > 1)
            {
                return new Label("-");
            }
            var propertyField = new PropertyField(serializedObject.FindProperty("_guid"));
            propertyField.SetEnabled(!Application.isPlaying);
            return propertyField;
        }
    }
}
