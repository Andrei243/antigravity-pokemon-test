using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace ContentCheck;

/// <summary>
/// Fields of a hand-written file that its format doesn't have. The game's reader passes over a field it doesn't know
/// without a word, so a slip of the pen (<c>"dialogue"</c> for <c>"dialog"</c>, <c>"hiddenby"</c>) quietly leaves
/// someone with nothing to say or always on the map: this walks the file's JSON beside the record it is read into and
/// names each field no property answers to, with the nearest one that would.
/// </summary>
public static class JsonShape
{
    /// <summary>Each unknown field as (where it is in the file, what is wrong).</summary>
    public static List<(string Path, string What)> UnknownFields(string json, Type type)
    {
        var found = new List<(string, string)>();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            Walk(document.RootElement, type, "", found);
        }
        catch (JsonException)
        {
            // A file that can't be read at all is told of where it is read
        }
        return found;
    }

    private static void Walk(JsonElement element, Type type, string path, List<(string, string)> found)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (element.ValueKind == JsonValueKind.Array)
        {
            if (ElementOf(type) is not { } item) return;
            int i = 0;
            foreach (var child in element.EnumerateArray()) Walk(child, item, $"{path}[{i++}]", found);
            return;
        }
        if (element.ValueKind != JsonValueKind.Object) return;

        // A table by name (an overlay's people, its signs): every key is a name of the file's own
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            foreach (var entry in element.EnumerateObject()) Walk(entry.Value, type.GetGenericArguments()[1], Join(path, entry.Name), found);
            return;
        }
        if (!IsRecord(type)) return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetCustomAttribute<JsonIgnoreAttribute>() is not { Condition: JsonIgnoreCondition.Always })
            .ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => p);
        foreach (var field in element.EnumerateObject())
        {
            if (properties.TryGetValue(field.Name, out var property))
            {
                Walk(field.Value, property.PropertyType, Join(path, field.Name), found);
                continue;
            }
            string? near = properties.Keys.OrderBy(name => Distance(name, field.Name)).FirstOrDefault(name => Distance(name, field.Name) <= Math.Max(2, field.Name.Length / 3));
            found.Add((Join(path, field.Name), $"a {Describe(type)} has no field \"{field.Name}\", so the game passes over it without a word" + (near != null ? $" (\"{near}\"?)" : "")));
        }
    }

    private static string Join(string path, string name) => path.Length == 0 ? name : path + "." + name;

    private static Type? ElementOf(Type type) =>
        type.IsArray ? type.GetElementType()
        : type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Length == 1 ? type.GetGenericArguments()[0]
        : null;

    /// <summary>One of the game's own records, whose fields are its properties: a class of the engine that is no enum and no text.</summary>
    private static bool IsRecord(Type type) => type.IsClass && type != typeof(string) && type.Assembly == typeof(GameDataFiles).Assembly;

    private static string Describe(Type type) => type == typeof(WorldOverlayFile) ? "overlay" : type == typeof(MapFile) ? "map file" : type.Name;

    /// <summary>How many letters must be changed, added or dropped to make one word of another, case set aside.</summary>
    private static int Distance(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        var row = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) row[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            int diagonal = row[0];
            row[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int above = row[j];
                row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), diagonal + (a[i - 1] == b[j - 1] ? 0 : 1));
                diagonal = above;
            }
        }
        return row[b.Length];
    }
}
