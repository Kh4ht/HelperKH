using KH.Enums;
using UnityEngine;

namespace KH.Utils
{
    public static class KHUtils
    {
        public static Vector2 GetHotspotPosition(Texture2D texture, CursorHotspot position)
        {
            float width = texture.width;
            float height = texture.height;

            return position switch
            {
                CursorHotspot.TopLeft => new Vector2(0, 0),
                CursorHotspot.TopCenter => new Vector2(width / 2f, 0),
                CursorHotspot.TopRight => new Vector2(width, 0),

                CursorHotspot.CenterLeft => new Vector2(0, height / 2f),
                CursorHotspot.Center => new Vector2(width / 2f, height / 2f),
                CursorHotspot.CenterRight => new Vector2(width, height / 2f),

                CursorHotspot.BottomLeft => new Vector2(0, height),
                CursorHotspot.BottomCenter => new Vector2(width / 2f, height),
                CursorHotspot.BottomRight => new Vector2(width, height),

                _ => new Vector2(0, height), // default fallback
            };
        }

        public static string AddColorTag(this string message, XMLColors color)
        {
            string openTag = $"<b><color={Extract(color)}>";
            string closeTag = $"</b></color>";

            return openTag + message + closeTag;
        }

        private static string Extract(XMLColors color)
        {
            switch (color)
            {
                case XMLColors.Red:
                    return "#ffd9d9";

                case XMLColors.Yellow:
                    return "yellow";

                case XMLColors.White:
                    return "white";
            }

            return "white";
        }
    }
}