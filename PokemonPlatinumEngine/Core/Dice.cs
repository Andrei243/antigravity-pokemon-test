using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Where the game's chance comes from when nobody hands a generator of their own: a new Pokémon's genes, a
/// battle's rolls, a wild encounter. It is chance, unless a tool has asked for a run that repeats
/// (<see cref="Seed"/>): the screenshot harness does, so that two runs draw the same pictures and a change in the
/// look can be found by comparing them.
/// </summary>
public static class Dice
{
    private static Random? seeded;

    /// <summary>
    /// Makes every roll from here on repeat, run after run; null gives chance back. For tools, on one thread:
    /// the tests leave it alone, because they run side by side.
    /// </summary>
    public static void Seed(int? seed) => seeded = seed is { } s ? new Random(s) : null;

    /// <summary>A generator for something that keeps its own (a battle, a map, the field).</summary>
#pragma warning disable RS0030 // the game's one source of unseeded chance
    public static Random New() => seeded != null ? new Random(seeded.Next()) : new Random();

    /// <summary>The generator for a single roll.</summary>
    public static Random Shared => seeded ?? Random.Shared;
#pragma warning restore RS0030
}
