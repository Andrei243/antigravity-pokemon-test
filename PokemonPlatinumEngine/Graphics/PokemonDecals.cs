using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Eyes and markings laid onto a Pokémon's surface (plan 04 · G7, style guide "Pokémon"). Each decal takes the
/// triangles under its square, projected along its normal, and gives them texture coordinates into one cell of an
/// atlas; the atlas is painted once per eye state (open, shut, squeezed, fierce) with smooth anti-aliased shapes,
/// cut out where there is nothing so the surface shows through. Projection and painting run without GPU calls.
/// </summary>
internal static class PokemonDecals
{
    /// <summary>Pixels per decal in the atlas.</summary>
    public const int Cell = 96;

    /// <summary>Atlas cells in a row.</summary>
    public const int Columns = 4;

    private static readonly Color Ink = new(30, 24, 40, 255);
    private static readonly Color DarkEye = new(28, 24, 40, 255);

    /// <summary>The decal's own axes on the surface: right and up as seen looking at it.</summary>
    internal static void Frame(PokeDecal d, out Vector3 right, out Vector3 up)
    {
        var n = d.Normal;
        right = Vector3.Cross(Vector3.UnitY, n);
        right = right.LengthSquared() < 1e-4f ? Vector3.UnitX : Vector3.Normalize(right);
        up = Vector3.Cross(n, right);
        if (d.Roll != 0f)
        {
            float c = MathF.Cos(d.Roll), s = MathF.Sin(d.Roll);
            (right, up) = (right * c + up * s, up * c - right * s);
        }
    }

    /// <summary>Builds <see cref="PokeModel.DecalPatch"/>: the triangles under every decal, lifted a hair off the surface.</summary>
    public static void Project(PokeModel m)
    {
        m.DecalPatch = null;
        m.DecalUVs = null;
        if (m.Decals.Count == 0) return;
        var mesh = m.Mesh;
        int count = m.Decals.Count;
        int cols = Math.Min(count, Columns), rows = (count + cols - 1) / cols;
        m.AtlasColumns = cols;
        m.AtlasRows = rows;
        float lift = m.Height * 0.0025f;

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var materials = new List<byte>();
        var boneIndices = new List<byte>();
        var boneWeights = new List<float>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();

        for (int d = 0; d < count; d++)
        {
            var decal = m.Decals[d];
            Frame(decal, out var right, out var up);
            var n = decal.Normal;
            float depth = Math.Max(decal.Half.X, decal.Half.Y) * 1.5f;
            int col = d % cols, row = d / cols;

            // Where each vertex lands in the decal's square (NaN: not under it)
            var local = new Dictionary<int, Vector2>();
            bool Under(int v, out Vector2 at)
            {
                if (local.TryGetValue(v, out at)) return !float.IsNaN(at.X);
                var p = mesh.Positions[v] - decal.Center;
                float x = Vector3.Dot(p, right) / decal.Half.X, y = Vector3.Dot(p, up) / decal.Half.Y;
                bool inside = MathF.Abs(x) <= 1f && MathF.Abs(y) <= 1f && MathF.Abs(Vector3.Dot(p, n)) <= depth
                    && Vector3.Dot(mesh.Normals[v], n) > 0.1f && mesh.WeightOf(v, decal.Bone) >= 0.3f;
                at = inside ? new Vector2(x, y) : new Vector2(float.NaN);
                local[v] = at;
                return inside;
            }

            var map = new Dictionary<int, int>();
            int Map(int v, Vector2 at)
            {
                if (map.TryGetValue(v, out int i)) return i;
                i = positions.Count;
                map[v] = i;
                positions.Add(mesh.Positions[v] + mesh.Normals[v] * lift);
                normals.Add(mesh.Normals[v]);
                materials.Add(mesh.Materials[v]);
                for (int k = 0; k < 4; k++)
                {
                    boneIndices.Add(mesh.BoneIndices[v * 4 + k]);
                    boneWeights.Add(mesh.BoneWeights[v * 4 + k]);
                }
                uvs.Add(new Vector2((col + 0.5f + at.X * 0.5f) / cols, (row + 0.5f - at.Y * 0.5f) / rows));
                return i;
            }

            var under = new Vector3();
            float weight = 0f;
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
                if (!Under(a, out var ua) || !Under(b, out var ub) || !Under(c, out var uc)) continue;
                indices.Add(Map(a, ua));
                indices.Add(Map(b, ub));
                indices.Add(Map(c, uc));
            }

            // The colour of the surface round the middle of the decal (eyelids are painted in it)
            foreach (var (v, at) in local)
            {
                if (float.IsNaN(at.X) || at.Length() > 0.6f) continue;
                var col4 = mesh.Colors[v];
                float w = 1f - at.Length() / 0.6f;
                under += new Vector3(col4.R, col4.G, col4.B) * w;
                weight += w;
            }
            if (weight > 0f)
            {
                under /= weight;
                decal.Under = new Color((int)under.X, (int)under.Y, (int)under.Z, 255);
            }
        }

        int count2 = positions.Count;
        var patch = new SdfMesh
        {
            Positions = positions.ToArray(), Normals = normals.ToArray(), Colors = new Color[count2], Materials = materials.ToArray(),
            BoneIndices = boneIndices.ToArray(), BoneWeights = boneWeights.ToArray(), Indices = indices.ToArray()
        };
        Array.Fill(patch.Colors, Color.White);
        m.DecalPatch = patch;
        m.DecalUVs = uvs.ToArray();
    }

    // ------------------------------------------------------------------ painting

    /// <summary>The decal atlas with the eyes in <paramref name="state"/>.</summary>
    public static PixelCanvas Paint(PokeModel m, EyeState state)
    {
        int cols = m.AtlasColumns, rows = m.AtlasRows;
        var canvas = new PixelCanvas(cols * Cell, rows * Cell);
        for (int d = 0; d < m.Decals.Count; d++)
        {
            var decal = m.Decals[d];
            var center = new Vector2((d % cols + 0.5f) * Cell, (d / cols + 0.5f) * Cell);
            // Pixels per model unit across and down the cell (a decal's square can be taller than it is wide)
            var k = new Vector2(Cell * 0.5f / decal.Half.X, Cell * 0.5f / decal.Half.Y);
            if (decal.IsEye) PaintEye(canvas, center, k, decal, state);
            else PaintMark(canvas, center, k, decal);
        }
        return canvas;
    }

    /// <summary>
    /// One eye. <paramref name="k"/> converts model units to pixels; offsets are in units of the eye's size, x to
    /// the viewer's right and y up.
    /// </summary>
    private static void PaintEye(PixelCanvas c, Vector2 center, Vector2 k, PokeDecal d, EyeState state)
    {
        float size = d.Size;
        // Inward, toward the middle of the face: +x for the eye on the left as you look at it
        float inward = d.Center.X < 0f ? 1f : (d.Center.X > 0f ? -1f : 0f);
        Vector2 P(float x, float y) => center + new Vector2(x * size * k.X, -y * size * k.Y);
        Vector2 R(float rx, float ry) => new(rx * size * k.X, ry * size * k.Y);
        float line = Math.Max(2f, 0.17f * size * k.Y);
        if (d.Closed && state != EyeState.Squeeze) state = EyeState.Shut;
        if (d.Glare && state == EyeState.Open) state = EyeState.Fierce;

        switch (state)
        {
            case EyeState.Shut:
                // A lid closed in a gentle curve
                AaPaint.Arc(c, P(0, 0.12f), R(0.78f, 0.5f).X, R(0.78f, 0.5f).Y, MathF.PI * 0.12f, MathF.PI * 0.88f, line, Ink);
                return;
            case EyeState.Squeeze:
            {
                // Screwed shut: a chevron pointing in toward the nose
                float x0 = -0.62f * inward, x1 = 0.48f * inward;
                if (inward == 0f) { x0 = -0.6f; x1 = 0.6f; }
                AaPaint.Line(c, P(x0, 0.5f), P(x1, 0f), line, Ink);
                AaPaint.Line(c, P(x1, 0f), P(x0, -0.5f), line, Ink);
                return;
            }
        }

        if (d.Sclera)
        {
            // A white eye ringed in ink with a dark pupil looking a little inward
            var white = R(0.78f, 1f);
            AaPaint.Ellipse(c, P(0, 0), white.X + line * 0.6f, white.Y + line * 0.6f, Ink);
            AaPaint.Ellipse(c, P(0, 0), white.X, white.Y, d.White ?? new Color(250, 250, 252, 255));
            var pupilAt = new Vector2(0.14f * inward, -0.08f);
            var pr = R(0.44f, 0.6f);
            AaPaint.Ellipse(c, P(pupilAt.X, pupilAt.Y), pr.X, pr.Y, d.Pupil ?? DarkEye);
            Glints(c, P(pupilAt.X - 0.17f, pupilAt.Y + 0.26f), P(pupilAt.X + 0.16f, pupilAt.Y - 0.28f), R(0.19f, 0.19f), R(0.09f, 0.09f));
        }
        else
        {
            // A dark oval, its lower part the iris (lighter toward the bottom), two white glints
            var dark = d.Pupil ?? DarkEye;
            var oval = R(0.7f, 1f);
            AaPaint.Ellipse(c, P(0, 0), oval.X, oval.Y, dark);
            if (d.Iris is Color iris)
            {
                var light = PixelCanvas.Light1(iris, 0.35f);
                var at = P(0, -0.3f);
                var ir = R(0.5f, 0.56f);
                for (int y = (int)(at.Y - ir.Y) - 1; y <= (int)(at.Y + ir.Y) + 1; y++)
                {
                    float t = Math.Clamp((y - (at.Y - ir.Y * 0.3f)) / (ir.Y * 1.3f), 0f, 1f);
                    AaPaint.EllipseRow(c, at, ir.X, ir.Y, y, PixelCanvas.Mix(iris, light, t * 0.85f));
                }
                // Keep the oval's dark rim below the iris
                AaPaint.Arc(c, P(0, 0), oval.X - line * 0.25f, oval.Y - line * 0.25f, MathF.PI * 0.15f, MathF.PI * 0.85f, line * 0.55f, dark);
            }
            Glints(c, P(-0.24f, 0.38f), P(0.22f, -0.32f), R(0.25f, 0.25f), R(0.1f, 0.1f));
        }

        if (state == EyeState.Fierce)
        {
            // A lid pressed down toward the inner corner, in the colour of the skin around the eye
            float io = inward == 0f ? 1f : inward;
            var outerTop = P(-1.15f * io, 1.25f);
            var innerTop = P(1.15f * io, 1.25f);
            var inner = P(1.15f * io, 0.2f);
            var outer = P(-1.15f * io, 0.78f);
            AaPaint.Polygon(c, new[] { outerTop, innerTop, inner, outer }, d.Under);
            AaPaint.Line(c, P(-0.95f * io, 0.74f), P(0.95f * io, 0.24f), line * 1.2f, Ink);
        }
    }

    private static void Glints(PixelCanvas c, Vector2 big, Vector2 small, Vector2 bigR, Vector2 smallR)
    {
        AaPaint.Ellipse(c, big, bigR.X, bigR.Y, Color.White);
        AaPaint.Ellipse(c, small, smallR.X, smallR.Y, Color.White);
    }

    private static void PaintMark(PixelCanvas c, Vector2 center, Vector2 k, PokeDecal d)
    {
        // A marking fills the middle half of its square
        float rx = d.Half.X * 0.5f * k.X, ry = d.Half.Y * 0.5f * k.Y;
        switch (d.Shape)
        {
            case MarkShape.Disc:
                AaPaint.Ellipse(c, center, rx, ry, d.Color);
                break;
            case MarkShape.Ring:
            {
                float w = Math.Max(2f, Math.Min(rx, ry) * 0.3f);
                AaPaint.Arc(c, center, rx - w * 0.5f, ry - w * 0.5f, 0f, MathF.Tau, w, d.Color);
                break;
            }
            case MarkShape.Star:
                // Four points: a tall thin diamond and a wide one
                AaPaint.Polygon(c, new[] { center + new Vector2(0, -ry), center + new Vector2(rx * 0.3f, 0), center + new Vector2(0, ry), center + new Vector2(-rx * 0.3f, 0) }, d.Color);
                AaPaint.Polygon(c, new[] { center + new Vector2(-rx, 0), center + new Vector2(0, -ry * 0.3f), center + new Vector2(rx, 0), center + new Vector2(0, ry * 0.3f) }, d.Color);
                break;
            case MarkShape.Bar:
                AaPaint.Line(c, center - new Vector2(rx - ry, 0), center + new Vector2(rx - ry, 0), ry * 2f, d.Color);
                break;
            case MarkShape.Star5:
            {
                // Five points, the first straight up: a triangle out from each side of a pentagon (the painter fills
                // convex shapes only); the triangles reach a little into the pentagon so no seam shows between them
                var outer = new Vector2[5];
                var inner = new Vector2[5];
                var under = new Vector2[5];
                for (int i = 0; i < 5; i++)
                {
                    float a = -MathF.PI / 2f + i * MathF.Tau / 5f, between = a + MathF.PI / 5f;
                    outer[i] = center + new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
                    var toInner = new Vector2(MathF.Cos(between) * rx, MathF.Sin(between) * ry) * 0.42f;
                    inner[i] = center + toInner;
                    under[i] = center + toInner * 0.8f;
                }
                AaPaint.Polygon(c, inner, d.Color);
                for (int i = 0; i < 5; i++) AaPaint.Polygon(c, new[] { under[(i + 4) % 5], outer[i], under[i] }, d.Color);
                break;
            }
            case MarkShape.Diamond:
                // Four points, upright, like the facets of ice
                AaPaint.Polygon(c, new[] { center + new Vector2(0, -ry), center + new Vector2(rx, 0), center + new Vector2(0, ry), center + new Vector2(-rx, 0) }, d.Color);
                break;
            case MarkShape.Triangle:
                // Pointing down from a flat top, like an arrowhead of feathers; the roll turns it
                AaPaint.Polygon(c, new[] { center + new Vector2(-rx, -ry), center + new Vector2(rx, -ry), center + new Vector2(0, ry) }, d.Color);
                break;
            case MarkShape.Zigzag:
            {
                // A W across the square: down, up, down, up in straight strokes, the joints rounded by the pen
                float w = Math.Max(2f, ry * 0.4f);
                float x0 = -rx + w * 0.5f, x1 = rx - w * 0.5f, top = -(ry - w * 0.5f), low = ry - w * 0.5f;
                var corners = new Vector2[5];
                for (int i = 0; i < 5; i++) corners[i] = center + new Vector2(x0 + (x1 - x0) * i / 4f, i % 2 == 0 ? top : low);
                for (int i = 0; i < 4; i++) AaPaint.Line(c, corners[i], corners[i + 1], w, d.Color);
                break;
            }
            case MarkShape.Wave:
            {
                // A wavy line across the square, a wave and a half long
                float w = Math.Max(2f, ry * 0.4f);
                var prev = center + new Vector2(-rx + w * 0.5f, 0);
                for (int i = 1; i <= 24; i++)
                {
                    float t = i / 24f;
                    var p = center + new Vector2(-rx + w * 0.5f + (2f * rx - w) * t, -MathF.Sin(t * MathF.PI * 3f) * (ry - w * 0.5f));
                    AaPaint.Line(c, prev, p, w, d.Color);
                    prev = p;
                }
                break;
            }
            case MarkShape.Smile:
            {
                // A shallow arc across the square, its ends turned up: the bottom of an ellipse, centred on the square
                float w = Math.Max(2f, ry * 0.45f);
                float arx = (rx - w * 0.5f) / MathF.Cos(MathF.PI * 0.15f), ary = (2f * ry - w) / (1f - MathF.Sin(MathF.PI * 0.15f));
                var at = center - new Vector2(0, ary * (1f + MathF.Sin(MathF.PI * 0.15f)) * 0.5f);
                AaPaint.Arc(c, at, arx, ary, MathF.PI * 0.15f, MathF.PI * 0.85f, w, d.Color);
                break;
            }
        }
    }

    /// <summary>Uploads the atlas for every eye state as skinned character materials (index = <see cref="EyeState"/>).</summary>
    public static Material[] Materials(PokeModel m, FieldShaders shaders)
    {
        var states = Enum.GetValues<EyeState>();
        var result = new Material[states.Length];
        foreach (var state in states)
        {
            var tex = Paint(m, state).ToTexture();
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
            Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
            result[(int)state] = RenderContext.MaterialFor(shaders.CharacterSkinned, tex);
        }
        return result;
    }
}
