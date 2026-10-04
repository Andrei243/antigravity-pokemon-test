using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The parts every generated body shares (plan 03 · D5): heads and faces, ears, what grows on the head and the back,
/// tails, wings, limbs and patterns. Each places itself on the surface of the ellipsoid it grows from, so it fits
/// whatever proportions the genome chose.
/// </summary>
internal sealed partial class PokemonSculptor
{
    /// <summary>An ellipsoid a part grows from: its bone, centre and radii (axis-aligned).</summary>
    private readonly record struct Shape(int Bone, Vector3 C, Vector3 R)
    {
        public (Vector3 P, Vector3 N) At(float yaw, float pitch) => On(C, R, yaw, pitch);
    }

    private static Vector3 V(float x, float y, float z) => new(x, y, z);

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>
    /// The point of an ellipsoid's surface in the direction given by <paramref name="yaw"/> (from +Z toward +X) and
    /// <paramref name="pitch"/> (up), and the surface's normal there.
    /// </summary>
    private static (Vector3 P, Vector3 N) On(Vector3 c, Vector3 r, float yaw, float pitch)
    {
        var u = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        return (c + r * u, Vector3.Normalize(u / r));
    }

    private static readonly Color Ink = Rgb(44, 42, 56);
    private static readonly Color Ivory = Rgb(242, 238, 226);
    private static readonly Color Gold = Rgb(240, 198, 72);
    private static readonly Color Ice = Rgb(184, 228, 244);

    /// <summary>A colour that shows against the body, for parts that should stand out.</summary>
    private Color Accent => g.Second;

    /// <summary>The colour of hands and feet: in a colour of their own on gloved and booted species, dark on socked ones.</summary>
    private Color Extremity => g.Gloves ? (PokemonGenomes.Luma(g.Main) > 150f ? g.Dark : Pale) : g.Pattern == PatternKind.Socks ? g.Dark : g.Main;

    /// <summary>Horns, claws and spikes: ivory, dark on dark species, steel on metal ones, pale blue on ice.</summary>
    private Color Horn => g.Metallic ? Rgb(196, 204, 218) : g.Icy ? Ice : PokemonGenomes.Luma(g.Main) < 90f || g.Primary == PokemonType.Dark ? Rgb(84, 80, 96) : Ivory;

    private SurfaceMaterial HornMaterial => g.Metallic ? SurfaceMaterial.Metal : SurfaceMaterial.Shell;

    private Color BeakColor => g.Primary is PokemonType.Fire or PokemonType.Electric or PokemonType.Normal or PokemonType.Flying
        ? Rgb(248, 166, 56) : Rgb(250, 206, 72);

    private Color Leaf => g.Primary == PokemonType.Grass && PokemonGenomes.Luma(g.Main) > 60f && g.Main.G > g.Main.R ? PixelCanvas.Light1(g.Main, 0.1f) : Rgb(96, 186, 82);

    /// <summary>The pale part of the face round the mouth: the belly colour when it shows, otherwise a lighter body.</summary>
    private Color Pale => PokemonGenomes.Luma(g.Belly) - PokemonGenomes.Luma(g.Main) > 18f ? g.Belly : PixelCanvas.Light1(g.Main, 0.32f);

    // ------------------------------------------------------------------ heads and faces

    /// <summary>The head's main shape, on bone <paramref name="bone"/>, with the coat colour.</summary>
    private Shape Head(int bone, Vector3 c, Vector3 r)
    {
        r *= g.HeadShape switch
        {
            HeadShape.Wide => V(1.14f, 0.94f, 1f),
            HeadShape.Tall => V(0.94f, 1.12f, 0.98f),
            HeadShape.Flat => V(1.05f, 0.86f, 1f),
            HeadShape.Long => V(1f, 0.98f, 1.1f),
            _ => Vector3.One
        };
        if (g.Robot) b.Box(bone, c, r * 0.9f, Math.Min(r.X, r.Y) * 0.45f, g.Main, blend: 0.02f);
        else b.Ell(bone, c, r, g.Main);
        return new Shape(bone, c, r);
    }

    /// <summary>
    /// Eyes, mouth and the marks of the face. <paramref name="eyeYaw"/> turns the eyes from the front toward the sides
    /// (birds and fish look sideways); the mouth follows <see cref="PokeGenome.Mouth"/>.
    /// </summary>
    private void Face(Shape h, float eyeYaw = 0.42f, float eyePitch = 0.12f, bool mouth = true, float eyeScale = 1f)
    {
        var r = h.R;
        if (mouth) Mouth(h);

        float size = 0.24f * Math.Min(r.X, r.Y) * g.EyeScale * eyeScale;
        Color? iris = g.IrisEye ? g.Eye : null;
        if (g.OneEye)
        {
            var (p, n) = h.At(0f, eyePitch * 0.5f);
            b.Eye(h.Bone, p, n, size * 1.55f, iris ?? g.Eye, g.ScleraEye || !g.IrisEye);
        }
        else
        {
            PokeBuilder.Both(s =>
            {
                var (p, n) = h.At(s * eyeYaw, eyePitch);
                b.Eye(h.Bone, p, n, size, iris, g.ScleraEye);
            });
        }

        if (g.Cheeks)
            PokeBuilder.Both(s =>
            {
                var (p, n) = h.At(s * Math.Min(1.15f, eyeYaw + 0.5f), eyePitch - 0.38f);
                b.Mark(h.Bone, p, n, r.X * 0.13f, r.Y * 0.11f, Rgb(232, 86, 70));
            });

        if (g.Pattern == PatternKind.Mask)
            b.PaintEll(h.Bone, h.C + V(0, r.Y * Math.Clamp(MathF.Sin(eyePitch), 0f, 0.3f), r.Z * 0.3f), V(r.X * 1.08f, r.Y * 0.26f, r.Z * 0.85f), g.Dark);
    }

    /// <summary>The mouth: a muzzle with a nose, a snout, a beak, a bill, a long jaw, a trunk or mandibles.</summary>
    private void Mouth(Shape h)
    {
        var c = h.C;
        var r = h.R;
        int bone = h.Bone;
        switch (g.Mouth)
        {
            case MouthKind.Muzzle:
            {
                var mc = c + V(0, -r.Y * 0.3f, r.Z * 0.66f);
                var mr = V(r.X * 0.42f, r.Y * 0.33f, r.Z * 0.42f);
                b.Ell(bone, mc, mr, Pale, blend: 0.02f);
                var (np, nn) = On(mc, mr, 0f, 0.42f);
                b.Mark(bone, np, nn, r.X * 0.11f, r.Y * 0.085f, Ink);
                if (g.Fangs) Fangs(bone, mc + V(0, -mr.Y * 0.6f, mr.Z * 0.75f), r.X * 0.05f);
                if (g.Whiskers) Whiskers(bone, mc, mr);
                break;
            }
            case MouthKind.Snout:
            {
                var sc = c + V(0, -r.Y * 0.18f, r.Z * 0.88f);
                var sr = V(r.X * 0.33f, r.Y * 0.27f, r.Z * 0.3f);
                b.Ell(bone, sc, sr, PixelCanvas.Light1(g.Main, 0.18f), blend: 0.015f);
                PokeBuilder.Both(s =>
                {
                    var (p, n) = On(sc, sr, s * 0.32f, 0.05f);
                    b.Mark(bone, p, n, sr.X * 0.2f, sr.Y * 0.26f, PixelCanvas.Shadow(g.Main, 0.55f));
                });
                break;
            }
            case MouthKind.Beak:
                b.Spike(bone, c + V(0, -r.Y * 0.1f, r.Z * 0.78f), c + V(0, -r.Y * 0.32f, r.Z * 1.55f), r.Y * 0.3f, BeakColor, 0.75f, SurfaceMaterial.Shell);
                break;
            case MouthKind.Bill:
                b.Ell(bone, c + V(0, -r.Y * 0.24f, r.Z * 1.0f), V(r.X * 0.5f, r.Y * 0.15f, r.Z * 0.5f), BeakColor, mat: SurfaceMaterial.Shell, blend: 0.012f);
                break;
            case MouthKind.Jaw:
            {
                // A snout in the colour of the head, a small pale chin hinged under it, the line of the mouth between
                var sc = c + V(0, -r.Y * 0.2f, r.Z * 0.52f);
                var sr = V(r.X * 0.6f, r.Y * 0.48f, r.Z * 0.6f);
                b.Ell(bone, sc, sr, g.Main, blend: 0.03f);
                int jaw = b.Jaw(bone, c + V(0, -r.Y * 0.38f, 0));
                b.Ell(jaw, c + V(0, -r.Y * 0.62f, r.Z * 0.6f), V(r.X * 0.46f, r.Y * 0.22f, r.Z * 0.48f), Pale, blend: 0.02f);
                b.PaintEll(bone, c + V(0, -r.Y * 0.44f, r.Z * 0.62f), V(r.X * 0.56f, r.Y * 0.035f, r.Z * 0.52f), PixelCanvas.Shadow(g.Main, 0.55f));
                PokeBuilder.Both(s =>
                {
                    var (np, nn) = On(sc, sr, s * 0.25f, 0.45f);
                    b.Mark(bone, np, nn, sr.X * 0.09f, sr.Y * 0.08f, PixelCanvas.Shadow(g.Main, 0.6f));
                });
                if (g.Fangs || g.Primary is PokemonType.Dragon or PokemonType.Dark)
                    PokeBuilder.Both(s => b.Spike(bone, c + V(s * r.X * 0.3f, -r.Y * 0.4f, r.Z * 0.92f), c + V(s * r.X * 0.3f, -r.Y * 0.56f, r.Z * 0.94f),
                        r.X * 0.06f, Ivory, mat: SurfaceMaterial.Shell, blend: 0.004f));
                break;
            }
            case MouthKind.Trunk:
            {
                var a = c + V(0, -r.Y * 0.22f, r.Z * 0.82f);
                var m = c + V(0, -r.Y * 0.85f, r.Z * 1.22f);
                var e = c + V(0, -r.Y * 1.28f, r.Z * 1.08f);
                b.Limb(bone, a, m, r.X * 0.22f, r.X * 0.15f, g.Main);
                b.Limb(bone, m, e, r.X * 0.15f, r.X * 0.13f, g.Main);
                break;
            }
            case MouthKind.Mandibles:
                PokeBuilder.Both(s => b.Spike(bone, c + V(s * r.X * 0.38f, -r.Y * 0.42f, r.Z * 0.7f), c + V(s * r.X * 0.08f, -r.Y * 0.58f, r.Z * 1.28f),
                    r.X * 0.11f, Horn, 0.6f, SurfaceMaterial.Shell));
                break;
            default:
                if (g.Fangs) Fangs(bone, c + V(0, -r.Y * 0.42f, r.Z * 0.9f), r.X * 0.06f);
                break;
        }
    }

    private void Fangs(int bone, Vector3 at, float size) =>
        PokeBuilder.Both(s => b.Spike(bone, at + V(s * size * 1.6f, size * 0.6f, -size * 0.3f), at + V(s * size * 1.6f, -size * 1.8f, 0f), size, Rgb(250, 250, 246),
            mat: SurfaceMaterial.Shell, blend: 0.004f));

    private void Whiskers(int bone, Vector3 c, Vector3 r) =>
        PokeBuilder.Both(s =>
        {
            var root = c + V(s * r.X * 0.7f, 0, r.Z * 0.2f);
            b.Limb(bone, root, root + V(s * r.X * 1.9f, -r.Y * 0.5f, r.Z * 0.5f), 0.011f, 0.007f, g.Dark, blend: 0.006f);
        });

    // ------------------------------------------------------------------ ears

    private void Ears(Shape h, float scale = 1f)
    {
        if (g.Ears == EarKind.None) return;
        var r = h.R;
        float er = r.X * 0.32f * scale;
        var inner = g.Ears is EarKind.Round or EarKind.Long ? Rgb(244, 168, 176) : g.Dark;
        if (PokemonGenomes.Luma(g.Main) < 90f) inner = PixelCanvas.Light1(g.Main, 0.3f);
        PokeBuilder.Both(s =>
        {
            switch (g.Ears)
            {
                case EarKind.Round:
                {
                    var (p, n) = h.At(s * 0.62f, 0.8f);
                    int ear = b.Ear(h.Bone, s, p);
                    var c = p + n * er * 0.35f;
                    b.Ell(ear, c, V(er, er, er * 0.42f), g.Main, V(0, 12f * s, -18f * s));
                    b.PaintEll(ear, c + V(0, 0, er * 0.35f), V(er * 0.62f, er * 0.62f, er * 0.4f), inner, V(0, 12f * s, -18f * s));
                    break;
                }
                case EarKind.Pointed:
                {
                    var (p, n) = h.At(s * 0.5f, 0.78f);
                    int ear = b.Ear(h.Bone, s, p);
                    var dir = Vector3.Normalize(n * 0.5f + Vector3.UnitY * 0.85f + V(s * 0.22f, 0, -0.06f));
                    var tip = p + dir * r.Y * 0.78f * scale;
                    b.Spike(ear, p - n * 0.01f, tip, er * 0.58f, g.Main, 0.5f);
                    b.Spike(ear, p - n * 0.005f + V(0, 0, 0.012f), p + (tip - p) * 0.82f + V(0, 0, 0.014f), er * 0.34f, inner, 0.4f);
                    if (g.Tips) b.PaintEll(ear, p + (tip - p) * 0.86f, V(er * 0.55f, r.Y * 0.2f * scale, er * 0.55f), PixelCanvas.Shadow(g.Dark, 0.2f));
                    break;
                }
                case EarKind.Long:
                {
                    var (p, n) = h.At(s * 0.34f, 0.86f);
                    int ear = b.Ear(h.Bone, s, p);
                    float len = r.Y * 0.95f * scale;
                    var c = p + V(s * len * 0.18f, len * 0.5f, -er * 0.1f);
                    b.Ell(ear, c, V(er * 0.5f, len * 0.55f, er * 0.32f), g.Main, V(0, 0, -12f * s));
                    b.PaintEll(ear, c + V(0, 0, er * 0.22f), V(er * 0.3f, len * 0.42f, er * 0.25f), inner, V(0, 0, -12f * s));
                    if (g.Tips) b.PaintEll(ear, c + V(s * len * 0.1f, len * 0.5f, 0), V(er * 0.6f, len * 0.16f, er * 0.4f), g.Dark, V(0, 0, -12f * s));
                    break;
                }
                case EarKind.Floppy:
                {
                    var (p, _) = h.At(s * 0.8f, 0.5f);
                    int ear = b.Ear(h.Bone, s, p);
                    b.Ell(ear, p + V(s * er * 0.45f, -er * 0.95f, -er * 0.1f), V(er * 0.36f, er * 1.25f, er * 0.82f), PixelCanvas.Shadow(g.Main, 0.15f), V(0, 0, 14f * s));
                    break;
                }
                case EarKind.Fin:
                {
                    var (p, _) = h.At(s * 1.25f, 0.35f);
                    int ear = b.Ear(h.Bone, s, p);
                    var fin = g.Ears == EarKind.Fin && PokemonGenomes.Luma(g.Main) < 200f ? Rgb(246, 246, 250) : Accent;
                    b.Ell(ear, p + V(s * er * 0.35f, er * 0.45f, -er * 0.45f), V(0.022f, er * 1.15f, er * 0.85f), fin, V(-28f, 0, -20f * s));
                    break;
                }
                case EarKind.Tuft:
                {
                    var (p, n) = h.At(s * 0.55f, 0.82f);
                    int ear = b.Ear(h.Bone, s, p);
                    b.Spike(ear, p - n * 0.01f, p + Vector3.Normalize(n + Vector3.UnitY + V(s * 0.3f, 0, 0)) * er * 1.5f, er * 0.5f, g.Main, 0.6f);
                    break;
                }
            }
        });
    }

    // ------------------------------------------------------------------ on top of the head

    private void Top(Shape h)
    {
        var c = h.C;
        var r = h.R;
        int bone = h.Bone;
        switch (g.Top)
        {
            case TopKind.Horn:
            {
                var (p, n) = h.At(0f, 0.6f);
                b.Spike(bone, p - n * 0.02f, p + Vector3.Normalize(V(0, 1f, 0.35f)) * r.Y * 0.9f, r.X * 0.17f, Horn, mat: HornMaterial);
                break;
            }
            case TopKind.Horns:
            {
                bool outward = rnd.Chance(0.5f);
                PokeBuilder.Both(s =>
                {
                    var (p, n) = h.At(s * 0.48f, 0.66f);
                    var dir = outward ? Vector3.Normalize(V(s * 1f, 0.55f, -0.1f)) : Vector3.Normalize(V(s * 0.35f, 1f, -0.3f));
                    b.Spike(bone, p - n * 0.02f, p + dir * r.Y * 0.8f, r.X * 0.13f, Horn, mat: HornMaterial);
                });
                break;
            }
            case TopKind.Antlers:
                PokeBuilder.Both(s =>
                {
                    var (p, _) = h.At(s * 0.42f, 0.72f);
                    var mid = p + V(s * r.X * 0.35f, r.Y * 0.75f, -r.Z * 0.1f);
                    var tip = mid + V(s * r.X * 0.3f, r.Y * 0.55f, -r.Z * 0.2f);
                    var antler = Rgb(150, 112, 74);
                    b.Limb(bone, p, mid, r.X * 0.07f, r.X * 0.06f, antler, SurfaceMaterial.Shell, 0.01f);
                    b.Spike(bone, mid, tip, r.X * 0.06f, antler, mat: SurfaceMaterial.Shell);
                    b.Spike(bone, mid, mid + V(s * r.X * 0.05f, r.Y * 0.4f, r.Z * 0.25f), r.X * 0.05f, antler, mat: SurfaceMaterial.Shell);
                });
                break;
            case TopKind.Crest:
            {
                var (p, n) = h.At(0f, 1.05f);
                int count = g.Kind == BodyKind.Bird ? 2 + Math.Max(0, g.Stage) : 1;
                for (int i = 0; i < count; i++)
                {
                    float spread = (i - (count - 1) / 2f) * 0.35f;
                    var tip = p + V(0, r.Y * (0.75f - 0.1f * MathF.Abs(spread)), -r.Z * (0.55f + 0.35f * spread));
                    var crest = b.Spike(bone, p - n * 0.03f + V(0, 0, r.Z * spread * 0.4f), tip, r.Z * 0.24f, i == count - 1 && count > 1 ? Accent : PixelCanvas.Shadow(g.Main, 0.12f));
                    crest.Rotation = PokeBuilder.AlignY(tip - p);
                    crest.Stretch = V(0.38f, 1f, 1f);
                }
                break;
            }
            case TopKind.Sprout:
            {
                var (p, _) = h.At(0f, 1.2f);
                var top = p + V(0, r.Y * 0.38f, 0);
                b.Limb(bone, p - V(0, 0.02f, 0), top, 0.022f, 0.018f, Rgb(118, 82, 50), SurfaceMaterial.Shell);
                int leaves = b.Part("sprout", bone, top, PokeRole.Leaf);
                // Two leaves rising from a bud on the stem's tip, their inner ends in it
                b.Ell(leaves, top, V(0.026f, 0.024f, 0.026f), Leaf, mat: SurfaceMaterial.Leaf, blend: 0.01f);
                PokeBuilder.Both(s => b.Ell(leaves, top + V(0.085f * s, 0.035f, 0), V(0.1f, 0.028f, 0.06f), Leaf, V(0, 0, 28f * s), SurfaceMaterial.Leaf, 0.012f));
                break;
            }
            case TopKind.Flower:
            {
                var (p, _) = h.At(0f, 1.25f);
                var at = p - V(0, 0.005f, 0);
                int flower = b.Part("flower", bone, at, PokeRole.Leaf);
                // Sitting on the head, held to it by a green calyx under the petals
                b.Ell(flower, p - V(0, 0.01f, 0), V(0.04f, 0.035f, 0.04f), Leaf, mat: SurfaceMaterial.Leaf, blend: 0.012f);
                var petal = g.Primary == PokemonType.Grass ? (g.Secondary is { } t ? PokemonGenomes.TypeColors(t).A : Rgb(244, 140, 170)) : Accent;
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f;
                    b.Ell(flower, at + V(MathF.Sin(a) * 0.085f, 0.02f, MathF.Cos(a) * 0.085f), V(0.065f, 0.026f, 0.045f), petal, V(0, a * 180f / MathF.PI, 0), SurfaceMaterial.Leaf, 0.01f);
                }
                b.Ell(flower, at + V(0, 0.045f, 0), V(0.04f, 0.03f, 0.04f), Rgb(252, 214, 84), mat: SurfaceMaterial.Leaf, blend: 0.01f);
                break;
            }
            case TopKind.Flame:
            {
                var (p, _) = h.At(0f, 1.15f);
                int flame = b.Part("flame", bone, p, PokeRole.Flame);
                PokemonModels.Flame(b, flame, p - V(0, 0.03f, 0), 0.75f + 0.12f * Math.Max(0, g.Stage));
                break;
            }
            case TopKind.Antennae:
                PokeBuilder.Both(s =>
                {
                    var (p, _) = h.At(s * 0.3f, 0.82f);
                    int ant = b.Ear(bone, s, p);
                    var tip = p + V(s * r.X * 0.38f, r.Y * 0.95f, r.Z * 0.25f);
                    b.Limb(ant, p, tip, 0.014f, 0.012f, g.Dark, blend: 0.006f);
                    b.Ell(ant, tip, V(0.032f, 0.032f, 0.032f), Accent, blend: 0.006f);
                });
                break;
            case TopKind.Tuft:
            {
                var (p, n) = h.At(0f, 1.15f);
                b.Ell(bone, p, V(r.X * 0.28f, r.Y * 0.24f, r.X * 0.28f), g.Main);
                b.Spike(bone, p + V(0, r.Y * 0.1f, 0), p + V(r.X * 0.12f, r.Y * 0.5f, r.Z * 0.3f), r.X * 0.2f, g.Main);
                break;
            }
            case TopKind.Gem:
            {
                var (p, n) = h.At(0f, 0.5f);
                var gem = g.Second.R > 150 && g.Second.G < 120 ? g.Second : Rgb(222, 64, 76);
                b.Ell(bone, p - n * 0.005f, V(r.X * 0.15f, r.Y * 0.13f, r.X * 0.09f), gem, mat: SurfaceMaterial.Shell, blend: 0.006f);
                break;
            }
            case TopKind.Cap:
            {
                var cc = c + V(0, r.Y * 0.78f, -r.Z * 0.05f);
                var cr = V(r.X * 1.35f, r.Y * 0.46f, r.Z * 1.3f);
                b.Ell(bone, cc, cr, Accent, blend: 0.015f);
                for (int i = 0; i < 4; i++)
                {
                    float yaw = -1.6f + i * 1.05f + rnd.Range(-0.2f, 0.2f);
                    var (p, n) = On(cc, cr, yaw, 0.95f);
                    b.Mark(bone, p, n, cr.X * 0.16f, cr.X * 0.16f, Rgb(248, 244, 236));
                }
                break;
            }
            case TopKind.Crown:
                for (int i = 0; i < 5; i++)
                {
                    float yaw = -0.9f + i * 0.45f;
                    var (p, n) = h.At(yaw, 0.95f);
                    b.Spike(bone, p - n * 0.02f, p + Vector3.Normalize(n + Vector3.UnitY * 1.5f) * r.Y * (i == 2 ? 0.5f : 0.35f), r.X * 0.09f, Gold, mat: SurfaceMaterial.Metal);
                }
                break;
        }
    }

    // ------------------------------------------------------------------ tails

    /// <summary>
    /// The tail (or tails) from <paramref name="at"/> on the body's rear, sized by <paramref name="size"/>; it rises
    /// behind a four-legged body and curls up behind a two-legged one.
    /// </summary>
    private void Tail(int parent, Vector3 at, float size)
    {
        if (g.Tail == TailKind.None) return;
        int count = g.Tail is TailKind.Bushy or TailKind.Thin ? g.Tails : 1;
        for (int i = 0; i < count; i++)
        {
            float spread = count == 1 ? 0f : (i - (count - 1) / 2f) / Math.Max(1f, (count - 1) / 2f);
            var side = V(spread * 0.12f * size, 0, 0);
            int tail = b.Tail(at + side * 0.3f, parent, i * 1.3f);
            Vector3 end, dir;
            var root = at + side * 0.3f;
            switch (g.Tail)
            {
                case TailKind.Stub:
                    b.Ell(tail, root + V(0, 0.01f, -0.04f) * size, V(0.065f, 0.06f, 0.06f) * size, g.Main);
                    continue;
                case TailKind.Thin:
                {
                    var mid = root + V(spread * 0.12f, 0.05f, -0.15f) * size;
                    end = mid + V(spread * 0.18f, 0.13f, -0.07f) * size;
                    b.Limb(tail, root, mid, 0.028f * size, 0.023f * size, g.Main, blend: 0.012f);
                    b.Limb(tail, mid, end, 0.023f * size, 0.019f * size, g.Main, blend: 0.01f);
                    dir = Vector3.Normalize(end - mid);
                    if (g.Tips && g.TailTip == TipKind.None) b.PaintEll(tail, end, V(0.04f, 0.05f, 0.04f) * size, g.Dark);
                    break;
                }
                case TailKind.Bushy:
                {
                    // A plume that leans back from the rump and curls up at its pale tip, on a stem that joins it to the
                    // body (the plume alone stops short of the rump); several fan out from one root
                    var c = root + V(spread * 0.07f, 0.03f, -0.15f) * size;
                    b.Limb(tail, root, c, 0.045f * size, 0.055f * size, g.Main, blend: 0.02f);
                    b.Ell(tail, c, V(0.068f, 0.1f, 0.085f) * size, g.Main, V(-64f, spread * 20f, -spread * 25f), blend: 0.03f);
                    var tip = c + V(spread * 0.05f, 0.07f, -0.07f) * size;
                    b.Ell(tail, tip, V(0.055f, 0.07f, 0.06f) * size, g.Main, V(-20f, 0, 0), blend: 0.03f);
                    b.PaintEll(tail, tip + V(0, 0.04f, 0.01f) * size, V(0.06f, 0.05f, 0.06f) * size, g.Tips ? g.Dark : Pale);
                    continue;
                }
                case TailKind.Thick:
                {
                    end = root + V(0, -0.04f, -0.34f) * size;
                    b.Limb(tail, root, end, 0.08f * size, 0.032f * size, g.Main, blend: 0.03f);
                    dir = Vector3.Normalize(end - root);
                    break;
                }
                case TailKind.Bolt:
                {
                    // A flat zigzag, wide seen from the side: a short stem in the dark colour, then two broad strokes
                    var p1 = root + V(0, 0.1f, -0.1f) * size;
                    var p2 = p1 + V(0, 0.03f, -0.12f) * size;
                    var p3 = p2 + V(0, 0.2f, -0.02f) * size;
                    b.Limb(tail, root, p1, 0.026f * size, 0.026f * size, g.Dark, blend: 0.01f);
                    foreach (var (a, e, w) in new[] { (p1, p2, 0.06f), (p2, p3, 0.1f) })
                    {
                        var seg = b.Spike(tail, a, e + (e - a) * 0.2f, w * size, g.Main, blend: 0.008f);
                        seg.Rotation = PokeBuilder.AlignY(e - a);
                        seg.Stretch = V(0.32f, 1f, 1f);
                    }
                    var flag = b.Ell(tail, p3 + V(0, -0.03f, 0.01f) * size, V(0.035f, 0.11f, 0.08f) * size, g.Main, V(-15f, 0, 0), blend: 0.01f);
                    flag.Stretch = V(0.6f, 1f, 1f);
                    continue;
                }
                case TailKind.Paddle:
                {
                    var c = root + V(0, -0.04f, -0.18f) * size;
                    b.Ell(tail, c, V(0.12f, 0.035f, 0.17f) * size, PixelCanvas.Shadow(g.Main, 0.15f), V(-18f, 0, 0), SurfaceMaterial.Scales);
                    continue;
                }
                case TailKind.Fin:
                {
                    var mid = root + V(0, 0.02f, -0.16f) * size;
                    b.Limb(tail, root, mid, 0.05f * size, 0.03f * size, g.Main, blend: 0.015f);
                    b.Ell(tail, mid + V(0, 0.03f, -0.07f) * size, V(0.02f, 0.11f, 0.08f) * size, Accent, V(-20f, 0, 0), blend: 0.012f);
                    continue;
                }
                case TailKind.Curl:
                    // A loop standing up behind the rump, on a stub that joins it to the body
                    b.Ell(tail, root, V(0.028f, 0.028f, 0.03f) * size, g.Main, blend: 0.01f);
                    b.Torus(tail, root + V(0, 0.025f, -0.03f) * size, 0.035f * size, 0.013f * size, g.Main, V(0, 0, 90f), blend: 0.008f);
                    continue;
                case TailKind.Wisp:
                {
                    var mid = root + V(0, -0.08f, -0.1f) * size;
                    end = mid + V(0.04f, -0.06f, -0.1f) * size;
                    b.Limb(tail, root, mid, 0.11f * size, 0.06f * size, g.Main, blend: 0.03f);
                    b.Limb(tail, mid, end, 0.06f * size, 0.015f * size, g.Main, blend: 0.02f);
                    continue;
                }
                default:
                    continue;
            }
            Tip(tail, end, dir, size);
        }
    }

    private void Tip(int tail, Vector3 end, Vector3 dir, float size)
    {
        switch (g.TailTip)
        {
            case TipKind.Flame:
            {
                int flame = b.Part("flame", tail, end, PokeRole.Flame);
                PokemonModels.Flame(b, flame, end, 0.62f * size);
                break;
            }
            case TipKind.Leaf:
            {
                int leaf = b.Part("tailLeaf", tail, end, PokeRole.Leaf);
                b.Ell(leaf, end + dir * 0.07f * size, V(0.04f, 0.09f, 0.06f) * size, Leaf, V(-10f, 0, 0), SurfaceMaterial.Leaf, 0.012f);
                break;
            }
            case TipKind.Tuft:
                b.Ell(tail, end + dir * 0.03f * size, V(0.045f, 0.06f, 0.045f) * size, g.Dark, blend: 0.012f);
                break;
            case TipKind.Ball:
                b.Ell(tail, end + dir * 0.03f * size, V(0.05f, 0.05f, 0.05f) * size, Accent, blend: 0.01f);
                break;
            case TipKind.Star:
                PokemonModels.StarTip(b, tail, end + dir * 0.05f * size, 0.08f * size, Accent);
                break;
            case TipKind.Spike:
                b.Spike(tail, end, end + dir * 0.11f * size, 0.035f * size, Horn, 0.7f, HornMaterial);
                break;
        }
    }

    // ------------------------------------------------------------------ the back

    /// <summary>
    /// A point on the top of <paramref name="body"/> along its spine: <paramref name="along"/> 1 is the front, -1 the
    /// rear, 0 the highest point.
    /// </summary>
    private static (Vector3 P, Vector3 N) Spine(Shape body, float along) => On(body.C, body.R, along > 0 ? 0f : MathF.PI, MathF.PI / 2f - MathF.Abs(along) * 1.05f);

    private void Back(Shape body)
    {
        var c = body.C;
        var r = body.R;
        switch (g.Back)
        {
            case BackKind.Shell:
            {
                var shell = PokemonGenomes.Luma(Accent) < 200f && Accent.R >= Accent.B ? Accent : Rgb(150, 100, 58);
                b.Ell(Body, c + V(0, r.Y * 0.42f, -r.Z * 0.04f), V(r.X * 1.12f, r.Y * 0.82f, r.Z * 1.04f), shell, mat: SurfaceMaterial.Shell);
                b.Torus(Body, c + V(0, r.Y * 0.02f, -r.Z * 0.04f), r.X * 1.06f, 0.034f, PixelCanvas.Light1(shell, 0.35f), sz: r.Z / r.X, mat: SurfaceMaterial.Shell);
                // Rings on the hind part of the shell, clear of the head
                for (int i = 0; i < 3; i++)
                {
                    var (p, n) = On(c + V(0, r.Y * 0.42f, -r.Z * 0.04f), V(r.X * 1.12f, r.Y * 0.82f, r.Z * 1.04f), MathF.PI + (i - 1) * 0.85f, 0.95f);
                    b.Mark(Body, p, n, r.X * 0.22f, r.Z * 0.18f, PixelCanvas.Shadow(shell, 0.2f), MarkShape.Ring);
                }
                break;
            }
            case BackKind.Bulb:
            {
                var (p, _) = Spine(body, 0f);
                int bulb = b.Part("bulb", Body, p, PokeRole.Leaf);
                var leaf = Leaf;
                b.Ell(bulb, p + V(0, r.X * 0.55f, 0), V(r.X * 0.82f, r.X * 0.9f, r.X * 0.82f), PixelCanvas.Light1(leaf, 0.12f), mat: SurfaceMaterial.Leaf, blend: 0.03f);
                b.PaintEll(bulb, p + V(0, r.X * 1.3f, 0), V(r.X * 0.3f, r.X * 0.25f, r.X * 0.3f), PixelCanvas.Shadow(leaf, 0.12f));
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.Tau / 4f + 0.4f;
                    b.Ell(bulb, p + V(MathF.Sin(a) * r.X * 0.62f, r.X * 0.12f, MathF.Cos(a) * r.X * 0.62f), V(r.X * 0.36f, 0.03f, r.X * 0.2f), PixelCanvas.Shadow(leaf, 0.1f),
                        V(0, a * 180f / MathF.PI + 90f, 0), SurfaceMaterial.Leaf, 0.015f);
                }
                break;
            }
            case BackKind.Leaves:
            {
                int count = 2 + Math.Max(0, g.Stage);
                for (int i = 0; i < count; i++)
                {
                    float along = 0.5f - i * (1f / Math.Max(1, count - 1)) * 0.9f;
                    var (p, n) = Spine(body, along);
                    int leaf = b.Part("leaf" + i, Body, p, PokeRole.Leaf, i * 1.7f);
                    float side = i % 2 == 0 ? 1f : -1f;
                    b.Ell(leaf, p + V(side * 0.06f, 0.08f, 0), V(0.07f, 0.13f, 0.035f), Leaf, V(0, 0, -side * 25f), SurfaceMaterial.Leaf, 0.015f);
                }
                break;
            }
            case BackKind.Flower:
            {
                var (p, _) = Spine(body, 0f);
                int flower = b.Part("flower", Body, p, PokeRole.Leaf);
                var petal = g.Secondary is { } t && t != PokemonType.Grass ? PokemonGenomes.TypeColors(t).A : Rgb(244, 132, 160);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f;
                    b.Ell(flower, p + V(MathF.Sin(a) * r.X * 0.62f, r.X * 0.22f, MathF.Cos(a) * r.X * 0.62f), V(r.X * 0.48f, 0.04f, r.X * 0.3f), petal,
                        V(0, a * 180f / MathF.PI + 90f, 0), SurfaceMaterial.Leaf, 0.015f);
                }
                b.Ell(flower, p + V(0, r.X * 0.32f, 0), V(r.X * 0.3f, r.X * 0.2f, r.X * 0.3f), Rgb(252, 214, 90), mat: SurfaceMaterial.Leaf, blend: 0.015f);
                break;
            }
            case BackKind.Spikes:
            case BackKind.Plates:
            case BackKind.Crystals:
            {
                int n = Math.Max(2, g.Spikes);
                bool plates = g.Back == BackKind.Plates;
                var color = g.Back == BackKind.Crystals ? Ice : plates ? Accent : Horn;
                for (int i = 0; i < n; i++)
                {
                    float along = 0.75f - 1.5f * i / Math.Max(1, n - 1);
                    var (p, nn) = Spine(body, along);
                    float len = r.Y * (0.5f + 0.35f * (1f - MathF.Abs(along))) * (plates ? 1.1f : 1f);
                    var dir = Vector3.Normalize(nn + V(0, 0.4f, -0.35f));
                    var spike = b.Spike(Body, p - nn * 0.015f, p + dir * len, r.Y * (plates ? 0.32f : 0.18f), color, mat: g.Back == BackKind.Crystals ? SurfaceMaterial.Shell : HornMaterial);
                    if (plates)
                    {
                        spike.Rotation = PokeBuilder.AlignY(dir);
                        spike.Stretch = V(0.32f, 1f, 1f);
                    }
                }
                break;
            }
            case BackKind.Mane:
                // A ruff round the neck, swept back
                for (int i = 0; i < 5; i++)
                {
                    float a = -1f + i * 0.5f;
                    var root = c + V(MathF.Sin(a) * r.X * 0.8f, r.Y * (0.45f + 0.35f * MathF.Cos(a)), r.Z * 0.72f);
                    b.Spike(Body, root, root + V(MathF.Sin(a) * r.X * 0.55f, r.Y * 0.35f, -r.Z * 0.5f), r.Y * 0.3f, g.Dark);
                }
                break;
            case BackKind.Flames:
            {
                int count = 1 + Math.Min(2, Math.Max(0, g.Stage));
                for (int i = 0; i < count; i++)
                {
                    // Along the back behind the neck, standing up out of it
                    float along = count == 1 ? 0.1f : 0.25f - 0.8f * i / (count - 1);
                    var (p, _) = Spine(body, along);
                    int flame = b.Part("flame" + i, Body, p, PokeRole.Flame, i * 2.1f);
                    PokemonModels.Flame(b, flame, p + V(0, 0.01f, 0), 0.66f);
                }
                break;
            }
            case BackKind.Mushroom:
            {
                var (p, _) = Spine(body, 0.1f);
                int cap = b.Part("mushroom", Body, p, PokeRole.Leaf);
                var cc = p + V(0, r.Y * 0.38f, 0);
                var cr = V(r.X * 1.05f, r.Y * 0.5f, r.Z * 0.75f);
                b.Limb(cap, p - V(0, 0.02f, 0), cc, r.X * 0.25f, r.X * 0.22f, Rgb(244, 232, 206), blend: 0.02f);
                b.Ell(cap, cc, cr, Accent, blend: 0.02f);
                for (int i = 0; i < 4; i++)
                {
                    var (sp, sn) = On(cc, cr, -1.5f + i * 1.0f, 0.95f);
                    b.Mark(cap, sp, sn, cr.X * 0.17f, cr.X * 0.17f, Rgb(250, 244, 230));
                }
                break;
            }
            case BackKind.Fin:
            {
                var (p, n) = Spine(body, 0.05f);
                var tip = p + V(0, r.Y * 0.9f, -r.Z * 0.45f);
                var fin = b.Spike(Body, p - n * 0.02f, tip, r.Z * 0.32f, PixelCanvas.Shadow(g.Main, 0.12f));
                fin.Rotation = PokeBuilder.AlignY(tip - p);
                fin.Stretch = V(0.3f, 1f, 1f);
                break;
            }
        }
    }

    /// <summary>A pair of wings on the back of a body that isn't a bird, at <paramref name="shoulder"/> (x is the half width).</summary>
    private void BackWings(Vector3 shoulder, float size)
    {
        if (g.Wings == WingKind.None) return;
        PokeBuilder.Both(s =>
        {
            var joint = V(shoulder.X * s, shoulder.Y, shoulder.Z);
            int wing = b.Wing(s, joint);
            switch (g.Wings)
            {
                case WingKind.Dragon:
                case WingKind.Bat:
                    Membrane(wing, joint, s, size * 0.9f);
                    break;
                case WingKind.Insect:
                case WingKind.Butterfly:
                    for (int k = 0; k < 2; k++)
                    {
                        var c = joint + V(s * 0.13f, 0.1f - 0.12f * k, -0.04f) * size;
                        b.Ell(wing, c, V(0.14f, 0.075f, 0.022f) * size, Rgb(228, 240, 248), V(0, 20f * s, (k == 0 ? -30f : 15f) * s), blend: 0.012f);
                        // A vein from inside the back holds the wing to it
                        b.Limb(wing, V(joint.X * 0.5f, joint.Y, joint.Z), c, 0.014f * size, 0.007f * size, Rgb(176, 192, 206), blend: 0.006f);
                    }
                    break;
                default:
                    b.Ell(wing, joint + V(s * 0.08f, 0.1f, -0.06f) * size, V(0.05f, 0.16f, 0.11f) * size, PixelCanvas.Shadow(g.Main, 0.1f), V(-20f, 0, -35f * s), blend: 0.015f);
                    b.Ell(wing, joint + V(s * 0.1f, 0.06f, -0.03f) * size, V(0.04f, 0.1f, 0.08f) * size, Accent, V(-20f, 0, -35f * s), blend: 0.01f);
                    break;
            }
        });
    }

    /// <summary>The colour of a wing's skin: a cool green-blue under a fiery body, else the accent (or a darker body where that is pale).</summary>
    private Color MembraneColor => g.Primary == PokemonType.Fire ? Rgb(72, 150, 156)
        : PokemonGenomes.Luma(Accent) > 215f ? PixelCanvas.Shadow(g.Main, 0.25f) : PixelCanvas.Mix(Accent, g.Main, 0.25f);

    /// <summary>
    /// A wing of skin from <paramref name="joint"/>: an arm out to the elbow, three fingers fanning out from it and
    /// the skin stretched between them.
    /// </summary>
    private void Membrane(int wing, Vector3 joint, float s, float k)
    {
        var bone = PokemonGenomes.Luma(g.Main) < 90f ? PixelCanvas.Light1(g.Main, 0.2f) : PixelCanvas.Shadow(g.Main, 0.15f);
        var elbow = joint + V(s * 0.17f, 0.16f, -0.05f) * k;
        b.Limb(wing, joint, elbow, 0.03f * k, 0.024f * k, bone, blend: 0.012f);
        var tips = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            tips[i] = elbow + V(s * (0.2f + 0.03f * i), 0.06f - 0.15f * i, -0.04f - 0.04f * i) * k;
            b.Spike(wing, elbow, tips[i], 0.018f * k, bone, blend: 0.008f);
        }
        var skin = MembraneColor;
        for (int i = 0; i < 2; i++)
        {
            var c = (joint * 0.4f + elbow * 0.2f + tips[i] * 0.2f + tips[i + 1] * 0.2f);
            b.Ell(wing, c + V(0, -0.02f, -0.01f) * k, V(0.13f, 0.1f, 0.018f) * k, skin, V(-8f, 22f * s, (i == 0 ? -25f : -55f) * s), blend: 0.012f);
        }
    }

    /// <summary>A fluffy ruff round the neck, from <paramref name="center"/> round <paramref name="axis"/> (the way the neck runs).</summary>
    private void Ruff(int bone, Vector3 center, Vector3 axis, float radius, float size)
    {
        axis = Vector3.Normalize(axis);
        var u = Vector3.Normalize(Vector3.Cross(axis, Vector3.UnitX));
        var v = Vector3.Cross(axis, u);
        var fluff = PokemonGenomes.Luma(g.Belly) - PokemonGenomes.Luma(g.Main) > 12f ? g.Belly : Accent;
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.2f;
            var at = center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * radius;
            b.Ell(bone, at, V(size, size * 0.8f, size), fluff, blend: 0.03f);
        }
    }

    // ------------------------------------------------------------------ hands and feet

    /// <summary>A hand at <paramref name="at"/> on arm bone <paramref name="arm"/>, pointing along <paramref name="dir"/>.</summary>
    private void Hand(int arm, Vector3 at, Vector3 dir, float size, float side)
    {
        var skin = Extremity;
        switch (g.Hands)
        {
            case HandKind.Fist:
                b.Ell(arm, at + dir * size * 0.4f, V(size * 1.35f, size * 1.25f, size * 1.3f), g.Primary == PokemonType.Fighting ? Accent : skin);
                break;
            case HandKind.Claw:
                b.Ell(arm, at, V(size, size, size), skin);
                for (int k = -1; k <= 1; k++)
                    b.Spike(arm, at + dir * size * 0.5f + V(k * size * 0.45f, 0, 0), at + dir * size * 1.6f + V(k * size * 0.55f, -size * 0.3f, 0), size * 0.28f, Horn,
                        mat: HornMaterial, blend: 0.006f);
                break;
            case HandKind.Blade:
            {
                var tip = at + Vector3.Normalize(dir + V(side * 0.3f, 0.9f, 0.4f)) * size * 5f;
                var blade = b.Spike(arm, at - dir * size * 0.5f, tip, size * 1.1f, g.Metallic ? Rgb(200, 208, 222) : PixelCanvas.Light1(g.Main, 0.25f), mat: HornMaterial);
                blade.Rotation = PokeBuilder.AlignY(tip - at);
                blade.Stretch = V(1f, 1f, 0.28f);
                break;
            }
            case HandKind.Pincer:
            {
                var claw = PixelCanvas.Light1(g.Main, 0.08f);
                var c = at + dir * size * 1.2f;
                b.Ell(arm, c, V(size * 1.5f, size * 1.15f, size * 1.4f), claw, mat: SurfaceMaterial.Shell);
                foreach (float k in new[] { 1f, -1f })
                    b.Spike(arm, c + V(0, k * size * 0.6f, size * 0.6f), c + dir * size * 2.4f + V(0, k * size * 0.35f, 0), size * 0.75f, claw, 0.6f, SurfaceMaterial.Shell);
                break;
            }
            case HandKind.Leaf:
                b.Ell(arm, at + dir * size * 0.8f, V(size * 1.6f, size * 0.45f, size * 1.0f), Leaf, V(0, 0, side * 30f), SurfaceMaterial.Leaf);
                break;
            case HandKind.Flipper:
                b.Ell(arm, at + dir * size * 0.6f, V(size * 0.6f, size * 1.6f, size * 1.1f), skin, V(0, 0, side * 20f));
                break;
            default:
                b.Ell(arm, at, V(size, size, size), skin);
                break;
        }
    }

    /// <summary>A foot: a rounded pad, hooved on long legs, with claws if it has them.</summary>
    private void Foot(int leg, Vector3 at, float r, Color color, bool hoof = false)
    {
        if (hoof)
        {
            b.Ell(leg, at + V(0, -r * 0.1f, 0), V(r * 1.05f, r * 0.85f, r * 1.1f), Rgb(84, 70, 64), mat: SurfaceMaterial.Shell);
            return;
        }
        b.Ell(leg, at, V(r * 1.05f, r * 0.8f, r * 1.3f), color);
        if (g.Claws)
            for (int k = -1; k <= 1; k++)
                b.Spike(leg, at + V(k * r * 0.5f, -r * 0.1f, r * 0.9f), at + V(k * r * 0.6f, -r * 0.3f, r * 1.7f), r * 0.24f, Horn, mat: HornMaterial, blend: 0.005f);
    }

    // ------------------------------------------------------------------ patterns

    /// <summary>The body's markings. <paramref name="front"/> is where the chest faces (+Z for an upright body, down for a four-legged one).</summary>
    private void Pattern(Shape body, bool upright)
    {
        var c = body.C;
        var r = body.R;
        switch (g.Pattern)
        {
            case PatternKind.Belly when g.Saddle && !upright:
                b.PaintEll(body.Bone, c + V(0, r.Y * 0.75f, -r.Z * 0.1f), V(r.X * 0.95f, r.Y * 0.42f, r.Z * 0.62f), PokemonGenomes.Luma(g.Main) > 120f ? g.Dark : Accent);
                goto case PatternKind.Socks;
            case PatternKind.Belly:
            case PatternKind.Socks:
            case PatternKind.Mask:
                if (PokemonGenomes.Luma(g.Belly) - PokemonGenomes.Luma(g.Main) is > 14f or < -14f)
                {
                    if (upright) b.PaintEll(body.Bone, c + V(0, -r.Y * 0.08f, r.Z * 0.45f), V(r.X * 0.7f, r.Y * 0.74f, r.Z * 0.62f), g.Belly);
                    else b.PaintEll(body.Bone, c + V(0, -r.Y * 0.6f, 0), V(r.X * 0.72f, r.Y * 0.5f, r.Z * 0.88f), g.Belly);
                }
                break;
            case PatternKind.Spots:
            {
                // A big shell, bulb, flower or cap covers a long body's back: no spots go under it
                if (!upright && g.Back is BackKind.Shell or BackKind.Bulb or BackKind.Flower or BackKind.Mushroom or BackKind.Leaves) break;
                int count = 3 + rnd.Int(3);
                for (int i = 0; i < count; i++)
                {
                    // On the chest of an upright body (arms hang at its sides, tails and wings cover its back), on the
                    // hind part of a long one's sides (its neck rises from the front, its tail from the rump)
                    float side = i % 2 == 0 ? 1f : -1f;
                    float yaw = upright ? rnd.Range(-0.8f, 0.8f) : side * rnd.Range(1.7f, 2.5f);
                    float pitch = upright ? rnd.Range(-0.2f, 0.45f) : rnd.Range(0.3f, 0.7f);
                    var (p, n) = body.At(yaw, pitch);
                    float s = Math.Min(r.X, r.Y) * rnd.Range(0.16f, 0.24f);
                    b.Mark(body.Bone, p, n, s, s, g.Second == g.Main ? g.Dark : Accent);
                }
                break;
            }
            case PatternKind.Stripes:
            {
                var stripe = PokemonGenomes.Luma(g.Main) > 150f ? Rgb(52, 48, 60) : g.Dark;
                for (int i = 0; i < 3; i++)
                {
                    if (upright)
                        b.PaintTorus(body.Bone, c + V(0, r.Y * (0.35f - 0.35f * i), 0), r.X * 0.98f, 0.024f, stripe, default, 1f, r.Z / r.X);
                    else
                        b.PaintTorus(body.Bone, c + V(0, 0, r.Z * (0.3f - 0.4f * i)), r.X * 0.98f, 0.026f, stripe, V(90f, 0, 0), 1f, r.Y / r.X);
                }
                break;
            }
            case PatternKind.TwoTone:
                b.PaintEll(body.Bone, c - V(0, r.Y * 0.95f, 0), V(r.X * 1.25f, r.Y * 0.98f, r.Z * 1.25f), Rgb(246, 244, 240));
                break;
            case PatternKind.ChestMark:
            {
                var (p, n) = upright ? body.At(0f, 0.1f) : body.At(0f, -0.2f);
                b.Mark(body.Bone, p, n, r.X * 0.22f, r.X * 0.22f, Accent, rnd.Chance(0.5f) ? MarkShape.Ring : MarkShape.Star);
                break;
            }
        }

        // Rocky bodies are lumpy (but a crescent's lumps could land in its bite); icy ones carry crystals
        if (g.Rocky && g.Kind is not (BodyKind.Serpent or BodyKind.Crawler) && g.Special != Special.Crescent)
            for (int i = 0; i < 4; i++)
            {
                var (p, n) = body.At(rnd.Range(-2.8f, 2.8f), rnd.Range(0.1f, 0.9f));
                float s = Math.Min(r.X, r.Y) * rnd.Range(0.2f, 0.3f);
                b.Ell(body.Bone, p, V(s, s * 0.8f, s), PixelCanvas.Shadow(g.Main, 0.12f), blend: 0.02f);
            }
        if (g.Icy && g.Back != BackKind.Crystals && g.Kind is BodyKind.Ball or BodyKind.Armed or BodyKind.Blob or BodyKind.Cluster)
            for (int i = 0; i < 3; i++)
            {
                var (p, n) = body.At(-1.2f + i * 1.2f, 0.9f);
                b.Spike(body.Bone, p - n * 0.02f, p + n * r.Y * 0.4f, r.Y * 0.12f, Ice, mat: SurfaceMaterial.Shell);
            }
    }
}
