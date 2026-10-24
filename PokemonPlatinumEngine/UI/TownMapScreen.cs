using System;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The map of the region on the screen (plan 01 · M12; style guide, "The Town Map"; the original's
/// <c>src/applications/town_map</c>): one screen for the Town Map out of the bag, for Fly's choice of a place to land
/// and for a map on a wall (<see cref="TownMapMode"/>). A cursor moves from cell to cell inside the region's bounds
/// and never wraps; the place under it is named and told of; the map is looked at whole or from three times nearer
/// (<see cref="ToggleZoom"/>, the original's lower screen). In Fly's mode the A button on a place that is open flies
/// there (<see cref="TakeChoice"/>). Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>,
/// <see cref="Cancel"/>, <see cref="ToggleZoom"/>, <see cref="Advance"/>), so tests and the harness drive it.
/// </summary>
public sealed class TownMapScreen
{
    /// <summary>How much nearer the zoomed map is.</summary>
    public const int Zoom = 3;

    /// <summary>How long each cell of the way come is the bright one (the original's 26 frames of thirty a second).</summary>
    public const float TrailStepTime = 26f / 30f;

    private const float AppearTime = 0.35f;

    private readonly HeldKey across = new(), down = new();
    private readonly CursorTick tick = new();
    private SpawnLocation? chosen;
    private float age, sinceMove = 1f, zoomAge = 1f;
    private float shownX, shownY;

    public bool IsActive { get; private set; }
    public TownMapMode Mode { get; private set; }

    /// <summary>What the game has put on the map; null until the screen is first opened.</summary>
    public TownMapState? State { get; private set; }

    /// <summary>The cell under the cursor, in the region's grid.</summary>
    public int CursorX { get; private set; }
    public int CursorY { get; private set; }

    /// <summary>Whether the map is looked at from near.</summary>
    public bool Zoomed { get; private set; }

    /// <summary>The Pokémon that uses Fly, one of the team or a wild helper (plan 12 · Q18) (Fly's mode).</summary>
    public Pokemon? Flier { get; private set; }

    public void Open(TownMapState state, TownMapMode mode, Pokemon? flier = null)
    {
        State = state;
        Mode = mode;
        Flier = flier;
        (CursorX, CursorY) = state.FirstCursor();
        Zoomed = false;
        chosen = null;
        age = 0f;
        sinceMove = zoomAge = 1f;
        across.Release();
        down.Release();
        (shownX, shownY) = ZoomTarget();
        IsActive = true;
        AudioManager.PlaySound("page");
    }

    /// <summary>The block under the cursor; null over open sea, forest, and a place the story has not revealed.</summary>
    public RegionBlock? Hovered => State?.BlockAt(CursorX, CursorY);

    /// <summary>Fly's place under the cursor, open or not.</summary>
    public TownMapSpot? HoveredSpot => State?.SpotAt(CursorX, CursorY);

    /// <summary>
    /// Moves the cursor a cell either way or both (the original's <c>HandleInput</c>): it stops at the region's
    /// bounds. Returns whether it moved.
    /// </summary>
    public bool Move(int dx, int dy, bool held = false)
    {
        if (State == null) return false;
        var b = State.Region.Bounds;
        int x = Math.Clamp(CursorX + Math.Sign(dx), b.Left, b.Right), y = Math.Clamp(CursorY + Math.Sign(dy), b.Top, b.Bottom);
        if (x == CursorX && y == CursorY) return false;
        (CursorX, CursorY) = (x, y);
        sinceMove = 0f;
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
        return true;
    }

    /// <summary>
    /// The A button: in Fly's mode it flies to the place under the cursor if that is open (<c>CanFlyToHoveredLocation</c>),
    /// and says no otherwise; looking at the map it changes how near the map is seen.
    /// </summary>
    public void Confirm()
    {
        if (State == null) return;
        if (Mode != TownMapMode.Fly)
        {
            ToggleZoom();
            return;
        }
        if (HoveredSpot is { Open: true } spot)
        {
            chosen = spot.Town;
            IsActive = false;
            AudioManager.PlaySound("select");
        }
        else AudioManager.PlaySound("error");
    }

    public void Cancel()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The whole region, or three times nearer round the cursor (the original's zoom button).</summary>
    public void ToggleZoom()
    {
        Zoomed = !Zoomed;
        zoomAge = 0f;
        (shownX, shownY) = ZoomTarget();
        AudioManager.PlaySound("page");
    }

    /// <summary>The place Fly was asked to land at, once.</summary>
    public SpawnLocation? TakeChoice()
    {
        var town = chosen;
        chosen = null;
        return town;
    }

    /// <summary>
    /// The top left of what the zoomed map shows, in cells from the view's own top left: the cursor in its middle,
    /// held inside the view.
    /// </summary>
    public (float X, float Y) ZoomTarget()
    {
        if (State == null) return (0f, 0f);
        var view = State.Region.View;
        float wide = view.Width / (float)Zoom, high = view.Height / (float)Zoom;
        return (Math.Clamp(CursorX - view.X + 0.5f - wide / 2f, 0f, view.Width - wide),
            Math.Clamp(CursorY - view.Y + 0.5f - high / 2f, 0f, view.Height - high));
    }

    /// <summary>Where the zoomed map's top left is drawn now: it follows <see cref="ZoomTarget"/> smoothly.</summary>
    public (float X, float Y) ZoomShown => (shownX, shownY);

    /// <summary>Which cell of the way come is the bright one now: from the oldest to the latest and round again, as the original shows one dot at a time.</summary>
    public int TrailShown
    {
        get
        {
            int count = State?.Trail.Count ?? 0;
            if (count == 0 || Mode == TownMapMode.Wall) return -1;
            return count - 1 - (int)(age / TrailStepTime) % count;
        }
    }

    /// <summary>How long ago the cursor moved, and the map was last brought nearer or further: what its drawing eases by.</summary>
    public float SinceMove => sinceMove;
    public float SinceZoom => zoomAge;
    public float Age => age;

    /// <summary>Moves the screen's clocks on; no input.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        age += dt;
        sinceMove += dt;
        zoomAge += dt;
        tick.Update(dt);
        var (tx, ty) = ZoomTarget();
        float pull = Math.Min(1f, dt * 16f);
        shownX += (tx - shownX) * pull;
        shownY += (ty - shownY) * pull;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int px = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int py = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        int hx = (InputManager.IsActionDown(GameAction.Right) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Left) ? 1 : 0);
        int hy = (InputManager.IsActionDown(GameAction.Down) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Up) ? 1 : 0);
        int sx = across.Advance(dt, px, hx), sy = down.Advance(dt, py, hy);
        // A key kept down runs on a cell at a time; both ways at once go across the map's diagonal
        for (int step = Math.Max(Math.Abs(sx), Math.Abs(sy)); step > 0; step--)
            Move(Math.Abs(sx) >= step ? sx : 0, Math.Abs(sy) >= step ? sy : 0, held: across.Repeating || down.Repeating);

        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) ToggleZoom();
    }

    public void Draw(int sw, int sh)
    {
        if (State != null) ModernUi.DrawTownMap(sw, sh, this, Math.Clamp(age / AppearTime, 0f, 1f));
    }
}
