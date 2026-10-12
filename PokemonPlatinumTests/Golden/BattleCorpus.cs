using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// One battle of the golden corpus (plan 24 · X2), made from its seed alone: battles 1 to 100 under Platinum's
/// rules, from Platinum's 493 species and its held items, and 101 to 200 under the modern rules, from all 1025
/// species and every held item the engine runs, each built <c>Under(Ruleset.Modern)</c>. Every third is a double
/// battle, and every fourth is against a trainer of <c>trainers.json</c> thinking with its own AI flags; the rest
/// against an Ace Trainer of three drawn like the player's. Both sides are chosen for by the trainer AI, so the
/// whole battle is played once it has started.
/// <para>
/// The teams are drawn from a <see cref="BattleRandom"/> of the seed, whose numbers are Platinum's and can't change
/// with .NET (species, level, a personality that decides nature, gender and ability, the IVs, a held item by its
/// hold effect or none, and the moves its level taught it), and the battle is given another of the same seed. The
/// rules and the player's name are handed to the battle, never read from the game's statics.
/// </para>
/// </summary>
internal sealed class BattleCorpus
{
    public const int PerRules = 100;
    public const int Battles = 2 * PerRules;
    public const string PlayerName = "Lucas";

    public int Seed { get; }
    public Ruleset Rules { get; }
    public BattleCore Core { get; }

    /// <summary>The trainer of <c>trainers.json</c> the player meets, or null for a drawn Ace Trainer.</summary>
    public TrainerRecord? Opponent { get; }

    private readonly Trainer foe;
    private readonly string teams;
    private readonly BattleText text;

    public static Ruleset RulesOf(int seed) => seed <= PerRules ? Ruleset.Platinum : Ruleset.Modern;
    public static bool DrawnDouble(int seed) => seed % 3 == 0;
    public static bool AgainstTheData(int seed) => seed % 4 == 0;

    /// <summary>The seeds of one rules' file: 1 to 100 for Platinum's, 101 to 200 for the modern ones.</summary>
    public static IEnumerable<int> SeedsOf(Ruleset rules) => Enumerable.Range(rules == Ruleset.Platinum ? 1 : PerRules + 1, PerRules);

    private static readonly List<PokemonSpecies> PlatinumSpecies =
        PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= 493).OrderBy(s => s.DexNumber).ToList();
    private static readonly List<PokemonSpecies> ModernSpecies =
        PokemonDatabase.GetAll().Where(s => s.DexNumber >= 1).OrderBy(s => s.DexNumber).Select(s => s.Under(Ruleset.Modern)).ToList();
    private static readonly Dictionary<string, PokemonSpecies> ModernByName = ModernSpecies.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    // One copy of each move under the modern rules, so that a move is the same move wherever it is met
    private static readonly Dictionary<string, MoveData> ModernMoves =
        MoveDatabase.GetAll().ToDictionary(m => m.Name, m => m.Under(Ruleset.Modern), StringComparer.OrdinalIgnoreCase);

    // The held items by hold effect, each effect as likely as another however many items share it
    private static readonly List<List<ItemData>> PlatinumItems = ByHoldEffect(DataImporter.Coverage.IsPlatinums);
    private static readonly List<List<ItemData>> ModernItems = ByHoldEffect(_ => true);

    private static readonly List<TrainerRecord> Trainers =
        TrainerDatabase.All.Where(t => t.Party.Count > 0).OrderBy(t => t.Id, StringComparer.Ordinal).ToList();

    private static List<List<ItemData>> ByHoldEffect(Func<ItemData, bool> whose) => ItemDatabase.GetAll()
        .Where(i => HeldItemEffects.IsHoldable(i) && whose(i))
        .GroupBy(i => i.HoldEffect!, StringComparer.Ordinal)
        .OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => g.OrderBy(i => i.Id).ToList())
        .ToList();

    /// <summary>The battle of this seed, drawn and ready to start.</summary>
    public BattleCorpus(int seed)
    {
        Seed = seed;
        Rules = RulesOf(seed);
        bool modern = Rules == Ruleset.Modern, doubles = DrawnDouble(seed);
        var dice = new BattleRandom((uint)seed);

        // A trainer of the data first, so that the player's levels can follow theirs
        int lowest = 5, highest = 80;
        if (AgainstTheData(seed))
        {
            var candidates = Trainers.Where(t => t.Party.Count >= (doubles ? 2 : 1)).ToList();
            Opponent = candidates[dice.Next(candidates.Count)];
            int top = Opponent.Party.Max(m => m.Level);
            (lowest, highest) = (Math.Max(1, top - 5), Math.Min(100, top + 5));
        }

        var mine = Team(dice, modern, lowest, highest);
        if (Opponent is { } record)
        {
            var party = TrainerDatabase.PartyOf(record);
            if (modern) foreach (var p in party.Members) Modernise(p);
            foe = new Trainer
            {
                Id = record.Id, Name = record.Name, TrainerClass = record.Class, Party = party, PrizeMoney = record.PrizeMoney,
                Ai = AiFlagNames.Parse(record.Ai), Items = record.Items.ToList(), DoubleBattle = record.DoubleBattle, FromPlatinum = true
            };
        }
        else foe = new Trainer { Name = "Tester", TrainerClass = "Ace Trainer", Party = Team(dice, modern, 5, 80) };

        Core = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, Trainers = new List<Trainer> { foe }, Random = new BattleRandom((uint)seed), Rules = Rules,
            Format = doubles ? BattleFormat.Double : BattleFormat.Single, PlayerController = TrainerAi.Instance, PlayerName = PlayerName
        });

        // The teams as they go in: the battle changes them
        text = BattleText.Of(Core);
        var head = new StringBuilder();
        head.Append("mine:\n");
        for (int i = 0; i < mine.Count; i++) head.Append($"  mine#{i} {BattleText.Member(mine.Members[i])}\n");
        head.Append($"foe: {foe.FullTitle}");
        if (Opponent != null) head.Append($" ({Opponent.Id})");
        head.Append($", ai {Flags(foe.Ai)}, items {(foe.Items.Count == 0 ? "none" : string.Join(", ", foe.Items))}\n");
        for (int i = 0; i < foe.Party.Count; i++) head.Append($"  foe#{i} {BattleText.Member(foe.Party.Members[i])}\n");
        teams = head.ToString();
    }

    /// <summary>The battle's block of the golden file: its seed, rules, format, both teams, the log, the result and the turns.</summary>
    public string Block()
    {
        var block = new StringBuilder();
        block.Append($"{GoldenFile.BlockMark}battle {Seed}\n");
        block.Append($"rules: {Rules.Name}\n");
        block.Append($"format: {Core.Format}\n");
        block.Append(teams);
        block.Append("log:\n");
        foreach (string line in text.Lines(Core.Log)) block.Append("  ").Append(line).Append('\n');
        block.Append($"result: {Core.Result} after {Core.Turn} turns\n");
        return block.ToString();
    }

    /// <summary>The flags a trainer thinks with, by name.</summary>
    public static string Flags(AiFlags flags)
    {
        var named = Enum.GetValues<AiFlags>().Where(f => f != AiFlags.None && flags.HasFlag(f)).Select(f => f.ToString()).ToList();
        return named.Count == 0 ? "none" : string.Join(" ", named);
    }

    private static Party Team(BattleRandom dice, bool modern, int lowest, int highest)
    {
        var pool = modern ? ModernSpecies : PlatinumSpecies;
        var items = modern ? ModernItems : PlatinumItems;
        var party = new Party();
        for (int i = 0; i < 3; i++)
        {
            var p = new Pokemon(pool[dice.Next(pool.Count)], dice.Next(lowest, highest + 1), dice);
            if (dice.Next(2) == 1)
            {
                var effect = items[dice.Next(items.Count)];
                p.HeldItem = effect[dice.Next(effect.Count)];
            }
            if (modern) ModerniseMoves(p);
            party.Add(p);
        }
        return party;
    }

    /// <summary>A trainer's Pokémon, built from Platinum's data, as the modern rules have its species and moves.</summary>
    private static void Modernise(Pokemon p)
    {
        p.Species = ModernByName[p.Species.Name];
        p.RecalculateStats();
        p.CurrentHP = p.MaxHP;
        ModerniseMoves(p);
    }

    private static void ModerniseMoves(Pokemon p)
    {
        for (int i = 0; i < p.Moves.Count; i++) p.Moves[i] = new Move(ModernMoves[p.Moves[i].Name]);
    }
}
