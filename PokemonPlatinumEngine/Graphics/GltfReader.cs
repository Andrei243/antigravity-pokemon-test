using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace PokemonPlatinumEngine.Graphics;

internal enum GltfPath { Translation, Rotation, Scale, Weights }

internal enum GltfInterpolation { Linear, Step, CubicSpline }

internal sealed class GltfNode
{
    public string Name = "";
    public int Parent = -1;
    public int[] Children = Array.Empty<int>();
    public Vector3 Translation;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    /// <summary>Set when the node gives a matrix instead of a translation, rotation and scale (it isn't animated then).</summary>
    public Matrix4x4? Matrix;

    public int Mesh = -1, Skin = -1;

    /// <summary>The node's transform relative to its parent (System.Numerics row-vector convention).</summary>
    public Matrix4x4 Local => Matrix ?? Compose(Translation, Rotation, Scale);

    public static Matrix4x4 Compose(Vector3 t, Quaternion r, Vector3 s) =>
        Matrix4x4.CreateScale(s) * Matrix4x4.CreateFromQuaternion(r) * Matrix4x4.CreateTranslation(t);
}

/// <summary>One drawable piece of a mesh, already turned into triangles.</summary>
internal sealed class GltfPrimitive
{
    public Vector3[] Positions = Array.Empty<Vector3>();
    public Vector3[]? Normals;
    public Vector2[]? UVs;
    public Vector4[]? Colors;

    /// <summary>Four joints per vertex (indices into the skin's joint list) with their weights.</summary>
    public int[]? Joints;
    public float[]? Weights;

    public int[] Indices = Array.Empty<int>();
    public int Material = -1;
}

internal sealed class GltfMesh
{
    public string Name = "";
    public readonly List<GltfPrimitive> Primitives = new();
}

internal sealed class GltfMaterial
{
    public string Name = "";
    public Vector4 BaseColor = Vector4.One;

    /// <summary>The image the base colour texture shows (-1: none), the texture coordinate set it reads, and its transform.</summary>
    public int Image = -1;
    public int TexCoord;
    public Vector2 UvOffset, UvScale = Vector2.One;
    public float UvRotation;

    public Vector3 Emissive;

    /// <summary>How metal the surface is, as far as its factor alone can say (see <c>Materials</c> in the reader).</summary>
    public float Metallic;
    public string AlphaMode = "OPAQUE";
    public float AlphaCutoff = 0.5f;
}

internal sealed class GltfImage
{
    public string Name = "";
    public string MimeType = "";
    public byte[] Data = Array.Empty<byte>();
}

internal sealed class GltfSkin
{
    public string Name = "";
    public int[] Joints = Array.Empty<int>();
    public Matrix4x4[] InverseBind = Array.Empty<Matrix4x4>();
}

internal sealed class GltfChannel
{
    public int Node;
    public GltfPath Path;
    public float[] Times = Array.Empty<float>();

    /// <summary><see cref="Width"/> floats per key (three for each key with cubic splines: in-tangent, value, out-tangent).</summary>
    public float[] Values = Array.Empty<float>();
    public int Width;
    public GltfInterpolation Interpolation;
}

internal sealed class GltfAnimation
{
    public string Name = "";
    public readonly List<GltfChannel> Channels = new();
    public float Duration;
}

/// <summary>
/// A glTF 2.0 asset read into plain arrays (plan 03 · D5's import path): the node tree, meshes as triangles,
/// materials with their base colour images (still encoded), skins and animations. No GPU calls.
/// </summary>
internal sealed class GltfAsset
{
    public readonly List<GltfNode> Nodes = new();
    public readonly List<GltfMesh> Meshes = new();
    public readonly List<GltfMaterial> Materials = new();
    public readonly List<GltfImage> Images = new();
    public readonly List<GltfSkin> Skins = new();
    public readonly List<GltfAnimation> Animations = new();

    /// <summary>The nodes of the scene shown, parents before their children.</summary>
    public int[] Order = Array.Empty<int>();

    /// <summary>Every node's transform in the default pose, relative to the scene.</summary>
    public Matrix4x4[] RestGlobals()
    {
        var globals = new Matrix4x4[Nodes.Count];
        foreach (int n in Order)
        {
            var node = Nodes[n];
            globals[n] = node.Parent < 0 ? node.Local : node.Local * globals[node.Parent];
        }
        return globals;
    }
}

/// <summary>
/// Reads glTF 2.0 files: binary <c>.glb</c>, or <c>.gltf</c> with its buffers and images embedded or in files beside
/// it. Compressed meshes (Draco, meshopt) aren't supported; their files are refused with a message saying so.
/// </summary>
internal static class GltfReader
{
    private const uint GlbMagic = 0x46546C67, JsonChunk = 0x4E4F534A, BinChunk = 0x004E4942;

    public static GltfAsset Read(string path) => Parse(File.ReadAllBytes(path), Path.GetDirectoryName(Path.GetFullPath(path)));

    /// <param name="folder">Where files the asset refers to are looked for (null: only embedded data can be read).</param>
    public static GltfAsset Parse(byte[] data, string? folder)
    {
        byte[]? bin = null;
        string json;
        if (data.Length >= 12 && BitConverter.ToUInt32(data, 0) == GlbMagic)
        {
            if (BitConverter.ToUInt32(data, 4) != 2) throw new InvalidDataException("only glTF 2.0 binaries can be read");
            int at = 12;
            json = "";
            while (at + 8 <= data.Length)
            {
                int length = BitConverter.ToInt32(data, at);
                uint type = BitConverter.ToUInt32(data, at + 4);
                if (length < 0 || at + 8 + length > data.Length) throw new InvalidDataException("a chunk runs past the end of the file");
                if (type == JsonChunk) json = Encoding.UTF8.GetString(data, at + 8, length);
                else if (type == BinChunk && bin == null) bin = data.AsSpan(at + 8, length).ToArray();
                at += 8 + ((length + 3) & ~3);
            }
            if (json.Length == 0) throw new InvalidDataException("the binary has no JSON chunk");
        }
        else json = Encoding.UTF8.GetString(data);

        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
        return new Parser(doc.RootElement, bin, folder).Run();
    }

    private sealed class Parser
    {
        private readonly JsonElement root;
        private readonly byte[]? bin;
        private readonly string? folder;
        private readonly List<byte[]> buffers = new();
        private readonly GltfAsset asset = new();

        public Parser(JsonElement root, byte[]? bin, string? folder)
        {
            this.root = root;
            this.bin = bin;
            this.folder = folder;
        }

        public GltfAsset Run()
        {
            foreach (var ext in Array("extensionsRequired"))
            {
                string name = ext.GetString() ?? "";
                if (name is "KHR_draco_mesh_compression" or "EXT_meshopt_compression")
                    throw new NotSupportedException($"the meshes are compressed ({name}); save the model again without mesh compression (in Blender: glTF export, Compression off)");
            }

            foreach (var b in Array("buffers")) buffers.Add(Buffer(b));
            Images();
            Materials();
            Meshes();
            Nodes();
            Skins();
            Animations();
            return asset;
        }

        // ------------------------------------------------------------------ json helpers

        private JsonElement.ArrayEnumerator Array(string name) => Array(root, name);

        private static JsonElement.ArrayEnumerator Array(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray() : default;

        private static int Count(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array ? a.GetArrayLength() : 0;

        private static JsonElement At(JsonElement e, string name, int index)
        {
            if (!e.TryGetProperty(name, out var a) || a.ValueKind != JsonValueKind.Array || index < 0 || index >= a.GetArrayLength())
                throw new InvalidDataException($"{name}[{index}] doesn't exist");
            return a[index];
        }

        private static int Int(JsonElement e, string name, int fallback = -1) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : fallback;

        private static float Float(JsonElement e, string name, float fallback) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : fallback;

        private static string Text(JsonElement e, string name, string fallback = "") =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;

        private static float[] Floats(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array) return System.Array.Empty<float>();
            var result = new float[v.GetArrayLength()];
            int i = 0;
            foreach (var f in v.EnumerateArray()) result[i++] = f.GetSingle();
            return result;
        }

        private static bool Has(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out _);

        private static JsonElement Child(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;

        // ------------------------------------------------------------------ data

        private byte[] Buffer(JsonElement b)
        {
            string uri = Text(b, "uri");
            if (uri.Length == 0) return bin ?? throw new InvalidDataException("a buffer without a uri needs the binary chunk of a .glb");
            return Resource(uri);
        }

        /// <summary>The bytes a uri names: embedded as base64, or a file beside the asset.</summary>
        private byte[] Resource(string uri)
        {
            if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int comma = uri.IndexOf(',');
                if (comma < 0 || !uri.AsSpan(0, comma).EndsWith(";base64")) throw new InvalidDataException("only base64 data uris can be read");
                return Convert.FromBase64String(uri[(comma + 1)..]);
            }
            if (folder == null) throw new InvalidDataException($"{uri} is a separate file and there is no folder to look in");
            string path = Path.GetFullPath(Path.Combine(folder, Uri.UnescapeDataString(uri)));
            if (!File.Exists(path)) throw new FileNotFoundException($"the file {uri} the model refers to is missing", path);
            return File.ReadAllBytes(path);
        }

        private (byte[] Data, int Offset, int Length, int Stride) View(int index)
        {
            var v = At(root, "bufferViews", index);
            if (Has(Child(v, "extensions"), "EXT_meshopt_compression"))
                throw new NotSupportedException("the model uses meshopt compression; save it again without compression");
            int buffer = Int(v, "buffer");
            if (buffer < 0 || buffer >= buffers.Count) throw new InvalidDataException($"buffer view {index} names a buffer that doesn't exist");
            int offset = Int(v, "byteOffset", 0), length = Int(v, "byteLength", 0);
            var data = buffers[buffer];
            if (offset < 0 || length < 0 || offset + length > data.Length) throw new InvalidDataException($"buffer view {index} runs past its buffer");
            return (data, offset, length, Int(v, "byteStride", 0));
        }

        private static int Components(string type) => type switch
        {
            "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT2" => 4, "MAT3" => 9, "MAT4" => 16,
            _ => throw new InvalidDataException($"unknown accessor type {type}")
        };

        private static int Size(int componentType) => componentType switch
        {
            5120 or 5121 => 1, 5122 or 5123 => 2, 5125 or 5126 => 4,
            _ => throw new InvalidDataException($"unknown component type {componentType}")
        };

        private static float Element(byte[] d, int at, int type, bool normalized) => type switch
        {
            5126 => BitConverter.ToSingle(d, at),
            5121 => normalized ? d[at] / 255f : d[at],
            5120 => normalized ? Math.Max((sbyte)d[at] / 127f, -1f) : (sbyte)d[at],
            5123 => normalized ? BitConverter.ToUInt16(d, at) / 65535f : BitConverter.ToUInt16(d, at),
            5122 => normalized ? Math.Max(BitConverter.ToInt16(d, at) / 32767f, -1f) : BitConverter.ToInt16(d, at),
            5125 => BitConverter.ToUInt32(d, at),
            _ => 0f
        };

        /// <summary>
        /// An accessor's values as floats, <paramref name="width"/> per element. Integer data is scaled to 0..1 (or
        /// -1..1) when the accessor is normalized, or when <paramref name="normalize"/> says the attribute always is.
        /// </summary>
        private float[] Read(int index, out int width, bool normalize = false)
        {
            var a = At(root, "accessors", index);
            int count = Int(a, "count", 0);
            int type = Int(a, "componentType", 5126);
            width = Components(Text(a, "type", "SCALAR"));
            bool normalized = (Has(a, "normalized") && a.GetProperty("normalized").GetBoolean()) || (normalize && type != 5126);
            var result = new float[count * width];
            int size = Size(type);
            if (Int(a, "bufferView") is int view and >= 0)
            {
                var (data, offset, length, stride) = View(view);
                int start = offset + Int(a, "byteOffset", 0);
                int step = stride > 0 ? stride : size * width;
                if (count > 0 && start + (count - 1) * step + size * width > offset + length) throw new InvalidDataException($"accessor {index} runs past its buffer view");
                for (int i = 0; i < count; i++)
                    for (int k = 0; k < width; k++)
                        result[i * width + k] = Element(data, start + i * step + k * size, type, normalized);
            }
            if (Has(a, "sparse"))
            {
                var sparse = a.GetProperty("sparse");
                int n = Int(sparse, "count", 0);
                var ix = sparse.GetProperty("indices");
                var vals = sparse.GetProperty("values");
                var (idata, ioff, _, _) = View(Int(ix, "bufferView"));
                var (vdata, voff, _, _) = View(Int(vals, "bufferView"));
                int itype = Int(ix, "componentType", 5125), isize = Size(itype);
                int istart = ioff + Int(ix, "byteOffset", 0), vstart = voff + Int(vals, "byteOffset", 0);
                for (int s = 0; s < n; s++)
                {
                    int target = (int)Element(idata, istart + s * isize, itype, false);
                    if ((uint)target >= (uint)count) throw new InvalidDataException($"accessor {index} has a sparse index out of range");
                    for (int k = 0; k < width; k++) result[target * width + k] = Element(vdata, vstart + (s * width + k) * size, type, normalized);
                }
            }
            return result;
        }

        private int[] ReadInts(int index)
        {
            var values = Read(index, out _);
            var result = new int[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = (int)values[i];
            return result;
        }

        // ------------------------------------------------------------------ parts of the asset

        private void Images()
        {
            foreach (var img in Array("images"))
            {
                var image = new GltfImage { Name = Text(img, "name"), MimeType = Text(img, "mimeType") };
                string uri = Text(img, "uri");
                try
                {
                    if (uri.Length > 0)
                    {
                        image.Data = Resource(uri);
                        if (image.MimeType.Length == 0)
                            image.MimeType = uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? uri[5..uri.IndexOf(';')] : MimeOf(Path.GetExtension(uri));
                    }
                    else if (Int(img, "bufferView") is int view and >= 0)
                    {
                        var (data, offset, length, _) = View(view);
                        image.Data = data.AsSpan(offset, length).ToArray();
                    }
                }
                catch (FileNotFoundException) { }
                asset.Images.Add(image);
            }
        }

        private static string MimeOf(string extension) => extension.ToLowerInvariant() switch
        {
            ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", ".ktx2" => "image/ktx2", _ => ""
        };

        /// <summary>The image a texture shows (its own source, or one an extension gives).</summary>
        private int ImageOf(int texture)
        {
            if (texture < 0 || texture >= Count(root, "textures")) return -1;
            var t = root.GetProperty("textures")[texture];
            int source = Int(t, "source");
            if (source >= 0) return source;
            foreach (var ext in new[] { "EXT_texture_webp", "KHR_texture_basisu", "MSFT_texture_dds" })
                if (Child(Child(t, "extensions"), ext) is { ValueKind: JsonValueKind.Object } e && Int(e, "source") is int s and >= 0) return s;
            return -1;
        }

        private void Materials()
        {
            foreach (var m in Array("materials"))
            {
                var mat = new GltfMaterial { Name = Text(m, "name"), AlphaMode = Text(m, "alphaMode", "OPAQUE"), AlphaCutoff = Float(m, "alphaCutoff", 0.5f) };
                var pbr = Child(m, "pbrMetallicRoughness");
                var factor = Floats(pbr, "baseColorFactor");
                if (factor.Length == 4) mat.BaseColor = new Vector4(factor[0], factor[1], factor[2], factor[3]);
                // glTF calls a surface metal unless told otherwise, but the factor alone says so only where no texture
                // varies it texel by texel (this reader reads none), and a material without the PBR block or marked
                // unlit was never made with metal in mind: lighting those as metal would turn most creatures to chrome
                bool varies = Child(pbr, "metallicRoughnessTexture").ValueKind == JsonValueKind.Object;
                bool unlit = Child(Child(m, "extensions"), "KHR_materials_unlit").ValueKind == JsonValueKind.Object;
                mat.Metallic = pbr.ValueKind != JsonValueKind.Object || varies || unlit ? 0f : Float(pbr, "metallicFactor", 1f);
                var tex = Child(pbr, "baseColorTexture");
                // Older models keep their colour in the specular-glossiness extension instead
                var specGloss = Child(Child(m, "extensions"), "KHR_materials_pbrSpecularGlossiness");
                if (tex.ValueKind != JsonValueKind.Object && specGloss.ValueKind == JsonValueKind.Object)
                {
                    tex = Child(specGloss, "diffuseTexture");
                    var diffuse = Floats(specGloss, "diffuseFactor");
                    if (diffuse.Length == 4) mat.BaseColor = new Vector4(diffuse[0], diffuse[1], diffuse[2], diffuse[3]);
                    mat.Metallic = 0f;
                }
                if (tex.ValueKind == JsonValueKind.Object)
                {
                    mat.Image = ImageOf(Int(tex, "index"));
                    mat.TexCoord = Int(tex, "texCoord", 0);
                    var transform = Child(Child(tex, "extensions"), "KHR_texture_transform");
                    if (transform.ValueKind == JsonValueKind.Object)
                    {
                        var off = Floats(transform, "offset");
                        var scale = Floats(transform, "scale");
                        if (off.Length == 2) mat.UvOffset = new Vector2(off[0], off[1]);
                        if (scale.Length == 2) mat.UvScale = new Vector2(scale[0], scale[1]);
                        mat.UvRotation = Float(transform, "rotation", 0f);
                        if (Int(transform, "texCoord") is int tc and >= 0) mat.TexCoord = tc;
                    }
                }
                var emissive = Floats(m, "emissiveFactor");
                if (emissive.Length == 3) mat.Emissive = new Vector3(emissive[0], emissive[1], emissive[2]);
                asset.Materials.Add(mat);
            }
        }

        private void Meshes()
        {
            foreach (var m in Array("meshes"))
            {
                var mesh = new GltfMesh { Name = Text(m, "name") };
                foreach (var p in Array(m, "primitives"))
                {
                    int mode = Int(p, "mode", 4);
                    if (mode < 4) continue; // points and lines draw nothing here
                    var attributes = Child(p, "attributes");
                    if (Int(attributes, "POSITION") < 0) continue;
                    if (Has(Child(p, "extensions"), "KHR_draco_mesh_compression") && !Has(At(root, "accessors", Int(attributes, "POSITION")), "bufferView"))
                        throw new NotSupportedException("the meshes are compressed with Draco; save the model again without mesh compression");

                    var prim = new GltfPrimitive { Material = Int(p, "material") };
                    var pos = Read(Int(attributes, "POSITION"), out _);
                    int count = pos.Length / 3;
                    prim.Positions = new Vector3[count];
                    for (int i = 0; i < count; i++) prim.Positions[i] = new Vector3(pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]);
                    if (Int(attributes, "NORMAL") is int ni and >= 0)
                    {
                        var n = Read(ni, out _, normalize: true);
                        prim.Normals = new Vector3[count];
                        for (int i = 0; i < count && i * 3 + 2 < n.Length; i++) prim.Normals[i] = new Vector3(n[i * 3], n[i * 3 + 1], n[i * 3 + 2]);
                    }
                    int texCoord = prim.Material >= 0 && prim.Material < asset.Materials.Count ? asset.Materials[prim.Material].TexCoord : 0;
                    int uvAccessor = Int(attributes, "TEXCOORD_" + texCoord);
                    if (uvAccessor < 0) uvAccessor = Int(attributes, "TEXCOORD_0");
                    if (uvAccessor >= 0)
                    {
                        var uv = Read(uvAccessor, out _, normalize: true);
                        prim.UVs = new Vector2[count];
                        for (int i = 0; i < count && i * 2 + 1 < uv.Length; i++) prim.UVs[i] = new Vector2(uv[i * 2], uv[i * 2 + 1]);
                    }
                    if (Int(attributes, "COLOR_0") is int ci and >= 0)
                    {
                        var c = Read(ci, out int w, normalize: true);
                        prim.Colors = new Vector4[count];
                        for (int i = 0; i < count; i++)
                            prim.Colors[i] = new Vector4(c[i * w], c[i * w + 1], c[i * w + 2], w == 4 ? c[i * w + 3] : 1f);
                    }
                    if (Int(attributes, "JOINTS_0") is int ji and >= 0 && Int(attributes, "WEIGHTS_0") is int wi and >= 0)
                    {
                        prim.Joints = ReadInts(ji);
                        prim.Weights = Read(wi, out _, normalize: true);
                        if (prim.Joints.Length < count * 4 || prim.Weights.Length < count * 4) prim.Joints = null;
                    }

                    int[] indices = Int(p, "indices") is int ii and >= 0 ? ReadInts(ii) : Sequence(count);
                    prim.Indices = mode switch
                    {
                        5 => Strip(indices),
                        6 => Fan(indices),
                        _ => indices
                    };
                    foreach (int i in prim.Indices)
                        if ((uint)i >= (uint)count) throw new InvalidDataException($"mesh {mesh.Name} has an index past its vertices");
                    mesh.Primitives.Add(prim);
                }
                asset.Meshes.Add(mesh);
            }
        }

        private static int[] Sequence(int n)
        {
            var s = new int[n];
            for (int i = 0; i < n; i++) s[i] = i;
            return s;
        }

        private static int[] Strip(int[] s)
        {
            var t = new List<int>();
            for (int i = 2; i < s.Length; i++)
            {
                if (i % 2 == 0) t.AddRange(new[] { s[i - 2], s[i - 1], s[i] });
                else t.AddRange(new[] { s[i - 1], s[i - 2], s[i] });
            }
            return t.ToArray();
        }

        private static int[] Fan(int[] s)
        {
            var t = new List<int>();
            for (int i = 2; i < s.Length; i++) t.AddRange(new[] { s[0], s[i - 1], s[i] });
            return t.ToArray();
        }

        private void Nodes()
        {
            foreach (var n in Array("nodes"))
            {
                var node = new GltfNode { Name = Text(n, "name"), Mesh = Int(n, "mesh"), Skin = Int(n, "skin") };
                var m = Floats(n, "matrix");
                if (m.Length == 16)
                    node.Matrix = new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
                var t = Floats(n, "translation");
                if (t.Length == 3) node.Translation = new Vector3(t[0], t[1], t[2]);
                var r = Floats(n, "rotation");
                if (r.Length == 4) node.Rotation = Quaternion.Normalize(new Quaternion(r[0], r[1], r[2], r[3]));
                var s = Floats(n, "scale");
                if (s.Length == 3) node.Scale = new Vector3(s[0], s[1], s[2]);
                var children = new List<int>();
                foreach (var c in Array(n, "children")) children.Add(c.GetInt32());
                node.Children = children.ToArray();
                if (node.Mesh >= asset.Meshes.Count) node.Mesh = -1;
                asset.Nodes.Add(node);
            }
            for (int i = 0; i < asset.Nodes.Count; i++)
                foreach (int c in asset.Nodes[i].Children)
                {
                    if ((uint)c >= (uint)asset.Nodes.Count || asset.Nodes[c].Parent >= 0) throw new InvalidDataException("the node tree is broken");
                    asset.Nodes[c].Parent = i;
                }

            // The scene shown: the default one, or the first, or every node that has no parent
            var roots = new List<int>();
            int scene = Int(root, "scene", 0);
            if (Count(root, "scenes") > scene)
                foreach (var n in Array(root.GetProperty("scenes")[scene], "nodes")) roots.Add(n.GetInt32());
            if (roots.Count == 0)
                for (int i = 0; i < asset.Nodes.Count; i++)
                    if (asset.Nodes[i].Parent < 0) roots.Add(i);
            var order = new List<int>();
            var stack = new Stack<int>();
            for (int i = roots.Count - 1; i >= 0; i--) stack.Push(roots[i]);
            var seen = new bool[asset.Nodes.Count];
            while (stack.Count > 0)
            {
                int n = stack.Pop();
                if ((uint)n >= (uint)seen.Length || seen[n]) continue;
                seen[n] = true;
                order.Add(n);
                var children = asset.Nodes[n].Children;
                for (int i = children.Length - 1; i >= 0; i--) stack.Push(children[i]);
            }
            asset.Order = order.ToArray();
        }

        private void Skins()
        {
            foreach (var s in Array("skins"))
            {
                var skin = new GltfSkin { Name = Text(s, "name") };
                var joints = new List<int>();
                foreach (var j in Array(s, "joints")) joints.Add(j.GetInt32());
                skin.Joints = joints.ToArray();
                skin.InverseBind = new Matrix4x4[skin.Joints.Length];
                System.Array.Fill(skin.InverseBind, Matrix4x4.Identity);
                if (Int(s, "inverseBindMatrices") is int ib and >= 0)
                {
                    var m = Read(ib, out _);
                    for (int i = 0; i < skin.Joints.Length && i * 16 + 15 < m.Length; i++)
                        skin.InverseBind[i] = new Matrix4x4(m[i * 16], m[i * 16 + 1], m[i * 16 + 2], m[i * 16 + 3], m[i * 16 + 4], m[i * 16 + 5], m[i * 16 + 6], m[i * 16 + 7],
                            m[i * 16 + 8], m[i * 16 + 9], m[i * 16 + 10], m[i * 16 + 11], m[i * 16 + 12], m[i * 16 + 13], m[i * 16 + 14], m[i * 16 + 15]);
                }
                foreach (int j in skin.Joints)
                    if ((uint)j >= (uint)asset.Nodes.Count) throw new InvalidDataException($"skin {skin.Name} names a joint that doesn't exist");
                asset.Skins.Add(skin);
            }
        }

        private void Animations()
        {
            foreach (var a in Array("animations"))
            {
                var anim = new GltfAnimation { Name = Text(a, "name") };
                var samplers = new List<JsonElement>();
                foreach (var s in Array(a, "samplers")) samplers.Add(s);
                foreach (var c in Array(a, "channels"))
                {
                    var target = Child(c, "target");
                    int node = Int(target, "node");
                    int sampler = Int(c, "sampler");
                    if (node < 0 || node >= asset.Nodes.Count || sampler < 0 || sampler >= samplers.Count) continue;
                    var path = Text(target, "path") switch
                    {
                        "translation" => GltfPath.Translation, "rotation" => GltfPath.Rotation, "scale" => GltfPath.Scale, _ => GltfPath.Weights
                    };
                    if (path == GltfPath.Weights) continue;
                    var s = samplers[sampler];
                    var channel = new GltfChannel
                    {
                        Node = node, Path = path,
                        Interpolation = Text(s, "interpolation", "LINEAR") switch { "STEP" => GltfInterpolation.Step, "CUBICSPLINE" => GltfInterpolation.CubicSpline, _ => GltfInterpolation.Linear },
                        Times = Read(Int(s, "input"), out _),
                        Values = Read(Int(s, "output"), out int width, normalize: path == GltfPath.Rotation)
                    };
                    channel.Width = width;
                    int keys = channel.Times.Length * (channel.Interpolation == GltfInterpolation.CubicSpline ? 3 : 1);
                    if (channel.Times.Length == 0 || channel.Values.Length < keys * width) continue;
                    anim.Channels.Add(channel);
                    anim.Duration = Math.Max(anim.Duration, channel.Times[^1]);
                }
                if (anim.Channels.Count > 0) asset.Animations.Add(anim);
            }
        }
    }
}
