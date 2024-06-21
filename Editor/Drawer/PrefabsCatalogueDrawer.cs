using GameCreator.Editor.Common;
using GameCreator.Runtime.SaveablePrefabs;
using UnityEditor;
using UnityEditor.UIElements;
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

            var itemsCount = items.arraySize;
            for (var i = 0; i < itemsCount; ++i)
            {
                var item = items.GetArrayElementAtIndex(i);
                var itemField = new PropertyField(item, string.Empty);

                itemField.SetEnabled(false);
                body.Add(itemField);
                body.Add(new SpaceSmaller());
            }
        }
    }
}
