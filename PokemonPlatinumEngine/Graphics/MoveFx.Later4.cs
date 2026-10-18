using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13a: the later games' moves, Generation 7's regular moves in moves.json's order (numbers 659 to 742, Shore
// Up to Double Iron Bash; the Z-Moves between them have their looks from Y14). As Y11 and Y12: no game's animation
// exists for any of them; the shapes, colours and timings are our own, taken from each move's data (type, category,
// target, effect, description) and the style guide's G8 vocabulary. Every recipe lands at ImpactTime, is over by
// EffectTime and stays under MaxQuads. This file holds the first half (659 to 694), MoveFx.Later4b.cs the second.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later4() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Shore Up"] = ShoreUpY13,
        ["First Impression"] = FirstImpressionY13,
        ["Baneful Bunker"] = BanefulBunkerY13,
        ["Spirit Shackle"] = SpiritShackleY13,
        ["Darkest Lariat"] = DarkestLariatY13,
        ["Sparkling Aria"] = SparklingAriaY13,
        ["Ice Hammer"] = IceHammerY13,
        ["Floral Healing"] = FloralHealingY13,
        ["High Horsepower"] = HighHorsepowerY13,
        ["Strength Sap"] = StrengthSapY13,
        ["Solar Blade"] = SolarBladeY13,
        ["Leafage"] = LeafageY13,
        ["Spotlight"] = SpotlightY13,
        ["Toxic Thread"] = ToxicThreadY13,
        ["Laser Focus"] = LaserFocusY13,
        ["Gear Up"] = GearUpY13,
        ["Throat Chop"] = ThroatChopY13,
        ["Pollen Puff"] = PollenPuffY13,
        ["Anchor Shot"] = AnchorShotY13,
        ["Psychic Terrain"] = PsychicTerrainY13,
        ["Lunge"] = LungeY13,
        ["Fire Lash"] = FireLashY13,
        ["Power Trip"] = PowerTripY13,
        ["Burn Up"] = BurnUpY13,
        ["Speed Swap"] = SpeedSwapY13,
        ["Smart Strike"] = SmartStrikeY13,
        ["Purify"] = PurifyY13,
        ["Revelation Dance"] = RevelationDanceY13,
        ["Core Enforcer"] = CoreEnforcerY13,
        ["Trop Kick"] = TropKickY13,
        ["Instruct"] = InstructY13,
        ["Beak Blast"] = BeakBlastY13,
        ["Clanging Scales"] = ClangingScalesY13,
        ["Dragon Hammer"] = DragonHammerY13,
        ["Brutal Swing"] = BrutalSwingY13,
        ["Aurora Veil"] = AuroraVeilY13
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A tall hexagonal panel standing before the user's side, for a screen or a shield: it rises, shimmers and fades with the cue.</summary>
    private static void L4Panel(in FxCtx c, FxList fx, Color body, Color rim, float width)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.2f, 0f, 1f));
        var at = c.FromFeet + L1Toward(c) * h * 0.2f + new Vector3(h * 0.35f, h * (0.12f + 0.4f * rise), 0.3f);
        float shimmer = 0.75f + 0.25f * MathF.Sin(c.Age * 13f);
        fx.Streak(FxShape.Hex, at, Vector3.UnitY, h * width, 1.5f, Fx.Fade(body, env * 0.8f * shimmer), FxBlend.Alpha);
        fx.Streak(FxShape.Hex, at, Vector3.UnitY, h * width * 1.08f, 1.5f, Fx.Fade(rim, env * 0.4f * shimmer));
        float k = (c.Age - 0.25f) / 0.45f;
        if (k > 0f && k < 1f) fx.Sprite(FxShape.Spark, at + new Vector3(0f, (k - 0.5f) * h * 0.9f, 0.1f), h * 0.2f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
    }

    /// <summary>Motes of <paramref name="shape"/> that drift out of <paramref name="at"/> and settle: a quiet scatter.</summary>
    private static void L4Drift(FxList fx, float age, int seed, Vector3 at, float h, float t0, float t1, int count, FxShape shape, Color a, Color b, float size, float reach, float lift)
    {
        float span = t1 - t0, t = age - t0;
        if (t < 0f || t > span) return;
        for (int i = 0; i < count; i++)
        {
            float u = (t / span + Fx.Rand(seed, i, 501)) % 1f;
            var p = at + new Vector3(Fx.Signed(seed, i, 502) * h * reach, h * lift * u, Fx.Signed(seed, i, 503) * h * 0.2f);
            fx.Sprite(shape, p, h * size * (0.7f + 0.5f * Fx.Rand(seed, i, 504)), Fx.Fade(Fx.Mix(a, b, Fx.Rand(seed, i, 505)), MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 4f + i);
        }
    }

    /// <summary>A curve through <paramref name="n"/> points between two spots that bows sideways by <paramref name="bow"/> and ripples, for a whip or a thread.</summary>
    private static Vector3[] L4Wave(Vector3 a, Vector3 b, float reach, float bow, float ripples, float phase, int n = 14)
    {
        var pts = new Vector3[n + 1];
        var dir = b - a;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;
            pts[i] = a + dir * u * reach + new Vector3(0f, (bow + MathF.Sin(u * MathF.Tau * ripples + phase) * bow) * MathF.Sin(u * MathF.PI), 0.1f);
        }
        return pts;
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Shore Up: sand whirls up round the user in a tan spiral and settles, and a soft green glow of healing follows it.</summary>
    private static void ShoreUpY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.5f, 0.7f);
        L1Spiral(fx, c.Age, c.Seed, c.FromFeet, h, 0f, 0.7f, 12, FxShape.Dot, Rgb(226, 196, 130), Rgb(190, 150, 90), 0.06f, 0.55f, 1.0f);
        if (env > 0f) fx.Sprite(FxShape.Smoke, c.FromFeet + new Vector3(0f, h * 0.2f, 0.1f), h * 0.9f * env, Fx.Fade(Rgb(222, 196, 140, 200), env * 0.6f), FxBlend.Alpha, c.Age * 2f);
        GroundRing(fx, c.Age, c.FromFeet, 0.05f, 0.5f, h * 1.1f, Rgb(214, 180, 118));
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, 0.32f);
    }

    /// <summary>First Impression: a bright star of light flashes on the foe before anything is seen, then a green-white double slash cuts it open.</summary>
    private static void FirstImpressionY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float star = Fx.Envelope(c.Age, I - 0.2f, I - 0.15f, I - 0.08f, I + 0.1f);
        if (star > 0f)
        {
            fx.Sprite(FxShape.Star, c.To, h * 0.9f * star, Fx.Fade(White, star), FxBlend.Alpha, MathF.PI / 4f);
            fx.Sprite(FxShape.Star, c.To, h * 0.6f * star, Fx.Fade(col.Light, star));
        }
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.06f, 2, 0.06f, col.Main, White);
        Fx.Stream(fx, c, 0f, 0.25f, 0.14f, 4, FxShape.Streak, White, col.Light, 0.1f, spread: 0.1f, trail: 3f, blend: FxBlend.Alpha);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Baneful Bunker: a dome of purple hexagons stands before the user, poison bubbles seeping up its face and dripping off its foot.</summary>
    private static void BanefulBunkerY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.03f) / 0.2f, 0f, 1f));
        var mid = c.FromFeet + L1Toward(c) * h * 0.2f + new Vector3(h * 0.25f, 0f, 0.2f);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 4 - row; i++)
            {
                float u = (i - (3 - row) * 0.5f) * 0.42f;
                var p = mid + new Vector3(u * h * 0.9f, h * (0.12f + 0.3f * row) * grow, 0f);
                fx.Sprite(FxShape.Hex, p, h * 0.2f * grow, Fx.Fade(col.Main, env * 0.75f), FxBlend.Alpha);
                fx.Sprite(FxShape.Hex, p, h * 0.22f * grow, Fx.Fade(col.Light, env * 0.3f));
            }
        for (int i = 0; i < 5; i++)
        {
            float u = (c.Age * 0.9f + i * 0.2f) % 1f;
            fx.Sprite(FxShape.Bubble, mid + new Vector3((i - 2) * h * 0.3f, h * (0.1f + 0.7f * u), 0.3f), h * 0.07f, Fx.Fade(Rgb(210, 140, 240), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.2f + i * 0.33f) % 1f;
            fx.Sprite(FxShape.Drop, mid + new Vector3((i - 1) * h * 0.55f, h * 0.15f - h * 0.15f * u, 0.3f), h * 0.07f, Fx.Fade(col.Light, env * (1f - u)), FxBlend.Alpha, MathF.PI);
        }
    }

    /// <summary>Spirit Shackle: a ghostly arrow is loosed from above and pins the foe's shadow to the ground, three violet rings closing like shackles.</summary>
    private static void SpiritShackleY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var top = c.ToFeet + new Vector3(h * 0.15f, h * 1.25f, 0f);
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        if (c.Age < I + 0.3f)
        {
            var tip = Vector3.Lerp(top, c.ToFeet + Vector3.UnitY * 0.05f, Fx.EaseOut(k));
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.3f);
            fx.Streak(FxShape.Arrow, tip + Vector3.UnitY * h * 0.2f, -Vector3.UnitY, h * 0.13f, 3f, Fx.Fade(col.Light, fade), FxBlend.Alpha, false);
            fx.Streak(FxShape.Beam, tip + Vector3.UnitY * h * 0.4f, Vector3.UnitY, h * 0.03f, 8f, Fx.Fade(White, fade * 0.8f));
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++)
            {
                float close = Fx.EaseOut(Math.Clamp((c.Age - I - i * 0.06f) / 0.2f, 0f, 1f));
                float fade = Fx.Envelope(c.Age, I + i * 0.06f, I + i * 0.06f + 0.05f, 1.0f, 1.2f);
                fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * (0.04f + i * h * 0.35f), Vector3.UnitY, h * (1.1f - 0.6f * close), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, fade * 0.9f), FxBlend.Alpha);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet, h, I, 0.7f, 7, FxShape.Wisp, col.Light, col.Main, 0.16f, 0.8f, -0.3f, FxBlend.Alpha, salt: 330);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Darkest Lariat: the user spins both arms like a rope, and a great black loop whips round the foe twice in a ring of dark smoke.</summary>
    private static void DarkestLariatY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.25f, I - 0.12f, I + 0.2f, I + 0.45f);
        if (env > 0f)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float turn = c.Age * 14f + pass * 3.14f;
                L2Ellipse(fx, c.To + new Vector3(0f, h * (0.1f - 0.2f * pass), 0f), h * (0.75f + 0.1f * pass), h * 0.28f, h * 0.09f, Fx.Fade(pass == 0 ? col.Dark : col.Main, env), 16, turn, FxBlend.Alpha);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.2f, 0.6f, 8, FxShape.Smoke, col.Dark, col.Main, 0.22f, 0.9f, -0.1f, FxBlend.Alpha, salt: 331);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.5f, 0.3f);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Sparkling Aria: a sung line of notes rises on a stream of bubbles that gather and burst over the foe, a clean glint left where a burn was.</summary>
    private static void SparklingAriaY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 7, FxShape.Note, col.Light, White, 0.17f, arc: 0.05f, spread: 0.3f, spin: 1f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.05f, 0.5f, 0.32f, 9, FxShape.Bubble, col.Main, White, 0.12f, arc: 0.04f, spread: 0.45f, blend: FxBlend.Alpha);
        Fx.Waves(fx, c, 0f, 0.5f, 3, FxShape.Ring, Fx.Fade(col.Light, 0.8f), 0.4f, 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Bubble, col.Light, White, 0.1f, 0.9f, -0.2f, FxBlend.Alpha, salt: 332);
            Fx.Glint(fx, c.Age, c.To + new Vector3(0f, h * 0.3f, 0f), h, I + 0.2f, White, 0.4f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Ice Hammer: a huge hammer of pale blue ice comes down on the foe, frost bursting from the head, and the user's speed falls away in down arrows.</summary>
    private static void IceHammerY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Fist, 0.9f, 0f, I, 0.8f, Rgb(200, 236, 255), 0f);
        DropOn(c, fx, FxShape.Shard, 0.95f, 0f, I, 0.55f, White, 0.3f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.5f, col.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Snowflake, White, col.Light, 0.12f, 1.1f, 0.4f, FxBlend.Alpha, spin: 4f, salt: 333);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        L2Down(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.15f, 2, Rgb(150, 200, 255));
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Floral Healing: a five-petalled blossom opens over the target and sheds green light and petals that sink into it.</summary>
    private static void FloralHealingY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.7f, 0.95f);
        if (env <= 0f) return;
        float open = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.3f, 0f, 1f));
        L3Flower(fx, c.ToFeet + new Vector3(0f, h * 1.0f, 0.2f), h * 0.4f * open, Rgb(255, 176, 214), Rgb(255, 232, 130), env, c.Age * 0.8f);
        L4Drift(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 0.9f, 0f), h, 0.2f, 0.9f, 8, FxShape.Petal, Rgb(255, 190, 220), White, 0.08f, 0.5f, -0.8f);
        L2Heal(fx, c.Age, c.Seed, c.ToFeet, h, 0.35f);
    }

    /// <summary>High Horsepower: a hoof stamps down on the foe twice over, the ground cracking out from the print in a ring of dust.</summary>
    private static void HighHorsepowerY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Foot, 0.8f, 0f, I - 0.1f, 0.5f, Rgb(214, 180, 126), 0f);
        DropOn(c, fx, FxShape.Foot, 0.9f, 0f, I, 0.65f, Rgb(236, 206, 150), 0f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + 0.3f;
                fx.Streak(FxShape.Crack, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.55f, 0.05f, MathF.Sin(a) * h * 0.3f), new Vector3(MathF.Cos(a), 0f, MathF.Sin(a)), h * 0.12f, 3f, Fx.Fade(Rgb(70, 48, 30), Fx.Envelope(c.Age, I, I + 0.03f, 0.9f, 1.1f)), FxBlend.Alpha);
            }
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.5f, col.Light);
            Dust(c, fx, c.ToFeet, h, I, 8);
            Jolt(fx, c.Age, I, 0.8f, 0.35f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Strength Sap: green sap is drawn out of the foe in a thread of leaves and drops to the user, which glows green, the foe slumping under a down arrow.</summary>
    private static void StrengthSapY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0.05f, 0.65f, 0.35f, 9, FxShape.Leaf, col.Main, col.Light, 0.2f, arc: 0.04f, spread: 0.2f, spin: 3f, blend: FxBlend.Alpha, reverse: true);
        Fx.Stream(fx, c, 0.1f, 0.65f, 0.35f, 7, FxShape.Drop, Rgb(190, 240, 130), White, 0.14f, spread: 0.15f, blend: FxBlend.Alpha, reverse: true);
        L2Down(fx, c.Age, c.Seed, c.ToFeet, h, 0.25f, 2, Rgb(230, 90, 80));
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0.4f);
    }

    /// <summary>Solar Blade: sunlight gathers on the user in a golden orb, then it is drawn out into a long blade of white-gold light that cuts down across the foe.</summary>
    private static void SolarBladeY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var sun = Rgb(255, 224, 110);
        var hand = c.From + new Vector3(h * 0.45f, h * 0.2f, 0.1f);
        float g = Fx.Envelope(c.Age, 0f, 0.1f, 0.22f, 0.3f);
        if (g > 0f)
        {
            fx.Sprite(FxShape.Glow, hand, h * 0.9f * g, Fx.Fade(sun, 0.7f * g));
            fx.Sprite(FxShape.Orb, hand, h * 0.35f * g, Fx.Fade(White, g), FxBlend.Alpha);
        }
        Gather(fx, c.Age, c.Seed, hand, h, 0f, 0.22f, 8, sun, col.Light, 0.07f, 0.9f);
        float k = (c.Age - (I - 0.12f)) / 0.12f;
        if (k > 0f && c.Age < I + 0.3f)
        {
            float fade = c.Age < I ? 1f : Math.Max(0f, 1f - (c.Age - I) / 0.3f);
            var along = Vector3.Normalize(new Vector3(0.5f, -1f, 0f));
            float len = c.ToSize * 2.6f * Fx.EaseOut(Math.Min(1f, k));
            var mid = c.To + new Vector3(-0.2f * c.ToSize, 0.2f * c.ToSize, 0.2f);
            fx.Streak(FxShape.Sword, mid, along, c.ToSize * 0.22f, len / (c.ToSize * 0.22f) * 0.5f, Fx.Fade(sun, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, mid, along, c.ToSize * 0.05f, len / (c.ToSize * 0.05f) * 0.5f, Fx.Fade(White, fade));
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 9, FxShape.Spark, White, sun, 0.12f, 1.0f, 0.2f, salt: 334);
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Leafage: a handful of green leaves is flung one at a time across to the foe, each a small slap of a flash.</summary>
    private static void LeafageY13(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, 0.4f, 0.22f, 6, FxShape.Leaf, col.Main, col.Light, 0.15f, arc: 0.03f, spread: 0.18f, spin: 8f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed, c.To + new Vector3(0.1f, 0.05f, 0f), c.ToSize, I - 0.05f, 0.18f, col.Main, col.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 6, FxShape.Leaf, col.Main, col.Light, 0.09f, 0.7f, 0.3f, FxBlend.Alpha, spin: 5f, salt: 335);
        }
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Spotlight: a cone of white stage light drops from above on the foe, a bright disc on the ground and sparkles, and every eye is on it.</summary>
    private static void SpotlightY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var top = c.ToFeet + new Vector3(h * 0.15f, h * 1.3f, 0f);
        fx.Streak(FxShape.Beam, Vector3.Lerp(top, c.ToFeet, 0.5f), top - c.ToFeet, h * 0.55f, 1.15f, Fx.Fade(Rgb(255, 250, 200), env * 0.45f), FxBlend.Alpha);
        fx.Streak(FxShape.Beam, Vector3.Lerp(top, c.ToFeet, 0.5f), top - c.ToFeet, h * 0.22f, 2.4f, Fx.Fade(White, env * 0.7f));
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.1f, Fx.Fade(Rgb(255, 246, 190), env * 0.7f), FxBlend.Alpha);
        fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 0.95f, Fx.Fade(White, env));
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 1.1f + i / 6f) % 1f;
            fx.Sprite(FxShape.Spark, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 336) * h * 0.6f, h * (0.1f + 0.9f * u), 0.1f), h * 0.08f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>Toxic Thread: a thin violet thread is spat across and winds round the foe in loops, a poison bubble swelling on it, a down arrow under it.</summary>
    private static void ToxicThreadY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.9f, 1.1f);
        if (env <= 0f) return;
        float reach = Fx.EaseOut(Math.Clamp(c.Age / (I + 0.05f), 0f, 1f));
        fx.Ribbon(L4Wave(c.From, c.Aim, reach, h * 0.12f, 1.5f, c.Age * 10f), h * 0.08f, Fx.Fade(col.Main, env), FxBlend.Alpha);
        fx.Ribbon(L4Wave(c.From, c.Aim, reach, h * 0.12f, 1.5f, c.Age * 10f), h * 0.03f, Fx.Fade(col.Light, env));
        if (c.Cue.Landed)
        {
            float wind = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f));
            for (int i = 0; i < 3; i++)
                L2Ellipse(fx, c.To + new Vector3(0f, h * (0.3f - 0.25f * i), 0f), h * 0.55f * wind, h * 0.12f, h * 0.035f, Fx.Fade(col.Main, env), 14, i * 1.3f, FxBlend.Alpha);
            float bub = Fx.Envelope(c.Age, I + 0.1f, I + 0.3f, 0.95f, 1.1f);
            fx.Sprite(FxShape.Bubble, c.To + new Vector3(h * 0.35f, h * 0.55f, 0.2f), h * 0.2f * bub, Fx.Fade(Rgb(214, 130, 240), bub), FxBlend.Alpha);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(190, 120, 230));
        }
    }

    /// <summary>Laser Focus: a red sight closes in on the user's eyes in shrinking rings and a thin line of light snaps taut, locking the aim.</summary>
    private static void LaserFocusY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var eye = c.FromFeet + new Vector3(h * 0.25f, h * 0.85f, 0.2f);
        var red = Rgb(255, 70, 70);
        for (int i = 0; i < 3; i++)
        {
            float k = Math.Clamp((c.Age - i * 0.08f) / 0.4f, 0f, 1f);
            fx.Sprite(FxShape.Ring, eye, h * (0.7f - 0.5f * Fx.EaseOut(k) - 0.05f * i), Fx.Fade(red, env * (0.5f + 0.5f * k)), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Target, eye, h * 0.28f, Fx.Fade(red, env), FxBlend.Alpha);
        float snap = Fx.Envelope(c.Age, 0.3f, 0.34f, 0.7f, 0.95f);
        if (snap > 0f)
            fx.Ribbon(new[] { eye, eye + L1Toward(c) * h * 3.5f + new Vector3(h * 1.5f, 0f, 0f) }, h * 0.025f, Fx.Fade(red, snap), FxBlend.Add);
        fx.Sprite(FxShape.Glow, eye, h * 0.35f, Fx.Fade(red, env * 0.6f));
    }

    /// <summary>Gear Up: two steel gears turn either side of the user, meshing, and up arrows climb between them.</summary>
    private static void GearUpY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.16f, 0.8f, 1.05f);
        if (env <= 0f) return;
        var steel = Rgb(176, 186, 204);
        for (int g = 0; g < 2; g++)
        {
            float side = g == 0 ? -1f : 1f;
            var at = c.FromFeet + new Vector3(side * h * 0.6f, h * (0.55f + 0.2f * g), 0.2f);
            float turn = c.Age * 5f * side;
            fx.Sprite(FxShape.Ring, at, h * 0.3f, Fx.Fade(steel, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Cross, at, h * 0.28f, Fx.Fade(col.Main, env), FxBlend.Alpha, turn);
            fx.Sprite(FxShape.Cross, at, h * 0.28f, Fx.Fade(col.Main, env), FxBlend.Alpha, turn + MathF.PI / 4f);
            fx.Sprite(FxShape.Dot, at, h * 0.08f, Fx.Fade(White, env), FxBlend.Alpha);
        }
        L3Up(fx, c.Age, c.FromFeet, h, 0.25f, 3, Rgb(255, 150, 120));
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + new Vector3(0f, h * 0.65f, 0.1f), h, 0.1f, 0.5f, 6, FxShape.Spark, White, steel, 0.07f, 0.6f, salt: 337);
    }

    /// <summary>Throat Chop: the edge of a dark hand chops down across the foe's throat, and the sound waves round its head are cut off with crossed marks.</summary>
    private static void ThroatChopY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Palm, new Vector3(0.7f, 1.1f, 0f), I, 0.85f, Rgb(150, 110, 190), -0.9f, 0.14f);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.45f, I + 0.65f);
            for (int i = 0; i < 2; i++)
            {
                float r = h * (0.25f + 0.18f * i) * (1f + 0.3f * (c.Age - I));
                fx.Sprite(FxShape.Wave, c.ToFeet + new Vector3(h * 0.45f, h * 0.95f, 0.2f), r, Fx.Fade(col.Light, env * 0.7f), FxBlend.Alpha);
            }
            fx.Sprite(FxShape.Cross, c.ToFeet + new Vector3(h * 0.3f, h * 0.95f, 0.3f), h * 0.2f, Fx.Fade(Rgb(255, 80, 90), env), FxBlend.Alpha, MathF.PI / 4f);
            Fx.Contact(fx, c.Age, c.Seed, c.To + new Vector3(0f, h * 0.2f, 0f), h, I, 0.25f, col.Main, col.Light);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Pollen Puff: a soft yellow puffball is lobbed across and bursts into a cloud of pollen that glitters and drifts down.</summary>
    private static void PollenPuffY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.05f) / (I - 0.05f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.08f);
            fx.Sprite(FxShape.Spore, p, c.FromSize * 0.4f, Fx.Fade(Rgb(250, 226, 110), 0.95f), FxBlend.Alpha, k * 7f);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 0.5f, Fx.Fade(col.Light, 0.5f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 10, FxShape.Spore, Rgb(250, 226, 110), col.Light, 0.14f, 0.9f, -0.15f, FxBlend.Alpha, salt: 338);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Dot, White, Rgb(250, 226, 110), 0.05f, 1.1f, -0.1f, salt: 339);
            L4Drift(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 1.0f, 0f), h, I, I + 0.8f, 6, FxShape.Spore, Rgb(250, 226, 110), White, 0.06f, 0.5f, -0.8f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Anchor Shot: a heavy anchor is flung down onto the foe by a chain, its flukes dragging it to the ground, and links of the chain lie in a ring round its feet.</summary>
    private static void AnchorShotY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize * 1.5f;
        var steel = Rgb(190, 200, 216);
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        float after = Math.Max(0f, (c.Age - I) / 0.5f);
        if (after < 1f)
        {
            float e = Fx.EaseOut(k);
            var pos = Vector3.Lerp(c.To + new Vector3(0f, h * 0.9f, 0.2f), c.To + new Vector3(0f, h * 0.15f, 0.2f), e);
            float a = 1f - after;
            fx.Ribbon(new[] { pos, pos + new Vector3(0f, h * 0.9f, 0f), c.From + new Vector3(0f, c.FromSize * 0.4f, 0f) }, h * 0.07f, Fx.Fade(Rgb(120, 130, 150), a), FxBlend.Alpha);
            // shaft, stock and the curved flukes
            fx.Ribbon(new[] { pos + new Vector3(0f, h * 0.35f, 0f), pos - new Vector3(0f, h * 0.35f, 0f) }, h * 0.11f, Fx.Fade(steel, a), FxBlend.Alpha);
            fx.Ribbon(new[] { pos + new Vector3(-h * 0.2f, h * 0.22f, 0f), pos + new Vector3(h * 0.2f, h * 0.22f, 0f) }, h * 0.1f, Fx.Fade(steel, a), FxBlend.Alpha);
            var flukes = new Vector3[9];
            for (int i = 0; i <= 8; i++)
            {
                float t = -MathF.PI * 0.85f + i * MathF.PI * 0.85f * 2f / 8f;
                flukes[i] = pos + new Vector3(MathF.Sin(t) * h * 0.32f, -h * 0.3f + MathF.Cos(t) * h * 0.15f, 0f);
            }
            fx.Ribbon(flukes, h * 0.11f, Fx.Fade(steel, a), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            L2Ellipse(fx, c.ToFeet + new Vector3(0f, h * 0.05f, 0.1f), h * 0.9f, h * 0.18f, h * 0.05f, Fx.Fade(steel, Fx.Envelope(c.Age, I, I + 0.06f, 1.0f, 1.2f)), 14, 0.2f, FxBlend.Alpha);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.2f, col.Light);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Psychic Terrain: pink-violet ripples spread over the ground between the two sides, little lights of thought rising slowly from them.</summary>
    private static void PsychicTerrainY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 3.2f, Fx.Fade(Rgb(214, 120, 240), env * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float u = (c.Age * 1.1f + i * 0.25f) % 1f;
            fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.04f, Vector3.UnitY, h * (0.5f + 2.6f * u), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env * (1f - u) * 0.9f), FxBlend.Alpha);
        }
        fx.Flat(FxShape.Swirl, mid + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.5f, Fx.Fade(col.Light, env * 0.6f), FxBlend.Alpha, c.Age * 2f);
        for (int i = 0; i < 7; i++)
        {
            float u = (c.Age * 0.7f + i / 7f) % 1f;
            float a = i * 2.4f;
            fx.Sprite(i % 2 == 0 ? FxShape.Star : FxShape.Spark, mid + new Vector3(MathF.Cos(a) * h * 1.4f, h * 0.9f * u, MathF.Sin(a) * h * 0.6f), h * 0.1f, Fx.Fade(Rgb(255, 214, 255), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 3f);
        }
    }

    /// <summary>Lunge: the user shoots across in one long green streak, a pair of mandibles snapping shut on the foe, and its Attack sinks.</summary>
    private static void LungeY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.06f) / (I - 0.06f), 0f, 1f);
        if (c.Age < I + 0.1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.2f, 5f, Fx.Fade(col.Main, 0.9f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, c.Aim - c.From, c.FromSize * 0.08f, 5f, Fx.Fade(White, 0.9f));
        }
        FangPair(c, fx, I - 0.1f, 0.2f, Rgb(214, 238, 150), col.Light);
        L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.12f, 2, Rgb(230, 100, 90));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Fire Lash: a whip of flame cracks across the field in an S, flames licking along it, and lashes the foe so that its Defense melts away.</summary>
    private static void FireLashY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float reach = Fx.EaseOut(Math.Clamp(c.Age / (I + 0.02f), 0f, 1f));
        float env = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            var pts = L4Wave(c.From, c.Aim, reach, h * 0.3f, 1.2f, c.Age * 12f);
            fx.Ribbon(pts, h * 0.1f, Fx.Fade(col.Main, env), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.04f, Fx.Fade(Rgb(255, 240, 160), env));
            for (int i = 3; i < pts.Length; i += 3) fx.Sprite(FxShape.Flame, pts[i] + new Vector3(0f, h * 0.08f, 0f), h * 0.14f, Fx.Fade(col.Light, env * 0.8f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Ember, col.Light, col.Main, 0.09f, 1.0f, 0.3f, salt: 340);
            fx.ScreenFlash(Rgb(255, 210, 150), 0.14f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.06f, I + 0.2f));
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(255, 150, 90));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Power Trip: a dark red aura swells round the user, up arrows stacking on it, and the whole weight of them is swung down in a black fist.</summary>
    private static void PowerTripY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float g = Fx.Envelope(c.Age, 0f, 0.1f, 0.22f, 0.3f);
        if (g > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.1f, Fx.Fade(Rgb(190, 40, 60), g * 0.6f), FxBlend.Alpha);
        L3Up(fx, c.Age, c.FromFeet, c.FromSize, 0.02f, 4, Rgb(255, 120, 120));
        Strike(c, fx, FxShape.Fist, new Vector3(0.5f, 1.1f, 0f), I, 0.65f, Rgb(70, 40, 80), 0.3f, 0.14f);
        Strike(c, fx, FxShape.Glow, new Vector3(0.5f, 1.1f, 0f), I, 0.9f, Rgb(200, 50, 70), 0f, 0.14f);
        if (c.Cue.Landed) { GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.3f, col.Light); Jolt(fx, c.Age, I, 0.7f, 0.3f); }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Burn Up: the user is swallowed by roaring flame that bursts out in a great wave across the field, and the fire on its own body dies out to ash.</summary>
    private static void BurnUpY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float eng = Fx.Envelope(c.Age, 0f, 0.12f, 0.3f, 0.7f);
        if (eng > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.3f * eng, Fx.Fade(col.Main, eng * 0.7f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 5f;
                fx.Sprite(FxShape.Flame, c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.45f, h * (0.3f + 0.2f * MathF.Sin(a * 2f)), MathF.Sin(a) * 0.1f), h * 0.4f * eng, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, eng), FxBlend.Add);
            }
        }
        Fx.Waves(fx, c, 0.2f, 0.75f, 3, FxShape.Flame, col.Main, 0.7f, 0.3f);
        Fx.Stream(fx, c, 0.2f, 0.55f, 0.28f, 8, FxShape.Ember, col.Light, col.Main, 0.1f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 12, FxShape.Flame, col.Light, col.Main, 0.18f, 1.1f, 0.3f, salt: 341);
            Jolt(fx, c.Age, I, 0.6f, 0.3f);
        }
        float ash = Fx.Envelope(c.Age, 0.55f, 0.65f, 0.9f, 1.15f);
        if (ash > 0f) Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.55f, 0.6f, 6, FxShape.Smoke, Rgb(150, 144, 140), Rgb(100, 96, 94), 0.18f, 0.5f, -0.1f, FxBlend.Alpha, salt: 342);
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Speed Swap: streaks of speed lines leave each Pokémon in turn, pass one another in the middle, and arrive on the other, pink sparkles marking the trade.</summary>
    private static void SpeedSwapY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float h = c.ToSize;
        for (int s = 0; s < 2; s++)
        {
            var from = s == 0 ? c.From : c.To;
            var to = s == 0 ? c.To : c.From;
            for (int i = 0; i < 3; i++)
            {
                float k = (c.Age - 0.05f - i * 0.05f - s * 0.04f) / 0.38f;
                if (k < 0f || k > 1f) continue;
                var p = Vector3.Lerp(from, to, Fx.EaseOut(k)) + new Vector3(0f, (i - 1) * h * 0.2f + (s == 0 ? 0.2f : -0.2f) * h, 0.1f);
                fx.Streak(FxShape.Streak, p, to - from, h * 0.07f, 5f, Fx.Fade(s == 0 ? col.Main : col.Light, MathF.Sin(k * MathF.PI) * env), FxBlend.Alpha);
            }
        }
        var mid = (c.From + c.To) * 0.5f;
        fx.Sprite(FxShape.Swirl, mid, h * 0.9f, Fx.Fade(col.Light, env * MathF.Sin(Math.Clamp((c.Age - 0.1f) / 0.5f, 0f, 1f) * MathF.PI)), FxBlend.Alpha, c.Age * 6f);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.2f, c.FromSize, 0.5f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.2f, c.ToSize, 0.5f, White, 0.4f);
    }

    /// <summary>Smart Strike: a red sight locks onto the foe and a sharp steel horn is driven through its middle.</summary>
    private static void SmartStrikeY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.06f, I - 0.08f, I);
        if (env > 0f)
        {
            float shrink = 1f - 0.5f * Math.Clamp(c.Age / (I - 0.08f), 0f, 1f);
            fx.Sprite(FxShape.Target, c.To, h * 0.7f * shrink, Fx.Fade(Rgb(255, 80, 80), env), FxBlend.Alpha, c.Age * 3f);
        }
        Strike(c, fx, FxShape.Horn, new Vector3(-1.3f, 0.2f, 0f), I, 1.0f, Rgb(214, 224, 240), -0.4f, 0.12f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, col.Light, 0.1f, 1.0f, salt: 343);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Purify: a drop of clear light falls on the foe and its sickness lifts away in a grey haze, white rings and green motes rising, the user mending.</summary>
    private static void PurifyY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float k = Math.Clamp((c.Age - 0.04f) / 0.3f, 0f, 1f);
        var top = c.ToFeet + new Vector3(0f, h * 1.3f, 0.2f);
        if (k < 1f) fx.Sprite(FxShape.Drop, Vector3.Lerp(top, c.To, Fx.EaseOut(k)), h * 0.38f, Fx.Fade(Rgb(210, 240, 255), env), FxBlend.Alpha, MathF.PI);
        for (int i = 0; i < 3; i++)
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * (0.04f + h * 0.3f * i * Fx.EaseOut(Math.Clamp((c.Age - 0.3f) / 0.4f, 0f, 1f))), Vector3.UnitY, h * (0.8f - 0.1f * i), Fx.Fade(White, env * Math.Max(0f, 1f - (c.Age - 0.3f) / 0.5f)), FxBlend.Alpha);
        Rising(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 0.2f, 0f), h, 0.3f, 1.0f, 5, FxShape.Smoke, Rgb(150, 140, 160, 200), 0.2f, 0.3f);
        L4Drift(fx, c.Age, c.Seed, c.ToFeet + new Vector3(0f, h * 0.2f, 0f), h, 0.35f, 1.0f, 6, FxShape.Spark, White, Rgb(200, 255, 220), 0.07f, 0.5f, 0.9f);
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0.5f);
    }

    /// <summary>Revelation Dance: the user whirls through a dance of its own colour, notes and petals circling it, and the flourish is thrown across to the foe as a ring.</summary>
    private static void RevelationDanceY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.3f, 0.4f);
        if (env > 0f)
        {
            Orbit(fx, c.Age, c.FromFeet + new Vector3(0f, h * 0.55f, 0f), h, 0f, 0.42f, 6, FxShape.Petal, col.Main, 0.14f, 0.7f, 9f);
            Orbit(fx, c.Age, c.FromFeet + new Vector3(0f, h * 0.7f, 0f), h, 0f, 0.42f, 4, FxShape.Note, col.Light, 0.12f, 0.5f, -7f, 0.35f);
        }
        Fx.Stream(fx, c, 0.2f, 0.55f, 0.3f, 7, FxShape.Star, col.Light, White, 0.14f, arc: 0.04f, spread: 0.3f, spin: 6f, blend: FxBlend.Alpha);
        Fx.Waves(fx, c, 0.25f, 0.65f, 3, FxShape.Ring, Fx.Fade(col.Main, 0.9f), 0.45f, 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Petal, col.Main, White, 0.12f, 1.0f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 344);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 6, FxShape.Note, col.Light, White, 0.1f, 0.8f, 0.1f, FxBlend.Alpha, salt: 345);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Core Enforcer: a beam of green-violet rings shrinks down on the foe, a white cross stamping out the ability over its head as the rings close.</summary>
    private static void CoreEnforcerY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Beam(fx, c, 0.02f, 0.55f, c.FromSize * 0.2f, Fx.Fade(col.Main, 0.8f), White, 8);
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.05f - i * 0.05f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, Vector3.Lerp(c.From, c.Aim, k), c.FromSize * (0.7f - 0.35f * k), Fx.Fade(col.Light, MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.45f, I + 0.65f);
            fx.Sprite(FxShape.Cross, c.ToFeet + new Vector3(0f, h * 1.15f, 0.3f), h * 0.25f * env, Fx.Fade(Rgb(255, 230, 250), env), FxBlend.Alpha, MathF.PI / 4f);
            fx.Sprite(FxShape.Ring, c.ToFeet + new Vector3(0f, h * 1.15f, 0.25f), h * 0.3f, Fx.Fade(col.Main, env), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(col.Light, 0.9f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Trop Kick: a bare foot swings up under the foe in a spray of green leaves and orange blossom, its Attack falling.</summary>
    private static void TropKickY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Foot, new Vector3(-0.2f, -1.0f, 0f), I, 0.55f, Rgb(240, 206, 160), 0.3f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 8, FxShape.Leaf, col.Main, col.Light, 0.14f, 1.1f, 0.3f, FxBlend.Alpha, spin: 4f, salt: 346);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 6, FxShape.Petal, Rgb(255, 150, 70), Rgb(255, 210, 90), 0.12f, 1.0f, 0.2f, FxBlend.Alpha, spin: 5f, salt: 347);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.12f, 2, Rgb(230, 100, 90));
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Instruct: the user points sharply at its ally and a turning arrow circles above that one's head, the ring of a command spreading out.</summary>
    private static void InstructY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float pt = Fx.Envelope(c.Age, 0f, 0.05f, 0.25f, 0.4f);
        fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.7f, 0.2f), c.FromSize * 0.18f, Fx.Fade(Rgb(255, 214, 255), pt), FxBlend.Alpha);
        Fx.Waves(fx, c, 0.05f, 0.5f, 3, FxShape.Ring, Fx.Fade(col.Main, 0.9f), 0.4f, 0.3f);
        var top = c.ToFeet + new Vector3(0f, h * 0.9f, 0.2f);
        for (int i = 0; i < 4; i++)
        {
            float a = c.Age * 6f + i * MathF.Tau / 4f;
            fx.Sprite(FxShape.Arrow, top + new Vector3(MathF.Cos(a) * h * 0.38f, MathF.Sin(a) * h * 0.1f, 0f), h * 0.16f, Fx.Fade(col.Light, env * (c.Age > I - 0.2f ? 1f : 0.4f)), FxBlend.Alpha, a + MathF.PI / 2f);
        }
        fx.Sprite(FxShape.Swirl, top, h * 0.3f, Fx.Fade(col.Main, env * 0.8f), FxBlend.Alpha, -c.Age * 5f);
    }

    /// <summary>Beak Blast: the user's beak heats up, glowing red and orange with steam at its tip, and then spears the foe with a burst of scorching sparks.</summary>
    private static void BeakBlastY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var beak = c.From + new Vector3(h * 0.55f, h * 0.15f, 0.1f);
        float heat = Fx.Envelope(c.Age, 0f, 0.14f, I - 0.06f, I + 0.02f);
        if (heat > 0f)
        {
            fx.Sprite(FxShape.Beak, beak, h * 0.4f, Fx.Fade(Fx.Mix(Rgb(255, 214, 140), Rgb(255, 60, 30), Math.Min(1f, c.Age * 3f)), heat), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, beak, h * 0.7f * heat, Fx.Fade(Rgb(255, 110, 40), heat * 0.6f));
            Rising(fx, c.Age, c.Seed, beak, h, 0f, I, 4, FxShape.Smoke, Rgb(240, 240, 240, 220), 0.14f, 0.1f);
        }
        Strike(c, fx, FxShape.Beak, new Vector3(-1.2f, 0.1f, 0f), I, 0.5f, Rgb(255, 120, 60), 0f, 0.12f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Ember, Rgb(255, 214, 120), Rgb(255, 80, 40), 0.08f, 1.0f, 0.3f, salt: 348);
            Jolt(fx, c.Age, I, 0.5f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Clanging Scales: the user's scales ring like cymbals, rings of sound rolling out of them to the foe, and its own Defense drops as they rattle loose.</summary>
    private static void ClangingScalesY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.3f, 0.45f);
        if (env > 0f)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 12f * (i % 2 == 0 ? 1f : -1f);
                fx.Sprite(FxShape.Hex, c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.5f + 0.1f * MathF.Sin(a)), 0.15f), h * 0.11f, Fx.Fade(i % 2 == 0 ? col.Light : White, env), FxBlend.Alpha, a);
            }
        }
        Fx.Waves(fx, c, 0.05f, 0.7f, 4, FxShape.Ring, Fx.Fade(col.Main, 0.9f), 0.55f, 0.3f);
        Fx.Waves(fx, c, 0.1f, 0.7f, 3, FxShape.Wave, Fx.Fade(White, 0.7f), 0.45f, 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.5f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.5f, 0.3f);
        }
        L2Down(fx, c.Age, c.Seed, c.FromFeet, h, I + 0.1f, 2, Rgb(230, 100, 90));
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Dragon Hammer: the user's whole head and tail come down on the foe as one great indigo hammer, the ground splitting under it in violet light.</summary>
    private static void DragonHammerY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Rock, 1.0f, 0f, I, 0.85f, Fx.Mix(col.Dark, col.Main, 0.5f), 0.2f);
        DropOn(c, fx, FxShape.Claw, 1.0f, 0.3f, I, 0.5f, Rgb(214, 190, 255), 0.5f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + 0.5f;
                fx.Streak(FxShape.Crack, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.5f, 0.05f, MathF.Sin(a) * h * 0.3f), new Vector3(MathF.Cos(a), 0f, MathF.Sin(a)), h * 0.11f, 3f, Fx.Fade(col.Light, Fx.Envelope(c.Age, I, I + 0.03f, 0.9f, 1.1f)), FxBlend.Alpha);
            }
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.6f, col.Light);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.08f, 0.5f, h * 1.0f, col.Main);
            Jolt(fx, c.Age, I, 0.9f, 0.35f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Brutal Swing: the user spins and swings its whole arm round in one huge dark arc that sweeps through everything beside it, a wake of black smoke after.</summary>
    private static void BrutalSwingY13(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.22f)) / 0.3f;
        if (k > 0f && k < 1f)
        {
            float ang = -1.1f + k * 2.2f;
            var pts = new Vector3[11];
            for (int i = 0; i <= 10; i++)
            {
                float a = ang - (10 - i) * 0.09f;
                pts[i] = c.ToFeet + new Vector3(MathF.Sin(a) * h * 1.4f, h * (0.55f + 0.35f * MathF.Cos(a)), 0.3f);
            }
            fx.Ribbon(pts, h * 0.2f, Fx.Fade(col.Dark, 0.85f * MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.07f, Fx.Fade(col.Light, MathF.Sin(k * MathF.PI)));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.05f, 0.8f, 8, FxShape.Smoke, col.Dark, col.Main, 0.26f, 0.9f, -0.1f, FxBlend.Alpha, salt: 349);
            Dust(c, fx, c.ToFeet, h, I, 5);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Aurora Veil: curtains of green, blue and pink light hang rippling over the user's side, snow glittering as it falls through them.</summary>
    private static void AuroraVeilY13(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.82f, 1.1f);
        if (env <= 0f) return;
        var hues = new[] { Rgb(120, 255, 180), Rgb(120, 200, 255), Rgb(255, 160, 230) };
        for (int band = 0; band < 3; band++)
        {
            var pts = new Vector3[13];
            for (int i = 0; i <= 12; i++)
            {
                float u = i / 12f;
                pts[i] = c.FromFeet + new Vector3((u - 0.5f) * h * 2.8f + h * 0.2f, h * (0.7f + 0.1f * band) + MathF.Sin(u * 7f + c.Age * 3f + band) * h * 0.1f, 0.2f + band * 0.02f);
            }
            fx.Ribbon(pts, h * 0.16f, Fx.Fade(hues[band], env * 0.55f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.06f, Fx.Fade(White, env * 0.5f));
        }
        for (int i = 0; i < 9; i++)
        {
            float u = (c.Age * 0.8f + i / 9f) % 1f;
            fx.Sprite(FxShape.Snowflake, c.FromFeet + new Vector3((Fx.Rand(c.Seed, i, 350) - 0.4f) * h * 2.2f, h * (1.0f - 0.95f * u), 0.15f), h * 0.07f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 5f);
        }
    }
}
