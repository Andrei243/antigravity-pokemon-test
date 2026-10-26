using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Battle Dome's bracket stands.</summary>
public enum BracketStep
{
    /// <summary>The tree of sixteen, the cursor on an entrant.</summary>
    Tree,
    /// <summary>An entrant's card: who they are, their three, how far they have come.</summary>
    Card,
    /// <summary>The opponent's card beside the player's three, two to be sent in.</summary>
    Pick
}

/// <summary>
/// The Battle Dome's bracket (plan 14 · B8; style guide, "Menu screens (G10)", "The bracket"): the sixteen of a
/// tournament in two halves of eight, the matches decided drawn as lines joining the winners round by round toward
/// the final in the middle, and an entrant's card. Opened to be looked at it closes on Back; opened to pick it shows
/// the opponent's card beside the player's three and closes only once two are chosen and BATTLE pressed, with Back
/// going to the tree and back again. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it.
/// </summary>
public sealed class BracketScreen
{
    /// <summary>The seats of a half, and the place of BATTLE after the three in the pick.</summary>
    public const int HalfSeats = BattleDomeChallenge.Entrants / 2, GoPlace = BattleDomeChallenge.TeamSize;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public BracketStep Step { get; private set; }
    public BattleDomeChallenge? Dome { get; private set; }

    /// <summary>The player's three, as they are in the team.</summary>
    public IReadOnlyList<Pokemon> Own { get; private set; } = Array.Empty<Pokemon>();

    /// <summary>Opened to send two in (the script's <c>bracket pick</c>), not only to be looked at.</summary>
    public bool Picking { get; private set; }

    /// <summary>The seat under the cursor on the tree, 0 to 15: 0 to 7 the left half top to bottom, 8 to 15 the right.</summary>
    public int Cursor { get; private set; }

    /// <summary>The place under the cursor in the pick: the player's three (0 to 2), then BATTLE.</summary>
    public int PickCursor { get; private set; }

    /// <summary>The places of the three chosen to be sent in, in the order chosen.</summary>
    public List<int> Chosen { get; } = new();

    /// <summary>1 once two were chosen and BATTLE pressed; 0 for a bracket only looked at.</summary>
    public int Answer { get; private set; }

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    /// <summary>The entrant in a seat.</summary>
    public int EntrantAt(int seat) => Dome?.Bracket.Seats[seat] ?? -1;

    /// <summary>The seat an entrant sits in.</summary>
    public int SeatOf(int entrant) => Dome?.Bracket.Seats.IndexOf(entrant) ?? -1;

    /// <summary>The bracket is opened (the script's <c>bracket show</c> or <c>bracket pick</c>): the cursor on the player's opponent, or on the player.</summary>
    public void Open(BattleDomeChallenge dome, IReadOnlyList<Pokemon> own, bool pick)
    {
        Dome = dome;
        Own = own;
        Picking = pick && dome.Opponent != null;
        IsActive = true;
        Answer = 0;
        Chosen.Clear();
        PickCursor = 0;
        int opponent = dome.Bracket.OpponentOf(dome.Round, BattleDomeChallenge.Player);
        Cursor = SeatOf(opponent >= 0 && !dome.Bracket.IsOver ? opponent : BattleDomeChallenge.Player);
        Age = 0f;
        Go(Picking ? BracketStep.Pick : BracketStep.Tree);
        AudioManager.PlaySound("select");
    }

    private void Go(BracketStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    /// <summary>BATTLE may be pressed: two of the three are chosen.</summary>
    public bool Ready => Chosen.Count == BattleDomeChallenge.Fielded;

    public string Prompt => Step switch
    {
        BracketStep.Pick when Ready => Text.Of("bracketScreen.prompt.ready"),
        BracketStep.Pick => Text.Of("bracketScreen.prompt.pick"),
        _ when Picking => Text.Of("bracketScreen.prompt.back"),
        _ => Text.Of("bracketScreen.prompt.look")
    };

    // ------------------------------------------------------------------ the buttons

    /// <summary>On the tree up and down go along a half and round it, left and right to the same place of the other half; in the pick up and down go over the three and BATTLE.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || dx == 0 && dy == 0) return;
        switch (Step)
        {
            case BracketStep.Tree:
                if (dx != 0) Cursor = (Cursor + HalfSeats) % BattleDomeChallenge.Entrants;
                else Cursor = Cursor / HalfSeats * HalfSeats + ((Cursor % HalfSeats + dy + HalfSeats) % HalfSeats);
                break;
            case BracketStep.Pick:
                if (dy == 0) return;
                PickCursor = (PickCursor + dy + GoPlace + 1) % (GoPlace + 1);
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
            case BracketStep.Tree:
                Go(BracketStep.Card);
                AudioManager.PlaySound("select");
                break;
            case BracketStep.Card:
                Go(BracketStep.Tree);
                AudioManager.PlaySound("cancel");
                break;
            case BracketStep.Pick:
                if (PickCursor == GoPlace)
                {
                    if (!Ready) { AudioManager.PlaySound("error"); return; }
                    Answer = 1;
                    IsActive = false;
                    AudioManager.PlaySound("select");
                    return;
                }
                if (Chosen.Remove(PickCursor)) { AudioManager.PlaySound("cancel"); return; }
                if (Ready) { AudioManager.PlaySound("error"); return; }
                Chosen.Add(PickCursor);
                // Two chosen: the cursor goes on to BATTLE
                if (Ready) PickCursor = GoPlace;
                AudioManager.PlaySound("select");
                break;
        }
    }

    /// <summary>Back: a card to the tree, the tree to the pick (when picking) or away, the pick to the tree.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case BracketStep.Card:
                Go(BracketStep.Tree);
                break;
            case BracketStep.Tree when Picking:
                Go(BracketStep.Pick);
                break;
            case BracketStep.Tree:
                IsActive = false;
                break;
            case BracketStep.Pick:
                Go(BracketStep.Tree);
                break;
        }
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The places of the three chosen as the screen closed, for the script (<see cref="BattleDomeChallenge.Send"/>).</summary>
    public IReadOnlyList<int> Outcome => Answer == 1 ? Chosen.ToList() : Array.Empty<int>();

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
        if (!IsActive || Dome == null) return;
        ModernUi.DrawBracket(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f), Math.Clamp(StepAge / AppearTime, 0f, 1f));
    }
}
