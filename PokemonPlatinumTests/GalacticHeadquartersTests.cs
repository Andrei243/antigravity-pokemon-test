using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The eighth chapter's second half (plan 02 · S11, part b, "Team Galactic's headquarters") played through with no
/// screen, in the order a player meets it: the grunt who lets the Storage Key fall in Veilstone City and Looker's
/// question, the warehouse's door, the headquarters' floors with their warp panels and shutters, the Galactic Key,
/// Cyrus's speech in the hall and his battle on the fourth floor, the laboratory, and the control room where Commander
/// Saturn is beaten and the three Pokémon of the lakes are set free. Each scene is started as the game starts it, on
/// maps of its own (<see cref="OpeningTests.Game"/>). The warp panels' own rule (<see cref="WarpPanels"/>) is held
/// here too.
/// </summary>
public class GalacticHeadquartersTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Team = "FLAG_HIDE_GALACTIC_HQ_TEAM_GALACTIC";

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(0, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 52));
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(System.IO.Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>
    /// The story as the chapters before leave it: Looker gone into the warehouse and out of it again with the
    /// explosion in Pastoria (veilstone_city.txt, pastoria_city.txt, plan 02 · S8), and the scene at Lake Acuity played
    /// (lake_acuity.txt, plan 02 · S11, part a), which brings the grunt with the Storage Key to Veilstone City.
    /// </summary>
    private static void PastLakeAcuity(OpeningTests.Game game) =>
        Run(game, "veilstone_city", "setflag FLAG_HIDE_VEILSTONE_CITY_LOOKER\nsetflag FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS\nsetflag FLAG_HIDE_VEILSTONE_COUNTERPART\n"
            + "setflag FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER\nsetvar VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE 2\n"
            + "setvar VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE 3\nsetvar VAR_LAKE_ACUITY_STATE 2\nclearflag FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY");

    /// <summary>Speaks to someone of the player's map from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing, string? place = null)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        return game.Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name)!, npc);
    }

    /// <summary>Faces a tile of a room that a script reads (a sign with a script of its own) and presses the button.</summary>
    private static HeadlessScriptHost Read(OpeningTests.Game game, int x, int y, Direction facing)
    {
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (x - dx, y - dy);
        game.Facing = facing;
        Assert.True(game.Map.IsWalkable(game.Tile.X, game.Tile.Y), $"{game.Map.Name}: nobody can stand at {game.Tile} to read {x},{y}");
        return game.Play(Scripts.Find(game.Map.SignScripts[(x, y)], game.Map.Name)!);
    }

    /// <summary>The grunt on the terrace spoken to, the player facing a way, and Looker's question answered.</summary>
    private static HeadlessScriptHost TakeTheStorageKey(OpeningTests.Game game, Direction facing = Direction.Up, bool agree = true)
    {
        game.Arrive("Sinnoh", 723, 598);
        game.Answers.Enqueue(agree ? 0 : 1);
        return TalkFrom(game, "grunt_m_storage_key", facing, "veilstone_city");
    }

    /// <summary>Up to the warehouse's door with Looker's word given: he unlocks it and goes on ahead.</summary>
    private static HeadlessScriptHost OpenTheWarehouse(OpeningTests.Game game, int x = 8)
    {
        game.Arrive("VeilstoneGalacticWarehouse", 8, 11);
        game.Tile = (x, 8);
        game.Facing = Direction.Up;
        var trigger = game.Map.Triggers.Single(t => t.Script == "LookerOpensDoor");
        Assert.Same(trigger, FieldScripts.TriggerAt(game.Map, x, 8, game.Story));
        return game.Play(Scripts.Find("LookerOpensDoor", "VeilstoneGalacticWarehouse")!);
    }

    /// <summary>The Galactic Key in the bag and every shutter it opens open, as their scripts leave them.</summary>
    private static void WithTheGalacticKey(OpeningTests.Game game)
    {
        game.Bag.AddItem(Item("Galactic Key"));
        Run(game, "GalacticHQ1F", "setflag FLAG_OBTAINED_GALACTIC_HQ_B2F_GALACTIC_KEY\nsetflag FLAG_HIDE_GALACTIC_HQ_B2F_DOOR\nsetflag FLAG_HIDE_GALACTIC_HQ_1F_DOOR\n"
            + "setflag FLAG_HIDE_GALACTIC_HQ_3F_DOOR\nsetflag FLAG_HIDE_GALACTIC_HQ_4F_DOOR");
    }

    private static HeadlessScriptHost HearTheSpeech(OpeningTests.Game game, int y = 12)
    {
        game.Arrive("GalacticHQHall", 24, 6);
        game.Tile = (20, y);
        game.Facing = Direction.Left;
        Assert.Same(game.Map.Triggers.Single(), FieldScripts.TriggerAt(game.Map, 20, y, game.Story));
        return game.Play(Scripts.Find("Speech", "GalacticHQHall")!);
    }

    private static HeadlessScriptHost MeetCyrus(OpeningTests.Game game, int x = 8)
    {
        game.Arrive("GalacticHQ4F", 4, 15);
        game.Tile = (x, 11);
        game.Facing = Direction.Up;
        Assert.Same(game.Map.Triggers.Single(), FieldScripts.TriggerAt(game.Map, x, 11, game.Story));
        return game.Play(Scripts.Find("Cyrus", "GalacticHQ4F")!);
    }

    // ------------------------------------------------------------------ warp panels: the rule

    /// <summary>A room three tiles by three with a panel in two of its corners, each leading to the other.</summary>
    private static Map PanelRoom()
    {
        var map = new Map(5, 5) { Name = "Panels", Interior = InteriorStyle.Lab };
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                map.SetGroundTile(x, y, x is 0 or 4 || y is 0 or 4 ? TileType.Wall : TileType.Floor, x is 0 or 4 || y is 0 or 4);
        map.SetBehaviour(1, 1, TileBehavior.WarpPanel);
        map.SetBehaviour(3, 3, TileBehavior.WarpPanel);
        map.Warps.Add(new Warp { SourceX = 1, SourceY = 1, TargetMap = "Panels", TargetX = 3, TargetY = 3, TargetFacing = Direction.Down });
        map.Warps.Add(new Warp { SourceX = 3, SourceY = 3, TargetMap = "Panels", TargetX = 1, TargetY = 1, TargetFacing = Direction.Down });
        // A staircase's warp beside them, which turns whoever takes it the way it says
        map.Warps.Add(new Warp { SourceX = 3, SourceY = 1, TargetMap = "Panels", TargetX = 2, TargetY = 2, TargetFacing = Direction.Left });
        return map;
    }

    private static Warp? StepOnto(Player player, Map map, Direction dir)
    {
        Warp? taken = null;
        bool started = false;
        for (int frame = 0; frame < 600; frame++)
        {
            player.Advance(1f / 60f, map, started ? null : dir, false, _ => { }, warp => taken = warp);
            started |= player.IsMoving;
            if (taken != null || started && !player.IsMoving) break;
        }
        return taken;
    }

    [Fact]
    public void AStepThatEndsOnAWarpPanelIsCarriedToTheFarPanelFacingAsItWas()
    {
        var map = PanelRoom();
        Assert.True(WarpPanels.IsPanel(map, 1, 1));
        Assert.False(WarpPanels.IsPanel(map, 2, 2));
        Assert.False(WarpPanels.IsPanel(map, -1, 9));
        Assert.Same(map.GetWarpAt(1, 1), WarpPanels.At(map, 1, 1));
        Assert.Null(WarpPanels.At(map, 3, 1));

        // Onto the panel from the east, walking west: the panel's warp is what the step comes to
        var player = new Player(2, 1);
        var taken = StepOnto(player, map, Direction.Left);
        Assert.Same(map.GetWarpAt(1, 1), taken);
        Assert.True(WarpPanels.Carries(map, taken!));
        // On the far panel, facing west as they did, whatever the warp's own facing says
        Assert.Equal((3, 3, Direction.Left), WarpPanels.Arrival(map, taken!, player.Facing));
        foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            Assert.Equal(way, WarpPanels.Arrival(map, taken!, way).Facing);
        // A warp that is no panel's puts its traveller down the way it says
        var stairs = map.GetWarpAt(3, 1)!;
        Assert.False(WarpPanels.Carries(map, stairs));
        Assert.Equal((2, 2, Direction.Left), WarpPanels.Arrival(map, stairs, Direction.Up));
    }

    [Fact]
    public void ArrivingOnAPanelIsNoStepSoItCarriesNobodyBackUntilTheyStepOffAndOn()
    {
        var map = PanelRoom();
        var player = new Player(2, 1);
        var taken = StepOnto(player, map, Direction.Left)!;
        var (x, y, facing) = WarpPanels.Arrival(map, taken, player.Facing);
        player.SetPosition(x, y, facing);
        // Standing on the far panel for a long while takes nobody anywhere
        Warp? again = null;
        for (int frame = 0; frame < 300; frame++) player.Advance(1f / 60f, map, null, false, _ => { }, warp => again = warp);
        Assert.Null(again);
        Assert.Equal((3, 3), (player.GridX, player.GridY));
        // Nor does walking into the wall beside it
        Assert.Null(StepOnto(player, map, Direction.Right));
        Assert.Equal((3, 3), (player.GridX, player.GridY));
        // A step off it is a step like any other, and the step back on is carried
        Assert.Null(StepOnto(player, map, Direction.Up));
        Assert.Equal((3, 2), (player.GridX, player.GridY));
        Assert.Same(map.GetWarpAt(3, 3), StepOnto(player, map, Direction.Down));
    }

    [Fact]
    public void TheSpinTurnsThroughAllFourWaysAndIsAFunctionOfItsTime()
    {
        foreach (var from in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            Assert.Equal(from, WarpPanels.Spin(from, 0f));
            Assert.Equal(from, WarpPanels.Spin(from, -1f));
            var seen = new List<Direction>();
            for (int quarter = 0; quarter < 4; quarter++) seen.Add(WarpPanels.Spin(from, (quarter + 0.5f) * WarpPanels.QuarterTurn));
            Assert.Equal(4, seen.Distinct().Count());
            Assert.Equal(from, seen[0]);
            // A whole turn later they face as they did
            Assert.Equal(from, WarpPanels.Spin(from, 4.5f * WarpPanels.QuarterTurn));
        }
        // Clockwise seen from above: down, left, up, right
        Assert.Equal(Direction.Left, WarpPanels.Spin(Direction.Down, 1.5f * WarpPanels.QuarterTurn));
        Assert.Equal(Direction.Up, WarpPanels.Spin(Direction.Left, 1.5f * WarpPanels.QuarterTurn));
        // The turn before the picture closes brings every way round at least once
        Assert.True(WarpPanels.SpinUp >= 4 * WarpPanels.QuarterTurn);
        Assert.True(WarpPanels.SpinDown > 0f && WarpPanels.SpinDown < WarpPanels.SpinUp);
    }

    [Fact]
    public void TheBehavioursTableSaysWhatAPanelDoes()
    {
        var (support, note) = TileBehaviors.InGame(TileBehavior.WarpPanel);
        // A step onto a panel is a step like any other: what it does comes once the step has ended
        Assert.Equal(BehaviourSupport.Plain, support);
        Assert.Contains("WarpPanels", note);
        Assert.DoesNotContain("M11", note);
    }

    [Fact]
    public void EveryPanelOfTheHeadquartersLeadsToAPanelThatLeadsBackButTheWayOut()
    {
        int panels = 0;
        var rooms = RoomPlanTests.GalacticHeadquarters.ToDictionary(n => n, Room);
        foreach (var (name, map) in rooms)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (!WarpPanels.IsPanel(map, x, y)) continue;
                    panels++;
                    string where = $"{name} {x},{y}";
                    Assert.False(map.IsSolid(x, y), where);
                    var warp = WarpPanels.At(map, x, y);
                    Assert.True(warp != null, $"{where}: a panel that leads nowhere");
                    Assert.Contains(map.Props, p => p.Type is PropType.WarpPanel or PropType.WarpPanelOut && p.X == x && p.Y == y);
                    var far = rooms[warp!.TargetMap];
                    if ((name, x, y) == ("GalacticHQ4F", 21, 17))
                    {
                        // The green panel: down to the pad by the building's third door, which is no panel and has no warp
                        Assert.Equal(("GalacticHQ1F", 41, 14), (warp.TargetMap, warp.TargetX, warp.TargetY));
                        Assert.False(WarpPanels.IsPanel(far, 41, 14));
                        Assert.Null(far.GetWarpAt(41, 14));
                        Assert.Contains(map.Props, p => p.Type == PropType.WarpPanelOut && p.X == x && p.Y == y);
                        Assert.Contains(far.Props, p => p.Type == PropType.WarpPanelOut && p.X == 41 && p.Y == 14);
                        continue;
                    }
                    Assert.True(WarpPanels.IsPanel(far, warp.TargetX, warp.TargetY), $"{where}: its far end {warp.TargetMap} {warp.TargetX},{warp.TargetY} is no panel");
                    var back = WarpPanels.At(far, warp.TargetX, warp.TargetY)!;
                    Assert.Equal((name, x, y), (back.TargetMap, back.TargetX, back.TargetY));
                    Assert.Contains(map.Props, p => p.Type == PropType.WarpPanel && p.X == x && p.Y == y);
                }
            // A plate on the floor where nothing carries anyone is the green pad alone
            foreach (var plate in map.Props.Where(p => p.Type is PropType.WarpPanel or PropType.WarpPanelOut))
                Assert.True(WarpPanels.IsPanel(map, plate.X, plate.Y) || (name, plate.X, plate.Y) == ("GalacticHQ1F", 41, 14), $"{name}: a plate at {plate.X},{plate.Y} with no panel under it");
        }
        // The original's: six on the ground floor, ten on the second, six on the third, three on the fourth, three in the
        // hall and one in the laboratory
        Assert.Equal(29, panels);
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheFloorsAreJoinedByTheOriginalsStairs()
    {
        // (below, the warp up, above, the warp down): each comes out beside the other's stairs, walking away from them
        var stairs = new[]
        {
            ("VeilstoneGalacticWarehouse", 13, 3, "GalacticHQB2F", 3, 16),
            ("GalacticHQB2F", 81, 3, "GalacticHQB1F", 3, 7),
            ("GalacticHQB2F", 50, 3, "GalacticHQ1F", 11, 3),
            ("GalacticHQB1F", 11, 3, "GalacticHQ1F", 46, 3),
            ("GalacticHQ1F", 4, 3, "GalacticHQ2F", 7, 3),
            ("GalacticHQ1F", 27, 3, "GalacticHQ2F", 24, 3),
            ("GalacticHQ1F", 19, 14, "GalacticHQ2F", 10, 11),
            ("GalacticHQ2F", 12, 3, "GalacticHQ3F", 15, 3),
            ("GalacticHQ2F", 50, 3, "GalacticHQ3F", 47, 3),
            ("GalacticHQ3F", 10, 11, "GalacticHQ4F", 3, 15),
        };
        foreach (var (a, ax, ay, b, bx, by) in stairs)
        {
            Map first = Room(a), second = Room(b);
            foreach (var (from, x, y, to, tx, ty) in new[] { (first, ax, ay, second, bx, by), (second, bx, by, first, ax, ay) })
            {
                var warp = from.GetWarpAt(x, y);
                Assert.True(warp != null, $"{from.Name}: no stairs at {x},{y}");
                Assert.Equal(to.Name, warp!.TargetMap);
                Assert.Equal(ty, warp.TargetY);
                // Beside the far stairs' own warp, on the side away from the flight, walking away from it
                Assert.Equal(1, Math.Abs(warp.TargetX - tx));
                Assert.Equal(warp.TargetX > tx ? Direction.Right : Direction.Left, warp.TargetFacing);
                Assert.True(to.IsWalkable(warp.TargetX, warp.TargetY), $"{from.Name} {x},{y}: lands in something at {warp.TargetX},{warp.TargetY}");
                var flight = to.Props.Single(p => p.Type is PropType.SideStairsUp or PropType.SideStairsDown or PropType.SideStairsUpEast or PropType.SideStairsDownWest
                    && p.Y == ty && (p.X == tx + 1 || p.X + p.Width == tx));
                Assert.Equal(flight.X > tx, warp.TargetX < tx);
            }
        }
    }

    [Fact]
    public void TheBuildingsThreeDoorsLeadOntoTheGroundFloorsMatsAndBack()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 717, 590);
        var floor = Room("GalacticHQ1F");
        Assert.Equal((1, 3), floor.RoomCorner());
        foreach (var (street, mat) in new[] { (714, 8), (717, 29), (720, 42) })
        {
            var into = game.Map.GetWarpAt(street, 589);
            Assert.True(into != null, $"the door at {street}, 589 is shut");
            Assert.Equal(("GalacticHQ1F", mat, 22, Direction.Up), (into!.TargetMap, into.TargetX, into.TargetY, into.TargetFacing));
            Assert.Null(into.OpenedBy);
            Assert.True(floor.IsWalkable(mat, 22));
            Assert.Equal(TileType.Door, floor.GetGroundTile(mat, 23));
            var back = floor.GetWarpAt(mat, 23)!;
            Assert.Equal(("Sinnoh", street, 590, Direction.Down), (back.TargetMap, back.TargetX, back.TargetY, back.TargetFacing));
            Assert.True(game.Map.IsWalkable(street, 590));
        }
        Assert.Equal(3, floor.Warps.Count(w => w.TargetMap == "Sinnoh"));
        // No other floor has a door to the street, and each begins where the original's does
        foreach (string name in RoomPlanTests.GalacticHeadquarters.Where(n => n != "GalacticHQ1F"))
        {
            var room = Room(name);
            Assert.DoesNotContain(room.Warps, w => w.TargetMap == "Sinnoh");
            Assert.True(room.IsIndoors);
            Assert.Equal(InteriorStyle.Lab, room.Interior);
        }
        Assert.Equal((1, 3), Room("GalacticHQB2F").RoomCorner());
        Assert.Equal((1, 3), Room("GalacticHQHall").RoomCorner());
        // The laboratory's way on is in its back wall: wall to look at, open to walk into, over the annex's floor
        var lab = Room("GalacticHQLaboratory");
        Assert.Equal((1, 3), lab.RoomCorner());
        Assert.Equal(TileType.Wall, lab.GetGroundTile(36, 2));
        Assert.False(lab.IsSolid(36, 2));
        var on = lab.GetWarpAt(36, 2)!;
        Assert.Equal(("GalacticHQControlRoom", 8, 13, Direction.Up), (on.TargetMap, on.TargetX, on.TargetY, on.TargetFacing));
        var control = Room("GalacticHQControlRoom");
        var out_ = control.GetWarpAt(8, 14)!;
        Assert.Equal(TileType.Door, control.GetGroundTile(8, 14));
        Assert.Equal(("GalacticHQLaboratory", 36, 3, Direction.Down), (out_.TargetMap, out_.TargetX, out_.TargetY, out_.TargetFacing));
    }

    /// <summary>
    /// Everywhere of the headquarters that can be walked to from a tile, through its stairs, panels and doors, with
    /// the story as it is: a shutter or a bollard that stands is in the way, people who would battle or talk are not.
    /// </summary>
    private static HashSet<(string Map, int X, int Y)> Walk(OpeningTests.Game game, string start, int sx, int sy)
    {
        var reached = new HashSet<(string, int, int)> { (start, sx, sy) };
        var queue = new Queue<(string Map, int X, int Y, bool Arrived)>();
        queue.Enqueue((start, sx, sy, true));
        while (queue.Count > 0)
        {
            var (name, x, y, arrived) = queue.Dequeue();
            var map = game.Maps[name];
            if (!arrived && map.GetWarpAt(x, y) is { } warp)
            {
                if (warp.TargetMap != "Sinnoh" && reached.Add((warp.TargetMap, warp.TargetX, warp.TargetY))) queue.Enqueue((warp.TargetMap, warp.TargetX, warp.TargetY, true));
                continue;
            }
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny)) continue;
                if (map.GetNpcAt(nx, ny) is { IsThing: true, IsItemBall: false }) continue;
                if (reached.Add((name, nx, ny))) queue.Enqueue((name, nx, ny, false));
            }
        }
        return reached;
    }

    [Fact]
    public void TheWayInIsTheWarehousesAndTheShuttersKeepWhatTheOriginalsKeep()
    {
        var game = NewGame();
        PastLakeAcuity(game);
        (string, int, int) key = ("GalacticHQB2F", 20, 5), cyrus = ("GalacticHQ4F", 8, 11), hall = ("GalacticHQHall", 20, 12), lab = ("GalacticHQLaboratory", 1, 11);

        // By the building's own doors: the lobby and the room of the green pad, and nothing else, whatever the story
        foreach (int mat in new[] { 8, 29 })
        {
            var lobby = Walk(game, "GalacticHQ1F", mat, 22);
            Assert.All(lobby, t => Assert.Equal("GalacticHQ1F", t.Map));
            Assert.Contains(("GalacticHQ1F", 29, 17), lobby);
            Assert.DoesNotContain(("GalacticHQ1F", 22, 17), lobby);
        }
        var pad = Walk(game, "GalacticHQ1F", 42, 22);
        Assert.Contains(("GalacticHQ1F", 41, 14), pad);
        Assert.All(pad, t => Assert.True(t.Map == "GalacticHQ1F" && t.X is >= 39 and <= 46 && t.Y >= 14, $"the third door leads to {t}"));

        // The warehouse's door shut: its hall and no further
        var shut = Walk(game, "VeilstoneGalacticWarehouse", 8, 11);
        Assert.All(shut, t => Assert.Equal("VeilstoneGalacticWarehouse", t.Map));
        Assert.DoesNotContain(("VeilstoneGalacticWarehouse", 8, 6), shut);

        // Opened: down the stairs and through the building by its stairs and panels to the Galactic Key, and no further
        // than the third floor's west side. The hall is reached by the second floor's middle, which the stairs behind
        // the lobby's shutter lead to ("the door beside the reception counter"), and the fourth floor only from the
        // hall's far side, by its west panel, once the ranks have filed out
        TakeTheStorageKey(game);
        OpenTheWarehouse(game);
        var open = Walk(game, "VeilstoneGalacticWarehouse", 8, 11);
        Assert.Contains(("GalacticHQB2F", 4, 16), open);
        Assert.Contains(key, open);
        Assert.Contains(("GalacticHQB1F", 5, 5), open);
        Assert.Contains(("GalacticHQ1F", 51, 14), open);
        Assert.Contains(("GalacticHQ1F", 17, 5), open);
        Assert.Contains(("GalacticHQ2F", 20, 5), open);
        Assert.DoesNotContain(("GalacticHQ2F", 40, 6), open);
        Assert.Contains(("GalacticHQ3F", 9, 20), open);
        Assert.DoesNotContain(hall, open);
        Assert.DoesNotContain(("GalacticHQHall", 1, 12), open);
        Assert.DoesNotContain(("GalacticHQ2F", 33, 8), open);
        Assert.DoesNotContain(("GalacticHQ3F", 30, 12), open);
        Assert.DoesNotContain(("GalacticHQ4F", 4, 15), open);
        Assert.DoesNotContain(cyrus, open);
        Assert.DoesNotContain(lab, open);
        Assert.DoesNotContain(("GalacticHQControlRoom", 8, 12), open);
        // The key's side of the basement is come to down the stairs from the ground floor, not through its shutter
        var storeRooms = new HashSet<(string, int, int)>();
        FloodOneRoom(game.Maps["GalacticHQB2F"], 4, 16, storeRooms);
        Assert.DoesNotContain(key, storeRooms);
        Assert.Contains(("GalacticHQB2F", 14, 7), storeRooms);
        Assert.DoesNotContain(("GalacticHQB2F", 14, 9), storeRooms);
        var keySide = new HashSet<(string, int, int)>();
        FloodOneRoom(game.Maps["GalacticHQB2F"], 49, 3, keySide);
        Assert.Contains(key, keySide);
        Assert.Contains(("GalacticHQB2F", 14, 9), keySide);

        // With the Galactic Key and its shutters open, in by the lobby (nothing inside leads to the lobby's shutter but
        // the lobby): Cyrus, and by his panels the laboratory and the control room; the way out by the green panel stays
        // shut by its bollards until Team Galactic has gone
        WithTheGalacticKey(game);
        Assert.DoesNotContain(cyrus, Walk(game, "VeilstoneGalacticWarehouse", 8, 11));
        var keyed = Walk(game, "GalacticHQ1F", 29, 22);
        Assert.Contains(cyrus, keyed);
        Assert.Contains(hall, keyed);
        Assert.Contains(("GalacticHQHall", 1, 12), keyed);
        Assert.Contains(("GalacticHQ2F", 33, 8), keyed);
        // (the third floor's shutter is on the warehouse's side of the building, as the basement's is)
        Assert.Contains(("GalacticHQ3F", 30, 12), Walk(game, "VeilstoneGalacticWarehouse", 8, 11));
        Assert.DoesNotContain(("GalacticHQ3F", 30, 12), keyed);
        Assert.Contains(("GalacticHQ2F", 40, 6), keyed);
        Assert.Contains(("GalacticHQ4F", 4, 15), keyed);
        // (while the ranks stand, the hall's back and its west panel are two places: nobody walks through them)
        var back = new HashSet<(string, int, int)>();
        FloodOneRoom(game.Maps["GalacticHQHall"], 24, 6, back, people: true);
        Assert.Contains(hall, back);
        Assert.DoesNotContain(("GalacticHQHall", 1, 12), back);
        Assert.Contains(lab, keyed);
        Assert.Contains(("GalacticHQControlRoom", 8, 12), keyed);
        Assert.Contains(("GalacticHQ1F", 22, 17), keyed);
        Assert.DoesNotContain(("GalacticHQ4F", 21, 17), keyed);
        Assert.DoesNotContain(("GalacticHQ1F", 41, 14), keyed);
        Run(game, "GalacticHQ4F", "setflag " + Team);
        var gone = Walk(game, "GalacticHQ1F", 29, 22);
        Assert.Contains(("GalacticHQ4F", 21, 17), gone);
        Assert.Contains(("GalacticHQ1F", 41, 14), gone);
        Assert.Contains(("GalacticHQ1F", 42, 22), gone);
    }

    private static void FloodOneRoom(Map map, int sx, int sy, HashSet<(string, int, int)> reached, bool people = false)
    {
        var queue = new Queue<(int X, int Y)>();
        reached.Add((map.Name, sx, sy));
        queue.Enqueue((sx, sy));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny) || map.GetNpcAt(nx, ny) is { IsThing: true, IsItemBall: false }) continue;
                if (people && map.GetNpcAt(nx, ny) is { IsThing: false }) continue;
                if (reached.Add((map.Name, nx, ny))) queue.Enqueue((nx, ny));
            }
        }
    }

    [Fact]
    public void TheFloorsKeepTheOriginalsItemsTrainersAndHiddenThings()
    {
        // Each ball on its tile with what the original puts in it
        var balls = new[]
        {
            ("GalacticHQ1F", 52, 14, "TM49"), ("GalacticHQ1F", 16, 6, "Dubious Disc"), ("GalacticHQ2F", 48, 21, "Max Revive"), ("GalacticHQ2F", 3, 11, "Green Shard"),
            ("GalacticHQ3F", 36, 12, "TM21"), ("GalacticHQ3F", 50, 21, "Max Elixir"), ("GalacticHQ3F", 38, 20, "Protein"), ("GalacticHQB2F", 29, 11, "TM36"),
            ("GalacticHQB2F", 20, 5, "Galactic Key"), ("GalacticHQB2F", 62, 3, "Zinc"), ("GalacticHQLaboratory", 6, 8, "Full Restore"),
        };
        foreach (var (room, x, y, item) in balls)
        {
            var ball = Room(room).Everyone.Single(n => n.IsItemBall && (n.GridX, n.GridY) == (x, y));
            Assert.Equal(item, ball.Item);
            Assert.StartsWith("FLAG_OBTAINED_GALACTIC_HQ_", ball.HiddenBy);
            Assert.NotNull(ItemDatabase.Get(item));
        }
        var hidden = new[]
        {
            ("GalacticHQ1F", 45, 21, "PP Up"), ("GalacticHQ2F", 44, 5, "Poké Doll"), ("GalacticHQ2F", 1, 12, "Max Potion"), ("GalacticHQ3F", 39, 21, "Elixir"),
            ("GalacticHQ3F", 2, 18, "Rare Candy"), ("GalacticHQ3F", 21, 14, "Nugget"), ("GalacticHQ4F", 20, 7, "Razor Claw"),
        };
        foreach (var (room, x, y, item) in hidden)
        {
            var map = Room(room);
            Assert.Equal(item, map.HiddenItems[(x, y)].Item);
            Assert.StartsWith("FLAG_OBTAINED_HIDDEN_GALACTIC_HQ_", map.HiddenItems[(x, y)].Flag);
            // Found by looking at its tile from a tile beside it, with no sign in the way of the look
            Assert.False(map.Signboards.ContainsKey((x, y)), $"{room}: a sign hides the {item}");
            Assert.Contains(new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }, d => map.IsWalkable(x + d.Item1, y + d.Item2));
        }

        // The trainers are Platinum's, by the trainer each object names, with its sight; all leave with Team Galactic
        var trainers = new[]
        {
            ("GalacticHQ1F", "galactic_grunt", "galactic_grunt_galactic_hq_1f", 2, "Glameow"), ("GalacticHQ1F", "scientist_fredrick", "scientist_fredrick", 6, "Kirlia"),
            ("GalacticHQ2F", "galactic_grunt_1", "galactic_grunt_galactic_hq_2f_1", 3, "Glameow"), ("GalacticHQ2F", "galactic_grunt_2", "galactic_grunt_galactic_hq_2f_2", 4, "Golbat"),
            ("GalacticHQ2F", "galactic_grunt_3", "galactic_grunt_galactic_hq_2f_3", 4, "Golbat"), ("GalacticHQ2F", "scientist_darrius", "scientist_darrius", 2, "Porygon2"),
            ("GalacticHQ3F", "galactic_grunt_1", "galactic_grunt_galactic_hq_3f_1", 3, "Stunky"), ("GalacticHQ3F", "galactic_grunt_2", "galactic_grunt_galactic_hq_3f_2", 2, "Murkrow"),
            ("GalacticHQ3F", "galactic_grunt_3", "galactic_grunt_galactic_hq_3f_3", 3, "Stunky"), ("GalacticHQ3F", "galactic_grunt_4", "galactic_grunt_galactic_hq_3f_4", 3, "Croagunk"),
            ("GalacticHQB2F", "galactic_grunt_1", "galactic_grunt_galactic_hq_b2f_1", 5, "Golbat"), ("GalacticHQB2F", "galactic_grunt_2", "galactic_grunt_galactic_hq_b2f_2", 3, "Houndour"),
        };
        foreach (var (room, key, id, sight, lead) in trainers)
        {
            var who = Room(room).Everyone.Single(n => n.Key == key);
            Assert.True(who.IsTrainer, $"{room}: {key}");
            Assert.Equal((id, sight, lead), (who.TrainerData!.Id, who.TrainerData.SightRange, who.TrainerData.Party.Members[0].Species.Name));
            Assert.True(who.TrainerData.Party.Members[0].Level >= 37);
            Assert.Equal(Team, who.HiddenBy);
            Assert.False(string.IsNullOrWhiteSpace(who.TrainerData.DialogueBefore));
            Assert.False(string.IsNullOrWhiteSpace(who.TrainerData.DialogueAfter));
        }
        // The grunt who paces the third floor's corridor walks back and forth along it, eight tiles each way
        var pacing = Room("GalacticHQ3F").Everyone.Single(n => n.Key == "galactic_grunt_2");
        Assert.Equal((PersonMoves.Loop, 8), (pacing.Movement!.Kind, pacing.Movement.RangeX));

        // Whoever only talks has something to say, and nobody stands in anything or on anyone
        foreach (string name in RoomPlanTests.GalacticHeadquarters)
        {
            var map = Room(name);
            foreach (var npc in map.Everyone.Where(n => !n.IsPokemon))
            {
                Assert.False(map.IsSolid(npc.GridX, npc.GridY), $"{name}: {npc.Key} stands in something solid");
                Assert.True(npc.DialogLines.Count > 0 || npc.Script != null || npc.IsTrainer || npc.IsItemBall, $"{name}: {npc.Key} has nothing to say");
            }
            Assert.Equal(map.Everyone.Count(), map.Everyone.Select(n => (n.GridX, n.GridY)).Distinct().Count());
            // Every script a person or a sign of the room names is the room's own or a common one
            foreach (var npc in map.Everyone.Where(n => n.Script != null))
                Assert.True(Scripts.Find(npc.Script!, name) != null, $"{name}: {npc.Key}'s script {npc.Script}");
            foreach (var (tile, script) in map.SignScripts)
            {
                Assert.True(Scripts.Find(script, name) != null, $"{name}: the sign at {tile} runs {script}");
                // (Saturn stands on the one tile the button is pressed from until he is beaten)
                Assert.Contains(new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }, d => !map.IsSolid(tile.X + d.Item1, tile.Y + d.Item2));
            }
        }
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameKeepsTheHeadquartersPeopleWhereTheOriginalDoes()
    {
        var game = NewGame();
        foreach (string flag in new[] { "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY", "FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN" })
            Assert.True(game.Story.Has(flag), flag);
        // Veilstone City: nobody on the terrace before the building yet
        game.Arrive("Sinnoh", 723, 598);
        Assert.Null(game.Present("grunt_m_storage_key", "veilstone_city"));
        Assert.Null(game.Map.GetNpcAt(721, 593));

        // The lobby: its receptionist behind her counter and three members, the shutter standing; no Saturn yet
        game.Arrive("GalacticHQ1F", 29, 22);
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_GALACTIC_HQ"));
        foreach (string key in new[] { "grunt_f_1", "scientist_m_1", "grunt_f_2", "grunt_m", "galactic_hq_door_west", "galactic_hq_door_east" })
            Assert.NotNull(game.Present(key));
        Assert.Null(game.Present("saturn"));
        Assert.True(game.Map.IsCounter(29, 16));
        Assert.Contains("Veilstone Building", string.Join(" ", game.Talk("grunt_f_1").Transcript.Select(l => l.Text)));
        Assert.All(new[] { "scientist_m_1", "grunt_m", "grunt_f_2" }, key => Assert.NotEmpty(game.Talk(key).Transcript));
        Assert.Contains("locked", string.Join(" ", TalkFrom(game, "galactic_hq_door_west", Direction.Up).Transcript.Select(l => l.Text)));

        // The hall: Looker at the back, the ranks across its middle, the dais empty
        game.Arrive("GalacticHQHall", 24, 6);
        Assert.Empty(game.Last.Transcript);
        var looker = game.Present("looker")!;
        Assert.Equal((19, 12, Direction.Left), (looker.GridX, looker.GridY, looker.Facing));
        Assert.Null(game.Present("cyrus"));
        var ranks = game.Map.NPCs.Where(n => n.HiddenBy == "FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS").ToList();
        Assert.Equal(81, ranks.Count);
        Assert.All(ranks, g => Assert.Equal(Direction.Up, g.Facing));
        Assert.All(ranks, g => Assert.True(g.GridX is >= 9 and <= 17 && g.GridY is >= 9 and <= 17));
        // They stand across the whole of the hall's middle, so the west panel leads nowhere while they are there
        for (int x = 9; x <= 17; x++) Assert.NotNull(game.Map.GetNpcAt(x, 12));
        for (int x = 9; x <= 17; x++) Assert.NotNull(game.Map.GetNpcAt(x, 13));
        Assert.True(game.Fires("Speech"));

        // The fourth floor: Cyrus at his desk, his shutter and the bollards standing
        game.Arrive("GalacticHQ4F", 4, 15);
        var cyrus = game.Present("cyrus")!;
        Assert.Equal((8, 8, Direction.Up), (cyrus.GridX, cyrus.GridY, cyrus.Facing));
        foreach (string key in new[] { "galactic_hq_door_west", "galactic_hq_door_east", "bollard_west", "bollard_east" }) Assert.NotNull(game.Present(key));
        Assert.True(game.Fires("Cyrus"));

        // The control room: Saturn before the button with Charon beside him, and the three in their machines
        game.Arrive("GalacticHQControlRoom", 8, 13);
        Assert.Equal((8, 6), (game.Present("saturn")!.GridX, game.Present("saturn")!.GridY));
        Assert.Equal((7, 6), (game.Present("charon")!.GridX, game.Present("charon")!.GridY));
        foreach (var (key, x, y) in new[] { ("mesprit", 2, 4), ("azelf", 14, 4), ("uxie", 8, 10) })
        {
            var held = game.Present(key)!;
            Assert.Equal((x, y, true), (held.GridX, held.GridY, held.IsPokemon));
            Assert.Equal(key, held.Species!.ToLowerInvariant());
            // Read from the tile south of its machine
            Assert.True(game.Map.IsWalkable(x, y + 1));
            Assert.Contains("shut inside", string.Join(" ", TalkFrom(game, key, Direction.Up).Transcript.Select(l => l.Text)));
        }
        Assert.False(game.Fires("CallBack"));
    }

    // ------------------------------------------------------------------ Veilstone City and the warehouse

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void TheGruntLetsTheStorageKeyFallAndLookerComesUpWithIt(Direction facing)
    {
        var game = NewGame();
        PastLakeAcuity(game);
        game.Arrive("Sinnoh", 723, 598);
        var grunt = game.Present("grunt_m_storage_key", "veilstone_city")!;
        Assert.Equal((721, 593, Direction.Right), (grunt.GridX, grunt.GridY, grunt.Facing));
        Assert.Null(game.Present("looker", "veilstone_city"));

        var scene = TakeTheStorageKey(game, facing);
        var (dx, dy) = FieldMovement.Delta(facing);
        var stood = (721 - dx, 593 - dy);
        Assert.Equal(stood, game.Tile);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Galactic Grunt" && l.Text.Contains("antennae"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Clefairy"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Storage Key") && l.Speaker == "Galactic Grunt");
        Assert.Contains(scene.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("Storage Key"));
        // The grunt has gone for good, and Looker stands a tile south of the player, the key his
        Assert.Null(game.Present("grunt_m_storage_key", "veilstone_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY"));
        Assert.True(game.Story.Has("FLAG_VEILSTONE_CITY_GRUNT_DROPPED_STORAGE_KEY"));
        var looker = game.Present("looker", "veilstone_city")!;
        Assert.Equal((stood.Item1, stood.Item2 + 1), (looker.GridX, looker.GridY));
        Assert.Equal(Direction.Up, looker.Facing);
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_LOOKER"));
        // The player said yes: his word is given, and the warehouse waits
        Assert.Equal(3, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        Assert.True(game.Story.Has("FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT"));
        // In Platinum the player never holds the key
        Assert.Equal(0, game.Bag.GetQuantity(Item("Storage Key")));
    }

    [Fact]
    public void LookerAsksAgainBeforeTheBuildingUntilThePlayerAgrees()
    {
        var game = NewGame();
        PastLakeAcuity(game);
        var scene = TakeTheStorageKey(game, Direction.Up, agree: false);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("no small thing"));
        Assert.Equal(2, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        Assert.False(game.Story.Has("FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT"));
        // The warehouse's door stays shut and nobody comes
        game.Arrive("VeilstoneGalacticWarehouse", 8, 11);
        Assert.False(game.Fires("LookerOpensDoor"));
        Assert.Contains("special key", string.Join(" ", TalkFrom(game, "galactic_hq_door_west", Direction.Up).Transcript.Select(l => l.Text)));
        Assert.Contains("special key", string.Join(" ", TalkFrom(game, "galactic_hq_door_east", Direction.Up).Transcript.Select(l => l.Text)));

        // Come to again, he waits before the building's middle door (the original's OnTransition) and asks once more
        game.Arrive("Sinnoh", 723, 598);
        var looker = game.Present("looker", "veilstone_city")!;
        Assert.Equal((717, 593, Direction.Up), (looker.GridX, looker.GridY, looker.Facing));
        game.Answers.Enqueue(1);
        Assert.Contains(game.Talk("looker", "veilstone_city").Transcript, l => l.Text.Contains("no small thing"));
        Assert.Equal(2, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        var yes = game.Talk("looker", "veilstone_city");
        Assert.Contains(yes.Asked, a => a.Answer == "Yes");
        Assert.Contains(yes.Transcript, l => l.Text.Contains("we meet"));
        Assert.Equal(3, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        // Agreed, he only says where to meet
        var after = game.Talk("looker", "veilstone_city");
        Assert.Empty(after.Asked);
        Assert.Contains(after.Transcript, l => l.Text.Contains("we meet"));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void LookerUnlocksTheWarehousesDoorAndGoesOnAhead(int x)
    {
        var game = NewGame();
        PastLakeAcuity(game);
        TakeTheStorageKey(game);
        game.Arrive("VeilstoneGalacticWarehouse", 8, 11);
        // The door is the original's: two leaves across the corridor, and behind it the stairs down
        foreach (var (key, tx) in new[] { ("galactic_hq_door_west", 8), ("galactic_hq_door_east", 9) })
        {
            var leaf = game.Present(key)!;
            Assert.Equal((tx, 7, NPC.GalacticDoorType), (leaf.GridX, leaf.GridY, leaf.NpcType));
            Assert.True(leaf.IsThing);
            Assert.False(game.Map.IsSolid(tx, 7));
        }
        Assert.Null(game.Present("looker"));

        var scene = OpenTheWarehouse(game, x);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("kept you waiting"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Storage Key"));
        Assert.Contains("sound door_slide", scene.Log);
        Assert.Contains("sound stairs", scene.Log);
        Assert.Equal((x, 8), game.Tile);
        Assert.Null(game.Present("galactic_hq_door_west"));
        Assert.Null(game.Present("galactic_hq_door_east"));
        Assert.Null(game.Present("looker"));
        foreach (string flag in new[] { "FLAG_USED_STORAGE_KEY", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_GALACTIC_HQ_DOOR", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER", "FLAG_HIDE_VEILSTONE_CITY_LOOKER" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Equal(4, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        Assert.False(game.Fires("LookerOpensDoor"));
        // He went down the stairs behind the door, and so can the player
        Assert.Equal((13, 3), (game.Map.Everyone.Single(n => n.Key == "looker").GridX, game.Map.Everyone.Single(n => n.Key == "looker").GridY));
        var down = game.Map.GetWarpAt(13, 3)!;
        Assert.Equal(("GalacticHQB2F", 4, 16, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.Contains(game.Map.Props, p => p.Type == PropType.SideStairsDown && p.X == 14 && p.Y == 3);
        // And he is gone from the street
        game.Through("Sinnoh");
        Assert.Null(game.Present("looker", "veilstone_city"));
    }

    // ------------------------------------------------------------------ the floors

    [Theory]
    [InlineData("GalacticHQ1F", 22, 18, Direction.Up, "FLAG_HIDE_GALACTIC_HQ_1F_DOOR")]
    [InlineData("GalacticHQ3F", 27, 19, Direction.Down, "FLAG_HIDE_GALACTIC_HQ_3F_DOOR")]
    [InlineData("GalacticHQ4F", 9, 14, Direction.Up, "FLAG_HIDE_GALACTIC_HQ_4F_DOOR")]
    [InlineData("GalacticHQB2F", 15, 8, Direction.Up, "FLAG_HIDE_GALACTIC_HQ_B2F_DOOR")]
    public void AShutterOpensToTheGalacticKeyAndStaysOpen(string room, int x, int y, Direction facing, string flag)
    {
        var game = NewGame();
        var arrival = game.Maps.Values.SelectMany(m => m.Warps).First(w => w.TargetMap == room);
        game.Arrive(room, arrival.TargetX, arrival.TargetY);
        var leaf = game.Map.GetNpcAt(x, y)!;
        Assert.Equal(NPC.GalacticDoorType, leaf.NpcType);
        Assert.Equal(flag, leaf.HiddenBy);
        // Both leaves stand side by side across a gap two tiles wide
        var other = game.Map.NPCs.Single(n => n.NpcType == NPC.GalacticDoorType && n != leaf);
        Assert.Equal((1, 0), (Math.Abs(other.GridX - leaf.GridX), Math.Abs(other.GridY - leaf.GridY)));
        Assert.True(game.Map.IsSolid(Math.Min(leaf.GridX, other.GridX) - 1, y) && game.Map.IsSolid(Math.Max(leaf.GridX, other.GridX) + 1, y));

        // Without the key it only says it is locked
        var locked = TalkFrom(game, leaf.Key, facing);
        Assert.Empty(locked.Asked);
        Assert.Contains(locked.Transcript, l => l.Speaker == null && l.Text.Contains("special key"));
        Assert.False(game.Story.Has(flag));

        // With it: asked, and a no leaves it shut
        game.Bag.AddItem(Item("Galactic Key"));
        game.Answers.Enqueue(1);
        var no = TalkFrom(game, leaf.Key, facing);
        Assert.Contains(no.Asked, a => a.Question.Contains("Galactic Key") && a.Answer == "No");
        Assert.False(game.Story.Has(flag));
        Assert.NotNull(game.Map.GetNpcAt(x, y));

        var yes = TalkFrom(game, leaf.Key, facing);
        Assert.Contains("sound door_slide", yes.Log);
        Assert.Contains(yes.Transcript, l => l.Text.Contains("slid open"));
        Assert.True(game.Story.Has(flag));
        Assert.DoesNotContain(game.Map.NPCs, n => n.NpcType == NPC.GalacticDoorType);
        // The key is kept, and the shutter is open for good
        Assert.Equal(1, game.Bag.GetQuantity(Item("Galactic Key")));
        game.Arrive(room, arrival.TargetX, arrival.TargetY);
        Assert.DoesNotContain(game.Map.NPCs, n => n.NpcType == NPC.GalacticDoorType);
        Assert.True(game.Map.IsWalkable(x, y));
    }

    [Fact]
    public void TheGalacticKeyIsFoundInTheBasement()
    {
        var game = NewGame();
        game.Arrive("GalacticHQB2F", 49, 3);
        var ball = game.Map.NPCs.Single(n => n.Item == "Galactic Key");
        game.Tile = (ball.GridX + 1, ball.GridY);
        game.Play(Scripts.Find(FieldScripts.For(ball)!, game.Map.Name)!, ball);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Galactic Key")));
        Assert.True(game.Story.Has("FLAG_OBTAINED_GALACTIC_HQ_B2F_GALACTIC_KEY"));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Item == "Galactic Key");
        // The grunt who mislaid it stands in the next room, and says where the speech is given
        Assert.Contains(game.Talk("grunt_m").Transcript, l => l.Text.Contains("key"));
    }

    [Fact]
    public void TheSecondFloorsBedsRestTheTeamAndItsReportIsReadPageByPage()
    {
        var game = NewGame();
        game.Arrive("GalacticHQ2F", 8, 3);
        var hurt = game.Party.Members[0];
        // From beside either bed, and from the foot of the western one; the eastern bed's foot hides a Poké Doll instead
        foreach (var (x, y, facing) in new[] { (40, 4, Direction.Right), (40, 4, Direction.Left), (44, 4, Direction.Left), (40, 5, Direction.Up) })
        {
            hurt.CurrentHP = 1;
            hurt.Status = StatusCondition.Poison;
            var rest = Read(game, x, y, facing);
            Assert.Contains(rest.Asked, a => a.Answer == "Yes");
            Assert.Contains(rest.Log, l => l.StartsWith("fanfare"));
            Assert.Equal((hurt.MaxHP, StatusCondition.None), (hurt.CurrentHP, hurt.Status));
            Assert.Contains("fade out", rest.Log);
            Assert.Contains("fade in", rest.Log);
        }
        hurt.CurrentHP = 1;
        game.Answers.Enqueue(1);
        Assert.Single(Read(game, 40, 4, Direction.Right).Asked);
        Assert.Equal(1, hurt.CurrentHP);
        Assert.False(game.Map.SignScripts.ContainsKey((44, 5)));
        Assert.Equal("Poké Doll", game.Map.HiddenItems[(44, 5)].Item);
        Assert.Contains(game.Map.Props, p => p.Type == PropType.Bed && p.X == 40 && p.Y == 3 && p.Depth == 3);
        Assert.Contains(game.Map.Props, p => p.Type == PropType.Bed && p.X == 44 && p.Y == 3 && p.Depth == 3);

        // The report on the kitchen table: not read, read to its first page, read through
        game.Answers.Enqueue(1);
        var unread = Read(game, 41, 17, Direction.Right);
        Assert.DoesNotContain(unread.Transcript, l => l.Text.Contains("DIALGA"));
        Assert.Contains(unread.Transcript, l => l.Text.Contains("put the report back"));
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(1);
        var half = Read(game, 41, 17, Direction.Right);
        Assert.Contains(half.Transcript, l => l.Text.Contains("DIALGA"));
        Assert.DoesNotContain(half.Transcript, l => l.Text.Contains("PALKIA"));
        var whole = Read(game, 41, 17, Direction.Right);
        Assert.Contains(whole.Transcript, l => l.Text.Contains("DIALGA"));
        Assert.Contains(whole.Transcript, l => l.Text.Contains("PALKIA"));
        Assert.Contains(whole.Transcript, l => l.Text.Contains("put the report back"));

        // The rooms' signs and notes are read with no name on the box
        foreach (var (x, y, text) in new[] { (42, 2, "Nap Room"), (31, 18, "TV Room"), (9, 19, "creed"), (39, 14, "fridge"), (41, 12, "sink"), (42, 12, "sink") })
        {
            Assert.Equal("Notice", game.Map.SignScripts[(x, y)]);
            Assert.Contains(text, game.Map.Signboards[(x, y)]);
        }
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("Notice", "GalacticHQ2F")!, own: new[] { game.Map.Signboards[(42, 2)] });
        runner.RunToEnd();
        Assert.Equal(((string?)null, game.Map.Signboards[(42, 2)]), Assert.Single(host.Transcript));
    }

    [Fact]
    public void TheFourthFloorsMachineHoldsTheThreeReportsOneToAReading()
    {
        var game = NewGame();
        game.Arrive("GalacticHQ4F", 4, 15);
        Assert.Contains(game.Map.Props, p => p.Type == PropType.Computer && p.X == 1 && p.Y == 6);
        foreach (var (answer, words) in new[] { (0, "explosion"), (1, "Poké Ball"), (2, "crystals") })
        {
            game.Answers.Enqueue(answer);
            var read = Read(game, 1, 6, Direction.Left);
            Assert.Contains(read.Transcript, l => l.Text.Contains(words));
            Assert.Contains(read.Transcript, l => l.Text.Contains("stops there"));
            Assert.Single(read.Asked);
        }
        game.Answers.Enqueue(3);
        Assert.DoesNotContain(Read(game, 1, 6, Direction.Left).Transcript, l => l.Text.Contains("stops there"));
        Assert.Contains("green panel", game.Map.Signboards[(21, 14)]);
        Assert.Equal("Notice", game.Map.SignScripts[(21, 14)]);
    }

    // ------------------------------------------------------------------ the hall

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    public void CyrusSpeaksInTheHallAndTheRanksFileOut(int y)
    {
        var game = NewGame();
        var speech = HearTheSpeech(game, y);
        // Looker calls the player over, and they watch from the back, side by side
        Assert.Equal("Looker", speech.Transcript[0].Speaker);
        Assert.Equal((20, 12), game.Tile);
        Assert.Contains("camera Pan 13 8", speech.Log);
        Assert.Contains("camera Release", speech.Log);
        // The speech, on the original's beats: the unfinished world, who will change it, glory
        var cyrus = speech.Transcript.Where(l => l.Speaker == "Cyrus").Select(l => l.Text).ToList();
        Assert.True(cyrus.Count >= 10);
        Assert.Contains(cyrus, t => t.Contains("unfinished"));
        Assert.Contains(cyrus, t => t.Contains("must change"));
        Assert.Contains(cyrus, t => t.Contains("Mt. Coronet"));
        Assert.Contains(cyrus, t => t.Contains("Glory to Team Galactic"));
        Assert.Contains(speech.Transcript, l => l.Speaker == "Galactic Grunts");
        var after = speech.Transcript.SkipWhile(l => l.Speaker != "Galactic Grunt").Where(l => l.Speaker == "Looker").Select(l => l.Text).ToList();
        Assert.Contains(after, t => t.Contains("twenty-seven"));
        Assert.Contains(after, t => t.Contains("International Police"));

        // Cyrus has come and gone by the dais's panel, the ranks have filed out and Looker has gone by the west panel
        Assert.Equal(1, game.Story.Var("VAR_GALACTIC_HQ_HALL_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS", "FLAG_HIDE_GALACTIC_HQ_HALL_LOOKER" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Empty(game.Map.NPCs);
        Assert.Equal((17, 3), (game.Map.Everyone.Single(n => n.Key == "cyrus").GridX, game.Map.Everyone.Single(n => n.Key == "cyrus").GridY));
        Assert.Equal((1, 12), (game.Map.Everyone.Single(n => n.Key == "looker").GridX, game.Map.Everyone.Single(n => n.Key == "looker").GridY));
        // Three rides of a panel are heard: Cyrus onto the dais and off it, and Looker out of the hall
        Assert.Equal(3, speech.Log.Count(l => l == "sound warp"));
        Assert.False(game.Fires("Speech"));
        // The hall can be crossed now, from one panel to the other
        var crossed = new HashSet<(string, int, int)>();
        FloodOneRoom(game.Map, 24, 6, crossed);
        Assert.Contains(("GalacticHQHall", 1, 12), crossed);
        // And nothing is said on coming back
        game.Arrive("GalacticHQHall", 1, 12);
        Assert.Empty(game.Last.Transcript);
    }

    // ------------------------------------------------------------------ Cyrus

    [Theory]
    [InlineData(8, PlayerLook.Boy)]
    [InlineData(9, PlayerLook.Girl)]
    public void CyrusIsBeatenGivesTheMasterBallAndLeavesByThePanel(int x, PlayerLook look)
    {
        var game = NewGame(look: look);
        WithTheGalacticKey(game);
        var scene = MeetCyrus(game, x);
        Assert.Contains("battle galactic_boss_cyrus_galactic_hq Won", scene.Log);
        var said = scene.Transcript.Where(l => l.Speaker == "Cyrus").Select(l => l.Text).ToList();
        Assert.Contains(said, t => t.Contains("A lie"));
        Assert.Contains(said, t => t.Contains("Uxie, Mesprit and Azelf"));
        Assert.Contains(said, t => t.Contains("regret"));
        Assert.Contains(said, t => t.Contains("Mt. Coronet"));
        Assert.Contains(said, t => t.Contains("warp panel"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("Master Ball")));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Master Ball") && l.Speaker != "Cyrus");
        Assert.True(game.Story.HasDefeated("galactic_boss_cyrus_galactic_hq"));
        Assert.Equal(1, game.Story.Var("VAR_GALACTIC_HQ_4F_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_GALACTIC_HQ_4F_CYRUS"));
        Assert.Null(game.Present("cyrus"));
        // He went by the panel to the laboratory, where the player may follow; the player stood where they were
        var gone = game.Map.Everyone.Single(n => n.Key == "cyrus");
        Assert.Equal((22, 3), (gone.GridX, gone.GridY));
        Assert.Contains("sound warp", scene.Log);
        Assert.Equal((x, 11), game.Tile);
        Assert.False(game.Fires("Cyrus"));
        Assert.Equal(("GalacticHQLaboratory", 1, 11), (game.Map.GetWarpAt(22, 3)!.TargetMap, game.Map.GetWarpAt(22, 3)!.TargetX, game.Map.GetWarpAt(22, 3)!.TargetY));
        Assert.Equal(("GalacticHQHall", 17, 3), (game.Map.GetWarpAt(8, 3)!.TargetMap, game.Map.GetWarpAt(8, 3)!.TargetX, game.Map.GetWarpAt(8, 3)!.TargetY));
    }

    [Fact]
    public void ABattleLostToCyrusLeavesHimAtHisDesk()
    {
        var game = NewGame(BattleOutcome.Lost);
        WithTheGalacticKey(game);
        var scene = MeetCyrus(game);
        Assert.Contains("battle galactic_boss_cyrus_galactic_hq Lost", scene.Log);
        Assert.Equal(0, game.Bag.GetQuantity(Item("Master Ball")));
        Assert.Equal(0, game.Story.Var("VAR_GALACTIC_HQ_4F_STATE"));
        Assert.False(game.Story.Has("FLAG_HIDE_GALACTIC_HQ_4F_CYRUS"));
        // He had walked up to the player: come to again, he is back at his desk and the scene waits
        Assert.NotEqual((8, 8), (game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY));
        game.Arrive("GalacticHQ4F", 4, 15);
        Assert.Equal((8, 8, Direction.Up), (game.Present("cyrus")!.GridX, game.Present("cyrus")!.GridY, game.Present("cyrus")!.Facing));
        Assert.True(game.Fires("Cyrus"));
    }

    // ------------------------------------------------------------------ the laboratory and the control room

    [Fact]
    public void TheLaboratoryHasItsTanksAndItsTwoScientists()
    {
        var game = NewGame();
        game.Arrive("GalacticHQLaboratory", 1, 11);
        var tanks = game.Map.Props.Where(p => p.Type == PropType.Tank).ToList();
        Assert.Equal(10, tanks.Count);
        Assert.All(tanks, t => Assert.Equal((2, 2), (t.Width, t.Depth)));
        // Each tank of the back row is read from in front of it
        foreach (int x in new[] { 10, 11, 17, 18, 21, 22, 25, 26, 29, 30 })
        {
            Assert.Contains("green fluid", game.Map.Signboards[(x, 9)]);
            Assert.Contains(tanks, t => t.X <= x && x < t.X + 2 && t.Y == 8);
            Assert.True(game.Map.IsWalkable(x, 10));
        }
        Assert.Contains(game.Talk("scientist_m").Transcript, l => l.Text.Contains("too far"));
        Assert.Contains(game.Talk("scientist_f").Transcript, l => l.Text.Contains("thing we made"));
    }

    private static OpeningTests.Game InTheControlRoom(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = NewGame(fight);
        WithTheGalacticKey(game);
        Run(game, "GalacticHQ4F", "setvar VAR_GALACTIC_HQ_4F_STATE 1\nsetflag FLAG_HIDE_GALACTIC_HQ_4F_CYRUS");
        game.Arrive("GalacticHQControlRoom", 8, 13);
        return game;
    }

    [Theory]
    [InlineData(Direction.Up, 9, 6, Direction.Down)]
    [InlineData(Direction.Left, 8, 7, Direction.Up)]
    public void SaturnIsBeatenAndStepsAwayFromTheButton(Direction facing, int asideX, int asideY, Direction looks)
    {
        var game = InTheControlRoom();
        // Before: the button can't be come to (he stands on the one tile it is pressed from), and nothing calls anyone back
        Assert.Equal("Button", game.Map.SignScripts[(8, 5)]);
        Assert.NotNull(game.Map.GetNpcAt(8, 6));
        Assert.True(game.Map.IsSolid(7, 5) && game.Map.IsSolid(9, 5) && game.Map.IsSolid(8, 4));
        Assert.False(game.Fires("CallBack"));
        // Charon needles Saturn, who answers him
        var needling = game.Talk("charon");
        Assert.Contains(needling.Transcript, l => l.Speaker == "Charon" && l.Text.Contains("Fuhyahya"));
        Assert.Contains(needling.Transcript, l => l.Speaker == "Saturn" && l.Text.Contains("Old man"));

        var battle = TalkFrom(game, "saturn", facing);
        Assert.Contains("battle commander_saturn_galactic_hq Won", battle.Log);
        Assert.Contains(battle.Transcript, l => l.Speaker == "Saturn" && l.Text.Contains("payback"));
        Assert.Contains(battle.Transcript, l => l.Text.Contains("sets them free"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN"));
        Assert.True(game.Story.HasDefeated("commander_saturn_galactic_hq"));
        Assert.Equal(1, game.Story.Var("VAR_GALACTIC_HQ_CONTROL_ROOM_STATE"));
        var saturn = game.Present("saturn")!;
        Assert.Equal((asideX, asideY, looks), (saturn.GridX, saturn.GridY, saturn.Facing));
        Assert.Null(game.Map.GetNpcAt(8, 6));

        // Making for the door, the player is called back a step
        game.Tile = (8, 13);
        var called = game.Step("CallBack");
        Assert.Contains(called.Transcript, l => l.Speaker == "Saturn" && l.Text.Contains("Press the button"));
        Assert.Equal((8, 12), game.Tile);
        // Spoken to again he says the same and battles nobody, and Charon has other thoughts
        var again = TalkFrom(game, "saturn", facing == Direction.Up ? Direction.Left : Direction.Up);
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
        Assert.Contains(again.Transcript, l => l.Text.Contains("Press the button"));
        Assert.Contains(game.Talk("charon").Transcript, l => l.Text.Contains("genius of Charon"));

        // A game saved here and loaded finds him aside (the room's own arrival script), and the button free
        game.Arrive("GalacticHQControlRoom", 8, 13);
        Assert.Equal((9, 6), (game.Present("saturn")!.GridX, game.Present("saturn")!.GridY));
        Assert.Null(game.Map.GetNpcAt(8, 6));
    }

    [Fact]
    public void ABattleLostToSaturnLeavesHimBeforeTheButton()
    {
        var game = InTheControlRoom(BattleOutcome.Lost);
        var battle = TalkFrom(game, "saturn", Direction.Up);
        Assert.Contains("battle commander_saturn_galactic_hq Lost", battle.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN"));
        Assert.Equal(0, game.Story.Var("VAR_GALACTIC_HQ_CONTROL_ROOM_STATE"));
        Assert.Equal((8, 6), (game.Present("saturn")!.GridX, game.Present("saturn")!.GridY));
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Left)]
    public void TheButtonSetsTheThreeFreeAndTeamGalacticLeavesTheBuilding(Direction foughtFacing)
    {
        var game = InTheControlRoom();
        TalkFrom(game, "saturn", foughtFacing);
        // A no leaves everything as it is
        game.Answers.Enqueue(1);
        var no = Read(game, 8, 5, Direction.Up);
        Assert.Contains(no.Asked, a => a.Answer == "No");
        Assert.False(game.Story.Has("FLAG_FREED_GALACTIC_HQ_POKEMON"));
        Assert.NotNull(game.Present("uxie"));

        var freed = Read(game, 8, 5, Direction.Up);
        Assert.Contains(freed.Transcript, l => l.Speaker == null && l.Text.Contains("pressed the button"));
        foreach (string cry in new[] { "cry Mesprit", "cry Azelf", "cry Uxie" }) Assert.Contains(cry, freed.Log);
        // Saturn tells of the Red Chain and is gone, and the three with him
        Assert.Contains(freed.Transcript, l => l.Speaker == "Saturn" && l.Text.Contains("Red Chain"));
        Assert.Contains(freed.Transcript, l => l.Text.Contains("Mt. Coronet"));
        foreach (string key in new[] { "uxie", "mesprit", "azelf", "saturn" }) Assert.Null(game.Present(key));
        Assert.NotNull(game.Present("charon"));
        // Out of their machines, two tiles toward the middle of the room, before they went
        foreach (var (key, x, y) in new[] { ("mesprit", 2, 6), ("azelf", 14, 6), ("uxie", 8, 12) })
            Assert.Equal((x, y), (game.Map.Everyone.Single(n => n.Key == key).GridX, game.Map.Everyone.Single(n => n.Key == key).GridY));
        foreach (string flag in new[] { "FLAG_FREED_GALACTIC_HQ_POKEMON", "FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_LAKE_GUARDIANS", Team, "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING" })
            Assert.True(game.Story.Has(flag), flag);
        // What the original sets for Mt. Coronet: the grunt back in its northern hall, the painting in shards
        foreach (string flag in new[] { "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F", "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS" })
            Assert.False(game.Story.Has(flag), flag);
        Assert.Equal(1, game.Story.Var("VAR_MT_CORONET_2F_STATE"));
        Assert.Equal(0, game.Story.Var("VAR_GALACTIC_HQ_CONTROL_ROOM_STATE"));
        Assert.False(game.Fires("CallBack"));

        // The button does nothing more, and the machines say who they held
        var again = Read(game, 8, 5, Direction.Up);
        Assert.Empty(again.Asked);
        Assert.Contains(again.Transcript, l => l.Text.Contains("pressed already"));
        foreach (var (x, y, who) in new[] { (8, 10, "Uxie"), (2, 4, "Mesprit"), (14, 4, "Azelf") })
        {
            Assert.Null(game.Map.GetNpcAt(x, y));
            Assert.Equal("Notice", game.Map.SignScripts[(x, y)]);
            Assert.Contains(who + " was held", game.Map.Signboards[(x, y)]);
        }

        // Team Galactic has left every floor: its trainers, its scientists, and the bollards on the way out
        foreach (string room in RoomPlanTests.GalacticHeadquarters)
        {
            game.Arrive(room, game.Maps.Values.SelectMany(m => m.Warps).First(w => w.TargetMap == room).TargetX, game.Maps.Values.SelectMany(m => m.Warps).First(w => w.TargetMap == room).TargetY);
            Assert.DoesNotContain(game.Map.NPCs, n => n.HiddenBy == Team);
            Assert.DoesNotContain(game.Map.NPCs, n => n.IsTrainer);
        }
        game.Arrive("GalacticHQ4F", 22, 3);
        Assert.Null(game.Present("bollard_west"));
        Assert.Null(game.Present("bollard_east"));
        // The lobby's four stay, as the original's do
        game.Arrive("GalacticHQ1F", 41, 14);
        foreach (string key in new[] { "grunt_f_1", "scientist_m_1", "grunt_f_2", "grunt_m" }) Assert.NotNull(game.Present(key));
    }

    // ------------------------------------------------------------------ the half-chapter in order

    [Fact]
    public void TheHalfChapterPlaysThroughInOrder()
    {
        var game = NewGame();
        PastLakeAcuity(game);
        // Veilstone City: the key, Looker's question, the warehouse
        TakeTheStorageKey(game, Direction.Right);
        OpenTheWarehouse(game);
        // Down the stairs, and by the stairs and panels to the Galactic Key
        game.Through("GalacticHQB2F");
        Assert.Equal((4, 16), game.Tile);
        game.Arrive("GalacticHQB2F", 49, 3);
        var ball = game.Map.NPCs.Single(n => n.Item == "Galactic Key");
        game.Play(Scripts.Find(FieldScripts.For(ball)!, game.Map.Name)!, ball);
        // The basement's shutter from the key's side, then the others on the way up
        TalkFrom(game, "galactic_hq_door_west", Direction.Up);
        game.Arrive("GalacticHQ1F", 20, 14);
        TalkFrom(game, "galactic_hq_door_west", Direction.Down);
        game.Arrive("GalacticHQ3F", 16, 3);
        TalkFrom(game, "galactic_hq_door_east", Direction.Down);
        foreach (string flag in new[] { "FLAG_HIDE_GALACTIC_HQ_B2F_DOOR", "FLAG_HIDE_GALACTIC_HQ_1F_DOOR", "FLAG_HIDE_GALACTIC_HQ_3F_DOOR" }) Assert.True(game.Story.Has(flag), flag);
        // The hall, by the second floor's panel
        HearTheSpeech(game);
        // The fourth floor: its shutter, Cyrus, the Master Ball
        game.Arrive("GalacticHQ4F", 4, 15);
        TalkFrom(game, "galactic_hq_door_west", Direction.Up);
        MeetCyrus(game);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Master Ball")));
        // By his panel to the laboratory and through its doorway to the control room
        var panel = game.Map.GetWarpAt(22, 3)!;
        game.Arrive(panel.TargetMap, panel.TargetX, panel.TargetY);
        game.Through("GalacticHQControlRoom");
        Assert.Equal((8, 13), game.Tile);
        game.Talk("saturn");
        Read(game, 8, 5, Direction.Up);
        Assert.True(game.Story.Has("FLAG_FREED_GALACTIC_HQ_POKEMON"));
        // Out by the laboratory, the fourth floor's green panel and the building's third door
        game.Through("GalacticHQLaboratory");
        Assert.Equal((36, 3), game.Tile);
        game.Arrive("GalacticHQ4F", 22, 3);
        var green = game.Map.GetWarpAt(21, 17)!;
        game.Arrive(green.TargetMap, green.TargetX, green.TargetY);
        Assert.Equal(("GalacticHQ1F", (41, 14)), (game.Map.Name, game.Tile));
        game.Through("Sinnoh");
        // Nothing of the half-chapter starts again
        game.Arrive("VeilstoneGalacticWarehouse", 8, 11);
        Assert.False(game.Fires("LookerOpensDoor"));
        game.Arrive("GalacticHQHall", 24, 6);
        Assert.False(game.Fires("Speech"));
        game.Arrive("GalacticHQ4F", 4, 15);
        Assert.False(game.Fires("Cyrus"));
    }

    // ------------------------------------------------------------------ the way through

    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() =>
    {
        var game = new OpeningTests.Game();
        return WorldWalk.From(name => game.Maps.TryGetValue(name, out var map) ? map : MapDatabase.Get(name), RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!);
    });

    [Fact]
    public void TheChaptersPlacesAreReached()
    {
        var walked = Walked.Value;
        HashSet<(int X, int Y)> Of(string name) => walked[walked.Keys.Single(m => m.Name == name)];
        // The terrace round the grunt, the building's three doors, the way Looker comes up
        foreach (var tile in new[] { (721, 594), (721, 592), (720, 593), (722, 593), (714, 590), (717, 590), (720, 590), (723, 601) }) Assert.Contains(tile, Of("Sinnoh"));
        // Every floor, and the places its scenes play in
        foreach (string room in RoomPlanTests.GalacticHeadquarters) Assert.NotEmpty(Of(room));
        Assert.Contains((8, 8), Of("VeilstoneGalacticWarehouse"));
        Assert.Contains((13, 3), Of("VeilstoneGalacticWarehouse"));
        Assert.Contains((21, 5), Of("GalacticHQB2F"));
        Assert.Contains((20, 12), Of("GalacticHQHall"));
        Assert.Contains((13, 7), Of("GalacticHQHall"));
        Assert.Contains((8, 11), Of("GalacticHQ4F"));
        Assert.Contains((21, 17), Of("GalacticHQ4F"));
        Assert.Contains((36, 2), Of("GalacticHQLaboratory"));
        Assert.Contains((8, 6), Of("GalacticHQControlRoom"));
        Assert.Contains((41, 14), Of("GalacticHQ1F"));
        Assert.Contains((40, 4 + 2), Of("GalacticHQ2F"));
        // Every warp of every floor is come to
        foreach (string room in RoomPlanTests.GalacticHeadquarters)
            foreach (var warp in Room(room).Warps)
                Assert.True(Of(room).Contains((warp.SourceX, warp.SourceY)), $"{room}: the way at {warp.SourceX},{warp.SourceY} isn't reached");
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheHeadquartersHidesItsPeopleAndKnowsWhetherTheLakeHasBeenSeen()
    {
        string[] always = { "FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN" };
        Assert.True(StoryState.CurrentVersion >= 14);

        // Not yet at Lake Acuity: the grunt with the Storage Key is kept off the terrace until its scene
        var before = new StoryState();
        StoryMigration.Upgrade(before, 13, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(before.Has(flag), flag);
        Assert.True(before.Has(StoryMigration.StorageKeyGruntFlag));

        // The scene at Lake Acuity already played: it cleared his flag when the flag meant nothing, so he is there
        var after = new StoryState();
        after.SetVar("VAR_LAKE_ACUITY_STATE", 2);
        StoryMigration.Upgrade(after, 13, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(after.Has(flag), flag);
        Assert.False(after.Has(StoryMigration.StorageKeyGruntFlag));

        // From any older version the same holds, through the chapter's own script
        var old = new StoryState();
        StoryMigration.Upgrade(old, 12, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(old.Has(StoryMigration.StorageKeyGruntFlag));
        foreach (string flag in always) Assert.True(old.Has(flag), flag);

        // A save of today is left as it is
        var today = new StoryState();
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(today.Has(StoryMigration.StorageKeyGruntFlag));
        Assert.Empty(today.Flags);
    }

    [Fact]
    public void ANewGameStartsWithTheHeadquartersAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { StoryMigration.StorageKeyGruntFlag, "FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER" })
        {
            Assert.True(fresh.Has(flag), flag);
            Assert.True(upgraded.Has(flag), flag);
        }
        // Everyone else of the headquarters is there from the start, as the original's are, and its shutters stand
        foreach (string flag in new[] { Team, "FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS", "FLAG_HIDE_GALACTIC_HQ_HALL_LOOKER", "FLAG_HIDE_GALACTIC_HQ_4F_CYRUS", "FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_LAKE_GUARDIANS",
            "FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_CHARON", "FLAG_HIDE_GALACTIC_HQ_1F_DOOR", "FLAG_HIDE_GALACTIC_HQ_3F_DOOR", "FLAG_HIDE_GALACTIC_HQ_4F_DOOR", "FLAG_HIDE_GALACTIC_HQ_B2F_DOOR",
            "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_GALACTIC_HQ_DOOR", "FLAG_FREED_GALACTIC_HQ_POKEMON" })
        {
            Assert.False(fresh.Has(flag), flag);
            Assert.False(upgraded.Has(flag), flag);
        }
    }

    // ------------------------------------------------------------------ the art, to the style guide's numbers

    private static readonly Color DarkPanel = new(104, 116, 130, 255), Teal = new(120, 230, 220, 255), Band = new(232, 204, 76, 255), Green = new(110, 220, 120, 255);

    private static bool Same(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

    private static int Count(PixelCanvas c, Color colour)
    {
        int n = 0;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                if (Same(c.Get(x, y), colour)) n++;
        return n;
    }

    [Fact]
    public void WhatIsTeamGalacticsOwnTakesTheColoursOfTheirBuildings()
    {
        Assert.True(Same(GalacticArt.Panel.Base, DarkPanel));
        Assert.True(Same(GalacticArt.Teal, Teal) && Same(GalacticArt.Band, Band) && Same(GalacticArt.Green, Green));

        // A warp panel: a plate 28 square with a ring of light twenty across, teal, or green for the way out
        var panel = new PixelCanvas(GalacticArt.PanelSize, GalacticArt.PanelSize);
        GalacticArt.PaintPanel(panel, wayOut: false);
        var wayOut = new PixelCanvas(GalacticArt.PanelSize, GalacticArt.PanelSize);
        GalacticArt.PaintPanel(wayOut, wayOut: true);
        Assert.Equal(28, GalacticArt.PanelSize);
        Assert.True(Count(panel, Teal) >= 100 && Count(panel, Green) == 0);
        Assert.Equal(Count(panel, Teal), Count(wayOut, Green));
        Assert.Equal(0, Count(wayOut, Teal));
        Assert.Equal(Count(panel, Band), Count(wayOut, Band));
        Assert.True(Count(panel, Band) >= 20);
        // The ring is twenty across: lit at ten texels from the middle, dark at seven and at twelve
        Assert.True(Same(panel.Get(14 + 8, 14), Teal) && Same(panel.Get(14 - 9, 14), Teal));
        Assert.False(Same(panel.Get(14 + 6, 14), Teal));
        Assert.False(Same(panel.Get(14 + 11, 14), Teal));
        // Opaque all over: it is a plate, not a light
        for (int y = 0; y < 28; y++) for (int x = 0; x < 28; x++) Assert.Equal(255, panel.Get(x, y).A);

        // A shutter's leaf: a tile wide, as high as a room's inner walls are cut, the yellow band along its top
        Assert.Equal((32, PropModels.InnerWallHeight), (GalacticArt.DoorWidth, GalacticArt.DoorHeight));
        var door = new PixelCanvas(GalacticArt.DoorWidth, GalacticArt.DoorHeight);
        GalacticArt.PaintDoor(door);
        for (int x = 0; x < 32; x++) Assert.True(Same(door.Get(x, 2), Band));
        Assert.Equal(20, Count(door, Teal));
        Assert.True(Count(door, DarkPanel) > 200);
        for (int y = 0; y < door.Height; y++) for (int x = 0; x < 32; x++) Assert.Equal(255, door.Get(x, y).A);
        // The things of the map are drawn with it
        var card = ThingCards.Paint(PropType.GalacticDoor);
        Assert.Equal((32, PropModels.InnerWallHeight), (card.Width, card.Height));
        Assert.Equal(PropType.GalacticDoor, new NPC { NpcType = NPC.GalacticDoorType }.GymThing);
        Assert.True(new NPC { NpcType = NPC.GalacticDoorType }.IsThing);

        // A tank's fluid is green and nothing of it is white
        var glass = new PixelCanvas(GalacticArt.TankWidth, GalacticArt.TankGlass);
        GalacticArt.PaintTankGlass(glass, front: true);
        Assert.True(Count(glass, new Color(96, 206, 140, 255)) > 600);
        Assert.True(Count(glass, new Color(40, 104, 80, 255)) > 150);
        for (int y = 0; y < glass.Height; y++)
            for (int x = 0; x < glass.Width; x++)
                Assert.False(glass.Get(x, y) is { R: > 235, G: > 252, B: > 235 });
        // None of the headquarters' own pieces blocks by itself: the room's tiles say where one walks
        foreach (var type in new[] { PropType.WarpPanel, PropType.WarpPanelOut, PropType.Tank })
            Assert.False(new Prop { Type = type }.IsSolid);
    }

    [Fact]
    public void ARoomsWallsAreLaidInLengthsTheArtSheetHolds()
    {
        // The basement is eighty-five tiles across: wider than the art sheet, so its back wall, its top and its kerb are
        // laid in lengths, and the hall is followed across its width
        var basement = Room("GalacticHQB2F");
        Assert.Equal(85, basement.Width);
        Assert.True((basement.Width - 2) * 32 > ArtSheet.Width - 2);
        Assert.True(basement.Width - 2 > WorldRenderer.WidestRoom);
        Assert.True(WorldRenderer.HallInView < WorldRenderer.WidestRoom);
        // The rooms that were there before are framed whole, as they were
        foreach (string name in new[] { "CanalaveGym", "ContestHallLobby", "TeamGalacticEternaBuilding1F", "VeilstoneGalacticWarehouse" })
            Assert.True(Room(name).Width - 2 <= WorldRenderer.WidestRoom, name);
        foreach (string name in new[] { "GalacticHQ1F", "GalacticHQ2F", "GalacticHQ3F", "GalacticHQLaboratory" })
            Assert.True(Room(name).Width - 2 > WorldRenderer.WidestRoom, name);
        foreach (string name in new[] { "GalacticHQ4F", "GalacticHQB1F", "GalacticHQHall", "GalacticHQControlRoom" })
            Assert.True(Room(name).Width - 2 <= WorldRenderer.WidestRoom, name);
    }
}
