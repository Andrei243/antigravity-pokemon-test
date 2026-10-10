using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the original's contestants (<c>contest_data.narc</c>'s first file, 96 of them): who they are, the Pokémon
/// they bring with its four moves and its condition, the contests they turn up in, and how they are dressed for each
/// of the twelve themes.
/// </summary>
public sealed class ContestOpponent
{
    /// <summary>The trainer's name and the Pokémon's nickname (<c>contest_opponent_names.json</c>).</summary>
    public string Name { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Species { get; set; } = "";
    public List<string> Moves { get; set; } = new();
    public int Cool { get; set; }
    public int Beauty { get; set; }
    public int Cute { get; set; }
    public int Smart { get; set; }
    public int Tough { get; set; }
    public int Sheen { get; set; }
    public ContestRank Rank { get; set; }

    /// <summary>The qualities whose contests they enter.</summary>
    public List<ContestStat> Contests { get; set; } = new();

    /// <summary>Met only in a practice round, or only in a single round held for itself (the original's two flags).</summary>
    public bool Practice { get; set; }
    public bool SingleRound { get; set; }

    /// <summary>
    /// When they turn up: 0 always, 1 only until the game is completed and the National Pokédex obtained, 2 only
    /// after, 3 after and as the one guest of a contest (<see cref="SuperContest.ChooseOpponents"/>).
    /// </summary>
    public int Season { get; set; }

    /// <summary>The trainer's gender, which the Pokémon's is made to be where its species allows (<c>sub_02095380</c>): 0 a man, 1 a woman.</summary>
    public int Gender { get; set; }

    /// <summary>
    /// Which of the original's four ways they think and dance, 0 to 3 (its <c>unk_FC</c>): the column of the Acting
    /// Competition's table they choose by, and in the dance the higher the looser on the beat.
    /// </summary>
    public int Mind { get; set; }

    /// <summary>The original's field look, by its constant's short name (<c>lass</c>), for whoever draws them.</summary>
    public string Look { get; set; } = "";

    /// <summary>The dressing they wear for each of the twelve themes: a place of <see cref="SuperContestData.Dressings"/>.</summary>
    public List<int> Dressings { get; set; } = new();

    /// <summary>The Pokémon's fame, which the applause follows.</summary>
    public int Fame { get; set; }
}

/// <summary>A judge (<c>contest_data.narc</c>'s second file): for each quality 0 never, 1 one of the two at the sides, 2 one who may sit in the middle.</summary>
public sealed class ContestJudge
{
    public string Name { get; set; } = "";

    /// <summary>The original's field look, by its constant's short name (the struct's two bytes after the name).</summary>
    public string Look { get; set; } = "";
    public ContestRank Rank { get; set; }
    public List<int> Seats { get; set; } = new();
}

/// <summary>
/// A ready-made dressing (the third file): up to twenty accessories, each <c>"accessory,x,y,priority"</c> in the
/// original's picture of 256 by 192, the Pokémon's own priority among them and the backdrop behind.
/// </summary>
public sealed class ContestDressing
{
    public List<string> Accessories { get; set; } = new();
    public int PokemonPriority { get; set; }
    public int Backdrop { get; set; }

    /// <summary>The accessories' numbers, in the order they were put on.</summary>
    public IEnumerable<int> Ids => Accessories.Select(a => int.Parse(a.AsSpan(0, a.IndexOf(','))));

    /// <summary>Each accessory with where it is in the picture.</summary>
    public IEnumerable<(int Id, int X, int Y, int Priority)> Placed => Accessories.Select(a =>
    {
        var parts = a.Split(',');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
    });
}

/// <summary>A theme of the Visual Competition and the points each accessory scores for it, a digit each in the accessories' order.</summary>
public sealed class ContestTheme
{
    public string Name { get; set; } = "";
    public string Points { get; set; } = "";

    public int PointsFor(int accessory) => accessory >= 0 && accessory < Points.Length ? Points[accessory] - '0' : 0;
}

/// <summary>What <c>contests.json</c> holds.</summary>
public sealed class SuperContestData
{
    public List<string> Accessories { get; set; } = new();

    /// <summary>The eighteen backdrops' names, in the original's order (<c>contest_backdrop_names.json</c>; plan 06 · R17b).</summary>
    public List<string> Backdrops { get; set; } = new();
    public List<ContestTheme> Themes { get; set; } = new();
    public List<ContestJudge> Judges { get; set; } = new();
    public List<ContestDressing> Dressings { get; set; } = new();
    public List<ContestOpponent> Opponents { get; set; } = new();
}

/// <summary>
/// The Super Contests' tables (plan 06 · R17a), imported from the decompilation's <c>contest_data.narc</c> and its
/// name banks by <c>tools/DataImporter</c>: the hundred accessories' names, the twelve themes and what each accessory
/// scores for them, the judges, the contestants and their dressings. Names and numbers only; nothing drawn.
/// </summary>
public static class SuperContestDatabase
{
    public const string FileName = "contests.json";

    /// <summary>The accessories that come one of a kind (the original's <c>NON_UNIQUE_ACCESSORY_COUNT</c> and on).</summary>
    public const int FirstUniqueAccessory = 61;

    private static readonly SuperContestData Data = GameDataFiles.Load<SuperContestData>(FileName);
    private static readonly Dictionary<string, int> AccessoryIds = Data.Accessories
        .Select((name, id) => (name, id)).ToDictionary(a => a.name, a => a.id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Accessories => Data.Accessories;
    public static IReadOnlyList<ContestTheme> Themes => Data.Themes;
    public static IReadOnlyList<ContestJudge> Judges => Data.Judges;
    public static IReadOnlyList<ContestDressing> Dressings => Data.Dressings;
    public static IReadOnlyList<ContestOpponent> Opponents => Data.Opponents;

    /// <summary>An accessory's number by its name, or -1.</summary>
    public static int Accessory(string name) => AccessoryIds.GetValueOrDefault(name, -1);

    public static string AccessoryName(int id) => id >= 0 && id < Data.Accessories.Count ? Data.Accessories[id] : "";

    public static IReadOnlyList<string> Backdrops => Data.Backdrops;

    /// <summary>A backdrop's number by its name, or -1.</summary>
    public static int Backdrop(string name) => Data.Backdrops.FindIndex(b => string.Equals(b, name, StringComparison.OrdinalIgnoreCase));

    public static string BackdropName(int id) => id >= 0 && id < Data.Backdrops.Count ? Data.Backdrops[id] : "";
}
