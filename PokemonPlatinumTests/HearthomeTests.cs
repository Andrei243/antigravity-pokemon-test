using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The fourth chapter's first part (plan 02 · S7, "Hearthome and Solaceon") played through with no screen, in the order
/// a player meets it: the assistant's Vs. Seeker on Route 207, Cyrus on Mt. Coronet, Route 208's Odd Keystone, Mira in
/// Wayward Cave, Keira's Buneary at Hearthome's edge, the fisherman's walk to the Contest Hall, Keira and the player's
/// mother and Fantina in its lobby, the guide who keeps the Gym's door until then, and the rival in the gate to Route
/// 209 once the Relic Badge is won.
/// </summary>
public class HearthomeTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Story.ChooseStarter("Turtwig");
        var grotle = new Pokemon(PokemonDatabase.Get("Grotle")!, 30);
        game.Party.Add(grotle);
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    /// <summary>What Fantina's script leaves behind once she is beaten (HearthomeGymLeaderRoom.txt; the Gym is plan 01 · M9's).</summary>
    private static void WinTheRelicBadge(OpeningTests.Game game) =>
        Run(game, "hearthome_city", "givebadge relic\n setvar VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE 1\n"
            + " setflag FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_BLOCKADE\n clearflag FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL");

    [Fact]
    public void ANewGameKeepsTheChaptersPeopleOutOfSightUntilTheirScenes()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 335, 713);
        Assert.Null(game.Present("counterpart", "route_207"));
        Assert.True(game.Fires("Assistant"));
        game.Arrive("Sinnoh", 470, 726);
        Assert.Null(game.Present("keira", "hearthome_city"));
        Assert.Null(game.Present("buneary", "hearthome_city"));
        Assert.True(game.Fires("Buneary"));
        // The guide keeps the Gym's door until Fantina goes back to it; the road east is shut until she is beaten
        Assert.NotNull(game.Present("gym_guide", "hearthome_city"));
        Assert.NotNull(game.Present("pokefan_m_2", "hearthome_city"));
        Assert.Equal("ContestHallLobby", game.Map.GetWarpAt(479, 691)!.TargetMap);
        Assert.Equal("Route209GateToHearthomeCity", game.Map.GetWarpAt(505, 726)!.TargetMap);
        game.Arrive("Route209GateToHearthomeCity", 1, 7);
        Assert.Null(game.Present("rival"));
        Assert.False(game.Fires("Rival"));
    }

    [Fact]
    public void TheAssistantBringsTheVsSeekerToTheFootOfMtCoronet()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 335, 713);
        var scene = game.Step("Assistant");
        Assert.Contains(scene.Asked, a => a.Question.Contains("Which hand"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Seeker")!));
        Assert.Contains(PoketchApp.DowsingMachine, scene.Poketch.Apps);
        Assert.Contains("music common/assistant", scene.Log);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_207_COUNTERPART_TRIGGER_STATE"));
        Assert.Null(game.Present("counterpart", "route_207"));
        Assert.False(game.Fires("Assistant"));
        // Eterna's bug catcher is back to the wind once she has been met (the original's EternaCity_BugCatcher2)
        Run(game, "eterna_city", "setflag FLAG_RECEIVED_BICYCLE");
        game.Arrive("Sinnoh", 300, 533);
        var bugCatcher = game.Map.NPCs.First(n => n.Script == "BugCatcherBike");
        Assert.Contains("wind", game.Play(Scripts.Find("BugCatcherBike", "eterna_city")!, bugCatcher).Transcript.Single().Text);
    }

    [Fact]
    public void CyrusSpeaksOfTheMountainWhereSinnohBegan()
    {
        var game = NewGame();
        game.Arrive("MtCoronet1FSouth", 5, 8);
        Assert.NotNull(game.Present("cyrus"));
        var scene = game.Step("Cyrus");
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("Sinnoh began"));
        Assert.Equal(1, game.Story.Var("VAR_MT_CORONET_1F_SOUTH_STATE"));
        Assert.Null(game.Present("cyrus"));
        Assert.False(game.Fires("Cyrus"));
        game.Arrive("MtCoronet1FSouth", 5, 8);
        Assert.Null(game.Present("cyrus"));
    }

    [Fact]
    public void TheBlackBeltOnRoute208GivesTheOddKeystoneOnce()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 437, 734);
        var keystone = ItemDatabase.Get("Odd Keystone")!;
        game.Talk("black_belt", "route_208");
        Assert.Equal(1, game.Bag.GetQuantity(keystone));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_208_ODD_KEYSTONE"));
        var again = game.Talk("black_belt", "route_208");
        Assert.Equal(1, game.Bag.GetQuantity(keystone));
        Assert.Contains("Route 209", again.Transcript.Single().Text);
    }

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Down)]
    public void MiraWalksWithThePlayerToWaywardCavesWayOut(Direction arriving)
    {
        var game = NewGame();
        game.Arrive("WaywardCave1F", 41, 52);
        Assert.NotNull(game.Present("mira"));
        Assert.False(game.Fires("Exit"));

        var join = game.Talk("mira");
        Assert.Contains(join.Transcript, l => l.Text.Contains("decided to go with Mira"));
        Assert.Contains(join.Log, l => l == "partner Mira mira_wayward_cave");
        Assert.Equal(1, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        // On the way she has three things to say, the last again and again
        Assert.Contains("help", game.Talk("mira").Transcript.Single().Text, StringComparison.OrdinalIgnoreCase);
        game.Talk("mira");
        Assert.Contains("Mira will be a Trainer", game.Talk("mira").Transcript.Single().Text);
        Assert.Contains("Mira will be a Trainer", game.Talk("mira").Transcript.Single().Text);

        // At the way out she thanks the player and goes
        Assert.True(game.Fires("Exit"));
        game.Facing = arriving;
        var exit = game.Step("Exit");
        Assert.Contains(exit.Transcript, l => l.Speaker == "Mira" && l.Text.Contains("way out"));
        Assert.Contains("partner off", exit.Log);
        Assert.NotEqual((41, 53), game.Tile);
        Assert.True(game.Story.Has("FLAG_TRAVELED_WITH_MIRA"));
        Assert.Equal(2, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        Assert.Null(game.Present("mira"));
        game.Arrive("WaywardCave1F", 41, 52);
        Assert.Null(game.Present("mira"));
        Assert.False(game.Fires("Exit"));
    }

    [Fact]
    public void MiraWaitsWhereSheStoodForAPlayerWhoLeftWithoutHer()
    {
        var game = NewGame();
        game.Arrive("WaywardCave1F", 41, 52);
        game.Talk("mira");
        Run(game, "wayward_cave_1f", "partner off");
        game.Arrive("WaywardCave1F", 41, 52);
        Assert.Equal(0, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        var mira = game.Present("mira")!;
        Assert.Equal((72, 13), (mira.GridX, mira.GridY));
        Assert.Contains(game.Talk("mira").Transcript, l => l.Text.Contains("I like that name"));
    }

    [Theory]
    [InlineData(725)]
    [InlineData(727)]
    [InlineData(729)]
    public void KeirasBunearyRunsIntoThePlayerAtTheCitysEdge(int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 460, y);
        game.Tile = (461, y);
        var scene = game.Play(Scripts.Find("Buneary", "hearthome_city")!);
        Assert.Contains("cry Buneary", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Keira" && l.Text.Contains("I'm Keira"));
        Assert.Equal(1, game.Story.Var("VAR_HEARTHOME_CITY_STATE"));
        Assert.Null(game.Present("keira", "hearthome_city"));
        Assert.Null(game.Present("buneary", "hearthome_city"));
        Assert.False(game.Fires("Buneary"));
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void TheFishermanWalksThePlayerToTheContestHall(Direction facing)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 470, 726);
        var fisherman = game.Present("fisherman", "hearthome_city")!;
        game.Tile = facing switch
        {
            Direction.Up => (487, 715), Direction.Down => (487, 713), Direction.Left => (488, 714), _ => (486, 714)
        };
        game.Facing = facing;
        var walk = game.Play(Scripts.Find("Fisherman", "hearthome_city")!, fisherman);
        Assert.Contains(walk.Transcript, l => l.Text.Contains("Contest Hall!"));
        Assert.Equal((479, 698), game.Tile);
        Assert.Equal((479, 697), (fisherman.GridX, fisherman.GridY));
        Assert.Contains("happy", game.Play(Scripts.Find("Fisherman", "hearthome_city")!, fisherman).Transcript.Single().Text);
    }

    [Fact]
    public void TheChapterPlaysThroughFromTheContestHallToTheRivalInTheGate()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 470, 726);
        game.Step("Buneary");

        // The guide at the Gym's door sends the player to the Contest Hall
        Assert.Contains(game.Talk("gym_guide", "hearthome_city").Transcript, l => l.Text.Contains("Contest Hall"));

        // In the lobby, Keira and the player's mother, who taught her contests
        game.Arrive("Sinnoh", 479, 692);
        game.Through("ContestHallLobby");
        Assert.Contains(game.Said, l => l.Speaker == "Keira" && l.Text.Contains("your child"));
        Assert.Contains(game.Said, l => l.Speaker == "Mom");
        Assert.Equal(1, game.Story.Var("VAR_CONTEST_HALL_LOBBY_STATE"));
        Assert.True(game.Story.Has("FLAG_CONTEST_HALL_VISITED"));
        Assert.Null(game.Present("keira"));
        Assert.Null(game.Present("mom"));
        // Only once
        game.Through("Sinnoh");
        game.Through("ContestHallLobby");
        Assert.DoesNotContain(game.Said, l => l.Speaker == "Keira");
        Assert.Null(game.Present("mom"));

        // Fantina goes back to her Gym, and its guide stops keeping the door
        var fantina = game.Talk("fantina");
        Assert.Contains(fantina.Transcript, l => l.Speaker == "Fantina" && l.Text.Contains("I wait for you"));
        Assert.Null(game.Present("fantina"));
        Assert.True(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));
        game.Through("Sinnoh");
        Assert.Null(game.Present("gym_guide", "hearthome_city"));
        Assert.Equal("HearthomeGym", game.Map.GetWarpAt(499, 697)!.TargetMap);

        // Her Badge opens the road east, and the rival waits in the gate
        WinTheRelicBadge(game);
        Assert.Null(game.Present("pokefan_m_2", "hearthome_city"));
        Assert.Null(game.Present("hiker_2", "hearthome_city"));
        game.Arrive("Route209GateToHearthomeCity", 1, 7);
        Assert.NotNull(game.Present("rival"));
        var rival = game.Step("Rival");
        Assert.Contains("battle rival_route_209_turtwig Won", rival.Log);
        Assert.Contains(rival.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Veilstone"));
        Assert.Equal(2, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        Assert.Null(game.Present("rival"));
        Assert.False(game.Fires("Rival"));
        // The gate leads on to Route 209
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "Sinnoh" && w.TargetX == 513);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void TheRivalComesToThePlayerOnEachTileOfTheGatesTrigger(int y)
    {
        var game = NewGame();
        WinTheRelicBadge(game);
        game.Arrive("Route209GateToHearthomeCity", 1, 7);
        game.Tile = (5, y);
        var rival = game.Play(Scripts.Find("Rival", "Route209GateToHearthomeCity")!);
        Assert.Contains(rival.Log, l => l.StartsWith("battle rival_route_209_"));
        Assert.Null(game.Present("rival"));
    }

    [Fact]
    public void LosingToTheRivalInTheGateEndsTheScene()
    {
        var game = NewGame(BattleOutcome.Lost);
        WinTheRelicBadge(game);
        game.Arrive("Route209GateToHearthomeCity", 1, 7);
        game.Step("Rival");
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        Assert.True(game.Fires("Rival"));
    }

    /// <summary>Plays the Hallowed Tower's script, standing below one of its tiles.</summary>
    private static HeadlessScriptHost ReadTheTower(OpeningTests.Game game)
    {
        var tile = game.Map.TileScripts.First(kv => kv.Value == "HallowedTower").Key;
        game.Arrive("Sinnoh", tile.X, tile.Y + 1);
        return game.Play(Scripts.Find("HallowedTower", "route_209")!);
    }

    [Fact]
    public void TheHallowedTowerWaitsForTheOddKeystoneAndThirtyTwoPeopleSpokenTo()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 700);
        Assert.Equal(4, game.Map.TileScripts.Count(kv => kv.Value == "HallowedTower"));
        Assert.Contains("missing", ReadTheTower(game).Transcript.Single().Text);

        game.Bag.AddItem(ItemDatabase.Get("Odd Keystone")!);
        var set = ReadTheTower(game);
        Assert.Contains(set.Asked, a => a.Question.Contains("Odd Keystone"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Odd Keystone")!));
        Assert.Equal(1, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));

        // Each person counts once, however often they are spoken to; thirty-one isn't enough
        for (int i = 0; i < 31; i++) game.Story.Greet($"sinnoh/person_{i}");
        game.Story.Greet("sinnoh/person_0");
        Assert.Equal(31, game.Story.Greetings);
        var stirring = ReadTheTower(game);
        Assert.DoesNotContain(stirring.Log, l => l.StartsWith("wildbattle"));
        Assert.Contains("inside the tower", stirring.Transcript.Single().Text);

        game.Story.Greet("sinnoh/person_31");
        var spiritomb = ReadTheTower(game);
        Assert.Contains("cry Spiritomb", spiritomb.Log);
        Assert.Contains("wildbattle Spiritomb 25 Won", spiritomb.Log);
        // The stone is spent and the count starts again
        Assert.Equal(0, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
        Assert.Equal(0, game.Story.Greetings);
        Assert.Contains("missing", ReadTheTower(game).Transcript.Single().Text);
    }

    [Theory]
    [InlineData(0, "set up long ago")]
    [InlineData(8, "moved a little")]
    [InlineData(15, "sobbing")]
    [InlineData(22, "trembling")]
    [InlineData(29, "inside the tower")]
    public void TheHallowedTowerStirsAsMorePeopleAreSpokenTo(int greetings, string words)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 700);
        game.Story.SetVar("VAR_HALLOWED_TOWER_STATE", 1);
        for (int i = 0; i < greetings; i++) game.Story.Greet($"sinnoh/person_{i}");
        Assert.Contains(words, ReadTheTower(game).Transcript.Single().Text);
    }

    [Fact]
    public void SpiritombIsListedOnRoute209ByTheHallowedTower()
    {
        var spiritomb = Habitats.Sinnoh!.Of("Spiritomb");
        var place = Assert.Single(spiritomb);
        Assert.Equal("Route 209", place.Name);
        Assert.True(place.Ways.HasFlag(HabitatWays.Special));
        Assert.Equal("The Hallowed Tower", place.How);
    }

    [Fact]
    public void TheFishermanOnRoute209GivesTheGoodRodOnce()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 700);
        game.Talk("fisherman", "route_209");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Good Rod")!));
        game.Talk("fisherman", "route_209");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Good Rod")!));
        Assert.Contains(game.Said, s => s.Text.Contains("press the button"));
    }

    [Fact]
    public void TheLostTowersFloorsLeadUpToTheFogAndTheOldWomenGiveTheirTagsOnceItIsCleared()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 700);
        Assert.Equal("LostTower1F", game.Map.GetWarpAt(568, 680)!.TargetMap);
        game.Arrive("LostTower1F", 7, 14);
        foreach (string floor in new[] { "LostTower2F", "LostTower3F", "LostTower4F", "LostTower5F" }) game.Through(floor);
        Assert.Equal("LostTower5F", game.Map.Name);
        Assert.Equal(FieldWeather.Fog, game.Map.WeatherAt(7, 9));
        game.Through("LostTower4F");
        game.Through("LostTower5F");

        game.Talk("old_woman_1");
        Assert.Contains("fog", game.Said.Single().Text);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Spell Tag")!));

        // Defog lifts the room's fog while the player stays
        game.Story.Set(FieldMoveRules.DefogFlag);
        game.Map.FogLifted = true;
        Assert.Equal(FieldWeather.Clear, game.Map.WeatherAt(7, 9));
        game.Talk("old_woman_1");
        game.Talk("old_woman_1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Spell Tag")!));
        Assert.Contains("Fantina", game.Said[0].Text);

        // Once cleared before her, the other gives her tag even after the fog has come back
        game.Story.Unset(FieldMoveRules.DefogFlag);
        game.Talk("old_woman_2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Cleanse Tag")!));
    }

    [Theory]
    [InlineData(557)]
    [InlineData(560)]
    [InlineData(563)]
    public void TheRivalComesDownSolaceonsStreetOnceAsThePlayerFirstArrives(int x)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 680);
        Assert.Null(game.Present("rival", "solaceon_town"));
        var trigger = game.Map.Triggers.Single(t => t.Script == "RivalInTown" && t.Y == 669);
        Assert.Equal((557, 7), (trigger.X, trigger.Width));
        game.Arrive("Sinnoh", x, 669);
        var scene = game.Play(Scripts.Find("RivalInTown", "solaceon_town")!);
        // He came down the player's own column and went back up it
        var rival = game.Map.Everyone.First(n => n.Key == "rival" && n.GridY == 661 && n.GridX >= 557 && n.GridX <= 563);
        Assert.Equal(x, rival.GridX);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Defog"));
        Assert.Equal(1, game.Story.Var("VAR_SOLACEON_TOWN_STATE"));
        Assert.Null(game.Present("rival", "solaceon_town"));
        Assert.False(game.Fires("RivalInTown"));
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    public void TheRuinManiacGivesThePokemonHistoryAppForFiftySpeciesSeen(int seen, bool gives)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 660);
        game.Seen = seen;
        var talk = game.Talk("ruin_maniac", "solaceon_town");
        Assert.Equal(gives, talk.Poketch.Apps.Contains(PoketchApp.PokemonHistory));
        Assert.Equal(gives, game.Story.Has("FLAG_RECEIVED_POKETCH_POKEMON_HISTORY"));
        if (!gives) return;
        game.Talk("ruin_maniac", "solaceon_town");
        Assert.Contains("history", game.Said[0].Text);
    }

    [Fact]
    public void TheHikerInTheRuinsTradesAGreenShardForALoanOfDefog()
    {
        var game = NewGame();
        var room = game.Maps.Values.First(m => m.Everyone.Any(n => n.Key == "hiker" && m.ScriptFileAt(n.GridX, n.GridY) == "solaceon_ruins_room_2"));
        var hiker = room.Everyone.First(n => n.Key == "hiker" && room.ScriptFileAt(n.GridX, n.GridY) == "solaceon_ruins_room_2");
        game.Arrive(room.Name, hiker.GridX, hiker.GridY + 1);
        game.Talk("hiker", "solaceon_ruins_room_2");
        Assert.Empty(game.Last.Asked);

        game.Bag.AddItem(ItemDatabase.Get("HM05")!);
        game.Play(Scripts.Find("Hiker", "solaceon_ruins_room_2")!, hiker);
        Assert.Contains(game.Last.Asked, a => a.Question.Contains("HM05"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Green Shard")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM05")!));
        game.Play(Scripts.Find("Hiker", "solaceon_ruins_room_2")!, hiker);
        Assert.Empty(game.Last.Asked);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Green Shard")!));
    }

    [Fact]
    public void ThePsyduckStandAcrossRoute210UntilTheSecretPotion()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 590);
        var psyduck = Enumerable.Range(1, 4).Select(i => game.Present($"psyduck_{i}", "route_210_south")).ToList();
        Assert.All(psyduck, Assert.NotNull);
        game.Talk("psyduck_1", "route_210_south");
        Assert.Contains("won't budge", game.Said[0].Text);
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));

        game.Bag.AddItem(ItemDatabase.Get("Secret Potion")!);
        game.Talk("psyduck_1", "route_210_south");
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
        Assert.All(Enumerable.Range(1, 4), i => Assert.Null(game.Present($"psyduck_{i}", "route_210_south")));
    }

    [Fact]
    public void TheSolaceonRuinsInscriptionsAreReadFromTheirBackWalls()
    {
        var game = NewGame();
        foreach (string room in new[] { "solaceon_ruins_room_1", "solaceon_ruins_room_7" })
        {
            game.Arrive("Sinnoh", 560, 660);
            var map = game.Maps.Values.First(m => m.TileScripts.Any(kv => kv.Value == "Inscription" && m.ScriptFileAt(kv.Key.X, kv.Key.Y) == room));
            Assert.Equal(3, map.TileScripts.Count(kv => kv.Value == "Inscription" && map.ScriptFileAt(kv.Key.X, kv.Key.Y) == room));
            game.Play(Scripts.Find("Inscription", room)!);
            Assert.Contains("Unown", game.Said[0].Text);
        }
    }

    [Fact]
    public void ASaveKeepsWhoThePlayerHasSpokenTo()
    {
        var story = new StoryState();
        story.Greet("sinnoh/b");
        story.Greet("sinnoh/a");
        int revision = story.Revision;
        story.Greet("sinnoh/a");
        Assert.Equal(2, story.Greetings);
        Assert.Equal(revision, story.Revision);
        var loaded = new StoryState();
        loaded.RestoreGreetings(story.Greeted);
        Assert.Equal(2, loaded.Greetings);
        story.Clear();
        Assert.Equal(0, story.Greetings);
    }

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndOpensFantinasDoorIfHerBadgeIsWon()
    {
        var before = new StoryState();
        StoryMigration.Upgrade(before, 8, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(before.Has("FLAG_HIDE_HEARTHOME_CITY_KEIRA"));
        Assert.True(before.Has("FLAG_HIDE_ROUTE_207_COUNTERPART"));
        Assert.False(before.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));

        var won = new StoryState();
        won.GiveBadge(Badge.Relic);
        StoryMigration.Upgrade(won, 8, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(won.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));
        Assert.True(won.Has("FLAG_HIDE_CONTEST_HALL_LOBBY_FANTINA"));
        Assert.Equal(9, StoryState.CurrentVersion);
    }
}
