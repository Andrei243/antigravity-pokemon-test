using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Battle Factory's screen is open for: the six on offer, or a trade after a battle won.</summary>
public enum FactoryScreenMode { Rent, Trade }

/// <summary>Where the Battle Factory's screen stands (the original's <c>BattleFactoryAppState</c>, for one player).</summary>
public enum FactoryStep
{
    /// <summary>The cursor is on the six on offer, or on the three in hand.</summary>
    Browse,
    /// <summary>A Pokémon's own menu is up: SUMMARY, RENT or REMOVE (EXCHANGE in a trade), CANCEL.</summary>
    Options,
    /// <summary>A rental's summary is being read.</summary>
    Summary,
    /// <summary>Three are chosen: "are these the ones?".</summary>
    Confirm,
    /// <summary>A trade: the cursor is on the beaten trainer's three.</summary>
    Receive,
    /// <summary>A trade: "take this one?".</summary>
    Accept,
    /// <summary>A trade: "go on without trading?".</summary>
    Quit,
    /// <summary>A trade was made, and the screen says so until a button is pressed.</summary>
    Traded
}

/// <summary>What a rental's own menu offers.</summary>
public enum FactoryOption { Summary, Rent, Remove, Exchange, Cancel }

/// <summary>
/// The Battle Factory's screen (plan 06 · R18b; style guide, "The Battle Factory"), on the original's rental
/// application (<c>src/applications/frontier/battle_factory/main.c</c>). Opened for the six on offer, three are
/// chosen in the order they will go into battle: a Pokémon's menu rents it, shows its summary or takes it back out,
/// Cancel takes the last one back, and the third asks whether the three are the ones (no takes the third back).
/// Opened for a trade, one of the three in hand is chosen to give up and one of the beaten trainer's three to take in
/// its place, each step with a way back and a way out without trading; only one's own are read in full, as in the
/// original. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and
/// the harness drive it.
/// </summary>
public sealed class FactoryScreen
{
    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public FactoryScreenMode Mode { get; private set; }
    public FactoryStep Step { get; private set; }

    /// <summary>The six on offer, or in a trade the three in hand.</summary>
    public IReadOnlyList<Pokemon> Mine { get; private set; } = Array.Empty<Pokemon>();

    /// <summary>In a trade, the beaten trainer's three, and who that was.</summary>
    public IReadOnlyList<Pokemon> Theirs { get; private set; } = Array.Empty<Pokemon>();
    public string TheirTrainer { get; private set; } = "";

    /// <summary>The cursor among <see cref="Mine"/>; in a trade the place after the last is CANCEL.</summary>
    public int Cursor { get; private set; }

    /// <summary>The cursor among <see cref="Theirs"/>, with BACK and CANCEL after the last.</summary>
    public int TheirCursor { get; private set; }

    /// <summary>The cursor in whatever menu is up: a Pokémon's own, or a yes and a no.</summary>
    public int MenuCursor { get; private set; }

    /// <summary>The Pokémon rented so far, by their places among the six, in the order chosen.</summary>
    public IReadOnlyList<int> Chosen => chosen;

    /// <summary>In a trade, the place of the one to be given up; -1 until it is chosen.</summary>
    public int Giving { get; private set; } = -1;

    /// <summary>What was decided as the screen closed: the three rented, or the place given up and the place taken; empty when no trade was made.</summary>
    public IReadOnlyList<int> Outcome { get; private set; } = Array.Empty<int>();

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    private readonly List<int> chosen = new();
    private int needed = 3;

    /// <summary>How many are to be rented.</summary>
    public int Needed => needed;

    // ------------------------------------------------------------------ opening

    /// <summary>The six on offer (<c>OpenBattleFactoryAppInitial</c>).</summary>
    public void OpenToRent(IReadOnlyList<Pokemon> offered, int count)
    {
        Reset(FactoryScreenMode.Rent);
        Mine = offered;
        needed = count;
    }

    /// <summary>A trade after a battle won (<c>OpenBattleFactoryAppForTrade</c>): the three in hand and the beaten trainer's.</summary>
    public void OpenToTrade(IReadOnlyList<Pokemon> mine, IReadOnlyList<Pokemon> theirs, string trainer)
    {
        Reset(FactoryScreenMode.Trade);
        Mine = mine;
        Theirs = theirs;
        TheirTrainer = trainer;
    }

    private void Reset(FactoryScreenMode mode)
    {
        Mode = mode;
        IsActive = true;
        Theirs = Array.Empty<Pokemon>();
        TheirTrainer = "";
        Cursor = TheirCursor = MenuCursor = 0;
        Giving = -1;
        chosen.Clear();
        Outcome = Array.Empty<int>();
        Age = 0f;
        Go(FactoryStep.Browse);
        AudioManager.PlaySound("select");
    }

    private void Go(FactoryStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    private void Close(IReadOnlyList<int> outcome)
    {
        Outcome = outcome;
        IsActive = false;
    }

    // ------------------------------------------------------------------ what is on screen

    /// <summary>The place after the last of one's own in a trade: CANCEL.</summary>
    public int CancelPlace => Mine.Count;

    /// <summary>The places after the last of the trainer's three: BACK, then CANCEL.</summary>
    public int BackPlace => Theirs.Count;
    public int TheirCancelPlace => Theirs.Count + 1;

    /// <summary>What the menu of the Pokémon under the cursor offers (<c>OpenMonOptionsMenu</c>).</summary>
    public IReadOnlyList<FactoryOption> Options => Mode == FactoryScreenMode.Trade
        ? new[] { FactoryOption.Summary, FactoryOption.Exchange, FactoryOption.Cancel }
        : new[] { FactoryOption.Summary, chosen.Contains(Cursor) ? FactoryOption.Remove : FactoryOption.Rent, FactoryOption.Cancel };

    /// <summary>The Pokémon whose summary is open.</summary>
    public Pokemon? Shown => Cursor >= 0 && Cursor < Mine.Count ? Mine[Cursor] : null;

    /// <summary>What the screen asks or says at this step.</summary>
    public string Prompt => Step switch
    {
        FactoryStep.Confirm => "Are these three the ones you want?",
        FactoryStep.Receive => "Which Pokémon will you take in exchange?",
        FactoryStep.Accept => TheirCursor < Theirs.Count ? $"Take {Theirs[TheirCursor].DisplayName}?" : "",
        FactoryStep.Quit => "Go on without trading?",
        FactoryStep.Traded => Outcome.Count == 2 ? $"{Mine[Outcome[0]].DisplayName} was traded for {Theirs[Outcome[1]].DisplayName}." : "",
        _ when Mode == FactoryScreenMode.Trade => "Which Pokémon will you give up?",
        _ => chosen.Count switch { 0 => "Choose the first Pokémon.", 1 => "Choose the second Pokémon.", _ => "Choose the third Pokémon." }
    };

    // ------------------------------------------------------------------ the buttons

    /// <summary>The arrows: along the Pokémon, or up and down a menu.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || (dx == 0 && dy == 0)) return;
        int before = Cursor * 100 + TheirCursor * 10 + MenuCursor;
        switch (Step)
        {
            case FactoryStep.Browse:
            {
                int places = Mode == FactoryScreenMode.Trade ? Mine.Count + 1 : Mine.Count;
                int step = dx != 0 ? Math.Sign(dx) : Math.Sign(dy);
                Cursor = UiNav.Wrap(Cursor, step, places);
                break;
            }
            case FactoryStep.Receive:
            {
                int step = dx != 0 ? Math.Sign(dx) : Math.Sign(dy);
                TheirCursor = UiNav.Wrap(TheirCursor, step, Theirs.Count + 2);
                break;
            }
            case FactoryStep.Options:
                if (dy != 0) MenuCursor = UiNav.Wrap(MenuCursor, Math.Sign(dy), Options.Count);
                break;
            case FactoryStep.Confirm:
            case FactoryStep.Accept:
            case FactoryStep.Quit:
                if (dx != 0 || dy != 0) MenuCursor = 1 - MenuCursor;
                break;
            case FactoryStep.Summary:
                // The next of one's own, as the summary of a team turns from one to the next
                if (dy != 0) Cursor = UiNav.Wrap(Cursor, Math.Sign(dy), Mine.Count);
                break;
        }
        if (before != Cursor * 100 + TheirCursor * 10 + MenuCursor) AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case FactoryStep.Browse:
                if (Mode == FactoryScreenMode.Trade && Cursor == CancelPlace) AskToQuit(FactoryStep.Browse);
                else
                {
                    MenuCursor = 0;
                    Go(FactoryStep.Options);
                    AudioManager.PlaySound("select");
                }
                break;
            case FactoryStep.Options:
                Choose(Options[MenuCursor]);
                break;
            case FactoryStep.Summary:
                Go(FactoryStep.Options);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryStep.Confirm:
                if (MenuCursor == 0)
                {
                    AudioManager.PlaySound("select");
                    Close(chosen.ToList());
                }
                else TakeBackLast();
                break;
            case FactoryStep.Receive:
                if (TheirCursor == TheirCancelPlace) AskToQuit(FactoryStep.Receive);
                else if (TheirCursor == BackPlace) BackToMine();
                else
                {
                    MenuCursor = 0;
                    Go(FactoryStep.Accept);
                    AudioManager.PlaySound("select");
                }
                break;
            case FactoryStep.Accept:
                if (MenuCursor == 0)
                {
                    // ConfirmReceiveSelection: the trade is made, and said before the screen closes
                    Outcome = new[] { Giving, TheirCursor };
                    Go(FactoryStep.Traded);
                    AudioManager.PlaySound("select");
                }
                else
                {
                    Go(FactoryStep.Receive);
                    AudioManager.PlaySound("cancel");
                }
                break;
            case FactoryStep.Quit:
                if (MenuCursor == 0)
                {
                    AudioManager.PlaySound("cancel");
                    Close(Array.Empty<int>());
                }
                else
                {
                    Go(quitFrom);
                    AudioManager.PlaySound("cancel");
                }
                break;
            case FactoryStep.Traded:
                Close(Outcome);
                break;
        }
    }

    /// <summary>The B button.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case FactoryStep.Browse:
                // Renting: the last one chosen is taken back; in a trade: the way out
                if (Mode == FactoryScreenMode.Trade) AskToQuit(FactoryStep.Browse);
                else if (chosen.Count > 0) TakeBackLast();
                break;
            case FactoryStep.Options:
                Go(FactoryStep.Browse);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryStep.Summary:
                Go(FactoryStep.Options);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryStep.Confirm:
                TakeBackLast();
                break;
            case FactoryStep.Receive:
                AskToQuit(FactoryStep.Receive);
                break;
            case FactoryStep.Accept:
                Go(FactoryStep.Receive);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryStep.Quit:
                Go(quitFrom);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryStep.Traded:
                Close(Outcome);
                break;
        }
    }

    private FactoryStep quitFrom;

    private void AskToQuit(FactoryStep from)
    {
        quitFrom = from;
        // No is under the cursor first, as the original has it
        MenuCursor = 1;
        Go(FactoryStep.Quit);
        AudioManager.PlaySound("select");
    }

    private void Choose(FactoryOption option)
    {
        switch (option)
        {
            case FactoryOption.Summary:
                Go(FactoryStep.Summary);
                AudioManager.PlaySound("select");
                break;
            case FactoryOption.Rent:
                // SelectMon
                chosen.Add(Cursor);
                MenuCursor = 0;
                Go(chosen.Count == needed ? FactoryStep.Confirm : FactoryStep.Browse);
                AudioManager.PlaySound("select");
                break;
            case FactoryOption.Remove:
                // RemoveSelectedMonUnderCursor: whoever was chosen after it moves up
                chosen.Remove(Cursor);
                Go(FactoryStep.Browse);
                AudioManager.PlaySound("cancel");
                break;
            case FactoryOption.Exchange:
                // SelectMonToBeExchanged
                Giving = Cursor;
                TheirCursor = 0;
                Go(FactoryStep.Receive);
                AudioManager.PlaySound("select");
                break;
            default:
                Go(FactoryStep.Browse);
                AudioManager.PlaySound("cancel");
                break;
        }
    }

    // RemoveLastSelectedMon, and what a no to "are these the ones?" does
    private void TakeBackLast()
    {
        if (chosen.Count > 0) chosen.RemoveAt(chosen.Count - 1);
        Go(FactoryStep.Browse);
        AudioManager.PlaySound("cancel");
    }

    // GoBackToExchangeSelect
    private void BackToMine()
    {
        Giving = -1;
        Go(FactoryStep.Browse);
        AudioManager.PlaySound("cancel");
    }

    // ------------------------------------------------------------------ the frame

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        if (Step == FactoryStep.Summary && Shown is { } shown)
            ModernUi.DrawSummary(screenWidth, screenHeight, shown, Cursor, Mine.Count, Math.Clamp(StepAge / AppearTime, 0f, 1f), rental: true);
        else ModernUi.DrawFactory(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
