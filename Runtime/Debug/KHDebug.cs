using UnityEngine;

namespace KH
{
    /// <summary>
    /// A static class that provides logging methods for debugging purposes.
    /// The logs are color-coded for better visibility in the Unity Editor console.
    /// </summary>
    public static class KHDebug
    {
        #region FIELDS

        private const string ColorRed = "#ffd9d9";
        private const string ColorYellow = "#ffff00";
        private const string ColorWhite = "#ffffff";

        #endregion
        #region PRIVATE

        private static string ColorTag(this object message, string color) => $"<b><color={color}>" + message + $"</b></color>";

        #endregion
        #region Log

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message) => Debug.Log(message.ColorTag(ColorWhite));

        #endregion
        #region LogWarning

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message) => Debug.LogWarning(message.ColorTag(ColorYellow));

        #endregion
        #region LogError
        // Note: The LogError methods are not conditional, as errors should always be logged regardless of the build configuration.

        public static void LogError(object message) => Debug.LogError(message.ColorTag(ColorRed));
        public static void LogError(object message, Object context) => Debug.LogError(message.ColorTag(ColorRed), context);

        #endregion

    }
}