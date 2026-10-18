using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The second part of the time after the Hall of Fame (plan 02 · S15, part b) played through with no screen, in the
/// order a Champion meets it: Team Galactic's grunts going into Stark Mountain, Commanders Mars and Jupiter and Charon
/// in its first room, Buck through its great room, Charon's arrest in its heart with Looker, his Croagunk and the
/// Magma Stone, the parting before the mountain's mouth and what it sets; Heatran once Buck has been spoken to at his
/// place; the Elite Four's and the Champion's rematch teams; Candice letting a Champion into Snowpoint Temple, and
/// Regigigas asleep at its foot until the three are brought; the nightmare of Sailor Eldritch's son, the sailing to
/// Fullmoon Island, Cresselia and the Lunar Wing; where the Pokédex says each is met; and the walk of the whole game.
/// Each scene is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class StarkMountainAndTheLegendaryPokemonTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Completed = "FLAG_GAME_COMPLETED", Hall = "PokemonLeagueHallOfFame", Arrested = "FLAG_ARRESTED_CHARON_STARK_MOUNTAIN";
    private const string Outside = "VAR_STARK_MOUNTAIN_OUTSIDE_STATE", Room1 = "VAR_STARK_MOUNTAIN_ROOM_1_STATE", Room3 = "VAR_STARK_MOUNTAIN_ROOM_3_STATE";
    private const string BuckState = "VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE", SpokenToBuck = "FLAG_TALKED_TO_BATTLEGROUND_BUCK";
    private const string Temple = "VAR_SNOWPOINT_CITY_STATE", Nightmare = "VAR_LUNAR_WING_EVENT_STATE";
    private const string InBed = "FLAG_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY_IN_BED", House = "CanalaveCitySailorEldritchHouse";
    private const string SeenTheBoy = "FLAG_TALKED_TO_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY", Woken = "FLAG_WOKE_UP_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY";
    private const string WingFlag = "FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING";

    /// <summary>A Champion with the National Pokédex, home again (plan 02 · S14's scene and plan 02 · S15, part a's upgrade).</summary>
    private static OpeningTests.Game Champion(BattleOutcome fight = BattleOutcome.Won, bool nationalDex = true)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 58));
        game.Story.SetBadges("Sinnoh", 0xFF);
        game.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 6);
        game.Story.Set(StoryState.PokedexFlag);
        game.Facing = Direction.Up;
        game.Arrive(Hall, 8, 15);
        Assert.True(game.Story.Has(Completed));
        game.NationalDex = nationalDex;
        return game;
    }

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static string? AreaKey(Map map, int x, int y) => map.AreaAt(x, y)?.Key;

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing, string? place = null)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        Assert.False(game.Map.IsSolid(game.Tile.X, game.Tile.Y), $"nobody can stand at {game.Tile} to speak to {key}");
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name), npc);
    }

    /// <summary>A step onto a tile of a trigger, which must start its script now.</summary>
    private static HeadlessScriptHost StepOnto(OpeningTests.Game game, int x, int y, string script)
    {
        game.Tile = (x, y);
        var trigger = FieldScripts.TriggerAt(game.Map, x, y, game.Story);
        Assert.NotNull(trigger);
        Assert.Equal(script, trigger!.Script);
        return game.Play(Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name));
    }

    /// <summary>Speaks to someone of a place wherever they have wandered to (whoever needs no particular side).</summary>
    private static HeadlessScriptHost Speak(OpeningTests.Game game, string key, string place)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place), npc);
    }

    /// <summary>The host of a script the game played by itself (a place's arrival), by its full name, the last of them.</summary>
    private static HeadlessScriptHost Played(OpeningTests.Game game, string script) => game.Played.Last(p => p.Script == script).Host;

    private static string Words(HeadlessScriptHost host) => string.Join(" ", host.Transcript.Select(l => l.Text));

    // ------------------------------------------------------------------ before any of it

    [Fact]
    public void EveryoneOfThisPartStandsWhereTheOriginalHasThemUnderItsFlags()
    {
        var game = new OpeningTests.Game();
        // No story version of its own: common.ChapterTwelve hid this part's people already (plan 02 · S15, part a)
        Assert.Equal(20, StoryState.CurrentVersion);   // 20 is plan 19 · K3's Kanto story, not this part's
        var sinnoh = game.Maps["Sinnoh"];
        NPC Of(Map map, string place, string key) => Assert.Single(map.Everyone, n => n.Key == key && (map.ScriptFileAt(n.GridX, n.GridY) == place || n.ScriptFile == place));

        // Stark Mountain's foot: the two grunts on the way round the lava, there from the start; Looker and Buck before
        // the mouth, hidden until Charon is caught
        Assert.Equal((742, 232, "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS", "Grunt"), (Of(sinnoh, "stark_mountain_outside", "grunt_m_1").GridX, Of(sinnoh, "stark_mountain_outside", "grunt_m_1").GridY,
            Of(sinnoh, "stark_mountain_outside", "grunt_m_1").HiddenBy, Of(sinnoh, "stark_mountain_outside", "grunt_m_1").NpcType));
        Assert.Equal((742, 233), (Of(sinnoh, "stark_mountain_outside", "grunt_m_2").GridX, Of(sinnoh, "stark_mountain_outside", "grunt_m_2").GridY));
        // (Looker east of the tile the player is put down on, 751, 233, where the original stands him north of it, behind the player's head)
        Assert.Equal((752, 233, "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER"), (Of(sinnoh, "stark_mountain_outside", "looker").GridX, Of(sinnoh, "stark_mountain_outside", "looker").GridY, Of(sinnoh, "stark_mountain_outside", "looker").HiddenBy));
        Assert.Equal((751, 232), Sinnoh.Area("stark_mountain_outside")!.Objects.Where(o => o.Id == "looker").Select(o => (o.X, o.Z)).Single());
        Assert.Equal((750, 233, "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK"), (Of(sinnoh, "stark_mountain_outside", "buck").GridX, Of(sinnoh, "stark_mountain_outside", "buck").GridY, Of(sinnoh, "stark_mountain_outside", "buck").HiddenBy));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER") && game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK"));
        Assert.False(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS"));
        var trigger = Assert.Single(sinnoh.Triggers, t => AreaKey(sinnoh, t.X, t.Y) == "stark_mountain_outside");
        Assert.Equal((752, 235, "GruntsGoIn", Outside, 0), (trigger.X, trigger.Y, trigger.Script, trigger.Variable, trigger.Value));

        // The first room: the two Commanders side by side, Charon between his grunts behind them, all there from the start
        var room1 = game.Maps["StarkMountainRoom1"];
        foreach (var (key, x, y) in new[] { ("mars", 20, 26), ("jupiter", 21, 26), ("charon", 20, 24), ("grunt_m_1", 19, 24), ("grunt_m_2", 21, 24) })
        {
            var who = Assert.Single(room1.Everyone, n => n.Key == key);
            Assert.Equal((x, y, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"), (who.GridX, who.GridY, who.HiddenBy));
        }
        Assert.False(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"));
        // Each has a look of their own (plan 11 · C2), so none stands as the boy the player is
        Assert.Equal(3, new[] { "mars", "jupiter", "charon" }.Select(key => room1.Everyone.Single(n => n.Key == key).NpcType).Distinct().Count(type => type != "Trainer"));
        // The way in puts whoever comes a step inside it, before Mars
        Assert.Equal(("StarkMountainRoom1", 20, 28), sinnoh.Warps.Where(w => (w.SourceX, w.SourceY) == (750, 231)).Select(w => (w.TargetMap, w.TargetX, w.TargetY)).Single());

        // The great room: Buck where the way opens out, hidden until he first comes in, and the three triggers
        var room2 = game.Maps["StarkMountainRoom2"];
        var buck = Assert.Single(room2.Everyone, n => n.Key == "buck");
        Assert.Equal((42, 68, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK", "Buck"), (buck.GridX, buck.GridY, buck.HiddenBy, buck.Script));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK"));
        Assert.Equal(new[] { (42, 69, "BuckJoins", 0), (42, 70, "PlayerTurnsBack", 1), (47, 3, "BuckGoesAhead", 1) }, room2.Triggers.Select(t => (t.X, t.Y, t.Script, t.Value)));
        Assert.All(room2.Triggers, t => Assert.Equal(BuckState, t.Variable));
        Assert.NotNull(TrainerDatabase.Get("buck_stark_mountain"));

        // The mountain's heart: Charon between his grunts before the Magma Stone and Buck inside the way in, there from
        // the start; Looker in his rock, his Croagunk, the policemen and Heatran, hidden
        var room3 = game.Maps["StarkMountainRoom3"];
        var expected = new (string Key, int X, int Y, string Flag, bool HiddenAtStart)[]
        {
            ("charon", 7, 11, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC", false), ("grunt_m_1", 6, 12, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC", false),
            ("grunt_m_2", 8, 12, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC", false), ("buck", 6, 16, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_BUCK", false),
            ("magma_stone", 7, 8, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_MAGMA_STONE", false), ("rock_smash_looker", 10, 13, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_ROCK_SMASH_LOOKER", false),
            ("looker", 10, 13, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_LOOKER", true), ("croagunk", 9, 13, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK", true),
            ("policeman_1", 7, 17, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN", true), ("policeman_2", 7, 17, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN", true),
            ("heatran", 7, 6, "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN", true)
        };
        Assert.Equal(expected.Select(e => e.Key).Order(), room3.Everyone.Select(n => n.Key!).Order());
        foreach (var (key, x, y, flag, hidden) in expected)
        {
            var who = Assert.Single(room3.Everyone, n => n.Key == key);
            Assert.Equal((x, y, flag), (who.GridX, who.GridY, who.HiddenBy));
            Assert.Equal(hidden, game.Story.Has(flag));
        }
        Assert.Equal(("Heatran", "Croagunk"), (room3.Everyone.Single(n => n.Key == "heatran").Species, room3.Everyone.Single(n => n.Key == "croagunk").Species));
        // The original's own objects for the stone and the rock have the looks of an obstacle, and are held back: these
        // are the story's, which no field move clears
        Assert.Equal(new[] { "magma_stone", "rock_smash_looker" }, Sinnoh.Overlay("stark_mountain_room_3")!.HeldBack);
        Assert.Equal((NPC.MagmaStoneType, NPC.FalseRockType), (room3.Everyone.Single(n => n.Key == "magma_stone").NpcType, room3.Everyone.Single(n => n.Key == "rock_smash_looker").NpcType));
        Assert.Equal(("StarkMountainRoom3", 7, 16), room2.Warps.Where(w => (w.SourceX, w.SourceY) == (47, 2)).Select(w => (w.TargetMap, w.TargetX, w.TargetY)).Single());

        // Snowpoint City: Candice down the street from the temple, hidden until she lets a Champion in
        var candice = Of(sinnoh, "snowpoint_city", "candice");
        Assert.Equal((366, 208, "FLAG_HIDE_SNOWPOINT_CITY_CANDICE"), (candice.GridX, candice.GridY, candice.HiddenBy));
        Assert.True(game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_CANDICE"));
        // The temple's foot: Regigigas on its dais, there from the start behind the temple's door
        var regigigas = Assert.Single(game.Maps["SnowpointTempleB5F"].Everyone);
        Assert.Equal(("regigigas", 11, 12, "Regigigas", "FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS"), (regigigas.Key, regigigas.GridX, regigigas.GridY, regigigas.Species, regigigas.HiddenBy));
        Assert.False(game.Story.Has("FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS"));

        // Fullmoon Island's wood: Cresselia, and on its tile the Lunar Wing's ball, hidden from the start
        var wood = game.Maps["FullmoonIslandForest"];
        Assert.Equal(new[] { "cresselia", "item_lunar_wing" }, wood.Everyone.Select(n => n.Key!).Order());
        var cresselia = wood.Everyone.Single(n => n.Key == "cresselia");
        var wing = wood.Everyone.Single(n => n.Key == "item_lunar_wing");
        Assert.Equal((16, 15, "Cresselia", "FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA"), (cresselia.GridX, cresselia.GridY, cresselia.Species, cresselia.HiddenBy));
        Assert.Equal((16, 15, "Lunar Wing", WingFlag, true), (wing.GridX, wing.GridY, wing.Item, wing.HiddenBy, wing.IsItemBall));
        Assert.True(game.Story.Has(WingFlag));
        Assert.False(game.Story.Has("FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA"));
        Assert.Null(Sinnoh.Overlay("fullmoon_island_forest")!.HeldBack);

        // Sailor Eldritch's house: the boy who runs about and his mother; the boy in his bed waits for the nightmare, and
        // his father for the day it ends (a step inside the door, clear of the tile whoever comes in is put on)
        var home = game.Maps[House];
        Assert.Equal(new[] { "little_boy", "little_boy_in_bed", "pokefan_f", "sailor_eldritch" }, home.Everyone.Select(n => n.Key!).Order());
        Assert.Equal(new[] { "little_boy", "pokefan_f" }, home.NPCs.Select(n => n.Key!).Order());
        var asleep = home.Everyone.Single(n => n.Key == "little_boy_in_bed");
        Assert.Equal((8, 6, InBed, "BoyInBed"), (asleep.GridX, asleep.GridY, asleep.ShownBy, asleep.Script));
        Assert.Equal(InBed, home.Everyone.Single(n => n.Key == "little_boy").HiddenBy);
        // He lies on the bed's middle tile, a little above the floor, and is looked at from the one tile beside it
        Assert.Contains(home.Props, p => p.Type == PropType.Bed && p.Covers(8, 6));
        Assert.True(asleep.Level is > 0.3f and < 0.6f);
        Assert.True(!home.IsSolid(7, 6) && home.IsSolid(8, 5) && home.IsSolid(8, 7));
        var father = home.Everyone.Single(n => n.Key == "sailor_eldritch");
        Assert.Equal((4, 7, "FLAG_HIDE_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_SAILOR_ELDRITCH"), (father.GridX, father.GridY, father.HiddenBy));
        Assert.Equal((4, 8), home.Warps.Select(w => (w.SourceX, w.SourceY - 1)).Single());
        Assert.True(game.Story.Has(father.HiddenBy!));
    }

    private static readonly Lazy<World> SinnohWorld = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Sinnoh));
    private static World Sinnoh => SinnohWorld.Value;

    // ------------------------------------------------------------------ Stark Mountain

    /// <summary>A Champion at the foot of Stark Mountain, asked along by Buck on Route 227 (plan 02 · S15, part a).</summary>
    private static OpeningTests.Game AtTheMountain(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = Champion(fight);
        game.Arrive("Sinnoh", 758, 280);
        game.Facing = Direction.Up;
        StepOnto(game, 758, 277, "BuckAsks");
        Assert.NotNull(game.Present("buck", "route_227"));
        game.Arrive("Sinnoh", 756, 239);
        return game;
    }

    [Fact]
    public void TheGruntsGoIntoTheMountainAheadOfWhoeverComesRoundTheLava()
    {
        var game = AtTheMountain();
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_EXTERIOR"));
        var first = game.Present("grunt_m_1", "stark_mountain_outside")!;
        var second = game.Present("grunt_m_2", "stark_mountain_outside")!;
        // They stand on the only way round to the mouth, and the trigger lies on the way to them
        Assert.True(game.Map.IsSolid(741, 232) && game.Map.IsSolid(743, 233) && game.Map.IsSolid(741, 233));
        Assert.True(game.Map.IsSolid(752, 234) && game.Map.IsSolid(751, 235));

        game.Facing = Direction.Left;
        var scene = StepOnto(game, 752, 235, "GruntsGoIn");
        Assert.Contains("camera Pan 747 233", scene.Log);
        Assert.Contains("camera Release", scene.Log);
        Assert.Equal(4, scene.Transcript.Count);
        Assert.All(scene.Transcript, l => Assert.Equal("Grunt", l.Speaker));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Master Cyrus"));
        Assert.Contains(scene.Transcript, l => l.Text.Contains("done with Team Galactic"));
        // They went in at the mouth, and are gone for good
        Assert.Equal(((750, 231), (750, 231)), ((first.GridX, first.GridY), (second.GridX, second.GridY)));
        Assert.NotNull(game.Map.GetWarpAt(750, 231));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS"));
        Assert.Equal(1, game.Story.Var(Outside));
        Assert.Null(game.Present("grunt_m_1", "stark_mountain_outside"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 752, 235, game.Story));
        game.Arrive("Sinnoh", 756, 239);
        Assert.Null(game.Present("grunt_m_2", "stark_mountain_outside"));
        // Nobody waits before the mouth yet
        Assert.Null(game.Present("looker", "stark_mountain_outside"));
        Assert.Null(game.Present("buck", "stark_mountain_outside"));
    }

    [Fact]
    public void MarsAndJupiterBattleInTheFirstRoomAndWalkOutOnTeamGalactic()
    {
        var game = AtTheMountain();
        game.Facing = Direction.Up;
        game.Through("StarkMountainRoom1");
        Assert.Equal("StarkMountainRoom1", game.Map.Name);
        var scene = Played(game, "stark_mountain_room_1.OnEnter");
        // In the original's order: Mars, her battle, Jupiter, hers, their leaving, Charon
        int mars = scene.Log.IndexOf("battle commander_mars_stark_mountain Won"), jupiter = scene.Log.IndexOf("battle commander_jupiter_stark_mountain Won");
        Assert.True(mars >= 0 && jupiter > mars);
        Assert.Equal(new[] { "Bronzong", "Golbat", "Purugly" }, TrainerDatabase.Get("commander_mars_stark_mountain")!.Party.Select(p => p.Species));
        Assert.Equal(new[] { "Bronzong", "Golbat", "Skuntank" }, TrainerDatabase.Get("commander_jupiter_stark_mountain")!.Party.Select(p => p.Species));
        Assert.Equal(new[] { "Mars", "Jupiter", "Charon" }, scene.Transcript.Select(l => l.Speaker).Distinct());
        Assert.Contains(scene.Transcript, l => l.Speaker == "Mars" && l.Text.Contains("Master Cyrus went"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Mars" && l.Text.Contains("through with Team Galactic"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Jupiter" && l.Text.Contains("leaving as well"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Charon" && l.Text.Contains("Magma Stone"));
        Assert.Equal(2, scene.Log.Count(l => l == "sound stairs"));

        // What the arrival and the scene leave
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_INTERIOR") && game.Story.Has("FLAG_HIDE_ROUTE_227_BUCK"));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"));
        Assert.Equal(1, game.Story.Var(Room1));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key is "mars" or "jupiter" or "charon" or "grunt_m_1" or "grunt_m_2");
        // The player made room for them a step to the west, and the two Commanders left by the way in
        Assert.Equal((19, 28), game.Tile);
        Assert.All(new[] { "mars", "jupiter" }, key => Assert.Equal((20, 29), (game.Map.Everyone.Single(n => n.Key == key).GridX, game.Map.Everyone.Single(n => n.Key == key).GridY)));
        // Charon and his grunts went on up the room
        Assert.All(new[] { "charon", "grunt_m_1", "grunt_m_2" }, key => Assert.Equal((20, 17), (game.Map.Everyone.Single(n => n.Key == key).GridX, game.Map.Everyone.Single(n => n.Key == key).GridY)));

        // Coming in again there is nobody, and Route 227's Buck is gone
        int before = game.Played.Count;
        game.Arrive("StarkMountainRoom1", 20, 28);
        Assert.Empty(game.Played[before].Host.Transcript);
        game.Arrive("Sinnoh", 758, 280);
        Assert.Null(game.Present("buck", "route_227"));
    }

    [Fact]
    public void ABattleLostInTheFirstRoomLeavesTeamGalacticWaitingThere()
    {
        var game = AtTheMountain(BattleOutcome.Lost);
        game.Facing = Direction.Up;
        game.Through("StarkMountainRoom1");
        var scene = Played(game, "stark_mountain_room_1.OnEnter");
        Assert.Contains("battle commander_mars_stark_mountain Lost", scene.Log);
        Assert.DoesNotContain(scene.Log, l => l.Contains("jupiter"));
        Assert.Equal(0, game.Story.Var(Room1));
        Assert.False(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"));
        // The scene plays again from its start the next time, the Commanders back in their places
        game.Arrive("StarkMountainRoom1", 20, 28);
        var again = Played(game, "stark_mountain_room_1.OnEnter");
        Assert.Contains(again.Transcript, l => l.Speaker == "Mars" && l.Text.Contains("Master Cyrus went"));
        Assert.Equal((20, 27), (game.Present("mars")!.GridX, game.Present("mars")!.GridY));
        Assert.Equal((21, 26), (game.Present("jupiter")!.GridX, game.Present("jupiter")!.GridY));
    }

    [Fact]
    public void AGameSavedFurtherUpTheFirstRoomIsBroughtDownToMeetTeamGalactic()
    {
        var game = AtTheMountain();
        game.Facing = Direction.Down;
        game.Arrive("StarkMountainRoom1", 20, 20);
        Assert.Equal(1, game.Story.Var(Room1));
        Assert.Equal((19, 28), game.Tile);
    }

    /// <summary>Into the great room with Team Galactic gone from the first.</summary>
    private static OpeningTests.Game InTheGreatRoom(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = AtTheMountain(fight);
        game.Story.SetVar(Room1, 1);
        game.Story.Set("FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC");
        game.Arrive("StarkMountainRoom2", 42, 85);
        Assert.Equal(0, game.Story.Var(BuckState));
        Assert.Null(game.Present("buck"));
        return game;
    }

    [Fact]
    public void BuckComesInAfterThePlayerAndWalksTheGreatRoomWithThem()
    {
        var game = InTheGreatRoom();
        // The passage in is one tile wide, so the tile where it opens out is stepped on by everyone
        Assert.All(Enumerable.Range(70, 16), y => Assert.True(!game.Map.IsSolid(42, y) && game.Map.IsSolid(41, y) && game.Map.IsSolid(43, y)));
        game.Facing = Direction.Up;
        var joined = StepOnto(game, 42, 69, "BuckJoins");
        Assert.Contains(joined.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("Wait for me"));
        Assert.Contains(joined.Transcript, l => l.Speaker == null && l.Text.Contains("decided to go with Buck"));
        Assert.Contains($"fanfare {PokemonPlatinumEngine.Audio.MusicRole.FanfareLevelUp}", joined.Log);
        Assert.Contains("partner Buck buck_stark_mountain", joined.Log);
        Assert.Equal(1, game.Story.Var(BuckState));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_STARK_MOUNTAIN_ROOM_2_BUCK"));
        Assert.False(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK"));
        // He came up the passage to the tile behind the player
        var buck = game.Present("buck")!;
        Assert.Equal((42, 70), (buck.GridX, buck.GridY));
        Assert.Equal(new[] { "Claydol" }, TrainerDatabase.Get("buck_stark_mountain")!.Party.Select(p => p.Species));

        // On the way he has four things to say, the last again and again
        buck.GridY = 67;
        var said = Enumerable.Range(0, 5).Select(_ => TalkFrom(game, "buck", Direction.Up).Transcript.Single().Text).ToList();
        Assert.Equal(4, said.Distinct().Count());
        Assert.Equal(said[3], said[4]);
        Assert.Contains("Flint", said[2]);
        Assert.Equal(3, game.Story.Var("VAR_FOLLOWER_BUCK_TIMES_TALKED"));

        // Turning back for the way out leaves him waiting where the way opens out
        game.Facing = Direction.Down;
        var back = StepOnto(game, 42, 70, "PlayerTurnsBack");
        Assert.Contains("partner off", back.Log);
        Assert.Contains(back.Transcript, l => l.Text.Contains("all on my own"));
        Assert.Equal(0, game.Story.Var(BuckState));
        Assert.Equal((42, 68, Direction.Down), (buck.GridX, buck.GridY, buck.Facing));

        // And he has other words for whoever comes back up to him
        game.Facing = Direction.Up;
        var again = StepOnto(game, 42, 69, "BuckJoins");
        Assert.Contains(again.Transcript, l => l.Text.Contains("go together"));
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("Wait for me"));
        Assert.Contains("partner Buck buck_stark_mountain", again.Log);
        Assert.Equal(1, game.Story.Var(BuckState));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 42, 69, game.Story));
    }

    [Theory]
    [InlineData(Direction.Up, 46, 3)]
    [InlineData(Direction.Left, 46, 3)]
    [InlineData(Direction.Right, 48, 3)]
    public void BuckGoesOnAheadIntoTheMountainsHeartFromWhicheverSideThePlayerComes(Direction facing, int asideX, int asideY)
    {
        var game = InTheGreatRoom();
        game.Facing = Direction.Up;
        StepOnto(game, 42, 69, "BuckJoins");
        // The way in to the heart is a tile north of the trigger's
        Assert.Equal("StarkMountainRoom3", game.Map.GetWarpAt(47, 2)?.TargetMap);

        game.Facing = facing;
        var ahead = StepOnto(game, 47, 3, "BuckGoesAhead");
        Assert.Contains("partner off", ahead.Log);
        Assert.Contains(ahead.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("Here we are"));
        Assert.Contains("sound stairs", ahead.Log);
        // The player made room, and he went in at the way on
        Assert.Equal((asideX, asideY), game.Tile);
        var buck = game.Map.Everyone.Single(n => n.Key == "buck");
        Assert.Equal((47, 2), (buck.GridX, buck.GridY));
        Assert.Null(game.Present("buck"));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK"));
        Assert.Equal(2, game.Story.Var(BuckState));

        // The room is his no longer: nothing fires, and coming in again leaves the state alone
        game.Arrive("StarkMountainRoom2", 42, 85);
        Assert.Equal(2, game.Story.Var(BuckState));
        Assert.All(new[] { (42, 69), (42, 70), (47, 3) }, t => Assert.Null(FieldScripts.TriggerAt(game.Map, t.Item1, t.Item2, game.Story)));
        Assert.Null(game.Present("buck"));
    }

    [Fact]
    public void BuckWaitsWhereTheWayOpensOutForAPlayerWhoLeftWithoutHim()
    {
        var game = InTheGreatRoom();
        game.Facing = Direction.Up;
        StepOnto(game, 42, 69, "BuckJoins");
        // A white-out leaves him behind with the state as it was: coming in again he waits a step further in
        game.Arrive("StarkMountainRoom2", 42, 85);
        Assert.Equal(0, game.Story.Var(BuckState));
        var buck = game.Present("buck")!;
        Assert.Equal((42, 68), (buck.GridX, buck.GridY));
        game.Facing = Direction.Up;
        Assert.Contains(StepOnto(game, 42, 69, "BuckJoins").Transcript, l => l.Text.Contains("go together"));
    }

    /// <summary>Into the mountain's heart, Buck gone ahead from the great room.</summary>
    private static OpeningTests.Game IntoTheHeart()
    {
        var game = InTheGreatRoom();
        game.Facing = Direction.Up;
        StepOnto(game, 42, 69, "BuckJoins");
        StepOnto(game, 47, 3, "BuckGoesAhead");
        game.Facing = Direction.Up;
        game.Through("StarkMountainRoom3");
        return game;
    }

    [Fact]
    public void CharonIsArrestedInTheMountainsHeartAndTheThreePartOutside()
    {
        var game = IntoTheHeart();
        var scene = Played(game, "stark_mountain_room_3.OnEnter");
        // The beats, in the original's order
        var speakers = scene.Transcript.Select(l => l.Speaker).ToList();
        Assert.Equal(new[] { "Charon", "Buck", "Charon", "???", "Charon", "???", "Looker", "Grunt", "Charon", "Looker" },
            speakers.Where((s, i) => i == 0 || s != speakers[i - 1]));
        Assert.Contains("camera Pan 7 12", scene.Log);
        Assert.Contains("camera Shake", scene.Log);
        Assert.Contains("camera Release", scene.Log);
        Assert.Contains("cry Croagunk", scene.Log);
        Assert.Contains(scene.Transcript, l => l.Speaker == "Charon" && l.Text.Contains("wake the Pokémon Heatran"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("as a rock"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Grunt" && l.Text.Contains("International Police"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("far too hot"));
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("battle", StringComparison.Ordinal));

        // Everything the scene sets, as the original's does
        Assert.Equal((1, 3), (game.Story.Var(Room3), game.Story.Var(Outside)));
        Assert.True(game.Story.Has(Arrested));
        foreach (string set in new[] { "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_MAGMA_STONE", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_ROCK_SMASH_LOOKER",
                     "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_LOOKER" })
            Assert.True(game.Story.Has(set), set);
        foreach (string cleared in new[] { "FLAG_HIDE_GAME_CORNER_LOOKER", "FLAG_HIDE_SURVIVAL_AREA_RIVAL", "FLAG_HIDE_BATTLEGROUND_BUCK", "FLAG_HIDE_SURVIVAL_AREA_BUCK" })
            Assert.False(game.Story.Has(cleared), cleared);
        // Heatran is not woken by it: that waits for Buck at his place
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN"));
        Assert.False(game.Story.Has("FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN"));
        // The flag is asked ahead of nothing any more: this is the scene that sets it
        Assert.DoesNotContain(Arrested, StoryTests.AskedAheadOfTheirScenes);
        Assert.Contains(Arrested, Scripts.FlagsWritten);

        // Where everyone went in the hall: the grunts and the three with Charon out by the way in
        var heart = game.Maps["StarkMountainRoom3"];
        Assert.All(new[] { "grunt_m_1", "grunt_m_2", "charon", "policeman_1", "policeman_2" }, key => Assert.Equal((7, 17), (heart.Everyone.Single(n => n.Key == key).GridX, heart.Everyone.Single(n => n.Key == key).GridY)));
        Assert.Equal((7, 11), (heart.Everyone.Single(n => n.Key == "looker").GridX, heart.Everyone.Single(n => n.Key == "looker").GridY));
        Assert.Equal((6, 14), (heart.Everyone.Single(n => n.Key == "buck").GridX, heart.Everyone.Single(n => n.Key == "buck").GridY));

        // The scene put the player down before the mountain's mouth, where the parting played as the place was come to
        Assert.Contains("warp Sinnoh 751 233", scene.Log);
        Assert.Equal(("Sinnoh", (751, 233)), (game.Map.Name, game.Tile));
        var parting = Played(game, "stark_mountain_outside.OnEnter");
        Assert.Contains(parting.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("Magma Stone"));
        Assert.Contains(parting.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("back in its place"));
        Assert.Contains(parting.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("Survival Area"));
        Assert.Contains(parting.Transcript, l => l.Speaker == "Looker" && l.Text.Contains("Farewell"));
        Assert.True(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK") && game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER"));
        Assert.Null(game.Present("looker", "stark_mountain_outside"));
        Assert.Null(game.Present("buck", "stark_mountain_outside"));
        // Buck went off round the lava by the road, to its south-west corner
        var buck = game.Map.Everyone.Single(n => n.Key == "buck" && AreaKey(game.Map, n.GridX, n.GridY) == "stark_mountain_outside");
        Assert.Equal((742, 244), (buck.GridX, buck.GridY));

        // Nothing plays a second time: outside, or in the heart, which is empty
        int before = game.Played.Count;
        game.Arrive("Sinnoh", 751, 233);
        Assert.Empty(game.Played[before].Host.Transcript);
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.Empty(game.Played[^1].Host.Transcript);
        Assert.Empty(game.Map.NPCs);
    }

    [Fact]
    public void CharonsArrestBringsBuckAndTheRivalToTheSurvivalAreaAndLookerBackToVeilstone()
    {
        var game = InTheGreatRoom();
        // Looker left the Game Corner when the Cobble Badge was won (the Veilstone Gym's script hides him)
        game.Story.Set("FLAG_HIDE_GAME_CORNER_LOOKER");
        game.Facing = Direction.Up;
        StepOnto(game, 42, 69, "BuckJoins");
        StepOnto(game, 47, 3, "BuckGoesAhead");
        game.Facing = Direction.Up;
        game.Through("StarkMountainRoom3");
        // Buck and the rival before the Battleground's door (plan 02 · S15, part a wrote them; this scene shows them)
        game.Arrive("Sinnoh", 655, 341);
        Assert.Equal((652, 339), (game.Present("buck", "survival_area")!.GridX, game.Present("buck", "survival_area")!.GridY));
        Assert.NotNull(game.Present("rival", "survival_area"));
        // The Battleground's door is open from the arrest on (plan 02 · S15, part c built the room)
        Assert.False(game.Map.IsSolid(652, 338));
        Assert.Equal("Battleground", game.Map.GetWarpAt(652, 338)!.TargetMap);

        // The Fight Area's rival is gone for good as the town is come to (the original's FightArea_HideRival)
        game.Story.SetVar("VAR_FIGHT_AREA_STATE", 2);
        Run(game, "fight_area", "clearflag FLAG_HIDE_FIGHT_AREA_RIVAL");
        game.Arrive("Sinnoh", 623, 434);
        Assert.Equal("fight_area", AreaKey(game.Map, 623, 434));
        Assert.True(game.Story.Has("FLAG_HIDE_FIGHT_AREA_RIVAL"));
        Assert.Null(game.Present("rival", "fight_area"));

        // Looker is back at the Game Corner with his other words
        var corner = game.Maps["VeilstoneGameCorner"];
        corner.ApplyPresence(game.Story.Has);
        var looker = Assert.Single(corner.NPCs, n => n.Key == "looker");
        var host = new HeadlessScriptHost(game.Story) { Map = corner };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Find("Looker", "VeilstoneGameCorner"), looker);
        runner.RunToEnd();
        Assert.Contains("holiday", Words(host));
    }

    [Fact]
    public void BeforeCharonIsCaughtTheFightAreasRivalIsLeftAlone()
    {
        var game = Champion();
        game.Story.SetVar("VAR_FIGHT_AREA_STATE", 2);
        game.Arrive("Sinnoh", 623, 434);
        Assert.False(game.Story.Has("FLAG_HIDE_FIGHT_AREA_RIVAL"));
    }

    // ------------------------------------------------------------------ Heatran

    /// <summary>Charon caught, and Buck spoken to at his place (the Battleground's own scene, plan 02 · S15, part c).</summary>
    private static OpeningTests.Game HeatranWakes(BattleOutcome fight = BattleOutcome.Won, bool spokenToBuck = true, bool nationalDex = true)
    {
        var game = Champion(fight, nationalDex);
        game.Story.SetVar(Room3, 1);
        game.Story.SetVar(Outside, 3);
        game.Story.Set(Arrested);
        foreach (string gone in new[] { "TEAM_GALACTIC", "BUCK", "MAGMA_STONE", "ROCK_SMASH_LOOKER" }) game.Story.Set($"FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_{gone}");
        if (spokenToBuck) game.Story.Set(SpokenToBuck);
        game.Arrive("StarkMountainRoom3", 7, 16);
        return game;
    }

    [Fact]
    public void HeatranShowsOnlyOnceBuckHasBeenSpokenToAtHisPlace()
    {
        // Charon's arrest alone doesn't wake it
        Assert.Null(HeatranWakes(spokenToBuck: false).Present("heatran"));
        // Nor does Buck's word without the National Pokédex (nobody comes here without it; the original asks all the same)
        Assert.Null(HeatranWakes(nationalDex: false).Present("heatran"));
        // With both it stands at the head of the hall, behind the Magma Stone's empty place
        var game = HeatranWakes();
        var heatran = game.Present("heatran")!;
        Assert.Equal((7, 6, "Heatran", true), (heatran.GridX, heatran.GridY, heatran.Species, heatran.IsPokemon));
        Assert.False(game.Map.IsSolid(7, 7));
        Assert.Null(game.Present("magma_stone"));
        // The flag that brings it is the Battleground's to set (plan 02 · S15, part c: Battleground.Buck)
        Assert.DoesNotContain(SpokenToBuck, StoryTests.AskedAheadOfTheirScenes);
        Assert.Contains(Find("Buck", "Battleground").Everything(), i => i.Op == Op.SetFlag && i.Name == SpokenToBuck);
        // And not before the mountain's scene has played, whatever else is set
        var early = Champion();
        early.Story.Set(SpokenToBuck);
        early.Story.SetVar(Room3, 2);
        early.Arrive("StarkMountainRoom3", 7, 16);
        Assert.Null(early.Present("heatran"));
    }

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Fled)]
    [InlineData(BattleOutcome.Caught)]
    public void HeatranBattlesAndIsGoneWhateverTheBattleComesTo(BattleOutcome outcome)
    {
        var game = HeatranWakes(outcome);
        var battle = TalkFrom(game, "heatran", Direction.Up);
        Assert.Contains("cry Heatran", battle.Log);
        Assert.Contains($"wildbattle Heatran 50 {outcome}", battle.Log);
        Assert.True(game.Story.Has("FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN") && game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN"));
        Assert.Equal(2, game.Story.Var(Room3));
        // Any battle not lost unlocks the Vs. Seeker's last level of rematches
        Assert.True(game.Story.Has(VsSeeker.LevelFlag(5)));
        Assert.Null(game.Present("heatran"));
        Assert.Equal(outcome == BattleOutcome.Caught, game.Story.Has("FLAG_CAUGHT_HEATRAN"));
        Assert.Equal(outcome != BattleOutcome.Caught, battle.Transcript.Any(l => l.Text.Contains("sank back")));
        if (outcome == BattleOutcome.Caught) Assert.Equal(("Heatran", 50), (game.Party.Members[^1].Species.Name, game.Party.Members[^1].Level));
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.Null(game.Present("heatran"));

        // The Hall of Fame brings it back unless it was caught, as the original's does
        game.Arrive(Hall, 8, 15);
        Assert.Equal(outcome == BattleOutcome.Caught ? 2 : 1, game.Story.Var(Room3));
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.Equal(outcome != BattleOutcome.Caught, game.Present("heatran") != null);
        Assert.DoesNotContain("FLAG_CAUGHT_HEATRAN", StoryTests.AskedAheadOfTheirScenes);

        // The Survival Area's cameraman has his scoop from the moment it is battled
        game.Arrive("Sinnoh", 655, 341);
        Assert.Contains("next film", Words(Speak(game, "cameraman", "survival_area")));
    }

    [Fact]
    public void ABattleLostToHeatranLeavesItWaiting()
    {
        var game = HeatranWakes(BattleOutcome.Lost);
        var battle = TalkFrom(game, "heatran", Direction.Up);
        Assert.Contains("wildbattle Heatran 50 Lost", battle.Log);
        Assert.Equal(1, game.Story.Var(Room3));
        Assert.False(game.Story.Has("FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN") || game.Story.Has(VsSeeker.LevelFlag(5)));
        game.Arrive("StarkMountainRoom3", 7, 16);
        Assert.NotNull(game.Present("heatran"));
        // Until it has been battled the cameraman tells of the red Gyarados
        game.Story.Unset("FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN");
        game.Arrive("Sinnoh", 655, 341);
        Assert.Contains("Gyarados", Words(Speak(game, "cameraman", "survival_area")));
    }

    // ------------------------------------------------------------------ the League again

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheEliteFourAndTheChampionBringTheirRematchTeamsOnceCharonIsCaught(bool arrested)
    {
        var game = new OpeningTests.Game();
        game.Story.ChooseStarter(StoryState.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 80));
        game.Story.SetBadges("Sinnoh", 0xFF);
        if (arrested) game.Story.Set(Arrested);
        string suffix = arrested ? "_rematch" : "";
        game.Arrive("PokemonLeagueNorthPokemonCenter", 11, 11);
        foreach (string who in new[] { "Aaron", "Bertha", "Flint", "Lucian" })
        {
            var into = game.Maps[$"PokemonLeagueElevatorTo{who}Room"].Warps.Single(w => w.TargetMap == $"PokemonLeague{who}Room");
            game.Facing = Direction.Up;
            game.Arrive(into.TargetMap, into.TargetX, into.TargetY);
            Assert.Contains($"battle elite_four_{who.ToLowerInvariant()}{suffix} Won", game.Talk(who.ToLowerInvariant()).Log);
        }
        var throne = game.Maps["PokemonLeagueElevatorToChampionRoom"].Warps.Single(w => w.TargetMap == "PokemonLeagueChampionRoom");
        game.Facing = Direction.Up;
        game.Arrive(throne.TargetMap, throne.TargetX, throne.TargetY);
        Assert.Contains($"battle champion_cynthia{suffix} Won", Played(game, "PokemonLeagueChampionRoom.OnEnter").Log);
    }

    // Platinum's rematch teams (res/trainers/data/elite_four_*_rematch.json, champion_cynthia_rematch.json)
    [Theory]
    [InlineData("elite_four_aaron_rematch", "elite_four_aaron", "Elite Four", 69)]
    [InlineData("elite_four_bertha_rematch", "elite_four_bertha", "Elite Four", 71)]
    [InlineData("elite_four_flint_rematch", "elite_four_flint", "Elite Four", 73)]
    [InlineData("elite_four_lucian_rematch", "elite_four_lucian", "Elite Four", 75)]
    [InlineData("champion_cynthia_rematch", "champion_cynthia", "Champion", 78)]
    public void TheRematchTeamsArePlatinumsOwnAndStrongerThanTheFirst(string id, string first, string trainerClass, int highest)
    {
        var rematch = TrainerDatabase.Get(id)!;
        var before = TrainerDatabase.Get(first)!;
        Assert.Equal((trainerClass, before.Name), (rematch.Class, rematch.Name));
        Assert.Equal(highest, rematch.Party.Max(p => p.Level));
        Assert.Equal(16, highest - before.Party.Max(p => p.Level));
        Assert.Equal(before.Party.Count, rematch.Party.Count);
        Assert.All(rematch.Party, p => Assert.Equal(4, p.Moves!.Count));
    }

    // ------------------------------------------------------------------ Snowpoint Temple

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void TheTemplesGuardStillTurnsBackWhoeverIsNoChampionWithTheNationalPokedex(bool champion, bool nationalDex)
    {
        var game = champion ? Champion(nationalDex: nationalDex) : new OpeningTests.Game();
        if (!champion)
        {
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
            game.NationalDex = nationalDex;
        }
        game.Arrive("Sinnoh", 366, 200);
        game.Facing = Direction.Up;
        var turned = StepOnto(game, 366, 198, "TempleGuard");
        Assert.Contains(turned.Transcript, l => l.Speaker == "Temple Guard" && l.Text.Contains("chosen"));
        Assert.DoesNotContain(turned.Transcript, l => l.Speaker == "Candice");
        Assert.Equal((366, 199), game.Tile);
        Assert.Equal(0, game.Story.Var(Temple));
        Assert.Null(game.Present("candice", "snowpoint_city"));
        Assert.NotNull(FieldScripts.TriggerAt(game.Map, 366, 198, game.Story));
        Assert.Contains("cannot let you pass", Words(TalkFrom(game, "temple_guard", Direction.Up, "snowpoint_city")));
        Assert.Contains("lived here all my life", Words(Speak(game, "snowpoint_npc_m", "snowpoint_city")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CandiceLetsAChampionWithTheNationalPokedexIntoTheTemple(bool byTheTrigger)
    {
        var game = Champion();
        game.Arrive("Sinnoh", 366, 200);
        game.Facing = Direction.Up;
        // By the step before the west door, or by speaking to the guard from the south
        var scene = byTheTrigger ? StepOnto(game, 366, 198, "TempleGuard") : TalkFrom(game, "temple_guard", Direction.Up, "snowpoint_city");
        Assert.Contains(scene.Transcript, l => l.Speaker == "Temple Guard" && l.Text.Contains("chosen"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Candice" && l.Text.Contains("let this one in"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Candice" && l.Text.Contains("Cynthia"));
        Assert.Equal(1, game.Story.Var(Temple));
        // The player wasn't sent away, and Candice came up the street to the foot of the temple's steps
        Assert.Equal(byTheTrigger ? (366, 198) : (367, 199), game.Tile);
        // (a tile nearer than the original's seven steps bring her, clear of the text box)
        var candice = game.Present("candice", "snowpoint_city")!;
        Assert.Equal((366, 200, Direction.Up), (candice.GridX, candice.GridY, candice.Facing));
        Assert.All(Enumerable.Range(200, 9), y => Assert.False(game.Map.IsSolid(366, y)));
        Assert.Equal(Direction.Down, game.Present("temple_guard", "snowpoint_city")!.Facing);
        Assert.Contains("Have fun exploring", Words(TalkFrom(game, "candice", Direction.Up, "snowpoint_city")));

        // The trigger is dead and the west door leads in; the guard never leaves the east one, which stays shut
        Assert.Null(FieldScripts.TriggerAt(game.Map, 366, 198, game.Story));
        var door = game.Map.GetWarpAt(366, 197)!;
        Assert.Equal(("SnowpointTemple1F", 8, 12), (door.TargetMap, door.TargetX, door.TargetY));
        Assert.Null(game.Map.GetWarpAt(367, 197));
        Assert.True(game.Map.IsSolid(367, 197));
        Assert.Equal(new[] { 6 }, Sinnoh.Overlay("snowpoint_city")!.Locked); // the tutor's house (warp 1) is open since plan 01 · M11, part d

        // In, and out again onto the trigger's tile: Candice has gone back to her Gym, and nothing turns the player back
        game.Arrive(door.TargetMap, door.TargetX, door.TargetY);
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_SNOWPOINT_TEMPLE"));
        var exit = game.Map.Warps.Single(w => w.TargetMap == "Sinnoh");
        Assert.Equal((366, 198), (exit.TargetX, exit.TargetY));
        game.Arrive("Sinnoh", exit.TargetX, exit.TargetY);
        Assert.Null(game.Present("candice", "snowpoint_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_CANDICE"));
        Assert.Contains("age nobody remembers", Words(TalkFrom(game, "temple_guard", Direction.Up, "snowpoint_city")));
        Assert.Contains("dragged the whole of Snowpoint City", Words(Speak(game, "snowpoint_npc_m", "snowpoint_city")));
    }

    /// <summary>A Champion before Regigigas, the temple open.</summary>
    private static OpeningTests.Game BeforeRegigigas(BattleOutcome fight, string titans)
    {
        var game = Champion(fight);
        game.Story.SetVar(Temple, 1);
        foreach (string titan in titans.Split(',', StringSplitOptions.RemoveEmptyEntries)) game.Party.Add(new Pokemon(PokemonDatabase.Get(titan)!, 30));
        game.Arrive("SnowpointTempleB5F", 7, 3);
        return game;
    }

    [Theory]
    [InlineData("")]
    [InlineData("Regirock,Regice")]
    [InlineData("Regirock,Registeel")]
    [InlineData("Regice,Registeel")]
    public void RegigigasSleepsUntilTheThreeAreBroughtBeforeIt(string titans)
    {
        var game = BeforeRegigigas(BattleOutcome.Won, titans);
        // Spoken to from the south, the one side its dais is open to
        Assert.True(game.Map.IsSolid(10, 11) && game.Map.IsSolid(9, 12) && game.Map.IsSolid(13, 12) && !game.Map.IsSolid(11, 13));
        var read = TalkFrom(game, "regigigas", Direction.Up);
        Assert.Equal(2, read.Transcript.Count);
        Assert.Contains("One of rock. One of ice. One of steel.", read.Transcript[1].Text);
        Assert.DoesNotContain(read.Log, l => l.StartsWith("wildbattle", StringComparison.Ordinal) || l.StartsWith("cry", StringComparison.Ordinal));
        Assert.False(game.Story.Has("FLAG_AWAKENED_REGIGIGAS"));
        Assert.NotNull(game.Present("regigigas"));
    }

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Fled)]
    [InlineData(BattleOutcome.Caught)]
    public void RegigigasWakesForTheThreeAndIsGoneWhateverTheBattleComesTo(BattleOutcome outcome)
    {
        // The team's order doesn't matter, nor who else is in it
        var game = BeforeRegigigas(outcome, "Registeel,Regirock,Regice");
        var battle = TalkFrom(game, "regigigas", Direction.Up);
        // The statue itself shakes as it wakes, eight times (plan 09 · L12: the original's ShakeObject)
        Assert.Contains("shake Regigigas 8", battle.Log);
        Assert.Contains("cry Regigigas", battle.Log);
        // At level 1, as Platinum has it
        Assert.Contains($"wildbattle Regigigas 1 {outcome}", battle.Log);
        Assert.True(game.Story.Has("FLAG_AWAKENED_REGIGIGAS") && game.Story.Has("FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS"));
        Assert.Null(game.Present("regigigas"));
        Assert.Equal(outcome == BattleOutcome.Caught, game.Story.Has("FLAG_CAUGHT_REGIGIGAS"));
        Assert.Equal(outcome != BattleOutcome.Caught, battle.Transcript.Any(l => l.Text.Contains("gone from sight")));
        Assert.DoesNotContain("FLAG_CAUGHT_REGIGIGAS", StoryTests.AskedAheadOfTheirScenes);

        // The Hall of Fame brings it back unless it was caught; awake already, it battles without the three or the words
        game.Arrive(Hall, 8, 15);
        game.Arrive("SnowpointTempleB5F", 7, 3);
        Assert.Equal(outcome != BattleOutcome.Caught, game.Present("regigigas") != null);
        if (outcome == BattleOutcome.Caught) return;
        game.Party.Members.RemoveAll(p => p.Species.Name.StartsWith("Regi", StringComparison.Ordinal));
        var again = TalkFrom(game, "regigigas", Direction.Up);
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("One of rock"));
        Assert.Contains($"wildbattle Regigigas 1 {outcome}", again.Log);
    }

    [Fact]
    public void ABattleLostToRegigigasLeavesItAwakeAndWaiting()
    {
        var game = BeforeRegigigas(BattleOutcome.Lost, "Regirock,Regice,Registeel");
        Assert.Contains("wildbattle Regigigas 1 Lost", TalkFrom(game, "regigigas", Direction.Up).Log);
        Assert.True(game.Story.Has("FLAG_AWAKENED_REGIGIGAS"));
        Assert.False(game.Story.Has("FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS") || game.Story.Has("FLAG_CAUGHT_REGIGIGAS"));
        Assert.NotNull(game.Present("regigigas"));
    }

    [Fact]
    public void TheThreeRegisRoomsStayShut()
    {
        // Platinum opens them only to whoever travels with a Regigigas of one of its events (docs/mechanics/rulings.md):
        // the Rock Peak Ruins on Route 228, the Iceberg Ruins in Mt. Coronet and the Iron Ruins on Iron Island
        // Each is two ways on one tile, of which the original keeps one: an empty chamber for everyone else, and the one
        // with the statue that answers to that Regigigas
        foreach (var (area, plain, regis) in new[] { ("route_228", "route_228_rock_peak_ruins", "rock_peak_ruins"), ("mt_coronet_1f_north_room_2", "mt_coronet_iceberg_ruins", "iceberg_ruins"),
                     ("iron_island_b3f", "iron_island_iron_ruins", "iron_ruins") })
        {
            var ways = Sinnoh.Area(area)!.Warps.Select((w, i) => (Warp: w, Number: i)).Where(w => w.Warp.To.EndsWith("_ruins", StringComparison.Ordinal)).ToList();
            Assert.Equal(new[] { plain, regis }, ways.Select(w => w.Warp.To));
            Assert.Single(ways.Select(w => (w.Warp.X, w.Warp.Z)).Distinct());
            Assert.All(ways, w => Assert.Contains(w.Number, Sinnoh.Overlay(area)!.Locked!));
            Assert.All(ways, w => Assert.Null(Sinnoh.MapOf(w.Warp.To)));
        }
    }

    // ------------------------------------------------------------------ the nightmare and the Lunar Wing

    /// <summary>Speaks to Sailor Eldritch at Canalave's quay from beside his boat.</summary>
    private static HeadlessScriptHost AskTheSailor(OpeningTests.Game game, params int[] answers)
    {
        if (game.Map.Name != "Sinnoh" || AreaKey(game.Map, game.Tile.X, game.Tile.Y) != "canalave_city") game.Arrive("Sinnoh", 44, 751);
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        return TalkFrom(game, "sailor_eldritch", Direction.Right, "canalave_city");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void NothingIsTheMatterInTheSailorsHouseUntilAChampionWithTheNationalPokedexComesIn(bool champion, bool nationalDex)
    {
        var game = champion ? Champion(nationalDex: nationalDex) : new OpeningTests.Game { NationalDex = nationalDex };
        game.Arrive(House, 4, 8);
        Assert.Equal(0, game.Story.Var(Nightmare));
        Assert.False(game.Story.Has(InBed));
        Assert.NotNull(game.Present("little_boy"));
        Assert.Null(game.Present("little_boy_in_bed"));
        var mother = game.Present("pokefan_f")!;
        Assert.Equal((8, 4), (mother.GridX, mother.GridY));
        Assert.Contains("energy", Words(TalkFrom(game, "pokefan_f", Direction.Right)));
        // The sailor sails to Iron Island and nowhere else
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        var asked = AskTheSailor(game, 1);
        Assert.Equal("Fancy a trip across to Iron Island?", asked.Asked.Single().Question);
        Assert.DoesNotContain(asked.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
    }

    [Fact]
    public void TheSailorsSonLiesInANightmareThatOnlyTheLunarWingEnds()
    {
        var game = Champion();
        game.Arrive(House, 4, 8);
        // The first visit begins it: the boy in his bed, his mother beside it
        Assert.Equal(1, game.Story.Var(Nightmare));
        Assert.True(game.Story.Has(InBed));
        Assert.Null(game.Present("little_boy"));
        var boy = game.Present("little_boy_in_bed")!;
        var mother = game.Present("pokefan_f")!;
        Assert.Equal((9, 6, Direction.Left), (mother.GridX, mother.GridY, mother.Facing));
        Assert.Contains("nightmare", Words(TalkFrom(game, "pokefan_f", Direction.Down)));
        // Until he has been looked at his father sails as ever
        Assert.Equal("Fancy a trip across to Iron Island?", AskTheSailor(game, 1).Asked.Single().Question);

        // Looked at, from the one tile beside the bed
        game.Arrive(House, 4, 8);
        var looked = TalkFrom(game, "little_boy_in_bed", Direction.Right);
        Assert.Equal((7, 6), game.Tile);
        Assert.All(looked.Transcript, l => Assert.Null(l.Speaker));
        Assert.Contains("watching me", Words(looked));
        Assert.Equal(2, game.Story.Var(Nightmare));
        Assert.True(game.Story.Has(SeenTheBoy));
        Assert.Equal(2, TalkFrom(game, "little_boy_in_bed", Direction.Right).Transcript.Count);

        // His father asks for help at the quay; refused, he waits
        var refused = AskTheSailor(game, 1);
        Assert.Contains("for my boy", refused.Asked.Single().Question);
        Assert.Contains(refused.Transcript, l => l.Text.Contains("I'll be here"));
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        // Agreed to, he sails to Fullmoon Island's pier, as the original lands the player
        var sailed = AskTheSailor(game, 0);
        Assert.Contains(sailed.Transcript, l => l.Text.Contains("Lunar Wing"));
        Assert.Contains("warp Sinnoh 39 277", sailed.Log);
        Assert.Equal(Direction.Left, Find("Ferry", "canalave_city").Everything().Where(i => i.Op == Op.Warp && (i.X, i.Y) == (39, 277)).Select(i => i.Direction).Distinct().Single());
        Assert.Equal("fullmoon_island", AreaKey(game.Map, 39, 277));
        Assert.True(game.Map.IsWalkable(39, 277));
        Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_FULLMOON_ISLAND"));
        Assert.Equal(0x0208, game.Story.Var("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND"));

        // The island's sailor is the same man, waiting by his boat: without the wing he asks before he sails back
        game.Answers.Enqueue(1);
        var waits = TalkFrom(game, "sailor", Direction.Right, "fullmoon_island");
        Assert.Single(waits.Asked);
        Assert.DoesNotContain(waits.Log, l => l.StartsWith("warp", StringComparison.Ordinal));

        // Into the wood: Cresselia cries, shimmers and is gone to roam, and the Lunar Wing glitters where it rested
        game.Arrive("FullmoonIslandForest", 16, 20);
        var met = TalkFrom(game, "cresselia", Direction.Left);
        Assert.Contains("cry Cresselia", met.Log);
        Assert.DoesNotContain(met.Log, l => l.StartsWith("wildbattle", StringComparison.Ordinal));
        Assert.Contains("glittering", Words(met));
        var roamer = met.Encounters.Roamers[Roamers.SlotOf("Cresselia")!.Value];
        Assert.True(roamer.Active);
        Assert.Equal(("Cresselia", 50), (roamer.Species, roamer.Level));
        Assert.Contains(Roamers.PlaceOf(roamer), Roamers.Routes);
        Assert.Equal(Roamers.Roaming, game.Story.Var(Roamers.StateVariable("Cresselia")));
        Assert.True(game.Story.Has("FLAG_MET_CRESSELIA") && game.Story.Has("FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA"));
        Assert.False(game.Story.Has(WingFlag));
        Assert.Null(game.Present("cresselia"));
        var wing = game.Present("item_lunar_wing")!;
        Assert.Equal((16, 15), (wing.GridX, wing.GridY));

        // Picked up, it is a key item, and its ball is gone for good
        Assert.Contains("Lunar Wing", Words(game.Talk("item_lunar_wing")));
        Assert.Equal(1, game.Bag.GetQuantity(Item("Lunar Wing")));
        Assert.True(game.Story.Has(WingFlag));
        Assert.Null(game.Present("item_lunar_wing"));

        // The sailor sees it and sails home at once; at the quay he sends the player to his son
        game.Arrive("Sinnoh", 39, 277);
        var home = TalkFrom(game, "sailor", Direction.Right, "fullmoon_island");
        Assert.Empty(home.Asked);
        Assert.Contains("wake my boy", Words(home));
        Assert.Contains("warp Sinnoh 44 750", home.Log);
        Assert.Equal("canalave_city", AreaKey(game.Map, game.Tile.X, game.Tile.Y));
        var urged = AskTheSailor(game);
        Assert.Empty(urged.Asked);
        Assert.Contains("take it to my son", Words(urged));

        // The cure: the wing glows, the boy wakes, his father runs in, thanks the player and goes back to his boat
        game.Arrive(House, 4, 8);
        Assert.Equal(2, game.Story.Var(Nightmare));
        var cured = TalkFrom(game, "little_boy_in_bed", Direction.Right);
        Assert.Contains("began to glow", Words(cured));
        Assert.Contains(cured.Transcript, l => l.Speaker == null && l.Text.Contains("woke up"));
        Assert.Contains($"fanfare {PokemonPlatinumEngine.Audio.MusicRole.FanfareHeal}", cured.Log);
        Assert.Equal(0, game.Bag.GetQuantity(Item("Lunar Wing")));
        Assert.Equal(3, game.Story.Var(Nightmare));
        Assert.True(game.Story.Has(Woken));
        Assert.Equal(5, cured.Transcript.Count(l => l.Speaker == "Sailor Eldritch"));
        Assert.Contains(cured.Transcript, l => l.Speaker == "Sailor Eldritch" && l.Text.Contains("sail with me"));
        // The player stepped back from the bed to let him by, and he left by the door
        Assert.Equal((7, 7), game.Tile);
        var father = game.Map.Everyone.Single(n => n.Key == "sailor_eldritch");
        Assert.Equal((4, 8), (father.GridX, father.GridY));
        Assert.Null(game.Present("sailor_eldritch"));
        Assert.True(game.Story.Has(father.HiddenBy!));
        // While the player stays, the boy tells of his dream from his bed and his mother gives thanks
        Assert.Contains("dark Pokémon", Words(TalkFrom(game, "little_boy_in_bed", Direction.Right)));
        Assert.Equal("Little Boy", TalkFrom(game, "little_boy_in_bed", Direction.Right).Transcript[0].Speaker);
        Assert.Contains("never be enough", Words(TalkFrom(game, "pokefan_f", Direction.Down)));

        // The next visit, and every one after, all is as it was before
        foreach (int _ in new[] { 0, 1 })
        {
            game.Arrive(House, 4, 8);
            Assert.Equal(0, game.Story.Var(Nightmare));
            Assert.False(game.Story.Has(InBed));
            Assert.NotNull(game.Present("little_boy"));
            Assert.Null(game.Present("little_boy_in_bed"));
            Assert.Equal((8, 4), (game.Present("pokefan_f")!.GridX, game.Present("pokefan_f")!.GridY));
            Assert.Contains("energy", Words(TalkFrom(game, "pokefan_f", Direction.Right)));
        }

        // And Fullmoon Island is one of the places his father sails to from now on
        var where = AskTheSailor(game, 2);
        Assert.Equal(("Where shall I take you?", "Nowhere today"), where.Asked.Single());
        Assert.DoesNotContain(where.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        Assert.Contains("warp Sinnoh 100 502", AskTheSailor(game, 0).Log);
        Assert.Contains("warp Sinnoh 39 277", AskTheSailor(game, 1).Log);
    }

    [Theory]
    [InlineData(Direction.Up, 16, 14)]
    [InlineData(Direction.Down, 16, 15)]
    [InlineData(Direction.Left, 16, 15)]
    [InlineData(Direction.Right, 16, 15)]
    public void TheLunarWingLiesWhereItCanBeSeenFromWhicheverSideCresseliaWasSpokenTo(Direction facing, int x, int y)
    {
        var game = Champion();
        game.Arrive("FullmoonIslandForest", 16, 20);
        var cresselia = game.Present("cresselia")!;
        // It is walked up to from every side
        foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var (dx, dy) = FieldMovement.Delta(way);
            Assert.False(game.Map.IsSolid(cresselia.GridX - dx, cresselia.GridY - dy));
        }
        TalkFrom(game, "cresselia", facing);
        // From the south the ball would lie behind the player's own head, so Cresselia draws back first, as in the original
        var wing = game.Present("item_lunar_wing")!;
        Assert.Equal((x, y), (wing.GridX, wing.GridY));
        Assert.False(game.Map.IsSolid(x, y));
        Assert.Equal(1, game.Talk("item_lunar_wing").Bag.GetQuantity(Item("Lunar Wing")));
    }

    [Fact]
    public void ACresseliaKnockedOutIsSetLooseAgainAfterTheHallOfFameAndLeavesNothing()
    {
        var game = Champion();
        game.Arrive("FullmoonIslandForest", 16, 20);
        TalkFrom(game, "cresselia", Direction.Up);
        game.Talk("item_lunar_wing");
        string state = Roamers.StateVariable("Cresselia");
        // Met on a route and knocked out (Roamers.AfterBattle sets the state): it roams no more, and the wood is empty
        Run(game, "fullmoon_island_forest", $"setvar {state} {Roamers.Defeated}");
        game.Arrive("FullmoonIslandForest", 16, 20);
        Assert.Null(game.Present("cresselia"));

        // The Hall of Fame puts it back on its island, to be set loose again
        game.Arrive(Hall, 8, 15);
        Assert.Equal(Roamers.Reset, game.Story.Var(state));
        game.Arrive("FullmoonIslandForest", 16, 20);
        var again = TalkFrom(game, "cresselia", Direction.Left);
        Assert.True(again.Encounters.Roamers[Roamers.SlotOf("Cresselia")!.Value].Active);
        Assert.Equal(Roamers.Roaming, game.Story.Var(state));
        Assert.DoesNotContain("glittering", Words(again));
        Assert.True(game.Story.Has(WingFlag));
        Assert.Null(game.Present("item_lunar_wing"));
        Assert.Null(game.Present("cresselia"));

        // One that was caught stays caught
        Run(game, "fullmoon_island_forest", $"setvar {state} {Roamers.Captured}");
        game.Arrive(Hall, 8, 15);
        Assert.Equal(Roamers.Captured, game.Story.Var(state));
        game.Arrive("FullmoonIslandForest", 16, 20);
        Assert.Null(game.Present("cresselia"));
    }

    [Fact]
    public void TheRoamersAreTheOriginalsSixAtItsLevels()
    {
        // src/roaming_pokemon.c, RoamingPokemon_ActivateSlot: Mesprit and Cresselia at 50, Darkrai at 40, the three birds at 60
        Assert.Equal(new[] { ("Mesprit", 50), ("Cresselia", 50), ("Darkrai", 40), ("Moltres", 60), ("Zapdos", 60), ("Articuno", 60) }, Roamers.Slots);
        // Each is set loose by a scene of its own: Mesprit's cavern (plan 02 · S13), Cresselia's wood, Professor Oak's
        // three birds in Eterna City (plan 02 · S15, part a). Darkrai's belongs to one of the original's events
        var loosed = Scripts.All.SelectMany(s => s.Everything()).Where(i => i.Op == Op.Roamer).Select(i => i.Name).Distinct().Order().ToList();
        Assert.Equal(new[] { "Articuno", "Cresselia", "Mesprit", "Moltres", "Zapdos" }, loosed);
    }

    // ------------------------------------------------------------------ the Pokédex, the things and the walk

    [Theory]
    [InlineData("Heatran", "Stark Mountain", "The heart of the mountain")]
    [InlineData("Regigigas", "Snowpoint Temple", "Its deepest room")]
    [InlineData("Cresselia", "Fullmoon Island", "The island's wood, then roaming")]
    public void ThePokedexSaysWhereEachIsMet(string species, string place, string how)
    {
        var habitat = Assert.Single(Habitats.Sinnoh!.Of(species));
        Assert.Equal((place, how), (habitat.Name, habitat.How));
        Assert.True(habitat.Ways.HasFlag(HabitatWays.Special));
    }

    [Fact]
    public void TheMagmaStoneAndLookersRockAreThingsNoFieldMoveClears()
    {
        var stone = new NPC { NpcType = NPC.MagmaStoneType };
        var rock = new NPC { NpcType = NPC.FalseRockType };
        Assert.True(stone.IsThing && rock.IsThing);
        // Neither is an obstacle: Strength pushes no stone and Rock Smash breaks no rock here
        Assert.Null(stone.Obstacle);
        Assert.Null(rock.Obstacle);
        Assert.Equal((PropType.MagmaStone, PropType.CrackedRock), (stone.GymThing, rock.GymThing));
        // The rock is the cracked rock's own card; the stone has its own (style guide, "The Magma Stone")
        var card = ThingCards.Paint(PropType.MagmaStone);
        Assert.Equal((30, 28), (card.Width, card.Height));
        var texels = Enumerable.Range(0, card.Width * card.Height).Select(i => card.Get(i % card.Width, i / card.Width)).Where(c => c.A > 0).ToList();
        Assert.True(texels.Count > 400);
        // Dark crust, with cracks that glow from deep red to an ember; nothing of it is white, and nothing is marked as lit
        Assert.Contains(texels, c => (c.R, c.G, c.B) == (60, 46, 50));
        Assert.Contains(texels, c => (c.R, c.G, c.B) == (150, 44, 30));
        Assert.Contains(texels, c => (c.R, c.G, c.B) == (250, 158, 50));
        Assert.Contains(texels, c => (c.R, c.G, c.B) == (255, 224, 118));
        Assert.DoesNotContain(texels, c => c.R > 240 && c.G > 240 && c.B > 200);
        Assert.All(texels, c => Assert.Equal(255, c.A));
        Assert.Equal(ThingCards.FootOf(PropType.StrengthBoulder), ThingCards.FootOf(PropType.MagmaStone));
        Assert.Equal(30, ThingCards.Paint(PropType.CrackedRock).Width);
    }

    [Fact]
    public void TheWholeGamesWalkFindsFullmoonIslandByTheSailorAndTheTempleByItsDoor()
    {
        // Only Newmoon Island is still come to by the story alone (the original's event)
        Assert.Equal(new[] { ("Sinnoh", 152, 277) }, WorldWalk.StoryArrivals.Select(s => (s.Map, s.X, s.Y)));
        var maps = MapDatabase.Fresh();
        var reached = WorldWalk.From(maps, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!);
        var sinnoh = maps("Sinnoh");
        Assert.Contains((39, 277), reached[sinnoh]);
        Assert.Contains((16, 16), reached[maps("FullmoonIslandForest")]);
        Assert.Contains((8, 12), reached[maps("SnowpointTemple1F")]);
        Assert.Contains((11, 13), reached[maps("SnowpointTempleB5F")]);
        Assert.Contains((7, 7), reached[maps("StarkMountainRoom3")]);
        Assert.Contains((750, 232), reached[sinnoh]);
        // Newmoon Island is not
        Assert.DoesNotContain(reached[sinnoh], t => AreaKey(sinnoh, t.X, t.Y) == "newmoon_island");
        Assert.False(reached.ContainsKey(maps("NewmoonIslandForest")));
    }

    // ------------------------------------------------------------------ the part in order, and saves from before

    [Fact]
    public void StarkMountainIsWalkedInOrderFromItsFootToTheParting()
    {
        var game = AtTheMountain();
        game.Facing = Direction.Left;
        StepOnto(game, 752, 235, "GruntsGoIn");
        game.Facing = Direction.Up;
        game.Through("StarkMountainRoom1");
        Assert.Equal(1, game.Story.Var(Room1));
        game.Through("StarkMountainRoom2");
        Assert.Equal((42, 85), game.Tile);
        StepOnto(game, 42, 69, "BuckJoins");
        StepOnto(game, 47, 3, "BuckGoesAhead");
        int before = game.Played.Count;
        game.Through("StarkMountainRoom3");
        Assert.Equal(new[] { "stark_mountain_room_3.OnEnter", "stark_mountain_outside.OnEnter" }, game.Played.Skip(before).Select(p => p.Script));
        Assert.Equal(("Sinnoh", (751, 233)), (game.Map.Name, game.Tile));
        Assert.Equal((1, 2, 1, 3), (game.Story.Var(Room1), game.Story.Var(BuckState), game.Story.Var(Room3), game.Story.Var(Outside)));
        // Everything that happened was said by someone the scenes name, and nobody walked into a wall
        Assert.All(game.Played.Skip(before), p => Assert.Empty(p.Host.Problems));
    }

    [Fact]
    public void ASaveInPlayNeedsNothingNew()
    {
        // The part's people were hidden by common.ChapterTwelve already (story version 18), and the boy's bed, Candice's
        // street and the mountain's scenes go by flags and variables that are nought until their scenes
        var champion = new StoryState();
        champion.Set(Completed);
        champion.CompleteRegion("Sinnoh");
        StoryMigration.Upgrade(champion, 17, Array.Empty<Pokemon>(), Scripts);
        foreach (string hidden in new[] { "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_LOOKER",
                     "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN",
                     "FLAG_HIDE_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_SAILOR_ELDRITCH", WingFlag })
            Assert.True(champion.Has(hidden), hidden);
        Assert.False(champion.Has(InBed));
        Assert.Equal((0, 0, 0, 0, 0), (champion.Var(Outside), champion.Var(Room1), champion.Var(Room3), champion.Var(Temple), champion.Var(Nightmare)));

        // A game saved in the mountain's heart while nobody stood there finds the scene at the way in, as everyone does
        var game = Champion();
        game.Story.SetVar(Room1, 1);
        game.Story.SetVar(BuckState, 0);
        game.Facing = Direction.Down;
        game.Arrive("StarkMountainRoom3", 7, 9);
        Assert.Equal(1, game.Story.Var(Room3));
        Assert.True(game.Story.Has(Arrested));
        Assert.Equal(("Sinnoh", (751, 233)), (game.Map.Name, game.Tile));
    }
}
