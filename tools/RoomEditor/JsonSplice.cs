using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;

namespace RoomEditor;

/// <summary>
/// Changes one entry of a hand-written JSON file and leaves the rest of it as it was written: its comments, its
/// order, its one-line records, its blank lines. An overlay is written by hand with comments the game's reader skips
/// (<see cref="GameDataFiles.Json"/>), so writing the whole <see cref="WorldOverlayFile"/> back would lose them all
/// and reformat every line; the editor writes only the value it changed, in the game's own format, indented to
/// where it stands (plan 16 · T9).
/// <para>
/// A path names a member of an object by its name and an element of an array by its number (<c>people</c>,
/// <c>people.youngster</c>, <c>doors.2</c>). Comments inside a value that is replaced or removed go with it.
/// </para>
/// </summary>
public static class JsonSplice
{
    /// <summary>A value's place in the file's bytes, and for an object or an array its members (a member's name is null in an array).</summary>
    public sealed class Node
    {
        public int Start, End;
        public JsonTokenType Kind;
        public List<(string? Name, int NameStart, Node Value)> Members { get; } = new();
    }

    private static readonly JsonReaderOptions Reading = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>The file's tree of places.</summary>
    public static Node Parse(byte[] bytes)
    {
        var reader = new Utf8JsonReader(bytes, Reading);
        if (!reader.Read()) throw new JsonException("The file is empty.");
        return ReadValue(ref reader);
    }

    private static Node ReadValue(ref Utf8JsonReader reader)
    {
        var node = new Node { Start = (int)reader.TokenStartIndex, Kind = reader.TokenType };
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            bool isObject = reader.TokenType == JsonTokenType.StartObject;
            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray) break;
                string? name = null;
                int nameStart = (int)reader.TokenStartIndex;
                if (isObject)
                {
                    name = reader.GetString();
                    reader.Read();
                }
                node.Members.Add((name, nameStart, ReadValue(ref reader)));
            }
        }
        node.End = (int)reader.BytesConsumed;
        return node;
    }

    /// <summary>The node a path names, or null where there is none.</summary>
    public static Node? Find(Node root, string path)
    {
        var node = root;
        foreach (string part in Steps(path))
        {
            int at = IndexOf(node, part);
            if (at < 0) return null;
            node = node.Members[at].Value;
        }
        return node;
    }

    private static IEnumerable<string> Steps(string path) => path.Length == 0 ? Array.Empty<string>() : path.Split('.');

    private static int IndexOf(Node node, string step)
    {
        if (node.Kind == JsonTokenType.StartArray)
            return int.TryParse(step, out int i) && i >= 0 && i < node.Members.Count ? i : -1;
        return node.Members.FindIndex(m => m.Name == step);
    }

    /// <summary>
    /// The text with the value at <paramref name="path"/> set to <paramref name="json"/> (a value as
    /// <see cref="GameDataFiles.Serialize"/> writes it): replaced where it is, added at the end of its object or array
    /// where it isn't (and its object made where that isn't either), or taken out when <paramref name="json"/> is null.
    /// The last step of a path into an array is a number for a replacement and <c>+</c> for an addition.
    /// </summary>
    public static string Set(string text, string path, string? json)
    {
        bool bom = text.Length > 0 && text[0] == '﻿';
        var bytes = Encoding.UTF8.GetBytes(bom ? text[1..] : text);
        var root = Parse(bytes);
        var steps = Steps(path).ToList();
        if (steps.Count == 0) throw new ArgumentException("name a value inside the file");

        // The deepest container that is there, and the steps it lacks
        var parent = root;
        int taken = 0;
        while (taken < steps.Count - 1)
        {
            int at = IndexOf(parent, steps[taken]);
            if (at < 0) break;
            parent = parent.Members[at].Value;
            taken++;
        }

        string result;
        if (taken < steps.Count - 1)
        {
            // A container on the way is missing: nothing to take out, else it is made with the value in it
            if (json == null) return text;
            string made = json;
            for (int i = steps.Count - 1; i > taken; i--)
                made = steps[i] == "+" ? $"[\n  {Indent(made, "  ")}\n]" : $"{{\n  {Quote(steps[i])}: {Indent(made, "  ")}\n}}";
            result = Add(bytes, parent, steps[taken], made, parent == root);
        }
        else
        {
            string last = steps[^1];
            int at = last == "+" ? -1 : IndexOf(parent, last);
            if (at >= 0)
            {
                var value = parent.Members[at].Value;
                // A value written on one line by hand is written back on one line
                if (json != null && SameLine(bytes, value.Start, value.End)) json = OneLine(json);
                result = json == null
                    ? Remove(bytes, parent, at)
                    : Splice(bytes, value.Start, value.End, Indent(json, LineIndent(bytes, parent.Members[at].NameStart)));
            }
            else result = json == null ? Encoding.UTF8.GetString(bytes) : Add(bytes, parent, last, json, parent == root);
        }
        return bom ? '﻿' + result : result;
    }

    /// <summary>A value serialized as the game writes its files, without the closing newline.</summary>
    public static string Serialize<T>(T value) => GameDataFiles.Serialize(value).TrimEnd('\n');

    private static string Quote(string name) => JsonSerializer.Serialize(name, Compact);

    private static readonly JsonSerializerOptions Compact = new(GameDataFiles.Json) { WriteIndented = false };

    /// <summary>
    /// A value on one line, as the one-line records of the game's files are written (<c>{ "warp": 4, "map": "C" }</c>,
    /// <c>[ 2, 3 ]</c>): what a value written on a line by hand is written back as.
    /// </summary>
    public static string OneLine(string json)
    {
        var text = new StringBuilder();
        Write(JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }), text);
        return text.ToString();

        static void Write(JsonNode? node, StringBuilder into)
        {
            switch (node)
            {
                case JsonObject o when o.Count == 0: into.Append("{}"); break;
                case JsonArray a when a.Count == 0: into.Append("[]"); break;
                case JsonObject o:
                    into.Append("{ ");
                    bool first = true;
                    foreach (var (name, value) in o)
                    {
                        if (!first) into.Append(", ");
                        first = false;
                        into.Append(Quote(name)).Append(": ");
                        Write(value, into);
                    }
                    into.Append(" }");
                    break;
                case JsonArray a:
                    into.Append("[ ");
                    for (int i = 0; i < a.Count; i++)
                    {
                        if (i > 0) into.Append(", ");
                        Write(a[i], into);
                    }
                    into.Append(" ]");
                    break;
                case null: into.Append("null"); break;
                default: into.Append(node.ToJsonString(Compact)); break;
            }
        }
    }

    /// <summary>Every line of a value after its first moved in by the indent of the line it starts on.</summary>
    private static string Indent(string json, string indent) => json.Replace("\n", "\n" + indent);

    private static string LineIndent(byte[] bytes, int at)
    {
        int start = at;
        while (start > 0 && bytes[start - 1] != '\n') start--;
        int end = start;
        while (end < bytes.Length && (bytes[end] == ' ' || bytes[end] == '\t')) end++;
        return Encoding.UTF8.GetString(bytes, start, end - start);
    }

    private static string Splice(byte[] bytes, int start, int end, string with) =>
        Encoding.UTF8.GetString(bytes, 0, start) + with + Encoding.UTF8.GetString(bytes, end, bytes.Length - end);

    /// <summary>A member put at the end of its object or array, on a line of its own like the one before it.</summary>
    private static string Add(byte[] bytes, Node container, string name, string json, bool isRoot)
    {
        bool isArray = container.Kind == JsonTokenType.StartArray;
        if (isArray && name != "+" && !int.TryParse(name, out _)) throw new ArgumentException($"'{name}' is no place in an array");
        if (!isArray && container.Kind != JsonTokenType.StartObject) throw new ArgumentException("only an object or an array holds members");
        string label = isArray ? "" : Quote(name) + ": ";

        if (container.Members.Count == 0)
        {
            string outer = LineIndent(bytes, container.Start), inner = outer + "  ";
            return Splice(bytes, container.Start + 1, container.End - 1, "\n" + inner + label + Indent(json, inner) + "\n" + outer);
        }
        var last = container.Members[^1];
        string indent = LineIndent(bytes, last.NameStart);
        // Beside records written a line each, a record of a line
        if (!isRoot && last.Value.Kind is JsonTokenType.StartObject or JsonTokenType.StartArray && SameLine(bytes, last.Value.Start, last.Value.End))
            json = OneLine(json);
        // An array of one-line records written on one line stays on one line
        bool oneLine = isArray && SameLine(bytes, container.Start, container.End) && !json.Contains('\n');
        string glue = oneLine ? ", " : ",\n" + indent;
        int after = last.Value.End;
        // A trailing comma the file already has is kept as the separator
        int probe = after;
        while (probe < container.End - 1 && bytes[probe] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n') probe++;
        if (bytes[probe] == ',') { after = probe + 1; glue = oneLine ? " " : "\n" + indent; }
        return Splice(bytes, after, after, glue + label + Indent(json, indent));
    }

    private static bool SameLine(byte[] bytes, int start, int end)
    {
        for (int i = start; i < end; i++) if (bytes[i] == '\n') return false;
        return true;
    }

    /// <summary>A member taken out with the comma that joined it, and its line where it had one of its own.</summary>
    private static string Remove(byte[] bytes, Node container, int index)
    {
        var member = container.Members[index];
        if (container.Members.Count == 1)
            return Splice(bytes, container.Start + 1, container.End - 1, "");

        if (index < container.Members.Count - 1)
        {
            // From the start of its line (or its name, when something stands before it on the line) to the next member
            int start = member.NameStart, lineStart = start;
            while (lineStart > 0 && bytes[lineStart - 1] is (byte)' ' or (byte)'\t') lineStart--;
            bool ownLine = lineStart == 0 || bytes[lineStart - 1] == '\n';
            if (ownLine) start = lineStart;
            int end = member.Value.End;
            while (end < bytes.Length && bytes[end] != ',') end++;
            end++;
            if (ownLine)
            {
                // The rest of the line goes with it
                int probe = end;
                while (probe < bytes.Length && bytes[probe] is (byte)' ' or (byte)'\t' or (byte)'\r') probe++;
                if (probe < bytes.Length && bytes[probe] == '\n') end = probe + 1;
            }
            else while (end < bytes.Length && bytes[end] == ' ') end++;
            return Splice(bytes, start, end, "");
        }

        // The last: from the end of the one before it (its comma with it) to its own end
        var before = container.Members[index - 1];
        return Splice(bytes, before.Value.End, member.Value.End, "");
    }
}
