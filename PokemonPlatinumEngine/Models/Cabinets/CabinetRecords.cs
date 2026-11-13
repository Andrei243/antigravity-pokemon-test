using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Cabinets;

/// <summary>A stage of the match-three cabinet (<c>Data/cabinets.json</c>, ours): its foe, HP, swaps, seed and coins.</summary>
public sealed class CabinetStage
{
    [DataRequired] public string Id { get; set; } = "";
    /// <summary>The foe's species, whose types the chart reads (a sculpt of the game's, drawn as its menu sprite).</summary>
    [DataRequired] public string Foe { get; set; } = "";
    [DataRequired] public int Hp { get; set; }
    [DataRequired] public int Swaps { get; set; }
    [DataRequired] public uint Seed { get; set; }
    [DataRequired] public int Coins { get; set; }
    [DataRequired] public int CoinsPerSwapLeft { get; set; }

    /// <summary>The foe's types (its species' own; Normal for a name the data lacks).</summary>
    public IReadOnlyList<PokemonType> FoeTypes
    {
        get
        {
            var species = PokemonDatabase.Get(Foe);
            if (species == null) return new[] { PokemonType.Normal };
            return species.SecondaryType is { } second ? new[] { species.PrimaryType, second } : new[] { species.PrimaryType };
        }
    }
}

/// <summary>What a picture of each grid size pays.</summary>
public sealed class PicturePay
{
    [DataRequired] public int Size { get; set; }
    [DataRequired] public int Coins { get; set; }
    /// <summary>What each mistake takes off it, down to <see cref="Least"/>.</summary>
    [DataRequired] public int PerMistake { get; set; }
    [DataRequired] public int Least { get; set; }
}

public sealed class CabinetsFile
{
    [DataRequired] public List<CabinetStage> Stages { get; set; } = new();
    /// <summary>The species that fill the board's places the team leaves empty, in order.</summary>
    [DataRequired] public List<string> Helpers { get; set; } = new();
    [DataRequired] public List<PicturePay> Pictures { get; set; } = new();
}

/// <summary>The arcade's cabinets' table (<c>Data/cabinets.json</c>, written by hand, ours).</summary>
public static class CabinetDatabase
{
    public const string FileName = "cabinets.json";

    private static readonly Lazy<CabinetsFile> file = new(() => GameDataFiles.Load<CabinetsFile>(FileName, strict: true));

    public static IReadOnlyList<CabinetStage> Stages => file.Value.Stages;
    public static IReadOnlyList<string> Helpers => file.Value.Helpers;
    public static IReadOnlyList<PicturePay> Pictures => file.Value.Pictures;

    public static CabinetStage? Stage(string id) => Stages.FirstOrDefault(s => s.Id == id);

    /// <summary>What a solved picture of <paramref name="size"/> pays after <paramref name="mistakes"/>.</summary>
    public static int PayFor(int size, int mistakes)
    {
        var pay = Pictures.FirstOrDefault(p => p.Size == size);
        if (pay == null) return 0;
        return Math.Max(pay.Least, pay.Coins - pay.PerMistake * mistakes);
    }

    /// <summary>
    /// The four species of a board: the team's first four species (each once, no Egg), then the table's helpers in
    /// their order for whatever places are left.
    /// </summary>
    public static List<string> BoardSpecies(IEnumerable<Pokemon> team)
    {
        var picked = new List<string>();
        foreach (var p in team)
        {
            if (p.IsEgg || picked.Contains(p.Species.Name)) continue;
            picked.Add(p.Species.Name);
            if (picked.Count == MatchThree.Pieces) return picked;
        }
        foreach (var h in Helpers)
        {
            if (picked.Count == MatchThree.Pieces) break;
            if (!picked.Contains(h)) picked.Add(h);
        }
        return picked;
    }
}

/// <summary>What a save keeps of the arcade (<see cref="Core.SaveData.Cabinets"/>, plan 42 · PS6). Null while nothing is.</summary>
public sealed class CabinetsSave
{
    public List<string> StagesCleared { get; set; } = new();
    public Dictionary<string, int> BestScores { get; set; } = new();
    /// <summary>Pictures solved, by species, with the grid size they were solved at.</summary>
    public Dictionary<string, int> Pictures { get; set; } = new();
}

/// <summary>The arcade's records (plan 42 · PS6; GPU-free): stages cleared and their best scores, pictures solved by species.</summary>
public sealed class CabinetRecords
{
    private readonly HashSet<string> cleared = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> best = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> pictures = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> StagesCleared => cleared;
    public IReadOnlyDictionary<string, int> Pictures => pictures;

    public bool HasCleared(string stage) => cleared.Contains(stage);
    public int BestOf(string stage) => best.TryGetValue(stage, out int b) ? b : 0;
    public bool HasSolved(string species) => pictures.ContainsKey(species);

    /// <summary>The first stage of the table not cleared yet (the last when all are).</summary>
    public CabinetStage? NextStage() =>
        CabinetDatabase.Stages.FirstOrDefault(s => !cleared.Contains(s.Id)) ?? CabinetDatabase.Stages.LastOrDefault();

    public void RecordStage(string stage, int score, bool won)
    {
        if (won) cleared.Add(stage);
        if (score > BestOf(stage)) best[stage] = score;
    }

    public void RecordPicture(string species, int size) => pictures[species] = size;

    public void Clear()
    {
        cleared.Clear();
        best.Clear();
        pictures.Clear();
    }

    public CabinetsSave? ToSave() => cleared.Count == 0 && best.Count == 0 && pictures.Count == 0 ? null
        : new CabinetsSave { StagesCleared = cleared.OrderBy(s => s, StringComparer.Ordinal).ToList(), BestScores = new(best), Pictures = new(pictures) };

    public void Load(CabinetsSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var s in save.StagesCleared) cleared.Add(s);
        foreach (var (k, v) in save.BestScores) best[k] = v;
        foreach (var (k, v) in save.Pictures) pictures[k] = v;
    }
}
