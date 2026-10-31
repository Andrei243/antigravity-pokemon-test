using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The looks of Johto's rooms that are no plain room or cave (plan 20, wave 77; style guide, "Rooms", "A wooden tower"
/// and "Walkways over the dark", and "Caves", "Caves that are built"): Sprout Tower's floors over the dark round its
/// pillar, the Violet Gym's walkways, the Burned Tower's charred timbers, the Ruins of Alph's stone, and the hall's door
/// on the grounds that is no lab.
/// </summary>
public class JohtoRoomLooksTests
{
    private static readonly Lazy<Dictionary<string, Map>> Built =
        new(() => World.LoadAll().Single(w => w.Index.Region == "Johto").BuildMaps().ToDictionary(m => m.Name));

    private static Map Named(string name) => Built.Value[name];

    [Fact]
    public void SproutTowersFloorsStandOverTheDarkRoundTheirPillar()
    {
        foreach (string name in new[] { "SproutTower_1F", "SproutTower_2F", "SproutTower_3F" })
        {
            var map = Named(name);
            Assert.Equal(InteriorStyle.Tower, map.Interior);
            // Where the original leaves the floor there is nothing, not a slab of wall top
            bool dark = false;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    dark |= map.GetGroundTile(x, y) == TileType.Void;
            Assert.True(dark, $"{name} has no dark between its floors");
            // The pillar is the room's columns, three by three, which the field still walks round
            var columns = map.Props.Where(p => p.Type == PropType.Column).ToList();
            Assert.Equal(9, columns.Count);
            Assert.All(columns, c => Assert.True(map.IsSolid(c.X, c.Y), $"{name}: the pillar's tile {c.X}, {c.Y} is walked through"));
        }
    }

    [Fact]
    public void TheVioletGymsWalkwaysStandOverTheDark()
    {
        var map = Named("Violet_Gym");
        Assert.Equal(InteriorStyle.Aerie, map.Interior);
        // Between the walkways, where the import had blocked tiles with nothing drawn, the room has nothing
        Assert.Equal(TileType.Void, map.GetGroundTile(12, 10));
        // and Falkner and his trainers still stand on the walkways
        Assert.All(map.Everyone.Where(n => n.IsTrainer), n => Assert.NotEqual(TileType.Void, map.GetGroundTile(n.GridX, n.GridY)));
    }

    [Fact]
    public void TheBurnedTowerIsCharredAndTheRuinsAreStone()
    {
        Assert.Equal(CaveLook.Charred, Named("BurnedTower_1F").CaveLook);
        Assert.Equal(CaveLook.Charred, Named("BurnedTower_B1F").CaveLook);
        foreach (string name in new[] { "RuinsOfAlph_NortheastChamber", "RuinsOfAlph_NorthwestChamber", "RuinsOfAlph_SoutheastChamber",
                     "RuinsOfAlph_SouthwestChamber", "RuinsOfAlph_HallEntrance", "RuinsOfAlph_UndergroundHall" })
            Assert.Equal(CaveLook.Ruins, Named(name).CaveLook);
        // Every other cave is rock
        Assert.Equal(CaveLook.Rock, Named("UnionCave_1F").CaveLook);
    }

    [Fact]
    public void TheRuinsHallHasNothingBeyondItsWalls()
    {
        var hall = Named("RuinsOfAlph_UndergroundHall");
        // The hall's chunk runs on south of its walls: the corner nobody reaches is the dark, not a slab of rock
        Assert.Equal(TileType.Void, hall.GetGroundTile(15, 55));
        // and its walls are still rock beside its floor, which is never dark
        bool walls = false;
        for (int y = 0; y < hall.Height; y++)
            for (int x = 0; x < hall.Width; x++)
                walls |= hall.GetGroundTile(x, y) == TileType.CaveWall;
        Assert.True(walls);
        Assert.NotEqual(TileType.Void, hall.GetGroundTile(15, 23));
    }

    [Fact]
    public void ABuiltCavesArtIsItsOwnAndNeverRocks()
    {
        foreach (var look in new[] { CaveLook.Charred, CaveLook.Ruins })
        {
            var c = new PixelCanvas(64, 32);
            BuiltArt.PaintGround(c, 0, 0, 3, 4, look, TileType.CaveFloor);
            BuiltArt.PaintGround(c, 32, 0, 3, 4, look, TileType.CaveWall);
            Assert.All(Enumerable.Range(0, 64), x => Assert.Equal(255, c.Get(x, 5).A));
            var face = BuiltArt.Face(look);
            Assert.Equal(32, face.Width);
            Assert.Equal(NatureArt.FaceCap + NatureArt.FaceBody, face.Height);
        }
        var stele = new PixelCanvas(32, 36);
        BuiltArt.PaintStele(stele);
        Assert.True(stele.Get(16, 20).A > 0);
    }

    [Fact]
    public void TheBuildingOverTheRuinsHallsDoorIsNoLab()
    {
        // ar_center stands over the hall's door (the grounds' warps 8 and 9), ar_study over the research centre's
        Assert.Equal(BuildingKind.Shrine, WorldModels.Of("ar_center", "Johto")!.Kind);
        Assert.Equal(BuildingKind.Lab, WorldModels.Of("ar_study", "Johto")!.Kind);
    }
}
