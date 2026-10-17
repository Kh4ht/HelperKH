using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace KH
{
    /// <summary>
    /// Renders the camera at 1 art pixel = 1 texel and presents it with sharp-bilinear scaling.
    /// Fixed visible area (letterboxed), smooth sub-pixel camera movement, zoom via <see cref="Zoom"/>.
    /// Add to the main camera. Replaces the URP Pixel Perfect Camera component.
    /// </summary>
    [AddComponentMenu("KH/Systems/" + nameof(KHPixelCamera))]
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    [DefaultExecutionOrder(short.MaxValue)]
    public sealed class KHPixelCamera : KHManagedBehaviour, IKHManagedLateUpdate
    {
        #region FIELDS

        public static KHPixelCamera Ins { get; private set; }

        [SerializeField] private int assetsPPU = 64;

        [Tooltip("Visible area at Zoom = 1, in texels. Its aspect ratio is the fixed aspect of the game view.")]
        [SerializeField] private Vector2Int referenceResolution = new(1280, 720);

        [SerializeField, Min(0.01f)] private float zoom = 1f;
        [SerializeField] private Vector2 zoomRange = new(0.5f, 4f);

        [Tooltip("Zooming out stops when one art pixel would be smaller than this many screen pixels. " +
                 "1 = never smaller than a screen pixel. Raise it (e.g. 2) to keep pixels from looking tiny on big maps.")]
        [SerializeField, Min(1f)] private float minScreenPixelsPerTexel = 1f;

        private Camera cam;
        private RenderTexture rt;
        private RawImage view;
        private Material material;
        private GameObject overlay;

        // Layout (recomputed in LateUpdate when something changes).
        private int texW, texH;             // visible texels, always even
        private int lastScreenW, lastScreenH;
        private Rect screenRect;            // where the image is drawn, in screen pixels
        private float pixelScale;           // screen pixels per texel

        // Camera snapping during render.
        private Vector3 savedPosition;
        private bool positionSwapped;

        #endregion
        #region PROPERTIES

        public float Zoom
        {
            get => zoom;
            set => zoom = Mathf.Clamp(value, zoomRange.x, zoomRange.y);
        }

        /// <summary>World units visible on screen at the current zoom.</summary>
        public Vector2 VisibleWorldSize => new Vector2(texW, texH) / assetsPPU;

        /// <summary>Screen pixels per art pixel at the current zoom and resolution.</summary>
        public float PixelScale => pixelScale;

        #endregion
        #region UNITY EVENTS

        protected override void OnEnable()
        {
            base.OnEnable();

            cam = GetComponent<Camera>();
            cam.orthographic = true;
            zoom = Mathf.Clamp(zoom, zoomRange.x, zoomRange.y);

            Shader shader = Resources.Load<Shader>("KHSharpBilinear");
            if (shader == null)
            {
                Debug.LogError("KHSharpBilinear.shader must be inside a Resources folder. Falling back to UI/Default (blurry).", this);
                shader = Shader.Find("UI/Default");
            }

            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            BuildOverlay();
            Ins = this;

            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;

            RefreshLayout(force: true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;

            if (positionSwapped)
            {
                transform.position = savedPosition;
                positionSwapped = false;
            }

            if (cam != null)
                cam.targetTexture = null;

            if (rt != null)
            {
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }

            if (overlay != null) Destroy(overlay);
            if (material != null) Destroy(material);
            if (Ins == this) Ins = null;
        }

        public void KHLateUpdate() => RefreshLayout(force: false);

        #endregion
        #region RENDER HOOKS

        // Snap the camera to the texel grid only while it renders, and show the
        // leftover fraction by shifting the visible window inside the texture.
        private void OnBeginCamera(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || rt == null)
                return;

            Vector3 p = transform.position;
            savedPosition = p;
            positionSwapped = true;

            float sx = Mathf.Round(p.x * assetsPPU) / assetsPPU;
            float sy = Mathf.Round(p.y * assetsPPU) / assetsPPU;
            transform.position = new Vector3(sx, sy, p.z);

            // Fraction in texels, within [-0.5, 0.5]. The texture has a 1-texel margin on every side.
            float fx = (p.x - sx) * assetsPPU;
            float fy = (p.y - sy) * assetsPPU;

            float rtW = texW + 2f, rtH = texH + 2f;
            view.uvRect = new Rect((1f + fx) / rtW, (1f + fy) / rtH, texW / rtW, texH / rtH);
        }

        private void OnEndCamera(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || !positionSwapped)
                return;

            transform.position = savedPosition;
            positionSwapped = false;
        }

        #endregion
        #region PRIVATE

        private static int EvenRound(float value) => Mathf.Max(2, Mathf.RoundToInt(value / 2f) * 2);

        private void BuildOverlay()
        {
            overlay = new GameObject("KHPixelCameraOverlay", typeof(RectTransform), typeof(Canvas));
            overlay.transform.SetParent(transform, false);

            // Lowest order: every other overlay canvas (HUD, scene transitions) draws on top, at full resolution.
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MinValue;

            // Solid background, so the bars are black and the screen never shows stale pixels.
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(overlay.transform, false);

            var bgRect = (RectTransform)background.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            var bg = background.GetComponent<Image>();
            bg.color = Color.black;
            bg.raycastTarget = false;

            var viewObject = new GameObject("View", typeof(RectTransform), typeof(RawImage));
            viewObject.transform.SetParent(overlay.transform, false);

            var rect = (RectTransform)viewObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;

            view = viewObject.GetComponent<RawImage>();
            view.material = material;
            view.raycastTarget = false;
        }

        private void RefreshLayout(bool force)
        {
            int sw = Screen.width, sh = Screen.height;
            if (sw <= 0 || sh <= 0)
                return;

            float designAspect = (float)referenceResolution.x / referenceResolution.y;

            // Largest design-aspect rectangle that fits the screen (letterbox / pillarbox).
            int fitH = Mathf.Min(sh, Mathf.FloorToInt(sw / designAspect));

            // Visible texels for this zoom, capped so pixels never get smaller than the minimum.
            int h = EvenRound(referenceResolution.y / Mathf.Clamp(zoom, zoomRange.x, zoomRange.y));
            int maxH = Mathf.FloorToInt(fitH / minScreenPixelsPerTexel) & ~1;
            h = Mathf.Max(16, Mathf.Min(h, maxH));
            int w = EvenRound(h * designAspect);

            if (!force && w == texW && h == texH && sw == lastScreenW && sh == lastScreenH)
                return;

            texW = w;
            texH = h;
            lastScreenW = sw;
            lastScreenH = sh;

            pixelScale = fitH / (float)h;
            int dispW = Mathf.Min(sw, Mathf.RoundToInt(pixelScale * w));
            screenRect = new Rect((sw - dispW) / 2, (sh - fitH) / 2, dispW, fitH); // whole pixels

            // Render texture with a 1-texel margin on every side (used for the sub-texel shift).
            int rtW = w + 2, rtH = h + 2;

            RenderTexture old = rt;
            rt = RenderTexture.GetTemporary(rtW, rtH, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            rt.filterMode = FilterMode.Bilinear; // the shader relies on hardware bilinear
            rt.wrapMode = TextureWrapMode.Clamp;
            cam.targetTexture = rt;
            if (old != null)
                RenderTexture.ReleaseTemporary(old);

            // 1 world unit = assetsPPU texels, so sprites map 1:1 to texels at every zoom.
            cam.orthographicSize = rtH / (2f * assetsPPU);

            view.texture = rt;
            RectTransform rect = view.rectTransform;
            rect.anchoredPosition = screenRect.position;
            rect.sizeDelta = screenRect.size;

            material.SetFloat("_Scale", pixelScale);
            material.SetVector("_TexSize", new Vector4(rtW, rtH, 0f, 0f));
        }

        #endregion
        #region PUBLIC

        /// <summary>Use this instead of Camera.ScreenToWorldPoint (see notes).</summary>
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            float nx = (screenPos.x - screenRect.x) / screenRect.width;
            float ny = (screenPos.y - screenRect.y) / screenRect.height;
            Vector3 c = transform.position;

            return new Vector2(c.x + (nx - 0.5f) * texW / assetsPPU,
                               c.y + (ny - 0.5f) * texH / assetsPPU);
        }

        /// <summary>Use this instead of Camera.WorldToScreenPoint.</summary>
        public Vector2 WorldToScreen(Vector2 worldPos)
        {
            Vector3 c = transform.position;
            float nx = (worldPos.x - c.x) * assetsPPU / texW + 0.5f;
            float ny = (worldPos.y - c.y) * assetsPPU / texH + 0.5f;

            return new Vector2(screenRect.x + nx * screenRect.width,
                               screenRect.y + ny * screenRect.height);
        }

        #endregion
    }
}