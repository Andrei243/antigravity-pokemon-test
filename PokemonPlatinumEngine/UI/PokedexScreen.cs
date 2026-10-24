using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Pokédex's cursor is: its list, the entry beside it, the search panel or a diploma.</summary>
/// <summary>Where the cursor is: the list, an entry's pages, the search panel, a diploma, or the cry page's rows (plan 08 · P18).</summary>
public enum PokedexFocus { List, Entry, Search, Diploma, Cry, Forms }

/// <summary>The pages of an entry.</summary>
public enum PokedexPage { Info, Area, Cry, Size, Forms }

/// <summary>The cry page's rows, top to bottom (plan 08 · P18).</summary>
public enum PokedexCryRow { Pitch, Stereo, Room, Loop, Play }

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

    /// <summary>
    /// The pages an entry offers, in order (plan 08 · P18): INFO, AREA, CRY and SIZE for a species seen. The FORMS page
    /// joins them once the Pokédex can show forms (plan 08 · P19).
    /// </summary>
    public IReadOnlyList<PokedexPage> PagesOf(PokedexEntry? entry) => FormsKnown
        ? new[] { PokedexPage.Info, PokedexPage.Area, PokedexPage.Cry, PokedexPage.Size, PokedexPage.Forms }
        : new[] { PokedexPage.Info, PokedexPage.Area, PokedexPage.Cry, PokedexPage.Size };

    // ---- The forms page (plan 08 · P19)

    /// <summary>The cards a row of the forms page holds, and how many rows show at once.</summary>
    public const int FormsColumns = 8, FormsVisibleRows = 2;

    /// <summary>
    /// Whether the Pokédex can show forms: Professor Rowan's aide has upgraded it (<see cref="Story.StoryState.FormDetectionFlag"/>),
    /// which the engine tells the screen as it opens. Until then FORMS is not among the pages.
    /// </summary>
    public bool FormsKnown { get; set; }

    /// <summary>The card the cursor is on, among <see cref="Looks"/>.</summary>
    public int FormsIndex { get; private set; }

    /// <summary>The first row of cards shown (the grid scrolls past two rows: Unown's twenty-eight marks).</summary>
    public int FormsFirstRow { get; private set; }

    /// <summary>Which of the look's written entries is shown; A turns to the next.</summary>
    public int FormsEntry { get; private set; }

    /// <summary>
    /// The looks of the species selected, in the order they were first seen. A species seen with no look recorded
    /// (a save rebuilt without one) has its own form, no gender known, so the page is never empty.
    /// </summary>
    public IReadOnlyList<SeenLook> Looks => Selected is { } entry ? LooksOf(entry.Species) : System.Array.Empty<SeenLook>();

    /// <summary>
    /// A species' looks as its FORMS page shows them: those seen, and after them its shiny look once one has been
    /// caught shiny (plan 09 · L1; its name is the species' look name, <c>Gyarados+s</c>).
    /// </summary>
    private IReadOnlyList<SeenLook> LooksOf(PokemonSpecies species)
    {
        IReadOnlyList<SeenLook> seen = pokedex.LooksOf(species.DexNumber) is { Count: > 0 } looks ? looks : new[] { new SeenLook(species.Name, Gender.Genderless) };
        if (!pokedex.IsCaughtShiny(species.DexNumber)) return seen;
        return seen.Append(new SeenLook(PokemonLook.Shiny(species.Name), Gender.Genderless)).ToList();
    }

    /// <summary>The look the cursor is on.</summary>
    public SeenLook? LookChosen => Looks.Count == 0 ? null : Looks[Math.Clamp(FormsIndex, 0, Looks.Count - 1)];

    /// <summary>The cursor goes to a card, bringing its row into view.</summary>
    private void GoToLook(int index)
    {
        int count = Math.Max(1, Looks.Count);
        FormsIndex = Math.Clamp(index, 0, count - 1);
        int row = FormsIndex / FormsColumns, rows = (count + FormsColumns - 1) / FormsColumns;
        if (row < FormsFirstRow) FormsFirstRow = row;
        if (row >= FormsFirstRow + FormsVisibleRows) FormsFirstRow = row - FormsVisibleRows + 1;
        FormsFirstRow = Math.Clamp(FormsFirstRow, 0, Math.Max(0, rows - FormsVisibleRows));
        FormsEntry = 0;
    }

    private void ResetForms()
    {
        FormsIndex = 0;
        FormsFirstRow = 0;
        FormsEntry = 0;
    }
    public static readonly PokedexSearchButton[] AllButtons = Enum.GetValues<PokedexSearchButton>();

    private readonly List<PokedexEntry> rows = new();
    private readonly Queue<string> diplomasDue = new();
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

    // ---- The cry page (plan 08 · P18)

    /// <summary>How the cry page bends the cry: nothing until a control is changed. Kept while the entry is open.</summary>
    public CryShape Shape { get; private set; }

    /// <summary>Whether the cry is played again as it ends, until the page's rows are left.</summary>
    public bool Loop { get; private set; }

    /// <summary>The row of the cry page under the cursor.</summary>
    public PokedexCryRow CryRow { get; private set; }

    /// <summary>Whether the cry is sounding, and how far through it the screen's own playhead is (0 to 1): advanced by <see cref="Update"/>, never read from the audio thread, so a shot is the same every run.</summary>
    public bool CryPlaying { get; private set; }
    public float CryPlayhead { get; private set; }

    private (int Species, CryShape Shape, SoundSample Sample)? cryMade;

    /// <summary>The cry as the page has it shaped now, for the species selected: made once per species and shape, and kept.</summary>
    public SoundSample? ShapedCry()
    {
        if (Selected is not { } entry) return null;
        if (cryMade is { } kept && kept.Species == entry.Species.DexNumber && kept.Shape == Shape) return kept.Sample;
        var sample = Cries.Shaped(entry.Species, null, Shape);
        cryMade = (entry.Species.DexNumber, Shape, sample);
        return sample;
    }

    /// <summary>The scope's columns for the cry as shaped now and the playhead's place (<see cref="Cries.Scope"/>).</summary>
    public ScopeColumn[] ScopeColumns(int columns) => ShapedCry() is { } cry ? Cries.Scope(cry.Samples, CryPlaying ? CryPlayhead : 0f, columns) : new ScopeColumn[columns];

    /// <summary>The cry sounds, from its start: A on the page's Play and Pitch rows, and again as a loop comes round.</summary>
    public void PlayCry()
    {
        if (Selected is not { } entry || !pokedex.IsSeen(entry.Species.DexNumber)) return;
        AudioManager.PlayCry(entry.Species, null, Shape);
        CryPlaying = true;
        CryPlayhead = 0f;
    }

    private void StopCry()
    {
        CryPlaying = false;
        CryPlayhead = 0f;
    }

    /// <summary>Puts the page's controls back as they were: no pitch, no chorus, no reverb, no loop.</summary>
    private void ResetCry()
    {
        Shape = CryShape.None;
        Loop = false;
        CryRow = PokedexCryRow.Pitch;
        StopCry();
    }

    /// <summary>The Pokédex the list shows; the screen remembers it between openings.</summary>
    public string Mode { get; private set; } = Pokedex.Sinnoh;

    /// <summary>The search the list shows the results of, or null for the whole Pokédex.</summary>
    public PokedexQuery? Results { get; private set; }

    /// <summary>The search being set up in the panel, and which Pokédex it will look in.</summary>
    public PokedexQuery Draft { get; private set; } = new();
    public string DraftMode { get; private set; } = Pokedex.Sinnoh;
    public int SearchIndex { get; private set; }
    public int ButtonIndex { get; private set; }

    /// <summary>The diploma being shown.</summary>
    public string ShownDiploma { get; private set; } = Pokedex.Sinnoh;

    /// <summary>The player's own field sprite, which the size page sets beside the species.</summary>
    public Texture2D? Portrait { get; private set; }

    public Pokedex Pokedex => pokedex;

    /// <summary>The region the player stands in as the Pokédex is opened (plan 18 · W3); the National listing's AREA page draws its map.</summary>
    public Region? Here { get; private set; }

    /// <summary>
    /// The region whose map and wild Pokémon the AREA page shows: the listing's own region, or, for the National
    /// listing, the region the player stands in, as Platinum shows Sinnoh for everything; Sinnoh's when neither is known.
    /// </summary>
    public Region? AreaRegion => Pokedex.RegionOf(Mode) ?? Here ?? RegionDatabase.First;

    /// <summary>
    /// How many times each species' entry has been opened (by its National number) while the game runs: the INFO
    /// page shows its written entries in turn, one more each opening (plan 08 · P13). Not saved.
    /// </summary>
    private readonly Dictionary<int, int> openings = new();

    /// <summary>
    /// Which of a species' written entries the INFO page shows: the first until its entry has been opened, then the
    /// next at each opening, round its entries (<see cref="DexEntries.EntryFor"/>).
    /// </summary>
    public int EntryShown(PokemonSpecies species) => Math.Max(0, openings.GetValueOrDefault(species.DexNumber) - 1);

    /// <summary>How many times a species' entry has been opened.</summary>
    public int Openings(PokemonSpecies species) => openings.GetValueOrDefault(species.DexNumber);

    /// <summary>
    /// How many times any entry has been opened while the game runs (plan 09 · L5): the drawing notes the moment it
    /// changes, from which the INFO page hops once and then breathes. No clock is read here.
    /// </summary>
    public int EntriesOpened { get; private set; }

    /// <summary>Whether the entry is open on its own (not the list beside it): only then does its sprite breathe.</summary>
    public bool LookingAtEntry => Focus == PokedexFocus.Entry;

    private void Opened()
    {
        EntriesOpened++;
        StopCry();
        ResetForms();
        if (Selected is { } entry) openings[entry.Species.DexNumber] = openings.GetValueOrDefault(entry.Species.DexNumber) + 1;
    }
    public IReadOnlyList<PokedexEntry> Rows => rows;
    public IReadOnlyList<PokemonSpecies> Species => rows.Select(r => r.Species).ToList();
    public PokedexEntry? Selected => SelectedIndex >= 0 && SelectedIndex < rows.Count ? rows[SelectedIndex] : null;

    /// <summary>
    /// Opens on the first species the player has seen, so the list doesn't begin with a page of dashes, and shows
    /// any diploma that has become due.
    /// </summary>
    public void Open(Pokedex? pokedex = null, Texture2D? portrait = null, Region? here = null)
    {
        IsActive = true;
        openAge = 0f;
        upDown.Release();
        leftRight.Release();
        this.pokedex = pokedex ?? new Pokedex();
        Portrait = portrait;
        Here = here;
        if (!this.pokedex.Dexes.Contains(Mode)) Mode = here?.Dex is { } own && this.pokedex.Dexes.Contains(own) ? own : this.pokedex.Dexes[0];
        Results = null;
        Focus = PokedexFocus.List;
        Page = PokedexPage.Info;
        ResetCry();
        ResetForms();
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
                        Opened();
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
            case PokedexFocus.Cry:
                CryRow = (PokedexCryRow)UiNav.Wrap((int)CryRow, Math.Sign(step), Enum.GetValues<PokedexCryRow>().Length);
                AudioManager.PlaySound("cursor");
                break;
            case PokedexFocus.Forms:
            {
                // A row of cards at a time, stopping at the first and last row (a last row that is short takes its last card)
                int row = FormsIndex / FormsColumns + Math.Sign(step), rows = (Looks.Count + FormsColumns - 1) / FormsColumns;
                if (row < 0 || row >= rows) break;
                GoToLook(Math.Min(row * FormsColumns + FormsIndex % FormsColumns, Looks.Count - 1));
                AudioManager.PlaySound("cursor");
                break;
            }
        }
    }

    /// <summary>
    /// Left and right on the cry page change the row's value by a step, in its range: the pitch by a semitone, a chorus
    /// or a pan by an eighth, a reverb or a filter by a notch, the loop on and off.
    /// </summary>
    private void ChangeCry(int step)
    {
        var shape = Shape;
        switch (CryRow)
        {
            case PokedexCryRow.Pitch:
                shape = shape with { Pitch = Math.Clamp(shape.Pitch + step, CryShape.LowestPitch, CryShape.HighestPitch) };
                break;
            case PokedexCryRow.Stereo:
                int least = shape.Stereo == CryStereo.Pan ? -CryShape.FurthestPan : 0, most = shape.Stereo == CryStereo.Pan ? CryShape.FurthestPan : CryShape.MostChorus;
                shape = shape with { StereoAmount = Math.Clamp(shape.StereoAmount + step, least, most) };
                break;
            case PokedexCryRow.Room:
                shape = shape with { RoomAmount = Math.Clamp(shape.RoomAmount + step, 0, CryShape.MostRoom) };
                break;
            case PokedexCryRow.Loop:
                Loop = !Loop;
                AudioManager.PlaySound("cursor");
                return;
            default:
                return;
        }
        if (shape == Shape) return;
        Shape = shape;
        AudioManager.PlaySound("cursor");
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
            {
                var pages = PagesOf(Selected);
                Page = pages[UiNav.Wrap(Math.Max(0, pages.ToList().IndexOf(Page)), Math.Sign(step), pages.Count)];
                AudioManager.PlaySound("page");
                break;
            }
            case PokedexFocus.Cry:
                ChangeCry(Math.Sign(step));
                break;
            case PokedexFocus.Forms:
                GoToLook(UiNav.Wrap(FormsIndex, Math.Sign(step), Math.Max(1, Looks.Count)));
                AudioManager.PlaySound("cursor");
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
                    Opened();
                    AudioManager.PlaySound("select");
                    AudioManager.PlayCry(entry.Species, null, CryMode.Pokedex);
                }
                break;
            case PokedexFocus.Entry when Page == PokedexPage.Forms:
                // A moves into the cards (plan 08 · P19)
                Focus = PokedexFocus.Forms;
                GoToLook(0);
                AudioManager.PlaySound("select");
                break;
            case PokedexFocus.Forms:
                // The written entry of the look turns to its next
                FormsEntry++;
                AudioManager.PlaySound("select");
                break;
            case PokedexFocus.Entry when Page == PokedexPage.Cry:
                // A moves into the page's rows (plan 08 · P18)
                Focus = PokedexFocus.Cry;
                CryRow = PokedexCryRow.Pitch;
                AudioManager.PlaySound("select");
                break;
            case PokedexFocus.Cry:
                switch (CryRow)
                {
                    // The switch and the dial change what they bend, as the original's do when touched
                    case PokedexCryRow.Stereo:
                        Shape = Shape with { Stereo = Shape.Stereo == CryStereo.Chorus ? CryStereo.Pan : CryStereo.Chorus, StereoAmount = 0 };
                        AudioManager.PlaySound("select");
                        break;
                    case PokedexCryRow.Room:
                        Shape = Shape with { Room = Shape.Room == CryRoom.Reverb ? CryRoom.Filter : CryRoom.Reverb };
                        AudioManager.PlaySound("select");
                        break;
                    case PokedexCryRow.Loop:
                        Loop = !Loop;
                        AudioManager.PlaySound("select");
                        break;
                    default:
                        PlayCry();
                        break;
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
            case PokedexFocus.Forms:
                Focus = PokedexFocus.Entry;
                break;
            case PokedexFocus.Cry:
                // B from the rows goes back to the pages, and the loop stops
                Focus = PokedexFocus.Entry;
                StopCry();
                break;
            case PokedexFocus.Entry:
                Focus = PokedexFocus.List;
                Page = PokedexPage.Info;
                ResetCry();
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
        Enum.GetValues<PokedexSearchRow>().Where(r => r != PokedexSearchRow.Mode || pokedex.Dexes.Count > 1).ToList();

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
                var modes = pokedex.Dexes;
                DraftMode = modes[UiNav.Wrap(Math.Max(0, modes.ToList().IndexOf(DraftMode)), step, modes.Count)];
                break;
            case PokedexSearchRow.Order:
                var orders = Enum.GetValues<PokedexOrder>();
                Draft = Draft with { Order = orders[UiNav.Wrap(Array.IndexOf(orders, Draft.Order), step, orders.Length)] };
                break;
            case PokedexSearchRow.Name:
                Draft = Draft with { NameGroup = UiNav.Wrap(Draft.NameGroup + 1, step, PokedexSearch.NameGroups.Count + 1) - 1 };
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

    /// <summary>A Pokédex's name as the search panel's row says it ("Sinnoh", "Kanto", "National").</summary>
    public static string ModeName(string dex)
    {
        string key = Pokedex.KeyOf(dex);
        return Text.Of($"pokedex.modeName.{key}");
    }

    /// <summary>What a row of the search panel says now.</summary>
    public string ValueOf(PokedexSearchRow row) => row switch
    {
        PokedexSearchRow.Mode => ModeName(DraftMode),
        PokedexSearchRow.Order => PokedexSearch.NameOf(Draft.Order),
        PokedexSearchRow.Name => PokedexSearch.NameOfGroup(Draft.NameGroup),
        PokedexSearchRow.Type1 => Draft.Type1 is { } first ? Words.Type(first) : Text.Of("pokedex.search.any"),
        PokedexSearchRow.Type2 => Draft.Type2 is { } second ? Words.Type(second) : Text.Of("pokedex.search.any"),
        PokedexSearchRow.Shape => Draft.Shape is { } shape ? PokedexSearch.NameOfShape(shape) : Text.Of("pokedex.search.any"),
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
                ShowDiploma(pokedex.Diplomas.Contains(DraftMode) ? DraftMode : pokedex.Diplomas.Order(StringComparer.Ordinal).First());
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

    public void ShowDiploma(string mode)
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
        AdvanceCry(dt);

        // A key kept down runs through the list (a species at a time, or ten) and through the entries; in the
        // search panel, and for an entry's pages, only a press counts
        bool scrolls = Focus is PokedexFocus.List or PokedexFocus.Entry;
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            scrolls ? InputManager.Axis(GameAction.Up, GameAction.Down, held: true) : 0);
        int dx = leftRight.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right),
            Focus == PokedexFocus.List ? InputManager.Axis(GameAction.Left, GameAction.Right, held: true) : 0);
        for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating);
        for (int i = 0; i < Math.Abs(dx); i++) Sideways(Math.Sign(dx), leftRight.Repeating);
        // The page buttons do what a press of left or right does: ten species down the list, a page on in an entry (plan 12 · Q5)
        int pages = InputManager.Axis(GameAction.PageLeft, GameAction.PageRight);
        if (pages != 0 && dx == 0 && dy == 0) Sideways(pages);

        // A button pressed while the list runs on still counts: it acts on the species the cursor has come to
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Menu))
        {
            if (Focus == PokedexFocus.Search) Search();
            else if (Focus == PokedexFocus.Diploma) Confirm();
            else if (Focus is not (PokedexFocus.Cry or PokedexFocus.Forms)) OpenSearch();
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    /// <summary>
    /// The playhead goes on at the screen's own pace while the cry sounds; at its end the cry is played again if the
    /// loop is on (as <c>crysub.c</c>'s <c>LoopCry</c> does) and the playhead stops otherwise. Leaving the page's rows stops it.
    /// </summary>
    public void AdvanceCry(float dt)
    {
        if (!CryPlaying) return;
        if (Focus != PokedexFocus.Cry && !(Focus == PokedexFocus.Entry && Page == PokedexPage.Cry)) { StopCry(); return; }
        double length = Math.Max(0.05, ShapedCry()?.Duration ?? 0.5);
        CryPlayhead += (float)(dt / length);
        if (CryPlayhead < 1f) return;
        if (Loop && Focus == PokedexFocus.Cry) PlayCry();
        else StopCry();
    }

    public void Draw(int screenWidth, int screenHeight, Pokedex pokedex)
    {
        if (!IsActive) return;
        Follow();
        ModernUi.DrawPokedex(screenWidth, screenHeight, this, pokedex, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
