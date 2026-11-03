using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 20 · J15, part u: the rooms part s left with their walls cut to the wainscot, each given what its blocked tiles
/// stand for; the S.S. Aqua's table among its bulkheads; and what part t's pictures showed wrong.
/// </summary>
public partial class JohtoTests
{
    [Theory]
    [InlineData("Route22_LeagueReceptionGate", PartitionLook.Hedge)]
    [InlineData("Saffron_MagnetTrainStation", PartitionLook.Counter)]
    [InlineData("Goldenrod_MagnetTrainStation", PartitionLook.Counter)]
    [InlineData("Celadon_DeptStore_1F", PartitionLook.Counter)]
    [InlineData("Celadon_DeptStore_5F", PartitionLook.Counter)]
    [InlineData("Goldenrod_DeptStore_Basement", PartitionLook.Crate)]
    [InlineData("Goldenrod_RadioTower_3F", PartitionLook.Console)]
    [InlineData("Route10_PowerPlant", PartitionLook.Console)]
    [InlineData("Pewter_Museum", PartitionLook.Case)]
    [InlineData("Ecruteak_DanceTheater", PartitionLook.Lattice)]
    [InlineData("Route39_MoomooFarmStable", PartitionLook.Fence)]
    [InlineData("TeamRocketHQ_B1F", PartitionLook.Bulkhead)]
    [InlineData("SSAqua_1F_SouthwestRooms", PartitionLook.Bulkhead)]
    public void TheRoomsPartSLeftHaveTheLookTheirBlockedTilesStandFor(string room, PartitionLook look)
    {
        Assert.Equal(look, WorldModels.PartitionsOf("Johto", room));
        Assert.Equal(look, MapNamed(room).Partitions);
    }

    [Fact]
    public void TheShipsMessHasATableAmongItsBulkheads()
    {
        var hold = MapNamed("SSAqua_B1F");
        var patch = Assert.Single(hold.PartitionPatches);
        Assert.Equal(PartitionLook.Table, patch.Look);
        // Every tile of the table is a blocked tile of the room, with a chair beside it, and the hold round it stays steel
        for (int y = patch.Y; y < patch.Y + patch.Height; y++)
            for (int x = patch.X; x < patch.X + patch.Width; x++)
            {
                Assert.Equal(TileType.Wall, hold.GetGroundTile(x, y));
                Assert.Equal(PartitionLook.Table, hold.PartitionAt(x, y));
            }
        Assert.Contains(hold.Props, p => p.Type == PropType.Chair && (p.X == patch.X - 1 || p.X == patch.X + patch.Width));
        Assert.Equal(PartitionLook.Bulkhead, hold.PartitionAt(patch.X - 3, patch.Y));
        // A room no patch names has none
        Assert.Empty(MapNamed("SSAqua_1F").PartitionPatches);
        Assert.Empty(WorldModels.PartitionPatchesOf("Hoenn", "SSAqua_B1F"));
    }

    [Theory]
    [InlineData(PartitionLook.Table)]
    [InlineData(PartitionLook.Crate)]
    [InlineData(PartitionLook.Console)]
    [InlineData(PartitionLook.Case)]
    [InlineData(PartitionLook.Lattice)]
    [InlineData(PartitionLook.Fence)]
    public void PartUsLooksAreWholeLowAndTheirOwn(PartitionLook look) => APartitionsArtIsWholeAndLowEnoughToBeSeenOver(look);

    [Fact]
    public void EveryLookOfAPartitionHasArtOfItsOwn()
    {
        // No two looks paint the same face or the same top (a new look given no art falls to the grey default)
        var faces = new HashSet<string>();
        foreach (var look in Enum.GetValues<PartitionLook>().Where(l => l != PartitionLook.Wall))
        {
            var face = new PixelCanvas(32, PartitionArt.HeightOf(look));
            PartitionArt.PaintFace(face, look);
            var top = new PixelCanvas(32, 32);
            PartitionArt.PaintTop(top, look, true, true, true, true);
            string key = string.Concat(Enumerable.Range(0, 32).Select(i => face.Get(i, face.Height / 2).R)) + "|" + string.Concat(Enumerable.Range(0, 32).Select(i => top.Get(i, 16).G));
            Assert.True(faces.Add(key), $"{look}'s art is another look's");
            Assert.NotEqual(128, top.Get(16, 16).R);
        }
    }

    [Fact]
    public void IlexForestsPrintIsTheWoodsNotACave()
    {
        // The wood is a cave to the field's camera; its photograph is still a forest's
        Assert.Equal(PhotoSetting.Forest, PhotoScenes.Of("ilex_forest", false, true, false, false));
        Assert.Equal(PhotoSetting.Cave, PhotoScenes.Of("johto_seafoam_islands_b1f", false, true, false, false));
    }

    [Fact]
    public void CameronStandsOnDryGroundAtEveryPlace()
    {
        var camerons = BuiltMaps.Value.Values.SelectMany(m => m.Everyone.Where(n => n.Name == "Cameron").Select(n => (Map: m, Npc: n))).ToList();
        Assert.True(camerons.Count >= 20, $"{camerons.Count} Camerons");
        // (one on a bridge's deck over the water stands on the deck: Route 26's)
        foreach (var (map, cameron) in camerons)
            Assert.False(map.DeckAt(cameron.GridX, cameron.GridY) == null
                && (map.IsDeepWater(cameron.GridX, cameron.GridY) || map.GetGroundTile(cameron.GridX, cameron.GridY) == TileType.Water),
                $"Cameron stands in the water on {map.Name} at {cameron.GridX},{cameron.GridY}");
    }
}
