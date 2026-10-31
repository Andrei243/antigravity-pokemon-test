using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Dynamax and Terastallization (plan 25 · Y15; style guide, "Dynamax, Max Moves and Terastallization"): a Dynamax's
// growth and its end, the crystal of a Terastallization, and what lasts of either (the wreath of red clouds over a
// giant's head, the crown of crystal over a Terastallized one). All of it our own, since Platinum has none of it:
// each cue a pure function of its age and seed, under MaxQuads at every age, with no screen flash over 0.35 and no
// shake; what lasts a pure function of the battle's clock and the place, under GiantCap a Pokémon. What the body
// itself shows (its red, its crystal's sheen) is BodyCast's and GiantCast's, which the renderer hands the shader.
internal static partial class MoveFx
{
    private static readonly Color DynaRed = Rgb(232, 40, 84), DynaPink = Rgb(255, 120, 170), DynaCloud = Rgb(90, 24, 48), DynaRim = Rgb(255, 176, 200),
        DynaGold = Rgb(255, 206, 96);

    /// <summary>The seven colours a Stellar Terastallization turns through.</summary>
    private static readonly Color[] Stellar =
    {
        Rgb(255, 92, 92), Rgb(255, 168, 64), Rgb(255, 222, 80), Rgb(110, 220, 110), Rgb(90, 220, 240), Rgb(96, 140, 255), Rgb(186, 110, 255)
    };

    /// <summary>The most a Pokémon's lasting Dynamax or Tera look draws (the wreath, the crown).</summary>
    public const int GiantCap = 12;

    /// <summary>A Stellar colour at a phase (whole turns go round the seven), blended between neighbours.</summary>
    public static Color StellarColour(float phase)
    {
        float p = (phase % 1f + 1f) % 1f * Stellar.Length;
        int i = (int)p;
        return Fx.Mix(Stellar[i % Stellar.Length], Stellar[(i + 1) % Stellar.Length], p - i);
    }

    /// <summary>A Tera Type's colours: its type's, or a Stellar one's at this phase.</summary>
    private static FxColors TeraColours(bool stellar, PokemonType type, float phase)
    {
        if (!stellar) return ColorsOf(type);
        var c = StellarColour(phase);
        return new(c, Fx.Mix(c, White, 0.6f), Fx.Mix(c, Rgb(20, 20, 40), 0.4f));
    }

    /// <summary>What the body shows through a Dynamax, its end or a Terastallization (none for any other cue).</summary>
    private static (Color Color, float Amount) GiantBodyCast(EffectCue cue)
    {
        float t = cue.Age;
        switch (cue.Kind)
        {
            case CueKind.Dynamax:
                // After the trainer's call, if there was one (plan 25 · YD1): the call's red is the drawing back's own
                return (DynaRed, 0.75f * Fx.Envelope(t - cue.Lead, 0f, 0.6f, 0.75f, 1.3f));
            case CueKind.DynamaxEnd:
                return (DynaPink, 0.4f * Fx.Envelope(t, 0f, 0.05f, 0.3f, 0.6f));
            case CueKind.Tera:
                return (TeraColours(cue.Stellar, cue.Type, t * 0.6f).Light, 0.8f * Fx.Envelope(t, 0.1f, 0.8f, 0.92f, 1.4f));
            default:
                return (default, 0f);
        }
    }

    /// <summary>
    /// What a Pokémon's body shows for as long as it is Dynamaxed (a red pulse) or Terastallized (a sheen of its Tera
    /// Type's light, a Stellar one's turning), at the battle's clock: under any cue's own cast (style guide, "Dynamax,
    /// Max Moves and Terastallization").
    /// </summary>
    public static (Color Color, float Amount) GiantCast(CombatantView v, float time)
    {
        if (v.Dynamaxed) return (DynaRed, 0.18f + 0.06f * MathF.Sin(time * MathF.Tau * PulseHz(v.DynamaxTurnsLeft)));
        if (v.Shown?.Terastallized is { } tera)
            return (TeraColours(tera.IsStellar, tera.Type, time * 0.25f).Light, 0.15f + 0.05f * MathF.Sin(time * MathF.Tau * 0.5f));
        return (default, 0f);
    }

    // ------------------------------------------------------------------ what lasts

    /// <summary>The wreath over every Dynamaxed Pokémon's head and the crown over every Terastallized one's.</summary>
    public static void Giants(BattleAnimator anim, FxPlaces places, FxList fx)
    {
        for (int s = 0; s < 2; s++)
            for (int slot = 0; slot < anim.SlotsOf((BattleSide)s); slot++)
            {
                var side = (BattleSide)s;
                var v = anim[side, slot];
                bool giant = v.Dynamaxed, tera = v.Shown?.Terastallized != null;
                if (!giant && !tera) continue;
                float shown = anim.LastingShown(side, slot);
                if (shown <= 0f) continue;
                var feet = places.Feet(side, slot);
                float h = places.Height(side, slot), w = places.HalfWidth(side, slot);
                int seed = slot * 37 + s * 101 + 7;
                if (giant) Wreath(feet, h, w, anim.Time, seed, v.Gigantamax, shown, fx, v.DynamaxTurnsLeft);
                else if (v.Shown!.Terastallized is { } type) Crown(feet, h, TeraColours(type.IsStellar, type.Type, anim.Time * 0.25f), anim.Time, type.IsStellar, shown, 0f, fx);
            }
    }

    /// <summary>How many clouds a giant's wreath has with this many turns left (plan 25 · YD3): six, five, four; six while unknown.</summary>
    public static int WreathClouds(int turns) => turns is >= 1 and <= 3 ? 3 + turns : 6;

    /// <summary>How fast a giant's body pulses with this many turns left (plan 25 · YD3): 1.2, 1.6, 2.2 Hz.</summary>
    public static float PulseHz(int turns) => turns switch { 1 => 2.2f, 2 => 1.6f, _ => 1.2f };

    /// <summary>The red of a Dynamax's clouds, bright enough to read over grass and sand alike.</summary>
    private static Color CloudRed => Fx.Mix(DynaCloud, DynaRed, 0.65f);

    /// <summary>
    /// The wreath of a giant: six red clouds turning slowly round its feet (the half behind it depth-tested), pink
    /// light on three of them, a glow under it and two motes rising up its body (a Gigantamax's gold). At most 12 quads.
    /// Round the feet, never over the head: a giant's head stands near the picture's top, and over it under the HP box.
    /// </summary>
    private static void Wreath(Vector3 feet, float h, float w, float time, int seed, bool gold, float strength, FxList fx, int turns = 3)
    {
        if (strength <= 0f) return;
        // It thins a cloud a turn, and flickers on the last (plan 25 · YD3)
        int clouds = WreathClouds(turns);
        if (turns == 1) strength *= 0.8f + 0.2f * MathF.Sin(time * MathF.Tau * 9f);
        float rx = w + h * 0.12f;
        for (int i = 0; i < clouds; i++)
        {
            float a = i * MathF.Tau / clouds + time * 0.5f;
            var p = feet + new Vector3(MathF.Cos(a) * rx, h * (0.07f + 0.02f * MathF.Sin(time * 2f + i)), MathF.Sin(a) * rx * 0.6f);
            bool behind = MathF.Sin(a) < 0f;
            fx.Sprite(FxShape.Smoke, p, h * 0.2f, Fx.Fade(CloudRed, strength * 0.7f), FxBlend.Alpha, a + time * 0.4f, depth: behind);
            if (i % 2 == 0) fx.Sprite(FxShape.Glow, p + Vector3.UnitY * h * 0.02f, h * 0.13f, Fx.Fade(gold && i == 0 ? DynaGold : DynaPink, strength * 0.7f), depth: behind);
        }
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, w + h * 0.3f, Fx.Fade(DynaRed, strength * 0.45f));
        for (int i = 0; i < 2; i++)
        {
            float u = (time * 0.5f + Fx.Rand(seed, i, 401)) % 1f;
            var p = feet + new Vector3(Fx.Signed(seed, i, 402) * w * 0.9f, h * (0.1f + 0.8f * u), h * 0.1f);
            fx.Sprite(gold ? FxShape.Spark : FxShape.Glow, p, h * 0.045f, Fx.Fade(gold ? DynaGold : DynaRim, strength * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>
    /// The crown of crystal over a Terastallized Pokémon's head: five upright facets in a fan 0.32 <i>h</i> across, the
    /// middle one tallest, and a glint over it. <paramref name="rise"/> lifts it from below as it first stands. 6 quads.
    /// </summary>
    private static void Crown(Vector3 feet, float h, FxColors col, float time, bool stellar, float strength, float rise, FxList fx)
    {
        if (strength <= 0f) return;
        var top = feet + Vector3.UnitY * h * (1.03f - 0.08f * rise);
        for (int i = -2; i <= 2; i++)
        {
            float tall = 0.15f - 0.025f * Math.Abs(i);
            var p = top + new Vector3(i * h * 0.065f, h * tall * 0.5f, 0f);
            var c = stellar ? StellarColour(time * 0.25f + (i + 2) / 7f) : (i % 2 == 0 ? col.Light : col.Main);
            fx.Sprite(FxShape.Shard, p, h * tall, Fx.Fade(c, strength * 0.9f), FxBlend.Alpha, -i * 0.22f);
        }
        float glint = 0.6f + 0.4f * MathF.Sin(time * 3f);
        fx.Sprite(FxShape.Spark, top + Vector3.UnitY * h * 0.17f, h * 0.07f, Fx.Fade(White, strength * glint), rotation: time);
    }

    // ------------------------------------------------------------------ Dynamax and its end

    /// <summary>
    /// A Dynamax: red clouds gather round the feet and a column of red light rises through the body as it grows (the
    /// animator's <see cref="BattleAnimator.GiantOf"/>); at 0.9 s a burst of cloud and a red ring (a Gigantamax's with
    /// gold sparks and bolts); the clouds close over its head into its wreath.
    /// </summary>
    private static void DynamaxGrowth(in FxCtx c, FxList fx)
    {
        // The growth comes after the trainer's call (plan 25 · YD1), whose ball and beam are the renderer's; its shown
        // age holds at the burst (the hit-stop), and the flash keeps the real clock so it is never held
        float lead = c.Cue.Lead;
        float t = c.Age - lead, h = c.FromSize, w = c.FromHalfWidth;
        float real = c.Cue.Age - lead;
        var feet = c.FromFeet;
        var middle = c.From;
        bool gold = c.Cue.Gigantamax;
        const float B = BattleAnimator.DynamaxGrown;
        fx.Land(lead + B);
        if (t < 0f) return;
        if (lead > 0f) OpenedBall(c, t, fx);

        // The clouds gathering round the feet, turning and drawing in
        float gather = Fx.Envelope(t, 0f, 0.15f, 0.8f, 1.0f);
        if (gather > 0f)
        {
            float r = (w + h * 0.1f) * (1.6f - 0.7f * Fx.EaseOut(Math.Clamp(t / 0.6f, 0f, 1f)));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + t * 2.2f;
                var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.08f + 0.06f * Fx.Rand(c.Seed, i, 411)), MathF.Sin(a) * r * 0.6f);
                bool behind = MathF.Sin(a) < 0f;
                fx.Sprite(FxShape.Smoke, p, h * 0.22f, Fx.Fade(CloudRed, gather * 0.75f), FxBlend.Alpha, a, depth: behind);
                if (i % 2 == 0) fx.Sprite(FxShape.Glow, p, h * 0.14f, Fx.Fade(DynaPink, gather * 0.6f), depth: behind);
            }
            for (int i = 0; i < 10; i++)
            {
                float u = (t * 1.3f + Fx.Rand(c.Seed, i, 412)) % 1f;
                var p = feet + new Vector3(Fx.Signed(c.Seed, i, 413) * w * 0.9f, h * 1.2f * u, h * 0.12f * Fx.Signed(c.Seed, i, 414));
                fx.Sprite(FxShape.Spark, p, h * 0.05f, Fx.Fade(i % 2 == 0 ? DynaRed : DynaRim, gather * MathF.Sin(u * MathF.PI)));
            }
        }

        // The column of red light rising up through the body
        float column = Fx.Envelope(t, 0.05f, 0.25f, 0.75f, 0.95f);
        if (column > 0f)
        {
            float reach = Fx.EaseOut(Math.Clamp((t - 0.05f) / 0.3f, 0f, 1f));
            var pts = new[] { feet, feet + Vector3.UnitY * h * 0.62f * reach, feet + Vector3.UnitY * h * 1.25f * reach };
            fx.Ribbon(pts, w * 0.9f + h * 0.1f, Fx.Fade(DynaRed, column * 0.45f), FxBlend.Alpha, depth: true);
            fx.Ribbon(pts, w * 0.35f + h * 0.04f, Fx.Fade(DynaPink, column * 0.7f));
        }

        // The burst: a red ring, cloud thrown out, a flash; a Gigantamax's gold sparks and bolts in the clouds
        Fx.Shockwave(fx, t, middle, Vector3.Zero, B, 0.5f, h * 1.4f, Fx.Fade(DynaRed, 0.9f));
        Fx.Shockwave(fx, t, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, B, 0.55f, h * 1.3f, Fx.Fade(DynaPink, 0.8f));
        Fx.Burst(fx, c.Seed, t, middle, h, B, 0.6f, 10, FxShape.Smoke, CloudRed, DynaPink, 0.18f, 0.8f, 0f, FxBlend.Alpha, salt: 61);
        fx.ScreenFlash(Rgb(255, 200, 220), 0.25f * Fx.Envelope(real, B - 0.02f, B + 0.03f, B + 0.06f, B + 0.25f));
        // The shockwave (plan 25 · YD1): a ring on the ground out across the stage, and dust thrown off the platform's rim
        Fx.Shockwave(fx, t, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, B, 0.7f, h * GiantWaveReach, Fx.Fade(DynaPink, 0.75f));
        GroundDust(c.Seed, t - B, feet, h, w, fx);
        if (gold)
        {
            Fx.Burst(fx, c.Seed, t, middle, h, B, 0.55f, 10, FxShape.Spark, DynaGold, White, 0.07f, 1.1f, salt: 62);
            for (int i = 0; i < 3; i++)
            {
                float t0 = B + 0.05f + i * 0.12f;
                float a = Fx.Rand(c.Seed, i, 415) * MathF.Tau;
                var from = feet + new Vector3(MathF.Cos(a) * w, h * 1.12f, MathF.Sin(a) * w * 0.6f);
                var to = from + new Vector3(Fx.Signed(c.Seed, i, 416) * h * 0.25f, -h * 0.2f, 0f);
                Fx.Bolt(fx, t, c.Seed + i * 13, from, to, t0, t0 + 0.16f, h * 0.025f, DynaGold, White, 0);
            }
        }

        // Settling: the clouds closing over its head into its wreath
        float settle = Fx.Envelope(t, 1.0f, 1.25f, BattleAnimator.DynamaxTime - BattleAnimator.ConditionReturnTime, BattleAnimator.DynamaxTime);
        if (settle > 0f) Wreath(feet, h, w, t, c.Seed, gold, settle, fx);
    }

    /// <summary>How far the growth's ground ring runs out, in the body's height (plan 25 · YD1).</summary>
    public const float GiantWaveReach = 3f;

    /// <summary>
    /// Six puffs of dust thrown off the platform's rim along the ground at the burst (plan 25 · YD1), <paramref name="k"/>
    /// seconds after it, gone by 0.7 s.
    /// </summary>
    private static void GroundDust(int seed, float k, Vector3 feet, float h, float w, FxList fx)
    {
        if (k < 0f || k >= 0.7f) return;
        float e = Fx.EaseOut(k / 0.7f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.4f + Fx.Rand(seed, i, 471) * 0.5f;
            float r = (w + h * 0.3f) + h * 0.9f * e;
            var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.05f + 0.06f * e), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Smoke, p, h * (0.14f + 0.12f * e), Fx.Fade(Rgb(200, 180, 170), 0.6f * (1f - k / 0.7f)), FxBlend.Alpha, a + k * 2f,
                depth: MathF.Sin(a) < 0f);
        }
    }

    /// <summary>
    /// The ball a trainer threw opening over the place (plan 25 · YD1): red light bursting out of it, the Pokémon rising
    /// out of the light (its size is the animator's <see cref="BattleAnimator.CalledBack"/>). 3 quads, gone by 0.35 s.
    /// </summary>
    private static void OpenedBall(in FxCtx c, float t, FxList fx)
    {
        if (t >= 0.35f) return;
        float k = t / 0.35f, h = c.FromSize;
        var at = c.FromFeet + Vector3.UnitY * h * 0.45f;
        fx.Sprite(FxShape.Glow, at, h * (0.5f + 0.6f * k), Fx.Fade(DynaRed, 0.85f * (1f - k)));
        fx.Sprite(FxShape.Glow, at, h * (0.25f + 0.3f * k), Fx.Fade(DynaRim, 0.9f * (1f - k)));
        Fx.Shockwave(fx, t, at, Vector3.Zero, 0f, 0.35f, h * 0.9f, Fx.Fade(DynaPink, 0.8f));
    }

    /// <summary>
    /// A Dynamax's end, the fall (plan 25 · YD3; style guide, "Its end, the fall"): while it shudders red light bleeds out
    /// of it in eight streams and the wreath's clouds are drawn in to it and up; as it falls in they are thrown out off
    /// the platform as dust, a pink ring on the ground and red motes going out. A giant that fainted (<see cref="EffectCue.Fell"/>)
    /// sinks into its clouds instead: they close over its feet and are thrown out as it goes. At most 33 quads.
    /// </summary>
    private static void DynamaxShrink(in FxCtx c, FxList fx)
    {
        float t = c.Age, h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        const float S = BattleAnimator.DynamaxFallStart, D = BattleAnimator.DynamaxShrunk;
        float fall = c.Cue.Fell ? 0f : S;
        fx.Land(c.Cue.Fell ? BattleAnimator.GiantFallTime : D);
        float rx = w + h * 0.12f;
        // The wreath's clouds: drawn in and up the body while it shudders (or closing over a falling giant's feet)
        float draw = Math.Clamp(t / Math.Max(0.2f, fall + 0.15f), 0f, 1f);
        if (t < fall + 0.15f || c.Cue.Fell && t < BattleAnimator.GiantFallTime)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + t * 2.5f;
                float r = c.Cue.Fell ? rx * (1f - 0.3f * draw) : rx * (1f - 0.75f * draw);
                float y = c.Cue.Fell ? h * (0.07f + 0.1f * draw) : h * (0.07f + 0.45f * draw);
                var p = feet + new Vector3(MathF.Cos(a) * r, y, MathF.Sin(a) * r * 0.6f);
                fx.Sprite(FxShape.Smoke, p, h * 0.2f * (1f + (c.Cue.Fell ? 0.4f * draw : -0.4f * draw)), Fx.Fade(CloudRed, 0.7f * (1f - 0.5f * draw)), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
            }
        // Red light bleeding out of it in streams while it shudders
        if (!c.Cue.Fell && t < S + 0.1f)
        {
            float e = Fx.Envelope(t, 0f, 0.06f, S - 0.05f, S + 0.1f);
            for (int i = 0; i < 8; i++)
            {
                float u = (t * 2.4f + Fx.Rand(c.Seed, i, 441)) % 1f;
                float a = i * MathF.Tau / 8f + 0.3f;
                var p = feet + new Vector3(MathF.Cos(a) * w * 0.8f, h * (0.25f + 0.85f * u), MathF.Sin(a) * w * 0.5f);
                fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.06f, 3f, Fx.Fade(i % 2 == 0 ? DynaRed : DynaPink, e * MathF.Sin(u * MathF.PI)), depth: MathF.Sin(a) < 0f);
            }
        }
        // The collapse: the clouds thrown out as dust, a ring on the ground, red motes going out
        float k = t - fall;
        GroundDust(c.Seed, k, feet, h, w, fx);
        Fx.Shockwave(fx, t, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, fall + 0.1f, 0.55f, h * 1.6f, Fx.Fade(DynaPink, 0.75f));
        for (int i = 0; i < 10; i++)
        {
            float m = (k - 0.05f * Fx.Rand(c.Seed, i, 421) * 4f) / 0.5f;
            if (m < 0f || m > 1f) continue;
            float a = Fx.Rand(c.Seed, i, 422) * MathF.Tau;
            var p = feet + new Vector3(MathF.Cos(a) * w, h * (0.3f + 0.9f * m), MathF.Sin(a) * w * 0.6f);
            fx.Sprite(c.Cue.Gigantamax && i % 3 == 0 ? FxShape.Spark : FxShape.Glow, p, h * 0.05f, Fx.Fade(i % 2 == 0 ? DynaRed : DynaPink, MathF.Sin(m * MathF.PI)),
                depth: MathF.Sin(a) < 0f);
        }
    }

    // ------------------------------------------------------------------ Terastallization

    /// <summary>
    /// A Terastallization: facets of crystal fly in and lock round the body on three rings, the body takes the Tera
    /// Type's light; at 0.9 s the shell shatters, a ring and a flash; the crown rises over the head and stays.
    /// </summary>
    private static void Terastallization(in FxCtx c, FxList fx)
    {
        float t = c.Age, h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        var middle = c.From;
        const float S = BattleAnimator.TeraSwapTime;
        bool stellar = c.Cue.Stellar;
        var col = TeraColours(stellar, c.Cue.Type, t * 0.6f);
        fx.Land(S);
        float[] heights = { 0.25f, 0.55f, 0.85f };
        int[] counts = { 8, 7, 5 };
        int n = 0;
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < counts[ring]; i++, n++)
            {
                float a = i * MathF.Tau / counts[ring] + ring * 0.4f;
                var dir = new Vector3(MathF.Cos(a), 0f, MathF.Sin(a) * 0.6f);
                float rest = w * (1f - 0.25f * MathF.Abs(heights[ring] - 0.55f)) + h * 0.06f;
                var colour = stellar ? StellarColour(n / 7f + t * 0.3f) : (n % 2 == 0 ? col.Light : col.Main);
                if (t < S)
                {
                    // Flying in, each at its own moment, and locked on
                    float arrive = 0.1f + 0.5f * Fx.Rand(c.Seed, n, 431);
                    float k = (t - arrive) / 0.25f;
                    if (k < 0f) continue;
                    float e = Fx.EaseOut(k);
                    var p = feet + Vector3.UnitY * h * heights[ring] + dir * (rest + w * 0.8f * (1f - e));
                    bool behind = MathF.Sin(a) < 0f;
                    fx.Sprite(FxShape.Hex, p, h * 0.14f, Fx.Fade(colour, 0.75f * Math.Min(1f, k * 2f)), FxBlend.Alpha, a, depth: behind);
                    if (n % 2 == 0) fx.Sprite(FxShape.Shard, p, h * 0.08f, Fx.Fade(White, 0.7f * Math.Min(1f, k * 2f)), rotation: a + t * 2f, depth: behind);
                }
                else
                {
                    // Shattered: thrown out and spinning away
                    float k = (t - S) / 0.45f;
                    if (k > 1f) continue;
                    var p = feet + Vector3.UnitY * h * (heights[ring] + 0.25f * k) + dir * (rest + h * 0.9f * Fx.EaseOut(k));
                    fx.Sprite(FxShape.Shard, p, h * 0.12f * (1f - 0.4f * k), Fx.Fade(colour, 1f - k * k), FxBlend.Alpha, a + k * 8f);
                }
            }

        // The ring and the flash as it shatters
        Fx.Shockwave(fx, t, middle, Vector3.Zero, S, 0.5f, h * 1.4f, Fx.Fade(col.Light, 0.9f));
        Fx.Shockwave(fx, t, middle, Vector3.Zero, S + 0.05f, 0.45f, h * 1.1f, Fx.Fade(col.Main, 0.8f), FxShape.Hex);
        fx.ScreenFlash(col.Light, 0.25f * Fx.Envelope(t, S - 0.02f, S + 0.03f, S + 0.06f, S + 0.25f));

        // The crown rising into place and held, giving way to the one that lasts as the cue ends
        float crown = Fx.Envelope(t, S, S + 0.15f, BattleAnimator.TeraTime - BattleAnimator.ConditionReturnTime, BattleAnimator.TeraTime);
        if (crown > 0f)
        {
            float rise = 1f - Fx.EaseOut(Math.Clamp((t - S) / 0.25f, 0f, 1f));
            Crown(feet, h, col, t, stellar, crown, rise, fx);
            if (t < S + 0.45f) fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 1.12f, h * 0.3f, Fx.Fade(col.Light, crown * 0.5f * (1f - (t - S) / 0.45f)));
        }
    }
}
