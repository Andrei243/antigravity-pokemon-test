using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// The Master Trainers of plan 14 · B14 (Let's Go's: a master for every species, battled one-on-one with that species
/// alone), written to <c>Data/master-trainers.json</c>: for each of the National Pokédex's species a trainer record
/// with the id <c>master_&lt;species&gt;</c> (<see cref="TrainerDatabase.MasterId"/>), the class "&lt;Species&gt; Master",
/// a given name of our own, no prize money, no items, the AI routines Platinum's Elite Four think with, and one
/// Pokémon at level 75 with every IV whole, a personality made from the national number and no held item.
/// <para>
/// Its four moves are read from Pokémon Showdown's random-battle sets (MIT; <c>data/random-battles/gen9/sets.json</c>
/// back to <c>gen2</c>, and <c>gen1/data.json</c>), the newest generation's first: a set fits when four of its moves
/// (or all of them, for a set of fewer) are moves the species' own data can teach it (<see cref="Legality"/>) and the
/// engine runs in full (<see cref="MoveData.Support"/>), and the four are drawn from those on Platinum's generator
/// (<see cref="BattleRandom"/>) seeded by the national number, so every import writes the same file. Where no set fits
/// whole, the newest of those that fit best gives what it can and the highest moves its level taught it make up the
/// four; a species with no set at all has no moves and knows what its level taught it, as any trainer's Pokémon with
/// none chosen does. The report counts both. Only the sets' move names are read, never anything else of Showdown's.
/// </para>
/// </summary>
public static class MasterTrainers
{
    public const int Level = TrainerDatabase.MasterLevel;

    /// <summary>The routines Platinum's Elite Four think with (<c>trainers.json</c>'s <c>elite_four_aaron</c>).</summary>
    public static readonly string[] Ai = { "Basic", "EvalAttack", "Expert" };

    /// <summary>Showdown's files of random-battle sets, newest generation first.</summary>
    public static string[] SetFiles => Sources.ShowdownSetFiles;

    /// <summary>Given names of our own, a master's drawn from them by the national number.</summary>
    public static readonly string[] GivenNames =
    {
        "Ada", "Bram", "Cleo", "Dov", "Edda", "Falk", "Greer", "Hollis", "Ines", "Jory", "Kit", "Lark", "Mira", "Nils",
        "Oona", "Piet", "Quill", "Rhea", "Sabin", "Tove", "Ulla", "Vance", "Wren", "Xavi", "Yara", "Zeno", "Asa", "Bette",
        "Corin", "Dagny", "Emrys", "Fern", "Gus", "Hedda", "Ivo", "Juno", "Kasper", "Lise", "Marek", "Nell", "Otto",
        "Petra", "Rafe", "Sigrid", "Tamsin", "Ugo", "Vera", "Wyatt", "Yusuf", "Zelda", "Aurel", "Brisa", "Cato", "Delphine",
        "Elio", "Freya", "Gideon", "Halle", "Isak", "Jessamy", "Kerr", "Linnea", "Matteo", "Noor"
    };

    /// <summary>
    /// What an import made: the records, how many masters a set gave moves (of whom <c>MadeUp</c> were made up to four
    /// with the moves their level taught them, no set fitting whole), and how many no set fitted at all.
    /// </summary>
    public sealed record Result(List<TrainerRecord> Masters, int WithoutMoves, int FromSets, int MadeUp);

    /// <summary>A move a master may be given: an ordinary move the engine runs in full.</summary>
    private static bool Runs(MoveData m) => m.Kind == MoveKind.Standard && m.Support == MoveEffectSupport.Full;

    /// <summary>The masters, in National Pokédex order, from the sets in Showdown's folder.</summary>
    public static Result Build(string showdownFolder, IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves, IReadOnlyList<ItemData> items)
    {
        // (A folder of Showdown's of one's own, --showdown, may hold the three tables and no sets: its masters have none)
        var sets = SetFiles.Select(f => Path.Combine(showdownFolder, f)).Where(File.Exists).Select(ReadSets).ToList();
        var movesById = new Dictionary<string, MoveData>();
        foreach (var m in moves.Where(m => m.Kind == MoveKind.Standard)) movesById.TryAdd(ShowdownId(m.Name), m);
        var byName = moves.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First());
        var machines = items.Where(i => i.TeachesMove != null).GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.First().TeachesMove);

        var masters = new List<TrainerRecord>();
        int without = 0, fromSets = 0, partial = 0;
        foreach (var s in species.OrderBy(s => s.DexNumber))
        {
            var rng = new BattleRandom((uint)s.DexNumber);
            string given = GivenNames[rng.Roll(RollKind.Other, GivenNames.Length)];
            uint personality = (uint)rng.Next16() << 16 | (uint)rng.Next16();
            var learnable = Legality.Learnable(s, species, machine => machines.GetValueOrDefault(machine));

            // The newest set that fits whole; failing that, the newest of those that fit best, made up to four with
            // the newest moves its level taught it
            List<string>? chosen = null;
            List<string>? best = null;
            string id = ShowdownId(s.Name);
            foreach (var generation in sets)
            {
                if (!generation.TryGetValue(id, out var pools)) continue;
                foreach (var pool in pools)
                {
                    var fit = pool.Select(p => movesById.GetValueOrDefault(p)).OfType<MoveData>()
                        .Where(m => Runs(m) && learnable.Contains(m.Name))
                        .Select(m => m.Name).Distinct().ToList();
                    if (fit.Count > 0 && fit.Count >= Math.Min(4, pool.Count))
                    {
                        chosen = Draw(fit, rng);
                        break;
                    }
                    if (fit.Count > (best?.Count ?? 0)) best = fit;
                }
                if (chosen != null) break;
            }
            if (chosen == null && best != null)
            {
                chosen = Draw(best, rng);
                foreach (var taught in s.Learnset.Select((m, i) => (m, i)).Where(x => x.m.Level <= Level)
                    .OrderByDescending(x => x.m.Level).ThenByDescending(x => x.i).Select(x => x.m.MoveName))
                {
                    if (chosen.Count >= 4) break;
                    if (!chosen.Contains(taught) && byName.TryGetValue(taught, out var known) && Runs(known)) chosen.Add(taught);
                }
                partial++;
            }
            if (chosen == null) without++;
            else fromSets++;

            masters.Add(new TrainerRecord
            {
                Id = TrainerDatabase.MasterId(s.Name),
                Name = given,
                Class = $"{s.Name} Master",
                Ai = Ai.ToList(),
                PrizeMoney = 0,
                Party = new()
                {
                    new TrainerPokemonRecord { Species = s.Name, Level = Level, IvScale = 255, Personality = personality, Moves = chosen }
                }
            });
        }
        return new Result(masters, without, fromSets, partial);
    }

    /// <summary>Four of the moves (all of them when there are no more), drawn one after another without putting back.</summary>
    private static List<string> Draw(List<string> fit, BattleRandom rng)
    {
        var left = fit.ToList();
        var drawn = new List<string>();
        while (drawn.Count < 4 && left.Count > 0)
        {
            int at = rng.Roll(RollKind.Other, left.Count);
            drawn.Add(left[at]);
            left.RemoveAt(at);
        }
        return drawn;
    }

    /// <summary>
    /// One generation's sets: each species' Showdown id to its sets' move pools, in the file's order, each pool a list
    /// of move ids. Generation 1's file has one pool to a species, its moves, essential, exclusive and combo moves together.
    /// </summary>
    public static Dictionary<string, List<List<string>>> ReadSets(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var bySpecies = new Dictionary<string, List<List<string>>>();
        foreach (var entry in doc.RootElement.EnumerateObject())
        {
            var pools = new List<List<string>>();
            if (entry.Value.TryGetProperty("sets", out var list))
            {
                foreach (var set in list.EnumerateArray())
                    if (set.TryGetProperty("movepool", out var pool)) pools.Add(pool.EnumerateArray().Select(m => MoveId(m.GetString()!)).Distinct().ToList());
            }
            else
            {
                var pool = new List<string>();
                foreach (string field in new[] { "moves", "essentialMoves", "exclusiveMoves", "comboMoves" })
                    if (entry.Value.TryGetProperty(field, out var moves)) pool.AddRange(moves.EnumerateArray().Select(m => MoveId(m.GetString()!)));
                if (pool.Count > 0) pools.Add(pool.Distinct().ToList());
            }
            if (pools.Count > 0) bySpecies[entry.Name] = pools;
        }
        return bySpecies;
    }

    /// <summary>A move's id as the sets give it, Hidden Power of any type as Hidden Power.</summary>
    private static string MoveId(string name)
    {
        string id = ShowdownId(name);
        return id.StartsWith("hiddenpower", StringComparison.Ordinal) ? "hiddenpower" : id;
    }

    /// <summary>A name as Showdown keys it: lower-case letters and digits alone (<c>Nidoran♀</c> is <c>nidoranf</c>, <c>Mr. Mime</c> <c>mrmime</c>).</summary>
    public static string ShowdownId(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            if (c == '♀') sb.Append('f');
            else if (c == '♂') sb.Append('m');
            else if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
