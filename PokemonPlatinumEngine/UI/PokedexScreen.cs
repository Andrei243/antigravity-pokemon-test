using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Pokédex's cursor is: its list, the entry beside it, the search panel or a diploma.</summary>
public enum PokedexFocus { List, Entry, Search, Diploma }

/// <summary>The pages of an entry.</summary>
public enum PokedexPage { Info, Area, Size }

/// <summary>The rows of the search panel; <see cref="Buttons"/> is the row of SEARCH, RESET and DIPLOMA.</summary>
public enum PokedexSearchRow { Mode, Order, Name, Type1, Type2, Shape, Buttons }

/// <summary>The buttons under the search panel's rows.</summary>
public enum PokedexSearchButton { Search, Reset, Diploma }

/// <summary>
/// The Pokédex (plan 03 · D10; style guide, "Menu screens"): the open Pokédex's species as a list, and the chosen
/// one beside it in three pages: what is known of it, where it lives and how big it is. The Start button opens the
/// search, which can also switch to the National Pokédex once it is open. A diploma is shown the first time the
/// Pokédex is opened after it is complete. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>, <see cref="Sideways"/>, <see cref="OpenSearch"/>), so tests and the harness drive it. A key
/// kept down runs on through the list and the entries (<see cref="HeldKey"/>).
/// </summary>
public class PokedexScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many species show at once, and how far left and right jump.</summary>
    public const int VisibleRows = 8, Jump = 10;

    public static readonly PokedexPage[] Pages = Enum.GetValues<PokedexPage>();
    public static readonly PokedexSearchButton[] AllButtons = Enum.GetValues<PokedexSearchButton>();

    private readonly List<PokedexEntry> rows = new();
    private readonly Queue<PokedexMode> diplomasDue = new();
    private Pokedex pokedex = new();
    private float openAge;
    private PokedexFocus beforeDiploma;
    private readonly HeldKey upDown = new(), leftRight = new();
    private readonly CursorTick tick = new();

    public int SelectedIndex { get; set; }
    public int FirstRow { get; private set; }
    public bool IsActive { get; set; }

    public PokedexFocus Focus { get; private set; }
    public PokedexPage Page { get; private set; }

    /// <summary>The Pokédex the list shows; the screen remembers it between openings.</summary>
    public PokedexMode Mode { get; private set; } = PokedexMode.Sinnoh;

    /// <summary>The search the list shows the results of, or null for the whole Pokédex.</summary>
    public PokedexQuery? Results { get; private set; }

    /// <summary>The search being set up in the panel, and which Pokédex it will look in.</summary>
    public PokedexQuery Draft { get; private set; } = new();
    public PokedexMode DraftMode { get; private set; }
    public int SearchIndex { get; private set; }
    public int ButtonIndex { get; private set; }

    /// <summary>The diploma being shown.</summary>
    public PokedexMode ShownDiploma { get; private set; }

    /// <summary>The player's own field sprite, which the size page sets beside the species.</summary>
    public Texture2D? Portrait { get; private set; }

    public Pokedex Pokedex => pokedex;
    public IReadOnlyList<PokedexEntry> Rows => rows;
    public IReadOnlyList<PokemonSpecies> Species => rows.Select(r => r.Species).ToList();
    public PokedexEntry? Selected => SelectedIndex >= 0 && SelectedIndex < rows.Count ? rows[SelectedIndex] : null;

    /// <summary>
    /// Opens on the first species the player has seen, so the list doesn't begin with a page of dashes, and shows
    /// any diploma that has become due.
    /// </summary>
    public void Open(Pokedex? pokedex = null, Texture2D? portrait = null)
    {
        IsActive = true;
        openAge = 0f;
        upDown.Release();
        leftRight.Release();
        this.pokedex = pokedex ?? new Pokedex();
        Portrait = portrait;
        if (!this.pokedex.Modes.Contains(Mode)) Mode = PokedexMode.Sinnoh;
        Results = null;
        Focus = PokedexFocus.List;
        Page = PokedexPage.Info;
        Rebuild();
        int firstSeen = rows.FindIndex(r => this.pokedex.IsSeen(r.Species.DexNumber));
        SelectedIndex = Math.Max(0, firstSeen);
        FirstRow = 0;
        Follow();

        diplomasDue.Clear();
        foreach (var mode in this.pokedex.AwardDiplomas()) diplomasDue.Enqueue(mode);
        if (diplomasDue.Count > 0) ShowDiploma(diplomasDue.Dequeue());
    }

    public void Close() => IsActive = false;

    private void Rebuild()
    {
        rows.Clear();
        rows.AddRange(Results is { } query ? PokedexSearch.Run(pokedex, Mode, query) : Pokedex.Entries(Mode));
    }

    // ------------------------------------------------------------------ moving

    /// <summary>
    /// In the list, one step wraps from the last species to the first and a jump of ten stops at either end. In an
    /// entry, a step goes to the previous or next species the player has seen. In the search panel it changes row.
    /// A step that comes from a key kept down (<paramref name="held"/>) stops at either end instead of wrapping,
    /// so holding a key brings the cursor to the end of the list and leaves it there.
    /// </summary>
    public void Move(int step, bool held = false)
    {
        if (step == 0) return;
        switch (Focus)
        {
            case PokedexFocus.List:
            {
                if (rows.Count == 0) return;
                int next = Math.Abs(step) == 1 && !held
                    ? UiNav.Wrap(SelectedIndex, step, rows.Count)
                    : Math.Clamp(SelectedIndex + step, 0, rows.Count - 1);
                if (next == SelectedIndex) return;
                SelectedIndex = next;
                Follow();
                Tick(held);
                break;
            }
            case PokedexFocus.Entry:
            {
                int i = SelectedIndex;
                for (int n = 0; n < rows.Count; n++)
                {
                    int beyond = i + Math.Sign(step);
                    if (held && (beyond < 0 || beyond >= rows.Count)) break;
                    i = UiNav.Wrap(i, Math.Sign(step), rows.Count);
                    if (!pokedex.IsSeen(rows[i].Species.DexNumber)) continue;
                    if (i != SelectedIndex)
                    {
                        SelectedIndex = i;
                        Follow();
                        Tick(held);
                    }
                    break;
                }
                break;
            }
            case PokedexFocus.Search:
                SearchIndex = UiNav.Wrap(SearchIndex, Math.Sign(step), SearchRows.Count);
                ButtonIndex = Math.Min(ButtonIndex, Buttons.Count - 1);
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>Left and right: a jump of ten in the list, a page in an entry, a value or a button in the search panel.</summary>
    public void Sideways(int step, bool held = false)
    {
        if (step == 0) return;
        switch (Focus)
        {
            case PokedexFocus.List:
                Move(step * Jump, held);
                break;
            case PokedexFocus.Entry:
                Page = Pages[UiNav.Wrap(Array.IndexOf(Pages, Page), Math.Sign(step), Pages.Length)];
                AudioManager.PlaySound("page");
                break;
            case PokedexFocus.Search:
                ChangeValue(Math.Sign(step));
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>The cursor's tick. A key held down takes steps faster than the tick lasts, so not every one of those sounds.</summary>
    private void Tick(bool held)
    {
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    private void Follow()
    {
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, rows.Count - 1));
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, rows.Count, VisibleRows);
    }

    // ------------------------------------------------------------------ the A and B buttons

    public void Confirm()
    {
        switch (Focus)
        {
            case PokedexFocus.List:
                if (Selected is { } entry && pokedex.IsSeen(entry.Species.DexNumber))
                {
                    Focus = PokedexFocus.Entry;
                    AudioManager.PlaySound("select");
                }
                break;
            case PokedexFocus.Search:
                if (SearchRows[SearchIndex] != PokedexSearchRow.Buttons) Move(1);
                else Press(Buttons[ButtonIndex]);
                break;
            case PokedexFocus.Diploma:
                CloseDiploma();
                break;
        }
    }

    public void Cancel()
    {
        switch (Focus)
        {
            case PokedexFocus.Entry:
                Focus = PokedexFocus.List;
                Page = PokedexPage.Info;
                break;
            case PokedexFocus.Search:
                Focus = PokedexFocus.List;
                break;
            case PokedexFocus.Diploma:
                CloseDiploma();
                return;
            case PokedexFocus.List when Results != null:
                ShowAll();
                break;
            default:
                Close();
                break;
        }
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Back from search results to the whole Pokédex, still on the species that was chosen.</summary>
    private void ShowAll()
    {
        var chosen = Selected?.Species;
        Results = null;
        Rebuild();
        SelectedIndex = chosen == null ? 0 : Math.Max(0, rows.FindIndex(r => r.Species == chosen));
        Follow();
    }

    // ------------------------------------------------------------------ the search panel

    /// <summary>The rows the search panel has: which Pokédex only once the National one is open.</summary>
    public IReadOnlyList<PokedexSearchRow> SearchRows =>
        Enum.GetValues<PokedexSearchRow>().Where(r => r != PokedexSearchRow.Mode || pokedex.NationalUnlocked).ToList();

    /// <summary>The buttons under the rows: DIPLOMA only once one has been given.</summary>
    public IReadOnlyList<PokedexSearchButton> Buttons =>
        AllButtons.Where(b => b != PokedexSearchButton.Diploma || pokedex.Diplomas.Count > 0).ToList();

    /// <summary>The types the search offers: every type a species has.</summary>
    public static readonly PokemonType[] SearchTypes = Enum.GetValues<PokemonType>();

    public void OpenSearch()
    {
        if (Focus is not (PokedexFocus.List or PokedexFocus.Entry)) return;
        Focus = PokedexFocus.Search;
        Draft = Results ?? new PokedexQuery();
        DraftMode = Mode;
        SearchIndex = 0;
        ButtonIndex = 0;
        AudioManager.PlaySound("select");
    }

    private void ChangeValue(int step)
    {
        static T? Cycle<T>(T? value, T[] values, int step) where T : struct
        {
            // null ("any") comes before the first value
            int i = value is { } v ? Array.IndexOf(values, v) + 1 : 0;
            i = UiNav.Wrap(i, step, values.Length + 1);
            return i == 0 ? null : values[i - 1];
        }

        switch (SearchRows[SearchIndex])
        {
            case PokedexSearchRow.Mode:
                var modes = pokedex.Modes;
                DraftMode = modes[UiNav.Wrap(Math.Max(0, modes.ToList().IndexOf(DraftMode)), step, modes.Count)];
                break;
            case PokedexSearchRow.Order:
                var orders = Enum.GetValues<PokedexOrder>();
                Draft = Draft with { Order = orders[UiNav.Wrap(Array.IndexOf(orders, Draft.Order), step, orders.Length)] };
                break;
            case PokedexSearchRow.Name:
                Draft = Draft with { NameGroup = UiNav.Wrap(Draft.NameGroup + 1, step, PokedexSearch.NameGroups.Length + 1) - 1 };
                break;
            case PokedexSearchRow.Type1:
                Draft = Draft with { Type1 = Cycle(Draft.Type1, SearchTypes, step) };
                break;
            case PokedexSearchRow.Type2:
                Draft = Draft with { Type2 = Cycle(Draft.Type2, SearchTypes, step) };
                break;
            case PokedexSearchRow.Shape:
            {
                int i = UiNav.Wrap(Draft.Shape == null ? 0 : Array.IndexOf(PokedexSearch.Shapes, Draft.Shape) + 1, step, PokedexSearch.Shapes.Length + 1);
                Draft = Draft with { Shape = i == 0 ? null : PokedexSearch.Shapes[i - 1] };
                break;
            }
            case PokedexSearchRow.Buttons:
                ButtonIndex = UiNav.Wrap(ButtonIndex, step, Buttons.Count);
                break;
        }
    }

    /// <summary>What a row of the search panel says now.</summary>
    public string ValueOf(PokedexSearchRow row) => row switch
    {
        PokedexSearchRow.Mode => DraftMode == PokedexMode.Sinnoh ? "Sinnoh" : "National",
        PokedexSearchRow.Order => PokedexSearch.NameOf(Draft.Order),
        PokedexSearchRow.Name => PokedexSearch.NameOfGroup(Draft.NameGroup),
        PokedexSearchRow.Type1 => Draft.Type1?.ToString() ?? "Any",
        PokedexSearchRow.Type2 => Draft.Type2?.ToString() ?? "Any",
        PokedexSearchRow.Shape => Draft.Shape ?? "Any",
        _ => ""
    };

    private void Press(PokedexSearchButton button)
    {
        switch (button)
        {
            case PokedexSearchButton.Search:
                Search();
                break;
            case PokedexSearchButton.Reset:
                Draft = new PokedexQuery();
                AudioManager.PlaySound("select");
                break;
            case PokedexSearchButton.Diploma:
                ShowDiploma(pokedex.Diplomas.Contains(DraftMode) ? DraftMode : pokedex.Diplomas.Min());
                break;
        }
    }

    /// <summary>Runs the search set up in the panel: the list shows what it finds, or the whole Pokédex for a plain one.</summary>
    public void Search()
    {
        Mode = DraftMode;
        Results = Draft.IsPlain ? null : Draft;
        Rebuild();
        Focus = PokedexFocus.List;
        Page = PokedexPage.Info;
        SelectedIndex = 0;
        FirstRow = 0;
        Follow();
        AudioManager.PlaySound("select");
    }

    // ------------------------------------------------------------------ diplomas

    public void ShowDiploma(PokedexMode mode)
    {
        if (Focus != PokedexFocus.Diploma) beforeDiploma = Focus;
        Focus = PokedexFocus.Diploma;
        ShownDiploma = mode;
        AudioManager.PlaySound("levelup");
    }

    private void CloseDiploma()
    {
        if (diplomasDue.Count > 0)
        {
            ShownDiploma = diplomasDue.Dequeue();
            return;
        }
        Focus = beforeDiploma;
        AudioManager.PlaySound("select");
    }

    // ------------------------------------------------------------------ each frame

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        tick.Update(dt);

        // A key kept down runs through the list (a species at a time, or ten) and through the entries; in the
        // search panel, and for an entry's pages, only a press counts
        bool scrolls = Focus is PokedexFocus.List or PokedexFocus.Entry;
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            scrolls ? InputManager.Axis(GameAction.Up, GameAction.Down, held: true) : 0);
        int dx = leftRight.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right),
            Focus == PokedexFocus.List ? InputManager.Axis(GameAction.Left, GameAction.Right, held: true) : 0);
        for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating);
        for (int i = 0; i < Math.Abs(dx); i++) Sideways(Math.Sign(dx), leftRight.Repeating);

        // A button pressed while the list runs on still counts: it acts on the species the cursor has come to
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Menu))
        {
            if (Focus == PokedexFocus.Search) Search();
            else if (Focus == PokedexFocus.Diploma) Confirm();
            else OpenSearch();
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight, Pokedex pokedex)
    {
        if (!IsActive) return;
        Follow();
        ModernUi.DrawPokedex(screenWidth, screenHeight, this, pokedex, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
