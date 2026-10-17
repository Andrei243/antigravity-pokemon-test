using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// How a ball opens (plan 25 · Y4; style guide, "Poké Ball (G8)"): the flash, ring and sparks every ball has, in the
/// colours of its kind, and matter of its kind's own. A pure function of the time since the lid opened and a seed:
/// GPU-free, so tests hold each kind to the style guide.
/// </summary>
internal readonly record struct BallRelease(BallKind Kind, Color Flash, Color Light)
{
    /// <summary>Seconds the release lasts from the moment the lid opens.</summary>
    public const float Length = 0.6f;

    /// <summary>The most quads one release draws.</summary>
    public const int Cap = 40;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    public static BallRelease Of(BallLook look) => look.Kind switch
    {
        BallKind.Great => new(look.Kind, Rgb(225, 240, 255), Rgb(120, 170, 255)),
        BallKind.Ultra => new(look.Kind, Rgb(255, 246, 200), Rgb(250, 206, 50)),
        BallKind.Master => new(look.Kind, Rgb(240, 225, 255), Rgb(190, 120, 240)),
        BallKind.Premier => new(look.Kind, Rgb(255, 255, 255), Rgb(255, 150, 150)),
        BallKind.Safari => new(look.Kind, Rgb(235, 255, 225), Rgb(150, 210, 100)),
        BallKind.Net => new(look.Kind, Rgb(220, 255, 255), Rgb(80, 200, 220)),
        BallKind.Dive => new(look.Kind, Rgb(225, 245, 255), Rgb(110, 170, 240)),
        BallKind.Nest => new(look.Kind, Rgb(245, 255, 200), Rgb(150, 220, 80)),
        BallKind.Repeat => new(look.Kind, Rgb(255, 235, 200), Rgb(255, 150, 70)),
        BallKind.Timer => new(look.Kind, Rgb(255, 255, 255), Rgb(255, 120, 110)),
        BallKind.Luxury => new(look.Kind, Rgb(255, 236, 150), Rgb(255, 208, 80)),
        BallKind.Dusk => new(look.Kind, Rgb(150, 110, 200), Rgb(255, 160, 70)),
        BallKind.Heal => new(look.Kind, Rgb(255, 225, 240), Rgb(255, 170, 210)),
        BallKind.Quick => new(look.Kind, Rgb(255, 250, 190), Rgb(110, 170, 255)),
        BallKind.Cherish => new(look.Kind, Rgb(255, 220, 220), Rgb(255, 200, 90)),
        _ => new(BallKind.Poke, Rgb(255, 255, 255), Rgb(255, 246, 200))
    };

    /// <summary>
    /// The release <paramref name="t"/> seconds after the lid opened, at <paramref name="at"/>, for a Pokémon
    /// <paramref name="h"/> tall.
    /// </summary>
    public void Emit(FxList fx, Vector3 at, float h, float t, int seed)
    {
        if (t < 0f || t > Length) return;
        // What every ball has: a glow, a ring and a burst of sparks (the Poké Ball's, unchanged)
        fx.Sprite(FxShape.Glow, at, h * (0.5f + 0.9f * Fx.EaseOut(t / 0.25f)), Fx.Fade(Flash, (1f - t / Length) * 0.9f));
        Fx.Shockwave(fx, t, at, Vector3.Zero, 0f, 0.4f, h * 1.1f, Light);
        Fx.Burst(fx, seed * 977 + 13, t, at, h, 0f, 0.55f, 14, FxShape.Spark, Flash, Light, 0.1f, 0.9f);

        int s = seed * 331 + 5;
        float fade = 1f - t / Length;
        switch (Kind)
        {
            case BallKind.Great:
                Fx.Burst(fx, s, t, at, h, 0f, 0.5f, 10, FxShape.Spark, Rgb(90, 150, 255), Rgb(200, 225, 255), 0.09f, 1.5f, streak: 0.9f, salt: 1);
                break;
            case BallKind.Ultra:
                Fx.Burst(fx, s, t, at, h, 0f, 0.55f, 7, FxShape.Star, Rgb(250, 206, 50), Rgb(255, 240, 150), 0.12f, 1.0f, spin: 3f, salt: 1);
                Fx.Burst(fx, s, t, at, h, 0f, 0.55f, 4, FxShape.Smoke, new Color(50, 50, 60, 150), new Color(70, 70, 84, 130), 0.22f, 0.5f, -0.1f, FxBlend.Alpha, salt: 2);
                break;
            case BallKind.Master:
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0.05f, 0.5f, h * 1.0f, Rgb(190, 110, 250), FxShape.Swirl);
                Fx.Burst(fx, s, t, at, h, 0f, 0.55f, 6, FxShape.Star, Rgb(236, 110, 180), Rgb(255, 200, 230), 0.12f, 1.1f, spin: 2f, salt: 1);
                break;
            case BallKind.Premier:
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0.12f, 0.45f, h * 1.4f, Rgb(230, 60, 66));
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 5, FxShape.Feather, Rgb(250, 250, 255), Rgb(235, 235, 245), 0.16f, 0.6f, 0.4f, FxBlend.Alpha, spin: 2f, salt: 1);
                break;
            case BallKind.Safari:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 8, FxShape.Leaf, Rgb(110, 170, 70), Rgb(170, 210, 100), 0.13f, 0.8f, 0.5f, FxBlend.Alpha, spin: 3f, salt: 1);
                break;
            case BallKind.Net:
                // A lattice of crosses spreading out: two rings of them
                for (int ring = 0; ring < 2; ring++)
                {
                    int count = ring == 0 ? 8 : 4;
                    float r = h * (ring == 0 ? 0.9f : 0.5f) * Fx.EaseOut(t / 0.5f);
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * MathF.Tau / count + ring * 0.4f;
                        fx.Sprite(FxShape.Cross, at + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.8f, 0f), h * 0.14f,
                            Fx.Fade(ring == 0 ? Rgb(60, 180, 200) : Rgb(200, 245, 250), fade), FxBlend.Add, a);
                    }
                }
                break;
            case BallKind.Dive:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 9, FxShape.Bubble, Rgb(170, 215, 255), Rgb(220, 240, 255), 0.12f, 0.5f, -0.9f, FxBlend.Alpha, salt: 1);
                break;
            case BallKind.Nest:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 7, FxShape.Leaf, Rgb(150, 220, 80), Rgb(244, 220, 120), 0.13f, 0.7f, 0.35f, FxBlend.Alpha, spin: 2f, salt: 1);
                break;
            case BallKind.Repeat:
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0.10f, 0.4f, h * 1.0f, Rgb(255, 150, 70));
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0.20f, 0.4f, h * 0.9f, Rgb(255, 220, 90));
                break;
            case BallKind.Timer:
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0f, 0.55f, h * 1.2f, Rgb(255, 255, 255));
                Fx.Shockwave(fx, t, at, Vector3.Zero, 0.25f, 0.35f, h * 0.8f, Rgb(230, 60, 50));
                for (int i = 0; i < 4; i++)
                {
                    if (t < i * 0.12f) continue;
                    float a = i * MathF.PI / 2f;
                    fx.Sprite(FxShape.Dot, at + new Vector3(MathF.Cos(a), MathF.Sin(a) * 0.8f, 0f) * h * 0.45f, h * 0.09f, Fx.Fade(Rgb(230, 60, 50), fade));
                }
                break;
            case BallKind.Luxury:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 7, FxShape.Star, Rgb(240, 200, 80), Rgb(255, 236, 160), 0.1f, 0.5f, -0.5f, spin: 2f, salt: 1);
                Fx.Burst(fx, s, t, at, h, 0f, 0.5f, 6, FxShape.Spark, Rgb(255, 230, 140), Rgb(255, 250, 220), 0.08f, 0.8f, -0.3f, salt: 2);
                break;
            case BallKind.Dusk:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 9, FxShape.Dot, new Color(50, 30, 80, 220), new Color(90, 60, 130, 220), 0.14f, 0.5f, -0.4f, FxBlend.Alpha, salt: 1);
                fx.Sprite(FxShape.Star, at + Vector3.UnitY * h * 0.3f, h * 0.2f, Fx.Fade(Rgb(255, 160, 70), Fx.Envelope(t, 0f, 0.1f, 0.25f, 0.5f)));
                break;
            case BallKind.Heal:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 6, FxShape.Heart, Rgb(255, 160, 200), Rgb(255, 230, 240), 0.15f, 0.45f, -0.7f, FxBlend.Alpha, salt: 1);
                break;
            case BallKind.Quick:
                Fx.Burst(fx, s, t, at, h, 0f, 0.45f, 8, FxShape.Streak, Rgb(250, 220, 80), Rgb(120, 170, 255), 0.08f, 2.0f, streak: 1.2f, salt: 1);
                break;
            case BallKind.Cherish:
                Fx.Burst(fx, s, t, at, h, 0f, 0.6f, 8, FxShape.Petal, Rgb(230, 60, 70), Rgb(255, 120, 130), 0.14f, 0.8f, 0.3f, FxBlend.Alpha, spin: 3f, salt: 1);
                Fx.Burst(fx, s, t, at, h, 0f, 0.5f, 4, FxShape.Star, Rgb(255, 255, 255), Rgb(255, 230, 160), 0.1f, 1.0f, spin: 2f, salt: 2);
                break;
        }
    }
}

/// <summary>Where a recalled Pokémon's ball is, and how wide its lid and its beam are (plan 25 · Y4).</summary>
internal readonly record struct RecallStep(bool Visible, Vector3 At, float Size, float Open, float Beam);

/// <summary>
/// A Pokémon called back (plan 25 · Y4; style guide, "Poké Ball (G8)"): its own ball flies in from where the next
/// send-out would come from, opens, a red beam draws the Pokémon in, it shuts and flies back out, and is off the
/// field by the end. Pure functions of the time since the call.
/// </summary>
internal static class BallRecall
{
    /// <summary>The fractions of the recall: in, lid open, beam, lid shut, out.</summary>
    public const float InEnd = 0.28f, OpenEnd = 0.40f, BeamStart = 0.32f, BeamFull = 0.40f, BeamHold = 0.66f, BeamEnd = 0.72f, ShutStart = 0.72f, ShutEnd = 0.82f;

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>How far the Pokémon has been drawn in by <paramref name="age"/>: 0 before the beam, 1 after it.</summary>
    public static float Shrink(float age, float length) => age < 0f ? -1f : Smooth(BeamStart, BeamEnd - 0.02f, age / length);

    public static RecallStep At(float age, float length, Vector3 from, Vector3 open, float h, float ballSize, float handSize)
    {
        if (age < 0f || age >= length) return new(false, open, 0f, 0f, 0f);
        float p = age / length;
        float lid = Smooth(InEnd, OpenEnd, p) * (1f - Smooth(ShutStart, ShutEnd, p));
        float beam = Fx.Envelope(p, BeamStart, BeamFull, BeamHold, BeamEnd);
        if (p < InEnd)
        {
            float f = p / InEnd;
            return new(true, BattleBall.Arc(from, open, h * 0.3f + 0.25f, f), handSize + (ballSize - handSize) * f, 0f, 0f);
        }
        if (p < ShutEnd) return new(true, open + Vector3.UnitY * 0.06f * MathF.Sin(p * MathF.PI * 6f), ballSize, lid, beam);
        float g = (p - ShutEnd) / (1f - ShutEnd);
        return new(true, BattleBall.Arc(open, from, h * 0.3f + 0.25f, g), ballSize + (handSize - ballSize) * g, 0f, 0f);
    }

    /// <summary>The red beam of three ribbons and a glow from the open ball to the Pokémon's body.</summary>
    public static void Beam(FxList fx, float pull, float time, Vector3 ball, Vector3 body, float h)
    {
        if (pull <= 0f) return;
        for (int k = 0; k < 3; k++)
        {
            var (across, up) = Fx.Frame(body - ball);
            var pts = new Vector3[6];
            for (int i = 0; i < pts.Length; i++)
            {
                float u = i / (float)(pts.Length - 1);
                float wave = MathF.Sin(u * MathF.PI) * h * 0.25f * MathF.Sin(time * 9f + k * 2.1f + u * 4f);
                pts[i] = Vector3.Lerp(ball, body, u) + (k == 1 ? up : across) * wave * (k == 2 ? -1f : 1f);
            }
            fx.Ribbon(pts, h * 0.07f * pull, new Color(255, 80, 80, (int)(220 * pull)));
        }
        fx.Sprite(FxShape.Glow, ball, h * 0.6f * pull, new Color(255, 90, 90, (int)(200 * pull)));
    }
}
