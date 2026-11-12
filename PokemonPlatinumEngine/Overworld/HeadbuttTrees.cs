namespace PokemonPlatinumEngine.Overworld;

/// <summary>Which of an area's tables a Headbutt tree shakes out (plan 38 · EX4).</summary>
public enum HeadbuttTreeKind
{
    /// <summary>A tree of the common table, which drops something half the time.</summary>
    Plain,
    /// <summary>A tree of the rare table, which drops something four times in five.</summary>
    Rich,
    /// <summary>One of HeartGold's secret trees, which gives its own table alone.</summary>
    Secret
}

/// <summary>
/// An area's Headbutt trees (plan 38 · EX4; HeartGold's <c>headbutt.json</c>, read by the map importer into
/// <c>WorldAreaFile.Headbutt</c>): the three tables, met by <see cref="HeadbuttTrees.SlotWeights"/>, and each tile of
/// each tree with its number and whether it is a secret one.
/// </summary>
public sealed class AreaHeadbuttTrees
{
    public List<WildEncounterEntry> Common { get; } = new();
    public List<WildEncounterEntry> Rare { get; } = new();
    public List<WildEncounterEntry> Secret { get; } = new();

    /// <summary>Every tile of every tree: the tree's number among its kind and whether it is a secret tree.</summary>
    public Dictionary<(int X, int Y), (int Tree, bool Secret)> Tiles { get; } = new();

    public int TreeCount { get; set; }
}

/// <summary>
/// The rule of a Headbutt tree (plan 38 · EX4). HeartGold's tables and trees are imported, its rule is not (the pinned
/// pokeheartgold holds no code of it), so the rule is ours (<c>docs/mechanics/rulings.md</c>, "Headbutt trees"): which
/// table a plain tree gives follows from the player's trainer number and the tree's number alone, with no draw, so the
/// same tree always gives the same table to the same player; only a shake draws, on the generator it is handed: whether
/// anything falls, then a slot by Gold and Silver's weights for Headbutt (50, 15, 15, 10, 5, 5), then its level.
/// GPU-free.
/// </summary>
public static class HeadbuttTrees
{
    /// <summary>The six slots' weights in a hundred (Gold and Silver's Headbutt table).</summary>
    public static readonly int[] SlotWeights = { 50, 15, 15, 10, 5, 5 };

    /// <summary>How often in a hundred a shake brings something down, by the tree's kind.</summary>
    public static int FallChance(HeadbuttTreeKind kind) => kind switch
    {
        HeadbuttTreeKind.Rich => 80,
        _ => 50
    };

    /// <summary>
    /// A tree's kind: a secret tree is secret; of the plain ones, two in five are rich for a player, by the last digit of
    /// the trainer number (the 16 bits shown on the Trainer Card) added to the tree's number.
    /// </summary>
    public static HeadbuttTreeKind KindOf(int tree, bool secret, uint trainerNumber) =>
        secret ? HeadbuttTreeKind.Secret
        : ((trainerNumber & 0xFFFF) % 10 + (uint)tree) % 5 < 2 ? HeadbuttTreeKind.Rich
        : HeadbuttTreeKind.Plain;

    /// <summary>The table a tree of a kind gives.</summary>
    public static List<WildEncounterEntry> TableOf(AreaHeadbuttTrees trees, HeadbuttTreeKind kind) => kind switch
    {
        HeadbuttTreeKind.Secret => trees.Secret,
        HeadbuttTreeKind.Rich => trees.Rare.Count > 0 ? trees.Rare : trees.Common,
        _ => trees.Common
    };

    /// <summary>
    /// What a shake of a tree brings down: null when nothing falls (the roll, or a table with nothing in it), else a
    /// slot of its table with its level drawn (<see cref="WildEncounterRules.Meet"/>, the lead's ability having its say,
    /// never a Repel).
    /// </summary>
    public static WildEncounterEntry? Shake(AreaHeadbuttTrees trees, int tree, bool secret, uint trainerNumber, Random random, WildLead? lead = null)
    {
        var kind = KindOf(tree, secret, trainerNumber);
        var table = TableOf(trees, kind);
        if (table.Count == 0 || random.Next(100) >= FallChance(kind)) return null;
        return WildEncounterRules.Meet(table, water: true, lead is { } first ? first with { RepelLevel = null } : null, random, keptAway: false);
    }

    /// <summary>A tree's tiles as the area file writes them, <c>"x,z x,z"</c>.</summary>
    public static IEnumerable<(int X, int Y)> TilesOf(string tree)
    {
        foreach (string pair in tree.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int comma = pair.IndexOf(',');
            yield return (int.Parse(pair[..comma], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(pair[(comma + 1)..], System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
