using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the original's eighty Seals (<c>generated/seals.txt</c>, <c>sealTypeValues</c> in
/// <c>src/applications/capsule_menu/main.c</c>): its constant without the id's prefix, its name from the bank of Seal
/// names, what the Sunyshore Market asks for one, and whether it is one of the twenty-eight that show a letter (A to
/// Z, the Shock Seal's "!" and the Mystery Seal's "?"), which burst alike wherever they are put.
/// </summary>
public sealed class SealRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public bool Letter { get; set; }
}

/// <summary>What <c>seals.json</c> holds.</summary>
public sealed class SealData
{
    /// <summary>The Seals in the original's order: the first is Seal 1.</summary>
    public List<SealRecord> Seals { get; set; } = new();

    /// <summary>The Sunyshore Market's seven Seals for each day, Sunday first (<c>SunyshoreMarketDailyStocks</c>).</summary>
    public List<List<string>> Market { get; set; } = new();

    /// <summary>
    /// The Ball Capsules trainers' Pokémon come out of (<c>res/trainers/ball_capsules.json</c>): each a list of
    /// <c>"SEAL,x,y"</c>. A trainer's Pokémon names one by its place here, counted from 1.
    /// </summary>
    public List<List<string>> TrainerCapsules { get; set; } = new();
}

/// <summary>
/// The Seals (plan 06 · R17b), imported by <c>tools/DataImporter</c>: names and numbers only. A Seal is a number from
/// 1 to <see cref="Count"/>, as the original's <c>BallSeal.type</c> is (0 is no Seal); a save names it by its constant.
/// </summary>
public static class SealDatabase
{
    public const string FileName = "seals.json";

    private static readonly SealData Data = GameDataFiles.Load<SealData>(FileName);
    private static readonly Dictionary<string, int> ByKey = Keys();

    private static Dictionary<string, int> Keys()
    {
        var keys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Data.Seals.Count; i++)
        {
            keys[Data.Seals[i].Id] = i + 1;
            keys[Data.Seals[i].Name] = i + 1;
        }
        return keys;
    }

    /// <summary>How many Seals there are: 80.</summary>
    public static int Count => Data.Seals.Count;

    /// <summary>A Seal's number by its constant (<c>HEART_SEAL_A</c>) or its name (<c>Heart Seal A</c>); 0 for none.</summary>
    public static int Seal(string? idOrName) => idOrName != null && ByKey.TryGetValue(idOrName, out int seal) ? seal : 0;

    public static bool Exists(int seal) => seal >= 1 && seal <= Count;

    public static SealRecord? Get(int seal) => Exists(seal) ? Data.Seals[seal - 1] : null;

    public static string Id(int seal) => Get(seal)?.Id ?? "";
    public static string Name(int seal) => Get(seal)?.Name ?? "";
    public static int Price(int seal) => Get(seal)?.Price ?? 0;

    /// <summary><c>CapsuleMenu_IsAlphabetSeal</c>.</summary>
    public static bool IsLetter(int seal) => Get(seal)?.Letter ?? false;

    /// <summary>The Seals the Sunyshore Market sells on a day of the week.</summary>
    public static IReadOnlyList<int> Market(DayOfWeek day) =>
        (int)day < Data.Market.Count ? Data.Market[(int)day].Select(Seal).Where(Exists).ToList() : Array.Empty<int>();

    /// <summary>How many Ball Capsules the trainers' table has.</summary>
    public static int TrainerCapsules => Data.TrainerCapsules.Count;

    /// <summary>A trainer's Ball Capsule by its number, from 1 (<c>Pokemon_SetBallSeal</c>); null for none.</summary>
    public static Models.BallCapsule? TrainerCapsule(int number) =>
        number >= 1 && number <= Data.TrainerCapsules.Count ? Models.BallCapsule.Parse(Data.TrainerCapsules[number - 1]) : null;
}
