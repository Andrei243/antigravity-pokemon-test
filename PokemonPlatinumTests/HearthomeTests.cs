using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The fourth chapter (plan 02 · S7, "Hearthome and Solaceon") played through with no screen, in the order a player
/// meets it: the assistant's Vs. Seeker and Dowsing Machine on Route 207, Mira in Wayward Cave, Cyrus in Mt. Coronet,
/// the Odd Keystone on Route 208, Keira and her Buneary, the fisherman's walk to the Contest Hall, Mom, Keira and
/// Fantina in its lobby, the Gym's guide stepping aside, the Relic Badge (given as the Gym's own script gives it: the
/// Gym is plan 01 · M9's), the rival at the gate to Route 209, the Good Rod, the rival in Solaceon Town, the hiker who
/// borrows HM05 in the ruins, and the Psyduck that still block Route 210 at the chapter's end. Each scene is started
/// as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class HearthomeTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(int starter = 0, BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(starter, fight);
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        var grotle = new Pokemon(PokemonDatabase.Get("Grotle")!, 28);
        game.Party.Add(grotle);
        return game;
    }

    /// <summary>Plays a few lines of the script language in a place, as a scene of the game would.</summary>
    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    /// <summary>
    /// Plays a script with the player facing a way and answering as told, on the game's map, as the game would when
    /// the player talks to someone from that side.
    /// </summary>
    private static HeadlessScriptHost Facing(OpeningTests.Game game, string script, string place, NPC? subject, Direction facing,
        TimeOfDay time = TimeOfDay.Day, params int[] answers)
    {
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
        {
            Map = game.Map, PlayerTile = game.Tile, PlayerFacing = facing, RivalName = OpeningTests.Game.Rival,
            Fight = _ => game.Fight, NeedsPokemon = true, TimeOfDay = time
        };
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, place)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        game.Tile = host.PlayerTile;
        game.Map.ApplyPresence(game.Story.Has);
        return host;
    }

    /// <summary>Fantina beaten in her Gym, by the Gym's own script (plan 01 · M9's room).</summary>
    private static void WinTheRelicBadge(OpeningTests.Game game)
    {
        game.Arrive("HearthomeGymLeaderRoom", 4, 11);
        game.Talk("fantina");
        Assert.True(game.Story.HasBadge(Badge.Relic));
    }

    [Fact]
    public void ANewGameFindsTheChapterAsItBegins()
    {
        var game = NewGame();
        // The assistant comes with her scene; Cyrus waits in the passage and Mira in the dark from the start
        game.Arrive("Sinnoh", 339, 713);
        Assert.Null(game.Present("counterpart", "route_207"));
        Assert.True(game.Fires("Assistant"));
        game.Arrive("MtCoronet1FSouth", 14, 24);
        Assert.NotNull(game.Present("cyrus"));
        game.Arrive("WaywardCave1F", 72, 14);
        Assert.NotNull(game.Present("mira"));

        // Hearthome City: Keira and her Buneary come with their scene, Fantina's guide is at her door, the gate to Route
        // 209 is crowded and the rival isn't at it yet
        game.Arrive("Sinnoh", 465, 726);
        Assert.Null(game.Present("keira", "hearthome_city"));
        Assert.Null(game.Present("buneary", "hearthome_city"));
        Assert.NotNull(game.Present("gym_guide", "hearthome_city"));
        Assert.NotNull(game.Present("pokefan_m_2", "hearthome_city"));
        Assert.NotNull(game.Present("hiker_2", "hearthome_city"));
        Assert.Null(game.Present("rival", "hearthome_city"));
        Assert.True(game.Fires("Keira"));
        Assert.False(game.Fires("Rival", "hearthome_city"));
        // The Contest Hall's door leads into its lobby, where Mom, Keira and Fantina are
        Assert.Equal("ContestHallLobby", game.Map.GetWarpAt(479, 691)!.TargetMap);
        game.Arrive("ContestHallLobby", 16, 13);
        Assert.Null(game.Present("mom"));
        Assert.NotNull(game.Present("fantina"));

        // Solaceon's rival, Route 210's Cynthia and the Day Care's guide come later; the Psyduck stand in the way
        game.Arrive("Sinnoh", 560, 668);
        Assert.Null(game.Present("rival", "solaceon_town"));
        Assert.True(game.Fires("Rival", "solaceon_town"));
        game.Arrive("Sinnoh", 560, 589);
        foreach (string psyduck in new[] { "psyduck_1", "psyduck_2", "psyduck_3", "psyduck_4" })
            Assert.NotNull(game.Present(psyduck, "route_210_south"));
        Assert.Null(game.Present("cynthia", "route_210_south"));
        Assert.True(game.Story.Has("FLAG_HIDE_DAY_CARE_GYM_GUIDE"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheChapterPlaysThroughFromRoute207ToTheRoadNorthOfSolaceon(int starter)
    {
        var game = NewGame(starter);

        // Route 207: the assistant catches the player up with the Vs. Seeker, from either hand, and the Dowsing Machine
        game.Arrive("Sinnoh", 339, 713);
        var assistant = game.Step("Assistant");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Seeker")!));
        Assert.True(game.Story.Has(VsSeeker.LevelFlag(1)));
        Assert.Contains(PoketchApp.DowsingMachine, assistant.Poketch.Apps);
        Assert.Contains("music common/assistant", assistant.Log);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_207_COUNTERPART_TRIGGER_STATE"));
        Assert.Null(game.Present("counterpart", "route_207"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_207_COUNTERPART"));
        Assert.False(game.Fires("Assistant"));

        // Mt. Coronet: Cyrus walks down to the player, speaks of the world's beginning and goes on west
        game.Arrive("MtCoronet1FSouth", 13, 23);
        var cyrus = game.Step("Cyrus");
        Assert.Contains(cyrus.Transcript, l => l.Speaker == "Cyrus" && l.Text.Contains("Sinnoh's history began"));
        Assert.Equal(1, game.Story.Var("VAR_MT_CORONET_1F_SOUTH_STATE"));
        Assert.Null(game.Present("cyrus"));
        Assert.True(game.Story.Has("FLAG_HIDE_MT_CORONET_1F_SOUTH_CYRUS"));
        Assert.Equal((14, 24), game.Tile);

        // Route 208: the black belt's odd stone
        game.Arrive("Sinnoh", 437, 734);
        game.Talk("black_belt", "route_208");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Odd Keystone")!));
        Assert.Contains("Route 209", game.Talk("black_belt", "route_208").Transcript.Single().Text);

        // Hearthome City: Buneary and Keira run up as the player comes in
        game.Arrive("Sinnoh", 460, 727);
        var keira = game.Step("Keira");
        Assert.Contains("cry Buneary", keira.Log);
        Assert.Contains(keira.Transcript, l => l.Speaker == "Keira");
        Assert.Equal(1, game.Story.Var("VAR_HEARTHOME_CITY_STATE"));
        Assert.Null(game.Present("keira", "hearthome_city"));
        Assert.Null(game.Present("buneary", "hearthome_city"));
        Assert.False(game.Fires("Keira"));

        // Fantina is out: her guide stands at the Gym's door and says so
        Assert.Contains("Contest Hall", game.Talk("gym_guide", "hearthome_city").Transcript.Last().Text);

        // The Contest Hall: Keira and Mom, who gives the player something to wear on stage, and both leave
        game.Arrive("Sinnoh", 479, 692);
        game.Through("ContestHallLobby");
        Assert.Contains(game.Said, l => l.Speaker == "Mom" && l.Text.Contains("on that stage"));
        Assert.Contains(game.Said, l => l.Speaker == "Keira" && l.Text.Contains("YOUR mom"));
        Assert.True(game.Story.Has("FLAG_CONTEST_HALL_VISITED"));
        Assert.Equal(1, game.Story.Var("VAR_CONTEST_HALL_LOBBY_STATE"));
        Assert.Null(game.Present("mom"));
        Assert.Null(game.Present("keira"));
        Assert.Equal((16, 11), game.Tile);
        // Only once
        game.Through("Sinnoh");
        game.Through("ContestHallLobby");
        Assert.DoesNotContain(game.Said, l => l.Speaker == "Mom");

        // Fantina twirls, says she'll wait at her Gym, and goes; her guide leaves its door
        game.Arrive("ContestHallLobby", 22, 10);
        var fantina = Facing(game, "Fantina", "ContestHallLobby", game.Map.FindPerson("fantina"), Direction.Up);
        Assert.Contains(fantina.Transcript, l => l.Speaker == "Fantina" && l.Text.Contains("wait for you"));
        Assert.Null(game.Present("fantina"));
        Assert.True(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));
        game.Arrive("Sinnoh", 499, 700);
        Assert.Null(game.Present("gym_guide", "hearthome_city"));

        // The Relic Badge clears the way to Route 209 and brings the rival to its gate
        WinTheRelicBadge(game);
        game.Arrive("Sinnoh", 500, 726);
        Assert.Null(game.Present("pokefan_m_2", "hearthome_city"));
        Assert.Null(game.Present("hiker_2", "hearthome_city"));
        Assert.NotNull(game.Present("rival", "hearthome_city"));
        Assert.True(game.Fires("Rival", "hearthome_city"));
        var rival = game.Step("Rival", "hearthome_city");
        string team = "rival_route_209_" + StoryState.Starters[starter].ToLowerInvariant();
        Assert.Contains($"battle {team} Won", rival.Log);
        Assert.Contains(rival.Transcript, l => l.Speaker == OpeningTests.Game.Rival);
        Assert.Equal(2, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        Assert.Null(game.Present("rival", "hearthome_city"));
        Assert.False(game.Fires("Rival", "hearthome_city"));

        // Route 209: the Good Rod
        game.Arrive("Sinnoh", 533, 727);
        var rod = Facing(game, "Fisherman", "route_209", game.Map.FindPerson("fisherman", "route_209"), Direction.Left);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Good Rod")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_GOOD_ROD"));
        Assert.Equal(2, rod.Asked.Count);

        // Solaceon Town: the rival dashes down to tell of the ruins, and off again
        game.Arrive("Sinnoh", 560, 668);
        var solaceon = game.Step("Rival", "solaceon_town");
        Assert.Contains(solaceon.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Solaceon Ruins"));
        Assert.Equal(1, game.Story.Var("VAR_SOLACEON_TOWN_STATE"));
        Assert.Null(game.Present("rival", "solaceon_town"));
        Assert.False(game.Fires("Rival", "solaceon_town"));

        // The ruins: HM05 in the deepest room, and the hiker who borrows it for a Green Shard
        game.Arrive("SolaceonRuinsRoom7", 6, 8);
        game.Talk("item_hm05");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM05")!));
        game.Arrive("SolaceonRuinsRoom2", 5, 6);
        var hiker = Facing(game, "Hiker", "solaceon_ruins_room_2", game.Map.FindPerson("hiker"), Direction.Left);
        Assert.Single(hiker.Asked);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Green Shard")!));

        // Route 210: the Psyduck still stand in the way north, and say why
        game.Arrive("Sinnoh", 560, 588);
        var psyduck = game.Talk("psyduck_2", "route_210_south");
        Assert.Contains("won't move", psyduck.Transcript.Single().Text);
        Assert.Empty(psyduck.Asked);
        Assert.NotNull(game.Present("psyduck_2", "route_210_south"));
    }

    [Theory]
    [InlineData(712)]
    [InlineData(713)]
    [InlineData(714)]
    public void TheAssistantCatchesThePlayerUpOnEachTileOfHerTrigger(int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 340, y);
        // Walking her along the road and back is checked for walls by the host
        var host = game.Play(Scripts.Find("Assistant", "route_207")!);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Seeker")!));
        Assert.Contains(host.Asked, a => a.Question.Contains("Which one"));
        Assert.Equal((340, y), game.Tile);
    }

    [Fact]
    public void EternasBugCatcherGoesBackToTheWindOnceTheAssistantHasBeenMet()
    {
        var game = NewGame();
        game.Story.Set("FLAG_RECEIVED_BICYCLE");
        game.Arrive("Sinnoh", 298, 532);
        Assert.Contains("Bicycle", game.Talk("bug_catcher_2", "eterna_city").Transcript[0].Text);
        game.Story.SetVar("VAR_ROUTE_207_COUNTERPART_TRIGGER_STATE", 1);
        Assert.Contains("wind", game.Talk("bug_catcher_2", "eterna_city").Transcript[0].Text);
    }

    [Theory]
    [InlineData(Direction.Down, 41, 52, 42, 53)]
    [InlineData(Direction.Left, 42, 53, 41, 52)]
    public void MiraIsTakenToTheWayOutAndGoesOutAhead(Direction facing, int fromX, int fromY, int endX, int endY)
    {
        var game = NewGame();
        game.Arrive("WaywardCave1F", 72, 14);
        var mira = game.Talk("mira");
        Assert.Contains("partner Mira mira_wayward_cave", mira.Log);
        Assert.Equal(1, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_WAYWARD_CAVE_1F_MIRA"));

        // Spoken to on the way, three things and the last again
        foreach (string said in new[] { "helpful moves", "muddled", "strong one day", "strong one day" })
            Assert.Contains(said, game.Talk("mira").Transcript.Single().Text);

        // The way out under Route 206: on its own tile the trigger goes before the warp
        game.Tile = (fromX, fromY);
        Assert.Equal("Exit", FieldScripts.TriggerAt(game.Map, 41, 53, game.Story)!.Script);
        game.Tile = (41, 53);
        var exit = Facing(game, "Exit", "wayward_cave_1f", null, facing);
        Assert.Contains("partner off", exit.Log);
        Assert.Contains(exit.Transcript, l => l.Speaker == "Mira" && l.Text.Contains("Daylight"));
        Assert.Equal((endX, endY), game.Tile);
        Assert.Null(game.Present("mira"));
        Assert.True(game.Story.Has("FLAG_TRAVELED_WITH_MIRA"));
        Assert.Equal(2, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 41, 53, game.Story));
    }

    [Fact]
    public void MiraIsLostAgainWhereSheWasIfLeftBehind()
    {
        var game = NewGame();
        game.Arrive("WaywardCave1F", 72, 14);
        game.Talk("mira");
        var mira = game.Map.FindPerson("mira")!;
        (mira.GridX, mira.GridY) = (60, 30);
        // Coming back with nobody along, she is where she was first found, and asks again by the player's name
        game.Arrive("WaywardCave1F", 72, 14);
        Assert.Equal(0, game.Story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE"));
        Assert.Equal((72, 13), (mira.GridX, mira.GridY));
        Assert.Contains(game.Talk("mira").Transcript, l => l.Text.Contains("remembered your name"));
    }

    [Theory]
    [InlineData(725)]
    [InlineData(726)]
    [InlineData(727)]
    [InlineData(728)]
    [InlineData(729)]
    public void KeiraRunsUpOnEachTileOfHerTrigger(int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 461, y);
        game.Play(Scripts.Find("Keira", "hearthome_city")!);
        Assert.Equal(1, game.Story.Var("VAR_HEARTHOME_CITY_STATE"));
        Assert.Equal((461, y), game.Tile);
        Assert.Null(game.Present("keira", "hearthome_city"));
    }

    [Theory]
    [InlineData(Direction.Up, 487, 715)]
    [InlineData(Direction.Down, 487, 713)]
    [InlineData(Direction.Left, 488, 714)]
    [InlineData(Direction.Right, 486, 714)]
    public void TheFishermanWalksThePlayerToTheContestHallFromEachSide(Direction facing, int x, int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", x, y);
        var fisherman = game.Map.FindPerson("fisherman", "hearthome_city")!;
        var walk = Facing(game, "Fisherman", "hearthome_city", fisherman, facing);
        Assert.Contains(walk.Transcript, l => l.Text.Contains("Contest Hall"));
        Assert.Equal((479, 698), game.Tile);
        Assert.Equal((479, 697), (fisherman.GridX, fisherman.GridY));
        // Once is enough while the player stays in the city
        Assert.Empty(Facing(game, "Fisherman", "hearthome_city", fisherman, Direction.Up).Asked);
        // A no, and he apologises
        game.Story.ClearLocal();
        Assert.Contains(Facing(game, "Fisherman", "hearthome_city", fisherman, Direction.Up, answers: 1).Transcript, l => l.Text.Contains("beg your pardon"));
    }

    [Theory]
    [InlineData(724)]
    [InlineData(725)]
    [InlineData(726)]
    [InlineData(727)]
    [InlineData(728)]
    [InlineData(729)]
    public void TheRivalBattlesAtTheGateOnEachTileOfItsTrigger(int y)
    {
        var game = NewGame();
        WinTheRelicBadge(game);
        game.Arrive("Sinnoh", 503, y);
        Assert.Equal("Rival", FieldScripts.TriggerAt(game.Map, 503, y, game.Story)!.Script);
        game.Play(Scripts.Find("Rival", "hearthome_city")!);
        Assert.Equal(2, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL"));
        Assert.Equal((503, y), game.Tile);
    }

    [Fact]
    public void LosingToTheRivalLeavesHimAtTheGate()
    {
        var game = NewGame();
        WinTheRelicBadge(game);
        game.Fight = BattleOutcome.Lost;
        game.Arrive("Sinnoh", 503, 726);
        game.Step("Rival", "hearthome_city");
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        Assert.NotNull(game.Present("rival", "hearthome_city"));
        Assert.True(game.Fires("Rival", "hearthome_city"));
    }

    [Theory]
    [InlineData(557)]
    [InlineData(558)]
    [InlineData(559)]
    [InlineData(560)]
    [InlineData(561)]
    [InlineData(562)]
    [InlineData(563)]
    public void SolaceonsRivalRunsDownToEachTileOfHisTrigger(int x)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", x, 669);
        game.Play(Scripts.Find("Rival", "solaceon_town")!);
        Assert.Equal(1, game.Story.Var("VAR_SOLACEON_TOWN_STATE"));
        Assert.Equal((x, 669), game.Tile);
    }

    [Fact]
    public void TheJoggersBattleOnlyInTheMorning()
    {
        var game = NewGame();
        foreach (var (time, battles) in new[] { (TimeOfDay.Morning, true), (TimeOfDay.Day, false), (TimeOfDay.Night, false), (TimeOfDay.Morning, true) })
        {
            game.Arrive("Sinnoh", 530, 722);
            Facing(game, "OnEnter", "route_209", null, Direction.Down, time);
            Assert.Equal(battles, game.Present("jogger_richard", "route_209") != null);
            Assert.Equal(!battles, game.Present("jogger_richard_no_battle", "route_209") != null);
            Assert.Equal(battles, game.Present("jogger_raul", "route_209") != null);
            game.Arrive("Sinnoh", 565, 620);
            Facing(game, "OnEnter", "route_210_south", null, Direction.Down, time);
            Assert.Equal(battles, game.Present("jogger_wyatt", "route_210_south") != null);
            Assert.Equal(!battles, game.Present("jogger_wyatt_no_battle", "route_210_south") != null);
        }
    }

    [Fact]
    public void TheRuinsHikerWaitsForHm05AndAsksAgainIfRefused()
    {
        var game = NewGame();
        game.Arrive("SolaceonRuinsRoom2", 5, 6);
        var hiker = game.Map.FindPerson("hiker")!;
        Assert.Empty(Facing(game, "Hiker", "solaceon_ruins_room_2", hiker, Direction.Left).Asked);
        game.Bag.AddItem(ItemDatabase.Get("HM05")!);
        Facing(game, "Hiker", "solaceon_ruins_room_2", hiker, Direction.Left, answers: 1);
        Assert.True(game.Story.Has("FLAG_DID_NOT_LOAN_HM_DEFOG"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Green Shard")!));
        Facing(game, "Hiker", "solaceon_ruins_room_2", hiker, Direction.Left);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Green Shard")!));
        Assert.Contains("Now I've got Defog", Facing(game, "Hiker", "solaceon_ruins_room_2", hiker, Direction.Left).Transcript.Single().Text);
    }

    [Fact]
    public void TheRuinsInscriptionsAreReadFromTheirStones()
    {
        var game = NewGame();
        foreach (var (map, place) in new[] { ("SolaceonRuinsRoom1", "solaceon_ruins_room_1"), ("SolaceonRuinsRoom7", "solaceon_ruins_room_7") })
        {
            game.Arrive(map, 5, 2);
            foreach (int x in new[] { 4, 5, 6 }) Assert.Equal("Inscription", game.Map.TileScripts[(x, 1)]);
            var read = game.Play(Scripts.Find("Inscription", place)!);
            Assert.Contains(read.Transcript, l => l.Text.Contains("Unown"));
        }
    }

    [Fact]
    public void ThePsyduckLeaveOnceTheSecretPotionCuresThem()
    {
        var game = NewGame();
        game.Bag.AddItem(ItemDatabase.Get("Secret Potion")!);
        game.Arrive("Sinnoh", 561, 588);
        var potion = Facing(game, "Psyduck", "route_210_south", game.Map.FindPerson("psyduck_1", "route_210_south"), Direction.Up);
        Assert.Contains(potion.Asked, a => a.Question.Contains("Secret Potion"));
        foreach (string psyduck in new[] { "psyduck_1", "psyduck_2", "psyduck_3", "psyduck_4" })
            Assert.Null(game.Present(psyduck, "route_210_south"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
        Assert.Contains(potion.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Celestic Town"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Old Charm")!));
        Assert.Null(game.Present("cynthia", "route_210_south"));
        Assert.True(game.Story.Has("FLAG_USED_SECRETPOTION"));
    }

    [Fact]
    public void Route210sTrainerGivesTm51Once()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 566, 613);
        game.Talk("ace_trainer_f", "route_210_south");
        game.Talk("ace_trainer_f", "route_210_south");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM51")!));
    }

    [Fact]
    public void TheRuinManiacGivesHisAppForFiftyKindsSeen()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 558, 660);
        var maniac = game.Map.FindPerson("ruin_maniac", "solaceon_town");
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map };
        host.Poketch.Enabled = true;
        foreach (var (seen, has) in new[] { (49, false), (50, true) })
        {
            host.DexSeen = seen;
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find("RuinManiac", "solaceon_town")!, maniac);
            runner.RunToEnd();
            Assert.Equal(has, host.Poketch.Apps.Contains(PoketchApp.PokemonHistory));
        }
    }

    [Fact]
    public void AmitySquareLetsInOnlyTrainersWithAPokemonItAllows()
    {
        var game = new OpeningTests.Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 20));
        game.Arrive("AmitySquare", 12, 47);

        // Nothing it allows: the receptionist lists the kinds and the player steps back out
        var refused = game.Step("WestGate");
        Assert.Contains(refused.Transcript, l => l.Text.Contains("Pachirisu"));
        Assert.Equal((12, 47), game.Tile);
        Assert.Equal(0, game.Story.Var("VAR_FOLLOWER_MON_ACTIVE"));

        // With a Pikachu: asked, healed and let in, and seen out again
        var pikachu = new Pokemon(PokemonDatabase.Get("Pikachu")!, 20);
        pikachu.CurrentHP = 1;
        game.Party.Add(pikachu);
        var allowed = game.Step("WestGate");
        Assert.Single(allowed.Asked);
        Assert.Equal(pikachu.MaxHP, pikachu.CurrentHP);
        Assert.Equal((12, 45), game.Tile);
        Assert.Equal(1, game.Story.Var("VAR_FOLLOWER_MON_ACTIVE"));
        Assert.False(game.Fires("WestGate"));
        var left = game.Step("Leave");
        Assert.Contains(left.Transcript, l => l.Text.Contains("stroll"));
        Assert.Equal(0, game.Story.Var("VAR_FOLLOWER_MON_ACTIVE"));

        // The east gate asks the same
        game.Arrive("AmitySquare", 51, 47);
        game.Step("EastGate");
        Assert.Equal((51, 45), game.Tile);
    }

    [Fact]
    public void ASaveFromBeforeTheChapterKeepsItsPeopleWhereTheStoryHasThem()
    {
        // A save of version 6, before the chapter: the chapter's people are taken off the map until their scenes
        var before = new StoryState();
        StoryMigration.BeginNewGame(before, Scripts);
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_207_COUNTERPART", "FLAG_HIDE_HEARTHOME_CITY_KEIRA", "FLAG_HIDE_SOLACEON_TOWN_RIVAL",
                     "FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL", "FLAG_HIDE_ROUTE_210_SOUTH_CYNTHIA" })
            before.Unset(flag);
        StoryMigration.Upgrade(before, 6, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(before.Has("FLAG_HIDE_ROUTE_207_COUNTERPART"));
        Assert.True(before.Has("FLAG_HIDE_HEARTHOME_CITY_KEIRA"));
        Assert.True(before.Has("FLAG_HIDE_SOLACEON_TOWN_RIVAL"));
        Assert.True(before.Has("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL"));
        Assert.False(before.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));

        // One that had beaten Fantina already: her Badge called the rival to the gate, and the guide and Fantina are gone
        var past = new StoryState();
        StoryMigration.BeginNewGame(past, Scripts);
        past.Unset("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL");
        past.GiveBadge(Badge.Relic);
        past.SetVar("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE", 1);
        StoryMigration.Upgrade(past, 6, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(past.Has("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL"));
        Assert.True(past.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE"));
        Assert.True(past.Has("FLAG_HIDE_CONTEST_HALL_LOBBY_FANTINA"));

        // A save of today is left as it is
        var today = new StoryState();
        StoryMigration.BeginNewGame(today, Scripts);
        today.Unset("FLAG_HIDE_SOLACEON_TOWN_RIVAL");
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(today.Has("FLAG_HIDE_SOLACEON_TOWN_RIVAL"));
    }

    [Fact]
    public void TheWayOnFromHearthomeIsWalkedToSolaceonAndTheContestHall()
    {
        var game = new OpeningTests.Game();
        // From the way in from Route 208, with every gate the story opens opened (WorldWalk passes whoever a script
        // takes away: the Gym's guide, the crowd at the gate, the Psyduck)
        var reached = WorldWalk.From(game.MapNamed, new MapSpot("Sinnoh", 461, 727));
        var overworld = game.MapNamed("Sinnoh");
        Assert.Contains((566, 660), reached[overworld]);
        Assert.Contains((560, 585), reached[overworld]);
        Assert.Contains(reached.Keys, m => m.Name == "ContestHallLobby");
        Assert.Contains(reached.Keys, m => m.Name == "HearthomeGym");
        Assert.Contains(reached.Keys, m => m.Name == "SolaceonRuinsRoom7");
    }
}
