using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The model generator (plan 03 · D5): sculpts any species from its <see cref="PokeGenome"/> with the kit the
/// hand-built models use (<see cref="PokeBuilder"/>), on the same body plans, so the same clips move it. No meshing
/// and no GPU calls: <see cref="PokemonModels"/> meshes what this returns.
/// </summary>
internal static class PokemonGenerator
{
    public static PokeBuilder Build(PokeGenome genome) => new PokemonSculptor(genome).Sculpt();

    /// <summary>The generated model of a species (null for a name that isn't one).</summary>
    public static PokeBuilder? Build(string species) => PokemonGenomes.For(species) is { } genome ? Build(genome) : null;
}

/// <summary>Turns one genome into a sculpt: a routine per <see cref="BodyKind"/>, built from the shared parts.</summary>
internal sealed partial class PokemonSculptor
{
    private const int Body = PokeBuilder.Body;

    /// <summary>How far fliers float above the ground at rest, as a share of their height.</summary>
    internal const float HoverGap = 0.12f;

    private readonly PokeGenome g;
    private readonly GenomeRandom rnd;
    private PokeBuilder b = null!;

    public PokemonSculptor(PokeGenome genome)
    {
        g = genome;
        rnd = new GenomeRandom(genome.Seed);
    }

    public PokeBuilder Sculpt()
    {
        switch (g.Kind)
        {
            case BodyKind.Quadruped: Quadruped(); break;
            case BodyKind.Upright: Biped(tailed: true); break;
            case BodyKind.Humanoid: Biped(tailed: false); break;
            case BodyKind.Legs: Legs(); break;
            case BodyKind.Bird: Bird(); break;
            case BodyKind.Bat: Bat(); break;
            case BodyKind.Insect: Insect(); break;
            case BodyKind.Serpent: Serpent(); break;
            case BodyKind.Crawler: Crawler(); break;
            case BodyKind.Arthropod: Arthropod(); break;
            case BodyKind.Fish: Fish(); break;
            case BodyKind.Ball: Ball(); break;
            case BodyKind.Armed: Armed(); break;
            case BodyKind.Blob: Blob(); break;
            case BodyKind.Cluster: Cluster(); break;
            case BodyKind.Cocoon: Cocoon(); break;
            case BodyKind.Jelly: Jelly(); break;
            default: Tentacled(); break;
        }
        b.Ground(g.Hovers ? HoverGap : 0f);
        FitDecals();
        return b;
    }

    /// <summary>
    /// Grows any eye or mark smaller than a few cells of the mesh the model will be meshed with: a decal is laid on
    /// whole triangles, so on a big model (wide wings, a flower on its back) a tiny nostril would find none.
    /// </summary>
    private void FitDecals()
    {
        var (min, max) = b.Sdf.Bounds();
        float extent = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
        float least = 2.5f * extent / PokemonModels.CellsAcross;
        foreach (var d in b.Model.Decals)
        {
            float small = Math.Min(d.Half.X, d.Half.Y);
            if (small >= least) continue;
            float k = least / small;
            d.Half *= k;
            d.Size *= k;
        }
    }

    private void Start(Vector3 body)
    {
        b = new PokeBuilder(g.Species, g.Fill, g.Plan, body) { Coat = g.Coat };
        if (g.Hovers) b.Hover();
        // Small species breathe and sway a little quicker than big ones
        b.Model.Tempo = 1.2f - 0.35f * (g.Fill - 0.45f) / 0.55f;
    }

    // ------------------------------------------------------------------ four legs

    private void Quadruped()
    {
        float legF = g.Stance switch { Stance.Long => 1.65f, Stance.Low => 0.55f, Stance.Stocky => 0.7f, _ => 1f };
        bool stocky = g.Stance == Stance.Stocky, hoof = g.Stance == Stance.Long;
        float girth = 0.15f * g.Girth * (stocky ? 1.22f : 1f);
        float half = 0.23f * g.BodyLength * (g.Stance == Stance.Low ? 1.25f : 1f);
        float legLen = 0.17f * g.LegScale * legF;
        float by = legLen + girth * 0.9f;
        Start(V(0, by, 0));

        float pawR = (hoof ? 0.045f : 0.055f) * (stocky ? 1.2f : 1f);
        float legR = (hoof ? 0.04f : 0.05f) * g.Girth * (stocky ? 1.3f : 1f);
        var paw = Extremity;
        foreach (var (z, front) in new[] { (half * 0.62f, true), (-half * 0.62f, false) })
            PokeBuilder.Both(s =>
            {
                float x = girth * 0.64f * s;
                var hip = V(x, by - girth * 0.25f, z);
                var foot = V(x * 1.05f, pawR * 0.8f, z + 0.02f);
                int leg = b.Leg(s, hip, front);
                b.Limb(leg, hip, foot + V(0, pawR * 0.4f, 0), legR, legR * 0.82f, g.Main);
                Foot(leg, foot, pawR, paw, hoof);
            });

        var body = new Shape(Body, V(0, by, 0), V(girth, girth, half));
        if (g.Robot) b.Box(Body, body.C, V(girth, girth * 0.9f, half), girth * 0.4f, g.Main, blend: 0.02f);
        else
        {
            b.Ell(Body, V(0, by, half * 0.3f), V(girth * 1.04f, girth, half * 0.66f), g.Main);
            b.Ell(Body, V(0, by - 0.005f, -half * 0.3f), V(girth * 0.98f, girth * 0.96f, half * 0.64f), g.Main);
        }
        Pattern(body, upright: false);
        Back(body);
        BackWings(V(girth * 0.55f, by + girth * 0.75f, half * 0.25f), 1f);
        Tail(Body, V(0, by + girth * 0.3f, -half * 0.92f), 1f + 0.15f * Math.Max(0, g.Stage));

        float hr = 0.17f * g.HeadScale * (stocky ? 1.08f : 1f);
        float neck = g.NeckLength + (hoof ? 0.12f * g.LegScale : 0f);
        var neckBase = V(0, by + girth * 0.4f, half * 0.6f);
        var hc = neckBase + V(0, girth * 0.3f + neck + hr * 0.5f, hr * 0.5f + neck * 0.3f);
        int head = b.Head(neckBase);
        if (neck > 0.03f) b.Limb(head, neckBase, hc - V(0, hr * 0.5f, hr * 0.2f), girth * 0.55f, hr * 0.55f, g.Main);
        var h = Head(head, hc, V(hr, hr * 0.92f, hr * (g.Mouth == MouthKind.Jaw ? 1.18f : 0.96f)));
        Face(h);
        Ears(h, g.Mouth == MouthKind.Trunk ? 1.7f : 1f);
        Top(h);
        if (g.Ruff) Ruff(Body, neckBase + V(0, girth * 0.15f, girth * 0.1f), hc - neckBase, girth * 0.6f, girth * 0.32f);
    }

    // ------------------------------------------------------------------ two legs

    private void Biped(bool tailed)
    {
        bool humanoid = !tailed, stocky = g.Stance == Stance.Stocky;
        float legF = g.Stance switch { Stance.Long => 1.4f, Stance.Low => 0.6f, Stance.Stocky => 0.7f, _ => 1f };
        float legLen = (humanoid ? 0.17f : 0.12f) * g.LegScale * legF;
        float footR = 0.055f * (stocky ? 1.25f : 1f);
        float tw = 0.15f * g.Girth * (stocky ? 1.35f : 1f) * (humanoid ? 0.95f : 1f);
        float th = 0.17f * g.BodyLength * (humanoid ? 1.1f : 1f) * (stocky ? 1.2f : 1f);
        float hipY = legLen + footR * 1.1f;
        float ty = hipY + th * 0.75f;
        Start(V(0, ty, 0));

        var feet = Extremity;
        float legR = 0.055f * g.Girth * (stocky ? 1.4f : 1f);
        PokeBuilder.Both(s =>
        {
            var hip = V(tw * 0.5f * s, hipY + 0.02f, 0);
            var ankle = V(tw * 0.58f * s, footR * 0.9f, 0.01f);
            int leg = b.Leg(s, hip);
            b.Limb(leg, hip, ankle, legR, legR * 0.86f, g.Main);
            Foot(leg, V(ankle.X, footR * 0.75f, 0.035f), footR, feet);
        });

        var torso = new Shape(Body, V(0, ty, 0), V(tw, th, tw * 0.88f));
        if (g.Robot) b.Box(Body, torso.C, torso.R * 0.95f, tw * 0.4f, g.Main, blend: 0.02f);
        else b.Ell(Body, torso.C, torso.R, g.Main);
        Pattern(torso, upright: true);
        if (g.Hands == HandKind.Fist) b.PaintTorus(Body, V(0, ty - th * 0.45f, 0), tw * 0.97f, 0.03f, g.Primary == PokemonType.Fighting ? Rgb(60, 56, 70) : g.Dark, default, 1f, 0.88f);
        BipedBack(torso);
        BackWings(V(tw * 0.5f, ty + th * 0.55f, -tw * 0.6f), 1f);
        if (tailed) Tail(Body, V(0, ty - th * 0.55f, -tw * 0.75f), 1f);

        float armR = 0.042f * g.Girth * (g.Hands == HandKind.Fist ? 1.25f : 1f) * (stocky ? 1.2f : 1f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(tw * 0.9f * s, ty + th * 0.5f, 0);
            var hand = shoulder + V(tw * 0.55f * s, -th * (humanoid ? 1.05f : 0.8f), 0.07f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, hand, armR, armR * 0.88f, g.Main);
            Hand(arm, hand, Vector3.Normalize(hand - shoulder), armR * 1.2f, s);
        });

        float hr = 0.19f * g.HeadScale * (humanoid ? 0.92f : 1f);
        int head = b.Head(V(0, ty + th * 0.8f, 0));
        var h = Head(head, V(0, ty + th + hr * 0.72f, 0.02f), V(hr, hr * 0.93f, hr * (g.Mouth == MouthKind.Jaw ? 1.12f : 0.92f)));
        Face(h);
        Ears(h);
        Top(h);
        if (g.Ruff) Ruff(Body, V(0, ty + th * 0.82f, 0.01f), Vector3.UnitY, tw * 0.7f, tw * 0.34f);
    }

    /// <summary>What an upright body carries on its back: a shell, a fin, a row of spikes, leaves or a flame.</summary>
    private void BipedBack(Shape torso)
    {
        var c = torso.C;
        var r = torso.R;
        switch (g.Back)
        {
            case BackKind.Shell:
            {
                var shell = PokemonGenomes.Luma(Accent) < 200f && Accent.R >= Accent.B ? Accent : Rgb(150, 100, 58);
                b.Ell(Body, c + V(0, 0, -r.Z * 0.42f), V(r.X * 1.1f, r.Y * 1.04f, r.Z * 0.78f), shell, mat: SurfaceMaterial.Shell);
                b.Torus(Body, c + V(0, 0, -r.Z * 0.12f), r.X * 1.06f, 0.032f, PixelCanvas.Light1(shell, 0.35f), V(90f, 0, 0), 1f, r.Y / r.X, SurfaceMaterial.Shell);
                break;
            }
            case BackKind.Spikes:
            case BackKind.Plates:
            case BackKind.Crystals:
            case BackKind.Fin:
            {
                int n = g.Back == BackKind.Fin ? 1 : Math.Max(2, Math.Min(5, g.Spikes));
                for (int i = 0; i < n; i++)
                {
                    float pitch = n == 1 ? 0.3f : 0.75f - 1.2f * i / (n - 1);
                    var (p, nn) = torso.At(MathF.PI, pitch);
                    var tip = p + Vector3.Normalize(nn + V(0, 0.35f, 0)) * r.Y * (g.Back == BackKind.Fin ? 0.9f : 0.5f);
                    var spike = b.Spike(Body, p - nn * 0.015f, tip, r.Y * (g.Back is BackKind.Plates or BackKind.Fin ? 0.35f : 0.18f),
                        g.Back == BackKind.Crystals ? Ice : g.Back == BackKind.Fin ? PixelCanvas.Shadow(g.Main, 0.12f) : Horn,
                        mat: g.Back == BackKind.Fin ? null : HornMaterial);
                    if (g.Back is BackKind.Plates or BackKind.Fin)
                    {
                        spike.Rotation = PokeBuilder.AlignY(tip - p);
                        spike.Stretch = V(0.3f, 1f, 1f);
                    }
                }
                break;
            }
            case BackKind.Leaves:
            case BackKind.Bulb:
            case BackKind.Flower:
            {
                var (p, _) = torso.At(MathF.PI, 0.45f);
                int leaves = b.Part("leaves", Body, p, PokeRole.Leaf);
                PokeBuilder.Both(s => b.Ell(leaves, p + V(s * r.X * 0.45f, r.Y * 0.3f, -0.04f), V(0.05f, 0.14f, 0.03f), Leaf, V(-25f, 0, -s * 30f), SurfaceMaterial.Leaf, 0.015f));
                break;
            }
            case BackKind.Flames:
            {
                var (p, _) = torso.At(MathF.PI, 0.6f);
                int flame = b.Part("flame", Body, p, PokeRole.Flame);
                PokemonModels.Flame(b, flame, p, 0.7f);
                break;
            }
        }
    }

    /// <summary>A round body on two legs with no arms (it is mostly its head): Oddish, Doduo, Torchic.</summary>
    private void Legs()
    {
        float legLen = 0.14f * g.LegScale * (g.Stance == Stance.Long ? 1.7f : 1f);
        float footR = 0.05f;
        float br = 0.24f * g.Girth;
        float hipY = legLen + footR;
        Start(V(0, hipY + br * 0.3f, 0));

        bool bird = g.Mouth is MouthKind.Beak or MouthKind.Bill;
        var leg = bird ? BeakColor : (g.Pattern == PatternKind.Socks ? g.Dark : PixelCanvas.Shadow(g.Main, 0.12f));
        PokeBuilder.Both(s =>
        {
            var hip = V(br * 0.42f * s, hipY + 0.03f, 0);
            int l = b.Leg(s, hip);
            b.Limb(l, hip, V(br * 0.46f * s, footR, 0.01f), bird ? 0.022f : 0.045f, bird ? 0.02f : 0.04f, leg);
            Foot(l, V(br * 0.46f * s, footR * 0.7f, 0.03f), footR, leg);
        });

        // A small seat for the legs on the body bone; the big round rest is the head
        b.Ell(Body, V(0, hipY + br * 0.25f, 0), V(br * 0.62f, br * 0.4f, br * 0.6f), g.Main);

        if (g.Special == Special.Tree)
        {
            // A trunk crowned with leaves, faces on the trunk
            var trunk = Rgb(150, 108, 70);
            int top = b.Head(V(0, hipY + br * 0.5f, 0));
            b.Limb(top, V(0, hipY + br * 0.4f, 0), V(0, hipY + br * 2.4f, 0), br * 0.42f, br * 0.36f, trunk);
            var crown = V(0, hipY + br * 2.6f, 0);
            int leaves = b.Part("crown", top, crown, PokeRole.Leaf);
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                b.Ell(leaves, crown + V(MathF.Sin(a) * br * 0.55f, 0.02f, MathF.Cos(a) * br * 0.55f), V(br * 0.55f, 0.035f, br * 0.25f), Leaf,
                    V(0, a * 180f / MathF.PI + 90f, -12f), SurfaceMaterial.Leaf, 0.015f);
            }
            int faces = Math.Max(1, g.Heads);
            for (int i = 0; i < faces; i++)
            {
                float ang = faces == 1 ? 0f : (i - (faces - 1) / 2f) * 0.9f;
                var c = V(MathF.Sin(ang) * br * 0.5f, hipY + br * (1.6f + 0.25f * (i % 2)), MathF.Cos(ang) * br * 0.5f);
                var hs = new Shape(top, c, V(br * 0.38f, br * 0.36f, br * 0.36f));
                b.Ell(top, hs.C, hs.R, Rgb(244, 222, 160));
                Face(hs, mouth: false);
            }
            return;
        }

        if (g.Heads >= 2)
        {
            // Several heads on long necks over a plump body
            b.Ell(Body, V(0, hipY + br * 0.55f, 0), V(br * 0.85f, br * 0.7f, br * 0.9f), g.Main);
            Pattern(new Shape(Body, V(0, hipY + br * 0.55f, 0), V(br * 0.85f, br * 0.7f, br * 0.9f)), upright: false);
            for (int i = 0; i < g.Heads; i++)
            {
                float x = (i - (g.Heads - 1) / 2f) * br * 0.7f;
                var neck = V(x * 0.6f, hipY + br * 1.05f, br * 0.2f);
                var hc = V(x, hipY + br * (2.0f + 0.2f * (i % 2)), br * 0.45f);
                int head = b.Part(i == 0 ? "head" : "head" + i, Body, neck, PokeRole.Head, i * 1.4f);
                b.Limb(head, neck, hc, 0.035f, 0.03f, g.Main);
                var hs = Head(head, hc, V(br * 0.42f, br * 0.4f, br * 0.42f));
                Face(hs);
                Top(hs);
            }
            return;
        }

        int h0 = b.Head(V(0, hipY + br * 0.45f, 0));
        var h = Head(h0, V(0, hipY + br * 0.95f, 0), V(br, br * 0.95f, br * 0.94f));
        Pattern(h, upright: true);
        Face(h, eyePitch: 0.2f);
        Ears(h);
        Top(h);
        if (bird)
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(br * 0.88f * s, hipY + br * 0.95f, 0));
                b.Ell(wing, V(br * 0.98f * s, hipY + br * 0.85f, -0.02f), V(0.04f, br * 0.4f, br * 0.36f), PixelCanvas.Shadow(g.Main, 0.1f), V(0, 0, 18f * s));
            });
        Tail(Body, V(0, hipY + br * 0.4f, -br * 0.5f), 0.9f);
    }

    // ------------------------------------------------------------------ wings

    private void Bird()
    {
        float br = 0.18f * g.Girth;
        float half = 0.23f * g.BodyLength;
        float legLen = 0.12f * g.LegScale;
        float by = legLen + br * 0.85f;
        Start(V(0, by, 0));

        var feet = BeakColor;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(br * 0.38f * s, by - br * 0.55f, 0.02f));
            b.Limb(leg, V(br * 0.38f * s, by - br * 0.6f, 0.02f), V(br * 0.42f * s, 0.035f, 0.04f), 0.022f, 0.018f, feet, SurfaceMaterial.Scales, 0.008f);
            b.Ell(leg, V(br * 0.42f * s, 0.02f, 0.07f), V(0.035f, 0.015f, 0.05f), feet, mat: SurfaceMaterial.Scales, blend: 0.008f);
        });
        var body = new Shape(Body, V(0, by, 0), V(br, br * 0.95f, half));
        b.Ell(Body, body.C, body.R, g.Main);
        Pattern(body, upright: true);

        // A fan of tail feathers, longer on big birds
        int feathers = 3 + (g.Stage >= 2 || g.Legendary ? 2 : 0);
        int tail = b.Tail(V(0, by, -half * 0.85f));
        for (int i = 0; i < feathers; i++)
        {
            float a = (i - (feathers - 1) / 2f) * (60f / feathers);
            var color = i == feathers / 2 ? g.Dark : PixelCanvas.Shadow(g.Main, 0.08f);
            float len = 0.14f * (1f + 0.25f * Math.Max(0, g.Stage)) * (g.Legendary ? 1.5f : 1f);
            b.Ell(tail, V(MathF.Sin(a * MathF.PI / 180f) * 0.08f, by - 0.01f, -half - len * 0.6f), V(0.05f, 0.028f, len), color, V(-14f, a, 0), blend: 0.012f);
        }

        Wings(V(br * 0.85f, by + br * 0.38f, 0.02f), br, half, folded: true);

        float hr = 0.13f * g.HeadScale;
        var neck = V(0, by + br * 0.45f, half * 0.4f);
        int head = b.Head(neck);
        var h = Head(head, neck + V(0, br * 0.4f + hr * 0.6f + g.NeckLength, hr * 0.55f), V(hr, hr * 0.94f, hr * 0.96f));
        // A pale face round the eyes on most birds
        if (rnd.Chance(0.5f)) b.PaintEll(head, h.C + V(0, -hr * 0.1f, hr * 0.55f), V(hr * 0.85f, hr * 0.6f, hr * 0.6f), Pale);
        Face(h, eyeYaw: 0.6f);
        Top(h);
    }

    /// <summary>A pair of wings (two pairs on insects), folded at the sides or spread, from <paramref name="shoulder"/>.</summary>
    private void Wings(Vector3 shoulder, float br, float half, bool folded)
    {
        PokeBuilder.Both(s =>
        {
            var joint = V(shoulder.X * s, shoulder.Y, shoulder.Z);
            int wing = b.Wing(s, joint);
            switch (g.Wings)
            {
                case WingKind.Dragon:
                case WingKind.Bat:
                    Membrane(wing, joint, s, br * 6f);
                    break;
                default:
                    if (folded)
                    {
                        b.Ell(wing, joint + V(s * 0.03f, -br * 0.45f, -half * 0.25f), V(0.05f, br * 0.72f, half * 0.85f), g.Dark, V(-15f, 0, 12f * s));
                        b.Ell(wing, joint + V(s * 0.04f, -br * 0.5f, -half * 0.12f), V(0.04f, br * 0.45f, half * 0.5f), PixelCanvas.Shadow(g.Main, 0.05f), V(-15f, 0, 12f * s), blend: 0.01f);
                    }
                    else
                        b.Ell(wing, joint + V(s * br * 1.0f, br * 0.3f, -half * 0.1f), V(br * 1.1f, 0.03f, half * 0.6f), g.Dark, V(0, 0, -20f * s), blend: 0.015f);
                    break;
            }
        });
    }

    private void Bat()
    {
        float r = 0.19f * g.Girth;
        float by = 0.06f + r;
        Start(V(0, by, 0));
        var body = new Shape(Body, V(0, by, 0), V(r, r * 0.95f, r * 0.9f));
        b.Ell(Body, body.C, body.R, g.Main);
        Pattern(body, upright: true);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(r * 0.4f * s, by - r * 0.7f, 0));
            b.Limb(leg, V(r * 0.4f * s, by - r * 0.7f, 0), V(r * 0.45f * s, 0.03f, 0.02f), 0.024f, 0.02f, g.Dark, blend: 0.01f);
            b.Ell(leg, V(r * 0.45f * s, 0.025f, 0.04f), V(0.03f, 0.02f, 0.04f), g.Dark, blend: 0.008f);
        });
        Wings(V(r * 0.8f, by + r * 0.3f, -0.02f), r, r, folded: false);
        Tail(Body, V(0, by - r * 0.4f, -r * 0.8f), 0.8f);

        float hr = r * 0.85f * g.HeadScale;
        int head = b.Head(V(0, by + r * 0.6f, 0.02f));
        var h = Head(head, V(0, by + r * 0.9f + hr * 0.45f, r * 0.15f), V(hr, hr * 0.9f, hr * 0.9f));
        Face(h);
        Ears(h, 1.35f);
        Top(h);
    }

    private void Insect()
    {
        float by = 0.42f;
        Start(V(0, by, 0));
        bool lean = g.BodyLength > 1.1f;
        var thorax = new Shape(Body, V(0, by, 0), V(0.11f, 0.1f, 0.12f) * g.Girth);
        b.Ell(Body, thorax.C, thorax.R, g.Main);
        var abdomen = lean
            ? new Shape(Body, V(0, by - 0.02f, -0.3f), V(0.055f, 0.055f, 0.3f) * g.Girth)
            : new Shape(Body, V(0, by - 0.05f, -0.22f), V(0.12f, 0.11f, 0.19f) * g.Girth);
        b.Ell(Body, abdomen.C, abdomen.R, g.Pattern == PatternKind.Stripes ? Accent : g.Main, V(lean ? 6f : 18f, 0, 0));
        if (g.Pattern == PatternKind.Stripes)
            for (int i = 0; i < 3; i++)
                b.PaintTorus(Body, abdomen.C + V(0, 0, abdomen.R.Z * (0.4f - 0.4f * i)), abdomen.R.X * 0.98f, 0.022f, Rgb(50, 46, 58), V(90f, 0, 0), 1f, abdomen.R.Y / abdomen.R.X);
        else Pattern(abdomen, upright: false);
        if (g.Primary == PokemonType.Poison || g.Secondary == PokemonType.Poison)
            b.Spike(Body, abdomen.C - V(0, abdomen.R.Y * 0.3f, abdomen.R.Z * 0.85f), abdomen.C - V(0, abdomen.R.Y * 0.5f, abdomen.R.Z * 1.45f), 0.03f, Horn, mat: HornMaterial);

        // Two pairs of thin legs, dangling
        for (int pair = 0; pair < 2; pair++)
        {
            float z = 0.05f - pair * 0.1f;
            PokeBuilder.Both(s =>
            {
                var hip = V(0.07f * s, by - 0.06f, z);
                int leg = b.Leg(s, hip, pair == 0);
                b.Limb(leg, hip, V(0.13f * s, by - 0.18f, z + 0.02f), 0.017f, 0.015f, g.Dark, blend: 0.008f);
                b.Limb(leg, V(0.13f * s, by - 0.18f, z + 0.02f), V(0.15f * s, by - 0.3f, z + 0.05f), 0.015f, 0.012f, g.Dark, blend: 0.006f);
            });
        }

        if (g.Hands == HandKind.Blade || g.Hands == HandKind.Pincer)
            PokeBuilder.Both(s =>
            {
                var shoulder = V(0.09f * s, by + 0.03f, 0.08f);
                int arm = b.Arm(s, shoulder);
                var hand = shoulder + V(0.06f * s, -0.04f, 0.1f);
                b.Limb(arm, shoulder, hand, 0.026f, 0.024f, g.Main);
                Hand(arm, hand, Vector3.Normalize(hand - shoulder), 0.03f, s);
            });

        // Wings: a big pair over a smaller one, clear or patterned
        bool butterfly = g.Wings == WingKind.Butterfly;
        var wingColor = butterfly ? Accent : Rgb(226, 238, 248);
        for (int pair = 0; pair < 2; pair++)
        {
            float k = pair == 0 ? 1f : 0.72f;
            PokeBuilder.Both(s =>
            {
                var joint = V(0.07f * s, by + 0.08f, 0.02f - pair * 0.07f);
                int wing = b.Wing(s, joint);
                var c = joint + V(s * 0.22f * k, 0.12f * k - pair * 0.06f, -0.05f - pair * 0.04f);
                var radii = butterfly ? V(0.26f, 0.022f, 0.2f) * k : V(0.24f, 0.022f, 0.1f) * k;
                b.Ell(wing, c, radii, wingColor, V(-6f, 20f * s * (pair == 0 ? 1f : -0.5f), -s * (pair == 0 ? 30f : 8f)), blend: 0.012f);
                // The wing's root just outside the thorax: a vein from inside it out to the wing's middle holds them together
                b.Limb(wing, Vector3.Lerp(joint, thorax.C, 0.4f), c, 0.018f * k, 0.008f * k, PixelCanvas.Shadow(wingColor, 0.3f), blend: 0.008f);
                if (butterfly && pair == 0)
                {
                    var n = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, Quaternion.CreateFromYawPitchRoll(20f * s * (pair == 0 ? 1f : -0.5f) * MathF.PI / 180f,
                        -6f * MathF.PI / 180f, -s * (pair == 0 ? 30f : 8f) * MathF.PI / 180f)));
                    b.Mark(wing, c + n * 0.022f, n, radii.X * 0.32f, radii.X * 0.32f, PokemonGenomes.Luma(Accent) > 140f ? g.Dark : Rgb(248, 244, 236), MarkShape.Ring);
                }
            });
        }

        int head = b.Head(V(0, by + 0.04f, 0.1f));
        var h = Head(head, V(0, by + 0.06f, 0.18f), V(0.12f, 0.11f, 0.11f) * g.HeadScale);
        Face(h, eyeYaw: 0.55f, eyeScale: 1.25f);
        Top(h);
    }

    // ------------------------------------------------------------------ long bodies

    private void Serpent()
    {
        int segs = Math.Max(2, g.Segments);
        bool slug = segs <= 2;
        float r0 = (slug ? 0.12f : 0.085f) * g.Girth;
        var points = new List<Vector3>();
        if (slug)
        {
            points.Add(V(0, r0, -0.3f));
            points.Add(V(0, r0, -0.08f));
            points.Add(V(0, r0 * 1.1f, 0.12f));
            points.Add(V(0, r0 * 2.6f, 0.2f));
        }
        else
        {
            // A coil on the ground, then the neck rising to the head
            float rad = 0.16f + 0.015f * segs;
            int coil = segs - 1;
            for (int i = 0; i < coil; i++)
            {
                float a = -2.4f + i * (3.6f / Math.Max(1, coil - 1));
                points.Add(V(MathF.Sin(a) * rad, r0 * (1f - 0.04f * i), MathF.Cos(a) * rad * 0.9f - 0.04f));
            }
            var last = points[^1];
            points.Add(V(last.X * 0.4f, 0.3f * g.LegScale + r0, 0.18f));
            points.Add(V(0, 0.46f * g.LegScale + r0, 0.22f));
        }
        Start(points[0]);
        bool rocks = g.Rocky || g.Metallic;
        int parent = Body;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int bone = i == 0 ? Body : b.Part("seg" + i, parent, points[i], PokeRole.Segment, i);
            float ra = r0 * (1f - 0.05f * i), rb = r0 * (1f - 0.05f * (i + 1));
            if (rocks)
            {
                // Boulders strung close enough to touch, however long the segment and thin the body
                int beads = Math.Max(2, (int)MathF.Ceiling(Vector3.Distance(points[i], points[i + 1]) / (0.9f * (ra + rb))));
                for (int k = 0; k < beads; k++)
                {
                    float t = k / (float)beads, rr = (ra + (rb - ra) * t) * (k % 2 == 0 ? 1.25f : 1.1f);
                    b.Ell(bone, Vector3.Lerp(points[i], points[i + 1], t), V(rr, rr, rr), k % 2 == 0 ? g.Main : PixelCanvas.Shadow(g.Main, 0.06f), blend: 0.01f);
                }
            }
            else b.Limb(bone, points[i], points[i + 1], ra, rb, g.Main);
            if (g.Pattern is PatternKind.Spots or PatternKind.Stripes or PatternKind.Mask)
            {
                // On the top of the segment, or on the back of the neck where it rises straight up
                var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
                var axis = Vector3.Normalize(points[i + 1] - points[i]);
                var up = Vector3.UnitY - axis * Vector3.Dot(Vector3.UnitY, axis);
                if (up.LengthSquared() < 0.1f) up = -Vector3.UnitZ - axis * Vector3.Dot(-Vector3.UnitZ, axis);
                up = Vector3.Normalize(up);
                float r = rocks ? ra * 1.15f : (ra + rb) * 0.5f;
                b.Mark(bone, mid + up * r, up, r * 0.5f, r * 0.5f, Accent, g.Pattern == PatternKind.Stripes ? MarkShape.Bar : MarkShape.Disc);
            }
            parent = bone;
        }
        // The tail tapers off behind the coil
        int tail = b.Tail(points[0]);
        var tailEnd = points[0] + (slug ? V(0, -r0 * 0.4f, -0.12f) : V(-0.16f, -r0 * 0.3f, -0.1f));
        b.Limb(tail, points[0], tailEnd, r0, r0 * 0.25f, g.Main);
        if (g.TailTip == TipKind.Spike || g.Primary == PokemonType.Dragon) b.Spike(tail, tailEnd, tailEnd + Vector3.Normalize(tailEnd - points[0]) * 0.08f, r0 * 0.3f, Horn, 0.5f, HornMaterial);
        if (slug && (g.Rocky || g.Back == BackKind.Shell))
            b.Ell(Body, points[1] + V(0, r0 * 1.2f, -0.02f), V(r0 * 1.3f, r0 * 1.2f, r0 * 1.3f), Rgb(150, 120, 100), mat: SurfaceMaterial.Shell);

        float hr = r0 * 1.45f * g.HeadScale;
        var neckTop = points[^1];
        int head = b.Head(neckTop, parent);
        var hc = neckTop + V(0, hr * 0.25f, hr * 0.55f);
        var h = Head(head, hc, V(hr, hr * 0.82f, hr * 1.25f));
        if (rocks) b.Spike(head, hc + V(0, hr * 0.6f, -hr * 0.2f), hc + V(0, hr * 1.5f, -hr * 0.6f), hr * 0.3f, Horn, mat: HornMaterial);
        Face(h, eyeYaw: 0.55f, eyePitch: 0.25f);
        Ears(h);
        Top(h);
    }

    private void Crawler()
    {
        int segs = Math.Max(3, g.Segments);
        float r0 = 0.1f * g.Girth;
        Start(V(0, r0, 0));
        bool striped = g.Pattern == PatternKind.Stripes;
        int parent = Body;
        for (int i = 0; i < segs; i++)
        {
            float r = r0 * (1f - 0.07f * i);
            var c = V(0, r, -i * r0 * 1.45f);
            int bone = i == 0 ? Body : b.Part("seg" + i, parent, c + V(0, 0, r0 * 0.7f), PokeRole.Segment, i);
            b.Ell(bone, c, V(r * 1.02f, r, r * 0.95f), striped && i % 2 == 1 ? Accent : g.Main, blend: 0.03f);
            PokeBuilder.Both(s => b.Ell(bone, c + V(s * r * 0.55f, -r * 0.8f, r * 0.2f), V(r * 0.22f, r * 0.2f, r * 0.24f), g.Dark, blend: 0.01f));
            if (g.Pattern == PatternKind.Spots && i > 0)
                PokeBuilder.Both(s =>
                {
                    var (p, n) = On(c, V(r * 1.02f, r, r * 0.95f), s * 0.9f, 0.5f);
                    b.Mark(bone, p, n, r * 0.22f, r * 0.22f, Accent);
                });
            parent = bone;
        }
        if (g.Primary == PokemonType.Poison || g.Secondary == PokemonType.Poison || g.TailTip == TipKind.Spike)
        {
            var end = V(0, r0 * 0.75f, -(segs - 1) * r0 * 1.45f - r0 * 0.6f);
            b.Spike(parent, end, end + V(0, r0 * 0.5f, -r0 * 0.9f), r0 * 0.3f, Horn, mat: HornMaterial);
        }

        float hr = r0 * 1.3f * g.HeadScale;
        int head = b.Head(V(0, r0 * 1.2f, r0 * 0.5f));
        var h = Head(head, V(0, r0 * 1.9f, r0 * 1.15f), V(hr, hr * 0.95f, hr * 0.9f));
        if (g.Primary == PokemonType.Poison || g.Secondary == PokemonType.Poison)
            b.Spike(head, h.C + V(0, hr * 0.7f, -hr * 0.1f), h.C + V(0, hr * 1.6f, hr * 0.15f), hr * 0.25f, Horn, mat: HornMaterial);
        Face(h, eyeYaw: 0.5f, eyePitch: 0.15f, eyeScale: 1.15f);
        Top(h);
    }

    private void Arthropod()
    {
        bool spider = g.LegPairs >= 3 && g.Hands != HandKind.Pincer;
        float br = 0.19f * g.Girth, bh = 0.12f * g.Girth, bl = 0.17f * g.BodyLength;
        float legLen = 0.12f * g.LegScale;
        float by = legLen + bh * 0.8f;
        Start(V(0, by, 0));

        var body = new Shape(Body, V(0, by, 0), V(br * (spider ? 0.8f : 1.15f), bh, bl));
        b.Ell(Body, body.C, body.R, g.Main);
        if (spider)
        {
            var abdomen = new Shape(Body, V(0, by + bh * 0.25f, -bl * 1.25f), V(br * 1.05f, bh * 1.35f, bl * 1.1f));
            b.Ell(Body, abdomen.C, abdomen.R, g.Main);
            Pattern(abdomen, upright: false);
        }
        else Pattern(body, upright: false);
        if (g.Back == BackKind.Shell) b.Ell(Body, body.C + V(0, bh * 0.35f, -bl * 0.1f), body.R * V(1.08f, 0.9f, 1.05f), Accent, mat: SurfaceMaterial.Shell);
        else if (g.Back is BackKind.Spikes or BackKind.Plates or BackKind.Crystals) Back(body);

        // Jointed legs splayed out to the sides
        for (int pair = 0; pair < g.LegPairs; pair++)
        {
            float z = g.LegPairs == 1 ? 0f : bl * (0.55f - 1.1f * pair / (g.LegPairs - 1));
            PokeBuilder.Both(s =>
            {
                var hip = V(body.R.X * 0.8f * s, by - bh * 0.15f, z);
                var knee = V(body.R.X * (1.45f + 0.15f * pair) * s, by + bh * 0.55f, z + 0.02f);
                var foot = V(body.R.X * (1.9f + 0.2f * pair) * s, 0.025f, z + 0.03f * (1 - pair));
                int leg = b.Leg(s, hip, pair == 0);
                b.Limb(leg, hip, knee, 0.03f, 0.024f, g.Main, blend: 0.01f);
                b.Limb(leg, knee, foot, 0.024f, 0.014f, g.Main, blend: 0.008f);
            });
        }

        if (g.Hands == HandKind.Pincer)
            PokeBuilder.Both(s =>
            {
                var shoulder = V(body.R.X * 0.75f * s, by + bh * 0.1f, bl * 0.7f);
                int arm = b.Arm(s, shoulder);
                var hand = shoulder + V(body.R.X * 0.45f * s, bh * 0.4f, bl * 0.65f);
                b.Limb(arm, shoulder, hand, 0.034f, 0.03f, g.Main);
                Hand(arm, hand, Vector3.Normalize(hand - shoulder), 0.045f, s);
            });

        // A scorpion's tail curls up over the back
        if (g.Category.Contains("corpion", StringComparison.OrdinalIgnoreCase) || g.TailTip == TipKind.Spike)
        {
            var root = V(0, by + bh * 0.2f, -bl * 0.95f);
            int tail = b.Tail(root);
            var mid = root + V(0, bh * 1.6f, -bl * 0.6f);
            var end = mid + V(0, bh * 1.4f, bl * 0.5f);
            b.Limb(tail, root, mid, 0.045f, 0.038f, g.Main);
            b.Limb(tail, mid, end, 0.038f, 0.03f, g.Main);
            b.Spike(tail, end, end + V(0, -bh * 0.3f, bl * 0.55f), 0.035f, Horn, mat: HornMaterial);
        }

        float hr = br * 0.55f * g.HeadScale;
        int head = b.Head(V(0, by, bl * 0.75f));
        var h = Head(head, V(0, by + bh * 0.1f, bl * 0.95f), V(hr, hr * 0.85f, hr * 0.8f));
        if (!spider && g.Hands == HandKind.Pincer && rnd.Chance(0.6f))
        {
            // Eyes on stalks
            PokeBuilder.Both(s =>
            {
                var root = h.C + V(s * hr * 0.4f, hr * 0.5f, 0);
                var top = root + V(s * hr * 0.25f, hr * 0.9f, hr * 0.1f);
                b.Limb(head, root, top, 0.018f, 0.016f, g.Main, blend: 0.008f);
                var ball = new Shape(head, top + V(0, 0.03f, 0), V(0.05f, 0.05f, 0.05f));
                b.Ell(head, ball.C, ball.R, Rgb(244, 240, 232), blend: 0.008f);
                var (p, n) = ball.At(s * 0.2f, 0.1f);
                b.Eye(head, p, n, 0.016f, null, false);
            });
            Mouth(h);
        }
        else Face(h, eyeYaw: 0.45f, eyePitch: 0.2f);
        Top(h);
    }

    private void Fish()
    {
        bool eel = g.BodyLength > 1.12f;
        float half = 0.32f * g.BodyLength * (eel ? 1.3f : 1f);
        float ht = 0.21f * g.Girth * (eel ? 0.62f : 1f);
        float wd = 0.14f * g.Girth * (eel ? 0.75f : 1f);
        float by = 0.4f;
        Start(V(0, by, 0));
        var body = new Shape(Body, V(0, by, 0), V(wd, ht, half));
        b.Ell(Body, body.C, body.R, g.Main);
        b.PaintEll(Body, body.C + V(0, -ht * 0.55f, half * 0.05f), V(wd * 0.92f, ht * 0.55f, half * 0.85f), g.Belly);
        if (g.Pattern == PatternKind.Stripes || g.Pattern == PatternKind.Mask)
            for (int i = 0; i < 2; i++)
                b.PaintTorus(Body, body.C + V(0, 0, half * (0.1f - 0.35f * i)), wd * 0.98f, 0.03f, g.Dark, V(90f, 0, 0), 1f, ht / wd);
        else if (g.Pattern == PatternKind.Spots) Pattern(body, upright: false);
        if (g.Spikes > 0 || g.Back == BackKind.Spikes)
            for (int i = 0; i < 8; i++)
            {
                var (p, n) = body.At(i * MathF.Tau / 8f, (i % 2 == 0 ? 0.35f : -0.25f));
                b.Spike(Body, p - n * 0.01f, p + n * ht * 0.4f, ht * 0.12f, Horn, mat: HornMaterial);
            }

        // The tail with its fin, a fin on the back and one at each side
        int tail = b.Tail(V(0, by, -half * 0.75f));
        var tailEnd = V(0, by, -half * 1.12f);
        b.Limb(tail, V(0, by, -half * 0.75f), tailEnd, wd * 0.55f, wd * 0.32f, g.Main);
        int fin = b.Part("tailFin", tail, tailEnd, PokeRole.Fin);
        var finColor = PixelCanvas.Mix(g.Main, Accent, 0.6f);
        b.Ell(fin, tailEnd + V(0, ht * 0.45f, -ht * 0.35f), V(0.022f, ht * 0.62f, ht * 0.32f), finColor, V(-30f, 0, 0), blend: 0.012f);
        b.Ell(fin, tailEnd + V(0, -ht * 0.45f, -ht * 0.35f), V(0.022f, ht * 0.62f, ht * 0.32f), finColor, V(30f, 0, 0), blend: 0.012f);
        int dorsal = b.Part("dorsal", Body, V(0, by + ht * 0.85f, 0), PokeRole.Fin);
        var top = V(0, by + ht * 1.55f, -half * 0.3f);
        var dorsalFin = b.Spike(dorsal, V(0, by + ht * 0.8f, half * 0.1f), top, half * 0.22f, finColor);
        dorsalFin.Rotation = PokeBuilder.AlignY(top - V(0, by + ht * 0.8f, half * 0.1f));
        dorsalFin.Stretch = V(0.28f, 1f, 1f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(wd * 0.85f * s, by - ht * 0.3f, half * 0.2f), PokeRole.Fin, s, s);
            b.Ell(side, V(wd * 1.3f * s, by - ht * 0.38f, half * 0.12f), V(0.08f, 0.022f, 0.06f), finColor, V(0, 0, 25f * s), blend: 0.012f);
        });

        int head = b.Head(V(0, by + 0.02f, half * 0.4f));
        var h = new Shape(head, V(0, by + ht * 0.08f, half * 0.55f), V(wd * 0.96f, ht * 0.88f, half * 0.46f));
        b.Ell(head, h.C, h.R, g.Main);
        b.PaintEll(head, h.C + V(0, -h.R.Y * 0.55f, h.R.Z * 0.1f), V(h.R.X * 0.9f, h.R.Y * 0.5f, h.R.Z * 0.9f), g.Belly);
        var (mp, mn) = h.At(0f, -0.12f);
        b.Mark(head, mp, mn, h.R.X * 0.35f, h.R.Y * 0.08f, PixelCanvas.Shadow(g.Main, 0.55f), MarkShape.Bar);
        if (g.Whiskers) Whiskers(head, h.C + V(0, -h.R.Y * 0.2f, h.R.Z * 0.6f), h.R * 0.5f);
        Face(h, eyeYaw: 0.95f, eyePitch: 0.22f, mouth: false);
        if (g.Top is TopKind.Horn or TopKind.Horns or TopKind.Crest or TopKind.Gem or TopKind.Crown) Top(h);
    }

    // ------------------------------------------------------------------ heads without bodies

    private void Ball()
    {
        float r = 0.29f * g.Girth;
        Start(V(0, r, 0));
        var body = new Shape(Body, V(0, r, 0), V(r, r * 0.97f, r * 0.96f));
        switch (g.Special)
        {
            case Special.Bivalve:
            {
                // A soft body between two halves of a shell
                b.Ell(Body, body.C, body.R * 0.82f, g.Main);
                var shell = PixelCanvas.Shadow(g.Main, 0.3f);
                b.Ell(Body, body.C + V(0, r * 0.45f, -r * 0.25f), V(r * 1.15f, r * 0.42f, r * 1.0f), shell, V(-35f, 0, 0), SurfaceMaterial.Shell, 0.01f);
                b.Ell(Body, body.C + V(0, -r * 0.55f, -r * 0.05f), V(r * 1.15f, r * 0.38f, r * 1.0f), shell, V(10f, 0, 0), SurfaceMaterial.Shell, 0.01f);
                Face(new Shape(Body, body.C + V(0, 0, r * 0.1f), body.R * 0.82f), eyePitch: 0f);
                return;
            }
            case Special.Pumpkin:
                Pumpkin(body);
                break;
            case Special.Crescent:
            {
                b.Ell(Body, body.C, body.R, g.Main);
                // Bitten from behind on one side, so the face stays whole
                b.Sdf.Ellipsoid(body.C + V(r * 0.62f, r * 0.22f, -r * 0.2f), V(r * 0.62f, r * 0.72f, r * 0.95f), g.Main, Body, 0.02f, g.Coat, null, SdfOp.Cut);
                break;
            }
            case Special.Gear:
                b.Torus(Body, body.C, r * 0.72f, r * 0.26f, g.Main, V(90f, 0, 0), mat: SurfaceMaterial.Metal, blend: 0.01f);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.Tau / 8f;
                    b.Box(Body, body.C + V(MathF.Cos(a) * r, MathF.Sin(a) * r, 0), V(r * 0.16f, r * 0.16f, r * 0.2f), 0.01f, g.Main, V(0, 0, a * 180f / MathF.PI), SurfaceMaterial.Metal, 0.01f);
                }
                b.Ell(Body, body.C, V(r * 0.5f, r * 0.5f, r * 0.3f), PixelCanvas.Light1(g.Main, 0.1f), mat: SurfaceMaterial.Metal, blend: 0.02f);
                Face(new Shape(Body, body.C, V(r * 0.5f, r * 0.5f, r * 0.3f)), eyePitch: 0.15f, mouth: false);
                return;
            case Special.Cotton:
                b.Ell(Body, body.C, body.R * 0.75f, g.Main);
                for (int i = 0; i < 7; i++)
                {
                    var (p, n) = body.At(i * MathF.Tau / 7f + 0.3f, i % 2 == 0 ? 0.45f : -0.1f);
                    b.Ell(Body, p - n * r * 0.1f, V(r * 0.36f, r * 0.32f, r * 0.34f), Rgb(250, 248, 244), blend: 0.04f);
                }
                Face(new Shape(Body, body.C, body.R * 0.75f), eyePitch: 0.05f);
                return;
            default:
                if (g.Robot || g.Special == Special.Box) b.Box(Body, body.C, body.R * 0.88f, r * 0.35f, g.Main, blend: 0.02f);
                else b.Ell(Body, body.C, body.R, g.Main);
                break;
        }
        Pattern(body, upright: true);
        if (g.Spikes > 0)
            for (int i = 0; i < g.Spikes; i++)
            {
                float yaw = i * MathF.Tau / g.Spikes + 0.4f;
                var (p, n) = body.At(yaw, i % 2 == 0 ? 0.25f : -0.2f);
                b.Spike(Body, p - n * 0.02f, p + n * r * 0.42f, r * 0.13f, g.Special == Special.Sun ? Rgb(252, 180, 60) : g.Icy ? Ice : Horn,
                    mat: g.Special == Special.Sun ? SurfaceMaterial.Glow : HornMaterial);
            }
        Tail(Body, V(0, r * 0.55f, -r * 0.7f), 1.15f);
        Face(body, eyePitch: 0.12f);
        Ears(body, 0.8f);
        Top(body);
    }

    /// <summary>A ribbed round body with a stem: five swellings round the middle.</summary>
    private void Pumpkin(Shape body)
    {
        var c = body.C;
        var r = body.R;
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            b.Ell(Body, c + V(MathF.Sin(a) * r.X * 0.35f, 0, MathF.Cos(a) * r.Z * 0.35f), V(r.X * 0.7f, r.Y * 0.92f, r.Z * 0.7f), i % 2 == 0 ? g.Main : PixelCanvas.Shadow(g.Main, 0.06f), blend: 0.03f);
        }
        b.Limb(Body, c + V(0, r.Y * 0.8f, 0), c + V(r.X * 0.12f, r.Y * 1.2f, -r.Z * 0.05f), r.X * 0.12f, r.X * 0.09f, Rgb(110, 84, 54), SurfaceMaterial.Shell);
    }

    private void Armed()
    {
        float r = 0.27f * g.Girth;
        bool sits = !g.Hovers;
        Start(V(0, r + 0.02f, 0));
        var body = new Shape(Body, V(0, r + 0.02f, 0), V(r, r * 1.04f, r * 0.94f));
        switch (g.Special)
        {
            case Special.Balloon:
                b.Ell(Body, body.C + V(0, r * 0.15f, 0), V(r, r * 1.08f, r * 0.98f), g.Main);
                body = new Shape(Body, body.C + V(0, r * 0.15f, 0), V(r, r * 1.08f, r * 0.98f));
                break;
            case Special.Bell:
                // A dome that flares into a skirt, which carries the rim
                b.Ell(Body, body.C + V(0, r * 0.2f, 0), V(r * 0.8f, r * 0.85f, r * 0.8f), g.Main, mat: SurfaceMaterial.Metal);
                b.Ell(Body, body.C + V(0, -r * 0.3f, 0), V(r * 0.86f, r * 0.36f, r * 0.86f), g.Main, mat: SurfaceMaterial.Metal, blend: 0.04f);
                b.Torus(Body, body.C + V(0, -r * 0.5f, 0), r * 0.82f, r * 0.2f, PixelCanvas.Shadow(g.Main, 0.1f), mat: SurfaceMaterial.Metal);
                body = new Shape(Body, body.C + V(0, r * 0.2f, 0), V(r * 0.8f, r * 0.85f, r * 0.8f));
                break;
            default:
                if (g.Robot) b.Box(Body, body.C, body.R * 0.9f, r * 0.35f, g.Main, blend: 0.02f);
                else b.Ell(Body, body.C, body.R, g.Main);
                if (sits) b.Ell(Body, V(0, r * 0.4f, 0), V(r * 1.05f, r * 0.45f, r * 1.0f), PixelCanvas.Shadow(g.Main, 0.05f), blend: 0.05f);
                break;
        }
        Pattern(body, upright: true);

        // Arms reaching out and forward (strings that hang down on a balloon)
        float armR = 0.045f * g.Girth * (g.Hands == HandKind.Fist ? 1.3f : 1f);
        PokeBuilder.Both(s =>
        {
            if (g.Special == Special.Balloon)
            {
                var root = body.C + V(s * r * 0.25f, -body.R.Y * 0.95f, 0.02f);
                int str = b.Arm(s, root);
                b.Limb(str, root, root + V(s * 0.04f, -r * 0.7f, 0.04f), 0.014f, 0.012f, Rgb(240, 236, 220), blend: 0.006f);
                b.Ell(str, root + V(s * 0.04f, -r * 0.75f, 0.04f), V(0.035f, 0.035f, 0.02f), Accent, blend: 0.006f);
                return;
            }
            var shoulder = body.C + V(s * body.R.X * 0.82f, body.R.Y * 0.1f, 0.02f);
            var hand = shoulder + V(s * r * 0.62f, -r * 0.25f, r * 0.32f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, hand, armR, armR * 0.85f, g.Main);
            Hand(arm, hand, Vector3.Normalize(hand - shoulder), armR * 1.3f, s);
        });
        Tail(Body, body.C + V(0, -body.R.Y * 0.4f, -body.R.Z * 0.75f), 1.2f);
        Face(body, eyePitch: 0.2f);
        Ears(body, 0.8f);
        Top(body);
    }

    private void Blob()
    {
        float r = 0.26f * g.Girth;
        Start(V(0, r * 0.7f, 0));
        switch (g.Special)
        {
            case Special.Star:
            {
                var c = V(0, r * 1.25f, 0);
                for (int i = 0; i < 5; i++)
                {
                    float a = MathF.PI / 2f + i * MathF.Tau / 5f;
                    var tip = c + V(MathF.Cos(a) * r * 1.35f, MathF.Sin(a) * r * 1.35f, 0);
                    var arm = b.Spike(Body, c, tip, r * 0.42f, g.Main);
                    arm.Rotation = PokeBuilder.AlignY(tip - c);
                    arm.Stretch = V(1f, 1f, 0.5f);
                }
                var core = new Shape(Body, c + V(0, 0, r * 0.08f), V(r * 0.42f, r * 0.42f, r * 0.3f));
                b.Ell(Body, core.C, core.R, g.Main);
                b.Ell(Body, core.C + V(0, -r * 0.12f, r * 0.22f), V(r * 0.14f, r * 0.14f, r * 0.1f), Rgb(222, 64, 76), mat: SurfaceMaterial.Shell, blend: 0.006f);
                Face(core, eyeYaw: 0.5f, eyePitch: 0.3f, mouth: false);
                return;
            }
            case Special.Candle:
            {
                var wax = new Shape(Body, V(0, r * 0.85f, 0), V(r * 0.6f, r * 0.85f, r * 0.6f));
                b.Sdf.Cylinder(wax.C, wax.R.X, wax.R.Y, r * 0.2f, g.Main, Body, 0.02f, g.Coat);
                b.Ell(Body, V(r * 0.35f, r * 1.4f, r * 0.42f), V(r * 0.12f, r * 0.3f, r * 0.12f), g.Main, blend: 0.03f);
                var wick = V(0, r * 1.72f, 0);
                int flame = b.Part("flame", Body, wick, PokeRole.Flame);
                PokemonModels.Flame(b, flame, wick, 0.85f);
                Face(new Shape(Body, wax.C + V(0, r * 0.1f, 0), V(wax.R.X, wax.R.Y * 0.8f, wax.R.X)), eyePitch: 0.15f);
                return;
            }
            case Special.Mound:
            {
                var earth = Rgb(156, 112, 76);
                b.Ell(Body, V(0, r * 0.25f, 0), V(r * 1.35f, r * 0.4f, r * 1.25f), earth, mat: SurfaceMaterial.Scales);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f + 0.5f;
                    b.Ell(Body, V(MathF.Sin(a) * r * 1.1f, r * 0.3f, MathF.Cos(a) * r * 1.0f), V(r * 0.32f, r * 0.24f, r * 0.3f), PixelCanvas.Shadow(earth, 0.1f), blend: 0.03f);
                }
                int up = b.Head(V(0, r * 0.6f, 0));
                var h = Head(up, V(0, r * 1.2f, 0.02f), V(r * 0.58f, r * 0.9f, r * 0.56f));
                Face(h, eyePitch: 0.35f, mouth: false);
                var (np, nn) = h.At(0f, 0.05f);
                b.Ell(up, np + nn * r * 0.04f, V(r * 0.2f, r * 0.14f, r * 0.12f), Rgb(232, 120, 140), blend: 0.01f);
                Top(h);
                return;
            }
            case Special.Cone:
            {
                var waffle = Rgb(214, 172, 112);
                b.Limb(Body, V(0, r * 0.12f, 0), V(0, r * 0.95f, 0), r * 0.12f, r * 0.62f, waffle, SurfaceMaterial.Shell);
                int scoop = b.Head(V(0, r * 1.0f, 0));
                var h = Head(scoop, V(0, r * 1.35f, 0), V(r * 0.72f, r * 0.6f, r * 0.7f));
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.Tau / 6f;
                    b.Ell(scoop, h.C + V(MathF.Sin(a) * r * 0.6f, -r * 0.35f, MathF.Cos(a) * r * 0.6f), V(r * 0.2f, r * 0.16f, r * 0.2f), g.Main, blend: 0.03f);
                }
                Face(h, eyePitch: 0.05f);
                Top(h);
                return;
            }
            case Special.Sword:
            {
                var tip = V(0, r * 0.05f, 0);
                var hilt = V(0, r * 2.2f, 0);
                var blade = b.Spike(Body, hilt, tip, r * 0.32f, Rgb(208, 214, 226), mat: SurfaceMaterial.Metal);
                blade.Rotation = PokeBuilder.AlignY(tip - hilt);
                blade.Stretch = V(1f, 1f, 0.22f);
                b.Box(Body, hilt + V(0, r * 0.05f, 0), V(r * 0.5f, r * 0.08f, r * 0.12f), 0.02f, Gold, mat: SurfaceMaterial.Metal);
                int grip = b.Head(hilt);
                var h = Head(grip, hilt + V(0, r * 0.4f, 0), V(r * 0.32f, r * 0.34f, r * 0.3f));
                Face(h, eyeYaw: 0.35f, eyePitch: 0.05f, mouth: false);
                return;
            }
        }

        if (g.Special == Special.Box || g.Robot) b.Box(Body, V(0, r * 0.75f, 0), V(r * 0.9f, r * 0.75f, r * 0.8f), r * 0.2f, g.Main, blend: 0.02f);
        else b.Ell(Body, V(0, r * 0.62f, 0), V(r * 1.08f, r * 0.64f, r * 1.0f), g.Main);
        var base0 = new Shape(Body, V(0, r * 0.62f, 0), V(r * 1.08f, r * 0.64f, r * 1.0f));
        int head = b.Head(V(0, r * 1.05f, 0));
        var top = g.Special == Special.Pumpkin
            ? new Shape(head, V(0, r * 1.35f, 0.01f), V(r * 0.8f, r * 0.72f, r * 0.76f))
            : Head(head, V(0, r * 1.42f, 0.01f), V(r * 0.8f, r * 0.82f, r * 0.76f));
        if (g.Special == Special.Pumpkin)
        {
            b.Ell(head, top.C, top.R, g.Main);
            b.Limb(head, top.C + V(0, top.R.Y * 0.8f, 0), top.C + V(0, top.R.Y * 1.3f, -0.03f), 0.03f, 0.024f, Rgb(110, 84, 54), SurfaceMaterial.Shell);
        }
        Pattern(base0, upright: true);
        if (g.Hands is HandKind.Leaf or HandKind.Flipper || (g.Primary == PokemonType.Water && rnd.Chance(0.5f)))
            PokeBuilder.Both(s =>
            {
                var shoulder = V(s * r * 0.95f, r * 0.8f, 0.03f);
                int arm = b.Arm(s, shoulder);
                b.Ell(arm, shoulder + V(s * r * 0.3f, -r * 0.05f, 0.02f), V(r * 0.35f, r * 0.1f, r * 0.22f), g.Hands == HandKind.Leaf ? Leaf : PixelCanvas.Mix(g.Main, Accent, 0.5f),
                    V(0, 0, -25f * s), g.Hands == HandKind.Leaf ? SurfaceMaterial.Leaf : null);
            });
        Tail(Body, V(0, r * 0.45f, -r * 0.85f), 1.1f);
        Face(top, eyePitch: 0.15f);
        Ears(top, 0.85f);
        Top(top);
    }

    private void Cluster()
    {
        int n = Math.Clamp(g.Heads, 2, 3);
        float r = 0.18f * g.Girth;
        Start(V(0, r, 0));
        bool mound = g.Special == Special.Mound;
        if (mound)
        {
            var earth = Rgb(156, 112, 76);
            b.Ell(Body, V(0, r * 0.3f, 0), V(r * 2.3f, r * 0.55f, r * 1.7f), earth, mat: SurfaceMaterial.Scales);
        }
        // Close enough to merge where they meet
        var spots = n == 2
            ? new[] { V(-r * 0.84f, r * 1.0f, 0), V(r * 0.86f, r * 1.15f, 0.03f) }
            : new[] { V(0, r * 2.05f, -0.03f), V(-r * 1.08f, r * 1.0f, 0.03f), V(r * 1.08f, r * 1.05f, 0.02f) };
        if (mound) spots = n == 2 ? new[] { V(-r * 0.8f, r * 1.3f, 0), V(r * 0.85f, r * 1.4f, 0.02f) } : new[] { V(0, r * 1.7f, -0.05f), V(-r * 1.15f, r * 1.25f, 0.05f), V(r * 1.15f, r * 1.3f, 0.04f) };
        Shape first = default;
        for (int i = 0; i < n; i++)
        {
            int bone = i == 0 && !mound ? Body : b.Part(i == 0 ? "head" : "head" + i, Body, spots[i] - V(0, r * 0.5f, 0), PokeRole.Head, i * 1.7f);
            var radii = mound ? V(r * 0.62f, r * 0.95f, r * 0.6f) : V(r, r * 0.96f, r * 0.94f) * (1f - 0.06f * i);
            var shape = Head(bone, spots[i], radii);
            if (i == 0) first = shape;
            Face(shape, eyePitch: mound ? 0.35f : 0.12f, mouth: false);
            if (mound)
            {
                var (np, nn) = shape.At(0f, 0.05f);
                b.Ell(bone, np + nn * r * 0.03f, V(r * 0.22f, r * 0.15f, r * 0.12f), Rgb(232, 120, 140), blend: 0.01f);
            }
        }
        if (g.Special == Special.Gear || g.Metallic)
            for (int i = 1; i < n; i++)
                b.Limb(Body, spots[0], spots[i], r * 0.12f, r * 0.12f, Rgb(170, 176, 190), SurfaceMaterial.Metal, 0.01f);
        if (g.Wings != WingKind.None || g.Hovers && g.Primary == PokemonType.Bug)
            BackWings(V(r * 0.6f, spots[0].Y + r * 0.5f, -r * 0.5f), 0.8f);
        Pattern(first, upright: true);
        Ears(first, 0.7f);
        Top(first);
    }

    private void Cocoon()
    {
        float r = 0.17f * g.Girth;
        Start(V(0, r * 1.9f, 0));
        var body = new Shape(Body, V(0, r * 1.9f, -0.01f), V(r, r * 1.9f, r * 0.95f));
        b.Ell(Body, body.C, body.R, g.Main, V(-6f, 0, 0));
        if (g.Primary == PokemonType.Poison || g.Secondary == PokemonType.Poison || g.Rocky)
            PokeBuilder.Both(s =>
            {
                var (p, n) = body.At(s * 1.45f, 0.1f);
                b.Spike(Body, p - n * 0.01f, p + Vector3.Normalize(n + V(0, -0.3f, 0)) * r * 0.55f, r * 0.2f, PixelCanvas.Shadow(g.Main, 0.2f), mat: HornMaterial);
            });
        if (g.Pattern == PatternKind.Stripes || g.Category.Contains("ocoon", StringComparison.OrdinalIgnoreCase) && g.Kind == BodyKind.Cocoon && g.Plan == BodyPlan.Floating && rnd.Chance(0.5f))
            for (int i = 0; i < 3; i++)
                b.PaintTorus(Body, body.C + V(0, r * (-0.9f + 0.7f * i), 0), r * 0.98f, 0.022f, Accent, default, 1f, 0.95f);
        int head = b.Head(V(0, r * 2.8f, 0));
        var h = Head(head, V(0, r * 3.05f, r * 0.12f), V(r * 0.9f, r * 0.82f, r * 0.85f));
        Face(h, eyePitch: 0.05f, mouth: false, eyeScale: 0.9f);
        Top(h);
    }

    private void Jelly()
    {
        float r = 0.25f * g.Girth;
        float by = 0.55f;
        Start(V(0, by, 0));
        var bell = new Shape(Body, V(0, by, 0), V(r, r * 0.82f, r));
        b.Ell(Body, bell.C, bell.R, g.Main);
        b.Torus(Body, V(0, by - r * 0.45f, 0), r * 0.82f, r * 0.16f, PixelCanvas.Light1(g.Main, 0.15f), blend: 0.03f);
        Pattern(bell, upright: true);
        int count = Math.Max(4, g.Tentacles);
        for (int i = 0; i < count; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / count;
            float sx = MathF.Sin(a), cz = MathF.Cos(a);
            var joint = V(sx * r * 0.62f, by - r * 0.55f, cz * r * 0.62f);
            int leg = b.Leg(MathF.Sign(sx) == 0 ? 1f : MathF.Sign(sx), joint, cz > 0f);
            var mid = V(sx * r * 0.82f, by - r * 1.35f, cz * r * 0.82f);
            var end = V(sx * r * 0.68f, by - r * 2.1f, cz * r * 0.68f);
            b.Limb(leg, joint, mid, r * 0.13f, r * 0.1f, PixelCanvas.Shadow(g.Main, 0.06f));
            b.Limb(leg, mid, end, r * 0.1f, r * 0.05f, PixelCanvas.Shadow(g.Main, 0.06f));
        }
        if (g.Top == TopKind.None && rnd.Chance(0.6f))
            PokeBuilder.Both(s =>
            {
                var (p, n) = bell.At(s * 0.7f, 0.75f);
                b.Ell(Body, p, V(r * 0.16f, r * 0.12f, r * 0.12f), Rgb(222, 64, 76), mat: SurfaceMaterial.Shell, blend: 0.006f);
            });
        Face(bell, eyePitch: 0.05f);
        Top(bell);
    }

    private void Tentacled()
    {
        float r = 0.25f * g.Girth;
        float by = r + 0.08f;
        Start(V(0, by, 0));
        var body = new Shape(Body, V(0, by, 0), V(r, r * 0.95f, r * 0.95f));
        if (g.Special == Special.Tree)
        {
            var bark = Rgb(120, 88, 60);
            b.Limb(Body, V(0, 0.08f, 0), V(0, by + r * 1.2f, 0), r * 0.62f, r * 0.5f, bark);
            body = new Shape(Body, V(0, by + r * 0.3f, 0), V(r * 0.6f, r * 1.0f, r * 0.6f));
            int crown = b.Part("crown", Body, V(0, by + r * 1.3f, 0), PokeRole.Leaf);
            b.Ell(crown, V(0, by + r * 1.65f, -r * 0.1f), V(r * 1.2f, r * 0.75f, r * 1.1f), Leaf, mat: SurfaceMaterial.Leaf, blend: 0.04f);
        }
        else if (g.Special == Special.Spiral)
        {
            b.Ell(Body, body.C, body.R * 0.85f, g.Main);
            var shell = Rgb(232, 214, 168);
            b.Torus(Body, body.C + V(0, r * 0.35f, -r * 0.35f), r * 0.62f, r * 0.42f, shell, V(0, 90f, 90f), mat: SurfaceMaterial.Shell, blend: 0.02f);
            b.Torus(Body, body.C + V(0, r * 0.45f, -r * 0.35f), r * 0.3f, r * 0.24f, PixelCanvas.Shadow(shell, 0.1f), V(0, 90f, 90f), mat: SurfaceMaterial.Shell, blend: 0.02f);
        }
        else b.Ell(Body, body.C, body.R, g.Main);
        Pattern(body, upright: true);

        int count = Math.Max(4, g.Tentacles);
        for (int i = 0; i < count; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / count;
            float sx = MathF.Sin(a), cz = MathF.Cos(a);
            var joint = V(sx * r * 0.6f, 0.13f, cz * r * 0.6f);
            int leg = b.Leg(MathF.Sign(sx) == 0 ? 1f : MathF.Sign(sx), joint, cz > 0f);
            var mid = V(sx * r * 1.2f, 0.05f, cz * r * 1.2f);
            var end = V(sx * r * 1.55f, 0.08f, cz * r * 1.55f);
            var color = g.Special == Special.Tree ? Rgb(120, 88, 60) : PixelCanvas.Shadow(g.Main, 0.06f);
            b.Limb(leg, joint, mid, r * 0.2f, r * 0.14f, color);
            b.Limb(leg, mid, end, r * 0.14f, r * 0.06f, color);
        }
        if (g.Hands is HandKind.Fist or HandKind.Pincer)
            PokeBuilder.Both(s =>
            {
                var shoulder = body.C + V(s * body.R.X * 0.85f, 0, 0.03f);
                int arm = b.Arm(s, shoulder);
                var hand = shoulder + V(s * r * 0.5f, -r * 0.1f, r * 0.3f);
                b.Limb(arm, shoulder, hand, r * 0.16f, r * 0.14f, g.Main);
                Hand(arm, hand, Vector3.Normalize(hand - shoulder), r * 0.18f, s);
            });
        Face(body, eyePitch: 0.2f);
        Top(body);
    }
}
