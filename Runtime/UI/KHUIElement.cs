using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using KH;
using KH.Enums;

[AddComponentMenu("KH/UI/" + nameof(KHUIElement)), DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
public class KHUIElement : KHManagedBehaviour
{
    #region FIELDS

    // Default values
    private Vector3 defaultScale;
    private Vector3 defaultPos;
    private Sprite defaultSprite;

    // Components
    private Image image;
    private CanvasGroup canvasGroup;
    private RectTransform parentCanvasRect;
    private KHUIManager manager;

    // Tween state
    private Tween activeTween;
    private KHUIAnimation currentAnimation;

    // Logical visibility, updated the INSTANT Show/Hide is requested
    // (gameObject.activeInHierarchy only flips after the hide tween ends).
    private bool isShown;
    private bool initialized;

    // INSPECTOR
    [Header("Show")]
    [Tooltip("Animation played when this element is shown.")]
    [SerializeField] private KHUIAnimation showAnimation = KHUIAnimation.Pop;
    [Tooltip("Duration of the show animation in seconds.")]
    [SerializeField] private float showDuration = 0.2f;

    [Header("Hide")]
    [Tooltip("Animation played when this element is hidden.")]
    [SerializeField] private KHUIAnimation hideAnimation = KHUIAnimation.Pop;
    [Tooltip("Duration of the hide animation in seconds.")]
    [SerializeField] private float hideDuration = 0.15f;

    [Header("Modal (requires a KHUIManager on a parent Canvas)")]
    [Tooltip("Modal elements use the manager's shared backdrop and are tracked in its stack. Only the top one closes on click-outside / Escape. Untick for non-popup elements (HUD, tooltips...).")]
    [SerializeField] private bool isModal = true;
    [Tooltip("Hide this element when its backdrop is clicked (only while it is the top element).")]
    [SerializeField] private bool closeOnBackdropClick = true;
    [Tooltip("Hide this element on Escape / Back (only while it is the top element).")]
    [SerializeField] private bool closeOnBackKey = true;
    [Tooltip("Backdrop opacity while this element is on top. 0 = invisible, but it still blocks and catches clicks.")]
    [SerializeField, Range(0f, 1f)] private float backdropAlpha = 0.5f;
    [Tooltip("Move this element in front of its siblings when shown, so the most recently opened element is always on top.")]
    [SerializeField] private bool bringToFrontOnShow = true;

    [Header("Events")]
    public UnityEvent onShown;
    public UnityEvent onHidden;

    #endregion
    #region PROPERTIES

    public bool IsShown => isShown;
    public bool IsModal => isModal;
    public float BackdropAlpha => backdropAlpha;
    public bool BringToFrontOnShow => bringToFrontOnShow;
    public bool CloseOnBackdropClick => closeOnBackdropClick;
    public bool CloseOnBackKey => closeOnBackKey;

    private bool UsesManager => isModal && manager != null;

    #endregion
    #region UNITY EVENTS

    protected virtual void Awake()
    {
        Initialize();
    }

    protected override void Start()
    {
        base.Start();
        Initialize();
    }

    #endregion
    #region PRIVATE

    // Awake does not run on objects that start inactive, so Show can be called
    // before Awake. Initializing lazily makes that safe.
    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;

        canvasGroup = GetComponent<CanvasGroup>();

        if (TryGetComponent(out Image img))
        {
            image = img;
            defaultSprite = image.sprite;
        }

        defaultScale = transform.localScale;
        defaultPos = transform.localPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            parentCanvasRect = canvas.GetComponent<RectTransform>();

        manager = GetComponentInParent<KHUIManager>(true);
        if (isModal && manager == null)
            Debug.LogWarning($"{name} is modal but no {nameof(KHUIManager)} was found on a parent Canvas. Add one, or untick 'Is Modal'.", this);

        isShown = gameObject.activeInHierarchy;

        // Already visible at startup: join the manager's stack right away.
        if (isShown && UsesManager)
            manager.RegisterShown(this, 0f, true);
    }

    private void ResetPose()
    {
        transform.localScale = defaultScale;
        transform.localPosition = defaultPos;
        canvasGroup.alpha = 1f;
    }

    private void RestoreDefaults()
    {
        ResetPose();
        canvasGroup.interactable = true;
    }

    private Vector3 GetHiddenPosition(KHUIAnimation animation)
    {
        float width = parentCanvasRect != null ? parentCanvasRect.rect.width : Screen.width;
        float height = parentCanvasRect != null ? parentCanvasRect.rect.height : Screen.height;

        return animation switch
        {
            KHUIAnimation.SlideLeft => defaultPos + Vector3.left * width,
            KHUIAnimation.SlideRight => defaultPos + Vector3.right * width,
            KHUIAnimation.SlideUp => defaultPos + Vector3.up * height,
            KHUIAnimation.SlideDown => defaultPos + Vector3.down * height,
            _ => defaultPos
        };
    }

    private void ApplyHiddenPose(KHUIAnimation animation)
    {
        switch (animation)
        {
            case KHUIAnimation.Pop:
                transform.localScale = Vector3.zero;
                break;
            case KHUIAnimation.Fade:
                canvasGroup.alpha = 0f;
                break;
            case KHUIAnimation.SlideLeft:
            case KHUIAnimation.SlideRight:
            case KHUIAnimation.SlideUp:
            case KHUIAnimation.SlideDown:
                transform.localPosition = GetHiddenPosition(animation);
                break;
        }
    }

    private void Finish(bool show)
    {
        RestoreDefaults();

        if (!show)
            gameObject.SetActive(false);

        (show ? onShown : onHidden)?.Invoke();
    }

    private void Play(bool show, KHUIAnimation animation)
    {
        Initialize();

        if (isShown == show)
            return;
        isShown = show;

        bool wasAnimating = activeTween.isAlive;
        if (wasAnimating)
            activeTween.Stop();

        bool sameAnimation = wasAnimating && animation == currentAnimation;

        // Interrupted by a different animation type: start from a clean pose.
        if (wasAnimating && !sameAnimation)
            ResetPose();

        if (show)
            gameObject.SetActive(true);

        canvasGroup.interactable = false;
        currentAnimation = animation;

        // Tell the manager immediately (logical state), so the stack and backdrop update
        // the instant Show/Hide is requested, not when the animation ends.
        if (UsesManager)
        {
            bool instant = animation == KHUIAnimation.None;

            if (show)
                manager.RegisterShown(this, showDuration, instant);
            else
                manager.RegisterHidden(this, hideDuration, instant);
        }

        if (animation == KHUIAnimation.None)
        {
            Finish(show);
            return;
        }

        // Snap to the hidden pose only when showing from fully hidden.
        // If we interrupt the same animation mid-way, continue from where it is.
        if (show && !sameAnimation)
            ApplyHiddenPose(animation);

        float duration = show ? showDuration : hideDuration;

        switch (animation)
        {
            case KHUIAnimation.Pop:
                activeTween = Tween.Scale(
                    transform,
                    endValue: show ? defaultScale : Vector3.zero,
                    duration: duration,
                    ease: show ? Ease.OutBack : Ease.InBack,
                    useUnscaledTime: true);
                break;

            case KHUIAnimation.Fade:
                activeTween = Tween.Alpha(
                    canvasGroup,
                    endValue: show ? 1f : 0f,
                    duration: duration,
                    useUnscaledTime: true);
                break;

            default: // Slide*
                activeTween = Tween.LocalPosition(
                    transform,
                    endValue: show ? defaultPos : GetHiddenPosition(animation),
                    duration: duration,
                    ease: show ? Ease.OutCubic : Ease.InCubic,
                    useUnscaledTime: true);
                break;
        }

        activeTween.OnComplete(() => Finish(show));
    }

    #endregion
    #region PUBLIC

    public void KH_Show() => Play(true, showAnimation);
    public void KH_Hide() => Play(false, hideAnimation);

    public void KH_Toggle()
    {
        if (isShown)
            KH_Hide();
        else
            KH_Show();
    }

    // Code-only overloads to override the inspector animation (not visible in UnityEvent dropdowns).
    public void Show(KHUIAnimation animation) => Play(true, animation);
    public void Hide(KHUIAnimation animation) => Play(false, animation);

    public void KH_ToggleSprite(Sprite sprite)
    {
        Initialize();

        if (image == null)
        {
            Debug.LogWarning($"Component {nameof(Image)} is NULL", this);
            return;
        }

        image.sprite = image.sprite != sprite ? sprite : defaultSprite;
    }

    public void KH_ToggleInteractable()
    {
        Initialize();
        canvasGroup.interactable = !canvasGroup.interactable;
    }

    // Kept for subclasses that used the old protected API.
    protected bool GetIsShown() => isShown;
    protected void SetIsShown(bool newValue) => isShown = newValue;

    #endregion
}
