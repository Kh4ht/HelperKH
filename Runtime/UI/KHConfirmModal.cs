using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KH.Enums;
using KH;

/// <summary>
/// Data for one confirmation dialog. Empty texts fall back to what the prefab already contains
/// (labels) or hide the element (title / message).
/// </summary>
public class KHConfirmRequest
{
    public string Message;
    public string ConfirmText;
    public string CancelText;
    public Action OnConfirm;
    public Action OnCancel;
}

/// <summary>
/// Put this on the root of your confirmation prefab (next to a modal KHUIElement) and assign the
/// text/button references. KHUIManager instantiates the prefab once and reuses it.
/// Backdrop click, Escape and any other way of closing count as Cancel.
/// </summary>
[AddComponentMenu("KH/UI/" + nameof(KHConfirmModal)), DisallowMultipleComponent, RequireComponent(typeof(KHUIElement))]
public class KHConfirmModal : KHManagedBehaviour
{
    #region FIELDS

    [Header("Texts")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private TMP_Text cancelLabel;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private KHUIElement element;
    private Action onClosed;
    private KHConfirmRequest current;
    private bool confirmed;
    private bool initialized;

    // Label texts that were in the prefab, used when a request doesn't specify its own.
    private string defaultConfirmText;
    private string defaultCancelText;

    #endregion
    #region PUBLIC

    /// <summary>
    /// Called once by KHUIManager after instantiating the prefab.
    /// onClosed fires after the user's callbacks, so the manager can show the next queued request.
    /// </summary>
    public void Setup(Action onClosed)
    {
        Initialize();
        this.onClosed = onClosed;

        // A freshly instantiated active prefab starts "shown" (and registered in the manager's stack).
        // Reset it to hidden, instantly.
        element.Hide(KHUIAnimation.None);
    }

    public void Open(KHConfirmRequest request)
    {
        Initialize();

        current = request;
        confirmed = false;

        SetText(messageText, request.Message);

        if (confirmLabel != null)
            confirmLabel.text = string.IsNullOrEmpty(request.ConfirmText) ? defaultConfirmText : request.ConfirmText;

        if (cancelLabel != null)
            cancelLabel.text = string.IsNullOrEmpty(request.CancelText) ? defaultCancelText : request.CancelText;

        element.KH_Show();

        // Texts changed size: make sure layout groups / size fitters are up to date.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    #endregion
    #region PRIVATE

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;

        element = GetComponent<KHUIElement>();

        if (confirmLabel != null)
            defaultConfirmText = confirmLabel.text;
        if (cancelLabel != null)
            defaultCancelText = cancelLabel.text;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Single completion point for every way of closing (buttons, backdrop, Escape, HideAll...).
        element.onHidden.AddListener(OnHidden);
    }

    private void OnConfirmClicked()
    {
        // Ignore clicks while already closing, so the first choice wins.
        if (current == null || !element.IsShown)
            return;

        confirmed = true;
        element.KH_Hide();
    }

    private void OnCancelClicked()
    {
        if (current == null || !element.IsShown)
            return;

        element.KH_Hide();
    }

    // Runs after the hide animation, outside of any OnDisable, so callbacks may safely open another confirmation.
    private void OnHidden()
    {
        if (current == null)
            return; // the initial reset in Setup

        KHConfirmRequest request = current;
        bool result = confirmed;

        current = null;
        confirmed = false;

        try
        {
            if (result)
                request.OnConfirm?.Invoke();
            else
                request.OnCancel?.Invoke();
        }
        finally
        {
            onClosed?.Invoke();
        }
    }

    // Empty text hides the element, so an unused title doesn't leave a gap.
    private static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        bool hasText = !string.IsNullOrEmpty(value);
        target.gameObject.SetActive(hasText);

        if (hasText)
            target.text = value;
    }

    #endregion
}
