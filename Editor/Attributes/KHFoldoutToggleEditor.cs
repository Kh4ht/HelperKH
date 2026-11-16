// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;

// namespace KH.EditorTools
// {
//     /// <summary>
//     /// Draws the default inspector, but turns bools marked [KHFoldoutToggle] into foldout headers
//     /// with a checkbox. Inherit from it for each type that uses the attribute.
//     /// </summary>
//     [CanEditMultipleObjects]
//     public class KHFoldoutToggleEditor : Editor
//     {
//         private class Marker
//         {
//             public bool IsStart;
//             public string Title;   // may be null: falls back to the field's display name
//             public bool IsEnd;
//         }

//         private static readonly Dictionary<Type, Dictionary<string, Marker>> Cache = new();
//         private static GUIStyle boldFoldout;

//         public override void OnInspectorGUI()
//         {
//             Dictionary<string, Marker> markers = GetMarkers(target.GetType());

//             // Types without the attribute: draw them exactly as Unity would.
//             if (markers.Count == 0)
//             {
//                 DrawDefaultInspector();
//                 return;
//             }

//             serializedObject.Update();

//             SerializedProperty it = serializedObject.GetIterator();
//             bool enterChildren = true;

//             bool boxOpen = false;
//             bool expanded = false;
//             bool enabled = true;

//             while (it.NextVisible(enterChildren))
//             {
//                 enterChildren = false;

//                 if (it.propertyPath == "m_Script")
//                 {
//                     using (new EditorGUI.DisabledScope(true))
//                         EditorGUILayout.PropertyField(it, true);
//                     continue;
//                 }

//                 markers.TryGetValue(it.name, out Marker marker);

//                 // Group start.
//                 if (marker is { IsStart: true } && it.propertyType == SerializedPropertyType.Boolean)
//                 {
//                     if (boxOpen)
//                         EndBox();

//                     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                     boxOpen = true;

//                     string title = string.IsNullOrEmpty(marker.Title) ? it.displayName : marker.Title;
//                     expanded = DrawHeader(it, title);
//                     enabled = it.boolValue || it.hasMultipleDifferentValues;
//                     continue;
//                 }

//                 // Outside any group.
//                 if (!boxOpen)
//                 {
//                     EditorGUILayout.PropertyField(it, true);
//                     continue;
//                 }

//                 // Inside a group.
//                 if (expanded)
//                 {
//                     using (new EditorGUI.DisabledScope(!enabled))
//                     {
//                         EditorGUI.indentLevel++;
//                         EditorGUILayout.PropertyField(it, true);
//                         EditorGUI.indentLevel--;
//                     }
//                 }

//                 if (marker != null && marker.IsEnd)
//                 {
//                     EndBox();
//                     boxOpen = false;
//                 }
//             }

//             if (boxOpen)
//                 EndBox();

//             serializedObject.ApplyModifiedProperties();
//         }

//         private static void EndBox()
//         {
//             EditorGUILayout.EndVertical();
//             EditorGUILayout.Space(2f);
//         }

//         private bool DrawHeader(SerializedProperty toggle, string title)
//         {
//             boldFoldout ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

//             Rect row = EditorGUILayout.GetControlRect(false);
//             Rect toggleRect = new(row.xMax - 18f, row.y, 18f, row.height);
//             Rect foldRect = new(row.x, row.y, toggleRect.x - row.x - 4f, row.height);

//             string key = $"KHFoldoutToggle.{target.GetType().FullName}.{toggle.propertyPath}";
//             bool open = EditorGUI.Foldout(foldRect, SessionState.GetBool(key, true), title, true, boldFoldout);
//             SessionState.SetBool(key, open);

//             int oldIndent = EditorGUI.indentLevel;
//             float oldLabelWidth = EditorGUIUtility.labelWidth;
//             EditorGUI.indentLevel = 0;
//             EditorGUIUtility.labelWidth = 0f;

//             // BeginProperty keeps prefab-override styling and undo working.
//             EditorGUI.BeginProperty(toggleRect, GUIContent.none, toggle);
//             EditorGUI.showMixedValue = toggle.hasMultipleDifferentValues;

//             EditorGUI.BeginChangeCheck();
//             bool value = EditorGUI.Toggle(toggleRect, toggle.boolValue);
//             if (EditorGUI.EndChangeCheck())
//                 toggle.boolValue = value;

//             EditorGUI.showMixedValue = false;
//             EditorGUI.EndProperty();

//             EditorGUIUtility.labelWidth = oldLabelWidth;
//             EditorGUI.indentLevel = oldIndent;

//             return open;
//         }

//         private static Dictionary<string, Marker> GetMarkers(Type type)
//         {
//             if (Cache.TryGetValue(type, out var cached))
//                 return cached;

//             var result = new Dictionary<string, Marker>();
//             const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

//             for (Type t = type; t != null && t != typeof(UnityEngine.Object); t = t.BaseType)
//             {
//                 foreach (FieldInfo field in t.GetFields(flags))
//                 {
//                     var start = field.GetCustomAttribute<KHFoldoutToggleAttribute>();
//                     bool end = field.IsDefined(typeof(KHEndFoldoutToggleAttribute), false);

//                     if (start == null && !end)
//                         continue;

//                     result[field.Name] = new Marker
//                     {
//                         IsStart = start != null,
//                         Title = start?.Title,
//                         IsEnd = end
//                     };
//                 }
//             }

//             Cache[type] = result;
//             return result;
//         }
//     }

//     // Applies to every MonoBehaviour / ScriptableObject that has no more specific custom editor.
//     [CustomEditor(typeof(MonoBehaviour), true)]
//     [CanEditMultipleObjects]
//     internal class KHFoldoutToggleMonoEditor : KHFoldoutToggleEditor { }

//     [CustomEditor(typeof(ScriptableObject), true)]
//     [CanEditMultipleObjects]
//     internal class KHFoldoutToggleScriptableEditor : KHFoldoutToggleEditor { }
// }