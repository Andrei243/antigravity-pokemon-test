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
/// The seventh chapter (plan 02 · S10, "Iron Island and the lakes") played through with no screen, in the order a
/// player meets it: Riley's HM04 on Iron Island, his partnership two floors down, the grunts before the lift and the
/// Riolu Egg; Byron's Badge and the rival's call to the library; the meeting on its third floor and the explosion; the
/// news outside; Lake Valor drained, its cameramen gone, and Commander Saturn in Valor Cavern; Lake Verity with the
/// professor, the assistant and Commander Mars; Mt. Coronet's grunts gone, and the road north to Route 217, Maylene and
/// HM08. Each scene is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class IronIslandAndTheLakesTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won, int starter = 0, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(starter, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get(new[] { "Torterra", "Infernape", "Empoleon" }[starter])!, 45));
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, string? place, Direction facing)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        return game.Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name)!, npc);
    }

    /// <summary>Reads what a signboard of a room runs, from the tile below it.</summary>
    private static HeadlessScriptHost ReadBook(OpeningTests.Game game, int x, int y, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        game.Tile = (x, y + 1);
        game.Facing = Direction.Up;
        return game.Play(Scripts.Find(game.Map.SignScripts[(x, y)], game.Map.Name)!);
    }

    /// <summary>Byron beaten, as his Gym's script beats him (CanalaveGym.txt, plan 01 · M9), and out of its door.</summary>
    private static HeadlessScriptHost WinTheMineBadge(OpeningTests.Game game)
    {
        game.Arrive("CanalaveGym", 16, 26);
        game.Talk("byron");
        Assert.True(game.Story.HasBadge("Sinnoh", "Mine"));
        Assert.Equal(2, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        game.Through("Sinnoh");
        return game.Last;
    }

    /// <summary>Riley's HM04 taken at the tunnels' door on Iron Island.</summary>
    private static void TakeStrength(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 117, 493);
        game.Talk("riley", "iron_island");
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM04")));
    }

    /// <summary>The rival followed into the library and up to its third floor, where the meeting plays; then out again to the news.</summary>
    private static (HeadlessScriptHost Meeting, HeadlessScriptHost News) ThroughTheLibrary(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 37, 722);
        game.Talk("rival_library", "canalave_city");
        game.Through("CanalaveLibrary1F");
        game.Through("CanalaveLibrary2F");
        game.Through("CanalaveLibrary3F");
        var meeting = game.Last;
        game.Through("CanalaveLibrary2F");
        game.Through("CanalaveLibrary1F");
        game.Through("Sinnoh");
        return (meeting, game.Last);
    }

    /// <summary>The story as the library leaves it, for the scenes after: the Badge, HM04, the explosion and its news.</summary>
    private static void PastTheLibrary(OpeningTests.Game game)
    {
        TakeStrength(game);
        WinTheMineBadge(game);
        ThroughTheLibrary(game);
        Assert.Equal(5, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
    }

    /// <summary>Commander Saturn beaten in Valor Cavern, spoken to from below.</summary>
    private static HeadlessScriptHost BeatSaturn(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 722, 760);
        game.Through("LakeValorDrained");
        game.Through("ValorCavern");
        return TalkFrom(game, "saturn", null, Direction.Up);
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameKeepsTheChaptersPeopleOutOfSightUntilTheirScenes()
    {
        var game = NewGame();
        // Canalave City: nobody before the library's door or outside it, and the door open
        game.Arrive("Sinnoh", 37, 722);
        foreach (string key in new[] { "rival_library", "rival_bridge", "prof_rowan", "counterpart", "sailor" })
            Assert.Null(game.Present(key, "canalave_city"));
        Assert.Equal("CanalaveLibrary1F", game.Map.GetWarpAt(37, 718)!.TargetMap);
        // The library: its readers, and nobody of the meeting
        game.Through("CanalaveLibrary1F");
        Assert.Equal((11, 11), game.Tile);
        Assert.NotNull(game.Present("receptionist"));
        game.Through("CanalaveLibrary2F");
        game.Through("CanalaveLibrary3F");
        Assert.Empty(game.Last.Transcript);
        foreach (string key in new[] { "rival", "prof_rowan", "counterpart" }) Assert.Null(game.Present(key));
        Assert.NotNull(game.Present("scientist_f"));

        // Iron Island: Riley before the tunnels' door, and two floors down with the grunts before the lift
        game.Arrive("Sinnoh", 117, 493);
        var riley = game.Present("riley", "iron_island")!;
        Assert.Equal((117, 490), (riley.GridX, riley.GridY));
        game.Arrive("IronIslandB2FLeft", 37, 3);
        var below = game.Present("riley")!;
        Assert.Equal((34, 3), (below.GridX, below.GridY));
        Assert.NotNull(game.Present("grunt_m_west"));
        Assert.NotNull(game.Present("grunt_m_east"));
        Assert.True(game.Fires("Join"));
        Assert.False(game.Fires("Leave"));
        Assert.False(game.Fires("Grunts"));

        // Valor Lakefront: the cameramen keep the lake shut, and its way in leads to the lake Team Galactic drained
        game.Arrive("Sinnoh", 722, 760);
        Assert.NotNull(game.Map.GetNpcAt(718, 760));
        Assert.NotNull(game.Map.GetNpcAt(718, 761));
        Assert.Equal("LakeValorDrained", game.Map.GetWarpAt(717, 760)!.TargetMap);
        Assert.Equal("LakeValorDrained", game.Map.GetWarpAt(717, 761)!.TargetMap);

        // Verity Lakefront leads to the lake as it was; nobody stands by its shore
        game.Arrive("Sinnoh", 80, 846);
        Assert.Equal("LakeVerity", game.Map.GetWarpAt(80, 843)!.TargetMap);
        game.Through("LakeVerity");
        Assert.Null(game.Present("prof_rowan"));
        Assert.Null(game.Present("counterpart"));

        // Mt. Coronet's northern hall: the two grunts across the way to the stairs down
        game.Arrive("MtCoronet1FNorthRoom1", 20, 10);
        Assert.NotNull(game.Map.GetNpcAt(14, 10));
        Assert.NotNull(game.Map.GetNpcAt(14, 11));

        // Route 217: Maylene not yet, her trigger waiting, HM08 in its ball
        game.Arrive("Sinnoh", 303, 380);
        Assert.Null(game.Present("maylene", "route_217"));
        Assert.True(game.Fires("Maylene"));
        Assert.Equal("HM08", game.Map.GetNpcAt(296, 305)!.Item);
    }

    // ------------------------------------------------------------------ Iron Island

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RileyGivesStrengthBeforeTheTunnelsAndGoesIn(bool badge)
    {
        var game = NewGame();
        if (badge) game.Story.GiveBadge("Sinnoh", "Mine");
        game.Arrive("Sinnoh", 117, 493);
        // He stands in the tunnels' doorway until spoken to
        Assert.Equal("IronIsland1F", game.Map.GetWarpAt(117, 489)!.TargetMap);
        var gift = game.Talk("riley", "iron_island");
        Assert.Contains(gift.Transcript, l => l.Speaker == "Riley" && l.Text.Contains("My name is Riley"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM04")));
        Assert.Contains(gift.Transcript, l => l.Text.Contains(badge ? "on you already" : "Something to aim for"));
        Assert.Null(game.Present("riley", "iron_island"));
        Assert.True(game.Story.Has("FLAG_HIDE_IRON_ISLAND_RILEY"));
        game.Arrive("Sinnoh", 117, 493);
        Assert.Null(game.Map.GetNpcAt(117, 490));

        // HM04 teaches Strength, which the Mine Badge lets a Pokémon use outside battle
        var machoke = new Pokemon(PokemonDatabase.Get("Machoke")!, 30);
        Assert.True(MoveTeaching.CanLearn(machoke, Item("HM04")));
        Assert.Equal("Mine", FieldMoveRules.BadgeFor(FieldMove.Strength, "Sinnoh"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void RileyTeamsUpAsThePlayerWalksUpToHimAndStaysBehindIfTheyTurnBack(int y)
    {
        var game = NewGame();
        game.Arrive("IronIslandB2FLeft", 37, 3);
        game.Tile = (35, y);
        game.Facing = Direction.Left;
        Assert.Same(game.Map.Triggers.First(t => t.Script == "Join"), FieldScripts.TriggerAt(game.Map, 35, y, game.Story));
        var join = game.Play(Scripts.Find("Join", "iron_island_b2f_left_room")!);
        Assert.Contains(join.Transcript, l => l.Speaker == "Riley" && l.Text.Contains("team up"));
        Assert.Contains(join.Transcript, l => l.Text.Contains("decided to go with Riley"));
        Assert.Contains("partner Riley riley_iron_island", join.Log);
        Assert.Equal(1, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));
        var riley = game.Present("riley")!;
        Assert.Equal((34, y), (riley.GridX, riley.GridY));
        Assert.False(game.Fires("Join"));
        // His partner is Platinum's, with a Lucario
        Assert.Equal("Lucario", TrainerDatabase.Get("riley_iron_island")!.Party.Single().Species);

        // On the way he has four things to say, and a fifth again and again
        Assert.Contains("side by side", game.Talk("riley").Transcript.Single().Text);
        Assert.Contains("iron ore", game.Talk("riley").Transcript.Single().Text);
        Assert.Contains("Byron", game.Talk("riley").Transcript.Single().Text);
        Assert.Contains("Steel against steel", game.Talk("riley").Transcript.Single().Text);
        Assert.Contains("afraid of something", game.Talk("riley").Transcript.Single().Text);
        Assert.Contains("afraid of something", game.Talk("riley").Transcript.Single().Text);

        // Turning back for the way out, he stays where he first stood, and joins again on the way back in
        game.Tile = (36, y);
        game.Facing = Direction.Right;
        Assert.Same(game.Map.Triggers.First(t => t.Script == "Leave"), FieldScripts.TriggerAt(game.Map, 36, y, game.Story));
        var leave = game.Play(Scripts.Find("Leave", "iron_island_b2f_left_room")!);
        Assert.Contains("partner off", leave.Log);
        Assert.Equal(0, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));
        Assert.Equal((34, 3), (riley.GridX, riley.GridY));
        Assert.Equal(Direction.Left, leave.PlayerFacing);
        game.Tile = (35, y);
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 35, y, game.Story));
        var again = game.Play(Scripts.Find("Join", "iron_island_b2f_left_room")!);
        Assert.Contains(again.Transcript, l => l.Text.Contains("Back again"));
        Assert.Equal(1, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));
    }

    private static void JoinRiley(OpeningTests.Game game)
    {
        game.Arrive("IronIslandB2FLeft", 37, 3);
        game.Facing = Direction.Left;
        game.Step("Join");
    }

    [Theory]
    [InlineData(40)]
    [InlineData(41)]
    public void TheGruntsBeforeTheLiftBattleThePairAndRileyGivesTheRioluEgg(int y)
    {
        var game = NewGame();
        JoinRiley(game);
        game.Tile = (21, y);
        game.Facing = Direction.Left;
        Assert.Same(game.Map.Triggers.First(t => t.Script == "Grunts"), FieldScripts.TriggerAt(game.Map, 21, y, game.Story));
        // No to the Egg the first time: he asks again
        game.Answers.Enqueue(1);
        game.Answers.Enqueue(0);
        var fight = game.Play(Scripts.Find("Grunts", "iron_island_b2f_left_room")!);
        Assert.Contains("partner off", fight.Log);
        Assert.Contains("battle galactic_grunt_iron_island_1 and galactic_grunt_iron_island_2 with riley_iron_island Won", fight.Log);
        Assert.Contains(fight.Transcript, l => l.Speaker == "Riley" && l.Text.Contains("frightening the Pokémon"));
        Assert.Contains(fight.Transcript, l => l.Speaker == "Galactic Grunt" && l.Text.Contains("big plan"));
        // They stood side by side on the tiles before the grunts
        Assert.Equal((20, y), game.Tile);
        // The grunts have gone for good, and the lift is clear
        Assert.Null(game.Present("grunt_m_west"));
        Assert.Null(game.Present("grunt_m_east"));
        Assert.True(game.Story.Has("FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_GRUNTS"));
        Assert.True(game.Story.HasDefeated("galactic_grunt_iron_island_1"));
        // The Egg, refused once and then taken
        Assert.Equal(2, fight.Asked.Count(a => a.Question.Contains("Pokémon Egg")));
        Assert.Contains(fight.Transcript, l => l.Text.Contains("token"));
        var egg = Assert.Single(game.Party.Members, p => p.IsEgg);
        Assert.Equal("Riolu", egg.Species.Name);
        Assert.True(game.Story.Has("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY"));
        Assert.False(game.Story.Has("FLAG_COULD_NOT_RECEIVE_RIOLU_EGG"));
        Assert.Equal(2, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));
        // Riley has gone, for good
        Assert.Null(game.Present("riley"));
        Assert.True(game.Story.Has("FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_RILEY"));
        game.Arrive("IronIslandB2FLeft", 37, 3);
        Assert.Null(game.Present("riley"));
        Assert.Null(game.Present("grunt_m_west"));
        Assert.False(game.Fires("Join"));
        Assert.False(game.Fires("Grunts"));
    }

    [Fact]
    public void WithAFullTeamRileyKeepsTheEggByTheLiftUntilThereIsRoom()
    {
        var game = NewGame();
        for (int i = 0; i < 5; i++) game.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 20));
        JoinRiley(game);
        game.Tile = (21, 40);
        game.Facing = Direction.Left;
        var fight = game.Play(Scripts.Find("Grunts", "iron_island_b2f_left_room")!);
        Assert.Contains(fight.Transcript, l => l.Text.Contains("no room for an Egg"));
        Assert.DoesNotContain(game.Party.Members, p => p.IsEgg);
        Assert.True(game.Story.Has("FLAG_COULD_NOT_RECEIVE_RIOLU_EGG"));
        Assert.Equal(2, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));

        // Back again with a place free: he waits by the lift, where the grunts stood, and hands it over
        game.Arrive("IronIslandB2FLeft", 37, 3);
        var riley = game.Present("riley")!;
        Assert.Equal((19, 41, Direction.Right), (riley.GridX, riley.GridY, riley.Facing));
        game.Party.Members.RemoveAt(5);
        var given = TalkFrom(game, "riley", null, Direction.Left);
        Assert.Contains(given.Transcript, l => l.Text.Contains("received the Egg"));
        Assert.Single(game.Party.Members, p => p.IsEgg);
        Assert.False(game.Story.Has("FLAG_COULD_NOT_RECEIVE_RIOLU_EGG"));
        Assert.True(game.Story.Has("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY"));
        Assert.Null(game.Present("riley"));
    }

    [Fact]
    public void LosingToTheGruntsLeavesThemBeforeTheLiftAndRileyWhereHeFirstStood()
    {
        var game = NewGame(BattleOutcome.Lost);
        JoinRiley(game);
        game.Tile = (21, 41);
        game.Facing = Direction.Left;
        var fight = game.Play(Scripts.Find("Grunts", "iron_island_b2f_left_room")!);
        Assert.Contains("battle galactic_grunt_iron_island_1 and galactic_grunt_iron_island_2 with riley_iron_island Lost", fight.Log);
        Assert.DoesNotContain(game.Party.Members, p => p.IsEgg);
        Assert.False(game.Story.Has("FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_GRUNTS"));
        game.Arrive("IronIslandB2FLeft", 37, 3);
        Assert.Equal(0, game.Story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE"));
        var riley = game.Present("riley")!;
        Assert.Equal((34, 3), (riley.GridX, riley.GridY));
        Assert.NotNull(game.Present("grunt_m_west"));
        Assert.True(game.Fires("Join"));
    }

    [Theory]
    [InlineData("grunt_m_west", 40)]
    [InlineData("grunt_m_east", 41)]
    public void AGruntSpokenToStartsTheSameSceneWithRileyAndSendsAPlayerAloneAway(string grunt, int y)
    {
        var alone = NewGame();
        alone.Arrive("IronIslandB2FLeft", 37, 3);
        var sent = TalkFrom(alone, grunt, null, Direction.Left);
        Assert.Contains(sent.Transcript, l => l.Text.Contains("Clear off"));
        Assert.DoesNotContain(sent.Log, l => l.StartsWith("battle", StringComparison.Ordinal));

        var game = NewGame();
        JoinRiley(game);
        var fight = TalkFrom(game, grunt, null, Direction.Left);
        Assert.Contains("battle galactic_grunt_iron_island_1 and galactic_grunt_iron_island_2 with riley_iron_island Won", fight.Log);
        Assert.Equal((20, y), game.Tile);
        Assert.Single(game.Party.Members, p => p.IsEgg);
    }

    [Fact]
    public void TheRoomsTrainersStandInPairsForTheTwoOfThem()
    {
        var game = NewGame();
        game.Arrive("IronIslandB2FLeft", 37, 3);
        foreach (string key in new[] { "hiker_damon", "hiker_maurice", "black_belt_kendal", "battle_girl_tyler", "worker_brendon", "worker_quentin", "ace_trainer_jonah", "ace_trainer_brenda" })
            Assert.True(game.Present(key)!.IsTrainer, key);
        // The two grunts are Platinum's, and battle only in their scene
        foreach (var (key, id) in new[] { ("grunt_m_west", "galactic_grunt_iron_island_1"), ("grunt_m_east", "galactic_grunt_iron_island_2") })
        {
            var grunt = game.Present(key)!;
            Assert.Equal((id, 0), (grunt.TrainerData!.Id, grunt.TrainerData.SightRange));
            Assert.Equal("Grunts", grunt.Script);
        }
    }

    // ------------------------------------------------------------------ Canalave City and its library

    [Fact]
    public void OutOfTheGymTheRivalCallsThePlayerToTheLibraryAndRunsThere()
    {
        var game = NewGame();
        var called = WinTheMineBadge(game);
        Assert.Equal((39, 732), game.Tile);
        Assert.Contains(called.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("library"));
        Assert.Equal(Direction.Left, called.PlayerFacing);
        Assert.Equal(3, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_CANALAVE_LIBRARY_STATE"));
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE"));
        // He waits before the library's door
        var rival = game.Present("rival_library", "canalave_city")!;
        Assert.Equal((37, 719), (rival.GridX, rival.GridY));
        // The next arrival has nothing more to say
        game.Arrive("Sinnoh", 39, 732);
        Assert.Empty(game.Last.Transcript);
        // A man in town has seen him stamping about
        Assert.Contains(game.Talk("pokemon_breeder_m", "canalave_city").Transcript, l => l.Text.Contains("terrible hurry"));
    }

    [Fact]
    public void ComingBackSomeOtherWayTheRivalHasGoneToTheLibraryAlready()
    {
        var game = NewGame();
        Run(game, "canalave_city", "givebadge mine\n clearflag FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE\n clearflag FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY\n setvar VAR_CANALAVE_CITY_STATE 2");
        game.Arrive("Sinnoh", 55, 724);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal(3, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_CANALAVE_LIBRARY_STATE"));
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.NotNull(game.Present("rival_library", "canalave_city"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheRivalKeepsTheLibrarysDoorUntilStrengthIsThePlayers(bool knows)
    {
        var game = NewGame();
        WinTheMineBadge(game);
        game.Arrive("Sinnoh", 37, 722);
        var refused = game.Talk("rival_library", "canalave_city");
        Assert.Contains(refused.Transcript, l => l.Text.Contains("Iron Island"));
        Assert.NotNull(game.Map.GetNpcAt(37, 719));
        Assert.False(game.Story.Has("FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY"));

        if (knows)
        {
            var machoke = new Pokemon(PokemonDatabase.Get("Machoke")!, 30);
            MoveTeaching.Learn(machoke, "Strength", forget: 0);
            game.Party.Add(machoke);
        }
        else game.Bag.AddItem(Item("HM04"));
        var let = game.Talk("rival_library", "canalave_city");
        Assert.Contains(let.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("too slow"));
        Assert.Null(game.Present("rival_library", "canalave_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY"));
        game.Arrive("Sinnoh", 37, 722);
        Assert.Null(game.Map.GetNpcAt(37, 719));
    }

    [Fact]
    public void TheLibrarysFloorsAreJoinedByTheirStairsAndItsDoorLeadsBackToTown()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 37, 722);
        game.Through("CanalaveLibrary1F");
        Assert.Equal((11, 11), game.Tile);
        game.Through("CanalaveLibrary2F");
        Assert.Equal((3, 4), game.Tile);
        game.Through("CanalaveLibrary3F");
        Assert.Equal((3, 3), game.Tile);
        game.Through("CanalaveLibrary2F");
        Assert.Equal((12, 4), game.Tile);
        game.Through("CanalaveLibrary1F");
        Assert.Equal((12, 4), game.Tile);
        game.Through("Sinnoh");
        Assert.Equal((37, 719), game.Tile);

        // Its readers, before the explosion
        game.Through("CanalaveLibrary1F");
        Assert.Contains(game.Talk("receptionist").Transcript, l => l.Text.Contains("voice down"));
        var lass = game.Talk("lass");
        Assert.Contains(lass.Transcript, l => l.Text.Contains("disturb"));
        Assert.Equal(Direction.Up, game.Present("lass")!.Facing);
        Assert.Contains("crammed", game.Map.GetSignboardAt(3, 4));
        game.Through("CanalaveLibrary2F");
        Assert.Contains(game.Talk("school_kid_m").Transcript, l => l.Text.Contains("third floor"));
        Assert.Contains("No food", game.Map.GetSignboardAt(13, 3));
    }

    [Theory]
    [InlineData(PlayerLook.Boy)]
    [InlineData(PlayerLook.Girl)]
    public void TheMeetingInTheLibraryEndsInTheExplosionAndTheNewsOutside(PlayerLook look)
    {
        var game = NewGame(look: look);
        TakeStrength(game);
        WinTheMineBadge(game);
        var (meeting, news) = ThroughTheLibrary(game);

        // The rival takes the player to the table; the professor asks for help with the lakes, and shares them out
        Assert.Contains(meeting.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Over here"));
        Assert.Contains(meeting.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("I'm staying"));
        Assert.Contains(meeting.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("three great lakes"));
        Assert.Contains(meeting.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("Lake Verity"));
        Assert.Contains(meeting.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("Lake Acuity"));
        Assert.Contains(meeting.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("Lake Valor"));
        string assistant = PlayerIdentity.DefaultName(look == PlayerLook.Boy ? PlayerLook.Girl : PlayerLook.Boy);
        Assert.Contains(meeting.Transcript, l => l.Speaker == assistant && l.Text.Contains(look == PlayerLook.Boy ? "count on me" : "You bet"));
        // The explosion, the television's news, and everyone out
        Assert.Contains("sound thunder_rumble", meeting.Log);
        Assert.Contains("camera Shake", meeting.Log);
        Assert.Contains(meeting.Transcript, l => l.Speaker == "TV" && l.Text.Contains("Lake Valor"));
        Assert.Contains(meeting.Transcript, l => l.Text.Contains("Don't dawdle"));
        Assert.True(game.Story.Has("FLAG_LAKE_VALOR_EXPLODED"));
        Assert.True(game.Story.Has("FLAG_HIDE_VALOR_LAKEFRONT_CAMERAMEN"));
        Assert.Equal(2, game.Story.Var("VAR_CANALAVE_LIBRARY_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_CANALAVE_LIBRARY_RIVAL", "FLAG_HIDE_CANALAVE_LIBRARY_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN" })
            Assert.True(game.Story.Has(flag), flag);

        // Outside: a sailor brings the news, the rival runs for Lake Acuity, and the professor sends the player to Lake Valor
        Assert.Equal((37, 719), game.Tile);
        Assert.Contains(news.Transcript, l => l.Speaker == "Sailor" && l.Text.Contains("Lake Valor"));
        Assert.Contains(news.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Lake Acuity"));
        Assert.Contains(news.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("I need you at Lake Valor"));
        Assert.Contains(news.Transcript, l => l.Speaker == assistant && l.Text.Contains("Lake Verity"));
        Assert.Equal(5, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.Null(game.Present("sailor", "canalave_city"));
        Assert.Null(game.Present("rival_bridge", "canalave_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_CANALAVE_CITY_SAILOR"));
        Assert.True(game.Story.Has("FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE"));
        foreach (string flag in new[] { "FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN", "FLAG_HIDE_LAKE_ACUITY_LOW_WATER_RIVAL" })
            Assert.False(game.Story.Has(flag), flag);
        // The professor and the assistant wait outside the library until the player has left town
        var rowan = game.Present("prof_rowan", "canalave_city")!;
        Assert.Equal((38, 720), (rowan.GridX, rowan.GridY));
        Assert.Contains(TalkFrom(game, "prof_rowan", "canalave_city", Direction.Up).Transcript, l => l.Text.Contains("east of Pastoria"));
        Assert.Contains(TalkFrom(game, "counterpart", "canalave_city", Direction.Up).Transcript, l => l.Text.Contains("Lake Verity"));
        game.Arrive("Sinnoh", 55, 724);
        Assert.Equal(6, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.Null(game.Present("prof_rowan", "canalave_city"));
        Assert.Null(game.Present("counterpart", "canalave_city"));
    }

    [Fact]
    public void AfterTheExplosionTheTownAndTheLibraryTalkOfTheTremor()
    {
        var game = NewGame();
        PastTheLibrary(game);
        Assert.Contains(game.Talk("collector", "canalave_city").Transcript, l => l.Text.Contains("no earthquake"));
        Assert.Contains(game.Talk("old_man", "canalave_city").Transcript, l => l.Text.Contains("explosion"));
        Assert.Contains(game.Talk("ace_trainer_f", "canalave_city").Transcript, l => l.Text.Contains("Lake Valor"));
        Assert.Contains(game.Talk("lass", "canalave_city").Transcript, l => l.Text.Contains("tremor"));
        Assert.Contains(game.Talk("pokemon_breeder_m", "canalave_city").Transcript, l => l.Text.Contains("tremor"));
        var psyduck = game.Talk("psyduck", "canalave_city");
        Assert.Contains("cry Psyduck", psyduck.Log);
        game.Through("CanalaveLibrary1F");
        Assert.Contains(game.Talk("receptionist").Transcript, l => l.Text.Contains("shriek"));
        Assert.Contains(game.Talk("lass").Transcript, l => l.Text.Contains("flutter"));
        game.Through("CanalaveLibrary2F");
        Assert.Contains(game.Talk("school_kid_m").Transcript, l => l.Text.Contains("wicked"));
        game.Through("CanalaveLibrary3F");
        Assert.Empty(game.Last.Transcript);
        Assert.Contains(game.Talk("scientist_f").Transcript, l => l.Text.Contains("earthquake"));
        Assert.Contains(game.Talk("youngster").Transcript, l => l.Text.Contains("wobbly"));
        // Mom at home has felt it too
        game.Arrive("PlayerHouse", 6, 10);
        Assert.Contains(game.Talk("mom").Transcript, l => l.Text.Contains("whole house shook"));
    }

    [Fact]
    public void TheLibrarysBooksAreReadOnAYesAndPutBackOnANo()
    {
        var game = NewGame();
        game.Arrive("CanalaveLibrary3F", 3, 3);
        Assert.Contains(ReadBook(game, 9, 3).Transcript, l => l.Text.Contains("tall grass"));
        Assert.Contains(ReadBook(game, 10, 3, 1).Transcript, l => l.Text.Contains("put the book back"));
        Assert.Contains(ReadBook(game, 11, 3).Transcript, l => l.Text.Contains("three Pokémon"));
        Assert.Contains(ReadBook(game, 17, 3).Transcript, l => l.Text.Contains("rubbed away"));
        // The longer ones ask whether to read on
        var veilstone = ReadBook(game, 19, 3);
        Assert.Equal(3, veilstone.Asked.Count);
        Assert.Contains(veilstone.Transcript, l => l.Text.Contains("broke his sword"));
        var stopped = ReadBook(game, 20, 3, 0, 1);
        Assert.DoesNotContain(stopped.Transcript, l => l.Text.Contains("broke his sword"));
        Assert.Contains(stopped.Transcript, l => l.Text.Contains("put the book back"));
        Assert.Contains(ReadBook(game, 15, 9).Transcript, l => l.Text.Contains("Original One"));
        Assert.Contains(ReadBook(game, 17, 9).Transcript, l => l.Text.Contains("eyes"));
        // The folk stories are three to choose from
        Assert.Contains(ReadBook(game, 19, 9, 0).Transcript, l => l.Text.Contains("bones"));
        Assert.Contains(ReadBook(game, 19, 9, 1).Transcript, l => l.Text.Contains("hide"));
        Assert.Contains(ReadBook(game, 20, 9, 2).Transcript, l => l.Text.Contains("same table"));
        Assert.Contains(ReadBook(game, 20, 9, 3).Transcript, l => l.Text.Contains("put the book back"));
        Assert.Contains("third floor", game.Map.GetSignboardAt(14, 2));
    }

    // ------------------------------------------------------------------ Lake Valor

    [Fact]
    public void TheCameramenKeepLakeValorShutUntilTheExplosion()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 722, 760);
        Assert.Contains(game.Map.GetNpcAt(718, 760)!.DialogLines, l => l.Contains("filming"));
        Assert.Contains(game.Map.GetNpcAt(718, 761)!.DialogLines, l => l.Contains("legendary Pokémon"));
        PastTheLibrary(game);
        game.Arrive("Sinnoh", 722, 760);
        Assert.Null(game.Map.GetNpcAt(718, 760));
        Assert.Null(game.Map.GetNpcAt(718, 761));
    }

    [Fact]
    public void LakeValorLiesDrainedWithItsMagikarpAndTeamGalacticsGrunts()
    {
        var game = NewGame();
        PastTheLibrary(game);
        game.Arrive("Sinnoh", 722, 760);
        game.Through("LakeValorDrained");
        Assert.Equal((51, 10), game.Tile);
        // Twenty-three Magikarp left high and dry
        var carp = game.Map.NPCs.Where(n => n.Species == "Magikarp").ToList();
        Assert.Equal(23, carp.Count);
        Assert.Contains("cry Magikarp", game.Talk("magikarp_1").Log);
        // The grunts who talk, and the three who battle with Platinum's teams
        Assert.Contains(game.Talk("grunt_m_2").Transcript, l => l.Text.Contains("Lake Verity"));
        Assert.Contains(game.Talk("grunt_f").Transcript, l => l.Text.Contains("Magikarp"));
        foreach (string key in new[] { "galactic_grunt_lake_valor_1", "galactic_grunt_lake_valor_2", "galactic_grunt_lake_valor_3" })
            Assert.Equal(key, game.Present(key)!.TrainerData!.Id);
        // Its TM lies on a shelf only the water leads to, and waits for the lake as it was
        Assert.DoesNotContain(game.Map.Everyone, n => n.IsItemBall);
        // The cavern's mouth is open in the middle of the lake bed, and the way out leads back to the lakefront
        Assert.Equal("ValorCavern", game.Map.GetWarpAt(32, 32)!.TargetMap);
        game.Through("Sinnoh");
        Assert.Equal((718, 760), game.Tile);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Down)]
    public void CommanderSaturnBeatenInValorCavernLeavesTheLakeToItsMagikarp(Direction facing)
    {
        var game = NewGame();
        PastTheLibrary(game);
        game.Arrive("Sinnoh", 722, 760);
        game.Through("LakeValorDrained");
        game.Through("ValorCavern");
        Assert.Equal((14, 28), game.Tile);
        var saturn = game.Present("saturn")!;
        Assert.Equal((14, 19, Direction.Up), (saturn.GridX, saturn.GridY, saturn.Facing));
        var fight = TalkFrom(game, "saturn", null, facing);
        Assert.Contains(fight.Transcript, l => l.Speaker == "Commander Saturn" && l.Text.Contains("mission"));
        Assert.Contains("battle commander_saturn_valor_cavern Won", fight.Log);
        Assert.Contains(fight.Transcript, l => l.Text.Contains("Lake Verity"));
        Assert.Null(game.Present("saturn"));
        foreach (string flag in new[] { "FLAG_HIDE_LAKE_VALOR_GALACTIC", "FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN", "FLAG_ALT_MUSIC_LAKE_VALOR" })
            Assert.True(game.Story.Has(flag), flag);
        // The grunts who only talked have gone from the lake with him; the three who battle stay, and so do the Magikarp
        game.Through("LakeValorDrained");
        Assert.Equal((32, 33), game.Tile);
        foreach (string key in new[] { "grunt_m_1", "grunt_f", "grunt_m_2" }) Assert.Null(game.Present(key));
        Assert.NotNull(game.Present("galactic_grunt_lake_valor_1"));
        Assert.Equal(23, game.Map.NPCs.Count(n => n.Species == "Magikarp"));
        // Verity Lakefront now leads to the lake Team Galactic holds
        game.Arrive("Sinnoh", 80, 846);
        Assert.Equal("LakeVerityGalactic", game.Map.GetWarpAt(80, 843)!.TargetMap);
        Assert.Equal("LakeVerityGalactic", game.Map.GetWarpAt(81, 843)!.TargetMap);
    }

    [Fact]
    public void LosingToSaturnLeavesHimInTheCavern()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Story.Set("FLAG_HIDE_VALOR_LAKEFRONT_CAMERAMEN");
        var lost = BeatSaturn(game);
        Assert.Contains("battle commander_saturn_valor_cavern Lost", lost.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN"));
        game.Arrive("ValorCavern", 14, 28);
        Assert.NotNull(game.Present("saturn"));
        game.Arrive("Sinnoh", 80, 846);
        Assert.Equal("LakeVerity", game.Map.GetWarpAt(80, 843)!.TargetMap);
    }

    // ------------------------------------------------------------------ Lake Verity

    [Fact]
    public void BeforeSaturnIsBeatenTheProfessorAndTheAssistantFindLakeVerityQuiet()
    {
        var game = NewGame();
        PastTheLibrary(game);
        game.Arrive("Sinnoh", 80, 846);
        game.Through("LakeVerity");
        var rowan = game.Present("prof_rowan")!;
        Assert.Equal((48, 43), (rowan.GridX, rowan.GridY));
        Assert.Contains(game.Talk("prof_rowan").Transcript, l => l.Text.Contains("Not a ripple"));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_LAKE_VERITY_LOW_WATER_PROF_ROWAN"));
        Assert.DoesNotContain(game.Talk("prof_rowan").Transcript, l => l.Text.Contains("Not a ripple"));
        Assert.Contains(game.Talk("counterpart").Transcript, l => l.Text.Contains("Lake Valor"));
    }

    [Theory]
    [InlineData(46, PlayerLook.Boy)]
    [InlineData(47, PlayerLook.Girl)]
    public void AtLakeVerityTheProfessorCallsThePlayerInAndCommanderMarsIsBeaten(int x, PlayerLook look)
    {
        var game = NewGame(look: look);
        PastTheLibrary(game);
        BeatSaturn(game);
        game.Arrive("Sinnoh", x == 46 ? 80 : 81, 846);
        game.Map.Warps.Single(w => w.TargetMap == "LakeVerityGalactic" && w.TargetX == x);
        game.Arrive("LakeVerityGalactic", x, 53);
        // The professor, held back by a grunt, sends the player on
        var call = game.Last;
        Assert.Contains(call.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("Team Galactic was here before us"));
        Assert.Equal(1, game.Story.Var("VAR_LAKE_VERITY_PROF_ROWAN_STATE"));
        var rowan = game.Present("prof_rowan")!;
        Assert.Equal((46, 51), (rowan.GridX, rowan.GridY));
        Assert.Contains(TalkFrom(game, "grunt_m", null, Direction.Up).Transcript, l => l.Text.Contains("old-timer"));
        Assert.Contains(TalkFrom(game, "prof_rowan", null, Direction.Up).Transcript, l => l.Text.Contains("needs your help"));
        // Four grunts along the shore, in pairs, with Platinum's teams
        foreach (int n in new[] { 1, 2, 3, 4 })
            Assert.Equal($"galactic_grunt_lake_verity_{n}", game.Present($"galactic_grunt_{n}")!.TrainerData!.Id);
        // The assistant has lost to Mars; the player beats her from the tile below
        string assistant = PlayerIdentity.DefaultName(look == PlayerLook.Boy ? PlayerLook.Girl : PlayerLook.Boy);
        Assert.Contains(TalkFrom(game, "counterpart", null, Direction.Up).Transcript, l => l.Speaker == assistant && l.Text.Contains(look == PlayerLook.Boy ? "couldn't beat" : "Only just"));
        var mars = game.Present("mars")!;
        Assert.Equal((55, 38), (mars.GridX, mars.GridY));
        var fight = TalkFrom(game, "mars", null, Direction.Up);
        Assert.Contains("battle commander_mars_lake_verity Won", fight.Log);
        Assert.Contains(fight.Transcript, l => l.Speaker == "Commander Mars" && l.Text.Contains("all three"));
        Assert.Contains(fight.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("Lake Acuity"));
        // Team Galactic has left the lake, and Mt. Coronet's northern hall; Lake Acuity waits for the next chapter
        foreach (string key in new[] { "mars", "grunt_m", "galactic_grunt_1", "galactic_grunt_2", "galactic_grunt_3", "galactic_grunt_4" })
            Assert.Null(game.Present(key));
        foreach (string flag in new[] { "FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC", "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M", "FLAG_ALT_MUSIC_LAKE_VERITY" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.Story.Has("FLAG_HIDE_LAKE_ACUITY_JUPITER"));
        Assert.Equal(1, game.Story.Var("VAR_LAKE_ACUITY_STATE"));
        Assert.Equal((53, 39), (rowan.GridX, rowan.GridY));
        Assert.Equal(Direction.Left, fight.PlayerFacing);

        // Coming again, the professor and the assistant stand by the water and send the player north
        game.Arrive("LakeVerityGalactic", 46, 53);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal((50, 37), (rowan.GridX, rowan.GridY));
        var beside = game.Present("counterpart")!;
        Assert.Equal((50, 39), (beside.GridX, beside.GridY));
        Assert.Contains(TalkFrom(game, "prof_rowan", null, Direction.Left).Transcript, l => l.Text.Contains("Route 217"));
        Assert.Contains(TalkFrom(game, "counterpart", null, Direction.Left).Transcript, l => l.Speaker == assistant);
        // The lake's TM lies on its far shore
        Assert.Contains(game.Map.NPCs, n => n.IsItemBall && n.Item == "TM38");
    }

    [Fact]
    public void LosingToMarsLeavesTeamGalacticAtTheLake()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Story.Set("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN");
        game.Arrive("LakeVerityGalactic", 46, 53);
        var lost = TalkFrom(game, "mars", null, Direction.Up);
        Assert.Contains("battle commander_mars_lake_verity Lost", lost.Log);
        Assert.False(game.Story.Has("FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY"));
        // Back again, the professor is still held at the way in, and says nothing new
        game.Arrive("LakeVerityGalactic", 46, 53);
        Assert.Empty(game.Last.Transcript);
        var rowan = game.Present("prof_rowan")!;
        Assert.Equal((46, 51), (rowan.GridX, rowan.GridY));
        Assert.NotNull(game.Present("mars"));
    }

    // ------------------------------------------------------------------ the way north

    /// <summary>Whether the stairs down in Mt. Coronet's northern hall can be walked to from the hall's east, past whoever stands there.</summary>
    private static bool StairsReached(Map hall)
    {
        var seen = new HashSet<(int, int)> { (20, 10) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(hall, x, y, way, new Walker(TravelMode.OnFoot, hall.HeightAt(x, y)));
                if (step.Moves && seen.Add((step.X, step.Y))) queue.Enqueue((step.X, step.Y));
            }
        }
        return seen.Contains((12, 10));
    }

    [Fact]
    public void TeamGalacticsGruntsKeepTheWayNorthUntilMarsIsBeaten()
    {
        var game = NewGame();
        game.Arrive("MtCoronet1FNorthRoom1", 20, 10);
        Assert.Equal("MtCoronetB1F", game.Map.GetWarpAt(11, 10)!.TargetMap);
        Assert.False(StairsReached(game.Map));
        var talk = TalkFrom(game, "grunt_m_1", null, Direction.Left);
        Assert.Contains(talk.Transcript, l => l.Text.Contains("Defog"));
        Assert.Contains(talk.Transcript, l => l.Text.Contains("nobody gets past"));
        Assert.Contains(TalkFrom(game, "grunt_m_2", null, Direction.Left).Transcript, l => l.Text.Contains("fog"));

        // As the scene at Lake Verity leaves it
        Run(game, "mt_coronet_1f_north_room_1", "setflag FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M");
        game.Arrive("MtCoronet1FNorthRoom1", 20, 10);
        Assert.Null(game.Map.GetNpcAt(14, 10));
        Assert.Null(game.Map.GetNpcAt(14, 11));
        Assert.True(StairsReached(game.Map));
    }

    [Fact]
    public void ASaveAlreadyNorthOfTheGruntsFindsThemGoneAsItComesUpTheStairs()
    {
        var game = NewGame();
        // Up the stairs from below, beside the stairs' head: nobody gets there past the grunts in a game of today
        game.Arrive("MtCoronet1FNorthRoom1", 12, 10);
        Assert.True(game.Story.Has(StoryMigration.CoronetGruntsGoneFlag));
        Assert.Null(game.Map.GetNpcAt(14, 10));
        // From the east they stay
        var other = NewGame();
        other.Arrive("MtCoronet1FNorthRoom1", 20, 10);
        Assert.False(other.Story.Has(StoryMigration.CoronetGruntsGoneFlag));
    }

    [Fact]
    public void TheTrainerOnRoute211GivesPsychUpOnce()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 400, 530);
        var trainer = game.Map.NPCs.First(n => n.Key == "ace_trainer_m" && n.ScriptFile == "route_211_east");
        game.Arrive("Sinnoh", trainer.GridX, trainer.GridY + 1);
        var gift = game.Talk("ace_trainer_m", "route_211_east");
        Assert.Contains(gift.Transcript, l => l.Text.Contains("creep up"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM77")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_211_EAST_TM77"));
        var again = game.Talk("ace_trainer_m", "route_211_east");
        Assert.Equal(1, game.Bag.GetQuantity(Item("TM77")));
        Assert.Contains("Psych Up", again.Transcript.Single().Text);
    }

    [Theory]
    [InlineData(302)]
    [InlineData(303)]
    public void MayleneCatchesThePlayerUpOnRoute217OnHerWayToSnowpoint(int x)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 303, 380);
        game.Tile = (x, 376);
        game.Facing = Direction.Up;
        Assert.Same(game.Map.Triggers.First(t => t.Script == "Maylene"), FieldScripts.TriggerAt(game.Map, x, 376, game.Story));
        var met = game.Play(Scripts.Find("Maylene", "route_217")!);
        Assert.Contains(met.Transcript, l => l.Speaker == "Maylene" && l.Text.Contains("Snowpoint City"));
        Assert.Equal(Direction.Up, met.PlayerFacing);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_217_STATE"));
        Assert.Null(game.Present("maylene", "route_217"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_217_MAYLENE"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE"));
        Assert.False(game.Fires("Maylene"));
    }

    [Fact]
    public void Hm08LiesOnRoute217AndTeachesRockClimbWhichWaitsForTheIcicleBadge()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 296, 306);
        var ball = game.Map.GetNpcAt(296, 305)!;
        Assert.True(ball.IsItemBall);
        game.Play(Scripts.Find(FieldScripts.ItemBall)!, ball);
        Assert.Equal(1, game.Bag.GetQuantity(Item("HM08")));
        Assert.True(game.Story.Has("FLAG_OBTAINED_ROUTE_217_HM08"));
        var torterra = game.Party.Members[0];
        if (torterra.Species.Name == "Torterra") Assert.True(MoveTeaching.CanLearn(torterra, Item("HM08")));
        Assert.Equal("Icicle", FieldMoveRules.BadgeFor(FieldMove.RockClimb, "Sinnoh"));
    }

    // ------------------------------------------------------------------ ways the story switches

    [Fact]
    public void AWayTheStorySwitchesLeadsWhereItsFlagsSay()
    {
        var map = new Map(4, 4);
        var before = new Warp { SourceX = 1, SourceY = 1, TargetMap = "Before", Until = "FLAG_TEST_SWITCH" };
        var after = new Warp { SourceX = 1, SourceY = 1, TargetMap = "After", From = "FLAG_TEST_SWITCH", Shut = true };
        map.Warps.Add(before);
        map.Warps.Add(after);
        map.ApplyDoors(_ => false);
        Assert.Same(before, map.GetWarpAt(1, 1));
        map.ApplyDoors(flag => flag == "FLAG_TEST_SWITCH");
        Assert.Same(after, map.GetWarpAt(1, 1));
        Assert.Equal(2, map.WarpsAt(1, 1).Count());
        map.ApplyDoors(_ => false);
        Assert.Same(before, map.GetWarpAt(1, 1));
    }

    [Fact]
    public void TheLakesWaysInAreSwitchedByTheOriginalsFlags()
    {
        var game = NewGame();
        var sinnoh = game.Maps["Sinnoh"];
        // Lake Valor: drained until the story fills it again (plan 02 · S13), then the lake as it was
        foreach (int y in new[] { 760, 761 })
        {
            var ways = sinnoh.WarpsAt(717, y).ToList();
            Assert.Equal(new[] { "LakeValorDrained", "LakeValor" }, ways.Select(w => w.TargetMap));
            Assert.Equal(("FLAG_GALACTIC_LEFT_LAKE_VALOR", (string?)null), (ways[0].Until, ways[0].From));
            Assert.Equal(((string?)null, "FLAG_GALACTIC_LEFT_LAKE_VALOR"), (ways[1].Until, ways[1].From));
        }
        game.Story.Set("FLAG_GALACTIC_LEFT_LAKE_VALOR");
        game.Arrive("Sinnoh", 722, 760);
        Assert.Equal("LakeValor", game.Map.GetWarpAt(717, 760)!.TargetMap);
        // Valor Cavern's way out follows the lake
        game.Arrive("ValorCavern", 14, 28);
        Assert.Equal("LakeValor", game.Map.GetWarpAt(14, 29)!.TargetMap);
        // And the lake as it was has the cavern's mouth on its island
        game.Arrive("LakeValor", 51, 10);
        Assert.Equal("ValorCavern", game.Map.GetWarpAt(32, 32)!.TargetMap);
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
        // The library's table, Riley's corner and the lift past the grunts, the cavern, both lakes' far ends
        Assert.Contains((7, 7), Of("CanalaveLibrary3F"));
        Assert.Contains((35, 3), Of("IronIslandB2FLeft"));
        Assert.Contains((19, 45), Of("IronIslandB2FLeft"));
        Assert.Contains((14, 20), Of("ValorCavern"));
        Assert.Contains((32, 33), Of("LakeValorDrained"));
        Assert.Contains((55, 39), Of("LakeVerityGalactic"));
        // Mt. Coronet's stairs past the grunts, Route 217's HM08 and Maylene's trigger
        Assert.Contains((12, 10), Of("MtCoronet1FNorthRoom1"));
        Assert.Contains((296, 306), Of("Sinnoh"));
        Assert.Contains((302, 376), Of("Sinnoh"));
        // Iron Island's door past Riley, the lake past the cameramen
        Assert.Contains((117, 490), Of("Sinnoh"));
        Assert.Contains((718, 760), Of("Sinnoh"));
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKnowsWhatItHasDone()
    {
        string[] always = { "FLAG_HIDE_CANALAVE_CITY_COUNTERPART", "FLAG_HIDE_CANALAVE_CITY_PROF_ROWAN", "FLAG_HIDE_CANALAVE_CITY_SAILOR",
            "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_LAKE_ACUITY_LOW_WATER_RIVAL", "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_ROUTE_217_MAYLENE",
            "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN" };
        string[] untilByron = { "FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY", "FLAG_HIDE_CANALAVE_LIBRARY_RIVAL", "FLAG_HIDE_CANALAVE_LIBRARY_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN" };

        var before = new StoryState();
        StoryMigration.Upgrade(before, 11, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always.Concat(untilByron)) Assert.True(before.Has(flag), flag);
        Assert.False(before.Has(StoryMigration.CoronetGruntsGoneFlag));
        Assert.Equal(0, before.Var("VAR_ROUTE_217_STATE"));

        // Byron beaten already: the library's people are as his Gym's script left them
        var badge = new StoryState();
        badge.GiveBadge("Sinnoh", "Mine");
        badge.SetVar("VAR_CANALAVE_CITY_STATE", 2);
        StoryMigration.Upgrade(badge, 11, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(badge.Has(flag), flag);
        foreach (string flag in untilByron) Assert.False(badge.Has(flag), flag);

        // Been to Snowpoint City already: nobody stood in Mt. Coronet's hall then, and Maylene has long gone by
        foreach (bool byBadge in new[] { false, true })
        {
            var north = new StoryState();
            if (byBadge) north.GiveBadge("Sinnoh", "Icicle");
            else north.Set(StoryMigration.ArrivedInSnowpointFlag);
            StoryMigration.Upgrade(north, 11, Array.Empty<Pokemon>(), Scripts, new Inventory());
            Assert.True(north.Has(StoryMigration.CoronetGruntsGoneFlag));
            Assert.Equal(1, north.Var("VAR_ROUTE_217_STATE"));
        }
        Assert.True(StoryState.CurrentVersion >= 12);
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { "FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN", "FLAG_HIDE_CANALAVE_CITY_SAILOR", "FLAG_HIDE_ROUTE_217_MAYLENE" })
        {
            Assert.True(fresh.Has(flag), flag);
            Assert.True(upgraded.Has(flag), flag);
        }
    }
}
