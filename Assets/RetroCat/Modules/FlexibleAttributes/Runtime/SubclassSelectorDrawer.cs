#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RetroCat.Modules.Attributes
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private string[] _typeNames;
        private Type[] _types;
        private int _selectedIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SubclassSelectorAttribute attr = (SubclassSelectorAttribute)attribute;
        
            if (_types == null)
            {
                _types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => attr.BaseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToArray();

                _typeNames = _types.Select(t => t.FullName).ToArray();
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                _selectedIndex = Array.IndexOf(_typeNames, property.stringValue);
                _selectedIndex = _selectedIndex < 0 ? 0 : _selectedIndex;

                EditorGUI.BeginProperty(position, label, property);
                _selectedIndex = EditorGUI.Popup(position, label.text, _selectedIndex, _typeNames);
                property.stringValue = _typeNames[_selectedIndex];
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use with string fields only.");
            }
        }
    }
}
#endif