using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Veilstone Gym's punching bags (plan 01 · M9), ported from the original's <c>gym_features.c</c>: nine bags hang
/// on tracks across the dojo and eleven stacks of tyres stand in its passages. Facing a bag and pressing the button
/// kicks it (<c>VeilstoneGym_HitPunchingBag</c>): it runs along its track the way the player kicked it until the
/// track ends, it reaches a point it stops at, or a stack of tyres stands in front of it, and that stack falls and is
/// gone. A bag that can't move that way only swings. Where a bag may run is the original's own table of the room's
/// tiles (<c>sVeilstoneTileBehaviors</c>), and the bags and the stacks are laid out afresh each time the player comes
/// in by the door (<c>PersistedMapFeatures_InitForVeilstoneGym</c>), as the original's are; a battle doesn't move them.
/// </summary>
public sealed class VeilstoneBags : GymPuzzle
{
    public const string PuzzleName = "VeilstoneBags";
    public override string Name => PuzzleName;

    /// <summary>Where the bags and the stacks start (<c>sPunchingBagPositions</c>, <c>sTireStackPositions</c>, the objects being made two tiles north of each).</summary>
    public static readonly (int X, int Y)[] Bags = { (3, 10), (4, 22), (8, 7), (8, 20), (15, 26), (16, 10), (20, 17), (21, 24), (23, 11) };
    public static readonly (int X, int Y)[] TireStacks =
        { (3, 21), (8, 10), (8, 12), (8, 14), (12, 7), (12, 10), (19, 10), (20, 11), (20, 26), (22, 7), (23, 28) };

    // sVeilstoneTileBehaviors, a row for each of the dojo's 32 rows: 0 a bag may be here, 1 it may not, 3 it may not
    // go west or east from here, 4 it stops on passing here, 5 it may not go west from here (2, no north or south, is
    // in the original's list but nowhere in its table)
    private static readonly string[] Track =
    {
        "11111111111111111111111111111111", "11111111111111111111111111111111", "11111111111111111111111111111111",
        "11111111111111111111111111111111", "11111111111111111111111111111111", "11111111111111111111111111111111",
        "11111111111111111111111111111111", "11111111000400000000001111111111", "11111111111111111111111111111111",
        "11111111111111111111111111111111", "11101111100004004001111111111111", "11101111111111111111111011111111",
        "11135000111111111111011011111111", "11130111111111111111011011111111", "11100000111111111111011011111111",
        "11110111111111111111011011111111", "11110111111111111111301011111111", "11100000011111111111301011111111",
        "11100111011111111111101011111111", "11100111011111111111101011111111", "11100000011111111111141011111111",
        "11110111111111111111101011111111", "11110111111111111111101011111111", "11111111111111111111101011111111",
        "11111111111111111111101011111111", "11111111111111111111111011111111", "11111111111111100000111011111111",
        "11111111111111111111111011111111", "11111111111111111111111111111111", "11111111111111111111111111111111",
        "11111111111111111111111111111111", "11111111111111111111111111111111"
    };

    [Flags]
    public enum Flags
    {
        None = 0,
        /// <summary>The bag can't go on that way from here.</summary>
        Blocked = 1,
        /// <summary>A stack of tyres stands on the next tile.</summary>
        TireStack = 2,
        /// <summary>The bag stops on passing this tile.</summary>
        Pause = 4
    }

    public static int TrackAt(int x, int y) => x < 0 || y < 0 || x >= 32 || y >= 32 ? 1 : Track[y][x] - '0';

    /// <summary>What a bag on a tile, going a way, meets (<c>VeilstoneGym_GetTileFlags</c>).</summary>
    public static Flags FlagsAt(int x, int y, Direction dir, Func<int, int, bool> tireStackAt)
    {
        var flags = Flags.None;
        switch (TrackAt(x, y))
        {
            case 4: flags |= Flags.Pause; break;
            case 2 when dir is Direction.Up or Direction.Down: flags |= Flags.Blocked; break;
            case 3 when dir is Direction.Left or Direction.Right: flags |= Flags.Blocked; break;
            case 5 when dir == Direction.Left: flags |= Flags.Blocked; break;
        }
        var (dx, dy) = FieldMovement.Delta(dir);
        if (TrackAt(x + dx, y + dy) == 1) flags |= Flags.Blocked;
        if (tireStackAt(x + dx, y + dy)) flags |= Flags.TireStack;
        return flags;
    }

    /// <summary>
    /// How far a bag kicked a way from a tile runs, and what stops it (<c>VeilstoneGym_CalculateDistanceBagWillTravel</c>):
    /// it doesn't move at all where anything but a point it stops at is in its way from where it hangs.
    /// </summary>
    public static (int Distance, Flags Stop) Run(int x, int y, Direction dir, Func<int, int, bool> tireStackAt)
    {
        var flags = FlagsAt(x, y, dir, tireStackAt);
        if (flags != Flags.None && flags != Flags.Pause) return (0, flags);
        var (dx, dy) = FieldMovement.Delta(dir);
        int distance = 0;
        do
        {
            x += dx;
            y += dy;
            distance++;
            flags = FlagsAt(x, y, dir, tireStackAt);
        } while (flags == Flags.None);
        return (distance, flags);
    }

    /// <summary>A kick and what it came to: where the bag went, and the stack it knocked down.</summary>
    public sealed record Kick(NPC Bag, Direction Way, int FromX, int FromY, int Distance, NPC? Toppled);

    public static bool IsBag(NPC npc) => npc.NpcType == NPC.PunchingBagType;
    public static bool IsTireStack(NPC npc) => npc.NpcType == NPC.TireStackType;

    private static NPC? TireStackOn(Map map, int x, int y) => map.NPCs.FirstOrDefault(n => IsTireStack(n) && n.GridX == x && n.GridY == y);

    /// <summary>
    /// Kicks a bag of the map the way the player faces: the bag goes to where its run ends, and the stack of tyres it
    /// runs into falls and is taken off the map until the player comes in again. The drawing plays it out afterwards.
    /// </summary>
    public static Kick KickBag(Map map, NPC bag, Direction way, bool toppleNow = true)
    {
        var (distance, stop) = Run(bag.GridX, bag.GridY, way, (x, y) => TireStackOn(map, x, y) != null);
        int fromX = bag.GridX, fromY = bag.GridY;
        var (dx, dy) = FieldMovement.Delta(way);
        NPC? toppled = null;
        if (distance > 0)
        {
            bag.GridX += dx * distance;
            bag.GridY += dy * distance;
            if (stop.HasFlag(Flags.TireStack) && TireStackOn(map, bag.GridX + dx, bag.GridY + dy) is { } stack)
            {
                toppled = stack;
                if (toppleNow) Topple(map, stack);
            }
        }
        return new Kick(bag, way, fromX, fromY, distance, toppled);
    }

    /// <summary>A stack of tyres falls and is off the map until the player comes in again.</summary>
    public static void Topple(Map map, NPC stack)
    {
        stack.Forced = false;
        map.NPCs.Remove(stack);
        if (!map.Absent.Contains(stack)) map.Absent.Add(stack);
    }

    // Where each bag and stack first stood, to put them back as the player comes in
    private readonly Dictionary<NPC, (int X, int Y)> homes = new();

    public override void Apply(Map map, StoryState story)
    {
        if (homes.Count > 0) return;
        foreach (var npc in map.Everyone.Where(n => IsBag(n) || IsTireStack(n))) homes[npc] = (npc.GridX, npc.GridY);
    }

    public override void Arrive(Map map, StoryState story, Random rng)
    {
        Apply(map, story);
        foreach (var (npc, (x, y)) in homes)
        {
            (npc.GridX, npc.GridY) = (x, y);
            npc.StepOffsetX = npc.StepOffsetY = 0f;
            npc.Forced = null;
        }
        map.ApplyPresence(story.Has);
    }
}
