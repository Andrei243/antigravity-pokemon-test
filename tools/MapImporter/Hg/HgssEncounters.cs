using System.Text.Json;

namespace MapImporter.Hg;

/// <summary>A species or a level as HGSS's tables give it: one value, or one for each version where they differ.</summary>
public readonly record struct Versioned<T>(T HeartGold, T SoulSilver)
{
    public bool Split => !EqualityComparer<T>.Default.Equals(HeartGold, SoulSilver);
}

/// <summary>One slot of a table: its levels and its species at each time of day (Morning, Day, Night), by version.</summary>
public sealed record HgSlot(Versioned<int> MinLevel, Versioned<int> MaxLevel, Versioned<string> Morning, Versioned<string> Day, Versioned<string> Night)
{
    /// <summary>The slot in one version at one time (<c>morn</c>, <c>day</c>, <c>nite</c>).</summary>
    public string Species(bool heartGold, string time)
    {
        var s = time switch { "morn" => Morning, "nite" => Night, _ => Day };
        return heartGold ? s.HeartGold : s.SoulSilver;
    }
}

/// <summary>A table of slots with its rate (out of 100; 0 for none).</summary>
public sealed record HgTable(int Rate, List<HgSlot> Slots)
{
    public static readonly HgTable None = new(0, new List<HgSlot>());
}

/// <summary>A map's wild Pokémon (<c>gs_enc_data.json</c>), by its short name (<c>R29</c>).</summary>
public sealed record HgEncounters(
    string Map, HgTable Land, HgTable Surf, HgTable RockSmash, HgTable OldRod, HgTable GoodRod, HgTable SuperRod,
    List<Versioned<string>> Hoenn, List<Versioned<string>> Sinnoh, Versioned<string>? LandSwarm, Versioned<string>? SurfSwarm, Versioned<string>? FishSwarm, Versioned<string>? NightFish);

/// <summary>A map's Headbutt trees (<c>files/arc/headbutt.json</c>): the three tables and the trees, each its tiles.</summary>
public sealed record HgHeadbutt(string Map, List<HgSlot> Common, List<HgSlot> Rare, List<HgSlot> Secret,
    List<List<(int X, int Z)>> Trees, List<List<(int X, int Z)>> SecretTrees);

/// <summary>One of the Safari Zone's areas (<c>files/arc/safari_enc.json</c>): each way's slots by time, and the slots objects bring.</summary>
public sealed record HgSafariArea(string Area, Dictionary<string, List<HgSlot>> Ways, Dictionary<string, int> BonusSlots);

/// <summary>
/// HeartGold's tables of wild Pokémon (plan 20 · J1): <c>gs_enc_data.json</c> with both versions' slots, the
/// Headbutt trees and the Safari Zone's areas. Species are the data's constants (<c>SPECIES_PIDGEY</c>); a value
/// that differs by version is an object of <c>HEARTGOLD</c> and <c>SOULSILVER</c> (the Headbutt file's <c>gold</c>
/// and <c>silver</c>).
/// </summary>
public static class HgssEncounters
{
    public static List<HgEncounters> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<HgEncounters>();
        foreach (var e in doc.RootElement.GetProperty("encounters").EnumerateArray())
        {
            var fishing = e.TryGetProperty("fishing", out var f) ? f : default;
            list.Add(new HgEncounters(
                e.GetProperty("map").GetString() ?? "",
                Table(e, "land"), Table(e, "surf"), Table(e, "rock_smash"),
                Table(fishing, "old_rod"), Table(fishing, "good_rod"), Table(fishing, "super_rod"),
                Names(e, "hoenn"), Names(e, "sinnoh"),
                Text(e, "landSwarm"), Text(e, "surfSwarm"), Text(e, "fishSwarm"), Text(e, "nightFish")));
        }
        return list;
    }

    public static List<HgHeadbutt> ParseHeadbutt(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<HgHeadbutt>();
        foreach (var t in doc.RootElement.GetProperty("tables").EnumerateArray())
        {
            List<List<(int, int)>> Trees(string name) => t.GetProperty(name).EnumerateArray()
                .Select(tree => tree.EnumerateArray().Select(p => (p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32())).ToList()).ToList();
            List<HgSlot> Mons(string name) => t.GetProperty(name).EnumerateArray().Select(m =>
            {
                var min = Number(m.GetProperty("minLevel"));
                var max = Number(m.GetProperty("maxLevel"));
                var species = Species(m.GetProperty("species"));
                return new HgSlot(min, max, species, species, species);
            }).ToList();
            list.Add(new HgHeadbutt(t.GetProperty("Map").GetString() ?? "", Mons("CommonMons"), Mons("RareMons"), Mons("SecretMons"), Trees("Trees"), Trees("SecretTrees")));
        }
        return list;
    }

    public static List<HgSafariArea> ParseSafari(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<HgSafariArea>();
        foreach (var a in doc.RootElement.GetProperty("encounters").EnumerateArray())
        {
            var ways = new Dictionary<string, List<HgSlot>>(StringComparer.Ordinal);
            var bonus = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string way in new[] { "land", "surf", "oldrod", "goodrod", "superrod" })
            {
                if (!a.TryGetProperty(way, out var w) || !w.TryGetProperty("mons", out var mons)) continue;
                List<(Versioned<string> Species, Versioned<int> Level)> Time(string time) => mons.TryGetProperty(time, out var slots)
                    ? slots.EnumerateArray().Select(s => (Species(s.GetProperty("species")), Number(s.GetProperty("level")))).ToList() : new();
                var morn = Time("morn"); var day = Time("day"); var nite = Time("nite");
                ways[way] = Enumerable.Range(0, Math.Max(morn.Count, Math.Max(day.Count, nite.Count))).Select(i =>
                {
                    var d = i < day.Count ? day[i] : morn[i];
                    return new HgSlot(d.Level, d.Level, i < morn.Count ? morn[i].Species : d.Species, d.Species, i < nite.Count ? nite[i].Species : d.Species);
                }).ToList();
                bonus[way] = w.TryGetProperty("bonus_mons", out var b) ? b.GetArrayLength() : 0;
            }
            list.Add(new HgSafariArea(a.GetProperty("area").GetString() ?? "", ways, bonus));
        }
        return list;
    }

    private static HgTable Table(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var t)) return HgTable.None;
        int rate = t.TryGetProperty("rate", out var r) ? r.GetInt32() : 0;
        var slots = new List<HgSlot>();
        foreach (var m in t.GetProperty("mons").EnumerateArray())
        {
            var level = m.GetProperty("level");
            Versioned<int> min, max;
            if (level.ValueKind == JsonValueKind.Object && level.TryGetProperty("min", out var lo)) (min, max) = (Number(lo), Number(level.GetProperty("max")));
            else min = max = Number(level);
            var species = m.GetProperty("species");
            if (species.ValueKind == JsonValueKind.Object && species.TryGetProperty("morn", out var morn))
                slots.Add(new HgSlot(min, max, Species(morn), Species(species.GetProperty("day")), Species(species.GetProperty("nite"))));
            else
            {
                var one = Species(species);
                slots.Add(new HgSlot(min, max, one, one, one));
            }
        }
        return new HgTable(rate, slots);
    }

    private static Versioned<int> Number(JsonElement e) => e.ValueKind == JsonValueKind.Number
        ? new(e.GetInt32(), e.GetInt32())
        : new(Pick(e, "HEARTGOLD", "gold").GetInt32(), Pick(e, "SOULSILVER", "silver").GetInt32());

    private static Versioned<string> Species(JsonElement e) => e.ValueKind == JsonValueKind.String
        ? new(e.GetString() ?? "", e.GetString() ?? "")
        : new(Pick(e, "HEARTGOLD", "gold").GetString() ?? "", Pick(e, "SOULSILVER", "silver").GetString() ?? "");

    private static JsonElement Pick(JsonElement e, string a, string b) =>
        e.TryGetProperty(a, out var v) ? v : e.TryGetProperty(b, out v) ? v : throw new InvalidDataException($"A value split by version has neither {a} nor {b}");

    private static List<Versioned<string>> Names(JsonElement e, string name) =>
        e.TryGetProperty(name, out var list) ? list.EnumerateArray().Select(Species).ToList() : new List<Versioned<string>>();

    private static Versioned<string>? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) ? Species(v) : null;
}
