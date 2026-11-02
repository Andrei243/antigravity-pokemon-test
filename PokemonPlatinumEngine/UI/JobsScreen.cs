using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Poké Jobs board stands: its three places, a Pokémon of the boxes being chosen, the job being set, and what a job paid.</summary>
public enum JobsPhase { Board, Choose, Setup, Result }

/// <summary>The rows of the job's choice: which job, and for how many hours.</summary>
public enum JobsSetupRow { Kind, Hours }

/// <summary>
/// The Poké Jobs board (plan 15 · E3; style guide, "Menu screens", "Poké Jobs"): a PC's POKé JOBS in a game played by
/// the modern rules. The board's three places, each a Pokémon away or an empty place; an empty one chooses a Pokémon of
/// the boxes (no Egg, none of the team), then its job and hours; a Pokémon away is brought back with A and its pay shown.
/// Its logic takes no input (<see cref="Move"/>, <see cref="Sideways"/>, <see cref="Confirm"/>, <see cref="Cancel"/>),
/// each told the time where it matters, so tests and the harness drive it; what a Pokémon brought back may evolve into
/// is the engine's (<see cref="TakeCollected"/>).
/// </summary>
public class JobsScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The rows the choosing list shows: as many as its panel holds.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    private float openAge;
    private JobResult? collected;
    private Random random = new(0);

    public bool IsActive { get; private set; }
    public JobsPhase Phase { get; private set; }
    public JobBoard Board { get; private set; } = new();
    public PcBoxes Boxes { get; private set; } = new();
    public Inventory Bag { get; private set; } = new();

    /// <summary>The place of the board under the cursor, 0 to <see cref="JobBoard.Slots"/> − 1.</summary>
    public int Cursor { get; private set; }

    /// <summary>The boxes' Pokémon that may be sent, in the boxes' order, while one is chosen.</summary>
    public IReadOnlyList<Pokemon> Candidates { get; private set; } = Array.Empty<Pokemon>();
    public int ChooseCursor { get; private set; }
    public int ChooseFirst { get; private set; }

    /// <summary>The Pokémon chosen to be sent, while its job is set.</summary>
    public Pokemon? Chosen { get; private set; }
    public JobsSetupRow SetupRow { get; private set; }
    public JobKind Kind { get; private set; }
    public int Hours { get; private set; } = 8;

    /// <summary>The Pokémon away as Pokémon, for the board to draw, kept in step with the board's jobs.</summary>
    public IReadOnlyList<Pokemon> Away { get; private set; } = Array.Empty<Pokemon>();

    /// <summary>What the last job brought back paid, while the result shows.</summary>
    public JobResult? Result { get; private set; }

    /// <summary>A word said under the board after something was refused (the boxes full, the clock gone back), or null.</summary>
    public string? Notice { get; private set; }

    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>The job under the board's cursor, or null on an empty place.</summary>
    public Job? SelectedJob => Phase == JobsPhase.Board && Cursor < Board.Jobs.Count ? Board.Jobs[Cursor] : null;

    /// <summary>The Pokémon of the place under the cursor, away or being chosen; null on an empty place.</summary>
    public Pokemon? Shown => Phase switch
    {
        JobsPhase.Board => Cursor < Away.Count ? Away[Cursor] : null,
        JobsPhase.Choose => ChooseCursor < Candidates.Count ? Candidates[ChooseCursor] : null,
        _ => Chosen
    };

    public void Open(JobBoard board, PcBoxes boxes, Inventory bag, Random random)
    {
        Board = board;
        Boxes = boxes;
        Bag = bag;
        this.random = random;
        Phase = JobsPhase.Board;
        Cursor = 0;
        Notice = null;
        Result = null;
        collected = null;
        Chosen = null;
        openAge = 0f;
        IsActive = true;
        Refresh();
        AudioManager.PlaySound("menu_open");
    }

    public void Close() => IsActive = false;

    private void Refresh() => Away = Board.Jobs.Select(j => j.Pokemon.ToPokemon()).ToList();

    /// <summary>A Pokémon brought back, for the engine to see to its evolution; taken once.</summary>
    public JobResult? TakeCollected()
    {
        var taken = collected;
        collected = null;
        return taken;
    }

    /// <summary>Up (−1) and down (+1): the board's places, the boxes' Pokémon, or the job's two rows.</summary>
    public void Move(int delta)
    {
        if (delta == 0) return;
        switch (Phase)
        {
            case JobsPhase.Board:
                Cursor = UiNav.Wrap(Cursor, delta, JobBoard.Slots);
                Notice = null;
                break;
            case JobsPhase.Choose:
                if (Candidates.Count == 0) return;
                ChooseCursor = UiNav.Wrap(ChooseCursor, delta, Candidates.Count);
                ChooseFirst = UiNav.Window(ChooseFirst, ChooseCursor, Candidates.Count, VisibleRows);
                break;
            case JobsPhase.Setup:
                SetupRow = SetupRow == JobsSetupRow.Kind ? JobsSetupRow.Hours : JobsSetupRow.Kind;
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Left (−1) and right (+1): while the job is set, the kind or the hours (1 to 24, stopping at the ends).</summary>
    public void Sideways(int delta)
    {
        if (Phase != JobsPhase.Setup || delta == 0) return;
        if (SetupRow == JobsSetupRow.Kind)
        {
            var kinds = Enum.GetValues<JobKind>();
            Kind = kinds[UiNav.Wrap(Array.IndexOf(kinds, Kind), Math.Sign(delta), kinds.Length)];
        }
        else
        {
            int hours = Math.Clamp(Hours + Math.Sign(delta), JobBoard.MinHours, JobBoard.MaxHours);
            if (hours == Hours) return;
            Hours = hours;
        }
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button, at the time <paramref name="now"/> (UTC).</summary>
    public void Confirm(DateTime now)
    {
        switch (Phase)
        {
            case JobsPhase.Board:
                if (SelectedJob is { } job) BringBack(job, now);
                else ChooseOne();
                break;
            case JobsPhase.Choose:
                if (Candidates.Count == 0) { AudioManager.PlaySound("error"); return; }
                Chosen = Candidates[ChooseCursor];
                SetupRow = JobsSetupRow.Kind;
                Phase = JobsPhase.Setup;
                AudioManager.PlaySound("select");
                break;
            case JobsPhase.Setup:
                Send(now);
                break;
            case JobsPhase.Result:
                Result = null;
                Phase = JobsPhase.Board;
                AudioManager.PlaySound("select");
                break;
        }
    }

    /// <summary>The B button: a step back, and off the board from its places.</summary>
    public void Cancel()
    {
        switch (Phase)
        {
            case JobsPhase.Board:
                Close();
                break;
            case JobsPhase.Choose:
                Phase = JobsPhase.Board;
                break;
            case JobsPhase.Setup:
                Chosen = null;
                Phase = JobsPhase.Choose;
                break;
            case JobsPhase.Result:
                Result = null;
                Phase = JobsPhase.Board;
                break;
        }
        AudioManager.PlaySound("cancel");
    }

    private void ChooseOne()
    {
        if (Board.IsFull) { AudioManager.PlaySound("error"); return; }
        Candidates = Boxes.All.Where(p => !p.IsEgg).ToList();
        if (Candidates.Count == 0)
        {
            Notice = Text.Of("jobs.noneInTheBoxes");
            AudioManager.PlaySound("error");
            return;
        }
        ChooseCursor = ChooseFirst = 0;
        Kind = JobKind.Errand;
        Hours = 8;
        Phase = JobsPhase.Choose;
        AudioManager.PlaySound("select");
    }

    private void Send(DateTime now)
    {
        if (Chosen is not { } pokemon || Board.WhyNot(pokemon, Hours) != JobRefusal.None || !Boxes.Remove(pokemon))
        {
            AudioManager.PlaySound("error");
            return;
        }
        Board.Post(pokemon, Kind, Hours, now);
        Refresh();
        Cursor = Board.Jobs.Count - 1;
        Chosen = null;
        Phase = JobsPhase.Board;
        Notice = Text.Of("jobs.sent", ("name", pokemon.DisplayName), ("job", KindName(Kind)));
        AudioManager.PlaySound("select");
    }

    private void BringBack(Job job, DateTime now)
    {
        if (Boxes.IsFull)
        {
            Notice = Text.Of("jobs.boxesFull");
            AudioManager.PlaySound("error");
            return;
        }
        var result = Board.Collect(job, now, random, out var refusal);
        if (result == null)
        {
            Notice = refusal == JobRefusal.ClockBack ? Text.Of("jobs.clockBack") : null;
            AudioManager.PlaySound("error");
            return;
        }
        Boxes.Store(result.Pokemon);
        result.GiveTo(Bag);
        Refresh();
        Cursor = Math.Min(Cursor, JobBoard.Slots - 1);
        Result = collected = result;
        Notice = null;
        Phase = JobsPhase.Result;
        AudioManager.PlaySound("levelup");
    }

    /// <summary>A job's name in words (ERRAND, ORCHARD, HOT SPRING).</summary>
    public static string KindName(JobKind kind)
    {
        string key = kind.ToString();
        return Text.Of($"jobs.kind.{key}");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) Sideways(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) Sideways(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(WallClock.UtcNow);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawJobs(screenWidth, screenHeight, this, WallClock.UtcNow, Appear);
}
