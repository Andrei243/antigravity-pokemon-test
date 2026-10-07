using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Eterna Gym's flower clock (plan 01 · M9), ported from the original's <c>gym_features.c</c>: a clock face of
/// flowers thirteen tiles across, whose two hands are the only ways over it. Each trainer beaten and the Leader
/// turn the clock on (<c>AdvanceEternaGymClock</c>, the script command <c>flowerclock</c>), to the original's five
/// times: 7:25, 6:15, 9:15, 12:45 and 12:30. Which tiles of the face can be walked is the original's table for each
/// time (<c>sEternaGymClockCollision</c>): along the hands, which lie on the paths of the face's cross when the time
/// is right; the hour hand ends a tile short of the ring round the face, and that tile is hopped over along the
/// hand (<c>EternaGym_IsHourHandJumpTile</c>). After the second trainer the right fountain's water drains away, and
/// after the third the left one's, opening the side passages (<c>sEternaGymFountainCollision</c>). The time is the
/// story's <see cref="StateVar"/>, the original's own variable, so it lasts from one visit to the next.
/// </summary>
public sealed class EternaClock : GymPuzzle
{
    public const string PuzzleName = "EternaClock";
    public override string Name => PuzzleName;

    /// <summary>The original's variable for the clock's time (<c>SystemVars_GetEternaGymFlowerClockState</c>): 0 to 4.</summary>
    public const string StateVar = "VAR_ETERNA_GYM_FLOWER_CLOCK_STATE";

    /// <summary>How many of the Gym's three trainers have been beaten, in the order the clock lets them be reached.</summary>
    public const string TrainersVar = "VAR_ETERNA_GYM_TRAINERS_BEATEN";

    public const int Initial = 0, FirstTrainer = 1, SecondTrainer = 2, ThirdTrainer = 3, Leader = 4;

    // ETERNA_CLOCK_* and ETERNA_GYM_* of gym_features.c
    public const int Diameter = 13, Left = 5, Top = 7, CenterX = 11, CenterZ = 13;
    public const int FountainZ = 19, LeftFountainX = 2, RightFountainX = 20, FountainRowWidth = 21;

    /// <summary>The time each state shows (<c>sEternaGymClockTimes</c>), on a twelve-hour face (0 is twelve o'clock).</summary>
    public static readonly (int Hour, int Minute)[] Times = { (7, 25), (6, 15), (9, 15), (0, 45), (0, 30) };

    /// <summary>The tile hopped over at the hour hand's tip in each state, and whether it is hopped north and south (<c>sEternaGymHourHandJumpTiles</c>).</summary>
    public static readonly (int X, int Z, bool NorthSouth)?[] HopTiles =
    {
        null, (11, 18, true), (6, 13, false), (11, 8, true), (11, 8, true)
    };

    // sEternaGymClockCollision: for each state, the face's 13 by 13 tiles from (5, 7), 1 where they are closed
    private static readonly string[][] FaceRows =
    {
        new[]
        {
            "1111111111111", "1111111111111", "1111111111111", "1111111111111", "1111111111111", "1111111111111",
            "1111111111111", "1111111111111", "1111111111111", "1111111111111", "1111111111111", "1111111111111",
            "0110000000110"
        },
        new[]
        {
            "1111111111110", "1111111111111", "1111111111111", "1111111111110", "1111111111110", "1111110111110",
            "1111100000000", "1111110111110", "1111110111110", "1111110111110", "1111110111111", "1111110111111",
            "0110000000110"
        },
        new[]
        {
            "0111111111110", "1111111111111", "1111111111111", "0111111111110", "0111111111110", "0111110111110",
            "0000000000000", "0111110111110", "0111111111110", "0111111111110", "1111111111111", "1111111111111",
            "0110000000110"
        },
        new[]
        {
            "0110000000110", "1111110111111", "1111110111111", "0111110111110", "0111110111110", "0111110111110",
            "0000000011110", "0111110111110", "0111111111110", "0111111111110", "1111111111111", "1111111111111",
            "0110000000110"
        },
        new[]
        {
            "0110000000110", "1111110111111", "1111110111111", "0111110111110", "0111110111110", "0111110111110",
            "0111100011110", "0111110111110", "0111110111110", "0111110111110", "1111110111111", "1111110111111",
            "0110000000110"
        }
    };

    // sEternaGymFountainCollision: row 19 from x = 1, 1 where a fountain's water stands
    private static readonly string[] FountainRows =
    {
        "111100000000000001111",
        "111100000000000001111",
        "111100000000000000000",
        "000000000000000000000",
        "000000000000000000000"
    };

    /// <summary>Whether a tile of the face, or of the fountains' row, is closed in a state (<c>EternaGym_DynamicMapFeaturesCheckCollision</c>).</summary>
    public static bool Closed(int state, int x, int z)
    {
        state = Math.Clamp(state, Initial, Leader);
        if (z >= Top && z < Top + Diameter && x >= Left && x < Left + Diameter && FaceRows[state][z - Top][x - Left] == '1') return true;
        return z == FountainZ && x >= 1 && x <= FountainRowWidth && FountainRows[state][x - 1] == '1';
    }

    /// <summary>Whether a tile is one the clock decides about.</summary>
    public static bool Covers(int x, int z) =>
        (z >= Top && z < Top + Diameter && x >= Left && x < Left + Diameter) || (z == FountainZ && x >= 1 && x <= FountainRowWidth);

    /// <summary>Whether a fountain still has its water in a state: the right one until the second trainer, the left until the third.</summary>
    public static bool HasWater(int state, bool right) => state < (right ? SecondTrainer : ThirdTrainer);

    /// <summary>The state applied to the map last; -1 before the first time.</summary>
    public int State { get; private set; } = -1;

    // The map's own solid flags of the tiles the clock decides about, as its file has them
    private bool[]? baseSolid;

    public override void Apply(Map map, StoryState story)
    {
        int state = Math.Clamp(story.Var(StateVar), Initial, Leader);
        if (state == State) return;
        if (baseSolid == null)
        {
            baseSolid = new bool[map.Width * map.Height];
            for (int z = 0; z < map.Height; z++)
                for (int x = 0; x < map.Width; x++)
                    baseSolid[z * map.Width + x] = map.IsSolid(x, z);
        }
        for (int z = 0; z < map.Height; z++)
            for (int x = 0; x < map.Width; x++)
                if (Covers(x, z)) map.SetSolid(x, z, baseSolid[z * map.Width + x] || Closed(state, x, z));
        State = state;
        if (Turning == null) Show(state);
    }

    public override bool HopsOver(int x, int y, Direction dir)
    {
        if (State < 0 || HopTiles[State] is not { } hop || (hop.X, hop.Z) != (x, y)) return false;
        return hop.NorthSouth ? dir is Direction.Up or Direction.Down : dir is Direction.Left or Direction.Right;
    }

    /// <summary>
    /// Turns the clock on to its next time (<c>EternaGym_AdvanceClockState</c>): the story's state goes up by one.
    /// False, with nothing changed, once the Leader is beaten.
    /// </summary>
    public static bool Advance(StoryState story)
    {
        int state = Math.Clamp(story.Var(StateVar), Initial, Leader);
        if (state >= Leader) return false;
        story.SetVar(StateVar, state + 1);
        return true;
    }

    // ------------------------------------------------------------------ what is shown

    /// <summary>The time the hands show now, while they turn as well as at rest; and whether the hour hand moves with the minutes.</summary>
    public int ShownHour { get; private set; } = Times[0].Hour;
    public int ShownMinute { get; private set; } = Times[0].Minute;
    public bool HourHandTurning { get; private set; }

    /// <summary>How full each fountain is shown: 1 full, 0 drained.</summary>
    public float LeftWater { get; private set; } = 1f;
    public float RightWater { get; private set; } = 1f;

    /// <summary>The clock turning on, when it is (<see cref="Turn"/>); null at rest.</summary>
    public Turn? Turning { get; private set; }

    private void Show(int state)
    {
        (ShownHour, ShownMinute) = Times[state];
        HourHandTurning = false;
        LeftWater = HasWater(state, right: false) ? 1f : 0f;
        RightWater = HasWater(state, right: true) ? 1f : 0f;
    }

    /// <summary>
    /// Where each hand points, in degrees clockwise from twelve o'clock (north): the minute hand by its minutes,
    /// the hour hand at its hour and, while it turns, on by half a degree a minute (<c>EternaGym_UpdateClockHandPositions</c>).
    /// </summary>
    public float MinuteAngle => ShownMinute * 6f;
    public float HourAngle => ShownHour * 30f + (HourHandTurning ? ShownMinute * 0.5f : 0f);

    /// <summary>Starts the hands turning from the time of one state to the next's, as the script's <c>flowerclock</c> does.</summary>
    public Turn StartTurn(int from, int to)
    {
        Show(Math.Clamp(from, Initial, Leader));
        return Turning = new Turn(this, Math.Clamp(to, Initial, Leader));
    }

    /// <summary>
    /// The clock turning to a new time, frame by frame as the original's task does it
    /// (<c>FieldTask_EternaGym_AdvanceClockState</c>): the camera goes to the clock's middle, the minute hand runs on
    /// two minutes a frame (at the original's thirty frames a second) with the hour hand following it until the new
    /// hour is reached, then, after the second and third trainers, the camera goes to the fountain that drains and its
    /// water falls for two seconds. <see cref="Look"/> is where the camera should be.
    /// </summary>
    public sealed class Turn
    {
        public const float FrameSeconds = 1f / 30f;

        private readonly EternaClock clock;
        private readonly int state;
        private readonly int destHour, destMinute;
        private float clockTime;
        private int phase, delay;
        private float stepTime;

        internal Turn(EternaClock clock, int state)
        {
            this.clock = clock;
            this.state = state;
            (destHour, destMinute) = Times[state];
            Look = (CenterX, CenterZ);
        }

        /// <summary>The tile the camera should look at.</summary>
        public (int X, int Y) Look { get; private set; }

        /// <summary>The fountain that drains with this turn, if one does: the right one with the second trainer, the left with the third.</summary>
        public bool? DrainsRight => state switch { SecondTrainer => true, ThirdTrainer => false, _ => null };

        /// <summary>The hands have stopped (the chime), and the camera may go on.</summary>
        public bool HandsStopped { get; private set; }

        /// <summary>The fountain's water is falling now (the sound of the water).</summary>
        public bool Draining { get; private set; }

        public bool IsDone { get; private set; }

        /// <summary>Seconds the camera takes to get where it is sent.</summary>
        public const float CameraSeconds = 0.8f;

        public void Update(float dt)
        {
            if (IsDone) return;
            stepTime += dt;
            while (stepTime >= FrameSeconds && !IsDone)
            {
                stepTime -= FrameSeconds;
                Frame();
            }
        }

        private void Frame()
        {
            switch (phase)
            {
                case 0:
                    // The camera goes to the clock's middle, then eight frames' pause
                    if (++delay < (int)(CameraSeconds / FrameSeconds) + 8) return;
                    delay = 0;
                    phase = 1;
                    return;
                case 1:
                    if (!StepHands()) return;
                    HandsStopped = true;
                    phase = 2;
                    return;
                case 2:
                    if (++delay < 8) return;
                    delay = 0;
                    if (DrainsRight is not { } right)
                    {
                        IsDone = true;
                        return;
                    }
                    Look = (right ? RightFountainX : LeftFountainX, FountainZ);
                    phase = 3;
                    return;
                case 3:
                    // To the fountain, four frames' pause, and sixty frames of falling water
                    if (++delay < (int)(CameraSeconds / FrameSeconds) + 4) return;
                    delay = 0;
                    Draining = true;
                    phase = 4;
                    return;
                case 4:
                    delay++;
                    float level = 1f - Math.Clamp(delay / 60f, 0f, 1f);
                    if (DrainsRight == true) clock.RightWater = level; else clock.LeftWater = level;
                    if (delay < 60) return;
                    Draining = false;
                    IsDone = true;
                    clock.Turning = null;
                    return;
            }
        }

        // EternaGym_UpdateClockHandTimes, with the Leader's state's own rule: the hour hand stays at twelve while the
        // minute hand goes on round from a quarter to the hour to half past
        private bool StepHands()
        {
            int hour = clock.ShownHour, minute = clock.ShownMinute;
            bool leader = state == Leader;
            if (leader && hour == destHour && minute > destMinute) hour = 11;
            if (hour != destHour || minute != destMinute)
            {
                int oldMinute = minute;
                minute += 2;
                while (minute >= 60)
                {
                    minute -= 60;
                    hour++;
                }
                hour %= 12;
                if (hour == destHour)
                {
                    if (oldMinute > minute)
                    {
                        if (oldMinute < destMinute + 60 && minute + 60 > destMinute + 60) minute = destMinute;
                    }
                    else if (oldMinute < destMinute && minute > destMinute) minute = destMinute;
                }
            }
            if (leader) hour = destHour;
            clock.ShownHour = hour;
            clock.ShownMinute = minute;
            clock.HourHandTurning = hour != destHour || (minute > destMinute && !leader);
            bool done = hour == destHour && minute == destMinute;
            if (done && !DrainsRight.HasValue) clock.Turning = null;
            return done;
        }
    }
}
