using UnityEditor;
using UnityEngine;

public class KHIDPrefixWindow : EditorWindow
{
    private string prefix = "";
    private bool confirmed;
    private bool focusedOnce;

    /// <summary>Blocks until closed. Returns true if the user pressed Generate.</summary>
    public static bool Prompt(string initialPrefix, out string result)
    {
        var window = CreateInstance<KHIDPrefixWindow>();
        window.titleContent = new GUIContent("ID Prefix");
        window.prefix = initialPrefix ?? "";

        Rect main = EditorGUIUtility.GetMainWindowPosition();
        Vector2 size = new Vector2(320, 100);
        window.minSize = window.maxSize = size;
        window.position = new Rect(main.center - size * 0.5f, size);

        window.ShowModalUtility();

        result = window.prefix;
        bool ok = window.confirmed;
        DestroyImmediate(window);
        return ok;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Prefix (optional)", EditorStyles.boldLabel);

        GUI.SetNextControlName("prefix");
        prefix = EditorGUILayout.TextField(prefix);
        if (!focusedOnce)
        {
            EditorGUI.FocusTextInControl("prefix");
            focusedOnce = true;
        }

        string clean = KHID.Sanitize(prefix);
        EditorGUILayout.LabelField("Result: " + (clean.Length == 0 ? "<guid>" : clean + "_<guid>"),
            EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel") || IsKey(KeyCode.Escape))
            Close();
        if (GUILayout.Button("Generate") || IsKey(KeyCode.Return) || IsKey(KeyCode.KeypadEnter))
        {
            confirmed = true;
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    private static bool IsKey(KeyCode key) =>
        Event.current.type == EventType.KeyDown && Event.current.keyCode == key;
}