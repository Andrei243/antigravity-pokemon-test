using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What drifts through each arena (plan 04 · G8): snow falling, sand blowing, light shafts and motes under the
/// trees, fireflies at night, dust in caves and rooms, glints on the sea, and in gyms and the League the matter of
/// their type (embers, bubbles, leaves, sparks, wisps). Particles loop in time, so nothing is simulated; GPU-free.
/// </summary>
internal static class ArenaFx
{
    private static float R(int i, int salt) => SoftCanvas.Rand(i, 0, salt);

    private static Color Rgba(int r, int g, int b, int a = 255) => new(r, g, b, a);

    /// <summary>A point in the box in front of the camera that loops: <paramref name="speed"/> world units per second.</summary>
    private static Vector3 Drift(int i, float time, Vector3 min, Vector3 max, Vector3 speed, int salt)
    {
        var size = max - min;
        var p = new Vector3(R(i, salt) * size.X, R(i, salt + 1) * size.Y, R(i, salt + 2) * size.Z) + speed * time;
        p = new Vector3(Wrap(p.X, size.X), Wrap(p.Y, size.Y), Wrap(p.Z, size.Z));
        return min + p;
    }

    private static float Wrap(float v, float span) => span <= 0f ? 0f : ((v % span) + span) % span;

    /// <summary>Fades a looping particle in and out near the ends of its fall or flight (0..1 along it).</summary>
    private static float EdgeFade(float u) => Math.Clamp(Math.Min(u, 1f - u) * 6f, 0f, 1f);

    public static void Emit(ArenaSpec spec, float time, bool night, FxList fx)
    {
        switch (spec.Kind)
        {
            case BattleArena.Snow:
                Snowfall(fx, time, 140, 1f);
                break;
            case BattleArena.Sand:
                BlowingSand(fx, time);
                break;
            case BattleArena.Forest:
                if (night) Fireflies(fx, time, 26);
                else
                {
                    LightShafts(fx, time);
                    Motes(fx, time, 30, Rgba(255, 246, 200, 150), 0.05f, new Vector3(0.15f, 0.08f, 0f));
                }
                break;
            case BattleArena.Grass:
                if (night) Fireflies(fx, time, 14);
                break;
            case BattleArena.Water:
                Glints(fx, time, night);
                break;
            case BattleArena.Cave:
                Motes(fx, time, 34, Rgba(255, 214, 160, 110), 0.045f, new Vector3(0.05f, 0.1f, 0.02f));
                break;
            case BattleArena.Indoors:
                Motes(fx, time, 22, Rgba(255, 248, 220, 90), 0.04f, new Vector3(0.04f, 0.05f, 0f));
                break;
            case BattleArena.Gym:
            case BattleArena.League:
                HallMatter(fx, time, spec.Theme, spec.Kind == BattleArena.League);
                break;
        }
    }

    private static readonly Vector3 NearMin = new(-22f, 0f, -14f), NearMax = new(16f, 10f, 22f);

    private static void Snowfall(FxList fx, float time, int count, float strength)
    {
        for (int i = 0; i < count; i++)
        {
            var speed = new Vector3(0.35f, -1.1f - R(i, 3) * 0.6f, 0.1f);
            var p = Drift(i, time, NearMin, NearMax, speed, 10);
            // A little sway as it falls
            p.X += MathF.Sin(time * 1.3f + i) * 0.25f;
            float u = (p.Y - NearMin.Y) / (NearMax.Y - NearMin.Y);
            fx.Sprite(FxShape.Snowflake, p, 0.07f + R(i, 4) * 0.06f, Rgba(255, 255, 255, (int)(230 * EdgeFade(u) * strength)), FxBlend.Alpha, time * (R(i, 5) - 0.5f) * 3f, depth: true);
        }
    }

    private static void BlowingSand(FxList fx, float time)
    {
        var min = new Vector3(-26f, 0f, -12f);
        var max = new Vector3(18f, 3.5f, 22f);
        for (int i = 0; i < 110; i++)
        {
            var speed = new Vector3(6f + R(i, 3) * 5f, 0.2f * MathF.Sin(i), 0.4f);
            var p = Drift(i, time, min, max, speed, 20);
            p.Y += MathF.Sin(time * 3f + i) * 0.15f;
            float u = (p.X - min.X) / (max.X - min.X);
            var color = Rgba(232, 206, 158, (int)(170 * EdgeFade(u)));
            fx.Streak(FxShape.Streak, p, Vector3.UnitX, 0.03f + R(i, 4) * 0.03f, 6f + R(i, 5) * 6f, color, FxBlend.Alpha, depth: true);
        }
        for (int i = 0; i < 6; i++)
        {
            var p = Drift(i, time, min, max, new Vector3(4f, 0.1f, 0f), 30);
            float u = (p.X - min.X) / (max.X - min.X);
            fx.Sprite(FxShape.Smoke, p + Vector3.UnitY, 1.4f + R(i, 6), Rgba(236, 214, 170, (int)(70 * EdgeFade(u))), FxBlend.Alpha, R(i, 7) * 6f, depth: true);
        }
    }

    /// <summary>Soft rays of sunlight slanting down through the canopy.</summary>
    private static void LightShafts(FxList fx, float time)
    {
        (float X, float Z, float W)[] shafts = { (-9f, -6f, 1.6f), (4f, -9f, 2.2f), (-2f, 3f, 1.2f), (10f, -2f, 1.4f) };
        var along = Vector3.Normalize(new Vector3(0.45f, -1f, 0.2f));
        for (int i = 0; i < shafts.Length; i++)
        {
            var (x, z, w) = shafts[i];
            float breathe = 0.75f + 0.25f * MathF.Sin(time * 0.4f + i * 1.7f);
            var at = new Vector3(x, 6f, z);
            fx.Streak(FxShape.Glow, at, along, w, 4.5f, Rgba(255, 244, 196, (int)(46 * breathe)), depth: true);
        }
    }

    private static void Motes(FxList fx, float time, int count, Color color, float size, Vector3 speed)
    {
        for (int i = 0; i < count; i++)
        {
            var p = Drift(i, time, NearMin, NearMax with { Y = 7f }, speed * (0.5f + R(i, 3)), 40);
            p += new Vector3(MathF.Sin(time * 0.7f + i) * 0.3f, MathF.Sin(time * 0.5f + i * 2f) * 0.2f, 0);
            float twinkle = 0.6f + 0.4f * MathF.Sin(time * 2f + i * 3f);
            fx.Sprite(FxShape.Glow, p, size * (0.7f + R(i, 4)), Fx.Fade(color, twinkle), depth: true);
        }
    }

    private static void Fireflies(FxList fx, float time, int count)
    {
        var min = new Vector3(-20f, 0.3f, -12f);
        var max = new Vector3(14f, 3.5f, 20f);
        for (int i = 0; i < count; i++)
        {
            var home = min + new Vector3(R(i, 1) * (max.X - min.X), R(i, 2) * (max.Y - min.Y), R(i, 3) * (max.Z - min.Z));
            var p = home + new Vector3(MathF.Sin(time * 0.6f + i) * 0.8f, MathF.Sin(time * 0.9f + i * 1.3f) * 0.4f, MathF.Cos(time * 0.5f + i) * 0.6f);
            // Each one blinks on and off on its own rhythm
            float blink = MathF.Max(0f, MathF.Sin(time * (1.2f + R(i, 4)) + i * 2.1f));
            fx.Sprite(FxShape.Glow, p, 0.16f, Rgba(220, 255, 140, (int)(230 * blink)), depth: true);
            fx.Sprite(FxShape.Glow, p, 0.05f, Rgba(255, 255, 230, (int)(255 * blink)), depth: true);
        }
    }

    /// <summary>Sun (or moon) glinting off the sea.</summary>
    private static void Glints(FxList fx, float time, bool night)
    {
        for (int i = 0; i < 40; i++)
        {
            var p = new Vector3(-22f + R(i, 1) * 40f, 0.05f, -20f + R(i, 2) * 40f);
            float k = (time * (0.8f + R(i, 3)) + R(i, 4)) % 1f;
            float pop = MathF.Max(0f, MathF.Sin(k * MathF.PI * 2f)) * (k < 0.5f ? 1f : 0f);
            fx.Sprite(FxShape.Spark, p, 0.18f + R(i, 5) * 0.12f, Rgba(255, 255, night ? 230 : 250, (int)(220 * pop * (night ? 0.5f : 1f))), depth: true, rotation: R(i, 6));
        }
    }

    /// <summary>A gym's or League room's type in the air.</summary>
    private static void HallMatter(FxList fx, float time, PokemonType? theme, bool league)
    {
        var colors = theme.HasValue ? MoveFx.ColorsOf(theme.Value) : new FxColors(Rgba(255, 230, 160), Rgba(255, 250, 230), Rgba(220, 180, 90));
        int count = league ? 44 : 32;
        switch (theme)
        {
            case PokemonType.Fire:
            case PokemonType.Dragon:
                Rising(fx, time, count, FxShape.Ember, colors.Main, colors.Light, 0.07f, 1.2f, FxBlend.Add);
                break;
            case PokemonType.Water:
                Rising(fx, time, count, FxShape.Bubble, colors.Light, Rgba(255, 255, 255), 0.09f, 0.8f, FxBlend.Alpha);
                break;
            case PokemonType.Grass:
            case PokemonType.Bug:
                for (int i = 0; i < count; i++)
                {
                    var p = Drift(i, time, NearMin, NearMax, new Vector3(0.4f, -0.6f, 0.1f), 50);
                    p.X += MathF.Sin(time * 1.1f + i) * 0.4f;
                    float u = p.Y / NearMax.Y;
                    fx.Sprite(theme == PokemonType.Bug ? FxShape.Spore : FxShape.Leaf, p, 0.1f, Fx.Fade(Fx.Mix(colors.Main, colors.Light, R(i, 5)), EdgeFade(u)),
                        theme == PokemonType.Bug ? FxBlend.Add : FxBlend.Alpha, time * 2f + i, depth: true);
                }
                break;
            case PokemonType.Electric:
                for (int i = 0; i < count; i++)
                {
                    var p = Drift(i, time * 0.2f, NearMin, NearMax with { Y = 6f }, Vector3.Zero, 60);
                    float k = (time * (1.5f + R(i, 3)) + R(i, 4)) % 1f;
                    fx.Sprite(FxShape.Spark, p, 0.16f, Fx.Fade(colors.Light, k < 0.25f ? MathF.Sin(k * 4f * MathF.PI) : 0f), depth: true, rotation: i);
                }
                break;
            case PokemonType.Ice:
                Snowfall(fx, time, count * 2, 0.8f);
                break;
            case PokemonType.Ghost:
            case PokemonType.Dark:
            case PokemonType.Psychic:
                Rising(fx, time, count / 2, FxShape.Wisp, colors.Main, colors.Light, 0.14f, 0.5f, FxBlend.Add);
                break;
            default:
                Motes(fx, time, count, Fx.Fade(colors.Light, 0.7f), 0.05f, new Vector3(0.05f, 0.12f, 0f));
                break;
        }
    }

    private static void Rising(FxList fx, float time, int count, FxShape shape, Color a, Color b, float size, float speed, FxBlend blend)
    {
        for (int i = 0; i < count; i++)
        {
            var p = Drift(i, time, NearMin, NearMax, new Vector3(0.1f, speed * (0.6f + R(i, 3) * 0.8f), 0f), 70);
            p.X += MathF.Sin(time * 1.4f + i) * 0.2f;
            float u = p.Y / NearMax.Y;
            fx.Sprite(shape, p, size * (0.7f + R(i, 4) * 0.6f), Fx.Fade(Fx.Mix(a, b, R(i, 5)), EdgeFade(u)), blend, time + i, depth: true);
        }
    }
}
