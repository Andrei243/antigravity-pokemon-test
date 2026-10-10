using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>What a trap does to whoever steps on it: one of the original's nine families.</summary>
public enum TrapFamily
{
    None,
    /// <summary>Slides the player a way until something stops them: 27 tiles at most, 45 for a Hurl Trap.</summary>
    Slide,
    /// <summary>Drops the player in a hole they climb out of by turning this way and that.</summary>
    Hole,
    /// <summary>Turns the steering round, or muddles it, for thirty steps.</summary>
    Steering,
    /// <summary>Fills the screen with smoke, wiped away by hand.</summary>
    Smoke,
    /// <summary>Drops a boulder before the player, broken by blows.</summary>
    Rock,
    /// <summary>Fills the screen with bubbles, burst one at a time.</summary>
    Bubbles,
    /// <summary>Calls out for the player: nothing more than a word.</summary>
    Alert,
    /// <summary>Covers the screen in leaves, blown away.</summary>
    Leaves,
    /// <summary>Lights a flame before the player, blown out with one long breath.</summary>
    Fire
}

/// <summary>
/// A trap gone off (plan 06 · R16a; <c>src/underground/traps.c</c> and the steps it leaves to
/// <c>src/comm_player_manager.c</c>), until the player is free of it. The original's stylus and microphone are the
/// arrows and the confirm button here: a turn, a tap, a wipe or a breath, each counted as the original counts it.
/// It is advanced a frame at a time at the console's thirty frames a second, with what the player did that frame.
/// No drawing or input.
/// </summary>
public sealed class TrapEffect
{
    public const float FramesPerSecond = 30f;

    /// <summary>The screen the smoke, bubbles and leaves are laid on, in the console's dots.</summary>
    public const int ScreenWidth = 256, ScreenHeight = 192, SmokeColumns = 32, SmokeRows = 24;

    public int Trap { get; }
    public TrapFamily Family { get; }
    public bool Done { get; private set; }

    /// <summary>The frames since it went off.</summary>
    public int Age { get; private set; }

    // ---- Slide
    public Direction Way { get; }

    /// <summary>The tiles the slide has still to go (<c>slideTilesLeft</c>).</summary>
    public int SlideLeft { get; private set; }

    // ---- Hole
    /// <summary>The turns still to make to climb out (<c>holeMovementsLeft</c> less the two the original never asks for).</summary>
    public int TurnsLeft { get; private set; }
    public bool Deep { get; }

    // ---- Steering
    /// <summary>The steps still to take with the steering wrong (<c>alteredMovementStepsLeft</c>).</summary>
    public int StepsLeft { get; private set; }
    private readonly Direction[] steering = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    // ---- Smoke
    /// <summary>The smoke, a tile to an entry, 0 for clear air and 1 to 5 from thickest to thinnest (the tilemap's numbers).</summary>
    public int[,] Smoke { get; } = new int[SmokeRows, SmokeColumns];
    /// <summary>
    /// How far the hand that wipes reaches above and below its middle. The original's smoke is scribbled away with the
    /// stylus, a touch a frame; the keys' hand stands in for a scribble, as tall as five touches one over another.
    /// </summary>
    public const int HandReach = 2;

    public int HandX { get; private set; } = SmokeColumns / 2;
    public int HandY { get; private set; } = SmokeRows / 2;

    // ---- Rock
    /// <summary>The boulder's damage: it breaks past fifteen (<c>boulderDamage</c>).</summary>
    public int BoulderDamage { get; private set; }

    // ---- Bubbles and leaves
    public sealed class Floater
    {
        public float X, Y;
        public int Size;
        public bool Gone;
        public int Angle;
    }

    public List<Floater> Floaters { get; } = new();

    // ---- Fire
    /// <summary>How high the flame burns, from 4 (a Fire Trap) or 2 (an Ember Trap) down to nothing.</summary>
    public int Flame { get; private set; }
    public int BlowFrames { get; private set; }
    private readonly int fullFlame;

    /// <summary>The line an Alert Trap calls out, in our own words (the original's four greetings to whoever is near).</summary>
    public string? Call { get; }

    public static TrapFamily FamilyOf(int trap) => (UndergroundDatabase.Trap(trap)?.Id ?? "") switch
    {
        "TRAP_MOVE_UP" or "TRAP_MOVE_RIGHT" or "TRAP_MOVE_DOWN" or "TRAP_MOVE_LEFT" or "TRAP_HURL_UP" or "TRAP_HURL_RIGHT" or "TRAP_HURL_DOWN"
            or "TRAP_HURL_LEFT" => TrapFamily.Slide,
        "TRAP_HOLE" or "TRAP_PIT" => TrapFamily.Hole,
        "TRAP_REVERSE" or "TRAP_CONFUSE" => TrapFamily.Steering,
        "TRAP_SMOKE" or "TRAP_BIG_SMOKE" => TrapFamily.Smoke,
        "TRAP_ROCK" or "TRAP_ROCKFALL" => TrapFamily.Rock,
        "TRAP_FOAM" or "TRAP_BUBBLE" => TrapFamily.Bubbles,
        "TRAP_ALERT_1" or "TRAP_ALERT_2" or "TRAP_ALERT_3" or "TRAP_ALERT_4" => TrapFamily.Alert,
        "TRAP_LEAF" or "TRAP_FLOWER" => TrapFamily.Leaves,
        "TRAP_EMBER" or "TRAP_FIRE" => TrapFamily.Fire,
        _ => TrapFamily.None
    };

    /// <summary>Whether the player can walk while the trap has them: a slide carries them, and wrong steering lets them go where it will.</summary>
    public bool Walks => Family is TrapFamily.Slide or TrapFamily.Steering;

    public TrapEffect(int trap, Random chance)
    {
        Trap = trap;
        Family = FamilyOf(trap);
        string id = UndergroundDatabase.Trap(trap)?.Id ?? "";
        switch (Family)
        {
            case TrapFamily.Slide:
                Way = id.EndsWith("UP", StringComparison.Ordinal) ? Direction.Up : id.EndsWith("DOWN", StringComparison.Ordinal) ? Direction.Down
                    : id.EndsWith("LEFT", StringComparison.Ordinal) ? Direction.Left : Direction.Right;
                // CommPlayer_StartSlide
                SlideLeft = id.StartsWith("TRAP_HURL", StringComparison.Ordinal) ? 45 : 27;
                break;
            case TrapFamily.Hole:
                // CommPlayerMan_PutPlayerInHole: 10 and 20, of which the last two are never asked for
                Deep = id == "TRAP_PIT";
                TurnsLeft = (Deep ? 20 : 10) - 2;
                break;
            case TrapFamily.Steering:
                StepsLeft = 30;
                if (id == "TRAP_REVERSE") steering = new[] { Direction.Down, Direction.Up, Direction.Right, Direction.Left };
                else
                {
                    // CommSys_RandomizePlayerMovement: the four ways dealt out afresh
                    var ways = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
                    for (int i = 0; i < 4; i++)
                    {
                        int pick = chance.Next(ways.Count);
                        steering[i] = ways[pick];
                        ways.RemoveAt(pick);
                    }
                    // Dealt as they were, it would be no trap at all
                    if (steering.SequenceEqual(new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right }))
                        steering = new[] { Direction.Left, Direction.Right, Direction.Down, Direction.Up };
                }
                break;
            case TrapFamily.Smoke:
                LaySmoke(id == "TRAP_BIG_SMOKE", chance);
                break;
            case TrapFamily.Rock:
                // Traps_ProcessBoulder: a Rock Trap's boulder comes cracked already
                BoulderDamage = id == "TRAP_ROCKFALL" ? -1 : 5;
                break;
            case TrapFamily.Bubbles:
            {
                // Traps_InitBubbles: twenty for a Bubble Trap, with one or two large; seven for a Foam Trap
                bool big = id == "TRAP_BUBBLE";
                int count = big ? 20 : 7, large = big ? chance.Next(2) + 1 : 0, medium = chance.Next(3) + large + 5;
                for (int i = 0; i < count; i++)
                {
                    int size = i < large ? 0 : i < medium ? 1 : 2, radius = BubbleRadius(size);
                    Floaters.Insert(0, new Floater
                    {
                        Size = size, X = chance.Next(ScreenWidth - radius * 2) + radius, Y = chance.Next(ScreenHeight - radius * 2) + radius,
                        Angle = chance.Next(0xFFFF)
                    });
                }
                break;
            }
            case TrapFamily.Leaves:
            {
                // Traps_InitLeaves: sixteen leaves, thirty-two for a Flower Trap
                int count = id == "TRAP_FLOWER" ? 32 : 16;
                for (int i = 0; i < count; i++)
                    Floaters.Add(new Floater { X = chance.Next(ScreenWidth), Y = chance.Next(ScreenHeight), Angle = chance.Next(0xFFFF) });
                break;
            }
            case TrapFamily.Fire:
                Flame = fullFlame = id == "TRAP_FIRE" ? 4 : 2;
                break;
            case TrapFamily.Alert:
                Call = id switch
                {
                    "TRAP_ALERT_1" => "Hello! Is anybody there?",
                    "TRAP_ALERT_2" => "Goodbye, everyone!",
                    "TRAP_ALERT_3" => "Shall we meet at the Union Room?",
                    _ => "Over here! Come over here!"
                };
                break;
            default:
                Done = true;
                break;
        }
    }

    /// <summary>The radius of a bubble by its size, in the console's dots (<c>sBubbleSizeRadii</c>).</summary>
    public static int BubbleRadius(int size) => size switch { 0 => 32, 1 => 28, _ => 24 };

    /// <summary>
    /// The smoke as it settles: a Big Smoke Trap's fills the whole screen thick; a Smoke Trap's is one of four
    /// patterns, drawn. The patterns themselves are pictures of the original's and aren't taken: ours are drifts of
    /// our own laying, as thick in the middle and thinning out.
    /// </summary>
    private void LaySmoke(bool big, Random chance)
    {
        int pattern = chance.Next(4);
        for (int y = 0; y < SmokeRows; y++)
            for (int x = 0; x < SmokeColumns; x++)
            {
                if (big)
                {
                    Smoke[y, x] = 1;
                    continue;
                }
                // Two drifts whose places hang on the pattern drawn
                float ax = 8 + pattern * 5, ay = 7 + pattern % 2 * 8, bx = 24 - pattern * 4, by = 16 - pattern % 2 * 7;
                float d = MathF.Min(MathF.Sqrt((x - ax) * (x - ax) + (y - ay) * (y - ay) * 1.6f), MathF.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by) * 1.6f));
                Smoke[y, x] = d < 4 ? 1 : d < 6.5f ? 2 : d < 8.5f ? 4 : 0;
            }
    }

    /// <summary>How many tiles of smoke are left: under thirteen and the air is clear enough (<c>Traps_ProcessSmoke</c>).</summary>
    public int SmokeLeft => Smoke.Cast<int>().Count(v => v != 0);

    /// <summary>The way a pressed direction takes the player while the steering is wrong.</summary>
    public Direction Steer(Direction pressed) => Family == TrapFamily.Steering && !Done ? steering[(int)IndexOf(pressed)] : pressed;

    private static int IndexOf(Direction d) => d switch { Direction.Up => 0, Direction.Down => 1, Direction.Left => 2, _ => 3 };

    /// <summary>A step was taken with the steering wrong: the thirtieth sets it right (<c>alteredMovementStepsLeft</c>).</summary>
    public void Stepped()
    {
        if (Family != TrapFamily.Steering || Done) return;
        if (--StepsLeft <= 0) Done = true;
    }

    /// <summary>
    /// One tile of the slide (<c>CommPlayer_MoveSlide</c>): false when it ends here, because the tiles ran out or the
    /// next is blocked.
    /// </summary>
    public bool Slide(bool blockedAhead)
    {
        if (Family != TrapFamily.Slide || Done) return false;
        SlideLeft--;
        if (SlideLeft > 0 && blockedAhead) SlideLeft = 1;
        if (SlideLeft <= 1)
        {
            Done = true;
            return false;
        }
        return true;
    }

    /// <summary>
    /// A frame goes by. <paramref name="turned"/>: the player pressed a way they weren't facing (a hole).
    /// <paramref name="tap"/>: the confirm button was pressed this frame (a blow at the boulder, a bubble burst).
    /// <paramref name="held"/>: it is held down (a breath at the leaves or the flame).
    /// <paramref name="dx"/>, <paramref name="dy"/>: the arrows moved the hand through the smoke.
    /// </summary>
    public void Advance(bool turned = false, bool tap = false, bool held = false, int dx = 0, int dy = 0)
    {
        if (Done) return;
        Age++;
        switch (Family)
        {
            case TrapFamily.Hole:
                if (turned && --TurnsLeft <= 0) Done = true;
                break;
            case TrapFamily.Smoke:
                if (dx != 0 || dy != 0)
                {
                    HandX = Math.Clamp(HandX + dx, 0, SmokeColumns - 1);
                    HandY = Math.Clamp(HandY + dy, 0, SmokeRows - 1);
                    // The hand rubs up and down as it goes: a touch at every tile of its height
                    for (int k = -HandReach; k <= HandReach; k++) Wipe(HandX, HandY + k);
                }
                if (SmokeLeft < 13) Done = true;
                break;
            case TrapFamily.Rock:
                if (tap) BoulderDamage++;
                if (BoulderDamage > 15) Done = true;
                break;
            case TrapFamily.Bubbles:
                // A tap bursts one: the stylus's aim is not the button's, so the nearest the top of the pile goes
                if (tap && Floaters.FirstOrDefault(f => !f.Gone) is { } burst) burst.Gone = true;
                foreach (var f in Floaters.Where(f => !f.Gone)) Drift(f);
                if (Floaters.All(f => f.Gone)) Done = true;
                break;
            case TrapFamily.Leaves:
            {
                // Traps_ProcessLeaves: a breath's strength is the microphone's reading over 25, over 33 for a Flower
                // Trap; a held button is a full breath
                int strength = held ? 127 / (Floaters.Count > 16 ? 33 : 25) : 0;
                foreach (var f in Floaters.Where(f => !f.Gone))
                {
                    if (strength > 0)
                    {
                        f.X += f.X < ScreenWidth / 2f ? -strength : strength;
                        f.Y += f.Y < ScreenHeight / 2f ? -strength : strength;
                    }
                    if (f.X <= 0 || f.X >= ScreenWidth || f.Y <= 0 || f.Y >= ScreenHeight) f.Gone = true;
                }
                if (Floaters.All(f => f.Gone)) Done = true;
                break;
            }
            case TrapFamily.Fire:
                // Traps_ProcessFlame: a breath kept up wears the flame down a step every ten frames; a pause, and it
                // is as high as ever
                if (held)
                {
                    BlowFrames++;
                    if (BlowFrames % 10 == 9) Flame--;
                    if (Flame <= 0) Done = true;
                }
                else
                {
                    BlowFrames = 0;
                    Flame = fullFlame;
                }
                break;
            case TrapFamily.Alert:
                // Traps_AlertTrapClientTask: the call hangs in the air a second and is gone
                if (Age > 38) Done = true;
                break;
        }
    }

    /// <summary>
    /// A touch at a tile (<c>Traps_ClearSmoke</c>): the tile thins by three, the four beside it by two and the corners
    /// by one, and a tile thinned past five is clear.
    /// </summary>
    public void Wipe(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int tx = x + dx, ty = y + dy;
                if (tx < 0 || tx >= SmokeColumns || ty < 0 || ty >= SmokeRows || Smoke[ty, tx] == 0) continue;
                Smoke[ty, tx] += dx == 0 && dy == 0 ? 3 : dx == 0 || dy == 0 ? 2 : 1;
                if (Smoke[ty, tx] > 5) Smoke[ty, tx] = 0;
            }
    }

    /// <summary>A bubble drifts the way it is headed and turns back at the screen's edge (<c>Traps_ProcessBubbles</c>).</summary>
    private static void Drift(Floater f)
    {
        float radians = f.Angle / 65536f * MathF.Tau;
        f.Y -= MathF.Sin(radians);
        f.X += MathF.Cos(radians);
        int radius = BubbleRadius(f.Size);
        if (f.Y < radius || f.Y >= ScreenHeight - radius) f.Angle = 0xFFFF - f.Angle;
        if (f.X < radius || f.X >= ScreenWidth - radius) f.Angle = (0xFFFF - (f.Angle - 0x8000)) & 0xFFFF;
    }

    /// <summary>What the player is told to do, in our own words.</summary>
    public string Hint => Family switch
    {
        TrapFamily.Hole => "Turn this way and that to climb out!",
        TrapFamily.Smoke => "Wipe the smoke away with the arrows!",
        TrapFamily.Rock => "Break the boulder!",
        TrapFamily.Bubbles => "Burst the bubbles!",
        TrapFamily.Leaves => "Hold the button to blow the leaves away!",
        TrapFamily.Fire => "Hold the button to blow the flame out in one breath!",
        TrapFamily.Steering => "Which way is which?",
        _ => ""
    };
}
