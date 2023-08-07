using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Boxey.Core.Editor {
    public class ShowOnlyAttribute : PropertyAttribute{ }
    [CustomPropertyDrawer(typeof(ShowOnlyAttribute))]
    public class ShowOnlyDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
    }
}