using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The map of Sinnoh the Pokétch's map apps share (<c>poketch_map.c</c>, <c>PoketchMap_GetPlayerLocation</c> in
/// <c>inlines.h</c>): one cell for each chunk of 32 by 32 tiles of the overworld, as the Pokédex's area page and Fly
/// lay the region out (<c>Habitats.LookAt</c>, a chunk a square). The original draws columns 0 to 29 and rows 5 on
/// (its <c>mapPositionsY</c> puts the first five at nought: the empty north); here the rows run from 5 to 29, the
/// last with any ground. On the screen a cell is one block, the map 30 blocks by 25 with its top left at
/// <see cref="Left"/>, <see cref="Top"/>. What the original gives in its screen's pixels (where a roamer's route or a
/// hidden island stands) is turned into cells by the inverse of <c>mapPositionsX</c> and <c>mapPositionsY</c>
/// (a cell <c>x</c> is at pixel <c>26 + 6x</c>, a cell <c>y</c> at <c>24 + 6(y − 5)</c>), to the nearest cell.
/// No drawing or input here.
/// </summary>
public static class PoketchMap
{
    /// <summary>The map's cells: columns 0 to 29, rows <see cref="FirstRow"/> to 29.</summary>
    public const int Columns = 30, FirstRow = 5, Rows = 25;

    /// <summary>Where the map's first cell stands on the screen, in blocks: the map in the middle of the 45 by 37.</summary>
    public const int Left = 7, Top = 6;

    /// <summary>The overworld's map: the only one whose tiles are a place on the map of Sinnoh.</summary>
    public const string Overworld = "Sinnoh";

    /// <summary>Whether a cell is on the map.</summary>
    public static bool OnMap(int x, int y) => x >= 0 && x < Columns && y >= FirstRow && y < FirstRow + Rows;

    /// <summary>
    /// The cell the player is in (<c>PoketchMap_GetPlayerLocation</c>): their tile over 32 on the overworld. The
    /// original falls back on the place the player last went in from off the main matrix; the Pokétch's context
    /// doesn't carry that place, so off the overworld the player is nowhere on the map (null).
    /// </summary>
    public static (int X, int Y)? PlayerCell(PoketchContext context)
    {
        if (context.Map?.Name != Overworld) return null;
        int x = context.X / 32, y = context.Y / 32;
        return OnMap(x, y) ? (x, y) : null;
    }

    /// <summary>A position in the original's pixels as a cell (the inverse of <c>PoketchMap_GetPositionOnMap</c>).</summary>
    public static (int X, int Y) CellOfPixel(int px, int py) =>
        ((int)MathF.Floor((px - 26) / 6f + 0.5f), (int)MathF.Floor((py - 24) / 6f + 0.5f) + FirstRow);

    // PoketchMap_GetPositionFromMapID: where each of the roamers' 29 places is drawn, in the order of
    // Roamers.Routes (MAP_HEADER_ROUTE_201 … ROUTE_222, VALLEY_WINDWORKS_OUTSIDE, FUEGO_IRONWORKS_OUTSIDE)
    private static readonly (int X, int Y)[] RoamerPixels =
    {
        (47, 150), (56, 144), (65, 132), (50, 126), (50, 120), (62, 108), (74, 90), (80, 111), (83, 126), (101, 126),
        (125, 126), (128, 102), (122, 90), (92, 90), (104, 90), (110, 138), (119, 150), (152, 147), (152, 120),
        (140, 102), (86, 66), (80, 51), (41, 132), (56, 156), (59, 162), (74, 162), (170, 138), (68, 114), (56, 102)
    };

    /// <summary>The cell a roamer in one of <see cref="Roamers.Routes"/> is shown in.</summary>
    public static (int X, int Y)? RouteCell(int route) =>
        route >= 0 && route < RoamerPixels.Length ? CellOfPixel(RoamerPixels[route].X, RoamerPixels[route].Y) : null;

    /// <summary>
    /// The cells of the roamers on the loose (<c>Roamer_GetData(ROAMER_DATA_ACTIVE)</c>, <c>ROAMER_DATA_MAP_ID</c>),
    /// by slot: the original's <c>ROAMING_SLOT_MAX</c>.
    /// </summary>
    public static IEnumerable<(int Slot, int X, int Y)> RoamerCells(SpecialEncounters? encounters)
    {
        if (encounters == null) yield break;
        for (int slot = 0; slot < encounters.Roamers.Count; slot++)
            if (encounters.Roamers[slot] is { Active: true } roamer && RouteCell(roamer.Route) is var (x, y))
                yield return (slot, x, y);
    }

    /// <summary>
    /// The four hidden places (<c>HIDDEN_LOCATION_*</c>: Fullmoon Island, Newmoon Island, the Spring Path, the
    /// Seabreak Path), each shown once its variable holds its magic number (<c>SystemVars_CheckHiddenLocation</c>).
    /// </summary>
    public static readonly (string Variable, int Magic, int Px, int Py)[] HiddenLocations =
    {
        ("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND", 0x0208, 32, 42),
        ("VAR_HIDDEN_LOCATION_NEW_MOON_ISLAND", 0x0229, 50, 42),
        ("VAR_HIDDEN_LOCATION_SPRING_PATH", 0x0312, 168, 122),
        ("VAR_HIDDEN_LOCATION_SEABREAK_PATH", 0x1028, 194, 58)
    };

    /// <summary>Whether a hidden place is on the map: its variable set to its magic number.</summary>
    public static bool Shows(StoryState? story, int hidden) =>
        story != null && story.Var(HiddenLocations[hidden].Variable) == HiddenLocations[hidden].Magic;

    /// <summary>A hidden place's cell (<c>PoketchMap_GetHiddenLocationPosition</c>).</summary>
    public static (int X, int Y) HiddenCell(int hidden) => CellOfPixel(HiddenLocations[hidden].Px, HiddenLocations[hidden].Py);

    /// <summary>Every cell of the map as a button of one block, the middle first (where the cursor starts), then by rows.</summary>
    internal static PoketchButton[] CellButtons()
    {
        var cells = new List<PoketchButton>();
        for (int y = FirstRow; y < FirstRow + Rows; y++)
            for (int x = 0; x < Columns; x++)
                cells.Add(new PoketchButton(Id(x, y), Left + x, Top + y - FirstRow, 1, 1));
        int middle = Id(Columns / 2, FirstRow + Rows / 2);
        return cells.OrderBy(b => b.Id == middle ? 0 : 1).ToArray();
    }

    /// <summary>A cell's button: its place in the map's rows.</summary>
    public static int Id(int x, int y) => (y - FirstRow) * Columns + x;

    public static (int X, int Y) CellOf(int id) => (id % Columns, FirstRow + id / Columns);
}

/// <summary>
/// The Marking Map (<c>marking_map/main.c</c>): the map of Sinnoh (<see cref="PoketchMap"/>) with the player's
/// cell, the roamers on the loose and six markers of the player's own, kept by the Pokétch
/// (<c>Poketch_SetMapMarker</c>; here in its memory) where they were last put, starting in a row at the bottom right
/// (<c>sDefaultMapMarkers</c>). The original drags a marker with the stylus; here every cell of the map is a button:
/// touching a marker's cell picks it up (<c>State_Idle</c>: the marker within eight pixels of the touch, the one moved
/// last first) and touching any cell puts it down there (<c>State_MovingMarker</c>).
/// </summary>
public sealed class MarkingMapApp : PoketchAppState
{
    /// <summary>The markers (<c>NUM_MARKING_MAP_ICONS</c>).</summary>
    public const int Markers = 6;

    /// <summary>
    /// The markers' first places (<c>sDefaultMapMarkers</c>: 104 to 184 across by 16, 152 down, from the screen's
    /// corner at 16, 16) as cells: a row in the sea south of the mainland.
    /// </summary>
    public static readonly (int X, int Y)[] DefaultMarkers =
        Enumerable.Range(0, Markers).Select(i => PoketchMap.CellOfPixel(104 + 16 * i + 16, 152 + 16)).ToArray();

    private static readonly PoketchButton[] cells = PoketchMap.CellButtons();

    private readonly (int X, int Y)[] markers = DefaultMarkers.ToArray();

    // markerOrder: the markers from the one moved last, which a touch finds first and which is drawn on top
    private readonly List<int> order = Enumerable.Range(0, Markers).ToList();

    public override PoketchApp App => PoketchApp.MarkingMap;

    /// <summary>Each marker's cell.</summary>
    public IReadOnlyList<(int X, int Y)> Positions => markers;

    /// <summary>The markers from the one moved last to the one moved longest ago.</summary>
    public IReadOnlyList<int> Order => order;

    /// <summary>The marker picked up and waiting to be put down; null when none is.</summary>
    public int? Carried { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => cells;

    protected override void Opened()
    {
        // Poketch_MapMarkerPos, read as the app starts
        if (Owner.Recall(App) is { Count: >= Markers * 2 } kept)
            for (int i = 0; i < Markers; i++)
                if (PoketchMap.OnMap(kept[2 * i], kept[2 * i + 1])) markers[i] = (kept[2 * i], kept[2 * i + 1]);
    }

    public override void Press(int button, PoketchContext context)
    {
        var (x, y) = PoketchMap.CellOf(button);
        if (!PoketchMap.OnMap(x, y)) return;
        if (Carried is { } carried)
        {
            // The stylus lifted: the marker stays where it is put (State_MovingMarker), and the Pokétch keeps it
            // (Poketch_SetMapMarker, which the original calls as the app closes)
            markers[carried] = (x, y);
            Carried = null;
            Keep();
            context.Sound("poketch");
            return;
        }
        // State_Idle: the first marker, in the order they were last moved, within eight pixels of the touch either
        // way: a cell is six pixels, so the touched cell and those round it
        foreach (int m in order)
        {
            if (Math.Abs(markers[m].X - x) > 1 || Math.Abs(markers[m].Y - y) > 1) continue;
            // UpdateMarkerPriorities: the marker touched goes to the front
            order.Remove(m);
            order.Insert(0, m);
            Carried = m;
            context.Sound("poketch");
            return;
        }
    }

    private void Keep()
    {
        var kept = new List<int>(Markers * 2);
        foreach (var (x, y) in markers)
        {
            kept.Add(x);
            kept.Add(y);
        }
        Owner.Keep(App, kept);
    }
}
