using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Gba;

/// <summary>One slot of a GBA table of wild Pokémon: its species (the game's constant), its levels and how often it is met in a hundred.</summary>
public sealed record GbaWildSlot(string Species, int MinLevel, int MaxLevel, int Weight);

/// <summary>One of a map's tables of wild Pokémon (land, water, Rock Smash, a rod) with the chance in a hundred that a step or a cast meets one.</summary>
public sealed record GbaWildTable(int Rate, List<GbaWildSlot> Slots);

/// <summary>A map's tables of wild Pokémon, as <c>wild_encounters.json</c> has them (plan 18 · W7).</summary>
public sealed class GbaWild
{
    public GbaWildTable? Land { get; init; }
    public GbaWildTable? Water { get; init; }
    public GbaWildTable? RockSmash { get; init; }
    public GbaWildTable? OldRod { get; init; }
    public GbaWildTable? GoodRod { get; init; }
    public GbaWildTable? SuperRod { get; init; }
}

/// <summary>A place a fainted team wakes in and Fly lands (<c>heal_locations.json</c>): a tile of a map, and the room of the nurse or the mother.</summary>
public sealed record GbaHealLocation(string Id, string Map, int X, int Y, string RespawnMap);

/// <summary>
/// The GBA game's tables beside its maps (plan 18 · W7): what each item's ball holds, which trainer each map's
/// trainer scripts battle as, the wild Pokémon of each map, the trainers and their parties, and the heal locations.
/// Everything here is a name or a number: the scripts' text beside the lines read is never kept.
/// </summary>
public sealed class GbaTables
{
    /// <summary>An item ball's script label to the item it holds (<c>Route2_EventScript_ItemEther</c> to <c>ITEM_ETHER</c>) and how many.</summary>
    public Dictionary<string, (string Item, int Count)> ItemBalls { get; } = new(StringComparer.Ordinal);

    /// <summary>A trainer's script label to the trainer it battles as (<c>Route3_EventScript_Robin</c> to <c>TRAINER_YOUNGSTER_BEN</c>).</summary>
    public Dictionary<string, string> TrainerScripts { get; } = new(StringComparer.Ordinal);

    /// <summary>Each map's wild Pokémon by its constant (<c>MAP_ROUTE1</c>).</summary>
    public Dictionary<string, GbaWild> Wild { get; } = new(StringComparer.Ordinal);

    public List<TrainerRecord> Trainers { get; } = new();
    public List<GbaHealLocation> HealLocations { get; } = new();

    /// <summary>
    /// Every berry tree of the game's maps by its constant (<c>BERRY_TREE_ROUTE_104_CHERI_1</c>), in the order of the
    /// names (plan 21 · H2, part b): a tree's place in the list is its patch past Sinnoh's
    /// (<c>BerryPatches.HoennFirst</c> + the place), the same whichever areas are open, since every map is read.
    /// </summary>
    public List<string> BerryTrees { get; } = new();
    public List<string> Problems { get; } = new();

    public GbaTables() { }

    /// <summary>
    /// Reads what of the files is in the checkout; a file that isn't there leaves its table empty. A trainer's id
    /// takes <paramref name="trainerPrefix"/> before it (<c>kanto_</c>, plan 19 · K2), so a region's trainers never
    /// meet Platinum's ids, or each other's, in the save's list of trainers beaten.
    /// </summary>
    public GbaTables(string dir, IEnumerable<GbaMap> maps, string trainerPrefix = "", IReadOnlyDictionary<string, int>? moneyRates = null)
    {
        string Path_(params string[] parts) => Path.Combine(new[] { dir }.Concat(parts).ToArray());
        maps = maps.ToList();
        BerryTrees.AddRange(maps.SelectMany(m => m.Objects).Select(o => o.SightOrBerryTree)
            .Where(s => s.StartsWith("BERRY_TREE_", StringComparison.Ordinal)).Distinct().OrderBy(s => s, StringComparer.Ordinal));
        if (File.Exists(Path_("data", "scripts", "item_ball_scripts.inc")))
            foreach (var (label, item, count) in ParseItemBalls(File.ReadAllText(Path_("data", "scripts", "item_ball_scripts.inc"))))
                ItemBalls[label] = (item, count);
        // Emerald keeps a map's trainers' scripts with the map's own; FireRed keeps them all in one file
        foreach (string scripts in maps.Select(m => Path_("data", "maps", m.Name, "scripts.inc")).Append(Path_("data", "scripts", "trainers.inc")))
        {
            if (!File.Exists(scripts)) continue;
            foreach (var (label, trainer) in ParseTrainerScripts(File.ReadAllText(scripts))) TrainerScripts.TryAdd(label, trainer);
        }
        if (File.Exists(Path_("src", "data", "wild_encounters.json")))
            foreach (var (map, wild) in ParseWild(File.ReadAllText(Path_("src", "data", "wild_encounters.json")), Problems))
                Wild[map] = wild;
        if (File.Exists(Path_("src", "data", "trainers.h")) && File.Exists(Path_("src", "data", "trainer_parties.h")))
            Trainers.AddRange(ParseTrainers(File.ReadAllText(Path_("src", "data", "trainers.h")), File.ReadAllText(Path_("src", "data", "trainer_parties.h")), Problems, trainerPrefix, moneyRates));
        if (File.Exists(Path_("src", "data", "heal_locations.json")))
            HealLocations.AddRange(ParseHealLocations(File.ReadAllText(Path_("src", "data", "heal_locations.json"))));
    }

    // ---------------------------------------------------------------- the parsers, public for the tests

    /// <summary><c>Label::</c> followed by <c>finditem ITEM_X[, n]</c>, from <c>item_ball_scripts.inc</c>.</summary>
    public static IEnumerable<(string Label, string Item, int Count)> ParseItemBalls(string text)
    {
        foreach (Match m in Regex.Matches(text, @"^(\w+)::\s*\r?\n\s*finditem\s+(ITEM_\w+)(?:\s*,\s*(\d+))?", RegexOptions.Multiline))
            yield return (m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 1);
    }

    /// <summary>
    /// A map's <c>scripts.inc</c>: each label whose first <c>trainerbattle…</c> line names a trainer (the first of the
    /// two for a double battle's pair). Only the label and the constant are read.
    /// </summary>
    public static IEnumerable<(string Label, string Trainer)> ParseTrainerScripts(string text)
    {
        string? label = null;
        bool found = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            var head = Regex.Match(line, @"^(\w+)::?$");
            if (head.Success) { label = head.Groups[1].Value; found = false; continue; }
            if (label == null || found) continue;
            var battle = Regex.Match(line, @"^trainerbattle\w*\s+(?:\w+\s*,\s*)?(TRAINER_\w+)");
            if (battle.Success)
            {
                found = true;
                yield return (label, battle.Groups[1].Value);
            }
        }
    }

    /// <summary>
    /// <c>wild_encounters.json</c>'s group for the maps: each map's tables, each slot weighted by the header's
    /// <c>encounter_rates</c> (the rods' share of the fishing table by its <c>groups</c>). FireRed keeps a table for
    /// each of its two versions (<c>…_FireRed</c>, <c>…_LeafGreen</c>): FireRed's is read. A map with more than one
    /// table (Emerald's Altering Cave) keeps its first, and the others are told.
    /// </summary>
    public static IEnumerable<(string Map, GbaWild Wild)> ParseWild(string json, List<string> problems)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var group in doc.RootElement.GetProperty("wild_encounter_groups").EnumerateArray())
        {
            if (!(group.TryGetProperty("for_maps", out var forMaps) && forMaps.ValueKind == JsonValueKind.True)) continue;
            var rates = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var rods = new Dictionary<string, int[]>(StringComparer.Ordinal);
            foreach (var field in group.GetProperty("fields").EnumerateArray())
            {
                string type = field.GetProperty("type").GetString()!;
                rates[type] = field.GetProperty("encounter_rates").EnumerateArray().Select(r => r.GetInt32()).ToArray();
                if (field.TryGetProperty("groups", out var groups))
                    foreach (var g in groups.EnumerateObject()) rods[g.Name] = g.Value.EnumerateArray().Select(i => i.GetInt32()).ToArray();
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in group.GetProperty("encounters").EnumerateArray())
            {
                if (!e.TryGetProperty("map", out var mapProperty)) continue;
                string map = mapProperty.GetString()!;
                string label = e.TryGetProperty("base_label", out var l) ? l.GetString()! : "";
                if (label.EndsWith("_LeafGreen", StringComparison.Ordinal)) continue;
                if (!seen.Add(map)) { problems.Add($"{map}: a second table of wild Pokémon ({label}) left out: the first is kept"); continue; }

                GbaWildTable? Table(string type, int[]? only = null)
                {
                    if (!e.TryGetProperty(type, out var t) || t.ValueKind != JsonValueKind.Object) return null;
                    var mons = t.GetProperty("mons").EnumerateArray().ToList();
                    var weights = rates.GetValueOrDefault(type) ?? Array.Empty<int>();
                    var slots = new List<GbaWildSlot>();
                    foreach (int i in only ?? Enumerable.Range(0, mons.Count).ToArray())
                    {
                        if (i >= mons.Count) continue;
                        var m = mons[i];
                        slots.Add(new GbaWildSlot(m.GetProperty("species").GetString()!, m.GetProperty("min_level").GetInt32(),
                            m.GetProperty("max_level").GetInt32(), i < weights.Length ? weights[i] : 1));
                    }
                    return new GbaWildTable(t.GetProperty("encounter_rate").GetInt32(), slots);
                }
                yield return (map, new GbaWild
                {
                    Land = Table("land_mons"),
                    Water = Table("water_mons"),
                    RockSmash = Table("rock_smash_mons"),
                    OldRod = rods.TryGetValue("old_rod", out var o) ? Table("fishing_mons", o) : null,
                    GoodRod = rods.TryGetValue("good_rod", out var g2) ? Table("fishing_mons", g2) : null,
                    SuperRod = rods.TryGetValue("super_rod", out var s) ? Table("fishing_mons", s) : null
                });
            }
        }
    }

    /// <summary><c>heal_locations.json</c>: each place's map and tile, and the map of the room a white-out wakes in (FireRed's <c>respawn_map</c>).</summary>
    public static IEnumerable<GbaHealLocation> ParseHealLocations(string json)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var h in doc.RootElement.GetProperty("heal_locations").EnumerateArray())
            yield return new GbaHealLocation(h.GetProperty("id").GetString()!, h.GetProperty("map").GetString()!, h.GetProperty("x").GetInt32(),
                h.GetProperty("y").GetInt32(), h.TryGetProperty("respawn_map", out var r) ? r.GetString()! : "");
    }

    /// <summary>The bits of the GBA's <c>AI_SCRIPT_*</c> flags in their order, which are the order of Platinum's (<c>AiFlags</c>).</summary>
    private static readonly string[] AiBits =
    {
        "CHECK_BAD_MOVE", "TRY_TO_FAINT", "CHECK_VIABILITY", "SETUP_FIRST_TURN", "RISKY", "PREFER_STRONGEST_MOVE", "PREFER_BATON_PASS",
        "DOUBLE_BATTLE", "HP_AWARE"
    };
    private static readonly string[] AiNames = { "Basic", "EvalAttack", "Expert", "SetupFirstTurn", "Risky", "PrioritizeExtremes", "BatonPass", "TagStrategy", "CheckHp" };

    /// <summary>
    /// <c>trainers.h</c> with the parties of <c>trainer_parties.h</c>, as records in the shape of <c>trainers.json</c>
    /// (decision 8): the id (the constant's name, lower case), the name, the class, the AI's routines, the items, the
    /// double battle and the team (species, level, the IVs' share of 255, the held item and the moves the trainer
    /// chose). The prize money and the personality are left at nought: they are worked out from tables W7 doesn't
    /// read (the class's money, the name's hash), which the region's plan adds. A trainer with no party (TRAINER_NONE)
    /// is left out.
    /// </summary>
    public static List<TrainerRecord> ParseTrainers(string trainersH, string partiesH, List<string> problems, string idPrefix = "", IReadOnlyDictionary<string, int>? moneyRates = null)
    {
        var parties = new Dictionary<string, List<TrainerPokemonRecord>>(StringComparer.Ordinal);
        // A party ends at the first "};" (FireRed's empty ones are a line each, {DUMMY_TRAINER_MON};)
        foreach (Match p in Regex.Matches(partiesH.Replace("\r\n", "\n"), @"(sParty_\w+)\s*\[\]\s*=\s*\{(.*?)\};", RegexOptions.Singleline))
        {
            var members = new List<TrainerPokemonRecord>();
            foreach (Match mon in Regex.Matches(p.Groups[2].Value, @"\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}", RegexOptions.Singleline))
            {
                string body = mon.Groups[1].Value;
                string Field(string name) => Regex.Match(body, @"\." + name + @"\s*=\s*([^,\n}]+)") is { Success: true } f ? f.Groups[1].Value.Trim() : "";
                string species = Field("species");
                if (species.Length == 0) continue;
                var member = new TrainerPokemonRecord
                {
                    Species = GbaNames.Species(species) ?? species,
                    Level = int.TryParse(Field("lvl"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lvl) ? lvl : 0,
                    IvScale = int.TryParse(Field("iv"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv) ? iv : 0
                };
                string item = Field("heldItem");
                if (item.Length > 0 && item != "ITEM_NONE") member.Item = GbaNames.Item(item) ?? item;
                if (Regex.Match(body, @"\.moves\s*=\s*\{([^}]*)\}") is { Success: true } moves)
                    member.Moves = moves.Groups[1].Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0 && s != "MOVE_NONE")
                        .Select(s => GbaNames.Move(s) ?? s).ToList();
                members.Add(member);
            }
            parties[p.Groups[1].Value] = members;
        }

        var records = new List<TrainerRecord>();
        foreach (Match t in Regex.Matches(trainersH, @"\[(TRAINER_\w+)\]\s*=\s*\{(.*?)\n\s*\},", RegexOptions.Singleline))
        {
            string body = t.Groups[2].Value;
            string Field(string name) => Regex.Match(body, @"\." + name + @"\s*=\s*([^\n]+?),?\s*$", RegexOptions.Multiline) is { Success: true } f ? f.Groups[1].Value.Trim() : "";
            var party = Regex.Match(body, @"(sParty_\w+)");
            if (!party.Success) continue;
            if (!parties.TryGetValue(party.Groups[1].Value, out var members)) { problems.Add($"{t.Groups[1].Value}: no party {party.Groups[1].Value}"); continue; }
            // A trainer the game keeps only as a placeholder (FireRed's Hoenn classes, DUMMY_TRAINER_MON) has nobody
            if (members.Count == 0) continue;
            string name = Regex.Match(body, @"\.trainerName\s*=\s*_\(""([^""]*)""\)") is { Success: true } n ? n.Groups[1].Value : "";
            var ai = new List<string>();
            string flags = Field("aiFlags");
            for (int bit = 0; bit < AiBits.Length; bit++)
                if (flags.Contains("AI_SCRIPT_" + AiBits[bit], StringComparison.Ordinal)) ai.Add(AiNames[bit]);
            var items = Regex.Match(body, @"\.items\s*=\s*\{([^}]*)\}") is { Success: true } i
                ? i.Groups[1].Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0 && s != "ITEM_NONE").Select(s => GbaNames.Item(s) ?? s).ToList()
                : new List<string>();
            string trainerClass = Field("trainerClass").Replace("TRAINER_CLASS_", "", StringComparison.Ordinal);
            bool doubleBattle = Field("doubleBattle") == "TRUE";
            bool female = Field("encounterMusic_gender").Contains("F_TRAINER_FEMALE", StringComparison.Ordinal);
            var personalities = Personalities(name, members.Select(m => m.Species).ToList(), doubleBattle, female);
            records.Add(new TrainerRecord
            {
                Id = idPrefix + t.Groups[1].Value["TRAINER_".Length..].ToLowerInvariant(),
                Name = Titled(name),
                Class = Titled(trainerClass.Replace('_', ' ')),
                Ai = ai,
                Items = items,
                DoubleBattle = doubleBattle,
                PrizeMoney = PrizeMoney(trainerClass, members[^1].Level, doubleBattle, moneyRates),
                Party = members.Select((m, i) => new TrainerPokemonRecord
                {
                    Species = m.Species, Level = m.Level, IvScale = m.IvScale, Personality = personalities[i], Item = m.Item, Moves = m.Moves?.ToList()
                }).ToList()
            });
        }
        return records;
    }

    /// <summary>
    /// FireRed's <c>gTrainerMoneyTable</c> (<c>src/battle_main.c</c>) for the classes of Kanto, which the checkout's
    /// folders don't hold: the factor of a trainer's prize. A class it doesn't list takes the table's last row, 5.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> MoneyRates = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["LEADER"] = 25, ["ELITE_FOUR"] = 25, ["PKMN_PROF"] = 25, ["RIVAL_EARLY"] = 4, ["RIVAL_LATE"] = 9, ["CHAMPION"] = 25,
        ["YOUNGSTER"] = 4, ["BUG_CATCHER"] = 3, ["HIKER"] = 9, ["BIRD_KEEPER"] = 6, ["PICNICKER"] = 5, ["SUPER_NERD"] = 6,
        ["FISHERMAN"] = 9, ["TEAM_ROCKET"] = 8, ["LASS"] = 4, ["BEAUTY"] = 18, ["BLACK_BELT"] = 6, ["CUE_BALL"] = 6,
        ["CHANNELER"] = 8, ["ROCKER"] = 6, ["GENTLEMAN"] = 18, ["BURGLAR"] = 22, ["SWIMMER_M"] = 1, ["ENGINEER"] = 12,
        ["JUGGLER"] = 10, ["SAILOR"] = 8, ["COOLTRAINER"] = 9, ["POKEMANIAC"] = 12, ["TAMER"] = 10, ["CAMPER"] = 5,
        ["PSYCHIC"] = 5, ["BIKER"] = 5, ["GAMER"] = 18, ["SCIENTIST"] = 12, ["CRUSH_GIRL"] = 6, ["TUBER"] = 1,
        ["PKMN_BREEDER"] = 7, ["PKMN_RANGER"] = 9, ["AROMA_LADY"] = 7, ["RUIN_MANIAC"] = 12, ["LADY"] = 50, ["PAINTER"] = 4,
        ["TWINS"] = 3, ["YOUNG_COUPLE"] = 7, ["SIS_AND_BRO"] = 1, ["COOL_COUPLE"] = 6, ["CRUSH_KIN"] = 6, ["SWIMMER_F"] = 1,
        ["PLAYER"] = 1, ["PKMN_TRAINER"] = 15, ["BOSS"] = 25
    };

    /// <summary>
    /// A trainer's prize as FireRed's <c>Cmd_getmoneyreward</c> works it out: four times the last Pokémon's level times
    /// the class's rate, twice that in a double battle.
    /// </summary>
    public static int PrizeMoney(string trainerClass, int lastLevel, bool doubleBattle, IReadOnlyDictionary<string, int>? rates = null) =>
        4 * lastLevel * (doubleBattle ? 2 : 1) * (rates ?? MoneyRates).GetValueOrDefault(trainerClass, 5);

    /// <summary>
    /// Emerald's <c>gTrainerMoneyTable</c> (<c>src/battle_main.c</c> of the pinned commit, plan 21 · H1), which the
    /// checkout's folders don't hold either: the same formula (<c>GetTrainerMoneyToGive</c>), another table. A class it
    /// doesn't list takes its last row, 5. Emerald's trainers' personalities are made as FireRed's are
    /// (<c>CreateNPCTrainerParty</c> is the same in both).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> EmeraldMoneyRates = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["TEAM_AQUA"] = 5, ["AQUA_ADMIN"] = 10, ["AQUA_LEADER"] = 20, ["AROMA_LADY"] = 10, ["RUIN_MANIAC"] = 15,
        ["INTERVIEWER"] = 12, ["TUBER_F"] = 1, ["TUBER_M"] = 1, ["SIS_AND_BRO"] = 3, ["COOLTRAINER"] = 12,
        ["HEX_MANIAC"] = 6, ["LADY"] = 50, ["BEAUTY"] = 20, ["RICH_BOY"] = 50, ["POKEMANIAC"] = 15, ["SWIMMER_M"] = 2,
        ["BLACK_BELT"] = 8, ["GUITARIST"] = 8, ["KINDLER"] = 8, ["CAMPER"] = 4, ["OLD_COUPLE"] = 10, ["BUG_MANIAC"] = 15,
        ["PSYCHIC"] = 6, ["GENTLEMAN"] = 20, ["ELITE_FOUR"] = 25, ["LEADER"] = 25, ["SCHOOL_KID"] = 5, ["SR_AND_JR"] = 4,
        ["POKEFAN"] = 20, ["EXPERT"] = 10, ["YOUNGSTER"] = 4, ["CHAMPION"] = 50, ["FISHERMAN"] = 10, ["TRIATHLETE"] = 10,
        ["DRAGON_TAMER"] = 12, ["BIRD_KEEPER"] = 8, ["NINJA_BOY"] = 3, ["BATTLE_GIRL"] = 6, ["PARASOL_LADY"] = 10,
        ["SWIMMER_F"] = 2, ["PICNICKER"] = 4, ["TWINS"] = 3, ["SAILOR"] = 8, ["COLLECTOR"] = 15, ["RIVAL"] = 15,
        ["PKMN_BREEDER"] = 10, ["PKMN_RANGER"] = 12, ["TEAM_MAGMA"] = 5, ["MAGMA_ADMIN"] = 10, ["MAGMA_LEADER"] = 20,
        ["LASS"] = 4, ["BUG_CATCHER"] = 4, ["HIKER"] = 10, ["YOUNG_COUPLE"] = 8, ["WINSTRATE"] = 10
    };

    /// <summary>
    /// The personalities of a trainer's Pokémon as FireRed's <c>CreateNPCTrainerParty</c> makes them: 0x80 for a
    /// double battle's, 0x78 for a woman's and 0x88 for a man's, plus a hash shifted up a byte, which adds the
    /// trainer's name and then the species' name, in the game's own characters, once for each Pokémon and never
    /// starts again (so the second Pokémon's hash has the name twice). The species' name is the GBA's, in capitals.
    /// </summary>
    public static uint[] Personalities(string trainerName, IReadOnlyList<string> species, bool doubleBattle, bool female)
    {
        var values = new uint[species.Count];
        uint hash = 0;
        for (int i = 0; i < species.Count; i++)
        {
            uint value = doubleBattle ? 0x80u : female ? 0x78u : 0x88u;
            hash += CharacterSum(trainerName);
            hash += CharacterSum(species[i].ToUpperInvariant());
            values[i] = unchecked(value + (hash << 8));
        }
        return values;
    }

    /// <summary>The sum of a name's characters in the GBA's own character set (<c>charmap.txt</c>): A is 0xBB, a 0xD5, 0 0xA1, a space nought.</summary>
    public static uint CharacterSum(string text)
    {
        uint sum = 0;
        foreach (char c in text) sum += GbaCharacter(c);
        return sum;
    }

    private static uint GbaCharacter(char c) => c switch
    {
        >= 'A' and <= 'Z' => 0xBBu + (uint)(c - 'A'),
        >= 'a' and <= 'z' => 0xD5u + (uint)(c - 'a'),
        >= '0' and <= '9' => 0xA1u + (uint)(c - '0'),
        ' ' => 0x00, '!' => 0xAB, '?' => 0xAC, '.' => 0xAD, '-' => 0xAE, '\'' or '’' => 0xB4,
        '♂' => 0xB5, '♀' => 0xB6, ',' => 0xB8, '/' => 0xBA, '&' => 0x2D, 'é' => 0x1B, 'É' => 0x06,
        _ => 0
    };

    /// <summary>"YOUNGSTER" to "Youngster", "PKMN TRAINER 1" to "Pkmn Trainer 1": the data's capitals in ours.</summary>
    public static string Titled(string upper) =>
        string.Join(' ', upper.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
}

/// <summary>
/// The GBA's constants as this game names the same thing (plan 18 · W7): <c>SPECIES_NIDORAN_F</c> is "Nidoran♀",
/// <c>ITEM_POKE_BALL</c> "Poké Ball", <c>ITEM_TM01_FOCUS_PUNCH</c> "TM01", <c>MOVE_DOUBLE_EDGE</c> "Double-Edge", by
/// the data's names with their marks taken off. Null for a constant the data has no name for.
/// </summary>
public static class GbaNames
{
    private static readonly Lazy<Dictionary<string, string>> SpeciesNames = new(() => ByLetters(PokemonDatabase.GetAll().Select(s => s.Name)));
    private static readonly Lazy<Dictionary<string, string>> ItemNames = new(() => ByLetters(ItemDatabase.GetAll().Select(i => i.Name)));
    private static readonly Lazy<Dictionary<string, string>> MoveNames = new(() => ByLetters(MoveDatabase.GetAll().Select(m => m.Name)));

    private static readonly Lazy<Dictionary<(string Kind, string Move), string>> Machines = new(() =>
    {
        var map = new Dictionary<(string, string), string>();
        foreach (var item in ItemDatabase.GetAll())
            if (item.TeachesMove is { } move && item.Name.Length > 2 && item.Name[..2] is "TM" or "HM")
                map.TryAdd((item.Name[..2], Letters(move)), item.Name);
        return map;
    });

    public static string? Species(string constant) => Look(SpeciesNames.Value, constant, "SPECIES_");
    public static string? Move(string constant) =>
        Look(MoveNames.Value, constant, "MOVE_") ?? (Renamed.TryGetValue(constant, out var later) ? Look(MoveNames.Value, later, "MOVE_") : null);

    /// <summary>The moves the third generation spelt otherwise than the data, which takes the later spelling.</summary>
    private static readonly Dictionary<string, string> Renamed = new(StringComparer.Ordinal)
    {
        ["MOVE_FAINT_ATTACK"] = "MOVE_FEINT_ATTACK", ["MOVE_VICE_GRIP"] = "MOVE_VISE_GRIP", ["MOVE_HI_JUMP_KICK"] = "MOVE_HIGH_JUMP_KICK",
        ["MOVE_SMELLING_SALT"] = "MOVE_SMELLING_SALTS"
    };

    public static string? Item(string constant)
    {
        // Emerald names its machines by their moves too (ITEM_TM01_FOCUS_PUNCH); the data calls them TM01
        var machine = Regex.Match(constant, @"^ITEM_(TM|HM)(\d\d)");
        if (machine.Success) return ItemNames.Value.GetValueOrDefault(machine.Groups[1].Value + machine.Groups[2].Value);
        // and at the pinned commit by their moves alone (ITEM_TM_SHADOW_BALL): the machine of the data that teaches it,
        // which is the same number, since the fourth generation kept the third's first fifty
        var byMove = Regex.Match(constant, @"^ITEM_(TM|HM)_(\w+)$");
        if (byMove.Success && Machines.Value.TryGetValue((byMove.Groups[1].Value, Letters(byMove.Groups[2].Value)), out var named)) return named;
        // The two the third generation named otherwise than the fourth
        if (constant == "ITEM_X_DEFEND") return ItemNames.Value.GetValueOrDefault(Letters("X Defense"));
        if (constant == "ITEM_X_SPECIAL") return ItemNames.Value.GetValueOrDefault(Letters("X Sp. Atk"));
        return Look(ItemNames.Value, constant, "ITEM_");
    }

    private static string? Look(Dictionary<string, string> names, string constant, string prefix) =>
        constant.StartsWith(prefix, StringComparison.Ordinal) ? names.GetValueOrDefault(Letters(constant[prefix.Length..])) : null;

    /// <summary>A name's letters and digits alone, in capitals, the gender signs as F and M: "Nidoran♀" and <c>NIDORAN_F</c> are both <c>NIDORANF</c>.</summary>
    public static string Letters(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            if (c == '♀') sb.Append('F');
            else if (c == '♂') sb.Append('M');
            else if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ByLetters(IEnumerable<string> names)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in names) map.TryAdd(Letters(name), name);
        return map;
    }
}
