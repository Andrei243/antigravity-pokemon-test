using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The sixth chapter (plan 02 · S9, "Celestic and the sea") played through with no screen, in the order a player meets
/// it: the Psyduck on Route 210 cured and Cynthia's Old Charm, the trainer's TM51, northern Route 210 in its fog,
/// Celestic Town's elder and Team Galactic's grunt, Cyrus in the ruins' cave and HM03 from the elder, Cynthia waiting
/// outside and Route 218's blockade lifted, the elder at home, the Fuego Ironworks and Mr. Fuego, the sea routes'
/// people, and the rival's battle on Canalave City's bridge. Each scene is started as the game starts it, on maps of
/// its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class CelesticAndTheSeaTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won, int starter = 0)
    {
        var game = new OpeningTests.Game(starter, fight);
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        var leader = new Pokemon(PokemonDatabase.Get(new[] { "Torterra", "Infernape", "Empoleon" }[starter])!, 40);
        game.Party.Add(leader);
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    /// <summary>Where Route 218's blockade stands before the Canalave gate: the guitarist, the fisherman and their Pokémon.</summary>
    private static readonly (int X, int Y)[] Blockade = { (73, 753), (73, 754), (72, 755), (73, 756), (71, 756), (73, 757) };

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, string? place, Direction facing)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        return game.Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name)!, npc);
    }

    /// <summary>Reads what the original's background event at a tile says, from the tile below it, facing it.</summary>
    private static HeadlessScriptHost Read(OpeningTests.Game game, int x, int y)
    {
        var script = game.Map.TileScripts[(x, y)];
        game.Tile = (x, y + 1);
        game.Facing = Direction.Up;
        return game.Play(Scripts.Find(script, game.Map.ScriptFileAt(x, y))!);
    }

    /// <summary>The Psyduck on Route 210 cured with the Secret Potion, from the tile below one of them, and Cynthia's errand taken on.</summary>
    private static HeadlessScriptHost CureThePsyduck(OpeningTests.Game game, int x = 561, params int[] answers)
    {
        game.Bag.AddItem(Item("Secret Potion"));
        game.Arrive("Sinnoh", 560, 595);
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        var psyduck = game.Map.NPCs.First(n => n.Key is "psyduck_1" or "psyduck_2" && n.GridX == x);
        game.Tile = (x, 588);
        game.Facing = Direction.Up;
        return game.Play(Scripts.Find("Psyduck", "route_210_south")!, psyduck);
    }

    /// <summary>Celestic Town's grunt beaten from below him, and the Old Charm handed to the elder.</summary>
    private static void ThroughTheGrunt(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 463, 530);
        TalkFrom(game, "grunt_m", "celestic_town", Direction.Up);
        Assert.True(game.Story.Has("FLAG_DELIVERED_OLD_CHARM"));
    }

    /// <summary>Into the ruins' cave, the painting read from (9, 3) and Cyrus beaten at once.</summary>
    private static HeadlessScriptHost BeatCyrus(OpeningTests.Game game, int x = 9)
    {
        game.Through("CelesticTownCave");
        Assert.Equal("CelesticTownCave", game.Map.Name);
        game.Tile = (x, 3);
        game.Facing = Direction.Up;
        return game.Play(Scripts.Find("Painting", "celestic_town_cave")!);
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameKeepsTheChaptersPeopleOutOfSightUntilTheirScenes()
    {
        var game = NewGame();
        // Route 210: the Psyduck across the road, Cynthia not yet
        game.Arrive("Sinnoh", 560, 595);
        Assert.NotNull(game.Present("psyduck_1", "route_210_south"));
        Assert.Null(game.Present("cynthia", "route_210_south"));

        // Celestic Town: the grunt before the ruins' door and the elder at the south end of town
        game.Arrive("Sinnoh", 463, 530);
        var grunt = game.Present("grunt_m", "celestic_town")!;
        Assert.Equal((463, 522), (grunt.GridX, grunt.GridY));
        var elder = game.Present("elder", "celestic_town")!;
        Assert.Equal((462, 538), (elder.GridX, elder.GridY));
        Assert.Null(game.Present("cynthia", "celestic_town"));
        Assert.True(game.Fires("ElderWarns"));
        // The cave's door and the elder's house are open; the cave is a map of its own
        Assert.Equal("CelesticTownCave", game.Map.GetWarpAt(463, 521)!.TargetMap);
        Assert.Equal("CelesticTownNorthHouse", game.Map.GetWarpAt(463, 515)!.TargetMap);
        game.Through("CelesticTownCave");
        Assert.Null(game.Present("elder", "celestic_town_cave"));
        Assert.Null(game.Present("cyrus", "celestic_town_cave"));
        game.Arrive("CelesticTownNorthHouse", 5, 8);
        Assert.Null(game.Present("elder"));
        Assert.NotNull(game.Present("expert_m"));

        // Route 218: the blockade fills the way to the Canalave gate
        game.Arrive("Sinnoh", 80, 755);
        foreach (string key in new[] { "guitarist", "fisherman", "clefairy_south", "clefairy_north", "pikachu_south", "pikachu_north" })
            Assert.NotNull(game.Present(key, "route_218"));
        Assert.All(Blockade, at => Assert.NotNull(game.Map.GetNpcAt(at.X, at.Y)));

        // Canalave City: the rival not yet on the bridge, and his trigger waiting
        game.Arrive("Sinnoh", 55, 724);
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.True(game.Fires("RivalOnTheBridge"));
    }

    // ------------------------------------------------------------------ Route 210

    [Theory]
    [InlineData(560)]
    [InlineData(561)]
    public void CuredPsyduckWaddleOffAndCynthiaComesUpTheRoadWithTheOldCharm(int x)
    {
        var game = NewGame();
        // Yes to the potion, no to her first question: she asks again
        var scene = CureThePsyduck(game, x, 0, 1);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("used the Secret Potion"));
        foreach (string key in new[] { "psyduck_1", "psyduck_2", "psyduck_3", "psyduck_4" }) Assert.Null(game.Present(key, "route_210_south"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
        // She asked twice: the second time it was yes
        Assert.Contains(scene.Asked, a => a.Question.Contains("Will you take it") && a.Answer == ScriptRunner.YesNo[1]);
        Assert.Contains(scene.Asked, a => a.Question.Contains("Celestic Town for me") && a.Answer == ScriptRunner.YesNo[0]);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("grandmother"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("Old Charm")));
        // The Secret Potion stays in the bag, as in the original
        Assert.Equal(1, game.Bag.GetQuantity(Item("Secret Potion")));
        Assert.True(game.Story.Has("FLAG_USED_SECRETPOTION"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_CYNTHIA"));
        Assert.Null(game.Present("cynthia", "route_210_south"));
        // The way north is open, and stays so
        game.Arrive("Sinnoh", 560, 595);
        Assert.Null(game.Map.GetNpcAt(x, 587));
    }

    [Fact]
    public void ThePsyduckStayWithoutTheSecretPotionOrWhenItIsntUsed()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 560, 595);
        TalkFrom(game, "psyduck_1", "route_210_south", Direction.Up);
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
        CureThePsyduck(game, 561, 1);
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
        Assert.Equal(0, game.Bag.GetQuantity(Item("Old Charm")));
        Assert.NotNull(game.Present("psyduck_1", "route_210_south"));
    }

    [Fact]
    public void TheTrainerByTheCafeGivesTM51Once()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 566, 615);
        game.Talk("ace_trainer_f", "route_210_south");
        game.Talk("ace_trainer_f", "route_210_south");
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM51")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_210_SOUTH_TM51"));
        Assert.Contains(game.Said, l => l.Text.Contains("land now and then"));
    }

    [Fact]
    public void NorthernRoute210LiesInFogThatDefogLiftsWithTheRelicBadge()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 530, 530);
        Assert.Equal(FieldWeather.Fog, game.Map.WeatherAt(530, 530));
        var spot = FieldMoveRules.SpotOf(game.Map, 530, 530, Direction.Up, new Walker(TravelMode.OnFoot, game.Map.HeightAt(530, 530)));
        Assert.True(spot.Fog);
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Defog, spot, game.Story));
        game.Story.GiveBadge(Badge.Relic);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Defog, spot, game.Story));
        // Celestic Town beyond it is clear
        Assert.NotEqual(FieldWeather.Fog, game.Map.WeatherAt(463, 530));
    }

    // ------------------------------------------------------------------ Celestic Town

    [Theory]
    [InlineData(463)]
    [InlineData(464)]
    [InlineData(465)]
    public void TheElderWarnsOfTheSpacemanAsThePlayerFirstComesUpTheRoad(int x)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 464, 541);
        game.Tile = (x, 538);
        Assert.Same(game.Map.Triggers.First(t => t.Script == "ElderWarns"), FieldScripts.TriggerAt(game.Map, x, 538, game.Story));
        var warned = game.Play(Scripts.Find("ElderWarns", "celestic_town")!);
        Assert.Contains(warned.Transcript, l => l.Speaker == "Elder" && l.Text.Contains("spaceman"));
        Assert.Equal(Direction.Left, warned.PlayerFacing);
        Assert.Equal(Direction.Right, game.Present("elder", "celestic_town")!.Facing);
        Assert.Equal(1, game.Story.Var("VAR_CELESTIC_TOWN_ELDER_STATE"));
        Assert.False(game.Fires("ElderWarns"));
        // Spoken to, she says the same
        Assert.Contains(game.Talk("elder", "celestic_town").Transcript, l => l.Text.Contains("bomb"));
    }

    // From below him, from his east and from his west: his way off, and the elder's way up, are on open ground
    [Theory]
    [InlineData(Direction.Up, 464, 523)]
    [InlineData(Direction.Left, 464, 523)]
    [InlineData(Direction.Right, 462, 523)]
    public void TheGruntBeatenRunsOffAndTheElderTakesTheOldCharm(Direction facing, int elderX, int elderY)
    {
        var game = NewGame();
        CureThePsyduck(game);
        game.Arrive("Sinnoh", 463, 530);
        var fight = TalkFrom(game, "grunt_m", "celestic_town", facing);
        Assert.Contains(fight.Asked, a => a.Question.Contains("get in my way") && a.Answer == ScriptRunner.YesNo[0]);
        Assert.Contains("battle galactic_grunt_celestic_town Won", fight.Log);
        Assert.Null(game.Present("grunt_m", "celestic_town"));
        Assert.True(game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_GRUNT_M"));
        var elder = game.Present("elder", "celestic_town")!;
        Assert.Equal((elderX, elderY), (elder.GridX, elder.GridY));
        Assert.Contains(fight.Transcript, l => l.Speaker == "Elder" && l.Text.Contains("granddaughter"));
        Assert.Contains(fight.Transcript, l => l.Text.Contains("handed the Old Charm"));
        Assert.Equal(0, game.Bag.GetQuantity(Item("Old Charm")));
        Assert.True(game.Story.Has("FLAG_DELIVERED_OLD_CHARM"));
        // Now the elder only asks the player to look round the ruins, and her trigger says nothing
        Assert.Contains(game.Talk("elder", "celestic_town").Transcript, l => l.Text.Contains("ruins"));
        var quiet = game.Play(Scripts.Find("ElderWarns", "celestic_town")!);
        Assert.Empty(quiet.Transcript);
        // Coming again, the way into the ruins is clear
        game.Arrive("Sinnoh", 463, 530);
        Assert.Null(game.Map.GetNpcAt(463, 522));
    }

    [Fact]
    public void TheGruntLeftAloneOrBeatingThePlayerStaysByTheRuins()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Arrive("Sinnoh", 463, 530);
        game.Answers.Enqueue(1);
        var left = TalkFrom(game, "grunt_m", "celestic_town", Direction.Up);
        Assert.Contains(left.Transcript, l => l.Text.Contains("partner"));
        Assert.DoesNotContain("battle", string.Join(" ", left.Log));
        TalkFrom(game, "grunt_m", "celestic_town", Direction.Up);
        Assert.False(game.Story.Has("FLAG_DELIVERED_OLD_CHARM"));
        game.Arrive("Sinnoh", 463, 530);
        Assert.NotNull(game.Present("grunt_m", "celestic_town"));
    }

    [Fact]
    public void WithoutTheOldCharmTheElderStillSendsThePlayerToTheRuins()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 463, 530);
        var fight = TalkFrom(game, "grunt_m", "celestic_town", Direction.Up);
        Assert.DoesNotContain(fight.Transcript, l => l.Text.Contains("Old Charm"));
        Assert.Contains(fight.Transcript, l => l.Text.Contains("inside the ruins"));
        Assert.True(game.Story.Has("FLAG_DELIVERED_OLD_CHARM"));
    }

    [Fact]
    public void TheCarvingsBesideTheRuinsDoorAreRead()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 463, 530);
        Assert.Equal(3, game.Map.TileScripts.Count(kv => kv.Value == "EtchingDialga"));
        Assert.Equal(3, game.Map.TileScripts.Count(kv => kv.Value == "EtchingPalkia"));
        Assert.Contains(Read(game, 460, 521).Transcript, l => l.Text.Contains("Time"));
        Assert.Contains(Read(game, 466, 521).Transcript, l => l.Text.Contains("Space"));
    }

    // ------------------------------------------------------------------ the ruins

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public void CyrusBeatenInTheRuinsAndTheElderGivesHm03(int x)
    {
        var game = NewGame();
        CureThePsyduck(game);
        ThroughTheGrunt(game);
        var scene = BeatCyrus(game, x);
        Assert.Contains(scene.Transcript, l => l.Text.Contains("ancient painting"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Elder" && l.Text.Contains("legend"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("My name is Cyrus"));
        Assert.Contains(scene.Asked, a => a.Question.Contains("challenge me") && a.Answer == ScriptRunner.YesNo[0]);
        Assert.Contains("battle galactic_boss_cyrus_celestic_town_ruins Won", scene.Log);
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM03")));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Surf"));
        Assert.Equal(1, game.Story.Var("VAR_CELESTIC_TOWN_STATE"));
        Assert.True(game.Story.Has("FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING"));
        // Cyrus and the elder have gone, and for good: she is at home now, and Cynthia in town
        Assert.Null(game.Present("cyrus", "celestic_town_cave"));
        Assert.Null(game.Present("elder", "celestic_town_cave"));
        foreach (string flag in new[] { "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS", "FLAG_HIDE_CELESTIC_TOWN_CAVE_ELDER", "FLAG_HIDE_CELESTIC_TOWN_ELDER" })
            Assert.True(game.Story.Has(flag));
        Assert.False(game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER"));
        Assert.False(game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_CYNTHIA"));
        Assert.Equal((x, 3), game.Tile);
        // The painting is only looked at now
        var again = Read(game, x, 2);
        Assert.Contains(again.Transcript, l => l.Text.Contains("sphere of light"));
        Assert.DoesNotContain(again.Transcript, l => l.Speaker == "Elder" || l.Speaker == "Cyrus");
    }

    // Said no, Cyrus shoves the player aside and stands before the painting; spoken to from either side he asks again
    [Theory]
    [InlineData(9, 8, 3, Direction.Right)]
    [InlineData(9, 9, 4, Direction.Up)]
    [InlineData(10, 11, 3, Direction.Left)]
    [InlineData(10, 10, 4, Direction.Up)]
    public void CyrusRefusedStandsBeforeThePaintingUntilChallenged(int readFrom, int x, int y, Direction facing)
    {
        var game = NewGame();
        ThroughTheGrunt(game);
        game.Through("CelesticTownCave");
        game.Answers.Enqueue(1);
        game.Tile = (readFrom, 3);
        game.Facing = Direction.Up;
        var refused = game.Play(Scripts.Find("Painting", "celestic_town_cave")!);
        Assert.Contains(refused.Transcript, l => l.Text.Contains("coward"));
        var cyrus = game.Present("cyrus", "celestic_town_cave")!;
        Assert.Equal((readFrom, 3), (cyrus.GridX, cyrus.GridY));
        Assert.Equal(readFrom == 9 ? (8, 3) : (11, 3), game.Tile);
        Assert.Equal(0, game.Bag.GetQuantity(Item("HM03")));
        // The elder says her piece while he stands there
        Assert.Contains(TalkFrom(game, "elder", "celestic_town_cave", Direction.Up).Transcript, l => l.Text.Contains("memories"));
        // Challenged at last, from beside him or below him
        game.Tile = (x, y);
        game.Facing = facing;
        var fight = game.Play(Scripts.Find("Cyrus", "celestic_town_cave")!, cyrus);
        Assert.Contains("battle galactic_boss_cyrus_celestic_town_ruins Won", fight.Log);
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM03")));
        // The player and the elder have turned to each other on the row before the painting
        Assert.Equal((readFrom, 3), game.Tile);
        Assert.Null(game.Present("elder", "celestic_town_cave"));
    }

    [Fact]
    public void LosingToCyrusLeavesHimAndTheElderInTheRuins()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Story.Set("FLAG_DELIVERED_OLD_CHARM");
        game.Story.Set("FLAG_HIDE_CELESTIC_TOWN_GRUNT_M");
        game.Arrive("Sinnoh", 463, 530);
        BeatCyrus(game);
        Assert.Equal(0, game.Bag.GetQuantity(Item("HM03")));
        Assert.Equal(0, game.Story.Var("VAR_CELESTIC_TOWN_STATE"));
        // Back in, they stand before the painting, and Cyrus asks again
        game.Arrive("Sinnoh", 463, 530);
        game.Through("CelesticTownCave");
        var cyrus = game.Present("cyrus", "celestic_town_cave")!;
        Assert.Equal((9, 3), (cyrus.GridX, cyrus.GridY));
        var elder = game.Present("elder", "celestic_town_cave")!;
        Assert.Equal((10, 3), (elder.GridX, elder.GridY));
        Assert.Null(game.Present("elder", "celestic_town"));
    }

    [Fact]
    public void CynthiaWaitsOutsideTheRuinsAndTheBlockadeOnRoute218Lifts()
    {
        var game = NewGame();
        CureThePsyduck(game);
        ThroughTheGrunt(game);
        BeatCyrus(game);
        game.Through("Sinnoh");
        Assert.Equal((463, 522), game.Tile);
        var outside = game.Last;
        Assert.Contains(outside.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Galactic Bomb"));
        Assert.Contains(outside.Transcript, l => l.Text.Contains("Canalave City"));
        Assert.Equal(Direction.Right, outside.PlayerFacing);
        var cynthia = game.Present("cynthia", "celestic_town")!;
        Assert.Equal((464, 522), (cynthia.GridX, cynthia.GridY));
        Assert.Equal(2, game.Story.Var("VAR_CELESTIC_TOWN_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_218_BLOCKADE"));
        // Spoken to, she points the way again; the next arrival leaves her be
        Assert.Contains(game.Talk("cynthia", "celestic_town").Transcript, l => l.Text.Contains("west from Jubilife"));
        game.Arrive("Sinnoh", 463, 530);
        Assert.Empty(game.Last.Transcript);
        Assert.True(game.Story.Has(VsSeeker.LevelFlag(2)));
        // The blockade has gone from before the Canalave gate
        game.Arrive("Sinnoh", 80, 755);
        Assert.All(Blockade, at => Assert.Null(game.Map.GetNpcAt(at.X, at.Y)));
        // And reaching Canalave City takes her away from Celestic Town
        game.Arrive("Sinnoh", 55, 724);
        Assert.True(game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_CYNTHIA"));
    }

    [Fact]
    public void ComingBackSomeOtherWayCynthiaStandsWhereSheIs()
    {
        var game = NewGame();
        Run(game, "celestic_town", "setvar VAR_CELESTIC_TOWN_STATE 1\n clearflag FLAG_HIDE_CELESTIC_TOWN_CYNTHIA");
        game.Arrive("Sinnoh", 470, 530);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal(2, game.Story.Var("VAR_CELESTIC_TOWN_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_218_BLOCKADE"));
        var cynthia = game.Present("cynthia", "celestic_town")!;
        Assert.Equal((466, 522), (cynthia.GridX, cynthia.GridY));
    }

    [Fact]
    public void Hm03TeachesSurfAndTheFenBadgeLetsItBeUsedOutside()
    {
        var game = NewGame();
        ThroughTheGrunt(game);
        BeatCyrus(game);
        var bibarel = new Pokemon(PokemonDatabase.Get("Bibarel")!, 30);
        Assert.True(MoveTeaching.CanLearn(bibarel, Item("HM03")));
        MoveTeaching.Learn(bibarel, "Surf", forget: 0);
        Assert.Contains(PartyScreen.ActionsFor(bibarel), a => a.Move == FieldMove.Surf);
        game.Party.Add(bibarel);
        // Before Celestic Town's pond
        game.Arrive("Sinnoh", 456, 539);
        var spot = FieldMoveRules.SpotOf(game.Map, 456, 539, Direction.Right, new Walker(TravelMode.OnFoot, game.Map.HeightAt(456, 539)));
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Surf, spot, game.Story));
        game.Story.GiveBadge(Badge.Fen);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Surf, spot, game.Story));
        // Faced, the water asks, and a yes rides out
        game.Tile = (456, 539);
        game.Facing = Direction.Right;
        var water = game.Play(Scripts.Find(FieldScripts.Water)!);
        Assert.Contains(water.Asked, a => a.Answer == ScriptRunner.YesNo[0]);
        Assert.Contains(water.Log, l => l.StartsWith("surf", StringComparison.Ordinal));
    }

    [Fact]
    public void TheElderAtHomeAndTheOldBookInHerHouse()
    {
        var game = NewGame();
        ThroughTheGrunt(game);
        BeatCyrus(game);
        game.Through("Sinnoh");
        game.Through("CelesticTownNorthHouse");
        Assert.Equal((5, 8), game.Tile);
        var elder = game.Present("elder")!;
        Assert.Equal((2, 6), (elder.GridX, elder.GridY));
        Assert.Contains(game.Talk("elder").Transcript, l => l.Text.Contains("Surf"));
        game.Story.Set(StoryMigration.ArrivedInCanalaveFlag);
        Assert.Contains(game.Talk("elder").Transcript, l => l.Text.Contains("oldest town"));
        // The old man tells of Mesprit to whoever wants to hear it
        Assert.Contains(game.Talk("expert_m").Transcript, l => l.Text.Contains("Uxie"));
        game.Answers.Enqueue(1);
        Assert.DoesNotContain(game.Talk("expert_m").Transcript, l => l.Text.Contains("Uxie"));
        // The book on the table and the scroll on the wall
        Assert.Contains("old book", game.Map.GetSignboardAt(5, 6));
        Assert.Contains("scroll", game.Map.GetSignboardAt(8, 3));
        // Out again, in front of the house's door
        game.Through("Sinnoh");
        Assert.Equal((463, 516), game.Tile);
    }

    // ------------------------------------------------------------------ the Fuego Ironworks

    [Fact]
    public void MrFuegoGivesAStarPieceAndTradesShardsForStarPieces()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 169, 592);
        game.Through("FuegoIronworksBuilding");
        Assert.Equal("FuegoIronworksBuilding", game.Map.Name);
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_FUEGO_IRONWORKS"));
        // The first visit: a Star Piece, and the offer to trade it at once
        game.Answers.Enqueue(1);
        var first = game.Talk("mr_fuego");
        Assert.Contains(first.Transcript, l => l.Text.Contains("moving floors"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("Star Piece")));
        Assert.True(game.Story.Has("FLAG_MR_FUEGO_ASKED_FOR_TRADE"));
        Assert.Contains(first.Transcript, l => l.Text.Contains("not trading"));
        // One for a Shard of each colour
        game.Talk("mr_fuego");
        Assert.Equal(0, game.Bag.GetQuantity(Item("Star Piece")));
        foreach (string shard in new[] { "Red Shard", "Blue Shard", "Yellow Shard", "Green Shard" }) Assert.Equal(1, game.Bag.GetQuantity(Item(shard)));
        // Ten at once
        game.Bag.AddItem(Item("Star Piece"), 12);
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(1);
        game.Talk("mr_fuego");
        Assert.Equal(2, game.Bag.GetQuantity(Item("Star Piece")));
        Assert.Equal(11, game.Bag.GetQuantity(Item("Green Shard")));
        // None left to trade, he only asks for some
        game.Bag.RemoveItem(Item("Star Piece"), 2);
        Assert.Contains("Star Pieces", game.Talk("mr_fuego").Transcript.Single().Text);
    }

    [Fact]
    public void TheIronworksFloorHasItsWorkersItemsAndMovingFloors()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 169, 592);
        game.Through("FuegoIronworksBuilding");
        foreach (string worker in new[] { "worker_dillan", "worker_holden", "worker_conrad" })
            Assert.True(game.Present(worker)!.IsTrainer, worker);
        Assert.Contains(game.Map.NPCs, n => n.IsItemBall && n.Item == "Fire Stone");
        Assert.Contains(game.Map.HiddenItems.Values, h => h.Item == "Star Piece");
        Assert.Contains(Enumerable.Range(0, game.Map.Width * game.Map.Height), i => game.Map.BehaviourAt(i % game.Map.Width, i / game.Map.Width) is TileBehavior.SlideEast or TileBehavior.SlideWest or TileBehavior.SlideNorth or TileBehavior.SlideSouth);
        // Out the way they came in
        game.Through("Sinnoh");
        Assert.Equal((169, 588), game.Tile);
    }

    // ------------------------------------------------------------------ the sea and Canalave City

    [Fact]
    public void TheSeaRoutesHaveTheirTrainersAndPalParksWorkers()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 175, 880);
        Assert.True(game.Present("tuber_trenton", "route_219")!.IsTrainer);
        Assert.True(game.Present("tuber_mariel", "route_219")!.IsTrainer);
        game.Arrive("Sinnoh", 290, 915);
        Assert.Contains("isn't open yet", game.Talk("worker_west", "route_221").Transcript.Single().Text);
        Assert.NotNull(game.Present("worker_east", "route_221"));
        // The blockade's people only rehearse; their Pokémon cry
        game.Arrive("Sinnoh", 80, 755);
        Assert.Contains("cry Clefairy", TalkFrom(game, "clefairy_south", "route_218", Direction.Left).Log);
        Assert.Contains(TalkFrom(game, "guitarist", "route_218", Direction.Left).Transcript, l => l.Text.Contains("Clefairy"));
    }

    [Theory]
    [InlineData(723, 0)]
    [InlineData(724, 1)]
    [InlineData(725, 2)]
    [InlineData(726, 0)]
    public void TheRivalBattlesOnCanalavesBridgeAndSendsThePlayerToIronIsland(int y, int starter)
    {
        var game = NewGame(starter: starter);
        game.Arrive("Sinnoh", 55, 724);
        game.Tile = (47, y);
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 47, y, game.Story));
        var bridge = game.Play(Scripts.Find("RivalOnTheBridge", "canalave_city")!);
        Assert.Contains($"battle rival_canalave_city_{StoryState.Starters[starter].ToLowerInvariant()} Won", bridge.Log);
        Assert.Contains(bridge.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Iron Island"));
        Assert.Equal(Direction.Left, bridge.PlayerFacing);
        Assert.Equal(1, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.False(game.Fires("RivalOnTheBridge"));
        game.Arrive("Sinnoh", 55, 724);
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
    }

    [Fact]
    public void LosingToTheRivalLeavesTheBridgeClearForAnotherTry()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Arrive("Sinnoh", 55, 724);
        game.Step("RivalOnTheBridge");
        Assert.Equal(0, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        game.Arrive("Sinnoh", 55, 724);
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.True(game.Fires("RivalOnTheBridge"));
    }

    [Fact]
    public void ByronsBadgeBringsTheRivalOutsideTheGym()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 55, 724);
        game.Step("RivalOnTheBridge");
        // As the Gym's script leaves it (CanalaveGym.txt): his flag cleared, state 2
        Run(game, "canalave_city", "givebadge mine\n clearflag FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE\n setvar VAR_CANALAVE_CITY_STATE 2");
        game.Arrive("Sinnoh", 55, 724);
        var rival = game.Present("rival_bridge", "canalave_city")!;
        Assert.Equal((39, 733), (rival.GridX, rival.GridY));
        Assert.Contains("Iron Island", game.Talk("rival_bridge", "canalave_city").Transcript.Single().Text);
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
        // The ruins' painting, the elder's house, the ironworks' furthest ball
        Assert.Contains((9, 3), walked[walked.Keys.Single(m => m.Name == "CelesticTownCave")]);
        Assert.Contains((2, 6), walked[walked.Keys.Single(m => m.Name == "CelesticTownNorthHouse")]);
        var ironworks = walked.Keys.Single(m => m.Name == "FuegoIronworksBuilding");
        Assert.Contains(ironworks.NPCs.Where(n => n.IsItemBall), ball => walked[ironworks].Any(t => Math.Abs(t.X - ball.GridX) + Math.Abs(t.Y - ball.GridY) == 1));
        var sinnoh = walked.Keys.Single(m => m.Name == "Sinnoh");
        // Celestic Town past the Psyduck and through the fog, Canalave's bridge past the blockade, the sea routes
        Assert.Contains((463, 530), walked[sinnoh]);
        Assert.Contains((47, 724), walked[sinnoh]);
        Assert.Contains((175, 880), walked[sinnoh]);
        Assert.Contains((290, 915), walked[sinnoh]);
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKnowsWhatItHasDone()
    {
        var before = new StoryState();
        StoryMigration.Upgrade(before, 10, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_210_SOUTH_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS",
            "FLAG_HIDE_CELESTIC_TOWN_CAVE_ELDER", "FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER", "FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE" })
            Assert.True(before.Has(flag), flag);
        Assert.False(before.Has("FLAG_HIDE_ROUTE_218_BLOCKADE"));
        Assert.False(before.Has("FLAG_USED_SECRETPOTION"));

        // Psyduck cured before Cynthia came after them: her errand is the save's now. Been to Canalave: the blockade has
        // gone. The Vs. Seeker in the bag: its first level of rematches. Byron beaten: the rival is as his Gym left him
        var ahead = new StoryState();
        ahead.Set(StoryMigration.PsyduckCuredFlag);
        ahead.Set(StoryMigration.ArrivedInCanalaveFlag);
        ahead.GiveBadge(Badge.Mine);
        var bag = new Inventory();
        bag.AddItem(Item(VsSeeker.Item));
        StoryMigration.Upgrade(ahead, 10, Array.Empty<Pokemon>(), Scripts, bag);
        Assert.Equal(1, bag.GetQuantity(Item("Old Charm")));
        Assert.True(ahead.Has("FLAG_USED_SECRETPOTION"));
        Assert.True(ahead.Has("FLAG_HIDE_ROUTE_218_BLOCKADE"));
        Assert.True(ahead.Has(VsSeeker.LevelFlag(1)));
        Assert.False(ahead.Has("FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE"));
        Assert.True(StoryState.CurrentVersion >= 11);
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_210_SOUTH_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS", "FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE" })
        {
            Assert.True(fresh.Has(flag), flag);
            Assert.True(upgraded.Has(flag), flag);
        }
    }

    [Fact]
    public void TheVsSeekerFromRoute207UnlocksTheFirstLevelOfRematches()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 345, 655);
        game.Step("Assistant");
        Assert.Equal(1, game.Bag.GetQuantity(Item(VsSeeker.Item)));
        Assert.True(game.Story.Has(VsSeeker.LevelFlag(1)));
    }
}
