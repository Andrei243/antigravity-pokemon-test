using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a reel of Mauville's machines shows. The order is Emerald's; the names and the pictures are our own.</summary>
public enum ReelSymbol { RedSeven, BlueSeven, Azurill, Lotad, Cherry, Power, Replay }

/// <summary>What the player's hands do in one frame at one of Mauville's machines.</summary>
[Flags]
public enum ReelKeys
{
    None = 0,
    /// <summary>One more coin in (Emerald's Down).</summary>
    Bet = 1,
    /// <summary>Three coins in at once (Emerald's R).</summary>
    MaxBet = 2,
    /// <summary>Stop the next reel; start the spin once a coin is in; yes to a question; close a notice (A).</summary>
    Confirm = 4,
    /// <summary>Ask to leave between games; no to a question; close a notice (B).</summary>
    Cancel = 8,
    /// <summary>A kept down: the coins are counted out twice as fast.</summary>
    Held = 16,
    /// <summary>All the coins paid out at once (Emerald's START).</summary>
    PayAll = 32
}

/// <summary>What a frame of a machine gave rise to, for the screen's sounds.</summary>
public enum ReelCue { Coin, Spin, Stop, Win, Jackpot, Lose, Bolt, ReelTimeBegins, ReelTimeWon, ReelTimeLost, Explosion }

/// <summary>What one of Mauville's machines tells its player.</summary>
public enum ReelNotice { None, NeedThree, MostCoins, NoCoins, Quit }

/// <summary>The steps of Reel Time, Pikachu's bonus machine, for the screen.</summary>
public enum ReelTimeStep { None, Enter, Wait, SpeedUp, Steady, Slow, Check, Land, React, Clear, Close, SetSpeed, Explode, Smoke }

/// <summary>
/// A slot machine of Mauville City's Game Corner (plan 21 · H12, part n): Emerald's <c>src/slot_machine.c</c> frame by
/// frame at sixty a second, with no screen or sound. Three reels of twenty-one symbols run eight pixels a frame (a symbol
/// is twenty-four); one, two or three coins light one, three or five lines; the machine draws its bias as the reels start
/// (a symbol it means to line up, or the sevens, or Reel Time), and each reel, stopped by the player, may run on up to four
/// symbols to line the bias up, or, with no bias or one already failed, to keep every lit line clear. Power symbols charge
/// Pikachu's gauge, up to sixteen bolts; Reel Time draws a number of spins (more likely the fuller the gauge) during which
/// the reels run slower and stop where they are told. Every draw is a sixteen-bit one from the <see cref="Random"/> the
/// machine is given, in Emerald's order. Each function names Emerald's; the waits Emerald spends on its pictures that are
/// not counted in frames there (the flashing lines, the digital display, the smoke) are our own lengths.
/// </summary>
public sealed class ReelMachine
{
    public const int FramesPerSecond = 60, Reels = 3, SymbolsPerReel = 21, SymbolHeight = 24, MaxBet = 3, MaxExtraTurns = 4, MostBolts = 16;
    public const int NormalSpeed = 8, HalfSpeed = 4, QuarterSpeed = 2;
    private const int ReelHeight = SymbolsPerReel * SymbolHeight;
    private const int RtSymbols = 6, RtSymbolHeight = 20, RtReelHeight = RtSymbols * RtSymbolHeight;
    /// <summary>Our own lengths, in frames: the lines flashing before the coins are counted, the bolt joining the gauge, the smoke.</summary>
    public const int FlashFrames = 32, BoltFrames = 24, SmokeFrames = 60;

    private const ReelSymbol R7 = ReelSymbol.RedSeven, B7 = ReelSymbol.BlueSeven, Az = ReelSymbol.Azurill, Lo = ReelSymbol.Lotad,
        Ch = ReelSymbol.Cherry, Pw = ReelSymbol.Power, Rp = ReelSymbol.Replay;

    /// <summary><c>sReelSymbols</c>: the three reels' tapes, from the top.</summary>
    public static readonly ReelSymbol[][] Tapes =
    {
        new[] { R7, Ch, Az, Rp, Pw, Lo, B7, Lo, Ch, Pw, Rp, Az, R7, Pw, Lo, Rp, Az, B7, Pw, Lo, Rp },
        new[] { R7, Ch, Rp, Lo, Az, Ch, Rp, Pw, Pw, Lo, B7, Lo, Rp, Ch, Az, Lo, Rp, Ch, Lo, Rp, Ch },
        new[] { R7, Pw, B7, Rp, Lo, Az, Rp, Lo, Pw, Az, Rp, Lo, Az, Pw, Rp, Lo, Az, Pw, Rp, Lo, Ch }
    };

    /// <summary><c>sReelTimeSymbols</c>: the Reel Time machine's six numbers, from the top.</summary>
    public static readonly int[] ReelTimeTape = { 1, 0, 5, 4, 3, 2 };

    // sInitialReelPositions: by whether the sitting is a lucky game
    private static readonly int[][] FirstPositions = { new[] { 0, 6 }, new[] { 0, 10 }, new[] { 0, 2 } };

    /// <summary><c>sSpecialDrawOdds</c>: the chance in 256 of drawing for a special bias, by the machine's luck and the coins bet.</summary>
    public static readonly int[][] SpecialDrawOdds = { new[] { 1, 1, 12 }, new[] { 1, 1, 14 }, new[] { 2, 2, 14 }, new[] { 2, 2, 14 }, new[] { 2, 3, 16 }, new[] { 3, 3, 16 } };

    // sBiasProbabilities_Special (straight sevens, Reel Time, mixed sevens) and _Regular (power, Azurill, Lotad, cherry, replay), by luck
    private static readonly int[][] SpecialOdds = { new[] { 25, 25, 30, 40, 40, 50 }, new[] { 25, 25, 30, 30, 35, 35 }, new[] { 25, 25, 30, 25, 25, 30 } };
    private static readonly int[][] RegularOdds =
    {
        new[] { 20, 25, 25, 20, 25, 25 }, new[] { 12, 15, 15, 18, 19, 22 }, new[] { 25, 25, 25, 30, 30, 40 },
        new[] { 25, 25, 20, 20, 15, 15 }, new[] { 40, 40, 35, 35, 40, 40 }
    };

    // sReelTimeProbabilities_NormalGame and _LuckyGame: the chance in 256 of each number of spins, by the bolts held
    private static readonly int[][] ReelTimeNormal =
    {
        new[] { 243, 243, 243, 80, 80, 80, 80, 40, 40, 40, 40, 40, 40, 5, 5, 5, 5 },
        new[] { 5, 5, 5, 150, 150, 150, 150, 130, 130, 130, 130, 130, 130, 100, 100, 100, 5 },
        new[] { 4, 4, 4, 20, 20, 20, 20, 80, 80, 80, 80, 80, 80, 100, 100, 100, 40 },
        new[] { 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 45, 45, 45, 100 },
        new[] { 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 5, 5, 5, 100 },
        new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 6 }
    };
    private static readonly int[][] ReelTimeLucky =
    {
        new[] { 243, 243, 243, 200, 200, 200, 200, 160, 160, 160, 160, 160, 160, 70, 70, 70, 5 },
        new[] { 5, 5, 5, 25, 25, 25, 25, 5, 5, 5, 5, 5, 5, 2, 2, 2, 6 },
        new[] { 4, 4, 4, 25, 25, 25, 25, 30, 30, 30, 30, 30, 30, 40, 40, 40, 35 },
        new[] { 2, 2, 2, 3, 3, 3, 3, 30, 30, 30, 30, 30, 30, 100, 100, 100, 50 },
        new[] { 1, 1, 1, 2, 2, 2, 2, 30, 30, 30, 30, 30, 30, 40, 40, 40, 100 },
        new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 4, 60 }
    };
    private static readonly int[] ExplodeOdds = { 128, 175, 200, 225, 256 };                         // sReelTimeExplodeProbability
    private static readonly int[][] SpeedOdds = { new[] { 10, 5 }, new[] { 10, 10 }, new[] { 10, 15 }, new[] { 10, 25 }, new[] { 10, 35 } }; // sReelTimeSpeed_Probabilities
    private static readonly int[] QuarterBoost = { 0, 5, 10, 15, 20 };                               // sQuarterSpeed_ProbabilityBoost
    private static readonly int[] StopShocks = { 2, 4, 4, 4, 8 };                                    // sReelStopShocks

    /// <summary>The machine's biases (Emerald's <c>BIAS_*</c>).</summary>
    [Flags]
    public enum Bias
    {
        None = 0, Replay = 1, Cherry = 2, Lotad = 4, Azurill = 8, Power = 16, ReelTime = 32, MixedSeven = 64, StraightSeven = 128,
        Sevens = StraightSeven | MixedSeven
    }

    // sBiasSymbols, by the bias's bit; sBiasesSpecial; sBiasesRegular
    private static readonly ReelSymbol[] BiasSymbols = { Rp, Ch, Lo, Az, Pw, R7, R7, R7 };
    private static readonly Bias[] SpecialBiases = { Bias.StraightSeven, Bias.ReelTime, Bias.MixedSeven };
    private static readonly Bias[] RegularBiases = { Bias.Power, Bias.Azurill, Bias.Lotad, Bias.Cherry, Bias.Replay };

    /// <summary>A line's win (Emerald's <c>MATCH_*</c>).</summary>
    public enum Match { Cherry, CornerCherry, Replay, Lotad, Azurill, Power, MixedSeven, RedSeven, BlueSeven, None }

    /// <summary><c>sSlotPayouts</c>: what each kind of line pays.</summary>
    public static readonly int[] Payouts = { 2, 4, 0, 6, 12, 3, 90, 300, 300 };

    /// <summary>The five lines, as rows of the window (1 the top) on the three reels: the middle, the top, the bottom and the two diagonals.</summary>
    public static readonly int[][] Lines = { new[] { 2, 2, 2 }, new[] { 1, 1, 1 }, new[] { 3, 3, 3 }, new[] { 1, 2, 3 }, new[] { 3, 2, 1 } };

    private enum S
    {
        Ready, AskBet, BetInput, StartSpin, ReelTime, ResetBias, WaitStop, WaitAllStop, CheckMatches, WaitPayout, MatchedPower,
        NoMatches, Notice, AskQuit, Finished
    }

    private readonly Random chance;
    private S state;
    private readonly int[] pixel = new int[Reels], position = new int[Reels], extraTurns = new int[Reels], winnerRows = new int[Reels];
    private readonly int[] shock = new int[Reels], shockMagnitude = new int[Reels], shockTimer = new int[Reels];
    private readonly ReelMotion[] motion = new ReelMotion[Reels];
    private readonly List<ReelCue> cues = new();
    private int timer, payoutTimer, flashFrames;
    private bool didNotFailBias;
    private ReelSymbol biasSymbol;
    private S afterNotice;

    private enum ReelMotion { Still, Spin, MoveToStop, Shake }

    public ReelMachine(int coins, int luck, Random chance)
    {
        this.chance = chance;
        Luck = Math.Clamp(luck, 0, 5);
        Coins = Math.Clamp(coins, 0, CoinCase.Most);
        // InitSlotMachine
        LuckyGame = (Next16() & 1) != 0;
        for (int i = 0; i < Reels; i++)
        {
            position[i] = FirstPositions[i][LuckyGame ? 1 : 0] % SymbolsPerReel;
            pixel[i] = (ReelHeight - position[i] * SymbolHeight) % ReelHeight;
        }
        state = S.Ready;
    }

    // ------------------------------------------------------------------ what the screen reads

    /// <summary>The machine's luck, 0 (the unluckiest) to 5 (the luckiest): Emerald's machine id.</summary>
    public int Luck { get; }
    public int Coins { get; private set; }
    public int Bet { get; private set; }
    public int Payout { get; private set; }
    /// <summary>The bias drawn for this spin (kept between spins while it is the sevens).</summary>
    public Bias MachineBias { get; private set; }
    /// <summary>Whether this sitting is a lucky game: a blue seven's jackpot makes the next one so, a red seven's not.</summary>
    public bool LuckyGame { get; private set; }
    /// <summary>Pikachu's gauge: the power symbols lined up, up to sixteen.</summary>
    public int Bolts { get; private set; }
    public int ReelTimeSpinsLeft { get; private set; }
    public int ReelTimeSpinsUsed { get; private set; }
    /// <summary>The coins lost and not won back this sitting, to 9999: Reel Time runs the reels slower the more it is.</summary>
    public int NetCoinLoss { get; private set; }
    public int ReelSpeed { get; private set; } = NormalSpeed;
    /// <summary>The reel the next stop is for.</summary>
    public int CurrentReel { get; private set; }
    /// <summary>The kinds of line the last spin won (one bit each by <see cref="Match"/>).</summary>
    public int Matches { get; private set; }
    /// <summary>The lines the last spin won, by their place in <see cref="Lines"/>.</summary>
    public IReadOnlyList<int> WonLines => wonLines;
    private readonly List<int> wonLines = new();
    public ReelNotice Notice { get; private set; }
    public bool Finished => state == S.Finished;
    public bool AskingToQuit => state == S.AskQuit;
    public bool Betting => state == S.BetInput;
    public bool Spinning => state is S.WaitStop or S.WaitAllStop or S.ResetBias or S.StartSpin;
    public bool Paying => state == S.WaitPayout;
    /// <summary>Whether the next press of the confirm button stops the <see cref="CurrentReel"/>.</summary>
    public bool AwaitingStop => state == S.WaitStop;
    /// <summary>Whether the last spin won nothing and the machine is between games.</summary>
    public bool Lost => state == S.NoMatches;
    public bool InReelTime => state == S.ReelTime;
    public ReelTimeStep ReelTimeStep { get; private set; }
    /// <summary>The number Reel Time drew, once it shows (0 for none or before).</summary>
    public int ReelTimeDraw { get; private set; }
    /// <summary>The Reel Time machine's reel, in pixels down its tape of six (twenty to a number).</summary>
    public int ReelTimePixel { get; private set; }
    /// <summary>The frames this machine has run.</summary>
    public long Frame { get; private set; }
    /// <summary>Sounds and sights of the last frame.</summary>
    public IReadOnlyList<ReelCue> Cues => cues;

    /// <summary>A reel's place down its tape, in pixels (a symbol is <see cref="SymbolHeight"/>), and its shudder as it stops.</summary>
    public int PixelOf(int reel) => pixel[reel];
    public int ShockOf(int reel) => shock[reel];
    public bool IsMoving(int reel) => motion[reel] != ReelMotion.Still;

    /// <summary><c>GetSymbolAtRest</c>: the symbol a row of the window shows (1 the top, 2 the middle, 3 the bottom; 0 the one above).</summary>
    public ReelSymbol SymbolAtRest(int reel, int offset)
    {
        int pos = (position[reel] + offset) % SymbolsPerReel;
        if (pos < 0) pos += SymbolsPerReel;
        return Tapes[reel][pos];
    }

    // GetSymbol: as if the reel were snapped down into place
    private ReelSymbol SymbolAt(int reel, int offset) => SymbolAtRest(reel, offset + (pixel[reel] % SymbolHeight != 0 ? -1 : 0));

    /// <summary>The Reel Time machine's number in its window (<c>GetReelTimeSymbol(1)</c>).</summary>
    public int ReelTimeShown => ReelTimeSymbol(1);

    private int ReelTimeSymbol(int offset)
    {
        int at = (RtPosition + offset) % RtSymbols;
        if (at < 0) at += RtSymbols;
        return ReelTimeTape[at];
    }

    private int RtPosition => RtSymbols - ReelTimePixel / RtSymbolHeight;

    private int Next16() => chance.Next(0x10000);

    // ------------------------------------------------------------------ a frame

    /// <summary>One frame, a sixtieth of a second, with what the player's hands did in it.</summary>
    public void Update(ReelKeys keys)
    {
        cues.Clear();
        if (state == S.Finished) return;
        Frame++;
        bool confirm = keys.HasFlag(ReelKeys.Confirm), cancel = keys.HasFlag(ReelKeys.Cancel);
        switch (state)
        {
            case S.Ready: ReadyNewSpin(); break;
            case S.AskBet:
                // SlotTask_AskInsertBet
                state = S.BetInput;
                if (Coins >= CoinCase.Most) Tell(ReelNotice.MostCoins, S.BetInput);
                break;
            case S.BetInput: BetInput(keys); break;
            case S.StartSpin: StartSpin(); break;
            case S.ReelTime: ReelTimeFrame(); break;
            case S.ResetBias:
                // SlotTask_ResetBiasFailure
                if (++timer >= 30)
                {
                    didNotFailBias = MachineBias != Bias.None;
                    state = S.WaitStop;
                }
                break;
            case S.WaitStop:
                if (confirm)
                {
                    cues.Add(ReelCue.Stop);
                    DecideStop(CurrentReel);
                    state = S.WaitAllStop;
                }
                break;
            case S.WaitAllStop:
                if (!IsMoving(CurrentReel))
                {
                    CurrentReel++;
                    state = CurrentReel >= Reels ? S.CheckMatches : S.WaitStop;
                }
                break;
            case S.CheckMatches: CheckMatches(); break;
            case S.WaitPayout: PayoutFrame(keys); break;
            case S.MatchedPower:
                if (++timer >= BoltFrames) AfterPayout(fromPower: true);
                break;
            case S.NoMatches:
                if (++timer > 64) { timer = 0; state = S.Ready; }
                break;
            case S.Notice:
                if (confirm || cancel)
                {
                    var was = Notice;
                    Notice = ReelNotice.None;
                    state = was == ReelNotice.NoCoins ? S.Finished : afterNotice;
                }
                break;
            case S.AskQuit:
                // SlotTask_HandleQuitInput: the cursor stands on yes
                if (confirm)
                {
                    Coins += Bet;
                    Bet = 0;
                    Notice = ReelNotice.None;
                    state = S.Finished;
                }
                else if (cancel)
                {
                    Notice = ReelNotice.None;
                    state = S.BetInput;
                }
                break;
        }
        for (int reel = 0; reel < Reels; reel++) ReelFrame(reel);
    }

    private void Tell(ReelNotice notice, S after)
    {
        Notice = notice;
        afterNotice = after;
        state = S.Notice;
    }

    // SlotTask_ReadyNewSpin
    private void ReadyNewSpin()
    {
        Payout = 0;
        Bet = 0;
        CurrentReel = 0;
        MachineBias &= Bias.Sevens;
        state = S.AskBet;
        if (Coins <= 0) Tell(ReelNotice.NoCoins, S.Finished);
    }

    // SlotTask_HandleBetInput
    private void BetInput(ReelKeys keys)
    {
        if (keys.HasFlag(ReelKeys.MaxBet))
        {
            if (Coins - (MaxBet - Bet) >= 0)
            {
                Coins -= MaxBet - Bet;
                Bet = MaxBet;
                state = S.StartSpin;
                cues.Add(ReelCue.Coin);
            }
            else Tell(ReelNotice.NeedThree, S.BetInput);
            return;
        }
        if (keys.HasFlag(ReelKeys.Bet) && Coins != 0)
        {
            cues.Add(ReelCue.Coin);
            Coins--;
            Bet++;
        }
        if (Bet >= MaxBet || (Bet != 0 && keys.HasFlag(ReelKeys.Confirm))) state = S.StartSpin;
        if (keys.HasFlag(ReelKeys.Cancel))
        {
            Notice = ReelNotice.Quit;
            state = S.AskQuit;
        }
    }

    // SlotTask_StartSpin
    private void StartSpin()
    {
        DrawMachineBias();
        for (int i = 0; i < Reels; i++) motion[i] = ReelMotion.Spin;
        cues.Add(ReelCue.Spin);
        timer = 0;
        ReelSpeed = NormalSpeed;
        if (MachineBias.HasFlag(Bias.ReelTime))
        {
            BeginReelTime();
            state = S.ReelTime;
        }
        else
        {
            state = S.ResetBias;
            if (ReelTimeSpinsLeft > 0) ReelSpeed = ReelTimeSpeed();
        }
    }

    // DrawMachineBias: at most a special bias and, beside Reel Time only, a regular one
    private void DrawMachineBias()
    {
        if (ReelTimeSpinsLeft != 0 || (MachineBias & Bias.Sevens) != 0) return;
        if (ShouldTrySpecialBias())
        {
            int which = TrySelectBias(special: true);
            if (which != SpecialBiases.Length)
            {
                MachineBias |= SpecialBiases[which];
                if (which != 1) return;
            }
        }
        int regular = TrySelectBias(special: false);
        if (regular != RegularBiases.Length) MachineBias |= RegularBiases[regular];
    }

    // ShouldTrySpecialBias: a u8 draw against the machine's odds for the coins bet
    private bool ShouldTrySpecialBias() => SpecialDrawOdds[Luck][Bet - 1] > (Next16() & 0xFF);

    // TrySelectBias_Special and _Regular
    private int TrySelectBias(bool special)
    {
        var odds = special ? SpecialOdds : RegularOdds;
        int which;
        for (which = 0; which < odds.Length; which++)
        {
            int rval = Next16() & 0xFF;
            int value = odds[which][Luck];
            if (!special && which == 0 && LuckyGame) value = Math.Min(value + 10, 0x100);
            else if (!special && which == 4 && LuckyGame) value = Math.Max(value - 10, 0);
            if (value > rval) break;
        }
        return which;
    }

    private static ReelSymbol BiasSymbolOf(Bias bias)
    {
        int bits = (int)bias;
        for (int i = 0; i < 8; i++, bits >>= 1)
            if ((bits & 1) != 0) return BiasSymbols[i];
        return R7;
    }

    // ReelTimeSpeed: slower the more the player has lost this sitting
    private int ReelTimeSpeed()
    {
        int i = NetCoinLoss >= 300 ? 4 : NetCoinLoss >= 250 ? 3 : NetCoinLoss >= 200 ? 2 : NetCoinLoss >= 150 ? 1 : 0;
        if (Next16() % 100 < SpeedOdds[i][0]) return HalfSpeed;
        if (Next16() % 100 < SpeedOdds[i][1] + QuarterBoost[Math.Min(ReelTimeSpinsUsed, QuarterBoost.Length - 1)]) return QuarterSpeed;
        return NormalSpeed;
    }

    // ------------------------------------------------------------------ the reels

    private void AdvanceReel(int reel, int value)
    {
        pixel[reel] = (pixel[reel] + value) % ReelHeight;
        position[reel] = SymbolsPerReel - pixel[reel] / SymbolHeight;
    }

    private int AdvanceToNextSymbol(int reel, int value)
    {
        int offset = pixel[reel] % SymbolHeight;
        if (offset != 0)
        {
            if (offset < value) value = offset;
            AdvanceReel(reel, value);
            offset = pixel[reel] % SymbolHeight;
        }
        return offset;
    }

    // Task_Reel: a reel's frame
    private void ReelFrame(int reel)
    {
        switch (motion[reel])
        {
            case ReelMotion.Spin:
                AdvanceReel(reel, ReelSpeed);
                break;
            case ReelMotion.MoveToStop:
            {
                // ReelTask_MoveToStop
                int at = pixel[reel] % SymbolHeight;
                if (at != 0) at = AdvanceToNextSymbol(reel, ReelSpeed);
                else if (extraTurns[reel] > 0)
                {
                    extraTurns[reel]--;
                    AdvanceReel(reel, ReelSpeed);
                    at = pixel[reel] % SymbolHeight;
                }
                if (at == 0 && extraTurns[reel] == 0)
                {
                    motion[reel] = ReelMotion.Shake;
                    shockMagnitude[reel] = StopShocks[Math.Clamp(shockTimer[reel], 0, StopShocks.Length - 1)];
                    shockTimer[reel] = 0;
                }
                break;
            }
            case ReelMotion.Shake:
                // ReelTask_ShakingStop
                shock[reel] = shockMagnitude[reel];
                shockMagnitude[reel] = -shockMagnitude[reel];
                shockTimer[reel]++;
                if ((shockTimer[reel] & 3) == 0) shockMagnitude[reel] >>= 1;
                if (shockMagnitude[reel] == 0)
                {
                    motion[reel] = ReelMotion.Still;
                    shock[reel] = 0;
                }
                break;
        }
    }

    // ReelTask_DecideStop: in Reel Time a reel stops where it is; otherwise the bias is tried, and failing it (or with none)
    // the reel runs on to keep the lines clear
    private void DecideStop(int reel)
    {
        winnerRows[reel] = 0;
        extraTurns[reel] = 0;
        if (ReelTimeSpinsLeft == 0)
        {
            if (MachineBias == Bias.None || !didNotFailBias || !DecideStopBias(reel))
            {
                didNotFailBias = false;
                DecideStopNoBias(reel);
            }
        }
        motion[reel] = ReelMotion.MoveToStop;
        // tExtraTurns, which picks the stop's shudder, is the turns as decided
        shockTimer[reel] = extraTurns[reel];
    }

    private bool DecideStopBias(int reel) => reel switch
    {
        0 => BiasReel1(),
        1 => Bet == 3 ? BiasReel2Bet3() : BiasReel2Bet1Or2(),
        _ => BiasReel3()
    };

    private void DecideStopNoBias(int reel)
    {
        if (reel == 0) NoBiasReel1();
        else if (reel == 1) { if (Bet == 1) NoBiasReel2Bet1(); else if (Bet == 2) NoBiasReel2Bet2(); else NoBiasReel2Bet3(); }
        else { if (Bet == 1) NoBiasReel3Bet1(); else if (Bet == 2) NoBiasReel3Bet2(); else NoBiasReel3Bet3(); }
    }

    // DecideStop_Bias_Reel1
    private bool BiasReel1()
    {
        var sym2 = BiasSymbolOf(MachineBias);
        var sym1 = sym2;
        if ((MachineBias & Bias.Sevens) != 0) (sym1, sym2) = (R7, B7);
        return Bet == 1 ? BiasReel1Bet1(sym1, sym2) : BiasReel1Bet2Or3(sym1, sym2);
    }

    private bool EitherAtReel1(int pos, ReelSymbol a, ReelSymbol b)
    {
        var sym = SymbolAt(0, pos);
        if (sym != a && sym != b) return false;
        biasSymbol = sym;
        return true;
    }

    private bool CherriesOnScreenReel1(int turns) =>
        SymbolAt(0, 1 - turns) == Ch || SymbolAt(0, 2 - turns) == Ch || SymbolAt(0, 3 - turns) == Ch;

    private bool BiasedTowardCherryOrSevens => (MachineBias & (Bias.Sevens | Bias.Cherry)) != 0;

    private bool BiasReel1Bet1(ReelSymbol a, ReelSymbol b)
    {
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (EitherAtReel1(2 - i, a, b)) { winnerRows[0] = 2; extraTurns[0] = i; return true; }
        return false;
    }

    private bool BiasReel1Bet2Or3(ReelSymbol a, ReelSymbol b)
    {
        bool cherry7 = BiasedTowardCherryOrSevens;
        if (cherry7 || !CherriesOnScreenReel1(0))
            for (int i = 1; i <= 3; i++)
                if (EitherAtReel1(i, a, b)) { winnerRows[0] = i; extraTurns[0] = 0; return true; }
        for (int i = 1; i <= MaxExtraTurns; i++)
        {
            if (!cherry7 && CherriesOnScreenReel1(i)) continue;
            if (!EitherAtReel1(1 - i, a, b)) continue;
            if (i == 1 && (cherry7 || !CherriesOnScreenReel1(3))) { winnerRows[0] = 3; extraTurns[0] = 3; return true; }
            if (i <= 3 && (cherry7 || !CherriesOnScreenReel1(i + 1))) { winnerRows[0] = 2; extraTurns[0] = i + 1; return true; }
            winnerRows[0] = 1;
            extraTurns[0] = i;
            return true;
        }
        return false;
    }

    private bool BiasReel2Bet1Or2()
    {
        int row = winnerRows[0];
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (SymbolAt(1, row - i) == biasSymbol) { winnerRows[1] = row; extraTurns[1] = i; return true; }
        return false;
    }

    private bool BiasReel2Bet3()
    {
        if (BiasReel2Bet1Or2())
        {
            if (winnerRows[0] != 2 && extraTurns[1] > 1 && extraTurns[1] != 4)
                for (int i = 0; i <= MaxExtraTurns; i++)
                    if (SymbolAt(1, 2 - i) == biasSymbol) { winnerRows[1] = 2; extraTurns[1] = i; break; }
            return true;
        }
        if (winnerRows[0] != 2)
            for (int i = 0; i <= MaxExtraTurns; i++)
                if (SymbolAt(1, 2 - i) == biasSymbol) { winnerRows[1] = 2; extraTurns[1] = i; return true; }
        return false;
    }

    private bool BiasReel3()
    {
        var symbol = biasSymbol;
        if (MachineBias.HasFlag(Bias.MixedSeven)) symbol = biasSymbol == R7 ? B7 : R7;
        return Bet == 3 ? BiasReel3Bet3(symbol) : BiasReel3Bet1Or2(symbol);
    }

    private bool BiasReel3Bet1Or2(ReelSymbol symbol)
    {
        int row = winnerRows[1];
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (SymbolAt(2, row - i) == symbol) { winnerRows[2] = row; extraTurns[2] = i; return true; }
        return false;
    }

    private bool BiasReel3Bet3(ReelSymbol symbol)
    {
        if (winnerRows[0] == winnerRows[1]) return BiasReel3Bet1Or2(symbol);
        int row = winnerRows[0] == 1 ? 3 : 1;
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (SymbolAt(2, row - i) == symbol) { extraTurns[2] = i; winnerRows[2] = row; return true; }
        return false;
    }

    // DecideStop_NoBias_Reel1: on until no cherry shows
    private void NoBiasReel1()
    {
        int i = 0;
        while (CherriesOnScreenReel1(i)) i++;
        extraTurns[0] = i;
    }

    private static bool SwitchSevenColour(ref ReelSymbol symbol)
    {
        if (symbol == R7) { symbol = B7; return true; }
        if (symbol == B7) { symbol = R7; return true; }
        return false;
    }

    private void NoBiasReel2Bet1()
    {
        if (winnerRows[0] == 0 || !MachineBias.HasFlag(Bias.StraightSeven)) return;
        var sym = SymbolAt(0, 2 - extraTurns[0]);
        if (!SwitchSevenColour(ref sym)) return;
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (sym == SymbolAt(1, 2 - i)) { winnerRows[1] = 2; extraTurns[1] = i; break; }
    }

    private void NoBiasReel2Bet2()
    {
        if (winnerRows[0] == 0 || !MachineBias.HasFlag(Bias.StraightSeven)) return;
        var sym = SymbolAt(0, winnerRows[0] - extraTurns[0]);
        if (!SwitchSevenColour(ref sym)) return;
        for (int i = 0; i <= MaxExtraTurns; i++)
            if (sym == SymbolAt(1, winnerRows[0] - i)) { winnerRows[1] = winnerRows[0]; extraTurns[1] = i; break; }
    }

    private void NoBiasReel2Bet3()
    {
        if (winnerRows[0] == 0 || !MachineBias.HasFlag(Bias.StraightSeven)) return;
        if (winnerRows[0] == 2) { NoBiasReel2Bet2(); return; }
        var sym = SymbolAt(0, winnerRows[0] - extraTurns[0]);
        if (!SwitchSevenColour(ref sym)) return;
        int j = winnerRows[0] == 3 ? 3 : 2;
        for (int i = 0; i < 2; i++, j--)
            if (sym == SymbolAt(1, j)) { winnerRows[1] = j; extraTurns[1] = 0; return; }
        for (j = 1; j <= MaxExtraTurns; j++)
        {
            if (sym != SymbolAt(1, winnerRows[0] - j)) continue;
            if (winnerRows[0] == 1) (winnerRows[1], extraTurns[1]) = j <= 2 ? (2, j + 1) : (1, j);
            else (winnerRows[1], extraTurns[1]) = j <= 2 ? (3, j) : (2, j - 1);
            return;
        }
    }

    private static bool MismatchedSevens(ReelSymbol a, ReelSymbol b) => (a == R7 && b == B7) || (a == B7 && b == R7);

    private static bool MismatchedSevens(ReelSymbol a, ReelSymbol b, ReelSymbol c) =>
        (a == R7 && b == B7 && c == R7) || (a == B7 && b == R7 && c == B7);

    private static bool NeitherMatchNorMismatch(ReelSymbol a, ReelSymbol b, ReelSymbol c) =>
        !(MismatchedSevens(a, b, c) || (a == R7 && b == R7 && c == B7) || (a == B7 && b == B7 && c == R7) || (a == b && a == c));

    private void NoBiasReel3Bet1()
    {
        int i = 0;
        var a = SymbolAt(0, 2 - extraTurns[0]);
        var b = SymbolAt(1, 2 - extraTurns[1]);
        if (a == b)
        {
            while (true)
            {
                var c = SymbolAt(2, 2 - i);
                if (!(a == c || (a == R7 && c == B7) || (a == B7 && c == R7))) break;
                i++;
            }
        }
        else if (MismatchedSevens(a, b))
        {
            if (MachineBias.HasFlag(Bias.StraightSeven))
                for (i = 0; i <= MaxExtraTurns; i++)
                    if (a == SymbolAt(2, 2 - i)) { extraTurns[2] = i; return; }
            i = 0;
            while (a == SymbolAt(2, 2 - i)) i++;
        }
        extraTurns[2] = i;
    }

    private void NoBiasReel3Bet2()
    {
        int extra = 0;
        if (winnerRows[1] != 0 && winnerRows[0] == winnerRows[1] && MachineBias.HasFlag(Bias.StraightSeven))
        {
            var a = SymbolAt(0, winnerRows[0] - extraTurns[0]);
            var b = SymbolAt(1, winnerRows[1] - extraTurns[1]);
            if (MismatchedSevens(a, b))
                for (int i = 0; i <= MaxExtraTurns; i++)
                    if (a == SymbolAt(2, winnerRows[1] - i)) { extra = i; break; }
        }
        while (true)
        {
            bool found = false;
            for (int row = 1; row <= 3; row++)
            {
                var a = SymbolAt(0, row - extraTurns[0]);
                var b = SymbolAt(1, row - extraTurns[1]);
                var c = SymbolAt(2, row - extra);
                if (!NeitherMatchNorMismatch(a, b, c) && !(MismatchedSevens(a, b, c) && MachineBias.HasFlag(Bias.StraightSeven)))
                {
                    found = true;
                    break;
                }
            }
            if (!found) break;
            extra++;
        }
        extraTurns[2] = extra;
    }

    private void NoBiasReel3Bet3()
    {
        NoBiasReel3Bet2();
        bool straight = MachineBias.HasFlag(Bias.StraightSeven);
        if (winnerRows[1] != 0 && winnerRows[0] != winnerRows[1] && straight)
        {
            var a = SymbolAt(0, winnerRows[0] - extraTurns[0]);
            var b = SymbolAt(1, winnerRows[1] - extraTurns[1]);
            if (MismatchedSevens(a, b))
            {
                int row = winnerRows[0] == 1 ? 3 : 1;
                for (int i = 0; i <= MaxExtraTurns; i++)
                    if (a == SymbolAt(2, row - (extraTurns[2] + i))) { extraTurns[2] += i; break; }
            }
        }
        foreach (var (r1, r3) in new[] { (1, 3), (3, 1) })
            while (true)
            {
                var a = SymbolAt(0, r1 - extraTurns[0]);
                var b = SymbolAt(1, 2 - extraTurns[1]);
                var c = SymbolAt(2, r3 - extraTurns[2]);
                if (NeitherMatchNorMismatch(a, b, c) || (MismatchedSevens(a, b, c) && straight)) break;
                extraTurns[2]++;
            }
    }

    // ------------------------------------------------------------------ the lines and the coins

    /// <summary><c>GetMatchFromSymbols</c>.</summary>
    public static Match MatchOf(ReelSymbol a, ReelSymbol b, ReelSymbol c)
    {
        if (a == b && a == c) return a switch
        {
            R7 => Match.RedSeven, B7 => Match.BlueSeven, Az => Match.Azurill, Lo => Match.Lotad, Ch => Match.Cherry, Pw => Match.Power, _ => Match.Replay
        };
        if ((a == R7 && b == R7 && c == B7) || (a == B7 && b == B7 && c == R7)) return Match.MixedSeven;
        return a == Ch ? Match.Cherry : Match.None;
    }

    // SlotTask_CheckMatches and CheckMatch: the middle line for a coin, the top and bottom for two, the diagonals for three
    private void CheckMatches()
    {
        MachineBias &= Bias.Sevens;
        Matches = 0;
        wonLines.Clear();
        int payout = 0;
        for (int line = 0; line < Lines.Length; line++)
        {
            if (line is 1 or 2 && Bet < 2) continue;
            if (line >= 3 && Bet < 3) continue;
            var rows = Lines[line];
            var match = MatchOf(SymbolAtRest(0, rows[0]), SymbolAtRest(1, rows[1]), SymbolAtRest(2, rows[2]));
            if (match == Match.None) continue;
            wonLines.Add(line);
            if (line is 1 or 2 && match == Match.Cherry) match = Match.CornerCherry;
            // A diagonal's cherry is the top's or the bottom's, already counted
            if (line >= 3 && match == Match.Cherry) continue;
            payout += Payouts[(int)match];
            Matches |= 1 << (int)match;
        }
        Payout = payout;
        if (ReelTimeSpinsLeft > 0)
        {
            ReelTimeSpinsLeft--;
            ReelTimeSpinsUsed++;
        }
        if (Matches != 0)
        {
            state = S.WaitPayout;
            flashFrames = 0;
            payoutTimer = 0;
            NetCoinLoss = Math.Max(0, NetCoinLoss - Payout);
            bool sevens = Has(Match.RedSeven) || Has(Match.BlueSeven);
            cues.Add(sevens || Has(Match.MixedSeven) ? ReelCue.Jackpot : ReelCue.Win);
            if (sevens || Has(Match.MixedSeven))
            {
                MachineBias &= ~Bias.Sevens;
                if (sevens)
                {
                    ReelTimeSpinsLeft = 0;
                    ReelTimeSpinsUsed = 0;
                    LuckyGame = Has(Match.BlueSeven);
                }
            }
            if (Has(Match.Power) && Bolts < MostBolts)
            {
                Bolts++;
                cues.Add(ReelCue.Bolt);
            }
        }
        else
        {
            cues.Add(ReelCue.Lose);
            state = S.NoMatches;
            timer = 0;
            NetCoinLoss = Math.Min(9999, NetCoinLoss + Bet);
        }
    }

    /// <summary>Whether the last spin won a line of a kind.</summary>
    public bool Has(Match match) => (Matches & (1 << (int)match)) != 0;

    // PayoutTask_GivePayout, once the lines have flashed: a coin every eight frames, every four with A held, all with START
    private void PayoutFrame(ReelKeys keys)
    {
        if (flashFrames < FlashFrames) { flashFrames++; return; }
        if (Payout > 0)
        {
            if (payoutTimer-- == 0)
            {
                cues.Add(ReelCue.Coin);
                Payout--;
                if (Coins < CoinCase.Most) Coins++;
                payoutTimer = keys.HasFlag(ReelKeys.Held) ? 4 : 8;
            }
            if (keys.HasFlag(ReelKeys.PayAll))
            {
                cues.Add(ReelCue.Coin);
                Coins = Math.Min(CoinCase.Most, Coins + Payout);
                Payout = 0;
            }
        }
        if (Payout == 0) AfterPayout(fromPower: false);
    }

    // SlotTask_EndPayout and SlotTask_MatchedPower: a replay spins again on the same coins
    private void AfterPayout(bool fromPower)
    {
        timer = 0;
        if (!fromPower && Has(Match.Power)) { state = S.MatchedPower; return; }
        if (Has(Match.Replay))
        {
            CurrentReel = 0;
            state = S.StartSpin;
            return;
        }
        state = S.Ready;
    }

    // ------------------------------------------------------------------ Reel Time

    private int rtTimer, rtWindow, rtSpeed, rtChecks, rtShake, rtTarget;

    // BeginReelTime and ReelTime_Init: the number is drawn as the window opens
    private void BeginReelTime()
    {
        ReelTimeSpinsLeft = 0;
        ReelTimePixel = 0;
        ReelTimeDraw = 0;
        rtTimer = 0;
        rtWindow = 0;
        rtChecks = 0;
        rtSpeed = 1280;
        ReelTimeStep = ReelTimeStep.Enter;
        cues.Add(ReelCue.ReelTimeBegins);
        GetReelTimeDraw();
    }

    // GetReelTimeDraw: a draw for none first, then for five down to one, and none if they all miss
    private void GetReelTimeDraw()
    {
        drawn = 0;
        if ((Next16() & 0xFF) < ReelTimeOdds(0)) return;
        int spins;
        for (spins = 5; spins > 0; spins--)
            if ((Next16() & 0xFF) < ReelTimeOdds(spins)) break;
        drawn = spins;
    }

    private int drawn;

    /// <summary>The number the Reel Time machine has drawn and will land on (known from the start, shown at the end).</summary>
    public int ReelTimeDrawn => drawn;

    private int ReelTimeOdds(int spins) => (LuckyGame ? ReelTimeLucky : ReelTimeNormal)[spins][Math.Clamp(Bolts, 0, MostBolts)];

    private void AdvanceRt(int value)
    {
        ReelTimePixel = (ReelTimePixel + value) % RtReelHeight;
    }

    private int AdvanceRtToNext(int value)
    {
        int offset = ReelTimePixel % RtSymbolHeight;
        if (offset != 0)
        {
            if (offset < value) value = offset;
            AdvanceRt(value);
            offset = ReelTimePixel % RtSymbolHeight;
        }
        return offset;
    }

    // Task_ReelTime, a step at a frame
    private void ReelTimeFrame()
    {
        switch (ReelTimeStep)
        {
            case ReelTimeStep.Enter:
                rtWindow += 8;
                AdvanceRt(rtSpeed >> 8);
                if (rtWindow >= 200) { ReelTimeStep = ReelTimeStep.Wait; rtTimer = 0; }
                break;
            case ReelTimeStep.Wait:
                AdvanceRt(rtSpeed >> 8);
                if (++rtTimer >= 60) ReelTimeStep = ReelTimeStep.SpeedUp;
                break;
            case ReelTimeStep.SpeedUp:
                AdvanceRt(rtSpeed >> 8);
                rtSpeed -= 4;
                if (rtSpeed <= 0x100) { rtSpeed = 0x100; rtTimer = 0; ReelTimeStep = ReelTimeStep.Steady; }
                break;
            case ReelTimeStep.Steady:
                AdvanceRt(rtSpeed >> 8);
                if (++rtTimer >= 80) { rtTimer = 0; ReelTimeStep = ReelTimeStep.Slow; }
                break;
            case ReelTimeStep.Slow:
                AdvanceRt(rtSpeed >> 8);
                rtSpeed = (rtSpeed & 0xFF) + 0x80;
                if (++rtTimer >= 80) { rtTimer = 0; ReelTimeStep = ReelTimeStep.Check; }
                break;
            case ReelTimeStep.Check:
                // ReelTime_CheckExplode: Emerald compares the spins left, nought here, with the checks made, so a draw lands at the first
                AdvanceRt(rtSpeed >> 8);
                rtSpeed = (rtSpeed & 0xFF) + 0x40;
                if (++rtTimer >= 40)
                {
                    rtTimer = 0;
                    if (drawn != 0) { if (ReelTimeSpinsLeft <= rtChecks) ReelTimeStep = ReelTimeStep.Land; }
                    else if (rtChecks > 3) ReelTimeStep = ReelTimeStep.Land;
                    else if ((Next16() & 0xFF) < ExplodeOdds[rtChecks])
                    {
                        ReelTimeStep = ReelTimeStep.Explode;
                        rtShake = 4;
                        cues.Add(ReelCue.Explosion);
                    }
                    rtChecks++;
                }
                break;
            case ReelTimeStep.Land:
            {
                int offset = ReelTimePixel % RtSymbolHeight;
                if (offset != 0)
                {
                    offset = AdvanceRtToNext(rtSpeed >> 8);
                    rtSpeed = (rtSpeed & 0xFF) + 0x40;
                }
                else if (ReelTimeSymbol(1) != drawn)
                {
                    AdvanceRt(rtSpeed >> 8);
                    offset = ReelTimePixel % RtSymbolHeight;
                    rtSpeed = (rtSpeed & 0xFF) + 0x40;
                }
                if (offset == 0 && ReelTimeSymbol(1) == drawn)
                {
                    rtSpeed = 0;
                    rtTimer = 0;
                    ReelTimeStep = ReelTimeStep.React;
                }
                break;
            }
            case ReelTimeStep.React:
                if (++rtTimer >= 60)
                {
                    ReelTimeDraw = drawn;
                    rtTimer = drawn == 0 ? 0xA0 : 0xC0;
                    if (drawn != 0) Bolts = 0;
                    cues.Add(drawn == 0 ? ReelCue.ReelTimeLost : ReelCue.ReelTimeWon);
                    ReelTimeStep = ReelTimeStep.Clear;
                }
                break;
            case ReelTimeStep.Clear:
                if (rtTimer == 0 || --rtTimer == 0) { rtWindow = 0; ReelTimeStep = ReelTimeStep.Close; }
                break;
            case ReelTimeStep.Close:
                rtWindow += 8;
                if (rtWindow >> 3 > 25) EndReelTime();
                break;
            case ReelTimeStep.SetSpeed:
                // ReelTime_SetReelSpeed: the reels slowed by halves to the speed drawn, on the left reel's symbol
                if (ReelSpeed == rtTarget) FinishReelTime();
                else if (pixel[0] % SymbolHeight == 0 && (++rtTimer & 7) == 0) ReelSpeed >>= 1;
                break;
            case ReelTimeStep.Explode:
                // ReelTime_WaitExplode: the window shakes, halving every 32 frames
                if ((rtTimer & 1) != 0) rtShake = -rtShake;
                if ((++rtTimer & 0x1F) == 0) rtShake >>= 1;
                if (rtShake == 0) { rtTimer = 0; ReelTimeStep = ReelTimeStep.Smoke; }
                break;
            case ReelTimeStep.Smoke:
                if (++rtTimer >= SmokeFrames)
                {
                    ReelTimeSpinsUsed = 0;
                    ReelTimeSpinsLeft = 0;
                    ReelSpeed = NormalSpeed;
                    FinishReelTime();
                }
                break;
        }
    }

    /// <summary>The Reel Time window's shudder as the machine blows up, in pixels.</summary>
    public int ReelTimeShake => ReelTimeStep == ReelTimeStep.Explode ? rtShake : 0;

    /// <summary>How far the Reel Time window has slid in (0 to 200) or, closing, out.</summary>
    public int ReelTimeWindow => ReelTimeStep switch
    {
        ReelTimeStep.Enter => rtWindow,
        ReelTimeStep.Close => Math.Max(0, 200 - rtWindow),
        ReelTimeStep.None or ReelTimeStep.SetSpeed => 0,
        _ => 200
    };

    // ReelTime_DestroySprites
    private void EndReelTime()
    {
        ReelTimeSpinsUsed = 0;
        ReelTimeSpinsLeft = drawn;
        ReelSpeed = NormalSpeed;
        if (ReelTimeSpinsLeft == 0) { FinishReelTime(); return; }
        rtTarget = ReelTimeSpeed();
        rtTimer = 0;
        ReelTimeStep = ReelTimeStep.SetSpeed;
    }

    // SlotTask_StartReelTimeSpin: the window gone, the reels wait to be stopped
    private void FinishReelTime()
    {
        ReelTimeStep = ReelTimeStep.None;
        MachineBias &= ~Bias.ReelTime;
        timer = 0;
        state = S.ResetBias;
    }

    // ------------------------------------------------------------------ the twelve machines

    // GetSlotMachineId: sSlotMachineRandomSeeds and sSlotMachineIds
    private static readonly int[] Seeds = { 12, 2, 4, 5, 1, 8, 7, 11, 3, 10, 9, 6 };
    private static readonly int[] Lucks = { 0, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 5 };

    /// <summary>
    /// <c>GetSlotMachineId</c>: a machine's luck that day. Emerald adds its seed to Dewford's trend, which changes as people
    /// talk about it; here the day's number stands in, so every machine has its day (<c>docs/mechanics/rulings.md</c>).
    /// </summary>
    public static int LuckOf(int machine, uint dailyNumber) =>
        Lucks[(int)((dailyNumber + (uint)Seeds[Math.Clamp(machine, 0, Seeds.Length - 1)]) % (uint)Seeds.Length)];

    /// <summary>For tests: the bias the machine holds, as if drawn (Emerald's draw is <see cref="DrawMachineBias"/>).</summary>
    internal void Rig(Bias bias) => MachineBias = bias;

    /// <summary>For tests: the gauge as if so many power lines had been won.</summary>
    internal void RigBolts(int bolts) => Bolts = Math.Clamp(bolts, 0, MostBolts);
}
