using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What a Hoenn contest effect is for, in Emerald's order (<c>CONTEST_EFFECT_TYPE_*</c>); the opponents' thinking asks the first few.
/// </summary>
public enum ContestEffectType { Appeal, AvoidStartle, StartleMon, StartleMons, Worsen, SpecialAppeal, TurnOrder, Unknown = 8 }

/// <summary>One of Emerald's 48 contest effects (<c>gContestEffects</c>): the hearts it appeals for and the jam it throws, both in tens of points.</summary>
public sealed class ContestEffectRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContestEffectType Type { get; set; }
    public int Appeal { get; set; }
    public int Jam { get; set; }
}

/// <summary>
/// A move on Emerald's stage (<c>gContestMoves</c>): its effect, the quality it appeals with, the combo it starts
/// (<see cref="ComboStarter"/>, 0 for none) and the starters it follows well (<see cref="Combos"/>).
/// </summary>
public sealed class HoennContestMove
{
    public int Effect { get; set; }
    public ContestStat Category { get; set; }
    public int ComboStarter { get; set; }
    public List<int> Combos { get; set; } = new();
}

/// <summary>Whether a contestant of the table turns up before the game is completed, after, or either (<c>gPostgameContestOpponentFilter</c>).</summary>
public enum OpponentTime { Either, OnlyAfterTheGame, OnlyBeforeTheGame }

/// <summary>
/// One of Emerald's 96 contestants (<c>gContestOpponents</c>): a Pokémon with four moves, the five qualities of its
/// condition and its sheen, the rank it enters and the contests it enters. The nickname is its species' name here.
/// </summary>
public sealed class HoennContestOpponent
{
    public string Trainer { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int Rank { get; set; }
    public List<ContestStat> Pools { get; set; } = new();
    public List<string> Moves { get; set; } = new();
    public int Cool { get; set; }
    public int Beauty { get; set; }
    public int Cute { get; set; }
    public int Smart { get; set; }
    public int Tough { get; set; }
    public int Sheen { get; set; }
    public OpponentTime Time { get; set; }
}

/// <summary>What <c>hoenn-contest.json</c> holds (written by <c>tools/DataImporter --hoenn-contest</c>, never by hand).</summary>
public sealed class HoennContestFile
{
    public List<ContestEffectRow> Effects { get; set; } = new();
    public List<string> ComboStarters { get; set; } = new();
    public Dictionary<string, HoennContestMove> Moves { get; set; } = new();
    public List<HoennContestOpponent> Opponents { get; set; } = new();
}

/// <summary>
/// Emerald's contest tables (plan 21 · H11; <c>src/data/contest_moves.h</c>, <c>contest_opponents.h</c>): the 48
/// effects, each move's effect and combos, and the 96 contestants. Kept in a file of their own beside
/// <c>moves.json</c> since Emerald's effects are not Platinum's (the two games' lists differ in number and meaning,
/// plan 06 · R17a's <see cref="Models.SuperContests.MoveContest"/> is Platinum's), and read through
/// <see cref="MoveData.HoennContest"/>.
/// </summary>
public static class HoennContestDatabase
{
    public const string FileName = "hoenn-contest.json";

    private static readonly HoennContestFile Data = GameDataFiles.Load<HoennContestFile>(FileName);

    public static IReadOnlyList<ContestEffectRow> Effects => Data.Effects;
    public static IReadOnlyList<string> ComboStarters => Data.ComboStarters;
    public static IReadOnlyList<HoennContestOpponent> Opponents => Data.Opponents;
    public static IReadOnlyDictionary<string, HoennContestMove> Moves => Data.Moves;

    /// <summary>An effect by its number.</summary>
    public static ContestEffectRow Effect(int id) => Data.Effects[id];

    /// <summary>
    /// A move on the stage: Emerald's own row where it has one (the 354 moves of the first three generations), else a
    /// plain highly-appealing act in the quality its Platinum contest data gives it, else the quality of its type. No
    /// move is left out of a contest, since the modern rules teach Hoenn's Pokémon moves Emerald never knew.
    /// </summary>
    public static HoennContestMove MoveOf(MoveData move)
    {
        if (Data.Moves.TryGetValue(move.Name, out var own)) return own;
        return new HoennContestMove { Effect = 0, Category = Fallback(move), ComboStarter = 0, Combos = new() };
    }

    public static HoennContestMove? MoveOf(string moveName)
    {
        if (Data.Moves.TryGetValue(moveName, out var own)) return own;
        return MoveDatabase.Has(moveName) ? MoveOf(MoveDatabase.Get(moveName)) : null;
    }

    /// <summary>The quality a move with no row of Emerald's appeals with: Platinum's, else its type's.</summary>
    private static ContestStat Fallback(MoveData move)
    {
        if (move.Contest is { } platinum) return platinum.Type;
        return move.Type switch
        {
            PokemonType.Fighting or PokemonType.Rock or PokemonType.Ground or PokemonType.Steel => ContestStat.Tough,
            PokemonType.Fire or PokemonType.Electric or PokemonType.Dragon or PokemonType.Flying => ContestStat.Cool,
            PokemonType.Water or PokemonType.Ice or PokemonType.Ghost or PokemonType.Fairy => ContestStat.Beauty,
            PokemonType.Normal or PokemonType.Grass or PokemonType.Bug => ContestStat.Cute,
            _ => ContestStat.Smart
        };
    }

    /// <summary>Whether two moves make a combo, the second after the first (<c>AreMovesContestCombo</c>).</summary>
    public static bool AreCombo(HoennContestMove last, HoennContestMove next) =>
        last.ComboStarter != 0 && next.Combos.Contains(last.ComboStarter);

    /// <summary>A combo starter's name by its number (1 to 62), "" for none.</summary>
    public static string ComboStarterName(int id) => id >= 1 && id <= Data.ComboStarters.Count ? Data.ComboStarters[id - 1] : "";
}
