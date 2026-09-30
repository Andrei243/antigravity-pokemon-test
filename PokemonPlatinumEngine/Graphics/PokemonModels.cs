using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>How a bone moves in the idle loop.</summary>
internal enum Motion { None, Body, Head, Eyes, Tail, WingL, WingR, ArmL, ArmR, Ear, Leaf, Flame, Jaw, Legs }

/// <summary>Animation inputs for one frame of a Pokémon.</summary>
internal struct PokePose
{
    public float Time;

    /// <summary>1 while the eyes are shut.</summary>
    public float Blink;

    /// <summary>0..1 progress of an attack (rear back, then strike).</summary>
    public float Attack;

    /// <summary>0..1 flinch after taking a hit.</summary>
    public float Hurt;
}

/// <summary>A rigid group of a Pokémon model that rotates about its joint.</summary>
internal sealed class PokeBone
{
    public string Name = "";
    public int Parent = -1;

    /// <summary>Model-space position of the joint (at rest).</summary>
    public Vector3 Pivot;

    public Motion Motion;
    public float Phase;

    /// <summary>Geometry relative to <see cref="Pivot"/>.</summary>
    public readonly MeshBuilder Geometry = new();

    public Mesh Mesh, Outline;
    public bool Uploaded;
}

/// <summary>A Pokémon built from rounded toon-shaded shapes, rendered into pixel-art battle and menu sprites.</summary>
internal sealed class PokeModel
{
    public string Species = "";
    public readonly List<PokeBone> Bones = new();

    /// <summary>Share of the sprite frame the model fills (small Pokémon look small).</summary>
    public float Fill = 0.7f;

    /// <summary>Flying Pokémon bob in the air instead of standing.</summary>
    public bool Hovers;

    /// <summary>Idle loop speed multiplier.</summary>
    public float Tempo = 1f;

    /// <summary>
    /// Model-space transform of every bone for a pose (System.Numerics row-vector convention). Children
    /// inherit their parent's motion.
    /// </summary>
    public void BoneTransforms(PokePose pose, Span<Matrix4x4> result)
    {
        float t = pose.Time * Tempo;
        float w = MathF.Tau / 1.7f;
        float attack = pose.Attack <= 0f ? 0f : MathF.Sin(Math.Clamp(pose.Attack, 0f, 1f) * MathF.PI);
        float hurt = pose.Hurt <= 0f ? 0f : MathF.Sin(Math.Clamp(pose.Hurt, 0f, 1f) * MathF.PI);

        for (int i = 0; i < Bones.Count; i++)
        {
            var b = Bones[i];
            float ph = b.Phase;
            Matrix4x4 local = b.Motion switch
            {
                Motion.Body => Matrix4x4.CreateScale(1f + 0.015f * MathF.Sin(w * t), 1f + 0.035f * MathF.Sin(w * t) - 0.08f * hurt, 1f)
                    * Matrix4x4.CreateRotationX(0.12f * attack - 0.14f * hurt)
                    * Matrix4x4.CreateTranslation(0, Hovers ? 0.06f * MathF.Sin(w * t * 0.8f) : 0f, 0.08f * attack),
                Motion.Head => Matrix4x4.CreateRotationX(0.06f * MathF.Sin(w * t + 0.7f) - 0.28f * attack + 0.2f * hurt)
                    * Matrix4x4.CreateRotationY(0.1f * MathF.Sin(w * t * 0.45f + ph)),
                Motion.Eyes => Matrix4x4.CreateScale(1f, pose.Blink > 0.5f || hurt > 0.3f ? 0.12f : 1f, 1f),
                Motion.Tail => Matrix4x4.CreateRotationY(0.32f * MathF.Sin(w * t * 1.3f + ph)) * Matrix4x4.CreateRotationX(0.1f * MathF.Sin(w * t + ph)),
                Motion.WingL => Matrix4x4.CreateRotationZ((Hovers ? 0.55f : 0.14f) * MathF.Sin(w * t * (Hovers ? 2.2f : 1f) + ph) + 0.5f * attack),
                Motion.WingR => Matrix4x4.CreateRotationZ(-(Hovers ? 0.55f : 0.14f) * MathF.Sin(w * t * (Hovers ? 2.2f : 1f) + ph) - 0.5f * attack),
                Motion.ArmL => Matrix4x4.CreateRotationX(0.14f * MathF.Sin(w * t + ph) - 0.9f * attack) * Matrix4x4.CreateRotationZ(0.05f * MathF.Sin(w * t)),
                Motion.ArmR => Matrix4x4.CreateRotationX(-0.14f * MathF.Sin(w * t + ph) - 0.9f * attack) * Matrix4x4.CreateRotationZ(-0.05f * MathF.Sin(w * t)),
                Motion.Ear => Matrix4x4.CreateRotationZ(0.1f * MathF.Sin(w * t * 0.9f + ph) * MathF.Sign(b.Pivot.X == 0 ? 1 : b.Pivot.X)),
                Motion.Leaf => Matrix4x4.CreateRotationZ(0.16f * MathF.Sin(w * t * 1.2f + ph)) * Matrix4x4.CreateRotationX(0.08f * MathF.Sin(w * t * 0.7f + ph)),
                Motion.Flame => Matrix4x4.CreateScale(1f + 0.08f * MathF.Sin(t * 11f + ph) + 0.05f * MathF.Sin(t * 17f),
                    1f + 0.14f * MathF.Sin(t * 9f + ph) + 0.08f * MathF.Sin(t * 23f + 1f) + 0.3f * attack, 1f + 0.08f * MathF.Sin(t * 13f))
                    * Matrix4x4.CreateRotationZ(0.08f * MathF.Sin(t * 5f + ph)),
                Motion.Jaw => Matrix4x4.CreateRotationX(0.55f * attack + 0.05f * MathF.Max(0f, MathF.Sin(w * t))),
                Motion.Legs => Matrix4x4.CreateRotationX(0.06f * MathF.Sin(w * t + ph)),
                _ => Matrix4x4.Identity
            };

            var joint = Matrix4x4.CreateTranslation(b.Parent >= 0 ? b.Pivot - Bones[b.Parent].Pivot : b.Pivot);
            result[i] = b.Parent >= 0 ? local * joint * result[b.Parent] : local * joint;
        }
    }
}

/// <summary>Builds each species from primitives, placing everything in model space (facing +Z, feet at y = 0).</summary>
internal sealed class PokeBuilder
{
    private const float Deg = MathF.PI / 180f;
    public const int Body = 0;
    private readonly PokeModel m;

    public PokeBuilder(string species, float fill)
    {
        m = new PokeModel { Species = species, Fill = fill };
        Bone("body", -1, Vector3.Zero, Motion.Body);
    }

    public PokeModel Model => m;

    public PokeBuilder Hover() { m.Hovers = true; return this; }

    public int Bone(string name, int parent, Vector3 pivot, Motion motion, float phase = 0f)
    {
        m.Bones.Add(new PokeBone { Name = name, Parent = parent, Pivot = pivot, Motion = motion, Phase = phase });
        return m.Bones.Count - 1;
    }

    private void Put(int bone, Matrix4x4 placement, Action<MeshBuilder> build) =>
        m.Bones[bone].Geometry.Add(placement * Matrix4x4.CreateTranslation(-m.Bones[bone].Pivot), build);

    private static Matrix4x4 Rot(Vector3 deg) => Matrix4x4.CreateFromYawPitchRoll(deg.Y * Deg, deg.X * Deg, deg.Z * Deg);

    public void Ell(int bone, Vector3 center, Vector3 radii, Color color, Vector3 rotationDeg = default) =>
        Put(bone, Rot(rotationDeg) * Matrix4x4.CreateTranslation(center), p => p.Ellipsoid(Vector3.Zero, radii, color, 18, 11));

    /// <summary>Ellipsoid shaded from <paramref name="bottom"/> to <paramref name="top"/> (bellies, fur fading).</summary>
    public void EllGrad(int bone, Vector3 center, Vector3 radii, Color bottom, Color top, Vector3 rotationDeg = default) =>
        Put(bone, Rot(rotationDeg) * Matrix4x4.CreateTranslation(center),
            p => p.Ellipsoid(Vector3.Zero, radii, k => PixelCanvas.Mix(bottom, top, k / 11f), 18, 11));

    /// <summary>Rounded limb (capsule) from <paramref name="a"/> to <paramref name="b"/>, tapering from ra to rb.</summary>
    public void Limb(int bone, Vector3 a, Vector3 b, float ra, float rb, Color color)
    {
        float length = Vector3.Distance(a, b);
        var profile = new List<Vector2>();
        for (int k = 0; k <= 4; k++)
        {
            float th = -MathF.PI / 2f + k * MathF.PI / 8f;
            profile.Add(new Vector2(ra * MathF.Cos(th), ra * MathF.Sin(th)));
        }
        for (int k = 1; k <= 4; k++)
        {
            float th = k * MathF.PI / 8f;
            profile.Add(new Vector2(rb * MathF.Cos(th), length + rb * MathF.Sin(th)));
        }
        Put(bone, AlignY(b - a) * Matrix4x4.CreateTranslation(a), p => p.Lathe(Vector3.Zero, profile, 12, color));
    }

    /// <summary>Cone from a round base at <paramref name="baseCenter"/> to a point at <paramref name="tip"/>.</summary>
    public void Spike(int bone, Vector3 baseCenter, Vector3 tip, float radius, Color color, float squash = 1f)
    {
        float length = Vector3.Distance(baseCenter, tip);
        Put(bone, AlignY(tip - baseCenter) * Matrix4x4.CreateTranslation(baseCenter),
            p => p.Lathe(Vector3.Zero, new[] { new Vector2(0, 0), new Vector2(radius, 0), new Vector2(radius * 0.55f, length * 0.45f), new Vector2(0, length) }, 10, color, 1f, squash));
    }

    public void Torus(int bone, Vector3 center, float ring, float tube, Color color, Vector3 rotationDeg = default, float sx = 1f, float sz = 1f) =>
        Put(bone, Rot(rotationDeg) * Matrix4x4.CreateTranslation(center), p => p.Torus(Vector3.Zero, ring, tube, color, 20, 8, sx, sz));

    /// <summary>
    /// A cartoon eye on the surface of the head: dark oval with a coloured iris and a white glint, or a white
    /// eye with a dark pupil (<paramref name="sclera"/>).
    /// </summary>
    public void Eye(int bone, Vector3 at, Vector3 facing, float size, Color? iris = null, bool sclera = false, Color? pupil = null)
    {
        var place = Face(facing) * Matrix4x4.CreateTranslation(at);
        var dark = pupil ?? new Color(28, 24, 40, 255);
        Put(bone, place, p =>
        {
            if (sclera)
            {
                p.Ellipsoid(Vector3.Zero, new Vector3(size * 0.82f, size, size * 0.34f), new Color(250, 250, 252, 255), 14, 8);
                p.Ellipsoid(new Vector3(size * 0.08f, -size * 0.08f, size * 0.14f), new Vector3(size * 0.5f, size * 0.64f, size * 0.26f), dark, 12, 8);
            }
            else
            {
                p.Ellipsoid(Vector3.Zero, new Vector3(size * 0.7f, size, size * 0.34f), dark, 14, 8);
                if (iris.HasValue)
                    p.Ellipsoid(new Vector3(0, -size * 0.3f, size * 0.12f), new Vector3(size * 0.48f, size * 0.52f, size * 0.25f), iris.Value, 12, 6);
            }
            p.Ellipsoid(new Vector3(-size * 0.25f, size * 0.38f, size * 0.26f), new Vector3(size * 0.26f, size * 0.26f, size * 0.12f), Color.White, 8, 5);
        });
    }

    /// <summary>Rotation turning +Z toward <paramref name="facing"/> while keeping +Y up.</summary>
    private static Matrix4x4 Face(Vector3 facing)
    {
        var f = Vector3.Normalize(facing);
        var r = Vector3.Cross(Vector3.UnitY, f);
        r = r.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(r);
        var u = Vector3.Cross(f, r);
        return new Matrix4x4(r.X, r.Y, r.Z, 0, u.X, u.Y, u.Z, 0, f.X, f.Y, f.Z, 0, 0, 0, 0, 1);
    }

    private static Matrix4x4 AlignY(Vector3 dir)
    {
        var d = Vector3.Normalize(dir);
        var axis = Vector3.Cross(Vector3.UnitY, d);
        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, d), -1f, 1f));
        if (axis.LengthSquared() < 1e-8f) return angle > 1f ? Matrix4x4.CreateRotationX(MathF.PI) : Matrix4x4.Identity;
        return Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
    }

    /// <summary>Calls <paramref name="build"/> once for each side (s = -1 left, +1 right) of a symmetric part.</summary>
    public static void Both(Action<float> build)
    {
        build(-1f);
        build(1f);
    }
}

/// <summary>The species models.</summary>
internal static class PokemonModels
{
    private static readonly Dictionary<string, PokeModel> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);
    private static Vector3 V(float x, float y, float z) => new(x, y, z);

    private static readonly Color White = Rgb(246, 246, 250);
    private static readonly Color Black = Rgb(44, 42, 56);
    private static readonly Color Claw = Rgb(240, 240, 236);

    public static PokeModel Get(string species)
    {
        if (Cache.TryGetValue(species, out var model)) return model;
        model = Build(species);
        Cache[species] = model;
        return model;
    }

    /// <summary>Every species that has a hand-built model (anything else uses a generic shape).</summary>
    public static readonly string[] Species =
    {
        "Turtwig", "Grotle", "Torterra", "Chimchar", "Monferno", "Infernape", "Piplup", "Prinplup", "Empoleon",
        "Starly", "Staravia", "Staraptor", "Bidoof", "Bibarel", "Shinx", "Luxio", "Luxray", "Riolu", "Lucario",
        "Gible", "Gabite", "Garchomp", "Giratina"
    };

    public static bool HasModel(string species) => Array.Exists(Species, s => s.Equals(species, StringComparison.OrdinalIgnoreCase));

    private static PokeModel Build(string species) => species.ToUpperInvariant() switch
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
        _ => Generic(species)
    };

    // ------------------------------------------------------------------ grass starters

    private static PokeModel Turtwig()
    {
        var b = new PokeBuilder("Turtwig", 0.58f);
        var green = Rgb(160, 212, 92);
        var legs = Rgb(118, 172, 70);
        var shell = Rgb(150, 98, 54);
        var rim = Rgb(214, 180, 104);
        var jaw = Rgb(244, 222, 110);

        PokeBuilder.Both(s => b.Ell(0, V(0.2f * s, 0.09f, 0.14f), V(0.1f, 0.1f, 0.11f), legs));
        PokeBuilder.Both(s => b.Ell(0, V(0.2f * s, 0.09f, -0.2f), V(0.1f, 0.1f, 0.11f), legs));
        b.Ell(0, V(0, 0.2f, -0.03f), V(0.3f, 0.16f, 0.36f), green);
        b.Ell(0, V(0, 0.29f, -0.09f), V(0.34f, 0.21f, 0.37f), shell);
        b.Torus(0, V(0, 0.2f, -0.08f), 0.31f, 0.045f, rim, sz: 1.12f);
        b.Ell(0, V(0, 0.2f, -0.42f), V(0.06f, 0.06f, 0.07f), green);

        int head = b.Bone("head", 0, V(0, 0.36f, 0.2f), Motion.Head);
        b.Ell(head, V(0, 0.52f, 0.27f), V(0.31f, 0.27f, 0.28f), green);
        b.Ell(head, V(0, 0.44f, 0.33f), V(0.27f, 0.14f, 0.24f), jaw);
        b.Ell(head, V(0, 0.62f, 0.2f), V(0.25f, 0.13f, 0.22f), PixelCanvas.Shadow(green, 0.12f));
        PokeBuilder.Both(s => b.Ell(head, V(0.045f * s, 0.5f, 0.54f), V(0.015f, 0.012f, 0.01f), Black));

        int eyes = b.Bone("eyes", head, V(0, 0.55f, 0.46f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.15f * s, 0.55f, 0.46f), V(0.62f * s, 0.1f, 1f), 0.07f, Rgb(70, 60, 50)));

        int leaves = b.Bone("leaves", head, V(0, 0.82f, 0.22f), Motion.Leaf);
        b.Limb(head, V(0, 0.74f, 0.22f), V(0, 0.86f, 0.22f), 0.03f, 0.025f, Rgb(116, 78, 46));
        PokeBuilder.Both(s => b.Ell(leaves, V(0.11f * s, 0.9f, 0.22f), V(0.13f, 0.035f, 0.08f), Rgb(92, 196, 78), V(0, 0, -30f * s)));
        return b.Model;
    }

    private static PokeModel Grotle()
    {
        var b = new PokeBuilder("Grotle", 0.74f);
        var green = Rgb(116, 166, 84);
        var legs = Rgb(96, 142, 70);
        var shell = Rgb(148, 106, 62);
        var bush = Rgb(74, 158, 74);
        var face = Rgb(236, 206, 112);

        foreach (float z in new[] { 0.2f, -0.28f })
            PokeBuilder.Both(s =>
            {
                b.Limb(0, V(0.27f * s, 0.26f, z), V(0.29f * s, 0.06f, z + 0.02f), 0.1f, 0.1f, legs);
                b.Spike(0, V(0.29f * s, 0.03f, z + 0.1f), V(0.29f * s, 0.02f, z + 0.18f), 0.03f, Claw);
            });
        b.Ell(0, V(0, 0.32f, -0.04f), V(0.38f, 0.2f, 0.5f), green);
        b.Ell(0, V(0, 0.44f, -0.08f), V(0.42f, 0.22f, 0.5f), shell);
        b.Torus(0, V(0, 0.3f, -0.07f), 0.39f, 0.04f, PixelCanvas.Shadow(shell, 0.3f), sz: 1.2f);
        // Two shrubs grow from the shell
        foreach (float x in new[] { -0.17f, 0.17f })
        {
            b.Ell(0, V(x, 0.66f, -0.02f), V(0.17f, 0.13f, 0.17f), bush);
            b.Ell(0, V(x - 0.06f, 0.72f, -0.08f), V(0.1f, 0.09f, 0.1f), PixelCanvas.Light1(bush, 0.15f));
            b.Ell(0, V(x + 0.07f, 0.7f, 0.05f), V(0.09f, 0.08f, 0.09f), PixelCanvas.Shadow(bush, 0.1f));
        }
        b.Ell(0, V(0, 0.28f, -0.56f), V(0.08f, 0.07f, 0.1f), green);

        int head = b.Bone("head", 0, V(0, 0.4f, 0.42f), Motion.Head);
        b.Ell(head, V(0, 0.46f, 0.6f), V(0.22f, 0.19f, 0.22f), green);
        b.Ell(head, V(0, 0.39f, 0.67f), V(0.2f, 0.11f, 0.18f), face);
        b.Ell(head, V(0, 0.57f, 0.58f), V(0.21f, 0.1f, 0.21f), shell);
        int eyes = b.Bone("eyes", head, V(0, 0.48f, 0.76f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.12f * s, 0.48f, 0.76f), V(0.6f * s, 0.1f, 1f), 0.05f, Rgb(80, 70, 50)));
        return b.Model;
    }

    private static PokeModel Torterra()
    {
        var b = new PokeBuilder("Torterra", 0.95f);
        var body = Rgb(96, 136, 84);
        var legs = Rgb(84, 118, 74);
        var shell = Rgb(128, 90, 56);
        var rock = Rgb(170, 174, 182);
        var leaves = Rgb(70, 150, 76);

        foreach (float z in new[] { 0.3f, -0.36f })
            PokeBuilder.Both(s =>
            {
                b.Limb(0, V(0.4f * s, 0.34f, z), V(0.43f * s, 0.08f, z), 0.15f, 0.14f, legs);
                b.Spike(0, V(0.46f * s, 0.2f, z), V(0.62f * s, 0.24f, z), 0.05f, rock);
            });
        b.Ell(0, V(0, 0.44f, 0), V(0.52f, 0.28f, 0.66f), body);
        b.Ell(0, V(0, 0.6f, -0.05f), V(0.56f, 0.24f, 0.64f), shell);
        // Rocky spikes around the shell, a tree growing from the middle
        for (int i = 0; i < 7; i++)
        {
            float a = -1.2f + i * 0.4f;
            b.Spike(0, V(MathF.Sin(a) * 0.46f, 0.72f, MathF.Cos(a) * 0.5f - 0.12f), V(MathF.Sin(a) * 0.6f, 0.95f, MathF.Cos(a) * 0.62f - 0.12f), 0.07f, rock);
        }
        b.Limb(0, V(0, 0.75f, -0.15f), V(0.04f, 1.15f, -0.18f), 0.08f, 0.06f, Rgb(118, 82, 50));
        int canopy = b.Bone("canopy", 0, V(0.03f, 1.15f, -0.18f), Motion.Leaf);
        b.Ell(canopy, V(0.03f, 1.28f, -0.18f), V(0.38f, 0.2f, 0.34f), leaves);
        b.Ell(canopy, V(-0.12f, 1.38f, -0.12f), V(0.22f, 0.13f, 0.2f), PixelCanvas.Light1(leaves, 0.18f));
        b.Ell(canopy, V(0.18f, 1.34f, -0.26f), V(0.2f, 0.12f, 0.18f), PixelCanvas.Shadow(leaves, 0.1f));

        int head = b.Bone("head", 0, V(0, 0.5f, 0.58f), Motion.Head);
        b.Ell(head, V(0, 0.54f, 0.82f), V(0.25f, 0.21f, 0.26f), body);
        b.Ell(head, V(0, 0.45f, 0.9f), V(0.22f, 0.11f, 0.2f), Rgb(214, 190, 112));
        b.Ell(head, V(0, 0.66f, 0.78f), V(0.24f, 0.1f, 0.23f), shell);
        int eyes = b.Bone("eyes", head, V(0, 0.57f, 1.0f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.13f * s, 0.57f, 1.0f), V(0.6f * s, 0.1f, 1f), 0.05f, Rgb(250, 214, 90)));
        return b.Model;
    }

    // ------------------------------------------------------------------ fire starters

    private static void Flame(PokeBuilder b, int bone, Vector3 at, float s)
    {
        b.Ell(bone, at + V(0, 0.1f * s, 0), V(0.1f * s, 0.17f * s, 0.1f * s), Rgb(242, 88, 44));
        b.Spike(bone, at + V(0, 0.18f * s, 0), at + V(0.02f * s, 0.36f * s, -0.02f * s), 0.08f * s, Rgb(242, 88, 44));
        b.Ell(bone, at + V(0, 0.1f * s, 0.02f * s), V(0.07f * s, 0.12f * s, 0.075f * s), Rgb(252, 166, 60));
        b.Ell(bone, at + V(0, 0.08f * s, 0.04f * s), V(0.04f * s, 0.07f * s, 0.045f * s), Rgb(255, 238, 130));
    }

    private static PokeModel Chimp(int stage)
    {
        bool mon = stage == 1;
        var b = new PokeBuilder(mon ? "Monferno" : "Chimchar", mon ? 0.74f : 0.6f);
        var orange = mon ? Rgb(226, 128, 56) : Rgb(242, 142, 60);
        var cream = Rgb(250, 226, 178);
        var brown = Rgb(176, 104, 54);
        float leg = mon ? 0.2f : 0.12f;

        // Legs and feet
        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.1f * s, 0.12f + leg, 0), V(0.11f * s, 0.06f, 0.02f), 0.075f, 0.07f, mon ? brown : orange);
            b.Ell(0, V(0.11f * s, 0.035f, 0.05f), V(0.075f, 0.04f, 0.11f), mon ? cream : brown);
        });
        float torso = 0.28f + leg;
        b.Ell(0, V(0, torso, 0), V(0.18f, 0.2f + (mon ? 0.04f : 0f), 0.16f), orange);
        b.Ell(0, V(0, torso - 0.01f, 0.065f), V(0.13f, 0.15f, 0.11f), cream);

        // Tail with the flame on its tip (Chimchar's flame sits right on its rear)
        int flame;
        if (mon)
        {
            b.Limb(0, V(0, torso - 0.12f, -0.12f), V(0, torso + 0.02f, -0.36f), 0.035f, 0.03f, orange);
            b.Limb(0, V(0, torso + 0.02f, -0.36f), V(0, torso + 0.22f, -0.42f), 0.03f, 0.03f, orange);
            flame = b.Bone("flame", 0, V(0, torso + 0.22f, -0.42f), Motion.Flame);
            Flame(b, flame, V(0, torso + 0.22f, -0.42f), 0.8f);
        }
        else
        {
            flame = b.Bone("flame", 0, V(0, torso - 0.1f, -0.16f), Motion.Flame);
            Flame(b, flame, V(0, torso - 0.1f, -0.16f), 1f);
        }

        // Arms with cream hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.16f * s, torso + 0.1f, 0), s < 0 ? Motion.ArmL : Motion.ArmR, s);
            b.Limb(arm, V(0.16f * s, torso + 0.1f, 0), V(0.25f * s, torso - 0.06f, 0.08f), 0.055f, 0.05f, orange);
            b.Ell(arm, V(0.26f * s, torso - 0.09f, 0.09f), V(0.06f, 0.06f, 0.06f), mon ? orange : cream);
            if (mon) b.Torus(arm, V(0.24f * s, torso - 0.03f, 0.07f), 0.05f, 0.018f, Rgb(244, 200, 70), V(0, 0, 60f * s));
        });

        int head = b.Bone("head", 0, V(0, torso + 0.18f, 0), Motion.Head);
        float hy = torso + 0.37f;
        b.Ell(head, V(0, hy, 0.02f), V(0.24f, 0.22f, 0.21f), orange);
        b.Ell(head, V(0, hy - 0.04f, 0.1f), V(0.18f, 0.15f, 0.14f), cream);
        b.Ell(head, V(0, hy - 0.02f, 0.235f), V(0.03f, 0.025f, 0.02f), Rgb(222, 72, 60));
        b.Ell(head, V(0, hy - 0.1f, 0.22f), V(0.05f, 0.015f, 0.012f), Rgb(170, 70, 60));
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.24f * s, hy + 0.02f, -0.01f), V(0.1f, 0.1f, 0.05f), orange, V(0, 20f * s, 0));
            b.Ell(head, V(0.25f * s, hy + 0.02f, 0.02f), V(0.065f, 0.065f, 0.03f), mon ? Rgb(244, 200, 70) : Rgb(236, 120, 110), V(0, 20f * s, 0));
        });
        if (mon)
        {
            // Blue face paint around the eyes, and a thin golden crest
            PokeBuilder.Both(s => b.Ell(head, V(0.085f * s, hy + 0.03f, 0.17f), V(0.07f, 0.06f, 0.05f), Rgb(80, 140, 226)));
            b.Spike(head, V(0, hy + 0.19f, 0.03f), V(0, hy + 0.3f, 0.14f), 0.035f, Rgb(244, 200, 70));
        }
        // Swirled tuft of hair on top
        b.Ell(head, V(0, hy + 0.22f, 0.0f), V(0.07f, 0.06f, 0.07f), orange);
        b.Spike(head, V(0, hy + 0.25f, 0.0f), V(0.03f, hy + 0.34f, 0.08f), 0.05f, orange);

        int eyes = b.Bone("eyes", head, V(0, hy + 0.03f, 0.18f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.085f * s, hy + 0.03f, 0.19f), V(0.35f * s, 0.05f, 1f), 0.058f, Rgb(80, 60, 50)));
        return b.Model;
    }

    private static PokeModel Infernape()
    {
        var b = new PokeBuilder("Infernape", 0.95f);
        var orange = Rgb(232, 124, 48);
        var white = Rgb(248, 244, 236);
        var gold = Rgb(244, 200, 66);
        var blue = Rgb(70, 130, 224);

        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.13f * s, 0.46f, -0.02f), V(0.2f * s, 0.24f, 0.08f), 0.08f, 0.07f, orange);
            b.Limb(0, V(0.2f * s, 0.24f, 0.08f), V(0.18f * s, 0.06f, 0.02f), 0.07f, 0.06f, orange);
            b.Ell(0, V(0.18f * s, 0.035f, 0.07f), V(0.08f, 0.04f, 0.12f), white);
            b.Torus(0, V(0.2f * s, 0.24f, 0.08f), 0.07f, 0.03f, gold, V(80, 0, 0));
        });
        b.Ell(0, V(0, 0.62f, 0), V(0.2f, 0.24f, 0.17f), white);
        b.Ell(0, V(0, 0.5f, 0.03f), V(0.17f, 0.1f, 0.14f), orange);
        b.Limb(0, V(0, 0.46f, -0.14f), V(0, 0.62f, -0.4f), 0.035f, 0.03f, orange);

        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.19f * s, 0.76f, 0), s < 0 ? Motion.ArmL : Motion.ArmR, s);
            b.Ell(arm, V(0.21f * s, 0.78f, 0), V(0.1f, 0.08f, 0.1f), gold);
            b.Limb(arm, V(0.22f * s, 0.74f, 0.02f), V(0.33f * s, 0.54f, 0.12f), 0.06f, 0.055f, white);
            b.Limb(arm, V(0.33f * s, 0.54f, 0.12f), V(0.38f * s, 0.4f, 0.2f), 0.055f, 0.05f, orange);
            b.Torus(arm, V(0.35f * s, 0.49f, 0.15f), 0.055f, 0.022f, gold, V(60, 0, 20f * s));
            b.Ell(arm, V(0.39f * s, 0.37f, 0.21f), V(0.06f, 0.06f, 0.06f), orange);
        });

        int head = b.Bone("head", 0, V(0, 0.84f, 0.02f), Motion.Head);
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.04f), V(0.18f, 0.17f, 0.17f), orange);
        b.Ell(head, V(0, hy - 0.05f, 0.1f), V(0.13f, 0.1f, 0.1f), white);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.075f * s, hy + 0.02f, 0.14f), V(0.06f, 0.045f, 0.04f), blue);
            b.Ell(head, V(0.075f * s, hy + 0.07f, 0.13f), V(0.07f, 0.02f, 0.03f), gold);
        });
        int flame = b.Bone("flame", head, V(0, hy + 0.12f, 0), Motion.Flame);
        Flame(b, flame, V(0, hy + 0.1f, -0.02f), 1.3f);
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.16f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.07f * s, hy + 0.02f, 0.17f), V(0.4f * s, 0.05f, 1f), 0.04f));
        return b.Model;
    }

    // ------------------------------------------------------------------ water starters

    private static PokeModel Piplup()
    {
        var b = new PokeBuilder("Piplup", 0.58f);
        var light = Rgb(150, 208, 244);
        var blue = Rgb(64, 132, 214);
        var dark = Rgb(40, 84, 170);
        var yellow = Rgb(252, 200, 60);

        PokeBuilder.Both(s => b.Ell(0, V(0.1f * s, 0.035f, 0.07f), V(0.08f, 0.035f, 0.1f), yellow));
        b.Ell(0, V(0, 0.28f, 0), V(0.21f, 0.24f, 0.19f), light);
        b.Ell(0, V(0, 0.27f, 0.08f), V(0.15f, 0.17f, 0.12f), White);
        PokeBuilder.Both(s => b.Ell(0, V(0.055f * s, 0.33f, 0.2f), V(0.03f, 0.03f, 0.015f), light));
        PokeBuilder.Both(s =>
        {
            int wing = b.Bone(s < 0 ? "wingL" : "wingR", 0, V(0.2f * s, 0.4f, 0), s < 0 ? Motion.WingL : Motion.WingR);
            b.Ell(wing, V(0.25f * s, 0.3f, 0.01f), V(0.05f, 0.15f, 0.09f), dark, V(0, 0, 18f * s));
        });

        int head = b.Bone("head", 0, V(0, 0.46f, 0), Motion.Head);
        float hy = 0.68f;
        b.Ell(head, V(0, hy, 0.02f), V(0.26f, 0.24f, 0.24f), blue);
        // Dark blue cap over the top and back of the head
        b.Ell(head, V(0, hy + 0.05f, -0.04f), V(0.265f, 0.22f, 0.245f), dark);
        b.Ell(head, V(0, hy - 0.05f, 0.1f), V(0.2f, 0.15f, 0.15f), light);
        b.Spike(head, V(0, hy - 0.06f, 0.2f), V(0, hy - 0.08f, 0.33f), 0.06f, yellow, 0.7f);
        b.Ell(head, V(0, hy - 0.1f, 0.21f), V(0.05f, 0.025f, 0.05f), PixelCanvas.Shadow(yellow, 0.2f));
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.2f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.09f * s, hy + 0.02f, 0.2f), V(0.45f * s, 0.05f, 1f), 0.06f, sclera: true));
        return b.Model;
    }

    private static PokeModel Prinplup()
    {
        var b = new PokeBuilder("Prinplup", 0.75f);
        var blue = Rgb(56, 104, 190);
        var light = Rgb(150, 206, 244);
        var yellow = Rgb(250, 200, 62);

        PokeBuilder.Both(s => b.Ell(0, V(0.11f * s, 0.035f, 0.08f), V(0.09f, 0.035f, 0.12f), yellow));
        b.Ell(0, V(0, 0.38f, 0), V(0.23f, 0.34f, 0.2f), blue);
        b.Ell(0, V(0, 0.36f, 0.08f), V(0.17f, 0.25f, 0.13f), light);
        foreach (var (x, y) in new[] { (-0.07f, 0.44f), (0.07f, 0.44f), (0f, 0.3f) })
            b.Ell(0, V(x, y, 0.2f), V(0.03f, 0.03f, 0.015f), White);
        PokeBuilder.Both(s =>
        {
            int wing = b.Bone(s < 0 ? "wingL" : "wingR", 0, V(0.21f * s, 0.54f, 0), s < 0 ? Motion.WingL : Motion.WingR);
            b.Ell(wing, V(0.29f * s, 0.4f, 0.02f), V(0.05f, 0.2f, 0.1f), Rgb(40, 80, 160), V(0, 0, 25f * s));
        });

        int head = b.Bone("head", 0, V(0, 0.66f, 0), Motion.Head);
        float hy = 0.84f;
        b.Ell(head, V(0, hy, 0.02f), V(0.19f, 0.18f, 0.18f), blue);
        b.Ell(head, V(0, hy - 0.04f, 0.09f), V(0.14f, 0.1f, 0.1f), light);
        // The beak grows up into a two-pronged crest
        b.Spike(head, V(0, hy - 0.05f, 0.16f), V(0, hy - 0.07f, 0.29f), 0.05f, yellow, 0.7f);
        b.Limb(head, V(0, hy - 0.02f, 0.17f), V(0, hy + 0.14f, 0.16f), 0.03f, 0.025f, yellow);
        PokeBuilder.Both(s => b.Spike(head, V(0, hy + 0.14f, 0.16f), V(0.09f * s, hy + 0.24f, 0.12f), 0.03f, yellow));
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.15f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.08f * s, hy + 0.02f, 0.15f), V(0.5f * s, 0.05f, 1f), 0.045f, sclera: true));
        return b.Model;
    }

    private static PokeModel Empoleon()
    {
        var b = new PokeBuilder("Empoleon", 0.95f);
        var navy = Rgb(36, 52, 104);
        var light = Rgb(110, 170, 226);
        var gold = Rgb(246, 204, 64);
        var steel = Rgb(120, 160, 206);

        PokeBuilder.Both(s => b.Ell(0, V(0.13f * s, 0.04f, 0.09f), V(0.1f, 0.04f, 0.14f), gold));
        b.Ell(0, V(0, 0.52f, 0), V(0.28f, 0.48f, 0.24f), navy);
        b.Ell(0, V(0, 0.5f, 0.1f), V(0.19f, 0.38f, 0.16f), White);
        foreach (var (x, y) in new[] { (-0.08f, 0.64f), (0.08f, 0.64f), (0f, 0.48f), (-0.07f, 0.34f), (0.07f, 0.34f) })
            b.Ell(0, V(x, y, 0.25f), V(0.035f, 0.035f, 0.015f), navy);
        PokeBuilder.Both(s =>
        {
            int wing = b.Bone(s < 0 ? "wingL" : "wingR", 0, V(0.26f * s, 0.8f, 0), s < 0 ? Motion.WingL : Motion.WingR);
            b.Ell(wing, V(0.36f * s, 0.55f, 0.02f), V(0.06f, 0.3f, 0.13f), steel, V(0, 0, 22f * s));
            b.Ell(wing, V(0.35f * s, 0.56f, 0.03f), V(0.065f, 0.24f, 0.08f), light, V(0, 0, 22f * s));
        });

        int head = b.Bone("head", 0, V(0, 0.98f, 0), Motion.Head);
        float hy = 1.1f;
        b.Ell(head, V(0, hy, 0.02f), V(0.16f, 0.15f, 0.16f), navy);
        b.Ell(head, V(0, hy - 0.03f, 0.08f), V(0.12f, 0.09f, 0.09f), light);
        // Golden trident crest rising from the beak
        b.Spike(head, V(0, hy - 0.04f, 0.14f), V(0, hy - 0.07f, 0.28f), 0.045f, gold, 0.7f);
        b.Limb(head, V(0, hy - 0.02f, 0.15f), V(0, hy + 0.2f, 0.12f), 0.035f, 0.03f, gold);
        PokeBuilder.Both(s =>
        {
            b.Limb(head, V(0.02f * s, hy + 0.02f, 0.14f), V(0.12f * s, hy + 0.14f, 0.1f), 0.028f, 0.022f, gold);
            b.Spike(head, V(0.12f * s, hy + 0.14f, 0.1f), V(0.16f * s, hy + 0.26f, 0.08f), 0.025f, gold);
        });
        b.Spike(head, V(0, hy + 0.2f, 0.12f), V(0, hy + 0.32f, 0.1f), 0.035f, gold);
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.14f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.07f * s, hy + 0.02f, 0.14f), V(0.55f * s, 0.05f, 1f), 0.035f, sclera: true));
        return b.Model;
    }

    // ------------------------------------------------------------------ Starly line

    private static PokeModel Bird(int stage)
    {
        string name = stage switch { 0 => "Starly", 1 => "Staravia", _ => "Staraptor" };
        var b = new PokeBuilder(name, stage switch { 0 => 0.52f, 1 => 0.68f, _ => 0.86f }).Hover();
        var gray = stage == 2 ? Rgb(104, 100, 108) : Rgb(126, 116, 112);
        var dark = Rgb(52, 48, 56);
        var orange = Rgb(248, 150, 44);
        float s1 = 1f + stage * 0.2f;

        // Legs, body and belly
        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.07f * s, 0.14f * s1, 0.02f), V(0.08f * s, 0.03f, 0.04f), 0.022f, 0.018f, orange);
            b.Ell(0, V(0.08f * s, 0.02f, 0.07f), V(0.035f, 0.015f, 0.05f), orange);
        });
        b.Ell(0, V(0, 0.28f * s1, 0), V(0.2f * s1, 0.18f * s1, 0.24f * s1), gray);
        b.Ell(0, V(0, 0.25f * s1, 0.08f * s1), V(0.15f * s1, 0.14f * s1, 0.15f * s1), White);
        foreach (var (x, y) in new[] { (-0.06f, 0.24f), (0.05f, 0.2f), (0f, 0.3f), (0.07f, 0.28f) })
            b.Ell(0, V(x * s1, y * s1, 0.21f * s1), V(0.02f * s1, 0.02f * s1, 0.01f), Rgb(170, 164, 164));

        // Fanned tail
        int tail = b.Bone("tail", 0, V(0, 0.28f * s1, -0.2f * s1), Motion.Tail);
        foreach (float a in new[] { -20f, 0f, 20f })
            b.Ell(tail, V(MathF.Sin(a * MathF.PI / 180f) * 0.08f * s1, 0.26f * s1, -0.34f * s1), V(0.05f * s1, 0.02f * s1, 0.14f * s1), a == 0 ? dark : White, V(-10f, a, 0));

        // Wings folded at the sides
        PokeBuilder.Both(s =>
        {
            int wing = b.Bone(s < 0 ? "wingL" : "wingR", 0, V(0.17f * s * s1, 0.36f * s1, 0.02f), s < 0 ? Motion.WingL : Motion.WingR);
            b.Ell(wing, V(0.2f * s * s1, 0.28f * s1, -0.05f * s1), V(0.05f * s1, 0.13f * s1, 0.2f * s1), dark, V(-15f, 0, 12f * s));
            b.Ell(wing, V(0.21f * s * s1, 0.27f * s1, -0.02f * s1), V(0.04f * s1, 0.08f * s1, 0.12f * s1), gray, V(-15f, 0, 12f * s));
        });

        int head = b.Bone("head", 0, V(0, 0.4f * s1, 0.08f * s1), Motion.Head);
        float hy = 0.54f * s1;
        b.Ell(head, V(0, hy, 0.1f * s1), V(0.15f * s1, 0.14f * s1, 0.14f * s1), dark);
        // White face mask around the eyes
        b.Ell(head, V(0, hy - 0.02f * s1, 0.17f * s1), V(0.13f * s1, 0.09f * s1, 0.09f * s1), White);
        b.Spike(head, V(0, hy - 0.02f * s1, 0.24f * s1), V(0, hy - 0.06f * s1, 0.36f * s1), 0.045f * s1, orange, 0.8f);
        if (stage == 0)
        {
            b.Spike(head, V(0, hy + 0.12f * s1, 0.08f * s1), V(0, hy + 0.2f * s1, 0.16f * s1), 0.035f * s1, dark);
        }
        else
        {
            // Crest: curled on Staravia, long and red-tipped on Staraptor
            b.Limb(head, V(0, hy + 0.1f * s1, 0.04f * s1), V(0, hy + 0.26f * s1, 0.14f * s1), 0.04f * s1, 0.03f * s1, dark);
            b.Limb(head, V(0, hy + 0.26f * s1, 0.14f * s1), V(0, hy + (stage == 2 ? 0.18f : 0.24f) * s1, 0.28f * s1), 0.03f * s1, 0.022f * s1, stage == 2 ? Rgb(214, 60, 60) : dark);
        }
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f * s1, 0.2f * s1), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.08f * s * s1, hy + 0.02f * s1, 0.2f * s1), V(0.55f * s, 0.05f, 1f), 0.035f * s1,
            stage == 2 ? Rgb(236, 90, 60) : Rgb(248, 150, 44)));
        return b.Model;
    }

    // ------------------------------------------------------------------ Bidoof line

    private static PokeModel Bidoof()
    {
        var b = new PokeBuilder("Bidoof", 0.6f);
        var brown = Rgb(176, 122, 68);
        var dark = Rgb(122, 80, 44);
        var tan = Rgb(232, 202, 148);

        foreach (float z in new[] { 0.16f, -0.16f })
            PokeBuilder.Both(s => b.Ell(0, V(0.17f * s, 0.05f, z), V(0.08f, 0.05f, 0.09f), dark));
        b.Ell(0, V(0, 0.3f, -0.02f), V(0.34f, 0.3f, 0.34f), brown);
        b.Ell(0, V(0, 0.2f, -0.36f), V(0.07f, 0.06f, 0.06f), dark);

        int head = b.Bone("head", 0, V(0, 0.3f, 0.1f), Motion.Head);
        // Darker ruff framing the face, tan muzzle, red nose and big buck teeth
        b.Ell(head, V(0, 0.36f, 0.14f), V(0.3f, 0.26f, 0.22f), dark);
        b.Ell(head, V(0, 0.36f, 0.19f), V(0.26f, 0.23f, 0.19f), brown);
        b.Ell(head, V(0, 0.28f, 0.3f), V(0.17f, 0.12f, 0.1f), tan);
        b.Ell(head, V(0, 0.34f, 0.39f), V(0.05f, 0.04f, 0.035f), Rgb(204, 64, 56));
        PokeBuilder.Both(s => b.Ell(head, V(0.028f * s, 0.23f, 0.37f), V(0.024f, 0.045f, 0.012f), White));
        PokeBuilder.Both(s => b.Ell(head, V(0.2f * s, 0.58f, 0.04f), V(0.08f, 0.08f, 0.045f), dark));
        int eyes = b.Bone("eyes", head, V(0, 0.44f, 0.33f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.12f * s, 0.44f, 0.33f), V(0.4f * s, 0.1f, 1f), 0.035f));
        return b.Model;
    }

    private static PokeModel Bibarel()
    {
        var b = new PokeBuilder("Bibarel", 0.78f);
        var brown = Rgb(156, 104, 62);
        var tan = Rgb(226, 196, 142);
        var dark = Rgb(98, 64, 40);

        PokeBuilder.Both(s => b.Ell(0, V(0.14f * s, 0.05f, 0.12f), V(0.09f, 0.05f, 0.12f), dark));
        b.Ell(0, V(0, 0.36f, 0), V(0.3f, 0.36f, 0.27f), brown);
        b.Ell(0, V(0, 0.32f, 0.1f), V(0.2f, 0.26f, 0.18f), tan);
        // Paddle tail with dark bands
        int tail = b.Bone("tail", 0, V(0, 0.18f, -0.22f), Motion.Tail);
        b.Ell(tail, V(0, 0.16f, -0.42f), V(0.17f, 0.05f, 0.24f), brown, V(-20f, 0, 0));
        foreach (float z in new[] { -0.36f, -0.46f, -0.56f })
            b.Ell(tail, V(0, 0.16f + (z + 0.42f) * -0.36f, z), V(0.15f, 0.052f, 0.022f), dark, V(-20f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.24f * s, 0.46f, 0.06f), s < 0 ? Motion.ArmL : Motion.ArmR);
            b.Limb(arm, V(0.24f * s, 0.46f, 0.06f), V(0.26f * s, 0.32f, 0.18f), 0.05f, 0.045f, brown);
        });

        int head = b.Bone("head", 0, V(0, 0.62f, 0.02f), Motion.Head);
        b.Ell(head, V(0, 0.74f, 0.04f), V(0.22f, 0.19f, 0.19f), brown);
        b.Ell(head, V(0, 0.69f, 0.15f), V(0.15f, 0.1f, 0.09f), tan);
        b.Ell(head, V(0, 0.74f, 0.23f), V(0.04f, 0.03f, 0.03f), dark);
        PokeBuilder.Both(s => b.Ell(head, V(0.024f * s, 0.63f, 0.21f), V(0.02f, 0.04f, 0.01f), White));
        PokeBuilder.Both(s => b.Ell(head, V(0.17f * s, 0.9f, 0.0f), V(0.06f, 0.06f, 0.035f), dark));
        int eyes = b.Bone("eyes", head, V(0, 0.8f, 0.19f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.09f * s, 0.8f, 0.19f), V(0.45f * s, 0.1f, 1f), 0.028f));
        return b.Model;
    }

    // ------------------------------------------------------------------ Shinx line

    private static void StarTip(PokeBuilder b, int bone, Vector3 at, float r, Color color)
    {
        foreach (var d in new[] { V(1, 0, 0), V(-1, 0, 0), V(0, 1, 0), V(0, -1, 0) })
            b.Spike(bone, at, at + d * r, r * 0.45f, color, 0.5f);
    }

    private static PokeModel Cat(int stage)
    {
        string name = stage switch { 0 => "Shinx", 1 => "Luxio", _ => "Luxray" };
        var b = new PokeBuilder(name, stage switch { 0 => 0.58f, 1 => 0.74f, _ => 0.92f });
        var blue = Rgb(98, 176, 236);
        var black = Rgb(48, 48, 62);
        var yellow = Rgb(252, 212, 60);
        float s1 = 1f + stage * 0.22f;
        float legLen = 0.18f + stage * 0.08f;

        // Legs: blue in front, black behind (Luxray's are mostly black with blue paws)
        foreach (var (z, front) in new[] { (0.14f, true), (-0.18f, false) })
            PokeBuilder.Both(s =>
            {
                var upper = stage == 2 ? black : (front ? blue : black);
                b.Limb(0, V(0.1f * s * s1, legLen + 0.06f, z * s1), V(0.1f * s * s1, 0.05f, (z + 0.02f) * s1), 0.055f * s1, 0.05f * s1, upper);
                b.Ell(0, V(0.1f * s * s1, 0.04f, (z + 0.04f) * s1), V(0.06f * s1, 0.04f * s1, 0.07f * s1), front || stage == 2 ? blue : black);
                if (front) b.Torus(0, V(0.1f * s * s1, 0.15f + stage * 0.05f, (z + 0.01f) * s1), 0.055f * s1, 0.016f * s1, yellow);
            });
        float by = legLen + 0.12f;
        b.Ell(0, V(0, by, 0.05f * s1), V(0.16f * s1, 0.15f * s1, 0.19f * s1), stage == 2 ? black : blue);
        b.Ell(0, V(0, by, -0.16f * s1), V(0.15f * s1, 0.14f * s1, 0.18f * s1), black);
        if (stage == 2) b.Ell(0, V(0, by - 0.06f, 0.0f), V(0.12f * s1, 0.08f * s1, 0.26f * s1), blue);

        // Tail with the yellow star
        int tail = b.Bone("tail", 0, V(0, by + 0.04f, -0.3f * s1), Motion.Tail);
        b.Limb(tail, V(0, by + 0.04f, -0.3f * s1), V(0, by + 0.2f * s1, -0.46f * s1), 0.025f, 0.02f, black);
        b.Limb(tail, V(0, by + 0.2f * s1, -0.46f * s1), V(0, by + 0.34f * s1, -0.42f * s1), 0.02f, 0.02f, black);
        StarTip(b, tail, V(0, by + 0.38f * s1, -0.42f * s1), 0.09f * s1, yellow);

        int head = b.Bone("head", 0, V(0, by + 0.1f, 0.14f * s1), Motion.Head);
        float hy = by + 0.2f * s1;
        float hr = stage == 0 ? 0.22f : 0.2f * s1;
        b.Ell(head, V(0, hy, 0.2f * s1), V(hr, hr * 0.92f, hr * 0.95f), blue);
        b.Ell(head, V(0, hy - hr * 0.35f, 0.2f * s1 + hr * 0.65f), V(hr * 0.35f, hr * 0.25f, hr * 0.22f), PixelCanvas.Light1(blue, 0.25f));
        b.Ell(head, V(0, hy - hr * 0.2f, 0.2f * s1 + hr * 0.92f), V(0.025f, 0.02f, 0.015f), black);
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
            int ear = b.Bone(s < 0 ? "earL" : "earR", head, V(hr * 0.62f * s, hy + hr * 0.6f, 0.16f * s1), Motion.Ear, s);
            b.Ell(ear, V(hr * 0.78f * s, hy + hr * 0.95f, 0.16f * s1), V(0.07f * s1, 0.12f * s1, 0.04f), blue, V(0, 0, -25f * s));
            b.Ell(ear, V(hr * 0.8f * s, hy + hr * 0.95f, 0.19f * s1), V(0.045f * s1, 0.08f * s1, 0.02f), black, V(0, 0, -25f * s));
            StarTip(b, ear, V(hr * 0.8f * s, hy + hr * 0.97f, 0.21f * s1), 0.04f * s1, yellow);
        });
        int eyes = b.Bone("eyes", head, V(0, hy + hr * 0.1f, 0.2f * s1 + hr * 0.8f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(hr * 0.42f * s, hy + hr * 0.1f, 0.2f * s1 + hr * 0.82f), V(0.55f * s, 0.05f, 1f), 0.055f * s1,
            stage == 2 ? Rgb(250, 200, 50) : Rgb(250, 212, 60), pupil: stage == 2 ? Rgb(200, 40, 40) : null));
        return b.Model;
    }

    // ------------------------------------------------------------------ Riolu line

    private static PokeModel Riolu()
    {
        var b = new PokeBuilder("Riolu", 0.6f);
        var blue = Rgb(84, 148, 222);
        var black = Rgb(44, 46, 62);
        var red = Rgb(214, 56, 60);

        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.08f * s, 0.26f, 0), V(0.09f * s, 0.06f, 0.02f), 0.06f, 0.055f, black);
            b.Ell(0, V(0.09f * s, 0.035f, 0.05f), V(0.06f, 0.035f, 0.09f), black);
        });
        b.Ell(0, V(0, 0.34f, 0), V(0.15f, 0.17f, 0.13f), blue);
        b.Ell(0, V(0, 0.26f, 0.0f), V(0.14f, 0.07f, 0.12f), black);
        int tail = b.Bone("tail", 0, V(0, 0.26f, -0.12f), Motion.Tail);
        b.Ell(tail, V(0, 0.26f, -0.2f), V(0.05f, 0.06f, 0.09f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.14f * s, 0.43f, 0), s < 0 ? Motion.ArmL : Motion.ArmR, s);
            b.Limb(arm, V(0.14f * s, 0.43f, 0), V(0.21f * s, 0.3f, 0.07f), 0.045f, 0.04f, blue);
            b.Ell(arm, V(0.22f * s, 0.27f, 0.08f), V(0.05f, 0.05f, 0.05f), black);
        });

        int head = b.Bone("head", 0, V(0, 0.5f, 0), Motion.Head);
        float hy = 0.64f;
        b.Ell(head, V(0, hy, 0.02f), V(0.19f, 0.17f, 0.17f), blue);
        b.Ell(head, V(0, hy - 0.06f, 0.12f), V(0.08f, 0.06f, 0.08f), PixelCanvas.Light1(blue, 0.2f));
        b.Ell(head, V(0, hy - 0.04f, 0.19f), V(0.02f, 0.016f, 0.012f), black);
        // Black mask across the eyes
        b.Ell(head, V(0, hy + 0.02f, 0.06f), V(0.195f, 0.06f, 0.14f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Bone(s < 0 ? "earL" : "earR", head, V(0.1f * s, hy + 0.12f, 0), Motion.Ear, s);
            b.Spike(ear, V(0.1f * s, hy + 0.1f, 0), V(0.2f * s, hy + 0.34f, -0.04f), 0.06f, blue, 0.5f);
            b.Spike(ear, V(0.105f * s, hy + 0.12f, 0.012f), V(0.19f * s, hy + 0.3f, -0.02f), 0.035f, black, 0.4f);
        });
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.16f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.075f * s, hy + 0.02f, 0.17f), V(0.4f * s, 0.05f, 1f), 0.042f, red));
        return b.Model;
    }

    private static PokeModel Lucario()
    {
        var b = new PokeBuilder("Lucario", 0.92f);
        var blue = Rgb(74, 132, 212);
        var black = Rgb(42, 44, 60);
        var cream = Rgb(236, 220, 162);
        var red = Rgb(214, 56, 60);

        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.1f * s, 0.46f, 0), V(0.12f * s, 0.24f, 0.04f), 0.075f, 0.06f, black);
            b.Limb(0, V(0.12f * s, 0.24f, 0.04f), V(0.12f * s, 0.06f, 0.0f), 0.055f, 0.05f, blue);
            b.Ell(0, V(0.12f * s, 0.035f, 0.05f), V(0.06f, 0.035f, 0.1f), blue);
        });
        b.Ell(0, V(0, 0.62f, 0), V(0.16f, 0.22f, 0.13f), blue);
        b.Ell(0, V(0, 0.64f, 0.06f), V(0.12f, 0.16f, 0.1f), cream);
        b.Spike(0, V(0, 0.66f, 0.15f), V(0, 0.68f, 0.26f), 0.035f, Claw);
        b.Ell(0, V(0, 0.46f, 0), V(0.15f, 0.07f, 0.12f), black);
        int tail = b.Bone("tail", 0, V(0, 0.44f, -0.12f), Motion.Tail);
        b.Ell(tail, V(0, 0.4f, -0.24f), V(0.05f, 0.08f, 0.12f), blue, V(-30f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.15f * s, 0.78f, 0), s < 0 ? Motion.ArmL : Motion.ArmR, s);
            b.Limb(arm, V(0.15f * s, 0.78f, 0), V(0.25f * s, 0.56f, 0.06f), 0.05f, 0.045f, black);
            b.Ell(arm, V(0.26f * s, 0.52f, 0.07f), V(0.05f, 0.055f, 0.05f), black);
            b.Spike(arm, V(0.26f * s, 0.54f, 0.02f), V(0.3f * s, 0.56f, -0.08f), 0.025f, Claw);
        });

        int head = b.Bone("head", 0, V(0, 0.84f, 0), Motion.Head);
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.02f), V(0.15f, 0.14f, 0.15f), blue);
        b.Ell(head, V(0, hy - 0.05f, 0.13f), V(0.07f, 0.05f, 0.08f), black);
        b.Ell(head, V(0, hy + 0.02f, 0.06f), V(0.155f, 0.05f, 0.12f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Bone(s < 0 ? "earL" : "earR", head, V(0.08f * s, hy + 0.1f, 0), Motion.Ear, s);
            b.Spike(ear, V(0.08f * s, hy + 0.08f, 0), V(0.14f * s, hy + 0.28f, -0.03f), 0.05f, blue, 0.5f);
            // The black appendages that hang from the back of its head
            b.Limb(head, V(0.06f * s, hy, -0.1f), V(0.1f * s, hy - 0.16f, -0.18f), 0.025f, 0.02f, black);
            b.Limb(head, V(0.04f * s, hy - 0.04f, -0.1f), V(0.07f * s, hy - 0.22f, -0.14f), 0.022f, 0.018f, black);
        });
        int eyes = b.Bone("eyes", head, V(0, hy + 0.02f, 0.14f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.065f * s, hy + 0.02f, 0.14f), V(0.45f * s, 0.05f, 1f), 0.035f, red));
        return b.Model;
    }

    // ------------------------------------------------------------------ Gible line

    private static PokeModel Gible()
    {
        var b = new PokeBuilder("Gible", 0.56f);
        var navy = Rgb(82, 104, 164);
        var red = Rgb(218, 82, 70);
        var yellow = Rgb(250, 206, 70);

        PokeBuilder.Both(s => b.Ell(0, V(0.1f * s, 0.06f, 0.03f), V(0.08f, 0.06f, 0.1f), navy));
        b.Ell(0, V(0, 0.27f, -0.02f), V(0.23f, 0.24f, 0.22f), navy);
        b.Ell(0, V(0, 0.24f, 0.08f), V(0.17f, 0.17f, 0.15f), red);
        int tail = b.Bone("tail", 0, V(0, 0.18f, -0.18f), Motion.Tail);
        b.Ell(tail, V(0, 0.16f, -0.3f), V(0.06f, 0.06f, 0.12f), navy);
        b.Spike(tail, V(0, 0.2f, -0.36f), V(0, 0.3f, -0.42f), 0.04f, navy, 0.4f);
        PokeBuilder.Both(s => b.Ell(0, V(0.2f * s, 0.3f, 0.06f), V(0.05f, 0.07f, 0.06f), navy));

        int head = b.Bone("head", 0, V(0, 0.4f, 0.02f), Motion.Head);
        float hy = 0.58f;
        b.Ell(head, V(0, hy, 0.06f), V(0.28f, 0.2f, 0.27f), navy);
        int jaw = b.Bone("jaw", head, V(0, hy - 0.08f, -0.05f), Motion.Jaw);
        b.Ell(jaw, V(0, hy - 0.12f, 0.12f), V(0.25f, 0.08f, 0.2f), red);
        b.Ell(head, V(0, hy - 0.05f, 0.14f), V(0.24f, 0.05f, 0.19f), Rgb(120, 40, 50));
        for (int i = -2; i <= 2; i++)
            b.Spike(head, V(i * 0.06f, hy - 0.04f, 0.27f - MathF.Abs(i) * 0.02f), V(i * 0.06f, hy - 0.1f, 0.28f - MathF.Abs(i) * 0.02f), 0.018f, Claw);
        // Dorsal fin with a notch
        b.Spike(head, V(0, hy + 0.14f, 0.0f), V(0, hy + 0.36f, -0.06f), 0.1f, navy, 0.35f);
        b.Ell(head, V(0, hy + 0.3f, 0.0f), V(0.02f, 0.04f, 0.04f), Rgb(110, 136, 196));
        PokeBuilder.Both(s => b.Ell(head, V(0.2f * s, hy + 0.08f, 0.02f), V(0.05f, 0.05f, 0.04f), Rgb(110, 136, 196)));
        int eyes = b.Bone("eyes", head, V(0, hy + 0.06f, 0.24f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.12f * s, hy + 0.06f, 0.24f), V(0.45f * s, 0.2f, 1f), 0.045f, yellow));
        return b.Model;
    }

    private static PokeModel Garchomp(bool big)
    {
        var b = new PokeBuilder(big ? "Garchomp" : "Gabite", big ? 0.95f : 0.76f);
        var navy = big ? Rgb(64, 80, 140) : Rgb(84, 90, 162);
        var red = Rgb(212, 70, 64);
        var yellow = Rgb(250, 206, 70);

        PokeBuilder.Both(s =>
        {
            b.Limb(0, V(0.12f * s, 0.46f, -0.02f), V(0.16f * s, 0.22f, 0.06f), 0.1f, 0.08f, navy);
            b.Limb(0, V(0.16f * s, 0.22f, 0.06f), V(0.15f * s, 0.05f, 0.0f), 0.07f, 0.06f, navy);
            b.Ell(0, V(0.15f * s, 0.035f, 0.05f), V(0.07f, 0.035f, 0.1f), navy);
            b.Spike(0, V(0.2f * s, 0.28f, 0.0f), V(0.3f * s, 0.3f, -0.08f), 0.035f, Claw);
        });
        b.Ell(0, V(0, 0.62f, 0), V(0.2f, 0.26f, 0.17f), navy);
        b.Ell(0, V(0, 0.58f, 0.07f), V(0.14f, 0.22f, 0.12f), red);
        int tail = b.Bone("tail", 0, V(0, 0.44f, -0.12f), Motion.Tail);
        b.Limb(tail, V(0, 0.44f, -0.12f), V(0, 0.2f, -0.42f), 0.09f, 0.05f, navy);
        b.Spike(tail, V(0, 0.34f, -0.26f), V(0, 0.46f, -0.36f), 0.05f, navy, 0.35f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Bone(s < 0 ? "armL" : "armR", 0, V(0.18f * s, 0.78f, 0), s < 0 ? Motion.ArmL : Motion.ArmR, s);
            b.Limb(arm, V(0.18f * s, 0.78f, 0), V(0.3f * s, 0.6f, 0.08f), 0.06f, 0.05f, navy);
            // Scythe-like fins on the arms
            b.Ell(arm, V(0.36f * s, 0.62f, 0.02f), V(0.03f, 0.2f, 0.12f), navy, V(20f, 0, 25f * s));
            b.Spike(arm, V(0.32f * s, 0.54f, 0.12f), V(0.36f * s, 0.46f, 0.2f), 0.025f, Claw);
        });

        int head = b.Bone("head", 0, V(0, 0.86f, 0), Motion.Head);
        float hy = 0.98f;
        // Streamlined head with fins on the sides and the yellow star on the snout
        b.Ell(head, V(0, hy, 0.08f), V(0.14f, 0.1f, 0.22f), navy);
        int jaw = b.Bone("jaw", head, V(0, hy - 0.04f, 0.0f), Motion.Jaw);
        b.Ell(jaw, V(0, hy - 0.06f, 0.14f), V(0.11f, 0.04f, 0.14f), red);
        PokeBuilder.Both(s => b.Ell(head, V(0.18f * s, hy + 0.02f, 0.0f), V(0.12f, 0.03f, 0.12f), navy, V(0, 30f * s, -20f * s)));
        if (big) b.Ell(head, V(0, hy + 0.08f, 0.14f), V(0.04f, 0.02f, 0.04f), yellow);
        b.Spike(head, V(0, hy + 0.06f, -0.06f), V(0, hy + 0.12f, -0.26f), 0.05f, navy, 0.4f);
        int eyes = b.Bone("eyes", head, V(0, hy + 0.03f, 0.2f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.09f * s, hy + 0.03f, 0.2f), V(0.7f * s, 0.05f, 1f), 0.03f, yellow));
        return b.Model;
    }

    // ------------------------------------------------------------------ Giratina

    private static PokeModel Giratina()
    {
        var b = new PokeBuilder("Giratina", 1f).Hover();
        var gray = Rgb(168, 168, 180);
        var black = Rgb(40, 36, 50);
        var red = Rgb(214, 56, 62);
        var gold = Rgb(236, 196, 72);

        // Six legs under a long body striped black and red
        foreach (float z in new[] { 0.2f, -0.05f, -0.3f })
            PokeBuilder.Both(s =>
            {
                b.Limb(0, V(0.16f * s, 0.34f, z), V(0.24f * s, 0.06f, z + 0.06f), 0.06f, 0.045f, gray);
                b.Spike(0, V(0.24f * s, 0.05f, z + 0.08f), V(0.25f * s, 0.02f, z + 0.18f), 0.03f, gold);
            });
        b.Ell(0, V(0, 0.44f, -0.05f), V(0.24f, 0.2f, 0.46f), gray);
        foreach (float z in new[] { 0.12f, -0.02f, -0.16f, -0.3f })
        {
            b.Torus(0, V(0, 0.44f, z), 0.2f, 0.028f, black, V(90, 0, 0), 1.1f, 0.95f);
            b.Torus(0, V(0, 0.44f, z - 0.05f), 0.21f, 0.016f, red, V(90, 0, 0), 1.1f, 0.95f);
        }
        int tail = b.Bone("tail", 0, V(0, 0.44f, -0.45f), Motion.Tail);
        b.Limb(tail, V(0, 0.44f, -0.45f), V(0, 0.3f, -0.8f), 0.1f, 0.03f, gray);

        // Shadowy wings with red spikes
        PokeBuilder.Both(s =>
        {
            int wing = b.Bone(s < 0 ? "wingL" : "wingR", 0, V(0.2f * s, 0.62f, 0.05f), s < 0 ? Motion.WingL : Motion.WingR);
            for (int i = 0; i < 3; i++)
            {
                float a = 30f + i * 26f;
                var tip = V(s * (0.35f + i * 0.14f), 0.62f + 0.36f - i * 0.2f, -0.1f - i * 0.1f);
                b.Limb(wing, V(0.22f * s, 0.64f, 0.02f - i * 0.05f), tip, 0.06f, 0.025f, black);
                b.Spike(wing, tip, tip + V(0.1f * s, 0.06f, -0.04f), 0.04f, red);
                b.Ell(wing, (V(0.22f * s, 0.64f, 0.02f) + tip) / 2f, V(0.05f, 0.1f, 0.03f), black, V(0, 0, -a * s));
            }
        });

        int head = b.Bone("head", 0, V(0, 0.6f, 0.36f), Motion.Head);
        b.Limb(head, V(0, 0.56f, 0.34f), V(0, 0.74f, 0.52f), 0.12f, 0.1f, gray);
        b.Torus(head, V(0, 0.66f, 0.44f), 0.12f, 0.035f, gold, V(-50, 0, 0));
        b.Ell(head, V(0, 0.8f, 0.6f), V(0.13f, 0.11f, 0.15f), gray);
        // Golden crest and jaw guards
        b.Ell(head, V(0, 0.88f, 0.56f), V(0.14f, 0.05f, 0.15f), gold, V(-15, 0, 0));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.08f * s, 0.9f, 0.5f), V(0.2f * s, 1.04f, 0.4f), 0.04f, gold);
            b.Spike(head, V(0.1f * s, 0.74f, 0.66f), V(0.16f * s, 0.66f, 0.74f), 0.03f, gold);
        });
        b.Ell(head, V(0, 0.76f, 0.72f), V(0.07f, 0.04f, 0.06f), black);
        int eyes = b.Bone("eyes", head, V(0, 0.84f, 0.7f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.07f * s, 0.84f, 0.7f), V(0.6f * s, 0.1f, 1f), 0.03f, red));
        return b.Model;
    }

    private static PokeModel Generic(string species)
    {
        var b = new PokeBuilder(species, 0.65f);
        var col = Rgb(168, 168, 120);
        b.Ell(0, V(0, 0.28f, 0), V(0.26f, 0.26f, 0.24f), col);
        int head = b.Bone("head", 0, V(0, 0.46f, 0), Motion.Head);
        b.Ell(head, V(0, 0.64f, 0.04f), V(0.2f, 0.19f, 0.18f), col);
        int eyes = b.Bone("eyes", head, V(0, 0.66f, 0.18f), Motion.Eyes);
        PokeBuilder.Both(s => b.Eye(eyes, V(0.08f * s, 0.66f, 0.19f), V(0.4f * s, 0.05f, 1f), 0.04f));
        return b.Model;
    }
}
