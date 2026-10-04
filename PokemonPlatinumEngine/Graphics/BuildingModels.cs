using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Where a building's geometry goes besides the art sheet: tiled roofs by colour, and the light it throws at night.</summary>
internal sealed class BuildingTargets
{
    /// <summary>The batch drawn with <see cref="BuildingArt.RoofTiles"/> in a given colour.</summary>
    public required Func<Color, MeshBuilder> RoofTiles { get; init; }

    /// <summary>Light pools from lights that burn all night.</summary>
    public required MeshBuilder PublicLight { get; init; }

    /// <summary>Light pools from the windows of homes.</summary>
    public required MeshBuilder HomeLight { get; init; }
}

/// <summary>
/// The 3D shape of a building: walls carrying the façades painted by <see cref="BuildingArt"/>, a roof (a gable
/// whose ridge runs east–west, a hip roof, or a flat roof behind a parapet), an entrance block, chimney, doorsteps
/// and rooftop equipment. Measured in texels from the north-west corner of the footprint. No GPU calls.
/// </summary>
internal static class BuildingModels
{
    private const int Eave = 8, BackEave = 4, Fascia = 3;

    /// <summary>The slope that tiled roofs aim for; the exact pitch makes the slope a whole number of tile rows.</summary>
    private const float PitchDeg = 33f;

    private static readonly Color Trim = new(244, 240, 230, 255);

    private static string Key(Color c) => $"{c.R}.{c.G}.{c.B}";

    /// <summary>A pitched roof's numbers, in texels across and screen rows up.</summary>
    internal readonly record struct Pitch(float Run, int Rows, float Tan, float RiseRows)
    {
        /// <summary>Length of the slope in texels: a whole number of tile rows.</summary>
        public float Slope => Rows * 8f;

        /// <summary>
        /// The pitch of a roof whose slope covers <paramref name="run"/> texels of ground from ridge to eave: the
        /// number of tile rows that makes it at least as steep as the kit's pitch.
        /// </summary>
        public static Pitch For(float run, float vs, float pitchDeg = PitchDeg)
        {
            int rows = (int)MathF.Ceiling(run / MathF.Cos(pitchDeg * MathF.PI / 180f) / 8f - 0.001f);
            float rise = MathF.Sqrt(rows * 8f * rows * 8f - run * run);
            return new Pitch(run, rows, rise / run, rise / vs);
        }

        /// <summary>How far the roof drops, in screen rows, over <paramref name="texels"/> of ground.</summary>
        public float Drop(float texels, float vs) => texels * Tan / vs;
    }

    /// <param name="baseY">The height of the ground the building stands on.</param>
    public static void Add(KitBuilder kit, BuildingInfo b, BuildingStyle s, BuildingTargets targets, float baseY = 0f)
    {
        kit.Origin = new Vector3(b.X0, baseY, b.Y0);
        string id = $"b{b.X0}_{b.Y0}";
        int x0 = BuildingArt.Inset, x1 = b.Width * BuildingArt.Bay - BuildingArt.Inset;
        // The back wall stands a tile inside the footprint, so the roof doesn't swallow the whole block
        int zB = BuildingArt.Bay, zF = b.Depth * BuildingArt.Bay;

        // A tower or a lighthouse is a ground stage like any flat-roofed building, with its stages standing on that
        var whole = s;
        bool stages = b.Kind is BuildingKind.Tower or BuildingKind.Lighthouse && !b.Annex;
        if (stages) s = s with { Roof = RoofShape.Flat, Storeys = 1, Gear = RoofGear.None, Tiers = 0 };

        int h = s.WallHeight, depth = zF - zB;
        int eave = s.DeepEaves ? Eave * 2 : Eave;

        float half = depth / 2f;
        var pitch = Pitch.For(half + eave, kit.VS, s.Steep ? SteepDeg : PitchDeg);
        float ridgeY = s.Roof == RoofShape.Gable ? h + pitch.Drop(half, kit.VS) : h - pitch.Drop(eave, kit.VS) + pitch.RiseRows;
        int gable = s.Roof == RoofShape.Gable ? (int)MathF.Ceiling(ridgeY - h) : 0;

        // Walls: the front, and the two sides (the north wall is never seen). The ends of a gate house on a road
        // that runs east and west have its doors, each where the road meets it
        var front = kit.Face(id + ".front", x1 - x0, h, c => BuildingArt.PaintFront(c, b, s));
        Art west, east;
        if (b.SideDoors.Count > 0)
        {
            // Along the west wall from its north end; the east wall is seen the other way round
            var along = b.SideDoors.Where(row => row > b.Y0).Select(row => (row - b.Y0 - 1) * BuildingArt.Bay + BuildingArt.Bay / 2).ToList();
            west = kit.Face(id + ".west", depth, h + gable, c => BuildingArt.PaintSide(c, b, s, h, along));
            east = kit.Face(id + ".east", depth, h + gable, c => BuildingArt.PaintSide(c, b, s, h, along.Select(d => depth - d).ToList()));
        }
        else west = east = kit.Face(id + ".side", depth, h + gable, c => BuildingArt.PaintSide(c, b, s, h));
        kit.Box(x0, x1, zB, zF, 0, h, south: front,
            west: new Art(west.X, west.Y + gable, west.Width, h), east: new Art(east.X, east.Y + gable, east.Width, h));
        if (gable > 0)
        {
            float apex = gable - (ridgeY - h);
            kit.Tri(kit.At(x0, h, zB), kit.At(x0, h, zF), kit.At(x0, ridgeY, zB + half), west,
                new(0, gable), new(depth, gable), new(depth / 2f, apex), -Vector3.UnitX);
            kit.Tri(kit.At(x1, h, zF), kit.At(x1, h, zB), kit.At(x1, ridgeY, zB + half), east,
                new(0, gable), new(depth, gable), new(depth / 2f, apex), Vector3.UnitX);
        }

        switch (s.Roof)
        {
            case RoofShape.Gable: GableRoof(kit, s, targets, x0, x1, zB, zF, h, pitch, eave); break;
            case RoofShape.Hip: HipRoof(kit, s, targets, x0, x1, zB, zF, h, pitch, eave); break;
            default: FlatRoof(kit, b, s, x0, x1, zB, zF, h); break;
        }
        if (stages) Stages(kit, b, whole, targets, id, x0, x1, zB, zF, h);
        else if (s.Tiers > 0) Tiers(kit, b, s, id, x0, x1, zB, zF, h);

        var bays = BuildingArt.BaysOf(b);
        byte windows = BuildingArt.WindowLight(b, s);
        var windowPools = windows == ArtSheet.HomeLight ? targets.HomeLight : targets.PublicLight;
        // A porch the world's data builds out takes the place of the entrance block the style would add
        bool porch = b.Porch.Count > 0;
        if (porch) Porch(kit, b, s, targets, id, zF);
        for (int i = 0; i < bays.Length; i++)
        {
            float cx = i * BuildingArt.Bay + BuildingArt.Bay / 2f;
            bool open = b.Doors.Exists(d => d.X == b.X0 + i && d.Target != null);
            if (bays[i] == BuildingArt.BayKind.Door)
            {
                bool portal = s.Portal > 0 && s.Pitched && !porch && !s.SlidingDoor;
                if (portal) Portal(kit, b, s, id, cx, zF);
                float doorZ = zF + (portal ? BuildingArt.PortalDepth : 0);
                float stepHalf = s.SlidingDoor ? 28 : s.GlassDoor ? 21 : portal ? 23 : 14;
                // In front of an entrance block the step is shallow, so it stays behind whoever stands at the door
                kit.Block("step", BuildingArt.StepStone, cx - stepHalf, cx + stepHalf, doorZ, doorZ + (portal ? 4 : 7), 0, 3);
                // The lantern by a house door burns all night; a shop's doors light the street while it is open
                if (open) LightPool(kit, targets.PublicLight, cx, doorZ, s.GlassDoor ? 100 : 64, 84);
            }
            else if (bays[i] == BuildingArt.BayKind.Window && s.Window != WindowKind.None)
            {
                LightPool(kit, windowPools, cx, zF, 70, 66);
            }
        }
    }

    /// <summary>The pitch of the Foreign Building's roof.</summary>
    private const float SteepDeg = 50f;

    // ------------------------------------------------------------------ porches, tiers and towers

    /// <summary>
    /// The entrance the world's data builds out a tile in front of the wall. Closed: one block as wide as its
    /// tiles, with the doors in its front. Open: a block on each side of the way in, up to where the canopy
    /// hangs, and the canopy across them all; the door is in the wall behind.
    /// </summary>
    private static void Porch(KitBuilder kit, BuildingInfo b, BuildingStyle s, BuildingTargets targets, string id, int zF)
    {
        int bay = BuildingArt.Bay, inset = BuildingArt.Inset;
        int ph = BuildingArt.PorchHeight(s), z0 = zF - 4, z1 = zF + bay - inset;
        int Left(int tile) => (tile - b.X0) * bay + (tile == b.X0 ? inset : 0);
        int Right(int tile) => (tile - b.X0 + 1) * bay - (tile == b.X1 ? inset : 0);
        int px0 = Left(b.Porch[0]), px1 = Right(b.Porch[^1]);
        var side = kit.Face($"{id}.porch.side", z1 - z0, ph, c => BuildingArt.PaintPortalSide(c, s));
        var top = kit.Face($"{id}.porch.top", px1 - px0, z1 - z0, c => BuildingArt.PaintPortalTop(c, s));

        if (!b.PorchIsOpen)
        {
            var front = kit.Face(id + ".porch", px1 - px0, ph, c => BuildingArt.PaintPorch(c, b, s, b.Porch[0]));
            kit.Box(px0, px1, z0, z1, 0, ph, top, front, side, side);
            foreach (var (x, target) in b.Doors)
            {
                float cx = (x - b.X0) * bay + bay / 2f;
                kit.Block("step", BuildingArt.StepStone, cx - 21, cx + 21, z1, z1 + 4, 0, 3);
                if (target != null) LightPool(kit, targets.PublicLight, cx, z1, s.GlassDoor ? 100 : 64, 84);
            }
            return;
        }

        // Each run of porch tiles is one side block
        int under = BuildingArt.CanopyUnder(ph);
        for (int i = 0; i < b.Porch.Count; i++)
        {
            int first = b.Porch[i];
            while (i + 1 < b.Porch.Count && b.Porch[i + 1] == b.Porch[i] + 1) i++;
            int fx0 = Left(first), fx1 = Right(b.Porch[i]);
            var flank = kit.Face($"{id}.porch.flank{first}", fx1 - fx0, under, c => BuildingArt.PaintPorchFlank(c, s, first));
            var flankSide = kit.Face($"{id}.porch.flankside", z1 - z0, under, c => BuildingArt.PaintPorchFlank(c, s, 3));
            kit.Box(fx0, fx1, z0, z1, 0, under, south: flank, west: flankSide, east: flankSide);
        }
        var canopy = kit.Face(id + ".canopy", px1 - px0, ph - under, c => BuildingArt.PaintCanopy(c, s, sign: s.Pitched));
        var canopySide = kit.Face(id + ".canopy.side", z1 - z0, ph - under, c => BuildingArt.PaintCanopy(c, s, sign: false));
        kit.Box(px0, px1, z0, z1, under, ph, top, canopy, canopySide, canopySide);
    }

    /// <summary>One stage of a tower, or one tier stepping back on a flat roof: four walls and a top.</summary>
    private static void Stage(KitBuilder kit, BuildingStyle s, string key, float x0, float x1, float z0, float z1, float y0, float y1, Color? band = null)
    {
        int w = (int)MathF.Round(x1 - x0), d = (int)MathF.Round(z1 - z0), h = (int)MathF.Round(y1 - y0);
        var front = kit.Face($"{key}.front", w, h, c =>
        {
            BuildingArt.PaintTier(c, s, w, ArtSheet.PublicLight);
            if (band is { } colour) BuildingArt.PaintTierBand(c, colour);
        });
        var side = kit.Face($"{key}.side", d, h, c =>
        {
            BuildingArt.PaintTier(c, s, d + 3, ArtSheet.PublicLight);
            if (band is { } colour) BuildingArt.PaintTierBand(c, colour);
        });
        var top = kit.Face($"{key}.top", w, d, c => BuildingArt.PaintTierTop(c, s));
        kit.Box(x0, x1, z0, z1, y0, y1, top, front, side, side);
    }

    /// <summary>Storeys stepping back on a flat roof, each a tile narrower each side than what it stands on.</summary>
    private static void Tiers(KitBuilder kit, BuildingInfo b, BuildingStyle s, string id, int x0, int x1, int zB, int zF, int h)
    {
        const int rows = BuildingArt.UpperStorey + BuildingArt.TopBand;
        float tx0 = x0, tx1 = x1, tz0 = zB, tz1 = zF, y = h - 5;
        for (int tier = 0; tier < s.Tiers; tier++)
        {
            tx0 += 28; tx1 -= 28; tz0 += 10; tz1 -= 22;
            if (tx1 - tx0 < 48 || tz1 - tz0 < 20) return;
            Stage(kit, s, $"{id}.tier{tier}", tx0, tx1, tz0, tz1, y, y + rows);
            y += rows;
        }
    }

    /// <summary>
    /// The stages of a tower or a lighthouse on its ground stage, as tall together as the model stands: each a
    /// little narrower than the one below. A tower ends in a pointed cap of slate; a lighthouse in a gallery,
    /// a lantern room whose glass burns all night, and a small red cap.
    /// </summary>
    private static void Stages(KitBuilder kit, BuildingInfo b, BuildingStyle s, BuildingTargets targets, string id, int x0, int x1, int zB, int zF, int h)
    {
        bool lighthouse = b.Kind == BuildingKind.Lighthouse;
        int count = lighthouse ? 2 : 3;
        int total = Math.Clamp((int)MathF.Round(b.Height * GroundBaker.ArtTile / kit.VS), 150, 300);
        const int lantern = 24, capRows = 30;
        int rows = Math.Max(30, (total - h - (lighthouse ? lantern + 6 : 0)) / count);

        // The shaft stands in the middle of the ground stage, no wider than three tiles and a little deeper than half
        float width = MathF.Min(x1 - x0 - 16, 96), depthOf = MathF.Min(zF - zB - 4, 64);
        float cx = (x0 + x1) / 2f, cz = zB + (zF - zB) * 0.45f, y = h - 5;
        for (int i = 0; i < count; i++)
        {
            Stage(kit, s, $"{id}.stage{i}", cx - width / 2, cx + width / 2, cz - depthOf / 2, cz + depthOf / 2, y, y + rows,
                lighthouse && i == 0 ? s.Accent : null);
            y += rows;
            width = MathF.Max(40, width - 12);
            depthOf = MathF.Max(24, depthOf - 8);
        }
        width += 12; depthOf += 8;   // the size of the last stage

        if (lighthouse)
        {
            // The gallery, then the lantern room, set in from it
            Stage(kit, s with { Window = WindowKind.None }, id + ".gallery", cx - width / 2 - 6, cx + width / 2 + 6, cz - depthOf / 2 - 4, cz + depthOf / 2 + 6, y, y + 6);
            y += 6;
            float lw = MathF.Max(24, width - 12), ld = MathF.Max(16, depthOf - 10);
            var glass = kit.Face($"{id}.lantern.{(int)lw}", (int)lw, lantern, BuildingArt.PaintLantern);
            var glassSide = kit.Face($"{id}.lantern.{(int)ld}", (int)ld, lantern, BuildingArt.PaintLantern);
            kit.Box(cx - lw / 2, cx + lw / 2, cz - ld / 2, cz + ld / 2, y, y + lantern, south: glass, west: glassSide, east: glassSide);
            y += lantern;
            width = lw + 8; depthOf = ld + 8;
        }

        // The cap: four slopes to a point, in the roof's own tiles
        var tiles = targets.RoofTiles(s.RoofColor);
        float capHeight = lighthouse ? capRows * 0.6f : capRows;
        float ex0 = cx - width / 2 - 3, ex1 = cx + width / 2 + 3, ez0 = cz - depthOf / 2 - 3, ez1 = cz + depthOf / 2 + 3;
        var apex = kit.At(cx, y + capHeight, cz);
        var white = Color.White;
        void Slope(Vector3 a, Vector3 c, Vector3 normal)
        {
            float span = Vector3.Distance(a, c) * GroundBaker.ArtTile / 64f / 2f;
            tiles.Tri(a, c, apex, new(-span, 0.6f), new(span, 0.6f), new(0, 0), normal, normal, normal, white, white, white);
        }
        Slope(kit.At(ex0, y, ez1), kit.At(ex1, y, ez1), Vector3.Normalize(new Vector3(0, 0.6f, 1)));
        Slope(kit.At(ex1, y, ez0), kit.At(ex0, y, ez0), Vector3.Normalize(new Vector3(0, 0.6f, -1)));
        Slope(kit.At(ex0, y, ez0), kit.At(ex0, y, ez1), Vector3.Normalize(new Vector3(-1, 0.6f, 0)));
        Slope(kit.At(ex1, y, ez1), kit.At(ex1, y, ez0), Vector3.Normalize(new Vector3(1, 0.6f, 0)));
    }

    // ------------------------------------------------------------------ roofs

    /// <summary>A gable roof with its ridge running east–west: the south slope faces the camera, tile rows level.</summary>
    private static void GableRoof(KitBuilder kit, BuildingStyle s, BuildingTargets targets, int x0, int x1, int zB, int zF, int h, Pitch p, int Eave)
    {
        var tiles = targets.RoofTiles(s.RoofColor);
        float vs = kit.VS, half = (zF - zB) / 2f, zMid = zB + half;
        float rx0 = x0 - Eave, rx1 = x1 + Eave, cx = (x0 + x1) / 2f;
        float ridgeY = h + p.Drop(half, vs), eaveY = h - p.Drop(Eave, vs), backY = h - p.Drop(BackEave, vs);
        float u0 = (rx0 - cx) / 64f, u1 = (rx1 - cx) / 64f;
        float rise = p.Tan * p.Run;
        var southN = Vector3.Normalize(new Vector3(0, p.Run, rise));
        var northN = Vector3.Normalize(new Vector3(0, p.Run, -rise));

        tiles.Quad(kit.At(rx0, eaveY, zF + Eave), kit.At(rx1, eaveY, zF + Eave), kit.At(rx1, ridgeY, zMid), kit.At(rx0, ridgeY, zMid),
            new(u0, p.Slope / 64f), new(u1, p.Slope / 64f), new(u1, 0), new(u0, 0), Color.White, southN);
        float backSlope = p.Slope * (half + BackEave) / p.Run;
        tiles.Quad(kit.At(rx1, backY, zB - BackEave), kit.At(rx0, backY, zB - BackEave), kit.At(rx0, ridgeY, zMid), kit.At(rx1, ridgeY, zMid),
            new(-u1, backSlope / 64f), new(-u0, backSlope / 64f), new(-u0, 0), new(-u1, 0), Color.White, northN);

        int width = (int)(rx1 - rx0);
        RidgeCap(kit, s, rx0, rx1, zMid, ridgeY);

        // The board along the front eave, and the bargeboards that follow the slopes at both gable ends
        var fascia = kit.Face($"fascia.{Key(Trim)}.{width}", width, Fascia, c => BuildingArt.PaintFascia(c, Trim));
        kit.Box(rx0, rx1, zF + Eave - 1, zF + Eave, eaveY - Fascia, eaveY, south: fascia);

        int len = (int)MathF.Round(p.Slope);
        var verge = kit.Face($"fascia.{Key(Trim)}.{len}", len, Fascia, c => BuildingArt.PaintFascia(c, Trim));
        foreach (var (x, normal) in new[] { (rx0, -Vector3.UnitX), (rx1, Vector3.UnitX) })
        {
            var ridgeLow = kit.At(x, ridgeY - Fascia, zMid);
            var ridgeTop = kit.At(x, ridgeY, zMid);
            var southLow = kit.At(x, eaveY - Fascia, zF + Eave);
            var southTop = kit.At(x, eaveY, zF + Eave);
            var northLow = kit.At(x, backY - Fascia, zB - BackEave);
            var northTop = kit.At(x, backY, zB - BackEave);
            if (normal.X < 0)
            {
                // Seen from the west, south is to the right
                kit.Quad(ridgeLow, southLow, southTop, ridgeTop, verge, normal);
                kit.Quad(northLow, ridgeLow, ridgeTop, northTop, verge, normal);
            }
            else
            {
                kit.Quad(southLow, ridgeLow, ridgeTop, southTop, verge, normal);
                kit.Quad(ridgeLow, northLow, northTop, ridgeTop, verge, normal);
            }
        }

        if (s.Chimney) Chimney(kit, x0 + 20, zMid + 5, h, ridgeY);
        if (s.Gear.HasFlag(RoofGear.Spire))
            kit.Sprite(kit.Face("spire", 16, 44, BuildingArt.PaintSpire), cx, zMid + 1, ridgeY - 3);
        if (s.Gear.HasFlag(RoofGear.Solar)) SolarPanel(kit, cx, eaveY, zF + Eave, ridgeY, zMid, southN);
    }

    /// <summary>A solar panel lying on a roof's south slope, a third of the way up it.</summary>
    private static void SolarPanel(KitBuilder kit, float cx, float eaveY, float eaveZ, float ridgeY, float ridgeZ, Vector3 normal)
    {
        var art = kit.Face("solar", 38, 23, BuildingArt.PaintSolar);
        Vector3 On(float x, float t) => kit.At(x, eaveY + (ridgeY - eaveY) * t + 0.8f, eaveZ + (ridgeZ - eaveZ) * t);
        kit.Quad(On(cx - 19, 0.28f), On(cx + 19, 0.28f), On(cx + 19, 0.78f), On(cx - 19, 0.78f), art, normal);
    }

    /// <summary>A hip roof: four slopes of the same pitch meeting at a short ridge.</summary>
    private static void HipRoof(KitBuilder kit, BuildingStyle s, BuildingTargets targets, int x0, int x1, int zB, int zF, int h, Pitch p, int Eave)
    {
        var tiles = targets.RoofTiles(s.RoofColor);
        float vs = kit.VS, zMid = (zB + zF) / 2f, cx = (x0 + x1) / 2f;
        float rx0 = x0 - Eave, rx1 = x1 + Eave, rz0 = zB - Eave, rz1 = zF + Eave;
        float eaveY = h - p.Drop(Eave, vs), ridgeY = eaveY + p.RiseRows;
        float run = MathF.Min(p.Run, (rx1 - rx0) / 2f);
        float ex0 = rx0 + run, ex1 = rx1 - run;
        float rise = p.Tan * p.Run, v = p.Slope / 64f;

        Vector2 South(float x, float along) => new((x - cx) / 64f, along);
        tiles.Quad(kit.At(rx0, eaveY, rz1), kit.At(rx1, eaveY, rz1), kit.At(ex1, ridgeY, zMid), kit.At(ex0, ridgeY, zMid),
            South(rx0, v), South(rx1, v), South(ex1, 0), South(ex0, 0), Color.White, Vector3.Normalize(new Vector3(0, p.Run, rise)));
        tiles.Quad(kit.At(rx1, eaveY, rz0), kit.At(rx0, eaveY, rz0), kit.At(ex0, ridgeY, zMid), kit.At(ex1, ridgeY, zMid),
            South(-rx1, v), South(-rx0, v), South(-ex0, 0), South(-ex1, 0), Color.White, Vector3.Normalize(new Vector3(0, p.Run, -rise)));

        var westN = Vector3.Normalize(new Vector3(-rise, p.Run, 0));
        var eastN = Vector3.Normalize(new Vector3(rise, p.Run, 0));
        float side = p.Run / 64f;
        tiles.Tri(kit.At(rx0, eaveY, rz0), kit.At(rx0, eaveY, rz1), kit.At(ex0, ridgeY, zMid),
            new(-side, v), new(side, v), new(0, 0), westN, westN, westN, Color.White, Color.White, Color.White);
        tiles.Tri(kit.At(rx1, eaveY, rz1), kit.At(rx1, eaveY, rz0), kit.At(ex1, ridgeY, zMid),
            new(-side, v), new(side, v), new(0, 0), eastN, eastN, eastN, Color.White, Color.White, Color.White);

        if (ex1 - ex0 >= 4) RidgeCap(kit, s, ex0, ex1, zMid, ridgeY);

        // Cap tiles down the four hips, lying just above the slopes
        int hipLength = (int)MathF.Round(MathF.Sqrt(2f * run * run + rise * rise));
        var hip = kit.Face($"ridge.{Key(s.RoofColor)}.{hipLength}x4", hipLength, 4, c => BuildingArt.PaintRidge(c, s.RoofColor));
        foreach (var (cornerX, cornerZ, endX) in new[] { (rx0, rz1, ex0), (rx1, rz1, ex1), (rx0, rz0, ex0), (rx1, rz0, ex1) })
        {
            // Two texels either side of the hip line, measured across it on the ground
            float dx = cornerX < endX ? 1.4f : -1.4f, dz = cornerZ > zMid ? 1.4f : -1.4f;
            kit.Quad(kit.At(cornerX + dx, eaveY + 0.6f, cornerZ), kit.At(endX + dx, ridgeY + 0.6f, zMid - dz * 0.01f),
                kit.At(endX - dx, ridgeY + 0.6f, zMid + dz * 0.01f), kit.At(cornerX, eaveY + 0.6f, cornerZ - dz), hip, Vector3.UnitY);
        }

        var dark = PixelCanvas.Shadow(s.RoofColor, 0.3f);
        int width = (int)(rx1 - rx0), depth = (int)(rz1 - rz0);
        var front = kit.Face($"fascia.{Key(dark)}.{width}", width, Fascia, c => BuildingArt.PaintFascia(c, dark));
        var flank = kit.Face($"fascia.{Key(dark)}.{depth}", depth, Fascia, c => BuildingArt.PaintFascia(c, dark));
        kit.Box(rx0, rx1, rz0, rz1, eaveY - Fascia, eaveY, south: front, west: flank, east: flank);
        if (s.Gear.HasFlag(RoofGear.Solar) && ex1 - ex0 >= 40)
            SolarPanel(kit, cx, eaveY, rz1, ridgeY, zMid, Vector3.Normalize(new Vector3(0, p.Run, rise)));
    }

    private static void RidgeCap(KitBuilder kit, BuildingStyle s, float x0, float x1, float z, float y)
    {
        int width = Math.Max(1, (int)MathF.Round(x1 - x0));
        var face = kit.Face($"ridge.{Key(s.RoofColor)}.{width}x3", width, 3, c => BuildingArt.PaintRidge(c, s.RoofColor));
        var top = kit.Face($"ridge.{Key(s.RoofColor)}.{width}x4", width, 4, c => BuildingArt.PaintRidge(c, s.RoofColor));
        kit.Box(x0, x1, z - 2, z + 2, y - 1, y + 2, top: top, south: face);
    }

    /// <summary>A flat roof five rows below the top of the walls, which stand up round it as a parapet, with its equipment.</summary>
    private static void FlatRoof(KitBuilder kit, BuildingInfo b, BuildingStyle s, int x0, int x1, int zB, int zF, int h)
    {
        const int wall = 3;
        int roofY = h - 5, w = x1 - x0, d = zF - zB;
        bool snow = s.RoofColor.Equals(BuildingArt.SnowRoof);
        var surface = kit.Face($"flatroof{(snow ? ".snow" : "")}.{w - 2 * wall}x{d - 2 * wall}", w - 2 * wall, d - 2 * wall, c => BuildingArt.PaintFlatRoof(c, snow));
        kit.Box(x0 + wall, x1 - wall, zB + wall, zF - wall, roofY - 1, roofY, top: surface);

        Art Cap(int cw, int cd) => kit.Face($"parapet.cap.{cw}x{cd}", cw, cd, c => BuildingArt.PaintParapet(c, cap: true));
        Art Inner(int cw) => kit.Face($"parapet.inner.{cw}", cw, 5, c => BuildingArt.PaintParapet(c, cap: false));
        kit.Box(x0, x1, zF - wall, zF, roofY, h, top: Cap(w, wall));
        kit.Box(x0, x1, zB, zB + wall, roofY, h, top: Cap(w, wall), south: Inner(w));
        kit.Box(x0, x0 + wall, zB + wall, zF - wall, roofY, h, top: Cap(wall, d - 2 * wall), east: Inner(d - 2 * wall));
        kit.Box(x1 - wall, x1, zB + wall, zF - wall, roofY, h, top: Cap(wall, d - 2 * wall), west: Inner(d - 2 * wall));

        // Equipment stands toward the back, where the camera sees the roof best
        float back = zB + wall + 6;
        if (s.Gear.HasFlag(RoofGear.Vents))
        {
            var top = kit.Face("vent.top", 20, 14, c => BuildingArt.PaintVent(c, top: true));
            var face = kit.Face("vent.front", 20, 9, c => BuildingArt.PaintVent(c, top: false));
            var flank = kit.Face("vent.side", 14, 9, c => BuildingArt.PaintVent(c, top: false));
            for (float vx = x0 + 14; vx + 20 < x1 - 40; vx += 62)
                kit.Box(vx, vx + 20, back, back + 14, roofY, roofY + 9, top, face, flank, flank);
        }
        if (s.Gear.HasFlag(RoofGear.Skylight))
        {
            var glass = kit.Face("skylight", 42, 18, BuildingArt.PaintSkylight);
            kit.Box(x1 - 62, x1 - 20, zF - wall - 30, zF - wall - 12, roofY, roofY + 2, top: glass);
        }
        if (s.Gear.HasFlag(RoofGear.Mast))
            kit.Sprite(kit.Face("mast", 16, 64, BuildingArt.PaintMast), x1 - 30, back + 8, roofY);
        if (s.Gear.HasFlag(RoofGear.Dish))
            kit.Sprite(kit.Face("dish", 22, 22, BuildingArt.PaintDish), x1 - 64, back + 16, roofY);
        if (s.Gear.HasFlag(RoofGear.Globe))
            kit.Sprite(kit.Face("globe", 30, 36, BuildingArt.PaintGlobe), (x0 + x1) / 2f, back + 12, roofY);
        if (s.Gear.HasFlag(RoofGear.Stack))
        {
            // Two smokestacks toward the back, east of the middle
            var stack = kit.Face("stack", 16, 84, BuildingArt.PaintStack);
            var rim = kit.Face("stack.top", 16, 16, c => BuildingArt.PaintChimney(c, top: true));
            foreach (float sx in new[] { x1 - 46f, x1 - 78f })
                if (sx > x0 + 20) kit.Box(sx, sx + 16, back, back + 16, roofY, roofY + 84, rim, stack, stack, stack);
        }
        if (s.Gear.HasFlag(RoofGear.Spikes))
        {
            var spike = kit.Face("spike", 12, 30, BuildingArt.PaintSpike);
            foreach (float sx in new[] { x0 + 10f, x1 - 10f })
            {
                kit.Sprite(spike, sx, zB + wall + 4, roofY);
                kit.Sprite(spike, sx, zF - wall - 3, roofY);
            }
        }
    }

    // ------------------------------------------------------------------ parts

    /// <summary>The entrance block: taller than the eave, standing proud of the wall, with the sign over the door.</summary>
    private static void Portal(KitBuilder kit, BuildingInfo b, BuildingStyle s, string id, float cx, int zF)
    {
        int w = s.Portal, d = BuildingArt.PortalDepth + 4, h = BuildingArt.PortalHeight;
        var front = kit.Face(id + ".portal", w, h, c => BuildingArt.PaintPortal(c, b, s));
        var side = kit.Face(id + ".portal.side", d, h, c => BuildingArt.PaintPortalSide(c, s));
        var top = kit.Face(id + ".portal.top", w, d, c => BuildingArt.PaintPortalTop(c, s));
        kit.Box(cx - w / 2f, cx + w / 2f, zF - 4, zF + BuildingArt.PortalDepth, 0, h, top, front, side, side);
    }

    /// <summary>A stone chimney, 12 texels square, rising from the south slope to above the ridge.</summary>
    private static void Chimney(KitBuilder kit, float x, float z, int wallTop, float ridgeY)
    {
        int height = (int)MathF.Round(ridgeY + 9 - wallTop);
        var side = kit.Face($"chimney.{height}", 12, height, c => BuildingArt.PaintChimney(c, top: false));
        var top = kit.Face("chimney.top", 12, 12, c => BuildingArt.PaintChimney(c, top: true));
        kit.Box(x, x + 12, z, z + 12, wallTop, wallTop + height, top, side, side, side);
    }

    /// <summary>
    /// Light spilling onto the ground in front of a wall after dark: <paramref name="width"/> texels wide at the
    /// wall, <paramref name="depth"/> deep, brightest at the wall.
    /// </summary>
    public static void LightPool(KitBuilder kit, MeshBuilder pools, float cx, float wallZ, float width, float depth)
    {
        const float lift = 0.03f;
        var level = new Color(200, 200, 200, 255);
        Vector3 P(float x, float z) => kit.Origin + new Vector3(x * KitBuilder.Texel, lift, z * KitBuilder.Texel);
        pools.Quad(P(cx - width / 2, wallZ + depth), P(cx + width / 2, wallZ + depth), P(cx + width / 2, wallZ), P(cx - width / 2, wallZ),
            new(0, 1), new(1, 1), new(1, 0), new(0, 0), level, Vector3.UnitY);
    }
}
