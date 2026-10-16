using System;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public struct KHID : IEquatable<KHID>
{
    [SerializeField, HideInInspector]
    private string value;

    public static readonly KHID Empty = default;

    public string Value => value ?? string.Empty;
    public bool IsValid => !string.IsNullOrEmpty(value);

    private KHID(string value) => this.value = value;

    /// <summary>Creates a new globally unique ID.</summary>
    public static KHID New() => new KHID(Guid.NewGuid().ToString("N"));

    /// <summary>Creates a new unique ID with a readable prefix, e.g. "Sword_3f2a...".</summary>
    public static KHID New(string prefix)
    {
        string clean = Sanitize(prefix);
        string guid = Guid.NewGuid().ToString("N");
        return new KHID(clean.Length == 0 ? guid : $"{clean}_{guid}");
    }

    /// <summary>Keeps letters, digits, '_' and '-'; everything else becomes '_'.</summary>
    public static string Sanitize(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : Regex.Replace(text.Trim(), @"[^A-Za-z0-9_\-]+", "_").Trim('_');

    /// <summary>Wraps an existing ID string (e.g. loaded from a save file).</summary>
    public static KHID FromString(string existing) => new KHID(existing);

    public bool Equals(KHID other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => obj is KHID other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(KHID a, KHID b) => a.Equals(b);
    public static bool operator !=(KHID a, KHID b) => !a.Equals(b);
}