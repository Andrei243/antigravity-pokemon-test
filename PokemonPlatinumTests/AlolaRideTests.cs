using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola's Ride Pager on land (plan 23, wave 91; Z5's "the rider drawn and the rides' own travel modes"): the key item's
/// script calls a registered ride (<see cref="TravelMode.Riding"/>), which goes at a run's pace, is refused unregistered,
/// indoors and outside Alola, and a Tauros run into a cracked rock is stopped by it and told to the engine, whose
/// <c>common.TaurosCharge</c> breaks it.
/// </summary>
public class AlolaRideTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static Map MapNamed(string name) => BuiltMaps.Value[name];

    private static HeadlessScriptHost Pager(int answer, bool registered = true, string region = RegionDatabase.Alola)
    {
        var host = new HeadlessScriptHost { Region = region };
        if (registered) host.Story.Set("FLAG_ALOLA_RIDE_TAUROS");
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(PokemonPlatinumEngine.Core.GameEngine.RidePagerScript)!, null);
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void ThePagerCallsARegisteredRideAndSendsItHome()
    {
        var host = Pager(0);
        Assert.Empty(host.Problems);
        Assert.Contains("ride Tauros", host.Log);
        Assert.Equal("Tauros", host.Riding);
        Assert.Equal(TravelMode.Riding, host.PlayerMode);

        // Used again while riding, the ride is sent home
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(PokemonPlatinumEngine.Core.GameEngine.RidePagerScript)!, null);
        runner.RunToEnd();
        Assert.Contains("ride off", host.Log);
        Assert.Null(host.Riding);
        Assert.Equal(TravelMode.OnFoot, host.PlayerMode);
    }

    [Fact]
    public void ARideIsRefusedUnregisteredAndOutsideAlola()
    {
        // Stoutland's flag isn't set
        var stoutland = Pager(1);
        Assert.Contains("ride refused Stoutland", stoutland.Log);
        Assert.Null(stoutland.Riding);
        Assert.Contains(stoutland.Transcript, t => t.Text.StartsWith("Nothing came"));
        // Tauros is registered, but Sinnoh offers no riding
        var sinnoh = Pager(0, region: RegionDatabase.Sinnoh);
        Assert.Contains("ride refused Tauros", sinnoh.Log);
        // Sharpedo comes only on the water (plan 23, the wave after Ultra Space), and Cancel calls nothing
        Assert.Contains("ride refused Sharpedo", Pager(3).Log);
        Assert.DoesNotContain(Pager(4).Log, l => l.StartsWith("ride"));

        Assert.True(RegionDatabase.Get(RegionDatabase.Alola)!.Movement.Offers(PokemonPlatinumEngine.Data.RegionMode.Ride));
        Assert.False(RegionDatabase.Get(RegionDatabase.Kalos)!.Movement.Offers(PokemonPlatinumEngine.Data.RegionMode.Ride));
        Assert.Null(RidePager.LandRideOf("Lapras"));
        Assert.Equal("Tauros", RidePager.LandRideOf("tauros")!.Value.Species);
    }

    [Fact]
    public void ARideGoesAtARunsPaceAndASaveMadeRidingWakesOnFoot()
    {
        var map = MapNamed("Melemele");
        // A plain step of Route 1 or Iki Town: on foot a walk, riding a run's pace
        var (x, y) = Enumerable.Range(140, 30).SelectMany(ty => Enumerable.Range(228, 30).Select(tx => (tx, ty)))
            .First(t => FieldMovement.Step(map, t.tx, t.ty, Direction.Up, new Walker(TravelMode.OnFoot, map.HeightAt(t.tx, t.ty))) is { Kind: StepKind.Walk, Pace: Pace.Walk });
        var walk = FieldMovement.Step(map, x, y, Direction.Up, new Walker(TravelMode.OnFoot, map.HeightAt(x, y)));
        var ride = FieldMovement.Step(map, x, y, Direction.Up, new Walker(TravelMode.Riding, map.HeightAt(x, y)));
        Assert.Equal(walk.Kind, ride.Kind);
        Assert.Equal(Pace.Fast, ride.Pace);
        Assert.Equal(TravelMode.Riding, ride.Mode);

        var player = new Player(x, y);
        Assert.False(player.SetRiding(null));
        Assert.True(player.SetRiding("Tauros"));
        Assert.Equal(TravelMode.Riding, player.Mode);
        Assert.Equal("Tauros", player.Steed);
        // The Bicycle puts the ride away
        Assert.True(player.SetCycling(true));
        Assert.Null(player.Steed);
        // A mode set without a steed (a save's) is on foot
        player.SetMode(TravelMode.Riding);
        Assert.Equal(TravelMode.OnFoot, player.Mode);
    }

    [Fact]
    public void TaurosRunIntoTheCrackedRockBreaksItWithNoQuestion()
    {
        var map = MapNamed("Melemele");
        var rock = map.Everyone.Single(n => n.Key == "rock_ten_carat");
        Assert.Equal(PropType.CrackedRock, rock.Obstacle);

        // Riding, a step into the rock is stopped by it and the tile is told once
        var player = new Player(rock.GridX + 1, rock.GridY) { Facing = Direction.Left };
        Assert.True(player.SetRiding("Tauros"));
        for (int i = 0; i < 10; i++) player.Advance(1f / 30f, map, Direction.Left, false, null, null);
        Assert.Equal((rock.GridX + 1, rock.GridY), (player.GridX, player.GridY));
        Assert.Equal((rock.GridX, rock.GridY), player.TakeRanInto());
        Assert.Null(player.TakeRanInto());
        Assert.True(RidePager.Charges(player.Steed));
        Assert.False(RidePager.Charges("Stoutland"));

        // The charge's script breaks it by the Rock Smash the pager grants, asking nothing
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (rock.GridX + 1, rock.GridY), PlayerFacing = Direction.Left, Region = RegionDatabase.Alola };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Rowlet")!, 15));
        host.Story.Set("FLAG_ALOLA_RIDE_TAUROS");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(PokemonPlatinumEngine.Core.GameEngine.TaurosChargeScript)!, rock);
        runner.RunToEnd();
        Assert.Empty(host.Asked);
        Assert.True(host.Story.Has(rock.HiddenBy!));
        Assert.Contains(host.Log, l => l.StartsWith("usemove Rock Smash Tauros"));
    }

    [Fact]
    public void HalaHandsOverTheRidePagerWithTauros()
    {
        var item = ItemDatabase.Get(RidePager.Item)!;
        Assert.Equal("RidePager", item.FieldUse);
        Assert.True(PokemonPlatinumEngine.UI.BagScreen.UsedInField(item));
        var hala = ScriptLibrary.Default.All.Single(s => s.File == "iki_town" && s.Code.Any(i => i.Op == Op.Give && i.Name == RidePager.Item));
        var code = hala.Code.ToList();
        int pager = code.FindIndex(i => i.Op == Op.Give && i.Name == RidePager.Item);
        Assert.Contains(code.Skip(pager), i => i.Op == Op.SetFlag && i.Name == "FLAG_ALOLA_RIDE_TAUROS");
    }
}
