using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

/// <summary>
/// Emerald's Trainer Hill (plan 21, part l; <c>dotnet run --project tools/DataImporter -- --trainer-hill</c>): the four
/// challenges of <c>src/data/battle_frontier/trainer_hill.h</c> (each floor's two trainers with their classes and the
/// numbers of their three Pokémon, where they stand, and the floor's collision from the binary rows beside it) and the
/// prize lists of <c>src/trainer_hill.c</c>, written into <c>trainer-hill.json</c>. Numbers and constants only: no
/// trainer's name, no word they say, no metatile. A trainer's name is one of our own (<see cref="OurNames"/>).
/// </summary>
public static partial class TrainerHillImport
{
    private static readonly string[] Modes = { "Normal", "Variety", "Unique", "Expert" };

    /// <summary>The files read: the header, the prizes' source and the sixteen floors' collision rows.</summary>
    public static string[] Files => new[] { "src/data/battle_frontier/trainer_hill.h", "src/trainer_hill.c", TilesetAttributes }
        .Concat(Modes.SelectMany(m => Enumerable.Range(0, 4).SelectMany(f => new[]
        {
            $"graphics/trainer_hill/maps_{m.ToLowerInvariant()}/floor_{f}/collision.bin",
            $"graphics/trainer_hill/maps_{m.ToLowerInvariant()}/floor_{f}/metatiles.bin"
        })))
        .ToArray();

    // The Hill's own tileset's attributes: only each metatile's behaviour is read, for the ledges hopped down (MB_JUMP_*)
    private const string TilesetAttributes = "data/tilesets/secondary/trainer_hill/metatile_attributes.bin";

    /// <summary>Names of our own for the Hill's thirty-two trainers, in the order of the challenges' floors.</summary>
    private static readonly string[] OurNames =
    {
        "Morwen", "Tobin", "Dariel", "Ysolde", "Fenwick", "Opaline", "Corvin", "Liesl",
        "Barnaby", "Clementine", "Rosamund", "Quill", "Wendeline", "Ansel", "Hollis", "Peregrin",
        "Isaura", "Kestrel", "Marlo", "Tamsin", "Oswin", "Juniper", "Ellery", "Saffi",
        "Thaddeus", "Brielle", "Caspian", "Odette", "Lorcan", "Verity", "Ambrose", "Philippa"
    };

    // The moves and species Emerald spells otherwise than this game's data does
    private static readonly Dictionary<string, string> MoveAliases = new()
    {
        ["vicegrip"] = "visegrip", ["smellingsalt"] = "smellingsalts", ["faintattack"] = "feintattack",
        ["hijumpkick"] = "highjumpkick"
    };

    public static TrainerHillFile Build(string repo, string cache)
    {
        string dir = Sources.Download(cache, "pokeemerald-trainer-hill", Sources.EmeraldRaw, Sources.EmeraldCommit, Files);
        string header = File.ReadAllText(Path.Combine(dir, "src/data/battle_frontier/trainer_hill.h"));
        string source = File.ReadAllText(Path.Combine(dir, "src/trainer_hill.c"));
        byte[] attributeBytes = File.ReadAllBytes(Path.Combine(dir, TilesetAttributes));
        var attributes = Enumerable.Range(0, attributeBytes.Length / 2).Select(i => attributeBytes[2 * i] | attributeBytes[2 * i + 1] << 8).ToArray();

        string dataDir = Path.Combine(repo, "PokemonPlatinumEngine", "Data");
        var moveNames = RowNames(Path.Combine(dataDir, "moves.json"));
        var speciesNames = RowNames(Path.Combine(dataDir, "species.json"));
        var itemNames = RowNames(Path.Combine(dataDir, "items.json"));
        var machines = Machines(Path.Combine(dataDir, "items.json"));
        var problems = new List<string>();

        string Item(string constant, string where)
        {
            if (constant == "NONE") return "";
            string key = Normal(constant);
            if (constant.StartsWith("TM_"))
            {
                if (machines.TryGetValue(Normal(constant[3..]), out var tm)) return tm;
            }
            else if (itemNames.TryGetValue(key, out var name)) return name;
            problems.Add($"{where}: ITEM_{constant} is no item of items.json");
            return "";
        }

        var file = new TrainerHillFile();
        int nameIndex = 0;
        foreach (string mode in Modes)
        {
            int start = header.IndexOf($"sFloors_{mode}[]", StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException($"no sFloors_{mode}");
            int end = header.IndexOf("static const", start + 10, StringComparison.Ordinal);
            string section = end < 0 ? header[start..] : header[start..end];
            var record = new TrainerHillModeRecord { Name = mode };
            var floorStarts = Regex.Matches(section, @"\.trainerNum1\s*=\s*(\d+)").Select(m => m.Index).ToList();
            if (floorStarts.Count != 4) problems.Add($"{mode}: {floorStarts.Count} floors, not 4");
            for (int f = 0; f < floorStarts.Count; f++)
            {
                string floor = section[floorStarts[f]..(f + 1 < floorStarts.Count ? floorStarts[f + 1] : section.Length)];
                int num1 = int.Parse(Regex.Match(floor, @"\.trainerNum1\s*=\s*(\d+)").Groups[1].Value);
                int num2 = int.Parse(Regex.Match(floor, @"\.trainerNum2\s*=\s*(\d+)").Groups[1].Value);
                var classes = Regex.Matches(floor, @"\.facilityClass\s*=\s*FACILITY_CLASS_(\w+)").ToList();
                int mapAt = floor.IndexOf(".map", StringComparison.Ordinal);
                if (classes.Count != 2 || mapAt < 0) { problems.Add($"{mode} floor {f}: not two trainers and a map"); continue; }
                var floorRecord = new TrainerHillFloorRecord();
                for (int t = 0; t < 2; t++)
                {
                    string chunk = floor[classes[t].Index..(t == 0 ? classes[1].Index : mapAt)];
                    string constant = classes[t].Groups[1].Value;
                    var trainer = new TrainerHillTrainerRecord
                    {
                        Number = t == 0 ? num1 : num2,
                        Class = ClassName(constant),
                        Look = constant.ToLowerInvariant(),
                        Name = OurNames[nameIndex++ % OurNames.Length]
                    };
                    // The six places of the trainer's mons; the trainer brings three of them (sTrainerPartySlots)
                    int monsAt = chunk.IndexOf(".mons", StringComparison.Ordinal);
                    if (monsAt < 0) { problems.Add($"{mode} floor {f} trainer {t}: no .mons"); continue; }
                    chunk = chunk[monsAt..];
                    var entries = Regex.Matches(chunk, @"\[(\d)\]\s*=\s*(DUMMY_HILL_MON|\{)").Take(6).ToList();
                    for (int e = 0; e < entries.Count; e++)
                    {
                        int slot = int.Parse(entries[e].Groups[1].Value);
                        if (slot / 3 != t || entries[e].Groups[2].Value != "{") continue;
                        string body = chunk[entries[e].Index..(e + 1 < entries.Count ? entries[e + 1].Index : chunk.Length)];
                        string where = $"{mode} floor {f} trainer {t} slot {slot}";
                        string speciesKey = Normal(Regex.Match(body, @"\.species\s*=\s*SPECIES_(\w+)").Groups[1].Value);
                        if (!speciesNames.TryGetValue(speciesKey, out var species)) { problems.Add($"{where}: SPECIES_{speciesKey}"); continue; }
                        var mon = new TrainerHillMon
                        {
                            Species = species,
                            Item = NullIfEmpty(Item(Regex.Match(body, @"\.heldItem\s*=\s*ITEM_(\w+)").Groups[1].Value, where)),
                            AbilityNum = int.Parse(Field(body, @"\.abilityNum\s*=\s*(\d+)", "0")),
                            Personality = ParseNumber(Field(body, @"\.personality\s*=\s*(0x[0-9A-Fa-f]+|\d+)", "0"))
                        };
                        foreach (string move in Regex.Match(body, @"\.moves\s*=\s*\{([^}]*)\}").Groups[1].Value
                                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (move == "MOVE_NONE" || !move.StartsWith("MOVE_")) continue;
                            string key = Normal(move["MOVE_".Length..]);
                            key = MoveAliases.GetValueOrDefault(key, key);
                            if (moveNames.TryGetValue(key, out var name)) mon.Moves.Add(name);
                            else problems.Add($"{where}: {move} is no move of moves.json");
                        }
                        foreach (string stat in new[] { "hp", "attack", "defense", "speed", "spAttack", "spDefense" })
                        {
                            mon.Evs.Add(int.Parse(Field(body, $@"\.{stat}EV\s*=\s*(\d+)", "0")));
                            mon.Ivs.Add(int.Parse(Field(body, $@"\.{stat}IV\s*=\s*(\d+)", "0")));
                        }
                        trainer.Team.Add(mon);
                    }
                    if (trainer.Team.Count != 3) problems.Add($"{mode} floor {f} trainer {t}: {trainer.Team.Count} Pokémon, not 3");
                    floorRecord.Trainers.Add(trainer);
                }

                // The floor's map: where the two stand, the ways they face, how far they see, and the collision rows
                string map = floor[mapAt..];
                var coords = Regex.Match(map, @"COORDS_XY\((\d+),\s*(\d+)\)\s*,\s*COORDS_XY\((\d+),\s*(\d+)\)");
                var dirs = Regex.Match(map, @"TRAINER_DIRS\(DIR_(\w+),\s*DIR_(\w+)\)");
                var ranges = Regex.Match(map, @"TRAINER_RANGE\((\d+),\s*(\d+)\)");
                var collision = Regex.Match(map, @"INCBIN_U16\(""(graphics/trainer_hill/[^""]+/collision\.bin)""\)");
                if (!coords.Success || !dirs.Success || !ranges.Success || !collision.Success) { problems.Add($"{mode} floor {f}: its map wasn't read"); continue; }
                for (int t = 0; t < 2; t++)
                    floorRecord.Spots.Add(new TrainerHillSpot
                    {
                        X = int.Parse(coords.Groups[1 + 2 * t].Value),
                        Y = int.Parse(coords.Groups[2 + 2 * t].Value),
                        Facing = Facing(dirs.Groups[1 + t].Value),
                        Range = int.Parse(ranges.Groups[1 + t].Value)
                    });
                byte[] bytes = File.ReadAllBytes(Path.Combine(dir, collision.Groups[1].Value));
                byte[] metatiles = File.ReadAllBytes(Path.Combine(dir, collision.Groups[1].Value.Replace("collision.bin", "metatiles.bin")));
                for (int y = 0; y < 16; y++)
                {
                    int row = bytes[2 * y] | bytes[2 * y + 1] << 8;
                    floorRecord.Rows.Add(new string(Enumerable.Range(0, 16).Select(x =>
                    {
                        // A ledge is hopped over whatever its collision says (GetLedgeJumpDirection reads the behaviour first)
                        int behaviour = attributes[metatiles[16 * y + x]] & 0xFF;
                        if (behaviour is >= 0x38 and <= 0x3B)
                        {
                            if (behaviour == 0x3A) problems.Add($"{mode} floor {f}: a ledge hopped north at {x}, {y}, which this game has no tile for");
                            return behaviour switch { 0x38 => '>', 0x39 => '<', _ => 'v' };
                        }
                        return (row >> (15 - x) & 1) == 1 ? '#' : '.';
                    }).ToArray()));
                }
                record.Floors.Add(floorRecord);
            }
            file.Modes.Add(record);
        }

        // ---- the prizes: each list by name, then the two sets of ten
        var lists = new Dictionary<string, List<string>>();
        foreach (Match m in Regex.Matches(source, @"sPrizeList(\w+)\[\]\s*=\s*\{([^}]*)\}"))
            if (!m.Groups[1].Value.StartsWith("Sets")) lists[m.Groups[1].Value] = m.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => Item(c["ITEM_".Length..], $"sPrizeList{m.Groups[1].Value}")).ToList();
        foreach (Match m in Regex.Matches(source, @"sPrizeLists[12]\[NUM_TRAINER_HILL_PRIZE_LISTS\]\s*=\s*\{([^}]*)\}"))
            file.PrizeSets.Add(m.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(n => lists[n["sPrizeList".Length..]]).ToList());
        if (file.PrizeSets.Count != 2 || file.PrizeSets.Any(s => s.Count != 10 || s.Any(l => l.Count != 6))) problems.Add("the prize lists weren't read whole");

        Console.WriteLine($"  {file.Modes.Count} challenges, {file.Modes.Sum(m => m.Floors.Count)} floors, {file.Modes.Sum(m => m.Floors.Sum(f => f.Trainers.Count))} trainers, {file.PrizeSets.Count} prize sets");
        foreach (var p in problems) Console.WriteLine($"  !! {p}");
        if (problems.Count > 0) throw new InvalidDataException($"{problems.Count} problems in Emerald's Trainer Hill");
        return file;
    }

    private static string? NullIfEmpty(string s) => s.Length == 0 ? null : s;

    private static string Field(string body, string pattern, string otherwise)
    {
        var m = Regex.Match(body, pattern);
        return m.Success ? m.Groups[1].Value : otherwise;
    }

    private static uint ParseNumber(string s) => s.StartsWith("0x") ? Convert.ToUInt32(s[2..], 16) : uint.Parse(s);

    private static Direction Facing(string dir) => dir switch
    {
        "NORTH" => Direction.Up,
        "SOUTH" => Direction.Down,
        "WEST" => Direction.Left,
        _ => Direction.Right
    };

    /// <summary>A facility class as this game names the class: <c>PKMN_BREEDER_F</c> to "Pokémon Breeder", <c>COOLTRAINER_M</c> to "Ace Trainer".</summary>
    private static string ClassName(string constant)
    {
        string body = Regex.Replace(constant, "_(M|F)$", "");
        return body switch
        {
            "COOLTRAINER" => "Ace Trainer",
            "PKMN_BREEDER" => "Pokémon Breeder",
            "PKMN_RANGER" => "Pokémon Ranger",
            "POKEFAN" => "Poké Fan",
            _ => string.Join(' ', body.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()))
        };
    }

    private static string Normal(string name) => new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static Dictionary<string, string> RowNames(string path)
    {
        var result = new Dictionary<string, string>();
        foreach (var row in JsonNode.Parse(File.ReadAllText(path))!.AsArray())
        {
            string name = row!["name"]!.GetValue<string>();
            result.TryAdd(Normal(name), name);
        }
        return result;
    }

    /// <summary>Each machine's name by the move it teaches (a TM's own number is the same in Emerald and Platinum for every prize).</summary>
    private static Dictionary<string, string> Machines(string path)
    {
        var result = new Dictionary<string, string>();
        foreach (var row in JsonNode.Parse(File.ReadAllText(path))!.AsArray())
        {
            string name = row!["name"]!.GetValue<string>();
            if (name.StartsWith("TM") && row["teachesMove"]?.GetValue<string>() is { } move) result.TryAdd(Normal(move), name);
        }
        return result;
    }
}
