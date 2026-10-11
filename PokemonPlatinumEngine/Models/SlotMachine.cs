using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a reel shows. The numbers are the original's; the names and the pictures are our own.</summary>
public enum SlotSymbol { RedSeven, BlueSeven, Replay, Cherry, Ball, Moon, None }

/// <summary>What the player's hands do in one frame at a machine.</summary>
[Flags]
public enum SlotKeys
{
    None = 0,
    /// <summary>Bet (the original's X), and pay out at once while coins are counted.</summary>
    Bet = 1,
    /// <summary>Pull the lever (Down; Bet does it too).</summary>
    Lever = 2,
    Stop1 = 4, Stop2 = 8, Stop3 = 16,
    /// <summary>Leave the machine (START).</summary>
    Quit = 32,
    /// <summary>Close a notice (A or B).</summary>
    Confirm = 64,
    /// <summary>A button kept down: the coins are counted twice as fast.</summary>
    Held = 128
}

/// <summary>What a machine tells its player before stopping.</summary>
public enum SlotNotice { None, NoCoins, TooFewCoins, CaseFull }

/// <summary>Sounds and sights a frame gave rise to, for the screen.</summary>
public enum SlotCue { Bet, Start, Stop, StopBonus, Coin, Tell, BonusBegins, Jackpot, RoundBegins, Cry, MoonRed, MoonBright, RoundContinues, BonusEnds }

/// <summary>The moon over a bonus game: nothing, red (winning this game tires Clefairy), or bright (another round is certain).</summary>
public enum SlotMoon { Plain, Red, Bright }

/// <summary>
/// A slot machine of the Veilstone Game Corner (plan 06 · R16b), the original's <c>overlay101</c> frame by frame at
/// thirty a second, with no screen or sound: three reels of twenty-one symbols turning half a symbol a frame, the
/// draw made as the lever is pulled, the reels slipped up to four symbols (or all the way round, for a prize the draw
/// marks so, while they are stopped left to right) to bring the drawn symbol onto a line or to keep every line
/// clear, five lines, the replay, the bonus that waits for three sevens, and the bonus game: rounds of fifteen
/// games at a coin each in which Clefairy points out the order to stop the reels in, a replay on the middle line
/// paying fifteen, and a chance by the kind of bonus that another round follows. Each function names the original's.
/// What the mascot does on screen is not here; the waits the original spends on it are our own lengths.
/// </summary>
public sealed class SlotMachine
{
    public const int Symbols = 21, SymbolPixels = 32, MostCoins = CoinCase.Most, FramesPerSecond = 30;
    public const int Bet = 3, BonusBet = 1, BonusGames = 15, Speed = 16;
    private const int Strip = Symbols * SymbolPixels;

    /// <summary><c>Unk_ov101_021D9688</c>: the three reels, from the top.</summary>
    public static readonly int[][] Reels =
    {
        new[] { 5, 3, 1, 0, 2, 4, 5, 3, 1, 2, 4, 5, 3, 1, 2, 4, 5, 2, 0, 2, 4 },
        new[] { 3, 0, 1, 5, 4, 2, 3, 0, 5, 4, 2, 3, 0, 5, 4, 2, 3, 1, 5, 4, 2 },
        new[] { 4, 2, 1, 0, 2, 5, 4, 2, 1, 3, 5, 4, 2, 1, 3, 5, 4, 2, 0, 3, 5 }
    };

    /// <summary><c>Unk_ov101_021D9550</c> and <c>Unk_ov101_021D94F0</c>: a line's coins by its symbol, in the game and in the bonus game.</summary>
    public static readonly int[] Pays = { 100, 100, 0, 2, 10, 15 }, BonusPays = { 100, 100, 15, 2, 10, 15 };

    // By the machine's mood, 0 to 5 (sub_0203E484 deals the twelve machines theirs each day)
    private static readonly int[] HitChance = { 25, 25, 30, 30, 35, 35 };                     // Unk_ov101_021D9520
    private static readonly int[][] HitTable =                                                 // Unk_ov101_021D95C8
    {
        new[] { 1, 4, 2, 13, 1, 39, 1 }, new[] { 1, 4, 2, 13, 1, 39, 1 }, new[] { 2, 3, 4, 11, 2, 38, 2 },
        new[] { 3, 2, 4, 11, 2, 38, 2 }, new[] { 4, 1, 6, 9, 3, 37, 3 }, new[] { 4, 1, 6, 9, 3, 37, 3 }
    };
    private static readonly int[] TellChance = { 80, 80, 80, 80, 80, 70 };                    // Unk_ov101_021D94D8
    // Unk_ov101_021D9934: the kind of bonus drawn (weight, kind, entrance)
    private static readonly (int Weight, int Kind, int Entrance)[][] BonusKinds =
    {
        new[] { (5, 3, 0), (10, 1, 1), (10, 5, 2), (10, 2, 2), (15, 4, 1), (50, 0, 0) },
        new[] { (5, 3, 0), (10, 1, 1), (10, 5, 2), (10, 2, 2), (15, 4, 1), (50, 0, 0) },
        new[] { (5, 3, 0), (15, 1, 1), (10, 5, 2), (15, 2, 2), (15, 4, 1), (40, 0, 0) },
        new[] { (5, 3, 0), (15, 1, 1), (15, 5, 2), (15, 2, 2), (10, 4, 1), (40, 0, 0) },
        new[] { (5, 3, 0), (20, 1, 1), (15, 5, 2), (20, 2, 2), (10, 4, 1), (30, 0, 0) },
        new[] { (5, 3, 0), (20, 1, 1), (15, 5, 2), (20, 2, 2), (10, 4, 1), (30, 0, 0) }
    };
    /// <summary><c>Unk_ov101_021D9568</c>: the chance in a hundred that another round follows, by the kind of bonus.</summary>
    public static readonly int[] Stamina = { 50, 70, 90, 50, 70, 90 };
    // Unk_ov101_021D9784: how the bonus is announced (by the entrance: weights of the three ways), the same for every mood
    private static readonly int[][] Entrances = { new[] { 5, 20, 75 }, new[] { 20, 60, 20 }, new[] { 75, 20, 5 } };
    private static readonly int[] SmallChance = { 25, 25, 30, 30, 35, 35 };                   // Unk_ov101_021D9538
    private static readonly int[] SmallTable = { 5, 15, 30, 50 };                             // Unk_ov101_021D9628 (every mood's)
    private static readonly int[] BrightChance = { 1, 1, 3, 3, 5, 5 };                        // Unk_ov101_021D9580
    private static readonly int[] RedChance = { 3, 3, 5, 5, 7, 7 };                           // Unk_ov101_021D9508
    private static readonly int[][] Tiring = { new[] { 25, 20 }, new[] { 20, 15 }, new[] { 15, 15 }, new[] { 15, 15 }, new[] { 10, 15 }, new[] { 5, 10 } }; // Unk_ov101_021D9598
    // Unk_ov101_021D9AE4: how Clefairy looks as a round ends, by what stamina is left (at least, tired, plain)
    private static readonly (int AtLeast, int Tired, int Plain)[] Moods =
    {
        (90, 5, 5), (80, 10, 10), (60, 30, 10), (50, 40, 10), (30, 50, 20), (20, 50, 30), (10, 40, 50), (10, 30, 60), (10, 10, 80)
    };
    /// <summary><c>Unk_ov101_021D87A8</c>: the six orders Clefairy may point the reels out in.</summary>
    public static readonly int[][] Orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };

    /// <summary>The draw's marks (<c>unk_74</c>): a symbol, and whether the bonus comes with it.</summary>
    [Flags]
    public enum Draw
    {
        None = 0, Replay = 1, ReplayBonus = 2, Cherry = 4, CherryBonus = 8, Ball = 16, BallBonus = 32, Moon = 64, MoonBonus = 128,
        BlueSevens = 256, RedSevens = 512,
        Bonus = ReplayBonus | CherryBonus | BallBonus | MoonBonus, Wide = Replay | ReplayBonus | Ball | BallBonus
    }

    private enum S
    {
        Begin = 0, WaitBet = 1, WaitLever = 2, Pull = 3, SpinUp = 4, Stops = 5, Settle = 6, Judge = 7, Miss = 8, MissWait = 9,
        MissTold = 10, MissToldWait = 11, Pay = 12, PayWait = 13, Replay = 14, ReplayWait = 15,
        BonusUnlined = 16, BonusUnlinedWait = 17, BonusUnlinedShown = 18, BonusLined = 19, BonusLinedWait = 20,
        BonusReplay = 21, BonusReplayWait = 22, ToChance = 23, ToChanceFree = 24,
        ChanceBegin = 25, ChanceWaitBet = 26, ChanceWaitLever = 27, ChancePull = 28, ChanceSpinUp = 29, ChanceStops = 30,
        ChanceSettle = 31, ChanceJudge = 32, ChanceMiss = 33, ChanceMissWait = 34, ChancePay = 35, ChancePayWait = 36,
        ChanceReplay = 37, ChanceReplayWait = 38, Jackpot = 39, JackpotWait = 40, BonusSetUp = 41,
        RoundBegin = 42, GameBegin = 43, GameWaitBet = 44, GameWaitLever = 45, GamePull = 46, GameSpinUp = 47, GameStops = 48,
        GameSettle = 49, GameJudge = 50, GameMiss = 51, GamePay = 52, GamePayWait = 53, RoundEnd = 54, RoundOn = 55,
        RoundOnBright = 56, RoundOnTired = 57, BonusOver = 58, SayTooFew = 59, SayNone = 60, SayFull = 61, Notice = 62, Leave = 63
    }

    private readonly Random rng;
    private S state = S.Begin, afterNotice;
    private int wait;

    // The reels: where each strip stands in pixels, the symbol above its top row, what its task is doing
    private readonly int[] position = new int[3], index = new int[3], bounce = new int[3];
    private readonly int[] task = new int[3], slips = new int[3], landedRow = new int[3], settle = new int[3], settleStep = new int[3];
    private readonly bool[] pressed = new bool[3];
    private readonly List<int> order = new();
    private bool aim, wide;

    // The mascot's part, as waits: frames left of what the game is waiting for
    private int show;
    // The coins counted out: 0 idle, 1 starting, 2 the lines flashing, 3 counting
    private int counting, countTick, lineFlash;
    private bool counted;

    private int stamina, kind;

    /// <summary>The machine's mood, 0 to 5: the better, the oftener it pays.</summary>
    public int Mood { get; }
    public int Coins { get; private set; }
    /// <summary>Coins won and still being counted into the case.</summary>
    public int Payout { get; private set; }
    public Draw Drawn { get; private set; }
    /// <summary>The lines that paid: bit 0 the middle, 1 the top, 2 the bottom, 3 the diagonal down, 4 the diagonal up.</summary>
    public int Lines { get; private set; }
    public SlotSymbol Lined { get; private set; } = SlotSymbol.None;
    /// <summary>What the machine lets on before the reels are stopped: a symbol, or <see cref="SlotSymbol.None"/> for a sign that says only that something was drawn.</summary>
    public SlotSymbol? Tell { get; private set; }
    /// <summary>The bonus has been won and waits for three sevens of this colour.</summary>
    public SlotSymbol? Awaited => state is >= S.ToChance and <= S.JackpotWait ? (kind <= 2 ? SlotSymbol.RedSeven : SlotSymbol.BlueSeven) : null;
    public bool InBonus { get; private set; }
    /// <summary>Games left of the bonus round under way, this one among them.</summary>
    public int GamesLeft { get; private set; }
    /// <summary>Rounds of the bonus under way, and the most in a row at this sitting (<c>unk_1C</c>, <c>unk_20</c>).</summary>
    public int Rounds { get; private set; }
    public int MostRounds { get; private set; }
    /// <summary>Coins the bonus under way has brought.</summary>
    public int BonusCoins { get; private set; }
    public SlotMoon Moon { get; private set; }
    /// <summary>The order Clefairy points the reels out in, this game (an entry of <see cref="Orders"/>).</summary>
    public int PointedOrder { get; private set; }
    public SlotNotice Notice { get; private set; }
    public bool Finished => state == S.Leave && left;
    private bool left;
    /// <summary>Bonus rounds begun at this sitting (the original's game record), and coins bet.</summary>
    public int RoundsPlayed { get; private set; }
    public int CoinsBet { get; private set; }

    public bool WaitsForBet => state is S.WaitBet or S.ChanceWaitBet or S.GameWaitBet;
    public bool WaitsForLever => state is S.WaitLever or S.ChanceWaitLever or S.GameWaitLever;
    public bool Spinning => state is S.Stops or S.ChanceStops or S.GameStops;
    public bool Stopped(int reel) => task[reel] == 0;
    public bool Pressed(int reel) => pressed[reel];
    public int Presses => order.Count;

    private readonly List<SlotCue> cues = new();
    public IReadOnlyList<SlotCue> Cues => cues;

    public SlotMachine(int coins, int mood, Random rng)
    {
        Coins = Math.Clamp(coins, 0, MostCoins);
        Mood = Math.Clamp(mood, 0, 5);
        this.rng = rng;
        // ov101_021D1A68
        for (int i = 0; i < 3; i++)
        {
            int at = rng.Next(Symbols);
            index[i] = at;
            position[i] = (Strip - at * SymbolPixels) % Strip;
        }
    }

    /// <summary><c>sub_0203E484</c>: the moods of the twelve machines, dealt anew from the day's number.</summary>
    public static int MoodOf(int machine, uint dayNumber)
    {
        int[] moods = { 0, 5, 1, 1, 4, 4, 2, 2, 2, 3, 3, 3 };
        // The original seeds the console's generator with the day's number and draws from it; ours is seeded the same way
        var deal = new Random(unchecked((int)dayNumber));
        for (int i = 0; i < moods.Length; i++)
            for (int j = i + 1; j < moods.Length; j++)
            {
                int other = deal.Next(moods.Length);
                (moods[i], moods[other]) = (moods[other], moods[i]);
            }
        return moods[Math.Clamp(machine, 0, moods.Length - 1)];
    }

    /// <summary>The symbol a reel shows in a row (1 the top, 3 the bottom), or the one on its way there while the reel is between two (<c>ov101_021D55D4</c>).</summary>
    public SlotSymbol Shows(int reel, int row) => (SlotSymbol)Sym(reel, row);

    /// <summary>Where a reel's strip stands: the symbol above its top row and how far, in pixels, the strip has moved on from there; and its bounce as it settles.</summary>
    public (int Above, int Pixels, float Bounce) Reel(int reel) => (index[reel] % Symbols, position[reel] % SymbolPixels, bounce[reel] / 4096f);

    private int Sym(int reel, int row)
    {
        if (position[reel] % SymbolPixels != 0) row--;
        int at = (index[reel] + row) % Symbols;
        return Reels[reel][at < 0 ? at + Symbols : at];
    }

    // ov101_021D3FE8
    private void Turn(int reel, int pixels)
    {
        position[reel] = (position[reel] + pixels) % Strip;
        index[reel] = Symbols - position[reel] / SymbolPixels;
    }

    private int Percent() => rng.Next(100);                                                    // ov101_021D405C

    // ov101_021D406C: one share of a hundred taken off the roll
    private static bool Takes(int share, ref int roll)
    {
        int was = roll;
        roll = Math.Max(0, roll - share);
        return was < share;
    }

    /// <summary>One frame: the game's own step first, then the reels, the mascot and the coins, as the original's tasks run.</summary>
    public void Update(SlotKeys keys)
    {
        cues.Clear();
        while (Step(keys)) { }
        for (int reel = 0; reel < 3; reel++) RunReel(reel);
        if (show > 0) show--;
        CountCoins(keys);
    }

    private bool Has(SlotKeys keys, SlotKeys which) => (keys & which) != 0;

    private bool Step(SlotKeys keys)
    {
        switch (state)
        {
            case S.Begin:                                                                      // ov101_021D1AD0
                Payout = 0;
                Drawn = Draw.None;
                InBonus = false;
                Tell = null;
                Lines = 0;
                state = S.WaitBet;
                return CheckCoins(Bet, S.WaitBet);
            case S.WaitBet:                                                                    // ov101_021D1B40
            case S.ChanceWaitBet:                                                              // ov101_021D2198
                if (Has(keys, SlotKeys.Quit)) { state = S.Leave; return true; }
                if (Has(keys, SlotKeys.Bet))
                {
                    Coins -= Bet;
                    CoinsBet += Bet;
                    Lines = 0;
                    Lined = SlotSymbol.None;
                    cues.Add(SlotCue.Bet);
                    state = state == S.WaitBet ? S.WaitLever : S.ChanceWaitLever;
                }
                return false;
            case S.WaitLever:                                                                  // ov101_021D1B9C
            case S.ChanceWaitLever:                                                            // ov101_021D21F4
                if (Has(keys, SlotKeys.Quit)) { Coins += Bet; state = S.Leave; return true; }
                if (Has(keys, SlotKeys.Lever | SlotKeys.Bet)) { state = state == S.WaitLever ? S.Pull : S.ChancePull; return true; }
                return false;

            case S.Pull:                                                                       // ov101_021D1BD0
                DrawForGame();
                wide = (Drawn & Draw.Wide) != 0;
                aim = false;
                Tell = null;
                if (Drawn != Draw.None)
                {
                    // ov101_021D41EC: most of the time the machine lets on what it drew
                    Tell = Percent() < TellChance[Mood] ? TargetOf(Drawn) : SlotSymbol.None;
                    aim = true;
                    cues.Add(SlotCue.Tell);
                }
                StartReels();
                state = S.SpinUp;
                return true;
            case S.SpinUp: return Wait(8, S.Stops);                                            // ov101_021D1C9C
            case S.Stops:                                                                      // ov101_021D1CBC
                ReadStops(keys, bonus: false);
                if (pressed[0] && pressed[1] && pressed[2]) state = S.Settle;
                return false;
            case S.Settle:                                                                     // ov101_021D1DB4
                if (task[0] != 0 || task[1] != 0 || task[2] != 0) return false;
                state = S.Judge;
                return true;
            case S.Judge:                                                                      // ov101_021D1DEC
            {
                Lined = Judge(out int lines);
                Lines = lines;
                bool bonus = (Drawn & Draw.Bonus) != 0;
                if (Lined == SlotSymbol.None) state = bonus ? S.BonusUnlined : Drawn != Draw.None ? S.MissTold : S.Miss;
                else if (bonus) state = (Drawn & Draw.ReplayBonus) != 0 ? S.BonusReplay : S.BonusLined;
                else state = Lined == SlotSymbol.Replay ? S.Replay : S.Pay;
                return true;
            }
            case S.Miss: wait = 0; state = S.MissWait; return true;                            // ov101_021D1E54
            case S.MissWait: return Wait(8, S.Begin, again: false);                            // ov101_021D1E68
            case S.MissTold:                                                                   // ov101_021D1E84
                Show(20);
                state = S.MissToldWait;
                return false;
            case S.MissToldWait:                                                               // ov101_021D1E9C
                if (show > 0) return false;
                Tell = null;
                state = S.Begin;
                return true;
            case S.Pay:                                                                        // ov101_021D1EBC
                Payout = PayFor(Lined, Lines, Pays);
                StartCounting();
                wait = 0;
                state = S.PayWait;
                return false;
            case S.PayWait:                                                                    // ov101_021D1EEC
                if (++wait == 30) Show(20);
                else if (wait > 30 && counted && show == 0)
                {
                    Tell = null;
                    state = S.Begin;
                    return true;
                }
                return false;
            case S.Replay:                                                                     // ov101_021D1F34
                Payout = 0;
                StartCounting();
                wait = 0;
                state = S.ReplayWait;
                return false;
            case S.ReplayWait:                                                                 // ov101_021D1F54
                if (++wait == 15) Show(20);
                else if (wait > 15 && counted && show == 0)
                {
                    Tell = null;
                    // A replay: the lever is pulled again for nothing
                    state = S.Pull;
                    return true;
                }
                return false;

            case S.BonusUnlined:                                                               // ov101_021D1FA0
                DrawBonusKind();
                wait = 0;
                state = S.BonusUnlinedWait;
                return false;
            case S.BonusUnlinedWait:                                                           // ov101_021D1FC8
                if (++wait < 8) return false;
                Show(60);
                cues.Add(SlotCue.BonusBegins);
                state = S.BonusUnlinedShown;
                return false;
            case S.BonusUnlinedShown:                                                          // ov101_021D1FF4
                if (show > 0) return false;
                state = S.ToChance;
                return true;
            case S.BonusLined:                                                                 // ov101_021D200C
                Payout = PayFor(Lined, Lines, Pays);
                StartCounting();
                DrawBonusKind();
                wait = 0;
                state = S.BonusLinedWait;
                return false;
            case S.BonusLinedWait:                                                             // ov101_021D203C
                if (++wait == 30)
                {
                    Show(60);
                    cues.Add(SlotCue.BonusBegins);
                }
                else if (wait >= 30 && counted && show == 0)
                {
                    state = S.ToChance;
                    return true;
                }
                return false;
            case S.BonusReplay:                                                                // ov101_021D2088
                Payout = 0;
                StartCounting();
                DrawBonusKind();
                wait = 0;
                state = S.BonusReplayWait;
                return false;
            case S.BonusReplayWait:                                                            // ov101_021D20B0
                if (++wait == 8)
                {
                    Show(60);
                    cues.Add(SlotCue.BonusBegins);
                }
                else if (wait >= 8 && counted && show == 0)
                {
                    state = S.ToChanceFree;
                    return true;
                }
                return false;
            case S.ToChance: state = S.ChanceBegin; return true;                               // ov101_021D20FC
            case S.ToChanceFree: Tell = null; state = S.ChancePull; return true;               // ov101_021D2114

            case S.ChanceBegin:                                                                // ov101_021D212C
                Payout = 0;
                Drawn = Draw.None;
                Tell = null;
                state = S.ChanceWaitBet;
                return CheckCoins(Bet, S.ChanceWaitBet);
            case S.ChancePull:                                                                 // ov101_021D2228
                DrawForChance();
                wide = (Drawn & Draw.Wide) != 0;
                aim = Drawn != Draw.None;
                StartReels();
                state = S.ChanceSpinUp;
                return true;
            case S.ChanceSpinUp: return Wait(8, S.ChanceStops);                                // ov101_021D22D0
            case S.ChanceStops:                                                                // ov101_021D22F0
                ReadStops(keys, bonus: false);
                if (pressed[0] && pressed[1] && pressed[2]) state = S.ChanceSettle;
                return false;
            case S.ChanceSettle:                                                               // ov101_021D23E8
                if (task[0] != 0 || task[1] != 0 || task[2] != 0) return false;
                state = S.ChanceJudge;
                return true;
            case S.ChanceJudge:                                                                // ov101_021D2420
            {
                Lined = Judge(out int lines);
                Lines = lines;
                if (Lined == SlotSymbol.None) state = S.ChanceMiss;
                else if ((Drawn & (Draw.BlueSevens | Draw.RedSevens)) != 0) state = S.Jackpot;
                else state = Lined == SlotSymbol.Replay ? S.ChanceReplay : S.ChancePay;
                return true;
            }
            case S.ChanceMiss: wait = 0; state = S.ChanceMissWait; return true;                // ov101_021D2464
            case S.ChanceMissWait: return Wait(8, S.ChanceBegin);                              // ov101_021D2480
            case S.ChancePay:                                                                  // ov101_021D24A0
                Payout = PayFor(Lined, Lines, Pays);
                StartCounting();
                state = S.ChancePayWait;
                return false;
            case S.ChancePayWait:                                                              // ov101_021D24D0
                if (!counted) return false;
                state = S.ChanceBegin;
                return true;
            case S.ChanceReplay:                                                               // ov101_021D24F0
                Payout = 0;
                StartCounting();
                wait = 0;
                state = S.ChanceReplayWait;
                return false;
            case S.ChanceReplayWait:                                                           // ov101_021D2510
                if (++wait <= 15 || !counted) return false;
                state = S.ChancePull;
                return true;
            case S.Jackpot:                                                                    // ov101_021D254C
                Payout = PayFor(Lined, Lines, Pays);
                StartCounting();
                cues.Add(SlotCue.Jackpot);
                state = S.JackpotWait;
                return false;
            case S.JackpotWait:                                                                // ov101_021D2598
                if (!counted) return false;
                state = S.BonusSetUp;
                return true;
            case S.BonusSetUp:                                                                 // ov101_021D25C4
                Rounds = 0;
                BonusCoins = 0;
                InBonus = true;
                state = S.RoundBegin;
                return true;

            case S.RoundBegin:                                                                 // ov101_021D25DC
                RoundsPlayed++;
                Rounds = Math.Min(999, Rounds + 1);
                GamesLeft = BonusGames + 1;
                InBonus = true;
                Moon = SlotMoon.Plain;
                moonBright = false;
                moonRed = 0;
                // ov101_021D44E8: whether this round is the last is settled as it begins
                lastRound = Percent() >= stamina;
                cues.Add(SlotCue.RoundBegins);
                state = S.GameBegin;
                return true;
            case S.GameBegin:                                                                  // ov101_021D2628
                GamesLeft--;
                state = S.GameWaitBet;
                return CheckCoins(BonusBet, S.GameWaitBet);
            case S.GameWaitBet:                                                                // ov101_021D269C
                if (Has(keys, SlotKeys.Quit)) { state = S.Leave; return true; }
                if (Has(keys, SlotKeys.Bet))
                {
                    Coins -= BonusBet;
                    CoinsBet += BonusBet;
                    BonusCoins = Math.Max(0, BonusCoins - BonusBet);
                    Lines = 0;
                    Lined = SlotSymbol.None;
                    cues.Add(SlotCue.Bet);
                    state = S.GameWaitLever;
                }
                return false;
            case S.GameWaitLever:                                                              // ov101_021D2704
                if (Has(keys, SlotKeys.Quit)) { Coins += BonusBet; state = S.Leave; return true; }
                if (Has(keys, SlotKeys.Lever | SlotKeys.Bet)) { state = S.GamePull; return true; }
                return false;
            case S.GamePull:                                                                   // ov101_021D2738
                Drawn = Draw.Replay;
                aim = true;
                wide = true;
                StartReels();
                PointedOrder = rng.Next(Orders.Length);                                        // ov101_021D45B0
                if (!moonBright && moonRed == 0 && Percent() < RedChance[Mood])                // ov101_021D44C4
                {
                    moonRed = 1;
                    Moon = SlotMoon.Red;
                    cues.Add(SlotCue.MoonRed);
                }
                if (!moonBright && moonRed == 0 && Percent() < BrightChance[Mood])             // ov101_021D44A0
                {
                    moonBright = true;
                    Moon = SlotMoon.Bright;
                    cues.Add(SlotCue.MoonBright);
                }
                state = S.GameSpinUp;
                return true;
            case S.GameSpinUp: return Wait(8, S.GameStops);                                    // ov101_021D2828
            case S.GameStops:                                                                  // ov101_021D2850
                ReadStops(keys, bonus: true);
                if (pressed[0] && pressed[1] && pressed[2]) state = S.GameSettle;
                return false;
            case S.GameSettle:                                                                 // ov101_021D2950
                if (task[0] != 0 || task[1] != 0 || task[2] != 0) return false;
                state = S.GameJudge;
                return true;
            case S.GameJudge:                                                                  // ov101_021D2988
            {
                var middle = Line(2, 2, 2);                                                    // ov101_021D57EC: the middle line alone
                Lined = middle;
                Lines = middle != SlotSymbol.None ? 1 : 0;
                if (moonRed == 1) moonRed = middle == SlotSymbol.None ? 3 : 2;
                if (Moon == SlotMoon.Red && moonRed != 1) Moon = SlotMoon.Plain;
                state = middle == SlotSymbol.None ? S.GameMiss : S.GamePay;
                return true;
            }
            case S.GameMiss:                                                                   // ov101_021D29C0
                state = GamesLeft <= 1 ? S.RoundEnd : S.GameBegin;
                return false;
            case S.GamePay:                                                                    // ov101_021D29E4
                Payout = BonusPays[(int)Lined];
                StartCounting();
                state = S.GamePayWait;
                return false;
            case S.GamePayWait:                                                                // ov101_021D2A18
                if (!counted) return false;
                state = GamesLeft <= 1 ? S.RoundEnd : S.GameBegin;
                return true;
            case S.RoundEnd:                                                                   // ov101_021D2A44
                GamesLeft = 0;
                if (lastRound && !moonBright)
                {
                    Show(75);
                    cues.Add(SlotCue.BonusEnds);
                    state = S.BonusOver;
                    return false;
                }
                state = S.RoundOn;
                return true;
            case S.RoundOn:                                                                    // ov101_021D2A78
            {
                Tire();
                int mood = MoodAtRoundsEnd();
                if (moonBright) mood = 2;
                cues.Add(SlotCue.RoundContinues);
                if (mood == 2) { Show(60); state = S.RoundOnBright; }
                else if (mood == 1) { Show(60); state = S.RoundOnTired; }
                else { cues.Add(SlotCue.Cry); state = S.RoundBegin; }
                return false;
            }
            case S.RoundOnBright:                                                              // ov101_021D2AC8
            case S.RoundOnTired:                                                               // ov101_021D2AE0
                if (show > 0) return false;
                state = S.RoundBegin;
                return true;
            case S.BonusOver:                                                                  // ov101_021D2AF8
                if (show > 0) return false;
                MostRounds = Math.Max(MostRounds, Rounds);
                InBonus = false;
                Moon = SlotMoon.Plain;
                state = S.Begin;
                return true;

            case S.SayTooFew: Notice = SlotNotice.TooFewCoins; state = S.Notice; return false; // ov101_021D2B28
            case S.SayNone: Notice = SlotNotice.NoCoins; state = S.Notice; return false;       // ov101_021D2B50
            case S.SayFull: Notice = SlotNotice.CaseFull; state = S.Notice; return false;      // ov101_021D2B3C
            case S.Notice:                                                                     // ov101_021D2B64
                if (Has(keys, SlotKeys.Confirm))
                {
                    Notice = SlotNotice.None;
                    state = afterNotice;
                }
                return false;
            case S.Leave:                                                                      // ov101_021D2B80
                left = true;
                return false;
        }
        return false;
    }

    private bool moonBright, lastRound;
    private int moonRed;

    // The checks every game begins with: nothing left, too little for a bet, a case that holds no more
    private bool CheckCoins(int bet, S goOn)
    {
        if (Coins == 0) { state = S.SayNone; afterNotice = S.Leave; }
        else if (Coins < bet) { state = S.SayTooFew; afterNotice = S.Leave; }
        else if (Coins >= MostCoins) { state = S.SayFull; afterNotice = goOn; }
        return true;
    }

    private bool Wait(int frames, S next, bool again = true)
    {
        if (++wait < frames) return false;
        wait = 0;
        state = next;
        return again;
    }

    private void Show(int frames) => show = frames;

    // ov101_021D40A8: the draw of an ordinary game
    private void DrawForGame()
    {
        Drawn = Draw.None;
        if (Percent() >= HitChance[Mood]) return;
        int roll = Percent();
        int[] t = HitTable[Mood];
        if (Takes(t[0], ref roll)) Drawn = Draw.CherryBonus;
        else if (Takes(t[1], ref roll)) Drawn = Draw.Cherry;
        else if (Takes(t[2], ref roll)) Drawn = Draw.MoonBonus;
        else if (Takes(t[3], ref roll)) Drawn = Draw.Moon;
        else if (Takes(t[4], ref roll)) Drawn = Draw.BallBonus;
        else if (Takes(t[5], ref roll)) Drawn = Draw.Ball;
        else if (Takes(t[6], ref roll)) Drawn = Draw.ReplayBonus;
        else Drawn = Draw.Replay;
    }

    // ov101_021D4394: the draw while the bonus waits for its sevens. The second share of the table is asked twice and
    // the last two never, as the original has it
    private void DrawForChance()
    {
        int roll = Percent();
        Drawn = Draw.None;
        if (Percent() >= SmallChance[Mood])
        {
            Drawn = kind <= 2 ? Draw.RedSevens : Draw.BlueSevens;
            return;
        }
        if (Takes(SmallTable[0], ref roll)) Drawn = Draw.Cherry;
        else if (Takes(SmallTable[1], ref roll)) Drawn = Draw.Moon;
        else if (Takes(SmallTable[1], ref roll)) Drawn = Draw.Ball;
        else Drawn = Draw.Replay;
    }

    // ov101_021D4210 and ov101_021D42D0: which bonus has been won, and how it is announced
    private void DrawBonusKind()
    {
        int roll = Percent();
        var table = BonusKinds[Mood];
        var drawn = table[^1];
        foreach (var entry in table)
            if (Takes(entry.Weight, ref roll)) { drawn = entry; break; }
        kind = drawn.Kind;
        stamina = Stamina[kind];
        roll = Percent();
        int[] ways = Entrances[drawn.Entrance];
        Announced = 2;
        for (int i = 0; i < ways.Length; i++)
            if (Takes(ways[i], ref roll)) { Announced = i; break; }
    }

    /// <summary>How the bonus just won was announced: 0 quietly, 1 plainly, 2 with a fanfare. The louder, the likelier a long one.</summary>
    public int Announced { get; private set; }

    // ov101_021D44FC: Clefairy tires as a round ends, for certain where the player won under a red moon
    private void Tire()
    {
        int roll = Percent();
        if (moonRed == 2) stamina -= 10;
        else if (Takes(Tiring[Mood][0], ref roll)) stamina -= 10;
        else if (Takes(Tiring[Mood][1], ref roll)) stamina -= 5;
        stamina = Math.Max(0, stamina);
    }

    // ov101_021D4550: 1 tired, 0 plain, 2 lively
    private int MoodAtRoundsEnd()
    {
        int roll = Percent();
        foreach (var row in Moods)
        {
            if (stamina < row.AtLeast) continue;
            if (Takes(row.Tired, ref roll)) return 1;
            return Takes(row.Plain, ref roll) ? 0 : 2;
        }
        return 0;
    }

    /// <summary>The chance in a hundred that another round follows the one under way (for tests and the harness; the screen shows only Clefairy's mood).</summary>
    public int StaminaLeft => stamina;

    private static SlotSymbol TargetOf(Draw drawn) =>                                          // ov101_021D5618
        (drawn & (Draw.Replay | Draw.ReplayBonus)) != 0 ? SlotSymbol.Replay
        : (drawn & (Draw.Cherry | Draw.CherryBonus)) != 0 ? SlotSymbol.Cherry
        : (drawn & (Draw.Ball | Draw.BallBonus)) != 0 ? SlotSymbol.Ball
        : (drawn & (Draw.Moon | Draw.MoonBonus)) != 0 ? SlotSymbol.Moon
        : (drawn & Draw.BlueSevens) != 0 ? SlotSymbol.BlueSeven
        : (drawn & Draw.RedSevens) != 0 ? SlotSymbol.RedSeven
        : SlotSymbol.None;

    private void StartReels()
    {
        for (int i = 0; i < 3; i++)
        {
            task[i] = 1;
            pressed[i] = false;
            bounce[i] = 0;
        }
        order.Clear();
        wait = 0;
        cues.Add(SlotCue.Start);
    }

    // ov101_021D1CBC and ov101_021D2850: one reel a frame, the left before the middle before the right
    private void ReadStops(SlotKeys keys, bool bonus)
    {
        int reel = !pressed[0] && Has(keys, SlotKeys.Stop1) ? 0 : !pressed[1] && Has(keys, SlotKeys.Stop2) ? 1 : !pressed[2] && Has(keys, SlotKeys.Stop3) ? 2 : -1;
        if (reel < 0) return;
        pressed[reel] = true;
        order.Add(reel);
        if (!bonus)
        {
            // ov101_021D5880: stopped out of their order, the reels slip no further than four
            if (!InOrder(Orders[0])) wide = false;
        }
        else if (!InOrder(Orders[PointedOrder]))                                               // ov101_021D58C0
        {
            wide = false;
            aim = false;
        }
        task[reel] = 2;
        cues.Add(bonus ? SlotCue.StopBonus : SlotCue.Stop);
    }

    private bool InOrder(int[] wanted)
    {
        for (int i = 0; i < order.Count; i++)
            if (order[i] != wanted[i]) return false;
        return true;
    }

    // ------------------------------------------------------------------ a reel's task (ov101_021D2C10)

    private void RunReel(int reel)
    {
        while (true)
        {
            switch (task[reel])
            {
                case 0: return;
                case 1:                                                                        // ov101_021D2C34
                    Turn(reel, Speed);
                    return;
                case 2:                                                                        // ov101_021D2C4C
                    task[reel] = 3;
                    landedRow[reel] = 0;
                    slips[reel] = 0;
                    if (aim)
                    {
                        bool found = InBonus ? AimMiddle(reel, TargetOf(Drawn)) : Aim(reel);
                        if (!found)
                        {
                            if (InBonus) AvoidMiddle(reel);
                            else Avoid(reel);
                            aim = false;
                        }
                    }
                    else if (InBonus) AvoidMiddle(reel);
                    else Avoid(reel);
                    continue;
                case 3:                                                                        // ov101_021D2CA8
                {
                    bool moved = false;
                    int off = position[reel] % SymbolPixels;
                    if (off != 0)
                    {
                        moved = true;
                        Turn(reel, Math.Min(Speed, off));
                        off = position[reel] % SymbolPixels;
                    }
                    else if (slips[reel] > 0)
                    {
                        moved = true;
                        slips[reel]--;
                        Turn(reel, Speed);
                        off = position[reel] % SymbolPixels;
                    }
                    if (off != 0 || slips[reel] != 0) return;
                    settle[reel] = 4 * 4096;
                    settleStep[reel] = 0;
                    task[reel] = 4;
                    if (moved) return;
                    continue;
                }
                case 4:                                                                        // ov101_021D2D50
                    bounce[reel] = settle[reel];
                    settle[reel] = -settle[reel];
                    if ((++settleStep[reel] & 1) == 0) settle[reel] >>= 2;
                    if (settle[reel] == 0)
                    {
                        task[reel] = 0;
                        bounce[reel] = 0;
                    }
                    return;
            }
        }
    }

    private int MostSlips => wide ? Symbols : 4;

    private void Land(int reel, int row, int slip)
    {
        landedRow[reel] = row;
        slips[reel] = slip;
    }

    // ov101_021D3AF0: a cherry would show on the left reel after so many slips
    private bool CherryShows(int slip) => Sym(0, 1 - slip) == 3 || Sym(0, 2 - slip) == 3 || Sym(0, 3 - slip) == 3;

    // ov101_021D3B34
    private bool Safe(int slip, int target) => target == 3 || !CherryShows(slip);

    // ov101_021D38FC: this reel is the first to be stopped
    private bool First(int reel) => reel switch
    {
        0 => !pressed[1] && !pressed[2],
        1 => !pressed[0] && !pressed[2],
        _ => !pressed[0] && !pressed[1]
    };

    // ov101_021D2D88
    private bool Aim(int reel)
    {
        if (Drawn == Draw.None) return false;
        int target = (int)TargetOf(Drawn);
        if (target == 6) return false;
        if (reel == 0) return First(0) ? AimLeftFirst(target) : AimLeftLater(target);
        return First(reel) ? AimOtherFirst(reel, target) : AimOtherLater(reel, target);
    }

    // ov101_021D2E94
    private bool AimLeftFirst(int target)
    {
        for (int row = 1; row <= 3; row++)
        {
            if (Sym(0, row) != target) continue;
            for (int slip = 0; row + slip <= 3; slip++)
                if (Safe(slip, target)) { Land(0, row + slip, slip); return true; }
        }
        int at = index[0];
        for (int slip = 1; slip <= MostSlips; slip++)
        {
            if (Sym(0, 1 - slip) != target) continue;
            for (int more = 0; more < 3; more++)
            {
                if (!Safe(slip + more, target)) continue;
                if (more == 0 && at % 4 == 0 && Safe(slip + 2, target)) { Land(0, 3, slip + 2); return true; }
                if (more == 1 && at % 6 < 2 && Safe(slip + 2, target)) { Land(0, 3, slip + 2); return true; }
                Land(0, 1 + more, slip + more);
                return true;
            }
        }
        return false;
    }

    // ov101_021D2FAC
    private bool AimLeftLater(int target)
    {
        Rows(0, out int a, out int b, out int c);
        if (Safe(0, target))
        {
            if (Sym(0, a) == target) { Land(0, a, 0); return true; }
            if (b != 0 && Sym(0, b) == target) { Land(0, b, 0); return true; }
            if (c != 0 && Sym(0, c) == target) { Land(0, c, 0); return true; }
        }
        bool Try(int row, int slip)
        {
            if (Sym(0, row - slip) != target || !Safe(slip, target)) return false;
            Land(0, row, slip);
            return true;
        }
        for (int slip = 1; slip <= MostSlips; slip++)
        {
            if (b == 0)
            {
                if (Try(a, slip)) return true;
            }
            else if (c == 0)
            {
                if ((slip & 1) != 0 ? Try(a, slip) || Try(b, slip) : Try(b, slip) || Try(a, slip)) return true;
            }
            else
            {
                int turn = slip & 3;
                if (turn == 0 ? Try(a, slip) || Try(b, slip) || Try(c, slip)
                    : turn == 1 ? Try(b, slip) || Try(a, slip)
                    : Try(c, slip) || Try(a, slip) || Try(b, slip)) return true;
            }
        }
        return false;
    }

    // ov101_021D32EC
    private bool AimOtherFirst(int reel, int target)
    {
        for (int row = 1; row <= 3; row++)
            if (Sym(reel, row) == target) { Land(reel, row, 0); return true; }
        for (int slip = 1; slip <= MostSlips; slip++)
        {
            if (Sym(reel, 1 - slip) != target) continue;
            int turn = slip % 4;
            if (turn == 1) Land(reel, 3, slip + 2);
            else if (turn is 2 or 3) Land(reel, 2, slip + 1);
            else Land(reel, 1, slip);
            return true;
        }
        return false;
    }

    // ov101_021D3394
    private bool AimOtherLater(int reel, int target)
    {
        Rows(reel, out int a, out int b, out int c);
        if (Sym(reel, a) == target) { Land(reel, a, 0); return true; }
        if (b != 0 && Sym(reel, b) == target) { Land(reel, b, 0); return true; }
        if (c != 0 && Sym(reel, c) == target) { Land(reel, c, 0); return true; }
        bool Run(int row)
        {
            for (int slip = 1; slip <= MostSlips; slip++)
                if (Sym(reel, row - slip) == target) { Land(reel, row, slip); return true; }
            return false;
        }
        if (b == 0) return Run(a);
        int at = index[reel];
        if (c == 0) return (at & 1) != 0 ? Run(a) || Run(b) : Run(b) || Run(a);
        return (at % 3) switch
        {
            0 => Run(a) || Run(b) || Run(c),
            1 => Run(b) || Run(c) || Run(a),
            _ => Run(c) || Run(a) || Run(b)
        };
    }

    // ov101_021D394C: the rows the drawn symbol may still make a line on, by where it landed on the reels already stopped
    private void Rows(int reel, out int a, out int b, out int c)
    {
        bool s0 = pressed[0], s1 = pressed[1], s2 = pressed[2];
        int r0 = landedRow[0], r1 = landedRow[1], r2 = landedRow[2];
        a = b = c = 0;
        switch (reel)
        {
            case 0:
                if (s1 && s2) a = r1 == r2 ? r1 : r2 == 1 ? 3 : 1;
                else if (s1)
                {
                    if (r1 == 2) { a = 1; b = 2; c = 3; }
                    else a = r1;
                }
                else if (r2 == 2) a = r2;
                else { a = 1; b = 3; }
                break;
            case 1:
                if (s0 && s2) a = r0 == r2 ? r0 : 2;
                else
                {
                    int r = s0 ? r0 : r2;
                    if (r == 2) a = 2;
                    else if (r == 1) { a = 1; b = 2; }
                    else { a = 2; b = 3; }
                }
                break;
            default:
                if (s0 && s1) a = r0 == r1 ? r0 : r0 == 1 ? 3 : 1;
                else if (s0)
                {
                    if (r0 == 2) a = 2;
                    else { a = 1; b = 3; }
                }
                else if (r1 == 2) { a = 1; b = 2; c = 3; }
                else a = r1;
                break;
        }
    }

    // ov101_021D3780: nothing was drawn (or the aim failed): the reel slips until no line pays
    private void Avoid(int reel)
    {
        int stopped = (pressed[0] ? 1 : 0) + (pressed[1] ? 1 : 0) + (pressed[2] ? 1 : 0);
        if (stopped < 2)
        {
            if (reel != 0) return;
            for (int slip = 0; slip <= Symbols; slip++)
                if (!CherryShows(slip)) { slips[0] = slip; return; }
            return;
        }
        for (int slip = 1; slip <= Symbols; slip++)
            if (!WouldPay(reel, slip)) { slips[reel] = slip; return; }
    }

    // ov101_021D3B50, ov101_021D3C9C, ov101_021D3DD4: a line would pay with this reel slipped so far and the others
    // where their own slips leave them (a reel still turning is read where it is)
    private bool WouldPay(int reel, int slip)
    {
        int s0 = reel == 0 ? slip : slips[0], s1 = reel == 1 ? slip : slips[1], s2 = reel == 2 ? slip : slips[2];
        bool Pays(int row0, int row1, int row2)
        {
            int left = Sym(0, row0 - s0);
            return (reel == 0 && left == 3) || (left == Sym(1, row1 - s1) && left == Sym(2, row2 - s2));
        }
        return Pays(1, 1, 1) || Pays(2, 2, 2) || Pays(3, 3, 3) || Pays(1, 2, 3) || Pays(3, 2, 1);
    }

    // ov101_021D3738: in the bonus game the symbol is brought to the middle row
    private bool AimMiddle(int reel, SlotSymbol target)
    {
        if (Drawn == Draw.None) return false;
        for (int slip = 0; slip <= MostSlips; slip++)
            if (Sym(reel, 2 - slip) == (int)target) { Land(reel, 2, slip); return true; }
        return false;
    }

    // ov101_021D3830
    private void AvoidMiddle(int reel)
    {
        int stopped = (pressed[0] ? 1 : 0) + (pressed[1] ? 1 : 0) + (pressed[2] ? 1 : 0);
        if (stopped < 2)
        {
            if (reel != 0) return;
            for (int slip = 0; slip < Symbols; slip++)
                if (Sym(0, 2 - slip) != 3) { slips[0] = slip; return; }
            return;
        }
        for (int slip = 0; slip <= Symbols; slip++)
        {
            int s0 = reel == 0 ? slip : slips[0], s1 = reel == 1 ? slip : slips[1], s2 = reel == 2 ? slip : slips[2];
            int left = Sym(0, 2 - s0);
            bool pays = (reel == 0 && left == 3) || (left == Sym(1, 2 - s1) && left == Sym(2, 2 - s2));
            if (!pays) { slips[reel] = slip; return; }
        }
    }

    // ------------------------------------------------------------------ the lines

    // ov101_021D5660 and its four fellows: a cherry on the left reel pays its line by itself
    private SlotSymbol Line(int row0, int row1, int row2)
    {
        int left = Sym(0, row0);
        return left == 3 || (left == Sym(1, row1) && left == Sym(2, row2)) ? (SlotSymbol)left : SlotSymbol.None;
    }

    // ov101_021D5778: every line that pays, and the symbol of the last of them
    private SlotSymbol Judge(out int lines)
    {
        int found = 0;
        var lined = SlotSymbol.None;
        void Check(int row0, int row1, int row2, int bit)
        {
            var symbol = Line(row0, row1, row2);
            if (symbol == SlotSymbol.None) return;
            lined = symbol;
            found |= 1 << bit;
        }
        Check(1, 1, 1, 1);
        Check(2, 2, 2, 0);
        Check(3, 3, 3, 2);
        Check(1, 2, 3, 3);
        Check(3, 2, 1, 4);
        lines = found;
        return lined;
    }

    // ov101_021D597C: every line pays what the last line's symbol pays
    private static int PayFor(SlotSymbol lined, int lines, int[] pays)
    {
        int coins = 0;
        for (int bit = 0; bit < 5; bit++)
            if ((lines & (1 << bit)) != 0) coins += pays[(int)lined];
        return coins;
    }

    // ------------------------------------------------------------------ the coins counted out (ov101_021D4614)

    private void StartCounting()
    {
        counting = 1;
        counted = false;
        countTick = 0;
    }

    private void CountCoins(SlotKeys keys)
    {
        switch (counting)
        {
            case 1:
                // The lines flash before a coin moves: our own length for the original's line pictures settling
                lineFlash = 12;
                counting = 2;
                break;
            case 2:
                if (--lineFlash > 0) break;
                counting = 3;
                goto case 3;
            case 3:
                if (Has(keys, SlotKeys.Bet) || Payout == 0)
                {
                    Pay(Payout);
                    counted = true;
                    counting = 0;
                    break;
                }
                if ((++countTick & (Has(keys, SlotKeys.Held) ? 1 : 3)) == 0) Pay(1);
                break;
        }
    }

    private void Pay(int coins)
    {
        if (coins <= 0) return;
        Payout -= coins;
        Coins = Math.Min(MostCoins, Coins + coins);
        if (InBonus) BonusCoins += coins;
        cues.Add(SlotCue.Coin);
    }

    /// <summary>Whether coins are being counted out now.</summary>
    public bool Counting => counting != 0;

    // ------------------------------------------------------------------ for tests and tools

    /// <summary>Puts the machine in the state a test wants to look at: a draw made as if by the lever, with the reels turning.</summary>
    internal void Rig(Draw drawn, bool bonusGame = false, int pointed = 0, SlotMoon moon = SlotMoon.Plain)
    {
        Drawn = drawn;
        InBonus = bonusGame;
        Lines = 0;
        Lined = SlotSymbol.None;
        Tell = bonusGame || drawn == Draw.None || (drawn & (Draw.RedSevens | Draw.BlueSevens)) != 0 ? null : TargetOf(drawn);
        Moon = moon;
        moonRed = moon == SlotMoon.Red ? 1 : 0;
        moonBright = moon == SlotMoon.Bright;
        wide = bonusGame || (drawn & Draw.Wide) != 0;
        aim = drawn != Draw.None;
        PointedOrder = pointed;
        StartReels();
        wait = 0;
        state = bonusGame ? S.GameStops : (drawn & (Draw.RedSevens | Draw.BlueSevens)) != 0 ? S.ChanceStops : S.Stops;
        if (bonusGame && GamesLeft == 0) GamesLeft = BonusGames;
    }

    /// <summary>Sets the kind of bonus that waits or runs (0 to 5), as the draw would.</summary>
    internal void RigBonus(int bonusKind)
    {
        kind = bonusKind;
        stamina = Stamina[kind];
    }

    /// <summary>The name of the step the machine is at, for tools that print it.</summary>
    public string Phase => state.ToString();
}

/// <summary>The Coin Case's coins (<c>coins.c</c>): fifty thousand at most.</summary>
public sealed class CoinCase
{
    public const int Most = 50000;
    public int Coins { get; private set; }

    public void Set(int coins) => Coins = Math.Clamp(coins, 0, Most);
    /// <summary><c>Coins_CanAdd</c>: the whole amount fits.</summary>
    public bool CanAdd(int amount) => Coins + amount <= Most;
    /// <summary><c>Coins_Add</c>: nothing into a full case, otherwise as much as fits.</summary>
    public bool Add(int amount)
    {
        if (Coins >= Most) return false;
        Coins = Math.Min(Most, Coins + amount);
        return true;
    }
    /// <summary><c>Coins_Subtract</c>: all of the amount, or none.</summary>
    public bool Take(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }
}

/// <summary>The Veilstone prize exchange's wares (<c>ScrCmd_GetGameCornerPrizeData</c>): nineteen, for coins.</summary>
public static class GameCornerPrizes
{
    public static readonly (string Item, int Coins)[] All =
    {
        ("Silk Scarf", 1000), ("Wide Lens", 1000), ("Zoom Lens", 1000), ("Metronome", 1000), ("TM90", 2000), ("TM58", 2000),
        ("TM75", 4000), ("TM32", 4000), ("TM44", 6000), ("TM89", 6000), ("TM10", 6000), ("TM27", 8000), ("TM21", 8000),
        ("TM35", 10000), ("TM24", 10000), ("TM13", 10000), ("TM29", 10000), ("TM74", 15000), ("TM68", 20000)
    };

    /// <summary>What the counter sells coins at: fifty for 1,000, five hundred for 10,000.</summary>
    public static readonly (int Coins, int Money)[] Counter = { (50, 1000), (500, 10000) };

    /// <summary>Bonus rounds in a row that earn the counter's memento, TM64 (<c>ScrCmd_CheckBonusRoundStreak</c>).</summary>
    public const int StreakForMemento = 10;
    public const string StreakVariable = "VAR_CONSECUTIVE_BONUS_ROUND_WINS";
}
