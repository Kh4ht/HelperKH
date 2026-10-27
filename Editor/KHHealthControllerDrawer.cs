using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KH.EditorTools
{
    [CustomPropertyDrawer(typeof(KHHealthController))]
    public class KHHealthControllerDrawer : PropertyDrawer
    {
        [Flags]
        private enum Issue
        {
            None = 0,
            MaxBelowOne = 1,
            HealthAboveMax = 2,
            HealthNegative = 4,
            DeadWithHealth = 8
        }

        private static readonly Issue[] IssueOrder =
        {
            Issue.MaxBelowOne, Issue.HealthAboveMax, Issue.HealthNegative, Issue.DeadWithHealth
        };

        private const float WarningHeight = 36f;

        private static FieldInfo[] listenerFields;
        private static GUIStyle barText;

        // Per-drawer UI state.
        private float amount = 10f;
        private bool usePercent;
        private bool showListeners;

        #region HEIGHT

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!TryGetProps(property, out SerializedProperty health, out SerializedProperty max, out SerializedProperty dead))
                return EditorGUI.GetPropertyHeight(property, label, true);

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float step = line + gap;

            if (!property.isExpanded)
                return line;

            bool multi = property.serializedObject.isEditingMultipleObjects;
            float h = line + step * 3f; // Max Health, Health, Is Dead

            if (!Application.isPlaying && !multi)
                h += CountBits(GetIssues(health, max, dead)) * (WarningHeight + gap);

            if (Application.isPlaying && !multi && GetLive(property) != null)
            {
                h += step * 4f; // amount row, kill row, info row, listeners foldout
                if (showListeners)
                    h += step * ListenerFields.Length;
            }

            return h;
        }

        #endregion
        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetProps(property, out SerializedProperty healthP, out SerializedProperty maxP, out SerializedProperty deadP))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float step = line + gap;

            bool multi = property.serializedObject.isEditingMultipleObjects;
            KHHealthController live = Application.isPlaying && !multi ? GetLive(property) : null;
            bool playing = live != null;

            if (Application.isPlaying && Event.current.type == EventType.Repaint)
                KHHealthRepaint.Touch();

            // In Play mode read the live object: the serialized copy can be stale.
            float health = playing ? live.Health : healthP.floatValue;
            int max = playing ? live.MaxHealth : maxP.intValue;
            bool dead = playing ? live.IsDead : deadP.boolValue;
            bool mixed = multi && (healthP.hasMultipleDifferentValues || maxP.hasMultipleDifferentValues);

            // ── Header: foldout + health bar ─────────────────────────────────
            Rect header = new(position.x, position.y, position.width, line);
            EditorGUI.BeginProperty(header, label, property);

            float labelWidth = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, labelWidth, line), property.isExpanded, label, true);

            Rect bar = new(header.x + labelWidth + 2f, header.y + 1f, header.width - labelWidth - 2f, line - 2f);
            if (bar.width > 10f)
                DrawBar(bar, health, max, dead, mixed);

            EditorGUI.EndProperty();

            if (!property.isExpanded)
                return;

            float y = position.y + step;
            Rect NextRow()
            {
                var row = new Rect(position.x, y, position.width, line);
                y += step;
                return row;
            }

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = oldIndent + 1;

            // ── Max Health ───────────────────────────────────────────────────
            Rect r = NextRow();
            EditorGUI.showMixedValue = maxP.hasMultipleDifferentValues && !playing;
            EditorGUI.BeginChangeCheck();
            int newMax = EditorGUI.IntField(r, new GUIContent("Max Health", "Minimum 1."), max);
            if (EditorGUI.EndChangeCheck())
            {
                newMax = Mathf.Max(1, newMax);

                if (playing)
                    live.MaxHealth = newMax; // goes through the property so listeners fire
                else
                {
                    maxP.intValue = newMax;
                    if (healthP.floatValue > newMax)
                        healthP.floatValue = newMax;
                }
            }
            EditorGUI.showMixedValue = false;

            // ── Health ───────────────────────────────────────────────────────
            r = NextRow();
            using (new EditorGUI.DisabledScope(playing))
            {
                Rect slider = new(r.x, r.y, r.width - 44f, r.height);
                Rect maxButton = new(r.xMax - 40f, r.y, 40f, r.height);

                EditorGUI.showMixedValue = healthP.hasMultipleDifferentValues && !playing;
                EditorGUI.BeginChangeCheck();
                float newHealth = EditorGUI.Slider(slider,
                    new GUIContent("Health", playing ? "Use the buttons below in Play mode so listeners fire." : null),
                    health, 0f, Mathf.Max(1, max));
                if (EditorGUI.EndChangeCheck())
                    healthP.floatValue = newHealth;
                EditorGUI.showMixedValue = false;

                if (GUI.Button(maxButton, new GUIContent("Max", "Set Health to Max Health"), EditorStyles.miniButton))
                {
                    healthP.floatValue = Mathf.Max(1, maxP.intValue);
                    healthP.serializedObject.ApplyModifiedProperties();
                }
            }

            // ── Is Dead (read only) ──────────────────────────────────────────
            r = NextRow();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.showMixedValue = deadP.hasMultipleDifferentValues && !playing;
                EditorGUI.Toggle(r, new GUIContent("Is Dead", "Derived from Health. Use Kill / Revive in Play mode."), dead);
                EditorGUI.showMixedValue = false;
            }

            // ── Warnings (edit mode) ─────────────────────────────────────────
            if (!Application.isPlaying && !multi)
            {
                Issue issues = GetIssues(healthP, maxP, deadP);

                foreach (Issue issue in IssueOrder)
                {
                    if ((issues & issue) == 0)
                        continue;

                    var row = new Rect(position.x, y, position.width, WarningHeight);
                    y += WarningHeight + gap;

                    if (DrawIssue(row, Describe(issue, healthP.floatValue, maxP.intValue)))
                    {
                        ApplyFix(issue, healthP, maxP, deadP);
                        GUIUtility.ExitGUI(); // the layout changed, so restart this pass
                    }
                }
            }

            // ── Play mode panel ──────────────────────────────────────────────
            if (playing)
            {
                EditorGUI.indentLevel = 0; // rects below are indented by hand
                DrawPlayPanel(NextRow(), NextRow(), NextRow(), NextRow(), live, ref y, position, step, line);
            }

            EditorGUI.indentLevel = oldIndent;
        }

        private void DrawPlayPanel(Rect row1, Rect row2, Rect row3, Rect row4,
                                   KHHealthController live, ref float y, Rect position, float step, float line)
        {
            // Indent manually: indentLevel is 0 here, so shift by one level (15px).
            Rect Inset(Rect rect) { rect.xMin += 15f; return rect; }

            row1 = Inset(row1);
            row2 = Inset(row2);
            row3 = Inset(row3);
            row4 = Inset(row4);

            // Row 1: amount | % | Damage | Heal
            float amountW = Mathf.Max(50f, row1.width * 0.32f);
            Rect amountR = new(row1.x, row1.y, amountW, row1.height);
            Rect pctR = new(amountR.xMax + 2f, row1.y, 28f, row1.height);
            float bw = (row1.xMax - pctR.xMax - 4f) * 0.5f;
            Rect dmgR = new(pctR.xMax + 2f, row1.y, bw, row1.height);
            Rect healR = new(dmgR.xMax + 2f, row1.y, bw, row1.height);

            amount = Mathf.Max(0f, EditorGUI.FloatField(amountR, amount));
            usePercent = GUI.Toggle(pctR, usePercent, new GUIContent("%", "Treat the amount as a percent of Max Health"), EditorStyles.miniButton);

            bool dead = live.IsDead;

            using (new EditorGUI.DisabledScope(dead))
            {
                if (GUI.Button(dmgR, "Damage"))
                {
                    if (usePercent) live.RemoveHealthPercent(amount / 100f);
                    else live.RemoveHealth(amount);
                }

                if (GUI.Button(healR, "Heal"))
                {
                    if (usePercent) live.AddHealthPercent(amount / 100f);
                    else live.AddHealth(amount);
                }
            }

            // Row 2: Kill | Revive | Full Heal
            float w = (row2.width - 4f) / 3f;
            Rect killR = new(row2.x, row2.y, w, row2.height);
            Rect reviveR = new(killR.xMax + 2f, row2.y, w, row2.height);
            Rect fullR = new(reviveR.xMax + 2f, row2.y, w, row2.height);

            using (new EditorGUI.DisabledScope(dead))
            {
                if (GUI.Button(killR, "Kill"))
                    live.RemoveHealth(live.MaxHealth);

                if (GUI.Button(fullR, "Full Heal"))
                    live.AddHealth(live.MaxHealth);
            }

            using (new EditorGUI.DisabledScope(!dead))
            {
                if (GUI.Button(reviveR, "Revive"))
                    live.Revive();
            }

            // Row 3: info
            float since = live.GetTimeSinceLastDamage();
            string sinceText = float.IsInfinity(since) ? "never" : $"{since:0.0}s ago";
            EditorGUI.LabelField(row3, $"Last damage: {sinceText}" + (live.HasFullHealth ? "   |   Full health" : ""), EditorStyles.miniLabel);

            // Row 4: listeners foldout
            FieldInfo[] fields = ListenerFields;
            int total = 0;
            foreach (FieldInfo f in fields)
                total += (f.GetValue(live) as List<Action>)?.Count ?? 0;

            showListeners = EditorGUI.Foldout(row4, showListeners,
                new GUIContent($"Listeners ({total})", "Subscriptions currently registered. A count that keeps growing means a missing Remove...Listener call."),
                true);

            if (!showListeners)
                return;

            foreach (FieldInfo f in fields)
            {
                var row = new Rect(position.x + 30f, y, position.width - 30f, line);
                y += step;

                string count = f.GetValue(live) is List<Action> list ? list.Count.ToString() : "n/a";
                EditorGUI.LabelField(row, ListenerName(f), count);
            }
        }

        private static void DrawBar(Rect rect, float health, int max, bool dead, bool mixed)
        {
            barText ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            string text;

            if (mixed)
                text = "\u2014";
            else if (dead)
            {
                EditorGUI.DrawRect(rect, new Color(0.45f, 0.1f, 0.1f, 1f));
                text = "DEAD";
            }
            else
            {
                float ratio = max > 0 ? Mathf.Clamp01(health / max) : 0f;
                Color fill = Color.HSVToRGB(ratio * 0.33f, 0.65f, 0.8f); // red -> green
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), fill);
                text = $"{health:0.#} / {max}   ({ratio * 100f:0}%)";
            }

            GUI.Label(rect, text, barText);
        }

        #endregion
        #region ISSUES

        private static Issue GetIssues(SerializedProperty health, SerializedProperty max, SerializedProperty dead)
        {
            Issue issues = Issue.None;
            float h = health.floatValue;
            int m = max.intValue;

            if (m < 1) issues |= Issue.MaxBelowOne;
            if (h > m) issues |= Issue.HealthAboveMax;
            if (h < 0f) issues |= Issue.HealthNegative;
            if (dead.boolValue && h > 0f) issues |= Issue.DeadWithHealth;

            return issues;
        }

        private static string Describe(Issue issue, float health, int max) => issue switch
        {
            Issue.MaxBelowOne => "Max Health must be at least 1.",
            Issue.HealthAboveMax => $"Health ({health:0.#}) is above Max Health ({max}).",
            Issue.HealthNegative => "Health is negative.",
            Issue.DeadWithHealth => "Marked dead but Health is above 0.",
            _ => string.Empty
        };

        private static void ApplyFix(Issue issue, SerializedProperty health, SerializedProperty max, SerializedProperty dead)
        {
            switch (issue)
            {
                case Issue.MaxBelowOne: max.intValue = 1; break;
                case Issue.HealthAboveMax: health.floatValue = Mathf.Max(1, max.intValue); break;
                case Issue.HealthNegative: health.floatValue = 0f; break;
                case Issue.DeadWithHealth: dead.boolValue = false; break;
            }

            health.serializedObject.ApplyModifiedProperties();
        }

        private static bool DrawIssue(Rect row, string message)
        {
            Rect box = EditorGUI.IndentedRect(row);
            EditorGUI.HelpBox(new Rect(box.x, box.y, box.width - 54f, box.height), message, MessageType.Warning);
            return GUI.Button(new Rect(box.xMax - 50f, box.y + (box.height - 20f) * 0.5f, 50f, 20f), "Fix");
        }

        private static int CountBits(Issue issue)
        {
            int n = 0;
            for (int v = (int)issue; v != 0; v >>= 1)
                n += v & 1;
            return n;
        }

        #endregion
        #region HELPERS

        private static bool TryGetProps(SerializedProperty property, out SerializedProperty health,
                                        out SerializedProperty max, out SerializedProperty dead)
        {
            health = property.FindPropertyRelative("_health");
            max = property.FindPropertyRelative("_maxHealth");
            dead = property.FindPropertyRelative("_isDead");
            return health != null && max != null && dead != null;
        }

        private static FieldInfo[] ListenerFields => listenerFields ??= BuildListenerFields();

        private static FieldInfo[] BuildListenerFields()
        {
            var result = new List<FieldInfo>();

            foreach (FieldInfo f in typeof(KHHealthController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(List<Action>))
                    result.Add(f);
            }

            return result.ToArray();
        }

        private static string ListenerName(FieldInfo f)
        {
            string n = f.Name.TrimStart('_');
            if (n.StartsWith("on")) n = n.Substring(2);
            if (n.EndsWith("Listeners")) n = n.Substring(0, n.Length - 9);
            return ObjectNames.NicifyVariableName(n);
        }

        /// <summary>Finds the live object behind a serialized path (supports fields and lists).</summary>
        private static KHHealthController GetLive(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "[");

            foreach (string part in path.Split('.'))
            {
                if (obj == null)
                    return null;

                string name = part;
                int index = -1;
                int bracket = part.IndexOf('[');

                if (bracket >= 0)
                {
                    name = part.Substring(0, bracket);
                    index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
                }

                FieldInfo field = null;
                for (Type t = obj.GetType(); t != null && field == null; t = t.BaseType)
                    field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field == null)
                    return null;

                obj = field.GetValue(obj);

                if (index >= 0)
                {
                    if (obj is IList list && index < list.Count)
                        obj = list[index];
                    else
                        return null;
                }
            }

            return obj as KHHealthController;
        }

        #endregion
    }

    /// <summary>
    /// The inspector only repaints on events. While a health controller is on screen in
    /// Play mode, this repaints about 10 times a second so the bar stays live, and it stops by itself.
    /// </summary>
    [InitializeOnLoad]
    internal static class KHHealthRepaint
    {
        private static double lastDraw;
        private static double lastRepaint;

        static KHHealthRepaint() => EditorApplication.update += Tick;

        internal static void Touch() => lastDraw = EditorApplication.timeSinceStartup;

        private static void Tick()
        {
            if (!Application.isPlaying)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (now - lastDraw > 0.5 || now - lastRepaint < 0.1)
                return;

            lastRepaint = now;
            InternalEditorUtility.RepaintAllViews();
        }
    }
}