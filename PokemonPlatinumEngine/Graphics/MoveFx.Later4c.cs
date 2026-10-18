using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13a, the last part: the first thirteen regular moves of Generation 8, from Dynamax Cannon (744) to Court
// Change (756); Max Guard (743) and the Max Moves after it are Y15's. As MoveFx.Later4.cs.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later4c() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dynamax Cannon"] = DynamaxCannonY13,
        ["Snipe Shot"] = SnipeShotY13,
        ["Jaw Lock"] = JawLockY13,
        ["Stuff Cheeks"] = StuffCheeksY13,
        ["No Retreat"] = NoRetreatY13,
        ["Tar Shot"] = TarShotY13,
        ["Magic Powder"] = MagicPowderY13,
        ["Dragon Darts"] = DragonDartsY13,
        ["Teatime"] = TeatimeY13,
        ["Octolock"] = OctolockY13,
        ["Bolt Beak"] = BoltBeakY13,
        ["Fishious Rend"] = FishiousRendY13,
        ["Court Change"] = CourtChangeY13
    };

    /// <summary>Dynamax Cannon: three great rings of crimson-violet light stack up as a muzzle and a colossal beam is fired through them, ending in a wide ring and a deep shock.</summary>
    private static void DynamaxCannonY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var red = Rgb(230, 60, 90);
        var dir = Vector3.Normalize(c.Aim - c.From);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.3f, 0.45f);
        for (int i = 0; i < 3; i++)
        {
            float k = Math.Clamp((c.Age - i * 0.05f) / 0.2f, 0f, 1f);
            if (env > 0f) fx.Sprite(FxShape.Ring, c.From + dir * h * (0.5f + 0.4f * i), h * (0.7f - 0.15f * i) * k, Fx.Fade(i % 2 == 0 ? red : col.Light, env), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.16f, 0.62f, h * 0.42f, Fx.Fade(red, 0.85f), White, 12);
        Fx.Stream(fx, c, 0.18f, 0.55f, 0.28f, 8, FxShape.Spark, col.Light, White, 0.12f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, c.ToSize * 2.0f, Fx.Fade(red, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.06f, 0.45f, c.ToSize * 1.4f, Fx.Fade(White, 0.9f));
            fx.ScreenFlash(Rgb(255, 190, 200), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.06f, I + 0.24f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Snipe Shot: a pin-thin line of water is drawn through a red sight, and a single bead of water hits the foe at the middle of the sight, unbent by anyone's pull.</summary>
    private static void SnipeShotY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float sight = Fx.Envelope(c.Age, 0f, 0.06f, I - 0.06f, I);
        if (sight > 0f)
        {
            fx.Sprite(FxShape.Target, c.To, h * 0.55f * (1.2f - 0.4f * Math.Clamp(c.Age / I, 0f, 1f)), Fx.Fade(Rgb(255, 80, 80), sight), FxBlend.Alpha);
            fx.Ribbon(new[] { c.From, c.Aim }, h * 0.015f, Fx.Fade(Rgb(255, 120, 120), sight * 0.8f), FxBlend.Add);
        }
        float k = (c.Age - (I - 0.12f)) / 0.12f;
        if (k > 0f && k < 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.08f, 7f, Fx.Fade(col.Main, 1f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.03f, 7f, White);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Drop, col.Light, White, 0.09f, 0.9f, 0.5f, FxBlend.Alpha, salt: 390);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.3f, h * 0.9f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.4f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Jaw Lock: a great pair of dark jaws closes on the foe, and a ring of chain link joins the two Pokémon so that neither can leave.</summary>
    private static void JawLockY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FangPair(c, fx, I - 0.14f, 0.7f, Rgb(70, 54, 90), col.Light);
        float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.95f, 1.15f);
        if (env > 0f && c.Cue.Landed)
        {
            var a = c.FromFeet + new Vector3(0f, c.FromSize * 0.3f, 0.2f);
            var b = c.ToFeet + new Vector3(0f, h * 0.3f, 0.2f);
            for (int i = 0; i <= 7; i++)
            {
                var p = Vector3.Lerp(a, b, i / 7f) + new Vector3(0f, MathF.Sin(i / 7f * MathF.PI) * -h * 0.25f, 0f);
                L2Ellipse(fx, p, h * 0.1f, h * 0.06f, h * 0.025f, Fx.Fade(Rgb(176, 184, 204), env), 8, i % 2 == 0 ? 0f : 1.57f, FxBlend.Alpha);
            }
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Spark, White, col.Light, 0.09f, 0.8f, salt: 391);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Stuff Cheeks: red and blue berries pop into the user one after another, its cheeks swelling in two round glows, and two up arrows climb.</summary>
    private static void StuffCheeksY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var mouth = c.From + new Vector3(h * 0.12f, h * 0.2f, 0.25f);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.04f - i * 0.1f) / 0.22f;
            if (k < 0f || k > 1f) continue;
            var from = mouth + new Vector3(h * (0.9f - 0.2f * i), h * (0.8f - 0.05f * i), 0f);
            fx.Sprite(FxShape.Dot, Vector3.Lerp(from, mouth, Fx.EaseOut(k)), h * 0.1f, Fx.Fade(i % 2 == 0 ? Rgb(230, 60, 80) : Rgb(80, 130, 230), env * (1f - 0.5f * k)), FxBlend.Alpha);
        }
        float puff = Fx.EaseOut(Math.Clamp((c.Age - 0.2f) / 0.3f, 0f, 1f));
        for (int s = -1; s <= 1; s += 2)
            fx.Sprite(FxShape.Glow, mouth + new Vector3(s * h * 0.2f, -h * 0.05f, 0f), h * 0.3f * puff, Fx.Fade(Rgb(255, 170, 150), env * 0.8f), FxBlend.Alpha);
        L3Up(fx, c.Age, c.FromFeet, h, 0.35f, 2, Rgb(255, 200, 110));
    }

    /// <summary>No Retreat: a ring of red stakes rises round the user's feet, shutting every way back, and five arrows of power climb up inside it.</summary>
    private static void NoRetreatY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float rise = Fx.EaseOut(Math.Clamp(c.Age / 0.25f, 0f, 1f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var foot = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.8f, 0.05f, MathF.Sin(a) * h * 0.45f);
            fx.Streak(FxShape.Beam, foot + new Vector3(0f, h * 0.35f * rise, 0f), Vector3.UnitY, h * 0.06f, 5f * rise, Fx.Fade(Rgb(230, 70, 70), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, foot + new Vector3(0f, h * 0.7f * rise, 0f), h * 0.07f, Fx.Fade(White, env * rise), FxBlend.Alpha);
        }
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.06f, Vector3.UnitY, h * 0.95f, Fx.Fade(Rgb(255, 110, 90), env), FxBlend.Alpha);
        L3Up(fx, c.Age, c.FromFeet, h, 0.2f, 5, Rgb(255, 140, 110));
    }

    /// <summary>Tar Shot: a glob of black tar is flung at the foe, splatters over it and drips down in long strings, a down arrow sinking under its speed.</summary>
    private static void TarShotY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var tar = Rgb(40, 34, 38);
        float k = (c.Age - 0.03f) / (I - 0.03f);
        if (k >= 0f && k <= 1f) fx.Sprite(FxShape.Orb, Lob(c.From, c.Aim, k, 0.07f), c.FromSize * 0.28f, Fx.Fade(tar, 1f), FxBlend.Alpha, k * 6f);
        if (c.Age >= I)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.9f, 1.1f);
            fx.Sprite(FxShape.Smoke, c.To, h * 0.8f, Fx.Fade(tar, env * 0.8f), FxBlend.Alpha, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                float drip = Math.Clamp((c.Age - I) / 0.6f, 0f, 1f);
                var top = c.To + new Vector3((i - 2) * h * 0.2f, h * 0.2f, 0.3f);
                fx.Ribbon(new[] { top, top - new Vector3(0f, h * (0.2f + 0.5f * drip * (0.6f + 0.1f * i)), 0f) }, h * 0.06f, Fx.Fade(tar, env), FxBlend.Alpha);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Drop, tar, Rgb(90, 80, 86), 0.12f, 0.9f, 0.4f, FxBlend.Alpha, salt: 392);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(240, 170, 90));
        }
    }

    /// <summary>Magic Powder: a pinch of purple glitter is sprinkled from above over the foe in a drifting cloud and its outline glows a Psychic violet.</summary>
    private static void MagicPowderY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.05f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(Rgb(190, 110, 240), env * 0.45f * Math.Clamp(c.Age / 0.5f, 0f, 1f)), FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 0.2f, 0f), h, 0f, 0.8f, 0.5f, 14, FxShape.Spark, Rgb(230, 170, 255), White, 0.08f, FxBlend.Add);
        Fx.Rain(fx, c.Age, c.Seed + 4, c.ToFeet + new Vector3(0f, h * 0.2f, 0f), h, 0.05f, 0.8f, 0.5f, 6, FxShape.Dot, Rgb(180, 100, 230), Rgb(230, 170, 255), 0.07f, FxBlend.Alpha);
        L1Spiral(fx, c.Age, c.Seed, c.ToFeet, h, 0.15f, 1.0f, 6, FxShape.Star, Rgb(220, 150, 255), White, 0.08f, 0.55f, 1.0f);
    }

    /// <summary>Dragon Darts: two long darts with indigo heads are loosed in turn, one a moment behind the other, each pinning the foe with its own small flash.</summary>
    private static void DragonDartsY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int d = 0; d < 2; d++)
        {
            float t = I - 0.1f + d * 0.1f;
            float k = (c.Age - (t - 0.14f)) / 0.14f;
            if (k < 0f || c.Age > t + 0.25f) continue;
            var to = c.Aim + new Vector3(0f, (d == 0 ? 1f : -1f) * h * 0.18f, 0f);
            var p = Vector3.Lerp(c.From + new Vector3(0f, (d - 0.5f) * c.FromSize * 0.2f, 0f), to, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.25f);
            fx.Streak(FxShape.Streak, p, to - c.From, c.FromSize * 0.07f, 9f, Fx.Fade(Rgb(214, 200, 240), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Arrow, p + Vector3.Normalize(to - c.From) * c.FromSize * 0.2f, to - c.From, c.FromSize * 0.11f, 1.5f, Fx.Fade(col.Main, fade), FxBlend.Alpha);
            if (c.Cue.Landed && c.Age >= t) Fx.Contact(fx, c.Age, c.Seed + d, to, h, t, 0.2f, col.Main, col.Light);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.4f, 0.2f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Teatime: a white cup on a saucer appears in the middle of the field, steam curling from it and berries and tea-lights drifting round, and everyone's berries glow.</summary>
    private static void TeatimeY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var cup = mid + new Vector3(0f, h * 0.3f, 0.2f);
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.2f, Fx.Fade(Rgb(255, 224, 170), env * 0.5f), FxBlend.Alpha);
        L2Ellipse(fx, mid + new Vector3(0f, h * 0.12f, 0.15f), h * 0.55f, h * 0.12f, h * 0.05f, Fx.Fade(Rgb(250, 246, 240), env), 14, 0f, FxBlend.Alpha);
        fx.Sprite(FxShape.Orb, cup, h * 0.32f, Fx.Fade(Rgb(250, 246, 240), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Orb, cup + new Vector3(0f, h * 0.1f, 0.05f), h * 0.22f, Fx.Fade(Rgb(150, 90, 50), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, cup + new Vector3(h * 0.38f, 0f, 0f), h * 0.12f, Fx.Fade(Rgb(250, 246, 240), env), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, cup + new Vector3(0f, h * 0.2f, 0f), h, 0f, 1.0f, 5, FxShape.Smoke, Rgb(255, 255, 255, 220), 0.14f, 0.1f);
        for (int i = 0; i < 4; i++)
        {
            float a = c.Age * 2f + i * 1.57f;
            fx.Sprite(FxShape.Heart, mid + new Vector3(MathF.Cos(a) * h * 1.1f, h * (0.5f + 0.1f * MathF.Sin(a * 2f)), MathF.Sin(a) * 0.3f), h * 0.1f, Fx.Fade(i % 2 == 0 ? Rgb(255, 150, 160) : Rgb(150, 200, 255), env), FxBlend.Alpha);
        }
    }

    /// <summary>Octolock: eight long arms of violet-red light spiral out and wind tight round the foe, the grip squeezing in and its Defense and Sp. Def draining.</summary>
    private static void OctolockY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.88f, 1.1f);
        if (env <= 0f) return;
        float grip = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / 0.35f, 0f, 1f));
        for (int i = 0; i < 6; i++)
        {
            var pts = new Vector3[14];
            float a0 = i * MathF.Tau / 6f;
            for (int j = 0; j <= 13; j++)
            {
                float u = j / 13f;
                float turn = a0 + u * MathF.Tau * 1.3f;
                float r = h * (1.3f - 0.8f * u * grip);
                pts[j] = c.ToFeet + new Vector3(MathF.Cos(turn) * r, h * (0.1f + 0.9f * u * grip), MathF.Sin(turn) * r * 0.5f + 0.1f);
            }
            fx.Ribbon(pts, h * 0.09f, Fx.Fade(i % 2 == 0 ? col.Main : Rgb(200, 70, 90), env), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(255, 130, 110));
            Jolt(fx, c.Age, I, 0.4f, 0.25f);
        }
    }

    /// <summary>Bolt Beak: the user's beak crackles yellow and it dives in beak-first, the foe struck in a burst of sparks and a bolt that arcs on over its head.</summary>
    private static void BoltBeakY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var beak = c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.15f, 0.1f);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.08f, I);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Beak, beak, c.FromSize * 0.4f, Fx.Fade(Rgb(255, 238, 120), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, beak, c.FromSize * 0.7f, Fx.Fade(col.Main, env * 0.6f));
            Fx.Bolt(fx, c.Age, c.Seed, beak, beak + new Vector3(c.FromSize * 0.4f, c.FromSize * 0.3f, 0f), 0f, 0.3f, c.FromSize * 0.03f, col.Main, col.Light, 0);
        }
        Strike(c, fx, FxShape.Beak, new Vector3(-1.2f, 0.4f, 0f), I, 0.55f, Rgb(255, 238, 120), 0.2f, 0.12f);
        if (c.Cue.Landed)
        {
            Fx.Bolt(fx, c.Age, c.Seed + 9, c.To, c.To + new Vector3(h * 0.7f, h * 0.8f, 0f), I, I + 0.2f, h * 0.05f, col.Main, col.Light, 1);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, col.Main, 0.1f, 1.0f, salt: 393);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Fishious Rend: a pair of water-blue fangs closes on the foe in a spray of scales and drops, the pale blue scales falling like rain after.</summary>
    private static void FishiousRendY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FangPair(c, fx, I - 0.12f, 0.5f, Rgb(120, 190, 255), White);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 9, FxShape.Drop, col.Light, White, 0.1f, 1.0f, 0.5f, FxBlend.Alpha, salt: 394);
            Fx.Rain(fx, c.Age, c.Seed, c.To, h, I, 0.95f, 0.4f, 7, FxShape.Hex, Rgb(190, 225, 255), Rgb(120, 180, 240), 0.07f, FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Court Change: two ground panels, one on each side, lift and trade places along curved arrows, the ground's marks swapped from side to side.</summary>
    private static void CourtChangeY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var a = c.FromFeet + Vector3.UnitY * 0.05f;
        var b = c.Foe - Vector3.UnitY * c.FoeSize * 0.5f + Vector3.UnitY * 0.05f;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.12f) / 0.45f, 0f, 1f));
        foreach (var (from, to, color) in new[] { (a, b, Rgb(255, 214, 120)), (b, a, Rgb(170, 220, 255)) })
        {
            var p = Vector3.Lerp(from, to, k) + new Vector3(0f, MathF.Sin(k * MathF.PI) * h * 0.9f, 0f);
            fx.Flat(FxShape.Hex, p, Vector3.UnitY, h * 0.7f, Fx.Fade(color, env * 0.8f), FxBlend.Alpha, c.Age * 3f);
            fx.Flat(FxShape.Ring, from, Vector3.UnitY, h * (0.5f + 0.4f * k), Fx.Fade(color, env * (1f - k)), FxBlend.Alpha);
            var arc = new Vector3[11];
            for (int i = 0; i <= 10; i++)
                arc[i] = Vector3.Lerp(from, to, i / 10f) + new Vector3(0f, MathF.Sin(i / 10f * MathF.PI) * h * 0.9f, 0.1f);
            fx.Ribbon(arc, h * 0.04f, Fx.Fade(color, env * 0.7f), FxBlend.Alpha);
        }
        Fx.Glint(fx, c.Age, (a + b) * 0.5f + Vector3.UnitY * h * 0.5f, h, 0.55f, White, 0.5f);
    }
}
