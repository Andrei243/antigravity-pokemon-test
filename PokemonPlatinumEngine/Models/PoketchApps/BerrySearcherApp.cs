using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Berry Searcher (<c>berry_searcher/main.c</c>, <c>graphics.c</c>): the map of Sinnoh (<see cref="PoketchMap"/>)
/// with the player's cell and a berry in each cell where a patch has fruit on it (<c>GetReadyBerryPatches</c>: a
/// patch growing and in fruit, by the original's table of where each patch is, <c>sBerryPositions</c>). It looks as
/// it comes up and again whenever its screen is touched, the picture coming back into focus as it does
/// (<c>Task_RefreshMap</c>: a mosaic of six pixels down to none, a size every three frames); in between, only the
/// player's cell moves.
/// </summary>
public sealed class BerrySearcherApp : PoketchAppState
{
    /// <summary>The most cells it shows (<c>MAX_MAP_BERRIES</c>).</summary>
    public const int MaxCells = 64;

    /// <summary>How long the picture takes to come back into focus: six sizes, three frames each.</summary>
    public const float RefreshSeconds = 18f / 60f;

    /// <summary>
    /// Where each patch is shown (<c>sBerryPositions</c>), by its number: cells of the map. The original's table
    /// stops at 118; the last ten patches, which no soil in the world names, are never shown.
    /// </summary>
    public static readonly (int X, int Y)[] PatchCells =
    {
        (5, 20), (5, 20), (6, 20), (6, 20), (6, 19), (6, 19), (7, 17), (7, 17), (7, 17), (7, 17),
        (5, 18), (5, 18), (5, 18), (5, 18), (8, 16), (8, 16), (8, 16), (8, 16), (9, 19), (9, 19),
        (9, 21), (9, 21), (9, 22), (9, 22), (9, 22), (9, 22), (13, 22), (13, 22), (13, 22), (13, 22),
        (16, 22), (16, 22), (17, 21), (17, 21), (17, 20), (17, 20), (17, 20), (17, 20), (17, 19), (17, 19),
        (17, 19), (17, 19), (15, 16), (15, 16), (15, 16), (15, 16), (13, 16), (13, 16), (13, 16), (13, 16),
        (14, 24), (14, 24), (14, 25), (14, 25), (17, 26), (17, 26), (17, 26), (17, 26), (19, 25), (19, 25),
        (19, 25), (19, 25), (20, 25), (20, 25), (20, 25), (20, 25), (22, 20), (22, 20), (22, 20), (22, 20),
        (19, 18), (19, 18), (20, 18), (20, 18), (2, 23), (2, 23), (2, 23), (2, 23), (8, 28), (8, 28),
        (8, 28), (8, 28), (23, 24), (23, 24), (23, 24), (23, 24), (28, 16), (28, 16), (28, 16), (28, 16),
        (19, 13), (20, 13), (20, 13), (20, 13), (19, 10), (19, 10), (19, 10), (19, 10), (21, 10), (21, 10),
        (21, 10), (21, 10), (24, 12), (24, 12), (24, 12), (24, 12), (25, 13), (25, 13), (25, 13), (25, 13),
        (25, 14), (25, 14), (25, 14), (25, 14), (21, 13), (21, 13), (21, 13), (21, 13)
    };

    private static readonly PoketchButton[] screen = { new(0, 0, 0, Columns, Rows) };

    private List<(int X, int Y)>? ready;

    public override PoketchApp App => PoketchApp.BerrySearcher;

    /// <summary>Seconds left of the picture coming back into focus.</summary>
    public float Refreshing { get; private set; }

    /// <summary>How far out of focus the picture is, 1 just touched to 0 sharp.</summary>
    public float Blur => Math.Clamp(Refreshing / RefreshSeconds, 0f, 1f);

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => screen;

    /// <summary>The cells with fruit, as last looked (looked at once as the app comes up).</summary>
    public IReadOnlyList<(int X, int Y)> Ready(PoketchContext context) => ready ??= Look(context.Berries);

    /// <summary>
    /// <c>GetReadyBerryPatches</c>: each patch growing and in fruit gives its cell, the same cell of the patches
    /// after it skipped; at most <see cref="MaxCells"/>.
    /// </summary>
    public static List<(int X, int Y)> Look(BerryPatches? berries)
    {
        var cells = new List<(int X, int Y)>();
        if (berries == null) return cells;
        for (int i = 0; i < PatchCells.Length && i < berries.Patches.Count; i++)
        {
            var patch = berries[i];
            if (!patch.Growing || patch.Stage != BerryStage.Fruit) continue;
            cells.Add(PatchCells[i]);
            // The patches after it in the same cell are passed over, whatever they hold
            while (i + 1 < PatchCells.Length && PatchCells[i + 1] == PatchCells[i]) i++;
            if (cells.Count >= MaxCells) break;
        }
        return cells;
    }

    public override void Press(int button, PoketchContext context)
    {
        // State_UpdateApp: a touch while the picture is still coming back does nothing
        if (Refreshing > 0f) return;
        ready = Look(context.Berries);
        Refreshing = RefreshSeconds;
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context)
    {
        ready ??= Look(context.Berries);
        Refreshing = Math.Max(0f, Refreshing - dt);
    }
}
