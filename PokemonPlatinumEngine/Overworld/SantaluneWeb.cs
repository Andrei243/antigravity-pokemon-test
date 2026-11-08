using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Viola's Gym in Santalune City (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms, its water and the
/// Mamoswine ride"): X and Y's giant web, as it is publicly described (a web of threads over the hall, followed by its
/// drops of water), in a form of our own. The hall between the door's floor and Viola's is the dark with silk strung
/// over it: two rings run across it east and west and always hold; between the floors and the rings run radial strands
/// north and south in two sets, A and B, and a strand holds only while the dew lies on it. At each end of a ring is a
/// knot: whoever steps onto a knot shakes the web, and the dew runs from one set to the other. Laid out afresh (the dew
/// on A) as the player comes in. The door's floor reaches the first ring by A, the first ring the second by B, and the
/// second Viola's floor by A, so the way to her shakes the web twice. Nobody is stranded: the dew moves only while its
/// shaker stands on a knot, which is on a ring, and a ring always holds. GPU-free.
/// </summary>
public sealed class SantaluneWeb : GymPuzzle
{
    public const string PuzzleName = "SantaluneWeb";

    public override string Name => PuzzleName;

    /// <summary>What a tile of the web is: a ring (always holds), a radial strand of set A or B, or a knot (a ring's end, which shakes the web).</summary>
    public enum Strand { Ring, A, B, Knot }

    /// <summary>The rows of the two rings (the second nearer Viola), each running from column 2 to 10.</summary>
    public const int FirstRing = 10, SecondRing = 7;

    /// <summary>The web, tile by tile: everything over the dark that can ever be walked.</summary>
    public static readonly IReadOnlyDictionary<(int X, int Y), Strand> Web = Lay();

    private static Dictionary<(int X, int Y), Strand> Lay()
    {
        var web = new Dictionary<(int X, int Y), Strand>();
        foreach (int row in new[] { FirstRing, SecondRing })
        {
            for (int x = 2; x <= 10; x++) web[(x, row)] = Strand.Ring;
            web[(1, row)] = Strand.Knot;
            web[(11, row)] = Strand.Knot;
        }
        // From Viola's floor down to the second ring: set A
        web[(3, 6)] = Strand.A;
        web[(9, 6)] = Strand.A;
        // Between the rings: set B
        foreach (int x in new[] { 4, 8 })
            for (int y = SecondRing + 1; y < FirstRing; y++) web[(x, y)] = Strand.B;
        // From the first ring down to the door's floor: set A at either side, and set B straight up from the door
        foreach (int y in new[] { 11, 12 })
        {
            web[(3, y)] = Strand.A;
            web[(9, y)] = Strand.A;
            web[(6, y)] = Strand.B;
        }
        return web;
    }

    /// <summary>Whether the dew lies on set B now (on set A when not).</summary>
    public bool DewOnB { get; set; }

    /// <summary>How many times the web has been shaken or laid out since the game began (the renderer follows it).</summary>
    public int Shakes { get; private set; }

    public static bool IsKnot(int x, int y) => Web.TryGetValue((x, y), out var s) && s == Strand.Knot;

    /// <summary>Whether a tile of the web holds now: a ring or a knot always, a radial strand while the dew lies on its set.</summary>
    public bool Holds(int x, int y) => Web.TryGetValue((x, y), out var s) && s switch
    {
        Strand.A => !DewOnB,
        Strand.B => DewOnB,
        _ => true
    };

    /// <summary>The dew on set A, as the hall is laid out.</summary>
    public void Reset()
    {
        DewOnB = false;
        Shakes++;
    }

    /// <summary>Shakes the web: the dew runs to the other set.</summary>
    public void Shake()
    {
        DewOnB = !DewOnB;
        Shakes++;
    }

    /// <summary>The strands are open in the room's file; a dry one is closed by <see cref="Refuses"/>.</summary>
    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>A strand with no dew on it gives under a step: it is the dark, as a turntable's missing arm is.</summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat) => Web.ContainsKey((x, y)) && !Holds(x, y);

    /// <summary>A step onto a knot shakes the web as it is taken.</summary>
    public override void Crossed(Map map, int x, int y, Direction dir)
    {
        if (IsKnot(x, y)) Shake();
    }
}
