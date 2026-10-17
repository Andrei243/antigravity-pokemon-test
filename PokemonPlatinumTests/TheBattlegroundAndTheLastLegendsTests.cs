using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The last part of the time after the Hall of Fame (plan 02 · S15, part c) played through with no screen: the
/// Battleground in the Survival Area, its door, Buck's grandfather's welcome, Buck's leaving for the Tower, the day's
/// guests drawn as the original draws them, each guest's question, rematch and words, and the count the Villa reads;
/// Rotom in the Old Chateau's television after dark; the elder of Celestic Town's book and the assistant's word of it;
/// Cynthia before the ruins' painting; and Dialga and Palkia behind their rifts at Spear Pillar, for whoever carries
/// their orbs up Mt. Coronet. Each scene is started as the game starts it, on maps of its own
/// (<see cref="OpeningTests.Game"/>).
/// </summary>
public class TheBattlegroundAndTheLastLegendsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Completed = "FLAG_GAME_COMPLETED", Hall = "PokemonLeagueHallOfFame", Arrested = "FLAG_ARRESTED_CHARON_STARK_MOUNTAIN";
    private const string Room = "Battleground", Drawn = "FLAG_DAILY_SET_BATTLEGROUND_TRAINERS", Battles = "VAR_TIMES_BATTLED_AT_BATTLEGROUND";
    private const string SpokenToBuck = "FLAG_TALKED_TO_BATTLEGROUND_BUCK", BuckLeft = "FLAG_BUCK_LEFT_BATTLEGROUND";
    private const string Chateau = "OldChateauBackMiddleWestRoom", RotomToday = "FLAG_DAILY_BATTLED_OLD_CHATEAU_ROTOM", RotomCaught = "FLAG_CAUGHT_OLD_CHATEAU_ROTOM";
    private const string ElderTold = "FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR", HeardCynthia = "FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA";
    private const string Out = "VAR_EXITED_DISTORTION_WORLD_STATE", Torn = "FLAG_SPEAR_PILLAR_IS_DISTORTED";

    /// <summary>The thirteen who may be in: their key in the room, the trainer of Platinum's data they battle as, and the day's flag.</summary>
    public static IEnumerable<object[]> Guests => new[]
    {
        new object[] { "roark", "leader_roark_rematch", "ROARK" },
        new object[] { "gardenia", "leader_gardenia_rematch", "GARDENIA" },
        new object[] { "wake", "leader_wake_rematch", "CRASHER_WAKE" },
        new object[] { "maylene", "leader_maylene_rematch", "MAYLENE" },
        new object[] { "fantina", "leader_fantina_rematch", "FANTINA" },
        new object[] { "candice", "leader_candice_rematch", "CANDICE" },
        new object[] { "byron", "leader_byron_rematch", "BYRON" },
        new object[] { "volkner", "leader_volkner_rematch", "VOLKNER" },
        new object[] { "cheryl", "cheryl_battleground", "CHERYL" },
        new object[] { "riley", "riley_battleground", "RILEY" },
        new object[] { "marley", "marley_battleground", "MARLEY" },
        new object[] { "buck_guest", "buck_battleground", "BUCK" },
        new object[] { "mira", "mira_battleground", "MIRA" },
    };

    private static readonly string[] Leaders = { "roark", "gardenia", "wake", "maylene", "fantina", "candice", "byron", "volkner" };
    private static readonly string[] Partners = { "cheryl", "riley", "marley", "buck_guest", "mira" };

    /// <summary>A Champion with the National Pokédex, home again; with Charon caught on Stark Mountain unless told otherwise.</summary>
    private static OpeningTests.Game Champion(BattleOutcome fight = BattleOutcome.Won, bool arrested = true)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 72));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 66));
        game.Story.SetBadges("Sinnoh", 0xFF);
        game.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 6);
        game.Story.Set(StoryState.PokedexFlag);
        game.Facing = Direction.Up;
        game.Arrive(Hall, 8, 15);
        Assert.True(game.Story.Has(Completed));
        game.NationalDex = true;
        if (arrested)
        {
            // What Stark Mountain's last scene leaves (plan 02 · S15, part b)
            game.Story.Set(Arrested);
            game.Story.SetVar("VAR_STARK_MOUNTAIN_ROOM_3_STATE", 1);
            foreach (string shown in new[] { "FLAG_HIDE_BATTLEGROUND_BUCK", "FLAG_HIDE_SURVIVAL_AREA_BUCK", "FLAG_HIDE_SURVIVAL_AREA_RIVAL" }) game.Story.Unset(shown);
        }
        return game;
    }

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    /// <summary>Speaks to someone of the room from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing)
    {
        var npc = game.Present(key) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        Assert.False(game.Map.IsSolid(game.Tile.X, game.Tile.Y), $"nobody can stand at {game.Tile} to speak to {key}");
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? game.Map.Name), npc);
    }

    private static string Words(HeadlessScriptHost host) => string.Join(" ", host.Transcript.Select(l => l.Text));

    /// <summary>Comes into the Battleground by its door with a day's draw queued: the first, the throws for the second and the third, the fourth, the words.</summary>
    private static OpeningTests.Game InTheRoom(OpeningTests.Game game, params int[] rolls)
    {
        foreach (int roll in rolls) game.Rolls.Enqueue(roll);
        game.Arrive(Room, 7, 11);
        return game;
    }

    private static string[] In(OpeningTests.Game game) => Leaders.Concat(Partners).Where(key => game.Present(key) != null).ToArray();

    // ------------------------------------------------------------------ before any of it

    [Fact]
    public void ThisPartNeedsNoStoryVersionAndItsPeopleStandUnderTheirFlags()
    {
        var game = new OpeningTests.Game();
        // common.ChapterTwelve hid this part's people already (plan 02 · S15, part a): Buck in his room, Cynthia in the ruins
        Assert.Equal(19, StoryState.CurrentVersion);   // 19 is plan 18 · W2's badges by region, not this part's
        var room = game.Maps[Room];
        Assert.Equal((17, 13, InteriorStyle.House), (room.Width, room.Height, room.Interior));
        var buck = Assert.Single(room.Everyone, n => n.Key == "buck");
        Assert.Equal((11, 8, "FLAG_HIDE_BATTLEGROUND_BUCK", "Buck"), (buck.GridX, buck.GridY, buck.HiddenBy, buck.Script));
        Assert.True(game.Story.Has("FLAG_HIDE_BATTLEGROUND_BUCK"));
        // His grandfather is behind the bar, spoken to across it (the original's counter under the bar's own tiles)
        var oldMan = Assert.Single(room.Everyone, n => n.Key == "expert_m");
        Assert.Equal((13, 9, Direction.Left), (oldMan.GridX, oldMan.GridY, oldMan.Facing));
        Assert.Equal(TileBehavior.Counter, room.BehaviourAt(12, 9));
        Assert.True(room.IsSolid(12, 9) && room.IsWalkable(11, 9));
        // The thirteen who may be in are each shown by a flag of the room's own, which nothing has set: nobody is in
        foreach (string key in Leaders.Concat(Partners))
        {
            var guest = Assert.Single(room.Everyone, n => n.Key == key);
            Assert.StartsWith("FLAG_BATTLEGROUND_HAS_", guest.ShownBy);
            Assert.False(guest.IsTrainer, $"{key} is a trainer of the map: a Leader's class brings no eye theme (MusicTests)");
        }
        room.ApplyPresence(game.Story.Has);
        Assert.Equal(new[] { "expert_m" }, room.NPCs.Select(n => n.Key));
        // The four places are the original's, open floor each
        foreach (var (x, y) in new[] { (2, 9), (6, 8), (9, 7), (11, 5) }) Assert.True(room.IsWalkable(x, y), $"({x},{y})");
        // Every trainer they battle as is in Platinum's data, the Leaders' with teams of the sixties
        foreach (object[] guest in Guests)
        {
            var trainer = TrainerDatabase.Get((string)guest[1]);
            Assert.NotNull(trainer);
            Assert.True(trainer!.Party.Max(p => p.Level) >= 55, (string)guest[1]);
        }
        Assert.All(Leaders, key => Assert.Equal("Leader", TrainerDatabase.Get(((string)Guests.Single(g => (string)g[0] == key)[1]))!.Class));

        // Cynthia in the ruins, hidden from the start; the two tiles before the painting
        var cave = game.Maps["CelesticTownCave"];
        var cynthia = Assert.Single(cave.Everyone, n => n.Key == "cynthia");
        Assert.Equal(("FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA", "Cynthia"), (cynthia.HiddenBy, cynthia.NpcType));
        Assert.True(game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA"));
        Assert.Equal("Painting", cave.TileScripts[(9, 2)]);
        Assert.Equal("Painting", cave.TileScripts[(10, 2)]);

        // The two pillars: nobody on either, one rift each over the altar, read from below it, and the way down
        foreach (var (name, x) in new[] { ("SpearPillarDialga", 29), ("SpearPillarPalkia", 33) })
        {
            var pillar = game.Maps[name];
            Assert.Empty(pillar.Everyone);
            var rift = Assert.Single(pillar.Props, p => p.Type == PropType.Rift);
            Assert.True(rift.Covers(x, 17));
            Assert.DoesNotContain(pillar.Props, p => p.Type == PropType.RiftShadow);
            Assert.Equal("Rift", pillar.TileScripts[(x, 16)]);
            Assert.Equal("Rift", pillar.TileScripts[(x, 17)]);
            Assert.True(pillar.IsWalkable(x, 18), $"{name}: nobody can stand below the rift");
            Assert.Equal(("MtCoronet6F", 7), (pillar.GetWarpAt(31, 53)!.TargetMap, pillar.GetWarpAt(31, 53)!.TargetX));
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)?.Id);
        }
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(Room)?.Id);
        // Nothing of this part is asked ahead of its scene any more
        Assert.Empty(StoryTests.AskedAheadOfTheirScenes);
    }

    // ------------------------------------------------------------------ the Battleground's door and the welcome

    [Fact]
    public void TheBattlegroundsDoorOpensOnceCharonIsCaught()
    {
        var game = Champion(arrested: false);
        game.Arrive("Sinnoh", 655, 341);
        Assert.True(game.Map.IsSolid(652, 338));
        Assert.Contains("locked", Words(game.Play(Find("BattlegroundDoor", "survival_area"))));
        Run(game, "survival_area", $"setflag {Arrested}");
        Assert.False(game.Map.IsSolid(652, 338));
        Assert.DoesNotContain("locked", Words(game.Play(Find("BattlegroundDoor", "survival_area"))));
        // Through the door onto the mat, facing up the room; and back out onto the step before the door
        var door = game.Map.GetWarpAt(652, 338)!;
        Assert.Equal((Room, 7, 11, Direction.Up), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        var room = game.Maps[Room];
        var back = Assert.Single(room.Warps);
        Assert.Equal((7, 12, "Sinnoh", 652, 339, Direction.Down), (back.SourceX, back.SourceY, back.TargetMap, back.TargetX, back.TargetY, back.TargetFacing));
        Assert.True(game.Map.IsWalkable(652, 339));
    }

    [Fact]
    public void BucksGrandfatherWelcomesWhoeverFirstComesIn()
    {
        var game = InTheRoom(Champion());
        var welcome = game.Played.Last(p => p.Script == "Battleground.OnEnter").Host;
        Assert.Contains("emote Old Man Exclaim", welcome.Log, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(welcome.Transcript, l => l.Speaker == "Old Man" && l.Text.Contains("Welcome to the Battleground"));
        // The player has gone up toward the bar, a tile short of the original's, which is the tile under Buck's
        Assert.Equal((10, 9), game.Tile);
        Assert.Equal(1, game.Story.Var("VAR_BATTLEGROUND_STATE"));
        Assert.True(game.Story.Has("FLAG_VISITED_BATTLEGROUND"));
        // Once only
        game.Arrive(Room, 7, 11);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal((7, 11), game.Tile);
        // Spoken to, he tells of Stark Mountain until Heatran has been met there
        Assert.Contains("Magma Stone", Words(game.Talk("expert_m")));
        game.Story.Set("FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN");
        Assert.DoesNotContain("Magma Stone", Words(game.Talk("expert_m")));
    }

    // ------------------------------------------------------------------ Buck

    [Theory]
    [InlineData(Direction.Up, 7, 11)]      // from below him (11, 9): west, south, west and out
    [InlineData(Direction.Down, 7, 11)]    // from above him (11, 7)
    [InlineData(Direction.Right, 7, 11)]   // from his west (10, 8): south first
    public void BuckSaysHisPlaceIsThePlayersAndLeavesForTheTower(Direction facing, int outX, int outY)
    {
        var game = InTheRoom(Champion());
        var buck = game.Present("buck")!;
        Assert.Equal((11, 8), (buck.GridX, buck.GridY));
        Assert.False(game.Story.Has(SpokenToBuck));
        var said = TalkFrom(game, "buck", facing);
        Assert.Contains("Battle Frontier", Words(said));
        Assert.Equal((outX, outY), (buck.GridX, buck.GridY));
        Assert.Null(game.Present("buck"));
        Assert.True(game.Story.Has(SpokenToBuck) && game.Story.Has(BuckLeft) && game.Story.Has("FLAG_HIDE_BATTLEGROUND_BUCK"));
        // He is one the Battle Salon lets in from now on (plan 06 · R18a)
        Assert.Contains(BuckLeft, BattleTowerChallenge.PartnerFlags);
        // ...and gone from the room the next time
        game.Arrive(Room, 7, 11);
        Assert.Null(game.Present("buck"));
    }

    [Fact]
    public void HeatranWakesOnceBuckHasBeenSpokenToAtHisPlace()
    {
        var game = Champion();
        foreach (string gone in new[] { "TEAM_GALACTIC", "BUCK", "MAGMA_STONE", "ROCK_SMASH_LOOKER" }) game.Story.Set($"FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_{gone}");
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.Null(game.Present("heatran"));
        InTheRoom(game);
        TalkFrom(game, "buck", Direction.Up);
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.NotNull(game.Present("heatran"));
        // The flag is set by a script now, so the Hall of Fame's own question about it is answered
        Assert.Contains(Find("Buck", Room).Everything(), i => i.Op == Op.SetFlag && i.Name == SpokenToBuck);
        Assert.Contains(SpokenToBuck, Scripts.FlagsWritten);
    }

    // ------------------------------------------------------------------ the day's guests

    [Fact]
    public void TheDaysGuestsAreDrawnAsTheOriginalDrawsThem()
    {
        // Crasher Wake first; the second throw shows him again and is thrown again, then Fantina; a nine for the
        // third is nobody; a six for the fourth is nobody; and the second set of words
        var game = InTheRoom(Champion(), 2, 2, 4, 8, 5, 1);
        Assert.Equal(new[] { "wake", "fantina" }, In(game));
        Assert.Equal((2, 9, Direction.Right), (game.Present("wake")!.GridX, game.Present("wake")!.GridY, game.Present("wake")!.Facing));
        Assert.Equal((6, 8, Direction.Down), (game.Present("fantina")!.GridX, game.Present("fantina")!.GridY, game.Present("fantina")!.Facing));
        Assert.Equal((2, 4, 8, 5, 1), (game.Story.Var("VAR_BATTLEGROUND_TRAINER_1"), game.Story.Var("VAR_BATTLEGROUND_TRAINER_2"), game.Story.Var("VAR_BATTLEGROUND_TRAINER_3"),
            game.Story.Var("VAR_BATTLEGROUND_TRAINER_4"), game.Story.Var("VAR_RANDOM_BATTLEGROUND_MESSAGE")));
        Assert.True(game.Story.Has(Drawn) && game.Story.Has("FLAG_HIDE_BATTLEGROUND_TRAINER_4"));

        // The same day they are the same, with no die thrown
        game.Rolls.Enqueue(7);
        game.Arrive(Room, 7, 11);
        Assert.Equal(new[] { "wake", "fantina" }, In(game));
        Assert.Equal(2, game.Story.Var("VAR_BATTLEGROUND_TRAINER_1"));

        // A new day draws again: Candice; Gardenia; the third throw shows each of them in turn before Volkner; and
        // Buck for the fourth, who comes only once he has left for the Tower
        game.Story.ClearDaily();
        InTheRoom(game, 5, 1, 5, 1, 7, 3, 0);
        Assert.Equal(new[] { "gardenia", "candice", "volkner" }, In(game));
        Assert.Equal((2, 9), (game.Present("candice")!.GridX, game.Present("candice")!.GridY));
        Assert.Equal((6, 8), (game.Present("gardenia")!.GridX, game.Present("gardenia")!.GridY));
        Assert.Equal((9, 7, Direction.Left), (game.Present("volkner")!.GridX, game.Present("volkner")!.GridY, game.Present("volkner")!.Facing));
        Assert.True(game.Story.Has("FLAG_HIDE_BATTLEGROUND_TRAINER_4"));
        game.Story.ClearDaily();
        game.Story.Set(BuckLeft);
        InTheRoom(game, 5, 1, 5, 1, 7, 3, 0);
        Assert.Equal(new[] { "gardenia", "candice", "volkner", "buck_guest" }, In(game));
        Assert.Equal((11, 5, Direction.Up), (game.Present("buck_guest")!.GridX, game.Present("buck_guest")!.GridY, game.Present("buck_guest")!.Facing));
        Assert.False(game.Story.Has("FLAG_HIDE_BATTLEGROUND_TRAINER_4"));
    }

    [Theory]
    [InlineData(2, 9, true)]
    [InlineData(6, 8, true)]
    [InlineData(9, 7, true)]
    [InlineData(11, 5, true)]
    [InlineData(5, 8, false)]
    [InlineData(11, 9, false)]
    public void AGameTakenUpOnOneOfTheFourPlacesIsMovedToTheMat(int x, int y, bool moved)
    {
        // A save made on a place that was empty that day, loaded on a day that fills it: nobody is put down on the player
        var game = InTheRoom(Champion());
        game.Rolls.Enqueue(3);
        game.Story.ClearDaily();
        game.Arrive(Room, x, y);
        Assert.Equal(moved ? (7, 11) : (x, y), game.Tile);
        Assert.DoesNotContain(game.Map.NPCs, n => (n.GridX, n.GridY) == game.Tile);
    }

    [Fact]
    public void ALeaderDrawnAlreadyIsThrownForAgainAndNobodyComesAtTheEighthThrow()
    {
        // Roark, and then nothing but Roark: seven throws that show him are thrown again and the eighth ends it with
        // nobody, for the second place and for the third (the die shows nought once a test's faces run out)
        var game = InTheRoom(Champion());
        Assert.Equal(new[] { "roark" }, In(game));
        Assert.Equal((0, 8, 8), (game.Story.Var("VAR_BATTLEGROUND_TRAINER_1"), game.Story.Var("VAR_BATTLEGROUND_TRAINER_2"), game.Story.Var("VAR_BATTLEGROUND_TRAINER_3")));
        // ...and the fourth is Cheryl, who hasn't been travelled with
        Assert.Equal(0, game.Story.Var("VAR_BATTLEGROUND_TRAINER_4"));
        Assert.True(game.Story.Has("FLAG_HIDE_BATTLEGROUND_TRAINER_4"));
    }

    [Theory]
    [InlineData(0, "cheryl", "FLAG_TRAVELED_WITH_CHERYL")]
    [InlineData(1, "riley", "FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY")]
    [InlineData(2, "marley", "FLAG_TRAVELED_WITH_MARLEY")]
    [InlineData(3, "buck_guest", BuckLeft)]
    [InlineData(4, "mira", "FLAG_TRAVELED_WITH_MIRA")]
    public void TheFourthGuestComesOnlyOnceTheStoryHasBroughtThem(int face, string key, string flag)
    {
        // A day that draws them before the player has travelled with them: nobody stands before the screen all day,
        // even once the story has caught up (the original hides the place at the draw)
        var game = InTheRoom(Champion(), 0, 8, 8, face, 0);
        Assert.Null(game.Present(key));
        Assert.True(game.Story.Has("FLAG_HIDE_BATTLEGROUND_TRAINER_4"));
        game.Story.Set(flag);
        game.Arrive(Room, 7, 11);
        Assert.Null(game.Present(key));
        // The next day they are in
        game.Story.ClearDaily();
        InTheRoom(game, 0, 8, 8, face, 0);
        Assert.Equal((11, 5), (game.Present(key)!.GridX, game.Present(key)!.GridY));
        Assert.Contains(flag, BattleTowerChallenge.PartnerFlags);
    }

    // ------------------------------------------------------------------ a guest's question, battle and words

    /// <summary>The room with one guest in, whoever the day would have drawn.</summary>
    private static OpeningTests.Game With(string key, int words = 0, BattleOutcome fight = BattleOutcome.Won)
    {
        var game = Champion(fight);
        int leader = Array.IndexOf(Leaders, key), partner = Array.IndexOf(Partners, key);
        foreach (string flag in BattleTowerChallenge.PartnerFlags) game.Story.Set(flag);
        InTheRoom(game, leader >= 0 ? leader : 0, 8, 8, partner >= 0 ? partner : 5, words);
        Assert.NotNull(game.Present(key));
        return game;
    }

    [Theory]
    [MemberData(nameof(Guests))]
    public void EachGuestAsksAsksAgainOnceRefusedAndBattlesWithTheirRematchTeam(string key, string trainer, string who)
    {
        foreach (int words in new[] { 0, 1 })
        {
            var game = With(key, words);
            string beaten = $"FLAG_DAILY_DEFEATED_BATTLEGROUND_{who}";
            // Refused: no battle, nothing counted, and other words for the same question next time
            game.Answers.Enqueue(1);
            var refused = game.Talk(key);
            string first = Assert.Single(refused.Asked).Question;
            Assert.DoesNotContain(refused.Log, l => l.StartsWith("battle "));
            Assert.Equal(0, game.Story.Var(Battles));
            Assert.False(game.Story.Has(beaten));
            // Agreed to: the battle is theirs of Platinum's data, counted, and they are beaten for the day
            var fought = game.Talk(key);
            string second = Assert.Single(fought.Asked).Question;
            Assert.NotEqual(first, second);
            Assert.Contains($"battle {trainer} Won", fought.Log);
            Assert.Equal(1, game.Story.Var(Battles));
            Assert.True(game.Story.Has(beaten));
            // Spoken to again they ask nothing more today, in this room or on coming back
            var after = game.Talk(key);
            Assert.Empty(after.Asked);
            Assert.NotEmpty(after.Transcript);
            Assert.DoesNotContain(after.Log, l => l.StartsWith("battle "));
            game.Arrive(Room, 7, 11);
            Assert.Empty(game.Talk(key).Asked);
            // The next day they ask again
            game.Story.ClearDaily();
            int leader = Array.IndexOf(Leaders, key), partner = Array.IndexOf(Partners, key);
            InTheRoom(game, leader >= 0 ? leader : 0, 8, 8, partner >= 0 ? partner : 5, words);
            Assert.Single(game.Talk(key).Asked);
        }
    }

    [Fact]
    public void TheTwoSetsOfWordsAreDifferentForEveryGuest()
    {
        foreach (object[] guest in Guests)
        {
            string key = (string)guest[0];
            string Asks(int words) => Assert.Single(With(key, words).Talk(key).Asked).Question;
            Assert.NotEqual(Asks(0), Asks(1));
        }
    }

    [Fact]
    public void ABattleLostIsCountedAndLeavesTheGuestToBeAskedAgain()
    {
        var game = With("maylene", fight: BattleOutcome.Lost);
        var lost = game.Talk("maylene");
        Assert.Contains("battle leader_maylene_rematch Lost", lost.Log);
        // The original counts the battle as it begins
        Assert.Equal(1, game.Story.Var(Battles));
        Assert.False(game.Story.Has("FLAG_DAILY_DEFEATED_BATTLEGROUND_MAYLENE"));
    }

    [Fact]
    public void RoarkAndByronHaveOtherWordsWhileBothAreIn()
    {
        // Roark at the first table, his father in the middle of the floor
        var game = InTheRoom(Champion(), 0, 6, 8, 5, 0);
        Assert.Equal(new[] { "roark", "byron" }, In(game));
        game.Talk("roark");
        Assert.Contains("my dad", Words(game.Talk("roark")));
        game.Talk("byron");
        Assert.Contains("old man", Words(game.Talk("byron")));
        // Alone, each has the day's own words
        var alone = With("roark");
        alone.Talk("roark");
        Assert.DoesNotContain("my dad really is", Words(alone.Talk("roark")));
    }

    [Fact]
    public void TheVillasCatalogueReadsTheCountTheBattlegroundKeeps()
    {
        Assert.Equal(Battles, VillaRules.BattlegroundVariable);
        var game = With("gardenia");
        game.Talk("gardenia");
        Assert.Equal(1, game.Story.Var(VillaRules.BattlegroundVariable));
        // Every guest's script counts
        foreach (object[] guest in Guests)
        {
            var npc = game.Maps[Room].Everyone.Single(n => n.Key == (string)guest[0]);
            Assert.Contains(Find(npc.Script!, Room).Everything(), i => i.Op == Op.AddVar && i.Name == Battles && i.Number == 1);
        }
    }

    // ------------------------------------------------------------------ Rotom

    private static OpeningTests.Game BeforeTheTelevision(TimeOfDay time, BattleOutcome fight = BattleOutcome.Won)
    {
        // No Hall of Fame and no National Pokédex: the original asks for neither
        var game = new OpeningTests.Game(0, fight) { Time = time };
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Grotle")!, 24));
        game.Arrive(Chateau, 11, 4);
        game.Facing = Direction.Up;
        return game;
    }

    private static HeadlessScriptHost Thump(OpeningTests.Game game) => game.Play(Find(game.Map.SignScripts[(11, 3)], Chateau));

    [Theory]
    [InlineData(TimeOfDay.Morning)]
    [InlineData(TimeOfDay.Day)]
    [InlineData(TimeOfDay.Twilight)]
    public void ByDayTheTelevisionOnlyStares(TimeOfDay time)
    {
        var game = BeforeTheTelevision(time);
        var looked = Thump(game);
        Assert.Empty(looked.Asked);
        Assert.Contains("staring", Words(looked));
        Assert.DoesNotContain(looked.Log, l => l.StartsWith("wildbattle"));
        Assert.False(game.Story.Has(RotomToday));
    }

    [Theory]
    [InlineData(TimeOfDay.Night, BattleOutcome.Won, false)]
    [InlineData(TimeOfDay.LateNight, BattleOutcome.Fled, false)]
    [InlineData(TimeOfDay.Night, BattleOutcome.Caught, true)]
    [InlineData(TimeOfDay.LateNight, BattleOutcome.Lost, false)]
    public void AfterDarkRotomComesOutOfItOnceADayUntilItIsCaught(TimeOfDay time, BattleOutcome fight, bool caught)
    {
        var game = BeforeTheTelevision(time, fight);
        // Refused, nothing happens and the day isn't used up
        game.Answers.Enqueue(1);
        var refused = Thump(game);
        Assert.Contains(refused.Asked, a => a.Question.Contains("thump"));
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("wildbattle"));
        Assert.False(game.Story.Has(RotomToday));
        // Thumped: its cry, and a Rotom of level 20 that may be run from
        var thumped = Thump(game);
        Assert.Contains("cry Rotom", thumped.Log);
        Assert.Contains($"wildbattle Rotom 20 {fight}", thumped.Log);
        Assert.True(game.Story.Has(RotomToday));
        Assert.Equal(caught, game.Story.Has(RotomCaught));
        Assert.Equal(caught, game.Party.Members.Any(p => p.Species.Name == "Rotom" && p.Level == 20));
        if (fight is BattleOutcome.Won or BattleOutcome.Fled) Assert.Contains("slipped back", Words(thumped));
        // Not again tonight
        var again = Thump(game);
        Assert.Empty(again.Asked);
        Assert.Contains("staring", Words(again));
        // The next night it is back, unless it was caught
        game.Story.ClearDaily();
        Assert.Equal(caught ? 0 : 1, Thump(game).Asked.Count);
    }

    [Fact]
    public void BothTilesOfTheSetAreReadAndTheSecretKeysRoomStaysShut()
    {
        var room = MapDatabase.Fresh()(Chateau);
        Assert.Equal(("TV", "TV"), (room.SignScripts[(11, 3)], room.SignScripts[(12, 3)]));
        // Rotom's appliances are behind the wall the Secret Key of an event opens: nothing gives the key
        Assert.DoesNotContain(Scripts.All.SelectMany(s => s.Everything()), i => i.Op is Op.Give or Op.Find or Op.AddItem && i.Name == "Secret Key");
        // ...and the professor who waits in that room is still hidden for every game
        Assert.True(new OpeningTests.Game().Story.Has("FLAG_HIDE_ROTOMS_ROOM_PROF_ROWAN"));
    }

    // ------------------------------------------------------------------ the elder's book, and the assistant

    [Fact]
    public void TheElderReadsFromHerBookOnlyToAChampion()
    {
        var game = new OpeningTests.Game();
        game.Story.Unset("FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER");
        game.Arrive("CelesticTownNorthHouse", 4, 8);
        Assert.DoesNotContain("Adamant", Words(game.Talk("elder")));
        Assert.False(game.Story.Has(ElderTold));
        game.Story.Set(Completed);
        var read = game.Talk("elder");
        Assert.Contains("Adamant Orb", Words(read));
        Assert.Contains("Lustrous Orb", Words(read));
        Assert.True(game.Story.Has(ElderTold));
        // Asked again she reads it again
        Assert.Contains("Adamant Orb", Words(game.Talk("elder")));
    }

    [Fact]
    public void TheAssistantPassesOnTheEldersWordAndThenTheProfessors()
    {
        var game = Champion();
        game.NationalDex = false;
        game.Seen = 120;
        game.Arrive("Sinnoh", 180, 842);
        HeadlessScriptHost Asked() => game.Play(Find("Counterpart", "sandgem_town"), game.Maps["Sinnoh"].Everyone.First(n => n.Script == "Counterpart"));
        // Until the elder has told it: she has something to say
        Assert.Contains("elder of Celestic Town", Words(Asked()));
        // After it: the professor's word of Spear Pillar
        game.Story.Set(ElderTold);
        Assert.Contains("Spear Pillar", Words(Asked()));
        // Once either has been battled there since the last victory: her congratulations
        game.Story.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 1);
        Assert.Contains("Congratulations", Words(Asked()));
        game.Story.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 0);
        game.Story.SetVar("VAR_SPEAR_PILLAR_PALKIA_STATE", 1);
        Assert.Contains("Congratulations", Words(Asked()));
        // With every Pokémon of Sinnoh seen she wants the Pokédex shown, whatever the elder said
        game.Seen = 210;
        Assert.Contains("Professor Rowan", Words(Asked()));
    }

    // ------------------------------------------------------------------ Cynthia before the painting

    private static OpeningTests.Game BeforeThePainting(int x, bool arrested = true)
    {
        var game = Champion(arrested: arrested);
        game.Story.Set("FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING");
        game.Story.Set("FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS");
        game.Arrive("CelesticTownCave", x, 3);
        game.Facing = Direction.Up;
        return game;
    }

    private static HeadlessScriptHost LookAtThePainting(OpeningTests.Game game) => game.Play(Find(game.Map.TileScripts[(game.Tile.X, 2)], "celestic_town_cave"));

    [Theory]
    [InlineData(9, 10)]
    [InlineData(10, 9)]
    public void CynthiaComesInAndTellsHerReadingOfThePainting(int x, int beside)
    {
        var game = BeforeThePainting(x);
        Assert.Null(game.Present("cynthia"));
        var told = LookAtThePainting(game);
        // She came in by the door and stood on the other tile before the painting; three questions, each a yes
        Assert.Equal(3, told.Asked.Count);
        Assert.Contains("Giratina", Words(told));
        Assert.Contains(told.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("myths"));
        Assert.DoesNotContain("carried away", Words(told));
        // She is gone again, by her flag, and has been heard
        Assert.Null(game.Present("cynthia"));
        Assert.True(game.Story.Has(HeardCynthia) && game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA"));
        var cynthia = game.Maps["CelesticTownCave"].Everyone.Single(n => n.Key == "cynthia");
        Assert.Equal((4, 11), (cynthia.GridX, cynthia.GridY));
        Assert.Equal((x, 3), game.Tile);
        Assert.NotEqual(x, beside);
        // The painting is only a painting from then on...
        var again = LookAtThePainting(game);
        Assert.Empty(again.Asked);
        Assert.DoesNotContain(again.Transcript, l => l.Speaker == "Cynthia");
        // ...until the Hall of Fame has been entered again, whose script lets her be heard once more
        game.Play(Find("Victory", Hall));
        Assert.False(game.Story.Has(HeardCynthia));
        Assert.Equal(3, LookAtThePainting(game).Asked.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ANoAtAnyOfHerQuestionsAndSheBegsPardonAndGoes(int asked)
    {
        var game = BeforeThePainting(10);
        // Yes to every question before the last one asked, and a no to that
        foreach (int answer in Enumerable.Repeat(0, asked - 1).Append(1)) game.Answers.Enqueue(answer);
        var told = LookAtThePainting(game);
        Assert.Equal(asked, told.Asked.Count);
        Assert.Contains("carried away", Words(told));
        Assert.DoesNotContain(told.Transcript, l => l.Text.Contains("someone who studies myths"));
        Assert.Null(game.Present("cynthia"));
        Assert.True(game.Story.Has(HeardCynthia) && game.Story.Has("FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA"));
    }

    [Fact]
    public void SheDoesNotComeUntilCharonIsCaught()
    {
        var game = BeforeThePainting(9, arrested: false);
        var looked = LookAtThePainting(game);
        Assert.Empty(looked.Asked);
        Assert.DoesNotContain(looked.Transcript, l => l.Speaker == "Cynthia");
        Assert.False(game.Story.Has(HeardCynthia));
    }

    // ------------------------------------------------------------------ Mt. Coronet's summit

    private static OpeningTests.Game UnderTheSummit(Action<OpeningTests.Game> with)
    {
        var game = Champion();
        game.Story.SetVar(Out, 3);
        game.Story.Set(ElderTold);
        with(game);
        game.Arrive("MtCoronet6F", 7, 7);
        return game;
    }

    private static void Hold(OpeningTests.Game game, string item) => game.Party.Members[0].HeldItem = ItemDatabase.Get(item)!;

    [Fact]
    public void TheMountainsWayOutLeadsToThePillarOfWhicheverOrbIsCarried()
    {
        string Leads(Action<OpeningTests.Game> with) => UnderTheSummit(with).Map.GetWarpAt(7, 5)!.TargetMap;
        // No orb: the pillar as it stands
        Assert.Equal("SpearPillar", Leads(_ => { }));
        // The Adamant Orb in the bag, or held by one of the team: Dialga's
        Assert.Equal("SpearPillarDialga", Leads(g => g.Bag.AddItem(ItemDatabase.Get("Adamant Orb")!)));
        Assert.Equal("SpearPillarDialga", Leads(g => Hold(g, "Adamant Orb")));
        // The Lustrous Orb: Palkia's
        Assert.Equal("SpearPillarPalkia", Leads(g => g.Bag.AddItem(ItemDatabase.Get("Lustrous Orb")!)));
        Assert.Equal("SpearPillarPalkia", Leads(g => Hold(g, "Lustrous Orb")));
        // Both: Dialga first, then Palkia once Dialga has been battled or caught, then the pillar as it stands
        void Both(OpeningTests.Game g) { g.Bag.AddItem(ItemDatabase.Get("Adamant Orb")!); g.Bag.AddItem(ItemDatabase.Get("Lustrous Orb")!); }
        Assert.Equal("SpearPillarDialga", Leads(Both));
        Assert.Equal("SpearPillarPalkia", Leads(g => { Both(g); g.Story.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 1); }));
        Assert.Equal("SpearPillarPalkia", Leads(g => { Both(g); g.Story.Set("FLAG_CAUGHT_DIALGA"); }));
        Assert.Equal("SpearPillar", Leads(g => { Both(g); g.Story.SetVar("VAR_SPEAR_PILLAR_DIALGA_STATE", 1); g.Story.SetVar("VAR_SPEAR_PILLAR_PALKIA_STATE", 1); }));
        Assert.Equal("SpearPillar", Leads(g => { Both(g); g.Story.Set("FLAG_CAUGHT_DIALGA"); g.Story.Set("FLAG_CAUGHT_PALKIA"); }));
        // Not before the elder has read from her book, nor before the Distortion World is behind the player
        Assert.Equal("SpearPillar", Leads(g => { Both(g); g.Story.Unset(ElderTold); }));
        Assert.Equal("SpearPillar", Leads(g => { Both(g); g.Story.SetVar(Out, 1); }));
        // The torn pillar comes before either
        Assert.Equal("SpearPillarDistorted", Leads(g => { Both(g); g.Story.Set(Torn); }));
        // An Egg holds nothing
        Assert.Equal("SpearPillar", Leads(g => { Hold(g, "Adamant Orb"); g.Party.Members[0].IsEgg = true; }));
    }

    [Fact]
    public void AWayThatIsThereFromAFlagTakesTheTileFromOneThatIsThereUntilAnother()
    {
        var map = new Map(4, 4);
        map.Warps.Add(new Warp { SourceX = 1, SourceY = 1, TargetMap = "Plain", Until = "FLAG_A" });
        map.Warps.Add(new Warp { SourceX = 1, SourceY = 1, TargetMap = "Other", From = "FLAG_A" });
        map.Warps.Add(new Warp { SourceX = 1, SourceY = 1, TargetMap = "Third", From = "FLAG_B" });
        var set = new HashSet<string>();
        map.ApplyDoors(set.Contains);
        Assert.Equal("Plain", map.GetWarpAt(1, 1)!.TargetMap);
        set.Add("FLAG_B");
        map.ApplyDoors(set.Contains);
        Assert.Equal("Third", map.GetWarpAt(1, 1)!.TargetMap);
        set.Clear();
        set.Add("FLAG_A");
        map.ApplyDoors(set.Contains);
        Assert.Equal("Other", map.GetWarpAt(1, 1)!.TargetMap);
        Assert.Equal(3, map.WarpsAt(1, 1).Count());
        // The tile is never shut: a way always leads on from it
        Assert.False(map.IsSolid(1, 1));
    }

    [Fact]
    public void SetvarTakesAnotherVariablesValueAndTheTeamIsAskedWhatItHolds()
    {
        var game = Champion();
        var host = Run(game, "sandgem_town", "random 6\nsetvar VAR_TEST_COPY RESULT\nsetvar VAR_TEST_OTHER VAR_TEST_COPY\naddvar VAR_TEST_OTHER 2");
        Assert.Equal((0, 2), (host.Story.Var("VAR_TEST_COPY"), host.Story.Var("VAR_TEST_OTHER")));
        game.Rolls.Enqueue(4);
        Run(game, "sandgem_town", "random 6\nsetvar VAR_TEST_COPY RESULT");
        Assert.Equal(4, game.Story.Var("VAR_TEST_COPY"));
        int Holds(string item) { Run(game, "sandgem_town", $"team holds \"{item}\"\nsetvar VAR_TEST_COPY RESULT"); return game.Story.Var("VAR_TEST_COPY"); }
        Assert.Equal(0, Holds("Adamant Orb"));
        Hold(game, "Adamant Orb");
        Assert.Equal(1, Holds("Adamant Orb"));
        Assert.Equal(0, Holds("Lustrous Orb"));
        // What is wrong is told as the file is read
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  setvar RESULT 1\n"));
        Assert.Contains(ScriptLibrary.FromSources(("test", "script A\n  team holds \"Adamant Orbb\"\n  end\n")).Problems(), p => p.Contains("no item"));
    }

    // ------------------------------------------------------------------ Dialga and Palkia

    public static IEnumerable<object[]> Rifts => new[]
    {
        new object[] { "SpearPillarDialga", "spear_pillar_dialga", "Dialga", 29, "Adamant Orb" },
        new object[] { "SpearPillarPalkia", "spear_pillar_palkia", "Palkia", 33, "Lustrous Orb" },
    };

    private static OpeningTests.Game BelowTheRift(string map, int x, string orb, BattleOutcome fight = BattleOutcome.Won)
    {
        var game = Champion(fight);
        game.Story.SetVar(Out, 3);
        game.Story.Set(ElderTold);
        game.Bag.AddItem(ItemDatabase.Get(orb)!);
        game.Arrive("MtCoronet6F", 7, 7);
        game.Through(map);
        Assert.Equal(map, game.Map.Name);
        game.Tile = (x, 18);
        game.Facing = Direction.Up;
        return game;
    }

    [Theory]
    [MemberData(nameof(Rifts))]
    public void TheRiftIsAskedAndItsPokemonComesOutAtLevelSeventy(string map, string place, string species, int x, string orb)
    {
        string upper = species.ToUpperInvariant();
        foreach (var fight in new[] { BattleOutcome.Won, BattleOutcome.Fled, BattleOutcome.Caught, BattleOutcome.Lost })
        {
            var game = BelowTheRift(map, x, orb, fight);
            HeadlessScriptHost Read() => game.Play(Find(game.Maps[map].TileScripts[(x, 17)], place));
            // Refused: nothing
            game.Answers.Enqueue(1);
            var refused = Read();
            Assert.Contains(refused.Asked, a => a.Question.Contains(species));
            Assert.DoesNotContain(refused.Log, l => l.StartsWith("wildbattle"));
            Assert.Equal(0, game.Story.Var($"VAR_SPEAR_PILLAR_{upper}_STATE"));
            // Challenged: its cry, and the battle at level 70
            var met = Read();
            Assert.Contains($"cry {species}", met.Log);
            Assert.Contains($"wildbattle {species} 70 {fight}", met.Log);
            bool lost = fight == BattleOutcome.Lost, caught = fight == BattleOutcome.Caught;
            Assert.Equal(lost ? 0 : 1, game.Story.Var($"VAR_SPEAR_PILLAR_{upper}_STATE"));
            Assert.Equal(caught, game.Story.Has($"FLAG_CAUGHT_{upper}"));
            Assert.Equal(caught, game.Party.Members.Any(p => p.Species.Name == species && p.Level == 70));
            if (lost) continue;
            // The rift is quiet, and the mountain's way leads to the pillar as it stands
            var quiet = Read();
            Assert.Empty(quiet.Asked);
            Assert.Contains("Where does it lead", Words(quiet));
            game.Through("MtCoronet6F");
            Assert.Equal("SpearPillar", game.Map.GetWarpAt(7, 5)!.TargetMap);
            // The Hall of Fame brings back whichever wasn't caught
            game.Play(Find("Victory", Hall));
            Assert.Equal(caught ? 1 : 0, game.Story.Var($"VAR_SPEAR_PILLAR_{upper}_STATE"));
            game.Arrive("MtCoronet6F", 7, 7);
            Assert.Equal(caught ? "SpearPillar" : map, game.Map.GetWarpAt(7, 5)!.TargetMap);
        }
    }

    [Fact]
    public void TheOrbsLieInMtCoronetAndTheirPokemonAreInThePokedexsPlaces()
    {
        var rooms = MapDatabase.Fresh()("MtCoronet4FRooms1And2");
        Assert.Contains(rooms.Everyone, n => n.IsItemBall && n.Item == "Adamant Orb");
        Assert.Contains(rooms.Everyone, n => n.IsItemBall && n.Item == "Lustrous Orb");
        foreach (var (species, place) in new[] { ("Dialga", "Spear Pillar"), ("Palkia", "Spear Pillar"), ("Rotom", "Old Chateau"), ("Giratina", "Turnback Cave") })
        {
            var habitat = Assert.Single(Habitats.Sinnoh!.Of(species), h => h.Ways.HasFlag(HabitatWays.Special));
            Assert.Equal(place, habitat.Name);
        }
    }

    // ------------------------------------------------------------------ the walk, and a rule mended on the way

    [Fact]
    public void TheWholeGamesWalkFindsTheBattlegroundAndBothPillars()
    {
        var maps = MapDatabase.Fresh();
        var reached = WorldWalk.From(maps, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!);
        Assert.Contains((7, 11), reached[maps(Room)]);
        Assert.Contains((10, 9), reached[maps(Room)]);
        Assert.Contains((29, 18), reached[maps("SpearPillarDialga")]);
        Assert.Contains((33, 18), reached[maps("SpearPillarPalkia")]);
        Assert.Contains((11, 4), reached[maps(Chateau)]);
        Assert.Contains((9, 3), reached[maps("CelesticTownCave")]);
    }

    [Fact]
    public void ARockClimbDoesNotEndOnATileSomeoneStandsOn()
    {
        // Acuity Lakefront's rock face (plan 02 · S11, part a, which found it): climbed from (310, 242) to (310, 237)
        // The rival stands at its head from the start of every game, which is where it showed
        var map = MapDatabase.Fresh()("Sinnoh");
        var rival = Assert.Single(map.NPCs, n => (n.GridX, n.GridY) == (310, 237));
        float foot = map.HeightAt(310, 242);
        var climber = new Walker(TravelMode.OnFoot, foot, Moves: FieldMoves.RockClimb, Climbing: true);
        var stopped = FieldMovement.Step(map, 310, 242, Direction.Up, climber);
        Assert.Equal((StepKind.Blocked, Obstacle.RockFace), (stopped.Kind, stopped.Obstacle));
        // Once he has run on to the lake the face is climbed to its head
        map.NPCs.Remove(rival);
        var climb = FieldMovement.Step(map, 310, 242, Direction.Up, climber);
        Assert.Equal((StepKind.Climb, 310, 237), (climb.Kind, climb.X, climb.Y));
    }

    // ------------------------------------------------------------------ what the original's new game hides, against ours

    [Fact]
    public void EveryoneTheOriginalsNewGameHidesIsHiddenHereButWhoBelongsToAnotherPlan()
    {
        // scripts_init_new_game.s sets these and common.NewGame, or a chapter it calls, does not: each with whose it is
        var elsewhere = new Dictionary<string, string>
        {
            // The original's own leftovers: its unused Battle Park, and three dummies
            ["FLAG_HIDE_BATTLE_PARK_PALMER"] = "unused in Platinum", ["FLAG_HIDE_BATTLE_PARK_RIVAL"] = "unused in Platinum",
            ["FLAG_DUMMY_0x0193"] = "unused", ["FLAG_DUMMY_0x019A"] = "unused", ["FLAG_UNUSED_0x04E4"] = "unused",
            // The Battle Tower's rooms lay their own people out as each is come to (plan 06 · R18a)
            ["FLAG_HIDE_BATTLE_TOWER_BATTLE_ROOM_OPPONENT"] = "plan 06 · R18a", ["FLAG_HIDE_BATTLE_TOWER_TEALA"] = "plan 06 · R18a",
            // The Villa's delivery man and its book (plan 06 · R16b), and the Poffin House's reporter (plan 08's interviews)
            ["FLAG_HIDE_VILLA_DELIVERY_GUY"] = "plan 06 · R16b", ["FLAG_HIDE_VILLA_BOOK"] = "plan 06 · R16b",
            ["FLAG_HIDE_POFFIN_HOUSE_REPORTER"] = "plan 08",
        };
        var game = new OpeningTests.Game();
        foreach (string flag in OriginalsNewGameFlags)
            Assert.True(game.Story.Has(flag) || elsewhere.ContainsKey(flag), $"{flag} is set by the original's new game and by nothing here");
        // Nothing is excused that the game does set
        Assert.DoesNotContain(elsewhere.Keys, game.Story.Has);
    }

    /// <summary>The flags the original's new game sets (res/field/scripts/scripts_init_new_game.s of the pinned decompilation), in its order.</summary>
    private static readonly string[] OriginalsNewGameFlags =
    {
        "FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_2F_RIVAL", "FLAG_HIDE_ROUTE_201_COUNTERPART", "FLAG_HIDE_ROUTE_201_PROF_ROWAN", "FLAG_HIDE_ROUTE_201_BRIEFCASE",
        "FLAG_HIDE_LAKE_VERITY_LOW_WATER_STARLY", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART", "FLAG_DUMMY_0x0193",
        "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_VERITY_CAVERN_PROF_OAK",
        "FLAG_HIDE_LAKE_ACUITY_LOW_WATER_RIVAL", "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_TWINLEAF_TOWN_RIVAL", "FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_1F_RIVAL_MOM",
        "FLAG_HIDE_SANDGEM_TOWN_RIVAL", "FLAG_HIDE_SANDGEM_TOWN_PROF_ROWAN", "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK", "FLAG_HIDE_VALLEY_WINDWORKS_BUILDING_LITTLE_GIRL",
        "FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_LOOKER", "FLAG_HIDE_ETERNA_FOREST_GARDENIA", "FLAG_HIDE_CELESTIC_TOWN_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS",
        "FLAG_HIDE_CELESTIC_TOWN_CAVE_ELDER", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER", "FLAG_HIDE_JUBILIFE_CITY_COUNTERPART",
        "FLAG_HIDE_JUBILIFE_ROWAN", "FLAG_HIDE_JUBILIFE_GALACTIC_GRUNTS", "FLAG_HIDE_JUBILIFE_CITY_POKETCH_CO_PRESIDENT", "FLAG_HIDE_POKETCH_CO_1F_POKETCH_CO_PRESIDENT",
        "FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2", "FLAG_HIDE_JUBILIFE_CITY_COLLECTOR", "FLAG_HIDE_CANALAVE_CITY_COUNTERPART", "FLAG_HIDE_CANALAVE_CITY_PROF_ROWAN",
        "FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE", "FLAG_HIDE_CANALAVE_CITY_SAILOR", "FLAG_HIDE_CANALAVE_LIBRARY_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN",
        "FLAG_HIDE_CANALAVE_LIBRARY_RIVAL", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN", "FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY", "FLAG_DUMMY_0x019A",
        "FLAG_HIDE_ETERNA_CITY_CYNTHIA", "FLAG_HIDE_ETERNA_CITY_RIVAL", "FLAG_HIDE_ETERNA_CITY_POKECENTER_1F_BUNEARY", "FLAG_HIDE_CYCLE_SHOP_CLEFAIRY",
        "FLAG_HIDE_CYCLE_SHOP_POKEFAN_M", "FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK", "FLAG_HIDE_TEAM_GALACTIC_ETERNA_BUILDING_1F_LOOKER", "FLAG_HIDE_ROTOMS_ROOM_PROF_ROWAN",
        "FLAG_HIDE_HEARTHOME_CITY_BUNEARY", "FLAG_HIDE_HEARTHOME_CITY_KEIRA", "FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL", "FLAG_HIDE_PASTORIA_CITY_CROAGUNK",
        "FLAG_HIDE_PASTORIA_CITY_CRASHER_WAKE", "FLAG_HIDE_ROUTE_213_GRUNT_M", "FLAG_HIDE_ROUTE_213_LOOKER", "FLAG_HIDE_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER",
        "FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN", "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY", "FLAG_UNUSED_0x04E4",
        "FLAG_HIDE_VEILSTONE_CRASHER_WAKE", "FLAG_HIDE_VEILSTONE_CITY_LOOKER", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER", "FLAG_HIDE_VEILSTONE_STORE_B1F_PROF_ROWAN",
        "FLAG_HIDE_ROUTE_207_COUNTERPART", "FLAG_HIDE_POKEMON_LEAGUE_NORTH_POKECENTER_1F_RIVAL", "FLAG_HIDE_FLOAROMA_MEADOW_WORKS_KEY", "FLAG_HIDE_BATTLE_TOWER_BATTLE_ROOM_OPPONENT",
        "FLAG_HIDE_BATTLE_TOWER_TEALA", "FLAG_HIDE_BATTLE_PARK_PALMER", "FLAG_HIDE_BATTLE_PARK_RIVAL", "FLAG_HIDE_BATTLEGROUND_BUCK",
        "FLAG_HIDE_SURVIVAL_AREA_BUCK", "FLAG_HIDE_SURVIVAL_AREA_RIVAL", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_LOOKER", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN",
        "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER",
        "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK", "FLAG_HIDE_DAY_CARE_GYM_GUIDE", "FLAG_HIDE_SOLACEON_TOWN_RIVAL", "FLAG_HIDE_SNOWPOINT_CITY_CANDICE",
        "FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA", "FLAG_HIDE_VALOR_LAKEFRONT_CYNTHIA", "FLAG_HIDE_VALOR_LAKEFRONT_RIVAL", "FLAG_HIDE_VALOR_LAKEFRONT_GRUNT_M",
        "FLAG_HIDE_ROUTE_210_SOUTH_CYNTHIA", "FLAG_HIDE_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_BEAUTY", "FLAG_HIDE_IRON_ISLAND_HOUSE_BYRON",
        "FLAG_HIDE_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_SAILOR_ELDRITCH", "FLAG_HIDE_ROUTE_224_PROF_OAK", "FLAG_HIDE_ROUTE_224_MARLEY",
        "FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM", "FLAG_HIDE_POFFIN_HOUSE_REPORTER",
        "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS", "FLAG_HIDE_MT_CORONET_2F_LOOKER", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F",
        "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT", "FLAG_HIDE_ROUTE_205_SOUTH_YOUNGSTER", "FLAG_HIDE_ROUTE_217_MAYLENE",
        "FLAG_HIDE_SUNYSHORE_CITY_RIVAL", "FLAG_HIDE_FIGHT_AREA_PALMER", "FLAG_HIDE_VILLA_DELIVERY_GUY", "FLAG_HIDE_VILLA_BOOK",
    };
}
