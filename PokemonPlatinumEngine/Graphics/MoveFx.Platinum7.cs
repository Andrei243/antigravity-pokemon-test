using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y9, its second part (Y9b): the moves the story shows from the Sunyshore Gym to Route 224, in MoveSightings'
// order, each a look of its own. The beats are the original's, read from each move's animation script (what comes
// first, how often it hits, when it lands, who shakes); the shapes, colours and timings are our own. Every recipe
// lands at ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads. What is the
// body's own and not the effects' (Dive's and Brave Bird's user leaving the field, Giga Impact's and Flare Blitz's
// lunge, Wring Out's squash of the target, Heat Wave's push of the foes, the stage dimming or changing for Hyper Beam,
// Solar Beam, Power Gem, Destiny Bond, Grudge, Perish Song and the rest) is not drawn here.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum7() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Super Fang"] = SuperFangY9b,
        ["Mirror Shot"] = MirrorShotY9b,
        ["Charge Beam"] = ChargeBeamY9b,
        ["Giga Impact"] = GigaImpactY9b,
        ["Octazooka"] = OctazookaY9b,
        ["Wring Out"] = WringOutY9b,
        ["Dark Pulse"] = DarkPulseY9b,
        ["Dragon Pulse"] = DragonPulseY9b,
        ["Dragon Rush"] = DragonRushY9b,
        ["Hammer Arm"] = HammerArmY9b,
        ["Hyper Beam"] = HyperBeamY9b,
        ["Natural Gift"] = NaturalGiftY9b,
        ["Power Whip"] = PowerWhipY9b,
        ["Brave Bird"] = BraveBirdY9b,
        ["Head Smash"] = HeadSmashY9b,
        ["Leaf Storm"] = LeafStormY9b,
        ["Attack Order"] = AttackOrderY9b,
        ["Bug Buzz"] = BugBuzzY9b,
        ["Cross Poison"] = CrossPoisonY9b,
        ["Defend Order"] = DefendOrderY9b,
        ["Heal Order"] = HealOrderY9b,
        ["Iron Head"] = IronHeadY9b,
        ["Megahorn"] = MegahornY9b,
        ["Power Gem"] = PowerGemY9b,
        ["Earth Power"] = EarthPowerY9b,
        ["Rock Wrecker"] = RockWreckerY9b,
        ["Flare Blitz"] = FlareBlitzY9b,
        ["Overheat"] = OverheatY9b,
        ["Solar Beam"] = SolarBeamY9b,
        ["Energy Ball"] = EnergyBallY9b,
        ["Leaf Blade"] = LeafBladeY9b,
        ["Aura Sphere"] = AuraSphereY9b,
        ["Extreme Speed"] = ExtremeSpeedY9b,
        ["Mirror Coat"] = MirrorCoatY9b,
        ["Surf"] = SurfY9b,
        ["Heat Wave"] = HeatWaveY9b,
        ["Acid"] = AcidY9b,
        ["Constrict"] = ConstrictY9b,
        ["Razor Wind"] = RazorWindY9b,
        ["Tailwind"] = TailwindY9b,
        ["Bullet Punch"] = BulletPunchY9b,
        ["Dive"] = DiveY9b,
        ["Dynamic Punch"] = DynamicPunchY9b,
        ["Guard Swap"] = GuardSwapY9b,
        ["Gunk Shot"] = GunkShotY9b,
        ["Outrage"] = OutrageY9b,
        ["Perish Song"] = PerishSongY9b,
        ["Power Swap"] = PowerSwapY9b,
        ["Sheer Cold"] = SheerColdY9b,
        ["Spore"] = SporeY9b,
        ["Acid Armor"] = AcidArmorY9b,
        ["Aromatherapy"] = AromatherapyY9b,
        ["Destiny Bond"] = DestinyBondY9b,
        ["Gastro Acid"] = GastroAcidY9b,
        ["Grudge"] = GrudgeY9b,
        ["Mega Kick"] = MegaKickY9b,
        ["Moonlight"] = MoonlightY9b,
        ["Trick"] = TrickY9b
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A ring grown from <paramref name="r0"/> to <paramref name="r1"/> and fading: a body in <paramref name="body"/> (so it reads on a bright field) with a rim of light on it.</summary>
    private static void PulseRing(FxList fx, float age, Vector3 at, float t0, float dur, float r0, float r1, Color body, Color rim)
    {
        float k = (age - t0) / dur;
        if (k < 0f || k > 1f) return;
        float r = r0 + (r1 - r0) * Fx.EaseOut(k);
        float a = 1f - k;
        fx.Sprite(FxShape.Ring, at, r, Fx.Fade(body, a * 0.9f), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, at, r * 1.04f, Fx.Fade(rim, a * 0.55f));
    }

    /// <summary>Little things (bees, spores, petals) circling a point between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Swarm(FxList fx, float age, int seed, Vector3 centre, float h, float t0, float t1, int count, FxShape shape, Color body, Color glint, float size, float radius)
    {
        float span = t1 - t0, t = age - t0;
        if (t < 0f || t > span) return;
        float env = Fx.Envelope(t, 0f, 0.06f, span - 0.1f, span);
        for (int i = 0; i < count; i++)
        {
            float a = Fx.Rand(seed, i, 300) * MathF.Tau + t * (6f + 3f * Fx.Rand(seed, i, 301));
            float r = h * radius * (0.7f + 0.5f * Fx.Rand(seed, i, 302));
            var p = centre + new Vector3(MathF.Cos(a) * r, MathF.Sin(a * 1.3f) * r * 0.5f + h * 0.08f, MathF.Sin(a) * r * 0.4f);
            fx.Sprite(shape, p, h * size, Fx.Fade(body, env), FxBlend.Alpha, a);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * size * 0.2f, h * size * 0.25f, 0f), h * size * 0.4f, Fx.Fade(glint, env), FxBlend.Alpha);
        }
    }

    /// <summary>A shaft of light or earth standing up from the ground at <paramref name="feet"/>, <paramref name="height"/> tall, for a moment.</summary>
    private static void Shaft(FxList fx, float age, Vector3 feet, float t0, float dur, float height, float width, Color color, Color core)
    {
        float env = Fx.Envelope(age, t0, t0 + dur * 0.25f, t0 + dur * 0.6f, t0 + dur);
        if (env <= 0f) return;
        float rise = Fx.EaseOut(Math.Min(1f, (age - t0) / (dur * 0.35f)));
        float len = height * rise;
        var mid = feet + Vector3.UnitY * len * 0.5f;
        fx.Streak(FxShape.Beam, mid, Vector3.UnitY, width, Math.Max(0.1f, len * 0.5f / width), Fx.Fade(color, env * 0.9f), FxBlend.Alpha);
        fx.Streak(FxShape.Beam, mid, Vector3.UnitY, width * 0.4f, Math.Max(0.1f, len * 0.5f / (width * 0.4f)), Fx.Fade(core, env));
    }

    /// <summary>A shape lobbed from the user to the target (or past it) with a halo, between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    private static void Ball(in FxCtx c, FxList fx, FxShape shape, float t0, float t1, float size, float arc, Color body, Color glow, float spin = 0f)
    {
        float k = (c.Age - t0) / (t1 - t0);
        if (k < 0f || k > 1f) return;
        var p = Lob(c.From, c.Aim, k, arc);
        float s = c.ToSize * size;
        fx.Sprite(FxShape.Glow, p, s * 1.8f, Fx.Fade(glow, 0.5f));
        fx.Sprite(shape, p, s, Fx.Fade(body, 1f), FxBlend.Alpha, spin * k);
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Super Fang: two great white fangs close on the target and bite, stars flying off the bite.</summary>
    private static void SuperFangY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        FangPair(c, fx, I - 0.2f, 0.62f, Rgb(255, 252, 238), Rgb(255, 196, 150));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Star, Rgb(255, 236, 170), White, 0.1f, 0.9f, 0.4f, FxBlend.Alpha, spin: 5f, salt: 121);
            Tint(c, fx, I, 0.8f, Rgb(255, 120, 100), 0.3f);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Mirror Shot: silver glints flash on the user's body, a ball of mirror-light is sent across and bursts in a silver flash.</summary>
    private static void MirrorShotY9b(in FxCtx c, FxColors col, FxList fx)
    {
        var silver = Rgb(214, 224, 244);
        float h = c.FromSize;
        for (int i = 0; i < 4; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 310) * h * 0.4f, Fx.Signed(c.Seed, i, 311) * h * 0.4f, 0f), h, 0.02f + i * 0.045f, White, 0.3f);
        Ball(c, fx, FxShape.Orb, 0.1f, I, 0.34f, 0.04f, silver, Rgb(190, 214, 255), 6f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Shard, silver, White, 0.09f, 1.0f, 0.5f, FxBlend.Alpha, spin: 7f, salt: 122);
            fx.ScreenFlash(White, 0.22f * Fx.Envelope(c.Age, I - 0.01f, I + 0.01f, I + 0.04f, I + 0.18f));
            Tint(c, fx, I, 0.85f, silver, 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Charge Beam: yellow sparks gather round the user, crackling, and a thick yellow beam is held across to the target, which glows with it.</summary>
    private static void ChargeBeamY9b(in FxCtx c, FxColors col, FxList fx)
    {
        var el = ColorsOf(PokemonType.Electric);
        float h = c.FromSize;
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.2f, 9, el.Main, White, 0.08f, 0.9f);
        if (c.Age < 0.26f) Fx.Bolt(fx, c.Age, c.Seed, c.From + new Vector3(-h * 0.5f, h * 0.5f, 0f), c.From + new Vector3(h * 0.4f, -h * 0.3f, 0f), 0.04f, 0.26f, h * 0.035f, el.Main, White, 1);
        Fx.Beam(fx, c, 0.18f, 0.78f, 0.14f, el.Main, White, 6);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 8, FxShape.Spark, el.Main, White, 0.09f, 1.0f, 0.3f, salt: 123);
            Tint(c, fx, I, 0.9f, el.Light, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, 0.6f, 0.78f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Giga Impact: the user draws back in a cloud of white, then charges in with a long shower of speed and crashes with a great star and rings.</summary>
    private static void GigaImpactY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Dust(c, fx, c.FromFeet, h, 0.04f, 6);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.18f, 8, White, Rgb(255, 210, 230), 0.09f, 0.8f);
        SpeedLines(c, fx, 0.14f, 0.36f, 12, 1.1f);
        if (c.Cue.Landed)
        {
            float k = Fx.EaseOut((c.Age - I) / 0.25f);
            fx.Sprite(FxShape.Star, c.To, c.ToSize * 1.5f * (0.6f + 0.6f * k), Fx.Fade(Rgb(255, 240, 210), Fx.Envelope(c.Age, I - 0.01f, I, I + 0.08f, I + 0.32f)), FxBlend.Alpha, 0.3f);
            PulseRing(fx, c.Age, c.To, I, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.5f, Rgb(255, 180, 130), White);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.04f, 0.45f, c.ToSize * 1.7f, Fx.Fade(White, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.55f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.34f));
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Octazooka: balls of black ink are spat across one after another and burst on the target in a splash of ink and smoke.</summary>
    private static void OctazookaY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var ink = Rgb(42, 40, 58);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.03f - i * 0.06f) / (I - 0.1f);
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.04f) + Vector3.UnitY * Fx.Signed(c.Seed, i, 312) * h * 0.1f;
            fx.Sprite(FxShape.Dot, p, h * 0.32f, Fx.Fade(ink, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(h * 0.04f, h * 0.05f, 0f), h * 0.1f, Fx.Fade(Rgb(150, 150, 190), 1f), FxBlend.Alpha);
        }
        Dust(c, fx, c.FromFeet, c.FromSize, 0.02f, 3);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Dot, ink, Rgb(80, 76, 110), 0.14f, 1.0f, 1.2f, FxBlend.Alpha, salt: 124);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 5, FxShape.Smoke, Rgb(60, 58, 80, 220), Rgb(110, 106, 130, 200), 0.24f, 0.6f, -0.3f, FxBlend.Alpha, salt: 125);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.2f, h, I + 0.05f, 5, ink);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Wring Out: three twisting bands wring the target, each tighter, drops squeezed out of it and flying off.</summary>
    private static void WringOutY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Wound(c, fx, 4, 0.2f, 0.8f, Rgb(214, 150, 214), Rgb(255, 236, 255));
        if (!c.Cue.Landed) { fx.Land(I); return; }
        for (int i = 0; i < 3; i++)
        {
            float at = I + i * 0.2f;
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, at, 0.35f, 4, FxShape.Drop, Rgb(190, 226, 255), White, 0.08f, 0.8f, 1.2f, FxBlend.Alpha, salt: 126 + i);
            fx.Shake = Math.Max(fx.Shake, 0.18f * Fx.Envelope(c.Age, at, at + 0.02f, at + 0.06f, at + 0.16f));
        }
        float twist = Fx.Envelope(c.Age, I, I + 0.1f, I + 0.6f, I + 0.8f);
        if (twist > 0f) fx.Sprite(FxShape.Swirl, c.To, h * 0.9f, Fx.Fade(Rgb(255, 220, 255), twist * 0.5f), rotation: c.Age * 9f);
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Dark Pulse: rings of black and violet are sent out from the user, one after another, and break over the target, which is stained dark.</summary>
    private static void DarkPulseY9b(in FxCtx c, FxColors col, FxList fx)
    {
        var dark = ColorsOf(PokemonType.Dark);
        var violet = Rgb(120, 60, 170);
        float h = c.FromSize;
        float glow = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.3f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(violet, glow * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.05f - i * 0.08f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            float s = c.ScaleAt(k) * (0.45f + 0.5f * k);
            fx.Sprite(FxShape.Ring, p, s, Fx.Fade(dark.Dark, Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, p, s * 1.05f, Fx.Fade(violet, Math.Min(1f, (1f - k) * 3f) * 0.8f), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.4f, dark.Dark, Rgb(200, 150, 255));
            Tint(c, fx, I, 1.0f, Rgb(60, 20, 90), 0.5f);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Dragon Pulse: a braid of teal and violet light rolls across with three rings of force after it, and breaks over the target in a great ring.</summary>
    private static void DragonPulseY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var teal = Rgb(70, 220, 220);
        var violet = Rgb(130, 100, 255);
        SineRibbon(fx, c.Age, c.From, c.Aim, 0.04f, 0.62f, 0f, 0.05f, 1.5f, h * 0.2f, teal, Rgb(220, 255, 255));
        SineRibbon(fx, c.Age, c.From, c.Aim, 0.04f, 0.62f, MathF.PI, 0.05f, 1.5f, h * 0.2f, violet, Rgb(230, 220, 255));
        Fx.Waves(fx, c, 0.08f, 0.5f, 3, FxShape.Ring, Rgb(160, 240, 255), 0.4f, 0.26f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.5f, h * 0.3f, h * 1.6f, teal, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.55f, 8, FxShape.Spark, teal, violet, 0.1f, 1.0f, 0.2f, salt: 129);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.16f, I + 0.34f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Dragon Rush: a coiling dragon of blue and violet light lashes across with a snapping jaw, a ring of force where it meets the target.</summary>
    private static void DragonRushY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var blue = Rgb(96, 110, 250);
        Whip(fx, c.Age, c.From, c.Aim, 0.0f, I + 0.02f, 0.03f, 0.05f, h * 0.22f, blue, Rgb(214, 200, 255), FxShape.Fang, h * 0.5f, 0f);
        Whip(fx, c.Age, c.From + Vector3.UnitY * h * 0.15f, c.Aim, 0.05f, I, 0.03f, -0.05f, h * 0.14f, Rgb(150, 130, 255), White, FxShape.Fang, h * 0.3f, 0f);
        SpeedLines(c, fx, 0.04f, 0.3f, 6, 0.8f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.3f, h * 1.5f, blue, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Spark, blue, Rgb(214, 200, 255), 0.1f, 1.1f, 0.3f, salt: 130);
            fx.Shake = Math.Max(fx.Shake, 0.45f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.3f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Hammer Arm: a great fist is raised and brought down on the target, the ground cracking and stones thrown up.</summary>
    private static void HammerArmY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float raise = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.14f, 0.2f);
        if (raise > 0f) fx.Sprite(FxShape.Fist, c.To + new Vector3(-h * 0.3f, h * 0.95f, 0f), h * 0.8f, Fx.Fade(Rgb(255, 200, 150), raise * 0.9f), FxBlend.Alpha, 0.3f);
        DropOn(c, fx, FxShape.Fist, 0.9f, -0.3f, I, 0.95f, Rgb(255, 196, 150), 0.3f);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.6f, Fx.Fade(White, 0.9f));
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.3f, Fx.Fade(Rgb(60, 44, 34), Fx.Envelope(c.Age, I, I + 0.03f, 0.8f, 1.05f)), FxBlend.Alpha, c.Seed * 0.1f, depth: true);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.6f, 6, FxShape.Rock, Rgb(176, 148, 104), Rgb(120, 98, 68), 0.12f, 0.9f, 2.2f, FxBlend.Alpha, spin: 5f, salt: 131);
            Dust(c, fx, c.ToFeet, h, I, 6);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Hyper Beam: a hot ball of orange light gathers at the user's mouth, then a huge beam is held on the target, shaking it, ending in a great burst.</summary>
    private static void HyperBeamY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var orange = Rgb(255, 140, 50);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.14f, 8, orange, White, 0.08f, 0.8f);
        float ball = Fx.Envelope(c.Age, 0f, 0.14f, 0.16f, 0.24f);
        if (ball > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.8f * ball, Fx.Fade(orange, 0.85f));
        Fx.Beam(fx, c, 0.12f, 0.86f, 0.3f, orange, White, 6);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 10, FxShape.Spark, orange, White, 0.11f, 1.1f, 0.2f, salt: 132);
            fx.Sprite(FxShape.Star, c.To, c.ToSize * 1.2f * Fx.EaseOut((c.Age - I) / 0.3f), Fx.Fade(Rgb(255, 230, 190), Fx.Envelope(c.Age, I, I + 0.03f, I + 0.12f, I + 0.45f)), FxBlend.Alpha);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.04f, 0.78f, 0.9f));
            fx.ScreenFlash(Rgb(255, 236, 200), 0.2f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Natural Gift: the berry, in the colours of the type it gives the move, is drawn out and thrown, bursting in a spray of its juice and leaves.</summary>
    private static void NaturalGiftY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.2f, 6, Rgb(130, 220, 110), col.Light, 0.08f, 0.8f);
        float bud = Fx.Envelope(c.Age, 0.04f, 0.16f, 0.24f, 0.3f);
        if (bud > 0f)
        {
            fx.Sprite(FxShape.Dot, c.From + new Vector3(h * 0.5f, h * 0.55f, 0f), h * 0.3f, Fx.Fade(col.Main, bud), FxBlend.Alpha);
            fx.Sprite(FxShape.Leaf, c.From + new Vector3(h * 0.55f, h * 0.72f, 0f), h * 0.16f, Fx.Fade(Rgb(96, 190, 72), bud), FxBlend.Alpha, 0.5f);
        }
        Ball(c, fx, FxShape.Dot, 0.18f, I, 0.3f, 0.07f, col.Main, col.Light);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 8, FxShape.Drop, col.Main, col.Light, 0.1f, 1.0f, 1.2f, FxBlend.Alpha, salt: 133);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 5, FxShape.Leaf, Rgb(96, 190, 72), Rgb(160, 230, 110), 0.1f, 0.9f, 0.6f, FxBlend.Alpha, spin: 6f, salt: 134);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Power Whip: a thick green vine lashes at the target from one side and a second from the other, crossing, leaves flying.</summary>
    private static void PowerWhipY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(76, 170, 64);
        var light = Rgb(190, 244, 130);
        Whip(fx, c.Age, c.To + new Vector3(-h * 1.5f, h * 0.45f, 0f), c.To + new Vector3(h * 0.3f, -h * 0.2f, 0f), 0.04f, I - 0.04f, 0.03f, 0.05f, h * 0.22f, green, light, FxShape.Leaf, h * 0.45f, 0f);
        Whip(fx, c.Age, c.To + new Vector3(h * 1.5f, h * 0.45f, 0f), c.To + new Vector3(-h * 0.3f, -h * 0.2f, 0f), 0.1f, I, 0.03f, -0.05f, h * 0.22f, green, light, FxShape.Leaf, h * 0.45f, 0f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Leaf, green, light, 0.11f, 1.0f, 0.8f, FxBlend.Alpha, spin: 7f, salt: 135);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.3f, Fx.Fade(light, 0.8f));
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Brave Bird: the user wrapped in blue-white flame, a great beak at its head, drives in leaving a comet's tail; the recoil rings red on the user.</summary>
    private static void BraveBirdY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var blue = Rgb(110, 180, 255);
        Fx.Stream(fx, c, 0.04f, I + 0.06f, 0.26f, 14, FxShape.Flame, blue, Rgb(230, 244, 255), 0.18f, spread: 0.3f);
        float wrap = Fx.Envelope(c.Age, 0f, 0.08f, 0.26f, 0.34f);
        if (wrap > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.1f, Fx.Fade(blue, wrap * 0.5f), FxBlend.Alpha);
        Strike(c, fx, FxShape.Beak, new Vector3(-1.5f, 0.35f, 0f), I, 0.7f, Rgb(255, 250, 235), 0f, 0.28f);
        SpeedLines(c, fx, 0.06f, 0.32f, 7, 0.9f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Feather, blue, White, 0.12f, 1.0f, 0.6f, FxBlend.Alpha, spin: 5f, salt: 136);
            PulseRing(fx, c.Age, c.From, I + 0.14f, 0.35f, c.FromSize * 0.3f, c.FromSize * 1.0f, Rgb(255, 100, 90), Rgb(255, 200, 150));
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Head Smash: the user drives its head into the target with a crack of stone, rocks and dust flying and the ground splitting; it reels from the blow.</summary>
    private static void HeadSmashY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Dust(c, fx, c.FromFeet, c.FromSize, 0.04f, 5);
        SpeedLines(c, fx, 0.06f, 0.32f, 6, 0.8f);
        Strike(c, fx, FxShape.Impact, new Vector3(-1.2f, 0.4f, 0f), I, 0.8f, Rgb(228, 204, 160), 0.3f, 0.26f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Rock, Rgb(190, 160, 110), Rgb(120, 98, 68), 0.14f, 1.1f, 1.8f, FxBlend.Alpha, spin: 5f, salt: 137);
            fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.3f, Fx.Fade(Rgb(60, 44, 34), Fx.Envelope(c.Age, I, I + 0.03f, 0.8f, 1.05f)), FxBlend.Alpha, c.Seed * 0.17f, depth: true);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.4f, Rgb(210, 180, 130), White);
            PulseRing(fx, c.Age, c.From, I + 0.14f, 0.35f, c.FromSize * 0.3f, c.FromSize * 1.0f, Rgb(255, 100, 90), Rgb(255, 200, 150));
            fx.Shake = Math.Max(fx.Shake, 0.6f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.36f));
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Leaf Storm: leaves whirl up round the user in a spiral, and a storm of them is flung across, blowing round the target.</summary>
    private static void LeafStormY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var green = ColorsOf(PokemonType.Grass);
        Swarm(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.5f, h, 0f, 0.32f, 8, FxShape.Leaf, green.Main, green.Light, 0.14f, 0.7f);
        float wind = Fx.Envelope(c.Age, 0f, 0.1f, 0.26f, 0.36f);
        if (wind > 0f) fx.Sprite(FxShape.Swirl, c.FromFeet + Vector3.UnitY * h * 0.4f, h * 1.2f, Fx.Fade(Rgb(190, 244, 150), wind * 0.5f), rotation: c.Age * 10f);
        Fx.Stream(fx, c, 0.14f, 0.72f, 0.3f, 22, FxShape.Leaf, green.Main, green.Light, 0.15f, arc: 0.03f, spread: 0.55f, spin: 10f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.06f, 0.85f, 1.1f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 6f;
                fx.Sprite(FxShape.Leaf, c.To + new Vector3(MathF.Cos(a) * c.ToSize * 0.5f, MathF.Sin(a * 1.3f) * c.ToSize * 0.35f, 0f), c.ToSize * 0.15f, Fx.Fade(green.Main, env), FxBlend.Alpha, a);
            }
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.4f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Attack Order: a swarm of little bugs pours across in lines from the user, and stings the target all over.</summary>
    private static void AttackOrderY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var brown = Rgb(70, 54, 30);
        var yellow = Rgb(255, 214, 70);
        Fx.Stream(fx, c, 0.02f, 0.68f, 0.3f, 12, FxShape.Dot, brown, Rgb(100, 80, 50), 0.2f, arc: 0.04f, spread: 0.5f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.03f, 0.68f, 0.3f, 12, FxShape.Dot, yellow, Rgb(255, 240, 150), 0.1f, arc: 0.04f, spread: 0.5f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Swarm(fx, c.Age, c.Seed, c.To, h, I, 0.95f, 6, FxShape.Dot, brown, yellow, 0.16f, 0.5f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.4f, 5, FxShape.Spark, yellow, White, 0.08f, 0.8f, 0.2f, salt: 138);
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, 0.6f, 0.8f));
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Bug Buzz: rings of sound, green and yellow, ripple out of the user with little notes, and the target shudders under the din.</summary>
    private static void BugBuzzY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var green = Rgb(150, 200, 40);
        var yellow = Rgb(255, 236, 90);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.04f - i * 0.1f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            float s = c.ScaleAt(k) * (0.8f + 1.0f * k);
            Color shade = i % 2 == 0 ? green : yellow;
            fx.Sprite(FxShape.Wave, p, s, Fx.Fade(shade, Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, ScreenAngle(c.Aim - c.From));
            fx.Sprite(FxShape.Wave, p, s * 0.8f, Fx.Fade(shade, Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, ScreenAngle(c.Aim - c.From));
        }
        Fx.Stream(fx, c, 0.04f, 0.6f, 0.34f, 6, FxShape.Note, Rgb(110, 160, 20), yellow, 0.14f, arc: 0.05f, spread: 0.4f, blend: FxBlend.Alpha);
        if (c.Age < 0.5f) fx.Sprite(FxShape.Glow, c.From, h * 0.7f, Fx.Fade(yellow, 0.25f * (0.5f + 0.5f * MathF.Sin(c.Age * 60f))), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.03f, 0.7f, 0.85f) * (0.6f + 0.4f * MathF.Sin(c.Age * 70f)));
            Tint(c, fx, I, 0.9f, yellow, 0.25f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Cross Poison: two violet slashes cross on the target in an X, venom dripping from the cuts.</summary>
    private static void CrossPoisonY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var venom = ColorsOf(PokemonType.Poison);
        foreach (var (t0, tilt) in new[] { (I - 0.22f, 0.8f), (I - 0.12f, -0.8f) })
        {
            float k = (c.Age - t0) / 0.45f;
            if (k < 0f || k > 1f || c.Cue.Missed) continue;
            float grow = Fx.EaseOut(Math.Min(1f, k * 2.4f));
            var along = new Vector3(MathF.Sin(tilt), -MathF.Cos(tilt), 0f);
            float hold = k < 0.5f ? 1f : (1f - k) / 0.5f;
            fx.Streak(FxShape.Streak, c.To, along, h * 0.28f, 4.5f * grow, Fx.Fade(venom.Main, hold), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, c.To, along, h * 0.1f, 4.5f * grow, Fx.Fade(White, hold));
        }
        if (c.Cue.Landed)
        {
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.2f, h, I + 0.04f, 6, venom.Main);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 6, FxShape.Bubble, venom.Main, venom.Light, 0.09f, 0.9f, -0.3f, salt: 139);
            Tint(c, fx, I, 0.9f, venom.Main, 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.2f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Defend Order: a swarm circles the user, then golden shell-plates flash up round it and its body glows red-gold, hardened.</summary>
    private static void DefendOrderY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Swarm(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.5f, h, 0f, 0.62f, 8, FxShape.Dot, Rgb(70, 54, 30), Rgb(255, 214, 70), 0.17f, 0.75f);
        for (int i = 0; i < 4; i++)
        {
            float a = (i - 1.5f) * 0.7f;
            Panel(fx, c.Age, c.FromFeet + new Vector3(MathF.Sin(a) * h * 0.85f, h * (0.5f + MathF.Cos(a) * 0.3f), 0f), h * 0.36f, 0.62f + i * 0.04f, Rgb(255, 214, 90));
        }
        float glow = Fx.Envelope(c.Age, 0.7f, 0.78f, 0.95f, 1.15f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(Rgb(255, 120, 80), glow * 0.45f), FxBlend.Alpha);
    }

    /// <summary>Heal Order: a swarm circles the user and green motes rise from it, a white glow breaking out with crosses.</summary>
    private static void HealOrderY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Swarm(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.5f, h, 0f, 0.62f, 8, FxShape.Dot, Rgb(70, 54, 30), Rgb(160, 255, 160), 0.17f, 0.75f);
        Rising(fx, c.Age, c.Seed, c.FromFeet, h, 0.3f, 1.15f, 4, FxShape.Cross, Rgb(160, 255, 170), 0.14f, 0.3f);
        float glow = Fx.Envelope(c.Age, 0.65f, 0.75f, 0.9f, 1.15f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(White, glow * 0.5f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.35f, h, 0.7f, Rgb(200, 255, 200), 0.4f);
    }

    /// <summary>Iron Head: the user's head gleams with steel, then is rammed into the target twice with a ring of metal and sparks.</summary>
    private static void IronHeadY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(206, 216, 236);
        float gleam = Fx.Envelope(c.Age, 0f, 0.06f, 0.14f, 0.24f);
        if (gleam > 0f) fx.Sprite(FxShape.Glow, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize * 0.7f, Fx.Fade(steel, gleam * 0.6f), FxBlend.Alpha);
        Fx.Glint(fx, c.Age, c.From + new Vector3(c.FromSize * 0.2f, c.FromSize * 0.5f, 0f), c.FromSize, 0.05f, White, 0.4f);
        SpeedLines(c, fx, 0.06f, 0.3f, 5, 0.6f);
        Strike(c, fx, FxShape.Orb, new Vector3(-1.3f, 0.3f, 0f), I, 0.5f, steel, 0f, 0.24f);
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.2f, steel, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.45f, 8, FxShape.Spark, Rgb(255, 236, 170), White, 0.08f, 1.0f, 0.5f, salt: 140);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Megahorn: a huge horn of green light is thrust into the target, rays of light flying out from the puncture.</summary>
    private static void MegahornY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(140, 210, 40);
        SpeedLines(c, fx, 0.04f, 0.3f, 6, 0.8f);
        Strike(c, fx, FxShape.Horn, new Vector3(-1.6f, 0.2f, 0f), I, 1.0f, Rgb(250, 240, 200), -MathF.PI / 4f, 0.28f);
        Strike(c, fx, FxShape.Horn, new Vector3(-1.6f, 0.2f, 0f), I, 1.1f, Fx.Fade(green, 0.6f), -MathF.PI / 4f, 0.28f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 10, FxShape.Streak, green, White, 0.1f, 1.1f, 0.1f, streak: 4f, salt: 141);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.4f, green, White);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Power Gem: a pink-white gem of light forms before the user, spins and is shot across, scattering shards of light on the target.</summary>
    private static void PowerGemY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var pink = Rgb(255, 160, 214);
        float form = Fx.Envelope(c.Age, 0f, 0.14f, 0.2f, 0.26f);
        if (form > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From + new Vector3(h * 0.6f, h * 0.3f, 0f), h * 0.8f, Fx.Fade(pink, form * 0.6f));
            fx.Sprite(FxShape.Hex, c.From + new Vector3(h * 0.6f, h * 0.3f, 0f), h * 0.42f * form, Fx.Fade(Rgb(255, 230, 245), form), FxBlend.Alpha, c.Age * 6f);
        }
        Fx.Glint(fx, c.Age, c.From + new Vector3(h * 0.7f, h * 0.45f, 0f), h, 0.12f, White, 0.35f);
        Ball(c, fx, FxShape.Hex, 0.2f, I, 0.34f, 0.05f, Rgb(255, 224, 242), pink, 8f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Shard, pink, White, 0.1f, 1.1f, 0.4f, FxBlend.Alpha, spin: 8f, salt: 142);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, 0.6f, 0.8f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Earth Power: the ground splits and glowing columns of earth burst up under the target one after another, from the left, the middle and the right.</summary>
    private static void EarthPowerY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var glow = Rgb(255, 156, 60);
        var earth = Rgb(176, 132, 80);
        for (int i = 0; i < 3; i++)
        {
            float t0 = 0.0f + i * 0.14f;
            var feet = c.ToFeet + new Vector3((i - 1) * h * 0.6f, 0f, 0f);
            Shaft(fx, c.Age, feet, t0, 0.45f, h * 1.15f, h * 0.3f, glow, Rgb(255, 236, 170));
            float k = (c.Age - t0) / 0.3f;
            if (k > 0f && k < 1f) Fx.Burst(fx, c.Seed, c.Age, feet + Vector3.UnitY * h * 0.1f, h, t0, 0.5f, 3, FxShape.Rock, earth, Rgb(120, 92, 60), 0.1f, 0.7f, 2f, FxBlend.Alpha, spin: 5f, salt: 143 + i);
            GroundRing(fx, c.Age, feet, t0, 0.35f, h * 0.7f, Fx.Fade(Rgb(255, 210, 140), 0.8f));
        }
        if (c.Cue.Landed) fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, 0.6f, 0.8f));
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Rock Wrecker: stones are drawn in and packed into a boulder before the user, which is flung and bursts into pieces on the target.</summary>
    private static void RockWreckerY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var rock = Rgb(176, 150, 108);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - 0.02f - i * 0.02f) / 0.2f;
            if (k < 0f || k > 1f) continue;
            var p = c.From + new Vector3(h * 0.7f, h * 0.2f, 0f) + Fx.Direction(c.Seed, 330 + i) * h * 0.9f * (1f - Fx.EaseOut(k));
            fx.Sprite(FxShape.Rock, p, h * 0.14f, Fx.Fade(rock, 1f), FxBlend.Alpha, k * 6f + i);
        }
        float pack = Fx.Envelope(c.Age, 0.14f, 0.22f, 0.26f, 0.3f);
        if (pack > 0f) fx.Sprite(FxShape.Rock, c.From + new Vector3(h * 0.7f, h * 0.2f, 0f), h * 0.5f * pack, Fx.Fade(rock, 1f), FxBlend.Alpha, 0.4f);
        Ball(c, fx, FxShape.Rock, 0.26f, I, 0.55f, 0.06f, rock, Rgb(240, 214, 160), 5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 9, FxShape.Rock, rock, Rgb(120, 98, 68), 0.15f, 1.1f, 1.8f, FxBlend.Alpha, spin: 5f, salt: 146);
            Dust(c, fx, c.ToFeet, c.ToSize, I, 6);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Flare Blitz: flames wrap the user, which hurtles in as a comet of fire with a great burst of it on the target, the recoil burning back on the user.</summary>
    private static void FlareBlitzY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        float wrap = Fx.Envelope(c.Age, 0f, 0.08f, 0.26f, 0.34f);
        if (wrap > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, c.FromSize * 1.1f, Fx.Fade(fire.Main, wrap * 0.6f));
            for (int i = 0; i < 5; i++)
                fx.Sprite(FxShape.Flame, c.From + new Vector3((i - 2) * c.FromSize * 0.22f, c.FromSize * (0.2f + 0.2f * MathF.Sin(c.Age * 18f + i)), 0f), c.FromSize * 0.3f, Fx.Fade(fire.Light, wrap));
        }
        Fx.Stream(fx, c, 0.08f, I + 0.05f, 0.22f, 12, FxShape.Flame, fire.Main, fire.Light, 0.2f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Flame, fire.Main, fire.Light, 0.2f, 1.1f, -0.3f, salt: 147);
            PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.3f, h * 1.5f, fire.Main, fire.Light);
            PulseRing(fx, c.Age, c.From, I + 0.14f, 0.35f, c.FromSize * 0.3f, c.FromSize * 1.0f, Rgb(255, 90, 60), Rgb(255, 220, 150));
            Tint(c, fx, I, 0.9f, Rgb(220, 60, 30), 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.32f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Overheat: the user glows red-hot and trembles, then lets off a vast white-yellow burst of flame, a ring of fire spreading on the ground under the target.</summary>
    private static void OverheatY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var fire = ColorsOf(PokemonType.Fire);
        float heat = Fx.Envelope(c.Age, 0f, 0.1f, 0.26f, 0.36f);
        if (heat > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From, h * 1.1f, Fx.Fade(Rgb(255, 70, 40), heat * 0.55f), FxBlend.Alpha);
            fx.Shake = Math.Max(fx.Shake, 0.15f * heat);
        }
        Fx.Stream(fx, c, 0.1f, I + 0.1f, 0.26f, 14, FxShape.Flame, Rgb(255, 210, 90), White, 0.2f, spread: 0.35f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 12, FxShape.Flame, fire.Main, Rgb(255, 240, 170), 0.22f, 1.2f, -0.2f, salt: 148);
            fx.Sprite(FxShape.Star, c.To, c.ToSize * 1.15f * Fx.EaseOut((c.Age - I) / 0.3f), Fx.Fade(Rgb(255, 214, 120), Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.4f) * 0.8f), FxBlend.Alpha);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, c.ToSize * 1.9f, Fx.Fade(Rgb(255, 170, 70), 0.95f));
            Tint(c, fx, I, 1.05f, Rgb(255, 120, 40), 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.36f));
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Solar Beam: motes of sunlight are drawn into the user in a bright bloom, and a thick golden-green beam is held on the target, flooding it with light.</summary>
    private static void SolarBeamY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var gold = Rgb(255, 236, 110);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.22f, 10, gold, White, 0.09f, 1.1f);
        float bloom = Fx.Envelope(c.Age, 0f, 0.18f, 0.22f, 0.3f);
        if (bloom > 0f) fx.Sprite(FxShape.Glow, c.From, h * 1.0f * bloom, Fx.Fade(gold, 0.7f));
        Fx.Beam(fx, c, 0.2f, 0.84f, 0.26f, Rgb(200, 240, 80), White, 6);
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.3f, Fx.Fade(gold, 0.6f * Fx.Envelope(c.Age, I, I + 0.05f, 0.7f, 0.9f)), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 8, FxShape.Spark, gold, White, 0.1f, 1.0f, 0.2f, salt: 149);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.03f, 0.72f, 0.86f));
            fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.04f, I + 0.2f));
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Energy Ball: green light swells into a ball before the user, which is thrown and bursts on the target in a bloom of green with leaves.</summary>
    private static void EnergyBallY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var green = Rgb(110, 220, 90);
        float swell = Fx.Envelope(c.Age, 0f, 0.16f, 0.2f, 0.26f);
        if (swell > 0f)
        {
            fx.Sprite(FxShape.Glow, c.From + new Vector3(h * 0.55f, h * 0.3f, 0f), h * 0.9f * swell, Fx.Fade(green, 0.7f));
            fx.Sprite(FxShape.Orb, c.From + new Vector3(h * 0.55f, h * 0.3f, 0f), h * 0.45f * swell, Fx.Fade(Rgb(214, 255, 190), 0.95f), rotation: c.Age * 5f);
        }
        Gather(fx, c.Age, c.Seed, c.From + new Vector3(h * 0.55f, h * 0.3f, 0f), h, 0f, 0.18f, 6, green, White, 0.07f, 0.6f);
        Ball(c, fx, FxShape.Orb, 0.2f, I, 0.38f, 0.06f, Rgb(214, 255, 190), green, 5f);
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 1.5f * Fx.EaseOut((c.Age - I) / 0.25f), Fx.Fade(green, Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.35f) * 0.8f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 7, FxShape.Leaf, green, Rgb(214, 255, 190), 0.11f, 1.0f, 0.5f, FxBlend.Alpha, spin: 7f, salt: 150);
            Tint(c, fx, I, 0.9f, Rgb(130, 255, 110), 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Leaf Blade: two blades of green light cross on the target in an X, leaves flying off the cuts.</summary>
    private static void LeafBladeY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var green = Rgb(100, 210, 80);
        float glow = Fx.Envelope(c.Age, 0f, 0.06f, 0.14f, 0.22f);
        if (glow > 0f) fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.8f, Fx.Fade(green, glow * 0.45f), FxBlend.Alpha);
        foreach (var (t0, tilt) in new[] { (I - 0.24f, 0.7f), (I - 0.13f, -0.7f) })
        {
            float k = (c.Age - t0) / 0.45f;
            if (k < 0f || k > 1f || c.Cue.Missed) continue;
            float grow = Fx.EaseOut(Math.Min(1f, k * 2.4f));
            var along = new Vector3(MathF.Sin(tilt), -MathF.Cos(tilt), 0f);
            float hold = k < 0.5f ? 1f : (1f - k) / 0.5f;
            fx.Streak(FxShape.Streak, c.To, along, h * 0.26f, 4.5f * grow, Fx.Fade(green, hold), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, c.To, along, h * 0.1f, 4.5f * grow, Fx.Fade(Rgb(236, 255, 220), hold));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I - 0.04f, 0.6f, 8, FxShape.Leaf, green, Rgb(190, 244, 130), 0.11f, 1.0f, 0.8f, FxBlend.Alpha, spin: 8f, salt: 151);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.08f, I + 0.22f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Aura Sphere: blue rings are drawn in to a point before the user, a sphere of aura is fired with rings turning round it, and bursts in blue-white on the target.</summary>
    private static void AuraSphereY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var blue = Rgb(80, 150, 255);
        var at = c.From + new Vector3(h * 0.55f, h * 0.3f, 0f);
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.06f) / 0.22f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, at, h * (1.0f - 0.7f * k), Fx.Fade(blue, k < 0.8f ? 0.8f : (1f - k) * 4f));
        }
        float swell = Fx.Envelope(c.Age, 0.06f, 0.2f, 0.22f, 0.28f);
        if (swell > 0f) fx.Sprite(FxShape.Orb, at, h * 0.42f * swell, Fx.Fade(Rgb(210, 236, 255), 0.95f), rotation: c.Age * 6f);
        Ball(c, fx, FxShape.Orb, 0.22f, I, 0.4f, 0.04f, Rgb(210, 236, 255), blue, 7f);
        float kk = (c.Age - 0.22f) / (I - 0.22f);
        if (kk > 0f && kk < 1f)
        {
            var p = Lob(c.From, c.Aim, kk, 0.04f);
            fx.Sprite(FxShape.Ring, p, c.ToSize * 0.55f, Fx.Fade(blue, 0.7f), rotation: c.Age * 10f);
        }
        if (c.Cue.Landed)
        {
            PulseRing(fx, c.Age, c.To, I, 0.45f, c.ToSize * 0.3f, c.ToSize * 1.5f, blue, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.55f, 8, FxShape.Spark, blue, White, 0.1f, 1.0f, 0.2f, salt: 152);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Extreme Speed: the user is a streak of white, ghosts of it flickering across, and three quick blows land one after another.</summary>
    private static void ExtremeSpeedY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        SpeedLines(c, fx, 0.0f, 0.3f, 12, 1.2f);
        for (int g = 0; g < 3; g++)
        {
            float k = (c.Age - 0.04f - g * 0.05f) / 0.18f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Glow, Vector3.Lerp(c.From, c.To, Fx.EaseOut(k)), c.FromSize * 0.8f, Fx.Fade(Rgb(255, 250, 235), (1f - k) * 0.55f));
        }
        Blows(c, col, fx, 3, 0.06f, 0.3f, 0.2f);
        if (c.Cue.Landed) PulseRing(fx, c.Age, c.To, I, 0.35f, h * 0.3f, h * 1.2f, Rgb(255, 214, 170), White);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Mirror Coat: a pane of silver glass stands up before the user, flashes with the blow it has taken and sends it back as a beam of pink light.</summary>
    private static void MirrorCoatY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var pink = Rgb(255, 130, 200);
        var pane = c.FromFeet + new Vector3(h * 0.65f, h * 0.55f, 0f);
        Panel(fx, c.Age, pane, h * 0.85f, 0.0f, Rgb(214, 226, 250));
        Fx.Glint(fx, c.Age, pane + new Vector3(-h * 0.15f, h * 0.2f, 0f), h, 0.08f, White, 0.5f);
        Fx.Glint(fx, c.Age, pane + new Vector3(h * 0.12f, -h * 0.2f, 0f), h, 0.16f, pink, 0.4f);
        float flash = Fx.Envelope(c.Age, 0.16f, 0.2f, 0.24f, 0.32f);
        if (flash > 0f) fx.Sprite(FxShape.Glow, pane, h * 1.2f, Fx.Fade(White, flash * 0.7f));
        Fx.Beam(fx, c, 0.2f, 0.6f, 0.12f, pink, White, 5);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Shard, Rgb(214, 226, 250), pink, 0.1f, 1.0f, 0.4f, FxBlend.Alpha, spin: 7f, salt: 153);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Surf: a great wave rises behind the user and rolls the length of the field, crashing over the target in foam, the water standing in drops about it.</summary>
    private static void SurfY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var water = ColorsOf(PokemonType.Water);
        Fx.WaveWall(fx, c, 0.04f, I + 0.02f, water.Main, Rgb(236, 248, 255));
        float k = (c.Age - 0.0f) / 0.45f;
        if (k > 0f && k < 1f)
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Wave, Vector3.Lerp(c.FromFeet, c.ToFeet, Fx.EaseOut(k)) + new Vector3(0f, h * (0.2f + 0.2f * i), (i - 1.5f) * h * 0.25f), h * 0.6f, Fx.Fade(water.Light, (1f - k) * 0.7f), FxBlend.Alpha, ScreenAngle(c.To - c.From));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.4f, h, I, 0.8f, 14, FxShape.Drop, water.Main, water.Light, 0.11f, 1.1f, 1.6f, FxBlend.Alpha, salt: 154);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.9f, Fx.Fade(White, 0.9f));
            Tint(c, fx, I, 1.0f, Rgb(60, 180, 190), 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, 0.6f, 0.8f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Heat Wave: waves of scorching air, orange and red, sweep over the foes with embers in them, the air shimmering.</summary>
    private static void HeatWaveY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = ColorsOf(PokemonType.Fire);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.02f - i * 0.1f) / 0.46f;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k)) + Vector3.UnitY * h * (i % 2 == 0 ? 0.12f : -0.12f);
            fx.Sprite(FxShape.Wave, p, c.ScaleAt(k) * (1.0f + 1.0f * k), Fx.Fade(i % 2 == 0 ? fire.Main : Rgb(255, 90, 50), Math.Min(1f, (1f - k) * 3f)), FxBlend.Alpha, ScreenAngle(c.Aim - c.From));
        }
        Fx.Stream(fx, c, 0.04f, 0.72f, 0.4f, 12, FxShape.Ember, fire.Main, fire.Light, 0.1f, arc: 0.03f, spread: 0.55f);
        if (c.Cue.Landed)
        {
            float haze = Fx.Envelope(c.Age, I, I + 0.08f, 0.8f, 1.05f);
            fx.Sprite(FxShape.Smoke, c.To, h * 1.3f, Fx.Fade(Rgb(255, 120, 60, 200), haze * 0.3f), FxBlend.Alpha);
            Tint(c, fx, I, 1.0f, Rgb(255, 100, 50), 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.03f, 0.7f, 0.9f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Acid: two blobs of acid, purple and green, are flung across and splash on the target, fizzing and eating at it.</summary>
    private static void AcidY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var purple = Rgb(170, 70, 200);
        var green = Rgb(150, 230, 90);
        for (int i = 0; i < 2; i++)
        {
            float k = (c.Age - 0.02f - i * 0.05f) / (I - 0.07f);
            if (k < 0f || k > 1f) continue;
            var p = Lob(c.From, c.Aim, k, 0.05f) + Vector3.UnitY * (i - 0.5f) * h * 0.2f;
            fx.Sprite(FxShape.Drop, p, h * 0.34f, Fx.Fade(i == 0 ? purple : green, 1f), FxBlend.Alpha, MathF.PI * 0.5f);
            fx.Sprite(FxShape.Bubble, p + new Vector3(h * 0.04f, h * 0.05f, 0f), h * 0.12f, Fx.Fade(White, 1f));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Drop, purple, green, 0.12f, 1.0f, 1.5f, FxBlend.Alpha, salt: 155);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 5, FxShape.Bubble, Rgb(200, 255, 180), White, 0.08f, 0.7f, -0.4f, salt: 156);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.2f, h, I + 0.05f, 4, purple);
            Tint(c, fx, I, 0.9f, purple, 0.35f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Constrict: pink feelers coil round the target in three quick bands, which squeeze in a series of small knocks and then let go.</summary>
    private static void ConstrictY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Wound(c, fx, 3, 0.2f, 0.7f, Rgb(230, 130, 160), Rgb(255, 214, 224));
        if (!c.Cue.Landed) { fx.Land(I); return; }
        for (int i = 0; i < 4; i++)
        {
            float at = I - 0.05f + i * 0.1f;
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, at, 0.25f, 2, FxShape.Star, Rgb(255, 236, 190), White, 0.07f, 0.6f, 0f, FxBlend.Alpha, spin: 4f, salt: 157 + i);
            fx.Shake = Math.Max(fx.Shake, 0.12f * Fx.Envelope(c.Age, at, at + 0.02f, at + 0.04f, at + 0.1f));
        }
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Razor Wind: a whirlwind gathers round the user, then two crescents of keen wind are let off in turn and cut across the foes.</summary>
    private static void RazorWindY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float whirl = Fx.Envelope(c.Age, 0f, 0.08f, 0.2f, 0.28f);
        if (whirl > 0f) fx.Sprite(FxShape.Swirl, c.From, h * 1.2f, Fx.Fade(Rgb(214, 240, 255), whirl * 0.55f), rotation: c.Age * 12f);
        Gather(fx, c.Age, c.Seed, c.From, h, 0f, 0.2f, 6, Rgb(214, 240, 255), White, 0.07f, 0.8f);
        Crescent(fx, c.Age, c.From, c.Aim, 0.14f, I - 0.12f, c.FromSize * 0.5f, c.ToSize * 0.9f, Rgb(214, 244, 255), White);
        Crescent(fx, c.Age, c.From + Vector3.UnitY * h * 0.15f, c.Aim, 0.2f, I - 0.06f, c.FromSize * 0.45f, c.ToSize * 0.8f, Rgb(190, 232, 255), White);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.45f, 6, FxShape.Streak, Rgb(214, 244, 255), White, 0.09f, 1.0f, 0f, streak: 4f, salt: 161);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.3f));
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Tailwind: gusts of white air stream past the user's side from behind, feathers and leaves blown with them, a spiral of wind turning on the ground.</summary>
    private static void TailwindY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var air = Rgb(214, 240, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        for (int i = 0; i < 9; i++)
        {
            float u = (c.Age * (1.6f + 0.3f * Fx.Rand(c.Seed, i, 340)) + Fx.Rand(c.Seed, i, 341)) % 1f;
            var p = c.FromFeet + new Vector3((u - 0.5f) * h * 3.2f, h * (0.15f + 0.9f * Fx.Rand(c.Seed, i, 342)), Fx.Signed(c.Seed, i, 343) * h * 0.2f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.12f, 5f, Fx.Fade(air, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            fx.Streak(FxShape.Streak, p, Vector3.UnitX, h * 0.05f, 5f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)));
        }
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.1f + i * 0.33f) % 1f;
            var p = c.FromFeet + new Vector3((u - 0.5f) * h * 3f, h * (0.4f + 0.3f * MathF.Sin(u * 9f + i)), 0f);
            fx.Sprite(i == 1 ? FxShape.Leaf : FxShape.Feather, p, h * 0.24f, Fx.Fade(i == 1 ? Rgb(120, 200, 90) : White, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 8f);
        }
        fx.Flat(FxShape.Swirl, c.FromFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.1f, Fx.Fade(air, env * 0.5f), rotation: c.Age * 8f, depth: true);
    }

    /// <summary>Bullet Punch: two steel fists, quick as bullets, one after the other, each with a ring of metal and sparks.</summary>
    private static void BulletPunchY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = Rgb(206, 216, 236);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, 0.2f, 0f), I - 0.1f, 0.55f, Rgb(176, 190, 220), -0.2f, 0.18f);
        Strike(c, fx, FxShape.Fist, new Vector3(-1.4f, -0.1f, 0f), I, 0.65f, Rgb(176, 190, 220), -0.2f, 0.18f);
        SpeedLines(c, fx, 0.0f, 0.26f, 6, 0.6f);
        if (c.Cue.Landed)
        {
            foreach (float at in new[] { I - 0.1f, I })
            {
                PulseRing(fx, c.Age, c.To, at, 0.3f, h * 0.2f, h * 0.9f, steel, White);
                Fx.Burst(fx, c.Seed, c.Age, c.To, h, at, 0.35f, 4, FxShape.Spark, Rgb(255, 236, 170), White, 0.07f, 0.9f, 0.3f, salt: 162 + (at > I - 0.05f ? 1 : 0));
            }
            fx.Shake = Math.Max(fx.Shake, 0.3f * (Fx.Envelope(c.Age, I - 0.1f, I - 0.08f, I - 0.04f, I + 0.02f) + Fx.Envelope(c.Age, I, I + 0.02f, I + 0.06f, I + 0.18f)));
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Dive: a splash and a ring where the user goes under, ripples racing along the ground to the target, and a spout of water erupting under it.</summary>
    private static void DiveY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var water = ColorsOf(PokemonType.Water);
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * c.FromSize * 0.2f, c.FromSize, 0.02f, 0.5f, 8, FxShape.Drop, water.Main, water.Light, 0.1f, 0.8f, 1.6f, FxBlend.Alpha, salt: 164);
        GroundRing(fx, c.Age, c.FromFeet, 0.02f, 0.4f, c.FromSize * 1.3f, Fx.Fade(water.Light, 0.9f));
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.08f - i * 0.05f) / 0.26f;
            if (k < 0f || k > 1f) continue;
            fx.Flat(FxShape.Ring, Vector3.Lerp(c.FromFeet, c.ToFeet, k) + Vector3.UnitY * 0.04f, Vector3.UnitY, h * (0.5f + 0.2f * i), Fx.Fade(water.Light, (1f - k) * 0.85f), depth: true);
        }
        if (c.Cue.Landed)
        {
            Shaft(fx, c.Age, c.ToFeet, I - 0.04f, 0.5f, h * 1.2f, h * 0.42f, water.Main, White);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.5f, h, I, 0.7f, 12, FxShape.Drop, water.Main, water.Light, 0.11f, 1.0f, 1.8f, FxBlend.Alpha, salt: 165);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.6f, Fx.Fade(White, 0.9f));
            fx.Shake = Math.Max(fx.Shake, 0.4f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.12f, I + 0.3f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Dynamic Punch: a fist wreathed in orange fire is driven into the target with a great star, rings and a jolt that rocks the whole picture.</summary>
    private static void DynamicPunchY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var fire = Rgb(255, 130, 50);
        float flame = Fx.Envelope(c.Age, 0.0f, 0.08f, I - 0.04f, I + 0.04f);
        if (flame > 0f) fx.Sprite(FxShape.Flame, c.To + new Vector3(-h * 0.9f * (1f - Fx.EaseOut(c.Age / I)), h * 0.1f, 0f), h * 0.9f, Fx.Fade(fire, flame * 0.8f));
        Strike(c, fx, FxShape.Fist, new Vector3(-1.5f, 0.2f, 0f), I, 0.85f, Rgb(255, 214, 170), -0.2f, 0.26f);
        SpeedLines(c, fx, 0.04f, 0.3f, 6, 0.8f);
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Star, c.To, h * 1.5f * Fx.EaseOut((c.Age - I) / 0.25f), Fx.Fade(Rgb(255, 230, 170), Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.38f)), FxBlend.Alpha, 0.3f);
            PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.3f, h * 1.6f, fire, White);
            PulseRing(fx, c.Age, c.To, I + 0.1f, 0.4f, h * 0.3f, h * 1.3f, Rgb(255, 90, 60), Rgb(255, 214, 150));
            Tint(c, fx, I, 1.0f, Rgb(210, 50, 40), 0.35f);
            fx.Shake = Math.Max(fx.Shake, 0.6f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.16f, I + 0.4f));
        }
        Impact(c, col, fx, 1.25f);
    }

    /// <summary>Guard Swap: shield motes in blue fly from the user to the foe while the foe's cyan ones fly back, crossing, and a pane flashes at each end.</summary>
    private static void GuardSwapY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var blue = Rgb(110, 170, 255);
        var cyan = Rgb(120, 240, 230);
        Fx.Stream(fx, c, 0.04f, 0.78f, 0.36f, 8, FxShape.Hex, blue, White, 0.24f, arc: 0.1f, spread: 0.2f, spin: 4f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.78f, 0.36f, 8, FxShape.Hex, cyan, White, 0.24f, arc: -0.1f, spread: 0.2f, spin: -4f, blend: FxBlend.Alpha, reverse: true);
        Panel(fx, c.Age, c.From, c.FromSize * 0.7f, 0.3f, blue);
        Panel(fx, c.Age, c.To, c.ToSize * 0.7f, 0.5f, cyan);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.4f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.3f, c.ToSize, 0.6f, White, 0.4f);
    }

    /// <summary>Gunk Shot: a heavy lump of garbage and filth, dark green and violet, is hurled across and splatters over the target in a rank mess.</summary>
    private static void GunkShotY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var gunk = Rgb(100, 70, 120);
        var slime = Rgb(120, 160, 60);
        Ball(c, fx, FxShape.Dot, 0.06f, I, 0.5f, 0.05f, gunk, Rgb(160, 110, 190));
        Fx.Stream(fx, c, 0.08f, I, 0.26f, 8, FxShape.Dot, slime, gunk, 0.1f, arc: 0.05f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 11, FxShape.Dot, gunk, slime, 0.22f, 1.0f, 1.4f, FxBlend.Alpha, salt: 166);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.9f, 4, FxShape.Smoke, Rgb(120, 90, 140, 220), Rgb(80, 70, 90, 200), 0.22f, 0.6f, -0.3f, FxBlend.Alpha, salt: 167);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.25f, h, I + 0.04f, 6, slime);
            Tint(c, fx, I, 1.0f, gunk, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Outrage: the user wrapped in the red of rage with anger marks beating on it, flames of dragon fire flung at the target twice and the field pulsing red.</summary>
    private static void OutrageY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        var red = Rgb(255, 70, 50);
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.25f, h * 1.1f, 0f), h, 0f, 0.9f);
        float rage = Fx.Envelope(c.Age, 0f, 0.08f, 0.7f, 1.0f);
        if (rage > 0f) fx.Sprite(FxShape.Glow, c.From, h * 0.95f, Fx.Fade(red, rage * (0.25f + 0.15f * MathF.Sin(c.Age * 20f))), FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.5f, 0.24f, 10, FxShape.Flame, Rgb(255, 130, 60), Rgb(255, 230, 150), 0.2f, spread: 0.4f);
        Fx.Stream(fx, c, 0.3f, 0.8f, 0.24f, 10, FxShape.Flame, Rgb(255, 130, 60), Rgb(255, 230, 150), 0.2f, spread: 0.4f);
        if (c.Cue.Landed)
        {
            foreach (var (t0, i) in new[] { (I, 0), (I + 0.3f, 1) })
            {
                Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, t0, 0.45f, 5, FxShape.Flame, red, Rgb(255, 214, 120), 0.18f, 0.9f, -0.2f, salt: 168 + i);
                PulseRing(fx, c.Age, c.To, t0, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.3f, red, Rgb(255, 214, 150));
                fx.Shake = Math.Max(fx.Shake, 0.35f * Fx.Envelope(c.Age, t0, t0 + 0.02f, t0 + 0.1f, t0 + 0.26f));
            }
            Tint(c, fx, I, 1.1f, red, 0.3f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Perish Song: grey and violet notes drift out over both sides and a dark swirl turns under every Pokémon, which is tinted grey as the song ends.</summary>
    private static void PerishSongY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var grey = Rgb(170, 160, 200);
        var dark = Rgb(60, 50, 90);
        foreach (var (at, feet, h) in new[] { (c.From, c.FromFeet, c.FromSize), (c.Foe, c.Foe - Vector3.UnitY * c.FoeSize * 0.5f, c.FoeSize) })
        {
            float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.8f, 1.1f);
            fx.Flat(FxShape.Swirl, feet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 1.4f, Fx.Fade(dark, env * 0.95f), FxBlend.Alpha, c.Age * 4f, depth: true);
            Rising(fx, c.Age, c.Seed + (int)(at.X * 7f), at - Vector3.UnitY * h * 0.4f, h, 0.05f, 1.1f, 3, FxShape.Note, Rgb(120, 104, 170), 0.3f, 0.35f);
            float tint = Fx.Envelope(c.Age, 0.5f, 0.7f, 0.9f, 1.15f);
            if (tint > 0f) fx.Sprite(FxShape.Glow, at, h * 1.0f, Fx.Fade(Rgb(110, 100, 140), tint * 0.45f), FxBlend.Alpha);
        }
    }

    /// <summary>Power Swap: red and orange fists of power fly from the user to the foe and back, crossing in the middle, a flash at each end.</summary>
    private static void PowerSwapY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var red = Rgb(255, 110, 70);
        var orange = Rgb(255, 190, 80);
        Fx.Stream(fx, c, 0.04f, 0.78f, 0.36f, 7, FxShape.Fist, red, orange, 0.24f, arc: 0.1f, spread: 0.2f, spin: 2f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.04f, 0.78f, 0.36f, 7, FxShape.Fist, orange, red, 0.24f, arc: -0.1f, spread: 0.2f, spin: -2f, blend: FxBlend.Alpha, reverse: true);
        PulseRing(fx, c.Age, c.From, 0.3f, 0.4f, c.FromSize * 0.3f, c.FromSize * 1.0f, red, orange);
        PulseRing(fx, c.Age, c.To, 0.5f, 0.4f, c.ToSize * 0.3f, c.ToSize * 1.0f, orange, red);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.4f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.3f, c.ToSize, 0.6f, White, 0.4f);
    }

    /// <summary>Sheer Cold: a great frost spreads on the ground under the target, spears of ice thrust up round it and it is locked in white, the ice shattering in flakes.</summary>
    private static void SheerColdY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var ice = ColorsOf(PokemonType.Ice);
        float frost = Fx.Envelope(c.Age, 0f, 0.2f, 0.85f, 1.1f);
        fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.05f, Vector3.UnitY, h * 2f * Fx.EaseOut(Math.Min(1f, c.Age / 0.3f)), Fx.Fade(ice.Main, frost * 0.8f), FxBlend.Alpha, depth: true);
        for (int i = 0; i < 5; i++)
            Shaft(fx, c.Age, c.ToFeet + new Vector3((i - 2) * h * 0.35f, 0f, Fx.Signed(c.Seed, i, 350) * h * 0.2f), 0.08f + i * 0.04f, 0.6f, h * (0.9f + 0.3f * Fx.Rand(c.Seed, i, 351)), h * 0.2f, ice.Main, Rgb(236, 252, 255));
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0f, 0.7f, 0.3f, 10, FxShape.Snowflake, Rgb(236, 252, 255), ice.Light, 0.12f);
        if (c.Cue.Landed)
        {
            Tint(c, fx, I, 1.0f, Rgb(214, 244, 255), 0.5f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I + 0.1f, 0.6f, 8, FxShape.Shard, ice.Light, White, 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 170);
            fx.Shake = Math.Max(fx.Shake, 0.3f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.1f, I + 0.28f));
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Spore: a cloud of pale yellow-green spores shakes from the user's head, drifts over to the target and settles round it, sparkling.</summary>
    private static void SporeY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var spore = Rgb(214, 236, 120);
        Fx.Stream(fx, c, 0.04f, 0.7f, 0.42f, 14, FxShape.Spore, spore, Rgb(255, 250, 190), 0.22f, arc: 0.05f, spread: 0.5f, blend: FxBlend.Alpha);
        float env = Fx.Envelope(c.Age, 0.3f, 0.45f, 0.9f, 1.2f);
        for (int i = 0; i < 8 && env > 0f; i++)
        {
            float u = (c.Age * 0.7f + Fx.Rand(c.Seed, i, 352)) % 1f;
            var p = c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 353) * h * 0.5f, h * (1.1f - 0.9f * u), Fx.Signed(c.Seed, i, 354) * h * 0.2f);
            fx.Sprite(FxShape.Spore, p, h * 0.2f, Fx.Fade(spore, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 5f);
            if (i % 3 == 0) fx.Sprite(FxShape.Spark, p, h * 0.06f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)));
        }
        Tint(c, fx, 0.5f, 1.1f, spore, 0.3f);
    }

    /// <summary>Acid Armor: the user turns to liquid, wobbling in rings, a spatter of green drops falling and plopping round it four times.</summary>
    private static void AcidArmorY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var green = Rgb(140, 230, 100);
        float melt = Fx.Envelope(c.Age, 0f, 0.14f, 0.7f, 1.0f);
        fx.Sprite(FxShape.Glow, c.From, h * 1.0f, Fx.Fade(green, melt * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * (0.05f + i * 0.02f), Vector3.UnitY, h * (0.6f + 0.15f * MathF.Sin(c.Age * 14f + i * 1.7f)) * (1f + 0.3f * i), Fx.Fade(green, melt * 0.7f), FxBlend.Alpha, depth: true);
        foreach (float at in new[] { 0.3f, 0.36f, 0.46f, 0.52f })
        {
            float k = (c.Age - at) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float side = Fx.Signed(c.Seed, (int)(at * 100f), 355);
            fx.Sprite(FxShape.Drop, c.FromFeet + new Vector3(side * h * 0.5f, h * (0.8f - 0.75f * k * k), 0f), h * 0.12f, Fx.Fade(green, 1f - k * 0.5f), FxBlend.Alpha, MathF.PI);
            if (k > 0.8f) fx.Flat(FxShape.Ring, c.FromFeet + new Vector3(side * h * 0.5f, 0.05f, 0f), Vector3.UnitY, h * 0.25f * (k - 0.8f) * 5f, Fx.Fade(White, (1f - k) * 5f), depth: true);
        }
    }

    /// <summary>Aromatherapy: a lime mist rolls over and sweet petals and flowers drift down through it, sparkles and healing crosses rising.</summary>
    private static void AromatherapyY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var lime = Rgb(190, 250, 120);
        float mist = Fx.Envelope(c.Age, 0f, 0.2f, 0.75f, 1.05f);
        fx.Sprite(FxShape.Smoke, c.From, h * 1.5f, Fx.Fade(lime, mist * 0.35f), FxBlend.Alpha);
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 0.9f + Fx.Rand(c.Seed, i, 356)) % 1f;
            var p = c.FromFeet + new Vector3(Fx.Signed(c.Seed, i, 357) * h * 0.7f + MathF.Sin(u * 7f + i) * h * 0.15f, h * (1.2f - 1.1f * u), Fx.Signed(c.Seed, i, 358) * h * 0.2f);
            fx.Sprite(FxShape.Petal, p, h * 0.13f, Fx.Fade(i % 2 == 0 ? Rgb(255, 170, 210) : Rgb(255, 240, 150), mist * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 7f + i);
        }
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.4f, 1.15f, 4, FxShape.Cross, Rgb(170, 255, 170), 0.13f, 0.3f);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.3f, h, 0.7f, White, 0.4f);
    }

    /// <summary>Destiny Bond: a purple eye opens over the user and a chain of dark light links it to the foe, pulsing twice, ghost-fire flickering on both.</summary>
    private static void DestinyBondY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var ghost = ColorsOf(PokemonType.Ghost);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.1f);
        var head = c.FromFeet + Vector3.UnitY * h * 1.05f;
        fx.Sprite(FxShape.Eye, head, h * 0.26f * (0.4f + 0.6f * Fx.EaseOut(c.Age / 0.2f)), Fx.Fade(Rgb(255, 220, 255), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, head, h * 0.42f, Fx.Fade(ghost.Main, env * 0.8f));
        float reach = Fx.EaseOut(Math.Min(1f, (c.Age - 0.1f) / 0.3f));
        if (c.Age > 0.1f)
        {
            var pts = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                float s = i / 9f * reach;
                pts[i] = Vector3.Lerp(c.From, c.Foe, s) + Vector3.UnitY * MathF.Sin(s * 9f - c.Age * 12f) * h * 0.06f;
            }
            float pulse = 0.55f + 0.45f * (MathF.Sin((c.Age - 0.1f) * 20f) * 0.5f + 0.5f);
            fx.Ribbon(pts, h * 0.12f, Fx.Fade(ghost.Dark, env * 0.9f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.05f, Fx.Fade(ghost.Light, env * pulse));
        }
        foreach (var (at, size) in new[] { (c.From, c.FromSize), (c.Foe, c.FoeSize) })
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Flame, at + new Vector3((i - 1) * size * 0.3f, size * (0.45f + 0.1f * MathF.Sin(c.Age * 16f + i)), 0f), size * 0.2f, Fx.Fade(ghost.Main, env * 0.8f));
    }

    /// <summary>Gastro Acid: a cloud of yellow-green acid settles about the target's middle and eats into it, dripping, the target ringed twice.</summary>
    private static void GastroAcidY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var acid = Rgb(200, 230, 60);
        Fx.Stream(fx, c, 0.02f, I, 0.28f, 8, FxShape.Bubble, acid, Rgb(240, 255, 160), 0.12f, arc: 0.06f, spread: 0.3f);
        Fx.Stream(fx, c, 0.03f, I, 0.28f, 6, FxShape.Drop, Rgb(160, 200, 40), acid, 0.1f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.08f, 0.85f, 1.1f);
            fx.Sprite(FxShape.Smoke, c.To, h * 1.0f, Fx.Fade(acid, env * 0.4f), FxBlend.Alpha);
            PulseRing(fx, c.Age, c.To, I, 0.35f, h * 0.2f, h * 0.9f, Rgb(160, 200, 40), White);
            PulseRing(fx, c.Age, c.To, I + 0.2f, 0.35f, h * 0.2f, h * 0.9f, Rgb(160, 200, 40), White);
            Drips(fx, c.Age, c.Seed, c.To, h, I + 0.06f, 5, Rgb(160, 200, 40));
            Tint(c, fx, I, 1.0f, acid, 0.4f);
            fx.Shake = Math.Max(fx.Shake, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, 0.5f, 0.7f));
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Grudge: black-violet flames flicker round the user, red eyes open in them and wisps are drawn in from every side to burn on it.</summary>
    private static void GrudgeY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var ghost = ColorsOf(PokemonType.Ghost);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.8f, 1.1f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 2f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * (0.35f + 0.1f * MathF.Sin(c.Age * 14f + i * 2f)), MathF.Sin(a) * h * 0.25f);
            fx.Sprite(FxShape.Flame, p, h * 0.34f, Fx.Fade(ghost.Dark, env), FxBlend.Alpha);
            fx.Sprite(FxShape.Flame, p, h * 0.2f, Fx.Fade(ghost.Light, env * 0.8f));
        }
        Gather(fx, c.Age, c.Seed, c.From, h, 0.1f, 0.6f, 8, ghost.Main, ghost.Light, 0.1f, 1.1f);
        float eyes = Fx.Envelope(c.Age, 0.3f, 0.4f, 0.8f, 1.0f);
        if (eyes > 0f)
        {
            fx.Sprite(FxShape.Eye, c.From + new Vector3(-h * 0.2f, h * 0.2f, 0.05f), h * 0.2f, Fx.Fade(Rgb(255, 70, 70), eyes), FxBlend.Alpha);
            fx.Sprite(FxShape.Eye, c.From + new Vector3(h * 0.2f, h * 0.2f, 0.05f), h * 0.2f, Fx.Fade(Rgb(255, 70, 70), eyes), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.From + new Vector3(0f, h * 0.2f, 0f), h * 0.9f, Fx.Fade(Rgb(255, 60, 60), eyes * 0.25f), FxBlend.Alpha);
        }
    }

    /// <summary>Mega Kick: a giant foot swings in and kicks the target, rays flying off from the blow, the whole picture rocked.</summary>
    private static void MegaKickY9b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        SpeedLines(c, fx, 0.06f, 0.3f, 6, 0.8f);
        Strike(c, fx, FxShape.Foot, new Vector3(-1.5f, -0.2f, 0f), I, 1.0f, Rgb(255, 232, 196), 0.3f, 0.28f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 10, FxShape.Streak, Rgb(255, 214, 150), White, 0.1f, 1.1f, 0.1f, streak: 4f, salt: 171);
            fx.Sprite(FxShape.Star, c.To, h * 1.4f * Fx.EaseOut((c.Age - I) / 0.25f), Fx.Fade(Rgb(255, 240, 200), Fx.Envelope(c.Age, I, I + 0.03f, I + 0.08f, I + 0.3f)), FxBlend.Alpha, 0.2f);
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.3f, h * 1.5f, Rgb(255, 214, 170), White);
            fx.Shake = Math.Max(fx.Shake, 0.55f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.14f, I + 0.34f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Moonlight: a pale moon rises behind the user, soft beams of moonlight fall on it and stars sparkle, healing crosses rising.</summary>
    private static void MoonlightY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var moon = Rgb(236, 240, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.16f, 0.75f, 1.05f);
        var disc = c.FromFeet + new Vector3(h * 0.6f, h * 1.15f, 0f);
        fx.Sprite(FxShape.Glow, disc, h * 0.9f, Fx.Fade(moon, env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Orb, disc, h * 0.4f * (0.6f + 0.4f * Fx.EaseOut(c.Age / 0.2f)), Fx.Fade(moon, env), FxBlend.Alpha);
        for (int i = -1; i <= 1; i++)
            fx.Streak(FxShape.Beam, c.FromFeet + new Vector3(i * h * 0.3f + h * 0.3f, h * 0.6f, 0f), new Vector3(-0.3f, -1f, 0f), h * 0.2f, 3f, Fx.Fade(Rgb(200, 220, 255), env * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 359) * h * 0.6f, Fx.Rand(c.Seed, i, 360) * h * 0.7f, 0f), h, 0.2f + i * 0.14f, White, 0.3f);
        Rising(fx, c.Age, c.Seed, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.35f, 1.1f, 4, FxShape.Cross, Rgb(180, 220, 255), 0.13f, 0.3f);
    }

    /// <summary>Trick: a puff of stars, and the two Pokémon's items, a white ball and a gold one, are whisked across each other's way in arcs and switched, confetti falling.</summary>
    private static void TrickY9b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var mid = (c.From + c.To) * 0.5f;
        float poof = Fx.Envelope(c.Age, 0.04f, 0.1f, 0.2f, 0.4f);
        if (poof > 0f) fx.Sprite(FxShape.Smoke, mid, h * 0.9f, Fx.Fade(Rgb(255, 240, 255, 220), poof * 0.7f), FxBlend.Alpha);
        float k = (c.Age - 0.12f) / 0.6f;
        if (k >= 0f && k <= 1f)
        {
            var a = Lob(c.From + Vector3.UnitY * h * 0.2f, c.To + Vector3.UnitY * h * 0.2f, Fx.EaseOut(k), 0.08f);
            var b = Lob(c.To + Vector3.UnitY * h * 0.2f, c.From + Vector3.UnitY * h * 0.2f, Fx.EaseOut(k), -0.04f);
            fx.Sprite(FxShape.Glow, a, h * 0.5f, Fx.Fade(White, 0.5f));
            fx.Sprite(FxShape.Dot, a, h * 0.22f, Fx.Fade(White, 1f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, b, h * 0.5f, Fx.Fade(Rgb(255, 214, 80), 0.5f));
            fx.Sprite(FxShape.Dot, b, h * 0.22f, Fx.Fade(Rgb(255, 200, 60), 1f), FxBlend.Alpha);
        }
        Fx.Burst(fx, c.Seed, c.Age, mid, h, 0.2f, 0.7f, 8, FxShape.Star, Rgb(255, 230, 120), Rgb(255, 150, 220), 0.1f, 1.0f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 172);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.72f, White, 0.4f);
        Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.3f, c.ToSize, 0.78f, White, 0.4f);
    }
}
