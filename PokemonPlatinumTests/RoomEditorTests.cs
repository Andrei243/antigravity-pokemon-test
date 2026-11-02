using ContentCheck;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using RoomEditor;

namespace PokemonPlatinumTests;

/// <summary>
/// The room and overlay editor's model (plan 16 · T9, <c>tools/RoomEditor</c>), which needs no window: a room opened
/// is the file it was opened from, every change is a step undone back to the file, a Pokémon Center drawn, furnished
/// and saved from nothing loads in the game and passes the content check, and an overlay is changed a value at a
/// time with its comments and its layout kept.
/// </summary>
public class RoomEditorTests
{
    private static string RoomText(string name) =>
        File.ReadAllText(Path.Combine(GameDataFiles.Directory, MapDatabase.Folder, name + ".json")).Replace("\r\n", "\n");

    // ------------------------------------------------------------------ rooms

    [Fact]
    public void EveryRoomOfTheGameRoundTripsThroughTheDocument()
    {
        string folder = Path.Combine(GameDataFiles.Directory, MapDatabase.Folder);
        int rooms = 0;
        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            var document = EditorDocument.Open(text);
            Assert.True(text == document.Text, $"{Path.GetFileName(path)} doesn't read back through the editor as it is written");
            Assert.True(text == document.SaveText(), $"{Path.GetFileName(path)} isn't saved by the editor as it is written");
            Assert.False(document.Dirty);
            rooms++;
        }
        Assert.True(rooms > 250);
    }

    [Fact]
    public void UndoReturnsTheFileAndRedoTheEdits()
    {
        string original = RoomText("OreburghPokemonCenter");
        var document = EditorDocument.Open(original);

        document.Paint(3, 4, TileType.Planks);
        document.PlaceProp(PropType.Plant, 14, 6);
        document.PlacePerson(new MapFile.NpcRecord { Name = "Hiker", NpcType = "Hiker", X = 6, Y = 7, Dialog = new() { "Long way up." } });
        document.SetField(6, 7, ThingKind.Person, "dialog", "Long way up.|Longer way down.");
        document.PlaceSign(9, 6, "Rest here.");
        document.PlaceTrigger(6, 11, "Greeting", 3, 1, "VAR_TEST", 1);
        document.Remove(3, 3);
        document.SetOverhead(4, 8, TileType.Planks);
        string edited = document.Text;
        Assert.True(document.Dirty);
        Assert.NotEqual(original, edited);

        int steps = 0;
        while (document.CanUndo) { document.Undo(); steps++; }
        Assert.Equal(8, steps);
        Assert.Equal(original, document.Text);
        Assert.False(document.Dirty);

        while (document.CanRedo) document.Redo();
        Assert.Equal(edited, document.Text);

        // A new change after an undo forgets what could be redone
        document.Undo();
        document.Paint(5, 8, TileType.Paving);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void AStrokeIsOneStep()
    {
        var document = EditorDocument.New("StrokeRoom", 10, 8);
        string before = document.Text;
        document.BeginStroke();
        for (int x = 1; x < 9; x++) document.Paint(x, 4, TileType.Planks);
        document.EndStroke();
        document.Fill(2, 5, 3, 2, TileType.Paving);
        Assert.Equal(TileType.Planks, document.GroundAt(5, 4));
        Assert.Equal(TileType.Paving, document.GroundAt(4, 6));

        document.Undo();
        Assert.Equal(TileType.Floor, document.GroundAt(4, 6));
        Assert.Equal(TileType.Planks, document.GroundAt(5, 4));
        document.Undo();
        Assert.Equal(before, document.Text);
        Assert.False(document.CanUndo);
    }

    [Fact]
    public void WhatIsPaintedAndPlacedBlocksAsTheGameReadsIt()
    {
        var document = EditorDocument.New("BlockRoom", 10, 8);
        Assert.True(document.SolidAt(0, 3) && document.SolidAt(4, 1) && !document.SolidAt(4, 4));

        // A wall painted blocks, floor painted over it opens again
        document.Paint(4, 4, TileType.Wall);
        Assert.True(document.SolidAt(4, 4));
        document.Paint(4, 4, TileType.Floor);
        Assert.False(document.SolidAt(4, 4));

        // Furniture blocks its tiles; taken away, it opens those nothing else holds closed
        document.PlaceProp(PropType.Table, 3, 3, 2, 1);
        document.PlaceProp(PropType.Plant, 4, 3);
        Assert.True(document.SolidAt(3, 3) && document.SolidAt(4, 3));
        Assert.Equal(ThingKind.Prop, document.Remove(3, 3));
        Assert.False(document.SolidAt(3, 3));
        Assert.True(document.SolidAt(4, 3));

        // A cushion is sat on, not walked round
        document.PlaceProp(PropType.Cushion, 6, 5, block: false);
        Assert.False(document.SolidAt(6, 5));

        // The topmost thing is taken first
        document.PlaceWarp(5, 7, "Sinnoh", 1, 1);
        document.PlaceSign(5, 7, "Mind the step");
        Assert.Equal(new[] { ThingKind.Sign, ThingKind.Warp }, document.ThingsAt(5, 7));
        Assert.Equal(ThingKind.Sign, document.Remove(5, 7));
        Assert.Equal(ThingKind.Warp, document.Remove(5, 7));
        Assert.Null(document.Remove(5, 7));

        // The map the game makes agrees with what the editor shows
        var map = document.ToMap();
        Assert.True(map.IsSolid(4, 3));
        Assert.False(map.IsSolid(3, 3));
    }

    [Fact]
    public void AFieldThatCantTakeAValueChangesNothing()
    {
        var document = EditorDocument.New("FieldRoom", 8, 8);
        document.PlaceWarp(3, 7, "Sinnoh", 10, 10);
        string before = document.Text;
        Assert.Throws<ArgumentException>(() => document.SetField(3, 7, ThingKind.Warp, "targetX", "ten"));
        Assert.Throws<ArgumentException>(() => document.SetField(3, 7, ThingKind.Warp, "nothing", "1"));
        Assert.Equal(before, document.Text);
        Assert.Equal(1, Count(document));

        document.SetField(3, 7, ThingKind.Warp, "targetFacing", "up");
        Assert.Equal(Direction.Up, document.File.Warps[0].TargetFacing);
        Assert.Contains(document.Fields(3, 7, ThingKind.Warp), f => f.Label == "targetFacing" && f.Value == "Up");

        static int Count(EditorDocument d) { int n = 0; while (d.CanUndo) { d.Undo(); n++; } foreach (var _ in Enumerable.Range(0, n)) d.Redo(); return n; }
    }

    /// <summary>The plan's "done when": a Pokémon Center drawn, furnished, previewed and saved from nothing, and the game loads it.</summary>
    [Fact]
    public void APokemonCenterIsDrawnFurnishedPreviewedAndSavedAndTheGameLoadsIt()
    {
        var document = EditorDocument.New("EditorPokemonCenter", 17, 14, InteriorStyle.PokemonCenter);
        document.SetRoomField("displayName", "Pokémon Center");
        document.SetRoomField("bgmTrack", GameDataFiles.Deserialize<MapFile>(RoomText("OreburghPokemonCenter")).BgmTrack);

        // Drawn: the door in the south wall, the counter's alcoves
        document.Paint(8, 13, TileType.Door);
        foreach (int x in new[] { 1, 2, 14, 15 }) document.Paint(x, 9, TileType.Wall);
        // Furnished: the counter, the machine, the PC on the wall
        document.PlaceProp(PropType.Counter, 5, 5, 7, 1);
        document.PlaceProp(PropType.HealingMachine, 7, 4);
        document.PlaceProp(PropType.Computer, 10, 3);
        document.PlaceProp(PropType.Cushion, 3, 12, block: false);
        document.Paint(12, 3, TileType.PC);
        // Peopled: the nurse behind her counter, the PC's terminal, the way out
        document.PlacePerson(new MapFile.NpcRecord { Name = "Nurse Joy", NpcType = "Nurse", X = 8, Y = 4 });
        document.SetField(8, 4, ThingKind.Person, "nurse", "true");
        document.PlacePerson(new MapFile.NpcRecord { Name = "PC Terminal", NpcType = "Trainer", X = 12, Y = 3, IsPCTerminal = true });
        document.PlaceWarp(8, 13, "Sinnoh", 303, 757);

        // Previewed: the content check's plan of the map the game makes of it
        var plan = Plans.OfRoom(document.ToMap());
        Assert.True(plan.Picture.Width > 17 && plan.Marks.Count > 0);

        // Checked as it is saved: the content check finds nothing wrong with it
        var found = EditorChecks.Room(GameContent.OfTheGame, document);
        Assert.True(found.Count == 0, string.Join("\n", found));

        // Saved, and loaded as the game loads a room
        string folder = Path.Combine(Path.GetTempPath(), "room-editor-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = document.Save(folder);
            Assert.False(document.Dirty);
            var map = GameDataFiles.Deserialize<MapFile>(File.ReadAllText(path)).ToMap();
            Assert.Equal("EditorPokemonCenter", map.Name);
            Assert.Equal(InteriorStyle.PokemonCenter, map.Interior);
            Assert.True(map.GetNpcAt(8, 4)!.IsHealingNurse);
            Assert.True(map.GetNpcAt(12, 3)!.IsPCTerminal);
            Assert.Equal("Sinnoh", map.GetWarpAt(8, 13)!.TargetMap);
            Assert.True(map.IsSolid(9, 5) && !map.IsSolid(3, 12) && !map.IsSolid(8, 13));
            // And what was saved is the game's own writing of it: it reads back as it is written
            Assert.Equal(File.ReadAllText(path), GameDataFiles.Serialize(MapFile.FromMap(map)));
            // (a folder of one room: its door leads to a map that folder hasn't, which is all the check may say of it)
            Assert.DoesNotContain(Checks.All(GameContent.Load(folder)), f => f.File.Contains("EditorPokemonCenter") && f.Kind != Kind.Way);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void TheCheckOnSaveNamesWhatIsWrong()
    {
        var document = EditorDocument.New("BrokenRoom", 8, 8);
        document.PlacePerson(new MapFile.NpcRecord { Name = "Lost", NpcType = "Youngster", X = 3, Y = 4, Script = "NoSuchScriptAnywhere" });
        document.PlaceWarp(4, 7, "NoSuchMapAnywhere", 1, 1);
        var found = EditorChecks.Room(GameContent.OfTheGame, document);
        Assert.Contains(found, f => f.Kind == Kind.Script && f.What.Contains("NoSuchScriptAnywhere"));
        Assert.Contains(found, f => f.Kind == Kind.Way);

        // A room the game wouldn't load is refused before anything is written
        document.SetRoomField("name", "");
        Assert.Throws<InvalidDataException>(() => document.SaveText());
    }

    // ------------------------------------------------------------------ the splice

    private const string Hand = """
        {
          "area": "a",
          // the doors, written by hand
          "doors": [
            { "warp": 0, "map": "A", "x": 1, "y": 2, "facing": "Up" },
            // the second
            { "warp": 1, "map": "B", "x": 3, "y": 4, "facing": "Up" }
          ],
          "locked": [ 2, 3 ],
          "people": {
            // she stays
            "one": { "name": "One" },
            "two": { "name": "Two" }
          }
        }

        """;

    [Fact]
    public void ASpliceChangesOneValueAndKeepsTheRest()
    {
        string text = Hand.Replace("\r\n", "\n");
        string changed = JsonSplice.Set(text, "people.two", "{\n  \"name\": \"Deux\"\n}");
        Assert.Contains("// she stays", changed);
        Assert.Contains("// the second", changed);
        Assert.Contains("\"name\": \"Deux\"", changed);
        Assert.Equal("Deux", GameDataFiles.Deserialize<WorldOverlayFile>(changed).People!["two"].Name);
        // Nothing before the value moved
        int at = text.IndexOf("\"two\"", StringComparison.Ordinal);
        Assert.Equal(text[..at], changed[..at]);
    }

    [Fact]
    public void ASpliceAddsAndRemovesMembers()
    {
        string text = Hand.Replace("\r\n", "\n");
        // An element added to a one-line array stays on its line
        string locked = JsonSplice.Set(text, "locked.+", "5");
        Assert.Contains("\"locked\": [ 2, 3, 5 ]", locked);
        // A door added to a list of doors on lines of their own, on a line of its own
        string doors = JsonSplice.Set(text, "doors.+", JsonSplice.Serialize(new OverlayDoor { Warp = 4, Map = "C" }));
        Assert.Equal(3, GameDataFiles.Deserialize<WorldOverlayFile>(doors).Doors!.Count);
        Assert.Contains("\n    { \"warp\": 4, \"map\": \"C\"", doors);
        // Taken out, first, last and only
        var first = GameDataFiles.Deserialize<WorldOverlayFile>(JsonSplice.Set(text, "doors.0", null));
        Assert.Equal("B", Assert.Single(first.Doors!).Map);
        var last = GameDataFiles.Deserialize<WorldOverlayFile>(JsonSplice.Set(text, "people.two", null));
        Assert.Equal("one", Assert.Single(last.People!).Key);
        string none = JsonSplice.Set(JsonSplice.Set(text, "locked.0", null), "locked.0", null);
        Assert.Empty(GameDataFiles.Deserialize<WorldOverlayFile>(none).Locked!);
        // A container that isn't there is made
        string made = JsonSplice.Set(text, "signs.map_signpost", "\"Welcome\"");
        Assert.Equal("Welcome", GameDataFiles.Deserialize<WorldOverlayFile>(made).Signs!["map_signpost"]);
        Assert.Contains("// the doors, written by hand", made);
        // Taking away what isn't there changes nothing
        Assert.Equal(text, JsonSplice.Set(text, "exits.0", null));
    }

    // ------------------------------------------------------------------ overlays

    private static (OverlayDocument Document, string Text) Sandgem()
    {
        string data = GameDataFiles.Directory;
        var found = EditorAreas.Find(data, "sandgem_town")!.Value;
        string text = File.ReadAllText(found.OverlayPath).Replace("\r\n", "\n");
        return (new OverlayDocument("sandgem_town", found.Area, text), text);
    }

    [Fact]
    public void AnOverlayListsTheAreasObjectsWithItsEntriesBeside()
    {
        var (document, _) = Sandgem();
        var rows = document.Rows();
        Assert.Equal(document.Area.Objects.Count, rows.Count(r => r.Kind == OverlayRowKind.Person));
        Assert.Equal(document.Area.Warps.Count, rows.Count(r => r.Kind == OverlayRowKind.Door));
        Assert.Equal(document.Area.Triggers.Count, rows.Count(r => r.Kind == OverlayRowKind.Trigger));
        // The signposts the overlay gives words, by the object's id
        var signs = rows.Where(r => r.Kind == OverlayRowKind.Sign).ToList();
        Assert.NotEmpty(signs);
        Assert.All(document.Overlay.Signs!.Keys, key => Assert.Contains(signs, r => r.Key == key));
        var youngster = rows.Single(r => r.Kind == OverlayRowKind.Person && r.Key == "youngster");
        Assert.Contains("Boy", youngster.Overlay);
        Assert.Contains("2 lines", youngster.Overlay);
        Assert.Contains("RowanLab", rows.Single(r => r.Kind == OverlayRowKind.Door && r.Key == "2").Overlay);
        Assert.Equal("MeetAssistant", rows.Single(r => r.Kind == OverlayRowKind.Trigger && r.Key == "0").Overlay);
        Assert.Contains(document.Fields(OverlayRowKind.Person, "youngster"), f => f.Label == "dialog" && f.Value.Contains('|'));
    }

    [Fact]
    public void AnOverlayIsWrittenBackAValueAtATimeWithItsComments()
    {
        var (document, original) = Sandgem();
        document.SetField(OverlayRowKind.Person, "youngster", "dialog", "The sea is south.|Bring a swimmer.");
        document.SetField(OverlayRowKind.Door, "4", "x", "5");
        document.SetField(OverlayRowKind.Trigger, "0", "script", "MeetAssistantAgain");

        string text = document.Text;
        // Every comment of the file is still there, and the lines nobody touched
        foreach (string comment in original.Split('\n').Where(l => l.TrimStart().StartsWith("//")))
            Assert.Contains(comment, text);
        Assert.Contains("\"pokemon_breeder_m\": {", text);
        Assert.DoesNotContain("\"still\"", text);

        var overlay = document.Overlay;
        Assert.Equal(new[] { "The sea is south.", "Bring a swimmer." }, overlay.People!["youngster"].Dialog);
        Assert.Equal("Boy", overlay.People["youngster"].Name);
        Assert.Equal(5, overlay.Doors!.Single(d => d.Warp == 4).X);
        Assert.Equal("MeetAssistantAgain", overlay.Triggers!.Single().Script);

        // Undo returns the file
        while (document.CanUndo) document.Undo();
        Assert.Equal(original, document.Text);
        Assert.False(document.Dirty);
    }

    [Fact]
    public void AnOverlayLocksDoorsAndForgetsEntriesLeftEmpty()
    {
        var (document, original) = Sandgem();
        document.SetField(OverlayRowKind.Door, "4", "locked", "true");
        Assert.Contains(4, document.Overlay.Locked!);
        document.SetField(OverlayRowKind.Door, "4", "locked", "");
        Assert.Empty(document.Overlay.Locked!);

        // A door with no map leaves the doors; a trigger with no script leaves the triggers
        document.SetField(OverlayRowKind.Door, "4", "map", "");
        Assert.DoesNotContain(document.Overlay.Doors!, d => d.Warp == 4);
        document.SetField(OverlayRowKind.Trigger, "0", "script", "");
        Assert.Empty(document.Overlay.Triggers!);

        // A person of the area with nothing said of them is given an entry, and a name taken away takes it out again
        string id = document.Area.Objects.First(o => document.Overlay.People?.ContainsKey(o.Id) != true).Id;
        document.SetField(OverlayRowKind.Person, id, "name", "Passer-by");
        Assert.Equal("Passer-by", document.Overlay.People![id].Name);
        document.SetField(OverlayRowKind.Person, id, "name", "");
        Assert.False(document.Overlay.People!.ContainsKey(id));
        Assert.Throws<ArgumentException>(() => document.SetField(OverlayRowKind.Person, "nobody_of_this_area", "name", "X"));

        while (document.CanUndo) document.Undo();
        Assert.Equal(original, document.Text);
    }

    [Fact]
    public void TheCheckOnSaveNamesWhatIsWrongInAnOverlay()
    {
        var (document, _) = Sandgem();
        Assert.Empty(EditorChecks.Overlay(GameContent.OfTheGame, document));
        document.SetField(OverlayRowKind.Person, "youngster", "script", "NoSuchScriptAnywhere");
        document.SetField(OverlayRowKind.Door, "4", "map", "NoSuchMapAnywhere");
        var found = EditorChecks.Overlay(GameContent.OfTheGame, document);
        Assert.Contains(found, f => f.Kind == Kind.Script && f.What.Contains("NoSuchScriptAnywhere"));
        Assert.Contains(found, f => f.Kind == Kind.Way && f.What.Contains("NoSuchMapAnywhere"));
    }

    // ------------------------------------------------------------------ the overlay's own things

    private static (OverlayDocument Document, string Text) Overlay(string key)
    {
        var found = EditorAreas.Find(GameDataFiles.Directory, key)!.Value;
        string text = File.ReadAllText(found.OverlayPath).Replace("\r\n", "\n");
        return (new OverlayDocument(key, found.Area, text), text);
    }

    [Fact]
    public void ASpliceWritesAValueWrittenOnALineBackOnALine()
    {
        string text = Hand.Replace("\r\n", "\n");
        string changed = JsonSplice.Set(text, "people.two", "{\n  \"name\": \"Deux\",\n  \"dialog\": [\n    \"Hi.\"\n  ]\n}");
        Assert.Contains("\"two\": { \"name\": \"Deux\", \"dialog\": [ \"Hi.\" ] }", changed);
        // Beside records of a line each, an added record is a line
        string added = JsonSplice.Set(text, "people.three", "{\n  \"name\": \"Trois\"\n}");
        Assert.Contains("\"three\": { \"name\": \"Trois\" }", added);
        Assert.Equal("{ \"a\": [ 1, 2 ], \"b\": {}, \"c\": \"é\" }", JsonSplice.OneLine("{\"a\":[1,2],\"b\":{},\"c\":\"é\"}"));
    }

    [Fact]
    public void AnOverlaysOwnPeoplePropsExitsTriggersAndTilesReadAreAddedChangedAndTakenAway()
    {
        var (document, original) = Overlay("ancient_tomb");
        int people = document.Overlay.Npcs!.Count;

        // A person of the overlay's own, on a line beside the three of rubble, then given lines and moved
        string person = document.AddPerson(10, 12);
        Assert.Equal(people.ToString(), person);
        document.SetField(OverlayRowKind.OwnPerson, person, "dialog", "Ancient.|Very ancient.");
        document.SetField(OverlayRowKind.OwnPerson, person, "x", "11");
        var npc = document.Overlay.Npcs![people];
        Assert.Equal((11, 12), (npc.X, npc.Y));
        Assert.Equal(new[] { "Ancient.", "Very ancient." }, npc.Dialog);
        Assert.Single(document.Text.Split('\n'), line => line.Contains("\"Very ancient.\"") && line.TrimStart().StartsWith("{ \"name\": \"Someone\""));
        Assert.Contains(document.Fields(OverlayRowKind.OwnPerson, person), f => f.Label == "y" && f.Value == "12");

        // A prop, a way out, a trigger and a tile read, each in a list the file hasn't yet
        string prop = document.AddProp(PropType.Planter, 9, 12);
        document.SetField(OverlayRowKind.OwnProp, prop, "width", "2");
        string exit = document.AddExit(8, 13);
        document.SetField(OverlayRowKind.Exit, exit, "map", "AncientTomb");
        document.SetField(OverlayRowKind.Exit, exit, "facing", "Down");
        string trigger = document.AddTrigger(8, 14);
        document.SetField(OverlayRowKind.OwnTrigger, trigger, "variable", "VAR_TEST");
        document.SetField(OverlayRowKind.OwnTrigger, trigger, "value", "2");
        string read = document.AddReadAt(7, 14, "Wall");
        var overlay = document.Overlay;
        Assert.Equal(2, overlay.Props!.Single().Width);
        Assert.Equal(("AncientTomb", Direction.Down), (overlay.Exits!.Single().Map, overlay.Exits!.Single().Facing));
        Assert.Contains(overlay.Triggers!, t => t.Trigger == null && t.X == 8 && t.Z == 14 && t.Variable == "VAR_TEST" && t.Value == 2);
        Assert.Equal("Wall", overlay.ReadAt!.Single().Script);
        var rows = document.Rows();
        Assert.Contains(rows, r => r.Kind == OverlayRowKind.OwnTrigger && r.Key == trigger && r.Covers(8, 14));
        Assert.Contains(document.RowsAt(7, 14), r => r.Kind == OverlayRowKind.ReadAt && r.Key == read);

        // The area's signs: what one says, what the overlay reads it as; a door's switch
        document.SetField(OverlayRowKind.Sign, "AncientTomb_EventScript_CaveEntranceSide", "read", "WallAgain");
        Assert.Equal("WallAgain", document.Overlay.Read!["AncientTomb_EventScript_CaveEntranceSide"]);
        document.SetField(OverlayRowKind.Door, "1", "until", "FLAG_TEST");
        Assert.Equal("FLAG_TEST", document.Overlay.Switches!.Single(s => s.Warp == 1).Until);
        document.SetField(OverlayRowKind.Door, "1", "until", "");
        Assert.Empty(document.Overlay.Switches!);

        // Every comment of the file kept; taken away again, and a row of the area's isn't the overlay's to take
        foreach (string comment in original.Split('\n').Where(l => l.TrimStart().StartsWith("//")))
            Assert.Contains(comment, document.Text);
        document.Remove(OverlayRowKind.OwnPerson, person);
        Assert.Equal(people, document.Overlay.Npcs!.Count);
        Assert.Throws<ArgumentException>(() => document.Remove(OverlayRowKind.Person, "registeel"));
        Assert.Throws<FormatException>(() => document.SetField(OverlayRowKind.Exit, exit, "toX", "far"));

        while (document.CanUndo) document.Undo();
        Assert.Equal(original, document.Text);
    }

    // ------------------------------------------------------------------ the area view

    [Fact]
    public void TheAreaViewShowsAChunksLayersAndPlacesTheOverlaysOwnThings()
    {
        var (document, original) = Sandgem();
        var view = AreaView.Open(GameContent.OfTheGame, "sandgem_town");
        Assert.Equal(Map.ChunkTiles, view.Window.Width);
        Assert.True(view.InWindow(view.Window.X, view.Window.Y));

        // The layers are the map the game makes of the area's chunks
        int blocked = 0, behaviours = 0;
        for (int y = view.Window.Y; y < view.Window.Y + view.Window.Height; y++)
            for (int x = view.Window.X; x < view.Window.X + view.Window.Width; x++)
            {
                Assert.Equal(view.Map.IsSolid(x, y), view.Blocked(x, y));
                if (view.Blocked(x, y)) blocked++;
                if (view.Behaviour(x, y) != TileBehavior.None) behaviours++;
            }
        Assert.True(blocked > 0 && behaviours > 0);
        view.Layer = AreaLayer.Behaviour;

        // A door of the area stands on its tile, and is what a pick of the tile finds
        var warp = document.Area.Warps[2];
        Assert.Contains(document.RowsAt(warp.X, warp.Z), r => r.Kind == OverlayRowKind.Door && r.Key == "2");
        Assert.Contains("RowanLab", view.Describe(warp.X, warp.Z, document));

        // A person placed on an open tile of the area is the overlay's own, on that tile
        var (ox, oy) = Enumerable.Range(0, view.Window.Width * view.Window.Height)
            .Select(i => (view.Window.X + i % view.Window.Width, view.Window.Y + i / view.Window.Width))
            .First(t => view.Belongs(t.Item1, t.Item2) && !view.Blocked(t.Item1, t.Item2) && document.RowsAt(t.Item1, t.Item2).Count == 0);
        var row = view.Place(document, OverlayRowKind.OwnPerson, ox, oy);
        Assert.Equal((ox, oy), (row.X, row.Z));
        Assert.Equal((ox, oy), (document.Overlay.Npcs!.Last().X, document.Overlay.Npcs!.Last().Y));
        var trigger = view.Place(document, OverlayRowKind.OwnTrigger, ox, oy);
        Assert.Equal(new[] { OverlayRowKind.OwnPerson, OverlayRowKind.OwnTrigger }, document.RowsAt(ox, oy).Select(r => r.Kind).OrderBy(k => k));
        Assert.Throws<ArgumentException>(() => view.Place(document, OverlayRowKind.Door, ox, oy));
        Assert.DoesNotContain(EditorChecks.Overlay(GameContent.OfTheGame, document), f => f.What.Contains("Someone"));

        // A tile that isn't the area's is refused
        Assert.Throws<ArgumentException>(() => view.Place(document, OverlayRowKind.OwnPerson, -5, -5));

        // The content check's plan of the window, and the chunks round from the last to the first
        var plan = view.Plan(8);
        Assert.Contains(plan.Marks, m => m.Kind == "warp");
        var first = view.Window;
        int chunks = view.Chunks().Count();
        for (int i = 0; i < chunks; i++) view.NextChunk();
        Assert.Equal(first, view.Window);

        while (document.CanUndo) document.Undo();
        Assert.Equal(original, document.Text);
    }

    // ------------------------------------------------------------------ the round trip (--check, --roundtrip)

    [Fact]
    public void EveryRoomAndOverlayOfTheGameRoundTripsThroughTheEditor()
    {
        var (files, found) = RoundTrip.All(GameDataFiles.Directory);
        Assert.True(files > 1500, $"only {files} files");
        Assert.True(found.Count == 0, string.Join("\n", found.Take(20)));
    }

    [Fact]
    public void TheRoundTripNamesWhatTheEditorWouldChange()
    {
        // A line with a bar in it can't be written back through the field that joins lines with bars
        var area = EditorAreas.Find(GameDataFiles.Directory, "sandgem_town")!.Value.Area;
        string overlay = "{\n  \"area\": \"sandgem_town\",\n  \"people\": {\n    \"youngster\": { \"name\": \"Boy\", \"dialog\": [ \"Left | right\" ] }\n  }\n}\n";
        var found = RoundTrip.Overlay("sandgem.json", "sandgem_town", area, overlay);
        Assert.Contains(found, f => f.Contains("youngster") && f.Contains("dialog"));

        // A room written otherwise than the game writes it is told, with its line
        string room = RoomText("OreburghPokemonCenter").Replace("\"name\": ", "\"name\":  ");
        Assert.Contains(RoundTrip.Room("room.json", room), f => f.Contains("line 2"));
        Assert.Empty(RoundTrip.Room("room.json", RoomText("OreburghPokemonCenter")));
    }
}
