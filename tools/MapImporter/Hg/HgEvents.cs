using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapImporter.Hg;

/// <summary>
/// An area's events as HeartGold keeps them (plan 20 · J1; <c>files/fielddata/eventdata/zone_event/NNN_&lt;short&gt;.json</c>:
/// <c>bgs</c>, <c>objects</c>, <c>warps</c>, <c>coords</c>), read into the same <see cref="AreaEvents"/> Platinum's
/// files become, so whatever reads one reads the other. A position is a tile of the area's matrix, as in Platinum.
/// What a script number says is kept as the label it is (<c>_EV_scr_seq_T20_000 + 1</c> becomes <c>T20_000+1</c>,
/// <c>std_trainer(TRAINER_YOUNGSTER_JOEY)</c> the trainer's constant), never what the script does or says.
/// </summary>
public static class HgEvents
{
    public static AreaEvents Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var events = new AreaEvents();
        IEnumerable<JsonElement> List(string name) => root.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray() : Enumerable.Empty<JsonElement>();
        int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        string Text(JsonElement e, string name) => e.TryGetProperty(name, out var v)
            ? v.ValueKind == JsonValueKind.Number ? v.GetInt64().ToString(CultureInfo.InvariantCulture) : v.GetString() ?? "" : "";

        foreach (var b in List("bgs"))
            events.Signs.Add(new AreaEvents.BgEvent
            {
                Script = Script(Text(b, "scriptId")), Type = Int(b, "type"), X = Int(b, "x"), Z = Int(b, "z"), Y = Int(b, "y"),
                PlayerFacingDir = Int(b, "dir").ToString(CultureInfo.InvariantCulture)
            });
        foreach (var o in List("objects"))
        {
            string flag = Text(o, "eventFlag");
            events.Objects.Add(new AreaEvents.ObjectEvent
            {
                Id = Text(o, "id"),
                GraphicsId = Text(o, "spriteId"),
                MovementType = Int(o, "movement").ToString(CultureInfo.InvariantCulture),
                // The original's object type: 1 a trainer who sees (param0 tiles ahead), 2 one who looks every way
                TrainerType = Int(o, "type") switch { 0 => "", var t => t.ToString(CultureInfo.InvariantCulture) },
                HiddenFlag = flag is "FLAG_NOTHING" ? "" : flag,
                Script = Script(Text(o, "scriptId")),
                InitialDir = Int(o, "facingDirection"),
                Data = new List<int> { Int(o, "param0"), Int(o, "param1"), Int(o, "param2") },
                MovementRangeX = Int(o, "xRange"),
                MovementRangeZ = Int(o, "yRange"),
                X = Int(o, "x"), Z = Int(o, "z"), Y = Int(o, "y")
            });
        }
        foreach (var w in List("warps"))
            events.Warps.Add(new AreaEvents.WarpEvent { X = Int(w, "x"), Z = Int(w, "z"), DestHeaderId = Text(w, "header"), DestWarpId = Int(w, "anchor") });
        foreach (var c in List("coords"))
            events.Triggers.Add(new AreaEvents.CoordEvent
            {
                Script = Script(Text(c, "scriptId")), X = Int(c, "x"), Z = Int(c, "z"), Y = Int(c, "y"),
                Width = Int(c, "w"), Length = Int(c, "h"), Var = Text(c, "var"), Value = Int(c, "val").ToString(CultureInfo.InvariantCulture)
            });
        return events;
    }

    /// <summary>A script's label as the events give it, shortened: the area's own scripts by their number, a trainer by its constant.</summary>
    public static string Script(string label)
    {
        if (Regex.Match(label, @"^std_trainer\((TRAINER_\w+)\)$") is { Success: true } trainer) return trainer.Groups[1].Value;
        if (Regex.Match(label, @"^_EV_scr_seq_(\w+)\s*\+\s*(\d+)$") is { Success: true } own) return own.Groups[1].Value + "+" + own.Groups[2].Value;
        return label;
    }

    /// <summary>
    /// What an item's ball holds, by its script's label, which names it (<c>std_itemball_r29_potion</c>): the item's
    /// constant after the area's short name, as the data names items (<c>ITEM_POTION</c>). Null when the label is no
    /// item ball's.
    /// </summary>
    public static string? BallItem(string script, string shortName)
    {
        if (!script.StartsWith("std_itemball_", StringComparison.Ordinal)) return null;
        string rest = script["std_itemball_".Length..];
        string prefix = shortName.ToLowerInvariant() + "_";
        if (rest.StartsWith(prefix, StringComparison.Ordinal)) rest = rest[prefix.Length..];
        // A ball can be labelled by another map's short name (a room sharing its neighbour's script: d22r0101_),
        // and a second ball of the same item ends in _2
        rest = Regex.Replace(rest, @"^(?:[a-z]+\d+[a-z0-9]*_)+", "");
        rest = Regex.Replace(rest, @"_\d+$", "");
        return "ITEM_" + rest.ToUpperInvariant();
    }
}
