using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What can be done with a Pokémon of the team from the field's menu.</summary>
public enum PartyActionKind { Summary, FieldMove, Switch, Cancel }

/// <summary>One entry of a Pokémon's menu: its summary, one of its field moves, moving it, or backing out.</summary>
public readonly record struct PartyAction(PartyActionKind Kind, FieldMove Move = default)
{
    public string Label => Kind switch
    {
        PartyActionKind.Summary => "SUMMARY",
        PartyActionKind.Switch => "SWITCH",
        PartyActionKind.Cancel => "CANCEL",
        _ => FieldMoveRules.MoveName(Move).ToUpperInvariant()
    };
}

/// <summary>
/// The Pokémon menu: the team as two columns of cards, reordering (Shift picks a Pokémon up, A puts it down),
/// and the summary of the chosen Pokémon, where up and down step through the team. Choosing a Pokémon opens its
/// menu, as the original's does (plan 02 · S2): its summary, the moves it knows that are used in the field, and
/// moving it. A field move chosen is handed to the game (<see cref="TakeFieldMove"/>), which checks it and either
/// says why it can't be used (<see cref="Message"/>) or closes the menu and uses it; Milk Drink and Soft-Boiled are
/// used here, on a Pokémon chosen next.
/// </summary>
public class PartyScreen
{
    private const float AppearTime = 0.45f, SummaryAppearTime = 0.25f;

    private float openAge, summaryAge;

    public int SelectedIndex { get; set; } = 0;
    public int? SwapSourceIndex { get; set; } = null;
    public bool ShowSummary { get; set; } = false;
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Whether the summary has a page for the contest condition (plan 06 · R14c): as in the original, once the
    /// Contest Hall has been visited (<c>PokemonSummaryScreen_ShowContestData</c>). The game sets it as it opens the menu.
    /// </summary>
    public bool ShowCondition { get; set; }

    /// <summary>The summary's page: 0 the Pokémon, its stats and moves; 1 its condition; 2 its ribbons (plan 06 · R17b).</summary>
    public int SummaryPage { get; private set; }

    public const int StatsPage = 0, ConditionPage = 1, RibbonsPage = 2;

    /// <summary>How many ribbons a row of the ribbons' page shows, and how many rows (the original shows four by three).</summary>
    public const int RibbonColumns = 6, RibbonRows = 3;

    /// <summary>
    /// The ribbon under the cursor while the ribbons' page is looked through (the original's ribbon select, A on the
    /// page), by its place among the Pokémon's own; -1 while the page only shows them.
    /// </summary>
    public int RibbonCursor { get; private set; } = -1;

    /// <summary>The pages a summary turns through: the condition's only once the Contest Hall has been visited.</summary>
    public IReadOnlyList<int> SummaryPages => ShowCondition ? new[] { StatsPage, ConditionPage, RibbonsPage } : new[] { StatsPage, RibbonsPage };

    /// <summary>The chosen Pokémon's menu while it is open, and the entry under its cursor.</summary>
    public IReadOnlyList<PartyAction>? Actions { get; private set; }
    public int ActionIndex { get; private set; }

    /// <summary>While Milk Drink or Soft-Boiled waits for the Pokémon to give HP to: the place of the one giving it, and the move.</summary>
    public (int Giver, FieldMove Move)? Sharing { get; private set; }

    /// <summary>A line in place of the prompt (why a move can't be used, what was healed), until the next button.</summary>
    public string? Message { get; set; }

    private (FieldMove Move, int Index)? chosenMove;

    /// <summary>
    /// Open for a script to have a Pokémon of the team chosen (plan 06 · R12: the one to trade): A chooses the one
    /// under the cursor and closes, B closes with none. <see cref="Chosen"/> is the place chosen, or
    /// <see cref="NoneChosen"/>.
    /// </summary>
    public bool Choosing { get; private set; }
    public int Chosen { get; private set; } = NoneChosen;
    public const int NoneChosen = 255;

    /// <summary>
    /// Open for several Pokémon to be entered in order (plan 06 · R18a: the three, four or two of a Battle Tower
    /// challenge): how many are wanted, or 0 while the screen is open for anything else. A enters the Pokémon under the
    /// cursor, or takes it out again; with all entered, A goes in with them; B takes the last one back, and with none
    /// entered closes the screen. <see cref="EnteredAll"/> says whether it closed with the full count.
    /// </summary>
    public int EnterCount { get; private set; }
    public List<int> Entered { get; } = new();
    public bool EnteredAll { get; private set; }
    public bool Entering => EnterCount > 0;
    private Func<Pokemon, bool> mayEnter = _ => true;

    /// <summary>Opens the team for <paramref name="count"/> Pokémon to be entered; <paramref name="allowed"/> says which may be.</summary>
    public void OpenToEnter(int count, Func<Pokemon, bool> allowed)
    {
        Open();
        EnterCount = count;
        Entered.Clear();
        EnteredAll = false;
        mayEnter = allowed;
    }

    /// <summary>What a Pokémon's card says while the team is entered: its place among those entered, or that it can't go in.</summary>
    public string? EnterTag(Party party, Pokemon pokemon)
    {
        int place = Entered.IndexOf(party.Members.IndexOf(pokemon));
        if (place >= 0) return place switch { 0 => "1ST", 1 => "2ND", 2 => "3RD", _ => "4TH" };
        return mayEnter(pokemon) ? null : "NOT ABLE";
    }

    /// <summary>
    /// Opens the team for a Pokémon to be chosen (<see cref="Choosing"/>). With <paramref name="allowed"/> (plan 14 ·
    /// B14: <c>choosepokemon "Bidoof"</c>) only those it lets may be: the rest are greyed and tagged NOT ABLE, and A on
    /// one says <paramref name="refusal"/> in the prompt and chooses nothing.
    /// </summary>
    public void OpenToChoose(Func<Pokemon, bool>? allowed = null, string? refusal = null)
    {
        Open();
        Choosing = true;
        Chosen = NoneChosen;
        mayChoose = allowed;
        chooseRefusal = refusal;
    }

    private Func<Pokemon, bool>? mayChoose;
    private string? chooseRefusal;

    /// <summary>Whether only some of the team may be chosen (a species named on <c>choosepokemon</c>).</summary>
    public bool ChoosingSome => Choosing && mayChoose != null;

    /// <summary>What a Pokémon's card says while only some may be chosen: NOT ABLE for one that may not, else nothing.</summary>
    public string? ChooseTag(Pokemon pokemon) => mayChoose == null || mayChoose(pokemon) ? null : "NOT ABLE";

    public void Open()
    {
        IsActive = true;
        Choosing = false;
        EnterCount = 0;
        SelectedIndex = 0;
        SwapSourceIndex = null;
        ShowSummary = false;
        Actions = null;
        Sharing = null;
        Message = null;
        chosenMove = null;
        openAge = 0f;
    }

    public void Close()
    {
        IsActive = false;
        SwapSourceIndex = null;
        ShowSummary = false;
        Actions = null;
        Sharing = null;
    }

    /// <summary>
    /// A Pokémon's menu in the field: its summary, then its field moves in the order of its moves (Chatter left
    /// out), then moving it and backing out, as the original's (<c>GetContextMenuEntriesForPartyMon</c>).
    /// </summary>
    public static List<PartyAction> ActionsFor(Pokemon pokemon)
    {
        var actions = new List<PartyAction> { new(PartyActionKind.Summary) };
        // An Egg knows no field moves it could use (plan 06 · R15)
        if (!pokemon.IsEgg) actions.AddRange(FieldMoveRules.Known(pokemon).Distinct().Select(m => new PartyAction(PartyActionKind.FieldMove, m)));
        actions.Add(new PartyAction(PartyActionKind.Switch));
        actions.Add(new PartyAction(PartyActionKind.Cancel));
        return actions;
    }

    /// <summary>The field move chosen to be used, once: the move and the place of the Pokémon that uses it.</summary>
    public (FieldMove Move, int Index)? TakeFieldMove()
    {
        var chosen = chosenMove;
        chosenMove = null;
        return chosen;
    }

    /// <summary>The prompt under the team: the message if there is one, or what the player is choosing.</summary>
    public string Prompt => Message ?? (Sharing != null ? "Give HP to which Pokémon?"
        : SwapSourceIndex.HasValue ? "Move to where?"
        : Actions != null ? "Do what with this Pokémon?"
        : Entering ? Entered.Count == EnterCount ? "Go in with these Pokémon?" : $"Choose {EnterCount} Pokémon to enter. ({EnterCount - Entered.Count} to go)"
        : Choosing ? "Choose which Pokémon?"
        : "Choose a Pokémon.");

    /// <summary>The confirm button: opens a Pokémon's menu, carries out the entry chosen in it, puts a Pokémon down, or gives HP.</summary>
    public void Confirm(Party party)
    {
        Message = null;
        if (SelectedIndex >= party.Count) return;
        if (Entering)
        {
            if (Entered.Count == EnterCount)
            {
                EnteredAll = true;
                AudioManager.PlaySound("select");
                Close();
            }
            else if (Entered.Remove(SelectedIndex)) AudioManager.PlaySound("cancel");
            else if (!mayEnter(party.Members[SelectedIndex]))
            {
                Message = party.Members[SelectedIndex].IsEgg ? "An Egg can't take part." : $"{party.Members[SelectedIndex].DisplayName} can't take part.";
                AudioManager.PlaySound("error");
            }
            else
            {
                Entered.Add(SelectedIndex);
                AudioManager.PlaySound("select");
            }
            return;
        }
        if (Choosing)
        {
            if (mayChoose != null && !mayChoose(party.Members[SelectedIndex]))
            {
                Message = chooseRefusal ?? (party.Members[SelectedIndex].IsEgg ? "An Egg can't be chosen." : $"{party.Members[SelectedIndex].DisplayName} can't be chosen.");
                AudioManager.PlaySound("error");
                return;
            }
            Chosen = SelectedIndex;
            AudioManager.PlaySound("select");
            Close();
            return;
        }
        if (Sharing is var (giver, _))
        {
            int healed = FieldMoveRules.ShareHp(party.Members[giver], party.Members[SelectedIndex]);
            if (healed == 0)
            {
                Message = "That can't be done for that Pokémon.";
                AudioManager.PlaySound("error");
                return;
            }
            AudioManager.PlaySound("heal");
            Message = $"{party.Members[SelectedIndex].Nickname} recovered {healed} HP.";
            Sharing = null;
            SelectedIndex = giver;
            return;
        }
        if (SwapSourceIndex.HasValue)
        {
            party.Swap(SwapSourceIndex.Value, SelectedIndex);
            SwapSourceIndex = null;
            AudioManager.PlaySound("select");
            return;
        }
        if (Actions == null)
        {
            Actions = ActionsFor(party.Members[SelectedIndex]);
            ActionIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }

        var action = Actions[ActionIndex];
        Actions = null;
        AudioManager.PlaySound("select");
        switch (action.Kind)
        {
            case PartyActionKind.Summary:
                ShowSummary = true;
                SummaryPage = 0;
                RibbonCursor = -1;
                summaryAge = 0f;
                if (!party.Members[SelectedIndex].IsEgg) AudioManager.PlayCry(party.Members[SelectedIndex]);
                break;
            case PartyActionKind.Switch:
                SwapSourceIndex = SelectedIndex;
                break;
            case PartyActionKind.FieldMove when action.Move is FieldMove.MilkDrink or FieldMove.Softboiled:
                // A fifth of its most HP for another of the team (PartyMenu_StartFieldMoveHPTransfer)
                if (!FieldMoveRules.CanShareHp(party.Members[SelectedIndex])) Message = "It hasn't enough HP to share.";
                else Sharing = (SelectedIndex, action.Move);
                break;
            case PartyActionKind.FieldMove:
                chosenMove = (action.Move, SelectedIndex);
                break;
        }
    }

    /// <summary>The cancel button: backs out of whatever is open, one step at a time.</summary>
    public void Cancel()
    {
        Message = null;
        AudioManager.PlaySound("cancel");
        if (Entering && Entered.Count > 0)
        {
            Entered.RemoveAt(Entered.Count - 1);
            return;
        }
        if (Sharing is var (giver, _))
        {
            Sharing = null;
            SelectedIndex = giver;
        }
        else if (Actions != null) Actions = null;
        else if (SwapSourceIndex.HasValue) SwapSourceIndex = null;
        else Close();
    }

    /// <summary>Moves the cursor of the open menu up or down.</summary>
    public void MoveAction(int dy)
    {
        if (Actions == null || dy == 0) return;
        ActionIndex = ((ActionIndex + dy) % Actions.Count + Actions.Count) % Actions.Count;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Turns the summary to its next page or the one before, round and round (<see cref="SummaryPages"/>).</summary>
    public void TurnSummaryPage(int dx)
    {
        if (!ShowSummary || dx == 0) return;
        var pages = SummaryPages;
        int at = Math.Max(0, pages.ToList().IndexOf(SummaryPage));
        SummaryPage = pages[UiNav.Wrap(at, Math.Sign(dx), pages.Count)];
        RibbonCursor = -1;
        AudioManager.PlaySound("page");
    }

    /// <summary>
    /// The confirm button on the ribbons' page (the original's A): the cursor comes up on the Pokémon's first ribbon,
    /// and its name and what it is for are shown. False when the Pokémon has none, or another page is open.
    /// </summary>
    public bool SelectRibbons(Pokemon shown)
    {
        if (!ShowSummary || SummaryPage != RibbonsPage || shown.IsEgg || shown.Ribbons.Count == 0) return false;
        RibbonCursor = 0;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>Stops looking through the ribbons (the original's B).</summary>
    public void LeaveRibbons()
    {
        RibbonCursor = -1;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Moves the ribbons' cursor over their rows of <see cref="RibbonColumns"/>, round at the edges.</summary>
    public void MoveRibbon(int dx, int dy, int count)
    {
        if (RibbonCursor < 0 || count <= 0 || (dx == 0 && dy == 0)) return;
        int next = UiNav.Grid(Math.Min(RibbonCursor, count - 1), count, RibbonColumns, dx, dy);
        if (next == RibbonCursor) return;
        RibbonCursor = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Moves the cursor over the two-column list, or to the next Pokémon while the summary is open.</summary>
    public void MoveCursor(int dx, int dy, int count)
    {
        if (count <= 0 || (dx == 0 && dy == 0)) return;
        int next = ShowSummary
            ? ((SelectedIndex + (dy != 0 ? dy : dx)) % count + count) % count
            : UiNav.Grid(SelectedIndex, count, 2, dx, dy);
        if (next == SelectedIndex) return;
        SelectedIndex = next;
        RibbonCursor = -1;
        AudioManager.PlaySound("cursor");
    }

    public void Update(Party party, float dt)
    {
        if (!IsActive) return;
        openAge += dt;
        summaryAge += dt;

        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);

        if (ShowSummary)
        {
            var shown = SelectedIndex < party.Count ? party.Members[SelectedIndex] : null;
            // Looking through the ribbons: the arrows move over them, and either button stops (plan 06 · R17b)
            if (RibbonCursor >= 0 && shown != null)
            {
                if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm)) LeaveRibbons();
                else MoveRibbon(dx, dy, shown.Ribbons.Count);
                return;
            }
            if (InputManager.IsActionPressed(GameAction.Confirm) && shown != null && SelectRibbons(shown)) return;
            if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm))
            {
                ShowSummary = false;
                AudioManager.PlaySound("cancel");
            }
            // Left and right turn the page (an Egg's summary has the one), and up and down go through the team
            else if (dx != 0 && shown is { IsEgg: false }) TurnSummaryPage(dx);
            else MoveCursor(0, dy, party.Count);
            return;
        }

        if (Actions != null && dy != 0)
        {
            MoveAction(dy);
        }
        else if (Actions == null && (dx != 0 || dy != 0))
        {
            Message = null;
            MoveCursor(dx, dy, party.Count);
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Cancel();
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            Confirm(party);
        }
        else if (InputManager.IsActionPressed(GameAction.Run) && !SwapSourceIndex.HasValue && Actions == null && Sharing == null && !Entering && !Choosing)
        {
            SwapSourceIndex = SelectedIndex;
            AudioManager.PlaySound("select");
        }
    }

    public void Draw(int screenWidth, int screenHeight, Party party)
    {
        if (!IsActive) return;

        if (ShowSummary && SelectedIndex < party.Count)
        {
            ModernUi.DrawSummary(screenWidth, screenHeight, party.Members[SelectedIndex], SelectedIndex, party.Count,
                Math.Clamp(summaryAge / SummaryAppearTime, 0f, 1f), SummaryPage, RibbonCursor);
        }
        else if (Entering)
        {
            ModernUi.DrawPartyChoice(screenWidth, screenHeight, party, SelectedIndex, Prompt, p => EnterTag(party, p), Math.Clamp(openAge / AppearTime, 0f, 1f),
                Entered.Count == EnterCount ? "Go in" : "Enter");
        }
        else if (ChoosingSome)
        {
            // Only some may be chosen (plan 14 · B14): the rest greyed under NOT ABLE
            ModernUi.DrawPartyChoice(screenWidth, screenHeight, party, SelectedIndex, Prompt, ChooseTag, Math.Clamp(openAge / AppearTime, 0f, 1f),
                "Choose", greyRefused: true);
        }
        else
        {
            ModernUi.DrawParty(screenWidth, screenHeight, party, SelectedIndex, SwapSourceIndex, Math.Clamp(openAge / AppearTime, 0f, 1f),
                Prompt, Actions?.Select(a => a.Label).ToList(), ActionIndex, Sharing?.Giver);
        }
    }
}
