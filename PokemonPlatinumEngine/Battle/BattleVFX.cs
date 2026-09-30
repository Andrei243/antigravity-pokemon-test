using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Battle;

public enum VfxType
{
    None,
    TackleBump,
    Ember,
    WaterGun,
    RazorLeaf,
    Thunderbolt,
    StatBoost,
    StatDrop,
    PokeballThrow,
    HealSparkle
}

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float Size;
    public float Life;
    public float MaxLife;
    public float Spin;
    public float Delay;
}

/// <summary>
/// 2D move effects drawn over the battle field: projectiles fly from the attacker to the target over the effect's
/// duration, then an impact burst plays where they land. Also plays the Poké Ball throw.
/// </summary>
public class BattleVFX
{
    private const float ImpactTime = 0.3f;

    private readonly List<Particle> particles = new();
    private VfxType activeType = VfxType.None;
    private float vfxTimer = 0f;
    private float vfxDuration = 0f;
    private float totalTime = 0f;
    private Vector2 sourcePos;
    private Vector2 targetPos;
    private readonly Random rng = new();

    // Pokeball throw specific: flight, then open in mid-air while the Pokémon is pulled in, a drop onto the
    // platform, one wobble per successful shake check, and finally a click or a break-out
    public const float BallFlightTime = 0.8f;
    private const float BallOpenTime = 0.55f;
    private const float BallDropTime = 0.25f;
    private const float BallWobbleTime = 0.4f;
    private const float BallEndTime = 0.4f;
    private Vector2 groundPos;
    private bool ballResting;
    private string ballName = "Poké Ball";

    public int BallShakes { get; private set; }

    public bool IsActive => activeType != VfxType.None && vfxTimer < totalTime;

    /// <summary>How long a throw with <paramref name="shakes"/> successful shake checks takes to play out.</summary>
    public static float BallThrowTime(int shakes) =>
        BallFlightTime + BallOpenTime + BallDropTime + BallWobbleTime * Math.Min(shakes, 3) + BallEndTime;

    /// <param name="duration">Flight time from <paramref name="source"/> to <paramref name="target"/>; the impact follows.</param>
    public void TriggerVfx(VfxType type, Vector2 source, Vector2 target, float duration = 0.6f)
    {
        ballResting = false;
        activeType = type;
        sourcePos = source;
        targetPos = target;
        vfxDuration = duration;
        totalTime = duration + ImpactTime;
        vfxTimer = 0f;
        particles.Clear();

        SpawnInitialParticles();
    }

    /// <param name="ball">Item name of the ball, for its colours.</param>
    /// <param name="target">Where the ball opens (the Pokémon's body).</param>
    /// <param name="ground">Where it lands and wobbles (the Pokémon's feet).</param>
    /// <param name="shakes">Successful shake checks; 4 means caught.</param>
    public void TriggerPokeballThrow(string ball, Vector2 source, Vector2 target, Vector2 ground, int shakes)
    {
        activeType = VfxType.PokeballThrow;
        sourcePos = source;
        targetPos = target;
        groundPos = ground - new Vector2(0, 18);
        ballName = ball;
        vfxDuration = totalTime = BallThrowTime(shakes);
        vfxTimer = 0f;
        BallShakes = shakes;
        ballResting = false;
        particles.Clear();
    }

    private void SpawnInitialParticles()
    {
        int count = activeType switch
        {
            VfxType.Ember => 26,
            VfxType.WaterGun => 30,
            VfxType.RazorLeaf => 6,
            VfxType.Thunderbolt => 16,
            VfxType.HealSparkle => 20,
            _ => 0
        };

        for (int i = 0; i < count; i++)
        {
            Color c = activeType switch
            {
                VfxType.Ember => rng.Next(3) == 0 ? new Color(255, 232, 48, 255) : new Color(248, 120, 32, 255),
                VfxType.WaterGun => rng.Next(2) == 0 ? new Color(72, 144, 240, 255) : new Color(150, 206, 255, 255),
                VfxType.RazorLeaf => rng.Next(2) == 0 ? new Color(88, 190, 64, 255) : new Color(140, 224, 96, 255),
                VfxType.Thunderbolt => rng.Next(2) == 0 ? new Color(255, 240, 48, 255) : Color.White,
                VfxType.HealSparkle => new Color(96, 240, 128, 255),
                _ => Color.White
            };

            // Projectiles leave one after another so they arrive as a stream
            float delay = activeType == VfxType.Thunderbolt ? 0f : i / (float)count * vfxDuration * 0.45f;
            float flight = vfxDuration - delay;
            var spread = new Vector2(rng.Next(-40, 41), rng.Next(-40, 41));
            particles.Add(new Particle
            {
                Position = activeType == VfxType.Thunderbolt ? targetPos : sourcePos + new Vector2(rng.Next(-10, 11), rng.Next(-10, 11)),
                Velocity = activeType == VfxType.Thunderbolt
                    ? new Vector2(rng.Next(-260, 261), rng.Next(-260, 120))
                    : (targetPos - sourcePos) / flight + spread,
                Color = c,
                Size = activeType == VfxType.RazorLeaf ? 16f : rng.Next(6, 14),
                Life = 0f,
                MaxLife = activeType == VfxType.Thunderbolt ? 0.35f : flight,
                Spin = (float)rng.NextDouble() * MathF.Tau,
                Delay = activeType == VfxType.Thunderbolt ? vfxDuration * 0.6f : delay
            });
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;

        vfxTimer += dt;

        // A caught Pokémon's ball stays on the platform until the battle ends
        if (activeType == VfxType.PokeballThrow && vfxTimer >= vfxDuration && BallShakes >= 4) ballResting = true;

        // Update particles
        foreach (var p in particles)
        {
            if (p.Delay > 0f)
            {
                p.Delay -= dt;
                continue;
            }
            p.Life += dt;
            p.Position += p.Velocity * dt;
        }
        particles.RemoveAll(p => p.Life >= p.MaxLife);
    }

    public void Draw()
    {
        if (ballResting)
        {
            DrawBall(groundPos, 0f, 0f);
            return;
        }
        if (!IsActive) return;

        if (activeType == VfxType.PokeballThrow)
        {
            DrawPokeballThrowVfx();
            return;
        }

        if (activeType == VfxType.Thunderbolt) DrawLightning();

        foreach (var p in particles)
        {
            if (p.Delay > 0f) continue;
            float life = p.Life / p.MaxLife;
            DrawParticle(p, life);
        }

        // Impact burst where the move lands
        float impact = (vfxTimer - vfxDuration * 0.85f) / ImpactTime;
        if (impact >= 0f && impact <= 1f) DrawImpact(impact);
    }

    private void DrawParticle(Particle p, float life)
    {
        switch (activeType)
        {
            case VfxType.Ember:
            {
                // Flickering flames with a glow
                float flicker = 0.8f + 0.2f * MathF.Sin(vfxTimer * 40f + p.Spin * 5f);
                float size = p.Size * (1f - life * 0.4f) * flicker;
                Raylib.BeginBlendMode(BlendMode.Additive);
                Raylib.DrawCircleV(p.Position, size * 2.2f, WithAlpha(p.Color, 70));
                Raylib.EndBlendMode();
                Raylib.DrawCircleV(p.Position, size, p.Color);
                Raylib.DrawCircleV(p.Position - new Vector2(0, size * 0.3f), size * 0.45f, new Color(255, 248, 200, 255));
                break;
            }
            case VfxType.WaterGun:
            {
                // Bubbles with a highlight
                float size = p.Size * (0.8f + 0.2f * MathF.Sin(vfxTimer * 20f + p.Spin));
                Raylib.DrawCircleV(p.Position, size, WithAlpha(p.Color, 220));
                Raylib.DrawCircleLinesV(p.Position, size, new Color(230, 244, 255, 255));
                Raylib.DrawCircleV(p.Position + new Vector2(-size * 0.35f, -size * 0.35f), size * 0.28f, Color.White);
                break;
            }
            case VfxType.RazorLeaf:
            {
                // Spinning leaves
                float a = p.Spin + vfxTimer * 16f;
                var along = new Vector2(MathF.Cos(a), MathF.Sin(a)) * p.Size;
                var across = new Vector2(-along.Y, along.X) * 0.38f;
                var tip = p.Position + along;
                var tail = p.Position - along;
                Tri(tip, p.Position + across, tail, p.Color);
                Tri(tip, tail, p.Position - across, p.Color);
                Raylib.DrawLineEx(tail, tip, 2f, new Color(44, 110, 40, 255));
                break;
            }
            case VfxType.Thunderbolt:
            {
                // Sparks thrown off the strike
                var end = p.Position - Vector2.Normalize(p.Velocity + new Vector2(0.01f, 0.01f)) * 14f;
                Raylib.DrawLineEx(p.Position, end, 4f, WithAlpha(p.Color, (int)(255 * (1f - life))));
                break;
            }
            default:
            {
                float alpha = 1f - life;
                Raylib.DrawCircleV(p.Position, p.Size, WithAlpha(p.Color, (int)(255 * alpha)));
                break;
            }
        }
    }

    /// <summary>A jagged bolt from the sky onto the target, flickering as it strikes.</summary>
    private void DrawLightning()
    {
        float f = vfxTimer / vfxDuration;
        if (f > 1f || (int)(vfxTimer * 30f) % 3 == 2) return;

        var bolt = new Random((int)(vfxTimer * 20f));
        var top = new Vector2(targetPos.X + 60f, -20f);
        var from = top;
        const int steps = 9;
        for (int i = 1; i <= steps; i++)
        {
            var to = Vector2.Lerp(top, targetPos, i / (float)steps);
            if (i < steps) to.X += bolt.Next(-36, 37);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawLineEx(from, to, 22f, new Color(255, 236, 90, 90));
            Raylib.EndBlendMode();
            Raylib.DrawLineEx(from, to, 8f, new Color(255, 244, 120, 255));
            Raylib.DrawLineEx(from, to, 3f, Color.White);
            from = to;
        }
    }

    /// <summary>A ring and a burst of spikes at the target, coloured by the move's type.</summary>
    private void DrawImpact(float f)
    {
        var color = activeType switch
        {
            VfxType.Ember => new Color(255, 170, 60, 255),
            VfxType.WaterGun => new Color(150, 206, 255, 255),
            VfxType.RazorLeaf => new Color(170, 240, 120, 255),
            VfxType.Thunderbolt => new Color(255, 240, 90, 255),
            _ => new Color(255, 255, 255, 255)
        };
        int alpha = (int)(230 * (1f - f));
        float radius = 30f + 70f * MathF.Sqrt(f);
        Raylib.DrawRing(targetPos, radius * 0.8f, radius, 0, 360, 40, WithAlpha(color, alpha));
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            float inner = radius * 0.35f, outer = radius * (i % 2 == 0 ? 1.25f : 0.95f);
            var side = new Vector2(-dir.Y, dir.X) * 7f * (1f - f);
            Tri(targetPos + dir * outer, targetPos + dir * inner - side, targetPos + dir * inner + side, WithAlpha(color, alpha));
        }
        if (f < 0.35f) Raylib.DrawCircleV(targetPos, 34f * (1f - f / 0.35f), WithAlpha(Color.White, 220));
    }

    private static Color WithAlpha(Color c, int alpha) => new(c.R, c.G, c.B, (byte)Math.Clamp(alpha, 0, 255));

    /// <summary>A filled triangle whatever its winding (raylib culls one of them).</summary>
    private static void Tri(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        Raylib.DrawTriangle(a, b, c, color);
        Raylib.DrawTriangle(a, c, b, color);
    }

    private void DrawPokeballThrowVfx()
    {
        float t = vfxTimer;

        if (t < BallFlightTime)
        {
            // Arcs over from the player's side, spinning end over end
            float f = t / BallFlightTime;
            var pos = Vector2.Lerp(sourcePos, targetPos, f) - new Vector2(0, MathF.Sin(f * MathF.PI) * 220f);
            DrawBall(pos, f * 900f, 0f);
            return;
        }

        if ((t -= BallFlightTime) < BallOpenTime)
        {
            // Pops open in mid-air and pulls the Pokémon in with a burst of red light
            float f = t / BallOpenTime;
            Raylib.DrawCircleV(targetPos, 40f + 70f * f, new Color(255, 80, 80, (int)(150 * (1f - f))));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + f;
                var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                Raylib.DrawLineEx(targetPos + dir * 20f, targetPos + dir * (60f + 90f * f), 5f, new Color(255, 120, 110, (int)(220 * (1f - f))));
            }
            DrawBall(targetPos, 0f, 1f - f);
            return;
        }

        if ((t -= BallOpenTime) < BallDropTime)
        {
            // Drops onto the platform with a little bounce
            float f = t / BallDropTime;
            float fall = f < 0.7f ? (f / 0.7f) * (f / 0.7f) : 1f - MathF.Sin((f - 0.7f) / 0.3f * MathF.PI) * 0.06f;
            DrawBall(Vector2.Lerp(targetPos, groundPos, fall), 0f, 0f);
            return;
        }

        t -= BallDropTime;
        int wobbles = Math.Min(BallShakes, 3);
        int index = (int)(t / BallWobbleTime);
        if (index < wobbles)
        {
            // One wobble per successful shake check, rocking on its base
            float f = t / BallWobbleTime - index;
            DrawBall(groundPos, MathF.Sin(f / 0.75f * MathF.Tau) * 24f * (f < 0.75f ? 1f : 0f), 0f);
            return;
        }

        float end = Math.Clamp((t - wobbles * BallWobbleTime) / BallEndTime, 0f, 1f);
        if (BallShakes >= 4)
        {
            // Click! Stars pop out of the ball
            DrawBall(groundPos, 0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                float a = -MathF.PI / 2f + (i - 1) * 0.7f;
                var p = groundPos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (20f + 50f * end);
                DrawStar(p, 10f * (1f - end * 0.6f), new Color(255, 236, 110, (int)(255 * (1f - end))));
            }
        }
        else
        {
            // Bursts open in a flash of light
            Raylib.DrawCircleV(groundPos, 30f + 90f * end, new Color(255, 255, 255, (int)(200 * (1f - end))));
            if (end < 0.3f) DrawBall(groundPos, 0f, end / 0.3f);
        }
    }

    /// <summary>The ball, rotated about its base; <paramref name="flash"/> 0..1 washes it white (opening).</summary>
    private void DrawBall(Vector2 center, float angle, float flash)
    {
        var tex = PixelArtGenerator.GetBallTexture(ballName);
        const float scale = 1.5f;
        float w = tex.Width * scale, h = tex.Height * scale;
        var src = new Rectangle(0, 0, tex.Width, tex.Height);
        var dest = new Rectangle(MathF.Round(center.X), MathF.Round(center.Y + h / 2f), w, h);
        var origin = new Vector2(w / 2f, h);
        Raylib.DrawTexturePro(tex, src, dest, origin, angle, Color.White);
        if (flash > 0f)
        {
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawTexturePro(tex, src, dest, origin, angle, new Color(255, 255, 255, (int)(255 * Math.Clamp(flash, 0f, 1f))));
            Raylib.EndBlendMode();
        }
    }

    private static void DrawStar(Vector2 c, float r, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            float a0 = -MathF.PI / 2f + i * MathF.Tau / 5f;
            float a1 = a0 + MathF.Tau / 10f, am = a0 - MathF.Tau / 10f;
            var tip = c + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * r;
            var left = c + new Vector2(MathF.Cos(am), MathF.Sin(am)) * r * 0.45f;
            var right = c + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * r * 0.45f;
            Tri(tip, left, right, color);
            Tri(c, right, left, color);
        }
    }
}
