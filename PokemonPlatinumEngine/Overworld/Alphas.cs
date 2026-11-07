using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Alphas (plan 10 · F10; docs/mechanics/rulings.md, "Alphas"), GPU-free: a rare, larger Pokémon of the new zone's
/// grass, met only after the region's Hall of Fame and only in the areas whose overlay lists one (<c>alphas</c>,
/// <see cref="OverlayAlphas"/>: the post-game zone, none of Platinum's own areas). An alpha is one and a half times
/// its kind's size, is met at levels of the overlay's own (above the area's grass), has three perfect IVs and one
/// move it would not know yet, and cries lower. Nothing here draws from anywhere but the generator it is handed, and
/// nothing runs until an area lists an alpha and the Hall of Fame is entered, so no draw of any other meeting moves.
/// </summary>
public static class Alphas
{
    /// <summary>An alpha's size against its species' (1.5 times; the camera, the sprite and the field board take it).</summary>
    public const float Scale = 1.5f;

    /// <summary>A step in the tall grass of an area that lists alphas meets one time in this many (after the meeting is made).</summary>
    public const int OneIn = 100;

    /// <summary>How many of an alpha's IVs are made perfect.</summary>
    public const int PerfectIvs = 3;

    /// <summary>The levels above its own an alpha's extra move is looked for in.</summary>
    public const int MoveReach = 10;

    /// <summary>An alpha's cry is lower by this many semitones.</summary>
    public const float CrySemitones = -5f;

    /// <summary>Whether alphas are met in a region now: its Hall of Fame is entered.</summary>
    public static bool Open(Region? region, Story.StoryState story) => region != null && story.Has(region.StoryCompleteFlag);

    /// <summary>
    /// The row of an alpha for a tile of an area's grass: one time in <see cref="OneIn"/>, one of the area's list by
    /// an even draw; null for none. Draws nothing when the area lists none.
    /// </summary>
    public static WildEncounterEntry? Roll(IReadOnlyList<OursSlot> list, Random rng)
    {
        if (list.Count == 0 || rng.Next(OneIn) != 0) return null;
        var slot = list[rng.Next(list.Count)];
        return new WildEncounterEntry { SpeciesName = slot.Species, MinLevel = slot.MinLevel, MaxLevel = slot.MaxLevel, Weight = 1, Alpha = true };
    }

    /// <summary>
    /// What makes the Pokémon made for an alpha row one: its size, three of its IVs perfect (stats drawn among those
    /// not yet perfect) and one move of its line it would not know yet, the strongest within
    /// <see cref="MoveReach"/> levels, put in the empty place or the last one. Drawn after the Pokémon is made.
    /// </summary>
    public static void Apply(Pokemon p, Random rng)
    {
        p.Scale = Scale;
        var stats = new List<StatType> { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };
        for (int n = PerfectIvs; n > 0; n--)
        {
            var stat = stats[rng.Next(stats.Count)];
            stats.Remove(stat);
            switch (stat)
            {
                case StatType.HP: p.IvHP = 31; break;
                case StatType.Attack: p.IvAttack = 31; break;
                case StatType.Defense: p.IvDefense = 31; break;
                case StatType.SpAttack: p.IvSpAttack = 31; break;
                case StatType.SpDefense: p.IvSpDefense = 31; break;
                default: p.IvSpeed = 31; break;
            }
        }
        p.ReckonStats();
        if (ExtraMove(p) is { } move)
        {
            if (p.Moves.Count < 4) p.Moves.Add(new Move(move));
            else p.Moves[^1] = new Move(move);
        }
    }

    /// <summary>
    /// The strongest move of its level-up list that it does not know and that runs: among those it will learn within
    /// <see cref="MoveReach"/> levels, else among any other it does not know (one it passed over); null for none.
    /// </summary>
    public static MoveData? ExtraMove(Pokemon p)
    {
        MoveData? Strongest(IEnumerable<LearnableMove> from) =>
            from.Where(m => p.Moves.All(k => k.Data.Name != m.MoveName) && MoveDatabase.Runs(m.MoveName))
                .Select(m => MoveDatabase.Get(m.MoveName))
                .OrderByDescending(m => m.Power).ThenBy(m => m.Name, StringComparer.Ordinal)
                .FirstOrDefault();
        return Strongest(p.Learnset.Where(m => m.Level > p.Level && m.Level <= p.Level + MoveReach)) ?? Strongest(p.Learnset);
    }
}
