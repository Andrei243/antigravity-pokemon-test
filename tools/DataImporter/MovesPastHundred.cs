using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Plan 06 · R31: the moves of our own learned past level 100 (<c>docs/plans/R31-move-proposals.md</c>, approved by the
/// user on 2026-10-25). The 72 are whole rows of <c>Overrides/moves.json</c> (ids <see cref="FirstId"/> to
/// <see cref="LastId"/>, four to a type: its status move, its physical and its special attack, and an extra, a utility
/// move or a capstone of 120 power or more). This hands them out by the proposal's rule, appending to every species'
/// learnset by level (and to its newest games' list, which the modern rules read) and to each form's that has one of its
/// own; a level-up move past 100 is never written by hand.
/// <list type="number">
/// <item>How it fights, by its newest base stats: physical with Attack 10 or more above Sp. Atk, special the other way
/// round, both otherwise.</item>
/// <item>For each of its types (newest), first then second: the type's status move, its attacks of the categories it
/// fights with, and its extra if that is a utility move it can use.</item>
/// <item>At least four (capstones not counted): the first type's utility extra and both its attacks, whatever their
/// category, then Normal's moves, until it has four.</item>
/// <item>Capstones last: each type's, if it can use it.</item>
/// <item>The first at 110, the last at 250, the rest evenly between, each rounded to 5.</item>
/// </list>
/// Six species learn none (<see cref="LearnNone"/>).
/// </summary>
public static class MovesPastHundred
{
    public const int FirstId = 3001, LastId = 3072;
    public const int FirstLevel = 110, LastLevel = 250;

    /// <summary>Who never learns by level as others do: Ditto, Unown, Smeargle (it Sketches them), Wobbuffet, Wynaut and Shedinja.</summary>
    public static readonly IReadOnlySet<string> LearnNone = new HashSet<string> { "Ditto", "Unown", "Smeargle", "Wobbuffet", "Wynaut", "Shedinja" };

    /// <summary>One of ours.</summary>
    public static bool IsOurs(MoveData move) => move.Id is >= FirstId and <= LastId;

    private sealed record Kit(MoveData Status, MoveData Physical, MoveData Special, MoveData Extra)
    {
        public bool Capstone => Extra.Power >= 120;
    }

    /// <summary>Appends the moves to every learnset; returns how many species learn how many (for the importer's report).</summary>
    public static SortedDictionary<int, int> Append(List<PokemonSpecies> species, IReadOnlyList<MoveData> moves)
    {
        var kits = Kits(moves);
        var counts = new SortedDictionary<int, int>();
        foreach (var s in species)
        {
            if (LearnNone.Contains(s.Name)) continue;
            var types = s.Modern?.Types ?? (s.SecondaryType is { } second ? new List<PokemonType> { s.PrimaryType, second } : new List<PokemonType> { s.PrimaryType });
            var stats = s.Modern?.BaseStats ?? new StatSpread { Attack = s.BaseAttack, SpAttack = s.BaseSpAttack };
            var own = LearnedBy(types, stats, kits);
            counts[own.Count] = counts.GetValueOrDefault(own.Count) + 1;
            AppendTo(s.Learnset, own);
            if (s.Modern?.Learnset is { } modern) AppendTo(modern, own);

            // A form with moves of its own learns by its own types and stats; one with none shares its species' list
            foreach (var f in s.Forms ?? new())
            {
                if (f.Learnset == null && f.Modern?.Learnset == null) continue;
                var formTypes = f.Modern?.Types ?? f.Types ?? types;
                var formStats = f.Modern?.BaseStats ?? f.BaseStats ?? stats;
                var formOwn = LearnedBy(formTypes, formStats, kits);
                if (f.Learnset != null) AppendTo(f.Learnset, formOwn);
                if (f.Modern?.Learnset is { } formModern) AppendTo(formModern, formOwn);
            }
        }
        return counts;
    }

    private static Dictionary<PokemonType, Kit> Kits(IEnumerable<MoveData> moves)
    {
        var kits = moves.Where(IsOurs).GroupBy(m => m.Type).ToDictionary(g => g.Key, g =>
        {
            var four = g.OrderBy(m => m.Id).ToList();
            if (four.Count != 4 || four[0].Category != MoveCategory.Status || four[1].Category != MoveCategory.Physical || four[2].Category != MoveCategory.Special)
                throw new InvalidDataException($"The moves past 100 of the {g.Key} type are not a status move, a physical and a special attack and an extra");
            return new Kit(four[0], four[1], four[2], four[3]);
        });
        if (kits.Count != 18) throw new InvalidDataException($"Moves past 100 for {kits.Count} types, not 18");
        return kits;
    }

    /// <summary>The moves past 100 the rule gives a Pokémon of these types and stats, in the order it learns them (plan 03 · D14's tests ask it of the species of our own).</summary>
    public static List<MoveData> For(List<PokemonType> types, StatSpread stats, IEnumerable<MoveData> moves) => LearnedBy(types, stats, Kits(moves));

    /// <summary>The moves a Pokémon of these types and stats learns past 100, in the order it learns them.</summary>
    private static List<MoveData> LearnedBy(List<PokemonType> types, StatSpread stats, Dictionary<PokemonType, Kit> kits)
    {
        bool physical = stats.Attack - stats.SpAttack >= 10, special = stats.SpAttack - stats.Attack >= 10;
        bool Can(MoveData m) => m.Category == MoveCategory.Status || (!physical && !special) || (physical == (m.Category == MoveCategory.Physical));
        var list = new List<MoveData>();
        void Add(MoveData m)
        {
            if (!list.Contains(m)) list.Add(m);
        }

        foreach (var type in types)
        {
            var kit = kits[type];
            Add(kit.Status);
            if (Can(kit.Physical)) Add(kit.Physical);
            if (Can(kit.Special)) Add(kit.Special);
            if (!kit.Capstone && Can(kit.Extra)) Add(kit.Extra);
        }
        if (list.Count < 4)
        {
            var first = kits[types[0]];
            if (!first.Capstone) Add(first.Extra);
            Add(first.Physical);
            Add(first.Special);
        }
        var normal = kits[PokemonType.Normal];
        foreach (var m in new[] { normal.Status, normal.Physical, normal.Special, normal.Extra })
        {
            if (list.Count >= 4) break;
            Add(m);
        }
        foreach (var type in types)
            if (kits[type] is { Capstone: true } kit && Can(kit.Extra)) Add(kit.Extra);
        return list;
    }

    /// <summary>The level each of <paramref name="count"/> moves is learned at: 110 to 250, evenly, each rounded to 5.</summary>
    public static int LevelOf(int index, int count) =>
        count == 1 ? FirstLevel : (int)Math.Round((FirstLevel + (LastLevel - FirstLevel) * (double)index / (count - 1)) / 5, MidpointRounding.AwayFromZero) * 5;

    private static void AppendTo(List<LearnableMove> learnset, List<MoveData> own)
    {
        learnset.RemoveAll(l => own.Any(m => m.Name == l.MoveName));
        for (int i = 0; i < own.Count; i++) learnset.Add(new LearnableMove { Level = LevelOf(i, own.Count), MoveName = own[i].Name });
    }
}
