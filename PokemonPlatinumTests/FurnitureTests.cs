using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Furniture and the programmes (plan 08 · P8): what a television, a bookshelf, a bin and a map on a wall say when
/// looked at (<see cref="FieldScripts.ForFurniture"/>, the four common scripts), a map file's <c>tileScripts</c>, and
/// the programmes a television shows (<see cref="Broadcasts"/>, the script's <c>broadcast</c>). In the collection that
/// runs alone, since the programmes' words are English's and another test may change the language.
/// </summary>
[Collection("Shared clocks and dice")]
public class FurnitureTests
{
    // ------------------------------------------------------------------ which script a piece of furniture runs

    [Fact]
    public void ARoomsTelevisionShelfAndBinRunTheirCommonScripts()
    {
        // Twinleaf Town's north-east house: a television at 6, 3 (two tiles wide) and shelves at 8, 3 and 9, 3; the
        // counterpart's room in Sandgem Town: a bin at 4, 3. Each is a prop of the room, looked at from the south
        var house = MapDatabase.Get("TwinleafTownNortheastHouse");
        Assert.Equal(FieldScripts.Television, FieldScripts.ForFurniture(house, 6, 3, Direction.Up));
        Assert.Equal(FieldScripts.Television, FieldScripts.ForFurniture(house, 7, 3, Direction.Up));
        Assert.Equal(FieldScripts.Bookshelf, FieldScripts.ForFurniture(house, 8, 3, Direction.Up));
        Assert.Equal(FieldScripts.Bookshelf, FieldScripts.ForFurniture(house, 9, 3, Direction.Up));
        // A prop of ours is read from any side
        Assert.Equal(FieldScripts.Bookshelf, FieldScripts.ForFurniture(house, 9, 3, Direction.Left));
        var room = MapDatabase.Get("SandgemTownCounterpartHouse2F");
        Assert.Equal(FieldScripts.TrashCan, FieldScripts.ForFurniture(room, 4, 3, Direction.Up));
        // Bare floor and wall say nothing
        Assert.Null(FieldScripts.ForFurniture(house, 5, 5, Direction.Up));
        Assert.Null(FieldScripts.ForFurniture(house, -1, 3, Direction.Up));
    }

    [Fact]
    public void ATilesBehaviourMakesItFurnitureOnAMapOfTheTestsOwn()
    {
        // The imported games' rooms mark furniture by the tile's behaviour, read as the GBA games read it: a bin from
        // any side, a television, a shelf and a map on a wall only facing north
        var map = new Map(8, 8) { Name = "FurnitureTest" };
        map.SetBehaviour(1, 1, TileBehavior.TrashCan);
        map.SetBehaviour(2, 1, TileBehavior.TownMap);
        map.SetBehaviour(3, 1, TileBehavior.Television);
        map.SetBehaviour(4, 1, TileBehavior.TallBookshelf);
        map.SetBehaviour(5, 1, TileBehavior.SmallBookshelf);

        Assert.Equal(FieldScripts.TrashCan, FieldScripts.ForFurniture(map, 1, 1, Direction.Up));
        Assert.Equal(FieldScripts.TrashCan, FieldScripts.ForFurniture(map, 1, 1, Direction.Right));
        Assert.Equal(FieldScripts.TownMap, FieldScripts.ForFurniture(map, 2, 1, Direction.Up));
        Assert.Null(FieldScripts.ForFurniture(map, 2, 1, Direction.Left));
        Assert.Equal(FieldScripts.Television, FieldScripts.ForFurniture(map, 3, 1, Direction.Up));
        Assert.Null(FieldScripts.ForFurniture(map, 3, 1, Direction.Down));
        Assert.Equal(FieldScripts.Bookshelf, FieldScripts.ForFurniture(map, 4, 1, Direction.Up));
        Assert.Equal(FieldScripts.Bookshelf, FieldScripts.ForFurniture(map, 5, 1, Direction.Up));
        Assert.Null(FieldScripts.ForFurniture(map, 6, 1, Direction.Up));
    }

    [Fact]
    public void FacingFurnitureInTheGameRunsItsScriptAndATileOfItsOwnWins()
    {
        string folder = Path.Combine(Path.GetTempPath(), "furniture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(8);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 12));
            game.State = GameState.Overworld;

            // The television of Twinleaf Town's north-east house shows a programme: common.Television, in the text box
            var house = MapDatabase.Get("TwinleafTownNortheastHouse");
            game.Place(house, 6, 4, Direction.Up);
            game.Interact();
            Assert.Equal(FieldScripts.Television, game.ScriptNow);
            Assert.Equal(GameState.Dialogue, game.State);
            Read(engine);

            // Its bookshelf
            game.Place(house, 8, 4, Direction.Up);
            game.Interact();
            Assert.Equal(FieldScripts.Bookshelf, game.ScriptNow);
            Read(engine);

            // The living room's television at home keeps its own programme (the room's own TV, a signboard's script),
            // and the bedroom's keeps the opening's
            var home = MapDatabase.Get("PlayerHouse");
            game.Place(home, 6, 6, Direction.Up);
            game.Interact();
            Assert.Equal("PlayerHouse.TV", game.ScriptNow);
            Read(engine);

            // A tile with a script of its own goes before the furniture's common one
            house.TileScripts[(8, 3)] = "common.TrashCan";
            try
            {
                game.Place(house, 8, 4, Direction.Up);
                game.Interact();
                Assert.Equal("common.TrashCan", game.ScriptNow);
                Read(engine);
            }
            finally { house.TileScripts.Remove((8, 3)); }
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // ------------------------------------------------------------------ the common scripts, played

    [Fact]
    public void TheFurnituresScriptsSayTheirLines()
    {
        var bin = Play("TrashCan", new HeadlessScriptHost());
        Assert.Equal(new[] { "Nothing in it but a few scraps of paper." }, bin.Transcript.Select(t => t.Text));

        var shelf = new HeadlessScriptHost();
        shelf.Rolls.Enqueue(2);
        Play("Bookshelf", shelf);
        Assert.Equal(new[] { "Picture books, travel guides and an old atlas, all lined up neatly." }, shelf.Transcript.Select(t => t.Text));

        // A map on a wall opens the Town Map's screen; where the region has none, a line instead
        var wall = Play("TownMap", new HeadlessScriptHost());
        Assert.Contains("open TownMap", wall.Log);
        Assert.Empty(wall.Transcript);
        var faded = Play("TownMap", new HeadlessScriptHost { TownMapOpens = false });
        Assert.Equal(new[] { "It's a map of the region, but it's too faded to make out." }, faded.Transcript.Select(t => t.Text));
    }

    [Fact]
    public void ATelevisionsProgrammeOnAKnownGameGivesKnownLines()
    {
        // A game with a Pokédex, a Turtwig at the head of a team of two, three Sinnoh Badges, two hours and five
        // minutes played, a Friday at 21:05. The die's third face picks the third programme on: the trainer survey
        var host = new HeadlessScriptHost
        {
            PlayerName = "Lucas", SeenInSinnoh = 40, Caught = 12, TimeOfDay = TimeOfDay.Night, Weekday = DayOfWeek.Friday,
            Hour = 21, Minute = 5, PlayTimeSeconds = 2 * 3600 + 5 * 60 + 30
        };
        host.Story.Set(StoryState.PokedexFlag);
        foreach (string badge in new[] { "Coal", "Forest", "Cobble" }) host.Story.GiveBadge(RegionDatabase.Sinnoh, badge);
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 18, new Random(1)));
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 15, new Random(2)));

        var facts = host.Broadcast();
        Assert.Equal(new[] { Programme.Clock, Programme.Pokedex, Programme.Survey, Programme.Lottery }, Broadcasts.On(facts));

        host.Rolls.Enqueue(2);
        Play("Television", host);
        Assert.Equal(new[]
        {
            "\"Now for our trainer survey. Today we asked Lucas about the team.\"",
            "\"Lucas travels with 2 Pokémon, with Turtwig out in front at level 18.\"",
            "\"3 Badges won already. Lucas is one to watch!\"",
            "\"Time on the road so far: 2:05.\"",
            "\"Thank you, Lucas! That's our survey for today.\""
        }, host.Transcript.Select(t => t.Text));

        // The same game's other programmes, each by the die's face
        Assert.Equal(new[]
        {
            "\"It's 21:05 on Friday, and this is the time check!\"",
            "\"Night has fallen. Some Pokémon are only seen after dark, so keep your eyes open!\"",
            "\"That was the time check. Stay tuned!\""
        }, Broadcasts.Compose(facts, _ => 0));
        Assert.Equal(new[]
        {
            "\"It's the Pokédex Report! Today we look at the Pokédex of a trainer called Lucas.\"",
            "\"Lucas has seen 40 kinds of Pokémon and caught 12 of them.\"",
            "\"A fine start! There are plenty of Pokémon out there still to meet.\"",
            "\"That's all from the Pokédex Report. Until next time!\""
        }, Broadcasts.Compose(facts, _ => 1));
        // The Lottery Corner tells the day's number, the one the counter at Jubilife TV draws against
        string ticket = Lottery.TicketOf(facts.DailyNumber).ToString("D5");
        Assert.Equal($"\"It's the Lottery Corner! Today's lucky number is {ticket}.\"", Broadcasts.Compose(facts, _ => 3)[0]);
    }

    [Fact]
    public void WhatIsOnFollowsTheGame()
    {
        // Before a Pokédex or a Pokémon, only the time check and (in Sinnoh) the Lottery Corner; outside Sinnoh no
        // lottery; once a team has entered the Hall of Fame its news is on, its count told
        Assert.Equal(new[] { Programme.Clock, Programme.Lottery }, Broadcasts.On(new BroadcastFacts()));
        Assert.Equal(new[] { Programme.Clock }, Broadcasts.On(new BroadcastFacts { Region = "Kanto" }));
        var champion = new BroadcastFacts { PlayerName = "Dawn", LeagueVictories = 3, Lead = "Piplup", LeadLevel = 60, TeamSize = 1, Badges = 8, BadgesInRegion = 8 };
        Assert.Equal(Programme.HallOfFame, Broadcasts.On(champion)[^1]);
        Assert.Equal("\"Dawn's teams have entered the Hall of Fame 3 times now!\"", Broadcasts.Lines(Programme.HallOfFame, champion)[1]);
        var survey = Broadcasts.Lines(Programme.Survey, champion);
        Assert.Equal("\"Dawn travels with just one Pokémon: Piplup, at level 60.\"", survey[1]);
        Assert.Equal("\"Every Badge of the region, won! What a trainer.\"", survey[2]);
        // A die that rolls past the end is held to the last programme on
        Assert.Equal(Programme.HallOfFame, Broadcasts.Choose(champion, sides => sides + 5));
    }

    // ------------------------------------------------------------------ a map file's tile scripts

    [Fact]
    public void AMapFilesTileScriptsAreReadAndWrittenBack()
    {
        var file = MapFile.FromMap(MapDatabase.Get("TwinleafTownNortheastHouse"));
        Assert.Null(file.TileScripts);
        file.TileScripts = new List<MapFile.TileScriptRecord> { new() { X = 8, Y = 3, Script = "common.TrashCan" } };
        var map = file.ToMap();
        Assert.Equal("common.TrashCan", map.TileScripts[(8, 3)]);
        var again = MapFile.FromMap(map);
        Assert.Equal((8, 3, "common.TrashCan"), (again.TileScripts![0].X, again.TileScripts[0].Y, again.TileScripts[0].Script));

        file.TileScripts[0].X = 99;
        Assert.Throws<InvalidDataException>(() => file.ToMap());
    }

    private static HeadlessScriptHost Play(string name, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("common", name)!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    // The lines read on with the confirm button until the script has ended and the field is back
    private static void Read(GameEngine engine)
    {
        for (int f = 0; f < 60 * 60; f++)
        {
            if (engine.Drive.State == GameState.Overworld && !engine.ScriptRunning) return;
            InputManager.Feed(f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            engine.Update(1f / 60f);
        }
        Assert.Fail($"The script never ended; the game is in {engine.Drive.State}.");
    }
}
