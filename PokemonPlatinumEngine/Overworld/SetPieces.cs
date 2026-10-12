using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The set pieces the original's scripts call by name (plan 09 · L12; style guide, "Rooms", "Machines at work", and
/// "Life", "Set pieces"): a script's <c>anim</c> plays one where the player stands.
/// </summary>
public enum SetPiece
{
    /// <summary>A Pokémon Center's machine: a ball for each of the team laid on its tray, then all of them lit to the heal's fanfare (<c>PlayPokecenterHealingAnimation</c>).</summary>
    Heal,
    /// <summary>A PC's screen coming on (<c>PlayPCBootUpAnimation</c>); it stays on until it is switched off.</summary>
    PcOn,
    /// <summary>A PC's screen going off (<c>PlayPCShutDownAnimation</c>).</summary>
    PcOff,
    /// <summary>A lift's car on its way up: its indicator's arrow running, the car humming and shuddering (<c>PlayElevatorAnimation</c>).</summary>
    ElevatorUp,
    /// <summary>The same on its way down.</summary>
    ElevatorDown,
    /// <summary>The Hall of Fame's machine: a ball for each of the team set in its sockets, then all of them lit (<c>PlayHallOfFameHealingAnimation</c>).</summary>
    HallOfFame
}

/// <summary>Something a set piece is heard to do at a moment of its own: a sound of the bank, or the heal's fanfare.</summary>
public readonly record struct SetPieceCue(float At, string? Sound, bool Fanfare);

/// <summary>
/// What each set piece shows as a function of the time since it began (plan 09 · L12). GPU-free: the field's renderer
/// asks it what to draw, the script's reader and the headless host how long a piece takes, and tests hold every
/// number here to the style guide's. The counts are the original's (a ball a Pokémon, the hums of a lift's ride, the
/// times someone is shaken or flickers); the timing, the art and the sounds are ours.
/// </summary>
public static class SetPieces
{
    // ------------------------------------------------------------------ the machines

    /// <summary>The most balls a machine holds: a team's six.</summary>
    public const int MostBalls = 6;

    /// <summary>A ball is laid on a machine every so often, the first as the piece begins.</summary>
    public const float BallSeconds = 0.35f;

    /// <summary>How long the balls are lit: as long as the heal's fanfare sounds (seven beats of its eight at 116 a minute).</summary>
    public const float LitSeconds = 3.6f;

    /// <summary>The fanfare's beat, which the lit balls keep: lit for three quarters of it, plain for the last.</summary>
    public const float LitBeat = 60f / 116f;

    private static int Laid(int count) => Math.Clamp(count, 0, MostBalls);

    /// <summary>How long a machine's piece takes for a team of <paramref name="count"/>.</summary>
    public static float MachineLength(int count) => Laid(count) * BallSeconds + LitSeconds;

    /// <summary>How many balls lie on the machine at an age of its piece: one more every <see cref="BallSeconds"/>, up to the team's.</summary>
    public static int Balls(float age, int count)
    {
        if (age < 0f) return 0;
        return Math.Min(Laid(count), (int)MathF.Floor(age / BallSeconds + 1e-4f) + 1);
    }

    /// <summary>Whether the machine is at its last stretch, the balls all laid and the fanfare sounding.</summary>
    public static bool Lighting(float age, int count) => age >= Laid(count) * BallSeconds - 1e-4f && age < MachineLength(count);

    /// <summary>Whether the balls show lit at an age: on the fanfare's beat while it sounds, three quarters of each.</summary>
    public static bool Lit(float age, int count)
    {
        if (!Lighting(age, count)) return false;
        float into = age - Laid(count) * BallSeconds;
        return into % LitBeat < LitBeat * 0.75f;
    }

    /// <summary>The frames a machine's tray is painted in: empty, one ball to six, and one to six lit.</summary>
    public const int TrayFrames = 1 + 2 * MostBalls;

    /// <summary>The frame of the tray that shows so many balls, lit or not (<see cref="TrayFrames"/>): 0 empty, 1 to 6, 7 to 12 lit.</summary>
    public static int TrayFrame(int balls, bool lit)
    {
        balls = Laid(balls);
        return balls == 0 ? 0 : lit ? MostBalls + balls : balls;
    }

    /// <summary>How many balls a tray's frame shows, and whether they are lit: what paints it.</summary>
    public static (int Balls, bool Lit) OnTray(int frame) => frame <= MostBalls ? (frame, false) : (frame - MostBalls, true);

    // ------------------------------------------------------------------ the PC

    /// <summary>A PC's screen: dark, starting (or going), and on.</summary>
    public const int PcDark = 0, PcStarting = 1, PcLit = 2, PcFrames = 3;

    /// <summary>How long a screen takes to come on, and to go off.</summary>
    public const float PcOnSeconds = 0.4f, PcOffSeconds = 0.3f;

    /// <summary>How long the screen shows its middle frame on the way on and on the way off.</summary>
    public const float PcStartingSeconds = 0.3f, PcGoingSeconds = 0.2f;

    /// <summary>The frame of a PC's screen at an age of its coming on (<paramref name="on"/>) or its going off.</summary>
    public static int PcFrame(bool on, float age)
    {
        if (on) return age < PcStartingSeconds ? PcStarting : PcLit;
        return age < PcGoingSeconds ? PcStarting : PcDark;
    }

    // ------------------------------------------------------------------ the lift

    /// <summary>How many times a lift hums on a ride when the script doesn't say: the original's four (three in the Battle Tower's).</summary>
    public const int Hums = 4, MostHums = 12;

    /// <summary>One hum of a lift's ride: one round of its indicator's arrow.</summary>
    public const float HumSeconds = 0.5f;

    /// <summary>How long the car settles as it arrives, its chime sounding.</summary>
    public const float ArrivalSeconds = 0.5f;

    public static float ElevatorLength(int hums) => Math.Clamp(hums, 1, MostHums) * HumSeconds + ArrivalSeconds;

    /// <summary>Whether a lift is between floors at an age of its ride.</summary>
    public static bool Riding(float age, int hums) => age >= 0f && age < Math.Clamp(hums, 1, MostHums) * HumSeconds;

    /// <summary>The frames a lift's indicator is painted in: at rest, and its arrow in three places on the way up and on the way down.</summary>
    public const int IndicatorFrames = 7, IndicatorSteps = 3;

    /// <summary>The indicator's frame at an age of a ride: the arrow of the way it goes, a texel further on every third of a hum.</summary>
    public static int IndicatorFrame(bool up, float age, int hums)
    {
        if (!Riding(age, hums)) return 0;
        int step = (int)MathF.Floor(age / (HumSeconds / IndicatorSteps) + 1e-4f) % IndicatorSteps;
        return (up ? 1 : 1 + IndicatorSteps) + step;
    }

    /// <summary>
    /// How far the car's picture is thrown at an age of a ride, in whole texels, stepped thirty times a second: a
    /// texel up for a thirtieth of a second in every four while it rides, and two texels down for a fifteenth as it
    /// stops. Nought the rest of the time.
    /// </summary>
    public static int Shudder(float age, int hums)
    {
        if (age < 0f) return 0;
        int beat = (int)MathF.Floor(age * 30f + 1e-3f);
        if (Riding(age, hums)) return beat % 4 == 0 ? 1 : 0;
        int since = beat - (int)MathF.Round(Math.Clamp(hums, 1, MostHums) * HumSeconds * 30f);
        return since is >= 0 and < 2 ? -2 : 0;
    }

    // ------------------------------------------------------------------ someone shaken, someone flickering

    /// <summary>How many times someone is shaken or flickers when the script doesn't say: the counts of the original's two uses this game has, Regigigas's eight and Cresselia's six.</summary>
    public const int Shakes = 8, Flickers = 6, MostTimes = 32;

    /// <summary>How far a shake throws someone to each side, in texels.</summary>
    public const int ShakeReach = 3;

    /// <summary>One shake: east, back, west and back, a thirtieth of a second each.</summary>
    public const float ShakeSeconds = 4f / 30f;

    public static float ShakeLength(int times) => Math.Clamp(times, 1, MostTimes) * ShakeSeconds;

    /// <summary>How many texels east of their tile someone is drawn at an age of their shaking; nought before it and once it is over.</summary>
    public static int ShakeOffset(float age, int times)
    {
        if (age < 0f || age >= ShakeLength(times)) return 0;
        return ((int)MathF.Floor(age * 30f + 1e-3f) % 4) switch { 0 => ShakeReach, 2 => -ShakeReach, _ => 0 };
    }

    /// <summary>One change of a flicker, from seen to unseen or back.</summary>
    public const float FlickerSeconds = 0.2f;

    public static float FlickerLength(int times) => Math.Clamp(times, 1, MostTimes) * FlickerSeconds;

    /// <summary>
    /// Whether someone who flickers is seen at an age: unseen first, then seen, changing <paramref name="times"/>
    /// times in all (the original's count), and seen again before and after.
    /// </summary>
    public static bool FlickerSeen(float age, int times)
    {
        if (age < 0f || age >= FlickerLength(times)) return true;
        return (int)MathF.Floor(age / FlickerSeconds + 1e-4f) % 2 == 1;
    }

    // ------------------------------------------------------------------ a piece as a whole

    /// <summary>How long a piece takes, which is how long its script waits: <paramref name="count"/> is the team's for a machine, the hums for a lift.</summary>
    public static float Length(SetPiece piece, int count) => piece switch
    {
        SetPiece.Heal or SetPiece.HallOfFame => MachineLength(count),
        SetPiece.PcOn => PcOnSeconds,
        SetPiece.PcOff => PcOffSeconds,
        _ => ElevatorLength(count)
    };

    /// <summary>The words a script calls a piece by (<c>anim heal</c>, <c>anim elevator up</c>), which the headless host writes in its log.</summary>
    public static string WordsOf(SetPiece piece) => piece switch
    {
        SetPiece.Heal => "heal",
        SetPiece.PcOn => "pc on",
        SetPiece.PcOff => "pc off",
        SetPiece.ElevatorUp => "elevator up",
        SetPiece.ElevatorDown => "elevator down",
        _ => "halloffame"
    };

    /// <summary>The sound a ball makes as it is laid on a Pokémon Center's machine, and as it is set in a socket of the Hall of Fame's.</summary>
    public const string TraySound = "machine_ball", SocketSound = "ball_click";

    /// <summary>
    /// What a piece is heard to do, each at its moment (<c>docs/sound-effects.md</c>): a ball laid, the heal's fanfare
    /// as the balls light, a PC's two sounds, a lift's hum to each round of its lights and its chime as it arrives.
    /// </summary>
    public static IEnumerable<SetPieceCue> Cues(SetPiece piece, int count)
    {
        switch (piece)
        {
            case SetPiece.Heal:
            case SetPiece.HallOfFame:
                for (int ball = 0; ball < Laid(count); ball++)
                    yield return new SetPieceCue(ball * BallSeconds, piece == SetPiece.Heal ? TraySound : SocketSound, false);
                yield return new SetPieceCue(Laid(count) * BallSeconds, null, true);
                break;
            case SetPiece.PcOn:
                yield return new SetPieceCue(0f, "pc_on", false);
                break;
            case SetPiece.PcOff:
                yield return new SetPieceCue(0f, "pc_off", false);
                break;
            default:
                int hums = Math.Clamp(count, 1, MostHums);
                for (int hum = 0; hum < hums; hum++) yield return new SetPieceCue(hum * HumSeconds, "lift_hum", false);
                yield return new SetPieceCue(hums * HumSeconds, "lift_ding", false);
                break;
        }
    }
}

/// <summary>
/// The set pieces playing now (plan 09 · L12), each with the moment it began on the field's own clock: a room's
/// machine, the PC that is on, a lift's ride, and whoever is being shaken or flickers. GPU-free: the game tells it
/// what begins and the renderer asks it what to draw; coming to another place forgets it all.
/// </summary>
public sealed class SetPieceStage
{
    private (SetPiece Piece, int Count, double Born)? machine;
    private (bool On, int X, int Y, double Born)? pc;
    private (bool Up, int Hums, double Born)? lift;

    /// <summary>Whoever is shaken or flickers: nobody stands for the player.</summary>
    private readonly List<(NPC? Who, bool Flickers, int Times, double Born)> moved = new();

    // What has been heard of each piece so far, so a cue sounds once, and the column of the map it sounds from
    private readonly List<(SetPiece Piece, int Count, double Born, double Heard, int? X)> sounding = new();

    /// <summary>
    /// Begins a machine's piece, a PC's or a lift's, and says how long it takes. <paramref name="at"/> is the tile it
    /// plays on: the machine's, the PC's (which is which PC comes on), the car's.
    /// </summary>
    public float Play(SetPiece piece, int count, double now, (int X, int Y)? at = null)
    {
        int? x = at?.X;
        switch (piece)
        {
            case SetPiece.Heal:
            case SetPiece.HallOfFame:
                machine = (piece, count, now);
                break;
            case SetPiece.PcOn:
                pc = (true, at?.X ?? 0, at?.Y ?? 0, now);
                break;
            case SetPiece.PcOff:
                // The PC that is on goes off; with none on there is nothing to see
                if (pc is not { On: true } lit) return 0f;
                pc = (false, lit.X, lit.Y, now);
                x = lit.X;
                break;
            default:
                lift = (piece == SetPiece.ElevatorUp, count, now);
                break;
        }
        sounding.Add((piece, count, now, double.NegativeInfinity, x));
        return SetPieces.Length(piece, count);
    }

    /// <summary>Someone is shaken so many times (nobody is the player), from now; says how long it takes.</summary>
    public float Shake(NPC? who, int times, double now)
    {
        moved.RemoveAll(m => m.Who == who && !m.Flickers);
        moved.Add((who, false, times, now));
        return SetPieces.ShakeLength(times);
    }

    /// <summary>Someone flickers, changing so many times between unseen and seen, from now; says how long it takes.</summary>
    public float Flicker(NPC? who, int times, double now)
    {
        moved.RemoveAll(m => m.Who == who && m.Flickers);
        moved.Add((who, true, times, now));
        return SetPieces.FlickerLength(times);
    }

    /// <summary>Forgets every piece: coming to another place.</summary>
    public void Clear()
    {
        machine = null;
        pc = null;
        lift = null;
        moved.Clear();
        sounding.Clear();
    }

    /// <summary>
    /// Every piece playing is at its end now, unheard (plan 12 · Q20: a scene skipped): a healing machine's tray is
    /// empty again, the Hall of Fame's balls lie in their sockets, a PC switched on is on and one switched off is
    /// dark, a ride is over, and nobody is shaken or flickers. What a played piece leaves is what is left.
    /// </summary>
    public void Settle(double now)
    {
        if (machine is { } m) machine = m.Piece == SetPiece.HallOfFame ? m with { Born = now - SetPieces.MachineLength(m.Count) - 1.0 } : null;
        if (pc is { } p) pc = p.On ? p with { Born = now - SetPieces.PcOnSeconds - 1.0 } : null;
        lift = null;
        moved.Clear();
        sounding.Clear();
    }

    /// <summary>Lets go of what is over: a ride ended, a shake done, a screen gone dark. A machine and a PC that is on are kept.</summary>
    public void Advance(double now)
    {
        if (lift is { } ride && now - ride.Born >= SetPieces.ElevatorLength(ride.Hums)) lift = null;
        if (pc is { On: false } off && now - off.Born >= SetPieces.PcOffSeconds) pc = null;
        if (machine is { Piece: SetPiece.Heal } heal && now - heal.Born >= SetPieces.MachineLength(heal.Count)) machine = null;
        moved.RemoveAll(m => now - m.Born >= (m.Flickers ? SetPieces.FlickerLength(m.Times) : SetPieces.ShakeLength(m.Times)));
    }

    /// <summary>
    /// What lies on a machine now and whether it is lit: a Pokémon Center's tray while its piece plays and nothing
    /// afterwards (the nurse has the balls back); the Hall of Fame's sockets from the first ball on, since the balls
    /// stay there once they have been lit.
    /// </summary>
    public (int Balls, bool Lit) Machine(SetPiece which, double now)
    {
        if (machine is not { } m || m.Piece != which) return (0, false);
        float age = (float)(now - m.Born);
        if (age >= SetPieces.MachineLength(m.Count)) return which == SetPiece.HallOfFame ? (Math.Clamp(m.Count, 0, SetPieces.MostBalls), false) : (0, false);
        return (SetPieces.Balls(age, m.Count), SetPieces.Lit(age, m.Count));
    }

    /// <summary>Whether a Pokémon Center's machine is at work now: its balls all laid and lighting.</summary>
    public bool MachineAtWork(double now) =>
        machine is { Piece: SetPiece.Heal } m && SetPieces.Lighting((float)(now - m.Born), m.Count);

    /// <summary>The PC whose screen isn't dark, with its screen's frame; null when every screen is.</summary>
    public (int X, int Y, int Frame)? Screen(double now) =>
        pc is { } p ? (p.X, p.Y, SetPieces.PcFrame(p.On, (float)(now - p.Born))) : null;

    /// <summary>The frame of a lift's indicator now: at rest unless a ride is under way.</summary>
    public int Indicator(double now) => lift is { } ride ? SetPieces.IndicatorFrame(ride.Up, (float)(now - ride.Born), ride.Hums) : 0;

    /// <summary>How many texels a lift's ride throws the picture now.</summary>
    public int Shudder(double now) => lift is { } ride ? SetPieces.Shudder((float)(now - ride.Born), ride.Hums) : 0;

    /// <summary>How many texels east of their tile someone is drawn now (nobody is the player): nought unless they are being shaken.</summary>
    public int OffsetOf(NPC? who, double now)
    {
        foreach (var m in moved)
            if (m.Who == who && !m.Flickers) return SetPieces.ShakeOffset((float)(now - m.Born), m.Times);
        return 0;
    }

    /// <summary>Whether someone is seen now: always, unless they flicker and this is a moment they don't show.</summary>
    public bool Seen(NPC? who, double now)
    {
        foreach (var m in moved)
            if (m.Who == who && m.Flickers) return SetPieces.FlickerSeen((float)(now - m.Born), m.Times);
        return true;
    }

    /// <summary>True while anyone is being shaken or flickers: whoever draws people asks each of them only then.</summary>
    public bool AnyoneMoved => moved.Count > 0;

    /// <summary>
    /// What the pieces playing are heard to do up to now and haven't been heard to do yet, each cue once, with the
    /// column of the map it sounds from (none for a piece played nowhere in particular).
    /// </summary>
    public void TakeCues(double now, List<(SetPieceCue Cue, int? X)> into)
    {
        for (int i = 0; i < sounding.Count; i++)
        {
            var s = sounding[i];
            double age = now - s.Born;
            foreach (var cue in SetPieces.Cues(s.Piece, s.Count))
                if (cue.At > s.Heard && cue.At <= age) into.Add((cue, s.X));
            sounding[i] = s with { Heard = age };
        }
        sounding.RemoveAll(s => s.Heard >= SetPieces.Length(s.Piece, s.Count));
    }
}
