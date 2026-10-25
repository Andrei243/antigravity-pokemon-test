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
/// Unova's first chapter (plan 22 · U5, part a: "Aspertia to Route 20's end") played through with no screen, as a player
/// meets it: Mom's questions at the foot of the stairs, Hugh and his sister at their door, the gate before there is a
/// Pokémon, Hugh catching up at the outlook's steps, Bianca and the choice of three, the Pokédex and Hugh's first
/// battle, the Pokémon Center's tour, Mom's Running Shoes and the two Town Maps, the gate's Potion, Bianca's lesson and
/// Alder on Route 19, Alder in Floccesy Town, the ranch's Hugh, owners, lost Herdier and Team Plasma's grunt, Alder's
/// lesson at home, Cheren's Gym and the Basic Badge, Cheren and Hugh outside the school, and the hiker and Cheren's
/// advice on Route 20. Each scene starts as the game starts it (the place's arrival, a step onto its trigger, a door,
/// someone spoken to), on maps of this class's own.
/// </summary>
public class UnovaStoryTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Unova = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Unova));

    /// <summary>A game of Unova of its own: its maps, story, team and bag, and the tile the player stands on.</summary>
    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public int Starter { get; init; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public string? BeganIn { get; init; }
        /// <summary>Whom the player plays as (plan 22 · U6: Nimbasa's young Trainer is the hero the player isn't).</summary>
        public PlayerLook Look { get; set; } = PlayerLook.Boy;
        /// <summary>The day of the week the game's scripts see (plan 22 · U6, part c: Anville Town's weekend crowd).</summary>
        public DayOfWeek Weekday { get; set; } = DayOfWeek.Monday;
        public List<(string Script, HeadlessScriptHost Host)> Played { get; } = new();
        public HeadlessScriptHost Last => Played[^1].Host;

        public Game(int starter = 0, string? beganIn = null, bool begin = true)
        {
            Starter = starter;
            BeganIn = beganIn;
            foreach (var map in Unova.Value.BuildMaps()) maps[map.Name] = map;
            // Castelia's avenue is a map of ours, Black 2's standing on rails (plan 22 · U5, part c)
            maps["CasteliaCity"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "CasteliaCity.json")).ToMap();
            // and so is Clay's Gym, Black 2's mine standing on rails (plan 22 · U6, part b)
            maps["DriftveilCity_Gym"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "DriftveilCity_Gym.json")).ToMap();
            // and so is the Gear Station, Black 2's hall standing on rails (plan 22 · U6, part c)
            maps["GearStation"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "GearStation.json")).ToMap();
            // (a game from before the story was written began with none of what a new game sets now)
            if (!begin) return;
            StoryMigration.BeginNewGame(Story, Scripts);
            var start = RegionDatabase.Get(RegionDatabase.Unova)!.Start!;
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

        public NPC? Present(string key, string area) => Map.NPCs.FirstOrDefault(n => n.Key == key && n.ScriptFile == area);

        public HeadlessScriptHost Talk(string key, string area)
        {
            var npc = Map.NPCs.First(n => n.Key == key && n.ScriptFile == area);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>Speaks to someone of a map file (their script file is the map's), by their id.</summary>
        public HeadlessScriptHost TalkTo(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                StarterChoice = Starter,
                RivalName = "Hugh",
                Fight = _ => Fight,
                NeedsPokemon = true,
                BeganInRegion = BeganIn,
                PlayerLook = Look,
                Weekday = Weekday
            };
            var runner = new ScriptRunner(Scripts, host);
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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheChapterPlaysFromThePlayersRoomToRouteTwentysEnd(int starter)
    {
        var game = new Game(starter);
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        string mine = unova.Starters[starter];

        // A new game: the player's room, nothing in hand, Mom at the table and the chapter's people where they start
        Assert.Equal("AspertiaCity_PlayersHouse", game.Map.Name);
        Assert.Empty(game.Party.Members);
        Assert.False(game.Story.Has(StoryState.PokedexFlag));
        Assert.True(game.Story.Has("FLAG_UNOVA_STORY_BEGUN"));
        Assert.NotNull(game.Present("0", "aspertia_city_2"));

        // Mom's questions at the foot of the stairs
        game.Step("MomQuestions");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_HOUSE"));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Mom" && l.Text.Contains("Bianca"));
        Assert.False(game.Fires("MomQuestions"));

        // Out of the door: Hugh and his sister, then both gone their own ways
        game.Through("Unova");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_ASPERTIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGHS_SISTER"));
        Assert.Null(game.Present("0", "aspertia_city"));

        // The gate turns back a player with no Pokémon, in town and inside the gate house
        Assert.True(game.Fires("NotYet"));
        game.Step("NotYet");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ASPERTIA"));

        // Hugh catches up at the outlook's steps
        game.Step("HughCatchesUp");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_OUTLOOK"));
        Assert.NotNull(game.Present("0", "aspertia_city"));
        Assert.True(game.Fires("HughTurnsBack"));

        // Bianca: the choice of three, the Pokédex, Hugh's first battle, then the Pokémon Center's tour
        var bianca = game.Talk("2", "aspertia_city");
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        string rival = starter switch { 0 => "unova_rival", 1 => "unova_rival_2", _ => "unova_rival_3" };
        Assert.Contains(bianca.Log, l => l.StartsWith($"battle {rival}"));
        Assert.Equal(unova.RivalStarterFor(mine), Assert.Single(TrainerDatabase.Get(rival)!.Party).Species);
        Assert.Equal("AspertiaCity_PokemonCenter", game.Map.Name);
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.Equal(10, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_CENTER"));

        // Out of the Pokémon Center: Mom's Running Shoes and the sister's two Town Maps
        game.Through("Unova");
        Assert.True(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Town Map")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HUGHS_TOWN_MAP"));
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.False(game.Fires("NotYet"));
        Assert.NotNull(game.Present("1", "aspertia_city"));

        // The gate's Potion
        game.Arrive("AspertiaGate", 5, 13);
        game.Step("GateGift");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_GATE"));

        // Route 19: Bianca's lesson, and Alder at the bottom of the hill
        game.Arrive("Unova", 53, 701);
        game.Step("BiancasLesson");
        Assert.Contains(game.Last.Log, l => l.StartsWith("catchinglesson Patrat 2"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ROUTE_19"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_MOM_HOME"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_MOM_ASPERTIA"));
        game.Step("AlderMeets");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_ROUTE_19"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ALDER_ROUTE_19"));

        // Floccesy Town: Alder's welcome, and the second Town Map sending the player on to the ranch
        game.Step("AlderWelcome");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_FLOCCESY"));
        game.Step("AlderTownMaps");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_FLOCCESY"));
        Assert.True(game.Fires("AlderWaits"));

        // The ranch: Hugh's battle and his Town Map, the owners, the search, and the grunt with the lost Herdier
        game.Arrive("FloccesyRanch", 46, 53);
        var ranch = game.Step("HughAtTheRanch");
        string rival4 = starter switch { 0 => "unova_rival_4", 1 => "unova_rival_5", _ => "unova_rival_6" };
        Assert.Contains(ranch.Log, l => l.StartsWith($"battle {rival4}"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HUGHS_TOWN_MAP"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_RANCH_OWNERS"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_RANCH"));
        Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        game.Step("HughNorth");
        game.Step("HughShares");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Paralyze Heal")!));
        game.Step("ACry");
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_RANCH"));
        game.Step("TheHerdier");
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_RANCH"));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_FLOCCESY"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM21")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_RANCH"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_RANCH_GRUNT"));

        // Back in Floccesy: Alder asks the player in, and his lesson with two of the school's pupils
        game.Arrive("Unova", 113, 668);
        Assert.True(game.Fires("AlderHome"));
        game.Step("AlderHome");
        Assert.Equal("FloccesyTown_AldersHouse", game.Map.Name);
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_FLOCCESY"));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_ROUTE_19"));
        Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!));
        string suffix = starter switch { 0 => "", 1 => "_2", _ => "_3" };
        Assert.Contains(game.Last.Log, l => l.StartsWith($"battle unova_school_kid_seymour{suffix} "));
        Assert.Contains(game.Last.Log, l => l.StartsWith($"battle unova_school_kid_cassie{suffix} "));

        // Route 20's hiker wants a Badge before he lets anyone east
        game.Arrive("Unova", 157, 645);
        Assert.True(game.Fires("HikerBlocks"));
        game.Step("HikerBlocks");
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_ROUTE_20"));

        // Cheren's Gym: his pupils first, then the Basic Badge and TM83
        game.Arrive("AspertiaCity_Gym", 15, 20);
        game.Talk("0", "aspertia_city_gym");
        Assert.False(game.Story.HasBadge(RegionDatabase.Unova, "Basic"));
        game.Talk("1", "aspertia_city_gym");
        game.Talk("2", "aspertia_city_gym");
        Assert.True(game.Story.HasDefeated("unova_youngster_pedro"));
        Assert.True(game.Story.HasDefeated("unova_lass_serena"));
        var cheren = game.Talk("0", "aspertia_city_gym");
        Assert.Contains(cheren.Log, l => l.StartsWith("battle unova_leader_cheren "));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Basic"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM83")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ROUTE_20"));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRoleOf("Leader Cheren"));

        // Out of the school: Bianca's TM27, and Cheren taking Hugh's challenge
        game.Arrive("Unova", 39, 739);
        Assert.True(game.Story.Has("FLAG_UNOVA_AFTER_THE_BASIC_BADGE"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM27")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_CHEREN_ASPERTIA"));

        // Route 20: the hiker's battle, and Cheren's advice past him
        game.Arrive("Unova", 157, 645);
        var hiker = game.Step("HikerChallenge");
        Assert.Contains(hiker.Log, l => l.StartsWith("battle unova_hiker_jerome"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_ROUTE_20"));
        game.Step("CherensAdvice");
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_ROUTE_20"));
        Assert.Equal(3, game.Bag.GetQuantity(ItemDatabase.Get("Pecha Berry")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_ROUTE_20"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_CHEREN_ROUTE_20"));

        PlayTheVirbankChapter(game);
    }

    /// <summary>
    /// Unova's Virbank chapter (plan 22 · U5, part b), played on from Route 20's end: the gate's Great Balls, Roxie and
    /// her father in the street, the studios' man at the north gate, the Gym's guide, Roxie's band and the Toxic Badge,
    /// the scout and Roxie's run, Team Plasma at the dock and HM01, the grunt on Route 20, the captain back at the dock
    /// and the two in the ship terminal.
    /// </summary>
    private static void PlayTheVirbankChapter(Game game)
    {
        // The gate from Route 20: two Great Balls for the road
        game.Arrive("VirbankGate", 2, 5);
        game.Step("GateGift");
        Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_VIRBANK_GATE"));
        Assert.False(game.Fires("GateGift"));

        // Virbank City: Roxie and the captain in the street, and the studios' man turning the player back
        game.Arrive("Unova", 195, 651);
        Assert.NotNull(game.Present("0", "virbank_city"));
        Assert.Null(game.Present("9", "virbank_city"));
        game.Step("RoxieAndTheCaptain");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROXIE_AND_CAPTAIN"));
        Assert.Null(game.Present("1", "virbank_city"));
        Assert.True(game.Fires("StudiosClosed"));
        game.Step("StudiosClosed");
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Studio Hand" && l.Text.Contains("Pokéstar"));

        // The Gym's way down: the guide's Fresh Water
        game.Arrive("VirbankCity_Gym", 1, 15);
        game.Step("GuideGreets");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Fresh Water")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_VIRBANK_GYM"));

        // Roxie's club: she can't hear anyone over her band until both bandmates are beaten
        game.Arrive("VirbankCity_GymStage", 7, 14);
        game.Talk("0", "virbank_city_gym_2");
        Assert.False(game.Story.HasBadge(RegionDatabase.Unova, "Toxic"));
        game.Talk("1", "virbank_city_gym_2");
        game.Talk("2", "virbank_city_gym_2");
        Assert.True(game.Story.HasDefeated("unova_roughneck_nicky"));
        Assert.True(game.Story.HasDefeated("unova_guitarist_billy_jo"));
        var roxie = game.Talk("0", "virbank_city_gym_2");
        Assert.Contains(roxie.Log, l => l.StartsWith("battle unova_leader_roxie "));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Toxic"));
        Assert.Equal(2, game.Story.BadgeCount(RegionDatabase.Unova));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM09")!));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRoleOf("Leader Roxie"));

        // On the way out: the studios' scout, and Roxie running off after her father; Team Plasma on the dock
        Assert.True(game.Fires("TheScout"));
        game.Step("TheScout");
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROXIE_GYM"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_POKESTAR_GUARD"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_VIRBANK_DOCK"));
        Assert.Null(game.Present("0", "virbank_city_gym_2"));

        // The dock: Team Plasma's speech, the grunt's battle, the grunts' flight and Roxie's HM01
        game.Arrive("Unova", 230, 670);
        Assert.NotNull(game.Present("9", "virbank_city"));
        Assert.Null(game.Present("6", "virbank_city"));
        game.Step("PlasmaAtTheDock");
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_VIRBANK"));
        var dock = game.Talk("9", "virbank_city");
        Assert.Contains(dock.Log, l => l.StartsWith("battle unova_team_plasma_grunt_44 "));
        Assert.Equal(MusicRole.BattleGalactic, MusicDirector.BattleRoleOf("Team Plasma Grunt"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_ROUTE_20"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_VIRBANK_DOCK"));
        Assert.Null(game.Present("9", "virbank_city"));

        // Route 20: Hugh searching by the bridge, and the grunt in the dark grass, who runs for the sea
        game.Arrive("Unova", 157, 660);
        var hugh = game.Present("21", "unova_route_20");
        Assert.NotNull(hugh);
        Assert.Equal((153, 645), (hugh!.GridX, hugh.GridY));
        var grunt = game.Talk("22", "unova_route_20");
        Assert.Contains(grunt.Log, l => l.StartsWith("battle unova_team_plasma_grunt_48 "));
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_ROUTE_20"));
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROUTE_20_GRUNT"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_ROUTE_20"));

        // The dock again: the captain back from his screen test, and off to his ship
        game.Arrive("Unova", 230, 670);
        var captain = game.Present("0", "virbank_city");
        Assert.NotNull(captain);
        Assert.Equal((243, 670), (captain!.GridX, captain.GridY));
        game.Step("TheCaptainSails");
        Assert.Equal(8, game.Story.Var("VAR_UNOVA_VIRBANK"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROXIE_AND_CAPTAIN"));

        // The ship terminal: Hugh bound for Castelia, and the captain takes them both across the bay (part c)
        game.Arrive("VirbankCity_Terminal", 6, 10);
        Assert.NotNull(game.Present("2", "virbank_city_5"));
        var terminal = game.Talk("3", "virbank_city_5");
        Assert.Contains(terminal.Transcript, l => l.Speaker == "Captain" && l.Text.Contains("Castelia"));

        PlayTheCasteliaChapter(game);
    }

    /// <summary>
    /// Unova's Castelia chapter (plan 22 · U5, part c), played on from the voyage: ashore at Prime Pier, the Harlequin's
    /// Bicycle on the avenue, Clyde and Iris on Gym Street, Iris on the avenue, Hugh at Thumb Pier, the sewers beside him
    /// with Team Plasma's two grunts, Burgh and HM04, Iris's word, Burgh's Gym and the Insect Badge, Colress on the
    /// plaza and his battle on Route 4.
    /// </summary>
    private static void PlayTheCasteliaChapter(Game game)
    {
        // Ashore at Prime Pier: Hugh sees a ship with black sails and runs off
        Assert.Equal("CasteliaCity_PrimePier", game.Map.Name);
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_TERMINAL"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_PIER"));
        Assert.Null(game.Present("1", "castelia_city_10"));

        // The avenue: the Harlequin's welcome and a Bicycle
        game.Through("CasteliaCity");
        Assert.Equal("CasteliaCity", game.Map.Name);
        Assert.True(game.Fires("HarlequinScene"));
        game.Step("HarlequinScene");
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!) >= 1);
        Assert.False(game.Fires("HarlequinScene"));

        // Gym Street: Clyde at the door with Burgh away, and Iris come looking for him
        game.Through("CasteliaCity_GymStreet");
        Assert.NotNull(game.Present("0", "castelia_city_3"));
        game.Talk("0", "castelia_city_3");
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_IRIS_GYM_STREET"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_IRIS_AVENUE"));

        // The avenue again: Iris calls the player on and runs down to Thumb Pier
        game.Through("CasteliaCity");
        Assert.Contains(game.Map.NPCs, n => n.Key == "iris");
        game.Step("IrisCalls");
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_IRIS_AVENUE"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_IRIS_PIER"));

        // Thumb Pier: Hugh comes running and goes down into the sewers
        game.Through("CasteliaCity_ThumbPier");
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_THUMB"));
        Assert.NotNull(game.Present("1", "castelia_city_12"));

        // The sewers: Hugh comes along, the two grunts beaten beside him, Burgh, HM04 and the stranger in the white coat
        game.Through("CasteliaSewers");
        Assert.NotNull(game.Present("0", "castelia_sewers"));
        game.Step("HughJoins");
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_CASTELIA"));
        var sewers = game.Step("PlasmaInTheSewers");
        Assert.Contains(sewers.Log, l => l.StartsWith("battle unova_team_plasma_grunt "));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM04")!));
        Assert.Equal(8, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_SEWER_GRUNTS"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_SEWERS"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_BURGH_GYM"));
        Assert.Null(game.Present("1", "castelia_sewers"));

        // Back up on Thumb Pier: Iris's word, and Clyde gone in from the Gym's door
        game.Through("CasteliaCity_ThumbPier");
        game.Step("IrisAfterward");
        Assert.Equal(9, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_CLYDE_STREET"));

        // The Gym: Clyde's Fresh Water, Burgh's battle, the Insect Badge and TM76
        game.Arrive("CasteliaCity_Gym", 16, 19);
        game.Talk("4", "castelia_city_gym");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_WATER_CASTELIA"));
        var burgh = game.Talk("burgh", "castelia_city_gym");
        Assert.Contains(burgh.Log, l => l.StartsWith("battle unova_leader_burgh "));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Insect"));
        Assert.Equal(3, game.Story.BadgeCount(RegionDatabase.Unova));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM76")!));
        Assert.Equal(10, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRoleOf("Leader Burgh"));

        // The plaza: Colress looks over the team and goes to wait on Route 4
        game.Arrive("CasteliaCity_Plaza", 15, 6);
        Assert.NotNull(game.Present("0", "castelia_city_2"));
        game.Step("Colress");
        Assert.Equal(11, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_COLRESS_PLAZA"));

        // Route 4, past the gate: the Crustle woken, Colress's battle and his Protein
        game.Arrive("CasteliaGate", 5, 12);
        game.Through("Unova");
        Assert.NotNull(game.Present("12", "unova_route_4"));
        var route4 = game.Step("ColressWaits");
        Assert.Contains(route4.Log, l => l.StartsWith("battle unova_colress "));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Protein")!));
        Assert.Equal(12, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_CRUSTLE_ROUTE_4"));
        Assert.Null(game.Present("12", "unova_route_4"));

        PlayTheNimbasaChapter(game);
    }

    /// <summary>
    /// Unova's Nimbasa chapter (plan 22 · U6), played on from Route 4: the rest house, the Desert Resort's Soft Sand, the
    /// Subway Bosses beside the hero the player isn't and the Vs. Recorder, the Nimbasa Gym's three models, Elesa and
    /// the Bolt Badge, and Hugh against Team Plasma on the street to Route 5.
    /// </summary>
    private static void PlayTheNimbasaChapter(Game game)
    {
        // Route 4's trainers stand on the road now, and its rest house sees to the team
        Assert.NotNull(game.Present("0", "unova_route_4"));
        game.Arrive("Route4_RestHouse", 4, 9);
        var lead = game.Party.Members[0];
        lead.CurrentHP = 1;
        game.Talk("1", "unova_route_4_3");
        Assert.Equal(lead.MaxHP, lead.CurrentHP);

        // The Desert Resort beyond its gate house: the archaeologist's Soft Sand, once
        game.Arrive("DesertResort", 75, 49);
        game.Talk("0", "desert_resort_2");
        game.Talk("0", "desert_resort_2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Soft Sand")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_SOFT_SAND"));

        // Nimbasa City, in from Join Avenue: Rosa before the Gear Station (the player is a boy), and nobody of Team Plasma
        game.Arrive("Unova", 430, 477);
        Assert.NotNull(game.Present("5", "nimbasa_city"));
        Assert.Null(game.Present("nate", "nimbasa_city"));
        Assert.Null(game.Present("3", "nimbasa_city"));
        Assert.Null(game.Present("0", "nimbasa_city"));

        // Beside Rosa against the Subway Bosses, and her Vs. Recorder
        var subway = game.Talk("5", "nimbasa_city");
        Assert.Contains(subway.Log, l => l.StartsWith("battle ") && l.Contains("unova_subway_boss_ingo"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Recorder")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_SUBWAY_BOSSES"));
        Assert.Null(game.Present("5", "nimbasa_city"));
        Assert.Null(game.Present("6", "nimbasa_city"));

        // The Gym in the amusement park: the guide's Fresh Water, the three models down the runway, Elesa and the Bolt Badge
        game.Arrive("NimbasaCity_Gym", 15, 59);
        Assert.False(game.Fires("Fleming"));
        game.Talk("1", "nimbasa_city_gym");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_WATER_NIMBASA"));
        // (before the runway is walked Elesa only waits)
        var early = game.Talk("4", "nimbasa_city_gym");
        Assert.DoesNotContain(early.Log, l => l.StartsWith("battle "));
        Assert.Contains(game.Step("Nikola").Log, l => l.StartsWith("battle unova_beauty_nikola "));
        Assert.Contains(game.Step("Fleming").Log, l => l.StartsWith("battle unova_beauty_fleming "));
        Assert.Contains(game.Step("Ampere").Log, l => l.StartsWith("battle unova_beauty_amp_re "));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_NIMBASA_GYM"));
        var elesa = game.Talk("4", "nimbasa_city_gym");
        Assert.Contains(elesa.Log, l => l.StartsWith("battle unova_leader_elesa "));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Bolt"));
        Assert.Equal(4, game.Story.BadgeCount(RegionDatabase.Unova));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM72")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_NIMBASA"));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRoleOf("Leader Elesa"));

        // The street to Route 5: Hugh and Team Plasma, the two grunts beaten, and Hugh's word of his sister's Purrloin
        game.Arrive("Unova", 420, 438);
        Assert.NotNull(game.Present("3", "nimbasa_city"));
        Assert.NotNull(game.Present("0", "nimbasa_city"));
        var plasma = game.Step("HughAndPlasma");
        Assert.Contains(plasma.Log, l => l.StartsWith("battle unova_team_plasma_grunt_4 "));
        Assert.Contains(plasma.Log, l => l.StartsWith("battle unova_team_plasma_grunt_47 "));
        Assert.Contains(plasma.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Purrloin"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_NIMBASA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_NIMBASA_PLASMA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_NIMBASA"));
        Assert.Null(game.Present("3", "nimbasa_city"));
        Assert.False(game.Fires("HughAndPlasma"));

        PlayTheDriftveilChapter(game);
    }

    /// <summary>
    /// Unova's Driftveil chapter (plan 22 · U6, part b), played on from Nimbasa: Bianca's Fly on Route 5, Hugh chasing
    /// Team Plasma's grunt off Driftveil's bridge, Rood's test at the old church and his tale inside, the Gym's lobby and
    /// its Fresh Water, Clay and the Quake Badge on the Gym's floor of ours, and Clay's word of the World Tournament.
    /// </summary>
    private static void PlayTheDriftveilChapter(Game game)
    {
        // Route 5, out of Nimbasa's west gate: Bianca runs after the player with HM02
        game.Arrive("Unova", 376, 438);
        Assert.Null(game.Present("bianca", "unova_route_5"));
        Assert.True(game.Fires("BiancasFly"));
        var fly = game.Step("BiancasFly");
        Assert.Contains(fly.Transcript, l => l.Speaker == "Bianca" && l.Text.Contains("Grotto"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM02")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ROUTE_5"));
        Assert.Null(game.Present("bianca", "unova_route_5"));
        Assert.False(game.Fires("BiancasFly"));

        // Driftveil's bridge into town: the grunt and the former member, Hugh's chase
        game.Arrive("Unova", 230, 432);
        Assert.NotNull(game.Present("3", "driftveil_city"));
        Assert.NotNull(game.Present("2", "driftveil_city"));
        Assert.Null(game.Present("5", "driftveil_city"));
        // (the Gym's door is kept by a worker while Clay is busy)
        Assert.NotNull(game.Present("1", "driftveil_city"));
        var bridge = game.Step("PlasmaOnTheBridge");
        Assert.Contains(bridge.Transcript, l => l.Speaker == "Hugh");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_DRIFTVEIL"));
        Assert.Null(game.Present("3", "driftveil_city"));
        Assert.Null(game.Present("2", "driftveil_city"));
        Assert.Null(game.Present("5", "driftveil_city"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_EXPLASMA_HOME"));

        // Rood at the old church's door: his test, Hugh back from the chase, and the tale inside
        game.Arrive("Unova", 192, 399);
        Assert.NotNull(game.Present("4", "driftveil_city"));
        var rood = game.Step("RoodCalls");
        Assert.Contains(rood.Log, l => l.StartsWith("battle unova_rood "));
        Assert.Equal("DriftveilCity_PlasmaHouse", game.Map.Name);
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_DRIFTVEIL"));
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Speaker == "Rood" && l.Text.Contains("Ghetsis")));
        Assert.NotNull(game.Present("0", "driftveil_city_6"));
        Assert.NotNull(game.Present("8", "driftveil_city_6"));
        Assert.Null(game.Present("12", "driftveil_city_6"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_DRIFTVEIL_GYM_WORKER"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROOD_DOOR"));
        Assert.Equal(MusicRole.BattleTrainer, MusicDirector.BattleRoleOf("Pokémon Trainer Rood"));

        // The Gym's lobby: the guide's Fresh Water, and the lift down to the Gym's floor
        game.Arrive("DriftveilCity_GymLobby", 7, 13);
        game.Talk("0", "driftveil_city_gym_2");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_WATER_DRIFTVEIL"));
        game.Through("DriftveilCity_Gym");
        Assert.Equal("DriftveilCity_Gym", game.Map.Name);

        // Clay at the head of the mine: his battle, the Quake Badge and TM78
        var clay = game.TalkTo("clay");
        Assert.Contains(clay.Log, l => l.StartsWith("battle unova_leader_clay "));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Quake"));
        Assert.Equal(5, game.Story.BadgeCount(RegionDatabase.Unova));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM78")!));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_DRIFTVEIL"));
        Assert.Equal(MusicRole.BattleGymLeader, MusicDirector.BattleRoleOf("Leader Clay"));

        // Out of the Gym's door: Clay and Hugh, and the World Tournament south of the city
        game.Arrive("Unova", 212, 401);
        Assert.NotNull(game.Present("6", "driftveil_city"));
        var outside = game.Step("ClayAndHugh");
        Assert.Contains(outside.Transcript, l => l.Speaker == "Clay" && l.Text.Contains("World Tournament"));
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_DRIFTVEIL"));
        Assert.Null(game.Present("6", "driftveil_city"));
        Assert.Null(game.Present("hugh_gym", "driftveil_city"));
        Assert.False(game.Fires("ClayAndHugh"));

        PlayTheFrigateChapter(game);
    }

    /// <summary>
    /// Unova's chapter of the Plasma Frigate (plan 22 · U6, part c), played on from Clay's word of the tournament: the
    /// gangway shut until the first step onto the tournament's pier plays Cheren, Hugh, the running grunt and Colress;
    /// aboard, the two grunts' battles, Zinzolin and the Shadow Triad; ashore again, Hugh and Cheren on the pier, and the
    /// frigate gone.
    /// </summary>
    private static void PlayTheFrigateChapter(Game game)
    {
        // The tournament's grounds south of Driftveil City: the frigate's gangway is shut until its scene
        game.Arrive("Unova", 197, 470);
        Assert.Equal("pwt", game.Map.AreaAt(197, 470)?.Key);
        Assert.All(game.Map.Warps.Where(w => w.TargetMap == "PlasmaFrigate"), w => Assert.True(w.Shut));
        Assert.Null(game.Present("hugh_pwt", "pwt"));
        Assert.True(game.Fires("PlasmaAtThePier"));
        var pier = game.Step("PlasmaAtThePier");
        Assert.Contains(pier.Transcript, l => l.Speaker == "Cheren");
        Assert.Contains(pier.Transcript, l => l.Speaker == "Colress");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_PWT"));
        Assert.Null(game.Present("hugh_pwt", "pwt"));
        Assert.Null(game.Present("colress_pwt", "pwt"));
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "PlasmaFrigate" && !w.Shut);
        Assert.False(game.Fires("PlasmaAtThePier"));

        // Aboard: two grunts, Zinzolin and the Shadow Triad, who put everyone back on the pier
        game.Tile = (198, 494);
        game.Through("PlasmaFrigate");
        Assert.Contains(game.Played, p => p.Host.Log.Any(l => l.StartsWith("battle unova_team_plasma_grunt_5")));
        Assert.Contains(game.Played, p => p.Host.Log.Any(l => l.StartsWith("battle unova_team_plasma_grunt_6")));
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Speaker == "Zinzolin" && l.Text.Contains("ice")));
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Speaker == "Shadow Triad"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_FRIGATE_GRUNTS"));

        // Ashore: Hugh and Cheren on the pier, and the frigate gone
        Assert.Equal("Unova", game.Map.Name);
        Assert.Equal("pwt", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Speaker == "Cheren" && l.Text.Contains("Route 6")));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_PWT"));
        Assert.True(game.Story.Has("FLAG_UNOVA_FRIGATE_SAILED"));
        Assert.Null(game.Present("hugh_pier", "pwt"));
        Assert.Null(game.Present("cheren_pier", "pwt"));
        Assert.All(game.Map.Warps.Where(w => w.TargetMap == "PlasmaFrigate"), w => Assert.True(w.Shut));
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_HUGH_FRIGATE", "FLAG_UNOVA_HIDE_CHEREN_FRIGATE", "FLAG_UNOVA_HIDE_ZINZOLIN_FRIGATE",
                     "FLAG_UNOVA_HIDE_TRIAD_FRIGATE" })
            Assert.True(game.Story.Has(flag), flag);
    }

    [Fact]
    public void TheFrigatesScenePlaysOnlyOnceClayHasToldOfTheTournament()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
        game.Arrive("Unova", 197, 470);
        // Before the Quake Badge the pier is only a pier
        game.Step("PlasmaAtThePier");
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_PWT"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_PWT"));
        Assert.All(game.Map.Warps.Where(w => w.TargetMap == "PlasmaFrigate"), w => Assert.True(w.Shut));
        // and whoever reaches the deck otherwise is seen straight back down the gangway
        game.Arrive("PlasmaFrigate", 9, 29);
        Assert.Equal("Unova", game.Map.Name);
        Assert.DoesNotContain(game.Played, p => p.Host.Log.Any(l => l.StartsWith("battle ")));
    }

    [Fact]
    public void ASaveFromBeforeTheFrigateChapterIsGivenItsHiddenPeople()
    {
        // A save of plan 22 · U6, part b's: the Quake Badge won, nothing of the frigate's chapter set
        var game = new Game(begin: false);
        foreach (string flag in new[] { "FLAG_UNOVA_STORY_BEGUN", "FLAG_UNOVA_VIRBANK_BEGUN", "FLAG_UNOVA_CASTELIA_BEGUN", "FLAG_UNOVA_NIMBASA_BEGUN",
                     "FLAG_UNOVA_DRIFTVEIL_BEGUN" })
            game.Story.Set(flag);
        game.Story.SetVar("VAR_UNOVA_ASPERTIA", 7);
        game.Story.SetVar("VAR_UNOVA_DRIFTVEIL", 5);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 34));
        game.Arrive("Unova", 197, 470);
        Assert.True(game.Story.Has("FLAG_UNOVA_FRIGATE_BEGUN"));
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_HUGH_PWT", "FLAG_UNOVA_HIDE_CHEREN_PWT", "FLAG_UNOVA_HIDE_COLRESS_PWT",
                     "FLAG_UNOVA_HIDE_HUGH_FRIGATE", "FLAG_UNOVA_HIDE_ZINZOLIN_FRIGATE", "FLAG_UNOVA_HIDE_LOSTLORN_ZOROARK" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Null(game.Present("hugh_pwt", "pwt"));
        // and Driftveil's count stands, while the pier waits for its scene
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_DRIFTVEIL"));
        Assert.True(game.Fires("PlasmaAtThePier"));
    }

    [Fact]
    public void TheLostlornForestsTravellerIsAZoroark()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("LostlornForest", 12, 50);
        Assert.NotNull(game.Present("7", "lostlorn_forest"));
        Assert.Null(game.Present("zoroark", "lostlorn_forest"));
        var traveller = game.Talk("7", "lostlorn_forest");
        Assert.Contains(traveller.Transcript, l => l.Speaker == "Traveller" && l.Text.Contains("Zoroark"));
        Assert.Contains(traveller.Log, l => l.StartsWith("cry Zoroark"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM46")!));
        // the traveller is gone for good, and so is the Zoroark
        Assert.Null(game.Present("7", "lostlorn_forest"));
        Assert.Null(game.Present("zoroark", "lostlorn_forest"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_LOSTLORN_TRAVELLER"));
    }

    [Fact]
    public void TheGearStationsTrainRunsToAnvilleTownAndBack()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("GearStation", 17, 4);
        // The Subway's desks only talk until its lines run
        var single = game.TalkTo("single_desk");
        Assert.DoesNotContain(single.Log, l => l.StartsWith("battle ") || l.StartsWith("warp "));
        // The Anville Town Line's agent takes the player out to Anville
        var line = game.TalkTo("anville_desk");
        Assert.Contains(line.Transcript, l => l.Text.Contains("rolls into Anville Town"));
        Assert.Equal("AnvilleTown", game.Map.Name);
        // a weekday: the yard is quiet, its weekend crowd away
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ANVILLE_WEEKEND"));
        Assert.Null(game.Present("14", "anville_town"));
        Assert.NotNull(game.Present("0", "anville_town"));
        // the lost and found: a Rare Candy first, then one thing a day
        game.Arrive("AnvilleTown_House3", 5, 8);
        game.Talk("0", "anville_town_4");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        var again = game.Talk("0", "anville_town_4");
        Assert.Contains(again.Transcript, l => l.Text.Contains("Come back tomorrow"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        // and the station agent brings the player back to the Gear Station
        game.Arrive("AnvilleTown", 20, 52);
        game.Talk("2", "anville_town");
        Assert.Equal("GearStation", game.Map.Name);

        // At the weekend the town turns out to see the trains
        var weekend = new Game { Weekday = DayOfWeek.Saturday };
        weekend.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        weekend.Arrive("AnvilleTown", 20, 52);
        Assert.False(weekend.Story.Has("FLAG_UNOVA_HIDE_ANVILLE_WEEKEND"));
        Assert.NotNull(weekend.Present("14", "anville_town"));
    }

    [Fact]
    public void TheRelicCastlesSandPitsDropToTheFloorBelowAndVolcaronaWaitsInItsDepths()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 36));
        game.Arrive("RelicCastle", 24, 23);
        game.Step("SandPitWest");
        Assert.Equal("RelicCastle_B1F", game.Map.Name);
        Assert.Equal((10, 10), game.Tile);
        Assert.True(game.Map.IsWalkable(10, 10));
        game.Arrive("RelicCastle", 24, 23);
        game.Step("SandPitEast");
        Assert.Equal(("RelicCastle_B1F", (15, 11)), (game.Map.Name, game.Tile));
        Assert.True(game.Map.IsWalkable(15, 11));

        // Volcarona at the head of the deepest room: beaten, it is gone for good
        game.Arrive("RelicCastle_Depths", 16, 26);
        Assert.NotNull(game.Present("0", "relic_castle_23"));
        var volcarona = game.Talk("0", "relic_castle_23");
        Assert.Contains(volcarona.Log, l => l.StartsWith("wildbattle Volcarona 35"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_VOLCARONA"));
        Assert.Null(game.Present("0", "relic_castle_23"));
    }

    [Fact]
    public void JoinAvenuesThreeShopsSellFromTheirCounters()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("JoinAvenue", 15, 74);
        foreach (var (key, mart, sign) in new[] { ("market", "join_avenue_market", (7, 27)), ("florist", "join_avenue_flowers", (23, 20)),
                     ("waitress", "join_avenue_cafe", (7, 41)) })
        {
            var clerk = game.Present(key, "join_avenue")!;
            // behind a counter, spoken to across it, its end the shop's sign
            int across = clerk.Facing == Direction.Right ? 1 : -1;
            Assert.True(game.Map.IsCounter(clerk.GridX + across, clerk.GridY));
            Assert.True(game.Map.TileScripts.ContainsKey(sign));
            var shop = game.Talk(key, "join_avenue");
            Assert.Contains(shop.Log, l => l == $"open Shop {mart}");
            Assert.NotEmpty(MartDatabase.Stock(mart, 0));
        }
        Assert.Contains(MartDatabase.Stock("join_avenue_cafe", 0), i => i.Name == "Moomoo Milk");
    }

    [Fact]
    public void ABoulderPushedIntoARelicPassageHoleFillsItForGood()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 36));
        game.Arrive("RelicPassage", 88, 12);
        // The hole before the tunnel up into the Relic Castle is a wall, its tiles pits a pushed boulder may come to rest in
        Assert.True(game.Map.IsSolid(88, 7));
        Assert.Equal("FLAG_UNOVA_RELIC_PASSAGE_HOLE_NORTH", game.Map.FilledHoles[(88, 7)]);
        Assert.Equal("FillTheNorthHole", game.Map.BoulderPits[(88, 7)]);
        var boulder = game.Present("9", "relic_passage_2")!;
        Assert.Equal(PropType.StrengthBoulder, boulder.Obstacle);
        // The boulder comes to rest in it: the hole is filled, walked across, and the boulder is gone with it
        var fall = game.Play(Scripts.Find("FillTheNorthHole", "relic_passage_2")!, boulder);
        Assert.Contains(fall.Transcript, l => l.Text.Contains("filled it in"));
        Assert.True(game.Story.Has("FLAG_UNOVA_RELIC_PASSAGE_HOLE_NORTH"));
        Assert.False(game.Map.IsSolid(88, 7));
        Assert.False(game.Map.IsSolid(89, 6));
        Assert.Null(game.Present("9", "relic_passage_2"));
        // and on the next visit too
        game.Arrive("RelicPassage", 88, 12);
        Assert.False(game.Map.IsSolid(88, 7));
        Assert.Null(game.Present("9", "relic_passage_2"));
    }

    [Fact]
    public void ANewGameOfUnovaBeginsWithNothingAndTheChaptersPeopleHidden()
    {
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        Assert.True(unova.HasStory);
        var game = new Game();
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_MOM_ASPERTIA", "FLAG_UNOVA_HIDE_CHEREN_ASPERTIA", "FLAG_UNOVA_HIDE_BIANCA_CENTER",
                     "FLAG_UNOVA_HIDE_BIANCA_ROUTE_19", "FLAG_UNOVA_HIDE_RANCH_OWNERS", "FLAG_UNOVA_HIDE_RANCH_GRUNT",
                     "FLAG_UNOVA_HIDE_HUGH_ROUTE_20", "FLAG_UNOVA_HIDE_CHEREN_ROUTE_20",
                     "FLAG_UNOVA_HIDE_ROXIE_SCENE", "FLAG_UNOVA_HIDE_VIRBANK_DOCK", "FLAG_UNOVA_HIDE_ROUTE_20_GRUNT",
                     "FLAG_UNOVA_HIDE_CAPTAIN_TERMINAL", "FLAG_UNOVA_HIDE_HUGH_TERMINAL", "FLAG_UNOVA_VIRBANK_BEGUN",
                     "FLAG_UNOVA_HIDE_HUGH_PIER", "FLAG_UNOVA_HIDE_IRIS_GYM_STREET", "FLAG_UNOVA_HIDE_SEWER_GRUNTS", "FLAG_UNOVA_HIDE_BURGH_GYM",
                     "FLAG_UNOVA_HIDE_COLRESS_PLAZA", "FLAG_UNOVA_HIDE_COLRESS_ROUTE_4", "FLAG_UNOVA_CASTELIA_BEGUN",
                     "FLAG_UNOVA_HIDE_HUGH_NIMBASA", "FLAG_UNOVA_HIDE_NIMBASA_PLASMA", "FLAG_UNOVA_HIDE_ROSA_NIMBASA",
                     "FLAG_UNOVA_HIDE_NATE_NIMBASA", "FLAG_UNOVA_NIMBASA_BEGUN", "FLAG_UNOVA_FRIGATE_BEGUN", "FLAG_UNOVA_HIDE_HUGH_PWT",
                     "FLAG_UNOVA_HIDE_ZINZOLIN_FRIGATE", "FLAG_UNOVA_HIDE_LOSTLORN_ZOROARK" })
            Assert.True(game.Story.Has(flag), flag);
        // Mom at home, Hugh and his sister, Bianca at the outlook and Alder in the open from the start
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_MOM_HOME", "FLAG_UNOVA_HIDE_HUGH_ASPERTIA", "FLAG_UNOVA_HIDE_HUGHS_SISTER",
                     "FLAG_UNOVA_HIDE_BIANCA_ASPERTIA", "FLAG_UNOVA_HIDE_ALDER_ROUTE_19", "FLAG_UNOVA_HIDE_ALDER_FLOCCESY" })
            Assert.False(game.Story.Has(flag), flag);
        // Hugh's battles are the rival's, called by the name the player gave him, to the rival's song
        Assert.True(TrainerDatabase.IsRival(TrainerDatabase.Get("unova_rival")!.Class));
        Assert.Equal(MusicRole.BattleRival, MusicDirector.BattleRoleOf(TrainerDatabase.RivalClass));
    }

    [Fact]
    public void AGameBegunInUnovaBeforeItsStoryIsTakenPastTheOpening()
    {
        // A save of plan 22 · U2's: begun in Unova with a Pokémon and none of the chapter's flags
        var game = new Game(begin: false);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 5));
        game.Arrive("Unova", 47, 762);
        Assert.True(game.Story.Has("FLAG_UNOVA_STORY_BEGUN"));
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        Assert.Single(game.Party.Members);

        // A Champion who crossed in plays the opening all the same
        var crossed = new Game(beganIn: RegionDatabase.Hoenn, begin: false);
        crossed.Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 60));
        crossed.Arrive("Unova", 47, 762);
        Assert.Equal(1, crossed.Story.Var("VAR_UNOVA_ASPERTIA"));
        Assert.Contains(crossed.Played, p => p.Host.Log.Any(l => l.StartsWith("open NameRival")));
    }

    [Fact]
    public void UnovasRolesHaveSongsOfTheirOwn()
    {
        bool Exists(string id) => File.Exists(GameDataFiles.PathOf(Path.Combine("music", id + ".mml")));
        foreach (string song in new[] { "aspertia", "route19", "floccesy", "gym", "battle_wild", "battle_trainer", "battle_rival", "battle_gym",
                     "victory_trainer", "victory_gym", "eye_boy", "eye_girl", "virbank", "eye_plasma", "castelia",
                     "route4", "nimbasa", "desert_resort", "driftveil", "lostlorn_forest", "relic_castle", "relic_passage", "plasma_frigate", "subway", "anville" })
            Assert.True(Exists("unova/" + song), song);
        Assert.Equal("unova/battle_rival", MusicDirector.Resolve(MusicRole.BattleRival, RegionDatabase.Unova, Exists));
        foreach (string key in new[] { "aspertia_city", "unova_route_19", "floccesy_town", "virbank_city", "virbank_complex_2", "castelia_city_3", "castelia_city_10",
                     "unova_route_4", "nimbasa_city", "nimbasa_city_2", "join_avenue", "desert_resort_2",
                     "unova_route_5", "driftveil_drawbridge", "driftveil_city", "driftveil_city_7", "musical_theater",
                     "unova_route_16", "lostlorn_forest", "anville_town", "relic_passage_2", "relic_castle", "relic_castle_23", "pwt", "plasma_frigate" })
            Assert.StartsWith("unova/", Unova.Value.Overlay(key)!.BgmTrack);
        // Team Plasma's grunts bring a song of Unova's own when they spot the player (plan 22 · U5, part b)
        Assert.Equal("unova/eye_plasma", MusicDirector.EyeSong("Team Plasma Grunt", RegionDatabase.Unova, Exists));
    }

    [Fact]
    public void ASaveFromBeforeTheVirbankChapterIsGivenItsHiddenPeople()
    {
        // A save of part a's: the story begun, Route 20's end reached, nothing of Virbank's set
        var game = new Game(begin: false);
        game.Story.Set("FLAG_UNOVA_STORY_BEGUN");
        game.Story.SetVar("VAR_UNOVA_ASPERTIA", 7);
        game.Story.SetVar("VAR_UNOVA_ROUTE_20", 3);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 12));
        game.Arrive("Unova", 157, 660);
        Assert.True(game.Story.Has("FLAG_UNOVA_VIRBANK_BEGUN"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_VIRBANK_DOCK"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_ROUTE_20_GRUNT"));
        Assert.Null(game.Present("22", "unova_route_20"));
        // and the first chapter's flags are not set again
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_ASPERTIA"));
    }

    [Fact]
    public void ASaveFromBeforeTheCasteliaChapterIsGivenItsHiddenPeople()
    {
        // A save of part b's: Virbank's chapter played to the terminal, nothing of Castelia's set
        var game = new Game(begin: false);
        game.Story.Set("FLAG_UNOVA_STORY_BEGUN");
        game.Story.Set("FLAG_UNOVA_VIRBANK_BEGUN");
        game.Story.SetVar("VAR_UNOVA_ASPERTIA", 7);
        game.Story.SetVar("VAR_UNOVA_VIRBANK", 8);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 20));
        game.Arrive("VirbankCity_Terminal", 6, 10);
        Assert.True(game.Story.Has("FLAG_UNOVA_CASTELIA_BEGUN"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_COLRESS_PLAZA"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BURGH_GYM"));
        // and the chapter waits for the voyage: the captain sails nowhere before Hugh has asked
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_CASTELIA"));
        Assert.Equal(8, game.Story.Var("VAR_UNOVA_VIRBANK"));
    }

    [Fact]
    public void ASaveFromBeforeTheNimbasaChapterIsGivenItsHiddenPeople()
    {
        // A save of part c's: Colress beaten on Route 4, nothing of Nimbasa's set
        var game = new Game(begin: false);
        foreach (string flag in new[] { "FLAG_UNOVA_STORY_BEGUN", "FLAG_UNOVA_VIRBANK_BEGUN", "FLAG_UNOVA_CASTELIA_BEGUN" }) game.Story.Set(flag);
        game.Story.SetVar("VAR_UNOVA_ASPERTIA", 7);
        game.Story.SetVar("VAR_UNOVA_CASTELIA", 12);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("Unova", 430, 477);
        Assert.True(game.Story.Has("FLAG_UNOVA_NIMBASA_BEGUN"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_NIMBASA_PLASMA"));
        Assert.Null(game.Present("3", "nimbasa_city"));
        Assert.Null(game.Present("0", "nimbasa_city"));
        // the young Trainer before the Gear Station is shown as the city is come to, and Castelia's count stands
        Assert.NotNull(game.Present("5", "nimbasa_city"));
        Assert.Equal(12, game.Story.Var("VAR_UNOVA_CASTELIA"));
    }

    [Fact]
    public void ASaveFromBeforeTheDriftveilChapterIsGivenItsHiddenPeople()
    {
        // A save of plan 22 · U6, part a's: Hugh's scene in Nimbasa played, nothing of Driftveil's set
        var game = new Game(begin: false);
        foreach (string flag in new[] { "FLAG_UNOVA_STORY_BEGUN", "FLAG_UNOVA_VIRBANK_BEGUN", "FLAG_UNOVA_CASTELIA_BEGUN", "FLAG_UNOVA_NIMBASA_BEGUN" })
            game.Story.Set(flag);
        game.Story.SetVar("VAR_UNOVA_ASPERTIA", 7);
        game.Story.SetVar("VAR_UNOVA_NIMBASA", 2);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
        game.Arrive("Unova", 376, 438);
        Assert.True(game.Story.Has("FLAG_UNOVA_DRIFTVEIL_BEGUN"));
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_BIANCA_ROUTE_5", "FLAG_UNOVA_HIDE_HUGH_DRIFTVEIL", "FLAG_UNOVA_HIDE_ROOD_HOME",
                     "FLAG_UNOVA_HIDE_CLAY_DRIFTVEIL", "FLAG_UNOVA_HIDE_HUGH_GYM" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.Null(game.Present("bianca", "unova_route_5"));
        // and Nimbasa's count stands, while Route 5 waits for Bianca
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_NIMBASA"));
        Assert.True(game.Fires("BiancasFly"));
    }

    [Fact]
    public void TheFerrisWheelGoesRoundOverNimbasa()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 25));
        game.Arrive("NimbasaCity_AmusementPark", 57, 17);
        var ride = game.Talk("wheel", "nimbasa_city_2");
        Assert.Contains(ride.Transcript, l => l.Text.Contains("The car climbs slowly"));
        Assert.Contains(ride.Transcript, l => l.Speaker == "Attendant" && l.Text.Contains("Welcome back"));
        // a ride changes nothing of the story
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_NIMBASA"));
        // Said no, there is no ride
        var npc = game.Present("wheel", "nimbasa_city_2")!;
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = (npc.GridX, npc.GridY + 1) };
        host.Answers.Enqueue(1);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(FieldScripts.For(npc)!, "nimbasa_city_2")!, npc);
        runner.RunToEnd();
        Assert.DoesNotContain(host.Transcript, l => l.Text.Contains("The car climbs slowly"));
    }

    [Fact]
    public void AGirlBattlesTheSubwayBossesBesideNate()
    {
        var game = new Game(starter: 2) { Look = PlayerLook.Girl };
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Oshawott")!, 25));
        game.Story.ChooseStarter("Oshawott");
        game.Arrive("Unova", 430, 477);
        Assert.NotNull(game.Present("nate", "nimbasa_city"));
        Assert.Null(game.Present("5", "nimbasa_city"));
        var subway = game.Talk("nate", "nimbasa_city");
        // Nate brings the starter neither the player (Oshawott) nor Hugh took, as Black 2's TRAINER_NATE_3 does
        Assert.Contains(subway.Log, l => l.StartsWith("battle unova_subway_boss_ingo and unova_subway_boss_emmet with unova_nate_3 "));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Recorder")!));
        Assert.Null(game.Present("nate", "nimbasa_city"));
        // Said no, the Bosses wait for another time
        var other = new Game() { Look = PlayerLook.Girl };
        other.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 25));
        other.Arrive("Unova", 430, 477);
        var npc = other.Present("nate", "nimbasa_city")!;
        other.Tile = (npc.GridX, npc.GridY + 1);
        var host = new HeadlessScriptHost(other.Story, other.Party, other.Bag) { Map = other.Map, PlayerTile = other.Tile, PlayerLook = PlayerLook.Girl, NeedsPokemon = true };
        host.Answers.Enqueue(1);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(FieldScripts.For(npc)!, "nimbasa_city")!, npc);
        runner.RunToEnd();
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle "));
        Assert.False(other.Story.Has("FLAG_UNOVA_HIDE_SUBWAY_BOSSES"));
    }
}
