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

/// <summary>A tab of the Underground's screen: what its list holds.</summary>
public enum UndergroundTab
{
    Traps,
    Spheres,
    Goods,
    Treasures,
    Surface,
    /// <summary>A hiker's own stock.</summary>
    Wares,
    YourTraps,
    YourGoods,
    YourTreasures,
    Decorate,
    Store,
    Take,
    Flags
}

/// <summary>What can be done with a row of the bag below ground.</summary>
public enum UndergroundAction
{
    Set,
    Use,
    Bury,
    PutInBag,
    Trash,
    Cancel
}

/// <summary>A row of one of the screen's lists: a name, a word at its right, what the panel beside says of it, and its picture.</summary>
public sealed record UndergroundRow(string Name, string Value, string Detail, string Icon, bool Enabled = true)
{
    /// <summary>A row's picture of a Sphere, and of a treasure, begins so; the rest of the name says which.</summary>
    public const string SpherePicture = "sphere:", TreasurePicture = "treasure:";

    public static string SphereIcon(SphereKind kind) => SpherePicture + kind;

    public static string TreasureIcon(int id) => TreasurePicture + id;
}

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
    public IReadOnlyList<UndergroundTab> TabKinds { get; private set; } = Array.Empty<UndergroundTab>();

    /// <summary>The tabs' names, as the screen shows them.</summary>
    public IReadOnlyList<string> Tabs => TabKinds.Select(TabName).ToList();
    public int Tab { get; private set; }
    public int Cursor { get; private set; }
    public int First { get; private set; }
    public IReadOnlyList<UndergroundRow> Rows { get; private set; } = Array.Empty<UndergroundRow>();

    /// <summary>The little menu open on a row, if any, and its cursor.</summary>
    public IReadOnlyList<UndergroundAction>? ActionKinds { get; private set; }

    /// <summary>The little menu's words, as the screen shows them.</summary>
    public IReadOnlyList<string>? Actions => ActionKinds?.Select(ActionName).ToList();
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
        Begin(state, UndergroundMode.Bag, new[] { UndergroundTab.Traps, UndergroundTab.Spheres, UndergroundTab.Goods, UndergroundTab.Treasures, UndergroundTab.Surface }, tab);
        this.putInBag = putInBag;
    }

    public void OpenHiker(UndergroundState state, int vendor, bool national)
    {
        Vendor = vendor;
        National = national;
        stock = UndergroundVendors.Stock(state.Seed, vendor, national);
        string kind = UndergroundVendors.KindOf(vendor);
        Begin(state, UndergroundMode.Hiker, kind == "Treasures" ? new[] { UndergroundTab.YourTreasures }
            : new[] { UndergroundTab.Wares, kind == "Traps" ? UndergroundTab.YourTraps : UndergroundTab.YourGoods }, 0);
    }

    public void OpenPc(UndergroundState state) => Begin(state, UndergroundMode.Pc, new[] { UndergroundTab.Decorate, UndergroundTab.Store, UndergroundTab.Take, UndergroundTab.Flags }, 0);

    private void Begin(UndergroundState state, UndergroundMode mode, UndergroundTab[] tabs, int tab)
    {
        State = state;
        Mode = mode;
        TabKinds = tabs;
        Tab = Math.Clamp(tab, 0, tabs.Length - 1);
        Cursor = First = 0;
        ActionKinds = null;
        Paying = false;
        Question = Message = null;
        request = default;
        Age = 0f;
        IsActive = true;
        Rebuild();
        AudioManager.PlaySound("select");
    }

    public void Close() => IsActive = false;

    public static string TabName(UndergroundTab tab) => Text.Of($"belowGround.tab.{tab}");

    public static string ActionName(UndergroundAction action) => Text.Of($"belowGround.action.{action}");

    private static string SphereWord(SphereKind kind, int size) => Text.Of($"belowGround.price.{kind}", ("size", size));

    /// <summary>The list under the tab, as things stand.</summary>
    private void Rebuild()
    {
        var rows = new List<UndergroundRow>();
        var tab = TabKinds[Tab];
        switch (tab)
        {
            case UndergroundTab.Traps or UndergroundTab.YourTraps:
                foreach (int trap in State.Traps)
                    rows.Add(new UndergroundRow(UndergroundDatabase.TrapName(trap), Worth(trap), TrapBlurb(trap), "trap"));
                break;
            case UndergroundTab.Spheres:
                foreach (var sphere in State.Spheres)
                    rows.Add(new UndergroundRow(UndergroundDatabase.SphereName(sphere.Kind), sphere.Size.ToString(),
                        Text.Of("belowGround.sphereNote"), UndergroundRow.SphereIcon(sphere.Kind)));
                break;
            case UndergroundTab.Goods or UndergroundTab.YourGoods or UndergroundTab.Store:
                foreach (int good in State.Goods)
                    rows.Add(new UndergroundRow(UndergroundDatabase.GoodName(good), Worth(good), GoodBlurb(good), "good"));
                break;
            case UndergroundTab.Treasures or UndergroundTab.YourTreasures:
                foreach (int treasure in State.Treasures)
                    rows.Add(new UndergroundRow(UndergroundDatabase.TreasureName(treasure), Worth(treasure),
                        Text.Of("belowGround.treasureNote"), UndergroundRow.TreasureIcon(treasure)));
                break;
            case UndergroundTab.Surface:
                rows.Add(new UndergroundRow(Text.Of("belowGround.goUp.name"), "", Text.Of("belowGround.goUp.note"), "up"));
                rows.Add(new UndergroundRow(Text.Of("belowGround.record.spheresDug"), State.Records.SpheresDug.ToString(), "", UndergroundRow.SphereIcon(SphereKind.Prism), false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.record.fossilsDug"), State.Records.FossilsDug.ToString(), "", UndergroundRow.TreasureIcon(0), false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.record.treasuresDug"), State.Records.TreasuresDug.ToString(), "", UndergroundRow.TreasureIcon(0), false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.record.trapsTriggered"), State.Records.TrapsTriggered.ToString(), "", "trap", false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.flags.broughtHome"), State.Records.FlagsCaptured.ToString(), "", "flag", false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.record.timesBaseMoved"), State.Records.TimesBaseMoved.ToString(), "", "good", false));
                break;
            case UndergroundTab.Wares:
                foreach (var offer in stock)
                {
                    bool traps = UndergroundVendors.KindOf(Vendor) == "Traps";
                    rows.Add(new UndergroundRow(traps ? UndergroundDatabase.TrapName(offer.Thing) : UndergroundDatabase.GoodName(offer.Thing),
                        SphereWord(offer.Kind, offer.Size), traps ? TrapBlurb(offer.Thing) : GoodBlurb(offer.Thing), traps ? "trap" : "good"));
                }
                break;
            case UndergroundTab.Decorate:
                rows.Add(new UndergroundRow(Text.Of("decorating.title"), "", Text.Of("belowGround.decorateNote"), "good"));
                break;
            case UndergroundTab.Take:
                for (int slot = 0; slot < State.PcGoods.Count; slot++)
                    rows.Add(new UndergroundRow(UndergroundDatabase.GoodName(State.PcGoods[slot]), State.IsDisplayed(slot) ? Text.Of("belowGround.out") : "", GoodBlurb(State.PcGoods[slot]), "good"));
                break;
            case UndergroundTab.Flags:
            {
                int flags = State.Records.FlagsCaptured;
                var rank = SecretBaseRules.FlagFor(flags);
                rows.Add(new UndergroundRow(Text.Of("belowGround.flags.thisBase"), Text.Of($"belowGround.flag.{rank}"), "", "flag", false));
                rows.Add(new UndergroundRow(Text.Of("belowGround.flags.broughtHome"), flags.ToString(), "", "flag", false));
                foreach (string owner in State.FlagOwners) rows.Add(new UndergroundRow(Text.Of("belowGround.flags.owner", ("owner", owner)), "", "", "flag", false));
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
    public static string TrapBlurb(int trap) => UndergroundDatabase.Trap(trap)?.Id == DiggerDrill
        ? Text.Of("belowGround.trap.drill")
        : TrapEffect.FamilyOf(trap) switch
        {
            TrapFamily.Slide => Text.Of("belowGround.trap.slide"),
            TrapFamily.Hole => Text.Of("belowGround.trap.hole"),
            TrapFamily.Steering => Text.Of("belowGround.trap.steering"),
            TrapFamily.Smoke => Text.Of("belowGround.trap.smoke"),
            TrapFamily.Rock => Text.Of("belowGround.trap.rock"),
            TrapFamily.Bubbles => Text.Of("belowGround.trap.bubbles"),
            TrapFamily.Alert => Text.Of("belowGround.trap.alert"),
            TrapFamily.Leaves => Text.Of("belowGround.trap.leaves"),
            TrapFamily.Fire => Text.Of("belowGround.trap.fire"),
            _ => ""
        };

    // The Digger Drill's id in the trap table: it is used where the other traps are set
    private const string DiggerDrill = "TRAP_DIGGER_DRILL";

    /// <summary>What a good is, in our own words.</summary>
    public static string GoodBlurb(int good)
    {
        var record = UndergroundDatabase.Good(good);
        if (record == null) return "";
        string size = record.Width * record.Depth == 1 ? Text.Of("belowGround.good.oneTile") : Text.Of("belowGround.good.tiles", ("width", record.Width), ("depth", record.Depth));
        return SecretBaseRules.ToolTrap(good) != 0
            ? Text.Of("belowGround.good.trap", ("size", size))
            : Text.Of("belowGround.good.floor", ("size", size))
              + (record.Record != null ? Text.Of("belowGround.good.written") : "");
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
        if (ActionKinds != null)
        {
            if (dy != 0) ActionCursor = UiNav.Wrap(ActionCursor, Math.Sign(dy), ActionKinds.Count);
            return;
        }
        if (Paying)
        {
            if (dy != 0 && State.Spheres.Count > 0) PayCursor = UiNav.Wrap(PayCursor, Math.Sign(dy), State.Spheres.Count);
            return;
        }
        if (dx != 0)
        {
            Tab = UiNav.Wrap(Tab, Math.Sign(dx), TabKinds.Count);
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
        if (ActionKinds != null)
        {
            var action = ActionKinds[ActionCursor];
            ActionKinds = null;
            Act(action);
            Rebuild();
            return;
        }
        if (Selected is not { Enabled: true } row) return;
        var tab = TabKinds[Tab];
        switch (tab)
        {
            case UndergroundTab.Traps:
                Open(UndergroundDatabase.Trap(State.Traps[Cursor])?.Id == DiggerDrill ? UndergroundAction.Use : UndergroundAction.Set, UndergroundAction.Trash, UndergroundAction.Cancel);
                break;
            case UndergroundTab.Spheres:
                Open(UndergroundAction.Bury, UndergroundAction.Trash, UndergroundAction.Cancel);
                break;
            case UndergroundTab.Goods:
                Open(UndergroundAction.Trash, UndergroundAction.Cancel);
                break;
            case UndergroundTab.Treasures:
                Open(UndergroundAction.PutInBag, UndergroundAction.Trash, UndergroundAction.Cancel);
                break;
            case UndergroundTab.Surface:
                Hand(UndergroundRequestKind.GoUp);
                break;
            case UndergroundTab.Wares:
                if (State.Spheres.Count == 0) Say(Text.Of("belowGround.hiker.noSpheres"), false);
                else
                {
                    Paying = true;
                    PayCursor = Math.Clamp(PayCursor, 0, State.Spheres.Count - 1);
                    AudioManager.PlaySound("select");
                }
                break;
            case UndergroundTab.YourTraps or UndergroundTab.YourGoods or UndergroundTab.YourTreasures:
            {
                int slot = Cursor;
                var list = tab == UndergroundTab.YourTraps ? State.Traps : tab == UndergroundTab.YourGoods ? State.Goods : State.Treasures;
                var worth = UndergroundVendors.Worth(State.Seed, Vendor, list[slot]);
                if (worth == null || worth.Size < 1) Say(Text.Of("belowGround.hiker.noUse"), false);
                else
                {
                    string sphere = UndergroundDatabase.SphereName(worth.Kind);
                    Ask(Text.Of("belowGround.hiker.trade", ("thing", row.Name), ("sphere", sphere), ("size", worth.Size)), () =>
                    {
                        var result = UndergroundVendors.Sell(State, Vendor, slot);
                        Say(result == TradeResult.Done ? Text.Of("belowGround.hiker.fairSwap", ("sphere", sphere), ("size", worth.Size))
                            : result == TradeResult.BagFull ? Text.Of("belowGround.hiker.nowhere") : Text.Of("belowGround.hiker.noUse"), result == TradeResult.Done);
                    });
                }
                break;
            }
            case UndergroundTab.Decorate:
                Hand(UndergroundRequestKind.Decorate);
                break;
            case UndergroundTab.Store:
                Say(State.Deposit(Cursor) ? Text.Of("belowGround.stored", ("thing", row.Name)) : Text.Of("belowGround.pcFull"));
                Rebuild();
                break;
            case UndergroundTab.Take:
                if (State.IsDisplayed(Cursor)) Say(Text.Of("belowGround.onDisplay"), false);
                else Say(State.Withdraw(Cursor) ? Text.Of("belowGround.taken", ("thing", row.Name)) : Text.Of("belowGround.bagFull"));
                Rebuild();
                break;
        }
    }

    private void Open(params UndergroundAction[] actions)
    {
        ActionKinds = actions;
        ActionCursor = 0;
        AudioManager.PlaySound("select");
    }

    private void Act(UndergroundAction action)
    {
        var tab = TabKinds[Tab];
        int slot = Cursor;
        switch (action)
        {
            case UndergroundAction.Set:
                Hand(UndergroundRequestKind.SetTrap, slot);
                break;
            case UndergroundAction.Use:
                Hand(UndergroundRequestKind.UseDrill, slot);
                break;
            case UndergroundAction.Bury:
                Hand(UndergroundRequestKind.BurySphere, slot);
                break;
            case UndergroundAction.PutInBag:
            {
                var treasure = UndergroundDatabase.Treasure(State.Treasures[slot]);
                if (treasure?.Item != null && putInBag(treasure.Item))
                {
                    State.Treasures.RemoveAt(slot);
                    Say(Text.Of("belowGround.putInBag", ("thing", treasure.Name)));
                }
                else Say(Text.Of("belowGround.noRoomInBag"), false);
                break;
            }
            case UndergroundAction.Trash:
            {
                string name = Rows[slot].Name;
                Ask(Text.Of("belowGround.throwAway", ("thing", name)), () =>
                {
                    if (tab == UndergroundTab.Traps) State.Traps.RemoveAt(slot);
                    else if (tab == UndergroundTab.Spheres) State.Spheres.RemoveAt(slot);
                    else if (tab == UndergroundTab.Goods) State.Goods.RemoveAt(slot);
                    else State.Treasures.RemoveAt(slot);
                    Say(Text.Of("belowGround.thrownAway", ("thing", name)));
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
            TradeResult.Done => Text.Of("belowGround.hiker.done", ("thing", name)),
            TradeResult.WrongSphere => Text.Of("belowGround.hiker.wrongSphere"),
            TradeResult.SphereTooSmall => Text.Of("belowGround.hiker.tooSmall"),
            _ => Text.Of("belowGround.hiker.nowhere")
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
        else if (ActionKinds != null) ActionKinds = null;
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
