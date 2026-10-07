using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Hearthome Gym's dark rooms (plan 01 · M9), ported from the original's <c>gym_features.c</c>: its two trainer
/// rooms are dark but for a little light round the player and round each trainer, and each has a row of doors in its
/// back wall, marked with signs: a circle, a square and a triangle in the first room, a sun, a ring, a moon, a star
/// and a heart in the second. As the player comes into a room one door is chosen as the way on, and its sign is shown
/// somewhere on the floor (<c>HearthomeGym_DynamicMapFeaturesInit</c>): on a tile the original's table allows, where
/// nobody stands. Every other door leads back to the entrance (the original rewrites their warps the same way).
/// After the Relic Badge the rooms are less dark (the original's thinner fog, <see cref="Fog"/>).
/// </summary>
public sealed class HearthomeDoors : GymPuzzle
{
    public const string Room1Name = "HearthomeRoom1", Room2Name = "HearthomeRoom2";
    public override string Name => room == 1 ? Room1Name : Room2Name;

    /// <summary>The signs on the doors (<c>HEARTHOME_DOOR_ID_*</c>).</summary>
    public enum Sign { Circle, Square, Triangle, Sun, Donut, Moon, Star, Heart }

    /// <summary>The room a wrong door leads back to, and the tile one comes out on: in front of its door to the first room.</summary>
    public const string Entrance = "HearthomeGym";
    public static readonly (int X, int Y) BackAtTheEntrance = (4, 3);

    private readonly int room;
    public HearthomeDoors(int room) => this.room = room;

    /// <summary>Each room's doors, their signs and tiles (<c>sTrainerRoom1Doors</c>, <c>sTrainerRoom2Doors</c>).</summary>
    public IReadOnlyList<(Sign Sign, int X, int Y)> Doors => room == 1 ? Room1Doors : Room2Doors;

    private static readonly (Sign, int, int)[] Room1Doors = { (Sign.Circle, 4, 2), (Sign.Square, 8, 2), (Sign.Triangle, 12, 2) };
    private static readonly (Sign, int, int)[] Room2Doors = { (Sign.Sun, 4, 2), (Sign.Donut, 9, 2), (Sign.Moon, 14, 2), (Sign.Star, 19, 2), (Sign.Heart, 24, 2) };

    // sTrainerRoom1CluePositions and sTrainerRoom2CluePositions: the tiles from (1, 3) where the sign may be shown (0)
    private static readonly string[] Room1Clues =
    {
        "111111111111111", "111111111111111", "111011111110111", "010001111100100",
        "010001111100100", "011101000001110", "011100111001110", "111111111111111"
    };
    private static readonly string[] Room2Clues =
    {
        "011111111111111111111111110", "011111111111111111111111110", "011111000111010100011000010",
        "011111000000010000000111110", "011111000000010000000111110", "000100111111111111111001000",
        "000100111101111111111001000", "111011111000010000111110000", "111011111000010000111110000",
        "000001110011101110011111000", "000001110011101110011111000", "000000011110000011110111111",
        "000000011110010011110111111", "000000000100010001000001000", "000000000111111111000001000",
        "111111100001111110001111111", "111111101110010011101111111", "111111101110010011101111111",
        "111111100100111001001111111", "111111111111111111111111111"
    };

    /// <summary>Whether the original lets the sign be shown on a tile of a room (<c>sTrainerRoomValidCluePositions</c>, from (1, 3)).</summary>
    public static bool ClueMayLie(int room, int x, int y)
    {
        var rows = room == 1 ? Room1Clues : Room2Clues;
        int lx = x - 1, ly = y - 3;
        return ly >= 0 && ly < rows.Length && lx >= 0 && lx < rows[ly].Length && rows[ly][lx] == '0';
    }

    /// <summary>The door that leads on this time, and where its sign is shown; null until the player has come in.</summary>
    public Sign? Correct { get; private set; }
    public (int X, int Y) Clue { get; private set; }

    /// <summary>Whether the rooms are only dimmed now, the Relic Badge won.</summary>
    public bool Dimmed { get; private set; }

    // Where each door leads when it is the right one, as the map file says
    private readonly Dictionary<Warp, (string Map, int X, int Y, Direction Facing)> ways = new();

    public override void Apply(Map map, StoryState story) => Dimmed = story.HasBadge(Badge.Relic);

    /// <summary>
    /// How dark the room is away from every light, 0 to 1: the original's black fog, 109 of 127 in the first room and
    /// 119 in the second, 91 in either once the Relic Badge is won (<c>HearthomeGym_InitFog</c>). It is not a cave's
    /// dark: Flash does nothing here (the header's weather is clear), so it isn't <see cref="Map.IsDark"/>.
    /// </summary>
    public float Fog => (Dimmed ? 91 : room == 2 ? 119 : 109) / 127f;

    /// <summary>
    /// The light carried by the player and by each trainer of the room (the original gives one to the player and to
    /// every trainer whose sight isn't nought, sized by it: 2 for all of them): the tiles of floor seen clearly round
    /// them, and over how many more the light gives out.
    /// </summary>
    public const float LightRadius = 1.5f, LightSoft = 1.1f;

    /// <summary>How much of the fog lies a number of tiles from the nearest light: 0 at the light, 1 past its reach.</summary>
    public static float FogAt(float tiles)
    {
        float t = Math.Clamp((tiles - LightRadius) / LightSoft, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Who carries a light besides the player: the room's trainers (on the map now).</summary>
    public static IEnumerable<NPC> LightBearers(Map map) => map.NPCs.Where(n => n.IsTrainer && n.GymThing == null);

    /// <summary>
    /// Chooses the door that leads on and where its sign lies, as the original does each time the room is come into:
    /// a door at random, then tiles at random until one the table allows that nobody stands on.
    /// </summary>
    public override void Arrive(Map map, StoryState story, Random rng)
    {
        var doors = Doors;
        var correct = doors[rng.Next(doors.Count)].Sign;
        var occupied = map.NPCs.Select(n => (n.GridX, n.GridY)).ToHashSet();
        int width = room == 1 ? 15 : 27, height = room == 1 ? 8 : 20;
        (int X, int Y) clue;
        do clue = (1 + rng.Next(width), 3 + rng.Next(height));
        while (!ClueMayLie(room, clue.X, clue.Y) || occupied.Contains(clue));
        Choose(map, correct, clue);
        Apply(map, story);
    }

    /// <summary>Makes a door the way on and every other one the way back to the entrance (tests choose for themselves).</summary>
    public void Choose(Map map, Sign correct, (int X, int Y) clue)
    {
        Correct = correct;
        Clue = clue;
        foreach (var (sign, x, y) in Doors)
        {
            if (map.GetWarpAt(x, y) is not { } warp) continue;
            if (!ways.ContainsKey(warp)) ways[warp] = (warp.TargetMap, warp.TargetX, warp.TargetY, warp.TargetFacing);
            var way = sign == correct ? ways[warp] : (Entrance, BackAtTheEntrance.X, BackAtTheEntrance.Y, Direction.Down);
            (warp.TargetMap, warp.TargetX, warp.TargetY, warp.TargetFacing) = way;
        }
    }

    /// <summary>The sign on a door's tile, if a door is there.</summary>
    public Sign? SignAt(int x, int y) => Doors.FirstOrDefault(d => d.X == x && d.Y == y) is var (sign, dx, dy) && (dx, dy) == (x, y) ? sign : null;
}
