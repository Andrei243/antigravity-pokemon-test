using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where a friends' page asks for the clipboard: the game's is raylib's, a test's its own.</summary>
public interface IFriendsHost
{
    string? ReadClipboard();
    void WriteClipboard(string text);
}

/// <summary>What a row of the friends' page is.</summary>
public enum FriendsRowKind { Trainer, ReadCode, PasteTeam, Export }

/// <summary>A row of the page: its kind and the file's or team's place in its list.</summary>
public readonly record struct FriendsRow(FriendsRowKind Kind, int Index);

/// <summary>
/// The friends' page of the battle room (plan 13 · V22; style guide, "Menu screens", "The friends' page"): the trainer files
/// of <c>trainers/</c> in the order of their names, each shown with its team or refused with its reason, READ A CODE and
/// PASTE A TEAM from the clipboard, and the built teams to send (EXPORT: a trainer file written to the folder and its
/// code put on the clipboard and shown in a panel). Everything that comes in is untrusted: it is read whole or refused
/// (<see cref="TrainerFile"/>, <see cref="ShowdownPaste"/>) and nothing of its own is shown but a name. A fight with a trainer
/// is handed over (<see cref="Chosen"/>); the teams changed are the engine's to write (<see cref="Changed"/>). Its logic takes
/// no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it.
/// </summary>
public sealed class FriendsScreen
{
    private const float AppearTime = 0.3f;
    private float age;
    private string folder = "";
    private IFriendsHost? host;

    public bool IsActive { get; private set; }

    /// <summary>B: the page is left for the room.</summary>
    public bool Left { get; private set; }

    /// <summary>A trainer was chosen to fight; handed over once (<see cref="TakeChosen"/>).</summary>
    public TrainerFile? Chosen { get; private set; }

    public float Appear => Math.Clamp(age / AppearTime, 0f, 1f);

    public BuiltTeamsFile Teams { get; private set; } = new();

    /// <summary>The teams were changed (a paste added one) since the page was opened.</summary>
    public bool Changed { get; private set; }

    public IReadOnlyList<TrainerFiles.Entry> Files { get; private set; } = Array.Empty<TrainerFiles.Entry>();
    public IReadOnlyList<FriendsRow> Rows { get; private set; } = Array.Empty<FriendsRow>();

    public int Cursor { get; private set; }
    public int FirstRow { get; private set; }

    /// <summary>What the last thing done came to: the key of its words, what they name, and whether it went well.</summary>
    public (string Key, string Value, bool Good)? Notice { get; private set; }

    /// <summary>The code of the team last sent, shown in the panel.</summary>
    public string? Code { get; private set; }

    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    public void Open(BuiltTeamsFile teams, string trainersFolder, IFriendsHost clipboard)
    {
        Teams = teams;
        folder = trainersFolder;
        host = clipboard;
        Changed = false;
        Left = false;
        Chosen = null;
        Notice = null;
        Code = null;
        Refresh();
        Cursor = FirstRow = 0;
        age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>The page again after a fight, the teams as they stand.</summary>
    public void Resume()
    {
        IsActive = true;
        Chosen = null;
        age = AppearTime;
        Refresh();
    }

    public void Close() => IsActive = false;

    public TrainerFile? TakeChosen()
    {
        var chosen = Chosen;
        Chosen = null;
        return chosen;
    }

    // The folder read again, and the rows laid out: files, the two readers, the teams to send
    private void Refresh()
    {
        Files = TrainerFiles.In(folder);
        var rows = new List<FriendsRow>();
        for (int i = 0; i < Files.Count; i++) rows.Add(new(FriendsRowKind.Trainer, i));
        rows.Add(new(FriendsRowKind.ReadCode, 0));
        rows.Add(new(FriendsRowKind.PasteTeam, 0));
        for (int i = 0; i < Teams.Teams.Count; i++) rows.Add(new(FriendsRowKind.Export, i));
        Rows = rows;
        Cursor = Math.Clamp(Cursor, 0, Rows.Count - 1);
        FirstRow = UiNav.Window(FirstRow, Cursor, Rows.Count, VisibleRows);
    }

    public FriendsRow RowAtCursor => Rows[Cursor];

    public void Move(int delta)
    {
        if (Rows.Count <= 1) return;
        int next = UiNav.Wrap(Cursor, delta, Rows.Count);
        if (next == Cursor) return;
        Cursor = next;
        FirstRow = UiNav.Window(FirstRow, Cursor, Rows.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        var row = RowAtCursor;
        switch (row.Kind)
        {
            case FriendsRowKind.Trainer:
                if (Files[row.Index].File is not { } file) { Say("friends.notice.refused", "", false); AudioManager.PlaySound("error"); return; }
                Chosen = file;
                IsActive = false;
                AudioManager.PlaySound("select");
                break;
            case FriendsRowKind.ReadCode:
                ReadCode(host?.ReadClipboard());
                break;
            case FriendsRowKind.PasteTeam:
                PasteTeam(host?.ReadClipboard());
                break;
            case FriendsRowKind.Export:
                Export(row.Index);
                break;
        }
    }

    public void Cancel()
    {
        AudioManager.PlaySound("cancel");
        IsActive = false;
        Left = true;
    }

    // ------------------------------------------------------------------ the things the rows do

    /// <summary>A code read: a trainer it holds is kept as a file of <c>trainers/</c> (named for the trainer) and stands in the list; a refused one is told and kept nowhere.</summary>
    public void ReadCode(string? code)
    {
        var (file, refused) = TrainerFile.ReadCode(code);
        if (file == null || refused != null)
        {
            var why = refused ?? new TrainerRefusal("trainerFile.refused.code");
            Say(why.Key, why.Value, false);
            AudioManager.PlaySound("error");
            return;
        }
        try
        {
            TrainerFiles.Write(folder, file);
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Say("friends.notice.cantWrite", "", false);
            AudioManager.PlaySound("error");
            return;
        }
        Refresh();
        int at = Files.ToList().FindIndex(f => f.File?.Checksum == file.Checksum);
        if (at >= 0) { Cursor = at; FirstRow = UiNav.Window(FirstRow, Cursor, Rows.Count, VisibleRows); }
        Say("friends.notice.read", file.Name, true);
        AudioManager.PlaySound("select");
    }

    /// <summary>A paste read as a team and kept among the built ones (it can be built on, or taken into a run, from TEAMS).</summary>
    public void PasteTeam(string? paste)
    {
        if (Teams.Teams.Count >= BuiltTeams.Most) { Say("friends.notice.full", BuiltTeams.Most.ToString(), false); AudioManager.PlaySound("error"); return; }
        string name = Text.Of("friends.pastedName", ("n", Teams.Teams.Count + 1));
        var (team, problems, illegal) = ShowdownPaste.Team(name, paste);
        if (team == null)
        {
            if (problems.Count > 0) Say(problems[0].Key, problems[0].Line.ToString(), false);
            else if (illegal is { } why) Say(why.Key, why.Value, false);
            AudioManager.PlaySound("error");
            return;
        }
        Teams.Teams.Add(team);
        Changed = true;
        Refresh();
        Say("friends.notice.pasted", team.Members.Count.ToString(), true);
        AudioManager.PlaySound("select");
    }

    /// <summary>A built team sent: written as a trainer file of <c>trainers/</c> and its code put on the clipboard.</summary>
    public void Export(int team)
    {
        if (team < 0 || team >= Teams.Teams.Count) return;
        var built = Teams.Teams[team];
        if (BuiltTeams.WhyNot(built) is { } why) { Say(why.Key, why.Value, false); Code = null; AudioManager.PlaySound("error"); return; }
        var file = TrainerFile.Of(built.Name, PlayerLook.Boy, TrainerFile.Classes[0], 0, 1, built);
        Code = file.ToCode();
        try
        {
            TrainerFiles.Write(folder, file);
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Say("friends.notice.cantWrite", "", false);
            return;
        }
        host?.WriteClipboard(Code);
        Refresh();
        Say("friends.notice.sent", built.Name, true);
        AudioManager.PlaySound("select");
    }

    private void Say(string key, string value, bool good) => Notice = (key, value, good);

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawFriends(screenWidth, screenHeight, this, Appear);
}
