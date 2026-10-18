using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the PokéNav's screen hands back to the game once the player has chosen.</summary>
public enum PokeNavRequest { None, Call, Map, Close }

/// <summary>
/// The PokéNav (plan 21 · H2, part b; style guide, "Menu screens", "The PokéNav"): its pages as tabs. The map page
/// opens the map of Hoenn; the condition page shows a Pokémon of the team's contest condition round a pentagon; Match
/// Call lists the numbers registered and rings one; the ribbons page counts each Pokémon's ribbons. Its logic is in
/// methods that take no input (<see cref="MovePage"/>, <see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>), so tests and the harness drive it.
/// </summary>
public sealed class PokeNavScreen
{
    private readonly UiReveal reveal = new(0.2f, 0.12f);

    public bool IsActive { get; private set; }

    /// <summary>The pages it has, in their order, and the one on show.</summary>
    public IReadOnlyList<PokeNavPage> Pages { get; private set; } = new[] { PokeNavPage.Map };
    public int PageIndex { get; private set; }
    public PokeNavPage Page => Pages[Math.Clamp(PageIndex, 0, Pages.Count - 1)];

    /// <summary>The numbers registered, and the one under the cursor.</summary>
    public IReadOnlyList<PhoneContact> Contacts { get; private set; } = Array.Empty<PhoneContact>();
    public int ContactIndex { get; private set; }

    /// <summary>The team (no Eggs), and the Pokémon the condition and ribbons pages are on.</summary>
    public IReadOnlyList<Pokemon> Team { get; private set; } = Array.Empty<Pokemon>();
    public int PokemonIndex { get; private set; }

    /// <summary>How many rows of a list show at once.</summary>
    public const int VisibleRows = 7;

    /// <summary>Where a contact is met, by its area's key, as the game names it: the game's to set.</summary>
    public Func<string, string>? PlaceOf { get; set; }

    /// <summary>The player's cell of the region's map and the name of the place they stand in, for the map page: the game's to set.</summary>
    public (int X, int Y)? PlayerCell { get; set; }
    public string PlaceHere { get; set; } = "";
    public string? RegionName { get; set; }

    private PokeNavRequest request;

    /// <summary>The contact the player rang.</summary>
    public PhoneContact? Called { get; private set; }

    public void Open(IReadOnlyList<PokeNavPage> pages, IReadOnlyList<PhoneContact> contacts, IReadOnlyList<Pokemon> team, PokeNavPage? on = null)
    {
        Pages = pages.Count == 0 ? new[] { PokeNavPage.Map } : pages;
        Contacts = contacts;
        Team = team;
        PageIndex = 0;
        if (on is { } page)
            for (int i = 0; i < Pages.Count; i++)
                if (Pages[i] == page) PageIndex = i;
        ContactIndex = Math.Clamp(ContactIndex, 0, Math.Max(0, contacts.Count - 1));
        PokemonIndex = Math.Clamp(PokemonIndex, 0, Math.Max(0, team.Count - 1));
        Called = null;
        request = PokeNavRequest.None;
        IsActive = true;
        reveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>The request the player's last choice made, taken once.</summary>
    public PokeNavRequest TakeRequest()
    {
        var r = request;
        request = PokeNavRequest.None;
        return r;
    }

    // ------------------------------------------------------------------ logic

    /// <summary>To the next page or the one before, round the end.</summary>
    public void MovePage(int step)
    {
        if (!IsActive || step == 0 || Pages.Count < 2) return;
        PageIndex = ((PageIndex + step) % Pages.Count + Pages.Count) % Pages.Count;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The arrows: left and right change the page, up and down move down a list or to the next Pokémon.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        if (dx != 0)
        {
            MovePage(dx);
            return;
        }
        if (dy == 0) return;
        switch (Page)
        {
            case PokeNavPage.MatchCall when Contacts.Count > 0:
                ContactIndex = Math.Clamp(ContactIndex + dy, 0, Contacts.Count - 1);
                AudioManager.PlaySound("cursor");
                break;
            case PokeNavPage.Condition or PokeNavPage.Ribbons when Team.Count > 0:
                PokemonIndex = ((PokemonIndex + dy) % Team.Count + Team.Count) % Team.Count;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>The first row of a list on show.</summary>
    public int FirstRow(int index, int count) => Math.Clamp(index - VisibleRows / 2, 0, Math.Max(0, count - VisibleRows));

    /// <summary>The A button: rings the contact under the cursor, or opens the map.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        switch (Page)
        {
            case PokeNavPage.MatchCall when Contacts.Count > 0:
                Called = Contacts[ContactIndex];
                request = PokeNavRequest.Call;
                Close();
                break;
            case PokeNavPage.Map:
                request = PokeNavRequest.Map;
                Close();
                break;
        }
    }

    /// <summary>The B button: the PokéNav is put away.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        request = PokeNavRequest.Close;
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
        if (InputManager.IsActionPressed(GameAction.PageLeft)) MovePage(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) MovePage(1);
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
        ModernUi.PokeNav(this, screenWidth, screenHeight);
    }
}
