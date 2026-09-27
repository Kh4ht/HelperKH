using PrimeTween;
using UnityEngine;
using KH;
using UnityEngine.UI;

[AddComponentMenu("KH/UI/" + nameof(KHUIController)), DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
public class KHUIController : KHManagedBehaviour
{
    #region Fields

    private const float UI_SHOW_SPEED = 0.2f;
    private const float UI_HIDE_SPEED = 0.15f;
    private const float WAIT_BEFORE_INTERACTABLE_TIME = 0.1f;
    private static readonly WaitForSeconds waitTime = new(0.1f);

    // Original Values
    private Vector3 originalScale;
    private Vector3 originalPos;
    private Sprite originalSprite;

    // Components
    private Image image;
    private CanvasGroup canvasGroup;
    private RectTransform parentCanvasRect;

    // Tween
    private Tween activeTween;

    // Logical visibility state. This is updated the INSTANT Show/Hide is
    // requested, unlike gameObject.activeInHierarchy which only flips to
    // false once the hide tween's OnComplete fires (after UI_HIDE_SPEED
    // seconds). Using the Unity active flag as the toggle guard caused
    // rapid clicks landing inside that animation window to be misread,
    // since the object still reported "active" while it was mid-close.
    private bool isShown;

    #endregion
    #region UNITY EVENTS

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (TryGetComponent(out Image img))
        {
            image = img;
            originalSprite = image.sprite;
        }

        originalScale = transform.localScale;
        originalPos = transform.localPosition;

        parentCanvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    protected override void Start()
    {
        base.Start();

        isShown = gameObject.activeInHierarchy;
    }

    #endregion
    #region PRIVATE

    private void RestoreDefaults(bool waitBeforeInteractable = false)
    {
        transform.localScale = originalScale;
        transform.localPosition = originalPos;

        canvasGroup.alpha = 1;

        if (waitBeforeInteractable)
            Tween.Delay(duration: WAIT_BEFORE_INTERACTABLE_TIME,
                        onComplete: () => canvasGroup.interactable = true,
                        useUnscaledTime: true);

        else
            canvasGroup.interactable = true;
    }

    #endregion
    #region PUBLIC

    protected bool GetIsShown() => isShown;
    protected void SetIsShown(bool newValue)
    {
        if (newValue == isShown)
            return;

        isShown = newValue;
    }

    public void KH_ToggleSprite(Sprite sprite)
    {
        if (image == null)
        {
            Debug.LogWarning($"Component {nameof(Image)} is NULL");
            return;
        }

        if (image.sprite != sprite)
            image.sprite = sprite;
        else
            image.sprite = originalSprite;
    }

    public void KH_ToggleInteractable()
    {
        canvasGroup.interactable = !canvasGroup.interactable;
    }

    #endregion
    #region POP

    public void KH_PopShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);
        canvasGroup.interactable = false;

        transform.localScale = Vector3.zero;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.Scale(
            transform,
            endValue: originalScale,
            duration: UI_SHOW_SPEED,
            ease: Ease.OutBack,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_PopHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.Scale(
            transform,
            endValue: Vector3.zero,
            duration: UI_HIDE_SPEED,
            ease: Ease.InBack,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_PopToggle()
    {
        if (isShown)
            KH_PopHide();
        else
            KH_PopShow();
    }

    #endregion
    #region LEFT

    public void KH_LeftShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);

        transform.localPosition = originalPos + Vector3.left * parentCanvasRect.rect.width;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos,
            duration: UI_SHOW_SPEED,
            ease: Ease.OutCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_LeftHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos + Vector3.left * parentCanvasRect.rect.width,
            duration: UI_HIDE_SPEED,
            ease: Ease.InCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_LeftToggle()
    {
        if (isShown)
            KH_LeftHide();
        else
            KH_LeftShow();
    }

    #endregion
    #region RIGHT

    public void KH_RightShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);
        canvasGroup.interactable = true;

        transform.localPosition = originalPos + Vector3.right * parentCanvasRect.rect.width;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos,
            duration: UI_SHOW_SPEED,
            ease: Ease.OutCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_RightHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos + Vector3.right * parentCanvasRect.rect.width,
            duration: UI_HIDE_SPEED,
            ease: Ease.InCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_RightToggle()
    {
        if (isShown)
            KH_RightHide();
        else
            KH_RightShow();
    }

    #endregion
    #region UP

    public void KH_UpShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);
        canvasGroup.interactable = true;

        transform.localPosition = originalPos + Vector3.up * parentCanvasRect.rect.width;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos,
            duration: UI_SHOW_SPEED,
            ease: Ease.OutCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_UpHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos + Vector3.up * parentCanvasRect.rect.width,
            duration: UI_HIDE_SPEED,
            ease: Ease.InCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_UpToggle()
    {
        if (isShown)
            KH_UpHide();
        else
            KH_UpShow();
    }

    #endregion
    #region DOWN

    public void KH_DownShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);
        canvasGroup.interactable = true;

        transform.localPosition = originalPos + Vector3.down * parentCanvasRect.rect.width;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos,
            duration: UI_SHOW_SPEED,
            ease: Ease.OutCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_DownHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.LocalPosition(
            transform,
            endValue: originalPos + Vector3.down * parentCanvasRect.rect.width,
            duration: UI_HIDE_SPEED,
            ease: Ease.InCubic,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_DownToggle()
    {
        if (isShown)
            KH_DownHide();
        else
            KH_DownShow();
    }

    #endregion
    #region FADE

    public void KH_FadeShow()
    {
        if (isShown)
            return;
        isShown = true;

        gameObject.SetActive(true);

        canvasGroup.alpha = 0;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.Alpha(
            canvasGroup,
            endValue: 1f,
            duration: UI_SHOW_SPEED,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults(true);
        });
    }

    public void KH_FadeHide()
    {
        if (!isShown)
            return;
        isShown = false;

        canvasGroup.interactable = false;

        if (activeTween.isAlive)
        {
            activeTween.Stop();
        }
        activeTween = Tween.Alpha(
            canvasGroup,
            endValue: 0f,
            duration: UI_HIDE_SPEED,
            useUnscaledTime: true
        ).OnComplete(() =>
        {
            RestoreDefaults();
            gameObject.SetActive(false);
        });
    }

    public void KH_FadeToggle()
    {
        if (isShown)
            KH_FadeHide();
        else
            KH_FadeShow();
    }

    #endregion
}