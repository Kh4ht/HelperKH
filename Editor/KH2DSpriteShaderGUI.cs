// MasterSpriteShaderGUI.cs
// Custom Inspector for Custom/2D/Master Sprite Shader.
// IMPORTANT: This file must live inside a folder named "Editor" anywhere under Assets/
// (e.g. Assets/Shaders/Editor/MasterSpriteShaderGUI.cs), or Unity will refuse to compile it
// into the correct assembly and you'll get "UnityEditor could not be found" errors.

using UnityEditor;
using UnityEngine;

public class MasterSpriteShaderGUI : ShaderGUI
{
    private struct Section
    {
        public string title;
        public string toggleProp;   // property name that drives the [Toggle] keyword, or null for always-visible
        public string keyword;      // shader_feature_local keyword this toggle must enable/disable
        public string[] props;      // property names to show when expanded

        public Section(string title, string toggleProp, string keyword, params string[] props)
        {
            this.title = title;
            this.toggleProp = toggleProp;
            this.keyword = keyword;
            this.props = props;
        }
    }

    private static readonly Section[] sections = new[]
    {
        new Section("Alpha Cutoff", "_EnableAlphaCutoff", "_ALPHACUTOFF_ON", "_AlphaCutoff"),
        new Section("Outer Outline", "_EnableOutline", "_OUTLINE_ON", "_OutlineColor", "_OutlineWidth", "_OutlineOnly"),
        new Section("Inner Outline / Glow", "_EnableInnerOutline", "_INNEROUTLINE_ON", "_InnerOutlineColor", "_InnerOutlineWidth"),
        new Section("Dissolve", "_EnableDissolve", "_DISSOLVE_ON", "_DissolveNoiseTex", "_DissolveAmount", "_DissolveEdgeWidth", "_DissolveEdgeColor", "_DissolveInvert"),
        new Section("Flash / Hit Feedback", "_EnableFlash", "_FLASH_ON", "_FlashColor", "_FlashAmount"),
        new Section("Fill / Silhouette Recolor", "_EnableFill", "_FILL_ON", "_FillColor", "_FillAmount"),
        new Section("Hue / Saturation / Brightness / Contrast", "_EnableHSBC", "_HSBC_ON", "_Hue", "_Saturation", "_Brightness", "_Contrast"),
        new Section("Grayscale", "_EnableGrayscale", "_GRAYSCALE_ON", "_GrayscaleAmount"),
        new Section("Edge Glow (Rim)", "_EnableRim", "_RIM_ON", "_RimColor", "_RimWidth", "_RimIntensity"),
        new Section("Shine Sweep", "_EnableShine", "_SHINE_ON", "_ShineColor", "_ShineWidth", "_ShineAngle", "_ShineSpeed", "_ShineIntensity", "_ShineLoop"),
        new Section("Chromatic Aberration", "_EnableChromatic", "_CHROMATIC_ON", "_ChromaticAmount"),
        new Section("Pixelation", "_EnablePixelate", "_PIXELATE_ON", "_PixelSize"),
        new Section("Wave Distortion", "_EnableWave", "_WAVE_ON", "_WaveAmplitude", "_WaveFrequency", "_WaveSpeed", "_WaveVertical"),
    };

    private static readonly System.Collections.Generic.Dictionary<string, bool> foldoutStates =
        new System.Collections.Generic.Dictionary<string, bool>();

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        EditorGUILayout.Space(4);
        DrawProp(materialEditor, properties, "_MainTex");
        DrawProp(materialEditor, properties, "_Color");
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        foreach (var section in sections)
        {
            DrawSection(materialEditor, properties, material, section);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
        DrawProp(materialEditor, properties, "_Cull");
        DrawProp(materialEditor, properties, "_ZWrite");

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Reset All Effects To Off"))
        {
            foreach (var section in sections)
            {
                if (!string.IsNullOrEmpty(section.toggleProp))
                {
                    MaterialProperty toggle = FindProp(section.toggleProp, properties, false);
                    if (toggle != null) toggle.floatValue = 0f;
                }
                if (!string.IsNullOrEmpty(section.keyword))
                {
                    SyncKeyword(materialEditor, section.keyword, false);
                }
            }
        }

        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }

    private void DrawSection(MaterialEditor materialEditor, MaterialProperty[] properties, Material material, Section section)
    {
        MaterialProperty toggleProp = string.IsNullOrEmpty(section.toggleProp)
            ? null
            : FindProp(section.toggleProp, properties, false);

        bool isOn = toggleProp == null || toggleProp.floatValue > 0.5f;

        // Keep the shader_feature_local keyword in sync with the toggle's value every
        // frame - this is required because shader_feature keywords are NOT driven
        // automatically unless Unity's built-in [Toggle] drawer is used, and this
        // custom Inspector draws its own checkbox instead of that drawer.
        if (toggleProp != null && !string.IsNullOrEmpty(section.keyword))
        {
            SyncKeyword(materialEditor, section.keyword, isOn);
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        if (!foldoutStates.ContainsKey(section.title))
            foldoutStates[section.title] = isOn;

        if (toggleProp != null)
        {
            EditorGUI.BeginChangeCheck();
            bool newOn = EditorGUILayout.Toggle(isOn, GUILayout.Width(18));
            if (EditorGUI.EndChangeCheck())
            {
                toggleProp.floatValue = newOn ? 1f : 0f;
                isOn = newOn;
                foldoutStates[section.title] = newOn;
                SyncKeyword(materialEditor, section.keyword, newOn);
            }
        }

        foldoutStates[section.title] = EditorGUILayout.Foldout(
            foldoutStates[section.title], section.title, true, EditorStyles.foldoutHeader);

        EditorGUILayout.EndHorizontal();

        if (foldoutStates[section.title])
        {
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!isOn))
            {
                foreach (string propName in section.props)
                {
                    DrawProp(materialEditor, properties, propName);
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void SyncKeyword(MaterialEditor materialEditor, string keyword, bool on)
    {
        foreach (Object target in materialEditor.targets)
        {
            Material m = target as Material;
            if (m == null) continue;

            bool currentlyOn = m.IsKeywordEnabled(keyword);
            if (currentlyOn == on) continue;

            if (on) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }
    }

    private void DrawProp(MaterialEditor materialEditor, MaterialProperty[] properties, string name)
    {
        MaterialProperty prop = FindProp(name, properties, false);
        if (prop != null)
        {
            materialEditor.ShaderProperty(prop, prop.displayName);
        }
    }

    private MaterialProperty FindProp(string name, MaterialProperty[] properties, bool required)
    {
        return FindProperty(name, properties, required);
    }
}
