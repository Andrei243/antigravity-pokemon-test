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

    private const FieldMoves Everything = FieldMoves.Surf | FieldMoves.Waterfall | FieldMoves.RockClimb | FieldMoves.Whirlpool | FieldMoves.Dive;

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

        // Who and what takes the player somewhere when spoken to or read, by the tile they are on, whenever in the story
        // they are there and whatever they stand on (plan 02 · S12): Cynthia by the tear at Spear Pillar, who is hidden
        // until her scene, and a portal of the Distortion World, which hangs on rock or over nothing
        var sending = new Dictionary<Map, Dictionary<(int X, int Y), List<PokemonPlatinumEngine.Story.Instruction>>>();
        Dictionary<(int X, int Y), List<PokemonPlatinumEngine.Story.Instruction>> SendingOf(Map map)
        {
            if (sending.TryGetValue(map, out var found)) return found;
            sending[map] = found = new();
            void Add(int x, int y, string script, string? file)
            {
                var sent = WarpsOf(script, file).ToList();
                if (sent.Count == 0) return;
                if (!found.TryGetValue((x, y), out var list)) found[(x, y)] = list = new();
                list.AddRange(sent);
            }
            foreach (var person in map.Everyone)
                if (person.Script is { } theirs) Add(person.GridX, person.GridY, theirs, person.ScriptFile ?? map.ScriptFileAt(person.GridX, person.GridY));
            foreach (var (at, read) in map.TileScripts) Add(at.X, at.Y, read, map.ScriptFileAt(at.X, at.Y));
            return found;
        }

        while (queue.Count > 0)
        {
            var (map, x, y, mode, height, arrived) = queue.Dequeue();

            // A place whose own arrival script carries the player on is a way there too (plan 02 · S13: the Vista
            // Lighthouse's lift, whose car rides to its other end as it is come into)
            if (arrived)
                foreach (var sent in WarpsOf(PokemonPlatinumEngine.Story.ScriptLibrary.OnEnter, map.ScriptFileAt(x, y)))
                {
                    var onto = mapOf(sent.Name);
                    Visit(onto, sent.X, sent.Y, TravelMode.OnFoot, onto.HeightAt(sent.X, sent.Y), arrived: true);
                }

            // A tile whose own script puts the player down elsewhere on the map is a way there (plan 06 · R16c: the huts
            // of Amity Square's ruins, three tiles in each, which are the only way onto most of its raised ground)
            foreach (var trigger in map.TriggersAt(x, y))
            {
                foreach (var put in PutsOf(trigger.Script, trigger.ScriptFile ?? map.ScriptFileAt(x, y)))
                    if (put.X >= 0 && put.Y >= 0 && put.X < map.Width && put.Y < map.Height && !map.IsSolid(put.X, put.Y))
                        Visit(map, put.X, put.Y, TravelMode.OnFoot, map.HeightAt(put.X, put.Y), arrived: true);
                // and a tile that sends whoever steps on it to another map is a way there (plan 22 · U6, part c: the Relic
                // Castle's sand pits, which drop to the floor below, the only way to its stairs up to the entrance's ledge)
                if (trigger.Script.Length > 0)
                    foreach (var sent in WarpsOf(trigger.Script, trigger.ScriptFile ?? map.ScriptFileAt(x, y)))
                    {
                        var onto = mapOf(sent.Name);
                        if (onto.InBounds(sent.X, sent.Y) && !onto.IsSolid(sent.X, sent.Y))
                            Visit(onto, sent.X, sent.Y, TravelMode.OnFoot, onto.HeightAt(sent.X, sent.Y), arrived: true);
                    }
            }

            // Stepping onto a warp takes it; one is only ever stood on by arriving through another
            if (!arrived && map.WarpsAt(x, y).Any())
            {
                // Every way the tile leads at some point of the story: the way into a lake is one map before Team
                // Galactic comes and another after (plan 02 · S10)
                foreach (var warp in map.WarpsAt(x, y))
                {
                    var onto = mapOf(warp.TargetMap);
                    // (a way along the sea floor into another floor of its own keeps the diver diving: plan 21 · H8,
                    // part b, the passages into the Seafloor Cavern's pool and the pool under Sootopolis)
                    Visit(onto, warp.TargetX, warp.TargetY, onto.IsUnderwater ? TravelMode.Diving : TravelMode.OnFoot, onto.HeightAt(warp.TargetX, warp.TargetY), arrived: true);
                }
                // A door of Turnback Cave is aimed as the player comes in, and can lead into any of its rooms
                int door = TurnbackCave.IsPart(map.AreaAt(x, y)?.Key) ? IndexOf(TurnbackCave.Doors, (x, y)) : -1;
                if (door >= 0)
                    foreach (string room in TurnbackCave.Everywhere)
                        if (TurnbackCave.WayInto(room, TurnbackCave.Opposite(door)) is { } way)
                        {
                            var into = mapOf(way.TargetMap);
                            Visit(into, way.TargetX, way.TargetY, TravelMode.OnFoot, into.HeightAt(way.TargetX, way.TargetY), arrived: true);
                        }
                continue;
            }

            // The Explorer Kit leads down from anywhere the region's own map is walked (plan 06 · R16a): onto the tile of
            // the Underground under the chunk stood in. Its tunnels are several systems that don't join one another
            if (map.Name == "Sinnoh" && mode == TravelMode.OnFoot && y >= 6 * Map.ChunkTiles && x >= Map.ChunkTiles
                && map.DeckAt(x, y) == null && map.BehaviourAt(x, y) != TileBehavior.NoExplorerKit)
            {
                var (bx, bz) = PokemonPlatinumEngine.Models.Underground.UndergroundField.Below(x, y);
                var below = mapOf("Underground");
                if (below.Name == "Underground" && below.InBounds(bx, bz) && !below.IsSolid(bx, bz))
                    Visit(below, bx, bz, TravelMode.OnFoot, below.HeightAt(bx, bz), arrived: true);
            }

            // Dive takes a surfer on dark water down to the same tile of the sea floor under it, and a diver back up to
            // the same tile of the sea above wherever the floor lets them (plan 21 · H5)
            // (or, where the original names one place below or above an area, to that place: plan 21 · H8, part b)
            if (mode == TravelMode.Surfing && FieldMovement.CanDive(map, x, y, new Walker(mode, height, Moves: Everything)))
            {
                var spot = map.DiveTargetAt(x, y)!;
                var below = mapOf(spot.Map);
                if (below.InBounds(spot.X, spot.Y) && !below.IsSolid(spot.X, spot.Y))
                    Visit(below, spot.X, spot.Y, TravelMode.Diving, below.HeightAt(spot.X, spot.Y), arrived: false);
            }
            if (mode == TravelMode.Diving && FieldMovement.CanSurface(map, x, y))
            {
                var spot = map.EmergeTargetAt(x, y)!;
                var above = mapOf(spot.Map);
                if (FieldMovement.SurfacesOnto(above, spot.X, spot.Y))
                    Visit(above, spot.X, spot.Y, TravelMode.Surfing, above.SurfaceAt(spot.X, spot.Y, above.HeightAt(spot.X, spot.Y)).Height, arrived: false);
            }

            // A cracked floor that drops to the floor below (plan 21 · H8, part c: Sky Pillar's fourth floor down into its
            // third's middle, the only way to the stairs up from there) takes a walker down onto the same tile
            if (mode == TravelMode.OnFoot && map.HoleTargetAt(x, y) is { } hole)
            {
                var below = mapOf(hole.Map);
                if (below.InBounds(hole.X, hole.Y) && !below.IsSolid(hole.X, hole.Y))
                    Visit(below, hole.X, hole.Y, TravelMode.OnFoot, below.HeightAt(hole.X, hole.Y), arrived: false);
            }

            // A platform of the League's (plan 01 · M9, part 2d) carries whoever steps onto it at the foot of its
            // shaft up to the top, once
            if (map.Puzzle is LeagueLift lift && lift.Covers(x, y) && MathF.Abs(height - lift.Where.Bottom) < 0.01f)
                Visit(map, x, y, mode, lift.Where.Top, arrived: false);

            // Every climb the field offers is taken: the player says yes to Waterfall and Rock Climb
            var walker = new Walker(mode, height, Moves: Everything, Climbing: true);
            foreach (var way in Ways)
            {
                var (ax, ay) = FieldMovement.Delta(way);
                // A secret base's spot is a way into its room, by Secret Power (plan 21 · H10): in at the tile inside its
                // door, whose way out leads back
                if (mode == TravelMode.OnFoot && map.SecretBaseSpots.TryGetValue((x + ax, y + ay), out int baseId)
                    && PokemonPlatinumEngine.Models.SecretBaseLayouts.Of(baseId) is { } layout && mapOf(layout.Map) is { } baseRoom
                    && baseRoom.Name == layout.Map && baseRoom.Warps.FirstOrDefault() is { } baseDoor)
                    Visit(baseRoom, baseDoor.SourceX, baseDoor.SourceY - 1, TravelMode.OnFoot, baseRoom.HeightAt(baseDoor.SourceX, baseDoor.SourceY - 1), arrived: true);
                if (SendingOf(map).TryGetValue((x + ax, y + ay), out var portals))
                    foreach (var sent in portals)
                    {
                        var onto = mapOf(sent.Name);
                        Visit(onto, sent.X, sent.Y, TravelMode.OnFoot, onto.HeightAt(sent.X, sent.Y), arrived: true);
                    }
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
                // Someone a new game hides until their scene (the rival on his own doorstep) isn't in the way at all, and
                // nor is someone the story takes away (the rival at the Oreburgh Gym's door, once Roark is back)
                else if (step.Obstacle == Obstacle.Person && map.GetNpcAt(nx, ny) is { HiddenBy: { } hiding }
                         && (HiddenAtStart.Value.Contains(hiding) || HiddenByTheStory.Value.Contains(hiding))
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, mode, map.HeightAt(nx, ny), arrived: false);
                // Someone whose own script walks them aside (the guard at the League's door, once he has seen the
                // Badges) stands in the way only until then
                else if (step.Obstacle == Obstacle.Person && map.GetNpcAt(nx, ny) is { Script: { } aside } stepping && StepsAside(aside, stepping.ScriptFile ?? map.Name)
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, mode, map.HeightAt(nx, ny), arrived: false);
                // A walkway of the Sunyshore Gym's gears that isn't there now is there once a button has turned them
                // (GymTests searches the buttons; here every walkway counts as one that can be come to)
                // and so is the Blackthorn Gym's lava, which its slabs carry the player over (BlackthornGymTests holds
                // the slabs; here every tile of lava counts as one a slab can be brought under; plan 20 · J11, part c)
                // and so is a hole a pushed boulder fills (plan 22 · U6, part c: the Relic Passage's, which its pits' scripts
                // fill; UnovaStoryTests pushes the boulders in)
                else if (step.Obstacle == Obstacle.Solid && mode == TravelMode.OnFoot && map.FilledHoles.ContainsKey((nx, ny))
                         && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, mode, map.HeightAt(nx, ny), arrived: false);
                else if (step.Obstacle == Obstacle.Solid && map.Puzzle is SunyshoreGears or BlackthornPlatforms && mode == TravelMode.OnFoot
                         && map.InBounds(nx, ny) && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, mode, map.HeightAt(nx, ny), arrived: false);
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

    /// <summary>The flags a new game starts with set (common.NewGame): whoever they hide is off the map when the walk sets out.</summary>
    public static readonly Lazy<HashSet<string>> HiddenAtStart = new(() =>
    {
        var story = new PokemonPlatinumEngine.Story.StoryState();
        PokemonPlatinumEngine.Story.StoryMigration.BeginNewGame(story, PokemonPlatinumEngine.Story.ScriptLibrary.Default);
        return story.Flags.ToHashSet();
    });

    /// <summary>The flags some script sets: whoever they hide stands in the way only until the story moves them on.</summary>
    public static readonly Lazy<HashSet<string>> HiddenByTheStory = new(() =>
        PokemonPlatinumEngine.Story.ScriptLibrary.Default.All.SelectMany(s => s.Everything())
            .Where(i => i.Op == PokemonPlatinumEngine.Story.Op.SetFlag && !i.Own).Select(i => i.Name).ToHashSet(StringComparer.Ordinal));

    /// <summary>
    /// Where a person's script can send the player (its <c>warp</c> commands, whichever answer leads there, and those of
    /// the scripts it calls, however deep: the Seagallop's sailors' trips are common scripts, plan 19 · K7, and the
    /// Rainbow Pass's menu is a common script that calls them, plan 19 · K9, part b).
    /// </summary>
    private static IEnumerable<PokemonPlatinumEngine.Story.Instruction> WarpsOf(string script, string? file)
    {
        var library = PokemonPlatinumEngine.Story.ScriptLibrary.Default;
        var lines = new List<PokemonPlatinumEngine.Story.Instruction>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var waiting = new Queue<string>();
        waiting.Enqueue(script);
        while (waiting.Count > 0)
        {
            string name = waiting.Dequeue();
            if (!seen.Add(name) || library.Find(name, file) is not { } found) continue;
            foreach (var line in found.Everything())
            {
                lines.Add(line);
                if (line.Op == PokemonPlatinumEngine.Story.Op.Call) waiting.Enqueue(line.Name);
            }
        }
        return lines.Where(i => i.Op == PokemonPlatinumEngine.Story.Op.Warp);
    }

    /// <summary>
    /// Where a tile's script puts the player down on its own map: its <c>place player</c>, or that of a script it
    /// calls. A script that warps is no such way (what it places is on the map it warps to).
    /// </summary>
    private static IEnumerable<PokemonPlatinumEngine.Story.Instruction> PutsOf(string script, string? file)
    {
        var library = PokemonPlatinumEngine.Story.ScriptLibrary.Default;
        var lines = library.Find(script, file)?.Everything().ToList();
        if (lines == null) return Enumerable.Empty<PokemonPlatinumEngine.Story.Instruction>();
        lines.AddRange(lines.Where(i => i.Op == PokemonPlatinumEngine.Story.Op.Call).ToList()
            .SelectMany(call => library.Find(call.Name, file)?.Everything() ?? Enumerable.Empty<PokemonPlatinumEngine.Story.Instruction>()));
        if (lines.Any(i => i.Op == PokemonPlatinumEngine.Story.Op.Warp)) return Enumerable.Empty<PokemonPlatinumEngine.Story.Instruction>();
        return lines.Where(i => i.Op == PokemonPlatinumEngine.Story.Op.Place && i.Name == "player");
    }

    /// <summary>Whether a person's script walks them off their own tile (<c>walk self</c>).</summary>
    internal static bool StepsAside(string script, string? file) =>
        PokemonPlatinumEngine.Story.ScriptLibrary.Default.Find(script, file)?.Everything()
            .Any(i => i.Op == PokemonPlatinumEngine.Story.Op.Walk && i.Name == "self") == true;

    private static int IndexOf(IReadOnlyList<(int X, int Y)> tiles, (int X, int Y) tile)
    {
        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i] == tile) return i;
        return -1;
    }

    /// <summary>
    /// Where the story alone takes the player, with no way there on foot yet: a walk of the whole game sets out from
    /// these too, as the player will once the scenes open them. (The Distortion World's two arrivals stood here until
    /// plan 02 · S12's scenes led there: Cynthia's question at the torn pillar and the portal in Turnback Cave's last
    /// room, which the walk follows by itself; so did the Fight Area's pier until plan 02 · S15, part a gave Snowpoint
    /// City's sailor his ship, and Pal Park's lobby until the same session took Route 221's workers from its doors;
    /// Fullmoon Island's pier until plan 02 · S15, part b gave Canalave City's sailor that answer, and Snowpoint
    /// Temple's hall until the same session opened its door.)
    /// What is left is Newmoon Island of plan 01 · M10, part b, where the original's Harbor Inn leaves whoever has the
    /// Member Card of one of its events and where Canalave City's sailor sails afterwards (its TakeShipFromCanalave to
    /// 152, 277, facing east): the session that brings the event's gifts writes the way there.
    /// </summary>
    public static readonly MapSpot[] StoryArrivals =
    {
        new("Sinnoh", 152, 277, Direction.Right),
    };

    /// <summary>
    /// Places of a region opened before the way to them: a walk of the whole game sets out from these too, until the
    /// session that opens the way takes its line out. None is, today: Route 124 (plan 21 · H5, the sea over Dive's first
    /// floor) was, until plan 21 · H7, part a opened Lilycove City, from whose sea it is come to.
    /// </summary>
    public static readonly MapSpot[] OpenedAhead = { };

    /// <summary>
    /// Floors reached only by falling through the one above, which the field doesn't do yet: the walk sets out from where
    /// the fall lands. Icefall Cave's B1F lies under 1F's thin ice, which FireRed breaks under a second step (plan 19 ·
    /// K9, part c, whose session takes the line out once the ice gives way).
    /// </summary>
    public static readonly MapSpot[] FallenTo =
    {
        new("FourIsland_IcefallCave_B1F", 8, 9, Direction.Down),
    };

    /// <summary>
    /// Places a walk by the starting layouts can't join. Johto's Victory Road has a stretch no tile of the import joins to the rest: 2F's east and north-east and 3F's
    /// middle, whose only edges are walls and ledges that hop out of it (the ladder at 3F 30,27 has no warp). The walk
    /// sets out from inside it until plan 20 · J12, which writes the floors' own scenes, finds how HeartGold enters it
    /// and takes the line out.
    /// </summary>
    public static readonly MapSpot[] NotYetLinked =
    {
        new("IndigoVictoryRoad_2F", 54, 23, Direction.Down),
        // The port's east steps lead to the Dunsparce Tunnel's far end, which exists only once the dig has laid the tunnel
        // out again (FLAG_THREE_ISLAND_TUNNEL_DUG_OUT): the walk goes by the layout before it
        new("ThreeIsland", 134, 45, Direction.Down),
        new("ThreeIsland_DunsparceTunnel", 25, 5, Direction.Down),
        // Icefall Cave's thin ice (see FallenTo): the stairs it hides lead down to B1F
        new("FourIsland_IcefallCave_1F", 12, 3, Direction.Down),
        new("FourIsland_IcefallCave_1F", 15, 16, Direction.Down),
        new("FourIsland_IcefallCave_B1F", 12, 3, Direction.Down),
        new("FourIsland_IcefallCave_B1F", 15, 16, Direction.Down),
    };

    /// <summary>The whole game from where each region that is built begins, and from where only the story takes the player.</summary>
    public static Dictionary<Map, HashSet<(int X, int Y)>> FromEveryStart(Func<string, Map> mapOf) =>
        From(mapOf, RegionDatabase.All.Where(r => r.Start != null).Select(r => r.Start!).Concat(StoryArrivals).Concat(OpenedAhead).Concat(FallenTo).Concat(NotYetLinked).ToArray());
}
