using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KH
{
    /// <summary>
    /// One migration step: upgrades raw save JSON from FromVersion to FromVersion + 1.
    /// Works on the raw JObject, so renamed/removed/restructured fields can still be read.
    /// </summary>
    public interface IKHSaveMigration
    {
        int FromVersion { get; }
        void Migrate(JObject data);
    }

    public static class KHSaveMigrationSystem
    {
        public const string VERSION_KEY = "version";

        private static readonly Dictionary<Type, SortedList<int, IKHSaveMigration>> map = new();

        /// <summary>Register once at startup, before the first Load.</summary>
        public static void Register<T>(IKHSaveMigration migration)
        {
            if (!map.TryGetValue(typeof(T), out var list))
                map[typeof(T)] = list = new SortedList<int, IKHSaveMigration>();

            list[migration.FromVersion] = migration;
        }

        /// <summary>Runs every needed step (v1 -> v2 -> v3 ...) on the raw JSON.</summary>
        public static void Migrate<T>(JObject data)
        {
            if (!map.TryGetValue(typeof(T), out var list)) return;

            int version = data.Value<int?>(VERSION_KEY) ?? 0;

            while (list.TryGetValue(version, out var step))
            {
                step.Migrate(data);
                version++;
                data[VERSION_KEY] = version;
                Debug.Log($"Migrated {typeof(T).Name} save to version {version}");
            }
        }
    }
}
