using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What the DexNav remembers (plan 10 · F11, F12; docs/mechanics/rulings.md, "The DexNav"): whether it is switched
/// on, each species' search level (one more for every Pokémon of it found and beaten or caught) and the chain of
/// finds of one species in a row. Saved as <see cref="SaveData.DexNav"/>, null while it has never been used.
/// </summary>
public sealed class DexNavState
{
    /// <summary>The highest search level a species reaches.</summary>
    public const int MostLevel = 999;

    /// <summary>The longest chain counted.</summary>
    public const int MostChain = 999;

    public bool On { get; set; }

    /// <summary>The search level of each species, by its name; a species never found is at 0.</summary>
    public SortedDictionary<string, int> Levels { get; } = new(StringComparer.Ordinal);

    /// <summary>The species the chain is of, and how many of it were found and beaten or caught in a row.</summary>
    public string? ChainSpecies { get; private set; }
    public int Chain { get; private set; }

    public int LevelOf(string species) => Levels.GetValueOrDefault(species);

    /// <summary>The chain a find of this species would count on: the chain, if it is of this species.</summary>
    public int ChainFor(string species) => ChainSpecies == species ? Chain : 0;

    /// <summary>A Pokémon the DexNav found was beaten or caught: its species' level rises and the chain goes on (or starts on it).</summary>
    public void Found(string species)
    {
        Levels[species] = Math.Min(MostLevel, LevelOf(species) + 1);
        Chain = ChainSpecies == species ? Math.Min(MostChain, Chain + 1) : 1;
        ChainSpecies = species;
    }

    /// <summary>The chain is broken: a find scared off, a battle run from or lost.</summary>
    public void Break()
    {
        Chain = 0;
        ChainSpecies = null;
    }

    public bool IsEmpty => !On && Levels.Count == 0 && Chain == 0;

    public DexNavSave ToSave() => new()
    {
        On = On,
        Levels = Levels.Count > 0 ? new Dictionary<string, int>(Levels) : null,
        ChainSpecies = ChainSpecies,
        Chain = Chain
    };

    public static DexNavState From(DexNavSave? save)
    {
        var state = new DexNavState();
        if (save == null) return state;
        state.On = save.On;
        foreach (var (species, level) in save.Levels ?? new()) if (level > 0) state.Levels[species] = Math.Min(MostLevel, level);
        if (save.ChainSpecies != null && save.Chain > 0)
        {
            state.ChainSpecies = save.ChainSpecies;
            state.Chain = Math.Min(MostChain, save.Chain);
        }
        return state;
    }
}

/// <summary>The DexNav as a save keeps it (plan 10 · F11, F12).</summary>
public sealed class DexNavSave
{
    public bool On { get; set; }
    public Dictionary<string, int>? Levels { get; set; }
    public string? ChainSpecies { get; set; }
    public int Chain { get; set; }
}

/// <summary>A species on the DexNav's HERE list: how it is met here, whether the Pokédex has seen and caught it, and its search level.</summary>
public sealed record DexNavEntry(string Species, HabitatWays Ways, bool Seen, bool Caught, int Level);

/// <summary>A Pokémon the DexNav found hiding in the grass: where the grass rustles and the row it was drawn from.</summary>
public sealed record Rustle(int X, int Y, WildEncounterEntry Entry);

/// <summary>What a step did to a rustle in sight.</summary>
public enum RustleAfterStep
{
    /// <summary>It is still there.</summary>
    Stays,
    /// <summary>The player stepped onto it: it is met.</summary>
    Reached,
    /// <summary>It heard the player come too fast, or saw them hop or ride, and fled: the chain is broken.</summary>
    Scared,
    /// <summary>The player walked out of its sight: it is forgotten, and the chain stands.</summary>
    Lost
}

/// <summary>
/// The DexNav's rules (plan 10 · F11, F12; docs/mechanics/rulings.md, "The DexNav"), GPU-free: the HERE list of what
/// lives where the player stands by the hour, where a rustle may land, what a step does to it, and the bonus a find
/// carries. The draws are the field's own generator's, the one the game's chance (<see cref="Dice"/>) seeds; nothing
/// here runs while the DexNav is off, so Platinum's grass is what it was.
/// </summary>
public static class DexNavRules
{
    /// <summary>The name of the key item.</summary>
    public const string Item = "DexNav";

    /// <summary>A step on foot with the DexNav on and nothing found finds something one time in this many.</summary>
    public const int FindOneIn = 12;

    /// <summary>The window round the player a rustle lands in and stays in sight within, in tiles across and down.</summary>
    public const int ReachX = 6, ReachY = 4;

    /// <summary>No rustle lands this close to the player, in tiles either way.</summary>
    public const int NotWithin = 2;

    /// <summary>Within this many tiles (either way) a walk is heard: only a creep comes closer.</summary>
    public const int HearsWithin = 3;

    /// <summary>Tries at a tile of grass for one find.</summary>
    public const int Tries = 8;

    /// <summary>
    /// The HERE list (plan 10 · F11): the species of the place the player stands in that are met now, in the region's
    /// order: those of the grass at this hour (<see cref="Habitats.GrassAt"/>), on the water and on the rods; with what
    /// the Pokédex knows of each and its search level.
    /// </summary>
    public static IReadOnlyList<DexNavEntry> Here(Habitats habitats, int tileX, int tileY, TimeOfDay time, Pokedex pokedex, DexNavState state)
    {
        var now = Habitats.GrassAt(time) | HabitatWays.Surfing | HabitatWays.Fishing;
        var seen = new Dictionary<string, HabitatWays>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var here in habitats.AtTile(tileX, tileY))
        {
            var ways = here.Ways & now;
            if (ways == HabitatWays.None) continue;
            if (!seen.ContainsKey(here.Species)) order.Add(here.Species);
            seen[here.Species] = seen.GetValueOrDefault(here.Species) | ways;
        }
        return order.Select(s =>
        {
            int dex = PokemonDatabase.Get(s)?.DexNumber ?? 0;
            return new DexNavEntry(s, seen[s], dex > 0 && pokedex.IsSeen(dex), dex > 0 && pokedex.IsCaught(dex), state.LevelOf(s));
        }).ToList();
    }

    /// <summary>Whether a step finds something: one time in <see cref="FindOneIn"/>, on the field's generator.</summary>
    public static bool Finds(Random rng) => rng.Next(FindOneIn) == 0;

    /// <summary>
    /// A rustle somewhere in the grass in sight: a tile drawn in the window round the player, on tall grass nobody
    /// stands in, not within <see cref="NotWithin"/>, with the row the place's table gives (never a roamer); null when
    /// <see cref="Tries"/> draws find none.
    /// </summary>
    /// <param name="draw">Draws a wild Pokémon's row for a tile of grass, as Sweet Scent draws one out; null where nothing lives.</param>
    public static Rustle? Place(Map map, int px, int py, Random rng, Func<int, int, WildEncounterEntry?> draw)
    {
        for (int i = 0; i < Tries; i++)
        {
            int x = px + rng.Next(-ReachX, ReachX + 1), y = py + rng.Next(-ReachY, ReachY + 1);
            if (Math.Abs(x - px) <= NotWithin && Math.Abs(y - py) <= NotWithin) continue;
            if (!FieldPokemon.CanStand(map, x, y, water: false)) continue;
            if (draw(x, y) is not { } entry || entry.Roamer != null) continue;
            return new Rustle(x, y, entry);
        }
        return null;
    }

    /// <summary>
    /// What a step the player has just taken does to a rustle: onto its tile, it is met; out of sight, forgotten; on a
    /// Bicycle or in a hop within sight, or at a run within sight, or at a walk within <see cref="HearsWithin"/>, it
    /// flees; a creep (<see cref="Pace.Sneak"/>) keeps it.
    /// </summary>
    public static RustleAfterStep AfterStep(Rustle rustle, int px, int py, Pace pace, TravelMode mode, bool hopped)
    {
        if ((px, py) == (rustle.X, rustle.Y)) return RustleAfterStep.Reached;
        int dx = Math.Abs(px - rustle.X), dy = Math.Abs(py - rustle.Y);
        if (dx > ReachX + 1 || dy > ReachY + 1) return RustleAfterStep.Lost;
        if (mode != TravelMode.OnFoot || hopped) return RustleAfterStep.Scared;
        if (pace is Pace.Fast or Pace.Fastest) return RustleAfterStep.Scared;
        if (pace == Pace.Walk && dx <= HearsWithin && dy <= HearsWithin) return RustleAfterStep.Scared;
        return RustleAfterStep.Stays;
    }

    /// <summary>
    /// How many of a find's IVs are made perfect: one from search level 10 and two from 50 (the search's own), and one
    /// from a chain of 5 and two from 10; three at most.
    /// </summary>
    public static int PerfectIvs(int level, int chain) =>
        Math.Min(3, (level >= 50 ? 2 : level >= 10 ? 1 : 0) + (chain >= 10 ? 2 : chain >= 5 ? 1 : 0));

    /// <summary>The chance in a hundred that a find has its hidden ability, by search level: none under 5, then 5, 10 from 25, 15 from 50, 20 from 100.</summary>
    public static int HiddenAbilityChance(int level) => level switch
    {
        >= 100 => 20,
        >= 50 => 15,
        >= 25 => 10,
        >= 5 => 5,
        _ => 0
    };

    /// <summary>
    /// The find's bonus on the Pokémon made for it: its perfect IVs, each a stat drawn among those not yet perfect,
    /// and under the modern rules its hidden ability by the level's chance; Platinum's rules give the IVs alone, as
    /// Platinum has no hidden abilities. Drawn on the field's generator, after the Pokémon is made.
    /// </summary>
    public static void Bonus(Pokemon p, int level, int chain, Ruleset rules, Random rng)
    {
        var stats = new List<StatType> { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };
        for (int n = PerfectIvs(level, chain); n > 0 && stats.Count > 0; n--)
        {
            var stat = stats[rng.Next(stats.Count)];
            stats.Remove(stat);
            SetIv(p, stat, 31);
        }
        p.ReckonStats();
        if (rules.Preset == RulesPreset.Modern && p.HiddenAbilityName is { } hidden && HiddenAbilityChance(level) is > 0 and var chance
            && rng.Next(100) < chance)
            p.AbilityName = hidden;
    }

    private static void SetIv(Pokemon p, StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.HP: p.IvHP = value; break;
            case StatType.Attack: p.IvAttack = value; break;
            case StatType.Defense: p.IvDefense = value; break;
            case StatType.SpAttack: p.IvSpAttack = value; break;
            case StatType.SpDefense: p.IvSpDefense = value; break;
            default: p.IvSpeed = value; break;
        }
    }
}
