using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Home screen stands (plan 12 · Q28, Q29).</summary>
public enum HomePhase
{
    /// <summary>TRADE or HOME BOX.</summary>
    Menu,
    /// <summary>A slot chosen from the saves: the first and then the second of a trade, or the one the Home box is opened against.</summary>
    ChooseSlot,
    /// <summary>Two saves side by side, a Pokémon chosen on each.</summary>
    Trade,
    /// <summary>"Trade these two?"</summary>
    ConfirmTrade,
    /// <summary>A save's team and boxes on the left, the Home box on the right.</summary>
    Box,
    /// <summary>The engine is writing the files.</summary>
    Working,
    /// <summary>What came of it, or why it couldn't be.</summary>
    Result
}

/// <summary>What the Home screen asks the engine to do.</summary>
public enum HomeRequestKind { Trade, Deposit, Withdraw }

/// <summary>A trade or a move asked for: the slots and the Pokémon by id (<see cref="HomeMoves"/> carries it out).</summary>
public sealed record HomeRequest(HomeRequestKind Kind, int Slot, Guid Pokemon, int OtherSlot = 0, Guid Other = default);

/// <summary>A slot that can take part: its number and its save as read for the title.</summary>
public sealed record HomeSlot(int Number, SaveData Save);

/// <summary>A Pokémon on a side of the screen, with where it is kept and the Pokémon made of it to draw.</summary>
public sealed class HomeEntry
{
    public HomeEntry(SavedPokemonData saved, bool onTeam, int box, bool randomised)
    {
        Saved = saved;
        OnTeam = onTeam;
        Box = box;
        Randomised = randomised;
    }

    public SavedPokemonData Saved { get; }
    public bool OnTeam { get; }
    /// <summary>Its box, from 0 (unused on the team).</summary>
    public int Box { get; }
    /// <summary>In the Home box: put in from a randomised run (plan 13 · V6), the mark it keeps.</summary>
    public bool Randomised { get; }

    private Pokemon? made;

    /// <summary>The Pokémon made of it, for its icon and name; made once.</summary>
    public Pokemon Made => made ??= Saved.ToPokemon();
}

/// <summary>
/// The title's HOME (plan 12 · Q28, Q29; style guide, "Menu screens", "Home"): trades between two of the player's own
/// saves, and the Home box beside one save's team and boxes. It reads the slots as the title read them and never loads a
/// game; what it asks for goes to the engine as a <see cref="HomeRequest"/> (<see cref="TakeRequest"/>), which
/// <see cref="HomeMoves"/> carries out and <see cref="Finish"/> answers. The rules are asked as a Pokémon is chosen, so a
/// refusal is said at once. The logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>);
/// <see cref="Update"/> reads the keys and <see cref="Draw"/> paints it (<c>ModernUi.Home.cs</c>).
/// </summary>
public sealed class HomeScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many rows each side's list shows.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop - ModernUi.HomeFooter);

    private float openAge;
    private HomeRequest? request;
    private readonly int[] cursor = new int[2], firstRow = new int[2];
    private readonly HomeEntry?[] picked = new HomeEntry?[2];

    public bool IsActive { get; private set; }
    public HomePhase Phase { get; private set; } = HomePhase.Menu;

    /// <summary>The menu's row (0 TRADE, 1 HOME BOX), and the row of the list of slots.</summary>
    public int MenuCursor { get; private set; }
    public int SlotCursor { get; private set; }

    /// <summary>Whether the screen is about a trade or the Home box.</summary>
    public bool Trading { get; private set; }

    /// <summary>The saves that can take part.</summary>
    public IReadOnlyList<HomeSlot> Slots { get; private set; } = Array.Empty<HomeSlot>();

    /// <summary>The slots chosen: the left side's, and the right side's in a trade (null on the Home box's).</summary>
    public HomeSlot? Left { get; private set; }
    public HomeSlot? Right { get; private set; }

    /// <summary>The Home box as read, and why it can't be used where it can't (another data version, damaged).</summary>
    public HomeBank? Bank { get; private set; }
    public HomeRefusal BankRefusal { get; private set; }

    /// <summary>The side the cursor is on (0 left, 1 right), and each side's cursor and first row.</summary>
    public int Side { get; private set; }
    public int CursorOf(int side) => cursor[side];
    public int FirstRowOf(int side) => firstRow[side];

    /// <summary>The Pokémon chosen on each side of a trade.</summary>
    public HomeEntry? PickedOn(int side) => picked[side];

    /// <summary>The trade's question: true while YES is highlighted.</summary>
    public bool ConfirmYes { get; private set; }

    /// <summary>What came of it: the request carried out (None) or why it wasn't, and whether it was a refusal said before asking.</summary>
    public HomeRefusal Outcome { get; private set; }
    public HomeRequestKind? Done { get; private set; }

    /// <summary>The Pokémon on each side, as the files stand.</summary>
    public IReadOnlyList<HomeEntry> EntriesOf(int side) => side == 0 ? leftEntries : rightEntries;

    private List<HomeEntry> leftEntries = new(), rightEntries = new();

    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>Whether a trade can be offered: two saves to trade between.</summary>
    public bool CanTrade => Slots.Count >= 2;

    /// <summary>Opens on the menu, with the saves that can take part and the Home box as read.</summary>
    public void Open(IReadOnlyList<HomeSlot> slots, HomeBank? bank, HomeRefusal bankRefusal)
    {
        Slots = slots;
        Bank = bank;
        BankRefusal = bankRefusal;
        IsActive = true;
        openAge = 0f;
        request = null;
        Left = Right = null;
        MenuCursor = CanTrade ? 0 : 1;
        Enter(HomePhase.Menu);
    }

    public void Close() => IsActive = false;

    /// <summary>The saves and the Home box read afresh after a request, the sides laid out again and the cursors kept where they can be.</summary>
    public void Refresh(IReadOnlyList<HomeSlot> slots, HomeBank? bank, HomeRefusal bankRefusal)
    {
        Slots = slots;
        Bank = bank;
        BankRefusal = bankRefusal;
        if (Left != null) Left = slots.FirstOrDefault(s => s.Number == Left.Number);
        if (Right != null) Right = slots.FirstOrDefault(s => s.Number == Right.Number);
        Lay();
    }

    private void Enter(HomePhase phase)
    {
        Phase = phase;
        ConfirmYes = false;
    }

    // ------------------------------------------------------------------ the sides

    /// <summary>A save's Pokémon as a side shows them: the team, then the boxes.</summary>
    public static List<HomeEntry> EntriesOf(SaveData save)
    {
        var list = save.Party.Select(p => new HomeEntry(p, true, -1, false)).ToList();
        if (save.Boxes != null)
            for (int b = 0; b < save.Boxes.Boxes.Count; b++)
                foreach (var boxed in save.Boxes.Boxes[b].Pokemon.OrderBy(p => p.Slot))
                    list.Add(new HomeEntry(boxed.Pokemon, false, b, false));
        return list;
    }

    /// <summary>The Home box's Pokémon as its side shows them.</summary>
    public static List<HomeEntry> EntriesOf(HomeBank bank) =>
        bank.Ordered.Select(p => new HomeEntry(p.Pokemon, false, p.Box, p.Randomised)).ToList();

    private void Lay()
    {
        leftEntries = Left != null ? EntriesOf(Left.Save) : new();
        rightEntries = Trading ? Right != null ? EntriesOf(Right.Save) : new() : Bank != null ? EntriesOf(Bank) : new();
        for (int side = 0; side < 2; side++)
        {
            int count = EntriesOf(side).Count;
            cursor[side] = Math.Clamp(cursor[side], 0, Math.Max(0, count - 1));
            firstRow[side] = UiNav.Window(Math.Min(firstRow[side], Math.Max(0, count - 1)), cursor[side], count, VisibleRows);
            if (picked[side] != null) picked[side] = EntriesOf(side).FirstOrDefault(e => e.Saved.Id == picked[side]!.Saved.Id);
        }
    }

    /// <summary>The slots the list of slots offers: a trade's second leaves out its first.</summary>
    public IReadOnlyList<HomeSlot> SlotChoices => Trading && Left != null ? Slots.Where(s => s.Number != Left.Number).ToList() : Slots;

    /// <summary>The Pokémon under the cursor on a side, or null.</summary>
    public HomeEntry? Under(int side) => cursor[side] < EntriesOf(side).Count ? EntriesOf(side)[cursor[side]] : null;

    /// <summary>Why the Pokémon under the cursor can't be moved or traded as things stand (None when it can).</summary>
    public HomeRefusal WhyNotHere(int side)
    {
        if (Under(side) is not { } entry) return HomeRefusal.NotFound;
        if (Trading)
        {
            var save = side == 0 ? Left?.Save : Right?.Save;
            return save == null ? HomeRefusal.Unreadable : HomeMoves.WhyNotLeave(save, entry.Saved, trade: true);
        }
        if (Left == null) return HomeRefusal.Unreadable;
        if (Bank == null) return BankRefusal == HomeRefusal.None ? HomeRefusal.HomeDamaged : BankRefusal;
        if (side == 0) return HomeMoves.WhyNotDeposit(Left.Save, entry.Saved, Bank);
        var banked = Bank.Pokemon.FirstOrDefault(p => p.Pokemon.Id == entry.Saved.Id);
        return banked == null ? HomeRefusal.NotFound : HomeMoves.WhyNotWithdraw(Left.Save, banked);
    }

    // ------------------------------------------------------------------ the buttons

    public void Move(int dx, int dy)
    {
        switch (Phase)
        {
            case HomePhase.Menu:
                if (dy != 0) { MenuCursor = (MenuCursor + dy + 2) % 2; AudioManager.PlaySound("cursor"); }
                break;
            case HomePhase.ChooseSlot:
                if (dy != 0 && SlotChoices.Count > 0) { SlotCursor = (SlotCursor + dy + SlotChoices.Count) % SlotChoices.Count; AudioManager.PlaySound("cursor"); }
                break;
            case HomePhase.ConfirmTrade:
                if (dx != 0 || dy != 0) { ConfirmYes = !ConfirmYes; AudioManager.PlaySound("cursor"); }
                break;
            case HomePhase.Trade or HomePhase.Box:
                if (dx != 0)
                {
                    Side = dx > 0 ? 1 : 0;
                    AudioManager.PlaySound("cursor");
                }
                else if (dy != 0)
                {
                    int count = EntriesOf(Side).Count;
                    if (count == 0) return;
                    cursor[Side] = (cursor[Side] + dy + count) % count;
                    firstRow[Side] = UiNav.Window(firstRow[Side], cursor[Side], count, VisibleRows);
                    AudioManager.PlaySound("cursor");
                }
                break;
        }
    }

    public void Confirm()
    {
        switch (Phase)
        {
            case HomePhase.Menu:
                if (MenuCursor == 0 && !CanTrade) { AudioManager.PlaySound("error"); return; }
                AudioManager.PlaySound("select");
                Trading = MenuCursor == 0;
                Left = Right = null;
                SlotCursor = 0;
                // With one save the Home box needs no asking which; with two a trade needs none either
                if (!Trading && Slots.Count == 1) OpenSides(Slots[0], null);
                else if (Trading && Slots.Count == 2) OpenSides(Slots[0], Slots[1]);
                else Enter(HomePhase.ChooseSlot);
                break;
            case HomePhase.ChooseSlot:
                if (SlotChoices.Count == 0) return;
                AudioManager.PlaySound("select");
                var chosen = SlotChoices[SlotCursor];
                if (Trading && Left == null)
                {
                    Left = chosen;
                    SlotCursor = 0;
                    if (SlotChoices.Count == 1) OpenSides(Left, SlotChoices[0]);
                }
                else if (Trading) OpenSides(Left!, chosen);
                else OpenSides(chosen, null);
                break;
            case HomePhase.Trade:
            {
                var why = WhyNotHere(Side);
                if (why != HomeRefusal.None) { Refuse(why); return; }
                AudioManager.PlaySound("select");
                picked[Side] = Under(Side);
                if (picked[0] != null && picked[1] != null)
                {
                    var whole = HomeMoves.WhyNotTrade(Left!.Save, picked[0]!.Saved, Right!.Save, picked[1]!.Saved);
                    if (whole != HomeRefusal.None) { Refuse(whole); return; }
                    Enter(HomePhase.ConfirmTrade);
                }
                else Side = 1 - Side;
                break;
            }
            case HomePhase.ConfirmTrade:
                if (!ConfirmYes)
                {
                    AudioManager.PlaySound("cancel");
                    Enter(HomePhase.Trade);
                    return;
                }
                AudioManager.PlaySound("select");
                request = new HomeRequest(HomeRequestKind.Trade, Left!.Number, picked[0]!.Saved.Id!.Value, Right!.Number, picked[1]!.Saved.Id!.Value);
                Enter(HomePhase.Working);
                break;
            case HomePhase.Box:
            {
                var why = WhyNotHere(Side);
                if (why != HomeRefusal.None) { Refuse(why); return; }
                AudioManager.PlaySound("select");
                request = new HomeRequest(Side == 0 ? HomeRequestKind.Deposit : HomeRequestKind.Withdraw, Left!.Number, Under(Side)!.Saved.Id!.Value);
                Enter(HomePhase.Working);
                break;
            }
            case HomePhase.Result:
                AudioManager.PlaySound("select");
                Enter(Trading ? HomePhase.Trade : HomePhase.Box);
                break;
        }
    }

    public void Cancel()
    {
        switch (Phase)
        {
            case HomePhase.Menu:
                AudioManager.PlaySound("cancel");
                IsActive = false;
                break;
            case HomePhase.ChooseSlot:
                AudioManager.PlaySound("cancel");
                if (Trading && Left != null) { Left = null; SlotCursor = 0; }
                else Enter(HomePhase.Menu);
                break;
            case HomePhase.Trade:
                AudioManager.PlaySound("cancel");
                if (picked[Side] != null) picked[Side] = null;
                else if (picked[1 - Side] != null) picked[1 - Side] = null;
                else BackToMenu();
                break;
            case HomePhase.ConfirmTrade:
                AudioManager.PlaySound("cancel");
                Enter(HomePhase.Trade);
                break;
            case HomePhase.Box:
                AudioManager.PlaySound("cancel");
                BackToMenu();
                break;
            case HomePhase.Result:
                Confirm();
                break;
        }
    }

    private void BackToMenu()
    {
        Left = Right = null;
        picked[0] = picked[1] = null;
        Enter(HomePhase.Menu);
    }

    private void OpenSides(HomeSlot left, HomeSlot? right)
    {
        Left = left;
        Right = right;
        Side = 0;
        cursor[0] = cursor[1] = firstRow[0] = firstRow[1] = 0;
        picked[0] = picked[1] = null;
        Lay();
        Enter(Trading ? HomePhase.Trade : HomePhase.Box);
        // The Home box of another data version is told of at once: nothing can be moved until it is put right
        if (!Trading && Bank == null) Refuse(BankRefusal == HomeRefusal.None ? HomeRefusal.HomeDamaged : BankRefusal);
    }

    private void Refuse(HomeRefusal why)
    {
        AudioManager.PlaySound("error");
        Outcome = why;
        Done = null;
        Enter(HomePhase.Result);
    }

    /// <summary>The request, once: the engine carries it out and answers with <see cref="Finish"/>.</summary>
    public HomeRequest? TakeRequest()
    {
        var taken = request;
        request = null;
        return taken;
    }

    /// <summary>What came of the request: the saves and the Home box as they now stand, and the result to show.</summary>
    public void Finish(HomeRequest asked, HomeRefusal outcome, IReadOnlyList<HomeSlot> slots, HomeBank? bank, HomeRefusal bankRefusal)
    {
        Outcome = outcome;
        Done = outcome == HomeRefusal.None ? asked.Kind : null;
        picked[0] = picked[1] = null;
        Refresh(slots, bank, bankRefusal);
        AudioManager.PlaySound(outcome == HomeRefusal.None ? "select" : "error");
        Enter(HomePhase.Result);
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (Phase == HomePhase.Working) return;
        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawHome(screenWidth, screenHeight, this, Appear);
}
