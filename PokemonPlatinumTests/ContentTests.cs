using ContentCheck;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// The content check (plan 16 · T8, <c>tools/ContentCheck</c>): the game's own overlays, rooms and scripts pass it,
/// and what is broken on purpose in a small world written here is named, file and thing. The world of these tests
/// is a folder of its own, laid out like <c>Data/</c>: one chunk of open grass, a town on it, a room and three
/// script files, sound as they stand (<see cref="ASoundFolderHasNoFindings"/>); each test breaks its own copy.
/// </summary>
public class ContentTests
{
    // ------------------------------------------------------------------ the repository

    [Fact]
    public void TheGamesOwnFilesPass()
    {
        var found = Checks.OfTheGame;
        Assert.True(found.Count == 0, $"{found.Count} findings in the game's files (dotnet run --project tools/ContentCheck prints them):\n" + string.Join("\n", found.Take(40)));

        // Everything was read: the whole world, with the maps the game makes of it, every room and every script
        var content = GameContent.OfTheGame;
        var sinnoh = Assert.Single(content.Regions);
        Assert.Equal(sinnoh.World.Index.Areas.Count, sinnoh.Overlays.Count);
        Assert.NotNull(sinnoh.Maps);
        Assert.True(content.Rooms.Count >= 300 && content.Rooms.All(room => room.Map != null));
        Assert.True(content.Scripts.Files.Count >= 300);
        // And the lines were measured: thousands of them, none past the box's rows
        Assert.True(found.LinesByRows.Values.Sum() > 7000);
        Assert.True(found.LinesByRows.Keys.Max() <= DialogueBox.Rows);
    }

    // ------------------------------------------------------------------ a world to break

    private sealed class Folder : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "content-check-" + Guid.NewGuid().ToString("N"));

        public void Write(string relative, string text)
        {
            string path = Path.Combine(Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public Findings Check() => Checks.All(GameContent.Load(Root));

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }

    private const string OverlayFile = "world/test/overlays/test_town.json", RoomFile = "maps/TestRoom.json";

    private const string SoundOverlay = """
        {
          "area": "test_town",
          "bgmTrack": "sinnoh/twinleaf",
          "doors": [ { "warp": 0, "map": "TestRoom", "x": 2, "y": 3, "facing": "Up" } ],
          "locked": [ 1 ],
          "people": {
            "lass": { "name": "Lass", "dialog": [ "Hello, {player}! Have you seen {rival}?" ] },
            "hiker": { "name": "Hiker", "script": "Hiker" },
            "youngster_tristan": { "name": "Tristan", "trainer": { "dialogueBefore": "Youngster Tristan: A battle!", "dialogueAfter": "Too strong!" } }
          },
          "signs": { "map_signpost": "Test Town\nA place for tests." },
          "read": { "4": "Door" },
          "triggers": [ { "trigger": 0, "script": "Stops" } ]
        }
        """;

    private static MapFile SoundRoom() => new()
    {
        Name = "TestRoom", DisplayName = "Test Room", Interior = InteriorStyle.House, Width = 5, Height = 5,
        Ground = new() { "#####", "#___#", "#___#", "#___#", "##D##" },
        Solid = new() { "#####", "#...#", "#...#", "#...#", "##.##" },
        Warps = new() { new Warp { SourceX = 2, SourceY = 4, TargetMap = "TestTown", TargetX = 10, TargetY = 11, TargetFacing = Direction.Down } },
        Signboards = new() { new MapFile.SignRecord { X = 1, Y = 0, Text = "A calendar." } },
        Npcs = new() { new MapFile.NpcRecord { Id = "mother", Name = "Mother", X = 2, Y = 1, Dialog = new() { "Mind how you go, {player}." } } }
    };

    /// <summary>A room's file as the game writes it, so it reads back as it is written.</summary>
    private static string Written(MapFile room) => GameDataFiles.Serialize(MapFile.FromMap(room.ToMap()));

    /// <summary>A folder with nothing wrong in it: what every test here begins from.</summary>
    private static Folder Sound()
    {
        var folder = new Folder();
        string row(string cell) => string.Concat(Enumerable.Repeat(cell, WorldChunkFile.Tiles));
        var chunk = new WorldChunkFile
        {
            Id = 1,
            Behaviours = Enumerable.Repeat(row("00"), WorldChunkFile.Tiles).ToList(),
            Solid = Enumerable.Repeat(row("."), WorldChunkFile.Tiles).ToList(),
            Cover = Enumerable.Repeat(row("."), WorldChunkFile.Tiles).ToList(),
            Heights = new() { new HeightPlate { Width = WorldChunkFile.Tiles, Depth = WorldChunkFile.Tiles, Height = 1f } }
        };
        var area = new WorldAreaFile
        {
            Key = "test_town", Name = "Test Town", Matrix = 1, Kind = "Town", Sign = "Town", Weather = "Clear", Camera = "Default", BattleBackground = "City",
            Warps =
            {
                new AreaWarp { X = 10, Z = 10, To = "test_room", ToWarp = 0 }, new AreaWarp { X = 12, Z = 10, To = "nowhere", ToWarp = 0 },
                new AreaWarp { X = 14, Z = 10, To = "nowhere", ToWarp = 0 }
            },
            Objects =
            {
                new AreaObject { Id = "lass", Looks = "lass", Movement = "none", X = 5, Z = 5, Facing = 1, Script = "1" },
                new AreaObject { Id = "hiker", Looks = "hiker", Movement = "none", X = 6, Z = 5, Facing = 1, Script = "2" },
                new AreaObject { Id = "sailor", Looks = "sailor", Movement = "none", X = 7, Z = 5, Facing = 1, Script = "3" },
                new AreaObject { Id = "map_signpost", Looks = "map_signpost", Movement = "none", X = 8, Z = 8, Script = "5" },
                new AreaObject { Id = "item_potion", Looks = "poke_ball", Movement = "none", X = 9, Z = 12, Item = "Potion", HiddenBy = "FLAG_OBTAINED_TEST_POTION", Script = "7000" },
                new AreaObject { Id = "youngster_tristan", Looks = "youngster", Movement = "none", X = 7, Z = 14, Facing = 1, Trainer = "normal", Sight = 3, Script = "TRAINER_YOUNGSTER_TRISTAN" },
                new AreaObject { Id = "grunt", Looks = "grunt_m", Movement = "none", X = 11, Z = 14, Facing = 1, Script = "9" }
            },
            Signs = { new AreaSign { X = 15, Z = 15, Type = 0, Script = "4" } },
            Triggers = { new AreaTrigger { X = 20, Z = 20, Width = 2, Script = "1", Variable = "VAR_TEST_STATE", Value = "0" } }
        };
        folder.Write("world/test/world.json", """{ "region": "Test", "maps": [ { "name": "TestTown", "matrix": 1, "area": "test_town" } ], "areas": [ "test_town" ] }""");
        folder.Write("world/test/matrices/001.json", """{ "id": 1, "width": 1, "height": 1, "chunks": [ "1" ] }""");
        folder.Write("world/test/chunks/001.json", GameDataFiles.Serialize(chunk));
        folder.Write("world/test/areas/test_town.json", GameDataFiles.Serialize(area));
        folder.Write(OverlayFile, SoundOverlay);
        folder.Write(RoomFile, Written(SoundRoom()));
        folder.Write("scripts/common.txt", "script Sign\n  speaker \"Sign\"\n  sayown\n  end\n");
        folder.Write("scripts/test_town.txt", "script Hiker\n  say \"The mountain is that way.\"\n  setflag FLAG_TEST_HIKER_MET\n  end\n\nscript Door\n  text \"It is locked.\"\n  end\n\nscript Stops\n  say \"Not yet!\"\n  end\n");
        folder.Write("scripts/TestRoom.txt", "script Mother\n  say \"Supper is at six.\"\n  end\n");
        return folder;
    }

    private static void Has(Findings found, Kind kind, string file, string name, string says = "")
    {
        Assert.True(found.Any(f => f.Kind == kind && f.File == file && f.Name == name && f.What.Contains(says, StringComparison.Ordinal)),
            $"no {kind} finding for '{name}' of {file}{(says.Length > 0 ? $" saying '{says}'" : "")} among:\n" + string.Join("\n", found.Select(f => $"[{f.Kind}] {f}")));
    }

    [Fact]
    public void ASoundFolderHasNoFindings()
    {
        using var folder = Sound();
        var found = folder.Check();
        Assert.True(found.Count == 0, string.Join("\n", found));
        // Its lines were read: the lass's, the trainer's two, the sign's, the room's two, the scripts' four
        Assert.Equal(10, found.LinesByRows.Values.Sum());
    }

    // ------------------------------------------------------------------ an overlay

    [Fact]
    public void WhatAnOverlayNamesThatTheAreaHasNotIsNamed()
    {
        using var folder = Sound();
        folder.Write(OverlayFile, """
            {
              "area": "tset_town",
              "bgmTrack": "sinnoh/no_such_song",
              "evolutionSites": [ "Mossy Rock" ],
              "doors": [
                { "warp": 0, "map": "NoSuchRoom", "x": 2, "y": 3 },
                { "warp": 2, "map": "TestRoom", "x": 0, "y": 0, "openedBy": "FLAG_NOTHING_SETS_THIS_DOOR" },
                { "warp": 7, "map": "TestRoom", "x": 2, "y": 3 }
              ],
              "locked": [ 9 ],
              "switches": [ { "warp": 8, "until": "FLAG_TEST_HIKER_MET" }, { "warp": 1, "from": "FLAG_TEST_HIKER_MET" } ],
              "exits": [ { "x": 100, "z": 100, "map": "TestRoom", "toX": 2, "toY": 2 } ],
              "heldBack": [ "ghost" ],
              "people": {
                "lass": { "name": "Lass", "dialogue": [ "Nobody ever hears this." ], "dialog": [ "Hello!" ] },
                "nobody": { "name": "Nobody", "dialog": [ "I am not here." ] },
                "map_signpost": { "name": "Signpost", "dialog": [ "Read me." ] },
                "item_potion": { "name": "Potion", "dialog": [ "Take me." ] },
                "sailor": { "name": "Sailor" },
                "hiker": { "name": "Hiker", "npcType": "Hikre", "script": "NoSuchScript", "shownBy": "FLAG_NOTHING_SETS_THIS" }
              },
              "signs": { "no_such_sign": "Nowhere", "lass": "Written on a lass" },
              "signScripts": { "map_signpost": "NoSuchSignScript" },
              "read": { "99": "Door" },
              "readAt": [ { "x": 500, "z": 500, "script": "Door" } ],
              "triggers": [ { "trigger": 0, "script": "NoSuchTrigger" }, { "trigger": 0, "script": "Stops" }, { "x": 40, "z": 3, "script": "Stops" } ],
              "npcs": [ { "name": "Stranger", "x": 40, "y": 3, "dialog": [ "Outside." ], "mart": "no_such_counter" } ],
              "props": [ { "type": "LampPost", "x": 60, "y": 2 } ]
            }
            """);
        folder.Write("world/test/overlays/nowhere.json", """{ "area": "nowhere", "bgmTrack": "sinnoh/twinleaf" }""");
        folder.Write("world/test/world.json", """{ "region": "Test", "maps": [ { "name": "TestTown", "matrix": 1, "area": "test_town" } ], "areas": [ "test_town" ], "swap": [] }""");
        var found = folder.Check();

        Has(found, Kind.UnknownField, "world/test/world.json", "swap", "\"swaps\"?");
        Has(found, Kind.Misplaced, OverlayFile, "area", "tset_town");
        Has(found, Kind.Misplaced, "world/test/overlays/nowhere.json", "nowhere", "no area of that name");
        Has(found, Kind.Music, OverlayFile, "bgmTrack", "sinnoh/no_such_song");
        Has(found, Kind.Data, OverlayFile, "evolutionSites", "Mossy Rock");
        Has(found, Kind.UnknownField, OverlayFile, "people.lass.dialogue", "\"dialog\"?");

        // People: someone the area hasn't, a signpost and an item's ball given lines, someone with nothing to say
        Has(found, Kind.Person, OverlayFile, "people.nobody", "never appear");
        Has(found, Kind.Person, OverlayFile, "people.map_signpost", "signpost");
        Has(found, Kind.Person, OverlayFile, "people.item_potion", "passed over");
        Has(found, Kind.Person, OverlayFile, "people.sailor", "nothing to say");
        Has(found, Kind.Person, OverlayFile, "heldBack.ghost");
        Has(found, Kind.Data, OverlayFile, "people.hiker", "'Hikre', which is no look");
        Has(found, Kind.Script, OverlayFile, "people.hiker", "NoSuchScript");
        Has(found, Kind.Flag, OverlayFile, "people.hiker", "FLAG_NOTHING_SETS_THIS");

        // Ways: a door to no map, onto a wall, past the area's warps; a locked warp and a switch past them; a switch
        // of a warp that leads nowhere; an exit from outside the area
        Has(found, Kind.Way, OverlayFile, "doors: warp 0", "'NoSuchRoom', which is no map");
        Has(found, Kind.Way, OverlayFile, "doors: warp 2", "blocked tile");
        Has(found, Kind.Flag, OverlayFile, "doors: warp 2", "FLAG_NOTHING_SETS_THIS_DOOR");
        Has(found, Kind.Way, OverlayFile, "doors: warp 7", "3 warps");
        Has(found, Kind.Way, OverlayFile, "locked: warp 9");
        Has(found, Kind.Way, OverlayFile, "switches: warp 8");
        Has(found, Kind.Way, OverlayFile, "switches: warp 1", "nothing to switch");
        Has(found, Kind.Way, OverlayFile, "exits 100,100", "not a tile of the area");

        // Places: signs that are none, a script number nothing runs, tiles and things outside the area, a trigger bound twice
        Has(found, Kind.Place, OverlayFile, "signs.no_such_sign");
        Has(found, Kind.Place, OverlayFile, "signs.lass", "no signpost");
        Has(found, Kind.Script, OverlayFile, "signScripts.map_signpost", "NoSuchSignScript");
        Has(found, Kind.Place, OverlayFile, "read.99");
        Has(found, Kind.Place, OverlayFile, "readAt 500,500");
        Has(found, Kind.Script, OverlayFile, "triggers: trigger 0", "NoSuchTrigger");
        Has(found, Kind.Place, OverlayFile, "triggers: trigger 0", "twice");
        Has(found, Kind.Place, OverlayFile, "triggers[2]", "not all the area's");
        Has(found, Kind.Place, OverlayFile, "npcs: Stranger at 40,3", "outside the area");
        Has(found, Kind.Data, OverlayFile, "npcs: Stranger at 40,3", "no_such_counter");
        Has(found, Kind.Place, OverlayFile, "props: LampPost at 60,2");

        // What is sound in it is left alone: the lass's own line, the switch's flag, which a script sets
        Assert.DoesNotContain(found, f => f.Name == "people.lass" && f.Kind != Kind.UnknownField);
        Assert.DoesNotContain(found, f => f.What.Contains("FLAG_TEST_HIKER_MET", StringComparison.Ordinal));
    }

    [Fact]
    public void ALineThatRunsOutOfTheBoxOrCannotBeDrawnIsNamed()
    {
        using var folder = Sound();
        string wide = string.Join(" ", Enumerable.Repeat("These words go on and on until the box is full and then they go on some more.", 5));
        folder.Write(OverlayFile, SoundOverlay
            .Replace("Hello, {player}! Have you seen {rival}?", wide)
            .Replace("Too strong!", "Too strong’ ♪")
            .Replace("A place for tests.", ""));
        folder.Write("scripts/test_town.txt",
            "script Hiker\n  say \"Fine.\" \"" + wide + "\"\n  ask \"Supercalifragilisticexpialidocious" + new string('W', 80) + "?\"\n  choose \"Which?\" \"" + new string('W', 70) + "\" \"No\"\n  end\n\n"
            + "script Door\n  text \"→ this way\"\n  end\n\nscript Stops\n  say \"Not yet!\"\n  end\n");
        var found = folder.Check();

        Has(found, Kind.TooWide, OverlayFile, "people.lass", "rows and the text box holds 3");
        Has(found, Kind.Character, OverlayFile, "people.youngster_tristan", "U+266A");
        Has(found, Kind.Character, OverlayFile, "people.youngster_tristan", "U+266A");
        Has(found, Kind.TooWide, "scripts/test_town.txt", "line 2 (Hiker)", "rows and the text box holds 3");
        Has(found, Kind.TooWide, "scripts/test_town.txt", "line 3 (Hiker)", "one word wider than the text box");
        Has(found, Kind.TooWide, "scripts/test_town.txt", "line 4 (Hiker)", "panel wider than the screen");
        // The font is asked for the arrows and has none of them
        Has(found, Kind.Character, "scripts/test_town.txt", "line 8 (Door)", "U+2192");
        // The sign's two rows are one line of the box, and still fit with one of them empty
        Assert.DoesNotContain(found, f => f.Name == "signs.map_signpost");

        // The same line fits with an everyday name and runs over with the widest the keyboard takes
        string close = "{player} and {rival} went up the hill to fetch a pail of water, and {player} came down again with {rival} close behind, and with {player} came {rival}, and then {player} said so to {rival}, and {rival} said that {player} had said so before, and {player} said so too.";
        Assert.Equal(3, DialogueBox.RowsOf(PlayerIdentity.Fill(close, "Lucas", PlayerLook.Boy, "Barry"), GameEngine.VirtualWidth).Count);
        Assert.Equal(4, DialogueBox.RowsOf(Lines.Filled(close), GameEngine.VirtualWidth).Count);
    }

    [Fact]
    public void ATranslatedLineThatRunsOutOfTheBoxOrCannotBeDrawnIsNamed()
    {
        // Plan 17 · N6: a language's own lines are held to the text box as the English ones are
        using var folder = Sound();
        string wide = string.Join(" ", Enumerable.Repeat("Cuvintele acestea tot merg și merg până se umple caseta, apoi mai merg puțin.", 5));
        folder.Write("text/ro/scripts/test_town.json",
            "{ \"Hiker\": [ { \"en\": [ \"The mountain is that way.\" ], \"text\": [ \"" + wide + "\" ] } ],"
            + " \"Door\": [ { \"en\": [ \"It is locked.\" ], \"text\": [ \"E încuiat ♪\" ] } ],"
            + " \"Stops\": [ { \"en\": [ \"Stale English.\" ], \"text\": [ \"" + wide + "\" ] } ] }");
        folder.Write("text/ro/world/test_town.json",
            "{ \"people\": { \"lass\": { \"dialog\": { \"en\": [ \"x\" ], \"text\": [ \"" + wide + "\" ] } } }, \"signs\": { \"map_signpost\": { \"en\": [ \"x\" ], \"text\": [ \"Orășelul Test\\nE scurt.\" ] } } }");
        var found = folder.Check();

        Has(found, Kind.TooWide, "text/ro/scripts/test_town.json", "Hiker #0", "rows and the text box holds 3");
        Has(found, Kind.Character, "text/ro/scripts/test_town.json", "Door #0", "U+266A");
        Has(found, Kind.TooWide, "text/ro/world/test_town.json", "lass", "rows and the text box holds 3");
        // A stale entry is shown in English, which the English check holds already; a sign that fits is no finding
        Assert.DoesNotContain(found, f => f.Name == "Stops #0");
        Assert.DoesNotContain(found, f => f.File == "text/ro/world/test_town.json" && f.Name == "map_signpost");
    }

    [Fact]
    public void WhatWouldStopTheGameIsNamedToo()
    {
        // A trainer the table lacks: the world would not load
        using (var folder = Sound())
        {
            folder.Write(OverlayFile, SoundOverlay.Replace("\"hiker\": { \"name\": \"Hiker\", \"script\": \"Hiker\" }",
                "\"hiker\": { \"name\": \"Hiker\", \"script\": \"Hiker\" }, \"grunt\": { \"name\": \"Grunt\", \"trainer\": { \"id\": \"no_such_grunt\", \"dialogueBefore\": \"Halt!\" } }"));
            var found = folder.Check();
            Has(found, Kind.Data, OverlayFile, "people.grunt", "no_such_grunt");
            Has(found, Kind.Unreadable, "world/test", "Test", "would not load this world");
        }
        // A trigger past the area's own
        using (var folder = Sound())
        {
            folder.Write(OverlayFile, SoundOverlay.Replace("\"trigger\": 0", "\"trigger\": 5"));
            var found = folder.Check();
            Has(found, Kind.Place, OverlayFile, "triggers: trigger 5", "1 triggers");
            Has(found, Kind.Unreadable, "world/test", "Test");
        }
        // An overlay that is no JSON, a room with a tile nobody knows, a script that is no script
        using (var folder = Sound())
        {
            folder.Write(OverlayFile, SoundOverlay.Replace("\"locked\": [ 1 ],", "\"locked\": [ 1 ]"));
            folder.Write(RoomFile, Written(SoundRoom()).Replace("#___#", "#_?_#"));
            folder.Write("scripts/TestRoom.txt", "script Mother\n  sing \"La la la.\"\n  end\n");
            var found = folder.Check();
            Has(found, Kind.Unreadable, OverlayFile, "test_town", "can't be read");
            Has(found, Kind.Unreadable, RoomFile, "TestRoom", "unknown tile code '?'");
            Has(found, Kind.Unreadable, "scripts/TestRoom.txt", "line 2");
        }
    }

    // ------------------------------------------------------------------ a room and the scripts

    [Fact]
    public void ABrokenRoomIsNamed()
    {
        using var folder = Sound();
        var room = SoundRoom();
        room.BgmTrack = "sinnoh/no_such_song";
        room.Warps.Add(new Warp { SourceX = 9, SourceY = 9, TargetMap = "NoSuchMap", TargetX = 1, TargetY = 1 });
        room.Warps.Add(new Warp { SourceX = 1, SourceY = 3, TargetMap = "TestTown", TargetX = 400, TargetY = 2, From = "FLAG_NOTHING_SETS_THIS_WAY" });
        room.Signboards.Add(new MapFile.SignRecord { X = 3, Y = 0, Text = "" });
        room.Signboards.Add(new MapFile.SignRecord { X = 2, Y = 0, Text = "", Script = "Mother" });
        room.Triggers = new() { new MapFile.TriggerRecord { X = 1, Y = 1, Script = "NoSuchScript" }, new MapFile.TriggerRecord { X = 3, Y = 3, Width = 4, Script = "Mother" } };
        room.Props.Add(new MapFile.PropRecord { Type = PropType.Table, X = 4, Y = 4, Width = 2 });
        room.Npcs.Add(new MapFile.NpcRecord { Id = "clerk", Name = "Clerk", NpcType = "Clerk Lady", X = 7, Y = 1, IsPokeMartClerk = true, Mart = "no_such_counter", Script = "NoSuchScript", HiddenBy = "FLAG_NOTHING_SETS_THIS" });
        room.Npcs.Add(new MapFile.NpcRecord
        {
            Id = "nobody", Name = "Nobody", X = 3, Y = 2, Trainer = new MapFile.TrainerRecord { Id = "no_such_trainer", DialogueBefore = "", DialogueAfter = "" }
        });
        room.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Missingno", MinLevel = 2, MaxLevel = 3, Weight = 1 });
        // Written as the game writes it, and then a field misspelt by hand: the file no longer reads back as it is written
        folder.Write(RoomFile, Written(room).Replace("\"displayName\": \"Test Room\",", "\"displayName\": \"Test Room\",\n  \"interiour\": \"House\","));
        var found = folder.Check();

        Has(found, Kind.UnknownField, RoomFile, "interiour", "\"interior\"?");
        Has(found, Kind.RoundTrip, RoomFile, "line 4", "interiour");
        Has(found, Kind.Music, RoomFile, "bgmTrack");
        Has(found, Kind.Way, RoomFile, "the warp at 9,9", "outside the room");
        Has(found, Kind.Way, RoomFile, "the warp at 9,9", "'NoSuchMap', which is no map");
        Has(found, Kind.Way, RoomFile, "the warp at 1,3", "32 by 32");
        Has(found, Kind.Flag, RoomFile, "the warp at 1,3", "FLAG_NOTHING_SETS_THIS_WAY");
        Has(found, Kind.EmptyLine, RoomFile, "the sign at 3,0");
        Has(found, Kind.Script, RoomFile, "the trigger at 1,1", "NoSuchScript");
        Has(found, Kind.Place, RoomFile, "the trigger at 3,3", "outside the room");
        Has(found, Kind.Place, RoomFile, "the Table at 4,4", "outside the room");
        Has(found, Kind.Place, RoomFile, "clerk at 7,1", "outside the room");
        Has(found, Kind.Data, RoomFile, "clerk at 7,1", "no_such_counter");
        Has(found, Kind.Data, RoomFile, "clerk at 7,1", "'Clerk Lady', which is no look");
        Has(found, Kind.Script, RoomFile, "clerk at 7,1", "NoSuchScript");
        Has(found, Kind.Flag, RoomFile, "clerk at 7,1", "FLAG_NOTHING_SETS_THIS");
        Has(found, Kind.Data, RoomFile, "nobody at 3,2", "there is nobody to battle");
        Has(found, Kind.Data, RoomFile, "wildEncounters", "Missingno");
        // A sign with words of its own that its script never says is no empty line; the room's own people are left alone
        Assert.DoesNotContain(found, f => f.Name == "the sign at 2,0");
        Assert.DoesNotContain(found, f => f.Name.StartsWith("mother", StringComparison.Ordinal));
    }

    [Fact]
    public void WhatTheScriptsNameThatIsNotThereIsNamed()
    {
        using var folder = Sound();
        folder.Write("scripts/test_town.txt",
            "script Hiker\n  give \"Potoin\"\n  call NoSuchScript\n  warp \"NoSuchMap\" 1 1 down\n  music \"sinnoh/no_such_song\"\n  end\n\nscript Door\n  text \"It is locked.\"\n  end\n\nscript Stops\n  say \"Not yet!\"\n  end\n");
        folder.Write("scripts/nowhere_at_all.txt", "script Lost\n  say \"Nobody comes here.\"\n  end\n");
        var found = folder.Check();

        Has(found, Kind.ScriptProblem, "scripts/test_town.txt", "line 2", "Potoin");
        Has(found, Kind.ScriptProblem, "scripts/test_town.txt", "line 3", "NoSuchScript");
        Has(found, Kind.ScriptProblem, "scripts/test_town.txt", "line 4", "NoSuchMap");
        Has(found, Kind.ScriptProblem, "scripts/test_town.txt", "line 5", "no_such_song");
        Has(found, Kind.Misplaced, "scripts/nowhere_at_all.txt", "nowhere_at_all", "no open area and no map");
    }

    [Fact]
    public void AFlagListedAsNeverSetIsToldOfOnceSomethingSetsIt()
    {
        // The one exception the check makes is a flag of the original's that nothing sets, here or there
        Assert.Equal(new[] { "FLAG_HIDE_ETERNA_CITY_GYM_GARDENIA" }, Checks.Flags.NeverSetOnPurpose.Keys);
        Assert.All(Checks.Flags.NeverSetOnPurpose.Values, reason => Assert.True(reason.Length > 40, "an exception names its reason"));

        using var folder = Sound();
        folder.Write("scripts/TestRoom.txt", "script Mother\n  setflag FLAG_HIDE_ETERNA_CITY_GYM_GARDENIA\n  end\n");
        Has(folder.Check(), Kind.Flag, "tools/ContentCheck/Checks.cs", "FLAG_HIDE_ETERNA_CITY_GYM_GARDENIA", "take it off");
    }

    // ------------------------------------------------------------------ measuring with no window

    [Fact]
    public void TextIsMeasuredAsTheGameMeasuresIt()
    {
        // The numbers are raylib's own for the typeface loaded at its atlas's size (held against its loader for every
        // character the interface loads, in all three weights, when the reader was written): each glyph's advance in
        // the font's units, scaled to 160 and cut to a whole number, then scaled to the size asked for
        Assert.Equal(664.5f, TextMetrics.Measure("Hello there! Welcome to the world of Pokémon!", 40, UiWeight.ExtraBold));
        Assert.Equal(0f, TextMetrics.Measure("", 40, UiWeight.ExtraBold));
        float i = TextMetrics.Measure("i", 40, UiWeight.ExtraBold), w = TextMetrics.Measure("W", 40, UiWeight.ExtraBold);
        Assert.True(w > i * 3);
        Assert.Equal(i * 2, TextMetrics.Measure("ii", 40, UiWeight.ExtraBold));
        // Under 40 the letters are set a touch apart, as UiFonts sets them
        Assert.Equal(TextMetrics.Measure("i", 34, UiWeight.Black) * 2 + 0.4f, TextMetrics.Measure("ii", 34, UiWeight.Black), 3);
        // Black is wider than Bold
        Assert.True(TextMetrics.Measure("Pokémon", 30, UiWeight.Black) > TextMetrics.Measure("Pokémon", 30, UiWeight.Bold));

        // What the interface can draw: Latin-1, Latin Extended-A, the comma-below letters, the dashes, the curly
        // quotes and the ellipsis (plan 17 · N5). No arrow (the typeface has none), no gender mark; and the
        // non-breaking space is drawn as a plain space (it has no outline in the typeface)
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            Assert.Empty(TextMetrics.Undrawable("Pokémon: 100% naïve façade × ÷ ¿? – — …", weight));
            Assert.Empty(TextMetrics.Undrawable("it’s „ro” ‘x’ ăâîșțĂÂÎȘȚ şţ ĄęŁőű", weight));
            Assert.Equal(new[] { 0x2192, 0x2642 }, TextMetrics.Undrawable("→ it’s ♂ →", weight).Select(r => r.Value));
        }
        Assert.Equal(TextMetrics.Measure("?", 40), TextMetrics.Measure("→", 40));
        Assert.Equal(TextMetrics.Measure("a b", 40), TextMetrics.Measure("a b", 40));
        Assert.NotEqual(0f, TextMetrics.Measure(" ", 40));

        // Wrapping is the interface's one way of it: a break where a word would run over, and where the text says so
        Assert.Equal(new[] { "one two", "three" }, TextMetrics.Wrap("one two three", 7, text => text.Length));
        Assert.Equal(new[] { "one", "", "two" }, TextMetrics.Wrap("one\n\ntwo", 100, text => text.Length));
        Assert.Equal(new[] { "extraordinarily", "long" }, TextMetrics.Wrap("extraordinarily long", 5, text => text.Length));
    }

    [Fact]
    public void TheTextBoxHoldsThreeRowsAndNamesAreFilledAtTheirWidest()
    {
        // The panel is 222 tall; its rows begin 58 down and are 58 apart, each 40 tall: the third ends 8 above the
        // panel's foot, a fourth would begin under it
        Assert.Equal(3, DialogueBox.Rows);
        Assert.True(DialogueBox.TextTop + (DialogueBox.Rows - 1) * DialogueBox.RowHeight + DialogueBox.TextSize <= DialogueBox.Height);
        Assert.True(DialogueBox.TextTop + DialogueBox.Rows * DialogueBox.RowHeight > DialogueBox.Height);
        Assert.Equal(1558f, DialogueBox.TextWidth(GameEngine.VirtualWidth));

        // The widest name the keyboard takes: its widest key, seven times
        Assert.Equal(PlayerIdentity.MaxNameLength, Lines.LongestName.Length);
        Assert.Single(Lines.LongestName.Distinct());
        var keyboard = new NameEntry(extras: "ĂÂÎȘȚ");
        string keys = string.Concat(Enumerable.Range(0, keyboard.RowCount).Select(keyboard.RowKeys));
        Assert.Contains(Lines.LongestName[0], keys);
        Assert.All(keys + keys.ToLowerInvariant(), key =>
            Assert.True(TextMetrics.Measure(key.ToString(), DialogueBox.TextSize, DialogueBox.TextWeight) <= TextMetrics.Measure(Lines.LongestName[..1], DialogueBox.TextSize, DialogueBox.TextWeight)));

        // (the assistant is one of two, and the wider name of the two is the girl's)
        string assistant = new[] { PlayerLook.Boy, PlayerLook.Girl }.Select(PlayerIdentity.DefaultName).MaxBy(name => TextMetrics.Measure(name, DialogueBox.TextSize, DialogueBox.TextWeight))!;
        Assert.Equal($"{Lines.LongestName} met {Lines.LongestName} and {assistant}", Lines.Filled("{player} met {rival} and {assistant}"));
        // What a script fills in as it runs is filled with the widest of its kind in the data
        Assert.Contains(Lines.Filled("{lead}"), PokemonDatabase.GetAll().Select(s => s.Name));
        Assert.Contains(Lines.Filled("{item}"), ItemDatabase.GetAll().Select(item => item.Name));
        Assert.DoesNotContain("{", Lines.Filled("{var:VAR_X} {money} {foe} {forgotten} {marsh:3}"));
    }

    // ------------------------------------------------------------------ plans

    [Fact]
    public void ARoomsPlanHasThePropsWhereTheFilePutsThem()
    {
        var file = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "PlayerHouse.json"));
        var plan = Plans.OfRoom(file.ToMap());

        // Every prop of the file is marked on its own tiles, framed in the props' colour from its north-west corner
        Assert.NotEmpty(file.Props);
        foreach (var prop in file.Props)
        {
            var mark = Assert.Single(plan.Marks, m => m.Kind == "prop" && (m.X, m.Y) == (prop.X, prop.Y) && m.Text.StartsWith(prop.Type.ToString(), StringComparison.Ordinal));
            Assert.Equal((prop.Width, prop.Depth), (mark.Width, mark.Depth));
            var (x, y) = plan.PixelOf(prop.X, prop.Y);
            // (the corner itself is under the mark's tag: the frame's top edge beyond it, and its left edge below it.
            // A way out drawn on a mat lies inside the mat's frame, and so does whoever stands on a rug)
            Assert.Equal(Plans.PropColor, plan.Picture.Get(x + prop.Width * plan.Tile - 1, y));
            Assert.Equal(Plans.PropColor, plan.Picture.Get(x, y + prop.Depth * plan.Tile - 1));
        }
        // And so are its ways out, its people and its signs, each named in the key
        Assert.All(file.Warps, warp => Assert.Single(plan.Marks, m => m.Kind == "warp" && (m.X, m.Y) == (warp.SourceX, warp.SourceY) && m.Text.Contains(warp.TargetMap)));
        Assert.All(file.Npcs, npc => Assert.Contains(plan.Marks, m => (m.X, m.Y) == (npc.X, npc.Y) && m.Text.Contains(npc.Name)));
        Assert.All(file.Signboards, sign => Assert.Single(plan.Marks, m => m.Kind == "sign" && (m.X, m.Y) == (sign.X, sign.Y)));
        Assert.Equal(plan.Marks.Count, plan.Legend.Count());
        Assert.Equal(plan.Marks.Count, plan.Marks.Select(m => m.Tag).Distinct().Count());

        // The picture is the room with a tile of margin round it, the key under it; it saves as a PNG
        Assert.True(plan.Picture.Width >= (file.Width + 2) * plan.Tile && plan.Picture.Height > (file.Height + 2) * plan.Tile);
        string path = Path.Combine(Path.GetTempPath(), "content-check-plan-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            plan.Save(path);
            Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, File.ReadAllBytes(path).Take(4));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AnAreasPlanIsDrawnFromItsOwnChunks()
    {
        var content = GameContent.OfTheGame;
        var plan = Assert.Single(Plans.Of(content, "twinleaf_town"));
        var map = content.MapNamed("Sinnoh")!;
        var bounds = map.AreaBounds("twinleaf_town")!.Value;

        // One chunk of the map of Sinnoh and a tile of margin, never the map whole
        Assert.Equal((Map.ChunkTiles, Map.ChunkTiles), (bounds.Width, bounds.Height));
        Assert.True(plan.Picture.Width < 2000 && plan.Picture.Height < 4000);
        Assert.Equal((bounds.X, bounds.Y), (plan.X, plan.Y));

        // Its people stand where the map has them, the rival on his own doorstep among them, and no one of another area
        var rival = map.Everyone.Single(n => n.Key == "rival" && n.ScriptFile == "twinleaf_town");
        Assert.Contains(plan.Marks, m => (m.X, m.Y) == (rival.GridX, rival.GridY) && m.Text.Contains("rival") && m.Text.Contains("hidden by FLAG_HIDE_TWINLEAF_TOWN_RIVAL"));
        Assert.All(plan.Marks, m => Assert.Equal("twinleaf_town", map.AreaAt(m.X, m.Y)?.Key));
        // Its houses' doors lead into their rooms, and its buildings are named
        Assert.Contains(plan.Marks, m => m.Kind == "warp" && m.Text.Contains("PlayerHouse"));
        Assert.Contains(plan.Marks, m => m.Kind == "building");

        // A rectangle of a map's tiles by itself, and what is no map
        var part = Assert.Single(Plans.Of(content, $"Sinnoh@{bounds.X + 4},{bounds.Y + 4},12,10"));
        Assert.Equal((bounds.X + 4, bounds.Y + 4), (part.X, part.Y));
        Assert.Throws<ArgumentException>(() => Plans.Of(content, "Sinnoh"));
        Assert.Throws<ArgumentException>(() => Plans.Of(content, "no_such_place"));
    }

    [Fact]
    public void ThePlansLetteringHasEveryCharacterOfAKey()
    {
        // Seven rows of five to a character, and none of the signs a key is written with is missing
        var canvas = new PokemonPlatinumEngine.Graphics.PixelCanvas(8, 8);
        foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,:-_()/><='\"+#!?[]*@")
        {
            canvas.Fill(default);
            PlanFont.Text(canvas, 0, 0, c.ToString(), new Raylib_cs.Color(0, 0, 0, 255));
            Assert.True(Enumerable.Range(0, 64).Any(at => canvas.Get(at % 8, at / 8).A > 0), $"'{c}' draws nothing");
        }
        Assert.Equal(17, PlanFont.Width("abc", 1));
    }
}
