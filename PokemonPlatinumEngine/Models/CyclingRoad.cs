using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Seaside Cycling Road's timed run (plan 21 · H12, part k; Emerald's <c>field_specials.c</c>,
/// <c>DetermineCyclingRoadResults</c> and <c>RecordCyclingRoadResults</c>). GPU-free. A run begins as a rider on the Mach
/// Bike leaves the north gate onto Route 110 (<c>VAR_CYCLING_CHALLENGE_STATE</c> 1, then 2 while it is timed) and is
/// judged at the south end: its time in frames, sixty a second, and its collisions, counted to 100, make a mark of
/// nought to ten; the best time is kept with its collisions (<c>VAR_CYCLING_ROAD_RECORD_*</c>).
/// </summary>
public static class CyclingRoad
{
    public const string StateVariable = "VAR_CYCLING_CHALLENGE_STATE";
    public const string RecordFrames = "VAR_CYCLING_ROAD_RECORD_FRAMES";
    public const string RecordCollisions = "VAR_CYCLING_ROAD_RECORD_COLLISIONS";
    public const string LastFrames = "VAR_CYCLING_ROAD_LAST_FRAMES";
    public const string LastCollisions = "VAR_CYCLING_ROAD_LAST_COLLISIONS";

    /// <summary>The most collisions counted (Emerald's <c>gBikeCollisions &lt; 100</c>).</summary>
    public const int MostCollisions = 100;

    /// <summary>Emerald's mark: up to five for few collisions and up to five for a short time.</summary>
    public static int Mark(int frames, int collisions)
    {
        int mark = collisions == 0 ? 5 : collisions < 4 ? 4 : collisions < 10 ? 3 : collisions < 20 ? 2 : collisions < 100 ? 1 : 0;
        int seconds = frames / 60;
        mark += seconds <= 10 ? 5 : seconds <= 15 ? 4 : seconds <= 20 ? 3 : seconds <= 40 ? 2 : seconds < 60 ? 1 : 0;
        return mark;
    }

    /// <summary>
    /// The run just ended judged and kept (<c>cyclingroad finish</c>): the mark, and the best time with its collisions
    /// where it is faster than the last best or there was none.
    /// </summary>
    public static int Finish(StoryState story, int frames, int collisions)
    {
        collisions = Math.Min(collisions, MostCollisions);
        story.SetVar(LastFrames, frames);
        story.SetVar(LastCollisions, collisions);
        int best = story.Var(RecordFrames);
        if (best == 0 || best > frames)
        {
            story.SetVar(RecordFrames, Math.Max(1, frames));
            story.SetVar(RecordCollisions, collisions);
        }
        // The run is over: the end's triggers ask for 2 and wait for the next run from the north gate
        story.SetVar(StateVariable, 3);
        return Mark(frames, collisions);
    }

    /// <summary>A time as the result's line says it: seconds to the hundredth, or a minute or more.</summary>
    public static string Time(int frames) => frames < 3600
        ? Text.Of("cyclingRoad.seconds", ("time", $"{frames / 60}.{frames % 60 * 100 / 60:00}"))
        : Text.Of("cyclingRoad.minute");

    /// <summary>Collisions as the line says them: so many times, or 99 and more.</summary>
    public static string Collisions(int collisions) => collisions < MostCollisions
        ? Text.Of("cyclingRoad.times", ("count", collisions))
        : Text.Of("cyclingRoad.many");
}
