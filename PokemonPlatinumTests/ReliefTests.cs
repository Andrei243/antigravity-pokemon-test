using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// How the field draws height (style guide, "Relief"; plan 01 · M3): the heights the ground is drawn at, the
/// art of the faces between levels, decks and stairs, the Pokémon that carries a surfer, and the obstacles.
/// Everything here runs without a GPU.
/// </summary>
public class ReliefTests
{
    private const int T = GroundBaker.ArtTile;

    private static Map Level(int width, int height, float at, float groundLevel = 0f)
    {
        var map = new Map(width, height) { Name = "Relief", GroundLevel = groundLevel };
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map.SetHeight(x, y, at);
        return map;
    }

    private static (double Share, int Colours) Cleanliness(PixelCanvas c)
    {
        int alone = 0, opaque = 0;
        var colours = new HashSet<(byte, byte, byte)>();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (!c.IsOpaque(x, y)) continue;
                opaque++;
                var col = c.Get(x, y);
                colours.Add((col.R, col.G, col.B));
                bool lone = true;
                for (int dy = -1; dy <= 1 && lone; dy++)
                    for (int dx = -1; dx <= 1 && lone; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = c.Get(x + dx, y + dy);
                        if (c.IsOpaque(x + dx, y + dy) && n.R == col.R && n.G == col.G && n.B == col.B) lone = false;
                    }
                if (lone) alone++;
            }
        return (opaque == 0 ? 0 : alone / (double)opaque, colours.Count);
    }

    // ------------------------------------------------------------------ drawn heights

    [Fact]
    public void AMapWithoutHeightsIsDrawnFlatAsBefore()
    {
        var map = new Map(6, 6) { Name = "Flat" };
        map.SetGroundTile(2, 2, TileType.Water);
        Assert.False(Relief.Has(map));
        Assert.Equal((0f, 0f, 0f, 0f), Relief.Corners(map, 2, 2));
        Assert.Equal(0f, Relief.At(map, 2.5f, 2.5f));
        Assert.Equal(0f, Relief.Under(map, 2.5f, 2.5f, 0f));
        Assert.Equal((0f, 0f), Relief.Range(map, 0, 0, 5, 5));
    }

    [Fact]
    public void TheLowlandsAreDrawnAtZeroAndWaterLevelWithItsBanks()
    {
        // Sinnoh's numbers: the land a tile up, a pond half a tile under it
        var map = Level(8, 8, 1f, groundLevel: WorldMapBuilder.GroundLevel);
        for (int y = 3; y <= 4; y++)
            for (int x = 3; x <= 4; x++)
            {
                map.SetGroundTile(x, y, TileType.Water);
                map.SetHeight(x, y, 0.5f);
            }
        Assert.True(Relief.Has(map));
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                Assert.Equal((0f, 0f, 0f, 0f), Relief.Corners(map, x, y));

        // The same pond up a mountain is level with the mountain
        var high = Level(8, 8, 4f, groundLevel: 1f);
        high.SetGroundTile(3, 3, TileType.Water);
        high.SetHeight(3, 3, 3.5f);
        Assert.Equal((3f, 3f, 3f, 3f), Relief.Corners(high, 3, 3));
        Assert.Equal(3f, Relief.At(high, 3.5f, 3.5f));

        // The real thing: Twinleaf Town, its pond and Route 219's beach and sea are all drawn flat
        MapDatabase.Initialize();
        var sinnoh = MapDatabase.Get("Sinnoh");
        Assert.True(Relief.Has(sinnoh));
        Assert.Equal(WorldMapBuilder.GroundLevel, sinnoh.GroundLevel);
        Assert.Equal(1f, sinnoh.HeightAt(116, 886));
        for (int y = 832; y < 896; y++)
            for (int x = 96; x < 192; x++)
                Assert.Equal((0f, 0f, 0f, 0f), Relief.Corners(sinnoh, x, y));
        Assert.Contains(Enumerable.Range(864, 32).SelectMany(y => Enumerable.Range(160, 32).Select(x => sinnoh.HeightAt(x, y))), h => h < 1f);
        // ...while Jubilife City, in view to the north as scenery, has a part a tile higher with steps up to it
        Assert.True(Relief.Range(sinnoh, 128, 768, 191, 799).Max >= 1f);
    }

    [Fact]
    public void ASmallStepIsASlopeAndABigOneKeepsItsEdge()
    {
        // West half low, east half half a tile up: the low tiles beside the step rise to meet it
        var kerb = Level(6, 3, 0f);
        for (int y = 0; y < 3; y++)
            for (int x = 3; x < 6; x++)
                kerb.SetHeight(x, y, 0.5f);
        Assert.Equal((0f, 0.5f, 0f, 0.5f), Relief.Corners(kerb, 2, 1));
        Assert.Equal((0f, 0f, 0f, 0f), Relief.Corners(kerb, 1, 1));
        Assert.Equal((0.5f, 0.5f, 0.5f, 0.5f), Relief.Corners(kerb, 3, 1));
        Assert.Equal(0.25f, Relief.At(kerb, 2.5f, 1.5f), 4);
        Assert.Equal(0.5f, Relief.At(kerb, 3.5f, 1.5f), 4);

        // A tile and more is a face: each side keeps its own height at the edge
        var cliff = Level(6, 3, 0f);
        for (int y = 0; y < 3; y++)
            for (int x = 3; x < 6; x++)
                cliff.SetHeight(x, y, 2f);
        Assert.Equal((0f, 0f, 0f, 0f), Relief.Corners(cliff, 2, 1));
        Assert.Equal((2f, 2f, 2f, 2f), Relief.Corners(cliff, 3, 1));
        Assert.Equal((0f, 2f), Relief.Range(cliff, 0, 0, 5, 2));

        // Just under the limit still joins; at the limit it doesn't
        foreach (var (step, joins) in new[] { (Relief.WeldLimit - 0.01f, true), (Relief.WeldLimit, false) })
        {
            var edge = Level(4, 1, 0f);
            edge.SetHeight(2, 0, step);
            edge.SetHeight(3, 0, step);
            Assert.Equal(joins ? step : 0f, Relief.Corners(edge, 1, 0).NE, 4);
        }

        // Three small steps round one corner make one slope up to the highest
        var corner = Level(2, 2, 0f);
        corner.SetHeight(1, 0, 0.5f);
        corner.SetHeight(0, 1, 0.5f);
        corner.SetHeight(1, 1, 1f);
        Assert.Equal(1f, Relief.Corners(corner, 0, 0).SE);
    }

    [Fact]
    public void StairsAreDrawnAsThePlanesTheyAre()
    {
        // Two tiles of stairs climbing north from the ground to a terrace two tiles up
        var map = Level(3, 6, 0f);
        for (int x = 0; x < 3; x++)
        {
            map.SetHeight(x, 0, 2f);
            map.SetHeight(x, 1, 2f);
        }
        map.SetHeight(1, 2, 1.5f, slopeZ: -1f);
        map.SetHeight(1, 3, 0.5f, slopeZ: -1f);

        Assert.Equal((2f, 2f, 1f, 1f), Relief.Corners(map, 1, 2));
        Assert.Equal((1f, 1f, 0f, 0f), Relief.Corners(map, 1, 3));
        Assert.Equal(1.5f, Relief.At(map, 1.5f, 2.5f), 4);
        Assert.Equal((0f, -1f), map.SlopeAt(1, 2));
        Assert.Equal((0f, 0f), map.SlopeAt(1, 1));

        // The ground beside the flight stays where it is: the stairs stand in a notch, with a face each side
        Assert.Equal((2f, 2f, 2f, 2f), Relief.Corners(map, 0, 1));
        Assert.Equal(0f, Relief.Corners(map, 0, 3).NE);
    }

    [Fact]
    public void SomeoneOnABridgeIsDrawnOnItsDeck()
    {
        var map = Level(5, 3, 2f);
        map.SetGroundTile(2, 1, TileType.Planks);
        map.SetBehaviour(2, 1, TileBehavior.BridgeOverWater);
        map.SetHeight(2, 1, -0.5f);
        map.SetDeck(2, 1, 2f);

        Assert.Equal(2f, Relief.Deck(map, 2, 1));
        Assert.Null(Relief.Deck(map, 1, 1));
        Assert.Equal(2f, Relief.Under(map, 2.5f, 1.5f, standingHeight: 2f));
        // On the water under it: the surface, drawn half a tile above the water's own height
        Assert.Equal(0f, Relief.Under(map, 2.5f, 1.5f, standingHeight: -0.5f), 4);
        Assert.True(GroundBaker.IsWaterAt(map, 2, 1));
        Assert.False(GroundBaker.IsWaterAt(map, 1, 1));
    }

    [Fact]
    public void DeepSnowAndMudTakeAWalkerInByRows()
    {
        Assert.Equal(0, WorldRenderer.SinkRows(TileBehavior.None));
        Assert.Equal(0, WorldRenderer.SinkRows(TileBehavior.ShallowSnow));
        Assert.True(WorldRenderer.SinkRows(TileBehavior.DeepSnow) > 0);
        Assert.True(WorldRenderer.SinkRows(TileBehavior.DeeperSnow) > WorldRenderer.SinkRows(TileBehavior.DeepSnow));
        Assert.True(WorldRenderer.SinkRows(TileBehavior.DeepestSnow) > WorldRenderer.SinkRows(TileBehavior.DeeperSnow));
        Assert.True(WorldRenderer.SinkRows(TileBehavior.DeepMud) > WorldRenderer.SinkRows(TileBehavior.Mud));
        // Never past the knee: the sprite is 58 rows tall
        Assert.All(Enum.GetValues<TileBehavior>(), b => Assert.InRange(WorldRenderer.SinkRows(b), 0, 12));
    }

    // ------------------------------------------------------------------ the art

    [Fact]
    public void TheFacesBetweenLevelsAreCleanPixelArtThatRepeatsDownward()
    {
        foreach (var (name, art, width) in new[] { ("bank", NatureArt.BankFace(), 32), ("rock", NatureArt.RockFace(), 32 * NatureArt.RockFaceTiles) })
        {
            Assert.Equal((width, NatureArt.FaceCap + NatureArt.FaceBody), (art.Width, art.Height));
            var (share, colours) = Cleanliness(art);
            Assert.True(share < 0.01, $"{name}: {share:P1} of its texels stand alone");
            Assert.True(colours <= 6, $"{name}: {colours} colours");

            // One bright line right along the top edge, and nothing transparent
            for (int x = 1; x < art.Width; x++) Assert.Equal(art.Get(0, 0), art.Get(x, 0));
            Assert.Equal(art.Width * art.Height, Enumerable.Range(0, art.Height).Sum(y => Enumerable.Range(0, art.Width).Count(x => art.IsOpaque(x, y))));
        }

        // The bank's lip is the ledge's, so a ledge and a bank beside it look alike
        var bank = NatureArt.BankFace();
        var ledge = NatureArt.LedgeFace();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 32; x++)
                Assert.Equal(ledge.Get(x, y), bank.Get(x, y));

        // The rock has beds of uneven thickness, and no crack runs the height of two of them
        var rock = NatureArt.RockFace();
        var crack = new Color(74, 70, 86, 255);
        for (int x = 0; x < rock.Width; x++)
        {
            int run = 0, longest = 0;
            for (int y = 0; y < rock.Height; y++)
            {
                run = rock.Get(x, y).Equals(crack) ? run + 1 : 0;
                longest = Math.Max(longest, run);
            }
            Assert.True(longest <= 10, $"a crack runs {longest} rows down column {x}");
        }

        var falls = NatureArt.Waterfall();
        Assert.Equal((32, 32), (falls.Width, falls.Height));
        Assert.True(Cleanliness(falls).Colours <= 4);
        // Its streaks run the way the water falls: every lit texel has another above or below it
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                var c = falls.Get(x, y);
                if (c.Equals(falls.Get(0, 0))) continue;
                Assert.True(falls.Get(x, (y + 1) % 32).Equals(c) || falls.Get(x, (y + 31) % 32).Equals(c));
            }
    }

    [Fact]
    public void ADecksBoardsLieAcrossTheWayItRuns()
    {
        var groove = new Color(126, 90, 62, 255);
        var rail = new Color(104, 74, 54, 255);

        // A deck that runs east to west: grooves are lines from north to south, eight texels apart
        var c = new PixelCanvas(T, T);
        PixelGround.PaintPlanks(c, 0, 0, 5, 5, runsEastWest: true, west: true, east: true, north: false, south: false);
        for (int y = 1; y < T - 1; y++)
            for (int x = 0; x < T; x++)
                Assert.Equal(x % 8 == 7, c.Get(x, y).Equals(groove));
        // Open to the north and south, so it has a rail along each
        for (int x = 0; x < T; x++)
        {
            Assert.Equal(rail, c.Get(x, 0));
            Assert.Equal(rail, c.Get(x, T - 1));
        }

        // One that runs north to south: the same turned round, railed on its west and east
        var n = new PixelCanvas(T, T);
        PixelGround.PaintPlanks(n, 0, 0, 5, 5, runsEastWest: false, west: false, east: false, north: true, south: true);
        for (int y = 0; y < T; y++)
            for (int x = 1; x < T - 1; x++)
                Assert.Equal(y % 8 == 7, n.Get(x, y).Equals(groove));
        for (int y = 0; y < T; y++) Assert.Equal(rail, n.Get(0, y));
        Assert.True(Cleanliness(c).Share < 0.005);

        // In a baked map the longer run decides: a bridge three tiles long and two wide is boarded across its length
        var map = new Map(9, 8) { Name = "Bridge" };
        for (int y = 0; y < 8; y++) map.SetGroundTile(4, y, TileType.Water);
        for (int x = 3; x <= 5; x++)
            for (int y = 3; y <= 4; y++)
                map.SetGroundTile(x, y, TileType.Planks);
        map.SetBehaviour(4, 3, TileBehavior.BridgeOverWater);
        map.SetBehaviour(4, 4, TileBehavior.BridgeOverWater);
        var ground = PixelGround.Bake(map, new TileWindow(0, 0, 9, 8), pad: 0, worldSeeds: false, out var mask);
        for (int y = 3 * T + 1; y < 5 * T - 1; y++)
            Assert.Equal(groove, ground.Get(4 * T + 7, y));
        Assert.NotEqual(groove, ground.Get(4 * T + 3, 3 * T + 7));

        // The water runs on under the boards: the mask has it right across the bridge, so the river isn't cut in two
        Assert.NotNull(mask);
        Assert.True(mask!.Get(4 * T + 16, 3 * T + 16).A > 0);
        Assert.True(mask.Get(4 * T + 16, 1 * T + 16).A > 0);
        Assert.Equal(0, mask.Get(3 * T + 8, 3 * T + 16).A);   // the bank end of the bridge is on dry land
    }

    [Theory]
    [InlineData(0f, -1f, true, 6)]    // climbing north: each tread's nose is on its south edge
    [InlineData(0f, 1f, true, 1)]     // climbing south: on its north edge
    [InlineData(-1f, 0f, false, 6)]   // climbing west: on its east edge
    [InlineData(1f, 0f, false, 1)]    // climbing east: on its west edge
    [InlineData(0f, 0f, true, 6)]     // steps on flat ground are drawn climbing north
    public void AFlightOfStairsShowsEachTreadsNoseOnItsDownhillEdge(float slopeX, float slopeZ, bool rows, int noseAt)
    {
        var nose = new Color(214, 210, 204, 255);
        var c = new PixelCanvas(T, T);
        PixelGround.PaintStairs(c, 0, 0, slopeX, slopeZ, west: true, east: true, north: true, south: true);
        for (int along = 0; along < T; along++)
            for (int across = 0; across < T; across++)
            {
                var texel = rows ? c.Get(across, along) : c.Get(along, across);
                Assert.Equal(along % 8 == noseAt, texel.Equals(nose));
            }
        Assert.True(Cleanliness(c).Colours == 3);
    }

    [Fact]
    public void TheSwimmerIsOneSpriteSeenFromFourSides()
    {
        var sides = Enumerable.Range(0, 4).Select(SurfMount.Paint).ToList();
        foreach (var art in sides)
        {
            Assert.Equal((SurfMount.Width, SurfMount.Height), (art.Width, art.Height));
            int opaque = 0, nearBlack = 0;
            for (int y = 0; y < art.Height; y++)
                for (int x = 0; x < art.Width; x++)
                {
                    var col = art.Get(x, y);
                    if (col.A == 0) continue;
                    opaque++;
                    if (col.R < 30 && col.G < 30 && col.B < 30) nearBlack++;
                }
            Assert.True(opaque > art.Width * art.Height / 3, "the swimmer is nearly empty");
            Assert.True(opaque < art.Width * art.Height, "the swimmer fills its card: there is no outline to cut out");
            Assert.Equal(0, nearBlack);
            // It floats on the bottom of its card
            Assert.Contains(Enumerable.Range(0, art.Width), x => art.IsOpaque(x, art.Height - 1) || art.IsOpaque(x, art.Height - 2));
            Assert.True(Cleanliness(art).Colours <= 12);
        }

        // Heading left is heading right in a mirror; coming and going differ (a face, a tail)
        for (int y = 0; y < SurfMount.Height; y++)
            for (int x = 0; x < SurfMount.Width; x++)
                Assert.Equal(sides[1].Get(x, y), sides[3].Get(SurfMount.Width - 1 - x, y));
        Assert.Contains(Enumerable.Range(0, SurfMount.Width * SurfMount.Height),
            i => !sides[0].Get(i % SurfMount.Width, i / SurfMount.Width).Equals(sides[2].Get(i % SurfMount.Width, i / SurfMount.Width)));

        // The rider sits low enough to be on its back, not above it
        Assert.InRange(SurfMount.Seat, 6, SurfMount.Height - 6);
    }

    [Fact]
    public void ObstaclesStandInTheWayAndComeFromTheOriginalsObjects()
    {
        Assert.Equal(PropType.CutTree, WorldMapBuilder.ObstacleFor("cut_tree"));
        Assert.Equal(PropType.CrackedRock, WorldMapBuilder.ObstacleFor("rock_smash"));
        Assert.Equal(PropType.StrengthBoulder, WorldMapBuilder.ObstacleFor("strength_boulder"));
        Assert.Null(WorldMapBuilder.ObstacleFor("lass"));

        var map = new Map(5, 5) { Name = "Obstacles" };
        foreach (var (type, x) in new[] { (PropType.CutTree, 1), (PropType.CrackedRock, 2), (PropType.StrengthBoulder, 3) })
        {
            var prop = map.AddProp(type, x, 2);
            Assert.True(prop.IsSolid);
            Assert.False(map.IsWalkable(x, 2));
            Assert.Equal(Obstacle.Solid, FieldMovement.Step(map, x, 3, Direction.Up, new Walker()).Obstacle);
        }

        // Each is a sprite with an outline, like the other things that stand on the ground
        foreach (var (name, w, h, paint) in new (string, int, int, Action<PixelCanvas>)[]
        {
            ("cut tree", 26, 40, OutdoorProps.PaintCutTree), ("cracked rock", 30, 26, OutdoorProps.PaintCrackedRock),
            ("strength boulder", 30, 30, OutdoorProps.PaintStrengthBoulder)
        })
        {
            var c = new PixelCanvas(w, h);
            paint(c);
            int opaque = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var col = c.Get(x, y);
                    if (col.A == 0) continue;
                    opaque++;
                    Assert.False(col.R < 30 && col.G < 30 && col.B < 30, $"{name} has a near-black texel at ({x},{y})");
                }
            Assert.True(opaque > w * h / 4, $"{name} is nearly empty");
            Assert.True(opaque < w * h, $"{name} fills its whole card");
            Assert.Contains(Enumerable.Range(0, w), x => c.IsOpaque(x, h - 1) || c.IsOpaque(x, h - 2));
            // A handful of flat shades, and the outline's darker shade of each
            Assert.True(Cleanliness(c).Colours <= 14, $"{name}: {Cleanliness(c).Colours} colours");
        }

        // The three can be told apart at a glance: green, brown, grey
        static Color Middle(Action<PixelCanvas> paint, int w, int h, int x, int y)
        {
            var c = new PixelCanvas(w, h);
            paint(c);
            return c.Get(x, y);
        }
        var tree = Middle(OutdoorProps.PaintCutTree, 26, 40, 13, 16);
        var cracked = Middle(OutdoorProps.PaintCrackedRock, 30, 26, 6, 12);
        var boulder = Middle(OutdoorProps.PaintStrengthBoulder, 30, 30, 12, 20);
        Assert.True(tree.G > tree.R && tree.G > tree.B);
        Assert.True(cracked.R > cracked.B + 20);
        Assert.True(Math.Abs(boulder.R - boulder.B) < 20);
    }
}
