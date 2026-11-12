using System;
using System.Collections.Generic;
using Raylib_cs;
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

    /// <summary>
    /// The regions the screen can show, one tab each (plan 18 · W9: Fly's choice across the regions entered); a
    /// screen opened on one region alone has that one and no tabs are drawn.
    /// </summary>
    public IReadOnlyList<TownMapState> Tabs { get; private set; } = Array.Empty<TownMapState>();

    /// <summary>The tab shown now, whose region <see cref="State"/> is.</summary>
    public int Tab { get; private set; }

    /// <summary>The tab of the region the player stands in, the one the screen opened on.</summary>
    public int HomeTab { get; private set; }

    /// <summary>Whether there is more than one region to choose between.</summary>
    public bool HasTabs => Tabs.Count > 1;

    public void Open(TownMapState state, TownMapMode mode, Pokemon? flier = null)
    {
        Report = null;
        ReportShown = false;
        scanAsked = false;
        soarAsked = false;
        SoarableTab = null;
        Tabs = new[] { state };
        Tab = HomeTab = 0;
        Show(state);
        Mode = mode;
        Flier = flier;
        chosen = null;
        age = 0f;
        IsActive = true;
        AudioManager.PlaySound("page");
    }

    /// <summary>
    /// Opens on several regions, a tab each, on <paramref name="home"/>'s (plan 18 · W9): Fly's choice once more than
    /// one region can be flown to. One region alone opens as <see cref="Open(TownMapState, TownMapMode, Pokemon)"/> does.
    /// </summary>
    public void Open(IReadOnlyList<TownMapState> tabs, int home, TownMapMode mode, Pokemon? flier = null)
    {
        Open(tabs[home], mode, flier);
        Tabs = tabs;
        Tab = HomeTab = home;
    }

    /// <summary>
    /// Shows the next region's map or the one before (the shoulder buttons; the arrows move the cursor): the cursor
    /// at its first place there, the map whole. It stops at either end. Returns whether the tab changed.
    /// </summary>
    public bool ChangeTab(int step)
    {
        int to = Math.Clamp(Tab + Math.Sign(step), 0, Tabs.Count - 1);
        if (to == Tab || !IsActive) return false;
        Tab = to;
        Show(Tabs[to]);
        AudioManager.PlaySound("page");
        return true;
    }

    // One region's map on the screen: the cursor at its first place, the map whole
    private void Show(TownMapState state)
    {
        State = state;
        (CursorX, CursorY) = state.FirstCursor();
        Zoomed = false;
        sinceMove = zoomAge = 1f;
        across.Release();
        down.Release();
        (shownX, shownY) = ZoomTarget();
    }

    // ---------------------------------------------------------------- the day's report (plan 38 · EX2)

    private bool scanAsked;

    /// <summary>
    /// What the region has today (plan 38 · EX2, <see cref="Overworld.FieldReport"/>): given by the game as the map opens and
    /// again after a scan; null before the region's Hall of Fame is entered, and then the item button does nothing.
    /// </summary>
    public Overworld.FieldReport? Report { get; private set; }

    /// <summary>Whether the report's page is shown in place of the map as Platinum draws it (the first page).</summary>
    public bool ReportShown { get; private set; }

    /// <summary>Whether the screen has a report to turn to: the Town Map and the Pokégear's map card, with a report given.</summary>
    public bool HasReport => Report != null && Mode is TownMapMode.Item or TownMapMode.Card;

    /// <summary>The game hands the screen the region's report (null for none, and the first page comes back).</summary>
    public void SetReport(Overworld.FieldReport? report)
    {
        Report = report;
        if (report == null) ReportShown = false;
    }

    /// <summary>The item button: the report's page, or the map's again. Returns whether the page changed.</summary>
    public bool ToggleReport()
    {
        if (!IsActive || !HasReport) return false;
        ReportShown = !ReportShown;
        AudioManager.PlaySound("page");
        return true;
    }

    /// <summary>A scan was asked for on the report's page (the A button where one may be made), once.</summary>
    public bool TakeScan()
    {
        bool asked = scanAsked;
        scanAsked = false;
        return asked;
    }

    // ---------------------------------------------------------------- soaring (plan 38 · EX7)

    private bool soarAsked;

    /// <summary>
    /// Which tabs may be soared over (plan 38 · EX7), as the game tells it as Fly's choice opens: a region whose Hall of
    /// Fame is set; none before any, and then the item button does nothing in Fly's mode and its hint isn't drawn.
    /// </summary>
    public Func<int, bool>? SoarableTab { get; set; }

    /// <summary>Whether SOAR is offered on the tab shown.</summary>
    public bool CanSoar => Mode == TownMapMode.Fly && SoarableTab?.Invoke(Tab) == true;

    /// <summary>The item button in Fly's mode: SOAR over the region shown, where it may be soared over. Returns whether it was asked.</summary>
    public bool AskSoar()
    {
        if (!IsActive || !CanSoar) return false;
        soarAsked = true;
        IsActive = false;
        AudioManager.PlaySound("select");
        return true;
    }

    /// <summary>SOAR was asked for, once.</summary>
    public bool TakeSoar()
    {
        bool asked = soarAsked;
        soarAsked = false;
        return asked;
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

    // ---------------------------------------------------------------- the pointer (plan 12 · Q6)

    /// <summary>Where the map's picture stands on the screen once the screen has slid in (<c>ModernUi.DrawTownMap</c>).</summary>
    public Rectangle PictureRect()
    {
        var view = State!.Region.View;
        const float MapWidth = 960, MapHeight = 768, Pad = 32;
        float whole = MathF.Floor(Math.Min(MapWidth / view.Width, MapHeight / view.Height));
        return new Rectangle(ModernUi.Margin + Pad + (MapWidth - view.Width * whole) / 2f, ModernUi.ContentTop + Pad + (MapHeight - view.Height * whole) / 2f,
            view.Width * whole, view.Height * whole);
    }

    /// <summary>
    /// The cell of the map under a point, as its column and row from the view's top left times 4096 (row) and 1 (column);
    /// null off the map's picture. A click is the A button on it: Fly's choice there, or the zoom while looking.
    /// </summary>
    public int? HitTest(System.Numerics.Vector2 at)
    {
        if (!IsActive || State == null) return null;
        var picture = PictureRect();
        if (!UiPointer.Inside(picture, at)) return null;
        var view = State.Region.View;
        var (zx, zy) = ZoomShown;
        float shownW = Zoomed ? view.Width / (float)Zoom : view.Width, shownH = Zoomed ? view.Height / (float)Zoom : view.Height;
        float originX = Zoomed ? zx : 0f, originY = Zoomed ? zy : 0f;
        float cell = picture.Width / shownW;
        int cx = (int)MathF.Floor(originX + (at.X - picture.X) / cell), cy = (int)MathF.Floor(originY + (at.Y - picture.Y) / cell);
        if (cx < 0 || cy < 0 || cx >= view.Width || cy >= view.Height) return null;
        return cy * 4096 + cx;
    }

    /// <summary>A hover: the cursor onto the cell under the pointer, held inside the region's bounds.</summary>
    public void PointAt(int index)
    {
        if (State == null || index < 0) return;
        var view = State.Region.View;
        var b = State.Region.Bounds;
        int x = Math.Clamp(view.X + index % 4096, b.Left, b.Right), y = Math.Clamp(view.Y + index / 4096, b.Top, b.Bottom);
        if (x == CursorX && y == CursorY) return;
        (CursorX, CursorY) = (x, y);
        sinceMove = 0f;
        if (tick.Sounds(false)) AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: in Fly's mode it flies to the place under the cursor if that is open (<c>CanFlyToHoveredLocation</c>),
    /// and says no otherwise; looking at the map it changes how near the map is seen.
    /// </summary>
    public void Confirm()
    {
        if (State == null) return;
        // On the report's page the A button scans, where a scan may be made (plan 38 · EX2)
        if (ReportShown && Report != null)
        {
            if (Report.Scan == Overworld.ScanState.Ready)
            {
                scanAsked = true;
                AudioManager.PlaySound("select");
            }
            else AudioManager.PlaySound("error");
            return;
        }
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

        // The shoulders change the region (plan 18 · W9)
        if (HasTabs && InputManager.IsActionPressed(GameAction.PageLeft)) ChangeTab(-1);
        else if (HasTabs && InputManager.IsActionPressed(GameAction.PageRight)) ChangeTab(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) ToggleZoom();
        else if (InputManager.IsActionPressed(GameAction.Item)) { if (!AskSoar()) ToggleReport(); }
    }

    public void Draw(int sw, int sh)
    {
        if (State != null) ModernUi.DrawTownMap(sw, sh, this, Math.Clamp(age / AppearTime, 0f, 1f));
    }
}
