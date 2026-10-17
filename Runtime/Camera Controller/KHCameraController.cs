using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KH
{
    /// <summary>
    /// Pan / zoom / follow / focus / shake controller for 2D orthographic cameras.
    /// Works with a plain camera or with <see cref="KHPixelCamera"/> on the same object.
    /// Add to the camera. Needs the Input System package.
    /// </summary>
    [AddComponentMenu("KH/Systems/" + nameof(KHCameraController))]
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public sealed class KHCameraController : KHManagedBehaviour, IKHManagedLateUpdate
    {
        #region FIELDS

        [Header("General")]
        [Tooltip("Ignore mouse input while the pointer is over UI.")]
        [SerializeField] private bool ignoreWhenOverUI = true;

        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [Tooltip("World-space rectangle the view must stay inside.")]
        [SerializeField] private Rect worldBounds = new(-20f, -11.25f, 40f, 22.5f);
        [Tooltip("Zooming out stops when the view would become larger than the bounds.")]
        [SerializeField] private bool limitZoomOutToBounds = true;

        [Header("Drag Pan")]
        [SerializeField] private bool canDrag = true;
        [SerializeField] private bool dragWithRightButton = true;
        [SerializeField] private bool dragWithMiddleButton = true;
        [SerializeField] private bool dragWithLeftButton = false;

        [Header("Keyboard Pan")]
        [SerializeField] private bool keyboardPan = true;
        [Tooltip("Screen heights per second. Zoom-independent, so it feels the same at every zoom level.")]
        [SerializeField, Min(0f)] private float keyboardPanSpeed = 0.8f;

        [Header("Edge Pan")]
        [SerializeField] private bool edgePan = false;
        [SerializeField, Min(1f)] private float edgeSizePixels = 12f;
        [SerializeField, Min(0f)] private float edgePanSpeed = 0.8f;

        [Header("Zoom")]
        [SerializeField] private bool canZoom = true;
        [Tooltip("Multiplier. 1 = the view the scene starts with, 2 = twice as close.")]
        [SerializeField, Min(0.01f)] private float minZoom = 0.5f;
        [SerializeField, Min(0.01f)] private float maxZoom = 4f;
        [Tooltip("Zoom change per scroll step (0.15 = 15%).")]
        [SerializeField, Range(0.01f, 1f)] private float zoomStep = 0.15f;
        [SerializeField] private bool zoomTowardsCursor = true;
        [SerializeField] private bool invertScroll = false;

        [Header("Smoothing (0 = instant)")]
        [SerializeField, Min(0f)] private float panSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.08f;

        [Header("Follow")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector2 followOffset;
        [Tooltip("Manual panning stops following. When off, panning is locked while following.")]
        [SerializeField] private bool releaseFollowOnPan = true;

        private Camera cam;
        private KHPixelCamera pixelCam;
        private bool initialized;

        // View at zoom = 1.
        private float baseHeight;
        private float pixelAspect;

        // Actual (rendered) state and target state.
        private Vector2 position, targetPos, velocity;
        private float zoom, targetZoom, zoomVelocity;

        private Vector2 initialPos;
        private float initialZoom;

        private bool isDragging;
        private Vector2 lastDragScreen;

        private float shakeMagnitude, shakeTimeLeft, shakeDuration;

        #endregion
        #region PROPERTIES

        /// <summary>Disable to block all player input (cutscenes, menus). Code control still works.</summary>
        public bool InputEnabled { get; set; } = true;

        public float Zoom => zoom;
        public float TargetZoom => targetZoom;
        public Vector2 Position => position;
        public Transform FollowTarget => followTarget;

        private float Aspect => pixelCam != null ? pixelAspect : cam.aspect;

        private Vector2 RenderedViewSize => pixelCam != null
            ? pixelCam.VisibleWorldSize
            : new Vector2(2f * cam.orthographicSize * cam.aspect, 2f * cam.orthographicSize);

        private float EffectiveMinZoom
        {
            get
            {
                float min = minZoom;

                if (useBounds && limitZoomOutToBounds && worldBounds.width > 0f && worldBounds.height > 0f)
                    min = Mathf.Max(min, baseHeight / worldBounds.height, baseHeight * Aspect / worldBounds.width);

                return min;
            }
        }

        #endregion
        #region UNITY EVENTS

        private void Awake()
        {
            cam = GetComponent<Camera>();
            TryGetComponent(out pixelCam);

            if (!cam.orthographic && pixelCam == null)
                Debug.LogWarning($"{nameof(KHCameraController)} is built for orthographic cameras.", this);
        }

        public void KHLateUpdate()
        {
            if (!initialized && !TryInitialize())
                return;

            float dt = Time.unscaledDeltaTime;

            if (InputEnabled)
                HandleInput(dt);

            if (followTarget != null)
                targetPos = (Vector2)followTarget.position + followOffset;

            targetZoom = ClampZoom(targetZoom);
            targetPos = ClampPosition(targetPos, ViewSizeAt(targetZoom));

            Smooth(dt);

            // Final clamp guarantees the view never shows outside the bounds, even mid-animation.
            position = ClampPosition(position, ViewSizeAt(zoom));

            Apply(dt);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }

        #endregion
        #region INITIALIZATION

        private bool TryInitialize()
        {
            if (pixelCam != null)
            {
                // KHPixelCamera lays itself out in its own LateUpdate; wait until it has.
                Vector2 visible = pixelCam.VisibleWorldSize;
                if (visible.y <= 0.001f)
                    return false;

                baseHeight = visible.y * pixelCam.Zoom;
                pixelAspect = visible.x / visible.y;
                zoom = pixelCam.Zoom;
            }
            else
            {
                baseHeight = cam.orthographicSize * 2f;
                zoom = 1f;
            }

            zoom = targetZoom = ClampZoom(zoom);
            position = targetPos = (Vector2)transform.position;
            initialPos = position;
            initialZoom = zoom;

            initialized = true;
            return true;
        }

        #endregion
        #region INPUT

        private void HandleInput(float dt)
        {
            bool overUI = ignoreWhenOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                HandleDrag(mouse, overUI);

                if (canZoom && !overUI)
                    HandleScroll(mouse);

                if (edgePan && !overUI)
                    HandleEdgePan(mouse, dt);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboardPan)
                HandleKeyboard(keyboard, dt);
        }

        private bool DragButton(Mouse mouse, bool pressedThisFrame)
        {
            if (dragWithRightButton && (pressedThisFrame ? mouse.rightButton.wasPressedThisFrame : mouse.rightButton.isPressed)) return true;
            if (dragWithMiddleButton && (pressedThisFrame ? mouse.middleButton.wasPressedThisFrame : mouse.middleButton.isPressed)) return true;
            if (dragWithLeftButton && (pressedThisFrame ? mouse.leftButton.wasPressedThisFrame : mouse.leftButton.isPressed)) return true;
            return false;
        }

        private void HandleDrag(Mouse mouse, bool overUI)
        {
            if (!canDrag)
            {
                isDragging = false;
                return;
            }

            Vector2 screenPos = mouse.position.ReadValue();

            if (!isDragging && !overUI && DragButton(mouse, true) && TryTakePanControl())
            {
                isDragging = true;
                lastDragScreen = screenPos;
                return;
            }

            if (!isDragging)
                return;

            if (!DragButton(mouse, false))
            {
                isDragging = false;
                return;
            }

            Vector2 screenDelta = lastDragScreen - screenPos;
            lastDragScreen = screenPos;

            if (screenDelta == Vector2.zero)
                return; // holding still: nothing moves

            // World units per screen pixel. Position cancels out in the difference, so this is
            // pure scale and doesn't depend on where the camera currently is.
            Vector2 worldPerPixel = ScreenToWorld(Vector2.one) - ScreenToWorld(Vector2.zero);

            targetPos += screenDelta * worldPerPixel;
            velocity = Vector2.zero;
        }

        private void HandleScroll(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            // Only the sign is used: scroll magnitudes differ between platforms and devices.
            float direction = Mathf.Sign(scroll) * (invertScroll ? -1f : 1f);
            float newZoom = ClampZoom(targetZoom * Mathf.Pow(1f + zoomStep, direction));

            if (zoomTowardsCursor)
                ZoomAnchored(newZoom, mouse.position.ReadValue());
            else
                targetZoom = newZoom;
        }

        private void HandleKeyboard(Keyboard keyboard, float dt)
        {
            Vector2 dir = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1f;

            if (dir == Vector2.zero || !TryTakePanControl())
                return;

            targetPos += dir.normalized * (keyboardPanSpeed * ViewSizeAt(targetZoom).y * dt);
        }

        private void HandleEdgePan(Mouse mouse, float dt)
        {
            if (!Application.isFocused)
                return;

            Vector2 m = mouse.position.ReadValue();
            if (m.x < 0f || m.y < 0f || m.x > Screen.width || m.y > Screen.height)
                return;

            Vector2 dir = Vector2.zero;
            if (m.x <= edgeSizePixels) dir.x = -1f;
            else if (m.x >= Screen.width - edgeSizePixels) dir.x = 1f;
            if (m.y <= edgeSizePixels) dir.y = -1f;
            else if (m.y >= Screen.height - edgeSizePixels) dir.y = 1f;

            if (dir == Vector2.zero || !TryTakePanControl())
                return;

            targetPos += dir.normalized * (edgePanSpeed * ViewSizeAt(targetZoom).y * dt);
        }

        /// <summary>Returns true if the player may pan now (and stops following if configured to).</summary>
        private bool TryTakePanControl()
        {
            if (followTarget == null)
                return true;

            if (!releaseFollowOnPan)
                return false;

            followTarget = null;
            return true;
        }

        #endregion
        #region CORE

        private Vector2 ViewSizeAt(float z)
        {
            float h = baseHeight / z;
            return new Vector2(h * Aspect, h);
        }

        private float ClampZoom(float z)
        {
            float min = EffectiveMinZoom;
            return Mathf.Clamp(z, min, Mathf.Max(maxZoom, min));
        }

        private Vector2 ClampPosition(Vector2 pos, Vector2 viewSize)
        {
            if (!useBounds || worldBounds.width <= 0f || worldBounds.height <= 0f)
                return pos;

            return new Vector2(ClampAxis(pos.x, viewSize.x * 0.5f, worldBounds.xMin, worldBounds.xMax),
                               ClampAxis(pos.y, viewSize.y * 0.5f, worldBounds.yMin, worldBounds.yMax));
        }

        private static float ClampAxis(float value, float halfView, float min, float max)
        {
            float lo = min + halfView;
            float hi = max - halfView;

            // View larger than the bounds on this axis: centre it.
            return lo > hi ? (min + max) * 0.5f : Mathf.Clamp(value, lo, hi);
        }

        /// <summary>Zooms so the world point under <paramref name="screenPos"/> stays under it.</summary>
        private void ZoomAnchored(float newZoom, Vector2 screenPos)
        {
            Vector2 view = RenderedViewSize;
            if (view.x <= 0f || view.y <= 0f)
            {
                targetZoom = newZoom;
                return;
            }

            // Where the cursor sits inside the view, as an offset from the centre (-0.5..0.5).
            Vector2 p = ScreenToWorld(screenPos);
            Vector2 c = transform.position;
            Vector2 offset = new((p.x - c.x) / view.x, (p.y - c.y) / view.y);

            Vector2 before = ViewSizeAt(targetZoom);
            Vector2 after = ViewSizeAt(newZoom);

            Vector2 anchor = targetPos + new Vector2(offset.x * before.x, offset.y * before.y);
            targetPos = anchor - new Vector2(offset.x * after.x, offset.y * after.y);
            targetZoom = newZoom;
        }

        private void Smooth(float dt)
        {
            if (zoomSmoothTime > 0f)
            {
                // Smoothed in log space so zooming in and out feel symmetrical.
                float logZoom = Mathf.SmoothDamp(Mathf.Log(zoom), Mathf.Log(targetZoom), ref zoomVelocity,
                                                 zoomSmoothTime, Mathf.Infinity, dt);
                zoom = Mathf.Exp(logZoom);
            }
            else
                zoom = targetZoom;

            if (panSmoothTime > 0f && !isDragging)
                position = Vector2.SmoothDamp(position, targetPos, ref velocity, panSmoothTime, Mathf.Infinity, dt);
            else
            {
                position = targetPos;
                velocity = Vector2.zero;
            }
        }

        private void Apply(float dt)
        {
            Vector2 shake = Vector2.zero;

            if (shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= dt;
                float falloff = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
                shake = Random.insideUnitCircle * (shakeMagnitude * falloff);
            }

            Vector3 p = transform.position;
            transform.position = new Vector3(position.x + shake.x, position.y + shake.y, p.z);

            if (pixelCam != null)
                pixelCam.Zoom = zoom;
            else
                cam.orthographicSize = baseHeight * 0.5f / zoom;
        }

        #endregion
        #region PUBLIC

        /// <summary>Mouse/screen position to world position. Use this instead of Camera.ScreenToWorldPoint.</summary>
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (pixelCam != null)
                return pixelCam.ScreenToWorld(screenPos);

            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        }

        /// <summary>Smoothly move to a world position, optionally changing zoom (zoom &lt;= 0 keeps the current one).</summary>
        public void FocusOn(Vector2 worldPos, float newZoom = 0f)
        {
            followTarget = null;
            targetPos = worldPos;

            if (newZoom > 0f)
                targetZoom = ClampZoom(newZoom);
        }

        /// <summary>Jump instantly to a world position, optionally changing zoom.</summary>
        public void SnapTo(Vector2 worldPos, float newZoom = 0f)
        {
            followTarget = null;

            if (newZoom > 0f)
                zoom = targetZoom = ClampZoom(newZoom);

            position = targetPos = ClampPosition(worldPos, ViewSizeAt(zoom));
            velocity = Vector2.zero;
        }

        public void SetZoom(float newZoom, bool instant = false)
        {
            targetZoom = ClampZoom(newZoom);

            if (instant)
                zoom = targetZoom;
        }

        public void Follow(Transform target, Vector2 offset = default)
        {
            followTarget = target;
            followOffset = offset;
        }

        public void StopFollowing() => followTarget = null;

        public void SetBounds(Rect bounds, bool enable = true)
        {
            worldBounds = bounds;
            useBounds = enable;
        }

        /// <summary>Back to the position and zoom the scene started with.</summary>
        public void ResetView(bool instant = false)
        {
            if (instant)
                SnapTo(initialPos, initialZoom);
            else
                FocusOn(initialPos, initialZoom);
        }

        /// <summary>Camera shake. A stronger shake replaces a weaker one that is still running.</summary>
        public void Shake(float magnitude, float duration)
        {
            if (duration <= 0f)
                return;

            float currentStrength = shakeDuration > 0f ? shakeMagnitude * (shakeTimeLeft / shakeDuration) : 0f;
            if (magnitude < currentStrength)
                return;

            shakeMagnitude = magnitude;
            shakeDuration = shakeTimeLeft = duration;
        }

        #endregion
    }
}