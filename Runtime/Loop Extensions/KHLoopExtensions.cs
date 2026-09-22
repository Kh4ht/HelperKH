using System.Collections.Generic;
using UnityEngine;
using System;

namespace KH
{
    public static class LoopExtensions
    {
        #region FIND

        /// <summary>
        /// Searches for the first element in the list that matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search through.</param>
        /// <param name="predicate">
        /// A function that takes an element and returns <c>true</c> if it matches the search criteria.
        /// </param>
        /// <returns>
        /// The first element that satisfies the predicate, or <c>default(T)</c> if no element matches.
        /// </returns>
        public static T KHFind<T>(this IList<T> list, Func<T, bool> action)
        {
            for (int i = 0; i < list.Count; i++)
            {
                bool result = action(list[i]);
                if (result)
                    return list[i];
            }

            return default;
        }

        /// <summary>
        /// Searches for the first element in the list that matches the specified predicate,
        /// providing both the element and its index.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search through.</param>
        /// <param name="predicate">
        /// A function that takes an element and its index, and returns <c>true</c> if it matches the search criteria.
        /// </param>
        /// <returns>
        /// The first element that satisfies the predicate, or <c>default(T)</c> if no element matches.
        /// </returns>
        public static T KHFind<T>(this IList<T> list, Func<T, int, bool> action)
        {
            for (int i = 0; i < list.Count; i++)
            {
                bool result = action(list[i], i);
                if (result)
                    return list[i];
            }

            return default;
        }

        #endregion
        #region FIND ALL

        /// <summary>
        /// Searches for all elements in the list that match the specified predicate and returns them as a new list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static List<T> KHFindAll<T>(this IList<T> list, Func<T, bool> action)
        {
            List<T> resultList = new();

            for (int i = 0; i < list.Count; i++)
            {
                if (action(list[i]))
                    resultList.Add(list[i]);
            }

            return resultList;
        }

        /// <summary>
        /// Returns a new dictionary containing all key-value pairs that match the specified predicate.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="predicate">The condition used to determine which pairs to include.</param>
        /// <returns>
        /// A new dictionary containing all matching key-value pairs.
        /// </returns>
        public static Dictionary<TKey, TValue> KHFindAll<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            Func<TKey, TValue, bool> predicate)
        {
            Dictionary<TKey, TValue> result = new();

            foreach (var pair in dictionary)
            {
                if (predicate(pair.Key, pair.Value))
                    result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        /// <summary>
        /// Returns a list containing all keys whose key-value pairs match the specified predicate.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="predicate">The condition used to determine which keys to include.</param>
        /// <returns>
        /// A list containing all matching keys.
        /// </returns>
        public static List<TKey> KHFindAllKeys<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            Func<TKey, TValue, bool> predicate)
        {
            List<TKey> result = new();

            foreach (var pair in dictionary)
            {
                if (predicate(pair.Key, pair.Value))
                    result.Add(pair.Key);
            }

            return result;
        }

        /// <summary>
        /// Returns a list containing all values whose key-value pairs match the specified predicate.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="predicate">The condition used to determine which values to include.</param>
        /// <returns>
        /// A list containing all matching values.
        /// </returns>
        public static List<TValue> KHFindAllValues<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            Func<TKey, TValue, bool> predicate)
        {
            List<TValue> result = new();

            foreach (var pair in dictionary)
            {
                if (predicate(pair.Key, pair.Value))
                    result.Add(pair.Value);
            }

            return result;
        }

        #endregion
        #region FOR EACH CHILD

        public static void KHForEachChild(this Transform transform, Action<Transform> action)
        {
            for (int i = 0; i < transform.childCount; i++)
                action(transform.GetChild(i));
        }

        public static void KHForEachChild(this Transform transform, Action<Transform, int> action)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                action(transform.GetChild(i), i);
            }
        }

        #endregion
    }
}
