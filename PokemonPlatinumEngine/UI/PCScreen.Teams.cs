using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What can be done with a team kept (plan 12 · Q27).</summary>
public enum PcTeamAction { SwapIn, Delete, Cancel }

/// <summary>
/// Saved teams and the judge (plan 12 · Q27; style guide, "PC boxes", "Teams" and "The judge"): TEAMS on the box's menu
/// opens the teams kept, each swapped in from the PC in one step (<see cref="SavedTeams.SwapIn"/>), and SAVE THIS TEAM keeps
/// the party under a name typed on the keyboard; JUDGE on a Pokémon's menu turns the detail column into the judge's page.
/// Both behind the boxes' tools. The rules are <see cref="SavedTeams"/>' and <see cref="IvJudge"/>'s.
/// </summary>
public partial class PCScreen
{
    /// <summary>The teams kept: the game's own list (set every frame the PC is open), or a test's.</summary>
    public List<SavedTeam> Teams { get; set; } = new();

    /// <summary>What gives the player's Pokémon their ids before a team is kept (the game's <c>GiveIds</c>); none in a test that gives them itself.</summary>
    public Action? BeforeKeeping { get; set; }

    /// <summary>Whether the detail column is the judge's page (JUDGE), until B.</summary>
    public bool Judging { get; set; }

    /// <summary>Whether the teams' panel is open.</summary>
    public bool TeamsOpen { get; private set; }

    /// <summary>The panel's row under the cursor: each team kept, then SAVE THIS TEAM while there is room.</summary>
    public int TeamIndex { get; set; }

    /// <summary>The menu of what to do with the team under the cursor, while it is open.</summary>
    public IReadOnlyList<PcTeamAction>? TeamMenu { get; private set; }

    /// <summary>The name of a team being kept, being typed; null while none is.</summary>
    public NameEntry? TeamNaming { get; private set; }

    /// <summary>The panel's rows: one to a team, and SAVE THIS TEAM while fewer than <see cref="SavedTeams.Most"/> are kept.</summary>
    public int TeamRows => Teams.Count + (Teams.Count < SavedTeams.Most ? 1 : 0);

    /// <summary>Whether the row under the cursor is SAVE THIS TEAM.</summary>
    public bool OnSaveRow => TeamIndex >= Teams.Count;

    private bool TeamsBusy => TeamsOpen || TeamMenu != null || TeamNaming != null;

    private void CloseTeams()
    {
        TeamsOpen = false;
        TeamMenu = null;
        TeamNaming = null;
        Judging = false;
    }

    /// <summary>Opens the teams' panel on its first row.</summary>
    public void OpenTeams()
    {
        TeamsOpen = true;
        TeamIndex = 0;
        TeamMenu = null;
        AudioManager.PlaySound("select");
    }

    private bool MoveTeams(int dy)
    {
        if (TeamNaming != null) return true;
        if (TeamMenu is { } menu)
        {
            if (dy != 0) { MenuIndex = UiNav.Wrap(MenuIndex, dy, menu.Count); AudioManager.PlaySound("cursor"); }
            return true;
        }
        if (!TeamsOpen) return false;
        if (dy != 0 && TeamRows > 0) { TeamIndex = UiNav.Wrap(TeamIndex, Math.Sign(dy), TeamRows); AudioManager.PlaySound("cursor"); }
        return true;
    }

    private bool ConfirmTeams(Party party, PcBoxes pc, Action<string> onNotification)
    {
        if (TeamNaming != null) return true;
        if (TeamMenu is { } menu)
        {
            var action = menu[MenuIndex];
            TeamMenu = null;
            if (action == PcTeamAction.SwapIn) SwapIn(party, pc, onNotification);
            else if (action == PcTeamAction.Delete)
            {
                onNotification(Text.Of("boxTeams.deleted", ("team", Teams[TeamIndex].Name)));
                Teams.RemoveAt(TeamIndex);
                TeamIndex = Math.Clamp(TeamIndex, 0, Math.Max(0, TeamRows - 1));
            }
            AudioManager.PlaySound("select");
            return true;
        }
        if (!TeamsOpen) return false;
        if (OnSaveRow)
        {
            if (party.Count == 0) return true;
            TeamNaming = new NameEntry(DefaultTeamName(), SavedTeams.NameLength);
        }
        else
        {
            TeamMenu = new[] { PcTeamAction.SwapIn, PcTeamAction.Delete, PcTeamAction.Cancel };
            MenuIndex = 0;
        }
        AudioManager.PlaySound("select");
        return true;
    }

    private bool CancelTeams()
    {
        if (TeamNaming != null) TeamNaming = null;
        else if (TeamMenu != null) TeamMenu = null;
        else if (TeamsOpen) TeamsOpen = false;
        else return false;
        return true;
    }

    /// <summary>The name a team starts from on the keyboard: "Team 1", "Team 2"… the first not taken.</summary>
    private string DefaultTeamName()
    {
        for (int n = 1; ; n++)
        {
            string name = Text.Of("boxTeams.defaultName", ("number", n));
            if (!Teams.Any(t => t.Name == name)) return name;
        }
    }

    /// <summary>Keeps the party as a team under the name given (SAVE THIS TEAM's end), its members given their ids first.</summary>
    public void KeepTeam(Party party, string name, Action<string> onNotification)
    {
        TeamNaming = null;
        if (party.Count == 0 || Teams.Count >= SavedTeams.Most) return;
        BeforeKeeping?.Invoke();
        Teams.Add(SavedTeams.Of(name, party));
        TeamIndex = Teams.Count - 1;
        onNotification(Text.Of("boxTeams.kept", ("team", name)));
    }

    /// <summary>Swaps the team under the cursor in (SWAP IN), telling who was skipped or why it couldn't be.</summary>
    public void SwapIn(Party party, PcBoxes pc, Action<string> onNotification)
    {
        var team = Teams[TeamIndex];
        var swap = SavedTeams.SwapIn(team, party, pc, WhyNotCarry, PartyLimit);
        if (!swap.Done)
        {
            AudioManager.PlaySound("error");
            onNotification(Text.Of(swap.Refusal!, swap.Values.ToArray()));
            return;
        }
        Changed = true;
        PartyIndex = 0;
        onNotification(swap.Missing.Count == 0
            ? Text.Of("boxTeams.swapped", ("team", team.Name))
            : Text.Of("boxTeams.swappedSome", ("team", team.Name), ("count", swap.Missing.Count)));
    }

    /// <summary>The keyboard while a team's name is typed; true while it has the screen.</summary>
    private bool UpdateTeamNaming(Party party, Action<string> onNotification)
    {
        if (TeamNaming is not { } naming) return false;
        int nx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int ny = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (nx != 0 || ny != 0) naming.Move(nx, ny);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) { if (!naming.Backspace()) TeamNaming = null; }
        else if (InputManager.IsActionPressed(GameAction.Menu)) naming.ToDone();
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            naming.Press();
            if (naming.Done) KeepTeam(party, naming.Result(DefaultTeamName()), onNotification);
        }
        return true;
    }


    /// <summary>Ends typing a team's name as the keyboard's DONE does: a tool's or a test's way through it.</summary>
    public void FinishTeamNaming(Party party, string? name, Action<string> notify)
    {
        if (TeamNaming is not { } naming) return;
        if (name == null) { TeamNaming = null; return; }
        KeepTeam(party, PlayerIdentity.Clean(name, SavedTeams.NameLength) is { Length: > 0 } clean ? clean : naming.Result(DefaultTeamName()), notify);
    }
}
