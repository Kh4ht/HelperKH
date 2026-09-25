// KH2DSpriteEffects.cs
// Per-instance controller for EVERY effect on KH2DSpriteShader, using MaterialPropertyBlock.
// Attach to the same GameObject as your SpriteRenderer (sprites) or TilemapRenderer
// (tilemap layers) - anything with a Renderer that uses the KH2D shader works.
//
// IMPORTANT: every effect on the shader is now ALWAYS compiled in and ALWAYS evaluated -
// there are no more [Toggle(_X_ON)] shader_feature keywords. Those keywords are baked into
// the material asset at edit time and can't be flipped per-instance from a
// MaterialPropertyBlock, which caused visible glitches when toggled at runtime. Instead,
// "off" is just a value that produces zero visible change (Amount/Intensity/Width = 0, or
// neutral 1 where 1 means "no change", e.g. Saturation/Contrast). Since one shared material
// now always has every effect active, whether an effect is visible is controlled purely
// through its value from this script - call the matching SetX() method, edit the field in
// the Inspector, or call ResetAllEffects() / the individual ResetX() methods to snap it
// back to its neutral/off value.
//
// WHAT'S NEW IN THIS VERSION
// ---------------------------------------------------------------------------------------
// 1) Every effect value is now exposed as a serialized field, so you can author/preview it
//    straight from the Inspector. Every one of those fields still only ever reaches the
//    GPU through this object's own MaterialPropertyBlock - nothing here ever touches
//    spriteRenderer.material (which would silently clone the material) or
//    spriteRenderer.sharedMaterial's property values. Editing these fields, calling the
//    public methods, or both, is guaranteed to only affect THIS renderer.
// 2) Reset() (which Unity calls automatically the moment you add this component to a
//    GameObject, or when you pick "Reset" on it) will assign the shared KH2D material to
//    the Renderer for you if it doesn't already have one. See AutoAssignMaterial().
// 3) ResetAllEffects() (and the per-effect ResetX() methods) snap effect values back to
//    their neutral/off defaults, replacing the old shader-keyword on/off switches.
// 4) A companion KH2DSpriteEffectsEditor.cs (place it in an "Editor" folder) draws a
//    foldout + enable-toggle + reset button per effect in the Inspector.
//
// This does NOT depend on PrimeTween - simple coroutines only. Swap for tweens if you like.

using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Deliberately NOT [RequireComponent(typeof(SpriteRenderer))]: Unity won't let a
// GameObject carry both a SpriteRenderer and a TilemapRenderer at once, so forcing a
// SpriteRenderer here would make this component unusable on Tilemap layers. Instead this
// targets the generic Renderer base class, which SpriteRenderer, TilemapRenderer and
// MeshRenderer all derive from - GetPropertyBlock/SetPropertyBlock/sharedMaterial all live
// there, so the exact same script works on any of them.
[AddComponentMenu("KH/Material Controllers/" + nameof(KH2DSpriteEffects))]
[ExecuteAlways]
public class KH2DSpriteEffects : MonoBehaviour
{
    // Must match the shader's `Shader "KH/KH2D Sprite Shader"` declaration exactly.
    private const string ShaderName = "KH/KH2D Sprite Shader";

    // Optional convention for builds: drop a material called "KH2DSpriteMaterial" in any
    // Resources folder and it will be picked up automatically at runtime (Resources.Load
    // can't use AssetDatabase, which only exists in the editor).
    private const string RuntimeFallbackResourcePath = "KH2DSpriteMaterial";

    // Cached so repeated AddComponent calls in the same editor session don't re-search
    // the whole AssetDatabase every time.
    private static Material cachedSharedMaterial;

    // ---- cached property IDs (one Shader.PropertyToID call per property, ever) ----
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private static readonly int AlphaCutoffID = Shader.PropertyToID("_AlphaCutoff");

    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineOnlyID = Shader.PropertyToID("_OutlineOnly");

    private static readonly int InnerOutlineColorID = Shader.PropertyToID("_InnerOutlineColor");
    private static readonly int InnerOutlineWidthID = Shader.PropertyToID("_InnerOutlineWidth");

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveEdgeWidthID = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int DissolveEdgeColorID = Shader.PropertyToID("_DissolveEdgeColor");
    private static readonly int DissolveInvertID = Shader.PropertyToID("_DissolveInvert");

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");

    private static readonly int HueID = Shader.PropertyToID("_Hue");
    private static readonly int SaturationID = Shader.PropertyToID("_Saturation");
    private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
    private static readonly int ContrastID = Shader.PropertyToID("_Contrast");

    private static readonly int GrayscaleAmountID = Shader.PropertyToID("_GrayscaleAmount");

    private static readonly int RimColorID = Shader.PropertyToID("_RimColor");
    private static readonly int RimWidthID = Shader.PropertyToID("_RimWidth");
    private static readonly int RimIntensityID = Shader.PropertyToID("_RimIntensity");

    private static readonly int ShineColorID = Shader.PropertyToID("_ShineColor");
    private static readonly int ShineWidthID = Shader.PropertyToID("_ShineWidth");
    private static readonly int ShineAngleID = Shader.PropertyToID("_ShineAngle");
    private static readonly int ShineSpeedID = Shader.PropertyToID("_ShineSpeed");
    private static readonly int ShineIntensityID = Shader.PropertyToID("_ShineIntensity");
    private static readonly int ShineLoopID = Shader.PropertyToID("_ShineLoop");

    private static readonly int ChromaticAmountID = Shader.PropertyToID("_ChromaticAmount");

    private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");

    private static readonly int WaveAmplitudeID = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int WaveFrequencyID = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
    private static readonly int WaveVerticalID = Shader.PropertyToID("_WaveVertical");

    // ---------------------------------------------------------------------------------
    // Inspector-editable effect values. These are just the "resting" values applied on
    // Awake/OnValidate - every public method below still works at runtime and keeps
    // these fields in sync, so the Inspector always reflects the live state.
    //
    // Defaults below match the shader's own neutral/off values - a fresh component (or a
    // fresh material) shows no visible effects until you dial one in.
    // ---------------------------------------------------------------------------------

    [Header("Tint")]
    [SerializeField] private Color tint = Color.white;

    [Header("Alpha Cutoff")]
    [SerializeField, Range(0f, 1f)] private float alphaCutoff = 0f;

    [Header("Outer Outline")]
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 0f;
    [SerializeField] private bool outlineOnly = false;

    [Header("Inner Outline Glow")]
    [SerializeField] private Color innerOutlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float innerOutlineWidth = 0f;

    [Header("Dissolve")]
    [SerializeField] private Color dissolveEdgeColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField, Range(0f, 0.5f)] private float dissolveEdgeWidth = 0.05f;
    [SerializeField] private bool dissolveInvert = false;
    [SerializeField, Range(0f, 1f)] private float dissolveAmount = 0f;

    [Header("Flash Hit Feedback")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashAmount = 0f;

    [Header("Fill / Silhouette Recolor")]
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float fillAmount = 0f;

    [Header("Hue / Saturation / Brightness / Contrast")]
    [SerializeField, Range(-180f, 180f)] private float hue = 0f;
    [SerializeField, Range(0f, 2f)] private float saturation = 1f;
    [SerializeField, Range(-1f, 1f)] private float brightness = 0f;
    [SerializeField, Range(0f, 2f)] private float contrast = 1f;

    [Header("Grayscale")]
    [SerializeField, Range(0f, 1f)] private float grayscaleAmount = 0f;

    [Header("Edge Glow Rim")]
    [SerializeField] private Color rimColor = Color.white;
    [SerializeField, Range(0f, 20f)] private float rimWidth = 4f;
    [SerializeField, Range(0f, 5f)] private float rimIntensity = 0f;

    [Header("Shine Sweep")]
    [SerializeField] private Color shineColor = Color.white;
    [SerializeField, Range(0.01f, 1f)] private float shineWidth = 0.15f;
    [SerializeField, Range(0f, 180f)] private float shineAngle = 30f;
    [SerializeField, Range(0f, 5f)] private float shineSpeed = 1f;
    [SerializeField, Range(0f, 5f)] private float shineIntensity = 0f;
    [SerializeField] private bool shineLoop = true;

    [Header("Chromatic Aberration")]
    [SerializeField, Range(0f, 10f)] private float chromaticAmount = 0f;

    [Header("Pixelation")]
    [SerializeField, Range(1f, 64f)] private float pixelSize = 1f;

    [Header("Wave Distortion")]
    [SerializeField, Range(0f, 0.1f)] private float waveAmplitude = 0f;
    [SerializeField, Range(0f, 50f)] private float waveFrequency = 10f;
    [SerializeField, Range(0f, 10f)] private float waveSpeed = 2f;
    [SerializeField] private bool waveVertical = false;

    // ---------------------------------------------------------------------------------
    // "Turn it on" defaults - used by the companion Editor's per-effect toggle, which
    // needs some sensible starting value to jump to when you flip an effect on (these
    // mirror what the shader used to ship as its Properties-block defaults, back when
    // effects were opt-in via shader keywords). Purely a convenience; every field above
    // is still just a normal value you can set to anything from script.
    // ---------------------------------------------------------------------------------
    public const float AlphaCutoffOnDefault = 0.1f;
    public const float OutlineWidthOnDefault = 1f;
    public const float InnerOutlineWidthOnDefault = 1f;
    public const float FillAmountOnDefault = 1f;
    public const float GrayscaleAmountOnDefault = 1f;
    public const float RimIntensityOnDefault = 1f;
    public const float ShineIntensityOnDefault = 1f;
    public const float ChromaticAmountOnDefault = 1f;
    public const float PixelSizeOnDefault = 8f;
    public const float WaveAmplitudeOnDefault = 0.01f;

    private Renderer targetRenderer;
    private MaterialPropertyBlock mpb;

    private Coroutine flashRoutine;
    private Coroutine dissolveRoutine;

    // ---------------------------------------------------------------------------------
    // Read-only accessors for the fields above - lets the companion Editor script (or any
    // other code) inspect the current value of an effect without reflection. Setting a
    // value always goes through the SetX()/ResetX() methods further down, never through
    // these.
    // ---------------------------------------------------------------------------------
    public Color Tint => tint;
    public float AlphaCutoff => alphaCutoff;
    public Color OutlineColor => outlineColor;
    public float OutlineWidth => outlineWidth;
    public bool OutlineOnly => outlineOnly;
    public Color InnerOutlineColor => innerOutlineColor;
    public float InnerOutlineWidth => innerOutlineWidth;
    public Color DissolveEdgeColor => dissolveEdgeColor;
    public float DissolveEdgeWidth => dissolveEdgeWidth;
    public bool DissolveInvert => dissolveInvert;
    public float DissolveAmount => dissolveAmount;
    public Color FlashColor => flashColor;
    public float FlashAmount => flashAmount;
    public Color FillColor => fillColor;
    public float FillAmount => fillAmount;
    public float Hue => hue;
    public float Saturation => saturation;
    public float Brightness => brightness;
    public float Contrast => contrast;
    public float GrayscaleAmount => grayscaleAmount;
    public Color RimColor => rimColor;
    public float RimWidth => rimWidth;
    public float RimIntensity => rimIntensity;
    public Color ShineColor => shineColor;
    public float ShineWidth => shineWidth;
    public float ShineAngle => shineAngle;
    public float ShineSpeed => shineSpeed;
    public float ShineIntensity => shineIntensity;
    public bool ShineLoop => shineLoop;
    public float ChromaticAmount => chromaticAmount;
    public float PixelSize => pixelSize;
    public float WaveAmplitude => waveAmplitude;
    public float WaveFrequency => waveFrequency;
    public float WaveSpeed => waveSpeed;
    public bool WaveVertical => waveVertical;

    // =====================================================================================
    // Unity lifecycle
    // =====================================================================================

    // Reset() runs once, automatically, the instant you add this component to a GameObject
    // in the editor (and any time you pick "Reset" from its context menu). This is where we
    // make sure the object actually has the KH2D material - without ever creating a private
    // clone of it (that would break the "one shared material, per-instance MPB" model).
    private void Reset()
    {
        AutoAssignMaterial();
    }

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        // ExecuteAlways + ScriptableObject reloads mean Awake isn't always guaranteed to
        // have run yet in edit mode - make sure the refs exist before we touch the MPB.
        EnsureInitialized();
        ApplyAllEffects();
    }

#if UNITY_EDITOR
    // Lets you tweak the fields above in the Inspector (in or out of Play mode) and see
    // the result immediately. Still goes through SetPropertyBlock, so it never dirties
    // the shared material asset - other objects using it are completely unaffected.
    private void OnValidate()
    {
        EnsureInitialized();
        if (targetRenderer == null) return;
        ApplyAllEffects();
    }
#endif

    private void EnsureInitialized()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    // =====================================================================================
    // Auto material assignment
    // =====================================================================================

    /// <summary>
    /// Ensures this object's Renderer (SpriteRenderer, TilemapRenderer, whichever one the
    /// object actually has) is using a material built from the KH2D shader. Never creates a
    /// new material instance per-object - it always points at one shared asset, exactly
    /// like the rest of this script expects. If you already assigned a (correct) material
    /// yourself, this is a no-op. If the object has no Renderer at all yet (e.g. a bare
    /// Tilemap GameObject before "Add Tilemap Layer" has run), this does nothing - add the
    /// SpriteRenderer/TilemapRenderer first, then call this again (or just re-add/Reset the
    /// component once it does).
    /// </summary>
    public void AutoAssignMaterial()
    {
        var renderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
        if (renderer == null) return;

        if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null &&
            renderer.sharedMaterial.shader.name == ShaderName)
        {
            return; // Already set up correctly.
        }

        Material material = FindSharedMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
#if UNITY_EDITOR
            EditorUtility.SetDirty(renderer);
#endif
        }
        else
        {
            Debug.LogWarning(
                "KH2DSpriteEffects: couldn't find a Material using shader '" + ShaderName +
                "'. Assign one to the Renderer manually, or place one in a " +
                "Resources folder named '" + RuntimeFallbackResourcePath +
                "' so it can be found automatically in builds.", this);
        }
    }

    private static Material FindSharedMaterial()
    {
        if (cachedSharedMaterial != null) return cachedSharedMaterial;

#if UNITY_EDITOR
        // Editor: search the whole project for any Material asset using this shader and
        // reuse the first one found. This runs only in-editor (Reset/AddComponent), never
        // in a build.
        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null && mat.shader.name == ShaderName)
            {
                cachedSharedMaterial = mat;
                return cachedSharedMaterial;
            }
        }
#endif

        // Runtime/build fallback: look for a conventionally-named material in Resources.
        Material fromResources = Resources.Load<Material>(RuntimeFallbackResourcePath);
        if (fromResources != null && fromResources.shader != null &&
            fromResources.shader.name == ShaderName)
        {
            cachedSharedMaterial = fromResources;
        }

        return cachedSharedMaterial;
    }

    // =====================================================================================
    // Generic setters: every specific method below funnels through these two. Each call
    // reads-modifies-writes THIS renderer's own MaterialPropertyBlock only - the shared
    // material asset, and every other object using it, is never touched.
    // =====================================================================================

    private void SetFloat(int id, float value)
    {
        EnsureInitialized();
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(id, value);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private void SetColor(int id, Color value)
    {
        EnsureInitialized();
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(id, value);
        targetRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>Re-applies every serialized field to this object's property block in one
    /// pass. Called automatically on enable/validate; call it yourself after bulk-editing
    /// the public fields via script if you're not going through the setter methods.</summary>
    [ContextMenu("Apply All Effects")]
    public void ApplyAllEffects()
    {
        SetTint(tint);
        SetAlphaCutoff(alphaCutoff);
        SetOutline(outlineColor, outlineWidth, outlineOnly);
        SetInnerOutline(innerOutlineColor, innerOutlineWidth);
        SetDissolveEdge(dissolveEdgeColor, dissolveEdgeWidth, dissolveInvert);
        SetFloat(DissolveAmountID, dissolveAmount);
        SetFlashColor(flashColor);
        SetFloat(FlashAmountID, flashAmount);
        SetFill(fillColor, fillAmount);
        SetHSBC(hue, saturation, brightness, contrast);
        SetGrayscale(grayscaleAmount);
        SetRim(rimColor, rimWidth, rimIntensity);
        SetShine(shineColor, shineWidth, shineAngle, shineSpeed, shineIntensity, shineLoop);
        SetChromaticAmount(chromaticAmount);
        SetPixelSize(pixelSize);
        SetWave(waveAmplitude, waveFrequency, waveSpeed, waveVertical);
    }

    /// <summary>Snaps EVERY effect back to its neutral/off default in one call - the
    /// script-side replacement for the old per-effect shader toggles. Safe to call at
    /// any time; only touches this object's own property block.</summary>
    [ContextMenu("Reset All Effects")]
    public void ResetAllEffects()
    {
        if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
        if (dissolveRoutine != null) { StopCoroutine(dissolveRoutine); dissolveRoutine = null; }

        SetTint(Color.white);
        ResetAlphaCutoff();
        ResetOutline();
        ResetInnerOutline();
        ResetDissolve();
        ResetFlash();
        ResetFill();
        ResetHSBC();
        ResetGrayscale();
        ResetRim();
        ResetShine();
        ResetChromatic();
        ResetPixelation();
        ResetWave();
    }

    public void ResetTint() => SetTint(Color.white);

    public void ResetAlphaCutoff() => SetAlphaCutoff(0f);

    public void ResetOutline() => SetOutline(Color.white, 0f, false);

    public void ResetInnerOutline() => SetInnerOutline(Color.white, 0f);

    public void ResetDissolve()
    {
        SetDissolveEdge(new Color(1f, 0.5f, 0f, 1f), 0.05f, false);
        SetFloat(DissolveAmountID, 0f);
        dissolveAmount = 0f;
    }

    public void ResetFlash()
    {
        SetFlashColor(Color.white);
        SetFloat(FlashAmountID, 0f);
        flashAmount = 0f;
    }

    public void ResetFill() => SetFill(Color.white, 0f);

    public void ResetHSBC() => SetHSBC(0f, 1f, 0f, 1f);

    public void ResetGrayscale() => SetGrayscale(0f);

    public void ResetRim() => SetRim(Color.white, 4f, 0f);

    public void ResetShine() => SetShine(Color.white, 0.15f, 30f, 1f, 0f, true);

    public void ResetChromatic() => SetChromaticAmount(0f);

    public void ResetPixelation() => SetPixelSize(1f);

    public void ResetWave() => SetWave(0f, 10f, 2f, false);

    // ---- tint ----

    public void SetTint(Color color)
    {
        tint = color;
        SetColor(ColorID, color);
    }

    // ---- alpha cutoff ----

    public void SetAlphaCutoff(float threshold01)
    {
        alphaCutoff = threshold01;
        SetFloat(AlphaCutoffID, threshold01);
    }

    // ---- outer outline ----

    public void SetOutline(Color color, float widthPx, bool outlineOnlyValue = false)
    {
        outlineColor = color;
        outlineWidth = widthPx;
        outlineOnly = outlineOnlyValue;
        SetColor(OutlineColorID, color);
        SetFloat(OutlineWidthID, widthPx);
        SetFloat(OutlineOnlyID, outlineOnlyValue ? 1f : 0f);
    }

    // ---- inner outline glow ----

    public void SetInnerOutline(Color color, float widthPx)
    {
        innerOutlineColor = color;
        innerOutlineWidth = widthPx;
        SetColor(InnerOutlineColorID, color);
        SetFloat(InnerOutlineWidthID, widthPx);
    }

    // ---- dissolve ----

    public void SetDissolveEdge(Color edgeColor, float edgeWidth01, bool invert = false)
    {
        dissolveEdgeColor = edgeColor;
        dissolveEdgeWidth = edgeWidth01;
        dissolveInvert = invert;
        SetColor(DissolveEdgeColorID, edgeColor);
        SetFloat(DissolveEdgeWidthID, edgeWidth01);
        SetFloat(DissolveInvertID, invert ? 1f : 0f);
    }

    /// <summary>Dissolves from fully visible (0) to fully gone (1) over 'duration' seconds.</summary>
    public void DissolveOut(float duration = 1f, System.Action onComplete = null)
    {
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(DissolveRoutine(0f, 1f, duration, onComplete));
    }

    /// <summary>Reverses a dissolve, bringing the sprite back from gone (1) to visible (0).</summary>
    public void DissolveIn(float duration = 1f, System.Action onComplete = null)
    {
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(DissolveRoutine(1f, 0f, duration, onComplete));
    }

    private IEnumerator DissolveRoutine(float from, float to, float duration, System.Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            dissolveAmount = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            SetFloat(DissolveAmountID, dissolveAmount);
            yield return null;
        }
        dissolveAmount = to;
        SetFloat(DissolveAmountID, to);
        dissolveRoutine = null;
        onComplete?.Invoke();
    }

    // ---- flash hit feedback ----

    public void SetFlashColor(Color color)
    {
        flashColor = color;
        SetColor(FlashColorID, color);
    }

    /// <summary>Briefly flashes the sprite, fading FlashAmount 1 -> 0 over 'duration' seconds.</summary>
    public void Flash(float duration = 0.12f)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            flashAmount = 1f - Mathf.Clamp01(t / duration);
            SetFloat(FlashAmountID, flashAmount);
            yield return null;
        }
        flashAmount = 0f;
        SetFloat(FlashAmountID, 0f);
        flashRoutine = null;
    }

    // ---- fill / silhouette recolor ----

    public void SetFill(Color color, float amount01)
    {
        fillColor = color;
        fillAmount = amount01;
        SetColor(FillColorID, color);
        SetFloat(FillAmountID, amount01);
    }

    // ---- hue / saturation / brightness / contrast ----

    public void SetHSBC(float hueDegrees, float saturationValue, float brightnessValue, float contrastValue)
    {
        hue = hueDegrees;
        saturation = saturationValue;
        brightness = brightnessValue;
        contrast = contrastValue;
        SetFloat(HueID, hueDegrees);
        SetFloat(SaturationID, saturationValue);
        SetFloat(BrightnessID, brightnessValue);
        SetFloat(ContrastID, contrastValue);
    }

    // ---- grayscale ----

    public void SetGrayscale(float amount01)
    {
        grayscaleAmount = amount01;
        SetFloat(GrayscaleAmountID, amount01);
    }

    // ---- edge glow rim ----

    public void SetRim(Color color, float widthPx, float intensity)
    {
        rimColor = color;
        rimWidth = widthPx;
        rimIntensity = intensity;
        SetColor(RimColorID, color);
        SetFloat(RimWidthID, widthPx);
        SetFloat(RimIntensityID, intensity);
    }

    // ---- shine sweep ----

    public void SetShine(Color color, float bandWidth01, float angleDegrees, float speed, float intensity, bool loop = true)
    {
        shineColor = color;
        shineWidth = bandWidth01;
        shineAngle = angleDegrees;
        shineSpeed = speed;
        shineIntensity = intensity;
        shineLoop = loop;
        SetColor(ShineColorID, color);
        SetFloat(ShineWidthID, bandWidth01);
        SetFloat(ShineAngleID, angleDegrees);
        SetFloat(ShineSpeedID, speed);
        SetFloat(ShineIntensityID, intensity);
        SetFloat(ShineLoopID, loop ? 1f : 0f);
    }

    // ---- chromatic aberration ----

    public void SetChromaticAmount(float amountPx)
    {
        chromaticAmount = amountPx;
        SetFloat(ChromaticAmountID, amountPx);
    }

    // ---- pixelation ----

    public void SetPixelSize(float sizePx)
    {
        pixelSize = sizePx;
        SetFloat(PixelSizeID, sizePx);
    }

    // ---- wave distortion ----

    public void SetWave(float amplitudeUV, float frequency, float speed, bool vertical = false)
    {
        waveAmplitude = amplitudeUV;
        waveFrequency = frequency;
        waveSpeed = speed;
        waveVertical = vertical;
        SetFloat(WaveAmplitudeID, amplitudeUV);
        SetFloat(WaveFrequencyID, frequency);
        SetFloat(WaveSpeedID, speed);
        SetFloat(WaveVerticalID, vertical ? 1f : 0f);
    }
}
