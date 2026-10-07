using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Turnback Cave (plan 01 · M8): rooms whose doors lead somewhere new every time the player comes in, as the
/// original's <c>ScrCmd_InitTurnbackCave</c> decides it. Every room has a door on each side, in the original's
/// order north, east, south and west; going back out the way one came leads to the entrance, and each of the
/// other three leads on: to Giratina's room once three pillars have been seen, to the entrance after thirty rooms,
/// to a pillar's room one time in four, and otherwise to one of the six rooms before the next pillar. The script
/// of each room keeps the count (<see cref="PillarsSeen"/>, <see cref="RoomsVisited"/>) and calls
/// <c>turnback</c>. No drawing or input.
/// </summary>
public static class TurnbackCave
{
    public const string PillarsSeen = "VAR_TURNBACK_CAVE_PILLARS_SEEN", RoomsVisited = "VAR_TURNBACK_CAVE_ROOMS_VISITED";

    public const string Entrance = "turnback_cave_entrance", PillarRoom = "turnback_cave_pillar_room",
        GiratinaRoom = "turnback_cave_giratina_room";

    /// <summary>The rooms before each pillar, six to a pillar.</summary>
    public static readonly IReadOnlyList<string> Rooms =
        Enumerable.Range(1, 3).SelectMany(p => Enumerable.Range(1, 6).Select(r => $"turnback_cave_pillar_{p}_room_{r}")).ToList();

    /// <summary>Every place the cave's doors can lead to.</summary>
    public static IEnumerable<string> Everywhere => Rooms.Append(Entrance).Append(PillarRoom).Append(GiratinaRoom);

    /// <summary>Whether a room's doors are aimed by <see cref="Reaim"/>.</summary>
    public static bool IsPart(string? area) => area != null && (area == Entrance || area == PillarRoom || area == GiratinaRoom || Rooms.Contains(area));

    /// <summary>The four doors of every room: north, east, south, west (the warps' own order in the original).</summary>
    public static readonly IReadOnlyList<(int X, int Y)> Doors = new[] { (11, 1), (20, 11), (11, 20), (2, 11) };

    /// <summary>Where the doors lead next, by the original's draws (<c>LCRNG_Next() % 100</c>, then <c>% 6</c>).</summary>
    public static string Next(int pillarsSeen, int roomsVisited, Random rng)
    {
        if (pillarsSeen >= 3) return GiratinaRoom;
        if (roomsVisited >= 30) return Entrance;
        if (rng.Next(100) < 25) return PillarRoom;
        return Rooms[rng.Next(6) + pillarsSeen * 6];
    }

    /// <summary>
    /// The door the player came in by, read from where they stand (one step inside it), or -1 for none (coming
    /// in from Sendoff Spring). The original reads the warp's own tile, on which the player arrives.
    /// </summary>
    public static int EntryDoor(int x, int y)
    {
        for (int i = 0; i < Doors.Count; i++)
            if (Math.Abs(Doors[i].X - x) + Math.Abs(Doors[i].Y - y) <= 1) return i;
        return -1;
    }

    /// <summary>A door leads into the next room through the door opposite: north into its south one, east into its west.</summary>
    public static int Opposite(int door) => (door + 2) % 4;

    // Where coming in through each door of each room puts the player down, as the world was built
    private static readonly ConcurrentDictionary<(string Area, int Door), Warp> ways = new();

    /// <summary>Called as the world is built: the way into <paramref name="area"/> through its door <paramref name="door"/>.</summary>
    public static void Learn(string area, int door, Warp way) => ways[(area, door)] = way;

    /// <summary>
    /// Aims the doors of the room the player has just come into (the room's own map): the one they came in by
    /// back to the entrance, the others on to the next room. What it chose is returned; null if the cave isn't built.
    /// </summary>
    public static string? Reaim(Map map, int playerX, int playerY, StoryState story, Random rng)
    {
        string next = Next(story.Var(PillarsSeen), story.Var(RoomsVisited), rng);
        int entry = EntryDoor(playerX, playerY);
        bool any = false;
        for (int door = 0; door < Doors.Count; door++)
        {
            if (map.GetWarpAt(Doors[door].X, Doors[door].Y) is not { } warp) continue;
            string to = door == entry ? Entrance : next;
            if (!ways.TryGetValue((to, Opposite(door)), out var way)) continue;
            warp.TargetMap = way.TargetMap;
            warp.TargetX = way.TargetX;
            warp.TargetY = way.TargetY;
            warp.TargetFacing = way.TargetFacing;
            any = true;
        }
        return any ? next : null;
    }

    /// <summary>The way in through a door, as learned (for tests and the walk through the world).</summary>
    public static Warp? WayInto(string area, int door) => ways.TryGetValue((area, door), out var way) ? way : null;
}
