using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KH;

namespace KH
{
    public sealed class KHSpriteAnimator
    {
        #region CONSTRUCTOR

        public KHSpriteAnimator(MonoBehaviour newMonoBehaviour,
                              SpriteRenderer newRenderer,
                              int newFrameRate = 12)
        {
            renderer = newRenderer;
            _monoBehaviour = newMonoBehaviour;
            FrameRate = newFrameRate;

            _transform = _monoBehaviour.transform;
        }

        #endregion
        #region FIELDS

        private readonly Transform _transform;
        private readonly SpriteRenderer renderer;
        private readonly MonoBehaviour _monoBehaviour;


        private Coroutine _animCoro;
        private List<Sprite> currentAnimation;


        private int _frameRate;
        public int FrameRate
        {
            get => _frameRate;
            set => _frameRate = Mathf.Max(1, value);
        }

        #endregion
        #region PUBLIC METHODS

        /// <summary>
        /// Plays a frame-by-frame sprite animation on a <see cref="SpriteRenderer"/> using coroutine.
        /// </summary>
        /// <param name="frames">A list of <see cref="Sprite"/>s representing the animation frames in order.</param>
        /// <param name="loopCount">The number of times to loop the animation, use -1 for infinite loop.</param>
        /// <param name="onComplete">Action to call when all loops of the animation are done.</param>
        public void PlaySpriteAnimation(List<Sprite> frames,
                                        int loopCount, //-1 for infinite
                                        Action<int> onFrame = null,
                                        Action onComplete = null)
        {
            // Null or empty check for the frames list
            if (frames == null || frames.Count == 0)
            {
                Debug.Log($"<b><color=white>[{nameof(KHSpriteAnimator)}] Frames list is null or empty.</color></b>");
                onComplete?.Invoke();
                return;
            }

            if (renderer == null)
            {
                Debug.LogWarning($"<b><color=yellow>[{nameof(KHSpriteAnimator)}] {nameof(renderer)} is missing.</color></b>");
                onComplete?.Invoke();
                return;
            }

            // Don't restart if already playing the same animation
            if (currentAnimation == frames && loopCount < 0)
                return;

            // Warn if OnComplete won't be called
            if (loopCount < 0 && onComplete != null)
            {
                Debug.LogWarning($"<b><color=yellow>[{nameof(KHSpriteAnimator)}] {nameof(onComplete)} will never be called for infinite loops.</color></b>");
            }

            currentAnimation = frames;

            if (_animCoro != null)
                _monoBehaviour.StopCoroutine(_animCoro);

            _animCoro = _monoBehaviour.StartCoroutine(PlayAnimationCoroutine(renderer, frames, loopCount, onFrame, onComplete));
        }

        IEnumerator PlayAnimationCoroutine(SpriteRenderer renderer,
                                           List<Sprite> frames,
                                           int loopCount,
                                           Action<int> onFrame,
                                           Action onComplete)
        {
            if (renderer == null)
            {
                Debug.LogWarning($"<b><color=yellow>[{nameof(KHSpriteAnimator)}] {nameof(renderer)} is null in coroutine.</color></b>");
                yield break;
            }

            if (frames == null || frames.Count == 0)
            {
                Debug.Log($"<b><color=white>[{nameof(KHSpriteAnimator)}] Frames list is null or empty in coroutine.</color></b>");
                yield break;
            }

            float frameDuration = 1f / Mathf.Max(FrameRate, 0.0001f); // avoid divide by zero
            int loops = 0;

            while (loopCount < 0 || loops < loopCount)
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    if (frames[i] != null)
                    {
                        renderer.sprite = frames[i];
                        onFrame?.Invoke(i);
                    }

                    yield return new WaitForSeconds(frameDuration);
                }
                loops++;
            }

            currentAnimation = null;

            if (loopCount >= 0)
                onComplete?.Invoke();
        }

        public void StopAnimation()
        {
            if (_animCoro != null)
                _monoBehaviour.StopCoroutine(_animCoro);

            currentAnimation = null;
        }

        #endregion
    }
}
