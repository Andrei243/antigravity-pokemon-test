using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

/// <summary>
/// FireRed's Trainer Tower on Seven Island (plan 37 · FC6; <c>dotnet run --project tools/DataImporter -- --trainer-tower</c>):
/// the four challenges of <c>src/trainer_tower_sets.c</c> (each floor's kind, prize and trainers with their facility class
/// and six Pokémon: species, item, moves, effort, IVs, ability and personality) and the tables of <c>src/trainer_tower.c</c>
/// (which of the six each floor brings, the prize list), written into <c>trainer-tower.json</c>. Numbers and constants only:
/// no trainer's name, no easy-chat word, no nickname. A trainer's name is one of our own (<see cref="TowerNames"/>). The
/// Trainer Hill's helpers read the Pokémon (its class names, items, moves and species).
/// </summary>
public static partial class TrainerHillImport
{
    /// <summary>The files read of pret/pokefirered at its pinned commit.</summary>
    public static readonly string[] TowerFiles = { "src/trainer_tower_sets.c", "src/trainer_tower.c" };

    private static readonly string[] TowerChallenges = { "SINGLE", "DOUBLE", "KNOCKOUT", "MIXED" };

    /// <summary>Names of our own for the tower's trainers, in the order the floors are first met.</summary>
    private static readonly string[] TowerNames =
    {
        "Aldous", "Brisa", "Cormac", "Delphine", "Emrys", "Fable", "Gideon", "Hesper", "Ivo", "Jessamy",
        "Keir", "Linnea", "Mabon", "Nerys", "Orrin", "Perpetua", "Quentin", "Rhosyn", "Silas", "Tamar",
        "Ulric", "Vanya", "Wystan", "Xanthe", "Yorick", "Zinnia", "Arlo", "Bryony", "Cedric", "Dorcas",
        "Elowen", "Ferris", "Gwyneth", "Hamish", "Iona", "Jasper", "Kerensa", "Leofric", "Morwenna", "Niall",
        "Ottoline", "Pascoe", "Rowena", "Sorrel", "Tegan", "Ulla", "Vaughn", "Wren", "Yestin", "Zillah",
        "Ansel", "Briar", "Corin", "Demelza", "Evander", "Fenella", "Garnet", "Hywel", "Isolde", "Jory",
        "Kit", "Lowenna", "Merrick", "Nessa", "Oakley", "Primrose", "Ronan", "Senna", "Talfryn", "Una"
    };

    public static TrainerTowerFile BuildTower(string repo, string cache)
    {
        string dir = Sources.Download(cache, "pokefirered-trainer-tower", Sources.FireRedRaw, Sources.FireRedCommit, TowerFiles);
        string sets = File.ReadAllText(Path.Combine(dir, "src/trainer_tower_sets.c"));
        string source = File.ReadAllText(Path.Combine(dir, "src/trainer_tower.c"));

        string dataDir = Path.Combine(repo, "PokemonPlatinumEngine", "Data");
        var moveNames = RowNames(Path.Combine(dataDir, "moves.json"));
        var speciesNames = RowNames(Path.Combine(dataDir, "species.json"));
        var itemNames = RowNames(Path.Combine(dataDir, "items.json"));
        // FireRed's names this game's data spells otherwise (the two Nidoran are one key once their signs are dropped)
        speciesNames["nidoranf"] = "Nidoran♀";
        speciesNames["nidoranm"] = "Nidoran♂";
        itemNames.TryAdd("stick", "Leek");
        var problems = new List<string>();

        string Item(string constant, string where)
        {
            if (constant == "NONE") return "";
            if (itemNames.TryGetValue(Normal(constant), out var name)) return name;
            problems.Add($"{where}: ITEM_{constant} is no item of items.json");
            return "";
        }

        // ---- every floor by its name: its kind, prize and trainers
        var floors = new Dictionary<string, TrainerTowerFloorRecord>();
        var nameAt = new Dictionary<string, int>();
        foreach (Match m in Regex.Matches(sets, @"static const struct TrainerTowerFloor (sTrainerTowerFloor_\w+) = \{"))
        {
            int end = sets.IndexOf("\nstatic const", m.Index + 10, StringComparison.Ordinal);
            if (end < 0) end = sets.IndexOf("\nconst struct", m.Index + 10, StringComparison.Ordinal);
            string body = sets[m.Index..end];
            string where = m.Groups[1].Value;
            string kind = Regex.Match(body, @"\.challengeType\s*=\s*CHALLENGE_TYPE_(\w+)").Groups[1].Value;
            var floor = new TrainerTowerFloorRecord
            {
                Kind = Array.IndexOf(TowerChallenges, kind),
                Prize = Item(Regex.Match(body, @"\.prize\s*=\s*TTPRIZE_(\w+)").Groups[1].Value, where)
            };
            if (floor.Kind is < 0 or > 2) problems.Add($"{where}: challenge type {kind}");
            var classes = Regex.Matches(body, @"\.facilityClass\s*=\s*FACILITY_CLASS_(\w+)").ToList();
            for (int t = 0; t < classes.Count; t++)
            {
                string chunk = body[classes[t].Index..(t + 1 < classes.Count ? classes[t + 1].Index : body.Length)];
                string constant = classes[t].Groups[1].Value;
                var trainer = new TrainerHillTrainerRecord { Class = ClassName(constant), Look = constant.ToLowerInvariant() };
                int monsAt = chunk.IndexOf(".mons", StringComparison.Ordinal);
                if (monsAt < 0) { problems.Add($"{where} trainer {t}: no .mons"); continue; }
                chunk = chunk[monsAt..];
                var tokens = Regex.Matches(chunk, @"DUMMY_TOWER_MON\(\d+\)|\.species\s*=").ToList();
                for (int e = 0; e < tokens.Count && e < 6; e++)
                {
                    if (tokens[e].Value.StartsWith("DUMMY", StringComparison.Ordinal)) { trainer.Team.Add(new TrainerHillMon()); continue; }
                    string mon = chunk[tokens[e].Index..(e + 1 < tokens.Count ? tokens[e + 1].Index : chunk.Length)];
                    string at = $"{where} trainer {t} mon {e}";
                    string speciesKey = Normal(Regex.Match(mon, @"\.species\s*=\s*SPECIES_(\w+)").Groups[1].Value);
                    if (!speciesNames.TryGetValue(speciesKey, out var species)) { problems.Add($"{at}: SPECIES_{speciesKey}"); trainer.Team.Add(new TrainerHillMon()); continue; }
                    var record = new TrainerHillMon
                    {
                        Species = species,
                        Item = NullIfEmpty(Item(Regex.Match(mon, @"\.heldItem\s*=\s*ITEM_(\w+)").Groups[1].Value, at)),
                        AbilityNum = int.Parse(Field(mon, @"\.abilityNum\s*=\s*(\d+)", "0")),
                        Personality = ParseNumber(Field(mon, @"\.personality\s*=\s*(0x[0-9A-Fa-f]+|\d+)", "0"))
                    };
                    foreach (string move in Regex.Match(mon, @"\.moves\s*=\s*\{([^}]*)\}").Groups[1].Value
                                 .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (move == "MOVE_NONE" || !move.StartsWith("MOVE_")) continue;
                        string key = Normal(move["MOVE_".Length..]);
                        key = MoveAliases.GetValueOrDefault(key, key);
                        if (moveNames.TryGetValue(key, out var name)) record.Moves.Add(name);
                        else problems.Add($"{at}: {move} is no move of moves.json");
                    }
                    foreach (string stat in new[] { "hp", "attack", "defense", "speed", "spAttack", "spDefense" })
                    {
                        record.Evs.Add(int.Parse(Field(mon, $@"\.{stat}EV\s*=\s*(\d+)", "0")));
                        record.Ivs.Add(int.Parse(Field(mon, $@"\.{stat}IV\s*=\s*(\d+)", "0")));
                    }
                    trainer.Team.Add(record);
                }
                if (trainer.Team.Count != 6) problems.Add($"{where} trainer {t}: {trainer.Team.Count} Pokémon, not 6");
                floor.Trainers.Add(trainer);
            }
            int wanted = floor.Kind + 1;
            if (floor.Trainers.Count < wanted) problems.Add($"{where}: {floor.Trainers.Count} trainers for a floor of kind {kind}");
            floor.Trainers = floor.Trainers.Take(wanted).ToList();
            floors[where] = floor;
        }

        // ---- the four challenges, their floors in FireRed's order, and names of ours in the order the floors are met
        var file = new TrainerTowerFile();
        int table = sets.IndexOf("gTrainerTowerFloors[", StringComparison.Ordinal);
        if (table < 0) throw new InvalidDataException("no gTrainerTowerFloors");
        int nameIndex = 0;
        var named = new Dictionary<TrainerHillTrainerRecord, string>(ReferenceEqualityComparer.Instance);
        foreach (string challenge in TowerChallenges)
        {
            var section = Regex.Match(sets[table..], $@"\[CHALLENGE_TYPE_{challenge}\]\s*=\s*\{{([^}}]*)\}}");
            if (!section.Success) { problems.Add($"no floors of {challenge}"); continue; }
            var record = new TrainerTowerChallengeRecord { Name = char.ToUpperInvariant(challenge[0]) + challenge[1..].ToLowerInvariant() };
            foreach (Match f in Regex.Matches(section.Groups[1].Value, @"&(sTrainerTowerFloor_\w+)"))
            {
                if (!floors.TryGetValue(f.Groups[1].Value, out var floor)) { problems.Add($"{challenge}: no floor {f.Groups[1].Value}"); continue; }
                foreach (var trainer in floor.Trainers)
                    if (!named.ContainsKey(trainer)) trainer.Name = named[trainer] = TowerNames[nameIndex++ % TowerNames.Length];
                record.Floors.Add(floor);
            }
            if (record.Floors.Count != 8) problems.Add($"{challenge}: {record.Floors.Count} floors, not 8");
            file.Challenges.Add(record);
        }

        // ---- which of the six each floor brings, by the floors cleared
        List<List<int>> Slots(string name, int width)
        {
            var m = Regex.Match(source, $@"{name}\[MAX_TRAINER_TOWER_FLOORS\]\[{width}\]\s*=\s*\{{(.*?)\}};", RegexOptions.Singleline);
            if (!m.Success) { problems.Add($"no {name}"); return new(); }
            var rows = Regex.Matches(m.Groups[1].Value, @"\{([^}]*)\}").Select(r => r.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToList()).ToList();
            if (rows.Count != 8 || rows.Any(r => r.Count != width)) problems.Add($"{name} wasn't read whole");
            return rows;
        }
        file.SingleSlots = Slots("sSingleBattleChallengeMonIdxs", 2);
        file.DoubleSlots = Slots("sDoubleBattleChallengeMonIdxs", 2);
        file.KnockoutSlots = Slots("sKnockoutChallengeMonIdxs", 3);

        // Every Pokémon a floor brings is one of FireRed's (no dummy is ever picked)
        foreach (var challenge in file.Challenges)
            for (int f = 0; f < challenge.Floors.Count; f++)
            {
                var floor = challenge.Floors[f];
                // On floor f the floors cleared are f - 1 (BuildEnemyParty reads TRAINER_TOWER.floorsCleared)
                foreach (int cleared in new[] { f })
                {
                    var picked = floor.Kind switch
                    {
                        0 => file.SingleSlots[cleared].Select(i => floor.Trainers[0].Team[i]),
                        1 => file.DoubleSlots[cleared].Select((i, t) => floor.Trainers[t].Team[i]),
                        _ => file.KnockoutSlots[cleared].Select((i, t) => floor.Trainers[t].Team[i])
                    };
                    if (picked.Any(p => p.Species.Length == 0)) problems.Add($"{challenge.Name} floor {f + 1}: a dummy is picked after {cleared} floors");
                }
            }

        Console.WriteLine($"  {file.Challenges.Count} challenges, {file.Challenges.Sum(c => c.Floors.Count)} floors, {named.Count} trainers");
        foreach (var p in problems) Console.WriteLine($"  !! {p}");
        if (problems.Count > 0) throw new InvalidDataException($"{problems.Count} problems in FireRed's Trainer Tower");
        return file;
    }
}
