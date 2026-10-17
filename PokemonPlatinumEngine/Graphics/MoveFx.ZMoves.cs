using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// The Z-Moves (plan 25 · Y14; style guide, "Mega Evolution, Ultra Burst and Z-Moves"): the eighteen of the types, one
// recipe to a type whatever move it was made of, and the seventeen of single species, each built from the same pieces.
// Every one is our own, its beats too (no original file has them): a short gathering on the user, the crossing, a
// landing at ImpactTime bigger than any move's, then the type's set piece round the target to 1.4 s, all faded by
// BattleAnimator.ZMoveTime. Pure in the cue's age and seed, under MaxQuads at every age; no flash over 0.35, no shake
// over 0.5.
internal static partial class MoveFx
{
    /// <summary>When a Z-Move's set piece round its target is at its height.</summary>
    private static float SetPiece(float age) => Fx.Envelope(age, I, I + 0.1f, 1.15f, 1.4f);

    /// <summary>The Z-Moves that have a look of their own: every one of the 35.</summary>
    public static bool HasZLook(string move) => ZSignatures.Value.ContainsKey(move) || TypeZMoveNames.Contains(move);

    private static readonly HashSet<string> TypeZMoveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Breakneck Blitz", "All-Out Pummeling", "Supersonic Skystrike", "Acid Downpour", "Tectonic Rage", "Continental Crush",
        "Savage Spin-Out", "Never-Ending Nightmare", "Corkscrew Crash", "Inferno Overdrive", "Hydro Vortex", "Bloom Doom",
        "Gigavolt Havoc", "Shattered Psyche", "Subzero Slammer", "Devastating Drake", "Black Hole Eclipse", "Twinkle Tackle"
    };

    /// <summary>A Z-Move's look: a species' own, or its type's.</summary>
    public static FxRecipe ZMoveRecipe(string move, PokemonType type) =>
        ZSignatures.Value.TryGetValue(move, out var own) ? own : (in FxCtx c, FxColors col, FxList fx) => ZTyped(c, col, fx, type);

    /// <summary>A type's Z-Move: the common beats and the type's set piece.</summary>
    private static void ZTyped(in FxCtx c, FxColors col, FxList fx, PokemonType type)
    {
        ZCommon(c, col, fx);
        if (c.Cue.Landed) TypePiece(c, col, fx, type);
    }

    /// <summary>
    /// What every Z-Move shares: a glow and a ring gathering on the user, the crossing (speed lines for a physical
    /// one, an orb of its type for any other), and a landing bigger than any move's with two rings.
    /// </summary>
    private static void ZCommon(in FxCtx c, FxColors col, FxList fx, bool cross = true, float scale = 1.3f)
    {
        float t = c.Age;
        float g = Fx.Envelope(t, 0f, 0.08f, 0.2f, 0.32f);
        if (g > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.7f, Fx.Fade(col.Main, g * 0.5f));
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * c.FromSize * 0.03f, Vector3.UnitY, c.FromSize * (0.95f - 0.4f * Math.Clamp(t / 0.32f, 0f, 1f)), Fx.Fade(col.Light, g));
        }
        if (cross && !c.Cue.OnSelf)
        {
            if (c.Cue.Category == MoveCategory.Physical) SpeedLines(c, fx, 0.05f, 0.34f, 6, 0.6f);
            else Fx.Orb(fx, c, 0.06f, I, 0.3f, col.Main, col.Light);
        }
        Impact(c, col, fx, scale);
        if (!c.Cue.Landed) return;
        float h = c.ToSize;
        Fx.Shockwave(fx, t, c.To, Vector3.Zero, I + 0.04f, 0.5f, h * 1.7f, Fx.Fade(col.Light, 0.85f));
        Fx.Shockwave(fx, t, c.ToFeet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, I, 0.6f, h * 1.9f, Fx.Fade(col.Main, 0.85f));
        // The type's light standing behind it and lying under it for as long as the set piece plays: what makes a
        // Z-Move read bigger than any move from the overview
        float e = SetPiece(t);
        if (e <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To - Vector3.UnitZ * (c.ToHalfWidth + h * 0.25f), h * 1.35f, Fx.Fade(col.Main, e * 0.45f), depth: true);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.5f, Fx.Fade(col.Main, e * 0.5f));
    }

    // ------------------------------------------------------------------ the types' set pieces

    private static void TypePiece(in FxCtx c, FxColors col, FxList fx, PokemonType type)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        var mid = c.To;
        var feet = c.ToFeet;
        int s = c.Seed;
        switch (type)
        {
            case PokemonType.Fighting:
                // Fists of light raining blows on it from every side
                for (int i = 0; i < 14; i++)
                {
                    float k = (t - I - i * 0.055f) / 0.24f;
                    if (k < 0f || k > 1f) continue;
                    var dir = Fx.Direction(s, i + 200);
                    var p = mid + dir * h * (0.95f - 0.55f * Fx.EaseOut(k));
                    fx.Sprite(FxShape.Fist, p, h * 0.28f, Fx.Fade(col.Light, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.Atan2(-dir.Y, -dir.X));
                    if (k > 0.6f) fx.Sprite(FxShape.Glow, p, h * 0.16f, Fx.Fade(col.Main, (1f - k) * 2f));
                }
                break;
            case PokemonType.Flying:
                // A dive out of the sky in long streaks, and a ring of wind on the ground
                for (int i = 0; i < 10; i++)
                {
                    float k = (t - I + 0.05f - i * 0.07f) / 0.25f;
                    if (k < 0f || k > 1f) continue;
                    var start = mid + new Vector3(h * (0.8f + 0.4f * Fx.Signed(s, i, 210)), h * 1.4f, h * 0.3f * Fx.Signed(s, i, 211));
                    var p = Vector3.Lerp(start, mid, k);
                    fx.Streak(FxShape.Streak, p, mid - start, h * 0.08f, 8f, Fx.Fade(i % 2 == 0 ? White : col.Light, MathF.Sin(k * MathF.PI)));
                }
                fx.Flat(FxShape.Swirl, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, h * 1.3f, Fx.Fade(col.Light, e * 0.9f), rotation: t * 8f);
                fx.Sprite(FxShape.Swirl, feet + Vector3.UnitY * h * 0.3f, h * 0.9f, Fx.Fade(White, e * 0.6f), rotation: -t * 9f, depth: true);
                break;
            case PokemonType.Poison:
                // A pool of poison under it and a downpour of purple drops
                fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.1f, Fx.Fade(col.Dark, e * 0.85f), FxBlend.Alpha);
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 1.0f, Fx.Fade(col.Main, e));
                Fx.Rain(fx, t, s, mid, h, I, 1.35f, 0.35f, 22, FxShape.Drop, col.Main, col.Light, 0.07f, FxBlend.Alpha, streak: 1.2f);
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.1f, h, I + 0.3f, 0.8f, 8, FxShape.Bubble, col.Main, col.Light, 0.08f, 0.6f, -0.6f, FxBlend.Alpha, salt: 40);
                break;
            case PokemonType.Ground:
                // The ground split under it, rocks thrown up, and the stage shaking
                fx.Flat(FxShape.Crack, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 1.3f * Fx.EaseOut((t - I) / 0.2f), Fx.Fade(col.Dark, e), FxBlend.Alpha, Fx.Rand(s, 0, 220) * MathF.Tau);
                fx.Flat(FxShape.Crack, feet + Vector3.UnitY * h * 0.035f, Vector3.UnitY, h * 1.0f * Fx.EaseOut((t - I) / 0.2f), Fx.Fade(col.Dark, e), FxBlend.Alpha, Fx.Rand(s, 1, 220) * MathF.Tau);
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.1f, h, I + 0.05f, 0.85f, 14, FxShape.Rock, col.Main, col.Dark, 0.12f, 1.4f, 2.4f, FxBlend.Alpha, spin: 4f, salt: 50);
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.15f, h, I + 0.1f, 0.95f, 6, FxShape.Smoke, Fx.Fade(col.Light, 0.8f), Fx.Fade(col.Main, 0.7f), 0.3f, 0.6f, -0.3f, FxBlend.Alpha, salt: 51);
                fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(t, I, I + 0.05f, I + 0.3f, I + 0.6f));
                break;
            case PokemonType.Rock:
                // A rain of great boulders
                Fx.Rain(fx, t, s, mid, h, I, 1.35f, 0.35f, 9, FxShape.Rock, col.Light, col.Dark, 0.2f, FxBlend.Alpha);
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.1f, h, I + 0.5f, 0.7f, 10, FxShape.Rock, col.Main, col.Dark, 0.08f, 1.1f, 2.4f, FxBlend.Alpha, spin: 4f, salt: 52);
                break;
            case PokemonType.Bug:
            {
                // Silk wound round and round it in a cocoon
                float reveal = Math.Clamp((t - I) / 0.45f, 0f, 1f);
                for (int k = 0; k < 4; k++)
                {
                    var helix = Helix(feet, h, w + h * 0.12f, w * 0.7f + h * 0.05f, 1.0f, 3f, k * MathF.PI / 2f + t * 6f, reveal, 24);
                    RoundRibbon(fx, helix, feet.Z, h * 0.025f, Fx.Fade(Rgb(240, 244, 222), e * 0.85f), FxBlend.Alpha);
                }
                Fx.Burst(fx, s, t, mid, h, I + 0.2f, 0.9f, 10, FxShape.Spore, col.Main, col.Light, 0.06f, 0.9f, 0.3f, salt: 53);
                break;
            }
            case PokemonType.Ghost:
                // A pool of shadow, and wisps and claws of shadow rising out of it
                fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.2f, Fx.Fade(Rgb(26, 14, 44), e * 0.9f), FxBlend.Alpha);
                for (int i = 0; i < 9; i++)
                {
                    float u = (t * 0.9f + Fx.Rand(s, i, 230)) % 1f;
                    float a = i * MathF.Tau / 9f + t * 2f;
                    float r = w + h * 0.1f;
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.15f * u, MathF.Sin(a) * r * 0.6f);
                    fx.Sprite(FxShape.Wisp, p, h * 0.18f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), depth: MathF.Sin(a) < 0f);
                }
                for (int i = 0; i < 4; i++)
                {
                    float k = (t - I - 0.1f - i * 0.12f) / 0.5f;
                    if (k < 0f || k > 1f) continue;
                    float x = (i - 1.5f) * w * 0.7f;
                    var p = feet + new Vector3(x, h * (0.1f + 0.75f * Fx.EaseOut(k)), h * 0.15f);
                    fx.Sprite(FxShape.Claw, p, h * 0.22f, Fx.Fade(col.Dark, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.PI * (0.4f + 0.2f * i));
                }
                break;
            case PokemonType.Steel:
            {
                // A drill of steel turning into it, sparks thrown off
                var axis = c.To - c.From;
                var (across, up) = Fx.Frame(axis);
                var n = Vector3.Normalize(axis);
                float k = Math.Clamp((t - I) / 0.7f, 0f, 1f);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * MathF.Tau / 12f + t * 14f;
                    float r = h * 0.5f * (1f - 0.7f * Fx.EaseOut(k));
                    var p = mid - n * h * (0.35f - 0.25f * k) + (across * MathF.Cos(a) + up * MathF.Sin(a)) * r;
                    fx.Streak(FxShape.Streak, p, axis, h * 0.06f, 4f, Fx.Fade(i % 2 == 0 ? col.Light : col.Main, e));
                }
                Fx.Burst(fx, s, t, mid, h, I + 0.1f, 0.6f, 12, FxShape.Spark, White, col.Main, 0.08f, 1.1f, salt: 54);
                Fx.Burst(fx, s, t, mid, h, I + 0.5f, 0.6f, 10, FxShape.Streak, col.Light, White, 0.05f, 1.2f, 0.6f, streak: 2.5f, salt: 55);
                break;
            }
            case PokemonType.Fire:
                // A column of fire, flames whirling up it
                for (int i = 0; i < 16; i++)
                {
                    float u = (t * 1.4f + Fx.Rand(s, i, 240)) % 1f;
                    float a = i * MathF.Tau / 16f + t * 6f;
                    float r = w * 0.9f + h * 0.05f;
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.25f * u, MathF.Sin(a) * r * 0.6f);
                    fx.Sprite(FxShape.Flame, p, h * 0.22f * (1f - 0.4f * u), Fx.Fade(i % 3 == 0 ? col.Light : col.Main, e * MathF.Sin(u * MathF.PI)), depth: MathF.Sin(a) < 0f);
                }
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, w + h * 0.3f, Fx.Fade(col.Main, e), rotation: t * 4f);
                fx.Sprite(FxShape.Glow, mid, h * 0.9f, Fx.Fade(col.Main, e * 0.3f), depth: true);
                break;
            case PokemonType.Water:
                // A whirlpool of drops round it, rising
                for (int i = 0; i < 18; i++)
                {
                    float u = (t * 0.9f + Fx.Rand(s, i, 250)) % 1f;
                    float a = i * MathF.Tau / 18f + t * 9f;
                    float r = (w + h * 0.15f) * (1.15f - 0.4f * u);
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.2f * u, MathF.Sin(a) * r * 0.6f);
                    fx.Sprite(FxShape.Drop, p, h * 0.1f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
                }
                fx.Flat(FxShape.Swirl, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, h * 1.25f, Fx.Fade(col.Light, e), rotation: -t * 9f);
                break;
            case PokemonType.Grass:
            {
                // Flowers of light opening round its feet, and petals and leaves thrown up
                float open = Fx.EaseOut((t - I) / 0.35f);
                for (int f = 0; f < 4; f++)
                {
                    float a = f * MathF.Tau / 4f + MathF.PI / 4f;
                    var at = feet + new Vector3(MathF.Cos(a) * (w + h * 0.3f), h * 0.04f, MathF.Sin(a) * (w + h * 0.3f) * 0.6f);
                    fx.Flat(FxShape.Glow, at, Vector3.UnitY, h * 0.18f * open, Fx.Fade(Rgb(255, 236, 140), e));
                    for (int p = 0; p < 5; p++)
                    {
                        float pa = p * MathF.Tau / 5f + t;
                        fx.Flat(FxShape.Petal, at + new Vector3(MathF.Cos(pa), 0f, MathF.Sin(pa)) * h * 0.12f * open, Vector3.UnitY, h * 0.11f * open,
                            Fx.Fade(f % 2 == 0 ? Rgb(255, 150, 200) : Rgb(255, 230, 120), e), FxBlend.Alpha, pa);
                    }
                }
                Fx.Burst(fx, s, t, feet + Vector3.UnitY * h * 0.2f, h, I + 0.15f, 0.95f, 14, FxShape.Leaf, col.Main, col.Light, 0.1f, 1.2f, 0.6f, FxBlend.Alpha, spin: 6f, salt: 56);
                break;
            }
            case PokemonType.Electric:
                // Bolts striking down from the sky, one after another
                for (int i = 0; i < 7; i++)
                {
                    float t0 = I + i * 0.12f;
                    var top = mid + new Vector3(h * 0.9f * Fx.Signed(s, i, 260), h * 2.6f, -h * 0.6f);
                    Fx.Bolt(fx, t, s + i * 31, top, feet + Vector3.UnitY * h * 0.3f, t0, t0 + 0.24f, h * 0.08f, col.Main, col.Light, 1);
                }
                fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.04f, Vector3.UnitY, w + h * 0.35f, Fx.Fade(col.Main, e), rotation: t * 5f);
                Fx.Burst(fx, s, t, mid, h, I + 0.2f, 0.8f, 10, FxShape.Spark, col.Main, col.Light, 0.1f, 1.0f, salt: 57);
                break;
            case PokemonType.Psychic:
                // Rings of pink light closing in, and bursting outward
                for (int i = 0; i < 4; i++)
                {
                    float k = (t - I - i * 0.1f) / 0.4f;
                    if (k < 0f || k > 1f) continue;
                    fx.Sprite(FxShape.Ring, mid, h * (1.5f - 1.2f * k), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, MathF.Sin(k * MathF.PI)));
                }
                Fx.Shockwave(fx, t, mid, Vector3.Zero, 0.85f, 0.5f, h * 1.8f, col.Main);
                Fx.Shockwave(fx, t, mid, Vector3.Zero, 0.92f, 0.45f, h * 1.4f, col.Light);
                Fx.Burst(fx, s, t, mid, h, 0.85f, 0.5f, 10, FxShape.Spark, col.Light, col.Main, 0.09f, 1.0f, salt: 58);
                break;
            case PokemonType.Ice:
            {
                // Shards of ice closing on it in a ring, and frost thrown out
                float k = Fx.EaseOut((t - I) / 0.5f);
                Encircle(fx, feet, h * 0.45f, (w + h * 0.1f) * (1.7f - 0.7f * k), 8, 1.5f, t, s, FxShape.Shard, col.Light, col.Main, h * 0.2f, FxBlend.Alpha, e);
                Encircle(fx, feet, h * 0.85f, (w + h * 0.05f) * (1.5f - 0.6f * k), 5, -1.2f, t, s + 7, FxShape.Shard, col.Light, White, h * 0.14f, FxBlend.Alpha, e);
                Fx.Burst(fx, s, t, mid, h, I + 0.45f, 0.6f, 10, FxShape.Snowflake, White, col.Light, 0.08f, 1.0f, 0.4f, FxBlend.Alpha, spin: 3f, salt: 59);
                break;
            }
            case PokemonType.Dragon:
                // Dragon fire streaming round it in two spirals
                for (int j = 0; j < 2; j++)
                    for (int i = 0; i < 10; i++)
                    {
                        float u = (i / 10f + t * 0.8f) % 1f;
                        float a = j * MathF.PI + u * MathF.Tau * 2f + t * 3f;
                        float r = w + h * 0.12f;
                        var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.2f * u, MathF.Sin(a) * r * 0.6f);
                        fx.Sprite(FxShape.Flame, p, h * 0.17f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), rotation: a, depth: MathF.Sin(a) < 0f);
                    }
                break;
            case PokemonType.Dark:
            {
                // A disc of dark behind it, its rim of violet light drawing motes in
                var behind = mid - Vector3.UnitZ * (w + h * 0.2f);
                float grow = Fx.EaseOut((t - I) / 0.3f);
                fx.Sprite(FxShape.Orb, behind, h * 0.75f * grow, Fx.Fade(Rgb(18, 8, 30), e * 0.95f), FxBlend.Alpha, t * 2f, depth: true);
                fx.Sprite(FxShape.Ring, behind, h * 0.8f * grow, Fx.Fade(Rgb(186, 120, 255), e), rotation: -t * 3f, depth: true);
                fx.Sprite(FxShape.Swirl, behind, h * 0.7f * grow, Fx.Fade(Rgb(120, 70, 190), e * 0.8f), rotation: t * 6f, depth: true);
                for (int i = 0; i < 12; i++)
                {
                    float k = (t * 1.2f + Fx.Rand(s, i, 270)) % 1f;
                    var p = behind + Fx.Direction(s, i + 270) * h * 1.3f * (1f - k);
                    fx.Sprite(FxShape.Glow, p, h * 0.05f, Fx.Fade(col.Light, e * k));
                }
                break;
            }
            case PokemonType.Fairy:
                // A field of stars, hearts and sparkles round it
                for (int i = 0; i < 14; i++)
                {
                    float u = (t * 0.6f + Fx.Rand(s, i, 280)) % 1f;
                    float a = i * MathF.Tau / 14f + t * 2.5f;
                    float r = w + h * (0.15f + 0.2f * Fx.Rand(s, i, 281));
                    var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.1f + 1.1f * u), MathF.Sin(a) * r * 0.6f);
                    var shape = (i % 3) switch { 0 => FxShape.Heart, 1 => FxShape.Star, _ => FxShape.Spark };
                    fx.Sprite(shape, p, h * 0.1f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, e * MathF.Sin(u * MathF.PI)), shape == FxShape.Spark ? FxBlend.Add : FxBlend.Alpha, t * 2f + i, depth: MathF.Sin(a) < 0f);
                }
                Fx.Shockwave(fx, t, mid, Vector3.Zero, I + 0.3f, 0.6f, h * 1.6f, Fx.Fade(col.Light, 0.8f));
                break;
            default:
                // Normal: rays out of a great star of light, and three rings in turn
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.Tau / 6f + 0.3f;
                    var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                    fx.Streak(FxShape.Streak, mid + dir * h * 0.6f * Fx.EaseOut((t - I) / 0.3f), dir, h * 0.09f, 6f, Fx.Fade(i % 2 == 0 ? White : col.Light, e));
                }
                fx.Sprite(FxShape.Star, mid, h * 0.45f * e, Fx.Fade(Rgb(255, 240, 160), e * 0.9f), FxBlend.Alpha, t * 1.5f);
                for (int i = 0; i < 3; i++) Fx.Shockwave(fx, t, mid, Vector3.Zero, I + 0.15f + i * 0.2f, 0.5f, h * 1.6f, Fx.Fade(i == 1 ? Rgb(255, 230, 140) : White, 0.85f));
                break;
        }
    }

    // ------------------------------------------------------------------ the species' own

    private static readonly Lazy<Dictionary<string, FxRecipe>> ZSignatures = new(() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Catastropika"] = Catastropika,
        ["Sinister Arrow Raid"] = SinisterArrowRaid,
        ["Malicious Moonsault"] = MaliciousMoonsault,
        ["Oceanic Operetta"] = OceanicOperetta,
        ["Guardian of Alola"] = GuardianOfAlola,
        ["Soul-Stealing 7-Star Strike"] = SevenStarStrike,
        ["Stoked Sparksurfer"] = StokedSparksurfer,
        ["Pulverizing Pancake"] = PulverizingPancake,
        ["Extreme Evoboost"] = ExtremeEvoboost,
        ["Genesis Supernova"] = GenesisSupernova,
        ["10,000,000 Volt Thunderbolt"] = TenMillionVolts,
        ["Light That Burns the Sky"] = LightThatBurnsTheSky,
        ["Searing Sunraze Smash"] = SearingSunrazeSmash,
        ["Menacing Moonraze Maelstrom"] = MenacingMoonrazeMaelstrom,
        ["Let's Snuggle Forever"] = LetsSnuggleForever,
        ["Splintered Stormshards"] = SplinteredStormshards,
        ["Clangorous Soulblaze"] = ClangorousSoulblaze
    });

    /// <summary>The species' own Z-Moves, by name.</summary>
    public static IEnumerable<string> ZSignatureMoves => ZSignatures.Value.Keys;

    private static void Catastropika(in FxCtx c, FxColors col, FxList fx)
    {
        // A ball of lightning dropping on it from above, then the bolts of the sky
        float h = c.ToSize;
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        if (c.Age < I)
        {
            var p = Vector3.Lerp(c.To + Vector3.UnitY * h * 1.3f, c.To, k * k);
            fx.Sprite(FxShape.Glow, p, h * 0.55f, Fx.Fade(col.Main, 0.8f));
            fx.Sprite(FxShape.Orb, p, h * 0.3f, Fx.Fade(col.Light, 0.9f), rotation: c.Age * 8f);
            for (int i = 0; i < 3; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i * 13, p, p + Fx.Direction(c.Seed, i + 290) * h * 0.4f, 0f, I, h * 0.025f, col.Main, col.Light, 0);
        }
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        TypePiece(c, col, fx, PokemonType.Electric);
        // The ball broken open round it: a crackling cage of sparks turning on it
        float e = SetPiece(c.Age);
        Encircle(fx, c.ToFeet, h * 0.55f, c.ToHalfWidth + h * 0.2f, 8, 5f, c.Age, c.Seed, FxShape.Spark, col.Light, White, h * 0.12f, FxBlend.Add, e);
    }

    private static void SinisterArrowRaid(in FxCtx c, FxColors col, FxList fx)
    {
        // Arrows of shadow raining down on it, over a pool of shadow
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        float h = c.ToSize, e = SetPiece(c.Age);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 1.1f, Fx.Fade(Rgb(26, 14, 44), e * 0.9f), FxBlend.Alpha);
        for (int i = 0; i < 16; i++)
        {
            float t0 = 0.05f + i * 0.07f;
            float k = (c.Age - t0) / 0.3f;
            if (k < 0f || k > 1f) continue;
            var top = c.To + new Vector3(h * (0.5f + 0.6f * Fx.Signed(c.Seed, i, 300)), h * 1.6f, h * 0.4f * Fx.Signed(c.Seed, i, 301));
            var end = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 302) * h * 0.5f, h * 0.1f, Fx.Signed(c.Seed, i, 303) * h * 0.3f);
            var p = Vector3.Lerp(top, end, k);
            fx.Streak(FxShape.Streak, p, end - top, h * 0.06f, 5f, Fx.Fade(col.Main, 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 0.07f, Fx.Fade(col.Light, 0.9f));
        }
    }

    private static void MaliciousMoonsault(in FxCtx c, FxColors col, FxList fx)
    {
        // A dark moon rising over it and dropping on it
        float h = c.ToSize;
        float rise = Fx.EaseOut(c.Age / 0.25f);
        if (c.Age < I + 0.1f)
        {
            float drop = Math.Clamp((c.Age - 0.25f) / 0.1f, 0f, 1f);
            var p = c.To + Vector3.UnitY * h * (0.4f + 0.85f * rise - 0.85f * drop);
            fx.Sprite(FxShape.Orb, p, h * 0.32f, Fx.Fade(Rgb(52, 34, 70), 0.95f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, p, h * 0.34f, Fx.Fade(Rgb(255, 236, 160), 0.9f));
        }
        ZCommon(c, col, fx, cross: false, scale: 1.4f);
        if (!c.Cue.Landed) return;
        fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.2f, I + 0.45f));
        TypePiece(c, col, fx, PokemonType.Dark);
    }

    private static void OceanicOperetta(in FxCtx c, FxColors col, FxList fx)
    {
        // A great bubble of water carried over it and breaking on it
        float h = c.ToSize;
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        if (c.Age < I)
        {
            var p = Vector3.Lerp(c.From, c.To + Vector3.UnitY * h * 0.9f, Fx.EaseOut(k));
            float size = c.ScaleAt(k) * 0.5f;
            fx.Sprite(FxShape.Bubble, p, size, Fx.Fade(col.Light, 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, size * 1.2f, Fx.Fade(col.Main, 0.5f));
        }
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        Fx.Burst(fx, c.Seed, c.Age, c.To + Vector3.UnitY * h * 0.4f, h, I, 0.8f, 20, FxShape.Drop, col.Main, col.Light, 0.1f, 1.3f, 2.6f, FxBlend.Alpha, salt: 60);
        TypePiece(c, col, fx, PokemonType.Water);
    }

    private static void GuardianOfAlola(in FxCtx c, FxColors col, FxList fx)
    {
        // A giant palm of light pressing down on it
        float h = c.ToSize;
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.25f, I + 0.55f);
        var p = c.To + Vector3.UnitY * h * (1.6f - 1.1f * k * k) + Vector3.UnitZ * h * 0.2f;
        fx.Sprite(FxShape.Palm, p, h * 0.85f, Fx.Fade(Rgb(214, 196, 150), fade * 0.9f), FxBlend.Alpha, MathF.PI);
        fx.Sprite(FxShape.Glow, p, h * 0.9f, Fx.Fade(col.Light, fade * 0.4f));
        ZCommon(c, col, fx, cross: false, scale: 1.4f);
        if (!c.Cue.Landed) return;
        fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.25f, I + 0.5f));
        Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, I + 0.1f, 0.7f, h * 2.1f, Fx.Fade(Rgb(255, 236, 170), 0.9f));
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.15f, h, I + 0.05f, 0.9f, 8, FxShape.Smoke, Rgb(236, 226, 200, 220), Rgb(200, 190, 170, 200), 0.3f, 0.7f, -0.3f, FxBlend.Alpha, salt: 61);
    }

    private static void SevenStarStrike(in FxCtx c, FxColors col, FxList fx)
    {
        // Seven stars round it, then a fist of shadow through them
        ZCommon(c, col, fx);
        if (!c.Cue.Landed) return;
        float h = c.ToSize, e = SetPiece(c.Age);
        for (int i = 0; i < 7; i++)
        {
            float shown = Math.Clamp((c.Age - I - i * 0.06f) / 0.1f, 0f, 1f);
            float a = i * MathF.Tau / 7f + c.Age * 1.5f;
            var p = c.To + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * (c.ToHalfWidth + h * 0.3f);
            fx.Sprite(FxShape.Star, p, h * 0.1f * shown, Fx.Fade(Rgb(255, 226, 110), e), FxBlend.Alpha, c.Age * 3f);
            fx.Sprite(FxShape.Glow, p, h * 0.12f * shown, Fx.Fade(col.Light, e * 0.8f));
        }
        float k = (c.Age - 0.8f) / 0.3f;
        if (k >= 0f && k <= 1f)
        {
            var from = c.To + Vector3.UnitX * h * (0.9f - 0.9f * k) + Vector3.UnitZ * h * 0.3f;
            fx.Sprite(FxShape.Fist, from, h * 0.35f, Fx.Fade(col.Dark, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, MathF.PI);
            Fx.Contact(fx, c.Age, c.Seed + 9, c.To, h, 1.05f, 0.4f, col.Main, col.Light);
        }
    }

    private static void StokedSparksurfer(in FxCtx c, FxColors col, FxList fx)
    {
        // A wave carrying bolts across
        var water = ColorsOf(PokemonType.Water);
        Fx.WaveWall(fx, c, 0f, I + 0.1f, water.Main, water.Light);
        if (c.Age < I)
            Fx.Bolt(fx, c.Age, c.Seed, c.From, c.To, 0.1f, I, c.ToSize * 0.05f, col.Main, col.Light, 1);
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        TypePiece(c, col, fx, PokemonType.Electric);
        // The wave broken over it: foam lying round its feet and spray thrown up
        fx.Flat(FxShape.Swirl, c.ToFeet + Vector3.UnitY * c.ToSize * 0.04f, Vector3.UnitY, c.ToSize * 1.2f, Fx.Fade(water.Light, SetPiece(c.Age) * 0.9f), rotation: c.Age * 5f);
        Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.2f, 0.8f, 7, FxShape.Drop, water.Main, water.Light, 0.08f, 1.2f, 2.4f, FxBlend.Alpha, salt: 72);
    }

    private static void PulverizingPancake(in FxCtx c, FxColors col, FxList fx)
    {
        // A dark shadow growing under it, and a ring as it is flattened
        float h = c.ToSize;
        float grow = Fx.Envelope(c.Age, 0f, I, I + 0.2f, 0.9f);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * (0.4f + 0.9f * Math.Clamp(c.Age / I, 0f, 1f)), Fx.Fade(Rgb(20, 16, 24), grow * 0.85f), FxBlend.Alpha);
        ZCommon(c, col, fx, scale: 1.5f);
        if (!c.Cue.Landed) return;
        fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.25f, I + 0.55f));
        for (int i = 0; i < 2; i++)
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, I + i * 0.15f, 0.7f, h * 2.2f, Fx.Fade(i == 0 ? White : col.Light, 0.9f));
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.9f, 10, FxShape.Smoke, Rgb(230, 226, 214, 220), Rgb(190, 186, 176, 200), 0.32f, 0.9f, -0.2f, FxBlend.Alpha, salt: 62);
    }

    /// <summary>The colours of Eevee's eight evolutions, in their order.</summary>
    private static readonly PokemonType[] Eeveelutions =
        { PokemonType.Water, PokemonType.Electric, PokemonType.Fire, PokemonType.Psychic, PokemonType.Dark, PokemonType.Grass, PokemonType.Ice, PokemonType.Fairy };

    private static void ExtremeEvoboost(in FxCtx c, FxColors col, FxList fx)
    {
        // On the user: an aura in the colours of each of Eevee's evolutions, in turn
        fx.Land(I);
        float h = c.FromSize, w = c.FromHalfWidth;
        for (int j = 0; j < Eeveelutions.Length; j++)
        {
            var each = ColorsOf(Eeveelutions[j]);
            float k = (c.Age - j * 0.12f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            float fade = MathF.Sin(k * MathF.PI);
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (0.05f + 1.0f * k), Vector3.UnitY, w + h * (0.35f - 0.15f * k), Fx.Fade(each.Main, fade));
            fx.Sprite(FxShape.Glow, c.FromFeet + Vector3.UnitY * h * (0.1f + 1.0f * k), h * 0.3f, Fx.Fade(each.Light, fade * 0.6f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * MathF.Tau / 3f + j + c.Age * 4f;
                var p = c.FromFeet + new Vector3(MathF.Cos(a) * (w + h * 0.15f), h * (0.1f + 1.05f * k), MathF.Sin(a) * (w + h * 0.15f) * 0.6f);
                fx.Sprite(FxShape.Spark, p, h * 0.07f, Fx.Fade(each.Light, fade), depth: MathF.Sin(a) < 0f);
            }
        }
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.9f, h, 1.05f, White);
    }

    private static void GenesisSupernova(in FxCtx c, FxColors col, FxList fx)
    {
        // A sphere of psychic light swelling on it and bursting, over a pink field
        ZCommon(c, col, fx);
        if (!c.Cue.Landed) return;
        float h = c.ToSize, e = SetPiece(c.Age);
        float swell = Math.Clamp((c.Age - I) / 0.6f, 0f, 1f);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, h * 2.0f, Fx.Fade(col.Main, e * 0.5f));
        if (c.Age < 0.98f)
        {
            fx.Sprite(FxShape.Glow, c.To, h * (0.3f + 0.7f * swell), Fx.Fade(col.Light, 0.35f));
            fx.Sprite(FxShape.Ring, c.To, h * (0.35f + 0.75f * swell), Fx.Fade(col.Main, 0.9f), rotation: c.Age * 2f);
            fx.Sprite(FxShape.Swirl, c.To, h * (0.3f + 0.65f * swell), Fx.Fade(col.Light, 0.6f), rotation: -c.Age * 5f);
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.95f, 0.45f, h * 2.0f, col.Light);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, 0.95f, 0.45f, 16, FxShape.Spark, col.Light, col.Main, 0.1f, 1.4f, salt: 63);
        fx.ScreenFlash(col.Light, 0.25f * Fx.Envelope(c.Age, 0.94f, 0.97f, 1.0f, 1.2f));
    }

    private static readonly Color[] SevenColours =
        { Rgb(255, 90, 90), Rgb(255, 170, 70), Rgb(255, 236, 90), Rgb(110, 230, 110), Rgb(90, 200, 255), Rgb(120, 120, 255), Rgb(220, 120, 255) };

    private static void TenMillionVolts(in FxCtx c, FxColors col, FxList fx)
    {
        // Seven bolts of seven colours converging on it
        float h = c.FromSize;
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var start = c.From + new Vector3(MathF.Cos(a) * h * 0.6f, MathF.Sin(a) * h * 0.6f, 0f);
            Fx.Bolt(fx, c.Age, c.Seed + i * 17, start, c.Aim, 0.1f + i * 0.02f, 0.9f, h * 0.035f, SevenColours[i], White, 0);
        }
        ZCommon(c, col, fx, cross: false);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.1f, 0.8f, 14, FxShape.Spark, col.Main, col.Light, 0.1f, 1.1f, salt: 64);
    }

    private static void LightThatBurnsTheSky(in FxCtx c, FxColors col, FxList fx)
    {
        // A pillar of white-gold light falling on it from the sky, behind it so the body stands in it
        float h = c.ToSize;
        float pillar = Fx.Envelope(c.Age, I - 0.12f, I, I + 0.2f, 1.2f);
        if (pillar > 0f)
        {
            var bottom = c.ToFeet - Vector3.UnitZ * (c.ToHalfWidth * 0.6f);
            var top = bottom + Vector3.UnitY * h * 4f;
            fx.Ribbon(new[] { top, Vector3.Lerp(top, bottom, 0.5f), bottom }, h * 0.75f * pillar, Fx.Fade(Rgb(255, 236, 170), pillar * 0.45f), depth: true);
            fx.Ribbon(new[] { top, Vector3.Lerp(top, bottom, 0.5f), bottom }, h * 0.25f * pillar, Fx.Fade(White, pillar * 0.8f), depth: true);
        }
        ZCommon(c, col, fx, cross: false, scale: 1.4f);
        if (!c.Cue.Landed) return;
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I + 0.1f, 0.9f, 14, FxShape.Spark, Rgb(255, 236, 170), White, 0.1f, 1.2f, salt: 65);
        fx.ScreenFlash(Rgb(255, 246, 220), 0.18f * Fx.Envelope(c.Age, I - 0.02f, I + 0.02f, I + 0.05f, I + 0.25f));
    }

    private static void SearingSunrazeSmash(in FxCtx c, FxColors col, FxList fx)
    {
        // A sun disc behind the user, and a streak of gold into it
        float h = c.FromSize;
        float sun = Fx.Envelope(c.Age, 0f, 0.1f, 0.35f, 0.6f);
        var behind = c.From - Vector3.UnitZ * (c.FromHalfWidth + h * 0.2f) + Vector3.UnitY * h * 0.2f;
        fx.Sprite(FxShape.Glow, behind, h * 1.0f, Fx.Fade(Rgb(255, 196, 80), sun * 0.7f), depth: true);
        fx.Sprite(FxShape.Ring, behind, h * 0.7f, Fx.Fade(Rgb(255, 236, 160), sun), rotation: c.Age * 2f, depth: true);
        float k = (c.Age - 0.12f) / 0.23f;
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, k);
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.ScaleAt(k) * 0.12f, 9f, Fx.Fade(Rgb(255, 214, 110), 0.95f));
            fx.Sprite(FxShape.Glow, p, c.ScaleAt(k) * 0.4f, Fx.Fade(Rgb(255, 236, 160), 0.8f));
        }
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        TypePiece(c, col, fx, PokemonType.Steel);
        // The sun's heat left on it: a gold glow behind it and a slow ring of light
        float e = SetPiece(c.Age);
        var back = c.To - Vector3.UnitZ * (c.ToHalfWidth + c.ToSize * 0.2f);
        fx.Sprite(FxShape.Glow, back, c.ToSize * 0.9f, Fx.Fade(Rgb(255, 196, 80), e * 0.6f), depth: true);
        fx.Sprite(FxShape.Ring, back, c.ToSize * 0.75f, Fx.Fade(Rgb(255, 236, 160), e), rotation: c.Age * 2f, depth: true);
    }

    private static void MenacingMoonrazeMaelstrom(in FxCtx c, FxColors col, FxList fx)
    {
        // A violet moon over the user, and a beam of moonlight
        float h = c.FromSize;
        float moon = Fx.Envelope(c.Age, 0f, 0.1f, 0.6f, 0.9f);
        var over = c.FromFeet + Vector3.UnitY * h * 1.15f;
        fx.Sprite(FxShape.Orb, over, h * 0.22f, Fx.Fade(Rgb(96, 60, 160), moon * 0.95f), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, over, h * 0.24f, Fx.Fade(Rgb(214, 190, 255), moon));
        Fx.Beam(fx, c, 0.08f, 0.9f, 0.16f, Rgb(170, 120, 255), White, 6);
        ZCommon(c, col, fx, cross: false);
        if (c.Cue.Landed) TypePiece(c, col, fx, PokemonType.Ghost);
    }

    private static void LetsSnuggleForever(in FxCtx c, FxColors col, FxList fx)
    {
        // A cloud of dust round it with hearts and stars flying out
        ZCommon(c, col, fx);
        if (!c.Cue.Landed) return;
        float h = c.ToSize;
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 1.0f, 10, FxShape.Smoke, Rgb(236, 230, 220, 230), Rgb(210, 200, 196, 210), 0.3f, 0.5f, -0.1f, FxBlend.Alpha, salt: 66);
        for (int i = 0; i < 4; i++)
            Fx.Burst(fx, c.Seed + i, c.Age, c.To, h, I + 0.1f + i * 0.18f, 0.5f, 5, i % 2 == 0 ? FxShape.Heart : FxShape.Star,
                i % 2 == 0 ? col.Main : Rgb(255, 236, 140), col.Light, 0.09f, 1.1f, 0.5f, FxBlend.Alpha, spin: 3f, salt: 67 + i);
    }

    private static void SplinteredStormshards(in FxCtx c, FxColors col, FxList fx)
    {
        // Shards of rock spiralling up round it
        ZCommon(c, col, fx);
        if (!c.Cue.Landed) return;
        float h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(c.Age);
        for (int i = 0; i < 16; i++)
        {
            float u = (c.Age * 0.9f + i / 16f) % 1f;
            float a = i * 2.4f + c.Age * 7f;
            float r = (w + h * 0.15f) * (1.1f - 0.3f * u);
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * r, h * 1.25f * u, MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Shard, p, h * 0.14f, Fx.Fade(i % 2 == 0 ? col.Light : Rgb(140, 120, 100), e * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
        }
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.8f, 8, FxShape.Rock, col.Main, col.Dark, 0.1f, 1.1f, 2.2f, FxBlend.Alpha, spin: 4f, salt: 71);
    }

    private static void ClangorousSoulblaze(in FxCtx c, FxColors col, FxList fx)
    {
        // Rings of sound pounding it, and gold glints
        Fx.Waves(fx, c, 0f, I, 4, FxShape.Wave, col.Light, 0.5f, 0.25f);
        ZCommon(c, col, fx, cross: false);
        if (!c.Cue.Landed) return;
        float h = c.ToSize;
        for (int i = 0; i < 4; i++) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.1f + i * 0.18f, 0.4f, h * 1.5f, Fx.Fade(i % 2 == 0 ? col.Main : Rgb(255, 214, 110), 0.9f));
        TypePiece(c, col, fx, PokemonType.Dragon);
        for (int i = 0; i < 6; i++) Fx.Glint(fx, c.Age, c.To + Fx.Direction(c.Seed, i + 300) * h * 0.6f, h, I + 0.15f + i * 0.12f, Rgb(255, 226, 120), 0.3f);
    }
}
