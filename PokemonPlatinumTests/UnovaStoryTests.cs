using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
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
        /// <summary>The answers the next script played is given, in the order it asks (plan 22 · U6, part d: the Subway's menus).</summary>
        public List<int> Answers { get; } = new();
        /// <summary>The battle of a script (counting from 1) that is lost, where one is (plan 22 · U6, part d: a Subway run ended early).</summary>
        public int LoseAt { get; set; }
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
            // and the Battle Subway's car (plan 22 · U6, part d)
            maps["BattleSubwayTrain"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "BattleSubwayTrain.json")).ToMap();
            // and the World Tournament's hall (plan 22 · U6, part d)
            maps["PokemonWorldTournament"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "PokemonWorldTournament.json")).ToMap();
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
            int fought = 0;
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                StarterChoice = Starter,
                RivalName = "Hugh",
                Fight = _ => ++fought == LoseAt ? BattleOutcome.Lost : Fight,
                NeedsPokemon = true,
                BeganInRegion = BeganIn,
                PlayerLook = Look,
                Weekday = Weekday
            };
            foreach (int answer in Answers) host.Answers.Enqueue(answer);
            Answers.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            // what this script didn't ask is for the next (a warp's arrival script)
            Answers.AddRange(host.Answers);
            host.Answers.Clear();
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
        // The hall is open, and its first tournament comes first: until it has been played the pier is only a pier
        game.Step("PlasmaAtThePier");
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_PWT"));
        // (a team of three to enter: the chapter has played with the starter alone)
        foreach (string species in new[] { "Lillipup", "Pidove" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 36));
        game.Tile = (197, 466);
        game.Through("PokemonWorldTournament");
        Assert.True(game.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));
        game.Arrive("Unova", 197, 470);
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

    // ------------------------------------------------------------------ the World Tournament's hall (plan 22 · U6, part d)

    private static Game AtTheTournament(int starter, Action<Game>? before = null)
    {
        var game = new Game(starter);
        foreach (string species in new[] { "Snivy", "Lillipup", "Pidove", "Roggenrola" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 36));
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Unova)!.Starters[starter]);
        game.Story.SetVar("VAR_UNOVA_DRIFTVEIL", 5);
        before?.Invoke(game);
        game.Arrive("Unova", 197, 470);
        return game;
    }

    [Theory]
    [InlineData(0, "unova_rival_10")]
    [InlineData(1, "unova_rival_11")]
    [InlineData(2, "unova_rival_12")]
    public void TheFirstTournamentIsAKnockoutOfThreeRoundsAndOpensThePier(int starter, string hugh)
    {
        var game = AtTheTournament(starter);
        // Clay's word opens the hall's door (a save past it is given the flag as the grounds are come to)
        Assert.True(game.Story.Has("FLAG_UNOVA_PWT_OPEN"));
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "PokemonWorldTournament" && !w.Shut);
        Assert.True(game.Map.IsWalkable(197, 465));
        game.Tile = (197, 466);
        game.Through("PokemonWorldTournament");

        // The welcome, the offer, the team of three and the bracket: Cheren, Hugh and Clay, each at level 50
        var hall = game.Played.First(p => p.Script.Contains("PokemonWorldTournament"));
        Assert.Contains(hall.Host.Transcript, l => l.Speaker == "Clay" && l.Text.Contains("Welcome to the Pokémon World Tournament"));
        var battles = hall.Host.FrontierBattles;
        Assert.Equal(new[] { "unova_cheren", hugh, "unova_leader_clay" }, battles.Select(b => Assert.Single(b.Foes).Id));
        Assert.All(battles, b => Assert.Equal(3, b.Team.Count));
        Assert.All(battles, b => Assert.False(b.Doubles));
        Assert.All(battles, b => Assert.All(b.Foes[0].Party.Members, p => Assert.Equal(50, p.Level)));
        Assert.All(game.Party.Members, p => Assert.Equal(36, p.Level));
        // the prize, the flag and nothing left of the run
        Assert.Equal(5, hall.Host.Frontier.BattlePoints);
        Assert.Contains(hall.Host.Transcript, l => l.Text.Contains("received 5 BP"));
        Assert.True(game.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));
        Assert.Equal(0, game.Story.Var(BattleSubway.Vars.State));
        Assert.Equal(("PokemonWorldTournament", (11, 11)), (game.Map.Name, game.Tile));
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "hugh");
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "clay");
        Assert.Contains(hall.Host.Transcript, l => l.Speaker == "Clay" && l.Text.Contains("big ship"));

        // The desk afterwards says the tournament has been played
        Assert.Contains(game.TalkTo("receptionist").Transcript, l => l.Text.Contains("has been played"));
        // and coming in again plays nothing
        int played = game.Played.Count;
        game.Arrive("PokemonWorldTournament", 11, 14);
        Assert.Equal(played + 1, game.Played.Count);
        Assert.Empty(game.Played[^1].Host.FrontierBattles);
    }

    [Fact]
    public void ATournamentLostIsStillPlayedAndAnotherCanBeDeclined()
    {
        // knocked out in the second round: no prize, and the story goes on all the same
        var game = AtTheTournament(0);
        game.LoseAt = 2;
        game.Tile = (197, 466);
        game.Through("PokemonWorldTournament");
        var hall = game.Played.First(p => p.Script.Contains("PokemonWorldTournament"));
        Assert.Equal(2, hall.Host.FrontierBattles.Count);
        Assert.Equal(0, hall.Host.Frontier.BattlePoints);
        Assert.Contains(hall.Host.Transcript, l => l.Text.Contains("I beat you"));
        Assert.True(game.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));

        // one who says no isn't entered, and may be asked again by Clay
        var shy = AtTheTournament(0);
        shy.Answers.Add(1);
        shy.Tile = (197, 466);
        shy.Through("PokemonWorldTournament");
        Assert.Empty(shy.Played.First(p => p.Script.Contains("PokemonWorldTournament")).Host.FrontierBattles);
        Assert.False(shy.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));
        Assert.Contains(shy.Map.NPCs, n => n.Key == "clay");
        var again = shy.TalkTo("clay");
        Assert.Equal(3, again.FrontierBattles.Count);
        Assert.True(shy.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));

        // a team too small is turned back, not entered
        var few = new Game();
        few.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 36));
        few.Story.SetVar("VAR_UNOVA_DRIFTVEIL", 5);
        few.Arrive("Unova", 197, 470);
        few.Tile = (197, 466);
        few.Through("PokemonWorldTournament");
        Assert.Contains(few.Played.First(p => p.Script.Contains("PokemonWorldTournament")).Host.Transcript, l => l.Text.Contains("You need 3 Pokémon"));
        Assert.False(few.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));
    }

    [Fact]
    public void TheHallIsClosedUntilClayHasToldOfItAndASaveWhoSawTheFrigateHasPlayedTheTournament()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 30));
        game.Arrive("Unova", 197, 470);
        Assert.False(game.Story.Has("FLAG_UNOVA_PWT_OPEN"));
        Assert.False(game.Map.IsWalkable(197, 465));
        // a save of part c's, past the frigate's scene, is counted past the tournament too
        var old = new Game();
        old.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 36));
        old.Story.SetVar("VAR_UNOVA_DRIFTVEIL", 5);
        old.Story.SetVar("VAR_UNOVA_PWT", 3);
        old.Arrive("Unova", 197, 470);
        Assert.True(old.Story.Has("FLAG_UNOVA_PWT_OPEN"));
        Assert.True(old.Story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT"));
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
        // The Subway's desks turn a team of one back (they run since plan 22 · U6, part d: the Subway's own tests)
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
    public void DriftveilMarketsThreeStallsSellTheirOwnGoods()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("DriftveilCity_Market", 9, 16);
        foreach (var (key, mart, item) in new[] { ("1", "driftveil_market_herbs", "Revival Herb"), ("3", "driftveil_market_goods", "Poké Doll"),
                     ("4", "driftveil_market_sweets", "Old Gateau") })
        {
            var shop = game.Talk(key, "driftveil_city_7");
            Assert.Contains(shop.Log, l => l == $"open Shop {mart}");
            Assert.Contains(MartDatabase.Stock(mart, 0), i => i.Name == item);
        }
        // The others in the hall only talk
        Assert.DoesNotContain(game.Talk("0", "driftveil_city_7").Log, l => l.StartsWith("open Shop"));
    }

    [Fact]
    public void AnvillesCollectorsSwapAndTheGirlsLostPansageIsFoundAndReturned()
    {
        // At the weekend the collector wants a Timburr for his Klink, once; the girl who tried swapping a Tympole for her Dwebble
        var game = new Game { Weekday = DayOfWeek.Sunday };
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Timburr")!, 22));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("AnvilleTown", 20, 52);
        var collector = game.Talk("14", "anville_town");
        Assert.Contains(collector.Log, l => l.StartsWith("trade cogs"));
        var cogs = game.Party.Members[0];
        Assert.Equal(("Klink", "Cogs", 22, "Plus", "Metal Coat"), (cogs.Species.Name, cogs.Nickname, cogs.Level, cogs.AbilityName, cogs.HeldItem?.Name));
        Assert.Equal(Gender.Genderless, cogs.Gender);
        Assert.True(game.Story.Has("FLAG_UNOVA_ANVILLE_COGS_TRADED"));
        Assert.Contains(game.Talk("14", "anville_town").Transcript, l => l.Text.Contains("Cogs is doing well"));
        // a Pokémon that isn't the one asked for is kept
        var wrong = game.Talk("9", "anville_town");
        Assert.Contains(wrong.Transcript, l => l.Text.Contains("not a Tympole"));
        Assert.False(game.Story.Has("FLAG_UNOVA_ANVILLE_SHELLY_TRADED"));
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Tympole")!, 18));
        game.Talk("9", "anville_town");
        Assert.Equal(("Dwebble", "Shelly", Gender.Female), (game.Party.Members[0].Species.Name, game.Party.Members[0].Nickname, game.Party.Members[0].Gender));
        Assert.True(game.Story.Has("FLAG_UNOVA_ANVILLE_SHELLY_TRADED"));

        // The Pansage: nowhere to be seen until the girl tells of it, found in the plaza, handed back for a Miracle Seed
        Assert.Null(game.Present("15", "anville_town"));
        Assert.Null(game.Present("pansage_home", "anville_town"));
        game.Talk("16", "anville_town");
        Assert.True(game.Story.Has("FLAG_UNOVA_ANVILLE_PANSAGE_LOST"));
        var pansage = game.Present("15", "anville_town")!;
        Assert.Equal((20, 54), (pansage.GridX, pansage.GridY));
        Assert.True(game.Map.IsWalkable(20, 53) || game.Map.IsWalkable(21, 54));
        game.Talk("16", "anville_town");
        Assert.False(game.Story.Has("FLAG_UNOVA_ANVILLE_PANSAGE_FOUND"));
        var found = game.Talk("15", "anville_town");
        Assert.Contains(found.Log, l => l.StartsWith("cry Pansage"));
        Assert.True(game.Story.Has("FLAG_UNOVA_ANVILLE_PANSAGE_FOUND"));
        Assert.Null(game.Present("15", "anville_town"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Miracle Seed")!));
        game.Talk("16", "anville_town");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Miracle Seed")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_ANVILLE_PANSAGE_THANKED"));
        Assert.NotNull(game.Present("pansage_home", "anville_town"));
        game.Talk("16", "anville_town");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Miracle Seed")!));
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

    // ------------------------------------------------------------------ the Battle Subway (plan 22 · U6, part d)

    private static Game Rider(DayOfWeek day = DayOfWeek.Monday)
    {
        var game = new Game { Weekday = day };
        foreach (string species in new[] { "Snivy", "Lillipup", "Pidove", "Roggenrola", "Purrloin", "Patrat" })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 26));
        game.Arrive("GearStation", 11, 4);
        return game;
    }

    private static HeadlessScriptHost Car(Game game) => game.Played.First(p => p.Script.Contains("BattleSubwayTrain")).Host;

    [Theory]
    [InlineData("single_desk", 0, 3, false)]
    [InlineData("double_desk", 1, 4, true)]
    [InlineData("multi_desk", 2, 2, true)]
    public void EverySubwayDeskOpensARunWithItsFormat(string desk, int train, int size, bool doubles)
    {
        var game = Rider();
        // RIDE; the first set of seven cleared and the second answer of "get off here, or go on" being the first: get off
        var desks = game.TalkTo(desk);
        Assert.Contains(desks.Log, l => l.StartsWith("warp BattleSubwayTrain"));
        var car = Car(game);

        // Seven cars, each with the Pokémon entered as copies and the train's own number of them against a trainer's
        Assert.Equal(7, car.FrontierBattles.Count);
        Assert.All(car.FrontierBattles, b => Assert.Equal(size, b.Team.Count));
        Assert.All(car.FrontierBattles, b => Assert.Equal(doubles, b.Doubles));
        Assert.All(car.FrontierBattles, b => Assert.All(b.Team.Members, p => Assert.True(p.Level <= 50)));
        if (train == 0 || train == 1)
            Assert.All(car.FrontierBattles, b => Assert.Equal(size, Assert.Single(b.Foes).Party.Count));
        else
        {
            // a pair against a pair, the player's partner of the Subway's own staff beside them
            Assert.All(car.FrontierBattles, b => Assert.Equal(new[] { 2, 2 }, b.Foes.Select(f => f.Party.Count)));
            Assert.All(car.FrontierBattles, b => Assert.Equal(2, b.Partner!.Party.Count));
            Assert.Single(car.FrontierBattles.Select(b => b.Partner!.Id).Distinct());
            Assert.Single(car.FrontierBattles.Select(b => string.Join(",", b.Partner!.Party.Members.Select(p => p.Species.Name))).Distinct());
        }
        // The Pokémon themselves are never fought with
        Assert.All(game.Party.Members, p => Assert.Equal(26, p.Level));

        // Getting off after a set of seven: its Battle Points, the best streak of the train, back in the Gear Station
        Assert.Equal(3, car.Frontier.BattlePoints);
        Assert.Contains(car.Transcript, l => l.Text.Contains("received 3 BP"));
        Assert.Equal(7, game.Story.Var(BattleSubway.Vars.Best((SubwayTrain)train)));
        Assert.Equal(0, game.Story.Var(BattleSubway.Vars.State));
        Assert.Equal("GearStation", game.Map.Name);
        Assert.Equal((11, 4), game.Tile);
    }

    [Fact]
    public void ADeskTurnsBackATeamTooSmallAndRefusesTwoOfAKind()
    {
        // one Pokémon: three are needed
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 26));
        game.Arrive("GearStation", 11, 4);
        var few = game.TalkTo("single_desk");
        Assert.Contains(few.Transcript, l => l.Text.Contains("You need 3 Pokémon"));
        Assert.DoesNotContain(few.Log, l => l.StartsWith("warp "));
        Assert.Equal(0, game.Story.Var(BattleSubway.Vars.State));

        // an Egg or a banned species can't make up the number
        var banned = new Game();
        foreach (string species in new[] { "Snivy", "Lillipup", "Mewtwo" }) banned.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 40));
        banned.Arrive("GearStation", 11, 4);
        Assert.Contains(banned.TalkTo("single_desk").Transcript, l => l.Text.Contains("You need 3 Pokémon"));

        // two of a species, entered by hand, are refused
        var twins = new Game();
        foreach (string species in new[] { "Snivy", "Lillipup", "Pidove", "Lillipup" }) twins.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 26));
        twins.Arrive("GearStation", 11, 4);
        var desk = twins.Map.NPCs.First(n => n.Key == "single_desk");
        twins.Tile = (desk.GridX, desk.GridY + 1);
        var host = new HeadlessScriptHost(twins.Story, twins.Party, twins.Bag) { Map = twins.Map, PlayerTile = twins.Tile, NeedsPokemon = true };
        host.Entering.Enqueue(new[] { 1, 3, 0 });
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("SingleTrain", "GearStation")!, desk);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, l => l.Text.Contains("different species"));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp "));
        Assert.Equal(0, twins.Story.Var(BattleSubway.Vars.State));
    }

    [Fact]
    public void ABattleLostEndsTheRunWithTheStreakAndThePointsOfTheSetsCleared()
    {
        var game = Rider();
        game.LoseAt = 4;
        game.TalkTo("single_desk");
        var car = Car(game);
        Assert.Equal(4, car.FrontierBattles.Count);
        Assert.Equal(0, car.Frontier.BattlePoints);
        Assert.Contains(car.Transcript, l => l.Text.Contains("cleared 3 cars"));
        Assert.Equal(3, game.Story.Var(BattleSubway.Vars.Best(SubwayTrain.Single)));
        Assert.Equal(0, game.Story.Var(BattleSubway.Vars.State));
        Assert.Equal("GearStation", game.Map.Name);

        // The second set lost: the first set's points are paid, and a longer streak replaces the shorter
        var next = Rider();
        next.Answers.AddRange(new[] { 0, 1 });
        next.LoseAt = 10;
        next.TalkTo("single_desk");
        var second = Car(next);
        Assert.Equal(10, second.FrontierBattles.Count);
        Assert.Equal(3, second.Frontier.BattlePoints);
        Assert.Equal(9, next.Story.Var(BattleSubway.Vars.Best(SubwayTrain.Single)));
    }

    [Fact]
    public void IngoWaitsAtTheTwentyFirstCarAndEmmetAtTheFortyNinth()
    {
        // GO ON after the first and the second set, GET OFF after the third: the 21st battle is Ingo's, and 3 + 4 + 5 points
        var game = Rider();
        game.Answers.AddRange(new[] { 0, 1, 1, 0 });
        game.TalkTo("single_desk");
        var car = Car(game);
        Assert.Equal(21, car.FrontierBattles.Count);
        Assert.Equal("subway_ingo", Assert.Single(car.FrontierBattles[20].Foes).Id);
        Assert.Equal(new[] { "Excadrill", "Conkeldurr", "Archeops" }, car.FrontierBattles[20].Foes[0].Party.Members.Select(p => p.Species.Name));
        Assert.All(car.FrontierBattles.Take(20), b => Assert.StartsWith("frontier_", Assert.Single(b.Foes).Id));
        Assert.Equal(12, car.Frontier.BattlePoints);
        Assert.Equal(21, game.Story.Var(BattleSubway.Vars.Best(SubwayTrain.Single)));

        // and on to the 49th, which is Emmet's, in the Double Train with the pair of them side by side in the Multi Train
        var far = Rider();
        far.Answers.AddRange(new[] { 0 }.Concat(Enumerable.Repeat(1, 6)).Concat(new[] { 0 }));
        far.TalkTo("double_desk");
        var carD = Car(far);
        Assert.Equal(49, carD.FrontierBattles.Count);
        Assert.Equal("subway_emmet", Assert.Single(carD.FrontierBattles[48].Foes).Id);
        Assert.True(carD.FrontierBattles[48].Doubles);
        Assert.Equal((49, 42), (far.Story.Var(BattleSubway.Vars.Best(SubwayTrain.Double)), carD.Frontier.BattlePoints));

        var multi = Rider();
        multi.Answers.AddRange(new[] { 0, 1, 1, 0 });
        multi.TalkTo("multi_desk");
        var pair = Car(multi).FrontierBattles[20];
        Assert.Equal(new[] { "subway_ingo", "subway_emmet" }, pair.Foes.Select(f => f.Id));
        Assert.All(pair.Foes, f => Assert.Equal(2, f.Party.Count));
        Assert.NotNull(pair.Partner);
    }

    [Fact]
    public void TheSubwaysRulesAreTheFrontiersOnATrain()
    {
        Assert.Equal(new[] { 3, 4, 2 }, new[] { SubwayTrain.Single, SubwayTrain.Double, SubwayTrain.Multi }.Select(BattleSubway.PartySize));
        Assert.Equal(new[] { 0, 0, 1, 0, 2, 0 }, new[] { 1, 20, 21, 22, 49, 50 }.Select(BattleSubway.BossAt));
        // Three sets of 3, 4 and 5 points, and none for a set not finished
        Assert.Equal(new[] { 0, 0, 3, 3, 7, 12 }, new[] { 0, 6, 7, 13, 14, 21 }.Select(BattleSubway.PointsFor));
        Assert.Equal(82, BattleSubway.PointsFor(70 + 7));
        // A battle's trainer is of its room's row, whatever the draw
        var rng = new Random(3);
        for (int battle = 1; battle <= 70; battle++)
        {
            int number = BattleSubway.TrainerFor(battle, () => rng.Next(0x10000));
            int room = (battle - 1) / 7, place = (battle - 1) % 7;
            var row = room >= 7 ? FrontierDatabase.TowerRooms[7] : place == 6 ? FrontierDatabase.TowerBossRooms[room] : FrontierDatabase.TowerRooms[room];
            Assert.InRange(number, row[0], row[1]);
        }
        // Their teams hold the train's number of Pokémon, at level 50, none of a species twice
        foreach (var train in new[] { SubwayTrain.Single, SubwayTrain.Double, SubwayTrain.Multi })
        {
            var foes = BattleSubway.MakeOpponents(train, 9, () => rng.Next(0x10000));
            Assert.Equal(train == SubwayTrain.Multi ? 2 : 1, foes.Count);
            Assert.All(foes, f => Assert.Equal(BattleSubway.PartySize(train), f.Pokemon.Count));
            Assert.All(foes, f => Assert.All(f.Pokemon, p => Assert.Equal(50, p.Level)));
            Assert.All(foes, f => Assert.Equal(f.Pokemon.Count, f.Pokemon.Select(p => p.Species.Name).Distinct().Count()));
        }
        // The partner is the same two Pokémon for a seed and another pair for another
        var partner = BattleSubway.MakePartner(77);
        Assert.Equal(partner.Pokemon.Select(p => p.Species.Name), BattleSubway.MakePartner(77).Pokemon.Select(p => p.Species.Name));
        Assert.Equal(2, partner.Pokemon.Count);
        // The bosses are of our own data, level 50 and fit for a double or a pair
        foreach (var train in new[] { SubwayTrain.Single, SubwayTrain.Multi })
        {
            Assert.All(new[] { 1, 2 }, which => Assert.All(BattleSubway.Boss(which, train).Party.Members, p => Assert.Equal(50, p.Level)));
            Assert.Equal(train == SubwayTrain.Multi ? 2 : 3, BattleSubway.Boss(1, train).Party.Count);
        }
        Assert.Equal(new[] { "Subway Boss Ingo", "Subway Boss Emmet" }, BattleSubway.Bosses(SubwayTrain.Multi, 21).Select(b => b.TrainerClass));
        Assert.Single(BattleSubway.Bosses(SubwayTrain.Single, 49));
        Assert.Empty(BattleSubway.Bosses(SubwayTrain.Single, 8));
    }

    [Fact]
    public void ARunBeingFoughtKeepsItsStateInTheStoryAndTheCarComesBackToIt()
    {
        // A game saved in the car comes back to the car: the run is the story's own variables
        var game = Rider();
        game.Story.SetVar(BattleSubway.Vars.State, 1);
        game.Story.SetVar(BattleSubway.Vars.Train, (int)SubwayTrain.Single);
        game.Story.SetVar(BattleSubway.Vars.Wins, 4);
        BattleSubway.KeepSlots(game.Story, new[] { 0, 2, 3 });
        game.Arrive("BattleSubwayTrain", 3, 5);
        var car = game.Played.Last().Host;
        // three more cars to the end of the set, the fifth battle first
        Assert.Equal(3, car.FrontierBattles.Count);
        Assert.Equal(new[] { "Snivy", "Pidove", "Roggenrola" }, car.FrontierBattles[0].Team.Members.Select(p => p.Species.Name));
        Assert.Contains(car.Transcript, l => l.Text.Contains("Car 5"));
        // a player who wanders in with no run under way is simply left in the car
        var idle = new Game();
        idle.Arrive("BattleSubwayTrain", 3, 5);
        Assert.Empty(idle.Played.Last().Host.FrontierBattles);
        Assert.False(idle.Map.NPCs.Any(n => n.Key is "opponent" or "ingo" or "emmet" or "partner"));
    }

    // ---- Plan 22 · U7, part a: Route 6 to Lentimas Town ------------------------------------------------------------------

    /// <summary>A game past the Plasma Frigate's chapter, a team of six.</summary>
    private static Game PastTheFrigate()
    {
        var game = new Game();
        foreach (string species in new[] { "Emolga", "Snivy", "Lillipup", "Pidove", "Roggenrola", "Purrloin" })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 30));
        game.Story.SetVar("VAR_UNOVA_PWT", 3);
        game.Story.Set("FLAG_UNOVA_FRIGATE_SAILED");
        return game;
    }

    [Fact]
    public void RouteSixCherenTakesThePlayerToTheLabAndGivesSurf()
    {
        var game = PastTheFrigate();
        game.Arrive("Unova", 138, 372);
        // As the route is come to once the frigate has sailed, Cheren stands on the path
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_ROUTE_6"));
        Assert.NotNull(game.Present("8", "unova_route_6"));
        Assert.True(game.Fires("CherenCalls"));

        game.Step("CherenCalls");
        Assert.Equal("Route6_SeasonLab", game.Map.Name);
        // In the lab his scene plays by its own arrival: the cold snap told, HM03 given, the count moved on
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_ROUTE_6"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM03")!));
        Assert.Contains(game.Played.SelectMany(p => p.Host.Transcript), l => l.Speaker == "Cheren" && l.Text.Contains("Lacunosa"));

        // The researcher's Deerling, once
        game.Party.Members.RemoveAt(5);
        game.Answers.Add(0);
        game.TalkTo("8");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_DEERLING"));
        Assert.Equal("Deerling", game.Party.Members.Last().Species.Name);
    }

    [Fact]
    public void CobalionCrossesTheWestBridgeOnceTheLabHasBeenVisited()
    {
        var game = PastTheFrigate();
        game.Story.SetVar("VAR_UNOVA_ROUTE_6", 2);
        game.Arrive("Unova", 103, 366);
        Assert.True(game.Fires("CobalionPasses"));
        game.Step("CobalionPasses");
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_ROUTE_6"));
        Assert.False(game.Fires("CobalionPasses"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_COBALION_ROUTE_6"));
    }

    [Fact]
    public void BiancaAndNMeetThePlayerInChargestoneCave()
    {
        var game = PastTheFrigate();
        game.Arrive("ChargestoneCave", 11, 49);
        game.Step("BiancaAtTheMouth");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_CHARGESTONE"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_CHARGESTONE"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_CHARGESTONE_HEART"));
        game.Step("NAlone");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_CHARGESTONE"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_N_CHARGESTONE"));

        // On the floor beneath the two men each give a Nugget, once
        game.Arrive("ChargestoneCave_Heart", 5, 5);
        game.TalkTo("2");
        game.TalkTo("2");
        game.TalkTo("1");
        Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Nugget")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_NUGGET_MAN") && game.Story.Has("FLAG_UNOVA_GOT_NUGGET_BOY"));
    }

    [Fact]
    public void JuniperAndSkylaMeetThePlayerInMistraltonCityAndTheGymGivesTheJetBadge()
    {
        var game = PastTheFrigate();
        game.Arrive("Unova", 102, 301);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_MISTRALTON"));
        Assert.True(game.Fires("JuniperAndSkyla"));
        game.Step("JuniperAndSkyla");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_MISTRALTON"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Master Ball")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_MASTER_BALL"));
        // Juniper goes on to the tower and Skyla to her Gym
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_JUNIPER_TOWER"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_SKYLA_GYM"));
        Assert.False(game.Fires("JuniperAndSkyla"));

        // Skyla's Gym: the guide's water, the battle with Skyla's own team, the Jet Badge and TM62
        game.Arrive("MistraltonCity_Gym", 8, 30);
        game.TalkTo("5");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_WATER_MISTRALTON"));
        var skyla = game.TalkTo("3");
        Assert.Contains(skyla.Log, l => l.StartsWith("battle") && l.Contains("unova_leader_skyla"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Jet"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM62")!));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_MISTRALTON"));
        Assert.NotEmpty(TrainerDatabase.Get("unova_leader_skyla")!.Party);
        // A defeated Skyla only talks
        var again = game.TalkTo("3");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
    }

    [Fact]
    public void JuniperLeavesTheTowerForTheHallOnceTheBadgeIsWonAndTheFlightEndsInLentimasTown()
    {
        var game = PastTheFrigate();
        game.Arrive("Unova", 102, 301);
        game.Step("JuniperAndSkyla");
        // Before the Badge the professor gives a Lucky Egg and stays
        game.Arrive("CelestialTower_1F", 14, 20);
        game.TalkTo("0");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Lucky Egg")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_MISTRALTON"));

        // With the Jet Badge she goes back to the Cargo Service's hall
        game.Story.GiveBadge(RegionDatabase.Unova, "Jet");
        game.TalkTo("0");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Lucky Egg")!));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_MISTRALTON"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_JUNIPER_TOWER"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_JUNIPER_HALL"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_SKYLA_HALL"));

        // In the hall Skyla asks and the plane takes off: it lands in Lentimas Town, where Juniper tells her news
        game.Arrive("MistraltonCity_CargoHall", 8, 9);
        game.Answers.Add(0);
        var flight = game.TalkTo("4");
        Assert.Contains(flight.Log, l => l.StartsWith("warp Unova 616 308"));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_MISTRALTON"));
        Assert.Equal("Unova", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_LENTIMAS"));
        Assert.Contains(game.Played.Select(p => p.Host).SelectMany(h => h.Transcript), l => l.Speaker == "Prof. Juniper" && l.Text.Contains("Drayden"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_LENTIMAS"));
        Assert.False(game.Story.Has("FLAG_UNOVA_HIDE_JUNIPER_LENTIMAS"));
        Assert.NotNull(game.Present("2", "lentimas_town"));
        Assert.Null(game.Present("5", "lentimas_town"));
        // The scene is played once
        game.Arrive("Unova", 616, 308);
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_LENTIMAS"));
    }

    [Fact]
    public void LentimasTownsAttendantFliesThePlayerBackToMistraltonCity()
    {
        var game = PastTheFrigate();
        game.Story.SetVar("VAR_UNOVA_MISTRALTON", 4);
        game.Story.SetVar("VAR_UNOVA_LENTIMAS", 1);
        game.Arrive("Unova", 617, 304);
        game.Answers.Add(0);
        var back = game.Talk("7", "lentimas_town");
        Assert.Contains(back.Log, l => l.StartsWith("warp Unova 108 305"));
        game.Answers.Add(1);
        var no = game.Talk("7", "lentimas_town");
        Assert.DoesNotContain(no.Log, l => l.StartsWith("warp"));
    }

    [Fact]
    public void RouteSevensHikerTradesAGigalithForAnEmolgaAndTheOldManTellsOfLandorus()
    {
        var game = PastTheFrigate();
        game.Arrive("Route7_House1", 3, 5);
        game.Answers.Add(0);
        game.TalkTo("0");
        Assert.True(game.Story.Has("FLAG_UNOVA_TRADED_GIGALITH"));
        Assert.Equal("Gigalith", game.Party.Members[0].Species.Name);
        Assert.DoesNotContain(game.Party.Members.Skip(1), p => p.Species.Name == "Emolga");

        game.Arrive("Route7_House2", 3, 5);
        game.TalkTo("1");
        Assert.True(game.Story.Has("FLAG_UNOVA_LANDORUS_TALE"));
    }

    [Fact]
    public void TheCelestialTowersFloorsAreJoinedByTheirSpiralOfRamps()
    {
        var game = PastTheFrigate();
        var floors = new[] { "CelestialTower_1F", "CelestialTower_2F", "CelestialTower_3F", "CelestialTower_4F", "CelestialTower_Summit" };
        for (int i = 0; i < floors.Length; i++)
        {
            game.Arrive(floors[i], 10, 10);
            if (i < floors.Length - 1) Assert.Contains(game.Map.Warps, w => w.TargetMap == floors[i + 1]);
            if (i > 0) Assert.Contains(game.Map.Warps, w => w.TargetMap == floors[i - 1]);
        }
    }

    [Fact]
    public void MistraltonsAndTheTowersSongsAreOurOwn()
    {
        foreach (string song in new[] { "route6", "chargestone", "mistralton", "celestial_tower" })
            Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", song + ".mml"))), song);
    }

    [Fact]
    public void TheKidsAtTheRunwaysEdgeGiveTheirTmOnceAfterTheTreasureIsFound()
    {
        var game = PastTheFrigate();
        game.Arrive("Unova", 75, 329);
        game.Story.Unset("FLAG_UNOVA_HIDE_RUNWAY_TREASURE");
        game.Map.ApplyPresence(game.Story.Has);
        game.Talk("0", "mistralton_city");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_TM40"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM40")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_RUNWAY_KIDS"));
    }
}
