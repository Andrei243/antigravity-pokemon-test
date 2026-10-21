using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>A save the migration can bring Pokémon to: a slot of the title and what it holds.</summary>
public sealed record MigrateTarget(int Slot, SaveData Save);

/// <summary>What the player has asked of the migration: bring these six of this source into this slot's save.</summary>
public sealed record MigrateRequest(MigrateTarget Target, MigrationSource Source, IReadOnlyList<ForeignPokemon> Six);

public enum MigratePhase
{
    /// <summary>Which save the Pokémon go to (only with several saves).</summary>
    Save,
    /// <summary>Which of the files in the folder to bring them from.</summary>
    Files,
    /// <summary>A source's boxes, to choose six from.</summary>
    Box,
    /// <summary>"Bring these six?"</summary>
    Confirm,
    /// <summary>The answer is being carried out (the engine writes the save).</summary>
    Working,
    /// <summary>What came of it.</summary>
    Result
}

/// <summary>
/// The migration's screen (plan 08 · P21; style guide, "Menu screens", "Migrate"; <c>docs/mechanics/rulings.md</c>,
/// "Pal Park's migration"): reached from the title's MIGRATE. It lists the save to bring the Pokémon to (only when there
/// are several), the files of the <c>migrate</c> folder, a file's boxes as the PC draws them, each Pokémon that can't
/// come greyed with its reason, and six chosen; a question confirms. It writes nothing: the choice is handed over once
/// (<see cref="TakeRequest"/>) and the engine, which owns the saves, writes the receiving one and says what came of it
/// (<see cref="Finish"/>). Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>,
/// <see cref="ChangeBox"/>, <see cref="Done"/>), so tests and the harness drive it.
/// </summary>
public class MigrateScreen
{
    private const float AppearTime = 0.3f;
    /// <summary>The box is drawn as the PC draws one: six across, five down.</summary>
    public const int Columns = 6, Rows = 5, PerBox = Columns * Rows;

    private float openAge;
    private MigrateRequest? request;
    private readonly Dictionary<ForeignPokemon, MigrationVerdict> verdicts = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int, Pastimes> parks = new();
    private readonly List<(int Box, int Slot)> chosen = new();
    private Func<DateTime> clock = () => WallClock.UtcNow;

    /// <summary>How many rows a list shows (as many as its panel holds).</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    public bool IsActive { get; private set; }
    public MigratePhase Phase { get; private set; } = MigratePhase.Files;

    public IReadOnlyList<MigrateTarget> Targets { get; private set; } = Array.Empty<MigrateTarget>();
    public IReadOnlyList<MigrationSource> Sources { get; private set; } = Array.Empty<MigrationSource>();

    /// <summary>The save the Pokémon are going to; null before one is chosen.</summary>
    public MigrateTarget? Target { get; private set; }

    /// <summary>The cursor in the list the phase shows (the saves, the files), its first row, and in a box the Pokémon's place and the box's number.</summary>
    public int Cursor { get; private set; }
    public int FirstRow { get; private set; }
    public int Place { get; private set; }
    public int BoxNumber { get; private set; }

    /// <summary>The file chosen, whose boxes are on show.</summary>
    public MigrationSource? Source { get; private set; }

    /// <summary>In the question: true while YES is highlighted.</summary>
    public bool ConfirmYes { get; private set; }

    /// <summary>What came of the request, once the engine says (<see cref="Finish"/>).</summary>
    public MigrationBlock Outcome { get; private set; }

    /// <summary>Whether six were brought over this time the screen was open: the title's cards are read again if so.</summary>
    public bool Brought { get; private set; }

    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>Opens on the saves that can receive and the sources the folder gave; with one save the choice of it is skipped.</summary>
    public void Open(IReadOnlyList<MigrateTarget> targets, IReadOnlyList<MigrationSource> sources, Func<DateTime>? now = null)
    {
        Targets = targets;
        Sources = sources;
        clock = now ?? (() => WallClock.UtcNow);
        parks.Clear();
        verdicts.Clear();
        chosen.Clear();
        Target = targets.Count == 1 ? targets[0] : null;
        Phase = Target == null ? MigratePhase.Save : MigratePhase.Files;
        Cursor = 0;
        FirstRow = 0;
        Source = null;
        request = null;
        Brought = false;
        Outcome = MigrationBlock.None;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close() => IsActive = false;

    // ------------------------------------------------------------------ what the rules say now

    /// <summary>The park of a save, as it stands now: what waits in it and which sources have given.</summary>
    public PalParkState ParkOf(MigrateTarget target)
    {
        if (!parks.TryGetValue(target.Slot, out var pastimes))
        {
            pastimes = new Pastimes();
            pastimes.Load(target.Save.Pastimes);
            parks[target.Slot] = pastimes;
        }
        return pastimes.PalPark;
    }

    /// <summary>Whether a source can bring Pokémon to the chosen save now, and if not why.</summary>
    public MigrationBlock BlockOf(MigrationSource source) =>
        Target == null || source.Save == null ? MigrationBlock.None
            : Migration.WhyNotFrom(source.Save, Target.Save.NationalPokedex, ParkOf(Target), clock());

    /// <summary>What the player is told of a source that can't bring Pokémon now.</summary>
    public string BlockWords(MigrationSource source) =>
        Migration.Words(BlockOf(source), source.Save != null && Target != null ? Migration.WaitLeft(source.Save, ParkOf(Target), clock()) : default);

    /// <summary>Whether a Pokémon of the source on show can be chosen, and if not why.</summary>
    public MigrationVerdict VerdictOf(ForeignPokemon pokemon)
    {
        if (!verdicts.TryGetValue(pokemon, out var verdict))
            verdicts[pokemon] = verdict = Migration.WhyNot(pokemon, Target != null ? ParkOf(Target).Ledger : new PalParkLedger());
        return verdict;
    }

    /// <summary>The Pokémon under the cursor in the box on show; null in an empty place.</summary>
    public ForeignPokemon? Under => Source?.Save is { } save && BoxNumber < save.Boxes.Count && Place < save.Boxes[BoxNumber].Slots.Count
        ? save.Boxes[BoxNumber].Slots[Place] : null;

    public ForeignBox? Box => Source?.Save is { } save && BoxNumber < save.Boxes.Count ? save.Boxes[BoxNumber] : null;

    /// <summary>The Pokémon chosen, in the order they were chosen.</summary>
    public IReadOnlyList<ForeignPokemon> Six => chosen.Select(c => Source!.Save!.Boxes[c.Box].Slots[c.Slot]!).ToList();

    /// <summary>The place a Pokémon of the box on show was chosen in (1 to 6), or 0 when it wasn't.</summary>
    public int ChosenNumber(int slot) => chosen.FindIndex(c => c.Box == BoxNumber && c.Slot == slot) + 1;

    public int ChosenCount => chosen.Count;

    /// <summary>How many Pokémon a box holds, for its tab.</summary>
    public int BoxCount => Source?.Save?.Boxes.Count ?? 0;

    /// <summary>The rows of the list the phase shows.</summary>
    public int ListLength => Phase == MigratePhase.Save ? Targets.Count : Sources.Count;

    // ------------------------------------------------------------------ the buttons

    /// <summary>The cursor: up and down in a list, in all four ways in a box, either way over the question.</summary>
    public void Move(int dx, int dy)
    {
        switch (Phase)
        {
            case MigratePhase.Save:
            case MigratePhase.Files:
                if (dy == 0 || ListLength == 0) return;
                Cursor = UiNav.Wrap(Cursor, dy, ListLength);
                FirstRow = UiNav.Window(FirstRow, Cursor, ListLength, VisibleRows);
                AudioManager.PlaySound("cursor");
                break;
            case MigratePhase.Box:
                if (dx == 0 && dy == 0) return;
                int column = Place % Columns, row = Place / Columns;
                column = ((column + dx) % Columns + Columns) % Columns;
                row = ((row + dy) % Rows + Rows) % Rows;
                Place = row * Columns + column;
                AudioManager.PlaySound("cursor");
                break;
            case MigratePhase.Confirm:
                if (dx == 0 && dy == 0) return;
                ConfirmYes = !ConfirmYes;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>L and R in a box: the next box or the one before, round the source's boxes.</summary>
    public void ChangeBox(int step)
    {
        if (Phase != MigratePhase.Box || BoxCount < 2 || step == 0) return;
        BoxNumber = UiNav.Wrap(BoxNumber, step, BoxCount);
        Place = 0;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button.</summary>
    public void Confirm()
    {
        switch (Phase)
        {
            case MigratePhase.Save:
                if (Cursor >= Targets.Count) return;
                Target = Targets[Cursor];
                Phase = MigratePhase.Files;
                Cursor = 0;
                FirstRow = 0;
                AudioManager.PlaySound("select");
                break;
            case MigratePhase.Files:
                if (Cursor >= Sources.Count) return;
                var source = Sources[Cursor];
                if (!source.Readable || BlockOf(source) != MigrationBlock.None)
                {
                    AudioManager.PlaySound("error");
                    return;
                }
                Source = source;
                BoxNumber = 0;
                Place = 0;
                chosen.Clear();
                Phase = MigratePhase.Box;
                AudioManager.PlaySound("select");
                break;
            case MigratePhase.Box:
                Choose();
                break;
            case MigratePhase.Confirm:
                if (!ConfirmYes)
                {
                    Cancel();
                    return;
                }
                request = new MigrateRequest(Target!, Source!, Six);
                Phase = MigratePhase.Working;
                AudioManager.PlaySound("select");
                break;
            case MigratePhase.Result:
                IsActive = false;
                AudioManager.PlaySound("select");
                break;
        }
    }

    /// <summary>The A button over a Pokémon: chosen if it may be and fewer than six are, put back if it was.</summary>
    private void Choose()
    {
        if (Under is not { } pokemon) return;
        int number = ChosenNumber(Place);
        if (number > 0)
        {
            chosen.RemoveAt(number - 1);
            AudioManager.PlaySound("cancel");
            return;
        }
        if (!VerdictOf(pokemon).Allowed || chosen.Count >= Migration.Size)
        {
            AudioManager.PlaySound("error");
            return;
        }
        chosen.Add((BoxNumber, Place));
        AudioManager.PlaySound("select");
    }

    /// <summary>The Menu button in a box: with six chosen, the question is asked.</summary>
    public void Done()
    {
        if (Phase != MigratePhase.Box) return;
        if (chosen.Count != Migration.Size)
        {
            AudioManager.PlaySound("error");
            return;
        }
        ConfirmYes = false;
        Phase = MigratePhase.Confirm;
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: back a step, out of the screen from the first.</summary>
    public void Cancel()
    {
        switch (Phase)
        {
            case MigratePhase.Save:
                IsActive = false;
                break;
            case MigratePhase.Files:
                if (Targets.Count > 1)
                {
                    Phase = MigratePhase.Save;
                    Cursor = Targets.ToList().FindIndex(t => t == Target);
                    FirstRow = 0;
                    Target = null;
                }
                else IsActive = false;
                break;
            case MigratePhase.Box:
                Phase = MigratePhase.Files;
                Cursor = Math.Max(0, Sources.ToList().FindIndex(s => s == Source));
                FirstRow = UiNav.Window(0, Cursor, ListLength, VisibleRows);
                chosen.Clear();
                Source = null;
                break;
            case MigratePhase.Confirm:
                Phase = MigratePhase.Box;
                break;
            case MigratePhase.Working:
                return;
            case MigratePhase.Result:
                IsActive = false;
                break;
        }
        AudioManager.PlaySound("cancel");
    }

    // ------------------------------------------------------------------ with the engine

    /// <summary>The choice made, handed over once; null while there isn't one.</summary>
    public MigrateRequest? TakeRequest()
    {
        var made = request;
        request = null;
        return made;
    }

    /// <summary>The engine has carried the request out, or couldn't: the screen tells the player.</summary>
    public void Finish(MigrationBlock outcome)
    {
        Outcome = outcome;
        Brought = Brought || outcome == MigrationBlock.None;
        Phase = MigratePhase.Result;
        // What was brought makes the park busy: the lists are asked again
        parks.Clear();
        verdicts.Clear();
        chosen.Clear();
        AudioManager.PlaySound(outcome == MigrationBlock.None ? "select" : "error");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        int pages = InputManager.Axis(GameAction.PageLeft, GameAction.PageRight);
        if (pages != 0 && Phase == MigratePhase.Box) ChangeBox(pages);
        else if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Menu)) Done();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawMigrate(screenWidth, screenHeight, this, Appear);
}
