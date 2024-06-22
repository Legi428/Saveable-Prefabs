using GameCreator.Editor.Common;
using GameCreator.Runtime.SaveablePrefabs;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCreator.Editor.SaveablePrefabs
{
    [CustomPropertyDrawer(typeof(PrefabsCatalogue))]
    public class PrefabsCatalogueDrawer : TTitleDrawer
    {
        protected override string Title => "Prefabs";

        protected override void CreateContent(VisualElement body, SerializedProperty property)
        {
            body.Add(new SpaceSmall());

            var items = property.FindPropertyRelative("_prefabs");

            var list = new List<SerializedProperty>(items.arraySize);
            for (var i = 0; i < items.arraySize; ++i)
            {
                var item = items.GetArrayElementAtIndex(i);
                list.Add(item);
            }
            var listView = new ListView(list);
            listView.makeItem += () =>
            {
                var objectField = new ObjectField();
                objectField.objectType = typeof(GameObject);
                objectField.SetEnabled(false);
                return objectField;
            };
            listView.bindItem += (element, i) =>
            {
                if (element is not ObjectField objectField) return;
                objectField.value = list[i].objectReferenceValue;
            };
            listView.style.maxHeight = 500;

            body.Add(listView);
        }
    }
}
