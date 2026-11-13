using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// One piece of a character's file (style guide, "Characters from files"): a mesh on the build's skeleton, named
/// <c>&lt;slot&gt;.&lt;Shape&gt;</c>, which either carries its own colours or zones a look's colours fill.
/// </summary>
internal sealed class CharacterPiece
{
    public string Name = "";
    public SdfMesh Mesh = new();

    /// <summary>The vertex colours as the file has them, linear: zones' shares, or the piece's own colours.</summary>
    public Vector3[] Raw = Array.Empty<Vector3>();

    /// <summary>Whether the colours are zones (<see cref="CharacterFiles.Fill"/>) or the piece's own.</summary>
    public bool Zones;

    /// <summary>Whose colours fill the zones (<see cref="CharacterFiles.Palette"/>).</summary>
    public string Tint = "";

    /// <summary>The body's parts this piece covers, which are left out when it is worn.</summary>
    public string[] Hides = Array.Empty<string>();
}

/// <summary>The pieces of one build's file, by name.</summary>
internal sealed class CharacterFile
{
    public string Path = "";
    public readonly Dictionary<string, CharacterPiece> Pieces = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Characters read from files made in Blender (plan 47, route b; style guide, "Characters from files"): a file a build
/// (<c>Kid.glb</c>, <c>Grown-curved.glb</c>) in <c>overrides/characters</c> or <c>assets/characters</c>, holding the
/// body in parts, every hair cut, hat, garment, thing held and the face's pieces for each expression, all skinned to
/// the engine's own skeleton by bone name. A look is assembled from the pieces its <see cref="CharacterStyle"/> asks
/// for; where the file lacks one of them the look is sculpted as before (<see cref="CharacterModels"/>).
/// Reading and assembling run without GPU calls.
/// </summary>
internal static class CharacterFiles
{
    /// <summary>Off, every look is sculpted by the kits as before (safe mode turns it off too).</summary>
    public static bool Enabled { get; set; } = true;

    public static readonly List<string> Folders = new()
    {
        Path.Combine(AppContext.BaseDirectory, "overrides", "characters"),
        Path.Combine(Environment.CurrentDirectory, "overrides", "characters"),
        Path.Combine(Environment.CurrentDirectory, "assets", "characters"),
        // A release's own: built by its workflow with Blender and shipped beside the executable, under the manifest
        Path.Combine(AppContext.BaseDirectory, "Assets", "characters")
    };

    private static readonly Dictionary<string, CharacterFile?> Loaded = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private static readonly Color Ink = new(34, 26, 40, 255);

    /// <summary>
    /// Has the game look in the repository's own <c>assets/characters</c> too when its executable is inside the
    /// repository (started from Visual Studio or by <c>dotnet run</c> from another folder, where the folder it is
    /// started in is not the repository's root). Only the game itself asks (<c>Program.Main</c>): a test's or a tool's
    /// engine reads no file it wasn't handed, so files a developer built never reach a test or a shot.
    /// </summary>
    public static void LookInTheRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) continue;
            Folders.Add(Path.Combine(dir.FullName, "assets", "characters"));
            Refresh();
            return;
        }
    }

    /// <summary>Forgets the files read (after files were added or removed).</summary>
    public static void Refresh()
    {
        lock (Gate) Loaded.Clear();
    }

    /// <summary>A build's file name: the build, and <c>-curved</c> for a grown figure with a waist and a bust.</summary>
    public static string NameOf(BodyBuild build, bool curved) => curved ? $"{build}-curved" : build.ToString();

    /// <summary>The file of a build on a skeleton, or null where there is none (or files are off).</summary>
    public static CharacterFile? For(BodyBuild build, bool curved, Skeleton skeleton)
    {
        if (!Enabled || !Overrides.Enabled) return null;
        string name = NameOf(build, curved);
        lock (Gate)
        {
            if (Loaded.TryGetValue(name, out var file)) return file;
            string? path = Folders.Distinct().Select(f => Path.Combine(f, name + ".glb")).FirstOrDefault(File.Exists);
            try
            {
                file = path == null ? null : Read(path, skeleton);
                // Said once a build, so the log shows whether people come from the files or from the sculpting kits
                Core.Diagnostics.Log.Info(file == null
                    ? $"People of the {name} build are sculpted by the kit: no {name}.glb in {string.Join(", ", Folders.Distinct())}."
                    : $"People of the {name} build are made from {path} ({file.Pieces.Count} pieces).");
            }
            catch (Exception e) when (e is IOException or InvalidDataException or FormatException or KeyNotFoundException or System.Text.Json.JsonException)
            {
                Core.Diagnostics.Log.Warn($"The character file {path} can't be read: {e.Message}");
                file = null;
            }
            return Loaded[name] = file;
        }
    }

    // ------------------------------------------------------------------ reading

    /// <summary>Reads a file's pieces, each vertex's bones found on <paramref name="skeleton"/> by name.</summary>
    public static CharacterFile Read(string path, Skeleton skeleton) => Read(GltfReader.Read(path), skeleton, path);

    public static CharacterFile Read(GltfAsset asset, Skeleton skeleton, string path = "")
    {
        var file = new CharacterFile { Path = path };
        var globals = asset.RestGlobals();
        for (int n = 0; n < asset.Nodes.Count; n++)
        {
            var node = asset.Nodes[n];
            if (node.Mesh < 0 || !node.Name.Contains('.')) continue;
            int[] joints = node.Skin >= 0 ? asset.Skins[node.Skin].Joints : Array.Empty<int>();
            var bones = joints.Select(j => skeleton.Find(asset.Nodes[j].Name)).ToArray();
            var piece = new CharacterPiece
            {
                Name = node.Name,
                Zones = !node.Extras.TryGetValue("colours", out var colours) || !colours.Equals("true", StringComparison.OrdinalIgnoreCase),
                Tint = node.Extras.GetValueOrDefault("tint", ""),
                Hides = node.Extras.GetValueOrDefault("hides", "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            };
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var raw = new List<Vector3>();
            var boneIndices = new List<byte>();
            var weights = new List<float>();
            var indices = new List<int>();
            // A skinned mesh's points are in the skeleton's space already; a plain one's are its node's
            var place = node.Skin >= 0 ? Matrix4x4.Identity : globals[n];
            foreach (var prim in asset.Meshes[node.Mesh].Primitives)
            {
                int first = positions.Count;
                var own = prim.Normals;
                for (int v = 0; v < prim.Positions.Length; v++)
                {
                    positions.Add(Vector3.Transform(prim.Positions[v], place));
                    var nrm = own != null ? Vector3.TransformNormal(own[v], place) : Vector3.UnitY;
                    normals.Add(nrm.LengthSquared() > 1e-12f ? Vector3.Normalize(nrm) : Vector3.UnitY);
                    var c = prim.Colors?[v] ?? Vector4.One;
                    raw.Add(new Vector3(c.X, c.Y, c.Z));
                    AddWeights(prim, v, bones, boneIndices, weights);
                }
                foreach (int i in prim.Indices) indices.Add(first + i);
            }
            if (positions.Count > ushort.MaxValue) throw new InvalidDataException($"{node.Name} has {positions.Count} vertices; a piece holds at most {ushort.MaxValue}");
            piece.Mesh = new SdfMesh
            {
                Positions = positions.ToArray(), Normals = normals.ToArray(), Colors = new Color[positions.Count], Materials = new byte[positions.Count],
                BoneIndices = boneIndices.ToArray(), BoneWeights = weights.ToArray(), Indices = indices.ToArray()
            };
            piece.Raw = raw.ToArray();
            file.Pieces[piece.Name] = piece;
        }
        return file;
    }

    private static void AddWeights(GltfPrimitive prim, int v, int[] bones, List<byte> boneIndices, List<float> weights)
    {
        Span<int> b = stackalloc int[4];
        Span<float> w = stackalloc float[4];
        float sum = 0f;
        for (int k = 0; k < 4; k++)
        {
            float weight = prim.Weights != null && prim.Joints != null ? prim.Weights[v * 4 + k] : 0f;
            int joint = prim.Joints != null ? prim.Joints[v * 4 + k] : 0;
            int bone = weight > 0f && joint < bones.Length ? bones[joint] : -1;
            // A bone the skeleton doesn't have gives its share to the others
            if (bone < 0) weight = 0f;
            (b[k], w[k]) = (Math.Max(bone, 0), weight);
            sum += weight;
        }
        for (int k = 0; k < 4; k++)
        {
            boneIndices.Add((byte)b[k]);
            weights.Add(sum > 0f ? w[k] / sum : k == 0 ? 1f : 0f);
        }
    }

    // ------------------------------------------------------------------ what a look wears

    private const string BagSlot = "bag";

    /// <summary>The body's parts, each a piece <c>body.&lt;part&gt;</c> that a garment's <c>hides</c> may name.</summary>
    public static readonly string[] BodyParts = { "head", "torso", "arms", "hands", "legs", "feet" };

    /// <summary>The face's pieces that change: the eyes, the brows and the mouth for an expression, a blink and a syllable.</summary>
    public static IEnumerable<string> FacePieces(Expression expression, bool blink, bool mouthOpen, bool lashes = false)
    {
        string eyes = blink ? "shut" : expression == Expression.Surprised ? "wide" : "open";
        yield return "face.eyes." + eyes;
        if (lashes) yield return "face.lashes." + eyes;
        yield return "face.brows." + expression;
        yield return mouthOpen ? "face.mouth.open" : "face.mouth." + expression;
    }

    /// <summary>Every piece of a changing face, which a file must hold for a look to be made from it.</summary>
    public static IEnumerable<string> EveryFacePiece() =>
        Enum.GetValues<Expression>().SelectMany(e => new[] { false, true }.SelectMany(b => new[] { false, true }.SelectMany(o => FacePieces(e, b, o, true)))).Distinct();

    /// <summary>Whether a hat covers the crown: the hair under it is the cut's <c>.hat</c> piece, with no locks over the top.</summary>
    public static bool CoversCrown(Headwear hat) => hat is not (Headwear.None or Headwear.Bow or Headwear.NurseCap or Headwear.Visor or Headwear.Headband);

    /// <summary>The top a look wears, by its flags in the order the kits read them.</summary>
    public static string TopOf(CharacterStyle s) =>
        s.Swimsuit ? "Swimsuit" : s.Robe ? "Robe" : s.Coat ? "Coat" : s.Jacket ? "Jacket" : s.ShortSleeves ? "Tee" : "Shirt";

    /// <summary>What it wears below.</summary>
    public static string BottomsOf(CharacterStyle s) => s.Swimsuit ? "Swim" : s.Skirt ? "Skirt" : s.Shorts ? "Shorts" : "Trousers";

    /// <summary>
    /// The pieces a look is made of but its changing face, in three layers: the body with its hair and what grows on
    /// the face, the clothes, and what is held. A name ending in <c>?</c> is worn if the file has it.
    /// </summary>
    public static (List<string> Body, List<string> Clothes, List<string> Held) Wanted(CharacterStyle s)
    {
        var body = BodyParts.Select(p => "body." + p).ToList();
        body.Add("hair." + s.Hair + (CoversCrown(s.Hat) ? ".hat" : ""));
        if (s.Mustache) body.Add("face.Mustache");
        if (s.Beard) body.Add("face.Beard");
        if (s.BushyBrows) body.Add("face.BushyBrows");
        if (s.Blush) body.Add("face.Blush");

        var clothes = new List<string>();
        if (s.Hat != Headwear.None) clothes.Add("hat." + s.Hat);
        string top = TopOf(s);
        clothes.Add("top." + top);
        if (s.Coat && s.Buttoned && !s.Robe && !s.Swimsuit) clothes.Add("extra.Buttons");
        clothes.Add("bottoms." + BottomsOf(s));
        clothes.Add("shoes.Shoes");
        if (!s.Swimsuit)
        {
            clothes.Add(s.Scarf ? "extra.Scarf" : "extra.Collar?");
            if (s.Overalls) clothes.Add("extra.Overalls");
            if (s.Apron != null) clothes.Add("extra.Apron");
            if (s.Vest != null) clothes.Add("extra.Vest");
            if (s.GalacticMark) clothes.Add("extra.GalacticMark");
        }
        // Written apart: "bag" is also a section of the words on screen, whose keys a test looks for in quotes
        if (s.Bag != null) clothes.Add(BagSlot + ".Bag");
        if (s.Glasses != null) clothes.Add("face.Glasses");

        var held = new List<string>();
        if (s.Holds != HeldProp.None) held.Add("held." + s.Holds);
        return (body, clothes, held);
    }

    /// <summary>
    /// A piece by the name a look asks for it by: a cut worn under a hat (<c>hair.Afro.hat</c>) is the plain cut where
    /// the file has no piece of its own for it.
    /// </summary>
    public static CharacterPiece? Find(CharacterFile file, string name)
    {
        name = name.TrimEnd('?');
        if (file.Pieces.TryGetValue(name, out var piece)) return piece;
        const string Hatted = ".hat";
        return name.EndsWith(Hatted, StringComparison.OrdinalIgnoreCase) ? file.Pieces.GetValueOrDefault(name[..^Hatted.Length]) : null;
    }

    /// <summary>The pieces a look needs that the file lacks (none: the look can be made from it).</summary>
    public static List<string> Missing(CharacterFile file, CharacterStyle s)
    {
        var (body, clothes, held) = Wanted(s);
        return body.Concat(clothes).Concat(held).Concat(EveryFacePiece())
            .Where(name => !name.EndsWith('?') && Find(file, name) == null).ToList();
    }

    // ------------------------------------------------------------------ colours

    /// <summary>
    /// The colours that fill a piece's zones, by its tint: the primary, the accent, the stripe's (the accent where the
    /// look has stripes, else the primary) and a third.
    /// </summary>
    public static (Color Primary, Color Accent, Color Stripe, Color Third) Palette(string tint, CharacterStyle s)
    {
        static Color Shade(Color c, float f) => MeshBuilder.Scale(c, f);
        Color top = s.Top, accent = s.Accent;
        (Color p, Color a, Color third) = tint.ToLowerInvariant() switch
        {
            "skin" => (s.Skin, Shade(s.Skin, 0.86f), new Color(250, 250, 252, 255)),
            "blush" => (PixelCanvas.Mix(s.Skin, new Color(250, 120, 130, 255), 0.32f), s.Skin, s.Skin),
            "parasol" => (s.PropColor ?? top, accent, Ink),
            "guitar" => (s.PropColor ?? new Color(196, 120, 58, 255), new Color(84, 56, 40, 255), Ink),
            "camera" => (s.PropColor ?? new Color(58, 60, 70, 255), new Color(28, 30, 36, 255), Ink),
            "float" => (s.PropColor ?? new Color(240, 96, 80, 255), new Color(248, 248, 244, 255), Ink),
            "hair" => (s.HairColor, PixelCanvas.Light1(s.HairColor, 0.3f), Shade(s.HairColor, 0.7f)),
            "eyes" => (s.Eyes, PixelCanvas.Light1(s.Eyes, 0.45f), PixelCanvas.Mix(s.Eyes, Ink, 0.8f)),
            "brows" => (PixelCanvas.Mix(s.HairColor, Ink, 0.45f), s.HairColor, Ink),
            "hat" => (s.HatColor, s.HatBand, Shade(s.HatColor, 0.72f)),
            "top" => (top, accent, Shade(top, 0.72f)),
            "lining" => (Shade(top, 0.62f), accent, top),
            "accent" => (accent, Shade(accent, 0.8f), top),
            "bottom" => (s.Bottom, Shade(s.Bottom, 0.78f), accent),
            "shoes" => (s.Shoes, s.Sole ?? Shade(s.Shoes, 0.62f), Shade(s.Shoes, 0.8f)),
            "bag" => (s.Bag ?? s.Accent, Shade(s.Bag ?? s.Accent, 0.72f), accent),
            "apron" => (s.Apron ?? accent, Shade(s.Apron ?? accent, 0.8f), accent),
            "vest" => (s.Vest ?? accent, Shade(s.Vest ?? accent, 0.8f), accent),
            "glasses" => (s.Glasses ?? Ink, new Color(214, 232, 244, 255), Ink),
            "prop" => (s.PropColor ?? new Color(200, 70, 70, 255), new Color(150, 110, 70, 255), new Color(170, 176, 190, 255)),
            _ => (top, accent, Shade(top, 0.72f))
        };
        return (p, a, s.Stripes ? a : p, third);
    }

    /// <summary>The surface a tint's piece is shaded as.</summary>
    public static SurfaceMaterial SurfaceOf(string tint) => tint.ToLowerInvariant() switch
    {
        "skin" or "blush" => SurfaceMaterial.Skin,
        "parasol" => SurfaceMaterial.Cloth,
        "guitar" => SurfaceMaterial.Leather,
        "camera" or "float" => SurfaceMaterial.Plastic,
        "hair" or "brows" => SurfaceMaterial.Hair,
        "shoes" or "bag" => SurfaceMaterial.Leather,
        "glasses" or "prop" => SurfaceMaterial.Plastic,
        "eyes" => SurfaceMaterial.Default,
        "" => SurfaceMaterial.Default,
        _ => SurfaceMaterial.Cloth
    };

    /// <summary>
    /// A zone colour (style guide, "Characters from files", "Zones"): red is the share of the primary colour, green of
    /// the accent, blue of white and what is left of ink; green and blue together past a whole are the stripe's,
    /// red and green together the primary's shade, red and blue the third colour.
    /// </summary>
    public static Color Fill(Vector3 zone, (Color Primary, Color Accent, Color Stripe, Color Third) palette)
    {
        float r = Math.Clamp(zone.X, 0f, 1f), g = Math.Clamp(zone.Y, 0f, 1f), b = Math.Clamp(zone.Z, 0f, 1f);
        float over = MathF.Max(0f, r + g + b - 1f);
        float stripe = 0f, shade = 0f, third = 0f;
        if (over > 0f)
        {
            // The two channels that are full together say which of the paired zones it is: both must hold the
            // share, and where two pairs could (a primary easing into a stripe is also a sum of primary, white and
            // shade) it is the pair whose channels are nearest alike
            const float Slack = 0.02f;
            float best = float.MaxValue;
            int pair = 0;
            void Try(int which, float x, float y)
            {
                if (MathF.Min(x, y) < over - Slack || MathF.Abs(x - y) >= best) return;
                (best, pair) = (MathF.Abs(x - y), which);
            }
            Try(1, g, b); Try(2, r, g); Try(3, r, b);
            if (pair == 1) { stripe = over; g -= over; b -= over; }
            else if (pair == 2) { shade = over; r -= over; g -= over; }
            else if (pair == 3) { third = over; r -= over; b -= over; }
        }
        r = MathF.Max(r, 0f); g = MathF.Max(g, 0f); b = MathF.Max(b, 0f);
        float ink = MathF.Max(0f, 1f - r - g - b - stripe - shade - third);
        var shadeColor = MeshBuilder.Scale(palette.Primary, 0.72f);
        float R = 0, G = 0, B = 0;
        void Add(Color c, float w) { R += c.R * w; G += c.G * w; B += c.B * w; }
        Add(palette.Primary, r); Add(palette.Accent, g); Add(new Color(250, 250, 252, 255), b); Add(Ink, ink);
        Add(palette.Stripe, stripe); Add(shadeColor, shade); Add(palette.Third, third);
        return new Color((byte)Math.Clamp(MathF.Round(R), 0, 255), (byte)Math.Clamp(MathF.Round(G), 0, 255), (byte)Math.Clamp(MathF.Round(B), 0, 255), (byte)255);
    }

    private static byte Srgb(float linear) => (byte)MathF.Round(MathF.Pow(Math.Clamp(linear, 0f, 1f), 1f / 2.2f) * 255f);

    // ------------------------------------------------------------------ assembling

    /// <summary>
    /// The named pieces as one mesh in a look's colours (those the file lacks and those in <paramref name="hidden"/>
    /// left out), or null when it would hold nothing. <paramref name="lift"/> pushes it out along its normals.
    /// </summary>
    public static SdfMesh? Assemble(CharacterFile file, IEnumerable<string> names, CharacterStyle s, ISet<string>? hidden = null, float lift = 0f)
    {
        var pieces = names.Select(n => n.TrimEnd('?')).Where(n => hidden == null || !hidden.Contains(n))
            .Select(n => Find(file, n)).Where(p => p != null && p.Mesh.VertexCount > 0).Select(p => p!).ToList();
        int count = pieces.Sum(p => p.Mesh.VertexCount);
        if (count == 0) return null;
        if (count > ushort.MaxValue) throw new InvalidDataException($"{string.Join(", ", pieces.Select(p => p.Name))}: {count} vertices together; a mesh holds at most {ushort.MaxValue}");
        var mesh = new SdfMesh
        {
            Positions = new Vector3[count], Normals = new Vector3[count], Colors = new Color[count], Materials = new byte[count],
            BoneIndices = new byte[count * 4], BoneWeights = new float[count * 4], Indices = new int[pieces.Sum(p => p.Mesh.Indices.Length)]
        };
        int at = 0, index = 0;
        foreach (var piece in pieces)
        {
            var m = piece.Mesh;
            int n = m.VertexCount;
            var palette = Palette(piece.Tint, s);
            byte surface = (byte)SurfaceOf(piece.Tint);
            for (int v = 0; v < n; v++)
            {
                mesh.Positions[at + v] = m.Positions[v] + m.Normals[v] * lift;
                mesh.Normals[at + v] = m.Normals[v];
                var raw = piece.Raw[v];
                mesh.Colors[at + v] = piece.Zones ? Fill(raw, palette) : new Color(Srgb(raw.X), Srgb(raw.Y), Srgb(raw.Z), (byte)255);
                mesh.Materials[at + v] = surface;
            }
            Array.Copy(m.BoneIndices, 0, mesh.BoneIndices, at * 4, n * 4);
            Array.Copy(m.BoneWeights, 0, mesh.BoneWeights, at * 4, n * 4);
            foreach (int i in m.Indices) mesh.Indices[index++] = at + i;
            at += n;
        }
        return mesh;
    }

    /// <summary>The body's pieces that what a look wears covers.</summary>
    public static HashSet<string> HiddenBy(CharacterFile file, IEnumerable<string> worn)
    {
        var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in worn)
            if (Find(file, name) is { } piece)
                foreach (string part in piece.Hides) hidden.Add(part.Contains('.') ? part : "body." + part);
        return hidden;
    }

    /// <summary>Where a ray from <paramref name="from"/> along <paramref name="dir"/> first meets a mesh, or null.</summary>
    public static Vector3? Hit(SdfMesh mesh, Vector3 from, Vector3 dir)
    {
        float best = float.MaxValue;
        for (int t = 0; t + 2 < mesh.Indices.Length; t += 3)
        {
            Vector3 a = mesh.Positions[mesh.Indices[t]], e1 = mesh.Positions[mesh.Indices[t + 1]] - a, e2 = mesh.Positions[mesh.Indices[t + 2]] - a;
            var h = Vector3.Cross(dir, e2);
            float det = Vector3.Dot(e1, h);
            if (MathF.Abs(det) < 1e-12f) continue;
            var sv = from - a;
            float u = Vector3.Dot(sv, h) / det;
            if (u < 0f || u > 1f) continue;
            var q = Vector3.Cross(sv, e1);
            float w = Vector3.Dot(dir, q) / det;
            if (w < 0f || u + w > 1f) continue;
            float dist = Vector3.Dot(e2, q) / det;
            if (dist > 0f && dist < best) best = dist;
        }
        return best < float.MaxValue ? from + dir * best : null;
    }
}
