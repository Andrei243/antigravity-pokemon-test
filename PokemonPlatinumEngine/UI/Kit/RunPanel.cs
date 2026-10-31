using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>What a row of the run panel sets (plan 13 · V1).</summary>
public enum RunRow
{
    Preset, Difficulty, LevelCap, ScaleTrainers, Nuzlocke, DupesClause, ShinyClause, GiftsCount, Monotype, PartyLimit,
    NoBattleItems, NoCenter, NoCatching, NoRunning, SetStyle, NoLaterMechanics, ShinyOdds, RandomSpecies, RandomItems, RandomAbilities, RandomMoves, RandomTypes, Seed, Start
}

/// <summary>
/// The panel of the ways to play that NEW GAME opens (plan 13 · V1; style guide, "Opening and title screen"): rows like
/// the options', the preset first and a START row last. GPU-free: <see cref="Move"/>, <see cref="Change"/> and
/// <see cref="Confirm"/> take no input, so tests and the harness walk it. Editing any row after the preset makes the run a
/// custom one unless it lands on a preset again; Confirm on the preset row, on START, or anywhere on a Normal run starts
/// the game at once.
/// </summary>
public sealed class RunPanel
{
    private int selected;

    /// <summary>The run being put together.</summary>
    public RunRules Rules { get; private set; } = RunRules.Normal;

    /// <summary>The first row showing, for a list taller than the panel.</summary>
    public int First { get; private set; }

    /// <summary>How many rows the panel holds at once.</summary>
    public const int VisibleRows = 9;

    /// <summary>A custom run's preset row steps on from the last preset chosen, so Left and Right never lose their place.</summary>
    private int presetAt;

    /// <summary>The rows shown now: the Nuzlocke's clauses only while it is on.</summary>
    public IReadOnlyList<RunRow> Rows
    {
        get
        {
            var rows = new List<RunRow>();
            foreach (RunRow row in Enum.GetValues<RunRow>())
            {
                if (row is RunRow.DupesClause or RunRow.ShinyClause or RunRow.GiftsCount && !Rules.Nuzlocke) continue;
                // The seed shows once something is randomised (plan 13 · V6)
                if (row == RunRow.Seed && Rules.Random == null) continue;
                rows.Add(row);
            }
            return rows;
        }
    }

    public int Selected => selected;
    public RunRow SelectedRow => Rows[Math.Min(selected, Rows.Count - 1)];

    /// <summary>Whether the player has pressed START (or confirmed a Normal run); the title reads it once.</summary>
    public bool Done { get; private set; }

    /// <summary>A panel holding a finished run, for whoever shows it (the Trainer Card's page); not for choosing.</summary>
    public static RunPanel Showing(RunRules rules) => new() { Rules = rules };

    /// <summary>Opens the panel on Normal, the cursor on the preset row.</summary>
    public void Open()
    {
        Rules = RunRules.Normal;
        selected = 0;
        First = 0;
        presetAt = 0;
        Done = false;
        SeedEntry = null;
    }

    // ------------------------------------------------------------------ the randomiser's seed (plan 13 · V6)

    /// <summary>Where a new seed comes from when the randomiser is first turned on or the seed row is stepped: one roll of the game's chance (a test or a tool hands its own).</summary>
    public Func<int> RollSeed { get; set; } = () => Dice.Shared.Next(1, RandomizerOptions.MaxSeed + 1);

    /// <summary>
    /// The keyboard a seed is typed on while the player types one (the introduction's, with the digits only; the letters are
    /// passed over); null otherwise.
    /// </summary>
    public NameEntry? SeedEntry { get; private set; }

    /// <summary>Whether the keyboard for the seed is open: the panel's other rows wait.</summary>
    public bool EnteringSeed => SeedEntry != null;

    /// <summary>How many digits a seed has at most.</summary>
    public static readonly int SeedDigits = RandomizerOptions.MaxSeed.ToString().Length;

    private void OpenSeedEntry() => SeedEntry = new NameEntry(null, SeedDigits, extras: "");

    /// <summary>A step of the keyboard's cursor while a seed is being typed.</summary>
    public void MoveCursor(int dx, int dy) => SeedEntry?.Move(dx, dy);

    /// <summary>The A button on the seed's keyboard: a digit typed, a letter passed over, OK takes the seed typed (the one it had when nothing was).</summary>
    public void PressSeedKey()
    {
        if (SeedEntry is not { } entry) return;
        if (entry.KeyUnderCursor == NameKey.Letter && !char.IsDigit(entry.RowKeys(entry.Row)[entry.Column])) return;
        entry.Press();
        if (!entry.Done) return;
        if (Randomizer.ParseSeed(entry.Text) is { } typed && Rules.Random is { } options) Rules = Rules with { Random = options with { Seed = typed } };
        SeedEntry = null;
    }

    /// <summary>The B button on the seed's keyboard: a digit taken back, and with none left the keyboard closed with the seed as it was.</summary>
    public void CancelSeedKey()
    {
        if (SeedEntry is not { } entry) return;
        if (!entry.Backspace()) SeedEntry = null;
    }

    /// <summary>The Start button on the seed's keyboard: the cursor to OK.</summary>
    public void SeedToDone() => SeedEntry?.ToDone();

    /// <summary>One half of the randomiser turned over: turned on alone with a rolled seed when nothing was randomised, and the randomiser gone once no half is on.</summary>
    private RandomizerOptions? Toggled(RandomizerOptions? options, RunRow row)
    {
        options ??= new RandomizerOptions { Seed = RollSeed(), Species = false, Items = false };
        var changed = row switch
        {
            RunRow.RandomSpecies => options with { Species = !options.Species },
            RunRow.RandomItems => options with { Items = !options.Items },
            RunRow.RandomAbilities => options with { Abilities = !options.Abilities },
            RunRow.RandomMoves => options with { Moves = !options.Moves },
            RunRow.RandomTypes => options with { Types = !options.Types },
            _ => options
        };
        return changed.Any ? changed : null;
    }

    public void Move(int step)
    {
        if (EnteringSeed) return;
        var rows = Rows;
        selected = ((selected + step) % rows.Count + rows.Count) % rows.Count;
        First = UiNav.Window(First, selected, rows.Count, VisibleRows);
    }

    /// <summary>Left (-1) or right (+1) on the selected row: the next value of it.</summary>
    public void Change(int direction)
    {
        int d = Math.Sign(direction);
        if (d == 0 || EnteringSeed) return;
        var r = Rules;
        switch (SelectedRow)
        {
            case RunRow.Preset:
                var presets = RunRules.Presets;
                presetAt = ((presetAt + d) % presets.Count + presets.Count) % presets.Count;
                Rules = presets[presetAt].Rules;
                break;
            case RunRow.Difficulty:
                Rules = r with { Difficulty = (Difficulty)Math.Clamp((int)r.Difficulty + d, 0, 2) };
                break;
            case RunRow.LevelCap: Rules = r with { LevelCap = !r.LevelCap }; break;
            case RunRow.ScaleTrainers: Rules = r with { ScaleTrainers = !r.ScaleTrainers }; break;
            case RunRow.Nuzlocke:
                Rules = r with { Nuzlocke = !r.Nuzlocke, DupesClause = false, ShinyClause = false, GiftsCount = false };
                break;
            case RunRow.DupesClause: Rules = r with { DupesClause = !r.DupesClause }; break;
            case RunRow.ShinyClause: Rules = r with { ShinyClause = !r.ShinyClause }; break;
            case RunRow.GiftsCount: Rules = r with { GiftsCount = !r.GiftsCount }; break;
            case RunRow.Monotype:
                // None first, then the eighteen types, wrapping round
                var types = new List<PokemonType?> { null };
                types.AddRange(Enum.GetValues<PokemonType>().Select(t => (PokemonType?)t));
                int at = ((types.IndexOf(r.Monotype) + d) % types.Count + types.Count) % types.Count;
                Rules = r with { Monotype = types[at] };
                break;
            case RunRow.PartyLimit: Rules = r with { PartyLimit = RunRules.ClampLimit(r.PartyLimit + d) }; break;
            case RunRow.NoBattleItems: Rules = r with { NoBattleItems = !r.NoBattleItems }; break;
            case RunRow.NoCenter: Rules = r with { NoCenter = !r.NoCenter }; break;
            case RunRow.NoCatching: Rules = r with { NoCatching = !r.NoCatching }; break;
            case RunRow.NoRunning: Rules = r with { NoRunning = !r.NoRunning }; break;
            case RunRow.SetStyle: Rules = r with { SetStyle = !r.SetStyle }; break;
            case RunRow.NoLaterMechanics: Rules = r with { NoLaterMechanics = !r.NoLaterMechanics }; break;
            case RunRow.ShinyOdds:
                int k = Array.IndexOf(RunRules.ShinyChoices, r.ShinyDenominator);
                k = ((k + d) % RunRules.ShinyChoices.Length + RunRules.ShinyChoices.Length) % RunRules.ShinyChoices.Length;
                Rules = r with { ShinyDenominator = RunRules.ShinyChoices[k] };
                break;
            case RunRow.RandomSpecies or RunRow.RandomItems or RunRow.RandomAbilities or RunRow.RandomMoves or RunRow.RandomTypes:
                Rules = r with { Random = Toggled(r.Random, SelectedRow) };
                break;
            // Left and right roll another seed; A types one
            case RunRow.Seed when r.Random is { } options: Rules = r with { Random = options with { Seed = RollSeed() } }; break;
            default: return;
        }
        // (A row that was selected among the clauses stays in the list when the Nuzlocke goes)
        selected = Math.Min(selected, Rows.Count - 1);
        First = UiNav.Window(First, selected, Rows.Count, VisibleRows);
    }

    /// <summary>
    /// A: the preset row, START, or any row of a run that is Normal starts the game as it is; another row changes its value
    /// as Right does.
    /// </summary>
    public void Confirm()
    {
        var row = SelectedRow;
        if (EnteringSeed) { PressSeedKey(); return; }
        // A on the seed types one
        if (row == RunRow.Seed) { OpenSeedEntry(); return; }
        if (row is RunRow.Preset or RunRow.Start || Rules.IsNormal) Done = true;
        else Change(1);
    }

    /// <summary>The words for a row's value: a key of the table.</summary>
    public string ValueKey(RunRow row) => row switch
    {
        RunRow.Preset => $"run.preset.{Rules.PresetKey}",
        RunRow.Difficulty => $"run.difficulty.{Rules.Difficulty}",
        RunRow.LevelCap => OnOff(Rules.LevelCap),
        RunRow.ScaleTrainers => OnOff(Rules.ScaleTrainers),
        RunRow.Nuzlocke => OnOff(Rules.Nuzlocke),
        RunRow.DupesClause => OnOff(Rules.DupesClause),
        RunRow.ShinyClause => OnOff(Rules.ShinyClause),
        RunRow.GiftsCount => OnOff(Rules.GiftsCount),
        RunRow.NoBattleItems => OnOff(Rules.NoBattleItems),
        RunRow.NoCenter => OnOff(Rules.NoCenter),
        RunRow.NoCatching => OnOff(Rules.NoCatching),
        RunRow.NoRunning => OnOff(Rules.NoRunning),
        RunRow.SetStyle => OnOff(Rules.SetStyle),
        RunRow.NoLaterMechanics => OnOff(Rules.NoLaterMechanics),
        RunRow.RandomSpecies => OnOff(Rules.Random?.Species == true),
        RunRow.RandomItems => OnOff(Rules.Random?.Items == true),
        RunRow.RandomAbilities => OnOff(Rules.Random?.Abilities == true),
        RunRow.RandomMoves => OnOff(Rules.Random?.Moves == true),
        RunRow.RandomTypes => OnOff(Rules.Random?.Types == true),
        _ => ""
    };

    private static string OnOff(bool on) => on ? "shared.on" : "shared.off";

    /// <summary>A row's value as it is shown: a word, a type, a number.</summary>
    public string ValueText(RunRow row) => row switch
    {
        RunRow.Monotype => Rules.Monotype is { } t ? Words.Type(t) : Text.Of("run.none"),
        RunRow.PartyLimit => Rules.PartyLimit.ToString(),
        RunRow.ShinyOdds => Rules.ShinyDenominator == 0 ? Text.Of("run.shinyOwn") : Text.Of("run.shinyOdds", ("n", Rules.ShinyDenominator)),
        RunRow.Seed => Randomizer.SeedText(Rules.Random?.Seed ?? 0),
        RunRow.Start => "",
        _ => Text.Of(ValueKey(row))
    };
}
