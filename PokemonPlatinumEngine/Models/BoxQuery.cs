using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A place in the boxes: the box, and the place in it counted row by row.</summary>
public readonly record struct BoxPlace(int Box, int Slot);

/// <summary>What the boxes can be put in order by (plan 12 · Q26).</summary>
public enum BoxSort { Number, Level, Name, Type, Recent }

/// <summary>
/// What a search of the boxes asks for (plan 12 · Q26): each part null (or nothing) for any, and a Pokémon is found when
/// it has every part asked for. Names are the data's identifiers (a species', a move's, an ability's, an item's), never
/// the language's.
/// </summary>
public sealed record BoxFilter
{
    public string? Species { get; init; }
    public PokemonType? Type { get; init; }
    public string? Move { get; init; }
    public string? Ability { get; init; }
    public Nature? Nature { get; init; }

    /// <summary>The lowest and the highest level found; 0 for no bound.</summary>
    public int MinLevel { get; init; }
    public int MaxLevel { get; init; }

    /// <summary>What it holds: an item's name, or <see cref="Anything"/> and <see cref="Nothing"/>.</summary>
    public string? Item { get; init; }
    public Gender? Gender { get; init; }

    /// <summary>True for only shiny Pokémon; null for any.</summary>
    public bool? Shiny { get; init; }

    /// <summary>The marks a Pokémon must have set (all of them); none for any.</summary>
    public Markings Marks { get; init; }

    /// <summary>The held item's two words that aren't an item: holding something, and holding nothing.</summary>
    public const string Anything = "*", Nothing = "-";

    /// <summary>Whether it asks for nothing at all.</summary>
    public bool IsEmpty => this == new BoxFilter();
}

/// <summary>
/// The boxes' tools (plan 12 · Q26): a search of every box, a box or all of them put in order, and where several
/// Pokémon picked up together go down. GPU-free; <c>PCScreen</c> asks it. The team is never touched.
/// </summary>
public static class BoxQuery
{
    /// <summary>Whether a Pokémon has everything the filter asks for. An Egg is found only by a search that asks nothing.</summary>
    public static bool Matches(Pokemon p, BoxFilter f)
    {
        if (f.IsEmpty) return true;
        if (p.IsEgg) return false;
        if (f.Species != null && p.Species.Name != f.Species) return false;
        if (f.Type is { } type && !p.HasType(type)) return false;
        if (f.Move != null && !p.Moves.Any(m => m.Name == f.Move)) return false;
        if (f.Ability != null && p.AbilityName != f.Ability) return false;
        if (f.Nature is { } nature && p.Nature != nature) return false;
        if (f.MinLevel > 0 && p.Level < f.MinLevel) return false;
        if (f.MaxLevel > 0 && p.Level > f.MaxLevel) return false;
        if (f.Item == BoxFilter.Anything && p.HeldItem == null) return false;
        if (f.Item == BoxFilter.Nothing && p.HeldItem != null) return false;
        if (f.Item is { } item && item != BoxFilter.Anything && item != BoxFilter.Nothing && p.HeldItem?.Name != item) return false;
        if (f.Gender is { } gender && p.Gender != gender) return false;
        if (f.Shiny is { } shiny && p.IsShiny != shiny) return false;
        if (f.Marks != Markings.None && ((Markings)p.Marks & f.Marks) != f.Marks) return false;
        return true;
    }

    /// <summary>The places of every stored Pokémon the filter finds, box by box and place by place.</summary>
    public static IReadOnlyList<BoxPlace> Find(PcBoxes pc, BoxFilter filter)
    {
        var found = new List<BoxPlace>();
        for (int box = 0; box < pc.Boxes.Count; box++)
            for (int slot = 0; slot < PcBoxes.BoxSize; slot++)
                if (pc[box, slot] is { } p && Matches(p, filter)) found.Add(new BoxPlace(box, slot));
        return found;
    }

    /// <summary>
    /// The Pokémon in order: by the National Pokédex's number, the highest level first, by name as shown, by first type
    /// in the type chart's order, or the most recently met first; ties keep the order they had, Eggs last.
    /// </summary>
    public static List<Pokemon> Ordered(IEnumerable<Pokemon> pokemon, BoxSort by)
    {
        var list = pokemon.ToList();
        var eggs = list.Where(p => p.IsEgg).ToList();
        var rest = list.Where(p => !p.IsEgg);
        IEnumerable<Pokemon> ordered = by switch
        {
            BoxSort.Level => rest.OrderByDescending(p => p.Level).ThenBy(p => p.Species.DexNumber),
            BoxSort.Name => rest.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Species.DexNumber),
            BoxSort.Type => rest.OrderBy(p => (int)p.PrimaryType).ThenBy(p => p.SecondaryType is { } s ? (int)s : -1).ThenBy(p => p.Species.DexNumber),
            BoxSort.Recent => rest.OrderBy(p => p.MetDate == null).ThenByDescending(p => p.MetDate ?? DateTime.MinValue),
            _ => rest.OrderBy(p => p.Species.DexNumber).ThenByDescending(p => p.Level)
        };
        return ordered.Concat(eggs).ToList();
    }

    /// <summary>Puts one box in order, its Pokémon packed from its first place. The other boxes are left as they are.</summary>
    public static void Sort(PcBoxes pc, int box, BoxSort by)
    {
        var inBox = new List<Pokemon>();
        for (int slot = 0; slot < PcBoxes.BoxSize; slot++)
            if (pc[box, slot] is { } p) inBox.Add(p);
        var ordered = Ordered(inBox, by);
        for (int slot = 0; slot < PcBoxes.BoxSize; slot++) pc.Lay(box, slot, slot < ordered.Count ? ordered[slot] : null);
    }

    /// <summary>Puts every box in order at once: every stored Pokémon laid out from the first box's first place on. Boxes keep their names and wallpapers.</summary>
    public static void SortAll(PcBoxes pc, BoxSort by)
    {
        var ordered = Ordered(pc.All, by);
        int i = 0;
        for (int box = 0; box < pc.Boxes.Count; box++)
            for (int slot = 0; slot < PcBoxes.BoxSize; slot++, i++)
                pc.Lay(box, slot, i < ordered.Count ? ordered[i] : null);
    }

    /// <summary>
    /// Where <paramref name="count"/> Pokémon put down together in a box go: its empty places from <paramref name="from"/> on,
    /// round to its first, in order; null when the box hasn't room for every one of them (nothing goes down then).
    /// </summary>
    public static IReadOnlyList<int>? PlacesFor(PcBoxes pc, int box, int from, int count)
    {
        var places = new List<int>();
        for (int i = 0; i < PcBoxes.BoxSize && places.Count < count; i++)
        {
            int slot = (from + i) % PcBoxes.BoxSize;
            if (pc[box, slot] == null) places.Add(slot);
        }
        return places.Count == count ? places : null;
    }

    /// <summary>The places of a rectangle of a box, its corners two places of it, row by row.</summary>
    public static IReadOnlyList<int> Rectangle(int corner, int other)
    {
        int c0 = Math.Min(corner % PcBoxes.Columns, other % PcBoxes.Columns), c1 = Math.Max(corner % PcBoxes.Columns, other % PcBoxes.Columns);
        int r0 = Math.Min(corner / PcBoxes.Columns, other / PcBoxes.Columns), r1 = Math.Max(corner / PcBoxes.Columns, other / PcBoxes.Columns);
        var cells = new List<int>();
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++) cells.Add(r * PcBoxes.Columns + c);
        return cells;
    }
}
