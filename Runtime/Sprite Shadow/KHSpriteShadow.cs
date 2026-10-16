using UnityEngine;

namespace KH
{
    /// <summary>
    /// Mirrors the SpriteRenderer found on a parent object onto this object's SpriteRenderer.
    /// Position/scale/rotation of the shadow are controlled by this object's own Transform.
    /// Does nothing if no parent SpriteRenderer exists. Works in edit mode.
    /// </summary>

    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("KH/Systems/" + nameof(KHSpriteShadow))]
    [ExecuteAlways, DisallowMultipleComponent]
    public class KHSpriteShadow : KHManagedBehaviour, IKHManagedLateUpdate
    {
        #region FIELDS

        [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0.4f);

        [Tooltip("Multiply the shadow alpha by the source alpha (fades, death animations, etc.).")]
        [SerializeField] private bool inheritSourceAlpha = true;

        [SerializeField] private int sortingOrderOffset = -1;

        private SpriteRenderer shadow;
        private SpriteRenderer source;

        #endregion
        #region UNITY EVENTS

        private void Reset() => Refresh();

        private void Awake() => Refresh();

        private void OnValidate() => Refresh();

        private void OnTransformParentChanged() => FindSource();

        public void KHLateUpdate() => Sync();

#if UNITY_EDITOR
        private void LateUpdate()
        {
            if (!Application.isPlaying)
                Sync();
        }
#endif

        #endregion
        #region PRIVATE

        private void Refresh()
        {
            if (shadow == null)
                shadow = GetComponent<SpriteRenderer>();

            transform.localScale = new Vector3(1f, 0.5f, 1f);
            transform.localPosition = Vector3.zero;

            FindSource();
            Sync();
        }

        private void FindSource()
        {
            // GetComponentInParent also checks the object itself, so start from the parent.
            source = transform.parent != null
                ? transform.parent.GetComponentInParent<SpriteRenderer>(true)
                : null;
        }

        private void Sync()
        {
            if (shadow == null)
                return;

            if (source == null)
                return;

            if (shadow.sprite != source.sprite) shadow.sprite = source.sprite;
            if (shadow.flipX != source.flipX) shadow.flipX = source.flipX;
            if (shadow.flipY != source.flipY) shadow.flipY = source.flipY;

            bool visible = source.enabled && source.gameObject.activeInHierarchy;
            if (shadow.enabled != visible) shadow.enabled = visible;

            if (shadow.sortingLayerID != source.sortingLayerID)
                shadow.sortingLayerID = source.sortingLayerID;

            int order = source.sortingOrder + sortingOrderOffset;
            if (shadow.sortingOrder != order) shadow.sortingOrder = order;

            Color c = shadowColor;
            if (inheritSourceAlpha) c.a *= source.color.a;
            if (shadow.color != c) shadow.color = c;
        }

        #endregion

    }
}