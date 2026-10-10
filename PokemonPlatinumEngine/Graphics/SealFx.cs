using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The fourteen kinds of Seal, by what bursts out of them.</summary>
internal enum SealFamily { Heart, Star, Line, Smoke, Ele, Foamy, Fire, Party, Flora, Song, Letter, Liquid, Burst, Twinkle }

/// <summary>
/// One piece of a Seal's burst at one moment: a shape of the effects' atlas, where it is in the dots of the
/// capsule's own picture (right and up from the Pokémon's place), how big, its colour and how it is turned.
/// </summary>
internal readonly record struct SealMote(FxShape Shape, Vector2 At, float Size, Color Color, float Rotation, FxBlend Blend);

/// <summary>
/// What the Seals look like (plan 06 · R17b; style guide, "Ball Capsules and Seals"): our own sign and colour for each
/// of the eighty, and what bursts out of a Ball Capsule as its Pokémon is sent out. The burst is a pure function of the
/// capsule, its age and a seed, in the dots of the capsule's own picture, so the capsule's screen shows in two
/// dimensions exactly what a battle shows in three. When each Seal goes off and where is the rules'
/// (<see cref="CapsuleRules.BurstDelay"/>, <see cref="CapsuleRules.BurstOffset"/>). GPU-free.
/// </summary>
internal static class SealFx
{
    /// <summary>How long one Seal's burst lasts, and a whole capsule's from the ball's opening (the last Seal goes off twenty frames in).</summary>
    public const float SealTime = 1.4f, Length = 20f / CapsuleRules.FramesPerSecond + SealTime;

    /// <summary>How long a capsule tried out on its screen rests before it bursts again.</summary>
    public const float Pause = 1.3f;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color[] HeartColors = { Rgb(255, 128, 176), Rgb(240, 84, 130), Rgb(232, 70, 82), Rgb(214, 96, 214), Rgb(255, 150, 132), Rgb(170, 120, 240) };
    private static readonly Color[] StarColors = { Rgb(255, 224, 80), Rgb(250, 186, 56), Rgb(150, 210, 255), Rgb(246, 246, 255), Rgb(255, 156, 72), Rgb(120, 232, 224) };
    private static readonly Color[] LineColors = { Rgb(96, 164, 255), Rgb(110, 214, 130), Rgb(244, 100, 96), Rgb(250, 214, 84) };
    private static readonly Color[] SmokeColors = { Rgb(236, 238, 244), Rgb(170, 174, 190), Rgb(176, 140, 214), Rgb(96, 98, 118) };
    private static readonly Color[] EleColors = { Rgb(255, 228, 70), Rgb(255, 250, 190), Rgb(150, 220, 255), Rgb(255, 170, 70) };
    private static readonly Color[] FoamyColors = { Rgb(150, 214, 255), Rgb(255, 176, 214), Rgb(160, 236, 190), Rgb(240, 246, 255) };
    private static readonly Color[] FireColors = { Rgb(255, 140, 48), Rgb(244, 80, 60), Rgb(110, 170, 255), Rgb(190, 120, 250) };
    private static readonly Color[] PartyColors = { Rgb(255, 120, 130), Rgb(110, 180, 255), Rgb(250, 206, 80), Rgb(130, 220, 140) };
    private static readonly Color[] FloraColors = { Rgb(255, 158, 196), Rgb(250, 246, 250), Rgb(252, 220, 96), Rgb(240, 96, 104), Rgb(186, 136, 240), Rgb(255, 170, 96) };
    private static readonly Color[] SongColors = { Rgb(110, 214, 140), Rgb(110, 176, 255), Rgb(250, 214, 84), Rgb(255, 140, 190), Rgb(255, 166, 86), Rgb(186, 140, 250), Rgb(240, 244, 255) };

    /// <summary>A Seal's kind, from its constant (<c>HEART_SEAL_B</c>, <c>ELE_SEAL_A</c>, <c>Q_SEAL</c>).</summary>
    public static SealFamily FamilyOf(int seal)
    {
        string id = SealDatabase.Id(seal);
        if (SealDatabase.IsLetter(seal)) return SealFamily.Letter;
        foreach (var family in Enum.GetValues<SealFamily>())
            if (id.StartsWith(family.ToString().ToUpperInvariant() + "_", StringComparison.Ordinal)) return family;
        return SealFamily.Twinkle;
    }

    /// <summary>Which of its kind it is, from 0: the A to G its constant ends in.</summary>
    public static int Variant(int seal)
    {
        string id = SealDatabase.Id(seal);
        return id.Length > 2 && id[^2] == '_' && id[^1] is >= 'A' and <= 'G' && !SealDatabase.IsLetter(seal) ? id[^1] - 'A' : 0;
    }

    /// <summary>The letter a letter Seal shows: A to Z, "!" for the Shock Seal and "?" for the Mystery Seal.</summary>
    public static char LetterOf(int seal)
    {
        string id = SealDatabase.Id(seal);
        return id switch
        {
            "SHOCK_SEAL" => '!',
            "MYSTERY_SEAL" => '?',
            _ => id.Length > 0 ? id[0] : '?'
        };
    }

    /// <summary>A Seal's own colour.</summary>
    public static Color ColorOf(int seal)
    {
        int v = Variant(seal);
        return FamilyOf(seal) switch
        {
            SealFamily.Heart => HeartColors[v % HeartColors.Length],
            SealFamily.Star => StarColors[v % StarColors.Length],
            SealFamily.Line => LineColors[v % LineColors.Length],
            SealFamily.Smoke => SmokeColors[v % SmokeColors.Length],
            SealFamily.Ele => EleColors[v % EleColors.Length],
            SealFamily.Foamy => FoamyColors[v % FoamyColors.Length],
            SealFamily.Fire => FireColors[v % FireColors.Length],
            SealFamily.Party => PartyColors[v % PartyColors.Length],
            SealFamily.Flora => FloraColors[v % FloraColors.Length],
            SealFamily.Song => SongColors[v % SongColors.Length],
            SealFamily.Letter => LetterColor(LetterOf(seal)),
            SealFamily.Liquid => Rgb(96, 200, 236),
            SealFamily.Burst => Rgb(255, 170, 80),
            _ => Rgb(226, 234, 255)
        };
    }

    // The letters go round the colours, so a word spelt out on a ball is a row of different ones
    private static Color LetterColor(char letter)
    {
        int i = letter == '!' ? 26 : letter == '?' ? 27 : Math.Clamp(letter - 'A', 0, 25);
        float hue = i * 360f / 28f * 5f % 360f;
        return Raylib.ColorFromHSV(hue, 0.62f, 0.98f);
    }

    /// <summary>The shape a Seal's own sign is drawn with, and its burst's pieces are.</summary>
    public static FxShape ShapeOf(int seal) => FamilyOf(seal) switch
    {
        SealFamily.Heart => FxShape.Heart,
        SealFamily.Star => FxShape.Star,
        SealFamily.Line => FxShape.Streak,
        SealFamily.Smoke => FxShape.Smoke,
        SealFamily.Ele => FxShape.Spark,
        SealFamily.Foamy => FxShape.Bubble,
        SealFamily.Fire => FxShape.Flame,
        SealFamily.Party => FxShape.Shard,
        SealFamily.Flora => FxShape.Petal,
        SealFamily.Song => FxShape.Note,
        SealFamily.Liquid => FxShape.Drop,
        SealFamily.Burst => FxShape.Impact,
        SealFamily.Twinkle => FxShape.Cross,
        _ => FxShape.Dot
    };

    private static readonly Dictionary<char, string> Marks = new()
    {
        ['!'] = "..X....X....X....X....X.........X..",
        ['?'] = ".XXX.X...X....X...X...X.........X.."
    };

    /// <summary>A letter of the signs' alphabet as seven rows of five (<see cref="Pix"/>'s, with the two marks the Seals add).</summary>
    public static string GlyphOf(char letter) => Marks.TryGetValue(letter, out var mark) ? mark : Pix.Glyph(letter) ?? Marks['?'];

    private static float Rand(int seed, int i, int salt) => SoftCanvas.Rand(seed, i, salt);
    private static float Signed(int seed, int i, int salt) => Rand(seed, i, salt) * 2f - 1f;
    private static Color Fade(Color c, float alpha) => new(c.R, c.G, c.B, (byte)Math.Clamp((int)(255 * Math.Clamp(alpha, 0f, 1f)), 0, 255));
    private static Color Light(Color c, float t) => PixelCanvas.Mix(c, Color.White, t);
    private static float EaseOut(float t) => 1f - (1f - Math.Clamp(t, 0f, 1f)) * (1f - Math.Clamp(t, 0f, 1f));

    /// <summary>
    /// What a capsule shows <paramref name="age"/> seconds after its ball opened: every Seal's pieces, each from the
    /// moment its Seal goes off, the Seals at the back first.
    /// </summary>
    public static void Motes(BallCapsule capsule, float age, int seed, List<SealMote> into)
    {
        for (int i = capsule.Seals.Count - 1; i >= 0; i--)
        {
            var seal = capsule.Seals[i];
            float t = age - CapsuleRules.BurstDelay(seal) / CapsuleRules.FramesPerSecond;
            if (t < 0f || t > SealTime) continue;
            var (right, up) = CapsuleRules.BurstOffset(seal);
            One(seal.Seal, new Vector2(right, up), t, seed * 31 + i * 977 + seal.Seal, into);
        }
    }

    /// <summary>One Seal's pieces <paramref name="t"/> seconds after it went off, round <paramref name="at"/>.</summary>
    public static void One(int seal, Vector2 at, float t, int seed, List<SealMote> into)
    {
        if (t < 0f || t > SealTime) return;
        float k = t / SealTime;
        var color = ColorOf(seal);
        int v = Variant(seal);
        // Every Seal goes off with a small flash of its own colour
        if (t < 0.22f) into.Add(new SealMote(FxShape.Glow, at, 26f * (0.4f + t / 0.22f), Fade(Light(color, 0.5f), 0.8f * (1f - t / 0.22f)), 0f, FxBlend.Add));

        switch (FamilyOf(seal))
        {
            case SealFamily.Heart:
            {
                // Hearts float up, swaying: a few large ones or many small
                int count = v % 2 == 0 ? 3 : 6;
                float size = v % 2 == 0 ? 15f : 10f;
                for (int i = 0; i < count; i++)
                {
                    float own = Math.Clamp((t - i * 0.07f) / (SealTime * 0.8f), 0f, 1f);
                    if (own <= 0f || own >= 1f) continue;
                    float sway = MathF.Sin(own * 7f + i * 2.1f) * 5f;
                    var p = at + new Vector2(Signed(seed, i, 1) * 16f + sway, own * (34f + 14f * Rand(seed, i, 2)));
                    into.Add(new SealMote(FxShape.Heart, p, size * (0.5f + 0.7f * EaseOut(own * 3f)), Fade(color, 1f - own * own), Signed(seed, i, 3) * 0.3f, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Star:
            {
                // Stars are thrown out all round and spin as they slow
                int count = 5 + v % 3;
                for (int i = 0; i < count; i++)
                {
                    float a = (i + Rand(seed, i, 1) * 0.6f) * MathF.Tau / count;
                    float reach = (22f + 16f * Rand(seed, i, 2)) * EaseOut(k * 1.4f);
                    var p = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * reach - new Vector2(0, 10f * k * k);
                    into.Add(new SealMote(FxShape.Star, p, 11f * (1f - 0.4f * k), Fade(Light(color, 0.2f * Rand(seed, i, 3)), 1f - k * k), t * 5f * (i % 2 == 0 ? 1f : -1f) + a, FxBlend.Add));
                }
                break;
            }
            case SealFamily.Line:
            {
                // Rays shoot straight out and are gone
                int count = 8;
                float q = Math.Clamp(t / 0.6f, 0f, 1f);
                if (q >= 1f) break;
                for (int i = 0; i < count; i++)
                {
                    float a = i * MathF.Tau / count + v * 0.39f;
                    var p = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (10f + 30f * EaseOut(q));
                    into.Add(new SealMote(FxShape.Streak, p, 16f * (1f - 0.5f * q), Fade(Light(color, 0.3f), 1f - q), a, FxBlend.Add));
                }
                break;
            }
            case SealFamily.Smoke:
            {
                // Puffs swell and drift up
                for (int i = 0; i < 5; i++)
                {
                    var p = at + new Vector2(Signed(seed, i, 1) * 14f * (0.4f + k), Signed(seed, i, 2) * 8f + 26f * k * (0.6f + 0.4f * Rand(seed, i, 3)));
                    into.Add(new SealMote(FxShape.Smoke, p, (14f + 6f * Rand(seed, i, 4)) * (0.5f + 0.9f * EaseOut(k)), Fade(color, 0.85f * (1f - k)), Rand(seed, i, 5) * MathF.Tau + t, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Ele:
            {
                // Sparks crackle on the spot, jumping from frame to frame
                int beat = (int)(t * 18f);
                float q = Math.Clamp(t / 0.9f, 0f, 1f);
                if (q >= 1f) break;
                for (int i = 0; i < 6; i++)
                {
                    var p = at + new Vector2(Signed(seed + beat, i, 1), Signed(seed + beat, i, 2)) * (10f + 18f * q);
                    into.Add(new SealMote(FxShape.Spark, p, 12f * (0.6f + 0.6f * Rand(seed + beat, i, 3)), Fade(Light(color, 0.35f), 1f - q * q), Rand(seed + beat, i, 4) * MathF.Tau, FxBlend.Add));
                }
                break;
            }
            case SealFamily.Foamy:
            {
                // Bubbles wobble their way up and pop
                for (int i = 0; i < 7; i++)
                {
                    float own = Math.Clamp((t - i * 0.05f) / (SealTime * (0.6f + 0.35f * Rand(seed, i, 1))), 0f, 1f);
                    if (own <= 0f || own >= 1f) continue;
                    var p = at + new Vector2(Signed(seed, i, 2) * 18f + MathF.Sin(own * 9f + i) * 3f, own * (30f + 16f * Rand(seed, i, 3)));
                    into.Add(new SealMote(FxShape.Bubble, p, 7f + 7f * Rand(seed, i, 4), Fade(Light(color, 0.25f), own > 0.85f ? (1f - own) / 0.15f : 1f), 0f, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Fire:
            {
                // Flames lick upward and shrink away, with a few embers
                for (int i = 0; i < 6; i++)
                {
                    float own = Math.Clamp((t - i * 0.04f) / (SealTime * 0.7f), 0f, 1f);
                    if (own <= 0f || own >= 1f) continue;
                    var p = at + new Vector2(Signed(seed, i, 1) * 12f * (1f - own * 0.5f), own * (26f + 18f * Rand(seed, i, 2)));
                    into.Add(new SealMote(i % 3 == 2 ? FxShape.Ember : FxShape.Flame, p, (i % 3 == 2 ? 6f : 16f) * (1f - 0.6f * own), Fade(Light(color, 0.3f * (1f - own)), 1f - own * own), Signed(seed, i, 3) * 0.25f, FxBlend.Add));
                }
                break;
            }
            case SealFamily.Party:
            {
                // Confetti is thrown up and flutters down, every piece its own colour
                for (int i = 0; i < 12; i++)
                {
                    float vx = Signed(seed, i, 1) * 36f, vy = 44f + 30f * Rand(seed, i, 2);
                    var p = at + new Vector2(vx * t, vy * t - 60f * t * t);
                    var piece = PartyColors[(i + v) % PartyColors.Length];
                    into.Add(new SealMote(i % 2 == 0 ? FxShape.Shard : FxShape.Dot, p, i % 2 == 0 ? 8f : 5f, Fade(piece, 1f - k * k * k), t * (6f + 6f * Rand(seed, i, 3)) + i, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Flora:
            {
                // Petals open out and drift down, turning
                for (int i = 0; i < 7; i++)
                {
                    float a = i * MathF.Tau / 7f + Rand(seed, i, 1);
                    var p = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 20f * EaseOut(k * 1.6f) + new Vector2(MathF.Sin(t * 3f + i) * 4f, -18f * k * k);
                    into.Add(new SealMote(FxShape.Petal, p, 11f, Fade(color, 1f - k * k), a + t * 2.2f, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Song:
            {
                // Notes hop out one after another, rocking
                for (int i = 0; i < 4; i++)
                {
                    float own = Math.Clamp((t - i * 0.14f) / (SealTime * 0.62f), 0f, 1f);
                    if (own <= 0f || own >= 1f) continue;
                    float side = i % 2 == 0 ? 1f : -1f;
                    var p = at + new Vector2(side * (8f + 20f * own), 34f * MathF.Sin(own * MathF.PI * 0.75f) + 6f * i);
                    into.Add(new SealMote(FxShape.Note, p, 14f, Fade(color, 1f - own * own), MathF.Sin(own * 10f) * 0.3f, FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Letter:
            {
                // The letter pops out, hangs in the air and floats off: dots of the signs' alphabet
                string glyph = GlyphOf(LetterOf(seal));
                float pop = t < 0.18f ? 1.25f * EaseOut(t / 0.18f) : 1f + 0.25f * MathF.Max(0f, 1f - (t - 0.18f) / 0.12f);
                float fade = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                const float pitch = 3.6f;
                var middle = at + new Vector2(0, 12f * k);
                for (int row = 0; row < 7; row++)
                    for (int col = 0; col < 5; col++)
                    {
                        if (glyph[row * 5 + col] != 'X') continue;
                        var p = middle + new Vector2((col - 2) * pitch, (3 - row) * pitch) * pop;
                        into.Add(new SealMote(FxShape.Dot, p, 4f * pop, Fade(color, fade), 0f, FxBlend.Alpha));
                    }
                break;
            }
            case SealFamily.Liquid:
            {
                // Drops fly out in arcs and fall
                for (int i = 0; i < 8; i++)
                {
                    float vx = Signed(seed, i, 1) * 44f, vy = 30f + 30f * Rand(seed, i, 2);
                    var p = at + new Vector2(vx * t, vy * t - 70f * t * t);
                    into.Add(new SealMote(FxShape.Drop, p, 8f, Fade(Light(color, 0.2f), 1f - k * k), MathF.Atan2(vx, -(vy - 140f * t)), FxBlend.Alpha));
                }
                break;
            }
            case SealFamily.Burst:
            {
                // A ring bangs outward with a burst of sparks
                float q = Math.Clamp(t / 0.7f, 0f, 1f);
                if (q < 1f) into.Add(new SealMote(FxShape.Ring, at, 60f * EaseOut(q), Fade(Light(color, 0.3f), 1f - q), 0f, FxBlend.Add));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.Tau / 8f + Rand(seed, i, 1);
                    var p = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 36f * EaseOut(k * 1.5f);
                    into.Add(new SealMote(FxShape.Impact, p, 10f * (1f - k), Fade(color, 1f - k), a, FxBlend.Add));
                }
                break;
            }
            default:
            {
                // Twinkles wink in and out here and there
                for (int i = 0; i < 8; i++)
                {
                    float start = i * 0.11f, own = (t - start) / 0.5f;
                    if (own <= 0f || own >= 1f) continue;
                    var p = at + new Vector2(Signed(seed, i, 1) * 28f, Signed(seed, i, 2) * 22f + 8f);
                    into.Add(new SealMote(FxShape.Cross, p, 11f * MathF.Sin(own * MathF.PI), Fade(Light(color, 0.4f), MathF.Sin(own * MathF.PI)), own * 1.5f, FxBlend.Add));
                }
                break;
            }
        }
    }

    /// <summary>
    /// A capsule's burst in a battle (the cue of a Pokémon sent out with one): the pieces stand in the plane across the
    /// field, a dot of the capsule's picture an eightieth of the Pokémon's height (held between a small and a large
    /// Pokémon's, so a word stays a word), from the moment the ball opens.
    /// </summary>
    public static void Play(in FxCtx c, FxList fx, List<SealMote> scratch)
    {
        if (c.Cue.Capsule is not { IsEmpty: false } capsule) return;
        float age = c.Age - BattleAnimator.SendOutBallTime;
        if (age < 0f) return;
        scratch.Clear();
        Motes(capsule, age, c.Seed, scratch);
        float unit = Math.Clamp(c.FromSize, 0.7f, 1.6f) / 80f;
        var toward = c.To - c.From;
        toward.Y = 0f;
        var (across, _) = Fx.Frame(toward);
        // The foe's side faces the camera: across the field runs the other way on screen, and a word must still read
        if (c.Cue.FromSide == BattleSide.Enemy) across = -across;
        var origin = c.FromFeet + Vector3.UnitY * c.FromSize * 0.35f;
        foreach (var mote in scratch)
            fx.Sprite(mote.Shape, origin + across * mote.At.X * unit + Vector3.UnitY * mote.At.Y * unit, mote.Size * unit, mote.Color, mote.Blend, mote.Rotation);
    }
}
