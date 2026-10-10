using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>One Pokémon of a team in the Hall of Fame, as the original keeps it: what it was, never its stats.</summary>
public sealed record HallOfFameMember(
    string Species, string? Form, int Level, string Nickname, Gender Gender, bool Shiny, uint Personality,
    string TrainerName, int TrainerId, IReadOnlyList<string> Moves);

/// <summary>A team entered into the Hall of Fame, and the day it was.</summary>
public sealed record HallOfFameEntry(DateTime Date, IReadOnlyList<HallOfFameMember> Team);

/// <summary>
/// The Hall of Fame's records (plan 06 · R12; <c>hall_of_fame_entries.c</c>): the last thirty teams to enter it,
/// and how many have in all, which stops counting at 9,999 (then nothing more is kept). The PC shows the newest
/// first, numbered from the count down. The day of the first entry is the Trainer Card's "Hall of Fame debut". No
/// drawing or input.
/// </summary>
public sealed class HallOfFame
{
    public const int Kept = 30, MostEver = 9999;

    private readonly List<HallOfFameEntry> entries = new();

    /// <summary>The teams kept, oldest first.</summary>
    public IReadOnlyList<HallOfFameEntry> Entries => entries;

    /// <summary>How many teams have entered, ever.</summary>
    public int Total { get; private set; }

    /// <summary>When the first team entered (the card's debut), the day and the time; null before.</summary>
    public DateTime? Debut { get; private set; }

    /// <summary>
    /// Enters a team (<c>ClearGame_AddHallOfFameEntry</c>): its Pokémon in the team's order, each with whose it
    /// is. Nothing more is kept once 9,999 have entered.
    /// </summary>
    public void Enter(Party party, Func<Pokemon, (string Name, int Id)> trainerOf, DateTime now)
    {
        if (Total >= MostEver) return;
        Debut ??= now;
        var team = party.Members.Select(p =>
        {
            var (name, id) = trainerOf(p);
            return new HallOfFameMember(p.Species.Name, p.Form, p.Level, p.Nickname, p.Gender, p.IsShiny, p.Personality, name, id,
                p.Moves.Select(m => m.Name).ToList());
        }).ToList();
        entries.Add(new HallOfFameEntry(now.Date, team));
        if (entries.Count > Kept) entries.RemoveAt(0);
        Total++;
    }

    /// <summary>The teams newest first, each with the number the PC shows it under (the newest is the count).</summary>
    public IEnumerable<(int Number, HallOfFameEntry Entry)> NewestFirst() =>
        entries.AsEnumerable().Reverse().Select((e, i) => (Total - i, e));

    /// <summary>Brings back what a save kept.</summary>
    public void Restore(int total, DateTime? debut, IEnumerable<HallOfFameEntry> kept)
    {
        entries.Clear();
        entries.AddRange(kept.TakeLast(Kept));
        Total = Math.Max(total, entries.Count);
        Debut = debut;
    }
}

/// <summary>
/// The Trainer Card's colour (plan 06 · R12; <c>TrainerCase_CalculateTrainerCardLevel</c>): a step for each of the
/// five things the original asks, from the plain card to black. Without a Pokédex the card has a colour of its own.
/// </summary>
public static class TrainerCardRules
{
    public enum CardColour { NoPokedex, Normal, Cobalt, Bronze, Silver, Gold, Black }

    /// <summary>
    /// The species the National Pokédex counts as complete without (<c>sExcludedMonsNational</c>): the mythical
    /// ones, and Lugia and Ho-Oh, which Platinum has no way to meet. 493 less these is 482.
    /// </summary>
    public static readonly IReadOnlyList<string> NotNeededNational = new[]
    {
        "Mew", "Lugia", "Ho-Oh", "Celebi", "Jirachi", "Deoxys", "Phione", "Manaphy", "Darkrai", "Shaymin", "Arceus"
    };

    public const int NationalGoal = 493 - 11, TowerStreak = 100, UndergroundFlags = 50;

    /// <summary>How many of the five the player has done: the Hall of Fame, the National Pokédex caught, a streak of a hundred at the Battle Tower, a Master Rank contest won, fifty flags taken in the Underground.</summary>
    public static int Level(bool hallOfFame, int nationalCaught, int towerStreak, bool contestMaster, int undergroundFlags) =>
        (hallOfFame ? 1 : 0) + (nationalCaught >= NationalGoal ? 1 : 0) + (towerStreak >= TowerStreak ? 1 : 0)
        + (contestMaster ? 1 : 0) + (undergroundFlags >= UndergroundFlags ? 1 : 0);

    public static CardColour Colour(bool hasPokedex, int level) => !hasPokedex ? CardColour.NoPokedex : (CardColour)(1 + Math.Clamp(level, 0, 5));

    /// <summary>The species the player has caught that count toward the National Pokédex's goal (Platinum's 493, less the eleven).</summary>
    public static int NationalCaught(IEnumerable<int> caughtNumbers) =>
        caughtNumbers.Count(n => n is >= 1 and <= 493 && !NotNeededNational.Contains(Data.PokemonDatabase.GetByDex(n)?.Name ?? ""));
}

/// <summary>
/// The trainer score of the Trainer Card's front (plan 06 · R12; <c>sTrainerScoreIncrements</c>): points for what
/// the player does, the ones this game can do so far. It stops at 99,999,999.
/// </summary>
public static class TrainerScore
{
    public const int Limit = 99_999_999;

    public const int WonWildBattle = 2, CaughtRegional = 2, CaughtNational = 3, WonTrainerBattle = 3,
        CaughtNewSpecies = 20, Badge = 30, HallOfFame = 35;

    /// <summary>A Poffin cooked alone (plan 06 · R14c; <c>TRAINER_SCORE_EVENT_UNK_12</c>).</summary>
    public const int CookedPoffin = 3;

    public static int Add(int score, int points) => (int)Math.Min(Limit, (long)score + points);
}
