using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Poni's last places, Blush Mountain, Route 11 and the rides of the rough ground and the sea (plan 23, the wave after
/// Ultra Space; rulings, "Poni's last places, Blush Mountain, Route 11 and the rides of the rough ground and the sea"):
/// Poni Grove with Stakataka and Blacephalon, Poni Plains, Poni Gauntlet up to the Battle Tree's shut doors, Poni Meadow
/// with Resolution Cave and its Zygarde, Poni Coast; Route 11 and Blush Mountain on Ula'ula; Mudsdale over rough ground,
/// Sharpedo under a surfer ramming the sea's rocks; the Elite Four's rematches; the altar keeper's Cosmog. Played headless.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaPoniTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static readonly (string Map, string Area)[] NewPlaces =
    {
        ("Poni", "poni_grove"), ("Poni", "poni_plains"), ("Poni", "poni_gauntlet"), ("Poni", "poni_meadow"), ("Poni", "poni_coast"),
        ("ResolutionCave", "resolution_cave"), ("Ulaula", "ulaula_route_11"), ("Ulaula", "blush_mountain"),
    };

    [Fact]
    public void EveryNewPlaceIsReachedFromTheNewHome()
    {
        var walked = Walked.Value;
        foreach (var (map, key) in NewPlaces)
            Assert.True(walked.Any(p => p.Key.Name == map && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        // Every one of them is open, and Alola's
        foreach (var (map, key) in NewPlaces)
            Assert.Contains(MapNamed(map).Areas, a => a.Key == key && a.Open);
        Assert.Equal(RegionDatabase.Alola, RegionDatabase.RegionOfMap("ResolutionCave")?.Id);
    }

    [Fact]
    public void RoughGroundIsCrossedOnlyOnMudsdalesBack()
    {
        var poni = MapNamed("Poni");
        // The rough ground across Poni Gauntlet, from the path below it
        Assert.Equal(TileBehavior.RoughGround, poni.BehaviourAt(299, 95));
        float h = poni.HeightAt(299, 96);
        var onFoot = FieldMovement.Step(poni, 299, 96, Direction.Up, new Walker(TravelMode.OnFoot, h));
        Assert.False(onFoot.Moves);
        Assert.Equal(Obstacle.Rough, onFoot.Obstacle);
        Assert.False(FieldMovement.Step(poni, 299, 96, Direction.Up, new Walker(TravelMode.Cycling, h)).Moves);
        Assert.False(FieldMovement.Step(poni, 299, 96, Direction.Up, new Walker(TravelMode.Riding, h, Steed: "Tauros")).Moves);
        var mudsdale = FieldMovement.Step(poni, 299, 96, Direction.Up, new Walker(TravelMode.Riding, h, Steed: "Mudsdale"));
        Assert.True(mudsdale.Moves);
        Assert.Equal(Pace.Fast, mudsdale.Pace);
        // and before Blush Mountain and on Poni Coast too
        Assert.Equal(TileBehavior.RoughGround, MapNamed("Ulaula").BehaviourAt(120, 195));
        Assert.Equal(TileBehavior.RoughGround, poni.BehaviourAt(195, 255));
        Assert.True(RidePager.CrossesRoughGround("Mudsdale"));
        Assert.False(RidePager.CrossesRoughGround("Stoutland"));
    }

    [Fact]
    public void SharpedoIsCalledOnlyUnderASurferAndGoesAtABicyclesTopSpeed()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Alola, Map = MapNamed("Ulaula"), PlayerTile = (301, 296) };
        // Not registered yet
        host.PlayerMode = TravelMode.Surfing;
        Assert.False(host.RidePokemon("Sharpedo"));
        host.Story.Set(RidePager.SharpedoFlag);
        // Not on foot
        host.PlayerMode = TravelMode.OnFoot;
        Assert.False(host.RidePokemon("Sharpedo"));
        // Under a surfer
        host.PlayerMode = TravelMode.Surfing;
        Assert.True(host.RidePokemon("Sharpedo"));
        Assert.Equal("Sharpedo", host.Riding);
        Assert.Equal(TravelMode.Surfing, host.PlayerMode);
        // Sent home, the player goes on surfing
        Assert.True(host.RidePokemon(null));
        Assert.Equal(TravelMode.Surfing, host.PlayerMode);
        Assert.Null(host.Riding);

        Assert.Equal(Pace.Fastest, FieldMovement.PaceOn(TileBehavior.Sea, new Walker(TravelMode.Surfing, Steed: "Sharpedo")));
        Assert.Equal(Pace.Fast, FieldMovement.PaceOn(TileBehavior.Sea, new Walker(TravelMode.Surfing)));
    }

    [Fact]
    public void ThePlayersOwnSharpedoGoesHomeOffTheWater()
    {
        var player = new Player(0, 0) { Carrier = "Lapras" };
        player.SetMode(TravelMode.Surfing);
        Assert.True(player.SetJet("Sharpedo"));
        Assert.Equal("Sharpedo", player.Steed);
        Assert.Equal("Sharpedo", player.Carrier);
        Assert.True(player.SetJet(null));
        Assert.Null(player.Steed);
        Assert.Equal("Lapras", player.Carrier);
        // A Sharpedo is no ride on land
        player.SetMode(TravelMode.OnFoot);
        Assert.False(player.SetJet("Sharpedo"));
    }

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();
        public TravelMode Mode = TravelMode.OnFoot;
        public string? Riding;
        private readonly Dictionary<string, Map> built = LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name);
        private readonly Func<string, Map> rooms = MapDatabase.Fresh();
        private readonly Dictionary<string, Map> cache = new();

        public Game()
        {
            Story.ChooseStarter("Litten");
            Story.SetVar("VAR_ALOLA_STORY", 54);
            Story.SetVar("VAR_ALOLA_AKALA", 8);
            Story.Set("AlolaHallOfFame");
            Party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 60));
        }

        public Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);

        public HeadlessScriptHost Play(string file, string script, string map, string? person = null)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(Story, Party, Bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola, Money = 50000, PlayerMode = Mode };
            if (Riding != null) host.RidePokemon(Riding);
            var library = ScriptLibrary.Default;
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person && (at.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    [Fact]
    public void GrimsleyRegistersSharpedoAndItRamsTheRockOffRoute15()
    {
        var game = new Game();
        game.Play("ulaula_route_15", "Grimsley", "Ulaula", "grimsley");
        Assert.True(game.Story.Has(RidePager.SharpedoFlag));
        // A surfer on a Lapras's back is told what could ram it; on Sharpedo's back the rock falls
        var rock = game.Fresh("Ulaula").Everyone.Single(n => n.Key == "sea_rock" && n.ScriptFile == "ulaula_route_15");
        Assert.Equal(PropType.CrackedRock, rock.Obstacle);
        game.Mode = TravelMode.Surfing;
        var lapras = game.Play("ulaula_route_15", "SeaRock", "Ulaula", "sea_rock");
        Assert.DoesNotContain(lapras.Log, l => l.StartsWith("setflag"));
        Assert.False(game.Story.Has(rock.HiddenBy!));
        game.Riding = "Sharpedo";
        game.Play("ulaula_route_15", "SeaRock", "Ulaula", "sea_rock");
        Assert.True(game.Story.Has(rock.HiddenBy!));
        // The islet's rock on Poni Coast is the same
        Assert.Equal("SeaRock", game.Fresh("Poni").Everyone.Single(n => n.Key == "sea_rock" && n.ScriptFile == "poni_coast").Script);
    }

    [Fact]
    public void StakatakaAndBlacephalonWaitInPoniGroveOnceTheLightIsBackAndAreMetAfterTheirBeats()
    {
        var game = new Game();
        var grove = game.Fresh("Poni");
        var stakataka = grove.Everyone.Single(n => n.Key == "stakataka" && n.ScriptFile == "poni_grove");
        Assert.Equal("FLAG_ALOLA_LIGHT_RETURNED", stakataka.ShownBy);
        Assert.Equal("poni_grove", grove.AreaAt(stakataka.GridX, stakataka.GridY)?.Key);
        game.Story.Set("FLAG_ALOLA_LIGHT_RETURNED");
        Assert.Contains(game.Play("poni_grove", "Stakataka", "Poni", "stakataka").Log, l => l.StartsWith("wildbattle Stakataka 60"));
        Assert.Contains(game.Play("poni_grove", "Blacephalon", "Poni", "blacephalon").Log, l => l.StartsWith("wildbattle Blacephalon 60"));
        // Neither stands in Ultra Space any more
        Assert.DoesNotContain(game.Fresh("UltraMegalopolis").Everyone, n => n.Key == "blacephalon");
        Assert.DoesNotContain(game.Fresh("PoniAltar").Everyone, n => n.Key == "stakataka");
    }

    [Fact]
    public void TheGrovesRangerKeepsItsWayInUntilTheHallOfFame()
    {
        var ranger = MapNamed("Poni").Everyone.Single(n => n.Key == "ranger" && n.ScriptFile == "poni_grove");
        Assert.Equal("AlolaHallOfFame", ranger.HiddenBy);
        Assert.Equal((124, 190), (ranger.GridX, ranger.GridY));
        Assert.Contains("AlolaHallOfFame", WorldWalk.HiddenByTheStory.Value);
    }

    [Fact]
    public void ZygardeWaitsAtTheBottomOfResolutionCave()
    {
        var game = new Game();
        var cave = game.Play("resolution_cave", "Zygarde", "ResolutionCave", "legend");
        Assert.Contains(cave.Log, l => l.StartsWith("wildbattle Zygarde 60 nofleeing"));
        // and the cave is come into from Poni Meadow's rock
        var meadow = MapNamed("Poni");
        Assert.Contains(meadow.Warps, w => w.TargetMap == "ResolutionCave");
    }

    [Fact]
    public void TheAltarsKeeperGivesTheChampionACosmogOnce()
    {
        var game = new Game();
        game.Play("poni_altar", "Keeper", "PoniAltar", "keeper");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Cosmog");
        Assert.True(game.Story.Has("FLAG_ALOLA_COSMOG_GIVEN"));
        int count = game.Party.Members.Count;
        game.Play("poni_altar", "Keeper", "PoniAltar", "keeper");
        Assert.Equal(count, game.Party.Members.Count);
        // Cosmoem becomes Solgaleo by day and Lunala by night
        var cosmoem = PokemonDatabase.Get("Cosmoem")!;
        Assert.Contains(cosmoem.Evolutions, e => e.TargetSpecies == "Lunala" && e.Method == EvolutionMethod.LevelNight);
    }

    [Theory]
    [InlineData("AlolaLeague_HalaRoom", "hala")]
    [InlineData("AlolaLeague_OliviaRoom", "olivia")]
    [InlineData("AlolaLeague_AcerolaRoom", "acerola")]
    [InlineData("AlolaLeague_KahiliRoom", "kahili")]
    public void TheEliteFourFightTheirRematchTeamsOnEveryClimbAfterTheHallOfFame(string room, string who)
    {
        var game = new Game();
        var host = game.Play(room, "League", room, who);
        Assert.Contains(host.Log, l => l.StartsWith($"battle alola_{who}_rematch"));
        Assert.True(game.Story.Has($"FLAG_ALOLA_DEFEATED_{who.ToUpperInvariant()}"));
        var team = TrainerDatabase.Get($"alola_{who}_rematch", RegionDatabase.Alola)!;
        Assert.Equal(6, team.Party.Count);
        Assert.True(team.Party.Min(p => p.Level) >= 63);
        // Before it, the League's own team
        var first = new Game();
        first.Story.Unset("AlolaHallOfFame");
        Assert.Contains(first.Play(room, "League", room, who).Log, l => l.StartsWith($"battle alola_{who}_league"));
    }

    [Fact]
    public void EveryWildSlotOfAlolaInAFormIsOneOfItsSpeciesForms()
    {
        var world = LoadedWorld.Value;
        foreach (string key in world.Index.Areas)
        {
            var area = world.Area(key);
            if (area == null) continue;
            var slots = (area.Land ?? new()).Concat(area.Water ?? new()).Concat(area.OldRod ?? new()).Concat(area.GoodRod ?? new()).Concat(area.SuperRod ?? new());
            foreach (var slot in slots.Where(s => s.Form != null))
            {
                var species = PokemonDatabase.Get(slot.Species)!;
                Assert.True(species.Forms.Any(f => f.Name == slot.Form), $"{key}: {slot.Species} has no form {slot.Form}");
            }
        }
        // Ula'ula Meadow's Oricorio are the Baile Style, which is the species' own
        Assert.All(world.Area("ulaula_meadow")!.Land!.Where(s => s.Species == "Oricorio"), s => Assert.Null(s.Form));
        Assert.Contains(world.Area("poni_meadow")!.Land!, s => s.Form == "Oricorio-Sensu");
    }

    [Fact]
    public void ThePoniCoastsFishermanHandsOverASuperRodOnce()
    {
        var game = new Game();
        game.Play("poni_coast", "Fisher", "Poni", "fisher");
        var rod = ItemDatabase.Get("Super Rod")!;
        Assert.Equal(1, game.Bag.GetQuantity(rod));
        game.Play("poni_coast", "Fisher", "Poni", "fisher");
        Assert.Equal(1, game.Bag.GetQuantity(rod));
    }

    [Fact]
    public void ThePlacesOfEvolutionAreWhereSunAndMoonKeepThem()
    {
        var poni = MapNamed("Poni");
        Assert.Contains("Moss Rock", poni.EvolutionSitesAt(150, 190));
        Assert.Contains("Magnetic Field", MapNamed("Ulaula").EvolutionSitesAt(120, 170));
        Assert.Contains("Ice Rock", MapNamed("MountLanakila").EvolutionSitesAt(40, 40));
    }
}
