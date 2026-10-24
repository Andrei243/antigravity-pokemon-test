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
        /// <summary>The turns a battle of the scripts to come takes (the host's BattleTurns), and what is done to the team in it.</summary>
        public int Turns { get; set; } = 3;
        public Action? DuringFight { get; set; }
        /// <summary>How the questions of the scripts to come are answered (left out, the first answer: yes).</summary>
        public Func<string, IReadOnlyList<string>, int>? Decide { get; set; }
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

        /// <summary>A step onto a tile, which starts its trigger if the story is at its value (null where nothing starts).</summary>
        public HeadlessScriptHost? StepAt(int x, int y)
        {
            Tile = (x, y);
            if (FieldScripts.TriggerAt(Map, x, y, Story) is not { } trigger) return null;
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.ScriptFileAt(x, y))!);
        }

        public bool Fires(string script)
        {
            var trigger = TriggerOf(script);
            return FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story) == trigger;
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            // (the nearest of that key: the overworld has a fisherman on Route 104 and another in Dewford Town)
            var npc = Map.NPCs.Where(n => n.Key == key).OrderBy(n => Math.Abs(n.GridX - Tile.X) + Math.Abs(n.GridY - Tile.Y)).First();
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
                Decide = Decide,
                NeedsPokemon = true
            };
            // (a battle's turns, and whatever happens to the team in it: Gabby and Ty ask after both)
            host.Fight = _ => { host.BattleTurns = Turns; DuringFight?.Invoke(); return Fight; };
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
        Assert.NotNull(game.Present("route101_birch"));
        Assert.NotNull(game.Present("route101_zigzagoon"));
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

    // ------------------------------------------------------------------ part b: Route 104 to Rusturf Tunnel

    /// <summary>A game past Wally's catch: the opening played, the starter chosen and on the team.</summary>
    private static Game PastWallysCatch(int starter, PlayerLook look, BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new Game(starter, look, fight);
        string mine = RegionDatabase.Get(RegionDatabase.Hoenn)!.Starters[starter];
        game.Play(Scripts.Find("HoennOpeningDone", ScriptLibrary.Common)!);
        game.Story.SetVar("VAR_PETALBURG_GYM_STATE", 2);
        game.Story.ChooseStarter(mine);
        game.Party.Add(new Pokemon(PokemonDatabase.Get(mine)!, 15));
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    [InlineData(2, PlayerLook.Girl)]
    public void TheSecondStretchPlaysFromTheWoodsToThePokeNav(int starter, PlayerLook look)
    {
        var game = PastWallysCatch(starter, look);
        string mine = RegionDatabase.Get(RegionDatabase.Hoenn)!.Starters[starter];

        // Petalburg Woods: the Devon researcher, the grunt, the battle and a Great Ball
        game.Arrive("PetalburgWoods", 26, 25);
        Assert.Null(game.Present("petalburg_woods_grunt"));
        Assert.NotNull(game.Present("petalburg_woods_devon_employee"));
        Assert.True(game.Fires("DevonResearcher"));
        var woods = game.Step("DevonResearcher");
        Assert.Contains(woods.Log, l => l.StartsWith("battle hoenn_grunt_petalburg_woods"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        Assert.Equal(1, game.Story.Var("VAR_PETALBURG_WOODS_STATE"));
        Assert.Null(game.Present("petalburg_woods_grunt"));
        Assert.Null(game.Present("petalburg_woods_devon_employee"));
        Assert.False(game.Fires("DevonResearcher"));

        // Rustboro's Gym: Roxanne, the Stone Badge and TM39; her trainers count as beaten, and Cut's gate is open
        game.Arrive("RustboroCity_Gym", 5, 16);
        var roxanne = game.Talk("roxanne");
        Assert.Contains(roxanne.Log, l => l.StartsWith("battle hoenn_roxanne_1"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Stone"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM39")!));
        Assert.Equal(1, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.Equal(3, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        Assert.True(game.Story.HasDefeated("hoenn_josh"));
        Assert.Equal("Stone", FieldMoveRules.BadgeFor(FieldMove.Cut, RegionDatabase.Hoenn));

        // The Cutter gives HM01 once
        game.Arrive("RustboroCity_CuttersHouse", 6, 6);
        game.Talk("pokefan_m");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        game.Talk("pokefan_m");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));

        // Out of the Gym, west along the road: the goods snatched and the chase sets up the tunnel
        game.Arrive("Hoenn", 24, 143);
        Assert.True(game.Fires("StolenGoods"));
        game.Step("StolenGoods");
        Assert.True(game.Story.Has("FLAG_DEVON_GOODS_STOLEN"));
        Assert.Equal(2, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.Equal(2, game.Story.Var("VAR_RUSTURF_TUNNEL_STATE"));
        Assert.NotNull(game.Present("rustboro_devon_employee"));
        Assert.False(game.Fires("StolenGoods"));
        Assert.True(game.Fires("HelpGetGoods"));
        game.Step("HelpGetGoods");
        Assert.Equal(3, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));

        // Route 116: Mr. Briney frantic at the tunnel's mouth
        game.Arrive("Hoenn", 87, 132);
        Assert.NotNull(game.Present("route116_briney"));
        Assert.True(game.Fires("BrineyTrigger"));
        game.Step("BrineyTrigger");
        Assert.Equal(2, game.Story.Var("VAR_ROUTE116_STATE"));

        // Rusturf Tunnel: the grunt cornered with Peeko, his battle, the goods back and Mr. Briney's thanks
        game.Arrive("RusturfTunnel", 7, 4);
        var grunt = game.Present("rusturf_tunnel_grunt");
        Assert.NotNull(grunt);
        Assert.Equal((13, 5), (grunt!.GridX, grunt.GridY));
        Assert.True(game.Fires("GruntBacksUp"));
        game.Step("GruntBacksUp");
        Assert.Equal(3, game.Story.Var("VAR_RUSTURF_TUNNEL_STATE"));
        var tunnel = game.Talk("rusturf_tunnel_grunt");
        Assert.Contains(tunnel.Log, l => l.StartsWith("battle hoenn_grunt_rusturf_tunnel"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Devon Goods")!));
        Assert.True(game.Story.Has("FLAG_RECOVERED_DEVON_GOODS"));
        Assert.False(game.Story.Has("FLAG_DEVON_GOODS_STOLEN"));
        Assert.Equal(4, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_BRINEY_HOUSE_STATE"));
        Assert.Null(game.Present("rusturf_tunnel_grunt"));
        Assert.Null(game.Present("rusturf_tunnel_peeko"));

        // Back in Rustboro: the employee's Great Ball, and up to the president with the PokéNav and the Letter
        game.Arrive("Hoenn", 30, 135);
        Assert.True(game.Fires("ReturnGoods"));
        game.Step("ReturnGoods");
        Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        Assert.True(game.Story.Has("FLAG_RETURNED_DEVON_GOODS"));
        Assert.Equal("RustboroCity_DevonCorp_3F", game.Map.Name);
        Assert.True(game.Story.Has(PokeNav.Flag));
        Assert.True(PokeNav.Has(game.Story, RegionDatabase.Hoenn));
        Assert.False(PokeNav.Has(game.Story, RegionDatabase.Sinnoh));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Letter")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Devon Goods")!));
        Assert.Equal(6, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_DEVON_CORP_3F_STATE"));

        // Out of Devon: Match Call, with Mom's and Mr. Stone's numbers
        game.Arrive("Hoenn", 11, 138);
        Assert.Equal(7, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.True(game.Story.Has(PokeNav.MatchCallFlag));
        Assert.Equal(new[] { "hoenn_mom", "mr_stone" }, PokeNav.Contacts(game.Story).Select(c => c.Id));
        Assert.Equal(new[] { PokeNavPage.Map, PokeNavPage.Condition, PokeNavPage.MatchCall }, PokeNav.Pages(game.Story, game.Party.Members));

        // The rival at the south edge: registered, asked to battle and beaten with Emerald's team for the starter
        game.Arrive("Hoenn", 14, 174);
        Assert.NotNull(game.Present("rustboro_rival"));
        Assert.True(game.Fires("RivalTrigger"));
        var rival = game.Step("RivalTrigger");
        string team = $"hoenn_{game.Rival.ToLowerInvariant()}_rustboro_{mine.ToLowerInvariant()}";
        Assert.Contains(rival.Log, l => l.StartsWith($"battle {team}"));
        Assert.Contains("addcontact hoenn_rival", rival.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_RIVAL_RUSTBORO"));
        Assert.Equal(8, game.Story.Var("VAR_RUSTBORO_CITY_STATE"));
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "hoenn_rival");
        Assert.False(game.Fires("RivalTrigger"));

        // Wanda's young man now digs in the tunnel; Mr. Briney and Peeko are home
        game.Arrive("RusturfTunnel", 7, 4);
        Assert.NotNull(game.Present("rusturf_tunnel_wandas_bf"));
        game.Arrive("Route104_MrBrineysHouse", 5, 6);
        Assert.NotNull(game.Present("brineys_house_briney"));
        Assert.NotNull(game.Present("brineys_house_peeko"));
    }

    [Fact]
    public void TheRivalInRustboroMayBeToldNotNowAndAsksAgain()
    {
        var game = PastWallysCatch(0, PlayerLook.Boy);
        game.Story.SetVar("VAR_RUSTBORO_CITY_STATE", 7);
        game.Story.Unset("FLAG_HIDE_RUSTBORO_CITY_RIVAL");
        game.Arrive("Hoenn", 14, 174);
        var host = PlayWithAnswer(game, "RivalTrigger", yes: false);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle "));
        Assert.True(game.Story.Has("FLAG_MET_RIVAL_RUSTBORO"));
        Assert.False(game.Story.Has("FLAG_DEFEATED_RIVAL_RUSTBORO"));
        Assert.False(game.Fires("RivalTrigger"));
        Assert.NotNull(game.Present("rustboro_rival"));
    }

    /// <summary>A trigger's script played with every question answered one way.</summary>
    private static HeadlessScriptHost PlayWithAnswer(Game game, string script, bool yes)
    {
        var trigger = game.Map.Triggers.First(t => t.Script == script);
        game.Tile = (trigger.X, trigger.Y);
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
        {
            Map = game.Map,
            PlayerTile = game.Tile,
            StarterChoice = game.Starter,
            RivalName = game.Rival,
            PlayerLook = game.Look,
            Fight = _ => BattleOutcome.Won,
            Decide = (_, _) => yes ? 0 : 1
        };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name)!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        game.Map.ApplyPresence(game.Story.Has);
        return host;
    }

    [Fact]
    public void ALostBattleWithTheGruntInTheWoodsLeavesTheResearcherWaiting()
    {
        var game = PastWallysCatch(0, PlayerLook.Boy, BattleOutcome.Lost);
        game.Arrive("PetalburgWoods", 26, 25);
        game.Step("DevonResearcher");
        Assert.Equal(0, game.Story.Var("VAR_PETALBURG_WOODS_STATE"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
    }

    [Fact]
    public void TheDevonStairsAreGuardedUntilTheGoodsAreBack()
    {
        var game = PastWallysCatch(0, PlayerLook.Boy);
        game.Arrive("RustboroCity_DevonCorp_1F", 6, 7);
        var guard = game.Present("devon_corp_stair_guard")!;
        Assert.Equal((14, 2), (guard.GridX, guard.GridY));
        game.Story.Set("FLAG_RETURNED_DEVON_GOODS");
        game.Arrive("RustboroCity_DevonCorp_1F", 6, 7);
        Assert.NotEqual((14, 2), (game.Present("devon_corp_stair_guard")!.GridX, game.Present("devon_corp_stair_guard")!.GridY));
    }

    [Fact]
    public void ASaveFromBeforeTheSecondStretchHasItsPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 23, Array.Empty<Pokemon>(), Scripts);
        Assert.True(story.Has("FLAG_HIDE_PETALBURG_WOODS_AQUA_GRUNT"));
        Assert.True(story.Has("FLAG_HIDE_RUSTURF_TUNNEL_WANDA"));
        Assert.True(story.Has("FLAG_HIDE_RUSTBORO_CITY_RIVAL"));
        Assert.True(StoryState.CurrentVersion >= 24);
    }

    [Fact]
    public void HoennsBerryTreesArePatchesOfTheirOwnPastSinnohs()
    {
        var map = Hoenn.Value.BuildMaps().Single(m => m.Name == "Hoenn");
        var soil = map.Everyone.Where(n => n.IsBerrySoil).ToList();
        Assert.NotEmpty(soil);
        Assert.All(soil, n => Assert.InRange(n.BerryPatch!.Value, BerryPatches.HoennFirst, BerryPatches.Count - 1));
        Assert.Equal(soil.Count, soil.Select(n => n.BerryPatch).Distinct().Count());
        // A new game finds Route 104's Cheri trees in fruit; a save from before Hoenn's trees is given them
        var fresh = BerryPatches.NewGame();
        Assert.Equal(BerryPatches.Count, fresh.Patches.Count);
        Assert.Equal("Cheri Berry", fresh[BerryPatches.HoennFirst + 2].Berry);
        var old = new BerryPatches { Patches = fresh.Patches.Take(BerryPatches.SinnohCount).ToList() };
        Assert.Equal(BerryPatches.Count, old.Grown().Patches.Count);
        Assert.Equal("Cheri Berry", old[BerryPatches.HoennFirst + 2].Berry);
    }

    [Fact]
    public void ThePokeNavsScreenMovesBetweenItsPagesAndRingsAContact()
    {
        var story = new StoryState();
        story.Set(PokeNav.Flag);
        story.Set(PokeNav.MatchCallFlag);
        Pokegear.Register(story, "hoenn_mom");
        Pokegear.Register(story, "mr_stone");
        var team = new List<Pokemon> { new(PokemonDatabase.Get("Treecko")!, 10), new(PokemonDatabase.Get("Wingull")!, 8) };
        var screen = new PokemonPlatinumEngine.UI.PokeNavScreen();
        screen.Open(PokeNav.Pages(story, team), PokeNav.Contacts(story), team);
        Assert.Equal(PokeNavPage.Map, screen.Page);
        screen.Move(1, 0);
        Assert.Equal(PokeNavPage.Condition, screen.Page);
        screen.Move(0, 1);
        Assert.Equal(1, screen.PokemonIndex);
        screen.Move(1, 0);
        Assert.Equal(PokeNavPage.MatchCall, screen.Page);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(PokemonPlatinumEngine.UI.PokeNavRequest.Call, screen.TakeRequest());
        Assert.Equal("mr_stone", screen.Called!.Id);
        // Mr. Stone and Mom are Hoenn's numbers: the Pokégear never lists them
        Assert.Empty(Pokegear.Contacts(story));
        Assert.NotNull(Scripts.Find("MrStone", GameEngine.PhoneScripts));
        Assert.NotNull(Scripts.Find("HoennMom", GameEngine.PhoneScripts));
        Assert.NotNull(Scripts.Find("HoennRival", GameEngine.PhoneScripts));
        // The ribbons' page is there once a Pokémon of the team wears one
        team[0].Ribbons.Add("RIBBON_EFFORT");
        Assert.Contains(PokeNavPage.Ribbons, PokeNav.Pages(story, team));
        // Hoenn has a map for its page
        Assert.NotNull(RegionMap.Of("hoenn"));
    }

    [Fact]
    public void TheSongsOfTheRoadToRustboroAreThere()
    {
        foreach (string song in new[] { "hoenn/route104", "hoenn/petalburg_woods", "hoenn/rustboro", "hoenn/rusturf" })
            Assert.True(MusicLibrary.Exists(song), song);
        Assert.Equal("hoenn/battle_gym", MusicDirector.Resolve(MusicRole.BattleGymLeader, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/victory_gym", MusicDirector.Resolve(MusicRole.VictoryGymLeader, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/battle_galactic", MusicDirector.Resolve(MusicRole.BattleGalactic, "Hoenn", MusicLibrary.Exists));
        Assert.Equal("hoenn/eye_galactic", MusicDirector.Resolve(MusicRole.EyeGalactic, "Hoenn", MusicLibrary.Exists));
        Assert.Equal(MusicRole.BattleGalactic, MusicDirector.BattleRoleOf("Team Aqua"));
        Assert.Equal(MusicRole.EyeGalactic, MusicDirector.EyeRole("Team Aqua"));
    }

    /// <summary>A game past the second stretch: Peeko safe, the Devon Goods and the Letter in the bag, the PokéNav had.</summary>
    private static Game PastThePokeNav(int starter, PlayerLook look)
    {
        var game = PastWallysCatch(starter, look);
        game.Story.SetVar("VAR_BRINEY_HOUSE_STATE", 1);
        game.Story.Set("FLAG_RECOVERED_DEVON_GOODS");
        game.Story.Set("FLAG_RETURNED_DEVON_GOODS");
        game.Story.Set(PokeNav.Flag);
        game.Story.Set(PokeNav.MatchCallFlag);
        game.Story.GiveBadge(RegionDatabase.Hoenn, "Stone");
        game.Story.SetVar("VAR_PETALBURG_GYM_STATE", 3);
        game.Bag.AddItem(ItemDatabase.Get("Letter")!);
        game.Bag.AddItem(ItemDatabase.Get("Devon Goods")!);
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheThirdStretchPlaysFromBrineysBoatToTheMuseum(int starter, PlayerLook look)
    {
        var game = PastThePokeNav(starter, look);

        // Mr. Briney's cottage: asked, he sails to Dewford Town's jetty, where he waits
        game.Arrive("Route104_MrBrineysHouse", 5, 6);
        var sail = game.Talk("brineys_house_briney");
        Assert.Contains(sail.Log, l => l.StartsWith("warp Hoenn 71 371"));
        Assert.Equal("Hoenn", game.Map.Name);
        Assert.Equal("dewford_town", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        Assert.Equal(2, game.Story.Var("VAR_BRINEY_LOCATION"));
        Assert.True(game.Story.Has("FLAG_VISITED_DEWFORD_TOWN"));
        Assert.NotNull(game.Present("dewford_briney"));

        // The fisherman's Old Rod, once
        game.Talk("fisherman");
        game.Talk("fisherman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Old Rod")!));

        // Before Steven has the letter, Mr. Briney only offers the way home (answered no)
        game.Decide = (_, _) => 1;
        var offer = game.Talk("dewford_briney");
        game.Decide = null;
        Assert.DoesNotContain(offer.Log, l => l.StartsWith("warp "));

        // Granite Cave: the hiker's Flash, then Steven takes the Letter, gives TM47, swaps numbers and leaves
        game.Arrive("GraniteCave_1F", 37, 11);
        game.Talk("hiker");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM70")!));
        game.Arrive("GraniteCave_StevensRoom", 7, 4);
        game.Talk("granite_cave_steven");
        Assert.True(game.Story.Has("FLAG_DELIVERED_STEVEN_LETTER"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Letter")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM47")!));
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "steven");
        Assert.Null(game.Present("granite_cave_steven"));
        game.Arrive("GraniteCave_StevensRoom", 7, 4);
        Assert.Null(game.Present("granite_cave_steven"));

        // Brawly's Gym: dark at first, lighter for each trainer beaten, lit once he is beaten
        game.Arrive("DewfordTown_Gym", 5, 26);
        Assert.Equal(7, DewfordLights.LevelFor(game.Story));
        DewfordLights.Keep(game.Map, game.Story, 0f);
        Assert.True(game.Map.IsDark);
        float darkest = game.Map.DarkReach;
        game.Story.Defeat("hoenn_takao");
        game.Story.Defeat("hoenn_jocelyn");
        Assert.Equal(5, DewfordLights.LevelFor(game.Story));
        DewfordLights.Keep(game.Map, game.Story, 0f);
        Assert.True(game.Map.DarkReach > darkest);
        var brawly = game.Talk("brawly");
        Assert.Contains(brawly.Log, l => l.StartsWith("battle hoenn_brawly_1"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Knuckle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM08")!));
        Assert.Equal(4, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        Assert.True(game.Story.HasDefeated("hoenn_lilith"));
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "brawly");
        Assert.Equal(0, DewfordLights.LevelFor(game.Story));
        DewfordLights.Keep(game.Map, game.Story, 0f);
        Assert.False(game.Map.IsDark);
        Assert.Equal("Knuckle", FieldMoveRules.BadgeFor(FieldMove.Flash, RegionDatabase.Hoenn));

        // Back at the jetty: Slateport now, across to Route 109's beach
        game.Arrive("Hoenn", 70, 371);
        game.Decide = (_, answers) => answers.ToList().FindIndex(a => a.Contains("Slateport"));
        var east = game.Talk("dewford_briney");
        game.Decide = null;
        Assert.Contains(east.Log, l => l.StartsWith("warp Hoenn 221 343"));
        Assert.Equal("route_109", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        Assert.Equal(3, game.Story.Var("VAR_BRINEY_LOCATION"));
        Assert.NotNull(game.Present("route109_briney"));
        game.Arrive("Hoenn", 72, 372);
        Assert.Null(game.Present("dewford_briney"));

        // Slateport: Team Aqua queues at the museum's door until Dock sends the player there
        game.Arrive("Hoenn", 219, 281);
        Assert.True(game.Story.Has("FLAG_VISITED_SLATEPORT_CITY"));
        Assert.NotNull(game.Present("slateport_grunt_1"));
        game.Arrive("SlateportCity_SternsShipyard_1F", 2, 13);
        game.Talk("dock");
        Assert.True(game.Story.Has("FLAG_DOCK_REJECTED_DEVON_GOODS"));
        game.Arrive("Hoenn", 219, 281);
        Assert.Null(game.Present("slateport_grunt_1"));

        // The museum: the fee, the familiar grunt's TM46, and upstairs the captain, two grunts, Archie and the goods
        game.Arrive("SlateportCity_OceanicMuseum_1F", 9, 7);
        Assert.True(game.Fires("PayEntranceFee"));
        game.Step("PayEntranceFee");
        Assert.Equal(1, game.Story.Var("VAR_SLATEPORT_MUSEUM_1F_STATE"));
        Assert.False(game.Fires("PayEntranceFee"));
        game.Talk("oceanic_museum_familiar_grunt");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM46")!));
        game.Arrive("SlateportCity_OceanicMuseum_2F", 6, 2);
        Assert.Null(game.Present("oceanic_museum_2f_archie"));
        Assert.Null(game.Present("old_man"));
        var museum = game.Talk("oceanic_museum_2f_capt_stern");
        Assert.Contains(museum.Log, l => l.StartsWith("battle hoenn_grunt_museum_1"));
        Assert.Contains(museum.Log, l => l.StartsWith("battle hoenn_grunt_museum_2"));
        Assert.True(game.Story.Has("FLAG_DELIVERED_DEVON_GOODS"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Devon Goods")!));
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_116_DEVON_EMPLOYEE"));
        Assert.Equal(1, game.Story.Var("VAR_SLATEPORT_OUTSIDE_MUSEUM_STATE"));
        Assert.Null(game.Present("oceanic_museum_2f_capt_stern"));
        Assert.Null(game.Present("oceanic_museum_2f_grunt_1"));
        Assert.NotNull(game.Present("old_man"));

        // Out of the door: Scott, and his number
        game.Arrive("Hoenn", 230, 287);
        Assert.Equal(2, game.Story.Var("VAR_SLATEPORT_OUTSIDE_MUSEUM_STATE"));
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "scott");
        Assert.Null(game.Present("slateport_scott"));

        // Mr. Briney on the beach sails back to Dewford when asked, the parcel delivered
        game.Arrive("Hoenn", 221, 343);
        game.Decide = (_, answers) => answers.ToList().FindIndex(a => a.Contains("Dewford"));
        var west = game.Talk("route109_briney");
        game.Decide = null;
        Assert.Contains(west.Log, l => l.StartsWith("warp Hoenn 71 371"));
        Assert.Equal(2, game.Story.Var("VAR_BRINEY_LOCATION"));

        // And home again to the cottage from Dewford
        game.Decide = (_, answers) => answers.ToList().FindIndex(a => a.Contains("104"));
        game.Talk("dewford_briney");
        game.Decide = null;
        Assert.Equal("Route104_MrBrineysHouse", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_BRINEY_LOCATION"));
        Assert.NotNull(game.Present("brineys_house_briney"));
    }

    [Fact]
    public void TheSeashoreHousesOwnerGivesSodaPopOnceItsTrainersAreBeaten()
    {
        var game = PastThePokeNav(0, PlayerLook.Boy);
        game.Arrive("Route109_SeashoreHouse", 6, 8);
        game.Talk("pokefan_m");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Soda Pop")!));
        foreach (string t in new[] { "hoenn_dwayne", "hoenn_simon", "hoenn_johanna" }) game.Story.Defeat(t);
        game.Talk("pokefan_m");
        Assert.Equal(6, game.Bag.GetQuantity(ItemDatabase.Get("Soda Pop")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_6_SODA_POP"));
    }

    [Fact]
    public void ASaveFromBeforeTheThirdStretchHasItsPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 24, Array.Empty<Pokemon>(), Scripts);
        Assert.True(story.Has("FLAG_HIDE_MR_BRINEY_DEWFORD_TOWN"));
        Assert.True(story.Has("FLAG_HIDE_SLATEPORT_CITY_OCEANIC_MUSEUM_2F_ARCHIE"));
        Assert.True(story.Has("FLAG_HIDE_SLATEPORT_MUSEUM_POPULATION"));
        Assert.True(StoryState.CurrentVersion >= 25);
    }

    [Fact]
    public void TheSongsOfDewfordAndSlateportAreThere()
    {
        foreach (string song in new[] { "hoenn/dewford", "hoenn/slateport", "hoenn/sailing" })
            Assert.True(MusicLibrary.Exists(song), song);
    }

    /// <summary>A game past the third stretch: the Devon Goods delivered in Slateport, two Badges won.</summary>
    private static Game PastTheMuseum(int starter, PlayerLook look)
    {
        var game = PastThePokeNav(starter, look);
        game.Story.Set("FLAG_DELIVERED_STEVEN_LETTER");
        game.Story.Set("FLAG_DELIVERED_DEVON_GOODS");
        game.Story.GiveBadge(RegionDatabase.Hoenn, "Knuckle");
        game.Story.SetVar("VAR_PETALBURG_GYM_STATE", 4);
        game.Bag.RemoveItem(ItemDatabase.Get("Letter")!, 1);
        game.Bag.RemoveItem(ItemDatabase.Get("Devon Goods")!, 1);
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheFourthStretchPlaysFromRouteOneTenToVerdanturf(int starter, PlayerLook look)
    {
        var game = PastTheMuseum(starter, look);
        string[] starters = { "treecko", "torchic", "mudkip" };
        string rival = look == PlayerLook.Girl ? "brendan" : "may";

        // Route 110: the rival waits by the road's north end and battles at the first step past them, then gives the
        // Itemfinder and rides off
        game.Arrive("Hoenn", 234, 218);
        Assert.NotNull(game.Present("route110_rival"));
        var met = game.StepAt(234, 216);
        Assert.NotNull(met);
        Assert.Contains(met!.Log, l => l.StartsWith($"battle hoenn_{rival}_route_110_{starters[starter]}"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Itemfinder")!));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE110_STATE"));
        Assert.Null(game.StepAt(233, 216));
        game.Arrive("Hoenn", 234, 218);
        Assert.Null(game.Present("route110_rival"));

        // The Trick House: the Trick Master under the desk, the scroll's code, the door, his reward
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Null(game.Present("trick_master"));
        game.Read(6, 3);
        Assert.True(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_PUZZLE_1_BEGUN"));
        Assert.Equal(1, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        game.Through("Route110_TrickHousePuzzle1");
        Assert.Equal("Route110_TrickHousePuzzle1", game.Map.Name);
        Assert.True(game.Map.GetWarpAt(13, 1) is null or { Shut: true });
        game.Read(13, 1);
        Assert.False(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_PUZZLE_1_UNLOCKED"));
        game.Read(3, 16);
        Assert.Equal(1, game.Story.Var("VAR_TRICK_HOUSE_PUZZLE_1_STATE"));
        game.Read(13, 1);
        Assert.True(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_PUZZLE_1_UNLOCKED"));
        game.Through("Route110_TrickHouseEnd");
        int candies = game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!);
        game.Talk("trick_master_end");
        Assert.Equal(candies + 1, game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        Assert.Equal(1, game.Story.Var("VAR_TRICK_HOUSE_LEVEL"));
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.NotNull(game.Present("trick_master"));
        Assert.True(game.Map.GetWarpAt(5, 2) is null or { Shut: true });

        // Mauville City: Wally asks for a battle at the Gym's door, loses, and goes to Verdanturf; Scott saw it
        game.Arrive("Hoenn", 222, 147);
        Assert.True(game.Story.Has("FLAG_VISITED_MAUVILLE_CITY"));
        Assert.NotNull(game.Present("mauville_wally"));
        var wally = game.Talk("mauville_wally");
        Assert.Contains(wally.Log, l => l.StartsWith("battle hoenn_wally_mauville"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_WALLY_MAUVILLE"));
        Assert.Equal(1, game.Story.Var("VAR_SCOTT_STATE"));
        Assert.Null(game.Present("mauville_wally"));
        Assert.Null(game.Present("mauville_scott"));

        // Rydel's two bikes: the Mach Bike first, then swapped for the Acro Bike
        game.Arrive("MauvilleCity_BikeShop", 3, 7);
        game.Talk("man_2");
        Assert.True(game.Story.Has("FLAG_RECEIVED_BIKE"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Mach Bike")!));
        game.Talk("man_2");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Mach Bike")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Acro Bike")!));

        // HM06 from the man who smashes rocks
        game.Arrive("MauvilleCity_House1", 3, 6);
        game.Talk("scientist_1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM06")!));

        // The Gym: every barrier flips at a switch, and Wattson's Badge turns them all off
        game.Arrive("MauvilleCity_Gym", 4, 19);
        Assert.True(game.Story.Has("FLAG_HOENN_MAUVILLE_GYM_BEAMS_A"));
        Assert.NotNull(game.Present("beam_a_1"));
        Assert.Null(game.Present("beam_b_1"));
        game.StepAt(0, 15);
        Assert.True(game.Story.Has("FLAG_MAUVILLE_GYM_BARRIERS_STATE"));
        Assert.Null(game.Present("beam_a_1"));
        Assert.NotNull(game.Present("beam_b_1"));
        game.StepAt(0, 15);
        Assert.True(game.Story.Has("FLAG_MAUVILLE_GYM_BARRIERS_STATE"));
        var wattson = game.Talk("wattson");
        Assert.Contains(wattson.Log, l => l.StartsWith("battle hoenn_wattson_1"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Dynamo"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM34")!));
        Assert.Equal(5, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        Assert.True(game.Story.HasDefeated("hoenn_angelo"));
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "wattson");
        Assert.Null(game.Present("beam_a_1"));
        Assert.Null(game.Present("beam_b_1"));
        Assert.Equal("Dynamo", FieldMoveRules.BadgeFor(FieldMove.RockSmash, RegionDatabase.Hoenn));

        // Rusturf Tunnel: both rocks smashed, the young man's thanks and HM04; the two go home to Verdanturf
        game.Arrive("RusturfTunnel", 22, 4);
        game.StepAt(23, 4);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("HM04")!));
        game.Story.Set("FLAG_HIDE_RUSTURF_TUNNEL_ROCK_1");
        game.Story.Set("FLAG_HIDE_RUSTURF_TUNNEL_ROCK_2");
        game.StepAt(23, 4);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM04")!));
        Assert.True(game.Story.Has("FLAG_RUSTURF_TUNNEL_OPENED"));
        Assert.Null(game.Present("rusturf_tunnel_wanda"));
        game.StepAt(23, 4);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM04")!));

        // Verdanturf Town: Wally, his uncle, Wanda and her young man in Wanda's house
        game.Arrive("Hoenn", 130, 155);
        Assert.True(game.Story.Has("FLAG_VISITED_VERDANTURF_TOWN"));
        game.Arrive("VerdanturfTown_WandasHouse", 7, 6);
        foreach (string key in new[] { "wally", "pokefan_m", "black_belt", "woman_2" }) Assert.NotNull(game.Present(key));
        game.Talk("wally");
        Assert.True(game.Story.Has("FLAG_WALLY_SPEECH"));

        // Route 117: a trainer beaten swaps numbers
        game.Arrive("Hoenn", 178, 157);
        game.Talk("running_triathlete_m");
        Assert.Contains(PokeNav.Contacts(game.Story), c => c.Id == "hoenn_dylan");
    }

    [Fact]
    public void ASaveFromBeforeTheFourthStretchHasItsPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 26, Array.Empty<Pokemon>(), Scripts);
        Assert.True(story.Has("FLAG_HIDE_VERDANTURF_TOWN_WANDAS_HOUSE_WALLY"));
        Assert.True(story.Has("FLAG_HIDE_TRICK_HOUSE_ENTRANCE_MAN"));
        Assert.True(story.Has("FLAG_HIDE_MAUVILLE_CITY_SCOTT"));
        Assert.True(StoryState.CurrentVersion >= 27);
    }

    [Fact]
    public void TheSongsOfMauvilleAndVerdanturfAreThere()
    {
        foreach (string song in new[] { "hoenn/route110", "hoenn/verdanturf", "hoenn/trick_house", "hoenn/bicycle" })
            Assert.True(MusicLibrary.Exists(song), song);
        Assert.Equal("hoenn/bicycle", MusicDirector.Resolve(MusicRole.Bicycle, "Hoenn", MusicLibrary.Exists));
    }

    private static Game PastVerdanturf(int starter, PlayerLook look)
    {
        var game = PastTheMuseum(starter, look);
        game.Story.GiveBadge(RegionDatabase.Hoenn, "Dynamo");
        game.Story.Set("FLAG_DEFEATED_WALLY_MAUVILLE");
        game.Story.Set("FLAG_RUSTURF_TUNNEL_OPENED");
        game.Story.SetVar("VAR_PETALBURG_GYM_STATE", 5);
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheFifthStretchPlaysFromTheWinstratesToMeteorFalls(int starter, PlayerLook look)
    {
        var game = PastVerdanturf(starter, look);

        // Route 111: the Winstrates, one after another at their door, and Victoria's Macho Brace inside
        game.Arrive("Hoenn", 213, 118);
        Assert.NotNull(game.Present("route111_victor"));
        Assert.Null(game.Present("route111_victoria"));
        var family = game.Talk("route111_victor");
        foreach (string id in new[] { "hoenn_victor", "hoenn_victoria", "hoenn_vivi", "hoenn_vicky" })
        {
            Assert.Contains(family.Log, l => l.StartsWith($"battle {id}"));
            Assert.True(game.Story.HasDefeated(id), id);
        }
        foreach (string key in new[] { "route111_victor", "route111_victoria", "route111_vivi", "route111_vicky" })
            Assert.Null(game.Present(key));
        game.Arrive("Route111_WinstrateFamilysHouse", 3, 6);
        game.Talk("winstrate_house_victoria");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Macho Brace")!));
        game.Talk("winstrate_house_victoria");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Macho Brace")!));

        // The desert's storm turns back anyone without Go-Goggles, a step down at its south edge and left at its west
        game.Arrive("Hoenn", 212, 63);
        var pushed = game.StepAt(212, 61);
        Assert.NotNull(pushed);
        Assert.Equal((212, 62), game.Tile);
        Assert.Contains(pushed!.Transcript, line => line.Text.Contains("sandstorm"));
        Assert.Equal(FieldWeather.Sandstorm, game.Map.WeatherAt(225, 60));
        Assert.Equal("hoenn/desert", game.Map.BgmTrackAt(225, 60));
        Assert.Equal(FieldWeather.Clear, game.Map.WeatherAt(205, 90));

        // The old lady's rest stop heals, twice when asked
        game.Arrive("Route111_OldLadysRestStop", 3, 6);
        var rest = game.Talk("expert_f");
        Assert.Equal(2, rest.Log.Count(l => l == "fanfare FanfareHeal"));

        // Route 112: Team Magma's two grunts before the cable car, who talk of Fallarbor and a meteorite
        game.Arrive("Hoenn", 188, 52);
        Assert.NotNull(game.Present("route112_grunt_1"));
        var grunts = game.Talk("route112_grunt_1");
        Assert.Contains(grunts.Transcript, line => line.Text.Contains("Fallarbor"));

        // Route 113: the glass workshop's Soot Sack, the ash gathered and a flute made of it
        Assert.Equal(FieldWeather.Ash, game.Map.WeatherAt(150, 10));
        game.Arrive("Route113_GlassWorkshop", 3, 7);
        game.Talk("man_1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Soot Sack")!));
        Assert.Equal(1, game.Story.Var("VAR_GLASS_WORKSHOP_STATE"));
        game.Arrive("Route113_GlassWorkshop", 3, 7);
        Assert.Equal(2, game.Story.Var("VAR_GLASS_WORKSHOP_STATE"));
        game.Story.SetVar(GameEngine.AshCountVariable, 300);
        game.Talk("man_1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Blue Flute")!));
        Assert.Equal(50, game.Story.Var(GameEngine.AshCountVariable));

        // Fallarbor Town, and Lanette in its Pokémon Center, who goes home to Route 114
        game.Arrive("Hoenn", 90, 12);
        Assert.True(game.Story.Has("FLAG_VISITED_FALLARBOR_TOWN"));
        game.Arrive("FallarborTown_PokemonCenter_1F", 7, 7);
        game.Talk("fallarbor_lanette");
        Assert.True(game.Story.Has("FLAG_HIDE_FALLARBOR_POKEMON_CENTER_LANETTE"));
        Assert.Null(game.Present("fallarbor_lanette"));
        game.Arrive("Route114_LanettesHouse", 5, 6);
        Assert.NotNull(game.Present("woman_2"));
        game.Talk("woman_2");
        Assert.True(game.Story.Has("FLAG_SYS_PC_LANETTE"));

        // Cozmo's wife says where he went; he isn't home
        game.Arrive("FallarborTown_CozmosHouse", 3, 7);
        Assert.Null(game.Present("scientist_1"));
        Assert.Contains(game.Talk("woman_2").Transcript, line => line.Text.Contains("Meteor Falls"));

        // Route 114: the gentleman's TM05 and the Fossil Maniac's brother's TM28
        game.Arrive("Hoenn", 59, 15);
        game.Talk("gentleman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM05")!));
        game.Arrive("Route114_FossilManiacsHouse", 4, 6);
        game.Talk("ninja_boy");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM28")!));

        // Meteor Falls: Team Magma takes the meteorite, Team Aqua comes after them, and Route 112 is clear
        game.Arrive("MeteorFalls_1F_1R", 27, 17);
        Assert.NotNull(game.Present("meteor_falls_magma_grunt_1"));
        Assert.Null(game.Present("meteor_falls_archie"));
        var scene = game.StepAt(14, 18);
        Assert.NotNull(scene);
        Assert.Equal(1, game.Story.Var("VAR_METEOR_FALLS_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_112_TEAM_MAGMA", "FLAG_MET_ARCHIE_METEOR_FALLS", "FLAG_HIDE_METEOR_FALLS_TEAM_MAGMA", "FLAG_HIDE_METEOR_FALLS_TEAM_AQUA" })
            Assert.True(game.Story.Has(flag), flag);
        foreach (string key in new[] { "meteor_falls_magma_grunt_1", "meteor_falls_archie", "meteor_falls_aqua_grunt_1" })
            Assert.Null(game.Present(key));
        Assert.NotNull(game.Present("scientist_1"));
        Assert.Null(game.StepAt(14, 18));
        game.Arrive("Hoenn", 188, 52);
        Assert.Null(game.Present("route112_grunt_1"));
    }

    [Fact]
    public void MauvillesGameCornerTakesCoinsFromACoinCaseTradedForHarborMail()
    {
        var game = PastVerdanturf(0, PlayerLook.Boy);
        game.Arrive("MauvilleCity_House2", 3, 6);
        game.Talk("woman_3");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!));
        game.Bag.AddItem(ItemDatabase.Get("Harbor Mail")!, 1);
        game.Talk("woman_3");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Harbor Mail")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_COIN_CASE"));
        Assert.Contains("Harbor Mail", MartDatabase.Stock("slateport_mail", 0).Select(i => i.Name));

        // The Game Corner's door is open, its man gives 20 Coins to a case with none, and its TMs are Emerald's five
        game.Arrive("MauvilleCity_GameCorner", 11, 9);
        Assert.True(game.Story.Has("FLAG_RECEIVED_COIN_CASE"));
        game.Talk("pokefan_m");
        Assert.True(game.Story.Has("FLAG_RECEIVED_20_COINS"));
        Assert.Equal(new[] { "TM32", "TM29", "TM35", "TM24", "TM13" }, GameCornerPrizes.Mauville.Select(p => p.Item));
        Assert.Equal(12, game.Map.TileScripts.Values.Count(s => s.StartsWith("Machine")));
    }

    [Fact]
    public void ARockSmashedInHoennMayHideAPokemonAndPlatinumsHideNone()
    {
        var world = Hoenn.Value;
        var overworld = world.BuildMaps().Single(m => m.Name == "Hoenn");
        int met = 0, tries = 4000;
        for (int i = 0; i < tries; i++)
            if (overworld.SmashOut(218, 101) is { } wild)
            {
                met++;
                Assert.Equal("Geodude", wild.SpeciesName);
                Assert.InRange(wild.MinLevel, 5, 20);
            }
        // Emerald's rate of 20 in sixteenths out of 2,880: one rock in nine
        Assert.InRange(met, tries / 9 - 150, tries / 9 + 150);
        Assert.False(WildEncounterRules.RockSmashMeets(0, null, new Random(1)));
        // No place of Platinum's keeps Pokémon under its rocks
        foreach (var sinnoh in World.LoadAll().Where(w => w.Index.Region == "Sinnoh"))
            foreach (string key in sinnoh.Index.Areas)
                Assert.Null(sinnoh.Area(key)?.RockSmash);
    }

    [Fact]
    public void ASaveFromBeforeTheFifthStretchHasItsPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 27, Array.Empty<Pokemon>(), Scripts);
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_111_VICTORIA_WINSTRATE", "FLAG_HIDE_METEOR_FALLS_TEAM_AQUA", "FLAG_HIDE_LANETTES_HOUSE_LANETTE", "FLAG_HIDE_FALLARBOR_HOUSE_PROF_COZMO" })
            Assert.True(story.Has(flag), flag);
        Assert.True(StoryState.CurrentVersion >= 28);
    }

    [Fact]
    public void TheSongsOfTheDesertAndFallarborAreThere()
    {
        foreach (string song in new[] { "hoenn/desert", "hoenn/route113", "hoenn/fallarbor", "hoenn/cave_of_origin", "hoenn/game_corner" })
            Assert.True(MusicLibrary.Exists(song), song);
    }

    /// <summary>Past the fifth stretch: Team Magma gone from Meteor Falls with the meteorite (plan 21 · H4, part a).</summary>
    private static Game PastMeteorFalls(int starter, PlayerLook look)
    {
        var game = PastVerdanturf(starter, look);
        game.Arrive("MeteorFalls_1F_1R", 27, 17);
        Assert.NotNull(game.StepAt(14, 18));
        Assert.Equal(1, game.Story.Var("VAR_METEOR_FALLS_STATE"));
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheSixthStretchPlaysFromMtChimneyToNorman(int starter, PlayerLook look)
    {
        var game = PastMeteorFalls(starter, look);

        // Mt. Chimney's summit: Maxie beaten at his machine, Team Magma and Team Aqua gone, the meteorite taken back
        game.Arrive("MtChimney", 13, 7);
        Assert.NotNull(game.Present("mt_chimney_maxie"));
        Assert.Null(game.Present("old_woman"));
        var maxie = game.Talk("mt_chimney_maxie");
        Assert.Contains(maxie.Log, l => l.StartsWith("battle hoenn_maxie_mt_chimney"));
        foreach (string flag in new[] { "FLAG_DEFEATED_EVIL_TEAM_MT_CHIMNEY", "FLAG_HIDE_MT_CHIMNEY_TEAM_MAGMA", "FLAG_HIDE_MT_CHIMNEY_TEAM_AQUA", "FLAG_HIDE_METEOR_FALLS_1F_1R_COZMO" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.Story.Has("FLAG_HIDE_FALLARBOR_HOUSE_PROF_COZMO"));
        Assert.Null(game.Present("mt_chimney_maxie"));
        Assert.Null(game.Present("mt_chimney_archie"));
        Assert.NotNull(game.Present("old_woman"));
        var machine = game.Map.TileScripts.First(kv => kv.Value == "MeteoriteMachine").Key;
        game.Read(machine.X, machine.Y);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Meteorite")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_METEORITE"));

        // Flannery's Gym: the Heat Badge, TM50 and the town's state for the rival's Go-Goggles
        game.Arrive("LavaridgeTown_Gym_1F", 13, 17);
        game.Talk("flannery");
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Heat"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM50")!));
        Assert.Equal(1, game.Story.Var("VAR_LAVARIDGE_TOWN_STATE"));
        Assert.Equal(6, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        foreach (string id in new[] { "hoenn_cole", "hoenn_gerald", "hoenn_axle", "hoenn_danielle", "hoenn_jace", "hoenn_keegan", "hoenn_jeff", "hoenn_eli" })
            Assert.True(game.Story.HasDefeated(id), id);

        // Out of the Gym's door, the rival with the Go-Goggles; the mountain's trainers are back up there
        game.Arrive("Hoenn", 145, 76);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Go-Goggles")!));
        Assert.Equal(2, game.Story.Var("VAR_LAVARIDGE_TOWN_STATE"));
        Assert.Null(game.Present("lavaridge_rival"));
        Assert.False(game.Story.Has("FLAG_HIDE_MT_CHIMNEY_TRAINERS"));

        // The desert lets the player through with the goggles on
        game.Arrive("Hoenn", 212, 63);
        game.StepAt(212, 61);
        Assert.Equal((212, 61), game.Tile);

        // Mirage Tower: a fossil taken, the other lost, and the tower gone
        game.Arrive("MirageTower_4F", 5, 5);
        game.Talk("mirage_root_fossil");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Root Fossil")!));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Claw Fossil")!));
        Assert.True(game.Story.Has("FLAG_HOENN_MIRAGE_TOWER_FALLEN"));
        Assert.Equal("Hoenn", game.Map.Name);

        // Petalburg's Gym: Norman waits in his room, the hall's doors open to four Badges and the rooms' to their trainers
        game.Arrive("PetalburgCity_Gym", 4, 109);
        Assert.Equal(2, game.Present("petalburg_gym_norman")!.GridY);
        game.Read(1, 105);
        Assert.Equal((7, 84), game.Tile);
        var shut = game.Read(1, 79);
        Assert.Contains(shut.Transcript, l => l.Text.Contains("locked"));
        game.Talk("man_3");
        Assert.True(game.Story.HasDefeated("hoenn_randall"));
        game.Read(1, 79);
        Assert.Equal((7, 45), game.Tile);

        // Norman's battle, the Balance Badge and TM42, and Wally's father's HM03 at home
        var norman = game.Talk("petalburg_gym_norman");
        Assert.Contains(norman.Log, l => l.StartsWith("battle hoenn_norman_1"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Balance"));
        Assert.Equal(7, game.Story.Var("VAR_PETALBURG_GYM_STATE"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM42")!));
        foreach (string id in new[] { "hoenn_randall", "hoenn_mary", "hoenn_parker", "hoenn_alexia", "hoenn_george", "hoenn_jody", "hoenn_berke" })
            Assert.True(game.Story.HasDefeated(id), id);
        Assert.False(game.Story.Has("FLAG_HIDE_MAUVILLE_CITY_WATTSON"));
        Assert.Equal("PetalburgCity_WallysHouse", game.Map.Name);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM03")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_HM_SURF"));
        Assert.Equal(5, game.Story.Var("VAR_PETALBURG_CITY_STATE"));
    }

    [Fact]
    public void PetalburgsGymDoorsAreShutUntilFourBadges()
    {
        var game = PastTheMuseum(0, PlayerLook.Boy);
        game.Arrive("PetalburgCity_Gym", 4, 109);
        Assert.Equal(107, game.Present("petalburg_gym_norman")!.GridY);
        var speed = game.Read(1, 105);
        Assert.Contains(speed.Transcript, l => l.Text.Contains("locked"));
        Assert.Equal((1, 106), game.Tile);
        // Each room's own mat leads back to the floor before the door it was come in by
        foreach (var (x, y, toX, toY) in new[] { (6, 85, 1, 106), (2, 98, 7, 106), (7, 46, 1, 80), (1, 59, 7, 80), (6, 59, 1, 93), (1, 72, 7, 93), (1, 20, 7, 41), (7, 20, 1, 54), (1, 33, 7, 54), (6, 33, 1, 67), (2, 7, 7, 15), (6, 7, 1, 28) })
        {
            var warp = game.Map.GetWarpAt(x, y);
            Assert.NotNull(warp);
            Assert.Equal(("PetalburgCity_Gym", toX, toY), (warp!.TargetMap, warp.TargetX, warp.TargetY));
        }
    }

    [Fact]
    public void ASaveFromBeforeTheSixthStretchHasItsPeopleHidden()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 28, Array.Empty<Pokemon>(), Scripts);
        foreach (string flag in new[] { "FLAG_HIDE_MT_CHIMNEY_TRAINERS", "FLAG_HIDE_MT_CHIMNEY_LAVA_COOKIE_LADY", "FLAG_HIDE_LAVARIDGE_TOWN_RIVAL", "FLAG_HIDE_PETALBURG_GYM_WALLYS_DAD" })
            Assert.True(story.Has(flag), flag);
        Assert.False(story.Has("FLAG_HIDE_MIRAGE_TOWER_ROOT_FOSSIL"));
        Assert.False(story.Has("FLAG_HIDE_MIRAGE_TOWER_CLAW_FOSSIL"));
        Assert.True(StoryState.CurrentVersion >= 29);
    }

    [Fact]
    public void TheSongsOfMtChimneyAndTheCableCarAreThere()
    {
        foreach (string song in new[] { "hoenn/mt_chimney", "hoenn/cable_car", "hoenn/eye_magma", "hoenn/battle_galactic_boss", "hoenn/victory_galactic_boss" })
            Assert.True(MusicLibrary.Exists(song), song);
        Assert.Contains("Revival Herb", MartDatabase.Stock("lavaridge_herb_shop", 0).Select(i => i.Name));
    }

    // ------------------------------------------------------------------ plan 21 · H6, part a: Route 118 to Fortree City

    /// <summary>Past the sixth stretch: Flannery's and Norman's Badges won and Surf had (plan 21 · H4, part b).</summary>
    private static Game PastNorman(int starter, PlayerLook look)
    {
        var game = PastMeteorFalls(starter, look);
        game.Story.GiveBadge(RegionDatabase.Hoenn, "Heat");
        game.Story.GiveBadge(RegionDatabase.Hoenn, "Balance");
        game.Story.Set("FLAG_RECEIVED_HM_SURF");
        game.Story.SetVar("VAR_PETALBURG_GYM_STATE", 7);
        game.Bag.AddItem(ItemDatabase.Get("HM03")!);
        return game;
    }

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheSeventhStretchPlaysFromRouteOneEighteenToWinona(int starter, PlayerLook look)
    {
        var game = PastNorman(starter, look);
        string mine = RegionDatabase.Get(RegionDatabase.Hoenn)!.Starters[starter];
        Assert.True(game.Story.Has("FLAG_HOENN_CHAPTER_SEVEN"));

        // Route 118: Steven hops down the ledge, has his say and walks off east; the fisherman's Good Rod
        game.Arrive("Hoenn", 284, 152);
        Assert.NotNull(game.Present("route118_steven"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_118_GABBY_AND_TY_1"));
        var steven = game.Step("StevenTrigger1");
        Assert.Contains(steven.Transcript, l => l.Text.Contains("Route 120"));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE118_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_118_STEVEN"));
        Assert.Null(game.Present("route118_steven"));
        Assert.False(game.Fires("StevenTrigger0"));
        game.Talk("fisherman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Good Rod")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_GOOD_ROD"));

        // The Weather Institute: Shelly beaten upstairs, Team Aqua gone, Castform holding a Mystic Water
        game.Arrive("Route119_WeatherInstitute_2F", 17, 2);
        Assert.Equal((1, 6), (game.Present("weather_institute_2f_scientist")!.GridX, game.Present("weather_institute_2f_scientist")!.GridY));
        var shelly = game.Talk("weather_institute_2f_shelly");
        Assert.Contains(shelly.Log, l => l.StartsWith("battle hoenn_shelly_weather_institute"));
        Assert.Equal(1, game.Story.Var("VAR_WEATHER_INSTITUTE_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_119_TEAM_AQUA"));
        Assert.True(game.Story.Has("FLAG_RECEIVED_CASTFORM"));
        Assert.Null(game.Present("weather_institute_2f_shelly"));
        Assert.NotNull(game.Present("man_4"));
        var castform = Assert.Single(game.Party.Members, p => p.Species.Name == "Castform");
        Assert.Equal(25, castform.Level);
        Assert.Equal("Mystic Water", castform.HeldItem?.Name);

        // Back out on Route 119: the workers go downstairs and the bridge before the door is free
        game.Arrive("Hoenn", 286, 33);
        Assert.Equal(2, game.Story.Var("VAR_WEATHER_INSTITUTE_STATE"));
        Assert.False(game.Story.Has("FLAG_HIDE_WEATHER_INSTITUTE_1F_WORKERS"));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "aqua_member_m" && n.GridX == 293 && n.GridY == 33);

        // The rival on the bridge north of the institute: the battle by the starter, HM02, and Scott afterwards
        game.Arrive("Hoenn", 305, 33);
        int scott = game.Story.Var("VAR_SCOTT_STATE");
        var rival = game.Step("RivalTrigger1");
        Assert.Contains(rival.Log, l => l.StartsWith($"battle hoenn_{game.Rival.ToLowerInvariant()}_route_119_{mine.ToLowerInvariant()}"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM02")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_HM_FLY"));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE119_STATE"));
        Assert.Equal(scott + 1, game.Story.Var("VAR_SCOTT_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_119_RIVAL", "FLAG_HIDE_ROUTE_119_SCOTT" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.Fires("RivalTrigger2"));

        // Fortree City: the Kecleon on the bridge to the Gym is nothing anyone can see, and can't be got past yet
        game.Arrive("Hoenn", 345, 10);
        Assert.True(game.Story.Has("FLAG_VISITED_FORTREE_CITY"));
        var unseen = game.Talk("kecleon");
        Assert.Contains(unseen.Transcript, l => l.Text.Contains("unseeable"));
        Assert.False(game.Story.Has("FLAG_HIDE_FORTREE_CITY_KECLEON"));

        // Route 120: Steven, the Kecleon on the bridge shown by the Devon Scope, and the Scope handed over
        game.Arrive("Hoenn", 373, 17);
        var bridge = game.Talk("route120_steven");
        Assert.Contains(bridge.Log, l => l.StartsWith("wildbattle Kecleon 30"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Devon Scope")!));
        foreach (string flag in new[] { "FLAG_RECEIVED_DEVON_SCOPE", "FLAG_HIDE_ROUTE_120_STEVEN", "FLAG_HIDE_ROUTE_120_KECLEON_BRIDGE" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Null(game.Present("route120_steven"));
        Assert.Null(game.Present("bridge_kecleon"));
        // One of the five more along the route, seen through the Scope now
        game.Arrive("Hoenn", 380, 12);
        var another = game.Talk("kecleon");
        Assert.Contains(another.Log, l => l.StartsWith("wildbattle Kecleon 30"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_120_KECLEON_1"));

        // Back in Fortree City, the Kecleon is seen and flees, and the woman can go to the Gym
        game.Arrive("Hoenn", 345, 10);
        var fled = game.Talk("kecleon");
        Assert.DoesNotContain(fled.Log, l => l.StartsWith("wildbattle"));
        Assert.True(game.Story.Has("FLAG_KECLEON_FLED_FORTREE"));
        Assert.True(game.Story.Has("FLAG_HIDE_FORTREE_CITY_KECLEON"));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "kecleon" && n.GridX == 345 && n.GridY == 8);
        var woman = game.Talk("woman_5");
        Assert.Contains(woman.Transcript, l => l.Text.Contains("gone"));

        // Winona: the Feather Badge, TM40, the Gym's trainers counted beaten and her number in the PokéNav
        game.Arrive("FortreeCity_Gym", 15, 22);
        var winona = game.Talk("winona");
        Assert.Contains(winona.Log, l => l.StartsWith("battle hoenn_winona_1"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Hoenn, "Feather"));
        Assert.Equal(6, game.Story.BadgeCount(RegionDatabase.Hoenn));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM40")!));
        foreach (string flag in new[] { "FLAG_DEFEATED_FORTREE_GYM", "FLAG_RECEIVED_TM_AERIAL_ACE", "FLAG_ENABLE_WINONA_MATCH_CALL", "FLAG_PHONE_CONTACT_WINONA", "FLAG_SCOTT_CALL_FORTREE_GYM" })
            Assert.True(game.Story.Has(flag), flag);
        foreach (string id in new[] { "hoenn_jared", "hoenn_flint", "hoenn_ashley", "hoenn_edwardo", "hoenn_humberto", "hoenn_darius" })
            Assert.True(game.Story.HasDefeated(id), id);
        var again = game.Talk("winona");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
    }

    [Fact]
    public void StevenOnRouteOneTwentyWaitsWhenThePlayerIsNotReady()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("Hoenn", 373, 17);
        game.Decide = (_, _) => 1;
        var refused = game.Talk("route120_steven");
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("wildbattle"));
        Assert.True(game.Story.Has("FLAG_NOT_READY_FOR_BATTLE_ROUTE_120"));
        Assert.NotNull(game.Present("route120_steven"));
        game.Decide = null;
        game.Talk("route120_steven");
        Assert.True(game.Story.Has("FLAG_RECEIVED_DEVON_SCOPE"));
    }

    [Fact]
    public void ANewGameOfHoennHidesTheSeventhStretchsPeopleAndAnOlderSaveHasThemHiddenOnTheWay()
    {
        var fresh = new Game();
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN", "FLAG_HIDE_ROUTE_119_RIVAL", "FLAG_HIDE_ROUTE_119_SCOTT", "FLAG_HIDE_WEATHER_INSTITUTE_1F_WORKERS", "FLAG_HIDE_ROUTE_120_GABBY_AND_TY_1" })
            Assert.True(fresh.Story.Has(flag), flag);
        Assert.False(fresh.Story.Has("FLAG_HIDE_ROUTE_118_STEVEN"));

        // A save from before plan 21 · H6 never ran the chapter's script: Route 118's arrival runs it, and no story version is raised
        var old = PastNorman(0, PlayerLook.Boy);
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN", "FLAG_HIDE_ROUTE_119_RIVAL", "FLAG_HIDE_ROUTE_119_SCOTT" })
            old.Story.Unset(flag);
        old.Arrive("Hoenn", 270, 148);
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN", "FLAG_HIDE_ROUTE_119_RIVAL", "FLAG_HIDE_ROUTE_119_SCOTT" })
            Assert.True(old.Story.Has(flag), flag);
    }

    [Fact]
    public void TheSongsOfRouteOneEighteenToFortreeAreThere()
    {
        foreach (string song in new[] { "hoenn/route118", "hoenn/route119", "hoenn/fortree", "hoenn/route120" })
            Assert.True(MusicLibrary.Exists(song), song);
        Assert.Equal(9111, ItemDatabase.Get("Devon Scope")!.Id);
    }

    // ------------------------------------------------------------------ plan 21 · H6, part b: Route 121, the Safari Zone and Fortree's houses

    [Fact]
    public void TeamAquasGruntsMoveOutOfRouteOneTwentyOneOnce()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("Hoenn", 428, 90);
        Assert.True(game.Story.Has("FLAG_HOENN_CHAPTER_SEVEN_B"));
        Assert.NotNull(game.Present("route121_grunt_1"));
        var scene = game.Step("GruntsMoveOut");
        Assert.Contains(scene.Transcript, l => l.Text.Contains("Mt. Pyre"));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE121_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_121_TEAM_AQUA_GRUNTS"));
        foreach (string grunt in new[] { "route121_grunt_1", "route121_grunt_2", "route121_grunt_3" })
            Assert.Null(game.Present(grunt));
        Assert.False(game.Fires("GruntsMoveOut"));
    }

    [Fact]
    public void TheSafariZoneIsPaidForAtItsCounterAndLeftPastItsAttendant()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("Route121_SafariZoneEntrance", 8, 5);
        var counter = game.Step("Counter");
        // $500 for thirty Safari Balls and Emerald's five hundred steps, then through the zone's door
        Assert.True(counter.Safari.Active);
        Assert.Equal(SafariGame.StartBalls, counter.Safari.Balls);
        Assert.Equal(500, counter.Safari.Steps);
        Assert.Equal(3000 - SafariGame.Fee, counter.Money);
        Assert.Equal("HoennSafariZone", game.Map.Name);
        Assert.Equal((72, 75), game.Tile);
        Assert.Equal(0, game.Story.Var("VAR_SAFARI_ZONE_STATE"));
        Assert.False(game.Story.Has("FLAG_GOOD_LUCK_SAFARI_ZONE"));

        // A game over in Hoenn (the last step, the last ball) brings the player back to the gate, past the counter
        game.Play(Scripts.Find("SafariTimeUp", "common")!);
        Assert.Equal("Route121_SafariZoneEntrance", game.Map.Name);
        Assert.Equal(0, game.Story.Var("VAR_SAFARI_ZONE_STATE"));
        Assert.Equal((9, 4), game.Tile);

        // With no game on, the attendant inside the door shows the player out the same way
        game.Arrive("HoennSafariZone", 72, 75);
        game.Talk("safari_exit_attendant");
        Assert.Equal("Route121_SafariZoneEntrance", game.Map.Name);
        Assert.Equal((9, 4), game.Tile);
        Assert.Equal(0, game.Story.Var("VAR_SAFARI_ZONE_STATE"));

        // Turned down, the counter sends the player a step back and takes nothing
        game.Arrive("Route121_SafariZoneEntrance", 8, 5);
        game.Decide = (_, _) => 1;
        var refused = game.Step("Counter");
        Assert.False(refused.Safari.Active);
        Assert.Equal(3000, refused.Money);
        Assert.Equal("Route121_SafariZoneEntrance", game.Map.Name);
    }

    [Fact]
    public void ANewGameOfHoennKeepsTheSafariZonesExpansionShutAndAnOlderSaveIsToldOnRouteOneTwentyOne()
    {
        var fresh = new Game();
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN_B", "FLAG_HIDE_SAFARI_ZONE_SOUTH_EAST_EXPANSION" })
            Assert.True(fresh.Story.Has(flag), flag);
        Assert.False(fresh.Story.Has("FLAG_HIDE_ROUTE_121_TEAM_AQUA_GRUNTS"));

        // A save from before part b never ran its script: Route 121's arrival runs it, and no story version is raised
        var old = PastNorman(0, PlayerLook.Boy);
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN_B", "FLAG_HIDE_SAFARI_ZONE_SOUTH_EAST_EXPANSION" })
            old.Story.Unset(flag);
        old.Arrive("Hoenn", 428, 90);
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN_B", "FLAG_HIDE_SAFARI_ZONE_SOUTH_EAST_EXPANSION" })
            Assert.True(old.Story.Has(flag), flag);
    }

    [Fact]
    public void FortreesBoyTradesHisPlusleForAVolbeatOnce()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Volbeat")!, 20));
        game.Arrive("FortreeCity_House1", 3, 4);
        game.Talk("ninja_boy");
        Assert.True(game.Story.Has("FLAG_FORTREE_NPC_TRADE_COMPLETED"));
        var pluses = game.Party.Members[0];
        Assert.Equal("Plusle", pluses.Species.Name);
        Assert.Equal("Pluses", pluses.Nickname);
        Assert.Equal("Grass Mail", pluses.HeldItem?.Name);
        // Emerald's trade gives each condition its own number, and a sheen
        Assert.Equal((5, 5, 30, 5, 5, 10), (pluses.Cool, pluses.Beauty, pluses.Cute, pluses.Smart, pluses.Tough, pluses.Sheen));
        var again = game.Talk("ninja_boy");
        Assert.Empty(again.Asked);

        // One who brings no Volbeat keeps their own
        var other = PastNorman(0, PlayerLook.Boy);
        string mine = other.Party.Members[0].Species.Name;
        other.Arrive("FortreeCity_House1", 3, 4);
        other.Talk("ninja_boy");
        Assert.False(other.Story.Has("FLAG_FORTREE_NPC_TRADE_COMPLETED"));
        Assert.Equal(mine, other.Party.Members[0].Species.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FortreesOldWomanGivesHiddenPowerForThreeRightGuesses(bool right)
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("FortreeCity_House2", 3, 4);
        // The coin is in the right hand, the right, then the left (Emerald's answers)
        game.Decide = (question, _) => question.Contains("One more") ? (right ? 1 : 0) : 0;
        game.Talk("expert_f");
        Assert.True(game.Story.Has("FLAG_MET_HIDDEN_POWER_GIVER"));
        Assert.Equal(right, game.Story.Has("FLAG_RECEIVED_TM_HIDDEN_POWER"));
        Assert.Equal(right ? 1 : 0, game.Bag.GetQuantity(ItemDatabase.Get("TM10")!));
    }

    [Fact]
    public void FortreesOldManTeachesSleepTalkOnce()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Slakoth")!, 5));
        game.Arrive("FortreeCity_House2", 6, 4);
        game.Talk("expert_m");
        Assert.True(game.Party.Members[0].Knows("Sleep Talk"));
        Assert.True(game.Story.Has("FLAG_MOVE_TUTOR_TAUGHT_SLEEP_TALK"));
        var again = game.Talk("expert_m");
        Assert.Empty(again.Asked);
        Assert.Equal(TutorAnswer.Able, MoveTutors.ForKanto(new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 10), "Sleep Talk"));
    }

    // ------------------------------------------------------------------ plan 21 · H6, part c: Gabby and Ty, and Fortree's Wingull

    /// <summary>Where the pair stands for each count of their battles, 0 to 8: the route's tile to arrive at, and the person (the sixth places go round again).</summary>
    private static readonly (int X, int Y, string Gabby)[] GabbyPlaces =
    {
        (213, 88, "route111_gabby_1"), (270, 148, "route118_gabby_1"), (373, 17, "route120_gabby_1"),
        (213, 88, "route111_gabby_2"), (270, 148, "route118_gabby_2"), (373, 17, "route120_gabby_2"),
        (213, 88, "route111_gabby_3"), (270, 148, "route118_gabby_3"), (373, 17, "route120_gabby_2")
    };

    private static readonly string[] EveryGabby =
    {
        "route111_gabby_1", "route111_gabby_2", "route111_gabby_3", "route118_gabby_1", "route118_gabby_2", "route118_gabby_3",
        "route120_gabby_1", "route120_gabby_2"
    };

    /// <summary>A game past Norman with two Pokémon on the team, for a double battle.</summary>
    private static Game WithTwo()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        if (game.Party.Count < 2) game.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 6));
        return game;
    }

    [Fact]
    public void GabbyAndTyStandWhereTheCountOfTheirBattlesPutsThemAndFightWithTheNthParty()
    {
        var game = WithTwo();
        for (int count = 0; count < GabbyPlaces.Length; count++)
        {
            var (x, y, gabby) = GabbyPlaces[count];
            game.Story.ClearLocal();
            game.Story.SetVar("VAR_GABBY_AND_TY_BATTLE_NUM", count);
            game.Arrive("Hoenn", x, y);
            foreach (string key in EveryGabby)
                Assert.Equal(key == gabby, game.Present(key) != null);

            var host = game.Talk(gabby);
            // The sixth party from then on, and the count goes round from nine to six
            Assert.Contains(host.Log, l => l.StartsWith($"battle hoenn_gabby_and_ty_{Math.Min(count, 5) + 1} "));
            Assert.Equal(count + 1 >= 9 ? 6 : count + 1, game.Story.Var("VAR_GABBY_AND_TY_BATTLE_NUM"));
            Assert.True(game.Story.Has("FLAG_MAP_LOCAL_GABBY_AND_TY_BEATEN"));
        }
    }

    [Fact]
    public void GabbyAndTyOnlySayTheyAreWatchingOnceFoughtAndWantTwoPokemonToBegin()
    {
        var game = WithTwo();
        game.Arrive("Hoenn", 213, 88);
        game.Talk("route111_gabby_1");
        // The same visit: no second battle
        var again = game.Talk("route111_gabby_1");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle "));
        Assert.Equal(1, game.Story.Var("VAR_GABBY_AND_TY_BATTLE_NUM"));

        // One Pokémon makes half of a double battle
        var alone = PastNorman(0, PlayerLook.Boy);
        while (alone.Party.Count > 1) alone.Party.Members.RemoveAt(alone.Party.Count - 1);
        alone.Arrive("Hoenn", 213, 88);
        var turned = alone.Talk("route111_ty_1");
        Assert.DoesNotContain(turned.Log, l => l.StartsWith("battle "));
        Assert.Equal(0, alone.Story.Var("VAR_GABBY_AND_TY_BATTLE_NUM"));
        Assert.False(alone.Story.Has("FLAG_MAP_LOCAL_GABBY_AND_TY_BEATEN"));
    }

    [Fact]
    public void AnInterviewIsKeptQuotedBackAndPutOnTheAirAndTheBattleIsRememberedFor()
    {
        var game = WithTwo();
        string lead = game.Party.Members[0].Species.DisplayName;
        string second = game.Party.Members[1].Species.DisplayName;

        // The first battle, a normal length: the interview is given and kept, and the segment is ready for the television
        game.Arrive("Hoenn", 213, 88);
        var first = game.Talk("route111_gabby_1");
        Assert.Contains(first.Log, l => l.Contains("open Phrase GABBY_AND_TY"));
        Assert.Equal(new[] { "feelings:happy", "small:together" }, game.Story.PhraseOf("GABBY_AND_TY"));
        foreach (string flag in new[] { "FLAG_GABBY_AND_TY_ON_AIR", "FLAG_GABBY_AND_TY_SHOW_NEW", "FLAG_GABBY_AND_TY_QUOTE_PENDING" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Equal(0, game.Story.Var("VAR_GABBY_AND_TY_TRIVIA"));
        Assert.Equal(111, game.Story.Var("VAR_GABBY_AND_TY_ROUTE"));

        // The television shows it first, once
        var tv = game.Play(Scripts.Find("Television", "common")!);
        Assert.False(game.Story.Has("FLAG_GABBY_AND_TY_SHOW_NEW"));
        Assert.Contains(tv.Transcript, l => l.Text.Contains(lead) && l.Text.Contains(second));
        Assert.Contains(tv.Transcript, l => l.Text.Contains("together"));
        var facts = tv.Broadcast();
        Assert.True(facts.GabbyOnAir);
        Assert.Contains(Programme.GabbyAndTy, Broadcasts.On(facts));

        // The second battle is over at once: the pair quote the answer back, and ask the question the first left
        game.Story.ClearLocal();
        game.Turns = 0;
        game.Arrive("Hoenn", 270, 148);
        var second2 = game.Talk("route118_gabby_1");
        Assert.Contains(second2.Transcript, l => l.Text.Contains("Last time you told us") && l.Text.Contains("together"));
        Assert.Equal(1, game.Story.Var("VAR_GABBY_AND_TY_TRIVIA"));
        Assert.Equal(118, game.Story.Var("VAR_GABBY_AND_TY_ROUTE"));

        // The third remembers that: the viewers were told it was over at once
        game.Story.ClearLocal();
        game.Turns = 3;
        game.Arrive("Hoenn", 373, 17);
        var third = game.Talk("route120_gabby_1");
        Assert.Contains(third.Asked, a => a.Item1.Contains("flattened"));
        Assert.Equal(120, game.Story.Var("VAR_GABBY_AND_TY_ROUTE"));
        Assert.Equal(3, game.Story.Var("VAR_GABBY_AND_TY_BATTLE_NUM"));
    }

    [Fact]
    public void ALostPokemonIsWhatTheNextBattleIsAskedAbout()
    {
        var game = WithTwo();
        game.Arrive("Hoenn", 213, 88);
        game.DuringFight = () => game.Party.Members[1].CurrentHP = 0;
        game.Talk("route111_gabby_1");
        Assert.Equal(4, game.Story.Var("VAR_GABBY_AND_TY_TRIVIA"));

        game.DuringFight = null;
        game.Party.Members[1].CurrentHP = game.Party.Members[1].MaxHP;
        game.Story.ClearLocal();
        game.Arrive("Hoenn", 270, 148);
        var next = game.Talk("route118_gabby_1");
        Assert.Contains(next.Asked, a => a.Item1.Contains("lost a Pokémon"));
    }

    [Fact]
    public void AnInterviewRefusedLeavesNothingOnTheAirButTheBattleIsCounted()
    {
        var game = WithTwo();
        game.Decide = (_, _) => 1;
        game.Arrive("Hoenn", 213, 88);
        var host = game.Talk("route111_ty_1");
        Assert.Contains(host.Transcript, l => l.Text.Contains("no interview"));
        Assert.Equal(1, game.Story.Var("VAR_GABBY_AND_TY_BATTLE_NUM"));
        Assert.False(game.Story.Has("FLAG_GABBY_AND_TY_ON_AIR"));
        Assert.False(game.Story.Has("FLAG_GABBY_AND_TY_QUOTE_PENDING"));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("open Phrase"));
    }

    [Fact]
    public void GabbyAndTysSegmentIsHoennsAloneAndNeedsAnInterview()
    {
        var on = new BroadcastFacts { Region = RegionDatabase.Hoenn, GabbyOnAir = true, GabbyMon1 = "Treecko", GabbyMon2 = "Zigzagoon", GabbyRoute = 120, GabbyTrivia = 4, GabbyQuote = new[] { "feelings:happy", "small:together" }, PlayerName = "Brendan" };
        Assert.Contains(Programme.GabbyAndTy, Broadcasts.On(on));
        Assert.DoesNotContain(Programme.GabbyAndTy, Broadcasts.On(on with { Region = RegionDatabase.Sinnoh }));
        Assert.DoesNotContain(Programme.GabbyAndTy, Broadcasts.On(on with { GabbyOnAir = false }));
        Assert.DoesNotContain(Programme.GabbyAndTy, Broadcasts.On(on with { GabbyMon1 = null }));

        string said = string.Join(" ", Broadcasts.Lines(Programme.GabbyAndTy, on));
        Assert.Contains("Treecko", said);
        Assert.Contains("Zigzagoon", said);
        Assert.Contains("together", said);
        Assert.Contains("Brendan", said);
        // One Pokémon fought alone has a line of its own
        string solo = string.Join(" ", Broadcasts.Lines(Programme.GabbyAndTy, on with { GabbyMon2 = null }));
        Assert.DoesNotContain("Zigzagoon", solo);
        Assert.Contains("gabbyandty", Broadcasts.Names);
    }

    [Fact]
    public void FortreesBoySendsHisWingullOffOnItsErrandOnce()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("FortreeCity_House4", 3, 4);
        Assert.NotNull(game.Present("fortree_house_wingull"));
        Assert.True(game.Story.Has("FLAG_HIDE_MOSSDEEP_CITY_HOUSE_2_WINGULL"));

        game.Talk("boy_3");
        Assert.True(game.Story.Has("FLAG_WINGULL_SENT_ON_ERRAND"));
        Assert.True(game.Story.Has("FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL"));
        Assert.False(game.Story.Has("FLAG_HIDE_MOSSDEEP_CITY_HOUSE_2_WINGULL"));
        Assert.Null(game.Present("fortree_house_wingull"));

        // Spoken to again, he only says the bird is away; coming back to the house, it is still out
        var again = game.Talk("boy_3");
        Assert.Contains(again.Transcript, l => l.Text.Contains("errand"));
        game.Arrive("FortreeCity_House4", 3, 4);
        Assert.Null(game.Present("fortree_house_wingull"));
    }

    [Fact]
    public void ANewGameOfHoennHidesGabbyAndTyAndAnOlderSaveIsGivenTheThirdPartOnTheWay()
    {
        var fresh = new Game();
        foreach (string flag in new[] { "FLAG_HOENN_CHAPTER_SEVEN_C", "FLAG_HIDE_ROUTE_111_GABBY_AND_TY_1", "FLAG_HIDE_ROUTE_118_GABBY_AND_TY_3", "FLAG_HIDE_ROUTE_120_GABBY_AND_TY_2", "FLAG_HIDE_MOSSDEEP_CITY_HOUSE_2_WINGULL" })
            Assert.True(fresh.Story.Has(flag), flag);
        Assert.False(fresh.Story.Has("FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL"));

        // A save from before part c never ran its script (part b hid the Wingull): Route 120's arrival runs it, and no story version is raised
        var old = PastNorman(0, PlayerLook.Boy);
        old.Story.Unset("FLAG_HOENN_CHAPTER_SEVEN_C");
        old.Story.Unset("FLAG_HIDE_ROUTE_120_GABBY_AND_TY_1");
        old.Story.Set("FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL");
        old.Arrive("Hoenn", 373, 17);
        Assert.True(old.Story.Has("FLAG_HOENN_CHAPTER_SEVEN_C"));
        Assert.True(old.Story.Has("FLAG_HIDE_ROUTE_120_GABBY_AND_TY_1"));
        Assert.False(old.Story.Has("FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL"));
    }

    // ------------------------------------------------------------------ plan 21 · H7, part a: Lilycove City and Mt. Pyre

    [Theory]
    [InlineData(0, PlayerLook.Boy)]
    [InlineData(1, PlayerLook.Girl)]
    [InlineData(2, PlayerLook.Boy)]
    public void TheRivalBattlesOnceBeforeLilycovesDepartmentStore(int starter, PlayerLook look)
    {
        var game = PastNorman(starter, look);
        string mine = RegionDatabase.Get(RegionDatabase.Hoenn)!.Starters[starter];
        game.Arrive("Hoenn", 507, 80);
        Assert.True(game.Story.Has("FLAG_VISITED_LILYCOVE_CITY"));
        Assert.NotNull(game.Present("var_0"));

        // Said no, the rival stays and asks again
        game.Decide = (_, _) => 1;
        var no = game.Talk("var_0");
        Assert.True(game.Story.Has("FLAG_DECLINED_RIVAL_BATTLE_LILYCOVE"));
        Assert.DoesNotContain(no.Log, l => l.StartsWith("battle "));
        Assert.NotNull(game.Present("var_0"));

        // Said yes, the battle with the team of the starter taken, and the rival goes home for good
        game.Decide = null;
        var yes = game.Talk("var_0");
        Assert.Contains(yes.Log, l => l.StartsWith($"battle hoenn_{game.Rival.ToLowerInvariant()}_lilycove_{mine.ToLowerInvariant()}"));
        Assert.True(game.Story.Has("FLAG_MET_RIVAL_LILYCOVE"));
        Assert.True(game.Story.Has("FLAG_HIDE_LILYCOVE_CITY_RIVAL"));
        game.Arrive("Hoenn", 507, 80);
        Assert.Null(game.Present("var_0"));
    }

    [Theory]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    public void TeamAquaLeavesMtPyresSummitAndTheOldWomanGivesTheMagmaEmblem(int column)
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("MtPyre_Summit", 23, 30);
        Assert.NotNull(game.Present("mt_pyre_summit_archie"));
        Assert.NotNull(game.Present("mt_pyre_summit_grunt_1"));
        Assert.Null(game.Present("mt_pyre_summit_maxie"));

        var scene = game.StepAt(column, 7);
        Assert.NotNull(scene);
        Assert.Contains(scene!.Transcript, l => l.Text.Contains("Red Orb"));
        Assert.Equal(1, game.Story.Var("VAR_MT_PYRE_STATE"));
        foreach (string flag in new[] { "FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE", "FLAG_HIDE_MT_PYRE_SUMMIT_TEAM_AQUA",
                     "FLAG_RECEIVED_RED_OR_BLUE_ORB", "FLAG_HIDE_JAGGED_PASS_MAGMA_GUARD" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Magma Emblem")!));
        foreach (string who in new[] { "mt_pyre_summit_archie", "mt_pyre_summit_grunt_1", "mt_pyre_summit_grunt_2",
                     "mt_pyre_summit_grunt_3", "mt_pyre_summit_grunt_4" })
            Assert.Null(game.Present(who));

        // Once is all: the row below the shrine starts nothing again
        Assert.Null(game.StepAt(column, 7));
        game.Arrive("MtPyre_Summit", 23, 30);
        Assert.Null(game.Present("mt_pyre_summit_archie"));
    }

    [Fact]
    public void MtPyreIsClimbedFromRouteOneTwentyTwoToItsSummit()
    {
        // Every floor of the tower and the mountainside are rooms of Hoenn, joined by their stairs, with the old woman's
        // Cleanse Tag on the ground floor once
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("MtPyre_1F", 17, 17);
        game.Talk("old_woman");
        Assert.True(game.Story.Has("FLAG_RECEIVED_CLEANSE_TAG"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Cleanse Tag")!));
        Assert.DoesNotContain(game.Talk("old_woman").Log, l => l.StartsWith("give"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Cleanse Tag")!));

        game.Through("MtPyre_2F");
        game.Through("MtPyre_3F");
        game.Through("MtPyre_4F");
        game.Through("MtPyre_5F");
        game.Through("MtPyre_6F");
        Assert.Equal("MtPyre_6F", game.Map.Name);
        game.Arrive("MtPyre_1F", 3, 7);
        game.Through("MtPyre_Exterior");
        game.Through("MtPyre_Summit");
        Assert.Equal("MtPyre_Summit", game.Map.Name);
    }

    [Fact]
    public void LilycovesStoreSellsEmeraldsStockAndItsLotteryDrawsOnceADay()
    {
        foreach (var (key, item) in new[] { ("lilycove_2f_balls", "Fluffy Tail"), ("lilycove_2f_medicine", "Max Repel"),
                     ("lilycove_3f_vitamins", "HP Up"), ("lilycove_3f_battle", "X Accuracy"), ("lilycove_4f_attack", "TM15"),
                     ("lilycove_4f_defense", "TM17") })
            Assert.Contains(item, MartDatabase.Specialties[key]);

        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("LilycoveCity_DepartmentStore_1F", 10, 3);
        var draw = game.Talk("lottery_clerk");
        Assert.True(game.Story.Has("FLAG_DAILY_PICKED_LOTO_TICKET"));
        Assert.Contains(draw.Transcript, l => l.Text.Contains("ticket"));
        var again = game.Talk("lottery_clerk");
        Assert.Empty(again.Asked);
    }

    [Fact]
    public void LilycovesRoofTeachesSubstituteOnceAndRestIsGivenOnce()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 20));
        game.Arrive("LilycoveCity_DepartmentStoreRooftop", 15, 6);
        game.Talk("woman_3");
        Assert.True(game.Party.Members[0].Knows("Substitute"));
        Assert.True(game.Story.Has("FLAG_MOVE_TUTOR_TAUGHT_SUBSTITUTE"));
        Assert.Empty(game.Talk("woman_3").Asked);

        game.Arrive("LilycoveCity_House2", 2, 5);
        game.Talk("fat_man");
        game.Talk("fat_man");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM44")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_TM_REST"));
    }

    [Fact]
    public void TheBerryMasterGivesTwoBerriesADayAndTheGirlOfRouteOneTwentyThreeWantsAGrassPokemon()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("Route123_BerryMastersHouse", 4, 5);
        int before = game.Bag.AllItems.Sum(s => s.Quantity);
        game.Talk("expert_m");
        Assert.True(game.Story.Has("FLAG_DAILY_BERRY_MASTER_RECEIVED_BERRY"));
        // (the headless die rolls 0: two Pomeg Berries)
        Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Pomeg Berry")!));
        game.Talk("expert_m");
        Assert.Equal(before + 2, game.Bag.AllItems.Sum(s => s.Quantity));

        // The girl by the berry trees: nothing without a Grass Pokémon, TM19 with one, and once
        var girl = PastNorman(1, PlayerLook.Boy);
        girl.Party.Members.Clear();
        girl.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 30));
        girl.Arrive("Hoenn", 427, 153);
        girl.Talk("girl_3");
        Assert.False(girl.Story.Has("FLAG_RECEIVED_TM_GIGA_DRAIN"));
        girl.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Shroomish")!, 30));
        girl.Talk("girl_3");
        Assert.True(girl.Story.Has("FLAG_RECEIVED_TM_GIGA_DRAIN"));
        Assert.Equal(1, girl.Bag.GetQuantity(ItemDatabase.Get("TM19")!));
    }

    [Fact]
    public void ANewGameOfHoennHidesTheEighthStretchsPeopleAndAnOlderSaveIsGivenThemOnTheWay()
    {
        string[] hidden =
        {
            "FLAG_HOENN_CHAPTER_EIGHT", "FLAG_HIDE_MT_PYRE_SUMMIT_MAXIE", "FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_1",
            "FLAG_HIDE_LILYCOVE_MUSEUM_TOURISTS", "FLAG_HIDE_LILYCOVE_FAN_CLUB_INTERVIEWER", "FLAG_HIDE_LILYCOVE_MOTEL_GAME_DESIGNERS"
        };
        var fresh = new Game();
        foreach (string flag in hidden) Assert.True(fresh.Story.Has(flag), flag);
        Assert.False(fresh.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE"));

        // A save from before part a never ran its script: Lilycove's, Route 122's and Route 123's arrivals run it, and no
        // story version is raised
        foreach (var (x, y) in new[] { (507, 80), (442, 130), (333, 152) })
        {
            var old = PastNorman(0, PlayerLook.Boy);
            foreach (string flag in hidden) old.Story.Unset(flag);
            old.Arrive("Hoenn", x, y);
            foreach (string flag in hidden) Assert.True(old.Story.Has(flag), $"{flag} at {x},{y}");
        }
    }

    [Fact]
    public void TheSongsOfLilycoveAndMtPyreAreThere()
    {
        foreach (string song in new[] { "hoenn/lilycove", "hoenn/route122", "hoenn/mt_pyre", "hoenn/mt_pyre_exterior" })
            Assert.True(MusicLibrary.Exists(song), song);
        Assert.Equal(9112, ItemDatabase.Get("Magma Emblem")!.Id);
    }

    // ------------------------------------------------------------------ plan 21 · H7, part b: the two teams' hideouts

    /// <summary>Past Mt. Pyre's summit: the Magma Emblem in the bag and Jagged Pass's guard gone, as the summit leaves them.</summary>
    private static Game PastMtPyre(int starter = 0, PlayerLook look = PlayerLook.Boy)
    {
        var game = PastNorman(starter, look);
        game.Bag.AddItem(ItemDatabase.Get("Magma Emblem")!);
        game.Story.Set("FLAG_RECEIVED_RED_OR_BLUE_ORB");
        game.Story.Set("FLAG_HIDE_JAGGED_PASS_MAGMA_GUARD");
        game.Story.SetVar("VAR_MT_PYRE_STATE", 1);
        return game;
    }

    [Fact]
    public void TheGuardOnJaggedPassBattlesOnceBeforeTheEmblem()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("JaggedPass", 16, 22);
        Assert.NotNull(game.Present("magma_hideout_guard"));
        var first = game.Talk("magma_hideout_guard");
        Assert.Contains(first.Log, l => l.StartsWith("battle hoenn_grunt_jagged_pass"));
        Assert.True(game.Story.Has("FLAG_BEAT_MAGMA_GRUNT_JAGGED_PASS"));
        Assert.DoesNotContain(game.Talk("magma_hideout_guard").Log, l => l.StartsWith("battle"));

        // Without the emblem the rock stays shut and the triggers do nothing
        Assert.Equal(0, game.Story.Var("VAR_JAGGED_PASS_STATE"));
        Assert.Null(game.StepAt(21, 15));
        Assert.All(game.Map.Warps.Where(w => w.TargetMap == "MagmaHideout_1F"), w => Assert.True(w.Shut));
    }

    [Theory]
    [InlineData(21, 15)]
    [InlineData(22, 20)]
    [InlineData(13, 15)]
    public void TheMagmaEmblemShakesTheRockOpenOnJaggedPass(int x, int y)
    {
        var game = PastMtPyre();
        game.Arrive("JaggedPass", 16, 22);
        Assert.Null(game.Present("magma_hideout_guard"));
        Assert.Equal(1, game.Story.Var("VAR_JAGGED_PASS_STATE"));
        var scene = game.StepAt(x, y);
        Assert.NotNull(scene);
        Assert.Equal(2, game.Story.Var("VAR_JAGGED_PASS_STATE"));
        Assert.True(game.Story.Has("FLAG_HOENN_MAGMA_HIDEOUT_OPEN"));
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "MagmaHideout_1F" && !w.Shut);
        // Once: the triggers start nothing again
        Assert.Null(game.StepAt(x, y));
        game.Through("MagmaHideout_1F");
        Assert.Equal("MagmaHideout_1F", game.Map.Name);
    }

    [Fact]
    public void GroudonWakesAndFleesAndMaxieGoesAfterItWithTeamMagma()
    {
        var game = PastMtPyre();
        game.Story.SetVar("VAR_JAGGED_PASS_STATE", 2);
        game.Story.Set("FLAG_HOENN_MAGMA_HIDEOUT_OPEN");
        game.Arrive("MagmaHideout_4F", 20, 21);
        Assert.NotNull(game.Present("magma_hideout_4f_groudon_sleeping"));
        Assert.Null(game.Present("magma_hideout_4f_groudon"));
        Assert.NotNull(game.Present("magma_hideout_4f_tabitha"));

        var scene = game.Talk("magma_hideout_4f_maxie");
        Assert.Contains(scene.Log, l => l.StartsWith("battle hoenn_maxie_magma_hideout"));
        // No battle with Groudon: it is woken and gone
        Assert.DoesNotContain(scene.Log, l => l.StartsWith("wildbattle"));
        foreach (string flag in new[] { "FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT", "FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON",
                     "FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON_ASLEEP", "FLAG_HIDE_MAGMA_HIDEOUT_GRUNTS" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.Story.Has("FLAG_HIDE_SLATEPORT_CITY_CAPTAIN_STERN"));
        Assert.Equal(1, game.Story.Var("VAR_SLATEPORT_CITY_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_SLATEPORT_HARBOR_STATE"));

        game.Arrive("MagmaHideout_4F", 20, 21);
        foreach (string who in new[] { "magma_hideout_4f_maxie", "magma_hideout_4f_tabitha", "magma_hideout_4f_groudon",
                     "magma_hideout_4f_groudon_sleeping", "magma_hideout_4f_grunt_1" })
            Assert.Null(game.Present(who));
        game.Arrive("MagmaHideout_1F", 10, 33);
        Assert.Null(game.Present("magma_member_m"));
    }

    /// <summary>Past Team Magma's hideout: Groudon woken and Slateport's two scenes ready.</summary>
    private static Game PastTheMagmaHideout()
    {
        var game = PastMtPyre();
        foreach (string flag in new[] { "FLAG_HOENN_MAGMA_HIDEOUT_OPEN", "FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT",
                     "FLAG_HIDE_MAGMA_HIDEOUT_GRUNTS", "FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON_ASLEEP" })
            game.Story.Set(flag);
        game.Story.Unset("FLAG_HIDE_SLATEPORT_CITY_CAPTAIN_STERN");
        game.Story.Unset("FLAG_HIDE_SLATEPORT_CITY_GABBY_AND_TY");
        game.Story.SetVar("VAR_JAGGED_PASS_STATE", 2);
        game.Story.SetVar("VAR_SLATEPORT_CITY_STATE", 1);
        game.Story.SetVar("VAR_SLATEPORT_HARBOR_STATE", 1);
        return game;
    }

    [Theory]
    [InlineData(8, 11)]
    [InlineData(8, 12)]
    [InlineData(8, 13)]
    [InlineData(8, 14)]
    public void TeamAquaTakesCaptainSternsSubmarineFromSlateportsHarbour(int x, int y)
    {
        var game = PastTheMagmaHideout();
        game.Arrive("Hoenn", 227, 274);
        Assert.NotNull(game.Present("slateport_capt_stern"));
        Assert.NotNull(game.Present("slateport_gabby"));

        game.Talk("slateport_capt_stern");
        Assert.Equal("SlateportCity_Harbor", game.Map.Name);
        Assert.Equal(2, game.Story.Var("VAR_SLATEPORT_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_SLATEPORT_CITY_GABBY_AND_TY"));
        Assert.NotNull(game.Present("slateport_harbor_archie"));
        Assert.NotNull(game.Present("slateport_harbor_capt_stern"));
        Assert.Null(game.Present("beauty"));

        var scene = game.StepAt(x, y);
        Assert.NotNull(scene);
        Assert.Contains(scene!.Transcript, l => l.Text.Contains("Lilycove"));
        Assert.Equal(2, game.Story.Var("VAR_SLATEPORT_HARBOR_STATE"));
        foreach (string flag in new[] { "FLAG_MET_TEAM_AQUA_HARBOR", "FLAG_HIDE_SLATEPORT_CITY_HARBOR_ARCHIE",
                     "FLAG_HIDE_SLATEPORT_CITY_HARBOR_AQUA_GRUNT", "FLAG_HIDE_AQUA_HIDEOUT_1F_GRUNT_1_BLOCKING_ENTRANCE",
                     "FLAG_HIDE_AQUA_HIDEOUT_1F_GRUNT_2_BLOCKING_ENTRANCE", "FLAG_HIDE_LILYCOVE_MOTEL_SCOTT" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Null(game.Present("slateport_harbor_archie"));
        Assert.Null(game.StepAt(x, y));

        // The street's Stern is gone into the harbour, and the hideout's way in is clear
        game.Arrive("Hoenn", 227, 274);
        Assert.Null(game.Present("slateport_capt_stern"));
        game.Arrive("AquaHideout_1F", 13, 26);
        Assert.Null(game.Present("aqua_member_m"));
        Assert.Null(game.Present("aqua_member_m_2"));
    }

    [Fact]
    public void TheHideoutsGuardsHintWhereArchieIsByHowFarTheStoryHasGone()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("AquaHideout_1F", 13, 26);
        Assert.Contains(game.Talk("aqua_member_m_2").Transcript, l => l.Text.Contains("Mt. Pyre"));
        game.Story.Set("FLAG_RECEIVED_RED_OR_BLUE_ORB");
        Assert.Contains(game.Talk("aqua_member_m_2").Transcript, l => l.Text.Contains("Mt. Chimney"));
        game.Story.Set("FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT");
        Assert.Contains(game.Talk("aqua_member_m_2").Transcript, l => l.Text.Contains("Slateport"));
    }

    [Theory]
    [InlineData("item_ball_3", "FLAG_DEFEATED_ELECTRODE_1_AQUA_HIDEOUT")]
    [InlineData("item_ball_5", "FLAG_DEFEATED_ELECTRODE_2_AQUA_HIDEOUT")]
    public void TwoOfTheBallsInTeamAquasHideoutAreElectrode(string ball, string flag)
    {
        var game = PastTheMagmaHideout();
        game.Arrive("AquaHideout_B1F", 29, 2);
        Assert.NotNull(game.Present(ball));
        var battle = game.Talk(ball);
        Assert.Contains(battle.Log, l => l.StartsWith("wildbattle Electrode 30"));
        Assert.True(game.Story.Has(flag));
        // (it was never a ball: nothing is found)
        Assert.DoesNotContain(battle.Log, l => l.StartsWith("find") || l.StartsWith("give"));
        game.Arrive("AquaHideout_B1F", 29, 2);
        Assert.Null(game.Present(ball));
    }

    [Fact]
    public void MattKeepsThePlayerBusyWhileTheSubmarineSailsAndLilycovesChannelOpens()
    {
        var game = PastTheMagmaHideout();
        game.Story.SetVar("VAR_SLATEPORT_HARBOR_STATE", 2);
        game.Arrive("Hoenn", 507, 80);
        Assert.NotNull(game.Present("wailmer_1"));
        Assert.NotNull(game.Present("wailmer_1b"));
        Assert.NotNull(game.Present("aqua_member_m"));

        game.Arrive("AquaHideout_B2F", 32, 19);
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        var scene = game.Talk("aqua_hideout_matt");
        Assert.Contains(scene.Log, l => l.StartsWith("battle hoenn_matt"));
        Assert.True(game.Story.Has("FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE"));
        Assert.True(game.Story.Has("FLAG_HIDE_LILYCOVE_CITY_AQUA_GRUNTS"));
        // Spoken to again, he only laughs
        Assert.DoesNotContain(game.Talk("aqua_hideout_matt").Log, l => l.StartsWith("battle"));
        game.Arrive("AquaHideout_B2F", 32, 19);
        Assert.Equal(1, game.Story.Var("VAR_TEMP_1"));

        game.Arrive("Hoenn", 507, 80);
        foreach (string who in new[] { "wailmer_1", "wailmer_1b", "wailmer_2", "wailmer_3d", "aqua_member_m", "aqua_member_m_5" })
            // (Lilycove's own: other areas of the overworld have an aqua_member_m of their own)
            Assert.True(!game.Map.NPCs.Any(n => n.Key == who && game.Map.ScriptFileAt(n.GridX, n.GridY) == "lilycove_city"), $"{who} is still in Lilycove City");
        Assert.Contains(game.Talk("sailor").Transcript, l => l.Text.Contains("east"));
    }

    [Fact]
    public void LilycovesWailmerShutTheChannelOnEmeraldsTwelveTiles()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("Hoenn", 507, 80);
        foreach (var (x, y) in new[] { (556, 82), (557, 82), (556, 83), (557, 83), (556, 84), (557, 84), (556, 85), (557, 85),
                     (557, 86), (558, 86), (557, 87), (558, 87) })
            Assert.Contains(game.Map.NPCs, n => n.GridX == x && n.GridY == y && n.HiddenBy == "FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE");
        // Only three of them are drawn: the rest of each Wailmer is nobody to be seen
        Assert.Equal(3, game.Map.NPCs.Count(n => n.HiddenBy == "FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE" && n.IsPokemon));
    }

    [Fact]
    public void TheRivalIsHomeInTheirBedroomAfterLilycove()
    {
        var game = PastNorman(0, PlayerLook.Boy);
        game.Arrive("LittlerootTown_MaysHouse_2F", 4, 5);
        Assert.Null(game.Present("rivals_house_2f_rival"));
        game.Arrive("Hoenn", 507, 80);
        game.Talk("var_0");
        Assert.False(game.Story.Has("FLAG_HIDE_LITTLEROOT_TOWN_MAYS_HOUSE_RIVAL_BEDROOM"));
        game.Arrive("LittlerootTown_MaysHouse_2F", 4, 5);
        Assert.NotNull(game.Present("rivals_house_2f_rival"));
        Assert.Contains(game.Talk("rivals_house_2f_rival").Transcript, l => l.Text.Contains("cheering"));
    }

    [Fact]
    public void ANewGameOfHoennHidesTheHideoutsPeopleAndAnOlderSaveIsGivenThemOnTheWay()
    {
        string[] hidden =
        {
            "FLAG_HOENN_CHAPTER_EIGHT_B", "FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON", "FLAG_HIDE_SLATEPORT_CITY_CAPTAIN_STERN",
            "FLAG_HIDE_SLATEPORT_CITY_GABBY_AND_TY", "FLAG_HIDE_SLATEPORT_CITY_HARBOR_CAPTAIN_STERN",
            "FLAG_HIDE_SLATEPORT_CITY_HARBOR_ARCHIE", "FLAG_HIDE_SLATEPORT_CITY_HARBOR_AQUA_GRUNT"
        };
        var fresh = new Game();
        foreach (string flag in hidden) Assert.True(fresh.Story.Has(flag), flag);
        Assert.False(fresh.Story.Has("FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON_ASLEEP"));

        // A save from before part b: Jagged Pass's, Slateport City's and Lilycove City's arrivals run it, and no story
        // version is raised
        foreach (var (map, x, y) in new[] { ("JaggedPass", 16, 22), ("Hoenn", 227, 280), ("Hoenn", 507, 80) })
        {
            var old = PastNorman(0, PlayerLook.Boy);
            foreach (string flag in hidden) old.Story.Unset(flag);
            old.Arrive(map, x, y);
            foreach (string flag in hidden) Assert.True(old.Story.Has(flag), $"{flag} at {map} {x},{y}");
        }
    }

    [Fact]
    public void TheSongsOfTheHideoutsAreThere()
    {
        Assert.True(MusicLibrary.Exists("hoenn/aqua_magma_hideout"));
        Assert.True(MusicLibrary.Get("hoenn/aqua_magma_hideout")!.Loops);
        Assert.True(MusicLibrary.Exists("hoenn/awaken_legend"));
        Assert.False(MusicLibrary.Get("hoenn/awaken_legend")!.Loops);
    }
}
