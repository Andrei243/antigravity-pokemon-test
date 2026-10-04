using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// How a species stands and moves (plan 04 · G7): which bones its skeleton has and how its animation clips move
/// them. Plan 03's model generator picks one per species.
/// </summary>
internal enum BodyPlan { Biped, Quadruped, Bird, Serpent, Fish, Floating }

/// <summary>What a bone of a Pokémon is, which tells the animation clips how to move it.</summary>
internal enum PokeRole { Root, Body, Head, Jaw, Arm, Leg, Wing, Tail, Ear, Leaf, Flame, Fin, Segment }

/// <summary>
/// A bone's part in the animation: its role, its side (-1 for x &lt; 0, 1 for x &gt; 0, 0 on the middle), a phase
/// offset for idle motion (a segment's place along a serpent) and, for legs, whether it is a front leg.
/// </summary>
internal readonly record struct PokeBoneInfo(PokeRole Role, float Side, float Phase, bool Front);

/// <summary>Animation inputs for one frame of a Pokémon. Every action is 0 when it isn't playing.</summary>
internal struct PokePose
{
    public float Time;

    /// <summary>1 while the eyes are shut.</summary>
    public float Blink;

    /// <summary>0..1 progress of a move being used.</summary>
    public float Attack;

    /// <summary>The kind of move being used: physical moves strike, special ones are cast, status moves a hop.</summary>
    public MoveCategory Kind;

    /// <summary>0..1 flinch after taking a hit.</summary>
    public float Hurt;

    /// <summary>0..1 collapse when fainting (stays at 1 once down).</summary>
    public float Faint;

    /// <summary>0..1 arrival out of the Poké Ball.</summary>
    public float Entry;
}

/// <summary>Which eye texture shows (see <see cref="PokemonDecals"/>).</summary>
internal enum EyeState { Open, Shut, Squeeze, Fierce }

internal enum MarkShape { Disc, Ring, Star, Bar }

/// <summary>
/// A texture laid onto the surface (plan 04 · G7): an eye or a marking, projected onto the mesh along its normal.
/// Sculpting them would blur them at the mesh's resolution; a decal keeps them crisp.
/// </summary>
internal sealed class PokeDecal
{
    public int Bone;
    public Vector3 Center;
    public Vector3 Normal;

    /// <summary>
    /// Half the size of the decal's square of texture, in model units, in its own plane. The eye or marking fills
    /// about the middle half of it, so the triangles under the square cover it fully however coarse the mesh.
    /// </summary>
    public Vector2 Half;

    /// <summary>Turn of the decal in its plane (radians).</summary>
    public float Roll;

    public bool IsEye;

    // Eyes: the size of the eye (half its height), iris and pupil colours, white eyes with a pupil (sclera)
    public float Size;
    public Color? Iris;
    public bool Sclera;
    public Color? Pupil;

    // Markings
    public MarkShape Shape;
    public Color Color;

    /// <summary>The surface colour under the decal (found when it is projected), for lids drawn over an eye.</summary>
    public Color Under = Color.White;
}

/// <summary>
/// A Pokémon (plan 04 · G7): one smooth body sculpted with the SDF kit and skinned to a skeleton laid out by its
/// body plan, with its eyes and markings as decals and an offset shell for its outline. The same model is drawn
/// in 3D in battle and baked into the menus' pixel sprites. Model space: feet at the origin, facing +Z.
/// </summary>
internal sealed class PokeModel
{
    public string Species = "";
    public BodyPlan Plan;

    /// <summary>Share of the sprite frame the model fills (small Pokémon look small).</summary>
    public float Fill = 0.7f;

    /// <summary>Flying and floating Pokémon bob in the air instead of standing.</summary>
    public bool Hovers;

    /// <summary>Idle loop speed multiplier.</summary>
    public float Tempo = 1f;

    public readonly Skeleton Skeleton = new();
    public readonly List<PokeBoneInfo> Bones = new();
    public readonly List<PokeDecal> Decals = new();

    public SdfMesh Mesh = new();

    /// <summary>The surface pushed out by the outline's width, meshed on its own (see <see cref="PokemonModels.OutlinePixels"/>).</summary>
    public SdfMesh Shell = new();

    /// <summary>Grid cell the body was meshed with.</summary>
    public float Cell;

    /// <summary>Height of the model's top at rest, which clips scale their moves by.</summary>
    public float Height = 1f;

    /// <summary>How each view frames the model (see <see cref="PokemonSprites.Framing"/>), worked out once; models are built on several threads.</summary>
    internal readonly ConcurrentDictionary<(SpriteView, int), SpriteFraming> Framings = new();

    /// <summary>Set for a model imported from a glTF file (its pieces, bones and clips); null for a sculpted one.</summary>
    public ImportedRig? Imported;

    /// <summary>The model plays an idle clip of its own, so the body plan's idle motion stays out of it.</summary>
    public bool OwnIdle;

    /// <summary>The triangles under the decals, with texture coordinates into the decal atlas.</summary>
    public SdfMesh? DecalPatch;
    public Vector2[]? DecalUVs;
    public int AtlasColumns = 1, AtlasRows = 1;

    // On the GPU (see PokemonRenderer.EnsureUploaded)
    public SkinnedModel? Body;
    public SkinnedModel? Outline;
    public SkinnedModel? DecalModel;
    public Material[] DecalMaterials = Array.Empty<Material>();
    public bool Uploaded;

    // Scratch for posing: one pose at a time, on the main thread
    internal SkeletonPose Pose = null!;
    internal Matrix4x4[] Skin = Array.Empty<Matrix4x4>();

    internal void PrepareScratch()
    {
        Pose = new SkeletonPose(Skeleton.Count);
        Skin = new Matrix4x4[Skeleton.Count];
    }

    /// <summary>Poses the skeleton; the skinning matrices are in <see cref="Skin"/>.</summary>
    public void Animate(PokePose pose)
    {
        if (Imported != null)
        {
            ImportedModels.Animate(this, pose);
            return;
        }
        PokemonAnimation.Apply(this, pose, Pose);
        Skeleton.Evaluate(Pose, Skin);
    }

    /// <summary>The first bone with this role (-1 if there is none).</summary>
    public int Find(PokeRole role) => Bones.FindIndex(b => b.Role == role);
}

/// <summary>
/// Builds a species with the SDF kit, placing everything in model space (facing +Z, feet at y = 0). Every shape
/// names the bone it moves with; limbs, wings, tails and ears get bones of their own so the clips can move them.
/// </summary>
internal sealed class PokeBuilder
{
    private const float Deg = MathF.PI / 180f;
    public const int Root = 0, Body = 1;
    private readonly PokeModel m;
    private readonly SdfModel sdf;

    /// <summary>The material shapes get when none is named: the species' fur, scales or skin.</summary>
    public SurfaceMaterial Coat = SurfaceMaterial.Fur;

    /// <param name="body">Where the body bone turns: about the middle of the torso.</param>
    public PokeBuilder(string species, float fill, BodyPlan plan, Vector3 body)
    {
        m = new PokeModel { Species = species, Fill = fill, Plan = plan };
        // Shapes blend smoothly but colours change over a narrow band, or a belly lying close under the skin of the
        // body would tint a wide blur round itself
        sdf = new SdfModel("pokemon " + species) { WeightBlend = 0.03f, ColorSharpness = 0.88f };
        Bone("root", -1, Vector3.Zero, PokeRole.Root);
        Bone("body", Root, body, PokeRole.Body);
    }

    public PokeModel Model => m;
    public SdfModel Sdf => sdf;

    public PokeBuilder Hover()
    {
        m.Hovers = true;
        return this;
    }

    // ------------------------------------------------------------------ bones

    public int Bone(string name, int parent, Vector3 pivot, PokeRole role, float side = 0f, float phase = 0f, bool front = false)
    {
        int i = m.Skeleton.Add(name, parent, pivot);
        m.Bones.Add(new PokeBoneInfo(role, side, phase, front));
        return i;
    }

    public int Head(Vector3 neck, int parent = Body) => Bone("head", parent, neck, PokeRole.Head);

    public int Jaw(int head, Vector3 hinge) => Bone("jaw", head, hinge, PokeRole.Jaw);

    public int Arm(float side, Vector3 shoulder) => Bone(side < 0 ? "armL" : "armR", Body, shoulder, PokeRole.Arm, side, side);

    /// <summary>A leg hangs from the root, so it stays planted while the body breathes and bobs above it.</summary>
    public int Leg(float side, Vector3 hip, bool front = true) =>
        Bone((front ? "leg" : "hind") + (side < 0 ? "L" : "R"), Root, hip, PokeRole.Leg, side, side * 0.5f + (front ? 0f : 1.6f), front);

    public int Wing(float side, Vector3 joint) => Bone(side < 0 ? "wingL" : "wingR", Body, joint, PokeRole.Wing, side);

    public int Tail(Vector3 root, int parent = Body, float phase = 0f) => Bone("tail", parent, root, PokeRole.Tail, 0f, phase);

    public int Ear(int head, float side, Vector3 joint) => Bone(side < 0 ? "earL" : "earR", head, joint, PokeRole.Ear, side, side);

    public int Part(string name, int parent, Vector3 pivot, PokeRole role, float phase = 0f, float side = 0f) => Bone(name, parent, pivot, role, side, phase);

    // ------------------------------------------------------------------ shapes

    public SdfPrimitive Ell(int bone, Vector3 center, Vector3 radii, Color color, Vector3 rotationDeg = default, SurfaceMaterial? mat = null, float blend = 0.025f) =>
        sdf.Ellipsoid(center, radii, color, bone, blend, mat ?? Coat, Rot(rotationDeg));

    /// <summary>A rounded limb (capsule) from <paramref name="a"/> to <paramref name="b"/>, tapering from ra to rb.</summary>
    public SdfPrimitive Limb(int bone, Vector3 a, Vector3 b, float ra, float rb, Color color, SurfaceMaterial? mat = null, float blend = 0.02f) =>
        sdf.Capsule(a, b, ra, rb, color, bone, blend, mat ?? Coat);

    /// <summary>A cone from a round base to a rounded point (horns, claws, beaks, spikes); squash flattens it into a blade.</summary>
    public SdfPrimitive Spike(int bone, Vector3 baseCenter, Vector3 tip, float radius, Color color, float squash = 1f, SurfaceMaterial? mat = null, float blend = 0.012f)
    {
        var p = sdf.Capsule(baseCenter, tip, radius, Math.Max(0.004f, radius * 0.12f), color, bone, blend, mat ?? Coat);
        if (squash != 1f)
        {
            p.Rotation = AlignY(tip - baseCenter);
            p.Stretch = new Vector3(1f, 1f, squash);
        }
        return p;
    }

    public SdfPrimitive Torus(int bone, Vector3 center, float ring, float tube, Color color, Vector3 rotationDeg = default, float sx = 1f, float sz = 1f,
        SurfaceMaterial? mat = null, float blend = 0.006f)
    {
        var p = sdf.Torus(center, ring, tube, color, bone, blend, mat ?? Coat, Rot(rotationDeg));
        if (sx != 1f || sz != 1f) p.Stretch = new Vector3(sx, 1f, sz);
        return p;
    }

    public SdfPrimitive Box(int bone, Vector3 center, Vector3 half, float rounding, Color color, Vector3 rotationDeg = default, SurfaceMaterial? mat = null, float blend = 0f) =>
        sdf.Box(center, half, rounding, color, bone, blend, mat ?? Coat, Rot(rotationDeg));

    /// <summary>
    /// Recolours the surface of <paramref name="bone"/> inside an ellipsoid without changing its shape (masks, bands,
    /// patches). The edge is softened over <paramref name="soft"/>, about two cells of the mesh, or it would follow
    /// the vertices in steps.
    /// </summary>
    public SdfPrimitive PaintEll(int bone, Vector3 center, Vector3 radii, Color color, Vector3 rotationDeg = default, float soft = PaintEdge) =>
        sdf.Paint(sdf.Ellipsoid(center, radii, color, bone, soft, Coat, Rot(rotationDeg)), false, bone);

    /// <summary>Paint's default soft edge (model units).</summary>
    public const float PaintEdge = 0.014f;

    /// <summary>A painted ring round the surface (stripes round a body).</summary>
    public SdfPrimitive PaintTorus(int bone, Vector3 center, float ring, float tube, Color color, Vector3 rotationDeg = default, float sx = 1f, float sz = 1f)
    {
        var p = sdf.Torus(center, ring, tube, color, bone, PaintEdge, Coat, Rot(rotationDeg));
        if (sx != 1f || sz != 1f) p.Stretch = new Vector3(sx, 1f, sz);
        return sdf.Paint(p, false, bone);
    }

    // ------------------------------------------------------------------ decals

    /// <summary>
    /// A cartoon eye painted on the surface at <paramref name="at"/>, looking along <paramref name="facing"/>:
    /// a dark eye with a coloured iris and white glints, or a white eye with a dark pupil (<paramref name="sclera"/>).
    /// <paramref name="size"/> is half the eye's height.
    /// </summary>
    public void Eye(int bone, Vector3 at, Vector3 facing, float size, Color? iris = null, bool sclera = false, Color? pupil = null) =>
        m.Decals.Add(new PokeDecal
        {
            Bone = bone, Center = at, Normal = Vector3.Normalize(facing), Half = new Vector2(size * 1.6f, size * 1.8f), IsEye = true,
            Size = size, Iris = iris, Sclera = sclera, Pupil = pupil
        });

    /// <summary>A marking painted on the surface: a dot, ring, star or bar with radii <paramref name="rx"/> and <paramref name="ry"/>.</summary>
    public void Mark(int bone, Vector3 at, Vector3 facing, float rx, float ry, Color color, MarkShape shape = MarkShape.Disc, float rollDeg = 0f) =>
        m.Decals.Add(new PokeDecal
        {
            Bone = bone, Center = at, Normal = Vector3.Normalize(facing), Half = new Vector2(rx, ry) * 2f, Roll = rollDeg * Deg, Shape = shape, Color = color
        });

    // ------------------------------------------------------------------ placing the whole sculpt

    /// <summary>
    /// Moves everything up or down so the lowest point of the surface is <paramref name="gap"/> times the model's
    /// height above the ground: 0 stands it on its feet, more lifts a flier off its platform. The root stays at the
    /// origin. Used by the generated models, whose parts are placed by proportion rather than by hand.
    /// </summary>
    public void Ground(float gap = 0f)
    {
        float low = float.MaxValue, high = float.MinValue;
        foreach (var s in sdf.Shapes)
        {
            if (s.Op != SdfOp.Add) continue;
            float reach = Reach(s);
            low = Math.Min(low, (s.Shape == SdfShape.RoundCone ? Math.Min(s.A.Y - s.Size.X, s.B.Y - s.Size.Y) : s.A.Y - reach));
            high = Math.Max(high, (s.Shape == SdfShape.RoundCone ? Math.Max(s.A.Y + s.Size.X, s.B.Y + s.Size.Y) : s.A.Y + reach));
        }
        if (low == float.MaxValue) return;
        var d = new Vector3(0, gap * (high - low) - low, 0);
        if (MathF.Abs(d.Y) < 1e-6f) return;
        foreach (var s in sdf.Shapes)
        {
            s.A += d;
            s.B += d;
        }
        sdf.Changed();
        m.Skeleton.Shift(d, first: 1);
        foreach (var decal in m.Decals) decal.Center += d;
    }

    /// <summary>How far a shape reaches up or down from its centre (exact for ellipsoids and spheres, a safe bound otherwise).</summary>
    private static float Reach(SdfPrimitive s)
    {
        var up = Vector3.Transform(Vector3.UnitY, Quaternion.Inverse(Quaternion.Normalize(s.Rotation)));
        switch (s.Shape)
        {
            case SdfShape.Sphere:
                return s.Size.X * Math.Max(s.Stretch.X, Math.Max(s.Stretch.Y, s.Stretch.Z));
            case SdfShape.Ellipsoid:
                return (up * s.Size * s.Stretch).Length();
        }
        var half = s.Shape switch
        {
            SdfShape.Cylinder => new Vector3(s.Size.X, s.Size.Y, s.Size.X),
            SdfShape.Torus => new Vector3(s.Size.X + s.Size.Y, s.Size.Y, s.Size.X + s.Size.Y),
            _ => s.Size
        } * s.Stretch;
        return MathF.Abs(up.X) * half.X + MathF.Abs(up.Y) * half.Y + MathF.Abs(up.Z) * half.Z;
    }

    // ------------------------------------------------------------------ helpers

    private static Quaternion Rot(Vector3 deg) => Quaternion.CreateFromYawPitchRoll(deg.Y * Deg, deg.X * Deg, deg.Z * Deg);

    /// <summary>The rotation that turns +Y toward <paramref name="dir"/>.</summary>
    internal static Quaternion AlignY(Vector3 dir)
    {
        var d = Vector3.Normalize(dir);
        var axis = Vector3.Cross(Vector3.UnitY, d);
        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, d), -1f, 1f));
        if (axis.LengthSquared() < 1e-8f) return angle > 1f ? Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI) : Quaternion.Identity;
        return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
    }

    /// <summary>Calls <paramref name="build"/> once for each side (s = -1, then +1) of a symmetric part.</summary>
    public static void Both(Action<float> build)
    {
        build(-1f);
        build(1f);
    }
}

/// <summary>The species models, built and meshed on demand (in the background when preloaded) and kept.</summary>
internal static class PokemonModels
{
    /// <summary>Grid cells across a model's largest dimension when it is meshed.</summary>
    public const float CellsAcross = 128f;

    /// <summary>Outline width in battle, in pixels of the 128-px sprite frame (style guide, "Models").</summary>
    public const float OutlinePixels = 0.55f;

    private static readonly ConcurrentDictionary<string, Lazy<PokeModel>> Models = new(StringComparer.OrdinalIgnoreCase);

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);
    private static Vector3 V(float x, float y, float z) => new(x, y, z);

    private static readonly Color White = Rgb(246, 246, 250);
    private static readonly Color Black = Rgb(44, 42, 56);
    private static readonly Color Claw = Rgb(240, 240, 236);

    private const int Root = PokeBuilder.Root, Body = PokeBuilder.Body;
    private const SurfaceMaterial Fur = SurfaceMaterial.Fur, Scales = SurfaceMaterial.Scales, Shell = SurfaceMaterial.Shell,
        Leaf = SurfaceMaterial.Leaf, Metal = SurfaceMaterial.Metal, Glow = SurfaceMaterial.Glow;

    /// <summary>The model for a species (built now if it isn't ready; waits for a background build under way).</summary>
    public static PokeModel Get(string species) => Models.GetOrAdd(species, s => new Lazy<PokeModel>(() => Build(s))).Value;

    /// <summary>Species preloaded at start-up, which <see cref="Trim"/> never lets go of.</summary>
    private static readonly ConcurrentDictionary<string, bool> Pinned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Starts building these species' models in the background, and keeps them.</summary>
    public static void Preload(IEnumerable<string> species)
    {
        foreach (var name in species)
        {
            Pinned[name] = true;
            Request(name);
        }
    }

    /// <summary>
    /// Starts building a species' model in the background unless it is built or on its way (plan 03 · D5: models
    /// are made when first needed, ahead of the battle or scene that shows them).
    /// </summary>
    public static void Request(string species)
    {
        var lazy = Models.GetOrAdd(species, s => new Lazy<PokeModel>(() => Build(s)));
        if (!lazy.IsValueCreated) Task.Run(() => lazy.Value);
    }

    /// <summary>The model if it is built, without waiting for it.</summary>
    public static bool TryGet(string species, out PokeModel model)
    {
        if (Models.TryGetValue(species, out var lazy) && lazy.IsValueCreated)
        {
            model = lazy.Value;
            return true;
        }
        model = null!;
        return false;
    }

    /// <summary>
    /// Lets go of every model but these and the preloaded ones (main thread: it frees GPU buffers). The game calls it
    /// between scenes, so a long game doesn't keep every species it has met in memory.
    /// </summary>
    public static void Trim(IEnumerable<string> keep)
    {
        var wanted = new HashSet<string>(keep, StringComparer.OrdinalIgnoreCase);
        foreach (var name in Models.Keys)
            if (!wanted.Contains(name) && !Pinned.ContainsKey(name) && name != PokemonSprites.Fallback) Release(name);
    }

    private static readonly ConcurrentDictionary<string, string> Signatures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A short text that changes whenever the species' model would come out differently: the model file's size and
    /// date, or a hash of the sculpt. Caches of what is made from a model (the menu sprites) are keyed by it.
    /// </summary>
    public static string Signature(string species) => Signatures.GetOrAdd(species, s =>
    {
        string text;
        if (ModelOverrides.Find(s) is { } file)
        {
            var info = new FileInfo(file);
            var options = new FileInfo(System.IO.Path.ChangeExtension(file, ".json"));
            text = $"file|{ImportedModels.Version}|{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}|{(options.Exists ? options.LastWriteTimeUtc.Ticks : 0)}";
        }
        else
        {
            var b = Create(s);
            var sb = new System.Text.StringBuilder(b.Sdf.Describe());
            sb.Append('|').Append(b.Model.Plan).Append('|').Append(b.Model.Fill.ToString("R")).Append('|').Append(b.Model.Hovers);
            foreach (var d in b.Model.Decals)
                sb.Append('|').Append(d.Bone).Append(d.Center).Append(d.Normal).Append(d.Half).Append(d.Roll).Append(d.IsEye).Append(d.Size)
                    .Append(d.Iris?.R).Append(d.Iris?.G).Append(d.Iris?.B).Append(d.Sclera).Append(d.Pupil?.R).Append(d.Shape).Append(d.Color.R).Append(d.Color.G).Append(d.Color.B);
            text = "sculpt|" + sb;
        }
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    });

    /// <summary>Forgets signatures (after model files were added or removed).</summary>
    public static void ForgetSignatures() => Signatures.Clear();

    /// <summary>Every species that has a hand-built model (anything else uses a generic shape).</summary>
    public static readonly string[] Species =
    {
        "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
        "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Shinx", "Luxio", "Luxray", "Riolu", "Lucario",
        "Gible", "Gabite", "Garchomp", "Giratina"
    };

    public static bool HasModel(string species) => Array.Exists(Species, s => s.Equals(species, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Forgets a species' model so it is built again when next asked for, freeing its memory and its GPU buffers
    /// (so call it on the main thread). For tools that go through many species, like the harness's Pokédex boards.
    /// </summary>
    public static void Release(string species)
    {
        if (Models.TryRemove(species, out var lazy) && lazy.IsValueCreated) PokemonRenderer.Unload(lazy.Value);
    }

    /// <summary>
    /// Builds and meshes a species' model (no GPU calls): from a model file in <c>overrides/models</c> if there is one
    /// for it (<see cref="ModelOverrides"/>), else hand-built or generated.
    /// </summary>
    internal static PokeModel Build(string species)
    {
        if (ModelOverrides.Find(species) is { } file)
        {
            try
            {
                return ImportedModels.Load(file, species);
            }
            catch (Exception e) when (e is IOException or InvalidDataException or NotSupportedException or System.Text.Json.JsonException or FormatException
                                          or ArgumentException or IndexOutOfRangeException or KeyNotFoundException or InvalidOperationException)
            {
                ImportedModels.Report($"{System.IO.Path.GetFileName(file)} couldn't be used for {species}: {e.Message}");
            }
        }
        try
        {
            return Finish(Create(species));
        }
        catch (Exception e) when (species != PokemonSprites.Fallback)
        {
            // A sculpt the kit can't mesh mustn't stop the game: the species shows the stand-in instead
            ImportedModels.Report($"{species}'s model couldn't be built ({e.Message}); it shows the stand-in");
            return Finish(Generic(species));
        }
    }

    /// <summary>A model for each body plan, including the ones no hand-built species uses yet (for tests and the harness).</summary>
    internal static PokeModel Sample(BodyPlan plan) => plan switch
    {
        BodyPlan.Biped => Get("Riolu"),
        BodyPlan.Quadruped => Get("Shinx"),
        BodyPlan.Bird => Get("Starly"),
        _ => Models.GetOrAdd("sample " + plan, _ => new Lazy<PokeModel>(() => Finish(plan switch
        {
            BodyPlan.Serpent => SampleSerpent(),
            BodyPlan.Fish => SampleFish(),
            _ => SampleFloating()
        }))).Value
    };

    private static PokeBuilder Create(string species) => species.ToUpperInvariant() switch
    {
        "TURTWIG" => Turtwig(),
        "GROTLE" => Grotle(),
        "TORTERRA" => Torterra(),
        "CHIMCHAR" => Chimp(0),
        "MONFERNO" => Chimp(1),
        "INFERNAPE" => Infernape(),
        "PIPLUP" => Piplup(),
        "PRINPLUP" => Prinplup(),
        "EMPOLEON" => Empoleon(),
        "STARLY" => Bird(0),
        "STARAVIA" => Bird(1),
        "STARAPTOR" => Bird(2),
        "BIDOOF" => Bidoof(),
        "BIBAREL" => Bibarel(),
        "SHINX" => Cat(0),
        "LUXIO" => Cat(1),
        "LUXRAY" => Cat(2),
        "RIOLU" => Riolu(),
        "LUCARIO" => Lucario(),
        "GIBLE" => Gible(),
        "GABITE" => Garchomp(false),
        "GARCHOMP" => Garchomp(true),
        "GIRATINA" => Giratina(),
        // Every other species is generated from its data (plan 03 · D5); a name that isn't a species gets the stand-in
        _ => PokemonGenerator.Build(species) ?? Generic(species)
    };

    /// <summary>Meshes the sculpted model, its outline shell and its decals.</summary>
    private static PokeModel Finish(PokeBuilder b)
    {
        var m = b.Model;
        var sdf = b.Sdf;
        sdf.BoneCount = m.Skeleton.Count;
        if (m.Skeleton.Count > SkinnedModel.MaxBones) throw new InvalidOperationException($"{m.Species} has {m.Skeleton.Count} bones");

        var (min, max) = sdf.Bounds();
        float extent = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
        float cell = extent / CellsAcross;
        var mesh = SdfCache.Get(sdf, cell);
        // One mesh can index 65,536 vertices: a model with a lot of surface is meshed a little coarser
        while (mesh.VertexCount > 60000)
        {
            cell *= 1.15f;
            mesh = SdfCache.Get(sdf, cell);
        }
        m.Mesh = mesh;
        m.Cell = cell;
        float top = 0f;
        foreach (var p in mesh.Positions) top = Math.Max(top, p.Y);
        m.Height = Math.Max(0.1f, top);

        var framing = PokemonSprites.Framing(m, SpriteView.Front, PokemonSprites.Size);
        m.Shell = SdfCache.Get(sdf, cell * 1.25f, framing.WorldPerPixel * OutlinePixels);
        PokemonDecals.Project(m);
        m.PrepareScratch();
        return m;
    }

    // ------------------------------------------------------------------ grass starters

    private static PokeBuilder Turtwig()
    {
        var b = new PokeBuilder("Turtwig", 0.58f, BodyPlan.Quadruped, V(0, 0.22f, -0.05f)) { Coat = Scales };
        var green = Rgb(160, 212, 92);
        var legs = Rgb(118, 172, 70);
        var shell = Rgb(150, 98, 54);
        var rim = Rgb(214, 180, 104);
        var jaw = Rgb(244, 222, 110);

        foreach (var (z, front) in new[] { (0.14f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.2f * s, 0.16f, z), front);
                b.Ell(leg, V(0.2f * s, 0.09f, z), V(0.1f, 0.1f, 0.11f), legs);
            });
        b.Ell(Body, V(0, 0.2f, -0.03f), V(0.3f, 0.16f, 0.36f), green);
        b.Ell(Body, V(0, 0.29f, -0.09f), V(0.34f, 0.21f, 0.37f), shell, mat: Shell);
        b.Torus(Body, V(0, 0.2f, -0.08f), 0.31f, 0.045f, rim, sz: 1.12f, mat: Shell);
        int tail = b.Tail(V(0, 0.2f, -0.36f));
        b.Ell(tail, V(0, 0.2f, -0.42f), V(0.06f, 0.06f, 0.07f), green);

        int head = b.Head(V(0, 0.36f, 0.2f));
        b.Ell(head, V(0, 0.52f, 0.27f), V(0.31f, 0.27f, 0.28f), green);
        b.Ell(head, V(0, 0.44f, 0.33f), V(0.27f, 0.14f, 0.24f), jaw);
        b.Ell(head, V(0, 0.62f, 0.2f), V(0.25f, 0.13f, 0.22f), PixelCanvas.Shadow(green, 0.12f));
        PokeBuilder.Both(s => b.Mark(head, V(0.045f * s, 0.5f, 0.54f), V(0.15f * s, 0.1f, 1f), 0.016f, 0.012f, Black));
        PokeBuilder.Both(s => b.Eye(head, V(0.15f * s, 0.55f, 0.46f), V(0.62f * s, 0.1f, 1f), 0.07f, Rgb(70, 60, 50)));

        int leaves = b.Part("leaves", head, V(0, 0.82f, 0.22f), PokeRole.Leaf);
        b.Limb(head, V(0, 0.74f, 0.22f), V(0, 0.86f, 0.22f), 0.03f, 0.025f, Rgb(116, 78, 46), Shell);
        PokeBuilder.Both(s => b.Ell(leaves, V(0.11f * s, 0.9f, 0.22f), V(0.13f, 0.035f, 0.08f), Rgb(92, 196, 78), V(0, 0, -30f * s), Leaf, 0.015f));
        return b;
    }

    private static PokeBuilder Grotle()
    {
        var b = new PokeBuilder("Grotle", 0.74f, BodyPlan.Quadruped, V(0, 0.36f, -0.06f)) { Coat = Scales };
        var green = Rgb(116, 166, 84);
        var legs = Rgb(96, 142, 70);
        var shell = Rgb(148, 106, 62);
        var bush = Rgb(74, 158, 74);
        var face = Rgb(236, 206, 112);

        foreach (var (z, front) in new[] { (0.2f, true), (-0.28f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.27f * s, 0.3f, z), front);
                b.Limb(leg, V(0.27f * s, 0.26f, z), V(0.29f * s, 0.1f, z + 0.02f), 0.1f, 0.1f, legs);
                b.Spike(leg, V(0.29f * s, 0.03f, z + 0.1f), V(0.29f * s, 0.02f, z + 0.18f), 0.03f, Claw, mat: Shell);
            });
        b.Ell(Body, V(0, 0.32f, -0.04f), V(0.38f, 0.2f, 0.5f), green);
        b.Ell(Body, V(0, 0.44f, -0.08f), V(0.42f, 0.22f, 0.5f), shell, mat: Shell);
        b.Torus(Body, V(0, 0.3f, -0.07f), 0.39f, 0.04f, PixelCanvas.Shadow(shell, 0.3f), sz: 1.2f, mat: Shell);
        // Two shrubs grow from the shell
        foreach (float x in new[] { -0.17f, 0.17f })
        {
            int shrub = b.Part(x < 0 ? "shrubL" : "shrubR", Body, V(x, 0.6f, -0.02f), PokeRole.Leaf, x * 6f);
            b.Ell(shrub, V(x, 0.66f, -0.02f), V(0.17f, 0.13f, 0.17f), bush, mat: Leaf);
            b.Ell(shrub, V(x - 0.06f, 0.72f, -0.08f), V(0.1f, 0.09f, 0.1f), PixelCanvas.Light1(bush, 0.15f), mat: Leaf);
            b.Ell(shrub, V(x + 0.07f, 0.7f, 0.05f), V(0.09f, 0.08f, 0.09f), PixelCanvas.Shadow(bush, 0.1f), mat: Leaf);
        }
        int tail = b.Tail(V(0, 0.28f, -0.48f));
        b.Ell(tail, V(0, 0.28f, -0.56f), V(0.08f, 0.07f, 0.1f), green);

        int head = b.Head(V(0, 0.4f, 0.42f));
        b.Ell(head, V(0, 0.46f, 0.6f), V(0.22f, 0.19f, 0.22f), green);
        b.Ell(head, V(0, 0.39f, 0.67f), V(0.2f, 0.11f, 0.18f), face);
        b.Ell(head, V(0, 0.57f, 0.58f), V(0.21f, 0.1f, 0.21f), shell, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.12f * s, 0.48f, 0.76f), V(0.6f * s, 0.1f, 1f), 0.05f, Rgb(80, 70, 50)));
        return b;
    }

    private static PokeBuilder Torterra()
    {
        var b = new PokeBuilder("Torterra", 0.95f, BodyPlan.Quadruped, V(0, 0.48f, 0)) { Coat = Scales };
        var body = Rgb(96, 136, 84);
        var legs = Rgb(84, 118, 74);
        var shell = Rgb(128, 90, 56);
        var rock = Rgb(170, 174, 182);
        var leaves = Rgb(70, 150, 76);

        foreach (var (z, front) in new[] { (0.3f, true), (-0.36f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.4f * s, 0.4f, z), front);
                b.Limb(leg, V(0.4f * s, 0.34f, z), V(0.43f * s, 0.14f, z), 0.15f, 0.14f, legs);
                b.Spike(leg, V(0.46f * s, 0.2f, z), V(0.62f * s, 0.24f, z), 0.05f, rock, mat: Shell);
            });
        b.Ell(Body, V(0, 0.44f, 0), V(0.52f, 0.28f, 0.66f), body);
        b.Ell(Body, V(0, 0.6f, -0.05f), V(0.56f, 0.24f, 0.64f), shell, mat: Shell);
        // Rocky spikes around the shell, a tree growing from the middle
        for (int i = 0; i < 7; i++)
        {
            float a = -1.2f + i * 0.4f;
            b.Spike(Body, V(MathF.Sin(a) * 0.46f, 0.72f, MathF.Cos(a) * 0.5f - 0.12f), V(MathF.Sin(a) * 0.6f, 0.95f, MathF.Cos(a) * 0.62f - 0.12f), 0.07f, rock, mat: Shell);
        }
        b.Limb(Body, V(0, 0.75f, -0.15f), V(0.04f, 1.15f, -0.18f), 0.08f, 0.06f, Rgb(118, 82, 50), Shell);
        int canopy = b.Part("canopy", Body, V(0.03f, 1.15f, -0.18f), PokeRole.Leaf);
        b.Ell(canopy, V(0.03f, 1.28f, -0.18f), V(0.38f, 0.2f, 0.34f), leaves, mat: Leaf, blend: 0.04f);
        b.Ell(canopy, V(-0.12f, 1.38f, -0.12f), V(0.22f, 0.13f, 0.2f), PixelCanvas.Light1(leaves, 0.18f), mat: Leaf, blend: 0.04f);
        b.Ell(canopy, V(0.18f, 1.34f, -0.26f), V(0.2f, 0.12f, 0.18f), PixelCanvas.Shadow(leaves, 0.1f), mat: Leaf, blend: 0.04f);

        int head = b.Head(V(0, 0.5f, 0.58f));
        b.Ell(head, V(0, 0.54f, 0.82f), V(0.25f, 0.21f, 0.26f), body);
        b.Ell(head, V(0, 0.45f, 0.9f), V(0.22f, 0.11f, 0.2f), Rgb(214, 190, 112));
        b.Ell(head, V(0, 0.66f, 0.78f), V(0.24f, 0.1f, 0.23f), shell, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.13f * s, 0.57f, 1.0f), V(0.6f * s, 0.1f, 1f), 0.05f, Rgb(250, 214, 90)));
        return b;
    }

    // ------------------------------------------------------------------ fire starters

    /// <summary>A flame that flickers on its own bone: orange, lighter toward its heart, lit from within.</summary>
    internal static void Flame(PokeBuilder b, int bone, Vector3 at, float s)
    {
        var red = Rgb(242, 88, 44);
        b.Ell(bone, at + V(0, 0.1f * s, 0), V(0.1f * s, 0.17f * s, 0.1f * s), red, mat: Glow, blend: 0.03f * s);
        b.Spike(bone, at + V(0, 0.18f * s, 0), at + V(0.02f * s, 0.36f * s, -0.02f * s), 0.08f * s, red, mat: Glow, blend: 0.03f * s);
        b.PaintEll(bone, at + V(0, 0.11f * s, 0.06f * s), V(0.07f * s, 0.13f * s, 0.08f * s), Rgb(252, 166, 60), soft: 0.03f * s);
        b.PaintEll(bone, at + V(0, 0.08f * s, 0.08f * s), V(0.04f * s, 0.07f * s, 0.06f * s), Rgb(255, 238, 130), soft: 0.025f * s);
    }

    private static PokeBuilder Chimp(int stage)
    {
        bool mon = stage == 1;
        var orange = mon ? Rgb(226, 128, 56) : Rgb(242, 142, 60);
        var cream = Rgb(250, 226, 178);
        var brown = Rgb(176, 104, 54);
        float leg = mon ? 0.2f : 0.12f;
        float torso = 0.28f + leg;
        var b = new PokeBuilder(mon ? "Monferno" : "Chimchar", mon ? 0.74f : 0.6f, BodyPlan.Biped, V(0, torso, 0)) { Coat = Fur };

        // Legs and feet
        PokeBuilder.Both(s =>
        {
            int l = b.Leg(s, V(0.1f * s, 0.12f + leg, 0));
            b.Limb(l, V(0.1f * s, 0.12f + leg, 0), V(0.11f * s, 0.06f, 0.02f), 0.075f, 0.07f, mon ? brown : orange);
            b.Ell(l, V(0.11f * s, 0.035f, 0.05f), V(0.075f, 0.04f, 0.11f), mon ? cream : brown);
        });
        b.Ell(Body, V(0, torso, 0), V(0.18f, 0.2f + (mon ? 0.04f : 0f), 0.16f), orange);
        b.PaintEll(Body, V(0, torso - 0.01f, 0.065f), V(0.13f, 0.15f, 0.11f), cream);

        // Tail with the flame on its tip (Chimchar's flame sits right on its rear)
        if (mon)
        {
            int tail = b.Tail(V(0, torso - 0.12f, -0.12f));
            b.Limb(tail, V(0, torso - 0.12f, -0.12f), V(0, torso + 0.02f, -0.36f), 0.035f, 0.03f, orange);
            b.Limb(tail, V(0, torso + 0.02f, -0.36f), V(0, torso + 0.22f, -0.42f), 0.03f, 0.03f, orange);
            int flame = b.Part("flame", tail, V(0, torso + 0.22f, -0.42f), PokeRole.Flame);
            Flame(b, flame, V(0, torso + 0.22f, -0.42f), 0.8f);
        }
        else
        {
            int flame = b.Part("flame", Body, V(0, torso - 0.1f, -0.16f), PokeRole.Flame);
            Flame(b, flame, V(0, torso - 0.1f, -0.16f), 1f);
        }

        // Arms with cream hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, torso + 0.1f, 0));
            b.Limb(arm, V(0.16f * s, torso + 0.1f, 0), V(0.25f * s, torso - 0.06f, 0.08f), 0.055f, 0.05f, orange);
            b.Ell(arm, V(0.26f * s, torso - 0.09f, 0.09f), V(0.06f, 0.06f, 0.06f), mon ? orange : cream);
            if (mon) b.Torus(arm, V(0.24f * s, torso - 0.03f, 0.07f), 0.05f, 0.018f, Rgb(244, 200, 70), V(0, 0, 60f * s), mat: Metal);
        });

        int head = b.Head(V(0, torso + 0.18f, 0));
        float hy = torso + 0.37f;
        b.Ell(head, V(0, hy, 0.02f), V(0.24f, 0.22f, 0.21f), orange);
        b.Ell(head, V(0, hy - 0.04f, 0.1f), V(0.18f, 0.15f, 0.14f), cream);
        b.Ell(head, V(0, hy - 0.02f, 0.235f), V(0.03f, 0.025f, 0.02f), Rgb(222, 72, 60), blend: 0.01f);
        b.Mark(head, V(0, hy - 0.1f, 0.22f), V(0, -0.2f, 1f), 0.05f, 0.012f, Rgb(170, 70, 60), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.2f * s, hy + 0.02f, 0));
            b.Ell(ear, V(0.24f * s, hy + 0.02f, -0.01f), V(0.1f, 0.1f, 0.05f), orange, V(0, 20f * s, 0));
            b.PaintEll(ear, V(0.26f * s, hy + 0.02f, 0.03f), V(0.065f, 0.065f, 0.04f), mon ? Rgb(244, 200, 70) : Rgb(236, 120, 110), V(0, 20f * s, 0));
        });
        if (mon)
        {
            // Blue face paint around the eyes, and a thin golden crest
            PokeBuilder.Both(s => b.PaintEll(head, V(0.085f * s, hy + 0.03f, 0.17f), V(0.075f, 0.065f, 0.08f), Rgb(80, 140, 226)));
            b.Spike(head, V(0, hy + 0.19f, 0.03f), V(0, hy + 0.3f, 0.14f), 0.035f, Rgb(244, 200, 70), mat: Metal);
        }
        // Swirled tuft of hair on top
        b.Ell(head, V(0, hy + 0.22f, 0.0f), V(0.07f, 0.06f, 0.07f), orange);
        b.Spike(head, V(0, hy + 0.25f, 0.0f), V(0.03f, hy + 0.34f, 0.08f), 0.05f, orange);

        PokeBuilder.Both(s => b.Eye(head, V(0.085f * s, hy + 0.03f, 0.19f), V(0.35f * s, 0.05f, 1f), 0.058f, Rgb(80, 60, 50)));
        return b;
    }

    private static PokeBuilder Infernape()
    {
        var b = new PokeBuilder("Infernape", 0.95f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var orange = Rgb(232, 124, 48);
        var white = Rgb(248, 244, 236);
        var gold = Rgb(244, 200, 66);
        var blue = Rgb(70, 130, 224);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.46f, -0.02f));
            b.Limb(leg, V(0.13f * s, 0.46f, -0.02f), V(0.2f * s, 0.24f, 0.08f), 0.08f, 0.07f, orange);
            b.Limb(leg, V(0.2f * s, 0.24f, 0.08f), V(0.18f * s, 0.06f, 0.02f), 0.07f, 0.06f, orange);
            b.Ell(leg, V(0.18f * s, 0.035f, 0.07f), V(0.08f, 0.04f, 0.12f), white);
            b.Torus(leg, V(0.2f * s, 0.24f, 0.08f), 0.07f, 0.03f, gold, V(80, 0, 0), mat: Metal);
        });
        b.Ell(Body, V(0, 0.62f, 0), V(0.2f, 0.24f, 0.17f), white);
        b.PaintEll(Body, V(0, 0.5f, 0.03f), V(0.2f, 0.1f, 0.18f), orange);
        int tail = b.Tail(V(0, 0.46f, -0.14f));
        b.Limb(tail, V(0, 0.46f, -0.14f), V(0, 0.62f, -0.4f), 0.035f, 0.03f, orange);

        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.19f * s, 0.76f, 0));
            b.Ell(arm, V(0.21f * s, 0.78f, 0), V(0.1f, 0.08f, 0.1f), gold, mat: Metal);
            b.Limb(arm, V(0.22f * s, 0.74f, 0.02f), V(0.33f * s, 0.54f, 0.12f), 0.06f, 0.055f, white);
            b.Limb(arm, V(0.33f * s, 0.54f, 0.12f), V(0.38f * s, 0.4f, 0.2f), 0.055f, 0.05f, orange);
            b.Torus(arm, V(0.35f * s, 0.49f, 0.15f), 0.055f, 0.022f, gold, V(60, 0, 20f * s), mat: Metal);
            b.Ell(arm, V(0.39f * s, 0.37f, 0.21f), V(0.06f, 0.06f, 0.06f), orange);
        });

        int head = b.Head(V(0, 0.84f, 0.02f));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.04f), V(0.18f, 0.17f, 0.17f), orange);
        b.Ell(head, V(0, hy - 0.05f, 0.1f), V(0.13f, 0.1f, 0.1f), white);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, V(0.075f * s, hy + 0.02f, 0.14f), V(0.065f, 0.05f, 0.06f), blue);
            b.Ell(head, V(0.075f * s, hy + 0.07f, 0.13f), V(0.07f, 0.02f, 0.03f), gold, mat: Metal, blend: 0.01f);
        });
        int flame = b.Part("flame", head, V(0, hy + 0.12f, 0), PokeRole.Flame);
        Flame(b, flame, V(0, hy + 0.1f, -0.02f), 1.3f);
        PokeBuilder.Both(s => b.Eye(head, V(0.07f * s, hy + 0.02f, 0.17f), V(0.4f * s, 0.05f, 1f), 0.04f));
        return b;
    }

    // ------------------------------------------------------------------ water starters

    private static PokeBuilder Piplup()
    {
        var b = new PokeBuilder("Piplup", 0.58f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Scales };
        var light = Rgb(150, 208, 244);
        var blue = Rgb(64, 132, 214);
        var dark = Rgb(40, 84, 170);
        var yellow = Rgb(252, 200, 60);

        PokeBuilder.Both(s =>
        {
            int foot = b.Leg(s, V(0.1f * s, 0.1f, 0.03f));
            b.Ell(foot, V(0.1f * s, 0.035f, 0.07f), V(0.08f, 0.035f, 0.1f), yellow, mat: Shell);
        });
        b.Ell(Body, V(0, 0.28f, 0), V(0.21f, 0.24f, 0.19f), light);
        b.PaintEll(Body, V(0, 0.27f, 0.08f), V(0.15f, 0.17f, 0.12f), White);
        PokeBuilder.Both(s => b.Mark(Body, V(0.055f * s, 0.33f, 0.2f), V(0.2f * s, 0.1f, 1f), 0.03f, 0.03f, light));
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.2f * s, 0.4f, 0));
            b.Ell(wing, V(0.25f * s, 0.3f, 0.01f), V(0.05f, 0.15f, 0.09f), dark, V(0, 0, 18f * s));
        });

        int head = b.Head(V(0, 0.46f, 0));
        float hy = 0.68f;
        b.Ell(head, V(0, hy, 0.02f), V(0.26f, 0.24f, 0.24f), blue);
        // Dark blue cap over the top and back of the head
        b.Ell(head, V(0, hy + 0.05f, -0.04f), V(0.265f, 0.22f, 0.245f), dark, blend: 0.01f);
        b.Ell(head, V(0, hy - 0.05f, 0.1f), V(0.2f, 0.15f, 0.15f), light);
        b.Spike(head, V(0, hy - 0.06f, 0.2f), V(0, hy - 0.08f, 0.33f), 0.06f, yellow, 0.7f, Shell);
        b.Ell(head, V(0, hy - 0.1f, 0.21f), V(0.05f, 0.025f, 0.05f), PixelCanvas.Shadow(yellow, 0.2f), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Eye(head, V(0.09f * s, hy + 0.02f, 0.2f), V(0.45f * s, 0.05f, 1f), 0.06f, sclera: true));
        return b;
    }

    private static PokeBuilder Prinplup()
    {
        var b = new PokeBuilder("Prinplup", 0.75f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Scales };
        var blue = Rgb(56, 104, 190);
        var light = Rgb(150, 206, 244);
        var yellow = Rgb(250, 200, 62);

        PokeBuilder.Both(s =>
        {
            int foot = b.Leg(s, V(0.11f * s, 0.1f, 0.04f));
            b.Ell(foot, V(0.11f * s, 0.035f, 0.08f), V(0.09f, 0.035f, 0.12f), yellow, mat: Shell);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.23f, 0.34f, 0.2f), blue);
        b.PaintEll(Body, V(0, 0.36f, 0.08f), V(0.17f, 0.25f, 0.13f), light);
        foreach (var (x, y) in new[] { (-0.07f, 0.44f), (0.07f, 0.44f), (0f, 0.3f) })
            b.Mark(Body, V(x, y, 0.2f), V(x * 3f, 0.1f, 1f), 0.03f, 0.03f, White);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.21f * s, 0.54f, 0));
            b.Ell(wing, V(0.29f * s, 0.4f, 0.02f), V(0.05f, 0.2f, 0.1f), Rgb(40, 80, 160), V(0, 0, 25f * s));
        });

        int head = b.Head(V(0, 0.66f, 0));
        float hy = 0.84f;
        b.Ell(head, V(0, hy, 0.02f), V(0.19f, 0.18f, 0.18f), blue);
        b.Ell(head, V(0, hy - 0.04f, 0.09f), V(0.14f, 0.1f, 0.1f), light);
        // The beak grows up into a two-pronged crest
        b.Spike(head, V(0, hy - 0.05f, 0.16f), V(0, hy - 0.07f, 0.29f), 0.05f, yellow, 0.7f, Shell);
        b.Limb(head, V(0, hy - 0.02f, 0.17f), V(0, hy + 0.14f, 0.16f), 0.03f, 0.025f, yellow, Shell, 0.01f);
        PokeBuilder.Both(s => b.Spike(head, V(0, hy + 0.14f, 0.16f), V(0.09f * s, hy + 0.24f, 0.12f), 0.03f, yellow, mat: Shell));
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s, hy + 0.02f, 0.15f), V(0.5f * s, 0.05f, 1f), 0.045f, sclera: true));
        return b;
    }

    private static PokeBuilder Empoleon()
    {
        var b = new PokeBuilder("Empoleon", 0.95f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var navy = Rgb(36, 52, 104);
        var light = Rgb(110, 170, 226);
        var gold = Rgb(246, 204, 64);
        var steel = Rgb(120, 160, 206);

        PokeBuilder.Both(s =>
        {
            int foot = b.Leg(s, V(0.13f * s, 0.12f, 0.04f));
            b.Ell(foot, V(0.13f * s, 0.04f, 0.09f), V(0.1f, 0.04f, 0.14f), gold, mat: Metal);
        });
        b.Ell(Body, V(0, 0.52f, 0), V(0.28f, 0.48f, 0.24f), navy);
        b.PaintEll(Body, V(0, 0.5f, 0.1f), V(0.19f, 0.38f, 0.16f), White);
        foreach (var (x, y) in new[] { (-0.08f, 0.64f), (0.08f, 0.64f), (0f, 0.48f), (-0.07f, 0.34f), (0.07f, 0.34f) })
            b.Mark(Body, V(x, y, 0.25f), V(x * 3f, 0.05f, 1f), 0.035f, 0.035f, navy);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.26f * s, 0.8f, 0));
            b.Ell(wing, V(0.36f * s, 0.55f, 0.02f), V(0.06f, 0.3f, 0.13f), steel, V(0, 0, 22f * s), Metal);
            b.Ell(wing, V(0.35f * s, 0.56f, 0.03f), V(0.065f, 0.24f, 0.08f), light, V(0, 0, 22f * s), Metal, 0.01f);
        });

        int head = b.Head(V(0, 0.98f, 0));
        float hy = 1.1f;
        b.Ell(head, V(0, hy, 0.02f), V(0.16f, 0.15f, 0.16f), navy);
        b.Ell(head, V(0, hy - 0.03f, 0.08f), V(0.12f, 0.09f, 0.09f), light);
        // Golden trident crest rising from the beak
        b.Spike(head, V(0, hy - 0.04f, 0.14f), V(0, hy - 0.07f, 0.28f), 0.045f, gold, 0.7f, Metal);
        b.Limb(head, V(0, hy - 0.02f, 0.15f), V(0, hy + 0.2f, 0.12f), 0.035f, 0.03f, gold, Metal, 0.01f);
        PokeBuilder.Both(s =>
        {
            b.Limb(head, V(0.02f * s, hy + 0.02f, 0.14f), V(0.12f * s, hy + 0.14f, 0.1f), 0.028f, 0.022f, gold, Metal, 0.008f);
            b.Spike(head, V(0.12f * s, hy + 0.14f, 0.1f), V(0.16f * s, hy + 0.26f, 0.08f), 0.025f, gold, mat: Metal);
        });
        b.Spike(head, V(0, hy + 0.2f, 0.12f), V(0, hy + 0.32f, 0.1f), 0.035f, gold, mat: Metal);
        PokeBuilder.Both(s => b.Eye(head, V(0.07f * s, hy + 0.02f, 0.14f), V(0.55f * s, 0.05f, 1f), 0.035f, sclera: true));
        return b;
    }

    // ------------------------------------------------------------------ Starly line

    private static PokeBuilder Bird(int stage)
    {
        string name = stage switch { 0 => "Starly", 1 => "Staravia", _ => "Staraptor" };
        float s1 = 1f + stage * 0.2f;
        var b = new PokeBuilder(name, stage switch { 0 => 0.52f, 1 => 0.68f, _ => 0.86f }, BodyPlan.Bird, V(0, 0.28f * s1, 0)) { Coat = Fur }.Hover();
        var gray = stage == 2 ? Rgb(104, 100, 108) : Rgb(126, 116, 112);
        var dark = Rgb(52, 48, 56);
        var orange = Rgb(248, 150, 44);

        // Legs, body and belly
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.16f * s1, 0.02f));
            b.Limb(leg, V(0.07f * s, 0.14f * s1, 0.02f), V(0.08f * s, 0.03f, 0.04f), 0.022f, 0.018f, orange, Scales, 0.008f);
            b.Ell(leg, V(0.08f * s, 0.02f, 0.07f), V(0.035f, 0.015f, 0.05f), orange, mat: Scales, blend: 0.008f);
        });
        b.Ell(Body, V(0, 0.28f * s1, 0), V(0.2f * s1, 0.18f * s1, 0.24f * s1), gray);
        b.PaintEll(Body, V(0, 0.25f * s1, 0.08f * s1), V(0.15f * s1, 0.14f * s1, 0.15f * s1), White);
        foreach (var (x, y) in new[] { (-0.06f, 0.24f), (0.05f, 0.2f), (0f, 0.3f), (0.07f, 0.28f) })
            b.Mark(Body, V(x * s1, y * s1, 0.21f * s1), V(x * 4f, 0.05f, 1f), 0.02f * s1, 0.02f * s1, Rgb(170, 164, 164));

        // Fanned tail
        int tail = b.Tail(V(0, 0.28f * s1, -0.2f * s1));
        foreach (float a in new[] { -24f, 0f, 24f })
            b.Ell(tail, V(MathF.Sin(a * MathF.PI / 180f) * 0.08f * s1, 0.27f * s1, -0.34f * s1), V(0.055f * s1, 0.032f * s1, 0.14f * s1), a == 0 ? dark : gray, V(-14f, a, 0), blend: 0.012f);

        // Wings folded at the sides
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.17f * s * s1, 0.36f * s1, 0.02f));
            b.Ell(wing, V(0.2f * s * s1, 0.28f * s1, -0.05f * s1), V(0.05f * s1, 0.13f * s1, 0.2f * s1), dark, V(-15f, 0, 12f * s));
            b.Ell(wing, V(0.21f * s * s1, 0.27f * s1, -0.02f * s1), V(0.04f * s1, 0.08f * s1, 0.12f * s1), gray, V(-15f, 0, 12f * s), blend: 0.01f);
        });

        int head = b.Head(V(0, 0.4f * s1, 0.08f * s1));
        float hy = 0.54f * s1;
        b.Ell(head, V(0, hy, 0.1f * s1), V(0.15f * s1, 0.14f * s1, 0.14f * s1), dark);
        // White face mask around the eyes
        b.Ell(head, V(0, hy - 0.02f * s1, 0.17f * s1), V(0.13f * s1, 0.09f * s1, 0.09f * s1), White);
        b.Spike(head, V(0, hy - 0.02f * s1, 0.24f * s1), V(0, hy - 0.06f * s1, 0.36f * s1), 0.045f * s1, orange, 0.8f, Shell);
        if (stage == 0)
        {
            b.Spike(head, V(0, hy + 0.12f * s1, 0.08f * s1), V(0, hy + 0.2f * s1, 0.16f * s1), 0.035f * s1, dark);
        }
        else
        {
            // Crest: curled on Staravia, long and red-tipped on Staraptor
            b.Limb(head, V(0, hy + 0.1f * s1, 0.04f * s1), V(0, hy + 0.26f * s1, 0.14f * s1), 0.04f * s1, 0.03f * s1, dark);
            b.Limb(head, V(0, hy + 0.26f * s1, 0.14f * s1), V(0, hy + (stage == 2 ? 0.18f : 0.24f) * s1, 0.28f * s1), 0.03f * s1, 0.022f * s1,
                stage == 2 ? Rgb(214, 60, 60) : dark, blend: 0.01f);
        }
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s * s1, hy + 0.02f * s1, 0.2f * s1), V(0.55f * s, 0.05f, 1f), 0.035f * s1,
            stage == 2 ? Rgb(236, 90, 60) : Rgb(248, 150, 44)));
        return b;
    }

    // ------------------------------------------------------------------ Bidoof line

    private static PokeBuilder Bidoof()
    {
        var b = new PokeBuilder("Bidoof", 0.6f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Fur };
        var brown = Rgb(176, 122, 68);
        var dark = Rgb(122, 80, 44);
        var tan = Rgb(232, 202, 148);

        foreach (var (z, front) in new[] { (0.16f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.17f * s, 0.1f, z), front);
                b.Ell(leg, V(0.17f * s, 0.05f, z), V(0.08f, 0.05f, 0.09f), dark);
            });
        b.Ell(Body, V(0, 0.3f, -0.02f), V(0.34f, 0.3f, 0.34f), brown);
        int tail = b.Tail(V(0, 0.22f, -0.3f));
        b.Ell(tail, V(0, 0.2f, -0.36f), V(0.07f, 0.06f, 0.06f), dark);

        int head = b.Head(V(0, 0.3f, 0.1f));
        // Darker ruff framing the face, tan muzzle, red nose and big buck teeth
        b.Ell(head, V(0, 0.36f, 0.14f), V(0.3f, 0.26f, 0.22f), dark);
        b.Ell(head, V(0, 0.36f, 0.19f), V(0.26f, 0.23f, 0.19f), brown);
        b.Ell(head, V(0, 0.28f, 0.3f), V(0.17f, 0.12f, 0.1f), tan);
        b.Ell(head, V(0, 0.34f, 0.39f), V(0.05f, 0.04f, 0.035f), Rgb(204, 64, 56), mat: Scales, blend: 0.012f);
        PokeBuilder.Both(s => b.Box(head, V(0.026f * s, 0.21f, 0.37f), V(0.022f, 0.042f, 0.018f), 0.012f, White, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.17f * s, 0.52f, 0.06f));
            b.Ell(ear, V(0.2f * s, 0.58f, 0.04f), V(0.08f, 0.08f, 0.045f), dark);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.12f * s, 0.44f, 0.33f), V(0.4f * s, 0.1f, 1f), 0.035f));
        return b;
    }

    private static PokeBuilder Bibarel()
    {
        var b = new PokeBuilder("Bibarel", 0.78f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var brown = Rgb(156, 104, 62);
        var tan = Rgb(226, 196, 142);
        var dark = Rgb(98, 64, 40);

        PokeBuilder.Both(s =>
        {
            int foot = b.Leg(s, V(0.14f * s, 0.12f, 0.1f));
            b.Ell(foot, V(0.14f * s, 0.05f, 0.12f), V(0.09f, 0.05f, 0.12f), dark);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.3f, 0.36f, 0.27f), brown);
        b.PaintEll(Body, V(0, 0.32f, 0.1f), V(0.2f, 0.26f, 0.18f), tan);
        // Paddle tail with dark bands
        int tail = b.Tail(V(0, 0.18f, -0.22f));
        b.Ell(tail, V(0, 0.16f, -0.42f), V(0.17f, 0.05f, 0.24f), brown, V(-20f, 0, 0), Scales);
        foreach (float z in new[] { -0.36f, -0.46f, -0.56f })
            b.PaintEll(tail, V(0, 0.16f + (z + 0.42f) * -0.36f, z), V(0.2f, 0.09f, 0.022f), dark, V(-20f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.24f * s, 0.46f, 0.06f));
            b.Limb(arm, V(0.24f * s, 0.46f, 0.06f), V(0.26f * s, 0.32f, 0.18f), 0.05f, 0.045f, brown);
        });

        int head = b.Head(V(0, 0.62f, 0.02f));
        b.Ell(head, V(0, 0.74f, 0.04f), V(0.22f, 0.19f, 0.19f), brown);
        b.Ell(head, V(0, 0.69f, 0.15f), V(0.15f, 0.1f, 0.09f), tan);
        b.Ell(head, V(0, 0.74f, 0.23f), V(0.04f, 0.03f, 0.03f), dark, mat: Scales, blend: 0.01f);
        PokeBuilder.Both(s => b.Box(head, V(0.022f * s, 0.615f, 0.215f), V(0.019f, 0.038f, 0.015f), 0.01f, White, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.15f * s, 0.86f, 0.02f));
            b.Ell(ear, V(0.17f * s, 0.9f, 0.0f), V(0.06f, 0.06f, 0.035f), dark);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.09f * s, 0.8f, 0.19f), V(0.45f * s, 0.1f, 1f), 0.028f));
        return b;
    }

    // ------------------------------------------------------------------ Shinx line

    /// <summary>A four-pointed star of flattened spikes (the tip of the Shinx line's tails).</summary>
    internal static void StarTip(PokeBuilder b, int bone, Vector3 at, float r, Color color)
    {
        foreach (var d in new[] { V(1, 0, 0), V(-1, 0, 0), V(0, 1, 0), V(0, -1, 0) })
            b.Spike(bone, at, at + d * r, r * 0.45f, color, 0.5f, blend: r * 0.12f);
    }

    private static PokeBuilder Cat(int stage)
    {
        string name = stage switch { 0 => "Shinx", 1 => "Luxio", _ => "Luxray" };
        float s1 = 1f + stage * 0.22f;
        float legLen = 0.18f + stage * 0.08f;
        float by = legLen + 0.12f;
        var b = new PokeBuilder(name, stage switch { 0 => 0.58f, 1 => 0.74f, _ => 0.92f }, BodyPlan.Quadruped, V(0, by, -0.04f * s1)) { Coat = Fur };
        var blue = Rgb(98, 176, 236);
        var black = Rgb(48, 48, 62);
        var yellow = Rgb(252, 212, 60);

        // Legs: blue in front, black behind (Luxray's are mostly black with blue paws)
        foreach (var (z, front) in new[] { (0.14f, true), (-0.18f, false) })
            PokeBuilder.Both(s =>
            {
                var upper = stage == 2 ? black : (front ? blue : black);
                int leg = b.Leg(s, V(0.1f * s * s1, legLen + 0.1f, z * s1), front);
                b.Limb(leg, V(0.1f * s * s1, legLen + 0.06f, z * s1), V(0.1f * s * s1, 0.05f, (z + 0.02f) * s1), 0.055f * s1, 0.05f * s1, upper);
                b.Ell(leg, V(0.1f * s * s1, 0.04f, (z + 0.04f) * s1), V(0.06f * s1, 0.04f * s1, 0.07f * s1), front || stage == 2 ? blue : black);
                if (front) b.Torus(leg, V(0.1f * s * s1, 0.15f + stage * 0.05f, (z + 0.01f) * s1), 0.055f * s1, 0.016f * s1, yellow);
            });
        b.Ell(Body, V(0, by, 0.05f * s1), V(0.16f * s1, 0.15f * s1, 0.19f * s1), stage == 2 ? black : blue);
        b.Ell(Body, V(0, by, -0.16f * s1), V(0.15f * s1, 0.14f * s1, 0.18f * s1), black);
        if (stage == 2) b.PaintEll(Body, V(0, by - 0.06f, 0.0f), V(0.12f * s1, 0.08f * s1, 0.26f * s1), blue);

        // Tail with the yellow star
        int tail = b.Tail(V(0, by + 0.04f, -0.3f * s1));
        b.Limb(tail, V(0, by + 0.04f, -0.3f * s1), V(0, by + 0.2f * s1, -0.46f * s1), 0.025f, 0.02f, black, blend: 0.01f);
        b.Limb(tail, V(0, by + 0.2f * s1, -0.46f * s1), V(0, by + 0.34f * s1, -0.42f * s1), 0.02f, 0.02f, black, blend: 0.01f);
        StarTip(b, tail, V(0, by + 0.38f * s1, -0.42f * s1), 0.09f * s1, yellow);

        int head = b.Head(V(0, by + 0.1f, 0.14f * s1));
        float hy = by + 0.2f * s1;
        float hr = stage == 0 ? 0.22f : 0.2f * s1;
        b.Ell(head, V(0, hy, 0.2f * s1), V(hr, hr * 0.92f, hr * 0.95f), blue);
        b.Ell(head, V(0, hy - hr * 0.35f, 0.2f * s1 + hr * 0.65f), V(hr * 0.35f, hr * 0.25f, hr * 0.22f), PixelCanvas.Light1(blue, 0.25f));
        b.Mark(head, V(0, hy - hr * 0.2f, 0.2f * s1 + hr * 0.92f), V(0, 0.2f, 1f), 0.025f, 0.02f, black);
        // Mane: a tuft on Shinx, a shaggy black collar on the evolutions
        if (stage == 0)
        {
            b.Spike(head, V(0, hy + hr * 0.8f, 0.15f * s1), V(0, hy + hr * 1.25f, 0.25f * s1), 0.07f, black);
        }
        else
        {
            b.Ell(head, V(0, hy + hr * 0.3f, 0.06f * s1), V(hr * 1.15f, hr * 1.05f, hr * 0.9f), black);
            for (int i = 0; i < 5; i++)
            {
                float a = -0.9f + i * 0.45f;
                b.Spike(head, V(MathF.Sin(a) * hr * 0.9f, hy + hr * 0.2f, 0.02f * s1), V(MathF.Sin(a) * hr * 1.6f, hy + hr * (0.4f + 0.3f * MathF.Cos(a)), -0.18f * s1), 0.09f * s1, black);
            }
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(hr * 0.62f * s, hy + hr * 0.6f, 0.16f * s1));
            b.Ell(ear, V(hr * 0.78f * s, hy + hr * 0.95f, 0.16f * s1), V(0.07f * s1, 0.12f * s1, 0.04f), blue, V(0, 0, -25f * s));
            b.PaintEll(ear, V(hr * 0.8f * s, hy + hr * 0.95f, 0.19f * s1), V(0.05f * s1, 0.085f * s1, 0.04f), black, V(0, 0, -25f * s));
            b.Mark(ear, V(hr * 0.8f * s, hy + hr * 0.97f, 0.2f * s1), V(0.2f * s, 0.1f, 1f), 0.04f * s1, 0.04f * s1, yellow, MarkShape.Star, -25f * s);
        });
        PokeBuilder.Both(s => b.Eye(head, V(hr * 0.42f * s, hy + hr * 0.1f, 0.2f * s1 + hr * 0.82f), V(0.55f * s, 0.05f, 1f), 0.055f * s1,
            stage == 2 ? Rgb(250, 200, 50) : Rgb(250, 212, 60), pupil: stage == 2 ? Rgb(200, 40, 40) : null));
        return b;
    }

    // ------------------------------------------------------------------ Riolu line

    private static PokeBuilder Riolu()
    {
        var b = new PokeBuilder("Riolu", 0.6f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Fur };
        var blue = Rgb(84, 148, 222);
        var black = Rgb(44, 46, 62);
        var red = Rgb(214, 56, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.26f, 0));
            b.Limb(leg, V(0.08f * s, 0.26f, 0), V(0.09f * s, 0.06f, 0.02f), 0.06f, 0.055f, black);
            b.Ell(leg, V(0.09f * s, 0.035f, 0.05f), V(0.06f, 0.035f, 0.09f), black);
        });
        b.Ell(Body, V(0, 0.34f, 0), V(0.15f, 0.17f, 0.13f), blue);
        b.PaintEll(Body, V(0, 0.255f, 0.0f), V(0.18f, 0.075f, 0.16f), black);
        int tail = b.Tail(V(0, 0.26f, -0.12f));
        b.Ell(tail, V(0, 0.26f, -0.2f), V(0.05f, 0.06f, 0.09f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.43f, 0));
            b.Limb(arm, V(0.14f * s, 0.43f, 0), V(0.21f * s, 0.3f, 0.07f), 0.045f, 0.04f, blue);
            b.Ell(arm, V(0.22f * s, 0.27f, 0.08f), V(0.05f, 0.05f, 0.05f), black);
        });

        int head = b.Head(V(0, 0.5f, 0));
        float hy = 0.64f;
        b.Ell(head, V(0, hy, 0.02f), V(0.19f, 0.17f, 0.17f), blue);
        b.Ell(head, V(0, hy - 0.06f, 0.12f), V(0.08f, 0.06f, 0.08f), PixelCanvas.Light1(blue, 0.2f));
        b.Mark(head, V(0, hy - 0.04f, 0.19f), V(0, 0.1f, 1f), 0.02f, 0.016f, black);
        // Black mask across the eyes
        b.PaintEll(head, V(0, hy + 0.02f, 0.06f), V(0.23f, 0.06f, 0.18f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, hy + 0.12f, 0));
            b.Spike(ear, V(0.1f * s, hy + 0.1f, 0), V(0.2f * s, hy + 0.34f, -0.04f), 0.06f, blue, 0.5f);
            b.Spike(ear, V(0.105f * s, hy + 0.12f, 0.012f), V(0.19f * s, hy + 0.3f, -0.02f), 0.035f, black, 0.4f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.075f * s, hy + 0.02f, 0.17f), V(0.4f * s, 0.05f, 1f), 0.042f, red));
        return b;
    }

    private static PokeBuilder Lucario()
    {
        var b = new PokeBuilder("Lucario", 0.92f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var blue = Rgb(74, 132, 212);
        var black = Rgb(42, 44, 60);
        var cream = Rgb(236, 220, 162);
        var red = Rgb(214, 56, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.46f, 0));
            b.Limb(leg, V(0.1f * s, 0.46f, 0), V(0.12f * s, 0.24f, 0.04f), 0.075f, 0.06f, black);
            b.Limb(leg, V(0.12f * s, 0.24f, 0.04f), V(0.12f * s, 0.06f, 0.0f), 0.055f, 0.05f, blue);
            b.Ell(leg, V(0.12f * s, 0.035f, 0.05f), V(0.06f, 0.035f, 0.1f), blue);
        });
        b.Ell(Body, V(0, 0.62f, 0), V(0.16f, 0.22f, 0.13f), blue);
        b.PaintEll(Body, V(0, 0.64f, 0.06f), V(0.12f, 0.16f, 0.1f), cream);
        b.Spike(Body, V(0, 0.66f, 0.15f), V(0, 0.68f, 0.26f), 0.035f, Claw, mat: Shell);
        b.PaintEll(Body, V(0, 0.455f, 0), V(0.19f, 0.075f, 0.16f), black);
        int tail = b.Tail(V(0, 0.44f, -0.12f));
        b.Ell(tail, V(0, 0.4f, -0.24f), V(0.05f, 0.08f, 0.12f), blue, V(-30f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.78f, 0));
            b.Limb(arm, V(0.15f * s, 0.78f, 0), V(0.25f * s, 0.56f, 0.06f), 0.05f, 0.045f, black);
            b.Ell(arm, V(0.26f * s, 0.52f, 0.07f), V(0.05f, 0.055f, 0.05f), black);
            b.Spike(arm, V(0.26f * s, 0.54f, 0.02f), V(0.3f * s, 0.56f, -0.08f), 0.025f, Claw, mat: Shell);
        });

        int head = b.Head(V(0, 0.84f, 0));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.02f), V(0.15f, 0.14f, 0.15f), blue);
        b.Ell(head, V(0, hy - 0.05f, 0.13f), V(0.07f, 0.05f, 0.08f), black);
        b.PaintEll(head, V(0, hy + 0.02f, 0.06f), V(0.19f, 0.05f, 0.16f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, hy + 0.1f, 0));
            b.Spike(ear, V(0.08f * s, hy + 0.08f, 0), V(0.14f * s, hy + 0.28f, -0.03f), 0.05f, blue, 0.5f);
            // The black appendages that hang from the back of its head
            b.Limb(head, V(0.06f * s, hy, -0.1f), V(0.1f * s, hy - 0.16f, -0.18f), 0.025f, 0.02f, black, blend: 0.01f);
            b.Limb(head, V(0.04f * s, hy - 0.04f, -0.1f), V(0.07f * s, hy - 0.22f, -0.14f), 0.022f, 0.018f, black, blend: 0.01f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, hy + 0.02f, 0.14f), V(0.45f * s, 0.05f, 1f), 0.035f, red));
        return b;
    }

    // ------------------------------------------------------------------ Gible line

    private static PokeBuilder Gible()
    {
        var b = new PokeBuilder("Gible", 0.56f, BodyPlan.Biped, V(0, 0.27f, -0.02f)) { Coat = Scales };
        var navy = Rgb(82, 104, 164);
        var red = Rgb(218, 82, 70);
        var yellow = Rgb(250, 206, 70);
        var fin = Rgb(110, 136, 196);

        PokeBuilder.Both(s =>
        {
            int foot = b.Leg(s, V(0.1f * s, 0.12f, 0.03f));
            b.Ell(foot, V(0.1f * s, 0.06f, 0.03f), V(0.08f, 0.06f, 0.1f), navy);
        });
        b.Ell(Body, V(0, 0.27f, -0.02f), V(0.23f, 0.24f, 0.22f), navy);
        b.PaintEll(Body, V(0, 0.24f, 0.08f), V(0.17f, 0.17f, 0.15f), red);
        int tail = b.Tail(V(0, 0.18f, -0.18f));
        b.Ell(tail, V(0, 0.16f, -0.3f), V(0.06f, 0.06f, 0.12f), navy);
        b.Spike(tail, V(0, 0.2f, -0.36f), V(0, 0.3f, -0.42f), 0.04f, navy, 0.4f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.32f, 0.04f));
            b.Ell(arm, V(0.2f * s, 0.3f, 0.06f), V(0.05f, 0.07f, 0.06f), navy);
        });

        int head = b.Head(V(0, 0.4f, 0.02f));
        float hy = 0.58f;
        b.Ell(head, V(0, hy, 0.06f), V(0.28f, 0.2f, 0.27f), navy);
        // A wide mouth: the red lower jaw in front, the dark line of the mouth painted above it, teeth along it
        int jaw = b.Jaw(head, V(0, hy - 0.08f, -0.02f));
        b.Ell(jaw, V(0, hy - 0.115f, 0.15f), V(0.22f, 0.075f, 0.17f), red, blend: 0.014f);
        b.PaintEll(head, V(0, hy - 0.055f, 0.2f), V(0.25f, 0.024f, 0.16f), Rgb(110, 36, 48));
        for (int i = -2; i <= 2; i++)
            b.Spike(head, V(i * 0.06f, hy - 0.04f, 0.27f - MathF.Abs(i) * 0.02f), V(i * 0.06f, hy - 0.1f, 0.28f - MathF.Abs(i) * 0.02f), 0.018f, Claw, mat: Shell, blend: 0.004f);
        // Dorsal fin with a notch
        b.Spike(head, V(0, hy + 0.14f, 0.0f), V(0, hy + 0.36f, -0.06f), 0.1f, navy, 0.35f);
        b.Mark(head, V(0, hy + 0.3f, 0.02f), V(0.05f, 0.2f, 1f), 0.02f, 0.04f, fin);
        PokeBuilder.Both(s => b.Ell(head, V(0.2f * s, hy + 0.08f, 0.02f), V(0.05f, 0.05f, 0.04f), fin, blend: 0.015f));
        PokeBuilder.Both(s => b.Eye(head, V(0.12f * s, hy + 0.06f, 0.24f), V(0.45f * s, 0.2f, 1f), 0.045f, yellow));
        return b;
    }

    private static PokeBuilder Garchomp(bool big)
    {
        var b = new PokeBuilder(big ? "Garchomp" : "Gabite", big ? 0.95f : 0.76f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Scales };
        var navy = big ? Rgb(64, 80, 140) : Rgb(84, 90, 162);
        var red = Rgb(212, 70, 64);
        var yellow = Rgb(250, 206, 70);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.46f, -0.02f));
            b.Limb(leg, V(0.12f * s, 0.46f, -0.02f), V(0.16f * s, 0.22f, 0.06f), 0.1f, 0.08f, navy);
            b.Limb(leg, V(0.16f * s, 0.22f, 0.06f), V(0.15f * s, 0.05f, 0.0f), 0.07f, 0.06f, navy);
            b.Ell(leg, V(0.15f * s, 0.035f, 0.05f), V(0.07f, 0.035f, 0.1f), navy);
            b.Spike(leg, V(0.2f * s, 0.28f, 0.0f), V(0.3f * s, 0.3f, -0.08f), 0.035f, Claw, mat: Shell);
        });
        b.Ell(Body, V(0, 0.62f, 0), V(0.2f, 0.26f, 0.17f), navy);
        b.PaintEll(Body, V(0, 0.58f, 0.07f), V(0.14f, 0.22f, 0.12f), red);
        int tail = b.Tail(V(0, 0.44f, -0.12f));
        b.Limb(tail, V(0, 0.44f, -0.12f), V(0, 0.2f, -0.42f), 0.09f, 0.05f, navy);
        b.Spike(tail, V(0, 0.34f, -0.26f), V(0, 0.46f, -0.36f), 0.05f, navy, 0.35f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.78f, 0));
            b.Limb(arm, V(0.18f * s, 0.78f, 0), V(0.3f * s, 0.6f, 0.08f), 0.06f, 0.05f, navy);
            // Scythe-like fins on the arms
            b.Ell(arm, V(0.36f * s, 0.62f, 0.02f), V(0.03f, 0.2f, 0.12f), navy, V(20f, 0, 25f * s), blend: 0.015f);
            b.Spike(arm, V(0.32f * s, 0.54f, 0.12f), V(0.36f * s, 0.46f, 0.2f), 0.025f, Claw, mat: Shell);
        });

        int head = b.Head(V(0, 0.86f, 0));
        float hy = 0.98f;
        // Streamlined head with fins on the sides and the yellow star on the snout
        b.Ell(head, V(0, hy, 0.08f), V(0.14f, 0.1f, 0.22f), navy);
        int jaw = b.Jaw(head, V(0, hy - 0.04f, 0.0f));
        b.Ell(jaw, V(0, hy - 0.06f, 0.14f), V(0.11f, 0.04f, 0.14f), red, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(head, V(0.18f * s, hy + 0.02f, 0.0f), V(0.12f, 0.03f, 0.12f), navy, V(0, 30f * s, -20f * s), blend: 0.015f));
        if (big) b.Mark(head, V(0, hy + 0.08f, 0.14f), V(0, 1f, 0.35f), 0.045f, 0.045f, yellow, MarkShape.Star);
        b.Spike(head, V(0, hy + 0.06f, -0.06f), V(0, hy + 0.12f, -0.26f), 0.05f, navy, 0.4f);
        PokeBuilder.Both(s => b.Eye(head, V(0.09f * s, hy + 0.03f, 0.2f), V(0.7f * s, 0.05f, 1f), 0.03f, yellow));
        return b;
    }

    // ------------------------------------------------------------------ Giratina

    private static PokeBuilder Giratina()
    {
        var b = new PokeBuilder("Giratina", 1f, BodyPlan.Floating, V(0, 0.44f, -0.05f)) { Coat = Scales }.Hover();
        var gray = Rgb(168, 168, 180);
        var black = Rgb(40, 36, 50);
        var red = Rgb(214, 56, 62);
        var gold = Rgb(236, 196, 72);

        // Six legs under a long body striped black and red
        foreach (float z in new[] { 0.2f, -0.05f, -0.3f })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.16f * s, 0.36f, z), z > 0f);
                b.Limb(leg, V(0.16f * s, 0.34f, z), V(0.24f * s, 0.06f, z + 0.06f), 0.06f, 0.045f, gray);
                b.Spike(leg, V(0.24f * s, 0.05f, z + 0.08f), V(0.25f * s, 0.02f, z + 0.18f), 0.03f, gold, mat: Metal);
            });
        b.Ell(Body, V(0, 0.44f, -0.05f), V(0.24f, 0.2f, 0.46f), gray);
        foreach (float z in new[] { 0.12f, -0.02f, -0.16f, -0.3f })
        {
            b.PaintTorus(Body, V(0, 0.44f, z), 0.22f, 0.03f, black, V(90, 0, 0), 1.1f, 0.95f);
            b.PaintTorus(Body, V(0, 0.44f, z - 0.05f), 0.22f, 0.017f, red, V(90, 0, 0), 1.1f, 0.95f);
        }
        int tail = b.Tail(V(0, 0.44f, -0.45f));
        b.Limb(tail, V(0, 0.44f, -0.45f), V(0, 0.3f, -0.8f), 0.1f, 0.03f, gray);

        // Shadowy wings with red spikes
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.2f * s, 0.62f, 0.05f));
            for (int i = 0; i < 3; i++)
            {
                float a = 30f + i * 26f;
                var tip = V(s * (0.35f + i * 0.14f), 0.62f + 0.36f - i * 0.2f, -0.1f - i * 0.1f);
                b.Limb(wing, V(0.22f * s, 0.64f, 0.02f - i * 0.05f), tip, 0.06f, 0.025f, black);
                b.Spike(wing, tip, tip + V(0.1f * s, 0.06f, -0.04f), 0.04f, red, mat: Shell);
                b.Ell(wing, (V(0.22f * s, 0.64f, 0.02f) + tip) / 2f, V(0.05f, 0.1f, 0.03f), black, V(0, 0, -a * s));
            }
        });

        int head = b.Head(V(0, 0.6f, 0.36f));
        b.Limb(head, V(0, 0.56f, 0.34f), V(0, 0.74f, 0.52f), 0.12f, 0.1f, gray);
        b.Torus(head, V(0, 0.66f, 0.44f), 0.12f, 0.035f, gold, V(-50, 0, 0), mat: Metal);
        b.Ell(head, V(0, 0.8f, 0.6f), V(0.13f, 0.11f, 0.15f), gray);
        // Golden crest and jaw guards
        b.Ell(head, V(0, 0.88f, 0.56f), V(0.14f, 0.05f, 0.15f), gold, V(-15, 0, 0), Metal, 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.08f * s, 0.9f, 0.5f), V(0.2f * s, 1.04f, 0.4f), 0.04f, gold, mat: Metal);
            b.Spike(head, V(0.1f * s, 0.74f, 0.66f), V(0.16f * s, 0.66f, 0.74f), 0.03f, gold, mat: Metal);
        });
        b.Ell(head, V(0, 0.76f, 0.72f), V(0.07f, 0.04f, 0.06f), black, blend: 0.01f);
        PokeBuilder.Both(s => b.Eye(head, V(0.07f * s, 0.84f, 0.7f), V(0.6f * s, 0.1f, 1f), 0.03f, red));
        return b;
    }

    // ------------------------------------------------------------------ stand-ins and samples

    private static PokeBuilder Generic(string species)
    {
        var b = new PokeBuilder(species, 0.65f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var col = Rgb(168, 168, 120);
        b.Ell(Body, V(0, 0.28f, 0), V(0.26f, 0.26f, 0.24f), col);
        int head = b.Head(V(0, 0.46f, 0));
        b.Ell(head, V(0, 0.64f, 0.04f), V(0.2f, 0.19f, 0.18f), col);
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s, 0.66f, 0.19f), V(0.4f * s, 0.05f, 1f), 0.04f));
        return b;
    }

    /// <summary>A snake coiled on the ground with its head raised: the serpent plan's skeleton, a chain of segments.</summary>
    private static PokeBuilder SampleSerpent()
    {
        var b = new PokeBuilder("Sample Serpent", 0.8f, BodyPlan.Serpent, V(0, 0.1f, -0.3f)) { Coat = Scales };
        var teal = Rgb(70, 156, 150);
        var belly = Rgb(236, 220, 160);
        var points = new[] { V(0, 0.09f, -0.3f), V(0.18f, 0.09f, -0.12f), V(0.12f, 0.1f, 0.1f), V(-0.06f, 0.14f, 0.18f), V(-0.08f, 0.32f, 0.22f), V(0, 0.5f, 0.24f) };
        int parent = Body;
        for (int i = 1; i < points.Length - 1; i++)
        {
            int seg = b.Part("seg" + i, parent, points[i], PokeRole.Segment, i);
            b.Limb(seg, points[i], points[i + 1], 0.09f - i * 0.006f, 0.085f - i * 0.006f, teal);
            parent = seg;
        }
        b.Limb(Body, points[0], points[1], 0.07f, 0.09f, teal);
        int tail = b.Tail(points[0]);
        b.Limb(tail, points[0], V(-0.2f, 0.06f, -0.42f), 0.07f, 0.02f, teal);
        int head = b.Head(points[^1], parent);
        b.Ell(head, V(0, 0.56f, 0.3f), V(0.13f, 0.1f, 0.16f), teal);
        b.PaintEll(head, V(0, 0.5f, 0.32f), V(0.1f, 0.05f, 0.15f), belly);
        int jaw = b.Jaw(head, V(0, 0.52f, 0.22f));
        b.Ell(jaw, V(0, 0.5f, 0.34f), V(0.09f, 0.035f, 0.11f), belly, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s, 0.6f, 0.4f), V(0.6f * s, 0.2f, 1f), 0.035f, Rgb(240, 200, 60)));
        return b;
    }

    /// <summary>A fish hanging in the air: body, tail fin on two bones, side fins.</summary>
    private static PokeBuilder SampleFish()
    {
        var b = new PokeBuilder("Sample Fish", 0.7f, BodyPlan.Fish, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var orange = Rgb(240, 120, 60);
        var cream = Rgb(250, 230, 200);
        b.Ell(Body, V(0, 0.4f, 0), V(0.16f, 0.24f, 0.34f), orange);
        b.PaintEll(Body, V(0, 0.3f, 0.06f), V(0.14f, 0.14f, 0.3f), cream);
        int tail = b.Tail(V(0, 0.4f, -0.28f));
        b.Limb(tail, V(0, 0.4f, -0.28f), V(0, 0.4f, -0.42f), 0.08f, 0.05f, orange);
        int fin = b.Part("tailFin", tail, V(0, 0.4f, -0.42f), PokeRole.Fin);
        b.Ell(fin, V(0, 0.5f, -0.52f), V(0.025f, 0.14f, 0.07f), orange, V(-30f, 0, 0));
        b.Ell(fin, V(0, 0.3f, -0.52f), V(0.025f, 0.14f, 0.07f), orange, V(30f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.14f * s, 0.34f, 0.08f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.2f * s, 0.32f, 0.04f), V(0.08f, 0.02f, 0.06f), orange, V(0, 0, 25f * s));
        });
        int head = b.Head(V(0, 0.42f, 0.18f));
        b.Ell(head, V(0, 0.42f, 0.24f), V(0.14f, 0.2f, 0.12f), orange);
        PokeBuilder.Both(s => b.Eye(head, V(0.12f * s, 0.48f, 0.26f), V(1f * s, 0.1f, 0.4f), 0.05f, sclera: true));
        b.Mark(head, V(0, 0.36f, 0.355f), V(0, 0, 1f), 0.04f, 0.015f, Rgb(150, 50, 40), MarkShape.Bar);
        return b;
    }

    /// <summary>A ball floating in the air with two small arms and a wisp of a tail.</summary>
    private static PokeBuilder SampleFloating()
    {
        var b = new PokeBuilder("Sample Floating", 0.6f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Fur }.Hover();
        var lilac = Rgb(170, 140, 220);
        b.Ell(Body, V(0, 0.45f, 0), V(0.26f, 0.25f, 0.25f), lilac);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.42f, 0.04f));
            b.Limb(arm, V(0.22f * s, 0.42f, 0.04f), V(0.34f * s, 0.34f, 0.08f), 0.05f, 0.04f, lilac);
        });
        int tail = b.Tail(V(0, 0.28f, -0.12f));
        b.Limb(tail, V(0, 0.28f, -0.12f), V(0.06f, 0.14f, -0.24f), 0.08f, 0.02f, lilac);
        // The ball is its face too: no head of its own
        b.Mark(Body, V(0, 0.4f, 0.245f), V(0, -0.2f, 1f), 0.05f, 0.02f, Rgb(120, 60, 120), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(Body, V(0.09f * s, 0.5f, 0.235f), V(0.35f * s, 0.05f, 1f), 0.06f, Rgb(200, 60, 90)));
        return b;
    }
}
