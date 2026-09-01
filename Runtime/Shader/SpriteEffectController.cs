// SpriteEffectController.cs
// Optional helper for driving Master Sprite Shader effects at runtime from code,
// e.g. flashing white on hit, or dissolving a sprite out on death.
// Attach to the same GameObject as your SpriteRenderer.
//
// This does NOT depend on PrimeTween - it uses simple coroutines so it drops in
// with no extra packages. If your project already uses PrimeTween/DOTween, feel
// free to swap the coroutines below for tweens.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteEffectController : MonoBehaviour
{
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock mpb;
    private Coroutine flashRoutine;
    private Coroutine dissolveRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Briefly flashes the sprite using the shader's Flash effect.
    /// Requires "Enable Flash" to be turned on in the material.
    /// </summary>
    public void Flash(float duration = 0.12f)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float amount = 1f - Mathf.Clamp01(t / duration);
            SetFloat(FlashAmountID, amount);
            yield return null;
        }
        SetFloat(FlashAmountID, 0f);
        flashRoutine = null;
    }

    /// <summary>
    /// Dissolves the sprite from fully visible (0) to fully gone (1) over 'duration' seconds.
    /// Requires "Enable Dissolve" to be turned on and a noise texture assigned in the material.
    /// </summary>
    public void DissolveOut(float duration = 1f, System.Action onComplete = null)
    {
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(DissolveRoutine(0f, 1f, duration, onComplete));
    }

    /// <summary>
    /// Reverses a dissolve, bringing the sprite back from gone (1) to visible (0).
    /// </summary>
    public void DissolveIn(float duration = 1f, System.Action onComplete = null)
    {
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(DissolveRoutine(1f, 0f, duration, onComplete));
    }

    private IEnumerator DissolveRoutine(float from, float to, float duration, System.Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float amount = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            SetFloat(DissolveAmountID, amount);
            yield return null;
        }
        SetFloat(DissolveAmountID, to);
        dissolveRoutine = null;
        onComplete?.Invoke();
    }

    private void SetFloat(int propertyID, float value)
    {
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(propertyID, value);
        spriteRenderer.SetPropertyBlock(mpb);
    }
}
