using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>What the player's hands do in one frame at a roulette table.</summary>
[Flags]
public enum RouletteKeys
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    /// <summary>Place the wager; hurry the ball's result along; close a line; yes to a question (A).</summary>
    Confirm = 16,
    /// <summary>Close a line; no to the question of playing on (B).</summary>
    Cancel = 32
}

/// <summary>What a frame of a table gave rise to, for the screen's sounds.</summary>
public enum RouletteCue
{
    Move, Refused, Bet, Roll, Faster, Bounce, Land, Hit, Jackpot, Miss, Coin,
    ShroomishCry, TaillowCry, Falling, Thud, Flap, Drop
}

/// <summary>Where a sitting at a table stands (Emerald's tasks of <c>roulette.c</c>, by what the player sees).</summary>
public enum RoulettePhase { Betting, SlideOut, Rolling, Showing, SlideBack, Flashing, Waiting, Result, Payout, Cleared, MostCoins, Ask, Broke, Finished }

/// <summary>The ball's course (Emerald's ball sprite's callbacks).</summary>
public enum RouletteBall
{
    Hidden, Start, Fast, Medium, Slow, TryLand, TryAdjacent, Unstick, ShroomishWait, ShroomishBounce, TaillowWait,
    TaillowCarry, TaillowDrop, InSlot
}

/// <summary>Who clears a ball stuck between two squares already hit.</summary>
public enum RouletteHelper { None, Shroomish, Taillow }

/// <summary>Where the helper is in its turn, for the screen.</summary>
public enum RouletteHelperStep { None, Waiting, Shadow, Falling, Shaking, Leaving, FlyingIn, Carrying, FlyingAway }

/// <summary>
/// A sitting at one of Mauville's two roulette tables (plan 21 · H12, part o; Emerald's <c>src/roulette.c</c>), GPU-free and
/// frame by frame at sixty a second, the console's own rate. The wheel of twelve slots turns a degree every one, two or
/// three frames; the ball is thrown onto it at a speed and distance drawn as it starts, rolls inward in three stages
/// (fast, medium, slow) as Emerald's floats have it, and drops into the slot below it when it reaches a slot's middle. A
/// slot already landed on throws it to one side; if that one has been landed on too, the ball sticks between them, and
/// Shroomish drops onto the wheel to shake it loose or Taillow carries it off, by the quarter of the wheel it stuck in,
/// and it comes down in an empty slot a few on (one the player bet on three times in four when that Pokémon is in the
/// team). How far the ball may run beyond the table's own distance depends on the hour, on the balls thrown since the
/// player sat down and on whether Shroomish or Taillow is in the team. Every draw is a sixteen-bit one from the
/// <see cref="Random"/> the table is given, in Emerald's order. The board and its odds are <see cref="RouletteRound"/>.
/// The lines' lengths and the payout's pace are Emerald's frames; nothing waits on a picture.
/// </summary>
public sealed class RouletteWheel
{
    /// <summary>The two helpers, by their species' names (the data's): a team with either changes the ball's run and where it is put down.</summary>
    public const string ShroomishSpecies = "Shroomish", TaillowSpecies = "Taillow";

    public const int FramesPerSecond = 60, Slots = RouletteRound.Squares, DegreesPerSlot = 360 / Slots;
    private const int SlotMiddle = DegreesPerSlot / 2 - 1, SlideFrames = 32, ShowFrames = 60, ResultDelay = 30;
    /// <summary>Ours: a ball that has rolled this long without coming to rest is laid in the first empty slot (never seen in Emerald's tables).</summary>
    public const int RescueFrames = 2400;

    /// <summary>A table's numbers (Emerald's <c>sRouletteTables</c>): the left table's are the first, the right's the second.</summary>
    private sealed record Table(int MinBet, int RandHigh, int RandLow, int WheelSpeed, int WheelDelay, int ShroomishStart, int ShroomishDrop,
        int ShroomishSlowdown, int TaillowDelay, int TaillowRight, int TaillowLeft, int BallSpeed, int TravelDistance, float StuckSpeed);

    private static readonly Table[] Tables =
    {
        new(1, DegreesPerSlot * 2, DegreesPerSlot, 1, 1, 45, 30, 1, 75, 27, 24, 10, 360, -0.5f),
        new(3, DegreesPerSlot, DegreesPerSlot / 2, 1, 0, 75, 60, 2, 0, 54, 48, 10, 270, -1.0f)
    };

    /// <summary>The square of the board each slot of the wheel is (<c>sRouletteSlots</c>): orange Wynaut first, then on round the wheel.</summary>
    public static int SquareOfSlot(int slot) => slot % RouletteRound.Colours * RouletteRound.Pokemon + slot % RouletteRound.Pokemon;

    private readonly Table table;
    private readonly int tableId, hour;
    private readonly bool shroomish, taillow;
    private readonly Random chance;
    private readonly List<RouletteCue> cues = new();

    /// <param name="minBet">The table's wager, 1 (the left table) or 3 (the right).</param>
    /// <param name="hour">The hour of the day, which Emerald reads off its clock for the ball's run.</param>
    /// <param name="hasShroomish">Whether a Shroomish is in the team (not an Egg).</param>
    /// <param name="hasTaillow">Whether a Taillow is.</param>
    public RouletteWheel(int minBet, int coins, int hour, bool hasShroomish, bool hasTaillow, Random chance)
    {
        tableId = minBet >= 3 ? 1 : 0;
        table = Tables[tableId];
        this.hour = ((hour % 24) + 24) % 24;
        shroomish = hasShroomish;
        taillow = hasTaillow;
        this.chance = chance;
        Coins = Math.Clamp(coins, 0, CoinCase.Most);
        Board = new RouletteRound(table.MinBet);
        wheelDelay = table.WheelDelay;
        SelectFirstEmpty();
    }

    // ------------------------------------------------------------------ what the screen reads

    public int MinBet => table.MinBet;
    /// <summary>Whether this is the left table (a Coin a ball) or the right (three).</summary>
    public bool RightTable => tableId == 1;
    public int Coins { get; private set; }
    public RouletteRound Board { get; private set; }
    public RoulettePhase Phase { get; private set; } = RoulettePhase.Betting;
    /// <summary>The balls thrown this round, and since the player sat down (Emerald's <c>tBallNum</c>, <c>tTotalBallNum</c>).</summary>
    public int BallsThisRound => Board.Balls;
    public int BallsThrown { get; private set; }
    /// <summary>The wins in a row this sitting (Emerald keeps the best as a game record).</summary>
    public int WinsInARow { get; private set; }
    public int BestWinsInARow { get; private set; }

    /// <summary>The cursor on the board, by Emerald's selection numbers: 1 to 4 a Pokémon's column, 5, 10 and 15 a colour's row, the rest a square.</summary>
    public int Selection { get; private set; }
    /// <summary>The cursor's row on the board (-1 the Pokémon's heads) and column (-1 the colours' heads).</summary>
    public int SelectedRow => Selection / 5 - 1;
    public int SelectedColumn => Selection % 5 - 1;
    public RouletteRound.Kind SelectedKind => SelectedRow < 0 ? RouletteRound.Kind.Pokemon : SelectedColumn < 0 ? RouletteRound.Kind.Colour : RouletteRound.Kind.Square;
    /// <summary>What the cursor's wager pays, times the table's wager (nought for one all hit).</summary>
    public int SelectedOdds => Board.Multiplier(SelectedKind, Math.Max(0, SelectedRow), Math.Max(0, SelectedColumn));

    /// <summary>The wager placed on the ball in play, and its odds as it was placed.</summary>
    public RouletteRound.Kind BetKind { get; private set; }
    public int BetRow { get; private set; }
    public int BetColumn { get; private set; }
    public int BetOdds { get; private set; }
    public bool Won { get; private set; }
    /// <summary>The square the last ball landed on, or -1.</summary>
    public int WinningSquare { get; private set; } = -1;
    /// <summary>The coins the last ball's win still has to count out.</summary>
    public int PayoutLeft { get; private set; }
    /// <summary>The answer under the cursor while the table asks whether to play on (0 yes, 1 no).</summary>
    public int AskCursor { get; private set; }
    /// <summary>Frames the current phase has run, for the screen.</summary>
    public int PhaseFrames { get; private set; }

    /// <summary>The wheel's angle, 0 to 359 degrees (it turns the other way to the ball).</summary>
    public int WheelAngle { get; private set; }
    public RouletteBall Ball { get; private set; } = RouletteBall.Hidden;
    /// <summary>The ball's angle on the screen (0 the top, 90 the right) and its distance from the wheel's middle, in Emerald's pixels.</summary>
    public int BallAngle { get; private set; }
    public float BallDistance { get; private set; }
    /// <summary>How far the ball is lifted off the wheel (Taillow's carry), in Emerald's pixels.</summary>
    public float BallLift { get; private set; }
    /// <summary>Which slot of the wheel the ball is over, or -1.</summary>
    public int SlotUnderBall { get; private set; } = -1;
    public RouletteHelper Helper { get; private set; }
    public RouletteHelperStep HelperStep { get; private set; }
    /// <summary>Frames the helper's step has run.</summary>
    public int HelperFrames { get; private set; }
    /// <summary>Whether the ball stuck on the wheel's left half (the helper comes from that side).</summary>
    public bool StuckOnLeft { get; private set; }
    /// <summary>The screen's shake while Shroomish lands, in Emerald's pixels.</summary>
    public int Shake { get; private set; }
    /// <summary>How many times a ball was laid in a slot by <see cref="RescueFrames"/> (never, if Emerald's numbers hold).</summary>
    public int Rescued { get; private set; }
    public bool Finished => Phase == RoulettePhase.Finished;

    /// <summary>The sounds of the frame just run.</summary>
    public IReadOnlyList<RouletteCue> Cues => cues;

    // ------------------------------------------------------------------ the wheel and the ball's own numbers

    private int wheelTimer, wheelDelay;
    private float angle, angleSpeed, angleAccel, distance, fallSpeed, fallAccel, restDistance;
    private int travelFast, travelMedium, travelSlow;
    private int counter, slotOffset, carryUntil, wheelAngleOfBall;
    private bool stillStuck, ballStuck, ballUnstuck, landed;
    private int hitSlot = -1, stuckHitSlot, rollFrames, shakeIndex, shakeFrames, shakeRow, leaveX;

    private ushort Draw() => (ushort)chance.Next(65536);

    // ------------------------------------------------------------------ a frame

    /// <summary>One frame: the wheel turns, the table's step runs with what was pressed, and the ball and its helper move.</summary>
    public void Update(RouletteKeys keys)
    {
        cues.Clear();
        if (Finished) return;
        TurnWheel();
        PhaseFrames++;
        switch (Phase)
        {
            case RoulettePhase.Betting:
                Bet(keys);
                break;
            case RoulettePhase.SlideOut:
                if (PhaseFrames >= SlideFrames) { BeginRoll(); }
                break;
            case RoulettePhase.Rolling:
                // Task_RecordBallHit: wait for the ball to come to rest, and for a helper to be done with it
                if (Ball is RouletteBall.Start or RouletteBall.Fast or RouletteBall.Medium or RouletteBall.Slow or RouletteBall.TryLand or RouletteBall.TryAdjacent) break;
                if (ballStuck)
                {
                    if (ballUnstuck && landed) { ballStuck = false; ballUnstuck = false; }
                    break;
                }
                if (!landed) break;
                Record();
                Go(RoulettePhase.Showing);
                break;
            case RoulettePhase.Showing:
                if ((keys & RouletteKeys.Confirm) != 0 && PhaseFrames < ShowFrames) PhaseFrames = ShowFrames;
                if (PhaseFrames > ShowFrames) Go(RoulettePhase.SlideBack);
                break;
            case RoulettePhase.SlideBack:
                if (PhaseFrames >= SlideFrames) Go(RoulettePhase.Flashing);
                break;
            case RoulettePhase.Flashing:
                if (PhaseFrames >= (Won ? 120 : 60)) Go(RoulettePhase.Waiting);
                break;
            case RoulettePhase.Waiting:
                if (PhaseFrames >= ResultDelay)
                {
                    Go(RoulettePhase.Result);
                    cues.Add(!Won ? RouletteCue.Miss : BetOdds == 12 ? RouletteCue.Jackpot : RouletteCue.Hit);
                }
                break;
            case RoulettePhase.Result:
                if (Pressed(keys))
                {
                    if (Won)
                    {
                        WinsInARow++;
                        BestWinsInARow = Math.Max(BestWinsInARow, WinsInARow);
                        PayoutLeft = MinBet * BetOdds;
                        Go(RoulettePhase.Payout);
                    }
                    else
                    {
                        WinsInARow = 0;
                        EndTurn();
                    }
                }
                break;
            case RoulettePhase.Payout:
                // Task_GivePayout: a coin every fourth frame, or the rest when the case is full
                if (PayoutLeft > 0 && (PhaseFrames - 1) % 4 == 0)
                {
                    Coins++;
                    cues.Add(RouletteCue.Coin);
                    PayoutLeft = Coins >= CoinCase.Most ? 0 : PayoutLeft - 1;
                }
                if (PayoutLeft == 0 && PhaseFrames > 1 && Pressed(keys)) EndTurn();
                break;
            case RoulettePhase.Cleared:
                if (Pressed(keys))
                {
                    Board = new RouletteRound(MinBet);
                    Go(Coins >= CoinCase.Most ? RoulettePhase.MostCoins : RoulettePhase.Ask);
                }
                break;
            case RoulettePhase.MostCoins:
                if (Pressed(keys)) Go(RoulettePhase.Ask);
                break;
            case RoulettePhase.Ask:
                if ((keys & (RouletteKeys.Up | RouletteKeys.Down)) != 0) { AskCursor = 1 - AskCursor; cues.Add(RouletteCue.Move); }
                if ((keys & RouletteKeys.Cancel) != 0 || ((keys & RouletteKeys.Confirm) != 0 && AskCursor == 1)) Go(RoulettePhase.Finished);
                else if ((keys & RouletteKeys.Confirm) != 0) { SelectFirstEmpty(); Go(RoulettePhase.Betting); }
                break;
            case RoulettePhase.Broke:
                if (PhaseFrames >= 60 || Pressed(keys)) Go(RoulettePhase.Finished);
                break;
        }
        MoveBall();
        MoveHelper();
    }

    private static bool Pressed(RouletteKeys keys) => (keys & (RouletteKeys.Confirm | RouletteKeys.Cancel)) != 0;

    private void Go(RoulettePhase phase)
    {
        Phase = phase;
        PhaseFrames = 0;
        if (phase == RoulettePhase.Ask) AskCursor = 0;
    }

    // Task_SpinWheel: a degree less every so many frames, round from nought to the top again
    private void TurnWheel()
    {
        if (wheelTimer++ == wheelDelay)
        {
            wheelTimer = 0;
            WheelAngle -= table.WheelSpeed;
            if (WheelAngle < 0) WheelAngle = 360 - table.WheelSpeed;
        }
    }

    // ------------------------------------------------------------------ the wager

    // Task_SelectFirstEmptySquare: the orange row's first square not hit, or the green row's once orange is all hit
    private void SelectFirstEmpty()
    {
        int first = 6, last = 9;
        if (!Board.Open(RouletteRound.Kind.Colour, 0, 0)) { first = 11; last = 14; }
        int id = first;
        for (; id < last; id++)
            if (!Board.IsHit(SquareOf(id))) break;
        Selection = id;
        wheelDelay = 2;
        wheelTimer = 0;
    }

    private static int SquareOf(int id) => (id / 5 - 1) * RouletteRound.Pokemon + id % 5 - 1;

    private void Bet(RouletteKeys keys)
    {
        int before = Selection, id = Selection;
        bool moved = false;
        // ProcessBetGridInput: each way pressed moves in turn, wrapping within the column or the row
        if ((keys & RouletteKeys.Up) != 0) moved |= MoveSelection(ref id, 0);
        if ((keys & RouletteKeys.Down) != 0) moved |= MoveSelection(ref id, 1);
        if ((keys & RouletteKeys.Left) != 0) moved |= MoveSelection(ref id, 2);
        if ((keys & RouletteKeys.Right) != 0) moved |= MoveSelection(ref id, 3);
        Selection = id;
        if (moved && Selection != before) cues.Add(RouletteCue.Move);

        if ((keys & RouletteKeys.Confirm) == 0) return;
        if (SelectedOdds == 0)
        {
            cues.Add(RouletteCue.Refused);
            return;
        }
        // Task_PlaceBet: the odds are fixed now and the wager is taken; Task_StartSpin: the wheel to its spinning pace
        BetKind = SelectedKind;
        BetRow = Math.Max(0, SelectedRow);
        BetColumn = Math.Max(0, SelectedColumn);
        BetOdds = SelectedOdds;
        Coins = Math.Max(0, Coins - MinBet);
        cues.Add(RouletteCue.Bet);
        wheelDelay = RightTable ? 0 : 1;
        wheelTimer = 0;
        Go(RoulettePhase.SlideOut);
    }

    // CanMoveSelectionInDir: up and down wrap within a column (the Pokémon's head included, but not in the colours' column),
    // left and right within a row (the colour's head included, but not along the Pokémon's heads)
    private static bool MoveSelection(ref int id, int way)
    {
        int low, high, original = id;
        if (way < 2)
        {
            low = id % 5;
            high = low + 15;
            if (low == 0) low = 5;
        }
        else
        {
            low = id / 5 * 5;
            high = low + 4;
            if (low == 0) low = 1;
        }
        id += way switch { 0 => -5, 1 => 5, 2 => -1, _ => 1 };
        if (id < low) id = high;
        if (id > high) id = low;
        return id != original;
    }

    // ------------------------------------------------------------------ the throw

    // GetRandomForBallTravelDistance: half of what it gives is the most the ball may run beyond the table's own distance
    private int Spread(int ballsSoFar, int roll)
    {
        bool morning = hour > 3 && hour < 10;
        if (shroomish != taillow)
        {
            if (morning) return ballsSoFar < RouletteRound.BallsPerRound * 2 || (roll & 1) != 0 ? table.RandLow / 2 : 1;
            return (roll & 3) == 0 ? table.RandLow / 2 : table.RandLow;
        }
        if (shroomish && taillow)
        {
            if (hour > 3 && hour < 11) return ballsSoFar < RouletteRound.BallsPerRound || (roll & 1) != 0 ? table.RandLow / 2 : 1;
            return (roll & 1) != 0 && ballsSoFar > RouletteRound.BallsPerRound ? table.RandLow / 4 : table.RandLow / 2;
        }
        if (morning) return (roll & 3) == 0 ? 1 : table.RandLow / 2;
        if ((roll & 3) == 0) return ballsSoFar > RouletteRound.BallsPerRound * 2 ? table.RandLow / 2 : table.RandLow;
        if ((roll & 0x8000) != 0) return ballsSoFar > RouletteRound.BallsPerRound * 2 ? table.RandLow : table.RandHigh;
        return table.RandHigh * 2;
    }

    // Task_InitBallRoll: where the ball starts, how fast and how far
    private void BeginRoll()
    {
        ushort roll = Draw();
        int hundredth = roll % 100;
        int spread = (byte)Spread(BallsThrown, roll);
        int extra = spread == 0 ? 0 : (sbyte)(roll % spread - spread / 2);
        int startPair = hour < 13 ? 0 : 1;
        startPair = hundredth < 80 ? startPair * 2 : (1 - startPair) * 2;
        int[] starts = { 0, 180, 90, 270 };

        int travel = (ushort)(table.TravelDistance + extra);
        travel = (ushort)(travel / 5.0f);
        travelFast = (short)(travel * 3);
        travelMedium = travelSlow = travel;

        angle = starts[(roll & 1) + startPair];
        angleSpeed = table.BallSpeed;
        angleAccel = (angleSpeed * 0.5f - angleSpeed) / travelFast;
        distance = 68f;
        fallAccel = 0f;
        fallSpeed = -(8.0f / travelFast);
        restDistance = 36f;

        landed = ballStuck = ballUnstuck = false;
        hitSlot = -1;
        stuckHitSlot = 0;
        Helper = RouletteHelper.None;
        HelperStep = RouletteHelperStep.None;
        BallLift = 0f;
        rollFrames = 0;
        // Task_RollBall: the next ball, counted
        BallsThrown++;
        Ball = RouletteBall.Start;
        cues.Add(RouletteCue.Roll);
        Go(RoulettePhase.Rolling);
    }

    // ------------------------------------------------------------------ the ball

    private static float Sin(int degrees) => MathF.Sin(degrees * MathF.PI / 180f);
    private static float Cos(int degrees) => MathF.Cos(degrees * MathF.PI / 180f);

    /// <summary>The ball's place on the screen, relative to the wheel's middle (+y down), in Emerald's pixels.</summary>
    public (float X, float Y) BallPosition =>
        (Sin(BallAngle) * BallDistance, -Cos(BallAngle) * BallDistance + BallLift + (Ball == RouletteBall.InSlot ? Shake : 0));

    // UpdateBallPos: the angle on by its speed, the distance in by the fall
    private void Roll()
    {
        angleSpeed += angleAccel;
        angle += angleSpeed;
        if (angle >= 360) angle -= 360f;
        else if (angle < 0f) angle += 360f;
        BallAngle = (short)angle;
        fallSpeed += fallAccel;
        distance += fallSpeed;
        BallDistance = distance;
    }

    // UpdateBallRelativeWheelAngle and UpdateSlotBelowBall
    private int AngleOnWheel()
    {
        int onWheel = WheelAngle > BallAngle ? 360 - WheelAngle + BallAngle : BallAngle - WheelAngle;
        if (onWheel >= 360) onWheel -= 360;
        wheelAngleOfBall = onWheel;
        return onWheel;
    }

    private int SlotBelow() => hitSlot = (int)(AngleOnWheel() / (float)DegreesPerSlot);

    // GetBallDistanceToSlotMidpoint
    private int ToSlotMiddle()
    {
        int into = AngleOnWheel() % DegreesPerSlot;
        if (into == SlotMiddle) return 0;
        return into >= SlotMiddle ? DegreesPerSlot - 1 + SlotMiddle - into : SlotMiddle - into;
    }

    private bool SlotTaken(int slot) => Board.IsHit(SquareOfSlot(slot));

    // LandBall: into the slot below, at its middle, and round with the wheel from now on
    private void LandBall()
    {
        landed = true;
        SlotBelow();
        BallDistance = distance = 30;
        AngleOnWheel();
        wheelAngleOfBall = wheelAngleOfBall / DegreesPerSlot * DegreesPerSlot + 15;
        resting.Add(wheelAngleOfBall);
        Ball = RouletteBall.InSlot;
        cues.Add(RouletteCue.Land);
    }

    private void MoveBall()
    {
        if (Ball is not (RouletteBall.Hidden or RouletteBall.InSlot) && ++rollFrames > RescueFrames) Rescue();
        switch (Ball)
        {
            case RouletteBall.Start:
                counter = 0;
                Roll();
                Ball = RouletteBall.Fast;
                break;
            case RouletteBall.Fast:
                Roll();
                if (distance > 60f) break;
                cues.Add(RouletteCue.Faster);
                fallSpeed = -(20.0f / travelMedium);
                angleAccel = (1.0f - angleSpeed) / travelMedium;
                Ball = RouletteBall.Medium;
                break;
            case RouletteBall.Medium:
                Roll();
                if (distance > 40f) break;
                fallSpeed = -(4.0f / travelSlow);
                angleAccel = -(angleSpeed / travelSlow);
                Ball = RouletteBall.Slow;
                break;
            case RouletteBall.Slow:
                Roll();
                if (angleSpeed > 0.5f) break;
                SlotBelow();
                if (ToSlotMiddle() == 0)
                {
                    angleAccel = 0f;
                    angleSpeed -= (float)table.WheelSpeed / (table.WheelDelay + 1);
                    Ball = RouletteBall.TryLand;
                }
                else if (angleAccel != 0f && angleSpeed < 0f)
                {
                    angleAccel = 0f;
                    angleSpeed = 0f;
                    fallSpeed = (float)(fallSpeed / 1.2);
                }
                break;
            case RouletteBall.TryLand:
                Roll();
                counter = 0;
                SlotBelow();
                if (!SlotTaken(hitSlot)) { LandBall(); break; }
                TrySide();
                break;
            case RouletteBall.TryAdjacent:
                Roll();
                if (counter-- == 16) fallSpeed *= -1.0f;
                if (counter != 0) break;
                if (!stillStuck) { LandBall(); break; }
                cues.Add(RouletteCue.Bounce);
                Stick();
                break;
            case RouletteBall.Unstick:
                // SpriteCB_UnstickBall: the helper is sent for
                Roll();
                Ball = Helper == RouletteHelper.Taillow ? RouletteBall.TaillowWait : RouletteBall.ShroomishWait;
                if (Helper == RouletteHelper.Taillow) carryUntil = (slotOffset - 2) * table.RandHigh + table.TaillowDelay + 45;
                break;
            case RouletteBall.ShroomishWait:
                Roll();
                if (BallAngle != (StuckOnLeft ? 180 : 0)) break;
                {
                    // SpriteCB_UnstickBall_Shroomish: the wheel's jolt bounces the ball out and back as the wheel turns under it
                    float fallDistance = slotOffset * table.RandHigh + (table.RandLow - 1);
                    float speed = (StuckOnLeft ? -1f : 1f) * ((float)slotOffset / table.ShroomishSlowdown);
                    restDistance = distance;
                    fallSpeed = speed;
                    fallAccel = -(speed * 2.0f / fallDistance + 2.0f / (fallDistance * fallDistance));
                    angleSpeed = 0f;
                    counter = 0;
                    Ball = RouletteBall.ShroomishBounce;
                }
                break;
            case RouletteBall.ShroomishBounce:
                Roll();
                counter++;
                if (counter < DegreesPerSlot) break;
                if (StuckOnLeft ? distance >= restDistance - 2.0f : distance <= restDistance - 2.0f)
                {
                    LandBall();
                    fallAccel = fallSpeed = 0f;
                    angleSpeed = -1.0f;
                }
                break;
            case RouletteBall.TaillowWait:
                Roll();
                if (BallAngle != (StuckOnLeft ? 270 : 90)) break;
                counter = 0;
                Ball = RouletteBall.TaillowCarry;
                break;
            case RouletteBall.TaillowCarry:
                // SpriteCB_UnstickBall_TaillowPickUp: lifted off the wheel and held there while the wheel turns under it
                if (counter++ < 45) BallLift--;
                else if (counter >= carryUntil)
                {
                    counter = 0;
                    Ball = RouletteBall.TaillowDrop;
                    cues.Add(RouletteCue.Drop);
                }
                break;
            case RouletteBall.TaillowDrop:
                BallLift = (short)(counter * 0.05f * counter) - 45;
                counter++;
                if (counter >= DegreesPerSlot && BallLift >= 0)
                {
                    BallLift = 0f;
                    LandBall();
                    ballUnstuck = true;
                }
                break;
            case RouletteBall.InSlot:
                // SpriteCB_BallLandInSlot
                BallAngle = WheelAngle + wheelAngleOfBall;
                if (BallAngle >= 360) BallAngle -= 360;
                break;
        }
        SlotUnderBall = Ball == RouletteBall.Hidden ? -1 : (int)(AngleOnWheelNow() / (float)DegreesPerSlot);
    }

    private int AngleOnWheelNow()
    {
        int onWheel = WheelAngle > BallAngle ? 360 - WheelAngle + BallAngle : BallAngle - WheelAngle;
        return onWheel >= 360 ? onWheel - 360 : onWheel;
    }

    // SpriteCB_RollBall_TryLand, the slot taken: the ball falls to one side or the other, and sticks if that one is taken too
    private void TrySide()
    {
        cues.Add(RouletteCue.Bounce);
        bool right = (Draw() & 1) != 0;
        int side;
        if (right)
        {
            angleSpeed = 0f;
            stuckHitSlot = side = (hitSlot + 1) % Slots;
        }
        else
        {
            angleSpeed = table.StuckSpeed * 2.0f;
            side = (hitSlot + Slots - 1) % Slots;
            stuckHitSlot = hitSlot;
        }
        if (SlotTaken(side))
        {
            stillStuck = true;
            counter = table.RandLow;
        }
        else
        {
            stillStuck = false;
            if (RightTable) counter = table.RandHigh;
            else
            {
                counter = table.RandLow;
                angleSpeed = right ? 0.5f : -1.5f;
            }
        }
        fallSpeed = 0.085f;
        Ball = RouletteBall.TryAdjacent;
    }

    // SetBallStuck: which quarter of the wheel the ball stuck in sends for Shroomish or Taillow, and the empty slot it is to
    // come down in, counted on from the one it stuck on
    private void Stick()
    {
        ushort roll = Draw();
        ballStuck = true;
        ballUnstuck = false;
        hitSlot = -1;
        angle = BallAngle;
        fallSpeed = 0f;
        angleSpeed = table.StuckSpeed;

        // Emerald reads its Taillow flag here before setting it, after every ball has cleared it: always fifteen more
        int from = tableId * DegreesPerSlot + 33 + 15;
        bool useTaillow = true;
        StuckOnLeft = true;
        for (int quarter = 0; quarter < 4; quarter++)
        {
            if (from < BallAngle && BallAngle <= from + 90)
            {
                StuckOnLeft = quarter / 2 == 1;
                useTaillow = quarter % 2 == 1;
                break;
            }
            from += 90;
        }
        Helper = useTaillow ? RouletteHelper.Taillow : RouletteHelper.Shroomish;
        cues.Add(useTaillow ? RouletteCue.TaillowCry : RouletteCue.ShroomishCry);

        const int skip = 2;
        int checkTo = 5 + (useTaillow && RightTable ? 6 : skip);
        var candidates = new List<int>();
        int onBet = 0, slot = (stuckHitSlot + skip) % Slots;
        for (int i = skip; i < checkTo; i++)
        {
            if (!SlotTaken(slot))
            {
                candidates.Add(i);
                if (onBet == 0 && InBet(SquareOfSlot(slot))) onBet = i;
            }
            slot = (slot + 1) % Slots;
        }
        if (candidates.Count == 0) candidates.Add(skip);
        bool ownsHelper = useTaillow ? taillow : shroomish;
        slotOffset = ownsHelper && onBet != 0 && roll % 256 < 192 ? onBet : candidates[roll % candidates.Count];
        HelperStep = RouletteHelperStep.Waiting;
        HelperFrames = 0;
        Ball = RouletteBall.Unstick;
    }

    private bool InBet(int square) => BetKind switch
    {
        RouletteRound.Kind.Colour => square / RouletteRound.Pokemon == BetRow,
        RouletteRound.Kind.Pokemon => square % RouletteRound.Pokemon == BetColumn,
        _ => square == BetRow * RouletteRound.Pokemon + BetColumn
    };

    // Ours, never reached by Emerald's numbers: a ball that has rolled a long while is laid in the first empty slot
    private void Rescue()
    {
        Rescued++;
        int slot = 0;
        while (slot < Slots - 1 && SlotTaken(slot)) slot++;
        BallAngle = (WheelAngle + slot * DegreesPerSlot + 15) % 360;
        angle = BallAngle;
        BallLift = 0f;
        LandBall();
        ballStuck = false;
        ballUnstuck = false;
        Helper = RouletteHelper.None;
        HelperStep = RouletteHelperStep.None;
    }

    // ------------------------------------------------------------------ the helpers

    private void MoveHelper()
    {
        Shake = 0;
        if (Helper == RouletteHelper.None || HelperStep == RouletteHelperStep.None) return;
        HelperFrames++;
        var t = table;
        if (Helper == RouletteHelper.Shroomish)
        {
            switch (HelperStep)
            {
                // SpriteCB_Shroomish: its shadow shows once the ball comes round to the table's angle, and it drops at the next
                case RouletteHelperStep.Waiting:
                    if (BallAngle == t.ShroomishStart + (StuckOnLeft ? 180 : 0)) { Step(RouletteHelperStep.Shadow); cues.Add(RouletteCue.Falling); }
                    break;
                case RouletteHelperStep.Shadow:
                    if (BallAngle == t.ShroomishDrop + (StuckOnLeft ? 180 : 0)) Step(RouletteHelperStep.Falling);
                    break;
                // SpriteCB_ShroomishFall: sixty frames down
                case RouletteHelperStep.Falling:
                    if (HelperFrames > 60)
                    {
                        shakeFrames = slotOffset * t.RandHigh + t.RandLow - 1;
                        shakeRow = Math.Clamp((slotOffset - 2) / 2, 0, 2);
                        shakeIndex = 0;
                        Step(RouletteHelperStep.Shaking);
                        cues.Add(RouletteCue.Thud);
                    }
                    break;
                // SpriteCB_ShroomishShakeScreen, then SpriteCB_ShroomishExit: two pixels a frame off the left edge
                case RouletteHelperStep.Shaking:
                    if (HelperFrames <= shakeFrames)
                    {
                        if ((HelperFrames & 1) == 1) shakeIndex = (shakeIndex + 1) % 4;
                        int[] offsets = { -1, 0, 1, 0 };
                        Shake = offsets[shakeIndex] * (shakeRow + 1);
                    }
                    else
                    {
                        leaveX = 36;
                        Step(RouletteHelperStep.Leaving);
                    }
                    break;
                case RouletteHelperStep.Leaving:
                    leaveX -= 2;
                    if (leaveX < -16)
                    {
                        ballUnstuck = true;
                        Step(RouletteHelperStep.None);
                        Helper = RouletteHelper.None;
                    }
                    break;
            }
            return;
        }
        switch (HelperStep)
        {
            // SpriteCB_Taillow: it sets off once the ball comes round to the table's angle
            case RouletteHelperStep.Waiting:
                if (BallAngle == (StuckOnLeft ? t.TaillowLeft + 270 : t.TaillowRight + 90))
                {
                    Step(RouletteHelperStep.FlyingIn);
                    cues.Add(RouletteCue.Falling);
                }
                break;
            case RouletteHelperStep.FlyingIn:
                if (Ball is RouletteBall.TaillowCarry or RouletteBall.TaillowDrop)
                {
                    Step(RouletteHelperStep.Carrying);
                    cues.Add(RouletteCue.Flap);
                    cues.Add(RouletteCue.TaillowCry);
                }
                break;
            case RouletteHelperStep.Carrying:
                if (Ball == RouletteBall.TaillowDrop || Ball == RouletteBall.InSlot) Step(RouletteHelperStep.FlyingAway);
                break;
            case RouletteHelperStep.FlyingAway:
                if (HelperFrames > 100) { Step(RouletteHelperStep.None); Helper = RouletteHelper.None; }
                break;
        }
    }

    private void Step(RouletteHelperStep step)
    {
        HelperStep = step;
        HelperFrames = 0;
    }

    /// <summary>Where Shroomish has got to as it leaves (Emerald's sprite's x, from 36 to -16), for the screen.</summary>
    public int ShroomishLeaving => leaveX;

    // ------------------------------------------------------------------ after the ball

    // RecordHit and IsHitInBetSelection
    private void Record()
    {
        WinningSquare = SquareOfSlot(hitSlot);
        int won = Board.Land(BetKind, BetRow, BetColumn, WinningSquare);
        Won = won > 0;
    }

    // Task_EndTurn and Task_TryPrintEndTurnMsg
    private void EndTurn()
    {
        if (Coins < MinBet) { Go(RoulettePhase.Broke); return; }
        if (Board.Over)
        {
            // Task_ClearBoard (after its line): the balls are taken off the wheel
            Ball = RouletteBall.Hidden;
            resting.Clear();
            Go(RoulettePhase.Cleared);
            return;
        }
        Go(Coins >= CoinCase.Most ? RoulettePhase.MostCoins : RoulettePhase.Ask);
    }

    private readonly List<int> resting = new();

    /// <summary>The balls of this round lying in their slots, each by its angle on the wheel (add <see cref="WheelAngle"/> for the screen's).</summary>
    public IReadOnlyList<int> RestingBalls => resting;
}
