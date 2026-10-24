using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;

namespace MapImporter.Bw2;

/// <summary>
/// Where a chunk's textures lie (plan 22 · U2, the user's ruling of 2026-10-20): a chunk's <c>model.glb</c> read past
/// its JSON chunk into its binary one, as plain numbers, only to find which texture covers each tile. Each triangle's
/// three corners (the <c>POSITION</c> accessor, placed by the skin's joints as the glTF says) and the name of the
/// texture its material uses are all that is taken; nothing of them is kept but the <see cref="PokemonPlatinumEngine.Data.TerrainCover"/>
/// a tile comes to (<see cref="Bw2Cover.OfChunk"/>). No vertex, texture coordinate, colour, normal or image is written
/// anywhere, and the textures themselves are never fetched.
/// </summary>
public static class Bw2Geometry
{
    /// <summary>One triangle: its texture's name and its corners in the original's units from the chunk's middle (sixteen to a tile, +z south).</summary>
    public readonly record struct Triangle(string Texture, Vector3 A, Vector3 B, Vector3 C)
    {
        public Vector3 Normal
        {
            get
            {
                var n = Vector3.Cross(B - A, C - A);
                float l = n.Length();
                return l < 1e-9f ? Vector3.Zero : n / l;
            }
        }
    }

    /// <summary>The triangles of a binary glTF, each with the file name (no folder, no extension) of its material's texture.</summary>
    public static List<Triangle> Read(byte[] glb)
    {
        string json = Bw2Model.JsonChunk(glb);
        int jsonLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12));
        int binStart = 20 + ((jsonLength + 3) & ~3);
        var result = new List<Triangle>();
        if (binStart + 8 > glb.Length) return result;
        int binLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(binStart));
        if (BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(binStart + 4)) != 0x004E4942) throw new InvalidDataException("the glTF's second chunk isn't its binary one");
        var bin = new ReadOnlyMemory<byte>(glb, binStart + 8, Math.Min(binLength, glb.Length - binStart - 8));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var views = root.TryGetProperty("bufferViews", out var bv) ? bv.EnumerateArray().ToList() : new();
        var accessors = root.TryGetProperty("accessors", out var ac) ? ac.EnumerateArray().ToList() : new();

        // The texture of each material, by its image's file name
        var images = root.TryGetProperty("images", out var im) ? im.EnumerateArray().Select(i =>
            i.TryGetProperty("uri", out var u) && u.GetString() is { Length: > 0 } s ? Path.GetFileNameWithoutExtension(s.Replace('\\', '/').Split('/')[^1]) : "").ToList() : new();
        var textures = root.TryGetProperty("textures", out var tx) ? tx.EnumerateArray().Select(t =>
            t.TryGetProperty("source", out var src) && src.GetInt32() is var n && n >= 0 && n < images.Count ? images[n] : "").ToList() : new();
        var materials = root.TryGetProperty("materials", out var ma) ? ma.EnumerateArray().Select(m =>
            m.TryGetProperty("pbrMetallicRoughness", out var pbr) && pbr.TryGetProperty("baseColorTexture", out var bct)
                && bct.GetProperty("index").GetInt32() is var t && t >= 0 && t < textures.Count ? textures[t] : "").ToList() : new();

        var nodes = root.TryGetProperty("nodes", out var nd) ? nd.EnumerateArray().ToList() : new();
        var global = Globals(nodes);

        int Stride(JsonElement view, int natural) => view.TryGetProperty("byteStride", out var s) ? s.GetInt32() : natural;
        int Offset(JsonElement e) => e.TryGetProperty("byteOffset", out var o) ? o.GetInt32() : 0;

        Vector3[] Vec3(int accessor)
        {
            var a = accessors[accessor];
            if (a.GetProperty("componentType").GetInt32() != 5126) throw new InvalidDataException("positions that aren't floats");
            var view = views[a.GetProperty("bufferView").GetInt32()];
            int count = a.GetProperty("count").GetInt32(), stride = Stride(view, 12), start = Offset(view) + Offset(a);
            var v = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var at = bin.Span.Slice(start + i * stride, 12);
                v[i] = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(at), BinaryPrimitives.ReadSingleLittleEndian(at[4..]), BinaryPrimitives.ReadSingleLittleEndian(at[8..]));
            }
            return v;
        }

        int[] Ints(int accessor, int components)
        {
            var a = accessors[accessor];
            int type = a.GetProperty("componentType").GetInt32(), size = type switch { 5121 => 1, 5123 => 2, 5125 => 4, _ => throw new InvalidDataException($"integers of type {type}") };
            var view = views[a.GetProperty("bufferView").GetInt32()];
            int count = a.GetProperty("count").GetInt32() * components, stride = Stride(view, size * components), start = Offset(view) + Offset(a);
            var v = new int[count];
            for (int i = 0; i < count; i++)
            {
                int element = i / components, part = i % components;
                var at = bin.Span[(start + element * stride + part * size)..];
                v[i] = size == 1 ? at[0] : size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(at) : (int)BinaryPrimitives.ReadUInt32LittleEndian(at);
            }
            return v;
        }

        float[] Floats(int accessor, int components)
        {
            var a = accessors[accessor];
            var view = views[a.GetProperty("bufferView").GetInt32()];
            int count = a.GetProperty("count").GetInt32() * components, start = Offset(view) + Offset(a), stride = Stride(view, 4 * components);
            var v = new float[count];
            for (int i = 0; i < count; i++)
                v[i] = BinaryPrimitives.ReadSingleLittleEndian(bin.Span[(start + (i / components) * stride + (i % components) * 4)..]);
            return v;
        }

        var skins = root.TryGetProperty("skins", out var sk) ? sk.EnumerateArray().ToList() : new();
        for (int n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            if (!node.TryGetProperty("mesh", out var meshIndex)) continue;
            var mesh = root.GetProperty("meshes")[meshIndex.GetInt32()];
            // A skinned mesh is placed by its joints (each joint's place times its inverse bind matrix), one with no skin by its node
            Matrix4x4[]? joints = null;
            if (node.TryGetProperty("skin", out var skinIndex) && skinIndex.GetInt32() < skins.Count)
            {
                var skin = skins[skinIndex.GetInt32()];
                var jointNodes = skin.GetProperty("joints").EnumerateArray().Select(j => j.GetInt32()).ToArray();
                var ibm = skin.TryGetProperty("inverseBindMatrices", out var ib) ? Floats(ib.GetInt32(), 16) : null;
                joints = new Matrix4x4[jointNodes.Length];
                for (int j = 0; j < jointNodes.Length; j++)
                {
                    var bind = ibm == null ? Matrix4x4.Identity : Column(ibm, j * 16);
                    joints[j] = bind * global[jointNodes[j]];
                }
            }
            foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
            {
                if (primitive.TryGetProperty("mode", out var mode) && mode.GetInt32() != 4) continue;   // triangles only
                var attributes = primitive.GetProperty("attributes");
                if (!attributes.TryGetProperty("POSITION", out var positionAccessor)) continue;
                var positions = Vec3(positionAccessor.GetInt32());
                if (joints != null && attributes.TryGetProperty("JOINTS_0", out var ja) && attributes.TryGetProperty("WEIGHTS_0", out var wa))
                {
                    var js = Ints(ja.GetInt32(), 4);
                    var ws = accessors[wa.GetInt32()].GetProperty("componentType").GetInt32() == 5126 ? Floats(wa.GetInt32(), 4) : null;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        // The joint of the greatest weight (the chunks bind each vertex to one)
                        int best = 0;
                        if (ws != null) for (int k = 1; k < 4; k++) if (ws[i * 4 + k] > ws[i * 4 + best]) best = k;
                        int joint = js[i * 4 + best];
                        if (joint < joints.Length) positions[i] = Vector3.Transform(positions[i], joints[joint]);
                    }
                }
                else if (!global[n].IsIdentity)
                    for (int i = 0; i < positions.Length; i++) positions[i] = Vector3.Transform(positions[i], global[n]);

                string texture = primitive.TryGetProperty("material", out var mi) && mi.GetInt32() < materials.Count ? materials[mi.GetInt32()] : "";
                int[] indices = primitive.TryGetProperty("indices", out var ix) ? Ints(ix.GetInt32(), 1) : Enumerable.Range(0, positions.Length).ToArray();
                for (int i = 0; i + 2 < indices.Length; i += 3)
                    if (indices[i] < positions.Length && indices[i + 1] < positions.Length && indices[i + 2] < positions.Length)
                        result.Add(new Triangle(texture, positions[indices[i]], positions[indices[i + 1]], positions[indices[i + 2]]));
            }
        }
        return result;
    }

    /// <summary>The triangles of a file, or none where it can't be read.</summary>
    public static List<Triangle> ReadFile(string path) => Read(File.ReadAllBytes(path));

    // A glTF matrix is written column by column; System.Numerics multiplies row vectors, so it is the transpose read row by row
    private static Matrix4x4 Column(float[] m, int at) => new(
        m[at + 0], m[at + 1], m[at + 2], m[at + 3],
        m[at + 4], m[at + 5], m[at + 6], m[at + 7],
        m[at + 8], m[at + 9], m[at + 10], m[at + 11],
        m[at + 12], m[at + 13], m[at + 14], m[at + 15]);

    /// <summary>Each node's place in the scene: its own transform, then its parent's.</summary>
    private static Matrix4x4[] Globals(List<JsonElement> nodes)
    {
        var local = new Matrix4x4[nodes.Count];
        var parent = new int[nodes.Count];
        Array.Fill(parent, -1);
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n.TryGetProperty("matrix", out var m)) local[i] = Column(m.EnumerateArray().Select(v => v.GetSingle()).ToArray(), 0);
            else
            {
                var t = n.TryGetProperty("translation", out var tr) ? tr.EnumerateArray().Select(v => v.GetSingle()).ToArray() : new float[3];
                var r = n.TryGetProperty("rotation", out var ro) ? ro.EnumerateArray().Select(v => v.GetSingle()).ToArray() : new[] { 0f, 0f, 0f, 1f };
                var s = n.TryGetProperty("scale", out var sc) ? sc.EnumerateArray().Select(v => v.GetSingle()).ToArray() : new[] { 1f, 1f, 1f };
                local[i] = Matrix4x4.CreateScale(s[0], s[1], s[2]) * Matrix4x4.CreateFromQuaternion(new Quaternion(r[0], r[1], r[2], r[3]))
                    * Matrix4x4.CreateTranslation(t[0], t[1], t[2]);
            }
            if (n.TryGetProperty("children", out var children))
                foreach (var c in children.EnumerateArray()) if (c.GetInt32() < nodes.Count) parent[c.GetInt32()] = i;
        }
        var global = new Matrix4x4[nodes.Count];
        var done = new bool[nodes.Count];
        Matrix4x4 Of(int i, int depth)
        {
            if (done[i]) return global[i];
            var g = parent[i] >= 0 && depth < 64 ? local[i] * Of(parent[i], depth + 1) : local[i];
            done[i] = true;
            return global[i] = g;
        }
        for (int i = 0; i < nodes.Count; i++) Of(i, 0);
        return global;
    }

    /// <summary>What lies over the middle of each tile of a chunk, by texture name: on the walking surface, and above it.</summary>
    public sealed record Layers(string?[] Ground, string?[] Above, float[] AboveHeight)
    {
        /// <summary>The topmost texture under the walking surface, where neither of the others lies (the sea below a pier's edge: plan 22 · U5, part c).</summary>
        public string?[] Below { get; init; } = Array.Empty<string?>();
    }

    /// <summary>How far, in the original's units, a level triangle may sit from the walking surface and still be its ground.</summary>
    public const float GroundTolerance = 4f;

    /// <summary>
    /// Sorts the level triangles over the middle of each tile of a <paramref name="width"/> × <paramref name="depth"/>
    /// grid into the ground (those within <see cref="GroundTolerance"/> of the walking surface, whose height in units
    /// <paramref name="surface"/> gives for a tile and the triangle's own height (the nearest of a chunk's levels), null where the grid has nothing to say; the topmost wins) and what rises above
    /// it: a tree's crown, a roof, a rock. The grid's middle is the model's origin, as the chunk's terrain grid has it.
    /// </summary>
    public static Layers LayersOver(IReadOnlyList<Triangle> triangles, int width, int depth, Func<int, int, float, float?> surface)
    {
        const float unit = 16f;
        int count = width * depth;
        var ground = new string?[count];
        var above = new string?[count];
        var groundTop = new float[count];
        var aboveTop = new float[count];
        var below = new string?[count];
        var belowTop = new float[count];
        Array.Fill(belowTop, float.NegativeInfinity);
        Array.Fill(groundTop, float.NegativeInfinity);
        Array.Fill(aboveTop, float.NegativeInfinity);
        float halfX = width * unit / 2f, halfZ = depth * unit / 2f;
        foreach (var p in triangles)
        {
            var normal = p.Normal;
            if (MathF.Abs(normal.Y) < 0.2f || string.IsNullOrEmpty(p.Texture)) continue;   // a wall covers no tile
            int x0 = Math.Max(0, (int)MathF.Floor((MathF.Min(p.A.X, MathF.Min(p.B.X, p.C.X)) + halfX) / unit));
            int x1 = Math.Min(width - 1, (int)MathF.Floor((MathF.Max(p.A.X, MathF.Max(p.B.X, p.C.X)) + halfX) / unit));
            int z0 = Math.Max(0, (int)MathF.Floor((MathF.Min(p.A.Z, MathF.Min(p.B.Z, p.C.Z)) + halfZ) / unit));
            int z1 = Math.Min(depth - 1, (int)MathF.Floor((MathF.Max(p.A.Z, MathF.Max(p.B.Z, p.C.Z)) + halfZ) / unit));
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) * unit - halfX, pz = (z + 0.5f) * unit - halfZ;
                    if (!Inside(p, px, pz, out float height)) continue;
                    int i = z * width + x;
                    float? walked = surface(x, z, height);
                    bool onGround = walked == null || MathF.Abs(height - walked.Value) <= GroundTolerance;
                    if (onGround && height > groundTop[i]) { groundTop[i] = height; ground[i] = p.Texture; }
                    if (!onGround && height > (walked ?? 0) && height > aboveTop[i]) { aboveTop[i] = height; above[i] = p.Texture; }
                    if (!onGround && walked is { } level && height < level && height > belowTop[i]) { belowTop[i] = height; below[i] = p.Texture; }
                }
        }
        return new Layers(ground, above, aboveTop) { Below = below };
    }

    private static bool Inside(Triangle p, float x, float z, out float height)
    {
        float d = (p.B.Z - p.C.Z) * (p.A.X - p.C.X) + (p.C.X - p.B.X) * (p.A.Z - p.C.Z);
        height = 0;
        if (MathF.Abs(d) < 1e-6f) return false;
        float u = ((p.B.Z - p.C.Z) * (x - p.C.X) + (p.C.X - p.B.X) * (z - p.C.Z)) / d;
        float v = ((p.C.Z - p.A.Z) * (x - p.C.X) + (p.A.X - p.C.X) * (z - p.C.Z)) / d;
        float w = 1 - u - v;
        const float edge = -0.001f;
        if (u < edge || v < edge || w < edge) return false;
        height = u * p.A.Y + v * p.B.Y + w * p.C.Y;
        return true;
    }

    /// <summary>
    /// The box of a field prop's model (<c>res/field/props/.../model.glb</c>): the least and greatest corner of its
    /// positions as the JSON chunk's accessors give them, in units from the prop's origin; null where it has none.
    /// Nothing of the binary chunk is read for it.
    /// </summary>
    public static (Vector3 Min, Vector3 Max)? Box(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("meshes", out var meshes) || !root.TryGetProperty("accessors", out var accessors)) return null;
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        bool any = false;
        foreach (var mesh in meshes.EnumerateArray())
            foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
            {
                if (!primitive.GetProperty("attributes").TryGetProperty("POSITION", out var pa)) continue;
                var a = accessors[pa.GetInt32()];
                if (!a.TryGetProperty("min", out var lo) || !a.TryGetProperty("max", out var hi)) continue;
                var l = lo.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                var h = hi.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                min = Vector3.Min(min, new Vector3(l[0], l[1], l[2]));
                max = Vector3.Max(max, new Vector3(h[0], h[1], h[2]));
                any = true;
            }
        return any ? (min, max) : null;
    }
}
