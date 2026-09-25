using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(RectTransform))]
[DisallowMultipleComponent]
[AddComponentMenu("KH/UI/" + nameof(MatchSpriteSize))]
public class MatchSpriteSize : MonoBehaviour
{
    #region FIELDS

    // COMPONENTS
    [HideInInspector] public Image img;
    [HideInInspector] public RectTransform rt;

    #endregion
    #region UNITY EVENTS

    private void Reset()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    #endregion
    #region PUBLIC

    public void CacheReferences()
    {
        if (!img) img = GetComponent<Image>();
        if (!rt) rt = GetComponent<RectTransform>();
    }

    #endregion
}
