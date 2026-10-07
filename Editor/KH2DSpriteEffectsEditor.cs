// KH2DSpriteEffectsEditor.cs
// Custom Inspector for KH2DSpriteEffects.
//
// IMPORTANT: this file must live in a folder literally named "Editor" somewhere under
// Assets/ (e.g. Assets/Scripts/Editor/KH2DSpriteEffectsEditor.cs). Unity automatically
// excludes anything inside an "Editor" folder from player builds, which is required here
// since this uses UnityEditor APIs that don't exist in a build.
//
// What this gives you, per effect group:
//   - A foldout to collapse/expand it
//   - An "On" toggle for every effect that has a real off state (Amount/Intensity/Width),
//     which jumps to a sensible default when switched on and calls the matching ResetX()
//     when switched off
//   - A "Reset" button that snaps just that group back to its neutral/off default
//   - The plain fields underneath, wired through SerializedProperty so undo/multi-object
//     editing behave normally
// Plus "Assign KH2D Material" / "Apply All" / "Reset All" buttons up top.
//
// Groups with no meaningful "off" state (Tint, Dissolve, Flash hit-feedback, HSBC) only
// get a foldout + fields + Reset - no on/off toggle, since e.g. Dissolve/Flash are
// transient effects normally driven by DissolveIn/Out()/Flash() at runtime rather than a
// steady-state value.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KH.EditorTools
{
    [CustomEditor(typeof(KH2DSpriteEffects))]
    [CanEditMultipleObjects]
    public class KH2DSpriteEffectsEditor : Editor
    {
        private static readonly GUILayoutOption ToggleWidth = GUILayout.Width(36);
        private static readonly GUILayoutOption ResetButtonWidth = GUILayout.Width(56);

        // Foldout open/closed state per group, keyed by group title. Lives on the editor
        // instance, so it persists while this Inspector stays open/selected.
        private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Every effect below is always active on the shader - a value of 0 (or a " +
                "neutral 1, e.g. Saturation/Contrast) simply looks like it's off. Use the " +
                "On toggle or Reset button per effect, or Reset All, instead of a shader " +
                "on/off switch.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Assign KH2D Material"))
                ForEachTarget(t => t.AutoAssignMaterial(), "Assign KH2D Material");
            if (GUILayout.Button("Apply All"))
                ForEachTarget(t => t.ApplyAllEffects(), "Apply All Effects");
            if (GUILayout.Button("Reset All"))
                ForEachTarget(t => t.ResetAllEffects(), "Reset All Effects");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            DrawGroup("Tint", () =>
            {
                EditorGUILayout.PropertyField(P("tint"), new GUIContent("Color"));
            }, t => t.ResetTint());

            DrawGroup("Alpha Cutoff", () =>
            {
                EditorGUILayout.PropertyField(P("alphaCutoff"), new GUIContent("Threshold"));
            }, t => t.ResetAlphaCutoff(),
                t => t.AlphaCutoff > 0f,
                (t, on) => t.SetAlphaCutoff(on ? KH2DSpriteEffects.AlphaCutoffOnDefault : 0f));

            DrawGroup("Outer Outline", () =>
            {
                EditorGUILayout.PropertyField(P("outlineColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("outlineWidth"), new GUIContent("Width (px)"));
                EditorGUILayout.PropertyField(P("outlineOnly"), new GUIContent("Outline Only"));
            }, t => t.ResetOutline(),
                t => t.OutlineWidth > 0f,
                (t, on) => t.SetOutline(t.OutlineColor, on ? KH2DSpriteEffects.OutlineWidthOnDefault : 0f, t.OutlineOnly));

            DrawGroup("Inner Outline Glow", () =>
            {
                EditorGUILayout.PropertyField(P("innerOutlineColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("innerOutlineWidth"), new GUIContent("Width (px)"));
            }, t => t.ResetInnerOutline(),
                t => t.InnerOutlineWidth > 0f,
                (t, on) => t.SetInnerOutline(t.InnerOutlineColor, on ? KH2DSpriteEffects.InnerOutlineWidthOnDefault : 0f));

            DrawGroup("Dissolve", () =>
            {
                EditorGUILayout.PropertyField(P("dissolveAmount"), new GUIContent("Amount (preview)"));
                EditorGUILayout.PropertyField(P("dissolveEdgeColor"), new GUIContent("Edge Color"));
                EditorGUILayout.PropertyField(P("dissolveEdgeWidth"), new GUIContent("Edge Width"));
                EditorGUILayout.PropertyField(P("dissolveInvert"), new GUIContent("Invert Direction"));
            }, t => t.ResetDissolve());

            DrawGroup("Flash Hit Feedback", () =>
            {
                EditorGUILayout.PropertyField(P("flashColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("flashAmount"), new GUIContent("Amount (preview)"));
            }, t => t.ResetFlash());

            DrawGroup("Fill / Silhouette Recolor", () =>
            {
                EditorGUILayout.PropertyField(P("fillColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("fillAmount"), new GUIContent("Amount"));
            }, t => t.ResetFill(),
                t => t.FillAmount > 0f,
                (t, on) => t.SetFill(t.FillColor, on ? KH2DSpriteEffects.FillAmountOnDefault : 0f));

            DrawGroup("Hue / Saturation / Brightness / Contrast", () =>
            {
                EditorGUILayout.PropertyField(P("hue"), new GUIContent("Hue Shift"));
                EditorGUILayout.PropertyField(P("saturation"), new GUIContent("Saturation"));
                EditorGUILayout.PropertyField(P("brightness"), new GUIContent("Brightness"));
                EditorGUILayout.PropertyField(P("contrast"), new GUIContent("Contrast"));
            }, t => t.ResetHSBC());

            DrawGroup("Grayscale", () =>
            {
                EditorGUILayout.PropertyField(P("grayscaleAmount"), new GUIContent("Amount"));
            }, t => t.ResetGrayscale(),
                t => t.GrayscaleAmount > 0f,
                (t, on) => t.SetGrayscale(on ? KH2DSpriteEffects.GrayscaleAmountOnDefault : 0f));

            DrawGroup("Edge Glow Rim", () =>
            {
                EditorGUILayout.PropertyField(P("rimColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("rimWidth"), new GUIContent("Width (px)"));
                EditorGUILayout.PropertyField(P("rimIntensity"), new GUIContent("Intensity"));
            }, t => t.ResetRim(),
                t => t.RimIntensity > 0f,
                (t, on) => t.SetRim(t.RimColor, t.RimWidth, on ? KH2DSpriteEffects.RimIntensityOnDefault : 0f));

            DrawGroup("Shine Sweep", () =>
            {
                EditorGUILayout.PropertyField(P("shineColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(P("shineWidth"), new GUIContent("Band Width"));
                EditorGUILayout.PropertyField(P("shineAngle"), new GUIContent("Angle (deg)"));
                EditorGUILayout.PropertyField(P("shineSpeed"), new GUIContent("Speed"));
                EditorGUILayout.PropertyField(P("shineIntensity"), new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(P("shineLoop"), new GUIContent("Loop Continuously"));
            }, t => t.ResetShine(),
                t => t.ShineIntensity > 0f,
                (t, on) => t.SetShine(t.ShineColor, t.ShineWidth, t.ShineAngle, t.ShineSpeed,
                    on ? KH2DSpriteEffects.ShineIntensityOnDefault : 0f, t.ShineLoop));

            DrawGroup("Chromatic Aberration", () =>
            {
                EditorGUILayout.PropertyField(P("chromaticAmount"), new GUIContent("Amount (px)"));
            }, t => t.ResetChromatic(),
                t => t.ChromaticAmount > 0f,
                (t, on) => t.SetChromaticAmount(on ? KH2DSpriteEffects.ChromaticAmountOnDefault : 0f));

            DrawGroup("Pixelation", () =>
            {
                EditorGUILayout.PropertyField(P("pixelSize"), new GUIContent("Block Size (px)"));
            }, t => t.ResetPixelation(),
                t => t.PixelSize > 1f,
                (t, on) => t.SetPixelSize(on ? KH2DSpriteEffects.PixelSizeOnDefault : 1f));

            DrawGroup("Wave Distortion", () =>
            {
                EditorGUILayout.PropertyField(P("waveAmplitude"), new GUIContent("Amplitude (UV)"));
                EditorGUILayout.PropertyField(P("waveFrequency"), new GUIContent("Frequency"));
                EditorGUILayout.PropertyField(P("waveSpeed"), new GUIContent("Speed"));
                EditorGUILayout.PropertyField(P("waveVertical"), new GUIContent("Vertical Waves"));
            }, t => t.ResetWave(),
                t => t.WaveAmplitude > 0f,
                (t, on) => t.SetWave(on ? KH2DSpriteEffects.WaveAmplitudeOnDefault : 0f, t.WaveFrequency, t.WaveSpeed, t.WaveVertical));

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty P(string fieldName) => serializedObject.FindProperty(fieldName);

        /// <summary>Draws one collapsible effect group: a header row (foldout, optional On
        /// toggle, Reset button) followed by its fields when expanded. Box + indent are opened
        /// and closed within this single call, so they can never end up unbalanced.</summary>
        private void DrawGroup(
            string title,
            Action drawFields,
            Action<KH2DSpriteEffects> onReset,
            Func<KH2DSpriteEffects, bool> isOn = null,
            Action<KH2DSpriteEffects, bool> setOn = null)
        {
            if (!foldouts.TryGetValue(title, out bool expanded)) expanded = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            foldouts[title] = expanded;

            GUILayout.FlexibleSpace();

            if (isOn != null && setOn != null)
            {
                var first = (KH2DSpriteEffects)targets[0];
                bool currentOn = isOn(first);
                EditorGUI.showMixedValue = HasMixedOnState(isOn);
                bool newOn = GUILayout.Toggle(currentOn, "On", ToggleWidth);
                EditorGUI.showMixedValue = false;
                if (newOn != currentOn)
                    ForEachTarget(t => setOn(t, newOn), (newOn ? "Enable " : "Disable ") + title);
            }

            if (GUILayout.Button("Reset", ResetButtonWidth))
                ForEachTarget(onReset, "Reset " + title);

            EditorGUILayout.EndHorizontal();

            if (expanded)
            {
                EditorGUI.indentLevel++;
                drawFields();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private bool HasMixedOnState(Func<KH2DSpriteEffects, bool> isOn)
        {
            if (targets.Length < 2) return false;
            bool first = isOn((KH2DSpriteEffects)targets[0]);
            for (int i = 1; i < targets.Length; i++)
            {
                if (isOn((KH2DSpriteEffects)targets[i]) != first) return true;
            }
            return false;
        }

        /// <summary>Runs an action on every selected target with proper Undo support, then
        /// resyncs the SerializedObject so subsequently-drawn fields (and the final
        /// ApplyModifiedProperties call) reflect the change instead of stale cached data.</summary>
        private void ForEachTarget(Action<KH2DSpriteEffects> action, string undoLabel)
        {
            Undo.RecordObjects(targets, undoLabel);
            foreach (UnityEngine.Object obj in targets)
            {
                var comp = obj as KH2DSpriteEffects;
                if (comp == null) continue;
                action(comp);
                EditorUtility.SetDirty(comp);
            }
            serializedObject.Update();
        }
    }
}