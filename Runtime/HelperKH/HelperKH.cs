using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using KH;
namespace KH
{
    public static class Kh
    {
        #region FIELDS



        #endregion
        #region CHANGE CURSOR


        /// <summary>Changes the <see cref="Cursor"/> texture to a 
        /// custom <paramref name="texture2D"/> with a specified hotspot alignment.</summary>
        /// <param name="hotspotPosition">The position on the texture to use as the cursor's hotspot.</param>
        public static void ChangeCursor(Texture2D texture2D, CursorHotspot hotspotPosition = CursorHotspot.TopLeft)
        {
            Vector2 hotSpot = KHUtils.GetHotspotPosition(texture2D, hotspotPosition);
            Cursor.SetCursor(texture2D, hotSpot, CursorMode.Auto);
        }

        #endregion
        #region MOUSE


        /// <returns>The current mouse position, based on main camera</returns>
        public static Vector2 GetMouseWorldPos()
        {
            if (Mouse.current == null)
            {
                Debug.LogWarning("Mouse input is unavailable.".AddColorTag(KHUtils.XMLColors.Yellow));
                return Vector2.zero;
            }

            // Use the new Input System to get mouse position
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            return Camera.main.ScreenToWorldPoint(new(mouseScreenPos.x,
                                                      mouseScreenPos.y,
                                                      Camera.main.nearClipPlane));
        }

        /// <returns>True if the mouse pointer is currently over any UI element.</returns>
        public static bool IsMouseOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #endregion
        #region ENUM

        /// <summary>
        /// Cycles the current enum value to the next (or previous) value in declaration order,
        /// wrapping around at either end. Works regardless of the enum's underlying int values,
        /// gaps, or duplicates.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="current">The current enum value to cycle from.</param>
        /// <param name="step">
        /// Number of steps to move through the declared values. Use 1 (default) to move forward,
        /// -1 to move backward, or any other value to skip multiple steps.
        /// </param>
        /// <returns>The enum value at the resulting position after wrapping.</returns>
        public static T KHCycle<T>(this T current, int step = 1) where T : struct, System.Enum
        {
            T[] values = EnumCache<T>.Values;
            int currentIndex = System.Array.IndexOf(values, current);
            int nextIndex = ((currentIndex + step) % values.Length + values.Length) % values.Length;
            return values[nextIndex];
        }

        /// <summary>
        /// Caches the declared values of enum type <typeparamref name="T"/> so they're only
        /// retrieved via reflection once per type, rather than on every <see cref="KHCycle{T}"/> call.
        /// </summary>
        /// <typeparam name="T">The enum type whose values are cached.</typeparam>
        private static class EnumCache<T> where T : struct, System.Enum
        {
            public static readonly T[] Values = (T[])System.Enum.GetValues(typeof(T));
        }

        #endregion
        #region CAMERA


        /// <summary>
        /// Gets the visible world-space size (width and height) of an orthographic camera's viewport.
        /// </summary>
        /// <param name="camera">The orthographic Camera to measure.</param>
        /// <returns>
        /// A <see cref="Vector2"/> where x is the visible width and y is the visible height, in world units,
        /// or <see cref="Vector2.zero"/> if <paramref name="camera"/> is null or not orthographic.
        /// </returns>
        public static Vector2 GetCameraOrthographicSize(this Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError($"{nameof(camera)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return Vector2.zero;
            }
            if (!camera.orthographic)
            {
                Debug.LogWarning("Camera is not orthographic".AddColorTag(KHUtils.XMLColors.Yellow));
                return Vector2.zero;
            }

            return new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize) * 2f;
        }

        #endregion
        #region IS EMPTY


        /// <returns>True if the <paramref name="list"/> is null or empty.</returns>
        public static bool KHIsEmpty<T>(this IList<T> list)
        {
            if (list == null)
            {
                Debug.LogError($"{nameof(list)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return true;
            }

            return list.Count == 0;
        }

        #endregion
        #region PICK RANDOM


        /// <returns>A random item from a <paramref name="list"/>.</returns>
        public static T KHPickRandom<T>(this IList<T> list)
        {
            return list.KHIsEmpty() ? default : list[Random.Range(0, list.Count)];
        }


        /// <summary>
        /// Returns a random selection of unique items from the specified list.
        /// </summary>
        /// <typeparam name="T">The type of elements contained in the list.</typeparam>
        /// <param name="list">The list to select items from.</param>
        /// <param name="count">The number of unique random items to return.</param>
        /// <returns>A list containing <paramref name="count"/> unique random items from <paramref name="list"/>.</returns>
        public static List<T> KHPickRandom<T>(this IList<T> list, int count)
        {
            // Validate inputs
            if (list.KHIsEmpty() || count < 1 || count > list.Count)
            {
                Debug.LogError($"Invalid parameters: list size={list?.Count ?? 0}, count={count}".AddColorTag(KHUtils.XMLColors.Red));
                return new List<T>();
            }

            // Fisher-Yates shuffle - pick 'count' unique items
            List<T> shuffled = new(list);

            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(i, shuffled.Count);

                T temp = shuffled[i];
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }

            // Return the first 'count' items from shuffled list
            return shuffled.GetRange(0, count);
        }


        /// <summary>
        /// Returns a random character from the specified string.
        /// </summary>
        /// <param name="str">The string to select a character from.</param>
        /// <returns>A random character from <paramref name="str"/>.</returns>
        public static char KHPickRandom(this string str)
        {
            return str[Random.Range(0, str.Length)];
        }

        #endregion
        #region ID


        /// <summary>
        /// Generates a unique identifier by combining a base name with a random suffix.
        /// </summary>
        /// <param name="uniqueName">The base name used at the beginning of the generated identifier.</param>
        /// <param name="additionalRandomCharCount">The number of random characters to append to the identifier suffix.</param>
        /// <returns>A generated identifier string that includes the provided name and random characters.</returns>
        public static string GenerateId(string uniqueName, int additionalRandomCharCount)
        {
            string id = "";
            id = string.IsNullOrEmpty(uniqueName) ? "" : uniqueName + "__";

            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=|\\\'\"/?.>,<";

            for (var i = 0; i < additionalRandomCharCount; i++)
                id += characters.KHPickRandom();

            return id.Replace(" ", "_");
        }

        #endregion
        #region HAS TYPE


        /// <summary>
        /// Checks if the enumerable contains an element of type T.
        /// </summary>
        public static bool KHHasType<TType>(this IEnumerable list)
        {
            foreach (object item in list)
                if (item is TType)
                    return true;

            return false;
        }

        #endregion
        #region DESTROY CHILDREN IMMEDIATE


        public static void KHDestroyAllChildrenImmediate(this Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        #endregion
        #region SQUARE DISTANCE

        /// <summary>
        /// Computes the squared distance between two points. Cheaper than a real distance check
        /// since it avoids a square root — use this instead of <c>Vector3.Distance</c> when comparing
        /// against a threshold.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>
        /// The squared distance between <paramref name="a"/> and <paramref name="b"/>,
        /// or <see cref="float.MaxValue"/> if either point is <see cref="Vector3.negativeInfinity"/>.
        /// </returns>
        public static float GetSqrDistance(Vector3 a, Vector3 b)
        {
            if (a == Vector3.negativeInfinity)
            {
                Debug.LogError($"{nameof(a)} is {nameof(Vector3.negativeInfinity)}".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }
            if (b == Vector3.negativeInfinity)
            {
                Debug.LogError($"{nameof(b)} is {nameof(Vector3.negativeInfinity)}".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }

            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// Computes the squared distance between two Transforms' positions. Cheaper than a real
        /// distance check since it avoids a square root — use this instead of <c>Vector3.Distance</c>
        /// when comparing against a threshold.
        /// </summary>
        /// <param name="a">The first Transform.</param>
        /// <param name="b">The second Transform.</param>
        /// <returns>
        /// The squared distance between <paramref name="a"/> and <paramref name="b"/>,
        /// or <see cref="float.MaxValue"/> if either Transform is null or has an invalid position.
        /// </returns>
        public static float GetSqrDistance(Transform a, Transform b)
        {
            if (a == null)
            {
                Debug.LogError($"{nameof(a)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }
            if (b == null)
            {
                Debug.LogError($"{nameof(b)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }

            return GetSqrDistance(a.position, b.position);
        }

        /// <summary>
        /// Computes the squared distance between two MonoBehaviours' positions. Cheaper than a real
        /// distance check since it avoids a square root — use this instead of <c>Vector3.Distance</c>
        /// when comparing against a threshold.
        /// </summary>
        /// <param name="a">The first MonoBehaviour.</param>
        /// <param name="b">The second MonoBehaviour.</param>
        /// <returns>
        /// The squared distance between <paramref name="a"/> and <paramref name="b"/>,
        /// or <see cref="float.MaxValue"/> if either MonoBehaviour is null or has an invalid position.
        /// </returns>
        public static float GetSqrDistance(MonoBehaviour a, MonoBehaviour b)
        {
            if (a == null)
            {
                Debug.LogError($"{nameof(a)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }
            if (b == null)
            {
                Debug.LogError($"{nameof(b)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return float.MaxValue;
            }

            return GetSqrDistance(a.transform.position, b.transform.position);
        }

        /// <summary>
        /// Checks whether the distance between two points is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <param name="sqrDis">The squared distance that was computed between <paramref name="a"/> and <paramref name="b"/>.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Vector3 a, Vector3 b, float threshold, out float sqrDis)
        {
            sqrDis = GetSqrDistance(a, b);
            return sqrDis <= threshold * threshold;
        }

        /// <summary>
        /// Checks whether the distance between two points is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Vector3 a, Vector3 b, float threshold)
        {
            return SqrDistanceIsLessThan(a, b, threshold, out _);
        }

        /// <summary>
        /// Checks whether the distance between two Transforms is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first Transform.</param>
        /// <param name="b">The second Transform.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <param name="sqrDis">The squared distance that was computed between <paramref name="a"/> and <paramref name="b"/>.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Transform a, Transform b, float threshold, out float sqrDis)
        {
            sqrDis = GetSqrDistance(a, b);
            return sqrDis <= threshold * threshold;
        }

        /// <summary>
        /// Checks whether the distance between two Transforms is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first Transform.</param>
        /// <param name="b">The second Transform.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Transform a, Transform b, float threshold)
        {
            return SqrDistanceIsLessThan(a, b, threshold, out _);
        }

        /// <summary>
        /// Checks whether the distance between two MonoBehaviours is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first MonoBehaviour.</param>
        /// <param name="b">The second MonoBehaviour.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <param name="sqrDis">The squared distance that was computed between <paramref name="a"/> and <paramref name="b"/>.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(MonoBehaviour a, MonoBehaviour b, float threshold, out float sqrDis)
        {
            sqrDis = GetSqrDistance(a, b);
            return sqrDis <= threshold * threshold;
        }

        /// <summary>
        /// Checks whether the distance between two MonoBehaviours is less than or equal to <paramref name="threshold"/>,
        /// using a squared-distance comparison to avoid a square root.
        /// </summary>
        /// <param name="a">The first MonoBehaviour.</param>
        /// <param name="b">The second MonoBehaviour.</param>
        /// <param name="threshold">The distance threshold to compare against.</param>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/> is less than or equal to <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(MonoBehaviour a, MonoBehaviour b, float threshold)
        {
            return SqrDistanceIsLessThan(a, b, threshold, out _);
        }

        #endregion
        #region MOVEMENT

        /// <summary>
        /// Moves the <see cref="Transform"/> towards <paramref name="targetPos"/> at a constant <paramref name="moveSpeed"/>.
        /// </summary>
        /// <param name="transform">The Transform to move.</param>
        /// <param name="targetPos">The position to move towards.</param>
        /// <param name="moveSpeed">The speed at which to move, in units per call.</param>
        public static void KHMoveTowards(this Transform transform, Vector3 targetPos, float moveSpeed)
        {
            transform.position += (Vector3)GetDir(transform.position, targetPos) * moveSpeed;
        }

        /// <summary>
        /// Moves the <see cref="MonoBehaviour"/>'s Transform towards <paramref name="targetPos"/> at a constant <paramref name="moveSpeed"/>.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour whose Transform to move.</param>
        /// <param name="targetPos">The position to move towards.</param>
        /// <param name="moveSpeed">The speed at which to move, in units per call.</param>
        public static void KHMoveTowards(this MonoBehaviour monoBehaviour, Vector3 targetPos, float moveSpeed)
        {
            monoBehaviour.transform.KHMoveTowards(targetPos, moveSpeed);
        }

        #endregion
        #region DIRECTION

        /// <summary>
        /// Gets the direction from <paramref name="currentPos"/> to <paramref name="targetPos"/>.
        /// </summary>
        /// <param name="currentPos">The starting position.</param>
        /// <param name="targetPos">The target position.</param>
        /// <param name="normalizeDir">Whether to normalize the resulting direction. Defaults to true.</param>
        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="currentPos"/> to <paramref name="targetPos"/>,
        /// or <see cref="Vector2.zero"/> if the positions are the same.
        /// </returns>
        public static Vector2 GetDir(Vector3 currentPos, Vector3 targetPos, bool normalizeDir = true)
        {
            Vector2 delta = targetPos - currentPos;
            return delta == Vector2.zero ? Vector2.zero : normalizeDir ? delta.normalized : delta;
        }

        /// <summary>
        /// Gets the direction corresponding to a given angle.
        /// </summary>
        /// <param name="angle">The angle in degrees, measured counter-clockwise from the positive X axis.</param>
        /// <param name="normalizeDir">Whether to normalize the resulting direction. Defaults to true.</param>
        /// <returns>
        /// A <see cref="Vector2"/> representing the direction that corresponds to <paramref name="angle"/>.
        /// </returns>
        public static Vector2 GetDir(float angle, bool normalizeDir = true)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            return normalizeDir ? dir.normalized : dir;
        }

        /// <summary>
        /// Gets the direction from <paramref name="current"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="current">The MonoBehaviour to measure the direction from.</param>
        /// <param name="target">The MonoBehaviour to measure the direction towards.</param>
        /// <param name="normalizeDir">Whether to normalize the resulting direction. Defaults to true.</param>
        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="current"/> to <paramref name="target"/>,
        /// or <see cref="Vector2.zero"/> if either argument is null or the positions are the same.
        /// </returns>
        public static Vector2 GetDir(MonoBehaviour current, MonoBehaviour target, bool normalizeDir = true)
        {
            if (current == null)
            {
                Debug.LogError($"{nameof(current)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return Vector2.zero;
            }
            if (target == null)
            {
                Debug.LogError($"{nameof(target)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return Vector2.zero;
            }

            return GetDir(current.transform.position, target.transform.position, normalizeDir);
        }

        /// <summary>
        /// Gets the direction from <paramref name="current"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="current">The Transform to measure the direction from.</param>
        /// <param name="target">The Transform to measure the direction towards.</param>
        /// <param name="normalizeDir">Whether to normalize the resulting direction. Defaults to true.</param>
        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="current"/> to <paramref name="target"/>,
        /// or <see cref="Vector2.zero"/> if either argument is null or the positions are the same.
        /// </returns>
        public static Vector2 GetDir(Transform current, Transform target, bool normalizeDir = true)
        {
            if (current == null)
            {
                Debug.LogError($"{nameof(current)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return Vector2.zero;
            }
            if (target == null)
            {
                Debug.LogError($"{nameof(target)} is NULL".AddColorTag(KHUtils.XMLColors.Red));
                return Vector2.zero;
            }

            return GetDir(current.position, target.position, normalizeDir);
        }

        #endregion
        #region GET ANGLE

        /// <summary>
        /// Gets the angle (in degrees) of a direction vector, measured counter-clockwise from the positive X axis.
        /// </summary>
        /// <param name="dir">The direction vector to measure the angle of.</param>
        /// <returns>The angle in degrees, in the range (-180, 180].</returns>
        public static float KHGetAngle(this Vector2 dir)
        {
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Gets the angle (in degrees) from <paramref name="current"/> to <paramref name="target"/>,
        /// measured counter-clockwise from the positive X axis.
        /// </summary>
        /// <param name="current">The MonoBehaviour to measure the angle from.</param>
        /// <param name="target">The MonoBehaviour to measure the angle towards.</param>
        /// <returns>The angle in degrees, in the range (-180, 180].</returns>
        public static float KHGetAngle(this MonoBehaviour current, MonoBehaviour target)
        {
            return GetDir(current, target).KHGetAngle();
        }

        /// <summary>
        /// Gets the angle (in degrees) from <paramref name="currentPos"/> to <paramref name="targetPos"/>,
        /// measured counter-clockwise from the positive X axis.
        /// </summary>
        /// <param name="currentPos">The position to measure the angle from.</param>
        /// <param name="targetPos">The position to measure the angle towards.</param>
        /// <returns>The angle in degrees, in the range (-180, 180].</returns>
        public static float KHGetAngle(this Vector3 currentPos, Vector3 targetPos)
        {
            return GetDir(currentPos, targetPos).KHGetAngle();
        }

        #endregion
        #region RUN BATCHED


        /// <summary>
        /// Executes an action for a specified number of indexed items, processing
        /// the items in batches and yielding between batches when necessary.
        /// </summary>
        /// <param name="runner">The MonoBehaviour used to start the coroutine.</param>
        /// <param name="count">How many times to run the <paramref name="action"/>.</param>
        /// <param name="action">The action to invoke for each item index.</param>
        /// <param name="batchSize">The number of items to process before yielding.</param>
        public static void KHRunBatched(this MonoBehaviour runner,
                                        int count,
                                        System.Action<int> action,
                                        int batchSize = 5)
        {
            if (count <= batchSize)
            {
                for (int i = 0; i < count; i++)
                    action(i);

                return;
            }

            runner.StartCoroutine(KHRunBatchedCoroutine(
                count,
                batchSize,
                action));
        }

        private static IEnumerator KHRunBatchedCoroutine(int count,
                                                         int batchSize,
                                                         System.Action<int> action)
        {
            for (int i = 0; i < count; i++)
            {
                action(i);

                if ((i + 1) % batchSize == 0)
                    yield return null;
            }
        }

        #endregion
        #region ROUND


        public static float KHRoundToDecimalPlaces(this float value, int decimalPlaces = 2)
        {
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        public static Vector2 KHRoundToDecimalPlaces(this Vector2 value, int decimalPlaces = 2)
        {
            return new Vector2(value.x.KHRoundToDecimalPlaces(decimalPlaces), value.y.KHRoundToDecimalPlaces(decimalPlaces));
        }

        #endregion
    }
}