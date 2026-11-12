using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A trainer's team lifted to a level of the player's journey (plan 41 · EG3, which the circuit, EG4, and the gathering,
/// EG9, use too; <c>docs/mechanics/rulings.md</c>, "Pennant Hall"). The team is a copy, its highest Pokémon at the level
/// asked and the others the same number of levels below it as before (its spread kept), each taking the stages its new
/// level allows (<see cref="Trainer.Relevelled"/> with <c>grow</c>). A lifted copy says so
/// (<see cref="Trainer.LevelsKept"/>), and the battle relevels it no more for the region it is fought in. The trainer of the
/// map, or of the data, keeps its team as it was. No drawing or input.
/// </summary>
public static class LevelLift
{
    /// <summary>The post-game level a game with no region past Sinnoh finished stands at: Platinum's own after its Hall of Fame (the Battleground's best teams).</summary>
    public const int SinnohPostGame = 75;

    /// <summary>
    /// A copy of <paramref name="trainer"/> whose best Pokémon stands at <paramref name="top"/> (1 to the modern cap), the
    /// rest kept the same distance under it, never under level 1. Marked as kept, so no region's mapping is laid on it again.
    /// </summary>
    public static Trainer To(Trainer trainer, int top)
    {
        top = Math.Clamp(top, 1, Ruleset.HighestLevel);
        int best = trainer.Party.Members.Count == 0 ? top : trainer.Party.Members.Max(p => p.Level);
        int by = top - best;
        var lifted = trainer.Relevelled(level => Math.Clamp(level + by, 1, Ruleset.HighestLevel), grow: true);
        lifted.LevelsKept = true;
        return lifted;
    }

    /// <summary>
    /// The level the furthest finished region of the chain is at once its story is done: the last pair of its region file's
    /// <c>levels</c> (Kanto 120, Johto 156...), <see cref="SinnohPostGame"/> for Sinnoh alone or a region with no pairs, and
    /// 0 with no Hall of Fame entered.
    /// </summary>
    public static int PostGameLevel(StoryState story)
    {
        if (RematchLevels.Furthest(story) is not { } furthest) return 0;
        return furthest.Levels is { Points.Count: > 0 } levels ? levels.Points[^1].To : SinnohPostGame;
    }
}
