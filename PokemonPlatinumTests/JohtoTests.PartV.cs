using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 20 · J15, part v: the rooms part u left with their inner walls cut to the wainscot and looked at since: the gates
/// of twelve by twelve, the charcoal kiln's corners and Mahogany's souvenir shop.
/// </summary>
public partial class JohtoTests
{
    public static IEnumerable<object[]> TheTwelveByTwelveGates => new[]
    {
        "Azalea_IlexForestGate", "Route11_Route12Gate", "Route15_FuchsiaGate", "Route16_CyclingGate", "Route18_CyclingGate",
        "Route31_VioletGate", "Route32_RuinsOfAlphGate", "Route36_NationalParkGate", "Route38_EcruteakGate",
        "Route42_EcruteakGate", "Route7_SaffronGate", "Route8_SaffronGate", "Violet_Route36Gate",
    }.Select(g => new object[] { g });

    [Theory]
    [MemberData(nameof(TheTwelveByTwelveGates))]
    public void ATwelveByTwelveGatesFrontBlockIsAPlanterBed(string gate)
    {
        var map = MapNamed(gate);
        Assert.Equal((12, 12), (map.Width, map.Height));
        Assert.Equal(PartitionLook.Hedge, map.Partitions);
        // The block between the two alcoves at the front is blocked, and both alcoves are floor beside it
        Assert.Equal(TileType.Wall, map.GetGroundTile(3, 9));
        Assert.Equal(TileType.Wall, map.GetGroundTile(8, 9));
        Assert.NotEqual(TileType.Wall, map.GetGroundTile(2, 9));
        Assert.NotEqual(TileType.Wall, map.GetGroundTile(9, 9));
    }

    [Fact]
    public void EveryTwelveByTwelveGateOfJohtoIsNamed()
    {
        // A gate of this plan the table leaves out would show its block as the bare wall again
        var named = TheTwelveByTwelveGates.Select(r => (string)r[0]).ToHashSet();
        var gates = BuiltMaps.Value.Values.Where(m => m.Name.EndsWith("Gate", StringComparison.Ordinal) && m.Width == 12 && m.Height == 12
            && m.GetGroundTile(3, 9) == TileType.Wall && m.GetGroundTile(2, 9) != TileType.Wall).Select(m => m.Name).ToList();
        Assert.NotEmpty(gates);
        Assert.All(gates, g => Assert.Contains(g, named));
    }

    [Fact]
    public void TheCharcoalKilnsCornersAreCratesAndItsBackWallStaysWall()
    {
        var kiln = MapNamed("Azalea_CharcoalKiln");
        Assert.Equal(PartitionLook.Wall, kiln.Partitions);
        foreach (var (x, y) in new[] { (1, 8), (1, 9), (11, 7) })
        {
            Assert.Equal(TileType.Wall, kiln.GetGroundTile(x, y));
            Assert.Equal(PartitionLook.Crate, kiln.PartitionAt(x, y));
        }
        // The back wall come forward beside the doorway is the room's own wall, not a stack of crates
        Assert.Equal(PartitionLook.Wall, kiln.PartitionAt(9, 4));
        Assert.Equal(PartitionLook.Wall, kiln.PartitionAt(11, 5));
    }

    [Fact]
    public void MahoganysSouvenirShopHasCounters() =>
        Assert.Equal(PartitionLook.Counter, MapNamed("Mahogany_SouvenirShop").Partitions);
}
