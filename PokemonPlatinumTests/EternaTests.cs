using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The third chapter's second half (plan 02 · S6, "Windworks and Eterna", part 2) played through with no screen, in the
/// order a player meets it: the rival and Cyrus at Eterna City's statue, Cynthia and HM01, Gardenia at her Gym's door,
/// the Forest Badge (given here as the Gym's own script gives it: the Gym's inside is plan 01 · M9's), the tree before
/// Team Galactic's building, Looker in disguise, the building's grunts and Commander Jupiter, the cycle shop's Bicycle,
/// the ways out watched until the Underground Man's Explorer Kit, and Gardenia before the Old Chateau.
/// </summary>
public class EternaTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        var grotle = new Pokemon(PokemonDatabase.Get("Grotle")!, 24);
        grotle.Moves.Clear();
        foreach (string move in new[] { "Razor Leaf", "Bite", "Cut" }) grotle.Moves.Add(new Move(MoveDatabase.Get(move)!));
        game.Party.Add(grotle);
        return game;
    }

    /// <summary>Plays a few lines of the script language in a place, as a scene of the game would.</summary>
    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    /// <summary>What Eterna's Gym gives once Gardenia is beaten (scripts_eterna_city_gym.s; its scene is plan 01 · M9's).</summary>
    private static void WinTheForestBadge(OpeningTests.Game game) =>
        Run(game, "eterna_city", "givebadge forest\n clearflag FLAG_HIDE_ETERNA_FOREST_GARDENIA\n setflag FLAG_RECEIVED_GARDENIA_TM86");

    [Fact]
    public void ANewGameFindsEternaAsTheChapterBeginsIt()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 300, 533);
        // Cyrus before the statue, Gardenia at her Gym's door and Team Galactic's grunts about the town are there from
        // the start; the rival and Cynthia come with their scenes
        Assert.NotNull(game.Present("cyrus", "eterna_city"));
        Assert.NotNull(game.Present("gardenia", "eterna_city"));
        Assert.NotNull(game.Present("grunt_m_1", "eterna_city"));
        Assert.Null(game.Present("rival", "eterna_city"));
        Assert.Null(game.Present("cynthia", "eterna_city"));
        Assert.True(game.Fires("RivalAndCyrus"));
        Assert.False(game.Fires("CynthiaCut"));
        Assert.False(game.Fires("BlockWest"));
        Assert.False(game.Fires("BlockSouth"));
        // The Gym's door leads in (plan 01 · M9), with Gardenia before it; the building's door is shut by the tree before it
        Assert.Equal("EternaGym", game.Map.GetWarpAt(312, 562)!.TargetMap);
        Assert.NotNull(game.Present("cut_tree_2", "eterna_city"));
        Assert.Equal("TeamGalacticEternaBuilding1F", game.Map.GetWarpAt(305, 519)!.TargetMap);
    }

    [Fact]
    public void TheChapterPlaysThroughFromTheStatueToTheBicycle()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 300, 533);

        // The rival runs into the player and takes them to the statue, where Cyrus speaks of time and space and goes
        var rival = game.Step("RivalAndCyrus");
        Assert.Contains(rival.Transcript, l => l.Speaker == OpeningTests.Game.Rival);
        Assert.Contains(rival.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("Time and space"));
        Assert.Contains("music common/rival", rival.Log);
        Assert.Contains("camera Pan 327 525", rival.Log);
        Assert.Equal(1, game.Story.Var("VAR_ETERNA_CITY_STATE"));
        Assert.Equal((323, 526), game.Tile);
        Assert.Null(game.Present("rival", "eterna_city"));
        Assert.Null(game.Present("cyrus", "eterna_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_ETERNA_CITY_CYRUS"));
        Assert.False(game.Fires("RivalAndCyrus"));

        // Back at the building's door, Cynthia comes along with HM01
        var cynthia = game.Step("CynthiaCut");
        Assert.Contains(cynthia.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Professor Rowan"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        Assert.Equal(2, game.Story.Var("VAR_ETERNA_CITY_STATE"));
        Assert.Null(game.Present("cynthia", "eterna_city"));
        Assert.False(game.Fires("CynthiaCut"));

        // Without the Forest Badge the tree stays
        var tree = game.Talk("cut_tree_2", "eterna_city");
        Assert.Empty(tree.Asked);
        Assert.NotNull(game.Present("cut_tree_2", "eterna_city"));

        // Gardenia at the Gym's door has heard of the player from the rival, and goes in to wait
        var gardenia = game.Talk("gardenia", "eterna_city");
        Assert.Contains(gardenia.Transcript, l => l.Text.Contains(OpeningTests.Game.Rival));
        Assert.Null(game.Present("gardenia", "eterna_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_ETERNA_CITY_GARDENIA"));

        // The Gym's Badge, and Cut clears the tree before the building's door
        WinTheForestBadge(game);
        var cut = game.Talk("cut_tree_2", "eterna_city");
        Assert.Contains(cut.Log, l => l.StartsWith("usemove Cut"));
        Assert.Null(game.Present("cut_tree_2", "eterna_city"));

        // Inside, a grunt by the door is Looker in disguise, and warns of the stairs
        game.Through("TeamGalacticEternaBuilding1F");
        Assert.Contains(game.Said, l => l.Speaker == "Looker" && l.Text.Contains("two staircases"));
        Assert.Equal(1, game.Story.Var("VAR_TEAM_GALACTIC_ETERNA_BUILDING_1F_STATE"));
        // After the scene his disguise is back on, and only once
        Assert.NotNull(game.Present("grunt_m_looker"));
        Assert.Null(game.Present("looker"));
        game.Through("Sinnoh");
        game.Through("TeamGalacticEternaBuilding1F");
        Assert.Empty(game.Said.Where(l => l.Speaker == "Looker"));

        // The grunts of the floors are Platinum's trainers
        var grunt = game.Talk("galactic_grunt_1");
        Assert.Contains("battle galactic_grunt_team_galactic_eterna_building_1f_1 Won", grunt.Log);

        // Commander Jupiter on the top floor: beaten, Team Galactic leaves Eterna and the manager goes home
        game.Arrive("TeamGalacticEternaBuilding4F", 14, 7);
        var jupiter = game.Talk("jupiter");
        Assert.Contains("battle commander_jupiter_team_galactic_eterna_building Won", jupiter.Log);
        Assert.Contains(jupiter.Transcript, l => l.Speaker == "Jupiter" && l.Text.Contains("myths"));
        Assert.Contains(jupiter.Transcript, l => l.Speaker == "Manager" && l.Text.Contains("cycle shop"));
        Assert.Equal(3, game.Story.Var("VAR_ETERNA_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING"));
        Assert.Null(game.Present("jupiter"));
        Assert.Null(game.Present("pokefan_m"));
        game.Arrive("TeamGalacticEternaBuilding1F", 11, 14);
        Assert.Null(game.Present("galactic_grunt_2"));
        Assert.Null(game.Present("grunt_m_looker"));
        game.Arrive("Sinnoh", 305, 520);
        Assert.Null(game.Present("grunt_m_1", "eterna_city"));
        Assert.Contains("gone", game.Talk("pokemon_breeder_f_1", "eterna_city").Transcript.Single().Text);

        // The cycle shop's manager is back behind his counter, with the newest Bicycle for the player
        game.Arrive("EternaCycleShop", 7, 11);
        var shop = game.Talk("pokefan_m");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_BICYCLE"));
        Assert.Equal(1, game.Story.Var("VAR_ETERNA_CITY_BLOCK_EXITS_STATE"));
        Assert.Contains(shop.Transcript, l => l.Text.Contains("gear"));

        // Out in town, the ways out west and south are watched until the player has an Explorer Kit as well
        game.Through("Sinnoh");
        Assert.True(game.Fires("BlockWest"));
        Assert.True(game.Fires("BlockSouth"));
        var west = game.Step("BlockWest");
        Assert.Equal((298, 532), game.Tile);
        Assert.Contains(west.Transcript, l => l.Text.Contains("Cycling Road"));
        var south = game.Step("BlockSouth");
        Assert.Equal((303, 564), game.Tile);
        Assert.Contains(south.Transcript, l => l.Text.Contains("next door to the Pokémon Center"));

        // The Underground Man gives the Explorer Kit and offers to teach the player
        game.Arrive("EternaUndergroundManHouse", 4, 8);
        var kit = game.Talk("underground_man");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Explorer Kit")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_EXPLORER_KIT"));
        Assert.True(game.Story.Has("FLAG_ACCEPTED_UNDERGROUND_MAN_AS_MENTOR"));
        Assert.Single(kit.Asked);
        // With both, nobody stops the player any more
        game.Through("Sinnoh");
        Assert.Equal(0, game.Story.Var("VAR_ETERNA_CITY_BLOCK_EXITS_STATE"));
        Assert.False(game.Fires("BlockWest"));
        Assert.False(game.Fires("BlockSouth"));

        // Gardenia waits before the Old Chateau in Eterna Forest, and goes off east
        game.Arrive("EternaForest", 73, 35);
        Assert.NotNull(game.Present("gardenia"));
        var forest = game.Talk("gardenia");
        Assert.Contains(forest.Transcript, l => l.Text.Contains("Old Chateau"));
        Assert.Null(game.Present("gardenia"));
        Assert.True(game.Story.Has("FLAG_HIDE_ETERNA_FOREST_GARDENIA"));
    }

    [Theory]
    [InlineData(523)]
    [InlineData(524)]
    [InlineData(525)]
    public void TheRivalRunsIntoThePlayerOnEachTileOfHisTrigger(int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 303, y);
        game.Play(Scripts.Find("RivalAndCyrus", "eterna_city")!);
        // Wherever they were knocked from, they end beside the statue below where the rival stood
        Assert.Equal((323, 526), game.Tile);
        Assert.Equal(1, game.Story.Var("VAR_ETERNA_CITY_STATE"));
    }

    [Theory]
    [InlineData(304, 522)]
    [InlineData(305, 522)]
    [InlineData(306, 522)]
    [InlineData(304, 523)]
    [InlineData(304, 524)]
    [InlineData(304, 525)]
    public void CynthiaComesUpToThePlayerOnEachTileOfHerTriggers(int x, int y)
    {
        var game = NewGame();
        game.Story.SetVar("VAR_ETERNA_CITY_STATE", 1);
        game.Arrive("Sinnoh", x, y);
        Assert.Equal("CynthiaCut", FieldScripts.TriggerAt(game.Map, x, y, game.Story)!.Script);
        // Walking her there and away is checked for walls by the host: Play holds that nothing was walked into
        game.Play(Scripts.Find("CynthiaCut", "eterna_city")!);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        Assert.Equal((x, y), game.Tile);
    }

    [Theory]
    [InlineData(Direction.Up, 14, 7, 14, 8)]
    [InlineData(Direction.Left, 15, 6, 14, 6)]
    [InlineData(Direction.Right, 13, 6, 14, 6)]
    public void TheManagerComesUpToThePlayerWhereverJupiterWasSpokenToFrom(Direction facing, int x, int y, int toX, int toY)
    {
        var game = NewGame();
        game.Arrive("TeamGalacticEternaBuilding4F", x, y);
        var jupiter = game.Map.FindPerson("jupiter")!;
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = (x, y), PlayerFacing = facing };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("Jupiter", "TeamGalacticEternaBuilding4F")!, jupiter);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        var manager = game.Map.FindPerson("pokefan_m")!;
        Assert.Equal((toX, toY), (manager.GridX, manager.GridY));
        Assert.Equal(3, game.Story.Var("VAR_ETERNA_CITY_STATE"));
    }

    [Fact]
    public void LosingToJupiterLeavesTeamGalacticWhereItWas()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Arrive("TeamGalacticEternaBuilding4F", 14, 7);
        var jupiter = game.Talk("jupiter");
        Assert.Single(jupiter.Log, l => l.StartsWith("battle "));
        Assert.Equal(0, game.Story.Var("VAR_ETERNA_CITY_STATE"));
        Assert.False(game.Story.Has("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING"));
        Assert.NotNull(game.Present("jupiter"));
    }

    [Fact]
    public void GardeniaWaitsInTheForestOnlyOnceHerBadgeIsWon()
    {
        var game = NewGame();
        game.Arrive("EternaForest", 73, 35);
        Assert.Null(game.Present("gardenia"));
        WinTheForestBadge(game);
        game.Arrive("EternaForest", 73, 35);
        var gardenia = game.Present("gardenia")!;
        Assert.Equal((73, 33, Direction.Up), (gardenia.GridX, gardenia.GridY, gardenia.Facing));
    }

    [Fact]
    public void TheUndergroundManCanBeToldNoAndAskedAgain()
    {
        var game = NewGame();
        game.Arrive("EternaUndergroundManHouse", 4, 8);
        game.Play(Scripts.Find("UndergroundMan", "EternaUndergroundManHouse")!, game.Map.FindPerson("underground_man"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Explorer Kit")!));
        // The first host answered yes by default; a second with a no keeps him asking
        game.Story.Unset("FLAG_ACCEPTED_UNDERGROUND_MAN_AS_MENTOR");
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map };
        host.Answers.Enqueue(1);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("UndergroundMan", "EternaUndergroundManHouse")!, game.Map.FindPerson("underground_man"));
        runner.RunToEnd();
        Assert.False(game.Story.Has("FLAG_ACCEPTED_UNDERGROUND_MAN_AS_MENTOR"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Explorer Kit")!));
        Assert.Contains(host.Transcript, l => l.Text.Contains("lonely"));
    }

    [Fact]
    public void TheEternaPokemonCentersPeopleKnowWhatTeamGalacticDid()
    {
        var game = NewGame();
        game.Arrive("EternaPokemonCenter", 5, 7);
        Assert.Contains("took my Pokémon", game.Talk("school_kid_m").Transcript[0].Text);
        game.Story.Set("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING");
        Assert.Contains("got my Pokémon back", game.Talk("school_kid_m").Transcript[0].Text);

        // The woman puts the Friendship Checker on a Pokétch, once
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map };
        host.Poketch.Enabled = true;
        foreach (int lines in new[] { 2, 1 })
        {
            host.Transcript.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find("FriendshipChecker", "EternaPokemonCenter")!, game.Map.FindPerson("pokemon_breeder_f"));
            runner.RunToEnd();
            Assert.Equal(lines, host.Transcript.Count(l => l.Speaker == "Woman"));
        }
        Assert.Contains(PoketchApp.FriendshipChecker, host.Poketch.Apps);
        Assert.True(game.Story.Has("FLAG_RECEIVED_ETERNA_FRIENDSHIP_CHECKER"));
    }
}
