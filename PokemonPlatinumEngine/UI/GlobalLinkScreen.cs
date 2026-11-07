using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The rows of the Global Link counter's menu.</summary>
public enum GlobalLinkRow { MakeCode, EnterCode, BattleCode, Leave }

/// <summary>
/// The Global Link counter (plan 07 · O5 and O7, the game's half; style guide, "Menu screens", "The Global Link
/// counter"): the screen over a <see cref="GlobalLinkSession"/>. The menu with the friend code, the keypad for the
/// other player's link code, the code made and its seconds, and the trade room: the team and the boxes on the left, the
/// two offers on the right, the line under them. Its logic takes no input (<see cref="Move"/>, <see cref="Sideways"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it; the session does the talking.
/// </summary>
public class GlobalLinkScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The keypad's twelve keys, row by row: 1 to 9, then ERASE, 0 and OK.</summary>
    public const int KeypadColumns = 3, KeypadKeys = 12, EraseKey = 9, ZeroKey = 10, OkKey = 11;

    /// <summary>The rows the room's list shows: as many as its panel holds.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    private float openAge;
    private Func<IReadOnlyList<Pokemon>> offerables = () => Array.Empty<Pokemon>();
    private SavedPokemonData? theirsSeen, receivedSeen;

    public bool IsActive { get; private set; }
    public GlobalLinkSession Session { get; private set; } = new(null, new NoKeeper());

    /// <summary>Where the server is, as the counter names it (its host and port), or null with none set.</summary>
    public string? ServerShown { get; private set; }

    public GlobalLinkRow MenuRow { get; private set; }
    public int Key { get; private set; }

    /// <summary>The player's Pokémon that may be offered (the team, then the boxes), as the room last read them.</summary>
    public IReadOnlyList<Pokemon> Offerable { get; private set; } = Array.Empty<Pokemon>();

    /// <summary>The battles fought with the friend of the battle room, once the last one is counted (plan 07 · O6).</summary>
    public LinkTally? Record { get; set; }

    /// <summary>The team a battle room is shown, brought as it stands (plan 07 · O6).</summary>
    public IReadOnlyList<Pokemon> Team { get; private set; } = Array.Empty<Pokemon>();
    private Func<IReadOnlyList<Pokemon>>? team;
    public int Cursor { get; private set; }
    public int First { get; private set; }

    /// <summary>What the other offers and what a trade brought, as Pokémon to draw (made once each).</summary>
    public Pokemon? Theirs { get; private set; }
    public Pokemon? Received { get; private set; }

    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    public LinkPhase Phase => Session.Phase;

    /// <summary>The Pokémon under the cursor in the room, or null.</summary>
    public Pokemon? Selected => Cursor < Offerable.Count ? Offerable[Cursor] : null;

    /// <summary>Whether the Pokémon under the cursor is the one offered (its id is the offer's).</summary>
    public bool SelectedIsOffered => Selected is { } p && Session.MyOffer?.Id == p.Id;

    /// <param name="session">The visit, connecting already (or failed already, with no server set).</param>
    /// <param name="offerables">The player's Pokémon that may be offered, each with its id; read again whenever the room is entered or a trade is made.</param>
    /// <param name="serverShown">The server's host as the counter shows it.</param>
    public void Open(GlobalLinkSession session, Func<IReadOnlyList<Pokemon>> offerables, string? serverShown, Func<IReadOnlyList<Pokemon>>? team = null)
    {
        Session = session;
        this.team = team;
        Team = Array.Empty<Pokemon>();
        this.offerables = offerables;
        ServerShown = serverShown;
        MenuRow = GlobalLinkRow.MakeCode;
        Key = 0;
        Cursor = First = 0;
        Offerable = Array.Empty<Pokemon>();
        theirsSeen = receivedSeen = null;
        Theirs = Received = null;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>The session moved on by a frame, and what it shows brought level with it.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        openAge += dt;
        var before = Session.Phase;
        Session.Update(dt);
        // The team a battle room is shown, read as the room is joined (plan 07 · O6)
        if (Session.Phase == LinkPhase.BattleRoom && before != LinkPhase.BattleRoom) Team = team?.Invoke() ?? Array.Empty<Pokemon>();
        if (Session.Phase == LinkPhase.Room && (before != LinkPhase.Room || Offerable.Count == 0)) Refresh();
        if (!ReferenceEquals(theirsSeen, Session.TheirOffer))
        {
            theirsSeen = Session.TheirOffer;
            Theirs = theirsSeen?.ToPokemon();
        }
        if (!ReferenceEquals(receivedSeen, Session.Received))
        {
            receivedSeen = Session.Received;
            Received = receivedSeen?.ToPokemon();
            if (Received != null) AudioManager.PlaySound("levelup");
        }
        if (Session.Phase == LinkPhase.Closed) IsActive = false;
    }

    private void Refresh()
    {
        Offerable = offerables();
        Cursor = Math.Clamp(Cursor, 0, Math.Max(0, Offerable.Count - 1));
        First = UiNav.Window(First, Cursor, Offerable.Count, VisibleRows);
    }

    /// <summary>Up (−1) and down (+1): the menu's rows, the keypad's rows, the room's list.</summary>
    public void Move(int delta)
    {
        if (delta == 0) return;
        switch (Phase)
        {
            case LinkPhase.Menu:
                MenuRow = (GlobalLinkRow)UiNav.Wrap((int)MenuRow, delta, 4);
                break;
            case LinkPhase.EnterCode:
                Key = UiNav.Wrap(Key, delta * KeypadColumns, KeypadKeys);
                break;
            case LinkPhase.Room:
                if (Offerable.Count == 0) return;
                Cursor = UiNav.Wrap(Cursor, delta, Offerable.Count);
                First = UiNav.Window(First, Cursor, Offerable.Count, VisibleRows);
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Left (−1) and right (+1): along the keypad's row.</summary>
    public void Sideways(int delta)
    {
        if (Phase != LinkPhase.EnterCode || delta == 0) return;
        int row = Key / KeypadColumns, column = UiNav.Wrap(Key % KeypadColumns, Math.Sign(delta), KeypadColumns);
        Key = row * KeypadColumns + column;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The digit a key types, or −1 for ERASE and OK.</summary>
    public static int DigitOf(int key) => key switch { < 9 => key + 1, ZeroKey => 0, _ => -1 };

    /// <summary>The A button.</summary>
    public void Confirm()
    {
        bool done = false;
        switch (Phase)
        {
            case LinkPhase.Menu:
                done = MenuRow switch
                {
                    GlobalLinkRow.MakeCode => Session.MakeCode(),
                    GlobalLinkRow.EnterCode => Session.EnterCode(),
                    GlobalLinkRow.BattleCode => Session.MakeBattleCode(),
                    _ => Close()
                };
                if (MenuRow == GlobalLinkRow.EnterCode) Key = 0;
                break;
            case LinkPhase.EnterCode:
                done = Key switch
                {
                    EraseKey => Session.Erase(),
                    OkKey => Session.SubmitCode(),
                    _ => Session.TypeDigit(DigitOf(Key))
                };
                // A code whole: the cursor goes to OK, as a keyboard's does at its last letter
                if (Session.Typed.Length == OnlineRules.CodeDigits && Phase == LinkPhase.EnterCode) Key = OkKey;
                break;
            case LinkPhase.Room:
                if (Selected is not { } p) break;
                if (SelectedIsOffered) done = Session.ConfirmTrade();
                else done = Session.Offer(SavedPokemonData.FromPokemon(p));
                break;
            case LinkPhase.Traded:
                Session.Dismiss();
                Refresh();
                done = true;
                break;
            case LinkPhase.BattleRoom:
                // The team as it stands is brought, in its order (plan 07 · O6)
                done = Team.Count > 0 && Session.BringTeam(new LinkPlayer(PlayerIdentity.Name, Team.Select(RecordedPokemon.Of).ToList()));
                break;
            case LinkPhase.Battled:
                Session.DismissBattle();
                done = true;
                break;
            case LinkPhase.Failed:
                done = Close();
                break;
        }
        AudioManager.PlaySound(done ? "select" : "error");
    }

    /// <summary>The B button: a step back; the trade called off; out of the room; away from the counter.</summary>
    public void Cancel()
    {
        switch (Phase)
        {
            case LinkPhase.Menu:
            case LinkPhase.Failed:
            case LinkPhase.Connecting:
                Close();
                break;
            case LinkPhase.EnterCode:
                if (!Session.Erase()) Session.Leave();
                break;
            case LinkPhase.Room:
                if (!Session.CancelTrade()) Session.Leave();
                break;
            case LinkPhase.Traded:
                Session.Dismiss();
                Refresh();
                break;
            case LinkPhase.MakingCode:
            case LinkPhase.WaitingForOther:
            case LinkPhase.Joining:
            case LinkPhase.BattleRoom:
                Session.Leave();
                break;
            case LinkPhase.Battled:
                Session.DismissBattle();
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cancel");
    }

    private bool Close()
    {
        if (!Session.MayClose) return false;
        Session.Close();
        IsActive = false;
        return true;
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) Sideways(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) Sideways(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        Advance(dt);
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawGlobalLink(screenWidth, screenHeight, this, Appear);

    /// <summary>The save of a screen never opened: holds nothing.</summary>
    private sealed class NoKeeper : ITradeKeeper
    {
        public bool Holds(Guid id) => false;
        public void Swap(Guid given, SavedPokemonData received) => throw new InvalidOperationException("No save to trade into.");
    }
}
