using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Emerald's contest tables (plan 21 · H11; <c>dotnet run --project tools/DataImporter -- --hoenn-contest</c>):
/// <c>gContestEffects</c> (48 effects, each with its appeal and jam), <c>gContestMoves</c> (each of the first three
/// generations' moves with its effect, quality and combos) and <c>gContestOpponents</c> (the 96 contestants) read from
/// the decompilation's headers into <c>hoenn-contest.json</c>. Names and numbers only: no nickname, no line of what
/// anyone says. A contestant's nickname is its species' name in this game.
/// </summary>
public static partial class HoennContestImport
{
    // The moves Emerald spells otherwise than moves.json does (a later game's rename or PokeAPI's spelling)
    private static readonly Dictionary<string, string> MoveAliases = new()
    {
        ["vicegrip"] = "visegrip", ["smellingsalt"] = "smellingsalts", ["softboiled"] = "softboiled", ["faintattack"] = "feintattack",
        ["hijumpkick"] = "highjumpkick", ["solarbeam"] = "solarbeam", ["sonicboom"] = "sonicboom", ["selfdestruct"] = "selfdestruct"
    };

    public static HoennContestFile Build(string repo, string cache)
    {
        string dir = Sources.Download(cache, "pokeemerald-contest", Sources.EmeraldRaw, Sources.EmeraldCommit, Sources.EmeraldContestFiles);
        string moves = File.ReadAllText(Path.Combine(dir, "src/data/contest_moves.h"));
        string opponents = File.ReadAllText(Path.Combine(dir, "src/data/contest_opponents.h"));
        string constants = File.ReadAllText(Path.Combine(dir, "include/constants/contest.h"));

        string dataDir = Path.Combine(repo, "PokemonPlatinumEngine", "Data");
        var moveNames = RowNames(Path.Combine(dataDir, "moves.json"));
        var speciesNames = RowNames(Path.Combine(dataDir, "species.json"));
        var problems = new List<string>();

        var file = new HoennContestFile();

        // ---- the constants: effect ids, combo starter ids
        var effectIds = new Dictionary<string, int>();
        foreach (Match m in EffectDefine().Matches(constants)) effectIds[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        var starterIds = new Dictionary<string, int>();
        foreach (Match m in StarterDefine().Matches(constants)) starterIds[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        file.ComboStarters = starterIds.OrderBy(p => p.Value).Select(p => Names2.Title(p.Key)).ToList();

        // ---- the effects
        var effects = new SortedDictionary<int, ContestEffectRow>();
        foreach (Match m in EffectRow().Matches(moves))
        {
            string name = m.Groups[1].Value;
            int id = effectIds[name];
            effects[id] = new ContestEffectRow
            {
                Id = id,
                Name = Names2.Pascal(name),
                Type = Enum.Parse<ContestEffectType>(Names2.Pascal(m.Groups[2].Value)),
                Appeal = int.Parse(m.Groups[3].Value),
                Jam = int.Parse(m.Groups[4].Value)
            };
        }
        if (effects.Count != 48) problems.Add($"{effects.Count} contest effects, not 48");
        file.Effects = effects.Values.ToList();

        // ---- the moves
        foreach (Match m in MoveRow().Matches(moves))
        {
            string key = Normal(m.Groups[1].Value);
            if (MoveAliases.TryGetValue(key, out var alias)) key = alias;
            if (!moveNames.TryGetValue(key, out var name))
            {
                problems.Add($"MOVE_{m.Groups[1].Value} is no move of moves.json");
                continue;
            }
            var combos = m.Groups[5].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => c != "0").Select(c => starterIds[c["COMBO_STARTER_".Length..]]).ToList();
            string starter = m.Groups[4].Value;
            file.Moves[name] = new HoennContestMove
            {
                Effect = effectIds[m.Groups[2].Value],
                Category = Enum.Parse<ContestStat>(Names2.Pascal(m.Groups[3].Value)),
                ComboStarter = starter == "0" ? 0 : starterIds[starter["COMBO_STARTER_".Length..]],
                Combos = combos
            };
        }

        // ---- the contestants and when they turn up
        int at = opponents.IndexOf("gContestOpponents[]", StringComparison.Ordinal);
        int filterAt = opponents.IndexOf("gPostgameContestOpponentFilter", StringComparison.Ordinal);
        var filters = new Dictionary<string, OpponentTime>();
        foreach (Match m in FilterRow().Matches(opponents[filterAt..]))
            filters[m.Groups[1].Value] = m.Groups[2].Value switch
            {
                "NONE" => OpponentTime.Either,
                "ONLY_POSTGAME" => OpponentTime.OnlyAfterTheGame,
                _ => OpponentTime.OnlyBeforeTheGame
            };
        foreach (var chunk in opponents[at..filterAt].Split("[CONTEST_OPPONENT_", StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            string id = chunk[..chunk.IndexOf(']')];
            string Field(string pattern)
            {
                var m = Regex.Match(chunk, pattern);
                if (!m.Success) throw new InvalidDataException($"{id}: no {pattern}");
                return m.Groups[1].Value;
            }
            string speciesKey = Normal(Field(@"\.species = SPECIES_(\w+)"));
            if (!speciesNames.TryGetValue(speciesKey, out var species))
            {
                problems.Add($"SPECIES_{speciesKey} of {id} is no species of species.json");
                continue;
            }
            var pools = new List<ContestStat>();
            foreach (var quality in Enum.GetValues<ContestStat>())
                if (Field($@"\.aiPool_{quality} = (TRUE|FALSE)") == "TRUE") pools.Add(quality);
            var list = Regex.Match(chunk, @"\.moves\s*=\s*\{([^}]*)\}").Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w != "MOVE_NONE").Select(w => Normal(w["MOVE_".Length..])).ToList();
            var moveList = new List<string>();
            foreach (var move in list)
            {
                string key = MoveAliases.GetValueOrDefault(move, move);
                if (moveNames.TryGetValue(key, out var n)) moveList.Add(n);
                else problems.Add($"{id}'s MOVE_{move} is no move of moves.json");
            }
            file.Opponents.Add(new HoennContestOpponent
            {
                Trainer = Names2.Title(Field(@"\.trainerName = _\(""([^""]+)""\)").Replace(' ', '_')),
                Species = species,
                Rank = Field(@"\.whichRank = CONTEST_RANK_(\w+)") switch { "NORMAL" => 0, "SUPER" => 1, "HYPER" => 2, _ => 3 },
                Pools = pools,
                Moves = moveList,
                Cool = int.Parse(Field(@"\.cool = (\d+)")),
                Beauty = int.Parse(Field(@"\.beauty = (\d+)")),
                Cute = int.Parse(Field(@"\.cute = (\d+)")),
                Smart = int.Parse(Field(@"\.smart = (\d+)")),
                Tough = int.Parse(Field(@"\.tough = (\d+)")),
                Sheen = int.Parse(Field(@"\.sheen = (\d+)")),
                Time = filters.GetValueOrDefault(id, OpponentTime.Either)
            });
        }
        if (file.Opponents.Count != 96) problems.Add($"{file.Opponents.Count} contestants, not 96");
        Console.WriteLine($"  {file.Effects.Count} effects, {file.Moves.Count} moves, {file.Opponents.Count} contestants, {file.ComboStarters.Count} combo starters");
        foreach (var p in problems) Console.WriteLine($"  !! {p}");
        if (problems.Count > 0) throw new InvalidDataException($"{problems.Count} problems in Emerald's contest tables");
        return file;
    }

    private static string Normal(string name) => new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    /// <summary>The names of a data file's rows by their letters and digits alone.</summary>
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

    /// <summary>A constant's body as the game spells a name: <c>HIGHLY_APPEALING</c> to <c>HighlyAppealing</c>.</summary>
    private static class Names2
    {
        public static string Pascal(string constant) => string.Concat(constant.Split('_', ' ').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        public static string Title(string constant) => string.Join(' ', constant.Split('_', ' ').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    [GeneratedRegex(@"#define CONTEST_EFFECT_(?!TYPE_)(\w+)\s+(\d+)")]
    private static partial Regex EffectDefine();

    [GeneratedRegex(@"#define COMBO_STARTER_(\w+)\s+(\d+)")]
    private static partial Regex StarterDefine();

    [GeneratedRegex(@"\[CONTEST_EFFECT_(\w+)\]\s*=\s*\{\s*\.effectType\s*=\s*CONTEST_EFFECT_TYPE_(\w+),\s*\.appeal\s*=\s*(-?\d+),\s*\.jam\s*=\s*(-?\d+)")]
    private static partial Regex EffectRow();

    [GeneratedRegex(@"\[MOVE_(\w+)\]\s*=\s*\{\s*\.effect\s*=\s*CONTEST_EFFECT_(\w+),\s*\.contestCategory\s*=\s*CONTEST_CATEGORY_(\w+),\s*\.comboStarterId\s*=\s*(\w+),\s*(?:\.comboMoves\s*=\s*)?\{([^}]*)\}")]
    private static partial Regex MoveRow();

    [GeneratedRegex(@"\[CONTEST_OPPONENT_(\w+)\]\s*=\s*CONTEST_FILTER_(\w+)")]
    private static partial Regex FilterRow();
}
