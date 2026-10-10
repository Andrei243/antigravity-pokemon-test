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
/// The fifth chapter (plan 02 · S8, "Veilstone and Pastoria") played through with no screen, in the order a player meets
/// it: Route 215's black belt, the assistant and Crasher Wake before the Veilstone Gym, the warehouse's guards, the
/// assistant's call for help once Maylene's Badge is won, the tag battle by the warehouse, Looker and HM02 inside it, the
/// rival in Pastoria City's Gym doorway and his battle, Crasher Wake and the rival as the player comes out with the Fen
/// Badge, the bomb at the Great Marsh, the grunt's flight along Route 213 to Valor Lakefront, Cynthia's Secret Potion, the
/// road to Sunyshore shut and the television crew at the gate to Route 212. The Badges are won as the Gyms' own scripts
/// give them (VeilstoneGym.txt, PastoriaGym.txt: plan 01 · M9's).
/// </summary>
public class VeilstoneAndPastoriaTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won, int starter = 0)
    {
        var game = new OpeningTests.Game(starter, fight);
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        var leader = new Pokemon(PokemonDatabase.Get(new[] { "Grotle", "Monferno", "Prinplup" }[starter])!, 36);
        game.Party.Add(leader);
        return game;
    }

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, string? place, Direction facing)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        return game.Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name)!, npc);
    }

    /// <summary>Maylene beaten, as her Gym's script beats her (VeilstoneGym.txt), and out of its door.</summary>
    private static void WinTheCobbleBadge(OpeningTests.Game game)
    {
        game.Arrive("VeilstoneGym", 12, 30);
        game.Talk("maylene");
        Assert.True(game.Story.HasBadge(Badge.Cobble));
        game.Through("Sinnoh");
    }

    /// <summary>Crasher Wake beaten, as his Gym's script beats him (PastoriaGym.txt), and out of its door.</summary>
    private static void WinTheFenBadge(OpeningTests.Game game)
    {
        game.Arrive("PastoriaGym", 13, 41);
        game.Talk("crasher_wake");
        Assert.True(game.Story.HasBadge(Badge.Fen));
        game.Through("Sinnoh");
    }

    /// <summary>Veilstone's chapter played to the warehouse's HM02, the assistant spoken to from the west.</summary>
    private static void ThroughTheWarehouse(OpeningTests.Game game)
    {
        game.Arrive("Sinnoh", 682, 620);
        game.Step("CrasherWake");
        WinTheCobbleBadge(game);
        TalkFrom(game, "counterpart", "veilstone_city", Direction.Right);
        Assert.Equal("VeilstoneGalacticWarehouse", game.Map.Name);
        PickUp(game, "HM02");
    }

    private static void PickUp(OpeningTests.Game game, string item)
    {
        var ball = game.Map.NPCs.First(n => n.IsItemBall && n.Item == item);
        game.Tile = (ball.GridX, ball.GridY + 1);
        game.Play(Scripts.Find(FieldScripts.For(ball)!)!, ball);
    }

    // ------------------------------------------------------------------ before the chapter's scenes

    [Fact]
    public void ANewGameKeepsTheChaptersPeopleOutOfSightUntilTheirScenes()
    {
        var game = NewGame();
        // Veilstone: the assistant waits before the Gym, its guards by the warehouse; nobody else of the chapter's scenes
        game.Arrive("Sinnoh", 682, 620);
        var assistant = game.Present("counterpart", "veilstone_city")!;
        Assert.Equal((682, 614), (assistant.GridX, assistant.GridY));
        Assert.Equal("Assistant", assistant.NpcType);
        Assert.Null(game.Present("crasher_wake", "veilstone_city"));
        Assert.Null(game.Present("looker", "veilstone_city"));
        Assert.Null(game.Present("grunt_m_storage_key", "veilstone_city"));
        Assert.NotNull(game.Present("grunt_m_warehouse_north", "veilstone_city"));
        Assert.NotNull(game.Present("grunt_m_warehouse_south", "veilstone_city"));
        Assert.NotNull(game.Present("grunt_m_southeast", "veilstone_city"));
        Assert.True(game.Fires("CrasherWake"));
        Assert.True(game.Fires("GruntsBlock"));
        Assert.Equal("VeilstoneGalacticWarehouse", game.Map.GetWarpAt(701, 591)!.TargetMap);

        // Pastoria: the rival stands in the Gym's doorway while Crasher Wake is away; Team Galactic's grunt waits
        game.Arrive("Sinnoh", 600, 820);
        var rival = game.Present("rival", "pastoria_city")!;
        Assert.Equal((589, 828), (rival.GridX, rival.GridY));
        Assert.Null(game.Present("crasher_wake", "pastoria_city"));
        Assert.Null(game.Present("croagunk", "pastoria_city"));
        Assert.NotNull(game.Present("grunt_m", "pastoria_city"));
        Assert.Contains(game.Talk("rival", "pastoria_city").Transcript, l => l.Text.Contains("Veilstone"));
        Assert.False(game.Fires("RivalBattle"));

        // Route 213 and Valor Lakefront: the grunt, Looker, Cynthia and the rival come with their scenes
        game.Arrive("Sinnoh", 660, 815);
        Assert.Null(game.Present("grunt_m", "route_213"));
        Assert.Null(game.Present("looker", "route_213"));
        game.Arrive("Sinnoh", 720, 788);
        Assert.Null(game.Present("grunt_m", "valor_lakefront"));
        Assert.Null(game.Present("cynthia", "valor_lakefront"));
        Assert.Null(game.Present("rival", "valor_lakefront"));
        Assert.NotNull(game.Present("collector", "valor_lakefront"));
        Assert.True(game.Fires("BlockSunyshore"));
    }

    [Fact]
    public void TheBlackBeltOnRoute215GivesTM66Once()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 611, 600);
        game.Talk("black_belt", "route_215");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM66")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ROUTE_215_TM66"));
        var again = game.Talk("black_belt", "route_215");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM66")!));
        Assert.Contains("twice as hard", again.Transcript.Single().Text);
    }

    [Fact]
    public void AManInVeilstoneGivesTM63Once()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 687, 626);
        game.Talk("roughneck_2", "veilstone_city");
        game.Talk("roughneck_2", "veilstone_city");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM63")!));
        Assert.Contains(game.Said, l => l.Text.Contains("Embargo"));
        // The meteorites at the east end of town are read
        Assert.Equal(4, game.Map.TileScripts.Count(kv => kv.Value == "Meteorite"));
        Assert.Contains("stars", game.Play(Scripts.Find("Meteorite", "veilstone_city")!).Transcript.Single().Text);
    }

    // ------------------------------------------------------------------ Veilstone City

    [Theory]
    [InlineData(681)]
    [InlineData(682)]
    [InlineData(683)]
    [InlineData(684)]
    public void CrasherWakeComesOutOfTheVeilstoneGymAsThePlayerComesUpToIt(int x)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", x, 620);
        game.Tile = (x, 616);
        var scene = game.Play(Scripts.Find("CrasherWake", "veilstone_city")!);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Crasher Wake" && l.Text.Contains("Pastoria Gym"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "{assistant}" || l.Speaker == "Dawn");
        Assert.Equal(1, game.Story.Var("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE"));
        // Both go, and for good: her flag hides her until Maylene's Badge brings her back
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_COUNTERPART"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CRASHER_WAKE"));
        Assert.Null(game.Present("counterpart", "veilstone_city"));
        Assert.Null(game.Present("crasher_wake", "veilstone_city"));
        Assert.False(game.Fires("CrasherWake"));
    }

    [Fact]
    public void TheWarehousesGuardsPushThePlayerBackUntilMaylenesBadge()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 690, 596);
        var pushed = game.Step("GruntsBlock");
        Assert.Equal((696, 596), game.Tile);
        Assert.Contains(pushed.Transcript, l => l.Text.Contains("warehouse"));
        Assert.True(game.Fires("GruntsBlock"));
        var north = game.Present("grunt_m_warehouse_north", "veilstone_city")!;
        Assert.Equal((697, 595), (north.GridX, north.GridY));
        Assert.Contains("No kids allowed", game.Talk("grunt_m_warehouse_north", "veilstone_city").Transcript.Single().Text);

        game.Arrive("Sinnoh", 682, 620);
        game.Step("CrasherWake");
        WinTheCobbleBadge(game);
        Assert.False(game.Fires("GruntsBlock"));
        Assert.Contains(game.Talk("grunt_m_warehouse_south", "veilstone_city").Transcript, l => l.Text.Contains("Two on two"));
    }

    [Fact]
    public void TheChapterPlaysThroughVeilstoneToHm02()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 682, 620);
        game.Step("CrasherWake");

        // Maylene's Badge: the assistant comes running to the Gym's door, and goes to wait by the warehouse
        WinTheCobbleBadge(game);
        Assert.Equal((684, 612), game.Tile);
        Assert.Contains(game.Said, l => l.Text.Contains("Pokédex"));
        Assert.Contains("music common/assistant", game.Last.Log);
        Assert.Equal(2, game.Story.Var("VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE"));
        var assistant = game.Present("counterpart", "veilstone_city")!;
        Assert.Equal((696, 596), (assistant.GridX, assistant.GridY));
        // And there she waits, coming back to the city
        game.Arrive("Sinnoh", 690, 600);
        Assert.Equal((696, 596), (assistant.GridX, assistant.GridY));

        // Not yet: she waits
        game.Answers.Enqueue(1);
        var later = TalkFrom(game, "counterpart", "veilstone_city", Direction.Right);
        Assert.Contains(later.Transcript, l => l.Text.Contains("wait here"));
        Assert.Equal(2, game.Story.Var("VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE"));

        // Together: the guards battle two on two beside the assistant, run into the warehouse, and Looker takes the
        // player in after them
        var together = TalkFrom(game, "counterpart", "veilstone_city", Direction.Right);
        Assert.Contains("battle galactic_grunt_veilstone_city_1 and galactic_grunt_veilstone_city_2 with dawn_veilstone_city_turtwig Won", together.Log);
        Assert.Contains(together.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("warehouse"));
        Assert.Contains(together.Transcript, l => l.Text.Contains("Pokémon Mansion"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_COUNTERPART"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_LOOKER"));
        Assert.True(game.Story.Has("FLAG_HIDE_PASTORIA_CITY_RIVAL"));
        Assert.Contains("warp VeilstoneGalacticWarehouse 8 11", together.Log);

        // Inside: the way on is locked; Looker finds HM02 and tells the player to keep it
        Assert.Equal("VeilstoneGalacticWarehouse", game.Map.Name);
        var inside = game.Last;
        Assert.Contains(inside.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("Fly"));
        Assert.Contains(inside.Transcript, l => l.Text.Contains("Pastoria City"));
        Assert.Equal(1, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        Assert.Equal(2, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        PickUp(game, "HM02");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM02")!));
        Assert.True(game.Story.Has("FLAG_OBTAINED_VEILSTONE_CITY_GALACTIC_WAREHOUSE_HM02"));
        Assert.DoesNotContain(game.Map.NPCs, n => n.IsItemBall && n.Item == "HM02");
        // Looker waits by the crates, with what he overheard; the Dusk Stone lies behind the locked door
        game.Arrive("VeilstoneGalacticWarehouse", 8, 11);
        var looker = game.Present("looker")!;
        Assert.Equal((12, 8), (looker.GridX, looker.GridY));
        Assert.Contains("Pastoria City", game.Talk("looker").Transcript[0].Text);
        Assert.True(game.Map.IsSolid(8, 7) && game.Map.IsSolid(9, 7));
        Assert.Contains("special key", game.Map.GetSignboardAt(8, 7));

        // Out again: the guards and the assistant are gone, and the way to the warehouse is open
        game.Through("Sinnoh");
        Assert.Equal((701, 592), game.Tile);
        Assert.Null(game.Present("grunt_m_warehouse_north", "veilstone_city"));
        Assert.Null(game.Present("grunt_m_southeast", "veilstone_city"));
        Assert.Null(game.Present("counterpart", "veilstone_city"));
        Assert.False(game.Fires("GruntsBlock"));
    }

    // From the west, the north or the south of her (east of her is the gap between the guards): every walk of the
    // scene is on open ground, or the game's host would say so
    [Theory]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Up)]
    public void TheAssistantAndThePlayerLineUpBeforeTheGuardsFromEachSide(Direction facing)
    {
        var game = NewGame(starter: 1);
        game.Look = PlayerLook.Girl;
        game.Story.SetVar("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE", 1);
        WinTheCobbleBadge(game);
        var scene = TalkFrom(game, "counterpart", "veilstone_city", facing);
        // The girl's partner is Lucas, with the team strong against nobody's starter
        Assert.Contains("battle galactic_grunt_veilstone_city_1 and galactic_grunt_veilstone_city_2 with lucas_veilstone_city_chimchar Won", scene.Log);
        Assert.Equal("VeilstoneGalacticWarehouse", game.Map.Name);
        Assert.Equal((8, 11), game.Tile);
    }

    [Fact]
    public void LosingBesideTheAssistantLeavesHerWaiting()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Story.SetVar("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE", 1);
        game.Story.GiveBadge(Badge.Cobble);
        Run(game, "veilstone_city", "setvar VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE 2\n clearflag FLAG_HIDE_VEILSTONE_COUNTERPART");
        game.Arrive("Sinnoh", 690, 600);
        TalkFrom(game, "counterpart", "veilstone_city", Direction.Right);
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS"));
        Assert.Equal(0, game.Story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE"));
        game.Arrive("Sinnoh", 690, 600);
        var assistant = game.Present("counterpart", "veilstone_city")!;
        Assert.Equal((696, 596), (assistant.GridX, assistant.GridY));
        Assert.NotNull(game.Present("grunt_m_warehouse_north", "veilstone_city"));
    }

    [Fact]
    public void Hm02TeachesFlyAndTheCobbleBadgeLetsItBeUsedOutside()
    {
        var game = NewGame();
        ThroughTheWarehouse(game);
        var hm = ItemDatabase.Get("HM02")!;
        var staravia = new Pokemon(PokemonDatabase.Get("Staravia")!, 30);
        Assert.True(MoveTeaching.CanLearn(staravia, hm));
        MoveTeaching.Learn(staravia, "Fly", forget: 0);
        Assert.Contains(PartyScreen.ActionsFor(staravia), a => a.Move == FieldMove.Fly);
        game.Through("Sinnoh");
        var spot = FieldMoveRules.SpotOf(game.Map, game.Tile.X, game.Tile.Y, Direction.Down, new Walker(TravelMode.OnFoot, game.Map.HeightAt(game.Tile.X, game.Tile.Y)));
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Fly, spot, game.Story));
        // Not inside the warehouse, as the original's header has it, and not without the Badge
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Fly, spot, new StoryState()));
    }

    // ------------------------------------------------------------------ Pastoria City

    [Fact]
    public void TheRivalBattlesAtPastoriasGymDoorOnceLookerHasBeenToTheWarehouse()
    {
        var game = NewGame();
        ThroughTheWarehouse(game);
        game.Arrive("Sinnoh", 600, 820);
        // He has left the doorway; stepping in front of it brings him down the street
        Assert.Null(game.Present("rival", "pastoria_city"));
        Assert.True(game.Fires("RivalBattle"));
        var battle = game.Step("RivalBattle");
        Assert.Contains("battle rival_pastoria_city_turtwig Won", battle.Log);
        Assert.Contains(battle.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("apprentice"));
        Assert.Equal(2, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        Assert.Null(game.Present("rival", "pastoria_city"));
        Assert.False(game.Fires("RivalBattle"));
        Assert.Equal("PastoriaGym", game.Map.GetWarpAt(589, 827)!.TargetMap);
    }

    [Fact]
    public void LosingToTheRivalInPastoriaLeavesTheDoorwayClear()
    {
        var game = NewGame(BattleOutcome.Lost);
        Run(game, "pastoria_city", "setflag FLAG_HIDE_PASTORIA_CITY_RIVAL\n setvar VAR_PASTORIA_CITY_STATE 1");
        game.Arrive("Sinnoh", 600, 820);
        game.Step("RivalBattle");
        Assert.Equal(1, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        game.Arrive("Sinnoh", 600, 820);
        Assert.Null(game.Present("rival", "pastoria_city"));
        Assert.True(game.Fires("RivalBattle"));
    }

    [Fact]
    public void TheChapterPlaysThroughPastoriaToTheSecretPotion()
    {
        var game = NewGame();
        ThroughTheWarehouse(game);
        game.Arrive("Sinnoh", 600, 820);
        game.Step("RivalBattle");

        // The Fen Badge: the rival is waiting outside, and Crasher Wake comes out and hears of the bomb
        WinTheFenBadge(game);
        var outside = game.Last;
        Assert.Contains(outside.Transcript, l => l.Speaker == "Crasher Wake" && l.Text.Contains("BOMB"));
        Assert.Contains(outside.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Master"));
        Assert.Equal(4, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_PASTORIA_CITY_GYM_CRASHER_WAKE"));
        var wake = game.Present("crasher_wake", "pastoria_city")!;
        Assert.Equal((611, 810), (wake.GridX, wake.GridY));
        Assert.Equal((608, 814), (game.Present("rival", "pastoria_city")!.GridX, game.Present("rival", "pastoria_city")!.GridY));
        // He has left his Gym meanwhile
        game.Arrive("PastoriaGym", 13, 41);
        Assert.Null(game.Present("crasher_wake"));
        game.Through("Sinnoh");

        // By the observatory the bomb goes off; the grunt runs east, Crasher Wake into the marsh, the rival keeps its door
        var bomb = game.Step("Bomb");
        Assert.Contains("camera Shake", bomb.Log);
        Assert.Contains(bomb.Transcript, l => l.Text.Contains("KA-BOOOOOM"));
        Assert.Contains(bomb.Transcript, l => l.Speaker == "Galactic Grunt" && l.Text.Contains("Galactic Bomb"));
        Assert.Equal(5, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        Assert.Null(game.Present("crasher_wake", "pastoria_city"));
        var rival = game.Present("rival", "pastoria_city")!;
        Assert.Equal((611, 810), (rival.GridX, rival.GridY));
        var grunt = game.Present("grunt_m", "pastoria_city")!;
        Assert.Equal((637, 812), (grunt.GridX, grunt.GridY));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER"));
        Assert.False(game.Fires("Bomb"));

        // The rival turns the player back from the marsh
        game.Arrive("Sinnoh", 600, 820);
        Assert.Equal((611, 810), (rival.GridX, rival.GridY));
        game.Step("BlockGreatMarsh");
        Assert.Equal((610, 811), game.Tile);
        Assert.True(game.Fires("BlockGreatMarsh"));
        Assert.Contains("Galactic creep", game.Talk("rival", "pastoria_city").Transcript.Single().Text);

        // The grunt runs from Pastoria...
        var off = TalkFrom(game, "grunt_m", "pastoria_city", Direction.Right);
        Assert.Contains(off.Transcript, l => l.Text.Contains("lake"));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_PASTORIA_CITY_GRUNT_M"));
        Assert.Null(game.Present("grunt_m", "pastoria_city"));
        game.Arrive("Sinnoh", 600, 820);
        Assert.Null(game.Present("grunt_m", "pastoria_city"));

        // ...along Route 213, twice, and Looker gives chase
        game.Arrive("Sinnoh", 650, 815);
        var first = game.Present("grunt_m", "route_213")!;
        Assert.Equal((654, 812), (first.GridX, first.GridY));
        TalkFrom(game, "grunt_m", "route_213", Direction.Right);
        Assert.Equal((683, 833), (first.GridX, first.GridY));
        Assert.Contains(game.Said, l => l.Text.Contains("Windworks"));
        var looker = TalkFrom(game, "grunt_m", "route_213", Direction.Up);
        Assert.Contains(looker.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("bomb"));
        Assert.True(game.Story.Has("FLAG_ROUTE_213_GRUNT_M_LEFT"));
        Assert.Null(game.Present("grunt_m", "route_213"));
        Assert.Null(game.Present("looker", "route_213"));
        Assert.False(game.Story.Has("FLAG_HIDE_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER"));
        game.Arrive("Sinnoh", 650, 815);
        Assert.Null(game.Present("grunt_m", "route_213"));

        // ...to Valor Lakefront, where he gives up, and Cynthia gives the Secret Potion
        game.Arrive("Sinnoh", 720, 795);
        var cornered = game.Present("grunt_m", "valor_lakefront")!;
        Assert.Equal((719, 790), (cornered.GridX, cornered.GridY));
        TalkFrom(game, "grunt_m", "valor_lakefront", Direction.Up);
        Assert.Equal((723, 769), (cornered.GridX, cornered.GridY));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_VALOR_LAKEFRONT_GRUNT_M"));
        var lake = TalkFrom(game, "grunt_m", "valor_lakefront", Direction.Up);
        Assert.Contains("battle galactic_grunt_valor_lakefront Won", lake.Log);
        Assert.Contains(lake.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Psyduck"));
        Assert.Contains(lake.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Mr. Wake"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));
        Assert.Equal(6, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        foreach (string who in new[] { "grunt_m", "cynthia", "rival" }) Assert.Null(game.Present(who, "valor_lakefront"));
        Assert.True(game.Story.Has("FLAG_HIDE_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER"));

        // Crasher Wake is back in his Gym, and the rival has left the marsh's door
        game.Arrive("PastoriaGym", 13, 41);
        Assert.NotNull(game.Present("crasher_wake"));
        game.Arrive("Sinnoh", 600, 820);
        Assert.Null(game.Present("rival", "pastoria_city"));
        Assert.False(game.Fires("BlockGreatMarsh"));

        // And the Psyduck on Route 210 can be cured (plan 02 · S7's scene)
        game.Arrive("Sinnoh", 560, 590);
        game.Talk("psyduck_1", "route_210_south");
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK"));
    }

    [Theory]
    [InlineData(Direction.Up, 611, 811)]
    [InlineData(Direction.Left, 612, 810)]
    public void CrasherWakeSpokenToByTheObservatorySeesTheBombGoOff(Direction facing, int x, int y)
    {
        var game = NewGame();
        Run(game, "pastoria_city", "givebadge fen\n setvar VAR_PASTORIA_CITY_STATE 4\n clearflag FLAG_HIDE_PASTORIA_CITY_CRASHER_WAKE\n"
            + " clearflag FLAG_HIDE_PASTORIA_CITY_RIVAL\n setflag FLAG_HIDE_PASTORIA_CITY_GRUNT_M");
        game.Arrive("Sinnoh", 600, 820);
        var wake = game.Present("crasher_wake", "pastoria_city")!;
        game.Tile = (x, y);
        game.Facing = facing;
        var bomb = game.Play(Scripts.Find("Wake", "pastoria_city")!, wake);
        Assert.Contains(bomb.Transcript, l => l.Text.Contains("KA-BOOOOOM"));
        Assert.Equal(5, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        var rival = game.Present("rival", "pastoria_city")!;
        Assert.Equal((611, 810), (rival.GridX, rival.GridY));
        Assert.True(game.Story.Has("FLAG_PASTORIA_CITY_GRUNT_M_MOVED_EAST"));
    }

    [Theory]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Up)]
    public void TheGruntOnRoute213RunsAndLookerGivesChaseFromEachSide(Direction facing)
    {
        var game = NewGame();
        Run(game, "route_213", "givebadge fen\n setflag FLAG_TALKED_TO_PASTORIA_CITY_GRUNT_M\n setflag FLAG_TALKED_TO_ROUTE_213_GRUNT_M");
        game.Arrive("Sinnoh", 650, 815);
        var grunt = game.Present("grunt_m", "route_213")!;
        Assert.Equal((683, 833), (grunt.GridX, grunt.GridY));
        var chase = TalkFrom(game, "grunt_m", "route_213", facing);
        Assert.Contains(chase.Transcript, l => l.Speaker == "Looker");
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_213_LOOKER"));
        Assert.False(game.Story.Has("FLAG_HIDE_VALOR_LAKEFRONT_GRUNT_M"));
    }

    [Theory]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void CynthiaComesDownFromTheLakeWhereverThePlayerStandsByTheGrunt(Direction facing)
    {
        var game = NewGame();
        Run(game, "valor_lakefront", "givebadge fen\n setvar VAR_PASTORIA_CITY_STATE 5\n clearflag FLAG_HIDE_VALOR_LAKEFRONT_GRUNT_M\n setflag FLAG_TALKED_TO_VALOR_LAKEFRONT_GRUNT_M");
        game.Arrive("Sinnoh", 720, 795);
        game.Answers.Enqueue(1);
        var lake = TalkFrom(game, "grunt_m", "valor_lakefront", facing);
        Assert.Contains(lake.Asked, a => a.Question.Contains("Psyduck") && a.Answer == ScriptRunner.YesNo[1]);
        Assert.Contains(lake.Transcript, l => l.Text.Contains("You haven't?"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));
        Assert.Equal(6, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
    }

    [Fact]
    public void LosingAtTheLakefrontLeavesTheGruntThere()
    {
        var game = NewGame(BattleOutcome.Lost);
        Run(game, "valor_lakefront", "givebadge fen\n clearflag FLAG_HIDE_VALOR_LAKEFRONT_GRUNT_M\n setflag FLAG_TALKED_TO_VALOR_LAKEFRONT_GRUNT_M");
        game.Arrive("Sinnoh", 720, 795);
        TalkFrom(game, "grunt_m", "valor_lakefront", Direction.Up);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));
        game.Arrive("Sinnoh", 720, 795);
        var grunt = game.Present("grunt_m", "valor_lakefront")!;
        Assert.Equal((723, 769), (grunt.GridX, grunt.GridY));
    }

    [Theory]
    [InlineData(789)]
    [InlineData(790)]
    [InlineData(791)]
    public void TheRoadToSunyshoreIsClosedAfterItsBlackout(int y)
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 715, y);
        game.Tile = (724, y);
        game.Play(Scripts.Find("BlockSunyshore", "valor_lakefront")!);
        Assert.Equal((723, y), game.Tile);
        Assert.Contains(game.Said, l => l.Text.Contains("blackout"));
        var collector = game.Present("collector", "valor_lakefront")!;
        Assert.Equal((724, 788), (collector.GridX, collector.GridY));
        Assert.True(game.Fires("BlockSunyshore"));
    }

    [Fact]
    public void ATelevisionCrewKeepsTheGateToRoute212ShutUntilPastoria()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 458, 725);
        foreach (int x in new[] { 458, 459 })
        {
            Assert.Equal("Sinnoh", game.Map.GetWarpAt(x, 729)!.TargetMap);
            Assert.NotNull(game.Map.GetNpcAt(x, 729));
        }
        Assert.Contains("television", game.Talk("route_212_cameraman", "hearthome_city").Transcript[0].Text);
        // Coming to Pastoria City from the other side lifts it
        game.Arrive("Sinnoh", 600, 820);
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_212_BLOCKADE"));
        game.Arrive("Sinnoh", 458, 725);
        Assert.Null(game.Map.GetNpcAt(458, 729));
        Assert.Null(game.Map.GetNpcAt(459, 729));
    }

    // ------------------------------------------------------------------ the way through

    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() =>
    {
        var game = new OpeningTests.Game();
        return WorldWalk.From(name => game.Maps.TryGetValue(name, out var map) ? map : MapDatabase.Get(name), RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!);
    });

    [Fact]
    public void TheChaptersPlacesAreReachedAndTheWarehousesWayOnIsLocked()
    {
        var walked = Walked.Value;
        var warehouse = walked.Keys.Single(m => m.Name == "VeilstoneGalacticWarehouse");
        // Where HM02 is picked up from, and not past the rusty door, where the Dusk Stone lies
        Assert.Contains((13, 9), walked[warehouse]);
        Assert.DoesNotContain((8, 6), walked[warehouse]);
        Assert.DoesNotContain((7, 3), walked[warehouse]);
        var sinnoh = walked.Keys.Single(m => m.Name == "Sinnoh");
        // The Gyms' doors, Route 212 under the rain and the road on to Sunyshore beyond the man who closes it
        Assert.Contains((589, 828), walked[sinnoh]);
        Assert.Contains((684, 612), walked[sinnoh]);
        Assert.Contains((458, 736), walked[sinnoh]);
        Assert.Contains((735, 789), walked[sinnoh]);
        Assert.Contains(walked.Keys, m => m.Name == "PastoriaGym");
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleAndKnowsWhatItsBadgesBrought()
    {
        var before = new StoryState();
        StoryMigration.Upgrade(before, 9, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(before.Has("FLAG_HIDE_VEILSTONE_CRASHER_WAKE"));
        Assert.True(before.Has("FLAG_HIDE_VALOR_LAKEFRONT_CYNTHIA"));
        Assert.True(before.Has("FLAG_HIDE_ROUTE_213_GRUNT_M"));
        Assert.False(before.Has("FLAG_HIDE_PASTORIA_CITY_RIVAL"));
        Assert.False(before.Has("FLAG_HIDE_ROUTE_212_BLOCKADE"));
        Assert.Equal(0, before.Var("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE"));

        // With Maylene's Badge, Crasher Wake has already come out of her Gym; with Crasher Wake's, the rival has left
        // his doorway and the television crew the gate
        var won = new StoryState();
        won.GiveBadge(Badge.Cobble);
        won.GiveBadge(Badge.Fen);
        StoryMigration.Upgrade(won, 9, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.Equal(1, won.Var("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE"));
        Assert.True(won.Has("FLAG_HIDE_PASTORIA_CITY_RIVAL"));
        Assert.True(won.Has("FLAG_HIDE_ROUTE_212_BLOCKADE"));

        // Having been to Pastoria is enough for the gate
        var visited = new StoryState();
        visited.Set(StoryMigration.ArrivedInPastoriaFlag);
        StoryMigration.Upgrade(visited, 9, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(visited.Has("FLAG_HIDE_ROUTE_212_BLOCKADE"));
        Assert.Equal(10, StoryState.CurrentVersion);
    }

    [Fact]
    public void ANewGameStartsWithTheChapterAsAnOlderSaveIsGivenIt()
    {
        var fresh = new StoryState();
        StoryMigration.BeginNewGame(fresh, Scripts);
        var upgraded = new StoryState();
        StoryMigration.Upgrade(upgraded, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        foreach (string flag in new[] { "FLAG_HIDE_VEILSTONE_CITY_LOOKER", "FLAG_HIDE_PASTORIA_CITY_CRASHER_WAKE", "FLAG_HIDE_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER" })
        {
            Assert.True(fresh.Has(flag));
            Assert.True(upgraded.Has(flag));
        }
    }
}
