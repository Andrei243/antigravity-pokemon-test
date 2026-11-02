using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The team builder's pages: the teams, one team's six, one member's choices, a list to pick from, and the keyboard for a name.</summary>
public enum BuilderPage { Teams, Team, Member, Pick, Name }

/// <summary>What a member's row is.</summary>
public enum MemberRow { Species, Form, Level, Nature, Ability, Item, Move1, Move2, Move3, Move4, Remove }

/// <summary>What is being picked from a list.</summary>
public enum PickKind { Species, Item, Move }

/// <summary>One choice of a list to pick from: what it stands for (the data's name; null for none) and what it reads.</summary>
public sealed record PickOption(string? Value, string Label);

/// <summary>
/// The battle room's team builder (plan 13 · V13; style guide, "Menu screens", "The battle room"): the teams kept in
/// <c>teams.json</c>, each of one to six Pokémon built from nothing, with their species from the Pokédex's list, form,
/// level, nature, ability, item and moves from what the data lets them know. A member that can't battle says why in its
/// own words, by the same check the gauntlet holds a team to (<see cref="BuiltTeams.WhyNot(BuiltPokemon)"/>). Its logic
/// takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Jump"/>), so tests and
/// the harness drive it; the file is written by the engine once the screen closes (<see cref="Changed"/>).
/// </summary>
public sealed class TeamBuilderScreen
{
    private const float AppearTime = 0.3f;
    private readonly HeldKey held = new();
    private float age;

    public bool IsActive { get; private set; }
    public float Appear => Math.Clamp(age / AppearTime, 0f, 1f);

    public BuiltTeamsFile File { get; private set; } = new();

    /// <summary>The file was changed since it was opened.</summary>
    public bool Changed { get; private set; }

    public BuilderPage Page { get; private set; } = BuilderPage.Teams;

    /// <summary>The cursor of the page shown, and the first row its list shows.</summary>
    public int Cursor { get; private set; }
    public int FirstRow { get; private set; }

    /// <summary>The team and the member being looked at.</summary>
    public int TeamIndex { get; private set; }
    public int MemberIndex { get; private set; }

    /// <summary>The list being picked from, and what for.</summary>
    public IReadOnlyList<PickOption> Options { get; private set; } = Array.Empty<PickOption>();
    public PickKind Picking { get; private set; }

    /// <summary>The move slot a move is picked for, 0 to 3.</summary>
    public int PickingMove { get; private set; }

    /// <summary>The keyboard while a team is named.</summary>
    public NameEntry? Entry { get; private set; }

    /// <summary>DELETE TEAM was pressed once and waits for a second press.</summary>
    public bool DeleteArmed { get; private set; }

    // Where each page's cursor was when another page opened over it
    private int teamsCursor, teamCursor, memberCursor;

    /// <summary>How many rows a list page shows.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    public BuiltTeam? Team => TeamIndex >= 0 && TeamIndex < File.Teams.Count ? File.Teams[TeamIndex] : null;
    public BuiltPokemon? Member => Team is { } t && MemberIndex >= 0 && MemberIndex < t.Members.Count ? t.Members[MemberIndex] : null;

    public void Open(BuiltTeamsFile file)
    {
        File = file;
        Changed = false;
        Page = BuilderPage.Teams;
        Cursor = FirstRow = 0;
        DeleteArmed = false;
        age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close() => IsActive = false;

    // ------------------------------------------------------------------ the rows of each page

    /// <summary>The teams page's rows: each team, then NEW TEAM while there is room for one.</summary>
    public int TeamsRows => File.Teams.Count + (File.Teams.Count < BuiltTeams.Most ? 1 : 0);

    /// <summary>The team page's rows: its members, ADD while it has fewer than six, RENAME and DELETE TEAM.</summary>
    public int TeamRows => (Team?.Members.Count ?? 0) + (CanAdd ? 1 : 0) + 2;

    public bool CanAdd => Team is { } t && t.Members.Count < Party.MaxSize;

    /// <summary>What the team page's row at <paramref name="row"/> is: a member's index, or −1 for ADD, −2 RENAME, −3 DELETE.</summary>
    public int TeamRowAt(int row)
    {
        int members = Team?.Members.Count ?? 0;
        if (row < members) return row;
        int extra = row - members;
        if (CanAdd) { if (extra == 0) return -1; extra--; }
        return extra == 0 ? -2 : -3;
    }

    public static readonly MemberRow[] MemberRows = Enum.GetValues<MemberRow>();

    public int RowsOfPage => Page switch
    {
        BuilderPage.Teams => TeamsRows,
        BuilderPage.Team => TeamRows,
        BuilderPage.Member => MemberRows.Length,
        BuilderPage.Pick => Options.Count,
        _ => 0
    };

    // ------------------------------------------------------------------ moving

    /// <summary>The cursor up or down; on a member's page left and right change the row's value.</summary>
    public void Move(int dx, int dy)
    {
        if (Page == BuilderPage.Name)
        {
            Entry?.Move(dx, dy);
            return;
        }
        if (dy != 0 && RowsOfPage > 0)
        {
            int next = held.Repeating ? Math.Clamp(Cursor + dy, 0, RowsOfPage - 1) : UiNav.Wrap(Cursor, dy, RowsOfPage);
            if (next != Cursor)
            {
                Cursor = next;
                FirstRow = UiNav.Window(FirstRow, Cursor, RowsOfPage, VisibleRows);
                DeleteArmed = false;
                AudioManager.PlaySound("cursor");
            }
        }
        if (dx != 0 && Page == BuilderPage.Member) Step(dx);
    }

    /// <summary>The shoulders: ten rows at a time in a list, ten levels at a time on the level's row.</summary>
    public void Jump(int dir)
    {
        if (Page == BuilderPage.Member && MemberRows[Cursor] == MemberRow.Level) { Step(dir * 10); return; }
        if (Page != BuilderPage.Pick || Options.Count == 0) return;
        Cursor = Math.Clamp(Cursor + dir * 10, 0, Options.Count - 1);
        FirstRow = UiNav.Window(FirstRow, Cursor, Options.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    // Left and right on a member's row: the level, the nature, the ability and the form in turn
    private void Step(int by)
    {
        if (Member is not { } m) return;
        switch (MemberRows[Cursor])
        {
            case MemberRow.Level:
                m.Level = Math.Clamp(m.Level + by, 1, BuiltTeams.HighestLevel);
                break;
            case MemberRow.Nature:
                m.Nature = (Nature)UiNav.Wrap((int)m.Nature, Math.Sign(by), Enum.GetValues<Nature>().Length);
                break;
            case MemberRow.Ability:
            {
                var abilities = BuiltTeams.AbilitiesOf(m);
                if (abilities.Count == 0) return;
                int at = m.Ability == null ? 0 : Math.Max(0, abilities.ToList().IndexOf(m.Ability));
                m.Ability = abilities[UiNav.Wrap(at, Math.Sign(by), abilities.Count)];
                break;
            }
            case MemberRow.Form:
            {
                if (PokemonDatabase.Get(m.Species) is not { } species) return;
                var forms = new List<string?> { null };
                forms.AddRange(BuiltTeams.FormsOf(species).Select(f => (string?)f.Name));
                if (forms.Count == 1) return;
                int at = Math.Max(0, forms.IndexOf(m.Form));
                m.Form = forms[UiNav.Wrap(at, Math.Sign(by), forms.Count)];
                // A form has abilities of its own: the first of them, if the one held isn't
                if (m.Ability != null && !BuiltTeams.AbilitiesOf(m).Contains(m.Ability)) m.Ability = BuiltTeams.AbilitiesOf(m).FirstOrDefault();
                break;
            }
            default:
                return;
        }
        Changed = true;
        AudioManager.PlaySound("cursor");
    }

    // ------------------------------------------------------------------ confirming

    public void Confirm()
    {
        switch (Page)
        {
            case BuilderPage.Teams:
                if (Cursor < File.Teams.Count) OpenTeam(Cursor);
                else NewTeam();
                break;
            case BuilderPage.Team:
                ConfirmOnTeam();
                break;
            case BuilderPage.Member:
                ConfirmOnMember();
                break;
            case BuilderPage.Pick:
                TakePick();
                break;
            case BuilderPage.Name:
                if (Entry == null) return;
                if (Entry.Press() == NameKey.Done || Entry.Done)
                {
                    if (Team is { } named) named.Name = Entry.Result(named.Name);
                    Changed = true;
                    Entry = null;
                    Show(BuilderPage.Team, teamCursor);
                    AudioManager.PlaySound("select");
                }
                break;
        }
    }

    private void NewTeam()
    {
        if (File.Teams.Count >= BuiltTeams.Most) return;
        int n = File.Teams.Count + 1;
        while (File.Teams.Any(t => t.Name == Text.Of("battleRoom.builder.defaultName", ("n", n)))) n++;
        File.Teams.Add(new BuiltTeam { Name = Text.Of("battleRoom.builder.defaultName", ("n", n)) });
        Changed = true;
        OpenTeam(File.Teams.Count - 1);
    }

    private void OpenTeam(int index)
    {
        teamsCursor = Cursor;
        TeamIndex = index;
        AudioManager.PlaySound("select");
        Show(BuilderPage.Team, 0);
    }

    private void ConfirmOnTeam()
    {
        if (Team is not { } team) return;
        int what = TeamRowAt(Cursor);
        teamCursor = Cursor;
        if (what >= 0)
        {
            MemberIndex = what;
            AudioManager.PlaySound("select");
            Show(BuilderPage.Member, 0);
        }
        else if (what == -1)
        {
            MemberIndex = team.Members.Count;
            OpenPick(PickKind.Species);
        }
        else if (what == -2)
        {
            Entry = new NameEntry(team.Name, BuiltTeams.NameLength);
            AudioManager.PlaySound("select");
            Page = BuilderPage.Name;
        }
        else if (!DeleteArmed)
        {
            DeleteArmed = true;
            AudioManager.PlaySound("error");
        }
        else
        {
            File.Teams.RemoveAt(TeamIndex);
            Changed = true;
            DeleteArmed = false;
            AudioManager.PlaySound("select");
            Show(BuilderPage.Teams, Math.Min(teamsCursor, Math.Max(0, TeamsRows - 1)));
        }
    }

    private void ConfirmOnMember()
    {
        if (Member is not { } m || Team is not { } team) return;
        memberCursor = Cursor;
        switch (MemberRows[Cursor])
        {
            case MemberRow.Species: OpenPick(PickKind.Species); break;
            case MemberRow.Item: OpenPick(PickKind.Item); break;
            case MemberRow.Move1 or MemberRow.Move2 or MemberRow.Move3 or MemberRow.Move4:
                PickingMove = MemberRows[Cursor] - MemberRow.Move1;
                OpenPick(PickKind.Move);
                break;
            case MemberRow.Remove:
                team.Members.RemoveAt(MemberIndex);
                Changed = true;
                AudioManager.PlaySound("select");
                Show(BuilderPage.Team, Math.Min(teamCursor, TeamRows - 1));
                break;
            default:
                Step(1);
                break;
        }
    }

    /// <summary>Opens a list to pick from, with the cursor on what is chosen now.</summary>
    public void OpenPick(PickKind kind)
    {
        Picking = kind;
        var m = Member;
        Options = kind switch
        {
            PickKind.Species => BuiltTeams.Species().Select(s => new PickOption(s.Name, $"{s.DexNumber:D4}  {s.DisplayName}")).ToList(),
            PickKind.Item => new[] { new PickOption(null, Text.Of("battleRoom.builder.none")) }
                .Concat(BuiltTeams.HoldableItems().Select(i => new PickOption(i, LocalNames.Item(i)))).ToList(),
            _ => new[] { new PickOption(null, Text.Of("battleRoom.builder.noMove")) }
                .Concat(m == null ? Enumerable.Empty<PickOption>() : BuiltTeams.MovesOf(m).Select(mv => new PickOption(mv, LocalNames.Move(mv)))).ToList()
        };
        string? now = kind switch
        {
            PickKind.Species => m?.Species,
            PickKind.Item => m?.Item,
            _ => m != null && PickingMove < m.Moves.Count ? m.Moves[PickingMove] : null
        };
        int at = Options.ToList().FindIndex(o => o.Value == now);
        AudioManager.PlaySound("select");
        Show(BuilderPage.Pick, Math.Max(0, at));
    }

    private void TakePick()
    {
        if (Cursor < 0 || Cursor >= Options.Count || Team is not { } team) return;
        string? value = Options[Cursor].Value;
        switch (Picking)
        {
            case PickKind.Species when value != null:
                if (MemberIndex >= team.Members.Count) team.Members.Add(BuiltTeams.Fresh(value));
                else if (team.Members[MemberIndex].Species != value)
                {
                    // Another species keeps the level and nature and starts afresh in everything that hangs on the species
                    var old = team.Members[MemberIndex];
                    var fresh = BuiltTeams.Fresh(value);
                    fresh.Level = old.Level;
                    fresh.Nature = old.Nature;
                    fresh.Item = old.Item;
                    team.Members[MemberIndex] = fresh;
                }
                break;
            case PickKind.Item:
                if (Member is { } holder) holder.Item = value;
                break;
            case PickKind.Move:
                if (Member is not { } m) break;
                if (value == null)
                {
                    // A move taken off, but never the last
                    if (PickingMove < m.Moves.Count && m.Moves.Count > 1) m.Moves.RemoveAt(PickingMove);
                }
                else if (!m.Moves.Contains(value))
                {
                    if (PickingMove < m.Moves.Count) m.Moves[PickingMove] = value;
                    else m.Moves.Add(value);
                }
                break;
        }
        Changed = true;
        AudioManager.PlaySound("select");
        Show(BuilderPage.Member, Picking == PickKind.Species ? 0 : memberCursor);
    }

    public void Cancel()
    {
        AudioManager.PlaySound("cancel");
        switch (Page)
        {
            case BuilderPage.Teams: IsActive = false; break;
            case BuilderPage.Team: DeleteArmed = false; Show(BuilderPage.Teams, teamsCursor); break;
            case BuilderPage.Member: Show(BuilderPage.Team, teamCursor); break;
            case BuilderPage.Pick:
                // A species never picked leaves no member behind
                if (Team is { } t && MemberIndex >= t.Members.Count) Show(BuilderPage.Team, teamCursor);
                else Show(BuilderPage.Member, memberCursor);
                break;
            case BuilderPage.Name:
                if (Entry != null && Entry.Backspace()) return;
                Entry = null;
                Show(BuilderPage.Team, teamCursor);
                break;
        }
    }

    private void Show(BuilderPage page, int cursor)
    {
        Page = page;
        Cursor = Math.Clamp(cursor, 0, Math.Max(0, RowsOfPage - 1));
        FirstRow = UiNav.Window(0, Cursor, RowsOfPage, VisibleRows);
        held.Release();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        int dy = held.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (dy != 0 || dx != 0)
        {
            for (int i = 0; i < Math.Max(1, Math.Abs(dy)); i++) Move(i == 0 ? dx : 0, Math.Sign(dy));
        }
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) Jump(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) Jump(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (Page == BuilderPage.Name && InputManager.IsActionPressed(GameAction.Menu)) Entry?.ToDone();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawTeamBuilder(screenWidth, screenHeight, this, Appear);
}
