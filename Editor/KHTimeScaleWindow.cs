using System.IO;
using UnityEditor;
using UnityEngine;
using KH;

namespace KH.EditorTools
{
    #region  TimeScaleSettings

    public class KHTimeScaleWindow : EditorWindow
    {
        private float timeScale = 1f;
        private TimeScaleSettings settings;

        [MenuItem("Tools/KH/KH Time Scale Window")]
        public static void ShowWindow()
        {
            GetWindow<KHTimeScaleWindow>("KH Time Scale");
        }

        private void OnEnable()
        {
            // Load or create settings
            settings = Resources.Load<TimeScaleSettings>("TimeScaleSettings");
            if (settings == null)
            {
                settings = CreateInstance<TimeScaleSettings>();
                if (!Directory.Exists("Assets/Resources"))
                    Directory.CreateDirectory("Assets/Resources");

                AssetDatabase.CreateAsset(settings, "Assets/Resources/TimeScaleSettings.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void OnGUI()
        {
            SettingsButton();

            GUIStyle style = new()
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle buttonStyle = new(GUI.skin.button)
            {
                fontSize = 16,                  // Bigger font
                fontStyle = FontStyle.Bold,     // Bold text
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10) // Extra padding
            };

            // Set text color separately
            style.normal.textColor = new Color(0.1f, 0.9f, 0.1f); // Orange

            GUILayout.Space(20);

            GUILayout.Label("KHTime Scale Debugger", style);

            GUILayout.Space(20);

            // Slider
            timeScale = EditorGUILayout.Slider(timeScale, 0f, settings.maxTimeScale);
            KHTimeManager.SetTimeScaleMultiplierForDebugging(timeScale);

            GUILayout.Space(20);

            // --- ROW 1 ---
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("x0", buttonStyle)) SetTimeScale(0f);

            GUILayout.Space(10);

            if (GUILayout.Button("x0.25", buttonStyle)) SetTimeScale(0.25f);

            GUILayout.Space(10);

            if (GUILayout.Button("x0.5", buttonStyle)) SetTimeScale(0.5f);
            EditorGUILayout.EndHorizontal();

            // --- ROW 2 ---
            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("x1", buttonStyle)) SetTimeScale(1f);
            GUILayout.Space(10);

            // --- ROW 3 ---
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.blue;
            if (GUILayout.Button("x1.5", buttonStyle)) SetTimeScale(1.5f);
            GUILayout.Space(10);
            if (GUILayout.Button("x2", buttonStyle)) SetTimeScale(2f);
            GUILayout.Space(10);
            if (GUILayout.Button("x3", buttonStyle)) SetTimeScale(3f);
            GUILayout.Space(10);
            if (GUILayout.Button("x4", buttonStyle)) SetTimeScale(4f);
            GUILayout.Space(10);
            if (GUILayout.Button("x5", buttonStyle)) SetTimeScale(5f);
            EditorGUILayout.EndHorizontal();
        }

        private void SetTimeScale(float value)
        {
            timeScale = value;
            KHTimeManager.SetTimeScaleMultiplierForDebugging(value);
        }

        private bool showMaxField = false;

        private void SettingsButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Pushes content to the right

            // Set button width and height
            GUILayoutOption[] buttonSize = { GUILayout.Width(75), GUILayout.Height(25) };

            if (showMaxField)
            {
                if (GUILayout.Button("Close", buttonSize))
                    showMaxField = false;
            }
            else
            {
                if (GUILayout.Button("Settings", buttonSize))
                    showMaxField = true;
            }

            GUILayout.EndHorizontal();

            // Show float field if toggled
            if (showMaxField)
            {
                float newMax = EditorGUILayout.FloatField("Max Scale Time:", settings.maxTimeScale);

                if (Mathf.Approximately(newMax, settings.maxTimeScale))
                    return;

                // Clamp the value between 5 and 20
                newMax = Mathf.Clamp(newMax, 5f, 20f);

                settings.maxTimeScale = newMax;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

    }

    #endregion
    #region  TimeScaleSettings

    public class TimeScaleSettings : ScriptableObject
    {
        [HideInInspector]
        public float maxTimeScale = 5f;
    }

    #endregion
}
