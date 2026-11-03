using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The battle room's pages: its menu, a team to take into a run, the puzzles, between two battles of a run, and a run's or a puzzle's end.</summary>
public enum RoomPage { Menu, PickTeam, Puzzles, Between, RunOver, PuzzleOver }

/// <summary>The battle room's menu, in its order.</summary>
public enum RoomEntry { Teams, Gauntlet, Streak, Puzzles, Friends, Daily }

/// <summary>What the battle room asks the engine for, handed over once (<see cref="BattleRoomScreen.TakeRequest"/>).</summary>
public abstract record RoomRequest;
public sealed record OpenBuilder : RoomRequest;
public sealed record OpenFriends : RoomRequest;
public sealed record BeginRun(RoomChallenge Kind, int Team) : RoomRequest;
public sealed record NextBattle : RoomRequest;
public sealed record GiveUpRun : RoomRequest;
public sealed record PlayPuzzle(string Id) : RoomRequest;
/// <summary>The day's run begun (plan 13 · V14): its six on offer to be rented.</summary>
public sealed record BeginDaily : RoomRequest;
/// <summary>A trade offered after a win of the daily run.</summary>
public sealed record TradeDaily : RoomRequest;

/// <summary>
/// The battle room (plan 13 · V13, V26; style guide, "Opening and title screen" and "Menu screens", "The battle room"): the
/// title's BATTLE ROOM, where a team built from nothing goes against the gauntlet of Sinnoh's Gym Leaders, Elite Four and
/// Champion or an endless streak of trainers, and where the puzzles are. Nothing in it touches a save. Its logic takes no
/// input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it; what
/// it wants done it hands over as a <see cref="RoomRequest"/>.
/// </summary>
public sealed class BattleRoomScreen
{
    private const float AppearTime = 0.3f;
    private float age;
    private RoomRequest? request;
    private RoomChallenge picking;

    public bool IsActive { get; private set; }

    /// <summary>B on the menu: the room is left for the title.</summary>
    public bool Left { get; private set; }

    public float Appear => Math.Clamp(age / AppearTime, 0f, 1f);
    public RoomPage Page { get; private set; }
    public int Cursor { get; private set; }
    public int FirstRow { get; private set; }

    /// <summary>The teams kept, the puzzles, and this machine's records, as the engine hands them over.</summary>
    public IReadOnlyList<BuiltTeam> Teams { get; private set; } = Array.Empty<BuiltTeam>();
    public IReadOnlyList<PuzzleFile> PuzzleList { get; private set; } = Array.Empty<PuzzleFile>();
    public BattleRoomRecords Records { get; private set; } = new();

    /// <summary>The run under way or just over, and whether it ended on a new best.</summary>
    public RoomRun? Run { get; private set; }

    /// <summary>The daily run under way or just over (plan 13 · V14); null outside one.</summary>
    public DailyRun? Daily { get; private set; }

    /// <summary>This machine's daily results, as the engine hands them over.</summary>
    public DailyRecords DailyRecords { get; private set; } = new();

    /// <summary>Today as the daily run counts days, as the engine hands it over.</summary>
    public DateTime Today { get; private set; } = DailyRun.Today();
    public bool NewBest { get; private set; }

    /// <summary>The puzzle just played and whether it was solved.</summary>
    public PuzzleFile? LastPuzzle { get; private set; }
    public bool PuzzleSolved { get; private set; }

    /// <summary>Which challenge a team is being picked for.</summary>
    public RoomChallenge Picking => picking;

    public static readonly RoomEntry[] Entries = Enum.GetValues<RoomEntry>();

    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    public void Open(IReadOnlyList<BuiltTeam> teams, IReadOnlyList<PuzzleFile> puzzles, BattleRoomRecords records, DailyRecords? daily = null, DateTime? today = null)
    {
        Teams = teams;
        PuzzleList = puzzles;
        Records = records;
        DailyRecords = daily ?? new DailyRecords();
        Today = (today ?? DailyRun.Today()).Date;
        Run = null;
        Daily = null;
        request = null;
        Left = false;
        Show(RoomPage.Menu, 0);
        age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Back from the team builder with the teams as they now stand.</summary>
    public void Resume(IReadOnlyList<BuiltTeam> teams)
    {
        Teams = teams;
        IsActive = true;
        age = AppearTime;
    }

    /// <summary>A battle of the run is over: between battles if it goes on, or its end.</summary>
    public void AfterRunBattle(RoomRun run, bool newBest)
    {
        Run = run;
        NewBest = newBest;
        IsActive = true;
        age = AppearTime;
        Show(run.Over ? RoomPage.RunOver : RoomPage.Between, 0);
    }

    /// <summary>A battle of the daily run is over, or the run was given up: between battles if it goes on, or its end.</summary>
    public void AfterDailyBattle(DailyRun run, bool newBest)
    {
        Run = null;
        Daily = run;
        NewBest = newBest;
        IsActive = true;
        age = AppearTime;
        Show(run.Over ? RoomPage.RunOver : RoomPage.Between, 0);
    }

    /// <summary>The day's rentals were put back without three chosen: the room's menu again, on the daily row.</summary>
    public void AfterDailyRentals()
    {
        Daily = null;
        IsActive = true;
        age = AppearTime;
        Show(RoomPage.Menu, (int)RoomEntry.Daily);
    }

    /// <summary>A puzzle's battle is over, solved or not.</summary>
    public void AfterPuzzle(PuzzleFile puzzle, bool solved)
    {
        LastPuzzle = puzzle;
        PuzzleSolved = solved;
        IsActive = true;
        age = AppearTime;
        Show(RoomPage.PuzzleOver, 0);
    }

    public void Close() => IsActive = false;

    public RoomRequest? TakeRequest()
    {
        var r = request;
        request = null;
        return r;
    }

    /// <summary>How many rows the page has.</summary>
    public int Rows => Page switch
    {
        RoomPage.Menu => Entries.Length,
        RoomPage.PickTeam => Math.Max(1, Teams.Count),
        RoomPage.Puzzles => PuzzleList.Count,
        RoomPage.Between => Daily != null ? 3 : 2,
        RoomPage.PuzzleOver => 2,
        _ => 1
    };

    public void Move(int delta)
    {
        if (Rows <= 1) return;
        int next = UiNav.Wrap(Cursor, delta, Rows);
        if (next == Cursor) return;
        Cursor = next;
        FirstRow = UiNav.Window(FirstRow, Cursor, Rows, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Whether a team can be taken into a run: the check the builder shows (<see cref="BuiltTeams.WhyNot(BuiltTeam)"/>).</summary>
    public bool CanTake(int team) => team >= 0 && team < Teams.Count && BuiltTeams.IsLegal(Teams[team]);

    public void Confirm()
    {
        switch (Page)
        {
            case RoomPage.Menu:
                switch (Entries[Cursor])
                {
                    case RoomEntry.Teams: Ask(new OpenBuilder()); break;
                    case RoomEntry.Friends: Ask(new OpenFriends()); break;
                    case RoomEntry.Gauntlet: PickFor(RoomChallenge.Gauntlet); break;
                    case RoomEntry.Streak: PickFor(RoomChallenge.Streak); break;
                    case RoomEntry.Daily: Ask(new BeginDaily()); break;
                    case RoomEntry.Puzzles:
                        AudioManager.PlaySound("select");
                        Show(RoomPage.Puzzles, 0);
                        break;
                }
                break;
            case RoomPage.PickTeam:
                if (!CanTake(Cursor)) { AudioManager.PlaySound("error"); return; }
                Ask(new BeginRun(picking, Cursor));
                break;
            case RoomPage.Puzzles:
                if (Cursor < PuzzleList.Count) Ask(new PlayPuzzle(PuzzleList[Cursor].Id));
                break;
            case RoomPage.Between when Daily != null:
                // NEXT BATTLE, TRADE (once after each win), GIVE UP
                if (Cursor == 1 && !Daily.CanTrade) { AudioManager.PlaySound("error"); return; }
                Ask(Cursor == 0 ? new NextBattle() : Cursor == 1 ? new TradeDaily() : new GiveUpRun());
                break;
            case RoomPage.Between:
                Ask(Cursor == 0 ? new NextBattle() : new GiveUpRun());
                break;
            case RoomPage.RunOver:
                AudioManager.PlaySound("select");
                Run = null;
                if (Daily != null)
                {
                    Daily = null;
                    Show(RoomPage.Menu, (int)RoomEntry.Daily);
                    break;
                }
                Show(RoomPage.Menu, (int)(picking == RoomChallenge.Gauntlet ? RoomEntry.Gauntlet : RoomEntry.Streak));
                break;
            case RoomPage.PuzzleOver:
                if (Cursor == 0 && LastPuzzle != null) Ask(new PlayPuzzle(LastPuzzle.Id));
                else
                {
                    AudioManager.PlaySound("select");
                    Show(RoomPage.Puzzles, LastPuzzle == null ? 0 : Math.Max(0, PuzzleList.ToList().FindIndex(p => p.Id == LastPuzzle.Id)));
                }
                break;
        }
    }

    public void Cancel()
    {
        switch (Page)
        {
            case RoomPage.Menu:
                AudioManager.PlaySound("cancel");
                IsActive = false;
                Left = true;
                break;
            case RoomPage.PickTeam or RoomPage.Puzzles:
                AudioManager.PlaySound("cancel");
                Show(RoomPage.Menu, Page == RoomPage.Puzzles ? (int)RoomEntry.Puzzles : picking == RoomChallenge.Friend ? (int)RoomEntry.Friends : picking == RoomChallenge.Gauntlet ? (int)RoomEntry.Gauntlet : (int)RoomEntry.Streak);
                break;
            case RoomPage.PuzzleOver:
                AudioManager.PlaySound("cancel");
                Show(RoomPage.Puzzles, 0);
                break;
            // Between two battles B asks nothing: the run is given up only from its row
        }
    }

    /// <summary>A friend's trainer chosen on the friends' page: which of the teams takes them on (plan 13 · V22).</summary>
    public void PickForFriend(IReadOnlyList<BuiltTeam> teams)
    {
        Teams = teams;
        IsActive = true;
        age = AppearTime;
        PickFor(RoomChallenge.Friend);
    }

    /// <summary>A fight with a friend's trainer is over: the room's menu again, on the friends' row.</summary>
    public void AfterFriend(IReadOnlyList<BuiltTeam> teams)
    {
        Teams = teams;
        IsActive = true;
        age = AppearTime;
        Show(RoomPage.Menu, (int)RoomEntry.Friends);
    }

    private void PickFor(RoomChallenge kind)
    {
        picking = kind;
        AudioManager.PlaySound("select");
        Show(RoomPage.PickTeam, Math.Max(0, Teams.ToList().FindIndex(BuiltTeams.IsLegal)));
    }

    private void Ask(RoomRequest r)
    {
        // The engine answers every request: a battle, the builder, or the run's end shown again (AfterRunBattle)
        request = r;
        IsActive = false;
        AudioManager.PlaySound("select");
    }

    private void Show(RoomPage page, int cursor)
    {
        Page = page;
        Cursor = Math.Clamp(cursor, 0, Math.Max(0, Rows - 1));
        FirstRow = UiNav.Window(0, Cursor, Rows, VisibleRows);
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        age += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawBattleRoom(screenWidth, screenHeight, this, Appear);
}
