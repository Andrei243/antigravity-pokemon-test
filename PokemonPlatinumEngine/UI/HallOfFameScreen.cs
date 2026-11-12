using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The PC's HALL OF FAME (plan 06 · R12; the original's <c>pc_hall_of_fame</c>): the teams that entered it, newest
/// first, each under its number and day. Left and right go to an older or a newer team (round from the last to the
/// first), up and down through the team's Pokémon and on into the next team's, and A turns the panel between who a
/// Pokémon is and the moves it knew. Once two regions have teams in it (plan 41 · EG2) a row of tabs sorts them by the
/// region whose Hall of Fame they entered, with EARLIER TEAMS for those from before a team's region was kept, and the
/// shoulders turn the tabs; a plinth of the Laurel Gallery opens it on its region's tab. Its logic takes no input
/// (<see cref="Move"/>, <see cref="Turn"/>, <see cref="ChangeTab"/>), so tests and the harness drive it.
/// </summary>
public class HallOfFameScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;

    /// <summary>The tab of the teams from before a team's region was kept.</summary>
    public const string EarlierTab = "earlier";

    public bool IsActive { get; private set; }

    /// <summary>Which team, counted from the newest (0), and which of its Pokémon.</summary>
    public int Entry { get; private set; }
    public int Member { get; private set; }

    /// <summary>Whether the panel shows the moves instead of who the Pokémon is.</summary>
    public bool ShowingMoves { get; private set; }

    /// <summary>The tab shown: a region's id, <see cref="EarlierTab"/>, or null for every team in one list.</summary>
    public string? Tab { get; private set; }

    // A fixed list in place of the record's (the circuit's finals, plan 41 · EG4)
    private IReadOnlyList<(int Number, HallOfFameEntry Entry)>? fixedTeams;

    /// <summary>Opens on every team, or, with tabs, on the newest team's region.</summary>
    public void Open() => Open(null);

    /// <summary>Opens on a tab (a region's id or <see cref="EarlierTab"/>); null for the newest team's.</summary>
    public void Open(string? tab)
    {
        IsActive = true;
        Entry = 0;
        Member = 0;
        ShowingMoves = false;
        openAge = 0f;
        fixedTeams = null;
        Tab = tab;
        pendingTab = tab == null;
    }

    // The tab is chosen once the record is first seen (Open has none in hand)
    private bool pendingTab;

    /// <summary>Opens on a list of teams of its own, with no tabs (the Laurel Circuit's finals, plan 41 · EG4).</summary>
    public void OpenOn(IReadOnlyList<(int Number, HallOfFameEntry Entry)> teams)
    {
        Open(null);
        pendingTab = false;
        fixedTeams = teams;
    }

    public void Close() => IsActive = false;

    /// <summary>The tabs the record sorts into: the regions with teams in the chain's order, then EARLIER TEAMS; none with fewer than two.</summary>
    public IReadOnlyList<string> Tabs(HallOfFame records)
    {
        Settle(records);
        return TabsOf(records);
    }

    private IReadOnlyList<string> TabsOf(HallOfFame records)
    {
        if (fixedTeams != null) return Array.Empty<string>();
        var tabs = records.Regions(RegionDatabase.All.Select(r => r.Id)).ToList();
        if (records.HasEarlier) tabs.Add(EarlierTab);
        return tabs.Count >= 2 ? tabs : Array.Empty<string>();
    }

    private void Settle(HallOfFame records)
    {
        if (!pendingTab) return;
        pendingTab = false;
        var tabs = TabsOf(records);
        if (tabs.Count == 0) return;
        string newest = records.NewestFirst().Select(t => t.Entry.Region).FirstOrDefault() ?? EarlierTab;
        Tab = tabs.Contains(newest) ? newest : tabs[0];
    }

    /// <summary>The teams of the tab shown, newest first, each with its number.</summary>
    public IReadOnlyList<(int Number, HallOfFameEntry Entry)> Teams(HallOfFame records)
    {
        Settle(records);
        if (fixedTeams != null) return fixedTeams;
        if (Tab == null) return records.NewestFirst().ToList();
        return records.Of(Tab == EarlierTab ? null : Tab);
    }

    /// <summary>The team shown and its number, or null when nobody has entered yet.</summary>
    public (int Number, HallOfFameEntry Entry)? Shown(HallOfFame records)
    {
        var teams = Teams(records);
        return Entry < teams.Count ? teams[Entry] : null;
    }

    /// <summary>The shoulders: the next tab (+1) or the one before (−1), round; nothing without tabs.</summary>
    public void ChangeTab(int d, HallOfFame records)
    {
        Settle(records);
        var tabs = Tabs(records);
        if (tabs.Count == 0 || d == 0) return;
        int at = Tab == null ? 0 : Math.Max(0, tabs.ToList().IndexOf(Tab));
        Tab = tabs[((at + Math.Sign(d)) % tabs.Count + tabs.Count) % tabs.Count];
        Entry = 0;
        Member = 0;
        AudioManager.PlaySound("page");
    }

    /// <summary>Left and right: an older (+1) or newer (−1) team, round from the oldest to the newest; up and down: the next Pokémon, on into the next team.</summary>
    public void Move(int dx, int dy, HallOfFame records)
    {
        int teams = Teams(records).Count;
        if (teams == 0) return;
        if (dx != 0)
        {
            Entry = ((Entry + Math.Sign(dx)) % teams + teams) % teams;
            Member = 0;
        }
        else if (dy != 0)
        {
            int size = Shown(records)!.Value.Entry.Team.Count;
            Member += Math.Sign(dy);
            if (Member >= size)
            {
                Entry = (Entry + 1) % teams;
                Member = 0;
            }
            else if (Member < 0)
            {
                Entry = ((Entry - 1) % teams + teams) % teams;
                Member = Shown(records)!.Value.Entry.Team.Count - 1;
            }
        }
        else return;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A: the panel turns between the Pokémon and its moves.</summary>
    public void Turn()
    {
        ShowingMoves = !ShowingMoves;
        AudioManager.PlaySound("page");
    }

    public void Update(HallOfFame records, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        int dx = (InputManager.IsActionPressed(GameAction.Right) ? -1 : 0) + (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        int tab = (InputManager.IsActionPressed(GameAction.PageRight) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.PageLeft) ? 1 : 0);
        if (tab != 0) ChangeTab(tab, records);
        else if (dx != 0 || dy != 0) Move(dx, dy, records);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Turn();
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
    }

    public void Draw(int screenWidth, int screenHeight, HallOfFame records)
    {
        if (!IsActive) return;
        ModernUi.DrawHallOfFame(screenWidth, screenHeight, this, records, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
