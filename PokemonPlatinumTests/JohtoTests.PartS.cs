using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>Plan 20 · J15, part s: the imported rooms' partitions of their own, the S.S. Aqua's engines and Baoba's look.</summary>
public partial class JohtoTests
{
    [Fact]
    public void EveryRoomNamedForPartitionsOfItsOwnIsAJohtoRoomThatHasThem()
    {
        Assert.NotEmpty(WorldModels.JohtoRoomPartitions);
        foreach (var (name, look) in WorldModels.JohtoRoomPartitions)
        {
            Assert.NotEqual(PartitionLook.Wall, look);
            var map = MapNamed(name);
            Assert.True(map.IsIndoors, name);
            Assert.Equal(look, map.Partitions);
            // and it has inner walls for the look to stand on
            Assert.True(Enumerable.Range(1, map.Height - 2).Any(y => Enumerable.Range(1, map.Width - 2).Any(x => map.GetGroundTile(x, y) == TileType.Wall)), name);
        }
        // A room the table names nothing for keeps its wall cut to the wainscot
        Assert.Equal(PartitionLook.Wall, WorldModels.JohtoPartitionsOf("Azalea_CharcoalKiln"));
        Assert.Equal(PartitionLook.Wall, MapNamed("Azalea_CharcoalKiln").Partitions);
    }

    [Fact]
    public void TheShipsHoldHasItsEnginesOnTheTilesTheyBlockAndNoLawn()
    {
        var hold = MapNamed("SSAqua_B1F");
        var engines = hold.Props.Where(p => p.Type == PropType.ShipEngine).ToList();
        Assert.True(engines.Count >= 40, $"{engines.Count} tiles of engine");
        Assert.All(engines, e => Assert.True(hold.IsSolid(e.X, e.Y), $"an engine on open ground at {e.X},{e.Y}"));
        Assert.All(engines, e => Assert.True(e.X > 0 && e.X < hold.Width - 1 && e.Y > 0 && e.Y < hold.Height - 1));
        // The middle of the hold, under the engines' box, was drawn as lawn: nothing of the room is grass now
        for (int y = 0; y < hold.Height; y++)
            for (int x = 0; x < hold.Width; x++)
                Assert.NotEqual(TileType.Grass, hold.GetGroundTile(x, y));
        // and its crew's quarters have the beds and chairs its chunks hold
        Assert.Contains(hold.Props, p => p.Type == PropType.Bed);
    }

    [Theory]
    [InlineData(PartitionLook.Hedge)]
    [InlineData(PartitionLook.Counter)]
    [InlineData(PartitionLook.Bulkhead)]
    [InlineData(PartitionLook.Rock)]
    [InlineData(PartitionLook.Ice)]
    [InlineData(PartitionLook.Pillar)]
    public void APartitionsArtIsWholeAndLowEnoughToBeSeenOver(PartitionLook look)
    {
        int h = PartitionArt.HeightOf(look);
        Assert.InRange(h, 18, 48);
        // Only a pillar stands taller than the room's own inner wall, and it stands a tile alone
        if (look != PartitionLook.Pillar) Assert.True(h <= PropModels.InnerWallHeight, $"{look} stands {h}");
        var face = new PixelCanvas(32, h);
        PartitionArt.PaintFace(face, look);
        var top = new PixelCanvas(32, 32);
        PartitionArt.PaintTop(top, look, true, true, true, true);
        foreach (var c in new[] { face, top })
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                    Assert.Equal(255, c.Get(x, y).A);
    }

    [Fact]
    public void BaobaOnTheBeachIsNoTwinOfTheManBesideHim()
    {
        var baoba = Overworld.Everyone.Single(n => n.Key == "route_40_baoba");
        var man = Overworld.Everyone.Single(n => n.Key == "w40_gsmiddleman1");
        var looks = CharacterStyles.Default;
        Assert.True(looks.Has("safari_warden"));
        Assert.Equal("safari_warden", looks.CharacterFor(baoba.NpcType));
        Assert.NotEqual(looks.CharacterFor(man.NpcType), looks.CharacterFor(baoba.NpcType));
    }
}
