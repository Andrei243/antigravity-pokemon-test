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
}

public class BattleVFX
{
    private readonly List<Particle> particles = new();
    private VfxType activeType = VfxType.None;
    private float vfxTimer = 0f;
    private float vfxDuration = 0f;
    private Vector2 sourcePos;
    private Vector2 targetPos;
    private readonly Random rng = new();

    // Pokeball throw specific
    public int BallShakes { get; private set; }
    public int CurrentShakeIndex { get; private set; }
    public bool BallIsOpen { get; private set; }

    public bool IsActive => activeType != VfxType.None && vfxTimer < vfxDuration;

    public void TriggerVfx(VfxType type, Vector2 source, Vector2 target, float duration = 0.6f)
    {
        activeType = type;
        sourcePos = source;
        targetPos = target;
        vfxDuration = duration;
        vfxTimer = 0f;
        particles.Clear();

        SpawnInitialParticles();
    }

    public void TriggerPokeballThrow(Vector2 source, Vector2 target, int shakes, float duration = 2.5f)
    {
        activeType = VfxType.PokeballThrow;
        sourcePos = source;
        targetPos = target;
        vfxDuration = duration;
        vfxTimer = 0f;
        BallShakes = shakes;
        CurrentShakeIndex = 0;
        BallIsOpen = false;
        particles.Clear();
    }

    private void SpawnInitialParticles()
    {
        int count = activeType switch
        {
            VfxType.Ember => 24,
            VfxType.WaterGun => 30,
            VfxType.RazorLeaf => 18,
            VfxType.Thunderbolt => 25,
            VfxType.HealSparkle => 20,
            _ => 10
        };

        for (int i = 0; i < count; i++)
        {
            Color c = activeType switch
            {
                VfxType.Ember => rng.Next(2) == 0 ? new Color(248, 120, 32, 255) : new Color(255, 232, 48, 255),
                VfxType.WaterGun => rng.Next(2) == 0 ? new Color(72, 144, 240, 255) : new Color(184, 224, 255, 255),
                VfxType.RazorLeaf => rng.Next(2) == 0 ? new Color(88, 208, 64, 255) : new Color(160, 240, 112, 255),
                VfxType.Thunderbolt => rng.Next(2) == 0 ? new Color(255, 240, 48, 255) : Color.White,
                VfxType.HealSparkle => new Color(96, 240, 128, 255),
                _ => Color.White
            };

            particles.Add(new Particle
            {
                Position = sourcePos + new Vector2(rng.Next(-10, 10), rng.Next(-10, 10)),
                Velocity = new Vector2((targetPos.X - sourcePos.X) / vfxDuration + rng.Next(-40, 40), (targetPos.Y - sourcePos.Y) / vfxDuration + rng.Next(-40, 40)),
                Color = c,
                Size = (float)rng.Next(6, 14),
                Life = 0f,
                MaxLife = vfxDuration
            });
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;

        vfxTimer += dt;

        // Update particles
        foreach (var p in particles)
        {
            p.Life += dt;
            p.Position += p.Velocity * dt;
        }
        particles.RemoveAll(p => p.Life >= p.MaxLife);
    }

    public void Draw()
    {
        if (!IsActive) return;

        float progress = Math.Clamp(vfxTimer / vfxDuration, 0f, 1f);

        if (activeType == VfxType.PokeballThrow)
        {
            DrawPokeballThrowVfx(progress);
        }
        else if (activeType == VfxType.TackleBump)
        {
            // Impact flash at target
            Raylib.DrawCircle((int)targetPos.X, (int)targetPos.Y, 45f * (1f - progress), new Color(255, 255, 255, 200));
        }
        else
        {
            // Draw particles
            foreach (var p in particles)
            {
                float alpha = 1f - (p.Life / p.MaxLife);
                Color c = new(p.Color.R, p.Color.G, p.Color.B, (byte)(255 * alpha));
                Raylib.DrawCircle((int)p.Position.X, (int)p.Position.Y, p.Size, c);
            }
        }
    }

    private void DrawPokeballThrowVfx(float progress)
    {
        var ballTex = PixelArtGenerator.GetBallTexture("Poké Ball");

        if (progress < 0.4f)
        {
            // Flight Arc
            float arcProg = progress / 0.4f;
            float bx = sourcePos.X + (targetPos.X - sourcePos.X) * arcProg;
            float by = sourcePos.Y + (targetPos.Y - sourcePos.Y) * arcProg - MathF.Sin(arcProg * MathF.PI) * 180f;
            Raylib.DrawTextureEx(ballTex, new Vector2(bx - 24, by - 24), 0f, 1.5f, Color.White);
        }
        else
        {
            // Landed at target and shaking
            float shakeTime = (progress - 0.4f) / 0.6f;
            int shakeIndex = (int)(shakeTime * 4); // 0, 1, 2, 3

            float shakeOffset = 0f;
            if (shakeIndex < BallShakes)
            {
                shakeOffset = MathF.Sin(shakeTime * 20f) * 12f;
            }

            Raylib.DrawTextureEx(ballTex, new Vector2(targetPos.X - 24 + shakeOffset, targetPos.Y - 24), 0f, 1.5f, Color.White);

            // Red capture beam burst
            if (progress > 0.4f && progress < 0.6f)
            {
                Raylib.DrawCircle((int)targetPos.X, (int)targetPos.Y, 50f, new Color(240, 64, 64, 120));
            }
        }
    }
}
