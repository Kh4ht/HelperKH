// SettingsWindow.cs
// Binds the settings menu UI to KHGameSettingsManager. Put this on the root of your
// settings window and drag each control into its slot. Any slot you leave empty is
// simply skipped, so you can ship a menu with only some of the options.
//
// Uses TextMeshPro dropdowns. For legacy UI, replace TMP_Dropdown with UnityEngine.UI.Dropdown
// (the API used here is identical) and drop the "using TMPro;" line.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("KH/Settings/" + nameof(SettingsWindow)), DisallowMultipleComponent]
public class SettingsWindow : MonoBehaviour
{
    [Header("Frame rate")]
    [SerializeField] private TMP_Dropdown fpsDropdown;          // "VSync" + FPS cap presets

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown antiAliasingDropdown; // Off / 2x / 4x / 8x
    [SerializeField] private Toggle postProcessingToggle;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown fullscreenModeDropdown;

    [Header("Buttons")]
    [SerializeField] private Button resetButton;

    // Index in these arrays == index in the matching dropdown.
    private static readonly int[] AntiAliasingValues = { 0, 2, 4, 8 };
    private static readonly string[] AntiAliasingLabels = { "Off", "2x MSAA", "4x MSAA", "8x MSAA" };

    private static readonly FullScreenMode[] FullscreenModes =
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.ExclusiveFullScreen, // Windows only; the engine falls back elsewhere.
        FullScreenMode.Windowed
    };
    private static readonly string[] FullscreenLabels = { "Borderless", "Fullscreen", "Windowed" };

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    private KHGameSettingsManager Manager => KHGameSettingsManager.Instance;

    // =====================================================================================
    // Lifecycle
    // =====================================================================================

    private void OnEnable()
    {
        if (Manager == null)
        {
            Debug.LogError($"{nameof(SettingsWindow)}: no {nameof(KHGameSettingsManager)} found.", this);
            return;
        }

        PopulateOptions();
        Refresh();
        Subscribe();

        Manager.SettingsReset += OnSettingsReset;
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (Manager != null)
            Manager.SettingsReset -= OnSettingsReset;
    }

    // =====================================================================================
    // Listeners (added in OnEnable, removed in OnDisable so nothing is registered twice)
    // =====================================================================================

    private void Subscribe()
    {
        if (fpsDropdown) fpsDropdown.onValueChanged.AddListener(OnFpsChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (antiAliasingDropdown) antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
        if (postProcessingToggle) postProcessingToggle.onValueChanged.AddListener(OnPostProcessingChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (fullscreenModeDropdown) fullscreenModeDropdown.onValueChanged.AddListener(OnFullscreenModeChanged);
        if (resetButton) resetButton.onClick.AddListener(OnResetClicked);
    }

    private void Unsubscribe()
    {
        if (fpsDropdown) fpsDropdown.onValueChanged.RemoveListener(OnFpsChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (antiAliasingDropdown) antiAliasingDropdown.onValueChanged.RemoveListener(OnAntiAliasingChanged);
        if (postProcessingToggle) postProcessingToggle.onValueChanged.RemoveListener(OnPostProcessingChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (fullscreenModeDropdown) fullscreenModeDropdown.onValueChanged.RemoveListener(OnFullscreenModeChanged);
        if (resetButton) resetButton.onClick.RemoveListener(OnResetClicked);
    }

    // =====================================================================================
    // Filling the dropdowns with options
    // =====================================================================================

    private void PopulateOptions()
    {
        if (fpsDropdown)
        {
            var options = new List<string> { "VSync" };
            foreach (int fps in KHGameSettingsManager.FpsCapOptions)
                options.Add(fps == 0 ? "Unlimited" : fps + " FPS");
            SetOptions(fpsDropdown, options);
        }

        if (qualityDropdown)
            SetOptions(qualityDropdown, Manager.GetQualityLevelNames());

        if (antiAliasingDropdown)
            SetOptions(antiAliasingDropdown, AntiAliasingLabels);

        if (fullscreenModeDropdown)
            SetOptions(fullscreenModeDropdown, FullscreenLabels);

        if (resolutionDropdown)
            PopulateResolutions();
    }

    private void PopulateResolutions()
    {
        // Screen.resolutions repeats each size once per refresh rate, so dedupe by size.
        resolutions.Clear();
        foreach (var res in Manager.GetAvailableResolutions())
        {
            var size = new Vector2Int(res.width, res.height);
            if (!resolutions.Contains(size))
                resolutions.Add(size);
        }

        // Make sure the current size is selectable even if the display doesn't list it
        // (windowed mode, or a size set by a boot script).
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(current))
            resolutions.Add(current);

        var labels = new List<string>(resolutions.Count);
        foreach (var size in resolutions)
            labels.Add($"{size.x} x {size.y}");

        SetOptions(resolutionDropdown, labels);
    }

    private static void SetOptions(TMP_Dropdown dropdown, IEnumerable<string> labels)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(labels));
    }

    // =====================================================================================
    // Manager -> UI (SetValueWithoutNotify so refreshing never re-triggers the handlers)
    // =====================================================================================

    private void Refresh()
    {
        if (Manager == null) return;

        if (fpsDropdown)
            fpsDropdown.SetValueWithoutNotify(GetFpsDropdownIndex());

        if (qualityDropdown)
            qualityDropdown.SetValueWithoutNotify(Manager.CurrentQualityLevel);

        if (antiAliasingDropdown)
        {
            int index = System.Array.IndexOf(AntiAliasingValues, Manager.CurrentAntiAliasing);
            antiAliasingDropdown.SetValueWithoutNotify(Mathf.Max(index, 0));
        }

        if (postProcessingToggle)
            postProcessingToggle.SetIsOnWithoutNotify(Manager.CurrentPostProcessingEnabled);

        if (resolutionDropdown)
        {
            int index = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(index, 0));
        }

        if (fullscreenModeDropdown)
        {
            int index = System.Array.IndexOf(FullscreenModes, Screen.fullScreenMode);
            fullscreenModeDropdown.SetValueWithoutNotify(Mathf.Max(index, 0));
        }

        RefreshDropdownCaptions();
    }

    // Changing a value without notify doesn't always redraw the caption text.
    private void RefreshDropdownCaptions()
    {
        if (fpsDropdown) fpsDropdown.RefreshShownValue();
        if (qualityDropdown) qualityDropdown.RefreshShownValue();
        if (antiAliasingDropdown) antiAliasingDropdown.RefreshShownValue();
        if (resolutionDropdown) resolutionDropdown.RefreshShownValue();
        if (fullscreenModeDropdown) fullscreenModeDropdown.RefreshShownValue();
    }

    private int GetFpsDropdownIndex()
    {
        // Index 0 is "VSync"; the presets follow it.
        if (Manager.CurrentVSyncCount > 0) return 0;

        int presetIndex = System.Array.IndexOf(KHGameSettingsManager.FpsCapOptions, Manager.CurrentFpsCap);
        return presetIndex >= 0 ? presetIndex + 1 : 0;
    }

    // =====================================================================================
    // UI -> Manager
    // =====================================================================================

    private void OnFpsChanged(int index)
    {
        if (index == 0) Manager.SetVSync(1);
        else Manager.SetCustomFPS(KHGameSettingsManager.FpsCapOptions[index - 1]);
    }

    private void OnQualityChanged(int index) => Manager.SetQualityLevel(index);

    private void OnAntiAliasingChanged(int index) => Manager.SetAntiAliasing(AntiAliasingValues[index]);

    private void OnPostProcessingChanged(bool isOn) => Manager.SetPostProcessing(isOn);

    private void OnResolutionChanged(int index)
    {
        var size = resolutions[index];
        Manager.SetResolution(size.x, size.y, Screen.fullScreenMode);
    }

    private void OnFullscreenModeChanged(int index) => Manager.SetFullscreenMode(FullscreenModes[index]);

    private void OnResetClicked() => Manager.ResetToDefaults();

    // =====================================================================================
    // Reset handling
    // =====================================================================================

    private void OnSettingsReset()
    {
        // Screen.SetResolution only takes effect at the end of the frame, so Screen.width/
        // height are still the old values right now. Wait a frame before reading them.
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        PopulateResolutions();
        Refresh();
    }
}