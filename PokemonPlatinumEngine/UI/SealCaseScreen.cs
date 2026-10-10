using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Ball Capsules' screen is.</summary>
public enum CapsuleStep
{
    /// <summary>The twelve capsules, to choose one.</summary>
    Capsules,
    /// <summary>What to do with the capsule chosen.</summary>
    Menu,
    /// <summary>A second cursor on the capsule to change places with.</summary>
    Swap,
    /// <summary>The team, to put the capsule on one's ball.</summary>
    Attach,
    /// <summary>The capsule's Seals: the case's kinds under the cursor.</summary>
    Seals,
    /// <summary>A Seal in hand over the ball, moved with the arrows.</summary>
    Holding,
    /// <summary>The Seals on the ball, to pick one up again.</summary>
    OnBall,
    /// <summary>"Leave without keeping the changes?" with YES and NO.</summary>
    Quit
}

/// <summary>What the menu of a capsule offers.</summary>
public enum CapsuleAction { Attach, Detach, Edit, Swap, Cancel }

/// <summary>
/// The Ball Capsules (plan 06 · R17b; the original's capsule screen, <c>src/applications/capsule_menu/</c>): the Seal
/// Case's twelve capsules in three rows of four; a capsule's menu, by what is on it (nothing: EDIT, SWAP; Seals:
/// ATTACH first; on a Pokémon's ball: DETACH first); its Seals changed, the case's kinds eight to a page, a Seal taken
/// in hand, moved over the ball with the arrows and put down (off the ball it goes back into the case), those on the
/// ball picked up again; two capsules changing places; a capsule put on the ball of a Pokémon of the team. The
/// original's stylus is the arrows here. Its steps take no input of their own, so tests and the harness drive it.
/// </summary>
public sealed class SealCaseScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The capsules stand four to a row, and a page of Seals is two by four (<c>SEALS_PER_PAGE</c>).</summary>
    public const int Columns = 4, PageSize = 8, PageColumns = 2;

    /// <summary>How far a press of an arrow moves the Seal in hand, in the capsule's dots, and how far from the ball's middle it can be taken.</summary>
    public const int Nudge = 4, Range = 78;

    public bool IsActive { get; private set; }
    public CapsuleStep Step { get; private set; }

    /// <summary>The capsule under the cursor, and the second cursor while two change places.</summary>
    public int Cursor { get; private set; }
    public int Other { get; private set; }

    public IReadOnlyList<CapsuleAction> Menu { get; private set; } = Array.Empty<CapsuleAction>();
    public int MenuIndex { get; private set; }

    /// <summary>The Pokémon of the team under the cursor while a capsule is put on a ball.</summary>
    public int Target { get; private set; }

    /// <summary>The capsule being changed, while it is.</summary>
    public CapsuleEdit? Edit { get; private set; }

    /// <summary>The kinds of Seal the case had as the change began, the page of them shown and the cursor on it.</summary>
    public IReadOnlyList<int> Kinds { get; private set; } = Array.Empty<int>();
    public int Page { get; private set; }
    public int SealCursor { get; private set; }

    /// <summary>The Seal in hand, or under the cursor on the ball: its place on the capsule.</summary>
    public int Held { get; private set; }

    /// <summary>"Leave without keeping the changes?": 0 YES, 1 NO.</summary>
    public int QuitChoice { get; private set; }

    /// <summary>A line in place of the prompt until the next button.</summary>
    public string? Message { get; private set; }

    /// <summary>How many capsules were changed and kept this visit, for the Trainer Card's score (the original counts each CONFIRM).</summary>
    public int Kept { get; private set; }

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    private SealCase sealCase = new();
    private Party party = new();
    private float carryX, carryY;

    public SealCase Case => sealCase;

    public void Open(SealCase from, Party team)
    {
        sealCase = from;
        party = team;
        IsActive = true;
        Cursor = Other = 0;
        Edit = null;
        Message = null;
        Kept = 0;
        Age = 0f;
        Go(CapsuleStep.Capsules);
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    private void Go(CapsuleStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    /// <summary>The Pokémon of the team whose ball a capsule is on, or null.</summary>
    public Pokemon? HolderOf(int capsule) => CapsuleRules.HolderOf(party, capsule);

    /// <summary>The capsule a place shows: the one being changed as it stands, else the case's own.</summary>
    public BallCapsule Shown(int capsule) => Edit != null && Edit.Capsule == capsule ? Edit.Result : sealCase.Capsule(capsule);

    /// <summary>What a capsule's menu offers (<c>CapsuleGraphics_OpenCapsuleSelectionMenu</c>).</summary>
    public IReadOnlyList<CapsuleAction> ActionsFor(int capsule) =>
        HolderOf(capsule) != null ? new[] { CapsuleAction.Detach, CapsuleAction.Edit, CapsuleAction.Swap, CapsuleAction.Cancel }
        : !sealCase.Capsule(capsule).IsEmpty ? new[] { CapsuleAction.Attach, CapsuleAction.Edit, CapsuleAction.Swap, CapsuleAction.Cancel }
        : new[] { CapsuleAction.Edit, CapsuleAction.Swap, CapsuleAction.Cancel };

    public static string Label(CapsuleAction action) => action.ToString().ToUpperInvariant();

    /// <summary>The Seals of the page shown: up to eight kinds.</summary>
    public IReadOnlyList<int> PageKinds => Kinds.Skip(Page * PageSize).Take(PageSize).ToList();

    public int Pages => Math.Max(1, (Kinds.Count + PageSize - 1) / PageSize);

    /// <summary>The kind of Seal under the cursor, or 0.</summary>
    public int KindUnderCursor => PageKinds is var kinds && SealCursor < kinds.Count ? kinds[SealCursor] : 0;

    /// <summary>The prompt at the foot of the screen.</summary>
    public string Prompt => Message ?? Step switch
    {
        CapsuleStep.Menu => "Do what with this Ball Capsule?",
        CapsuleStep.Swap => "Swap it with which Ball Capsule?",
        CapsuleStep.Attach => "Put the Ball Capsule on whose ball?",
        CapsuleStep.Seals => Kinds.Count == 0 ? "The Seal Case is empty." : KindUnderCursor is var kind && kind != 0 ? SealDatabase.Name(kind) : "Choose a Seal.",
        CapsuleStep.Holding => Edit != null && Held < Edit.Seals.Count && !Edit.Seals[Held].OnTheBall ? "Off the ball, it goes back in the case." : "Move the Seal and stick it on.",
        CapsuleStep.OnBall => Edit != null && Held < Edit.Seals.Count ? SealDatabase.Name(Edit.Seals[Held].Seal) : "No Seals are on the ball.",
        CapsuleStep.Quit => "Leave without keeping the changes?",
        _ => "Choose a Ball Capsule."
    };

    // ---------------------------------------------------------------- the buttons

    /// <summary>
    /// The arrows. Over the capsules: left and right go round all twelve, up and down stop at the first and last
    /// rows (<c>CapsuleManager_HandleCapsulePadMovement</c>). Over a page of Seals: past its top or foot the page
    /// turns. With a Seal in hand it moves; on the ball left and right step through the Seals stuck on.
    /// </summary>
    public void Move(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;
        Message = null;
        switch (Step)
        {
            case CapsuleStep.Capsules:
                if (MoveCapsule(Cursor, dx, dy) is { } to)
                {
                    Cursor = Other = to;
                    AudioManager.PlaySound("cursor");
                }
                break;
            case CapsuleStep.Swap:
                if (MoveCapsule(Other, dx, dy) is { } second)
                {
                    Other = second;
                    AudioManager.PlaySound("cursor");
                }
                break;
            case CapsuleStep.Menu:
                if (dy == 0) return;
                MenuIndex = UiNav.Wrap(MenuIndex, Math.Sign(dy), Menu.Count);
                AudioManager.PlaySound("cursor");
                break;
            case CapsuleStep.Attach:
                if (party.Count == 0) return;
                int next = UiNav.Grid(Target, party.Count, 2, dx, dy);
                if (next == Target) return;
                Target = next;
                AudioManager.PlaySound("cursor");
                break;
            case CapsuleStep.Seals:
                MoveSealCursor(dx, dy);
                break;
            case CapsuleStep.Holding:
                Carry(dx * Nudge, -dy * Nudge);
                break;
            case CapsuleStep.OnBall:
                if (Edit == null || Edit.Seals.Count == 0) return;
                Held = UiNav.Wrap(Held, dx != 0 ? Math.Sign(dx) : Math.Sign(dy), Edit.Seals.Count);
                AudioManager.PlaySound("cursor");
                break;
            case CapsuleStep.Quit:
                QuitChoice = QuitChoice == 0 ? 1 : 0;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    private static int? MoveCapsule(int from, int dx, int dy)
    {
        if (dx > 0) return (from + 1) % SealCase.Capsules;
        if (dx < 0) return from > 0 ? from - 1 : SealCase.Capsules - 1;
        if (dy < 0) return from / Columns != 0 ? from - Columns : null;
        if (dy > 0) return from / Columns != SealCase.Capsules / Columns - 1 ? from + Columns : null;
        return null;
    }

    private void MoveSealCursor(int dx, int dy)
    {
        if (Kinds.Count == 0) return;
        int col = SealCursor % PageColumns, row = SealCursor / PageColumns;
        if (dx != 0) col = UiNav.Wrap(col, Math.Sign(dx), PageColumns);
        if (dy != 0)
        {
            row += Math.Sign(dy);
            // Off the top of the page, or past its last Seals, the page turns (the original's two page buttons)
            if (row < 0 || row >= (PageKinds.Count + PageColumns - 1) / PageColumns)
            {
                Page = UiNav.Wrap(Page, Math.Sign(dy), Pages);
                row = row < 0 ? (PageKinds.Count + PageColumns - 1) / PageColumns - 1 : 0;
                AudioManager.PlaySound("page");
            }
        }
        int count = PageKinds.Count;
        SealCursor = Math.Clamp(row * PageColumns + col, 0, Math.Max(0, count - 1));
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Puts the cursor on a kind of Seal, turning to its page; false when the case had none as the change began.</summary>
    public bool PointAt(int seal)
    {
        int at = Kinds.ToList().IndexOf(seal);
        if (Step != CapsuleStep.Seals || at < 0) return false;
        Page = at / PageSize;
        SealCursor = at % PageSize;
        return true;
    }

    /// <summary>Moves the Seal in hand by so many dots, right and up, within reach of the ball.</summary>
    public void Carry(int right, int up)
    {
        if (Step != CapsuleStep.Holding || Edit == null || Held >= Edit.Seals.Count) return;
        var seal = Edit.Seals[Held];
        int x = seal.X + right, y = seal.Y - up;
        // Never so far that it can't be seen: a little past the ball's edge is enough to put it back
        int dx = x - BallSeal.CentreX, dy = y - BallSeal.CentreY;
        double far = Math.Sqrt(dx * dx + dy * dy);
        if (far > Range)
        {
            x = BallSeal.CentreX + (int)Math.Round(dx * Range / far);
            y = BallSeal.CentreY + (int)Math.Round(dy * Range / far);
        }
        Edit.Move(Held, x, y);
    }

    public void Confirm()
    {
        Message = null;
        switch (Step)
        {
            case CapsuleStep.Capsules:
                Menu = ActionsFor(Cursor);
                MenuIndex = 0;
                Go(CapsuleStep.Menu);
                AudioManager.PlaySound("select");
                break;
            case CapsuleStep.Menu:
                Choose(Menu[MenuIndex]);
                break;
            case CapsuleStep.Swap:
                CapsuleRules.Swap(sealCase, party, Cursor, Other);
                Cursor = Other;
                Message = "The Ball Capsules changed places.";
                Go(CapsuleStep.Capsules);
                AudioManager.PlaySound("select");
                break;
            case CapsuleStep.Attach:
                AttachTo(Target);
                break;
            case CapsuleStep.Seals:
                TakeSeal();
                break;
            case CapsuleStep.Holding:
                PutDown();
                break;
            case CapsuleStep.OnBall:
                if (Edit == null || Edit.Seals.Count == 0) return;
                Held = Edit.Lift(Held);
                Go(CapsuleStep.Holding);
                AudioManager.PlaySound("select");
                break;
            case CapsuleStep.Quit:
                if (QuitChoice == 0)
                {
                    Edit?.GiveUp();
                    Edit = null;
                    Go(CapsuleStep.Capsules);
                    AudioManager.PlaySound("cancel");
                }
                else
                {
                    Go(CapsuleStep.Seals);
                    AudioManager.PlaySound("select");
                }
                break;
        }
    }

    /// <summary>Carries out an entry of the capsule's menu.</summary>
    public void Choose(CapsuleAction action)
    {
        AudioManager.PlaySound(action == CapsuleAction.Cancel ? "cancel" : "select");
        switch (action)
        {
            case CapsuleAction.Attach:
                Target = 0;
                Go(CapsuleStep.Attach);
                break;
            case CapsuleAction.Detach:
                string? from = HolderOf(Cursor)?.DisplayName;
                CapsuleRules.Free(party, Cursor);
                Message = from != null ? $"The Ball Capsule was taken off {from}'s ball." : null;
                Go(CapsuleStep.Capsules);
                break;
            case CapsuleAction.Edit:
                Edit = new CapsuleEdit(sealCase, Cursor);
                Kinds = sealCase.Owned;
                Page = 0;
                SealCursor = 0;
                Go(CapsuleStep.Seals);
                break;
            case CapsuleAction.Swap:
                Other = Cursor;
                Go(CapsuleStep.Swap);
                break;
            default:
                Go(CapsuleStep.Capsules);
                break;
        }
    }

    // PartyMenu_SetBallCapsule: the capsule goes on the ball of the Pokémon chosen; an Egg's ball takes none
    private void AttachTo(int slot)
    {
        if (slot < 0 || slot >= party.Count) return;
        var pokemon = party.Members[slot];
        if (!CapsuleRules.Attach(sealCase, Cursor, pokemon))
        {
            Message = pokemon.IsEgg ? "An Egg's ball can't take a Ball Capsule." : "There are no Seals on that Ball Capsule.";
            AudioManager.PlaySound("error");
            return;
        }
        // One capsule, one ball: whoever carried it before carries it no longer
        foreach (var other in party.Members.Where(p => p != pokemon && p.CapsuleId == Cursor + 1)) CapsuleRules.Detach(other);
        Message = $"The Ball Capsule was put on {pokemon.DisplayName}'s ball.";
        Go(CapsuleStep.Capsules);
        AudioManager.PlaySound("select");
    }

    // CapsuleManager_HandleSpritePress on one of the page's eight: a Seal out of the case into the hand
    private void TakeSeal()
    {
        if (Edit == null || KindUnderCursor is not (> 0 and var kind)) return;
        if (Edit.Full)
        {
            Message = "A Ball Capsule holds eight Seals at most.";
            AudioManager.PlaySound("error");
            return;
        }
        if (sealCase.Count(kind) == 0)
        {
            Message = "There are no more of that Seal in the case.";
            AudioManager.PlaySound("error");
            return;
        }
        Held = Edit.Take(kind);
        carryX = carryY = 0f;
        Go(CapsuleStep.Holding);
        AudioManager.PlaySound("select");
    }

    // The stylus lifted: on the ball the Seal sticks, off it it goes back into the case
    private void PutDown()
    {
        if (Edit == null) return;
        bool stuck = Edit.Drop(Held);
        Message = stuck ? null : "The Seal went back into the case.";
        AudioManager.PlaySound(stuck ? "select" : "cancel");
        Go(CapsuleStep.Seals);
    }

    /// <summary>
    /// The item button (C) while a capsule is changed: from the case's Seals to those on the ball and back, to pick one up
    /// again (the original touches a Seal on the ball).
    /// </summary>
    public void SwitchSide()
    {
        if (Edit == null) return;
        Message = null;
        if (Step == CapsuleStep.Seals)
        {
            if (Edit.Seals.Count == 0)
            {
                Message = "No Seals are on the ball.";
                AudioManager.PlaySound("error");
                return;
            }
            Held = 0;
            Go(CapsuleStep.OnBall);
            AudioManager.PlaySound("page");
        }
        else if (Step == CapsuleStep.OnBall)
        {
            Go(CapsuleStep.Seals);
            AudioManager.PlaySound("page");
        }
    }

    /// <summary>
    /// The Start button while a capsule is changed (the original's CONFIRM): the capsule is kept as it stands, and
    /// the Pokémon whose ball it is on takes its new Seals.
    /// </summary>
    public void Keep()
    {
        if (Edit == null || Step is not (CapsuleStep.Seals or CapsuleStep.OnBall)) return;
        CapsuleRules.Keep(Edit, party);
        Kept++;
        Edit = null;
        Message = "The Ball Capsule was put away as it is.";
        Go(CapsuleStep.Capsules);
        AudioManager.PlaySound("levelup");
    }

    public void Cancel()
    {
        Message = null;
        switch (Step)
        {
            case CapsuleStep.Capsules:
                Close();
                return;
            case CapsuleStep.Menu:
            case CapsuleStep.Attach:
                Go(CapsuleStep.Capsules);
                break;
            case CapsuleStep.Swap:
                Other = Cursor;
                Go(CapsuleStep.Capsules);
                break;
            case CapsuleStep.Seals:
                // CAPSULE_CANCEL_PRESSED: straight out when nothing changed, else the question
                if (Edit is { Changed: true })
                {
                    QuitChoice = 1;
                    Go(CapsuleStep.Quit);
                }
                else
                {
                    Edit?.GiveUp();
                    Edit = null;
                    Go(CapsuleStep.Capsules);
                }
                break;
            case CapsuleStep.Holding:
                // Let go of for good: back into the case
                Edit?.Remove(Held);
                Message = "The Seal went back into the case.";
                Go(CapsuleStep.Seals);
                break;
            case CapsuleStep.OnBall:
            case CapsuleStep.Quit:
                Go(CapsuleStep.Seals);
                break;
        }
        AudioManager.PlaySound("cancel");
    }

    // ---------------------------------------------------------------- the keys and the picture

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (Step == CapsuleStep.Holding && dx == 0 && dy == 0)
        {
            // An arrow kept down carries the Seal on smoothly, sixty dots a second
            carryX += InputManager.Axis(GameAction.Left, GameAction.Right, held: true) * 60f * dt;
            carryY -= InputManager.Axis(GameAction.Up, GameAction.Down, held: true) * 60f * dt;
            int right = (int)carryX, up = (int)carryY;
            if (right != 0 || up != 0)
            {
                carryX -= right;
                carryY -= up;
                Carry(right, up);
            }
        }
        if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Menu) && Edit != null) Keep();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Item) && Step is CapsuleStep.Seals or CapsuleStep.OnBall) SwitchSide();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawBallCapsules(screenWidth, screenHeight, this, party, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}

/// <summary>
/// The Seal merchant's stall at the Sunyshore Market (plan 06 · R17b; the original's <c>PokeMartSeal</c>,
/// <c>src/overlay007/shop_menu.c</c>): the day's seven Seals with their prices, how many the player has of the one
/// under the cursor, and "how many?" before anything is bought, as many as the money and the Seal Case's ninety-nine
/// of a kind allow. Nothing is bought back. Its logic takes no input, so tests and the harness drive it.
/// </summary>
public sealed class SealShopScreen
{
    private const float AppearTime = 0.3f;

    public const int MostAtOnce = 99;

    public bool IsActive { get; private set; }
    public IReadOnlyList<int> Stock { get; private set; } = Array.Empty<int>();
    public int Cursor { get; private set; }
    public bool ChoosingQuantity { get; private set; }
    public int Quantity { get; private set; } = 1;

    /// <summary>How many Seals were bought this visit.</summary>
    public int Bought { get; private set; }

    public float Age { get; private set; }

    private readonly UiReveal asking = new(0.18f, 0.1f);
    private SealCase sealCase = new();

    public SealCase Case => sealCase;
    public int Selected => Cursor < Stock.Count ? Stock[Cursor] : 0;

    public void Open(IReadOnlyList<int> stock, SealCase into)
    {
        Stock = stock;
        sealCase = into;
        IsActive = true;
        Cursor = 0;
        ChoosingQuantity = false;
        Quantity = 1;
        Bought = 0;
        Age = 0f;
        asking.Snap(false);
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        ChoosingQuantity = false;
    }

    /// <summary>The most of a Seal that can be bought at once: what the money buys and the case still has room for.</summary>
    public int MostOf(int seal, int money)
    {
        int price = SealDatabase.Price(seal);
        int afford = price <= 0 ? MostAtOnce : money / price;
        return Math.Clamp(Math.Min(afford, sealCase.Room(seal)), 0, MostAtOnce);
    }

    /// <summary>Up and down the stock; while "how many?" is up, left and right change the number by one and up and down by ten, wrapping.</summary>
    public void Move(int dx, int dy, int money)
    {
        if (dx == 0 && dy == 0) return;
        if (ChoosingQuantity)
        {
            int most = Math.Max(1, MostOf(Selected, money));
            int next = Quantity + dx - dy * 10;
            if (next > most) next = Quantity == most ? 1 : most;
            else if (next < 1) next = Quantity == 1 ? most : 1;
            if (next != Quantity) AudioManager.PlaySound("cursor");
            Quantity = next;
            return;
        }
        if (dy == 0 || Stock.Count == 0) return;
        Cursor = UiNav.Wrap(Cursor, Math.Sign(dy), Stock.Count);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: on a Seal, asks how many (or says why none can be bought); on the number, buys them. Returns what the money changes by.</summary>
    public int Confirm(int money, Action<string> onNotification)
    {
        int seal = Selected;
        if (seal == 0) return 0;
        if (!ChoosingQuantity)
        {
            if (sealCase.Room(seal) < 1)
            {
                onNotification("The Seal Case has no room for more of those.");
                AudioManager.PlaySound("error");
                return 0;
            }
            if (MostOf(seal, money) < 1)
            {
                onNotification("You don't have enough money.");
                AudioManager.PlaySound("error");
                return 0;
            }
            ChoosingQuantity = true;
            Quantity = 1;
            asking.Open();
            AudioManager.PlaySound("select");
            return 0;
        }
        int count = Math.Clamp(Quantity, 1, Math.Max(1, MostOf(seal, money)));
        ChoosingQuantity = false;
        asking.Close();
        int cost = count * SealDatabase.Price(seal);
        if (cost > money || !sealCase.Give(seal, count)) return 0;
        Bought += count;
        AudioManager.PlaySound("select");
        onNotification(count == 1 ? $"Bought a {SealDatabase.Name(seal)}." : $"Bought {count} × {SealDatabase.Name(seal)}.");
        return -cost;
    }

    public void Cancel()
    {
        if (ChoosingQuantity)
        {
            ChoosingQuantity = false;
            asking.Close();
        }
        else Close();
        AudioManager.PlaySound("cancel");
    }

    public void Update(ref int money, Action<string> onNotification, float dt)
    {
        if (!IsActive) return;
        Age += dt;
        asking.Update(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dy, money);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) money += Confirm(money, onNotification);
    }

    public void Draw(int screenWidth, int screenHeight, int money)
    {
        if (!IsActive) return;
        ModernUi.DrawSealShop(screenWidth, screenHeight, this, money, Math.Clamp(Age / AppearTime, 0f, 1f), asking.Shown, asking.Visible);
    }
}
