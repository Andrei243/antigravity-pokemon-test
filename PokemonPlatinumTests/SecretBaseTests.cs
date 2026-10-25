using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's secret bases (plan 21 · H10): the spots the reader finds and the rooms their ids make, Secret Power in the
/// party menu and at a spot, the decorations and where each may stand, a second base emptying the first, the save, the
/// sellers' scripts and the walk in and out of a base.
/// </summary>
public class SecretBaseTests
{
    // Maps of this class's own, read only
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Hoenn"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static Pokemon Knowing(params string[] moves)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 30);
        pokemon.Moves.Clear();
        foreach (string move in moves) pokemon.Moves.Add(new Move(MoveDatabase.Get(move)!));
        return pokemon;
    }

    [Fact]
    public void ABasesIdNamesItsRoomAndItsKindAsEmeraldsDoes()
    {
        Assert.Equal(1, SecretBaseLayouts.IdOf("SECRET_BASE_RED_CAVE1_1"));
        Assert.Equal(13, SecretBaseLayouts.IdOf("SECRET_BASE_RED_CAVE2_3"));
        Assert.Equal(164, SecretBaseLayouts.IdOf("SECRET_BASE_TREE1_4"));
        Assert.Equal(233, SecretBaseLayouts.IdOf("SECRET_BASE_SHRUB4_3"));
        Assert.Equal(0, SecretBaseLayouts.IdOf("SECRET_BASE_NOWHERE"));
        Assert.Equal(("SecretBase_Tree1", SecretBaseKind.Tree), (SecretBaseLayouts.Of(162)!.Map, SecretBaseLayouts.Of(162)!.Kind));
        Assert.Equal(("SecretBase_BlueCave3", SecretBaseKind.BlueCave), (SecretBaseLayouts.Of(101)!.Map, SecretBaseLayouts.Of(101)!.Kind));
        Assert.Null(SecretBaseLayouts.Of(0));
        Assert.Equal(24, SecretBaseLayouts.All.Count);
        Assert.True(SecretBaseLayouts.IsBaseMap("SecretBase_Shrub4"));
        Assert.False(SecretBaseLayouts.IsBaseMap("SecretBase_Shrub5"));
    }

    [Fact]
    public void EverySpotOfHoennOpensARoomOfItsKindThatIsBuilt()
    {
        var overworld = BuiltMaps.Value["Hoenn"];
        Assert.True(overworld.SecretBaseSpots.Count >= 70, $"only {overworld.SecretBaseSpots.Count} spots");
        foreach (var ((x, y), id) in overworld.SecretBaseSpots)
        {
            var layout = SecretBaseLayouts.Of(id);
            Assert.NotNull(layout);
            Assert.Equal(TileBehavior.SecretBaseSpot, overworld.BehaviourAt(x, y));
            Assert.True(BuiltMaps.Value.ContainsKey(layout!.Map), $"{layout.Map} isn't built");
            // Someone can stand before it to use Secret Power
            Assert.True(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => overworld.IsWalkable(x + d.Item1, y + d.Item2)), $"nothing open beside the spot at {x},{y}");
        }
        // The kind the spot's constant names is the kind of the room its id makes
        foreach (string file in Directory.GetFiles(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world", "hoenn", "areas")))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), "\"type\": 3, \"script\": \"(SECRET_BASE_([A-Z_]+?)[1-4]_[1-9])\""))
            {
                var kind = SecretBaseLayouts.Of(SecretBaseLayouts.IdOf(m.Groups[1].Value))!.Kind;
                Assert.Equal(m.Groups[2].Value.Replace("_", ""), kind.ToString().ToUpperInvariant());
            }
        // and every room is some spot's
        foreach (var layout in SecretBaseLayouts.All)
            Assert.Contains(overworld.SecretBaseSpots.Values, id => id / 10 == layout.Group);
    }

    [Fact]
    public void EveryRoomHasItsPcBeforeItsTileAndADoorThatLeadsOut()
    {
        foreach (var layout in SecretBaseLayouts.All)
        {
            var room = BuiltMaps.Value[layout.Map];
            Assert.True(room.IsWalkable(layout.PcFrontX, layout.PcFrontY), $"{layout.Map}: the tile before its PC is shut");
            Assert.Equal(TileBehavior.Computer, room.BehaviourAt(layout.PcFrontX, layout.PcFrontY - 1));
            var door = Assert.Single(room.Warps);
            Assert.Equal("Hoenn", door.TargetMap);
            // (open ground: the Secret Power man stands before the first tree till he has given his TM)
            Assert.False(BuiltMaps.Value["Hoenn"].IsSolid(door.TargetX, door.TargetY), $"{layout.Map}'s door leads onto a shut tile");
            Assert.True(room.IsWalkable(door.SourceX, door.SourceY - 1), $"{layout.Map}: the tile inside its door is shut");
            Assert.Empty(room.NPCs);
        }
    }

    [Fact]
    public void SecretPowerIsUsedOnlyAtASpotAndListedOnlyInHoenn()
    {
        var story = new StoryState();
        Assert.Equal(FieldMove.SecretPower, FieldMoveRules.Of("Secret Power"));
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.SecretPower, new FieldSpot { SecretBase = true, Region = RegionDatabase.Hoenn }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.SecretPower, new FieldSpot { Region = RegionDatabase.Hoenn }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.SecretPower, new FieldSpot { SecretBase = true, Surfing = true }, story));
        Assert.Equal(FieldMoveError.Partner, FieldMoveRules.Check(FieldMove.SecretPower, new FieldSpot { SecretBase = true, Partner = true }, story));
        var knower = Knowing("Secret Power", "Tackle");
        Assert.Contains(PartyScreen.ActionsFor(knower, RegionDatabase.Hoenn), a => a.Move == FieldMove.SecretPower);
        Assert.DoesNotContain(PartyScreen.ActionsFor(knower, RegionDatabase.Sinnoh), a => a.Move == FieldMove.SecretPower);
        // The spot is the map's: in front of it, and not beside it
        var map = new Map(5, 5) { Name = "Spot" };
        map.SecretBaseSpots[(2, 1)] = 161;
        Assert.True(FieldMoveRules.SpotOf(map, 2, 2, Direction.Up, new Walker(TravelMode.OnFoot, 0f)).SecretBase);
        Assert.False(FieldMoveRules.SpotOf(map, 2, 2, Direction.Left, new Walker(TravelMode.OnFoot, 0f)).SecretBase);
    }

    /// <summary>A spot faced with a team, the base's commands answered as <paramref name="check"/> says, and the question answered yes.</summary>
    private static HeadlessScriptHost Face(Pokemon pokemon, int check, int kind = 5, int answer = 0)
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(pokemon);
        host.SecretBaseResults["check"] = check;
        host.SecretBaseResults["kind"] = kind;
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.SecretBaseSpot)!);
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void ABaseIsMadeOnlyAtASpotWithTheMove()
    {
        // Without Secret Power the spot only says what it is
        var without = Face(Knowing("Tackle"), check: 1);
        Assert.Empty(without.Asked);
        Assert.DoesNotContain("openbase open", without.Log);
        // With it, a question, the move and the base made
        var with = Face(Knowing("Secret Power"), check: 1);
        Assert.Single(with.Asked);
        Assert.Contains(with.Log, l => l.StartsWith("usemove"));
        Assert.Contains("openbase open", with.Log);
        // Answered no, nothing
        Assert.DoesNotContain("openbase open", Face(Knowing("Secret Power"), check: 1, answer: 1).Log);
        // The player's own base is gone into, move or none
        var own = Face(Knowing("Tackle"), check: 2);
        Assert.Contains("openbase enter", own.Log);
        Assert.DoesNotContain("openbase open", own.Log);
        // A base elsewhere: asked whether to move it, then asked to use the move
        var elsewhere = Face(Knowing("Secret Power"), check: 3);
        Assert.Equal(2, elsewhere.Asked.Count);
        Assert.Contains("openbase open", elsewhere.Log);
    }

    /// <summary>A room of five tiles by four with a wall over it, a rock in its middle and the tile inside its door kept clear.</summary>
    private static BaseFloor SmallRoom()
    {
        var floor = new HashSet<(int X, int Y)>();
        for (int y = 1; y <= 4; y++)
            for (int x = 0; x < 5; x++)
                if ((x, y) != (2, 2)) floor.Add((x, y));
        var wall = new HashSet<(int X, int Y)> { (0, 0), (1, 0), (2, 0), (3, 0), (4, 0) };
        return new BaseFloor { Floor = floor, Wall = wall, KeptClear = new() { (2, 4) }, Start = (1, 1) };
    }

    [Fact]
    public void ADecorationStandsOnlyWhereItsPlacementLetsItAndOverNothingElse()
    {
        var floor = SmallRoom();
        var desk = DecorationDatabase.Get("Small Desk")!;
        var poster = DecorationDatabase.Get("Ball Poster")!;
        var plant = DecorationDatabase.Get("Red Plant")!;
        var bigDoll = DecorationDatabase.Get("Snorlax Doll")!;
        Assert.True(floor.Fits(desk, 0, 1));
        Assert.False(floor.Fits(desk, 2, 2));       // the rock
        Assert.False(floor.Fits(desk, 0, 0));       // the wall
        Assert.True(floor.Fits(poster, 1, 0));
        Assert.False(floor.Fits(poster, 1, 1));     // a poster only on the wall
        Assert.True(floor.Fits(plant, 4, 0));       // a tall plant's top before the wall
        Assert.False(floor.Fits(plant, 2, 1));      // its foot on the rock
        Assert.Equal((1, 1), bigDoll.Footprint);    // a big doll stands on one tile
        Assert.True(floor.Fits(bigDoll, 3, 3));

        // In the shared decorating: set down where it fits, refused over another and on the tile kept clear
        var store = new List<StoredDecoration>();
        foreach (string name in new[] { "Small Desk", "Pikachu Doll", "Ball Poster" }) SecretBases.Add(store, DecorationDatabase.IdOf(name));
        var room = new SecretBaseSave { Id = 161 };
        var decorated = SecretBases.Room(room, store, floor);
        var deskPiece = new RoomPiece { Kind = desk.Id, Width = 1, Depth = 1, Slot = 0 };
        Assert.True(decorated.CanStand(3, 1, 1, 1, deskPiece));
        decorated.Pieces.Add(new RoomPiece { Kind = desk.Id, X = 3, Y = 1, Slot = 0 });
        var doll = new RoomPiece { Kind = DecorationDatabase.IdOf("Pikachu Doll"), Width = 1, Depth = 1, Slot = 1 };
        Assert.False(decorated.CanStand(3, 1, 1, 1, doll));     // over the desk
        Assert.False(decorated.CanStand(2, 4, 1, 1, doll));     // the tile kept clear
        Assert.False(decorated.CanStand(2, 2, 1, 1, doll));     // the rock
        Assert.True(decorated.CanStand(4, 3, 1, 1, doll));
        Assert.Equal(RoomTile.Rock, decorated.TileAt!(2, 2));
        Assert.Equal(RoomTile.Wall, decorated.TileAt!(1, 0));
        Assert.Equal(SecretBases.MostOut, decorated.Limit);

        // A session sets the poster down on the wall and keeps it
        var session = new DecoratingSession(decorated, store, SecretBases.KindOf);
        session.Confirm();                  // DECORATE
        session.Move(0, 1);
        session.Move(0, 1);                 // the poster, third in the PC
        session.Confirm();
        Assert.Equal(DecoratingStep.Placing, session.Step);
        while (session.Y > 0) session.Move(0, -1);
        session.Confirm();
        Assert.False(session.Refused);
        SecretBases.Keep(room, decorated, store);
        Assert.Equal(2, room.Placed.Count);
        Assert.True(store[0].Out && store[2].Out && !store[1].Out);
    }

    [Fact]
    public void TheDecorationPcHoldsSoManyOfAKind()
    {
        var store = new List<StoredDecoration>();
        int desk = DecorationDatabase.IdOf("Small Desk");
        for (int i = 0; i < 10; i++) Assert.True(SecretBases.Add(store, desk));
        Assert.False(SecretBases.Add(store, desk));
        Assert.True(SecretBases.Add(store, DecorationDatabase.IdOf("Red Brick")));
        Assert.Equal(120, DecorationDatabase.All.Count);
        Assert.Equal(40, DecorationDatabase.Room(DecorationCategory.Doll));
        Assert.Equal((3, 2), DecorationDatabase.Get("Heavy Desk")!.Footprint);
        Assert.Equal(DecorationPlacement.Wall, DecorationDatabase.Get("DECOR_SEA_POSTER")!.Placement);
    }

    [Fact]
    public void ASecondSpotEmptiesTheFirst()
    {
        var store = new List<StoredDecoration>();
        SecretBases.Add(store, DecorationDatabase.IdOf("Small Desk"));
        SecretBases.Add(store, DecorationDatabase.IdOf("Pikachu Doll"));
        var first = SecretBases.Open(null, store, 161, "Hoenn", 213, 19, 213, 20);
        first.Placed.Add(new PlacedDecoration { Slot = 0, X = 3, Y = 3 });
        first.Placed.Add(new PlacedDecoration { Slot = 1, X = 4, Y = 3 });
        store[0].Out = store[1].Out = true;
        var second = SecretBases.Open(first, store, 13, "Hoenn", 307, 146, 307, 147);
        Assert.Equal(13, second.Id);
        Assert.Empty(second.Placed);
        Assert.Empty(first.Placed);
        Assert.All(store, s => Assert.False(s.Out));
        Assert.Equal(2, store.Count);
        Assert.Equal("SecretBase_RedCave2", second.Layout!.Map);
    }

    [Fact]
    public void TheSaveKeepsTheBaseAndTheDecorationPc()
    {
        var store = new List<StoredDecoration> { new() { Kind = DecorationDatabase.IdOf("Red Brick"), Out = true }, new() { Kind = DecorationDatabase.IdOf("Surf Mat") } };
        var room = new SecretBaseSave { Id = 162, Map = "Hoenn", X = 286, Y = 145, OutX = 286, OutY = 146, TimesEntered = 3 };
        room.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 4 });
        var back = SaveManager.Read(SaveManager.Write(new SaveData { SecretBase = room, Decorations = store }))!;
        Assert.Equal((162, "Hoenn", 286, 145, 286, 146, 3), (back.SecretBase!.Id, back.SecretBase.Map, back.SecretBase.X, back.SecretBase.Y, back.SecretBase.OutX, back.SecretBase.OutY, back.SecretBase.TimesEntered));
        Assert.Equal((0, 2, 4), (back.SecretBase.Placed[0].Slot, back.SecretBase.Placed[0].X, back.SecretBase.Placed[0].Y));
        Assert.Equal(store.Select(s => (s.Kind, s.Out)), back.Decorations!.Select(s => (s.Kind, s.Out)));
        // A save with neither writes neither, as every older save is
        var none = SaveManager.Write(new SaveData());
        Assert.DoesNotContain("secretBase", none);
        Assert.DoesNotContain("decorations", none);
    }

    [Fact]
    public void TheWalkGoesIntoABaseAtItsSpotAndLeavesThroughItsDoor()
    {
        var layout = SecretBaseLayouts.Of(161)!;
        var room = BuiltMaps.Value[layout.Map];
        var door = room.Warps.Single();
        var walked = WorldWalk.From(MapNamed, new MapSpot(layout.Map, door.SourceX, door.SourceY - 1, Direction.Up));
        Assert.Contains(walked.Keys, m => m.Name == "Hoenn");
        // and from outside every room is come into
        var outside = WorldWalk.From(MapNamed, new MapSpot("Hoenn", 213, 20, Direction.Up));
        Assert.Contains(outside.Keys, m => m.Name == layout.Map);
    }

    [Fact]
    public void TheSellersScriptsBuyIntoTheDecorationPc()
    {
        // The first answer of each counter's first page is bought with money enough, and the PC holds it
        foreach (var (file, script, bought) in new[]
                 {
                     ("slateport_city", "DecorClerk", "Red Brick"), ("slateport_city", "DollClerk", "Azurill Doll"),
                     ("lilycove_city_department_store_5f", "Dolls", "Pichu Doll"), ("lilycove_city_department_store_5f", "Cushions", "Pika Cushion"),
                     ("lilycove_city_department_store_5f", "Posters", "Ball Poster"), ("lilycove_city_department_store_5f", "Mats", "Surf Mat")
                 })
        {
            var host = new HeadlessScriptHost { Money = 50000 };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find($"{file}.{script}")!);
            runner.RunToEnd();
            var d = DecorationDatabase.Get(bought)!;
            Assert.Equal(d.Id, Assert.Single(host.Decorations).Kind);
            Assert.Equal(50000 - d.Price, host.Money);
        }
        // Short of money, nothing is bought
        var poor = new HeadlessScriptHost { Money = 100 };
        var shop = new ScriptRunner(ScriptLibrary.Default, poor);
        shop.Start(ScriptLibrary.Default.Find("slateport_city.DollClerk")!);
        shop.RunToEnd();
        Assert.Empty(poor.Decorations);
        Assert.Equal(100, poor.Money);
    }
}
