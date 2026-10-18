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

        public HeadlessScriptHost Talk(string key)
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
                RivalName = Rival,
                Fight = _ => Fight,
                NeedsPokemon = true,
                Caught = Caught
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
}
