using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the World Tournament's screen stands.</summary>
public enum TournamentStep
{
    /// <summary>The events to enter, one under another, a locked one greyed.</summary>
    Events,
    /// <summary>The bracket of eight, the cursor on an entrant.</summary>
    Tree,
    /// <summary>An entrant's card: who they are, their three at level 50, how far they have come.</summary>
    Card
}

/// <summary>
/// The World Tournament's screen (plan 14 · B11; style guide, "Menu screens (G10)", "The World Tournament"): opened at
/// the desk on its events, to choose one (a locked event says what opens it and can't be chosen), and in the hall on
/// the bracket of eight, two halves of four joined toward the final in the middle, with an entrant's card. The pick
/// of three is the team's own screen in its entering mode, as every facility's is, and the clauses are held after it.
/// Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness
/// drive it.
/// </summary>
public sealed class TournamentScreen
{
    public const int HalfSeats = Tournament.Entrants / 2;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public TournamentStep Step { get; private set; }

    /// <summary>The tournament the bracket is of; null on the events.</summary>
    public Tournament? Tournament { get; private set; }

    /// <summary>The events and whether each may be entered, as the screen opened.</summary>
    public IReadOnlyList<TournamentEventRecord> Events { get; private set; } = Array.Empty<TournamentEventRecord>();
    public IReadOnlyList<bool> Open { get; private set; } = Array.Empty<bool>();

    /// <summary>The titles won of each event, beside it.</summary>
    public IReadOnlyList<int> Titles { get; private set; } = Array.Empty<int>();

    /// <summary>The event under the cursor, or the seat on the tree (0 to 3 the left half top to bottom, 4 to 7 the right).</summary>
    public int Cursor { get; private set; }

    /// <summary>The event chosen counted from 1; 0 for one backed out of or a bracket only looked at.</summary>
    public int Answer { get; private set; }

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    public int EntrantAt(int seat) => Tournament?.Bracket.Seats[seat] ?? -1;
    public int SeatOf(int entrant) => Tournament?.Bracket.Seats.IndexOf(entrant) ?? -1;

    /// <summary>The events, to choose one to enter (the script's <c>tournament events</c>): the cursor on the first open one.</summary>
    public void OpenEvents(IReadOnlyList<TournamentEventRecord> events, TournamentRecord record)
    {
        Tournament = null;
        Events = events.ToList();
        Open = Events.Select(e => Models.Tournament.IsOpen(e, record)).ToList();
        Titles = Events.Select(e => record.TitlesOf(e.Id)).ToList();
        Cursor = Math.Max(0, Open.ToList().IndexOf(true));
        Begin(TournamentStep.Events);
    }

    /// <summary>The bracket, to be looked at (the script's <c>tournament show</c>): the cursor on the player's opponent, or on the player.</summary>
    public void OpenBracket(Tournament tournament)
    {
        Tournament = tournament;
        int opponent = tournament.Bracket.IsOver ? -1 : tournament.Bracket.OpponentOf(tournament.Round, Models.Tournament.Player);
        Cursor = SeatOf(opponent >= 0 ? opponent : Models.Tournament.Player);
        Begin(TournamentStep.Tree);
    }

    private void Begin(TournamentStep step)
    {
        IsActive = true;
        Answer = 0;
        Age = 0f;
        Go(step);
        AudioManager.PlaySound("select");
    }

    private void Go(TournamentStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    public string Prompt => Step switch
    {
        TournamentStep.Events when Cursor < Open.Count && !Open[Cursor] => Text.Of("tournamentScreen.prompt.locked"),
        TournamentStep.Events => Text.Of("tournamentScreen.prompt.events"),
        _ => Text.Of("tournamentScreen.prompt.look")
    };

    // ------------------------------------------------------------------ the buttons

    /// <summary>On the events up and down go through them; on the tree up and down along a half and round it, left and right to the other half.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || dx == 0 && dy == 0) return;
        switch (Step)
        {
            case TournamentStep.Events:
                if (dy == 0 || Events.Count == 0) return;
                Cursor = (Cursor + dy + Events.Count) % Events.Count;
                break;
            case TournamentStep.Tree:
                if (dx != 0) Cursor = (Cursor + HalfSeats) % Models.Tournament.Entrants;
                else Cursor = Cursor / HalfSeats * HalfSeats + ((Cursor % HalfSeats + dy + HalfSeats) % HalfSeats);
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case TournamentStep.Events:
                if (Cursor >= Open.Count || !Open[Cursor]) { AudioManager.PlaySound("error"); return; }
                Answer = Cursor + 1;
                IsActive = false;
                AudioManager.PlaySound("select");
                break;
            case TournamentStep.Tree:
                Go(TournamentStep.Card);
                AudioManager.PlaySound("select");
                break;
            case TournamentStep.Card:
                Go(TournamentStep.Tree);
                AudioManager.PlaySound("cancel");
                break;
        }
    }

    /// <summary>Back: a card to the tree, the tree or the events away (no event chosen).</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        if (Step == TournamentStep.Card) Go(TournamentStep.Tree);
        else
        {
            Answer = 0;
            IsActive = false;
        }
        AudioManager.PlaySound("cancel");
    }

    // ------------------------------------------------------------------ the frame

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawTournament(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f), Math.Clamp(StepAge / AppearTime, 0f, 1f));
    }
}
