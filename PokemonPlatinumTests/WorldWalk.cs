using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The game walked through from where it starts: every tile of every map that a player can come to who has
/// everything the field can ask for. They walk by the field's own rules (<see cref="FieldMovement"/>: ledges the
/// way they face, cliffs, bridges), set out onto water and ride it, ride the Bicycle (jumping its ramps, up muddy
/// slopes), climb waterfalls and rock faces, clear the
/// three obstacles (a small tree, a cracked rock, a boulder to push), pick up whatever item lies in their way, and
/// go through every warp to the map behind it. The world is no longer one place with one way through it: a cave lies between Route 204's two
/// halves, and a warp reached only by Surf is still reached.
/// </summary>
internal static class WorldWalk
{
    private static readonly Direction[] Ways = { Direction.Up, Direction.Right, Direction.Left, Direction.Down };

    private const FieldMoves Everything = FieldMoves.Surf | FieldMoves.Waterfall | FieldMoves.RockClimb;

    /// <summary>The tiles reached on each map, from one place or several.</summary>
    public static Dictionary<Map, HashSet<(int X, int Y)>> From(Func<string, Map> mapOf, params MapSpot[] starts)
    {
        var reached = new Dictionary<Map, HashSet<(int X, int Y)>>();
        var seen = new HashSet<(Map Map, int X, int Y, TravelMode Mode, int Level)>();
        var queue = new Queue<(Map Map, int X, int Y, TravelMode Mode, float Height, bool Arrived)>();

        void Visit(Map map, int x, int y, TravelMode mode, float height, bool arrived)
        {
            // The level tells the deck of a bridge from the ground under it
            if (!seen.Add((map, x, y, mode, (int)MathF.Round(height * 2f)))) return;
            if (!reached.TryGetValue(map, out var tiles)) reached[map] = tiles = new();
            tiles.Add((x, y));
            queue.Enqueue((map, x, y, mode, height, arrived));
        }

        foreach (var start in starts)
        {
            var map = mapOf(start.Map);
            Visit(map, start.X, start.Y, TravelMode.OnFoot, map.HeightAt(start.X, start.Y), arrived: true);
        }

        while (queue.Count > 0)
        {
            var (map, x, y, mode, height, arrived) = queue.Dequeue();

            // Stepping onto a warp takes it; one is only ever stood on by arriving through another
            if (!arrived && map.GetWarpAt(x, y) is { } warp)
            {
                var onto = mapOf(warp.TargetMap);
                Visit(onto, warp.TargetX, warp.TargetY, TravelMode.OnFoot, onto.HeightAt(warp.TargetX, warp.TargetY), arrived: true);
                continue;
            }

            // Every climb the field offers is taken: the player says yes to Waterfall and Rock Climb
            var walker = new Walker(mode, height, Moves: Everything, Climbing: true);
            foreach (var way in Ways)
            {
                var step = FieldMovement.Step(map, x, y, way, walker);
                if (step.Moves)
                {
                    Visit(map, step.X, step.Y, step.Mode, step.Height, arrived: false);
                    continue;
                }
                // On the Bicycle, in either gear, and off it again where it stops: a ramp is jumped, a muddy slope climbed
                if (mode == TravelMode.OnFoot)
                {
                    bool rode = false;
                    foreach (bool fast in new[] { true, false })
                    {
                        var ride = FieldMovement.Step(map, x, y, way, new Walker(TravelMode.Cycling, height, FastGear: fast, Moves: Everything, Climbing: true));
                        if (!ride.Moves) continue;
                        Visit(map, ride.X, ride.Y, TravelMode.OnFoot, ride.Height, arrived: false);
                        rode = true;
                    }
                    if (rode) continue;
                }

                var (dx, dy) = FieldMovement.Delta(way);
                int nx = x + dx, ny = y + dy;
                if (step.Obstacle == Obstacle.Water && FieldMovement.CanStartSurf(map, x, y, way, walker))
                    Visit(map, nx, ny, TravelMode.Surfing, map.SurfaceAt(nx, ny, height).Height, arrived: false);
                // An item in its ball is in the way only until it is picked up, and an obstacle until it is cut down,
                // smashed or pushed aside
                else if (step.Obstacle == Obstacle.Person && mode == TravelMode.OnFoot && map.GetNpcAt(nx, ny) is { IsThing: true }
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, TravelMode.OnFoot, map.HeightAt(nx, ny), arrived: false);
                // Someone who takes the player somewhere when spoken to (a ferry) is a way there too
                else if (step.Obstacle == Obstacle.Person && map.GetNpcAt(nx, ny) is { Script: { } name } person)
                    foreach (var sent in WarpsOf(name, person.ScriptFile))
                    {
                        var onto = mapOf(sent.Name);
                        Visit(onto, sent.X, sent.Y, TravelMode.OnFoot, onto.HeightAt(sent.X, sent.Y), arrived: true);
                    }
            }
        }
        return reached;
    }

    /// <summary>Where a person's script can send the player (its <c>warp</c> commands, whichever answer leads there).</summary>
    private static IEnumerable<PokemonPlatinumEngine.Story.Instruction> WarpsOf(string script, string? file) =>
        PokemonPlatinumEngine.Story.ScriptLibrary.Default.Find(script, file)?.Everything().Where(i => i.Op == PokemonPlatinumEngine.Story.Op.Warp)
        ?? Enumerable.Empty<PokemonPlatinumEngine.Story.Instruction>();

    /// <summary>The whole game from where each region that is built begins.</summary>
    public static Dictionary<Map, HashSet<(int X, int Y)>> FromEveryStart(Func<string, Map> mapOf) =>
        From(mapOf, RegionDatabase.All.Where(r => r.Start != null).Select(r => r.Start!).ToArray());
}
