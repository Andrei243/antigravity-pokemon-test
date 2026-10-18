using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y13b, the first part: Generation 8's moves from Clangorous Soul (775) to Thunder Cage (819), with the helpers
// the whole batch shares (MoveFx.Later5b.cs and Later5c.cs are the rest). As Y13a: no game's animation exists for any of
// them; the shapes, colours and timings are our own, taken from each move's data (type, category, target, effect,
// description) and the style guide's G8 vocabulary. Every recipe lands at ImpactTime, is over by EffectTime and stays
// under MaxQuads.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later5() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Clangorous Soul"] = ClangorousSoulY13b,
        ["Body Press"] = BodyPressY13b,
        ["Decorate"] = DecorateY13b,
        ["Drum Beating"] = DrumBeatingY13b,
        ["Snap Trap"] = SnapTrapY13b,
        ["Pyro Ball"] = PyroBallY13b,
        ["Behemoth Blade"] = BehemothBladeY13b,
        ["Behemoth Bash"] = BehemothBashY13b,
        ["Aura Wheel"] = AuraWheelY13b,
        ["Breaking Swipe"] = BreakingSwipeY13b,
        ["Branch Poke"] = BranchPokeY13b,
        ["Overdrive"] = OverdriveY13b,
        ["Apple Acid"] = AppleAcidY13b,
        ["Grav Apple"] = GravAppleY13b,
        ["Spirit Break"] = SpiritBreakY13b,
        ["Strange Steam"] = StrangeSteamY13b,
        ["Life Dew"] = LifeDewY13b,
        ["Obstruct"] = ObstructY13b,
        ["False Surrender"] = FalseSurrenderY13b,
        ["Meteor Assault"] = MeteorAssaultY13b,
        ["Eternabeam"] = EternabeamY13b,
        ["Steel Beam"] = SteelBeamY13b,
        ["Expanding Force"] = ExpandingForceY13b,
        ["Steel Roller"] = SteelRollerY13b,
        ["Scale Shot"] = ScaleShotY13b,
        ["Meteor Beam"] = MeteorBeamY13b,
        ["Shell Side Arm"] = ShellSideArmY13b,
        ["Misty Explosion"] = MistyExplosionY13b,
        ["Grassy Glide"] = GrassyGlideY13b,
        ["Rising Voltage"] = RisingVoltageY13b,
        ["Terrain Pulse"] = TerrainPulseY13b,
        ["Skitter Smack"] = SkitterSmackY13b,
        ["Burning Jealousy"] = BurningJealousyY13b,
        ["Lash Out"] = LashOutY13b,
        ["Poltergeist"] = PoltergeistY13b,
        ["Corrosive Gas"] = CorrosiveGasY13b,
        ["Coaching"] = CoachingY13b,
        ["Flip Turn"] = FlipTurnY13b,
        ["Triple Axel"] = TripleAxelY13b,
        ["Dual Wingbeat"] = DualWingbeatY13b,
        ["Scorching Sands"] = ScorchingSandsY13b,
        ["Jungle Healing"] = JungleHealingY13b,
        ["Wicked Blow"] = WickedBlowY13b,
        ["Surging Strikes"] = SurgingStrikesY13b,
        ["Thunder Cage"] = ThunderCageY13b
    };

    // ------------------------------------------------------------------ shared by the whole batch

    /// <summary>A ring of <paramref name="n"/> sprites round a point, in the picture's plane, turning by <paramref name="turn"/>.</summary>
    private static void L5Ring(FxList fx, Vector3 centre, float rx, float ry, int n, FxShape shape, Color color, float size, float turn, FxBlend blend = FxBlend.Alpha, bool along = false)
    {
        for (int i = 0; i < n; i++)
        {
            float a = turn + i * MathF.Tau / n;
            fx.Sprite(shape, centre + new Vector3(MathF.Cos(a) * rx, MathF.Sin(a) * ry, 0.1f), size, color, blend, along ? a - MathF.PI / 2f : 0f);
        }
    }

    /// <summary>An arc of an ellipse from angle <paramref name="a0"/> to <paramref name="a1"/> drawn as a ribbon: a crescent, a sweep.</summary>
    private static void L5Arc(FxList fx, Vector3 centre, float rx, float ry, float a0, float a1, float width, Color color, int n = 14, FxBlend blend = FxBlend.Alpha)
    {
        var pts = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float a = a0 + (a1 - a0) * i / n;
            pts[i] = centre + new Vector3(MathF.Cos(a) * rx, MathF.Sin(a) * ry, 0.12f);
        }
        fx.Ribbon(pts, width, color, blend);
    }

    /// <summary>A wheel facing the camera: a rim, six spokes and a hub, turned by <paramref name="turn"/>.</summary>
    private static void L5Wheel(FxList fx, Vector3 at, float r, float turn, Color rim, Color spoke, float alpha)
    {
        L2Ellipse(fx, at, r, r, r * 0.16f, Fx.Fade(rim, alpha), 16, 0f, FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float a = turn + i * MathF.Tau / 6f;
            fx.Ribbon(new[] { at + new Vector3(0f, 0f, 0.1f), at + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r, 0.1f) }, r * 0.1f, Fx.Fade(spoke, alpha), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Dot, at + new Vector3(0f, 0f, 0.15f), r * 0.3f, Fx.Fade(spoke, alpha), FxBlend.Alpha);
    }

    /// <summary>Motes that fly in from all round and gather on a point between two moments: a charge.</summary>
    private static void L5Gather(FxList fx, float age, int seed, Vector3 at, float h, float t0, float t1, int count, FxShape shape, Color a, Color b, float size, float from)
    {
        float t = (age - t0) / (t1 - t0);
        if (t < 0f || t > 1f) return;
        for (int i = 0; i < count; i++)
        {
            float u = Math.Clamp(t * 1.25f - Fx.Rand(seed, i, 511) * 0.25f, 0f, 1f);
            var off = Fx.Direction(seed, i) * h * from * (1f - Fx.EaseOut(u));
            fx.Sprite(shape, at + new Vector3(off.X, off.Y, 0.1f), h * size, Fx.Fade(Fx.Mix(a, b, Fx.Rand(seed, i, 512)), MathF.Sin(Math.Min(1f, u * 1.1f) * MathF.PI * 0.5f) * (1f - 0.7f * u)), FxBlend.Add);
        }
    }

    /// <summary>A soft glow over the target that swells, for the pause before a blow or the afterglow of one.</summary>
    private static void L5Halo(FxList fx, Vector3 at, float r, Color color, float amount) =>
        fx.Sprite(FxShape.Glow, at, r, Fx.Fade(color, amount), FxBlend.Alpha);

    /// <summary>A heavy prop (a hammer, a shield, a block) that drops or swings in from <paramref name="from"/> (in the target's heights) and lands at <paramref name="at"/>, then lingers and fades.</summary>
    private static void L5Slam(in FxCtx c, FxList fx, FxShape shape, Vector3 from, float size, Color color, Color rim, float rotation, float at = I, float lead = 0.2f)
    {
        float h = c.ToSize;
        float k = (c.Age - (at - lead)) / lead;
        if (k < 0f) return;
        float after = Math.Max(0f, (c.Age - at) / 0.35f);
        if (after > 1f || (c.Cue.Missed && k > 1f)) return;
        var p = c.To + from * h * (1f - Fx.EaseOut(Math.Min(1f, k)) * (1f - 0.04f));
        float a = Math.Min(1f, k * 2.5f) * (1f - after);
        fx.Sprite(shape, p, h * size * (1f + 0.12f * after), Fx.Fade(color, a), FxBlend.Alpha, rotation * (1f - Fx.EaseOut(Math.Min(1f, k))));
        fx.Sprite(shape, p, h * size * 1.1f, Fx.Fade(rim, a * 0.4f), FxBlend.Add, rotation * (1f - Fx.EaseOut(Math.Min(1f, k))));
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Clangorous Soul: the user beats out a drum of scales, three gold-violet shock rings with notes beating out of its chest, and five arrows climb at once.</summary>
    private static void ClangorousSoulY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float t = 0.08f + i * 0.2f;
            PulseRing(fx, c.Age, c.From, t, 0.35f, h * 0.3f, h * (1.0f + 0.2f * i), i % 2 == 0 ? Rgb(255, 214, 90) : col.Main, col.Light);
            Fx.Burst(fx, c.Seed + i, c.Age, c.From, h, t, 0.4f, 5, FxShape.Note, Rgb(255, 230, 150), col.Light, 0.11f, 0.8f, 0.2f, FxBlend.Alpha, salt: 500 + i);
        }
        L5Ring(fx, c.From, h * 0.7f, h * 0.55f, 6, FxShape.Hex, Fx.Fade(Rgb(150, 130, 255), env * 0.8f), h * 0.14f, c.Age * 4f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.3f, 5, Rgb(255, 214, 110));
        L2Down(fx, c.Age, c.Seed, c.FromFeet, h * 0.6f, 0.7f, 1, Rgb(255, 120, 120));
    }

    /// <summary>Body Press: the user rears back and throws its whole weight at the foe, a broad orange body-shaped glow with an armour plate before it, and the ground rings under the crash.</summary>
    private static void BodyPressY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        if (c.Age < I)
        {
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Sprite(FxShape.Glow, p, c.FromSize * 2.0f, Fx.Fade(Rgb(255, 150, 70), 0.8f * k), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, p + new Vector3(c.FromSize * 0.4f, 0f, 0.2f), c.FromSize * 1.5f, Fx.Fade(Rgb(236, 110, 50), k), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, p + new Vector3(c.FromSize * 0.4f, 0f, 0.25f), c.FromSize * 1.0f, Fx.Fade(Rgb(255, 190, 120), 0.7f * k), FxBlend.Alpha);
            SpeedLines(c, fx, 0.04f, I, 5, 0.5f);
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.6f, Rgb(255, 170, 90));
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 8, FxShape.Smoke, Rgb(210, 190, 160, 200), Rgb(160, 140, 120, 200), 0.2f, 0.7f, -0.2f, FxBlend.Alpha, salt: 501);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Decorate: a garland of ribbon is swagged over the target with stars hanging from it, and confetti falls in every colour while two arrows climb.</summary>
    private static void DecorateY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float draw = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        for (int g = 0; g < 2; g++)
        {
            var pts = new Vector3[13];
            for (int i = 0; i <= 12; i++)
            {
                float u = i / 12f * draw;
                pts[i] = c.ToFeet + new Vector3((u - 0.5f) * h * 1.8f, h * (1.15f - g * 0.25f) - MathF.Sin(u * MathF.PI) * h * 0.3f, 0.2f);
            }
            fx.Ribbon(pts, h * 0.05f, Fx.Fade(g == 0 ? Rgb(255, 120, 190) : Rgb(255, 214, 90), env), FxBlend.Alpha);
        }
        for (int i = 0; i < 4; i++)
        {
            float u = (i + 0.5f) / 4f;
            fx.Sprite(FxShape.Star, c.ToFeet + new Vector3((u - 0.5f) * h * 1.8f, h * (0.95f - MathF.Sin(u * MathF.PI) * 0.25f), 0.3f), h * 0.16f * draw, Fx.Fade(Rgb(255, 240, 150), env), FxBlend.Alpha);
        }
        Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, h, 0.1f, 0.95f, 0.55f, 14, FxShape.Dot, Rgb(255, 150, 200), Rgb(120, 200, 255), 0.07f, FxBlend.Alpha);
        Fx.Rain(fx, c.Age, c.Seed + 3, c.ToFeet, h, 0.15f, 0.95f, 0.55f, 8, FxShape.Petal, Rgb(255, 224, 120), Rgb(170, 240, 170), 0.09f, FxBlend.Alpha);
        L3Up(fx, c.Age, c.ToFeet, h, 0.3f, 2, Rgb(255, 160, 210));
    }

    /// <summary>Drum Beating: two sticks beat a stump three times, a green ring thumping out across the ground at each beat, the last through the foe, with notes and roots.</summary>
    private static void DrumBeatingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.8f, 1.0f);
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.24f + i * 0.12f;
            GroundRing(fx, c.Age, c.ToFeet, t, 0.4f, h * (1.3f + 0.5f * i), i == 2 ? col.Light : col.Main);
            float beat = (c.Age - t) / 0.35f;
            if (beat > 0f && beat < 1f) fx.Sprite(FxShape.Ring, c.ToFeet + new Vector3(0f, h * 0.45f, 0.3f), h * (0.6f + 0.7f * beat), Fx.Fade(i == 2 ? col.Light : col.Main, 1f - beat), FxBlend.Alpha);
            for (int s = -1; s <= 1; s += 2)
            {
                float k = (c.Age - t + 0.05f) / 0.1f;
                if (k > 0f && k < 1f) fx.Streak(FxShape.Streak, c.ToFeet + new Vector3(s * h * 0.25f, h * (0.5f - 0.35f * k), 0.2f), new Vector3(s, 1f, 0f), h * 0.1f, 4f, Fx.Fade(Rgb(150, 110, 60), 1f), FxBlend.Alpha);
            }
            if (c.Age > t && c.Age < t + 0.3f) fx.Sprite(FxShape.Note, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 520) * h * 0.5f, h * (0.9f + (c.Age - t)), 0.2f), h * 0.14f, Fx.Fade(col.Light, 1f - (c.Age - t) / 0.3f), FxBlend.Alpha);
        }
        if (c.Cue.Landed && env > 0f)
        {
            for (int i = 0; i < 5; i++)
                fx.Ribbon(new[] { c.ToFeet + new Vector3((i - 2) * h * 0.3f, 0.05f, 0.1f), c.ToFeet + new Vector3((i - 2) * h * 0.4f, h * 0.4f * Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f)), 0.1f) }, h * 0.04f, Fx.Fade(Rgb(110, 84, 50), env), FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Snap Trap: a pair of toothed green jaws snaps shut on the foe from either side, and a ring of vine cinches round it and holds.</summary>
    private static void SnapTrapY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - (I - 0.25f)) / 0.25f, 0f, 1f);
        float gap = h * 0.7f * (1f - Fx.EaseOut(k));
        float env = Fx.Envelope(c.Age, I - 0.25f, I - 0.2f, 0.95f, 1.15f);
        if (env > 0f)
            for (int s = -1; s <= 1; s += 2)
                for (int j = -1; j <= 1; j++)
                {
                    var at = c.To + new Vector3(s * (gap + h * 0.12f), j * h * 0.3f, 0.2f);
                    fx.Sprite(FxShape.Fang, at, h * 0.3f, Fx.Fade(Rgb(70, 170, 70), env), FxBlend.Alpha, s * MathF.PI / 2f);
                    fx.Sprite(FxShape.Leaf, at + new Vector3(s * h * 0.2f, 0f, -0.05f), h * 0.22f, Fx.Fade(Rgb(110, 210, 90), env * 0.9f), FxBlend.Alpha, s * 1.2f);
                }
        if (c.Cue.Landed)
        {
            for (int b = 0; b < 2; b++)
                L2Ellipse(fx, c.To + new Vector3(0f, (b - 0.5f) * h * 0.35f, 0f), h * 0.55f, h * 0.2f, h * 0.05f, Fx.Fade(Rgb(80, 190, 80), Fx.Envelope(c.Age, I, I + 0.05f, 1.0f, 1.2f)), 14, 0f, FxBlend.Alpha);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Pyro Ball: a ball of pebbles lit through with fire is kicked off, burning a long tail of flame and embers all the way, and bursts into flame on the foe.</summary>
    private static void PyroBallY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.05f) / (I - 0.05f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.05f);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 0.7f, Fx.Fade(Rgb(255, 120, 40), 0.7f));
            fx.Sprite(FxShape.Rock, p, c.FromSize * 0.3f, Fx.Fade(Rgb(120, 96, 80), 1f), FxBlend.Alpha, k * 9f);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.38f, Fx.Fade(Rgb(255, 170, 50), 0.9f));
            for (int i = 1; i <= 5; i++)
                fx.Sprite(FxShape.Flame, Lob(c.From, c.Aim, Math.Max(0f, k - i * 0.07f), 0.05f), c.FromSize * (0.3f - 0.04f * i), Fx.Fade(col.Main, 0.8f - 0.12f * i));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Rock, Rgb(130, 104, 86), Rgb(90, 74, 64), 0.1f, 1.0f, 1.8f, FxBlend.Alpha, salt: 502);
            fx.Sprite(FxShape.Flame, c.To + Vector3.UnitY * h * 0.1f, h * 1.2f * Fx.Envelope(c.Age, I, I + 0.08f, I + 0.25f, I + 0.55f), Fx.Fade(col.Main, 0.9f));
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Behemoth Blade: a sword taller than the foe comes straight down through it from above, a bright line of cut left standing, and the ground splits under the great weight.</summary>
    private static void BehemothBladeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L5Slam(c, fx, FxShape.Sword, new Vector3(0f, 1.9f, 0f), 1.6f, Rgb(160, 176, 204), Rgb(220, 232, 255), 0f, I, 0.22f);
        float line = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.25f, I + 0.55f);
        if (line > 0f && c.Cue.Landed)
        {
            fx.Ribbon(new[] { c.To + Vector3.UnitY * h * 1.4f, c.To - Vector3.UnitY * h * 0.6f }, h * 0.08f, Fx.Fade(White, line), FxBlend.Add);
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.8f, Fx.Fade(Rgb(90, 96, 110), line), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.4f, h * 1.8f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 1.0f, 0.3f);
        }
        Impact(c, col, fx, 1.4f);
    }

    /// <summary>Behemoth Bash: a round shield as broad as the foe is swung in from the side and bashed against it, bossed with a cross, and three rings of dent run out from the blow.</summary>
    private static void BehemothBashY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L5Slam(c, fx, FxShape.Orb, new Vector3(-2.2f, 0.3f, 0f), 1.5f, Rgb(130, 142, 168), Rgb(190, 204, 230), 0f, I, 0.22f);
        L5Slam(c, fx, FxShape.Cross, new Vector3(-2.2f, 0.3f, 0.1f), 0.8f, Rgb(80, 90, 116), Rgb(200, 210, 235), 0f, I, 0.22f);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 3; i++) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + i * 0.06f, 0.4f, h * (0.8f + 0.5f * i), Fx.Fade(i == 0 ? White : col.Main, 0.9f));
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Aura Wheel: a wheel of yellow and violet spokes spins up at the user's feet and rolls to the foe, trailing sparks, and the user's Speed arrow rises.</summary>
    private static void AuraWheelY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        if (c.Age < I + 0.05f)
        {
            var p = Vector3.Lerp(c.FromFeet + new Vector3(0f, h * 0.35f, 0.1f), c.ToFeet + new Vector3(0f, h * 0.35f, 0.1f), Fx.EaseOut(k));
            L5Wheel(fx, p, h * 0.8f, c.Age * 16f, Rgb(255, 222, 50), Rgb(150, 70, 210), 1f);
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(Rgb(255, 230, 90), 0.4f));
            Fx.Stream(fx, c, 0.04f, I, 0.18f, 6, FxShape.Spark, Rgb(255, 240, 120), Rgb(190, 110, 255), 0.1f, spread: 0.2f, trail: 2f);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.5f, 0.2f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.5f, 1, Rgb(255, 230, 110));
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Breaking Swipe: the user's tail sweeps a wide violet crescent across the whole side, and a down arrow sinks over each foe it struck.</summary>
    private static void BreakingSwipeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float sweep = Fx.EaseOut(Math.Clamp((c.Age - (I - 0.2f)) / 0.2f, 0f, 1f));
        float env = Fx.Envelope(c.Age, I - 0.2f, I - 0.12f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            L5Arc(fx, c.To + new Vector3(0f, -h * 0.1f, 0f), h * 1.3f, h * 0.7f, 2.6f, 2.6f - 3.6f * sweep, h * 0.22f, Fx.Fade(Rgb(120, 100, 250), env), 16);
            L5Arc(fx, c.To + new Vector3(0f, -h * 0.1f, 0f), h * 1.3f, h * 0.7f, 2.6f, 2.6f - 3.6f * sweep, h * 0.09f, Fx.Fade(Rgb(214, 200, 255), env), 16, FxBlend.Add);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Wisp, Rgb(150, 130, 255), White, 0.12f, 0.9f, salt: 503);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.08f, 2, Rgb(180, 160, 255));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Branch Poke: a leafy twig is jabbed twice, quick and small, a leaf flicking off at each poke.</summary>
    private static void BranchPokeY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 2; i++)
        {
            float t = I - 0.1f + i * 0.1f;
            float k = (c.Age - (t - 0.1f)) / 0.1f;
            if (k < 0f || c.Age > t + 0.2f) continue;
            var dir = Vector3.Normalize(c.Aim - c.From);
            var tip = Vector3.Lerp(c.From + dir * c.FromSize * 0.4f, c.Aim, Fx.EaseOut(Math.Min(1f, k)));
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.2f);
            fx.Ribbon(new[] { tip - dir * c.FromSize * 1.1f, tip }, h * 0.13f, Fx.Fade(Rgb(120, 84, 50), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Leaf, tip, h * 0.42f, Fx.Fade(col.Main, fade), FxBlend.Alpha, 0.6f + i);
            fx.Sprite(FxShape.Leaf, tip - dir * h * 0.3f + new Vector3(0f, h * 0.15f, 0f), h * 0.32f, Fx.Fade(col.Light, fade), FxBlend.Alpha, -0.7f - i);
            if (c.Cue.Landed && c.Age >= t) Fx.Burst(fx, c.Seed + i, c.Age, tip, h, t, 0.35f, 3, FxShape.Leaf, col.Main, col.Light, 0.1f, 0.8f, 0.8f, FxBlend.Alpha, salt: 504 + i);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Overdrive: a wall of distorted guitar, yellow rings and zigzag bolts roaring out of the user with notes riding them, the foe rattled in the din.</summary>
    private static void OverdriveY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Fx.Waves(fx, c, 0f, 0.6f, 5, FxShape.Ring, Rgb(255, 232, 80), 0.6f, 0.3f);
        Fx.Stream(fx, c, 0.05f, 0.5f, 0.28f, 5, FxShape.Note, Rgb(255, 250, 200), Rgb(255, 200, 70), 0.14f, spread: 0.5f, blend: FxBlend.Alpha);
        float z = Fx.Envelope(c.Age, 0.1f, 0.2f, I, I + 0.2f);
        if (z > 0f)
            for (int r = -1; r <= 1; r++)
            {
                var pts = new Vector3[11];
                for (int i = 0; i <= 10; i++)
                    pts[i] = Vector3.Lerp(c.From, c.Aim, i / 10f) + new Vector3(0f, r * h * 0.3f + ((i % 2 == 0) ? 1f : -1f) * h * 0.1f * (0.3f + i / 10f), 0.1f);
                fx.Ribbon(pts, h * 0.04f, Fx.Fade(Rgb(255, 238, 110), z), FxBlend.Add);
            }
        if (c.Cue.Landed)
        {
            Fx.Bolt(fx, c.Age, c.Seed, c.To + Vector3.UnitY * c.ToSize * 0.9f, c.To - Vector3.UnitY * c.ToSize * 0.2f, I, I + 0.2f, c.ToSize * 0.05f, col.Main, col.Light, 2);
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Apple Acid: a green apple is lobbed and splits on the foe, acid splashing and sizzling over it in bubbles and smoke while a down arrow sinks.</summary>
    private static void AppleAcidY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.03f) / (I - 0.03f);
        var acid = Rgb(170, 240, 70);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.05f);
            fx.Sprite(FxShape.Orb, p, c.FromSize * 0.7f, Fx.Fade(Rgb(120, 200, 60), 1f), FxBlend.Alpha, k * 5f);
            fx.Sprite(FxShape.Leaf, p + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize * 0.3f, Fx.Fade(Rgb(60, 140, 50), 1f), FxBlend.Alpha, 0.5f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Drop, acid, Rgb(230, 255, 150), 0.1f, 1.0f, 1.8f, FxBlend.Alpha, salt: 505);
            Fx.Rain(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.2f, h * 0.6f, I, 1.0f, 0.4f, 6, FxShape.Bubble, acid, White, 0.1f, FxBlend.Alpha);
            Rising(fx, c.Age, c.Seed, c.To, h, I, 1.1f, 3, FxShape.Smoke, Rgb(190, 230, 120, 190), 0.16f, 0.08f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(190, 240, 90));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Grav Apple: a great apple drops from far overhead and thuds down on the foe, a violet ring of gravity pressing the ground flat under it.</summary>
    private static void GravAppleY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        L5Slam(c, fx, FxShape.Orb, new Vector3(0f, 3.0f, 0f), 1.1f, Rgb(214, 70, 60), Rgb(255, 150, 130), 0f, I, 0.3f);
        L5Slam(c, fx, FxShape.Leaf, new Vector3(0.1f, 3.5f, 0.05f), 0.5f, Rgb(70, 170, 70), Rgb(160, 240, 130), 0.8f, I, 0.3f);
        float press = Fx.Envelope(c.Age, I - 0.25f, I - 0.1f, 0.9f, 1.1f);
        if (press > 0f)
        {
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * (1.8f - 0.5f * Math.Min(1f, press)), Fx.Fade(Rgb(150, 100, 230), press), FxBlend.Alpha);
            fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.4f, Fx.Fade(Rgb(120, 70, 190), press * 0.5f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 7, FxShape.Smoke, Rgb(180, 160, 190, 200), Rgb(130, 110, 150, 200), 0.2f, 0.6f, -0.2f, FxBlend.Alpha, salt: 506);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 2, Rgb(190, 160, 255));
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Spirit Break: a pink fist-shaped blow breaks a heart in two over the foe, the halves flying apart, and the crack runs out as a shiver of white.</summary>
    private static void SpiritBreakY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.3f, 0f), I, 0.65f, Rgb(255, 170, 220), 0f, 0.14f);
        float k = (c.Age - I) / 0.5f;
        if (c.Cue.Landed && k > 0f && k < 1f)
        {
            float away = Fx.EaseOut(k) * h * 0.45f;
            float fade = 1f - k;
            fx.Sprite(FxShape.Heart, c.To + new Vector3(-away, away * 0.3f, 0.3f), h * 0.5f, Fx.Fade(Rgb(255, 130, 190), fade), FxBlend.Alpha, 0.3f + k);
            fx.Sprite(FxShape.Heart, c.To + new Vector3(away, -away * 0.3f, 0.3f), h * 0.5f, Fx.Fade(Rgb(255, 130, 190), fade), FxBlend.Alpha, -0.3f - k);
            fx.Sprite(FxShape.Crack, c.To + new Vector3(0f, 0f, 0.4f), h * 0.8f, Fx.Fade(White, fade), FxBlend.Add);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.1f, 1, Rgb(255, 160, 210));
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Strange Steam: pink and white clouds of steam pour out of the user and curl round the foe in a swirl, odd stars wheeling over its head.</summary>
    private static void StrangeSteamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.45f, 0.3f, 9, FxShape.Smoke, Rgb(255, 214, 240, 220), Rgb(255, 255, 255, 220), 0.3f, arc: 0.05f, spread: 0.5f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.95f, 1.15f);
            fx.Sprite(FxShape.Swirl, c.To, h * 1.2f, Fx.Fade(Rgb(255, 180, 230), env * 0.8f), FxBlend.Alpha, c.Age * 3f);
            fx.Sprite(FxShape.Swirl, c.To, h * 0.8f, Fx.Fade(White, env * 0.6f), FxBlend.Alpha, -c.Age * 4f);
            L5Ring(fx, c.ToFeet + new Vector3(0f, h * 1.05f, 0f), h * 0.4f, h * 0.1f, 3, FxShape.Star, Fx.Fade(Rgb(255, 236, 130), env), h * 0.13f, c.Age * 5f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Life Dew: beads of dew shine on the user and its side and fall in a soft rain onto them, each landing as a small ring of blue-white light, and a green glow mends them.</summary>
    private static void LifeDewY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.05f);
        if (env <= 0f) return;
        Fx.Rain(fx, c.Age, c.Seed, c.FromFeet, h, 0f, 0.85f, 0.45f, 14, FxShape.Drop, Rgb(150, 220, 255), White, 0.17f, FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float t = 0.25f + i * 0.14f;
            GroundRing(fx, c.Age, c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 530) * h * 0.4f, 0f, 0.1f), t, 0.35f, h * 0.45f, Rgb(170, 230, 255));
        }
        fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(Rgb(150, 235, 220), env * 0.35f), FxBlend.Alpha);
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, 0.35f);
    }

    /// <summary>Obstruct: the user crosses its arms in a broad dark barrier, a red X glaring on a violet pane, and a flash of red where a blow would be stopped.</summary>
    private static void ObstructY13b(in FxCtx c, FxColors col, FxList fx)
    {
        L4Panel(c, fx, Rgb(70, 40, 100), Rgb(220, 70, 90), 0.85f);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.08f, 0.2f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var at = c.FromFeet + L1Toward(c) * h * 0.2f + new Vector3(h * 0.35f, h * 0.52f, 0.4f);
        fx.Sprite(FxShape.Cross, at, h * 0.6f, Fx.Fade(Rgb(240, 70, 80), env), FxBlend.Alpha, MathF.PI / 4f);
        fx.Sprite(FxShape.Cross, at, h * 0.66f, Fx.Fade(Rgb(255, 160, 170), env * 0.4f), FxBlend.Add, MathF.PI / 4f);
        Fx.Glint(fx, c.Age, at, h, 0.4f, Rgb(255, 120, 130), 0.5f);
    }

    /// <summary>False Surrender: a white flag is waved over the user's head, then lowered, and a dark blade stabs in from behind the foe with a red glint.</summary>
    private static void FalseSurrenderY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float flag = Fx.Envelope(c.Age, 0f, 0.06f, I - 0.12f, I - 0.04f);
        if (flag > 0f)
        {
            var pole = c.FromFeet + new Vector3(h * 0.2f, h * 0.95f, 0.2f);
            float wave = MathF.Sin(c.Age * 26f);
            fx.Ribbon(new[] { pole - Vector3.UnitY * h * 0.4f + new Vector3(wave * h * 0.04f, 0f, 0f), pole + Vector3.UnitY * h * 0.35f }, h * 0.06f, Fx.Fade(Rgb(190, 170, 140), flag), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, pole + new Vector3(h * (0.3f + 0.05f * wave), h * 0.26f, 0f), h * 0.55f, Fx.Fade(White, flag), FxBlend.Alpha, wave * 0.2f);
        }
        if (c.Cue.Landed)
        {
            Strike(c, fx, FxShape.Sword, new Vector3(1.2f, 0.9f, 0f), I, 0.8f, Rgb(70, 50, 90), 2.3f, 0.12f);
            Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.04f, 1, 0.05f, Rgb(200, 60, 80), White);
            Fx.Glint(fx, c.Age, c.To + new Vector3(c.ToSize * 0.2f, c.ToSize * 0.2f, 0.2f), c.ToSize, I, Rgb(255, 90, 100), 0.6f);
            Jolt(fx, c.Age, I, 0.5f, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Meteor Assault: a stalk as big as the user is swung overhead and brought down like a falling star, white at the root and green at the leaves, a ring of orange sparks flying from the crash.</summary>
    private static void MeteorAssaultY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - (I - 0.3f)) / 0.3f, 0f, 1f);
        float env = Fx.Envelope(c.Age, I - 0.3f, I - 0.2f, I + 0.1f, I + 0.4f);
        if (env > 0f)
        {
            float a0 = 2.0f - 2.2f * Fx.EaseOut(k);
            var root = c.To + new Vector3(h * 1.2f, -h * 0.2f, 0.2f);
            var tip = root + new Vector3(MathF.Cos(a0) * h * 1.5f, MathF.Sin(a0) * h * 1.5f, 0f);
            fx.Ribbon(new[] { root, Vector3.Lerp(root, tip, 0.55f), tip }, h * 0.3f, Fx.Fade(Rgb(80, 180, 80), env), FxBlend.Alpha);
            fx.Ribbon(new[] { root, Vector3.Lerp(root, tip, 0.5f) }, h * 0.22f, Fx.Fade(White, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Leaf, tip, h * 0.7f, Fx.Fade(Rgb(120, 220, 90), env), FxBlend.Alpha, a0);
            fx.Sprite(FxShape.Star, tip, h * 0.5f, Fx.Fade(Rgb(255, 200, 110), env * 0.6f), FxBlend.Add, k * 6f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Ember, Rgb(255, 170, 70), White, 0.09f, 1.2f, 1.0f, salt: 507);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 1.8f, Fx.Fade(Rgb(255, 150, 70), 0.9f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.5f);
    }

    /// <summary>Eternabeam: a magenta-black beam of the Eternal Flower's power, a tight helix of white wound round it and great stars blown off, the whole field flashing white at the blast.</summary>
    private static void EternabeamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var mag = Rgb(220, 60, 190);
        float charge = Fx.Envelope(c.Age, 0f, 0.1f, 0.16f, 0.2f);
        if (charge > 0f) L5Gather(fx, c.Age, c.Seed, c.From + new Vector3(h * 0.5f, 0f, 0.2f), h, 0f, 0.2f, 8, FxShape.Star, mag, White, 0.14f, 1.2f);
        Fx.Beam(fx, c, 0.18f, 0.7f, h * 0.38f, Fx.Fade(mag, 0.9f), White, 14);
        float beam = Fx.Envelope(c.Age, 0.18f, 0.24f, 0.62f, 0.7f);
        if (beam > 0f)
        {
            var dir = Vector3.Normalize(c.Aim - c.From);
            var pts = new Vector3[17];
            for (int i = 0; i <= 16; i++)
            {
                float u = i / 16f;
                pts[i] = Vector3.Lerp(c.From, c.Aim, u) + new Vector3(0f, MathF.Sin(u * 18f - c.Age * 30f) * h * 0.28f, MathF.Cos(u * 18f - c.Age * 30f) * h * 0.1f);
            }
            fx.Ribbon(pts, h * 0.07f, Fx.Fade(White, beam), FxBlend.Add);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 7, FxShape.Star, mag, White, 0.2f, 1.1f, 0.2f, salt: 508);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.5f, c.ToSize * 2.2f, Fx.Fade(mag, 0.9f));
            fx.ScreenFlash(Rgb(255, 200, 250), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.06f, I + 0.26f));
            Jolt(fx, c.Age, I, 1.0f, 0.4f);
        }
        Impact(c, col, fx, 1.5f);
    }

    /// <summary>Steel Beam: a brilliant silver beam is fired from the shining spikes on the user's body, shards of steel flying off it, and the user flares red at the cost.</summary>
    private static void SteelBeamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            float f = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.3f);
            if (f > 0f) fx.Streak(FxShape.Shard, c.From + new Vector3(MathF.Cos(a) * h * 0.4f, MathF.Sin(a) * h * 0.4f, 0.1f), new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.1f, 2.5f, Fx.Fade(Rgb(214, 222, 240), f), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.12f, 0.6f, h * 0.26f, Fx.Fade(Rgb(150, 170, 210), 0.9f), White, 12);
        Fx.Stream(fx, c, 0.14f, 0.55f, 0.2f, 7, FxShape.Shard, White, Rgb(170, 184, 214), 0.1f, spread: 0.35f, blend: FxBlend.Alpha, spin: 4f);
        float cost = Fx.Envelope(c.Age, 0.5f, 0.58f, 0.75f, 0.95f);
        if (cost > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.3f, Fx.Fade(Rgb(240, 60, 60), cost * 0.6f), FxBlend.Alpha);
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.8f, 0.3f);
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Expanding Force: pink rings of psychic force swell out from the user one after another, each wider than the last, reaching across the field to wash over the foe.</summary>
    private static void ExpandingForceY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float reach = Vector3.Distance(c.From, c.Aim);
        for (int i = 0; i < 4; i++)
        {
            float t = 0.02f + i * 0.1f;
            float k = (c.Age - t) / (I - t + 0.1f);
            if (k < 0f || k > 1f) continue;
            float grow = Fx.EaseOut(k);
            var centre = Vector3.Lerp(c.From, c.Aim, grow * 0.8f);
            L2Ellipse(fx, centre, h * (0.25f + 0.55f * grow), h * (0.4f + 0.7f * grow), h * 0.11f, Fx.Fade(i % 2 == 0 ? Rgb(255, 120, 200) : Rgb(190, 130, 255), 1f - k * 0.5f), 18, 0f, FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.5f, Rgb(255, 150, 215), White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 6, FxShape.Star, Rgb(255, 170, 220), White, 0.12f, 0.9f, salt: 509);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Steel Roller: a huge steel drum rolls across the foe and the ground, flattening it, and the plates of the field fly up in shards and fall back flat.</summary>
    private static void SteelRollerY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.05f) / 0.6f, 0f, 1f);
        float env = Fx.Envelope(c.Age, 0.05f, 0.14f, 0.7f, 0.95f);
        if (env > 0f)
        {
            var at = c.ToFeet + new Vector3(k * h * 4.0f - h * 2.2f, h * 0.55f, 0.35f);
            fx.Sprite(FxShape.Orb, at, h * 1.5f, Fx.Fade(Rgb(150, 162, 186), env), FxBlend.Alpha);
            L2Ellipse(fx, at, h * 0.65f, h * 0.65f, h * 0.07f, Fx.Fade(White, env * 0.9f), 14, c.Age * 10f, FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
                fx.Ribbon(new[] { at + new Vector3(0f, h * (0.3f - 0.2f * i), 0.1f), at + new Vector3(-h * 0.8f, h * (0.3f - 0.2f * i), 0.1f) }, h * 0.035f, Fx.Fade(White, env * 0.5f), FxBlend.Add);
            fx.Flat(FxShape.Hex, c.ToFeet + new Vector3((at.X - c.ToFeet.X) - h * 0.7f, 0.05f, 0f), Vector3.UnitY, h * 1.4f, Fx.Fade(Rgb(120, 130, 150), env * 0.6f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.7f, 9, FxShape.Shard, Rgb(180, 190, 210), White, 0.12f, 0.9f, 2.0f, FxBlend.Alpha, salt: 510);
            Jolt(fx, c.Age, I, 0.9f, 0.3f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Scale Shot: a volley of indigo scales is fired in a quick stream, each stinging in turn, and the user's Speed arrow rises as its Defense arrow sinks.</summary>
    private static void ScaleShotY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.14f, 12, FxShape.Hex, Rgb(110, 90, 250), Rgb(230, 224, 255), 0.22f, spread: 0.22f, spin: 3f, blend: FxBlend.Alpha, trail: 1.5f);
        if (c.Cue.Landed)
            for (int i = 0; i < 4; i++)
                Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(Fx.Signed(c.Seed, i, 540), Fx.Signed(c.Seed, i, 541), 0f) * h * 0.2f, h, I - 0.2f + i * 0.07f, 0.16f, col.Main, col.Light);
        L3Up(fx, c.Age, c.FromFeet, c.FromSize, 0.6f, 1, Rgb(190, 180, 255));
        L2Down(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, 0.7f, 1, Rgb(255, 130, 130));
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Meteor Beam: stones gather in a glowing orb over the user, then a beam of brown-gold light fires with small meteors in it, and a Sp. Atk arrow rises.</summary>
    private static void MeteorBeamY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var orbAt = c.From + new Vector3(0f, h * 0.7f, 0.2f);
        float orb = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.26f, 0.34f);
        L5Gather(fx, c.Age, c.Seed, orbAt, h, 0f, 0.26f, 9, FxShape.Rock, Rgb(190, 160, 110), Rgb(110, 92, 70), 0.14f, 1.4f);
        if (orb > 0f)
        {
            fx.Sprite(FxShape.Glow, orbAt, h * 0.9f * orb, Fx.Fade(Rgb(255, 214, 120), 0.8f));
            fx.Sprite(FxShape.Orb, orbAt, h * 0.4f * orb, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        Fx.Beam(fx, c, 0.2f, 0.62f, h * 0.28f, Fx.Fade(Rgb(210, 160, 80), 0.85f), Rgb(255, 244, 200), 10);
        Fx.Stream(fx, c, 0.22f, 0.58f, 0.2f, 6, FxShape.Rock, Rgb(160, 130, 90), Rgb(90, 76, 60), 0.12f, spread: 0.25f, blend: FxBlend.Alpha, spin: 4f);
        L3Up(fx, c.Age, c.FromFeet, h, 0.55f, 1, Rgb(255, 214, 120));
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.8f, 0.3f);
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Shell Side Arm: a spiral-barrelled muzzle ring flares at the user's side and a purple glob of sludge with a whirl in it is shot out, splashing over the foe.</summary>
    private static void ShellSideArmY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var muzzle = c.From + new Vector3(h * 0.5f, h * 0.05f, 0.2f);
        float flare = Fx.Envelope(c.Age, 0f, 0.05f, 0.14f, 0.24f);
        if (flare > 0f)
        {
            fx.Sprite(FxShape.Swirl, muzzle, h * 0.6f, Fx.Fade(Rgb(214, 130, 250), flare), FxBlend.Alpha, c.Age * 12f);
            fx.Sprite(FxShape.Ring, muzzle, h * 0.5f, Fx.Fade(White, flare * 0.6f));
        }
        float k = (c.Age - 0.1f) / (I - 0.1f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(muzzle, c.Aim, k);
            fx.Sprite(FxShape.Orb, p, h * 0.55f, Fx.Fade(Rgb(150, 70, 200), 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Swirl, p, h * 0.5f, Fx.Fade(Rgb(240, 190, 255), 0.9f), FxBlend.Add, k * 10f);
            for (int i = 1; i <= 3; i++) fx.Sprite(FxShape.Drop, Vector3.Lerp(muzzle, c.Aim, Math.Max(0f, k - 0.08f * i)), h * 0.1f, Fx.Fade(Rgb(172, 78, 210), 0.8f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Drop, Rgb(172, 78, 210), Rgb(236, 180, 250), 0.1f, 0.9f, 1.8f, FxBlend.Alpha, salt: 511);
            Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, c.ToSize, I, 1.0f, 0.4f, 4, FxShape.Bubble, Rgb(200, 120, 240), White, 0.1f, FxBlend.Alpha);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Misty Explosion: the user fills with pink mist, glows and bursts in a great dome of fine mist that rolls over everyone, a white flash at its height.</summary>
    private static void MistyExplosionY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var mid = c.From;
        float build = Fx.Envelope(c.Age, 0f, 0.15f, I - 0.04f, I);
        if (build > 0f)
        {
            fx.Sprite(FxShape.Glow, mid, h * (0.7f + 0.5f * build), Fx.Fade(Rgb(255, 170, 224), build * 0.9f));
            L5Gather(fx, c.Age, c.Seed, mid, h, 0f, I, 10, FxShape.Smoke, Rgb(255, 214, 240), White, 0.2f, 1.1f);
        }
        float boom = Fx.Envelope(c.Age, I, I + 0.06f, I + 0.35f, I + 0.75f);
        if (boom > 0f)
        {
            float grow = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.5f, 0f, 1f));
            fx.Sprite(FxShape.Smoke, mid, h * (1f + 3.0f * grow), Fx.Fade(Rgb(255, 196, 232, 230), boom * 0.45f), FxBlend.Alpha, c.Age);
            fx.Sprite(FxShape.Smoke, mid + new Vector3(h * 0.5f, 0f, 0.1f), h * (0.8f + 2.2f * grow), Fx.Fade(White, boom * 0.3f), FxBlend.Alpha, -c.Age);
            Fx.Shockwave(fx, c.Age, mid, Vector3.Zero, I, 0.5f, h * 4f, Fx.Fade(Rgb(255, 200, 235), 0.9f));
            fx.ScreenFlash(Rgb(255, 224, 242), 0.35f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.3f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Grassy Glide: the user slides along the ground to the foe on a green lane of flattened grass, blades of it flying up behind, and strikes as it passes.</summary>
    private static void GrassyGlideY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I + 0.3f)
        {
            float env = Fx.Envelope(c.Age, 0f, 0.06f, I + 0.1f, I + 0.3f);
            var a = c.FromFeet + Vector3.UnitY * 0.05f;
            var b = c.ToFeet + Vector3.UnitY * 0.05f;
            fx.Ribbon(new[] { a, Vector3.Lerp(a, b, k) }, h * 0.35f, Fx.Fade(Rgb(90, 190, 80), env * 0.8f), FxBlend.Alpha);
            fx.Ribbon(new[] { a, Vector3.Lerp(a, b, k) }, h * 0.1f, Fx.Fade(Rgb(190, 250, 140), env), FxBlend.Alpha);
            for (int i = 0; i < 9; i++)
            {
                float u = Math.Clamp(k - i * 0.07f, 0f, 1f);
                var p = Vector3.Lerp(a, b, u) + new Vector3(0f, h * (0.1f + 0.3f * Fx.Rand(c.Seed, i, 550)) * (1f - u * 0.3f), Fx.Signed(c.Seed, i, 551) * 0.2f);
                fx.Sprite(FxShape.Leaf, p, h * 0.13f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env), FxBlend.Alpha, i * 1.3f + c.Age * 5f);
            }
        }
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.03f, 2, 0.05f, col.Main, White);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Rising Voltage: the ground under the foe lights with a yellow ring, and three columns of lightning rise out of it one after another, each higher than the last.</summary>
    private static void RisingVoltageY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float ring = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 0.95f);
        if (ring > 0f)
        {
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.5f, Fx.Fade(col.Main, ring), FxBlend.Alpha);
            fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.8f, Fx.Fade(col.Main, ring * 0.45f), FxBlend.Alpha);
        }
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            float x = (i - 1) * h * 0.45f;
            Fx.Bolt(fx, c.Age, c.Seed + i, c.ToFeet + new Vector3(x, 0f, 0.1f), c.ToFeet + new Vector3(x * 0.8f, h * (0.9f + 0.4f * i), 0.1f), t, t + 0.25f, h * 0.06f, col.Main, col.Light, 1);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, col.Main, 0.1f, 1.0f, -0.3f, salt: 512);
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Terrain Pulse: a pulse the colour of the ground runs out of the user's feet in four hues at once, green, yellow, pink and violet, and an orb of the mixture is thrown.</summary>
    private static void TerrainPulseY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var hues = new[] { Rgb(110, 220, 90), Rgb(255, 224, 70), Rgb(255, 140, 210), Rgb(170, 130, 255) };
        for (int i = 0; i < 4; i++)
            GroundRing(fx, c.Age, c.FromFeet, 0.04f + i * 0.07f, 0.4f, h * (0.9f + 0.3f * i), hues[i]);
        float k = (c.Age - 0.1f) / (I - 0.1f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, k);
            fx.Sprite(FxShape.Glow, p, c.FromSize * 0.8f, Fx.Fade(White, 0.7f));
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Orb, p + new Vector3(MathF.Cos(k * 9f + i * 1.57f), MathF.Sin(k * 9f + i * 1.57f), 0f) * c.FromSize * 0.14f, c.FromSize * 0.2f, Fx.Fade(hues[i], 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
            for (int i = 0; i < 4; i++) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + i * 0.04f, 0.4f, c.ToSize * (0.8f + 0.25f * i), Fx.Fade(hues[i], 0.9f));
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Skitter Smack: the user scuttles in on a zigzag of small quick hops, little footprints left on the ground, and smacks the foe, a Sp. Atk arrow sinking.</summary>
    private static void SkitterSmackY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var a = c.FromFeet;
        var b = c.ToFeet;
        for (int i = 0; i < 7; i++)
        {
            float u = (i + 1) / 7f;
            float t = 0.03f + u * (I - 0.03f);
            if (c.Age < t) continue;
            float fade = Math.Max(0f, 1f - (c.Age - t) / 0.55f);
            var p = Vector3.Lerp(a, b, u * 0.85f) + new Vector3(0f, 0.06f + h * 0.15f * MathF.Max(0f, 1f - (c.Age - t) * 7f), (i % 2 == 0 ? 1f : -1f) * h * 0.25f);
            fx.Sprite(FxShape.Dot, p, h * 0.22f, Fx.Fade(Rgb(190, 230, 50), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.12f, 0f, 0f), h * 0.14f, Fx.Fade(Rgb(120, 150, 30), fade), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Strike(c, fx, FxShape.Palm, new Vector3(-1.2f, 0.2f, 0f), I, 0.5f, Rgb(214, 240, 110), 0f, 0.1f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, c.ToSize, I + 0.1f, 1, Rgb(210, 240, 110));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Burning Jealousy: green-hearted flames sweep across the foe's side from the user, orange at the edge, with an envious eye opening over each.</summary>
    private static void BurningJealousyY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(120, 236, 90);
        for (int i = 0; i < 9; i++)
        {
            float k = (c.Age - 0.04f - i * 0.025f) / 0.36f;
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.06f) + new Vector3(Fx.Signed(c.Seed, i, 560) * h * 0.2f, Fx.Signed(c.Seed, i, 561) * h * 0.2f, 0.05f);
            fx.Sprite(FxShape.Flame, p, c.FromSize * 0.36f, Fx.Fade(i % 3 == 0 ? col.Main : green, 0.9f - 0.3f * k));
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.8f, 1.0f);
            fx.Sprite(FxShape.Flame, c.To, h * 1.2f * env, Fx.Fade(green, 0.9f));
            fx.Sprite(FxShape.Eye, c.ToFeet + new Vector3(0f, h * 1.1f, 0.3f), h * 0.4f, Fx.Fade(Rgb(150, 255, 110), env), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Ember, Rgb(255, 160, 60), green, 0.08f, 1.0f, -0.8f, salt: 513);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Lash Out: the user flails in a rage, four red-black blows in a flurry from every side, jagged marks of fury flaring at each.</summary>
    private static void LashOutY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var sides = new[] { new Vector3(-1.2f, 0.6f, 0f), new Vector3(1.2f, 0.4f, 0f), new Vector3(-1.0f, -0.5f, 0f), new Vector3(0.2f, 1.2f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            float at = I - 0.18f + i * 0.06f;
            Strike(c, fx, FxShape.Claw, sides[i], at, 0.55f, Rgb(200, 40, 60), i * 1.3f, 0.08f);
            if (c.Cue.Landed) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(Fx.Signed(c.Seed, i, 570), Fx.Signed(c.Seed, i, 571), 0f) * h * 0.18f, h, at, 0.2f, Rgb(210, 40, 70), Rgb(255, 140, 150));
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.8f, 0.3f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Poltergeist: a teacup, a sword and a stone lift off in ghost-fire and wheel round the foe, then all three drop on it together.</summary>
    private static void PoltergeistY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var shapes = new[] { FxShape.Orb, FxShape.Sword, FxShape.Rock };
        float env = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.2f, I + 0.4f);
        for (int i = 0; i < 3; i++)
        {
            float k = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
            float a = c.Age * 7f + i * MathF.Tau / 3f;
            var orbit = c.To + new Vector3(MathF.Cos(a) * h * 0.9f * (1f - k), h * (0.55f + 0.3f * MathF.Sin(a * 0.5f)) * (1f - k * 0.9f), MathF.Sin(a) * 0.3f);
            if (env > 0f)
            {
                fx.Sprite(FxShape.Wisp, orbit, h * 0.9f, Fx.Fade(Rgb(150, 100, 255), env * 0.7f));
                fx.Sprite(shapes[i], orbit, h * 0.55f, Fx.Fade(i == 0 ? Rgb(240, 240, 250) : Rgb(150, 150, 180), env), FxBlend.Alpha, a);
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Wisp, Rgb(150, 100, 255), Rgb(220, 190, 255), 0.14f, 0.9f, -0.3f, salt: 514);
            Jolt(fx, c.Age, I, 0.7f, 0.25f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Corrosive Gas: a green-yellow cloud billows round the foe, its held item eaten away in a hiss of bubbles and a speck that shrinks to nothing.</summary>
    private static void CorrosiveGasY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.35f, 0f, 1f));
        for (int i = 0; i < 5; i++)
            fx.Sprite(FxShape.Smoke, c.To + new Vector3((i - 2) * h * 0.4f, (i % 2 - 0.3f) * h * 0.3f, 0.1f * i), h * 0.9f * grow, Fx.Fade(i % 2 == 0 ? Rgb(190, 235, 70, 230) : Rgb(130, 170, 50, 230), env * 0.95f), FxBlend.Alpha, c.Age + i);
        Fx.Rain(fx, c.Age, c.Seed, c.ToFeet + Vector3.UnitY * h * 0.2f, h * 0.7f, 0.15f, 0.9f, 0.4f, 9, FxShape.Bubble, Rgb(210, 250, 110), White, 0.18f, FxBlend.Alpha);
        float item = Math.Clamp((c.Age - 0.2f) / 0.5f, 0f, 1f);
        if (item < 1f) fx.Sprite(FxShape.Dot, c.To + new Vector3(h * 0.35f, h * 0.2f - item * h * 0.15f, 0.3f), h * 0.14f * (1f - item), Fx.Fade(Rgb(255, 220, 120), env), FxBlend.Alpha);
        L2Down(fx, c.Age, c.Seed, c.ToFeet, h, 0.45f, 1, Rgb(210, 240, 90));
    }

    /// <summary>Coaching: the user blows a whistle, three small orange rings out of its mouth, and the ally's Attack and Defense arrows rise with a star clapped between.</summary>
    private static void CoachingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var mouth = c.From + new Vector3(h * 0.35f, h * 0.15f, 0.3f);
        for (int i = 0; i < 3; i++)
            PulseRing(fx, c.Age, mouth, 0.03f + i * 0.09f, 0.3f, h * 0.1f, h * (0.35f + 0.15f * i), Rgb(255, 160, 70), White);
        fx.Sprite(FxShape.Note, mouth + new Vector3(0f, h * 0.2f, 0f), h * 0.2f * Fx.Envelope(c.Age, 0f, 0.06f, 0.2f, 0.32f), Fx.Fade(Rgb(255, 230, 150), 1f), FxBlend.Alpha);
        var ally = c.To;
        L3Up(fx, c.Age, c.ToFeet, c.ToSize, 0.3f, 2, Rgb(255, 150, 70));
        L3Up(fx, c.Age, c.ToFeet + new Vector3(c.ToSize * 0.2f, 0f, 0f), c.ToSize, 0.4f, 2, Rgb(120, 190, 255));
        Fx.Glint(fx, c.Age, ally + new Vector3(0f, c.ToSize * 0.1f, 0.3f), c.ToSize, 0.35f, Rgb(255, 240, 150), 0.6f);
    }

    /// <summary>Flip Turn: the user wraps itself in a hoop of water, flips through it into the foe, and shoots back along a streak of spray to where it came from.</summary>
    private static void FlipTurnY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float spin = Fx.Envelope(c.Age, 0f, 0.06f, I - 0.1f, I);
        if (spin > 0f)
        {
            L2Ellipse(fx, c.From, h * 0.75f, h * 0.75f, h * 0.1f, Fx.Fade(col.Main, spin), 18, c.Age * 18f, FxBlend.Alpha);
            L2Ellipse(fx, c.From, h * 0.75f, h * 0.75f, h * 0.04f, Fx.Fade(White, spin * 0.7f), 18, c.Age * 18f + 0.4f, FxBlend.Add);
        }
        Fx.Stream(fx, c, 0.04f, I, 0.14f, 6, FxShape.Drop, col.Main, col.Light, 0.1f, spread: 0.2f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I, I + 0.4f, 0.16f, 6, FxShape.Drop, col.Light, White, 0.09f, spread: 0.2f, blend: FxBlend.Alpha, reverse: true);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Drop, col.Main, White, 0.09f, 1.0f, 1.8f, FxBlend.Alpha, salt: 515);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Triple Axel: three skating kicks, each a wider crescent of ice than the one before, a snowflake spinning off each and the last one rending a deep frost.</summary>
    private static void TripleAxelY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float t = I - 0.2f + i * 0.1f;
            float env = Fx.Envelope(c.Age, t - 0.05f, t, t + 0.06f, t + 0.26f);
            if (env > 0f)
            {
                float r = h * (0.5f + 0.25f * i);
                L5Arc(fx, c.To, r, r * 0.8f, 3.3f - i * 0.3f, 0.2f - i * 0.3f, h * (0.1f + 0.04f * i), Fx.Fade(col.Main, env), 14);
                L5Arc(fx, c.To, r, r * 0.8f, 3.3f - i * 0.3f, 0.2f - i * 0.3f, h * 0.04f, Fx.Fade(White, env), 14, FxBlend.Add);
                fx.Sprite(FxShape.Snowflake, c.To + new Vector3((i - 1) * h * 0.4f, h * (0.5f + 0.1f * i), 0.3f), h * 0.2f * (1f + i * 0.2f), Fx.Fade(White, env), FxBlend.Alpha, c.Age * 4f + i);
            }
            if (c.Cue.Landed && c.Age >= t) Fx.Contact(fx, c.Age, c.Seed + i, c.To, h, t, 0.2f + 0.1f * i, col.Main, White);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.6f, 0.25f);
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Dual Wingbeat: a great wing sweeps in from the left, then another from the right, each a white-blue arc of wind with feathers blown off it.</summary>
    private static void DualWingbeatY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int s = 0; s < 2; s++)
        {
            float t = I - 0.1f + s * 0.1f;
            float dir = s == 0 ? 1f : -1f;
            float k = (c.Age - (t - 0.15f)) / 0.15f;
            if (k < 0f || c.Age > t + 0.25f) continue;
            float fade = c.Age < t ? 1f : Math.Max(0f, 1f - (c.Age - t) / 0.25f);
            var p = c.To + new Vector3(-dir * h * 1.2f * (1f - Fx.EaseOut(Math.Min(1f, k))), h * 0.1f, 0.3f);
            fx.Sprite(FxShape.Feather, p, h * 1.0f, Fx.Fade(Rgb(236, 244, 255), fade), FxBlend.Alpha, dir * 0.9f);
            L5Arc(fx, c.To, h * 0.9f, h * 0.6f, dir > 0 ? 2.8f : 0.3f, dir > 0 ? 0.3f : 2.8f, h * 0.1f, Fx.Fade(col.Main, fade * 0.8f), 12);
            if (c.Cue.Landed && c.Age >= t) Fx.Burst(fx, c.Seed + s, c.Age, c.To, h, t, 0.5f, 5, FxShape.Feather, White, col.Light, 0.14f, 0.9f, 0.5f, FxBlend.Alpha, salt: 516 + s);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.5f, 0.2f);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Scorching Sands: red-hot sand is flung in a fan from the user, glowing orange as it flies, with heat rippling above it and a few grains left burning on the foe.</summary>
    private static void ScorchingSandsY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.4f, 0.22f, 14, FxShape.Dot, Rgb(255, 190, 90), Rgb(220, 120, 50), 0.17f, arc: 0.06f, spread: 0.5f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.02f, 0.4f, 0.22f, 7, FxShape.Ember, Rgb(255, 130, 40), Rgb(255, 230, 130), 0.16f, spread: 0.45f);
        float heat = Fx.Envelope(c.Age, 0.05f, 0.2f, 0.6f, 0.9f);
        if (heat > 0f)
            for (int i = 0; i < 2; i++)
            {
                var pts = L4Wave(c.From + new Vector3(0f, c.FromSize * (0.35f + 0.25f * i), 0.1f), c.Aim + new Vector3(0f, h * (0.35f + 0.25f * i), 0.1f), 1f, c.FromSize * 0.07f, 3f, c.Age * 10f);
                fx.Ribbon(pts, c.FromSize * 0.04f, Fx.Fade(Rgb(255, 214, 160), heat * 0.35f), FxBlend.Alpha);
            }
        if (c.Cue.Landed)
        {
            Fx.Rain(fx, c.Age, c.Seed, c.ToFeet, h, I, 1.0f, 0.4f, 8, FxShape.Ember, Rgb(255, 160, 60), Rgb(255, 230, 130), 0.07f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Smoke, Rgb(230, 190, 130, 200), Rgb(190, 140, 90, 200), 0.18f, 0.8f, -0.1f, FxBlend.Alpha, salt: 517);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Jungle Healing: vines sprout green from the ground beside the user and its side, flowers open along them and a warm green light mends everyone.</summary>
    private static void JungleHealingY13b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.4f, 0f, 1f));
        for (int v = 0; v < 3; v++)
        {
            var pts = new Vector3[10];
            float x = (v - 1) * h * 0.7f;
            for (int i = 0; i <= 9; i++)
            {
                float u = i / 9f * grow;
                pts[i] = c.FromFeet + new Vector3(x + MathF.Sin(u * 7f + v) * h * 0.18f, h * 1.1f * u, 0.15f);
            }
            fx.Ribbon(pts, h * 0.05f, Fx.Fade(Rgb(70, 170, 70), env), FxBlend.Alpha);
            if (grow > 0.7f)
            {
                L3Flower(fx, pts[9] + new Vector3(0f, h * 0.05f, 0.1f), h * 0.18f * (grow - 0.7f) / 0.3f, v == 1 ? Rgb(255, 214, 90) : Rgb(255, 150, 200), Rgb(255, 250, 200), env, v);
                fx.Sprite(FxShape.Leaf, pts[5] + new Vector3(h * 0.12f, 0f, 0f), h * 0.18f, Fx.Fade(Rgb(120, 220, 100), env), FxBlend.Alpha, 0.8f);
            }
        }
        L2Heal(fx, c.Age, c.Seed, c.FromFeet, h, 0.3f);
    }

    /// <summary>Wicked Blow: a fist of black gathers all the light and drives into the foe, and a starred flash of the critical blow breaks over the hit.</summary>
    private static void WickedBlowY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.5f, 0.2f, 0f), I, 0.85f, Rgb(40, 28, 56), 0f, 0.16f);
        float pre = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.1f, I);
        if (pre > 0f) L5Gather(fx, c.Age, c.Seed, c.To + new Vector3(-h * 0.9f, h * 0.1f, 0.3f), h, 0f, I, 7, FxShape.Glow, Rgb(120, 80, 160), Rgb(40, 28, 56), 0.2f, 0.9f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.8f, Fx.Fade(Rgb(60, 36, 90), 0.95f));
            fx.Sprite(FxShape.Star, c.To, h * 1.3f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.14f, I + 0.3f), Fx.Fade(Rgb(255, 80, 100), 0.9f), FxBlend.Add, 0.4f);
            fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, I - 0.01f, I, I + 0.04f, I + 0.18f));
            Jolt(fx, c.Age, I, 1.0f, 0.35f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Surging Strikes: three punches wrapped in rushing water, each a spiral of blue that bursts on the foe in a ring, the last a great wave.</summary>
    private static void SurgingStrikesY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float at = I - 0.2f + i * 0.1f;
            Strike(c, fx, FxShape.Fist, new Vector3(-1.4f + 0.5f * i, 0.5f - 0.5f * i, 0f), at, 0.5f + 0.1f * i, Rgb(150, 210, 255), 0f, 0.1f);
            float k = (c.Age - (at - 0.1f)) / 0.2f;
            if (k > 0f && k < 1f) fx.Sprite(FxShape.Swirl, c.To + new Vector3(-h * (0.7f - 0.5f * k), h * (0.3f - 0.3f * i), 0.3f), h * 0.5f, Fx.Fade(col.Main, 1f - k), FxBlend.Alpha, c.Age * 14f);
            if (c.Cue.Landed) Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, at, 0.3f, h * (0.8f + 0.3f * i), Fx.Fade(i == 2 ? White : col.Light, 0.9f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Drop, col.Main, White, 0.1f, 1.1f, 2.0f, FxBlend.Alpha, salt: 518);
            Jolt(fx, c.Age, I, 0.8f, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Thunder Cage: six bars of yellow lightning stand round the foe and are joined by a ring top and bottom, forming a crackling cage that hums and spits sparks.</summary>
    private static void ThunderCageY13b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.2f, I - 0.12f, 0.95f, 1.15f);
        if (env > 0f)
        {
            float rise = Fx.EaseOut(Math.Clamp((c.Age - (I - 0.2f)) / 0.2f, 0f, 1f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + 0.3f;
                var foot = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.75f, 0.05f, MathF.Sin(a) * h * 0.4f);
                Fx.Bolt(fx, c.Age, c.Seed + (int)(c.Age * 30f) + i, foot, foot + Vector3.UnitY * h * 1.4f * rise, 0f, 2f, h * 0.045f, col.Main, col.Light, 0);
            }
            foreach (float y in new[] { 0.05f, 1.4f * rise })
                fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * (0.05f + y * h), Vector3.UnitY, h * 1.55f, Fx.Fade(col.Main, env * 0.85f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.To, h * 1.3f, Fx.Fade(col.Main, env * 0.25f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Spark, White, col.Main, 0.1f, 1.0f, salt: 519);
            Jolt(fx, c.Age, I, 0.6f, 0.25f);
        }
        Impact(c, col, fx, 0.95f);
    }
}
