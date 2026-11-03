using System;
using System.Text.Json;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// A field a hand-written data file must have (plan 24 · X4): asked for only by the strict reading
/// (<see cref="GameDataFiles.Strict"/>), so a lenient read of the same type, a save's or a tool's, is unchanged.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DataRequiredAttribute : Attribute
{
}

/// <summary>
/// A key a strictly read record has no property for (plan 24 · X4, <see cref="OneLine{T}"/>): the serializer sets the
/// path to the record, and <see cref="GameDataFiles.Finding"/> adds the key.
/// </summary>
public sealed class UnknownKeyException : JsonException
{
    public string Key { get; }

    public UnknownKeyException(string key) : base($"'{key}' is no field of this record") => Key = key;
}
