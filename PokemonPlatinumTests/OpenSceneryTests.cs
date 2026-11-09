using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using OpenWorldBake;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW9, the open world's renderer II, its rules without a window: the places' trees, groves and buildings
/// read and held to their rules, a grove's trees the same every time and within it, the kit's plan of a building over
/// its box, the grass's tufts on the grass alone and never in the water, which figures are live and which impostors,
/// and the rings to the horizon (the border's own heights, the hills, every face up, the far forests on the land).
/// GPU-free.
/// </summary>
public class OpenSceneryTests
{
    static string LabFolder => OpenWorldBakeTests.LabFolder;

    static readonly Lazy<(RegionDescription Description, TerrainWorld Land)> lab = new(() =>
    {
        var description = RegionDescription.Read(LabFolder);
        return (description, description.Land());
    });

    static readonly ConcurrentDictionary<(int, int), BakedChunk?> chunks = new();

    static BakedChunk Finest(int x, int z) =>
        chunks.GetOrAdd((x, z), _ => RegionBake.Finest(lab.Value.Land, lab.Value.Description.World, x, z))!;

    // ------------------------------------------------------------------ trees and groves

    [Fact]
    public void AGroveIsTheSameTreesEveryTimeEachInsideItAndApart()
    {
        var polygon = new List<(int, int)> { (-30000, -10000), (20000, -14000), (24000, 18000), (-26000, 20000) };
        var grove = new OpenGrove("a test grove", polygon, 5000, 750, 600, 900, 1250, 7);
        var once = grove.Trees();
        Assert.Equal(once, grove.Trees());
        Assert.Equal(once, new OpenGrove("a test grove", polygon, 5000, 750, 600, 900, 1250, 7).Trees());
        Assert.NotEqual(once, new OpenGrove("a test grove", polygon, 5000, 750, 600, 900, 1250, 8).Trees());
        Assert.All(once, t =>
        {
            Assert.True(OpenPolygons.Holds(polygon, t.X, t.Z));
            Assert.InRange(t.Size, 900, 1250);
            Assert.InRange(t.Variant, 0, OpenTree.Variants - 1);
            Assert.InRange(t.Yaw, 0, 0xFFFF);
            Assert.Null(t.Y);
        });
        // The middle seven tenths of a cell: no two trees nearer than three tenths of the spacing
        for (int i = 0; i < once.Count; i++)
            for (int j = i + 1; j < once.Count; j++)
                Assert.True(Math.Sqrt((double)(once[i].X - once[j].X) * (once[i].X - once[j].X) + (double)(once[i].Z - once[j].Z) * (once[i].Z - once[j].Z)) >= 1500 - 1,
                    $"{once[i]} and {once[j]}");
        // The cells filled and the pines about the shares asked for (a grove of a couple of hundred cells)
        double area = 50.0 * 31;
        Assert.InRange(once.Count, area / 25 * 0.75 * 0.75, area / 25 * 0.75 * 1.25);
        double pines = once.Count(t => t.Kind == OpenTreeKind.Pine) / (double)once.Count;
        Assert.InRange(pines, 0.45, 0.75);
    }

    [Fact]
    public void TheLabsMeadowLaysItsTreesAndItsHouseAsABuilding()
    {
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        var place = OpenPlaceFiles.Read(File.ReadAllText(Path.Combine(LabFolder, "places", "lab_meadow.json")), "places/lab_meadow.json");
        int grovesTrees = place.Groves.Sum(g => OpenPlaceFiles.GroveOf(g).Trees().Count);
        Assert.Equal(place.Trees.Count + grovesTrees, map.Trees.Count);
        Assert.True(map.Trees.Count > 40, $"{map.Trees.Count} trees");
        // A tree standing by itself, as its file has it
        var round = map.Trees.Single(t => t.X == -28500 && t.Z == -46000);
        Assert.Equal(OpenTreeKind.Round, round.Kind);
        Assert.Equal(1000, round.Size);
        // The house is its solid, built as a timber house facing south; nothing else is a building
        var house = Assert.Single(map.Buildings);
        Assert.Equal(map.Solids.Single(s => s.Name == "the house"), house.Box);
        Assert.Equal((BuildingKind.House, Architecture.Timber, 0, Direction.Down), (house.Kind, house.Town, house.Storeys, house.Faces));
        // No tree is a solid: the capsule walks through them (OW9 as built)
        Assert.Equal(place.Solids.Count, map.Solids.Count);
    }

    [Theory]
    [InlineData("{ \"trees\": [ { \"at\": [1] } ] }", "$.trees[0].at")]
    [InlineData("{ \"trees\": [ { \"at\": [1, 2], \"size\": 0 } ] }", "$.trees[0].size")]
    [InlineData("{ \"trees\": [ { \"at\": [1, 2], \"kind\": \"palm\" } ] }", "$.trees[0].kind")]
    [InlineData("{ \"groves\": [ { \"name\": \"g\", \"polygon\": [[0, 0], [1, 0], [1, 1]], \"spacing\": 1 } ] }", "$.groves[0].spacing")]
    [InlineData("{ \"groves\": [ { \"name\": \"g\", \"polygon\": [[0, 0], [1, 0], [1, 1]], \"fill\": 0 } ] }", "$.groves[0].fill")]
    [InlineData("{ \"groves\": [ { \"name\": \"g\", \"polygon\": [[0, 0], [1, 0], [1, 1]], \"pines\": 1.5 } ] }", "$.groves[0].pines")]
    [InlineData("{ \"groves\": [ { \"name\": \"g\", \"polygon\": [[0, 0], [1, 0], [1, 1]], \"size\": [1.2, 1.0] } ] }", "$.groves[0].size")]
    [InlineData("{ \"groves\": [ { \"name\": \"g\", \"polygon\": [[0, 0], [1, 0]] } ] }", "$.groves[0].polygon")]
    [InlineData("{ \"solids\": [ { \"name\": \"s\", \"min\": [0, 0, 0], \"max\": [1, 1, 1], \"building\": { \"storeys\": 9 } } ] }", "$.solids[0].building.storeys")]
    [InlineData("{ \"solids\": [ { \"name\": \"s\", \"min\": [0, 0, 0], \"max\": [1, 1, 1], \"building\": { \"roof\": \"red\" } } ] }", "roof")]
    public void APlacesTreesGrovesAndBuildingsAreHeldToTheirRules(string json, string named)
    {
        var e = Assert.ThrowsAny<Exception>(() => OpenPlaceFiles.Read(json, "places/test.json"));
        Assert.Contains("places/test.json", e.Message);
        Assert.Contains(named, e.Message);
    }

    // ------------------------------------------------------------------ the kit's plan of a building

    [Fact]
    public void TheKitsPlanStandsABuildingOnItsBoxWithItsDoorWhereTheMapsDoorIs()
    {
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        var house = map.Buildings.Single();
        var plan = OpenBuildingKit.PlanOf(house, map.Doors);
        // 8 m by 7 m at 1.5 m a tile: five tiles across, five of walls deep and the tile behind them the kit keeps
        Assert.Equal((5, 6), (plan.Width, plan.Depth));
        // The door volume's middle is the box's front's middle: the middle bay
        Assert.Equal(2, plan.DoorBay);
        Assert.Equal(0f, plan.Turn);
        Assert.Equal(new Vector2(-20, -43), plan.FrontMiddle);
        // The same box facing east: the kit turned a quarter, its width along the east side, no door near it
        var east = house with { Faces = Direction.Right };
        var turned = OpenBuildingKit.PlanOf(east, map.Doors);
        Assert.Equal((5, 6), (turned.Width, turned.Depth));
        Assert.Equal(-1, turned.DoorBay);
        Assert.Equal(MathF.PI / 2, turned.Turn, 5);
        Assert.Equal(new Vector2(-16, -46.5f), turned.FrontMiddle);
        Assert.Equal(new Vector2(1, 0), turned.Out);
    }

    // ------------------------------------------------------------------ the grass

    [Fact]
    public void TuftsStandOnTheGrassAloneTheSameEveryTimeAndNeverInTheWater()
    {
        // The chunk with the river across it, south of the hill, and a chunk of dry meadow
        var river = Finest(0, 1);
        var meadow = Finest(-2, -1);
        var tufts = OpenGrass.Of(river);
        Assert.Equal(tufts, OpenGrass.Of(river));
        var dry = OpenGrass.Of(meadow);
        // About three tufts to four square metres of the meadow, fewer where the river and its banks take the ground
        Assert.True(dry.Count > 400, $"{dry.Count} tufts on the meadow");
        Assert.True(tufts.Count > 100 && tufts.Count < dry.Count, $"{tufts.Count} tufts by the river, {dry.Count} on the meadow");
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre, cell = side / BakedChunk.WaterCells;
        foreach (var (chunk, list) in new[] { (river, tufts), (meadow, dry) })
            foreach (var t in list)
            {
                Assert.InRange(t.Variant, 0, OpenGrass.Variants - 1);
                // On the chunk's own ground, which reaches half a cell of the surface nets past its east and south edges
                Assert.InRange(t.At.X, chunk.ChunkX * side, (chunk.ChunkX + 1) * side + 0.5f);
                Assert.InRange(t.At.Z, chunk.ChunkZ * side, (chunk.ChunkZ + 1) * side + 0.5f);
                int i = Math.Clamp((int)MathF.Floor((t.At.X - chunk.ChunkX * side) / cell), 0, BakedChunk.WaterCells - 1);
                int k = Math.Clamp((int)MathF.Floor((t.At.Z - chunk.ChunkZ * side) / cell), 0, BakedChunk.WaterCells - 1);
                int level = chunk.WaterAt(i, k);
                Assert.True(level == BakedChunk.NoWater || t.At.Y >= level / (float)OpenPos.PerMetre + 0.08f, $"a tuft at {t.At} under the water at {level} mm");
            }
        // Only where all three corners are grasses or the highland
        Assert.Equal(-1, OpenGrass.VariantOf(OpenCover.Sand));
        Assert.Equal(-1, OpenGrass.VariantOf(OpenCover.Rock));
        // A thinned level has none (the tufts are drawn near the player alone)
        Assert.Empty(OpenGrass.Of(RegionBake.Chunk(lab.Value.Land, lab.Value.Description.World, 0, 1)[1]));
        // Shown whole near the player, shrinking to nothing at the reach
        Assert.Equal(1f, OpenGrass.Shown(3f));
        Assert.Equal(0f, OpenGrass.Shown(OpenGrass.Reach));
        Assert.InRange(OpenGrass.Shown(OpenGrass.Reach - OpenGrass.Fade / 2), 0.2f, 0.8f);
    }

    // ------------------------------------------------------------------ the figures' levels

    [Fact]
    public void TheTwelveNearestFiguresAreLiveAndTheRestImpostorsWithinTheReach()
    {
        var eye = new Vector3(0, 2, 0);
        var figures = new List<OpenFigure>();
        // Twenty at 2 m to 40 m away, one at 100 m and one past the reach, and two at the same 5 m
        for (int i = 0; i < 20; i++) figures.Add(new OpenFigure(OpenFigureKind.Person, $"p{i}", "lass", new Vector3(2 + i * 2, 2, 0), 0f));
        figures.Add(new OpenFigure(OpenFigureKind.Pokemon, "far", "Wooloo", new Vector3(0, 2, 100), 0f, Height: 0.6f));
        figures.Add(new OpenFigure(OpenFigureKind.Person, "gone", "lass", new Vector3(0, 2, OpenFigures.DrawReach + 5), 0f));
        figures.Add(new OpenFigure(OpenFigureKind.Person, "tie", "lass", new Vector3(0, 2, 6), 0f));
        var live = new List<int>();
        var far = new List<int>();
        OpenFigures.Choose(figures, eye, live, far);
        Assert.Equal(OpenFigures.MostLive, live.Count);
        // Nearest first; the two at 6 m by their places in the list
        Assert.Equal(new[] { 0, 1, 2, 22, 3, 4, 5, 6, 7, 8, 9, 10 }, live);
        Assert.Equal(Enumerable.Range(11, 9).Append(20).ToArray(), far.ToArray());
        Assert.DoesNotContain(21, live.Concat(far));
        // The same places choose the same every time
        var again = new List<int>();
        OpenFigures.Choose(figures, eye, again, new List<int>());
        Assert.Equal(live, again);
        Assert.Equal(0.35f, OpenFigures.PokemonHeight(0.1f));
        Assert.Equal(3.5f, OpenFigures.PokemonHeight(14.5f));
    }

    // ------------------------------------------------------------------ the rings to the horizon

    [Fact]
    public void TheLandBeyondMeetsTheBorderRisesIntoHillsAndFacesUp()
    {
        // A region of four chunks by four whose ground rises 1 m every 32 m east, with a channel at its east edge
        Func<float, float, float?> height = (x, z) => x / 32f + (x > 60 && MathF.Abs(z) < 4 ? -2f : 0f);
        var land = OpenHorizon.Of((-2, -2, 1, 1), height, 1234u);
        var points = land.Mesh.Positions.Take(land.Mesh.Count).ToList();
        for (int t = 0; t < points.Count; t += 3)
            Assert.True(Vector3.Cross(points[t + 1] - points[t], points[t + 2] - points[t]).Y > 0, $"the face at {points[t]} faces down");
        // A metre inside the west border at the border's own height, less a hair
        float west = -63.7f / 32f - OpenHorizon.Drop;
        Assert.Contains(points, p => MathF.Abs(p.X - (-63f)) < 0.01f && MathF.Abs(p.Y - west) < 0.001f);
        // The band eases to the median by 124 m and keeps under the border's highest; past it the hills rise
        float highest = 64f / 32f;
        Assert.All(points.Where(p => Beyond(p) <= OpenHorizon.Level + 0.5f), p => Assert.True(p.Y <= highest + 0.01f, $"{p} over the band"));
        Assert.Contains(points, p => Beyond(p) > OpenHorizon.HillsFull && p.Y > land.Median + 20f);
        Assert.Contains(points, p => p.X > 2000);
        Assert.Contains(points, p => p.Z < -2000);
        // The hills are the seed's: another seed, other hills; the same seed, the same land
        Assert.Equal(points, OpenHorizon.Of((-2, -2, 1, 1), height, 1234u).Mesh.Positions.Take(land.Mesh.Count));
        Assert.NotEqual(points, OpenHorizon.Of((-2, -2, 1, 1), height, 99u).Mesh.Positions.Take(land.Mesh.Count));
        // Far fewer triangles than a ring of the border's 2 m every ring out would be
        Assert.InRange(land.Mesh.Triangles, 1000, 40000);
        // The band is level ground to the covers as to the light: the channel eased out into it is no streak of deep grass
        var grass = OpenTerrainMesh.SlotsOf(OpenTerrainMesh.Covers(OpenCover.Grass, OpenCover.Grass, OpenCover.Grass));
        for (int t = 0; t < land.Mesh.Count; t += 3)
            if (Beyond(points[t]) <= OpenHorizon.Level && Beyond(points[t + 1]) <= OpenHorizon.Level && Beyond(points[t + 2]) <= OpenHorizon.Level)
            {
                Assert.Equal(grass, OpenTerrainMesh.SlotsOf(land.Mesh.Uvs[t]));
                Assert.Equal(Vector3.UnitY, land.Mesh.Normals[t]);
            }
        // The far forests stand on the land between their reaches, each on a row of the trees' atlas
        Assert.True(land.Trees.Count > 500, $"{land.Trees.Count} far trees");
        Assert.All(land.Trees, t =>
        {
            Assert.InRange(Beyond(t.Foot), OpenHorizon.TreesFrom, OpenHorizon.TreesTo);
            Assert.InRange(t.Row, 0, OpenFoliage.Rows - 1);
        });
        Assert.Equal(land.Trees, OpenHorizon.Of((-2, -2, 1, 1), height, 1234u).Trees);
    }

    // ------------------------------------------------------------------ the light of lit glass

    [Fact]
    public void LitGlassGlowsAfterDarkInTheOpenAirAsTheFieldsDoes()
    {
        // The open air's rig is the battle's of the hour, which lights no glass, with the field's lamps and windows
        var day = OpenLook.Open(13.5f);
        Assert.Equal((0f, 0f), (day.LampGlow, day.HomeGlow));
        var night = OpenLook.Open(22f);
        var field = ArtLook.FieldRig(22f, indoors: false);
        Assert.Equal((field.LampGlow, field.HomeGlow), (night.LampGlow, night.HomeGlow));
        Assert.True(night.HomeGlow > 0.9f && night.LampGlow > 0.9f);
        Assert.Equal(ArtLook.BattleRig(22f).Light, night.Light);
    }

    /// <summary>How far a point lies out from the test region's rectangle (64 m every way from the origin).</summary>
    static float Beyond(Vector3 p)
    {
        float dx = MathF.Max(0, MathF.Abs(p.X) - 64f), dz = MathF.Max(0, MathF.Abs(p.Z) - 64f);
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}
