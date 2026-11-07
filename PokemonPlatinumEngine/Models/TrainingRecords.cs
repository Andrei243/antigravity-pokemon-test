using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A prize a stat's first gold gives (plan 36 · TR2; <c>Data/training.json</c>'s <c>drillPrizes.firstGold</c>).</summary>
public sealed class FirstGoldPrize
{
    [DataRequired] public StatType Stat { get; set; }
    [DataRequired] public string Item { get; set; } = "";
}

/// <summary>
/// A prize every so many golds give (plan 36 · TR2; <c>drillPrizes.everyGolds</c>): an item for each whole
/// <see cref="Golds"/> golds won, given only once a Hall of Fame has been entered where <see cref="AfterHallOfFame"/> says.
/// </summary>
public sealed class RepeatingPrize
{
    [DataRequired] public int Golds { get; set; }
    [DataRequired] public string Item { get; set; } = "";
    public bool AfterHallOfFame { get; set; }
}

/// <summary>The drills' prizes (plan 36 · TR2), a table of ours.</summary>
public sealed class DrillPrizes
{
    public List<FirstGoldPrize> FirstGold { get; set; } = new();
    public List<RepeatingPrize> EveryGolds { get; set; } = new();
}

/// <summary>The file <c>Data/training.json</c> (plan 36 · TR2; ours, read strictly): the drills' prizes. TR3 adds its Battle Points list here.</summary>
public sealed class TrainingFile
{
    public DrillPrizes DrillPrizes { get; set; } = new();
}

/// <summary>The training club's tables (plan 36 · TR2; <c>Data/training.json</c>). No drawing or input.</summary>
public static class TrainingData
{
    // Written in two halves, as the Pokéathlon's is: "training" is also a section of ui.json, and the text tests take a
    // literal that begins with a section's name for a key
    private const string FileName = "training" + ".json";

    private static readonly Lazy<TrainingFile> file = new(() => GameDataFiles.Load<TrainingFile>(FileName, strict: true));

    public static TrainingFile File => file.Value;

    public static DrillPrizes Prizes => File.DrillPrizes;
}

/// <summary>What the training records keep in the save (plan 36 · TR2): plain values; null in <c>SaveData</c> while nothing is kept.</summary>
public sealed class TrainingSave
{
    /// <summary>Each stat's best score, by the stat's name.</summary>
    public Dictionary<string, int>? Best { get; set; }
    /// <summary>The stats that have earned a gold, by name.</summary>
    public List<string>? GoldStats { get; set; }
    public int Drills { get; set; }
    public int Golds { get; set; }
    /// <summary>The first-gold prizes handed over, by the stat's name.</summary>
    public List<string>? FirstGoldsGiven { get; set; }
    /// <summary>The repeating prizes handed over, how many of each item.</summary>
    public Dictionary<string, int>? Given { get; set; }
}

/// <summary>
/// The drills' records (plan 36 · TR2; <c>docs/mechanics/rulings.md</c>, "The training hall"): each stat's best score, the
/// drills played and the golds won, and the prizes they have earned and the coach has handed over. A prize is earned
/// by the records alone (a stat's first gold, every so many golds) and waits until the coach is spoken to
/// (<see cref="Owed"/>, <see cref="Hand"/>). Saved as <c>SaveData.Training</c>. No drawing or input.
/// </summary>
public sealed class TrainingRecords
{
    private readonly Dictionary<StatType, int> best = new();
    private readonly HashSet<StatType> goldStats = new();
    private readonly HashSet<StatType> firstGoldsGiven = new();
    private readonly Dictionary<string, int> given = new(StringComparer.Ordinal);

    public int Drills { get; private set; }
    public int Golds { get; private set; }

    public bool IsEmpty => Drills == 0 && Golds == 0 && best.Count == 0 && given.Count == 0 && firstGoldsGiven.Count == 0;

    /// <summary>A stat's best score, 0 before any.</summary>
    public int Best(StatType stat) => best.GetValueOrDefault(stat);

    /// <summary>Whether a stat has ever earned a gold.</summary>
    public bool HasGold(StatType stat) => goldStats.Contains(stat);

    /// <summary>How many of a repeating prize's item the coach has handed over.</summary>
    public int GivenCount(string item) => given.GetValueOrDefault(item);

    /// <summary>
    /// A drill played to its end: counted, its score kept as the stat's best where it beats it, a gold counted. Returns
    /// whether the score is a new best for the stat (a first score counts as one).
    /// </summary>
    public bool Record(StatType stat, int score, DrillGrade grade)
    {
        Drills++;
        if (grade == DrillGrade.Gold)
        {
            Golds++;
            goldStats.Add(stat);
        }
        if (best.TryGetValue(stat, out int was) && was >= score) return false;
        best[stat] = score;
        return true;
    }

    /// <summary>
    /// The prizes the records have earned and the coach hasn't handed over, in the order he hands them: each stat's first
    /// gold its vitamin (in the order the drills list the stats), then every repeating prize as many times as its golds
    /// have come round, those kept for a Champion only once <paramref name="hallOfFame"/>.
    /// </summary>
    public IReadOnlyList<string> Owed(bool hallOfFame, DrillPrizes? prizes = null)
    {
        prizes ??= TrainingData.Prizes;
        var owed = new List<string>();
        foreach (var stat in DrillKinds.Stats)
            if (goldStats.Contains(stat) && !firstGoldsGiven.Contains(stat) && prizes.FirstGold.FirstOrDefault(p => p.Stat == stat) is { } first)
                owed.Add(first.Item);
        foreach (var every in prizes.EveryGolds)
        {
            if (every.Golds <= 0 || every.AfterHallOfFame && !hallOfFame) continue;
            for (int n = Golds / every.Golds - GivenCount(every.Item); n > 0; n--) owed.Add(every.Item);
        }
        return owed;
    }

    /// <summary>
    /// The first prize owed, marked as handed over; null when nothing is owed. The caller puts it in the bag.
    /// </summary>
    public string? Hand(bool hallOfFame, DrillPrizes? prizes = null)
    {
        prizes ??= TrainingData.Prizes;
        foreach (var stat in DrillKinds.Stats)
            if (goldStats.Contains(stat) && !firstGoldsGiven.Contains(stat) && prizes.FirstGold.FirstOrDefault(p => p.Stat == stat) is { } first)
            {
                firstGoldsGiven.Add(stat);
                return first.Item;
            }
        foreach (var every in prizes.EveryGolds)
        {
            if (every.Golds <= 0 || every.AfterHallOfFame && !hallOfFame) continue;
            if (Golds / every.Golds - GivenCount(every.Item) <= 0) continue;
            given[every.Item] = GivenCount(every.Item) + 1;
            return every.Item;
        }
        return null;
    }

    public void Clear()
    {
        best.Clear();
        goldStats.Clear();
        firstGoldsGiven.Clear();
        given.Clear();
        Drills = Golds = 0;
    }

    public TrainingSave? ToSave() => IsEmpty ? null : new TrainingSave
    {
        Best = best.Count > 0 ? best.ToDictionary(p => p.Key.ToString(), p => p.Value) : null,
        GoldStats = goldStats.Count > 0 ? DrillKinds.Stats.Where(goldStats.Contains).Select(s => s.ToString()).ToList() : null,
        Drills = Drills,
        Golds = Golds,
        FirstGoldsGiven = firstGoldsGiven.Count > 0 ? DrillKinds.Stats.Where(firstGoldsGiven.Contains).Select(s => s.ToString()).ToList() : null,
        Given = given.Count > 0 ? new Dictionary<string, int>(given) : null
    };

    /// <summary>Takes up a save's records (none for null); a stat's name no stat has is passed over.</summary>
    public void Load(TrainingSave? save)
    {
        Clear();
        if (save == null) return;
        Drills = Math.Max(0, save.Drills);
        Golds = Math.Max(0, save.Golds);
        foreach (var (name, score) in save.Best ?? new())
            if (Enum.TryParse<StatType>(name, out var stat)) best[stat] = Math.Max(0, score);
        foreach (var name in save.GoldStats ?? new())
            if (Enum.TryParse<StatType>(name, out var stat)) goldStats.Add(stat);
        foreach (var name in save.FirstGoldsGiven ?? new())
            if (Enum.TryParse<StatType>(name, out var stat)) firstGoldsGiven.Add(stat);
        foreach (var (item, n) in save.Given ?? new())
            if (n > 0) given[item] = n;
    }
}
