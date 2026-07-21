using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Vẽ dropdown chọn subclass cho field [SerializeReference] + [SubclassSelector].
/// Áp cho từng phần tử của List nên dùng được cho cả 2 list Win/Lose.
/// </summary>
[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public sealed class SubclassSelectorDrawer : PropertyDrawer
{
    // Cache theo tên interface/base để khỏi quét TypeCache mỗi frame.
    private static readonly Dictionary<Type, Type[]> _candidates = new Dictionary<Type, Type[]>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        // Nút dropdown nằm đè lên vùng value của dòng đầu (bên phải label/foldout).
        var buttonRect = new Rect(position)
        {
            xMin = position.xMin + EditorGUIUtility.labelWidth + 2f,
            height = EditorGUIUtility.singleLineHeight,
        };

        var current = CurrentTypeName(property.managedReferenceFullTypename);
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(current), FocusType.Keyboard))
            ShowMenu(property, buttonRect);

        // Property field vẽ foldout + các field con của instance đang chọn.
        EditorGUI.PropertyField(position, property, label, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);

    private void ShowMenu(SerializedProperty property, Rect rect)
    {
        var attr = (SubclassSelectorAttribute)attribute;
        var baseType = GetManagedReferenceFieldType(property);
        var menu = new GenericMenu();

        // Bắt lại path + serializedObject vì callback của menu chạy sau OnGUI.
        var so = property.serializedObject;
        var path = property.propertyPath;
        var currentFull = property.managedReferenceFullTypename;

        if (attr.IncludeNull)
        {
            menu.AddItem(new GUIContent("<null>"), string.IsNullOrEmpty(currentFull),
                () => Assign(so, path, null));
        }

        if (baseType != null)
        {
            foreach (var type in GetCandidates(baseType))
            {
                bool on = currentFull == $"{type.Assembly.GetName().Name} {type.FullName}";
                var captured = type;
                menu.AddItem(new GUIContent(NiceName(type)), on, () => Assign(so, path, captured));
            }
        }

        menu.DropDown(rect);
    }

    private static void Assign(SerializedObject so, string path, Type type)
    {
        so.Update();
        var prop = so.FindProperty(path);
        prop.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
        so.ApplyModifiedProperties();
    }

    private static Type[] GetCandidates(Type baseType)
    {
        if (_candidates.TryGetValue(baseType, out var cached)) return cached;

        var list = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
            .Where(t => !typeof(UnityEngine.Object).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToArray();

        _candidates[baseType] = list;
        return list;
    }

    private static Type GetManagedReferenceFieldType(SerializedProperty property)
    {
        // Định dạng: "AssemblyName Namespace.TypeName"
        var parts = property.managedReferenceFieldTypename.Split(' ');
        if (parts.Length != 2) return null;

        var type = Type.GetType($"{parts[1]}, {parts[0]}");
        if (type != null) return type;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(parts[1]);
            if (type != null) return type;
        }
        return null;
    }

    private static string CurrentTypeName(string managedReferenceFullTypename)
    {
        if (string.IsNullOrEmpty(managedReferenceFullTypename)) return "(None)";
        var parts = managedReferenceFullTypename.Split(' ');
        var full = parts.Length == 2 ? parts[1] : managedReferenceFullTypename;
        int dot = full.LastIndexOf('.');
        return dot >= 0 ? full.Substring(dot + 1) : full;
    }

    private static string NiceName(Type type)
        => string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Name}  ({type.Namespace})";
}
