using System.Text.Json;
using System.Text.Json.Nodes;

namespace SaveTool;

/// <summary>
/// Two saves compared (plan 12 · Q3): every place in their JSON where they differ, one line each: <c>~</c> a value that
/// changed, <c>+</c> something only the second has, <c>-</c> something only the first has. The file's own checksum is
/// left out, since it differs whenever anything does. Lists of text (the story's flags, the species seen) are compared
/// as sets, so a flag set earlier or later is no difference; lists of records are compared place by place.
/// </summary>
public static class SaveDiff
{
    public static IReadOnlyList<string> Compare(string firstJson, string secondJson)
    {
        var a = Flatten(firstJson);
        var b = Flatten(secondJson);
        var lines = new List<string>();
        foreach (var path in a.Keys.Union(b.Keys).Order(StringComparer.Ordinal))
        {
            bool inA = a.TryGetValue(path, out string? x), inB = b.TryGetValue(path, out string? y);
            if (inA && inB) { if (x != y) lines.Add($"~ {path}: {x} -> {y}"); }
            else if (inB) lines.Add($"+ {path}: {y}");
            else lines.Add($"- {path}: {x}");
        }
        return lines;
    }

    // Every leaf of the JSON by its path; a list of plain text becomes one leaf per member, named by the member
    private static Dictionary<string, string> Flatten(string json)
    {
        var leaves = new Dictionary<string, string>(StringComparer.Ordinal);
        var root = JsonNode.Parse(json) ?? throw new JsonException("The file holds no JSON.");
        Walk(root, "", leaves);
        leaves.Remove("Checksum");
        return leaves;
    }

    private static void Walk(JsonNode? node, string path, Dictionary<string, string> leaves)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, child) in obj) Walk(child, path.Length == 0 ? name : $"{path}.{name}", leaves);
                break;
            case JsonArray array when array.All(item => item is JsonValue v && v.GetValueKind() == JsonValueKind.String):
                // A set of words: its members, in no order
                foreach (var item in array) leaves[$"{path}[{item!.GetValue<string>()}]"] = "present";
                if (array.Count == 0) leaves[path] = "[]";
                break;
            case JsonArray array:
                for (int i = 0; i < array.Count; i++) Walk(array[i], $"{path}[{i}]", leaves);
                if (array.Count == 0) leaves[path] = "[]";
                break;
            case null:
                leaves[path] = "null";
                break;
            default:
                leaves[path] = node.ToJsonString();
                break;
        }
    }
}
