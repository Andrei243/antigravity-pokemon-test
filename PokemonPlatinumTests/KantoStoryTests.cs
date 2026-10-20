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
/// Kanto's first chapter (plan 19 · K3, "Pallet to Pewter") played through with no screen, as a player meets it: the
/// bedroom, Oak stopping the player in the grass, the three Poké Balls on his table, the rival's first battle, Oak's
/// Parcel from Viridian's Mart, the Pokédex and five Poké Balls, the old man of Viridian's road north, the rival's
/// sister's Town Map, the rival on Route 22, the Gym's guide in Pewter City, Brock and the Boulder Badge, and the
/// Running Shoes. Each scene starts as the game starts it (a step onto its trigger, a door, someone spoken to), on
/// maps of this class's own.
/// </summary>
public class KantoStoryTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Kanto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Kanto));

    /// <summary>A game of Kanto of its own: its maps, story, team and bag, and the tile the player stands on.</summary>
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
        public int Caught { get; set; }
        /// <summary>Whether the Pokédex is the National one (<c>if nationaldex</c>), carried from one script to the next.</summary>
        public bool NationalDex { get; set; }
        public bool Impatient { get; init; }
        /// <summary>The species the Pokédex has seen (<c>random seen</c>, plan 19 · K9, part c: Selphy's wish).</summary>
        public List<string> Seen { get; } = new();
        /// <summary>How many teams have entered the Hall of Fame (<c>LEAGUE_VICTORIES</c>); -1 for the host's own count.</summary>
        public int Victories { get; set; } = -1;
        /// <summary>The script's dice to come (<c>random</c>) and the answers to its questions, handed on from one script to the next.</summary>
        public Queue<int> Rolls { get; } = new();
        public Queue<int> Answers { get; } = new();
        /// <summary>The team's place chosen when a script asks for one (<c>choosepokemon</c>); 255 backs out.</summary>
        public int PokemonChoice { get; set; }
        public List<(string Script, HeadlessScriptHost Host)> Played { get; } = new();
        public HeadlessScriptHost Last => Played[^1].Host;
        public const string Rival = "Gary";

        public Game(int starter = 0, BattleOutcome fight = BattleOutcome.Won, bool impatient = false)
        {
            Starter = starter;
            Fight = fight;
            Impatient = impatient;
            foreach (var map in Kanto.Value.BuildMaps()) maps[map.Name] = map;
            StoryMigration.BeginNewGame(Story, Scripts);
            var start = RegionDatabase.Get(RegionDatabase.Kanto)!.Start!;
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
            var warp = Map.Warps.Where(w => w.TargetMap.Equals(target, StringComparison.OrdinalIgnoreCase))
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

        public HeadlessScriptHost Talk(string key) => Talk(Map.NPCs.First(n => n.Key == key));

        /// <summary>Someone in particular, where one map has several of a key (the map of Kanto's people of many places).</summary>
        public HeadlessScriptHost Talk(NPC npc)
        {
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
                RivalName = Rival,
                Fight = _ => Fight,
                NeedsPokemon = true,
                Caught = Caught,
                NationalDex = NationalDex,
                PokemonChoice = PokemonChoice
            };
            host.PokedexSeen.AddRange(Seen);
            if (Victories >= 0) host.LeagueVictories = Victories;
            while (Rolls.Count > 0) host.Rolls.Enqueue(Rolls.Dequeue());
            while (Answers.Count > 0) host.Answers.Enqueue(Answers.Dequeue());
            var runner = new ScriptRunner(Scripts, host) { Impatient = Impatient };
            runner.Start(script, subject);
            runner.RunToEnd();
            while (host.Rolls.Count > 0) Rolls.Enqueue(host.Rolls.Dequeue());
            while (host.Answers.Count > 0) Answers.Enqueue(host.Answers.Dequeue());
            Played.Add((script.FullName, host));
            NationalDex = host.NationalDex;
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Log.LastOrDefault(l => l.StartsWith("warp ")) != null && host.Map is { } there)
            {
                Map = there;
                if (Scripts.In(Map.ScriptFileAt(Tile.X, Tile.Y), ScriptLibrary.OnEnter) is { } onEnter) Play(onEnter);
            }
            return host;
        }
    }

    [Theory]
    [InlineData(0, BattleOutcome.Won)]
    [InlineData(1, BattleOutcome.Lost)]
    [InlineData(2, BattleOutcome.Won)]
    public void TheChapterPlaysFromTheBedroomToTheBoulderBadgeAndTheRunningShoes(int starter, BattleOutcome fight)
    {
        var game = new Game(starter, fight);
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        string mine = kanto.Starters[starter];
        string rivals = kanto.RivalStarterFor(mine);

        // The bedroom, with nothing at all: no Pokémon, no Pokédex, no Running Shoes; Oak is out
        Assert.Equal("PalletTown_PlayersHouse_2F", game.Map.Name);
        Assert.Empty(game.Party.Members);
        Assert.Equal(0, game.Bag.AllItems.Count());
        Assert.False(game.Story.Has(StoryState.PokedexFlag));
        Assert.False(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.True(game.Story.Has("FLAG_HIDE_OAK_IN_HIS_LAB"));

        // Down and out, and north to the grass: Oak calls the player back and brings them to his lab, where he walks in
        // ahead and the rival is waiting
        game.Through("PalletTown_PlayersHouse_1F");
        game.Through("Kanto");
        Assert.True(game.Fires("OakStops"));
        game.Step("OakStops");
        Assert.Equal("PalletTown_ProfessorOaksLab", game.Map.Name);
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));
        Assert.NotNull(game.Present("oaks_lab_prof_oak"));
        Assert.NotNull(game.Present("oaks_lab_rival"));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == Game.Rival);
        // Walking off is turned back
        Assert.True(game.Fires("DontGoYet"));
        game.Step("DontGoYet");

        // A ball on the table: the player's choice joins the team, the rival takes the one strong against it, and both
        // of their balls are gone from the table
        game.Talk("bulbasaur_ball");
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        Assert.Equal(rivals, game.Story.RivalStarter);
        Assert.Single(new[] { "bulbasaur_ball", "charmander_ball", "squirtle_ball" }, b => game.Present(b) != null);
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));

        // On the way out the rival challenges, with FireRed's team for the one he took, and leaves win or lose
        var battle = game.Step("RivalChallenges");
        Assert.Contains($"battle kanto_rival_oaks_lab_{rivals.ToLowerInvariant()} first {fight}", battle.Log);
        Assert.Equal(rivals, Assert.Single(TrainerDatabase.Get($"kanto_rival_oaks_lab_{rivals.ToLowerInvariant()}")!.Party).Species);
        Assert.Null(game.Present("oaks_lab_rival"));
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));
        Assert.Equal(fight == BattleOutcome.Won, game.Story.Has("FLAG_BEAT_RIVAL_IN_OAKS_LAB"));

        // Viridian City: the old man lies across the road north; the Mart's clerk hands over the Parcel at once
        game.Through("Kanto");
        Assert.True(game.Fires("RoadBlocked"));
        Assert.False(game.Fires("Tutorial"));
        game.Through("ViridianCity_Mart");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Parcel")!));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_VIRIDIAN_CITY_MART"));
        Assert.DoesNotContain(game.Talk("viridian_mart_clerk").Log, l => l.StartsWith("open Shop"));

        // Back to the lab: the Parcel for the Pokédex and five Poké Balls, the rival in and out, and all it opens
        game.Through("Kanto");
        game.Through("PalletTown_ProfessorOaksLab");
        game.Talk("oaks_lab_prof_oak");
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Parcel")!));
        Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Null(game.Present("pokedex_1"));
        Assert.Null(game.Present("oaks_lab_rival"));
        Assert.Equal(6, game.Story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROUTE22"));

        // The rival's sister gives the Town Map from her table
        game.Through("Kanto");
        game.Through("PalletTown_RivalsHouse");
        game.Talk("daisy");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Town Map")!));
        Assert.True(game.Story.Has("FLAG_GOT_TOWN_MAP"));

        // Viridian again: the road north is open and the old man tells how to catch
        game.Through("Kanto");
        Assert.False(game.Fires("RoadBlocked"));
        game.Step("Tutorial");
        Assert.False(game.Fires("Tutorial"));

        // Route 22: the rival waits on the road west, battles with his Pidgey and his starter, and goes
        Assert.True(game.Fires("EarlyRival"));
        var road = game.Step("EarlyRival");
        Assert.Contains(road.Log, l => l.StartsWith($"battle kanto_rival_route22_early_{rivals.ToLowerInvariant()}"));
        Assert.False(game.Fires("EarlyRival"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_22_RIVAL"));

        // Pewter City: heading east without the Badge, the guide brings the player to the Gym's door
        var guided = game.Step("GymGuide");
        Assert.Equal((63, 77), guided.PlayerTile);
        Assert.Equal("PewterCity_Gym", game.Map.GetWarpAt(63, 76)?.TargetMap);
        Assert.False(game.Fires("RunningShoes"));

        // Brock (who must be beaten: a battle lost there wakes the player in the last Pokémon Center): the Boulder Badge,
        // TM39, his Camper counted beaten, and Flash's gate open
        game.Fight = BattleOutcome.Won;
        game.Through("PewterCity_Gym");
        var brock = game.Talk("brock");
        Assert.Contains(brock.Log, l => l.StartsWith("battle kanto_leader_brock"));
        Assert.True(game.Story.HasBadge("Kanto", "Boulder"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM39")!));
        Assert.True(game.Story.HasDefeated("kanto_camper_liam"));
        Assert.Equal("Boulder", FieldMoveRules.BadgeFor(FieldMove.Flash, "Kanto"));

        // Out again: the guide has gone home, and Oak's aide brings the Running Shoes
        game.Through("Kanto");
        Assert.Null(game.Present("pewter_gym_guide"));
        Assert.False(game.Fires("GymGuide"));
        game.Step("RunningShoes");
        Assert.True(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Null(game.Present("pewter_aide"));
        Assert.False(game.Fires("RunningShoes"));
    }

    [Fact]
    public void OaksAideOnRouteTwoGivesFlashForTenKindsCaught()
    {
        var game = new Game();
        game.Story.Set(StoryState.PokedexFlag);
        var aide = Kanto.Value.BuildMaps().Single(m => m.Name == "Route2_EastBuilding");
        game.Arrive("Route2_EastBuilding", 7, 9);
        game.Caught = 9;
        game.Talk("scientist");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("TM70")!));
        game.Caught = 10;
        game.Talk("scientist");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM70")!));
        Assert.True(game.Story.Has("FLAG_GOT_HM05"));
        Assert.NotNull(aide);
    }

    [Fact]
    public void TheMuseumAsksFiftyAtItsDeskAndTurnsBackWhoeverWontPay()
    {
        var game = new Game();
        game.Through("PalletTown_PlayersHouse_1F");
        game.Arrive("PewterCity_Museum_1F", 13, 8);
        Assert.True(game.Fires("Ticket"));
        game.Step("Ticket");
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_PEWTER_CITY_MUSEUM_1F"));
        Assert.False(game.Fires("Ticket"));
        // Pewter City sets it back on every visit
        game.Arrive("Kanto", 65, 67);
        Assert.Equal(0, game.Story.Var("VAR_MAP_SCENE_PEWTER_CITY_MUSEUM_1F"));
    }

    [Fact]
    public void KantosGatesAreItsOwnBadgesAndSinnohsAreUnchanged()
    {
        Assert.Equal("Boulder", FieldMoveRules.BadgeFor(FieldMove.Flash, "Kanto"));
        Assert.Equal("Cascade", FieldMoveRules.BadgeFor(FieldMove.Cut, "Kanto"));
        Assert.Equal("Thunder", FieldMoveRules.BadgeFor(FieldMove.Fly, "Kanto"));
        Assert.Equal("Rainbow", FieldMoveRules.BadgeFor(FieldMove.Strength, "Kanto"));
        Assert.Equal("Soul", FieldMoveRules.BadgeFor(FieldMove.Surf, "Kanto"));
        Assert.Equal("Marsh", FieldMoveRules.BadgeFor(FieldMove.RockSmash, "Kanto"));
        Assert.Equal("Volcano", FieldMoveRules.BadgeFor(FieldMove.Waterfall, "Kanto"));
        Assert.Equal("Forest", FieldMoveRules.BadgeFor(FieldMove.Cut, "Sinnoh"));
        Assert.Null(FieldMoveRules.BadgeFor(FieldMove.Flash, "Sinnoh"));
    }

    [Fact]
    public void KantosRolesWithNoSongOfTheirOwnPlayKantosAndSinnohsAreAsTheyWere()
    {
        // FireRed fights its rival and Team Rocket to the trainers' theme and the Elite Four to the Leaders'
        Assert.Equal("kanto/battle_trainer", MusicDirector.Resolve(MusicRole.BattleRival, "Kanto", MusicLibrary.Exists));
        Assert.Equal("kanto/battle_trainer", MusicDirector.Resolve(MusicRole.BattleGalactic, "Kanto", MusicLibrary.Exists));
        Assert.Equal("kanto/battle_gym", MusicDirector.Resolve(MusicRole.BattleEliteFour, "Kanto", MusicLibrary.Exists));
        Assert.Equal("kanto/victory_gym", MusicDirector.Resolve(MusicRole.VictoryEliteFour, "Kanto", MusicLibrary.Exists));
        Assert.Equal("kanto/rival", MusicDirector.Resolve(MusicRole.Rival, "Kanto", MusicLibrary.Exists));
        // Sinnoh's roles resolve to what they did
        Assert.Equal("common/battle_rival", MusicDirector.Resolve(MusicRole.BattleRival, "Sinnoh", MusicLibrary.Exists));
        Assert.Equal("common/battle_elite_four", MusicDirector.Resolve(MusicRole.BattleEliteFour, "Sinnoh", MusicLibrary.Exists));
        Assert.Equal("common/battle_trainer", MusicDirector.Resolve(MusicRole.BattleTrainer, "Sinnoh", MusicLibrary.Exists));
    }

    [Fact]
    public void KantosRivalTakesTheStarterStrongAgainstThePlayersAndIsCalledByHisName()
    {
        Assert.Equal("Charmander", StoryState.RivalStarterFor("Bulbasaur"));
        Assert.Equal("Squirtle", StoryState.RivalStarterFor("Charmander"));
        Assert.Equal("Bulbasaur", StoryState.RivalStarterFor("Squirtle"));
        Assert.Equal("Chimchar", StoryState.RivalStarterFor("Turtwig"));
        // FireRed's two classes of him are the rival's class: called {rival}, his music the rival's
        var trainer = new Trainer();
        TrainerDatabase.Fill(trainer, TrainerDatabase.Get("kanto_rival_oaks_lab_charmander")!);
        Assert.Equal("{rival}", trainer.Name);
        Assert.Equal(TrainerDatabase.RivalClass, trainer.TrainerClass);
    }

    // ------------------------------------------------------------------ plan 19 · K4: Mt. Moon to Bill

    /// <summary>A game past the first chapter, with a starter on its team, standing where the test puts it.</summary>
    private static Game PastPewter(int starter = 0)
    {
        var game = new Game(starter);
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter]);
        game.Party.Members.Add(new Pokemon(PokemonDatabase.Get(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter])!, 20));
        game.Story.Set(StoryState.PokedexFlag);
        return game;
    }

    [Theory]
    [InlineData("dome_fossil", "Dome Fossil", "helix_fossil", "FLAG_GOT_DOME_FOSSIL")]
    [InlineData("helix_fossil", "Helix Fossil", "dome_fossil", "FLAG_GOT_HELIX_FOSSIL")]
    public void MiguelBattlesForMtMoonsFossilsAndTakesTheOneThePlayerLeaves(string taken, string item, string left, string flag)
    {
        var game = PastPewter();
        game.Arrive("MtMoon_B2F", 15, 11);
        Assert.NotNull(game.Present("dome_fossil"));
        Assert.NotNull(game.Present("helix_fossil"));
        // Hands off before he is beaten
        game.Talk(taken);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get(item)!));
        // The step beside him starts his battle
        Assert.True(game.Fires("MiguelTrigger"));
        var battle = game.Step("MiguelTrigger");
        Assert.Contains(battle.Log, l => l.StartsWith("battle kanto_super_nerd_miguel"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_MT_MOON_B2F"));
        Assert.False(game.Fires("MiguelTrigger"));
        // One fossil for the player, the other for Miguel, and neither lies there any more, then or on the next visit
        game.Talk(taken);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(item)!));
        Assert.True(game.Story.Has(flag));
        Assert.True(game.Story.Has("FLAG_GOT_FOSSIL_FROM_MT_MOON"));
        Assert.Null(game.Present(taken));
        Assert.Null(game.Present(left));
        game.Arrive("MtMoon_B2F", 15, 11);
        Assert.Null(game.Present(taken));
        Assert.Null(game.Present(left));
    }

    [Theory]
    [InlineData(0, "RivalLeft")]
    [InlineData(1, "RivalMid")]
    [InlineData(2, "RivalRight")]
    public void TheRivalComesDownFromNuggetBridgeAndBattlesWithTheStarterStrongAgainstThePlayers(int starter, string trigger)
    {
        var game = PastPewter(starter);
        string rivals = StoryState.RivalStarterFor(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter]);
        game.Arrive("Kanto", 287, 52);
        Assert.Null(game.Present("cerulean_rival"));
        Assert.True(game.Fires(trigger));
        var scene = game.Step(trigger);
        Assert.Contains($"battle kanto_rival_cerulean_{rivals.ToLowerInvariant()} Won", scene.Log);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_CERULEAN_CITY_RIVAL"));
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_ROUTE22"));
        Assert.Null(game.Present("cerulean_rival"));
        Assert.False(game.Fires(trigger));
    }

    [Theory]
    [InlineData("RocketLeft")]
    [InlineData("RocketRight")]
    public void NuggetBridgesPrizeIsANuggetAndTeamRocketsOffer(string trigger)
    {
        var game = PastPewter();
        game.Arrive("Kanto", 287, 30);
        Assert.True(game.Fires(trigger));
        var scene = game.Step(trigger);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Nugget")!));
        Assert.Contains(scene.Log, l => l.StartsWith("battle kanto_team_rocket_grunt_6"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROUTE24"));
        Assert.False(game.Fires(trigger));
        // Spoken to again, he only talks
        Assert.DoesNotContain(game.Talk("route24_rocket").Log, l => l.StartsWith("battle"));
    }

    [Fact]
    public void BillIsSortedOutByTheCellSeparatorAndGivesTheTicketThatOpensCerulean()
    {
        var game = PastPewter();
        game.Arrive("Route25_SeaCottage", 7, 8);
        Assert.NotNull(game.Present("bill_clefairy"));
        Assert.Null(game.Present("bill_human"));
        var computer = game.Map.TileScripts.Single(kv => kv.Value == "Computer");
        // The PC does nothing yet; the Clefairy asks for help and steps into the teleporter
        game.Play(Scripts.Find("Computer", game.Map.ScriptFileAt(computer.Key.X, computer.Key.Y))!);
        Assert.False(game.Story.Has("FLAG_HELPED_BILL_IN_SEA_COTTAGE"));
        game.Talk("bill_clefairy");
        Assert.Null(game.Present("bill_clefairy"));
        // Now the cell separator brings Bill out as himself
        game.Play(Scripts.Find("Computer", game.Map.ScriptFileAt(computer.Key.X, computer.Key.Y))!);
        Assert.True(game.Story.Has("FLAG_HELPED_BILL_IN_SEA_COTTAGE"));
        Assert.NotNull(game.Present("bill_human"));
        game.Talk("bill_human");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("S.S. Ticket")!));
        Assert.True(game.Story.Has("FLAG_GOT_SS_TICKET"));
        Assert.True(game.Story.Has("FLAG_HIDE_NUGGET_BRIDGE_ROCKET"));
        // Come back, and he is himself still, with no Clefairy beside him
        game.Arrive("Route25_SeaCottage", 7, 8);
        Assert.NotNull(game.Present("bill_human"));
        Assert.Null(game.Present("bill_clefairy"));

        // Cerulean City: before the ticket the policeman keeps the burgled house's door; after it he steps aside, and
        // the thief in the yard behind hands back TM28 once beaten
        var before = PastPewter();
        before.Arrive("Kanto", 287, 60);
        Assert.Equal((294, 52), (before.Present("cerulean_policeman")!.GridX, before.Present("cerulean_policeman")!.GridY));
        game.Arrive("Kanto", 287, 60);
        Assert.Equal((295, 52), (game.Present("cerulean_policeman")!.GridX, game.Present("cerulean_policeman")!.GridY));
        Assert.True(game.Fires("GruntBottom"));
        var thief = game.Step("GruntBottom");
        Assert.Contains(thief.Log, l => l.StartsWith("battle kanto_team_rocket_grunt_5"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM28")!));
        Assert.Null(game.Present("cerulean_grunt"));
        Assert.False(game.Fires("GruntTop"));
    }

    [Fact]
    public void MistyGivesTheCascadeBadgeAndTmThreeAndCutsGateOpens()
    {
        var game = PastPewter();
        game.Arrive("CeruleanCity_Gym", 8, 16);
        var misty = game.Talk("misty");
        Assert.Contains(misty.Log, l => l.StartsWith("battle kanto_leader_misty"));
        Assert.True(game.Story.HasBadge("Kanto", "Cascade"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM03")!));
        Assert.True(game.Story.HasDefeated("kanto_picnicker_diana"));
        Assert.True(game.Story.HasDefeated("kanto_swimmer_male_luis"));
        Assert.Equal("Cascade", FieldMoveRules.BadgeFor(FieldMove.Cut, "Kanto"));
        // Spoken to again she battles no more
        Assert.DoesNotContain(game.Talk("misty").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM03")!));
    }

    [Fact]
    public void TheMagikarpSalesmanSellsOneForFiveHundred()
    {
        var game = PastPewter();
        game.Arrive("Route4_PokemonCenter_1F", 7, 7);
        game.Talk("balding_man");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Magikarp");
        Assert.True(game.Story.Has("FLAG_BOUGHT_MAGIKARP"));
        Assert.Equal(2500, game.Last.Money);
        int team = game.Party.Count;
        game.Talk("balding_man");
        Assert.Equal(team, game.Party.Count);
    }

    [Fact]
    public void RouteTwentyTwosGuardTurnsBackWhoeverHasNoBoulderBadge()
    {
        var game = PastPewter();
        game.Arrive("Route22_NorthEntrance", 7, 6);
        Assert.True(game.Fires("GuardTrigger"));
        var turned = game.Step("GuardTrigger");
        Assert.Equal((7, 3), turned.PlayerTile);
        Assert.Equal(0, game.Story.Var("VAR_MAP_SCENE_ROUTE23"));
        // With the Badge he lets the player by, and the step no longer stops anyone
        game.Story.SetBadges("Kanto", 1);
        game.Step("GuardTrigger");
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROUTE23"));
        Assert.False(game.Fires("GuardTrigger"));
    }

    [Fact]
    public void EveryBadgeOfKantoHasItsOwnColourAndMark()
    {
        // Style guide, "Badges": Kanto's eight each have a look of their own, all different; Sinnoh's keep the row's
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        Assert.Equal(8, kanto.BadgeNames.Count);
        foreach (string name in kanto.BadgeNames) Assert.True(PokemonPlatinumEngine.Graphics.BadgeLooks.ByName.ContainsKey(name), name);
        var looks = kanto.BadgeNames.Select(n => PokemonPlatinumEngine.Graphics.BadgeLooks.ByName[n]).ToList();
        Assert.Equal(8, looks.Select(l => l.Mark).Distinct().Count());
        Assert.Equal(8, looks.Select(l => (l.Color.R, l.Color.G, l.Color.B)).Distinct().Count());
        foreach (string name in RegionDatabase.Get(RegionDatabase.Sinnoh)!.BadgeNames)
            Assert.False(PokemonPlatinumEngine.Graphics.BadgeLooks.ByName.ContainsKey(name), name);
    }

    [Fact]
    public void ANewGameOfKantoHidesTheRivalAtNuggetBridgeAndBillAsHimself()
    {
        var game = new Game();
        Assert.True(game.Story.Has("FLAG_HIDE_CERULEAN_RIVAL"));
        Assert.True(game.Story.Has("FLAG_HIDE_BILL_HUMAN_SEA_COTTAGE"));
        // A save from before K4 has neither, and the places hide them themselves as they are come to
        game.Story.Unset("FLAG_HIDE_CERULEAN_RIVAL");
        game.Story.Unset("FLAG_HIDE_BILL_HUMAN_SEA_COTTAGE");
        game.Arrive("Kanto", 287, 60);
        Assert.True(game.Story.Has("FLAG_HIDE_CERULEAN_RIVAL"));
        Assert.Null(game.Present("cerulean_rival"));
        game.Arrive("Route25_SeaCottage", 7, 8);
        Assert.True(game.Story.Has("FLAG_HIDE_BILL_HUMAN_SEA_COTTAGE"));
        Assert.Null(game.Present("bill_human"));
    }

    [Fact]
    public void ASaveOfKantoFromBeforeTheChapterIsTakenPastItsOpening()
    {
        // A game begun in Kanto before K3 began in front of the house with a Bulbasaur and no starter recorded
        var story = new StoryState();
        StoryMigration.Upgrade(story, 19, new[] { new Pokemon(PokemonDatabase.Get("Bulbasaur")!, 5) }, Scripts);
        Assert.Equal("Bulbasaur", story.PlayerStarter);
        Assert.Equal(6, story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));
        Assert.True(story.Has(StoryState.PokedexFlag));
        Assert.True(story.Has("FLAG_HIDE_ROUTE_22_RIVAL"));
        // A game of Sinnoh before its briefcase is only given Kanto's flags
        var sinnoh = new StoryState();
        StoryMigration.Upgrade(sinnoh, 19, Array.Empty<Pokemon>(), Scripts);
        Assert.Null(sinnoh.PlayerStarter);
        Assert.Equal(0, sinnoh.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB"));
        Assert.True(sinnoh.Has("FLAG_HIDE_OAK_IN_PALLET_TOWN"));
    }

    // ------------------------------------------------------------------ plan 19 · K4, part b: Vermilion and the S.S. Anne

    [Fact]
    public void TheFanClubsChairmanGivesTheBikeVoucherAndTheBikeShopTakesItForABicycle()
    {
        var game = PastPewter();
        // Without the voucher a bicycle costs a million, which nobody has
        game.Arrive("CeruleanCity_BikeShop", 4, 6);
        game.Talk("man");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!));
        // The chairman, heard out, gives the voucher once
        game.Arrive("VermilionCity_PokemonFanClub", 5, 9);
        game.Talk("gentleman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bike Voucher")!));
        Assert.True(game.Story.Has("FLAG_GOT_BIKE_VOUCHER"));
        game.Talk("gentleman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bike Voucher")!));
        // Refused, he gives nothing
        var refused = PastPewter();
        refused.Arrive("VermilionCity_PokemonFanClub", 5, 9);
        refused.Answers.Enqueue(1);
        refused.Talk("gentleman");
        Assert.False(refused.Story.Has("FLAG_GOT_BIKE_VOUCHER"));
        // The shop takes the voucher for a Bicycle
        game.Arrive("CeruleanCity_BikeShop", 4, 6);
        game.Talk("man");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Bike Voucher")!));
        Assert.True(game.Story.Has("FLAG_GOT_BICYCLE"));
        game.Talk("man");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!));
    }

    [Fact]
    public void ThePiersSailorTurnsBackWhoeverHasNoTicketAndLetsTheTicketThrough()
    {
        var game = PastPewter();
        game.Arrive("Kanto", 287, 231);
        Assert.True(game.Fires("CheckTicket"));
        var turned = game.Step("CheckTicket");
        Assert.Equal((286, 232), turned.PlayerTile);
        Assert.Equal(0, game.Story.Var("VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER"));
        game.Story.Set("FLAG_GOT_SS_TICKET");
        game.Step("CheckTicket");
        Assert.Equal(1, game.Story.Var("VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER"));
        Assert.False(game.Fires("CheckTicket"));
        // Back on the quay the check starts again
        game.Step("ExitedTicketCheck");
        Assert.Equal(0, game.Story.Var("VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER"));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 31)]
    [InlineData(2, 32)]
    public void TheRivalStepsOutOfTheCaptainsDoorAndBattlesWithTheStarterStrongAgainstThePlayers(int starter, int column)
    {
        var game = PastPewter(starter);
        string rivals = StoryState.RivalStarterFor(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter]);
        game.Arrive("SSAnne_2F_Corridor", column, 8);
        Assert.Null(game.Present("ss_anne_rival"));
        var trigger = game.Map.Triggers.Single(t => t.Script == "Rival" && t.X == column);
        game.Tile = (trigger.X, trigger.Y);
        var scene = game.Play(Scripts.Find("Rival", trigger.ScriptFile!)!);
        Assert.Contains($"battle kanto_rival_ss_anne_{rivals.ToLowerInvariant()} Won", scene.Log);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_S_S_ANNE_2F_CORRIDOR"));
        Assert.Null(game.Present("ss_anne_rival"));
        Assert.False(game.Fires("Rival"));
    }

    [Fact]
    public void TheCaptainGivesCutAndTheShipSailsAsThePlayerComesOffHer()
    {
        var game = PastPewter();
        game.Story.Set("FLAG_GOT_SS_TICKET");
        // Before Cut, the gangway is only a gangway
        game.Arrive("SSAnne_Exterior", 32, 13);
        Assert.Equal("SSAnne_Exterior", game.Map.Name);
        game.Arrive("SSAnne_CaptainsOffice", 4, 6);
        game.Talk("ss_anne_captain");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        Assert.True(game.Story.Has("FLAG_GOT_HM01"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_VERMILION_CITY"));
        // Spoken to again, he gives nothing more
        game.Talk("ss_anne_captain");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        // Off the ship she sails, and the player is on the quay
        game.Arrive("SSAnne_Exterior", 32, 13);
        Assert.Equal("Kanto", game.Map.Name);
        Assert.Equal((287, 232), game.Tile);
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_VERMILION_CITY"));
        // From then on the pier's sailor turns everyone back
        var back = game.Step("CheckTicket");
        Assert.Equal((286, 232), back.PlayerTile);
        Assert.Equal(0, game.Story.Var("VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER"));
        // Cut, which the Cascade Badge opens, clears the tree before Lt. Surge's Gym
        Assert.Equal("Cascade", FieldMoveRules.BadgeFor(FieldMove.Cut, "Kanto"));
        var tree = game.Map.NPCs.Single(n => n.GridX == 283 && n.GridY == 224);
        Assert.Equal(PropType.CutTree, tree.Obstacle);
        game.Party.Members[0].Moves.Add(new Move(MoveDatabase.Get("Cut")!));
        // Without the Cascade Badge the tree only stands there; with it, Cut is asked and the tree goes
        Assert.Empty(game.Talk(tree).Asked);
        Assert.NotNull(game.Present(tree.Key));
        game.Story.SetBadges("Kanto", 0b11);
        var cut = game.Talk(tree);
        Assert.Single(cut.Asked);
        Assert.Contains(cut.Log, l => l.StartsWith("usemove Cut"));
        Assert.True(game.Story.Has(tree.HiddenBy!));
    }

    [Fact]
    public void LtSurgesBinsHideTwoSwitchesSideBySideAndAWrongBinResetsThem()
    {
        var game = PastPewter();
        // Coming in, the first switch is drawn (7, the second bin of the middle row) and the second beside it (8)
        game.Rolls.Enqueue(6);
        game.Rolls.Enqueue(1);
        game.Arrive("VermilionCity_Gym", 5, 18);
        Assert.Equal(7, game.Story.Var("VAR_VERMILION_GYM_SWITCH_1"));
        Assert.Equal(8, game.Story.Var("VAR_VERMILION_GYM_SWITCH_2"));
        Assert.Equal(6, game.Map.NPCs.Count(n => n.GymThing == PropType.ElectricGate));
        Assert.False(game.Map.IsWalkable(5, 7) && game.Map.NpcIn(5, 7, 0) == null);
        HeadlessScriptHost Can(int n)
        {
            var tile = game.Map.TileScripts.Single(kv => kv.Value == $"Can{n}").Key;
            game.Tile = (tile.X, tile.Y + 1);
            return game.Play(Scripts.Find($"Can{n}", game.Map.ScriptFileAt(tile.X, tile.Y))!);
        }
        Can(3);
        Assert.False(game.Story.Has("FLAG_MAP_LOCAL_VERMILION_GYM_FIRST_SWITCH"));
        Can(7);
        Assert.True(game.Story.Has("FLAG_MAP_LOCAL_VERMILION_GYM_FIRST_SWITCH"));
        // A wrong bin after the first resets both and draws them again (1, and 2 beside it)
        game.Rolls.Enqueue(0);
        game.Rolls.Enqueue(0);
        Can(9);
        Assert.False(game.Story.Has("FLAG_MAP_LOCAL_VERMILION_GYM_FIRST_SWITCH"));
        Assert.Equal(1, game.Story.Var("VAR_VERMILION_GYM_SWITCH_1"));
        Assert.Equal(2, game.Story.Var("VAR_VERMILION_GYM_SWITCH_2"));
        Can(1);
        Can(2);
        Assert.True(game.Story.Has("FLAG_FOUND_BOTH_VERMILION_GYM_SWITCHES"));
        Assert.Equal(0, game.Map.NPCs.Count(n => n.GymThing == PropType.ElectricGate));
        Assert.True(game.Map.IsWalkable(5, 7));
        Assert.True(game.Map.IsWalkable(5, 6));
        // Lt. Surge, for the Thunder Badge, TM34, his three trainers, and Oak's aide in the city for a player without Flash
        var surge = game.Talk("lt_surge");
        Assert.Contains(surge.Log, l => l.StartsWith("battle kanto_leader_lt_surge"));
        Assert.True(game.Story.HasBadge("Kanto", "Thunder"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM34")!));
        foreach (string trainer in new[] { "kanto_engineer_baily", "kanto_sailor_dwayne", "kanto_gentleman_tucker" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);
        Assert.False(game.Story.Has("FLAG_HIDE_VERMILION_CITY_OAKS_AIDE"));
        Assert.DoesNotContain(game.Talk("lt_surge").Log, l => l.StartsWith("battle"));
        // Come in again: the gates stay down, and no switch is drawn
        game.Arrive("VermilionCity_Gym", 5, 18);
        Assert.Equal(0, game.Map.NPCs.Count(n => n.GymThing == PropType.ElectricGate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TheFishingGuruGivesTheOldRodToWhoeverLikesToFish(int answer)
    {
        var game = PastPewter();
        game.Arrive("VermilionCity_House1", 4, 6);
        game.Answers.Enqueue(answer);
        game.Talk("fisher");
        Assert.Equal(answer == 0 ? 1 : 0, game.Bag.GetQuantity(ItemDatabase.Get("Old Rod")!));
        Assert.Equal(answer == 0, game.Story.Has("FLAG_GOT_OLD_ROD"));
    }

    [Fact]
    public void ThingsLaidOutToBeTakenHaveTheirOwnLooks()
    {
        var game = PastPewter();
        game.Arrive("MtMoon_B2F", 15, 11);
        Assert.Equal(PropType.DomeFossil, game.Present("dome_fossil")!.GymThing);
        Assert.Equal(PropType.HelixFossil, game.Present("helix_fossil")!.GymThing);
        // The Old Amber on its stand, given by the scientist beside it
        game.Arrive("PewterCity_Museum_1F", 21, 5);
        Assert.Equal(PropType.OldAmber, game.Present("old_amber")!.GymThing);
        game.Talk("scientist");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Old Amber")!));
        Assert.True(game.Story.Has("FLAG_GOT_OLD_AMBER"));
        Assert.Null(game.Present("old_amber"));
        // A new game's Pokédexes on Oak's desk and Town Map on the sister's table
        var start = new Game();
        start.Arrive("PalletTown_ProfessorOaksLab", 6, 10);
        Assert.Equal(PropType.DeskPokedex, start.Present("pokedex_1")!.GymThing);
        Assert.Equal(PropType.DeskPokedex, start.Present("pokedex_2")!.GymThing);
        start.Arrive("PalletTown_RivalsHouse", 6, 7);
        Assert.Equal(PropType.RolledTownMap, start.Present("town_map")!.GymThing);
        // Each is a card of its own, none the item's ball they lay as before
        static string Look(PropType kind)
        {
            var art = PokemonPlatinumEngine.Graphics.ThingCards.Paint(kind);
            var sb = new System.Text.StringBuilder($"{art.Width}x{art.Height}:");
            for (int y = 0; y < art.Height; y++)
                for (int x = 0; x < art.Width; x++) { var c = art.Get(x, y); sb.Append(c.R).Append(c.G).Append(c.B).Append(c.A); }
            return sb.ToString();
        }
        var looks = new[] { PropType.Mailbox, PropType.ElectricGate, PropType.DomeFossil, PropType.HelixFossil, PropType.OldAmber, PropType.DeskPokedex, PropType.RolledTownMap }
            .Select(Look).ToList();
        Assert.Equal(looks.Count, looks.Distinct().Count());
    }

    [Fact]
    public void ANewGameOfKantoHidesTheShipsRivalAndVermilionsAideAndAnOlderSaveHasThePlacesHideThem()
    {
        var game = new Game();
        Assert.True(game.Story.Has("FLAG_HIDE_SS_ANNE_RIVAL"));
        Assert.True(game.Story.Has("FLAG_HIDE_VERMILION_CITY_OAKS_AIDE"));
        game.Story.Unset("FLAG_HIDE_SS_ANNE_RIVAL");
        game.Story.Unset("FLAG_HIDE_VERMILION_CITY_OAKS_AIDE");
        game.Arrive("SSAnne_2F_Corridor", 30, 8);
        Assert.Null(game.Present("ss_anne_rival"));
        game.Arrive("Kanto", 287, 220);
        // Vermilion's own: Route 8 has a Super Nerd of the same key (plan 19 · K5)
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "scientist" && game.Map.AreaAt(n.GridX, n.GridY)?.Key == "vermilion_city");
    }

    // ------------------------------------------------------------------ plan 19 · K5, part a: Rock Tunnel to Lavender

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    public void TheRivalBattlesInThePokemonTowerAndWalksOffDownTheStairs(int starter, int trigger)
    {
        var game = PastPewter(starter);
        string rivals = StoryState.RivalStarterFor(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter]);
        game.Arrive("PokemonTower_2F", 18, 10);
        Assert.NotNull(game.Present("pokemon_tower_rival"));
        var step = game.Map.Triggers.Where(t => t.Script == "Rival").OrderBy(t => t.Y).ElementAt(trigger);
        game.Tile = (step.X, step.Y);
        var scene = game.Play(Scripts.Find("Rival", step.ScriptFile!)!);
        Assert.Contains($"battle kanto_rival_pokemon_tower_{rivals.ToLowerInvariant()} Won", scene.Log);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_POKEMON_TOWER_2F"));
        Assert.True(game.Story.Has("FLAG_HIDE_TOWER_RIVAL"));
        Assert.Null(game.Present("pokemon_tower_rival"));
        Assert.False(game.Fires("Rival"));
        game.Arrive("PokemonTower_2F", 18, 10);
        Assert.Null(game.Present("pokemon_tower_rival"));
    }

    [Fact]
    public void ThePurifiedGroundOfTheFifthFloorHealsTheTeamOncePerStepIn()
    {
        var game = PastPewter();
        game.Arrive("PokemonTower_5F", 18, 10);
        game.Party.Members[0].CurrentHP = 1;
        game.Step("PurifiedZone");
        Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
        Assert.Equal(1, game.Story.Var("VAR_TEMP_1"));
        Assert.False(game.Fires("PurifiedZone"));
        game.Step("ExitPurifiedZone");
        Assert.True(game.Fires("PurifiedZone"));
        // Come in again from below, and the floor forgets the step in
        game.Story.SetVar("VAR_TEMP_1", 1);
        game.Arrive("PokemonTower_5F", 18, 10);
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
    }

    [Fact]
    public void TheTowersSixthFloorIsBarredWithoutTheSilphScope()
    {
        var game = PastPewter();
        game.Arrive("PokemonTower_6F", 18, 10);
        foreach (var step in game.Map.Triggers.Where(t => t.Script == "MarowakGhost").ToList())
        {
            game.Tile = (step.X, step.Y);
            var ghost = game.Play(Scripts.Find("MarowakGhost", step.ScriptFile!)!);
            Assert.DoesNotContain(ghost.Log, l => l.StartsWith("battle") || l.StartsWith("wildbattle"));
            Assert.Equal((step.X, step.Y - 1), game.Tile);
            Assert.Equal(0, game.Story.Var("VAR_MAP_SCENE_POKEMON_TOWER_6F"));
        }
        // and Mr. Fuji is up there with Team Rocket, not at home
        game.Arrive("LavenderTown_VolunteerPokemonHouse", 4, 6);
        Assert.Null(game.Present("mr_fuji"));
        Assert.True(game.Story.Has("FLAG_HIDE_POKEHOUSE_FUJI"));
        game.Arrive("PokemonTower_7F", 11, 15);
        Assert.NotNull(game.Present("mr_fuji"));
        Assert.Equal(3, game.Map.NPCs.Count(n => n.IsTrainer));
    }

    [Theory]
    [InlineData("Route7_EastEntrance", -1)]
    [InlineData("Route8_WestEntrance", 1)]
    public void SaffronsGateGuardsAreThirstyAndTurnEveryoneBack(string gate, int back)
    {
        var game = PastPewter();
        game.Arrive(gate, 6, 5);
        foreach (var step in game.Map.Triggers.Where(t => t.Script == "Guard").ToList())
        {
            game.Tile = (step.X, step.Y);
            var turned = game.Play(Scripts.Find("Guard", step.ScriptFile!)!);
            Assert.Equal((step.X + back, step.Y), game.Tile);
            Assert.Single(turned.Transcript);
        }
        Assert.Equal(3, game.Map.Triggers.Count(t => t.Script == "Guard"));
    }

    [Theory]
    [InlineData("black_belt", "Mega Punch", 1, true)]
    [InlineData("black_belt_2", "Mega Kick", 1, true)]
    [InlineData("black_belt", "Mega Punch", 0, false)]
    public void RouteFoursBlackBeltsTeachTheirMoveOnceToAPokemonThatCanLearnIt(string who, string move, int starter, bool able)
    {
        // Charmander can learn Focus Punch from TM01, so a punch or a kick; Bulbasaur can't
        var game = PastPewter(starter);
        game.Arrive("Kanto", 204, 54);
        var black = game.Map.NPCs.Single(n => n.Key == who && game.Map.AreaAt(n.GridX, n.GridY)?.Key == "route_4");
        game.Answers.Enqueue(0);
        var taught = game.Talk(black);
        Assert.Equal(able, game.Party.Members[0].Knows(move));
        Assert.Equal(able, game.Story.Has(MoveTutors.KantoFlag(move)));
        if (!able) return;
        Assert.Contains(taught.Transcript, l => l.Text.Contains($"learned {move}"));
        // Once only: spoken to again, nobody is asked for
        var again = game.Talk(black);
        Assert.Empty(again.Asked);
        Assert.Equal("FLAG_TUTOR_MEGA_PUNCH", MoveTutors.KantoFlag("Mega Punch"));
    }

    [Fact]
    public void RockTunnelsYoungsterTeachesRockSlideAndKantosTutorsKnowWhoCanLearn()
    {
        var game = PastPewter();
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Geodude")!, 20));
        game.Arrive("RockTunnel_B1F", 3, 29);
        game.Answers.Enqueue(0);
        game.Talk("youngster");
        Assert.True(game.Party.Members[0].Knows("Rock Slide"));
        Assert.True(game.Story.Has("FLAG_TUTOR_ROCK_SLIDE"));
        Assert.Equal(TutorAnswer.NotAble, MoveTutors.ForKanto(new Pokemon(PokemonDatabase.Get("Caterpie")!, 10), "Mega Kick"));
        Assert.Equal(TutorAnswer.Able, MoveTutors.ForKanto(new Pokemon(PokemonDatabase.Get("Machop")!, 10), "Mega Kick"));
        Assert.Equal(TutorAnswer.Nobody, MoveTutors.ForKanto(null, "Mega Punch"));
    }

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    public void RouteTensAideGivesAnEverstoneForTwentyKindsCaught(int caught, bool given)
    {
        var game = PastPewter();
        game.Caught = caught;
        game.Arrive("Route10_PokemonCenter_1F", 7, 7);
        game.Answers.Enqueue(0);
        game.Talk("scientist");
        Assert.Equal(given ? 1 : 0, game.Bag.GetQuantity(ItemDatabase.Get("Everstone")!));
        Assert.Equal(given, game.Story.Has("FLAG_GOT_EVERSTONE_FROM_OAKS_AIDE"));
    }

    // ---------------------------------------------------------------- plan 19 · K5, part b

    private int Count(Game game, string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);

    [Theory]
    [InlineData("Route5_SouthEntrance", 0, -1)]
    [InlineData("Route6_NorthEntrance", 0, 1)]
    public void TheGatesOfRoutesFiveAndSixAreThirstyToo(string gate, int dx, int dy)
    {
        var game = PastPewter();
        game.Arrive(gate, 4, 5);
        foreach (var step in game.Map.Triggers.Where(t => t.Script == "Guard").ToList())
        {
            game.Tile = (step.X, step.Y);
            game.Play(Scripts.Find("Guard", step.ScriptFile!)!);
            Assert.Equal((step.X + dx, step.Y + dy), game.Tile);
        }
    }

    [Theory]
    [InlineData("Route5_SouthEntrance")]
    [InlineData("Route6_NorthEntrance")]
    [InlineData("Route7_EastEntrance")]
    [InlineData("Route8_WestEntrance")]
    public void CeladonsTeaOpensEverySaffronGate(string gate)
    {
        var game = PastPewter();
        game.Arrive("CeladonCity_Condominiums_1F", 2, 10);
        game.Talk("old_woman");
        Assert.Equal(1, Count(game, "Tea"));
        Assert.True(game.Story.Has("FLAG_GOT_TEA"));
        game.Talk("old_woman");
        Assert.Equal(1, Count(game, "Tea"));
        game.Arrive(gate, 4, 5);
        var step = game.Map.Triggers.First(t => t.Script == "Guard");
        game.Tile = (step.X, step.Y);
        game.Play(Scripts.Find("Guard", step.ScriptFile!)!);
        Assert.Equal((step.X, step.Y), game.Tile);
        Assert.Equal(0, Count(game, "Tea"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROUTE5_ROUTE6_ROUTE7_ROUTE8_GATES"));
        // and the four gates' steps are dead from then on
        foreach (string other in new[] { "Route5_SouthEntrance", "Route6_NorthEntrance", "Route7_EastEntrance", "Route8_WestEntrance" })
        {
            game.Arrive(other, 4, 5);
            Assert.False(game.Fires("Guard"));
        }
    }

    [Fact]
    public void ErikaGivesTheRainbowBadgeAndTmNineteenAndStrengthsGateOpens()
    {
        var game = PastPewter();
        game.Arrive("CeladonCity_Gym", 6, 16);
        var erika = game.Talk("erika");
        Assert.Contains(erika.Log, l => l.StartsWith("battle kanto_leader_erika Won"));
        Assert.True(game.Story.HasBadge("Kanto", "Rainbow"));
        Assert.Equal(1, Count(game, "TM19"));
        foreach (string id in new[] { "kanto_lass_kay", "kanto_lass_lisa", "kanto_picnicker_tina", "kanto_beauty_bridget", "kanto_beauty_tamia", "kanto_beauty_lori", "kanto_cooltrainer_mary" })
            Assert.True(game.Story.HasDefeated(id), id);
        Assert.Equal("Rainbow", FieldMoveRules.BadgeFor(FieldMove.Strength, "Kanto"));
        Assert.DoesNotContain(game.Talk("erika").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, Count(game, "TM19"));
    }

    [Fact]
    public void TheGameCornersGruntGuardsThePosterAndItsSwitchOpensTheStairsDown()
    {
        var game = PastPewter();
        game.Arrive("CeladonCity_GameCorner", 10, 12);
        var stairs = game.Map.Warps.Single(w => w.TargetMap == "RocketHideout_B1F");
        Assert.True(stairs.Shut);
        // The poster while he guards it
        game.Tile = (11, 2);
        var read = game.Play(Scripts.Find("Poster", "celadon_city_game_corner")!);
        Assert.False(game.Story.Has("FLAG_OPENED_ROCKET_HIDEOUT"));
        var grunt = game.Talk("game_corner_grunt");
        Assert.Contains(grunt.Log, l => l.StartsWith("battle kanto_team_rocket_grunt_7 Won"));
        Assert.True(game.Story.Has("FLAG_HIDE_GAME_CORNER_ROCKET"));
        Assert.Null(game.Present("game_corner_grunt"));
        game.Answers.Enqueue(0);
        game.Tile = (11, 2);
        game.Play(Scripts.Find("Poster", "celadon_city_game_corner")!);
        Assert.True(game.Story.Has("FLAG_OPENED_ROCKET_HIDEOUT"));
        Assert.False(stairs.Shut);
        // The restaurant's man gives his Coin Case, once
        game.Arrive("CeladonCity_Restaurant", 2, 4);
        game.Talk("balding_man");
        game.Talk("balding_man");
        Assert.Equal(1, Count(game, "Coin Case"));
    }

    [Fact]
    public void TheHideoutsLiftKeyBarrierAndGiovanniGiveUpTheSilphScope()
    {
        var game = PastPewter();
        game.Arrive("RocketHideout_B4F", 11, 16);
        game.Talk("silph_scope");
        Assert.Equal(0, Count(game, "Silph Scope"));
        Assert.Equal(4, game.Map.NPCs.Count(n => n.NpcType == NPC.ElectricGateType));
        // The key lies beside its grunt, who won't let it go until beaten
        game.Talk("lift_key");
        Assert.Equal(0, Count(game, "Lift Key"));
        game.Talk("rocket_m");
        Assert.True(game.Story.Has("FLAG_ROCKET_HIDEOUT_LIFT_KEY_DROPPED"));
        game.Talk("lift_key");
        Assert.Equal(1, Count(game, "Lift Key"));
        Assert.True(game.Story.Has("FLAG_CAN_USE_ROCKET_HIDEOUT_LIFT"));
        // The barrier holds until both its grunts are beaten
        game.Talk("rocket_m_2");
        Assert.False(game.Story.Has("FLAG_ROCKET_HIDEOUT_B4F_BARRIER_OPEN"));
        game.Talk("rocket_m_3");
        Assert.True(game.Story.Has("FLAG_ROCKET_HIDEOUT_B4F_BARRIER_OPEN"));
        Assert.Equal(0, game.Map.NPCs.Count(n => n.NpcType == NPC.ElectricGateType));
        var boss = game.Talk("hideout_giovanni");
        Assert.Contains(boss.Log, l => l.StartsWith("battle kanto_boss_giovanni Won"));
        Assert.True(game.Story.Has("FLAG_HIDE_CELADON_ROCKETS"));
        Assert.Null(game.Present("hideout_giovanni"));
        Assert.NotNull(game.Present("silph_scope"));
        game.Talk("silph_scope");
        Assert.Equal(1, Count(game, "Silph Scope"));
        Assert.Null(game.Present("silph_scope"));
    }

    [Fact]
    public void TheHideoutsLiftTakesNobodyWithoutItsKeyAndRidesWithIt()
    {
        var game = PastPewter();
        game.Arrive("RocketHideout_Elevator", 1, 3);
        game.Tile = (0, 3);
        var panel = game.Play(Scripts.Find("Panel", "rocket_hideout_elevator")!);
        Assert.DoesNotContain(panel.Log, l => l.StartsWith("warp"));
        game.Story.Set("FLAG_CAN_USE_ROCKET_HIDEOUT_LIFT");
        game.Answers.Enqueue(2);
        panel = game.Play(Scripts.Find("Panel", "rocket_hideout_elevator")!);
        Assert.Contains("warp RocketHideout_B4F 20 24", panel.Log);
        Assert.Equal(4, game.Story.Var("VAR_ROCKET_HIDEOUT_LIFT_FLOOR"));
    }

    [Theory]
    [InlineData(BattleOutcome.Won, 1)]
    [InlineData(BattleOutcome.Fled, 0)]
    public void WithTheSilphScopeTheGhostIsMarowakAndBeatenItRests(BattleOutcome outcome, int scene)
    {
        var game = PastPewter();
        game.Bag.AddItem(ItemDatabase.Get("Silph Scope")!, 1);
        game.Arrive("PokemonTower_6F", 18, 10);
        game.Fight = outcome;
        var step = game.Map.Triggers.First(t => t.Script == "MarowakGhost");
        game.Tile = (step.X, step.Y);
        var ghost = game.Play(Scripts.Find("MarowakGhost", step.ScriptFile!)!);
        Assert.Contains(ghost.Log, l => l.StartsWith("wildbattle Marowak 30"));
        Assert.Equal(scene, game.Story.Var("VAR_MAP_SCENE_POKEMON_TOWER_6F"));
        Assert.Equal(scene == 1 ? (step.X, step.Y) : (step.X, step.Y - 1), game.Tile);
        Assert.Equal(scene == 0, game.Fires("MarowakGhost"));
    }

    [Fact]
    public void MrFujiIsRescuedFromTheTowerTopAndGivesThePokeFluteAtHome()
    {
        var game = PastPewter();
        game.Arrive("PokemonTower_7F", 11, 15);
        foreach (var (key, flag) in new[] { ("pokemon_tower_grunt1", "FLAG_HIDE_TOWER_ROCKET_1"), ("pokemon_tower_grunt2", "FLAG_HIDE_TOWER_ROCKET_2"), ("pokemon_tower_grunt3", "FLAG_HIDE_TOWER_ROCKET_3") })
        {
            game.Talk(key);
            Assert.True(game.Story.Has(flag));
            Assert.Null(game.Present(key));
        }
        var fuji = game.Talk("mr_fuji");
        Assert.Contains("warp LavenderTown_VolunteerPokemonHouse 4 6", fuji.Log);
        Assert.True(game.Story.Has("FLAG_RESCUED_MR_FUJI"));
        Assert.Equal("LavenderTown_VolunteerPokemonHouse", game.Map.Name);
        Assert.NotNull(game.Present("mr_fuji"));
        game.Talk("mr_fuji");
        Assert.Equal(1, Count(game, "Poké Flute"));
        game.Talk("mr_fuji");
        Assert.Equal(1, Count(game, "Poké Flute"));
        game.Arrive("PokemonTower_7F", 11, 15);
        Assert.Null(game.Present("mr_fuji"));
    }

    [Theory]
    [InlineData("route_12", "FLAG_HIDE_ROUTE_12_SNORLAX", 398, 221)]
    [InlineData("route_16", "FLAG_HIDE_ROUTE_16_SNORLAX", 163, 144)]
    public void TheSnorlaxSleepsUntilThePokeFluteWakesIt(string area, string flag, int x, int y)
    {
        var game = PastPewter();
        game.Arrive("Kanto", x, y);
        var snorlax = game.Map.NPCs.Single(n => n.Key == "snorlax" && game.Map.AreaAt(n.GridX, n.GridY)?.Key == area);
        var asleep = game.Talk(snorlax);
        Assert.DoesNotContain(asleep.Log, l => l.StartsWith("wildbattle"));
        Assert.False(game.Story.Has(flag));
        game.Bag.AddItem(ItemDatabase.Get("Poké Flute")!, 1);
        game.Answers.Enqueue(0);
        var woken = game.Talk(snorlax);
        Assert.Contains(woken.Log, l => l.StartsWith("wildbattle Snorlax 30"));
        Assert.True(game.Story.Has(flag));
        Assert.DoesNotContain(game.Map.NPCs, n => n == snorlax);
    }

    [Fact]
    public void RouteSixteensHouseGivesFlyAndKantosTownsAreOnItsMap()
    {
        var game = PastPewter();
        game.Arrive("Route16_House", 4, 5);
        game.Talk("woman_1");
        game.Talk("woman_1");
        Assert.Equal(1, Count(game, "HM02"));
        Assert.True(game.Story.Has("FLAG_GOT_HM02"));
        Assert.Equal("Thunder", FieldMoveRules.BadgeFor(FieldMove.Fly, "Kanto"));
        var map = RegionMap.Of("Kanto")!;
        foreach (string town in new[] { "pallet_town", "viridian_city", "pewter_city", "cerulean_city", "vermilion_city", "lavender_town", "celadon_city", "saffron_city" })
            Assert.Contains(map.Blocks, b => b.Area.Key == town && b.Kind == RegionBlockKind.Town);
        Assert.True(map.FlySpots.Count >= 11);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheRivalWaitsInSilphCoAndBattlesWithTheStarterStrongAgainstThePlayers(int starter)
    {
        var game = PastPewter(starter);
        string rivals = StoryState.RivalStarterFor(RegionDatabase.Get(RegionDatabase.Kanto)!.Starters[starter]);
        game.Arrive("SilphCo_7F", 23, 4);
        Assert.NotNull(game.Present("silph_co_rival"));
        var scene = game.Step("RivalTrigger");
        Assert.Contains($"battle kanto_rival_silph_{rivals.ToLowerInvariant()} Won", scene.Log);
        Assert.True(game.Story.Has("FLAG_HIDE_SILPH_RIVAL"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_SILPH_CO_7F"));
        Assert.False(game.Fires("RivalTrigger"));
        game.Talk("worker_m");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Lapras");
        Assert.True(game.Story.Has("FLAG_GOT_LAPRAS_FROM_SILPH"));
    }

    [Fact]
    public void SilphCosDoorsOpenToTheCardKeyAndGiovanniFreesSaffronForTheMasterBall()
    {
        var game = PastPewter();
        game.Arrive("SilphCo_5F", 22, 4);
        var door = game.Map.NPCs.First(n => n.Key == "door1_00");
        game.Talk(door);
        Assert.False(game.Story.Has("FLAG_SILPH_5F_DOOR_1"));
        game.Bag.AddItem(ItemDatabase.Get("Card Key")!, 1);
        game.Talk(door);
        Assert.True(game.Story.Has("FLAG_SILPH_5F_DOOR_1"));
        Assert.Null(game.Present("door1_00"));
        // Every floor's doors are its things, gone with their flags
        game.Arrive("SilphCo_9F", 24, 4);
        Assert.Equal(16, game.Map.NPCs.Count(n => n.NpcType == NPC.GalacticDoorType));
        // Giovanni at the top
        game.Arrive("SilphCo_11F", 13, 4);
        var boss = game.Step("Giovanni");
        Assert.Contains(boss.Log, l => l.StartsWith("battle kanto_boss_giovanni_2 Won"));
        Assert.True(game.Story.Has("FLAG_HIDE_SAFFRON_ROCKETS"));
        Assert.False(game.Story.Has("FLAG_HIDE_SAFFRON_CIVILIANS"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_SILPH_CO_11F"));
        Assert.Null(game.Present("silph_co_giovanni"));
        game.Talk("old_man_2");
        game.Talk("old_man_2");
        Assert.Equal(1, Count(game, "Master Ball"));
        // The Gym's door opens with Team Rocket gone
        game.Arrive("Kanto", 300, 127);
        Assert.False(game.Map.IsSolid(300, 125));
    }

    [Fact]
    public void SaffronHoldsItsPeopleIndoorsAndItsGymShutWhileTeamRocketHoldsTheCity()
    {
        var game = PastPewter();
        game.Arrive("Kanto", 288, 150);
        Assert.True(game.Story.Has("FLAG_HIDE_SAFFRON_CIVILIANS"));
        Assert.True(game.Map.IsSolid(300, 125));
        var guard = game.Map.NPCs.Single(n => n.Key == "saffron_door_guard_grunt");
        Assert.Equal((287, 144), (guard.GridX, guard.GridY));
        Assert.Equal("saffron_city", game.Map.AreaAt(287, 143)?.Key);
    }

    [Fact]
    public void SabrinaGivesTheMarshBadgeAndTmFour()
    {
        var game = PastPewter();
        game.Arrive("SaffronCity_Gym", 14, 22);
        var sabrina = game.Talk("sabrina");
        Assert.Contains(sabrina.Log, l => l.StartsWith("battle kanto_leader_sabrina Won"));
        Assert.True(game.Story.HasBadge("Kanto", "Marsh"));
        Assert.Equal(1, Count(game, "TM04"));
        Assert.True(game.Story.HasDefeated("kanto_psychic_tyron"));
        Assert.Equal("Marsh", FieldMoveRules.BadgeFor(FieldMove.RockSmash, "Kanto"));
    }

    [Theory]
    [InlineData("item_ball", "Hitmonlee")]
    [InlineData("item_ball_2", "Hitmonchan")]
    public void TheDojosMasterBattlesAtHisStepAndGivesOneOfItsTwo(string ball, string species)
    {
        var game = PastPewter();
        game.Arrive("SaffronCity_Dojo", 6, 12);
        game.Talk(ball);
        Assert.DoesNotContain(game.Party.Members, p => p.Species.Name == species);
        var master = game.Step("MasterTrigger");
        Assert.Contains(master.Log, l => l.StartsWith("battle kanto_black_belt_koichi Won"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_SAFFRON_CITY_DOJO"));
        game.Answers.Enqueue(0);
        game.Talk(ball);
        Assert.Contains(game.Party.Members, p => p.Species.Name == species);
        string other = ball == "item_ball" ? "item_ball_2" : "item_ball";
        game.Answers.Enqueue(0);
        game.Talk(other);
        Assert.Equal(2, game.Party.Members.Count);
    }

    [Fact]
    public void ADepartmentStoreLiftRidesToTheFloorChosen()
    {
        var game = PastPewter();
        game.Arrive("CeladonCity_DepartmentStore_Elevator", 2, 3);
        game.Answers.Enqueue(0);
        game.Tile = (0, 3);
        var ride = game.Play(Scripts.Find("Panel", "celadon_city_department_store_elevator")!);
        Assert.Contains("warp CeladonCity_DepartmentStore_5F 6 2", ride.Log);
        Assert.Equal(5, game.Story.Var("VAR_ELEVATOR_FLOOR"));
    }

    [Fact]
    public void TheMansionsEeveeAndMrPsychicsTm()
    {
        var game = PastPewter();
        game.Arrive("CeladonCity_Condominiums_RoofRoom", 4, 6);
        game.Answers.Enqueue(0);
        game.Talk("eevee_pokeball");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Eevee" && p.Level == 25);
        Assert.Null(game.Present("eevee_pokeball"));
        game.Arrive("SaffronCity_MrPsychicsHouse", 4, 6);
        game.Talk("balding_man");
        Assert.Equal(1, Count(game, "TM29"));
    }

    // ---------------------------------------------------------------- plan 19 · K6, part a

    [Theory]
    [InlineData("Route16_NorthEntrance_1F", "route_16_north_entrance_1f")]
    [InlineData("Route18_EastEntrance_1F", "route_18_east_entrance_1f")]
    public void TheCyclingRoadsGatesTurnBackWhoeverHasNoBicycleAndNoteTheRoad(string gate, string file)
    {
        var game = PastPewter();
        game.Arrive(gate, 6, 6);
        var steps = game.Map.Triggers.Where(t => t.Script == "NeedBike").ToList();
        Assert.Equal(5, steps.Count);
        // On foot with no Bicycle, every step across the room is turned back east
        foreach (var step in steps)
        {
            game.Tile = (step.X, step.Y);
            var turned = game.Play(Scripts.Find("NeedBike", file)!);
            Assert.Equal((step.X + 1, step.Y), game.Tile);
            Assert.Contains(turned.Transcript, l => l.Text.Contains("Bicycle"));
        }
        // With one in the bag, nobody stops the player
        game.Bag.AddItem(ItemDatabase.Get("Bicycle")!, 1);
        var through = game.Step("NeedBike");
        Assert.Empty(through.Transcript);
        // The steps by the road's door note that the player is going onto it, those by the far door that they came off
        game.Step("OntoTheRoad");
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROUTE16"));
        Assert.False(game.Fires("OntoTheRoad"));
        game.Step("OffTheRoad");
        Assert.Equal(0, game.Story.Var("VAR_MAP_SCENE_ROUTE16"));
    }

    [Fact]
    public void TheGbasTemporaryVariablesAreForgottenAsAPlaceIsLeft()
    {
        // FireRed clears VAR_TEMP_0 to F as every map loads: the Pokémon Tower's fifth floor sets VAR_TEMP_1 1, and the
        // Cycling Road's gates and the Safari Zone's counter only fire on 0
        var story = new StoryState();
        story.SetVar("VAR_TEMP_1", 1);
        story.SetVar("VAR_MAP_SCENE_ROUTE16", 1);
        story.ClearLocal();
        Assert.Equal(0, story.Var("VAR_TEMP_1"));
        Assert.Equal(1, story.Var("VAR_MAP_SCENE_ROUTE16"));
    }

    [Theory]
    [InlineData(150, 138)]
    [InlineData(144, 250)]
    [InlineData(170, 320)]
    public void OnTheCyclingRoadThePlayerRidesAndCantGetOff(int x, int y)
    {
        var game = PastPewter();
        // Come by any other way, the road's three routes leave the player as they were
        game.Arrive("Kanto", x, y);
        Assert.DoesNotContain(game.Played, p => p.Host.Log.Contains("bicycle on"));
        Assert.False(game.Story.Has(BicycleRules.OnCyclingRoadFlag));
        // Come through a gate onto it, they ride, and the Bicycle can't be got off until the next door
        game.Story.SetVar("VAR_MAP_SCENE_ROUTE16", 1);
        game.Arrive("Kanto", x, y);
        Assert.Contains("bicycle on", game.Last.Log);
        Assert.True(game.Last.Cycling);
        Assert.True(game.Story.Has(BicycleRules.OnCyclingRoadFlag));
    }

    [Fact]
    public void TheSafariZonesCounterLetsThePlayerInForFiveHundredWithThirtyBallsAndSixHundredSteps()
    {
        var game = PastPewter();
        game.Arrive("FuchsiaCity_SafariZone_Entrance", 4, 6);
        // Turned down, the player is sent back a step
        game.Answers.Enqueue(1);
        var away = game.Step("Counter");
        Assert.False(away.Safari.Active);
        Assert.Equal(4, game.Tile.Y);
        // Agreed to, the fee is taken, the game begins with FireRed's numbers and the player is in the zone's centre
        game.Arrive("FuchsiaCity_SafariZone_Entrance", 4, 6);
        game.Answers.Enqueue(0);
        var counter = game.Step("Counter");
        Assert.True(counter.Safari.Active);
        Assert.Equal((SafariGame.StartBalls, SafariGame.KantoSteps), (counter.Safari.Balls, counter.Safari.Steps));
        Assert.Equal(2500, counter.Money);
        Assert.Contains("warp SafariZone_Center 26 29", counter.Log);
        Assert.Equal("SafariZone_Center", game.Map.Name);
        Assert.False(game.Map.IsSolid(26, 29));
        Assert.Equal("safari_zone_center", game.Map.AreaAt(26, 29)?.Key);
    }

    [Theory]
    [InlineData("SafariTimeUp")]
    [InlineData("SafariOutOfBalls")]
    public void AKantoSafariGameThatRunsOutEndsAtTheSafariZonesCounterAndASinnohOneAtTheMarsh(string script)
    {
        var game = PastPewter();
        game.Arrive("SafariZone_West", 20, 20);
        var over = game.Play(Scripts.Find(script, "common")!);
        Assert.False(over.Safari.Active);
        Assert.Contains("warp FuchsiaCity_SafariZone_Entrance 4 2", over.Log);
        // The gate sees the player off and leaves its counter quiet
        Assert.Equal("FuchsiaCity_SafariZone_Entrance", game.Map.Name);
        Assert.Equal(0, game.Story.Var("VAR_MAP_SCENE_FUCHSIA_CITY_SAFARI_ZONE_ENTRANCE"));
        Assert.Equal((4, 4), game.Tile);
        // In Sinnoh the same scripts take the player to Pastoria's observatory as before
        var marsh = new HeadlessScriptHost { Region = RegionDatabase.Sinnoh };
        marsh.Safari.Start();
        var runner = new ScriptRunner(Scripts, marsh);
        runner.Start(Scripts.Find(script, "common")!);
        runner.RunToEnd();
        Assert.Contains("warp PastoriaCityObservatoryGate1F 5 3", marsh.Log);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void LeavingTheSafariZoneEarlyIsAskedAtItsGate(int answer, bool leaves)
    {
        var game = PastPewter();
        game.Arrive("FuchsiaCity_SafariZone_Entrance", 4, 2);
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = (4, 1) };
        host.Safari.Start(SafariGame.KantoSteps);
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.In("fuchsia_city_safari_zone_entrance", ScriptLibrary.OnEnter)!);
        runner.RunToEnd();
        // Yes ends the game and walks the player out past the counter; no leads back into the zone with the game on
        Assert.Equal(leaves, !host.Safari.Active);
        if (leaves) Assert.Equal((4, 4), host.PlayerTile);
        else Assert.Contains("warp SafariZone_Center 26 29", host.Log);
    }

    [Fact]
    public void TheWardenMumblesUntilHisGoldTeethAreBackAndGivesStrength()
    {
        var game = PastPewter();
        game.Arrive("FuchsiaCity_WardensHouse", 6, 8);
        game.Answers.Enqueue(0);
        var mumbling = game.Talk("old_man_2");
        Assert.Equal(0, Count(game, "HM04"));
        Assert.Contains(mumbling.Transcript, l => l.Text.StartsWith("Ah, howhee"));
        // The teeth lie in the zone's west part, in an item ball of FireRed's
        game.Arrive("SafariZone_West", 28, 15);
        var teeth = game.Present("item_ball")!;
        Assert.Equal("Gold Teeth", teeth.Item);
        game.Bag.AddItem(ItemDatabase.Get("Gold Teeth")!, 1);
        game.Arrive("FuchsiaCity_WardensHouse", 6, 8);
        game.Talk("old_man_2");
        Assert.Equal(1, Count(game, "HM04"));
        Assert.Equal(0, Count(game, "Gold Teeth"));
        Assert.True(game.Story.Has("FLAG_GOT_HM04"));
        game.Talk("old_man_2");
        Assert.Equal(1, Count(game, "HM04"));
    }

    [Fact]
    public void TheSafariZonesSecretHouseGivesSurfOnce()
    {
        var game = PastPewter();
        game.Arrive("SafariZone_SecretHouse", 4, 8);
        game.Talk("worker_m");
        game.Talk("worker_m");
        Assert.Equal(1, Count(game, "HM03"));
        Assert.True(game.Story.Has("FLAG_GOT_HM03"));
    }

    [Fact]
    public void KogaGivesTheSoulBadgeAndTmSixAndSurfsGateOpens()
    {
        var game = PastPewter();
        game.Arrive("FuchsiaCity_Gym", 7, 20);
        var koga = game.Talk("koga");
        Assert.Contains(koga.Log, l => l.StartsWith("battle kanto_leader_koga Won"));
        Assert.True(game.Story.HasBadge("Kanto", "Soul"));
        Assert.Equal(1, Count(game, "TM06"));
        foreach (string id in new[] { "kanto_juggler_kayden", "kanto_juggler_shawn", "kanto_juggler_kirk", "kanto_juggler_nate", "kanto_tamer_edgar", "kanto_tamer_phil" })
            Assert.True(game.Story.HasDefeated(id), id);
        Assert.Equal("Soul", FieldMoveRules.BadgeFor(FieldMove.Surf, "Kanto"));
        Assert.DoesNotContain(game.Talk("koga").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, Count(game, "TM06"));
        Assert.Equal(PokemonType.Poison, game.Map.ArenaType);
    }

    [Theory]
    [InlineData("FuchsiaCity_House2", "Good Rod", "FLAG_GOT_GOOD_ROD")]
    [InlineData("Route12_FishingHouse", "Super Rod", "FLAG_GOT_SUPER_ROD")]
    public void TheFishingBrothersGiveTheirRodsToWhoeverLikesToFish(string room, string rod, string flag)
    {
        var game = PastPewter();
        game.Arrive(room, 3, 6);
        game.Answers.Enqueue(1);
        game.Talk("fisher");
        Assert.Equal(0, Count(game, rod));
        game.Answers.Enqueue(0);
        game.Talk("fisher");
        Assert.Equal(1, Count(game, rod));
        Assert.True(game.Story.Has(flag));
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    public void RouteFifteensAideGivesAnExpShareForFiftyKindsCaught(int caught, bool given)
    {
        var game = PastPewter();
        game.Caught = caught;
        game.Arrive("Route15_WestEntrance_2F", 5, 7);
        game.Answers.Enqueue(0);
        game.Talk("scientist");
        Assert.Equal(given ? 1 : 0, Count(game, "Exp. Share"));
        Assert.Equal(given, game.Story.Has("FLAG_GOT_EXP_SHARE_FROM_OAKS_AIDE"));
    }

    [Fact]
    public void FuchsiasPensShowKabutoOrOmanyteAndItsBoyTeachesSubstitute()
    {
        var game = PastPewter();
        game.Arrive("Kanto", 216, 320);
        Assert.Equal("Kabuto", game.Map.NPCs.Single(n => n.Key == "var_0").Species);
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "omanyte");
        game.Story.Set("FLAG_GOT_DOME_FOSSIL");
        game.Arrive("Kanto", 216, 320);
        Assert.Equal("Omanyte", game.Map.NPCs.Single(n => n.Key == "omanyte").Species);
        Assert.DoesNotContain(game.Map.NPCs, n => n.Key == "var_0");
        // The boy by the Mart teaches Substitute, once
        var tutor = game.Map.NPCs.Single(n => n.Key == "bug_catcher" && game.Map.AreaAt(n.GridX, n.GridY)?.Key == "fuchsia_city");
        game.Answers.Enqueue(0);
        game.Talk(tutor);
        Assert.True(game.Party.Members[0].Knows("Substitute"));
        Assert.True(game.Story.Has("FLAG_TUTOR_SUBSTITUTE"));
        Assert.Empty(game.Talk(tutor).Asked);
    }

    // ------------------------------------------------------------------ plan 19 · K6, part b: the sea and Cinnabar

    [Fact]
    public void BlainesQuizOpensItsDoorsAndBlaineGivesTheVolcanoBadge()
    {
        var game = PastPewter();
        game.Arrive("CinnabarIsland_Gym", 25, 22);
        Assert.NotNull(game.Present("door_1a"));
        // A right answer opens the door with no battle
        game.Answers.Enqueue(0);
        var right = game.Play(Scripts.Find("Quiz1", "cinnabar_island_gym")!);
        Assert.DoesNotContain(right.Log, l => l.StartsWith("battle"));
        Assert.True(game.Story.Has("FLAG_CINNABAR_GYM_QUIZ_1"));
        Assert.Null(game.Present("door_1a"));
        // A wrong one calls out the room's trainer, and beating them opens it
        game.Answers.Enqueue(0);
        var wrong = game.Play(Scripts.Find("Quiz2", "cinnabar_island_gym")!);
        Assert.Contains(wrong.Log, l => l.StartsWith("battle kanto_super_nerd_avery Won"));
        Assert.True(game.Story.Has("FLAG_CINNABAR_GYM_QUIZ_2"));
        // Once the trainer is beaten, a wrong answer calls nobody out
        game.Story.Unset("FLAG_CINNABAR_GYM_QUIZ_2");
        game.Answers.Enqueue(0);
        Assert.DoesNotContain(game.Play(Scripts.Find("Quiz2", "cinnabar_island_gym")!).Log, l => l.StartsWith("battle"));
        Assert.False(game.Story.Has("FLAG_CINNABAR_GYM_QUIZ_2"));

        var blaine = game.Talk("blaine");
        Assert.Contains(blaine.Log, l => l.StartsWith("battle kanto_leader_blaine Won"));
        Assert.True(game.Story.HasBadge("Kanto", "Volcano"));
        Assert.Equal(1, Count(game, "TM38"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_CINNABAR_ISLAND"));
        for (int n = 1; n <= 6; n++) Assert.True(game.Story.Has($"FLAG_CINNABAR_GYM_QUIZ_{n}"), $"door {n}");
        foreach (string id in new[] { "kanto_burglar_quinn", "kanto_super_nerd_erik", "kanto_burglar_ramon", "kanto_super_nerd_derek", "kanto_burglar_dusty", "kanto_super_nerd_zac" })
            Assert.True(game.Story.HasDefeated(id), id);
        Assert.Equal("Volcano", FieldMoveRules.BadgeFor(FieldMove.Waterfall, "Kanto"));
        Assert.DoesNotContain(game.Talk("blaine").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, Count(game, "TM38"));
        // Bill waits outside now (plan 19 · K7: BillTakesThePlayerToOneIslandWhereCelioGivesTheTriPass)
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_CINNABAR_ISLAND"));
    }

    [Fact]
    public void CinnabarsGymDoorIsLockedUntilTheMansionsSecretKeyIsFound()
    {
        var game = PastPewter();
        game.Arrive("Kanto", 80, 388);
        Assert.True(game.Fires("LockedGymDoor"));
        var turned = game.Step("LockedGymDoor");
        Assert.Contains(turned.Transcript, l => l.Text.Contains("locked"));
        // The key's ball in the Mansion's basement, picked up
        game.Arrive("PokemonMansion_B1F", 34, 28);
        var key = game.Present("item_ball_4")!;
        Assert.Equal("Secret Key", key.Item);
        game.Talk(key);
        Assert.Equal(1, Count(game, "Secret Key"));
        game.Arrive("Kanto", 80, 388);
        Assert.False(game.Fires("LockedGymDoor"));
    }

    [Fact]
    public void TheMansionsSwitchTurnsEveryBarrierOver()
    {
        var game = PastPewter();
        game.Arrive("PokemonMansion_1F", 8, 32);
        Assert.NotNull(game.Present("barrier_1"));
        Assert.Null(game.Present("barrier_19"));
        game.Answers.Enqueue(1);
        game.Play(Scripts.Find("PokemonMansionSwitch", "pokemon_mansion_1f")!);
        Assert.NotNull(game.Present("barrier_1"));
        game.Answers.Enqueue(0);
        game.Play(Scripts.Find("PokemonMansionSwitch", "pokemon_mansion_1f")!);
        Assert.True(game.Story.Has("FLAG_POKEMON_MANSION_SWITCH_STATE"));
        Assert.Null(game.Present("barrier_1"));
        Assert.NotNull(game.Present("barrier_19"));
        // On every floor at once, and back again
        game.Arrive("PokemonMansion_B1F", 34, 28);
        Assert.Null(game.Present("barrier_1"));
        Assert.NotNull(game.Present("barrier_10"));
        game.Answers.Enqueue(0);
        game.Play(Scripts.Find("PokemonMansionSwitch", "pokemon_mansion_b1f")!);
        Assert.False(game.Story.Has("FLAG_POKEMON_MANSION_SWITCH_STATE"));
        Assert.NotNull(game.Present("barrier_1"));
    }

    [Fact]
    public void SeafoamsBouldersFallFloorByFloorAndStillTheCurrentsWhichRouteTwentyLeavesAlone()
    {
        var game = PastPewter();
        // Coming to Route 20 puts every boulder where it starts
        game.Arrive("Kanto", 150, 390);
        Assert.Equal("route_20", game.Map.AreaAt(150, 390)?.Key);
        Assert.True(game.Story.Has("FLAG_HIDE_SEAFOAM_B1F_BOULDER_1"));
        Assert.False(game.Story.Has("FLAG_HIDE_SEAFOAM_1F_BOULDER_1"));
        // Each boulder down a hole is gone from its floor and shows on the floor below, its own below its own
        foreach (var (floor, file, script, boulders) in new[]
                 {
                     ("SeafoamIslands_1F", "seafoam_islands_1f", "FallFrom1F", new[] { ("pushable_boulder", "FLAG_HIDE_SEAFOAM_B1F_BOULDER_1"), ("pushable_boulder_2", "FLAG_HIDE_SEAFOAM_B1F_BOULDER_2") }),
                     ("SeafoamIslands_B1F", "seafoam_islands_b1f", "FallFromB1F", new[] { ("pushable_boulder", "FLAG_HIDE_SEAFOAM_B2F_BOULDER_1"), ("pushable_boulder_2", "FLAG_HIDE_SEAFOAM_B2F_BOULDER_2") }),
                     ("SeafoamIslands_B2F", "seafoam_islands_b2f", "FallFromB2F", new[] { ("pushable_boulder", "FLAG_HIDE_SEAFOAM_B3F_BOULDER_1"), ("pushable_boulder_2", "FLAG_HIDE_SEAFOAM_B3F_BOULDER_2") })
                 })
        {
            game.Arrive(floor, 10, 7);
            foreach (var (key, below) in boulders)
            {
                var boulder = game.Present(key)!;
                Assert.Equal(PropType.StrengthBoulder, boulder.Obstacle);
                string own = boulder.HiddenBy!;
                game.Play(Scripts.Find(script, file)!, boulder);
                Assert.True(game.Story.Has(own), own);
                Assert.False(game.Story.Has(below), below);
                Assert.Null(game.Present(key));
            }
        }
        // Both on B3F lie across its current, and coming down stills it for good
        Assert.False(game.Story.Has("FLAG_STOPPED_SEAFOAM_B3F_CURRENT"));
        game.Arrive("SeafoamIslands_B3F", 8, 15);
        Assert.True(game.Story.Has("FLAG_STOPPED_SEAFOAM_B3F_CURRENT"));
        Assert.NotNull(game.Present("pushable_boulder"));
        Assert.NotNull(game.Present("pushable_boulder_2"));
        // B3F's two that matter go down to B4F, and still its current too
        foreach (var (key, below) in new[] { ("pushable_boulder_6", "FLAG_HIDE_SEAFOAM_B4F_BOULDER_1"), ("pushable_boulder_3", "FLAG_HIDE_SEAFOAM_B4F_BOULDER_2") })
        {
            game.Play(Scripts.Find("FallFromB3F", "seafoam_islands_b3f")!, game.Present(key)!);
            Assert.False(game.Story.Has(below), below);
        }
        game.Arrive("SeafoamIslands_B4F", 15, 10);
        Assert.True(game.Story.Has("FLAG_STOPPED_SEAFOAM_B4F_CURRENT"));
        // Route 20 leaves a still current's boulders where they are
        game.Arrive("Kanto", 150, 390);
        Assert.False(game.Story.Has("FLAG_HIDE_SEAFOAM_B3F_BOULDER_1"));
        Assert.False(game.Story.Has("FLAG_HIDE_SEAFOAM_B4F_BOULDER_1"));
    }

    [Theory]
    [InlineData("SeafoamIslands_B4F", 15, 10, "articuno", "Articuno", "FLAG_FOUGHT_ARTICUNO", "FLAG_HIDE_ARTICUNO")]
    [InlineData("PowerPlant", 5, 37, "zapdos", "Zapdos", "FLAG_FOUGHT_ZAPDOS", "FLAG_HIDE_ZAPDOS")]
    [InlineData("MtEmber_Summit", 9, 12, "moltres", "Moltres", "FLAG_FOUGHT_MOLTRES", "FLAG_HIDE_MOLTRES")]
    public void TheLegendaryBirdFliesOffWhenRunFromAndIsGoneForGoodOnceFought(string map, int x, int y, string key, string species, string fought, string hide)
    {
        var game = PastPewter();
        game.Arrive(map, x, y);
        Assert.NotNull(game.Present(key));
        game.Fight = BattleOutcome.Fled;
        var fled = game.Talk(key);
        Assert.Contains(fled.Log, l => l.StartsWith($"wildbattle {species} 50"));
        Assert.Null(game.Present(key));
        Assert.False(game.Story.Has(fought));
        game.Arrive(map, x, y);
        Assert.NotNull(game.Present(key));
        game.Fight = BattleOutcome.Won;
        game.Talk(key);
        Assert.True(game.Story.Has(fought));
        Assert.True(game.Story.Has(hide));
        game.Arrive(map, x, y);
        Assert.Null(game.Present(key));
    }

    [Fact]
    public void ThePowerPlantsElectrodeLieInWaitAsPokeBallsAndAreGoneOnceFought()
    {
        var game = PastPewter();
        game.Arrive("PowerPlant", 5, 37);
        foreach (var (key, flag) in new[] { ("item_ball_7", "FLAG_FOUGHT_POWER_PLANT_ELECTRODE_1"), ("item_ball_6", "FLAG_FOUGHT_POWER_PLANT_ELECTRODE_2") })
        {
            game.Fight = BattleOutcome.Fled;
            var met = game.Talk(key);
            Assert.Contains(met.Log, l => l.StartsWith("wildbattle Electrode 34"));
            Assert.True(game.Story.Has(flag), flag);
            Assert.Null(game.Present(key));
        }
        game.Arrive("PowerPlant", 5, 37);
        Assert.Null(game.Present("item_ball_6"));
        Assert.NotNull(game.Present("item_ball_5"));
    }

    [Fact]
    public void CinnabarsLabBringsAFossilBackAtLevelFiveOnceThePlayerHasGoneOut()
    {
        var game = PastPewter();
        game.Bag.AddItem(ItemDatabase.Get("Helix Fossil")!, 1);
        game.Arrive("CinnabarIsland_PokemonLab_ExperimentRoom", 7, 8);
        game.Answers.Enqueue(0);
        game.Talk("scientist");
        Assert.Equal(0, Count(game, "Helix Fossil"));
        game.Talk("scientist");
        Assert.Single(game.Party.Members);
        game.Story.ClearLocal();
        game.Talk("scientist");
        var revived = game.Party.Members[^1];
        Assert.Equal("Omanyte", revived.Species.Name);
        Assert.Equal(5, revived.Level);
    }

    [Fact]
    public void TheLabsResearcherTeachesMetronomeOnce()
    {
        var game = PastPewter();
        Assert.Equal(TutorAnswer.Able, MoveTutors.ForKanto(new Pokemon(PokemonDatabase.Get("Clefairy")!, 20), "Metronome"));
        game.Party.Members.Clear();
        game.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Clefairy")!, 20));
        game.Arrive("CinnabarIsland_PokemonLab_ResearchRoom", 7, 8);
        game.Answers.Enqueue(0);
        game.Talk("poke_maniac");
        Assert.True(game.Party.Members[0].Knows("Metronome") || game.Story.Has("FLAG_TUTOR_METRONOME"));
        Assert.True(game.Story.Has("FLAG_TUTOR_METRONOME"));
        Assert.Empty(game.Talk("poke_maniac").Asked);
    }

    [Fact]
    public void ANewGameOfKantoHidesBillAndTheFerryOnCinnabarIsland()
    {
        var game = new Game();
        foreach (string flag in new[] { "FLAG_HIDE_CINNABAR_BILL", "FLAG_HIDE_CINNABAR_SEAGALLOP", "FLAG_HIDE_CINNABAR_POKECENTER_BILL" })
            Assert.True(game.Story.Has(flag), flag);
        // A save from before the chapter, which never came here, has the places' own arrivals hide them
        var older = PastPewter();
        foreach (string flag in new[] { "FLAG_HIDE_CINNABAR_BILL", "FLAG_HIDE_CINNABAR_SEAGALLOP", "FLAG_HIDE_CINNABAR_POKECENTER_BILL" })
            older.Story.Unset(flag);
        older.Arrive("Kanto", 80, 388);
        older.Arrive("CinnabarIsland_PokemonCenter_1F", 7, 7);
        Assert.True(older.Story.Has("FLAG_HIDE_CINNABAR_BILL"));
        Assert.True(older.Story.Has("FLAG_HIDE_CINNABAR_SEAGALLOP"));
        Assert.True(older.Story.Has("FLAG_HIDE_CINNABAR_POKECENTER_BILL"));
        Assert.Null(older.Present("cinnabar_pokemon_center_bill"));
    }

    // ------------------------------------------------------------------ plan 19 · K7: the road to the League

    private static readonly string[] AllBadges = { "Boulder", "Cascade", "Thunder", "Rainbow", "Soul", "Marsh", "Volcano", "Earth" };

    private static Game WithBadges(int count, int starter = 0)
    {
        var game = PastPewter(starter);
        foreach (string badge in AllBadges.Take(count)) game.Story.GiveBadge("Kanto", badge);
        return game;
    }

    [Fact]
    public void GiovanniGivesTheEarthBadgeAndTmTwentySixAndSendsTheRivalToRouteTwentyTwo()
    {
        var game = WithBadges(7);
        game.Arrive("ViridianCity_Gym", 17, 21);
        var giovanni = game.Talk("viridian_giovanni");
        Assert.Contains(giovanni.Log, l => l.StartsWith("battle kanto_leader_giovanni Won"));
        Assert.True(game.Story.HasBadge("Kanto", "Earth"));
        Assert.Equal(1, Count(game, "TM26"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_LEADER_GIOVANNI"));
        Assert.True(game.Story.Has("FLAG_HIDE_MISC_KANTO_ROCKETS"));
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_ROUTE22"));
        foreach (string id in new[] { "kanto_black_belt_takashi", "kanto_cooltrainer_yuji", "kanto_black_belt_atsushi", "kanto_tamer_jason",
                     "kanto_tamer_cole", "kanto_black_belt_kiyo", "kanto_cooltrainer_samuel", "kanto_cooltrainer_warren" })
            Assert.True(game.Story.HasDefeated(id), id);
        // Spoken to again, he gives up Team Rocket and is gone for good
        Assert.DoesNotContain(game.Talk("viridian_giovanni").Log, l => l.StartsWith("battle"));
        Assert.True(game.Story.Has("FLAG_HIDE_VIRIDIAN_GIOVANNI"));
        Assert.Null(game.Present("viridian_giovanni"));
        Assert.Equal(1, Count(game, "TM26"));
    }

    [Theory]
    [InlineData(0, "charmander")]
    [InlineData(1, "squirtle")]
    [InlineData(2, "bulbasaur")]
    public void TheRivalWaitsBeforeTheLeaguesGateOnceGiovanniIsBeaten(int starter, string his)
    {
        var game = WithBadges(8, starter);
        game.Arrive("Kanto", 33, 195);
        Assert.False(game.Fires("LateRival"));
        game.Story.SetVar("VAR_MAP_SCENE_ROUTE22", 3);
        Assert.True(game.Fires("LateRival"));
        var road = game.Step("LateRival");
        Assert.Contains(road.Log, l => l.StartsWith($"battle kanto_rival_route22_late_{his} Won"));
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_ROUTE22"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_22_RIVAL"));
        Assert.False(game.Fires("LateRival"));
    }

    [Fact]
    public void RouteTwentyThreesGuardsAskForEachBadgeInTheOrderTheyAreWon()
    {
        var game = WithBadges(1);
        game.Story.SetVar("VAR_MAP_SCENE_ROUTE23", 1);
        game.Arrive("Kanto", 8, 182);
        Assert.Equal("route_23", game.Map.AreaAt(8, 182)?.Key);
        // Every guard's step can be stood on (FireRed's lines run on over rock and trees here, and those tiles are left
        // out) and has an open step back south of it, but the one beside the last guard, which the rock south of it
        // sends back by the step west of it
        var wrong = new List<string>();
        foreach (var t in game.Map.Triggers.Where(t => t.ScriptFile == "route_23"))
        {
            if (game.Map.IsSolid(t.X, t.Y)) { wrong.Add($"{t.Script} on rock at {t.X},{t.Y}"); continue; }
            var back = (t.X, t.Y) == (5, 65) ? (X: 4, Y: 66) : (X: t.X, Y: t.Y + 1);
            if (game.Map.IsSolid(back.X, back.Y)) wrong.Add($"{t.Script} back from {t.X},{t.Y}");
        }
        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
        string[] guards = { "Cascade", "Thunder", "Rainbow", "Soul", "Marsh", "Volcano", "Earth" };
        for (int n = 0; n < guards.Length; n++)
        {
            string trigger = guards[n] + "Trigger";
            Assert.True(game.Fires(trigger), trigger);
            // Without the Badge, turned back a step and the guard still waits
            var refused = game.Step(trigger);
            Assert.Contains(refused.Transcript, l => l.Text.Contains($"need the {guards[n]} Badge"));
            Assert.Equal(n + 1, game.Story.Var("VAR_MAP_SCENE_ROUTE23"));
            // With it, let through, and the next guard waits
            game.Story.GiveBadge("Kanto", guards[n]);
            game.Step(trigger);
            Assert.Equal(n + 2, game.Story.Var("VAR_MAP_SCENE_ROUTE23"));
            Assert.False(game.Fires(trigger));
        }
        Assert.Contains(game.Talk("earth_badge_guard").Transcript, l => l.Text.Contains("all eight"));
    }

    [Fact]
    public void VictoryRoadsSwitchesLiftTheirBarriersUntilRouteTwentyThreeIsComeToAgain()
    {
        var game = WithBadges(8);
        game.Arrive("Kanto", 8, 182);
        foreach (var (floor, file, barriers, switches) in new[]
                 {
                     ("VictoryRoad_1F", "kanto_victory_road_1f", new[] { "rock_barrier_1", "rock_barrier_2" }, new[] { ("FloorSwitch", "FLAG_VICTORY_ROAD_1F_SWITCH_PRESSED") }),
                     ("VictoryRoad_3F", "kanto_victory_road_3f", new[] { "rock_barrier_1", "rock_barrier_2" }, new[] { ("FloorSwitch", "FLAG_VICTORY_ROAD_3F_SWITCH_PRESSED") }),
                     ("VictoryRoad_2F", "kanto_victory_road_2f", new[] { "rock_barrier_1", "rock_barrier_2", "rock_barrier_3", "rock_barrier_4" },
                         new[] { ("FloorSwitch1", "FLAG_VICTORY_ROAD_2F_SWITCH_1_PRESSED"), ("FloorSwitch2", "FLAG_VICTORY_ROAD_2F_SWITCH_2_PRESSED") })
                 })
        {
            game.Arrive(floor, 10, 10);
            foreach (string barrier in barriers) Assert.Equal(NPC.RubbleType, game.Present(barrier)!.NpcType);
            foreach (var (script, flag) in switches)
            {
                var pit = game.Map.BoulderPits.First(p => p.Value == script).Key;
                Assert.True(game.Map.InBounds(pit.X, pit.Y));
                game.Play(Scripts.Find(script, file)!, game.Map.Everyone.First(n => n.Obstacle == PropType.StrengthBoulder));
                Assert.True(game.Story.Has(flag), flag);
            }
            game.Arrive(floor, 10, 10);
            foreach (string barrier in barriers) Assert.Null(game.Present(barrier));
        }
        // 3F's boulder by the hole falls to 2F, where the second switch waits for it
        game.Arrive("VictoryRoad_3F", 10, 10);
        game.Play(Scripts.Find("FallThrough", "kanto_victory_road_3f")!, game.Present("pushable_boulder")!);
        Assert.True(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_3F_BOULDER"));
        Assert.False(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_2F_BOULDER"));
        // Route 23 puts every barrier and the two boulders back
        game.Arrive("Kanto", 8, 182);
        foreach (string flag in new[] { "FLAG_VICTORY_ROAD_1F_SWITCH_PRESSED", "FLAG_VICTORY_ROAD_2F_SWITCH_1_PRESSED",
                     "FLAG_VICTORY_ROAD_2F_SWITCH_2_PRESSED", "FLAG_VICTORY_ROAD_3F_SWITCH_PRESSED", "FLAG_HIDE_VICTORY_ROAD_3F_BOULDER" })
            Assert.False(game.Story.Has(flag), flag);
        Assert.True(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_2F_BOULDER"));
    }

    [Fact]
    public void VictoryRoadsCooltrainerTeachesDoubleEdgeOnce()
    {
        var game = WithBadges(8);
        game.Party.Members.Clear();
        game.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Snorlax")!, 40));
        game.Arrive("VictoryRoad_2F", 10, 10);
        game.Answers.Enqueue(0);
        game.Talk("cooltrainer_m");
        Assert.True(game.Story.Has("FLAG_TUTOR_DOUBLE_EDGE"));
        Assert.True(game.Party.Members[0].Knows("Double-Edge"));
    }

    [Theory]
    [InlineData(0, "charmander")]
    [InlineData(2, "bulbasaur")]
    public void TheEliteFourOpenTheirDoorsOneByOneAndTheChampionLeadsToTheHallOfFame(int starter, string his)
    {
        var game = WithBadges(8, starter);
        game.Story.Set("FLAG_DEFEATED_LORELEI");
        // The hall's Pokémon Center puts all four back
        game.Arrive("IndigoPlateau_PokemonCenter_1F", 11, 15);
        Assert.False(game.Story.Has("FLAG_DEFEATED_LORELEI"));
        var door = game.Map.Warps.First(w => w.TargetMap == "PokemonLeague_LoreleisRoom");
        Assert.Equal((6, 10), (door.TargetX, door.TargetY));
        foreach (var (room, who, id, flag, next) in new[]
                 {
                     ("PokemonLeague_LoreleisRoom", "lorelei", "kanto_elite_four_lorelei", "FLAG_DEFEATED_LORELEI", "PokemonLeague_BrunosRoom"),
                     ("PokemonLeague_BrunosRoom", "bruno", "kanto_elite_four_bruno", "FLAG_DEFEATED_BRUNO", "PokemonLeague_AgathasRoom"),
                     ("PokemonLeague_AgathasRoom", "agatha", "kanto_elite_four_agatha", "FLAG_DEFEATED_AGATHA", "PokemonLeague_LancesRoom"),
                     ("PokemonLeague_LancesRoom", "lance", "kanto_elite_four_lance", "FLAG_DEFEATED_LANCE", "PokemonLeague_ChampionsRoom")
                 })
        {
            game.Through(room);
            var ahead = game.Map.Warps.Single(w => w.TargetMap == next);
            // The door ahead is shut until the master of the room is beaten, and there is no way back
            Assert.True(game.Map.IsSolid(ahead.SourceX, ahead.SourceY), room);
            Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "IndigoPlateau_PokemonCenter_1F");
            var battle = game.Talk(who);
            Assert.Contains(battle.Log, l => l.StartsWith($"battle {id} Won"));
            Assert.True(game.Story.Has(flag));
            Assert.False(game.Map.IsSolid(ahead.SourceX, ahead.SourceY), room);
            Assert.DoesNotContain(game.Talk(who).Log, l => l.StartsWith("battle"));
        }
        // The Champion's room is one scene: the rival as Champion, Oak, and on to the Hall of Fame, the team recorded,
        // Kanto's story ended and the game saved at home
        game.Through("PokemonLeague_ChampionsRoom");
        var champion = game.Played.Single(p => p.Script.Contains("pokemon_league_champions_room") && p.Script.EndsWith("OnEnter")).Host;
        Assert.Contains(champion.Log, l => l.StartsWith($"battle kanto_champion_first_{his} Won"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_CHAMP"));
        Assert.False(game.Story.Has("FLAG_HIDE_OAK_IN_CHAMP_ROOM"));
        Assert.True(TrainerDatabase.IsRivalAsChampion(TrainerDatabase.Get($"kanto_champion_first_{his}")!));
        Assert.True(game.Story.Has(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag));
        Assert.True(game.Story.Has("FLAG_SYS_GAME_CLEAR"));
        var hall = game.Played.Single(p => p.Script.Contains("pokemon_league_hall_of_fame") && p.Script.EndsWith("OnEnter")).Host;
        Assert.Contains(hall.Log, l => l.StartsWith("halloffame"));
        Assert.Contains(hall.Log, l => l == "open Credits");
        Assert.Equal("PalletTown_PlayersHouse_2F", game.Map.Name);
    }

    [Fact]
    public void BillTakesThePlayerToOneIslandWhereCelioGivesTheTriPass()
    {
        var game = WithBadges(7);
        game.Story.SetVar("VAR_MAP_SCENE_CINNABAR_ISLAND", 1);
        // Refused, he goes to wait in the Pokémon Center
        game.Answers.Enqueue(1);
        game.Arrive("Kanto", 80, 385);
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_CINNABAR_ISLAND"));
        Assert.Null(game.Present("cinnabar_bill"));
        game.Arrive("CinnabarIsland_PokemonCenter_1F", 7, 7);
        Assert.NotNull(game.Present("cinnabar_pokemon_center_bill"));
        // Asked again there, he sails with the player, ashore at One Island and up to Celio, who gives the Tri-Pass
        game.Answers.Enqueue(0);
        game.Talk("cinnabar_pokemon_center_bill");
        Assert.Equal("OneIsland_PokemonCenter_1F", game.Map.Name);
        Assert.Equal(1, Count(game, "Tri-Pass"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        Assert.NotNull(game.Present("one_island_pokemon_center_bill"));
        Assert.NotNull(game.Present("celio"));
        // The harbour's sailor sails back to Cinnabar Island, and Vermilion's to One Island with the pass
        game.Arrive("OneIsland_Harbor", 8, 5);
        game.Answers.Enqueue(1);
        game.Talk("sailor");
        Assert.Equal("Kanto", game.Map.Name);
        Assert.Equal("cinnabar_island", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        game.Arrive("Kanto", 288, 232);
        game.Answers.Enqueue(0);
        game.Talk("vermilion_ferry_sailor");
        Assert.Equal("OneIsland_Harbor", game.Map.Name);
    }

    // ------------------------------------------------------------------ plan 19 · K9, part a: the first islands and the Ruby

    /// <summary>A Champion of Kanto who sailed with Bill to One Island and met Celio there (plan 19 · K7's scene).</summary>
    private static Game AtOneIsland()
    {
        var game = WithBadges(8);
        game.Story.SetVar("VAR_MAP_SCENE_CINNABAR_ISLAND", 1);
        game.Answers.Enqueue(0);
        game.Arrive("Kanto", 80, 385);
        Assert.Equal("OneIsland_PokemonCenter_1F", game.Map.Name);
        return game;
    }

    [Fact]
    public void BillsMeteoriteTakesThePlayerToTwoIslandAndLostellesRescueSendsBillHome()
    {
        var game = AtOneIsland();
        // Bill hands over the Meteorite for the game corner on Two Island as he and Celio set to work
        Assert.Equal(1, Count(game, "Meteorite"));
        Assert.True(game.Story.Has("FLAG_GOT_METEORITE_FROM_BILL"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        // Spoken to again he gives nothing more
        game.Talk("one_island_pokemon_center_bill");
        Assert.Equal(1, Count(game, "Meteorite"));
        // The Tri-Pass sails to Two Island, where Lostelle's father has lost her and a biker brings word of Three Island
        game.Arrive("OneIsland_Harbor", 8, 5);
        game.Answers.Enqueue(2);
        game.Talk("sailor");
        Assert.Equal("TwoIsland_Harbor", game.Map.Name);
        game.Arrive("TwoIsland_JoyfulGameCorner", 5, 7);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_TWO_ISLAND_JOYFUL_GAME_CORNER"));
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_THREE_ISLAND"));
        Assert.Null(game.Present("joyful_game_corner_lostelle"));
        Assert.Null(game.Present("joyful_game_corner_biker"));
        // He keeps the Meteorite waiting until she is home
        game.Talk("joyful_game_corner_daddy");
        Assert.Equal(1, Count(game, "Meteorite"));
        // On to Three Island: the gang's leader argues with the townsmen, and the gang is battled and leaves
        game.Arrive("TwoIsland_Harbor", 8, 5);
        game.Answers.Enqueue(3);
        game.Talk("sailor");
        Assert.Equal("ThreeIsland_Harbor", game.Map.Name);
        game.Arrive("ThreeIsland", 104, 28);
        Assert.NotNull(game.Present("paxton"));
        Assert.True(game.Fires("BikerIntro"));
        game.Step("BikerIntro");
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_THREE_ISLAND"));
        Assert.False(game.Fires("BikerIntro"));
        // Asked whether they want trouble: no, and the player is sent back a step
        game.Answers.Enqueue(1);
        var refused = game.Step("BikerBattle");
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("battle"));
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_THREE_ISLAND"));
        // Yes, and three of them and then Paxton are battled one after another
        game.Answers.Enqueue(0);
        var bikers = game.Step("BikerBattle");
        foreach (string id in new[] { "kanto_biker_goon", "kanto_biker_goon_2", "kanto_biker_goon_3", "kanto_cue_ball_paxton" })
            Assert.Contains(bikers.Log, l => l.StartsWith($"battle {id} Won"));
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_THREE_ISLAND"));
        Assert.True(game.Story.Has("FLAG_HIDE_THREE_ISLAND_BIKERS"));
        Assert.Null(game.Present("paxton"));
        Assert.Null(game.Present("three_island_biker1"));
        game.Talk("three_island_antibiker1");
        Assert.Equal(1, Count(game, "Full Restore"));
        game.Talk("three_island_antibiker1");
        Assert.Equal(1, Count(game, "Full Restore"));
        // Lostelle in Berry Forest: a Hypno, an Iapapa Berry, and home to the game corner, where her father thanks the player
        game.Arrive("ThreeIsland_BerryForest", 4, 10);
        Assert.NotNull(game.Present("berry_forest_lostelle"));
        var forest = game.Talk("berry_forest_lostelle");
        Assert.Contains(forest.Log, l => l.StartsWith("wildbattle Hypno 30"));
        Assert.Equal(1, Count(game, "Iapapa Berry"));
        Assert.True(game.Story.Has("FLAG_RESCUED_LOSTELLE"));
        Assert.Equal("TwoIsland_JoyfulGameCorner", game.Map.Name);
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_TWO_ISLAND_JOYFUL_GAME_CORNER"));
        Assert.NotNull(game.Present("joyful_game_corner_lostelle"));
        game.Arrive("ThreeIsland_BerryForest", 4, 10);
        Assert.Null(game.Present("berry_forest_lostelle"));
        // The Meteorite for a Moon Stone, which tells the machine on One Island it can be finished
        game.Arrive("TwoIsland_JoyfulGameCorner", 5, 7);
        game.Talk("joyful_game_corner_daddy");
        Assert.Equal(0, Count(game, "Meteorite"));
        Assert.Equal(1, Count(game, "Moon Stone"));
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_TWO_ISLAND_JOYFUL_GAME_CORNER"));
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        // Back at One Island the first step towards Bill sends him home by the Seagallop, and he says goodbye on Cinnabar
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        Assert.True(game.Fires("BillLeaves"));
        game.Step("BillLeaves");
        Assert.Equal("Kanto", game.Map.Name);
        Assert.Equal("cinnabar_island", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_CINNABAR_ISLAND"));
        Assert.True(game.Story.Has("FLAG_HIDE_CINNABAR_BILL"));
        Assert.True(game.Story.Has("FLAG_HIDE_ONE_ISLAND_POKECENTER_BILL"));
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        // Lostelle is home on Three Island from now on
        Assert.False(game.Story.Has("FLAG_HIDE_LOSTELLE_IN_HER_HOME"));
        game.Arrive("TwoIsland_JoyfulGameCorner", 5, 7);
        Assert.Null(game.Present("joyful_game_corner_lostelle"));
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        Assert.Null(game.Present("one_island_pokemon_center_bill"));
    }

    [Fact]
    public void OakGivesTheNationalPokedexToAChampionWhoHasBeenToTheIslandsAndCaughtSixty()
    {
        var game = WithBadges(8);
        game.Story.SetVar("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 6);
        game.Story.Unset("FLAG_HIDE_OAK_IN_HIS_LAB");
        game.Story.Set("FLAG_HIDE_RIVAL_IN_LAB");
        game.Caught = 60;
        game.Arrive("PalletTown_ProfessorOaksLab", 6, 10);
        // Not before the Hall of Fame
        game.Talk("oaks_lab_prof_oak");
        Assert.False(game.NationalDex);
        // Nor before the islands
        game.Story.Set("FLAG_SYS_GAME_CLEAR");
        game.Talk("oaks_lab_prof_oak");
        Assert.False(game.NationalDex);
        // Nor with fewer than sixty caught: Kanto's own rule, which the Pokédex asks too
        game.Story.SetVar("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F", 1);
        game.Caught = 59;
        game.Talk("oaks_lab_prof_oak");
        Assert.False(game.NationalDex);
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        var pokedex = new Pokedex();
        for (int n = 1; n <= 59; n++) pokedex.RegisterCaught(n);
        Assert.False(pokedex.CanUnlockNational(game.Story, kanto));
        pokedex.RegisterCaught(60);
        Assert.True(pokedex.CanUnlockNational(game.Story, kanto));
        // Sixty, and the rival comes in for his too
        game.Caught = 60;
        var upgrade = game.Talk("oaks_lab_prof_oak");
        Assert.True(game.NationalDex);
        Assert.Contains(upgrade.Log, l => l.Contains("nationaldex"));
        Assert.True(game.Story.Has("FLAG_HIDE_RIVAL_IN_LAB"));
        Assert.Null(game.Present("oaks_lab_rival"));
        Assert.DoesNotContain(game.Talk("oaks_lab_prof_oak").Log, l => l.Contains("nationaldex"));
    }

    [Fact]
    public void CelioAsksAChampionWithTheNationalPokedexForTheRubyAndGivesTheRainbowPassForIt()
    {
        var game = AtOneIsland();
        game.Story.SetVar("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F", 3);
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        Assert.Null(game.Present("one_island_pokemon_center_bill"));
        // Without the National Pokédex he only talks of his machine, and Mt. Ember's Ruby Path stays shut
        game.Talk("celio");
        Assert.Equal(3, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        Assert.False(game.Story.Has("FLAG_MT_EMBER_RUBY_PATH_OPEN"));
        game.Arrive("MtEmber_Exterior", 41, 44);
        Assert.Null(game.Present("mt_ember_grunt1"));
        // With it he asks for the Ruby, which opens the path and brings Team Rocket to the mountain
        game.NationalDex = true;
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        game.Talk("celio");
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        Assert.True(game.Story.Has("FLAG_MT_EMBER_RUBY_PATH_OPEN"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_MT_EMBER_EXTERIOR"));
        game.Arrive("MtEmber_Exterior", 41, 44);
        Assert.NotNull(game.Present("mt_ember_grunt1"));
        Assert.NotNull(game.Present("mt_ember_grunt2"));
        Assert.True(game.Fires("Password"));
        game.Step("Password");
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_MT_EMBER_EXTERIOR"));
        Assert.Contains(game.Talk("mt_ember_grunt1").Log, l => l.StartsWith("battle kanto_team_rocket_grunt_43 Won"));
        Assert.NotNull(game.Present("mt_ember_grunt2"));
        Assert.Contains(game.Talk("mt_ember_grunt2").Log, l => l.StartsWith("battle kanto_team_rocket_grunt_44 Won"));
        Assert.True(game.Story.Has("FLAG_HIDE_MT_EMBER_EXTERIOR_ROCKETS"));
        Assert.Null(game.Present("mt_ember_grunt1"));
        // The Ruby at the path's deepest room, picked up once
        game.Arrive("MtEmber_RubyPath_B5F", 7, 9);
        game.Talk("ruby");
        Assert.Equal(1, Count(game, "Ruby"));
        Assert.True(game.Story.Has("FLAG_GOT_RUBY"));
        game.Arrive("MtEmber_RubyPath_B5F", 7, 9);
        Assert.Null(game.Present("ruby"));
        // Celio asks a second favour: refused, he waits; agreed to, he takes the Ruby and the Tri-Pass for the Rainbow Pass
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        game.Answers.Enqueue(1);
        game.Talk("celio");
        Assert.Equal(1, Count(game, "Ruby"));
        Assert.Equal(4, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        game.Answers.Enqueue(0);
        game.Talk("celio");
        Assert.Equal(0, Count(game, "Ruby"));
        Assert.Equal(0, Count(game, "Tri-Pass"));
        Assert.Equal(1, Count(game, "Rainbow Pass"));
        Assert.Equal(5, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        // The harbours' sailors take the Rainbow Pass to every port (part b): the waters first, then the port
        game.Arrive("OneIsland_Harbor", 8, 5);
        game.Answers.Enqueue(1);
        game.Answers.Enqueue(2);
        game.Talk("sailor");
        Assert.Equal("ThreeIsland_Harbor", game.Map.Name);
        game.Answers.Enqueue(1);
        game.Answers.Enqueue(0);
        game.Talk("sailor");
        Assert.Equal("OneIsland_Harbor", game.Map.Name);
    }

    // ------------------------------------------------------------------ plan 19 · K9, part b: the last islands and the Sapphire

    /// <summary>A Champion of Kanto with the National Pokédex who has given Celio the Ruby for the Rainbow Pass (part a).</summary>
    private Game WithTheRainbowPass()
    {
        var game = AtOneIsland();
        game.NationalDex = true;
        game.Story.SetVar("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F", 5);
        game.Bag.AddItem(ItemDatabase.Get("Rainbow Pass")!);
        return game;
    }

    [Fact]
    public void TheRainbowPassSailsToFourToSevenIslandAndTheRivalMeetsThePlayerOnFourIsland()
    {
        var game = WithTheRainbowPass();
        game.Arrive("OneIsland_Harbor", 8, 5);
        // The far islands, from each harbour to the next
        foreach (var (n, harbor) in new[] { (0, "FourIsland_Harbor"), (1, "FiveIsland_Harbor"), (2, "SixIsland_Harbor"), (3, "SevenIsland_Harbor") })
        {
            game.Answers.Enqueue(2);
            game.Answers.Enqueue(n);
            game.Talk("sailor");
            Assert.Equal(harbor, game.Map.Name);
        }
        // The ship is moored at Seven Island already, and from there it sails back to One Island and to Vermilion City
        game.Answers.Enqueue(2);
        game.Answers.Enqueue(3);
        var here = game.Talk("sailor");
        Assert.Equal("SevenIsland_Harbor", game.Map.Name);
        Assert.Contains(here.Transcript, l => l.Text.Contains("moored right here"));
        game.Answers.Enqueue(1);
        game.Answers.Enqueue(0);
        game.Talk("sailor");
        Assert.Equal("OneIsland_Harbor", game.Map.Name);
        // Four Island: the rival comes out of the Day Care's way on the first visit, talks of his Egg and goes
        game.Answers.Enqueue(2);
        game.Answers.Enqueue(0);
        game.Talk("sailor");
        game.Through("FourIsland");
        Assert.Equal("FourIsland", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_FOUR_ISLAND"));
        Assert.Contains(game.Played, p => p.Host.Transcript.Any(l => l.Text.Contains("found an Egg")));
        Assert.True(game.Story.Has("FLAG_HIDE_FOUR_ISLAND_RIVAL"));
        Assert.Null(game.Present("four_island_rival"));
        int scenes = game.Played.Count(p => p.Host.Transcript.Any(l => l.Text.Contains("found an Egg")));
        game.Arrive("FourIsland", 8, 26);
        Assert.Equal(scenes, game.Played.Count(p => p.Host.Transcript.Any(l => l.Text.Contains("found an Egg"))));
        Assert.Null(game.Present("four_island_rival"));
    }

    [Fact]
    public void MetFirstInSixIslandsPokemonCenterTheRivalIsNoLongerWaitingOnFourIsland()
    {
        var game = WithTheRainbowPass();
        game.Arrive("SixIsland_PokemonCenter_1F", 7, 8);
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_SIX_ISLAND_POKEMON_CENTER_1F"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_FOUR_ISLAND"));
        Assert.Null(game.Present("six_island_rival"));
        game.Arrive("FourIsland", 8, 26);
        Assert.Null(game.Present("four_island_rival"));
        Assert.DoesNotContain(game.Last.Transcript, l => l.Text.Contains("found an Egg"));
    }

    [Fact]
    public void TheSapphireIsFoundStolenAndWonBackFromTeamRocketsWarehouseAndCelioLinksTheIslands()
    {
        var game = WithTheRainbowPass();
        // Ruin Valley's scientist stands before the dotted door until the poachers are driven out of Icefall Cave
        game.Arrive("SixIsland", 48, 187);
        Assert.NotNull(game.Present("ruin_valley_scientist"));
        game.Arrive("FourIsland_IcefallCave_Back", 12, 19);
        Assert.NotNull(game.Present("icefall_lorelei"));
        Assert.NotNull(game.Present("icefall_rocket3"));
        Assert.True(game.Fires("Poachers"));
        var poachers = game.Step("Poachers");
        Assert.Contains(poachers.Log, l => l.StartsWith("battle kanto_team_rocket_grunt_45 Won"));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ICEFALL_CAVE_BACK"));
        Assert.True(game.Story.Has("FLAG_HIDE_ICEFALL_CAVE_ROCKETS"));
        Assert.True(game.Story.Has("FLAG_HIDE_RUIN_VALLEY_SCIENTIST"));
        Assert.Null(game.Present("icefall_rocket3"));
        Assert.False(game.Fires("Poachers"));
        game.Arrive("FourIsland_IcefallCave_Back", 12, 19);
        Assert.Null(game.Present("icefall_lorelei"));
        // Lorelei has gone home to her house of dolls
        game.Arrive("FourIsland_LoreleisHouse", 7, 6);
        Assert.NotNull(game.Present("lorelei"));
        game.Talk("lorelei");
        Assert.False(game.Story.Has("FLAG_TALKED_TO_LORELEI_AFTER_WAREHOUSE"));
        // The warehouse's keypad wants two passwords: Mt. Ember's grunts gave the first, and nobody yet the second
        game.Arrive("FiveIsland", 84, 62);
        var keypad = game.Map.TileScripts.Single(kv => kv.Value == "WarehouseDoor").Key;
        var warehouseDoor = Scripts.Find("WarehouseDoor", game.Map.ScriptFileAt(keypad.X, keypad.Y))!;
        Assert.Contains(game.Play(warehouseDoor).Transcript, l => l.Text.Contains("second password"));
        Assert.False(game.Story.Has("FLAG_UNLOCKED_ROCKET_WAREHOUSE"));
        // The dotted door spells CUT: read without the move it stays shut, and Cut opens it
        game.Arrive("SixIsland", 48, 187);
        Assert.Null(game.Present("ruin_valley_scientist"));
        var dots = game.Map.TileScripts.Single(kv => kv.Value == "DottedHoleDoor").Key;
        var dottedDoor = Scripts.Find("DottedHoleDoor", game.Map.ScriptFileAt(dots.X, dots.Y))!;
        Assert.DoesNotContain(game.Party.Members, p => p.Knows("Cut"));
        game.Answers.Enqueue(0);
        Assert.Contains(game.Play(dottedDoor).Transcript, l => l.Text.Contains("CUT"));
        Assert.False(game.Story.Has("FLAG_USED_CUT_ON_RUIN_VALLEY_BRAILLE"));
        game.Party.Members[0].Moves[0] = new Move(MoveDatabase.Get("Cut")!);
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(0);
        var cut = game.Play(dottedDoor);
        Assert.Contains(cut.Log, l => l.StartsWith("usemove Cut"));
        Assert.True(game.Story.Has("FLAG_USED_CUT_ON_RUIN_VALLEY_BRAILLE"));
        // At the bottom of the Dotted Hole the Sapphire shines, and the scientist who followed the player snatches it
        game.Through("SixIsland_DottedHole_1F");
        Assert.Equal("SixIsland_DottedHole_1F", game.Map.Name);
        game.Arrive("SixIsland_DottedHole_SapphireRoom", 7, 9);
        Assert.NotNull(game.Present("sapphire"));
        Assert.Null(game.Present("dotted_hole_thief"));
        var stolen = game.Talk("sapphire");
        Assert.Contains(stolen.Transcript, l => l.Text.Contains("Ekans ate the cake"));
        Assert.Equal(0, Count(game, "Sapphire"));
        Assert.True(game.Story.Has("FLAG_HIDE_SAPPHIRE"));
        Assert.True(game.Story.Has("FLAG_LEARNED_YES_NAH_CHANSEY"));
        Assert.Null(game.Present("sapphire"));
        Assert.Null(game.Present("dotted_hole_thief"));
        // Both passwords, and the warehouse opens
        game.Arrive("FiveIsland", 84, 62);
        game.Play(warehouseDoor);
        Assert.True(game.Story.Has("FLAG_UNLOCKED_ROCKET_WAREHOUSE"));
        Assert.Contains(game.Play(warehouseDoor).Transcript, l => l.Text.Contains("is unlocked"));
        game.Through("FiveIsland_RocketWarehouse");
        Assert.Equal("FiveIsland_RocketWarehouse", game.Map.Name);
        // The admin calls the player over, is battled, and Team Rocket leaves the islands' Pokémon behind
        Assert.True(game.Fires("Admin2Trigger"));
        game.Step("Admin2Trigger");
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_ROCKET_WAREHOUSE"));
        Assert.False(game.Fires("Admin2Trigger"));
        var admin = game.Talk("warehouse_admin2");
        Assert.Contains(admin.Log, l => l.StartsWith("battle kanto_team_rocket_admin_2 Won"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_ROCKETS_IN_WAREHOUSE"));
        Assert.True(game.Story.Has("FLAG_HIDE_FIVE_ISLAND_ROCKETS"));
        Assert.Null(game.Present("warehouse_admin2"));
        Assert.Null(game.Present("warehouse_grunt2"));
        // Gideon, the scientist who stole it, battles for it and gives it up
        var gideon = game.Talk("scientist");
        Assert.Contains(gideon.Log, l => l.StartsWith("battle kanto_scientist_gideon Won"));
        Assert.Equal(1, Count(game, "Sapphire"));
        Assert.True(game.Story.Has("FLAG_RECOVERED_SAPPHIRE"));
        Assert.DoesNotContain(game.Talk("scientist").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, Count(game, "Sapphire"));
        // Lorelei has heard, and thanks the player once
        game.Arrive("FourIsland_LoreleisHouse", 7, 6);
        game.Talk("lorelei");
        Assert.True(game.Story.Has("FLAG_TALKED_TO_LORELEI_AFTER_WAREHOUSE"));
        // Celio sets the Sapphire beside the Ruby: the machine links the islands with Hoenn, and the world after the story opens
        game.Arrive("OneIsland_PokemonCenter_1F", 9, 8);
        var linked = game.Talk("celio");
        Assert.Contains(linked.Transcript, l => l.Text.Contains("Hoenn"));
        Assert.Equal(0, Count(game, "Sapphire"));
        Assert.Equal(6, game.Story.Var("VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F"));
        Assert.True(game.Story.Has("FLAG_SYS_CAN_LINK_WITH_RS"));
        Assert.True(game.Story.Has("FLAG_HIDE_CERULEAN_CAVE_GUARD"));
        Assert.DoesNotContain(game.Talk("celio").Transcript, l => l.Text.Contains("handed the Sapphire"));
        // Lorelei has gone back to the League, Gideon speaks of the boss's son, and Cerulean Cave's guard has gone
        game.Arrive("FourIsland_LoreleisHouse", 7, 6);
        Assert.Null(game.Present("lorelei"));
        game.Arrive("FiveIsland_RocketWarehouse", 24, 24);
        Assert.Contains(game.Talk("scientist").Transcript, l => l.Text.Contains("son"));
        game.Arrive("Kanto", 287, 60);
        Assert.Null(game.Present("cerulean_cave_guard"));
    }

    [Fact]
    public void CeruleanCavesGuardStandsUntilTheSapphireIsBack()
    {
        var game = WithTheRainbowPass();
        game.Arrive("Kanto", 287, 60);
        Assert.NotNull(game.Present("cerulean_cave_guard"));
        game.Story.Set("FLAG_HIDE_CERULEAN_CAVE_GUARD");
        game.Arrive("Kanto", 287, 60);
        Assert.Null(game.Present("cerulean_cave_guard"));
    }

    [Fact]
    public void MewtwoWaitsAtTheBottomOfCeruleanCaveAtLevelSeventyAndIsGoneForGoodOnceFought()
    {
        var game = WithTheRainbowPass();
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.NotNull(game.Present("mewtwo"));
        game.Fight = BattleOutcome.Fled;
        var fled = game.Talk("mewtwo");
        Assert.Contains(fled.Log, l => l.StartsWith("wildbattle Mewtwo 70"));
        Assert.Null(game.Present("mewtwo"));
        Assert.False(game.Story.Has("FLAG_FOUGHT_MEWTWO"));
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.NotNull(game.Present("mewtwo"));
        game.Fight = BattleOutcome.Won;
        game.Talk("mewtwo");
        Assert.True(game.Story.Has("FLAG_FOUGHT_MEWTWO"));
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.Null(game.Present("mewtwo"));
    }

    [Fact]
    public void TheTanobyKeysSevenSwitchesOpenTheChambersOnceAllArePressed()
    {
        var game = WithTheRainbowPass();
        game.Arrive("SevenIsland_SevaultCanyon_TanobyKey", 7, 12);
        Assert.Equal(7, game.Map.BoulderPits.Count);
        for (int n = 1; n <= 7; n++)
            Assert.Contains($"Switch{n}", game.Map.BoulderPits.Values);
        var file = game.Map.ScriptFileAt(7, 12);
        // Six pressed, and nothing yet; the same one pressed twice counts once
        for (int n = 1; n <= 6; n++) game.Play(Scripts.Find($"Switch{n}", file)!);
        game.Play(Scripts.Find("Switch1", file)!);
        Assert.False(game.Story.Has("FLAG_SYS_UNLOCKED_TANOBY_RUINS"));
        var last = game.Play(Scripts.Find("Switch7", file)!);
        Assert.True(game.Story.Has("FLAG_SYS_UNLOCKED_TANOBY_RUINS"));
        Assert.Contains(last.Transcript, l => l.Text.Contains("rumble"));
        // The chambers' doors are open from then on
        game.Arrive("SevenIsland", 55, 59);
        var chambers = game.Map.Warps.Where(w => w.TargetMap.StartsWith("SevenIsland_TanobyRuins_")).ToList();
        Assert.Equal(7, chambers.Count);
        Assert.All(chambers, door => Assert.Equal("FLAG_SYS_UNLOCKED_TANOBY_RUINS", door.OpenedBy));
        // A switch left half done is forgotten as the place is left (its flags are the place's own)
        var other = WithTheRainbowPass();
        other.Arrive("SevenIsland_SevaultCanyon_TanobyKey", 7, 12);
        other.Play(Scripts.Find("Switch1", file)!);
        Assert.True(other.Story.Has("FLAG_MAP_LOCAL_TANOBY_KEY_SWITCH_1"));
        other.Story.ClearLocal();
        Assert.False(other.Story.Has("FLAG_MAP_LOCAL_TANOBY_KEY_SWITCH_1"));
    }

    [Theory]
    [InlineData("FourIsland_House1", 8, 5, "fat_man", "Snorlax", "Body Slam")]
    [InlineData("SevenIsland", 55, 59, "swords_dance_tutor", "Scyther", "Swords Dance")]
    public void TheLastIslandsTutorsTeachTheirMoveOnce(string map, int x, int y, string who, string species, string move)
    {
        var game = WithTheRainbowPass();
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get(species)!, 30));
        game.Party.Members[0].Moves.RemoveAll(m => m.Name == move);
        while (game.Party.Members[0].Moves.Count > 3) game.Party.Members[0].Moves.RemoveAt(0);
        game.Arrive(map, x, y);
        game.Answers.Enqueue(0);
        var taught = game.Talk(who);
        Assert.True(game.Party.Members[0].Knows(move));
        Assert.True(game.Story.Has(MoveTutors.KantoFlag(move)));
        Assert.Contains(taught.Transcript, l => l.Text.Contains($"learned {move}"));
        Assert.Empty(game.Talk(who).Asked);
    }

    [Fact]
    public void CapeBrinksOldWomanTeachesAFullyGrownStarterThatTrustsItsTrainerItsStrongestMove()
    {
        var game = WithTheRainbowPass();
        var venusaur = new Pokemon(PokemonDatabase.Get("Venusaur")!, 50);
        while (venusaur.Moves.Count > 3) venusaur.Moves.RemoveAt(0);
        game.Party.Members.Insert(0, venusaur);
        game.Arrive("TwoIsland_CapeBrink_House", 4, 6);
        // Not yet: it doesn't trust its trainer enough
        venusaur.Friendship = 200;
        game.Answers.Enqueue(0);
        var close = game.Talk("cape_brink_tutor");
        Assert.Contains(close.Transcript, l => l.Text.Contains("trusts its trainer"));
        Assert.False(venusaur.Knows("Frenzy Plant"));
        // At full friendship, Frenzy Plant
        venusaur.Friendship = 255;
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(0);
        game.Talk("cape_brink_tutor");
        Assert.True(venusaur.Knows("Frenzy Plant"));
        // A Pokémon that isn't a starter grown to its last form can learn nothing of hers
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Rattata")!, 20) { Friendship = 255 });
        game.Answers.Enqueue(0);
        Assert.Contains(game.Talk("cape_brink_tutor").Transcript, l => l.Text.Contains("can't learn any of my moves"));
    }

    [Fact]
    public void TheWaterLabyrinthsGentlemanGivesAnEggToATrainerWhosePokemonLovesThemDearly()
    {
        var game = WithTheRainbowPass();
        game.Arrive("FiveIsland", 14, 32);
        var gentleman = game.Map.NPCs.Single(n => n.Key == "gentleman" && game.Map.AreaAt(n.GridX, n.GridY)?.Key == "five_island_water_labyrinth");
        // Not yet: the lead's friendship is short of the most
        game.Party.Members[0].Friendship = 254;
        game.Talk(gentleman);
        Assert.False(game.Story.Has("FLAG_GOT_TOGEPI_EGG"));
        // With a full team he asks the player to come back, and gives nothing
        game.Party.Members[0].Friendship = 255;
        while (game.Party.Members.Count < 6) game.Party.Members.Add(new Pokemon(PokemonDatabase.Get("Rattata")!, 5));
        game.Talk(gentleman);
        Assert.False(game.Story.Has("FLAG_GOT_TOGEPI_EGG"));
        // With room, a Togepi Egg, once
        game.Party.Members.RemoveAt(5);
        game.Talk(gentleman);
        Assert.True(game.Story.Has("FLAG_GOT_TOGEPI_EGG"));
        var egg = Assert.Single(game.Party.Members, p => p.IsEgg);
        Assert.Equal("Togepi", egg.Species.Name);
        game.Talk(gentleman);
        Assert.Single(game.Party.Members, p => p.IsEgg);
    }

    [Fact]
    public void TheMemorialPillarTakesALemonadeForTheOnixAndItsOwnerGivesTmFortyTwo()
    {
        var game = WithTheRainbowPass();
        game.Arrive("FiveIsland", 104, 106);
        var stone = game.Map.TileScripts.Single(kv => kv.Value == "Memorial").Key;
        var memorial = Scripts.Find("Memorial", game.Map.ScriptFileAt(stone.X, stone.Y))!;
        // Nothing to leave without Lemonade
        game.Play(memorial);
        Assert.False(game.Story.Has("FLAG_GOT_TM42_AT_MEMORIAL_PILLAR"));
        game.Bag.AddItem(ItemDatabase.Get("Lemonade")!, 2);
        // Asked, and refused
        game.Answers.Enqueue(1);
        game.Play(memorial);
        Assert.Equal(2, Count(game, "Lemonade"));
        // Asked, and left
        game.Answers.Enqueue(0);
        game.Play(memorial);
        Assert.Equal(1, Count(game, "Lemonade"));
        Assert.Equal(1, Count(game, "TM42"));
        Assert.True(game.Story.Has("FLAG_GOT_TM42_AT_MEMORIAL_PILLAR"));
        game.Play(memorial);
        Assert.Equal(1, Count(game, "Lemonade"));
        Assert.Equal(1, Count(game, "TM42"));
    }

    [Fact]
    public void SelphyIsFoundInTheLostCaveBattledAndTakenHomeToResortGorgeous()
    {
        var game = WithTheRainbowPass();
        // At Resort Gorgeous she is nowhere to be seen, and in her house only her butler waits
        game.Arrive("FiveIsland", 87, 10);
        Assert.Null(game.Present("resort_gorgeous_selphy"));
        game.Arrive("FiveIsland_ResortGorgeous_House", 4, 6);
        Assert.Null(game.Present("resort_gorgeous_house_selphy"));
        Assert.NotNull(game.Present("resort_gorgeous_house_butler"));
        // Deep in the Lost Cave she battles the player and is led home: the two come out before her door, and she goes in
        game.Arrive("FiveIsland_LostCave_Room10", 5, 7);
        Assert.Contains(game.Played, p => p.Host.Log.Any(l => l.StartsWith("battle kanto_lady_selphy Won")));
        Assert.Equal(1, game.Story.Var("VAR_MAP_SCENE_FIVE_ISLAND_LOST_CAVE_ROOM10"));
        Assert.Equal("FiveIsland", game.Map.Name);
        Assert.Equal("five_island_resort_gorgeous", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        Assert.Equal(2, game.Story.Var("VAR_MAP_SCENE_FIVE_ISLAND_RESORT_GORGEOUS"));
        Assert.Null(game.Present("resort_gorgeous_selphy"));
        // The cave is empty afterwards, and she is at home
        game.Arrive("FiveIsland_LostCave_Room10", 5, 7);
        Assert.Null(game.Present("lost_cave_selphy"));
        game.Arrive("FiveIsland_ResortGorgeous_House", 4, 6);
        Assert.NotNull(game.Present("resort_gorgeous_house_selphy"));
    }

    // ------------------------------------------------------------------ plan 19 · K9, part c

    [Fact]
    public void TheProspectorsTunnelIsDugThroughToTheHarbourStepsOnceTheNationalPokedexIsHad()
    {
        var game = WithTheRainbowPass();
        game.NationalDex = false;
        // Without the National Pokédex the tunnel is a dead end: the prospector stands at the wall he is working, his
        // own tile at the far end is empty, and the port's east steps are shut
        game.Arrive("ThreeIsland_DunsparceTunnel", 3, 4);
        Assert.False(game.Story.Has("FLAG_THREE_ISLAND_TUNNEL_DUG_OUT"));
        Assert.Equal((6, 3), WhereIs(game, "prospector_digging"));
        Assert.Null(game.Present("prospector"));
        Assert.Contains(game.Talk("prospector_digging").Transcript, l => l.Text.Contains("for gold"));
        Assert.True(game.Map.Warps.Single(w => w.TargetMap == "ThreeIsland" && w.SourceX == 25).Shut);
        // With it, the dig breaks through: the chunk is swapped for the dug-out one, the prospector stands at his own
        // end with a Nugget to give, and both ways in are open
        game.NationalDex = true;
        game.Arrive("ThreeIsland_DunsparceTunnel", 3, 4);
        Assert.True(game.Story.Has("FLAG_THREE_ISLAND_TUNNEL_DUG_OUT"));
        Assert.Null(game.Present("prospector_digging"));
        Assert.Equal((23, 2), WhereIs(game, "prospector"));
        Assert.False(game.Map.Warps.Single(w => w.TargetMap == "ThreeIsland" && w.SourceX == 25).Shut);
        int had = Count(game, "Nugget");
        game.Talk("prospector");
        Assert.Equal(had + 1, Count(game, "Nugget"));
        Assert.True(game.Story.Has("FLAG_GOT_NUGGET_FROM_DUNSPARCE_TUNNEL"));
        // ...once only, and the port's own side leads into the tunnel from then on
        game.Talk("prospector");
        Assert.Equal(had + 1, Count(game, "Nugget"));
        game.Arrive("ThreeIsland", 112, 46);
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "ThreeIsland_DunsparceTunnel" && !w.Shut);
    }

    private static (int X, int Y)? WhereIs(Game game, string key) => game.Present(key) is { } who ? (who.GridX, who.GridY) : null;

    [Fact]
    public void TheWaterPathsBeautyMeasuresHeracrossAndKeepsTheBiggestOnHerChart()
    {
        var game = WithTheRainbowPass();
        game.Arrive("SixIsland_WaterPath_House1", 3, 6);
        var chart = Scripts.Find("Chart", "six_island_water_path_house_1")!;
        // With no Heracross she only talks, and her chart is blank
        Assert.Contains(game.Talk("beauty").Transcript, l => l.Text.Contains("most marvellous"));
        Assert.Contains(game.Play(chart).Transcript, l => l.Text.Contains("Nothing is written"));
        // The first Heracross shown beats a record of nothing: a Nest Ball, and the size on the chart
        var small = Heracross(0x11110111);
        game.Party.Members.Insert(0, small);
        game.Talk("beauty");
        Assert.Equal(1, Count(game, "Nest Ball"));
        Assert.True(game.Story.Has("FLAG_GOT_NEST_BALL_FROM_WATER_PATH_HOUSE_1"));
        int kept = game.Story.Var("VAR_HERACROSS_SIZE_RECORD");
        Assert.Equal(SizeContest.Factor(small), kept);
        Assert.Equal(0, game.Story.Var(SizeContest.RecordVariable));
        Assert.Contains(game.Play(chart).Transcript, l => l.Text.Contains(SizeContest.Told(SizeContest.Millimetres(small))));
        // The same one again is the same size, and changes nothing
        Assert.Contains(game.Talk("beauty").Transcript, l => l.Text.Contains("to the millimetre"));
        Assert.Equal((1, kept), (Count(game, "Nest Ball"), game.Story.Var("VAR_HERACROSS_SIZE_RECORD")));
        // A smaller one earns nothing
        game.Party.Members[0] = Heracross(0);
        Assert.Contains(game.Talk("beauty").Transcript, l => l.Text.Contains("does not lie"));
        Assert.Equal((1, kept), (Count(game, "Nest Ball"), game.Story.Var("VAR_HERACROSS_SIZE_RECORD")));
        // A bigger one earns another ball and takes the record
        var big = Heracross(0xFFFFFFFF);
        game.Party.Members[0] = big;
        game.Talk("beauty");
        Assert.Equal((2, SizeContest.Factor(big)), (Count(game, "Nest Ball"), game.Story.Var("VAR_HERACROSS_SIZE_RECORD")));
        // Shown something else, she measures nothing (she asks only while a Heracross is on the team, as FireRed's does)
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Pinsir")!, 30));
        Assert.Contains(game.Talk("beauty").Transcript, l => l.Text.Contains("nothing else"));
        // ...and a refusal is a refusal
        game.Party.Members.RemoveAt(0);
        game.PokemonChoice = 255;
        Assert.Contains(game.Talk("beauty").Transcript, l => l.Text.Contains("my tape ready"));
    }

    // A size is the personality's low half with the IVs' low four bits worked into it (SizeContest.Factor), so the
    // Heracross shown have IVs of nought, or a smaller personality could still come to the bigger size
    private static Pokemon Heracross(uint personality) => new(PokemonDatabase.Get("Heracross")!, 30)
    {
        Personality = personality, IvHP = 0, IvAttack = 0, IvDefense = 0, IvSpeed = 0, IvSpAttack = 0, IvSpDefense = 0
    };

    [Fact]
    public void TheFishingBrotherOnRouteTwelveMeasuresMagikarpOnceHisSuperRodIsGiven()
    {
        var game = WithTheRainbowPass();
        game.Story.Unset("FLAG_GOT_SUPER_ROD");
        game.Arrive("Route12_FishingHouse", 4, 6);
        var board = Scripts.Find("Board", "route_12_fishing_house")!;
        var karp = new Pokemon(PokemonDatabase.Get("Magikarp")!, 15) { Personality = 0x5555AAAA };
        game.Party.Members.Insert(0, karp);
        // Before the rod he only offers it, measuring nothing, and his board is blank
        game.Answers.Enqueue(0);
        game.Talk("fisher");
        Assert.Equal(1, Count(game, "Super Rod"));
        Assert.Equal(0, game.Story.Var("VAR_MAGIKARP_SIZE_RECORD"));
        Assert.Contains(game.Play(board).Transcript, l => l.Text.Contains("Not one is written up"));
        // With the rod given, the Magikarp on the team is measured: the first is the record, and earns a Net Ball
        game.Answers.Enqueue(0);
        game.Talk("fisher");
        Assert.Equal(1, Count(game, "Net Ball"));
        Assert.True(game.Story.Has("FLAG_GOT_RECORD_SETTING_MAGIKARP"));
        Assert.Equal(SizeContest.Factor(karp), game.Story.Var("VAR_MAGIKARP_SIZE_RECORD"));
        Assert.Contains(game.Play(board).Transcript, l => l.Text.Contains(SizeContest.Told(SizeContest.Millimetres(karp))));
        // His record and the Heracross woman's are each their own
        Assert.Equal(0, game.Story.Var("VAR_HERACROSS_SIZE_RECORD"));
        // With no Magikarp he asks for one
        game.Party.Members.RemoveAt(0);
        Assert.Contains(game.Talk("fisher").Transcript, l => l.Text.Contains("bring it to me"));
    }

    [Fact]
    public void SelphyWishesForAPokemonThePokedexHasSeenAndHerButlerPaysForIt()
    {
        var game = WithTheRainbowPass();
        game.Story.SetVar("VAR_MAP_SCENE_FIVE_ISLAND_RESORT_GORGEOUS", 2);
        game.Seen.Add("Lapras");
        game.Arrive("FiveIsland_ResortGorgeous_House", 4, 6);
        // Her wish is a species the Pokédex has seen, kept by its National Pokédex number, and the gift is drawn with
        // it: a hundred's draw of 30 or more is a Luxury Ball
        game.Rolls.Enqueue(0);
        game.Rolls.Enqueue(40);
        var asked = game.Talk("resort_gorgeous_house_selphy");
        Assert.Equal(PokemonDatabase.Get("Lapras")!.DexNumber, game.Story.Var("VAR_RESORT_GORGEOUS_REQUESTED_MON"));
        Assert.Equal(1, game.Story.Var("VAR_RESORT_GORGEOUS_REWARD"));
        Assert.Contains(asked.Transcript, l => l.Text.Contains("Lapras"));
        // Asked again she says the same wish, and draws nothing new
        game.Talk("resort_gorgeous_house_selphy");
        Assert.Equal(1, game.Story.Var("VAR_RESORT_GORGEOUS_REWARD"));
        // The butler is off the map until she calls him
        Assert.Null(game.Present("resort_gorgeous_house_butler"));
        // With the Pokémon in the team she calls him in and he hands over the gift, once
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Lapras")!, 30));
        var shown = game.Talk("resort_gorgeous_house_selphy");
        Assert.NotNull(game.Present("resort_gorgeous_house_butler"));
        Assert.Contains(shown.Transcript, l => l.Speaker == "Butler");
        Assert.Equal(1, Count(game, "Luxury Ball"));
        Assert.Equal(0, game.Story.Var("VAR_RESORT_GORGEOUS_REQUESTED_MON"));
        Assert.Contains(game.Talk("resort_gorgeous_house_selphy").Transcript, l => l.Text.Contains("best of luck"));
        Assert.Equal(1, Count(game, "Luxury Ball"));
        // The next visit wishes again, and a draw under 30 gives one of the six deluxe rewards instead
        game.Seen.Add("Dratini");
        game.Rolls.Enqueue(1);
        game.Rolls.Enqueue(10);
        game.Rolls.Enqueue(4);
        game.Story.ClearLocal();
        game.Arrive("FiveIsland_ResortGorgeous_House", 4, 6);
        game.Talk("resort_gorgeous_house_selphy");
        Assert.Equal(PokemonDatabase.Get("Dratini")!.DexNumber, game.Story.Var("VAR_RESORT_GORGEOUS_REQUESTED_MON"));
        Assert.Equal(6, game.Story.Var("VAR_RESORT_GORGEOUS_REWARD"));
        int nuggets = Count(game, "Nugget");
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Dratini")!, 15));
        game.Talk("resort_gorgeous_house_selphy");
        Assert.Equal(nuggets + 1, Count(game, "Nugget"));
    }

    [Fact]
    public void LoreleisDollsComeOneByOneWithTheTimesTheTeamHasEnteredTheHallOfFame()
    {
        var game = WithTheRainbowPass();
        game.Story.SetVar("VAR_MAP_SCENE_ICEFALL_CAVE_BACK", 1);
        // Six of her dolls always stand; the eight behind their flags wait for the entries FireRed asks of them
        var earned = new (int Entries, string Doll)[]
        {
            (25, "meowth"), (50, "chansey"), (75, "nidoran_f"), (100, "jigglypuff"),
            (125, "nidoran_m"), (150, "fearow"), (175, "pidgeot"), (200, "lapras_doll")
        };
        string[] always = { "wigglytuff", "seel", "pikachu", "slowpoke", "slowbro", "psyduck" };
        foreach (int entries in new[] { 0, 25, 50, 75, 100, 125, 150, 175, 200 })
        {
            game.Victories = entries;
            game.Arrive("FourIsland_LoreleisHouse", 7, 6);
            foreach (string doll in always) Assert.NotNull(game.Present(doll));
            foreach (var (needed, doll) in earned) Assert.Equal(entries >= needed, game.Present(doll) != null);
        }
        // A doll says what a doll says, and Lorelei is at home
        Assert.Contains(game.Talk("meowth").Transcript, l => l.Text.Contains("doll"));
        Assert.NotNull(game.Present("lorelei"));
    }
}
