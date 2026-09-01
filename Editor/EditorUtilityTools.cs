using System.Collections.Generic;
using KH;
using UnityEditor;
using UnityEngine;

public static class EditorUtilityTools
{
    #region PreventListDuplications

    /// <summary>
    /// Replaces duplicate list entries with their default value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list whose duplicate entries should be replaced.</param>
    public static void PreventListDuplications<T>(this List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], list[j]))
                    list[j] = default;
            }
        }
    }

    #endregion
    #region MatchCount


    /// <summary>
    /// Adjusts the list so that it contains the specified number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list whose size should be adjusted.</param>
    /// <param name="count">The desired number of elements.</param>
    public static void KHMatchCount<T>(this List<T> list, int count)
    {
        if (list == null || count < 0)
            return;

        while (list.Count < count)
            list.Add(default);

        while (list.Count > count)
            list.RemoveAt(list.Count - 1);
    }

    #endregion
    #region AutoFillDataBase

    /// <summary>
    /// Finds all assets of the specified ScriptableObject type and fills the list with them.
    /// </summary>
    /// <typeparam name="T">The ScriptableObject type to search for.</typeparam>
    /// <param name="list">The list to clear and populate with the matching assets.</param>
    public static void KHAutoFillDataBase<T>(this List<T> list) where T : ScriptableObject
    {
        if (list == null)
            return;

        list.Clear();

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                list.Add(asset);
        }
    }

    #endregion
}
