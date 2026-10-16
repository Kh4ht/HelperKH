using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KH.EditorTools
{
    [CustomEditor(typeof(KHUIElement), true)] // true = also applies to subclasses
    [CanEditMultipleObjects]
    public class KHUIElementEditor : Editor
    {
        // Fields that only matter when isModal is ticked.
        private static readonly HashSet<string> ModalOnly = new()
        {
            "closeOnBackdropClick",
            "closeOnBackKey",
            "backdropAlpha",
            "bringToFrontOnShow",
        };

        // Cached, so the hierarchy search doesn't run on every inspector repaint.
        private bool missingManager;

        private void OnEnable()
        {
            RefreshManagerCheck();
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            RefreshManagerCheck();
            Repaint();
        }

        /// <summary>
        /// Finds elements with no manager above them and unticks Is Modal on them.
        /// Prefab assets selected in the Project window are skipped (see notes).
        /// </summary>
        private void RefreshManagerCheck()
        {
            missingManager = false;

            foreach (Object t in targets)
            {
                if (t is not KHUIElement element || EditorUtility.IsPersistent(element))
                    continue;

                if (element.GetComponentInParent<KHUIManager>(true) != null)
                    continue;

                missingManager = true;

                if (Application.isPlaying || !element.IsModal)
                    continue;

                // Untick through a SerializedObject so undo and prefab overrides work.
                var so = new SerializedObject(element);
                so.FindProperty("isModal").boolValue = false;
                so.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty isModal = serializedObject.FindProperty("isModal");

            // With mixed values across multiple selected objects, keep the fields visible.
            bool showModalFields = isModal.boolValue || isModal.hasMultipleDifferentValues;

            SerializedProperty it = serializedObject.GetIterator();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;

                // The greyed-out "Script" field at the top.
                if (it.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(it, true);
                    continue;
                }

                bool isDependent = ModalOnly.Contains(it.name);

                if (isDependent && !showModalFields)
                    continue;

                if (isDependent)
                    EditorGUI.indentLevel++;

                // Lock the Is Modal toggle while a manager is missing.
                bool lockToggle = it.name == "isModal" && missingManager;

                using (new EditorGUI.DisabledScope(lockToggle))
                    EditorGUILayout.PropertyField(it, true);

                if (isDependent)
                    EditorGUI.indentLevel--;

                if (lockToggle)
                {
                    string message = serializedObject.isEditingMultipleObjects
                        ? "At least one selected element has no KHUIManager on a parent Canvas, so Is Modal is locked. Add a KHUIManager to enable it."
                        : "No KHUIManager found on a parent Canvas, so Is Modal is locked. Add a KHUIManager to enable it.";

                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}