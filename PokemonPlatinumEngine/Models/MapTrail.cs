using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>One cell the player passed through, and the way they faced as they came into it (null: by a door or by air).</summary>
public readonly record struct TrailStep(int X, int Y, Direction? Facing);

/// <summary>
/// The cells of the region's map the player last passed through (plan 01 · M12; the original's
/// <c>OverworldMapHistory</c>, <c>src/overworld_map_history.c</c>): a ring of six, written as the player walks from
/// one cell of the overworld into the next and as a warp or a flight brings them onto it. The Town Map shows the
/// five before the newest as marks along the way come (<see cref="Recent"/>). It is the save's. No drawing or input.
/// </summary>
public sealed class MapTrail
{
    /// <summary>How many cells are kept (<c>OVERWORLD_MAP_HISTORY_LENGTH</c>).</summary>
    public const int Length = 6;

    /// <summary>How many of them the Town Map shows (<c>TOWN_MAP_HISTORY_LENGTH</c>): all but the newest, where the player is.</summary>
    public const int Shown = Length - 1;

    private readonly (int X, int Y, Direction? Facing, bool Set)[] items = new (int, int, Direction?, bool)[Length];
    private int pointer;
    private (int X, int Y) current;

    /// <summary>Forgets every cell (<c>OverworldMapHistory_Clear</c>): a new game's.</summary>
    public void Clear()
    {
        Array.Clear(items);
        pointer = 0;
        current = (0, 0);
    }

    /// <summary>
    /// The player stands in a cell of the overworld, facing a way (<c>OverworldMapHistory_Push</c>, asked every
    /// frame by the original and here at every step): written down when the cell is another than the last one.
    /// </summary>
    public void Push(int cellX, int cellY, Direction facing)
    {
        if ((cellX, cellY) == current) return;
        items[pointer] = (cellX, cellY, facing, true);
        current = (cellX, cellY);
        pointer = (pointer + 1) % Length;
    }

    /// <summary>
    /// The player came onto the overworld by a door, a ship or a flight (<c>OverworldMapHistory_PushViaWarp</c>):
    /// the cell is written down with no way faced. As in the original the cell stood in is left as it was, so the
    /// first step there writes the cell again with the way faced.
    /// </summary>
    public void PushViaWarp(int cellX, int cellY)
    {
        if ((cellX, cellY) == current) return;
        items[pointer] = (cellX, cellY, null, true);
        pointer = (pointer + 1) % Length;
    }

    /// <summary>
    /// The cells the Town Map marks (<c>TownMapContext_Init</c>, <c>LoadLocationHistory</c>): the five written before
    /// the newest, from the latest back, without the cell the player is in and each cell once. Where a cell was
    /// written more than once, the oldest writing says which way was faced, as the original's loop leaves it.
    /// </summary>
    public IReadOnlyList<TrailStep> Recent((int X, int Y)? here)
    {
        var steps = new List<TrailStep>();
        int index = (pointer - 2 + Length) % Length;
        for (int i = 0; i < Shown; i++)
        {
            var item = items[index];
            if (!item.Set) break;
            index = (index - 1 + Length) % Length;
            if (here is { } h && (item.X, item.Y) == h) continue;
            var step = new TrailStep(item.X, item.Y, item.Facing);
            int known = steps.FindIndex(s => s.X == item.X && s.Y == item.Y);
            if (known >= 0) steps[known] = step;
            else steps.Add(step);
        }
        return steps;
    }

    /// <summary>The trail as a save keeps it: where the ring's hand is, the cell stood in, then each place of the ring (x, y, the way faced or -1, whether written).</summary>
    public int[] ToSave()
    {
        var numbers = new List<int> { pointer, current.X, current.Y };
        foreach (var item in items)
        {
            numbers.Add(item.X);
            numbers.Add(item.Y);
            numbers.Add(item.Facing is { } f ? (int)f : -1);
            numbers.Add(item.Set ? 1 : 0);
        }
        return numbers.ToArray();
    }

    /// <summary>Puts a saved trail back; a save from before it (null) or one of another shape has none.</summary>
    public void Restore(int[]? saved)
    {
        Clear();
        if (saved == null || saved.Length != 3 + Length * 4) return;
        pointer = Math.Clamp(saved[0], 0, Length - 1);
        current = (saved[1], saved[2]);
        for (int i = 0; i < Length; i++)
        {
            int at = 3 + i * 4;
            Direction? facing = saved[at + 2] is >= 0 and <= 3 ? (Direction)saved[at + 2] : null;
            items[i] = (saved[at], saved[at + 1], facing, saved[at + 3] != 0);
        }
    }

    /// <summary>Whether nothing has been written yet.</summary>
    public bool IsEmpty => items.All(i => !i.Set);
}
