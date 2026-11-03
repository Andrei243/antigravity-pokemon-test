using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · part r (Hoenn's last pictures): the planks pressed under the player and under the Kecleon nobody sees, Shoal
/// Cave's salt by what is left, the abnormal weather's cave mouths and deep water, the S.S. Tidal's blocks of bulkheads
/// shut in whole, the Abandoned Ship's deckhouse over its blocked tiles alone, and a surfer under the Seaside Cycling Road.
/// </summary>
public class HoennPartRTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map MapNamed(string name) => Maps.Value.Single(m => m.Name == name);

    [Theory]
    [InlineData("ShoalSalt1", "FLAG_DAILY_RECEIVED_SHOAL_SALT_1")]
    [InlineData("shoal_cave_low_tide_lower_room.ShoalSalt4", "FLAG_DAILY_RECEIVED_SHOAL_SALT_4")]
    [InlineData("ShoalShell1", null)]
    [InlineData("ShoalSalt", null)]
    public void APilesFlagIsNamedByItsScript(string script, string? flag) => Assert.Equal(flag, SaltPiles.TakenFlagOf(script));

    [Fact]
    public void ThePileIsWholeThenLittleAndGoneUnderTheTide()
    {
        var room = MapNamed("ShoalCave_LowTideInnerRoom");
        var flags = new HashSet<string>();
        (int, int, SaltLeft) First() => SaltPiles.On(room, flags.Contains).Single(p => p.X == 31 && p.Y == 8);
        Assert.Equal(2, SaltPiles.On(room, flags.Contains).Count());
        Assert.Equal((31, 8, SaltLeft.Whole), First());
        flags.Add("FLAG_DAILY_RECEIVED_SHOAL_SALT_1");
        Assert.Equal((31, 8, SaltLeft.Little), First());
        flags.Add(SaltPiles.TideFlag);
        Assert.Equal((31, 8, SaltLeft.None), First());
    }

    [Fact]
    public void TheAbnormalWeatherOpensACaveMouthAndLaysDeepWater()
    {
        var hoenn = MapNamed("Hoenn");
        var flags = new HashSet<string>();
        Assert.DoesNotContain(GameEngine.StoryMarksOn(hoenn, flags.Contains), m => m.Kind is StoryMarkKind.CaveMouth or StoryMarkKind.DeepWater);
        flags.Add("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_114_SOUTH");
        Assert.Contains(new StoryMark(46, 48, StoryMarkKind.CaveMouth), GameEngine.StoryMarksOn(hoenn, flags.Contains).ToList());
        flags.Clear();
        flags.Add("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_105_NORTH");
        var deep = GameEngine.StoryMarksOn(hoenn, flags.Contains).Where(m => m.Kind == StoryMarkKind.DeepWater).ToList();
        Assert.Equal(4, deep.Count);
        Assert.All(deep, m => Assert.True(m.X is 10 or 11 && m.Y is 291 or 292, $"{m}"));
        // On screen the four are one patch, two tiles by two, with no seams between them
        var life = new FieldLife();
        life.SetMarks(hoenn, deep);
        List<LifeQuad> flat = new(), upright = new();
        life.MarkQuads(hoenn, Array.Empty<NPC>(), flat, upright);
        var patch = Assert.Single(flat);
        Assert.Equal((FieldLife.DeepWater, 2f, 2f, 11f, 292f), (patch.Cell, patch.Width, patch.Height, patch.At.X, patch.At.Z));
        Assert.Empty(upright);
    }

    [Fact]
    public void APlankIsPressedUnderTheKecleonNobodySeesAndNotOnceItIsGone()
    {
        var hoenn = MapNamed("Hoenn");
        var kecleon = hoenn.Everyone.Single(n => n.Key == "bridge_kecleon");
        Assert.True(kecleon.IsUnseen && FieldLife.UnderAnUnseenFoot(hoenn, kecleon.GridX, kecleon.GridY));
        var life = new FieldLife();
        List<LifeQuad> flat = new(), upright = new();
        life.MarkQuads(hoenn, new[] { kecleon }, flat, upright);
        var pressed = Assert.Single(flat);
        Assert.Equal(FieldLife.PressedPlank, pressed.Cell);
        Assert.Equal(kecleon.GridX + 0.5f, pressed.At.X);
        // Gone (its flag set, off the map): nobody presses the plank
        flat.Clear();
        life.MarkQuads(hoenn, Array.Empty<NPC>(), flat, upright);
        Assert.Empty(flat);
    }

    [Fact]
    public void TheMarksAreBlocksOfTheLifeAtlas()
    {
        var atlas = LifeArt.Atlas();
        Assert.Equal(FieldLife.Cell * FieldLife.AtlasRows, atlas.Height);
        foreach (int cell in new[] { FieldLife.PressedPlank, FieldLife.SaltHeap, FieldLife.SaltLittle, FieldLife.CaveMouth, FieldLife.DeepWater })
        {
            Assert.True(FieldLife.IsMark(cell));
            int x0 = cell % FieldLife.Columns * FieldLife.Cell, y0 = cell / FieldLife.Columns * FieldLife.Cell;
            Assert.True(x0 + FieldLife.Cell * 2 <= atlas.Width && y0 + FieldLife.Cell * 2 <= atlas.Height, $"block {cell}");
            int painted = 0;
            for (int y = y0; y < y0 + FieldLife.Cell * 2; y++)
                for (int x = x0; x < x0 + FieldLife.Cell * 2; x++)
                    if (atlas.IsOpaque(x, y) || atlas.Get(x, y).A > 0) painted++;
            Assert.True(painted > 40, $"block {cell} has {painted} texels painted");
        }
        // Life's own pieces are no marks
        Assert.False(FieldLife.IsMark(FieldLife.Bubble + 2));
    }

    [Fact]
    public void ARunOfWallsShutInByPartitionsIsBuiltAsOne()
    {
        // A block of walls five by five in a hall of floor: its middle has no floor within two tiles, yet it is shut in
        string[] plan =
        {
            "#########",
            "#.......#",
            "#.#####.#",
            "#.#####.#",
            "#.#####.#",
            "#.#####.#",
            "#.#####.#",
            "#.......#",
            "#########",
            "#########",
            "#########",
            "#########",
        };
        int w = plan[0].Length, h = plan.Length;
        bool Wall(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && plan[y][x] == '#';
        bool Floor(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && plan[y][x] == '.';
        var shut = MapScene.ShutInBetweenPartitions(w, h, Wall, Floor);
        Assert.True(shut[4, 4]);
        // The solid beyond the hall reaches the edge: left dark
        Assert.False(shut[4, 11]);
        Assert.False(MapScene.PartitionNearFloor(Floor, 4, 4));
        Assert.True(MapScene.PartitionNearFloor(Floor, 3, 3));
    }

    [Fact]
    public void TheSSTidalCorridorsMiddleIsShutIn()
    {
        var corridor = MapNamed("SSTidalCorridor");
        bool Wall(int x, int y) => corridor.InBounds(x, y) && corridor.GetGroundTile(x, y) == TileType.Wall;
        bool Floor(int x, int y) => corridor.InBounds(x, y) && corridor.GetGroundTile(x, y) != TileType.Wall;
        var shut = MapScene.ShutInBetweenPartitions(corridor.Width, corridor.Height, Wall, Floor);
        Assert.Contains(Enumerable.Range(0, corridor.Width), x => shut[x, 6]);
    }

    [Fact]
    public void TheAbandonedShipsDeckhouseStandsOnlyOverBlockedTiles()
    {
        var deck = MapNamed("AbandonedShip_Deck");
        var cabins = deck.Props.Where(p => p.Type == PropType.ShipCabin).ToList();
        Assert.True(cabins.Count >= 10);
        foreach (var p in cabins)
            for (int y = p.Y; y < p.Y + p.Depth; y++)
                for (int x = p.X; x < p.X + p.Width; x++)
                    Assert.True(deck.IsSolid(x, y) && deck.GetWarpAt(x, y) == null, $"({x}, {y}) under a deckhouse is walked");
    }

    [Fact]
    public void ASurferPassesUnderTheSeasideCyclingRoad()
    {
        var hoenn = MapNamed("Hoenn");
        int x = 209;
        float height = hoenn.HeightAt(x, 196);
        // West to east across the road's deck at its north end, where Emerald's sea runs under it
        for (int i = 0; i < 6; i++)
        {
            var step = FieldMovement.Step(hoenn, x, 196, Direction.Right, new Walker(TravelMode.Surfing, height, Moves: FieldMoves.Surf));
            Assert.True(step.Moves, $"blocked at ({x}, 196) by {step.Obstacle}");
            Assert.Equal(TravelMode.Surfing, step.Mode);
            x = step.X;
            height = step.Height;
        }
        Assert.True(hoenn.DeckAt(212, 196) is { } deck && deck > height + 1f);
    }
}
