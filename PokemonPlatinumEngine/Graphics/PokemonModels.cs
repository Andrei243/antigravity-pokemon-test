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

internal enum MarkShape { Disc, Ring, Star, Bar, Star5, Wave, Zigzag, Triangle, Diamond, Smile }

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

    /// <summary>Eyes that are always shut (Abra's), squeezed tighter by a hit.</summary>
    public bool Closed;

    /// <summary>The colour of an eye with a <see cref="Sclera"/> round its pupil (white when null): Gengar's red, Mismagius's yellow.</summary>
    public Color? White;

    /// <summary>Eyes that always glare, the lid pressed down as the fierce state's is (the Gastly line, Honchkrow).</summary>
    public bool Glare;

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

    /// <summary>
    /// A tube through <paramref name="points"/> (antennae, whiskers, curls, coiled tails): a limb from each point to
    /// the next, its radius going from <paramref name="ra"/> at the first point to <paramref name="rb"/> at the last.
    /// </summary>
    public void Tube(int bone, Vector3[] points, float ra, float rb, Color color, SurfaceMaterial? mat = null, float blend = 0.008f)
    {
        float length = 0f;
        for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
        float run = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            float step = Vector3.Distance(points[i - 1], points[i]);
            float r0 = ra + (rb - ra) * run / length, r1 = ra + (rb - ra) * (run + step) / length;
            Limb(bone, points[i - 1], points[i], r0, r1, color, mat, blend);
            run += step;
        }
    }

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
    /// Carves an ellipsoid out of every shape added so far (a gaping mouth, the scallops of a bat's wing). Shapes
    /// added after it are whole.
    /// </summary>
    public SdfPrimitive Cut(int bone, Vector3 center, Vector3 radii, Vector3 rotationDeg = default, float blend = 0.006f) =>
        sdf.Ellipsoid(center, radii, Color.White, bone, blend, Coat, Rot(rotationDeg), SdfOp.Cut);

    /// <summary>Carves a box out of every shape added so far (a straight edge); <paramref name="rotation"/> turns it.</summary>
    public SdfPrimitive CutBox(int bone, Vector3 center, Vector3 half, Quaternion rotation, float blend = 0f) =>
        sdf.Box(center, half, 0f, Color.White, bone, blend, Coat, rotation, SdfOp.Cut);

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
    /// <paramref name="size"/> is half the eye's height; <paramref name="closed"/> eyes are always shut and
    /// <paramref name="glare"/> ones always fierce; a sclera is white unless another colour is given.
    /// </summary>
    public void Eye(int bone, Vector3 at, Vector3 facing, float size, Color? iris = null, bool sclera = false, Color? pupil = null, bool closed = false,
        Color? white = null, bool glare = false) =>
        m.Decals.Add(new PokeDecal
        {
            Bone = bone, Center = at, Normal = Vector3.Normalize(facing), Half = new Vector2(size * 1.6f, size * 1.8f), IsEye = true,
            Size = size, Iris = iris, Sclera = sclera || white != null, Pupil = pupil, Closed = closed, White = white, Glare = glare
        });

    /// <summary>A marking painted on the surface (a dot, ring, star, bar, wavy line, zigzag, triangle, diamond or smile) with radii <paramref name="rx"/> and <paramref name="ry"/>.</summary>
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
internal static partial class PokemonModels
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
                    .Append(d.Iris?.R).Append(d.Iris?.G).Append(d.Iris?.B).Append(d.Sclera).Append(d.Closed ? "closed" : "").Append(d.White is { } w ? $"white{w.R},{w.G},{w.B}" : "").Append(d.Glare ? "glare" : "").Append(d.Pupil?.R).Append(d.Shape).Append(d.Color.R).Append(d.Color.G).Append(d.Color.B);
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
        "Gible", "Gabite", "Garchomp", "Giratina", "Buneary",
        // Plan 03 · D6, batch 1: the Sinnoh Pokédex from Kricketot to Lopunny
        "Kricketot", "Kricketune", "Abra", "Kadabra", "Alakazam", "Magikarp", "Gyarados", "Budew", "Roselia", "Roserade",
        "Zubat", "Golbat", "Crobat", "Geodude", "Graveler", "Golem", "Onix", "Steelix",
        "Cranidos", "Rampardos", "Shieldon", "Bastiodon", "Machop", "Machoke", "Machamp", "Psyduck", "Golduck",
        "Burmy", "Wormadam", "Mothim", "Wurmple", "Silcoon", "Beautifly", "Cascoon", "Dustox",
        "Combee", "Vespiquen", "Pachirisu", "Buizel", "Floatzel", "Cherubi", "Cherrim", "Shellos", "Gastrodon",
        "Heracross", "Aipom", "Ambipom", "Drifloon", "Drifblim", "Lopunny",
        // Plan 03 · D7, batch 2: the Sinnoh Pokédex from Gastly to Hippowdon
        "Gastly", "Haunter", "Gengar", "Misdreavus", "Mismagius", "Murkrow", "Honchkrow", "Glameow", "Purugly",
        "Goldeen", "Seaking", "Barboach", "Whiscash", "Chingling", "Chimecho", "Stunky", "Skuntank", "Meditite", "Medicham",
        "Bronzor", "Bronzong", "Ponyta", "Rapidash", "Bonsly", "Sudowoodo", "Mime Jr.", "Mr. Mime", "Happiny", "Chansey", "Blissey",
        "Cleffa", "Clefairy", "Clefable", "Chatot", "Pichu", "Pikachu", "Raichu", "Hoothoot", "Noctowl", "Spiritomb",
        "Munchlax", "Snorlax", "Unown", "Wooper", "Quagsire", "Wingull", "Pelipper", "Girafarig", "Hippopotas", "Hippowdon",
        // Plan 03 · D8, batch 3: the Sinnoh Pokédex from Azurill to Magnezone, but for its legendaries
        "Azurill", "Marill", "Azumarill", "Skorupi", "Drapion", "Croagunk", "Toxicroak", "Carnivine", "Remoraid", "Octillery",
        "Finneon", "Lumineon", "Tentacool", "Tentacruel", "Feebas", "Milotic", "Mantyke", "Mantine", "Snover", "Abomasnow",
        "Sneasel", "Weavile", "Rotom", "Gligar", "Gliscor", "Nosepass", "Probopass", "Ralts", "Kirlia", "Gardevoir", "Gallade",
        "Lickitung", "Lickilicky", "Eevee", "Vaporeon", "Jolteon", "Flareon", "Espeon", "Umbreon", "Leafeon", "Glaceon",
        "Swablu", "Altaria", "Togepi", "Togetic", "Togekiss", "Houndour", "Houndoom", "Magnemite", "Magneton", "Magnezone",
        // Plan 03 · D9, batch 4: the rest of the Sinnoh Pokédex, its legendaries and Tangela to Absol
        "Uxie", "Mesprit", "Azelf", "Dialga", "Palkia", "Manaphy", "Tangela", "Tangrowth", "Yanma", "Yanmega",
        "Tropius", "Rhyhorn", "Rhydon", "Rhyperior", "Duskull", "Dusclops", "Dusknoir", "Porygon", "Porygon2", "Porygon-Z",
        "Scyther", "Scizor", "Elekid", "Electabuzz", "Electivire", "Magby", "Magmar", "Magmortar", "Swinub", "Piloswine",
        "Mamoswine", "Snorunt", "Glalie", "Froslass", "Absol",
        // Popular species from outside the Sinnoh Pokédex (plan 03, decision 3): Kanto's first, Bulbasaur to Vileplume
        "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard", "Squirtle", "Wartortle", "Blastoise",
        "Caterpie", "Metapod", "Butterfree", "Weedle", "Kakuna", "Beedrill",
        "Pidgey", "Pidgeotto", "Pidgeot", "Rattata", "Raticate", "Spearow", "Fearow", "Ekans", "Arbok",
        "Sandshrew", "Sandslash", "Nidoran♀", "Nidorina", "Nidoqueen", "Nidoran♂", "Nidorino", "Nidoking",
        "Vulpix", "Ninetales", "Jigglypuff", "Wigglytuff", "Oddish", "Gloom", "Vileplume",
        // Kanto's second batch, Paras to Kingler
        "Paras", "Parasect", "Venonat", "Venomoth", "Diglett", "Dugtrio", "Meowth", "Persian", "Mankey", "Primeape",
        "Growlithe", "Arcanine", "Poliwag", "Poliwhirl", "Poliwrath", "Bellsprout", "Weepinbell", "Victreebel",
        "Slowpoke", "Slowbro", "Farfetch'd", "Doduo", "Dodrio", "Seel", "Dewgong", "Grimer", "Muk", "Shellder", "Cloyster",
        "Drowzee", "Hypno", "Krabby", "Kingler",
        // Kanto's third batch, Voltorb to Mew
        "Voltorb", "Electrode", "Exeggcute", "Exeggutor", "Cubone", "Marowak", "Hitmonlee", "Hitmonchan", "Koffing", "Weezing",
        "Kangaskhan", "Horsea", "Seadra", "Staryu", "Starmie", "Jynx", "Pinsir", "Tauros", "Lapras", "Ditto",
        "Omanyte", "Omastar", "Kabuto", "Kabutops", "Aerodactyl", "Articuno", "Zapdos", "Moltres", "Dratini", "Dragonair",
        "Dragonite", "Mewtwo", "Mew",
        // Johto's first batch, Chikorita to Corsola
        "Chikorita", "Bayleef", "Meganium", "Cyndaquil", "Quilava", "Typhlosion", "Totodile", "Croconaw", "Feraligatr",
        "Sentret", "Furret", "Ledyba", "Ledian", "Spinarak", "Ariados", "Chinchou", "Lanturn", "Igglybuff", "Natu", "Xatu",
        "Mareep", "Flaaffy", "Ampharos", "Bellossom", "Politoed", "Hoppip", "Skiploom", "Jumpluff", "Sunkern", "Sunflora",
        "Slowking", "Wobbuffet", "Pineco", "Forretress", "Dunsparce", "Snubbull", "Granbull", "Qwilfish", "Shuckle",
        "Teddiursa", "Ursaring", "Slugma", "Magcargo", "Corsola",
        // Johto's second batch, Delibird to Celebi
        "Delibird", "Skarmory", "Kingdra", "Phanpy", "Donphan", "Stantler", "Smeargle", "Tyrogue", "Hitmontop", "Smoochum", "Miltank",
        "Raikou", "Entei", "Suicune", "Larvitar", "Pupitar", "Tyranitar", "Lugia", "Ho-Oh", "Celebi",
        // Hoenn's first batch, Treecko to Delcatty
        "Treecko", "Grovyle", "Sceptile", "Torchic", "Combusken", "Blaziken", "Mudkip", "Marshtomp", "Swampert", "Poochyena",
        "Mightyena", "Zigzagoon", "Linoone", "Lotad", "Lombre", "Ludicolo", "Seedot", "Nuzleaf", "Shiftry", "Taillow", "Swellow",
        "Surskit", "Masquerain", "Shroomish", "Breloom", "Slakoth", "Vigoroth", "Slaking", "Nincada", "Ninjask", "Shedinja", "Whismur",
        "Loudred", "Exploud", "Makuhita", "Hariyama", "Skitty", "Delcatty",
        // Hoenn's second batch, Sableye to Armaldo
        "Sableye", "Mawile", "Aron", "Lairon", "Aggron", "Electrike", "Manectric", "Plusle", "Minun", "Volbeat", "Illumise",
        "Gulpin", "Swalot", "Carvanha", "Sharpedo", "Wailmer", "Wailord", "Numel", "Camerupt", "Torkoal", "Spoink", "Grumpig",
        "Spinda", "Trapinch", "Vibrava", "Flygon", "Cacnea", "Cacturne", "Zangoose", "Seviper", "Lunatone", "Solrock", "Corphish",
        "Crawdaunt", "Baltoy", "Claydol", "Lileep", "Cradily", "Anorith", "Armaldo",
        // Hoenn's third batch, Castform to Deoxys
        "Castform", "Kecleon", "Shuppet", "Banette", "Wynaut", "Spheal", "Sealeo", "Walrein", "Clamperl", "Huntail", "Gorebyss",
        "Relicanth", "Luvdisc", "Bagon", "Shelgon", "Salamence", "Beldum", "Metang", "Metagross", "Regirock", "Regice", "Registeel",
        "Latias", "Latios", "Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys",
        // Unova's first batch, Victini to Audino
        "Victini", "Snivy", "Servine", "Serperior", "Tepig", "Pignite", "Emboar", "Oshawott", "Dewott", "Samurott", "Patrat",
        "Watchog", "Lillipup", "Herdier", "Stoutland", "Purrloin", "Liepard", "Pansage", "Simisage", "Pansear", "Simisear", "Panpour",
        "Simipour", "Munna", "Musharna", "Pidove", "Tranquill", "Unfezant", "Blitzle", "Zebstrika", "Roggenrola", "Boldore", "Gigalith",
        "Woobat", "Swoobat", "Drilbur", "Excadrill", "Audino",
        // Unova's second batch, Timburr to Zoroark
        "Timburr", "Gurdurr", "Conkeldurr", "Tympole", "Palpitoad", "Seismitoad", "Throh", "Sawk", "Sewaddle", "Swadloon", "Leavanny",
        "Venipede", "Whirlipede", "Scolipede", "Cottonee", "Whimsicott", "Petilil", "Lilligant", "Basculin", "Sandile", "Krokorok",
        "Krookodile", "Darumaka", "Darmanitan", "Maractus", "Dwebble", "Crustle", "Scraggy", "Scrafty", "Sigilyph", "Yamask", "Cofagrigus",
        "Tirtouga", "Carracosta", "Archen", "Archeops", "Trubbish", "Garbodor", "Zorua", "Zoroark",
        // Unova's third batch, Minccino to Chandelure
        "Minccino", "Cinccino", "Gothita", "Gothorita", "Gothitelle", "Solosis", "Duosion", "Reuniclus", "Ducklett", "Swanna", "Vanillite",
        "Vanillish", "Vanilluxe", "Deerling", "Sawsbuck", "Emolga", "Karrablast", "Escavalier", "Foongus", "Amoonguss", "Frillish", "Jellicent",
        "Alomomola", "Joltik", "Galvantula", "Ferroseed", "Ferrothorn", "Klink", "Klang", "Klinklang", "Tynamo", "Eelektrik", "Eelektross",
        "Elgyem", "Beheeyem", "Litwick", "Lampent", "Chandelure",
        // Unova's fourth batch, Axew to Genesect
        "Axew", "Fraxure", "Haxorus", "Cubchoo", "Beartic", "Cryogonal", "Shelmet", "Accelgor", "Stunfisk", "Mienfoo", "Mienshao", "Druddigon", "Golett", "Golurk", "Pawniard", "Bisharp", "Bouffalant",
        "Rufflet", "Braviary", "Vullaby", "Mandibuzz", "Heatmor", "Durant", "Deino", "Zweilous", "Hydreigon", "Larvesta", "Volcarona",
        "Cobalion", "Terrakion", "Virizion", "Tornadus", "Thundurus", "Reshiram", "Zekrom", "Landorus", "Kyurem", "Keldeo", "Meloetta", "Genesect",
        // Kalos's first batch, Chespin to Slurpuff
        "Chespin", "Quilladin", "Chesnaught", "Fennekin", "Braixen", "Delphox", "Froakie", "Frogadier", "Greninja", "Bunnelby",
        "Diggersby", "Fletchling", "Fletchinder", "Talonflame", "Scatterbug", "Spewpa", "Vivillon", "Litleo", "Pyroar", "Flabébé",
        "Floette", "Florges", "Skiddo", "Gogoat", "Pancham", "Pangoro", "Furfrou", "Espurr", "Meowstic", "Honedge", "Doublade",
        "Aegislash", "Spritzee", "Aromatisse", "Swirlix", "Slurpuff",
        // Kalos's second batch, Inkay to Volcanion
        "Inkay", "Malamar", "Binacle", "Barbaracle", "Skrelp", "Dragalge", "Clauncher", "Clawitzer", "Helioptile", "Heliolisk",
        "Tyrunt", "Tyrantrum", "Amaura", "Aurorus", "Sylveon", "Hawlucha", "Dedenne", "Carbink", "Goomy", "Sliggoo", "Goodra", "Klefki",
        "Phantump", "Trevenant", "Pumpkaboo", "Gourgeist", "Bergmite", "Avalugg", "Noibat", "Noivern", "Xerneas", "Yveltal", "Zygarde",
        "Diancie", "Hoopa", "Volcanion",
        // Alola's first batch, Rowlet to Bewear
        "Rowlet", "Dartrix", "Decidueye", "Litten", "Torracat", "Incineroar", "Popplio", "Brionne", "Primarina", "Pikipek", "Trumbeak",
        "Toucannon", "Yungoos", "Gumshoos", "Grubbin", "Charjabug", "Vikavolt", "Crabrawler", "Crabominable", "Oricorio", "Cutiefly", "Ribombee",
        "Rockruff", "Lycanroc", "Wishiwashi", "Mareanie", "Toxapex", "Mudbray", "Mudsdale", "Dewpider", "Araquanid", "Fomantis", "Lurantis",
        "Morelull", "Shiinotic", "Salandit", "Salazzle", "Stufful", "Bewear",
        // Alola's second batch, Bounsweet to Kommo-o
        "Bounsweet", "Steenee", "Tsareena", "Comfey", "Oranguru", "Passimian", "Wimpod", "Golisopod", "Sandygast", "Palossand",
        "Pyukumuku", "Type: Null", "Silvally", "Minior", "Komala", "Turtonator", "Togedemaru", "Mimikyu", "Bruxish", "Drampa", "Dhelmise",
        "Jangmo-o", "Hakamo-o", "Kommo-o",
        // Alola's third batch, Tapu Koko to Melmetal
        "Tapu Koko", "Tapu Lele", "Tapu Bulu", "Tapu Fini", "Cosmog", "Cosmoem", "Solgaleo", "Lunala", "Nihilego", "Buzzwole", "Pheromosa",
        "Xurkitree", "Celesteela", "Kartana", "Guzzlord", "Necrozma", "Magearna", "Marshadow", "Poipole", "Naganadel", "Stakataka",
        "Blacephalon", "Zeraora", "Meltan", "Melmetal",
        // Sinnoh's last seven, outside Platinum's Sinnoh Pokédex
        "Heatran", "Regigigas", "Cresselia", "Phione", "Darkrai", "Shaymin", "Arceus",
        // Galar's first batch, Grookey to Eldegoss
        "Grookey", "Thwackey", "Rillaboom", "Scorbunny", "Raboot", "Cinderace", "Sobble", "Drizzile", "Inteleon", "Skwovet", "Greedent",
        "Rookidee", "Corvisquire", "Corviknight", "Blipbug", "Dottler", "Orbeetle", "Nickit", "Thievul", "Gossifleur", "Eldegoss",
        // Galar's second batch, Wooloo to Polteageist
        "Wooloo", "Dubwool", "Chewtle", "Drednaw", "Yamper", "Boltund", "Rolycoly", "Carkol", "Coalossal", "Applin", "Flapple", "Appletun",
        "Silicobra", "Sandaconda", "Cramorant", "Arrokuda", "Barraskewda", "Toxel", "Toxtricity", "Sizzlipede", "Centiskorch", "Clobbopus",
        "Grapploct", "Sinistea", "Polteageist",
        // Galar's third batch, Hatenna to Morpeko
        "Hatenna", "Hattrem", "Hatterene", "Impidimp", "Morgrem", "Grimmsnarl", "Obstagoon", "Perrserker", "Cursola", "Sirfetch'd", "Mr. Rime",
        "Runerigus", "Milcery", "Alcremie", "Falinks", "Pincurchin", "Snom", "Frosmoth", "Stonjourner", "Eiscue", "Indeedee", "Morpeko",
        // Galar's last batch, Cufant to Calyrex
        "Cufant", "Copperajah", "Dracozolt", "Arctozolt", "Dracovish", "Arctovish", "Duraludon", "Dreepy", "Drakloak", "Dragapult", "Zacian",
        "Zamazenta", "Eternatus", "Kubfu", "Urshifu", "Zarude", "Regieleki", "Regidrago", "Glastrier", "Spectrier", "Calyrex",
        // The seven first met in Hisui, Wyrdeer to Enamorus
        "Wyrdeer", "Kleavor", "Ursaluna", "Basculegion", "Sneasler", "Overqwil", "Enamorus",
        // Paldea's first batch, Sprigatito to Pawmot
        "Sprigatito", "Floragato", "Meowscarada", "Fuecoco", "Crocalor", "Skeledirge", "Quaxly", "Quaxwell", "Quaquaval", "Lechonk", "Oinkologne",
        "Tarountula", "Spidops", "Nymble", "Lokix", "Pawmi", "Pawmo", "Pawmot",
        // Paldea's second batch, Tandemaus to Klawf
        "Tandemaus", "Maushold", "Fidough", "Dachsbun", "Smoliv", "Dolliv", "Arboliva", "Squawkabilly", "Nacli", "Naclstack", "Garganacl",
        "Charcadet", "Armarouge", "Ceruledge", "Tadbulb", "Bellibolt", "Wattrel", "Kilowattrel", "Maschiff", "Mabosstiff", "Shroodle", "Grafaiai",
        "Bramblin", "Brambleghast", "Toedscool", "Toedscruel", "Klawf"
    };

    /// <summary>
    /// Forms with a hand-built model of their own: every form of a hand-built species, Platinum's own (Rotom's
    /// appliances, Giratina's Origin Forme, the cloaks, the East Sea, Cherrim in the sun, Unown's letters, Castform's
    /// weathers, Deoxys's formes, Shaymin's Sky Forme, Arceus's types) and the later games' (the regional forms, Dialga's and Palkia's Origin Formes, Basculin's
    /// stripes, Darmanitan's Zen Modes, Deerling's and Sawsbuck's seasons, the female Frillish and Jellicent, the Therian Formes, Kyurem's fusions, Keldeo's Resolute Form, Meloetta's Pirouette Forme, Genesect's drives, the Megas, the Primal Kyogre and Groudon, the Gigantamax forms, Pikachu's caps and
    /// costumes, the spiky-eared Pichu). The few that look just like their species (Mothim's cloaks, the partner Pikachu
    /// and Eevee) show its model.
    /// </summary>
    public static readonly string[] Forms =
    {
        "Rotom-Heat", "Rotom-Wash", "Rotom-Frost", "Rotom-Fan", "Rotom-Mow", "Giratina-Origin",
        "Burmy-Sandy", "Burmy-Trash", "Wormadam-Sandy", "Wormadam-Trash", "Shellos-East", "Gastrodon-East", "Cherrim-Sunshine",
        "Unown-B", "Unown-C", "Unown-D", "Unown-E", "Unown-F", "Unown-G", "Unown-H", "Unown-I", "Unown-J", "Unown-K",
        "Unown-L", "Unown-M", "Unown-N", "Unown-O", "Unown-P", "Unown-Q", "Unown-R", "Unown-S", "Unown-T", "Unown-U",
        "Unown-V", "Unown-W", "Unown-X", "Unown-Y", "Unown-Z", "Unown-Exclamation", "Unown-Question",
        "Castform-Sunny", "Castform-Rainy", "Castform-Snowy", "Deoxys-Attack", "Deoxys-Defense", "Deoxys-Speed",
        "Shaymin-Sky", "Arceus-Fighting", "Arceus-Flying", "Arceus-Poison", "Arceus-Ground", "Arceus-Rock", "Arceus-Bug", "Arceus-Ghost",
        "Arceus-Steel", "Arceus-Fire", "Arceus-Water", "Arceus-Grass", "Arceus-Electric", "Arceus-Psychic", "Arceus-Ice", "Arceus-Dragon",
        "Arceus-Dark", "Arceus-Fairy",
        "Rattata-Alola", "Raticate-Alola", "Raichu-Alola", "Sandshrew-Alola", "Sandslash-Alola", "Vulpix-Alola", "Ninetales-Alola",
        "Diglett-Alola", "Dugtrio-Alola", "Meowth-Alola", "Meowth-Galar", "Persian-Alola", "Growlithe-Hisui", "Arcanine-Hisui",
        "Geodude-Alola", "Graveler-Alola", "Golem-Alola", "Ponyta-Galar", "Rapidash-Galar", "Slowpoke-Galar", "Slowbro-Galar",
        "Farfetch'd-Galar", "Grimer-Alola", "Muk-Alola", "Voltorb-Hisui", "Electrode-Hisui", "Exeggutor-Alola", "Marowak-Alola",
        "Weezing-Galar", "Mr. Mime-Galar", "Tauros-Paldea-Combat-Breed", "Tauros-Paldea-Blaze-Breed", "Tauros-Paldea-Aqua-Breed",
        "Articuno-Galar", "Zapdos-Galar", "Moltres-Galar", "Typhlosion-Hisui", "Wooper-Paldea", "Slowking-Galar", "Qwilfish-Hisui",
        "Sneasel-Hisui", "Corsola-Galar", "Zigzagoon-Galar", "Linoone-Galar", "Dialga-Origin", "Palkia-Origin", "Samurott-Hisui",
        "Lilligant-Hisui", "Basculin-Blue-Striped", "Basculin-White-Striped", "Darumaka-Galar", "Darmanitan-Zen",
        "Darmanitan-Galar-Standard", "Darmanitan-Galar-Zen", "Yamask-Galar", "Zorua-Hisui", "Zoroark-Hisui",
        "Deerling-Summer", "Deerling-Autumn", "Deerling-Winter", "Sawsbuck-Summer", "Sawsbuck-Autumn", "Sawsbuck-Winter",
        "Frillish-Female", "Jellicent-Female", "Stunfisk-Galar", "Braviary-Hisui", "Tornadus-Therian", "Thundurus-Therian",
        "Landorus-Therian", "Kyurem-White", "Kyurem-Black", "Keldeo-Resolute", "Meloetta-Pirouette", "Genesect-Douse", "Genesect-Shock",
        "Genesect-Burn", "Genesect-Chill", "Greninja-Ash", "Vivillon-Icy-Snow", "Vivillon-Polar", "Vivillon-Tundra", "Vivillon-Continental", "Vivillon-Garden",
        "Vivillon-Elegant", "Vivillon-Modern", "Vivillon-Marine", "Vivillon-Archipelago", "Vivillon-High-Plains", "Vivillon-Sandstorm", "Vivillon-River",
        "Vivillon-Monsoon", "Vivillon-Savanna", "Vivillon-Sun", "Vivillon-Ocean", "Vivillon-Jungle", "Vivillon-Fancy", "Vivillon-Poke-Ball", "Pyroar-Female",
        "Flabébé-Yellow", "Flabébé-Orange", "Flabébé-Blue", "Flabébé-White", "Floette-Yellow", "Floette-Orange", "Floette-Blue", "Floette-White",
        "Floette-Eternal", "Florges-Yellow", "Florges-Orange", "Florges-Blue", "Florges-White", "Furfrou-Heart", "Furfrou-Star", "Furfrou-Diamond",
        "Furfrou-Debutante", "Furfrou-Matron", "Furfrou-Dandy", "Furfrou-La-Reine", "Furfrou-Kabuki", "Furfrou-Pharaoh", "Meowstic-Female",
        "Aegislash-Blade", "Sliggoo-Hisui", "Goodra-Hisui", "Pumpkaboo-Small", "Pumpkaboo-Large", "Pumpkaboo-Super", "Gourgeist-Small",
        "Gourgeist-Large", "Gourgeist-Super", "Avalugg-Hisui", "Xerneas-Active", "Zygarde-10", "Zygarde-Complete", "Hoopa-Unbound",
        "Decidueye-Hisui", "Oricorio-Pom-Pom", "Oricorio-Pau", "Oricorio-Sensu", "Lycanroc-Midnight", "Lycanroc-Dusk",
        "Wishiwashi-School", "Silvally-Fighting", "Silvally-Flying", "Silvally-Poison", "Silvally-Ground", "Silvally-Rock", "Silvally-Bug", "Silvally-Ghost", "Silvally-Steel", "Silvally-Fire", "Silvally-Water", "Silvally-Grass", "Silvally-Electric", "Silvally-Psychic", "Silvally-Ice", "Silvally-Dragon", "Silvally-Dark", "Silvally-Fairy", "Minior-Red", "Minior-Orange", "Minior-Yellow", "Minior-Green", "Minior-Blue", "Minior-Indigo", "Minior-Violet", "Mimikyu-Busted",
        "Necrozma-Dusk", "Necrozma-Dawn", "Necrozma-Ultra", "Magearna-Original",
        "Cramorant-Gulping", "Cramorant-Gorging", "Toxtricity-Low-Key",
        "Alcremie-Vanilla-Cream-Berry-Sweet", "Alcremie-Vanilla-Cream-Love-Sweet", "Alcremie-Vanilla-Cream-Star-Sweet", "Alcremie-Vanilla-Cream-Clover-Sweet",
        "Alcremie-Vanilla-Cream-Flower-Sweet", "Alcremie-Vanilla-Cream-Ribbon-Sweet", "Alcremie-Ruby-Cream-Strawberry-Sweet", "Alcremie-Ruby-Cream-Berry-Sweet",
        "Alcremie-Ruby-Cream-Love-Sweet", "Alcremie-Ruby-Cream-Star-Sweet", "Alcremie-Ruby-Cream-Clover-Sweet", "Alcremie-Ruby-Cream-Flower-Sweet",
        "Alcremie-Ruby-Cream-Ribbon-Sweet", "Alcremie-Matcha-Cream-Strawberry-Sweet", "Alcremie-Matcha-Cream-Berry-Sweet", "Alcremie-Matcha-Cream-Love-Sweet",
        "Alcremie-Matcha-Cream-Star-Sweet", "Alcremie-Matcha-Cream-Clover-Sweet", "Alcremie-Matcha-Cream-Flower-Sweet", "Alcremie-Matcha-Cream-Ribbon-Sweet",
        "Alcremie-Mint-Cream-Strawberry-Sweet", "Alcremie-Mint-Cream-Berry-Sweet", "Alcremie-Mint-Cream-Love-Sweet", "Alcremie-Mint-Cream-Star-Sweet",
        "Alcremie-Mint-Cream-Clover-Sweet", "Alcremie-Mint-Cream-Flower-Sweet", "Alcremie-Mint-Cream-Ribbon-Sweet", "Alcremie-Lemon-Cream-Strawberry-Sweet",
        "Alcremie-Lemon-Cream-Berry-Sweet", "Alcremie-Lemon-Cream-Love-Sweet", "Alcremie-Lemon-Cream-Star-Sweet", "Alcremie-Lemon-Cream-Clover-Sweet",
        "Alcremie-Lemon-Cream-Flower-Sweet", "Alcremie-Lemon-Cream-Ribbon-Sweet", "Alcremie-Salted-Cream-Strawberry-Sweet", "Alcremie-Salted-Cream-Berry-Sweet",
        "Alcremie-Salted-Cream-Love-Sweet", "Alcremie-Salted-Cream-Star-Sweet", "Alcremie-Salted-Cream-Clover-Sweet", "Alcremie-Salted-Cream-Flower-Sweet",
        "Alcremie-Salted-Cream-Ribbon-Sweet", "Alcremie-Ruby-Swirl-Strawberry-Sweet", "Alcremie-Ruby-Swirl-Berry-Sweet", "Alcremie-Ruby-Swirl-Love-Sweet",
        "Alcremie-Ruby-Swirl-Star-Sweet", "Alcremie-Ruby-Swirl-Clover-Sweet", "Alcremie-Ruby-Swirl-Flower-Sweet", "Alcremie-Ruby-Swirl-Ribbon-Sweet",
        "Alcremie-Caramel-Swirl-Strawberry-Sweet", "Alcremie-Caramel-Swirl-Berry-Sweet", "Alcremie-Caramel-Swirl-Love-Sweet", "Alcremie-Caramel-Swirl-Star-Sweet",
        "Alcremie-Caramel-Swirl-Clover-Sweet", "Alcremie-Caramel-Swirl-Flower-Sweet", "Alcremie-Caramel-Swirl-Ribbon-Sweet", "Alcremie-Rainbow-Swirl-Strawberry-Sweet",
        "Alcremie-Rainbow-Swirl-Berry-Sweet", "Alcremie-Rainbow-Swirl-Love-Sweet", "Alcremie-Rainbow-Swirl-Star-Sweet", "Alcremie-Rainbow-Swirl-Clover-Sweet",
        "Alcremie-Rainbow-Swirl-Flower-Sweet", "Alcremie-Rainbow-Swirl-Ribbon-Sweet",
        "Eiscue-Noice", "Indeedee-Female", "Morpeko-Hangry",
        "Zacian-Crowned", "Zamazenta-Crowned", "Eternatus-Eternamax", "Urshifu-Rapid-Strike", "Zarude-Dada", "Calyrex-Ice", "Calyrex-Shadow",
        "Ursaluna-Bloodmoon", "Basculegion-Female", "Enamorus-Therian", "Oinkologne-Female",
        "Maushold-Family-Of-Three", "Squawkabilly-Blue-Plumage", "Squawkabilly-Yellow-Plumage", "Squawkabilly-White-Plumage",
        "Venusaur-Mega", "Charizard-Mega-X", "Charizard-Mega-Y", "Blastoise-Mega", "Beedrill-Mega", "Pidgeot-Mega",
        "Raichu-Mega-X", "Raichu-Mega-Y", "Clefable-Mega", "Alakazam-Mega", "Victreebel-Mega", "Slowbro-Mega", "Gengar-Mega",
        "Kangaskhan-Mega", "Starmie-Mega", "Pinsir-Mega", "Gyarados-Mega", "Aerodactyl-Mega", "Dragonite-Mega", "Mewtwo-Mega-X",
        "Mewtwo-Mega-Y", "Meganium-Mega", "Feraligatr-Mega", "Ampharos-Mega", "Steelix-Mega", "Scizor-Mega", "Heracross-Mega", "Skarmory-Mega", "Houndoom-Mega", "Tyranitar-Mega", "Sceptile-Mega", "Blaziken-Mega", "Swampert-Mega", "Gardevoir-Mega", "Sableye-Mega", "Mawile-Mega", "Aggron-Mega",
        "Medicham-Mega", "Manectric-Mega", "Sharpedo-Mega", "Camerupt-Mega", "Altaria-Mega", "Banette-Mega", "Chimecho-Mega", "Absol-Mega", "Absol-Mega-Z", "Glalie-Mega", "Salamence-Mega", "Metagross-Mega", "Latias-Mega", "Latios-Mega", "Kyogre-Primal", "Groudon-Primal",
        "Rayquaza-Mega", "Staraptor-Mega", "Lopunny-Mega",
        "Garchomp-Mega", "Garchomp-Mega-Z", "Lucario-Mega", "Lucario-Mega-Z", "Abomasnow-Mega", "Gallade-Mega", "Froslass-Mega", "Heatran-Mega", "Darkrai-Mega", "Emboar-Mega", "Excadrill-Mega", "Audino-Mega", "Scolipede-Mega", "Scrafty-Mega", "Eelektross-Mega", "Chandelure-Mega", "Golurk-Mega",
        "Chesnaught-Mega", "Delphox-Mega", "Greninja-Mega", "Pyroar-Mega", "Floette-Mega", "Meowstic-Male-Mega", "Meowstic-Female-Mega",
        "Malamar-Mega", "Barbaracle-Mega", "Dragalge-Mega", "Hawlucha-Mega", "Zygarde-Mega", "Diancie-Mega", "Crabominable-Mega", "Golisopod-Mega", "Drampa-Mega", "Magearna-Mega", "Magearna-Original-Mega",
        "Zeraora-Mega", "Falinks-Mega",
        "Venusaur-Gmax", "Charizard-Gmax", "Blastoise-Gmax", "Butterfree-Gmax",
        "Pikachu-Gmax", "Meowth-Gmax", "Machamp-Gmax", "Gengar-Gmax", "Kingler-Gmax", "Lapras-Gmax", "Eevee-Gmax", "Snorlax-Gmax", "Garbodor-Gmax",
        "Melmetal-Gmax", "Rillaboom-Gmax", "Cinderace-Gmax", "Inteleon-Gmax", "Corviknight-Gmax", "Orbeetle-Gmax", "Drednaw-Gmax", "Coalossal-Gmax",
        "Flapple-Gmax", "Sandaconda-Gmax", "Toxtricity-Amped-Gmax", "Centiskorch-Gmax",
        "Hatterene-Gmax", "Grimmsnarl-Gmax", "Alcremie-Gmax", "Copperajah-Gmax", "Duraludon-Gmax", "Urshifu-Single-Strike-Gmax", "Urshifu-Rapid-Strike-Gmax",
        "Pikachu-Original-Cap", "Pikachu-Hoenn-Cap", "Pikachu-Sinnoh-Cap", "Pikachu-Unova-Cap", "Pikachu-Kalos-Cap", "Pikachu-Alola-Cap",
        "Pikachu-Partner-Cap", "Pikachu-World-Cap", "Pikachu-Rock-Star", "Pikachu-Belle", "Pikachu-Pop-Star", "Pikachu-Phd", "Pikachu-Libre",
        "Pikachu-Cosplay", "Pichu-Spiky-Eared"
    };

    /// <summary>
    /// Forms that look just like another form rather than like their species itself, and show that form's sculpt:
    /// Zygarde's 10% Forme with Power Construct is the 10% Forme to look at (with Power Construct, the 50% Forme is the
    /// species, so it needs no entry); the Gigantamax Appletun is the same great apple as the Gigantamax Flapple, and
    /// the Low Key Toxtricity's Gigantamax form the same as the Amped one's.
    /// </summary>
    internal static readonly Dictionary<string, string> LooksLike = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Zygarde-10-Power-Construct"] = "Zygarde-10",
        ["Appletun-Gmax"] = "Flapple-Gmax",
        ["Toxtricity-Low-Key-Gmax"] = "Toxtricity-Amped-Gmax"
    };

    /// <summary>Pikachu's caps and costumes among <see cref="Forms"/>, in capitals as the sculptor is asked for them.</summary>
    private static readonly HashSet<string> PikachuForms = Forms.Where(f => f.StartsWith("Pikachu-") && f != "Pikachu-Gmax").Select(f => f.ToUpperInvariant()).ToHashSet();

    /// <summary>Whether a species or one of its forms has a hand-built model (anything else is generated).</summary>
    public static bool HasModel(string species) =>
        Array.Exists(Species, s => s.Equals(species, StringComparison.OrdinalIgnoreCase)) ||
        Array.Exists(Forms, s => s.Equals(species, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The species start-up meshes and keeps, with their menu sprites (the hand-built models of plan 04 · G7 and
    /// G10): the title's Giratina, the introduction's Buneary, the starters and the Pokémon of the first routes.
    /// Every other model, the later hand-built batches included, is made when a scene asks for it, as the generated
    /// ones are; keeping them all would cost a few megabytes each for the whole game.
    /// </summary>
    public static readonly string[] Preloaded =
    {
        "Giratina", "Buneary", "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
        "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Shinx", "Luxio", "Luxray", "Riolu", "Lucario", "Gible", "Gabite", "Garchomp"
    };

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
        "BUNEARY" => Buneary(),
        // Plan 03 · D6: the Sinnoh Pokédex's first batch (PokemonModels.Sinnoh1.cs)
        "KRICKETOT" => Kricketot(),
        "KRICKETUNE" => Kricketune(),
        "ABRA" => Abra(),
        "KADABRA" => Kadabra(),
        "ALAKAZAM" => Alakazam(),
        "MAGIKARP" => Magikarp(),
        "GYARADOS" => Gyarados(),
        "BUDEW" => Budew(),
        "ROSELIA" => Roselia(),
        "ROSERADE" => Roserade(),
        "ZUBAT" => Zubat(),
        "GOLBAT" => Golbat(),
        "CROBAT" => Crobat(),
        "GEODUDE" => Geodude(),
        "GRAVELER" => Graveler(),
        "GOLEM" => Golem(),
        "ONIX" => Onix(),
        "STEELIX" => Steelix(),
        "CRANIDOS" => HeadButter(false),
        "RAMPARDOS" => HeadButter(true),
        "SHIELDON" => Shieldon(),
        "BASTIODON" => Bastiodon(),
        "MACHOP" => Machop(),
        "MACHOKE" => Machoke(),
        "MACHAMP" => Machamp(),
        "PSYDUCK" => Psyduck(),
        "GOLDUCK" => Golduck(),
        "BURMY" => Burmy(),
        "WORMADAM" => Wormadam(),
        "MOTHIM" => Mothim(),
        "WURMPLE" => Wurmple(),
        "SILCOON" => Cocoon(false),
        "BEAUTIFLY" => Beautifly(),
        "CASCOON" => Cocoon(true),
        "DUSTOX" => Dustox(),
        "COMBEE" => Combee(),
        "VESPIQUEN" => Vespiquen(),
        "PACHIRISU" => Pachirisu(),
        "BUIZEL" => Buizel(),
        "FLOATZEL" => Floatzel(),
        "CHERUBI" => Cherubi(),
        "CHERRIM" => Cherrim(),
        "SHELLOS" => SeaSlug(false),
        "GASTRODON" => SeaSlug(true),
        "HERACROSS" => Heracross(),
        "AIPOM" => Aipom(),
        "AMBIPOM" => Ambipom(),
        "DRIFLOON" => Drifloon(),
        "DRIFBLIM" => Drifblim(),
        "LOPUNNY" => Lopunny(),
        // Plan 03 · D7: the second batch (PokemonModels.Sinnoh2.cs)
        "GASTLY" => Gastly(),
        "HAUNTER" => Haunter(),
        "GENGAR" => Gengar(),
        "MISDREAVUS" => Misdreavus(),
        "MISMAGIUS" => Mismagius(),
        "MURKROW" => Murkrow(),
        "HONCHKROW" => Honchkrow(),
        "GLAMEOW" => Glameow(),
        "PURUGLY" => Purugly(),
        "GOLDEEN" => Goldeen(),
        "SEAKING" => Seaking(),
        "BARBOACH" => Barboach(),
        "WHISCASH" => Whiscash(),
        "CHINGLING" => Chingling(),
        "CHIMECHO" => Chimecho(),
        "STUNKY" => Skunk(false),
        "SKUNTANK" => Skunk(true),
        "MEDITITE" => Meditite(),
        "MEDICHAM" => Medicham(),
        "BRONZOR" => Bronzor(),
        "BRONZONG" => Bronzong(),
        "PONYTA" => Horse(false),
        "RAPIDASH" => Horse(true),
        "BONSLY" => Bonsly(),
        "SUDOWOODO" => Sudowoodo(),
        "MIME JR." => MimeJr(),
        "MR. MIME" => MrMime(),
        "HAPPINY" => Happiny(),
        "CHANSEY" => Chansey(),
        "BLISSEY" => Blissey(),
        "CLEFFA" => Cleffa(),
        "CLEFAIRY" => Fairy(false),
        "CLEFABLE" => Fairy(true),
        "CHATOT" => Chatot(),
        "PICHU" => Pichu(),
        "PIKACHU" => Pikachu(),
        "RAICHU" => Raichu(),
        "HOOTHOOT" => Hoothoot(),
        "NOCTOWL" => Noctowl(),
        "SPIRITOMB" => Spiritomb(),
        "MUNCHLAX" => Munchlax(),
        "SNORLAX" => Snorlax(),
        "UNOWN" => Unown(),
        "WOOPER" => Wooper(),
        "QUAGSIRE" => Quagsire(),
        "WINGULL" => Wingull(),
        "PELIPPER" => Pelipper(),
        "GIRAFARIG" => Girafarig(),
        "HIPPOPOTAS" => Hippopotas(),
        "HIPPOWDON" => Hippowdon(),
        // Plan 03 · D8: the third batch (PokemonModels.Sinnoh3.cs)
        "AZURILL" => Azurill(),
        "MARILL" => Marill(),
        "AZUMARILL" => Azumarill(),
        "SKORUPI" => Skorupi(),
        "DRAPION" => Drapion(),
        "CROAGUNK" => Croagunk(),
        "TOXICROAK" => Toxicroak(),
        "CARNIVINE" => Carnivine(),
        "REMORAID" => Remoraid(),
        "OCTILLERY" => Octillery(),
        "FINNEON" => Finneon(),
        "LUMINEON" => Lumineon(),
        "TENTACOOL" => Tentacool(),
        "TENTACRUEL" => Tentacruel(),
        "FEEBAS" => Feebas(),
        "MILOTIC" => Milotic(),
        "MANTYKE" => Mantyke(),
        "MANTINE" => Mantine(),
        "SNOVER" => Snover(),
        "ABOMASNOW" => Abomasnow(),
        "SNEASEL" => Sneasel(),
        "WEAVILE" => Weavile(),
        "ROTOM" => Rotom(),
        "GLIGAR" => Gligar(),
        "GLISCOR" => Gliscor(),
        "NOSEPASS" => Nosepass(),
        "PROBOPASS" => Probopass(),
        "RALTS" => Ralts(),
        "KIRLIA" => Kirlia(),
        "GARDEVOIR" => Gardevoir(),
        "GALLADE" => Gallade(),
        "LICKITUNG" => Lickitung(),
        "LICKILICKY" => Lickilicky(),
        "EEVEE" => Eevee(),
        "VAPOREON" => Vaporeon(),
        "JOLTEON" => Jolteon(),
        "FLAREON" => Flareon(),
        "ESPEON" => Espeon(),
        "UMBREON" => Umbreon(),
        "LEAFEON" => Leafeon(),
        "GLACEON" => Glaceon(),
        "SWABLU" => Swablu(),
        "ALTARIA" => Altaria(),
        "TOGEPI" => Togepi(),
        "TOGETIC" => Togetic(),
        "TOGEKISS" => Togekiss(),
        "HOUNDOUR" => Houndour(),
        "HOUNDOOM" => Houndoom(),
        "MAGNEMITE" => Magnemite(),
        "MAGNETON" => Magneton(),
        "MAGNEZONE" => Magnezone(),
        // Plan 03 · D9: the last batch (PokemonModels.Sinnoh4.cs)
        "TANGELA" => Tangela(),
        "TANGROWTH" => Tangrowth(),
        "YANMA" => Yanma(),
        "YANMEGA" => Yanmega(),
        "TROPIUS" => Tropius(),
        "RHYHORN" => Rhyhorn(),
        "RHYDON" => Rhydon(),
        "RHYPERIOR" => Rhyperior(),
        "DUSKULL" => Duskull(),
        "DUSCLOPS" => Dusclops(),
        "DUSKNOIR" => Dusknoir(),
        "PORYGON" => Porygon(),
        "PORYGON2" => Porygon2(),
        "PORYGON-Z" => PorygonZ(),
        "SCYTHER" => Scyther(),
        "SCIZOR" => Scizor(),
        "ELEKID" => Elekid(),
        "ELECTABUZZ" => Electabuzz(),
        "ELECTIVIRE" => Electivire(),
        "MAGBY" => Magby(),
        "MAGMAR" => Magmar(),
        "MAGMORTAR" => Magmortar(),
        "SWINUB" => Swinub(),
        "PILOSWINE" => Piloswine(),
        "MAMOSWINE" => Mamoswine(),
        "SNORUNT" => Snorunt(),
        "GLALIE" => Glalie(),
        "FROSLASS" => Froslass(),
        "ABSOL" => Absol(),
        "UXIE" => Uxie(),
        "MESPRIT" => Mesprit(),
        "AZELF" => Azelf(),
        "DIALGA" => Dialga(),
        "PALKIA" => Palkia(),
        "MANAPHY" => Manaphy(),
        // Popular species from outside the Sinnoh Pokédex: Kanto's first (PokemonModels.Kanto1.cs)
        "BULBASAUR" => Bulbasaur(),
        "IVYSAUR" => Ivysaur(),
        "VENUSAUR" => Venusaur(),
        "CHARMANDER" => Charmander(),
        "CHARMELEON" => Charmeleon(),
        "CHARIZARD" => Charizard(),
        "SQUIRTLE" => Squirtle(),
        "WARTORTLE" => Wartortle(),
        "BLASTOISE" => Blastoise(),
        "CATERPIE" => Caterpie(),
        "METAPOD" => Metapod(),
        "BUTTERFREE" => Butterfree(),
        "WEEDLE" => Weedle(),
        "KAKUNA" => Kakuna(),
        "BEEDRILL" => Beedrill(),
        "PIDGEY" => Pidgey(),
        "PIDGEOTTO" => Pidgeotto(),
        "PIDGEOT" => Pidgeot(),
        "RATTATA" => Rattata(),
        "RATICATE" => Raticate(),
        "SPEAROW" => Spearow(),
        "FEAROW" => Fearow(),
        "EKANS" => Ekans(),
        "ARBOK" => Arbok(),
        "SANDSHREW" => Sandshrew(),
        "SANDSLASH" => Sandslash(),
        "NIDORAN♀" => NidoranF(),
        "NIDORINA" => Nidorina(),
        "NIDOQUEEN" => Nidoqueen(),
        "NIDORAN♂" => NidoranM(),
        "NIDORINO" => Nidorino(),
        "NIDOKING" => Nidoking(),
        "VULPIX" => Vulpix(),
        "NINETALES" => Ninetales(),
        "JIGGLYPUFF" => Jigglypuff(),
        "WIGGLYTUFF" => Wigglytuff(),
        "ODDISH" => Oddish(),
        "GLOOM" => Gloom(),
        "VILEPLUME" => Vileplume(),
        // Kanto's second batch (PokemonModels.Kanto2.cs)
        "PARAS" => Paras(),
        "PARASECT" => Parasect(),
        "VENONAT" => Venonat(),
        "VENOMOTH" => Venomoth(),
        "DIGLETT" => Diglett(),
        "DUGTRIO" => Dugtrio(),
        "MEOWTH" => Meowth(),
        "PERSIAN" => Persian(),
        "MANKEY" => Mankey(),
        "PRIMEAPE" => Primeape(),
        "GROWLITHE" => Growlithe(),
        "ARCANINE" => Arcanine(),
        "POLIWAG" => Poliwag(),
        "POLIWHIRL" => Poliwhirl(),
        "POLIWRATH" => Poliwrath(),
        "BELLSPROUT" => Bellsprout(),
        "WEEPINBELL" => Weepinbell(),
        "VICTREEBEL" => Victreebel(),
        "SLOWPOKE" => Slowpoke(),
        "SLOWBRO" => Slowbro(),
        "FARFETCH'D" => Farfetchd(),
        "DODUO" => Doduo(),
        "DODRIO" => Dodrio(),
        "SEEL" => Seel(),
        "DEWGONG" => Dewgong(),
        "GRIMER" => Grimer(),
        "MUK" => Muk(),
        "SHELLDER" => Shellder(),
        "CLOYSTER" => Cloyster(),
        "DROWZEE" => Drowzee(),
        "HYPNO" => Hypno(),
        "KRABBY" => Krabby(),
        "KINGLER" => Kingler(),
        // Kanto's third batch (PokemonModels.Kanto3.cs)
        "VOLTORB" => Voltorb(),
        "ELECTRODE" => Electrode(),
        "EXEGGCUTE" => Exeggcute(),
        "EXEGGUTOR" => Exeggutor(),
        "CUBONE" => Cubone(),
        "MAROWAK" => Marowak(),
        "HITMONLEE" => Hitmonlee(),
        "HITMONCHAN" => Hitmonchan(),
        "KOFFING" => Koffing(),
        "WEEZING" => Weezing(),
        "KANGASKHAN" => Kangaskhan(),
        "HORSEA" => Horsea(),
        "SEADRA" => Seadra(),
        "STARYU" => Staryu(),
        "STARMIE" => Starmie(),
        "JYNX" => Jynx(),
        "PINSIR" => Pinsir(),
        "TAUROS" => Tauros(),
        "LAPRAS" => Lapras(),
        "DITTO" => Ditto(),
        "OMANYTE" => Omanyte(),
        "OMASTAR" => Omastar(),
        "KABUTO" => Kabuto(),
        "KABUTOPS" => Kabutops(),
        "AERODACTYL" => Aerodactyl(),
        "ARTICUNO" => Articuno(),
        "ZAPDOS" => Zapdos(),
        "MOLTRES" => Moltres(),
        "DRATINI" => Dratini(),
        "DRAGONAIR" => Dragonair(),
        "DRAGONITE" => Dragonite(),
        "MEWTWO" => Mewtwo(),
        "MEW" => Mew(),
        // Johto's first batch (PokemonModels.Johto1.cs)
        "CHIKORITA" => Chikorita(),
        "BAYLEEF" => Bayleef(),
        "MEGANIUM" => Meganium(),
        "CYNDAQUIL" => Cyndaquil(),
        "QUILAVA" => Quilava(),
        "TYPHLOSION" => Typhlosion(),
        "TOTODILE" => Totodile(),
        "CROCONAW" => Croconaw(),
        "FERALIGATR" => Feraligatr(),
        "SENTRET" => Sentret(),
        "FURRET" => Furret(),
        "LEDYBA" => Ledyba(),
        "LEDIAN" => Ledian(),
        "SPINARAK" => Spinarak(),
        "ARIADOS" => Ariados(),
        "CHINCHOU" => Chinchou(),
        "LANTURN" => Lanturn(),
        "IGGLYBUFF" => Igglybuff(),
        "NATU" => Natu(),
        "XATU" => Xatu(),
        "MAREEP" => Mareep(),
        "FLAAFFY" => Flaaffy(),
        "AMPHAROS" => Ampharos(),
        "BELLOSSOM" => Bellossom(),
        "POLITOED" => Politoed(),
        "HOPPIP" => Hoppip(),
        "SKIPLOOM" => Skiploom(),
        "JUMPLUFF" => Jumpluff(),
        "SUNKERN" => Sunkern(),
        "SUNFLORA" => Sunflora(),
        "SLOWKING" => Slowking(),
        "WOBBUFFET" => Wobbuffet(),
        "PINECO" => Pineco(),
        "FORRETRESS" => Forretress(),
        "DUNSPARCE" => Dunsparce(),
        "SNUBBULL" => Snubbull(),
        "GRANBULL" => Granbull(),
        "QWILFISH" => Qwilfish(),
        "SHUCKLE" => Shuckle(),
        "TEDDIURSA" => Teddiursa(),
        "URSARING" => Ursaring(),
        "SLUGMA" => Slugma(),
        "MAGCARGO" => Magcargo(),
        "CORSOLA" => Corsola(),
        // Johto's second batch (PokemonModels.Johto2.cs)
        "DELIBIRD" => Delibird(),
        "SKARMORY" => Skarmory(),
        "KINGDRA" => Kingdra(),
        "PHANPY" => Phanpy(),
        "DONPHAN" => Donphan(),
        "STANTLER" => Stantler(),
        "SMEARGLE" => Smeargle(),
        "TYROGUE" => Tyrogue(),
        "HITMONTOP" => Hitmontop(),
        "SMOOCHUM" => Smoochum(),
        "MILTANK" => Miltank(),
        "RAIKOU" => Raikou(),
        "ENTEI" => Entei(),
        "SUICUNE" => Suicune(),
        "LARVITAR" => Larvitar(),
        "PUPITAR" => Pupitar(),
        "TYRANITAR" => Tyranitar(),
        "LUGIA" => Lugia(),
        "HO-OH" => HoOh(),
        "CELEBI" => Celebi(),
        // Hoenn's first batch (PokemonModels.Hoenn1.cs)
        "TREECKO" => Treecko(),
        "GROVYLE" => Grovyle(),
        "SCEPTILE" => Sceptile(),
        "TORCHIC" => Torchic(),
        "COMBUSKEN" => Combusken(),
        "BLAZIKEN" => Blaziken(),
        "MUDKIP" => Mudkip(),
        "MARSHTOMP" => Marshtomp(),
        "SWAMPERT" => Swampert(),
        "POOCHYENA" => Poochyena(),
        "MIGHTYENA" => Mightyena(),
        "ZIGZAGOON" => Zigzagoon(),
        "LINOONE" => Linoone(),
        "LOTAD" => Lotad(),
        "LOMBRE" => Lombre(),
        "LUDICOLO" => Ludicolo(),
        "SEEDOT" => Seedot(),
        "NUZLEAF" => Nuzleaf(),
        "SHIFTRY" => Shiftry(),
        "TAILLOW" => Taillow(),
        "SWELLOW" => Swellow(),
        "SURSKIT" => Surskit(),
        "MASQUERAIN" => Masquerain(),
        "SHROOMISH" => Shroomish(),
        "BRELOOM" => Breloom(),
        "SLAKOTH" => Slakoth(),
        "VIGOROTH" => Vigoroth(),
        "SLAKING" => Slaking(),
        "NINCADA" => Nincada(),
        "NINJASK" => Ninjask(),
        "SHEDINJA" => Shedinja(),
        "WHISMUR" => Whismur(),
        "LOUDRED" => Loudred(),
        "EXPLOUD" => Exploud(),
        "MAKUHITA" => Makuhita(),
        "HARIYAMA" => Hariyama(),
        "SKITTY" => Skitty(),
        "DELCATTY" => Delcatty(),
        // Hoenn's second batch (PokemonModels.Hoenn2.cs)
        "SABLEYE" => Sableye(),
        "MAWILE" => Mawile(),
        "ARON" => Aron(),
        "LAIRON" => Lairon(),
        "AGGRON" => Aggron(),
        "ELECTRIKE" => Electrike(),
        "MANECTRIC" => Manectric(),
        "PLUSLE" => Plusle(),
        "MINUN" => Minun(),
        "VOLBEAT" => Volbeat(),
        "ILLUMISE" => Illumise(),
        "GULPIN" => Gulpin(),
        "SWALOT" => Swalot(),
        "CARVANHA" => Carvanha(),
        "SHARPEDO" => Sharpedo(),
        "WAILMER" => Wailmer(),
        "WAILORD" => Wailord(),
        "NUMEL" => Numel(),
        "CAMERUPT" => Camerupt(),
        "TORKOAL" => Torkoal(),
        "SPOINK" => Spoink(),
        "GRUMPIG" => Grumpig(),
        "SPINDA" => Spinda(),
        "TRAPINCH" => Trapinch(),
        "VIBRAVA" => Vibrava(),
        "FLYGON" => Flygon(),
        "CACNEA" => Cacnea(),
        "CACTURNE" => Cacturne(),
        "ZANGOOSE" => Zangoose(),
        "SEVIPER" => Seviper(),
        "LUNATONE" => Lunatone(),
        "SOLROCK" => Solrock(),
        "CORPHISH" => Corphish(),
        "CRAWDAUNT" => Crawdaunt(),
        "BALTOY" => Baltoy(),
        "CLAYDOL" => Claydol(),
        "LILEEP" => Lileep(),
        "CRADILY" => Cradily(),
        "ANORITH" => Anorith(),
        "ARMALDO" => Armaldo(),
        // Hoenn's third batch (PokemonModels.Hoenn3.cs)
        "CASTFORM" => Castform(),
        "KECLEON" => Kecleon(),
        "SHUPPET" => Shuppet(),
        "BANETTE" => Banette(),
        "WYNAUT" => Wynaut(),
        "SPHEAL" => Spheal(),
        "SEALEO" => Sealeo(),
        "WALREIN" => Walrein(),
        "CLAMPERL" => Clamperl(),
        "HUNTAIL" => Huntail(),
        "GOREBYSS" => Gorebyss(),
        "RELICANTH" => Relicanth(),
        "LUVDISC" => Luvdisc(),
        "BAGON" => Bagon(),
        "SHELGON" => Shelgon(),
        "SALAMENCE" => Salamence(),
        "BELDUM" => Beldum(),
        "METANG" => Metang(),
        "METAGROSS" => Metagross(),
        "REGIROCK" => Regirock(),
        "REGICE" => Regice(),
        "REGISTEEL" => Registeel(),
        "LATIAS" => Latias(),
        "LATIOS" => Latios(),
        "KYOGRE" => Kyogre(),
        "GROUDON" => Groudon(),
        "RAYQUAZA" => Rayquaza(),
        "JIRACHI" => Jirachi(),
        "DEOXYS" => Deoxys(),
        // Unova's first batch (PokemonModels.Unova1.cs)
        "VICTINI" => Victini(),
        "SNIVY" => Snivy(),
        "SERVINE" => Servine(),
        "SERPERIOR" => Serperior(),
        "TEPIG" => Tepig(),
        "PIGNITE" => Pignite(),
        "EMBOAR" => Emboar(),
        "OSHAWOTT" => Oshawott(),
        "DEWOTT" => Dewott(),
        "SAMUROTT" => Samurott(),
        "PATRAT" => Patrat(),
        "WATCHOG" => Watchog(),
        "LILLIPUP" => Lillipup(),
        "HERDIER" => Herdier(),
        "STOUTLAND" => Stoutland(),
        "PURRLOIN" => Purrloin(),
        "LIEPARD" => Liepard(),
        "PANSAGE" => Pansage(),
        "SIMISAGE" => Simisage(),
        "PANSEAR" => Pansear(),
        "SIMISEAR" => Simisear(),
        "PANPOUR" => Panpour(),
        "SIMIPOUR" => Simipour(),
        "MUNNA" => Munna(),
        "MUSHARNA" => Musharna(),
        "PIDOVE" => Pidove(),
        "TRANQUILL" => Tranquill(),
        "UNFEZANT" => Unfezant(),
        "BLITZLE" => Blitzle(),
        "ZEBSTRIKA" => Zebstrika(),
        "ROGGENROLA" => Roggenrola(),
        "BOLDORE" => Boldore(),
        "GIGALITH" => Gigalith(),
        "WOOBAT" => Woobat(),
        "SWOOBAT" => Swoobat(),
        "DRILBUR" => Drilbur(),
        "EXCADRILL" => Excadrill(),
        "AUDINO" => Audino(),
        // Unova's second batch (PokemonModels.Unova2.cs)
        "TIMBURR" => Timburr(),
        "GURDURR" => Gurdurr(),
        "CONKELDURR" => Conkeldurr(),
        "TYMPOLE" => Tympole(),
        "PALPITOAD" => Palpitoad(),
        "SEISMITOAD" => Seismitoad(),
        "THROH" => Throh(),
        "SAWK" => Sawk(),
        "SEWADDLE" => Sewaddle(),
        "SWADLOON" => Swadloon(),
        "LEAVANNY" => Leavanny(),
        "VENIPEDE" => Venipede(),
        "WHIRLIPEDE" => Whirlipede(),
        "SCOLIPEDE" => Scolipede(),
        "COTTONEE" => Cottonee(),
        "WHIMSICOTT" => Whimsicott(),
        "PETILIL" => Petilil(),
        "LILLIGANT" => Lilligant(),
        "BASCULIN" => Basculin(),
        "SANDILE" => Sandile(),
        "KROKOROK" => Krokorok(),
        "KROOKODILE" => Krookodile(),
        "DARUMAKA" => Darumaka(),
        "DARMANITAN" => Darmanitan(),
        "MARACTUS" => Maractus(),
        "DWEBBLE" => Dwebble(),
        "CRUSTLE" => Crustle(),
        "SCRAGGY" => Scraggy(),
        "SCRAFTY" => Scrafty(),
        "SIGILYPH" => Sigilyph(),
        "YAMASK" => Yamask(),
        "COFAGRIGUS" => Cofagrigus(),
        "TIRTOUGA" => Tirtouga(),
        "CARRACOSTA" => Carracosta(),
        "ARCHEN" => Archen(),
        "ARCHEOPS" => Archeops(),
        "TRUBBISH" => Trubbish(),
        "GARBODOR" => Garbodor(),
        "ZORUA" => Zorua(),
        "ZOROARK" => Zoroark(),
        // Unova's third batch (PokemonModels.Unova3.cs)
        "MINCCINO" => Minccino(),
        "CINCCINO" => Cinccino(),
        "GOTHITA" => Gothita(),
        "GOTHORITA" => Gothorita(),
        "GOTHITELLE" => Gothitelle(),
        "SOLOSIS" => Solosis(),
        "DUOSION" => Duosion(),
        "REUNICLUS" => Reuniclus(),
        "DUCKLETT" => Ducklett(),
        "SWANNA" => Swanna(),
        "VANILLITE" => Vanillite(),
        "VANILLISH" => Vanillish(),
        "VANILLUXE" => Vanilluxe(),
        "DEERLING" => Deerling(),
        "SAWSBUCK" => Sawsbuck(),
        "EMOLGA" => Emolga(),
        "KARRABLAST" => Karrablast(),
        "ESCAVALIER" => Escavalier(),
        "FOONGUS" => Foongus(),
        "AMOONGUSS" => Amoonguss(),
        "FRILLISH" => Frillish(),
        "JELLICENT" => Jellicent(),
        "ALOMOMOLA" => Alomomola(),
        "JOLTIK" => Joltik(),
        "GALVANTULA" => Galvantula(),
        "FERROSEED" => Ferroseed(),
        "FERROTHORN" => Ferrothorn(),
        "KLINK" => Klink(),
        "KLANG" => Klang(),
        "KLINKLANG" => Klinklang(),
        "TYNAMO" => Tynamo(),
        "EELEKTRIK" => Eelektrik(),
        "EELEKTROSS" => Eelektross(),
        "ELGYEM" => Elgyem(),
        "BEHEEYEM" => Beheeyem(),
        "LITWICK" => Litwick(),
        "LAMPENT" => Lampent(),
        "CHANDELURE" => Chandelure(),
        // Unova's fourth batch (PokemonModels.Unova4.cs)
        "AXEW" => Axew(),
        "FRAXURE" => Fraxure(),
        "HAXORUS" => Haxorus(),
        "CUBCHOO" => Cubchoo(),
        "BEARTIC" => Beartic(),
        "CRYOGONAL" => Cryogonal(),
        "SHELMET" => Shelmet(),
        "ACCELGOR" => Accelgor(),
        "STUNFISK" => Stunfisk(),
        "MIENFOO" => Mienfoo(),
        "MIENSHAO" => Mienshao(),
        "DRUDDIGON" => Druddigon(),
        "GOLETT" => Golett(),
        "GOLURK" => Golurk(),
        "PAWNIARD" => Pawniard(),
        "BISHARP" => Bisharp(),
        "BOUFFALANT" => Bouffalant(),
        "RUFFLET" => Rufflet(),
        "BRAVIARY" => Braviary(),
        "VULLABY" => Vullaby(),
        "MANDIBUZZ" => Mandibuzz(),
        "HEATMOR" => Heatmor(),
        "DURANT" => Durant(),
        "DEINO" => Deino(),
        "ZWEILOUS" => Zweilous(),
        "HYDREIGON" => Hydreigon(),
        "LARVESTA" => Larvesta(),
        "VOLCARONA" => Volcarona(),
        "COBALION" => Cobalion(),
        "TERRAKION" => Terrakion(),
        "VIRIZION" => Virizion(),
        "TORNADUS" => Tornadus(),
        "THUNDURUS" => Thundurus(),
        "RESHIRAM" => Reshiram(),
        "ZEKROM" => Zekrom(),
        "LANDORUS" => Landorus(),
        "KYUREM" => Kyurem(),
        "KELDEO" => Keldeo(),
        "MELOETTA" => Meloetta(),
        "GENESECT" => Genesect(),
        // Kalos's first batch (PokemonModels.Kalos1.cs)
        "CHESPIN" => Chespin(),
        "QUILLADIN" => Quilladin(),
        "CHESNAUGHT" => Chesnaught(),
        "FENNEKIN" => Fennekin(),
        "BRAIXEN" => Braixen(),
        "DELPHOX" => Delphox(),
        "FROAKIE" => Froakie(),
        "FROGADIER" => Frogadier(),
        "GRENINJA" => Greninja(),
        "BUNNELBY" => Bunnelby(),
        "DIGGERSBY" => Diggersby(),
        "FLETCHLING" => Fletchling(),
        "FLETCHINDER" => Fletchinder(),
        "TALONFLAME" => Talonflame(),
        "SCATTERBUG" => Scatterbug(),
        "SPEWPA" => Spewpa(),
        "VIVILLON" => Vivillon(),
        "LITLEO" => Litleo(),
        "PYROAR" => Pyroar(),
        "FLABÉBÉ" => Flabebe(),
        "FLOETTE" => Floette(),
        "FLORGES" => Florges(),
        "SKIDDO" => Skiddo(),
        "GOGOAT" => Gogoat(),
        "PANCHAM" => Pancham(),
        "PANGORO" => Pangoro(),
        "FURFROU" => Furfrou(),
        "ESPURR" => Espurr(),
        "MEOWSTIC" => Meowstic(),
        "HONEDGE" => Honedge(),
        "DOUBLADE" => Doublade(),
        "AEGISLASH" => Aegislash(),
        "SPRITZEE" => Spritzee(),
        "AROMATISSE" => Aromatisse(),
        "SWIRLIX" => Swirlix(),
        "SLURPUFF" => Slurpuff(),
        // Kalos's second batch (PokemonModels.Kalos2.cs)
        "INKAY" => Inkay(),
        "MALAMAR" => Malamar(),
        "BINACLE" => Binacle(),
        "BARBARACLE" => Barbaracle(),
        "SKRELP" => Skrelp(),
        "DRAGALGE" => Dragalge(),
        "CLAUNCHER" => Clauncher(),
        "CLAWITZER" => Clawitzer(),
        "HELIOPTILE" => Helioptile(),
        "HELIOLISK" => Heliolisk(),
        "TYRUNT" => Tyrunt(),
        "TYRANTRUM" => Tyrantrum(),
        "AMAURA" => Amaura(),
        "AURORUS" => Aurorus(),
        "SYLVEON" => Sylveon(),
        "HAWLUCHA" => Hawlucha(),
        "DEDENNE" => Dedenne(),
        "CARBINK" => Carbink(),
        "GOOMY" => Goomy(),
        "SLIGGOO" => Sliggoo(),
        "GOODRA" => Goodra(),
        "KLEFKI" => Klefki(),
        "PHANTUMP" => Phantump(),
        "TREVENANT" => Trevenant(),
        "PUMPKABOO" => Pumpkaboo(),
        "GOURGEIST" => Gourgeist(),
        "BERGMITE" => Bergmite(),
        "AVALUGG" => Avalugg(),
        "NOIBAT" => Noibat(),
        "NOIVERN" => Noivern(),
        "XERNEAS" => Xerneas(),
        "YVELTAL" => Yveltal(),
        "ZYGARDE" => Zygarde(),
        "DIANCIE" => Diancie(),
        "HOOPA" => Hoopa(),
        "VOLCANION" => Volcanion(),
        // Alola's first batch (PokemonModels.Alola1.cs)
        "ROWLET" => Rowlet(),
        "DARTRIX" => Dartrix(),
        "DECIDUEYE" => Decidueye(),
        "LITTEN" => Litten(),
        "TORRACAT" => Torracat(),
        "INCINEROAR" => Incineroar(),
        "POPPLIO" => Popplio(),
        "BRIONNE" => Brionne(),
        "PRIMARINA" => Primarina(),
        "PIKIPEK" => Pikipek(),
        "TRUMBEAK" => Trumbeak(),
        "TOUCANNON" => Toucannon(),
        "YUNGOOS" => Yungoos(),
        "GUMSHOOS" => Gumshoos(),
        "GRUBBIN" => Grubbin(),
        "CHARJABUG" => Charjabug(),
        "VIKAVOLT" => Vikavolt(),
        "CRABRAWLER" => Crabrawler(),
        "CRABOMINABLE" => Crabominable(),
        "ORICORIO" => Oricorio(),
        "CUTIEFLY" => Cutiefly(),
        "RIBOMBEE" => Ribombee(),
        "ROCKRUFF" => Rockruff(),
        "LYCANROC" => Lycanroc(),
        "WISHIWASHI" => Wishiwashi(),
        "MAREANIE" => Mareanie(),
        "TOXAPEX" => Toxapex(),
        "MUDBRAY" => Mudbray(),
        "MUDSDALE" => Mudsdale(),
        "DEWPIDER" => Dewpider(),
        "ARAQUANID" => Araquanid(),
        "FOMANTIS" => Fomantis(),
        "LURANTIS" => Lurantis(),
        "MORELULL" => Morelull(),
        "SHIINOTIC" => Shiinotic(),
        "SALANDIT" => Salandit(),
        "SALAZZLE" => Salazzle(),
        "STUFFUL" => Stufful(),
        "BEWEAR" => Bewear(),
        "BOUNSWEET" => Bounsweet(),
        "STEENEE" => Steenee(),
        "TSAREENA" => Tsareena(),
        "COMFEY" => Comfey(),
        "ORANGURU" => Oranguru(),
        "PASSIMIAN" => Passimian(),
        "WIMPOD" => Wimpod(),
        "GOLISOPOD" => Golisopod(),
        "SANDYGAST" => Sandygast(),
        "PALOSSAND" => Palossand(),
        "PYUKUMUKU" => Pyukumuku(),
        "TYPE: NULL" => TypeNull(),
        "SILVALLY" => Silvally(),
        "MINIOR" => Minior(),
        "KOMALA" => Komala(),
        "TURTONATOR" => Turtonator(),
        "TOGEDEMARU" => Togedemaru(),
        "MIMIKYU" => Mimikyu(),
        "BRUXISH" => Bruxish(),
        "DRAMPA" => Drampa(),
        "DHELMISE" => Dhelmise(),
        "JANGMO-O" => Jangmoo(),
        "HAKAMO-O" => Hakamoo(),
        "KOMMO-O" => Kommoo(),
        "TAPU KOKO" => TapuKoko(),
        "TAPU LELE" => TapuLele(),
        "TAPU BULU" => TapuBulu(),
        "TAPU FINI" => TapuFini(),
        "COSMOG" => Cosmog(),
        "COSMOEM" => Cosmoem(),
        "SOLGALEO" => Solgaleo(),
        "LUNALA" => Lunala(),
        "NIHILEGO" => Nihilego(),
        "BUZZWOLE" => Buzzwole(),
        "PHEROMOSA" => Pheromosa(),
        "XURKITREE" => Xurkitree(),
        "CELESTEELA" => Celesteela(),
        "KARTANA" => Kartana(),
        "GUZZLORD" => Guzzlord(),
        "NECROZMA" => Necrozma(),
        "MAGEARNA" => Magearna(),
        "MARSHADOW" => Marshadow(),
        "POIPOLE" => Poipole(),
        "NAGANADEL" => Naganadel(),
        "STAKATAKA" => Stakataka(),
        "BLACEPHALON" => Blacephalon(),
        "ZERAORA" => Zeraora(),
        "MELTAN" => Meltan(),
        "MELMETAL" => Melmetal(),
        "HEATRAN" => Heatran(),
        "REGIGIGAS" => Regigigas(),
        "CRESSELIA" => Cresselia(),
        "PHIONE" => Phione(),
        "DARKRAI" => Darkrai(),
        "SHAYMIN" => Shaymin(),
        "ARCEUS" => Arceus(),
        "GROOKEY" => Grookey(),
        "THWACKEY" => Thwackey(),
        "RILLABOOM" => Rillaboom(),
        "SCORBUNNY" => Scorbunny(),
        "RABOOT" => Raboot(),
        "CINDERACE" => Cinderace(),
        "SOBBLE" => Sobble(),
        "DRIZZILE" => Drizzile(),
        "INTELEON" => Inteleon(),
        "SKWOVET" => Skwovet(),
        "GREEDENT" => Greedent(),
        "ROOKIDEE" => Rookidee(),
        "CORVISQUIRE" => Corvisquire(),
        "CORVIKNIGHT" => Corviknight(),
        "BLIPBUG" => Blipbug(),
        "DOTTLER" => Dottler(),
        "ORBEETLE" => Orbeetle(),
        "NICKIT" => Nickit(),
        "THIEVUL" => Thievul(),
        "GOSSIFLEUR" => Gossifleur(),
        "ELDEGOSS" => Eldegoss(),
        "WOOLOO" => Wooloo(),
        "DUBWOOL" => Dubwool(),
        "CHEWTLE" => Chewtle(),
        "DREDNAW" => Drednaw(),
        "YAMPER" => Yamper(),
        "BOLTUND" => Boltund(),
        "ROLYCOLY" => Rolycoly(),
        "CARKOL" => Carkol(),
        "COALOSSAL" => Coalossal(),
        "APPLIN" => Applin(),
        "FLAPPLE" => Flapple(),
        "APPLETUN" => Appletun(),
        "SILICOBRA" => Silicobra(),
        "SANDACONDA" => Sandaconda(),
        "CRAMORANT" => Cramorant(),
        "ARROKUDA" => Arrokuda(),
        "BARRASKEWDA" => Barraskewda(),
        "TOXEL" => Toxel(),
        "TOXTRICITY" => Toxtricity(),
        "SIZZLIPEDE" => Sizzlipede(),
        "CENTISKORCH" => Centiskorch(),
        "CLOBBOPUS" => Clobbopus(),
        "GRAPPLOCT" => Grapploct(),
        "SINISTEA" => Sinistea(),
        "POLTEAGEIST" => Polteageist(),
        "HATENNA" => Hatenna(),
        "HATTREM" => Hattrem(),
        "HATTERENE" => Hatterene(),
        "IMPIDIMP" => Impidimp(),
        "MORGREM" => Morgrem(),
        "GRIMMSNARL" => Grimmsnarl(),
        "OBSTAGOON" => Obstagoon(),
        "PERRSERKER" => Perrserker(),
        "CURSOLA" => Cursola(),
        "SIRFETCH'D" => Sirfetchd(),
        "MR. RIME" => MrRime(),
        "RUNERIGUS" => Runerigus(),
        "MILCERY" => Milcery(),
        "ALCREMIE" => Alcremie(),
        "FALINKS" => Falinks(),
        "PINCURCHIN" => Pincurchin(),
        "SNOM" => Snom(),
        "FROSMOTH" => Frosmoth(),
        "STONJOURNER" => Stonjourner(),
        "EISCUE" => Eiscue(),
        "INDEEDEE" => Indeedee(),
        "MORPEKO" => Morpeko(),
        "CUFANT" => Cufant(),
        "COPPERAJAH" => Copperajah(),
        "DRACOZOLT" => Dracozolt(),
        "ARCTOZOLT" => Arctozolt(),
        "DRACOVISH" => Dracovish(),
        "ARCTOVISH" => Arctovish(),
        "DURALUDON" => Duraludon(),
        "DREEPY" => Dreepy(),
        "DRAKLOAK" => Drakloak(),
        "DRAGAPULT" => Dragapult(),
        "ZACIAN" => Zacian(),
        "ZAMAZENTA" => Zamazenta(),
        "ETERNATUS" => Eternatus(),
        "KUBFU" => Kubfu(),
        "URSHIFU" => Urshifu(),
        "ZARUDE" => Zarude(),
        "REGIELEKI" => Regieleki(),
        "REGIDRAGO" => Regidrago(),
        "GLASTRIER" => Glastrier(),
        "SPECTRIER" => Spectrier(),
        "CALYREX" => Calyrex(),
        "WYRDEER" => Wyrdeer(),
        "KLEAVOR" => Kleavor(),
        "URSALUNA" => Ursaluna(),
        "BASCULEGION" => Basculegion(),
        "SNEASLER" => Sneasler(),
        "OVERQWIL" => Overqwil(),
        "ENAMORUS" => Enamorus(),
        "SPRIGATITO" => Sprigatito(),
        "FLORAGATO" => Floragato(),
        "MEOWSCARADA" => Meowscarada(),
        "FUECOCO" => Fuecoco(),
        "CROCALOR" => Crocalor(),
        "SKELEDIRGE" => Skeledirge(),
        "QUAXLY" => Quaxly(),
        "QUAXWELL" => Quaxwell(),
        "QUAQUAVAL" => Quaquaval(),
        "LECHONK" => Lechonk(),
        "OINKOLOGNE" => Oinkologne(),
        "TAROUNTULA" => Tarountula(),
        "SPIDOPS" => Spidops(),
        "NYMBLE" => Nymble(),
        "LOKIX" => Lokix(),
        "PAWMI" => Pawmi(),
        "PAWMO" => Pawmo(),
        "PAWMOT" => Pawmot(),
        "TANDEMAUS" => Tandemaus(),
        "MAUSHOLD" => Maushold(),
        "FIDOUGH" => Fidough(),
        "DACHSBUN" => Dachsbun(),
        "SMOLIV" => Smoliv(),
        "DOLLIV" => Dolliv(),
        "ARBOLIVA" => Arboliva(),
        "SQUAWKABILLY" => Squawkabilly(),
        "NACLI" => Nacli(),
        "NACLSTACK" => Naclstack(),
        "GARGANACL" => Garganacl(),
        "CHARCADET" => Charcadet(),
        "ARMAROUGE" => Armarouge(),
        "CERULEDGE" => Ceruledge(),
        "TADBULB" => Tadbulb(),
        "BELLIBOLT" => Bellibolt(),
        "WATTREL" => Wattrel(),
        "KILOWATTREL" => Kilowattrel(),
        "MASCHIFF" => Maschiff(),
        "MABOSSTIFF" => Mabosstiff(),
        "SHROODLE" => Shroodle(),
        "GRAFAIAI" => Grafaiai(),
        "BRAMBLIN" => Bramblin(),
        "BRAMBLEGHAST" => Brambleghast(),
        "TOEDSCOOL" => Toedscool(),
        "TOEDSCRUEL" => Toedscruel(),
        "KLAWF" => Klawf(),
        // Platinum's own forms of its Sinnoh species (PokemonModels.Forms.cs)
        "ROTOM-HEAT" => RotomHeat(),
        "ROTOM-WASH" => RotomWash(),
        "ROTOM-FROST" => RotomFrost(),
        "ROTOM-FAN" => RotomFan(),
        "ROTOM-MOW" => RotomMow(),
        "GIRATINA-ORIGIN" => GiratinaOrigin(),
        "BURMY-SANDY" => Burmy(Cloak.Sandy),
        "BURMY-TRASH" => Burmy(Cloak.Trash),
        "WORMADAM-SANDY" => Wormadam(Cloak.Sandy),
        "WORMADAM-TRASH" => Wormadam(Cloak.Trash),
        "SHELLOS-EAST" => SeaSlug(false, east: true),
        "GASTRODON-EAST" => SeaSlug(true, east: true),
        "CHERRIM-SUNSHINE" => CherrimSunshine(),
        var unown when unown.StartsWith("UNOWN-") && UnownGlyphs.ContainsKey(unown[6..]) => Unown(unown[6..]),
        // Platinum's forms of the popular species from outside its Pokédex (PokemonModels.Forms.cs)
        "CASTFORM-SUNNY" => CastformSunny(),
        "CASTFORM-RAINY" => CastformRainy(),
        "CASTFORM-SNOWY" => CastformSnowy(),
        "DEOXYS-ATTACK" => DeoxysAttack(),
        "DEOXYS-DEFENSE" => DeoxysDefense(),
        "DEOXYS-SPEED" => DeoxysSpeed(),
        "SHAYMIN-SKY" => ShayminSky(),
        var arceus when arceus.StartsWith("ARCEUS-") && ArceusForm(arceus) != null => ArceusForm(arceus)!,
        // The later games' regional forms, Dialga's and Palkia's Origin Formes and the other forms that are neither Mega nor Gigantamax (PokemonModels.Regional.cs)
        "RATTATA-ALOLA" => RattataAlola(),
        "RATICATE-ALOLA" => RaticateAlola(),
        "RAICHU-ALOLA" => RaichuAlola(),
        "SANDSHREW-ALOLA" => SandshrewAlola(),
        "SANDSLASH-ALOLA" => SandslashAlola(),
        "VULPIX-ALOLA" => VulpixAlola(),
        "NINETALES-ALOLA" => NinetalesAlola(),
        "DIGLETT-ALOLA" => DiglettAlola(),
        "DUGTRIO-ALOLA" => DugtrioAlola(),
        "MEOWTH-ALOLA" => MeowthAlola(),
        "MEOWTH-GALAR" => MeowthGalar(),
        "PERSIAN-ALOLA" => PersianAlola(),
        "GROWLITHE-HISUI" => GrowlitheHisui(),
        "ARCANINE-HISUI" => ArcanineHisui(),
        "GEODUDE-ALOLA" => GeodudeAlola(),
        "GRAVELER-ALOLA" => GravelerAlola(),
        "GOLEM-ALOLA" => GolemAlola(),
        "PONYTA-GALAR" => HorseGalar(false),
        "RAPIDASH-GALAR" => HorseGalar(true),
        "SLOWPOKE-GALAR" => SlowpokeGalar(),
        "SLOWBRO-GALAR" => SlowbroGalar(),
        "FARFETCH'D-GALAR" => FarfetchdGalar(),
        "GRIMER-ALOLA" => GrimerAlola(),
        "MUK-ALOLA" => MukAlola(),
        "VOLTORB-HISUI" => VoltorbHisui(),
        "ELECTRODE-HISUI" => ElectrodeHisui(),
        "EXEGGUTOR-ALOLA" => ExeggutorAlola(),
        "MAROWAK-ALOLA" => MarowakAlola(),
        "WEEZING-GALAR" => WeezingGalar(),
        "MR. MIME-GALAR" => MrMimeGalar(),
        "TAUROS-PALDEA-COMBAT-BREED" => TaurosCombat(),
        "TAUROS-PALDEA-BLAZE-BREED" => TaurosBlaze(),
        "TAUROS-PALDEA-AQUA-BREED" => TaurosAqua(),
        "ARTICUNO-GALAR" => ArticunoGalar(),
        "ZAPDOS-GALAR" => ZapdosGalar(),
        "MOLTRES-GALAR" => MoltresGalar(),
        "TYPHLOSION-HISUI" => TyphlosionHisui(),
        "SLOWKING-GALAR" => SlowkingGalar(),
        "QWILFISH-HISUI" => QwilfishHisui(),
        "CORSOLA-GALAR" => CorsolaGalar(),
        "ZIGZAGOON-GALAR" => ZigzagoonGalar(),
        "LINOONE-GALAR" => LinooneGalar(),
        "WOOPER-PALDEA" => WooperPaldea(),
        "SNEASEL-HISUI" => SneaselHisui(),
        "DIALGA-ORIGIN" => DialgaOrigin(),
        "PALKIA-ORIGIN" => PalkiaOrigin(),
        "SAMUROTT-HISUI" => SamurottHisui(),
        "LILLIGANT-HISUI" => LilligantHisui(),
        "BASCULIN-BLUE-STRIPED" => BasculinBlue(),
        "BASCULIN-WHITE-STRIPED" => BasculinWhite(),
        "DARUMAKA-GALAR" => DarumakaGalar(),
        "DARMANITAN-ZEN" => DarmanitanZen(),
        "DARMANITAN-GALAR-STANDARD" => DarmanitanGalar(),
        "DARMANITAN-GALAR-ZEN" => DarmanitanGalarZen(),
        "YAMASK-GALAR" => YamaskGalar(),
        "ZORUA-HISUI" => ZoruaHisui(),
        "ZOROARK-HISUI" => ZoroarkHisui(),
        "DEERLING-SUMMER" => DeerlingSummer(),
        "DEERLING-AUTUMN" => DeerlingAutumn(),
        "DEERLING-WINTER" => DeerlingWinter(),
        "SAWSBUCK-SUMMER" => SawsbuckSummer(),
        "SAWSBUCK-AUTUMN" => SawsbuckAutumn(),
        "SAWSBUCK-WINTER" => SawsbuckWinter(),
        "FRILLISH-FEMALE" => FrillishFemale(),
        "JELLICENT-FEMALE" => JellicentFemale(),
        "STUNFISK-GALAR" => StunfiskGalar(),
        "BRAVIARY-HISUI" => BraviaryHisui(),
        "TORNADUS-THERIAN" => TornadusTherian(),
        "THUNDURUS-THERIAN" => ThundurusTherian(),
        "LANDORUS-THERIAN" => LandorusTherian(),
        "KYUREM-WHITE" => KyuremWhite(),
        "KYUREM-BLACK" => KyuremBlack(),
        "KELDEO-RESOLUTE" => KeldeoResolute(),
        "MELOETTA-PIROUETTE" => MeloettaPirouette(),
        "GENESECT-DOUSE" => GenesectDouse(),
        "GENESECT-SHOCK" => GenesectShock(),
        "GENESECT-BURN" => GenesectBurn(),
        "GENESECT-CHILL" => GenesectChill(),
        "GRENINJA-ASH" => GreninjaAsh(),
        var vivillon when vivillon.StartsWith("VIVILLON-") && VivillonForm(vivillon) != null => VivillonForm(vivillon)!,
        "PYROAR-FEMALE" => PyroarFemale(),
        "FLABÉBÉ-YELLOW" => FlabebeYellow(),
        "FLABÉBÉ-ORANGE" => FlabebeOrange(),
        "FLABÉBÉ-BLUE" => FlabebeBlue(),
        "FLABÉBÉ-WHITE" => FlabebeWhite(),
        "FLOETTE-YELLOW" => FloetteYellow(),
        "FLOETTE-ORANGE" => FloetteOrange(),
        "FLOETTE-BLUE" => FloetteBlue(),
        "FLOETTE-WHITE" => FloetteWhite(),
        "FLOETTE-ETERNAL" => FloetteEternal(),
        "FLORGES-YELLOW" => FlorgesYellow(),
        "FLORGES-ORANGE" => FlorgesOrange(),
        "FLORGES-BLUE" => FlorgesBlue(),
        "FLORGES-WHITE" => FlorgesWhite(),
        var furfrou when furfrou.StartsWith("FURFROU-") && FurfrouForm(furfrou) != null => FurfrouForm(furfrou)!,
        "MEOWSTIC-FEMALE" => MeowsticFemale(),
        "AEGISLASH-BLADE" => AegislashBlade(),
        "SLIGGOO-HISUI" => SliggooHisui(),
        "GOODRA-HISUI" => GoodraHisui(),
        "PUMPKABOO-SMALL" => PumpkabooSmall(),
        "PUMPKABOO-LARGE" => PumpkabooLarge(),
        "PUMPKABOO-SUPER" => PumpkabooSuper(),
        "GOURGEIST-SMALL" => GourgeistSmall(),
        "GOURGEIST-LARGE" => GourgeistLarge(),
        "GOURGEIST-SUPER" => GourgeistSuper(),
        "AVALUGG-HISUI" => AvaluggHisui(),
        "XERNEAS-ACTIVE" => XerneasActive(),
        "ZYGARDE-10" => Zygarde10(),
        "ZYGARDE-COMPLETE" => ZygardeComplete(),
        "HOOPA-UNBOUND" => HoopaUnbound(),
        "DECIDUEYE-HISUI" => DecidueyeHisui(),
        "ORICORIO-POM-POM" => OricorioBuild(1),
        "ORICORIO-PAU" => OricorioBuild(2),
        "ORICORIO-SENSU" => OricorioBuild(3),
        "LYCANROC-MIDNIGHT" => LycanrocBuild(1),
        "LYCANROC-DUSK" => LycanrocBuild(2),
        "WISHIWASHI-SCHOOL" => WishiwashiBuild(true),
        var silvally when silvally.StartsWith("SILVALLY-") && SilvallyForm(silvally) != null => SilvallyForm(silvally)!,
        var minior when minior.StartsWith("MINIOR-") && MiniorForm(minior) != null => MiniorForm(minior)!,
        "MIMIKYU-BUSTED" => MimikyuBusted(),
        "NECROZMA-DUSK" => NecrozmaDusk(),
        "NECROZMA-DAWN" => NecrozmaDawn(),
        "NECROZMA-ULTRA" => NecrozmaUltra(),
        "MAGEARNA-ORIGINAL" => MagearnaOriginal(),
        "CRAMORANT-GULPING" => CramorantGulping(),
        "CRAMORANT-GORGING" => CramorantGorging(),
        "TOXTRICITY-LOW-KEY" => ToxtricityLowKey(),
        var alcremie when alcremie.StartsWith("ALCREMIE-") && AlcremieForm(alcremie) != null => AlcremieForm(alcremie)!,
        "EISCUE-NOICE" => EiscueNoice(),
        "INDEEDEE-FEMALE" => IndeedeeFemale(),
        "MORPEKO-HANGRY" => MorpekoHangry(),
        "ZACIAN-CROWNED" => ZacianCrowned(),
        "ZAMAZENTA-CROWNED" => ZamazentaCrowned(),
        "ETERNATUS-ETERNAMAX" => EternatusEternamax(),
        "URSHIFU-RAPID-STRIKE" => UrshifuRapidStrike(),
        "ZARUDE-DADA" => ZarudeDada(),
        "CALYREX-ICE" => CalyrexIce(),
        "CALYREX-SHADOW" => CalyrexShadow(),
        "URSALUNA-BLOODMOON" => UrsalunaBloodmoon(),
        "BASCULEGION-FEMALE" => BasculegionFemale(),
        "ENAMORUS-THERIAN" => EnamorusTherian(),
        "OINKOLOGNE-FEMALE" => OinkologneFemale(),
        "MAUSHOLD-FAMILY-OF-THREE" => MausholdFamilyOfThree(),
        "SQUAWKABILLY-BLUE-PLUMAGE" => SquawkabillyBlue(),
        "SQUAWKABILLY-YELLOW-PLUMAGE" => SquawkabillyYellow(),
        "SQUAWKABILLY-WHITE-PLUMAGE" => SquawkabillyWhite(),
        // The Mega Evolutions (PokemonModels.Megas.cs)
        "VENUSAUR-MEGA" => VenusaurMega(),
        "CHARIZARD-MEGA-X" => CharizardMegaX(),
        "CHARIZARD-MEGA-Y" => CharizardMegaY(),
        "BLASTOISE-MEGA" => BlastoiseMega(),
        "BEEDRILL-MEGA" => BeedrillMega(),
        "PIDGEOT-MEGA" => PidgeotMega(),
        "RAICHU-MEGA-X" => RaichuMegaX(),
        "RAICHU-MEGA-Y" => RaichuMegaY(),
        "CLEFABLE-MEGA" => ClefableMega(),
        "ALAKAZAM-MEGA" => AlakazamMega(),
        "VICTREEBEL-MEGA" => VictreebelMega(),
        "SLOWBRO-MEGA" => SlowbroMega(),
        "GENGAR-MEGA" => GengarMega(),
        "KANGASKHAN-MEGA" => KangaskhanMega(),
        "STARMIE-MEGA" => StarmieMega(),
        "PINSIR-MEGA" => PinsirMega(),
        "GYARADOS-MEGA" => GyaradosMega(),
        "AERODACTYL-MEGA" => AerodactylMega(),
        "DRAGONITE-MEGA" => DragoniteMega(),
        "MEWTWO-MEGA-X" => MewtwoMegaX(),
        "MEWTWO-MEGA-Y" => MewtwoMegaY(),
        "MEGANIUM-MEGA" => MeganiumMega(),
        "FERALIGATR-MEGA" => FeraligatrMega(),
        "AMPHAROS-MEGA" => AmpharosMega(),
        "STEELIX-MEGA" => SteelixMega(),
        "SCIZOR-MEGA" => ScizorMega(),
        "HERACROSS-MEGA" => HeracrossMega(),
        "SKARMORY-MEGA" => SkarmoryMega(),
        "HOUNDOOM-MEGA" => HoundoomMega(),
        "TYRANITAR-MEGA" => TyranitarMega(),
        "SCEPTILE-MEGA" => SceptileMega(),
        "BLAZIKEN-MEGA" => BlazikenMega(),
        "SWAMPERT-MEGA" => SwampertMega(),
        "GARDEVOIR-MEGA" => GardevoirMega(),
        "SABLEYE-MEGA" => SableyeMega(),
        "MAWILE-MEGA" => MawileMega(),
        "AGGRON-MEGA" => AggronMega(),
        "MEDICHAM-MEGA" => MedichamMega(),
        "MANECTRIC-MEGA" => ManectricMega(),
        "SHARPEDO-MEGA" => SharpedoMega(),
        "CAMERUPT-MEGA" => CameruptMega(),
        "ALTARIA-MEGA" => AltariaMega(),
        "BANETTE-MEGA" => BanetteMega(),
        "CHIMECHO-MEGA" => ChimechoMega(),
        "ABSOL-MEGA" => AbsolMega(),
        "ABSOL-MEGA-Z" => AbsolMegaZ(),
        "GLALIE-MEGA" => GlalieMega(),
        "SALAMENCE-MEGA" => SalamenceMega(),
        "METAGROSS-MEGA" => MetagrossMega(),
        "LATIAS-MEGA" => LatiasMega(),
        "LATIOS-MEGA" => LatiosMega(),
        "KYOGRE-PRIMAL" => KyogrePrimal(),
        "GROUDON-PRIMAL" => GroudonPrimal(),
        "RAYQUAZA-MEGA" => RayquazaMega(),
        "STARAPTOR-MEGA" => StaraptorMega(),
        "LOPUNNY-MEGA" => LopunnyMega(),
        "GARCHOMP-MEGA" => GarchompMega(),
        "GARCHOMP-MEGA-Z" => GarchompMegaZ(),
        "LUCARIO-MEGA" => LucarioMega(),
        "LUCARIO-MEGA-Z" => LucarioMegaZ(),
        "ABOMASNOW-MEGA" => AbomasnowMega(),
        "GALLADE-MEGA" => GalladeMega(),
        "FROSLASS-MEGA" => FroslassMega(),
        "HEATRAN-MEGA" => HeatranMega(),
        "DARKRAI-MEGA" => DarkraiMega(),
        "EMBOAR-MEGA" => EmboarMega(),
        "EXCADRILL-MEGA" => ExcadrillMega(),
        "AUDINO-MEGA" => AudinoMega(),
        "SCOLIPEDE-MEGA" => ScolipedeMega(),
        "SCRAFTY-MEGA" => ScraftyMega(),
        "EELEKTROSS-MEGA" => EelektrossMega(),
        "CHANDELURE-MEGA" => ChandelureMega(),
        "GOLURK-MEGA" => GolurkMega(),
        "CHESNAUGHT-MEGA" => ChesnaughtMega(),
        "DELPHOX-MEGA" => DelphoxMega(),
        "GRENINJA-MEGA" => GreninjaMega(),
        "PYROAR-MEGA" => PyroarMega(),
        "FLOETTE-MEGA" => FloetteMega(),
        "MEOWSTIC-MALE-MEGA" => MeowsticMaleMega(),
        "MEOWSTIC-FEMALE-MEGA" => MeowsticFemaleMega(),
        "MALAMAR-MEGA" => MalamarMega(),
        "BARBARACLE-MEGA" => BarbaracleMega(),
        "DRAGALGE-MEGA" => DragalgeMega(),
        "HAWLUCHA-MEGA" => HawluchaMega(),
        "ZYGARDE-MEGA" => ZygardeMega(),
        "DIANCIE-MEGA" => DiancieMega(),
        "CRABOMINABLE-MEGA" => CrabominableMega(),
        "GOLISOPOD-MEGA" => GolisopodMega(),
        "DRAMPA-MEGA" => DrampaMega(),
        "MAGEARNA-MEGA" => MagearnaMega(),
        "MAGEARNA-ORIGINAL-MEGA" => MagearnaOriginalMega(),
        "ZERAORA-MEGA" => ZeraoraMega(),
        "FALINKS-MEGA" => FalinksMega(),
        // Gigantamax (PokemonModels.Gigantamax.cs), and Pikachu's caps and costumes and the spiky-eared Pichu (PokemonModels.Pikachu.cs)
        "VENUSAUR-GMAX" => VenusaurGmax(),
        "CHARIZARD-GMAX" => CharizardGmax(),
        "BLASTOISE-GMAX" => BlastoiseGmax(),
        "BUTTERFREE-GMAX" => ButterfreeGmax(),
        "PIKACHU-GMAX" => PikachuGmax(),
        "MEOWTH-GMAX" => MeowthGmax(),
        "MACHAMP-GMAX" => MachampGmax(),
        "GENGAR-GMAX" => GengarGmax(),
        "KINGLER-GMAX" => KinglerGmax(),
        "LAPRAS-GMAX" => LaprasGmax(),
        "EEVEE-GMAX" => EeveeGmax(),
        "SNORLAX-GMAX" => SnorlaxGmax(),
        "GARBODOR-GMAX" => GarbodorGmax(),
        "MELMETAL-GMAX" => MelmetalGmax(),
        "RILLABOOM-GMAX" => RillaboomGmax(),
        "CINDERACE-GMAX" => CinderaceGmax(),
        "INTELEON-GMAX" => InteleonGmax(),
        "CORVIKNIGHT-GMAX" => CorviknightGmax(),
        "ORBEETLE-GMAX" => OrbeetleGmax(),
        "DREDNAW-GMAX" => DrednawGmax(),
        "COALOSSAL-GMAX" => CoalossalGmax(),
        "FLAPPLE-GMAX" => FlappleGmax(),
        "SANDACONDA-GMAX" => SandacondaGmax(),
        "TOXTRICITY-AMPED-GMAX" => ToxtricityAmpedGmax(),
        "CENTISKORCH-GMAX" => CentiskorchGmax(),
        "HATTERENE-GMAX" => HattereneGmax(),
        "GRIMMSNARL-GMAX" => GrimmsnarlGmax(),
        "ALCREMIE-GMAX" => AlcremieGmax(),
        "COPPERAJAH-GMAX" => CopperajahGmax(),
        "DURALUDON-GMAX" => DuraludonGmax(),
        "URSHIFU-SINGLE-STRIKE-GMAX" => UrshifuSingleStrikeGmax(),
        "URSHIFU-RAPID-STRIKE-GMAX" => UrshifuRapidStrikeGmax(),
        "PICHU-SPIKY-EARED" => Pichu(spikyEared: true),
        var pikachu when pikachu.StartsWith("PIKACHU-") && PikachuForms.Contains(pikachu) => Pikachu(Array.Find(Forms, f => f.Equals(pikachu, StringComparison.OrdinalIgnoreCase))),
        // A form that looks just like another form of its species shows that form's sculpt
        _ when LooksLike.TryGetValue(species, out var like) => Create(like),
        // A form of a hand-built species shows its species' model until it has one of its own (plan 03 · D11)
        _ when PokemonDatabase.SpeciesOfForm(species) is { } owner && HasModel(owner.Name) => Create(owner.Name),
        // Every other species and form is generated from its data (plan 03 · D5); a name that is neither gets the stand-in
        _ => PokemonGenerator.Build(species) ?? Generic(species)
    };

    /// <summary>Meshes the sculpted model, its outline shell and its decals.</summary>
    internal static PokeModel Finish(PokeBuilder b)
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
        // The twig's tip, on the leaves' bone, reaches up to where the two leaves meet
        b.Limb(leaves, V(0, 0.84f, 0.22f), V(0, 0.92f, 0.22f), 0.025f, 0.02f, Rgb(116, 78, 46), Shell);
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

    // ------------------------------------------------------------------ Buneary (the Pokémon the professor shows in the introduction)

    private static PokeBuilder Buneary()
    {
        var b = new PokeBuilder("Buneary", 0.6f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var brown = Rgb(152, 106, 74);
        var cream = Rgb(246, 228, 182);
        var pink = Rgb(238, 150, 162);

        // Big fluffy feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            b.Limb(leg, V(0.08f * s, 0.2f, 0), V(0.09f * s, 0.07f, 0.02f), 0.06f, 0.055f, cream);
            b.Ell(leg, V(0.095f * s, 0.045f, 0.065f), V(0.076f, 0.045f, 0.115f), cream);
        });
        b.Ell(Body, V(0, 0.335f, 0), V(0.15f, 0.15f, 0.132f), brown);
        // The fluff it wears round its middle
        b.Ell(Body, V(0, 0.235f, 0), V(0.2f, 0.105f, 0.18f), cream);
        int tail = b.Tail(V(0, 0.25f, -0.13f));
        b.Ell(tail, V(0, 0.26f, -0.175f), V(0.058f, 0.058f, 0.058f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.41f, 0.02f));
            b.Limb(arm, V(0.12f * s, 0.41f, 0.02f), V(0.175f * s, 0.31f, 0.08f), 0.042f, 0.036f, brown);
        });

        int head = b.Head(V(0, 0.47f, 0));
        float hy = 0.6f;
        b.Ell(head, V(0, hy, 0.02f), V(0.2f, 0.175f, 0.175f), brown);
        b.Mark(head, V(0, hy - 0.04f, 0.195f), V(0, 0.1f, 1f), 0.022f, 0.015f, pink);

        // One ear stands tall with a cream tuft at its tip; the other is rolled up beside the head
        int tall = b.Ear(head, 1f, V(0.1f, hy + 0.13f, 0));
        b.Limb(tall, V(0.1f, hy + 0.11f, 0), V(0.13f, hy + 0.33f, -0.03f), 0.062f, 0.056f, brown);
        b.Ell(tall, V(0.135f, hy + 0.39f, -0.035f), V(0.078f, 0.085f, 0.072f), cream);
        int rolled = b.Ear(head, -1f, V(-0.1f, hy + 0.13f, 0));
        b.Limb(rolled, V(-0.1f, hy + 0.11f, 0), V(-0.17f, hy + 0.19f, -0.02f), 0.06f, 0.06f, brown);
        b.Ell(rolled, V(-0.205f, hy + 0.2f, -0.02f), V(0.088f, 0.088f, 0.08f), cream);

        // Cream spots over the eyes
        PokeBuilder.Both(s => b.Mark(head, V(0.082f * s, hy + 0.095f, 0.165f), V(0.35f * s, 0.4f, 1f), 0.026f, 0.017f, cream));
        PokeBuilder.Both(s => b.Eye(head, V(0.082f * s, hy + 0.02f, 0.178f), V(0.4f * s, 0.05f, 1f), 0.045f, Rgb(96, 62, 50)));
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
        // The tail's root runs from the small of its back into the tail
        b.Limb(tail, V(0, 0.48f, -0.07f), V(0, 0.44f, -0.17f), 0.035f, 0.045f, blue);
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
            // The wing's root, sunk into the shoulder, holds its three struts
            b.Ell(wing, V(0.19f * s, 0.6f, -0.03f), V(0.06f, 0.05f, 0.08f), black);
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
