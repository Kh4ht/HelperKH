using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KH
{
    /// <summary>A scene transition effect. Cover hides the screen, Reveal shows the new scene.</summary>
    public interface IKHSceneTransition
    {
        IEnumerator Cover(RectTransform root);
        IEnumerator Reveal(RectTransform root);
    }

    /// <summary>
    /// Base class for custom transitions authored as assets. Put one in a Resources folder
    /// named "KHSceneTransition" to replace the default fade for the whole game.
    /// </summary>
    public abstract class KHSceneTransitionAsset : ScriptableObject, IKHSceneTransition
    {
        public abstract IEnumerator Cover(RectTransform root);
        public abstract IEnumerator Reveal(RectTransform root);
    }

    /// <summary>Default transition: a solid colour fade. Builds its own visuals, no setup needed.</summary>
    public class KHFadeTransition : IKHSceneTransition
    {
        private readonly float duration;
        private readonly Color color;
        private CanvasGroup group;

        public KHFadeTransition(float duration = 0.25f, Color? color = null)
        {
            this.duration = duration;
            this.color = color ?? Color.black;
        }

        public IEnumerator Cover(RectTransform root)
        {
            Ensure(root);
            yield return Animate(0f, 1f);
        }

        public IEnumerator Reveal(RectTransform root)
        {
            Ensure(root);
            yield return Animate(1f, 0f);
        }

        private void Ensure(RectTransform root)
        {
            if (group != null)
                return;

            var go = new GameObject("Fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(root, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = color;
            group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        private IEnumerator Animate(float from, float to)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            group.alpha = to;
        }
    }

    [DisallowMultipleComponent]
    public sealed class KHSceneTransitionManager : MonoBehaviour
    {
        #region FIELDS

        [KHResetStatic]
        public static KHSceneTransitionManager Ins { get; private set; }

        /// <summary>Used when Load is called without an explicit transition.</summary>
        public static IKHSceneTransition DefaultTransition { get; set; }

        public bool IsTransitioning { get; private set; }

        /// <summary>Fired once the screen is fully covered, just before the load starts.</summary>
        public event Action Covered;

        /// <summary>Fired once the new scene is active, just before the reveal.</summary>
        public event Action SceneReady;

        private const int SORTING_ORDER = short.MaxValue;

        private RectTransform root;
        private CanvasGroup blocker;

        #endregion
        #region BOOTSTRAP

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Ins != null)
                return;

            // A designer-made asset wins; otherwise fall back to the built-in black fade.
            DefaultTransition = Resources.Load<KHSceneTransitionAsset>("KHSceneTransition")
                                ?? (IKHSceneTransition)new KHFadeTransition();

            new GameObject(nameof(KHSceneTransitionManager)).AddComponent<KHSceneTransitionManager>();
        }

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            if (Ins != null && Ins != this)
            {
                Destroy(gameObject);
                return;
            }

            Ins = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        #endregion
        #region PRIVATE

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;

            blocker = canvasObject.GetComponent<CanvasGroup>();
            blocker.blocksRaycasts = false;

            root = (RectTransform)canvasObject.transform;
        }

        private IEnumerator Run(Func<AsyncOperation> startLoad, IKHSceneTransition transition)
        {
            IsTransitioning = true;
            blocker.blocksRaycasts = true;

            transition ??= DefaultTransition;

            yield return transition.Cover(root);
            Covered?.Invoke();

            // Load while hidden. Hold activation at 90% so the swap happens exactly once.
            AsyncOperation op = startLoad();
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            op.allowSceneActivation = true;
            yield return op;

            yield return null; // let the new scene's Awake/Start run before revealing
            SceneReady?.Invoke();

            yield return transition.Reveal(root);

            blocker.blocksRaycasts = false;
            IsTransitioning = false;
        }

        #endregion
        #region PUBLIC

        public bool Load(string sceneName, IKHSceneTransition transition = null)
        {
            if (IsTransitioning)
                return false;

            StartCoroutine(Run(() => SceneManager.LoadSceneAsync(sceneName), transition));
            return true;
        }

        public bool Load(int buildIndex, IKHSceneTransition transition = null)
        {
            if (IsTransitioning)
                return false;

            StartCoroutine(Run(() => SceneManager.LoadSceneAsync(buildIndex), transition));
            return true;
        }

        public bool Reload(IKHSceneTransition transition = null) =>
            Load(SceneManager.GetActiveScene().buildIndex, transition);

        #endregion
    }
}