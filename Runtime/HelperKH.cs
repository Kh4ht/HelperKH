using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
            Vector2 hotSpot = KHMethods.GetHotspotPosition(texture2D, hotspotPosition);
            Cursor.SetCursor(texture2D, hotSpot, CursorMode.Auto);
        }

        #endregion
        #region GET MOUSE POS


        /// <returns>The current mouse position, based on main camera</returns>
        public static Vector2 GetMouseWorldPos()
        {
            if (Mouse.current == null)
                return Vector2.zero;

            // Use the new Input System to get mouse position
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            return Camera.main.ScreenToWorldPoint(new(mouseScreenPos.x,
                                                      mouseScreenPos.y,
                                                      Camera.main.nearClipPlane));
        }

        #endregion
        #region IsMouseOverUI


        /// <returns>True if the mouse pointer is currently over any UI element.</returns>
        public static bool IsMouseOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #endregion
        #region IS EMPTY


        /// <returns>True if the <paramref name="list"/> is null or empty.</returns>
        public static bool KHIsEmpty<T>(this IList<T> list)
        {
            if (list == null)
            {
                KHDebug.LogError($"{nameof(list)} is NULL");
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
                KHDebug.LogError($"Invalid parameters: list size={list?.Count ?? 0}, count={count}");
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
        #region GenerateId


        /// <summary>
        /// Generates a unique identifier by combining a base name with a random suffix.
        /// </summary>
        /// <param name="uniqueName">The base name used at the beginning of the generated identifier.</param>
        /// <param name="additionalRandomCharCount">The number of random characters to append to the identifier suffix.</param>
        /// <returns>A generated identifier string that includes the provided name and random characters.</returns>
        public static string GenerateId(string uniqueName, int additionalRandomCharCount)
        {
            string id = uniqueName + "__";

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
        public static bool KHHasType<TType>(this System.Collections.IEnumerable list)
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


        ///<summary>for performance optimization.</summary>
        /// <returns>The Squared distance between <paramref name="a"/> 
        /// and <paramref name="b"/></returns>
        public static float GetSqrDistance(Vector3 a, Vector3 b)
        {
            // Check for special invalid values (not null since Vector3 is a struct)
            if (a == Vector3.negativeInfinity || b == Vector3.negativeInfinity)
            {
                KHDebug.LogWarning($"[HDistance] Invalid world position (Vector3.negativeInfinity)." +
                                  $"a: {a}, b: {b}");
                return 0f;
            }

            return (a - b).sqrMagnitude;
        }

        ///<summary>for performance optimization.</summary>
        /// <returns>The Squared distance between <paramref name="a"/> 
        /// and <paramref name="b"/></returns>
        public static float GetSqrDistance(Transform a, Transform b)
        {
            if (a == null || b == null)
            {
                KHDebug.LogWarning($"[HDistance] Transform is null. a: {a}, b: {b}");
                return 0f;
            }
            // Check for special invalid values (not null since Vector3 is a struct)
            if (a.position == Vector3.negativeInfinity || b.position == Vector3.negativeInfinity)
            {
                KHDebug.LogWarning($"[HDistance] Invalid world position (Vector3.negativeInfinity)." +
                                  $"a: {a}, b: {b}");
                return 0f;
            }

            return (a.position - b.position).sqrMagnitude;
        }

        ///<summary>for performance optimization.</summary>
        /// <returns>The Squared distance between <paramref name="a"/> 
        /// and <paramref name="b"/></returns>
        public static float GetSqrDistance(MonoBehaviour a, MonoBehaviour b)
        {
            if (a == null || b == null)
            {
                KHDebug.LogWarning($"[HDistance] object is null. a: {a}, b: {b}");
                return 0f;
            }

            // Check for special invalid values (not null since Vector3 is a struct)
            if (a.transform.position == Vector3.negativeInfinity || b.transform.position == Vector3.negativeInfinity)
            {
                KHDebug.LogWarning($"[HDistance] Invalid world position (Vector3.negativeInfinity)." +
                                $"a: {a}, b: {b}");
                return 0f;
            }

            return (a.transform.position - b.transform.position).sqrMagnitude;
        }

        ///<summary>for performance optimization.</summary>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/>
        /// is less than the <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Vector3 a, Vector3 b, float threshold)
        {
            return GetSqrDistance(a, b) < threshold * threshold;
        }

        ///<summary>for performance optimization.</summary>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/>
        /// is less than the <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(Transform a, Transform b, float threshold)
        {
            return GetSqrDistance(a, b) <= threshold * threshold;
        }

        ///<summary>for performance optimization.</summary>
        /// <returns>True if the distance between <paramref name="a"/> and <paramref name="b"/>
        /// is less than the <paramref name="threshold"/>.</returns>
        public static bool SqrDistanceIsLessThan(MonoBehaviour a, MonoBehaviour b, float threshold)
        {
            return GetSqrDistance(a, b) <= threshold * threshold;
        }

        public static bool SqrDistanceIsLessThan(float dis, float threshold)
        {
            return dis <= threshold * threshold;
        }

        #endregion
        #region MOVE TOWARDS


        /// <summary>
        /// Moves the <see cref="Transform"/> towards the <paramref name="targetPos"/> at a constant <paramref name="moveSpeed"/>.
        /// </summary>
        public static void KHMoveTowards(this Transform transform, Vector3 targetPos, float moveSpeed)
        {
            transform.position += (Vector3)GetDir(transform.position, targetPos) * moveSpeed;
        }

        #endregion
        #region GET DIRECTION


        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="currentPos"/> to <paramref name="targetPos"/>.
        /// </returns>
        public static Vector2 GetDir(Vector3 currentPos, Vector3 targetPos, bool normalizeDir = true)
        {
            return (Vector2)(targetPos - currentPos) == Vector2.zero ? Vector2.zero : normalizeDir ? (targetPos - currentPos).normalized : (targetPos - currentPos);
        }

        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="currentPos"/> to <paramref name="targetPos"/>.
        /// </returns>
        public static Vector2 GetDir(float angle, bool normalizeDir = true)
        {
            return normalizeDir
                ? (Quaternion.Euler(0, 0, angle) * Vector2.right).normalized
                : Quaternion.Euler(0, 0, angle) * Vector2.right;
        }

        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="current"/> to <paramref name="target"/>.
        /// </returns>
        public static Vector2 GetDir(MonoBehaviour current, MonoBehaviour target, bool normalizeDir = true)
        {
            if (current == null)
            {
                KHDebug.LogError($"{nameof(current)} is NULL");
                return Vector2.zero;
            }
            if (target == null)
            {
                KHDebug.LogError($"{nameof(target)} is NULL");
                return Vector2.zero;
            }

            return (Vector2)(target.transform.position - current.transform.position) == Vector2.zero
                ? Vector2.zero
                : normalizeDir ? (target.transform.position - current.transform.position).normalized : (target.transform.position - current.transform.position);
        }

        /// <returns>
        /// A <see cref="Vector2"/> representing the direction from <paramref name="current"/> to <paramref name="target"/>.
        /// </returns>
        public static Vector2 GetDir(Transform current, Transform target, bool normalizeDir = true)
        {
            if (current == null)
            {
                KHDebug.LogError($"{nameof(current)} is NULL");
                return Vector2.zero;
            }
            if (target == null)
            {
                KHDebug.LogError($"{nameof(target)} is NULL");
                return Vector2.zero;
            }

            return (Vector2)(target.position - current.position) == Vector2.zero
                ? Vector2.zero
                : normalizeDir ? (target.position - current.position).normalized : (target.position - current.position);
        }

        #endregion
        #region GET ANGLE


        public static float KHGetAngle(this Vector2 dir)
        {
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        public static float KHGetAngle(this MonoBehaviour current, MonoBehaviour target)
        {
            Vector2 dir = GetDir(current, target);

            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        public static float KHGetAngle(this Vector3 currentPos, Vector3 targetPos)
        {
            Vector2 dir = GetDir(currentPos, targetPos);

            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        #endregion
        #region SORTING ORDER


        /// <summary>
        /// Updates the sprite's sorting order based on its Y position in the world.
        /// This creates a pseudo-depth effect where objects lower on the screen
        /// appear in front of those higher up.
        /// 
        /// The Y position is multiplied by 20 and cast to an integer to reduce
        /// the frequency of sorting order changes (helps performance by avoiding
        /// unnecessary updates for tiny movements).
        /// 
        /// If the calculated order differs from the last stored Y position,
        /// the sorting order is updated and the last position is saved.
        /// </summary>
        public static void KHUpdateSortingOrderBasedOnYPos(this SpriteRenderer spriteRenderer, float Ypos)
        {
            if (spriteRenderer.sortingOrder != -(int)(Ypos * 20))
            {
                spriteRenderer.sortingOrder = -(int)(Ypos * 20);
            }
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

        private static IEnumerator KHRunBatchedCoroutine(
            int count,
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
    }
}