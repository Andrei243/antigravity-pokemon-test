using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y9, its first part (Y9a): the moves the story shows from Lake Valor to Route 222, in MoveSightings' order,
// each a look of its own. The beats are the original's, read from each move's animation script (what comes first, how
// often it hits, when it lands, who shakes); the shapes, colours and timings are our own. Every recipe lands at
// ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads. What is the body's own
// and not the effects' (Bounce's and U-turn's user leaving the field, Waterfall's user rising, Double-Edge's spin, the
// stage dimming or changing for Thunder, Fire Blast, Blizzard, Focus Blast, Hydro Pump, Trick Room and Wish) is not
// drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum6() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Psych Up"] = PsychUpY9,
        ["Thunder"] = ThunderY9,
        ["Aurora Beam"] = AuroraBeamY9,
        ["Barrier"] = BarrierY9,
        ["Double-Edge"] = DoubleEdgeY9,
        ["Thrash"] = ThrashY9,
        ["Trick Room"] = TrickRoomY9,
        ["Brine"] = BrineY9,
        ["Mud Shot"] = MudShotY9,
        ["Waterfall"] = WaterfallY9,
        ["Bind"] = BindY9,
        ["Silver Wind"] = SilverWindY9,
        ["Synthesis"] = SynthesisY9,
        ["Thunder Punch"] = ThunderPunchY9,
        ["Helping Hand"] = HelpingHandY9,
        ["Bounce"] = BounceY9,
        ["Egg Bomb"] = EggBombY9,
        ["Fling"] = FlingY9,
        ["Meteor Mash"] = MeteorMashY9,
        ["Petal Dance"] = PetalDanceY9,
        ["Wish"] = WishY9,
        ["U-turn"] = UTurnY9,
        ["Flash Cannon"] = FlashCannonY9,
        ["Iron Tail"] = IronTailY9,
        ["Metal Burst"] = MetalBurstY9,
        ["Skill Swap"] = SkillSwapY9,
        ["Stone Edge"] = StoneEdgeY9,
        ["Dragon Breath"] = DragonBreathY9,
        ["Close Combat"] = CloseCombatY9,
        ["Dizzy Punch"] = DizzyPunchY9,
        ["High Jump Kick"] = HighJumpKickY9,
        ["Cross Chop"] = CrossChopY9,
        ["Ice Shard"] = IceShardY9,
        ["Swords Dance"] = SwordsDanceY9,
        ["Wood Hammer"] = WoodHammerY9,
        ["Heal Bell"] = HealBellY9,
        ["Psywave"] = PsywaveY9,
        ["Fire Blast"] = FireBlastY9,
        ["Flamethrower"] = FlamethrowerY9,
        ["Punishment"] = PunishmentY9,
        ["Conversion 2"] = Conversion2Y9,
        ["Icy Wind"] = IcyWindY9,
        ["Shadow Punch"] = ShadowPunchY9,
        ["Hail"] = HailY9,
        ["Rock Climb"] = RockClimbY9,
        ["Avalanche"] = AvalancheY9,
        ["Blizzard"] = BlizzardY9,
        ["Focus Blast"] = FocusBlastY9,
        ["Discharge"] = DischargeY9,
        ["Dragon Dance"] = DragonDanceY9,
        ["Hydro Pump"] = HydroPumpY9,
        ["Muddy Water"] = MuddyWaterY9,
        ["Thunder Fang"] = ThunderFangY9,
        ["Zen Headbutt"] = ZenHeadbuttY9
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A ribbon of a wave running from <paramref name="from"/> to <paramref name="to"/> between <paramref name="t0"/> and <paramref name="t1"/>, wobbling by <paramref name="amp"/> (in the way's length).</summary>
    private static void SineRibbon(FxList fx, float age, Vector3 from, Vector3 to, float t0, float t1, float phase, float amp, float waves, float width, Color color, Color core)
    {
        float k = (age - t0) / (t1 - t0);
        if (k < 0f || k > 1f) return;
        var path = to - from;
        float len = path.Length();
        var (across, up) = Fx.Frame(path);
        const int n = 16;
        var pts = new Vector3[n];
        float head = Math.Min(1f, k * 1.6f), tail = Math.Max(0f, k * 1.6f - 0.6f);
        for (int i = 0; i < n; i++)
        {
            float s = tail + (head - tail) * i / (n - 1);
            float wob = MathF.Sin(s * waves * MathF.Tau + phase - age * 14f) * amp * len * MathF.Sin(s * MathF.PI);
            pts[i] = from + path * s + (across * 0.35f + up * 0.9f) * wob;
        }
        float fade = k < 0.8f ? 1f : (1f - k) / 0.2f;
        fx.Ribbon(pts, width, Fx.Fade(color, 0.85f * fade), FxBlend.Alpha);
        fx.Ribbon(pts, width * 0.4f, Fx.Fade(core, 0.9f * fade));
    }

    /// <summary>A shape coming down on the target from <paramref name="above"/> (in its height), striking at <paramref name="at"/>, leaving a crack on the ground.</summary>
    private static void DropOn(in FxCtx c, FxList fx, FxShape shape, float above, float side, float at, float size, Color color, float rotation)
    {
        float h = c.ToSize;
        float k = (c.Age - (at - 0.16f)) / 0.16f;
        float after = Math.Max(0f, (c.Age - at) / 0.22f);
        if (k < 0f || after > 1f) return;
        var p = c.To + new Vector3(side * h * (1f - Fx.EaseOut(Math.Min(1f, k))), above * h * (1f - Math.Min(1f, k) * Math.Min(1f, k)), 0f);
        fx.Sprite(shape, p, h * size * (1f + 0.2f * after), Fx.Fade(color, Math.Min(1f, k * 4f) * (1f - after)), FxBlend.Alpha, rotation);
    }

    /// <summary>Snowflakes and ice dust blown across from the user to the target between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Flurry(in FxCtx c, FxList fx, float t0, float t1, int count, float size, float spread)
    {
        Fx.Stream(fx, c, t0, t1, 0.4f, count, FxShape.Snowflake, Rgb(236, 252, 255), Rgb(160, 220, 255), size, arc: 0.03f, spread: spread, spin: 5f, blend: FxBlend.Alpha);
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Psych Up: pink motes drawn out of the foe and swirled across to the user, which glows and rings as they arrive.</summary>
    private static void PsychUpY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var pink = ColorsOf(PokemonType.Psychic);
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.04f, 0.32f, c.ToSize * 1.1f, Fx.Fade(pink.Main, 0.8f));
        Fx.Stream(fx, c, 0.06f, 0.62f, 0.34f, 12, FxShape.Glow, pink.Main, pink.Light, 0.1f, arc: 0.12f, spread: 0.4f, blend: FxBlend.Alpha, reverse: true);
        Fx.Stream(fx, c, 0.06f, 0.62f, 0.34f, 6, FxShape.Spark, White, pink.Light, 0.08f, arc: 0.12f, spread: 0.3f, reverse: true);
        float glow = Fx.Envelope(c.Age, 0.5f, 0.64f, 0.85f, 1.1f);
        if (glow > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(pink.Light, glow * 0.4f), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.58f, 0.4f, c.FromSize * 0.95f, Fx.Fade(pink.Main, 0.9f));
        }
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.62f, White, 0.4f);
    }

    /// <summary>Thunder: a dark cloud gathers over the foe, a first bolt strikes and a second great one follows with a flash, sparks jumping.</summary>
    private static void ThunderY9(in FxCtx c, FxColors col, FxList fx)
    {
        var el = ColorsOf(PokemonType.Electric);
        float h = c.ToSize;
        var top = c.To + new Vector3(h * 0.35f, h * 3.4f, -h * 0.4f);
        float cloud = Fx.Envelope(c.Age, 0f, 0.14f, 0.6f, 0.9f);
        for (int i = 0; i < 4; i++)
            if (cloud > 0f) fx.Sprite(FxShape.Smoke, c.To + new Vector3((i - 1.5f) * h * 0.55f, h * (1.15f + Fx.Signed(c.Seed, i, 200) * 0.1f), 0f), h * 0.8f, Fx.Fade(Rgb(70, 66, 90, 230), cloud * 0.85f), FxBlend.Alpha);
        var bottom = c.Cue.Missed ? c.Aim - Vector3.UnitY * h * 0.3f : c.ToFeet;
        Fx.Bolt(fx, c.Age, c.Seed, top, bottom, 0.14f, 0.3f, h * 0.05f, el.Main, White, 1);
        Fx.Bolt(fx, c.Age, c.Seed + 9, top + Vector3.UnitX * h * 0.25f, bottom, I - 0.04f, I + 0.32f, h * 0.1f, el.Main, White, 3);
        fx.ScreenFlash(Rgb(255, 250, 210), 0.4f * Fx.Envelope(c.Age, I - 0.05f, I - 0.02f, I + 0.04f, I + 0.2f));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 10, FxShape.Spark, el.Main, White, 0.1f, 1.0f, 0.4f, streak: 0f, salt: 41);
            Tint(c, fx, I, 0.8f, el.Light, 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Aurora Beam: three ribbons of red, green and blue wave across side by side and ring on the target, sparkling.</summary>
    private static void AuroraBeamY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var (across, up) = Fx.Frame(c.Aim - c.From);
        var shades = new[] { Rgb(255, 110, 140), Rgb(120, 255, 160), Rgb(120, 170, 255) };
        for (int i = 0; i < 3; i++)
        {
            var off = (up * (i - 1) * 0.12f + across * (i - 1) * 0.05f) * h;
            SineRibbon(fx, c.Age, c.From + off, c.Aim + off, 0.04f, 0.7f, i * 2.1f, 0.06f, 2.5f, h * 0.09f, shades[i], Fx.Mix(shades[i], White, 0.7f));
        }
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.3f, 10, FxShape.Spark, White, Rgb(220, 240, 255), 0.07f, spread: 0.18f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 0.9f, Fx.Fade(shades[0], 0.8f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.08f, 0.4f, h * 0.9f, Fx.Fade(shades[2], 0.8f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Barrier: five hexagonal panes of glass stand up in an arc before the user, flash white along their edges and hold.</summary>
    private static void BarrierY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var glass = Rgb(214, 150, 255);
        var dir = c.To - c.From;
        dir.Y = 0f;
        var forward = dir.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(dir);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.55f;
            var at = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.95f * (forward.X >= 0f ? 1f : -1f), h * (0.55f + MathF.Sin(a) * 0.75f), 0f);
            Panel(fx, c.Age, at, h * 0.38f, i * 0.05f, glass);
            Fx.Glint(fx, c.Age, at + new Vector3(h * 0.06f, h * 0.06f, 0f), h, 0.3f + i * 0.05f, White, 0.3f);
        }
        float ring = Fx.Envelope(c.Age, 0.05f, 0.2f, 0.8f, 1.1f);
        if (ring > 0f) fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 0.8f, Fx.Fade(glass, ring * 0.7f), depth: true);
    }

    /// <summary>Double-Edge: white speed streaks gather round the user, then it crashes in with a great star of impact and a recoil ring on itself.</summary>
    private static void DoubleEdgeY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.2f, 10, White, Rgb(255, 236, 160), 0.07f, 0.9f);
        SpeedLines(c, fx, 0.1f, 0.36f, 10, 1.0f);
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Star, c.To, c.ToSize * 1.2f * (0.6f + 0.6f * Fx.EaseOut((c.Age - I) / 0.25f)), Fx.Fade(Rgb(255, 240, 190), Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.3f)), FxBlend.Alpha, 0.4f);
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, I, 0.4f, c.ToSize * 1.5f, Fx.Fade(White, 0.9f));
            // The recoil, back on the user
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I + 0.14f, 0.35f, h * 0.9f, Fx.Fade(Rgb(255, 120, 90), 0.8f));
            Fx.Burst(fx, c.Seed, c.Age, c.From, h, I + 0.14f, 0.4f, 4, FxShape.Star, Rgb(255, 120, 90), Rgb(255, 210, 120), 0.08f, 0.7f, 0.5f, FxBlend.Alpha, spin: 6f, salt: 42);
            fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Thrash: the user in a red fury, an anger mark beating, and three wild blows in turn, the last the heaviest.</summary>
    private static void ThrashY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.25f, h * 1.1f, 0f), h, 0f, 0.9f);
        float rage = Fx.Envelope(c.Age, 0f, 0.1f, 0.7f, 1.0f);
        if (rage > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(AngerRed, rage * 0.3f), FxBlend.Alpha);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.05f - i * 0.04f) / 0.2f;
            if (k < 0f || k > 1f) continue;
            float a = Fx.Rand(c.Seed, i, 205) * MathF.Tau;
            fx.Streak(FxShape.Streak, c.From + new Vector3(MathF.Cos(a) * h * 0.7f, MathF.Sin(a) * h * 0.5f, 0f), new Vector3(MathF.Cos(a), MathF.Sin(a), 0f), h * 0.06f, 4f, Fx.Fade(Rgb(255, 150, 120), 1f - k));
        }
        Blows(c, col, fx, 3, 0.1f, 0.34f, 0.3f);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Trick Room: a violet grid of light is laid over the ground between the two sides, which pulses and holds a beat before it fades.</summary>
    private static void TrickRoomY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var violet = Rgb(190, 120, 255);
        float env = Fx.Envelope(c.Age, 0.04f, 0.3f, 0.85f, 1.2f);
        if (env <= 0f) return;
        var a = c.FromFeet + Vector3.UnitY * 0.05f;
        var b = c.ToFeet + Vector3.UnitY * 0.05f;
        var mid = (a + b) * 0.5f;
        var half = new Vector3(Math.Max(Math.Abs(b.X - a.X), c.FromSize * 2f) * 0.75f, 0f, Math.Max(Math.Abs(b.Z - a.Z), c.FromSize * 2f) * 0.75f);
        float grow = Fx.EaseOut(c.Age / 0.3f);
        for (int i = -2; i <= 2; i++)
        {
            float t = i / 2f * grow;
            fx.Ribbon(new[] { mid + new Vector3(-half.X * grow, 0f, half.Z * t), mid + new Vector3(half.X * grow, 0f, half.Z * t) }, c.FromSize * 0.12f, Fx.Fade(violet, env * 0.95f), FxBlend.Alpha, depth: true);
            fx.Ribbon(new[] { mid + new Vector3(half.X * t, 0f, -half.Z * grow), mid + new Vector3(half.X * t, 0f, half.Z * grow) }, c.FromSize * 0.12f, Fx.Fade(violet, env * 0.95f), FxBlend.Alpha, depth: true);
        }
        float pulse = 0.5f + 0.5f * MathF.Sin(c.Age * 14f);
        foreach (var p in new[] { c.FromFeet, c.ToFeet })
            fx.Flat(FxShape.Ring, p + Vector3.UnitY * 0.05f, Vector3.UnitY, c.FromSize * (0.6f + 0.2f * pulse), Fx.Fade(White, env * 0.6f), depth: true);
        fx.Sprite(FxShape.Glow, mid + Vector3.UnitY * c.FromSize * 0.4f, c.FromSize * 3f, Fx.Fade(violet, env * 0.18f), FxBlend.Alpha);
    }

    /// <summary>Brine: a hard spray of salt water with white grains in it flung across, stinging the target red.</summary>
    private static void BrineY9(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0.02f, I, 0.22f, 12, FxShape.Drop, Rgb(120, 190, 235), water.Light, 0.17f, arc: 0.05f, spread: 0.3f, blend: FxBlend.Alpha, trail: 1.2f);
        Fx.Stream(fx, c, 0.04f, I, 0.22f, 9, FxShape.Dot, White, Rgb(226, 238, 246), 0.07f, arc: 0.05f, spread: 0.35f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Dot, White, Rgb(226, 238, 246), 0.05f, 1.0f, 1.8f, FxBlend.Alpha, salt: 43);
            Tint(c, fx, I, 0.9f, Rgb(255, 90, 90), 0.4f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Mud Shot: three globs of mud lobbed one after another that smack on the target and run down it.</summary>
    private static void MudShotY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.02f - i * 0.07f) / (I - 0.1f);
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.05f);
            fx.Sprite(FxShape.Dot, p, h * 0.28f, Fx.Fade(Mud, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.03f, h * 0.04f, 0f), h * 0.12f, Fx.Fade(MudDark, 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Dot, Mud, MudDark, 0.12f, 0.9f, 1.8f, FxBlend.Alpha, salt: 44);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.2f, h, I + 0.05f, 5, MudDark);
            Tint(c, fx, I, 0.9f, Mud, 0.35f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Waterfall: a column of water pours down on the user and rushes across as a curtain, breaking over the target in a splash and a ring.</summary>
    private static void WaterfallY9(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.FromSize;
        float pour = Fx.Envelope(c.Age, 0f, 0.05f, 0.2f, 0.3f);
        for (int i = 0; i < 7; i++)
            if (pour > 0f)
                fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 206) * h * 0.3f, h * (0.5f + 0.4f * ((c.Age * 6f + i * 0.27f) % 1f)), 0f), -Vector3.UnitY, h * 0.08f, 5f, Fx.Fade(water.Light, pour), FxBlend.Alpha);
        float k = (c.Age - 0.15f) / (I - 0.15f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(c.FromFeet, c.ToFeet, Fx.EaseOut(k)) + Vector3.UnitY * h * 0.5f;
            for (int j = 0; j < 6; j++)
                fx.Sprite(FxShape.Drop, p + new Vector3(0f, (j - 2.5f) * h * 0.18f, Fx.Signed(c.Seed, j, 207) * h * 0.1f), h * 0.35f, Fx.Fade(water.Main, 0.8f), FxBlend.Alpha, MathF.PI);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * c.ToSize * 0.3f, c.ToSize, I, 0.7f, 14, FxShape.Drop, water.Main, water.Light, 0.1f, 1.0f, 2.2f, FxBlend.Alpha, salt: 45);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, c.ToSize * 1.5f, Fx.Fade(White, 0.9f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Bind: two tan bands wind round the target and squeeze, twice, with a few stars at each bite.</summary>
    private static void BindY9(in FxCtx c, FxColors col, FxList fx)
    {
        var tan = Rgb(214, 186, 140);
        Wound(c, fx, 2, 0.3f, 0.7f, tan, Rgb(246, 232, 200));
        if (!c.Cue.Landed) { fx.Land(I); return; }
        foreach (float at in new[] { I, I + 0.25f })
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, at, 0.35f, 4, FxShape.Star, Rgb(255, 236, 170), tan, 0.08f, 0.7f, 0f, FxBlend.Alpha, spin: 4f, salt: 46);
        fx.Shake = Math.Max(fx.Shake, 0.2f * (Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.18f) + Fx.Envelope(c.Age, I + 0.25f, I + 0.27f, I + 0.31f, I + 0.43f)));
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Silver Wind: a gust of silver scales swept across from the user to the target, glinting as they turn.</summary>
    private static void SilverWindY9(in FxCtx c, FxColors col, FxList fx)
    {
        var silver = Rgb(222, 230, 244);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0.02f, 0.62f, 0.34f, 16, FxShape.Shard, silver, Rgb(170, 190, 220), 0.17f, arc: 0.05f, spread: 0.55f, spin: 7f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.3f, 7, FxShape.Streak, Rgb(190, 230, 255), White, 0.1f, spread: 0.45f, trail: 2f);
        Fx.Stream(fx, c, 0.1f, 0.6f, 0.3f, 6, FxShape.Spark, White, Rgb(190, 255, 190), 0.07f, spread: 0.5f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Shard, silver, White, 0.07f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 47);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Synthesis: three shafts of sunlight fall on the user, leaves turning in them, and healing crosses rise.</summary>
    private static void SynthesisY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.7f, 1.0f);
        for (int i = -1; i <= 1; i++)
            if (env > 0f)
                fx.Streak(FxShape.Beam, c.FromFeet + new Vector3(i * h * 0.35f - h * 0.2f, h * 1.1f, 0f), new Vector3(0.18f, -1f, 0f), h * 0.22f, 4.4f, Fx.Fade(Rgb(255, 244, 150), env * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.1f - i * 0.1f) / 0.55f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Leaf, c.FromFeet + new Vector3(MathF.Sin(k * 5f + i) * h * 0.35f, h * (1.1f - 0.9f * k), 0f), h * 0.1f, Fx.Fade(Rgb(120, 220, 90), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 6f + i);
        }
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.35f, 1.1f, 4, FxShape.Cross, Rgb(150, 255, 150), 0.14f, 0.3f);
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(255, 250, 190), 0.35f * Fx.Envelope(c.Age, 0.3f, 0.5f, 0.8f, 1.1f)), FxBlend.Alpha);
    }

    /// <summary>Thunder Punch: a fist wrapped in crackling yellow forks driven in, a bolt snapping off it and sparks jumping across the target.</summary>
    private static void ThunderPunchY9(in FxCtx c, FxColors col, FxList fx)
    {
        var el = ColorsOf(PokemonType.Electric);
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.5f, 0f), I, 0.6f, Rgb(255, 230, 90), 0f, 0.26f);
        float k = (c.Age - (I - 0.26f)) / 0.26f;
        if (k > 0f && k < 1.4f)
            Fx.Bolt(fx, c.Age, c.Seed, c.To + new Vector3(-h * 1.3f * (1f - Math.Min(1f, k)) , h * 0.5f, 0f), c.To + new Vector3(h * 0.3f, -h * 0.2f, 0f), I - 0.12f, I + 0.24f, h * 0.045f, el.Main, White, 1);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 12, FxShape.Spark, el.Main, White, 0.1f, 1.0f, 0.2f, salt: 48);
            Tint(c, fx, I, 0.8f, el.Light, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Helping Hand: two glowing palms pat the user on the shoulder in turn and sparkles of encouragement rise.</summary>
    private static void HelpingHandY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        foreach (var (t0, side) in new[] { (0.12f, 1f), (0.4f, -1f) })
        {
            float k = (c.Age - t0) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float pat = k < 0.5f ? Fx.EaseOut(k * 2f) : 1f - (k - 0.5f) * 0.9f;
            var p = c.FromFeet + new Vector3(side * h * (1.0f - 0.45f * pat), h * 0.85f, 0f);
            fx.Sprite(FxShape.Palm, p, h * 0.45f, Fx.Fade(Rgb(255, 240, 190), Math.Min(1f, k * 5f) * (1f - Math.Max(0f, k - 0.7f) / 0.3f)), FxBlend.Alpha, side > 0 ? 0.3f : MathF.PI - 0.3f);
            if (pat > 0.8f) Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + new Vector3(side * h * 0.5f, h * 0.9f, 0f), h, t0 + 0.12f, 0.35f, 5, FxShape.Star, Rgb(255, 236, 140), White, 0.07f, 0.7f, -0.2f, FxBlend.Alpha, spin: 5f, salt: 49 + (side > 0 ? 0 : 3));
        }
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.2f, 1.1f, 4, FxShape.Spark, Rgb(255, 236, 140), 0.12f, 0.35f);
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(255, 232, 150), 0.3f * Fx.Envelope(c.Age, 0.3f, 0.5f, 0.8f, 1.1f)), FxBlend.Alpha);
    }

    /// <summary>Bounce: a ring of dust where the user springs up and a streak after it, then a great foot falls from the sky on the target.</summary>
    private static void BounceY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Dust(c, fx, c.FromFeet, h, 0.04f, 7);
        float up = Fx.Envelope(c.Age, 0.04f, 0.1f, 0.18f, 0.3f);
        if (up > 0f)
        {
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(0f, h * (1f + 2f * (c.Age - 0.04f) * 5f), 0f), Vector3.UnitY, h * 0.12f, 5f, Fx.Fade(White, up));
            fx.Sprite(FxShape.Cross, c.FromFeet + new Vector3(h * 0.1f, h * (1.1f + 2.2f * (c.Age - 0.04f) * 5f), 0f), h * 0.2f, Fx.Fade(Rgb(255, 236, 160), up), FxBlend.Alpha);
        }
        DropOn(c, fx, FxShape.Foot, 1.3f, 0.3f, I, 0.8f, Rgb(250, 246, 226), 0f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, c.ToSize * 1.6f, Fx.Fade(White, 0.9f));
            Dust(c, fx, c.ToFeet, c.ToSize, I, 8);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.25f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Egg Bomb: a cream egg lobbed high that bursts on the target in flame, shell and smoke.</summary>
    private static void EggBombY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var shell = Rgb(250, 240, 210);
        float k = (c.Age - 0.04f) / (I - 0.04f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.1f);
            fx.Sprite(FxShape.Dot, p, h * 0.38f, Fx.Fade(shell, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.04f, h * 0.05f, 0f), h * 0.14f, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Flame, Rgb(255, 150, 50), Rgb(255, 226, 120), 0.18f, 0.8f, -0.6f, salt: 50);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 8, FxShape.Shard, shell, Rgb(200, 186, 150), 0.1f, 1.1f, 2.0f, FxBlend.Alpha, spin: 6f, salt: 51);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 5, FxShape.Smoke, Rgb(200, 190, 176, 210), Rgb(150, 140, 126, 200), 0.22f, 0.6f, -0.3f, FxBlend.Alpha, salt: 52);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.25f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Fling: the user's item, a small grey ball, whipped across fast with a streak behind it and a hard knock on landing.</summary>
    private static void FlingY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - 0.05f) / (I - 0.05f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.07f);
            var q = Lob(c.From, c.Aim, Math.Max(0f, k - 0.1f), 0.07f);
            fx.Streak(FxShape.Streak, q, p - q + Vector3.UnitX * 0.01f, h * 0.07f, 3f, Fx.Fade(White, 0.7f));
            fx.Sprite(FxShape.Dot, p, h * 0.2f, Fx.Fade(Rgb(150, 156, 170), 1f), FxBlend.Alpha, k * 20f);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.025f, h * 0.03f, 0f), h * 0.07f, Fx.Fade(White, 1f), FxBlend.Alpha);
        }
        float spin = Fx.Envelope(c.Age, 0f, 0.05f, 0.2f, 0.3f);
        if (spin > 0f) fx.Sprite(FxShape.Swirl, c.From, c.FromSize * 0.7f, Fx.Fade(White, spin * 0.5f), rotation: c.Age * 18f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 5, FxShape.Star, White, Rgb(255, 236, 160), 0.08f, 0.8f, 0.4f, FxBlend.Alpha, spin: 5f, salt: 53);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Meteor Mash: a comet with an orange tail falls on the target from the upper left and a steel fist follows it in, rings of impact.</summary>
    private static void MeteorMashY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = ColorsOf(PokemonType.Steel);
        float k = (c.Age - 0.0f) / 0.22f;
        if (k >= 0f && k <= 1f)
        {
            var from = c.To + new Vector3(-h * 1.5f, h * 1.3f, 0f);
            var p = Vector3.Lerp(from, c.To, k * k);
            var dir = c.To - from;
            fx.Streak(FxShape.Streak, p - Vector3.Normalize(dir) * h * 0.5f, dir, h * 0.12f, 5f, Fx.Fade(Rgb(255, 160, 60), 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, p, h * 0.4f, Fx.Fade(Rgb(255, 236, 190), 1f), FxBlend.Alpha, k * 10f);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(Rgb(255, 170, 80), 0.6f));
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.22f, 0.3f, h * 0.9f, Fx.Fade(Rgb(255, 190, 110), 0.8f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, 0.22f, 0.5f, 8, FxShape.Ember, Rgb(255, 170, 70), Rgb(255, 230, 150), 0.08f, 0.9f, 0.6f, salt: 54);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(1.4f, 0.2f, 0f), I, 0.6f, steel.Light, 0.4f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.1f, Fx.Fade(steel.Light, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.3f * (Fx.Envelope(c.Age, 0.22f, 0.24f, 0.28f, 0.4f) + Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.24f)));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Petal Dance: pink petals whirl round the user and are flung at the target in three waves, the first landing with the damage.</summary>
    private static void PetalDanceY9(in FxCtx c, FxColors col, FxList fx)
    {
        var grass = ColorsOf(PokemonType.Grass);
        var pink = Rgb(255, 160, 190);
        float h = c.FromSize;
        float whirl = Fx.Envelope(c.Age, 0f, 0.05f, 0.18f, 0.3f);
        for (int i = 0; i < 8; i++)
            if (whirl > 0f)
            {
                float a = c.Age * 14f + i * MathF.Tau / 8f;
                fx.Sprite(FxShape.Petal, c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.7f, h * (0.3f + 0.08f * i), MathF.Sin(a) * h * 0.3f), h * 0.14f, Fx.Fade(i % 2 == 0 ? pink : Rgb(255, 210, 220), whirl), FxBlend.Alpha, a);
            }
        for (int w = 0; w < 3; w++)
            Fx.Stream(fx, c, 0.15f + w * 0.22f, I + 0.22f * w, 0.2f, 5, FxShape.Petal, pink, grass.Light, 0.12f, arc: 0.06f, spread: 0.45f, spin: 8f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
            for (int w = 0; w < 3; w++)
                Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I + 0.22f * w, 0.4f, 6, FxShape.Petal, pink, Rgb(255, 224, 232), 0.1f, 0.8f, 0.6f, FxBlend.Alpha, spin: 6f, salt: 55 + w);
        Blows(c, col, fx, 3, 0.22f, 0.26f, 0.2f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Wish: a star slides down the sky onto the user in a long tail, and a warm ring and gold motes rise from where it settles.</summary>
    private static void WishY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var gold = Rgb(255, 232, 140);
        float k = c.Age / I;
        if (k >= 0f && k <= 1f)
        {
            var from = c.FromFeet + new Vector3(-h * 1.1f, h * 1.9f, 0f);
            var p = Vector3.Lerp(from, c.From + Vector3.UnitY * h * 0.2f, Fx.EaseOut(k));
            fx.Streak(FxShape.Streak, p - Vector3.Normalize(c.From - from) * h * 0.6f, c.From - from, h * 0.09f, 6f, Fx.Fade(Rgb(255, 244, 190), 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, p, h * 0.36f, Fx.Fade(gold, 1f), FxBlend.Alpha, k * 6f);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(gold, 0.5f));
        }
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I, 0.5f, h * 1.1f, Fx.Fade(gold, 0.8f));
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, I, 1.15f, 5, FxShape.Star, gold, 0.1f, 0.3f);
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(255, 244, 200), 0.35f * Fx.Envelope(c.Age, I, I + 0.15f, 0.9f, 1.2f)), FxBlend.Alpha);
    }

    /// <summary>U-turn: a streak darts from the user to the target, strikes with a flash, and swings back in a wide arc to where it began.</summary>
    private static void UTurnY9(in FxCtx c, FxColors col, FxList fx)
    {
        var bug = ColorsOf(PokemonType.Bug);
        float h = c.FromSize;
        Dust(c, fx, c.FromFeet, h, 0.02f, 4);
        float k = (c.Age - 0.04f) / (I - 0.04f);
        if (k >= 0f && k <= 1.9f)
        {
            // Out to the target, then back round the outside of the way
            const int n = 10;
            var pts = new Vector3[n];
            var (across, up) = Fx.Frame(c.To - c.From);
            float head = Math.Min(k, 1.9f);
            for (int i = 0; i < n; i++)
            {
                float s = Math.Max(0f, head - (n - 1 - i) * 0.05f);
                pts[i] = s <= 1f ? Vector3.Lerp(c.From, c.To, Fx.EaseOut(s))
                    : Vector3.Lerp(c.To, c.From, Fx.EaseOut(s - 1f)) + (across * 0.5f + up * 0.8f) * c.FromSize * 1.4f * MathF.Sin((s - 1f) * MathF.PI);
            }
            float fade = k < 1.6f ? 1f : (1.9f - k) / 0.3f;
            fx.Ribbon(pts, h * 0.1f, Fx.Fade(bug.Main, 0.8f * fade), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.04f, Fx.Fade(White, fade));
            fx.Sprite(FxShape.Star, pts[n - 1], h * 0.22f, Fx.Fade(bug.Light, fade), FxBlend.Alpha, k * 10f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.4f, 6, FxShape.Star, bug.Light, White, 0.08f, 0.8f, 0.3f, FxBlend.Alpha, spin: 5f, salt: 58);
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I + 0.4f, 0.3f, h * 0.9f, Fx.Fade(bug.Light, 0.8f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Flash Cannon: silver light gathers into a sphere in front of the user, rays turning, then a great white-silver beam fires with sparks.</summary>
    private static void FlashCannonY9(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        var dir = c.To - c.From;
        dir.Y = 0f;
        var ball = c.From + (dir.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(dir)) * h * 0.7f;
        float grow = Fx.Envelope(c.Age, 0f, 0.2f, 0.26f, 0.34f);
        if (grow > 0f)
        {
            fx.Sprite(FxShape.Glow, ball, h * (0.3f + 0.5f * grow), Fx.Fade(steel.Light, 0.6f * grow));
            fx.Sprite(FxShape.Orb, ball, h * 0.4f * grow, Fx.Fade(Rgb(226, 236, 250), grow), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
                fx.Streak(FxShape.Streak, ball, new Vector3(MathF.Cos(i * 1.05f + c.Age * 8f), MathF.Sin(i * 1.05f + c.Age * 8f), 0f), h * 0.07f, 3.5f * grow, Fx.Fade(White, grow * 0.8f));
        }
        Gather(fx, c.Age, c.Seed, ball, h, 0f, 0.24f, 8, White, steel.Main, 0.06f, 0.8f);
        Fx.Beam(fx, c, 0.2f, 0.66f, 0.2f, steel.Main, White, 8);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Spark, White, steel.Light, 0.1f, 1.1f, 0.2f, salt: 59);
            fx.ScreenFlash(White, 0.25f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Iron Tail: the user's flank gleams, glints running along it, and a heavy grey bar of steel swings down onto the target with a ringing flash.</summary>
    private static void IronTailY9(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        float shine = Fx.Envelope(c.Age, 0f, 0.06f, 0.2f, 0.3f);
        if (shine > 0f) fx.Sprite(FxShape.Glow, c.From + new Vector3(-h * 0.3f, -h * 0.1f, 0f), h * 0.7f, Fx.Fade(Rgb(210, 220, 240), shine * 0.5f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++) Fx.Glint(fx, c.Age, c.From + new Vector3(-h * (0.15f + 0.15f * i), -h * 0.15f + 0.1f * h * i, 0f), h, 0.04f + i * 0.07f, White, 0.3f);
        float k = (c.Age - 0.2f) / (I - 0.2f);
        if (k >= 0f && k <= 1.4f)
        {
            // The bar swings down through a quarter turn about the target
            float a = MathF.PI * 0.2f + Math.Min(k, 1f) * MathF.PI * 0.55f;
            var along = new Vector3(MathF.Cos(a), -MathF.Sin(a), 0f);
            var p = c.To + new Vector3(0f, c.ToSize * 0.5f, 0f) - along * c.ToSize * 0.1f;
            float fade = k < 1f ? 1f : (1.4f - k) / 0.4f;
            fx.Streak(FxShape.Streak, p, along, c.ToSize * 0.34f, 5.2f, Fx.Fade(Rgb(176, 186, 206), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, along, c.ToSize * 0.13f, 5.2f, Fx.Fade(White, fade));
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, c.ToSize * 1.0f, Fx.Fade(steel.Light, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.4f, 8, FxShape.Spark, White, steel.Main, 0.09f, 1.0f, 0.3f, salt: 60);
            fx.Shake = Math.Max(fx.Shake, 0.32f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.24f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Metal Burst: silver rings draw in and brighten on the user, then are loosed in a great ring of shards that crosses to the target.</summary>
    private static void MetalBurstY9(in FxCtx c, FxColors col, FxList fx)
    {
        var steel = ColorsOf(PokemonType.Steel);
        float h = c.FromSize;
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.07f) / 0.2f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, c.From, h * (1.3f - 0.8f * k), Fx.Fade(steel.Light, k), FxBlend.Alpha);
        }
        Fx.Glint(fx, c.Age, c.From, h, 0.24f, White, 0.7f);
        float f = (c.Age - 0.26f) / (I - 0.26f);
        if (f >= 0f && f <= 1f)
        {
            var p = Vector3.Lerp(c.From, c.Aim, f);
            fx.Sprite(FxShape.Ring, p, h * (0.5f + 0.9f * f), Fx.Fade(steel.Light, 1f - f * 0.3f), FxBlend.Alpha);
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + 0.3f;
                fx.Sprite(FxShape.Shard, p + new Vector3(0f, MathF.Sin(a), MathF.Cos(a)) * h * (0.3f + 0.7f * f) * 0.7f, h * 0.12f, Fx.Fade(White, 0.9f), FxBlend.Alpha, a);
            }
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Shard, steel.Light, White, 0.09f, 1.2f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 61);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Skill Swap: a gold orb leaves the user and a blue one the foe, crossing on arcs and passing; each end flashes with the other's.</summary>
    private static void SkillSwapY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var gold = Rgb(255, 214, 90);
        var blue = Rgb(110, 190, 255);
        float k = (c.Age - 0.05f) / (0.75f - 0.05f);
        if (k >= 0f && k <= 1f)
        {
            float e = Fx.EaseOut(k);
            var a = Lob(c.From, c.To, e, 0.05f);
            var b = Lob(c.To, c.From, e, -0.05f);
            float fade = Math.Min(1f, (1f - k) * 5f) * Math.Min(1f, k * 8f + 0.3f);
            foreach (var (p, color) in new[] { (a, gold), (b, blue) })
            {
                fx.Sprite(FxShape.Glow, p, c.FromSize * 0.8f, Fx.Fade(color, 0.6f * fade));
                fx.Sprite(FxShape.Orb, p, c.FromSize * 0.38f, Fx.Fade(color, fade), FxBlend.Alpha);
                fx.Sprite(FxShape.Spark, p, c.FromSize * 0.3f, Fx.Fade(White, fade * 0.8f), rotation: c.Age * 6f);
            }
        }
        foreach (var (p, color, size) in new[] { (c.To, gold, c.ToSize), (c.From, blue, c.FromSize) })
        {
            Fx.Shockwave(fx, c.Age, p, Vector3.Zero, 0.75f, 0.35f, size * 0.95f, Fx.Fade(color, 0.9f));
            Fx.Glint(fx, c.Age, p, size, 0.78f, White, 0.4f);
        }
    }

    /// <summary>Stone Edge: sharp stones thrust up from the ground round the target one after another and a ring of them hangs, then all fall in a burst.</summary>
    private static void StoneEdgeY9(in FxCtx c, FxColors col, FxList fx)
    {
        var rock = ColorsOf(PokemonType.Rock);
        float h = c.ToSize;
        int count = 7;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count + 0.4f;
            float k = (c.Age - 0.02f - i * 0.04f) / 0.12f;
            if (k < 0f) continue;
            float rise = Fx.EaseOut(Math.Min(1f, k));
            float fade = Fx.Envelope(c.Age, 0f, 0.1f, I + 0.1f, I + 0.4f);
            var foot = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.8f, 0f, MathF.Sin(a) * h * 0.35f);
            fx.Streak(FxShape.Shard, foot + Vector3.UnitY * h * 0.55f * rise, Vector3.UnitY, h * 0.36f, 2.8f * rise, Fx.Fade(i % 2 == 0 ? Rgb(236, 220, 180) : rock.Light, fade), FxBlend.Alpha);
            if (fade > 0f && rise >= 1f) fx.Sprite(FxShape.Spark, foot + Vector3.UnitY * h * 1.1f, h * 0.16f, Fx.Fade(White, fade * 0.7f * (0.5f + 0.5f * MathF.Sin(c.Age * 20f + i))));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Rock, rock.Light, rock.Dark, 0.11f, 1.1f, 2.2f, FxBlend.Alpha, spin: 6f, salt: 62);
            fx.Shake = Math.Max(fx.Shake, 0.38f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Dragon Breath: a wide cone of teal dragon-fire with a white heart breathed out in two puffs, setting the target red.</summary>
    private static void DragonBreathY9(in FxCtx c, FxColors col, FxList fx)
    {
        var teal = Rgb(60, 220, 180);
        var light = Rgb(200, 255, 232);
        for (int w = 0; w < 2; w++)
        {
            Fx.Stream(fx, c, 0.02f + 0.12f * w, 0.4f + 0.12f * w, 0.24f, 8, FxShape.Flame, teal, Rgb(80, 120, 250), 0.34f, spread: 0.45f);
            Fx.Stream(fx, c, 0.04f + 0.12f * w, 0.4f + 0.12f * w, 0.24f, 4, FxShape.Flame, light, White, 0.18f, spread: 0.25f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Flame, teal, light, 0.15f, 0.8f, -0.6f, salt: 63);
            Tint(c, fx, I, 0.95f, Rgb(255, 80, 80), 0.4f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Close Combat: a flurry of fists and feet from every side, each with a star, the target jolted again and again.</summary>
    private static void CloseCombatY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var orange = ColorsOf(PokemonType.Fighting);
        var dirs = new[] { new Vector3(-1.2f, 0.4f, 0f), new Vector3(1.2f, -0.2f, 0f), new Vector3(-0.8f, -0.7f, 0f), new Vector3(0.9f, 0.7f, 0f), new Vector3(-1.1f, 0.1f, 0f), new Vector3(1.0f, 0.3f, 0f) };
        for (int i = 0; i < 6; i++)
        {
            float at = I - (5 - i) * 0.05f;
            Strike(c, fx, i % 2 == 0 ? FxShape.Fist : FxShape.Foot, dirs[i], at, 0.62f, orange.Light, i * 0.7f, 0.09f);
            if (c.Cue.Landed) Fx.Contact(fx, c.Age, c.Seed + i, c.To + dirs[i] * h * -0.15f, h, at, 0.26f, orange.Main, White);
        }
        Dust(c, fx, c.ToFeet, h, I - 0.2f, 5);
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, 0.1f, 0.12f, I, I + 0.2f));
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Dizzy Punch: a fist driven in twice, the second leaving the target ringed by stars and a turning swirl.</summary>
    private static void DizzyPunchY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.2f, 0f), I - 0.15f, 0.5f, Rgb(250, 236, 220), 0f, 0.1f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, -0.1f, 0f), I, 0.55f, Rgb(250, 236, 220), 0f, 0.1f);
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed, c.To + Vector3.UnitX * h * 0.1f, h, I - 0.15f, 0.2f, Rgb(255, 236, 190), White);
            float swirl = Fx.Envelope(c.Age, I + 0.05f, I + 0.15f, I + 0.55f, I + 0.75f);
            if (swirl > 0f) fx.Sprite(FxShape.Swirl, c.To + Vector3.UnitY * h * 0.5f, h * 0.7f, Fx.Fade(Rgb(255, 200, 240), swirl * 0.8f), FxBlend.Alpha, c.Age * 9f);
            fx.Shake = Math.Max(fx.Shake, 0.25f * (Fx.Envelope(c.Age, I - 0.15f, I - 0.13f, I - 0.1f, I) + Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.2f)));
        }
        Dizzy(c, fx, I + 0.05f, 4);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>High Jump Kick: dust bursts at the user's feet, a streak climbs into the sky, and a great foot comes down from far above on the target.</summary>
    private static void HighJumpKickY9(in FxCtx c, FxColors col, FxList fx)
    {
        var orange = ColorsOf(PokemonType.Fighting);
        float h = c.FromSize;
        Dust(c, fx, c.FromFeet, h, 0.02f, 7);
        float up = Fx.Envelope(c.Age, 0.02f, 0.06f, 0.16f, 0.26f);
        if (up > 0f) fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(0f, h * (1.2f + 4f * (c.Age - 0.02f)), 0f), Vector3.UnitY, h * 0.1f, 6f, Fx.Fade(White, up));
        DropOn(c, fx, FxShape.Foot, 1.3f, 0.7f, I, 0.8f, orange.Light, 0.4f);
        float k = (c.Age - (I - 0.2f)) / 0.2f;
        if (k > 0f && k < 1f)
            fx.Streak(FxShape.Streak, c.To + new Vector3(0.7f * c.ToSize * (1f - k), 1.3f * c.ToSize * (1f - k) * (1f - k) + 0.3f * c.ToSize, 0f), new Vector3(-0.1f, -1f, 0f), c.ToSize * 0.14f, 5f, Fx.Fade(orange.Light, 0.8f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, c.ToSize * 1.5f, Fx.Fade(orange.Light, 0.9f));
            Dust(c, fx, c.ToFeet, c.ToSize, I, 6);
            fx.Shake = Math.Max(fx.Shake, 0.42f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Cross Chop: two hands brought down from either side in turn, then both together in a bright cross, a star where the bars cross.</summary>
    private static void CrossChopY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var orange = ColorsOf(PokemonType.Fighting);
        foreach (var (at, side, ang) in new[] { (0.2f, -1f, 0.7f), (0.27f, 1f, -0.7f) })
        {
            float k = (c.Age - (at - 0.1f)) / 0.2f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Palm, c.To + new Vector3(side * h * (0.9f - 0.6f * Fx.EaseOut(k)), h * (0.9f - 0.5f * Fx.EaseOut(k)), 0f), h * 0.45f, Fx.Fade(orange.Light, 1f - k * 0.5f), FxBlend.Alpha, side * 0.7f);
        }
        for (int i = 0; i < 2; i++)
        {
            float k = (c.Age - (I - 0.04f + i * 0.02f)) / 0.45f;
            if (k < 0f || k > 1f) continue;
            float ang = i == 0 ? 0.8f : -0.8f;
            var along = new Vector3(MathF.Cos(ang), MathF.Sin(ang), 0f);
            float fade = k < 0.5f ? 1f : (1f - k) / 0.5f;
            fx.Streak(FxShape.Streak, c.To, along, h * 0.26f, 8f * Fx.EaseOut(k * 3f), Fx.Fade(orange.Main, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, c.To, along, h * 0.1f, 8f * Fx.EaseOut(k * 3f), Fx.Fade(White, fade));
        }
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Star, c.To, h * 0.5f, Fx.Fade(White, Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.3f)), FxBlend.Alpha, 0.5f);
            fx.Shake = Math.Max(fx.Shake, 0.28f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Ice Shard: snow draws together in front of the user into a long ice shard that glints and is flung straight across to the target.</summary>
    private static void IceShardY9(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.FromSize;
        var dir = c.Aim - c.From;
        var ahead = c.From + Vector3.Normalize(dir) * h * 0.6f;
        Gather(fx, c.Age, c.Seed, ahead, h, 0f, 0.22f, 9, ice.Light, White, 0.07f, 0.8f);
        float form = Fx.Envelope(c.Age, 0.08f, 0.2f, 0.24f, 0.26f);
        if (form > 0f) fx.Sprite(FxShape.Shard, ahead, h * 0.9f * form, Fx.Fade(ice.Light, form), FxBlend.Alpha, ScreenAngle(dir) - 0.8f);
        Fx.Glint(fx, c.Age, ahead, h, 0.2f, White, 0.4f);
        float k = (c.Age - 0.24f) / (I - 0.24f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(ahead, c.Aim, k);
            fx.Streak(FxShape.Streak, p, dir, h * 0.16f, 5f, Fx.Fade(ice.Light, 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Shard, p, h * 0.9f, Fx.Fade(Rgb(214, 244, 255), 1f), FxBlend.Alpha, ScreenAngle(dir) - 0.8f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 9, FxShape.Shard, ice.Main, ice.Light, 0.1f, 1.0f, 1.2f, FxBlend.Alpha, spin: 5f, salt: 64);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 6, FxShape.Snowflake, White, ice.Light, 0.09f, 0.8f, 0.5f, FxBlend.Alpha, spin: 3f, salt: 65);
            Tint(c, fx, I, 0.8f, ice.Light, 0.35f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Swords Dance: four blades of light circle the user turning, cross above it in a flash and draw down as red-gold light.</summary>
    private static void SwordsDanceY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.75f, 1.0f);
        float draw = Fx.EaseOut(Math.Clamp((c.Age - 0.6f) / 0.3f, 0f, 1f));
        for (int i = 0; i < 4; i++)
        {
            float a = c.Age * 9f + i * MathF.Tau / 4f;
            float rad = h * 0.85f * (1f - 0.7f * draw);
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * rad, h * (0.55f + 0.35f * draw), MathF.Sin(a) * rad * 0.4f);
            if (env > 0f) fx.Sprite(FxShape.Sword, p, h * 0.38f, Fx.Fade(i % 2 == 0 ? Rgb(255, 140, 90) : Rgb(255, 224, 150), env), FxBlend.Alpha, a + MathF.PI / 2f);
        }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.1f, 0.95f, Rgb(255, 120, 90), Rgb(255, 214, 140), 8);
        Fx.Shockwave(fx, c.Age, c.From + Vector3.UnitY * h * 0.3f, Vector3.Zero, 0.7f, 0.3f, h * 0.9f, Fx.Fade(White, 0.9f));
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.35f, h, 0.7f, White, 0.6f);
    }

    /// <summary>Wood Hammer: a great log is raised and brought straight down on the target, leaves flying, a crack in the ground and a jolt back on the user.</summary>
    private static void WoodHammerY9(in FxCtx c, FxColors col, FxList fx)
    {
        var grass = ColorsOf(PokemonType.Grass);
        var bark = Rgb(142, 98, 58);
        float h = c.ToSize;
        float k = (c.Age - 0.05f) / (I - 0.05f);
        if (k >= 0f && k <= 1.4f)
        {
            // Raised a little first, then brought straight down
            float y = k < 0.35f ? h * (1.4f + 0.3f * k / 0.35f) : h * (1.7f - 1.35f * Math.Min(1f, (k - 0.35f) / 0.65f) * Math.Min(1f, (k - 0.35f) / 0.65f));
            float fade = k < 1f ? 1f : (1.4f - k) / 0.4f;
            var p = c.To + new Vector3(h * 0.1f, y, 0f);
            fx.Streak(FxShape.Streak, p + Vector3.UnitY * h * 0.5f, Vector3.UnitY, h * 0.34f, 2.6f, Fx.Fade(bark, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p + new Vector3(-h * 0.06f, h * 0.5f, 0f), Vector3.UnitY, h * 0.1f, 2.6f, Fx.Fade(Rgb(190, 150, 100), fade), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 12, FxShape.Leaf, grass.Main, grass.Light, 0.11f, 1.0f, 0.8f, FxBlend.Alpha, spin: 7f, salt: 66);
            var crack = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.4f, I + 0.7f);
            fx.Sprite(FxShape.Crack, c.ToFeet + Vector3.UnitY * h * 0.1f, h * 0.7f, Fx.Fade(Rgb(70, 50, 30), crack), FxBlend.Alpha);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.4f, Fx.Fade(grass.Light, 0.9f));
            Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I + 0.08f, 0.3f, c.FromSize * 0.7f, Fx.Fade(Rgb(255, 150, 120), 0.7f));
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Heal Bell: a golden bell-tone rings out as notes and rings spread from the user over its side, washing it in pale cyan.</summary>
    private static void HealBellY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var gold = Rgb(255, 226, 130);
        var cyan = Rgb(170, 244, 255);
        foreach (float t0 in new[] { 0.05f, 0.3f, 0.55f })
            Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.5f, Vector3.Zero, t0, 0.45f, h * 1.6f, Fx.Fade(gold, 0.8f));
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.08f - i * 0.1f) / 0.55f;
            if (k < 0f || k > 1f) continue;
            float a = (i - 2.5f) * 0.7f;
            fx.Sprite(FxShape.Note, c.FromFeet + new Vector3(MathF.Sin(a) * h * (0.4f + 0.9f * k), h * (0.8f + 0.7f * k), 0f), h * 0.2f, Fx.Fade(i % 2 == 0 ? gold : cyan, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, a * 0.3f);
        }
        float wash = Fx.Envelope(c.Age, 0.1f, 0.3f, 0.8f, 1.1f);
        if (wash > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(cyan, wash * 0.4f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 1.1f, h, 0.1f, White, 0.5f);
    }

    /// <summary>Psywave: three wobbling bands of violet and pink light travel across in turn, pulsing as they go, and break in rings on the target.</summary>
    private static void PsywaveY9(in FxCtx c, FxColors col, FxList fx)
    {
        var psy = ColorsOf(PokemonType.Psychic);
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
            SineRibbon(fx, c.Age, c.From, c.Aim, 0.02f + i * 0.08f, 0.5f + i * 0.08f, i * 2.1f, 0.12f, 2f, h * 0.12f, i % 2 == 0 ? psy.Main : Rgb(190, 130, 255), psy.Light);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.1f, Fx.Fade(psy.Main, 0.8f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.1f, 0.4f, h * 0.8f, Fx.Fade(psy.Light, 0.8f));
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Fire Blast: flame gathers into a ball at the user, flies across and bursts on the target into a five-pointed star of fire, with a flash.</summary>
    private static void FireBlastY9(in FxCtx c, FxColors col, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        float h = c.ToSize;
        var dir = c.Aim - c.From;
        var ahead = c.From + Vector3.Normalize(dir) * c.FromSize * 0.6f;
        float ball = Fx.Envelope(c.Age, 0f, 0.14f, 0.2f, 0.24f);
        if (ball > 0f)
        {
            fx.Sprite(FxShape.Flame, ahead, c.FromSize * 0.7f * ball, Fx.Fade(fire.Main, ball), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, ahead, c.FromSize * 0.9f * ball, Fx.Fade(fire.Light, 0.5f));
        }
        Gather(fx, c.Age, c.Seed, ahead, c.FromSize, 0f, 0.2f, 8, fire.Main, fire.Light, 0.07f, 0.8f);
        float k = (c.Age - 0.2f) / (I - 0.2f);
        if (k >= 0f && k <= 1f)
        {
            var p = Vector3.Lerp(ahead, c.Aim, k);
            fx.Sprite(FxShape.Flame, p, h * 0.6f, Fx.Fade(fire.Main, 1f), FxBlend.Alpha, ScreenAngle(dir) + 1.57f);
            fx.Sprite(FxShape.Glow, p, h * 0.8f, Fx.Fade(fire.Light, 0.6f));
            fx.Streak(FxShape.Flame, p - Vector3.Normalize(dir) * h * 0.3f, dir, h * 0.25f, 2.5f, Fx.Fade(fire.Dark, 0.7f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            float star = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.35f, I + 0.6f);
            for (int i = 0; i < 5; i++)
            {
                float a = MathF.PI / 2f + i * MathF.Tau / 5f;
                var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                fx.Streak(FxShape.Streak, c.To + d * h * 0.55f * Fx.EaseOut((c.Age - I) / 0.15f), d, h * 0.12f, 4f, Fx.Fade(fire.Main, star), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, c.To + d * h * 0.55f * Fx.EaseOut((c.Age - I) / 0.15f), d, h * 0.05f, 4f, Fx.Fade(White, star));
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Ember, fire.Light, fire.Main, 0.08f, 1.0f, 0.6f, salt: 67);
            fx.ScreenFlash(Rgb(255, 214, 150), 0.35f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.22f));
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Flamethrower: a steady thick stream of flame pours from the user across to the target and goes on burning on it, embers rising.</summary>
    private static void FlamethrowerY9(in FxCtx c, FxColors col, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0.02f, 0.85f, 0.26f, 22, FxShape.Flame, fire.Main, fire.Dark, 0.3f, spread: 0.22f);
        Fx.Stream(fx, c, 0.04f, 0.85f, 0.26f, 10, FxShape.Flame, fire.Light, White, 0.16f, spread: 0.12f);
        if (c.Cue.Landed)
        {
            float burn = Fx.Envelope(c.Age, I, I + 0.08f, 0.8f, 1.1f);
            if (burn > 0f) fx.Sprite(FxShape.Flame, c.ToFeet + Vector3.UnitY * h * 0.45f, h * 1.1f, Fx.Fade(fire.Main, burn * 0.7f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Ember, fire.Light, fire.Main, 0.07f, 0.8f, -0.8f, salt: 68);
            Tint(c, fx, I, 1.0f, Rgb(255, 80, 60), 0.35f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Punishment: a dark whip lashes down twice and red exclamation marks pop up over the target, one more each time.</summary>
    private static void PunishmentY9(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var dark = Rgb(90, 50, 120);
        Whip(fx, c.Age, c.To + new Vector3(-h * 1.5f, h * 0.9f, 0f), c.To + new Vector3(h * 0.2f, -h * 0.1f, 0f), 0.04f, 0.2f, 0.15f, 0.04f, h * 0.07f, dark, Rgb(210, 170, 240), FxShape.Spark, h * 0.2f);
        Whip(fx, c.Age, c.To + new Vector3(h * 1.5f, h * 0.9f, 0f), c.To + new Vector3(-h * 0.2f, h * 0.05f, 0f), I - 0.14f, 0.2f, 0.15f, 0.04f, h * 0.07f, dark, Rgb(210, 170, 240), FxShape.Spark, h * 0.2f);
        if (c.Cue.Landed)
            for (int i = 0; i < 3; i++)
            {
                float at = 0.22f + i * 0.14f;
                float env = Fx.Envelope(c.Age, at, at + 0.05f, 0.9f, 1.1f);
                if (env > 0f)
                {
                    var p = c.ToFeet + new Vector3((i - 1) * h * 0.3f, h * 1.15f, 0f);
                    fx.Sprite(FxShape.Exclaim, p, h * 0.22f, Fx.Fade(White, env), FxBlend.Alpha);
                    fx.Sprite(FxShape.Exclaim, p, h * 0.18f, Fx.Fade(AngerRed, env), FxBlend.Alpha);
                }
            }
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.22f * (Fx.Envelope(c.Age, 0.2f, 0.22f, 0.25f, 0.34f) + Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.18f)));
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Conversion 2: a scanning grid sweeps over the foe, then rings on the user change colour one after another as it takes a new type.</summary>
    private static void Conversion2Y9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var scan = Rgb(150, 230, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.4f, 0.55f);
        if (env > 0f)
        {
            float sweep = Fx.EaseOut(c.Age / 0.4f);
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Hex, c.ToFeet + new Vector3((i % 2 - 0.5f) * h * 0.5f, h * (0.25f + 0.28f * i), 0f), h * 0.45f, Fx.Fade(scan, env * 0.5f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, c.ToFeet + new Vector3(0f, h * sweep, 0f), Vector3.UnitX, h * 0.07f, 7f, Fx.Fade(White, env * 0.9f));
        }
        var colours = new[] { Rgb(255, 120, 110), Rgb(255, 214, 90), Rgb(110, 220, 130), Rgb(110, 170, 255), Rgb(220, 130, 255) };
        float f = c.FromSize;
        for (int i = 0; i < 5; i++)
        {
            float t0 = 0.45f + i * 0.1f;
            float e = Fx.Envelope(c.Age, t0, t0 + 0.06f, t0 + 0.1f, t0 + 0.25f);
            if (e > 0f) fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * f * (0.15f + 0.18f * i), Vector3.UnitY, f * (0.8f - 0.05f * i), Fx.Fade(colours[i], e * 0.9f), depth: true);
        }
        float glow = Fx.Envelope(c.Age, 0.5f, 0.7f, 0.9f, 1.15f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, f * 0.9f, Fx.Fade(Fx.Mix(colours[1], colours[3], 0.5f), glow * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Icy Wind: a cold wind of snowflakes sweeps across and settles frost on the foe, which is tinted pale blue.</summary>
    private static void IcyWindY9(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.ToSize;
        Flurry(c, fx, 0f, 0.7f, 18, 0.1f, 0.5f);
        Fx.Stream(fx, c, 0.02f, 0.7f, 0.36f, 6, FxShape.Streak, ice.Main, White, 0.06f, spread: 0.45f, trail: 3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 6, FxShape.Snowflake, White, ice.Light, 0.1f, 0.7f, 0.3f, FxBlend.Alpha, spin: 3f, salt: 69);
            Tint(c, fx, I, 1.0f, ice.Main, 0.42f);
            fx.Sprite(FxShape.Shard, c.ToFeet + new Vector3(-h * 0.3f, h * 0.2f, 0f), h * 0.2f, Fx.Fade(ice.Light, Fx.Envelope(c.Age, I + 0.05f, I + 0.15f, 0.9f, 1.1f)), FxBlend.Alpha, 0.3f);
            fx.Sprite(FxShape.Shard, c.ToFeet + new Vector3(h * 0.3f, h * 0.15f, 0f), h * 0.16f, Fx.Fade(ice.Light, Fx.Envelope(c.Age, I + 0.08f, I + 0.18f, 0.9f, 1.1f)), FxBlend.Alpha, -0.4f);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Shadow Punch: a dark violet fist comes from the user with three ghosts of itself behind it, and passes through the target with a cold ring.</summary>
    private static void ShadowPunchY9(in FxCtx c, FxColors col, FxList fx)
    {
        var ghost = ColorsOf(PokemonType.Ghost);
        float h = c.ToSize;
        float k = (c.Age - 0.06f) / (I - 0.06f);
        if (k >= 0f && k <= 1.3f)
            for (int i = 0; i < 4; i++)
            {
                float s = Math.Max(0f, Math.Min(1.1f, k - i * 0.1f));
                var p = Vector3.Lerp(c.From, c.To, Fx.EaseOut(Math.Min(1f, s)));
                float fade = (1f - i * 0.24f) * (k < 1.1f ? 1f : (1.3f - k) / 0.2f);
                fx.Sprite(FxShape.Fist, p, h * 0.5f, Fx.Fade(i == 0 ? Rgb(60, 36, 100) : ghost.Main, fade * (i == 0 ? 1f : 0.6f)), FxBlend.Alpha, ScreenAngle(c.To - c.From));
            }
        float dark = Fx.Envelope(c.Age, 0f, 0.1f, 0.4f, 0.55f);
        if (dark > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.9f, Fx.Fade(Rgb(40, 24, 70), dark * 0.5f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.1f, Fx.Fade(ghost.Light, 0.8f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 6, FxShape.Wisp, ghost.Main, ghost.Light, 0.14f, 0.7f, -0.5f, salt: 70);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Hail: white stones of ice pelt slanting down across both sides one after another, bouncing where they land.</summary>
    private static void HailY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.FromSize;
        for (int i = 0; i < 24; i++)
        {
            float k = (c.Age - 0.03f - i * 0.036f) / 0.26f;
            if (k < 0f || k > 1.5f) continue;
            float f = Fx.Rand(c.Seed, i, 210);
            var ground = Vector3.Lerp(c.FromFeet, c.ToFeet, f) + new Vector3(Fx.Signed(c.Seed, i, 211) * h * 0.7f, h * 0.08f, Fx.Signed(c.Seed, i, 212) * h * 0.3f);
            if (k <= 1f)
            {
                var p = ground + new Vector3(-h * 0.6f * (1f - k), h * 1.4f * (1f - k), 0f);
                fx.Streak(FxShape.Streak, p + new Vector3(-h * 0.15f, h * 0.3f, 0f), new Vector3(0.43f, -1f, 0f), h * 0.05f, 2.2f, Fx.Fade(Rgb(190, 226, 255), 0.8f), FxBlend.Alpha);
                fx.Sprite(FxShape.Shard, p, h * 0.2f, Fx.Fade(Rgb(236, 248, 255), 1f), FxBlend.Alpha, k * 7f + i);
            }
            else
            {
                float b = (k - 1f) / 0.5f;
                fx.Sprite(FxShape.Shard, ground + new Vector3(h * 0.2f * b, h * 0.3f * MathF.Sin(b * MathF.PI), 0f), h * 0.16f, Fx.Fade(ice.Light, 1f - b), FxBlend.Alpha, b * 5f);
            }
        }
    }
    /// <summary>Rock Climb: stone steps heave up from the ground between the two, the user charges up them, and rocks burst on the target.</summary>
    private static void RockClimbY9(in FxCtx c, FxColors col, FxList fx)
    {
        var rock = ColorsOf(PokemonType.Rock);
        float h = c.ToSize;
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.02f - i * 0.045f) / 0.12f;
            if (k < 0f) continue;
            float fade = Fx.Envelope(c.Age, 0f, 0.08f, I + 0.05f, I + 0.35f);
            float f = (i + 1) / 6f;
            var foot = Vector3.Lerp(c.FromFeet, c.ToFeet, f);
            float rise = Fx.EaseOut(Math.Min(1f, k)) * h * (0.15f + 0.1f * i);
            fx.Sprite(FxShape.Rock, foot + Vector3.UnitY * rise, h * 0.32f, Fx.Fade(i % 2 == 0 ? rock.Main : Rgb(160, 130, 90), fade), FxBlend.Alpha, i * 0.9f);
            if (k < 1f) Dust(c, fx, foot, h * 0.6f, 0.02f + i * 0.045f, 2);
        }
        SpeedLines(c, fx, 0.16f, 0.36f, 4, 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Rock, rock.Light, rock.Dark, 0.11f, 1.0f, 2.3f, FxBlend.Alpha, spin: 6f, salt: 71);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.26f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Avalanche: slabs of snow and ice slide down from the upper right in a white cloud and pile onto the target, smoke of snow rolling off.</summary>
    private static void AvalancheY9(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.ToSize;
        for (int i = 0; i < 12; i++)
        {
            float k = (c.Age - i * 0.02f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            var from = c.To + new Vector3(h * (1.2f + Fx.Signed(c.Seed, i, 213) * 0.4f), h * (1.2f + 0.5f * Fx.Rand(c.Seed, i, 214)), 0f);
            var p = Vector3.Lerp(from, c.To + new Vector3(Fx.Signed(c.Seed, i, 215) * h * 0.35f, -h * 0.1f, 0f), k * k);
            fx.Sprite(i % 3 == 0 ? FxShape.Shard : FxShape.Rock, p, h * 0.34f, Fx.Fade(i % 2 == 0 ? Rgb(236, 248, 255) : ice.Light, Math.Min(1f, k * 6f)), FxBlend.Alpha, k * 5f + i);
            fx.Sprite(FxShape.Smoke, p + new Vector3(h * 0.2f, h * 0.2f, 0f), h * 0.4f, Fx.Fade(Rgb(240, 246, 255, 200), 0.5f * Math.Min(1f, k * 5f)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 1.0f, 8, FxShape.Smoke, Rgb(240, 246, 255, 220), Rgb(200, 220, 240, 200), 0.3f, 0.7f, -0.2f, FxBlend.Alpha, salt: 72);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Shard, ice.Main, White, 0.1f, 1.0f, 1.8f, FxBlend.Alpha, spin: 5f, salt: 73);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Blizzard: a white storm of snowflakes whirls across the whole field, a heavy cloud settling on the foe, and crystals grow up round it.</summary>
    private static void BlizzardY9(in FxCtx c, FxColors col, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float h = c.ToSize;
        Flurry(c, fx, 0f, 0.7f, 26, 0.13f, 0.9f);
        float cloud = Fx.Envelope(c.Age, 0.05f, 0.25f, 0.85f, 1.15f);
        if (cloud > 0f)
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3((i - 1) * h * 0.6f, h * (0.8f + 0.2f * (i % 2)), 0f), h * 1.2f, Fx.Fade(Rgb(226, 238, 252, 220), cloud * 0.5f), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
            {
                float rise = Fx.EaseOut((c.Age - I - i * 0.03f) / 0.14f);
                float fade = Fx.Envelope(c.Age, I, I + 0.15f, 0.95f, 1.2f);
                float a = i * MathF.Tau / 5f;
                if (rise > 0f && fade > 0f)
                    fx.Streak(FxShape.Shard, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.55f, h * 0.35f * rise, MathF.Sin(a) * h * 0.25f), Vector3.UnitY, h * 0.18f, 2.2f * rise, Fx.Fade(ice.Light, fade * 0.85f), FxBlend.Alpha);
            }
            fx.ScreenFlash(Rgb(226, 244, 255), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.25f));
            Tint(c, fx, I, 1.1f, ice.Main, 0.4f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Focus Blast: orange fighting-spirit gathers in rings about the user, a great orb forms, is thrown and bursts on the target in a ring of fire-coloured light.</summary>
    private static void FocusBlastY9(in FxCtx c, FxColors col, FxList fx)
    {
        var fight = ColorsOf(PokemonType.Fighting);
        float h = c.FromSize;
        var dir = c.Aim - c.From;
        var ahead = c.From + Vector3.Normalize(dir) * h * 0.6f;
        for (int i = 0; i < 3; i++)
        {
            float k = ((c.Age - i * 0.06f) / 0.22f);
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, ahead, h * (1.2f - 0.9f * k), Fx.Fade(fight.Light, k), FxBlend.Alpha);
        }
        float orb = Fx.Envelope(c.Age, 0.08f, 0.2f, 0.22f, 0.24f);
        if (orb > 0f)
        {
            fx.Sprite(FxShape.Glow, ahead, h * 0.9f * orb, Fx.Fade(fight.Light, 0.5f));
            fx.Sprite(FxShape.Orb, ahead, h * 0.5f * orb, Fx.Fade(fight.Main, 1f), FxBlend.Alpha);
        }
        Fx.Orb(fx, c, 0.22f, I, 0.46f, fight.Main, White, FxShape.Glow, Rgb(255, 150, 70));
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, c.ToSize * 1.5f, Fx.Fade(fight.Light, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.07f, 0.45f, c.ToSize * 1.1f, Fx.Fade(fight.Main, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 8, FxShape.Flame, fight.Main, fight.Light, 0.16f, 0.9f, -0.5f, salt: 74);
            fx.ScreenFlash(Rgb(255, 214, 160), 0.3f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.05f, I + 0.22f));
            fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.3f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Discharge: bolts leap from the user in every direction, one reaching the foe, and yellow rings spread from it.</summary>
    private static void DischargeY9(in FxCtx c, FxColors col, FxList fx)
    {
        var el = ColorsOf(PokemonType.Electric);
        float h = c.FromSize;
        float charge = Fx.Envelope(c.Age, 0f, 0.1f, 0.2f, 0.3f);
        if (charge > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(el.Main, charge * 0.5f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + 0.4f;
            var end = c.From + new Vector3(MathF.Cos(a) * h * 1.3f, MathF.Sin(a) * h * 0.9f, 0f);
            Fx.Bolt(fx, c.Age, c.Seed + i * 7, c.From, end, 0.22f, I + 0.3f, h * 0.035f, el.Main, White, 1);
        }
        Fx.Bolt(fx, c.Age, c.Seed + 77, c.From, c.Aim, I - 0.1f, I + 0.3f, h * 0.06f, el.Main, White, 2);
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, 0.22f, 0.4f, h * 1.6f, Fx.Fade(el.Main, 0.8f));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Spark, el.Main, White, 0.1f, 1.0f, 0.2f, salt: 75);
            Tint(c, fx, I, 0.8f, el.Light, 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.26f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Dragon Dance: a blue serpent of light coils up round the user and its beat is rung out in three red-gold rings.</summary>
    private static void DragonDanceY9(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var dragon = ColorsOf(PokemonType.Dragon);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.15f);
        if (env > 0f)
        {
            const int n = 18;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float s = i / (n - 1f);
                float a = s * MathF.Tau * 2.2f + c.Age * 6f;
                pts[i] = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.6f * (1f - 0.2f * s), h * (0.05f + 1.1f * s), MathF.Sin(a) * h * 0.3f);
            }
            fx.Ribbon(pts, h * 0.22f, Fx.Fade(dragon.Main, env * 0.85f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.09f, Fx.Fade(dragon.Light, env));
            fx.Sprite(FxShape.Glow, pts[n - 1], h * 0.3f, Fx.Fade(Rgb(255, 140, 90), env * 0.7f));
        }
        foreach (float t0 in new[] { 0.15f, 0.45f, 0.75f })
            Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.5f, Vector3.Zero, t0, 0.35f, h * 1.2f, Fx.Fade(Rgb(255, 150, 80), 0.8f));
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(Rgb(255, 80, 60), 0.28f * Fx.Envelope(c.Age, 0.1f, 0.3f, 0.8f, 1.1f)), FxBlend.Alpha);
    }

    /// <summary>Hydro Pump: a wide braided jet of water hammers across with a heavy spray, then a great splash and ring.</summary>
    private static void HydroPumpY9(in FxCtx c, FxColors col, FxList fx)
    {
        var water = ColorsOf(PokemonType.Water);
        float h = c.ToSize;
        Fx.Beam(fx, c, 0.05f, 0.72f, 0.36f, water.Main, water.Light, 6);
        for (int i = 0; i < 2; i++)
            SineRibbon(fx, c.Age, c.From, c.Aim, 0.06f, 0.7f, i * MathF.PI, 0.05f, 3f, h * 0.05f, water.Light, White);
        Fx.Stream(fx, c, 0.06f, 0.7f, 0.2f, 12, FxShape.Drop, water.Light, White, 0.16f, spread: 0.35f, blend: FxBlend.Alpha, trail: 1.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 14, FxShape.Drop, water.Main, water.Light, 0.12f, 1.2f, 2.4f, FxBlend.Alpha, salt: 76);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.6f, Fx.Fade(water.Light, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(White, 0.8f));
            fx.Shake = Math.Max(fx.Shake, 0.42f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Muddy Water: a heaving wall of brown water rolls over the ground and breaks over the target, mud running down it.</summary>
    private static void MuddyWaterY9(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.WaveWall(fx, c, 0.04f, I + 0.1f, Rgb(140, 100, 62), Rgb(214, 186, 140));
        float h = c.ToSize;
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 10, FxShape.Dot, Mud, MudDark, 0.13f, 1.0f, 2.0f, FxBlend.Alpha, salt: 77);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.3f, h, I + 0.05f, 6, MudDark);
            Tint(c, fx, I, 1.0f, Mud, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Thunder Fang: yellow fangs snap shut on the target with a bolt cracking between them and sparks jumping off.</summary>
    private static void ThunderFangY9(in FxCtx c, FxColors col, FxList fx)
    {
        var el = ColorsOf(PokemonType.Electric);
        float h = c.ToSize;
        FangPair(c, fx, I - 0.2f, 0.4f, Rgb(255, 236, 110), el.Light);
        if (c.Cue.Landed)
        {
            Fx.Bolt(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.5f, c.To - Vector3.UnitY * h * 0.5f, I - 0.03f, I + 0.25f, h * 0.05f, el.Main, White, 1);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 10, FxShape.Spark, el.Main, White, 0.1f, 1.0f, 0.2f, salt: 78);
            Tint(c, fx, I, 0.8f, el.Light, 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Zen Headbutt: rings of thought close on the user's brow, a pink-blue orb of will forms there, and it rams the target, rings breaking out.</summary>
    private static void ZenHeadbuttY9(in FxCtx c, FxColors col, FxList fx)
    {
        var psy = ColorsOf(PokemonType.Psychic);
        var blue = Rgb(130, 170, 255);
        float h = c.FromSize;
        var brow = c.FromFeet + new Vector3(h * 0.15f, h * 0.95f, 0f);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.07f) / 0.22f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, brow, h * (0.9f - 0.65f * k), Fx.Fade(i % 2 == 0 ? psy.Main : blue, k), FxBlend.Alpha);
        }
        float k2 = (c.Age - 0.2f) / (I - 0.2f);
        if (k2 >= 0f && k2 <= 1f)
        {
            var p = Vector3.Lerp(brow, c.To, Fx.EaseOut(k2));
            fx.Sprite(FxShape.Glow, p, h * 0.8f, Fx.Fade(psy.Light, 0.6f));
            fx.Sprite(FxShape.Orb, p, h * 0.45f, Fx.Fade(Fx.Mix(psy.Main, blue, 0.5f), 1f), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, c.To - brow, h * 0.12f, 3f, Fx.Fade(blue, 0.7f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, c.ToSize * 1.2f, Fx.Fade(psy.Main, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.08f, 0.4f, c.ToSize * 0.9f, Fx.Fade(blue, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.24f));
        }
        Impact(c, col, fx, 0.95f);
    }
}
