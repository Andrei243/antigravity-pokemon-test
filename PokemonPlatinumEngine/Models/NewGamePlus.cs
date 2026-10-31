using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// New Game+ (plan 13 · V11; <c>docs/mechanics/rulings.md</c>, "New Game+"): what a finished save carries into a new
/// journey and what it leaves. GPU-free, and it reads the save as plain data, so the title can ask it of a card without
/// making a single Pokémon. The save itself is never changed: the old slot stays as it was, and the new game takes a
/// slot of its own.
/// <list type="bullet">
/// <item>Carried: the Pokédex's seen, caught and shiny caught and the looks seen; the PC's boxes as they were (the
/// National Pokédex's hundred kept); the team and whatever was left at the Day Care, put into the boxes; half the money
/// (never less than the 3,000 every game begins with); the Trainer Card's number and its hidden half, and the count of
/// Pokémon ids, so every carried Pokémon is still the player's own and no id is given twice; the run's rules, a step
/// harder with trainers scaled to the team; the clears, one more.</item>
/// <item>Left: the story (a fresh <see cref="Story.StoryState"/>, so <c>StoryMigration</c> is untouched), the bag and
/// every key item (the story gives them again), the National Pokédex itself and the diplomas (the professor gives the
/// one again and the counts earn the other), the Hall of Fame, the Journal, the counters, the medals, the Frontier, the
/// Underground, the Villa, the contests and every other record, and Pal Park's six who never came out of the park.</item>
/// </list>
/// </summary>
public static class NewGamePlus
{
    /// <summary>The money a new game begins with; New Game+ never begins with less.</summary>
    public const int StartingMoney = 3000;

    /// <summary>The region a save has finished, the one it began in first; null when it has finished none.</summary>
    public static Region? FinishedRegion(SaveData? save)
    {
        if (save == null) return null;
        var flags = new HashSet<string>(save.StoryFlags, StringComparer.Ordinal);
        var began = RegionDatabase.Get(save.BeganInOrHere());
        if (began != null && flags.Contains(began.StoryCompleteFlag)) return began;
        return RegionDatabase.All.FirstOrDefault(r => flags.Contains(r.StoryCompleteFlag));
    }

    /// <summary>Whether a save has finished a region's story: its Hall of Fame flag is set.</summary>
    public static bool IsFinished(SaveData? save) => FinishedRegion(save) != null;

    /// <summary>The Pokémon that go into the boxes: the team, then the Day Care's (an Egg it found stays behind with the couple).</summary>
    public static IReadOnlyList<SavedPokemonData> ToBox(SaveData save) =>
        save.Party.Concat(save.DayCare?.Places.Select(p => p?.Pokemon).OfType<SavedPokemonData>() ?? Enumerable.Empty<SavedPokemonData>()).ToList();

    /// <summary>How many boxes the carried PC has: the save's, or the National Pokédex's hundred where the save had the National Pokédex.</summary>
    public static int BoxCount(SaveData save)
    {
        int count = save.Boxes?.Boxes.Count ?? PcBoxes.BoxCount;
        if (save.NationalPokedex) count = Math.Max(count, PcBoxes.NationalBoxCount);
        return Math.Clamp(count, PcBoxes.BoxCount, PcBoxes.NationalBoxCount);
    }

    /// <summary>How many Pokémon the carried PC already holds, counted from the save.</summary>
    public static int Boxed(SaveData save) => save.Boxes?.Boxes.Sum(b => b.Pokemon.Count(p => p.Slot is >= 0 and < PcBoxes.BoxSize)) ?? save.BoxStorage.Count;

    /// <summary>Whether the boxes can take the team and the Day Care's: New Game+ is offered only then, since nothing is ever left behind.</summary>
    public static bool HasRoom(SaveData save) => BoxCount(save) * PcBoxes.BoxSize - Boxed(save) >= ToBox(save).Count;

    /// <summary>Whether the title offers New Game+ for this save: a finished region and room for the team.</summary>
    public static bool Offered(SaveData? save) => save != null && IsFinished(save) && HasRoom(save);

    /// <summary>The difficulty a step up: Easy to Normal, Normal to Challenge, Challenge stays.</summary>
    public static Difficulty StepUp(Difficulty d) => d switch
    {
        Difficulty.Easy => Difficulty.Normal,
        _ => Difficulty.Challenge
    };

    /// <summary>The run the new journey is played by: the save's own, a step harder, with trainers scaled to the team.</summary>
    public static RunRules Raised(RunRules? run) =>
        (run ?? RunRules.Normal) with { Difficulty = StepUp((run ?? RunRules.Normal).Difficulty), ScaleTrainers = true };

    /// <summary>The money carried: half, and never less than a new game's.</summary>
    public static int Money(SaveData save) => Math.Max(StartingMoney, save.Money / 2);

    /// <summary>The clears the new journey counts: the save's, and the one it just finished.</summary>
    public static int Clears(SaveData save) => (save.RunState?.Clears ?? 0) + 1;

    /// <summary>
    /// The save the title offers New Game+ for among its slots: of those offered, the one played longest (the lowest slot
    /// on a tie). Null when none is offered.
    /// </summary>
    public static T? Choose<T>(IEnumerable<T> slots, Func<T, int> number, Func<T, SaveData?> save) where T : class =>
        slots.Where(s => Offered(save(s))).OrderByDescending(s => save(s)!.PlayTimeSeconds).ThenBy(number).FirstOrDefault();
}
