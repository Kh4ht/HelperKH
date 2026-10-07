using KH;
using UnityEngine;
using KH.Utils;
using KH.Enums;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates and maintains a child "shadow" SpriteRenderer that mirrors the source sprite.
/// The shadow child is persisted in the scene/prefab (serialized reference), so it is never duplicated
/// on domain reload / scene load, and it works identically in builds.
/// </summary>
[AddComponentMenu("KH/Systems/" + nameof(KHShadowGenerator)), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public class KHShadowGenerator : KHManagedBehaviour, IKHManagedUpdate
{
    private const string ShadowObjectName = "ShadowObject";

    #region FIELDS

    [Header("Shadow Look")]
    [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0.4f);
    [SerializeField] private Vector2 shadowScale = new(1f, 0.5f);
    [SerializeField] private Vector2 shadowOffset;

    [Tooltip("Optional. Leave empty to reuse the source sprite's material.")]
    [SerializeField] private Material shadowMaterial;

    [Tooltip("Multiply the shadow alpha by the source sprite alpha (fade in/out, death fades, etc.).")]
    [SerializeField] private bool inheritSourceAlpha = true;

    [Header("Sorting")]
    [SerializeField] private int sortingOrderOffset = -1;

    [Tooltip("Enable if the source's sorting layer/order changes at runtime (e.g. Y-sorting). " +
             "Sorting is always applied once on startup.")]
    [SerializeField] private bool syncSortingEveryFrame = true;

    // Serialized so the reference survives domain reloads, scene reloads, duplication and prefabs.
    [SerializeField, HideInInspector] private SpriteRenderer shadowRenderer;

    private SpriteRenderer spriteRenderer;

    // Last values pushed to the shadow. Lets KHUpdate skip redundant native property writes.
    private Sprite lastSprite;
    private bool lastFlipX;
    private bool lastFlipY;
    private bool lastEnabled;
    private float lastAlpha;
    private int lastSortingLayerId;
    private int lastSortingOrder;

#if UNITY_EDITOR
    private bool refreshQueued;
#endif

    #endregion
    #region UNITY EVENTS

    private void Reset()
    {
        RefreshShadow();
    }

    private void Awake()
    {
        RefreshShadow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // OnValidate must not create objects or touch the hierarchy directly.
        // Defer to after the inspector update, and coalesce multiple calls (e.g. dragging a slider).
        if (refreshQueued)
            return;

        refreshQueued = true;
        EditorApplication.delayCall += DeferredEditorRefresh;
    }

    private void DeferredEditorRefresh()
    {
        refreshQueued = false;

        // Destroyed in the meantime, or a prefab asset in the Project window (not a real scene object).
        if (this == null || !gameObject.scene.IsValid())
            return;

        CacheSource();
        ApplySettings(); // Only updates an existing shadow; creation happens in Reset / Awake / the button.
    }
#endif

    public void KHUpdate()
    {
        if (shadowRenderer == null || spriteRenderer == null)
            return;

        SyncFromSource(force: false);
    }

    #endregion
    #region PUBLIC API

    public void RefreshShadow()
    {
        CacheSource();
        EnsureShadow();
        ApplySettings();
    }

    public void SetShadowColor(Color color)
    {
        shadowColor = color;
        ApplySettings();
    }

    public void SetShadowScale(Vector2 scale)
    {
        shadowScale = scale;
        ApplySettings();
    }

    public void SetShadowOffset(Vector2 offset)
    {
        shadowOffset = offset;
        ApplySettings();
    }

    #endregion
    #region PRIVATE

    private void CacheSource()
    {
        if (spriteRenderer == null)
            TryGetComponent(out spriteRenderer);
    }

    /// <summary>
    /// Guarantees exactly one shadow renderer: reuses the serialized reference, re-adopts an
    /// existing child with the same name (legacy / lost reference), and only then creates a new one.
    /// </summary>
    private void EnsureShadow()
    {
        if (shadowRenderer != null)
            return;

        Transform existing = transform.Find(ShadowObjectName);
        if (existing != null && existing.TryGetComponent(out SpriteRenderer found))
        {
            shadowRenderer = found;
            return;
        }

        var shadowObject = new GameObject(ShadowObjectName) { layer = gameObject.layer };

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(shadowObject, "Create Shadow");
#endif

        shadowObject.transform.SetParent(transform, false);
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();

#if UNITY_EDITOR
        Debug.Log($"ShadowObject Created for {name}".AddColorTag(XMLColors.White), this);
#endif
    }

    /// <summary>Pushes all inspector settings to the shadow and forces a full resync.</summary>
    private void ApplySettings()
    {
        if (spriteRenderer == null || shadowRenderer == null)
            return;

        Transform shadowTransform = shadowRenderer.transform;
        shadowTransform.localScale = new Vector3(shadowScale.x, shadowScale.y, 1f);
        shadowTransform.SetLocalPositionAndRotation(shadowOffset, Quaternion.identity);

        shadowRenderer.sharedMaterial = shadowMaterial != null ? shadowMaterial : spriteRenderer.sharedMaterial;

        SyncFromSource(force: true);
        MarkDirty();
    }

    /// <summary>
    /// Mirrors the source renderer onto the shadow. Every write is guarded by a cached value,
    /// so a static sprite costs a handful of reads and zero writes per frame.
    /// </summary>
    private void SyncFromSource(bool force)
    {
        SpriteRenderer src = spriteRenderer;
        SpriteRenderer dst = shadowRenderer;

        Sprite sprite = src.sprite;
        if (force || sprite != lastSprite)
        {
            lastSprite = sprite;
            dst.sprite = sprite;
        }

        bool flipX = src.flipX;
        if (force || flipX != lastFlipX)
        {
            lastFlipX = flipX;
            dst.flipX = flipX;
        }

        bool flipY = src.flipY;
        if (force || flipY != lastFlipY)
        {
            lastFlipY = flipY;
            dst.flipY = flipY;
        }

        bool isEnabled = src.enabled;
        if (force || isEnabled != lastEnabled)
        {
            lastEnabled = isEnabled;
            dst.enabled = isEnabled;
        }

        if (force || syncSortingEveryFrame)
        {
            int layerId = src.sortingLayerID;
            int order = src.sortingOrder;

            if (force || layerId != lastSortingLayerId || order != lastSortingOrder)
            {
                lastSortingLayerId = layerId;
                lastSortingOrder = order;
                dst.sortingLayerID = layerId;
                dst.sortingOrder = order + sortingOrderOffset;
            }
        }

        if (inheritSourceAlpha)
        {
            float alpha = src.color.a;
            if (force || alpha != lastAlpha)
            {
                lastAlpha = alpha;
                ApplyColor(alpha);
            }
        }
        else if (force)
        {
            lastAlpha = 1f;
            ApplyColor(1f);
        }
    }

    private void ApplyColor(float alphaMultiplier)
    {
        Color color = shadowColor;
        color.a *= alphaMultiplier;
        shadowRenderer.color = color;
    }

    /// <summary>Makes edit-time changes persist (scenes and especially prefab assets).</summary>
    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || shadowRenderer == null)
            return;

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(shadowRenderer);
        EditorUtility.SetDirty(shadowRenderer.transform);
#endif
    }

    #endregion
}