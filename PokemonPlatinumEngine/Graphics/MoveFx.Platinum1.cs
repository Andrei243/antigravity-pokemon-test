using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y6: the first chapter's moves (Twinleaf Town to the Oreburgh Gym, in MoveSightings' order), each a look of
// its own. The beats are the original's, read from each move's animation script (what comes first, when it lands, who
// shakes, whether the background darkens); the shapes, colours and timings are our own. Every recipe lands at
// ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads.
internal static partial class MoveFx
{
    /// <summary>
    /// Most quads and ribbons any move's look may draw at any age (plan 25 · Y6): the heaviest of G8's 146, Surf's wave
    /// at its height on a critical, super-effective hit, measured when this batch was written. A new look that needs
    /// more is cut down, not this raised: it is a share of the frame's 8 ms.
    /// </summary>
    internal const int MaxQuads = 124;

    private static Dictionary<string, FxRecipe> Platinum1() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Growl"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 4, FxShape.Wave, true, Rgb(255, 226, 190)),
        ["Leer"] = LeerY6,
        ["Pound"] = Pound,
        ["Scratch"] = (in FxCtx c, FxColors col, FxList fx) => { Rake(c, fx, 3, 0f, I - 0.12f); Impact(c, col, fx, 0.9f); },
        ["Fury Swipes"] = (in FxCtx c, FxColors col, FxList fx) => { Rake(c, fx, 3, 0.5f, I - 0.14f); Rake(c, fx, 3, -0.5f, I - 0.04f); Impact(c, col, fx, 0.85f); },
        ["Tackle"] = Tackle,
        ["Withdraw"] = Withdraw,
        ["Bide"] = Bide,
        ["Return"] = Return,
        ["Quick Attack"] = QuickAttackY6,
        ["Absorb"] = (in FxCtx c, FxColors col, FxList fx) => Drain(c, col, fx, 10),
        ["Bubble"] = (in FxCtx c, FxColors col, FxList fx) => Bubbles(c, col, fx, 9),
        ["Ember"] = (in FxCtx c, FxColors col, FxList fx) => FlameStream(c, col, fx, 9, 0.15f, 0.42f),
        ["Fake Out"] = FakeOut,
        ["Poison Gas"] = PoisonGas,
        ["Screech"] = Screech,
        ["Splash"] = Splash,
        ["Growth"] = GrowthY6,
        ["Leech Life"] = LeechLife,
        ["Low Kick"] = LowKick,
        ["Supersonic"] = (in FxCtx c, FxColors col, FxList fx) => Sound(c, fx, 6, FxShape.Ring, false, Rgb(255, 250, 200)),
        ["Tail Whip"] = TailWhip,
        ["Teleport"] = Teleport,
        ["Water Sport"] = (in FxCtx c, FxColors col, FxList fx) => Sport(c, fx, ColorsOf(PokemonType.Water), FxShape.Drop),
        ["Mud Sport"] = (in FxCtx c, FxColors col, FxList fx) => Sport(c, fx, new FxColors(Rgb(150, 108, 66), Rgb(196, 160, 110), Rgb(100, 70, 44)), FxShape.Drop),
        ["Defense Curl"] = DefenseCurl,
        ["Rock Smash"] = RockSmash,
        ["Aerial Ace"] = AerialAce,
        ["Astonish"] = Astonish,
        ["Brick Break"] = BrickBreak,
        ["Counter"] = Counter,
        ["Earthquake"] = Earthquake,
        ["Flash"] = FlashY6,
        ["Focus Punch"] = FocusPunch,
        ["Force Palm"] = ForcePalm,
        ["Rock Polish"] = RockPolish,
        ["Rock Slide"] = RockSlide,
        ["Take Down"] = TakeDown,
        ["Water Gun"] = (in FxCtx c, FxColors col, FxList fx) => Water(c, col, fx, 26, 0.09f),
        ["Harden"] = Harden,
        ["Headbutt"] = Headbutt,
        ["Pursuit"] = Pursuit,
        ["Rock Throw"] = RockThrow,
        ["Stealth Rock"] = StealthRock,
        ["Focus Energy"] = FocusEnergy,
        // The two that go by the ground (plan 06 · R6 asked for Secret Power's): Y6 gives the cue its terrain
        ["Secret Power"] = SecretPower,
        ["Camouflage"] = Camouflage
    };

    /// <summary>The colours of the ground a battle is fought on, for the looks that go by it.</summary>
    internal static FxColors ColorsOfGround(Battle.Sim.BattleTerrain terrain) => terrain switch
    {
        Battle.Sim.BattleTerrain.Grass => ColorsOf(PokemonType.Grass),
        Battle.Sim.BattleTerrain.Sand => new(Rgb(232, 208, 150), Rgb(250, 236, 196), Rgb(196, 160, 100)),
        Battle.Sim.BattleTerrain.Water or Battle.Sim.BattleTerrain.Puddle => ColorsOf(PokemonType.Water),
        Battle.Sim.BattleTerrain.Mountain or Battle.Sim.BattleTerrain.Cave => ColorsOf(PokemonType.Rock),
        Battle.Sim.BattleTerrain.Snow or Battle.Sim.BattleTerrain.Ice => ColorsOf(PokemonType.Ice),
        Battle.Sim.BattleTerrain.GreatMarsh => new(Rgb(150, 108, 66), Rgb(196, 160, 110), Rgb(100, 70, 44)),
        Battle.Sim.BattleTerrain.Special => ColorsOf(PokemonType.Psychic),
        _ => ColorsOf(PokemonType.Normal)
    };

    /// <summary>
    /// Secret Power: our own look by the ground, as the move's effect goes by it: thrown leaves on grass, sand on
    /// sand, a spray of water, rocks in a cave or on a mountain, ice on snow, mud in the marsh, rings in a hall, and a
    /// plain blow of stars anywhere else.
    /// </summary>
    private static void SecretPower(in FxCtx c, FxColors col, FxList fx)
    {
        var ground = ColorsOfGround(c.Cue.Terrain);
        switch (c.Cue.Terrain)
        {
            case Battle.Sim.BattleTerrain.Grass:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.3f, 8, FxShape.Leaf, ground.Main, ground.Light, 0.11f, arc: 0.05f, spread: 0.5f, spin: 9f, blend: FxBlend.Alpha);
                break;
            case Battle.Sim.BattleTerrain.Sand:
            case Battle.Sim.BattleTerrain.GreatMarsh:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.24f, 24, FxShape.Dot, ground.Main, ground.Dark, 0.04f, arc: 0.04f, spread: 0.55f, blend: FxBlend.Alpha);
                break;
            case Battle.Sim.BattleTerrain.Water:
            case Battle.Sim.BattleTerrain.Puddle:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.28f, 16, FxShape.Drop, ground.Main, ground.Light, 0.08f, arc: 0.06f, spread: 0.2f, blend: FxBlend.Alpha, trail: 1.4f);
                break;
            case Battle.Sim.BattleTerrain.Mountain:
            case Battle.Sim.BattleTerrain.Cave:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.3f, 3, FxShape.Rock, ground.Light, ground.Main, 0.15f, arc: 0.22f, spread: 0.3f, spin: 6f, blend: FxBlend.Alpha);
                break;
            case Battle.Sim.BattleTerrain.Snow:
            case Battle.Sim.BattleTerrain.Ice:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.28f, 8, FxShape.Shard, ground.Light, ground.Main, 0.09f, spread: 0.4f, spin: 6f, blend: FxBlend.Alpha);
                break;
            case Battle.Sim.BattleTerrain.Special:
                Fx.Waves(fx, c, 0f, I + 0.05f, 3, FxShape.Ring, ground.Main, 0.4f, 0.25f);
                break;
            default:
                Fx.Stream(fx, c, 0f, I + 0.05f, 0.28f, 5, FxShape.Star, Rgb(255, 236, 140), White, 0.1f, arc: 0.06f, spread: 0.4f, spin: 7f, blend: FxBlend.Alpha);
                break;
        }
        Impact(c, new FxColors(ground.Main, ground.Light, ground.Dark), fx);
    }

    /// <summary>Camouflage: the user takes on the colours of the ground, a wash of them rising over it.</summary>
    private static void Camouflage(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var ground = ColorsOfGround(c.Cue.Terrain);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0f, 1.0f, ground.Main, ground.Light, 10);
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.8f, Fx.Fade(ground.Main, 0.4f * Fx.Envelope(c.Age, 0.2f, I, 0.7f, 1f)), FxBlend.Alpha);
    }

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A shape that slides in from <paramref name="from"/> (in the target's height) to the target, striking at <paramref name="at"/>, and fades after.</summary>
    private static void Strike(in FxCtx c, FxList fx, FxShape shape, Vector3 from, float at, float size, Color color, float rotation = 0f, float lead = 0.12f)
    {
        float h = c.ToSize;
        float k = (c.Age - (at - lead)) / lead;
        if (k < 0f || c.Cue.Missed && k > 1f) return;
        float after = Math.Max(0f, (c.Age - at) / 0.2f);
        if (after > 1f) return;
        var p = c.To + from * h * (1f - Fx.EaseOut(Math.Min(1f, k)));
        float grow = 1f + 0.25f * after;
        fx.Sprite(shape, p, h * size * grow, Fx.Fade(color, Math.Min(1f, k * 3f) * (1f - after)), FxBlend.Alpha, rotation);
    }

    /// <summary>Claw marks raked down across the target one after another, tilted by <paramref name="tilt"/>.</summary>
    private static void Rake(in FxCtx c, FxList fx, int count, float tilt, float at)
    {
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        for (int i = 0; i < count; i++)
        {
            float k = (c.Age - at - i * 0.03f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float grow = Fx.EaseOut(k * 3f);
            var along = new Vector3(MathF.Sin(tilt), -MathF.Cos(tilt), 0f);
            var p = c.To + new Vector3((i - (count - 1) * 0.5f) * h * 0.16f, h * 0.3f, 0f) + along * h * 0.6f * grow * 0.5f;
            fx.Streak(FxShape.Streak, p, along, h * 0.07f, 6f * grow, Fx.Fade(White, 1f - k));
            fx.Streak(FxShape.Streak, p, along, h * 0.13f, 6f * grow, Fx.Fade(Rgb(255, 200, 160), (1f - k) * 0.6f));
        }
    }

    /// <summary>Little stars circling over the target's head after a blow, dizzying it.</summary>
    private static void Dizzy(in FxCtx c, FxList fx, float t0, int count = 3)
    {
        if (!c.Cue.Landed) return;
        float env = Fx.Envelope(c.Age, t0, t0 + 0.1f, t0 + 0.55f, t0 + 0.75f);
        if (env <= 0f) return;
        float h = c.ToSize;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count + c.Age * 7f;
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.28f, h * 1.05f, MathF.Sin(a) * h * 0.12f);
            fx.Sprite(FxShape.Star, p, h * 0.08f, Fx.Fade(Rgb(255, 230, 110), env), FxBlend.Alpha, c.Age * 4f);
        }
    }

    /// <summary>Dust kicked up at someone's feet as they set off.</summary>
    private static void Dust(in FxCtx c, FxList fx, Vector3 feet, float h, float t0, int count = 6) =>
        Fx.Burst(fx, c.Seed, c.Age, feet + Vector3.UnitY * h * 0.08f, h, t0, 0.5f, count, FxShape.Smoke, Rgb(222, 206, 176, 210), Rgb(176, 160, 132, 200),
            0.12f, 0.45f, -0.2f, FxBlend.Alpha, salt: 21);

    // ------------------------------------------------------------------ the recipes

    /// <summary>Leer: eyes narrow over the user twice, glinting, and red rings roll over to the foe.</summary>
    private static void LeerY6(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var head = c.FromFeet + Vector3.UnitY * c.FromSize * 0.95f;
        foreach (float t0 in new[] { 0f, 0.4f })
        {
            float k = (c.Age - t0) / 0.4f;
            if (k < 0f || k > 1f) continue;
            float s = MathF.Sin(k * MathF.PI);
            fx.Sprite(FxShape.Eye, head + Vector3.UnitY * c.FromSize * 0.12f, c.FromSize * 0.2f * (0.7f + 0.3f * s), Fx.Fade(Rgb(255, 110, 100), s), FxBlend.Alpha);
            Fx.Glint(fx, c.Age, head + Vector3.UnitY * c.FromSize * 0.12f, c.FromSize, t0 + 0.1f, Rgb(255, 160, 150), 0.35f);
        }
        Fx.Waves(fx, c, 0.15f, 0.9f, 3, FxShape.Ring, Rgb(255, 110, 110, 190), 0.4f, 0.25f);
    }

    /// <summary>Pound: an open hand swings in from the side and slaps the target.</summary>
    private static void Pound(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(-0.8f, 0.3f, 0f), I, 0.32f, Rgb(255, 236, 214), -0.4f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Tackle: dust thrown up as the user runs in, a ring where its body meets the target's.</summary>
    private static void Tackle(in FxCtx c, FxColors col, FxList fx)
    {
        Dust(c, fx, c.FromFeet, c.FromSize, 0.05f);
        SpeedLines(c, fx, 0.08f, 0.34f, 4, 0.45f);
        if (c.Cue.Landed) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.3f, c.ToSize * 0.8f, Fx.Fade(White, 0.8f));
        Impact(c, col, fx);
    }

    /// <summary>Withdraw: rings of shell close in round the user, and its shell glints.</summary>
    private static void Withdraw(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.12f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (0.25f + 0.25f * i), Vector3.UnitY, h * (0.9f - 0.45f * Fx.EaseOut(k)),
                Fx.Fade(Fx.Mix(col.Main, col.Light, i / 2f), MathF.Sin(k * MathF.PI)), depth: true);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.7f, Fx.Fade(col.Light, 0.35f * Fx.Envelope(c.Age, 0.2f, 0.4f, 0.6f, 0.9f)));
        Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.2f, h * 0.15f, 0f), h, 0.4f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.From - new Vector3(h * 0.2f, h * 0.1f, 0f), h, 0.55f, White, 0.3f);
    }

    /// <summary>Bide: a red glow beats round the user as it stores the blows, and pushes out a ring each beat.</summary>
    private static void Bide(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        float beat = 0.5f + 0.5f * MathF.Cos((c.Age - I) * MathF.Tau / 0.35f);
        fx.Sprite(FxShape.Glow, c.From, h * (0.7f + 0.15f * beat), Fx.Fade(Rgb(255, 90, 60), env * (0.25f + 0.25f * beat)), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I - 0.35f + i * 0.35f, 0.35f, h * 0.9f, Rgb(255, 140, 100));
    }

    /// <summary>Return: gold stars rise round the user as it bounds, then fly to the target.</summary>
    private static void Return(in FxCtx c, FxColors col, FxList fx)
    {
        var gold = Rgb(255, 222, 120);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - i * 0.05f) / 0.25f;
            if (k < 0f || k > 1f) continue;
            float a = i * MathF.Tau / 4f + k * 3f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * c.FromSize * 0.4f, c.FromSize * (0.3f + 0.7f * k), MathF.Sin(a) * c.FromSize * 0.2f);
            fx.Sprite(FxShape.Star, p, c.FromSize * 0.09f, Fx.Fade(gold, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 4f);
        }
        Fx.Stream(fx, c, 0.15f, I + 0.02f, 0.2f, 5, FxShape.Star, gold, White, 0.1f, arc: 0.08f, spread: 0.35f, spin: 6f, blend: FxBlend.Alpha);
        Impact(c, col, fx);
    }

    /// <summary>Quick Attack: a streak of white the user leaves behind as it darts in.</summary>
    private static void QuickAttackY6(in FxCtx c, FxColors col, FxList fx)
    {
        float env = Fx.Envelope(c.Age, 0.05f, 0.12f, 0.3f, 0.45f);
        if (env > 0f)
        {
            var end = Vector3.Lerp(c.From, c.To, Math.Clamp((c.Age - 0.05f) / 0.28f, 0f, 1f));
            fx.Ribbon(new[] { c.From, Vector3.Lerp(c.From, end, 0.5f), end }, c.FromSize * 0.12f * env, Fx.Fade(White, 0.6f * env));
        }
        QuickStrike(c, col, fx);
    }

    /// <summary>Fake Out: two hands clap together right in front of the target, and it flinches.</summary>
    private static void FakeOut(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(-0.9f, 0.1f, 0.2f), I, 0.28f, Rgb(255, 236, 214), -0.5f, 0.15f);
        Strike(c, fx, FxShape.Palm, new Vector3(0.9f, 0.1f, 0.2f), I, 0.28f, Rgb(255, 236, 214), 0.5f, 0.15f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.25f, c.ToSize * 0.9f, White);
            fx.ScreenFlash(White, 0.15f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.05f, I + 0.18f));
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Poison Gas: purple clouds drift over and swell round the target, lingering.</summary>
    private static void PoisonGas(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        Fx.Stream(fx, c, 0f, I + 0.05f, 0.3f, 6, FxShape.Smoke, Fx.Fade(col.Main, 0.75f), Fx.Fade(col.Light, 0.6f), 0.2f, arc: 0.06f, spread: 0.4f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.85f, 1.2f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 0.8f;
            var p = c.To + new Vector3(MathF.Cos(a) * c.ToSize * 0.4f, MathF.Sin(a * 0.7f) * c.ToSize * 0.25f, MathF.Sin(a) * c.ToSize * 0.2f);
            fx.Sprite(FxShape.Smoke, p, c.ToSize * (0.25f + 0.05f * MathF.Sin(c.Age * 3f + i)), Fx.Fade(i % 2 == 0 ? col.Main : col.Dark, env * 0.55f), FxBlend.Alpha, a);
        }
    }

    /// <summary>Screech: jagged waves of sound in a pale pink, and the air round the target jittering.</summary>
    private static void Screech(in FxCtx c, FxColors col, FxList fx)
    {
        Sound(c, fx, 6, FxShape.Wave, false, Rgb(255, 210, 230));
        if (c.Cue.Missed) return;
        float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.75f, 0.95f);
        if (env <= 0f) return;
        for (int i = 0; i < 4; i++)
        {
            float jitter = Fx.Signed(c.Seed + (int)(c.Age * 30f), i, 171);
            var at = c.To + new Vector3((i % 2 == 0 ? -1f : 1f) * c.ToSize * (0.55f + 0.05f * jitter), c.ToSize * (0.3f - 0.2f * (i / 2)), 0f);
            fx.Sprite(FxShape.Wave, at, c.ToSize * 0.2f, Fx.Fade(Rgb(255, 190, 220), env), rotation: i % 2 == 0 ? MathF.PI : 0f);
        }
    }

    /// <summary>Growth: leaves spiral up from the user's feet in a green glow, and a ring opens round it.</summary>
    private static void GrowthY6(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var green = ColorsOf(PokemonType.Grass);
        float h = c.FromSize;
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 1.1f + i / 8f) % 1f;
            float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, 1.05f);
            float a = i * MathF.Tau / 8f + u * 5f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.45f * (1f - 0.4f * u), h * 1.2f * u, MathF.Sin(a) * h * 0.25f);
            fx.Sprite(FxShape.Leaf, p, h * 0.1f, Fx.Fade(Fx.Mix(green.Main, green.Light, u), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, a);
        }
        Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I - 0.1f, 0.5f, h * 1.1f, green.Light);
        fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(green.Light, 0.3f * Fx.Envelope(c.Age, 0.1f, 0.3f, 0.7f, 1f)));
    }

    /// <summary>Leech Life: a sting driven in, and red motes drawn back to the user.</summary>
    private static void LeechLife(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, I - 0.18f, I, 0.16f, 1, FxShape.Streak, White, col.Light, 0.05f, trail: 6f);
        Impact(c, col, fx, 0.7f);
        if (!c.Cue.Landed) return;
        Fx.Stream(fx, c, I + 0.05f, 1.15f, 0.45f, 10, FxShape.Glow, Rgb(255, 110, 120), Rgb(255, 200, 200), 0.08f, arc: 0.1f, spread: 0.5f, reverse: true);
        Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.9f, Rgb(255, 190, 190), 0.4f);
    }

    /// <summary>Low Kick: a foot sweeps in low at the target's legs and kicks up dust.</summary>
    private static void LowKick(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.14f)) / 0.14f;
        float after = (c.Age - I) / 0.2f;
        if (k >= 0f && after <= 1f && !(c.Cue.Missed && k > 1f))
        {
            var p = c.ToFeet + new Vector3(-h * 0.9f * (1f - Fx.EaseOut(Math.Min(1f, k))), h * 0.22f, h * 0.1f);
            fx.Sprite(FxShape.Foot, p, h * 0.3f, Fx.Fade(Rgb(255, 230, 200), Math.Min(1f, k * 3f) * (1f - Math.Max(0f, after))), FxBlend.Alpha);
        }
        if (c.Cue.Landed) Dust(c, fx, c.ToFeet, h, I, 7);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Tail Whip: the user's tail swishes twice, leaving arcs of white.</summary>
    private static void TailWhip(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        foreach (float t0 in new[] { 0.05f, 0.35f })
        {
            float k = (c.Age - t0) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float a = MathF.PI * (0.2f + 0.6f * k) * (t0 > 0.1f ? -1f : 1f);
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.55f, h * 0.35f, MathF.Sin(a) * h * 0.2f);
            fx.Streak(FxShape.Streak, p, new Vector3(-MathF.Sin(a), 0.2f, MathF.Cos(a)), h * 0.08f, 5f, Fx.Fade(White, MathF.Sin(k * MathF.PI) * 0.8f));
        }
        Fx.Glint(fx, c.Age, c.To, c.ToSize, 0.45f, White, 0.3f);
    }

    /// <summary>Teleport: a column of pink light rises through the user, motes streaming up it.</summary>
    private static void Teleport(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.05f, 0.2f, 0.7f, 1.0f);
        if (env <= 0f) return;
        var bottom = c.FromFeet;
        var top = c.FromFeet + Vector3.UnitY * h * 2.4f;
        fx.Ribbon(new[] { bottom, Vector3.Lerp(bottom, top, 0.5f), top }, h * 0.9f * env, Fx.Fade(col.Main, 0.35f * env));
        fx.Ribbon(new[] { bottom, Vector3.Lerp(bottom, top, 0.5f), top }, h * 0.25f * env, Fx.Fade(col.Light, 0.6f * env));
        for (int i = 0; i < 10; i++)
        {
            float u = (c.Age * 1.6f + Fx.Rand(c.Seed, i, 181)) % 1f;
            var p = bottom + new Vector3(Fx.Signed(c.Seed, i, 182) * h * 0.35f, h * 2.2f * u, Fx.Signed(c.Seed, i, 183) * h * 0.15f);
            fx.Sprite(FxShape.Spark, p, h * 0.08f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>Water Sport and Mud Sport: drops thrown up round the user that rain back down round it.</summary>
    private static void Sport(in FxCtx c, FxList fx, FxColors col, FxShape shape)
    {
        fx.Land(I);
        float h = c.FromSize;
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * h * 0.4f, h, 0f, 0.5f, 10, shape, col.Main, col.Light, 0.08f, 0.9f, -1.6f, FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed + 5, c.FromFeet + Vector3.UnitY * h * 0.4f, h, I - 0.05f, 1.1f, 0.4f, 14, shape, col.Main, col.Light, 0.07f, FxBlend.Alpha, 0.6f);
        Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.5f, h * 1.0f, Fx.Fade(col.Dark, 0.8f));
    }

    /// <summary>Defense Curl: a pale swirl curls in round the user, and it pops with a glint.</summary>
    private static void DefenseCurl(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I, I + 0.2f);
        fx.Sprite(FxShape.Swirl, c.From, h * (0.8f - 0.35f * k), Fx.Fade(Rgb(255, 236, 214), env), rotation: -c.Age * 9f);
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I, 0.25f, h * 0.6f, Rgb(255, 240, 220));
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.2f, h, I, White, 0.4f);
    }

    /// <summary>Rock Smash: a fist driven into the target, cracks spreading and stone flying.</summary>
    private static void RockSmash(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Fist, new Vector3(-0.7f, 0.2f, 0.3f), I, 0.3f, Rgb(255, 214, 170));
        if (c.Cue.Landed)
        {
            float k = (c.Age - I) / 0.45f;
            if (k >= 0f && k <= 1f) fx.Sprite(FxShape.Crack, c.To, c.ToSize * 0.45f * Fx.EaseOut(k * 4f), Fx.Fade(Rgb(70, 50, 40), 1f - k * k), FxBlend.Alpha, c.Seed % 7 * 0.4f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Rock, Rgb(196, 170, 130), Rgb(120, 96, 70), 0.08f, 1.0f, 2.2f, FxBlend.Alpha, spin: 5f, salt: 22);
        }
        Impact(c, col, fx);
    }

    /// <summary>Aerial Ace: the user darts in and a long bright slash crosses the target.</summary>
    private static void AerialAce(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0f, 0.34f, 10, 0.75f);
        if (c.Cue.Landed)
        {
            float k = (c.Age - (I - 0.04f)) / 0.3f;
            if (k >= 0f && k <= 1f)
            {
                var along = Vector3.Normalize(new Vector3(1f, -0.8f, 0f));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.16f, 9f * Fx.EaseOut(k * 3f), Fx.Fade(col.Light, 1f - k));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.07f, 9f * Fx.EaseOut(k * 3f), Fx.Fade(White, 1f - k));
            }
            fx.ScreenFlash(White, 0.1f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.05f, I + 0.15f));
        }
        Impact(c, col, fx);
    }

    /// <summary>Astonish: a wisp leaps at the target, and an exclamation mark jumps over its head.</summary>
    private static void Astonish(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, I - 0.2f, I, 0.18f, 2, FxShape.Wisp, col.Main, col.Light, 0.18f, arc: 0.12f, spread: 0.2f);
        if (c.Cue.Landed)
        {
            float k = (c.Age - I) / 0.6f;
            if (k >= 0f && k <= 1f)
            {
                float hop = MathF.Sin(Math.Min(1f, k * 4f) * MathF.PI) * 0.15f;
                fx.Sprite(FxShape.Exclaim, c.ToFeet + Vector3.UnitY * c.ToSize * (1.15f + hop), c.ToSize * 0.17f, Fx.Fade(Rgb(255, 236, 120), Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha);
            }
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Brick Break: the edge of a hand chops down onto the target, and what it hits cracks and breaks.</summary>
    private static void BrickBreak(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(0.1f, 1.0f, 0.2f), I, 0.32f, Rgb(255, 214, 170), MathF.PI * 0.5f, 0.15f);
        if (c.Cue.Landed)
        {
            float k = (c.Age - I) / 0.5f;
            if (k >= 0f && k <= 1f) fx.Sprite(FxShape.Crack, c.To, c.ToSize * 0.5f, Fx.Fade(Rgb(80, 60, 50), 1f - k * k), FxBlend.Alpha, MathF.PI * 0.5f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.04f, 0.6f, 8, FxShape.Shard, Rgb(255, 230, 200), col.Main, 0.09f, 1.1f, 2f, FxBlend.Alpha, spin: 6f, salt: 23);
        }
        Impact(c, col, fx);
    }

    /// <summary>Counter: the user flares orange as it takes the blow, then strikes back with a fist.</summary>
    private static void Counter(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(col.Main, 0.45f * Fx.Envelope(c.Age, 0f, 0.06f, 0.12f, 0.24f)), FxBlend.Alpha);
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.02f, 0.22f, c.FromSize * 0.8f, col.Light);
        Strike(c, fx, FxShape.Fist, new Vector3(-0.6f, 0.1f, 0.3f), I, 0.3f, Rgb(255, 200, 150));
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Flash: a burst of white light from the user that floods the screen for a moment.</summary>
    private static void FlashY6(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        fx.ScreenFlash(White, 0.45f * Fx.Envelope(c.Age, 0.05f, 0.12f, I, I + 0.12f));
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.6f * Fx.EaseOut(c.Age / 0.3f), Fx.Fade(White, Fx.Envelope(c.Age, 0f, 0.1f, 0.4f, 0.8f)));
        Fx.Glint(fx, c.Age, c.From, c.FromSize, 0.05f, White, 1.2f);
    }

    /// <summary>Focus Punch: a fist gathers before the user, then flies into the target with a heavy blow.</summary>
    private static void FocusPunch(in FxCtx c, FxColors col, FxList fx)
    {
        float gather = Fx.Envelope(c.Age, 0f, 0.2f, 0.22f, 0.26f);
        var front = Vector3.Lerp(c.From, c.To, 0.15f);
        if (gather > 0f)
        {
            fx.Sprite(FxShape.Fist, front, c.FromSize * 0.24f * (0.6f + 0.4f * gather), Fx.Fade(Rgb(255, 200, 150), gather), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, front, c.FromSize * 0.5f, Fx.Fade(col.Main, 0.5f * gather));
        }
        float k = (c.Age - 0.24f) / (I - 0.24f);
        if (k >= 0f && k <= 1f && !c.Cue.Missed)
            fx.Sprite(FxShape.Fist, Vector3.Lerp(front, c.To, k), c.ScaleAt(k) * 0.3f, Rgb(255, 210, 170), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, c.ToSize * 1.3f, col.Light);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.2f, I + 0.4f));
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Force Palm: a palm pushed into the target, and rings of force bursting out of it with sparks.</summary>
    private static void ForcePalm(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(-0.6f, 0.1f, 0.3f), I, 0.32f, Rgb(200, 226, 255));
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.3f, c.ToSize * 1.0f, Rgb(140, 190, 255));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.1f, 0.3f, c.ToSize * 0.75f, Rgb(220, 236, 255));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.4f, 6, FxShape.Spark, Rgb(255, 240, 140), White, 0.08f, 0.9f, salt: 24);
        }
        Impact(c, col, fx);
    }

    /// <summary>Rock Polish: dust is ground off the user in a swirl and it shines, quick streaks rising off it.</summary>
    private static void RockPolish(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 0.95f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + c.Age * 8f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.2f + 0.08f * i), MathF.Sin(a) * h * 0.25f);
            fx.Sprite(FxShape.Dot, p, h * 0.04f, Fx.Fade(col.Light, env), FxBlend.Alpha);
        }
        SpeedLines(c, fx, 0.3f, 0.9f, 6, 0.5f);
        for (int i = 0; i < 3; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 191), Fx.Signed(c.Seed, i, 192), 0f) * h * 0.3f, h, I + i * 0.15f, White, 0.35f);
    }

    /// <summary>Take Down: the user backs off and charges, the blow lands hard, and it reels from the recoil.</summary>
    private static void TakeDown(in FxCtx c, FxColors col, FxList fx)
    {
        Dust(c, fx, c.FromFeet, c.FromSize, 0.1f, 8);
        SpeedLines(c, fx, 0.15f, 0.34f, 7, 0.6f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, c.ToSize * 1.1f, White);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.15f, I + 0.35f));
            // The recoil: little stars over the user's own head
            float env = Fx.Envelope(c.Age, I + 0.1f, I + 0.2f, 0.95f, 1.15f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * MathF.Tau / 3f + c.Age * 6f;
                var p = c.FromFeet + new Vector3(MathF.Cos(a) * c.FromSize * 0.25f, c.FromSize * 1.05f, MathF.Sin(a) * c.FromSize * 0.1f);
                fx.Sprite(FxShape.Star, p, c.FromSize * 0.07f, Fx.Fade(Rgb(255, 230, 110), env), FxBlend.Alpha, c.Age * 4f);
            }
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Harden: a grey sheen of metal climbs the user, glinting.</summary>
    private static void Harden(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var steel = ColorsOf(PokemonType.Steel);
        for (int i = 0; i < 2; i++)
        {
            float k = (c.Age - i * 0.25f) / 0.55f;
            if (k < 0f || k > 1f) continue;
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * k, Vector3.UnitY, h * 0.55f, Fx.Fade(steel.Main, MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.75f, Fx.Fade(steel.Light, 0.35f * Fx.Envelope(c.Age, 0.1f, 0.3f, 0.6f, 0.9f)));
        for (int i = 0; i < 4; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 201), Fx.Signed(c.Seed, i, 202), 0f) * h * 0.35f, h, 0.2f + i * 0.14f, White, 0.35f);
    }

    /// <summary>Headbutt: the user charges head down, the blow rings out and the target is left seeing stars.</summary>
    private static void Headbutt(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0.08f, 0.34f, 5, 0.5f);
        if (c.Cue.Landed) Fx.Shockwave(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.2f, Vector3.Zero, I, 0.25f, c.ToSize * 0.7f, White);
        Dizzy(c, fx, I + 0.1f);
        Impact(c, col, fx);
    }

    /// <summary>Pursuit: a dark shadow streaks after the target and slashes across it.</summary>
    private static void Pursuit(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, I, 0.25f, 6, FxShape.Smoke, Rgb(60, 40, 76, 220), Rgb(30, 20, 40, 220), 0.16f, spread: 0.25f, blend: FxBlend.Alpha);
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.04f, 1, 0.1f, col.Main, col.Light);
        Impact(c, col, fx);
    }

    /// <summary>Rock Throw: two stones lobbed one after the other, landing with dust.</summary>
    private static void RockThrow(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, I + 0.1f, 0.3f, 2, FxShape.Rock, col.Light, col.Main, 0.17f, arc: 0.25f, spread: 0.2f, spin: 6f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Dust(c, fx, c.ToFeet, c.ToSize, I, 5);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.1f, 0.5f, 6, FxShape.Rock, col.Light, col.Dark, 0.06f, 0.8f, 2.4f, FxBlend.Alpha, spin: 5f, salt: 25);
        }
        Impact(c, col, fx);
    }

    /// <summary>Stealth Rock: pointed stones lobbed high over the field, each settling round the foe's platform.</summary>
    private static void StealthRock(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        for (int i = 0; i < 5; i++)
        {
            float t0 = i * 0.06f;
            float k = (c.Age - t0) / (I - 0.05f);
            float a = i * MathF.Tau / 5f + 0.4f;
            var land = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.85f, h * 0.15f, MathF.Sin(a) * h * 0.4f);
            if (k >= 0f && k <= 1f)
            {
                var p = Vector3.Lerp(c.From, land, k) + Vector3.UnitY * Vector3.Distance(c.From, land) * 0.35f * 4f * k * (1f - k);
                fx.Sprite(FxShape.Shard, p, c.ScaleAt(k) * 0.12f, col.Light, FxBlend.Alpha, k * 7f + i);
            }
            else if (k > 1f)
            {
                float fade = Math.Clamp((1.25f - c.Age) / 0.3f, 0f, 1f);
                fx.Sprite(FxShape.Shard, land, h * 0.12f, Fx.Fade(col.Light, fade), FxBlend.Alpha, i);
                Fx.Burst(fx, c.Seed + i, c.Age, land, h * 0.6f, t0 + I - 0.05f, 0.35f, 3, FxShape.Smoke, Rgb(222, 206, 176, 200), Rgb(176, 160, 132, 190), 0.1f, 0.3f, -0.2f, FxBlend.Alpha, salt: 26);
            }
        }
    }

    /// <summary>Focus Energy: the user draws itself in, streaks of orange rushing into it and flaring up.</summary>
    private static void FocusEnergy(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        for (int i = 0; i < 10; i++)
        {
            float u = (c.Age * 1.8f + Fx.Rand(c.Seed, i, 211)) % 1f;
            float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.6f, 0.8f);
            float a = Fx.Rand(c.Seed, i, 212) * MathF.Tau;
            var dir = new Vector3(MathF.Cos(a), Fx.Signed(c.Seed, i, 213) * 0.5f, MathF.Sin(a) * 0.5f);
            var p = c.From + dir * h * 0.9f * (1f - u);
            fx.Streak(FxShape.Streak, p, dir, h * 0.06f, 4f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)));
        }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, I - 0.1f, 1.15f, col.Main, col.Light, 10);
    }
}
