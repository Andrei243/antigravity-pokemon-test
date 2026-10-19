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
        public bool Impatient { get; init; }
        /// <summary>The script's dice to come (<c>random</c>) and the answers to its questions, handed on from one script to the next.</summary>
        public Queue<int> Rolls { get; } = new();
        public Queue<int> Answers { get; } = new();
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
                Caught = Caught
            };
            while (Rolls.Count > 0) host.Rolls.Enqueue(Rolls.Dequeue());
            while (Answers.Count > 0) host.Answers.Enqueue(Answers.Dequeue());
            var runner = new ScriptRunner(Scripts, host) { Impatient = Impatient };
            runner.Start(script, subject);
            runner.RunToEnd();
            while (host.Rolls.Count > 0) Rolls.Enqueue(host.Rolls.Dequeue());
            while (host.Answers.Count > 0) Answers.Enqueue(host.Answers.Dequeue());
            Played.Add((script.FullName, host));
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
}
