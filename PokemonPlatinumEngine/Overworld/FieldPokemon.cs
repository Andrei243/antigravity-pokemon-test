using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Wild Pokémon in sight (plan 10 · F5; style guide, "Wild Pokémon in sight"; <c>docs/mechanics/rulings.md</c>, "Wild
/// Pokémon in sight"): with the options' WILD POKéMON row at VISIBLE, up to <see cref="MostInView"/> wild Pokémon stand
/// in the tall grass and on the water round the player, each drawn from the place's own table where it stands (the
/// same slot, level, nature and gender rules a step in the grass would have met it by) and made at once, so the
/// battle a touch starts is with the Pokémon seen. They wander a tile at a time near where they came out and are gone
/// once out of reach. No drawing or input: every draw is on the generator handed in, and what the field is (who
/// stands where, what a tile is) is asked of the map.
/// </summary>
public sealed class FieldPokemon
{
    /// <summary>The most wild Pokémon in sight at once.</summary>
    public const int MostInView = 6;

    /// <summary>The most in sight at once in the tall grass of a place with an outbreak today (plan 38 · EX1, <see cref="Outbreaks.MostInOutbreak"/>).</summary>
    public const int MostInOutbreak = Outbreaks.MostInOutbreak;

    /// <summary>How far, in tiles, one wanders from where it came out.</summary>
    public const int Roam = 3;

    /// <summary>
    /// The window round the player a wild Pokémon comes out in and stays in, in tiles across and down: about what the
    /// field's camera shows. One further than this is gone.
    /// </summary>
    public const int ReachX = 11, ReachY = 8;

    /// <summary>Nobody comes out this close to the player, in tiles either way.</summary>
    public const int NotWithin = 2;

    /// <summary>Seconds between tries at bringing one out while fewer than the most are in sight.</summary>
    public const float SpawnEvery = 0.75f;

    /// <summary>Tiles a wild Pokémon walks a second.</summary>
    public const float WalkPace = 3f;

    /// <summary>One in sight: the thing of the map it is drawn as, the Pokémon made for it and where it came from.</summary>
    public sealed class Wild
    {
        public required NPC Body { get; init; }
        public required Pokemon Pokemon { get; init; }
        public required WildEncounterEntry Entry { get; init; }
        public bool Water { get; init; }
        public int HomeX { get; init; }
        public int HomeY { get; init; }

        /// <summary>Seconds until it next thinks of a step.</summary>
        public float Wait { get; set; }

        internal NpcWalk? Walk;
    }

    private readonly List<Wild> all = new();
    private float spawnWait;
    private int made;

    /// <summary>Those in sight now.</summary>
    public IReadOnlyList<Wild> All => all;

    /// <summary>The map they stand on: they belong to it, and coming to another sends them all away.</summary>
    public Map? Map { get; private set; }

    /// <summary>The one dressed as <paramref name="body"/>, if it is one of them.</summary>
    public Wild? Of(NPC body)
    {
        foreach (var w in all) if (w.Body == body) return w;
        return null;
    }

    /// <summary>Whether a wild Pokémon may stand on a tile: the tall grass on land, open water on the water; nobody there.</summary>
    public static bool CanStand(Map map, int x, int y, bool water)
    {
        if (!map.InBounds(x, y) || map.GetNpcAt(x, y) != null) return false;
        if (water) return TileBehaviors.IsSurfable(map.BehaviourAt(x, y));
        return map.IsTallGrass(x, y) && map.IsWalkable(x, y);
    }

    /// <summary>
    /// A frame: those out of reach go, one may come out, and each moves on its step or thinks of the next. Returns the
    /// one that walked into the player, which is met at once; null when nobody did.
    /// </summary>
    /// <param name="draw">Draws a wild Pokémon's row for a tile (land or water), as Sweet Scent draws one out; null where nothing lives.</param>
    /// <param name="make">Makes the Pokémon of a row, as the grass would have.</param>
    /// <param name="put">Puts a body on the map, or (false) takes one off it.</param>
    /// <param name="most">How many may stand in sight at once: <see cref="MostInView"/>, or <see cref="Outbreaks.MostInOutbreak"/> in the place of a day's outbreak (plan 38 · EX1).</param>
    public Wild? Update(float dt, Map map, int px, int py, Random rng, Func<int, int, bool, WildEncounterEntry?> draw,
        Func<WildEncounterEntry, Pokemon> make, Action<NPC, bool> put, int most = MostInView)
    {
        if (Map != map) Clear(put);
        Map = map;

        // Those out of reach are gone, as their chunk leaves the view
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var w = all[i];
            if (Math.Abs(w.Body.GridX - px) <= ReachX + 1 && Math.Abs(w.Body.GridY - py) <= ReachY + 1) continue;
            w.Walk?.Finish();
            put(w.Body, false);
            all.RemoveAt(i);
        }

        // One may come out, at a tile drawn in the window round the player
        if ((spawnWait -= dt) <= 0f)
        {
            spawnWait = SpawnEvery;
            if (all.Count < most) TrySpawn(map, px, py, rng, draw, make, put);
        }

        // Each walks the step it is on, or thinks of the next
        Wild? touched = null;
        foreach (var w in all)
        {
            if (w.Walk != null)
            {
                w.Walk.Update(dt);
                if (w.Walk.IsDone) w.Walk = null;
                continue;
            }
            if ((w.Wait -= dt) > 0f) continue;
            w.Wait = 1f + (float)rng.NextDouble() * 2f;
            var dir = (Direction)rng.Next(4);
            var (dx, dy) = FieldMovement.Delta(dir);
            int nx = w.Body.GridX + dx, ny = w.Body.GridY + dy;
            w.Body.Facing = dir;
            // Walking into the player is meeting them
            if ((nx, ny) == (px, py))
            {
                touched ??= w;
                continue;
            }
            if (Math.Abs(nx - w.HomeX) > Roam || Math.Abs(ny - w.HomeY) > Roam || !CanStand(map, nx, ny, w.Water)) continue;
            w.Walk = new NpcWalk(w.Body, new[] { dir }, WalkPace);
        }
        return touched;
    }

    private void TrySpawn(Map map, int px, int py, Random rng, Func<int, int, bool, WildEncounterEntry?> draw,
        Func<WildEncounterEntry, Pokemon> make, Action<NPC, bool> put)
    {
        int x = px + rng.Next(-ReachX, ReachX + 1), y = py + rng.Next(-ReachY, ReachY + 1);
        if (Math.Abs(x - px) <= NotWithin && Math.Abs(y - py) <= NotWithin) return;
        bool water = map.InBounds(x, y) && TileBehaviors.IsSurfable(map.BehaviourAt(x, y));
        if (!CanStand(map, x, y, water) || draw(x, y, water) is not { } entry) return;
        // A roamer is never seen standing about: it is met only as it always was
        if (entry.Roamer != null) return;
        var pokemon = make(entry);
        var body = new NPC
        {
            Key = $"wild_in_sight_{made++}",
            GridX = x,
            GridY = y,
            Facing = (Direction)rng.Next(4)
        };
        Follower.Dress(body, pokemon);
        put(body, true);
        all.Add(new Wild { Body = body, Pokemon = pokemon, Entry = entry, Water = water, HomeX = x, HomeY = y, Wait = 1f + (float)rng.NextDouble() * 2f });
    }

    /// <summary>The one dressed as <paramref name="body"/> is met: it leaves the field.</summary>
    public void Remove(Wild wild, Action<NPC, bool> put)
    {
        wild.Walk?.Finish();
        put(wild.Body, false);
        all.Remove(wild);
    }

    /// <summary>All of them go: another map, the row turned to HIDDEN.</summary>
    public void Clear(Action<NPC, bool> put)
    {
        foreach (var w in all)
        {
            w.Walk?.Finish();
            put(w.Body, false);
        }
        all.Clear();
        Map = null;
    }

    /// <summary>
    /// Whether the player meets one from behind: walking into it as it faces the way the player faces, which is a back
    /// strike under the later rules (<see cref="Data.Ruleset.BackStrikes"/>).
    /// </summary>
    public static bool FromBehind(Direction playerFacing, Direction wildFacing) => playerFacing == wildFacing;
}
