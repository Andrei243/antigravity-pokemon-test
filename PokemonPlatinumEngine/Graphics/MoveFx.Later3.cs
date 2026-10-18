using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// Plan 25 - Y12: the later games' moves, Generation 6 in moves.json's order (numbers 560 to 621, Flying Press to
// Hyperspace Fury), each a look of its own. As Y11: no game's animation exists for any of them; the shapes, colours and
// timings are our own, taken from each move's data (type, category, target, effect, description) and the style guide's
// G8 vocabulary. Every recipe lands at ImpactTime, is over by EffectTime and stays under MaxQuads. This file holds the
// first half (560 to 590), MoveFx.Later3b.cs the second (591 to 621).
internal static partial class MoveFx
{
    private static Dictionary<string, FxRecipe> Later3() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Flying Press"] = FlyingPressY12,
        ["Mat Block"] = MatBlockY12,
        ["Belch"] = BelchY12,
        ["Rototiller"] = RototillerY12,
        ["Sticky Web"] = StickyWebY12,
        ["Fell Stinger"] = FellStingerY12,
        ["Phantom Force"] = PhantomForceY12,
        ["Trick-or-Treat"] = TrickOrTreatY12,
        ["Noble Roar"] = NobleRoarY12,
        ["Ion Deluge"] = IonDelugeY12,
        ["Parabolic Charge"] = ParabolicChargeY12,
        ["Forest's Curse"] = ForestsCurseY12,
        ["Petal Blizzard"] = PetalBlizzardY12,
        ["Freeze-Dry"] = FreezeDryY12,
        ["Disarming Voice"] = DisarmingVoiceY12,
        ["Parting Shot"] = PartingShotY12,
        ["Topsy-Turvy"] = TopsyTurvyY12,
        ["Draining Kiss"] = DrainingKissY12,
        ["Crafty Shield"] = CraftyShieldY12,
        ["Flower Shield"] = FlowerShieldY12,
        ["Grassy Terrain"] = GrassyTerrainY12,
        ["Misty Terrain"] = MistyTerrainY12,
        ["Electrify"] = ElectrifyY12,
        ["Play Rough"] = PlayRoughY12,
        ["Fairy Wind"] = FairyWindY12,
        ["Moonblast"] = MoonblastY12,
        ["Boomburst"] = BoomburstY12,
        ["Fairy Lock"] = FairyLockY12,
        ["King's Shield"] = KingsShieldY12,
        ["Play Nice"] = PlayNiceY12,
        ["Confide"] = ConfideY12,
        ["Diamond Storm"] = DiamondStormY12,
        ["Steam Eruption"] = SteamEruptionY12,
        ["Hyperspace Hole"] = HyperspaceHoleY12,
        ["Water Shuriken"] = WaterShurikenY12,
        ["Mystical Fire"] = MysticalFireY12,
        ["Spiky Shield"] = SpikyShieldY12,
        ["Aromatic Mist"] = AromaticMistY12,
        ["Eerie Impulse"] = EerieImpulseY12,
        ["Venom Drench"] = VenomDrenchY12,
        ["Powder"] = PowderY12,
        ["Geomancy"] = GeomancyY12,
        ["Magnetic Flux"] = MagneticFluxY12,
        ["Happy Hour"] = HappyHourY12,
        ["Electric Terrain"] = ElectricTerrainY12,
        ["Dazzling Gleam"] = DazzlingGleamY12,
        ["Celebrate"] = CelebrateY12,
        ["Hold Hands"] = HoldHandsY12,
        ["Baby-Doll Eyes"] = BabyDollEyesY12,
        ["Nuzzle"] = NuzzleY12,
        ["Hold Back"] = HoldBackY12,
        ["Infestation"] = InfestationY12,
        ["Power-Up Punch"] = PowerUpPunchY12,
        ["Oblivion Wing"] = OblivionWingY12,
        ["Thousand Arrows"] = ThousandArrowsY12,
        ["Thousand Waves"] = ThousandWavesY12,
        ["Land's Wrath"] = LandsWrathY12,
        ["Light of Ruin"] = LightOfRuinY12,
        ["Origin Pulse"] = OriginPulseY12,
        ["Precipice Blades"] = PrecipiceBladesY12,
        ["Dragon Ascent"] = DragonAscentY12,
        ["Hyperspace Fury"] = HyperspaceFuryY12
    };

    // ------------------------------------------------------------------ shared by the batch

    /// <summary>Up arrows rising in front of somebody: a stat that goes up, <paramref name="count"/> of them in turn.</summary>
    private static void L3Up(FxList fx, float age, Vector3 feet, float h, float t0, int count, Color color)
    {
        float env = Fx.Envelope(age, t0, t0 + 0.08f, t0 + 0.5f, t0 + 0.65f);
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float u = (age - t0 - i * 0.07f) / 0.5f;
            if (u < 0f || u > 1f) continue;
            fx.Sprite(FxShape.Arrow, feet + new Vector3((i - (count - 1) * 0.5f) * h * 0.32f, h * (0.35f + 0.5f * u), 0.2f), h * 0.18f, Fx.Fade(color, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    /// <summary>A web lying on the ground round <paramref name="at"/>: spokes and rings drawn on the flat.</summary>
    private static void L3GroundWeb(FxList fx, Vector3 at, float r, float width, Color color, float alpha)
    {
        at.Y += 0.04f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            fx.Ribbon(new[] { at, at + new Vector3(MathF.Cos(a) * r, 0f, MathF.Sin(a) * r * 0.6f) }, width, Fx.Fade(color, alpha), FxBlend.Alpha);
        }
        foreach (float f in new[] { 0.35f, 0.65f, 1f })
        {
            var pts = new Vector3[9];
            for (int i = 0; i <= 8; i++)
            {
                float a = i * MathF.Tau / 8f + 0.2f;
                pts[i] = at + new Vector3(MathF.Cos(a) * r * f, 0f, MathF.Sin(a) * r * f * 0.6f);
            }
            fx.Ribbon(pts, width, Fx.Fade(color, alpha), FxBlend.Alpha);
        }
    }

    /// <summary>A five-petalled flower facing the camera: <paramref name="size"/> is its half-width.</summary>
    private static void L3Flower(FxList fx, Vector3 at, float size, Color petal, Color heart, float alpha, float turn)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = turn + i * MathF.Tau / 5f;
            fx.Sprite(FxShape.Petal, at + new Vector3(MathF.Cos(a) * size * 0.55f, MathF.Sin(a) * size * 0.55f, 0.1f), size * 0.55f, Fx.Fade(petal, alpha), FxBlend.Alpha, a - MathF.PI / 2f);
        }
        fx.Sprite(FxShape.Dot, at + new Vector3(0f, 0f, 0.15f), size * 0.3f, Fx.Fade(heart, alpha), FxBlend.Alpha);
    }

    // ------------------------------------------------------------------ the recipes

    /// <summary>Flying Press: the user leaps and drops on the foe fist first, white wings of feathers spread out either side of it as it lands.</summary>
    private static void FlyingPressY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        DropOn(c, fx, FxShape.Fist, 0.5f, 0f, I, 0.8f, Rgb(255, 214, 160), 0f);
        DropOn(c, fx, FxShape.Glow, 0.5f, 0f, I, 1.1f, Fx.Fade(col.Main, 0.55f), 0f);
        if (c.Cue.Landed)
        {
            float wing = Fx.Envelope(c.Age, I - 0.02f, I + 0.04f, I + 0.2f, I + 0.45f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                    fx.Sprite(FxShape.Feather, c.To + new Vector3(s * h * (0.4f + 0.2f * i), h * (0.5f - 0.08f * i), 0.2f), h * 0.3f, Fx.Fade(i % 2 == 0 ? Rgb(236, 244, 255) : Rgb(176, 206, 255), wing), FxBlend.Alpha, s * (1.2f + 0.2f * i));
            GroundRing(fx, c.Age, c.ToFeet, I, 0.45f, h * 1.3f, col.Light);
            Jolt(fx, c.Age, I, 0.4f);
        }
        Impact(c, col, fx, 1.1f);
    }

    /// <summary>Mat Block: a tatami mat is flipped up on its edge in front of the user's side, three tan panels with a blue edge, dust puffing.</summary>
    private static void MatBlockY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.2f, 0f, 1f));
        for (int i = 0; i < 3; i++)
        {
            var p = c.FromFeet + new Vector3((i - 1) * h * 0.52f, h * (0.12f + 0.55f * rise), 0.2f);
            fx.Streak(FxShape.Hex, p, Vector3.UnitY, h * 0.26f, 1.9f, Fx.Fade(Rgb(220, 192, 124), env), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, p + new Vector3(0f, h * 0.12f, 0.05f), Vector3.UnitX, h * 0.05f, 5.5f, Fx.Fade(Rgb(60, 100, 170), env), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, p - new Vector3(0f, h * 0.12f, -0.05f), Vector3.UnitX, h * 0.05f, 5.5f, Fx.Fade(Rgb(60, 100, 170), env), FxBlend.Alpha);
        }
        float k = (c.Age - 0.25f) / 0.4f;
        if (k > 0f && k < 1f) fx.Sprite(FxShape.Spark, c.FromFeet + new Vector3((-0.6f + 1.2f * k) * h, h * 0.55f, 0.3f), h * 0.2f, Fx.Fade(White, MathF.Sin(k * MathF.PI)));
        Fx.Burst(fx, c.Seed, c.Age, c.FromFeet + new Vector3(0f, h * 0.05f, 0.1f), h, 0.05f, 0.5f, 7, FxShape.Smoke, Rgb(222, 206, 176, 210), Rgb(176, 160, 132, 200), 0.14f, 0.55f, -0.2f, FxBlend.Alpha, salt: 310);
    }

    /// <summary>Belch: a great sour burp of green gas rolls out of the user, bubbles in it, and bursts over the foe in a stinking cloud.</summary>
    private static void BelchY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var sour = Rgb(150, 190, 60);
        Fx.Stream(fx, c, 0f, 0.5f, 0.32f, 11, FxShape.Smoke, sour, Rgb(116, 150, 50), 0.62f, spread: 0.35f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0.05f, 0.5f, 0.3f, 6, FxShape.Bubble, Rgb(220, 240, 140), White, 0.2f, arc: 0.04f, spread: 0.4f, blend: FxBlend.Alpha);
        float mouth = Fx.Envelope(c.Age, 0f, 0.05f, 0.25f, 0.4f);
        fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.6f, 0.2f), c.FromSize * 0.16f, Fx.Fade(Rgb(210, 250, 120), mouth), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.8f, 9, FxShape.Smoke, sour, Rgb(110, 90, 140), 0.5f, 0.6f, -0.15f, FxBlend.Alpha, salt: 311);
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 6, FxShape.Bubble, col.Light, White, 0.1f, 0.8f, -0.2f, FxBlend.Alpha, salt: 312);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Rototiller: brown clods fly as the ground is dug in three long furrows across the field, and green sprouts come up in them.</summary>
    private static void RototillerY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var dir = L1Toward(c);
        var side = new Vector3(-dir.Z, 0f, dir.X);
        float run = Math.Clamp(c.Age / 0.55f, 0f, 1f);
        for (int row = -1; row <= 1; row++)
        {
            var a = mid - dir * h * 2.2f + side * row * h * 0.7f;
            var b = mid + dir * h * 2.2f + side * row * h * 0.7f;
            var end = Vector3.Lerp(a, b, run);
            fx.Ribbon(new[] { a + Vector3.UnitY * 0.04f, end + Vector3.UnitY * 0.04f }, h * 0.1f, Fx.Fade(Rgb(96, 66, 42), env * 0.9f), FxBlend.Alpha);
            for (int i = 0; i < 4; i++)
            {
                float u = (c.Age * 2.4f + i * 0.27f + row * 0.1f) % 1f;
                fx.Sprite(FxShape.Rock, end + new Vector3(Fx.Signed(c.Seed, i + row * 5, 313) * h * 0.2f, h * (0.05f + 0.35f * MathF.Sin(u * MathF.PI)), 0.1f), h * 0.07f, Fx.Fade(Rgb(124, 90, 58), env), FxBlend.Alpha, u * 6f);
            }
            float sprout = Fx.EaseOut(Math.Clamp((c.Age - 0.3f - (row + 1) * 0.06f) / 0.3f, 0f, 1f));
            for (int i = 0; i < 3; i++)
                fx.Sprite(FxShape.Leaf, Vector3.Lerp(a, b, 0.25f + 0.25f * i) + new Vector3(0f, h * 0.12f * sprout, 0.1f), h * 0.1f * sprout, Fx.Fade(col.Main, env), FxBlend.Alpha, i * 0.8f);
        }
    }

    /// <summary>Sticky Web: a pale silk web spreads over the ground at the foe's feet, one strand let down on it from above, the threads glinting.</summary>
    private static void StickyWebY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0f, 0.06f, 0.88f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.3f, 0f, 1f));
        var silk = Rgb(236, 232, 246);
        L3GroundWeb(fx, c.ToFeet, h * 1.7f * grow, h * 0.07f, silk, env);
        fx.Flat(FxShape.Glow, c.ToFeet + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 1.5f * grow, Fx.Fade(Rgb(214, 206, 240), env * 0.35f), FxBlend.Alpha);
        var top = c.ToFeet + new Vector3(h * 0.5f, h * 1.25f, 0f);
        float drop = Fx.EaseOut(Math.Clamp(c.Age / 0.3f, 0f, 1f));
        fx.Ribbon(new[] { top, Vector3.Lerp(top, c.ToFeet + Vector3.UnitY * 0.1f, drop) }, h * 0.03f, Fx.Fade(silk, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Dot, Vector3.Lerp(top, c.ToFeet + Vector3.UnitY * 0.1f, drop), h * 0.06f, Fx.Fade(col.Main, env), FxBlend.Alpha);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + c.Age * 2f;
            fx.Sprite(FxShape.Spark, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.9f * grow, h * 0.05f, MathF.Sin(a) * h * 0.5f * grow), h * 0.08f, Fx.Fade(White, env * (0.5f + 0.5f * MathF.Sin(c.Age * 9f + i))));
        }
    }

    /// <summary>Fell Stinger: a long needle-thin sting is stabbed twice into the foe, a drop of red at its tip, and a flash of triumph over the user.</summary>
    private static void FellStingerY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var dir = Vector3.Normalize(c.Aim - c.From);
        for (int i = 0; i < 2; i++)
        {
            float t0 = I - 0.2f + i * 0.14f;
            float k = Math.Clamp((c.Age - t0) / 0.14f, 0f, 1f);
            float fade = c.Age > t0 + 0.14f ? Math.Max(0f, 1f - (c.Age - t0 - 0.14f) / 0.22f) : 1f;
            if (c.Age < t0) continue;
            var tip = Vector3.Lerp(c.From + new Vector3(0f, i * h * 0.15f, 0f), c.Aim + new Vector3(0f, i * h * 0.15f, 0f), Fx.EaseOut(k));
            fx.Streak(FxShape.Shard, tip - dir * h * 0.8f, dir, h * 0.05f, 9f, Fx.Fade(Rgb(236, 232, 150), fade), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, tip - dir * h * 0.6f, dir, h * 0.025f, 12f, Fx.Fade(White, fade));
            if (k >= 1f && !c.Cue.Missed) fx.Sprite(FxShape.Drop, tip + new Vector3(0f, -h * 0.1f * (c.Age - t0 - 0.14f) * 4f, 0.1f), h * 0.07f, Fx.Fade(Rgb(210, 40, 50), fade), FxBlend.Alpha, MathF.PI);
        }
        if (c.Cue.Landed)
        {
            Fx.Contact(fx, c.Age, c.Seed, c.To, h, I - 0.14f, 0.2f, col.Main, col.Light);
            Fx.Glint(fx, c.Age, c.From + new Vector3(0f, c.FromSize * 0.4f, 0f), c.FromSize, I + 0.15f, Rgb(255, 220, 120), 0.5f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Phantom Force: a violet rift tears open over the foe, ghost fire swirling in it, and a spectral claw drops out of it onto the foe.</summary>
    private static void PhantomForceY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var rift = c.To + new Vector3(0f, h * 0.65f, 0.15f);
        float env = Fx.Envelope(c.Age, 0f, 0.08f, I + 0.1f, I + 0.35f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, rift, h * 1.0f, Fx.Fade(col.Dark, env * 0.8f), FxBlend.Alpha);
            L2Ellipse(fx, rift, h * 0.5f, h * 0.2f, h * 0.05f, Fx.Fade(col.Main, env), 14, c.Age * 2f);
            fx.Sprite(FxShape.Swirl, rift, h * 0.42f, Fx.Fade(col.Light, env * 0.8f), FxBlend.Alpha, c.Age * 8f);
            for (int i = 0; i < 4; i++)
            {
                float u = (c.Age * 1.4f + i / 4f) % 1f;
                fx.Sprite(FxShape.Wisp, rift + new Vector3(Fx.Signed(c.Seed, i, 314) * h * 0.5f, -h * 0.25f * u, 0.1f), h * 0.2f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            }
        }
        DropOn(c, fx, FxShape.Claw, 0.6f, 0f, I, 0.8f, Rgb(214, 190, 255), MathF.PI);
        if (c.Cue.Landed)
        {
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Wisp, col.Light, col.Main, 0.16f, 0.9f, -0.3f, FxBlend.Alpha, salt: 315);
            Jolt(fx, c.Age, I, 0.3f);
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Trick-or-Treat: an orange pumpkin lantern with a toothy grin and a spiral of sweets circle the foe and a ring of ghost-fire rises round it.</summary>
    private static void TrickOrTreatY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.04f, 0.16f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.3f, 0f, 1f));
        var p = Vector3.Lerp(c.From + new Vector3(0f, c.FromSize * 0.3f, 0f), c.To + new Vector3(h * 0.1f, h * 0.55f, 0.3f), k);
        fx.Sprite(FxShape.Glow, p, h * 1.0f, Fx.Fade(Rgb(255, 150, 40), env * 0.5f), FxBlend.Alpha);
        fx.Sprite(FxShape.Orb, p, h * 0.5f, Fx.Fade(Rgb(255, 128, 30), env), FxBlend.Alpha);
        for (int s = -1; s <= 1; s += 2) fx.Sprite(FxShape.Star, p + new Vector3(s * h * 0.15f, h * 0.08f, 0.1f), h * 0.09f, Fx.Fade(Rgb(60, 24, 10), env), FxBlend.Alpha, MathF.PI / 4f);
        fx.Sprite(FxShape.Cross, p + new Vector3(0f, -h * 0.15f, 0.1f), h * 0.18f, Fx.Fade(Rgb(60, 24, 10), env), FxBlend.Alpha, MathF.PI / 4f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + c.Age * 5f;
            var q = c.To + new Vector3(MathF.Cos(a) * h * 0.6f, h * 0.2f + MathF.Sin(a) * h * 0.25f, 0.2f);
            fx.Sprite(i % 2 == 0 ? FxShape.Heart : FxShape.Star, q, h * 0.09f, Fx.Fade(i % 3 == 0 ? Rgb(255, 120, 160) : i % 3 == 1 ? Rgb(255, 220, 90) : Rgb(150, 220, 255), env * k), FxBlend.Alpha, a);
        }
        for (int i = 0; i < 5; i++)
        {
            float u = (c.Age * 1.1f + i / 5f) % 1f;
            float a = i * MathF.Tau / 5f;
            fx.Sprite(FxShape.Wisp, c.ToFeet + new Vector3(MathF.Cos(a) * h * 0.7f, h * (0.1f + 0.7f * u), MathF.Sin(a) * h * 0.4f), h * 0.2f, Fx.Fade(col.Light, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
        }
    }

    /// <summary>Noble Roar: the user rears and a majestic roar of golden-white rings and stars blasts across, the foe cowed under two falling arrows.</summary>
    private static void NobleRoarY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        var gold = Rgb(255, 222, 120);
        Fx.Waves(fx, c, 0f, 0.7f, 4, FxShape.Ring, Rgb(255, 244, 200, 230), 0.6f, 0.3f);
        Fx.Waves(fx, c, 0.05f, 0.7f, 3, FxShape.Wave, gold, 0.5f, 0.3f);
        for (int i = 0; i < 5; i++)
        {
            float k = (c.Age - 0.05f - i * 0.05f) / 0.35f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(FxShape.Star, Vector3.Lerp(c.From, c.Aim, k) + new Vector3(0f, (i - 2) * h * 0.14f, 0.2f), h * 0.1f, Fx.Fade(gold, MathF.Sin(k * MathF.PI)), FxBlend.Add, k * 5f);
        }
        float rear = Fx.Envelope(c.Age, 0f, 0.05f, 0.4f, 0.55f);
        fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.7f, 0.2f), c.FromSize * 0.18f, Fx.Fade(gold, rear), FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            Jolt(fx, c.Age, I, 0.2f);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.05f, 2, Rgb(255, 170, 120));
        }
    }

    /// <summary>Ion Deluge: a cloud of charged air gathers over the whole field and drops a slow rain of yellow sparks, small bolts flickering in it.</summary>
    private static void IonDelugeY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 8; i++)
        {
            float x = (i - 3.5f) * h * 0.5f;
            fx.Sprite(FxShape.Smoke, mid + new Vector3(x, h * (1.4f + 0.12f * MathF.Sin(i * 1.7f)), 0.5f), h * 0.85f, Fx.Fade(i % 2 == 0 ? Rgb(120, 130, 170, 230) : Rgb(150, 150, 190, 230), env * 0.85f), FxBlend.Alpha, i * 1.3f);
        }
        Fx.Rain(fx, c.Age, c.Seed, mid, h, 0.1f, 1.0f, 0.4f, 20, FxShape.Spark, col.Main, col.Light, 0.12f, FxBlend.Add);
        for (int i = 0; i < 2; i++)
        {
            float k = (c.Age * 3f + i * 0.5f) % 1f;
            if (k < 0.5f) Fx.Bolt(fx, c.Age, c.Seed + i * 5 + (int)(c.Age * 6f), mid + new Vector3((i - 0.5f) * h * 1.4f, h * 1.3f, 0f), mid + new Vector3((i - 0.5f) * h * 1.2f, h * 0.45f, 0f), 0f, 1.4f, h * 0.04f, col.Main, White, 0);
        }
        fx.Sprite(FxShape.Glow, mid + Vector3.UnitY * h * 0.5f, h * 2.6f, Fx.Fade(col.Main, env * 0.12f), FxBlend.Alpha);
    }

    /// <summary>Parabolic Charge: arcs of electricity leap from the user in a high parabola onto the foe and a warm yellow glow is drawn back to the user.</summary>
    private static void ParabolicChargeY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var peak = (c.From + c.Aim) * 0.5f + Vector3.UnitY * h * 0.5f;
        for (int i = 0; i < 2; i++)
        {
            float t0 = 0.08f + i * 0.08f;
            Fx.Bolt(fx, c.Age, c.Seed + i * 9, c.From, peak + new Vector3(0f, i * h * 0.1f, 0f), t0, I + 0.1f, h * 0.06f, col.Main, col.Light, 1);
            Fx.Bolt(fx, c.Age, c.Seed + i * 9 + 4, peak + new Vector3(0f, i * h * 0.1f, 0f), c.Aim, t0 + 0.06f, I + 0.16f, h * 0.06f, col.Main, col.Light, 1);
        }
        fx.Sprite(FxShape.Glow, c.From, c.FromSize * 0.7f, Fx.Fade(col.Main, Fx.Envelope(c.Age, 0.02f, 0.1f, 0.3f, 0.45f)));
        if (c.Cue.Landed)
        {
            Fx.Stream(fx, c, I, I + 0.7f, 0.35f, 8, FxShape.Glow, col.Light, Rgb(255, 250, 190), 0.14f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.3f);
        }
        Impact(c, col, fx, 0.95f);
    }

    /// <summary>Forest's Curse: dark teal vines twist up round the foe with glowing eyes in the leaves, a violet curse-mark glowing on its flank.</summary>
    private static void ForestsCurseY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.04f, 0.2f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var teal = Rgb(44, 110, 96);
        for (int i = 0; i < 4; i++)
        {
            const int n = 9;
            var pts = new Vector3[n];
            float root = (i - 1.5f) * h * 0.4f;
            float grow = Fx.EaseOut(Math.Clamp((c.Age - 0.04f - i * 0.04f) / 0.35f, 0f, 1f));
            for (int j = 0; j < n; j++)
            {
                float t = grow * j / (n - 1);
                pts[j] = c.ToFeet + new Vector3(root + MathF.Sin(t * 8f + i * 2f) * h * 0.25f * t, h * 1.0f * t, 0.1f + 0.05f * i);
            }
            fx.Ribbon(pts, h * 0.09f, Fx.Fade(Rgb(26, 62, 58), env), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.035f, Fx.Fade(teal, env));
            fx.Sprite(FxShape.Leaf, pts[n - 1], h * 0.17f, Fx.Fade(Rgb(70, 150, 110), env), FxBlend.Alpha, i * 1.4f);
        }
        float blink = 0.6f + 0.4f * MathF.Sin(c.Age * 7f);
        for (int s = -1; s <= 1; s += 2)
            fx.Sprite(FxShape.Eye, c.ToFeet + new Vector3(s * h * 0.28f, h * 0.7f, 0.25f), h * 0.1f, Fx.Fade(Rgb(210, 120, 255), env * blink), FxBlend.Alpha);
        fx.Sprite(FxShape.Cross, c.To + new Vector3(h * 0.15f, h * 0.05f, 0.25f), h * 0.2f, Fx.Fade(Rgb(190, 100, 255), env * 0.8f), FxBlend.Alpha, MathF.PI / 4f);
        fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(Rgb(120, 50, 170), env * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Petal Blizzard: a gale of pink petals sweeps across the whole field from one side to the other, curling round both Pokémon.</summary>
    private static void PetalBlizzardY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var pink = Rgb(255, 150, 190);
        float env = Fx.Envelope(c.Age, 0f, 0.1f, 0.85f, 1.1f);
        var mid = L1Field(c);
        for (int i = 0; i < 28; i++)
        {
            float u = (c.Age * 1.3f + Fx.Rand(c.Seed, i, 316)) % 1f;
            float y = Fx.Rand(c.Seed, i, 317);
            var p = mid + new Vector3((u - 0.5f) * h * 6f, h * (0.1f + 1.1f * y) + MathF.Sin(u * 12f + i) * h * 0.12f, Fx.Signed(c.Seed, i, 318) * h * 0.9f);
            fx.Sprite(FxShape.Petal, p, h * 0.1f, Fx.Fade(i % 3 == 0 ? Rgb(255, 214, 228) : pink, env * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 14f + i);
        }
        if (c.Cue.Landed)
        {
            Round(c, fx, FxShape.Petal, 8, pink, Rgb(255, 214, 228), 0.65f, 0.12f, 10f, FxBlend.Alpha, 0.9f);
            Jolt(fx, c.Age, I, 0.15f);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Freeze-Dry: a thin white-teal beam of cold, a fine dry vapour rising off the foe as it frosts over, crystals prickling.</summary>
    private static void FreezeDryY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var teal = Rgb(150, 244, 230);
        Fx.Beam(fx, c, 0.06f, 0.6f, 0.09f, teal, White, 6);
        if (c.Cue.Landed)
        {
            float env = Fx.Envelope(c.Age, I, I + 0.06f, 0.9f, 1.15f);
            fx.Sprite(FxShape.Glow, c.To, h * 1.0f, Fx.Fade(teal, env * 0.3f), FxBlend.Alpha);
            for (int i = 0; i < 9; i++)
            {
                float u = (c.Age * 0.9f + i / 9f) % 1f;
                fx.Sprite(FxShape.Wisp, c.ToFeet + new Vector3(Fx.Signed(c.Seed, i, 319) * h * 0.4f, h * (0.3f + 0.9f * u), 0.15f), h * 0.14f, Fx.Fade(White, env * 0.7f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha);
            }
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 10, FxShape.Snowflake, White, teal, 0.07f, 0.9f, 0f, FxBlend.Alpha, spin: 3f, salt: 320);
        }
        Impact(c, col, fx, 0.9f);
    }

    /// <summary>Disarming Voice: soft pink rings of a sweet song roll across with hearts and notes riding them and a gentle sparkle on the foe.</summary>
    private static void DisarmingVoiceY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Waves(fx, c, 0f, 0.7f, 4, FxShape.Ring, Rgb(255, 170, 220, 230), 0.5f, 0.3f);
        for (int i = 0; i < 6; i++)
        {
            float k = (c.Age - i * 0.07f) / 0.45f;
            if (k < 0f || k > 1f) continue;
            fx.Sprite(i % 2 == 0 ? FxShape.Note : FxShape.Heart, Vector3.Lerp(c.From, c.Aim, k) + new Vector3(0f, MathF.Sin(k * 9f + i) * h * 0.2f + h * 0.1f, 0.2f), h * 0.14f, Fx.Fade(i % 2 == 0 ? Rgb(255, 230, 245) : Rgb(255, 130, 190), MathF.Sin(k * MathF.PI)), FxBlend.Alpha);
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Spark, White, col.Main, 0.08f, 0.8f, -0.2f, salt: 321);
        Impact(c, col, fx, 0.8f);
    }

    /// <summary>Parting Shot: the user yells, dark rings hit the foe and drop its stats, and the user itself streaks off to the side in a trail of dark speed.</summary>
    private static void PartingShotY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Waves(fx, c, 0f, 0.55f, 3, FxShape.Wave, Rgb(150, 70, 190), 0.9f, 0.3f);
        Fx.Waves(fx, c, 0.04f, 0.55f, 2, FxShape.Ring, Rgb(60, 24, 90, 230), 0.7f, 0.3f);
        float shout = Fx.Envelope(c.Age, 0f, 0.05f, 0.3f, 0.4f);
        fx.Sprite(FxShape.Exclaim, c.From + new Vector3(c.FromSize * 0.5f, c.FromSize * 0.7f, 0.2f), c.FromSize * 0.17f, Fx.Fade(Rgb(230, 130, 255), shout), FxBlend.Alpha);
        L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I, 2, Rgb(190, 110, 230));
        // Off it goes, to the user's own side and out of the picture
        float k = Math.Clamp((c.Age - 0.45f) / 0.4f, 0f, 1f);
        if (k > 0f)
        {
            for (int i = 0; i < 4; i++)
            {
                float u = Math.Max(0f, k - 0.08f * i);
                fx.Sprite(FxShape.Dot, c.From + new Vector3(-c.FromSize * 2.2f * u * u, c.FromSize * 0.1f * i, 0.2f), c.FromSize * 0.2f * (1f - 0.15f * i), Fx.Fade(col.Main, 0.5f * (1f - k)), FxBlend.Alpha);
                fx.Streak(FxShape.Streak, c.From + new Vector3(-c.FromSize * 2.2f * u * u + c.FromSize * 0.5f, (i - 1.5f) * c.FromSize * 0.2f, 0.2f), Vector3.UnitX, c.FromSize * 0.05f, 7f, Fx.Fade(White, 0.6f * (1f - k)));
            }
        }
    }

    /// <summary>Topsy-Turvy: two curved arrows chase one another round the foe and flip it upside down, a violet swirl under it.</summary>
    private static void TopsyTurvyY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, 0.8f, 1.05f);
        if (env <= 0f) return;
        float turn = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / 0.55f, 0f, 1f)) * MathF.PI;
        for (int s = 0; s < 2; s++)
        {
            float a = turn + s * MathF.PI;
            fx.Sprite(FxShape.Arrow, c.To + new Vector3(MathF.Cos(a) * h * 0.65f, MathF.Sin(a) * h * 0.5f, 0.2f), h * 0.28f, Fx.Fade(s == 0 ? Rgb(190, 130, 255) : Rgb(120, 90, 190), env), FxBlend.Alpha, a);
        }
        fx.Sprite(FxShape.Swirl, c.To, h * 0.9f, Fx.Fade(col.Main, env * 0.55f), FxBlend.Alpha, turn * 2f);
        fx.Sprite(FxShape.Ring, c.To, h * 0.85f, Fx.Fade(col.Light, env * 0.7f), FxBlend.Alpha);
        // The foe's stat marks swap sides: a small up arrow turns over into a down one
        float flip = Fx.EaseOut(Math.Clamp((c.Age - 0.2f) / 0.4f, 0f, 1f));
        fx.Sprite(FxShape.Arrow, c.To + new Vector3(h * 0.7f, h * 0.45f, 0.25f), h * 0.15f, Fx.Fade(Rgb(150, 230, 150), env), FxBlend.Alpha, flip * MathF.PI);
        fx.Sprite(FxShape.Arrow, c.To + new Vector3(h * 0.7f, -h * 0.05f, 0.25f), h * 0.15f, Fx.Fade(Rgb(240, 130, 130), env), FxBlend.Alpha, MathF.PI + flip * MathF.PI);
    }

    /// <summary>Draining Kiss: a pink kiss blown across in a stream of hearts leaves a lip-mark on the foe, and the warm pink glow is drawn back to the user.</summary>
    private static void DrainingKissY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.5f, 0.3f, 6, FxShape.Heart, Rgb(255, 120, 180), Rgb(255, 200, 225), 0.2f, arc: 0.03f, spread: 0.2f, spin: 0f, blend: FxBlend.Alpha);
        if (c.Cue.Landed)
        {
            float mark = Fx.Envelope(c.Age, I, I + 0.05f, I + 0.35f, I + 0.55f);
            fx.Sprite(FxShape.Heart, c.To + new Vector3(0f, h * 0.05f, 0.2f), h * 0.4f * mark, Fx.Fade(Rgb(255, 80, 150), mark * 0.85f), FxBlend.Alpha, 0.2f);
            fx.Sprite(FxShape.Glow, c.To, h * 0.9f, Fx.Fade(col.Main, mark * 0.3f), FxBlend.Alpha);
            Fx.Stream(fx, c, I + 0.1f, I + 0.75f, 0.35f, 7, FxShape.Glow, Rgb(255, 170, 210), White, 0.15f, arc: 0.06f, spread: 0.3f, blend: FxBlend.Alpha, reverse: true);
            L2Heal(fx, c.Age, c.Seed, c.FromFeet, c.FromSize, I + 0.3f);
        }
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Crafty Shield: a dome of small pink-lilac hexagons curves up over the user's side, a glimmering heart in its middle, sparkles running along it.</summary>
    private static void CraftyShieldY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.18f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 9; i++)
        {
            float a = MathF.PI * (i / 8f);
            float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.03f - 0.025f * i) / 0.18f, 0f, 1f));
            var p = c.FromFeet + new Vector3(-MathF.Cos(a) * h * 0.62f, h * (0.1f + 0.8f * MathF.Sin(a)) * rise, 0.2f);
            float shimmer = 0.75f + 0.25f * MathF.Sin(c.Age * 12f + i * 1.3f);
            fx.Sprite(FxShape.Hex, p, h * 0.22f, Fx.Fade(i % 2 == 0 ? Rgb(255, 170, 224) : Rgb(206, 170, 255), env * 0.8f * shimmer), FxBlend.Alpha);
            fx.Sprite(FxShape.Hex, p, h * 0.21f, Fx.Fade(White, env * 0.3f * shimmer));
        }
        fx.Sprite(FxShape.Heart, c.From + new Vector3(0f, h * 0.15f, 0.25f), h * 0.22f, Fx.Fade(Rgb(255, 200, 235), env * (0.7f + 0.3f * MathF.Sin(c.Age * 8f))), FxBlend.Alpha);
        float k = (c.Age - 0.25f) / 0.5f;
        if (k > 0f && k < 1f) fx.Sprite(FxShape.Spark, c.FromFeet + new Vector3(-MathF.Cos(k * MathF.PI) * h * 0.62f, h * (0.1f + 0.8f * MathF.Sin(k * MathF.PI)), 0.3f), h * 0.2f, Fx.Fade(White, MathF.Sin(k * MathF.PI)));
    }

    /// <summary>Flower Shield: a ring of pink blossoms opens round each Pokémon, turning slowly, and each holds a gleam of defence.</summary>
    private static void FlowerShieldY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float env = Fx.Envelope(c.Age, 0.02f, 0.2f, 0.85f, 1.1f);
        if (env <= 0f) return;
        foreach (var (feet, h) in new[] { (c.FromFeet, c.FromSize), (c.Foe - Vector3.UnitY * c.FoeSize * 0.5f, c.FoeSize) })
        {
            float bloom = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / 0.3f, 0f, 1f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + c.Age * 1.2f;
                var p = feet + new Vector3(MathF.Cos(a) * h * 0.75f, h * 0.55f + MathF.Sin(a) * h * 0.4f, 0.2f);
                L3Flower(fx, p, h * 0.2f * bloom, i % 2 == 0 ? Rgb(255, 160, 205) : Rgb(255, 214, 228), Rgb(255, 232, 130), env, a);
            }
            fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 0.5f, h * 1.0f, Fx.Fade(Rgb(255, 170, 214), env * 0.2f), FxBlend.Alpha);
        }
        Fx.Glint(fx, c.Age, c.From + Vector3.UnitY * c.FromSize * 0.3f, c.FromSize, 0.4f, White, 0.4f);
    }

    /// <summary>Grassy Terrain: a lush meadow spreads over the whole field, blades and clover standing up, soft green light settling on the ground.</summary>
    private static void GrassyTerrainY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.15f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float grow = Fx.EaseOut(Math.Clamp(c.Age / 0.5f, 0f, 1f));
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 3.2f * grow, Fx.Fade(Rgb(60, 190, 60), env * 0.9f), FxBlend.Alpha);
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.035f, Vector3.UnitY, h * 2.4f * grow, Fx.Fade(Rgb(190, 250, 120), env * 0.6f));
        fx.Flat(FxShape.Ring, mid + Vector3.UnitY * 0.04f, Vector3.UnitY, h * 3.0f * grow, Fx.Fade(col.Light, env * 0.8f), FxBlend.Alpha);
        for (int i = 0; i < 22; i++)
        {
            float a = Fx.Rand(c.Seed, i, 322) * MathF.Tau;
            float r = h * 2.4f * MathF.Sqrt(Fx.Rand(c.Seed, i, 323)) * grow;
            float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.05f - 0.01f * i) / 0.25f, 0f, 1f));
            var p = mid + new Vector3(MathF.Cos(a) * r, h * 0.12f * rise, MathF.Sin(a) * r * 0.5f);
            fx.Sprite(FxShape.Leaf, p + Vector3.UnitY * h * 0.1f * rise, h * 0.22f * rise, Fx.Fade(i % 3 == 0 ? col.Light : col.Dark, env), FxBlend.Alpha, -0.3f + 0.1f * (i % 5));
            if (i % 5 == 0) fx.Sprite(FxShape.Star, p + Vector3.UnitY * h * 0.3f, h * 0.09f * rise, Fx.Fade(Rgb(255, 240, 150), env), FxBlend.Alpha);
        }
    }

    /// <summary>Misty Terrain: a low lilac-pink mist rolls over the whole field and lies on the ground, sparkles twinkling in it.</summary>
    private static void MistyTerrainY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.2f, 0.85f, 1.1f);
        if (env <= 0f) return;
        for (int i = 0; i < 14; i++)
        {
            float u = (c.Age * 0.3f + Fx.Rand(c.Seed, i, 324)) % 1f;
            var p = mid + new Vector3((u - 0.5f) * h * 5.2f, h * (0.08f + 0.28f * Fx.Rand(c.Seed, i, 325)), Fx.Signed(c.Seed, i, 326) * h * 1.1f);
            fx.Sprite(FxShape.Smoke, p, h * (0.5f + 0.2f * Fx.Rand(c.Seed, i, 327)), Fx.Fade(i % 2 == 0 ? Rgb(255, 196, 232, 235) : Rgb(214, 190, 255, 235), env * 0.8f * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, i * 1.1f + c.Age * 0.3f);
        }
        for (int i = 0; i < 10; i++)
        {
            float k = (c.Age * 1.5f + i * 0.31f) % 1f;
            fx.Sprite(FxShape.Spark, mid + new Vector3(Fx.Signed(c.Seed, i, 328) * h * 2.2f, h * (0.1f + 0.5f * Fx.Rand(c.Seed, i, 329)), Fx.Signed(c.Seed, i, 330) * h * 0.8f), h * 0.07f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
        }
        fx.Flat(FxShape.Glow, mid + Vector3.UnitY * 0.03f, Vector3.UnitY, h * 3.0f, Fx.Fade(Rgb(255, 190, 230), env * 0.3f), FxBlend.Alpha);
    }

    /// <summary>Electrify: a bolt from the user ties on to the foe and a coat of yellow current runs round its whole body in looping arcs.</summary>
    private static void ElectrifyY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        Fx.Bolt(fx, c.Age, c.Seed, c.From, c.Aim, 0.04f, I, h * 0.05f, col.Main, col.Light, 1);
        float env = Fx.Envelope(c.Age, I - 0.05f, I + 0.05f, 0.9f, 1.15f);
        if (env <= 0f) return;
        fx.Sprite(FxShape.Glow, c.To, h * 1.2f, Fx.Fade(col.Main, env * 0.35f), FxBlend.Alpha);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.PI / 2f + c.Age * 7f;
            var p0 = c.To + new Vector3(MathF.Cos(a) * h * 0.6f, MathF.Sin(a) * h * 0.55f, 0.2f);
            var p1 = c.To + new Vector3(MathF.Cos(a + 1.2f) * h * 0.6f, MathF.Sin(a + 1.2f) * h * 0.55f, 0.2f);
            Fx.Bolt(fx, c.Age * 1.5f, c.Seed + i * 3, p0, p1, 0f, 3f, h * 0.035f, col.Main, White, 0);
        }
        for (int i = 0; i < 4; i++)
        {
            float k = (c.Age * 4f + i * 0.31f) % 1f;
            fx.Sprite(FxShape.Spark, c.To + new Vector3(Fx.Signed(c.Seed, i, 331) * h * 0.55f, Fx.Signed(c.Seed, i, 332) * h * 0.5f, 0.3f), h * 0.1f, Fx.Fade(White, env * MathF.Sin(k * MathF.PI)));
        }
    }

    /// <summary>Play Rough: a round cartoon scuffle-cloud of dust forms round the foe, little fists and feet poking out of it, pink stars flying.</summary>
    private static void PlayRoughY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.05f) / (I - 0.05f), 0f, 1f));
        float env = Fx.Envelope(c.Age, 0.05f, 0.12f, 0.8f, 1.05f);
        if (env > 0f)
        {
            var at = c.Cue.Missed ? c.Aim : Vector3.Lerp(c.From, c.To, k);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f + c.Age * 5f;
                fx.Sprite(FxShape.Smoke, at + new Vector3(MathF.Cos(a) * h * 0.4f, MathF.Sin(a) * h * 0.38f, 0.2f), h * 0.3f, Fx.Fade(i % 2 == 0 ? Rgb(250, 244, 246, 240) : Rgb(226, 214, 226, 240), env), FxBlend.Alpha, a);
            }
            for (int i = 0; i < 5; i++)
            {
                float t = (c.Age * 4f + i * 0.37f) % 1f;
                float a = i * 1.7f + c.Age * 3f;
                var q = at + new Vector3(MathF.Cos(a) * h * 0.5f * (0.6f + 0.5f * t), MathF.Sin(a) * h * 0.5f * (0.6f + 0.5f * t), 0.3f);
                fx.Sprite(i % 2 == 0 ? FxShape.Fist : FxShape.Foot, q, h * 0.13f, Fx.Fade(Rgb(255, 214, 222), env * MathF.Sin(t * MathF.PI)), FxBlend.Alpha, a);
            }
            for (int i = 0; i < 4; i++)
            {
                float t = (c.Age * 2.2f + i * 0.3f) % 1f;
                fx.Sprite(FxShape.Star, at + new Vector3(Fx.Signed(c.Seed, i, 333) * h * 0.7f * t, h * (0.3f + 0.5f * t), 0.35f), h * 0.1f, Fx.Fade(Rgb(255, 150, 214), env * MathF.Sin(t * MathF.PI)), FxBlend.Alpha, t * 6f);
            }
        }
        Impact(c, col, fx, 1.0f);
    }

    /// <summary>Fairy Wind: a gentle breeze of pink and lilac sparkles curls across in a long lazy stream, wisps trailing.</summary>
    private static void FairyWindY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        Fx.Stream(fx, c, 0f, 0.55f, 0.35f, 10, FxShape.Wisp, Rgb(255, 190, 230), Rgb(214, 190, 255), 0.22f, arc: 0.08f, spread: 0.4f, spin: 2f, blend: FxBlend.Alpha);
        Fx.Stream(fx, c, 0f, 0.55f, 0.35f, 8, FxShape.Spark, White, col.Light, 0.08f, arc: 0.08f, spread: 0.45f);
        Fx.Stream(fx, c, 0.05f, 0.55f, 0.35f, 4, FxShape.Star, Rgb(255, 230, 250), col.Light, 0.1f, arc: 0.1f, spread: 0.3f, spin: 3f, blend: FxBlend.Alpha);
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.5f, 6, FxShape.Wisp, col.Light, col.Main, 0.14f, 0.6f, -0.2f, FxBlend.Alpha, salt: 334);
        Impact(c, col, fx, 0.7f);
    }

    /// <summary>Moonblast: a pale pink crescent moon rises over the foe, glows, and drops a column of moonlight onto it with a ring of stars.</summary>
    private static void MoonblastY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.ToSize;
        var moon = Rgb(255, 232, 248);
        float env = Fx.Envelope(c.Age, 0f, 0.14f, I + 0.1f, I + 0.4f);
        var at = c.To + new Vector3(h * 0.1f, h * 0.5f, 0.2f);
        if (env > 0f)
        {
            fx.Sprite(FxShape.Glow, at, h * 1.1f, Fx.Fade(col.Main, env * 0.4f), FxBlend.Alpha);
            fx.Sprite(FxShape.Wave, at, h * 0.7f, Fx.Fade(moon, env), FxBlend.Alpha, MathF.PI / 2f + 0.5f);
            fx.Sprite(FxShape.Wave, at + new Vector3(h * 0.05f, 0f, 0.05f), h * 0.74f, Fx.Fade(White, env * 0.4f), FxBlend.Add, MathF.PI / 2f + 0.5f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.26f + c.Age * 2f;
                fx.Sprite(FxShape.Star, at + new Vector3(MathF.Cos(a) * h * 0.55f, MathF.Sin(a) * h * 0.2f, 0.1f), h * 0.07f, Fx.Fade(White, env * (0.5f + 0.5f * MathF.Sin(c.Age * 9f + i))), FxBlend.Alpha, a);
            }
        }
        float drop = Fx.Envelope(c.Age, I - 0.2f, I - 0.12f, I + 0.05f, I + 0.3f);
        if (drop > 0f)
        {
            fx.Streak(FxShape.Beam, c.To + Vector3.UnitY * h * 0.1f, Vector3.UnitY, h * 0.28f, 3.2f, Fx.Fade(col.Light, drop * 0.8f), FxBlend.Alpha);
            fx.Streak(FxShape.Beam, c.To + Vector3.UnitY * h * 0.1f, Vector3.UnitY, h * 0.1f, 8f, Fx.Fade(White, drop));
        }
        if (c.Cue.Landed) Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 8, FxShape.Star, moon, col.Main, 0.1f, 1.0f, 0f, FxBlend.Alpha, spin: 4f, salt: 335);
        Impact(c, col, fx, 1.05f);
    }

    /// <summary>Boomburst: a colossal blast: thick white shock rings and radial streaks slam out of the user in every direction and the whole picture shudders.</summary>
    private static void BoomburstY12(in FxCtx c, FxColors col, FxList fx)
    {
        float h = c.FromSize;
        float charge = Fx.Envelope(c.Age, 0f, 0.05f, I - 0.04f, I);
        fx.Sprite(FxShape.Glow, c.From, h * 0.7f * charge, Fx.Fade(White, charge * 0.7f));
        if (c.Cue.Landed || c.Age >= I)
        {
            for (int i = 0; i < 4; i++)
            {
                Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I + i * 0.06f, 0.45f, h * (2.4f + 0.7f * i), Fx.Fade(i % 2 == 0 ? White : col.Light, 0.9f));
                Fx.Shockwave(fx, c.Age, c.From, Vector3.Zero, I + i * 0.06f, 0.45f, h * (2.2f + 0.7f * i), Fx.Fade(col.Main, 0.6f), FxShape.Wave);
            }
            float k = (c.Age - I) / 0.4f;
            if (k > 0f && k < 1f)
                for (int i = 0; i < 12; i++)
                {
                    float a = i * MathF.Tau / 12f;
                    var d = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
                    fx.Streak(FxShape.Streak, c.From + d * h * (0.6f + 2.2f * Fx.EaseOut(k)), d, h * 0.05f, 6f, Fx.Fade(White, 1f - k));
                }
            Jolt(fx, c.Age, I, 0.7f, 0.3f);
            fx.ScreenFlash(White, 0.2f * Fx.Envelope(c.Age, I, I + 0.02f, I + 0.05f, I + 0.2f));
        }
        Impact(c, col, fx, 1.2f);
    }

    /// <summary>Fairy Lock: a great gold-and-pink padlock closes over the middle of the field and a ring of chain links draws tight round both Pokémon.</summary>
    private static void FairyLockY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = Math.Max(c.FromSize, c.FoeSize);
        var mid = L1Field(c);
        float env = Fx.Envelope(c.Age, 0f, 0.12f, 0.85f, 1.1f);
        if (env <= 0f) return;
        var gold = Rgb(255, 214, 110);
        var lock0 = mid + new Vector3(0f, h * 0.8f, 0.25f);
        float shut = Fx.EaseOut(Math.Clamp((c.Age - 0.2f) / 0.2f, 0f, 1f));
        fx.Sprite(FxShape.Ring, lock0 + new Vector3(0f, h * (0.34f - 0.08f * shut), 0f), h * 0.3f, Fx.Fade(gold, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Hex, lock0, h * 0.5f, Fx.Fade(Rgb(255, 150, 214), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, lock0, h * 0.5f, Fx.Fade(gold, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Dot, lock0, h * 0.1f, Fx.Fade(Rgb(120, 40, 90), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Glow, lock0, h * 0.9f, Fx.Fade(Rgb(255, 190, 230), env * 0.3f), FxBlend.Alpha);
        float tight = 1f - 0.2f * Fx.EaseOut(Math.Clamp((c.Age - 0.2f) / 0.4f, 0f, 1f));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + c.Age * 0.8f;
            var p = mid + new Vector3(MathF.Cos(a) * h * 2.2f * tight, h * 0.05f, MathF.Sin(a) * h * 1.1f * tight);
            fx.Sprite(FxShape.Ring, p, h * 0.2f, Fx.Fade(i % 2 == 0 ? gold : Rgb(255, 170, 220), env), FxBlend.Alpha, a, depth: MathF.Sin(a) < 0f);
        }
    }

    /// <summary>King's Shield: a broad steel shield rimmed in silver stands up before the user with a small crown of three points above it.</summary>
    private static void KingsShieldY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.FromSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.14f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float rise = Fx.EaseOut(Math.Clamp((c.Age - 0.02f) / 0.2f, 0f, 1f));
        var p = c.From + new Vector3(0f, h * 0.05f, 0.3f);
        fx.Sprite(FxShape.Glow, p, h * 1.0f * rise, Fx.Fade(Rgb(190, 214, 255), env * 0.35f), FxBlend.Alpha);
        fx.Sprite(FxShape.Hex, p, h * 0.75f * rise, Fx.Fade(Rgb(120, 134, 160), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Hex, p, h * 0.6f * rise, Fx.Fade(col.Main, env), FxBlend.Alpha);
        fx.Sprite(FxShape.Ring, p, h * 0.75f * rise, Fx.Fade(White, env * 0.8f), FxBlend.Alpha);
        for (int i = -1; i <= 1; i++)
            fx.Sprite(FxShape.Shard, p + new Vector3(i * h * 0.16f, h * (0.9f + (i == 0 ? 0.08f : 0f)) * 0.5f * rise, 0.05f), h * 0.12f * rise, Fx.Fade(Rgb(255, 224, 120), env), FxBlend.Alpha);
        fx.Sprite(FxShape.Cross, p, h * 0.2f * rise, Fx.Fade(White, env * 0.9f), FxBlend.Alpha);
        float k = (c.Age - 0.25f) / 0.4f;
        if (k > 0f && k < 1f) fx.Sprite(FxShape.Spark, p + new Vector3((k - 0.5f) * h * 0.9f, (0.5f - k) * h * 0.5f, 0.1f), h * 0.2f, Fx.Fade(White, MathF.Sin(k * MathF.PI)));
    }

    /// <summary>Play Nice: two open hands reach out from either side and clasp over the foe in a ring of friendly sparkles, its fist-clench falling away.</summary>
    private static void PlayNiceY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.1f, 0.85f, 1.1f);
        if (env <= 0f) return;
        float k = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f));
        for (int s = -1; s <= 1; s += 2)
            fx.Sprite(FxShape.Palm, c.To + new Vector3(s * h * (0.9f - 0.65f * k), h * 0.1f, 0.3f), h * 0.3f, Fx.Fade(Rgb(255, 224, 190), env), FxBlend.Alpha, s * 0.4f);
        if (c.Age > I)
        {
            PulseRing(fx, c.Age, c.To, I, 0.4f, h * 0.2f, h * 0.9f, Rgb(255, 214, 120), Rgb(255, 246, 210));
            Fx.Burst(fx, c.Seed, c.Age, c.To, h, I, 0.6f, 6, FxShape.Heart, Rgb(255, 130, 180), White, 0.09f, 0.8f, -0.3f, FxBlend.Alpha, salt: 336);
            L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.08f, 2, Rgb(255, 170, 110));
        }
    }

    /// <summary>Confide: a small speech bubble with three dots in it drifts over from the user, rings of whisper behind it, and the foe's mind is dimmed.</summary>
    private static void ConfideY12(in FxCtx c, FxColors col, FxList fx)
    {
        fx.Land(I);
        float h = c.ToSize;
        float env = Fx.Envelope(c.Age, 0.02f, 0.12f, I + 0.2f, I + 0.45f);
        if (env > 0f)
        {
            float k = Fx.EaseOut(Math.Clamp((c.Age - 0.04f) / (I - 0.04f), 0f, 1f));
            var p = Vector3.Lerp(c.From + new Vector3(0f, c.FromSize * 0.4f, 0f), c.To + new Vector3(0f, h * 0.55f, 0.3f), k) + new Vector3(0f, 0.05f * MathF.Sin(c.Age * 9f), 0f);
            fx.Sprite(FxShape.Orb, p, h * 0.36f, Fx.Fade(Rgb(236, 232, 255), env * 0.9f), FxBlend.Alpha);
            fx.Sprite(FxShape.Bubble, p, h * 0.48f, Fx.Fade(Rgb(246, 244, 255), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Glow, p, h * 0.7f, Fx.Fade(Rgb(190, 180, 255), env * 0.35f), FxBlend.Alpha);
            for (int i = -1; i <= 1; i++) fx.Sprite(FxShape.Dot, p + new Vector3(i * h * 0.13f, 0f, 0.1f), h * 0.06f, Fx.Fade(Rgb(110, 100, 170), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(-h * 0.24f, -h * 0.3f, 0f), h * 0.06f, Fx.Fade(Rgb(246, 244, 255), env), FxBlend.Alpha);
            fx.Sprite(FxShape.Dot, p + new Vector3(-h * 0.34f, -h * 0.42f, 0f), h * 0.04f, Fx.Fade(Rgb(246, 244, 255), env), FxBlend.Alpha);
        }
        Fx.Waves(fx, c, 0f, 0.6f, 2, FxShape.Ring, Rgb(214, 204, 255, 160), 0.3f, 0.3f);
        if (c.Age > I) L2Down(fx, c.Age, c.Seed, c.ToFeet, h, I + 0.05f, 1, Rgb(150, 150, 255));
        if (c.Age > I) Tint(c, fx, I, 1.0f, Rgb(140, 130, 210), 0.35f);
    }
}
