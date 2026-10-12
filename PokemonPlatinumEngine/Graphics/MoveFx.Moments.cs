using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// The turn's moments (plan 25 · Y3; style guide, "Marks on a Pokémon"): what the end of a turn and the checks before
// a move do to a Pokémon, each a mark of its own on the Pokémon in the cue's "to" place. The list is the original's
// battle animations that are not moves, and every look is our own. Each is a pure function of the cue's age and
// seed, lands at the impact (0.35 s, when its HP begins to move), ends within BattleAnimator.MomentTime and draws at
// most MomentCap quads. Sizes are in the Pokémon's height.
internal static partial class MoveFx
{
    /// <summary>The most quads and ribbons a moment draws at any age: a test holds every moment under it.</summary>
    public const int MomentCap = 40;

    private const float M = BattleAnimator.MomentTime;

    private static void Moment(in FxCtx c, FxList fx)
    {
        switch (c.Cue.Moment)
        {
            case BattleMoment.Asleep: Asleep(c, fx); break;
            case BattleMoment.Poisoned: Poisoned(c, fx); break;
            case BattleMoment.Burned: Burned(c, fx); break;
            case BattleMoment.Frozen: Frozen(c, fx); break;
            case BattleMoment.Paralyzed: Paralyzed(c, fx); break;
            case BattleMoment.Confused: ConfusedStars(c, fx); break;
            case BattleMoment.Infatuated: Infatuated(c, fx); break;
            case BattleMoment.LevelUp: LevelUp(c, fx); break;
            case BattleMoment.BagItem: BagItem(c, fx); break;
            case BattleMoment.EscapeItem: EscapeItem(c, fx); break;
            case BattleMoment.Happy: Happy(c, fx); break;
            case BattleMoment.Eating: Eating(c, fx); break;
            case BattleMoment.Angry: Angry(c, fx); break;
            case BattleMoment.Curse: Curse(c, fx); break;
            case BattleMoment.Nightmare: Nightmare(c, fx); break;
            case BattleMoment.LeechSeed: LeechSeed(c, fx); break;
            case BattleMoment.Ingrain: Ingrain(c, fx); break;
            case BattleMoment.Bind: Bands(c, fx, 2, Rgb(214, 186, 140), Rgb(250, 232, 190), 0f); break;
            case BattleMoment.Wrap: Bands(c, fx, 3, Rgb(120, 170, 80), Rgb(196, 232, 130), 0.34f); break;
            case BattleMoment.FireSpin: FireSpinHurt(c, fx); break;
            case BattleMoment.MagmaStorm: MagmaStorm(c, fx); break;
            case BattleMoment.Clamp: Clamp(c, fx); break;
            case BattleMoment.Whirlpool: Whirlpool(c, fx); break;
            case BattleMoment.SandTomb: SandTomb(c, fx); break;
            case BattleMoment.HeldItem: HeldItem(c, fx); break;
        }
    }

    /// <summary>The glow a moment that bites lays over its Pokémon: up to the impact, and away.</summary>
    private static void Bite(in FxCtx c, FxList fx, Color color, float strength = 0.38f) =>
        fx.Sprite(FxShape.Glow, c.To, c.ToSize * 0.85f, Fx.Fade(color, strength * Fx.Envelope(c.Age, 0.05f, I, I + 0.15f, 0.9f)));

    // ------------------------------------------------------------------ as each plays on its turn

    private static void Asleep(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        // A slow breath of pale light at its head, and three Z's drifting up out of it
        fx.Sprite(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.8f, h * 0.55f,
            Fx.Fade(Rgb(190, 210, 255), 0.35f * MathF.Sin(Math.Clamp(c.Age / M, 0f, 1f) * MathF.PI)));
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.04f - i * 0.22f) / 0.6f;
            if (k < 0f || k > 1f) continue;
            var p = c.ToFeet + new Vector3(h * (0.22f + 0.42f * k), h * (0.9f + 0.55f * k), 0);
            fx.Sprite(FxShape.Zzz, p, h * (0.1f + 0.1f * k), Fx.Fade(Rgb(210, 226, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.25f - 0.1f * i);
        }
    }

    private static void Poisoned(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var poison = ColorsOf(PokemonType.Poison);
        // A purple cast over it as the poison bites: matter, since light alone would only brighten it
        fx.Sprite(FxShape.Glow, c.To, h * 0.8f, Fx.Fade(poison.Main, 0.55f * Fx.Envelope(c.Age, 0.05f, I, I + 0.15f, 0.9f)), FxBlend.Alpha);
        // Bubbles rise through its body and burst into rings about as it does
        for (int i = 0; i < 9; i++)
        {
            float start = 0.02f * i;
            float pop = I + 0.035f * (i % 4);
            float x = Fx.Signed(c.Seed, i, 201) * h * 0.32f, z = Fx.Signed(c.Seed, i, 202) * h * 0.2f;
            float top = h * (0.35f + 0.5f * Fx.Rand(c.Seed, i, 203));
            float size = h * (0.07f + 0.04f * Fx.Rand(c.Seed, i, 204));
            float k = (c.Age - start) / (pop - start);
            if (k < 0f) continue;
            if (k < 1f)
            {
                var p = c.ToFeet + new Vector3(x + MathF.Sin(c.Age * 9f + i) * h * 0.03f, h * 0.05f + top * k, z);
                fx.Sprite(FxShape.Bubble, p, size * (0.6f + 0.4f * k), Fx.Fade(Fx.Mix(poison.Main, poison.Light, Fx.Rand(c.Seed, i, 205)), Math.Min(1f, k * 5f)), FxBlend.Alpha);
                continue;
            }
            float r = (c.Age - pop) / 0.3f;
            if (r <= 1f) fx.Sprite(FxShape.Ring, c.ToFeet + new Vector3(x, h * 0.05f + top, z), size * (0.9f + 0.7f * Fx.EaseOut(r)), Fx.Fade(poison.Light, (1f - r) * 0.9f), FxBlend.Alpha);
        }
    }

    private static void Burned(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.7f, 1.0f);
        // Flames lick up its body, each climbing and starting again; embers are thrown off as the burn bites
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 1.6f + Fx.Rand(c.Seed, i, 211)) % 1f;
            float flicker = 0.75f + 0.25f * MathF.Sin(c.Age * 24f + i * 2.1f);
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 212) * h * 0.42f, h * (0.08f + 0.75f * u), 0.06f + Fx.Signed(c.Seed, i, 213) * h * 0.15f);
            fx.Sprite(FxShape.Flame, p, h * 0.15f * flicker * (1f - 0.4f * u), Fx.Fade(i % 2 == 0 ? fire.Main : fire.Light, env * (1f - u * u)));
        }
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.55f, h, I, 0.5f, 8, FxShape.Ember, fire.Light, fire.Main, 0.06f, 0.7f, -0.8f, salt: 21);
        Bite(c, fx, fire.Main);
    }

    private static void Frozen(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var ice = ColorsOf(PokemonType.Ice);
        float close = Fx.EaseOut(c.Age / I);
        float fade = Fx.Envelope(c.Age, 0f, 0.1f, 0.75f, 1.0f);
        // A cold cast over it as the ice locks
        fx.Sprite(FxShape.Glow, c.To, h * 0.85f, Fx.Fade(ice.Main, 0.5f * Fx.Envelope(c.Age, I - 0.15f, I, 0.7f, 0.95f)), FxBlend.Alpha);
        // Shards close in round it and lock: those on the camera's side stand over its body, the rest behind it
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.3f;
            float rad = h * (0.95f - 0.45f * close);
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * rad, h * (0.2f + 0.55f * (i * 3 % 8) / 8f), MathF.Sin(a) * rad * 0.45f);
            fx.Sprite(FxShape.Shard, p, h * 0.17f, Fx.Fade(i % 2 == 0 ? ice.Main : ice.Light, fade), FxBlend.Alpha, MathF.Cos(a) * 0.4f, depth: MathF.Sin(a) < 0f);
        }
        Fx.Glint(fx, c.Age, c.ToFeet + Vector3.UnitY * h * 0.7f, h, I, White, 0.32f);
    }

    private static void Paralyzed(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var electric = ColorsOf(PokemonType.Electric);
        // Three fits of bolts across its body, sparks jumping off each
        for (int fit = 0; fit < 3; fit++)
        {
            float t0 = fit * 0.28f;
            for (int k = 0; k < 2; k++)
            {
                var a = c.To + Fx.Direction(c.Seed, 220 + fit * 4 + k) * h * 0.5f;
                var b = c.To + Fx.Direction(c.Seed, 230 + fit * 4 + k) * h * 0.5f;
                Fx.Bolt(fx, c.Age, c.Seed + fit * 29 + k * 11, a, b, t0, t0 + 0.22f, h * 0.03f, electric.Main, electric.Light, 0);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, t0 + 0.02f, 0.3f, 4, FxShape.Spark, electric.Main, electric.Light, 0.07f, 0.8f, salt: 30 + fit);
        }
        Bite(c, fx, electric.Main, 0.25f);
    }

    private static void ConfusedStars(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var over = c.ToFeet + Vector3.UnitY * h * 1.05f;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.05f);
        // Three stars circling over its head under a turning swirl
        for (int i = 0; i < 3; i++)
        {
            float a = c.Age * 5.5f + i * MathF.Tau / 3f;
            var p = over + new Vector3(MathF.Cos(a) * h * 0.38f, MathF.Sin(a * 2f) * h * 0.04f, MathF.Sin(a) * h * 0.16f);
            fx.Sprite(FxShape.Glow, p, h * 0.14f, Fx.Fade(Rgb(255, 240, 160), env * 0.6f));
            fx.Sprite(FxShape.Star, p, h * 0.1f, Fx.Fade(Rgb(255, 226, 90), env), FxBlend.Alpha, a);
        }
        var swirl = over + Vector3.UnitY * h * 0.16f;
        fx.Sprite(FxShape.Swirl, swirl, h * 0.24f, Fx.Fade(Rgb(255, 244, 190), env), FxBlend.Alpha, -c.Age * 7f);
        fx.Sprite(FxShape.Swirl, swirl, h * 0.24f, Fx.Fade(Rgb(255, 236, 150), env * 0.7f), rotation: -c.Age * 7f);
    }

    private static void Infatuated(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        // Hearts rising from its head one after another, swaying
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.03f - i * 0.16f) / 0.58f;
            if (k < 0f || k > 1f) continue;
            float side = (i % 2 == 0 ? -1f : 1f) * (0.12f + 0.1f * Fx.Rand(c.Seed, i, 241));
            var p = c.ToFeet + new Vector3(h * (side + 0.07f * MathF.Sin(k * 7f + i)), h * (0.85f + 0.6f * k), 0);
            fx.Sprite(FxShape.Heart, p, h * (0.1f + 0.06f * MathF.Sin(k * MathF.PI)),
                Fx.Fade(i % 2 == 0 ? Rgb(255, 120, 170) : Rgb(255, 170, 205), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.15f * MathF.Sin(k * 6f));
        }
        fx.Sprite(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.85f, h * 0.5f, Fx.Fade(Rgb(255, 150, 190), 0.3f * Fx.Envelope(c.Age, 0f, 0.15f, 0.7f, 1.0f)));
    }

    // ------------------------------------------------------------------ a level, the bag, the way out, a held item

    private static void LevelUp(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var gold = Rgb(255, 232, 140);
        var pale = Rgb(255, 250, 220);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        // Gold light rising round it: nothing over the screen, since the level's panel stands over the line
        for (int i = 0; i < 10; i++)
        {
            float u = (c.Age * 1.5f + Fx.Rand(c.Seed, i, 251)) % 1f;
            float a = Fx.Rand(c.Seed, i, 252) * MathF.Tau;
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.05f + 1.15f * u), MathF.Sin(a) * h * 0.3f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.045f, 6f, Fx.Fade(Fx.Mix(gold, pale, Fx.Rand(c.Seed, i, 253)), env * MathF.Sin(u * MathF.PI)));
        }
        // A ring climbing from its feet to its head
        float climb = (c.Age - 0.05f) / 0.6f;
        if (climb >= 0f && climb <= 1f)
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * h * climb, Vector3.UnitY, h * 0.6f, Fx.Fade(gold, MathF.Sin(climb * MathF.PI)));
        // Three stars popping over it
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - I - i * 0.12f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            var p = c.ToFeet + new Vector3((i - 1) * h * 0.3f, h * (1.1f + (i == 1 ? 0.12f : 0f) + 0.1f * Fx.EaseOut(k)), 0);
            fx.Sprite(FxShape.Spark, p, h * 0.16f * MathF.Sin(k * MathF.PI), Fx.Fade(pale, 0.8f), rotation: k);
            fx.Sprite(FxShape.Star, p, h * 0.11f * MathF.Sin(k * MathF.PI), gold, FxBlend.Alpha, k * 1.5f);
        }
        fx.Sprite(FxShape.Glow, c.To, h * 0.8f, Fx.Fade(gold, env * 0.28f));
    }

    private static void BagItem(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var pale = Rgb(214, 244, 255);
        // A glint over its head, pale motes falling through it, a ring where they land
        Fx.Glint(fx, c.Age, c.ToFeet + Vector3.UnitY * h * 1.15f, h, 0.03f, White, 0.4f);
        for (int i = 0; i < 10; i++)
        {
            float k = (c.Age - 0.12f - 0.04f * i) / 0.45f;
            if (k < 0f || k > 1f) continue;
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 261) * h * 0.4f, h * (1.1f - 1.0f * k * k), Fx.Signed(c.Seed, i, 262) * h * 0.2f);
            fx.Sprite(i % 3 == 0 ? FxShape.Spark : FxShape.Glow, p, h * 0.07f, Fx.Fade(Fx.Mix(pale, White, Fx.Rand(c.Seed, i, 263)), Math.Min(1f, (1f - k) * 4f)));
        }
        Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.45f, h * 0.7f, Fx.Fade(pale, 0.8f));
    }

    private static void EscapeItem(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        // A burst of smoke at its feet to slip away behind, a second puff rolling up through it, and a few stars
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.25f, h, 0.02f, 0.95f, 12, FxShape.Smoke, Rgb(232, 230, 226), Rgb(190, 188, 186), 0.44f, 0.8f, -0.25f, FxBlend.Alpha, salt: 41);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, 0.12f, 0.85f, 8, FxShape.Smoke, Rgb(244, 242, 238), Rgb(206, 204, 200), 0.36f, 0.55f, -0.5f, FxBlend.Alpha, salt: 43);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, 0.05f, 0.6f, 5, FxShape.Star, Rgb(255, 240, 160), White, 0.07f, 1.1f, 0.6f, FxBlend.Alpha, spin: 5f, salt: 42);
    }

    private static void HeldItem(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var over = c.ToFeet + Vector3.UnitY * h * 1.1f;
        if (c.Cue.Berry)
        {
            // A small round fruit with a leaf pops up over its head and is bitten away into crumbs
            float pop = Fx.EaseOut(c.Age / 0.18f);
            float eaten = Math.Clamp((c.Age - I) / 0.3f, 0f, 1f);
            float size = h * 0.16f * pop * (1f - eaten);
            var p = over + Vector3.UnitY * h * 0.06f * MathF.Sin(Math.Clamp(c.Age / 0.3f, 0f, 1f) * MathF.PI);
            // (The leaf is a dark green, to show against the trees behind the far platform)
            fx.Sprite(FxShape.Leaf, p + new Vector3(size * 0.5f, size * 1.0f, 0), size * 0.8f, Rgb(40, 132, 52), FxBlend.Alpha, -0.7f);
            fx.Sprite(FxShape.Dot, p, size * 1.3f, Rgb(236, 84, 110), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p + new Vector3(-size * 0.28f, size * 0.3f, 0), size * 0.45f, Fx.Fade(White, 0.75f));
            Fx.Burst(fx, c.Seed, c.Age, over, h, I, 0.45f, 5, FxShape.Dot, Rgb(236, 84, 110), Rgb(255, 190, 200), 0.03f, 0.4f, 1.8f, FxBlend.Alpha, salt: 91);
            Fx.Glint(fx, c.Age, over, h, I + 0.1f, Rgb(255, 240, 220), 0.3f);
            return;
        }
        // Anything else: a gold ring closing on a glint
        float k = Fx.EaseOut(c.Age / 0.4f);
        float fade = Fx.Envelope(c.Age, 0f, 0.08f, 0.5f, 0.8f);
        fx.Sprite(FxShape.Ring, over, h * (0.3f - 0.18f * k), Fx.Fade(Rgb(236, 190, 90), fade * 0.8f), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, over, h * (0.28f - 0.18f * k), Fx.Fade(Rgb(255, 226, 130), fade));
        Fx.Glint(fx, c.Age, over, h, I - 0.1f, Rgb(255, 250, 210), 0.36f);
        Fx.Glint(fx, c.Age, over + new Vector3(h * 0.1f, h * 0.07f, 0), h, I + 0.1f, White, 0.2f);
    }

    // ------------------------------------------------------------------ the Great Marsh

    private static void Happy(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        // Notes hopping up from its head, and two sparkles
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.04f - i * 0.18f) / 0.55f;
            if (k < 0f || k > 1f) continue;
            float hop = MathF.Abs(MathF.Sin(k * MathF.Tau));
            var p = c.ToFeet + new Vector3(h * (i - 1) * 0.34f, h * (0.95f + 0.25f * k + 0.1f * hop), 0);
            float fade = Math.Min(1f, (1f - k) * 3f);
            // A halo behind each, so a yellow note stands out against leaves
            fx.Sprite(FxShape.Glow, p, h * 0.2f, Fx.Fade(Rgb(255, 250, 190), fade * 0.55f));
            fx.Sprite(FxShape.Note, p, h * 0.15f, Fx.Fade(i % 2 == 0 ? Rgb(255, 232, 96) : Rgb(190, 240, 110), fade), FxBlend.Alpha, 0.2f * MathF.Sin(k * 9f + i));
        }
        Fx.Glint(fx, c.Age, c.ToFeet + new Vector3(-h * 0.4f, h * 0.9f, 0), h, 0.15f, Rgb(255, 250, 200), 0.3f);
        Fx.Glint(fx, c.Age, c.ToFeet + new Vector3(h * 0.42f, h * 0.75f, 0), h, 0.45f, Rgb(255, 250, 200), 0.3f);
    }

    private static void Eating(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        // Over its head, where the held berry shows too: before its own body a morsel is lost on a brown one, and
        // nobody can say where a body's mouth is
        var over = c.ToFeet + Vector3.UnitY * h * 1.12f;
        const float First = 0.06f, Apart = 0.3f, Bite = 0.22f;
        // A morsel of bait, a third of it gone with each of three bites
        float left = 1f;
        for (int bite = 0; bite < 3; bite++)
            left -= Fx.EaseOut((c.Age - First - bite * Apart) / Bite * 3f) / 3f;
        float morsel = h * 0.2f * Fx.EaseOut(c.Age / 0.08f) * Math.Max(0f, left);
        if (morsel > h * 0.004f)
        {
            fx.Sprite(FxShape.Dot, over, morsel * 1.18f, Rgb(140, 96, 56), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, over, morsel, Rgb(222, 176, 112), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, over + new Vector3(-morsel * 0.25f, morsel * 0.25f, 0), morsel * 0.4f, Fx.Fade(White, 0.6f));
        }
        // Each bite: a pale crescent snaps into it from one side and then the other, and crumbs fall
        for (int bite = 0; bite < 3; bite++)
        {
            float t0 = First + bite * Apart;
            Fx.Burst(fx, c.Seed, c.Age, over, h, t0 + 0.06f, 0.3f, 7, FxShape.Dot, Rgb(255, 240, 190), Rgb(240, 200, 130), 0.06f, 0.7f, 2.4f, FxBlend.Alpha, salt: 50 + bite);
            float k = (c.Age - t0) / Bite;
            if (k < 0f || k > 1f) continue;
            float side = bite % 2 == 0 ? 1f : -1f;
            // (The shape's arc lies three quarters of its half-width from its middle, on its right: turned half
            // round for a bite from the right, its bulge leads into the morsel)
            float radius = h * 0.13f;
            float reach = h * (0.13f - 0.17f * Fx.EaseOut(k * 2.5f));
            var at = over + Vector3.UnitX * side * (reach + radius);
            var mark = Fx.Fade(White, 1f - Math.Max(0f, k - 0.5f) / 0.5f);
            float turn = side > 0f ? MathF.PI : 0f;
            fx.Sprite(FxShape.Wave, at, radius / 0.75f, mark, FxBlend.Alpha, turn);
            fx.Sprite(FxShape.Wave, at, radius / 0.75f * 0.93f, mark, FxBlend.Alpha, turn);
            fx.Sprite(FxShape.Glow, over, h * 0.24f * MathF.Sin(k * MathF.PI), Fx.Fade(Rgb(255, 240, 200), 0.35f));
        }
    }

    private static void Angry(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var head = c.ToFeet + Vector3.UnitY * h * 0.95f;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.8f, 1.0f);
        // A red cast over its face
        fx.Sprite(FxShape.Glow, c.ToFeet + Vector3.UnitY * h * 0.6f, h * 0.7f, Fx.Fade(Rgb(236, 60, 44), 0.38f * env), FxBlend.Alpha);
        // Steam off its head in three puffs to either side
        for (int puff = 0; puff < 3; puff++)
        {
            float k = (c.Age - 0.03f - puff * 0.26f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            for (int side = -1; side <= 1; side += 2)
            {
                var p = head + new Vector3(side * h * (0.24f + 0.34f * Fx.EaseOut(k)), h * (0.02f + 0.32f * Fx.EaseOut(k)), 0);
                fx.Sprite(FxShape.Smoke, p, h * (0.13f + 0.13f * k), Fx.Fade(White, 1f - k * k), FxBlend.Alpha, k * 2f * side);
            }
        }
        // The mark of a temper beside its head, beating: a jagged red burst, dark under bright so it reads on anything
        float beat = 1f + 0.22f * MathF.Sin(c.Age * 22f);
        var mark = head + new Vector3(h * 0.36f, h * 0.06f, 0.02f);
        fx.Sprite(FxShape.Impact, mark, h * 0.19f * beat, Fx.Fade(Rgb(130, 18, 18), env), FxBlend.Alpha, 0.3f);
        fx.Sprite(FxShape.Impact, mark, h * 0.15f * beat, Fx.Fade(Rgb(255, 66, 52), env), FxBlend.Alpha, 0.3f);
        fx.Sprite(FxShape.Cross, mark, h * 0.08f * beat, Fx.Fade(Rgb(255, 214, 200), env), FxBlend.Alpha);
    }

    // ------------------------------------------------------------------ what the turn's end takes or gives

    private static void Curse(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var ghost = ColorsOf(PokemonType.Ghost);
        var dark = Rgb(96, 60, 140);
        // A dark nail driven down into it from above: it strikes at the impact and stays in
        float fall = Math.Clamp(c.Age / I, 0f, 1f);
        float hold = Fx.Envelope(c.Age, 0f, 0.06f, 0.7f, 0.95f);
        var nail = c.ToFeet + new Vector3(0, h * (0.75f + 1.6f * (1f - fall * fall)), 0.02f);
        fx.Sprite(FxShape.Glow, nail, h * 0.3f, Fx.Fade(ghost.Main, hold * 0.6f));
        fx.Sprite(FxShape.Shard, nail, h * 0.2f, Fx.Fade(dark, hold), FxBlend.Alpha, MathF.PI);
        fx.Sprite(FxShape.Glow, c.To, h * 0.9f, Fx.Fade(Rgb(60, 36, 110), 0.5f * Fx.Envelope(c.Age, I - 0.05f, I + 0.05f, 0.6f, 0.95f)), FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Wisp, ghost.Main, ghost.Light, 0.12f, 0.6f, -0.5f, salt: 61);
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 0.9f, Fx.Fade(ghost.Light, 0.9f));
    }

    private static void Nightmare(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var head = c.ToFeet + Vector3.UnitY * h * 0.95f;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.75f, 1.05f);
        float close = Fx.EaseOut(c.Age / I);
        // A dark cloud over its head, wisps circling it and closing in
        fx.Sprite(FxShape.Smoke, head + Vector3.UnitY * h * 0.14f, h * 0.5f, Fx.Fade(Rgb(34, 22, 52), env), FxBlend.Alpha, c.Age * 0.6f);
        fx.Sprite(FxShape.Smoke, head + new Vector3(h * 0.2f, h * 0.24f, 0), h * 0.36f, Fx.Fade(Rgb(58, 38, 84), env * 0.95f), FxBlend.Alpha, -c.Age * 0.8f);
        fx.Sprite(FxShape.Smoke, head + new Vector3(-h * 0.22f, h * 0.2f, 0), h * 0.32f, Fx.Fade(Rgb(46, 30, 70), env * 0.95f), FxBlend.Alpha, c.Age * 0.5f);
        for (int i = 0; i < 6; i++)
        {
            float a = c.Age * 4.5f + i * MathF.Tau / 6f;
            float rad = h * (0.7f - 0.35f * close);
            var p = head + new Vector3(MathF.Cos(a) * rad, MathF.Sin(a * 1.5f) * h * 0.08f, MathF.Sin(a) * rad * 0.4f);
            fx.Sprite(FxShape.Wisp, p, h * 0.13f, Fx.Fade(i % 2 == 0 ? Rgb(150, 110, 230) : Rgb(96, 62, 160), env), rotation: a);
        }
        // Two red points of light in the cloud as it bites
        float eyes = Fx.Envelope(c.Age, I - 0.06f, I, I + 0.3f, I + 0.5f);
        for (int side = -1; side <= 1; side += 2)
        {
            var eye = head + new Vector3(side * h * 0.11f, h * 0.16f, 0.05f);
            fx.Sprite(FxShape.Dot, eye, h * 0.045f, Fx.Fade(Rgb(255, 70, 60), eyes), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, eye, h * 0.1f, Fx.Fade(Rgb(255, 60, 60), eyes * 0.8f));
        }
    }

    private static void LeechSeed(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var grass = ColorsOf(PokemonType.Grass);
        // Leaves sprouting at the feet of the seeded Pokémon
        for (int i = 0; i < 3; i++)
        {
            float grow = Fx.EaseOut((c.Age - i * 0.05f) / 0.25f);
            float fade = Fx.Envelope(c.Age, i * 0.05f, i * 0.05f + 0.1f, 0.8f, 1.05f);
            float a = (i - 1) * 0.9f;
            var p = c.ToFeet + new Vector3(MathF.Sin(a) * h * 0.32f, h * 0.1f * grow, MathF.Cos(a) * h * 0.14f + 0.03f);
            fx.Sprite(FxShape.Leaf, p, h * 0.12f * grow, Fx.Fade(i == 1 ? grass.Light : grass.Main, fade), FxBlend.Alpha, a * 0.6f);
        }
        Bite(c, fx, grass.Main, 0.35f);

        // Its health leaves it in green motes, one after another, for the place the seed was sown from: in a low arc
        // to whoever stands there, or up and away where both ends are the same. Each is a pale core in a green halo,
        // so it reads over grass and leaves
        var mote = Rgb(120, 255, 110);
        var pale = Rgb(236, 255, 214);
        var to = c.Cue.OnSelf ? c.To + Vector3.UnitY * h * 1.6f : c.From;
        float dist = (to - c.To).Length();
        for (int i = 0; i < 12; i++)
        {
            float k = (c.Age - 0.1f - 0.038f * i) / 0.5f;
            if (k < 0f || k > 1f) continue;
            // Straight off toward the other end, in an arc low enough to cross the field between the two and not the sky
            var start = c.To + new Vector3(Fx.Signed(c.Seed, i, 271) * h * 0.3f, Fx.Signed(c.Seed, i, 272) * h * 0.3f, 0);
            var p = Vector3.Lerp(start, to, k) + Vector3.UnitY * dist * 0.2f * k * (1f - k);
            float size = (c.ToSize + (c.FromSize - c.ToSize) * k) * 0.085f;
            float fade = Math.Min(1f, k * 6f) * Math.Min(1f, (1f - k) * 5f);
            fx.Sprite(FxShape.Glow, p, size, Fx.Fade(mote, fade));
            fx.Sprite(FxShape.Dot, p, size * 0.34f, Fx.Fade(pale, fade), FxBlend.Alpha);
        }
        if (!c.Cue.OnSelf) Fx.Glint(fx, c.Age, to, c.FromSize, 0.72f, Rgb(200, 255, 190), 0.5f);
    }

    private static void Ingrain(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var root = Rgb(176, 130, 80);
        var deep = Rgb(110, 76, 44);
        var grass = ColorsOf(PokemonType.Grass);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.7f, 1.0f);
        // Six roots grow in over the ground from round it and up into its feet, and stay while it drinks
        float grown = Fx.EaseOut(c.Age / I);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.35f + Fx.Signed(c.Seed, i, 281) * 0.2f;
            var out3 = new Vector3(MathF.Cos(a), 0, MathF.Sin(a) * 0.6f);
            var side = new Vector3(-out3.Z, 0, out3.X) * Fx.Signed(c.Seed, i, 282) * 0.14f;
            var from = c.ToFeet + out3 * h * 0.85f + Vector3.UnitY * 0.03f;
            var bend = c.ToFeet + (out3 * 0.5f + side) * h + Vector3.UnitY * h * 0.05f;
            var knee = c.ToFeet + (out3 * 0.24f - side * 0.5f) * h + Vector3.UnitY * h * 0.16f;
            var into = c.ToFeet + out3 * h * 0.1f + Vector3.UnitY * h * 0.36f;
            // The root's tip as far as it has grown
            Vector3 Along(float t) => t < 1f / 3f ? Vector3.Lerp(from, bend, t * 3f) : t < 2f / 3f ? Vector3.Lerp(bend, knee, t * 3f - 1f) : Vector3.Lerp(knee, into, t * 3f - 2f);
            var points = new[] { from, Along(grown / 3f), Along(grown * 2f / 3f), Along(grown) };
            fx.Ribbon(points, h * 0.1f, Fx.Fade(i % 2 == 0 ? root : deep, env), FxBlend.Alpha);
            // What it draws in runs along the root to its feet
            float u = (c.Age * 1.6f + Fx.Rand(c.Seed, i, 284)) % 1f;
            if (c.Age > I * 0.6f) fx.Sprite(FxShape.Glow, Along(u), h * 0.06f, Fx.Fade(Rgb(200, 255, 160), env * MathF.Sin(u * MathF.PI)));
        }
        // Two rings close on it over the ground, and what it drew up rises through it in a green cast
        for (int r = 0; r < 2; r++)
        {
            float k = (c.Age - 0.05f - r * 0.3f) / 0.5f;
            if (k < 0f || k > 1f) continue;
            float size = h * (1.0f - 0.55f * k);
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, size, Fx.Fade(Rgb(214, 255, 150), MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, size, Fx.Fade(grass.Light, MathF.Sin(k * MathF.PI) * 0.8f));
        }
        fx.Sprite(FxShape.Glow, c.To, h * 0.8f, Fx.Fade(Rgb(150, 255, 140), 0.32f * Fx.Envelope(c.Age, I - 0.1f, I + 0.05f, 0.7f, 0.95f)));
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - I - i * 0.06f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 283) * h * 0.35f, h * (0.15f + 0.8f * k), 0);
            fx.Sprite(FxShape.Spark, p, h * 0.13f * MathF.Sin(k * MathF.PI), Rgb(214, 255, 190), rotation: k);
        }
    }

    // ------------------------------------------------------------------ the seven binds

    /// <summary>
    /// Bands round its body that tighten until the impact, hold a beat and let go: Bind's two, Wrap's three tilted
    /// coils. Each band is three arcs: the one on the camera's side is drawn over the body, the two behind it are
    /// hidden by it, so a band goes round a body of any width.
    /// </summary>
    private static void Bands(in FxCtx c, FxList fx, int count, Color color, Color light, float tilt)
    {
        float h = c.ToSize;
        float squeeze = c.Age < I ? Fx.EaseOut(c.Age / I) : c.Age < I + 0.3f ? 1f : 1f - Fx.EaseOut((c.Age - I - 0.3f) / 0.4f);
        float fade = Fx.Envelope(c.Age, 0f, 0.08f, 0.8f, 1.05f);
        for (int i = 0; i < count; i++)
        {
            float lean = tilt * (i % 2 == 0 ? 1f : -1f);
            // Each band dips toward the camera, or the battle's low lens would see it edge-on, as a thread
            var normal = Vector3.Normalize(new Vector3(MathF.Sin(lean), MathF.Cos(lean), 0.55f));
            var at = c.ToFeet + Vector3.UnitY * h * (0.36f + 0.36f * i / (count - 1));
            // The arc of the shape lies three quarters of the quad's half-width out
            float size = h * (0.78f - 0.3f * squeeze) / 0.75f;
            float front = TurnToward(normal, Vector3.UnitZ);
            for (int arc = 0; arc < 3; arc++)
            {
                // The first arc is the one on the camera's side of the field (+Z)
                float turn = front + arc * MathF.Tau / 3f;
                if (arc != 0)
                {
                    // Round a body narrower than the band the far side shows: fainter and unlit, so it reads as behind
                    fx.Flat(FxShape.Wave, at, normal, size, Fx.Fade(color, fade * 0.5f), FxBlend.Alpha, turn, depth: true);
                    continue;
                }
                fx.Flat(FxShape.Wave, at, normal, size, Fx.Fade(color, fade), FxBlend.Alpha, turn, depth: false);
                fx.Flat(FxShape.Wave, at, normal, size * 0.93f, Fx.Fade(color, fade), FxBlend.Alpha, turn, depth: false);
                fx.Flat(FxShape.Wave, at, normal, size * 1.04f, Fx.Fade(light, fade * 0.45f), rotation: turn, depth: false);
            }
        }
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 5, FxShape.Star, light, White, 0.07f, 0.9f, 0.6f, FxBlend.Alpha, spin: 5f, salt: 71);
    }

    /// <summary>
    /// The turn that points a flat quad's own right (where an arc's middle lies) along a direction of the world, as
    /// near as its plane lets it: the axes are the ones <see cref="FxRenderer"/> lays a flat quad out by.
    /// </summary>
    internal static float TurnToward(Vector3 normal, Vector3 toward)
    {
        var t1 = Vector3.Normalize(Vector3.Cross(normal, MathF.Abs(normal.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
        var t2 = Vector3.Cross(normal, t1);
        return MathF.Atan2(Vector3.Dot(toward, t2), Vector3.Dot(toward, t1));
    }

    /// <summary>
    /// Things whirling round its body and rising: those on the camera's side of it (+Z) are drawn over the body, the
    /// rest behind it, so they go round a body of any width.
    /// </summary>
    private static void Round(in FxCtx c, FxList fx, FxShape shape, int count, Color a, Color b, float radius, float size, float speed, FxBlend blend, float top = 0.85f)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathF.Tau / count + c.Age * speed;
            float y = (i * 5 % count / (float)count + c.Age * 0.5f) % 1f;
            var p = c.ToFeet + new Vector3(MathF.Cos(angle) * h * radius, h * (0.1f + top * y), MathF.Sin(angle) * h * radius * 0.6f);
            fx.Sprite(shape, p, h * size * (1f - 0.3f * y), Fx.Fade(i % 2 == 0 ? a : b, env * MathF.Sin(y * MathF.PI)), blend, depth: MathF.Sin(angle) < 0f);
        }
    }

    private static void FireSpinHurt(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        // A ring of fire on the ground under it, and flames whirling round it and rising
        fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 0.85f, Fx.Fade(fire.Main, env * 0.9f), rotation: c.Age * 3f);
        Round(c, fx, FxShape.Flame, 12, fire.Main, fire.Light, 0.66f, 0.24f, 6f, FxBlend.Add);
        Round(c, fx, FxShape.Flame, 6, fire.Dark, fire.Main, 0.6f, 0.2f, -4f, FxBlend.Alpha);
        Bite(c, fx, fire.Main);
    }

    private static void MagmaStorm(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        // A red glow on the ground, glowing rocks and flames whirling wide, embers thrown up as it bites
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.05f, Fx.Fade(Rgb(255, 70, 30), env * 0.6f));
        Round(c, fx, FxShape.Rock, 7, Rgb(255, 130, 60), Rgb(176, 56, 34), 0.82f, 0.15f, 4.5f, FxBlend.Alpha);
        Round(c, fx, FxShape.Flame, 8, Rgb(255, 96, 40), Rgb(255, 196, 96), 0.72f, 0.2f, -5.5f, FxBlend.Add);
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 0.55f, 8, FxShape.Ember, Rgb(255, 210, 120), Rgb(255, 110, 50), 0.07f, 1.0f, -1.2f, salt: 76);
        Bite(c, fx, Rgb(255, 80, 40));
    }

    private static void Clamp(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var shell = Rgb(186, 206, 226);
        // Two arcs of shell snap shut on it from either side, hold, and open again
        float shut = Fx.EaseOut(c.Age / I);
        float open = c.Age > I + 0.35f ? Fx.EaseOut((c.Age - I - 0.35f) / 0.35f) : 0f;
        float fade = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.05f);
        const float Half = 0.55f;
        // The arc of the shape stands three quarters of its half-width from its middle: the two meet round the body
        float reach = h * (0.42f + 0.7f * (1f - shut) + 0.5f * open) - h * Half * 0.75f;
        for (int side = -1; side <= 1; side += 2)
        {
            var p = c.To + new Vector3(side * reach, 0, 0.02f);
            float turn = side > 0 ? 0f : MathF.PI;
            fx.Sprite(FxShape.Wave, p, h * Half, Fx.Fade(shell, fade), FxBlend.Alpha, turn);
            fx.Sprite(FxShape.Wave, p, h * Half * 1.06f, Fx.Fade(White, fade * 0.4f), rotation: turn);
        }
        Fx.Contact(fx, c.Age, c.Seed, c.To, h, I, 0.22f, shell, White);
    }

    /// <summary>
    /// A whirl at its feet. The swirl lying on the ground is all but edge-on from the battle's low camera on the far
    /// platform, so two more stand up in it, facing the lens, low round its legs.
    /// </summary>
    private static void Vortex(in FxCtx c, FxList fx, Color main, Color light, float spin)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        fx.Flat(FxShape.Swirl, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.05f, Fx.Fade(main, env), FxBlend.Alpha, spin * c.Age);
        fx.Flat(FxShape.Swirl, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 0.9f, Fx.Fade(light, env * 0.7f), rotation: spin * c.Age + 1.6f);
        var low = c.ToFeet + new Vector3(0, h * 0.14f, 0.05f);
        fx.Sprite(FxShape.Swirl, low, h * 0.52f, Fx.Fade(main, env * 0.85f), FxBlend.Alpha, spin * c.Age);
        fx.Sprite(FxShape.Swirl, low, h * 0.52f, Fx.Fade(light, env * 0.6f), rotation: spin * c.Age);
        fx.Sprite(FxShape.Swirl, low + Vector3.UnitY * h * 0.08f, h * 0.36f, Fx.Fade(light, env * 0.8f), FxBlend.Alpha, spin * c.Age + 2.2f);
    }

    private static void Whirlpool(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var water = ColorsOf(PokemonType.Water);
        // A swirl of water turning under it, drops running round its body, a splash as it bites
        Vortex(c, fx, water.Main, water.Light, -6f);
        Round(c, fx, FxShape.Drop, 10, water.Main, water.Light, 0.66f, 0.12f, 5.5f, FxBlend.Alpha, 0.6f);
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 0.4f, 6, FxShape.Drop, water.Light, White, 0.06f, 0.8f, 2f, FxBlend.Alpha, salt: 81);
    }

    private static void SandTomb(in FxCtx c, FxList fx)
    {
        float h = c.ToSize;
        var sand = Rgb(236, 212, 150);
        var dark = Rgb(190, 158, 104);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.75f, 1.05f);
        // A swirl of sand draws grains in and down round its legs, under low dust
        Vortex(c, fx, dark, sand, 5f);
        for (int i = 0; i < 14; i++)
        {
            float u = (c.Age * 1.2f + Fx.Rand(c.Seed, i, 291)) % 1f;
            float angle = Fx.Rand(c.Seed, i, 292) * MathF.Tau + u * 5f;
            float rad = h * 0.8f * (1f - 0.6f * u);
            var p = c.ToFeet + new Vector3(MathF.Cos(angle) * rad, h * 0.5f * (1f - u), MathF.Sin(angle) * rad * 0.6f);
            fx.Sprite(FxShape.Dot, p, h * 0.045f, Fx.Fade(Fx.Mix(sand, dark, Fx.Rand(c.Seed, i, 293)), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, depth: MathF.Sin(angle) < 0f);
        }
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + c.Age * 1.5f;
            var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * 0.14f, MathF.Sin(a) * h * 0.34f);
            fx.Sprite(FxShape.Smoke, p, h * 0.26f, Fx.Fade(sand, env * 0.75f), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
        }
    }

    // ------------------------------------------------------------------ an attack sent ahead, landing

    /// <summary>
    /// The landing half of an attack sent ahead (Future Sight, Doom Desire): a glint in the place it was sent from,
    /// then Doom Desire's pillar of light out of the sky, or rings of the move's type falling onto the target from
    /// that side and closing on it; then the move's own landing.
    /// </summary>
    private static void Landing(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Glint(fx, c.Age, c.From, c.FromSize, 0f, col.Light, 0.5f);
        var lean = c.From - c.To;
        lean.Y = 0f;
        lean = lean.LengthSquared() < 1e-6f ? Vector3.Zero : Vector3.Normalize(lean);

        if (string.Equals(c.Cue.Move, "Doom Desire", StringComparison.OrdinalIgnoreCase))
        {
            float k = (c.Age - 0.12f) / 0.6f;
            if (k >= 0f && k <= 1f)
            {
                float width = h * 0.5f * MathF.Sin(k * MathF.PI);
                var pillar = new[] { c.ToFeet + Vector3.UnitY * h * 7f + lean * h * 1.2f, c.ToFeet };
                fx.Ribbon(pillar, width, Fx.Fade(col.Main, 0.8f), FxBlend.Alpha);
                fx.Ribbon(pillar, width * 1.5f, Fx.Fade(col.Light, 0.45f));
                fx.Ribbon(pillar, width * 0.3f, White);
            }
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.5f, h * 1.5f, col.Light);
            fx.ScreenFlash(col.Light, 0.25f * Fx.Envelope(c.Age, I - 0.03f, I + 0.02f, I + 0.06f, I + 0.3f));
            Impact(c, col, fx, 1.3f);
            return;
        }

        var above = c.To + Vector3.UnitY * h * 2.4f + lean * h;
        for (int i = 0; i < 3; i++)
        {
            float start = 0.04f * i;
            float k = (c.Age - start) / (I - start);
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, Vector3.Lerp(above, c.To, k * k), h * (1.1f - 0.7f * k), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, Math.Min(1f, k * 4f)));
        }
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 1.1f, col.Light);
        Impact(c, col, fx, 1.2f);
    }
}
