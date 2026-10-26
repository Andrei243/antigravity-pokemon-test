using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The levels a rematch is fought at in a region the player has gone past (plan 18 · W4b, L9; <c>docs/mechanics/rulings.md</c>,
/// "Rematches of a region gone past"). Coming back to an earlier region, its places and its trainers are met as they
/// were first met (Sinnoh's at Platinum's levels, a crossed region's by its own pairs); only its rematch teams
/// (<see cref="TrainerDatabase.IsRematchTeam"/>: its League's second teams, the Battleground's, the Vs. Seeker's rows
/// and the phone's) take the levels of the furthest region whose Hall of Fame is entered, by that region's own
/// <c>levels</c>, whose last pair is its post-game. Nothing changes while the furthest region is the rematch's own or one
/// before it, nor for a game begun anywhere but where the chain begins (the region begun in has no pairs to map by).
/// No drawing or input.
/// </summary>
public static class RematchLevels
{
    /// <summary>The region a trainer's team belongs to: the region whose folder its id begins with (<c>johto_</c>), else Sinnoh, whose ids have no prefix.</summary>
    public static Region RegionOfTeam(string id)
    {
        foreach (var region in RegionDatabase.All.Skip(1))
            if (id.StartsWith(region.Folder + "_", StringComparison.OrdinalIgnoreCase)) return region;
        return RegionDatabase.First;
    }

    /// <summary>The furthest region along the chain whose Hall of Fame is entered, or null for none.</summary>
    public static Region? Furthest(StoryState story) =>
        RegionDatabase.All.LastOrDefault(r => story.Has(r.StoryCompleteFlag));

    /// <summary>
    /// The levels a team is met at as a rematch now, or null where the region's usual levels stand (not a rematch's
    /// team; no Hall of Fame past its region; the furthest region the one begun in, or with no pairs).
    /// </summary>
    public static LevelMapping? For(string teamId, StoryState story, string? beganIn)
    {
        if (!TrainerDatabase.IsRematchTeam(teamId) || Furthest(story) is not { } furthest) return null;
        if (string.Equals(furthest.Id, beganIn, StringComparison.OrdinalIgnoreCase) || furthest.Levels is not { } levels) return null;
        var all = RegionDatabase.All;
        int own = IndexOf(RegionOfTeam(teamId)), far = IndexOf(furthest);
        return far > own ? levels : null;

        int IndexOf(Region r)
        {
            for (int k = 0; k < all.Count; k++)
                if (string.Equals(all[k].Id, r.Id, StringComparison.OrdinalIgnoreCase)) return k;
            return -1;
        }
    }
}
