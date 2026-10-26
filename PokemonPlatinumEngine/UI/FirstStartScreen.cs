using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The first start (plan 16 · T24; style guide, "Opening and title screen", "The first start"): on the game's first run
/// only, before the title's notice, one page of the options a player wants before they read a line, then CONTINUE.
/// Its rows are a fixed list of names (<see cref="Wanted"/>), each shown only where the options have a row of that
/// name (<see cref="RowsThatExist"/>): the language, then the sight rows of plan 12 · Q8 and Q9 and the button preset,
/// and last the graphics quality, which plan 16 · T23's measuring will fill in. A change goes through the options'
/// own <see cref="OptionsScreen.Change"/> and is applied by the game at once, so the language or a colour remap shows on
/// the page itself; CONTINUE asks the game to write the options, so the page never comes again. Its logic takes no
/// input (<see cref="Move"/>, <see cref="Change"/>, <see cref="Confirm"/>), so tests and the harness drive it.
/// </summary>
public class FirstStartScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>
    /// The rows the page would show, by the names the options give them, in the page's order. A name the options don't
    /// have yet (a row of a plan not built) is passed over, so each joins the page as its plan lands, with nothing else to change.
    /// </summary>
    public static IReadOnlyList<string> Wanted => new[]
    {
        "Language", "TextSize", "ColourVision", "HighContrast", "ReduceFlashes", "ReduceMotion", "Captions",
        "ButtonMode", "Quality", "DynamicResolution"
    };

    /// <summary>The rows of <paramref name="names"/> the options have, in the order named.</summary>
    public static IReadOnlyList<OptionRow> RowsThatExist(IEnumerable<string> names) =>
        names.Select(n => Enum.TryParse<OptionRow>(n, ignoreCase: false, out var row) && Enum.IsDefined(row) ? row : (OptionRow?)null)
            .Where(r => r.HasValue).Select(r => r!.Value).Distinct().ToList();

    /// <summary>The rows the page shows: those of <see cref="Wanted"/> the options have.</summary>
    public IReadOnlyList<OptionRow> Rows { get; }

    public FirstStartScreen(IReadOnlyList<OptionRow>? rows = null) => Rows = rows ?? RowsThatExist(Wanted);

    public bool IsActive { get; private set; }

    /// <summary>The row under the cursor: one of <see cref="Rows"/>, or <see cref="Rows"/>.Count for CONTINUE.</summary>
    public int Cursor { get; private set; }

    /// <summary>Whether the cursor is on CONTINUE, the page's last row.</summary>
    public bool OnContinue => Cursor == Rows.Count;

    /// <summary>The row under the cursor, or null on CONTINUE.</summary>
    public OptionRow? Selected => OnContinue ? null : Rows[Cursor];

    /// <summary>The rows the page shows at once, CONTINUE among them; more scroll in a window.</summary>
    public const int VisibleRows = 7;

    /// <summary>The first row in the window.</summary>
    public int First { get; private set; }

    /// <summary>How far the page has come in, 0 to 1.</summary>
    public float Appear { get; private set; }

    private bool continued;

    public void Open()
    {
        IsActive = true;
        Cursor = 0;
        First = 0;
        Appear = 0f;
        continued = false;
    }

    public void Close() => IsActive = false;

    public void Advance(float dt) => Appear = Math.Min(1f, Appear + dt / AppearTime);

    /// <summary>One step of the cursor up or down the rows and CONTINUE, wrapping round.</summary>
    public void Move(int dy)
    {
        int count = Rows.Count + 1;
        Cursor = ((Cursor + dy) % count + count) % count;
        First = UiNav.Window(First, Cursor, count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// Left or right on the row under the cursor: its setting moves a step through the options' own
    /// <see cref="OptionsScreen.Change"/>; true when something changed, which the game applies at once. Nothing on CONTINUE.
    /// </summary>
    public bool Change(GameSettings settings, int step)
    {
        if (Selected is not { } row || step == 0) return false;
        OptionsScreen.Change(settings, row, step);
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>
    /// The A button: on CONTINUE the page is done (true: the game writes the options and goes on to the title); on a
    /// row it changes the row as right does.
    /// </summary>
    public bool Confirm(GameSettings settings)
    {
        if (!OnContinue)
        {
            Change(settings, 1);
            return false;
        }
        AudioManager.PlaySound("select");
        IsActive = false;
        continued = true;
        return true;
    }

    /// <summary>Whether CONTINUE was pressed since the last ask, said once.</summary>
    public bool TakeContinue()
    {
        bool was = continued;
        continued = false;
        return was;
    }

    /// <summary>
    /// The keys, for the game: up and down move, left and right change, A changes or continues. B does nothing: the page
    /// is left by CONTINUE alone. Returns true when a setting changed.
    /// </summary>
    public bool Update(GameSettings settings, float dt)
    {
        if (!IsActive) return false;
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) return Change(settings, -1);
        else if (InputManager.IsActionPressed(GameAction.Right)) return Change(settings, 1);
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            bool onRow = !OnContinue;
            Confirm(settings);
            return onRow;
        }
        return false;
    }

    /// <summary>A row as the page draws it: the options' own label, value and help.</summary>
    public static (string Label, string Value, string Help) Describe(GameSettings settings, OptionRow row) => OptionsScreen.Describe(settings, row);

    public void Draw(int sw, int sh, GameSettings settings)
    {
        if (!IsActive) return;
        ModernUi.DrawFirstStart(sw, sh, this, settings);
    }
}
