using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The eleventh chapter (plan 02 · S14, "The Pokémon League") played through with no screen, in the order a player
/// meets it: the sea of Route 223 and Victory Road walked with the five field moves they ask for, the rival's battle
/// before the League's door and what it does to the Sunyshore Gym's statues, the Hall of Fame's scene for a first
/// victory and a later one with everything a victory sets, the save and the credits, Mom's news at home, and the
/// people the Hall of Fame brings on. The League's rooms and their battles are <see cref="LeagueTests"/>'. Each scene
/// is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>). The scene of the team's
/// entry and the credits are screens of their own, played here by their clocks and their one button.
/// </summary>
public class VictoryRoadAndTheHallOfFameTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Center = "PokemonLeagueNorthPokemonCenter", Hall = "PokemonLeagueHallOfFame";
    private const string RivalVar = "VAR_RIVAL_BEAT_SUNYSHORE_GYM", RivalFlag = "FLAG_HIDE_POKEMON_LEAGUE_NORTH_POKECENTER_1F_RIVAL";
    private const string Postgame = "VAR_PLAYER_HOUSE_POSTGAME_STATE", Completed = "FLAG_GAME_COMPLETED";

    private static OpeningTests.Game Challenger(int starter = 0, BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(starter, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 58));
        game.Story.SetBadges(0xFF);
        // Home is as the first chapter leaves it (common.OpeningDone): Mom has long since said her first piece
        game.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 6);
        return game;
    }

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    /// <summary>A step onto a tile of a trigger, which must start its script now.</summary>
    private static HeadlessScriptHost StepOnto(OpeningTests.Game game, int x, int y, string script)
    {
        game.Tile = (x, y);
        var trigger = FieldScripts.TriggerAt(game.Map, x, y, game.Story);
        Assert.NotNull(trigger);
        Assert.Equal(script, trigger!.Script);
        return game.Play(Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name));
    }

    /// <summary>Into the Hall of Fame as the hallway's scene brings the Champion in: the room's scene, and then home.</summary>
    private static HeadlessScriptHost EnterTheHall(OpeningTests.Game game)
    {
        int before = game.Played.Count;
        game.Facing = Direction.Up;
        game.Arrive(Hall, 8, 15);
        Assert.Equal($"{Hall}.OnEnter", game.Played[before].Script);
        return game.Played[before].Host;
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameKeepsTheRivalFromTheLeagueAndTheFanFromTheDayCare()
    {
        var game = new OpeningTests.Game();
        Assert.True(game.Story.Has(RivalFlag));
        Assert.True(game.Story.Has("FLAG_HIDE_DAY_CARE_GYM_GUIDE"));
        Assert.Equal(0, game.Story.Var(RivalVar));
        Assert.Equal(0, game.Story.Var(Postgame));
        Assert.True(StoryState.CurrentVersion >= 17);

        // The rival's own tile is the way out itself (the original's exit mat is its warp), and his trigger the row
        // before the guard
        var center = game.Maps[Center];
        var rival = center.Everyone.Single(n => n.Key == "rival");
        Assert.Equal((11, 12, Direction.Up, RivalFlag), (rival.GridX, rival.GridY, rival.Facing, rival.HiddenBy));
        var trigger = Assert.Single(center.Triggers);
        Assert.Equal((10, 4, 3, 1, "RivalBeforeTheDoor", RivalVar, 0), (trigger.X, trigger.Y, trigger.Width, trigger.Depth, trigger.Script, trigger.Variable, trigger.Value));
        game.Arrive(Center, 11, 11);
        Assert.Null(game.Present("rival"));
        Assert.NotNull(game.Present("door_guard"));
    }

    // ------------------------------------------------------------------ Route 223 and Victory Road

    /// <summary>
    /// Every map come to from a tile by the field's own rules, by someone with these field moves, who breaks cracked
    /// rocks or not and pushes boulders or not (a boulder pushed is a boulder out of the way, as the whole game's walk
    /// has it). On the map of Sinnoh the walk stays between Sunyshore City's beach and the League.
    /// </summary>
    private static HashSet<string> MapsReached(IReadOnlyDictionary<string, Map> maps, MapSpot from, FieldMoves moves, bool smash, bool strength)
    {
        var seen = new HashSet<(Map, int, int, TravelMode, int)>();
        var names = new HashSet<string>();
        var queue = new Queue<(Map Map, int X, int Y, TravelMode Mode, float Height, bool Arrived)>();
        void Visit(Map map, int x, int y, TravelMode mode, float height, bool arrived)
        {
            if (map.Name == "Sinnoh" && (x < 820 || x > 880 || y < 540 || y > 760)) return;
            if (!seen.Add((map, x, y, mode, (int)MathF.Round(height * 2f)))) return;
            names.Add(map.Name);
            queue.Enqueue((map, x, y, mode, height, arrived));
        }
        var first = maps[from.Map];
        Visit(first, from.X, from.Y, TravelMode.OnFoot, first.HeightAt(from.X, from.Y), true);
        while (queue.Count > 0)
        {
            var (map, x, y, mode, height, arrived) = queue.Dequeue();
            if (!arrived && map.WarpsAt(x, y).Any())
            {
                foreach (var warp in map.WarpsAt(x, y))
                {
                    var onto = maps[warp.TargetMap];
                    Visit(onto, warp.TargetX, warp.TargetY, TravelMode.OnFoot, onto.HeightAt(warp.TargetX, warp.TargetY), true);
                }
                continue;
            }
            var walker = new Walker(mode, height, Moves: moves, Climbing: true);
            foreach (var way in new[] { Direction.Up, Direction.Right, Direction.Left, Direction.Down })
            {
                var step = FieldMovement.Step(map, x, y, way, walker);
                if (step.Moves)
                {
                    Visit(map, step.X, step.Y, step.Mode, step.Height, false);
                    continue;
                }
                var (dx, dy) = FieldMovement.Delta(way);
                int nx = x + dx, ny = y + dy;
                // (The field's rules leave it to the water's own script to ask for Surf)
                if (step.Obstacle == Obstacle.Water && moves.HasFlag(FieldMoves.Surf) && FieldMovement.CanStartSurf(map, x, y, way, walker))
                    Visit(map, nx, ny, TravelMode.Surfing, map.SurfaceAt(nx, ny, height).Height, false);
                else if (step.Obstacle == Obstacle.Person && mode == TravelMode.OnFoot && map.GetNpcAt(nx, ny) is { IsThing: true } thing
                         && (thing.NpcType == NPC.CrackedRockType ? smash : thing.NpcType != NPC.BoulderType || strength)
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(map, nx, ny, TravelMode.OnFoot, map.HeightAt(nx, ny), false);
            }
        }
        return names;
    }

    [Fact]
    public void TheWayToTheLeagueIsWalkedWithItsFiveFieldMovesAndWithNoFewer()
    {
        var maps = new OpeningTests.Game().Maps;
        // From the sand of Sunyshore City's beach, where Jasmine gives the last of them
        var beach = new MapSpot("Sinnoh", 855, 746, Direction.Up);
        const FieldMoves all = FieldMoves.Surf | FieldMoves.Waterfall | FieldMoves.RockClimb;

        var reached = MapsReached(maps, beach, all, smash: true, strength: true);
        // Up Route 223 and its waterfall, through all three floors of Victory Road, to both of the League's Pokémon Centers
        Assert.Superset(new HashSet<string> { "VictoryRoad1F", "VictoryRoad2F", "VictoryRoadB1F", "PokemonLeagueSouthPokemonCenter", Center }, reached);

        // Without the sea nobody leaves the beach, and without the waterfall nobody comes up out of the sea
        Assert.DoesNotContain("VictoryRoad1F", MapsReached(maps, beach, all & ~FieldMoves.Surf, true, true));
        Assert.DoesNotContain("VictoryRoad1F", MapsReached(maps, beach, all & ~FieldMoves.Waterfall, true, true));
        // Inside, the road asks for each of the other three before it lets anyone out at the League's end
        Assert.DoesNotContain(Center, MapsReached(maps, beach, all & ~FieldMoves.RockClimb, true, true));
        Assert.DoesNotContain(Center, MapsReached(maps, beach, all, smash: false, strength: true));
        Assert.DoesNotContain(Center, MapsReached(maps, beach, all, smash: true, strength: false));
    }

    [Fact]
    public void VictoryRoadsCollectorKeepsTheBackRoomsUntilTheHallOfFame()
    {
        var game = Challenger();
        game.Arrive("VictoryRoad1F", 15, 77);
        // VictoryRoad_OnTransition
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_VICTORY_ROAD"));
        var collector = game.Present("collector");
        Assert.NotNull(collector);
        Assert.Contains(game.Talk("collector").Transcript, l => l.Text.Contains("Pokémon League"));

        // A Champion without the National Pokédex is still kept out (plan 02 · S15, part a), and hears why
        game.Story.Set(Completed);
        Assert.Contains(game.Talk("collector").Transcript, l => l.Text.Contains("professor"));
        game.Arrive("VictoryRoad1F", 15, 77);
        Assert.NotNull(game.Present("collector"));
        game.NationalDex = true;
        game.Arrive("VictoryRoad1F", 15, 77);
        Assert.Null(game.Present("collector"));
    }

    [Fact]
    public void TheLeaguesStatuesAreReadFromBeforeThem()
    {
        var sinnoh = new OpeningTests.Game().Maps["Sinnoh"];
        // The original's eight tile events, two to a statue
        foreach (var (x, y) in new[] { (842, 577), (843, 577), (851, 577), (852, 577), (837, 561), (838, 561), (856, 561), (857, 561) })
        {
            Assert.True(sinnoh.TileScripts.TryGetValue((x, y), out string? script), $"nothing is read at {x},{y}");
            Assert.Equal("Statue", script);
        }
        var host = new HeadlessScriptHost { Map = sinnoh, PlayerTile = (842, 578), PlayerFacing = Direction.Up };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Find("Statue", "pokemon_league"));
        runner.RunToEnd();
        Assert.Contains(host.Transcript, l => l.Speaker == null && l.Text.Contains("POKéMON LEAGUE"));
    }

    // ------------------------------------------------------------------ the rival before the door

    [Theory]
    [InlineData(10, 0)]
    [InlineData(11, 1)]
    [InlineData(12, 2)]
    public void TheRivalRunsInAndBattlesWhoeverFirstWalksUpToTheGuard(int x, int starter)
    {
        var game = Challenger(starter);
        game.Arrive(Center, 11, 11);
        var rival = game.Maps[Center].Everyone.Single(n => n.Key == "rival");

        game.Facing = Direction.Up;
        var scene = StepOnto(game, x, 4, "RivalBeforeTheDoor");
        Assert.Contains("music common/rival", scene.Log);
        // He battles with the team that goes with the player's starter
        Assert.Contains($"battle rival_pokemon_league_{StoryState.Starters[starter].ToLowerInvariant()} Won", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Elite Four"));
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Champion"));
        Assert.Equal(Direction.Down, scene.PlayerFacing);
        Assert.Empty(scene.Problems);

        // He is gone by the way he came, and the scene is over for good
        Assert.Equal(1, game.Story.Var(RivalVar));
        Assert.True(game.Story.Has(RivalFlag));
        Assert.Equal((11, 12), (rival.GridX, rival.GridY));
        game.Arrive(Center, 11, 11);
        Assert.Null(game.Present("rival"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, x, 4, game.Story));
        // The guard is still to be passed
        Assert.False(game.Story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY"));
    }

    [Fact]
    public void TheRivalStandsBelowThePlayerAsHeSpeaks()
    {
        foreach (int x in new[] { 10, 11, 12 })
        {
            var game = Challenger();
            game.Arrive(Center, 11, 11);
            var rival = game.Maps[Center].Everyone.Single(n => n.Key == "rival");
            game.Tile = (x, 4);
            var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
            {
                Map = game.Map, PlayerTile = game.Tile, PlayerFacing = Direction.Up, Pauses = true, RivalName = OpeningTests.Game.Rival, NeedsPokemon = true
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Find("RivalBeforeTheDoor", Center));
            // Read on until his first line is on the screen
            for (int i = 0; i < 200 && host.Transcript.Count == 0; i++)
            {
                host.Busy = false;
                host.Walking = false;
                runner.Update(0.1f);
            }
            Assert.Equal((x, 5), (rival.GridX, rival.GridY));
            Assert.Empty(host.Problems);
        }
    }

    [Fact]
    public void ABattleLostToTheRivalLeavesHimToComeRunningAgain()
    {
        var game = Challenger(fight: BattleOutcome.Lost);
        game.Arrive(Center, 11, 11);
        var lost = StepOnto(game, 11, 4, "RivalBeforeTheDoor");
        Assert.Contains("battle rival_pokemon_league_turtwig Lost", lost.Log);
        Assert.Equal(0, game.Story.Var(RivalVar));
        // The player wakes at this room's own nurse, and nobody stands in the doorway
        game.Arrive(Center, 4, 2);
        Assert.Null(game.Present("rival"));
        Assert.True(game.Story.Has(RivalFlag));
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 11, 4, game.Story));
    }

    [Fact]
    public void TheGuardCountsTheEightBadgesOffTownByTown()
    {
        var game = Challenger();
        game.Arrive(Center, 11, 11);
        game.Facing = Direction.Up;
        var said = game.Talk("door_guard").Transcript;
        foreach (string badge in new[] { "Coal", "Forest", "Relic", "Cobble", "Fen", "Mine", "Icicle", "Beacon" })
            Assert.Contains(said, l => l.Text.Contains($"{badge} Badge"));
        Assert.True(game.Story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY"));
    }

    [Fact]
    public void TheSunyshoreGymsStatuesNameTheRivalOnceHeHasBattledAtTheLeague()
    {
        var game = Challenger();
        string Statue()
        {
            var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { RivalName = OpeningTests.Game.Rival, PlayerName = "Ash" };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Find("GymStatue", "SunyshoreGym"));
            runner.RunToEnd();
            return host.Transcript.Single().Text;
        }
        // The player's Badge came first, and the statue says so
        Assert.EndsWith("Certified Trainers: Ash", Statue());
        game.Arrive(Center, 11, 11);
        StepOnto(game, 11, 4, "RivalBeforeTheDoor");
        Assert.EndsWith($"Certified Trainers: Ash, {OpeningTests.Game.Rival}", Statue());
        game.Story.SetBadges(0x7F);
        Assert.EndsWith("Leader: Volkner", Statue());
    }

    // ------------------------------------------------------------------ the Hall of Fame

    [Fact]
    public void AFirstVictoryIsRecordedSavedAtHomeAndFollowedByTheCredits()
    {
        var game = Challenger();
        // As the tenth chapter leaves the story, with a Mesprit that was knocked out and a Giratina that wasn't caught
        game.Story.SetVar("VAR_ROAMING_MESPRIT_STATE", 2);
        foreach (string flag in new[] { "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA",
            "FLAG_ALT_MUSIC_CHAMPION_ROOM", "FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART" })
            game.Story.Set(flag);
        Assert.False(game.Story.Has("FLAG_HIDE_SENDOFF_SPRING_CYNTHIA"));

        var hall = EnterTheHall(game);
        Assert.Empty(hall.Problems);
        // The professor and Cynthia speak of how long it has been, and she welcomes the player to the machine
        var said = hall.Transcript;
        Assert.Contains(said, l => l.Speaker == "Professor Rowan" && l.Text.Contains("long time"));
        Assert.Contains(said, l => l.Speaker == "Cynthia" && l.Text.Contains("became Champion"));
        Assert.DoesNotContain(said, l => l.Text.Contains("straighter"));
        Assert.Contains(said, l => l.Speaker == "Cynthia" && l.Text.Contains("Welcome to the Hall of Fame"));
        Assert.Contains(said, l => l.Speaker == "Cynthia" && l.Text.Contains("This machine"));

        // In the original's order: a ball for each of the team, the chime, the dark, the record, the scene of the
        // team's entry, home, the save, and the credits
        var log = hall.Log;
        Assert.Equal(game.Party.Count, log.Count(l => l == "sound ball_click"));
        int At(string line) => log.IndexOf(line) is >= 0 and var i ? i : throw new Xunit.Sdk.XunitException($"the scene never did '{line}': {string.Join("; ", log)}");
        int[] order = { At("sound ball_click"), At("fanfare FanfareHeal"), At("fade out"), At("halloffame"), At("open Ceremony"), At("warp PlayerHouse2F 4 4"), At("save"), At("sound save"), At("open Credits") };
        Assert.Equal(order.OrderBy(i => i), order);
        Assert.DoesNotContain("open HallOfFame", log);
        Assert.Contains(said, l => l.Speaker == null && l.Text.Contains("saved the game"));

        // The record: the team in its order, every one with Sinnoh's Champion Ribbon, and healed
        Assert.Equal(1, hall.HallOfFame.Total);
        Assert.Equal(new[] { "Garchomp", "Staraptor" }, hall.HallOfFame.Entries.Single().Team.Select(m => m.Species));
        Assert.All(game.Party.Members, p => Assert.Contains(RibbonRules.SinnohChamp, p.Ribbons));
        Assert.Contains("fanfare FanfareHeal", log);

        // The save is written in the player's own room with the victory already in it, so that is where the game
        // comes back to whenever it is next loaded
        var save = Assert.Single(hall.Saves);
        var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
        Assert.Equal((start.Map, (start.X, start.Y)), (save.Map, save.Tile));
        Assert.Contains(Completed, save.Story.Flags);
        Assert.Contains("FLAG_UNLOCKED_VS_SEEKER_LVL_4", save.Story.Flags);
        Assert.Equal(1, save.Story.Variables[Postgame]);
        Assert.Equal(("PlayerHouse2F", (start.X, start.Y)), (game.Map.Name, game.Tile));

        // What a victory sets (SetHallOfFameVictoryFlagsAndVars), under the original's names
        var story = game.Story;
        Assert.True(story.Has(Completed) && story.Has(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag));
        Assert.True(story.Has("FLAG_UNLOCKED_VS_SEEKER_LVL_4"));
        Assert.True(story.Has("FLAG_HIDE_SENDOFF_SPRING_CYNTHIA"));
        foreach (string cleared in new[] { "FLAG_HIDE_DAY_CARE_GYM_GUIDE", "FLAG_ALT_MUSIC_CHAMPION_ROOM", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE",
            "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART", "FLAG_HIDE_VEILSTONE_STORE_B1F_PROF_ROWAN",
            "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN", "FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA" })
            Assert.False(story.Has(cleared), cleared);
        Assert.Equal(3, story.Var("VAR_ROAMING_MESPRIT_STATE"));
        Assert.Equal(1, story.Var(Postgame));
        Assert.Equal(0, story.Var(GameEngine.MachineBallsVariable));
        // Without the National Pokédex the Spring Path is not shown by this room, and Regigigas is left as it is
        Assert.Equal(0, story.Var("VAR_HIDDEN_LOCATION_SPRING_PATH"));
    }

    [Fact]
    public void ALaterVictoryHasOtherWordsAndLeavesMomsNewsTold()
    {
        var game = Challenger(look: PlayerLook.Girl);
        EnterTheHall(game);
        // Mom's news is told downstairs, and a second victory doesn't bring it back
        game.Through("PlayerHouse");
        Assert.Equal(2, game.Story.Var(Postgame));

        var again = EnterTheHall(game);
        Assert.Contains(again.Transcript, l => l.Speaker == "Professor Rowan" && l.Text.Contains("straighter"));
        Assert.Contains(again.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("So does everyone"));
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("long time"));
        Assert.Contains("open Ceremony", again.Log);
        Assert.Contains("open Credits", again.Log);
        Assert.Equal(2, game.Story.Var(Postgame));
        Assert.Equal("PlayerHouse2F", game.Map.Name);
    }

    [Fact]
    public void ASaveThatFailsIsSaidAndTheCreditsStillRoll()
    {
        var game = Challenger();
        game.SaveFails = true;
        var hall = EnterTheHall(game);
        Assert.Contains("save failed", hall.Log);
        Assert.DoesNotContain("sound save", hall.Log);
        Assert.Contains(hall.Transcript, l => l.Text.Contains("could not be saved"));
        Assert.Contains("open Credits", hall.Log);
    }

    /// <summary>Plays the script of what a victory sets by itself, as the room's scene calls it.</summary>
    private static StoryState Victory(Action<StoryState> before, bool nationalDex = false)
    {
        var host = new HeadlessScriptHost { ShowsNothing = true, NationalDex = nationalDex };
        before(host.Story);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(ScriptLibrary.HallOfFameVictory)!);
        runner.RunToEnd();
        return host.Story;
    }

    [Fact]
    public void AVictoryPutsTheLegendaryPokemonBackWhereTheyWereNotCaught()
    {
        string[] hidden = { "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA",
            "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN", "FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS", "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA" };
        void Hide(StoryState s) { foreach (string flag in hidden) s.Set(flag); }

        // Knocked out or run from, with nothing else done: Azelf, Uxie and Giratina are back; Heatran waits for Buck
        // and Regigigas for the National Pokédex; a roamer that isn't beaten is left alone
        var plain = Victory(Hide);
        Assert.False(plain.Has(hidden[0]) || plain.Has(hidden[1]) || plain.Has(hidden[2]));
        Assert.True(plain.Has(hidden[3]) && plain.Has(hidden[4]) && plain.Has(hidden[5]) && plain.Has(hidden[6]));
        Assert.Equal(0, plain.Var("VAR_STARK_MOUNTAIN_ROOM_3_STATE"));

        // Caught, each stays gone
        var caught = Victory(s =>
        {
            Hide(s);
            foreach (string who in new[] { "AZELF", "UXIE", "GIRATINA", "HEATRAN", "REGIGIGAS", "DIALGA", "PALKIA" }) s.Set($"FLAG_CAUGHT_{who}");
            s.Set("FLAG_TALKED_TO_BATTLEGROUND_BUCK");
            s.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 2);
            s.SetVar("VAR_SPEAR_PILLAR_PALKIA_STATE", 2);
        }, nationalDex: true);
        Assert.All(hidden.Take(5), flag => Assert.True(caught.Has(flag), flag));
        Assert.Equal((2, 2), (caught.Var("VAR_SPEAR_PILLAR_DIALGA_STATE"), caught.Var("VAR_SPEAR_PILLAR_PALKIA_STATE")));

        // The gates of the post-game: Buck spoken to, the National Pokédex, and Dialga and Palkia met and not caught
        var later = Victory(s =>
        {
            Hide(s);
            s.Set("FLAG_TALKED_TO_BATTLEGROUND_BUCK");
            s.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 2);
            s.SetVar("VAR_SPEAR_PILLAR_PALKIA_STATE", 2);
        }, nationalDex: true);
        Assert.False(later.Has(hidden[3]) || later.Has(hidden[4]));
        Assert.Equal(1, later.Var("VAR_STARK_MOUNTAIN_ROOM_3_STATE"));
        Assert.Equal((0, 0), (later.Var("VAR_SPEAR_PILLAR_DIALGA_STATE"), later.Var("VAR_SPEAR_PILLAR_PALKIA_STATE")));

        // The roamers: one that was knocked out (2) can be set loose again (3), Mesprit and Cresselia back where they are met
        var roamers = Victory(s =>
        {
            Hide(s);
            foreach (string who in new[] { "MESPRIT", "CRESSELIA", "MOLTRES", "ZAPDOS", "ARTICUNO" }) s.SetVar($"VAR_ROAMING_{who}_STATE", 2);
        });
        foreach (string who in new[] { "Mesprit", "Cresselia", "Moltres", "Zapdos", "Articuno" }) Assert.Equal(Roamers.Reset, roamers.Var(Roamers.StateVariable(who)));
        Assert.False(roamers.Has(hidden[5]) || roamers.Has(hidden[6]));
        // One that is roaming, or caught, is left as it is
        var roaming = Victory(s => { s.SetVar("VAR_ROAMING_MESPRIT_STATE", Roamers.Roaming); s.SetVar("VAR_ROAMING_CRESSELIA_STATE", Roamers.Captured); });
        Assert.Equal((Roamers.Roaming, Roamers.Captured), (roaming.Var("VAR_ROAMING_MESPRIT_STATE"), roaming.Var("VAR_ROAMING_CRESSELIA_STATE")));
    }

    [Fact]
    public void AVictoryBringsOnTheProfessorOnlyUntilHeHasBeenSpokenTo()
    {
        const string rowan = "FLAG_HIDE_VEILSTONE_STORE_B1F_PROF_ROWAN", spoken = "FLAG_TALKED_TO_VEILSTONE_STORE_B1F_PROF_ROWAN";
        Assert.False(Victory(s => s.Set(rowan)).Has(rowan));
        Assert.True(Victory(s => { s.Set(rowan); s.Set(spoken); }).Has(rowan));
        // And the National Pokédex shows the Spring Path, as the way out of the Distortion World does
        Assert.Equal(0, Victory(_ => { }).Var("VAR_HIDDEN_LOCATION_SPRING_PATH"));
        var game = Challenger();
        game.NationalDex = true;
        EnterTheHall(game);
        Assert.Equal(786, game.Story.Var("VAR_HIDDEN_LOCATION_SPRING_PATH"));
        // Mom's state moves on only from nought
        Assert.Equal(2, Victory(s => s.SetVar(Postgame, 2)).Var(Postgame));
    }

    [Fact]
    public void TheFlagsAskedAheadOfTheirScenesAreTheOnesTheVictoryAsks()
    {
        var asked = Find("Victory", Hall).Everything().Where(i => i.Condition is { Query: Query.Flag }).Select(i => i.Condition!.Name).ToHashSet();
        // (but for the one Looker asks at the Game Corner, which is no flag of a victory's: plan 02 · S15, part a)
        Assert.Subset(asked, StoryTests.AskedAheadOfTheirScenes.Except(new[] { "FLAG_ARRESTED_CHARON_STARK_MOUNTAIN" }).ToHashSet());
        // The script of a victory may be run with no screen (the migration runs it): it sets things and nothing else
        Assert.DoesNotContain(Find("Victory", Hall).Everything(), i => i.Op is not (Op.SetFlag or Op.ClearFlag or Op.SetVar or Op.If or Op.Goto or Op.Return));
    }

    // ------------------------------------------------------------------ home, and who the victory brings on

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MomHasNewsTheFirstTimeTheChampionComesDownstairs(bool nationalDex)
    {
        var game = Challenger();
        game.NationalDex = nationalDex;
        EnterTheHall(game);
        Assert.Equal("PlayerHouse2F", game.Map.Name);
        // Nothing waits upstairs
        Assert.Equal("PlayerHouse2F.OnEnter", game.Played[^1].Script);
        Assert.Empty(game.Said);

        game.Through("PlayerHouse");
        var said = game.Said;
        Assert.Contains(said, l => l.Speaker == "Mom" && l.Text.Contains(OpeningTests.Game.Rival));
        Assert.Contains(said, l => l.Speaker == "Mom" && l.Text.Contains("ship at Snowpoint City"));
        // Until the National Pokédex is theirs she asks after the professor's Pokédex
        Assert.Equal(!nationalDex, said.Any(l => l.Text.Contains("Pokédex")));
        Assert.Equal(nationalDex, game.Story.Has("FLAG_TALKED_TO_MOM_ABOUT_RIVAL_SNOWPOINT_CITY"));
        Assert.Equal(!nationalDex, game.Story.Has("FLAG_TALKED_TO_MOM_ABOUT_NATIONAL_DEX_PROGRESS"));
        Assert.Equal(2, game.Story.Var(Postgame));
        Assert.Empty(game.Last.Problems);

        // It is said once: coming in again she is as she ever was, and spoken to she rests the team
        game.Through("Sinnoh");
        game.Through("PlayerHouse");
        Assert.Empty(game.Said);
        Assert.Contains("fanfare FanfareHeal", game.Talk("mom").Log);
    }

    [Fact]
    public void TheAssistantStandsInSandgemTownAgainWithWordsForAChampion()
    {
        var game = Challenger();
        // As the first chapter's tour leaves her
        game.Story.Set("FLAG_HIDE_SANDGEM_TOWN_COUNTERPART");
        game.Arrive("Sinnoh", 170, 846);
        Assert.Null(game.Present("sandgem_counterpart", "sandgem_town"));

        EnterTheHall(game);
        game.Arrive("Sinnoh", 170, 846);
        var her = game.Present("sandgem_counterpart", "sandgem_town");
        Assert.NotNull(her);
        Assert.Equal((168, 845), (her!.GridX, her.GridY));
        // Her first word is the elder of Celestic Town's message, as the original's is (plan 02 · S15, part c wrote the
        // scene it leads to); her congratulations come once the elder is heard and Dialga or Palkia met at Spear Pillar
        Assert.Contains(game.Talk("sandgem_counterpart", "sandgem_town").Transcript, l => l.Text.Contains("elder of Celestic Town"));
        game.Story.Set("FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR");
        game.Story.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 1);
        Assert.Contains(game.Talk("sandgem_counterpart", "sandgem_town").Transcript, l => l.Text.Contains("Congratulations, Champion"));

        // With every Pokémon of Sinnoh seen she wants the Pokédex shown to the professor, which is where the post-game begins
        game.Seen = Pokedex.Entries(PokedexMode.Sinnoh).Count;
        Assert.Equal(210, game.Seen);
        Assert.Contains(game.Talk("sandgem_counterpart", "sandgem_town").Transcript, l => l.Text.Contains("Professor Rowan"));
        game.NationalDex = true;
        Assert.Contains(game.Talk("sandgem_counterpart", "sandgem_town").Transcript, l => l.Text.Contains("National Pokédex"));
    }

    [Fact]
    public void TheDayCaresFanComesWithAPokemonLeftThereOrWithTheHallOfFame()
    {
        var game = Challenger();
        game.Arrive("PokemonDayCare", 9, 11);
        Assert.Null(game.Present("gym_guide"));
        // A Pokémon left with the couple brings him in at once (the Lady's script), and again whenever the room is come to
        game.Story.Set("FLAG_STORED_POKEMON_AT_DAY_CARE");
        game.Arrive("PokemonDayCare", 9, 11);
        Assert.False(game.Story.Has("FLAG_HIDE_DAY_CARE_GYM_GUIDE"));
        Assert.NotNull(game.Present("gym_guide"));
        Assert.Contains(Scripts.Find("common.DayCareLady")!.Everything(), i => i.Op == Op.ClearFlag && i.Name == "FLAG_HIDE_DAY_CARE_GYM_GUIDE");

        // And a Champion finds him there whether or not they ever left one
        var champion = Challenger();
        EnterTheHall(champion);
        champion.Arrive("PokemonDayCare", 9, 11);
        Assert.False(champion.Story.Has("FLAG_STORED_POKEMON_AT_DAY_CARE"));
        Assert.NotNull(champion.Present("gym_guide"));
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterIsBroughtUpToDate()
    {
        // One that has never been near the League: the rival and the fan are hidden until their time
        var early = new StoryState();
        StoryMigration.Upgrade(early, 16, Array.Empty<Pokemon>(), Scripts);
        Assert.True(early.Has(RivalFlag) && early.Has("FLAG_HIDE_DAY_CARE_GYM_GUIDE"));
        Assert.Equal((0, 0), (early.Var(RivalVar), early.Var(Postgame)));
        Assert.False(early.Has("FLAG_UNLOCKED_VS_SEEKER_LVL_4"));

        // One that has left a Pokémon at the Day Care keeps the couple's fan
        var breeder = new StoryState();
        breeder.Set("FLAG_STORED_POKEMON_AT_DAY_CARE");
        StoryMigration.Upgrade(breeder, 16, Array.Empty<Pokemon>(), Scripts);
        Assert.False(breeder.Has("FLAG_HIDE_DAY_CARE_GYM_GUIDE"));

        // One that entered the Hall of Fame when its scene ended in a stand-in is given what the scene sets now
        var champion = new StoryState();
        foreach (string flag in new[] { Completed, "SinnohHallOfFame", "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN",
            "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_CAUGHT_UXIE", "FLAG_HIDE_ACUITY_CAVERN_UXIE" })
            champion.Set(flag);
        StoryMigration.Upgrade(champion, 16, Array.Empty<Pokemon>(), Scripts);
        Assert.True(champion.Has(RivalFlag));
        Assert.Equal(1, champion.Var(RivalVar));
        Assert.Equal(1, champion.Var(Postgame));
        Assert.True(champion.Has("FLAG_UNLOCKED_VS_SEEKER_LVL_4"));
        foreach (string cleared in new[] { "FLAG_HIDE_DAY_CARE_GYM_GUIDE", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN", "FLAG_HIDE_VALOR_CAVERN_AZELF" })
            Assert.False(champion.Has(cleared), cleared);
        // A guardian that was caught stays caught
        Assert.True(champion.Has("FLAG_HIDE_ACUITY_CAVERN_UXIE") && champion.Has("FLAG_HIDE_SENDOFF_SPRING_CYNTHIA"));

        // A save of today is left as it is
        var today = new StoryState();
        today.Set(Completed);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts);
        Assert.Equal(0, today.Var(Postgame));
        Assert.False(today.Has(RivalFlag));
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { RivalFlag, "FLAG_HIDE_DAY_CARE_GYM_GUIDE" })
            Assert.True(fresh.Has(flag) && upgraded.Has(flag), flag);
        // The chapter's script is a link of the chain a new game runs (the time after the Hall of Fame is the next,
        // plan 02 · S15), and may only set things up
        Assert.Contains(Find("ChapterTen", ScriptLibrary.Common).Everything(), i => i.Op == Op.Call && i.Name == "ChapterEleven");
        Assert.All(Find("ChapterEleven", ScriptLibrary.Common).Everything(), i => Assert.Contains(i.Op, new[] { Op.SetFlag, Op.ClearFlag, Op.SetVar, Op.If, Op.Call, Op.Return }));
        Assert.Equal(ScriptLibrary.ChapterEleven, Find("ChapterEleven", ScriptLibrary.Common).FullName);
    }

    // ------------------------------------------------------------------ the scene of the team's entry

    private static HallOfFameCeremony Ceremony(out List<Pokemon> team, bool withEgg = true)
    {
        var garchomp = new Pokemon(PokemonDatabase.Get("Garchomp")!, 62) { Nickname = "Fang" };
        garchomp.Met("Wayward Cave", new DateTime(2026, 5, 1));
        var togekiss = new Pokemon(PokemonDatabase.Get("Togekiss")!, 55) { OriginalTrainer = new TrainerMark("Cynthia", 777, PlayerLook.Girl) };
        togekiss.Met("Eterna City", new DateTime(2026, 5, 2));
        togekiss.MetLevel = 0;
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 9);
        team = new List<Pokemon> { garchomp, togekiss, bidoof };
        var all = new List<Pokemon>(team);
        if (withEgg) all.Insert(1, Breeding.GiftEgg(PokemonDatabase.Get("Riolu")!, 12345, new Random(3), Ruleset.Platinum));
        var scene = new HallOfFameCeremony();
        scene.Begin(all, "Ash", 4321, 3 * 3600 + 25 * 60, "PLAYER");
        return scene;
    }

    private static void Pass(HallOfFameCeremony scene, float seconds)
    {
        for (float t = 0; t < seconds; t += 1f / 30f) scene.Advance(1f / 30f);
    }

    [Fact]
    public void TheCeremonyShowsEachPokemonInTurnAndThenTheChampionAmongThem()
    {
        var scene = Ceremony(out var team);
        // The Egg is left out, as the record leaves it out
        Assert.Equal(team, scene.Team);
        Assert.True(scene.IsActive);

        for (int member = 0; member < team.Count; member++)
        {
            Assert.Equal((CeremonyPhase.Enter, member), (scene.Phase, scene.Member));
            // It is still on its way in (the first from the very edge)
            Assert.True(scene.Appearance().Slide < 1f);
            if (member == 0) Assert.Equal(0f, scene.Appearance().Slide);
            Assert.Equal(0, scene.PartsShown);
            Pass(scene, HallOfFameCeremony.EnterTime);
            Assert.Equal(CeremonyPhase.Show, scene.Phase);
            Assert.Equal(1f, scene.Appearance().Slide);
            // The card fills in part by part
            Assert.Equal(1, scene.PartsShown);
            Pass(scene, HallOfFameCeremony.PartStep);
            Assert.Equal(2, scene.PartsShown);
            Pass(scene, HallOfFameCeremony.PartStep);
            Assert.Equal(HallOfFameCeremony.CardParts, scene.PartsShown);
            // It goes on by itself, and comes off the way it came
            Pass(scene, HallOfFameCeremony.ShowTime - 2 * HallOfFameCeremony.PartStep);
            Assert.Equal(CeremonyPhase.Leave, scene.Phase);
            Pass(scene, HallOfFameCeremony.LeaveTime / 3f);
            Assert.InRange(scene.Appearance().Slide, 0.05f, 0.95f);
            Pass(scene, HallOfFameCeremony.LeaveTime * 2f / 3f + 0.05f);
        }

        // The Champion steps up, the title comes, the team comes in from both sides one after another, and then paper falls
        Assert.Equal(CeremonyPhase.Champion, scene.Phase);
        Assert.True(scene.Appearance().Confetti < 0f);
        Pass(scene, HallOfFameCeremony.TeamAt + HallOfFameCeremony.TeamStep);
        Assert.Equal(1f, scene.Appearance().Figure);
        Assert.True(scene.TeamSlide(0) > scene.TeamSlide(1) && scene.TeamSlide(1) > scene.TeamSlide(2));
        scene.PressConfirm();
        Assert.Equal(CeremonyPhase.Champion, scene.Phase);
        Pass(scene, HallOfFameCeremony.ArrowAt);
        var look = scene.Appearance();
        Assert.True(look is { Title: 1f, Arrow: true, Black: 0f } && look.Confetti > 0f);
        Assert.All(Enumerable.Range(0, team.Count), i => Assert.Equal(1f, scene.TeamSlide(i)));
        // It waits as long as it is left
        Pass(scene, 20f);
        Assert.Equal(CeremonyPhase.Champion, scene.Phase);

        scene.PressConfirm();
        Assert.Equal(CeremonyPhase.Out, scene.Phase);
        Pass(scene, HallOfFameCeremony.OutTime / 2f);
        Assert.InRange(scene.Appearance().Black, 0.3f, 0.7f);
        Pass(scene, HallOfFameCeremony.OutTime);
        Assert.False(scene.IsActive);
    }

    [Fact]
    public void TheButtonMovesAWholeCardOnAndNotAHalfShownOne()
    {
        var scene = Ceremony(out _);
        scene.PressConfirm();
        Pass(scene, HallOfFameCeremony.EnterTime + 0.1f);
        scene.PressConfirm();
        Assert.Equal(CeremonyPhase.Show, scene.Phase);
        Pass(scene, 2 * HallOfFameCeremony.PartStep + 0.35f);
        scene.PressConfirm();
        Assert.Equal(CeremonyPhase.Leave, scene.Phase);
    }

    [Fact]
    public void ACardSaysWhoThePokemonIsWhoseItWasAndWhereItCameFrom()
    {
        var scene = Ceremony(out var team);
        var own = scene.CardOf(team[0]);
        Assert.Equal(("Fang", "Garchomp", 62, "Ash", "04321"), (own.Name, own.Species, own.Level, own.Trainer, own.TrainerId));
        Assert.Equal(new[] { PokemonType.Dragon, PokemonType.Ground }, own.Types);
        Assert.Equal("Met at Wayward Cave, at Lv. 62.", own.Met);
        // One that came from someone else keeps their name and number; one that hatched says so
        var given = scene.CardOf(team[1]);
        Assert.Equal(("Cynthia", "00777", "Hatched at Eterna City."), (given.Trainer, given.TrainerId, given.Met));
        Assert.Equal("Met along the way.", scene.CardOf(team[2]).Met);
        Assert.Equal(("Ash", "04321", "3:25"), (scene.Champion, scene.ChampionId, scene.TimePlayed));
    }

    [Fact]
    public void TheTeamStandsEitherSideOfTheChampionTheFirstOnTheRight()
    {
        Assert.Equal(new[] { 1, -1, 2, -2, 3, -3 }, Enumerable.Range(0, 6).Select(HallOfFameCeremony.SideOf));
        // With nobody to show (a tool's run) the scene is the Champion alone
        var alone = new HallOfFameCeremony();
        alone.Begin(Array.Empty<Pokemon>(), "Ash", 1, 0f, "PLAYER");
        Assert.Equal(CeremonyPhase.Champion, alone.Phase);
    }

    // ------------------------------------------------------------------ the credits

    [Fact]
    public void TheCreditsNameTheProjectsSourcesAsItsOwnRecordsHaveThem()
    {
        string all = string.Join("\n", CreditsDatabase.Sections.SelectMany(s => s.Entries.SelectMany(e => new[] { s.Heading, e.Name, e.Note ?? "" })));
        // Every source docs/art/CREDITS.md and THIRD-PARTY-NOTICES.md list, with whose it is and its licence
        foreach (string named in new[] { "Nunito", "Vernon Adams", "SIL Open Font License 1.1", "PokéAPI", "Paul Hallett", "BSD 3-Clause", "pret/pokeplatinum",
            "Pokémon Showdown", "Guangcong Luo", "MIT", "raylib", "GAME FREAK", "Nintendo", "not affiliated", "trademarks of Nintendo" })
            Assert.Contains(named, all);
        string repo = Repository.Root;
        string records = File.ReadAllText(Path.Combine(repo, "docs", "art", "CREDITS.md")) + File.ReadAllText(Path.Combine(repo, "THIRD-PARTY-NOTICES.md"));
        foreach (string named in new[] { "Nunito", "Vernon Adams", "Paul Hallett", "pret/pokeplatinum", "Guangcong Luo" }) Assert.Contains(named, records);

        // The interface's font is Latin-1, and the roll is written without a long dash
        Assert.DoesNotContain(all + CreditsDatabase.End + CreditsDatabase.Farewell, c => c > 0xFF);
        Assert.Single(CreditsDatabase.Sections, s => s.Team);
        Assert.Equal("THE END", CreditsDatabase.End);
    }

    [Fact]
    public void TheRollIsOneColumnWithTheChampionsTeamListedInIt()
    {
        var team = new[] { ("Fang", "Garchomp · Lv. 62"), ("Togekiss", "Lv. 55") };
        var (lines, height) = CreditsRoll.Lay(CreditsDatabase.Sections, "Ash", team);
        // Every line stands below the one before it, a heading opens each section, and the roll is as tall as its last line
        Assert.Equal(lines.Select(l => l.Y).OrderBy(y => y), lines.Select(l => l.Y));
        Assert.Equal(lines.Select(l => l.Y).Distinct().Count(), lines.Count);
        Assert.Equal(CreditsDatabase.Sections.Count, lines.Count(l => l.Kind == CreditLineKind.Heading));
        Assert.Equal(CreditLineKind.Heading, lines[0].Kind);
        Assert.InRange(height, lines[^1].Y + CreditsRoll.NotePitch, lines[^1].Y + 3 * CreditsRoll.NamePitch);
        // The Champion by name, and the team after them
        int champion = lines.FindIndex(l => l.Kind == CreditLineKind.Name && l.Text == "Ash");
        Assert.True(champion > 0);
        Assert.DoesNotContain(lines, l => l.Text.Contains("{player}"));
        Assert.Equal(new[] { "Fang", "Garchomp · Lv. 62", "Togekiss", "Lv. 55" }, lines.Skip(champion + 2).Take(4).Select(l => l.Text));
        // A note is wrapped to the column, and no line of one begins or ends with the dot that parts two things on a line
        Assert.All(lines.Where(l => l.Kind == CreditLineKind.Note), l => Assert.True(l.Text.Length <= CreditsRoll.RoughColumn, l.Text));
        var dotted = CreditsRoll.Lay(new[] { new CreditSection { Heading = "x", Entries = { new CreditEntry { Name = "n", Note = "one · two" } } } }, "Ash",
            Array.Empty<(string, string)>(), _ => new[] { "one ·", "· two" }).Lines;
        Assert.Equal(new[] { "X", "n", "one", "two" }, dotted.Select(l => l.Text));
        Assert.Equal(("Fang", "Garchomp · Lv. 62"), CreditsRoll.Listing(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62) { Nickname = "Fang" }));
        // One that goes by its species' name is listed with its level alone
        Assert.Equal(("Togekiss", "Lv. 55"), CreditsRoll.Listing(new Pokemon(PokemonDatabase.Get("Togekiss")!, 55)));
    }

    private static void Pass(CreditsScreen screen, float seconds)
    {
        for (float t = 0; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
    }

    [Fact]
    public void TheCreditsRunFromTheirTitleCardThroughTheRollToTheLastCard()
    {
        Ceremony(out var team, withEgg: false);
        var credits = new CreditsScreen();
        credits.Begin("Ash", team);
        Assert.Equal(CreditsPhase.Title, credits.Phase);
        Assert.Equal(team, credits.Team);

        // The title card fades in, holds and fades out
        Assert.Equal(0f, credits.Appearance().Title);
        Pass(credits, CreditsScreen.TitleIn + 0.5f);
        Assert.Equal(1f, credits.Appearance().Title);
        Pass(credits, CreditsScreen.TitleTime - CreditsScreen.TitleIn - 0.5f + 0.1f);
        Assert.Equal(CreditsPhase.Roll, credits.Phase);

        // The roll climbs at its pace, the button does nothing to it, and the team stands beside it one after another
        float start = credits.Scroll;
        Pass(credits, 10f);
        Assert.InRange(credits.Scroll - start, 10f * CreditsScreen.Pace - 8f, 10f * CreditsScreen.Pace + 8f);
        credits.PressConfirm();
        Assert.Equal(CreditsPhase.Roll, credits.Phase);
        var seen = new List<int>();
        for (int guard = 0; guard < 100_000 && credits.Phase == CreditsPhase.Roll; guard++)
        {
            int member = credits.Appearance().Member;
            if (seen.Count == 0 || seen[^1] != member) seen.Add(member);
            credits.Advance(1f / 30f);
        }
        Assert.Equal(new[] { 0, 1, 2 }, seen);
        Assert.Equal(credits.Length, credits.Scroll);
        // By then the last line has faded at the top of the picture
        Assert.True(CreditsScreen.ViewHeight - credits.Scroll + credits.Lines[^1].Y < CreditsScreen.FadeBand);

        // The last card waits for the button once it has had a moment, and then the picture closes
        Assert.Equal(CreditsPhase.End, credits.Phase);
        credits.PressConfirm();
        Assert.Equal(CreditsPhase.End, credits.Phase);
        Pass(credits, CreditsScreen.EndReady + 0.1f);
        Assert.True(credits.Appearance() is { End: 1f, Arrow: true });
        credits.PressConfirm();
        Assert.Equal(CreditsPhase.Out, credits.Phase);
        Pass(credits, CreditsScreen.OutTime + 0.1f);
        Assert.False(credits.IsActive);
    }

    [Fact]
    public void TheButtonHeldHurriesTheRollAndTheLastCardGoesOnByItself()
    {
        Ceremony(out var team, withEgg: false);
        var credits = new CreditsScreen();
        credits.Begin("Ash", team);
        credits.Held = true;
        Pass(credits, CreditsScreen.TitleTime / CreditsScreen.Hurry + 0.1f);
        Assert.Equal(CreditsPhase.Roll, credits.Phase);
        float start = credits.Scroll;
        Pass(credits, 2f);
        Assert.InRange(credits.Scroll - start, 2f * CreditsScreen.Pace * CreditsScreen.Hurry - 20f, 2f * CreditsScreen.Pace * CreditsScreen.Hurry + 20f);
        for (int guard = 0; guard < 100_000 && credits.Phase == CreditsPhase.Roll; guard++) credits.Advance(1f / 30f);
        Assert.Equal(CreditsPhase.End, credits.Phase);
        // Left alone, the last card gives way after half a minute, however the button is held
        Pass(credits, CreditsScreen.EndWait - 0.5f);
        Assert.Equal(CreditsPhase.End, credits.Phase);
        Pass(credits, 0.8f);
        Assert.Equal(CreditsPhase.Out, credits.Phase);
    }

    [Fact]
    public void EachOfTheTeamHasAnEqualShareOfTheRollFadingInAndOut()
    {
        Ceremony(out var team, withEgg: false);
        var credits = new CreditsScreen();
        credits.Begin("Ash", team);
        float share = credits.Length / team.Count;
        for (int i = 0; i < team.Count; i++)
        {
            Assert.Equal((i, 1f), credits.MemberAt(i * share + share / 2f));
            Assert.InRange(credits.MemberAt(i * share + 4f).Alpha, 0f, 0.2f);
            Assert.InRange(credits.MemberAt((i + 1) * share - 4f).Alpha, 0f, 0.2f);
        }
        // A line fades over the last stretch at the top and at the foot of the picture
        Assert.Equal(0f, CreditsScreen.LineAlpha(0f));
        Assert.Equal(1f, CreditsScreen.LineAlpha(CreditsScreen.ViewHeight / 2f));
        Assert.Equal(0f, CreditsScreen.LineAlpha(CreditsScreen.ViewHeight));
        Assert.InRange(CreditsScreen.LineAlpha(CreditsScreen.FadeBand / 2f), 0.4f, 0.6f);
        // With nobody on the team the pool is left empty
        var empty = new CreditsScreen();
        empty.Begin("Ash", Array.Empty<Pokemon>());
        Assert.Equal((-1, 0f), empty.MemberAt(100f));
    }

    // ------------------------------------------------------------------ the pieces of the room

    [Fact]
    public void TheMachinesBallIsTheFieldsOwnOutlined()
    {
        var card = new PokemonPlatinumEngine.Graphics.PixelCanvas(12, 12);
        PokemonPlatinumEngine.Graphics.LeagueArt.PaintMachineBall(card);
        // Red over white, ten texels across with the outline about it, and nothing in the corners
        Assert.False(card.IsOpaque(0, 0) || card.IsOpaque(11, 11));
        Assert.True(card.Get(6, 3).R > 200 && card.Get(6, 3).G < 100);
        Assert.True(card.Get(6, 9) is { R: > 190, G: > 190, B: > 190 });
        Assert.True(card.IsOpaque(0, 6) && card.IsOpaque(11, 6));
        Assert.Equal(GameEngine.MachineBallsVariable, "VAR_HALL_OF_FAME_BALLS");
        Assert.Contains(Find("LayBall", Hall).Everything(), i => i.Op == Op.AddVar && i.Name == GameEngine.MachineBallsVariable);
    }
}
