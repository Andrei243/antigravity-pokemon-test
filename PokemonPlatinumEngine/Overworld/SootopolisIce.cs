using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Sootopolis Gym's floor of thin ice (plan 21 · H8, part c; Emerald's <c>SootopolisGymIcePerStepCallback</c> and the
/// room's own scripts, <c>SootopolisCity_Gym_1F</c>): three fields of thin ice, one above another, each below a flight of
/// stairs glazed over with ice that slides whoever climbs it back down (the stairs' behaviour is Emerald's
/// <c>MB_SLIDE_SOUTH</c>). A step onto thin ice cracks it and counts (<c>VAR_ICE_STEP_COUNT</c>, one as the room is come
/// into); a step onto ice already cracked breaks through it, and the player falls to the basement below, onto the same
/// tile (<c>warphole</c>), with the count at nought. As the count reaches 8, 28 and 67 (every tile of the first field, of
/// the first two, of all three) the stairs above the field come clear (the original adds one to the count as it opens
/// them, so they open once), and the stairs once clear are plain stairs. Nothing is remembered once the room is left: the
/// original sets the count to one on every arrival, and the cracks are its temporary flags. GPU-free: the engine tells it
/// each tile the player comes onto (<see cref="StepOn"/>) and does what comes of it, and the room's pieces draw the cracks
/// and the glazed stairs from <see cref="Cracked"/> and <see cref="Glazed"/>.
/// </summary>
public sealed class SootopolisIce : GymPuzzle
{
    public const string PuzzleName = "SootopolisIce";

    public override string Name => PuzzleName;

    /// <summary>The map the floor's holes drop into.</summary>
    public const string Basement = "SootopolisCity_Gym_B1F";

    /// <summary>The counts at which each flight comes clear, lowest first (Emerald's <c>OnFrame</c> table).</summary>
    public static readonly int[] Thresholds = { 8, 28, 67 };

    /// <summary>Each flight's two tiles, lowest flight first (Emerald's <c>CheckSetStairMetatiles</c>).</summary>
    public static readonly (int X, int Y)[][] Flights =
    {
        new[] { (8, 15), (8, 16) },
        new[] { (8, 10), (8, 11) },
        new[] { (8, 4), (8, 5) }
    };

    /// <summary>What a step onto a tile came to.</summary>
    public enum Outcome { Nothing, Cracked, Opened, Fell }

    private readonly HashSet<(int X, int Y)> cracked = new();
    private int applied = -1;

    /// <summary>The count of ice stepped on (<c>VAR_ICE_STEP_COUNT</c>): one as the room is come into, nought after a fall.</summary>
    public int Count { get; private set; } = 1;

    /// <summary>How many flights are clear: none under 8, the first under 28, two under 67, all three from then on.</summary>
    public int FlightsOpen => Count < Thresholds[0] ? 0 : Count < Thresholds[1] ? 1 : Count < Thresholds[2] ? 2 : 3;

    /// <summary>The tiles of thin ice cracked so far.</summary>
    public IReadOnlyCollection<(int X, int Y)> Cracked => cracked;

    /// <summary>Whether a tile of the floor is cracked.</summary>
    public bool IsCracked(int x, int y) => cracked.Contains((x, y));

    /// <summary>The tiles of the flights still glazed over with ice.</summary>
    public IEnumerable<(int X, int Y)> Glazed
    {
        get
        {
            for (int f = FlightsOpen; f < Flights.Length; f++)
                foreach (var tile in Flights[f]) yield return tile;
        }
    }

    /// <summary>
    /// The player has come onto a tile: thin ice cracks and counts, opening a flight at its threshold; cracked ice gives
    /// way. Anything else is nothing.
    /// </summary>
    public Outcome StepOn(Map map, int x, int y)
    {
        if (map.BehaviourAt(x, y) != TileBehavior.ThinIce) return Outcome.Nothing;
        if (!cracked.Add((x, y)))
        {
            Count = 0;
            return Outcome.Fell;
        }
        Count++;
        if (Array.IndexOf(Thresholds, Count) < 0) return Outcome.Cracked;
        Count++;
        return Outcome.Opened;
    }

    /// <summary>A flight still glazed slides whoever climbs it back down; a clear one is plain stairs.</summary>
    public override void Apply(Map map, StoryState story)
    {
        int open = FlightsOpen;
        if (open == applied) return;
        applied = open;
        for (int f = 0; f < Flights.Length; f++)
            foreach (var (x, y) in Flights[f])
                map.SetBehaviour(x, y, f < open ? TileBehavior.None : TileBehavior.SlideSouth);
    }

    /// <summary>The room is come into: the ice whole again, the count at one, every flight glazed.</summary>
    public override void Arrive(Map map, StoryState story, Random rng)
    {
        cracked.Clear();
        Count = 1;
        applied = -1;
    }
}
