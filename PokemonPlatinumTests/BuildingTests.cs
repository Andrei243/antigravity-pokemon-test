using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>Buildings, props and rooms (plan 04 · G5): the art kit, the styles per town, the lights after dark and the map data behind them.</summary>
[Collection("MapDatabase")]
public class BuildingTests
{
    private const float OutdoorVS = 1.944f, IndoorVS = 1.624f;

    public static IEnumerable<object[]> MapNames()
    {
        MapDatabase.Initialize();
        return MapDatabase.MapNames.OrderBy(n => n).Select(n => new object[] { n });
    }

    private sealed record Built(KitBuilder Kit, MeshBuilder PublicLight, MeshBuilder HomeLight, Dictionary<string, MeshBuilder> Roofs);

    /// <summary>Builds a map's buildings and props the way the scene does, without a GPU.</summary>
    private static Built Build(Map map)
    {
        var built = new Built(new KitBuilder(new ArtSheet(), map.IsIndoors ? IndoorVS : OutdoorVS), new MeshBuilder(), new MeshBuilder(), new());
        if (map.IsIndoors)
        {
            foreach (var prop in map.Props) PropModels.Build(built.Kit, prop, map);
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.GetGroundTile(x, y) == TileType.PC) PropModels.BuildPc(built.Kit, x, y);
            return built;
        }

        var targets = new BuildingTargets
        {
            RoofTiles = color =>
            {
                string key = $"{color.R},{color.G},{color.B}";
                if (!built.Roofs.TryGetValue(key, out var roof)) built.Roofs[key] = roof = new MeshBuilder();
                return roof;
            },
            PublicLight = built.PublicLight,
            HomeLight = built.HomeLight
        };
        foreach (var b in MapStructures.FindBuildings(map)) BuildingModels.Add(built.Kit, b, BuildingArt.StyleOf(b, map.ArchitectureAt(b.X0, b.Y0)), targets);
        OutdoorProps.Add(built.Kit, map, built.PublicLight);
        return built;
    }

    private static BuildingInfo Building(string map, int x, int y) =>
        MapStructures.FindBuildings(Fixtures.Any(map)).Single(b => x >= b.X0 && x <= b.X1 && y >= b.Y0 && y <= b.Y1);

    private static BuildingStyle Style(string map, int x, int y) =>
        BuildingArt.StyleOf(Building(map, x, y), Fixtures.Any(map).ArchitectureAt(x, y));

    private static PixelCanvas Front(string map, int x, int y)
    {
        var b = Building(map, x, y);
        var s = BuildingArt.StyleOf(b, Fixtures.Any(map).ArchitectureAt(x, y));
        var c = new PixelCanvas(BuildingArt.FrontWidth(b), s.WallHeight);
        BuildingArt.PaintFront(c, b, s);
        return c;
    }

    private static int Count(PixelCanvas c, Func<Color, bool> match)
    {
        int n = 0;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                if (match(c.Get(x, y))) n++;
        return n;
    }

    /// <summary>The share of a canvas's opaque texels that have no neighbour of their own colour (specks), and how many there are.</summary>
    private static (double Share, int Opaque, int Colours, int Alone) Cleanliness(PixelCanvas c)
    {
        int isolated = 0, opaque = 0;
        var colours = new HashSet<(byte, byte, byte)>();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (!c.IsOpaque(x, y)) continue;
                opaque++;
                var col = c.Get(x, y);
                colours.Add((col.R, col.G, col.B));
                bool alone = true;
                for (int dy = -1; dy <= 1 && alone; dy++)
                    for (int dx = -1; dx <= 1 && alone; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = c.Get(x + dx, y + dy);
                        if (c.IsOpaque(x + dx, y + dy) && n.R == col.R && n.G == col.G && n.B == col.B) alone = false;
                    }
                if (alone) isolated++;
            }
        return (opaque == 0 ? 0 : isolated / (double)opaque, opaque, colours.Count, isolated);
    }

    // ------------------------------------------------------------------ the art

    /// <summary>
    /// "No per-texel noise anywhere": every face painted for every map (walls, roofs' trim, props, furniture) is
    /// placed shapes in flat shades. Almost no texel stands alone (a small sprite may have a handful of single
    /// accents), and no face has the hundreds of colours a gradient or noise would give.
    /// </summary>
    [Theory]
    [MemberData(nameof(MapNames))]
    public void EveryPaintedFaceIsCleanPixelArt(string mapName)
    {
        var map = MapDatabase.Get(mapName);
        var sheet = Build(map).Kit.Sheet;
        foreach (var (key, art) in sheet.Faces)
        {
            var (share, opaque, colours, alone) = Cleanliness(art);
            Assert.True(alone <= Math.Max(6, opaque / 50), $"{mapName} {key}: {alone} of its {opaque} texels stand alone ({share:P1})");
            Assert.True(colours <= 96, $"{mapName} {key}: {colours} colours");
        }

        if (map.IsIndoors)
        {
            var floor = GroundBaker.BakeInterior(map);
            Assert.True(Cleanliness(floor).Share < 0.01, $"{mapName}: the floor has specks");
            var wall = new PixelCanvas(96, PropModels.WallHeight);
            GroundBaker.PaintWall(wall, map.Interior);
            Assert.True(Cleanliness(wall).Share < 0.01, $"{mapName}: the wall has specks");
        }
    }

    [Fact]
    public void RoofTilesAreFlatShadesInRowsOfEight()
    {
        var teal = new Color(52, 166, 138, 255);
        var tiles = BuildingArt.RoofTiles(teal);
        Assert.Equal((64, 64), (tiles.Width, tiles.Height));
        var (share, _, colours, _) = Cleanliness(tiles);
        Assert.True(share < 0.01);
        Assert.True(colours <= 7, $"{colours} shades in the roof tiles");

        // Every eighth row is the dark line under a row of tiles, the same right across
        for (int y = 7; y < 64; y += 8)
            for (int x = 1; x < 64; x++)
                Assert.Equal(tiles.Get(0, y), tiles.Get(x, y));
        Assert.NotEqual(tiles.Get(3, 3), tiles.Get(0, 7));

        // A roof of another colour is another texture, hue and all
        Assert.NotEqual(tiles.Get(3, 3), BuildingArt.RoofTiles(new Color(214, 82, 66, 255)).Get(3, 3));
    }

    [Fact]
    public void TheSignAlphabetHasEveryLetter()
    {
        Assert.Equal(46, Pix.TextWidth("MART", 2));
        var shapes = new HashSet<string>();
        foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var c = new PixelCanvas(5, 7);
            Pix.Text(c, 0, 0, letter.ToString(), Color.White);
            int ink = Count(c, col => col.A > 0);
            Assert.InRange(ink, 9, 26);
            var bits = string.Concat(Enumerable.Range(0, 35).Select(i => c.IsOpaque(i % 5, i / 5) ? '#' : '.'));
            Assert.True(shapes.Add(bits), $"{letter} looks like another letter");
        }

        // Larger lettering is the same shapes in bigger blocks
        var big = new PixelCanvas(12, 16);
        Pix.Text(big, 0, 0, "T", Color.White, scale: 2);
        Assert.Equal(4 * 11, Count(big, col => col.A > 0));
    }

    // ------------------------------------------------------------------ styles

    [Fact]
    public void HousesFollowTheirTownAndPublicBuildingsLookTheSameEverywhere()
    {
        MapDatabase.Initialize();

        var twinleaf = Style("TwinleafTown", 6, 5);
        Assert.Equal((WallKind.Planks, RoofShape.Gable, WindowKind.Cottage), (twinleaf.Wall, twinleaf.Roof, twinleaf.Window));
        Assert.True(twinleaf is { Home: true, Chimney: true, Shutters: true, FlowerBoxes: true, Portal: 0, Storeys: 1 });
        Assert.Equal(new Color(52, 166, 138, 255), twinleaf.RoofColor);   // Twinleaf's teal

        var pallet = Style("PalletTown", 5, 4);
        Assert.Equal(WallKind.Clapboard, pallet.Wall);
        Assert.Equal(new Color(214, 82, 66, 255), pallet.RoofColor);

        foreach (var (map, x, y) in new[] { ("SandgemTown", 6, 4), ("JubilifeCity", 25, 24) })
        {
            var center = Style(map, x, y);
            Assert.Equal((WallKind.Plaster, RoofShape.Hip, SignKind.Center), (center.Wall, center.Roof, center.Sign));
            Assert.True(center is { GlassDoor: true, Home: false } && center.Portal > 0);
        }
        Assert.Equal(SignKind.Mart, Style("SandgemTown", 22, 4).Sign);
        Assert.Equal(SignKind.Lab, Style("SandgemTown", 7, 15).Sign);
        Assert.NotEqual(Style("SandgemTown", 6, 4).RoofColor, Style("SandgemTown", 22, 4).RoofColor);
    }

    [Fact]
    public void JubilifeIsACityOfNamedBuildings()
    {
        MapDatabase.Initialize();
        var city = MapDatabase.Get("JubilifeCity");
        Assert.Equal(Architecture.City, city.Architecture);
        Assert.Equal(Architecture.Timber, MapDatabase.Get("Sinnoh").ArchitectureAt(112, 880));   // Twinleaf Town

        var kinds = MapStructures.FindBuildings(city).Select(b => b.Kind).OrderBy(k => k).ToList();
        Assert.Equal(new[]
        {
            BuildingKind.PokemonCenter, BuildingKind.PokeMart, BuildingKind.School, BuildingKind.Office,
            BuildingKind.TvStation, BuildingKind.Terminal, BuildingKind.Apartments
        }, kinds);

        // The television station is the tallest: three storeys under a flat roof, with its mast and dish
        var tv = Style("JubilifeCity", 33, 5);
        Assert.Equal((RoofShape.Flat, 3, SignKind.Tv), (tv.Roof, tv.Storeys, tv.Sign));
        Assert.True(tv.Gear.HasFlag(RoofGear.Mast) && tv.Gear.HasFlag(RoofGear.Dish));
        Assert.True(tv.WallHeight > Style("JubilifeCity", 25, 5).WallHeight);
        Assert.True(Style("JubilifeCity", 25, 5).WallHeight > Style("JubilifeCity", 25, 24).WallHeight);

        // The school keeps a pitched roof and a wide entrance for its name
        var school = Style("JubilifeCity", 5, 5);
        Assert.Equal((WallKind.Brick, RoofShape.Gable, SignKind.School), (school.Wall, school.Roof, school.Sign));
        Assert.True(school.Portal >= Pix.TextWidth("SCHOOL", 2) + 8);
        Assert.True(Style("JubilifeCity", 14, 24) is { Home: true, Balconies: true, Wall: WallKind.Brick });
    }

    [Fact]
    public void AFrontWallHasWhatTheMapPutsOnItAndWindowsInTheOtherBays()
    {
        MapDatabase.Initialize();
        var home = Building("TwinleafTown", 6, 5);
        Assert.Equal(new[] { BuildingArt.BayKind.Window, BuildingArt.BayKind.Plaque, BuildingArt.BayKind.Door, BuildingArt.BayKind.Blank, BuildingArt.BayKind.Window },
            BuildingArt.BaysOf(home));

        // A building the map gives no door still shows an entrance: one closed door, beside its plaque
        var tv = Building("JubilifeCity", 33, 5);
        Assert.Empty(tv.Doors);
        var bays = BuildingArt.BaysOf(tv);
        Assert.Single(bays, b => b == BuildingArt.BayKind.Door);
        Assert.Equal(BuildingArt.BayKind.Plaque, bays[tv.Plaques[0] - tv.X0]);
        Assert.Equal(BuildingArt.BayKind.Door, bays[BuildingArt.ClosedEntrance(tv)]);
    }

    // ------------------------------------------------------------------ lights after dark

    [Fact]
    public void GlassIsMarkedTexelByTexelForTheNight()
    {
        MapDatabase.Initialize();
        static bool Public(Color c) => c.A == ArtSheet.PublicLight;
        static bool Home(Color c) => c.A == ArtSheet.HomeLight;

        // A home: its windows are home lights (or public ones in a house that stays up), the lantern by the door burns all night
        var house = Front("TwinleafTown", 6, 5);
        var b = Building("TwinleafTown", 6, 5);
        byte windows = BuildingArt.WindowLight(b, BuildingArt.StyleOf(b, Architecture.Timber));
        Assert.True(Count(house, c => c.A == windows) > 400, "two windows and the door's pane should be marked");
        Assert.True(Count(house, Public) >= 15, "the lantern's glass should burn all night");
        Assert.True(Count(house, c => c.A == 255) > house.Width * house.Height / 2, "walls are not lit");

        // A shop: every pane is a public light; the name on a Mart glows, the band behind it doesn't
        var mart = Building("SandgemTown", 22, 4);
        var style = BuildingArt.StyleOf(mart, Architecture.Plaster);
        var portal = new PixelCanvas(style.Portal, BuildingArt.PortalHeight);
        BuildingArt.PaintPortal(portal, mart, style);
        Assert.Equal(0, Count(portal, Home));
        Assert.True(Count(portal, c => Public(c) && c.R == 255 && c.G == 255 && c.B == 255) > 100, "the letters of MART glow");
        Assert.Equal(0, Count(portal, c => Public(c) && c.R == style.Accent.R && c.G == style.Accent.G && c.B == style.Accent.B));
        Assert.Equal(0, Count(Front("SandgemTown", 22, 4), Home));

        // A door that leads nowhere is shut and dark; the building's windows are still lit
        var tv = Building("JubilifeCity", 33, 5);
        var front = Front("JubilifeCity", 33, 5);
        int cx = BuildingArt.BayCenter(BuildingArt.ClosedEntrance(tv));
        for (int y = front.Height - 34; y < front.Height - 6; y++)
            for (int x = cx - 14; x <= cx + 14; x++)
                Assert.Equal(255, front.Get(x, y).A);
        Assert.True(Count(front, Public) > 1000);
    }

    [Fact]
    public void AboutOneHomeInThreeKeepsALightOnLateAtNight()
    {
        var style = new BuildingStyle { Home = true };
        var lights = Enumerable.Range(0, 60).Select(i => BuildingArt.WindowLight(new BuildingInfo { X0 = i * 7 % 40, Y0 = i * 3 % 30, X1 = 50, Y1 = 40 }, style)).ToList();
        int awake = lights.Count(l => l == ArtSheet.PublicLight);
        Assert.InRange(awake, 12, 28);
        Assert.All(lights, l => Assert.True(l is ArtSheet.PublicLight or ArtSheet.HomeLight));

        // A shop's windows always burn all night
        Assert.Equal(ArtSheet.PublicLight, BuildingArt.WindowLight(new BuildingInfo { X0 = 1, Y0 = 1, X1 = 5, Y1 = 4 }, new BuildingStyle()));

        // The shader tells the two apart by alpha, and neither is cut out
        Assert.InRange((float)ArtSheet.HomeLight, 0.5f * 255, 0.7f * 255);
        Assert.InRange((float)ArtSheet.PublicLight, 0.7f * 255, 0.95f * 255);
    }

    [Fact]
    public void LitDoorsWindowsAndLampsThrowLightOnTheGround()
    {
        MapDatabase.Initialize();
        var twinleaf = Build(Fixtures.Map("TwinleafTown"));
        // Two lamps (a pool and a halo each), two front doors with their lanterns, and four house windows
        Assert.Equal((2 * 2 + 2 + 4) * 6, twinleaf.PublicLight.VertexCount + twinleaf.HomeLight.VertexCount);
        // The lamps and lanterns burn all night whatever the houses do
        Assert.True(twinleaf.PublicLight.VertexCount >= (2 * 2 + 2) * 6);

        // The television station's door is shut: no light spills from it, only from its windows
        var jubilife = MapDatabase.Get("JubilifeCity");
        int lamps = jubilife.Props.Count(p => p.Type == PropType.LampPost);
        int openDoors = MapStructures.FindBuildings(jubilife).Sum(b => b.Doors.Count);
        int windows = MapStructures.FindBuildings(jubilife).Sum(b => BuildingArt.BaysOf(b).Count(k => k == BuildingArt.BayKind.Window));
        var built = Build(jubilife);
        Assert.Equal((lamps * 2 + openDoors + windows) * 6, built.PublicLight.VertexCount + built.HomeLight.VertexCount);
    }

    // ------------------------------------------------------------------ geometry

    [Theory]
    [InlineData(40f)]
    [InlineData(56f)]
    [InlineData(72f)]
    [InlineData(104f)]
    public void APitchedRoofIsAWholeNumberOfTileRows(float run)
    {
        var pitch = BuildingModels.Pitch.For(run, OutdoorVS);
        Assert.Equal(pitch.Rows * 8f, pitch.Slope);

        // At least as steep as 33 degrees, and less than a row of tiles steeper
        float target = run / MathF.Cos(33f * MathF.PI / 180f);
        Assert.InRange(pitch.Slope, target - 0.01f, target + 8f);
        Assert.Equal(MathF.Sqrt(pitch.Slope * pitch.Slope - run * run), pitch.Tan * run, 3);
        Assert.Equal(pitch.Tan * run / OutdoorVS, pitch.RiseRows, 3);
        Assert.Equal(pitch.RiseRows / 2f, pitch.Drop(run / 2f, OutdoorVS), 3);
    }

    [Fact]
    public void AHouseIsWallsARoofInItsOwnColourAndAChimney()
    {
        MapDatabase.Initialize();
        var built = Build(Fixtures.Map("TwinleafTown"));

        // Both houses share one teal roof texture: two slopes each
        Assert.Equal(new[] { "52,166,138" }, built.Roofs.Keys.ToArray());
        Assert.Equal(2 * 2 * 6, built.Roofs.Values.Single().VertexCount);

        var faces = built.Kit.Sheet.Faces.Select(f => f.Key).ToList();
        Assert.Contains("b4_4.front", faces);
        Assert.Contains("b15_4.side", faces);
        Assert.Contains("chimney.top", faces);
        Assert.Contains(faces, f => f.StartsWith("fence.wood.post"));
        Assert.Contains("lamp", faces);
        Assert.Contains("mailbox", faces);
        Assert.Contains("signpost", faces);

        // The ridge stands well above the walls; only the boulder in the pond reaches below the ground
        var (min, max) = built.Kit.Solid.Bounds();
        float wallTop = BuildingArt.PitchedWall * KitBuilder.Texel * OutdoorVS;
        Assert.True(max.Y > wallTop * 1.25f);
        Assert.InRange(min.Y, -0.25f, 0f);

        // A front wall is painted at one texel per texel: five tiles less the inset at each end
        var front = Front("TwinleafTown", 6, 5);
        Assert.Equal((5 * 32 - 4, BuildingArt.PitchedWall), (front.Width, front.Height));
    }

    [Fact]
    public void ArtIsPackedWithoutOverlapAndPaintedOnce()
    {
        var sheet = new ArtSheet();
        var placed = new List<Art>();
        int paints = 0;
        for (int i = 0; i < 60; i++)
        {
            int w = 5 + i * 37 % 200, h = 3 + i * 13 % 70;
            byte shade = (byte)(i + 1);
            placed.Add(sheet.Paint("face" + i, w, h, c => { paints++; c.Fill(new Color(shade, shade, shade, (byte)255)); }));
        }
        Assert.Equal(60, paints);
        Assert.Equal(placed[7], sheet.Paint("face7", 99, 99, _ => paints++));
        Assert.Equal(60, paints);

        // Every face keeps a texel of border to itself, inside the sheet
        for (int i = 0; i < placed.Count; i++)
        {
            var a = placed[i];
            Assert.True(a.X >= 1 && a.Y >= 1 && a.X + a.Width + 1 <= ArtSheet.Width && a.Y + a.Height + 1 <= sheet.Height);
            for (int j = i + 1; j < placed.Count; j++)
            {
                var b = placed[j];
                bool apart = a.X + a.Width + 1 <= b.X - 1 || b.X + b.Width + 1 <= a.X - 1 || a.Y + a.Height + 1 <= b.Y - 1 || b.Y + b.Height + 1 <= a.Y - 1;
                Assert.True(apart, $"faces {i} and {j} overlap");
            }
        }

        // The sheet holds each face where it was placed, its edge repeated once all round
        var canvas = sheet.ToCanvas();
        var third = placed[3];
        Assert.Equal(4, canvas.Get(third.X, third.Y).R);
        Assert.Equal(4, canvas.Get(third.X - 1, third.Y - 1).R);
        Assert.Equal(4, canvas.Get(third.X + third.Width, third.Y + third.Height).R);
        Assert.Throws<ArgumentException>(() => sheet.Paint("too wide", ArtSheet.Width, 4, _ => { }));
    }

    [Fact]
    public void ABoxShowsItsArtAtOneTexelPerTexel()
    {
        var kit = new KitBuilder(new ArtSheet(), OutdoorVS) { Origin = new Vector3(3, 0, 5) };
        var art = kit.Face("front", 20, 12, c => c.Fill(Color.White));
        kit.Box(4, 24, 0, 8, 0, 12, south: art);

        // One quad: 20 texels wide, 12 screen rows tall (stretched for the steep camera), on the box's south side
        Assert.Equal(6, kit.Solid.VertexCount);
        var (min, max) = kit.Solid.Bounds();
        Assert.Equal(3 + 4 / 32f, min.X, 4);
        Assert.Equal(3 + 24 / 32f, max.X, 4);
        Assert.Equal(12 / 32f * OutdoorVS, max.Y, 4);
        Assert.Equal(5 + 8 / 32f, min.Z, 4);
        Assert.Equal(min.Z, max.Z, 4);

        // Its texture coordinates are the art's rectangle, in texels until the sheet's size is final
        var uv = kit.Solid.TexCoords;
        Assert.Equal(art.X, uv.Min(t => t.X));
        Assert.Equal(art.X + 20, uv.Max(t => t.X));
        Assert.Equal(art.Y + 12, uv.Max(t => t.Y));
        kit.Finish();
        Assert.All(kit.Solid.TexCoords, t => Assert.True(t.X is > 0f and < 1f && t.Y is > 0f and <= 1f));
        Assert.Equal((art.X + 20f) / ArtSheet.Width, kit.Solid.TexCoords.Max(t => t.X), 5);
    }

    // ------------------------------------------------------------------ street furniture

    [Theory]
    [InlineData("TwinleafTown", 2)]
    [InlineData("SandgemTown", 4)]
    [InlineData("JubilifeCity", 16)]
    [InlineData("PalletTown", 2)]
    public void StreetFurnitureBlocksTheWay(string mapName, int lampsAtLeast)
    {
        MapDatabase.Initialize();
        var map = Fixtures.Any(mapName);
        var street = map.Props.Where(p => p.Type is PropType.Fence or PropType.LampPost or PropType.Mailbox or PropType.Planter or PropType.Bench).ToList();
        Assert.True(street.Count(p => p.Type == PropType.LampPost) >= lampsAtLeast);

        foreach (var prop in street)
        {
            Assert.True(prop.IsSolid);
            for (int y = prop.Y; y < prop.Y + prop.Depth; y++)
                for (int x = prop.X; x < prop.X + prop.Width; x++)
                {
                    Assert.False(map.IsWalkable(x, y), $"{mapName}: the {prop.Type} at {x},{y} should block the way");
                    Assert.True(map.GetGroundTile(x, y) is TileType.Grass or TileType.Path or TileType.FlowerGrass, $"{mapName}: {prop.Type} on {map.GetGroundTile(x, y)}");
                    Assert.Null(map.GetWarpAt(x, y));
                }
        }

        // Nobody is fenced in: every character and every door can still be reached from the map's first warp
        var reached = Reachable(map);
        foreach (var npc in map.NPCs.Where(n => !n.IsPCTerminal))
            Assert.True(Neighbours(npc.GridX, npc.GridY).Any(reached.Contains), $"{mapName}: nobody can walk up to {npc.Name}");
        foreach (var warp in map.Warps)
            Assert.True(reached.Contains((warp.SourceX, warp.SourceY)), $"{mapName}: the way to {warp.TargetMap} at {warp.SourceX},{warp.SourceY} is blocked");
    }

    private static IEnumerable<(int, int)> Neighbours(int x, int y) => new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) };

    private static HashSet<(int, int)> Reachable(Map map)
    {
        var start = (map.Warps[0].SourceX, map.Warps[0].SourceY);
        var seen = new HashSet<(int, int)> { start };
        var queue = new Queue<(int, int)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in Neighbours(x, y))
            {
                // Ledges are one-way, so they are left out: a place reached without them is certainly reachable
                if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny) || map.IsLedge(nx, ny) || !seen.Add((nx, ny))) continue;
                queue.Enqueue((nx, ny));
            }
        }
        return seen;
    }

    [Fact]
    public void FencesJoinUpAndLeaveTheGardenGateOpen()
    {
        MapDatabase.Initialize();
        var twinleaf = Fixtures.Map("TwinleafTown");
        var fenced = new HashSet<(int, int)>();
        foreach (var f in twinleaf.Props.Where(p => p.Type == PropType.Fence))
            for (int y = f.Y; y < f.Y + f.Depth; y++)
                for (int x = f.X; x < f.X + f.Width; x++)
                    fenced.Add((x, y));
        Assert.NotEmpty(fenced);

        // The way from the player's front door to the road is open, with the mailbox beside the gate
        Assert.True(twinleaf.IsWalkable(6, 8) && twinleaf.IsWalkable(6, 9) && twinleaf.IsWalkable(6, 10));
        Assert.Contains(twinleaf.Props, p => p.Type == PropType.Mailbox && p.X == 7 && p.Y == 9);
        Assert.Contains((5, 9), fenced);

        // A post for every fenced tile, and two rails toward each fenced neighbour
        var kit = new KitBuilder(new ArtSheet(), OutdoorVS);
        OutdoorProps.Add(kit, new Map(8, 8) { Name = "Fences" }.Also(m => m.AddProp(PropType.Fence, 2, 2, 3, 1)), new MeshBuilder());
        int Quads(int posts, int rails) => (posts * 4 + rails * 4) * 6;
        Assert.Equal(Quads(posts: 3, rails: 2 * 4), kit.Solid.VertexCount);

        // Pallet's fences are white, Twinleaf's are wood
        Assert.Contains(Build(MapDatabase.Get("PalletTown")).Kit.Sheet.Faces, f => f.Key.StartsWith("fence.white"));
        Assert.DoesNotContain(Build(twinleaf).Kit.Sheet.Faces, f => f.Key.StartsWith("fence.white"));
    }

    [Fact]
    public void SpritesHaveAnOutlineThatIsNeverNearBlack()
    {
        foreach (var (name, w, h, paint) in new (string, int, int, Action<PixelCanvas>)[]
        {
            ("lamp", 16, OutdoorProps.LampHeight, OutdoorProps.PaintLamp), ("mailbox", 16, 30, OutdoorProps.PaintMailbox),
            ("planter", 30, 28, OutdoorProps.PaintPlanter), ("bench", 60, 32, OutdoorProps.PaintBench),
            ("signpost", 30, 30, OutdoorProps.PaintSignpost), ("boulder", 36, 30, c => OutdoorProps.PaintBoulder(c, false)),
            ("plant", 28, 46, PropModels.PaintPlant), ("vase", 12, 16, PropModels.PaintVase)
        })
        {
            var c = new PixelCanvas(w, h);
            paint(c);
            int opaque = Count(c, col => col.A > 0);
            Assert.True(opaque > w * h / 6, $"{name} is nearly empty");
            Assert.True(opaque < w * h, $"{name} fills its whole card: it has no outline to cut out");
            Assert.Equal(0, Count(c, col => col.A > 0 && col.R < 30 && col.G < 30 && col.B < 30));
            // It stands on the bottom of its card
            Assert.Contains(Enumerable.Range(0, w), x => c.IsOpaque(x, h - 1) || c.IsOpaque(x, h - 2));
        }

        // The lamp's glass burns all night; a mirrored boulder is the same stone turned round
        var lamp = new PixelCanvas(16, OutdoorProps.LampHeight);
        OutdoorProps.PaintLamp(lamp);
        Assert.True(Count(lamp, col => col.A == ArtSheet.PublicLight) >= 40);
        var rock = new PixelCanvas(36, 30);
        var turned = new PixelCanvas(36, 30);
        OutdoorProps.PaintBoulder(rock, false);
        OutdoorProps.PaintBoulder(turned, true);
        for (int y = 0; y < 30; y++)
            for (int x = 0; x < 36; x++)
                Assert.Equal(rock.IsOpaque(x, y), turned.IsOpaque(35 - x, y));
        Assert.Contains(Enumerable.Range(0, 30), y => rock.IsOpaque(4, y) != turned.IsOpaque(4, y));
    }

    // ------------------------------------------------------------------ rooms

    [Theory]
    [MemberData(nameof(MapNames))]
    public void EveryPieceOfFurnitureStandsOnItsOwnTiles(string mapName)
    {
        var map = MapDatabase.Get(mapName);
        if (!map.IsIndoors) return;

        foreach (var prop in map.Props)
        {
            var kit = new KitBuilder(new ArtSheet(), IndoorVS);
            PropModels.Build(kit, prop, map);
            Assert.True(kit.Solid.VertexCount + kit.Flat.VertexCount > 0, $"{mapName}: the {prop.Type} at {prop.X},{prop.Y} builds nothing");

            var (min, max) = (kit.Solid.VertexCount > 0 ? kit.Solid : kit.Flat).Bounds();
            const float slack = 4 / 32f;
            Assert.True(min.X >= prop.X - slack && max.X <= prop.X + prop.Width + slack, $"{mapName}: the {prop.Type} at {prop.X},{prop.Y} is wider than its tiles");
            Assert.True(max.Y <= PropModels.WallHeight / 32f * IndoorVS + 0.01f, $"{mapName}: the {prop.Type} is taller than the walls");
            if (prop.IsSolid)
                Assert.True(min.Z >= prop.Y - slack && max.Z <= prop.Y + prop.Depth + slack, $"{mapName}: the {prop.Type} at {prop.X},{prop.Y} is deeper than its tiles");
            else if (prop.Type != PropType.Rug)
                // Things on the back wall hang on its face, two tiles in
                Assert.InRange(max.Z, 2f, 2.2f);
        }
    }

    [Fact]
    public void WallsArePaintedInTheRoomsOwnColours()
    {
        var walls = new Dictionary<InteriorStyle, PixelCanvas>();
        foreach (var style in new[] { InteriorStyle.House, InteriorStyle.PokemonCenter, InteriorStyle.PokeMart, InteriorStyle.Lab })
        {
            var c = new PixelCanvas(64, PropModels.WallHeight);
            GroundBaker.PaintWall(c, style);
            walls[style] = c;
            Assert.Equal(64 * PropModels.WallHeight, Count(c, col => col.A == 255));
            // The same strip repeats every 16 texels, so walls of any length line up
            for (int y = 0; y < c.Height; y++) Assert.Equal(c.Get(20, y), c.Get(36, y));
        }
        // Wallpaper and wainscot differ from style to style
        Assert.Equal(4, walls.Values.Select(c => c.Get(5, 20)).Distinct().Count());
        Assert.Equal(4, walls.Values.Select(c => c.Get(5, 60)).Distinct().Count());
        // The Center's wainscot is its red, the Mart's its blue
        Assert.True(walls[InteriorStyle.PokemonCenter].Get(5, 60).R > walls[InteriorStyle.PokemonCenter].Get(5, 60).B + 60);
        Assert.True(walls[InteriorStyle.PokeMart].Get(5, 60).B > walls[InteriorStyle.PokeMart].Get(5, 60).R + 60);
    }

    [Fact]
    public void AHouseFloorIsPlanksAndAShopFloorIsTiles()
    {
        MapDatabase.Initialize();
        var planks = GroundBaker.BakeInterior(MapDatabase.Get("PlayerHouse"));
        var groove = planks.Get(100, 7 + 96);
        // A groove every eight texels, right across the room
        for (int y = 96 + 7; y < planks.Height - 40; y += 8)
            Assert.Equal(groove, planks.Get(100, y));
        Assert.NotEqual(groove, planks.Get(100, 96 + 3));
        Assert.True(Cleanliness(planks).Colours <= 32, $"{Cleanliness(planks).Colours} colours in a plank floor");

        // The floor by the back wall is shaded in two flat steps, never a gradient
        var tiles = GroundBaker.BakeInterior(MapDatabase.Get("PokemonCenter"));
        var shades = Enumerable.Range(64, 14).Select(y => tiles.Get(5 * 32 + 5, y)).Distinct().Count();
        Assert.InRange(shades, 2, 6);
        Assert.True(tiles.Get(5 * 32 + 5, 64 + 2).R < tiles.Get(5 * 32 + 5, 64 + 20).R);

        // The mat at the door
        var mat = planks.Get(4 * 32 + 16, 8 * 32 + 10);
        Assert.Equal(new Color(255, 255, 255, 255), mat);
    }

    // ------------------------------------------------------------------ drawing only what is in view

    [Fact]
    public void TreesAreKeptInChunksOfEightTiles()
    {
        Assert.Equal(MeshBatches.ChunkOf(0, 0), MeshBatches.ChunkOf(7, 7));
        Assert.NotEqual(MeshBatches.ChunkOf(7, 0), MeshBatches.ChunkOf(8, 0));
        Assert.NotEqual(MeshBatches.ChunkOf(0, 7), MeshBatches.ChunkOf(0, 8));
        // The forest margin lies outside the map, at negative tiles: those are chunks too, and none is chunk 0
        var chunks = new HashSet<int>();
        for (int y = -16; y < 80; y += 8)
            for (int x = -16; x < 80; x += 8)
                Assert.True(chunks.Add(MeshBatches.ChunkOf(x, y)) && MeshBatches.ChunkOf(x, y) != 0);

        var rect = new GroundRect(0, 0, 10, 10);
        Assert.True(rect.Touches(new Vector3(9, 0, 9), new Vector3(14, 3, 14)));
        Assert.False(rect.Touches(new Vector3(11, 0, 2), new Vector3(14, 3, 6)));
    }

    [Fact]
    public void ShadowsAreGatheredFromTheSideTheSunIsOn()
    {
        var renderer = new WorldRenderer(new RenderContext(3840, 2160));
        var target = new Vector3(20f, 1.2f, 15f);
        var sun = ArtLook.FieldRigFor(PokemonPlatinumEngine.Core.TimeOfDay.Twilight).Light.SunDirection;
        var (view, casters) = renderer.VisibleRects(target, sun);

        Assert.True(view.MinX < target.X && view.MaxX > target.X && view.MinZ < target.Z && view.MaxZ > target.Z);
        Assert.InRange(view.MaxX - view.MinX, 16f, 30f);
        Assert.True(casters.MinX <= view.MinX && casters.MaxX >= view.MaxX && casters.MinZ <= view.MinZ && casters.MaxZ >= view.MaxZ);
        // The evening sun is low in the south-west: shadows come from the west and the south, a long way
        Assert.True(sun.X < 0 && sun.Z > 0);
        Assert.True(view.MinX - casters.MinX > 5f);
        Assert.True(casters.MaxZ - view.MaxZ > 3f);
        Assert.True(casters.MaxX - view.MaxX < 2f);
    }
}

internal static class TestExtensions
{
    /// <summary>Runs <paramref name="change"/> on a value and returns it, for setting things up inline.</summary>
    public static T Also<T>(this T value, Action<T> change)
    {
        change(value);
        return value;
    }
}
