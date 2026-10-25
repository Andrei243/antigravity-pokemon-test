using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 1: Kanto's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-kanto.md says: the forebear's build is taken whole where the body stays (renamed,
// stretched, its colours changed, a part taken away) and the new features are added to it, or, where the body
// changes its plan, it is built anew from the forebear's parts and colours. The forebears' own sculpts are never
// touched.
internal static partial class PokemonModels
{
    /// <summary>The species of our own of batch 1 with a sculpt of their own, by name in capitals (null for any other).</summary>
    private static PokeBuilder? CreateOurs(string species) => species.ToUpperInvariant() switch
    {
        "RATCHISEL" => Ratchisel(),
        "LANCEROW" => Lancerow(),
        "NAJAROK" => Najarok(),
        "SANDSPIRE" => Sandspire(),
        "KITSUNOX" => Kitsunox(),
        "CORDYHUSK" => Cordyhusk(),
        "SOMNIMOTH" => Somnimoth(),
        "MOLEMOUNT" => Molemount(),
        "REGALYNX" => Regalynx(),
        "PSYDUKE" => Psyduke(),
        "KIRININE" => Kirinine(),
        "MEDUSARCH" => Medusarch(),
        "SOLARION" => Solarion(),
        "SLOWMANTLE" => Slowmantle(),
        "PORRIGARD" => Porrigard(),
        "TRIPLUMA" => Tripluma(),
        "GLACITUSK" => Glacitusk(),
        "SLUDGEMIRE" => Sludgemire(),
        "CLAMSPIRE" => Clamspire(),
        "MESMERYX" => Mesmeryx(),
        "PINCEREX" => Pincerex(),
        "DYNAMORB" => Dynamorb(),
        "PALMAGUS" => Palmagus(),
        "OSSUGARD" => Ossugard(),
        "SAVATEUR" => Savateur(),
        "PUGILORD" => Pugilord(),
        "SMOGLOM" => Smoglom(),
        "MATRIROO" => Matriroo(),
        "SABREFIN" => Sabrefin(),
        "STELLAMIR" => Stellamir(),
        "MIMESTRO" => Mimestro(),
        "AURORELLE" => Aurorelle(),
        "MANDIBRON" => Mandibron(),
        "BOVARCH" => Bovarch(),
        "TYPHONARCH" => Typhonarch(),
        "FERRISLE" => Ferrisle(),
        "NAIADEON" => Naiadeon(),
        "FULGUREON" => Fulgureon(),
        "PYREON" => Pyreon(),
        "AMMONARCH" => Ammonarch(),
        "EURYPTEROX" => Eurypterox(),
        "AZHDARCH" => Azhdarch(),
        "DOZEMOUNT" => Dozemount(),
        "GLACIRUNE" => Glacirune(),
        "FULGURIS" => Fulguris(),
        "PYREVANT" => Pyrevant(),
        _ => null
    };

    // ------------------------------------------------------------------ building on a forebear's sculpt

    /// <summary>
    /// Takes a forebear's finished build as the start of one of ours: renamed (the model and its description), and
    /// filling <paramref name="fill"/> of the sprite frame.
    /// </summary>
    private static PokeBuilder Become(PokeBuilder b, string species, float fill)
    {
        b.Model.Species = species;
        b.Model.Fill = fill;
        b.Sdf.Name = "pokemon " + species;
        return b;
    }

    /// <summary>Changes every shape and mark of one colour to another (exact colours, as the forebear's code names them).</summary>
    private static void Recolour(PokeBuilder b, params (Color from, Color to)[] map)
    {
        static bool Same(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;
        foreach (var s in b.Sdf.Shapes)
            foreach (var (from, to) in map)
                if (Same(s.Color, from)) { s.Color = to; break; }
        foreach (var d in b.Model.Decals)
            foreach (var (from, to) in map)
            {
                if (Same(d.Color, from)) d.Color = to;
                if (d.Iris is { } iris && Same(iris, from)) d.Iris = to;
            }
        b.Sdf.Changed();
    }

    /// <summary>
    /// Stretches the whole sculpt about the origin by <paramref name="k"/> (taller and leaner, longer and lower):
    /// every shape's place and size, the joints and the marks. A shape turned askew is sized by how much each of its
    /// own axes is stretched, which is near enough for the soft shapes of a sculpt.
    /// </summary>
    private static void Stretch(PokeBuilder b, Vector3 k)
    {
        Grow(b, k, Vector3.Zero, (SdfPrimitive _) => true, _ => true);
        b.Model.Skeleton.Scale(k);
    }

    /// <summary>
    /// Stretches the shapes of the bones <paramref name="bones"/> says, and their marks, by <paramref name="k"/> about
    /// <paramref name="about"/> (a wing broadened from its shoulder, a shell grown round its middle); the joints stay.
    /// </summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<int, bool> bones) =>
        Grow(b, k, about, s => bones(s.Bone), bones);

    /// <summary>Stretches the shapes <paramref name="which"/> picks by <paramref name="k"/> about <paramref name="about"/>, and the marks of the bones <paramref name="marks"/> picks.</summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<SdfPrimitive, bool> which, Func<int, bool> marks)
    {
        float Along(Vector3 axis) => (k * Vector3.Normalize(axis)).Length();
        float Across(Vector3 axis)
        {
            var d = Vector3.Normalize(axis);
            return (k.X * (1f - d.X * d.X) + k.Y * (1f - d.Y * d.Y) + k.Z * (1f - d.Z * d.Z)) / 2f;
        }
        float mean = (k.X + k.Y + k.Z) / 3f;
        foreach (var s in b.Sdf.Shapes)
        {
            if (!which(s)) continue;
            var q = s.Rotation;
            var ax = Vector3.Transform(Vector3.UnitX, q);
            var ay = Vector3.Transform(Vector3.UnitY, q);
            var az = Vector3.Transform(Vector3.UnitZ, q);
            switch (s.Shape)
            {
                case SdfShape.RoundCone:
                    float across = s.B == s.A ? mean : Across(s.B - s.A);
                    s.Size = new Vector3(s.Size.X * across, s.Size.Y * across, s.Size.Z);
                    break;
                case SdfShape.Sphere:
                    s.Size = new Vector3(s.Size.X * mean, s.Size.Y, s.Size.Z);
                    break;
                case SdfShape.Torus:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * mean, s.Size.Z);
                    break;
                case SdfShape.Cylinder:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * Along(ay), s.Size.Z);
                    break;
                default:
                    s.Size = new Vector3(s.Size.X * Along(ax), s.Size.Y * Along(ay), s.Size.Z * Along(az));
                    break;
            }
            s.A = about + (s.A - about) * k;
            s.B = about + (s.B - about) * k;
            s.Blend *= mean;
        }
        b.Sdf.Changed();
        foreach (var d in b.Model.Decals)
        {
            if (!marks(d.Bone)) continue;
            d.Center = about + (d.Center - about) * k;
            d.Normal = Vector3.Normalize(d.Normal / k);
        }
    }

    /// <summary>Moves the shapes and marks of the bones <paramref name="bones"/> picks by <paramref name="d"/> (a head carried up on a longer neck); the joints stay.</summary>
    private static void Move(PokeBuilder b, Func<int, bool> bones, Vector3 d)
    {
        foreach (var s in b.Sdf.Shapes)
            if (bones(s.Bone))
            {
                s.A += d;
                s.B += d;
            }
        b.Sdf.Changed();
        foreach (var m in b.Model.Decals)
            if (bones(m.Bone)) m.Center += d;
    }

    /// <summary>Paints a colour and a material together over the surface of <paramref name="bone"/> inside an ellipsoid (steel over a claw).</summary>
    private static void PaintAs(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Color color, SurfaceMaterial mat, Vector3 rotationDeg = default)
    {
        var rot = Quaternion.CreateFromYawPitchRoll(rotationDeg.Y * Degree, rotationDeg.X * Degree, rotationDeg.Z * Degree);
        b.Sdf.Paint(b.Sdf.Ellipsoid(center, radii, color, bone, PokeBuilder.PaintEdge, mat, rot), true, bone);
    }

    /// <summary>A bone of the build by its name (it must be there).</summary>
    private static int BoneOf(PokeBuilder b, string name)
    {
        int i = b.Model.Skeleton.Find(name);
        if (i < 0) throw new InvalidOperationException($"{b.Model.Species} has no bone {name}");
        return i;
    }

    private static readonly Color Steel = Rgb(170, 176, 188);

    // ------------------------------------------------------------------ 2020 Ratchisel, from Raticate

    /// <summary>
    /// Ratchisel: Raticate down on all fours, lower and longer, its brown fur grizzled grey along the back, its
    /// whiskers kept, its buck teeth grown into square chisels of steel and its scaly tail thickened with three
    /// steel rings.
    /// </summary>
    /// <param name="female">A female keeps Raticate's: shorter whiskers.</param>
    private static PokeBuilder Ratchisel(bool female = false)
    {
        var b = new PokeBuilder("Ratchisel", 0.84f, BodyPlan.Quadruped, V(0, 0.25f, -0.04f)) { Coat = Fur };
        var fur = Rgb(200, 150, 90);
        var grey = Rgb(150, 148, 146);
        var cream = Rgb(244, 222, 176);
        var tailColor = Rgb(214, 176, 122);
        foreach (var (z, front) in new[] { (0.14f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.2f, z), front);
                if (front) b.Limb(leg, V(0.1f * s, 0.22f, z), V(0.11f * s, 0.05f, z + 0.02f), 0.05f, 0.036f, fur);
                else
                {
                    // Haunches as heavy as Raticate's, folded over the hind feet
                    b.Ell(leg, V(0.11f * s, 0.17f, z), V(0.07f, 0.1f, 0.1f), fur);
                    b.Limb(leg, V(0.11f * s, 0.12f, z - 0.02f), V(0.115f * s, 0.05f, z + 0.01f), 0.04f, 0.034f, fur);
                }
                b.Ell(leg, V(0.115f * s, 0.024f, z + 0.05f), V(0.042f, 0.024f, 0.07f), cream);
                Claws(b, leg, V(0.115f * s, 0.02f, z + 0.11f), V(0.014f, 0, 0), V(0, -0.1f, 1f), 0.016f, 0.007f);
            });
        b.Ell(Body, V(0, 0.25f, -0.04f), V(0.16f, 0.14f, 0.28f), fur);
        b.PaintEll(Body, V(0, 0.16f, 0.0f), V(0.12f, 0.07f, 0.22f), cream);
        // Grizzled grey down the length of its back
        b.PaintEll(Body, V(0, 0.36f, -0.06f), V(0.11f, 0.06f, 0.25f), grey);
        Patches(b, Body, grey, (V(0.1f, 0.33f, 0.1f), 0.035f), (V(-0.11f, 0.32f, -0.04f), 0.03f), (V(0.12f, 0.31f, -0.18f), 0.03f), (V(-0.08f, 0.34f, -0.2f), 0.025f));
        // A thick scaly tail lying out behind, banded with three rings of steel
        int tail = b.Tail(V(0, 0.2f, -0.3f));
        var path = Smooth(3, V(0, 0.2f, -0.3f), V(0.05f, 0.1f, -0.44f), V(0.16f, 0.06f, -0.56f), V(0.3f, 0.09f, -0.6f), V(0.38f, 0.2f, -0.56f));
        b.Tube(tail, path, 0.044f, 0.024f, tailColor, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 2)
            b.PaintTorus(tail, path[i], 0.04f - 0.0018f * i, 0.005f, PixelCanvas.Shadow(tailColor, 0.2f), Euler(path[i + 1] - path[i - 1]));
        foreach (int i in new[] { 3, 6, 9 })
        {
            float t = i / (float)(path.Length - 1);
            b.Torus(tail, path[i], 0.044f - 0.02f * t, 0.011f, Steel, Euler(path[i + 1] - path[i - 1]), mat: Metal, blend: 0.004f);
        }
        b.Limb(Body, V(0, 0.3f, 0.16f), V(0, 0.32f, 0.24f), 0.09f, 0.08f, fur);
        int head = b.Head(V(0, 0.32f, 0.22f));
        var c = V(0, 0.36f, 0.3f);
        var r = V(0.12f, 0.105f, 0.12f);
        b.Ell(head, c, r, fur);
        b.PaintEll(head, V(0, 0.4f, 0.24f), V(0.08f, 0.05f, 0.1f), grey);
        b.Ell(head, V(0, 0.32f, 0.4f), V(0.075f, 0.06f, 0.075f), fur);
        b.PaintEll(head, V(0, 0.3f, 0.42f), V(0.07f, 0.045f, 0.06f), cream);
        b.Ell(head, V(0, 0.345f, 0.47f), V(0.022f, 0.018f, 0.016f), Rgb(60, 40, 40), blend: 0.006f);
        // The chisels: two broad teeth of steel, cut square at their ends
        PokeBuilder.Both(s => b.Box(head, V(0.021f * s, 0.25f, 0.448f), V(0.019f, 0.045f, 0.011f), 0.003f, Steel, V(-10f, 0, 0), Metal, 0.004f));
        Whiskers(b, head, V(0.05f, 0.32f, 0.44f), female ? 0.1f : 0.17f, Rgb(244, 232, 210), 0.007f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.44f, 0.26f));
            b.Ell(ear, V(0.11f * s, 0.48f, 0.25f), V(0.045f, 0.05f, 0.016f), fur, V(0, -10f * s, -30f * s));
            b.PaintEll(ear, V(0.11f * s, 0.48f, 0.26f), V(0.028f, 0.032f, 0.012f), cream, V(0, -10f * s, -30f * s));
        });
        foreach (var (x, h) in new[] { (-0.03f, 0.08f), (0.0f, 0.1f), (0.03f, 0.08f) })
            b.Spike(head, V(x, 0.44f, 0.25f), V(x * 1.4f, 0.44f + h, 0.18f), 0.02f, grey, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.39f), V(0.55f * s, 0.15f, 1f), 0.026f, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ 2038 Kitsunox, from Ninetales

    /// <summary>
    /// Kitsunox: Ninetales, its gold paled to cream, its nine tails longer and wispier, each ending in a pale violet
    /// flame, its ruff grown into a mantle and a small pale flame floating at its brow on a thin strand.
    /// </summary>
    private static PokeBuilder Kitsunox()
    {
        var b = Become(NinetalesBuild(false), "Kitsunox", 0.96f);
        var cream = Rgb(242, 230, 196);
        var tailCream = Rgb(234, 228, 236);
        var violet = Rgb(176, 136, 238);
        var pale = Rgb(240, 230, 255);
        Recolour(b, (Rgb(246, 228, 164), cream), (Rgb(248, 232, 170), tailCream), (Rgb(246, 156, 72), Rgb(222, 210, 246)),
            (Rgb(252, 246, 222), Rgb(255, 252, 246)), (Rgb(214, 50, 56), Rgb(150, 92, 220)));
        // Each tail runs on past its end, thinning to a wisp, and burns at its tip in pale violet
        int tail = BoneOf(b, "tail");
        for (int i = 0; i < 9; i++)
        {
            float a = (-72f + 18f * i) * Degree;
            var flat = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a) * 0.9f));
            var root = V(0, 0.41f, -0.22f);
            float len = 0.5f - 0.08f * MathF.Abs(MathF.Sin(a));
            var end = root + flat * len + V(0, 0.04f, 0);
            var wisp = end + flat * 0.16f + V(0, 0.07f, 0);
            b.Tube(tail, Smooth(3, end - flat * 0.04f, end + flat * 0.08f + V(0, 0.02f, 0), wisp), 0.034f, 0.016f, tailCream, blend: 0f);
            FlameTongue(b, tail, wisp - flat * 0.01f, wisp + flat * 0.05f + V(0, 0.09f, 0), 0.026f, violet, pale);
        }
        // The ruff grown into a mantle over the shoulders, falling in locks down the breast
        b.Ell(Body, V(0, 0.45f, 0.07f), V(0.13f, 0.075f, 0.12f), Rgb(255, 252, 246), blend: 0.03f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.09f * s, 0.4f, 0.1f), V(0.06f, 0.09f, 0.07f), Rgb(255, 252, 246), blend: 0.03f));
        foreach (float x in new[] { -0.06f, -0.02f, 0.02f, 0.06f })
            b.Spike(Body, V(x, 0.36f, 0.17f), V(x * 1.3f, 0.25f, 0.2f), 0.024f, Rgb(255, 252, 246), 0.5f);
        // A pale flame at the brow on a thin strand
        int head = BoneOf(b, "head");
        b.Tube(head, Smooth(3, V(0, 0.665f, 0.215f), V(0, 0.72f, 0.26f), V(0, 0.77f, 0.25f)), 0.007f, 0.005f, cream, blend: 0.004f);
        FlameTongue(b, head, V(0, 0.765f, 0.25f), V(0, 0.84f, 0.24f), 0.022f, violet, pale);
        return b;
    }

    // ------------------------------------------------------------------ 2083 Porrigard, from Farfetch'd

    /// <summary>
    /// Porrigard: Farfetch'd grown taller and leaner, a cape of white feathers over its shoulders, a few grey
    /// feathers in its brows, and its leek grown into a long staff tipped with a bundle of green leaves.
    /// </summary>
    private static PokeBuilder Porrigard()
    {
        var b = Become(FarfetchdBuild(false), "Porrigard", 0.86f);
        // The leek goes: the staff takes its place in the same wing
        int grip = BoneOf(b, "wingL");
        var leekWhite = Rgb(242, 244, 232);
        var leekGreen = Rgb(96, 170, 72);
        b.Sdf.RemoveAll(s => s.Bone == grip && (s.Color.Equals(leekWhite) || s.Color.Equals(leekGreen)));
        Stretch(b, V(0.94f, 1.14f, 0.95f));
        var white = Rgb(246, 244, 238);
        var green = Rgb(80, 156, 64);
        // The staff, from the ground beside its foot up past its head, a sage stalk greening toward its top, bound
        // with a cord under its bundle of leaves
        var foot = V(-0.12f, 0.016f, 0.13f);
        var top = V(-0.19f, 0.86f, 0.0f);
        var d = top - foot;
        var up = Vector3.Normalize(d);
        b.Limb(grip, foot, foot + d * 0.75f, 0.02f, 0.021f, Rgb(196, 206, 160), Leaf, 0.008f);
        b.Limb(grip, foot + d * 0.73f, top, 0.021f, 0.024f, Rgb(132, 182, 96), Leaf, 0.012f);
        b.Torus(grip, top - up * 0.03f, 0.024f, 0.008f, Rgb(150, 96, 60), Euler(d), blend: 0.004f);
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
        foreach (var (t, z) in new[] { (-1.4f, 0.03f), (-0.7f, -0.04f), (0f, 0.04f), (0.7f, -0.03f), (1.4f, 0.02f), (0.3f, -0.06f) })
            Frond(b, grip, top - up * 0.01f, top + side * t * 0.09f + up * 0.2f + V(0, 0, z), 0.05f, green, V(0, 0, 1f), 0.3f, Leaf);
        // A cape of white feathers over its shoulders and down its back, its edges showing in front
        b.Ell(Body, V(0, 0.46f, -0.04f), V(0.14f, 0.06f, 0.12f), white, V(14f, 0, 0), blend: 0.02f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.1f * s, 0.43f, 0.03f), V(0.055f, 0.06f, 0.05f), white, blend: 0.02f));
        for (int i = 0; i < 5; i++)
        {
            float x = -0.08f + 0.04f * i;
            Blade(b, Body, V(x, 0.46f, -0.12f), V(x * 1.35f, 0.22f - MathF.Abs(x) * 0.4f, -0.2f), 0.05f, white, V(0, 0.2f, -1f), 0.25f);
        }
        // Grey feathers grown into its brows
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.685f, 0.05f), V(0.1f * s, 0.73f, 0.02f), 0.012f, Rgb(176, 176, 182), 0.5f));
        return b;
    }

    // ------------------------------------------------------------------ 2099 Pincerex, from Kingler

    /// <summary>
    /// Pincerex: Kingler with its great claw grown greater still and tempered to steel rimmed in red, the small claw
    /// a little bigger, its shell a darker red with spikes along its edge, the crown of five spikes on its head kept.
    /// </summary>
    private static PokeBuilder Pincerex()
    {
        var red = Rgb(196, 64, 50);
        var b = CrabBuild("Pincerex", 0.92f, 0.085f, 0.18f, 5, red, Rgb(238, 226, 214));
        // The great claw, steel with a red rim at the wrist
        int arm = BoneOf(b, "armR");
        var elbow = V(0.22f, 0.24f, 0.08f);
        var d = Vector3.Normalize(V(0.15f, 1f, 0.35f));
        PaintAs(b, arm, elbow + d * 0.18f * 1.4f, V(0.2f, 0.26f, 0.2f), Steel, Metal, Euler(d));
        b.Torus(arm, elbow + d * 0.18f * 0.35f, 0.075f, 0.016f, red, Euler(d), mat: Shell, blend: 0.006f);
        // Spikes along the rim of its shell, behind and at the sides
        for (int i = 0; i < 9; i++)
        {
            float a = (200f + 140f * i / 8f) * Degree;
            var at = V(0.16f * MathF.Cos(a), 0.18f, 0.12f * MathF.Sin(a));
            var outward = Vector3.Normalize(V(MathF.Cos(a), 0.25f, MathF.Sin(a) * 0.9f));
            b.Spike(Body, at - outward * 0.01f, at + outward * 0.06f, 0.016f, red);
        }
        return b;
    }

    /// <summary>How far a forebear's build was moved after it was sculpted (a flier lifted off the ground): where the joint of <paramref name="bone"/> is, less where it was sculpted.</summary>
    private static Vector3 Shifted(PokeBuilder b, string bone, Vector3 sculptedAt) => b.Model.Skeleton[BoneOf(b, bone)].Joint - sculptedAt;

    /// <summary>The bone an Arbok-built coil's point <paramref name="i"/> lies on (a bone for every three points, <see cref="Coils"/>).</summary>
    private static int CoilBone(PokeBuilder b, int i, int per = 3) => i < per ? Body : BoneOf(b, "seg" + i / per);

    // ------------------------------------------------------------------ 2024 Najarok, from Arbok

    /// <summary>
    /// Najarok: Arbok longer and heavier, its purple deepened toward violet with thin gold bands down its body, its
    /// hood widened into a crowned frill, and the pattern on the hood woken into a second face whose eyes open and
    /// blink.
    /// </summary>
    private static PokeBuilder Najarok()
    {
        var b = Become(Arbok(), "Najarok", 0.98f);
        var violet = Rgb(116, 78, 168);
        var gold = Rgb(230, 190, 84);
        Recolour(b, (Rgb(150, 116, 184), violet));
        // Thin gold bands round the body, a pair of points apart, from the tail to the neck
        var coil = Spiral3(0.28f, 0.15f, 0.07f, 0.17f, 2.2f, 1.3f, 20);
        var points = new[] { V(0.34f, 0.05f, -0.36f), V(0.3f, 0.06f, -0.3f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.28f, 0.08f), V(0, 0.44f, 0.1f), V(0, 0.58f, 0.08f), V(0, 0.72f, 0.06f) }).ToArray();
        for (int i = 3; i < points.Length - 3; i += 2)
        {
            float t = i / (float)(points.Length - 1);
            float radius = 0.022f + 0.05f * MathF.Min(1f, t * 2.5f);
            b.PaintTorus(CoilBone(b, i), points[i], radius, 0.007f, gold, Euler(points[i + 1] - points[i - 1]));
        }
        // The hood widens into a frill crowned with points
        int hood = BoneOf(b, "hood");
        var hoodAt = V(0, 0.6f, 0.04f);
        Grow(b, V(1.25f, 1.15f, 1f), hoodAt, bone => bone == hood);
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + 25f * i) * Degree;
            var rim = V(0, 0.62f, 0.02f) + V(MathF.Sin(a) * 0.235f, MathF.Cos(a) * 0.2f, 0);
            var outward = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 1.1f, -0.05f));
            b.Spike(hood, rim - outward * 0.02f, rim + outward * (i % 2 == 0 ? 0.08f : 0.06f), 0.026f, violet, 0.45f);
            b.PaintEll(hood, rim + outward * 0.05f, V(0.02f, 0.03f, 0.03f), gold, Euler(outward));
        }
        // The face on the hood wakes: its two rings become eyes that open and blink
        var rings = b.Model.Decals.Where(d => d.Bone == hood && d.Shape == MarkShape.Disc).ToList();
        var eyes = rings.Where(d => d.Color.R > 230).Select(d => (d.Center, d.Normal)).ToList();
        b.Model.Decals.RemoveAll(d => rings.Contains(d));
        foreach (var (at, normal) in eyes)
            b.Eye(hood, at, normal, 0.044f, sclera: true, white: Rgb(244, 214, 82), pupil: Rgb(196, 40, 48));
        Stretch(b, V(1.08f, 1.06f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2047 Cordyhusk, from Parasect

    /// <summary>
    /// Cordyhusk: Parasect after the mushroom has taken its host whole: the cap swollen into a broad parasol hanging
    /// over the body like a cloak, its spots glowing pale, threads of white mycelium hanging from its rim, and the bug
    /// beneath a pale hollow husk with its eyes painted out.
    /// </summary>
    private static PokeBuilder Cordyhusk()
    {
        var b = Become(Parasect(), "Cordyhusk", 0.88f);
        var husk = Rgb(220, 204, 178);
        var red = Rgb(178, 46, 60);
        var pale = Rgb(236, 246, 214);
        var thread = Rgb(244, 240, 228);
        Recolour(b, (Rgb(242, 150, 84), husk), (Rgb(214, 122, 66), Rgb(194, 178, 152)), (Rgb(210, 104, 150), red));
        // The spots glow
        foreach (var s in b.Sdf.Shapes.Where(s => s.Op == SdfOp.Paint && s.Color.Equals(Rgb(250, 202, 96))))
        {
            s.Color = pale;
            s.Material = Glow;
            s.PaintsMaterial = true;
        }
        // The cap swells into a parasol that hangs over the body
        int mushroom = BoneOf(b, "mushroom");
        var top = V(0, 0.37f, -0.08f);
        Grow(b, V(1.3f, 1.12f, 1.3f), top, bone => bone == mushroom);
        var turn = V(-12f, 0, 0);
        var capR = V(0.27f, 0.17f, 0.29f) * V(1.3f, 1.12f, 1.3f);
        var q = Quaternion.CreateFromYawPitchRoll(0, turn.X * Degree, 0);
        // Its rim droops into a cloak
        b.Ell(mushroom, top + V(0, -0.06f, 0), V(capR.X * 1.02f, 0.08f, capR.Z * 1.02f), red, turn, blend: 0.03f);
        // Threads of mycelium hanging from the rim, all round but before its face
        for (int i = 0; i < 14; i++)
        {
            float a = (40f + i * (280f / 13f)) * Degree;
            var rim = top + Vector3.Transform(V(MathF.Sin(a) * capR.X * 0.97f, -0.11f, MathF.Cos(a) * capR.Z * 0.97f), q);
            float drop = 0.11f + 0.05f * ((i * 7) % 3);
            b.Tube(mushroom, new[] { rim + V(0, 0.02f, 0), rim - V(0, drop * 0.5f, 0) + V(MathF.Sin(i * 1.3f) * 0.01f, 0, 0), rim - V(0, drop, 0) }, 0.008f, 0.004f, thread, blend: 0.004f);
            b.Ell(mushroom, rim - V(0, drop, 0), V(0.008f, 0.01f, 0.008f), thread, blend: 0.003f);
        }
        // The husk's eyes painted out
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(208, 194, 168);
            d.Pupil = Rgb(186, 172, 150);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2078 Solarion, from Rapidash

    /// <summary>
    /// Solarion: Rapidash taller and more muscular, its horn longer and spiralled, two wings of flame spread from its
    /// shoulders, its mane and tail streaming further and flames at its hooves.
    /// </summary>
    private static PokeBuilder Solarion()
    {
        var b = Become(Horse(true), "Solarion", 0.98f);
        const float k = 1.12f;
        int head = BoneOf(b, "head");
        // A longer horn, a band of gold spiralling up it
        var ivory = Rgb(250, 246, 236);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(ivory));
        var hornFoot = V(0, 0.9f, 0.36f * k);
        var hornTip = V(0, 1.2f, 0.54f * k);
        b.Spike(head, hornFoot, hornTip, 0.04f, ivory, mat: Shell);
        var axis = Vector3.Normalize(hornTip - hornFoot);
        var u = Vector3.Normalize(Vector3.Cross(axis, V(1f, 0, 0)));
        var w = Vector3.Cross(axis, u);
        var helix = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = 0.034f * (1f - t) + 0.006f;
            float a = t * MathF.Tau * 2.5f;
            return hornFoot + axis * Vector3.Distance(hornFoot, hornTip) * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(head, helix, 0.008f, 0.004f, Rgb(244, 196, 96), Shell, 0.003f);
        // Two wings of flame from its shoulders
        var rim = Rgb(248, 128, 48);
        var heart = Rgb(255, 226, 120);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.08f * s, 0.6f, 0.06f);
            int wing = b.Wing(s, shoulder);
            foreach (var (tip, r) in new[] { (V(0.42f * s, 1.0f, 0.02f), 0.05f), (V(0.54f * s, 0.88f, -0.1f), 0.048f), (V(0.52f * s, 0.74f, -0.22f), 0.044f), (V(0.4f * s, 0.64f, -0.3f), 0.04f) })
                FlameTongue(b, wing, shoulder, tip, r, rim, heart);
        });
        // The mane and the tail streaming further, flames at the hooves
        int tail = BoneOf(b, "tail");
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(0, 0.3f, -1f), 0.5f * k, 0.06f * k);
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(-0.2f, 0.45f, -1f), 0.42f * k, 0.05f * k);
        int mane = BoneOf(b, "mane");
        Blaze(b, mane, V(0, 0.8f, 0.22f * k), V(0, 0.55f, -1f), 0.4f * k, 0.065f * k);
        foreach (var (z, front) in new[] { (0.17f, true), (-0.21f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                Blaze(b, leg, V(0.085f * s, 0.04f, (z + 0.0f) * k), V(0.3f * s, 0.25f, -1f), 0.11f, 0.026f);
            });
        // The wings widen the sculpt and coarsen its mesh, where the nostrils' small marks lose the triangles under them:
        // they are painted instead
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.028f * s, 0.83f, 0.48f * k), V(0.011f, 0.009f, 0.014f), Rgb(120, 100, 90), soft: 0.006f));
        Stretch(b, V(1.08f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2105 Ossugard, from Marowak

    /// <summary>
    /// Ossugard: Marowak taller, its skull helmet grown into a great horned skull, a pale blue flame burning in its
    /// eyes, its club grown into a long staff of bone with a skull at its head.
    /// </summary>
    private static PokeBuilder Ossugard()
    {
        var b = Become(SkullBuild(true, false), "Ossugard", 0.86f);
        var skull = Rgb(232, 230, 224);
        var blue = Rgb(120, 200, 255);
        var paleBlue = Rgb(232, 248, 255);
        int grip = BoneOf(b, "armL");
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == grip && s.Color.Equals(skull));
        // The horns grow long and heavy, and a second pair rises before them
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(skull)).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.9f + V(0, 0.03f, 0);
            s.Size = new Vector3(s.Size.X * 1.4f, s.Size.Y, s.Size.Z);
        }
        float h = 1.18f;
        var c = V(0, 0.43f * h, 0.04f);
        var r = V(0.12f, 0.1f, 0.13f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.07f, 0.04f), c + V(0.1f * s, 0.15f, 0.1f), 0.02f, skull, mat: Shell));
        // Blue fire in the eye holes, rising over the brow
        Recolour(b, (Black, blue));
        PokeBuilder.Both(s =>
        {
            var socket = On(c, r, 0.055f * s, c.Y - 0.005f);
            FlameTongue(b, head, socket + V(0.005f * s, 0.035f, -0.01f), socket + V(0.03f * s, 0.11f, -0.05f), 0.016f, blue, paleBlue);
        });
        // The staff of bone, a skull at its head
        var bottom = V(-0.11f, 0.024f, 0.07f);
        var top = V(-0.2f, 0.74f, -0.03f);
        b.Limb(grip, bottom, top, 0.022f, 0.022f, skull, Shell, 0.01f);
        foreach (float s in new[] { -1f, 1f })
            b.Ell(grip, bottom + V(0.02f * s, 0.006f, 0), V(0.028f, 0.026f, 0.028f), skull, mat: Shell, blend: 0.01f);
        var sk = top + V(-0.01f, 0.05f, 0.01f);
        b.Ell(grip, sk, V(0.055f, 0.05f, 0.058f), skull, mat: Shell, blend: 0.012f);
        b.Ell(grip, sk + V(0, -0.025f, 0.05f), V(0.034f, 0.026f, 0.04f), skull, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(grip, sk + V(0.022f * s, 0.005f, 0.05f), V(0.014f, 0.013f, 0.014f), Rgb(60, 52, 64));
            b.Spike(grip, sk + V(0.035f * s, 0.03f, -0.02f), sk + V(0.06f * s, 0.08f, -0.06f), 0.012f, skull, mat: Shell);
        });
        FlameTongue(b, grip, sk + V(0, 0.04f, 0), sk + V(0, 0.12f, -0.02f), 0.022f, blue, paleBlue);
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2131 Ferrisle, from Lapras

    /// <summary>
    /// Ferrisle: Lapras much larger, its knobbled shell grown into a wide deck with a low crest of ice along its
    /// middle and small pines of ice on it, its neck and its horn longer.
    /// </summary>
    private static PokeBuilder Ferrisle()
    {
        var b = Become(LaprasBuild(false), "Ferrisle", 1f);
        var shell = Rgb(170, 170, 178);
        var blue = Rgb(92, 170, 230);
        var cream = Rgb(244, 236, 200);
        var ice = Rgb(204, 236, 250);
        // The knobs go, and the shell flattens and widens into a deck
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(shell));
        var deckShape = b.Sdf.Shapes.First(s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(shell));
        var dc = V(0, 0.38f, -0.08f);
        var dr = V(0.3f, 0.12f, 0.35f);
        // The point on the deck's top over (x, z)
        Vector3 Top(float x, float z) => V(x, dc.Y + dr.Y * MathF.Sqrt(MathF.Max(0f, 1f - x * x / (dr.X * dr.X) - (z - dc.Z) * (z - dc.Z) / (dr.Z * dr.Z))), z);
        deckShape.Color = Rgb(190, 202, 214);
        deckShape.A = dc;
        deckShape.Size = dr;
        b.Sdf.Changed();
        b.PaintTorus(Body, dc + V(0, -0.02f, 0), 0.31f, 0.012f, Rgb(140, 140, 152), default, sz: 0.35f / 0.3f);
        // A low crest of ice down its middle
        for (int i = 0; i < 7; i++)
        {
            float z = 0.14f - i * 0.065f;
            var at = Top(0, z);
            b.Spike(Body, at - V(0, 0.015f, 0), at + V(0, 0.05f + 0.015f * (i % 2), -0.015f), 0.03f, Rgb(176, 222, 248), 0.5f, Shell);
        }
        // Small pines of ice standing on the deck
        foreach (var (x, z, size) in new[] { (0.16f, 0.06f, 1.4f), (-0.17f, -0.04f, 1.6f), (0.15f, -0.22f, 1.2f), (-0.12f, -0.28f, 1.1f), (0.21f, -0.1f, 1f) })
        {
            var foot = Top(x, z) - V(0, 0.012f, 0);
            for (int t = 0; t < 3; t++)
                b.Spike(Body, foot + V(0, 0.035f * t * size, 0), foot + V(0, (0.08f + 0.035f * t) * size, 0), (0.036f - 0.008f * t) * size, ice, mat: Shell, blend: 0.006f);
        }
        // The neck rises further, carrying the head, and the horn grows long
        int neck = BoneOf(b, "neck");
        int head = BoneOf(b, "head");
        var d = V(0, 0.11f, 0.03f);
        Move(b, bone => bone == head, d);
        b.Model.Skeleton.Shift(d, first: head);
        var spine = Smooth(3, V(0, 0.75f, 0.31f), V(0, 0.84f, 0.33f), V(0, 0.79f, 0.3f) + d);
        b.Tube(neck, spine, 0.056f, 0.055f, blue, blend: 0f);
        b.PaintEll(neck, V(0, 0.82f, 0.38f), V(0.04f, 0.07f, 0.035f), cream, Euler(V(0, 1f, 0.1f)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(blue) && s.B.Y > s.A.Y + 0.05f).ToList())
            s.B = s.A + (s.B - s.A) * 1.8f;
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2139 Ammonarch, from Omastar

    /// <summary>
    /// Ammonarch: Omastar with its spiral shell grown much larger and ridged, spikes along its outer whorl, and its
    /// tentacles longer and more of them.
    /// </summary>
    private static PokeBuilder Ammonarch()
    {
        var b = Become(Omastar(), "Ammonarch", 0.92f);
        var shell = Rgb(232, 220, 182);
        var blue = Rgb(98, 186, 220);
        var lift = Shifted(b, "head", V(0, 0.26f, 0.06f));
        var sc = V(0, 0.4f, -0.06f) + lift;
        // The shell grows and sits further back over its face
        Grow(b, V(1.35f, 1.35f, 1.35f), sc, bone => bone == Body);
        var back = V(0, 0.06f, -0.08f);
        Move(b, bone => bone == Body, back);
        sc += back;
        var sr = V(0.18f, 0.22f, 0.21f) * 1.35f;
        // A keel round its outer whorl, and spikes along it
        b.Torus(Body, sc, sr.Y * 1.0f, 0.02f, shell, V(0, 0, 90f), sz: sr.Z / sr.Y, mat: Shell, blend: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-20f + i * 32f) * Degree;
            var dir = V(0, MathF.Cos(a), -MathF.Sin(a));
            var at = Out(sc, sr, default, dir);
            var n = Outward(sc, sr, at);
            b.Spike(Body, at - n * 0.01f, at + n * 0.1f, 0.032f, PixelCanvas.Mix(shell, Rgb(170, 140, 100), 0.4f), mat: Shell);
        }
        // More tentacles, longer, between the old ones
        int head = BoneOf(b, "head");
        var c = V(0, 0.25f, 0.12f) + lift;
        var r = V(0.17f, 0.12f, 0.1f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-100f + (i + 0.5f) * (200f / 7f)) * Degree;
            Tentacle(b, head, c + V(MathF.Sin(a) * r.X * 0.8f, -r.Y * 0.5f, MathF.Cos(a) * r.Z * 0.7f), V(MathF.Sin(a) * 1.3f, 0, MathF.Cos(a)), 0.34f, 0.03f, blue);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2141 Eurypterox, from Kabutops

    /// <summary>
    /// Eurypterox: Kabutops longer and lower, with a jointed tail ending in a flat paddle, its two scythes bigger and a
    /// second, smaller pair of claws below them.
    /// </summary>
    private static PokeBuilder Eurypterox()
    {
        var b = Become(Kabutops(), "Eurypterox", 0.98f);
        var brown = Rgb(170, 116, 70);
        var cream = Rgb(232, 214, 170);
        var blade = Rgb(228, 228, 232);
        // Bigger scythes
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.35f, 1.35f, 1.35f), V(0.25f * s, 0.67f, 0.08f), sh => sh.Bone == arm && sh.Color.Equals(blade), _ => false);
        });
        // A second, smaller pair of claws below them
        PokeBuilder.Both(s =>
        {
            int claw = b.Part(s < 0 ? "clawL" : "clawR", Body, V(0.07f * s, 0.48f, 0.04f), PokeRole.Arm, s, s);
            var elbow = V(0.16f * s, 0.42f, 0.1f);
            var wrist = V(0.19f * s, 0.48f, 0.16f);
            b.Limb(claw, V(0.06f * s, 0.48f, 0.04f), elbow, 0.022f, 0.02f, brown);
            b.Ell(claw, elbow, V(0.022f, 0.022f, 0.022f), brown);
            b.Limb(claw, elbow, wrist, 0.02f, 0.022f, brown);
            Blade(b, claw, wrist, wrist + V(0.05f * s, -0.15f, 0.03f), 0.032f, blade, V(0, 0, 1f), 0.34f, Shell);
        });
        // A jointed tail curving down behind it to a flat paddle on the ground
        int tail = b.Tail(V(0, 0.42f, -0.06f));
        var path = Smooth(3, V(0, 0.42f, -0.06f), V(0, 0.3f, -0.2f), V(0, 0.14f, -0.32f), V(0, 0.05f, -0.46f));
        for (int i = 0; i < path.Length - 1; i++)
        {
            float t = i / (float)(path.Length - 1);
            var mid = (path[i] + path[i + 1]) / 2f;
            float len = Vector3.Distance(path[i], path[i + 1]);
            b.Ell(tail, mid, V(0.06f - 0.03f * t, len * 0.62f, 0.05f - 0.025f * t), i % 2 == 0 ? brown : PixelCanvas.Shadow(brown, 0.12f), Euler(path[i + 1] - path[i]), blend: 0.006f);
        }
        var end = path[^1];
        b.Ell(tail, end + V(0, -0.02f, -0.06f), V(0.1f, 0.016f, 0.08f), brown, blend: 0.01f);
        b.PaintEll(tail, end + V(0, -0.01f, -0.06f), V(0.07f, 0.02f, 0.06f), cream);
        Stretch(b, V(1f, 0.92f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2142 Azhdarch, from Aerodactyl

    /// <summary>
    /// Azhdarch: Aerodactyl grown into a giant of the skies: its head carried high on a long stiff neck under a great
    /// crest, its wings broader and a deeper purple, and a ridge of amber stones down its back and tail.
    /// </summary>
    private static PokeBuilder Azhdarch()
    {
        var b = Become(AerodactylBuild(false), "Azhdarch", 1f);
        var grey = Rgb(182, 174, 206);
        var amber = Rgb(236, 160, 56);
        var lift = Shifted(b, "head", V(0, 0.6f, 0.06f));
        Recolour(b, (Rgb(156, 126, 196), Rgb(124, 88, 178)));
        // Broader wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.3f, 1.12f, 1f), V(0.08f * s, 0.56f, 0) + lift, bone => bone == wing);
        });
        // The head carried up and forward on a long stiff neck, which turns from its foot
        int head = BoneOf(b, "head");
        int jaw = BoneOf(b, "jaw");
        var d = V(0, 0.2f, 0.11f);
        Move(b, bone => bone == head || bone == jaw, d);
        b.Model.Skeleton.Shift(d, first: jaw);
        b.Limb(head, V(0, 0.56f, 0.02f) + lift, V(0, 0.6f, 0.04f) + d + lift, 0.056f, 0.05f, grey);
        // A great crest swept back from its skull
        var c = V(0, 0.68f, 0.12f) + d + lift;
        Blade(b, head, c + V(0, 0.04f, -0.01f), c + V(0, 0.22f, -0.3f), 0.12f, Rgb(160, 150, 190), V(1f, 0, 0), 0.22f);
        b.PaintEll(head, c + V(0, 0.15f, -0.19f), V(0.02f, 0.06f, 0.08f), Rgb(196, 110, 120), Euler(V(0, 0.6f, -0.8f)));
        // A ridge of amber stones down its back and along its tail
        var bc = V(0, 0.48f, 0) + lift;
        var br = V(0.1f, 0.13f, 0.11f);
        foreach (var dir in new[] { V(0, 0.75f, -0.6f), V(0, 0.4f, -0.9f), V(0, 0.05f, -1f) })
        {
            var at = Out(bc, br, V(-20f, 0, 0), dir);
            b.Ell(Body, at, V(0.022f, 0.018f, 0.026f), amber, mat: Shell, blend: 0.006f);
        }
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0, 0.42f, -0.07f), V(0, 0.36f, -0.25f), V(0.02f, 0.33f, -0.44f));
        for (int i = 2; i < path.Length - 1; i += 2)
        {
            float t = i / (float)(path.Length - 1);
            float rad = 0.04f + (0.014f - 0.04f) * t;
            b.Ell(tail, path[i] + lift + V(0, rad * 0.9f, 0), V(0.016f, 0.013f, 0.02f), amber, mat: Shell, blend: 0.005f);
        }
        return Lift(b);
    }

    /// <summary>A thin band spiralling up from <paramref name="foot"/> to <paramref name="tip"/> round a horn <paramref name="r"/> wide at its foot (a narwhal's tusk, a unicorn's horn).</summary>
    private static void Helix(PokeBuilder b, int bone, Vector3 foot, Vector3 tip, float r, float turns, float thick, Color color)
    {
        var axis = Vector3.Normalize(tip - foot);
        var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.X) < 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(axis, u);
        float length = Vector3.Distance(foot, tip);
        var points = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = r * (1f - t) * 0.95f + thick * 0.5f;
            float a = t * MathF.Tau * turns;
            return foot + axis * length * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(bone, points, thick, thick * 0.5f, color, Shell, 0.003f);
    }

    // ------------------------------------------------------------------ 2022 Lancerow, from Fearow

    /// <summary>
    /// Lancerow: Fearow with its long beak grown into a lance half again as long as its head, a ring of hard shell
    /// where it meets the face, its red crest swept back into three long plumes, and broader wings barred pale.
    /// </summary>
    private static PokeBuilder Lancerow()
    {
        var b = Become(Fearow(), "Lancerow", 0.95f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.15f));
        int head = BoneOf(b, "head");
        var red = Rgb(224, 74, 64);
        // The beak grows into a lance, ringed where it leaves the face
        var beak = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(232, 170, 164)));
        var dir = Vector3.Normalize(beak.B - beak.A);
        beak.B = beak.A + (beak.B - beak.A) * 1.45f;
        beak.Color = Rgb(236, 206, 176);
        b.Torus(head, beak.A + dir * 0.025f, 0.034f, 0.012f, Rgb(176, 120, 96), Euler(dir), mat: Shell, blend: 0.004f);
        // The crest swept back into three long plumes
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(red)).ToList())
        {
            s.B = s.A + (s.B - s.A) * 2.1f + V(0, -0.04f, -0.08f);
            s.Size = new Vector3(s.Size.X * 1.2f, s.Size.Y, s.Size.Z);
        }
        // Broader wings, barred pale across their flight feathers
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.13f * s, 0.46f, 0.07f) + lift;
            Grow(b, V(1.35f, 1.1f, 1.15f), shoulder, bone => bone == wing);
            var tip = shoulder + (V(0.18f * s, 0.32f, -0.32f) + lift - shoulder) * V(1.35f, 1.1f, 1.15f);
            var d = tip - shoulder;
            foreach (float t in new[] { 0.45f, 0.62f })
                b.PaintEll(wing, shoulder + d * t, V(0.07f, 0.012f, 0.13f), Rgb(236, 214, 170), Euler(d));
        });
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2028 Sandspire, from Sandslash

    /// <summary>
    /// Sandspire: Sandslash with its quills grown into fewer, taller spines of banded sandstone, tallest over its
    /// shoulders, its claws thickened into picks and a plate of rock over its brow.
    /// </summary>
    private static PokeBuilder Sandspire()
    {
        var b = Become(Sandslash(), "Sandspire", 0.9f);
        var quill = Rgb(146, 96, 58);
        var tipColor = Rgb(200, 156, 104);
        var stone = Rgb(204, 160, 104);
        var band = Rgb(150, 104, 64);
        b.Sdf.RemoveAll(s => s.Bone == Body && (s.Color.Equals(quill) || s.Color.Equals(tipColor)));
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.16f, 0.2f, 0.15f);
        for (int row = 0; row < 3; row++)
        {
            int cols = row == 0 ? 3 : 4;
            for (int col = 0; col < cols; col++)
            {
                float elev = (66f - row * 24f) * Degree;
                float phi = ((col - (cols - 1) / 2f) * 34f) * Degree;
                var dir = Vector3.Normalize(V(MathF.Sin(phi) * MathF.Cos(elev), MathF.Sin(elev), -MathF.Cos(phi) * MathF.Cos(elev)));
                var root = c + dir * r * 0.8f;
                float len = (row == 0 ? 0.34f : 0.28f - row * 0.03f) * (1f - MathF.Abs(phi) * 0.25f);
                var to = root + Vector3.Normalize(dir + V(0, 0.35f, -0.25f)) * len;
                b.Spike(Body, root, to, 0.065f, stone, 0.6f, Shell, 0.012f);
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.45f), V(0.06f, 0.025f, 0.06f), band, Euler(to - root));
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.75f), V(0.05f, 0.018f, 0.05f), band, Euler(to - root));
            }
        }
        // Claws thickened into picks
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.35f, 1.35f, 1.35f), V(0.2f * s, 0.33f, 0.18f), sh => sh.Bone == arm && sh.Color.Equals(White), _ => false);
        });
        Recolour(b, (White, Rgb(214, 208, 196)));
        // A plate of rock over its brow
        int head = BoneOf(b, "head");
        b.Ell(head, V(0, 0.62f, 0.13f), V(0.085f, 0.03f, 0.07f), Rgb(150, 132, 112), V(-18f, 0, 0), Shell, 0.012f);
        return b;
    }

    // ------------------------------------------------------------------ 2049 Somnimoth, from Venomoth

    /// <summary>
    /// Somnimoth: Venomoth with its four wings grown wider and paled to lilac, the lower pair trailing long tails like
    /// a luna moth's, an eyespot on each upper wing, its fuzzy body thicker and its feelers branched like ferns.
    /// </summary>
    private static PokeBuilder Somnimoth()
    {
        var b = Become(Venomoth(), "Somnimoth", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.5f, 0.02f));
        var lilac = Rgb(234, 222, 250);
        var vein = Rgb(182, 160, 222);
        Recolour(b, (Rgb(214, 196, 240), lilac), (Rgb(160, 130, 206), vein));
        Grow(b, V(1.25f, 1.25f, 1.25f), V(0, 0.42f, 0) + lift, bone => bone == Body);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.04f * s, 0.46f, -0.03f) + lift;
            var k = V(1.3f, 1.18f, 1f);
            Grow(b, k, shoulder, bone => bone == wing);
            // A long tail trailing from each lower wing
            var lower = shoulder + (V(0.17f * s, 0.34f, -0.04f) + lift - shoulder) * k;
            var foot = lower + V(0.05f * s, -0.07f, 0);
            Blade(b, wing, foot, foot + V(0.07f * s, -0.24f, -0.03f), 0.05f, lilac, V(0, 0, 1f), 0.25f);
            b.PaintEll(wing, foot + V(0.065f * s, -0.22f, -0.03f), V(0.03f, 0.04f, 0.02f), vein);
            // An eyespot on each upper wing
            var upper = shoulder + (V(0.26f * s, 0.6f, -0.02f) + lift - shoulder) * k;
            b.Mark(wing, upper + V(0.03f * s, 0.01f, 0), V(0, 0, 1f), 0.032f, 0.032f, Rgb(98, 70, 150), MarkShape.Disc);
            b.Mark(wing, upper + V(0.03f * s, 0.01f, 0), V(0, 0, 1f), 0.016f, 0.016f, Rgb(244, 220, 120), MarkShape.Disc);
        });
        // Feelers branched like ferns
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var from = V(0.02f * s, 0.59f, 0.02f) + lift;
            var to = V(0.06f * s, 0.72f, 0.0f) + lift;
            foreach (float t in new[] { 0.3f, 0.5f, 0.7f, 0.88f })
            {
                var at = Vector3.Lerp(from, to, t);
                foreach (float z in new[] { -1f, 1f })
                    b.Spike(ear, at, at + V(0.03f * s, 0.018f, 0.03f * z) * (1.2f - t * 0.6f), 0.007f, Rgb(144, 112, 196), blend: 0.003f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2051 Molemount, from Dugtrio

    /// <summary>
    /// Molemount: five of Dugtrio's heads in a ring, the middle one tallest, standing out of the wide mound of earth
    /// the three never showed, boulders set in its slopes.
    /// </summary>
    private static PokeBuilder Molemount()
    {
        var b = new PokeBuilder("Molemount", 0.84f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Scales };
        var brown = Rgb(172, 124, 94);
        var nose = Rgb(228, 150, 196);
        var earth = Rgb(132, 96, 64);
        Burrow(b, 0.44f, 0.38f, 20, Rgb(162, 162, 166), 2051u);
        // The mound, dark earth with paler dirt on its slopes, boulders set in it
        b.Ell(Root, V(0, 0.0f, 0), V(0.46f, 0.22f, 0.4f), earth, blend: 0.03f);
        foreach (var (x, z, sz) in new[] { (0.2f, 0.16f, 0.06f), (-0.24f, 0.05f, 0.07f), (0.06f, -0.24f, 0.08f), (-0.12f, 0.22f, 0.05f), (0.27f, -0.12f, 0.06f) })
            b.PaintEll(Root, V(x * 1.2f, 0.17f, z * 1.2f), V(sz * 1.8f, 0.06f, sz * 1.8f), Rgb(170, 132, 92));
        foreach (var (x, z, k) in new[] { (0.33f, 0.1f, 0.06f), (-0.3f, -0.14f, 0.07f), (-0.12f, 0.33f, 0.05f), (0.2f, -0.3f, 0.055f), (-0.36f, 0.12f, 0.05f) })
        {
            float y = 0.22f * MathF.Sqrt(MathF.Max(0f, 1f - x * x / 0.2116f - z * z / 0.16f));
            b.Ell(Root, V(x, y - k * 0.3f, z), V(k * 1.2f, k * 0.8f, k), Rgb(150, 146, 150), V(0, x * 200f, 10f), Shell, 0.01f);
        }
        // Five heads: the tallest in the middle, four round it, each bobbing on its own
        DiglettDome(b, Body, V(0, 0.17f, -0.03f), 1.1f, brown, nose);
        foreach (var (at, k, i) in new[] { (V(-0.25f, 0.14f, 0.08f), 0.76f, 0), (V(0.25f, 0.14f, 0.1f), 0.76f, 1), (V(-0.19f, 0.13f, -0.2f), 0.72f, 2), (V(0.19f, 0.13f, -0.19f), 0.7f, 3) })
        {
            int part = b.Part("head" + i, Root, at + V(0, 0.05f, 0), PokeRole.Head, 0.7f + 1.3f * i, at.X < 0 ? -1f : 1f);
            DiglettDome(b, part, at, k, brown, nose);
        }
        b.CutBox(Root, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        return b;
    }

    // ------------------------------------------------------------------ 2053 Regalynx, from Persian

    /// <summary>
    /// Regalynx: Persian longer and leaner, its cream coat brushed with gold at the paws and the tail, a mane of soft
    /// fur at its chest, its brow gem grown into a crown of three gems in a gold setting and its curled tail ending in
    /// a tuft.
    /// </summary>
    private static PokeBuilder Regalynx()
    {
        var b = Become(Persian(), "Regalynx", 0.88f);
        var fur = Rgb(246, 232, 194);
        var gold = Rgb(234, 188, 90);
        var gem = Rgb(222, 48, 62);
        int head = BoneOf(b, "head");
        // The crown of three gems
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(gem));
        var brow = V(0, 0.515f, 0.279f);
        b.Ell(head, brow + V(0, 0.004f, -0.004f), V(0.056f, 0.014f, 0.02f), gold, V(-40f, 0, 0), Metal, 0.004f);
        b.Ell(head, brow + V(0, 0.026f, 0.0f), V(0.02f, 0.026f, 0.013f), gem, V(-40f, 0, 0), Metal, 0.004f);
        PokeBuilder.Both(s => b.Ell(head, brow + V(0.042f * s, 0.008f, -0.01f), V(0.013f, 0.017f, 0.01f), gem, V(-40f, 0, 0), Metal, 0.004f));
        // Gold at the paws
        foreach (var (z, front) in new[] { (0.14f, true), (-0.15f, false) })
            PokeBuilder.Both(s => b.PaintEll(BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R")), V(0.066f * s, 0.03f, z + (front ? 0.04f : 0.01f)), V(0.04f, 0.04f, 0.055f), gold));
        // A mane of soft fur at its chest
        var mc = V(0, 0.33f, 0.16f);
        var mr = V(0.07f, 0.075f, 0.055f);
        b.Ell(Body, mc, mr, Rgb(252, 244, 222), blend: 0.03f);
        FurTufts(b, Body, mc, mr, 18, 0.04f, 0.02f, Rgb(252, 244, 222), 0.2f, -0.9f, 0.9f);
        // The tail's curl ends in a gold tuft
        int tail = BoneOf(b, "tail");
        var dir = Vector3.Normalize(V(0.2f, 0.7f, -1f));
        var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
        if (up.Y < 0f) up = -up;
        var end = V(0, 0.33f, -0.21f) + dir * 0.3f + up * 0.3f * 0.15f;
        var tuft = end + up * 0.06f * 2f;
        b.Ell(tail, tuft, V(0.035f, 0.035f, 0.035f), gold, blend: 0.012f);
        FurTufts(b, tail, tuft, V(0.035f, 0.035f, 0.035f), 14, 0.035f, 0.016f, gold, 2f, -0.9f, 0.2f);
        Stretch(b, V(0.95f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2055 Psyduke, from Golduck

    /// <summary>
    /// Psyduke: Golduck taller and leaner, its blue deepened, its bill longer, the gem on its brow grown into a third
    /// eye that glows, webbed fins spread from its forearms and a pale ring of light floating round its head.
    /// </summary>
    private static PokeBuilder Psyduke()
    {
        var b = Become(Golduck(), "Psyduke", 0.94f);
        var oldBlue = Rgb(92, 152, 222);
        var blue = Rgb(58, 106, 198);
        var bill = Rgb(246, 222, 140);
        Recolour(b, (oldBlue, blue), (PixelCanvas.Light1(oldBlue, 0.18f), PixelCanvas.Light1(blue, 0.18f)));
        int head = BoneOf(b, "head");
        // A longer bill
        Grow(b, V(1f, 1f, 1.35f), V(0, 0.8f, 0.06f), s => s.Bone == head && s.Color.Equals(bill), _ => false);
        // The third eye, wide and glowing
        var gem = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(220, 56, 60)) && s.Material == Glow);
        gem.Size = V(0.04f, 0.03f, 0.024f);
        gem.Color = Rgb(250, 110, 130);
        b.PaintEll(head, gem.A + V(0, 0, 0.018f), V(0.016f, 0.016f, 0.012f), Rgb(255, 222, 226));
        // Webbed fins from its forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Blade(b, arm, V(0.27f * s, 0.57f, 0.12f), V(0.39f * s, 0.55f, -0.04f), 0.06f, PixelCanvas.Light1(blue, 0.25f), V(s, 0.3f, 0), 0.22f);
        });
        // A ring of light floating round its head on thin strands
        var ring = V(0, 1.0f, -0.02f);
        b.Torus(head, ring, 0.17f, 0.011f, Rgb(250, 222, 240), mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.09f * s, 0.93f, -0.01f), V(0.17f * s, 1.0f, -0.02f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f));
        b.Tube(head, new[] { V(0, 0.93f, -0.1f), V(0, 1.0f, -0.19f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f);
        Stretch(b, V(0.93f, 1.08f, 0.93f));
        return b;
    }

    // ------------------------------------------------------------------ 2059 Kirinine, from Arcanine

    /// <summary>
    /// Kirinine: Arcanine larger, its mane flowing longer like a lion dance's, two short horns swept back from its
    /// brow and a long beard of cream fur.
    /// </summary>
    private static PokeBuilder Kirinine()
    {
        var b = Become(Arcanine(), "Kirinine", 0.98f);
        const float lift = 0.09f;
        var mane = Rgb(248, 228, 184);
        var c = V(0, 0.64f + lift, 0.25f);
        // A longer mane
        Grow(b, V(1.15f, 1.3f, 1.2f), V(0, 0.5f + lift, 0.16f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        int head = BoneOf(b, "head");
        // Two short horns swept back from its brow
        PokeBuilder.Both(s => b.Spike(head, c + V(0.045f * s, 0.07f, 0.02f), c + V(0.09f * s, 0.19f, -0.12f), 0.026f, Rgb(232, 208, 150), mat: Shell));
        // A long beard of cream fur
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Spike(head, V(x, c.Y - 0.075f, 0.33f), V(x * 1.4f, c.Y - 0.2f, 0.34f), 0.026f, mane, 0.6f);
        // Its stripes curled into scrolls on its flanks
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.06f, -0.12f })
                b.Mark(Body, Out(V(0, 0.38f + lift, -0.02f), V(0.13f, 0.12f, 0.27f), default, V(s, 0.15f, z * 3f)), V(s, 0.15f, z * 3f), 0.03f, 0.03f, Rgb(40, 38, 48), MarkShape.Ring);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2073 Medusarch, from Tentacruel

    /// <summary>
    /// Medusarch: Tentacruel with its bell raised into a tall sail-like float, blue paled to violet, its three red gems
    /// kept, and its tentacles doubled in number and length, trailing in a cluster beneath.
    /// </summary>
    private static PokeBuilder Medusarch()
    {
        var b = Become(Tentacruel(), "Medusarch", 0.98f);
        var lift = Shifted(b, "body", V(0, 0.66f, 0));
        var violet = Rgb(144, 150, 232);
        var sail = Rgb(204, 176, 240);
        Recolour(b, (Rgb(72, 172, 232), violet));
        var c = V(0, 0.68f, -0.02f) + lift;
        // The sail rising from the float, its edge pink
        b.Ell(Body, c + V(0, 0.2f, -0.05f), V(0.036f, 0.22f, 0.25f), sail, V(-15f, 0, 0), Shell, 0.03f);
        b.PaintEll(Body, c + V(0, 0.38f, -0.1f), V(0.045f, 0.06f, 0.22f), Rgb(240, 150, 200), V(-15f, 0, 0));
        // As many tentacles again, longer, between the old ones
        var grey = Rgb(132, 120, 110);
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f + 0.31f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.55f, -0.02f) + out1 * 0.1f + lift;
            int arm = b.Part("tentacleB" + i, Body, root, PokeRole.Leaf, i * 0.6f + 0.3f);
            float sag = 0.05f * (i % 3);
            b.Tube(arm, Smooth(3, root, root + out1 * 0.05f + V(0, -0.18f, 0), root + out1 * 0.12f + V(0, -0.3f - sag, 0), root + out1 * 0.2f + V(0, -0.4f - sag, 0)), 0.018f, 0.01f, grey);
        }
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone > Body && s.Color.Equals(grey) && s.Shape == SdfShape.RoundCone).ToList())
            if (b.Model.Skeleton[s.Bone].Name.StartsWith("tentacle") && !b.Model.Skeleton[s.Bone].Name.StartsWith("tentacleB"))
            {
                var root = b.Model.Skeleton[s.Bone].Joint;
                s.A = root + (s.A - root) * V(1f, 1.15f, 1f);
                s.B = root + (s.B - root) * V(1f, 1.15f, 1f);
            }
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2080 Slowmantle, from Slowbro

    /// <summary>
    /// Slowmantle: Slowbro sat back in the Shellder of its tail grown into a great spiral shell over its back like a
    /// hermit crab's house, grey lined in pink, a pearl gleaming in the shell's mouth.
    /// </summary>
    private static PokeBuilder Slowmantle()
    {
        var b = Become(Slowbro(), "Slowmantle", 0.94f);
        var pink = Rgb(240, 160, 184);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && !s.Color.Equals(pink));
        b.Model.Decals.RemoveAll(d => d.Bone == tail);
        var grey = Rgb(186, 188, 202);
        var line = Rgb(228, 140, 172);
        var sc = V(0, 0.46f, -0.17f);
        var sr = V(0.2f, 0.27f, 0.22f);
        Ammonite(b, Body, sc, sr, grey, line, 20f, -20f, -60f, -100f, -140f);
        // Its spire rising behind, and the pearl in the shell's mouth over its tail
        b.Spike(Body, sc + V(0, 0.16f, -0.1f), sc + V(0, 0.34f, -0.26f), 0.1f, grey, mat: Shell, blend: 0.02f);
        foreach (float t in new[] { 0.35f, 0.6f })
            b.PaintTorus(Body, Vector3.Lerp(sc + V(0, 0.16f, -0.1f), sc + V(0, 0.34f, -0.26f), t), 0.1f * (1f - t) + 0.01f, 0.006f, line, Euler(V(0, 0.18f, -0.16f)));
        var mouth = Out(sc, sr, default, V(0, -0.7f, -0.7f));
        b.Ell(Body, mouth + V(0, 0.02f, 0.02f), V(0.05f, 0.05f, 0.05f), Rgb(250, 246, 250), mat: Shell, blend: 0.01f);
        return b;
    }

    // ------------------------------------------------------------------ 2085 Tripluma, from Dodrio

    /// <summary>
    /// Tripluma: Dodrio with a crest of plumes on each head in its own colour (red, gold and white), a fan of tail
    /// feathers raised like a peacock's, and thicker legs; the female's beige necks kept.
    /// </summary>
    /// <param name="female">A female keeps Dodrio's: beige necks.</param>
    private static PokeBuilder Tripluma(bool female = false)
    {
        var b = Become(Dodrio(female), "Tripluma", 0.97f);
        var plume = Rgb(232, 132, 128);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // A fan of tail feathers raised high, each tipped in gold
        TailFan(b, tail, V(0, 0.47f, -0.12f), 9, 150f, 0.36f, 0.05f, plume, Rgb(240, 196, 80), -1.1f);
        // A crest of plumes on each head, each in its own colour
        foreach (var (name, at, color) in new[] { ("headL", V(-0.17f, 0.79f, 0.04f), Rgb(226, 62, 58)), ("headM", V(0.0f, 0.9f, -0.02f), Rgb(240, 196, 70)), ("headR", V(0.16f, 0.82f, 0.05f), Rgb(246, 244, 240)) })
        {
            int head = BoneOf(b, name);
            foreach (float x in new[] { -1f, 0f, 1f })
                b.Spike(head, at + V(0.02f * x, 0.06f, -0.01f), at + V(0.05f * x, 0.2f - MathF.Abs(x) * 0.03f, -0.1f), 0.016f, color, 0.5f);
        }
        // Thicker legs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Grow(b, V(1.45f, 1f, 1.45f), V(0.055f * s, 0.2f, 0.01f), sh => sh.Bone == leg && sh.Shape == SdfShape.RoundCone && sh.Size.X > 0.012f, _ => false);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2087 Glacitusk, from Dewgong

    /// <summary>
    /// Glacitusk: Dewgong bigger and heavier, its flippers lightly blue, its horn grown into a long spiralled tusk of
    /// ivory, two short tusks below its mouth and a mantle of thick fur at its neck.
    /// </summary>
    private static PokeBuilder Glacitusk()
    {
        var b = Become(Dewgong(), "Glacitusk", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.44f, 0.1f));
        var white = Rgb(238, 242, 250);
        var ivory = Rgb(244, 234, 206);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(226, 234, 246), Rgb(196, 222, 248)));
        // The horn grown into a long spiral tusk
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(white));
        var foot = V(0, 0.6f, 0.17f) + lift;
        var tip = V(0, 0.8f, 0.42f) + lift;
        b.Spike(head, foot, tip, 0.032f, ivory, mat: Shell);
        Helix(b, head, foot, tip, 0.032f, 3f, 0.007f, Rgb(214, 198, 160));
        // Its small tusks grown longer
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(White)).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.9f;
            s.Size = new Vector3(s.Size.X * 1.3f, s.Size.Y, s.Size.Z);
            s.Color = ivory;
        }
        // A mantle of thick fur round its neck
        var mc = V(0, 0.43f, 0.11f) + lift;
        var mr = V(0.15f, 0.08f, 0.13f);
        b.Ell(Body, mc, mr, white, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 28, 0.045f, 0.024f, white, 0.5f, -0.7f, 0.7f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2089 Sludgemire, from Muk

    /// <summary>
    /// Sludgemire: Muk thickened into tar, black-purple with a gloss and bubbles on its surface, bones and rocks half
    /// sunk in it, its arms thicker and dripping.
    /// </summary>
    private static PokeBuilder Sludgemire()
    {
        var b = Become(Muk(), "Sludgemire", 0.92f);
        const float k = 1.35f;
        var tar = Rgb(70, 46, 86);
        Recolour(b, (Rgb(162, 122, 182), tar), (Rgb(116, 84, 136), Rgb(46, 30, 58)));
        var hc = V(0, 0.26f, 0.02f);
        var hr = V(0.26f, 0.21f, 0.21f);
        // Bubbles rising on its surface
        foreach (var (d, size) in new[] { (V(0.6f, 0.6f, -0.3f), 0.03f), (V(-0.5f, 0.8f, -0.2f), 0.024f), (V(0.2f, 0.9f, -0.5f), 0.02f), (V(-0.8f, 0.2f, -0.4f), 0.032f), (V(0.8f, 0.1f, -0.5f), 0.022f) })
        {
            var at = Out(hc, hr, V(-8f, 0, 0), d);
            b.Ell(Body, at, V(size, size, size), Rgb(120, 92, 140), mat: Scales, blend: 0.006f);
        }
        // A bone and rocks half sunk in the puddle round it
        Club(b, Body, V(0.2f, 0.13f, 0.17f), V(0.33f, 0.1f, 0.03f), 0.018f, Rgb(232, 226, 210));
        foreach (var (at, size) in new[] { (V(-0.28f, 0.1f, 0.1f), 0.05f), (V(-0.15f, 0.12f, -0.22f), 0.045f), (V(0.28f, 0.1f, -0.12f), 0.04f) })
            b.Ell(Body, at, V(size * 1.2f, size * 0.8f, size), Rgb(120, 116, 122), V(0, at.X * 300f, 12f), Shell, 0.008f);
        // Thicker arms, dripping
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.34f * s, 0.2f, 0.2f) * k;
            foreach (float t in new[] { 0.45f, 0.8f })
            {
                var at = Vector3.Lerp(V(0.18f * s, 0.24f, 0.12f) * k, hand, t) - V(0, 0.045f * k, 0);
                b.Spike(arm, at + V(0, 0.02f, 0), at - V(0, 0.07f, 0), 0.02f, tar, blend: 0.012f);
            }
            Grow(b, V(1.22f, 1.22f, 1.22f), V(0.12f * s, 0.24f, 0.05f) * k, bone => bone == arm);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2091 Clamspire, from Cloyster

    /// <summary>
    /// Clamspire: Cloyster's spiked shell grown into a spire of shells grown together like a reef, smaller shells on
    /// its slopes, pale violet with white spikes; its pearl face kept, and a second, smaller face in a side shell.
    /// </summary>
    private static PokeBuilder Clamspire()
    {
        var b = Become(Cloyster(), "Clamspire", 0.96f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var violet = Rgb(188, 170, 222);
        var spike = Rgb(238, 236, 248);
        var pearl = Rgb(44, 40, 74);
        Recolour(b, (Rgb(142, 132, 180), violet));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(violet)))
            s.Color = spike;
        var c = V(0, 0.42f, -0.04f) + lift;
        // The spire of shells grown together over it
        foreach (var (at, r, n) in new[] { (V(0, 0.33f, -0.1f), V(0.19f, 0.14f, 0.16f), 5), (V(0, 0.5f, -0.14f), V(0.12f, 0.11f, 0.11f), 4), (V(0, 0.64f, -0.16f), V(0.07f, 0.07f, 0.07f), 3) })
        {
            b.Ell(Body, c + at, r, violet, mat: Shell, blend: 0.03f);
            for (int i = 0; i < n; i++)
            {
                float a = i * MathF.Tau / n + 0.4f;
                var d = Vector3.Normalize(V(MathF.Sin(a), 0.5f, MathF.Cos(a) * 0.8f - 0.3f));
                var root = Out(c + at, r, default, d);
                b.Spike(Body, root - d * 0.02f, root + d * (0.05f + r.X * 0.4f), 0.03f, spike, mat: Shell);
            }
        }
        b.Spike(Body, c + V(0, 0.68f, -0.16f), c + V(0, 0.82f, -0.2f), 0.04f, spike, mat: Shell);
        // Small shells on its slopes, one of them open on a second, smaller face
        foreach (var (at, size) in new[] { (V(-0.27f, 0.12f, 0.02f), 0.07f), (V(0.2f, -0.2f, 0.1f), 0.06f), (V(-0.18f, 0.28f, -0.12f), 0.055f) })
            b.Ell(Body, c + at, V(size * 1.2f, size, size), PixelCanvas.Light1(violet, 0.12f), V(0, 0, 20f), Shell, 0.015f);
        var side = c + V(0.28f, 0.12f, 0.06f);
        b.Ell(Body, side, V(0.09f, 0.085f, 0.08f), PixelCanvas.Light1(violet, 0.12f), mat: Shell, blend: 0.02f);
        var face = side + V(0.01f, 0, 0.05f);
        var faceR = V(0.065f, 0.06f, 0.05f);
        b.Ell(Body, face, faceR, pearl, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(Body, On(face, faceR, face.X + 0.024f * s, face.Y + 0.008f), V(0.3f * s, 0.05f, 1f), 0.018f, sclera: true, pupil: Black));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2097 Mesmeryx, from Hypno

    /// <summary>
    /// Mesmeryx: Hypno taller and thinner, its yellow paled, its collar grown into a high ruff (the female's fuller
    /// still), and its pendulum become a ring of three coins circling on a thin chain round its hand.
    /// </summary>
    /// <param name="female">A female keeps Hypno's: a fuller ruff.</param>
    private static PokeBuilder Mesmeryx(bool female = false)
    {
        var b = Become(Hypno(female), "Mesmeryx", 0.96f);
        var pale = Rgb(240, 222, 150);
        var ruff = Rgb(246, 246, 240);
        Recolour(b, (Rgb(250, 214, 82), pale));
        // The high ruff standing round the back of its neck
        var rc = V(0, 0.64f, 0);
        float f = female ? 1.2f : 1f;
        for (int i = 0; i < 9; i++)
        {
            float a = (-120f + i * 30f) * Degree;
            var foot = rc + V(MathF.Sin(a) * 0.13f * f, 0.02f, -MathF.Cos(a) * 0.1f * f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a)));
            Blade(b, Body, foot, foot + outward * 0.05f * f + V(0, 0.2f * f - MathF.Abs(MathF.Sin(a)) * 0.04f, 0), 0.05f * f, ruff, outward, 0.24f);
        }
        // The ring of three coins on a thin chain round its hand
        int arm = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => s.Bone == arm && (s.Color.Equals(Rgb(200, 200, 210)) || s.Color.Equals(Rgb(200, 204, 216))));
        var hand = V(0.27f, 0.74f, 0.06f);
        var ring = hand - V(0, 0.13f, 0);
        var chain = Rgb(206, 208, 218);
        b.Tube(arm, new[] { hand - V(0, 0.03f, -0.01f), ring + V(0, 0.005f, 0.08f) }, 0.005f, 0.005f, chain, Metal, 0f);
        b.Torus(arm, ring, 0.08f, 0.005f, chain, mat: Metal, blend: 0f);
        for (int i = 0; i < 3; i++)
        {
            float a = (90f + i * 120f) * Degree;
            var at = ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f);
            b.Ell(arm, at, V(0.03f, 0.03f, 0.008f), Rgb(232, 190, 80), V(0, -a / Degree + 90f, 0), Metal, 0.004f);
        }
        Stretch(b, V(0.92f, 1.08f, 0.92f));
        return b;
    }

    /// <summary>Gives the shapes <paramref name="add"/> lays down the material <paramref name="mat"/> (a ribbon of light, made by a helper that lays fur).</summary>
    private static void As(PokeBuilder b, SurfaceMaterial mat, Action add)
    {
        int before = b.Sdf.Shapes.Count;
        add();
        for (int i = before; i < b.Sdf.Shapes.Count; i++) b.Sdf.Shapes[i].Material = mat;
    }

    // ------------------------------------------------------------------ 2101 Dynamorb, from Electrode

    /// <summary>
    /// Dynamorb: Electrode larger, its red and white halves divided by a band of steel set with three studs, two arcs of
    /// current circling it on thin strands.
    /// </summary>
    private static PokeBuilder Dynamorb()
    {
        var b = Become(Electrode(), "Dynamorb", 0.78f);
        var c = V(0, 0.3f, 0);
        b.Torus(Body, c, 0.3f, 0.026f, Steel, mat: Metal, blend: 0.008f);
        foreach (float a in new[] { -42f, 42f, 180f })
        {
            float r = a * Degree;
            b.Ell(Body, c + V(MathF.Sin(r) * 0.325f, 0, MathF.Cos(r) * 0.325f), V(0.022f, 0.022f, 0.022f), Rgb(140, 146, 158), mat: Metal, blend: 0.004f);
        }
        // Two arcs of current, each held off the ball by two strands
        var current = Rgb(250, 242, 150);
        foreach (var turn in new[] { V(28f, 0, 24f), V(-28f, 0, -24f) })
        {
            var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
            b.Torus(Body, c, 0.4f, 0.009f, current, turn, mat: Glow, blend: 0f);
            foreach (var local in new[] { V(0.4f, 0, 0), V(-0.4f, 0, 0) })
            {
                var p = c + Vector3.Transform(local, q);
                b.Tube(Body, new[] { c + (p - c) * 0.7f, p }, 0.006f, 0.006f, current, Glow, 0f);
            }
        }
        return b;
    }

    // ------------------------------------------------------------------ 2103 Palmagus, from Exeggutor

    /// <summary>
    /// Palmagus: Exeggutor grown into a grove: two trunks rise from one root, each with its crown of leaves and three
    /// coconut heads, and a fourth head grows low at the join.
    /// </summary>
    private static PokeBuilder Palmagus()
    {
        var b = new PokeBuilder("Palmagus", 0.97f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Leaf };
        var bark = Rgb(170, 140, 100);
        var ring = PixelCanvas.Mix(bark, Rgb(70, 50, 30), 0.3f);
        var shell = Rgb(242, 228, 152);
        var leaf = Rgb(96, 170, 80);
        var cream = Rgb(240, 226, 176);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0));
            b.Limb(leg, V(0.1f * s, 0.21f, 0), V(0.12f * s, 0.06f, 0.03f), 0.075f, 0.065f, bark);
            b.Ell(leg, V(0.125f * s, 0.035f, 0.06f), V(0.075f, 0.035f, 0.085f), cream);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.125f * s + 0.035f * i, 0.025f, 0.125f), V(0.125f * s + 0.045f * i, 0.016f, 0.16f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.18f, 0.18f, 0.15f), bark);
        foreach (float y in new[] { 0.22f, 0.29f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), 0.18f * MathF.Sqrt(MathF.Max(0.05f, 1f - MathF.Pow((y - 0.32f) / 0.18f, 2))), 0.016f, ring, sz: 0.83f);
        int tail = b.Tail(V(0, 0.2f, -0.11f));
        b.Limb(tail, V(0, 0.2f, -0.1f), V(0, 0.08f, -0.26f), 0.055f, 0.03f, bark);
        // The fourth head, low at the join
        Coconut(b, Body, V(0, 0.42f, 0.12f), 0.085f, shell, V(0, 0, 1f), true, false, false);
        // Two trunks, each with its crown
        PokeBuilder.Both(s =>
        {
            int trunk = b.Part(s < 0 ? "trunkL" : "trunkR", Body, V(0.06f * s, 0.42f, -0.02f), PokeRole.Segment, s < 0 ? 0.2f : 1.4f, s);
            var spine = Smooth(3, V(0.05f * s, 0.4f, -0.02f), V(0.1f * s, 0.58f, -0.03f), V(0.16f * s, 0.74f, -0.02f), V(0.2f * s, 0.86f, -0.01f));
            b.Tube(trunk, spine, 0.065f, 0.05f, bark, blend: 0f);
            for (int i = 2; i < spine.Length - 1; i += 3)
                b.PaintTorus(trunk, spine[i], 0.062f - 0.002f * i, 0.012f, ring, Euler(spine[i + 1] - spine[i - 1]));
            var top = V(0.2f * s, 0.88f, -0.01f);
            int crown = b.Part(s < 0 ? "crownL" : "crownR", trunk, top, PokeRole.Head, s < 0 ? 0.6f : 2.2f, s);
            b.Ell(crown, top, V(0.09f, 0.07f, 0.08f), bark, blend: 0.04f);
            Coconut(b, crown, top + V(-0.08f, 0.05f, 0.05f), 0.085f, shell, V(-0.4f + 0.5f * s, 0, 1f), s < 0, s > 0, false);
            Coconut(b, crown, top + V(0.08f, 0.06f, 0.04f), 0.085f, shell, V(0.4f + 0.5f * s, 0, 1f), false, false, false);
            Coconut(b, crown, top + V(0.0f, 0.07f, -0.11f), 0.078f, shell, V(0.85f * s, 0.15f, 0.3f), false, false, s > 0);
            var root0 = top + V(0, 0.15f, -0.03f);
            int leaves = b.Part(s < 0 ? "leavesL" : "leavesR", crown, root0, PokeRole.Leaf, s);
            b.Ell(leaves, root0, V(0.065f, 0.055f, 0.065f), leaf);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f + 0.2f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                float front = MathF.Max(0f, dir.Z);
                float reach = 0.4f - 0.15f * front + 0.03f * (i % 3), rise = 0.3f + 0.1f * front;
                var root = root0 + dir * 0.03f;
                LongLeaf(b, leaves, root, root + dir * reach * 0.3f + V(0, rise, 0), root + dir * reach + V(0, rise * (0.6f - 0.15f * (i % 2)), 0), 0.04f, leaf, 0.012f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ 2106 Savateur, from Hitmonlee

    /// <summary>Savateur: Hitmonlee taller still, its legs longer and springier, a short cape of brown hide at its shoulders and wraps round its ankles.</summary>
    private static PokeBuilder Savateur()
    {
        var b = Become(Hitmonlee(), "Savateur", 0.95f);
        var hide = Rgb(134, 84, 56);
        var wrap = Rgb(240, 226, 186);
        // The cape over its shoulders, falling down its back
        b.Ell(Body, V(0, 0.74f, -0.06f), V(0.15f, 0.05f, 0.08f), hide, V(10f, 0, 0), blend: 0.02f);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.09f + 0.06f * i;
            Blade(b, Body, V(x, 0.74f, -0.11f), V(x * 1.4f, 0.5f, -0.18f), 0.06f, hide, V(0, 0.1f, -1f), 0.22f);
        }
        // Wraps round its ankles
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var ankle = V(0.11f * s, 0.07f, 0.03f);
            var dir = Vector3.Normalize(V(0.06f * s, 0.47f, 0) - ankle);
            foreach (float t in new[] { 0.02f, 0.055f })
                b.Torus(leg, ankle + dir * t, 0.042f, 0.013f, wrap, Euler(dir), blend: 0.006f);
        });
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2107 Pugilord, from Hitmonchan

    /// <summary>Pugilord: Hitmonchan broader in the chest, its red gloves become gauntlets of steel banded red, a gold champion's belt at its waist and a high collar.</summary>
    private static PokeBuilder Pugilord()
    {
        var b = Become(Hitmonchan(), "Pugilord", 0.92f);
        var glove = Rgb(222, 64, 64);
        var gold = Rgb(236, 190, 70);
        var tunic = Rgb(214, 210, 236);
        Grow(b, V(1.16f, 1.04f, 1.1f), V(0, 0.55f, 0), s => s.Bone == Body && s.Color.Equals(tunic), _ => false);
        Recolour(b, (glove, Steel), (White, Rgb(214, 54, 58)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Steel))) s.Material = Metal;
        // The champion's belt, its buckle in front
        b.Torus(Body, V(0, 0.46f, 0), 0.138f, 0.024f, gold, sz: 0.8f, mat: Metal, blend: 0.006f);
        b.Ell(Body, V(0, 0.46f, 0.12f), V(0.045f, 0.035f, 0.018f), gold, mat: Metal, blend: 0.004f);
        b.Ell(Body, V(0, 0.46f, 0.133f), V(0.02f, 0.016f, 0.008f), Rgb(214, 54, 58), mat: Shell, blend: 0.002f);
        // A high collar
        b.Torus(Body, V(0, 0.665f, -0.005f), 0.075f, 0.022f, tunic, V(-12f, 0, 0), sz: 0.9f, blend: 0.01f);
        b.Ell(Body, V(0, 0.69f, -0.06f), V(0.08f, 0.04f, 0.03f), tunic, blend: 0.015f);
        return b;
    }

    // ------------------------------------------------------------------ 2110 Smoglom, from Weezing

    /// <summary>
    /// Smoglom: Weezing joined by a third, smallest ball, all three ringed with a thin band of fire at their seams,
    /// smoke rising from vents on their tops.
    /// </summary>
    private static PokeBuilder Smoglom()
    {
        var b = Become(Weezing(), "Smoglom", 0.88f);
        var lift = Shifted(b, "small", V(0.13f, 0.42f, -0.04f));
        var purple = Rgb(136, 96, 152);
        var smoke = Rgb(176, 170, 168);
        Recolour(b, (Rgb(126, 106, 168), purple), (Rgb(214, 206, 150), smoke));
        var cBig = V(-0.08f, 0.32f, 0) + lift;
        var cSmall = V(0.19f, 0.42f, -0.04f) + lift;
        var cThird = V(-0.26f, 0.52f, -0.08f) + lift;
        var rThird = V(0.095f, 0.095f, 0.09f);
        int third = b.Part("third", Body, cThird + V(0.05f, -0.04f, 0), PokeRole.Head, 2.6f, -1f);
        b.Ell(third, cThird, rThird, purple);
        b.Ell(Body, (cBig + cThird) * 0.5f, V(0.06f, 0.06f, 0.06f), purple, blend: 0.05f);
        PokeBuilder.Both(s => b.Eye(third, Out(cThird, rThird, default, V(-0.2f + 0.4f * s, 0.15f, 1f)), V(-0.2f + 0.4f * s, 0.15f, 1f), 0.02f, sclera: true, pupil: Black, glare: true));
        b.Torus(third, Out(cThird, rThird, default, V(-0.1f, -0.35f, 1f)), 0.014f, 0.006f, Rgb(150, 110, 170), Euler(V(-0.1f, -0.35f, 1f)));
        // Bands of fire at the seams
        var fire = Rgb(250, 140, 50);
        foreach (var (a, z, bone) in new[] { (cBig, cSmall, Body), (cBig, cThird, Body) })
        {
            var d = Vector3.Normalize(z - a);
            var mid = a + (z - a) * 0.52f;
            b.Torus(bone, mid, 0.1f, 0.018f, fire, Euler(d), mat: Glow, blend: 0.008f);
        }
        // Vents on their tops, smoke rising from them
        foreach (var (at, size, bone) in new[] { (cBig + V(0.02f, 0.19f, -0.02f), 0.03f, Body), (cSmall + V(0.01f, 0.13f, -0.02f), 0.024f, BoneOf(b, "small")), (cThird + V(0, 0.09f, -0.01f), 0.02f, third) })
        {
            b.Limb(bone, at - V(0, 0.02f, 0), at + V(0, 0.03f, 0), size, size * 0.9f, Rgb(70, 52, 80));
            GasPuff(b, bone, at + V(0, 0.03f + size * 0.5f, 0), V(0.2f, 1f, 0), size * 1.2f, smoke);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2115 Matriroo, from Kangaskhan

    /// <summary>Matriroo: Kangaskhan taller, its helmet grown into a crest, and the child in its pouch older and wearing a helmet of its own.</summary>
    private static PokeBuilder Matriroo()
    {
        var b = Become(Kangaskhan(), "Matriroo", 0.97f);
        var dark = Rgb(92, 70, 62);
        int head = BoneOf(b, "head");
        var c = V(0, 0.9f, 0.08f);
        Blade(b, head, c + V(0, 0.07f, 0.04f), c + V(0, 0.2f, -0.17f), 0.11f, dark, V(1f, 0, 0), 0.25f, Shell);
        // The child grown older, its own helmet on
        int child = BoneOf(b, "baby");
        var bc = V(0, 0.53f, 0.2f);
        Grow(b, V(1.3f, 1.3f, 1.3f), bc - V(0, 0.04f, 0), bone => bone == child);
        var nc = bc - V(0, 0.04f, 0) + V(0, 0.04f, 0) * 1.3f;
        b.Ell(child, nc + V(0, 0.05f, -0.004f), V(0.088f, 0.04f, 0.082f), dark, mat: Shell, blend: 0.012f);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2119 Sabrefin, from Seaking

    /// <summary>
    /// Sabrefin: Seaking longer and sleeker, its horn grown into a straight sword's blade, patched like a koi, its fins
    /// flowing longer; the female's shorter horn kept as a shorter blade.
    /// </summary>
    /// <param name="female">A female keeps Seaking's: a shorter blade.</param>
    private static PokeBuilder Sabrefin(bool female = false)
    {
        var b = Become(Seaking(female), "Sabrefin", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.45f, 0.15f));
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(250, 236, 200)));
        var foot = V(0, 0.6f, 0.22f) + lift;
        var tip = (female ? V(0, 0.78f, 0.36f) : V(0, 0.9f, 0.46f)) + lift;
        Blade(b, head, foot, tip, 0.04f, Rgb(238, 242, 250), V(1f, 0, 0), 0.25f, Shell);
        b.Ell(head, foot + Vector3.Normalize(tip - foot) * 0.025f, V(0.055f, 0.012f, 0.022f), Rgb(222, 180, 90), Euler(tip - foot), Metal, 0.004f);
        // Patched like a koi
        foreach (var (at, size, color) in new[] { (V(0.1f, 0.5f, 0.08f), 0.06f, Rgb(246, 244, 240)), (V(-0.1f, 0.52f, -0.12f), 0.07f, Rgb(246, 244, 240)), (V(0.08f, 0.42f, -0.18f), 0.05f, Rgb(200, 56, 40)), (V(-0.09f, 0.4f, 0.04f), 0.045f, Rgb(200, 56, 40)) })
            b.PaintEll(Body, at + lift, V(size * 0.6f, size, size * 1.2f), color);
        // Longer, flowing fins
        int dorsal = BoneOf(b, "dorsal");
        Grow(b, V(1f, 1.12f, 1.15f), V(0, 0.62f, -0.04f) + lift, bone => bone == dorsal);
        int fin = BoneOf(b, "tailFin");
        Grow(b, V(1f, 1.12f, 1.2f), V(0, 0.45f, -0.33f) + lift, bone => bone == fin);
        PokeBuilder.Both(s =>
        {
            int side = BoneOf(b, s < 0 ? "finL" : "finR");
            Grow(b, V(1.1f, 1.12f, 1.15f), V(0.1f * s, 0.36f, 0.06f) + lift, bone => bone == side);
        });
        Stretch(b, V(0.92f, 1f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2121 Stellamir, from Starmie

    /// <summary>Stellamir: Starmie with a third, smaller star turning between its two, all three round the core, its gem holding a swirl of light.</summary>
    private static PokeBuilder Stellamir()
    {
        var b = Become(Starmie(), "Stellamir", 0.9f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var c = V(0, 0.42f, 0) + lift;
        int star = b.Part("star3", Body, c, PokeRole.Leaf, 1.2f);
        StarPoints(b, star, c - V(0, 0, 0.015f), 0.37f, 0.1f, 18f, 0.42f, Rgb(226, 162, 226));
        b.Ell(star, c - V(0, 0, 0.015f), V(0.1f, 0.1f, 0.05f), Rgb(226, 162, 226), blend: 0.02f);
        // The gem's swirl of light, pink and violet
        Recolour(b, (Rgb(214, 48, 78), Rgb(236, 120, 200)));
        var gem = c + V(0, 0, 0.09f + 0.068f * 0.15f + 0.03f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.6f;
            float rr = 0.012f + 0.008f * i;
            b.PaintEll(Body, gem + V(MathF.Cos(a) * rr, MathF.Sin(a) * rr, 0), V(0.012f, 0.012f, 0.02f), Rgb(170, 110, 240), soft: 0.006f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2122 Mimestro, from Mr. Mime

    /// <summary>
    /// Mimestro: Mr. Mime taller, in a tall hat and a conductor's tailcoat of pink and white, a pane of light held in
    /// its hand.
    /// </summary>
    private static PokeBuilder Mimestro()
    {
        var b = Become(MrMime(), "Mimestro", 0.92f);
        var pink = Rgb(238, 98, 120);
        var navy = Rgb(48, 54, 112);
        int head = BoneOf(b, "head");
        // A tall hat, banded pink
        b.Ell(head, V(0, 0.965f, 0.01f), V(0.12f, 0.014f, 0.11f), navy, mat: Shell, blend: 0.01f);
        b.Limb(head, V(0, 0.96f, 0.01f), V(0, 1.14f, 0.0f), 0.07f, 0.076f, navy, Shell, 0.004f);
        b.PaintTorus(head, V(0, 0.995f, 0.008f), 0.072f, 0.012f, pink, V(-3f, 0, 0));
        // The tailcoat's tails behind, its lapels in front
        foreach (float x in new[] { -1f, 1f })
            Blade(b, Body, V(0.05f * x, 0.58f, -0.09f), V(0.09f * x, 0.3f, -0.16f), 0.06f, pink, V(x * 0.3f, 0, -1f), 0.25f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.07f * s, 0.68f, 0.09f), V(0.035f, 0.07f, 0.03f), pink, V(0, 0, -20f * s)));
        // A pane of light held in its right hand
        b.Box(BoneOf(b, "armR"), V(0.5f, 0.72f, 0.12f), V(0.07f, 0.11f, 0.006f), 0.004f, Rgb(190, 228, 255), V(0, 0, -12f), Glow, 0.006f);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2124 Aurorelle, from Jynx

    /// <summary>
    /// Aurorelle: a new figure out of Jynx, tall and slender in a gown of ice crystals, its long hair pale blue-white
    /// and lit along its length by the green and violet bands of an aurora, its face a pale lilac mask with large eyes.
    /// </summary>
    private static PokeBuilder Aurorelle()
    {
        var b = Become(Jynx(), "Aurorelle", 0.92f);
        var gown = Rgb(170, 208, 240);
        var ice = Rgb(224, 242, 255);
        Recolour(b, (Rgb(214, 72, 66), gown), (Rgb(244, 226, 156), Rgb(226, 236, 250)), (Rgb(112, 92, 168), Rgb(206, 188, 232)), (Rgb(242, 200, 84), Rgb(214, 226, 244)));
        // Crystals of ice on the gown, round its hem and at its shoulders
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = d * 0.23f + V(0, 0.07f, 0);
            b.Spike(Body, at - d * 0.02f, at + d * 0.05f + V(0, 0.06f, 0), 0.025f, ice, mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.09f * s, 0.56f, 0.0f), V(0.14f * s, 0.65f, -0.02f), 0.024f, ice, mat: Shell, blend: 0.006f));
        // The aurora along its hair
        int head = BoneOf(b, "head");
        foreach (var (x, color) in new[] { (-0.07f, Rgb(120, 240, 190)), (-0.025f, Rgb(190, 140, 250)), (0.025f, Rgb(120, 240, 190)), (0.07f, Rgb(190, 140, 250)) })
            PaintAs(b, head, V(x, 0.58f, -0.11f), V(0.016f, 0.2f, 0.06f), color, Glow);
        // Larger eyes
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Size *= 1.25f;
            d.Half *= 1.25f;
        }
        Stretch(b, V(0.92f, 1.12f, 0.92f));
        return b;
    }

    // ------------------------------------------------------------------ 2127 Mandibron, from Pinsir

    /// <summary>
    /// Mandibron: Pinsir darkened to brown-black, its horns grown longer and near black with an edge of red teeth, and
    /// a smooth carapace closed over its back.
    /// </summary>
    private static PokeBuilder Mandibron()
    {
        var b = Become(Pinsir(), "Mandibron", 0.95f);
        var oldBrown = Rgb(176, 162, 150);
        var brown = Rgb(84, 64, 54);
        Recolour(b, (oldBrown, brown), (PixelCanvas.Mix(oldBrown, Rgb(60, 50, 50), 0.35f), Rgb(52, 40, 38)));
        int head = BoneOf(b, "head");
        var horn = Rgb(232, 232, 236);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(horn)))
            s.Color = s.Blend == 0f ? Rgb(44, 38, 42) : Rgb(204, 52, 52);
        Grow(b, V(1.1f, 1.3f, 1.1f), V(0, 0.6f, 0), bone => bone == head);
        // A smooth carapace over its back
        var c = V(0, 0.44f, 0);
        b.Ell(Body, c + V(0, 0.03f, -0.07f), V(0.18f, 0.22f, 0.1f), Rgb(60, 44, 42), mat: Shell, blend: 0.02f);
        b.PaintEll(Body, c + V(0, 0.03f, -0.17f), V(0.006f, 0.2f, 0.03f), Rgb(110, 60, 50), soft: 0.006f);
        return b;
    }

    // ------------------------------------------------------------------ 2128 Bovarch, from Tauros

    /// <summary>
    /// Bovarch: Tauros heavier in the shoulders, its horns grown wider and ringed, its mane longer and darker, and a
    /// crest of rock-like hide on its brow where the three studs were.
    /// </summary>
    private static PokeBuilder Bovarch()
    {
        var b = Become(Tauros(), "Bovarch", 1f);
        var oldMane = Rgb(116, 106, 100);
        var mane = Rgb(80, 70, 66);
        Recolour(b, (oldMane, mane));
        Grow(b, V(1.12f, 1.15f, 1.15f), V(0, 0.5f, 0.15f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        int head = BoneOf(b, "head");
        var horn = Rgb(150, 152, 170);
        // The studs give way to a crest of rock-like hide
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(horn));
        var c = V(0, 0.52f, 0.37f);
        var r = V(0.085f, 0.08f, 0.09f);
        var brow = On(c, r, 0, 0.555f);
        b.Ell(head, brow + V(0, 0.01f, -0.015f), V(0.06f, 0.03f, 0.04f), Rgb(150, 146, 140), V(-20f, 0, 0), Shell, 0.012f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(head, brow + V(x, 0.02f, -0.02f), brow + V(x * 1.3f, 0.06f, -0.05f), 0.016f, Rgb(150, 146, 140), mat: Shell, blend: 0.006f);
        // Wider horns, ringed
        Grow(b, V(1.4f, 1.08f, 1f), V(0, 0.56f, 0.35f), s => s.Bone == head && s.Color.Equals(horn), _ => false);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.15f * s, 0.585f, 0.36f), V(0.2f * s, 0.65f, 0.36f), V(0.18f * s, 0.74f, 0.38f));
            for (int i = 2; i < path.Length - 2; i += 2)
            {
                var p = V(0, 0.56f, 0.35f) + (path[i] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                var q = V(0, 0.56f, 0.35f) + (path[i + 1] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                float t = i / (float)(path.Length - 1);
                b.PaintTorus(head, p, 0.03f + (0.012f - 0.03f) * t, 0.005f, Rgb(96, 96, 112), Euler(q - p));
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ 2130 Typhonarch, from Gyarados

    /// <summary>
    /// Typhonarch: Gyarados grown into the dragon it has always looked like: two short forelegs with clawed hands, its
    /// crest grown into a great fan of fins and its whiskers long and flowing (the female's white).
    /// </summary>
    /// <param name="female">A female keeps Gyarados's: white whiskers.</param>
    private static PokeBuilder Typhonarch(bool female = false)
    {
        var b = Become(Gyarados(female), "Typhonarch", 1f);
        var blue = Rgb(58, 150, 226);
        var cream = Rgb(244, 226, 156);
        var navy = Rgb(36, 70, 160);
        var white = Rgb(236, 242, 248);
        const float hy = 0.94f;
        int head = BoneOf(b, "head");
        // The crest grown into a great fan of fins
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(navy));
        for (int i = 0; i < 7; i++)
        {
            float a = (-66f + i * 22f) * Degree;
            var root = V(MathF.Sin(a) * 0.04f, hy + 0.08f, 0.1f);
            Blade(b, head, root, root + V(MathF.Sin(a) * 0.3f, MathF.Cos(a) * 0.3f, -0.14f), 0.075f, navy, V(0, 0.15f, 1f), 0.25f);
        }
        // Long flowing whiskers
        var whisker = female ? white : Rgb(96, 172, 236);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.14f * s, hy - 0.26f, 0.31f), V(0.1f * s, hy - 0.38f, 0.28f), V(0.08f * s, hy - 0.48f, 0.33f), V(0.1f * s, hy - 0.56f, 0.4f) }, 0.009f, 0.005f, whisker));
        // Two short forelegs with clawed hands, from where it rears up
        int seg = BoneOf(b, "seg4");
        var mid = (V(0.16f, 0.44f, 0.05f) + V(0.05f, 0.62f, -0.03f)) / 2f;
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "armL" : "armR", seg, mid + V(0.1f * s, -0.03f, 0.04f), PokeRole.Arm, s, s);
            var shoulder = mid + V(0.08f * s, -0.02f, 0.05f);
            var elbow = mid + V(0.26f * s, -0.1f, 0.14f);
            var hand = mid + V(0.22f * s, -0.16f, 0.3f);
            b.Limb(arm, shoulder, elbow, 0.062f, 0.05f, blue);
            b.Ell(arm, elbow, V(0.05f, 0.05f, 0.05f), blue);
            b.Limb(arm, elbow, hand, 0.05f, 0.042f, blue);
            b.PaintEll(arm, (elbow + hand) / 2f + V(0, -0.025f, 0.01f), V(0.04f, 0.07f, 0.04f), cream, Euler(hand - elbow));
            b.Ell(arm, hand, V(0.045f, 0.04f, 0.045f), blue);
            Claws(b, arm, hand + V(0, -0.01f, 0.035f), V(0.024f, 0, 0), V(0, -0.4f, 1f), 0.05f, 0.014f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2134 Naiadeon, from Vaporeon

    /// <summary>
    /// Naiadeon: Vaporeon longer, its ruff grown into a flowing mane of fins, its tail fin wider and pale at its edge,
    /// pale wave lines painted down its body.
    /// </summary>
    private static PokeBuilder Naiadeon()
    {
        var b = Become(Vaporeon(), "Naiadeon", 0.84f);
        const float h = 0.17f;
        var cream = Rgb(248, 240, 200);
        var pale = Rgb(206, 238, 252);
        // The mane of fins flowing back down its neck
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var at = V(MathF.Sin(a) * 0.045f, h + 0.17f, 0.12f + MathF.Cos(a) * 0.035f);
            Blade(b, Body, at, at + V(MathF.Sin(a) * 0.07f, -0.04f, -0.14f), 0.03f, cream, V(MathF.Sin(a), 1f, 0), 0.3f);
        }
        // A wider tail fin, pale at its edge
        int tail = BoneOf(b, "tail");
        var end = V(0.15f, h - 0.03f, -0.4f);
        Grow(b, V(1.6f, 1.6f, 1.6f), end, s => s.Bone == tail && Vector3.Distance(s.A, end) < 0.01f, _ => false);
        b.PaintEll(tail, end + V(0.16f, 0.09f, -0.07f), V(0.06f, 0.06f, 0.06f), pale);
        b.PaintEll(tail, end + V(0.14f, -0.015f, 0.12f), V(0.06f, 0.06f, 0.06f), pale);
        // Wave lines down its body
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.05f, -0.07f })
            {
                var n = V(s, 0.35f, z * 4f);
                b.Mark(Body, Out(V(0, h + 0.02f, -0.025f), V(0.075f, 0.07f, 0.15f), default, n), n, 0.04f, 0.012f, pale, MarkShape.Wave);
            }
        });
        Stretch(b, V(1f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2135 Fulgureon, from Jolteon

    /// <summary>Fulgureon: Jolteon with its spikes grown long and swept back into bolts of lightning, white at their tips, and its ruff stood up in a crest.</summary>
    private static PokeBuilder Fulgureon()
    {
        var b = Become(Jolteon(), "Fulgureon", 0.8f);
        var yellow = Rgb(250, 214, 56);
        int tail = BoneOf(b, "tail");
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + d * 1.6f + V(0, 0, -0.05f);
            var dir = Vector3.Normalize(s.B - s.A);
            var kink = Vector3.Normalize(dir + Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f)) + V(0.001f, 0, 0)) * 0.9f);
            b.Spike(tail, s.B - dir * 0.015f, s.B + kink * 0.07f, 0.02f, Rgb(255, 252, 236), mat: Glow, blend: 0.006f);
        }
        // The ruff stood up in a crest
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(White) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + Vector3.Normalize(d + V(0, 0.16f, 0)) * d.Length() * 1.4f;
        }
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2136 Pyreon, from Flareon

    /// <summary>Pyreon: Flareon with its ruff and tail fuller, small flames burning at the ruff's tips, and the tuft on its brow streaming back.</summary>
    private static PokeBuilder Pyreon()
    {
        var b = Become(Flareon(), "Pyreon", 0.82f);
        const float h = 0.2f;
        var cream = Rgb(250, 230, 150);
        var about = V(0, h + 0.11f, 0.11f);
        Grow(b, V(1.25f, 1.25f, 1.25f), about, s => s.Bone == Body && s.Color.Equals(cream), _ => false);
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.25f, 1.3f, 1.25f), V(0, h + 0.1f, -0.2f), s => s.Bone == tail && s.Color.Equals(cream), _ => false);
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var n = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 0.4f, MathF.Cos(a) * 0.9f));
            var at = Out(about, V(0.106f, 0.094f, 0.081f), default, n);
            FlameTongue(b, Body, at - n * 0.012f, at + n * 0.04f + V(0, 0.045f, 0), 0.016f, Rgb(244, 104, 44), Rgb(255, 220, 100));
        }
        int head = BoneOf(b, "head");
        b.Spike(head, V(0, h + 0.3f, 0.15f), V(0, h + 0.36f, 0.0f), 0.03f, cream, 0.6f);
        return b;
    }

    // ------------------------------------------------------------------ 2143 Dozemount, from Snorlax

    /// <summary>
    /// Dozemount: Snorlax asleep so long the woods grew on it: a mossy mantle on its back and shoulders with small trees
    /// and flowers, and a bird's nest on its head.
    /// </summary>
    private static PokeBuilder Dozemount()
    {
        var b = Become(Snorlax(), "Dozemount", 1f);
        var moss = Rgb(98, 150, 72);
        var deep = Rgb(70, 120, 58);
        // The mantle of moss over its back and shoulders
        b.Ell(Body, V(0, 0.52f, -0.1f), V(0.34f, 0.2f, 0.24f), moss, V(-10f, 0, 0), Leaf, 0.04f);
        foreach (var (at, size) in new[] { (V(0.24f, 0.56f, 0.0f), 0.1f), (V(-0.24f, 0.56f, 0.0f), 0.1f), (V(0.12f, 0.66f, -0.14f), 0.09f), (V(-0.14f, 0.64f, -0.18f), 0.08f), (V(0.0f, 0.5f, -0.3f), 0.1f) })
            b.Ell(Body, at, V(size, size * 0.7f, size), moss, mat: Leaf, blend: 0.03f);
        // Small trees on it, flowers in the moss
        foreach (var (foot, r) in new[] { (V(0.18f, 0.66f, -0.12f), 0.07f), (V(-0.16f, 0.66f, -0.16f), 0.06f), (V(0.02f, 0.62f, -0.28f), 0.055f) })
            Foliage(b, Body, foot, foot + V(0, r * 1.6f, 0), r, Rgb(120, 86, 54), deep);
        foreach (var (at, color) in new[] { (V(0.26f, 0.62f, 0.06f), Rgb(246, 200, 220)), (V(-0.26f, 0.62f, 0.04f), Rgb(250, 230, 120)), (V(0.06f, 0.7f, -0.08f), Rgb(246, 246, 250)), (V(-0.08f, 0.68f, -0.06f), Rgb(246, 200, 220)) })
            b.Ell(Body, at, V(0.02f, 0.014f, 0.02f), color, mat: Leaf, blend: 0.004f);
        // A bird's nest on its head
        int head = BoneOf(b, "head");
        b.Torus(head, V(0, 0.9f, -0.01f), 0.065f, 0.028f, Rgb(140, 100, 62), mat: Leaf, blend: 0.01f);
        b.Ell(head, V(0, 0.9f, -0.01f), V(0.06f, 0.02f, 0.06f), Rgb(120, 86, 54), mat: Leaf, blend: 0.01f);
        foreach (float x in new[] { -0.02f, 0.02f })
            b.Ell(head, V(x, 0.925f, -0.01f + x), V(0.016f, 0.02f, 0.016f), Rgb(226, 236, 246), mat: Shell, blend: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ 2144 Glacirune, from Articuno

    /// <summary>
    /// Glacirune: Articuno grown, its two tail streamers become a long veil of aurora, green and violet, and crystals
    /// of ice at its wing tips.
    /// </summary>
    private static PokeBuilder Glacirune()
    {
        var b = Become(Articuno(), "Glacirune", 1f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.04f));
        int tail = BoneOf(b, "tail");
        var green = Rgb(120, 236, 190);
        var violet = Rgb(186, 140, 250);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail))
        {
            s.Color = s.A.X < 0 ? green : violet;
            s.Material = Glow;
        }
        As(b, Glow, () =>
        {
            foreach (float sx in new[] { -1f, 1f })
                Ribbon(b, tail, Smooth(3, V(0.01f * sx, 0.46f, -0.1f), V(0.03f * sx, 0.32f, -0.32f), V(0.06f * sx, 0.18f, -0.5f), V(0.1f * sx, 0.12f, -0.6f), V(0.14f * sx, 0.16f, -0.66f)).Select(p => p + lift).ToArray(),
                    0.05f, sx < 0 ? violet : green, V(sx * 0.4f, 0.2f, 0.6f), 0.014f);
        });
        // Crystals of ice at the ends of its long feathers
        var ice = Rgb(220, 242, 255);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.64f, -0.02f) + lift;
            var wrist = V(0.24f * s, 0.8f, -0.06f) + lift;
            for (int i = 2; i < 6; i++)
            {
                float t = i / 5f;
                float a = (112f + (28f - 112f) * t) * Degree;
                var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.3f), wrist, t);
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.25f));
                float len = 0.38f * (0.7f + 0.3f * MathF.Sin(MathF.PI * (0.25f + 0.75f * t)));
                var end = root + dir * len;
                b.Spike(wing, end - dir * 0.03f, end + dir * 0.06f, 0.022f, ice, mat: Shell, blend: 0.006f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2145 Fulguris, from Zapdos

    /// <summary>Fulguris: Zapdos grown, its jagged feathers longer and darker at their roots, a crown of storm cloud at its head.</summary>
    private static PokeBuilder Fulguris()
    {
        var b = Become(Zapdos(), "Fulguris", 1f);
        var lift = Shifted(b, "head", V(0, 0.66f, 0.05f));
        var yellow = Rgb(246, 214, 70);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone))
            s.B = s.A + (s.B - s.A) * 1.4f;
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.62f, -0.02f) + lift;
            Grow(b, V(1.22f, 1.22f, 1.1f), shoulder, bone => bone == wing);
            b.PaintEll(wing, shoulder + V(0.12f * s, 0.06f, -0.02f), V(0.14f, 0.1f, 0.08f), Rgb(196, 150, 40));
        });
        // A crown of storm cloud at its head
        int head = BoneOf(b, "head");
        var hc = V(0, 0.75f, 0.08f) + lift;
        var cloud = Rgb(140, 144, 158);
        foreach (var (at, r) in new[] { (V(0, 0.07f, -0.04f), 0.05f), (V(-0.05f, 0.06f, -0.06f), 0.04f), (V(0.05f, 0.06f, -0.06f), 0.04f), (V(0, 0.08f, -0.1f), 0.045f), (V(-0.035f, 0.1f, -0.02f), 0.032f), (V(0.035f, 0.1f, -0.02f), 0.032f) })
            b.Ell(head, hc + at, V(r, r * 0.8f, r), cloud, blend: 0.02f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2146 Pyrevant, from Moltres

    /// <summary>Pyrevant: Moltres with its fire whitened at the core, a tail of three long flame streamers and a halo of embers at its crest.</summary>
    private static PokeBuilder Pyrevant()
    {
        var b = Become(Moltres(), "Pyrevant", 1f);
        var lift = Shifted(b, "head", V(0, 0.64f, 0.1f));
        var rim = Rgb(244, 112, 50);
        var heart = Rgb(255, 246, 216);
        Recolour(b, (Rgb(255, 214, 90), heart), (PixelCanvas.Mix(rim, Rgb(255, 214, 90), 0.5f), PixelCanvas.Mix(rim, heart, 0.5f)));
        // Three long streamers of flame for a tail
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        foreach (float dx in new[] { -1f, 0f, 1f })
        {
            var path = Smooth(3, V(0, 0.5f, -0.14f), V(dx * 0.06f, 0.48f, -0.36f), V(dx * 0.14f, 0.42f, -0.58f), V(dx * 0.2f, 0.38f + 0.04f * MathF.Abs(dx), -0.8f)).Select(p => p + lift).ToArray();
            b.Tube(tail, path, 0.045f, 0.018f, rim, Glow, 0f);
            for (int i = 1; i < path.Length - 1; i += 2)
                b.PaintEll(tail, path[i], V(0.02f, 0.04f, 0.02f), heart, Euler(path[i + 1] - path[i - 1]));
            FlameTongue(b, tail, path[^1] - Vector3.Normalize(path[^1] - path[^2]) * 0.02f, path[^1] + Vector3.Normalize(path[^1] - path[^2]) * 0.12f + V(0, 0.04f, 0), 0.03f, rim, heart);
        }
        // A halo of embers round its crest, on a thread of light
        int head = BoneOf(b, "head");
        var c = V(0, 0.73f, 0.17f) + lift;
        var ring = c + V(0, 0.12f, -0.05f);
        b.Torus(head, ring, 0.08f, 0.013f, Rgb(255, 200, 110), mat: Glow, blend: 0.004f);
        b.Tube(head, new[] { c + V(0, 0.09f, -0.02f), ring + V(0, 0, 0.08f) }, 0.013f, 0.013f, Rgb(255, 200, 110), Glow, 0.004f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Ell(head, ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f), V(0.022f, 0.022f, 0.022f), Rgb(255, 170, 70), mat: Glow, blend: 0.006f);
        }
        return Lift(b);
    }
}
