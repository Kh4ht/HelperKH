using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HierarchyColorHeader
{
    private const string KEY = "[Header]";

    static HierarchyColorHeader()
    {
        EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(EntityId instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
        if (obj == null) return;

        if (obj.name.StartsWith(KEY))
        {
            // Mark as editor-only
            if (!obj.CompareTag("EditorOnly"))
            {
                obj.tag = "EditorOnly";
                EditorUtility.SetDirty(obj); // Save change in scene
            }
            if (!obj.isStatic)
            {
                obj.isStatic = true;
                EditorUtility.SetDirty(obj);
            }

            // Draw background color
            EditorGUI.DrawRect(selectionRect, new Color(0.2196078f, 0.2196078f, 0.2196078f, 1f));

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
            };
            style.normal.textColor = new Color(0.5019608f, 0.8392157f, 0.9921569f, 1f);

            GUIStyle style2 = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 16,
            };
            style2.normal.textColor = new Color(0f, 0f, 0f, 1f);

            EditorGUI.LabelField(selectionRect, obj.name.Replace(KEY, ""), style2);
            EditorGUI.LabelField(selectionRect, obj.name.Replace(KEY, ""), style);
        }
    }
}