using System;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// Motion for the interface (style guide, "Motion"): things slide in with an ease-out and leave a little faster;
/// nothing bounces. Free of GPU calls, so the timings are tested.
/// </summary>
public static class UiMotion
{
    /// <summary>Fast at first, settling gently: for things arriving.</summary>
    public static float EaseOut(float t)
    {
        t = 1f - Math.Clamp(t, 0f, 1f);
        return 1f - t * t * t;
    }

    /// <summary>Slow at first, then fast: for things leaving.</summary>
    public static float EaseIn(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * t;
    }

    /// <summary>Gentle at both ends.</summary>
    public static float EaseInOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Moves a value toward a target at a fixed rate without passing it.</summary>
    public static float Toward(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
}

/// <summary>
/// Whether something is on screen, as a number: 0 hidden, 1 fully shown. It opens over <see cref="OpenTime"/> and
/// closes over <see cref="CloseTime"/>; <see cref="Shown"/> is the eased amount to slide or scale by.
/// </summary>
public sealed class UiReveal
{
    private float amount;

    public float OpenTime { get; }
    public float CloseTime { get; }
    public bool IsOpen { get; private set; }

    public UiReveal(float openTime = 0.22f, float closeTime = 0.14f)
    {
        OpenTime = openTime;
        CloseTime = closeTime;
    }

    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;

    /// <summary>Shows or hides it at once, without the slide.</summary>
    public void Snap(bool open)
    {
        IsOpen = open;
        amount = open ? 1f : 0f;
    }

    public void Update(float dt) =>
        amount = UiMotion.Toward(amount, IsOpen ? 1f : 0f, dt / Math.Max(0.001f, IsOpen ? OpenTime : CloseTime));

    /// <summary>True while any of it is still on screen.</summary>
    public bool Visible => amount > 0f;

    /// <summary>0 hidden to 1 in place, eased out on the way in and eased in on the way out.</summary>
    public float Shown => IsOpen ? UiMotion.EaseOut(amount) : 1f - UiMotion.EaseIn(1f - amount);
}

/// <summary>Cursor movement in menus laid out as a grid, filled row by row.</summary>
public static class UiNav
{
    /// <summary>
    /// The slot reached from <paramref name="index"/> by one step. Rows and columns wrap round, skipping the
    /// empty slots of a last row that isn't full.
    /// </summary>
    public static int Grid(int index, int count, int columns, int dx, int dy)
    {
        if (count <= 0) return 0;
        columns = Math.Max(1, columns);
        int rows = (count + columns - 1) / columns;
        int col = index % columns, row = index / columns;

        for (int tries = 0; tries < Math.Max(rows, columns); tries++)
        {
            if (dx != 0) col = ((col + Math.Sign(dx)) % columns + columns) % columns;
            if (dy != 0) row = ((row + Math.Sign(dy)) % rows + rows) % rows;
            int next = row * columns + col;
            if (next < count) return next;
        }
        return index;
    }
}
