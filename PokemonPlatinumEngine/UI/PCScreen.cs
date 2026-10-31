using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the cursor is on the storage screen.</summary>
public enum StorageZone { Party, BoxName, Box }

/// <summary>What can be done with the Pokémon under the cursor (the box application's menu, plan 06 · R12).</summary>
public enum PcAction { Move, Withdraw, Store, Summary, Mark, Release, Cancel, Select }

/// <summary>What can be done with the box whose name is under the cursor (SEARCH and SORT are the boxes' tools', plan 12 · Q26).</summary>
public enum PcBoxAction { Name, Wallpaper, Cancel, Search, Sort }

/// <summary>What can be done with a rectangle of a box once it is drawn (plan 12 · Q26).</summary>
public enum PcGroupAction { Move, Release, Cancel }

/// <summary>The rows of the boxes' sort menus (plan 12 · Q26): which boxes, then by what.</summary>
public enum PcSortChoice { ThisBox, AllBoxes, Number, Level, Name, Type, Recent, Cancel }

/// <summary>
/// The Pokémon Storage System (plan 06 · R12): the party beside one of Platinum's eighteen boxes of thirty places
/// (<see cref="PcBoxes"/>). A on a Pokémon opens what can be done with it: pick it up to put down anywhere else
/// (swapping with whatever is there), take it onto the team or put it in the box, mark it, release it; with a
/// Pokémon picked up, A puts it down. A on the box's name renames the box or changes its wallpaper. The rules are
/// <see cref="PcBoxes"/>'s, and this keeps only the screen's state. Its logic takes no input (<see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Choose"/>), so tests and the harness drive it.
/// </summary>
public class PCScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>A box as Platinum has it: six across, five down, eighteen boxes.</summary>
    public const int Columns = PcBoxes.Columns, Rows = PcBoxes.Rows, BoxSize = PcBoxes.BoxSize;

    /// <summary>How many boxes the PC that was opened has: Platinum's eighteen, or the National Pokédex's hundred (plan 03 · D13).</summary>
    public int BoxCount { get; private set; } = PcBoxes.BoxCount;

    // Left and right at the box's name, which a key kept down runs through (a hundred boxes are a long way a press at a time)
    private readonly HeldKey boxKey = new();
    private readonly CursorTick tick = new();

    private float openAge;

    public StorageZone Zone { get; set; }
    public int PartyIndex { get; set; }

    /// <summary>The place of the box the cursor is on, counted row by row.</summary>
    public int Cell { get; set; }
    public int Box { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The Pokémon picked up, carried by the cursor until it is put down, and where it came from.</summary>
    public Pokemon? Held { get; private set; }
    public (StorageZone Zone, int Box, int Index) HeldFrom { get; private set; }

    /// <summary>
    /// What a run says of a Pokémon joining the team from a box (plan 13 · V5, a Monotype): the reason it may not, or null.
    /// Set by the game as it opens the PC; none under a run without the rule, which changes nothing.
    /// </summary>
    public Func<Pokemon, string?>? WhyNotCarry { get; set; }

    /// <summary>How many places the team has under the run (plan 13 · V5): 6, or fewer for a run that carries fewer.</summary>
    public int PartyLimit { get; set; } = Party.MaxSize;

    /// <summary>Whether a Pokémon was stored, moved or released this time (the Journal's line, plan 06 · R12).</summary>
    public bool Changed { get; private set; }

    /// <summary>The menu of what to do with the Pokémon under the cursor, while it is open.</summary>
    public IReadOnlyList<PcAction>? Menu { get; private set; }
    public int MenuIndex { get; set; }

    /// <summary>The menu of what to do with the box, while it is open.</summary>
    public IReadOnlyList<PcBoxAction>? BoxMenu { get; private set; }

    /// <summary>The marks being chosen for the Pokémon under the cursor (MARK), or null.</summary>
    public Markings? Marking { get; private set; }
    public int MarkIndex { get; set; }

    /// <summary>The summary of the Pokémon under the cursor while it is open over the boxes (SUMMARY, plan 08 · P4); its pages and cursors are <see cref="View"/>'s.</summary>
    public bool Summarizing { get; private set; }

    public SummaryView View { get; } = new();

    /// <summary>Whether the summary has a page for the contest condition: the game sets it as it opens the PC, as it does the team's menu.</summary>
    public bool ShowCondition { get => View.HasCondition; set => View.HasCondition = value; }

    private float summaryAge;

    /// <summary>A release waiting for its yes (RELEASE asks first).</summary>
    public bool AskingRelease { get; private set; }

    /// <summary>The box's new name being typed (NAME), or null.</summary>
    public NameEntry? Naming { get; private set; }

    // ------------------------------------------------------------------ the boxes' tools (plan 12 · Q26)

    /// <summary>
    /// Whether the boxes' tools are offered (the options' BOX TOOLS row, set by the game every frame the PC is open):
    /// SEARCH and SORT on the box's menu, SELECT on a Pokémon's. Off, the PC is plan 06 · R12's as it was.
    /// </summary>
    public bool Tools { get; set; }

    /// <summary>Whether a rectangle of the box is being drawn (SELECT), from <see cref="Anchor"/> to the cursor.</summary>
    public bool Selecting { get; private set; }

    /// <summary>The place a rectangle was begun on.</summary>
    public int Anchor { get; private set; }

    /// <summary>The places of the rectangle being drawn, row by row (empty while none is).</summary>
    public IReadOnlyList<int> Rectangle => Selecting ? BoxQuery.Rectangle(Anchor, Cell) : Array.Empty<int>();

    /// <summary>The menu of what to do with the rectangle drawn, while it is open.</summary>
    public IReadOnlyList<PcGroupAction>? GroupMenu { get; private set; }

    private readonly List<(Pokemon Pokemon, BoxPlace From)> carried = new();

    /// <summary>The Pokémon picked up together, in the order they will go down, and where each came from.</summary>
    public IReadOnlyList<(Pokemon Pokemon, BoxPlace From)> Carried => carried;

    /// <summary>The search panel, while it is open.</summary>
    public BoxSearch? Search { get; private set; }

    /// <summary>What the search found, while its results are shown over the panel.</summary>
    public IReadOnlyList<BoxPlace>? Results { get; private set; }

    /// <summary>The results' row under the cursor: 0 is PICK UP ALL, then each found.</summary>
    public int ResultIndex { get; set; }

    /// <summary>The results' rows shown at once, and the first of them.</summary>
    public const int ResultRows = 8;
    public int ResultFirst { get; private set; }

    /// <summary>The sort menu open (its rows), while it is: first which boxes, then by what.</summary>
    public IReadOnlyList<PcSortChoice>? SortMenu { get; private set; }
    private bool sortEvery;

    /// <summary>Releasing the rectangle's Pokémon, which waits for the confirm button held (<see cref="ReleaseHoldSeconds"/>).</summary>
    public bool AskingReleaseMany { get; private set; }

    /// <summary>How far the hold has filled, 0 to 1.</summary>
    public float ReleaseHold { get; private set; }

    /// <summary>How long the confirm button is held to release several at once.</summary>
    public const float ReleaseHoldSeconds = 1f;

    private List<int> releasing = new();

    /// <summary>The Pokémon a release of several would let go, in their places.</summary>
    public IReadOnlyList<int> Releasing => releasing;

    public void Open(PcBoxes? pc = null)
    {
        IsActive = true;
        Zone = StorageZone.Party;
        PartyIndex = 0;
        Cell = 0;
        // The PC opens on the box it was last left on
        BoxCount = pc?.Boxes.Count ?? PcBoxes.BoxCount;
        Box = Math.Clamp(pc?.CurrentBox ?? 0, 0, BoxCount - 1);
        Held = null;
        carried.Clear();
        Changed = false;
        CloseMenus();
        boxKey.Release();
        openAge = 0f;
        // (The PC's own two sounds are heard as its screen comes on and goes off in the field: the script's
        // anim pc on and anim pc off, plan 09 · L12)
    }

    public void Close() => IsActive = false;

    private void CloseMenus()
    {
        Menu = null;
        BoxMenu = null;
        Marking = null;
        AskingRelease = false;
        Naming = null;
        MenuIndex = 0;
        Selecting = false;
        GroupMenu = null;
        Search = null;
        Results = null;
        SortMenu = null;
        AskingReleaseMany = false;
        ReleaseHold = 0f;
    }

    /// <summary>Whether one of the screen's own menus or questions is open over the boxes.</summary>
    public bool Busy => Menu != null || BoxMenu != null || Marking != null || AskingRelease || Naming != null || Summarizing
        || Selecting || GroupMenu != null || Search != null || SortMenu != null || AskingReleaseMany;

    /// <summary>The place in the boxes that the cursor's place stands for, counted from the first box.</summary>
    public int StoredIndex => Box * BoxSize + Cell;

    /// <summary>The Pokémon under the cursor, if there is one.</summary>
    public Pokemon? Under(Party party, PcBoxes pc) => Zone switch
    {
        StorageZone.Party => PartyIndex < party.Count ? party.Members[PartyIndex] : null,
        StorageZone.Box => pc[Box, Cell],
        _ => null
    };

    /// <summary>
    /// One step of the cursor. Up and down the party; sideways from the party into the box and from the box's
    /// edge back to it; up from the box's top row (or down from its last) to its name, where left and right
    /// change box. With a menu open, up and down move through it. At the box's name a step that comes from a key
    /// kept down (<paramref name="held"/>) stops at the first and the last box, where a press goes round.
    /// </summary>
    public void Move(int dx, int dy, int partyCount, bool held = false)
    {
        if (dx == 0 && dy == 0) return;
        if (Naming != null || AskingReleaseMany) return;
        if (MoveTools(dx, dy)) return;
        if (Marking != null)
        {
            MarkIndex = UiNav.Wrap(MarkIndex, dx + dy, 7);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (Menu != null || BoxMenu != null)
        {
            if (dy != 0) MenuIndex = UiNav.Wrap(MenuIndex, dy, (Menu?.Count ?? BoxMenu!.Count));
            AudioManager.PlaySound("cursor");
            return;
        }
        if (AskingRelease) return;

        int boxBefore = Box;
        int col = Cell % Columns, row = Cell / Columns;
        // Someone carried can be put down on the team's empty place after its last member too
        int partyPlaces = Math.Min(PartyLimit, partyCount + (Held != null ? 1 : 0));
        switch (Zone)
        {
            case StorageZone.Party:
                if (dy != 0 && partyPlaces > 0) PartyIndex = UiNav.Wrap(Math.Min(PartyIndex, partyPlaces - 1), dy, partyPlaces);
                else if (dx != 0)
                {
                    // Into the box at the row beside this party card, on the near side
                    Zone = StorageZone.Box;
                    Cell = Math.Min(PartyIndex, Rows - 1) * Columns + (dx > 0 ? 0 : Columns - 1);
                }
                break;
            case StorageZone.BoxName:
                if (dx != 0)
                {
                    int next = held ? Math.Clamp(Box + Math.Sign(dx), 0, BoxCount - 1) : UiNav.Wrap(Box, dx, BoxCount);
                    if (next == Box) return;
                    Box = next;
                }
                else
                {
                    Zone = StorageZone.Box;
                    Cell = (dy > 0 ? 0 : Rows - 1) * Columns + col;
                }
                break;
            default:
                if (dx != 0)
                {
                    col += Math.Sign(dx);
                    if (col is < 0 or >= Columns)
                    {
                        if (partyPlaces == 0) col = UiNav.Wrap(col, 0, Columns);
                        else
                        {
                            Zone = StorageZone.Party;
                            PartyIndex = Math.Min(row, partyPlaces - 1);
                            break;
                        }
                    }
                    Cell = row * Columns + col;
                }
                else
                {
                    row += Math.Sign(dy);
                    if (row is < 0 or >= Rows) Zone = StorageZone.BoxName;
                    else Cell = row * Columns + col;
                }
                break;
        }
        // (a key kept down changes box faster than the sound lasts, so not every one of those sounds)
        if (tick.Sounds(held)) AudioManager.PlaySound(Box != boxBefore ? "page" : "cursor");
    }

    /// <summary>The page buttons (plan 12 · Q5): the previous or the next box, wherever the cursor is, unless a menu or a question is open.</summary>
    public bool ChangeBox(int step, int partyCount)
    {
        if (step == 0 || Busy || BoxCount < 2) return false;
        Box = UiNav.Wrap(Box, Math.Sign(step), BoxCount);
        AudioManager.PlaySound("page");
        return true;
    }

    /// <summary>What the menu offers for the Pokémon under the cursor (with the boxes' tools, SELECT before CANCEL in a box).</summary>
    public IReadOnlyList<PcAction> ActionsFor(StorageZone zone) => zone == StorageZone.Party
        ? new[] { PcAction.Move, PcAction.Store, PcAction.Summary, PcAction.Mark, PcAction.Release, PcAction.Cancel }
        : Tools
            ? new[] { PcAction.Move, PcAction.Withdraw, PcAction.Summary, PcAction.Mark, PcAction.Release, PcAction.Select, PcAction.Cancel }
            : new[] { PcAction.Move, PcAction.Withdraw, PcAction.Summary, PcAction.Mark, PcAction.Release, PcAction.Cancel };

    /// <summary>What the box's menu offers: with the boxes' tools, SEARCH and SORT before CANCEL.</summary>
    public IReadOnlyList<PcBoxAction> BoxActions => Tools
        ? new[] { PcBoxAction.Name, PcBoxAction.Wallpaper, PcBoxAction.Search, PcBoxAction.Sort, PcBoxAction.Cancel }
        : new[] { PcBoxAction.Name, PcBoxAction.Wallpaper, PcBoxAction.Cancel };

    /// <summary>
    /// The A button: with a Pokémon picked up, puts it down where the cursor is; on a Pokémon, opens what can be
    /// done with it; on the box's name, opens what can be done with the box; in a menu, does what is chosen.
    /// </summary>
    public void Confirm(Party party, PcBoxes pc, Action<string> onNotification)
    {
        if (Naming != null) return;
        if (ConfirmTools(party, pc, onNotification)) return;
        if (AskingRelease)
        {
            AnswerRelease(true, party, pc, onNotification);
            return;
        }
        if (Marking is { } marks)
        {
            // The six marks, then OK
            if (MarkIndex < 6) Marking = marks ^ (Markings)(1 << MarkIndex);
            else
            {
                if (Under(party, pc) is { } marked) marked.Marks = (int)marks;
                Marking = null;
            }
            AudioManager.PlaySound("select");
            return;
        }
        if (Menu is { } menu)
        {
            Choose(menu[MenuIndex], party, pc, onNotification);
            return;
        }
        if (BoxMenu is { } boxMenu)
        {
            ChooseForBox(boxMenu[MenuIndex], pc);
            return;
        }
        if (Held != null)
        {
            PutDown(party, pc, onNotification);
            return;
        }
        if (Zone == StorageZone.BoxName)
        {
            BoxMenu = BoxActions;
            MenuIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }
        if (Under(party, pc) == null) return;
        Menu = ActionsFor(Zone);
        MenuIndex = 0;
        AudioManager.PlaySound("select");
    }

    /// <summary>Goes to the next or the previous Pokémon of the team, or of the box on the screen (the summary's up and down).</summary>
    public void StepSummary(Party party, PcBoxes pc, int dy)
    {
        if (!Summarizing || dy == 0) return;
        int step = Math.Sign(dy);
        if (Zone == StorageZone.Party)
        {
            if (party.Count == 0) return;
            PartyIndex = UiNav.Wrap(Math.Min(PartyIndex, party.Count - 1), step, party.Count);
        }
        else
        {
            for (int i = 1; i <= BoxSize; i++)
            {
                int cell = UiNav.Wrap(Cell, step * i, BoxSize);
                if (pc[Box, cell] == null) continue;
                Cell = cell;
                break;
            }
        }
        View.PutCursorsAway();
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The place of the Pokémon shown among those that can be stepped through, and how many there are.</summary>
    public (int Index, int Count) SummaryPlace(Party party, PcBoxes pc)
    {
        if (Zone == StorageZone.Party) return (PartyIndex, party.Count);
        int index = 0, count = 0;
        for (int cell = 0; cell < BoxSize; cell++)
        {
            if (pc[Box, cell] == null) continue;
            if (cell == Cell) index = count;
            count++;
        }
        return (index, count);
    }

    /// <summary>The B button: closes what is open, or puts a Pokémon carried back where it came from; false when there was nothing to back out of.</summary>
    public bool Cancel(Party party, PcBoxes pc)
    {
        if (CancelTools(pc)) { AudioManager.PlaySound("cancel"); return true; }
        if (Summarizing) Summarizing = false;
        else if (Naming != null) Naming = null;
        else if (AskingRelease) AskingRelease = false;
        else if (Marking != null) Marking = null;
        else if (Menu != null || BoxMenu != null) { Menu = null; BoxMenu = null; }
        else if (Held != null) PutBack(party, pc);
        else return false;
        AudioManager.PlaySound("cancel");
        return true;
    }

    /// <summary>Does what was chosen in the Pokémon's menu.</summary>
    public void Choose(PcAction action, Party party, PcBoxes pc, Action<string> onNotification)
    {
        Menu = null;
        var pokemon = Under(party, pc);
        if (pokemon == null) return;
        switch (action)
        {
            case PcAction.Move:
                // Taken off the team, it must be one the team can spare
                if (Zone == StorageZone.Party && pc.WhyNotDeposit(party, PartyIndex, intoABox: false) is { } stays)
                {
                    onNotification(stays);
                    return;
                }
                PickUp(party, pc);
                break;
            case PcAction.Withdraw:
                if (party.IsFull)
                {
                    onNotification(Text.Of("boxes.partyFull"));
                    return;
                }
                // A run that keeps one type on the team (plan 13 · V5)
                if (WhyNotCarry?.Invoke(pokemon) is { } notThis)
                {
                    onNotification(notThis);
                    return;
                }
                pc.Withdraw(party, Box, Cell);
                AudioManager.PlaySound("select");
                onNotification(Text.Of("boxes.joined", ("name", pokemon.DisplayName)));
                break;
            case PcAction.Store:
            {
                if (pc.WhyNotDeposit(party, PartyIndex) is { } why)
                {
                    onNotification(why);
                    return;
                }
                // Into the box on the screen, or the next with room
                int box = Box;
                for (int i = 0; i < BoxCount && pc.Boxes[box].Count >= BoxSize; i++) box = (box + 1) % BoxCount;
                int slot = Array.IndexOf(pc.Boxes[box].Slots, null);
                pc.Deposit(party, PartyIndex, box, slot);
                Changed = true;
                Box = box;
                PartyIndex = Math.Max(0, Math.Min(PartyIndex, party.Count - 1));
                AudioManager.PlaySound("select");
                onNotification(Text.Of("boxes.putIn", ("name", pokemon.DisplayName), ("box", pc.ShownName(box))));
                break;
            }
            case PcAction.Summary:
                Summarizing = true;
                View.Open();
                summaryAge = 0f;
                AudioManager.PlaySound("select");
                if (!pokemon.IsEgg) AudioManager.PlayCry(pokemon);
                break;
            case PcAction.Mark:
                Marking = (Markings)pokemon.Marks;
                MarkIndex = 0;
                break;
            case PcAction.Release:
                AskingRelease = true;
                break;
            case PcAction.Select:
                // A rectangle begun here: the cursor is its other corner
                if (Zone != StorageZone.Box) return;
                Selecting = true;
                Anchor = Cell;
                AudioManager.PlaySound("select");
                break;
        }
    }

    /// <summary>The answer to "Release it?": yes lets it go if <see cref="PcBoxes.WhetherToRelease"/> allows.</summary>
    public void AnswerRelease(bool yes, Party party, PcBoxes pc, Action<string> onNotification)
    {
        AskingRelease = false;
        if (!yes || Under(party, pc) is not { } pokemon) return;
        switch (pc.Release(pokemon, party))
        {
            case ReleaseOutcome.Released:
                Changed = true;
                PartyIndex = Math.Max(0, Math.Min(PartyIndex, party.Count - 1));
                onNotification(Text.Of("boxes.released", ("name", pokemon.DisplayName)));
                break;
            case ReleaseOutcome.CameBack:
                onNotification(Text.Of("boxes.cameBack", ("name", pokemon.DisplayName)));
                break;
            case ReleaseOutcome.HoldsMail:
                onNotification(Text.Of("boxes.removeMail"));
                break;
            default:
                onNotification(Text.Of("boxes.lastAble"));
                break;
        }
    }

    /// <summary>Does what was chosen in the box's menu: NAME starts typing its name, WALLPAPER puts up the next one it may have.</summary>
    public void ChooseForBox(PcBoxAction action, PcBoxes pc)
    {
        BoxMenu = null;
        if (action == PcBoxAction.Name) Naming = new NameEntry(NameToEdit(pc), PcBoxes.NameLength);
        else if (action == PcBoxAction.Wallpaper) NextWallpaper(pc);
        else if (action == PcBoxAction.Search) OpenSearch(pc);
        else if (action == PcBoxAction.Sort)
        {
            SortMenu = new[] { PcSortChoice.ThisBox, PcSortChoice.AllBoxes, PcSortChoice.Cancel };
            MenuIndex = 0;
            AudioManager.PlaySound("select");
        }
    }

    /// <summary>Puts up the next wallpaper the PC has (the sixteen, and the eight once unlocked).</summary>
    public void NextWallpaper(PcBoxes pc)
    {
        var box = pc.Boxes[Box];
        int next = box.Wallpaper;
        do next = (next + 1) % PcBoxes.WallpaperNames.Count;
        while (!pc.HasWallpaper(next));
        box.Wallpaper = next;
        AudioManager.PlaySound("select");
    }

    /// <summary>
    /// The name the keyboard starts from: the box's name as shown ("Cutia 3"), or the name it is saved under where the
    /// language's is longer than a box's name may be ("Cutia 100").
    /// </summary>
    private string NameToEdit(PcBoxes pc) =>
        pc.ShownName(Box) is { Length: <= PcBoxes.NameLength } shown ? shown : pc.Boxes[Box].Name;

    /// <summary>Ends typing the box's name: given, it is the box's name from now on.</summary>
    public void FinishNaming(PcBoxes pc, string? name)
    {
        if (name != null) pc.Rename(Box, name);
        Naming = null;
    }

    private void PickUp(Party party, PcBoxes pc)
    {
        if (Zone == StorageZone.Party)
        {
            Held = party.Members[PartyIndex];
            HeldFrom = (StorageZone.Party, 0, PartyIndex);
            party.RemoveAt(PartyIndex);
        }
        else
        {
            Held = pc.TakeOut(Box, Cell);
            HeldFrom = (StorageZone.Box, Box, Cell);
        }
        AudioManager.PlaySound("select");
    }

    /// <summary>Puts the Pokémon carried down where the cursor is: into an empty place, or swapped with whoever is there.</summary>
    private void PutDown(Party party, PcBoxes pc, Action<string> onNotification)
    {
        var held = Held!;
        Changed = true;
        if (Zone == StorageZone.BoxName) return;
        if (Zone == StorageZone.Party)
        {
            // From a box onto the team: the run's Monotype and the team's size apply (plan 13 · V5); the Pokémon stays in hand
            if (HeldFrom.Zone == StorageZone.Box && WhyNotCarry?.Invoke(held) is { } notThis)
            {
                onNotification(notThis);
                return;
            }
            if (PartyIndex >= party.Count && party.IsFull)
            {
                onNotification(Text.Of("boxes.partyFull"));
                return;
            }
            if (PartyIndex < party.Count)
            {
                // Swapped with the member there, who is carried now
                var there = party.Members[PartyIndex];
                party.RemoveAt(PartyIndex);
                party.Insert(PartyIndex, FromTheBox(held));
                Held = there;
                HeldFrom = (StorageZone.Party, 0, PartyIndex);
            }
            else
            {
                party.Add(FromTheBox(held));
                Held = null;
            }
        }
        else
        {
            if (pc[Box, Cell] is { } there)
            {
                pc.TakeOut(Box, Cell);
                pc.PutIn(Box, Cell, held);
                Held = there;
                HeldFrom = (StorageZone.Box, Box, Cell);
            }
            else
            {
                pc.PutIn(Box, Cell, held);
                Held = null;
            }
        }
        AudioManager.PlaySound("select");
    }

    /// <summary>A Pokémon put on the team from the boxes comes out of them as withdrawing does.</summary>
    private Pokemon FromTheBox(Pokemon pokemon)
    {
        if (HeldFrom.Zone == StorageZone.Box) PcBoxes.AsWithdrawn(pokemon);
        return pokemon;
    }

    /// <summary>Puts a Pokémon carried back where it was picked up (or, if that place has been filled, anywhere with room).</summary>
    private void PutBack(Party party, PcBoxes pc)
    {
        var held = Held!;
        if (HeldFrom.Zone == StorageZone.Party && !party.IsFull) party.Insert(Math.Min(HeldFrom.Index, party.Count), held);
        else if (HeldFrom.Zone == StorageZone.Box && pc[HeldFrom.Box, HeldFrom.Index] == null) pc.PutIn(HeldFrom.Box, HeldFrom.Index, held);
        else if (!party.IsFull) party.Add(held);
        else pc.Store(held);
        Held = null;
    }

    public void Update(Party party, PcBoxes pc, Action<string> onNotification, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;

        if (Naming is { } naming)
        {
            // The keyboard's own keys while a box is being named
            int nx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
            int ny = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
            if (nx != 0 || ny != 0) naming.Move(nx, ny);
            else if (InputManager.IsActionPressed(GameAction.Cancel)) { if (!naming.Backspace()) FinishNaming(pc, null); }
            else if (InputManager.IsActionPressed(GameAction.Menu)) naming.ToDone();
            else if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                naming.Press();
                if (naming.Done) FinishNaming(pc, naming.Result(NameToEdit(pc)));
            }
            return;
        }

        // The summary open over the boxes has the same keys as the team's (plan 08 · P4)
        if (Summarizing)
        {
            summaryAge += dt;
            int sx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
            int sy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
            if (sx == 0) sx = InputManager.Axis(GameAction.PageLeft, GameAction.PageRight);
            var pressed = View.Press(Under(party, pc), sx, sy);
            if (pressed.Close) Summarizing = false;
            else if (pressed.Step != 0) StepSummary(party, pc, pressed.Step);
            return;
        }

        // Releasing several waits for the confirm button held (plan 12 · Q26); B keeps them
        if (AskingReleaseMany)
        {
            if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel(party, pc);
            else HoldRelease(dt, InputManager.IsActionDown(GameAction.Confirm), party, pc, onNotification);
            return;
        }

        // A key kept down at the box's name runs on through the boxes; anywhere else only a press counts
        tick.Update(dt);
        bool runs = Zone == StorageZone.BoxName && !Busy;
        int dx = boxKey.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), runs ? InputManager.Axis(GameAction.Left, GameAction.Right, held: true) : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        int pages = InputManager.Axis(GameAction.PageLeft, GameAction.PageRight);
        if (pages != 0 && dx == 0 && dy == 0 && ChangeBox(pages, party.Count)) return;
        if (boxKey.Repeating && dx != 0)
            for (int i = 0; i < Math.Abs(dx); i++) Move(Math.Sign(dx), 0, party.Count, held: true);
        else if (dx != 0 || dy != 0) Move(dx, dy, party.Count);
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            if (!Cancel(party, pc))
            {
                // The PC remembers the box it was left on
                pc.CurrentBox = Box;
                Close();
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(party, pc, onNotification);
    }

    // ------------------------------------------------------------------ the boxes' tools (plan 12 · Q26)

    /// <summary>The cursor's step where a tool has it: the search's rows, the results, the sort's and the rectangle's menus, the rectangle's corner. True when a tool took it.</summary>
    private bool MoveTools(int dx, int dy)
    {
        if (Results is { } results)
        {
            if (dy == 0) return true;
            ResultIndex = UiNav.Wrap(ResultIndex, Math.Sign(dy), results.Count + 1);
            ResultFirst = UiNav.Window(ResultFirst, ResultIndex, results.Count + 1, ResultRows);
            AudioManager.PlaySound("cursor");
            return true;
        }
        if (Search is { } search)
        {
            if (dy != 0) search.Move(dy);
            else if (!search.Change(dx)) return true;
            AudioManager.PlaySound("cursor");
            return true;
        }
        if (SortMenu is { } sort)
        {
            if (dy != 0) { MenuIndex = UiNav.Wrap(MenuIndex, dy, sort.Count); AudioManager.PlaySound("cursor"); }
            return true;
        }
        if (GroupMenu is { } group)
        {
            if (dy != 0) { MenuIndex = UiNav.Wrap(MenuIndex, dy, group.Count); AudioManager.PlaySound("cursor"); }
            return true;
        }
        if (Selecting)
        {
            // The rectangle's corner stays in the box
            int col = Math.Clamp(Cell % Columns + Math.Sign(dx), 0, Columns - 1), row = Math.Clamp(Cell / Columns + Math.Sign(dy), 0, Rows - 1);
            if (row * Columns + col != Cell) { Cell = row * Columns + col; AudioManager.PlaySound("cursor"); }
            return true;
        }
        return false;
    }

    /// <summary>The A button where a tool has it; true when a tool took it.</summary>
    private bool ConfirmTools(Party party, PcBoxes pc, Action<string> onNotification)
    {
        if (AskingReleaseMany) return true;
        if (Results is { } results)
        {
            if (ResultIndex == 0) PickUpMany(pc, results);
            else
            {
                var place = results[ResultIndex - 1];
                Box = place.Box;
                Cell = place.Slot;
                Zone = StorageZone.Box;
                AudioManager.PlaySound("select");
            }
            Search = null;
            Results = null;
            return true;
        }
        if (Search is { } search)
        {
            if (BoxSearch.Rows[search.Row] != BoxSearchRow.Go) return true;
            var found = BoxQuery.Find(pc, search.Filter);
            if (found.Count == 0)
            {
                AudioManager.PlaySound("error");
                onNotification(Text.Of("boxTools.search.none"));
                return true;
            }
            Results = found;
            ResultIndex = 0;
            ResultFirst = 0;
            AudioManager.PlaySound("select");
            return true;
        }
        if (SortMenu is { } sort)
        {
            var choice = sort[MenuIndex];
            if (choice == PcSortChoice.Cancel) SortMenu = null;
            else if (choice is PcSortChoice.ThisBox or PcSortChoice.AllBoxes)
            {
                sortEvery = choice == PcSortChoice.AllBoxes;
                SortMenu = new[] { PcSortChoice.Number, PcSortChoice.Level, PcSortChoice.Name, PcSortChoice.Type, PcSortChoice.Recent, PcSortChoice.Cancel };
                MenuIndex = 0;
            }
            else
            {
                var by = choice switch
                {
                    PcSortChoice.Level => BoxSort.Level,
                    PcSortChoice.Name => BoxSort.Name,
                    PcSortChoice.Type => BoxSort.Type,
                    PcSortChoice.Recent => BoxSort.Recent,
                    _ => BoxSort.Number
                };
                if (sortEvery) BoxQuery.SortAll(pc, by);
                else BoxQuery.Sort(pc, Box, by);
                SortMenu = null;
                Changed = true;
                onNotification(Text.Of(sortEvery ? "boxTools.sort.doneAll" : "boxTools.sort.done", ("box", pc.ShownName(Box))));
            }
            AudioManager.PlaySound("select");
            return true;
        }
        if (GroupMenu is { } group)
        {
            var action = group[MenuIndex];
            GroupMenu = null;
            var cells = BoxQuery.Rectangle(Anchor, Cell).Where(c => pc[Box, c] != null).ToList();
            Selecting = false;
            if (action == PcGroupAction.Move) PickUpMany(pc, cells.Select(c => new BoxPlace(Box, c)).ToList());
            else if (action == PcGroupAction.Release)
            {
                releasing = cells;
                AskingReleaseMany = true;
                ReleaseHold = 0f;
            }
            AudioManager.PlaySound("select");
            return true;
        }
        if (Selecting)
        {
            if (!BoxQuery.Rectangle(Anchor, Cell).Any(c => pc[Box, c] != null)) return true;
            GroupMenu = new[] { PcGroupAction.Move, PcGroupAction.Release, PcGroupAction.Cancel };
            MenuIndex = 0;
            AudioManager.PlaySound("select");
            return true;
        }
        if (carried.Count > 0)
        {
            PutDownMany(pc, onNotification);
            return true;
        }
        return false;
    }

    /// <summary>The B button where a tool has it: each steps back once; true when a tool took it.</summary>
    private bool CancelTools(PcBoxes pc)
    {
        if (AskingReleaseMany) { AskingReleaseMany = false; ReleaseHold = 0f; releasing = new(); }
        else if (Results != null) Results = null;
        else if (Search != null) Search = null;
        else if (SortMenu != null) SortMenu = null;
        else if (GroupMenu != null) GroupMenu = null;
        else if (Selecting) Selecting = false;
        else if (carried.Count > 0) PutBackMany(pc);
        else return false;
        return true;
    }

    /// <summary>Opens the search panel on what the boxes hold now.</summary>
    public void OpenSearch(PcBoxes pc)
    {
        Search = new BoxSearch(pc);
        Results = null;
        AudioManager.PlaySound("select");
    }

    /// <summary>Picks up every Pokémon of the places given, together, in their order (a rectangle's, a search's).</summary>
    public void PickUpMany(PcBoxes pc, IReadOnlyList<BoxPlace> places)
    {
        foreach (var place in places)
            if (pc.TakeOut(place.Box, place.Slot) is { } p) carried.Add((p, place));
        if (carried.Count > 0) AudioManager.PlaySound("select");
    }

    /// <summary>
    /// Puts every Pokémon carried down in the box on the screen, into its empty places from the cursor's on and round to
    /// its first, in their order; refused whole where the box hasn't room for all of them, or off the box.
    /// </summary>
    public bool PutDownMany(PcBoxes pc, Action<string> onNotification)
    {
        if (Zone != StorageZone.Box)
        {
            AudioManager.PlaySound("error");
            if (Zone == StorageZone.Party) onNotification(Text.Of("boxTools.many.notTheTeam"));
            return false;
        }
        if (BoxQuery.PlacesFor(pc, Box, Cell, carried.Count) is not { } places)
        {
            AudioManager.PlaySound("error");
            onNotification(Text.Of("boxTools.many.noRoom", ("count", carried.Count), ("box", pc.ShownName(Box))));
            return false;
        }
        for (int i = 0; i < places.Count; i++) pc.Lay(Box, places[i], carried[i].Pokemon);
        carried.Clear();
        Changed = true;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>Puts each Pokémon carried back where it came from (or, if that place has been filled, the first place with room).</summary>
    private void PutBackMany(PcBoxes pc)
    {
        foreach (var (p, from) in carried)
        {
            if (pc[from.Box, from.Slot] == null) pc.Lay(from.Box, from.Slot, p);
            else pc.Store(p);
        }
        carried.Clear();
    }

    /// <summary>
    /// A frame of the hold that releases several (<see cref="ReleaseHoldSeconds"/>): held, it fills, and full it lets them go;
    /// let go, it empties. A press alone releases nothing.
    /// </summary>
    public void HoldRelease(float dt, bool down, Party party, PcBoxes pc, Action<string> onNotification)
    {
        if (!AskingReleaseMany) return;
        ReleaseHold = down ? Math.Min(1f, ReleaseHold + dt / ReleaseHoldSeconds) : Math.Max(0f, ReleaseHold - dt / ReleaseHoldSeconds * 2f);
        if (ReleaseHold < 1f) return;
        int released = 0, stayed = 0;
        foreach (int cell in releasing)
        {
            if (pc[Box, cell] is not { } p) continue;
            if (pc.Release(p, party) == ReleaseOutcome.Released) released++;
            else stayed++;
        }
        AskingReleaseMany = false;
        ReleaseHold = 0f;
        releasing = new();
        if (released > 0) Changed = true;
        AudioManager.PlaySound("select");
        onNotification(stayed == 0
            ? Text.Of("boxTools.many.released", ("count", released))
            : Text.Of("boxTools.many.releasedSome", ("count", released), ("stayed", stayed)));
    }

    public void Draw(int screenWidth, int screenHeight, Party party, PcBoxes pc)
    {
        if (!IsActive) return;
        if (Summarizing && Under(party, pc) is { } shown)
        {
            var (index, count) = SummaryPlace(party, pc);
            ModernUi.DrawSummary(screenWidth, screenHeight, shown, index, count, Math.Clamp(summaryAge / 0.25f, 0f, 1f), (int)View.Page,
                View.RibbonCursor, View.MarkCursor, View.MoveSelection);
            return;
        }
        ModernUi.DrawStorage(screenWidth, screenHeight, this, party, pc, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
