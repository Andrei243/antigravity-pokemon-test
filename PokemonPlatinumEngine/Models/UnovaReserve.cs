using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>One of the reserve's grounds: a part of its map with Pokémon of its own and slots the objects bring.</summary>
public sealed record ReserveGround(string Key, int X, int Y, int Width, int Height,
    IReadOnlyList<(string Species, int Min, int Max)> Table, IReadOnlyList<SafariSlot> Slots)
{
    public bool Holds(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
    public (int X, int Y, int Width, int Height) Bounds => (X, Y, Width, Height);
}

/// <summary>
/// The Floccesy Reserve (plan 38 · EX9; <c>docs/mechanics/rulings.md</c>, "The Floccesy Reserve"): ours, since Black 2
/// has no Safari Zone. A warden at Floccesy Ranch takes a Champion of Unova to a gate of ours, where $500 buys thirty
/// Safari Balls and five hundred steps (<see cref="SafariGame"/>) in a zone of ours of three grounds, each with a table of
/// ours. The gate's ranger arranges a ground with EX8's objects (<see cref="SafariObjects"/>): the k-th of a ground's slots
/// for objects, open, takes slot k of its table. Levels are written in Black 2's own scale, as the world's are. Nothing here
/// draws from any chance. No drawing or input.
/// </summary>
public static class UnovaReserve
{
    public const string ZoneMap = "UnovaReserve";
    public const string GateMap = "UnovaReserve_Gate";

    /// <summary>The tile the gate puts a player down on in the zone, which no object is set down near.</summary>
    public const int ArrivalX = 20, ArrivalY = 30;

    /// <summary>The slots a ground's table has, met as often as one another, as HeartGold's Safari Zone's are.</summary>
    public const int TableSlots = 10;

    private static SafariSlot Slot(string species, int level, params (SafariObjectKind Kind, int Count)[] needs) =>
        new(SafariWay.Land, species, null, null, level, needs);

    /// <summary>The three grounds, in the ranger's order: the order <c>safari reserve</c> numbers them.</summary>
    public static IReadOnlyList<ReserveGround> Grounds { get; } = new ReserveGround[]
    {
        new("unova_reserve_meadow", 1, 14, 19, 17,
            new (string, int, int)[]
            {
                ("Herdier", 55, 57), ("Zebstrika", 56, 58), ("Deerling", 55, 57), ("Sawsbuck", 58, 60), ("Petilil", 55, 56),
                ("Cottonee", 55, 56), ("Minccino", 55, 57), ("Cinccino", 58, 60), ("Tranquill", 56, 58), ("Bouffalant", 58, 60)
            },
            new[]
            {
                Slot("Kangaskhan", 58, (SafariObjectKind.Plains, 3)),
                Slot("Tauros", 58, (SafariObjectKind.Plains, 6)),
                Slot("Miltank", 59, (SafariObjectKind.Plains, 10)),
                Slot("Smeargle", 57, (SafariObjectKind.Plains, 4), (SafariObjectKind.Water, 2)),
                Slot("Chansey", 60, (SafariObjectKind.Plains, 8), (SafariObjectKind.Water, 4)),
                Slot("Girafarig", 60, (SafariObjectKind.Plains, 14))
            }),
        new("unova_reserve_woods", 20, 14, 19, 17,
            new (string, int, int)[]
            {
                ("Swadloon", 55, 57), ("Whirlipede", 55, 57), ("Foongus", 55, 56), ("Amoonguss", 58, 60), ("Karrablast", 55, 57),
                ("Shelmet", 55, 57), ("Heracross", 58, 60), ("Gurdurr", 56, 58), ("Sewaddle", 55, 56), ("Venipede", 55, 56)
            },
            new[]
            {
                Slot("Aipom", 57, (SafariObjectKind.Forest, 3)),
                Slot("Scyther", 59, (SafariObjectKind.Forest, 6)),
                Slot("Slakoth", 58, (SafariObjectKind.Forest, 10)),
                Slot("Chatot", 57, (SafariObjectKind.Forest, 4), (SafariObjectKind.Plains, 3)),
                Slot("Tropius", 60, (SafariObjectKind.Forest, 8), (SafariObjectKind.Water, 4)),
                Slot("Kecleon", 60, (SafariObjectKind.Forest, 14))
            }),
        new("unova_reserve_crags", 1, 1, 38, 13,
            new (string, int, int)[]
            {
                ("Boldore", 55, 57), ("Krokorok", 56, 58), ("Rufflet", 55, 57), ("Vullaby", 55, 57), ("Mienfoo", 55, 57),
                ("Pawniard", 55, 57), ("Heatmor", 58, 60), ("Durant", 58, 60), ("Sandile", 55, 56), ("Druddigon", 59, 61)
            },
            new[]
            {
                Slot("Skarmory", 59, (SafariObjectKind.Peak, 4)),
                Slot("Gligar", 58, (SafariObjectKind.Peak, 6)),
                Slot("Larvitar", 57, (SafariObjectKind.Peak, 10)),
                Slot("Gible", 57, (SafariObjectKind.Peak, 8), (SafariObjectKind.Water, 2)),
                Slot("Bagon", 58, (SafariObjectKind.Peak, 14)),
                Slot("Beldum", 60, (SafariObjectKind.Peak, 20))
            })
    };

    public static ReserveGround? GroundAt(int x, int y) => Grounds.FirstOrDefault(g => g.Holds(x, y));

    public static ReserveGround? Ground(string key) => Grounds.FirstOrDefault(g => g.Key == key);

    /// <summary>
    /// The grass's table at a tile of the reserve at a moment: the ground's own ten, met alike, with the slots its objects
    /// open laid in; empty off every ground. A new list each time, so nothing a caller does moves the ground's own.
    /// </summary>
    public static List<WildEncounterEntry> Table(int x, int y, EncounterMoment? moment)
    {
        if (GroundAt(x, y) is not { } ground) return new();
        var table = ground.Table.Select(s => new WildEncounterEntry { SpeciesName = s.Species, MinLevel = s.Min, MaxLevel = s.Max, Weight = 10 }).ToList();
        if (moment?.SafariObjects is { } placed)
            SafariObjects.Lay(table, ground.Slots, SafariWay.Land, placed.In(ground.Key), moment.SafariDay, moment.Time);
        return table;
    }

    /// <summary>Every species the reserve gives, whatever stands in it (for the species checks).</summary>
    public static IEnumerable<(string Ground, string Species)> Species() =>
        Grounds.SelectMany(g => g.Table.Select(s => s.Species).Concat(g.Slots.Select(s => s.Species)).Select(s => (g.Key, s)));
}
