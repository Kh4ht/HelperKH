using System;

[AttributeUsage(AttributeTargets.Field)]
public class KHIDPrefixAttribute : Attribute
{
    public readonly string Prefix;
    public KHIDPrefixAttribute(string prefix) => Prefix = prefix;
}