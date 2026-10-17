using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y11, its first part (Y11a): the later games' moves, Generation 5 from Hone Claws to Bulldoze (numbers 468
// to 523), each a look of its own. Platinum's decompilation holds no animation for any of them, so nothing here is
// read from a game: the shapes, colours and timings are our own, taken from each move's data (type, category, contact,
// target, effect and description) and the style guide's G8 vocabulary. Every recipe lands at ImpactTime (FxList.Land,
// which Impact sets), is over by EffectTime and stays under MaxQuads. A move on its user plays on the user; one that
// is cast on the user's side but is for the foes, or on the whole field, puts its look between the two (c.Foe).
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later1() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hone Claws"] = HoneClawsY11,
        ["Wide Guard"] = WideGuardY11,
        ["Guard Split"] = GuardSplitY11,
        ["Power Split"] = PowerSplitY11,
        ["Wonder Room"] = WonderRoomY11,
        ["Psyshock"] = PsyshockY11,
        ["Venoshock"] = VenoshockY11,
        ["Autotomize"] = AutotomizeY11,
        ["Rage Powder"] = RagePowderY11,
        ["Telekinesis"] = TelekinesisY11,
        ["Magic Room"] = MagicRoomY11,
        ["Smack Down"] = SmackDownY11,
        ["Storm Throw"] = StormThrowY11,
        ["Flame Burst"] = FlameBurstY11,
        ["Sludge Wave"] = SludgeWaveY11,
        ["Quiver Dance"] = QuiverDanceY11,
        ["Heavy Slam"] = HeavySlamY11,
        ["Synchronoise"] = SynchronoiseY11,
        ["Electro Ball"] = ElectroBallY11,
        ["Soak"] = SoakY11,
        ["Flame Charge"] = FlameChargeY11,
        ["Coil"] = CoilY11,
        ["Low Sweep"] = LowSweepY11,
        ["Acid Spray"] = AcidSprayY11,
        ["Foul Play"] = FoulPlayY11,
        ["Simple Beam"] = SimpleBeamY11,
        ["Entrainment"] = EntrainmentY11,
        ["After You"] = AfterYouY11,
        ["Round"] = RoundY11,
        ["Echoed Voice"] = EchoedVoiceY11,
        ["Chip Away"] = ChipAwayY11,
        ["Clear Smog"] = ClearSmogY11,
        ["Stored Power"] = StoredPowerY11,
        ["Quick Guard"] = QuickGuardY11,
        ["Ally Switch"] = AllySwitchY11,
        ["Scald"] = ScaldY11,
        ["Shell Smash"] = ShellSmashY11,
        ["Heal Pulse"] = HealPulseY11,
        ["Hex"] = HexY11,
        ["Sky Drop"] = SkyDropY11,
        ["Shift Gear"] = ShiftGearY11,
        ["Circle Throw"] = CircleThrowY11,
        ["Incinerate"] = IncinerateY11,
        ["Quash"] = QuashY11,
        ["Acrobatics"] = AcrobaticsY11,
        ["Reflect Type"] = ReflectTypeY11,
        ["Retaliate"] = RetaliateY11,
        ["Final Gambit"] = FinalGambitY11,
        ["Bestow"] = BestowY11,
        ["Inferno"] = InfernoY11,
        ["Water Pledge"] = WaterPledgeY11,
        ["Fire Pledge"] = FirePledgeY11,
        ["Grass Pledge"] = GrassPledgeY11,
        ["Volt Switch"] = VoltSwitchY11,
        ["Struggle Bug"] = StruggleBugY11,
        ["Bulldoze"] = BulldozeY11
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A unit vector from the user toward the foe across the field (flat), for moves cast on the user's side.</summary>
    private static Vector3 L1Toward(in FxCtx c)
    {
        var d = c.Foe - c.From;
        d.Y = 0f;
        return d.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(d);
    }

    /// <summary>The middle of the field between the user and the foe across from it, at ground level (a room's, a weather's).</summary>
    private static Vector3 L1Field(in FxCtx c)
    {
        var a = c.FromFeet;
        var b = c.Foe - Vector3.UnitY * c.FoeSize * 0.5f;
        return (a + b) * 0.5f;
    }

    /// <summary>Two orbs, one from each Pokémon, sliding to the middle and meeting there at the landing, then parting again in the other's colour.</summary>
    private static void L1Trade(in FxCtx c, FxList fx, FxShape shape, Color mine, Color theirs, Color rim)
    {
        fx.Land(I);
        float env = Fx.Envelope(c.Age, 0f, 0.05f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var mid = (c.From + c.To) * 0.5f + Vector3.UnitY * c.ToSize * 0.35f;
        float s = c.ToSize * 0.26f;
        if (c.Age < I)
        {
            float e = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f));
            foreach (var (from, col) in new[] { (c.From, mine), (c.To, theirs) })
            {
                var p = Vector3.Lerp(from, mid, e);
                fx.Sprite(FxShape.Glow, p, s * 2f, Fx.Fade(col, env * 0.6f));
                fx.Sprite(shape, p, s, Fx.Fade(col, env), FxBlend.Alpha, c.Age * 5f);
            }
        }
        else
        {
            float e = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.4f, 0f, 1f));
            var mixed = Fx.Mix(mine, theirs, 0.5f);
            // Each goes home in the other's colour
            foreach (var (home, col) in new[] { (c.From, theirs), (c.To, mine) })
            {
                var p = Vector3.Lerp(mid, home, e);
                fx.Sprite(FxShape.Glow, p, s * 2f, Fx.Fade(Fx.Mix(mixed, col, e), env * 0.6f));
                fx.Sprite(shape, p, s * (1f - 0.4f * e), Fx.Fade(Fx.Mix(mixed, col, e), env), FxBlend.Alpha, c.Age * 5f);
            }
            Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.2f, c.FromSize, I + 0.38f, theirs, 0.4f);
            Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * c.ToSize * 0.2f, c.ToSize, I + 0.38f, mine, 0.4f);
        }
        PulseRing(fx, c.Age, mid, I, 0.4f, s * 0.8f, s * 4.5f, Fx.Mix(mine, theirs, 0.5f), rim);
    }

    /// <summary>Hexagonal panels of a barrier standing in a row from <paramref name="a"/> to <paramref name="b"/>, rising one after another and shimmering.</summary>
    private static void L1Barrier(FxList fx, float age, Vector3 a, Vector3 b, float h, int count, Color body, Color rim, float size)
    {
        float env = Fx.Envelope(age, 0.02f, 0.16f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float u = count == 1 ? 0.5f : i / (float)(count - 1);
            float rise = Fx.EaseOut(Math.Clamp((age - 0.04f - 0.03f * i) / 0.18f, 0f, 1f));
            var p = Vector3.Lerp(a, b, u) + Vector3.UnitY * h * (0.1f + 0.42f * rise) + new Vector3(0f, 0f, 0.15f);
            float shimmer = 0.75f + 0.25f * MathF.Sin(age * 14f + i * 1.7f);
            fx.Sprite(FxShape.Hex, p, h * size, Fx.Fade(body, env * 0.8f * shimmer), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, p, h * size * 1.06f, Fx.Fade(rim, env * 0.35f * shimmer));
        }
        // A sheen sweeping along the row
        float k = ((age - 0.25f) / 0.5f);
        if (k > 0f && k < 1f)
            fx.Sprite(FxShape.Spark, Vector3.Lerp(a, b, k) + Vector3.UnitY * h * 0.5f + new Vector3(0f, 0f, 0.2f), h * 0.3f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
    }

    /// <summary>A pillar standing up from the foot of the target with a ground ring and a flurry round it: a Pledge's, a Pledge's own colour.</summary>
    private static void L1Column(in FxCtx c, FxList fx, Color body, Color core, Color ring, float t0, float width)
    {
        float h = c.ToSize;
        Shaft(fx, c.Age, c.ToFeet, t0, 0.8f, h * 2.1f, h * width, body, core);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, t0 + 0.04f, 0.5f, h * 1.2f, ring);
            GroundRing(fx, c.Age, c.ToFeet, t0 + 0.14f, 0.5f, h * 0.8f, Fx.Mix(ring, White, 0.4f));
        }
    }

    /// <summary>A rising column of <paramref name="shape"/> sprites winding round <paramref name="feet"/>, in <paramref name="count"/> turns of the spiral.</summary>
    private static void L1Spiral(FxList fx, float age, int seed, Vector3 feet, float h, float t0, float t1, int count, FxShape shape, Color a, Color b, float size, float radius, float height)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.1f, t1 - 0.2f, t1);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float u = ((age - t0) * 0.9f + i / (float)count) % 1f;
            float ang = u * MathF.Tau * 2f + i * 0.9f;
            float r = h * radius * (1f - 0.25f * u);
            var p = feet + new Vector3(MathF.Cos(ang) * r, h * height * u, MathF.Sin(ang) * r * 0.5f + 0.05f);
            fx.Sprite(shape, p, h * size * (0.8f + 0.5f * Fx.Rand(seed, i, 440)), Fx.Fade(Fx.Mix(a, b, Fx.Rand(seed, i, 441)), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, ang);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Hone Claws: three dark claws rise beside the user and a glint is whetted down each edge, a purple shine at the eyes.</summary>
    private static void HoneClawsY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var d = ColorsOf(PokemonType.Dark);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(d.Main, env * 0.4f), FxBlend.Alpha);
        for (int i = 0; i < 3; i++)
        {
            var p = c.FromFeet + new Vector3((i - 1) * h * 0.42f, h * (0.55f + 0.06f * (i == 1 ? 1 : 0)), 0.12f);
            fx.Sprite(FxShape.Claw, p, h * 0.62f, Fx.Fade(Rgb(214, 168, 255), env), FxBlend.Alpha, (1 - i) * 0.22f);
            fx.Sprite(FxShape.Claw, p, h * 0.66f, Fx.Fade(d.Light, env * 0.3f), rotation: (1 - i) * 0.22f);
            float k = (c.Age * 1.7f - 0.18f * i) % 1f;
            fx.Sprite(FxShape.Spark, p + new Vector3(0f, h * 0.3f * (1f - 2f * k), 0.1f), h * 0.14f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
        }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.05f, 0.95f, d.Main, d.Light, 7);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.3f, h, 0.4f, Rgb(255, 210, 255), 0.4f);
    }

    /// <summary>Wide Guard: a broad row of stone-coloured panels stands across the user's side, rocks at its foot, a sheen sweeping it.</summary>
    private static void WideGuardY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var r = ColorsOf(PokemonType.Rock);
        var side = new Vector3(h * 1.45f, 0f, 0f);
        L1Barrier(fx, c.Age, c.FromFeet - side, c.FromFeet + side, h, 5, Rgb(176, 140, 90), r.Light, 0.62f);
        float env = Fx.Envelope(c.Age, 0.05f, 0.2f, 0.85f, 1.1f);
        for (int i = -2; i <= 2; i++)
            fx.Sprite(FxShape.Rock, c.FromFeet + new Vector3(i * h * 0.72f, h * 0.1f, 0.2f), h * 0.2f, Fx.Fade(r.Dark, env), FxBlend.Alpha, i * 0.7f);
    }

    /// <summary>Guard Split: two blue shields, one from each Pokémon, slide together, join in a flash and part again in each other's colour.</summary>
    private static void GuardSplitY11(in FxCtx c, FxColors col, FxList fx) =>
        L1Trade(c, fx, FxShape.Hex, Rgb(96, 170, 255), Rgb(120, 210, 230), Rgb(220, 240, 255));

    /// <summary>Power Split: two red fists of force, one from each Pokémon, slide together, join in a flash and part again swapped.</summary>
    private static void PowerSplitY11(in FxCtx c, FxColors col, FxList fx) =>
        L1Trade(c, fx, FxShape.Fist, Rgb(255, 110, 70), Rgb(255, 170, 90), Rgb(255, 230, 170));

    /// <summary>Wonder Room: a violet-teal ring on the ground between them, two arrows (one blue, one violet) crossing and trading places above it.</summary>
    private static void WonderRoomY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 2.3f * grow, Fx.Fade(Rgb(150, 110, 255), env * 0.8f), FxBlend.Alpha);
        fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 1.7f * grow, Fx.Fade(Rgb(90, 220, 220), env * 0.6f), FxBlend.Alpha, c.Age * 0.8f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 0.8f;
            fx.Sprite(FxShape.Hex, mid + new Vector3(MathF.Cos(a) * h * 1.8f, h * 0.15f, MathF.Sin(a) * h * 0.8f), h * 0.3f, Fx.Fade(Rgb(190, 160, 255), env * 0.6f), FxBlend.Alpha, a);
        }
        // Two arrows trading places across the middle
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.1f) / 0.6f, 0f, 1f));
        var up = mid + Vector3.UnitY * h * 0.8f;
        fx.Sprite(FxShape.Arrow, up + new Vector3(h * 0.5f * (1f - 2f * k), h * 0.2f, 0.2f), h * 0.42f, Fx.Fade(Rgb(100, 170, 255), env), FxBlend.Alpha, MathF.PI / 2f);
        fx.Sprite(FxShape.Arrow, up + new Vector3(-h * 0.5f * (1f - 2f * k), -h * 0.2f, 0.2f), h * 0.42f, Fx.Fade(Rgb(190, 130, 255), env), FxBlend.Alpha, -MathF.PI / 2f);
        fx.Sprite(FxShape.Glow, up, h * 1.4f, Fx.Fade(Rgb(170, 140, 255), env * 0.25f), FxBlend.Alpha);
    }

    /// <summary>Psyshock: a spray of pink orbs hurled across, each breaking on the target into sharp crystal shards that fly off the body.</summary>
    private static void PsyshockY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, I, 0.22f, 7, FxShape.Orb, col.Main, col.Light, 0.16f, arc: 0.05f, spread: 0.45f, spin: 6f);
        Fx.Stream(fx, c, 0f, I, 0.22f, 7, FxShape.Glow, col.Light, White, 0.2f, arc: 0.05f, spread: 0.45f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 11, FxShape.Shard, col.Light, col.Main, 0.14f, 1.2f, 1.2f, FxBlend.Alpha, spin: 6f, salt: 230);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.1f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.2f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Venoshock: a heavy pour of violet venom falls on the target from above and lies in glistening pools, bubbles rising.</summary>
    private static void VenoshockY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.02f, I + 0.12f, 0.24f, 16, FxShape.Drop, col.Main, Rgb(220, 120, 255), 0.3f, FxBlend.Alpha, 1.5f);
        Haze(fx, c.Age, c.Seed, c.To, h, 0.1f, 0.85f, 4, Rgb(150, 70, 200), 0.5f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 0.7f, 9, FxShape.Bubble, col.Light, White, 0.1f, 0.5f, -0.6f, FxBlend.Alpha, salt: 231);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.0f, col.Main);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Autotomize: grey flakes of metal peel off the user and drift up, a silver sheen races over it and light arrows rise.</summary>
    private static void AutotomizeY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var steel = ColorsOf(PokemonType.Steel);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.08f, 0.85f, 12, FxShape.Shard, Rgb(170, 180, 200), steel.Light, 0.12f, 0.9f, -1.4f, FxBlend.Alpha, spin: 5f, salt: 232);
        float env = Fx.Envelope(c.Age, 0.05f, 0.15f, 0.8f, 1.05f);
        float k = (c.Age - 0.1f) / 0.5f;
        if (k > 0f && k < 1f)
            fx.Streak(FxShape.Streak, c.FromFeet + new Vector3(h * (-0.6f + 1.2f * k), h * 0.55f, 0.15f), new Vector3(0.4f, 1f, 0f), h * 0.12f, 5f, Fx.Fade(White, MathF.Sin(k * MathF.PI)));
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.1f + i / 3f) % 1f;
            fx.Sprite(FxShape.Arrow, c.FromFeet + new Vector3((i - 1) * h * 0.4f, h * (0.2f + 0.9f * u), 0.2f), h * 0.2f, Fx.Fade(steel.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(steel.Light, env * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Rage Powder: a cloud of red-orange spore dust puffs out of the user, spores whirling round it and an anger mark over its head.</summary>
    private static void RagePowderY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var red = Rgb(255, 96, 60);
        Haze(fx, c.Age, c.Seed, c.From, h, 0.02f, 0.95f, 7, Rgb(255, 150, 90), 0.7f);
        Swarm(fx, c.Age, c.Seed, c.From, h, 0.05f, 1.0f, 11, FxShape.Spore, red, Rgb(255, 230, 160), 0.09f, 0.75f);
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.05f, 0.6f, 8, FxShape.Spore, Rgb(255, 170, 90), red, 0.08f, 1.0f, 0.2f, salt: 233);
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.35f, h * 1.1f, 0.2f), h, 0.2f, 0.95f);
    }

    /// <summary>Telekinesis: pink rings rise from under the foe and lift it, light motes drifting up in its wake, a thread of force from the user.</summary>
    private static void TelekinesisY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.1f + i / 3f) % 1f;
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * h * (0.05f + 0.3f * u), Vector3.UnitY, h * (1.0f - 0.3f * u), Fx.Fade(i == 1 ? col.Light : col.Main, env * (1f - u)), depth: true);
        }
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 0.9f + Fx.Rand(c.Seed, i, 442)) % 1f;
            fx.Sprite(FxShape.Wisp, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 443) * h * 0.5f, h * (0.1f + 1.2f * u), 0.1f), h * 0.12f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
        SineRibbon(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.To + Vector3.UnitY * h * 0.2f, 0.04f, 0.6f, 0f, 0.05f, 2f, h * 0.12f, col.Main, White);
        fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(col.Main, env * 0.25f), FxBlend.Alpha);
    }

    /// <summary>Magic Room: a magenta ring on the ground and small round items floating up inside it, each in turn crossed out and winking away.</summary>
    private static void MagicRoomY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        var mag = Rgb(240, 90, 200);
        fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 2.3f * grow, Fx.Fade(mag, env * 0.85f), FxBlend.Alpha);
        fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 2.1f * grow, Fx.Fade(Rgb(255, 190, 240), env * 0.5f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.4f;
            var p = mid + new Vector3(MathF.Cos(a) * h * 1.2f, h * (0.4f + 0.25f * MathF.Sin(c.Age * 3f + i)), MathF.Sin(a) * h * 0.7f);
            float gone = (c.Age - 0.3f - i * 0.1f) / 0.2f;
            fx.Sprite(FxShape.Orb, p, h * 0.2f * (1f - Math.Clamp(gone, 0f, 1f)), Fx.Fade(Rgb(255, 230, 250), env), FxBlend.Alpha);
            if (gone > 0f && gone < 1f)
            {
                fx.Sprite(FxShape.Cross, p, h * 0.3f, Fx.Fade(mag, 1f - gone), FxBlend.Alpha, MathF.PI / 4f);
                fx.Sprite(FxShape.Spark, p, h * 0.25f * gone, Fx.Fade(White, 1f - gone));
            }
        }
    }

    /// <summary>Smack Down: a great rock is dropped on the target from above and slams it to the ground, which cracks and throws up dust.</summary>
    private static void SmackDownY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Rock, 1.2f, 0f, I, 0.85f, Rgb(176, 150, 108), 0.6f);
        DropOn(c, fx, FxShape.Rock, 1.2f, 0f, I, 0.6f, Rgb(120, 98, 68), 0.2f);
        float env = Fx.Envelope(c.Age, 0.1f, 0.2f, I - 0.03f, I + 0.05f);
        if (env > 0f) fx.Sprite(FxShape.Arrow, c.To + new Vector3(h * 0.6f, h * 0.9f, 0.2f), h * 0.24f, Fx.Fade(col.Light, env), FxBlend.Alpha, MathF.PI);
        if (c.Cue.Landed)
        {
            float k = (c.Age - I) / 0.5f;
            if (k > 0f && k < 1f) fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 1.5f * Fx.EaseOut(k * 3f), Fx.Fade(Rgb(90, 70, 50), 1f - k), FxBlend.Alpha, c.Seed % 7 * 0.4f);
            Dust(c, fx, c.ToFeet, h, I, 8);
            Jolt(fx, c.Age, I, 0.45f, 0.22f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Storm Throw: an orange whirl seizes the target and spins it round, then a fist slams it into the ground, a shower of stars (it always crits).</summary>
    private static void StormThrowY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float spin = Fx.Envelope(c.Age, 0.04f, 0.1f, I - 0.08f, I);
        if (spin > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 18f;
                fx.Sprite(FxShape.Swirl, c.To + new Vector3(MathF.Cos(a) * h * 0.7f, MathF.Sin(a) * h * 0.4f, 0.1f), h * 0.3f, Fx.Fade(col.Main, spin), FxBlend.Alpha, a);
            }
        Strike(c, fx, FxShape.Fist, new Vector3(-0.9f, 0.9f, 0.2f), I, 0.42f, Rgb(255, 224, 196), -0.6f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Star, Rgb(255, 232, 130), col.Light, 0.1f, 1.0f, 1f, FxBlend.Alpha, spin: 5f, salt: 234);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.2f, col.Light);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Flame Burst: a fireball bursts on the target and a ring of flame and sparks flies out sideways to the Pokémon beside it.</summary>
    private static void FlameBurstY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Orb(fx, c, 0.02f, I, 0.34f, col.Main, col.Light, FxShape.Flame);
        if (c.Cue.Landed)
            for (int s = -1; s <= 1; s += 2)
            {
                float k = (c.Age - I) / 0.5f;
                if (k < 0f || k > 1f) continue;
                for (int j = 1; j <= 3; j++)
                    fx.Sprite(FxShape.Flame, c.To + new Vector3(s * h * (0.3f + 0.85f * j * Fx.EaseOut(k)), h * (0.1f + 0.08f * j) * (1f - k), 0.1f), h * (0.3f - 0.05f * j), Fx.Fade(j % 2 == 0 ? col.Light : col.Main, 1f - k), rotation: -s * MathF.PI / 2f);
                Fx.Burst(fx, c.Seed, c.Age, c.To + new Vector3(s * h * 1.3f, 0f, 0f), h, I + 0.1f, 0.4f, 4, FxShape.Ember, col.Light, col.Main, 0.08f, 0.7f, -0.2f, salt: 235 + s);
            }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Sludge Wave: a thick wall of purple sludge rolls across and breaks over the target, bubbling.</summary>
    private static void SludgeWaveY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.WaveWall(fx, c, 0.02f, I + 0.2f, Rgb(140, 60, 180), Rgb(224, 160, 250));
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 10, FxShape.Bubble, col.Light, White, 0.1f, 0.7f, -0.4f, FxBlend.Alpha, salt: 236);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.5f, h, I + 0.05f, 6, col.Main);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Quiver Dance: butterfly-bright petals in green, rose and sky blue circle the user in two rings, light rising from its feet.</summary>
    private static void QuiverDanceY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var mid = c.From;
        Orbit(fx, c.Age, mid, h, 0.02f, 1.0f, 6, FxShape.Petal, Rgb(150, 220, 90), 0.2f, 0.7f, 5f, 0.45f);
        Orbit(fx, c.Age, mid, h, 0.1f, 1.05f, 6, FxShape.Petal, Rgb(255, 150, 190), 0.2f, 0.85f, -4f, 0.5f);
        Orbit(fx, c.Age, mid, h, 0.2f, 1.1f, 4, FxShape.Petal, Rgb(130, 200, 255), 0.18f, 0.55f, 6f, 0.4f);
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.05f, 1.0f, col.Main, col.Light, 8);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.3f, h, 0.45f, White, 0.4f);
    }

    /// <summary>Heavy Slam: a vast slab of steel drops on the target from the top of the picture and flattens it, the ground ringing with the blow.</summary>
    private static void HeavySlamY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var steel = ColorsOf(PokemonType.Steel);
        float sk = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        float slab = Fx.Envelope(c.Age, 0.03f, 0.08f, I + 0.1f, I + 0.4f);
        if (slab > 0f)
        {
            var at = c.To + new Vector3(0f, h * (1.5f - 1.0f * sk * sk), 0.2f);
            float squash = c.Age > I ? 1f + 0.15f * Math.Min(1f, (c.Age - I) / 0.1f) : 1f;
            fx.Sprite(FxShape.Hex, at, h * 1.25f * squash, Fx.Fade(Rgb(140, 152, 176), slab), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, at, h * 0.9f, Fx.Fade(Rgb(96, 106, 130), slab), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, at, h * 1.32f, Fx.Fade(steel.Light, slab * 0.5f));
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.8f, steel.Light);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.08f, 0.5f, h * 1.3f, steel.Main);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.45f, 8, FxShape.Spark, White, steel.Light, 0.12f, 1.0f, salt: 237);
            Dust(c, fx, c.ToFeet, h, I, 6);
            Jolt(fx, c.Age, I, 0.7f, 0.28f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Synchronoise: rings pulse out of the user in time with rings on the target, a wavering thread of force tying them, then both beat together.</summary>
    private static void SynchronoiseY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float at = 0.02f + i * 0.11f;
            PulseRing(fx, c.Age, c.From, at, 0.3f, c.FromSize * 0.3f, c.FromSize * 1.2f, col.Main, col.Light);
            PulseRing(fx, c.Age, c.To, at, 0.3f, h * 0.3f, h * 1.2f, col.Main, col.Light);
        }
        SineRibbon(fx, c.Age, c.From, c.Aim, 0.05f, I + 0.05f, 0f, 0.05f, 3f, h * 0.05f, col.Main, col.Light);
        SineRibbon(fx, c.Age, c.From, c.Aim, 0.05f, I + 0.05f, MathF.PI, 0.05f, 3f, h * 0.05f, col.Light, White);
        if (c.Cue.Landed) PulseRing(fx, c.Age, c.To, I, 0.45f, h * 0.4f, h * 1.8f, Rgb(255, 190, 230), White);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Electro Ball: a crackling yellow ball with bolts leaping from it speeds across and bursts on the target in sparks.</summary>
    private static void ElectroBallY11(in FxCtx c, FxColors col, FxList fx)
    {
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        if (c.Age < I)
        {
            var p = Vector3.Lerp(c.From, c.Aim, k * k);
            float s = c.FromSize * (0.32f + 0.12f * k);
            fx.Sprite(FxShape.Glow, p, s * 2.4f, Fx.Fade(col.Main, 0.6f));
            fx.Sprite(FxShape.Orb, p, s, Fx.Fade(col.Main, 0.95f), FxBlend.Alpha, c.Age * 9f);
            fx.Sprite(FxShape.Orb, p, s * 0.6f, Fx.Fade(White, 0.95f), rotation: -c.Age * 9f);
            for (int i = 0; i < 3; i++)
                Fx.Bolt(fx, c.Age, c.Seed + i * 5, p, p + Fx.Direction(c.Seed, 450 + i) * s * 1.1f, 0f, I, s * 0.12f, col.Main, col.Light, 0);
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.45f, 14, FxShape.Spark, col.Main, White, 0.1f, 1.1f, salt: 238);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Soak: a drenching downpour falls on the target, a puddle ringing out at its feet and the water running down its sides.</summary>
    private static void SoakY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Rain(fx, c.Age, c.Seed, c.To, h, 0.02f, 0.75f, 0.22f, 18, FxShape.Drop, col.Main, col.Light, 0.26f, FxBlend.Alpha, 1.5f);
        Tint(c, fx, 0.1f, 0.95f, col.Main, 0.4f);
        for (int i = 0; i < 3; i++) GroundRing(fx, c.Age, c.ToFeet, 0.2f + i * 0.17f, 0.45f, h * 1.1f, col.Light);
        Splash(c, fx, c.ToFeet + Vector3.UnitY * h * 0.15f, h, 0.3f, col.Main, col.Light, 6);
        Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.2f, h, 0.5f, 4, col.Light);
    }

    /// <summary>Flame Charge: the user wrapped in fire dashes at the target, a trail of flames left burning along its path, an ember spray at the blow.</summary>
    private static void FlameChargeY11(in FxCtx c, FxColors col, FxList fx)
    {
        float k = Math.Clamp((c.Age - 0.03f) / (I - 0.03f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.03f, I + 0.1f, I + 0.45f);
        if (fade > 0f)
        {
            for (int i = 0; i < 9; i++)
            {
                float u = Math.Max(0f, k - i * 0.075f);
                var p = Vector3.Lerp(c.From, c.Aim, u * u) + Vector3.UnitY * c.FromSize * (0.1f - 0.1f * i / 9f);
                fx.Sprite(FxShape.Flame, p, c.FromSize * (0.55f - 0.04f * i), Fx.Fade(i < 3 ? col.Light : col.Main, fade * (1f - i / 10f)), rotation: -MathF.PI / 2f);
            }
            SpeedLines(c, fx, 0.03f, I, 5, 0.5f);
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 10, FxShape.Ember, col.Light, col.Main, 0.08f, 1.1f, -0.5f, salt: 239);
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Coil: a violet ribbon winds round the user in a tightening spiral from its feet to its head, a gleam running up it.</summary>
    private static void CoilY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float head = Math.Clamp(c.Age / 0.55f, 0f, 1f);
        const int n = 26;
        var pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float t = head * i / (n - 1);
            float a = t * MathF.Tau * 2.3f;
            float r = h * 0.55f * (1f - 0.35f * t);
            pts[i] = c.FromFeet + new Vector3(MathF.Cos(a) * r, h * (0.08f + 0.85f * t), MathF.Sin(a) * r * 0.5f + 0.1f);
        }
        fx.Ribbon(pts, h * 0.1f, Fx.Fade(col.Main, 0.9f * env), FxBlend.Alpha);
        fx.Ribbon(pts, h * 0.04f, Fx.Fade(col.Light, env));
        fx.Sprite(FxShape.Spark, pts[n - 1], h * 0.2f, Fx.Fade(White, env));
        fx.Sprite(FxShape.Glow, c.From, h * 0.9f, Fx.Fade(col.Main, env * 0.25f), FxBlend.Alpha);
        if (c.Age > 0.5f) PulseRing(fx, c.Age, c.From, 0.5f, 0.4f, h * 0.4f, h * 1.1f, col.Light, White);
    }

    /// <summary>Low Sweep: a leg whips round low across the target's feet in a flat arc, dust kicked up along it.</summary>
    private static void LowSweepY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = (c.Age - (I - 0.2f)) / 0.24f;
        if (k > 0f && k < 1.3f && !c.Cue.Missed)
        {
            float u = Math.Min(1f, k);
            var p = c.ToFeet + new Vector3(h * (-1.0f + 2.0f * Fx.EaseOut(u)), h * 0.15f, 0.15f);
            float fade = k < 1f ? 1f : 1f - (k - 1f) / 0.3f;
            fx.Sprite(FxShape.Foot, p, h * 0.55f, Fx.Fade(Rgb(255, 224, 196), fade), FxBlend.Alpha, -0.35f);
            fx.Streak(FxShape.Streak, p - Vector3.UnitX * h * 0.5f, Vector3.UnitX, h * 0.12f, 6f, Fx.Fade(col.Light, fade * 0.8f));
            fx.Streak(FxShape.Streak, p - Vector3.UnitX * h * 0.5f + Vector3.UnitY * h * 0.12f, Vector3.UnitX, h * 0.06f, 7f, Fx.Fade(White, fade));
        }
        if (c.Cue.Landed)
        {
            Dust(c, fx, c.ToFeet, h, I - 0.05f, 8);
            Jolt(fx, c.Age, I, 0.2f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Acid Spray: a stream of glistening acid-green globs spat across, hissing on the target, which sizzles and drips.</summary>
    private static void AcidSprayY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var acid = Rgb(150, 226, 50);
        var pale = Rgb(226, 255, 130);
        Fx.Stream(fx, c, 0f, I, 0.2f, 12, FxShape.Drop, acid, Rgb(90,170,30), 0.3f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, I, 0.2f, 6, FxShape.Bubble, pale, White, 0.18f, arc: 0.06f, spread: 0.3f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 7, FxShape.Smoke, Rgb(200, 240, 140, 180), Rgb(150, 200, 90, 160), 0.14f, 0.35f, -0.9f, FxBlend.Alpha, salt: 240);
            Drips(fx, c.Age, c.Seed, c.To + Vector3.UnitY * h * 0.5f, h, I + 0.05f, 6, acid);
            PulseRing(fx, c.Age, c.To, I, 0.35f, h * 0.3f, h * 1.0f, acid, pale);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Foul Play: a shadow double of the foe steps out of its body and a dark fist swings back through it, the foe hitting itself.</summary>
    private static void FoulPlayY11(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var d = ColorsOf(PokemonType.Dark);
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, I - 0.02f, I + 0.18f);
        if (env > 0f)
        {
            float out_ = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / 0.2f, 0f, 1f));
            var p = c.To + new Vector3(-h * 0.9f * out_, 0f, 0.15f);
            fx.Sprite(FxShape.Wisp, p, h * 1.25f, Fx.Fade(Rgb(60, 36, 80), env * 0.85f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(d.Main, env * 0.4f), FxBlend.Alpha);
            for (int s = -1; s <= 1; s += 2) fx.Sprite(FxShape.Eye, p + new Vector3(s * h * 0.17f, h * 0.18f, 0.1f), h * 0.2f, Fx.Fade(Rgb(255, 120, 190), env), FxBlend.Alpha);
        }
        Strike(c, fx, FxShape.Fist, new Vector3(-1.3f, 0.2f, 0.2f), I, 0.45f, Rgb(120, 70, 150), 0.25f, 0.16f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 7, FxShape.Wisp, d.Light, d.Main, 0.14f, 0.8f, -0.4f, salt: 241);
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Simple Beam: a thin, plain, pale beam, and a bare ring with an equals sign hung over the foe's head as it takes the plain ability.</summary>
    private static void SimpleBeamY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Beam(fx, c, 0.04f, 0.5f, 0.05f, Rgb(214, 208, 190), White, 4);
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.08f, 0.85f, 1.05f);
        if (env > 0f)
        {
            var at = c.To + new Vector3(0f, h * 0.85f, 0.2f);
            fx.Sprite(FxShape.Ring, at, h * 0.42f, Fx.Fade(Rgb(236, 232, 214), env), FxBlend.Alpha, c.Age);
            fx.Streak(FxShape.Beam, at + Vector3.UnitY * h * 0.06f, Vector3.UnitX, h * 0.05f, 2.2f, Fx.Fade(Rgb(250, 248, 236), env), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, at - Vector3.UnitY * h * 0.06f, Vector3.UnitX, h * 0.05f, 2.2f, Fx.Fade(Rgb(250, 248, 236), env), FxBlend.Alpha);
        }
    }

    /// <summary>Entrainment: a line of notes in the user's tune flows across, and the foe beats along with it, a ring pulsing on each note.</summary>
    private static void EntrainmentY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.6f, 0.3f, 8, FxShape.Note, Rgb(255, 190, 90), Rgb(255, 120, 170), 0.3f, arc: 0.08f, spread: 0.2f, spin: 0f, blend: FxBlend.Alpha);
        foreach (float at in new[] { I, I + 0.17f, I + 0.34f })
            PulseRing(fx, c.Age, c.To, at, 0.3f, h * 0.3f, h * 1.0f, Rgb(255, 214, 160), Rgb(255, 245, 220));
        float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.9f, 1.1f);
        if (env > 0f)
            fx.Sprite(FxShape.Note, c.To + new Vector3(h * 0.35f, h * (0.85f + 0.06f * MathF.Sin(c.Age * 12f)), 0.2f), h * 0.22f, Fx.Fade(Rgb(255, 190, 170), env), FxBlend.Alpha);
    }

    /// <summary>After You: an open hand beckons from the user toward the target, chevrons pointing the way, and a soft light settles on the target to go first.</summary>
    private static void AfterYouY11(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var dir = c.To - c.From;
        var towards = dir.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(dir);
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.8f, 1.05f);
        float wave = MathF.Sin(c.Age * 13f) * 0.35f;
        fx.Sprite(FxShape.Palm, c.From + towards * c.FromSize * 0.5f + Vector3.UnitY * c.FromSize * 0.5f + new Vector3(0f, 0f, 0.2f), c.FromSize * 0.36f, Fx.Fade(Rgb(255, 236, 214), env), FxBlend.Alpha, wave);
        for (int i = 0; i < 3; i++)
        {
            float u = (c.Age * 1.6f + i / 3f) % 1f;
            var p = Vector3.Lerp(c.From + towards * c.FromSize * 0.7f, c.To - towards * h * 0.4f, u) + Vector3.UnitY * c.FromSize * 0.6f;
            fx.Sprite(FxShape.Arrow, p, h * 0.2f, Fx.Fade(Rgb(190, 240, 200), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, ScreenAngle(dir) - MathF.PI / 2f);
        }
        Tint(c, fx, I - 0.05f, 0.95f, Rgb(200, 255, 210), 0.35f);
        Fx.Glint(fx, c.Age, c.To + Vector3.UnitY * h * 0.3f, h, I, White, 0.45f);
    }

    // ------------------------------------------------------------------ more recipes, in Later1b.cs
}
