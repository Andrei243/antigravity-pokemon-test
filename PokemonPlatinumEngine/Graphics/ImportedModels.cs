using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What a clip of an imported model is played for.</summary>
internal enum ClipRole { Idle, Physical, Special, Status, Hit, Faint, Entry }

/// <summary>
/// One drawable piece of an imported model: triangles of one material that use at most
/// <see cref="SkinnedModel.MaxBones"/> bones and fit one mesh. Its vertices' bone indices point into
/// <see cref="Palette"/>, which lists bones of the model.
/// </summary>
internal sealed class ImportedPart
{
    public SdfMesh Mesh = new();
    public Vector2[] UVs = Array.Empty<Vector2>();

    /// <summary>The surface pushed out by the outline's width (empty for parts that get no outline).</summary>
    public SdfMesh Shell = new();

    public int[] Palette = Array.Empty<int>();

    /// <summary>The base colour image (-1: the vertex colours alone).</summary>
    public int Image = -1;

    public SkinnedModel? Body, Outline;
}

/// <summary>
/// The part of a <see cref="PokeModel"/> that came from a glTF file (plan 03 · D5): its pieces, its bones (a node of
/// the file and the offset that takes the mesh to it), its clips and the transform that sizes and places it.
/// </summary>
internal sealed class ImportedRig
{
    public GltfAsset Asset = null!;
    public string Source = "";

    /// <summary>The file's space to model space (turned to face +Z, scaled to a height of 1, feet at the origin), and back.</summary>
    public Matrix4x4 Normalize = Matrix4x4.Identity, Denormalize = Matrix4x4.Identity;

    public readonly List<ImportedPart> Parts = new();
    public (int Node, Matrix4x4 Offset)[] Bones = Array.Empty<(int, Matrix4x4)>();
    public readonly Dictionary<ClipRole, int> Clips = new();

    /// <summary>The base colour images, decoded (null where an image couldn't be read).</summary>
    public PixelCanvas?[] Images = Array.Empty<PixelCanvas?>();

    // On the GPU
    public Material?[] Materials = Array.Empty<Material?>();

    // Scratch for posing, on the main thread
    internal Matrix4x4[] BodySkin = new Matrix4x4[2];
    internal Matrix4x4[] Globals = Array.Empty<Matrix4x4>();
    internal Vector3[] T = Array.Empty<Vector3>(), S = Array.Empty<Vector3>();
    internal Quaternion[] R = Array.Empty<Quaternion>();
    internal readonly Matrix4x4[] Palette = new Matrix4x4[SkinnedModel.MaxBones];

    public bool Has(ClipRole role) => Clips.ContainsKey(role);
}

/// <summary>Settings an optional <c>&lt;model&gt;.json</c> beside a model file can give.</summary>
internal sealed class ImportOptions
{
    /// <summary>Degrees to turn the model about the vertical, for files whose front isn't +Z.</summary>
    public float Yaw { get; set; }

    public float? Fill { get; set; }
    public bool? Hovers { get; set; }

    /// <summary>Clip names by role ("idle", "physical", "special", "status", "hit", "faint", "entry"), where the names don't say.</summary>
    public Dictionary<string, string>? Clips { get; set; }

    public static ImportOptions Read(string path)
    {
        if (!File.Exists(path)) return new ImportOptions();
        return JsonSerializer.Deserialize<ImportOptions>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true })
               ?? new ImportOptions();
    }
}

/// <summary>
/// Finds the model files that replace generated and hand-built models (plan 04's asset policy): a <c>.glb</c> or
/// <c>.gltf</c> named after the species (any case and punctuation: <c>Mr. Mime.glb</c>, <c>mr-mime.glb</c>) or its
/// National Pokédex number (<c>0025.glb</c>), in <c>overrides/models</c> next to the game or in the folder it was
/// started from. Git ignores every <c>overrides</c> folder, so these files never go into the repository.
/// </summary>
internal static class ModelOverrides
{
    public static readonly List<string> Folders = new()
    {
        Path.Combine(AppContext.BaseDirectory, "overrides", "models"),
        Path.Combine(Environment.CurrentDirectory, "overrides", "models")
    };

    private static Dictionary<string, string>? files;
    private static readonly object Gate = new();

    /// <summary>Looks at the folders again (after files were added or removed).</summary>
    public static void Refresh()
    {
        lock (Gate) files = null;
    }

    /// <summary>The model file for a species, or null if there is none.</summary>
    public static string? Find(string species)
    {
        var map = Files();
        if (map.Count == 0) return null;
        if (map.TryGetValue(Key(species), out var path)) return path;
        if (Data.PokemonDatabase.Get(species) is { } data && map.TryGetValue("#" + data.DexNumber, out path)) return path;
        return null;
    }

    private static Dictionary<string, string> Files()
    {
        lock (Gate)
        {
            if (files != null) return files;
            var map = new Dictionary<string, string>();
            foreach (var folder in Folders.Distinct())
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in Directory.EnumerateFiles(folder).OrderBy(f => f, StringComparer.Ordinal))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is not (".glb" or ".gltf")) continue;
                    string stem = Path.GetFileNameWithoutExtension(file);
                    string key = stem.All(char.IsDigit) && stem.Length > 0 ? "#" + int.Parse(stem) : Key(stem);
                    map.TryAdd(key, file);
                }
            }
            return files = map;
        }
    }

    /// <summary>A name as letters and digits only, lowercase: "Mr. Mime" and "mr-mime" are the same; ♀ and ♂ become f and m.</summary>
    internal static string Key(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in name.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (c == '♀') sb.Append('f');
            else if (c == '♂') sb.Append('m');
            else if (char.IsLetterOrDigit(c) && c < 128) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}

/// <summary>
/// Turns a glTF asset into a <see cref="PokeModel"/> that battles, menus and the harness draw like any other
/// (plan 03 · D5): skinned on the GPU in pieces, lit by the character shader with its textures, outlined by a shell,
/// and played with the file's own clips where their names say what they are for, with the body plan's motion for
/// the rest. Everything but <see cref="Upload"/> runs without GPU calls.
/// </summary>
internal static class ImportedModels
{
    /// <summary>Raise when the conversion changes, so menu sprites baked from imported models are baked again.</summary>
    public const int Version = 1;

    /// <summary>What went wrong with model files, for the harness and the log.</summary>
    public static readonly ConcurrentQueue<string> Problems = new();

    public static void Report(string message)
    {
        Problems.Enqueue(message);
        Raylib.TraceLog(TraceLogLevel.Warning, "MODELS: " + message);
    }

    public static PokeModel Load(string path, string species)
    {
        var asset = GltfReader.Read(path);
        var options = ImportOptions.Read(Path.ChangeExtension(path, ".json"));
        return FromAsset(asset, species, options, Path.GetFileName(path));
    }

    public static PokeModel FromAsset(GltfAsset asset, string species, ImportOptions? options = null, string source = "")
    {
        options ??= new ImportOptions();
        var genome = PokemonGenomes.For(species);
        var m = new PokeModel
        {
            Species = species,
            Plan = genome?.Plan ?? BodyPlan.Biped,
            Fill = options.Fill ?? genome?.Fill ?? 0.75f,
            Hovers = options.Hovers ?? genome?.Hovers ?? false
        };
        var rig = new ImportedRig { Asset = asset, Source = source };
        m.Imported = rig;

        var rest = asset.RestGlobals();
        var gathered = Gather(asset, rest, rig);
        if (gathered.Count == 0) throw new InvalidDataException("the model has no triangles");

        // Turn, scale and place: the file's front to +Z, a height of 1, the lowest point on the ground
        var yaw = Matrix4x4.CreateRotationY(options.Yaw * MathF.PI / 180f);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var g in gathered)
            foreach (var p in g.World)
            {
                var q = Vector3.Transform(p, yaw);
                min = Vector3.Min(min, q);
                max = Vector3.Max(max, q);
            }
        float height = max.Y - min.Y;
        if (!(height > 1e-6f)) height = Math.Max(max.X - min.X, max.Z - min.Z);
        if (!(height > 1e-6f)) throw new InvalidDataException("the model has no size");
        float scale = 1f / height;
        rig.Normalize = yaw * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(-(min.X + max.X) / 2f * scale, -min.Y * scale, -(min.Z + max.Z) / 2f * scale);
        Matrix4x4.Invert(rig.Normalize, out rig.Denormalize);

        BuildParts(asset, gathered, rig, yaw);
        Measure(m, gathered, rig);

        // The body plan's skeleton, reduced to the root and the body: the clips sway, squash and topple it whole
        m.Skeleton.Add("root", -1, Vector3.Zero);
        m.Bones.Add(new PokeBoneInfo(PokeRole.Root, 0f, 0f, false));
        m.Skeleton.Add("body", 0, new Vector3(0, m.Height * 0.42f, 0));
        m.Bones.Add(new PokeBoneInfo(PokeRole.Body, 0f, 0f, false));
        m.Pose = new SkeletonPose(2);
        m.Skin = new Matrix4x4[rig.Bones.Length];
        rig.Globals = new Matrix4x4[asset.Nodes.Count];
        rig.T = new Vector3[asset.Nodes.Count];
        rig.R = new Quaternion[asset.Nodes.Count];
        rig.S = new Vector3[asset.Nodes.Count];

        foreach (var (role, index) in MatchClips(asset.Animations.Select(a => a.Name).ToList(), options.Clips)) rig.Clips[role] = index;
        m.OwnIdle = rig.Has(ClipRole.Idle);

        // Outlines half a sprite pixel wide, like every other model
        var framing = PokemonSprites.Framing(m, SpriteView.Front, PokemonSprites.Size);
        float width = framing.WorldPerPixel * PokemonModels.OutlinePixels;
        foreach (var part in rig.Parts) part.Shell = Shell(part, width);

        rig.Images = asset.Images.Select(Decode).ToArray();
        return m;
    }

    // ------------------------------------------------------------------ gathering the triangles

    /// <summary>A primitive with the bones of each vertex (into the rig's bones) and where it is in the default pose.</summary>
    private sealed class Gathered
    {
        public GltfPrimitive Prim = null!;
        public GltfMaterial? Material;
        public int[] Bones = Array.Empty<int>();
        public float[] Weights = Array.Empty<float>();
        public Vector3[] World = Array.Empty<Vector3>();
    }

    /// <summary>
    /// Every primitive of every mesh in the scene, its vertices bound to bones: skinned meshes to their skin's joints
    /// (each joint with its inverse bind matrix), others wholly to the node that holds them.
    /// </summary>
    private static List<Gathered> Gather(GltfAsset asset, Matrix4x4[] rest, ImportedRig rig)
    {
        var bones = new List<(int Node, Matrix4x4 Offset)>();
        var jointBone = new Dictionary<(int Skin, int Slot), int>();
        var nodeBone = new Dictionary<int, int>();
        int Rigid(int node)
        {
            if (nodeBone.TryGetValue(node, out int b)) return b;
            nodeBone[node] = bones.Count;
            bones.Add((node, Matrix4x4.Identity));
            return bones.Count - 1;
        }

        var result = new List<Gathered>();
        foreach (int n in asset.Order)
        {
            var node = asset.Nodes[n];
            if (node.Mesh < 0) continue;
            var skin = node.Skin >= 0 && node.Skin < asset.Skins.Count ? asset.Skins[node.Skin] : null;
            foreach (var prim in asset.Meshes[node.Mesh].Primitives)
            {
                int count = prim.Positions.Length;
                var g = new Gathered
                {
                    Prim = prim,
                    Material = prim.Material >= 0 && prim.Material < asset.Materials.Count ? asset.Materials[prim.Material] : null,
                    Bones = new int[count * 4], Weights = new float[count * 4], World = new Vector3[count]
                };
                bool skinned = skin != null && prim.Joints != null && prim.Weights != null && skin.Joints.Length > 0;
                for (int v = 0; v < count; v++)
                {
                    if (!skinned)
                    {
                        g.Bones[v * 4] = Rigid(n);
                        g.Weights[v * 4] = 1f;
                        g.World[v] = Vector3.Transform(prim.Positions[v], rest[n]);
                        continue;
                    }
                    float sum = 0f;
                    for (int k = 0; k < 4; k++) sum += Math.Max(0f, prim.Weights![v * 4 + k]);
                    var world = Vector3.Zero;
                    for (int k = 0; k < 4; k++)
                    {
                        float w = sum > 1e-6f ? Math.Max(0f, prim.Weights![v * 4 + k]) / sum : (k == 0 ? 1f : 0f);
                        int slot = Math.Clamp(prim.Joints![v * 4 + k], 0, skin!.Joints.Length - 1);
                        if (w <= 0f) continue;
                        if (!jointBone.TryGetValue((node.Skin, slot), out int b))
                        {
                            b = bones.Count;
                            jointBone[(node.Skin, slot)] = b;
                            bones.Add((skin.Joints[slot], skin.InverseBind[slot]));
                        }
                        g.Bones[v * 4 + k] = b;
                        g.Weights[v * 4 + k] = w;
                        world += w * Vector3.Transform(prim.Positions[v], skin.InverseBind[slot] * rest[skin.Joints[slot]]);
                    }
                    g.World[v] = world;
                }
                result.Add(g);
            }
        }
        rig.Bones = bones.ToArray();
        return result;
    }

    // ------------------------------------------------------------------ cutting into parts

    private sealed class PartBuilder
    {
        public readonly List<Vector3> Positions = new(), Normals = new();
        public readonly List<Color> Colors = new();
        public readonly List<byte> Materials = new(), BoneIndices = new();
        public readonly List<float> Weights = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<int> Indices = new();
        public readonly List<int> Palette = new();
        public readonly Dictionary<int, int> PaletteIndex = new();
        public readonly Dictionary<int, int> VertexMap = new();
    }

    /// <summary>
    /// Cuts every primitive into parts that each use at most <see cref="SkinnedModel.MaxBones"/> bones and 65,535
    /// vertices: a model with a hundred bones becomes several draws, each with its own palette of bone matrices.
    /// </summary>
    private static void BuildParts(GltfAsset asset, List<Gathered> gathered, ImportedRig rig, Matrix4x4 yaw)
    {
        foreach (var g in gathered)
        {
            var prim = g.Prim;
            var mat = g.Material;
            var normals = prim.Normals ?? FaceNormals(prim);
            var tint = mat?.BaseColor ?? Vector4.One;
            byte surface = (byte)(mat != null && Math.Max(mat.Emissive.X, Math.Max(mat.Emissive.Y, mat.Emissive.Z)) > 0.4f ? SurfaceMaterial.Glow
                : mat != null && mat.Metallic >= 0.7f ? SurfaceMaterial.Metal : SurfaceMaterial.Default);
            int image = mat?.Image ?? -1;
            if (prim.UVs == null) image = -1;
            var part = new PartBuilder();

            void Finish()
            {
                if (part.Indices.Count == 0) return;
                int n = part.Positions.Count;
                rig.Parts.Add(new ImportedPart
                {
                    Mesh = new SdfMesh
                    {
                        Positions = part.Positions.ToArray(), Normals = part.Normals.ToArray(), Colors = part.Colors.ToArray(), Materials = part.Materials.ToArray(),
                        BoneIndices = part.BoneIndices.ToArray(), BoneWeights = part.Weights.ToArray(), Indices = part.Indices.ToArray()
                    },
                    UVs = part.UVs.Count == n ? part.UVs.ToArray() : new Vector2[n],
                    Palette = part.Palette.ToArray(),
                    Image = image
                });
                part = new PartBuilder();
            }

            int Vertex(int v)
            {
                if (part.VertexMap.TryGetValue(v, out int i)) return i;
                i = part.Positions.Count;
                part.VertexMap[v] = i;
                part.Positions.Add(Vector3.Transform(prim.Positions[v], rig.Normalize));
                part.Normals.Add(SafeNormal(Vector3.TransformNormal(normals[v], yaw)));
                var c = tint * (prim.Colors?[v] ?? Vector4.One);
                part.Colors.Add(new Color(Srgb(c.X), Srgb(c.Y), Srgb(c.Z), 255));
                part.Materials.Add(surface);
                if (prim.UVs != null) part.UVs.Add(Transform(prim.UVs[v], mat));
                for (int k = 0; k < 4; k++)
                {
                    float w = g.Weights[v * 4 + k];
                    part.BoneIndices.Add(w > 0f ? (byte)part.PaletteIndex[g.Bones[v * 4 + k]] : (byte)0);
                    part.Weights.Add(w);
                }
                return i;
            }

            var needed = new List<int>(12);
            for (int t = 0; t + 2 < prim.Indices.Length; t += 3)
            {
                needed.Clear();
                for (int c = 0; c < 3; c++)
                {
                    int v = prim.Indices[t + c];
                    for (int k = 0; k < 4; k++)
                    {
                        int b = g.Bones[v * 4 + k];
                        if (g.Weights[v * 4 + k] > 0f && !part.PaletteIndex.ContainsKey(b) && !needed.Contains(b)) needed.Add(b);
                    }
                }
                if (part.Palette.Count + needed.Count > SkinnedModel.MaxBones || part.Positions.Count + 3 > ushort.MaxValue)
                {
                    Finish();
                    t -= 3;
                    continue;
                }
                foreach (int b in needed)
                {
                    part.PaletteIndex[b] = part.Palette.Count;
                    part.Palette.Add(b);
                }
                for (int c = 0; c < 3; c++) part.Indices.Add(Vertex(prim.Indices[t + c]));
            }
            Finish();
        }
    }

    private static Vector3 SafeNormal(Vector3 n) => n.LengthSquared() > 1e-12f ? Vector3.Normalize(n) : Vector3.UnitY;

    private static int Srgb(float linear) => (int)MathF.Round(MathF.Pow(Math.Clamp(linear, 0f, 1f), 1f / 2.2f) * 255f);

    /// <summary>A texture coordinate through the material's texture transform (offset, rotation, scale).</summary>
    private static Vector2 Transform(Vector2 uv, GltfMaterial? mat)
    {
        if (mat == null || (mat.UvOffset == Vector2.Zero && mat.UvScale == Vector2.One && mat.UvRotation == 0f)) return uv;
        var s = uv * mat.UvScale;
        float c = MathF.Cos(mat.UvRotation), n = MathF.Sin(mat.UvRotation);
        return new Vector2(c * s.X + n * s.Y, -n * s.X + c * s.Y) + mat.UvOffset;
    }

    /// <summary>Smooth normals from the faces, for meshes that come without.</summary>
    private static Vector3[] FaceNormals(GltfPrimitive prim)
    {
        var normals = new Vector3[prim.Positions.Length];
        for (int t = 0; t + 2 < prim.Indices.Length; t += 3)
        {
            int a = prim.Indices[t], b = prim.Indices[t + 1], c = prim.Indices[t + 2];
            var n = Vector3.Cross(prim.Positions[b] - prim.Positions[a], prim.Positions[c] - prim.Positions[a]);
            normals[a] += n;
            normals[b] += n;
            normals[c] += n;
        }
        for (int i = 0; i < normals.Length; i++) normals[i] = SafeNormal(normals[i]);
        return normals;
    }

    /// <summary>
    /// The model as one mesh in its default pose, in model space: what the sprite framing, the battle's sizing and
    /// the tests measure. Its height is the model's.
    /// </summary>
    private static void Measure(PokeModel m, List<Gathered> gathered, ImportedRig rig)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        foreach (var g in gathered)
        {
            int start = positions.Count;
            foreach (var p in g.World) positions.Add(Vector3.Transform(p, rig.Normalize));
            foreach (int i in g.Prim.Indices) indices.Add(start + i);
        }
        int n = positions.Count;
        m.Mesh = new SdfMesh
        {
            Positions = positions.ToArray(), Normals = new Vector3[n], Colors = new Color[n], Materials = new byte[n],
            BoneIndices = new byte[n * 4], BoneWeights = new float[n * 4], Indices = indices.ToArray()
        };
        for (int v = 0; v < n; v++) m.Mesh.BoneWeights[v * 4] = 1f;
        float top = 0f;
        foreach (var p in positions) top = Math.Max(top, p.Y);
        m.Height = Math.Max(0.1f, top);
    }

    /// <summary>
    /// A part pushed out along its normals, smoothed across seams first so the shell doesn't split where the mesh
    /// has hard edges or texture seams: drawn with its front faces culled, it rings the silhouette.
    /// </summary>
    private static SdfMesh Shell(ImportedPart part, float width)
    {
        var mesh = part.Mesh;
        int n = mesh.VertexCount;
        if (n == 0) return new SdfMesh();
        var smooth = new Dictionary<(int, int, int), Vector3>();
        (int, int, int) Cell(Vector3 p) => ((int)MathF.Round(p.X * 4000f), (int)MathF.Round(p.Y * 4000f), (int)MathF.Round(p.Z * 4000f));
        for (int v = 0; v < n; v++)
        {
            var key = Cell(mesh.Positions[v]);
            smooth[key] = smooth.TryGetValue(key, out var sum) ? sum + mesh.Normals[v] : mesh.Normals[v];
        }
        var shell = new SdfMesh
        {
            Positions = new Vector3[n], Normals = (Vector3[])mesh.Normals.Clone(), Colors = (Color[])mesh.Colors.Clone(), Materials = (byte[])mesh.Materials.Clone(),
            BoneIndices = (byte[])mesh.BoneIndices.Clone(), BoneWeights = (float[])mesh.BoneWeights.Clone(), Indices = (int[])mesh.Indices.Clone()
        };
        for (int v = 0; v < n; v++) shell.Positions[v] = mesh.Positions[v] + SafeNormal(smooth[Cell(mesh.Positions[v])]) * width;
        return shell;
    }

    // ------------------------------------------------------------------ images

    /// <summary>Textures are kept at most this many pixels across: a model in battle or a 128-px sprite shows no more.</summary>
    public const int MaxTexture = 1024;

    /// <summary>A base colour image decoded on the CPU and halved until it fits <see cref="MaxTexture"/>, or null.</summary>
    private static PixelCanvas? Decode(GltfImage image)
    {
        var canvas = DecodeFull(image);
        while (canvas != null && Math.Max(canvas.Width, canvas.Height) > MaxTexture) canvas = Half(canvas);
        return canvas;
    }

    /// <summary>The image at half size, each pixel the average of four.</summary>
    private static PixelCanvas Half(PixelCanvas c)
    {
        int w = Math.Max(1, c.Width / 2), h = Math.Max(1, c.Height / 2);
        var half = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (int k = 0; k < 4; k++)
                {
                    var p = c.Get(Math.Min(c.Width - 1, x * 2 + (k & 1)), Math.Min(c.Height - 1, y * 2 + (k >> 1)));
                    r += p.R;
                    g += p.G;
                    b += p.B;
                    a += p.A;
                }
                half.SetRaw(x, y, new Color(r / 4, g / 4, b / 4, a / 4));
            }
        return half;
    }

    /// <summary>A base colour image decoded on the CPU (PNG through raylib, JPEG with <see cref="JpegDecoder"/>), or null.</summary>
    private static PixelCanvas? DecodeFull(GltfImage image)
    {
        if (image.Data.Length == 0) return null;
        string type = image.MimeType switch
        {
            "image/png" => ".png", "image/jpeg" => ".jpg", "image/bmp" => ".bmp", "image/gif" => ".gif",
            _ => image.Data.Length > 4 && image.Data[0] == 0x89 && image.Data[1] == (byte)'P' ? ".png"
                : image.Data.Length > 3 && image.Data[0] == 0xFF && image.Data[1] == 0xD8 ? ".jpg" : ""
        };
        if (type.Length == 0)
        {
            Report($"an image of type '{image.MimeType}' can't be read; save the model's textures as PNG or JPEG");
            return null;
        }
        if (type == ".jpg")
        {
            // The raylib build reads PNG only: JPEG has a decoder of its own
            try
            {
                return JpegDecoder.Decode(image.Data);
            }
            catch (Exception e) when (e is InvalidDataException or NotSupportedException or IndexOutOfRangeException or ArgumentException)
            {
                Report($"a JPEG texture couldn't be decoded ({e.Message})");
                return null;
            }
        }
        var img = Raylib.LoadImageFromMemory(type, image.Data);
        unsafe
        {
            if (img.Data == null || img.Width <= 0 || img.Height <= 0)
            {
                Report($"an image ({type}) couldn't be decoded; save the model's textures as PNG");
                return null;
            }
        }
        Raylib.ImageFormat(ref img, PixelFormat.UncompressedR8G8B8A8);
        var canvas = PixelCanvas.FromImage(img);
        Raylib.UnloadImage(img);
        return canvas;
    }

    // ------------------------------------------------------------------ clips

    private static readonly (ClipRole Role, string[] Words)[] ClipWords =
    {
        (ClipRole.Idle, new[] { "battlewait", "battle_wait", "idle", "wait", "stand", "breath", "loop" }),
        (ClipRole.Physical, new[] { "attack01", "attack1", "attack_1", "physical", "tackle", "bite", "strike", "punch", "attack" }),
        (ClipRole.Special, new[] { "attack02", "attack2", "attack_2", "special", "cast", "shoot", "beam", "attack" }),
        (ClipRole.Status, new[] { "status", "buff", "attack03", "attack3", "happy", "attack" }),
        (ClipRole.Hit, new[] { "damage", "hurt", "flinch", "hit" }),
        (ClipRole.Faint, new[] { "down", "faint", "death", "die", "dead", "lose" }),
        (ClipRole.Entry, new[] { "appear", "entry", "intro", "landing", "roar", "cry" })
    };

    /// <summary>
    /// Which clip plays for what: named in the model's options, else the clip whose name has the strongest word for the
    /// role ("battlewait" before "wait"). A file with a single clip plays it as its idle.
    /// </summary>
    internal static Dictionary<ClipRole, int> MatchClips(IReadOnlyList<string> names, Dictionary<string, string>? chosen = null)
    {
        var result = new Dictionary<ClipRole, int>();
        foreach (var (role, words) in ClipWords)
        {
            if (chosen != null && chosen.FirstOrDefault(c => c.Key.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase)).Value is { } wanted)
            {
                int named = names.ToList().FindIndex(n => n.Equals(wanted, StringComparison.OrdinalIgnoreCase));
                if (named >= 0)
                {
                    result[role] = named;
                    continue;
                }
            }
            int best = -1, bestScore = 0;
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i].ToLowerInvariant();
                for (int w = 0; w < words.Length; w++)
                {
                    int score = words.Length - w;
                    if (!name.Contains(words[w]) || score < bestScore || (score == bestScore && best >= 0 && names[best].Length <= names[i].Length)) continue;
                    best = i;
                    bestScore = score;
                }
            }
            if (best >= 0) result[role] = best;
        }
        if (names.Count == 1 && !result.ContainsKey(ClipRole.Idle)) result[ClipRole.Idle] = 0;
        return result;
    }

    // ------------------------------------------------------------------ posing

    /// <summary>
    /// Poses an imported model: the clip the pose calls for (the file's own when it has one for it) moves its nodes,
    /// and the body plan's motion for the rest moves it whole. Fills <see cref="PokeModel.Skin"/>, one matrix per bone.
    /// </summary>
    public static void Animate(PokeModel m, PokePose pose)
    {
        var rig = m.Imported!;
        var own = pose;
        if (rig.Has(ClipRole.Faint)) own.Faint = 0f;
        if (rig.Has(ClipRole.Hit)) own.Hurt = 0f;
        if (rig.Has(ClipRole.Entry)) own.Entry = 0f;
        if (rig.Has(Action(pose.Kind))) own.Attack = 0f;
        PokemonAnimation.Apply(m, own, m.Pose);
        m.Skeleton.Evaluate(m.Pose, rig.BodySkin);
        var body = rig.BodySkin[1];

        var (clip, time) = Choose(rig, pose, m.Tempo);
        PoseNodes(rig, clip, time);
        for (int b = 0; b < rig.Bones.Length; b++)
        {
            var (node, offset) = rig.Bones[b];
            m.Skin[b] = rig.Denormalize * offset * rig.Globals[node] * rig.Normalize * body;
        }
    }

    private static ClipRole Action(Data.MoveCategory kind) => kind switch
    {
        Data.MoveCategory.Physical => ClipRole.Physical,
        Data.MoveCategory.Special => ClipRole.Special,
        _ => ClipRole.Status
    };

    /// <summary>The clip to play and the time in it: actions in order of urgency, stretched over their length; else the idle loop.</summary>
    private static (int Clip, float Time) Choose(ImportedRig rig, PokePose p, float tempo)
    {
        float Length(int clip) => Math.Max(1e-3f, rig.Asset.Animations[clip].Duration);
        if (p.Faint > 0f && rig.Clips.TryGetValue(ClipRole.Faint, out int c)) return (c, Math.Min(1f, p.Faint) * Length(c));
        if (p.Hurt > 0f && p.Hurt < 1f && rig.Clips.TryGetValue(ClipRole.Hit, out c)) return (c, p.Hurt * Length(c));
        if (p.Attack > 0f && p.Attack < 1f && rig.Clips.TryGetValue(Action(p.Kind), out c)) return (c, p.Attack * Length(c));
        if (p.Entry > 0f && p.Entry < 1f && rig.Clips.TryGetValue(ClipRole.Entry, out c)) return (c, p.Entry * Length(c));
        if (rig.Clips.TryGetValue(ClipRole.Idle, out c))
        {
            float length = Length(c);
            float t = p.Time * tempo % length;
            return (c, t < 0f ? t + length : t);
        }
        return (-1, 0f);
    }

    /// <summary>Every node's transform at <paramref name="time"/> into a clip (-1: the default pose), into the rig's globals.</summary>
    internal static void PoseNodes(ImportedRig rig, int clip, float time)
    {
        var asset = rig.Asset;
        for (int n = 0; n < asset.Nodes.Count; n++)
        {
            var node = asset.Nodes[n];
            rig.T[n] = node.Translation;
            rig.R[n] = node.Rotation;
            rig.S[n] = node.Scale;
        }
        if (clip >= 0 && clip < asset.Animations.Count)
        {
            Span<float> value = stackalloc float[4];
            foreach (var ch in asset.Animations[clip].Channels)
            {
                Sample(ch, time, value);
                switch (ch.Path)
                {
                    case GltfPath.Translation: rig.T[ch.Node] = new Vector3(value[0], value[1], value[2]); break;
                    case GltfPath.Scale: rig.S[ch.Node] = new Vector3(value[0], value[1], value[2]); break;
                    case GltfPath.Rotation: rig.R[ch.Node] = Quaternion.Normalize(new Quaternion(value[0], value[1], value[2], value[3])); break;
                }
            }
        }
        foreach (int n in asset.Order)
        {
            var node = asset.Nodes[n];
            var local = node.Matrix ?? GltfNode.Compose(rig.T[n], rig.R[n], rig.S[n]);
            rig.Globals[n] = node.Parent < 0 ? local : local * rig.Globals[node.Parent];
        }
    }

    /// <summary>A channel's value at <paramref name="t"/>: held before the first key and after the last, interpolated between.</summary>
    internal static void Sample(GltfChannel ch, float t, Span<float> value)
    {
        var times = ch.Times;
        int w = ch.Width;
        bool cubic = ch.Interpolation == GltfInterpolation.CubicSpline;
        int Key(int k) => cubic ? (k * 3 + 1) * w : k * w;
        int last = times.Length - 1;
        if (t <= times[0] || last == 0)
        {
            ch.Values.AsSpan(Key(0), w).CopyTo(value);
            return;
        }
        if (t >= times[last])
        {
            ch.Values.AsSpan(Key(last), w).CopyTo(value);
            return;
        }
        int lo = 0, hi = last;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (times[mid] <= t) lo = mid;
            else hi = mid;
        }
        if (ch.Interpolation == GltfInterpolation.Step)
        {
            ch.Values.AsSpan(Key(lo), w).CopyTo(value);
            return;
        }
        float f = (t - times[lo]) / Math.Max(1e-6f, times[hi] - times[lo]);
        int a = Key(lo), b = Key(hi);
        if (ch.Path == GltfPath.Rotation && w == 4)
        {
            var qa = new Quaternion(ch.Values[a], ch.Values[a + 1], ch.Values[a + 2], ch.Values[a + 3]);
            var qb = new Quaternion(ch.Values[b], ch.Values[b + 1], ch.Values[b + 2], ch.Values[b + 3]);
            var q = Quaternion.Slerp(qa, qb, f);
            value[0] = q.X;
            value[1] = q.Y;
            value[2] = q.Z;
            value[3] = q.W;
            return;
        }
        for (int k = 0; k < w && k < value.Length; k++) value[k] = ch.Values[a + k] + (ch.Values[b + k] - ch.Values[a + k]) * f;
    }

    // ------------------------------------------------------------------ on the GPU

    /// <summary>Puts the parts, their shells and the textures on the GPU (main thread).</summary>
    public static void Upload(PokeModel m, FieldShaders shaders)
    {
        var rig = m.Imported!;
        rig.Materials = new Material?[rig.Images.Length];
        foreach (var part in rig.Parts)
        {
            part.Body = SkinnedModel.Upload(part.Mesh, part.Palette.Length, part.UVs);
            if (part.Shell.VertexCount > 0) part.Outline = SkinnedModel.Upload(part.Shell, part.Palette.Length);
            if (part.Image >= 0 && part.Image < rig.Images.Length && rig.Images[part.Image] is { } canvas && rig.Materials[part.Image] == null)
            {
                var tex = canvas.ToTexture();
                Raylib.GenTextureMipmaps(ref tex);
                Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
                Raylib.SetTextureWrap(tex, TextureWrap.Repeat);
                rig.Materials[part.Image] = RenderContext.MaterialFor(shaders.CharacterSkinned, tex);
            }
        }
    }

    public static unsafe void Unload(PokeModel m)
    {
        var rig = m.Imported!;
        foreach (var part in rig.Parts)
        {
            part.Body?.Unload();
            part.Outline?.Unload();
            part.Body = part.Outline = null;
        }
        foreach (var material in rig.Materials)
        {
            if (material is not { } mat) continue;
            Raylib.UnloadTexture(mat.Maps[(int)MaterialMapIndex.Albedo].Texture);
            Raylib.MemFree(mat.Maps);
        }
        rig.Materials = new Material?[rig.Images.Length];
    }

    /// <summary>Draws the posed parts for one pass (the skin is already posed by <see cref="PokeModel.Animate"/>).</summary>
    public static void Draw(RenderContext context, PokeModel m, Matrix4x4 root, CharacterPass pass)
    {
        var rig = m.Imported!;
        foreach (var part in rig.Parts)
        {
            var model = pass == CharacterPass.Outline ? part.Outline : part.Body;
            if (model == null) continue;
            for (int i = 0; i < part.Palette.Length; i++) rig.Palette[i] = m.Skin[part.Palette[i]];
            var skin = rig.Palette.AsSpan(0, part.Palette.Length);
            var material = pass switch
            {
                CharacterPass.Depth => context.DepthSkinned,
                CharacterPass.Outline => context.OutlineSkinned,
                _ => part.Image >= 0 && part.Image < rig.Materials.Length && rig.Materials[part.Image] is { } textured ? textured : context.ToonSkinned
            };
            model.Draw(material, skin, root);
        }
    }
}
