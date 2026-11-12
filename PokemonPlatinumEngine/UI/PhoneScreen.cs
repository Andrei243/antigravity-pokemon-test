using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the travel phone's screen hands back to the game once the player has chosen.</summary>
public enum PhoneRequest { None, Call, Map, Close, Powers }

/// <summary>
/// The travel phone (plan 43 · LF6; style guide, "Menu screens", "The travel phone"): its cards as tabs. CALLS lists
/// the region's numbers saved and rings one; NEWS shows the day's programme in its lines; MAP opens the map of the
/// region. Its logic is in methods that take no input (<see cref="MoveCard"/>, <see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it; what it shows is the game's, set
/// on the screen as it opens.
/// </summary>
public sealed class PhoneScreen
{
    private readonly UiReveal reveal = new(0.2f, 0.12f);

    public bool IsActive { get; private set; }

    /// <summary>The cards, in their order, and the one on show.</summary>
    public IReadOnlyList<PhoneCard> Cards { get; private set; } = new[] { PhoneCard.Calls };
    public int CardIndex { get; private set; }
    public PhoneCard Card => Cards[Math.Clamp(CardIndex, 0, Cards.Count - 1)];

    /// <summary>The numbers saved, and the one under the cursor.</summary>
    public IReadOnlyList<PhoneContact> Contacts { get; private set; } = Array.Empty<PhoneContact>();
    public int ContactIndex { get; private set; }

    /// <summary>How many rows of the list show at once.</summary>
    public const int VisibleRows = 7;

    /// <summary>The day's news, its lines; the game's to set.</summary>
    public IReadOnlyList<string> News { get; set; } = Array.Empty<string>();

    /// <summary>The region the phone is used in (its colour and its map), the player's cell and place: the game's to set.</summary>
    public string? RegionName { get; set; }
    public (int X, int Y)? PlayerCell { get; set; }
    public string PlaceHere { get; set; } = "";

    /// <summary>Where a contact is met, by its area's key, as the game names it; and whether they wait for a rematch.</summary>
    public Func<string, string>? PlaceOf { get; set; }
    public Func<PhoneContact, bool>? Waiting { get; set; }

    private PhoneRequest request;

    /// <summary>The contact the player rang.</summary>
    public PhoneContact? Called { get; private set; }

    public void Open(IReadOnlyList<PhoneCard> cards, IReadOnlyList<PhoneContact> contacts, PhoneCard? on = null)
    {
        Cards = cards.Count == 0 ? new[] { PhoneCard.Calls } : cards;
        Contacts = contacts;
        CardIndex = 0;
        if (on is { } card)
            for (int i = 0; i < Cards.Count; i++)
                if (Cards[i] == card) CardIndex = i;
        ContactIndex = Math.Clamp(ContactIndex, 0, Math.Max(0, contacts.Count - 1));
        Called = null;
        request = PhoneRequest.None;
        IsActive = true;
        reveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>The request the player's last choice made, taken once.</summary>
    public PhoneRequest TakeRequest()
    {
        var r = request;
        request = PhoneRequest.None;
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

    /// <summary>The arrows: left and right change the card, up and down move down the list of CALLS.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        if (dx != 0)
        {
            MoveCard(dx);
            return;
        }
        if (Card == PhoneCard.Calls && Contacts.Count > 0 && dy != 0)
        {
            ContactIndex = Math.Clamp(ContactIndex + dy, 0, Contacts.Count - 1);
            AudioManager.PlaySound("cursor");
        }
    }

    /// <summary>The first row of the list on show.</summary>
    public int FirstRow => Math.Clamp(ContactIndex - VisibleRows / 2, 0, Math.Max(0, Contacts.Count - VisibleRows));

    /// <summary>The A button: rings the contact under the cursor, or opens the map.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        switch (Card)
        {
            case PhoneCard.Calls when Contacts.Count > 0:
                Called = Contacts[ContactIndex];
                request = PhoneRequest.Call;
                Close();
                break;
            case PhoneCard.Map:
                request = PhoneRequest.Map;
                Close();
                break;
            case PhoneCard.Powers:
                request = PhoneRequest.Powers;
                Close();
                break;
        }
    }

    /// <summary>The B button: the phone is put away.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        request = PhoneRequest.Close;
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
        else if (InputManager.IsActionPressed(GameAction.Left)) Move(-1, 0);
        else if (InputManager.IsActionPressed(GameAction.Right)) Move(1, 0);
        else if (InputManager.IsActionPressed(GameAction.Up)) Move(0, -1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(0, 1);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.Phone(this, screenWidth, screenHeight);
    }
}
