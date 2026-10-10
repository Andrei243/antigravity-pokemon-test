using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Day-Care Checker (<c>daycare_checker/main.c</c> and <c>graphics.c</c>): the two Pokémon left at the Day Care,
/// each with its level and its gender over it, and an egg between them when the couple has found one. What it shows
/// is read as the app comes up and again each time the screen is touched (<c>LoadDaycareSummary</c>), the picture
/// coming back through a mosaic that clears in 40 frames (<c>Task_ReloadDaycareState</c>).
///
/// The Pokémon come from <see cref="PoketchContext.DayCare"/> (plan 06 · R15), each at the level its steps there have
/// brought it to (<see cref="PoketchContext.DayCareLevels"/>).
/// </summary>
public sealed class DayCareCheckerApp : PoketchAppState
{
    /// <summary>The whole screen: a touch anywhere reads the Day Care again.</summary>
    public static readonly PoketchButton Screen = new(0, 0, 0, Columns, Rows);

    private static readonly PoketchButton[] buttons = { Screen };

    /// <summary>The mosaic's largest step, and the frames each step holds (<c>mosaicSize = 10</c>, <c>mosaicProgress &gt;= 4</c>).</summary>
    public const int MosaicStart = 10, FramesPerStep = 4;

    private const float Frame = 1f / 60f;

    private readonly List<Pokemon> shown = new();
    private bool read;
    private float clock;

    public override PoketchApp App => PoketchApp.DayCareChecker;

    /// <summary>The Pokémon shown, none to two, as last read.</summary>
    public IReadOnlyList<Pokemon> Shown => shown;

    /// <summary>The level each is shown at, as last read: the level its steps at the Day Care have brought it to.</summary>
    public IReadOnlyList<int> Levels => levels;

    private readonly List<int> levels = new();

    /// <summary>Whether an egg is shown between them (<c>Daycare_HasEgg</c>).</summary>
    public bool HasEgg { get; private set; }

    /// <summary>How coarse the picture is while it comes back, <see cref="MosaicStart"/> down to nought (none).</summary>
    public int Mosaic { get; private set; }

    private int mosaicFrames;

    /// <summary>
    /// The level a Pokémon is shown at: its own, at most 100 (<c>SetLevelSprites</c>, which shows the hundreds only
    /// from 100 and the tens only from 10).
    /// </summary>
    public static int LevelOf(Pokemon pokemon) => Math.Clamp(pokemon.Level, 1, 100);

    /// <summary>The figures of a level as shown: no leading noughts.</summary>
    public static string Figures(int level) => Math.Clamp(level, 0, 100).ToString();

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    private void Read(PoketchContext context)
    {
        read = true;
        shown.Clear();
        shown.AddRange(context.DayCare.Take(2));
        levels.Clear();
        for (int i = 0; i < shown.Count; i++) levels.Add(i < context.DayCareLevels.Count ? context.DayCareLevels[i] : LevelOf(shown[i]));
        HasEgg = context.DayCareEgg;
    }

    public override void Press(int button, PoketchContext context)
    {
        // A touch while the picture is still coming back does nothing (State_UpdateApp waits for the task)
        if (Mosaic > 0) return;
        Read(context);
        Mosaic = MosaicStart;
        mosaicFrames = 0;
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context)
    {
        if (!read) Read(context);
        clock = Math.Min(clock + dt, 10 * Frame);
        while (clock >= Frame)
        {
            clock -= Frame;
            if (Mosaic > 0 && ++mosaicFrames >= FramesPerStep)
            {
                mosaicFrames = 0;
                Mosaic--;
            }
        }
    }

    /// <summary>Reads the Day Care now if it hasn't been yet (the drawing asks before the first frame has run).</summary>
    public void EnsureRead(PoketchContext context)
    {
        if (!read) Read(context);
    }
}
