using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(KHID))]
public class KHIDDrawer : PropertyDrawer
{
    private const float ActionButtonWidth = 70f;
    private const float MenuButtonWidth = 22f;
    private const float Spacing = 2f;

    private string GetPrefix()
    {
        var attr = fieldInfo != null
            ? (KHIDPrefixAttribute)System.Attribute.GetCustomAttribute(fieldInfo, typeof(KHIDPrefixAttribute))
            : null;
        return attr?.Prefix ?? string.Empty;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty valueProp = property.FindPropertyRelative("value");
        string prefix = GetPrefix();

        HandleContextClick(position, valueProp, prefix);

        EditorGUI.BeginProperty(position, label, property);

        Rect contentRect = EditorGUI.PrefixLabel(position, label);

        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        bool hasValue = !string.IsNullOrEmpty(valueProp.stringValue);

        Rect menuRect = new Rect(contentRect.xMax - MenuButtonWidth, contentRect.y,
            MenuButtonWidth, contentRect.height);
        Rect buttonRect = new Rect(menuRect.x - Spacing - ActionButtonWidth, contentRect.y,
            ActionButtonWidth, contentRect.height);
        Rect fieldRect = new Rect(contentRect.x, contentRect.y,
            buttonRect.x - Spacing - contentRect.x, contentRect.height);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.TextField(fieldRect, hasValue ? valueProp.stringValue : "(not generated)");
        }

        if (!hasValue)
        {
            if (GUI.Button(buttonRect, "Generate"))
                SetId(valueProp, KHID.New(prefix).Value);
        }
        else
        {
            if (GUI.Button(buttonRect, "Copy"))
                EditorGUIUtility.systemCopyBuffer = valueProp.stringValue;
        }

        if (GUI.Button(menuRect, "▾"))
            BuildMenu(valueProp, prefix).DropDown(menuRect);

        EditorGUI.indentLevel = oldIndent;
        EditorGUI.EndProperty();
    }

    private static void HandleContextClick(Rect rowRect, SerializedProperty valueProp, string prefix)
    {
        Event e = Event.current;
        if (e.type != EventType.ContextClick || !rowRect.Contains(e.mousePosition))
            return;

        BuildMenu(valueProp, prefix).ShowAsContext();
        e.Use();
    }

    private static GenericMenu BuildMenu(SerializedProperty valueProp, string defaultPrefix)
    {
        SerializedObject so = valueProp.serializedObject;
        string path = valueProp.propertyPath;
        string current = valueProp.stringValue;
        bool hasValue = !string.IsNullOrEmpty(current);

        var menu = new GenericMenu();

        if (hasValue)
            menu.AddItem(new GUIContent("Copy ID"), false,
                () => EditorGUIUtility.systemCopyBuffer = current);
        else
            menu.AddDisabledItem(new GUIContent("Copy ID"));

        if (hasValue)
        {
            menu.AddItem(new GUIContent("Regenerate ID"), false, () =>
                ConfirmAndSet(so, path, "Regenerate ID?", RegenerateMessage(current),
                    "Regenerate", KHID.New(defaultPrefix).Value));

            menu.AddItem(new GUIContent("Regenerate ID With Prefix..."), false, () =>
            {
                if (!KHIDPrefixWindow.Prompt(defaultPrefix, out string prefix))
                    return;

                ConfirmAndSet(so, path, "Regenerate ID?", RegenerateMessage(current),
                    "Regenerate", KHID.New(prefix).Value);
            });

            menu.AddItem(new GUIContent("Clear ID"), false, () =>
                ConfirmAndSet(so, path, "Clear ID?",
                    "This will remove the current ID:\n\n" + current +
                    "\n\nAnything that references it will no longer match.",
                    "Clear", string.Empty));
        }
        else
        {
            menu.AddItem(new GUIContent("Generate ID"), false, () =>
            {
                so.Update();
                SetId(so.FindProperty(path), KHID.New(defaultPrefix).Value);
            });

            menu.AddItem(new GUIContent("Generate ID With Prefix..."), false, () =>
            {
                if (!KHIDPrefixWindow.Prompt(defaultPrefix, out string prefix))
                    return;

                so.Update();
                SetId(so.FindProperty(path), KHID.New(prefix).Value);
            });

            menu.AddDisabledItem(new GUIContent("Clear ID"));
        }

        return menu;
    }

    private static string RegenerateMessage(string current) =>
        "This will replace the current ID with a new one:\n\n" + current +
        "\n\nAnything that references the old ID (save files, other objects, " +
        "databases) will no longer match.";

    private static void ConfirmAndSet(SerializedObject so, string path, string title,
        string message, string okButton, string newValue)
    {
        if (!EditorUtility.DisplayDialog(title, message, okButton, "Cancel"))
            return;

        so.Update();
        SetId(so.FindProperty(path), newValue);
    }

    private static void SetId(SerializedProperty valueProp, string newValue)
    {
        valueProp.stringValue = newValue;
        valueProp.serializedObject.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}