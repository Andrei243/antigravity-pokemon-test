using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part f (plan 21 · H12; docs/mechanics/rulings.md, "The event islands, Devon's fossils and
/// Wattson's rematch"): the passes Lilycove's ferry desk gives for the Battle Frontier's prints, Navel Rock with Ho-Oh at
/// its peak and Lugia at its foot, Birth Island's triangle and Deoxys, the Devon Corporation's Fossil Regenerator, and
/// Wattson's rematch over Match Call. Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennEventIslandsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private static readonly string[] Islands =
    {
        "BirthIsland_Harbor", "BirthIsland_Exterior", "NavelRock_Harbor", "NavelRock_Exterior", "NavelRock_Entrance", "NavelRock_B1F",
        "NavelRock_Fork", "NavelRock_Up1", "NavelRock_Up2", "NavelRock_Up3", "NavelRock_Up4", "NavelRock_Top",
        "NavelRock_Down01", "NavelRock_Down02", "NavelRock_Down03", "NavelRock_Down04", "NavelRock_Down05", "NavelRock_Down06",
        "NavelRock_Down07", "NavelRock_Down08", "NavelRock_Down09", "NavelRock_Down10", "NavelRock_Down11", "NavelRock_Bottom",
    };

    /// <summary>Emerald's eleven places of Birth Island's triangle (sDeoxysRockCoords), the first where it stands.</summary>
    private static readonly (int X, int Y)[] RockPlaces =
        { (15, 12), (11, 14), (15, 8), (19, 14), (12, 11), (18, 11), (15, 14), (11, 14), (19, 14), (15, 15), (15, 10) };

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string map, int x, int y)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Story.Set("HoennHallOfFame");
            Story.SetBadges("Hoenn", 0xFF);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1)));
            Party.Add(new Pokemon(PokemonDatabase.Get("Manectric")!, 70, new Random(2)));
            Arrive(map, x, y);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc, answers);
        }

        public HeadlessScriptHost? StepAt(int x, int y)
        {
            Tile = (x, y);
            if (FieldScripts.TriggerAt(Map, x, y, Story) is not { } trigger) return null;
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.ScriptFileAt(x, y))!);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Map is { } there && there != Map) Arrive(there.Name, host.PlayerTile.X, host.PlayerTile.Y);
            return Last = host;
        }
    }

    [Fact]
    public void BirthIslandAndNavelRockAreHoennsAndJoinedLadderByLadder()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (string name in Islands) Assert.Contains(name, hoenn.Maps);
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        foreach (string name in Islands) Assert.True(built.ContainsKey(name), name);
        // The peak is reached from the fork by four ladders up, the foot by eleven down
        Assert.Equal("NavelRock_Up1", built["NavelRock_Fork"].GetWarpAt(4, 6)!.TargetMap);
        Assert.Equal("NavelRock_Down01", built["NavelRock_Fork"].GetWarpAt(22, 6)!.TargetMap);
        Assert.Equal("NavelRock_Top", built["NavelRock_Up4"].GetWarpAt(5, 5)!.TargetMap);
        Assert.Equal("NavelRock_Bottom", built["NavelRock_Down11"].GetWarpAt(5, 5)!.TargetMap);
        Assert.Equal("BirthIsland_Exterior", built["BirthIsland_Harbor"].GetWarpAt(8, 2)!.TargetMap);
        // The quays the ferry sets the player down on are open
        Assert.True(built["NavelRock_Harbor"].IsWalkable(8, 4));
        Assert.True(built["BirthIsland_Harbor"].IsWalkable(8, 4));
        // and every place of the triangle is sand to walk round
        foreach (var (x, y) in RockPlaces) Assert.Equal("birth_island_exterior", built["BirthIsland_Exterior"].AreaAt(x, y)?.Key);
    }

    [Fact]
    public void TheFerryDeskGivesTheMysticTicketForAPrintAndTheAuroraTicketForTwo()
    {
        var game = new Game("LilycoveCity_Harbor", 8, 11);
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        // No print, no pass: the ferry goes to Southern Island as before
        game.Talk("lilycove_harbor_attendant");
        Assert.False(game.Story.Has("FLAG_ENABLE_SHIP_NAVEL_ROCK"));
        Assert.Equal("SouthernIsland_Exterior", game.Map.Name);

        game.Arrive("LilycoveCity_Harbor", 8, 11);
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        var first = game.Talk("lilycove_harbor_attendant", 2);
        Assert.True(game.Story.Has("FLAG_ENABLE_SHIP_NAVEL_ROCK"));
        Assert.False(game.Story.Has("FLAG_ENABLE_SHIP_BIRTH_ISLAND"));
        Assert.Contains(first.Log, l => l.StartsWith("warp NavelRock_Harbor 8 4"));
        Assert.Equal("NavelRock_Harbor", game.Map.Name);

        // Birth Island without the Aurora Ticket is refused
        game.Arrive("LilycoveCity_Harbor", 8, 11);
        game.Talk("lilycove_harbor_attendant", 3);
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);

        // A second facility's print (a gold one counts) gives it, and the ferry sails there
        game.Story.SetVar("VAR_BATTLE_ARCADE_PRINT_STATE", 4);
        game.Talk("lilycove_harbor_attendant", 3);
        Assert.True(game.Story.Has("FLAG_ENABLE_SHIP_BIRTH_ISLAND"));
        Assert.Equal("BirthIsland_Harbor", game.Map.Name);

        // The sailors on both quays sail the player home
        game.Talk("birth_island_sailor");
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
        game.Talk("lilycove_harbor_attendant", 2);
        game.Talk("navel_rock_sailor");
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
    }

    [Fact]
    public void HoOhComesDownToTheStepBeforeThePeakAndIsGoneOnceBeatenOrCaught()
    {
        var game = new Game("NavelRock_Top", 13, 20);
        Assert.NotNull(game.Present("navel_rock_ho_oh"));
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        // Run from, it is back the next time
        game.Fight = BattleOutcome.Fled;
        game.StepAt(12, 10);
        Assert.Contains("wildbattle Ho-Oh 70 Fled", game.Last.Log);
        Assert.Null(game.Present("navel_rock_ho_oh"));
        game.Arrive("NavelRock_Top", 13, 20);
        Assert.NotNull(game.Present("navel_rock_ho_oh"));
        // Caught, it is gone for good and the step does nothing
        game.Fight = BattleOutcome.Caught;
        game.StepAt(12, 10);
        Assert.True(game.Story.Has("FLAG_CAUGHT_HO_OH"));
        game.Arrive("NavelRock_Top", 13, 20);
        Assert.Null(game.Present("navel_rock_ho_oh"));
        Assert.Null(game.StepAt(12, 10));
    }

    [Fact]
    public void LugiaAtTheFootIsMetWhenSpokenToAndGoneOnceBeaten()
    {
        var game = new Game("NavelRock_Bottom", 14, 19);
        Assert.NotNull(game.Present("navel_rock_lugia"));
        game.Talk("navel_rock_lugia");
        Assert.Contains("wildbattle Lugia 70 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_LUGIA"));
        game.Arrive("NavelRock_Bottom", 14, 19);
        Assert.Null(game.Present("navel_rock_lugia"));
    }

    [Fact]
    public void BirthIslandsTriangleMovesThroughItsElevenPlacesAndThenLetsDeoxysDown()
    {
        var game = new Game("BirthIsland_Exterior", 15, 23);
        var rock = game.Present("birth_island_exterior_rock")!;
        Assert.Null(game.Present("birth_island_deoxys"));
        Assert.Equal(RockPlaces[0], (rock.GridX, rock.GridY));
        for (int level = 1; level < RockPlaces.Length; level++)
        {
            game.Talk("birth_island_exterior_rock");
            Assert.Equal(level, game.Story.Var("VAR_DEOXYS_ROCK_LEVEL"));
            Assert.Equal(RockPlaces[level], (rock.GridX, rock.GridY));
        }
        // Coming back puts it at its first place again
        game.Arrive("BirthIsland_Exterior", 15, 23);
        Assert.Equal(0, game.Story.Var("VAR_DEOXYS_ROCK_LEVEL"));
        Assert.Equal(RockPlaces[0], (rock.GridX, rock.GridY));
        for (int level = 1; level < RockPlaces.Length; level++) game.Talk("birth_island_exterior_rock");

        // The eleventh touch breaks it and Deoxys comes down; run from, it is all there again next time
        game.Fight = BattleOutcome.Fled;
        game.Talk("birth_island_exterior_rock");
        Assert.Contains(game.Last.Log, l => l.StartsWith("wildbattle Deoxys") && l.EndsWith(" 30 Fled"));
        Assert.Null(game.Present("birth_island_exterior_rock"));
        game.Arrive("BirthIsland_Exterior", 15, 23);
        Assert.NotNull(game.Present("birth_island_exterior_rock"));
        Assert.False(game.Story.Has("FLAG_DEOXYS_ROCK_COMPLETE"));
        // Caught, both are gone for good
        game.Story.SetVar("VAR_DEOXYS_ROCK_LEVEL", 10);
        game.Fight = BattleOutcome.Caught;
        game.Talk("birth_island_exterior_rock");
        Assert.True(game.Story.Has("FLAG_BATTLED_DEOXYS"));
        game.Arrive("BirthIsland_Exterior", 15, 23);
        Assert.Null(game.Present("birth_island_exterior_rock"));
        Assert.Null(game.Present("birth_island_deoxys"));
    }

    [Fact]
    public void DevonsRegeneratorTakesAFossilAndHandsBackThePokemonAfterAWalk()
    {
        var game = new Game("RustboroCity_DevonCorp_2F", 14, 1);
        // With no fossil, the researcher only talks
        game.Talk("fossil_scientist");
        Assert.Equal(0, game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE"));

        game.Bag.AddItem(ItemDatabase.Get("Claw Fossil")!, 1);
        game.Talk("fossil_scientist", 0);
        Assert.Equal((1, 2), (game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE"), game.Story.Var("VAR_WHICH_FOSSIL_REVIVED")));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Claw Fossil")!));
        // Still at work while the player stays; ready once the floor is come to again
        game.Talk("fossil_scientist");
        Assert.Equal(1, game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE"));
        game.Arrive("RustboroCity_DevonCorp_2F", 14, 1);
        Assert.Equal(2, game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE"));
        game.Talk("fossil_scientist");
        Assert.Equal("Anorith", game.Party.Members[^1].Species.Name);
        Assert.Equal(20, game.Party.Members[^1].Level);
        Assert.True(game.Story.Has("FLAG_RECEIVED_REVIVED_FOSSIL_MON"));
        Assert.Equal(0, game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE"));

        // Both fossils in the bag: the question asks which, and the Root Fossil is Lileep
        game.Bag.AddItem(ItemDatabase.Get("Root Fossil")!, 1);
        game.Bag.AddItem(ItemDatabase.Get("Claw Fossil")!, 1);
        game.Talk("fossil_scientist", 0, 0);
        Assert.Equal(1, game.Story.Var("VAR_WHICH_FOSSIL_REVIVED"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Root Fossil")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Claw Fossil")!));
        // and a fossil of another region's is no fossil the regenerator takes
        Assert.True(SpeciesSources.Hoenn.Has("Lileep") && SpeciesSources.Hoenn.Has("Armaldo"));
    }

    [Fact]
    public void WattsonAsksForARematchOnlyOnceBackInHisGymAndGrowsStrongerEachTime()
    {
        var wattson = PhoneBook.Get("wattson")!;
        var story = new StoryState();
        story.Set("HoennHallOfFame");
        story.Set(Pokegear.ContactFlag("wattson"));
        story.Defeat("hoenn_wattson_1");
        var thursday = new DateTime(2026, 10, 29);
        Assert.Equal(DayOfWeek.Thursday, thursday.DayOfWeek);
        Assert.True(PhoneRematch.Wants(wattson, story, thursday, TimeOfDay.Day));
        Assert.False(PhoneRematch.Wants(wattson, story, thursday.AddDays(1), TimeOfDay.Day));
        Assert.Equal(new[] { "hoenn_wattson_2", "hoenn_wattson_3", "hoenn_wattson_4", "hoenn_wattson_5" }, TrainerDatabase.Get("hoenn_wattson_1")!.Rematches!);

        // Before New Mauville the call that would ask is only a talk, and takes the waiting back
        var game = new Game("MauvilleCity_Gym", 5, 3);
        game.Story.Set(Pokegear.ContactFlag("wattson"));
        game.Story.SetVar("VAR_PHONE_CALL", 2);
        game.Story.Set("FLAG_PHONE_REMATCH_WATTSON");
        game.Play(Scripts.Find("Wattson", "phone")!);
        Assert.False(game.Story.Has("FLAG_PHONE_REMATCH_WATTSON"));

        // Back in his Gym, the call sets the rematch waiting, and he battles one team stronger each time
        game.Story.Set("FLAG_WATTSON_REMATCH_AVAILABLE");
        game.Story.Unset("FLAG_HIDE_MAUVILLE_GYM_WATTSON");
        foreach (string team in new[] { "hoenn_wattson_2", "hoenn_wattson_3", "hoenn_wattson_4", "hoenn_wattson_5", "hoenn_wattson_5" })
        {
            game.Story.SetVar("VAR_PHONE_CALL", 2);
            game.Play(Scripts.Find("Wattson", "phone")!);
            Assert.True(game.Story.Has("FLAG_PHONE_REMATCH_WATTSON"));
            game.Arrive("MauvilleCity_Gym", 5, 3);
            game.Talk("wattson");
            Assert.Contains(game.Last.Log, l => l.StartsWith("battle") && l.Contains(team));
            Assert.False(game.Story.Has("FLAG_PHONE_REMATCH_WATTSON"));
        }
    }
}
