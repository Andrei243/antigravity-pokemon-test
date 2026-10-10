using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One cook's stylus on the pot for a frame (the original's <c>UnkStruct_ov83_0223F88C</c>): where it touches, in the
/// pot screen's pixels (256 by 192, the pot's middle at 128, 96), how far it swirled round the middle since the frame
/// before (the stroke's length along the turn, × 160) and whether that was against the clock.
/// </summary>
public readonly record struct PotTouch(int X, int Y, int Swirl, bool Backward)
{
    /// <summary>Nobody's stylus: the middle of the pot, not moving (<c>ov83_0223C82C</c>).</summary>
    public static readonly PotTouch Still = new(PoffinPot.MiddleX, PoffinPot.MiddleY, 0, false);
}

/// <summary>
/// A stylus on the pot, frame by frame (<c>UnkStruct_ov83_0223F820</c>, <c>ov83_0223F83C</c>, <c>ov83_0223F88C</c>):
/// where it is, where it was a frame ago, and the swirl between the two. A stylus that has just come down has not
/// moved; one lifted stays where it was and swirls no more.
/// </summary>
public sealed class PotStylus
{
    public int X { get; private set; } = PoffinPot.MiddleX;
    public int Y { get; private set; } = PoffinPot.MiddleY;
    private int lastX = PoffinPot.MiddleX, lastY = PoffinPot.MiddleY, swirl;
    private bool wasDown;

    /// <summary>The stylus this frame: down at a point of the pot screen, or up.</summary>
    public void Frame(bool down, int x, int y)
    {
        if (down)
        {
            X = x;
            Y = y;
            if (!wasDown) (lastX, lastY) = (X, Y);
        }
        swirl = PotMath.Swirl(X, Y, lastX, lastY);
        (lastX, lastY) = (X, Y);
        wasDown = down;
    }

    /// <summary>What the frame sends to the pot (<c>ov83_0223F88C</c>): the place as bytes, the swirl as a 16-bit size and its sign.</summary>
    public PotTouch Sample => new((byte)X, (byte)Y, (ushort)Math.Abs(swirl), swirl < 0);
}

/// <summary>
/// The original's arithmetic for the pot (<c>src/overlay083/ov83_0223F7F4.c</c>, <c>src/math_util.c</c>,
/// <c>src/int_distance.c</c>), in the console's fixed point: 12 bits of fraction, products rounded
/// (<c>FX_Mul</c>), quotients rounded (<c>FX_Div</c>).
/// </summary>
public static class PotMath
{
    public const int One = 4096;

    public static int FxMul(int a, int b) => (int)(((long)a * b + 0x800) >> 12);

    public static int FxDiv(int a, int b) => (int)(((((long)a << 32) / b) + (1L << 19)) >> 20);

    /// <summary>
    /// How far a stroke from one point to another swirled round the pot's middle (<c>ov83_0223F7F4</c> with
    /// <c>ApproximateArcLength</c>): the stroke measured along the point it came from, its coordinates swapped and
    /// made a unit, in whole pixels; negative when it went against the clock on the screen (the cross product of the
    /// two places is not positive); × 160. As the original measures it, a stroke round the pot counts in full where it
    /// passes the four points straight across from the middle and hardly at all half way between them.
    /// </summary>
    public static int Swirl(int x, int y, int fromX, int fromY)
    {
        int x0 = fromX - PoffinPot.MiddleX, y0 = fromY - PoffinPot.MiddleY;
        int x1 = x - PoffinPot.MiddleX, y1 = y - PoffinPot.MiddleY;
        long cross = (long)x0 * y1 - (long)y0 * x1;
        // VEC_Normalize of (y0, x0): a unit vector in fixed point; nothing for the pot's very middle
        double length = Math.Sqrt((double)x0 * x0 + (double)y0 * y0);
        int nx = length > 0 ? (int)Math.Round(y0 * One / length) : 0;
        int ny = length > 0 ? (int)Math.Round(x0 * One / length) : 0;
        // VEC_DotProduct with the stroke, then >> FX32_SHIFT to whole pixels
        int dot = nx * (x1 - x0) + ny * (y1 - y0);
        int result = Math.Abs(dot >> 12);
        if (cross <= 0) result = -result;
        return result * 160;
    }

    /// <summary>A whole-pixel distance (<c>CalcDistance2D</c>): the square root rounded down.</summary>
    public static int Distance(int x0, int y0, int x1, int y1)
    {
        long dx = x0 - x1, dy = y0 - y1;
        return (int)Math.Sqrt(dx * dx + dy * dy);
    }

    // CalcRadialAngle(68, d): 2 × 68 × FX32_CONST(3.140f) is 427 pixels round
    private const int Circumference = 427;

    /// <summary>The turn a distance round the pot makes, in sixty-fifths of a thousand of a turn (<c>CalcRadialAngle</c>).</summary>
    public static int RadialAngle(int distance) => distance * 0xFFFF / Circumference;
}

/// <summary>
/// The pot at the Poffin House (plan 06 · R14c): the batter, the heat under it, the arrow that says which way to
/// stir, its three stages and what comes out, by the original's code (<c>src/overlay083/ov83_0223F7F4.c</c>:
/// <c>UnkStruct_ov83_0223FDB0</c> and its functions). It runs a frame at a time, thirty to the second as the original's
/// cooking does, on the touches of every cook. No drawing or input.
/// </summary>
public sealed class PoffinPot
{
    public const int MiddleX = 128, MiddleY = 96;

    /// <summary>Frames to the second, as the original's cooking counts them.</summary>
    public const int FramesPerSecond = 30;

    /// <summary>A stage ends after twenty seconds or sixteen turns the way the arrow says.</summary>
    public const int StageFrames = 20 * FramesPerSecond, TurnsPerStage = 16, Stages = 3;

    /// <summary>The batter's speed: at or under 910 it burns, at 3640 (the most) it spills over, but in the last stage.</summary>
    public const int Slow = 910, Top = 3640;

    // Unk_ov83_02240590 and Unk_ov83_0224059C: how much a stroke pushes the batter, and how much it slows by itself
    private static readonly int[] Push = { 8, 7, 7 };
    private static readonly int[] Drag = { 64, 72, 80 };
    // ov83_0223FBBC: frames before the arrow may turn, in each stage (and a draw of up to two seconds more)
    private static readonly int[] ArrowFrames = { 30 * 5, 30 * 4, 30 * 3 };
    // Unk_ov83_02240598: the bonus for stirring together, a tenth of it a share for two, three or four cooks
    private static readonly int[] TogetherShares = { 0, 1, 5, 10 };

    private readonly Random rng;

    public PoffinPot(Random rng)
    {
        this.rng = rng;
        // ov83_0223FDB0: the arrow's odds at two, its timer run out, the stage's light full, and the first arrow drawn
        odds = 2;
        arrowTimer = -1;
        Fade = 31;
        Arrow(0, 0, false);
    }

    // ---------------------------------------------------------------- the arrow (UnkStruct_ov83_0223FBA4)

    /// <summary>The way the arrow says to stir: false with the clock on the screen, true against it.</summary>
    public bool Backward { get; private set; }

    /// <summary>The arrow was drawn again this frame (the original shows it and chimes, whether or not it turned).</summary>
    public bool ArrowDrawn { get; private set; }
    private int arrowTimer, odds;

    // ---------------------------------------------------------------- the batter (UnkStruct_ov83_0223F8AC)

    /// <summary>The stage the batter is in, 0 to 2.</summary>
    public int Stage { get; private set; }

    /// <summary>Turns the batter has gone the arrow's way in this stage.</summary>
    public int Turns { get; private set; }

    /// <summary>Where the batter has turned to, in 65,536ths of a turn (with the clock on the screen as it grows).</summary>
    public int Angle { get; private set; }

    /// <summary>How fast the batter goes round, against the clock when negative; at most 3640 either way.</summary>
    public int Speed { get; private set; }

    /// <summary>Every cook stirred inside the batter this frame, neither in its very middle nor past its rim.</summary>
    public bool Steady { get; private set; } = true;

    // ---------------------------------------------------------------- the heat (UnkStruct_ov83_0223FAAC)

    /// <summary>How many times the batter burned and spilled over (at most 9,999 each).</summary>
    public int Burns { get; private set; }
    public int Spills { get; private set; }

    /// <summary>What the heat did this frame: 0 nothing, 1 the batter burned, 2 it is about to.</summary>
    public int BurnSign { get; private set; }

    /// <summary>The batter spilled over this frame.</summary>
    public bool Spilled { get; private set; }
    private int slowFrames, fastFrames;
    private bool warned;

    // ---------------------------------------------------------------- the stages (UnkStruct_ov83_0223FC58)

    /// <summary>The stage the cooking is in (3 once it is done), and the frames it has gone on.</summary>
    public int Phase { get; private set; }
    public int PhaseFrames { get; private set; }

    /// <summary>How far from the stage's end, 31 down to 0 in its last two seconds or last five turns: the next stage's batter shows through as it falls.</summary>
    public int Fade { get; private set; }

    // ---------------------------------------------------------------- stirring together (UnkStruct_ov83_0223FDB0_sub1)

    /// <summary>Frames every cook stirred together, close by and fast enough, and whether they do now (sparkles).</summary>
    public int TogetherFrames { get; private set; }
    public bool Together { get; private set; }
    public bool Sparkle { get; private set; }
    private int togetherRun;

    // ---------------------------------------------------------------- the whole

    /// <summary>The frames the cooking has taken so far (<c>ov83_0223FFA0</c>).</summary>
    public int Frames { get; private set; }

    /// <summary>The three stages are over.</summary>
    public bool Done { get; private set; }

    /// <summary>Whether a speed burns the batter: at 910 or under.</summary>
    public static bool Burning(int speed) => Math.Abs(speed) <= Slow;

    /// <summary>Whether a speed spills it over: the most, but never in the last stage (<c>ov83_0223FB30</c>).</summary>
    public static bool Spilling(int speed, int phase) => phase != 2 && Math.Abs(speed) >= Top;

    /// <summary>Whether the batter goes the other way from the arrow (<c>ov83_0223FC3C</c>).</summary>
    public bool WrongWay => (Speed < 0 && !Backward) || (Speed > 0 && Backward);

    /// <summary>
    /// A frame of cooking (<c>ov83_0223FDD8</c>, then <c>ov83_0223FFA0</c>): the stage ends or goes on, the cooks'
    /// strokes push the batter and it slows, the heat burns or spills it, the cooks stir together or not, and the
    /// arrow counts down. The touches are what each cook sent the frame before, as the original's game hears them back
    /// a frame late. True once the three stages are over.
    /// </summary>
    public bool Step(IReadOnlyList<PotTouch> cooks)
    {
        if (Done) return true;
        if (cooks.Count is < 1 or > 4) throw new ArgumentException("One to four cooks stir a pot.", nameof(cooks));
        if (NextPhase())
        {
            Done = true;
            return true;
        }
        StirBatter(cooks);
        Heat();
        StirTogether(cooks);
        Arrow(Phase, Speed, Together);
        Frames++;
        return false;
    }

    // ov83_0223FC58: a stage ends after 600 frames or 16 turns, and the batter is told its new stage
    private bool NextPhase()
    {
        if (Phase >= Stages) return true;
        int turns = Turns;
        if (PhaseFrames == StageFrames || turns >= TurnsPerStage)
        {
            Phase++;
            PhaseFrames = 0;
            turns = 0;
            if (Phase >= Stages) return true;
            Stage = Phase;
            Turns = 0;
        }
        int left = StageFrames - PhaseFrames, byTime = 31, byTurns = 31;
        if (left <= 2 * FramesPerSecond) byTime = 31 * left / (2 * FramesPerSecond);
        int toGo = TurnsPerStage - turns;
        if (toGo <= 5) byTurns = 31 * toGo / 5;
        Fade = Math.Min(byTime, byTurns);
        PhaseFrames++;
        return false;
    }

    // ov83_0223F8AC: where a cook's stylus is against the batter: 0 in it, 1 in its very middle, 2 past its rim,
    // which spreads from 72 pixels across as the batter speeds up past 910 to 88 at the most
    private int Where(int x, int y)
    {
        int distance = PotMath.Distance(x, y, MiddleX, MiddleY);
        int over = Math.Max(0, Math.Abs(Speed) - Slow);
        int spread = PotMath.One + PotMath.FxDiv(PotMath.FxMul(PotMath.One / 4, over * PotMath.One), (Top - Slow) * PotMath.One);
        int rim = (PotMath.FxMul(spread, 64 * PotMath.One) >> 12) + 8;
        if (distance < 16) return 1;
        return distance > rim ? 2 : 0;
    }

    // ov83_0223F900: the cooks' strokes, averaged, push the batter; it slows, never past standing still; it turns, and
    // a turn counts when it goes the arrow's way
    private void StirBatter(IReadOnlyList<PotTouch> cooks)
    {
        Steady = true;
        int sum = 0;
        foreach (var cook in cooks)
        {
            int push;
            switch (Where(cook.X, cook.Y))
            {
                case 0: push = cook.Swirl; break;
                case 1: push = cook.Swirl / 2; Steady = false; break;
                default: push = 0; Steady = false; break;
            }
            sum += cook.Backward ? -push : push;
        }
        sum /= cooks.Count;
        int share = PotMath.FxDiv(Push[Stage] * PotMath.One, 204 * PotMath.One);
        sum = PotMath.FxMul(sum * PotMath.One, share) >> 12;

        int speed = Speed + sum;
        if (speed > 0) speed = Math.Max(0, speed - Drag[Stage]);
        else if (speed < 0) speed = Math.Min(0, speed + Drag[Stage]);
        Speed = Math.Clamp(speed, -Top, Top);

        int before = Angle;
        Angle += PotMath.RadialAngle(Speed / 160);
        if ((!Backward && Speed >= 0) || (Backward && Speed < 0))
        {
            if (!Backward ? (before & 0xFFFF) > (Angle & 0xFFFF) : (before & 0xFFFF) < (Angle & 0xFFFF)) Turns++;
        }
    }

    // ov83_0223FAAC: a second at the most spills the batter over, each time; three seconds slow is a warning, and each
    // three more a burn; going at a good speed clears the warning
    private void Heat()
    {
        Spilled = false;
        BurnSign = 0;
        if (Spilling(Speed, Phase))
        {
            if (++fastFrames >= FramesPerSecond)
            {
                if (Spills < 9999) Spills++;
                Spilled = true;
                fastFrames = 0;
            }
            warned = false;
        }
        else if (Burning(Speed))
        {
            if (++slowFrames >= 3 * FramesPerSecond)
            {
                slowFrames = 0;
                if (!warned)
                {
                    BurnSign = 2;
                    warned = true;
                }
                else
                {
                    BurnSign = 1;
                    if (Burns < 9999) Burns++;
                }
            }
        }
        else warned = false;
    }

    // ov83_0223FCE8: cooks stir together while the batter goes at a good speed the arrow's way, every one of them in
    // the batter and stroking hard (over 600), and each within 32 pixels of the first; after four such frames each
    // counts and sparkles. One cook alone never does.
    private void StirTogether(IReadOnlyList<PotTouch> cooks)
    {
        Sparkle = false;
        if (Burning(Speed) || Spilling(Speed, Phase) || !Steady || cooks.Count <= 1)
        {
            togetherRun = 0;
            Together = false;
            return;
        }
        if (WrongWay) return;
        if (cooks.Any(c => c.Swirl <= 600)) return;
        for (int i = 1; i < cooks.Count; i++)
        {
            if (PotMath.Distance(cooks[0].X, cooks[0].Y, cooks[i].X, cooks[i].Y) > 32)
            {
                togetherRun = 0;
                Together = false;
                return;
            }
        }
        if (togetherRun < 4) togetherRun++;
        else
        {
            Sparkle = true;
            TogetherFrames++;
            Together = true;
        }
    }

    // ov83_0223FBBC: the arrow waits while the batter goes the wrong way or the cooks stir together; once its time is
    // up it is drawn again: one way or the other by its odds, which lean further from the way it kept each time it
    // keeps it, back to even when it turns, and a new time
    private void Arrow(int phase, int speed, bool together)
    {
        ArrowDrawn = false;
        bool wrong = (speed < 0 && !Backward) || (speed > 0 && Backward);
        if (wrong || together) return;
        if (arrowTimer < 0)
        {
            uint r = (uint)rng.NextInt64(0, 1L << 32);
            bool back = r % 5 <= (uint)odds;
            ArrowDrawn = true;
            if (back == Backward)
            {
                if (Backward) { if (odds - 1 >= 0) odds--; }
                else if (odds + 1 < 5) odds++;
            }
            else
            {
                Backward = back;
                odds = 2;
            }
            arrowTimer = ArrowFrames[phase] + (int)(r % (2 * FramesPerSecond));
        }
        arrowTimer--;
    }

    // ---------------------------------------------------------------- what comes out

    /// <summary>The bonus for stirring together (<c>ov83_0223FFA8</c>): a sixth of the frames together, by the share for the number of cooks, a tenth of that.</summary>
    public int TogetherBonus(int cooks) => TogetherBonusOf(TogetherFrames, cooks);

    public static int TogetherBonusOf(int togetherFrames, int cooks) =>
        Math.Min(9999, togetherFrames / 6 * TogetherShares[Math.Clamp(cooks, 1, 4) - 1] / 10);

    /// <summary>The cooking's time, as the results show it: minutes, seconds and hundredths (<c>ov83_022401AC</c>).</summary>
    public (int Minutes, int Seconds, int Hundredths) Time
    {
        get
        {
            int f = Frames;
            int minutes = f / (60 * FramesPerSecond);
            f -= minutes * 60 * FramesPerSecond;
            int seconds = f / FramesPerSecond;
            f -= seconds * FramesPerSecond;
            return (minutes, seconds, 100 * f / FramesPerSecond);
        }
    }

    /// <summary>
    /// The Poffin the pot makes from the cooks' berries (<c>ov83_0223FFD4</c>). The berries' flavours are added up,
    /// each less the next one round (spicy less dry … sour less spicy), and each less one for every one of those that
    /// came out below nought; all four or five below nought, or two cooks bringing the same berry, make it foul. The
    /// flavours are then scaled by how quickly it was cooked (× 1,800,000 ÷ the frames, rounded to a hundredth: a
    /// minute is × 1), rounded, less one for each burn and each spill, and never below nought. The smoothness is the
    /// berries' average less the number of cooks, less the bonus for stirring together (at most 10), and at least 15.
    /// Flavours and smoothness are bytes, as the original keeps them, so a flavour over 255 wraps round.
    /// </summary>
    public Poffin Finish(IReadOnlyList<ItemData> berries, Random rng) => Result(berries, Frames, Burns, Spills, TogetherFrames, rng);

    /// <summary>What a cooking of these berries comes to, after so many frames with so many burns and spills and frames stirred together (<c>ov83_0223FFD4</c>; <see cref="Finish"/>).</summary>
    public static Poffin Result(IReadOnlyList<ItemData> berries, int frames, int burns, int spills, int togetherFrames, Random rng)
    {
        int n = berries.Count;
        if (n is < 1 or > 4) throw new ArgumentException("One to four berries go into a pot.", nameof(berries));
        var sums = new int[5];
        int smooth = 0, sameMost = 0;
        for (int i = 0; i < n; i++)
        {
            int same = berries.Count(x => x.Id == berries[i].Id);
            sameMost = Math.Max(sameMost, same);
            var b = berries[i].Berry ?? throw new ArgumentException($"{berries[i].Name} is no berry.", nameof(berries));
            sums[0] += b.Spiciness;
            sums[1] += b.Dryness;
            sums[2] += b.Sweetness;
            sums[3] += b.Bitterness;
            sums[4] += b.Sourness;
            smooth += b.Smoothness;
        }
        bool foul = sameMost >= 2 && n > 1;
        int smoothness = smooth / n - n;

        var f = new int[5];
        int below = 0;
        for (int i = 0; i < 5; i++)
        {
            f[i] = sums[i] - sums[(i + 1) % 5];
            if (f[i] < 0) below++;
        }
        for (int i = 0; i < 5; i++) f[i] -= below;
        if (below >= 4) foul = true;

        int quick = StageFrames * Stages * 1000 / Math.Max(1, frames);
        if (quick % 10 >= 5) quick += 10;
        quick /= 10;
        for (int i = 0; i < 5; i++)
        {
            int scaled = f[i] * quick;
            if (scaled % 100 >= 50) scaled += 100;
            scaled /= 100;
            f[i] = Math.Max(0, scaled - (burns + spills));
        }

        if (n > 1) smoothness -= Math.Min(10, TogetherBonusOf(togetherFrames, n));
        smoothness = Math.Max(15, smoothness);
        return Poffins.Make(f, smoothness, foul, rng);
    }
}
