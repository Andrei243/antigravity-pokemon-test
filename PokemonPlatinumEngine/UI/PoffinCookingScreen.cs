using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where a cooking at the Poffin House has got to.</summary>
public enum CookingPhase
{
    /// <summary>The berry drops into the batter.</summary>
    Pour,
    /// <summary>The three stages of stirring (<see cref="PoffinPot"/>).</summary>
    Stir,
    /// <summary>It's done: the batter sets and the picture fades.</summary>
    Finish,
    /// <summary>What came out: the Poffin, the time, the spills and burns.</summary>
    Results,
    /// <summary>The Poffin is put away in the case.</summary>
    PutAway,
    /// <summary>"Cook another?" with YES and NO.</summary>
    Again,
    /// <summary>Why no more can be cooked: the case is full, or there is no berry left.</summary>
    Notice,
    /// <summary>Over: the game goes back to the field, or to the bag for another berry (<see cref="PoffinCookingScreen.WantsBerry"/>).</summary>
    Done
}

/// <summary>
/// Cooking a Poffin at the Poffin House (plan 06 · R14c): the berry goes in, the batter is stirred through its three
/// stages, and the Poffin is shown, put away in the case and another offered, on the beats and timings of the
/// original's cooking (<c>src/overlay083/ov83_0223B5A0.c</c>). The rules are <see cref="PoffinPot"/>'s, a frame
/// every thirtieth of a second; this screen is their clock and their spoon. Everything here is a function of
/// <see cref="Advance"/> and the buttons (<see cref="Turn"/>, <see cref="Touch"/>, <see cref="PressConfirm"/>,
/// <see cref="PressCancel"/>, <see cref="MoveChoice"/>), so tests cook without a window; <see cref="Update"/> reads
/// the keys and <see cref="Draw"/> draws.
/// </summary>
public sealed class PoffinCookingScreen
{
    /// <summary>How long the berry takes to go in, and the batter to set at the end.</summary>
    public const float PourTime = 2f, FinishTime = 1f;

    /// <summary>The results: the Poffin a second, then the results card another, then up to twenty seconds or a button (<c>ov83_0223BF74</c>).</summary>
    public const float PoffinShown = 1f, CardShown = 2f, ResultsWait = 22f;

    /// <summary>A message waits up to five seconds or a button (<c>ov83_0223BF74</c>, states 6 and 10).</summary>
    public const float MessageWait = 5f;

    /// <summary>The spoon goes round this far from the pot's middle, and this far a frame while an arrow is held (the pot screen's pixels).</summary>
    public const int SpoonRadius = 48, SpoonStep = 32;

    private const float Tick = 1f / PoffinPot.FramesPerSecond;

    public bool IsActive { get; private set; }
    public CookingPhase Phase { get; private set; } = CookingPhase.Done;

    /// <summary>Seconds since the phase began.</summary>
    public float PhaseTime { get; private set; }

    /// <summary>The pot being stirred, the berries in it, and the Poffin it made once it is done.</summary>
    public PoffinPot? Pot { get; private set; }
    public IReadOnlyList<ItemData> Berries { get; private set; } = Array.Empty<ItemData>();
    public Poffin? Made { get; private set; }

    /// <summary>How many cook at the pot: each takes home as many Poffins (<c>ov83_0223C8B0</c>). One here, as cooking alone.</summary>
    public int Cooks { get; private set; } = 1;

    /// <summary>The Poffins made went into the case (false when it filled up first).</summary>
    public bool PutAwayAll { get; private set; }

    /// <summary>Where the spoon is round the pot (radians, with the clock on the screen as it grows), and which way an arrow turns it: 1 with the clock, -1 against, 0 still.</summary>
    public float SpoonAngle { get; private set; } = -MathF.PI / 2f;
    public int Turning { get; private set; }

    /// <summary>
    /// The arrows as a tool holds them in place of the keys (the screenshot harness: 1 with the clock, -1 against, 0
    /// neither); null reads the keys. As <see cref="GameEngine.Steering"/> walks the player.
    /// </summary>
    public int? Held { get; set; }

    /// <summary>The stylus the pot reads: the spoon, or a touch of its own (<see cref="Touch"/>).</summary>
    public PotStylus Stylus { get; private set; } = new();

    /// <summary>The answer under the cursor in "Cook another?": 0 YES, 1 NO.</summary>
    public int Choice { get; private set; }

    /// <summary>The line on the screen now, if any: the berry going in, a warning, the put-away line, a notice.</summary>
    public string? Line { get; private set; }

    /// <summary>Seconds since the pot last burned, spilled, warned of burning, drew its arrow or the spoon made a turn: what the picture shows a moment of.</summary>
    public float SinceBurn { get; private set; } = 99f;
    public float SinceSpill { get; private set; } = 99f;
    public float SinceWarning { get; private set; } = 99f;
    public float SinceArrow { get; private set; } = 99f;

    /// <summary>The batter's angle a frame ago, so the picture can turn it smoothly between frames.</summary>
    public int LastAngle { get; private set; }

    /// <summary>The part of a frame since the last one, 0 to 1, for drawing between frames.</summary>
    public float Between => carry / Tick;

    /// <summary>Once over: true when the player wants another berry (the game opens the bag again), false to go back to the field.</summary>
    public bool WantsBerry { get; private set; }

    private string player = "";
    private Random rng = new(0);
    private PoffinCase? poffinCase;
    private Inventory? bag;
    private float carry;
    private PotTouch sent = PotTouch.Still;
    private (int X, int Y)? touch;
    private string? warning;
    private float warningAge = 99f;
    private bool cooked;

    /// <summary>The pot has just made its Poffin: true once, for the Trainer Card's score.</summary>
    public bool TakeCooked()
    {
        bool was = cooked;
        cooked = false;
        return was;
    }

    /// <summary>
    /// Puts a berry in the pot and starts cooking: the pot is a new one, its arrow drawn on <paramref name="chance"/>
    /// (the original draws it as the cooking opens, <c>ov83_0223FDB0</c>). The Poffin goes into
    /// <paramref name="into"/>, and "Cook another?" asks <paramref name="from"/> whether a berry is left.
    /// </summary>
    public void Begin(ItemData berry, string playerName, Random chance, PoffinCase into, Inventory from)
    {
        if (berry.Berry == null) throw new ArgumentException($"{berry.Name} is no berry.", nameof(berry));
        IsActive = true;
        player = playerName;
        rng = chance;
        poffinCase = into;
        bag = from;
        Berries = new[] { berry };
        Cooks = 1;
        Pot = new PoffinPot(rng);
        Made = null;
        PutAwayAll = false;
        Stylus = new PotStylus();
        sent = PotTouch.Still;
        touch = null;
        Turning = 0;
        SpoonAngle = -MathF.PI / 2f;
        LastAngle = 0;
        carry = 0f;
        Choice = 0;
        WantsBerry = false;
        SinceBurn = SinceSpill = SinceWarning = SinceArrow = 99f;
        warning = null;
        warningAge = 99f;
        Enter(CookingPhase.Pour);
        Line = $"The {berry.Name} went in!";
        AudioManager.PlaySound("step_puddle");
    }

    /// <summary>Puts the screen away without anything more (the game is leaving it).</summary>
    public void Close()
    {
        IsActive = false;
        Phase = CookingPhase.Done;
    }

    private void Enter(CookingPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
    }

    // ---------------------------------------------------------------- the buttons

    /// <summary>Turns the spoon round the pot while held: 1 with the clock on the screen, -1 against it, 0 to hold it still.</summary>
    public void Turn(int way)
    {
        Turning = Math.Sign(way);
        touch = null;
    }

    /// <summary>Puts a stylus of its own on the pot screen at a point (256 by 192, the middle at 128, 96), in place of the spoon, until <see cref="Turn"/>.</summary>
    public void Touch(int x, int y) => touch = (x, y);

    /// <summary>The confirm button: on past the results and the lines, and answers the question.</summary>
    public void PressConfirm()
    {
        switch (Phase)
        {
            case CookingPhase.Results when PhaseTime >= CardShown:
                PutAway();
                break;
            case CookingPhase.PutAway:
                Ask();
                break;
            case CookingPhase.Again:
                Answer(Choice == 0);
                break;
            case CookingPhase.Notice:
                End(false);
                break;
        }
    }

    /// <summary>The cancel button: as confirm on the lines, NO to the question.</summary>
    public void PressCancel()
    {
        if (Phase == CookingPhase.Again) Answer(false);
        else PressConfirm();
    }

    /// <summary>Moves the cursor between YES and NO.</summary>
    public void MoveChoice(int step)
    {
        if (Phase != CookingPhase.Again || step == 0) return;
        Choice = Choice == 0 ? 1 : 0;
        AudioManager.PlaySound("cursor");
    }

    // ---------------------------------------------------------------- time

    /// <summary>Time goes on: the berry goes in, the pot is stirred a frame each thirtieth of a second, the results come up and the lines wait.</summary>
    public void Advance(float dt)
    {
        if (!IsActive || dt <= 0f) return;
        PhaseTime += dt;
        SinceBurn += dt;
        SinceSpill += dt;
        SinceWarning += dt;
        SinceArrow += dt;
        warningAge += dt;
        if (warning != null && warningAge > 1.4f) warning = null;

        switch (Phase)
        {
            case CookingPhase.Pour:
                if (PhaseTime >= PourTime)
                {
                    // The arrow drawn as the cooking opened shows as the stirring begins
                    Line = null;
                    SinceArrow = 0f;
                    Enter(CookingPhase.Stir);
                }
                break;
            case CookingPhase.Stir:
                carry += dt;
                while (carry >= Tick && Phase == CookingPhase.Stir)
                {
                    carry -= Tick;
                    Frame();
                }
                // The last frame's "Done!" stays over the finish
                if (Phase == CookingPhase.Stir) Line = warning;
                break;
            case CookingPhase.Finish:
                if (PhaseTime >= FinishTime)
                {
                    // "Done!" was the pot's; the results' card says the rest
                    Line = null;
                    Enter(CookingPhase.Results);
                    AudioManager.PlaySound("levelup");
                }
                break;
            case CookingPhase.Results:
                if (PhaseTime >= ResultsWait) PutAway();
                break;
            case CookingPhase.PutAway:
                if (PhaseTime >= MessageWait) Ask();
                break;
            case CookingPhase.Notice:
                if (PhaseTime >= MessageWait) End(false);
                break;
        }
    }

    // A frame of the pot: the stylus is read, the pot steps on what was sent the frame before, and this frame's touch
    // is sent (ov83_0223BB40)
    private void Frame()
    {
        var pot = Pot!;
        if (touch is { } t) Stylus.Frame(true, t.X, t.Y);
        else
        {
            SpoonAngle += Turning * (float)SpoonStep / SpoonRadius;
            Stylus.Frame(true, PoffinPot.MiddleX + (int)MathF.Round(MathF.Cos(SpoonAngle) * SpoonRadius),
                PoffinPot.MiddleY + (int)MathF.Round(MathF.Sin(SpoonAngle) * SpoonRadius));
        }

        int turns = pot.Turns, phase = pot.Phase;
        LastAngle = pot.Angle;
        bool done = pot.Step(new[] { sent });
        sent = Stylus.Sample;
        if (done)
        {
            LastAngle = pot.Angle;
            Made = pot.Finish(Berries, rng);
            cooked = true;
            warning = null;
            Line = "Done!";
            Enter(CookingPhase.Finish);
            AudioManager.PlaySound("heal");
            return;
        }

        // What the picture and the speaker show of the frame (ov83_0223C3E8)
        if (pot.Phase == phase && pot.Turns > turns) AudioManager.PlaySound("step_mud");
        if (pot.ArrowDrawn)
        {
            SinceArrow = 0f;
            AudioManager.PlaySound("poketch_beep");
        }
        if (pot.Spilled)
        {
            SinceSpill = 0f;
            Warn(pot.Phase == 0 ? "Too fast! It's slopping over!" : "Easy there! Ease off a little!");
            AudioManager.PlaySound("step_puddle");
        }
        if (pot.BurnSign == 1)
        {
            SinceBurn = 0f;
            Warn(BurnLine(pot.Phase));
            AudioManager.PlaySound("status_burn");
        }
        else if (pot.BurnSign == 2)
        {
            SinceWarning = 0f;
            Warn(BurnLine(pot.Phase));
        }
    }

    // The original's lines for a burn (ov83_0223C600), on its beats in our own words: keep stirring, hurry, faster still
    private static string BurnLine(int phase) => phase switch
    {
        0 => "It's starting to catch! Keep stirring!",
        1 => "It's catching! Stir faster!",
        _ => "Stir, stir, stir!"
    };

    private void Warn(string line)
    {
        warning = line;
        warningAge = 0f;
    }

    // ov83_0223BF74, state 5: the Poffins go into the case, one for each cook, as long as there is room
    private void PutAway()
    {
        if (Made == null) return;
        int put = 0;
        for (int i = 0; i < Cooks; i++)
            if (poffinCase?.Add(Made) == true) put++;
        PutAwayAll = put == Cooks;
        Line = Cooks == 1
            ? $"{player} put the {Made.Name} away in the Poffin Case."
            : $"{player} put the {Made.Name}s away in the Poffin Case.";
        Enter(CookingPhase.PutAway);
        AudioManager.PlaySound("select");
    }

    private void Ask()
    {
        Line = "Would you like to cook another Poffin?";
        Choice = 0;
        Enter(CookingPhase.Again);
    }

    // ov83_0223BF74, state 8: yes asks the case and the bag first
    private void Answer(bool yes)
    {
        AudioManager.PlaySound(yes ? "select" : "cancel");
        if (!yes)
        {
            End(false);
            return;
        }
        if (poffinCase?.IsFull == true) Notify("The Poffin Case is full.");
        else if (bag != null && !bag.AllItems.Any(s => s.Quantity > 0 && s.Data.Berry != null)) Notify("There are no Berries left to cook.");
        else End(true);
    }

    private void Notify(string line)
    {
        Line = line;
        Enter(CookingPhase.Notice);
    }

    private void End(bool another)
    {
        WantsBerry = another;
        Line = null;
        IsActive = false;
        Enter(CookingPhase.Done);
    }

    // ---------------------------------------------------------------- the keys and the picture

    /// <summary>A frame of the game: the arrows turn the spoon, the buttons go on, and time passes.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == CookingPhase.Stir)
            Turn(Held ?? InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        else if (Phase == CookingPhase.Again)
        {
            int dx = InputManager.Axis(GameAction.Left, GameAction.Right) + InputManager.Axis(GameAction.Up, GameAction.Down);
            if (dx != 0) MoveChoice(dx);
        }
        if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        Advance(dt);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPoffinCooking(screenWidth, screenHeight, this);
    }
}
