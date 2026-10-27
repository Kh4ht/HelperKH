using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KH.EditorTools
{
    [CustomPropertyDrawer(typeof(KHCoin))]
    public class KHCoinDrawer : PropertyDrawer
    {
        private static readonly long[] QuickAmounts = { 10, 100, 1000 };
        private static FieldInfo changedField;
        private static GUIStyle barText;

        private long amount = 100;

        #region HEIGHT

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;

            float step = line + EditorGUIUtility.standardVerticalSpacing;
            KHCoin live = GetLiveIfPlaying(property);

            if (live == null)
                return line + step;                       // header + quick row

            float h = line + step * 4f;                   // header, info, actions, quick, listeners
            if (HasMax(live))
                h += step;                                // bar
            return h;
        }

        #endregion
        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty amountP = property.FindPropertyRelative("amount");
            if (amountP == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float line = EditorGUIUtility.singleLineHeight;
            float step = line + EditorGUIUtility.standardVerticalSpacing;
            KHCoin live = GetLiveIfPlaying(property);

            if (Application.isPlaying && Event.current.type == EventType.Repaint)
                KHCoinRepaint.Touch();

            // ── Header: foldout + amount ─────────────────────────────────────
            float labelWidth = EditorGUIUtility.labelWidth;
            Rect foldRect = new(position.x, position.y, labelWidth, line);
            Rect fieldRect = new(position.x + labelWidth + 2f, position.y, position.width - labelWidth - 2f, line);

            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            EditorGUI.BeginProperty(fieldRect, GUIContent.none, amountP);
            EditorGUI.showMixedValue = amountP.hasMultipleDifferentValues && live == null;
            EditorGUI.BeginChangeCheck();
            long shown = live != null ? live.Amount : amountP.longValue;
            long edited = EditorGUI.LongField(fieldRect, shown);
            if (EditorGUI.EndChangeCheck())
            {
                edited = Math.Max(0, edited);

                if (live != null) live.Set(edited);          // goes through the coin, so OnChanged fires
                else amountP.longValue = edited;
            }
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();

            if (!property.isExpanded)
                return;

            float y = position.y + step;
            Rect NextRow()
            {
                var r = new Rect(position.x, y, position.width, line);
                y += step;
                return r;
            }

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Rows are inset by hand, since indentLevel is 0.
            Rect Inset(Rect r) { r.xMin += 15f; return r; }

            if (live == null)
            {
                DrawQuickRow(Inset(NextRow()), null, amountP);
            }
            else
            {
                // Info
                EditorGUI.LabelField(Inset(NextRow()),
                    $"{live.Name}   |   Max: {(HasMax(live) ? live.Max.ToString("N0") : "no limit")}",
                    EditorStyles.miniLabel);

                // Bar
                if (HasMax(live))
                    DrawBar(Inset(NextRow()), live);

                // Amount | Add | Spend | Set
                Rect row = Inset(NextRow());
                float fieldW = Mathf.Max(50f, row.width * 0.34f);
                float bw = (row.width - fieldW - 6f) / 3f;

                amount = Math.Max(0, EditorGUI.LongField(new Rect(row.x, row.y, fieldW, row.height), amount));

                Rect addR = new(row.x + fieldW + 2f, row.y, bw, row.height);
                Rect spendR = new(addR.xMax + 2f, row.y, bw, row.height);
                Rect setR = new(spendR.xMax + 2f, row.y, bw, row.height);

                using (new EditorGUI.DisabledScope(live.Amount >= live.Max))
                    if (GUI.Button(addR, "Add")) live.Add(amount);

                using (new EditorGUI.DisabledScope(!live.CanAfford(amount)))
                    if (GUI.Button(spendR, new GUIContent("Spend", "TrySpend: disabled when the balance can't cover it"))) live.TrySpend(amount);

                if (GUI.Button(setR, "Set")) live.Set(amount);

                DrawQuickRow(Inset(NextRow()), live, amountP);

                // Listeners
                int count = ListenerCount(live);
                EditorGUI.LabelField(Inset(NextRow()),
                    new GUIContent($"OnChanged listeners: {count}",
                        "A number that keeps growing means a missing unsubscribe (-=)."),
                    EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel = oldIndent;
        }

        private void DrawQuickRow(Rect row, KHCoin live, SerializedProperty amountP)
        {
            int buttons = QuickAmounts.Length + 1;
            float w = (row.width - 2f * (buttons - 1)) / buttons;

            for (int i = 0; i < QuickAmounts.Length; i++)
            {
                var r = new Rect(row.x + i * (w + 2f), row.y, w, row.height);
                long q = QuickAmounts[i];

                if (!GUI.Button(r, "+" + q.ToString("N0"), EditorStyles.miniButton))
                    continue;

                if (live != null)
                    live.Add(q);
                else
                    amountP.longValue = SafeAdd(amountP.longValue, q);
            }

            var clearR = new Rect(row.x + QuickAmounts.Length * (w + 2f), row.y, w, row.height);
            if (GUI.Button(clearR, new GUIContent("Empty", "Set the balance to 0"), EditorStyles.miniButton))
            {
                if (live != null) live.Set(0);
                else amountP.longValue = 0;
            }
        }

        private static void DrawBar(Rect rect, KHCoin coin)
        {
            barText ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            double ratio = coin.Max > 0 ? (double)coin.Amount / coin.Max : 0d;
            ratio = Math.Min(1d, Math.Max(0d, ratio));

            rect.yMin += 1f;
            rect.yMax -= 1f;

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * (float)ratio, rect.height),
                               ratio >= 1d ? new Color(0.9f, 0.7f, 0.15f) : new Color(0.25f, 0.6f, 0.85f));

            GUI.Label(rect, $"{coin.Amount:N0} / {coin.Max:N0}   ({ratio * 100d:0.#}%)", barText);
        }

        #endregion
        #region HELPERS

        // Max below long.MaxValue means a real cap worth drawing a bar for.
        private static bool HasMax(KHCoin coin) => coin.Max > 0 && coin.Max < long.MaxValue;

        private static long SafeAdd(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;

        private static int ListenerCount(KHCoin coin)
        {
            changedField ??= typeof(KHCoin).GetField("OnChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            return (changedField?.GetValue(coin) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        private static KHCoin GetLiveIfPlaying(SerializedProperty property)
        {
            if (!Application.isPlaying || property.serializedObject.isEditingMultipleObjects)
                return null;

            return GetLive(property);
        }

        /// <summary>Finds the live object behind a serialized path (supports fields and lists).</summary>
        private static KHCoin GetLive(SerializedProperty property)
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

            return obj as KHCoin;
        }

        #endregion
    }

    /// <summary>
    /// The inspector only repaints on events. While a coin is on screen in Play mode,
    /// this repaints about 10 times a second, and stops by itself when nothing is drawn.
    /// </summary>
    [InitializeOnLoad]
    internal static class KHCoinRepaint
    {
        private static double lastDraw;
        private static double lastRepaint;

        static KHCoinRepaint() => EditorApplication.update += Tick;

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