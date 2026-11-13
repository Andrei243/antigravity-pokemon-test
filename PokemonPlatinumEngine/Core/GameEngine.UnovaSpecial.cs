using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Unova's shaking grass, rippling water, dust clouds and bridge shadows as the game plays them (plan 38 · EX11; rules in
/// <see cref="UnovaSpecialGrass"/>). Shaking grass and rippling water are met by the step's own roll
/// (<see cref="Map.RollWildEncounter"/>, asked through <see cref="EncounterMoment.SpecialDay"/>) and the rod
/// (<see cref="Map.Fish"/>); a dust cloud and a bridge's shadow are met here, on the step onto their tile, once a day each,
/// and what the player stands near is drawn with the field's own pieces. Nothing is drawn from the dice to find a patch.
/// </summary>
public partial class GameEngine
{
    /// <summary>How far from the player patches are shown, in tiles.</summary>
    private const int SpecialReach = 11;

    // The tiles whose dust or shadow has been met today (not saved: a day's clouds come back with the day, and a save loaded is a fresh sky)
    private readonly HashSet<(string Map, int X, int Y, uint Day)> metToday = new();

    // The patches near the player, found again when the player moves a tile, the place changes or the day does
    private readonly List<(SpecialKind Kind, int X, int Y)> nearSpecial = new();
    private (string Map, int X, int Y, uint Day, bool Water)? specialFor;
    private double nextSpecialStir;

    /// <summary>The day's number where the special tiles are in force (Unova's); null everywhere else, and then nothing is laid.</summary>
    internal uint? SpecialDayNow() => Here.Id == RegionDatabase.Unova ? flow.Encounters.DailyNumber : null;

    /// <summary>Finds the patches near the player again if anything they depend on moved, and returns them.</summary>
    internal IReadOnlyList<(SpecialKind Kind, int X, int Y)> SpecialsNear()
    {
        if (SpecialDayNow() is not { } day) { nearSpecial.Clear(); specialFor = null; return nearSpecial; }
        bool water = flow.Player.Mode == TravelMode.Surfing;
        var key = (flow.Map.Name, flow.Player.GridX, flow.Player.GridY, day, water);
        if (specialFor == key) return nearSpecial;
        specialFor = key;
        nearSpecial.Clear();
        for (int y = flow.Player.GridY - SpecialReach; y <= flow.Player.GridY + SpecialReach; y++)
            for (int x = flow.Player.GridX - SpecialReach; x <= flow.Player.GridX + SpecialReach; x++)
            {
                if (!flow.Map.InBounds(x, y)) continue;
                // The water ripples for whoever could ride it and for whoever watches from the bank; the land's patches for all
                if (UnovaSpecialGrass.KindAt(flow.Map, x, y, day, true) is { } ripple) nearSpecial.Add((ripple, x, y));
                else if (UnovaSpecialGrass.KindAt(flow.Map, x, y, day, false) is { } kind && !metToday.Contains((flow.Map.Name, x, y, day)))
                    nearSpecial.Add((kind, x, y));
            }
        return nearSpecial;
    }

    /// <summary>The patches near stir as the DexNav's rustle does, every <see cref="Graphics.FieldLife.SpecialEvery"/> seconds, and the grass is parted at each shaking one.</summary>
    private void TickUnovaSpecial()
    {
        var near = SpecialsNear();
        foreach (var (kind, x, y) in near)
            if (kind == SpecialKind.Shaking) world.Stirring.Add((x, y, true));
        var life = world.Life;
        if (near.Count == 0 || life.Now < nextSpecialStir) return;
        nextSpecialStir = life.Now + Graphics.FieldLife.SpecialEvery;
        foreach (var (kind, x, y) in near)
            switch (kind)
            {
                case SpecialKind.Shaking: life.Rustle(flow.Map, x, y, true, false); break;
                case SpecialKind.Rippling: life.WaterRipple(flow.Map, x, y); break;
                case SpecialKind.Dust: life.Dust(flow.Map, x, y); break;
                case SpecialKind.Shadow: life.Shadow(flow.Map, x, y); break;
            }
    }

    /// <summary>
    /// A step onto a dust cloud or a bridge's shadow (rules in <see cref="UnovaSpecialGrass"/>): a cloud gives a Gem or a Pokémon of the
    /// area's shaking-grass table, a shadow always a flying one of it; either goes for the day once met. True when a battle began.
    /// </summary>
    private bool UnovaSpecialStep()
    {
        if (SpecialDayNow() is not { } day || flow.Player.Mode == TravelMode.Surfing || WildEncountersOff) return false;
        int x = flow.Player.GridX, y = flow.Player.GridY;
        if (UnovaSpecialGrass.KindAt(flow.Map, x, y, day, false) is not { } kind || kind is SpecialKind.Shaking) return false;
        if (flow.Map.AreaAt(x, y) is not { } area || !metToday.Add((flow.Map.Name, x, y, day))) return false;
        specialFor = null;
        if (kind == SpecialKind.Dust && UnovaSpecialGrass.DustAt(area, day, x, y) is { Item: { } name } && ItemDatabase.Get(name) is { } gem)
        {
            flow.Bag.AddItem(gem, 1);
            ShowNotification(Text.Of("unovaSpecial.dust", ("item", gem.DisplayName)));
            return false;
        }
        var entry = kind == SpecialKind.Dust
            ? UnovaSpecialGrass.DustAt(area, day, x, y).Pokemon
            : Overworld.WildEncounterRules.Meet(area.ShakingGrassEncounters, false, flow.Player.Lead, fieldRandom);
        if (entry == null) return false;
        ShowNotification(Text.Of(kind == SpecialKind.Dust ? "unovaSpecial.dustPokemon" : "unovaSpecial.shadow"));
        MeetInTheField(entry);
        return true;
    }
}
