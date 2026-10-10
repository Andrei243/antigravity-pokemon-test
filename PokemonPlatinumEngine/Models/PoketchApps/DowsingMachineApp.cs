using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>What a touch of the Dowsing Machine found (<c>DOWSING_RESULT_*</c>).</summary>
public enum DowsingResult { NoItems, FarItems, NearbyItems }

/// <summary>A hidden item the Dowsing Machine shows: its tile from the player's, and how far off it notices it (0 to 2).</summary>
public readonly record struct DowsedItem(int Dx, int Dy, int Range);

/// <summary>
/// The Dowsing Machine (<c>dowsing_machine/main.c</c>, <c>graphics.c</c>): the tiles round the player as a grid,
/// 15 across and 14 down (<c>FieldSystem_GetNearbyHiddenItems</c>: seven to either side, seven up and six down), the
/// player in the middle. Touching a tile sends a ring out from it and looks for the hidden items of the player's place
/// not yet found (<c>FindNearbyHiddenItems</c>): one is shown where it lies if the touch is within its own range of it
/// (8, 24 or 48 of the original's pixels, a tile being 11), at most eight of them; if none is but one is within 48,
/// the ring goes on and on to say something is near. Walking stops it all (<c>State_UpdateApp</c>).
/// </summary>
public sealed class DowsingMachineApp : PoketchAppState
{
    /// <summary>The tiles shown either side of the player, and up and down (<c>playerMinX</c> … <c>playerMaxZ</c>).</summary>
    public const int Left = 7, Right = 7, Up = 7, Down = 6;

    /// <summary>The grid's tiles across and down.</summary>
    public const int Across = Left + Right + 1, Tall = Up + Down + 1;

    /// <summary>A tile of the grid on the screen, in blocks, and where the grid's top left stands: the player's tile in the middle.</summary>
    public const int Cell = 2, GridX = 7, GridY = 4;

    /// <summary>The most items one touch shows (<c>MAX_DOWSING_ITEMS</c>).</summary>
    public const int MaxItems = 8;

    /// <summary>A tile in the original's pixels (<c>GetItemScreenPosition</c>: 11 apart).</summary>
    public const int TilePixels = 11;

    /// <summary>How near a touch must be to an item to show it, by the item's range (<c>sRangeDistances</c>), in pixels.</summary>
    public static readonly int[] RangePixels = { 8, 24, 48 };

    /// <summary>How long the ring takes to spread, and how long the items found blink before it goes out again (our own timings).</summary>
    public const float PingSeconds = 0.6f, ItemSeconds = 0.8f;

    private static readonly PoketchButton[] tiles = BuildTiles();

    private (string? Map, int X, int Y) standing;

    public override PoketchApp App => PoketchApp.DowsingMachine;

    /// <summary>What the last touch found, where it was (a tile from the player's) and the items it shows.</summary>
    public DowsingResult Result { get; private set; }
    public (int Dx, int Dy) Touch { get; private set; }
    public IReadOnlyList<DowsedItem> Items { get; private set; } = Array.Empty<DowsedItem>();

    /// <summary>Whether the ring or the items are on the screen.</summary>
    public bool Pinging { get; private set; }

    /// <summary>Whether the items found are showing (after the ring, for a touch that found some).</summary>
    public bool ShowingItems { get; private set; }

    /// <summary>Seconds into the ring, or into the items' showing.</summary>
    public float Age { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => tiles;

    /// <summary>The button of a tile from the player's.</summary>
    public static int TileId(int dx, int dy) => (dy + Up) * Across + dx + Left;

    public static (int Dx, int Dy) TileOf(int id) => (id % Across - Left, id / Across - Up);

    // Every tile of the grid, the player's own first so the cursor starts there
    private static PoketchButton[] BuildTiles()
    {
        var list = new List<PoketchButton>();
        for (int dy = -Up; dy <= Down; dy++)
            for (int dx = -Left; dx <= Right; dx++)
                list.Add(new PoketchButton(TileId(dx, dy), GridX + (dx + Left) * Cell, GridY + (dy + Up) * Cell, Cell, Cell));
        return list.OrderBy(b => b.Id == TileId(0, 0) ? 0 : 1).ToArray();
    }

    public override void Press(int button, PoketchContext context)
    {
        var (dx, dy) = TileOf(button);
        if (dx < -Left || dx > Right || dy < -Up || dy > Down) return;
        // State_UpdateApp: a touch looks, rings, and starts the ring afresh over one still going
        Find(context, dx, dy);
        context.Sound("dowsing_ping");
        Pinging = true;
        ShowingItems = false;
        Age = 0f;
        standing = (context.Map?.Name, context.X, context.Y);
    }

    /// <summary>
    /// <c>FindNearbyHiddenItems</c> on <c>FieldSystem_GetNearbyHiddenItems</c>: the hidden items of the player's
    /// place in the grid round them whose flag isn't set, each checked from the touch by its own range.
    /// </summary>
    public void Find(PoketchContext context, int touchX, int touchY)
    {
        Touch = (touchX, touchY);
        Result = DowsingResult.NoItems;
        var found = new List<DowsedItem>();
        Items = found;
        if (context.Map is not { } map) return;
        var place = map.AreaAt(context.X, context.Y);
        for (int dy = -Up; dy <= Down; dy++)
            for (int dx = -Left; dx <= Right; dx++)
            {
                int x = context.X + dx, y = context.Y + dy;
                // playerMinX and playerMinZ are kept from going under nought
                if (x < 0 || y < 0) continue;
                if (!map.HiddenItems.TryGetValue((x, y), out var item)) continue;
                if (context.Story?.Has(item.Flag) == true) continue;
                // The original lists the bg events of the player's own map header: here the place's own
                if (map.AreaAt(x, y) != place) continue;
                int range = Math.Clamp(item.Range, 0, RangePixels.Length - 1);
                // FX_Sqrt of the pixels apart, tiles 11 apart: compared squared
                int apart = TilePixels * TilePixels * ((dx - touchX) * (dx - touchX) + (dy - touchY) * (dy - touchY));
                if (apart <= RangePixels[range] * RangePixels[range])
                {
                    if (found.Count < MaxItems)
                    {
                        found.Add(new DowsedItem(dx, dy, range));
                        Result = DowsingResult.NearbyItems;
                    }
                }
                else if (apart <= RangePixels[^1] * RangePixels[^1] && Result == DowsingResult.NoItems)
                    Result = DowsingResult.FarItems;
            }
    }

    public override void Update(float dt, PoketchContext context)
    {
        if (!Pinging) return;
        // PoketchSystem_IsPlayerMoving: a step taken (or another place) stops the ring and the items
        if (standing != (context.Map?.Name, context.X, context.Y))
        {
            Stop();
            return;
        }
        Age += dt;
        if (!ShowingItems && Age >= PingSeconds)
        {
            // Task_StartRadarPing: no items, one ring; far items, the ring again and again; items near, the items,
            // then the ring again (Task_ShowNearbyItems)
            Age -= PingSeconds;
            switch (Result)
            {
                case DowsingResult.NoItems:
                    Stop();
                    break;
                case DowsingResult.NearbyItems:
                    ShowingItems = true;
                    break;
            }
        }
        else if (ShowingItems && Age >= ItemSeconds)
        {
            Age -= ItemSeconds;
            ShowingItems = false;
        }
    }

    /// <summary><c>PoketchDowsingMachineGraphics_StopAllAnimations</c>.</summary>
    public void Stop()
    {
        Pinging = false;
        ShowingItems = false;
        Age = 0f;
    }
}
