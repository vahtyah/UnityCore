using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace VahTyah
{
    /// <summary>
    /// Marks a string field as an Android small-icon Id selector. The editor draws it as a dropdown
    /// populated from the sibling <see cref="ModuleNotifications.NotificationIcon"/>[] (only the Small entries),
    /// so the value can never drift out of sync with a typo. An explicit "Auto" option maps to the empty string.
    /// </summary>
    public sealed class SmallIconIdAttribute : PropertyAttribute
    {
        /// <summary>Name of the sibling NotificationIcon[] field to read Small Ids from (use nameof).</summary>
        public readonly string IconsField;

        public SmallIconIdAttribute(string iconsField) => IconsField = iconsField;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SmallIconIdAttribute))]
    internal sealed class SmallIconIdDrawer : PropertyDrawer
    {
        private const string AutoLabel = "(Auto — use the single Small icon)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = (SmallIconIdAttribute)attribute;
            SerializedProperty icons = property.serializedObject.FindProperty(attr.IconsField);

            // Gather the Ids of every registered Small icon.
            var ids = new List<string>();
            if (icons != null && icons.isArray)
            {
                for (int i = 0; i < icons.arraySize; i++)
                {
                    SerializedProperty el = icons.GetArrayElementAtIndex(i);
                    SerializedProperty isSmall = el.FindPropertyRelative("IsSmall");
                    SerializedProperty id = el.FindPropertyRelative("Id");
                    if (isSmall != null && isSmall.boolValue && id != null && !string.IsNullOrEmpty(id.stringValue))
                        ids.Add(id.stringValue);
                }
            }

            if (ids.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.TextField(position, label, "(register a Small icon below first)");
                return;
            }

            // Options: [Auto, id0, id1, ...]; append the current value as "(missing)" if it isn't registered.
            var options = new List<string> { AutoLabel };
            options.AddRange(ids);

            string current = property.stringValue;
            int index = string.IsNullOrEmpty(current) ? 0 : ids.IndexOf(current) + 1;
            if (!string.IsNullOrEmpty(current) && index == 0)
            {
                options.Add($"{current}  (missing!)");
                index = options.Count - 1;
            }

            int newIndex = EditorGUI.Popup(position, label.text, index, options.ToArray());
            if (newIndex != index)
            {
                if (newIndex == 0) property.stringValue = "";                       // Auto
                else if (newIndex <= ids.Count) property.stringValue = ids[newIndex - 1];
                // else: the "(missing!)" row — leave the value untouched.
            }
        }
    }
#endif
}
