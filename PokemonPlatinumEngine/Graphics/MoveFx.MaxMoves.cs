using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// The Max Moves and the G-Max Moves (plan 25 · Y15; style guide, "Dynamax, Max Moves and Terastallization"): the
// eighteen of the types, one recipe to a type whatever move it was made of, Max Guard, and the thirty-three of the
// Gigantamax forms, each its type's with a mark of its own. Every one is our own, its beats too: a red pulse on the
// user, the crossing sheathed in Dynamax red, a landing at ImpactTime, then the type's set piece round the target to
// 1.4 s under a red glow, all faded by BattleAnimator.MaxMoveTime. Pure in the cue's age and seed, under MaxQuads at
// every age; no flash over 0.35, no shake over 0.5; nothing over 1.3 h above the target's feet.
internal static partial class MoveFx
{
    /// <summary>The eighteen Max Moves of the types (Max Guard is the nineteenth, the status moves').</summary>
    private static readonly Dictionary<string, PokemonType> TypeMaxMoves = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Max Strike"] = PokemonType.Normal, ["Max Knuckle"] = PokemonType.Fighting, ["Max Airstream"] = PokemonType.Flying,
        ["Max Ooze"] = PokemonType.Poison, ["Max Quake"] = PokemonType.Ground, ["Max Rockfall"] = PokemonType.Rock,
        ["Max Flutterby"] = PokemonType.Bug, ["Max Phantasm"] = PokemonType.Ghost, ["Max Steelspike"] = PokemonType.Steel,
        ["Max Flare"] = PokemonType.Fire, ["Max Geyser"] = PokemonType.Water, ["Max Overgrowth"] = PokemonType.Grass,
        ["Max Lightning"] = PokemonType.Electric, ["Max Mindstorm"] = PokemonType.Psychic, ["Max Hailstorm"] = PokemonType.Ice,
        ["Max Wyrmwind"] = PokemonType.Dragon, ["Max Darkness"] = PokemonType.Dark, ["Max Starfall"] = PokemonType.Fairy
    };

    /// <summary>How a G-Max Move's own mark moves round its target.</summary>
    private enum GMaxMotion { Circling, Falling, Rising, Thrown, Lying, Sign }

    /// <summary>A G-Max Move's own mark: its shape, two colours, and how it moves.</summary>
    private readonly record struct GMaxMark(FxShape Shape, Color A, Color B, GMaxMotion Motion, FxBlend Blend = FxBlend.Alpha);

    private static readonly Dictionary<string, GMaxMark> GMaxMarks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["G-Max Befuddle"] = new(FxShape.Spore, Rgb(176, 120, 230), Rgb(255, 214, 90), GMaxMotion.Circling, FxBlend.Add),
        ["G-Max Cannonade"] = new(FxShape.Orb, Rgb(60, 130, 230), Rgb(200, 236, 255), GMaxMotion.Falling),
        ["G-Max Centiferno"] = new(FxShape.Flame, Rgb(255, 120, 30), Rgb(255, 60, 30), GMaxMotion.Circling, FxBlend.Add),
        ["G-Max Chi Strike"] = new(FxShape.Star, Rgb(255, 210, 80), Rgb(255, 250, 220), GMaxMotion.Thrown, FxBlend.Add),
        ["G-Max Cuddle"] = new(FxShape.Heart, Rgb(255, 150, 190), Rgb(240, 214, 170), GMaxMotion.Rising),
        ["G-Max Depletion"] = new(FxShape.Hex, Rgb(90, 130, 190), Rgb(160, 230, 255), GMaxMotion.Lying),
        ["G-Max Drum Solo"] = new(FxShape.Note, Rgb(120, 190, 70), Rgb(150, 100, 60), GMaxMotion.Thrown),
        ["G-Max Finale"] = new(FxShape.Dot, Rgb(255, 244, 220), Rgb(255, 180, 210), GMaxMotion.Rising, FxBlend.Add),
        ["G-Max Fireball"] = new(FxShape.Orb, Rgb(255, 140, 40), Rgb(255, 230, 120), GMaxMotion.Sign, FxBlend.Add),
        ["G-Max Foam Burst"] = new(FxShape.Bubble, Rgb(230, 244, 255), Rgb(120, 180, 240), GMaxMotion.Thrown),
        ["G-Max Gold Rush"] = new(FxShape.Ring, Rgb(255, 210, 70), Rgb(255, 240, 170), GMaxMotion.Falling, FxBlend.Add),
        ["G-Max Gravitas"] = new(FxShape.Ring, Rgb(150, 90, 220), Rgb(90, 120, 230), GMaxMotion.Lying, FxBlend.Add),
        ["G-Max Hydrosnipe"] = new(FxShape.Target, Rgb(70, 160, 255), Rgb(220, 244, 255), GMaxMotion.Sign),
        ["G-Max Malodor"] = new(FxShape.Smoke, Rgb(120, 170, 70), Rgb(140, 90, 150), GMaxMotion.Rising),
        ["G-Max Meltdown"] = new(FxShape.Drop, Rgb(200, 206, 220), Rgb(130, 136, 156), GMaxMotion.Falling),
        ["G-Max One Blow"] = new(FxShape.Fist, Rgb(50, 36, 60), Rgb(220, 60, 50), GMaxMotion.Sign),
        ["G-Max Rapid Flow"] = new(FxShape.Wave, Rgb(70, 150, 240), Rgb(220, 240, 255), GMaxMotion.Circling),
        ["G-Max Replenish"] = new(FxShape.Seed, Rgb(220, 60, 70), Rgb(90, 170, 70), GMaxMotion.Thrown),
        ["G-Max Resonance"] = new(FxShape.Wave, Rgb(150, 240, 190), Rgb(150, 210, 255), GMaxMotion.Rising, FxBlend.Add),
        ["G-Max Sandblast"] = new(FxShape.Smoke, Rgb(220, 190, 120), Rgb(180, 140, 80), GMaxMotion.Circling),
        ["G-Max Smite"] = new(FxShape.Star, Rgb(255, 130, 200), Rgb(190, 120, 255), GMaxMotion.Falling),
        ["G-Max Snooze"] = new(FxShape.Zzz, Rgb(180, 120, 230), Rgb(255, 160, 210), GMaxMotion.Rising),
        ["G-Max Steelsurge"] = new(FxShape.Shard, Rgb(200, 120, 70), Rgb(210, 214, 226), GMaxMotion.Lying),
        ["G-Max Stonesurge"] = new(FxShape.Rock, Rgb(150, 160, 150), Rgb(90, 160, 160), GMaxMotion.Thrown),
        ["G-Max Stun Shock"] = new(FxShape.Spark, Rgb(255, 230, 70), Rgb(190, 110, 255), GMaxMotion.Circling, FxBlend.Add),
        ["G-Max Sweetness"] = new(FxShape.Petal, Rgb(230, 70, 70), Rgb(255, 170, 190), GMaxMotion.Thrown),
        ["G-Max Tartness"] = new(FxShape.Leaf, Rgb(220, 60, 60), Rgb(120, 200, 70), GMaxMotion.Circling),
        ["G-Max Terror"] = new(FxShape.Eye, Rgb(150, 80, 220), Rgb(40, 20, 60), GMaxMotion.Sign),
        ["G-Max Vine Lash"] = new(FxShape.Leaf, Rgb(70, 160, 60), Rgb(40, 100, 40), GMaxMotion.Lying),
        ["G-Max Volcalith"] = new(FxShape.Ember, Rgb(255, 120, 40), Rgb(70, 60, 60), GMaxMotion.Rising, FxBlend.Add),
        ["G-Max Volt Crash"] = new(FxShape.Cross, Rgb(255, 236, 90), Rgb(255, 255, 220), GMaxMotion.Falling, FxBlend.Add),
        ["G-Max Wildfire"] = new(FxShape.Flame, Rgb(255, 80, 40), Rgb(255, 210, 90), GMaxMotion.Lying, FxBlend.Add),
        ["G-Max Wind Rage"] = new(FxShape.Feather, Rgb(70, 80, 130), Rgb(200, 210, 230), GMaxMotion.Circling)
    };

    /// <summary>The Max and G-Max Moves that have a look of their own: every one of the 52.</summary>
    public static bool HasMaxLook(string move) =>
        TypeMaxMoves.ContainsKey(move) || GMaxMarks.ContainsKey(move) || string.Equals(move, "Max Guard", StringComparison.OrdinalIgnoreCase);

    /// <summary>The G-Max Moves with a mark of their own (33).</summary>
    public static IEnumerable<string> GMaxMoves => GMaxMarks.Keys;

    /// <summary>A Max Move's look: Max Guard's, a G-Max Move's (its type's with its own mark), or its type's.</summary>
    public static FxRecipe MaxMoveRecipe(string move, PokemonType type)
    {
        if (string.Equals(move, "Max Guard", StringComparison.OrdinalIgnoreCase)) return MaxGuard;
        if (GMaxMarks.TryGetValue(move, out var mark))
            return (in FxCtx c, FxColors col, FxList fx) =>
            {
                MaxTyped(c, col, fx, type);
                if (c.Cue.Landed) GMaxAccent(c, fx, mark);
            };
        return (in FxCtx c, FxColors col, FxList fx) => MaxTyped(c, col, fx, type);
    }

    private static void MaxTyped(in FxCtx c, FxColors col, FxList fx, PokemonType type)
    {
        MaxCommon(c, col, fx);
        if (c.Cue.Landed) MaxPiece(c, col, fx, type);
    }

    /// <summary>
    /// What every Max Move shares: a red pulse on the user, the crossing sheathed in Dynamax red (a beam of the type's
    /// colour for a special move, the speed lines and a red streak for a physical one), a landing at 1.5 with a ring
    /// of the type's light and a red one on the ground, and a red glow behind and under the target while its piece plays.
    /// </summary>
    private static void MaxCommon(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age;
        float g = Fx.Envelope(t, 0f, 0.08f, 0.22f, 0.34f);
        if (g > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.75f, Fx.Fade(DynaRed, g * 0.45f));
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * c.FromSize * 0.03f, Vector3.UnitY, c.FromSize * (1.0f - 0.4f * Math.Clamp(t / 0.34f, 0f, 1f)), Fx.Fade(DynaPink, g));
        }
        if (!c.Cue.OnSelf)
        {
            float reach = Fx.EaseOut(Math.Clamp((t - 0.1f) / 0.22f, 0f, 1f));
            float cross = Fx.Envelope(t, 0.1f, 0.14f, 0.36f, 0.48f);
            if (cross > 0f)
            {
                var end = Vector3.Lerp(c.From, c.Aim, reach);
                var pts = new[] { c.From, Vector3.Lerp(c.From, end, 0.5f), end };
                float size = (c.FromSize + c.ToSize) * 0.5f;
                bool physical = c.Cue.Category == MoveCategory.Physical;
                if (physical) SpeedLines(c, fx, 0.05f, 0.34f, 6, 0.6f);
                fx.Ribbon(pts, size * (physical ? 0.08f : 0.18f), Fx.Fade(DynaRed, cross * 0.6f), FxBlend.Alpha);
                fx.Ribbon(pts, size * (physical ? 0.03f : 0.07f), Fx.Fade(physical ? DynaPink : col.Light, cross));
            }
        }
        Impact(c, col, fx, 1.5f);
        if (!c.Cue.Landed) return;
        float h = c.ToSize;
        Fx.Shockwave(fx, t, c.To, Vector3.Zero, I + 0.04f, 0.5f, h * 1.6f, Fx.Fade(col.Light, 0.85f));
        Fx.Shockwave(fx, t, c.ToFeet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, I, 0.6f, h * 1.8f, Fx.Fade(DynaRed, 0.85f));
        float e = SetPiece(t);
        if (e <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To - Vector3.UnitZ * (c.ToHalfWidth + h * 0.25f), h * 1.3f, Fx.Fade(DynaRed, e * 0.4f), depth: true);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.4f, Fx.Fade(Fx.Mix(DynaRed, col.Main, 0.4f), e * 0.45f));
    }

    /// <summary>Where on a ring round the target's feet the <paramref name="i"/>th of <paramref name="n"/> stands.</summary>
    private static Vector3 AroundFeet(in FxCtx c, int i, int n, float r, float turn = 0f)
    {
        float a = i * MathF.Tau / n + turn;
        return c.ToFeet + new Vector3(MathF.Cos(a) * r, 0f, MathF.Sin(a) * r * 0.6f);
    }

    // ------------------------------------------------------------------ the types' set pieces

    private static void MaxPiece(in FxCtx c, FxColors col, FxList fx, PokemonType type)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        var mid = c.To;
        var feet = c.ToFeet;
        int s = c.Seed;
        float ring = w + h * 0.3f;
        switch (type)
        {
            case PokemonType.Fighting:
            {
                // A great fist falling on it out of the sky, a ring where it lands
                float k = Math.Clamp((t - I) / 0.25f, 0f, 1f);
                var p = Vector3.Lerp(mid + Vector3.UnitY * h * 0.75f, mid + Vector3.UnitY * h * 0.1f, k * k);
                fx.Sprite(FxShape.Fist, p, h * 0.6f, Fx.Fade(col.Light, e), FxBlend.Alpha, MathF.PI / 2f);
                fx.Sprite(FxShape.Glow, p, h * 0.5f, Fx.Fade(DynaRed, e * 0.4f));
                Fx.Shockwave(fx, t, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, I + 0.25f, 0.5f, h * 1.5f, Fx.Fade(col.Main, 0.9f));
                Fx.Burst(fx, s, t, mid, h, I + 0.25f, 0.6f, 10, FxShape.Spark, col.Light, White, 0.08f, 1.0f, salt: 71);
                break;
            }
            case PokemonType.Flying:
                // A tornado: rings of wind rising round it, wider as they rise
                for (int j = 0; j < 6; j++)
                {
                    float u = (t * 0.8f + j / 6f) % 1f;
                    fx.Flat(FxShape.Swirl, feet + Vector3.UnitY * h * (0.05f + 1.15f * u), Vector3.UnitY, (w + h * 0.1f) * (0.7f + 0.7f * u),
                        Fx.Fade(j % 2 == 0 ? col.Light : White, e * MathF.Sin(u * MathF.PI) * 0.9f), rotation: t * 9f + j, depth: false);
                }
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.Tau / 8f + t * 7f;
                    var p = feet + new Vector3(MathF.Cos(a) * ring, h * (0.2f + 0.1f * i), MathF.Sin(a) * ring * 0.6f);
                    fx.Streak(FxShape.Streak, p, new Vector3(-MathF.Sin(a), 0f, MathF.Cos(a)), h * 0.05f, 4f, Fx.Fade(White, e), depth: MathF.Sin(a) < 0f);
                }
                break;
            case PokemonType.Poison:
                // Three spouts of ooze bursting up round it, bubbles rising
                fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.1f, Fx.Fade(col.Dark, e * 0.8f), FxBlend.Alpha);
                for (int j = 0; j < 3; j++)
                {
                    var at = AroundFeet(c, j, 3, ring, 0.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        float u = (t * 1.6f + i / 5f + j * 0.3f) % 1f;
                        fx.Sprite(i % 2 == 0 ? FxShape.Bubble : FxShape.Drop, at + new Vector3(0f, h * 1.15f * u, 0f), h * (0.14f - 0.05f * u),
                            Fx.Fade(i % 2 == 0 ? col.Light : col.Main, e * (1f - u * 0.7f)), FxBlend.Alpha, depth: at.Z < feet.Z);
                    }
                }
                break;
            case PokemonType.Ground:
            {
                // Slabs of ground heaved up in a ring round it, and the stage shaking
                for (int i = 0; i < 8; i++)
                {
                    float k = Math.Clamp((t - I - i * 0.03f) / 0.6f, 0f, 1f);
                    if (k <= 0f) continue;
                    var at = AroundFeet(c, i, 8, ring * (1f + 0.3f * k));
                    float lift = MathF.Sin(k * MathF.PI) * h * 0.45f;
                    fx.Sprite(FxShape.Rock, at + Vector3.UnitY * (h * 0.1f + lift), h * 0.24f, Fx.Fade(i % 2 == 0 ? col.Main : col.Dark, e), FxBlend.Alpha, k * 3f + i, depth: at.Z < feet.Z);
                }
                fx.Flat(FxShape.Crack, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 1.4f * Fx.EaseOut((t - I) / 0.2f), Fx.Fade(col.Dark, e), FxBlend.Alpha, Fx.Rand(s, 0, 72) * MathF.Tau);
                fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(t, I, I + 0.05f, I + 0.35f, I + 0.7f));
                break;
            }
            case PokemonType.Rock:
                // Pillars of stone bursting up out of the ground round it
                for (int j = 0; j < 5; j++)
                {
                    var at = AroundFeet(c, j, 5, ring, 0.3f);
                    float k = Fx.EaseOut((t - I - j * 0.06f) / 0.25f);
                    if (k <= 0f) continue;
                    for (int i = 0; i < 3; i++)
                        fx.Sprite(FxShape.Rock, at + Vector3.UnitY * h * (0.12f + 0.25f * i) * k, h * (0.26f - 0.04f * i), Fx.Fade(i == 2 ? col.Light : col.Main, e), FxBlend.Alpha, i + j, depth: at.Z < feet.Z);
                }
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.1f, h, I + 0.05f, 0.7f, 8, FxShape.Rock, col.Main, col.Dark, 0.07f, 1.0f, 2.2f, FxBlend.Alpha, spin: 4f, salt: 73);
                break;
            case PokemonType.Bug:
                // A swarm of glowing motes spiralling in on it
                for (int i = 0; i < 22; i++)
                {
                    float u = (t * 0.9f + Fx.Rand(s, i, 74)) % 1f;
                    float a = Fx.Rand(s, i, 75) * MathF.Tau + u * MathF.Tau * 1.5f;
                    float r = (w + h * 0.15f) * (1.6f - 1.2f * u);
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.2f + 0.8f * Fx.Rand(s, i, 76)), MathF.Sin(a) * r * 0.6f);
                    fx.Sprite(i % 3 == 0 ? FxShape.Spore : FxShape.Glow, p, h * 0.06f, Fx.Fade(i % 2 == 0 ? col.Light : col.Main, e * MathF.Sin(u * MathF.PI)), depth: MathF.Sin(a) < 0f);
                }
                break;
            case PokemonType.Ghost:
            {
                // Wisps of shadow closing in on it into a dark orb that bursts
                float k = Math.Clamp((t - I) / 0.5f, 0f, 1f);
                for (int i = 0; i < 10; i++)
                {
                    if (k >= 1f) break;
                    var p = mid + Fx.Direction(s, i + 80) * h * 1.3f * (1f - Fx.EaseOut(k));
                    fx.Sprite(FxShape.Wisp, p, h * 0.16f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * (1f - k * 0.5f)));
                }
                float orb = Fx.Envelope(t, I + 0.3f, I + 0.5f, I + 0.55f, I + 0.7f);
                fx.Sprite(FxShape.Orb, mid, h * 0.6f * orb, Fx.Fade(Rgb(26, 14, 44), orb * 0.9f), FxBlend.Alpha, t * 3f);
                Fx.Shockwave(fx, t, mid, Vector3.Zero, I + 0.6f, 0.5f, h * 1.5f, Fx.Fade(col.Main, 0.9f));
                Fx.Burst(fx, s, t, mid, h, I + 0.6f, 0.5f, 10, FxShape.Wisp, col.Main, col.Dark, 0.12f, 1.0f, salt: 77);
                break;
            }
            case PokemonType.Steel:
                // Spikes of steel thrust up out of the ground round it in a ring
                for (int i = 0; i < 9; i++)
                {
                    var at = AroundFeet(c, i, 9, ring, 0.2f);
                    float k = Fx.EaseOut((t - I - 0.03f * i) / 0.15f);
                    if (k <= 0f) continue;
                    fx.Streak(FxShape.Shard, at + Vector3.UnitY * h * 0.25f * k, Vector3.UnitY, h * 0.12f, 3f * k, Fx.Fade(i % 2 == 0 ? col.Light : col.Main, e), FxBlend.Alpha, depth: at.Z < feet.Z);
                }
                Fx.Burst(fx, s, t, mid, h, I + 0.15f, 0.5f, 10, FxShape.Spark, White, col.Main, 0.07f, 1.0f, salt: 78);
                break;
            case PokemonType.Fire:
            {
                // A great ball of fire falling on it and bursting in flames
                float k = Math.Clamp((t - I) / 0.25f, 0f, 1f);
                if (k < 1f)
                {
                    var p = Vector3.Lerp(mid + new Vector3(h * 0.3f, h * 0.8f, 0f), mid, k * k);
                    fx.Sprite(FxShape.Orb, p, h * 0.5f, Fx.Fade(col.Main, e), FxBlend.Add, t * 6f);
                    fx.Sprite(FxShape.Flame, p, h * 0.45f, Fx.Fade(col.Light, e), rotation: MathF.PI);
                }
                Fx.Burst(fx, s, t, mid, h, I + 0.25f, 0.8f, 16, FxShape.Flame, col.Main, col.Light, 0.2f, 0.65f, salt: 79);
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, ring, Fx.Fade(col.Main, e), rotation: t * 3f);
                // Flames leaping up out of the ring where it burst
                if (t > I + 0.25f)
                    for (int i = 0; i < 8; i++)
                    {
                        float u = (t * 1.6f + i / 8f) % 1f;
                        var at = AroundFeet(c, i, 8, ring * 0.85f, 0.2f);
                        fx.Sprite(FxShape.Flame, at + Vector3.UnitY * h * (0.1f + 0.8f * u), h * 0.22f * (1f - 0.5f * u),
                            Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), depth: at.Z < feet.Z);
                    }
                break;
            }
            case PokemonType.Water:
                // Three geysers bursting up round it, drops falling back
                for (int j = 0; j < 3; j++)
                {
                    var at = AroundFeet(c, j, 3, ring, 0.2f);
                    fx.Flat(FxShape.Ring, at + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 0.3f, Fx.Fade(col.Light, e));
                    for (int i = 0; i < 6; i++)
                    {
                        float u = (t * 1.8f + i / 6f + j * 0.37f) % 1f;
                        var p = at + new Vector3(Fx.Signed(s, i + j * 6, 81) * h * 0.06f, h * 1.2f * MathF.Sin(u * MathF.PI * 0.9f), 0f);
                        fx.Sprite(FxShape.Drop, p, h * 0.12f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e), FxBlend.Alpha, u < 0.5f ? MathF.PI : 0f, depth: at.Z < feet.Z);
                    }
                }
                break;
            case PokemonType.Grass:
                // Leaves bursting up out of the ground and whirling up round it
                for (int i = 0; i < 16; i++)
                {
                    float u = (t * 1.1f + Fx.Rand(s, i, 82)) % 1f;
                    float a = i * MathF.Tau / 16f + u * MathF.Tau;
                    float r = ring * (0.8f + 0.3f * u);
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.2f * u, MathF.Sin(a) * r * 0.6f);
                    fx.Sprite(FxShape.Leaf, p, h * 0.13f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, a * 2f, depth: MathF.Sin(a) < 0f);
                }
                for (int j = 0; j < 4; j++)
                    fx.Flat(FxShape.Glow, AroundFeet(c, j, 4, ring, 0.8f) + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 0.25f, Fx.Fade(col.Light, e * 0.8f));
                break;
            case PokemonType.Electric:
                // A ring of bolts standing up out of the ground round it at once
                for (int j = 0; j < 6; j++)
                {
                    var at = AroundFeet(c, j, 6, ring);
                    Fx.Bolt(fx, t, s + j * 17, at, at + Vector3.UnitY * h * 1.2f, I + 0.02f * j, I + 0.55f, h * 0.05f, col.Main, col.Light, 0);
                }
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, ring, Fx.Fade(col.Main, e), rotation: -t * 5f);
                break;
            case PokemonType.Psychic:
            {
                // A sphere of pink light closing round it, and ripples out of it
                float k = Fx.EaseOut((t - I) / 0.35f);
                fx.Sprite(FxShape.Ring, mid, h * (1.4f - 0.55f * k), Fx.Fade(col.Main, e));
                fx.Sprite(FxShape.Ring, mid, h * (1.3f - 0.5f * k), Fx.Fade(col.Light, e * 0.8f), rotation: t * 2f);
                fx.Sprite(FxShape.Glow, mid, h * 0.9f, Fx.Fade(col.Main, e * 0.3f));
                for (int i = 0; i < 3; i++) Fx.Shockwave(fx, t, mid, Vector3.Zero, I + 0.4f + i * 0.15f, 0.45f, h * 1.6f, Fx.Fade(i == 1 ? col.Light : col.Main, 0.8f));
                break;
            }
            case PokemonType.Ice:
                // Hail falling on it and frost thrown up
                Fx.Rain(fx, t, s, mid, h, I, 1.35f, 0.3f, 16, FxShape.Shard, col.Light, White, 0.1f, FxBlend.Alpha, streak: 0.8f);
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.1f, h, I + 0.2f, 0.8f, 10, FxShape.Snowflake, White, col.Light, 0.07f, 0.9f, -0.2f, FxBlend.Alpha, spin: 3f, salt: 83);
                break;
            case PokemonType.Dragon:
            {
                // A column of dragon fire coiling up round it
                float reveal = Math.Clamp((t - I) / 0.4f, 0f, 1f);
                for (int k = 0; k < 2; k++)
                {
                    var helix = Helix(feet, h, w + h * 0.15f, w * 0.6f + h * 0.05f, 1.15f, 2.5f, k * MathF.PI + t * 5f, reveal, 20);
                    RoundRibbon(fx, helix, feet.Z, h * 0.07f, Fx.Fade(col.Main, e * 0.8f), FxBlend.Alpha);
                    RoundRibbon(fx, helix, feet.Z, h * 0.03f, Fx.Fade(col.Light, e), FxBlend.Add);
                }
                for (int i = 0; i < 6; i++)
                {
                    float u = (t * 1.5f + i / 6f) % 1f;
                    fx.Sprite(FxShape.Flame, feet + new Vector3(Fx.Signed(s, i, 84) * w * 0.6f, h * (0.9f + 0.25f * u), 0f), h * 0.12f, Fx.Fade(col.Light, e * (1f - u)));
                }
                break;
            }
            case PokemonType.Dark:
            {
                // Spikes of shadow rising round it and a dark orb over it
                for (int i = 0; i < 7; i++)
                {
                    var at = AroundFeet(c, i, 7, ring, 0.4f);
                    float k = Fx.EaseOut((t - I - 0.04f * i) / 0.2f);
                    if (k <= 0f) continue;
                    fx.Streak(FxShape.Shard, at + Vector3.UnitY * h * 0.3f * k, Vector3.UnitY, h * 0.13f, 3.5f * k, Fx.Fade(Rgb(30, 18, 40), e * 0.95f), FxBlend.Alpha, depth: at.Z < feet.Z);
                    fx.Sprite(FxShape.Glow, at + Vector3.UnitY * h * 0.55f * k, h * 0.08f, Fx.Fade(Rgb(186, 120, 255), e * k), depth: at.Z < feet.Z);
                }
                var over = feet + Vector3.UnitY * h * 1.12f;
                float grow = Fx.EaseOut((t - I - 0.1f) / 0.3f);
                fx.Sprite(FxShape.Orb, over, h * 0.28f * grow, Fx.Fade(Rgb(20, 10, 34), e * 0.95f), FxBlend.Alpha, t * 2f);
                fx.Sprite(FxShape.Ring, over, h * 0.32f * grow, Fx.Fade(Rgb(186, 120, 255), e), rotation: -t * 3f);
                break;
            }
            case PokemonType.Fairy:
                // Stars falling on it in a shower, a ring of pink light
                Fx.Rain(fx, t, s, mid, h, I, 1.35f, 0.3f, 16, FxShape.Star, col.Main, col.Light, 0.11f, FxBlend.Alpha);
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, ring, Fx.Fade(col.Main, e), rotation: t * 2f);
                Fx.Shockwave(fx, t, mid, Vector3.Zero, I + 0.5f, 0.6f, h * 1.5f, Fx.Fade(col.Light, 0.8f));
                break;
            default:
                // Normal: five stars of light falling round it, bursting where they land
                for (int i = 0; i < 5; i++)
                {
                    var at = AroundFeet(c, i, 5, ring, 0.6f);
                    float t0 = I + i * 0.11f;
                    float k = (t - t0) / 0.22f;
                    if (k < 0f) continue;
                    if (k < 1f) fx.Sprite(FxShape.Star, Vector3.Lerp(at + Vector3.UnitY * h * 1.2f, at + Vector3.UnitY * h * 0.1f, k * k), h * 0.2f,
                        Fx.Fade(Rgb(255, 240, 170), e), FxBlend.Alpha, k * 4f, depth: at.Z < feet.Z);
                    else Fx.Shockwave(fx, t, at + Vector3.UnitY * h * 0.04f, Vector3.UnitY, t0 + 0.22f, 0.4f, h * 0.5f, Fx.Fade(White, 0.9f));
                }
                fx.Sprite(FxShape.Star, mid, h * 0.35f * e, Fx.Fade(White, e * 0.6f), rotation: t);
                break;
        }
    }

    // ------------------------------------------------------------------ Max Guard

    /// <summary>Max Guard, on the user: a dome of red hexagons closing round it and held, a ring at its feet, a glint before it.</summary>
    private static void MaxGuard(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        fx.Land(I);
        float env = Fx.Envelope(t, 0.05f, 0.25f, 1.1f, 1.4f);
        if (env <= 0f) return;
        float close = Fx.EaseOut(Math.Clamp((t - 0.05f) / 0.3f, 0f, 1f));
        float r = (w + h * 0.2f) * (1.4f - 0.4f * close);
        (float Y, float R, int N)[] rows = { (0.2f, 1f, 8), (0.7f, 0.8f, 5), (1.1f, 0f, 1) };
        int n = 0;
        foreach (var row in rows)
            for (int i = 0; i < row.N; i++, n++)
            {
                float a = i * MathF.Tau / row.N + row.Y;
                var p = feet + new Vector3(MathF.Cos(a) * r * row.R, h * Math.Min(row.Y, 1.08f), MathF.Sin(a) * r * row.R * 0.6f);
                bool behind = row.R > 0f && MathF.Sin(a) < 0f;
                fx.Sprite(FxShape.Hex, p, h * 0.3f, Fx.Fade(DynaRed, env * 0.45f), FxBlend.Alpha, a * 0.2f, depth: behind);
                if (n % 2 == 0) fx.Sprite(FxShape.Hex, p, h * 0.3f, Fx.Fade(DynaPink, env * 0.5f * (0.7f + 0.3f * MathF.Sin(t * 6f + n))), rotation: a * 0.2f, depth: behind);
            }
        fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, r, Fx.Fade(DynaPink, env));
        Fx.Glint(fx, t, c.From + Vector3.UnitZ * (w + h * 0.2f), h, 0.4f, White);
    }

    // ------------------------------------------------------------------ the G-Max Moves' marks

    /// <summary>A G-Max Move's own mark over its type's Max Move: at most 10 quads, by its motion.</summary>
    private static void GMaxAccent(in FxCtx c, FxList fx, GMaxMark m)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        var mid = c.To;
        int s = c.Seed;
        float ring = w + h * 0.2f;
        switch (m.Motion)
        {
            case GMaxMotion.Circling:
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.Tau / 8f + t * 3f;
                    var p = feet + new Vector3(MathF.Cos(a) * ring, h * (0.45f + 0.15f * MathF.Sin(a * 2f + t)), MathF.Sin(a) * ring * 0.6f);
                    fx.Sprite(m.Shape, p, h * 0.12f, Fx.Fade(i % 2 == 0 ? m.A : m.B, e), m.Blend, a, depth: MathF.Sin(a) < 0f);
                }
                break;
            case GMaxMotion.Falling:
                Fx.Rain(fx, t, s + 91, mid, h, I + 0.1f, 1.35f, 0.3f, 9, m.Shape, m.A, m.B, 0.11f, m.Blend);
                break;
            case GMaxMotion.Rising:
                for (int i = 0; i < 8; i++)
                {
                    float u = (t * 1.2f + i / 8f) % 1f;
                    var p = feet + new Vector3(Fx.Signed(s, i, 92) * w * 0.7f, h * (0.1f + 1.1f * u), h * 0.15f);
                    fx.Sprite(m.Shape, p, h * 0.1f, Fx.Fade(i % 2 == 0 ? m.A : m.B, e * MathF.Sin(u * MathF.PI)), m.Blend, u * 2f);
                }
                break;
            case GMaxMotion.Thrown:
                Fx.Burst(fx, s, t, mid, h, I + 0.25f, 0.8f, 10, m.Shape, m.A, m.B, 0.1f, 1.2f, 0.4f, m.Blend, spin: 3f, salt: 93);
                break;
            case GMaxMotion.Lying:
            {
                float spread = Fx.EaseOut((t - I) / 0.4f);
                for (int i = 0; i < 7; i++)
                {
                    var at = AroundFeet(c, i, 7, ring * (0.8f + 0.5f * spread), 0.25f);
                    fx.Flat(m.Shape, at + Vector3.UnitY * h * 0.04f, Vector3.UnitY, h * 0.18f, Fx.Fade(i % 2 == 0 ? m.A : m.B, e), m.Blend, i + t);
                }
                break;
            }
            case GMaxMotion.Sign:
            {
                var at = feet + Vector3.UnitY * h * 1.08f;
                float grow = Fx.EaseOut((t - I) / 0.3f);
                fx.Sprite(FxShape.Glow, at, h * 0.4f * grow, Fx.Fade(m.B, e * 0.5f));
                fx.Sprite(m.Shape, at, h * 0.3f * grow, Fx.Fade(m.A, e), m.Blend, 0.1f * MathF.Sin(t * 4f));
                break;
            }
        }
    }
}
