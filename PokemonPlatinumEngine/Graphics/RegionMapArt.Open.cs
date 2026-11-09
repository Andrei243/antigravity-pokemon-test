using System;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The map of an open region (plan 32 · OW10, <c>RegionMapArt</c>'s open half): a region with no grid map is painted
/// from above out of what its baker baked, a texel a metre, so a chunk of 32 metres is a cell of 32 texels as a grid
/// chunk is. The grid's cell 0,0 lies on the land's north-west chunk (<see cref="OpenCellOf"/>). Each texel is the
/// highest ground facing up over its middle (a cave under a hill is never seen; the hill over it is), coloured by the
/// cover of the triangle's nearest corner in the Town Map's own colours, lit from the north-west by its slope, and the
/// baked water over it where its surface stands higher, deepening from the shore. No GPU call: the same picture every time.
/// </summary>
public static partial class RegionMapArt
{
    /// <summary>The cells of an open region's picture: its land's chunks' bounds (west, north, columns, rows).</summary>
    public static (int X, int Z, int Columns, int Rows) OpenBoundsOf(BakedRegion land)
    {
        int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
        foreach (var (x, z) in land.Chunks)
        {
            minX = Math.Min(minX, x); minZ = Math.Min(minZ, z);
            maxX = Math.Max(maxX, x); maxZ = Math.Max(maxZ, z);
        }
        return minX > maxX ? (0, 0, 0, 0) : (minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
    }

    /// <summary>The cell of an open region's map a place stands in (its chunk from the land's north-west one), and the texel in the picture.</summary>
    public static (int X, int Y) OpenCellOf(BakedRegion land, OpenPos pos)
    {
        var (x0, z0, _, _) = OpenBoundsOf(land);
        var (cx, cz) = OpenMap.ChunkOf(pos);
        return (cx - x0, cz - z0);
    }

    /// <summary>
    /// An open region painted from above: its land's chunks at <paramref name="level"/> (nought, the finest, unless told:
    /// the far levels keep every hole but lose the small slopes the light reads). A texel no ground lies under is clear.
    /// </summary>
    public static PixelCanvas PaintOpen(BakedRegion land, int level = 0) => PaintOpen(land, Array.Empty<OpenBox>(), level);

    /// <summary>
    /// An open region painted from above with the solid boxes of its places over the land, each a roof (lighter along
    /// its north edge) over a wall's row along its south, as the grid's map draws a building.
    /// </summary>
    public static PixelCanvas PaintOpen(BakedRegion land, System.Collections.Generic.IEnumerable<OpenBox> solids, int level = 0)
    {
        const int T = CellTexels;
        var (x0, z0, columns, rows) = OpenBoundsOf(land);
        int w = columns * T, h = rows * T;
        var canvas = new PixelCanvas(Math.Max(1, w), Math.Max(1, h));
        if (w == 0) return canvas;
        var height = new float[w * h];
        var cover = new byte[w * h];
        var water = new float[w * h];
        Array.Fill(height, float.NegativeInfinity);
        Array.Fill(water, float.NegativeInfinity);

        foreach (var (cx, cz) in land.Chunks)
        {
            if (land.Chunk(cx, cz, level) is not { } chunk) continue;
            // The ground's own triangles (an apron is the neighbour's, a skirt hangs under a border), each laid top-down
            for (int t = 0; t < chunk.GroundTriangles; t++)
            {
                int ia = chunk.Indices[3 * t], ib = chunk.Indices[3 * t + 1], ic = chunk.Indices[3 * t + 2];
                var (a, b, c) = (chunk.Vertex(ia), chunk.Vertex(ib), chunk.Vertex(ic));
                double ax = a.X / 1000.0, az = a.Z / 1000.0, bx = b.X / 1000.0, bz = b.Z / 1000.0, ccx = c.X / 1000.0, ccz = c.Z / 1000.0;
                // (a face standing on its edge covers nothing from above; one turned down is passed by below)
                double area = (bx - ax) * (ccz - az) - (bz - az) * (ccx - ax);
                if (Math.Abs(area) < 1e-9) continue;
                int tx0 = (int)Math.Floor(Math.Min(ax, Math.Min(bx, ccx))) - x0 * T, tx1 = (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, ccx))) - x0 * T;
                int tz0 = (int)Math.Floor(Math.Min(az, Math.Min(bz, ccz))) - z0 * T, tz1 = (int)Math.Ceiling(Math.Max(az, Math.Max(bz, ccz))) - z0 * T;
                for (int ty = Math.Max(0, tz0); ty <= Math.Min(h - 1, tz1); ty++)
                    for (int tx = Math.Max(0, tx0); tx <= Math.Min(w - 1, tx1); tx++)
                    {
                        double px = tx + x0 * T + 0.5, pz = ty + z0 * T + 0.5;
                        // Barycentric weights on the ground
                        double wa = ((bx - px) * (ccz - pz) - (bz - pz) * (ccx - px)) / area;
                        double wb = ((ccx - px) * (az - pz) - (ccz - pz) * (ax - px)) / area;
                        double wc = 1 - wa - wb;
                        if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9) continue;
                        float y = (float)((wa * a.Y + wb * b.Y + wc * c.Y) / 1000.0);
                        int i = ty * w + tx;
                        if (y <= height[i]) continue;
                        if (!FacesUp(chunk, ia, ib, ic)) continue;
                        height[i] = y;
                        int nearest = wa >= wb && wa >= wc ? ia : wb >= wc ? ib : ic;
                        cover[i] = (byte)chunk.Covers[nearest];
                    }
            }
            // The water over it, by its 2 m cells
            int cell = OpenMap.ChunkSide / BakedChunk.WaterCells / 1000;
            for (int wz = 0; wz < BakedChunk.WaterCells; wz++)
                for (int wx = 0; wx < BakedChunk.WaterCells; wx++)
                {
                    int surface = chunk.WaterAt(wx, wz);
                    if (surface == BakedChunk.NoWater) continue;
                    for (int dz = 0; dz < cell; dz++)
                        for (int dx = 0; dx < cell; dx++)
                        {
                            int tx = (cx - x0) * T + wx * cell + dx, ty = (cz - z0) * T + wz * cell + dz;
                            if (tx < w && ty < h) water[ty * w + tx] = surface / 1000f;
                        }
                }
        }

        for (int ty = 0; ty < h; ty++)
            for (int tx = 0; tx < w; tx++)
            {
                int i = ty * w + tx;
                if (float.IsNegativeInfinity(height[i])) continue;
                float depth = water[i] - height[i];
                if (depth > 0.05f)
                {
                    bool shore = depth < 0.35f;
                    canvas.SetRaw(tx, ty, shore ? Shore : depth < 1.2f ? SeaShallow : depth < 2.5f ? SeaMid : SeaDeep);
                    continue;
                }
                var colour = OpenCoverColour((OpenCover)cover[i], tx, ty);
                // Lit from the north-west by the slope there: a metre's rise toward the light over a texel brightens
                float nw = tx > 0 && ty > 0 && !float.IsNegativeInfinity(height[i - w - 1]) ? height[i - w - 1] : height[i];
                float rise = height[i] - nw;
                if (rise > 0.12f) colour = PixelCanvas.Light1(colour, Math.Min(0.35f, rise * 0.4f));
                else if (rise < -0.12f) colour = PixelCanvas.Shadow(colour, Math.Min(0.4f, -rise * 0.45f));
                canvas.SetRaw(tx, ty, colour);
            }

        // The places' solids: roofs over a wall's row, as the grid's buildings are drawn
        var roof = new Color(178, 92, 70, 255);
        foreach (var box in solids)
        {
            int bx0 = (int)Math.Floor(box.Min.X / 1000.0) - x0 * T, bx1 = (int)Math.Ceiling(box.Max.X / 1000.0) - 1 - x0 * T;
            int bz0 = (int)Math.Floor(box.Min.Z / 1000.0) - z0 * T, bz1 = (int)Math.Ceiling(box.Max.Z / 1000.0) - 1 - z0 * T;
            for (int ty = Math.Max(0, bz0); ty <= Math.Min(h - 1, bz1); ty++)
                for (int tx = Math.Max(0, bx0); tx <= Math.Min(w - 1, bx1); tx++)
                {
                    bool wall = ty == bz1 && bz1 > bz0;
                    canvas.SetRaw(tx, ty, wall ? WallColor : ty == bz0 ? PixelCanvas.Mix(roof, Color.White, 0.22f) : roof);
                }
        }
        return canvas;
    }

    // A triangle faces up when its normal, from its corners' own normals, points to the sky (decision 14's faces up)
    private static bool FacesUp(BakedChunk chunk, int a, int b, int c) =>
        chunk.Normals[3 * a + 1] + chunk.Normals[3 * b + 1] + chunk.Normals[3 * c + 1] > 3 * 127 * OpenGround.FacesUpY;

    /// <summary>An open cover in the Town Map's colours, with a grain of the texel's own where the grid's map has one.</summary>
    private static Color OpenCoverColour(OpenCover cover, int x, int y)
    {
        bool grain = ((x * 7 + y * 13) & 7) == 0;
        return cover switch
        {
            OpenCover.Grass => grain ? LawnShade : LawnColor,
            OpenCover.GrassLight => LawnColor,
            OpenCover.GrassDeep => grain ? GrassDark : GrassColor,
            OpenCover.Path => PathColor,
            OpenCover.Rock or OpenCover.ClimbableRock => Rock[2],
            OpenCover.RockDark => Rock[0],
            OpenCover.CaveFloor => DirtColor,
            OpenCover.Highland => grain ? Rock[3] : Rock[4],
            OpenCover.Sand => SandColor,
            OpenCover.Snow => grain ? SnowShade : SnowColor,
            OpenCover.Mud => MarshColor,
            _ => BareColor
        };
    }
}
