using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Pokégear's screen hands back to the game once the player has chosen.</summary>
public enum PokegearRequest { None, Call, Map, Close }

/// <summary>
/// The Pokégear (plan 20 · J3; style guide, "Menu screens", "The Pokégear"): its cards as tabs, with the clock over
/// them. The phone lists the numbers registered and rings one; the map card opens the map of the region; the radio
/// is a round dial with a cursor the arrows move, and the station under it plays or talks. Its logic is in methods
/// that take no input (<see cref="MoveCard"/>, <see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>),
/// so tests and the harness drive it; what the station says and plays is the game's, set on the screen each frame.
/// </summary>
public sealed class PokegearScreen
{
    private readonly UiReveal reveal = new(0.2f, 0.12f);
    // How long an arrow has been held on the radio's dial
    private float dialHeld;
    private const float DialRepeat = 0.05f;

    public bool IsActive { get; private set; }

    /// <summary>The cards held, in their order, and the one on show.</summary>
    public IReadOnlyList<PokegearCard> Cards { get; private set; } = new[] { PokegearCard.Phone };
    public int CardIndex { get; private set; }
    public PokegearCard Card => Cards[Math.Clamp(CardIndex, 0, Cards.Count - 1)];

    /// <summary>The numbers registered, and the one under the cursor.</summary>
    public IReadOnlyList<PhoneContact> Contacts { get; private set; } = Array.Empty<PhoneContact>();
    public int ContactIndex { get; private set; }

    /// <summary>How many rows of the phone's list show at once.</summary>
    public const int VisibleRows = 7;

    /// <summary>The radio's dial.</summary>
    public RadioDial Dial { get; } = new();

    /// <summary>What the station under the cursor is, how it comes in and what it says now: the game's to set.</summary>
    public RadioStation? Station { get; set; }
    public bool Clear { get; set; }
    public string StationLine { get; set; } = "";

    /// <summary>The song the radio is playing over the field, if any.</summary>
    public RadioSong Playing { get; set; }

    /// <summary>The clock on the cards: the day and the hour, the game's to set.</summary>
    public DateTime Now { get; set; }

    /// <summary>Where the region's map was drawn from, for the map card's picture: the game's to set.</summary>
    public string? RegionName { get; set; }

    /// <summary>Where a contact is met, by its area's key, as the game names it; and whether they wait for a rematch: the game's to set.</summary>
    public Func<string, string>? PlaceOf { get; set; }
    public Func<PhoneContact, bool>? Waiting { get; set; }

    /// <summary>The player's cell of the region's map and the name of the place they stand in, for the map card.</summary>
    public (int X, int Y)? PlayerCell { get; set; }
    public string PlaceHere { get; set; } = "";

    private PokegearRequest request;

    /// <summary>The contact the player rang.</summary>
    public PhoneContact? Called { get; private set; }

    public void Open(IReadOnlyList<PokegearCard> cards, IReadOnlyList<PhoneContact> contacts, PokegearCard? on = null)
    {
        Cards = cards.Count == 0 ? new[] { PokegearCard.Phone } : cards;
        Contacts = contacts;
        CardIndex = on is { } card && IndexOf(card) is >= 0 and var i ? i : 0;
        ContactIndex = Math.Clamp(ContactIndex, 0, Math.Max(0, contacts.Count - 1));
        Called = null;
        request = PokegearRequest.None;
        IsActive = true;
        reveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    private int IndexOf(PokegearCard card)
    {
        for (int i = 0; i < Cards.Count; i++) if (Cards[i] == card) return i;
        return -1;
    }

    /// <summary>The request the player's last choice made, taken once.</summary>
    public PokegearRequest TakeRequest()
    {
        var r = request;
        request = PokegearRequest.None;
        return r;
    }

    // ------------------------------------------------------------------ logic

    /// <summary>To the next card or the one before, round the end.</summary>
    public void MoveCard(int step)
    {
        if (!IsActive || step == 0 || Cards.Count < 2) return;
        CardIndex = ((CardIndex + step) % Cards.Count + Cards.Count) % Cards.Count;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The arrows: the phone's list up and down, the radio's cursor any way; left and right change the other cards.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        if (Card == PokegearCard.Radio)
        {
            Dial.Move(dx, dy);
            return;
        }
        if (dx != 0)
        {
            MoveCard(dx);
            return;
        }
        if (Card == PokegearCard.Phone && Contacts.Count > 0 && dy != 0)
        {
            ContactIndex = Math.Clamp(ContactIndex + dy, 0, Contacts.Count - 1);
            AudioManager.PlaySound("cursor");
        }
    }

    /// <summary>The first row of the phone's list on show.</summary>
    public int FirstRow => Math.Clamp(ContactIndex - VisibleRows / 2, 0, Math.Max(0, Contacts.Count - VisibleRows));

    /// <summary>The A button: rings the contact under the cursor, or opens the map.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        switch (Card)
        {
            case PokegearCard.Phone when Contacts.Count > 0:
                Called = Contacts[ContactIndex];
                request = PokegearRequest.Call;
                Close();
                break;
            case PokegearCard.Map:
                request = PokegearRequest.Map;
                Close();
                break;
        }
    }

    /// <summary>The B button: the Pokégear is put away (the radio plays on).</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        request = PokegearRequest.Close;
        Close();
        AudioManager.PlaySound("menu_close");
    }

    private void Close()
    {
        IsActive = false;
        reveal.Snap(false);
    }

    public void Update(float dt)
    {
        reveal.Update(dt);
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.PageLeft)) MoveCard(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) MoveCard(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (Card == PokegearCard.Radio)
        {
            // The dial's cursor moves while an arrow is held
            int dx = (InputManager.IsActionDown(GameAction.Right) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Left) ? 1 : 0);
            int dy = (InputManager.IsActionDown(GameAction.Down) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Up) ? 1 : 0);
            if (dx == 0 && dy == 0) dialHeld = 0f;
            else
            {
                if (dialHeld <= 0f) Move(dx, dy);
                dialHeld += dt;
                // (a step every twentieth of a second once held)
                while (dialHeld >= DialRepeat)
                {
                    dialHeld -= DialRepeat;
                    Move(dx, dy);
                }
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Left)) Move(-1, 0);
        else if (InputManager.IsActionPressed(GameAction.Right)) Move(1, 0);
        else if (InputManager.IsActionPressed(GameAction.Up)) Move(0, -1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(0, 1);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.Pokegear(this, screenWidth, screenHeight);
    }
}
