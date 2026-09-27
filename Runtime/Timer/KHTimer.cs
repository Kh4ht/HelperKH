using UnityEngine;

namespace KH
{
    [System.Serializable]
    public class KHTimer
    {
        #region FIELDS

        // PRIVATE
        private const double TIMER_MAX_VALUE = double.MaxValue - 100;

        // TIMER
        public double Seconds { get; private set; } = 0;

        // GETTERS
        public double SecondsNormalized => Seconds % 60;
        public int Minutes => (int)(Seconds / 60);
        public double Hours => Seconds / 3600;

        #endregion
        #region RUN

        /// <summary>Called on Update() to run the timer</summary>
        public void Run()
        {
            // Prevents the timer from overflowing and becoming negative. The 100 is just a buffer to prevent it from getting too close to double.MaxValue, which could cause issues with the DidExceed() method.
            if (Seconds < TIMER_MAX_VALUE)
                Seconds += Time.deltaTime;
        }

        #endregion
        #region DID EXCEED

        /// <returns>True: if the timer exceeded the <paramref name="duration"/></returns>
        public bool DidExceed(double duration)
        {
            if (duration < 0)
            {
                Debug.LogWarning($"{nameof(duration)} is negative. It will return FALSE.".AddColorTag(KHUtils.XMLColors.Yellow));
                return false;
            }
            else if (duration > TIMER_MAX_VALUE)
            {
                Debug.LogWarning($"{nameof(duration)} is greater {nameof(double.MaxValue)}. It will return FALSE.".AddColorTag(KHUtils.XMLColors.Yellow));
                return false;
            }

            return Seconds >= duration;
        }

        #endregion
        #region RESET

        /// <summary>
        /// Restarts the timer and optionally gives it a headstart. A headstart is a value that the timer will start at instead of 0. For example, if you want the timer to start at 0.5 seconds, you would give it a headstart of 0.5. This can be useful for things like cooldowns, where you want the timer to start at a certain point instead of 0.
        /// </summary>
        /// <param name="timerHeadstart"></param>
        public void Reset(double timerHeadstart = 0)
        {
            if (timerHeadstart < 0)
            {
                Seconds = 0;
                Debug.LogWarning($"{nameof(timerHeadstart)} is negative. It has been set to 0.".AddColorTag(KHUtils.XMLColors.Yellow));
                return;
            }
            if (timerHeadstart > TIMER_MAX_VALUE)
            {
                Seconds = 0;
                Debug.LogWarning($"{nameof(timerHeadstart)} exceeded {nameof(double.MaxValue)}. It has been set to 0.".AddColorTag(KHUtils.XMLColors.Yellow));
                return;
            }

            Seconds = 0 + timerHeadstart;
        }

        #endregion
    }
}