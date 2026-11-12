using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola moves freely since the user's switch of 2026-11-12: the rides' rules still go through the grid's own step
/// (<see cref="FieldMovement.Step"/>), which free movement only asks (<see cref="FreeMovement.Classify"/>). A body
/// steered by the stick is refused the rough ground on foot and gallops across it on Mudsdale; a Lapras's rider steered
/// onto the reef's current is carried back out, and Sharpedo's stems it (the Tauros's charge is
/// <c>AlolaRideTests.UnderFreeMovementTaurosStillRunsIntoTheCrackedRock</c>).
/// </summary>
public class AlolaFreeRidesTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static Map MapNamed(string name) => BuiltMaps.Value[name];

    private static Player Free(int x, int y) => new(x, y) { Directions = MovementDirections.Free };

    private static void Steer(Player player, Map map, Vector2 way, int frames)
    {
        for (int f = 0; f < frames; f++) player.SteerFreely(1f / 60f, map, way, false, _ => { }, _ => { });
        // let go: whatever a step, a slide or a current had begun plays out
        for (int f = 0; f < 600 && (player.IsMoving || player.IsSliding); f++) player.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
    }

    [Fact]
    public void RoughGroundIsRefusedOnFootAndGallopedAcrossOnMudsdaleSteeredFreely()
    {
        var poni = MapNamed("Poni");
        Assert.Equal(TileBehavior.RoughGround, poni.BehaviourAt(299, 95));
        var walker = Free(299, 96);
        Steer(walker, poni, new Vector2(0f, -1f), 90);
        Assert.Equal((299, 96), (walker.GridX, walker.GridY));

        var rider = Free(299, 96);
        Assert.True(rider.SetRiding("Mudsdale"));
        Steer(rider, poni, new Vector2(0f, -1f), 30);
        Assert.True(rider.GridY < 96, $"Mudsdale stayed at {rider.GridX},{rider.GridY}");
    }

    // The gap of the reef off the Secluded Shore (AlolaOddsAndEndsTests): a current flowing west, the islet to its east
    private const int GapX = 176, GapY = 344, WaterX = 175;

    [Fact]
    public void TheReefsCurrentCarriesALaprasBackOutAndSharpedoStemsItSteeredFreely()
    {
        var map = MapNamed("Ulaula");
        Assert.Equal(TileBehavior.CurrentWest, map.BehaviourAt(GapX, GapY));
        var lapras = Free(WaterX, GapY);
        lapras.Carrier = "Lapras";
        lapras.SetMode(TravelMode.Surfing);
        Steer(lapras, map, new Vector2(1f, 0f), 40);
        Assert.True(lapras.GridX <= GapX, $"the Lapras got through to {lapras.GridX}");

        var jet = Free(WaterX, GapY);
        jet.Carrier = "Lapras";
        jet.SetMode(TravelMode.Surfing);
        Assert.True(jet.SetJet("Sharpedo"));
        Steer(jet, map, new Vector2(1f, 0f), 60);
        Assert.True(jet.GridX > GapX, $"Sharpedo stopped at {jet.GridX}");
    }
}
