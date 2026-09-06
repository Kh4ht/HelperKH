using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KH
{
    /// <summary>
    /// Marks a static field or property to be reset to its default value
    /// when a new Unity runtime session is initialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class KHResetStaticAttribute : Attribute { }

    public static class StaticResetSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IReadOnlyList<Assembly> assemblies = UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // FIELDS

                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    foreach (FieldInfo field in fields)
                    {
                        if (!field.IsDefined(typeof(KHResetStaticAttribute), false))
                            continue;

                        object defaultValue = GetDefaultValue(field.FieldType);

                        field.SetValue(null, defaultValue);
                    }

                    // PROPERTIES

                    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    foreach (PropertyInfo property in properties)
                    {
                        if (!property.IsDefined(typeof(KHResetStaticAttribute), false) || !property.CanWrite)
                            continue;

                        object defaultValue = GetDefaultValue(property.PropertyType);

                        property.SetValue(null, defaultValue);
                    }
                }
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }
    }
}