using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's first chapter (plan 21 · H2, its first part: "the van to Wally's catch") played through with no screen, as a
/// player meets it: the moving van and Mom, the clock, Dad on the television, the neighbours and the rival's room, the
/// little girl at the top of the road, Professor Birch chased on Route 101 and the choice of three from his bag, the
/// rival's battle on Route 103, the Pokédex and five Poké Balls, Mom's Running Shoes, Oldale Town's footprints and its
/// Potion, Petalburg's boy, Norman in his Gym and Wally's catch. Each scene starts as the game starts it (the place's
/// arrival, a step onto its trigger, a door, someone spoken to), on maps of this class's own.
/// </summary>
public class HoennStoryTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    /// <summary>A game of Hoenn of its own: its maps, story, team and bag, and the tile the player stands on.</summary>
    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public int Starter { get; init; }
        public PlayerLook Look { get; init; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public bool Impatient { get; init; }
        public List<(string Script, HeadlessScriptHost Host)> Played { get; } = new();
        public HeadlessScriptHost Last => Played[^1].Host;
        public string Rival => Look == PlayerLook.Girl ? "Brendan" : "May";

        public Game(int starter = 0, PlayerLook look = PlayerLook.Boy, BattleOutcome fight = BattleOutcome.Won, bool impatient = false)
        {
            Starter = starter;
            Look = look;
            Fight = fight;
            Impatient = impatient;
            foreach (var map in Hoenn.Value.BuildMaps()) maps[map.Name] = map;
            StoryMigration.BeginNewGame(Story, Scripts);
            var start = RegionDatabase.Get(RegionDatabase.Hoenn)!.Start!;
            Arrive(start.Map, start.X, start.Y);
        }

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

        /// <summary>Through a door of this map that leads to another (the one nearest the player, where there are several).</summary>
        public void Through(string target)
        {
            var warp = Map.Warps.Where(w => w.TargetMap.Equals(target, StringComparison.OrdinalIgnoreCase) && !w.Shut)
                .OrderBy(w => Math.Abs(w.SourceX - Tile.X) + Math.Abs(w.SourceY - Tile.Y)).First();
            Arrive(warp.TargetMap, warp.TargetX, warp.TargetY);
        }

        private StepTrigger TriggerOf(string script) => Map.Triggers.First(t => t.Script == script);

        public HeadlessScriptHost Step(string script)
        {
            var trigger = TriggerOf(script);
            Tile = (trigger.X, trigger.Y);
            Assert.Same(trigger, FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story));
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.Name)!);
        }

        public bool Fires(string script)
        {
            var trigger = TriggerOf(script);
            return FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story) == trigger;
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A tile of this map read by facing it (the bedroom's clock).</summary>
        public HeadlessScriptHost Read(int x, int y)
        {
            Tile = (x, y + 1);
            Assert.True(Map.TileScripts.TryGetValue((x, y), out var name), $"Nothing is read at {x},{y} of {Map.Name}");
            return Play(Scripts.Find(name!, Map.ScriptFileAt(x, y))!);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                StarterChoice = Starter,
                RivalName = Rival,
                PlayerLook = Look,
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            var runner = new ScriptRunner(Scripts, host) { Impatient = Impatient };
            runner.Start(script, subject);
            runner.RunToEnd();
            Played.Add((script.FullName, host));
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Log.LastOrDefault(l => l.StartsWith("warp ")) != null && host.Map is { } there && there != Map)
            {
                Map = there;
                Map.ForgetForced();
                Map.ApplyPresence(Story.Has);
                if (Scripts.In(Map.ScriptFileAt(Tile.X, Tile.Y), ScriptLibrary.OnEnter) is { } onEnter) Play(onEnter);
            }
            return host;
        }
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy, BattleOutcome.Won)]
    [InlineData(1, PlayerLook.Girl, BattleOutcome.Won)]
    [InlineData(2, PlayerLook.Boy, BattleOutcome.Won)]
    [InlineData(0, PlayerLook.Girl, BattleOutcome.Won)]
    public void TheChapterPlaysFromTheVanToWallysCatch(int starter, PlayerLook look, BattleOutcome fight)
    {
        var game = new Game(starter, look, fight);
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        string mine = hoenn.Starters[starter];

        // The van's ride, Mom at the door and in with her: nothing yet, no Pokémon, no Pokédex, no Running Shoes
        Assert.Equal("LittlerootTown_BrendansHouse_1F", game.Map.Name);
        Assert.Empty(game.Party.Members);
        Assert.Equal(0, game.Bag.AllItems.Count());
        Assert.False(game.Story.Has(StoryState.PokedexFlag));
        Assert.False(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Equal(4, game.Story.Var("VAR_LITTLEROOT_INTRO_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_LITTLEROOT_TOWN_BRENDANS_HOUSE_TRUCK"));
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Speaker == "Mom"));
        Assert.NotNull(game.Present("vigoroth_carrying_box"));

        // Out of the door before the clock is set, Mom turns the player back
        Assert.True(game.Fires("GoSeeRoom"));
        game.Step("GoSeeRoom");
        Assert.Equal(4, game.Story.Var("VAR_LITTLEROOT_INTRO_STATE"));

        // Upstairs, the clock; downstairs again, Dad on the television and the Vigoroth gone
        game.Through("LittlerootTown_BrendansHouse_2F");
        game.Read(5, 1);
        Assert.Equal(6, game.Story.Var("VAR_LITTLEROOT_INTRO_STATE"));
        game.Through("LittlerootTown_BrendansHouse_1F");
        Assert.Equal(7, game.Story.Var("VAR_LITTLEROOT_INTRO_STATE"));
        Assert.Null(game.Present("vigoroth_carrying_box"));
        Assert.False(game.Fires("GoSeeRoom"));

        // Outside, the girl at the top of the road turns back a player with no Pokémon
        game.Through("Hoenn");
        Assert.True(game.Fires("NeedPokemon"));
        game.Step("NeedPokemon");
        Assert.True(game.Fires("NeedPokemon"));

        // Next door, the professor's wife, and upstairs the rival comes in at the Poké Ball
        game.Through("LittlerootTown_MaysHouse_1F");
        Assert.Equal(2, game.Story.Var("VAR_LITTLEROOT_HOUSES_STATE_MAY"));
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains(game.Rival));
        game.Through("LittlerootTown_MaysHouse_2F");
        Assert.Equal(2, game.Story.Var("VAR_LITTLEROOT_RIVAL_STATE"));
        game.Talk("item_ball");
        Assert.Equal(3, game.Story.Var("VAR_LITTLEROOT_RIVAL_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_LITTLEROOT_TOWN_STATE"));
        Assert.Null(game.Present("item_ball"));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == game.Rival);

        // The girl now sends the player to help; Route 101's professor in trouble, and his bag
        game.Through("LittlerootTown_MaysHouse_1F");
        game.Through("Hoenn");
        Assert.False(game.Fires("NeedPokemon"));
        game.Step("GoSaveBirch");
        Assert.Equal(2, game.Story.Var("VAR_LITTLEROOT_TOWN_STATE"));
        game.Arrive("Hoenn", 130, 279);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE101_STATE"));
        game.Step("BirchInTrouble");
        Assert.Equal(2, game.Story.Var("VAR_ROUTE101_STATE"));
        Assert.True(game.Fires("DontLeaveMe"));
        var bag = game.Talk("birchs_bag");
        Assert.Contains("wildbattle Zigzagoon 2 Won", bag.Log);
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        Assert.True(game.Story.Has("FLAG_RESCUED_BIRCH"));
        Assert.Equal(3, game.Story.Var("VAR_ROUTE101_STATE"));

        // Into the lab: the professor's thanks, and the rival on Route 103
        Assert.Equal("LittlerootTown_ProfessorBirchsLab", game.Map.Name);
        Assert.Equal(3, game.Story.Var("VAR_BIRCH_LAB_STATE"));
        Assert.NotNull(game.Present("birchs_lab_birch"));
        Assert.Null(game.Present("birchs_lab_rival"));

        // Route 103: the rival's battle, with Emerald's team for the player's starter
        game.Arrive("Hoenn", 130, 224);
        var rival = game.Talk("route103_rival");
        string team = $"hoenn_{game.Rival.ToLowerInvariant()}_route_103_{mine.ToLowerInvariant()}";
        Assert.Contains(rival.Log, l => l.StartsWith($"battle {team}"));
        Assert.Equal(hoenn.RivalStarterFor(mine), Assert.Single(TrainerDatabase.Get(team)!.Party).Species);
        Assert.True(game.Story.Has("FLAG_DEFEATED_RIVAL_ROUTE103"));
        Assert.Equal(4, game.Story.Var("VAR_BIRCH_LAB_STATE"));
        Assert.Null(game.Present("route103_rival"));

        // Through Oldale, the rival runs on ahead
        game.Arrive("Hoenn", 129, 258);
        Assert.True(game.Fires("RivalWaiting"));
        game.Step("RivalWaiting");
        Assert.Equal(2, game.Story.Var("VAR_OLDALE_RIVAL_STATE"));
        Assert.False(game.Fires("BlockedPath") && game.Story.Has("FLAG_ADVENTURE_STARTED"));

        // The lab: the Pokédex, and five Poké Balls from the rival
        game.Arrive("LittlerootTown_ProfessorBirchsLab", 6, 11);
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        Assert.True(game.Story.Has("FLAG_ADVENTURE_STARTED"));
        Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Equal(5, game.Story.Var("VAR_BIRCH_LAB_STATE"));
        Assert.Equal(3, game.Story.Var("VAR_LITTLEROOT_TOWN_STATE"));

        // Home: Mom at the door with the Running Shoes
        game.Through("Hoenn");
        Assert.NotNull(game.Present("littleroot_mom"));
        Assert.True(game.Fires("RunningShoes"));
        game.Step("RunningShoes");
        Assert.True(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Equal(4, game.Story.Var("VAR_LITTLEROOT_TOWN_STATE"));
        Assert.False(game.Fires("RunningShoes"));

        // Oldale's west road is free, and the clerk's Potion
        game.Arrive("Hoenn", 126, 252);
        Assert.Equal(1, game.Story.Var("VAR_OLDALE_TOWN_STATE"));
        Assert.False(game.Fires("BlockedPath"));
        game.Talk("oldale_mart_employee");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));

        // Petalburg: the boy points out the Gym, Norman in its hall, Wally's catch, and Norman's rule
        game.Arrive("Hoenn", 49, 243);
        Assert.True(game.Fires("ShowGym"));
        game.Step("ShowGym");
        Assert.Equal(1, game.Story.Var("VAR_PETALBURG_CITY_STATE"));
        game.Arrive("Hoenn", 55, 241);
        game.Through("PetalburgCity_Gym");
        var norman = game.Present("petalburg_gym_norman");
        Assert.NotNull(norman);
        Assert.Equal((4, 107), (norman!.GridX, norman.GridY));
        game.Talk("petalburg_gym_norman");
        Assert.Contains(game.Played, p => p.Host.Log.Contains("catcher Wally Zigzagoon 7"));
        Assert.Contains(game.Played, p => p.Host.Log.Contains("catchinglesson Ralts 5 Caught"));
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        Assert.Equal(3, game.Story.Var("VAR_PETALBURG_CITY_STATE"));
        Assert.Equal(2, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        Assert.Equal("PetalburgCity_Gym", game.Map.Name);
        Assert.True(game.Story.Has("FLAG_HIDE_PETALBURG_CITY_WALLY"));
        Assert.Null(game.Present("petalburg_gym_wally"));
    }

    [Fact]
    public void ARivalBattleLostOnRouteOneOhThreeLeavesTheRivalWaiting()
    {
        var game = new Game();
        game.Story.SetVar("VAR_BIRCH_LAB_STATE", 3);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Treecko")!, 5));
        game.Story.ChooseStarter("Treecko");
        game.Arrive("Hoenn", 130, 224);
        game.Fight = BattleOutcome.Lost;
        game.Talk("route103_rival");
        Assert.False(game.Story.Has("FLAG_DEFEATED_RIVAL_ROUTE103"));
        Assert.Equal(3, game.Story.Var("VAR_BIRCH_LAB_STATE"));
    }

    [Fact]
    public void OldalesWestRoadIsShutUntilTheAdventureHasBegun()
    {
        var game = new Game();
        game.Arrive("Hoenn", 126, 252);
        Assert.True(game.Fires("BlockedPath"));
        var turned = game.Step("BlockedPath");
        Assert.NotEqual((120, 252), turned.PlayerTile);
        Assert.True(game.Fires("BlockedPath"));
    }

    [Fact]
    public void ANewGameOfHoennBeginsInTheVanWithTheChaptersPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        foreach (string flag in new[]
                 {
                     "FLAG_HIDE_LITTLEROOT_TOWN_MOM_OUTSIDE", "FLAG_HIDE_LITTLEROOT_TOWN_BIRCHS_LAB_BIRCH",
                     "FLAG_HIDE_LITTLEROOT_TOWN_BIRCHS_LAB_RIVAL", "FLAG_HIDE_OLDALE_TOWN_RIVAL", "FLAG_HIDE_PETALBURG_CITY_WALLY",
                     "FLAG_HIDE_PETALBURG_GYM_WALLY", "FLAG_HIDE_LITTLEROOT_TOWN_MAYS_HOUSE_RIVAL_BEDROOM"
                 })
            Assert.True(story.Has(flag), flag);
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        Assert.True(hoenn.HasStory);
        Assert.Equal(("Hoenn", 123, 292), (hoenn.Start!.Map, hoenn.Start.X, hoenn.Start.Y));
        Assert.Equal(("Hoenn", 125, 291), (hoenn.ArrivalSpot!.Map, hoenn.ArrivalSpot.X, hoenn.ArrivalSpot.Y));
        // The rival is the hero the player isn't, named by the look, and the introduction asks no name
        Assert.Null(hoenn.Rival);
        Assert.Equal("May", hoenn.RivalFor(PlayerLook.Boy));
        Assert.Equal("Brendan", hoenn.RivalFor(PlayerLook.Girl));
        Assert.Equal("MAY", PlayerIdentity.CharacterFor("Rival:MAY:BRENDAN", PlayerLook.Boy));
        Assert.Equal("BRENDAN", PlayerIdentity.CharacterFor("Rival:MAY:BRENDAN", PlayerLook.Girl));
        // Johto's and Sinnoh's rivals are named as they were
        Assert.Equal("Silver", RegionDatabase.Get(RegionDatabase.Johto)!.RivalFor(PlayerLook.Girl));
    }

    [Fact]
    public void AChampionWhoArrivesBeforeTheHouseIsNotPutInTheVan()
    {
        var game = new Game();
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        var host = new HeadlessScriptHost(story) { PlayerTile = (125, 291) };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.In("littleroot_town", ScriptLibrary.OnEnter)!);
        runner.RunToEnd();
        Assert.Equal(0, story.Var("VAR_LITTLEROOT_INTRO_STATE"));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp "));
    }

    [Fact]
    public void HoennsRolesHaveSongsOfTheirOwnAndSinnohsAreAsTheyWere()
    {
        Assert.Equal("hoenn/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/battle_trainer", MusicDirector.Resolve(MusicRole.BattleTrainer, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/battle_rival", MusicDirector.Resolve(MusicRole.BattleRival, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/victory_trainer", MusicDirector.Resolve(MusicRole.VictoryTrainer, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("common/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, "Sinnoh", MusicLibrary.Exists));
    }

    [Fact]
    public void ASaveOfHoennFromBeforeTheChapterIsTakenPastItsOpening()
    {
        // A game begun in Hoenn before H2 began in front of the house with a Treecko and no starter recorded
        var story = new StoryState();
        StoryMigration.Upgrade(story, 22, new[] { new Pokemon(PokemonDatabase.Get("Treecko")!, 5) }, Scripts);
        Assert.Equal("Treecko", story.PlayerStarter);
        Assert.Equal(5, story.Var("VAR_BIRCH_LAB_STATE"));
        Assert.Equal(7, story.Var("VAR_LITTLEROOT_INTRO_STATE"));
        Assert.True(story.Has(StoryState.PokedexFlag));
        Assert.True(story.Has(StoryState.RunningShoesFlag));
        // A game of Sinnoh is only given Hoenn's flags
        var sinnoh = new StoryState();
        StoryMigration.Upgrade(sinnoh, 22, Array.Empty<Pokemon>(), Scripts);
        Assert.Null(sinnoh.PlayerStarter);
        Assert.Equal(0, sinnoh.Var("VAR_BIRCH_LAB_STATE"));
        Assert.True(sinnoh.Has("FLAG_HIDE_PETALBURG_CITY_WALLY"));
    }
}
