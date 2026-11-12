using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>A hollow as <c>Data/hollows.json</c> writes it (plan 44 · SD9).</summary>
public sealed record HollowData
{
    /// <summary>The hollow's id: its words are <c>delve.hollow.&lt;key&gt;</c>.</summary>
    [DataRequired] public string Key { get; init; } = "";
    /// <summary>Its name in English, as the place a recruit was met is kept.</summary>
    [DataRequired] public string Name { get; init; } = "";
    [DataRequired] public DelveTheme Theme { get; init; }
    [DataRequired] public int Floors { get; init; }
    [DataRequired] public int Level { get; init; }
    [DataRequired] public List<string> Residents { get; init; } = new();
    public int ResidentsPerFloor { get; init; } = 3;
    [DataRequired] public List<string> Finds { get; init; } = new();
    public int FindsPerFloor { get; init; } = 3;
    public int SnaresPerFloor { get; init; } = 2;
    /// <summary>The species that guards the last floor's stairs.</summary>
    public string? Guardian { get; init; }
    /// <summary>The camp's rank it opens at.</summary>
    public int Rank { get; init; }
    /// <summary>The song its floors play (a song's id, <c>sinnoh/cave</c>).</summary>
    public string? Song { get; init; }
    /// <summary>Its floors are dark round the team.</summary>
    public bool Dark { get; init; }

    public DelveHollow ToHollow() => new(Key, Theme, Floors, Level, Residents, ResidentsPerFloor, Finds, FindsPerFloor, SnaresPerFloor, Guardian, Rank, Song, Dark, Name);
}

/// <summary>A prize of the camp's board, by a request's kind (plan 44 · SD9).</summary>
public sealed record HollowPrizeData
{
    [DataRequired] public DelveRequestKind Kind { get; init; }
    [DataRequired] public string Item { get; init; } = "";
    public int Count { get; init; } = 1;
}

/// <summary>The file of the hollows and the board's prizes (<c>Data/hollows.json</c>).</summary>
public sealed record HollowsFile
{
    public List<HollowData> Hollows { get; init; } = new();
    public List<HollowPrizeData> Prizes { get; init; } = new();
}

/// <summary>The four hollows of the camp, read strictly from <c>Data/hollows.json</c> (plan 44 · SD9). No drawing or input.</summary>
public static class DelveHollows
{
    public const string FileName = "hollows.json";

    private static readonly Lazy<HollowsFile> file = new(() => GameDataFiles.Load<HollowsFile>(FileName, strict: true));
    private static readonly Lazy<IReadOnlyList<DelveHollow>> all = new(() => file.Value.Hollows.Select(h => h.ToHollow()).ToList());

    public static IReadOnlyList<DelveHollow> All => all.Value;
    public static IReadOnlyList<HollowPrizeData> Prizes => file.Value.Prizes;

    /// <summary>A hollow by its key, or null (the test hollow too, for the tests and a tool).</summary>
    public static DelveHollow? Get(string? key) =>
        key == DelveHollow.Test.Key ? DelveHollow.Test : All.FirstOrDefault(h => h.Key == key);
}
