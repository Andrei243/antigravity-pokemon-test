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
/// The eighth chapter's first half (plan 02 · S11, part a, "Lake Acuity and Snowpoint") played through with no screen,
/// in the order a player meets it: the rival at the head of Acuity Lakefront's rock face, Snowpoint City with its
/// temple's guard and Maylene in its Pokémon Center, Candice's Badge and what it changes at the lakefront, and Lake
/// Acuity, where Commander Jupiter has beaten the rival and leaves for Veilstone City. Each scene is started as the
/// game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class SnowpointAndLakeAcuityTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(0) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 48));
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

    /// <summary>The story as Commander Mars's defeat at Lake Verity leaves it (lake_verity.txt, plan 02 · S10).</summary>
    private static void PastLakeVerity(OpeningTests.Game game) =>
        Run(game, "lake_verity", "setflag FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY\nclearflag FLAG_HIDE_LAKE_ACUITY_JUPITER\nsetvar VAR_LAKE_ACUITY_STATE 1\n"
            + "setflag FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M");

    /// <summary>A step onto the rival's trigger at the foot of the rock face, from the south.</summary>
    private static HeadlessScriptHost MeetTheRivalAtTheRockFace(OpeningTests.Game game, int x = 310)
    {
        game.Arrive("Sinnoh", x, 246);
        game.Tile = (x, 244);
        game.Facing = Direction.Up;
        Assert.Same(game.Map.Triggers.First(t => t.Script == "RivalCallsDown" && t.ScriptFile == "acuity_lakefront"), FieldScripts.TriggerAt(game.Map, x, 244, game.Story));
        return game.Play(Scripts.Find("RivalCallsDown", "acuity_lakefront")!);
    }

    /// <summary>Candice beaten, as her Gym's script beats her (SnowpointGym.txt, plan 01 · M9), and out of its door.</summary>
    private static void WinTheIcicleBadge(OpeningTests.Game game)
    {
        game.Arrive("SnowpointGym", 11, 28);
        game.Talk("candice");
        Assert.True(game.Story.HasBadge(Badge.Icicle));
        game.Through("Sinnoh");
    }

    /// <summary>Into the lake Team Galactic has come to, through the lakefront's way in on the west tile or the east.</summary>
    private static HeadlessScriptHost IntoTheLake(OpeningTests.Game game, int x = 14)
    {
        game.Arrive("Sinnoh", x == 14 ? 308 : 309, 230);
        var way = game.Map.GetWarpAt(x == 14 ? 308 : 309, 229)!;
        Assert.Equal(("LakeAcuityGalactic", x, 49), (way.TargetMap, way.TargetX, way.TargetY));
        game.Facing = Direction.Up;
        game.Arrive(way.TargetMap, way.TargetX, way.TargetY);
        return game.Last;
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameStandsTheChaptersPeopleWhereTheOriginalDoes()
    {
        var game = NewGame();
        // Acuity Lakefront: the rival at the head of the rock face, his trigger waiting at its foot, and the way in
        // leading to the lake as it is before Team Galactic comes
        game.Arrive("Sinnoh", 310, 246);
        Assert.Empty(game.Last.Transcript);
        var rival = game.Present("rival", "acuity_lakefront")!;
        Assert.Equal((310, 237, Direction.Down), (rival.GridX, rival.GridY, rival.Facing));
        Assert.True(game.Fires("RivalCallsDown"));
        Assert.Equal("LakeAcuity", game.Map.GetWarpAt(308, 229)!.TargetMap);
        Assert.Equal("LakeAcuity", game.Map.GetWarpAt(309, 229)!.TargetMap);

        // The lake as it is: nobody by the water yet (the rival comes with the news of the explosion)
        game.Through("LakeAcuity");
        Assert.Equal(49, game.Tile.Y);
        Assert.Null(game.Present("rival"));

        // Snowpoint City: the temple's guard before its east door, which is shut for good, and the trigger before its
        // west one, which turns everyone back until the Hall of Fame (plan 02 · S15, part b opened the door behind it);
        // the sailor on the quay; Candice and Cynthia wait for the Hall of Fame
        game.Arrive("Sinnoh", 366, 200);
        var guard = game.Present("temple_guard", "snowpoint_city")!;
        Assert.Equal((367, 198, Direction.Down), (guard.GridX, guard.GridY, guard.Facing));
        Assert.Null(game.Map.GetWarpAt(367, 197));
        Assert.True(game.Map.IsSolid(367, 197));
        Assert.Equal("SnowpointTemple1F", game.Map.GetWarpAt(366, 197)?.TargetMap);
        Assert.True(game.Fires("TempleGuard"));
        Assert.Equal((366, 198), game.Map.Triggers.Where(t => t.Script == "TempleGuard").Select(t => (t.X, t.Y)).Single());
        Assert.NotNull(game.Present("sailor_ss_spiral", "snowpoint_city"));
        // (Cynthia is on the quay's map since plan 02 · S15, part a, and Candice down the street from the temple since
        // part b, each behind her flag)
        Assert.Null(game.Present("candice", "snowpoint_city"));
        Assert.Null(game.Present("cynthia", "snowpoint_city"));
        foreach (string flag in new[] { "FLAG_HIDE_SNOWPOINT_CITY_CANDICE", "FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA", "FLAG_HIDE_ACUITY_CAVERN_UXIE" })
            Assert.True(game.Story.Has(flag), flag);

        // Its Pokémon Center: Maylene by the counter, with the town's people
        game.Through("SnowpointPokemonCenter");
        var maylene = game.Present("maylene")!;
        // (on the original's tile before the counter since the room was rebuilt to its plan: plan 01 · M11)
        Assert.Equal((7, 6, Direction.Up), (maylene.GridX, maylene.GridY, maylene.Facing));
        foreach (string key in new[] { "lass", "battle_girl", "psyduck" }) Assert.NotNull(game.Present(key));
        Assert.Equal("Psyduck", game.Present("psyduck")!.Species);
    }

    // ------------------------------------------------------------------ Acuity Lakefront

    [Theory]
    [InlineData(310)]
    [InlineData(311)]
    public void TheRivalCallsDownFromTheRockFaceAndRunsOnToTheLake(int x)
    {
        var game = NewGame();
        var call = MeetTheRivalAtTheRockFace(game, x);
        Assert.Contains("camera Pan 310 238", call.Log);
        Assert.Contains("camera Release", call.Log);
        Assert.All(call.Transcript, l => Assert.Equal(OpeningTests.Game.Rival, l.Speaker));
        Assert.Contains(call.Transcript, l => l.Text.Contains("Badge from Snowpoint's Gym"));
        Assert.Contains(call.Transcript, l => l.Text.Contains("Team Galactic"));
        // He has gone on to the lake, for good, and the player stands where they stood
        Assert.Null(game.Present("rival", "acuity_lakefront"));
        Assert.True(game.Story.Has(StoryMigration.LakefrontRivalGoneFlag));
        Assert.Equal(1, game.Story.Var("VAR_ACUITY_LAKEFRONT_STATE"));
        Assert.Equal((x, 244), game.Tile);
        Assert.False(game.Fires("RivalCallsDown"));
        game.Arrive("Sinnoh", 310, 246);
        Assert.Null(game.Present("rival", "acuity_lakefront"));
    }

    /// <summary>The tiles of Acuity Lakefront walked to on foot from a tile of it, never stepping onto the tiles given.</summary>
    private static HashSet<(int X, int Y)> WalkedOnTheLakefront(Map map, (int X, int Y) from, params (int X, int Y)[] shut)
    {
        var seen = new HashSet<(int, int)> { from };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, x, y, way, new Walker(TravelMode.OnFoot, map.HeightAt(x, y)));
                if (!step.Moves || shut.Contains((step.X, step.Y)) || map.AreaAt(step.X, step.Y)?.Key != "acuity_lakefront") continue;
                if (seen.Add((step.X, step.Y))) queue.Enqueue((step.X, step.Y));
            }
        }
        return seen;
    }

    [Fact]
    public void TheRoadFromRoute217ToSnowpointCrossesTheRivalsTrigger()
    {
        var game = NewGame();
        var map = game.Maps["Sinnoh"];
        var trigger = map.Triggers.First(t => t.Script == "RivalCallsDown" && t.ScriptFile == "acuity_lakefront");
        Assert.Equal((310, 244, 2, 1), (trigger.X, trigger.Y, trigger.Width, trigger.Depth));
        Assert.Equal("acuity_lakefront", map.AreaAt(310, 254)!.Key);

        // From the lakefront's south end, where Route 217 comes up: the east end, where Snowpoint City lies, is walked
        // to only over the trigger's two tiles, and so is the foot of the rock face
        var open = WalkedOnTheLakefront(map, (310, 254));
        Assert.Contains(open, t => t.X >= 345);
        Assert.Contains((310, 242), open);
        var shut = WalkedOnTheLakefront(map, (310, 254), (310, 244), (311, 244));
        Assert.DoesNotContain(shut, t => t.X >= 320);
        Assert.DoesNotContain((310, 242), shut);
    }

    [Fact]
    public void TheRockFaceIsClimbedOnlyWithRockClimbWhichWaitsForTheIcicleBadge()
    {
        var game = NewGame();
        MeetTheRivalAtTheRockFace(game);
        var map = game.Map;
        float foot = map.HeightAt(310, 242);
        Assert.Equal(TileBehavior.RockClimbNorthSouth, map.BehaviourAt(310, 241));
        // On foot the face is a wall, and so it is to someone who knows the move and wasn't asked
        Assert.False(FieldMovement.Step(map, 310, 242, Direction.Up, new Walker(TravelMode.OnFoot, foot)).Moves);
        Assert.Equal(Obstacle.RockFace, FieldMovement.Step(map, 310, 242, Direction.Up, new Walker(TravelMode.OnFoot, foot, Moves: FieldMoves.RockClimb)).Obstacle);
        // A climb said yes to ends at the head of the face, where the rival stood
        var climb = FieldMovement.Step(map, 310, 242, Direction.Up, new Walker(TravelMode.OnFoot, foot, Moves: FieldMoves.RockClimb, Climbing: true));
        Assert.Equal((StepKind.Climb, 310, 237), (climb.Kind, climb.X, climb.Y));
        Assert.Equal(Badge.Icicle, FieldMoveRules.BadgeFor(FieldMove.RockClimb));

        // The face's own script, which is the common one here too: no Badge, no climb
        Assert.Equal(Scripts.Find(FieldScripts.RockFace)!.FullName, Scripts.Find("RockFace", "acuity_lakefront")!.FullName);
        var torterra = game.Party.Members[0];
        torterra.Moves.Clear();
        torterra.Moves.Add(new Move(MoveDatabase.Get("Rock Climb")!));
        game.Tile = (310, 242);
        game.Facing = Direction.Up;
        var refused = game.Play(Scripts.Find(FieldScripts.RockFace)!);
        Assert.Empty(refused.Asked);
        game.Story.GiveBadge(Badge.Icicle);
        var asked = game.Play(Scripts.Find(FieldScripts.RockFace)!);
        Assert.Contains(asked.Asked, a => a.Question.Contains("Rock Climb"));
    }

    // ------------------------------------------------------------------ Snowpoint City

    [Fact]
    public void TheTemplesGuardTurnsThePlayerBackFromItsDoor()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 366, 200);
        game.Facing = Direction.Up;
        var turned = game.Step("TempleGuard");
        Assert.Contains(turned.Transcript, l => l.Speaker == "Temple Guard" && l.Text.Contains("chosen"));
        Assert.Equal((366, 199), turned.PlayerTile);
        Assert.Equal(Direction.Down, game.Present("temple_guard", "snowpoint_city")!.Facing);
        // Every time: nothing lets the player in before the Hall of Fame
        Assert.Equal(0, game.Story.Var("VAR_SNOWPOINT_CITY_STATE"));
        Assert.True(game.Fires("TempleGuard"));
        Assert.Contains(game.Talk("temple_guard", "snowpoint_city").Transcript, l => l.Text.Contains("cannot let you pass"));
    }

    [Fact]
    public void SnowpointsPeopleSayTheirPieceAndTheSailorSailsNobodyYet()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 366, 240);
        var sailor = game.Talk("sailor_ss_spiral", "snowpoint_city");
        Assert.Contains(sailor.Transcript, l => l.Text.Contains("Pokémon League"));
        Assert.Empty(sailor.Asked);
        Assert.DoesNotContain(sailor.Log, l => l.StartsWith("warp"));
        foreach (string key in new[] { "ace_trainer_snow_f", "snowpoint_npc_f_1", "snowpoint_npc_f_2", "snowpoint_npc_m", "sailor" })
            Assert.NotEmpty(game.Talk(key, "snowpoint_city").Transcript);

        // The Mart's two customers, and the Pokémon Center's
        game.Through("SnowpointPokeMart");
        Assert.Contains(game.Talk("youngster").Transcript, l => l.Text.Contains("Elite Four"));
        Assert.Contains(game.Talk("snowpoint_lady").Transcript, l => l.Text.Contains("Ice Heals"));
        game.Through("Sinnoh");
        game.Through("SnowpointPokemonCenter");
        Assert.Contains(game.Talk("lass").Transcript, l => l.Text.Contains("temple"));
        Assert.Contains(game.Talk("battle_girl").Transcript, l => l.Text.Contains("quiet"));
        Assert.Contains("cry Psyduck", game.Talk("psyduck").Log);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Right)]
    public void MayleneThawsOutInThePokemonCenterUntilTheBadgeAndTheLakefrontSendHerHome(Direction facing)
    {
        var game = NewGame();
        // Route 217 took her out of her own Gym (route_217.txt, plan 02 · S10)
        Run(game, "route_217", "setflag FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE\nsetvar VAR_ROUTE_217_STATE 1");
        game.Arrive("VeilstoneGym", 12, 30);
        Assert.Null(game.Present("maylene"));

        game.Arrive("SnowpointPokemonCenter", 8, 12);
        var before = TalkFrom(game, "maylene", null, facing);
        Assert.Contains(before.Transcript, l => l.Speaker == "Maylene" && l.Text.Contains("Candice uses Ice-type"));
        Assert.Contains(before.Transcript, l => l.Text.Contains("Fighting"));
        Assert.Equal(FieldMovement.Opposite(facing), game.Present("maylene")!.Facing);

        // The lakefront without the Badge changes nothing
        game.Arrive("Sinnoh", 310, 246);
        Assert.False(game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_POKECENTER_1F_MAYLENE"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE"));

        // With the Badge she has something else to say, and is still there
        WinTheIcicleBadge(game);
        Assert.Equal((367, 223), game.Tile);
        game.Through("SnowpointPokemonCenter");
        var after = TalkFrom(game, "maylene", null, facing);
        Assert.Contains(after.Transcript, l => l.Text.Contains("Icicle Badge"));
        Assert.Contains(after.Transcript, l => l.Text.Contains("Rock Climb"));

        // Coming to Acuity Lakefront with the Badge sends her home (the original's OnTransition)
        game.Arrive("Sinnoh", 318, 246);
        Assert.True(game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_POKECENTER_1F_MAYLENE"));
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE"));
        game.Arrive("SnowpointPokemonCenter", 8, 12);
        Assert.Null(game.Present("maylene"));
        game.Arrive("VeilstoneGym", 12, 30);
        Assert.NotNull(game.Present("maylene"));
    }

    // ------------------------------------------------------------------ Lake Acuity

    [Fact]
    public void UntilMarsIsBeatenTheLakefrontLeadsToTheLakeAsItWasWhereTheRivalKeepsWatch()
    {
        var game = NewGame();
        var sinnoh = game.Maps["Sinnoh"];
        // Two ways on each tile, each by the original's flag
        foreach (int x in new[] { 308, 309 })
        {
            var ways = sinnoh.WarpsAt(x, 229).ToList();
            Assert.Equal(new[] { "LakeAcuity", "LakeAcuityGalactic" }, ways.Select(w => w.TargetMap));
            Assert.Equal(("FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", (string?)null), (ways[0].Until, ways[0].From));
            Assert.Equal(((string?)null, "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY"), (ways[1].Until, ways[1].From));
        }

        // The news in Canalave sends the rival to the lake (canalave_city.txt clears his flag)
        Run(game, "canalave_city", "clearflag FLAG_HIDE_LAKE_ACUITY_LOW_WATER_RIVAL");
        game.Arrive("Sinnoh", 308, 230);
        game.Through("LakeAcuity");
        Assert.Equal((14, 49), game.Tile);
        var rival = game.Present("rival")!;
        Assert.Equal((16, 46), (rival.GridX, rival.GridY));
        var first = game.Talk("rival");
        Assert.Contains(first.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("nothing going on"));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_LAKE_ACUITY_LOW_WATER_RIVAL"));
        Assert.Contains(game.Talk("rival").Transcript, l => l.Text.Contains("Sunyshore"));
        // Its TM lies beyond cliffs on this map
        Assert.DoesNotContain(game.Map.Everyone, n => n.IsItemBall);
        game.Through("Sinnoh");
        Assert.Equal((308, 230), game.Tile);

        // Mars beaten: the same tiles lead to the lake Team Galactic has come to
        PastLakeVerity(game);
        game.Arrive("Sinnoh", 308, 230);
        Assert.Equal("LakeAcuityGalactic", game.Map.GetWarpAt(308, 229)!.TargetMap);
        Assert.Equal("LakeAcuityGalactic", game.Map.GetWarpAt(309, 229)!.TargetMap);
    }

    [Theory]
    [InlineData(14, PlayerLook.Boy)]
    [InlineData(15, PlayerLook.Girl)]
    public void AtLakeAcuityJupiterHasBeatenTheRivalAndLeavesForVeilstone(int x, PlayerLook look)
    {
        var game = NewGame(look);
        // As the chapter before leaves the story: the lake's news in Canalave, the professor and the assistant at Lake Verity
        Run(game, "canalave_city", "setflag FLAG_LAKE_VALOR_EXPLODED\nclearflag FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN\nclearflag FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART");
        PastLakeVerity(game);
        game.Story.GiveBadge(Badge.Icicle);

        var scene = IntoTheLake(game, x);
        // Nobody battles: she has won already
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("battle"));
        Assert.Contains("camera Pan 16 45", scene.Log);
        Assert.Contains("camera Release", scene.Log);
        var said = scene.Transcript;
        Assert.Equal(OpeningTests.Game.Rival, said[0].Speaker);
        Assert.Contains("Team Galactic", said[0].Text);
        Assert.Contains(said, l => l.Speaker == "Jupiter" && l.Text.Contains("Back to headquarters in Veilstone"));
        Assert.Contains(said, l => l.Speaker == "Jupiter" && l.Text.Contains("Eterna City"));
        Assert.Contains(said, l => l.Speaker == "Jupiter" && l.Text.Contains("don't waste your time coming to our headquarters"));
        Assert.Contains(said, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Uxie"));
        Assert.Contains(said, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("get tougher"));
        // Jupiter speaks to the rival before she speaks to the player, and the rival last of all
        int veilstone = said.FindIndex(l => l.Text.Contains("Back to headquarters"));
        int eterna = said.FindIndex(l => l.Text.Contains("Eterna City"));
        int uxie = said.FindIndex(l => l.Text.Contains("Uxie"));
        Assert.True(veilstone < eterna && eterna < uxie);
        Assert.Equal(OpeningTests.Game.Rival, said[^1].Speaker);

        // Both have gone, for good; the player stands beside where the rival stood, looking after him
        Assert.Null(game.Present("jupiter"));
        Assert.Null(game.Present("rival"));
        Assert.Equal((16, 45), game.Tile);
        Assert.Equal(Direction.Down, scene.PlayerFacing);
        Assert.Equal(2, game.Story.Var("VAR_LAKE_ACUITY_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_RIVAL", "FLAG_ALT_MUSIC_LAKE_ACUITY",
            "FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_COUNTERPART", "FLAG_HIDE_LAKE_VERITY_PROF_ROWAN" })
            Assert.True(game.Story.Has(flag), flag);
        // What opens Team Galactic's headquarters in Veilstone, and the rest the original clears
        foreach (string flag in new[] { "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY", "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN", "FLAG_LAKE_VALOR_EXPLODED" })
            Assert.False(game.Story.Has(flag), flag);

        // Coming again, the shore is empty and nothing plays; the lake's TM lies on its far shore, and the cavern on the
        // island is open (plan 02 · S13), with nobody in it until the professor has heard of the Distortion World
        game.Arrive("LakeAcuityGalactic", x, 49);
        Assert.Empty(game.Last.Transcript);
        Assert.Null(game.Present("jupiter"));
        Assert.Contains(game.Map.NPCs, n => n.IsItemBall && n.Item == "TM14");
        Assert.Equal("AcuityCavern", game.Map.GetWarpAt(32, 32)!.TargetMap);
        Assert.True(game.Story.Has("FLAG_HIDE_ACUITY_CAVERN_UXIE"));
        var back = game.Map.GetWarpAt(x, 50)!;
        Assert.Equal(("Sinnoh", x == 14 ? 308 : 309, 230), (back.TargetMap, back.TargetX, back.TargetY));

        // The professor and the assistant have gone home from Lake Verity, on both of its maps
        game.Arrive("LakeVerityGalactic", 46, 53);
        Assert.Null(game.Present("prof_rowan"));
        Assert.Null(game.Present("counterpart"));
        game.Arrive("LakeVerity", 46, 53);
        Assert.Null(game.Present("prof_rowan"));
        Assert.Null(game.Present("counterpart"));
        // And Canalave City no longer talks of the tremor
        game.Arrive("Sinnoh", 37, 722);
        Assert.DoesNotContain(game.Talk("collector", "canalave_city").Transcript, l => l.Text.Contains("tremor"));
    }

    [Fact]
    public void TheLakesScenePlaysOnlyInItsState()
    {
        var game = NewGame();
        // Someone put on the lake's shore before Mars is beaten (no way leads there in a game of today) sees the two
        // of them standing, and nothing plays
        game.Story.Unset("FLAG_HIDE_LAKE_ACUITY_JUPITER");
        game.Arrive("LakeAcuityGalactic", 14, 49);
        Assert.Empty(game.Last.Transcript);
        Assert.Equal(0, game.Story.Var("VAR_LAKE_ACUITY_STATE"));
        var jupiter = game.Present("jupiter")!;
        var rival = game.Present("rival")!;
        Assert.Equal((17, 44, Direction.Down), (jupiter.GridX, jupiter.GridY, jupiter.Facing));
        Assert.Equal((17, 45, Direction.Up), (rival.GridX, rival.GridY, rival.Facing));
    }

    // ------------------------------------------------------------------ the half-chapter in order

    [Fact]
    public void TheHalfChapterPlaysThroughFromRoute217ToTheEndOfTheLakesScene()
    {
        var game = NewGame();
        // Up Route 217: Maylene catches the player up (plan 02 · S10)
        PastLakeVerity(game);
        game.Arrive("Sinnoh", 303, 380);
        game.Tile = (302, 376);
        game.Facing = Direction.Up;
        game.Play(Scripts.Find("Maylene", "route_217")!);
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE"));

        // Acuity Lakefront: the rival calls down and runs on; the way in leads to the lake Team Galactic has come to,
        // but the rock face keeps it shut
        MeetTheRivalAtTheRockFace(game);
        Assert.Equal("LakeAcuityGalactic", game.Map.GetWarpAt(308, 229)!.TargetMap);
        Assert.Equal(1, game.Story.Var("VAR_LAKE_ACUITY_STATE"));

        // Snowpoint City: the guard, Maylene, and Candice's Badge
        game.Arrive("Sinnoh", 366, 200);
        game.Step("TempleGuard");
        game.Arrive("Sinnoh", 379, 234);
        game.Through("SnowpointPokemonCenter");
        Assert.Contains(game.Talk("maylene").Transcript, l => l.Text.Contains("Candice"));
        game.Through("Sinnoh");
        WinTheIcicleBadge(game);
        Assert.True(game.Story.Has("FLAG_RECEIVED_CANDICE_TM72"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS"));

        // Back at the lakefront Maylene goes home; up the rock face and into the lake
        game.Arrive("Sinnoh", 318, 246);
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE"));
        var scene = IntoTheLake(game);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Jupiter");
        Assert.Equal(2, game.Story.Var("VAR_LAKE_ACUITY_STATE"));
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY"));

        // Nothing of the half-chapter starts again
        game.Through("Sinnoh");
        game.Arrive("Sinnoh", 310, 246);
        Assert.False(game.Fires("RivalCallsDown"));
        game.Arrive("LakeAcuityGalactic", 14, 49);
        Assert.Empty(game.Last.Transcript);
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
        // The rival's trigger, the foot and the head of the rock face, the way into the lake
        Assert.Contains((310, 244), Of("Sinnoh"));
        Assert.Contains((310, 242), Of("Sinnoh"));
        Assert.Contains((310, 237), Of("Sinnoh"));
        Assert.Contains((308, 230), Of("Sinnoh"));
        // Both lakes' shores, and the far shore where the TM lies, which the water leads to on the later map only
        Assert.Contains((16, 46), Of("LakeAcuity"));
        Assert.Contains((16, 45), Of("LakeAcuityGalactic"));
        var ball = MapDatabase.Get("LakeAcuityGalactic").Everyone.Single(n => n.IsItemBall);
        Assert.Equal("TM14", ball.Item);
        Assert.Contains(Of("LakeAcuityGalactic"), t => Math.Abs(t.X - ball.GridX) + Math.Abs(t.Y - ball.GridY) == 1);
        // Snowpoint City: the temple's door, the Gym's, the quay, and Maylene's corner of the Pokémon Center
        Assert.Contains((366, 198), Of("Sinnoh"));
        Assert.Contains((367, 223), Of("Sinnoh"));
        Assert.Contains((356, 246), Of("Sinnoh"));
        Assert.Contains((8, 6), Of("SnowpointPokemonCenter"));
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKnowsWhereItHasBeen()
    {
        string[] always = { "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_SNOWPOINT_CITY_CANDICE", "FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA" };

        var before = new StoryState();
        StoryMigration.Upgrade(before, 12, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in always) Assert.True(before.Has(flag), flag);
        // Not yet north of the lakefront: the rival still waits at the head of the rock face
        Assert.False(before.Has(StoryMigration.LakefrontRivalGoneFlag));
        Assert.Equal(0, before.Var("VAR_ACUITY_LAKEFRONT_STATE"));

        // Been to Snowpoint City already, or won its Badge: it walked under the rock face when nobody stood there
        foreach (bool byBadge in new[] { false, true })
        {
            var north = new StoryState();
            if (byBadge) north.GiveBadge(Badge.Icicle);
            else north.Set(StoryMigration.ArrivedInSnowpointFlag);
            StoryMigration.Upgrade(north, 12, Array.Empty<Pokemon>(), Scripts, new Inventory());
            foreach (string flag in always) Assert.True(north.Has(flag), flag);
            Assert.True(north.Has(StoryMigration.LakefrontRivalGoneFlag));
            Assert.Equal(1, north.Var("VAR_ACUITY_LAKEFRONT_STATE"));
        }

        // A save of today is left as it is
        var today = new StoryState();
        today.Set(StoryMigration.ArrivedInSnowpointFlag);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(today.Has(StoryMigration.LakefrontRivalGoneFlag));
        Assert.True(StoryState.CurrentVersion >= 13);
    }

    [Fact]
    public void ASaveThatHadBeenToSnowpointFindsTheRockFaceClearAndTheSceneAtTheLake()
    {
        var game = NewGame();
        game.Story.Set(StoryMigration.ArrivedInSnowpointFlag);
        game.Story.GiveBadge(Badge.Icicle);
        StoryMigration.Upgrade(game.Story, 12, Array.Empty<Pokemon>(), Scripts, new Inventory());
        game.Arrive("Sinnoh", 318, 246);
        Assert.Null(game.Present("rival", "acuity_lakefront"));
        Assert.Null(game.Map.GetNpcAt(310, 237));
        Assert.False(game.Fires("RivalCallsDown"));
        // The lake's scene still waits for Mars's defeat, and plays then
        PastLakeVerity(game);
        Assert.Contains(IntoTheLake(game).Transcript, l => l.Speaker == "Jupiter");
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_SNOWPOINT_CITY_CANDICE", "FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA", "FLAG_HIDE_LAKE_ACUITY_JUPITER" })
        {
            Assert.True(fresh.Has(flag), flag);
            Assert.True(upgraded.Has(flag), flag);
        }
        // The rival at the lakefront and at the lake, and Maylene in the Pokémon Center, are there from the start
        foreach (string flag in new[] { StoryMigration.LakefrontRivalGoneFlag, "FLAG_HIDE_LAKE_ACUITY_RIVAL", "FLAG_HIDE_SNOWPOINT_CITY_POKECENTER_1F_MAYLENE" })
        {
            Assert.False(fresh.Has(flag), flag);
            Assert.False(upgraded.Has(flag), flag);
        }
    }
}
