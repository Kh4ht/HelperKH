using System.Collections;
using PrimeTween;
using UnityEngine;
using KH;
using UnityEngine.UI;

[AddComponentMenu("KH/KHUI Controller"), DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
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
        if (!gameObject.activeInHierarchy)
        {
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
    }

    public void KH_PopHide()
    {
        if (gameObject.activeInHierarchy)
        {
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
    }

    public void KH_PopToggle()
    {
        KH_PopHide();
        KH_PopShow();
    }

    #endregion
    #region LEFT

    public void KH_LeftShow()
    {
        if (!gameObject.activeInHierarchy)
        {
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
    }

    public void KH_LeftHide()
    {
        if (gameObject.activeInHierarchy)
        {
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
    }

    public void KH_LeftToggle()
    {
        KH_LeftHide();
        KH_LeftShow();
    }

    #endregion
    #region RIGHT

    public void KH_RightShow()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);

            canvasGroup.interactable = true;
            gameObject.SetActive(true);

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
    }

    public void KH_RightHide()
    {
        if (gameObject.activeInHierarchy)
        {
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
    }

    public void KH_RightToggle()
    {
        KH_RightHide();
        KH_RightShow();
    }

    #endregion
    #region UP

    public void KH_UpShow()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);

            canvasGroup.interactable = true;
            gameObject.SetActive(true);

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
    }

    public void KH_UpHide()
    {
        if (gameObject.activeInHierarchy)
        {
            canvasGroup.interactable = false;

            if (activeTween.isAlive)
            {
                activeTween.Stop();
            }
            if (parentCanvasRect == null)
                Debug.Log("parentCanvasRect == null ");
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
    }

    public void KH_UpToggle()
    {
        KH_UpHide();
        KH_UpShow();
    }

    #endregion
    #region DOWN

    public void KH_DownShow()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);

            canvasGroup.interactable = true;
            gameObject.SetActive(true);

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
    }

    public void KH_DownHide()
    {
        if (gameObject.activeInHierarchy)
        {
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
    }

    public void KH_DownToggle()
    {
        KH_DownHide();
        KH_DownShow();
    }

    #endregion
    #region FADE

    public void KH_FadeShow()
    {
        if (!gameObject.activeInHierarchy)
        {
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
    }

    public void KH_FadeHide()
    {
        if (gameObject.activeInHierarchy)
        {
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
    }

    public void KH_FadeToggle()
    {
        KH_FadeHide();
        KH_FadeShow();
    }

    #endregion
}