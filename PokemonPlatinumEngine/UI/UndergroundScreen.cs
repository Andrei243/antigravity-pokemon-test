using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Underground's screen is open as.</summary>
public enum UndergroundMode
{
    /// <summary>The bag below ground: traps, Spheres, goods, treasures, and the way up.</summary>
    Bag,
    /// <summary>A hiker's trade.</summary>
    Hiker,
    /// <summary>The base's PC.</summary>
    Pc
}

/// <summary>What the game is to do once the screen has closed: something that happens in the tunnels themselves.</summary>
public enum UndergroundRequestKind
{
    None,
    SetTrap,
    BurySphere,
    UseDrill,
    GoUp,
    Decorate
}

public readonly record struct UndergroundRequest(UndergroundRequestKind Kind, int Slot = -1);

/// <summary>A row of one of the screen's lists: a name, a word at its right, what the panel beside says of it, and its picture.</summary>
public sealed record UndergroundRow(string Name, string Value, string Detail, string Icon, bool Enabled = true);

/// <summary>
/// The Underground's lists (plan 06 · R16a; the original's <c>src/underground/menus.c</c>, <c>vendors.c</c> and
/// <c>pc.c</c>): the bag below ground, a hiker's trade and the base's PC, one screen in three guises. Each is tabs
/// over a list; a row's button opens what can be done with it. Its logic is <see cref="Move"/>, <see cref="Confirm"/>
/// and <see cref="Cancel"/>, which take no input. What happens in the tunnels themselves (a trap set, a Sphere
/// buried, the drill, the way up, the decorating) is handed back as a <see cref="UndergroundRequest"/> as it closes.
/// </summary>
public sealed class UndergroundScreen
{
    public const int VisibleRows = 8;

    public bool IsActive { get; private set; }
    public UndergroundMode Mode { get; private set; }
    public int Vendor { get; private set; } = -1;
    public IReadOnlyList<string> Tabs { get; private set; } = Array.Empty<string>();
    public int Tab { get; private set; }
    public int Cursor { get; private set; }
    public int First { get; private set; }
    public IReadOnlyList<UndergroundRow> Rows { get; private set; } = Array.Empty<UndergroundRow>();

    /// <summary>The little menu open on a row, if any, and its cursor.</summary>
    public IReadOnlyList<string>? Actions { get; private set; }
    public int ActionCursor { get; private set; }

    /// <summary>Choosing a Sphere to pay a hiker with: the cursor in the bag's Spheres.</summary>
    public bool Paying { get; private set; }
    public int PayCursor { get; private set; }

    /// <summary>A question with yes and no, if one is up, and where its cursor is (0 is yes).</summary>
    public string? Question { get; private set; }
    public int Answer { get; private set; }

    /// <summary>The last thing said: a hiker's word, what was stored. The next button clears it.</summary>
    public string? Message { get; private set; }

    public float Age { get; private set; }
    public UndergroundState State { get; private set; } = new();
    public bool National { get; private set; }

    private UndergroundRequest request;
    private Func<string, bool> putInBag = _ => false;
    private List<VendorOffer> stock = new();
    private Action? onYes;

    /// <summary>What the game is to do now that the screen has closed; asked once.</summary>
    public UndergroundRequest TakeRequest()
    {
        var asked = request;
        request = default;
        return asked;
    }

    /// <param name="putInBag">Puts an item of the bag above ground into it (a treasure taken out of the Underground's own bag); false when it can't.</param>
    public void OpenBag(UndergroundState state, Func<string, bool> putInBag, int tab = 0)
    {
        Begin(state, UndergroundMode.Bag, new[] { "TRAPS", "SPHERES", "GOODS", "TREASURES", "SURFACE" }, tab);
        this.putInBag = putInBag;
    }

    public void OpenHiker(UndergroundState state, int vendor, bool national)
    {
        Vendor = vendor;
        National = national;
        stock = UndergroundVendors.Stock(state.Seed, vendor, national);
        string kind = UndergroundVendors.KindOf(vendor);
        Begin(state, UndergroundMode.Hiker, kind == "Treasures" ? new[] { "YOUR TREASURES" } : new[] { "HIS WARES", kind == "Traps" ? "YOUR TRAPS" : "YOUR GOODS" }, 0);
    }

    public void OpenPc(UndergroundState state) => Begin(state, UndergroundMode.Pc, new[] { "DECORATE", "STORE", "TAKE", "FLAGS" }, 0);

    private void Begin(UndergroundState state, UndergroundMode mode, string[] tabs, int tab)
    {
        State = state;
        Mode = mode;
        Tabs = tabs;
        Tab = Math.Clamp(tab, 0, tabs.Length - 1);
        Cursor = First = 0;
        Actions = null;
        Paying = false;
        Question = Message = null;
        request = default;
        Age = 0f;
        IsActive = true;
        Rebuild();
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    private static string SphereWord(SphereKind kind, int size) => $"{kind} {size}";

    /// <summary>The list under the tab, as things stand.</summary>
    private void Rebuild()
    {
        var rows = new List<UndergroundRow>();
        string tab = Tabs[Tab];
        switch (tab)
        {
            case "TRAPS" or "YOUR TRAPS":
                foreach (int trap in State.Traps)
                    rows.Add(new UndergroundRow(UndergroundDatabase.TrapName(trap), Worth(trap), TrapBlurb(trap), "trap"));
                break;
            case "SPHERES":
                foreach (var sphere in State.Spheres)
                    rows.Add(new UndergroundRow(UndergroundDatabase.SphereName(sphere.Kind), sphere.Size.ToString(),
                        "Buried in the floor of a tunnel, a Sphere grows a little every day. The hikers take them in trade.", $"sphere:{sphere.Kind}"));
                break;
            case "GOODS" or "YOUR GOODS" or "STORE":
                foreach (int good in State.Goods)
                    rows.Add(new UndergroundRow(UndergroundDatabase.GoodName(good), Worth(good), GoodBlurb(good), "good"));
                break;
            case "TREASURES" or "YOUR TREASURES":
                foreach (int treasure in State.Treasures)
                    rows.Add(new UndergroundRow(UndergroundDatabase.TreasureName(treasure), Worth(treasure),
                        "Dug out of a wall. Put in the bag, it is an item like any other above ground.", $"treasure:{treasure}"));
                break;
            case "SURFACE":
                rows.Add(new UndergroundRow("GO UP", "", "Back to the surface, where you went down.", "up"));
                rows.Add(new UndergroundRow("Spheres dug up", State.Records.SpheresDug.ToString(), "", "sphere:Prism", false));
                rows.Add(new UndergroundRow("Fossils dug up", State.Records.FossilsDug.ToString(), "", "treasure:0", false));
                rows.Add(new UndergroundRow("Treasures dug up", State.Records.TreasuresDug.ToString(), "", "treasure:0", false));
                rows.Add(new UndergroundRow("Traps stepped on", State.Records.TrapsTriggered.ToString(), "", "trap", false));
                rows.Add(new UndergroundRow("Flags brought home", State.Records.FlagsCaptured.ToString(), "", "flag", false));
                rows.Add(new UndergroundRow("Times the base moved", State.Records.TimesBaseMoved.ToString(), "", "good", false));
                break;
            case "HIS WARES":
                foreach (var offer in stock)
                {
                    bool traps = UndergroundVendors.KindOf(Vendor) == "Traps";
                    rows.Add(new UndergroundRow(traps ? UndergroundDatabase.TrapName(offer.Thing) : UndergroundDatabase.GoodName(offer.Thing),
                        SphereWord(offer.Kind, offer.Size), traps ? TrapBlurb(offer.Thing) : GoodBlurb(offer.Thing), traps ? "trap" : "good"));
                }
                break;
            case "DECORATE":
                rows.Add(new UndergroundRow("DECORATE THE BASE", "", "Put goods out of the PC about the base, put them away again, or move them.", "good"));
                break;
            case "TAKE":
                for (int slot = 0; slot < State.PcGoods.Count; slot++)
                    rows.Add(new UndergroundRow(UndergroundDatabase.GoodName(State.PcGoods[slot]), State.IsDisplayed(slot) ? "OUT" : "", GoodBlurb(State.PcGoods[slot]), "good"));
                break;
            case "FLAGS":
            {
                int flags = State.Records.FlagsCaptured;
                rows.Add(new UndergroundRow("This base's flag", SecretBaseRules.FlagFor(flags).ToString(), "", "flag", false));
                rows.Add(new UndergroundRow("Flags brought home", flags.ToString(), "", "flag", false));
                foreach (string owner in State.FlagOwners) rows.Add(new UndergroundRow($"{owner}'s flag", "", "", "flag", false));
                break;
            }
        }
        Rows = rows;
        Cursor = Math.Clamp(Cursor, 0, Math.Max(0, rows.Count - 1));
        First = UiNav.Window(First, Cursor, rows.Count, VisibleRows);
    }

    /// <summary>What the hiker at hand gives for a thing, as a word; nothing where no hiker is asked.</summary>
    private string Worth(int thing)
    {
        if (Mode != UndergroundMode.Hiker) return "";
        var worth = UndergroundVendors.Worth(State.Seed, Vendor, thing);
        return worth == null || worth.Size < 1 ? "-" : SphereWord(worth.Kind, worth.Size);
    }

    /// <summary>What a trap does, in our own words, by its family.</summary>
    public static string TrapBlurb(int trap) => UndergroundDatabase.Trap(trap)?.Id == "TRAP_DIGGER_DRILL"
        ? "Used facing a wall of the tunnels, it opens a secret base there. One drill, one base: a second base moves the first."
        : TrapEffect.FamilyOf(trap) switch
        {
            TrapFamily.Slide => "Whoever steps on it is sent sliding the way its arrow points, until something stops them.",
            TrapFamily.Hole => "Whoever steps on it falls in, and has to wriggle this way and that to climb out.",
            TrapFamily.Steering => "Whoever steps on it finds their steps going the wrong way for a while.",
            TrapFamily.Smoke => "Whoever steps on it is wrapped in smoke until they have wiped it away.",
            TrapFamily.Rock => "A boulder drops in front of whoever steps on it, and has to be broken up.",
            TrapFamily.Bubbles => "Whoever steps on it is buried in bubbles until every one is burst.",
            TrapFamily.Alert => "It calls out when someone steps on it. Nothing more.",
            TrapFamily.Leaves => "Whoever steps on it is covered in leaves until they are blown away.",
            TrapFamily.Fire => "A flame springs up before whoever steps on it, and has to be blown out.",
            _ => ""
        };

    /// <summary>What a good is, in our own words.</summary>
    public static string GoodBlurb(int good)
    {
        var record = UndergroundDatabase.Good(good);
        if (record == null) return "";
        string size = record.Width * record.Depth == 1 ? "It takes one tile" : $"It takes {record.Width} tiles by {record.Depth}";
        return SecretBaseRules.ToolTrap(good) != 0
            ? $"{size} of a secret base, and goes off like a trap under whoever steps on it."
            : $"{size} of a secret base's floor."
              + (record.Record != null ? " Something is written on it." : "");
    }

    public UndergroundRow? Selected => Cursor < Rows.Count ? Rows[Cursor] : null;

    public void Move(int dx, int dy, bool held = false)
    {
        if (dx == 0 && dy == 0) return;
        if (Message != null) return;
        if (Question != null)
        {
            if (dy != 0 || dx != 0) Answer = 1 - Answer;
            return;
        }
        if (Actions != null)
        {
            if (dy != 0) ActionCursor = UiNav.Wrap(ActionCursor, Math.Sign(dy), Actions.Count);
            return;
        }
        if (Paying)
        {
            if (dy != 0 && State.Spheres.Count > 0) PayCursor = UiNav.Wrap(PayCursor, Math.Sign(dy), State.Spheres.Count);
            return;
        }
        if (dx != 0)
        {
            Tab = UiNav.Wrap(Tab, Math.Sign(dx), Tabs.Count);
            Cursor = First = 0;
            Rebuild();
            AudioManager.PlaySound("page");
            return;
        }
        if (Rows.Count == 0) return;
        int next = held ? Math.Clamp(Cursor + Math.Sign(dy), 0, Rows.Count - 1) : UiNav.Wrap(Cursor, Math.Sign(dy), Rows.Count);
        if (next == Cursor) return;
        Cursor = next;
        First = UiNav.Window(First, Cursor, Rows.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    private void Say(string message, bool good = true)
    {
        Message = message;
        AudioManager.PlaySound(good ? "select" : "error");
    }

    private void Ask(string question, Action yes)
    {
        Question = question;
        Answer = 0;
        onYes = yes;
    }

    private void Hand(UndergroundRequestKind kind, int slot = -1)
    {
        request = new UndergroundRequest(kind, slot);
        IsActive = false;
    }

    public void Confirm()
    {
        if (Message != null)
        {
            Message = null;
            return;
        }
        if (Question != null)
        {
            Question = null;
            var yes = onYes;
            onYes = null;
            if (Answer == 0) yes?.Invoke();
            Rebuild();
            return;
        }
        if (Paying)
        {
            Pay();
            return;
        }
        if (Actions != null)
        {
            string action = Actions[ActionCursor];
            Actions = null;
            Act(action);
            Rebuild();
            return;
        }
        if (Selected is not { Enabled: true } row) return;
        string tab = Tabs[Tab];
        switch (tab)
        {
            case "TRAPS":
                Open(UndergroundDatabase.Trap(State.Traps[Cursor])?.Id == "TRAP_DIGGER_DRILL" ? "USE" : "SET", "TRASH", "CANCEL");
                break;
            case "SPHERES":
                Open("BURY", "TRASH", "CANCEL");
                break;
            case "GOODS":
                Open("TRASH", "CANCEL");
                break;
            case "TREASURES":
                Open("PUT IN BAG", "TRASH", "CANCEL");
                break;
            case "SURFACE":
                Hand(UndergroundRequestKind.GoUp);
                break;
            case "HIS WARES":
                if (State.Spheres.Count == 0) Say("No Spheres on you? Come back when you've dug some up.", false);
                else
                {
                    Paying = true;
                    PayCursor = Math.Clamp(PayCursor, 0, State.Spheres.Count - 1);
                    AudioManager.PlaySound("select");
                }
                break;
            case "YOUR TRAPS" or "YOUR GOODS" or "YOUR TREASURES":
            {
                int slot = Cursor;
                var list = tab == "YOUR TRAPS" ? State.Traps : tab == "YOUR GOODS" ? State.Goods : State.Treasures;
                var worth = UndergroundVendors.Worth(State.Seed, Vendor, list[slot]);
                if (worth == null || worth.Size < 1) Say("I've no use for that.", false);
                else
                    Ask($"Trade the {row.Name} for a {UndergroundDatabase.SphereName(worth.Kind)} of size {worth.Size}?", () =>
                    {
                        var result = UndergroundVendors.Sell(State, Vendor, slot);
                        Say(result == TradeResult.Done ? $"A fair swap! You got a {UndergroundDatabase.SphereName(worth.Kind)} of size {worth.Size}."
                            : result == TradeResult.BagFull ? "Hold on, you've nowhere to put it!" : "I've no use for that.", result == TradeResult.Done);
                    });
                break;
            }
            case "DECORATE":
                Hand(UndergroundRequestKind.Decorate);
                break;
            case "STORE":
                Say(State.Deposit(Cursor) ? $"The {row.Name} was stored in the PC." : "The PC is full.");
                Rebuild();
                break;
            case "TAKE":
                if (State.IsDisplayed(Cursor)) Say("It's out on display. Put it away first.", false);
                else Say(State.Withdraw(Cursor) ? $"The {row.Name} was taken out of the PC." : "The bag is full.");
                Rebuild();
                break;
        }
    }

    private void Open(params string[] actions)
    {
        Actions = actions;
        ActionCursor = 0;
        AudioManager.PlaySound("select");
    }

    private void Act(string action)
    {
        string tab = Tabs[Tab];
        int slot = Cursor;
        switch (action)
        {
            case "SET":
                Hand(UndergroundRequestKind.SetTrap, slot);
                break;
            case "USE":
                Hand(UndergroundRequestKind.UseDrill, slot);
                break;
            case "BURY":
                Hand(UndergroundRequestKind.BurySphere, slot);
                break;
            case "PUT IN BAG":
            {
                var treasure = UndergroundDatabase.Treasure(State.Treasures[slot]);
                if (treasure?.Item != null && putInBag(treasure.Item))
                {
                    State.Treasures.RemoveAt(slot);
                    Say($"The {treasure.Name} was put in the bag.");
                }
                else Say("The bag is full. It couldn't be put away.", false);
                break;
            }
            case "TRASH":
            {
                string name = Rows[slot].Name;
                Ask($"Throw the {name} away?", () =>
                {
                    if (tab == "TRAPS") State.Traps.RemoveAt(slot);
                    else if (tab == "SPHERES") State.Spheres.RemoveAt(slot);
                    else if (tab == "GOODS") State.Goods.RemoveAt(slot);
                    else State.Treasures.RemoveAt(slot);
                    Say($"The {name} was thrown away.");
                });
                break;
            }
        }
    }

    /// <summary>The Sphere under the paying cursor for the ware under the list's (<c>UndergroundVendors_TryBuyItem</c>), and the hiker's word.</summary>
    private void Pay()
    {
        Paying = false;
        var offer = stock[Cursor];
        string name = Rows[Cursor].Name;
        var result = UndergroundVendors.Buy(State, Vendor, offer, PayCursor);
        Say(result switch
        {
            TradeResult.Done => $"Done! The {name} is yours.",
            TradeResult.WrongSphere => "No, no. It's another colour of Sphere I'm after.",
            TradeResult.SphereTooSmall => "Too small, that one. I want a bigger Sphere for it.",
            _ => "Hold on, you've nowhere to put it!"
        }, result == TradeResult.Done);
        Rebuild();
    }

    public void Cancel()
    {
        AudioManager.PlaySound("cancel");
        if (Message != null) Message = null;
        else if (Question != null)
        {
            Question = null;
            onYes = null;
        }
        else if (Paying) Paying = false;
        else if (Actions != null) Actions = null;
        else IsActive = false;
    }

    private readonly HeldKey heldY = new();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dy = heldY.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (dx != 0 || dy != 0) Move(dx, dy, heldY.Repeating);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (IsActive) ModernUi.DrawUnderground(screenWidth, screenHeight, this);
    }
}
