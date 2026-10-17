using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 · Y7, its first part: the moves the story shows from Route 204 to Team Galactic's Eterna Building, in
// MoveSightings' order, each a look of its own. The beats are the original's, read from each move's animation script
// (what comes first, when it lands, who shakes); the shapes, colours and timings are our own. Every recipe lands at
// ImpactTime (FxList.Land, which Impact sets), is over by EffectTime and stays under MaxQuads.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Platinum2() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["String Shot"] = StringShot,
        ["Rock Tomb"] = RockTomb,
        ["Water Pulse"] = WaterPulse,
        ["Bullet Seed"] = BulletSeed,
        ["Charge"] = ChargeY7,
        ["Charm"] = CharmY7,
        ["Leech Seed"] = LeechSeed,
        ["Poison Sting"] = PoisonSting,
        ["Stun Spore"] = StunSpore,
        ["Wing Attack"] = WingAttack,
        ["Pluck"] = (in FxCtx c, FxColors col, FxList fx) => { Jabs(c, fx, 3, 0.08f, FxShape.Beak, Rgb(255, 214, 120)); Impact(c, col, fx, 0.85f); },
        ["Baton Pass"] = BatonPass,
        ["Foresight"] = (in FxCtx c, FxColors col, FxList fx) => Reticle(c, fx, Rgb(255, 244, 170), stepped: false),
        ["Karate Chop"] = KarateChop,
        ["Mega Drain"] = (in FxCtx c, FxColors col, FxList fx) => Siphon(c, col, fx, 14, 0),
        ["Mud Bomb"] = MudBomb,
        ["Mud-Slap"] = MudSlap,
        ["Peck"] = (in FxCtx c, FxColors col, FxList fx) => { Jabs(c, fx, 2, 0.1f, FxShape.Beak, Rgb(255, 214, 120)); Impact(c, col, fx, 0.8f); },
        ["Rage"] = Rage,
        ["Sand Attack"] = SandAttack,
        ["Sonic Boom"] = SonicBoom,
        ["Hypnosis"] = HypnosisY7,
        ["Thunderbolt"] = ThunderboltY7,
        ["Bite"] = (in FxCtx c, FxColors col, FxList fx) => Fangs(c, col, fx, hard: false),
        ["Feint Attack"] = FeintAttack,
        ["Toxic"] = ToxicY7,
        ["Agility"] = AgilityY7,
        ["Aqua Jet"] = AquaJet,
        ["Body Slam"] = BodySlam,
        ["Confuse Ray"] = ConfuseRay,
        ["Crunch"] = (in FxCtx c, FxColors col, FxList fx) => Fangs(c, col, fx, hard: true),
        ["Fire Punch"] = FirePunch,
        ["Fire Spin"] = FireSpinY7,
        ["Hidden Power"] = HiddenPower,
        ["Lock-On"] = (in FxCtx c, FxColors col, FxList fx) => Reticle(c, fx, Rgb(255, 90, 80), stepped: true),
        ["Magnet Bomb"] = MagnetBomb,
        ["Rain Dance"] = RainDanceY7,
        ["Spark"] = SparkY7,
        ["Swift"] = SwiftY7,
        ["Thunder Wave"] = ThunderWaveY7,
        ["Confusion"] = ConfusionY7,
        ["Gust"] = GustY7,
        ["Horn Attack"] = (in FxCtx c, FxColors col, FxList fx) => { Jabs(c, fx, 2, 0.14f, FxShape.Horn, Rgb(236, 226, 200)); Impact(c, col, fx); },
        ["Rollout"] = Rollout,
        ["Sleep Talk"] = SleepTalk,
        ["Curse"] = CurseY7,
        ["Endure"] = Endure,
        ["Frustration"] = Frustration,
        ["Mean Look"] = MeanLook,
        ["Meditate"] = Meditate,
        ["Protect"] = ProtectY7,
        ["Spite"] = Spite,
        ["Night Shade"] = NightShade,
        ["Substitute"] = Substitute,
        ["Cut"] = Cut,
        ["Thief"] = Thief,
        ["Grass Knot"] = GrassKnot,
        ["Magical Leaf"] = MagicalLeafY7,
        ["Razor Leaf"] = RazorLeafY7,
        ["Reflect"] = ReflectY7,
        ["Safeguard"] = Safeguard,
        ["Smokescreen"] = Smokescreen,
        ["Sunny Day"] = SunnyDayY7,
        ["Recycle"] = Recycle,
        ["Taunt"] = Taunt,
        ["Giga Drain"] = (in FxCtx c, FxColors col, FxList fx) => Siphon(c, col, fx, 20, 4),
        ["Night Slash"] = NightSlash
    };

    // ------------------------------------------------------------------ shared by the batch

    private static readonly Color Mud = Rgb(132, 96, 60), MudDark = Rgb(92, 66, 42);
    private static readonly Color AngerRed = Rgb(232, 44, 44);

    /// <summary>The way from the user to the target as the picture turns a sprite: the angle a shape pointing right is turned by.</summary>
    private static float ScreenAngle(Vector3 dir) => MathF.Atan2(-dir.Z * 0.3f + dir.Y, dir.X);

    /// <summary>Where something lobbed along an arc <paramref name="arc"/> × the way high is, <paramref name="k"/> of the way.</summary>
    private static Vector3 Lob(Vector3 from, Vector3 to, float k, float arc) =>
        Vector3.Lerp(from, to, k) + Vector3.UnitY * Vector3.Distance(from, to) * arc * 4f * k * (1f - k);

    /// <summary>
    /// A shape pointed at the target jabbing in <paramref name="count"/> times, the last at the impact: a beak, a horn.
    /// Each comes in from the user's side, strikes with a small flash and draws back.
    /// </summary>
    private static void Jabs(in FxCtx c, FxList fx, int count, float gap, FxShape shape, Color color)
    {
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var dir = c.To - c.From;
        var back = -Vector3.Normalize(dir);
        // The beak and the horn point right; the horn's tip leans up a quarter turn's half
        float turn = ScreenAngle(dir) - (shape == FxShape.Horn ? MathF.PI / 4f : 0f);
        for (int i = 0; i < count; i++)
        {
            float at = I - (count - 1 - i) * gap;
            float k = (c.Age - (at - 0.07f)) / 0.07f;
            float after = (c.Age - at) / 0.14f;
            if (k < 0f || after > 1f) continue;
            float reach = k < 1f ? Fx.EaseOut(k) : 1f - 0.4f * after;
            var p = c.To + back * h * (0.75f - 0.45f * reach) + Vector3.UnitY * h * 0.05f;
            fx.Sprite(shape, p, h * 0.42f, Fx.Fade(color, Math.Min(1f, k * 4f) * (1f - Math.Max(0f, after))), FxBlend.Alpha, turn);
            if (i < count - 1) Fx.Contact(fx, c.Age, c.Seed + i, c.To + back * h * 0.2f, h, at, 0.16f, Rgb(255, 240, 200), White);
        }
    }

    /// <summary>An anger mark popping at <paramref name="at"/>: a red cross on a white rim, beating.</summary>
    private static void Anger(FxList fx, float age, Vector3 at, float h, float t0, float t1)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.06f, t1 - 0.12f, t1);
        if (env <= 0f) return;
        float beat = 1f + 0.18f * MathF.Max(0f, MathF.Sin((age - t0) * 22f));
        fx.Sprite(FxShape.Cross, at, h * 0.2f * beat, Fx.Fade(White, env), FxBlend.Alpha, 0.25f);
        fx.Sprite(FxShape.Cross, at, h * 0.17f * beat, Fx.Fade(AngerRed, env), FxBlend.Alpha, 0.25f);
    }

    /// <summary>
    /// A ring closing on the foe and a glint in it: Foresight's, or Lock-On's, which tightens in steps and holds red.
    /// </summary>
    private static void Reticle(in FxCtx c, FxList fx, Color color, bool stepped)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float close = stepped ? (c.Age < 0.12f ? 0f : c.Age < 0.24f ? 0.45f : c.Age < I ? 0.75f : 1f) : Fx.EaseOut(c.Age / I);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.9f, 1.15f);
        float size = h * (1.5f - 0.75f * close);
        fx.Sprite(FxShape.Target, c.To, size, Fx.Fade(color, env), FxBlend.Alpha, stepped ? 0f : (1f - close) * 0.8f);
        fx.Sprite(FxShape.Target, c.To, size * 1.06f, Fx.Fade(color, env * 0.35f), rotation: stepped ? 0f : (1f - close) * 0.8f);
        Fx.Glint(fx, c.Age, c.To + new Vector3(h * 0.08f, h * 0.1f, 0f), h, I, White, 0.4f);
        if (stepped)
            foreach (float t in new[] { 0.12f, 0.24f, I })
                Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, t, 0.2f, size * 0.6f, Fx.Fade(color, 0.7f));
    }

    /// <summary>
    /// Life drawn out of the target in a helix of green motes back to the user (and leaves among them), which glows
    /// pale as they reach it: Mega Drain's and Giga Drain's.
    /// </summary>
    private static void Siphon(in FxCtx c, FxColors col, FxList fx, int count, int leaves)
    {
        Impact(c, col, fx, 0.8f);
        if (c.Cue.Missed || c.Cue.Blocked) return;
        var (across, up) = Fx.Frame(c.From - c.To);
        for (int i = 0; i < count + leaves; i++)
        {
            float leave = I + 0.02f + i * 0.4f / (count + leaves);
            float t = (c.Age - leave) / 0.42f;
            if (t < 0f || t > 1f) continue;
            float swirl = t * MathF.Tau * 1.5f + i * 0.9f;
            float wide = c.ScaleAt(1f - t) * 0.32f * MathF.Sin(t * MathF.PI);
            var p = Vector3.Lerp(c.To, c.From, Fx.EaseOut(t)) + (across * MathF.Cos(swirl) + up * MathF.Sin(swirl)) * wide;
            float fade = Math.Min(1f, (1f - t) * 5f) * Math.Min(1f, t * 6f + 0.2f);
            if (i >= count) fx.Sprite(FxShape.Leaf, p, c.ScaleAt(1f - t) * 0.1f, Fx.Fade(Rgb(110, 210, 90), fade), FxBlend.Alpha, swirl);
            else fx.Sprite(FxShape.Glow, p, c.ScaleAt(1f - t) * 0.08f, Fx.Fade(Fx.Mix(Rgb(140, 255, 130), Rgb(230, 255, 200), t), fade));
        }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.75f, Fx.Fade(Rgb(220, 255, 220), 0.4f * Fx.Envelope(c.Age, 0.7f, 0.85f, 0.95f, 1.2f)));
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.15f, c.FromSize, 0.85f, Rgb(220, 255, 210), 0.45f);
    }

    /// <summary>Fangs closing on the target; shadows thrown off it for Bite, shards broken off it for Crunch.</summary>
    private static void Fangs(in FxCtx c, FxColors col, FxList fx, bool hard)
    {
        Chomp(c, col, fx);
        if (!c.Cue.Landed) return;
        float h = c.ToSize;
        if (hard)
        {
            float k = (c.Age - I) / 0.45f;
            if (k >= 0f && k <= 1f) fx.Sprite(FxShape.Crack, c.To, h * 0.4f, Fx.Fade(Rgb(60, 40, 60), 1f - k * k), FxBlend.Alpha, 0.5f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Shard, Rgb(236, 230, 240), Rgb(160, 150, 170), 0.08f, 1.0f, 2.2f, FxBlend.Alpha, spin: 6f, salt: 31);
            fx.Shake = Math.Max(fx.Shake, 0.25f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.1f, I + 0.25f));
        }
        else Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 5, FxShape.Smoke, Rgb(70, 50, 80, 210), Rgb(40, 28, 46, 210), 0.14f, 0.55f, -0.2f, FxBlend.Alpha, salt: 32);
    }

    /// <summary>Bands of something wound round the target from the impact on, tightening and letting go (String Shot's threads, Grass Knot's grass).</summary>
    private static void Wound(in FxCtx c, FxList fx, int count, float low, float high, Color color, Color light)
    {
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float k = (c.Age - I + 0.05f) / 0.3f;
        float fade = Fx.Envelope(c.Age, I - 0.05f, I + 0.05f, 0.85f, 1.1f);
        if (fade <= 0f) return;
        float squeeze = Fx.EaseOut(Math.Clamp(k, 0f, 1f));
        for (int i = 0; i < count; i++)
        {
            float lean = 0.18f * (i % 2 == 0 ? 1f : -1f);
            var normal = Vector3.Normalize(new Vector3(MathF.Sin(lean), MathF.Cos(lean), 0.55f));
            var at = c.ToFeet + Vector3.UnitY * h * (low + (high - low) * (count == 1 ? 0f : i / (float)(count - 1)));
            float size = h * (0.85f - 0.3f * squeeze) / 0.75f;
            float front = TurnToward(normal, Vector3.UnitZ);
            fx.Flat(FxShape.Wave, at, normal, size, Fx.Fade(color, fade), FxBlend.Alpha, front, depth: false);
            fx.Flat(FxShape.Wave, at, normal, size * 1.04f, Fx.Fade(light, fade * 0.4f), rotation: front, depth: false);
            fx.Flat(FxShape.Wave, at, normal, size, Fx.Fade(color, fade * 0.5f), FxBlend.Alpha, front + MathF.Tau / 3f, depth: true);
            fx.Flat(FxShape.Wave, at, normal, size, Fx.Fade(color, fade * 0.5f), FxBlend.Alpha, front - MathF.Tau / 3f, depth: true);
        }
    }

    /// <summary>A hexagonal panel of a shield at <paramref name="at"/>, springing up from small at <paramref name="t0"/>.</summary>
    private static void Panel(FxList fx, float age, Vector3 at, float size, float t0, Color color)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.12f, 0.85f, 1.15f);
        if (env <= 0f) return;
        float grow = 0.3f + 0.7f * Fx.EaseOut((age - t0) / 0.18f);
        fx.Sprite(FxShape.Hex, at, size * grow, Fx.Fade(color, env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Hex, at, size * grow * 1.04f, Fx.Fade(color, env * 0.2f));
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>String Shot: three white threads shot across and wound round the target in bands.</summary>
    private static void StringShot(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var (across, up) = Fx.Frame(c.Aim - c.From);
        for (int k = 0; k < 3; k++)
        {
            float t0 = k * 0.05f;
            float reach = Math.Clamp((c.Age - t0) / (I - 0.04f - t0), 0f, 1f);
            float fade = Fx.Envelope(c.Age, t0, t0 + 0.04f, 0.55f, 0.75f);
            if (reach <= 0f || fade <= 0f) continue;
            var off = (across * (k - 1) * 0.12f + up * Fx.Signed(c.Seed, k, 41) * 0.08f) * h;
            var start = c.From + off * 0.3f;
            var end = Vector3.Lerp(c.From, c.Aim, reach) + off;
            var mid = Vector3.Lerp(start, end, 0.5f) - Vector3.UnitY * Vector3.Distance(start, end) * 0.04f;
            fx.Ribbon(new[] { start, mid, end }, h * 0.03f, Fx.Fade(Rgb(250, 250, 244), 0.95f * fade), FxBlend.Alpha);
        }
        Wound(c, fx, 3, 0.3f, 0.7f, Rgb(246, 246, 240), White);
    }

    /// <summary>Rock Tomb: four rocks dropped round the target one after another, each with dust and a jolt, then standing a moment.</summary>
    private static void RockTomb(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        if (!c.Cue.Missed)
            for (int i = 0; i < 4; i++)
            {
                float land = I - (3 - i) * 0.07f;
                float a = i * MathF.Tau / 4f + 0.6f;
                var ground = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * 0.2f, MathF.Sin(a) * h * 0.35f);
                float k = (c.Age - (land - 0.18f)) / 0.18f;
                if (k < 0f) continue;
                float fade = Math.Clamp((1.15f - c.Age) / 0.25f, 0f, 1f);
                var p = k < 1f ? ground + Vector3.UnitY * h * 2.2f * (1f - k * k) : ground;
                fx.Sprite(FxShape.Rock, p, h * 0.3f, Fx.Fade(col.Light, fade), FxBlend.Alpha, i * 1.3f, depth: MathF.Sin(a) < 0f);
                Fx.Burst(fx, c.Seed + i, c.Age, ground - Vector3.UnitY * h * 0.12f, h * 0.7f, land, 0.45f, 4, FxShape.Smoke, Rgb(222, 206, 176, 210), Rgb(176, 160, 132, 200),
                    0.14f, 0.45f, -0.2f, FxBlend.Alpha, salt: 33);
                fx.Shake = Math.Max(fx.Shake, 0.22f * Fx.Envelope(c.Age, land, land + 0.02f, land + 0.05f, land + 0.14f));
            }
        Impact(c, col, fx);
    }

    /// <summary>Water Pulse: a ball of water rolling across in pulsing rings and breaking over the target in rings.</summary>
    private static void WaterPulse(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Orb(fx, c, 0.02f, I, 0.2f, col.Main, col.Light, FxShape.Drop);
        for (int j = 0; j < 4; j++)
        {
            float born = 0.04f + j * 0.07f;
            float k = (c.Age - born) / 0.28f;
            if (k < 0f || k > 1f) continue;
            float u = (born - 0.02f) / (I - 0.02f);
            var at = Vector3.Lerp(c.From, c.Aim, u * u * (3f - 2f * u));
            fx.Sprite(FxShape.Ring, at, c.ScaleAt(u) * (0.25f + 0.5f * Fx.EaseOut(k)), Fx.Fade(col.Light, (1f - k) * 0.8f));
        }
        if (c.Cue.Landed)
            for (int j = 0; j < 3; j++)
                Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + j * 0.08f, 0.4f, c.ToSize * (0.9f + 0.25f * j), Fx.Fade(j % 2 == 0 ? col.Main : col.Light, 0.9f));
        Impact(c, col, fx);
    }

    /// <summary>Bullet Seed: a rapid string of seeds, a pop where each lands.</summary>
    private static void BulletSeed(in FxCtx c, FxColors col, FxList fx)
    {
        const int count = 6;
        const float flight = 0.14f;
        Fx.Stream(fx, c, 0f, I + 0.01f, flight, count, FxShape.Seed, Rgb(214, 176, 96), Rgb(170, 132, 70), 0.09f, spread: 0.12f, spin: 0f, blend: FxBlend.Alpha);
        if (!c.Cue.Missed)
            for (int i = 0; i < count - 1; i++)
            {
                float arrive = (I + 0.01f - flight) * i / (count - 1) + flight;
                var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 34), Fx.Signed(c.Seed, i, 35), 0f) * c.ToSize * 0.15f;
                Fx.Shockwave(fx, c.Age, at, Vector3.Zero, arrive, 0.16f, c.ToSize * 0.4f, Fx.Fade(col.Light, 0.9f));
            }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Charge: sparks drawn into the user and little bolts crackling round it as its glow swells.</summary>
    private static void ChargeY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.8f, 1.1f);
        if (env <= 0f) return;
        var middle = c.FromFeet + Vector3.UnitY * h * 0.5f;
        for (int i = 0; i < 12; i++)
        {
            float u = (c.Age * 1.7f + Fx.Rand(c.Seed, i, 36)) % 1f;
            float a = Fx.Rand(c.Seed, i, 37) * MathF.Tau;
            var dir = new Vector3(MathF.Cos(a), Fx.Signed(c.Seed, i, 38) * 0.5f, MathF.Sin(a) * 0.6f);
            fx.Sprite(FxShape.Spark, middle + dir * h * (1f - Fx.EaseOut(u)), h * 0.08f, Fx.Fade(Fx.Mix(col.Main, col.Light, u), env * MathF.Sin(u * MathF.PI)), rotation: a);
        }
        for (int k = 0; k < 3; k++)
        {
            float t0 = 0.12f + k * 0.22f;
            var a = middle + Fx.Direction(c.Seed, 40 + k) * h * 0.5f;
            var b = middle + Fx.Direction(c.Seed, 44 + k) * h * 0.5f;
            Fx.Bolt(fx, c.Age, c.Seed + k * 5, a, b, t0, t0 + 0.2f, h * 0.025f, col.Main, col.Light, 0);
        }
        float swell = Math.Clamp(c.Age / 0.9f, 0f, 1f);
        fx.Sprite(FxShape.Glow, middle, h * (0.2f + 0.3f * swell), Fx.Fade(col.Main, env * 0.55f));
        Fx.Shockwave(fx, c.Age, middle, Vector3.Zero, I, 0.3f, h * 0.9f, col.Light);
    }

    /// <summary>Charm: hearts hopping up round the user, then one big heart floating over to the foe and bursting.</summary>
    private static void CharmY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var pink = Rgb(255, 120, 170);
        var light = Rgb(255, 200, 225);
        float h = c.FromSize;
        foreach (float t0 in new[] { 0f, 0.14f })
            Fx.Burst(fx, c.Seed + (int)(t0 * 100), c.Age, c.FromFeet + Vector3.UnitY * h * 0.9f, h, t0, 0.5f, 3, FxShape.Heart, pink, light, 0.09f, 0.7f, 1.2f, FxBlend.Alpha);
        float k = (c.Age - 0.12f) / (I - 0.12f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From + Vector3.UnitY * h * 0.4f, c.Aim, k, 0.12f);
            fx.Sprite(FxShape.Heart, p, c.ScaleAt(k) * 0.22f, pink, FxBlend.Alpha, MathF.Sin(c.Age * 12f) * 0.25f);
            fx.Sprite(FxShape.Glow, p, c.ScaleAt(k) * 0.3f, Fx.Fade(light, 0.5f));
        }
        if (!c.Cue.Missed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 6, FxShape.Heart, pink, light, 0.08f, 0.75f, -0.4f, FxBlend.Alpha, salt: 35);
    }

    /// <summary>Leech Seed: three seeds lobbed onto the target, each sprouting a pair of leaves.</summary>
    private static void LeechSeed(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        Fx.Stream(fx, c, 0f, I + 0.06f, 0.3f, 3, FxShape.Seed, Rgb(150, 112, 60), Rgb(110, 84, 46), 0.15f, arc: 0.28f, spread: 0.35f, spin: 6f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        var green = ColorsOf(PokemonType.Grass);
        for (int i = 0; i < 3; i++)
        {
            float t0 = I + i * 0.06f;
            float k = (c.Age - t0) / 0.3f;
            float fade = Math.Clamp((1.15f - c.Age) / 0.25f, 0f, 1f);
            if (k < 0f || fade <= 0f) continue;
            var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 39) * h * 0.3f, (Fx.Rand(c.Seed, i, 40) - 0.3f) * h * 0.4f, h * 0.1f);
            float grow = Fx.EaseOut(k);
            fx.Sprite(FxShape.Seed, at, h * 0.11f, Fx.Fade(Rgb(130, 98, 54), fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Leaf, at + new Vector3(-h * 0.07f, h * 0.1f, 0f) * grow, h * 0.17f * grow, Fx.Fade(green.Main, fade), FxBlend.Alpha, 0.7f);
            fx.Sprite(FxShape.Leaf, at + new Vector3(h * 0.07f, h * 0.1f, 0f) * grow, h * 0.17f * grow, Fx.Fade(green.Light, fade), FxBlend.Alpha, -0.7f);
        }
    }

    /// <summary>Poison Sting: one long needle driven in, and a purple puff where it goes in.</summary>
    private static void PoisonSting(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, I - 0.16f, I, 0.16f, 1, FxShape.Streak, Rgb(236, 220, 250), White, 0.06f, trail: 7f);
        Fx.Stream(fx, c, I - 0.16f, I, 0.16f, 1, FxShape.Dot, col.Main, col.Main, 0.05f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 5, FxShape.Smoke, Fx.Fade(col.Main, 0.7f), Fx.Fade(col.Light, 0.6f), 0.16f, 0.4f, -0.3f, FxBlend.Alpha, salt: 36);
            fx.Sprite(FxShape.Glow, c.To, c.ToSize * 0.6f, Fx.Fade(col.Main, 0.4f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.2f, I + 0.45f)), FxBlend.Alpha);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Stun Spore: a yellow cloud puffed over the target that sprinkles down on it, crackling.</summary>
    private static void StunSpore(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var yellow = Rgb(255, 222, 70);
        var pale = Rgb(255, 248, 190);
        Fx.Stream(fx, c, 0f, I, 0.28f, 4, FxShape.Smoke, Fx.Fade(yellow, 0.85f), Fx.Fade(pale, 0.75f), 0.3f, arc: 0.18f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.8f, 1.1f);
        for (int i = 0; i < 3; i++)
            fx.Sprite(FxShape.Smoke, c.ToFeet + new Vector3((i - 1) * h * 0.35f, h * (1.2f + 0.08f * (i % 2)), 0f), h * 0.42f, Fx.Fade(yellow, env * 0.8f), FxBlend.Alpha, i + c.Age);
        Fx.Rain(fx, c.Age, c.Seed, c.To, h * 0.8f, I, 1.1f, 0.5f, 18, FxShape.Dot, yellow, Rgb(230, 190, 40), 0.06f, FxBlend.Alpha);
        var electric = ColorsOf(PokemonType.Electric);
        foreach (var (t0, k) in new[] { (0.55f, 0), (0.78f, 1) })
            Fx.Bolt(fx, c.Age, c.Seed + 50 + k, c.To + Fx.Direction(c.Seed, 50 + k) * h * 0.4f, c.To + Fx.Direction(c.Seed, 55 + k) * h * 0.4f, t0, t0 + 0.14f, h * 0.025f, electric.Main, electric.Light, 0);
    }

    /// <summary>Wing Attack: two wings sweeping in from either side and crossing on the target, feathers flying.</summary>
    private static void WingAttack(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Feather, new Vector3(-0.9f, 0.45f, 0.2f), I, 0.5f, Rgb(246, 248, 255), -0.9f, 0.18f);
        Strike(c, fx, FxShape.Feather, new Vector3(0.9f, 0.45f, 0.2f), I, 0.5f, Rgb(230, 236, 250), 0.9f, 0.18f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.9f, 7, FxShape.Feather, White, col.Light, 0.1f, 0.9f, 0.5f, FxBlend.Alpha, spin: 3f, salt: 37);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.25f, c.ToSize * 0.8f, Fx.Fade(White, 0.8f));
        }
        Impact(c, col, fx);
    }

    /// <summary>Baton Pass: a baton spun up over the user, glinting at its height.</summary>
    private static void BatonPass(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var centre = c.FromFeet + Vector3.UnitY * h * (0.9f + 0.45f * Fx.EaseOut(c.Age / 0.6f));
        float a = c.Age * 16f;
        var arm = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * h * 0.3f;
        fx.Ribbon(new[] { centre - arm, centre, centre + arm }, h * 0.07f, Fx.Fade(Rgb(250, 246, 236), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, centre + arm, h * 0.14f, Fx.Fade(Rgb(255, 120, 150), env));
        fx.Sprite(FxShape.Glow, centre - arm, h * 0.14f, Fx.Fade(Rgb(120, 170, 255), env));
        Fx.Glint(fx, c.Age, centre, h, I, White, 0.5f);
    }

    /// <summary>Karate Chop: the edge of a hand chopping down across the target, a streak where it passes.</summary>
    private static void KarateChop(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(0.7f, 0.9f, 0.2f), I, 0.32f, Rgb(255, 214, 170), MathF.PI * 0.3f, 0.14f);
        if (c.Cue.Landed)
        {
            float k = (c.Age - (I - 0.03f)) / 0.28f;
            if (k >= 0f && k <= 1f)
            {
                var along = Vector3.Normalize(new Vector3(-0.7f, -0.9f, 0f));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.14f, 7f * Fx.EaseOut(k * 3f), Fx.Fade(col.Light, 1f - k));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.06f, 7f * Fx.EaseOut(k * 3f), Fx.Fade(White, 1f - k));
            }
        }
        Impact(c, col, fx);
    }

    /// <summary>Mud Bomb: a ball of mud lobbed high that bursts over the target in splatter.</summary>
    private static void MudBomb(in FxCtx c, FxColors col, FxList fx)
    {
        float k = (c.Age - 0.02f) / (I - 0.02f);
        if (k >= 0f && k <= 1f)
        {
            var p = Lob(c.From, c.Aim, k, 0.3f);
            fx.Sprite(FxShape.Orb, p, c.ScaleAt(k) * 0.24f, Mud, FxBlend.Alpha, c.Age * 6f);
            fx.Sprite(FxShape.Orb, p + Vector3.UnitY * c.ScaleAt(k) * 0.03f, c.ScaleAt(k) * 0.14f, Fx.Fade(Rgb(176, 136, 92), 0.8f), FxBlend.Alpha, c.Age * 6f);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.7f, 12, FxShape.Drop, Mud, MudDark, 0.09f, 1.1f, 2.6f, FxBlend.Alpha, salt: 38);
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.8f, 4, FxShape.Smoke, Fx.Fade(Mud, 0.75f), Fx.Fade(MudDark, 0.7f), 0.2f, 0.4f, -0.1f, FxBlend.Alpha, salt: 39);
        }
        Impact(c, col, fx);
    }

    /// <summary>Mud-Slap: clumps of mud flung from the user's feet, splattering on the target and staying a moment.</summary>
    private static void MudSlap(in FxCtx c, FxColors col, FxList fx)
    {
        var start = c.FromFeet + Vector3.UnitY * c.FromSize * 0.2f;
        for (int i = 0; i < 5; i++)
        {
            float leave = i * 0.03f;
            float k = (c.Age - leave) / (I - 0.12f);
            if (k < 0f || k > 1f) continue;
            var end = c.Aim + new Vector3(Fx.Signed(c.Seed, i, 41), Fx.Signed(c.Seed, i, 42), 0f) * c.ToSize * 0.25f;
            fx.Sprite(FxShape.Drop, Lob(start, end, k, 0.15f), c.ScaleAt(k) * 0.11f, Fx.Fade(i % 2 == 0 ? Mud : MudDark, 0.95f), FxBlend.Alpha, k * 6f + i);
        }
        if (c.Cue.Landed)
        {
            float fade = Fx.Envelope(c.Age, I - 0.02f, I + 0.04f, 0.85f, 1.15f);
            for (int i = 0; i < 4; i++)
            {
                var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 43) * 0.35f, Fx.Signed(c.Seed, i, 44) * 0.3f, 0.12f) * c.ToSize;
                fx.Sprite(FxShape.Smoke, at, c.ToSize * (0.12f + 0.04f * Fx.Rand(c.Seed, i, 45)), Fx.Fade(MudDark, fade * 0.9f), FxBlend.Alpha, i * 1.7f);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 8, FxShape.Drop, Mud, MudDark, 0.06f, 0.9f, 2.4f, FxBlend.Alpha, salt: 40);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Rage: the user flaring red with an anger mark over its head, then the blow.</summary>
    private static void Rage(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        fx.Sprite(FxShape.Glow, c.From, h * 0.85f, Fx.Fade(Rgb(255, 60, 50), 0.4f * Fx.Envelope(c.Age, 0f, 0.06f, 0.18f, 0.3f)), FxBlend.Alpha);
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.3f, h * 1.05f, 0f), h, 0f, 0.4f);
        SpeedLines(c, fx, 0.15f, 0.34f, 5, 0.55f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.4f, 6, FxShape.Spark, Rgb(255, 90, 70), Rgb(255, 200, 170), 0.1f, 0.9f, salt: 41);
        Impact(c, col, fx);
    }

    /// <summary>Sand Attack: two handfuls of sand fanned at the target and a cloud of it left over the target's face.</summary>
    private static void SandAttack(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var sand = Rgb(232, 208, 150);
        var dark = Rgb(196, 160, 100);
        Fx.Stream(fx, c, 0f, 0.3f, 0.2f, 14, FxShape.Dot, sand, dark, 0.04f, arc: 0.05f, spread: 0.6f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.13f, I + 0.08f, 0.2f, 14, FxShape.Dot, dark, sand, 0.04f, arc: 0.08f, spread: 0.6f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.85f, 1.15f);
        var head = c.ToFeet + Vector3.UnitY * h * 0.8f;
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + c.Age * 0.9f;
            fx.Sprite(FxShape.Smoke, head + new Vector3(MathF.Cos(a) * h * 0.3f, MathF.Sin(a * 0.8f) * h * 0.12f, 0.1f), h * 0.26f, Fx.Fade(sand, env * 0.55f), FxBlend.Alpha, a);
        }
    }

    /// <summary>Sonic Boom: three pale crescents in quick succession, a ring where each breaks.</summary>
    private static void SonicBoom(in FxCtx c, FxColors col, FxList fx)
    {
        var pale = Rgb(236, 244, 255);
        Fx.Waves(fx, c, 0f, I, 3, FxShape.Wave, pale, 0.6f, 0.2f);
        if (!c.Cue.Missed)
            for (int j = 0; j < 3; j++)
                Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, 0.2f + j * 0.075f, 0.2f, c.ToSize * 0.7f, Fx.Fade(pale, 0.85f));
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Hypnosis: the user's eyes glowing, rings rolling to the target, and a swirl turning over its head.</summary>
    private static void HypnosisY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float eyes = Fx.Envelope(c.Age, 0f, 0.08f, 0.3f, 0.45f);
        var head = c.FromFeet + Vector3.UnitY * h * 0.85f;
        if (eyes > 0f)
            for (int i = -1; i <= 1; i += 2)
                fx.Sprite(FxShape.Glow, head + new Vector3(i * h * 0.1f, 0f, h * 0.1f), h * 0.16f, Fx.Fade(col.Light, eyes));
        Fx.Waves(fx, c, 0.05f, I, 4, FxShape.Ring, Fx.Fade(col.Main, 0.85f), 0.4f, 0.22f);
        if (c.Cue.Missed) return;
        float env = Fx.Envelope(c.Age, I - 0.03f, I + 0.1f, 0.9f, 1.15f);
        var over = c.ToFeet + Vector3.UnitY * c.ToSize * 1.1f;
        fx.Sprite(FxShape.Swirl, over, c.ToSize * 0.35f, Fx.Fade(col.Main, env), FxBlend.Alpha, -c.Age * 8f);
        fx.Sprite(FxShape.Swirl, over, c.ToSize * 0.35f, Fx.Fade(col.Light, env * 0.6f), rotation: -c.Age * 8f);
    }

    /// <summary>Thunderbolt: three bolts struck down onto the target from above, sparks and a pale flash.</summary>
    private static void ThunderboltY7(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int k = 0; k < 3; k++)
        {
            float t0 = 0.08f + k * 0.1f;
            var top = c.To + new Vector3((k - 1) * h * 0.5f, h * 5f, -h * 1.2f);
            var bottom = c.Cue.Missed ? c.Aim : c.To + new Vector3((k - 1) * h * 0.15f, -h * 0.1f, 0f);
            Fx.Bolt(fx, c.Age, c.Seed + k * 19, top, bottom, t0, t0 + 0.24f, h * 0.06f, col.Main, col.Light, 2);
        }
        // A small spark at the user, not a glow over it: light added over a pale body washes it out to white
        fx.Sprite(FxShape.Spark, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize * 0.4f, Fx.Fade(col.Main, Fx.Envelope(c.Age, 0f, 0.06f, 0.2f, 0.35f)), rotation: c.Age * 3f);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(255, 250, 210), 0.25f * Fx.Envelope(c.Age, I - 0.02f, I + 0.02f, I + 0.05f, I + 0.25f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 10, FxShape.Spark, col.Main, col.Light, 0.1f, 1.1f, salt: 42);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Feint Attack: dark smoke where the user was, and smoke on the far side of the target as the blow lands.</summary>
    private static void FeintAttack(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Burst(fx, c.Seed, c.Age, c.From, c.FromSize, 0.02f, 0.5f, 6, FxShape.Smoke, Rgb(60, 40, 76, 220), Rgb(30, 20, 40, 210), 0.2f, 0.5f, -0.2f, FxBlend.Alpha, salt: 43);
        var behind = c.To + Vector3.Normalize(c.To - c.From) * c.ToSize * 0.45f + Vector3.UnitY * c.ToSize * 0.1f;
        Fx.Burst(fx, c.Seed, c.Age, behind, c.ToSize, I - 0.12f, 0.5f, 6, FxShape.Smoke, Rgb(60, 40, 76, 220), Rgb(30, 20, 40, 210), 0.2f, 0.5f, -0.2f, FxBlend.Alpha, salt: 44);
        if (c.Cue.Landed) Fx.Slash(fx, c.Age, c.Seed, c.To, c.ToSize, I - 0.03f, 1, 0.1f, col.Main, col.Light);
        Impact(c, col, fx);
    }

    /// <summary>Toxic: sludge lobbed over, then purple bubbles welling up round the target and popping.</summary>
    private static void ToxicY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        Fx.Stream(fx, c, 0f, I, 0.3f, 3, FxShape.Orb, col.Main, col.Dark, 0.13f, arc: 0.18f, spread: 0.25f, blend: FxBlend.Alpha);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.2f, h, I, 0.7f, 10, FxShape.Orb, col.Main, col.Dark, 0.12f, 0.6f, -1.1f, FxBlend.Alpha, salt: 45);
        Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.3f, h, I + 0.25f, 0.6f, 8, FxShape.Bubble, col.Light, col.Main, 0.08f, 0.7f, -1.2f, FxBlend.Alpha, salt: 46);
        fx.Sprite(FxShape.Glow, c.To, h * 0.8f, Fx.Fade(col.Main, 0.4f * Fx.Envelope(c.Age, I, I + 0.1f, 0.8f, 1.1f)), FxBlend.Alpha);
    }

    /// <summary>Agility: the user darting from side to side, streaks either side of it, twice.</summary>
    private static void AgilityY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        foreach (float t0 in new[] { 0.04f, 0.42f })
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float k = (c.Age - t0 - i * 0.03f) / 0.22f;
                if (k < 0f || k > 1f) continue;
                var p = c.FromFeet + new Vector3(side * h * (0.45f + 0.35f * k), h * (0.25f + 0.12f * i), 0.1f);
                fx.Streak(FxShape.Streak, p, Vector3.UnitX * side, h * 0.06f, 7f, Fx.Fade(White, 0.8f * MathF.Sin(k * MathF.PI)));
            }
            float ghost = Fx.Envelope(c.Age, t0, t0 + 0.04f, t0 + 0.12f, t0 + 0.25f);
            if (ghost > 0f)
                for (int s = -1; s <= 1; s += 2)
                    fx.Sprite(FxShape.Glow, c.From + Vector3.UnitX * s * h * 0.45f, h * 0.6f, Fx.Fade(col.Light, ghost * 0.3f));
        }
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.2f, h, 0.75f, White, 0.4f);
    }

    /// <summary>Aqua Jet: a jet of water from the user to the target, and its spray.</summary>
    private static void AquaJet(in FxCtx c, FxColors col, FxList fx)
    {
        float reach = Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0.05f, 0.1f, I + 0.05f, I + 0.3f);
        if (reach > 0f && fade > 0f)
        {
            var end = Vector3.Lerp(c.From, c.Aim, reach * reach);
            var pts = new[] { c.From, Vector3.Lerp(c.From, end, 0.5f), end };
            float w = (c.FromSize + c.ToSize) * 0.5f;
            fx.Ribbon(pts, w * 0.35f * fade, Fx.Fade(col.Main, 0.75f), FxBlend.Alpha);
            fx.Ribbon(pts, w * 0.12f * fade, Fx.Fade(col.Light, 0.9f));
            fx.Sprite(FxShape.Glow, end, w * 0.45f * fade, Fx.Fade(col.Light, 0.7f));
        }
        Fx.Stream(fx, c, 0.05f, I + 0.05f, 0.18f, 10, FxShape.Drop, col.Light, col.Main, 0.07f, spread: 0.35f, blend: FxBlend.Alpha, trail: 1.2f);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 10, FxShape.Drop, col.Main, col.Light, 0.08f, 1.0f, 2.4f, FxBlend.Alpha, salt: 47);
        Impact(c, col, fx);
    }

    /// <summary>Body Slam: the weight landing, a wide ring on the ground and dust, the picture jolted.</summary>
    private static void BodySlam(in FxCtx c, FxColors col, FxList fx)
    {
        SpeedLines(c, fx, 0.12f, 0.34f, 5, 0.5f);
        if (c.Cue.Landed)
        {
            float h = c.ToSize;
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.45f, h * 1.9f, Fx.Fade(Rgb(240, 230, 210), 0.9f));
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I + 0.08f, 0.45f, h * 1.4f, Fx.Fade(Rgb(200, 186, 160), 0.8f));
            Dust(c, fx, c.ToFeet, h * 1.3f, I, 8);
            fx.Shake = Math.Max(fx.Shake, 0.5f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.15f, I + 0.4f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Confuse Ray: a wobbling orb of pale light that floats over and circles the target's head.</summary>
    private static void ConfuseRay(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var light = Rgb(255, 238, 130);
        Vector3 p;
        float size;
        if (c.Age < I)
        {
            float k = c.Age / I;
            p = Vector3.Lerp(c.From, c.Aim, k * k * (3f - 2f * k)) + Vector3.UnitY * MathF.Sin(k * 18f) * c.ScaleAt(k) * 0.12f;
            size = c.ScaleAt(k);
        }
        else
        {
            if (c.Cue.Missed) return;
            float a = (c.Age - I) * 9f;
            p = c.ToFeet + new Vector3(MathF.Cos(a) * c.ToSize * 0.4f, c.ToSize * (1.0f + 0.06f * MathF.Sin(a * 2f)), MathF.Sin(a) * c.ToSize * 0.25f);
            size = c.ToSize;
        }
        float env = Fx.Envelope(c.Age, 0f, 0.08f, 0.85f, 1.15f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, p, size * 0.4f, Fx.Fade(light, env * 0.8f));
        fx.Sprite(FxShape.Orb, p, size * 0.18f, Fx.Fade(Rgb(255, 250, 220), env), rotation: c.Age * 5f);
        for (int i = 1; i <= 3; i++)
            fx.Sprite(FxShape.Spark, p + Fx.Direction(c.Seed + (int)(c.Age * 12f), i) * size * 0.25f, size * 0.07f, Fx.Fade(light, env * (1f - i * 0.25f)));
    }

    /// <summary>Fire Punch: a flaming fist driven in, flames bursting off it and a deep red flicker.</summary>
    private static void FirePunch(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Fist, new Vector3(-0.7f, 0.2f, 0.3f), I, 0.3f, Rgb(255, 196, 140));
        Fx.Stream(fx, c, I - 0.16f, I + 0.02f, 0.14f, 6, FxShape.Flame, col.Main, col.Light, 0.15f, spread: 0.2f);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(140, 30, 14), 0.15f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.08f, I + 0.3f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.6f, 8, FxShape.Flame, col.Main, col.Light, 0.15f, 0.8f, -0.8f, salt: 48);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Fire Spin: embers sent across, then a column of flame spiralling up round the target.</summary>
    private static void FireSpinY7(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, I, 0.25f, 8, FxShape.Ember, col.Light, col.Main, 0.1f, arc: 0.04f, spread: 0.3f);
        if (!c.Cue.Missed)
        {
            float h = c.ToSize;
            float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.1f, 0.95f, 1.2f);
            if (env > 0f)
                for (int i = 0; i < 14; i++)
                {
                    float y = (i / 14f + c.Age * 0.9f) % 1f;
                    float a = y * MathF.Tau * 2f + c.Age * 6f;
                    var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * (0.65f - 0.25f * y), h * (0.05f + 1.0f * y), MathF.Sin(a) * h * (0.4f - 0.15f * y));
                    fx.Sprite(FxShape.Flame, p, h * 0.24f * (1f - 0.3f * y), Fx.Fade(i % 2 == 0 ? col.Main : col.Light, env * MathF.Sin(y * MathF.PI)), depth: MathF.Sin(a) < 0f);
                }
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Hidden Power: six orbs circling the user, then converging on the target.</summary>
    private static void HiddenPower(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float go = Fx.EaseOut((c.Age - 0.2f) / (I - 0.2f));
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I, I + 0.05f);
        if (env > 0f && !(c.Cue.Missed && go >= 1f))
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 7f;
                var ring = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * 0.55f, MathF.Sin(a) * h * 0.35f);
                var p = Vector3.Lerp(ring, c.Aim, go);
                float s = c.ScaleAt(go) * 0.1f;
                fx.Sprite(FxShape.Glow, p, s * 2f, Fx.Fade(col.Light, env * 0.7f));
                fx.Sprite(FxShape.Orb, p, s, Fx.Fade(Rgb(236, 230, 255), env), rotation: c.Age * 4f);
            }
        Impact(c, col, fx);
    }

    /// <summary>Magnet Bomb: four steel orbs arcing in, each bursting in sparks where it lands.</summary>
    private static void MagnetBomb(in FxCtx c, FxColors col, FxList fx)
    {
        const float flight = 0.28f;
        Fx.Stream(fx, c, 0f, I + 0.04f, flight, 4, FxShape.Orb, col.Light, col.Main, 0.14f, arc: 0.22f, spread: 0.45f, blend: FxBlend.Alpha);
        if (!c.Cue.Missed)
            for (int i = 0; i < 4; i++)
            {
                float arrive = (I + 0.04f - flight) * i / 3f + flight;
                var at = c.To + new Vector3(Fx.Signed(c.Seed, i, 49), Fx.Signed(c.Seed, i, 50), 0f) * c.ToSize * 0.2f;
                Fx.Burst(fx, c.Seed + i, c.Age, at, c.ToSize, arrive, 0.3f, 4, FxShape.Spark, White, col.Light, 0.1f, 0.8f, salt: 51);
                fx.Shake = Math.Max(fx.Shake, 0.15f * Fx.Envelope(c.Age, arrive, arrive + 0.02f, arrive + 0.05f, arrive + 0.12f));
            }
        Impact(c, col, fx);
    }

    /// <summary>Rain Dance: dark clouds gathering high over the field and rain falling from them, splashing at the user's feet.</summary>
    private static void RainDanceY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.25f, 0.9f, 1.2f);
        // Over the field behind the user (the cameras stand on +Z), low enough to stay in the picture over a near body
        var sky = c.From + new Vector3(0f, h * 0.6f, -h * 3f);
        for (int i = 0; i < 5; i++)
        {
            var at = sky + new Vector3((i - 2) * h * 0.6f, Fx.Signed(c.Seed, i, 52) * h * 0.1f, Fx.Signed(c.Seed, i, 53) * h * 0.2f);
            fx.Sprite(FxShape.Smoke, at, h * 0.75f, Fx.Fade(Rgb(86, 94, 116), env * 0.85f), FxBlend.Alpha, i * 1.3f);
        }
        var blue = ColorsOf(PokemonType.Water);
        Fx.Rain(fx, c.Age, c.Seed, c.From + new Vector3(-h * 0.3f, -h * 1.0f, -h * 2.2f), h * 0.6f, 0.15f, 1.15f, 0.3f, 30, FxShape.Streak, blue.Main, blue.Dark, 0.06f, FxBlend.Alpha, 4f);
        for (int j = 0; j < 3; j++)
            Fx.Shockwave(fx, c.Age, c.FromFeet + new Vector3((j - 1) * h * 0.4f, 0.03f, 0f), Vector3.UnitY, 0.45f + j * 0.15f, 0.35f, h * 0.35f, Fx.Fade(blue.Light, 0.8f));
    }

    /// <summary>Spark: the user crackling yellow, a quick flicker, and sparks thrown off the target.</summary>
    private static void SparkY7(in FxCtx c, FxColors col, FxList fx)
    {
        for (int k = 0; k < 3; k++)
        {
            var a = c.From + Fx.Direction(c.Seed, 56 + k) * c.FromSize * 0.5f;
            var b = c.From + Fx.Direction(c.Seed, 60 + k) * c.FromSize * 0.5f;
            Fx.Bolt(fx, c.Age, c.Seed + k * 7, a, b, k * 0.06f, 0.3f, c.FromSize * 0.03f, col.Main, col.Light, 0);
        }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.8f, Fx.Fade(col.Main, 0.45f * Fx.Envelope(c.Age, 0f, 0.05f, 0.2f, 0.32f)));
        SpeedLines(c, fx, 0.12f, 0.34f, 5, 0.5f);
        if (c.Cue.Landed)
        {
            fx.ScreenFlash(Rgb(255, 250, 190), 0.12f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.05f, I + 0.18f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.45f, 10, FxShape.Spark, col.Main, col.Light, 0.11f, 1.0f, streak: 1.5f, salt: 52);
        }
        Impact(c, col, fx);
    }

    /// <summary>Swift: a fan of stars curving in with trails.</summary>
    private static void SwiftY7(in FxCtx c, FxColors col, FxList fx)
    {
        var gold = Rgb(255, 236, 120);
        var (across, up) = Fx.Frame(c.Aim - c.From);
        for (int i = 0; i < 7; i++)
        {
            float leave = i * 0.012f;
            float bend = (i - 3) / 3f;
            for (int trail = 0; trail < 3; trail++)
            {
                float t = (c.Age - leave - trail * 0.035f) / (I - 0.08f);
                if (t < 0f || t > 1f) continue;
                var p = Vector3.Lerp(c.From, c.Aim, t) + (across * bend * 1.0f + up * (0.3f + 0.2f * MathF.Abs(bend))) * c.ScaleAt(t) * MathF.Sin(t * MathF.PI);
                float s = c.ScaleAt(t) * 0.12f * (1f - trail * 0.3f);
                var color = trail == 0 ? gold : Fx.Fade(gold, 0.55f - trail * 0.15f);
                fx.Sprite(FxShape.Star, p, s, Fx.Fade(color, Math.Min(1f, (1f - t) * 6f)), trail == 0 ? FxBlend.Alpha : FxBlend.Add, c.Age * 8f + i);
            }
        }
        Impact(c, col, fx);
    }

    /// <summary>Thunder Wave: rings rolled over, then a ring of electricity tightening round the target and crackling.</summary>
    private static void ThunderWaveY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        Fx.Waves(fx, c, 0f, I, 2, FxShape.Ring, Fx.Fade(col.Main, 0.8f), 0.32f, 0.25f);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.05f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float squeeze = Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f));
        var normal = Vector3.Normalize(new Vector3(0f, 1f, 0.55f));
        var at = c.ToFeet + Vector3.UnitY * h * 0.5f;
        float radius = h * (0.95f - 0.3f * squeeze);
        fx.Flat(FxShape.Ring, at, normal, radius, Fx.Fade(col.Main, env), depth: false);
        var across = Vector3.UnitX;
        var round = Vector3.Normalize(Vector3.Cross(normal, across));
        for (int k = 0; k < 4; k++)
        {
            float a0 = k * MathF.Tau / 4f + c.Age * 3f, a1 = a0 + 0.9f;
            var p0 = at + (across * MathF.Cos(a0) + round * MathF.Sin(a0)) * radius * 0.7f;
            var p1 = at + (across * MathF.Cos(a1) + round * MathF.Sin(a1)) * radius * 0.7f;
            float t0 = I + k * 0.1f;
            Fx.Bolt(fx, c.Age, c.Seed + k * 3, p0, p1, t0, t0 + 0.4f, h * 0.025f, col.Main, col.Light, 0);
        }
    }

    /// <summary>Confusion: the user glowing pale, and pink rings rippling on the target, wobbling.</summary>
    private static void ConfusionY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.7f, Fx.Fade(col.Light, 0.4f * Fx.Envelope(c.Age, 0f, 0.05f, 0.15f, 0.28f)));
        if (!c.Cue.Missed)
        {
            float env = Fx.Envelope(c.Age, 0.1f, 0.2f, 0.75f, 0.95f);
            if (env > 0f)
                for (int j = 0; j < 4; j++)
                {
                    float u = (c.Age * 1.5f + j / 4f) % 1f;
                    var wobble = new Vector3(MathF.Sin(c.Age * 21f + j) * c.ToSize * 0.04f, MathF.Cos(c.Age * 17f + j) * c.ToSize * 0.03f, 0f);
                    fx.Sprite(FxShape.Ring, c.To + wobble, c.ToSize * (0.3f + 0.8f * u), Fx.Fade(j % 2 == 0 ? col.Main : col.Light, env * (1f - u)));
                }
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Gust: a whirlwind at the target with feathers caught in it.</summary>
    private static void GustY7(in FxCtx c, FxColors col, FxList fx)
    {
        Fx.Stream(fx, c, 0f, I - 0.05f, 0.2f, 6, FxShape.Streak, col.Light, White, 0.05f, spread: 0.5f, trail: 3f);
        if (!c.Cue.Missed)
        {
            Whirl(c, col, fx, 0.15f, 0.95f);
            Round(c, fx, FxShape.Feather, 6, White, col.Light, 0.5f, 0.1f, 9f, FxBlend.Alpha);
        }
        Impact(c, col, fx);
    }

    /// <summary>Rollout: a rolling wheel of dust carried in along the ground.</summary>
    private static void Rollout(in FxCtx c, FxColors col, FxList fx)
    {
        float k = Math.Clamp(c.Age / I, 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.05f, I, I + 0.15f);
        if (fade > 0f)
        {
            float size = c.ScaleAt(k);
            var p = Vector3.Lerp(c.FromFeet, c.Cue.Missed ? c.Aim : c.ToFeet, k * k) + Vector3.UnitY * size * 0.4f;
            fx.Sprite(FxShape.Swirl, p, size * 0.5f, Fx.Fade(Rgb(214, 196, 160), fade * 0.9f), FxBlend.Alpha, -c.Age * 22f);
            fx.Sprite(FxShape.Ring, p, size * 0.45f, Fx.Fade(White, fade * 0.5f));
            Fx.Burst(fx, c.Seed, c.Age, p - Vector3.UnitY * size * 0.35f, size, c.Age - (c.Age % 0.1f), 0.25f, 2, FxShape.Smoke, Rgb(222, 206, 176, 200), Rgb(176, 160, 132, 190), 0.12f, 0.3f, -0.2f, FxBlend.Alpha, salt: 53);
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * c.ToSize * 0.2f, c.ToSize, I, 0.6f, 6, FxShape.Rock, Rgb(196, 170, 130), Rgb(120, 96, 70), 0.08f, 1.0f, 2.2f, FxBlend.Alpha, spin: 5f, salt: 54);
        Impact(c, col, fx);
    }

    /// <summary>Sleep Talk: Z's rising from the sleeper and a pale wave of a mumble.</summary>
    private static void SleepTalk(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - i * 0.2f) / 0.75f;
            if (k < 0f || k > 1f) continue;
            var p = c.FromFeet + new Vector3(h * (0.25f + 0.35f * k), h * (0.9f + 0.45f * k), 0f);
            fx.Sprite(FxShape.Zzz, p, h * (0.09f + 0.07f * k), Fx.Fade(Rgb(210, 226, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, 0.25f);
        }
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.15f - i * 0.15f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            var p = c.FromFeet + new Vector3(-h * (0.3f + 0.4f * k), h * 0.75f, h * 0.1f);
            fx.Sprite(FxShape.Wave, p, h * (0.18f + 0.2f * k), Fx.Fade(Rgb(236, 240, 255), (1f - k) * 0.8f), rotation: MathF.PI);
        }
    }

    /// <summary>Curse: the user flaring red, shuddering, and dark wisps rising round it.</summary>
    private static void CurseY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        fx.Sprite(FxShape.Glow, c.From, h * 0.85f, Fx.Fade(Rgb(230, 40, 50), 0.4f * Fx.Envelope(c.Age, 0f, 0.1f, 0.3f, 0.5f)), FxBlend.Alpha);
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + Vector3.UnitY * h * 0.2f, h, 0.25f, 0.85f, 8, FxShape.Wisp, Rgb(70, 40, 90, 220), Rgb(130, 90, 170, 200), 0.16f, 0.5f, -1.0f, FxBlend.Alpha, salt: 55);
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 1.3f + Fx.Rand(c.Seed, i, 56)) % 1f;
            float env = Fx.Envelope(c.Age, 0.1f, 0.25f, 0.85f, 1.1f);
            float a = Fx.Rand(c.Seed, i, 57) * MathF.Tau;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.1f + 1.0f * u), MathF.Sin(a) * h * 0.3f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.05f, 5f, Fx.Fade(Rgb(255, 90, 90), env * MathF.Sin(u * MathF.PI)));
        }
        fx.Shake = Math.Max(fx.Shake, 0.15f * Fx.Envelope(c.Age, I - 0.05f, I, I + 0.15f, I + 0.3f));
    }

    /// <summary>Endure: the user bracing, red pulses round it and red streaks rising.</summary>
    private static void Endure(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.8f, 1.05f);
        float beat = 0.5f + 0.5f * MathF.Cos(c.Age * MathF.Tau / 0.3f);
        fx.Sprite(FxShape.Glow, c.From, h * (0.75f + 0.1f * beat), Fx.Fade(Rgb(255, 70, 60), env * (0.25f + 0.2f * beat)), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.05f, I, 0.65f })
            Fx.Shockwave(fx, c.Age, c.FromFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, t0, 0.35f, h * 1.1f, Rgb(255, 110, 90));
        for (int i = 0; i < 6; i++)
        {
            float u = (c.Age * 1.6f + Fx.Rand(c.Seed, i, 58)) % 1f;
            float a = Fx.Rand(c.Seed, i, 59) * MathF.Tau;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.45f, h * (0.1f + 1.0f * u), MathF.Sin(a) * h * 0.25f);
            fx.Streak(FxShape.Streak, p, Vector3.UnitY, h * 0.05f, 5f, Fx.Fade(Rgb(255, 150, 120), env * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>Frustration: an anger mark popping over the user, then a flurry of blows on the target.</summary>
    private static void Frustration(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        Anger(fx, c.Age, c.FromFeet + new Vector3(h * 0.3f, h * 1.05f, 0f), h, 0f, 0.35f);
        fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(Rgb(255, 60, 50), 0.35f * Fx.Envelope(c.Age, 0f, 0.06f, 0.2f, 0.3f)), FxBlend.Alpha);
        if (c.Cue.Landed)
            for (int i = 0; i < 3; i++)
            {
                var off = new Vector3(Fx.Signed(c.Seed, i, 60), Fx.Signed(c.Seed, i, 61), 0f) * c.ToSize * 0.3f;
                Fx.Contact(fx, c.Age, c.Seed + i, c.To + off, c.ToSize, I - 0.1f + i * 0.08f, 0.22f, col.Main, col.Light);
            }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Mean Look: a great dark eye opening over the target, and purple rings closing in on it.</summary>
    private static void MeanLook(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.05f, I, 0.85f, 1.15f);
        var at = c.To + new Vector3(0f, h * 0.15f, h * 0.2f);
        fx.Sprite(FxShape.Eye, at, h * (0.8f + 0.25f * env), Fx.Fade(Rgb(80, 30, 100), env * 0.9f), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, at, h * 1.1f, Fx.Fade(Rgb(150, 60, 190), env * 0.35f));
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - I - i * 0.12f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Ring, c.To, h * (1.4f - 0.9f * k), Fx.Fade(Rgb(170, 90, 220), MathF.Sin(k * MathF.PI)));
        }
    }

    /// <summary>Meditate: calm rings floating up round the user, motes drifting with them and a still glow.</summary>
    private static void Meditate(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var calm = Rgb(214, 196, 255);
        for (int j = 0; j < 3; j++)
        {
            float u = (c.Age * 0.7f + j / 3f) % 1f;
            fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (0.1f + 0.9f * u), Vector3.UnitY, h * (0.5f + 0.15f * u), Fx.Fade(calm, env * MathF.Sin(u * MathF.PI)), depth: true);
        }
        for (int i = 0; i < 8; i++)
        {
            float u = (c.Age * 0.5f + Fx.Rand(c.Seed, i, 62)) % 1f;
            float a = Fx.Rand(c.Seed, i, 63) * MathF.Tau + c.Age;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.2f + 1.0f * u), MathF.Sin(a) * h * 0.3f);
            fx.Sprite(FxShape.Glow, p, h * 0.07f, Fx.Fade(calm, env * MathF.Sin(u * MathF.PI)));
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.85f, Fx.Fade(calm, env * 0.3f));
        Fx.Glint(fx, c.Age, c.FromFeet + Vector3.UnitY * h * 0.85f, h, I + 0.2f, White, 0.35f);
    }

    /// <summary>Protect: a pale hexagonal shield springing up before the user, glinting.</summary>
    private static void ProtectY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var at = c.From + new Vector3(0f, h * 0.05f, h * 0.45f);
        Panel(fx, c.Age, at, h * 1.0f, 0.05f, Rgb(190, 255, 214));
        Fx.Glint(fx, c.Age, at + new Vector3(-h * 0.25f, h * 0.25f, 0f), h, I, White, 0.5f);
    }

    /// <summary>Reflect: a wall of hexagonal panels springing up before the user one after another, a glint sweeping across.</summary>
    private static void ReflectY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var centre = c.From + new Vector3(0f, h * 0.05f, h * 0.45f);
        float s = h * 0.42f;
        // A honeycomb of seven: the middle one and the six round it, flat sides up and down
        for (int i = 0; i < 7; i++)
        {
            var off = i == 0 ? Vector3.Zero : new Vector3(MathF.Cos((i - 1) * MathF.PI / 3f + MathF.PI / 6f), MathF.Sin((i - 1) * MathF.PI / 3f + MathF.PI / 6f), 0f) * s * 0.9f;
            Panel(fx, c.Age, centre + off, s, 0.02f + i * 0.035f, Rgb(200, 220, 255));
        }
        float k = (c.Age - I) / 0.35f;
        if (k >= 0f && k <= 1f)
            fx.Streak(FxShape.Streak, centre + new Vector3((k - 0.5f) * s * 3.4f, 0f, h * 0.02f), Vector3.UnitY, h * 0.08f, 6f, Fx.Fade(White, MathF.Sin(k * MathF.PI) * 0.8f));
    }

    /// <summary>Safeguard: a soft ring of light settling down round the user, sparkles in it.</summary>
    private static void Safeguard(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        var light = Rgb(226, 214, 255);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.15f);
        if (env <= 0f) return;
        float y = 1.4f - 1.3f * Fx.EaseOut(c.Age / I);
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * y, Vector3.UnitY, h * 0.85f, Fx.Fade(light, env), depth: true);
        fx.Flat(FxShape.Ring, c.FromFeet + Vector3.UnitY * h * (y + 0.04f), Vector3.UnitY, h * 0.8f, Fx.Fade(White, env * 0.5f), depth: true);
        fx.Sprite(FxShape.Glow, c.From, h * 0.85f, Fx.Fade(light, env * 0.3f));
        for (int i = 0; i < 6; i++)
            Fx.Glint(fx, c.Age, c.From + new Vector3(Fx.Signed(c.Seed, i, 64) * 0.55f, Fx.Signed(c.Seed, i, 65) * 0.45f, 0.1f) * h, h, 0.25f + i * 0.1f, White, 0.3f);
    }

    /// <summary>Spite: a pale wisp drawn up out of the target, flickering, and a dark ring where it left.</summary>
    private static void Spite(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.6f, Fx.Fade(Rgb(230, 230, 255), 0.35f * Fx.Envelope(c.Age, 0f, 0.05f, 0.15f, 0.3f)));
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I - 0.05f, 0.4f, h * 0.9f, Rgb(110, 80, 150));
        float k = (c.Age - I) / 0.75f;
        if (k >= 0f && k <= 1f && (int)(c.Age * 24f) % 3 != 2)
        {
            var p = c.To + new Vector3(MathF.Sin(k * 9f) * h * 0.1f, h * (0.1f + 0.9f * k), 0.1f);
            fx.Sprite(FxShape.Wisp, p, h * 0.22f, Fx.Fade(Rgb(206, 196, 255), MathF.Sin(k * MathF.PI)), rotation: MathF.Sin(k * 7f) * 0.3f);
        }
    }

    /// <summary>Night Shade: a dark aura round the user and a ribbon of shadow driven at the target.</summary>
    private static void NightShade(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.5f, 0.75f);
        if (env > 0f)
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 3f;
                var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * (0.3f + 0.1f * (i % 3)), MathF.Sin(a) * h * 0.3f);
                fx.Sprite(FxShape.Smoke, p, h * 0.28f, Fx.Fade(Rgb(46, 26, 66, 220), env * 0.8f), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
            }
        Fx.Beam(fx, c, 0.15f, 0.72f, 0.12f, Rgb(70, 36, 110), Rgb(190, 150, 240), 6);
        Impact(c, col, fx);
    }

    /// <summary>Substitute: a puff of smoke round the user and a pop as the doll stands in.</summary>
    private static void Substitute(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        Fx.Burst(fx, c.Seed, c.Age, c.From, h, 0.02f, 0.7f, 8, FxShape.Smoke, Rgb(236, 232, 226, 230), Rgb(196, 192, 186, 220), 0.25f, 0.6f, -0.2f, FxBlend.Alpha, salt: 66);
        Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I, 0.3f, h * 0.9f, White);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.3f, h, I + 0.05f, White, 0.45f);
    }

    /// <summary>Cut: one long clean slash across the target.</summary>
    private static void Cut(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Landed)
        {
            float k = (c.Age - (I - 0.05f)) / 0.32f;
            if (k >= 0f && k <= 1f)
            {
                var along = Vector3.Normalize(new Vector3(1f, -0.6f, 0f));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.18f, 11f * Fx.EaseOut(k * 3f), Fx.Fade(Rgb(220, 230, 240), 1f - k));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.07f, 11f * Fx.EaseOut(k * 3f), Fx.Fade(White, 1f - k));
            }
        }
        Impact(c, col, fx);
    }

    /// <summary>Thief: a hand darting in and back with something glinting in it.</summary>
    private static void Thief(in FxCtx c, FxColors col, FxList fx)
    {
        Strike(c, fx, FxShape.Palm, new Vector3(-0.7f, 0.15f, 0.3f), I, 0.26f, Rgb(200, 180, 210), -0.4f, 0.14f);
        if (c.Cue.Landed)
        {
            float k = (c.Age - I - 0.05f) / 0.4f;
            if (k >= 0f && k <= 1f)
            {
                var p = Vector3.Lerp(c.To, c.From, Fx.EaseOut(k)) + Vector3.UnitY * c.ScaleAt(1f - k) * 0.2f * MathF.Sin(k * MathF.PI);
                float fade = Math.Min(1f, (1f - k) * 4f);
                fx.Sprite(FxShape.Palm, p, c.ScaleAt(1f - k) * 0.2f, Fx.Fade(Rgb(200, 180, 210), fade), FxBlend.Alpha, 0.4f);
                fx.Sprite(FxShape.Spark, p + Vector3.UnitY * c.ScaleAt(1f - k) * 0.08f, c.ScaleAt(1f - k) * 0.18f, Fx.Fade(Rgb(255, 236, 140), fade), rotation: k * 3f);
            }
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Grass Knot: blades of grass rising round the target's feet and tightening round them.</summary>
    private static void GrassKnot(in FxCtx c, FxColors col, FxList fx)
    {
        if (!c.Cue.Missed)
        {
            float h = c.ToSize;
            float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.85f, 1.1f);
            float rise = Fx.EaseOut(c.Age / I);
            if (env > 0f)
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.Tau / 6f + 0.3f;
                    var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.5f, h * 0.15f * rise, MathF.Sin(a) * h * 0.3f);
                    fx.Sprite(FxShape.Leaf, p, h * 0.16f * rise, Fx.Fade(i % 2 == 0 ? col.Main : col.Dark, env), FxBlend.Alpha, MathF.Cos(a) * 0.3f, depth: MathF.Sin(a) < 0f);
                }
            Wound(c, fx, 1, 0.12f, 0.12f, col.Main, col.Light);
        }
        Impact(c, col, fx);
    }

    /// <summary>Magical Leaf: leaves spiralling in round their way to the target, sparkling trails behind them.</summary>
    private static void MagicalLeafY7(in FxCtx c, FxColors col, FxList fx)
    {
        var (across, up) = Fx.Frame(c.Aim - c.From);
        for (int i = 0; i < 8; i++)
        {
            float t = (c.Age - i * 0.025f) / (I - 0.18f);
            if (t < 0f || t > 1f) continue;
            float swirl = t * MathF.Tau * 1.2f + i * MathF.Tau / 8f;
            float wide = c.ScaleAt(t) * 0.45f * MathF.Sin(t * MathF.PI);
            var p = Vector3.Lerp(c.From, c.Aim, t) + (across * MathF.Cos(swirl) + up * MathF.Sin(swirl)) * wide;
            float fade = Math.Min(1f, (1f - t) * 6f);
            fx.Sprite(FxShape.Leaf, p, c.ScaleAt(t) * 0.12f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, fade), FxBlend.Alpha, swirl * 2f);
            fx.Sprite(FxShape.Spark, p - Vector3.Normalize(c.Aim - c.From) * c.ScaleAt(t) * 0.15f, c.ScaleAt(t) * 0.08f, Fx.Fade(Rgb(255, 250, 190), fade * 0.8f));
        }
        Impact(c, col, fx);
    }

    /// <summary>Razor Leaf: spinning leaf blades flung in a flat arc, a thin streak behind each.</summary>
    private static void RazorLeafY7(in FxCtx c, FxColors col, FxList fx)
    {
        var (across, _) = Fx.Frame(c.Aim - c.From);
        var dir = c.Aim - c.From;
        for (int i = 0; i < 6; i++)
        {
            float t = (c.Age - i * 0.03f) / (I - 0.15f);
            if (t < 0f || t > 1f) continue;
            float side = i % 2 == 0 ? 1f : -1f;
            var p = Vector3.Lerp(c.From, c.Aim, t) + across * side * c.ScaleAt(t) * 0.7f * MathF.Sin(t * MathF.PI);
            float fade = Math.Min(1f, (1f - t) * 6f);
            fx.Sprite(FxShape.Leaf, p, c.ScaleAt(t) * 0.13f, Fx.Fade(i % 3 == 0 ? col.Light : col.Main, fade), FxBlend.Alpha, c.Age * 25f + i);
            fx.Streak(FxShape.Streak, p, dir + across * side * MathF.Cos(t * MathF.PI) * dir.Length() * 0.7f, c.ScaleAt(t) * 0.04f, 4f, Fx.Fade(White, fade * 0.6f));
        }
        Impact(c, col, fx);
    }

    /// <summary>Smokescreen: a ball of smoke lobbed over that bursts into a dark cloud round the target.</summary>
    private static void Smokescreen(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        var smoke = Rgb(70, 66, 72, 230);
        float k = c.Age / I;
        if (k <= 1f) fx.Sprite(FxShape.Smoke, Lob(c.From, c.Aim, k, 0.25f), c.ScaleAt(k) * 0.25f, smoke, FxBlend.Alpha, c.Age * 5f);
        if (c.Cue.Missed) return;
        float h = c.ToSize;
        Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 10, FxShape.Smoke, smoke, Rgb(40, 38, 44, 220), 0.3f, 0.6f, -0.15f, FxBlend.Alpha, salt: 67);
        float env = Fx.Envelope(c.Age, I + 0.1f, I + 0.3f, 0.9f, 1.15f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + c.Age * 0.6f;
            fx.Sprite(FxShape.Smoke, c.To + new Vector3(MathF.Cos(a) * h * 0.3f, MathF.Sin(a) * h * 0.2f, 0.12f), h * 0.4f, Fx.Fade(smoke, env * 0.75f), FxBlend.Alpha, a);
        }
    }

    /// <summary>Sunny Day: a sun over the field turning its rays, shafts of light falling and a warm glow (0.3).</summary>
    private static void SunnyDayY7(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.25f, 0.85f, 1.15f);
        if (env <= 0f) return;
        var sun = c.From + new Vector3(h * 0.5f, h * 2.6f, -h * 0.5f);
        fx.Sprite(FxShape.Glow, sun, h * 1.4f, Fx.Fade(Rgb(255, 200, 90), env * 0.8f));
        fx.Sprite(FxShape.Spark, sun, h * 1.6f, Fx.Fade(Rgb(255, 240, 180), env), rotation: c.Age * 0.8f);
        fx.Sprite(FxShape.Orb, sun, h * 0.45f, Fx.Fade(Rgb(255, 236, 160), env), rotation: c.Age * 2f);
        for (int i = 0; i < 6; i++)
        {
            var foot = c.FromFeet + new Vector3((i - 2.5f) * h * 0.45f, 0f, Fx.Signed(c.Seed, i, 68) * h * 0.4f);
            float shimmer = 0.5f + 0.5f * MathF.Sin(c.Age * 6f + i * 1.7f);
            fx.Ribbon(new[] { sun, Vector3.Lerp(sun, foot, 0.5f), foot }, h * 0.12f, Fx.Fade(Rgb(255, 230, 150), env * 0.25f * shimmer));
        }
        fx.ScreenFlash(Rgb(255, 220, 150), 0.3f * Fx.Envelope(c.Age, 0f, 0.2f, 0.6f, 1.1f));
    }

    /// <summary>Recycle: three arrows circling the user, and a pale glow as it finds what it had.</summary>
    private static void Recycle(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f + c.Age * 5f;
            var p = c.FromFeet + new Vector3(MathF.Cos(a) * h * 0.6f, h * 0.55f + MathF.Sin(a) * h * 0.15f, MathF.Sin(a) * h * 0.35f);
            fx.Sprite(FxShape.Arrow, p, h * 0.22f, Fx.Fade(Rgb(150, 230, 140), env), FxBlend.Alpha, a + MathF.PI / 2f, depth: MathF.Sin(a) < 0f);
        }
        fx.Sprite(FxShape.Glow, c.From, h * 0.8f, Fx.Fade(White, 0.35f * Fx.Envelope(c.Age, I, I + 0.1f, 0.6f, 0.85f)));
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.2f, h, 0.6f, White, 0.4f);
    }

    /// <summary>Taunt: a hand beckoning over the user, then an anger mark over the target and a red flush on it.</summary>
    private static void Taunt(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float hand = Fx.Envelope(c.Age, 0f, 0.06f, 0.45f, 0.6f);
        if (hand > 0f)
            fx.Sprite(FxShape.Palm, c.FromFeet + new Vector3(h * 0.45f, h * 1.05f, h * 0.1f), h * 0.24f, Fx.Fade(Rgb(255, 226, 196), hand), FxBlend.Alpha, MathF.Sin(c.Age * 18f) * 0.45f);
        if (c.Cue.Missed) return;
        Anger(fx, c.Age, c.ToFeet + new Vector3(c.ToSize * 0.3f, c.ToSize * 1.05f, 0f), c.ToSize, I, 1.1f);
        fx.Sprite(FxShape.Glow, c.To, c.ToSize * 0.8f, Fx.Fade(Rgb(255, 70, 60), 0.3f * Fx.Envelope(c.Age, I, I + 0.05f, I + 0.2f, I + 0.45f)), FxBlend.Alpha);
    }

    /// <summary>Night Slash: a dark crescent slashed across the target, then a second across it.</summary>
    private static void NightSlash(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Landed)
        {
            foreach (var (t0, tilt) in new[] { (I - 0.06f, 0.6f), (I + 0.08f, -0.6f) })
            {
                float k = (c.Age - t0) / 0.3f;
                if (k < 0f || k > 1f) continue;
                var along = Vector3.Normalize(new Vector3(1f, -tilt, 0f));
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.2f, 9f * Fx.EaseOut(k * 3f), Fx.Fade(Rgb(60, 30, 80, 230), 1f - k), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, c.To, along, c.ToSize * 0.07f, 9f * Fx.EaseOut(k * 3f), Fx.Fade(Rgb(220, 180, 255), 1f - k));
            }
            fx.ScreenFlash(Rgb(40, 20, 50), 0.15f * Fx.Envelope(c.Age, I - 0.06f, I - 0.03f, I + 0.05f, I + 0.25f));
        }
        Impact(c, col, fx);
    }
}
