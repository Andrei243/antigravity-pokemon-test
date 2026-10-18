using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y11, its second part (Y11b): the later games' moves, the other 36 of Generation 5 (numbers 524 to 559,
// Frost Breath to Fusion Bolt), each a look of its own. As Y11a: no game's animation exists for any of them, so the
// shapes, colours and timings are our own, taken from each move's data (type, category, target, effect, description) and
// the style guide's G8 vocabulary. Every recipe lands at ImpactTime, is over by EffectTime and stays under MaxQuads.
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later2() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Frost Breath"] = FrostBreathY11b,
        ["Dragon Tail"] = DragonTailY11b,
        ["Work Up"] = WorkUpY11b,
        ["Electroweb"] = ElectrowebY11b,
        ["Wild Charge"] = WildChargeY11b,
        ["Drill Run"] = DrillRunY11b,
        ["Dual Chop"] = DualChopY11b,
        ["Heart Stamp"] = HeartStampY11b,
        ["Horn Leech"] = HornLeechY11b,
        ["Sacred Sword"] = SacredSwordY11b,
        ["Razor Shell"] = RazorShellY11b,
        ["Heat Crash"] = HeatCrashY11b,
        ["Leaf Tornado"] = LeafTornadoY11b,
        ["Steamroller"] = SteamrollerY11b,
        ["Cotton Guard"] = CottonGuardY11b,
        ["Night Daze"] = NightDazeY11b,
        ["Psystrike"] = PsystrikeY11b,
        ["Tail Slap"] = TailSlapY11b,
        ["Hurricane"] = HurricaneY11b,
        ["Head Charge"] = HeadChargeY11b,
        ["Gear Grind"] = GearGrindY11b,
        ["Searing Shot"] = SearingShotY11b,
        ["Techno Blast"] = TechnoBlastY11b,
        ["Relic Song"] = RelicSongY11b,
        ["Secret Sword"] = SecretSwordY11b,
        ["Glaciate"] = GlaciateY11b,
        ["Bolt Strike"] = BoltStrikeY11b,
        ["Blue Flare"] = BlueFlareY11b,
        ["Fiery Dance"] = FieryDanceY11b,
        ["Freeze Shock"] = FreezeShockY11b,
        ["Ice Burn"] = IceBurnY11b,
        ["Snarl"] = SnarlY11b,
        ["Icicle Crash"] = IcicleCrashY11b,
        ["V-create"] = VCreateY11b,
        ["Fusion Flare"] = FusionFlareY11b,
        ["Fusion Bolt"] = FusionBoltY11b
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>A closed ellipse drawn as a ribbon, facing the camera, round <paramref name="centre"/>.</summary>
    private static void L2Ellipse(FxList fx, Vector3 centre, float rx, float ry, float width, Color color, int n = 16, float turn = 0f, FxBlend blend = FxBlend.Add)
    {
        var pts = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float a = i * MathF.Tau / n + turn;
            pts[i] = centre + new Vector3(MathF.Cos(a) * rx, MathF.Sin(a) * ry, 0.1f);
        }
        fx.Ribbon(pts, width, color, blend);
    }

    /// <summary>A point <paramref name="k"/> of the way along a lob from the user's middle to where the move ends, for something thrown.</summary>
    private static Vector3 L2Flight(in FxCtx c, float k, float arc = 0f) => Lob(c.From, c.Aim, Math.Clamp(k, 0f, 1f), arc);

    /// <summary>A heart-shaped warning that the target flinched: a small exclamation over its head after the landing.</summary>
    private static void L2Flinch(in FxCtx c, FxList fx, float t0)
    {
        if (!c.Cue.Landed) return;
        float env = Fx.Envelope(c.Age, t0, t0 + 0.06f, t0 + 0.4f, t0 + 0.55f);
        if (env <= 0f) return;
        float h = c.ToSize;
        fx.Sprite(FxShape.Exclaim, c.ToFeet + new Vector3(h * 0.3f, h * (0.95f + 0.04f * MathF.Sin(c.Age * 20f)), 0.2f), h * 0.2f, Fx.Fade(Rgb(255, 236, 120), env), FxBlend.Alpha);
    }

    /// <summary>What a move that drains leaves on its user: a green aura of the stock heal, but over within 0.8 s so the cue's end is clear.</summary>
    private static void L2Heal(FxList fx, float age, int seed, Vector3 feet, float h, float t0) =>
        Fx.Aura(fx, age, seed, feet, h, t0, Math.Min(t0 + 0.8f, 1.25f), Rgb(120, 230, 140), Rgb(230, 255, 220), 10);

    /// <summary>Down arrows over somebody, for a stat that falls: <paramref name="count"/> of them drifting down.</summary>
    private static void L2Down(FxList fx, float age, int seed, Vector3 feet, float h, float t0, int count, Color color)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.08f, t0 + 0.5f, t0 + 0.65f);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float u = (age - t0 - i * 0.07f) / 0.5f;
            if (u < 0f || u > 1f) continue;
            fx.Sprite(FxShape.Arrow, feet + new Vector3((i - (count - 1) * 0.5f) * h * 0.32f, h * (0.95f - 0.3f * u), 0.2f), h * 0.18f, Fx.Fade(color, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, MathF.PI);
        }
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Frost Breath: a wide, pale breath of snow and frost-mist blown from the user, and at the landing a bright four-rayed star (it always crits).</summary>
    private static void FrostBreathY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Flurry(c, fx, 0.0f, 0.5f, 16, 0.18f, 0.7f);
        Fx.Stream(fx, c, 0.0f, 0.5f, 0.32f, 12, FxShape.Smoke, Rgb(222, 244, 255, 235), Rgb(170, 222, 250, 235), 0.38f, spread: 0.6f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.0f, 0.5f, 0.32f, 6, FxShape.Glow, White, col.Light, 0.3f, spread: 0.5f);
        if (c.Cue.Landed)
        {
            float star = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.14f, I + 0.34f);
            fx.Sprite(FxShape.Star, c.To, h * 0.8f * star, Fx.Fade(White, star), FxBlend.Alpha, MathF.PI / 4f);
            fx.Sprite(FxShape.Star, c.To, h * 0.55f * star, Fx.Fade(col.Light, star));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Snowflake, White, col.Light, 0.1f, 1.0f, 0.4f, FxBlend.Alpha, spin: 4f, salt: 260);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.02f, 0.45f, h * 1.15f, col.Light);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Dragon Tail: a thick indigo tail lashes round from the user's side and the foe is whipped away, three arrows streaking off after it.</summary>
    private static void DragonTailY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var from = c.From + new Vector3(c.FromSize * 0.3f, -c.FromSize * 0.3f, 0.1f);
        Whip(fx, c.Age, from, c.Aim, I - 0.24f, 0.22f, 0.05f, 0.03f, h * 0.38f, col.Main, col.Light, FxShape.Horn, h * 0.34f, MathF.PI / 3f);
        Whip(fx, c.Age, from + new Vector3(0f, c.FromSize * 0.2f, 0f), c.Aim + new Vector3(0f, h * 0.2f, 0f), I - 0.2f, 0.19f, 0.12f, 0.03f, h * 0.2f, Rgb(214, 196, 255), White, FxShape.Spark, h * 0.2f);
        if (c.Cue.Landed)
        {
            var away = L1Toward(c);
            for (int i = 0; i < 3; i++)
            {
                float k = (c.Age - I - i * 0.06f) / 0.35f;
                if (k < 0f || k > 1f) continue;
                var p = c.To + away * h * (0.5f + 0.9f * Fx.EaseOut(k)) + new Vector3(0f, h * (i - 1) * 0.28f, 0.2f);
                fx.Streak(FxShape.Streak, p, away, h * 0.06f, 7f, Fx.Fade(col.Light, MathF.Sin(k * MathF.PI)));
                fx.Sprite(FxShape.Arrow, p + away * h * 0.2f, h * 0.18f, Fx.Fade(Rgb(214, 196, 255), MathF.Sin(k * MathF.PI)), FxBlend.Alpha, ScreenAngle(away) - MathF.PI / 2f);
            }
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.35f, h * 1.1f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Work Up: the user's heat rises, red rings pulsing out of it and fists of orange sparks climbing either side under two up arrows.</summary>
    private static void WorkUpY11b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var red = Rgb(255, 96, 70);
        fx.Sprite(FxShape.Glow, c.From, h * 1.2f, Fx.Fade(red, env * 0.35f), FxBlend.Alpha);
        foreach (float t0 in new[] { 0.05f, 0.3f, 0.55f })
            PulseRing(fx, c.Age, c.From, t0, 0.4f, h * 0.3f, h * 1.0f, Rgb(255, 150, 90), Rgb(255, 220, 170));
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 3; i++)
            {
                float u = (c.Age * 1.2f + i / 3f) % 1f;
                var p = c.FromFeet + new Vector3(s * h * (0.55f + 0.05f * i), h * (0.15f + 0.85f * u), 0.2f);
                fx.Sprite(FxShape.Arrow, p, h * 0.17f, Fx.Fade(i % 2 == 0 ? Rgb(255, 130, 90) : Rgb(255, 200, 110), env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
                fx.Sprite(FxShape.Spark, p + new Vector3(s * h * 0.1f, -h * 0.1f, 0f), h * 0.07f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)));
            }
        Fx.Aura(fx, c.Age, c.Seed, c.FromFeet, h, 0.05f, 0.95f, red, Rgb(255, 190, 120), 8);
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.25f, h, 0.4f, Rgb(255, 220, 190), 0.4f);
    }

    /// <summary>Electroweb: a net of yellow strands is flung across, opens over the foe and tightens on it, sparks running along its threads.</summary>
    private static void ElectrowebY11b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float fly = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float after = Fx.Envelope(c.Age, 0f, 0.04f, 0.8f, 1.05f);
        if (after <= 0f) return;
        var at = c.Cue.Missed ? c.Aim : Vector3.Lerp(c.From, c.To, Fx.EaseOut(fly));
        float open = Fx.EaseOut(fly);
        float tight = c.Age > I ? 1f - 0.18f * Fx.EaseOut(Math.Clamp((c.Age - I) / 0.3f, 0f, 1f)) : 1f;
        float r = h * (0.25f + 0.55f * open) * tight;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            fx.Ribbon(new[] { at, at + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r, 0.1f) }, h * 0.025f, Fx.Fade(col.Main, after));
        }
        foreach (float f in new[] { 0.4f, 0.7f, 1f }) L2Ellipse(fx, at, r * f, r * f, h * 0.022f, Fx.Fade(col.Main, after * 0.9f), 8, 0.2f);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age * 3f + i * 0.37f) % 1f;
            float a = i * MathF.Tau / 6f + 0.2f;
            fx.Sprite(FxShape.Spark, at + new Vector3(MathF.Cos(a) * r * k, MathF.Sin(a) * r * k, 0.2f), h * 0.1f, Fx.Fade(White, after * MathF.Sin(k * MathF.PI)));
        }
        fx.Sprite(FxShape.Glow, at, r * 1.2f, Fx.Fade(col.Light, after * 0.18f), FxBlend.Alpha);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Spark, White, col.Main, 0.1f, 0.8f, 0f, salt: 261);
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Wild Charge: the user is wrapped in jagged lightning and rams across in a streak of electricity, shedding sparks back on itself.</summary>
    private static void WildChargeY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float cloak = Fx.Envelope(c.Age, 0f, 0.08f, I - 0.04f, I + 0.2f);
        if (cloak > 0f)
            for (int i = 0; i < 3; i++)
            {
                var a = c.From + new Vector3((i - 1) * h * 0.4f, -h * 0.25f, 0.2f);
                var b = c.From + new Vector3((i - 1) * h * 0.55f + Fx.Signed(c.Seed, i, 262) * h * 0.2f, h * 0.45f, 0.2f);
                Fx.Bolt(fx, c.Age * 1.3f, c.Seed + i * 17, a, b, 0f, 1.5f, h * 0.05f, col.Main, col.Light, 1);
            }
        Fx.Orb(fx, c, 0.08f, I, 0.38f, col.Main, col.Light);
        SpeedLines(c, fx, 0.05f, I, 5, 0.5f);
        if (c.Cue.Landed)
        {
            // The recoil: sparks leap back onto the user
            Fx.Burst(fx, c.Seed, c.Age, c.From, h, I + 0.08f, 0.45f, 8, FxShape.Spark, White, col.Main, 0.09f, 0.9f, 0f, salt: 263);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Drill Run: a spinning cone of grey and brown whirls forward on a trail of dust and grinds into the foe, chips of rock flying.</summary>
    private static void DrillRunY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.15f, I + 0.35f);
        if (fade > 0f)
        {
            var dir = Vector3.Normalize(c.Aim - c.From);
            var head = Vector3.Lerp(c.From, c.Aim, k * k * (3f - 2f * k));
            // The cone: a long grey point and rings of its spiral growing behind it, all turning
            fx.Streak(FxShape.Shard, head + dir * h * 0.15f, dir, h * 0.3f, 2.4f, Fx.Fade(Rgb(206, 200, 192), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Shard, head + dir * h * 0.2f, dir, h * 0.16f, 2.4f, Fx.Fade(White, fade * 0.9f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
            {
                float s = h * (0.3f + 0.1f * i);
                var p = head - dir * h * 0.32f * i;
                float ang = c.Age * 26f + i * 0.9f;
                fx.Sprite(FxShape.Swirl, p, s, Fx.Fade(i % 2 == 0 ? Rgb(176, 166, 154) : Rgb(120, 96, 72), fade), FxBlend.Alpha, ang);
            }
            fx.Sprite(FxShape.Glow, head, h * 0.7f, Fx.Fade(Rgb(214, 190, 150), fade * 0.4f), FxBlend.Alpha);
            if (k < 1f) Fx.Burst(fx, c.Seed, c.Age, c.FromFeet, h, 0.04f, 0.4f, 6, FxShape.Smoke, Rgb(206, 180, 140, 190), Rgb(170, 146, 112, 190), 0.12f, 0.5f, -0.2f, FxBlend.Alpha, salt: 264);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 11, FxShape.Rock, Rgb(176, 150, 108), Rgb(120, 98, 68), 0.1f, 1.1f, 1.2f, FxBlend.Alpha, spin: 7f, salt: 265);
            float cr = (c.Age - I) / 0.5f;
            if (cr > 0f && cr < 1f) fx.Flat(FxShape.Crack, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 1.2f * Fx.EaseOut(cr * 3f), Fx.Fade(Rgb(90, 70, 50), 1f - cr), FxBlend.Alpha, 0.5f);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Dual Chop: two violet blades of force chop across the foe one after the other from opposite corners, crossing in an X.</summary>
    private static void DualChopY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Crescent(fx, c.Age, c.To + new Vector3(-h * 0.7f, h * 0.7f, 0.2f), c.To + new Vector3(h * 0.35f, -h * 0.3f, 0.2f), 0.08f, 0.2f, h * 0.8f, h * 1.2f, col.Main, col.Light);
        Crescent(fx, c.Age, c.To + new Vector3(h * 0.7f, h * 0.7f, 0.2f), c.To + new Vector3(-h * 0.35f, -h * 0.3f, 0.2f), I - 0.17f, 0.2f, h * 0.8f, h * 1.2f, Rgb(196, 176, 255), White);
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed + 5, c.To, h, I - 0.2f, 0.26f, col.Main, col.Light);
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.02f, 2, 0.1f, col.Light, White);
            Jolt(fx, c.Age, I, 0.2f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Heart Stamp: a big pink heart is stamped down on the foe from above and leaves its print a moment, little hearts jumping off it.</summary>
    private static void HeartStampY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Heart, 0.7f, 0f, I, 0.75f, col.Main, 0f);
        if (c.Cue.Landed)
        {
            float print = Fx.Envelope(c.Age, I, I + 0.04f, I + 0.45f, I + 0.7f);
            fx.Sprite(FxShape.Heart, c.To, h * 0.55f, Fx.Fade(col.Light, print * 0.8f), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, c.To, h * 0.62f, Fx.Fade(White, print * 0.7f), FxBlend.Alpha);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 8, FxShape.Heart, col.Light, White, 0.1f, 1.0f, -0.4f, FxBlend.Alpha, salt: 266);
            Jolt(fx, c.Age, I, 0.2f);
            L2Flinch(c, fx, I + 0.12f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Horn Leech: a green horn spears the foe, and a stream of pale-green life is drawn out of it across to the user, which glows.</summary>
    private static void HornLeechY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Strike(c, fx, FxShape.Glow, new Vector3(-1.1f, 0.25f, 0.2f), I, 0.8f, Rgb(80, 170, 60), 0f, 0.18f);
        Strike(c, fx, FxShape.Horn, new Vector3(-1.1f, 0.25f, 0.2f), I, 0.8f, Rgb(250, 250, 226), 0.7f, 0.18f);
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I, I + 0.7f, 0.35f, 8, FxShape.Glow, Rgb(190, 255, 150), Rgb(120, 220, 110), 0.15f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha, reverse: true);
            Fx.Stream(fx, c, I, I + 0.7f, 0.35f, 8, FxShape.Leaf, col.Main, col.Light, 0.1f, arc: 0.06f, spread: 0.3f, spin: 4f, blend: FxBlend.Alpha, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Sacred Sword: a pale gold blade of light cuts the foe in two crossing strokes and the barrier of its boosts breaks in blue shards.</summary>
    private static void SacredSwordY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var gold = Rgb(255, 230, 150);
        float barrier = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.02f, I + 0.1f);
        if (barrier > 0f) fx.Sprite(FxShape.Hex, c.To, h * 0.95f, Fx.Fade(Rgb(120, 170, 255), barrier * 0.5f), FxBlend.Alpha);
        Strike(c, fx, FxShape.Sword, new Vector3(-0.7f, 1.0f, 0.2f), I - 0.1f, 0.85f, gold, -0.6f, 0.16f);
        Strike(c, fx, FxShape.Sword, new Vector3(0.7f, 1.0f, 0.2f), I, 0.85f, White, 0.6f, 0.14f);
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.02f, 2, 0.1f, gold, White);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Shard, Rgb(150, 190, 255), Rgb(220, 236, 255), 0.11f, 1.1f, 0.8f, FxBlend.Alpha, spin: 6f, salt: 267);
            Fx.Glint(fx, c.Age, c.To, h, I, gold, 0.5f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Razor Shell: a pearl-white scallop edge is swung twice across the foe, each stroke a sheet of blue water, droplets thrown.</summary>
    private static void RazorShellY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var pearl = Rgb(246, 240, 232);
        Crescent(fx, c.Age, c.To + new Vector3(h * 0.7f, h * 0.65f, 0.2f), c.To + new Vector3(-h * 0.4f, -h * 0.3f, 0.2f), 0.08f, 0.26f, h * 0.9f, h * 1.4f, pearl, col.Light);
        Crescent(fx, c.Age, c.To + new Vector3(-h * 0.7f, h * 0.55f, 0.2f), c.To + new Vector3(h * 0.4f, -h * 0.4f, 0.2f), 0.2f, 0.2f, h * 0.8f, h * 1.2f, Rgb(190, 226, 255), White);
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age - 0.08f - i * 0.05f) / 0.3f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Drop, Vector3.Lerp(c.To + new Vector3(h * 0.8f, h * 0.7f, 0.2f), c.To + new Vector3(-h * 0.7f, -h * 0.4f, 0.2f), k) + Vector3.UnitY * h * 0.1f * i, h * 0.1f, Fx.Fade(col.Main, MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.03f, 2, 0.09f, pearl, White, FxShape.Wave);
            Splash(c, fx, c.To, h, I, col.Main, col.Light, 8, 6);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Heat Crash: the user drops on the foe wrapped in fire like a falling sun, the ground ringing out in flame, a heavy jolt and a ring of burning dust.</summary>
    private static void HeatCrashY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Glow, 0.75f, 0f, I, 1.5f, Fx.Fade(col.Main, 0.7f), 0f);
        DropOn(c, fx, FxShape.Flame, 0.75f, 0f, I, 0.9f, col.Light, MathF.PI);
        DropOn(c, fx, FxShape.Flame, 0.75f, 0f, I, 0.65f, col.Main, MathF.PI);
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.5f, col.Main);
            GroundRing(fx, c.Age, c.ToFeet, I + 0.08f, 0.5f, h * 1.0f, col.Light);
            for (int s = -1; s <= 1; s += 2)
                Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + new Vector3(s * h * 0.5f, h * 0.1f, 0.1f), h, I, 0.55f, 5, FxShape.Flame, col.Main, col.Dark, 0.18f, 0.9f, -0.3f, salt: 268 + s);
            Dust(c, fx, c.ToFeet, h, I, 7);
            // The ground burns on a while after
            float burn = Fx.Envelope(c.Age, I, I + 0.1f, 0.85f, 1.1f);
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 1.3f + i / 6f) % 1f;
                float a = i * MathF.Tau / 6f;
                fx.Sprite(FxShape.Flame, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.8f, h * (0.12f + 0.2f * u), MathF.Sin(a) * h * 0.4f), h * 0.2f * (1f - 0.4f * u), Fx.Fade(i % 2 == 0 ? col.Light : col.Main, burn * MathF.Sin(u * MathF.PI)), FxBlend.Add);
            }
            Jolt(fx, c.Age, I, 0.55f, 0.24f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Leaf Tornado: a funnel of green leaves spins across, widening, then whirls round the foe in a column with a dull brown haze that spoils its aim.</summary>
    private static void LeafTornadoY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I + 0.05f)
        {
            var head = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            for (int i = 0; i < 9; i++)
            {
                float a = i * 1.2f + c.Age * 16f;
                float up = (i / 9f);
                fx.Sprite(FxShape.Leaf, head + new Vector3(MathF.Cos(a) * h * 0.2f * (1f + up * 2.5f), (up - 0.4f) * h * 1.1f, MathF.Sin(a) * 0.1f), h * 0.2f, Fx.Fade(i % 2 == 0 ? col.Main : col.Light, Math.Min(1f, k * 5f)), FxBlend.Alpha, a);
            }
        }
        if (c.Cue.Landed)
        {
            Round(c, fx, FxShape.Leaf, 12, col.Main, col.Light, 0.5f, 0.14f, 9f, FxBlend.Alpha, 0.95f);
            Haze(fx, c.Age, c.Seed, c.To, h, I, 1.1f, 5, Rgb(160, 150, 96, 190), 0.3f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Steamroller: the user curls into a spiked ball and rolls across the ground, dust in its wake, flattening the foe in a ring and a start.</summary>
    private static void SteamrollerY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.1f, I + 0.35f);
        float s = c.FromSize * 0.62f;
        if (fade > 0f)
        {
            float run = k * (2f - k) * 0.5f + k * 0.5f;
            var p = Vector3.Lerp(c.FromFeet, c.Cue.Missed ? c.Aim - Vector3.UnitY * c.ToSize * 0.5f : c.ToFeet, run) + new Vector3(0f, s * 0.8f, 0.15f);
            float roll = c.Age * 26f;
            fx.Sprite(FxShape.Orb, p, s, Fx.Fade(col.Dark, fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Star, p, s * 1.25f, Fx.Fade(col.Main, fade), FxBlend.Alpha, roll);
            fx.Sprite(FxShape.Swirl, p, s * 0.85f, Fx.Fade(col.Light, fade * 0.8f), FxBlend.Alpha, -roll * 0.6f);
            for (int i = 0; i < 5; i++)
            {
                float u = Math.Max(0f, k - 0.07f * (i + 1));
                var back = Vector3.Lerp(c.FromFeet, c.ToFeet, u * (2f - u) * 0.5f + u * 0.5f) + new Vector3(0f, s * (0.3f + 0.1f * i), 0.1f);
                fx.Sprite(FxShape.Smoke, back, s * (0.5f + 0.1f * i), Fx.Fade(Rgb(214, 198, 160, 200), fade * (0.7f - 0.12f * i) * Math.Min(1f, k * 4f)), FxBlend.Alpha, i * 1.3f);
            }
        }
        if (c.Cue.Landed)
        {
            GroundRing(fx, c.Age, c.ToFeet, I, 0.4f, h * 1.1f, col.Light);
            Fx.Burst(fx, c.Seed, c.Age, c.ToFeet + Vector3.UnitY * h * 0.1f, h, I, 0.5f, 7, FxShape.Leaf, col.Main, col.Light, 0.1f, 0.9f, 0.6f, FxBlend.Alpha, spin: 5f, salt: 269);
            Jolt(fx, c.Age, I, 0.3f);
            L2Flinch(c, fx, I + 0.1f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Cotton Guard: round white cotton puffs bloom from the user until it is wrapped in a soft, thick coat, a faint green gleam of defence on it.</summary>
    private static void CottonGuardY11b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.5f, 0f, 1f));
        for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + ring * 0.4f;
                var p = c.From + new Vector3(MathF.Cos(a) * h * 0.55f * (0.6f + 0.4f * grow), MathF.Sin(a) * h * 0.55f * (0.6f + 0.4f * grow) + (ring - 0.5f) * h * 0.1f, 0.15f + ring * 0.1f);
                fx.Sprite(FxShape.Smoke, p, h * (0.2f + 0.12f * grow), Fx.Fade(Rgb(250, 250, 244, 235), env * (ring == 0 ? 0.8f : 0.95f)), FxBlend.Alpha, i * 1.7f + ring);
            }
        fx.Sprite(FxShape.Glow, c.From, h * 1.15f, Fx.Fade(Rgb(190, 240, 170), env * 0.28f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float u = (c.Age * 1.1f + i / 4f) % 1f;
            fx.Sprite(FxShape.Spark, c.From + new Vector3(Fx.Signed(c.Seed, i, 270) * h * 0.55f, (u - 0.4f) * h * 0.8f, 0.3f), h * 0.07f, Fx.Fade(White, env * MathF.Sin(u * MathF.PI)));
        }
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * h * 0.25f, h, 0.45f, Rgb(214, 255, 190), 0.4f);
    }

    /// <summary>Night Daze: rings of black shock roll out from the user and burst on the foe in a cloud of dark that blots out its sight, white flecks winking in it.</summary>
    private static void NightDazeY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        // Rings of dark, alpha-blended (an added ring would come out pale), rolling across and growing
        float flight = Math.Clamp((c.Age - 0.02f) / (I + 0.1f), 0f, 1f);
        float ringsOn = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.1f, I + 0.35f);
        for (int i = 0; i < 4 && ringsOn > 0f; i++)
        {
            float u = Math.Clamp(flight - 0.14f * i, 0f, 1f);
            if (u <= 0f) continue;
            float s = c.FromSize * (0.4f + 1.1f * u);
            var p = Vector3.Lerp(c.From, c.Aim, u);
            fx.Sprite(FxShape.Ring, p, s, Fx.Fade(i % 2 == 0 ? Rgb(120, 60, 170) : Rgb(30, 14, 50), ringsOn * (1f - 0.5f * u)), FxBlend.Alpha);
            fx.Sprite(FxShape.Ring, p, s * 0.88f, Fx.Fade(Rgb(70, 36, 110), ringsOn * 0.7f * (1f - 0.5f * u)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 9, FxShape.Smoke, Rgb(40, 22, 56, 235), Rgb(80, 50, 110, 220), 0.3f, 0.55f, -0.1f, FxBlend.Alpha, salt: 271);
            Tint(c, fx, I, 1.1f, Rgb(34, 18, 50), 0.75f);
            for (int i = 0; i < 5; i++)
            {
                float k = (c.Age - I - i * 0.07f) / 0.4f;
                if (k < 0f || k > 1f) continue;
                fx.Sprite(FxShape.Star, c.To + new Vector3(Fx.Signed(c.Seed, i, 272) * h * 0.4f, Fx.Signed(c.Seed, i, 273) * h * 0.3f, 0.2f), h * 0.07f, Fx.Fade(White, MathF.Sin(k * MathF.PI)), FxBlend.Alpha, k * 3f);
            }
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Psystrike: one long glassy lance of pink and white is thrown through the foe, ripples running back down its length, and crystals burst out of the hit.</summary>
    private static void PsystrikeY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        float fade = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.1f, I + 0.4f);
        if (fade > 0f)
        {
            var dir = Vector3.Normalize(c.Aim - c.From);
            var head = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Streak(FxShape.Shard, head - dir * h * 0.4f, dir, h * 0.13f, 6f, Fx.Fade(col.Light, fade), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, head - dir * h * 0.6f, dir, h * 0.06f, 12f, Fx.Fade(White, fade));
            fx.Sprite(FxShape.Glow, head, h * 0.5f, Fx.Fade(col.Main, fade * 0.6f), FxBlend.Alpha);
            for (int i = 0; i < 3; i++)
            {
                float u = Math.Max(0f, k - 0.1f * (i + 1));
                fx.Sprite(FxShape.Ring, Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(u)), h * (0.18f + 0.06f * i), Fx.Fade(col.Main, fade * 0.7f), FxBlend.Alpha);
            }
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Shard, col.Light, Rgb(200, 220, 255), 0.13f, 1.3f, 1.0f, FxBlend.Alpha, spin: 5f, salt: 274);
            fx.Sprite(FxShape.Hex, c.To, h * 0.8f * Fx.Envelope(c.Age, I, I + 0.04f, I + 0.1f, I + 0.4f), Fx.Fade(Rgb(200, 220, 255), 0.5f), FxBlend.Alpha);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(col.Light, 0.9f));
            Jolt(fx, c.Age, I, 0.25f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Tail Slap: four quick swats of a tail, each from the other side and each a small flash, the last a little harder.</summary>
    private static void TailSlapY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 4; i++)
        {
            float t = I - 0.24f + i * 0.08f;
            float side = i % 2 == 0 ? 1f : -1f;
            Crescent(fx, c.Age, c.To + new Vector3(side * h * 0.8f, h * 0.35f, 0.2f), c.To + new Vector3(-side * h * 0.5f, -h * 0.1f, 0.2f), t - 0.08f, 0.1f, h * 0.3f, h * 0.45f, Rgb(250, 240, 214), White);
            if (c.Cue.Landed || !c.Cue.Missed) Fx.Contact(fx, c.Age, c.Seed + i, c.To + new Vector3(0f, h * 0.05f * i, 0f), h, t, 0.16f + 0.05f * (i == 3 ? 1 : 0), col.Main, col.Light);
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.2f);
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Hurricane: a pale cyclone of wind and feathers boils up round the foe, streaks of air in it, and the foe sees stars.</summary>
    private static void HurricaneY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I)
            for (int i = 0; i < 6; i++)
            {
                float a = c.Age * 14f + i * 1.05f;
                var head = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
                fx.Sprite(FxShape.Swirl, head + new Vector3(MathF.Cos(a) * h * 0.3f, MathF.Sin(a) * h * 0.3f, 0.1f), h * 0.3f, Fx.Fade(Rgb(214, 232, 255), Math.Min(1f, k * 4f)), FxBlend.Alpha, a);
            }
        if (c.Cue.Landed)
        {
            Vortex(c, fx, Rgb(190, 214, 255), White, 7f);
            Round(c, fx, FxShape.Feather, 7, White, Rgb(220, 236, 255), 0.65f, 0.14f, 8f, FxBlend.Alpha, 1.0f);
            Round(c, fx, FxShape.Streak, 8, White, col.Light, 0.75f, 0.12f, -11f, FxBlend.Add, 0.9f);
            Dizzy(c, fx, I + 0.1f);
        }
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Head Charge: a blunt white horn of force leads the user's rush, speed lines trailing, and on the hit the user's own head is jarred back.</summary>
    private static void HeadChargeY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Orb(fx, c, 0.02f, I, 0.42f, Rgb(250, 236, 200), White, FxShape.Glow, Rgb(255, 250, 236));
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        if (c.Age < I + 0.05f)
        {
            var head = Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(k));
            fx.Sprite(FxShape.Horn, head, h * 0.42f, Fx.Fade(Rgb(255, 244, 214), Math.Min(1f, k * 5f)), FxBlend.Alpha, ScreenAngle(c.Aim - c.From) - MathF.PI / 2f);
        }
        SpeedLines(c, fx, 0.04f, I, 6, 0.55f);
        if (c.Cue.Landed)
        {
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.3f, Fx.Fade(White, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.From, c.FromSize, I + 0.1f, 0.4f, 6, FxShape.Star, Rgb(255, 236, 140), White, 0.09f, 0.7f, 0f, FxBlend.Alpha, spin: 5f, salt: 275);
            Jolt(fx, c.Age, I, 0.4f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Gear Grind: two toothed steel gears are flung in turn, each biting into the foe with a shriek of sparks.</summary>
    private static void GearGrindY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int g = 0; g < 2; g++)
        {
            float land = I - 0.14f + g * 0.14f;
            float k = Math.Clamp((c.Age - (land - 0.2f)) / 0.2f, 0f, 1f);
            if (c.Age < land - 0.2f || c.Age > land + 0.25f) continue;
            float fade = Fx.Envelope(c.Age, land - 0.2f, land - 0.15f, land + 0.08f, land + 0.25f);
            var p = Vector3.Lerp(c.From + new Vector3(0f, g * c.FromSize * 0.2f, 0f), c.Aim + new Vector3((g - 0.5f) * h * 0.2f, 0f, 0f), Fx.EaseOut(k));
            float ang = c.Age * (g == 0 ? 20f : -20f);
            float s = h * 0.36f;
            fx.Sprite(FxShape.Star, p, s * 1.2f, Fx.Fade(Rgb(150, 160, 180), fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Hex, p, s * 0.85f, Fx.Fade(col.Main, fade), FxBlend.Alpha, ang);
            fx.Sprite(FxShape.Ring, p, s * 0.45f, Fx.Fade(Rgb(90, 100, 120), fade), FxBlend.Alpha);
            if (k >= 1f && !c.Cue.Missed)
            {
                Fx.Contact(fx, c.Age, c.Seed + g, c.To, h, land, 0.22f, Rgb(255, 214, 120), White);
                Fx.Burst(fx, c.Seed, c.Age, c.To, h, land, 0.35f, 7, FxShape.Spark, Rgb(255, 230, 150), White, 0.08f, 1.1f, 0.5f, salt: 276 + g, streak: 1.2f);
            }
        }
        if (c.Cue.Landed) Jolt(fx, c.Age, I, 0.3f);
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Searing Shot: a fan of red-hot fireballs is thrown out in arcs and rains round the foe and the Pokémon beside it, leaving cinders.</summary>
    private static void SearingShotY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 7; i++)
        {
            float leave = 0.02f + i * 0.04f;
            float k = (c.Age - leave) / 0.3f;
            if (k < 0f || k > 1f) continue;
            float spread = (i - 3) * 0.5f;
            var end = c.Aim + new Vector3(spread * h * 0.5f, Fx.Signed(c.Seed, i, 277) * h * 0.3f, 0f);
            var p = Lob(c.From, end, k, 0.05f);
            fx.Sprite(FxShape.Glow, p, h * 0.3f, Fx.Fade(col.Main, 0.5f * Math.Min(1f, (1f - k) * 6f)), FxBlend.Alpha);
            fx.Sprite(FxShape.Flame, p, h * 0.16f, Fx.Fade(i % 2 == 0 ? col.Light : col.Main, Math.Min(1f, (1f - k) * 6f)), FxBlend.Add, -MathF.PI / 2f + k);
            fx.Sprite(FxShape.Ember, Lob(c.From, end, Math.Max(0f, k - 0.08f), 0.05f), h * 0.07f, Fx.Fade(col.Light, 0.8f * (1f - k)));
        }
        if (c.Cue.Landed)
        {
            for (int i = 0; i < 5; i++)
                Fx.Burst(fx, c.Seed, c.Age, c.To + new Vector3((i - 2) * h * 0.4f, Fx.Signed(c.Seed, i, 278) * h * 0.25f, 0.1f), h * 0.6f, I + i * 0.02f, 0.45f, 3, FxShape.Ember, col.Light, col.Main, 0.1f, 0.8f, -0.4f, salt: 279 + i);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.2f, col.Main);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Techno Blast: a narrow cyan-white beam built of square pixels in stepped rows, a grid of data blocks lighting up on the foe and winking out.</summary>
    private static void TechnoBlastY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var cyan = Rgb(96, 232, 255);
        Fx.Beam(fx, c, 0.06f, 0.55f, 0.1f, cyan, White, 4);
        float env = Fx.Envelope(c.Age, 0.06f, 0.12f, 0.5f, 0.6f);
        for (int i = 0; i < 12 && env > 0f; i++)
        {
            float u = ((c.Age * 2.2f + i * 0.31f) % 1f);
            var p = Vector3.Lerp(c.From, c.Aim, u);
            // Stepped to a grid, so it reads as pixels
            p.Y = MathF.Round(p.Y / (h * 0.12f)) * h * 0.12f + Fx.Signed(c.Seed, i, 280) * h * 0.1f;
            fx.Sprite(FxShape.Dot, p, h * 0.07f, Fx.Fade(i % 3 == 0 ? White : cyan, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, 0f);
        }
        if (c.Cue.Landed)
            for (int gx = -1; gx <= 1; gx++)
                for (int gy = -1; gy <= 1; gy++)
                {
                    float k = (c.Age - I - (gx + gy + 2) * 0.05f) / 0.3f;
                    if (k < 0f || k > 1f) continue;
                    fx.Sprite(FxShape.Dot, c.To + new Vector3(gx * h * 0.3f, gy * h * 0.28f, 0.2f), h * 0.12f, Fx.Fade(cyan, MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
                }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Relic Song: a slow old melody of cream notes drifts across, a runic ring of turning marks opens over the foe, and drowsy Z's float up from it.</summary>
    private static void RelicSongY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.55f, 0.35f, 7, FxShape.Note, Rgb(255, 238, 190), Rgb(190, 232, 220), 0.3f, arc: 0.1f, spread: 0.25f, spin: 0f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.1f, 0.95f, 1.15f);
            fx.Flat(FxShape.Ring, c.ToFeet + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 1.1f, Fx.Fade(Rgb(255, 232, 170), env), FxBlend.Alpha, c.Age * 1.5f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f - c.Age * 1.5f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.95f, h * 0.05f, MathF.Sin(a) * h * 0.55f);
                fx.Sprite(i % 2 == 0 ? FxShape.Cross : FxShape.Star, p, h * 0.13f, Fx.Fade(Rgb(255, 238, 190), env), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
            }
            fx.Sprite(FxShape.Glow, c.To, h * 1.1f, Fx.Fade(Rgb(255, 232, 170), env * 0.2f), FxBlend.Alpha);
            Rising(fx, c.Age, c.Seed, c.ToFeet + new Vector3(h * 0.2f, h * 0.5f, 0f), h, I + 0.15f, 1.15f, 3, FxShape.Zzz, Rgb(200, 220, 255), 0.18f, 0.08f);
        }
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Secret Sword: the user's horn flashes and two great gold crescents of light fly out and cross over the foe, sword shapes spinning in them.</summary>
    private static void SecretSwordY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var gold = Rgb(255, 214, 100);
        Crescent(fx, c.Age, c.From + new Vector3(0f, c.FromSize * 0.3f, 0.1f), c.Aim + new Vector3(0f, h * 0.25f, 0.2f), 0.04f, I - 0.04f, c.FromSize * 0.4f, h * 0.8f, gold, White);
        Crescent(fx, c.Age, c.From + new Vector3(0f, -c.FromSize * 0.1f, 0.1f), c.Aim + new Vector3(0f, -h * 0.25f, 0.2f), 0.1f, I - 0.1f, c.FromSize * 0.3f, h * 0.7f, Rgb(255, 170, 90), Rgb(255, 236, 190));
        float k = Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f);
        if (k > 0f && c.Age < I + 0.1f)
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Sword, Vector3.Lerp(c.From, c.Aim, Fx.EaseOut(Math.Max(0f, k - 0.07f * i))) + Vector3.UnitY * h * (i - 1) * 0.2f, h * 0.2f, Fx.Fade(Rgb(255, 240, 190), 0.8f - 0.2f * i), FxBlend.Alpha, c.Age * 10f + i);
        if (c.Cue.Landed)
        {
            Fx.Slash(fx, c.Age, c.Seed, c.To, h, I - 0.02f, 2, 0.12f, gold, White);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.4f, h * 1.2f, Fx.Fade(gold, 0.9f));
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Glaciate: a wave of freezing wind rolls over the field and the foe's feet are frosted, ice crystals standing up round it.</summary>
    private static void GlaciateY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float gust = Fx.Envelope(c.Age, 0f, 0.06f, I + 0.1f, I + 0.35f);
        for (int i = 0; i < 12 && gust > 0f; i++)
        {
            float u = Math.Clamp((c.Age - 0.02f) / (I + 0.1f) * 1.25f - i * 0.045f, 0f, 1f);
            var p = Vector3.Lerp(c.FromFeet, c.ToFeet, u) + new Vector3(0f, h * (0.12f + 0.4f * Fx.Rand(c.Seed, i, 300)), Fx.Signed(c.Seed, i, 301) * h * 0.3f);
            fx.Sprite(FxShape.Smoke, p, h * (0.28f + 0.1f * Fx.Rand(c.Seed, i, 302)), Fx.Fade(i % 2 == 0 ? Rgb(226, 246, 255, 235) : Rgb(180, 224, 250, 235), gust * 0.8f * Math.Min(1f, u * 6f)), FxBlend.Alpha, i * 1.3f);
        }
        Flurry(c, fx, 0f, 0.55f, 10, 0.1f, 0.7f);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.05f, 0.95f, 1.2f);
            fx.Flat(FxShape.Snowflake, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 1.3f, Fx.Fade(col.Light, env * 0.8f), FxBlend.Alpha, 0.3f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + 0.4f;
                float grow = Fx.EaseOut(Math.Clamp((c.Age - I - i * 0.03f) / 0.2f, 0f, 1f));
                var foot = c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.7f, 0f, MathF.Sin(a) * h * 0.4f);
                fx.Streak(FxShape.Shard, foot + Vector3.UnitY * h * 0.3f * grow, Vector3.UnitY, h * 0.09f, 2.6f * grow + 0.01f, Fx.Fade(i % 2 == 0 ? col.Light : White, env), FxBlend.Alpha);
            }
            Fx.Shockwave(fx, c.Age, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, I, 0.5f, h * 1.4f, Fx.Fade(col.Light, 0.9f));
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Bolt Strike: a huge blue-white bolt falls out of the sky onto the foe in a double flash, blue arcs racing over the ground and a heavy jolt.</summary>
    private static void BoltStrikeY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var blue = Rgb(120, 190, 255);
        var top = c.To + new Vector3(h * 0.5f, h * 3.5f, -h * 0.5f);
        var bottom = c.Cue.Missed ? c.Aim - Vector3.UnitY * h * 0.4f : c.ToFeet;
        Fx.Bolt(fx, c.Age, c.Seed, top, bottom, 0.14f, 0.55f, h * 0.2f, blue, Rgb(240, 250, 255), 3);
        float flash = Fx.Envelope(c.Age, 0.14f, 0.2f, 0.24f, 0.4f);
        fx.ScreenFlash(Rgb(214, 236, 255), 0.22f * flash);
        if (c.Cue.Landed)
        {
            fx.Sprite(FxShape.Glow, c.To, h * 1.7f * Fx.Envelope(c.Age, I - 0.02f, I, I + 0.1f, I + 0.45f), Fx.Fade(blue, 0.7f), FxBlend.Add);
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + 0.6f;
                float k = (c.Age - I) / 0.4f;
                if (k < 0f || k > 1f) continue;
                Fx.Bolt(fx, c.Age, c.Seed + i * 7, c.ToFeet, c.ToFeet + new Vector3(MathF.Cos(a) * h * 1.2f * k, 0.05f, MathF.Sin(a) * h * 0.6f * k), I, I + 0.4f, h * 0.05f, blue, White, 0);
            }
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.6f, blue);
            Jolt(fx, c.Age, I, 0.5f, 0.24f);
        }
        Impact(c, col, fx, 1.3f);
    }

    /// <summary>Blue Flare: a roiling wave of deep blue fire sweeps across and bursts on the foe in a white-hot core ringed in blue, heat rippling away from it.</summary>
    private static void BlueFlareY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var blue = Rgb(70, 120, 255);
        var pale = Rgb(190, 220, 255);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 12, FxShape.Flame, blue, pale, 0.28f, arc: 0.04f, spread: 0.35f, spin: 0f, blend: FxBlend.Add);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 8, FxShape.Glow, Rgb(50, 90, 230), pale, 0.3f, arc: 0.04f, spread: 0.3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I - 0.02f, I + 0.02f, I + 0.15f, I + 0.55f);
            fx.Sprite(FxShape.Glow, c.To, h * 1.8f * env, Fx.Fade(blue, 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, c.To, h * 1.0f * env, Fx.Fade(White, 0.9f));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Flame, blue, pale, 0.22f, 1.0f, -0.8f, salt: 281);
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I, 0.45f, h * 1.6f, Fx.Fade(pale, 0.9f));
            Fx.Shockwave(fx, c.Age, c.To, Vector3.Zero, I + 0.1f, 0.45f, h * 2.1f, Fx.Fade(blue, 0.7f));
            fx.ScreenFlash(Rgb(190, 214, 255), 0.22f * Fx.Envelope(c.Age, I, I + 0.03f, I + 0.06f, I + 0.22f));
            Jolt(fx, c.Age, I, 0.35f);
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Fiery Dance: flames spin round the user in a dancing ring and, as it turns, are thrown across in two fans; red sparks lift off the user.</summary>
    private static void FieryDanceY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float dance = Fx.Envelope(c.Age, 0f, 0.1f, I - 0.05f, I + 0.1f);
        Encircle(fx, c.FromFeet, h * 0.5f, h * 0.75f, 6, 9f, c.Age, c.Seed, FxShape.Flame, col.Main, col.Light, h * 0.26f, FxBlend.Add, dance);
        Fx.Stream(fx, c, 0.15f, 0.55f, 0.28f, 8, FxShape.Flame, col.Main, col.Light, 0.2f, arc: 0.08f, spread: 0.7f, spin: 3f);
        Fx.Stream(fx, c, 0.2f, 0.55f, 0.28f, 6, FxShape.Ember, col.Light, White, 0.09f, arc: 0.12f, spread: 0.9f);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, c.ToSize, I, 0.5f, 7, FxShape.Flame, col.Main, col.Dark, 0.16f, 0.8f, -0.7f, salt: 282);
            // The chance of a rise in the user's Special Attack
            float up = Fx.Envelope(c.Age, I + 0.15f, I + 0.2f, I + 0.5f, I + 0.65f);
            if (up > 0f) fx.Sprite(FxShape.Arrow, c.From + new Vector3(h * 0.55f, h * (0.35f + 0.2f * (c.Age - I)), 0.2f), h * 0.2f, Fx.Fade(Rgb(255, 150, 190), up), FxBlend.Alpha);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Freeze Shock: a great block of ice crackling with yellow lightning drops on the foe, bolts leaping off it, and shatters in sparking shards.</summary>
    private static void FreezeShockY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var yellow = Rgb(255, 232, 70);
        DropOn(c, fx, FxShape.Hex, 0.7f, 0f, I, 1.0f, Rgb(176, 226, 255), 0.3f);
        DropOn(c, fx, FxShape.Snowflake, 0.7f, 0f, I, 0.6f, White, 0f);
        float k = (c.Age - (I - 0.16f)) / 0.16f;
        if (k > 0f && k < 1.3f)
            for (int i = 0; i < 3; i++)
            {
                var block = c.To + new Vector3(0f, 0.7f * h * (1f - Math.Min(1f, k) * Math.Min(1f, k)), 0f);
                var end = block + new Vector3(Fx.Signed(c.Seed, i, 283) * h * 0.55f, Fx.Signed(c.Seed, i, 284) * h * 0.45f, 0.2f);
                Fx.Bolt(fx, c.Age * 1.4f, c.Seed + i * 11, block, end, 0f, 1.4f, h * 0.045f, yellow, White, 0);
            }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 11, FxShape.Shard, col.Light, White, 0.12f, 1.2f, 1.0f, FxBlend.Alpha, spin: 5f, salt: 285);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 7, FxShape.Spark, yellow, White, 0.09f, 1.1f, 0f, salt: 286);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.2f, col.Light);
            Jolt(fx, c.Age, I, 0.35f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Ice Burn: a pale cyan fire, cold to look at, rolls across and stands up round the foe in a flickering column dusted with snow.</summary>
    private static void IceBurnY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var cyan = Rgb(150, 236, 255);
        var white = Rgb(246, 254, 255);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 10, FxShape.Flame, cyan, white, 0.26f, arc: 0.04f, spread: 0.3f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.05f, 0.5f, 0.3f, 8, FxShape.Snowflake, white, col.Light, 0.1f, arc: 0.04f, spread: 0.4f, spin: 5f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Shaft(fx, c.Age, c.ToFeet, I - 0.02f, 0.6f, h * 1.25f, h * 0.4f, Fx.Fade(cyan, 0.7f), white);
            for (int i = 0; i < 6; i++)
            {
                float u = (c.Age * 1.2f + i / 6f) % 1f;
                fx.Sprite(FxShape.Flame, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 287) * h * 0.45f, h * (0.1f + 0.9f * u), 0.1f), h * 0.2f * (1f - 0.4f * u), Fx.Fade(i % 2 == 0 ? cyan : white, MathF.Sin(u * MathF.PI) * Fx.Envelope(c.Age, I, I + 0.1f, 0.85f, 1.1f)), FxBlend.Alpha);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 9, FxShape.Snowflake, white, cyan, 0.1f, 1.0f, -0.3f, FxBlend.Alpha, spin: 4f, salt: 288);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Snarl: the user bares its fangs and a roar of jagged dark waves rolls over the foe, which shrinks under a falling arrow.</summary>
    private static void SnarlY11b(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var wine = Rgb(150, 50, 110);
        float mouth = Fx.Envelope(c.Age, 0f, 0.06f, 0.5f, 0.7f);
        for (int s = -1; s <= 1; s += 2)
            fx.Sprite(FxShape.Fang, c.From + new Vector3(c.FromSize * 0.18f, c.FromSize * (0.15f * s), 0.2f), c.FromSize * 0.12f, Fx.Fade(White, mouth), FxBlend.Alpha, (s < 0 ? 0f : MathF.PI) - MathF.PI / 2f);
        float roar = Fx.Envelope(c.Age, 0f, 0.05f, I + 0.15f, I + 0.4f);
        for (int i = 0; i < 4 && roar > 0f; i++)
        {
            float u = Math.Clamp((c.Age - 0.04f) / (I + 0.1f) - 0.14f * i, 0f, 1f);
            if (u <= 0f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, u);
            fx.Sprite(FxShape.Wave, p, c.FromSize * (0.4f + 0.6f * u), Fx.Fade(i % 2 == 0 ? wine : Rgb(60, 24, 80), roar * (1f - 0.4f * u)), FxBlend.Alpha, ScreenAngle(c.Aim - c.From));
        }
        for (int i = 0; i < 3; i++)
        {
            float k = (c.Age - 0.12f - i * 0.1f) / 0.4f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * (0.5f + 0.35f * k), c.FromSize * (0.3f + 0.22f * i), 0.2f), c.FromSize * 0.14f, Fx.Fade(Rgb(255, 120, 170), MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Smoke, Rgb(60, 30, 80, 220), wine, 0.2f, 0.5f, -0.2f, FxBlend.Alpha, salt: 289);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.05f, 3, Rgb(210, 110, 190));
            Jolt(fx, c.Age, I, 0.15f);
        }
        Impact(c, col, fx, 0.85f);
    }

    /// <summary>Icicle Crash: three long icicles fall one after another onto the foe and burst in snow, the ground frosting where they strike.</summary>
    private static void IcicleCrashY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        for (int i = 0; i < 3; i++)
        {
            float land = I - 0.1f + i * 0.05f;
            float k = (c.Age - (land - 0.26f)) / 0.26f;
            if (k < 0f || c.Age > land + 0.25f) continue;
            float kk = Math.Min(1f, k);
            float fade = c.Age > land ? Math.Max(0f, 1f - (c.Age - land) / 0.25f) : 1f;
            var p = c.To + new Vector3((i - 1) * h * 0.38f, h * 0.9f * (1f - kk * kk), 0.15f);
            fx.Streak(FxShape.Shard, p + Vector3.UnitY * h * 0.25f, Vector3.UnitY, h * 0.16f, 3.2f, Fx.Fade(i == 1 ? White : col.Light, Math.Min(1f, k * 5f) * fade), FxBlend.Alpha);
            if (c.Age > land && !c.Cue.Missed)
                Fx.Burst(fx, c.Seed, c.Age, c.To + new Vector3((i - 1) * h * 0.38f, 0f, 0.1f), h * 0.7f, land, 0.45f, 5, FxShape.Shard, col.Light, White, 0.1f, 1.0f, 0.9f, FxBlend.Alpha, spin: 5f, salt: 290 + i);
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 9, FxShape.Snowflake, White, col.Light, 0.1f, 1.0f, 0.3f, FxBlend.Alpha, spin: 4f, salt: 293);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.3f, col.Light);
            Jolt(fx, c.Age, I, 0.3f);
            L2Flinch(c, fx, I + 0.1f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>V-create: two ribbons of flame form a V over the foe and close on it in a great blast, and three grey arrows sink over the user, its guard let go.</summary>
    private static void VCreateY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f));
        float fade = Fx.Envelope(c.Age, 0.02f, 0.08f, I + 0.1f, I + 0.35f);
        if (fade > 0f)
            for (int s = -1; s <= 1; s += 2)
            {
                var start = c.To + new Vector3(s * h * 1.3f, h * 0.9f, 0.2f);
                const int n = 9;
                var pts = new Vector3[n];
                for (int j = 0; j < n; j++) pts[j] = Vector3.Lerp(start, c.To, k * j / (n - 1));
                fx.Ribbon(pts, h * 0.2f, Fx.Fade(col.Dark, fade * 0.8f), FxBlend.Alpha);
                fx.Ribbon(pts, h * 0.09f, Fx.Fade(col.Main, fade));
                for (int j = 1; j < n; j += 2)
                    fx.Sprite(FxShape.Flame, pts[j], h * 0.17f, Fx.Fade(j % 4 == 1 ? col.Light : col.Main, fade), FxBlend.Add, -s * 0.5f);
            }
        if (c.Cue.Landed)
        {
            Blast(c, fx, c.To, c.ToFeet, h, I, 12, 4, 1.1f);
            Jolt(fx, c.Age, I, 0.5f, 0.24f);
            L2Down(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.15f, 3, Rgb(180, 190, 210));
        }
        Impact(c, col, fx, 1.35f);
    }

    /// <summary>Fusion Flare: a huge orange-white fireball with a trail of smoke falls from the upper left onto the foe, yellow sparks flying in it.</summary>
    private static void FusionFlareY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        var start = c.Aim + new Vector3(-h * 1.3f, h * 0.55f, 0.2f);
        float fade = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.02f, I + 0.2f);
        if (fade > 0f)
        {
            var p = Vector3.Lerp(start, c.Aim, k * k);
            for (int i = 0; i < 5; i++)
            {
                var back = Vector3.Lerp(start, c.Aim, Math.Max(0f, k - 0.09f * (i + 1)) * Math.Max(0f, k - 0.09f * (i + 1)));
                fx.Sprite(FxShape.Smoke, back, h * (0.34f - 0.03f * i), Fx.Fade(Rgb(120, 90, 80, 200), fade * (0.6f - 0.1f * i)), FxBlend.Alpha, i);
            }
            fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(col.Main, fade * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Flame, p, h * 0.62f, Fx.Fade(col.Main, fade), FxBlend.Add, ScreenAngle(c.Aim - start) + MathF.PI / 2f);
            fx.Sprite(FxShape.Flame, p, h * 0.38f, Fx.Fade(White, fade), FxBlend.Add, ScreenAngle(c.Aim - start) + MathF.PI / 2f);
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Spark, p + new Vector3(Fx.Signed(c.Seed, i, 294) * h * 0.3f, Fx.Signed(c.Seed, i, 295) * h * 0.3f, 0.2f), h * 0.08f, Fx.Fade(Rgb(255, 240, 90), fade));
        }
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.7f, 12, FxShape.Flame, col.Main, col.Dark, 0.2f, 1.0f, -0.7f, salt: 296);
            GroundRing(fx, c.Age, c.ToFeet, I, 0.5f, h * 1.4f, col.Main);
            Jolt(fx, c.Age, I, 0.35f);
        }
        Impact(c, col, fx, 1.15f);
    }

    /// <summary>Fusion Bolt: a crackling yellow sphere of electricity with orange flecks drops from the upper right, bolts looping off it, and bursts on the foe in a cross of light.</summary>
    private static void FusionBoltY11b(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Math.Clamp((c.Age - 0.02f) / (I - 0.02f), 0f, 1f);
        var start = c.Aim + new Vector3(h * 1.3f, h * 0.55f, 0.2f);
        float fade = Fx.Envelope(c.Age, 0f, 0.04f, I + 0.02f, I + 0.2f);
        if (fade > 0f)
        {
            var p = Vector3.Lerp(start, c.Aim, k * k);
            fx.Sprite(FxShape.Glow, p, h * 0.9f, Fx.Fade(col.Main, fade * 0.6f), FxBlend.Alpha);
            fx.Sprite(FxShape.Orb, p, h * 0.34f, Fx.Fade(col.Light, fade), FxBlend.Alpha);
            for (int i = 0; i < 3; i++)
            {
                float a = c.Age * 12f + i * 2.1f;
                Fx.Bolt(fx, c.Age * 1.3f, c.Seed + i * 13, p, p + new Vector3(MathF.Cos(a) * h * 0.5f, MathF.Sin(a) * h * 0.5f, 0.2f), 0f, 1.4f, h * 0.04f, col.Main, White, 0);
            }
            for (int i = 0; i < 4; i++)
                fx.Sprite(FxShape.Ember, p + new Vector3(Fx.Signed(c.Seed, i, 297) * h * 0.35f, Fx.Signed(c.Seed, i, 298) * h * 0.35f, 0.2f), h * 0.07f, Fx.Fade(Rgb(255, 150, 70), fade));
        }
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.03f, I + 0.12f, I + 0.4f);
            fx.Sprite(FxShape.Star, c.To, h * 0.65f * env, Fx.Fade(col.Main, env), FxBlend.Alpha, 0f);
            fx.Sprite(FxShape.Star, c.To, h * 0.4f * env, Fx.Fade(White, env * 0.9f), FxBlend.Add, MathF.PI / 4f);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 8, FxShape.Spark, White, col.Main, 0.1f, 1.2f, 0f, salt: 299, streak: 1f);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 1.05f);
    }
}
