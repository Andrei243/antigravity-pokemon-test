using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A tuft of grass standing on an open map's ground: where, which of the grasses' tufts it is, how it is turned and its size.</summary>
internal readonly record struct OpenTuft(Vector3 At, int Variant, float Yaw, float Scale);

/// <summary>
/// The grass's tufts (plan 32 · OW9; style guide, "Open world", "Grass"): scattered over a chunk's finest ground where
/// all three corners of a triangle are of the grasses or the highland and it is no steeper than
/// <see cref="Steepest"/>, about <see cref="Density"/> to a square metre, each tuft at a point of the triangle and of a
/// size and turn decided by <see cref="SplitMix.Mix"/> of the chunk and the triangle (the same tufts on every machine,
/// whatever was asked before), and drawn only within <see cref="Reach"/> of the player, shrinking to nothing over its
/// last <see cref="Fade"/> metres. Worked out on the streamer's worker from the chunk alone; GPU-free.
/// </summary>
internal static class OpenGrass
{
    /// <summary>Tufts to a square metre of grass, the steepest ground they stand on (degrees) and how far from the player they are drawn (metres).</summary>
    public const float Density = 2.2f, Steepest = 34f, Reach = 22f, Fade = 5f;

    /// <summary>The tufts' looks: the grass's, the light grass's, the deep grass's and the highland's, by the cover under them.</summary>
    public const int Variants = 4;

    /// <summary>The tufts' meshes: each cover's tuft, and after them each cover's tuft with wild flowers in it (a look of <see cref="Variants"/> or more).</summary>
    public const int Looks = 2 * Variants;

    /// <summary>Flowers grow in drifts: the land is cut into squares of this many metres, about one in three has them, and there about one tuft in five flowers.</summary>
    public const float FlowerPatch = 7f;

    /// <summary>The look a tuft on a cover takes, or −1 where none grows.</summary>
    public static int VariantOf(OpenCover cover) => cover switch
    {
        OpenCover.Grass => 0,
        OpenCover.GrassLight => 1,
        OpenCover.GrassDeep => 2,
        OpenCover.Highland => 3,
        _ => -1
    };

    /// <summary>The tufts of a chunk at its finest level (none at a thinned level: the tufts are drawn near the player alone).</summary>
    public static List<OpenTuft> Of(BakedChunk chunk)
    {
        var tufts = new List<OpenTuft>();
        if (chunk.Level != 0) return tufts;
        float steepest = MathF.Cos(Steepest * MathF.PI / 180f);
        uint seed = SplitMix.Mix(0x6a09e667u, (uint)chunk.ChunkX, (uint)chunk.ChunkZ);
        for (int t = 0; t < chunk.GroundTriangles; t++)
        {
            int ia = chunk.Indices[t * 3], ib = chunk.Indices[t * 3 + 1], ic = chunk.Indices[t * 3 + 2];
            int va = VariantOf(chunk.Covers[ia]), vb = VariantOf(chunk.Covers[ib]), vc = VariantOf(chunk.Covers[ic]);
            if (va < 0 || vb < 0 || vc < 0) continue;
            var a = Pos(chunk, ia); var b = Pos(chunk, ib); var c = Pos(chunk, ic);
            var cross = Vector3.Cross(b - a, c - a);
            float area = cross.Length() * 0.5f;
            if (area < 1e-6f || MathF.Abs(cross.Y) / (2f * area) < steepest) continue;
            uint h = SplitMix.Mix(seed, (uint)t, 0x1u);
            float expected = area * Density;
            int count = (int)expected + ((h & 0xFFFF) / 65536f < expected - (int)expected ? 1 : 0);
            for (int k = 0; k < count; k++)
            {
                uint g = SplitMix.Mix(seed, (uint)t, 0x100u + (uint)k);
                float u = (g & 0x3FF) / 1023f, v = ((g >> 10) & 0x3FF) / 1023f;
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                var at = a + (b - a) * u + (c - a) * v;
                // Never under the water (a bank's grass runs under a river's edge)
                if (UnderWater(chunk, at)) continue;
                // The cover of the corner the tuft stands nearest
                float wa = 1f - u - v;
                int variant = wa >= u && wa >= v ? va : u >= v ? vb : vc;
                float yaw = ((g >> 20) & 0x3FF) / 1023f * MathF.Tau;
                float scale = 0.8f + 0.45f * ((h >> (16 + k % 8)) & 0xFF) / 255f;
                // Wild flowers, in drifts (by the square of the world the tuft stands in, so a drift crosses chunks)
                uint patch = SplitMix.Mix(0x3c6ef372u, (uint)(int)MathF.Floor(at.X / FlowerPatch), (uint)(int)MathF.Floor(at.Z / FlowerPatch));
                if ((patch & 0xFF) < 84 && ((g >> 3) & 0xFF) < 52) variant += Variants;
                tufts.Add(new OpenTuft(at, variant, yaw, scale));
            }
        }
        return tufts;
    }

    /// <summary>How large a tuft is drawn at a distance from the player: whole, shrinking to nothing over the last <see cref="Fade"/> metres of <see cref="Reach"/>.</summary>
    public static float Shown(float distance)
    {
        if (distance >= Reach) return 0f;
        float t = Math.Clamp((Reach - distance) / Fade, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>The tufts' colours, root and tip, by look (style guide, "Open world": the covers' own marks).</summary>
    public static (Color Root, Color Tip) ColoursOf(int variant) => variant switch
    {
        1 => (new Color(92, 164, 78, 255), new Color(146, 216, 112, 255)),
        2 => (new Color(58, 128, 66, 255), new Color(108, 186, 98, 255)),
        3 => (new Color(104, 136, 86, 255), new Color(160, 190, 128, 255)),
        _ => (new Color(78, 152, 72, 255), new Color(132, 208, 104, 255))
    };

    /// <summary>Whether a point stands under the chunk's water, or within a few centimetres of its surface (the bake's 2 m cells).</summary>
    private static bool UnderWater(BakedChunk chunk, Vector3 at)
    {
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre, cell = side / BakedChunk.WaterCells;
        int i = Math.Clamp((int)MathF.Floor((at.X - chunk.ChunkX * side) / cell), 0, BakedChunk.WaterCells - 1);
        int k = Math.Clamp((int)MathF.Floor((at.Z - chunk.ChunkZ * side) / cell), 0, BakedChunk.WaterCells - 1);
        // The cell itself and those beside it: a tuft on the bank's last metre is a tuft in the shallows
        for (int dk = -1; dk <= 1; dk++)
            for (int di = -1; di <= 1; di++)
            {
                int ci = i + di, ck = k + dk;
                if (ci < 0 || ck < 0 || ci >= BakedChunk.WaterCells || ck >= BakedChunk.WaterCells) continue;
                int level = chunk.WaterAt(ci, ck);
                if (level != BakedChunk.NoWater && at.Y < level / (float)OpenPos.PerMetre + 0.08f) return true;
            }
        return false;
    }

    private static Vector3 Pos(BakedChunk c, int v) =>
        new(c.Positions[v * 3] / (float)OpenPos.PerMetre, c.Positions[v * 3 + 1] / (float)OpenPos.PerMetre, c.Positions[v * 3 + 2] / (float)OpenPos.PerMetre);
}
