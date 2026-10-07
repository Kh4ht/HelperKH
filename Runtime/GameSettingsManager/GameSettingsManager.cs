// GameSettingsManager.cs
// A persistent, singleton settings manager covering the graphics/performance options a
// shipped game's Settings menu typically needs: FPS cap, VSync, quality level, resolution
// + fullscreen mode, anti-aliasing, post-processing on/off, and automatic throttling while
// the game is in the background. Every setter here both applies the change immediately AND
// saves it to PlayerPrefs, and everything is re-applied automatically on startup.
//
// Attach this to one GameObject in your first/boot scene. It marks itself
// DontDestroyOnLoad and is a singleton, so any other script (including your Settings UI)
// can just call GameSettingsManager.Instance.SetCustomFPS(120) etc. from anywhere.
//
// POST-PROCESSING NOTES
// ---------------------------------------------------------------------------------------
//   - Built-in pipeline + Post Processing Stack v2: works automatically (the package
//     defines UNITY_POST_PROCESSING_STACK_V2 itself).
//   - URP: add USING_URP to Project Settings > Player > Scripting Define Symbols.
//   - HDRP / custom pipelines: not handled directly - listen to PostProcessingChanged and
//     toggle your Volumes (weight/enabled) yourself.
//   - If no symbol is defined, the setting still saves and fires the event, but nothing
//     changes on screen.
//
// WHY THIS IS "OPTIMIZATION"
// ---------------------------------------------------------------------------------------
// Capping frame rate / enabling VSync doesn't make any single frame cheaper to render -
// it stops the engine from rendering MORE frames than anyone can see or than the display
// can show. That still matters a lot in practice:
//   - Uncapped FPS in a simple menu/UI scene can hit hundreds or thousands of fps, which
//     needlessly maxes out the GPU, drains battery, and generates heat for zero visual
//     benefit. Capping it (or enabling VSync) fixes that "free" waste.
//   - VSync eliminates screen tearing at the cost of a small amount of input latency.
//   - A lower FPS cap while the game is in the background/minimized saves real power on
//     laptops and mobile, since nothing there is even visible.
//   - Quality level / resolution / anti-aliasing / post-processing are the actual
//     "reduce GPU workload" levers - letting the player trade visual fidelity for frame
//     rate on weaker hardware.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

[AddComponentMenu("KH/Settings/" + nameof(KHGameSettingsManager)), DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)] // Apply saved settings before other scripts read them.
public class KHGameSettingsManager : MonoBehaviour
{
    public static KHGameSettingsManager Instance { get; private set; }

    // ---- common FPS cap presets for a Settings UI dropdown; 0 = Unlimited ----
    public static readonly int[] FpsCapOptions = { 30, 60, 90, 120, 144, 240, 0 };

    private const int MinFpsCap = 10; // Guards against an accidental 0/negative freeze.
    private const int BackgroundFps = 15; // Throttled rate while unfocused/minimized.

    private const string PrefKeyFpsCap = "Settings_FpsCap";
    private const string PrefKeyVSync = "Settings_VSyncCount";
    private const string PrefKeyQuality = "Settings_QualityLevel";
    private const string PrefKeyAntiAliasing = "Settings_AntiAliasing";
    private const string PrefKeyResWidth = "Settings_ResWidth";
    private const string PrefKeyResHeight = "Settings_ResHeight";
    private const string PrefKeyFullscreenMode = "Settings_FullscreenMode";
    private const string PrefKeyPostProcessing = "Settings_PostProcessing";

    // ---- current state (read-only from outside; use the SetX methods to change these) ----
    public int CurrentFpsCap { get; private set; } = 60; // 0 = Unlimited
    public int CurrentVSyncCount { get; private set; } = 1; // 0 = off, 1 = full, 2 = half refresh
    public int CurrentQualityLevel { get; private set; }
    public int CurrentAntiAliasing { get; private set; } // 0, 2, 4 or 8
    public bool CurrentPostProcessingEnabled { get; private set; } = true;

    /// <summary>Raised after post-processing is toggled. Hook this if you use a custom
    /// pipeline or want to flip your own Volumes/effects.</summary>
    public event Action<bool> PostProcessingChanged;

    private int fpsCapBeforeBackground;
    private bool isThrottledInBackground;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cameras in newly loaded scenes don't know about the setting, so re-apply it
        // every time a scene loads.
        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadAndApplySavedSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyPostProcessing();
    }

    // =====================================================================================
    // Frame rate + VSync
    // =====================================================================================

    /// <summary>Enables standard VSync. vSyncCount 1 = match monitor refresh rate,
    /// 2 = half refresh rate (e.g. 30fps on a 60Hz screen, uses less power/heat).</summary>
    public void SetVSync(int vSyncCount = 1)
    {
        vSyncCount = Mathf.Clamp(vSyncCount, 0, 2);
        CurrentVSyncCount = vSyncCount;
        QualitySettings.vSyncCount = vSyncCount;

        // VSync takes priority over targetFrameRate when vSyncCount > 0 on most platforms,
        // but mobile in particular can ignore vSyncCount entirely - always keep
        // targetFrameRate sane too rather than assuming VSync alone will be honored.
        Application.targetFrameRate = vSyncCount > 0 ? -1 : CurrentFpsCap;

        PlayerPrefs.SetInt(PrefKeyVSync, vSyncCount);
        PlayerPrefs.Save();
    }

    /// <summary>Sets a custom FPS cap and disables VSync (VSync and a custom cap are
    /// mutually exclusive - VSync overrides targetFrameRate on platforms that honor it).
    /// Pass 0 (or anything &lt;= 0) for "Unlimited".</summary>
    public void SetCustomFPS(int targetFPS)
    {
        CurrentVSyncCount = 0;
        QualitySettings.vSyncCount = 0;

        CurrentFpsCap = targetFPS <= 0 ? 0 : Mathf.Max(targetFPS, MinFpsCap);
        Application.targetFrameRate = CurrentFpsCap == 0 ? -1 : CurrentFpsCap;

        PlayerPrefs.SetInt(PrefKeyVSync, 0);
        PlayerPrefs.SetInt(PrefKeyFpsCap, CurrentFpsCap);
        PlayerPrefs.Save();
    }

    // =====================================================================================
    // Quality level (QualitySettings' built-in Low/Medium/High/Ultra tiers, or whatever
    // tiers your project defines in Project Settings > Quality)
    // =====================================================================================

    public string[] GetQualityLevelNames() => QualitySettings.names;

    public void SetQualityLevel(int index, bool applyExpensiveChanges = true)
    {
        index = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        CurrentQualityLevel = index;
        QualitySettings.SetQualityLevel(index, applyExpensiveChanges);

        PlayerPrefs.SetInt(PrefKeyQuality, index);
        PlayerPrefs.Save();
    }

    // =====================================================================================
    // Anti-aliasing (MSAA sample count: 0 = off, 2, 4, or 8)
    // =====================================================================================

    public void SetAntiAliasing(int sampleCount)
    {
        if (sampleCount != 0 && sampleCount != 2 && sampleCount != 4 && sampleCount != 8)
            sampleCount = 0;

        CurrentAntiAliasing = sampleCount;
        QualitySettings.antiAliasing = sampleCount;

        PlayerPrefs.SetInt(PrefKeyAntiAliasing, sampleCount);
        PlayerPrefs.Save();
    }

    // =====================================================================================
    // Resolution + fullscreen mode
    // =====================================================================================

    /// <summary>Every resolution the current display supports, for a Settings UI dropdown.
    /// Can contain duplicates at different refresh rates - dedupe by width/height yourself
    /// if you only want one entry per resolution.</summary>
    public Resolution[] GetAvailableResolutions() => Screen.resolutions;

    public void SetResolution(int width, int height, FullScreenMode fullscreenMode)
    {
        Screen.SetResolution(width, height, fullscreenMode);

        PlayerPrefs.SetInt(PrefKeyResWidth, width);
        PlayerPrefs.SetInt(PrefKeyResHeight, height);
        PlayerPrefs.SetInt(PrefKeyFullscreenMode, (int)fullscreenMode);
        PlayerPrefs.Save();
    }

    public void SetFullscreenMode(FullScreenMode fullscreenMode)
    {
        SetResolution(Screen.width, Screen.height, fullscreenMode);
    }

    // =====================================================================================
    // Post-processing (bloom, color grading, SSAO, etc.) - one of the most expensive
    // per-pixel costs, so a toggle is a big win on weaker GPUs.
    // =====================================================================================

    public void SetPostProcessing(bool enabled)
    {
        CurrentPostProcessingEnabled = enabled;
        ApplyPostProcessing();

        PlayerPrefs.SetInt(PrefKeyPostProcessing, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void TogglePostProcessing() => SetPostProcessing(!CurrentPostProcessingEnabled);

    /// <summary>Pushes the current setting onto every camera in the loaded scenes. Call this
    /// yourself if you spawn a camera at runtime after the scene has loaded.</summary>
    public void ApplyPostProcessing()
    {
        bool enabled = CurrentPostProcessingEnabled;

#if UNITY_POST_PROCESSING_STACK_V2
        // Built-in pipeline: disabling the PostProcessLayer skips the whole stack.
        // (On Unity 2023+, swap in FindObjectsByType<T>(FindObjectsInactive.Include,
        // FindObjectsSortMode.None) to silence the deprecation warning.)
        foreach (var layer in FindObjectsOfType<PostProcessLayer>(true))
            layer.enabled = enabled;
#endif

#if USING_URP
        // URP: per-camera "Post Processing" checkbox.
        foreach (var cam in FindObjectsOfType<Camera>(true))
        {
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = enabled;
        }
#endif

        PostProcessingChanged?.Invoke(enabled);
    }

    // =====================================================================================
    // Background throttling - saves real power/heat by capping FPS hard while the game
    // isn't actually visible (minimized, alt-tabbed, or on mobile backgrounded).
    // =====================================================================================

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) EnterBackgroundThrottle();
        else ExitBackgroundThrottle();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) EnterBackgroundThrottle();
        else ExitBackgroundThrottle();
    }

    private void EnterBackgroundThrottle()
    {
        if (isThrottledInBackground) return;
        isThrottledInBackground = true;
        fpsCapBeforeBackground = CurrentFpsCap;

        // Bypass SetCustomFPS() here so we don't overwrite the player's saved preference -
        // this is a temporary throttle, not a settings change.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = BackgroundFps;
    }

    private void ExitBackgroundThrottle()
    {
        if (!isThrottledInBackground) return;
        isThrottledInBackground = false;

        // Restore whatever the player actually had set (VSync or a custom cap).
        if (CurrentVSyncCount > 0) SetVSync(CurrentVSyncCount);
        else SetCustomFPS(fpsCapBeforeBackground);
    }

    // =====================================================================================
    // Persistence
    // =====================================================================================

    private void LoadAndApplySavedSettings()
    {
        int savedVSync = PlayerPrefs.GetInt(PrefKeyVSync, 1);
        int savedFpsCap = PlayerPrefs.GetInt(PrefKeyFpsCap, 60);
        int savedQuality = PlayerPrefs.GetInt(PrefKeyQuality, QualitySettings.GetQualityLevel());
        int savedAA = PlayerPrefs.GetInt(PrefKeyAntiAliasing, QualitySettings.antiAliasing);

        if (savedVSync > 0) SetVSync(savedVSync);
        else SetCustomFPS(savedFpsCap);

        SetQualityLevel(savedQuality);
        SetAntiAliasing(savedAA);
        SetPostProcessing(PlayerPrefs.GetInt(PrefKeyPostProcessing, 1) == 1);

        // Only restore a saved resolution if one was actually saved before (a width of 0
        // means "never saved" - leave the OS/engine's chosen default resolution alone).
        int savedWidth = PlayerPrefs.GetInt(PrefKeyResWidth, 0);
        if (savedWidth > 0)
        {
            int savedHeight = PlayerPrefs.GetInt(PrefKeyResHeight, Screen.height);
            var savedMode = (FullScreenMode)PlayerPrefs.GetInt(PrefKeyFullscreenMode, (int)Screen.fullScreenMode);
            SetResolution(savedWidth, savedHeight, savedMode);
        }
    }

    /// <summary>Wipes every saved setting back to first-run defaults and re-applies them
    /// immediately. Wire this to a "Reset to Defaults" button in your Settings UI.</summary>
    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(PrefKeyFpsCap);
        PlayerPrefs.DeleteKey(PrefKeyVSync);
        PlayerPrefs.DeleteKey(PrefKeyQuality);
        PlayerPrefs.DeleteKey(PrefKeyAntiAliasing);
        PlayerPrefs.DeleteKey(PrefKeyResWidth);
        PlayerPrefs.DeleteKey(PrefKeyResHeight);
        PlayerPrefs.DeleteKey(PrefKeyFullscreenMode);
        PlayerPrefs.DeleteKey(PrefKeyPostProcessing);
        PlayerPrefs.Save();

        LoadAndApplySavedSettings();
    }
}