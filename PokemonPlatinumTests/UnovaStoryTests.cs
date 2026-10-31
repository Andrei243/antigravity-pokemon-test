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
        public Map Mapped(string name) => maps[name];
        public (int X, int Y) Tile { get; set; }
        public int Starter { get; init; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public string? BeganIn { get; init; }
        /// <summary>Whom the player plays as (plan 22 · U6: Nimbasa's young Trainer is the hero the player isn't).</summary>
        public PlayerLook Look { get; set; } = PlayerLook.Boy;
        /// <summary>The day of the week the game's scripts see (plan 22 · U6, part c: Anville Town's weekend crowd).</summary>
        public DayOfWeek Weekday { get; set; } = DayOfWeek.Monday;
        /// <summary>The time of day the game's scripts see (plan 22 · U10, part b: the Underground Ruins' door).</summary>
        public TimeOfDay Time { get; set; } = TimeOfDay.Morning;
        /// <summary>The answers the next script played is given, in the order it asks (plan 22 · U6, part d: the Subway's menus).</summary>
        public List<int> Answers { get; } = new();
        /// <summary>The battle of a script (counting from 1) that is lost, where one is (plan 22 · U6, part d: a Subway run ended early).</summary>
        public int LoseAt { get; set; }
        /// <summary>Who travels with the player, as which trainer (plan 22 · U7, part b: Bianca through Reversal Mountain): kept from one script to the next.</summary>
        public string? Along { get; set; }
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
            // and the Marine Tube, Black 2's walk on rails under the sea (plan 22 · U8, part b)
            maps["MarineTube"] = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "MarineTube.json")).ToMap();
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
                Weekday = Weekday,
                TimeOfDay = Time
            };
            if (Along != null) host.TravelWith(Map.FindPerson("bianca_along") ?? Map.Everyone.First(), Along);
            foreach (int answer in Answers) host.Answers.Enqueue(answer);
            Answers.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Along = host.Partner;
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

    // ---- Plan 22 · U7, part b: Lentimas Town to Lacunosa Town ---------------------------------------------------------------

    /// <summary>A game that has landed in Lentimas Town: Bianca has gone ahead to Reversal Mountain.</summary>
    private static Game AtLentimas()
    {
        var game = PastTheFrigate();
        game.Story.SetVar("VAR_UNOVA_MISTRALTON", 4);
        game.Story.SetVar("VAR_UNOVA_LENTIMAS", 1);
        game.Story.Unset("FLAG_UNOVA_HIDE_BIANCA_REVERSAL");
        return game;
    }

    [Fact]
    public void BiancaTravelsThroughReversalMountainAsThePartnerAndPartsAtTheEastEnd()
    {
        var game = AtLentimas();
        game.Arrive("ReversalMountain_3", 5, 9);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_REVERSAL"));
        Assert.NotNull(game.Present("13", "reversal_mountain_4"));
        // Nothing of her errand happens without her
        Assert.False(game.Fires("BiancaOffersToPart"));

        // She asks the player along, heals the team and travels with them as the partner of Black 2's Bianca's team
        var asks = game.TalkTo("13");
        Assert.Contains(asks.Log, l => l == "partner Bianca unova_bianca");
        Assert.Equal("unova_bianca", game.Along);
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_REVERSAL"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_REVERSAL"));
        Assert.NotNull(TrainerDatabase.Get("unova_bianca"));
        Assert.True(game.Fires("BiancaOffersToPart"));

        // At the passage she stops, and in the lair beyond it she wonders at Heatran's place
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_REVERSAL_LAIR"));
        game.Step("BiancaStopsAtThePassage");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_REVERSAL_LAIR"));
        Assert.False(game.Fires("BiancaStopsAtThePassage"));
        game.Arrive("ReversalMountain_4", 10, 24);
        var lair = game.Step("BiancaInTheLair");
        Assert.Contains(lair.Transcript, l => l.Speaker == "Bianca" && l.Text.Contains("Heatran"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_REVERSAL_LAIR"));
        // Heatran itself waits for the post-game
        Assert.Null(game.Present("0", "reversal_mountain_5"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HEATRAN"));

        // At the east end she says Undella Town is near and offers to part: refused, she stays; accepted, she goes on her own way
        game.Arrive("ReversalMountain_6", 22, 5);
        game.Answers.Add(1);
        var refused = game.Step("BiancaSaysGoodbye");
        Assert.Contains(refused.Transcript, l => l.Speaker == "Bianca" && l.Text.Contains("look around a little more"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_REVERSAL"));
        Assert.Equal("unova_bianca", game.Along);
        Assert.Null(game.Present("0", "reversal_mountain_7"));
        game.Answers.Add(0);
        var parted = game.Step("BiancaSaysGoodbye");
        Assert.Contains(parted.Log, l => l == "partner off");
        Assert.Null(game.Along);
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_REVERSAL"));
        Assert.False(game.Fires("BiancaSaysGoodbye"));
        // and she stands in the tunnel, talking about the Magma Stone
        Assert.NotNull(game.Present("0", "reversal_mountain_7"));
        var talk = game.TalkTo("0");
        Assert.Contains(talk.Transcript, l => l.Speaker == "Bianca" && l.Text.Contains("Magma Stone"));
    }

    [Fact]
    public void BiancaOffersToPartAtEveryWayOutOfTheCaveAndSheWaitsAgainAfterAGameLost()
    {
        // The first floor's way out to the slopes and the lower passage's two: each asks, and each takes her off
        foreach (var (map, x, y, trigger) in new[] { ("ReversalMountain_1", 25, 26, "BiancaOffersToPart"), ("ReversalMountain_5", 12, 7, "BiancaOffersToPart"),
                     ("ReversalMountain_5", 8, 14, "BiancaOffersToPartBelow"), ("ReversalMountain_3", 5, 9, "BiancaOffersToPart") })
        {
            var game = AtLentimas();
            game.Story.SetVar("VAR_UNOVA_REVERSAL", 1);
            game.Along = "unova_bianca";
            game.Arrive(map, x, y);
            Assert.True(game.Fires(trigger), $"{map} {trigger}");
            game.Answers.Add(0);
            game.Step(trigger);
            Assert.Equal(2, game.Story.Var("VAR_UNOVA_REVERSAL"));
            Assert.Null(game.Along);
        }

        // A game lost with her along (nobody travels with the player any more): she is at the west end again
        var lost = AtLentimas();
        lost.Story.SetVar("VAR_UNOVA_REVERSAL", 1);
        lost.Story.SetVar("VAR_UNOVA_REVERSAL_LAIR", 2);
        lost.Arrive("ReversalMountain_3", 5, 9);
        Assert.Equal(0, lost.Story.Var("VAR_UNOVA_REVERSAL"));
        Assert.Equal(0, lost.Story.Var("VAR_UNOVA_REVERSAL_LAIR"));
        Assert.NotNull(lost.Present("13", "reversal_mountain_4"));
    }

    [Fact]
    public void EveryFloorOfReversalMountainHasBiancaThePartnerToTakeOverAsTheyWalkThrough()
    {
        // The partner follows only on the map where she is a person (GameEngine.KeepPartnerAlong): every floor of the cave has its own,
        // with one key, hidden until she travels (FLAG_UNOVA_HIDE_BIANCA_ALONG) and in no other place of the story
        var game = AtLentimas();
        foreach (string floor in new[] { "ReversalMountain_1", "ReversalMountain_2", "ReversalMountain_3", "ReversalMountain_4", "ReversalMountain_5", "ReversalMountain_6" })
        {
            game.Arrive(floor, 10, 10);
            var twin = Assert.Single(game.Map.Everyone, n => n.Key == "bianca_along");
            Assert.Equal("FLAG_UNOVA_HIDE_BIANCA_ALONG", twin.HiddenBy);
            Assert.Null(game.Present("bianca_along", game.Map.Everyone.First(n => n.Key == "bianca_along").ScriptFile ?? ""));
        }
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_ALONG"));
    }

    [Fact]
    public void ThePartnerMovesToTheNextFloorsOwnBianca()
    {
        // Follower.Rehome: the same follower, now the person of the next floor
        var first = new NPC { Name = "Bianca", Key = "bianca_along", GridX = 5, GridY = 5 };
        var second = new NPC { Name = "Bianca", Key = "bianca_along", GridX = 20, GridY = 20 };
        var follower = new Follower(first, "unova_bianca");
        follower.PlayerLeft(6, 5);
        follower.Rehome(second);
        Assert.Same(second, follower.Who);
        Assert.Equal("unova_bianca", follower.TrainerId);
        follower.Behind(10, 10, Direction.Right);
        Assert.Equal((9, 10), (second.GridX, second.GridY));
        Assert.Equal((5, 5), (first.GridX, first.GridY));
        Assert.False(follower.Moving);
    }

    [Fact]
    public void TheStrangeHouseGirlWalksThroughTwiceAndTheAtticGivesTheLunarWing()
    {
        var game = AtLentimas();
        game.Arrive("StrangeHouse_Rooms", 30, 20);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_STRANGE"));
        Assert.True(game.Fires("TheGirlWalksThrough"));
        var first = game.Step("TheGirlWalksThrough");
        Assert.Contains(first.Transcript, l => l.Speaker == "Girl" && l.Text.Contains("Abra"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_STRANGE"));
        Assert.Null(game.Present("0", "strange_house_3"));
        Assert.False(game.Fires("TheGirlWalksThrough"));

        game.Arrive("StrangeHouse_Cellar", 30, 20);
        var second = game.Step("TheGirlComesBack");
        Assert.Contains(second.Transcript, l => l.Speaker == "Girl");
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_STRANGE"));

        // The attic's feather, once; the libraries' spell tag and dusk stone are items of the floor
        game.Arrive("StrangeHouse_Attic", 6, 12);
        game.TalkTo("0");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Lunar Wing")!));
        Assert.Null(game.Present("0", "strange_house_5"));
        Assert.Contains(game.Mapped("StrangeHouse_Library1").NPCs, n => n.IsItemBall && n.Item == "Spell Tag");
        Assert.Contains(game.Mapped("StrangeHouse_Library1").NPCs, n => n.IsItemBall && n.Item == "Dusk Stone");
        // the house is one way through: hall, library, rooms, library, cellar, attic
        var path = new[] { ("StrangeHouse_Hall", "StrangeHouse_Library1"), ("StrangeHouse_Library1", "StrangeHouse_Rooms"), ("StrangeHouse_Rooms", "StrangeHouse_Library2"),
            ("StrangeHouse_Library2", "StrangeHouse_Cellar"), ("StrangeHouse_Cellar", "StrangeHouse_Attic") };
        foreach (var (from, to) in path) Assert.Contains(game.Mapped(from).Warps, w => w.TargetMap == to);
    }

    [Theory]
    [InlineData(0, "unova_rival_10")]
    [InlineData(1, "unova_rival_11")]
    [InlineData(2, "unova_rival_12")]
    public void HughBattlesAtUndellaTownsEastEndWithTheTeamStrongAgainstTheStarter(int starter, string hugh)
    {
        var game = new Game(starter);
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Unova)!.Starters[starter]);
        foreach (string species in new[] { "Emolga", "Lillipup", "Pidove", "Roggenrola", "Purrloin" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 36));
        game.Arrive("Unova", 745, 299);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_UNDELLA"));
        Assert.True(game.Fires("HughAtTheEast"));
        Assert.Null(game.Present("hugh_undella", "undella_town"));
        var scene = game.Step("HughAtTheEast");
        Assert.Contains(scene.Log, l => l.StartsWith($"battle {hugh}"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_UNDELLA"));
        Assert.False(game.Fires("HughAtTheEast"));
        Assert.Null(game.Present("hugh_undella", "undella_town"));
        // Black 2's later scenes here wait for the post-game
        Assert.Null(game.Present("6", "undella_town"));
        Assert.Null(game.Present("7", "undella_town"));
        Assert.NotNull(TrainerDatabase.Get(hugh));
    }

    [Fact]
    public void LacunosaTownTellsItsTaleAndHughAndTheStarterFightZinzolinAndAGrunt()
    {
        var game = AtLentimas();
        game.Story.SetVar("VAR_UNOVA_REVERSAL", 2);
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Unova)!.Starters[0]);
        game.Arrive("Unova", 670, 183);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_LACUNOSA"));
        Assert.True(game.Fires("JuniperAndBiancaMeet"));
        Assert.Null(game.Present("12", "lacunosa_town"));
        // Juniper and Bianca meet the player and take them to the old woman's house, where the tale is told as the room is come to
        var meet = game.Step("JuniperAndBiancaMeet");
        Assert.Contains(meet.Transcript, l => l.Speaker == "Bianca" && l.Text.Contains("Fly"));
        Assert.Equal("LacunosaTown_House2", game.Map.Name);
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_LACUNOSA"));
        var tale = game.Played.Last(p => p.Script.EndsWith("OnEnter") && p.Script.Contains("lacunosa_town_3")).Host;
        Assert.Contains(tale.Transcript, l => l.Speaker == "Storyteller" && l.Text.Contains("meteorite"));
        Assert.Contains(tale.Transcript, l => l.Speaker == "Prof. Juniper" && l.Text.Contains("Drayden"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_JUNIPER_STORYTELLER") && game.Story.Has("FLAG_UNOVA_HIDE_BIANCA_STORYTELLER"));
        // The old woman tells it again to anyone who asks
        game.Answers.Add(0);
        var again = game.TalkTo("0");
        Assert.Contains(again.Transcript, l => l.Text.Contains("great, great hole"));

        // Hugh waits at the west end; across the lane below him Zinzolin and a grunt are come upon, and the tag battle is offered
        game.Through("Unova");
        Assert.NotNull(game.Present("12", "lacunosa_town"));
        Assert.True(game.Fires("HughAndTheGrunts"));
        game.Answers.Add(1);
        var turned = game.Step("HughAndTheGrunts");
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_LACUNOSA"));
        Assert.DoesNotContain(turned.Log, l => l.StartsWith("battle"));
        Assert.NotNull(game.Present("10", "lacunosa_town"));
        Assert.False(game.Fires("HughAndTheGrunts"));

        // Hugh's word offers it again; with the starter's team beside the player the two are beaten and run
        game.Answers.Add(0);
        var fight = game.Talk("12", "lacunosa_town");
        Assert.Contains(fight.Log, l => l == "partner {rival} unova_rival_25");
        Assert.Contains(fight.Log, l => l.StartsWith("battle unova_team_plasma_zinzolin_4 and unova_team_plasma_grunt_45 with unova_rival_25"));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_LACUNOSA"));
        Assert.Null(game.Along);
        Assert.Null(game.Present("10", "lacunosa_town"));
        Assert.Null(game.Present("11", "lacunosa_town"));
        Assert.Null(game.Present("12", "lacunosa_town"));
    }

    [Fact]
    public void LacunosasPeopleKeepIndoorsAfterDarkAndThePokemonCenterIsASpawn()
    {
        var spawns = RegionDatabase.Get(RegionDatabase.Unova)!.File!.Spawns;
        Assert.Contains(spawns, s => s.Room == "UndellaTown_PokemonCenter");
        Assert.Contains(spawns, s => s.Room == "LacunosaTown_PokemonCenter");
        Assert.Equal(spawns.Count, spawns.Select(s => s.Id).Distinct().Count());
        // the street's people are the ones the night flag hides
        var game = AtLentimas();
        game.Arrive("Unova", 660, 186);
        var street = game.Map.Everyone.Where(n => n.ScriptFile == "lacunosa_town" && n.HiddenBy == "FLAG_UNOVA_LACUNOSA_NIGHT").ToList();
        Assert.True(street.Count >= 8);
        game.Story.Set("FLAG_UNOVA_LACUNOSA_NIGHT");
        game.Map.ApplyPresence(game.Story.Has);
        Assert.All(street, n => Assert.DoesNotContain(n, game.Map.NPCs));
    }

    [Fact]
    public void RouteThirteensTreasureHunterAndUndellaBaysSwimmersAreThereAndTheSongsAreOurOwn()
    {
        var game = AtLentimas();
        game.Arrive("Unova", 683, 237);
        var hunter = game.Talk("13", "unova_route_13");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!));
        Assert.True(game.Story.Has("FLAG_DAILY_UNOVA_TREASURE_HUNTER"));
        game.Talk("13", "unova_route_13");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!));
        Assert.Contains(hunter.Transcript, l => l.Text.Contains("treasure"));
        game.Arrive("Route13_House1", 5, 8);
        game.TalkTo("2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Power Lens")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_POWER_LENS"));
        // Cobalion on the road waits for the post-game
        Assert.Null(game.Present("21", "unova_route_13"));
        // the bay's trainers swim in its water
        game.Arrive("Unova", 788, 281);
        Assert.True(game.Map.Everyone.Count(n => n.ScriptFile == "undella_bay" && n.TrainerData != null) >= 6);
        // the researcher of the great cave pays once for a Pokémon of the mountain
        game.Arrive("ReversalMountain_3", 79, 8);
        game.Party.Members.RemoveAt(5);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Woobat")!, 30));
        game.TalkTo("16");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM54")!));
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_FALSE_SWIPE"));
        foreach (string song in new[] { "strange_house", "reversal_mountain", "reversal_cave", "undella", "route13", "lacunosa" })
            Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", song + ".mml"))), song);
    }

    [Fact]
    public void RouteSevenHasNoDriftsOfItsOwnInTheImportAndSoNeitherSeasonOpensAWayThere()
    {
        // Black 2's winter layouts are the overworld's cells of Icirrus City, Route 8 and the Dragonspiral Tower, and Twist Mountain's and the
        // Moor of Icirrus's floors (the importer's report lists them): none lies on Route 7, whose winter is a change of its wild
        // Pokémon and of its look alone, so a walk across it is the same in every season (the drifts come with Twist Mountain, plan 22 · U10)
        var world = GameDataFiles.Load<WorldMatrixFile>(Path.Combine(World.Folder, "unova", "matrices", "000.json"));
        var route7 = Enumerable.Range(0, world.Height).SelectMany(y => Enumerable.Range(0, world.Width).Where(x => world.Areas[y].Replace(" ", "").Length > 0 && AreaAt(world, x, y) == "unova_route_7").Select(x => (x, y))).ToList();
        Assert.NotEmpty(route7);
        foreach (var season in world.Seasons ?? new())
            foreach (var (x, y) in route7)
                Assert.Equal("-", SeasonCell(season, x, y));
    }

    // ---- Plan 22 · U8, part a: Route 12 to Opelucid City ---------------------------------------------------------------------

    /// <summary>A game that has come through Lacunosa Town and Route 11 to the east gate of Opelucid City.</summary>
    private static Game AtOpelucid()
    {
        var game = AtLentimas();
        game.Story.SetVar("VAR_UNOVA_REVERSAL", 2);
        game.Story.SetVar("VAR_UNOVA_LACUNOSA", 4);
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Unova)!.Starters[0]);
        game.Arrive("Unova", 446, 175);
        return game;
    }

    [Fact]
    public void IrisMeetsThePlayerAtTheEastEndOfOpelucidCityAndTheChapterBeginsHidden()
    {
        var game = AtOpelucid();
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_OPELUCID"));
        // the chapter's people are hidden until their scenes
        foreach (string key in new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "19" })
            Assert.Null(game.Present(key, "opelucid_city"));
        Assert.NotNull(game.Present("12", "opelucid_city"));
        Assert.True(game.Fires("IrisGreets"));
        Assert.False(game.Fires("TriadAtTheEast"));
        var iris = game.Step("IrisGreets");
        Assert.Contains(iris.Transcript, l => l.Speaker == "Iris" && l.Text.Contains("Route 9"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_OPELUCID"));
        Assert.False(game.Fires("IrisGreets"));
        Assert.Null(game.Present("11", "opelucid_city"));
    }

    private static HashSet<(int, int)> ReachableOn(Map map, (int X, int Y) from)
    {
        var seen = new HashSet<(int, int)> { from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            void Visit(int nx, int ny)
            {
                if (seen.Contains((nx, ny)) || !map.InBounds(nx, ny) || map.IsSolid(nx, ny)) return;
                seen.Add((nx, ny));
                // a person's tile is stood next to and not walked through
                if (map.NPCs.At(nx, ny) != null) return;
                queue.Enqueue((nx, ny));
            }
            // a way of our own that leads to a tile of this map is taken
            if (map.Warps.FirstOrDefault(w => w.SourceX == x && w.SourceY == y && w.TargetMap == map.Name && !w.Shut) is { } warp) { Visit(warp.TargetX, warp.TargetY); continue; }
            Visit(x + 1, y); Visit(x - 1, y); Visit(x, y + 1); Visit(x, y - 1);
        }
        return seen;
    }

    [Fact]
    public void TheDragonGymsPlatformsAreJoinedAndEveryOneLeadsBackToTheDoor()
    {
        var game = AtOpelucid();
        game.Arrive("OpelucidCity_Gym", 22, 54);
        var map = game.Map;
        var fromTheDoor = ReachableOn(map, (22, 54));
        // the strip's top, in front of Drayden, and a tile of each platform with its veteran
        foreach (var tile in new[] { (15, 23), (15, 29), (11, 21), (4, 17), (18, 17) })
            Assert.Contains(tile, fromTheDoor);
        // and from the far platforms the way back is open
        foreach (var from in new[] { (4, 17), (18, 17), (11, 21), (15, 29) })
            Assert.Contains((22, 54), ReachableOn(map, from));
        // everyone stands where somebody can speak to them
        foreach (string key in new[] { "1", "2", "3", "4", "5", "6" })
        {
            var npc = map.NPCs.First(n => n.Key == key);
            Assert.Contains(new[] { (npc.GridX, npc.GridY + 1), (npc.GridX, npc.GridY - 1), (npc.GridX + 1, npc.GridY), (npc.GridX - 1, npc.GridY) }, t => fromTheDoor.Contains(t));
        }
    }

    [Fact]
    public void DraydenGivesTheLegendBadgeAndTm82AndAsksThePlayerToHisHouse()
    {
        var game = AtOpelucid();
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 1);
        game.Arrive("OpelucidCity_Gym", 22, 54);
        game.TalkTo("0");
        Assert.True(game.Story.Has("FLAG_UNOVA_GOT_WATER_OPELUCID"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Fresh Water")!));
        // a veteran fights once, as the trainer of the data
        var ron = game.TalkTo("1");
        Assert.Contains(ron.Log, l => l.StartsWith("battle unova_veteran_ron"));
        var again = game.TalkTo("1");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
        var drayden = game.TalkTo("6");
        Assert.Contains(drayden.Log, l => l.StartsWith("battle unova_leader_drayden"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Legend"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM82")!));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_OPELUCID"));
        Assert.True(game.Story.Has("FLAG_UNOVA_DRAYDEN_AT_HOME"));
        var after = game.TalkTo("6");
        Assert.DoesNotContain(after.Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM82")!));
        Assert.NotEmpty(TrainerDatabase.Get("unova_leader_drayden")!.Party);
    }

    private static int IceTiles(Map map) => Enumerable.Range(380, 70).SelectMany(x => Enumerable.Range(134, 55).Select(z => (x, z))).Count(t => map.GetGroundTile(t.x, t.z) is TileType.Ice or TileType.Snow);

    [Fact]
    public void DraydensTaleIsFollowedByTheFrigatesIceAndTheStreetsEmptyOfTheCityFolk()
    {
        var game = AtOpelucid();
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 2);
        game.Story.Set("FLAG_UNOVA_DRAYDEN_AT_HOME");
        // coming out of the Gym with the Badge, Drayden asks the player to follow him
        Assert.True(game.Fires("DraydenAsks"));
        var asks = game.Step("DraydenAsks");
        Assert.Contains(asks.Transcript, l => l.Speaker == "Drayden" && l.Text.Contains("house"));
        var map = game.Mapped("Unova");
        int before = IceTiles(map);
        // inside, Drayden tells the tale of the two dragons; as he ends the ice falls and the player is put at the door
        game.Arrive("OpelucidCity_DraydensHouse_1F", 9, 13);
        var tale = game.Played.First(p => p.Script.EndsWith("OnEnter") && p.Script.Contains("opelucid_city_2")).Host;
        Assert.Contains(tale.Transcript, l => l.Speaker == "Drayden" && l.Text.Contains("Reshiram"));
        Assert.Contains(tale.Transcript, l => l.Speaker == "Drayden" && l.Text.Contains("Kyurem"));
        Assert.Equal("Unova", game.Map.Name);
        Assert.True(game.Story.Has("FLAG_UNOVA_OPELUCID_ICE"));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_OPELUCID"));
        var ice = game.Played.Last(p => p.Script.StartsWith("opelucid_city.") && p.Script.EndsWith("OnEnter")).Host;
        Assert.Contains(ice.Transcript, l => l.Speaker == "Zinzolin" && l.Text.Contains("Splicers"));
        Assert.Contains(ice.Log, l => l.StartsWith("fade"));
        // the swap lays the city's frozen chunks while the flag is set, and the street's people are indoors
        map.ApplySwappedPlaces(game.Story.Var, game.Story.Has);
        Assert.True(IceTiles(map) > before + 200, $"{before} before, {IceTiles(map)} under the ice");
        Assert.Null(game.Present("12", "opelucid_city"));
        Assert.Null(game.Present("14", "opelucid_city"));
        // the grunts are about the streets, Zinzolin at the Gym's door
        foreach (string key in new[] { "1", "2", "3", "4", "5", "6", "7" })
            Assert.NotNull(game.Present(key, "opelucid_city"));
        var zinzolin = game.Present("1", "opelucid_city")!;
        Assert.Equal((405, 146), (zinzolin.GridX, zinzolin.GridY));
        // and the snow is the zone's while the flag lasts
        Assert.Equal(FieldWeather.Snow, game.Map.WeatherAt(418, 170));
    }

    [Fact]
    public void ThePlayerBeatsTheGruntsAndZinzolinAndTheShadowTriadTakesTheSplicersAndTheIceMeltsAfter()
    {
        var game = AtOpelucid();
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 3);
        game.Arrive("OpelucidCity_DraydensHouse_1F", 9, 13);
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 3);
        game.Story.Unset("FLAG_UNOVA_HIDE_GRUNTS_OPELUCID");
        game.Arrive("Unova", 418, 160);
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_OPELUCID"));
        var map = game.Mapped("Unova");
        map.ApplySwappedPlaces(game.Story.Var, game.Story.Has);
        int frozen = IceTiles(map);
        Assert.True(frozen > 0);
        // three grunts battle (once each), three only talk
        foreach (var (key, trainer) in new[] { ("2", "unova_team_plasma_grunt_41"), ("3", "unova_team_plasma_grunt_42"), ("4", "unova_team_plasma_grunt_43") })
        {
            var fight = game.Talk(key, "opelucid_city");
            Assert.Contains(fight.Log, l => l.StartsWith("battle " + trainer));
            var again = game.Talk(key, "opelucid_city");
            Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
        }
        Assert.DoesNotContain(game.Talk("5", "opelucid_city").Log, l => l.StartsWith("battle"));
        // Zinzolin at the Gym's door: beaten, he leaves with his grunts; Drayden shows the Splicers and the Triad takes them
        var zinzolin = game.Talk("1", "opelucid_city");
        Assert.Contains(zinzolin.Log, l => l.StartsWith("battle unova_team_plasma_zinzolin_3"));
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_OPELUCID"));
        Assert.Contains(zinzolin.Transcript, l => l.Speaker == "Shadow Triad" && l.Text.Contains("Gym"));
        Assert.Null(game.Present("1", "opelucid_city"));
        Assert.Null(game.Present("2", "opelucid_city"));
        Assert.Null(game.Present("9", "opelucid_city"));
        var triad = game.Present("8", "opelucid_city")!;
        Assert.Equal((442, 175), (triad.GridX, triad.GridY));
        // the Triad's member stops the player at the east end, and the Xtransceiver rings with Cheren's word; the ice melts after
        Assert.True(game.Fires("TriadAtTheEast"));
        Assert.False(game.Fires("IrisGreets"));
        var stand = game.Step("TriadAtTheEast");
        Assert.Contains(stand.Log, l => l.StartsWith("battle unova_team_plasma_shadow_4"));
        Assert.Contains(stand.Transcript, l => l.Speaker == "Cheren" && l.Text.Contains("Humilau"));
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_OPELUCID"));
        Assert.False(game.Story.Has("FLAG_UNOVA_OPELUCID_ICE"));
        map.ApplySwappedPlaces(game.Story.Var, game.Story.Has);
        Assert.True(IceTiles(map) < frozen / 4, "the ground comes back as it was");
        Assert.NotNull(game.Present("12", "opelucid_city"));
        Assert.Null(game.Present("8", "opelucid_city"));
        Assert.Null(game.Present("19", "opelucid_city"));
        Assert.False(game.Fires("TriadAtTheEast"));
    }

    [Fact]
    public void ASaveFromBeforeTheOpelucidChapterIsGivenItsHiddenPeople()
    {
        var game = new Game();
        game.Story.Unset("FLAG_UNOVA_OPELUCID_BEGUN");
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_DRAYDEN_OPELUCID", "FLAG_UNOVA_HIDE_GRUNTS_OPELUCID", "FLAG_UNOVA_HIDE_TRIAD_EAST", "FLAG_UNOVA_HIDE_IRIS_OPELUCID" })
            game.Story.Unset(flag);
        game.Play(Scripts.Find("UnovaCatchUpC", "common")!);
        Assert.True(game.Story.Has("FLAG_UNOVA_OPELUCID_BEGUN"));
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_DRAYDEN_OPELUCID", "FLAG_UNOVA_HIDE_GRUNTS_OPELUCID", "FLAG_UNOVA_HIDE_TRIAD_EAST", "FLAG_UNOVA_HIDE_IRIS_OPELUCID", "FLAG_UNOVA_HIDE_CHEREN_OPELUCID" })
            Assert.True(game.Story.Has(flag), flag);
        // a new game has them hidden already, through the chain of every chapter's hiding
        Assert.True(new Game().Story.Has("FLAG_UNOVA_OPELUCID_BEGUN"));
    }

    [Fact]
    public void RouteTwelveAndTheVillageBridgeAndRouteElevenAreWalkedByTheirTrainers()
    {
        var game = AtOpelucid();
        foreach (var (map, x, y, area, keys) in new[] {
                     ("Unova", 590, 175, "unova_route_12", new[] { "0", "1", "2", "3" }),
                     ("VillageBridge", 3, 32, "village_bridge", new[] { "4", "5", "6", "7", "19", "20", "21" }),
                     ("Unova", 480, 175, "unova_route_11", new[] { "4", "5", "6" }),
                     ("Unova", 360, 175, "unova_route_9", new[] { "0", "1", "2", "3", "4", "5" }) })
        {
            game.Arrive(map, x, y);
            foreach (string key in keys)
            {
                var npc = game.Present(key, area);
                Assert.NotNull(npc);
                Assert.True(npc!.IsTrainer, $"{area} {key}");
            }
        }
        // Virizion, held for plan 22 · U10, is not on the road, nor are the two spare balls
        game.Arrive("Unova", 480, 175);
        Assert.Null(game.Present("0", "unova_route_11"));
        Assert.Null(game.Present("7", "unova_route_11"));
        // Route 9's way west to the Tubeline Bridge waits for the post-game
        game.Arrive("Unova", 360, 175);
        Assert.True(game.Map.Warps.All(w => !w.TargetMap.Contains("Tubeline", StringComparison.OrdinalIgnoreCase)));
        // the gate houses join the bridge to both roads and Opelucid City to both of its
        game.Arrive("Unova", 583, 175);
        game.Through("VillageBridgeGate_East");
        Assert.Equal("VillageBridgeGate_East", game.Map.Name);
        game.Through("VillageBridge");
        Assert.Equal("VillageBridge", game.Map.Name);
    }

    [Fact]
    public void ShoppingMallNinesFiveCountersSellTheirOwnGoods()
    {
        var game = AtOpelucid();
        game.Arrive("ShoppingMallNine_1F", 15, 27);
        var counters = new[] { ("12", "mall_medicine", "Hyper Potion"), ("13", "mall_balls", "Dive Ball"), ("14", "mall_vitamins", "Protein"),
            ("15", "mall_battle", "X Speed"), ("16", "mall_machines", "TM83") };
        foreach (var (key, mart, item) in counters)
        {
            var shop = game.TalkTo(key);
            Assert.Contains(shop.Log, l => l == $"open Shop {mart}");
            Assert.Contains(MartDatabase.Stock(mart, 0), i => i.Name == item);
        }
        Assert.NotNull(game.Present("10", "shopping_mall"));
        game.Through("ShoppingMallNine_2F");
        Assert.Equal("ShoppingMallNine_2F", game.Map.Name);
    }

    [Fact]
    public void DraydensHouseAndTheOtherRoomsOfOpelucidAreThereAndTheSongsAreOurOwn()
    {
        var game = AtOpelucid();
        // the woman upstairs in Drayden's house gives the Ring Target once
        game.Arrive("OpelucidCity_DraydensHouse_2F", 9, 5);
        game.TalkTo("1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Ring Target")!));
        game.TalkTo("1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Ring Target")!));
        // each house of two floors has its stairs both ways
        foreach (var (down, up) in new[] { ("OpelucidCity_DraydensHouse_1F", "OpelucidCity_DraydensHouse_2F"), ("OpelucidCity_TripleHouse_1F", "OpelucidCity_TripleHouse_2F"),
                     ("OpelucidCity_MusicalHouse_1F", "OpelucidCity_MusicalHouse_2F"), ("OpelucidCity_TradeHouse_1F", "OpelucidCity_TradeHouse_2F"),
                     ("OpelucidCity_OldHouse_1F", "OpelucidCity_OldHouse_2F") })
        {
            Assert.Contains(game.Mapped(down).Warps, w => w.TargetMap == up);
            Assert.Contains(game.Mapped(up).Warps, w => w.TargetMap == down);
        }
        foreach (string song in new[] { "opelucid", "route12", "village_bridge", "shopping_mall" })
            Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", song + ".mml"))), song);
        Assert.Contains(RegionDatabase.Get(RegionDatabase.Unova)!.File!.Spawns, s => s.Room == "OpelucidCity_PokemonCenter");
    }

    // ---- Plan 22 · U8, part b: the Marine Tube to the Plasma Frigate --------------------------------------------------------

    /// <summary>A game that has seen Opelucid City's chapter to its end (Cheren's word that the Frigate made for Humilau City).</summary>
    private static Game AtHumilau()
    {
        var game = AtOpelucid();
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 7);
        game.Story.GiveBadge(RegionDatabase.Unova, "Legend");
        game.Arrive("Unova", 790, 179);
        return game;
    }

    [Fact]
    public void TheMarineTubeIsShutUntilOpelucidsChapterEndsAndRunsUnderTheSeaToHumilau()
    {
        var game = AtOpelucid();
        game.Arrive("MarineTubeGate_Undella", 13, 8);
        Assert.True(game.Fires("TubeClosed"));
        var shut = game.Step("TubeClosed");
        Assert.Contains(shut.Transcript, l => l.Speaker == "Worker");
        Assert.Equal(12, game.Tile.X);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_TUBE"));
        game.Story.SetVar("VAR_UNOVA_OPELUCID", 7);
        game.Step("TubeClosed");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_TUBE"));
        Assert.False(game.Fires("TubeClosed"));
        // the gate's north door is the tube's, a map of ours, whose far end is Humilau's gate
        game.Arrive("MarineTubeGate_Undella", 27, 2);
        game.Through("MarineTube");
        Assert.Equal("MarineTube", game.Map.Name);
        Assert.Equal("unova/marine_tube", game.Map.BgmTrack);
        var walk = ReachableOn(game.Map, game.Tile);
        Assert.Contains(walk, t => game.Map.GetWarpAt(t.Item1, t.Item2)?.TargetMap == "MarineTubeGate_Humilau");
        game.Through("MarineTubeGate_Humilau");
        Assert.Equal("MarineTubeGate_Humilau", game.Map.Name);
        game.Through("Unova");
        Assert.Equal("Unova", game.Map.Name);
        Assert.Equal("humilau_city", game.Map.ScriptFileAt(game.Tile.X, game.Tile.Y));
    }

    [Fact]
    public void HumilausHousesStandOnTheWaterAndAreWalkedIntoFromTheirBoardwalks()
    {
        var game = AtHumilau();
        var map = game.Map;
        foreach (string house in new[] { "HumilauCity_House1", "HumilauCity_House2", "HumilauCity_House3", "HumilauCity_House4", "HumilauCity_House5" })
        {
            var door = map.Warps.Single(w => w.TargetMap == house);
            Assert.False(map.IsSolid(door.SourceX, door.SourceY), house);
            // the room's way out puts the player on the deck before the door, never on the door
            var back = game.Mapped(house).Warps.First(w => w.TargetMap == "Unova");
            Assert.Equal((door.SourceX, door.SourceY + 1), (back.TargetX, back.TargetY));
            Assert.False(map.IsSolid(back.TargetX, back.TargetY), house);
        }
        // the boardwalks are walked from the city's square up to the northern houses
        var walk = ReachableOn(map, (784, 176));
        Assert.Contains((797, 134), walk);
        Assert.Equal(Architecture.Stilts, WorldModels.Of("c14_house", "unova")!.Town);
        Assert.Equal(PokemonType.Water, WorldModels.GymTheme("humilau_city"));
        Assert.Contains(RegionDatabase.Get(RegionDatabase.Unova)!.File!.Spawns, s => s.Room == "HumilauCity_PokemonCenter");
    }

    [Fact]
    public void HughSendsThePlayerToMarlonWhoGivesTheWaveBadgeAndHughSendsThemToRouteTwentyTwo()
    {
        var game = AtHumilau();
        Assert.NotNull(game.Present("0", "humilau_city"));
        Assert.Null(game.Present("1", "humilau_city"));
        Assert.True(game.Fires("HughWaits"));
        var hugh = game.Step("HughWaits");
        Assert.Contains(hugh.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("beach"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_HUMILAU"));
        // the Gym's guide: Fresh Water and Marlon away
        game.Arrive("HumilauCity_Gym", 16, 74);
        Assert.Null(game.Present("0", "humilau_city_gym"));
        var guide = game.TalkTo("7");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Fresh Water")!));
        Assert.Contains(game.TalkTo("7").Transcript, l => l.Text.Contains("swimming"));
        // Marlon on the beach goes back to his Gym
        game.Arrive("Unova", 784, 186);
        var marlon = game.Talk("1", "humilau_city");
        Assert.Contains(marlon.Transcript, l => l.Speaker == "Marlon" && l.Text.Contains("Gym"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_HUMILAU"));
        Assert.Null(game.Present("1", "humilau_city"));
        game.Arrive("HumilauCity_Gym", 16, 74);
        Assert.NotNull(game.Present("0", "humilau_city_gym"));
        // the walkway from the door reaches the dock and both trainers on the cross-walk
        var walk = ReachableOn(game.Map, (16, 74));
        foreach (var tile in new[] { (25, 61), (13, 63), (21, 63) }) Assert.Contains(tile, walk);
        var leader = game.TalkTo("0");
        Assert.Contains(leader.Log, l => l.StartsWith("battle unova_leader_marlon"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Unova, "Wave"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_HUMILAU"));
        // out of the Gym: Hugh and Marlon, then Route 22
        game.Arrive("Unova", 783, 173);
        Assert.True(game.Fires("MarlonAndHugh"));
        var outside = game.Step("MarlonAndHugh");
        Assert.Contains(outside.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Route 22"));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_HUMILAU"));
        Assert.Null(game.Present("0", "humilau_city"));
        Assert.Null(game.Present("1", "humilau_city"));
    }

    [Fact]
    public void ColressGivesHisMachineOnRouteTwentyTwoAndItWakesTheCrustleInTheSeasideCave()
    {
        var game = AtHumilau();
        game.Story.SetVar("VAR_UNOVA_HUMILAU", 4);
        game.Story.Unset("FLAG_UNOVA_HIDE_COLRESS_ROUTE_22");
        game.Arrive("Unova", 752, 147);
        Assert.Null(game.Present("6", "unova_route_22"));
        Assert.True(game.Fires("Colress"));
        var colress = game.Step("Colress");
        Assert.Contains(colress.Transcript, l => l.Speaker == "Colress" && l.Text.Contains("Seaside Cave"));
        Assert.True(game.Story.Has("FLAG_UNOVA_COLRESS_MACHINE"));
        Assert.Equal(5, game.Story.Var("VAR_UNOVA_HUMILAU"));
        Assert.Null(game.Present("7", "unova_route_22"));
        // the Crustle before the way out to Route 21's east beach
        game.Arrive("SeasideCave_1F", 28, 10);
        Assert.NotNull(game.Present("9", "seaside_cave"));
        game.Answers.Add(0);
        var crustle = game.Talk("9", "seaside_cave");
        Assert.Contains(crustle.Log, l => l.StartsWith("wildbattle Crustle 42"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_CRUSTLE"));
        Assert.True(game.Story.Has("FLAG_UNOVA_COLRESS_MACHINE_BROKE"));
        Assert.False(game.Story.Has("FLAG_UNOVA_COLRESS_MACHINE"));
        Assert.Null(game.Present("9", "seaside_cave"));
        // Rocky stands in the way from Undella Bay until beaten
        var rocky = game.Talk("12", "seaside_cave");
        Assert.Contains(rocky.Log, l => l.StartsWith("battle unova_black_belt_rocky"));
        Assert.Null(game.Present("12", "seaside_cave"));
        Assert.Null(game.Present("8", "seaside_cave"));
    }

    [Fact]
    public void MarlonsGangplankLeadsAboardAndTheFrigatesRoomsPlayToKyuremAndTheFlight()
    {
        var game = AtHumilau();
        game.Story.SetVar("VAR_UNOVA_HUMILAU", 5);
        game.Arrive("Unova", 795, 241);
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "PlasmaFrigate" && w.SourceX > 400 && !w.Shut);
        var plank = game.Step("Gangplank");
        Assert.Contains(plank.Transcript, l => l.Speaker == "Marlon" && l.Text.Contains("help"));
        Assert.True(game.Story.Has("FLAG_UNOVA_FRIGATE_AT_ROUTE_21"));
        Assert.Equal(6, game.Story.Var("VAR_UNOVA_HUMILAU"));
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "PlasmaFrigate" && w.SourceX == 800 && !w.Shut);
        // aboard: nobody turns the player back, and the gangway leads to Route 21 now
        game.Arrive("Unova", 799, 241);
        game.Through("PlasmaFrigate");
        Assert.Equal("PlasmaFrigate", game.Map.Name);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "Unova" && w.TargetX == 800 && !w.Shut);
        // the hold: Hugh's tag battle against two grunts
        game.Arrive("PlasmaFrigate_Hold", 11, 15);
        var hold = game.Step("HoldScene");
        Assert.Contains(hold.Log, l => l.StartsWith("battle unova_team_plasma_grunt_50 and unova_team_plasma_grunt_51 with unova_rival_28"));
        Assert.Equal(7, game.Story.Var("VAR_UNOVA_HUMILAU"));
        Assert.True(game.Fires("Barrier"));
        // the two switches: a cabin off the forward hall, and Colress in the stern room
        game.Arrive("PlasmaFrigate_Cabin", 6, 3);
        game.Answers.Add(0);
        game.Play(Scripts.Find("Switch", "plasma_frigate_9")!);
        Assert.True(game.Story.Has("FLAG_UNOVA_FRIGATE_SWITCH_A"));
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_FRIGATE_BARRIER"));
        game.Arrive("PlasmaFrigate_Stern", 11, 5);
        var colress = game.TalkTo("colress_stern");
        Assert.Contains(colress.Log, l => l.StartsWith("battle unova_team_plasma_colress"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_FRIGATE_BARRIER"));
        game.Arrive("PlasmaFrigate_Hold", 11, 9);
        Assert.False(game.Fires("Barrier"));
        // Kyurem's chamber: Zinzolin, Hugh's second tag battle, the Shadow Triad and the beach
        game.Arrive("PlasmaFrigate_Bridge", 12, 19);
        var kyurem = game.Step("Kyurem");
        Assert.Contains(kyurem.Log, l => l.StartsWith("battle unova_team_plasma_grunt_49 and unova_team_plasma_zinzolin with unova_rival_28"));
        Assert.Contains(kyurem.Transcript, l => l.Speaker == "Zinzolin" && l.Text.Contains("Kyurem"));
        Assert.Equal("Unova", game.Map.Name);
        Assert.Equal(9, game.Story.Var("VAR_UNOVA_HUMILAU"));
        var flight = game.Played.Last(p => p.Script.StartsWith("unova_route_21.") && p.Script.EndsWith("OnEnter")).Host;
        Assert.Contains(flight.Transcript, l => l.Speaker == "Cheren" && l.Text.Contains("Giant Chasm"));
        Assert.True(game.Story.Has("FLAG_UNOVA_FRIGATE_LEFT_ROUTE_21"));
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "PlasmaFrigate" && w.SourceX > 400 && !w.Shut);
        Assert.NotNull(game.Present("11", "unova_route_21"));
        Assert.Null(game.Present("9", "unova_route_21"));
    }

    [Fact]
    public void ASaveFromBeforeTheHumilauChapterIsGivenItsHiddenPeopleAndTheSongsAreOurOwn()
    {
        var game = new Game();
        game.Story.Unset("FLAG_UNOVA_HUMILAU_BEGUN");
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_HUGH_HUMILAU", "FLAG_UNOVA_HIDE_COLRESS_ROUTE_22", "FLAG_UNOVA_HIDE_TERRAKION" })
            game.Story.Unset(flag);
        game.Play(Scripts.Find("UnovaCatchUpD", "common")!);
        Assert.True(game.Story.Has("FLAG_UNOVA_HUMILAU_BEGUN"));
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_HUGH_HUMILAU", "FLAG_UNOVA_HIDE_COLRESS_ROUTE_22", "FLAG_UNOVA_HIDE_TERRAKION", "FLAG_UNOVA_HIDE_HUGH_BRIDGE" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.True(new Game().Story.Has("FLAG_UNOVA_HUMILAU_BEGUN"));
        foreach (string song in new[] { "humilau", "marine_tube", "seaside_cave", "colress" })
            Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", song + ".mml"))), song);
        Assert.Equal(Architecture.Skyline, WorldModels.Of("c8_buildb", "unova")!.Town);
    }

    // ---- Plan 22 · U9, part a: the Giant Chasm --------------------------------------------------------------------------

    [Fact]
    public void RoodsSpyKeepsTheChasmShutUntilTheFrigateHasFlownAndThenHughCatchesThePlayerUp()
    {
        var game = AtHumilau();
        game.Arrive("GiantChasm_Cave", 61, 13);
        Assert.Equal("unova/giant_chasm", game.Map.BgmTrackAt(61, 13));
        Assert.NotNull(game.Present("10", "giant_chasm_2"));
        Assert.Null(game.Present("hugh_cave", "giant_chasm_2"));
        // the way up into the crater is reached from Route 22's door; before the Frigate has flown the spy turns the player back from it
        Assert.Contains((23, 51), ReachableOn(game.Map, (61, 13)));
        Assert.True(game.Fires("HughInTheCave"));
        var back = game.Step("HughInTheCave");
        Assert.Contains(back.Transcript, l => l.Speaker == "Grunt" && l.Text.Contains("crater"));
        Assert.Equal((23, 52), game.Tile);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_CHASM"));
        Assert.Contains(game.Talk("10", "giant_chasm_2").Transcript, l => l.Speaker == "Grunt" && l.Text.Contains("Rood"));
        // once it has, Hugh catches up, the spy goes, and the way to the crater is open
        game.Story.SetVar("VAR_UNOVA_HUMILAU", 9);
        var hugh = game.Step("HughInTheCave");
        Assert.Contains(hugh.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Giant Chasm"));
        Assert.Contains(hugh.Transcript, l => l.Speaker == "Grunt" && l.Text.Contains("crater"));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_CHASM"));
        Assert.Null(game.Present("10", "giant_chasm_2"));
        Assert.Null(game.Present("hugh_cave", "giant_chasm_2"));
        Assert.False(game.Fires("HughInTheCave"));
        // the way into the crater leads to the crater with the Frigate; the guards keep the way out to Route 23
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "GiantChasm_CraterFrigate" && !w.Shut);
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "GiantChasm_Crater" && !w.Shut);
        Assert.NotNull(game.Present("7", "giant_chasm_2"));
        Assert.NotNull(game.Present("8", "giant_chasm_2"));
    }

    [Fact]
    public void RoodsStandTheTriadAboardTheFrigateAndGhetsisAndKyuremInTheCavernEndTheChapter()
    {
        var game = AtHumilau();
        game.Story.SetVar("VAR_UNOVA_HUMILAU", 9);
        game.Arrive("GiantChasm_Cave", 61, 13);
        game.Step("HughInTheCave");

        // the crater: Rood and his sages against the grunts, Hugh already there
        game.Arrive("GiantChasm_Cave", 23, 51);
        game.Through("GiantChasm_CraterFrigate");
        Assert.Equal("GiantChasm_CraterFrigate", game.Map.Name);
        Assert.NotNull(game.Present("2", "giant_chasm_4"));
        Assert.NotNull(game.Present("4", "giant_chasm_4"));
        Assert.NotNull(game.Present("6", "giant_chasm_4"));
        var stand = game.Step("RoodAndTheGrunts");
        Assert.Contains(stand.Log, l => l.StartsWith("battle unova_team_plasma_grunt_46"));
        Assert.Contains(stand.Transcript, l => l.Speaker == "Rood" && l.Text.Contains("ramp"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_CHASM"));
        Assert.Null(game.Present("4", "giant_chasm_4"));
        Assert.Null(game.Present("6", "giant_chasm_4"));
        Assert.NotNull(game.Present("2", "giant_chasm_4"));

        // aboard: Ghetsis's plan, Hugh, the Liepard and the three of the Triad
        game.Through("PlasmaFrigate_Ramp");
        Assert.Equal("PlasmaFrigate_Ramp", game.Map.Name);
        Assert.NotNull(game.Present("ghetsis_ramp", "plasma_frigate_6"));
        var aboard = game.Step("GhetsisAboard");
        Assert.Contains(aboard.Transcript, l => l.Speaker == "Ghetsis" && l.Text.Contains("Kyurem"));
        Assert.Contains(aboard.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Purrloin"));
        foreach (string triad in new[] { "unova_team_plasma_shadow", "unova_team_plasma_shadow_2", "unova_team_plasma_shadow_3" })
            Assert.Contains(aboard.Log, l => l == $"battle {triad}" || l.StartsWith($"battle {triad} "));
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_CHASM"));
        Assert.Null(game.Present("ghetsis_ramp", "plasma_frigate_6"));
        Assert.Null(game.Present("triad_ramp_a", "plasma_frigate_6"));
        Assert.NotNull(game.Present("hugh_ramp", "plasma_frigate_6"));
        Assert.NotNull(game.Present("liepard", "plasma_frigate_6"));

        // the cavern: Ghetsis and Kyurem, N and Zekrom, Kyurem's black form and Ghetsis's own battle
        game.Arrive("GiantChasm_Cavern", 15, 45);
        Assert.NotNull(game.Present("ghetsis", "giant_chasm_6"));
        Assert.NotNull(game.Present("kyurem", "giant_chasm_6"));
        Assert.Null(game.Present("kyurem_black", "giant_chasm_6"));
        var cavern = game.Step("Ghetsis");
        int fused = cavern.Log.FindIndex(l => l.StartsWith("battle unova_ghetsis_kyurem"));
        int himself = cavern.Log.FindIndex(l => l.StartsWith("battle unova_team_plasma_ghetsis"));
        Assert.True(fused >= 0 && himself > fused);
        Assert.Contains(cavern.Transcript, l => l.Speaker == "N" && l.Text.Contains("Pokémon League"));
        Assert.Contains(cavern.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Route 23"));
        Assert.Equal(4, game.Story.Var("VAR_UNOVA_CHASM"));
        Assert.True(game.Story.Has("FLAG_UNOVA_CHASM_CLEARED"));
        foreach (string gone in new[] { "ghetsis", "kyurem", "kyurem_black", "n", "zekrom", "hugh_cavern", "cheren_cavern", "triad_cavern_a" })
            Assert.Null(game.Present(gone, "giant_chasm_6"));
        Assert.False(game.Fires("Ghetsis"));

        // afterwards the cave and the passage lead into the crater as it is without the Frigate, where Rood stays on
        game.Arrive("GiantChasm_Cave", 23, 51);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "GiantChasm_Crater" && !w.Shut);
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "GiantChasm_CraterFrigate" && !w.Shut);
        Assert.Null(game.Present("7", "giant_chasm_2"));
        game.Arrive("GiantChasm_Passage", 16, 29);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "GiantChasm_Crater" && !w.Shut);
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "GiantChasm_CraterFrigate" && !w.Shut);
        game.Arrive("GiantChasm_Crater", 26, 78);
        Assert.Contains(game.Talk("7", "giant_chasm_3").Transcript, l => l.Speaker == "Rood");
        game.Arrive("PlasmaFrigate_Ramp", 11, 15);
        Assert.Null(game.Present("hugh_ramp", "plasma_frigate_6"));
    }

    [Fact]
    public void GhetsissKyuremIsATeamOfOursInItsBlackFormAndASaveFromBeforeTheChasmIsGivenItsHiddenPeople()
    {
        var kyurem = TrainerDatabase.PartyOf(TrainerDatabase.Get("unova_ghetsis_kyurem")!).Members.Single();
        Assert.Equal("Kyurem", kyurem.Species.Name);
        Assert.Equal("Kyurem-Black", kyurem.Form);
        Assert.Equal(50, kyurem.Level);
        Assert.NotNull(TrainerDatabase.Get("unova_team_plasma_ghetsis"));

        var game = new Game();
        Assert.True(game.Story.Has("FLAG_UNOVA_CHASM_BEGUN"));
        game.Story.Unset("FLAG_UNOVA_CHASM_BEGUN");
        foreach (string flag in new[] { "FLAG_UNOVA_HIDE_HUGH_CAVE", "FLAG_UNOVA_HIDE_ROOD_CHASM", "FLAG_UNOVA_HIDE_GHETSIS_CAVERN" })
            game.Story.Unset(flag);
        game.Play(Scripts.Find("UnovaCatchUpE", "common")!);
        foreach (string flag in new[] { "FLAG_UNOVA_CHASM_BEGUN", "FLAG_UNOVA_HIDE_HUGH_CAVE", "FLAG_UNOVA_HIDE_ROOD_CHASM", "FLAG_UNOVA_HIDE_GHETSIS_CAVERN", "FLAG_UNOVA_HIDE_N_CAVERN" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", "giant_chasm.mml"))));
        foreach (string map in new[] { "GiantChasm_Cave", "GiantChasm_Crater", "GiantChasm_CraterFrigate", "GiantChasm_Passage", "GiantChasm_Cavern", "PlasmaFrigate_Ramp" })
            Assert.Contains(map, RegionDatabase.Get(RegionDatabase.Unova)!.Maps);
    }

    // ---- Plan 22 · U9, parts b and c: Route 23, Victory Road and the League ---------------------------------------------

    /// <summary>A game past the Giant Chasm, with all eight of Unova's Badges, standing at the foot of Victory Road.</summary>
    private static Game PastTheChasm()
    {
        var game = AtHumilau();
        game.Story.SetVar("VAR_UNOVA_HUMILAU", 9);
        game.Story.SetVar("VAR_UNOVA_CHASM", 4);
        game.Story.Set("FLAG_UNOVA_CHASM_CLEARED");
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Unova)!.BadgeNames) game.Story.GiveBadge(RegionDatabase.Unova, badge);
        return game;
    }

    [Fact]
    public void RouteTwentyThreeLeadsFromTheChasmToVictoryRoadWhereNGivesWaterfallAndTheGatesCheckTheBadges()
    {
        var game = PastTheChasm();
        // the chasm's way out to Route 23 is no longer kept, and Route 23 climbs to Victory Road
        game.Arrive("GiantChasm_Cave", 23, 51);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "Unova" && !w.Shut);
        game.Arrive("Unova", 664, 137);
        Assert.Equal("unova/route23", game.Map.BgmTrackAt(664, 137));
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaVictoryRoad" && !w.Shut);
        // N meets the player at the head of the way up and gives HM05
        game.Arrive("UnovaVictoryRoad", 80, 57);
        Assert.Equal("unova/victory_road", game.Map.BgmTrackAt(80, 57));
        Assert.NotNull(game.Present("0", "unova_victory_road"));
        var n = game.Step("N");
        Assert.Contains(n.Transcript, l => l.Speaker == "N" && l.Text.Contains("Pokémon League"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM05")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_N_VICTORY_ROAD"));
        Assert.Null(game.Present("0", "unova_victory_road"));
        Assert.False(game.Fires("N"));
        // the eight gates, west along the road, then the last before the cave
        foreach (string gate in new[] { "BasicGate", "ToxicGate", "InsectGate", "BoltGate", "QuakeGate", "JetGate", "LegendGate", "WaveGate" })
        {
            Assert.True(game.Fires(gate), gate);
            Assert.Contains(game.Step(gate).Transcript, l => l.Text.Contains("confirmed"));
        }
        Assert.Contains(game.Step("LastGate").Transcript, l => l.Text.Contains("All eight Badges"));
        Assert.Equal(9, game.Story.Var("VAR_UNOVA_BADGE_GATES"));
        // the cave's mouth leads into Black 2's own floor of Victory Road, and the Pokémon Center stands at its head
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaVictoryRoad_Ruins" && !w.Shut);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaVictoryRoad_PokemonCenter" && !w.Shut);
    }

    [Fact]
    public void ABadgeCheckGateTurnsBackAPlayerWithoutItsBadge()
    {
        var game = PastTheChasm();
        game.Story.SetVar("VAR_UNOVA_N_VICTORY_ROAD", 1);
        game.Story.SetBadges(RegionDatabase.Unova, 0);
        game.Arrive("UnovaVictoryRoad", 76, 49);
        var shut = game.Step("BasicGate");
        Assert.Contains(shut.Transcript, l => l.Text.Contains("won't open"));
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_BADGE_GATES"));
        Assert.Equal((76, 48), game.Tile);
    }

    [Theory]
    [InlineData(0, "unova_rival_16")]
    [InlineData(1, "unova_rival_17")]
    [InlineData(2, "unova_rival_18")]
    public void HughBattlesThePlayerInVictoryRoadsLastTunnelAndItsWayOutReachesTheLeague(int starter, string team)
    {
        var game = PastTheChasm();
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Unova)!.Starters[starter]);
        game.Arrive("UnovaVictoryRoad_Tunnel", 22, 12);
        Assert.Null(game.Present("hugh_tunnel", "unova_victory_road_16"));
        var hugh = game.Step("Hugh");
        Assert.Contains(hugh.Log, l => l.StartsWith($"battle {team} "));
        Assert.Contains(hugh.Transcript, l => l.Speaker == "Hugh" && l.Text.Contains("Champion"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM24")!));
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_HUGH_TUNNEL"));
        Assert.Null(game.Present("hugh_tunnel", "unova_victory_road_16"));
        // Black 2's bridge on its rails is left out: the tunnel's way out leads onto the League's plaza, and back
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaPokemonLeague" && w.TargetX == 32 && w.TargetY == 57);
        game.Through("UnovaPokemonLeague");
        Assert.Equal("UnovaPokemonLeague", game.Map.Name);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaVictoryRoad_Tunnel" && !w.Shut);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "UnovaPokemonLeague_PokemonCenter" && !w.Shut);
    }

    [Fact]
    public void TheLeaguesPlazaIsItsOneChunkReadAsFourQuarters()
    {
        var plaza = Unova.Value.Matrix(87);
        Assert.Equal(2, plaza.Width);
        Assert.Equal(2, plaza.Height);
        var game = new Game(begin: false);
        Assert.Equal(64, game.Mapped("UnovaPokemonLeague").Width);
        Assert.Equal(64, game.Mapped("UnovaPokemonLeague").Height);
        // the steps up to the League's door and the Pokémon Center's door lie past the chunk's first quarter
        Assert.Contains(game.Mapped("UnovaPokemonLeague").Warps, w => w.TargetMap == "UnovaPokemonLeague_Hall" && w.SourceY == 45);
        Assert.Contains(game.Mapped("UnovaPokemonLeague").Warps, w => w.TargetMap == "UnovaPokemonLeague_PokemonCenter" && w.SourceY == 47);
    }

    [Fact]
    public void TheEliteFourTheChampionAndTheHallOfFameEndUnovasStoryAndThePokedexIsMadeNational()
    {
        var game = PastTheChasm();
        game.Story.SetVar("VAR_UNOVA_HUGH_TUNNEL", 1);
        // the plaza's veteran asks whoever comes to the steps; no and they step back, yes and they may go in
        game.Arrive("UnovaPokemonLeague", 32, 50);
        game.Answers.Add(1);
        game.Step("Guard");
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_LEAGUE_GUARD"));
        game.Answers.Add(0);
        game.Step("Guard");
        Assert.Equal(1, game.Story.Var("VAR_UNOVA_LEAGUE_GUARD"));
        game.Through("UnovaPokemonLeague_Hall");
        Assert.Equal("UnovaPokemonLeague_Hall", game.Map.Name);
        // the statue won't move before the Four are beaten
        Assert.Contains(game.Play(Scripts.Find("Statue", "unova_pokemon_league_2")!).Transcript, l => l.Text.Contains("won't move"));
        // each of the four in their room, up its stairs, and the light back down to the hall
        foreach (var (room, who, team) in new[]
        {
            ("UnovaPokemonLeague_ShauntalsRoom", "Shauntal", "unova_elite_four_shauntal"),
            ("UnovaPokemonLeague_GrimsleysRoom", "Grimsley", "unova_elite_four_grimsley"),
            ("UnovaPokemonLeague_CaitlinsRoom", "Caitlin", "unova_elite_four_caitlin"),
            ("UnovaPokemonLeague_MarshalsRoom", "Marshal", "unova_elite_four_marshal")
        })
        {
            game.Through(room);
            Assert.Equal(room, game.Map.Name);
            Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "UnovaPokemonLeague_Hall" && !w.Shut);
            game.Through(room);
            var master = game.Map.NPCs.Single(p => p.Name == who);
            Assert.Contains((master.GridX, master.GridY + 2), ReachableOn(game.Map, game.Tile));
            Assert.False(game.Fires("Pad"));
            var battle = game.Talk("0", game.Map.ScriptFileAt(master.GridX, master.GridY)!);
            Assert.Contains(battle.Log, l => l.StartsWith($"battle {team} "));
            Assert.Equal(1, game.Story.Var($"VAR_UNOVA_LEAGUE_{who.ToUpperInvariant()}"));
            Assert.True(game.Fires("Pad"));
            game.Step("Pad");
            Assert.Equal("UnovaPokemonLeague_Hall", game.Map.Name);
        }
        // the statue carries the challenger down, and the walk leads to the Champion's room
        game.Answers.Add(0);
        game.Play(Scripts.Find("Statue", "unova_pokemon_league_2")!);
        Assert.Equal("UnovaPokemonLeague_Descent", game.Map.Name);
        Assert.Contains((31, 3), ReachableOn(game.Map, game.Tile));
        game.Arrive("UnovaPokemonLeague_Descent", 31, 3);
        game.Through("UnovaPokemonLeague_ChampionsRoom");
        Assert.Equal("UnovaPokemonLeague_ChampionsRoom", game.Map.Name);
        game.Through("UnovaPokemonLeague_ChampionsRoom");
        Assert.Contains((16, 43), ReachableOn(game.Map, game.Tile));
        // Iris, then the Hall of Fame: the team recorded, Unova's story ended, home, saved and the credits
        var iris = game.Step("Iris");
        Assert.Contains(iris.Log, l => l.StartsWith("battle unova_champion_iris "));
        Assert.Contains(game.Played, p => p.Script.EndsWith("OnEnter") && p.Host.Log.Contains("halloffame"));
        Assert.True(game.Story.Has("UnovaHallOfFame"));
        Assert.Equal("AspertiaCity_PlayersHouse", game.Map.Name);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_LEAGUE_IRIS"));
        Assert.Equal(2, game.Story.Var("VAR_UNOVA_HOUSE"));
        // home: Mom, and the professor's father with the National Pokédex
        var home = game.Step("ChampionHome");
        Assert.Contains(home.Transcript, l => l.Speaker == "Prof. Cedric Juniper" && l.Text.Contains("National Pokédex"));
        Assert.True(home.NationalDex);
        Assert.Equal(3, game.Story.Var("VAR_UNOVA_HOUSE"));
        Assert.Null(game.Present("cedric_home", "aspertia_city_2"));
        // and the airline's attendant at Mistralton names the flight on to Kalos
        var link = RegionDatabase.LinkFrom(RegionDatabase.Unova)!;
        Assert.Equal(RegionDatabase.Kalos, link.To);
        Assert.Equal("MistraltonCity_CargoHall", link.DepartureMap);
        game.Arrive("MistraltonCity_CargoHall", 14, 23);
        var attendant = game.Map.NPCs.Single(p => p.IsTransportAttendant);
        Assert.Contains(game.Play(Scripts.Find(FieldScripts.For(attendant)!, "common")!, attendant).Transcript, l => l.Text.Contains("Kalos"));
    }

    [Fact]
    public void TheLeaguesChapterIsHiddenUntilItsScenesAndItsSongsAreThere()
    {
        var game = new Game();
        foreach (string flag in new[] { "FLAG_UNOVA_LEAGUE_BEGUN", "FLAG_UNOVA_HIDE_HUGH_TUNNEL", "FLAG_UNOVA_HIDE_CEDRIC_HOME" })
            Assert.True(game.Story.Has(flag), flag);
        game.Story.Unset("FLAG_UNOVA_LEAGUE_BEGUN");
        game.Story.Unset("FLAG_UNOVA_HIDE_HUGH_TUNNEL");
        game.Play(Scripts.Find("UnovaCatchUpF", "common")!);
        Assert.True(game.Story.Has("FLAG_UNOVA_LEAGUE_BEGUN"));
        Assert.True(game.Story.Has("FLAG_UNOVA_HIDE_HUGH_TUNNEL"));
        foreach (string song in new[] { "route23", "victory_road", "pokemon_league", "battle_elite_four", "battle_champion", "credits" })
            Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "unova", song + ".mml"))), song);
        foreach (string map in new[] { "UnovaVictoryRoad", "UnovaVictoryRoad_Tunnel", "UnovaPokemonLeague", "UnovaPokemonLeague_Hall", "UnovaPokemonLeague_ChampionsRoom", "UnovaPokemonLeague_HallOfFame" })
            Assert.Contains(map, RegionDatabase.Get(RegionDatabase.Unova)!.Maps);
        foreach (string four in new[] { "Shauntal", "Marshal", "Grimsley", "Caitlin" })
            Assert.Equal("eliteFour", SoundMap.BattleThemes[$"Elite Four {four}"]);
    }

    // ---- Plan 22 · U10, part a: the legendaries of the open roads after the Hall of Fame ------------------------------------

    [Theory]
    [InlineData("Unova", 684, 191, "21", "unova_route_13", "Cobalion", 65)]
    [InlineData("Unova", 468, 171, "0", "unova_route_11", "Virizion", 65)]
    [InlineData("Unova", 725, 139, "6", "unova_route_22", "Terrakion", 65)]
    [InlineData("Unova", 584, 81, "16", "unova_route_23", "Azelf", 65)]
    [InlineData("CelestialTower_Summit", 16, 23, "0", "celestial_tower", "Mesprit", 65)]
    [InlineData("GiantChasm_Cavern", 15, 18, "kyurem_rest", "giant_chasm_6", "Kyurem", 70)]
    public void ALegendaryWaitsOnItsRoadOnceTheHallOfFameIsEnteredAndIsGoneOnceFought(string map, int x, int y, string key, string area, string species, int level)
    {
        var game = PastTheChasm();
        string fought = $"FLAG_UNOVA_FOUGHT_{species.ToUpperInvariant()}";
        game.Arrive(map, x, y);
        Assert.Null(game.Present(key, area));
        game.Story.Set("UnovaHallOfFame");
        game.Arrive(map, x, y);
        Assert.NotNull(game.Present(key, area));
        // run from, it is gone until the place is come to again
        game.Fight = BattleOutcome.Fled;
        var fled = game.Talk(key, area);
        Assert.Contains(fled.Log, l => l.StartsWith($"wildbattle {species} {level}"));
        Assert.False(game.Story.Has(fought));
        game.Arrive(map, x, y);
        Assert.NotNull(game.Present(key, area));
        // caught or beaten, it is gone for good
        game.Fight = BattleOutcome.Won;
        game.Talk(key, area);
        Assert.True(game.Story.Has(fought));
        game.Arrive(map, x, y);
        Assert.Null(game.Present(key, area));
    }

    [Fact]
    public void TheMagmaStoneSetDownInReversalMountainsLairBringsHeatran()
    {
        var game = PastTheChasm();
        game.Arrive("ReversalMountain_4", 10, 22);
        Assert.Null(game.Present("magma_stone", "reversal_mountain_5"));
        game.Story.Set("UnovaHallOfFame");
        game.Arrive("ReversalMountain_4", 10, 22);
        Assert.Null(game.Present("0", "reversal_mountain_5"));
        // the heart of the lair is only warm without the stone
        var lair = game.Map.TileScripts.Single(t => t.Value == "MagmaStone").Key;
        Assert.Contains(game.Play(Scripts.Find("MagmaStone", "reversal_mountain_5")!).Transcript, l => l.Text.Contains("warm"));
        // the stone's ball on the ring path, then the stone set down and Heatran fallen into the lair
        game.Talk("magma_stone", "reversal_mountain_5");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Magma Stone")!));
        game.Arrive("ReversalMountain_4", 10, 22);
        Assert.Null(game.Present("magma_stone", "reversal_mountain_5"));
        game.Answers.Add(0);
        game.Play(Scripts.Find("MagmaStone", "reversal_mountain_5")!);
        Assert.True(game.Story.Has("FLAG_UNOVA_MAGMA_STONE_SET"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Magma Stone")!));
        Assert.NotNull(game.Present("0", "reversal_mountain_5"));
        var heatran = game.Talk("0", "reversal_mountain_5");
        Assert.Contains(heatran.Log, l => l.StartsWith("wildbattle Heatran 68"));
        Assert.True(game.Story.Has("FLAG_UNOVA_FOUGHT_HEATRAN"));
        game.Arrive("ReversalMountain_4", 10, 22);
        Assert.Null(game.Present("0", "reversal_mountain_5"));
        Assert.Equal((10, 12), (lair.X, lair.Y));
    }

    // ---- Plan 22 · U10, part b: the caves of the old roads off the open ones ------------------------------------------------

    [Fact]
    public void ClayTunnelOpensFromDriftveilOnceTheHallOfFameIsEnteredAndItsWorkersBreakThroughWithRockSmash()
    {
        var game = PastTheChasm();
        game.Arrive("Unova", 167, 392);
        Assert.True(game.Map.IsSolid(167, 391));
        game.Story.Set("UnovaHallOfFame");
        game.Arrive("Unova", 167, 392);
        Assert.False(game.Map.IsSolid(167, 391));
        game.Through("ClayTunnel_1");
        Assert.Equal("ClayTunnel_1", game.Map.Name);
        // the heap of rock shuts the way to the second floor until the workers break it
        Assert.NotNull(game.Present("9", "clay_tunnel"));
        var refused = game.Talk("8", "clay_tunnel");
        Assert.False(game.Story.Has("FLAG_UNOVA_CLAY_TUNNEL_DUG"));
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("usemove"));
        var smasher = new Pokemon(PokemonDatabase.Get("Roggenrola")!, 40); smasher.ReplaceMove(0, "Rock Smash"); game.Party.RemoveAt(game.Party.Count - 1); game.Party.Add(smasher);
        var dug = game.Talk("8", "clay_tunnel");
        Assert.Contains(dug.Log, l => l.StartsWith("usemove Rock Smash"));
        Assert.True(game.Story.Has("FLAG_UNOVA_CLAY_TUNNEL_DUG"));
        game.Arrive("ClayTunnel_1", 5, 14);
        Assert.Null(game.Present("9", "clay_tunnel"));
        Assert.Null(game.Present("8", "clay_tunnel"));
        game.Arrive("ClayTunnel_2", 22, 46);
        Assert.Null(game.Present("14", "clay_tunnel_2"));
    }

    [Theory]
    [InlineData("ClayTunnel_1", "16", "clay_tunnel", "ClayTunnel_2")]
    [InlineData("ClayTunnel_1", "17", "clay_tunnel", "ClayTunnel_3")]
    [InlineData("ClayTunnel_2", "7", "clay_tunnel_2", "ClayTunnel_1")]
    [InlineData("ClayTunnel_3", "9", "clay_tunnel_3", "ClayTunnel_3")]
    public void AWorkerByAMiningCartRidesThePlayerToAnotherFloor(string map, string key, string area, string to)
    {
        var game = PastTheChasm();
        game.Story.Set("UnovaHallOfFame");
        var npc = game.Mapped(map).Everyone.First(n => n.Key == key && n.ScriptFile == area);
        game.Arrive(map, npc.GridX, npc.GridY + 1);
        game.Answers.Add(1);
        var no = game.Talk(key, area);
        Assert.DoesNotContain(no.Log, l => l.StartsWith("warp "));
        game.Answers.Add(0);
        game.Talk(key, area);
        Assert.Equal(to, game.Map.Name);
        Assert.True(game.Map.IsWalkable(game.Tile.X, game.Tile.Y), $"{to} {game.Tile}");
    }

    [Theory]
    [InlineData(TimeOfDay.Morning, "UndergroundRuins_Sun")]
    [InlineData(TimeOfDay.Day, "UndergroundRuins_Sun")]
    [InlineData(TimeOfDay.Twilight, "UndergroundRuins_Moon")]
    [InlineData(TimeOfDay.Night, "UndergroundRuins_Moon")]
    public void TheUndergroundRuinsDoorLeadsToTheHallOfTheSunOrTheMoonByTheTimeOfDay(TimeOfDay time, string hall)
    {
        var game = PastTheChasm();
        game.Time = time;
        game.Arrive("UndergroundRuins", 15, 3);
        game.Step("TheDoor");
        Assert.Equal(hall, game.Map.Name);
        Assert.True(game.Map.IsWalkable(15, 59));
        game.Step("BackToTheDoor");
        Assert.Equal(("UndergroundRuins", 15, 3), (game.Map.Name, game.Tile.X, game.Tile.Y));
    }

    [Fact]
    public void TheRuinsSwitchTurnsTheDoorToEachChamberOfTheRegisInTurnAfterTheHallOfFame()
    {
        var game = PastTheChasm();
        game.Arrive("UndergroundRuins", 24, 11);
        var lair = game.Map.TileScripts.First(t => t.Value == "Switch").Key;
        game.Answers.Add(0);
        game.Play(Scripts.Find("Switch", "underground_ruins")!);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_RUINS_DOOR"));
        game.Story.Set("UnovaHallOfFame");
        foreach (var (chamber, area, species) in new[] { ("RockPeakChamber", "rock_peak_chamber", "Regirock"), ("IronChamber", "iron_chamber", "Registeel"), ("IcebergChamber", "iceberg_chamber", "Regice") })
        {
            game.Arrive("UndergroundRuins", 24, 11);
            game.Answers.Add(0);
            game.Play(Scripts.Find("Switch", "underground_ruins")!);
            game.Arrive("UndergroundRuins", 15, 3);
            game.Step("TheDoor");
            Assert.Equal(chamber, game.Map.Name);
            Assert.True(game.Map.IsWalkable(15, 59));
            // run from, it is back the next time; caught or beaten, it is gone for good
            string fought = $"FLAG_UNOVA_FOUGHT_{species.ToUpperInvariant()}";
            game.Fight = BattleOutcome.Fled;
            var fled = game.Talk("0", area);
            Assert.Contains(fled.Log, l => l.StartsWith($"wildbattle {species} 65"));
            Assert.False(game.Story.Has(fought));
            game.Arrive(chamber, 15, 59);
            game.Fight = BattleOutcome.Won;
            game.Talk("0", area);
            Assert.True(game.Story.Has(fought));
            game.Arrive(chamber, 15, 59);
            Assert.Null(game.Present("0", area));
        }
        // a fourth step brings the halls back
        game.Arrive("UndergroundRuins", 24, 11);
        game.Answers.Add(0);
        game.Play(Scripts.Find("Switch", "underground_ruins")!);
        Assert.Equal(0, game.Story.Var("VAR_UNOVA_RUINS_DOOR"));
        Assert.Equal(24, lair.X);
    }

    [Fact]
    public void MistraltonCavesUpperFloorAndTheGuidanceChamberAreOpenAndTheWayToClayTunnelStaysShut()
    {
        var game = PastTheChasm();
        game.Arrive("MistraltonCave", 13, 17);
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap.StartsWith("ClayTunnel") && !w.Shut);
        game.Through("MistraltonCave_2F");
        game.Through("GuidanceChamber");
        Assert.Equal("GuidanceChamber", game.Map.Name);
        var painter = game.Talk("2", "guidance_chamber");
        Assert.False(game.Story.Has("FLAG_UNOVA_PAINTER_SAW_COBALION"));
        game.Party.RemoveAt(game.Party.Count - 1); game.Party.Add(new Pokemon(PokemonDatabase.Get("Cobalion")!, 65));
        game.Talk("2", "guidance_chamber");
        Assert.True(game.Story.Has("FLAG_UNOVA_PAINTER_SAW_COBALION"));
        Assert.NotEmpty(painter.Transcript);
    }
    private static string AreaAt(WorldMatrixFile matrix, int x, int y)
    {
        var cells = matrix.Areas[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return cells[x] == "-" ? "" : matrix.AreaKeys[int.Parse(cells[x])];
    }

    private static string SeasonCell(MatrixSeason season, int x, int y) => season.Chunks[y].Split(' ', StringSplitOptions.RemoveEmptyEntries)[x];
}
