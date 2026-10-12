using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// A condition that lasts (plan 09 · L2; style guide, "Marks on a Pokémon", "A condition that lasts"): what a Pokémon
// shows for as long as it is asleep, poisoned, burned, paralysed or frozen, confused or in love. Every look is our
// own: between its moments Platinum shows a condition only as the letters on the HP box. Each mark is small and
// slow and keeps to the edge of the body or just over the head, since it is on screen for turns on end; each is a
// pure function of its age, how far it has come in and its seed, and draws no more than its cap. The body's own part
// (shut eyes, the hold, the cast) is the pose's and the shader's: PokePose.Asleep and Frozen, and Cast below.
// Sizes are in the Pokémon's height h; what stands beside its body stands off its middle by its half-width w, so it
// hugs a narrow body and clears a wide one.
internal static partial class MoveFx
{
    /// <summary>The most quads and ribbons a status condition's mark draws at any age (a test holds each to it).</summary>
    public static int ConditionCap(StatusCondition status) => status switch
    {
        StatusCondition.Sleep => 6,
        StatusCondition.Poison => 8,
        StatusCondition.Toxic => 12,
        StatusCondition.Burn => 8,
        StatusCondition.Paralyze => 7,
        StatusCondition.Freeze => 10,
        _ => 0
    };

    /// <summary>The most a confusion's stars and a love's hearts draw.</summary>
    public const int ConfusionCap = 6, LoveCap = 4;

    /// <summary>The most one Pokémon's lasting marks draw once they stand: the heaviest condition, confused and in love at once.</summary>
    public const int ConditionCapOnOne = 12 + ConfusionCap + LoveCap;

    /// <summary>The marks' loops, in seconds: how long a Z, a bubble, a heart is on its way, a star's round, the time between a paralysed Pokémon's fits and how long a fit is.</summary>
    public const float ZzzTime = 2.4f, BubbleTime = 1.9f, ToxicBubbleTime = 1.3f, FlameTime = 0.5f, FitEvery = 1.7f, FitTime = 0.25f,
        GlintEvery = 1.3f, StarRound = 2.4f, HeartTime = 2f;

    /// <summary>How high over its feet the stars circle a confused head, in the Pokémon's height: just clear of it on both platforms.</summary>
    public const float OverTheHead = 1.08f;

    private static readonly Color IceCast = Rgb(150, 214, 255), PoisonCast = Rgb(172, 78, 210), ToxicCast = Rgb(128, 48, 176), ParalysisCast = Rgb(255, 232, 90);

    private static float Loop(float v) => v - MathF.Floor(v);

    // ------------------------------------------------------------------ everyone on the field

    /// <summary>
    /// The marks that last about every Pokémon on the field, as far as each can be seen
    /// (<see cref="BattleAnimator.LastingShown"/>). Nothing at all, and nothing added to the list, while nobody has
    /// a condition.
    /// </summary>
    public static void Conditions(BattleAnimator anim, FxPlaces places, FxList fx)
    {
        if (!anim.Conditions.Any) return;
        for (int s = 0; s < 2; s++)
            for (int slot = 0; slot < Math.Clamp(anim.Slots, 1, 2); slot++)
            {
                var side = (BattleSide)s;
                var look = anim.Conditions.Of(side, slot);
                if (!look.Any) continue;
                float shown = anim.LastingShown(side, slot);
                if (shown <= 0f) continue;
                Condition(look, places.Feet(side, slot), places.Height(side, slot), places.HalfWidth(side, slot), shown, fx);
            }
    }

    /// <summary>
    /// One Pokémon's lasting marks: its condition's (and the one that is going, while it fades), its confusion's
    /// and its love's. <paramref name="shown"/> is how much of them shows, 0..1.
    /// </summary>
    /// <param name="h">The Pokémon's height.</param>
    /// <param name="w">Half its body's width as the battle sees it.</param>
    public static void Condition(ConditionLook look, Vector3 feet, float h, float w, float shown, FxList fx)
    {
        if (shown <= 0f) return;
        if (look.Going.Visible) Ailment(look.GoingStatus, look.Going.Age, look.Going.Strength * shown, look.Going.Seed, feet, h, w, fx);
        if (look.Ailment.Visible) Ailment(look.Status, look.Ailment.Age, look.Ailment.Strength * shown, look.Ailment.Seed, feet, h, w, fx);
        if (look.Confusion.Visible) ConfusionStars(look.Confusion.Age, look.Confusion.Strength * shown, look.Confusion.Seed, feet, h, w, fx);
        if (look.Love.Visible) LoveHearts(look.Love.Age, look.Love.Strength * shown, look.Love.Seed, feet, h, fx);
    }

    // ------------------------------------------------------------------ the six conditions

    /// <summary>A status condition's mark at an age, <paramref name="k"/> of it there (0..1).</summary>
    public static void Ailment(StatusCondition status, float age, float k, int seed, Vector3 feet, float h, float w, FxList fx)
    {
        if (k <= 0.004f) return;
        switch (status)
        {
            case StatusCondition.Sleep: SleepZs(age, k, feet, h, w, fx); break;
            case StatusCondition.Poison: PoisonBubbles(age, k, seed, feet, h, w, fx, badly: false); break;
            case StatusCondition.Toxic: PoisonBubbles(age, k, seed, feet, h, w, fx, badly: true); break;
            case StatusCondition.Burn: BurnFlames(age, k, seed, feet, h, w, fx); break;
            case StatusCondition.Paralyze: ParalysisFits(age, k, seed, feet, h, w, fx); break;
            case StatusCondition.Freeze: IceShell(age, k, seed, feet, h, w, fx); break;
        }
    }

    /// <summary>Asleep: three Z's rising one after another from beside its head, up and to its right, each over its own shadow.</summary>
    private static void SleepZs(float age, float k, Vector3 feet, float h, float w, FxList fx)
    {
        var pale = Rgb(214, 228, 255);
        var shade = Rgb(70, 96, 170);
        for (int i = 0; i < 3; i++)
        {
            // One every third of the loop; none before its own first start
            float start = i * ZzzTime / 3f;
            if (age < start) continue;
            float u = Loop((age - start) / ZzzTime);
            float fade = k * Math.Min(1f, u * 6f) * Math.Min(1f, (1f - u) * 3f);
            // From just outside the ring a confused head's stars keep to, so the two never cross
            var p = feet + new Vector3(HeadRing(h, w) + h * (0.04f + 0.26f * u), h * (1.0f + 0.34f * u), 0f);
            float size = h * (0.06f + 0.05f * u);
            float tilt = 0.22f + 0.08f * MathF.Sin(u * 6f + i);
            // A shadow of its own under each, down and to the right, so a pale letter reads on a pale sky
            fx.Sprite(FxShape.Zzz, p + new Vector3(size * 0.16f, -size * 0.16f, 0f), size, Fx.Fade(shade, fade), FxBlend.Alpha, tilt);
            fx.Sprite(FxShape.Zzz, p, size, Fx.Fade(pale, fade), FxBlend.Alpha, tilt);
        }
    }

    /// <summary>
    /// Poisoned: bubbles rising at the edge of its body, each gone at the top in a small ring. Badly poisoned
    /// deepens it: half as many again, darker, quicker.
    /// </summary>
    private static void PoisonBubbles(float age, float k, int seed, Vector3 feet, float h, float w, FxList fx, bool badly)
    {
        var poison = ColorsOf(PokemonType.Poison);
        var body = badly ? Rgb(104, 36, 150) : Rgb(150, 62, 190);
        var rim = badly ? Rgb(206, 120, 236) : poison.Light;
        int count = badly ? 6 : 4;
        float time = badly ? ToxicBubbleTime : BubbleTime;
        for (int i = 0; i < count; i++)
        {
            float cycle = age / time + Fx.Rand(seed, i, 301);
            float u = Loop(cycle);
            // Each time round a bubble rises somewhere else along the body's edge, so the eye never finds a pattern
            int lap = seed + (int)MathF.Floor(cycle) * 13;
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * w * (0.65f + 0.35f * Fx.Rand(lap, i, 302));
            float z = h * (0.04f + 0.08f * Fx.Signed(lap, i, 303));
            float size = h * (0.07f + 0.03f * Fx.Rand(seed, i, 304));
            const float Top = 0.85f;
            float rise = Math.Min(1f, u / Top);
            var p = feet + new Vector3(x + MathF.Sin(age * 5f + i * 1.7f) * h * 0.025f, h * (0.3f + 0.8f * rise), z);
            if (u < Top)
            {
                float fade = k * Math.Min(1f, u * 8f);
                float grown = size * (0.6f + 0.4f * rise);
                // A purple body under a pale rim and glint: a bubble that is only its rim is lost on leaves
                fx.Sprite(FxShape.Dot, p, grown * 1.3f, Fx.Fade(body, fade * 0.6f), FxBlend.Alpha);
                fx.Sprite(FxShape.Bubble, p, grown, Fx.Fade(rim, fade), FxBlend.Alpha);
                continue;
            }
            float r = (u - Top) / (1f - Top);
            fx.Sprite(FxShape.Ring, p, size * (0.9f + 0.8f * Fx.EaseOut(r)), Fx.Fade(rim, k * (1f - r) * 0.9f), FxBlend.Alpha);
        }
    }

    // Where a burned Pokémon's four flames sit: across in its half-width, up in its height. Its flanks, a shoulder, its head
    private static readonly Vector2[] FlameSpots = { new(-0.95f, 0.42f), new(0.95f, 0.5f), new(-0.62f, 0.8f), new(0.35f, 0.99f) };

    /// <summary>Burned: four small flames on its flanks, a shoulder and its head, each licking up and starting again.</summary>
    private static void BurnFlames(float age, float k, int seed, Vector3 feet, float h, float w, FxList fx)
    {
        var fire = ColorsOf(PokemonType.Fire);
        for (int i = 0; i < FlameSpots.Length; i++)
        {
            float u = Loop(age / FlameTime + Fx.Rand(seed, i, 311));
            float flicker = 0.8f + 0.2f * MathF.Sin(age * 24f + i * 2.1f);
            float size = h * (0.16f + 0.04f * Fx.Rand(seed, i, 312)) * flicker * (1f - 0.3f * u);
            var spot = FlameSpots[i];
            var p = feet + new Vector3(w * (spot.X + 0.05f * Fx.Signed(seed, i, 313)), h * (spot.Y + 0.08f * u), h * 0.05f);
            float fade = k * Math.Min(1f, u * 6f) * (1f - u * u);
            // A body that is matter, so it stays orange over a bright field, and a pale heart of light in it
            fx.Sprite(FxShape.Flame, p, size, Fx.Fade(fire.Main, fade), FxBlend.Alpha);
            fx.Sprite(FxShape.Flame, p - Vector3.UnitY * size * 0.25f, size * 0.55f, Fx.Fade(fire.Light, fade * 0.9f));
        }
    }

    /// <summary>How far into a fit a paralysed Pokémon is at an age (seconds), or -1 between fits; and which fit it is.</summary>
    internal static (float Into, int Fit) FitAt(float age)
    {
        float cycle = age / FitEvery;
        float into = Loop(cycle) * FitEvery;
        return (into < FitTime ? into : -1f, (int)MathF.Floor(cycle));
    }

    /// <summary>Paralysed: now and then a fit of two short crooked bolts across its body and three sparks jumping off.</summary>
    private static void ParalysisFits(float age, float k, int seed, Vector3 feet, float h, float w, FxList fx)
    {
        var (into, fit) = FitAt(age);
        if (into < 0f) return;
        var electric = ColorsOf(PokemonType.Electric);
        var body = feet + Vector3.UnitY * h * 0.5f;
        // As far across as its body is wide, as far up and down as most of its height
        var reach = new Vector3(w * 0.95f, h * 0.42f, w * 0.6f);
        float fade = k * Math.Min(1f, (FitTime - into) / 0.08f);
        int which = seed + fit * 7;
        // The bolts are drawn afresh twenty-four times a second, as lightning is
        int frame = (int)(age * 24f);
        for (int b = 0; b < 2; b++)
        {
            var from = body + Fx.Direction(which, 320 + b) * reach;
            var to = body + Fx.Direction(which, 330 + b) * reach;
            var (across, up) = Fx.Frame(to - from);
            float length = (to - from).Length();
            var points = new Vector3[5];
            for (int j = 0; j < points.Length; j++)
            {
                float t = j / (float)(points.Length - 1);
                float bend = MathF.Sin(t * MathF.PI) * length * 0.14f;
                points[j] = Vector3.Lerp(from, to, t) + (across * Fx.Signed(which + frame, b * 5 + j, 341) + up * Fx.Signed(which + frame, b * 5 + j, 342)) * bend;
            }
            fx.Ribbon(points, h * 0.05f, Fx.Fade(electric.Main, fade), FxBlend.Alpha);
            fx.Ribbon(points, h * 0.02f, Fx.Fade(White, fade));
        }
        float thrown = Fx.EaseOut(into / FitTime);
        for (int j = 0; j < 3; j++)
        {
            var p = body + Fx.Direction(which, 350 + j) * reach * (0.8f + 0.7f * thrown);
            fx.Sprite(FxShape.Spark, p, h * 0.1f * (1f - 0.4f * thrown), Fx.Fade(electric.Main, fade), FxBlend.Alpha, into * 9f + j);
        }
    }

    /// <summary>
    /// Frozen: a shell of ice. Six shards stand round its feet and two lean off its shoulders, at four fifths'
    /// strength so the body shows through; those behind it are hidden by it. A glint pops on one now and then.
    /// </summary>
    private static void IceShell(float age, float k, int seed, Vector3 feet, float h, float w, FxList fx)
    {
        var ice = ColorsOf(PokemonType.Ice);
        float grown = 0.6f + 0.4f * k;
        Vector3 glintAt = default;
        int glintOn = (seed + (int)MathF.Floor(age / GlintEvery) * 5) % 8;
        if (glintOn < 0) glintOn += 8;
        for (int i = 0; i < 6; i++)
        {
            float a = (30f + 60f * i) * MathF.PI / 180f;
            float size = h * (0.15f + 0.06f * Fx.Rand(seed, i, 361)) * grown;
            var p = feet + new Vector3(MathF.Cos(a) * w * 1.05f, size * 0.72f, MathF.Sin(a) * w * 0.63f);
            // On the camera's side of it (+Z) a shard stands before the body; behind it the body hides it
            fx.Sprite(FxShape.Shard, p, size, Fx.Fade(i % 2 == 0 ? ice.Main : ice.Light, k * 0.8f), FxBlend.Alpha, -MathF.Cos(a) * 0.3f, depth: MathF.Sin(a) < 0f);
            if (i == glintOn) glintAt = p + Vector3.UnitY * size * 0.5f;
        }
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            float size = h * 0.14f * grown;
            var p = feet + new Vector3(side * w * 0.95f, h * 0.58f, h * 0.05f);
            fx.Sprite(FxShape.Shard, p, size, Fx.Fade(i == 0 ? ice.Light : ice.Main, k * 0.8f), FxBlend.Alpha, -side * 0.6f);
            if (6 + i == glintOn) glintAt = p + Vector3.UnitY * size * 0.4f;
        }
        float t = Loop(age / GlintEvery) * GlintEvery / 0.35f;
        if (t < 1f)
        {
            float s = MathF.Sin(t * MathF.PI);
            fx.Sprite(FxShape.Spark, glintAt, h * 0.16f * s, Fx.Fade(White, k), rotation: t * 1.2f);
            fx.Sprite(FxShape.Glow, glintAt, h * 0.1f * s, Fx.Fade(ice.Light, k * 0.7f));
        }
    }

    // ------------------------------------------------------------------ confusion and love

    /// <summary>
    /// How far out a confused head's stars circle: about its body's half-width, never under a fifth of its height
    /// nor over a third, so the ring sits on a narrow head and a wide one alike.
    /// </summary>
    public static float HeadRing(float h, float w) => Math.Clamp(w * 0.8f, h * 0.2f, h * 0.32f);

    /// <summary>Confused: three stars circling its head; behind the head they are hidden by it.</summary>
    public static void ConfusionStars(float age, float k, int seed, Vector3 feet, float h, float w, FxList fx)
    {
        if (k <= 0.004f) return;
        var gold = Rgb(255, 226, 90);
        var rim = Rgb(176, 120, 20);
        float ring = HeadRing(h, w);
        float turn = age * MathF.Tau / StarRound + Fx.Rand(seed, 0, 371) * MathF.Tau;
        for (int i = 0; i < 3; i++)
        {
            float a = turn + i * MathF.Tau / 3f;
            var p = feet + new Vector3(MathF.Cos(a) * ring, h * (OverTheHead + 0.02f * MathF.Sin(a * 2f)), MathF.Sin(a) * ring * 0.4f);
            bool behind = MathF.Sin(a) < 0f;
            fx.Sprite(FxShape.Star, p, h * 0.075f * 1.3f, Fx.Fade(rim, k), FxBlend.Alpha, a, depth: behind);
            fx.Sprite(FxShape.Star, p, h * 0.075f, Fx.Fade(gold, k), FxBlend.Alpha, a, depth: behind);
        }
    }

    /// <summary>Infatuated: two hearts rising one after the other from over its head, to its left, swaying.</summary>
    public static void LoveHearts(float age, float k, int seed, Vector3 feet, float h, FxList fx)
    {
        if (k <= 0.004f) return;
        var pink = Rgb(255, 120, 170);
        var rim = Rgb(196, 60, 120);
        for (int i = 0; i < 2; i++)
        {
            float start = i * HeartTime / 2f;
            if (age < start) continue;
            float u = Loop((age - start) / HeartTime);
            float fade = k * Math.Min(1f, u * 5f) * Math.Min(1f, (1f - u) * 3f);
            float sway = 0.03f * MathF.Sin(u * 7f + i + Fx.Rand(seed, i, 381) * MathF.Tau);
            // To its left and on up past the ring a confused head's stars keep to, as the Z's go to its right
            var p = feet + new Vector3(-h * (0.1f + 0.16f * u) + h * sway, h * (1.1f + 0.24f * u), 0f);
            float size = h * (0.07f + 0.025f * MathF.Sin(u * MathF.PI));
            float tilt = 0.15f * MathF.Sin(u * 6f + i);
            fx.Sprite(FxShape.Heart, p, size * 1.25f, Fx.Fade(rim, fade), FxBlend.Alpha, tilt);
            fx.Sprite(FxShape.Heart, p, size, Fx.Fade(pink, fade), FxBlend.Alpha, tilt);
        }
    }

    // ------------------------------------------------------------------ the cast

    /// <summary>
    /// The colour a condition blends into its Pokémon's own, and how much of it (0: none): the body's own part of
    /// a condition, laid by the shader's flash, so it is matter and keeps its colour on any body. A frozen Pokémon
    /// is ice-blue for as long as it is frozen; poison pulses, deeper and quicker when it is bad; a paralysed one
    /// glints as each fit begins.
    /// </summary>
    public static (Color Color, float Amount) Cast(ConditionLook look)
    {
        var cast = CastOf(look.Status, look.Ailment.Age, look.Ailment.Strength);
        if (cast.Amount <= 0f && look.Going.Visible) cast = CastOf(look.GoingStatus, look.Going.Age, look.Going.Strength);
        return cast;
    }

    public static (Color Color, float Amount) CastOf(StatusCondition status, float age, float k)
    {
        if (k <= 0f) return (default, 0f);
        switch (status)
        {
            case StatusCondition.Freeze:
                return (IceCast, k * (0.4f + 0.04f * MathF.Sin(age * 1.9f)));
            case StatusCondition.Poison:
                return (PoisonCast, k * 0.24f * Pulse(age, 2.4f, 0.9f));
            case StatusCondition.Toxic:
                return (ToxicCast, k * 0.34f * Pulse(age, 1.5f, 0.9f));
            case StatusCondition.Paralyze:
            {
                float into = FitAt(age).Into;
                return (ParalysisCast, into >= 0f && into < 0.1f ? k * 0.22f * (1f - into / 0.1f) : 0f);
            }
            default:
                return (default, 0f);
        }
    }

    /// <summary>A swell to 1 and away over <paramref name="length"/> seconds, once every <paramref name="period"/>.</summary>
    private static float Pulse(float age, float period, float length)
    {
        float t = Loop(age / period) * period;
        return t < length ? MathF.Sin(t / length * MathF.PI) : 0f;
    }
}
