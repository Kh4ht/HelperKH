using System;
using System.Collections.Generic;
using KH;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Add this to your main Canvas. Modal KHUIElements find it automatically and:
///  - are tracked in a stack (most recently shown = top),
///  - share ONE backdrop that always sits directly behind the top element,
///  - close one at a time on backdrop click / Escape (only the top one).
/// It also owns the reusable confirmation modal (see Confirm).
/// </summary>
[AddComponentMenu("KH/UI/" + nameof(KHUIManager)), DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
public class KHUIManager : KHManagedBehaviour, IKHManagedUpdate
{
    #region FIELDS

    [Header("Backdrop")]
    [Tooltip("Optional. Assign your own Image (color, sprite, blur material) to customize it. If empty, a black one is created automatically.")]
    [SerializeField] private Image backdrop;

    [Header("Input")]
    [Tooltip("Close the top modal element when Escape is pressed (new Input System). To use other inputs, call HandleBack() from your own input action.")]
    [SerializeField] private bool handleEscapeKey = true;

    [Header("Confirmation")]
    [Tooltip("Your confirmation modal prefab (root needs KHConfirmModal + a modal KHUIElement). Instantiated once, on first use.")]
    [SerializeField] private KHConfirmModal confirmModalPrefab;

    private readonly List<KHUIElement> stack = new();
    private Tween backdropTween;
    private bool initialized;

    private readonly Queue<KHConfirmRequest> confirmQueue = new();
    private KHConfirmModal confirmModal;
    private bool confirmBusy;

    #endregion
    #region UNITY EVENTS

    private void Awake()
    {
        EnsureInitialized();
    }

    public void KHUpdate()
    {
        if (stack.Count == 0)
            return;

        // An element was destroyed/deactivated without going through KH_Hide.
        if (PruneStack())
            RefreshBackdrop(0.1f, false);

        // Keyboard.current is null on devices without a keyboard (phones, consoles).
        if (handleEscapeKey && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleBack();
    }

    #endregion
    #region PUBLIC API

    /// <summary>The currently top-most open modal element, or null.</summary>
    public KHUIElement Top
    {
        get
        {
            PruneStack();
            return stack.Count > 0 ? stack[stack.Count - 1] : null;
        }
    }

    public bool IsAnyOpen => Top != null;

    /// <summary>
    /// Back/Escape handling. Returns true if a modal element was open (the press is "consumed"),
    /// so you can skip your own back logic (e.g. opening a pause menu).
    /// </summary>
    public bool HandleBack()
    {
        KHUIElement top = Top;
        if (top == null)
            return false;

        if (top.CloseOnBackKey)
            top.KH_Hide();

        return true;
    }

    public void HideTop()
    {
        KHUIElement top = Top;
        if (top != null)
            top.KH_Hide();
    }

    public void HideAll()
    {
        PruneStack();
        KHUIElement[] copy = stack.ToArray();

        for (int i = copy.Length - 1; i >= 0; i--)
            copy[i].KH_Hide();
    }

    // Called by KHUIElement (modal elements only).
    public void RegisterShown(KHUIElement element, float duration, bool instant = false)
    {
        EnsureInitialized();

        stack.Remove(element);
        stack.Add(element);

        if (!instant && element.BringToFrontOnShow)
            element.transform.SetAsLastSibling();

        RefreshBackdrop(duration, instant);
    }

    // Called by KHUIElement (modal elements only).
    public void RegisterHidden(KHUIElement element, float duration, bool instant = false)
    {
        EnsureInitialized();

        if (stack.Remove(element))
            RefreshBackdrop(duration, instant);
    }

    #endregion
    #region CONFIRMATION

    /// <summary>
    /// Shows the confirmation modal. If one is already open, the request is queued and shown after it closes.
    /// Callbacks run after the modal has fully closed. Backdrop click / Escape count as Cancel.
    /// Null or empty texts: title/message are hidden, button labels use the prefab's defaults.
    /// </summary>
    public void Confirm(string message, Action onConfirm, Action onCancel = null,
        string confirmText = null, string cancelText = null)
    {
        Confirm(new KHConfirmRequest
        {
            Message = message,
            OnConfirm = onConfirm,
            OnCancel = onCancel,
            ConfirmText = confirmText,
            CancelText = cancelText
        });
    }

    public void Confirm(KHConfirmRequest request)
    {
        if (request == null)
            return;

        confirmQueue.Enqueue(request);
        TryShowNextConfirm();
    }

    private void TryShowNextConfirm()
    {
        // The modal instance was destroyed while busy: don't stay stuck.
        if (confirmModal == null)
            confirmBusy = false;

        if (confirmBusy || confirmQueue.Count == 0)
            return;

        if (!EnsureConfirmModal())
        {
            confirmQueue.Clear();
            return;
        }

        confirmBusy = true;
        confirmModal.Open(confirmQueue.Dequeue());
    }

    private bool EnsureConfirmModal()
    {
        if (confirmModal != null)
            return true;

        if (confirmModalPrefab == null)
        {
            Debug.LogError($"{nameof(KHUIManager)}: no confirmation modal prefab assigned.", this);
            return false;
        }

        confirmModal = Instantiate(confirmModalPrefab, transform);
        confirmModal.Setup(OnConfirmClosed);
        return true;
    }

    private void OnConfirmClosed()
    {
        confirmBusy = false;
        TryShowNextConfirm();
    }

    #endregion
    #region PRIVATE

    private void EnsureInitialized()
    {
        if (initialized)
            return;
        initialized = true;

        if (backdrop == null)
            CreateBackdrop();

        if (!backdrop.TryGetComponent(out Button button))
            button = backdrop.gameObject.AddComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.targetGraphic = backdrop;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.AddListener(OnBackdropClicked);

        SetBackdropAlpha(0f);
        backdrop.gameObject.SetActive(false);
    }

    private void CreateBackdrop()
    {
        GameObject go = new GameObject("KH_Backdrop", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        backdrop = go.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0f);
    }

    private void OnBackdropClicked()
    {
        KHUIElement top = Top;

        // A top element that doesn't allow click-outside still blocks the clicks (true modal).
        if (top != null && top.CloseOnBackdropClick)
            top.KH_Hide();
    }

    // Removes entries that were destroyed or deactivated behind our back.
    private bool PruneStack()
    {
        bool changed = false;

        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i] == null || !stack[i].gameObject.activeInHierarchy)
            {
                stack.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    private void RefreshBackdrop(float duration, bool instant)
    {
        PruneStack();

        if (backdropTween.isAlive)
            backdropTween.Stop();

        if (stack.Count == 0)
        {
            // Stop catching clicks immediately, fade out, then deactivate.
            backdrop.raycastTarget = false;
            FadeBackdrop(0f, duration, instant, deactivateAtEnd: true);
            return;
        }

        KHUIElement top = stack[stack.Count - 1];

        PlaceBehind(top.transform);

        // Fade in from 0 only when it was fully hidden; otherwise continue from current alpha.
        if (!backdrop.gameObject.activeSelf)
        {
            SetBackdropAlpha(0f);
            backdrop.gameObject.SetActive(true);
        }

        backdrop.raycastTarget = true;
        FadeBackdrop(top.BackdropAlpha, duration, instant, deactivateAtEnd: false);
    }

    private void FadeBackdrop(float target, float duration, bool instant, bool deactivateAtEnd)
    {
        // Nothing to animate: set directly (avoids PrimeTween's "endValue equals current" warning).
        if (instant || Mathf.Approximately(backdrop.color.a, target))
        {
            SetBackdropAlpha(target);
            if (deactivateAtEnd)
                backdrop.gameObject.SetActive(false);
            return;
        }

        backdropTween = Tween.Alpha(
            backdrop,
            endValue: target,
            duration: duration,
            useUnscaledTime: true);

        if (deactivateAtEnd)
            backdropTween.OnComplete(() => backdrop.gameObject.SetActive(false));
    }

    // Puts the backdrop in the target's parent, directly behind it.
    private void PlaceBehind(Transform target)
    {
        Transform parent = target.parent;
        if (parent == null)
            return;

        Transform bd = backdrop.transform;

        if (bd.parent != parent)
            bd.SetParent(parent, false);

        int targetIndex = target.GetSiblingIndex();
        int backdropIndex = bd.GetSiblingIndex();

        // SetSiblingIndex removes the object first, so the index depends on which side it starts on.
        bd.SetSiblingIndex(backdropIndex > targetIndex ? targetIndex : targetIndex - 1);
    }

    private void SetBackdropAlpha(float alpha)
    {
        Color c = backdrop.color;
        c.a = alpha;
        backdrop.color = c;
    }

    #endregion
}
