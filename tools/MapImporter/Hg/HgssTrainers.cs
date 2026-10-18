using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MapImporter.Gba;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Hg;

/// <summary>
/// HeartGold's trainers (plan 20 · J1; <c>files/poketool/trainer/trainers.json</c>, numbered by
/// <c>include/constants/trainers.h</c>) as records in the shape of Platinum's <c>Data/trainers.json</c> (plan 18's
/// decision 8): the id (the constant without <c>TRAINER_</c>, lower case), the name, the class, the AI's routines
/// (the same engine's bits as Platinum's, <see cref="AiFlags"/>), the items, the double battle and the team
/// (species, level, the IVs' share of 255 the original calls difficulty, the held item and the moves the trainer
/// chose). The file's <c>messages</c>, the trainers' lines, are never read. Prize money and personalities are left
/// at nought, as the GBA reader leaves them: their tables are a later session's.
/// </summary>
public static class HgssTrainers
{
    /// <summary>The moves the fourth generation spelt otherwise than the data, which takes the later spelling.</summary>
    private static readonly Dictionary<string, string> Renamed = new(StringComparer.Ordinal)
    {
        ["MOVE_FAINT_ATTACK"] = "MOVE_FEINT_ATTACK", ["MOVE_VICE_GRIP"] = "MOVE_VISE_GRIP", ["MOVE_HI_JUMP_KICK"] = "MOVE_HIGH_JUMP_KICK",
        ["MOVE_SMELLING_SALT"] = "MOVE_SMELLING_SALTS"
    };

    public static List<TrainerRecord> Parse(string json, string trainersH, List<string> problems)
    {
        var ids = new Dictionary<int, string>();
        foreach (Match m in Regex.Matches(trainersH, @"^#define\s+(TRAINER_\w+)\s+(\d+)", RegexOptions.Multiline))
            if (m.Groups[1].Value is not ("TRAINER_NONE" or "FIRST_TRAINER_INDEX") && !m.Groups[1].Value.StartsWith("TRAINER_NUM", StringComparison.Ordinal))
                ids.TryAdd(int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), m.Groups[1].Value);

        using var doc = JsonDocument.Parse(json);
        var records = new List<TrainerRecord>();
        int number = 0;
        foreach (var t in doc.RootElement.GetProperty("trainers").EnumerateArray())
        {
            int n = number++;
            if (!ids.TryGetValue(n, out string? id)) continue;   // number 0 is nobody
            var party = new List<TrainerPokemonRecord>();
            foreach (var p in t.GetProperty("party").EnumerateArray())
            {
                string species = p.GetProperty("species").GetString() ?? "";
                var member = new TrainerPokemonRecord
                {
                    Species = Name(GbaNames.Species(species), species, id, problems),
                    Level = p.GetProperty("level").GetInt32(),
                    IvScale = p.TryGetProperty("difficulty", out var d) ? d.GetInt32() : 0
                };
                if (p.TryGetProperty("item", out var item) && item.GetString() is { } held && held != "ITEM_NONE")
                    member.Item = Name(GbaNames.Item(held), held, id, problems);
                if (p.TryGetProperty("moves", out var moves))
                    member.Moves = moves.EnumerateArray().Select(m => m.GetString() ?? "").Where(m => m.Length > 0 && m != "MOVE_NONE")
                        .Select(m => Name(GbaNames.Move(Renamed.GetValueOrDefault(m, m)), m, id, problems)).ToList();
                party.Add(member);
            }
            if (party.Count == 0) continue;
            uint flags = (uint)t.GetProperty("ai_flags").GetInt64();
            records.Add(new TrainerRecord
            {
                Id = id["TRAINER_".Length..].ToLowerInvariant(),
                Name = (t.GetProperty("name").GetString() ?? "").Replace("{TRNAME}", "", StringComparison.Ordinal),
                Class = ClassOf(t.GetProperty("class").GetString() ?? ""),
                Ai = Enum.GetValues<AiFlags>().Where(f => f != AiFlags.None && (flags & (uint)f) != 0).Select(f => f.ToString()).ToList(),
                Items = t.GetProperty("items").EnumerateArray().Select(i => i.GetString() ?? "").Where(i => i.Length > 0 && i != "ITEM_NONE")
                    .Select(i => Name(GbaNames.Item(i), i, id, problems)).ToList(),
                DoubleBattle = t.GetProperty("double").GetInt32() != 0,
                Party = party
            });
        }
        return records;
    }

    /// <summary>
    /// <c>TRAINERCLASS_YOUNGSTER</c> to "Youngster", <c>TRAINERCLASS_LEADER_FALKNER</c> to "Leader" (Platinum's
    /// class for every Gym Leader, the person being the trainer's name), <c>TRAINERCLASS_ELITE_FOUR_WILL</c> to "Elite Four".
    /// </summary>
    public static string ClassOf(string constant)
    {
        string body = constant.StartsWith("TRAINERCLASS_", StringComparison.Ordinal) ? constant["TRAINERCLASS_".Length..] : constant;
        if (body.StartsWith("LEADER_", StringComparison.Ordinal)) return "Leader";
        if (body.StartsWith("ELITE_FOUR_", StringComparison.Ordinal)) return "Elite Four";
        return GbaTables.Titled(body.Replace('_', ' ')).Replace("Pokemon", "Pokémon", StringComparison.Ordinal);
    }

    private static string Name(string? name, string constant, string trainer, List<string> problems)
    {
        if (name != null) return name;
        string problem = $"{trainer}: {constant} is not in the game's data";
        if (!problems.Contains(problem)) problems.Add(problem);
        return constant;
    }
}

/// <summary>One contact of the Pokégear's phone book (<c>files/tel/pmtel_book.json</c>), as <c>Data/world/johto/phone.json</c> keeps it.</summary>
public sealed class PhoneContact
{
    /// <summary>The contact's constant without <c>PHONE_CONTACT_</c>, lower case (<c>mother</c>, <c>prof_elm</c>).</summary>
    public string Id { get; set; } = "";
    public int Type { get; set; }
    public string Class { get; set; } = "";

    /// <summary>The trainer they are, by the id of the region's <c>trainers.json</c>; null for nobody's.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Trainer { get; set; }

    /// <summary>The area they are met in, by its key (<c>new_bark_player_house_1f</c>).</summary>
    public string Area { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Gift { get; set; }

    /// <summary>The day of the week they ask for a rematch on (<c>Wednesday</c>); null for none.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? RematchDay { get; set; }

    /// <summary>The time of day of the rematch (<c>Morning</c>, <c>Day</c>, <c>Night</c>); null for none.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? RematchTime { get; set; }
}

/// <summary>The phone book (plan 20 · J1): who can be called, where they are met, their gift and their rematch's day and hour. No line of a call is read.</summary>
public static class Phonebook
{
    public static List<PhoneContact> Parse(string json, List<string> problems)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<PhoneContact>();
        foreach (var c in doc.RootElement.GetProperty("pmtel_book").EnumerateArray())
        {
            string Text(string name) => c.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";
            string id = Text("id");
            string trainer = Text("trainerId"), gift = Text("gift"), day = Text("rematchWeekday"), time = Text("rematchTimeOfDay");
            list.Add(new PhoneContact
            {
                Id = Regex.Replace(id.StartsWith("PHONE_CONTACT_", StringComparison.Ordinal) ? id["PHONE_CONTACT_".Length..] : id, "_+", "_").ToLowerInvariant(),
                Type = c.TryGetProperty("type", out var t) ? t.GetInt32() : 0,
                Class = HgssTrainers.ClassOf(Text("trainerClass").Replace("TRAINERCLASS_PHONE_", "TRAINERCLASS_", StringComparison.Ordinal)),
                Trainer = trainer is "" or "TRAINER_NONE" ? null : trainer["TRAINER_".Length..].ToLowerInvariant(),
                Area = Text("mapId").StartsWith("MAP_", StringComparison.Ordinal) ? Text("mapId")["MAP_".Length..].ToLowerInvariant() : Text("mapId"),
                Gift = gift is "" or "ITEM_NONE" ? null : GbaNames.Item(gift) ?? Problem(gift, id, problems),
                RematchDay = day is "" or "RTC_WEEK_MAX" ? null : GbaTables.Titled(day["RTC_WEEK_".Length..]),
                RematchTime = time switch
                {
                    "TIMEOFDAY_WILD_MORN" => "Morning",
                    "TIMEOFDAY_WILD_DAY" => "Day",
                    "TIMEOFDAY_WILD_NITE" => "Night",
                    _ => null
                }
            });
        }
        return list;
    }

    private static string Problem(string constant, string contact, List<string> problems)
    {
        problems.Add($"{contact}: the gift {constant} is not in the game's data");
        return constant;
    }
}
