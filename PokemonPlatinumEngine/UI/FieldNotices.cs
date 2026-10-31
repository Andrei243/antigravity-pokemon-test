using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The sign with a place's name that drops in at the top left when the player arrives there, holds, and lifts
/// away again, as in the games.
/// </summary>
public sealed class LocationSign
{
    public const float SlideTime = 0.35f, HoldTime = 2.4f;

    private float time = -1f;

    public string Name { get; private set; } = "";

    /// <summary>The season told on the sign's second line (Unova's, plan 22 · U3); null where the region has none, and the sign is one line.</summary>
    public Core.Season? Season { get; private set; }

    /// <summary>The sign's second line, or null: the season's name in a region with seasons.</summary>
    public string? SecondLine => Season is { } season ? Core.Words.Season(season) : null;

    public bool Visible => time >= 0f;

    public void Show(string name, Core.Season? season = null)
    {
        Name = name;
        Season = season;
        time = 0f;
    }

    public void Hide() => time = -1f;

    public void Update(float dt)
    {
        if (!Visible) return;
        time += dt;
        if (time >= SlideTime * 2f + HoldTime) time = -1f;
    }

    /// <summary>0 off screen to 1 in place.</summary>
    public float Shown =>
        !Visible ? 0f
        : time < SlideTime ? UiMotion.EaseOut(time / SlideTime)
        : time < SlideTime + HoldTime ? 1f
        : 1f - UiMotion.EaseIn((time - SlideTime - HoldTime) / SlideTime);

    public void Draw()
    {
        if (Visible) ModernUi.DrawLocationSign(Name, Shown, SecondLine);
    }
}

/// <summary>A short notice at the top of the screen ("Game saved."): it drops in, stays a few seconds and lifts away.</summary>
public sealed class Toast
{
    public const float SlideTime = 0.25f, HoldTime = 2.6f;

    private float time = -1f;

    public string Message { get; private set; } = "";
    public bool Visible => time >= 0f;

    public void Show(string message)
    {
        // A notice replacing one that is already up doesn't slide in again
        time = Visible && time < SlideTime + HoldTime ? SlideTime : 0f;
        Message = message;
    }

    public void Update(float dt)
    {
        if (!Visible) return;
        time += dt;
        if (time >= SlideTime * 2f + HoldTime) time = -1f;
    }

    public float Shown =>
        !Visible ? 0f
        : time < SlideTime ? UiMotion.EaseOut(time / SlideTime)
        : time < SlideTime + HoldTime ? 1f
        : 1f - UiMotion.EaseIn((time - SlideTime - HoldTime) / SlideTime);

    public void Draw(int screenWidth)
    {
        if (Visible) ModernUi.DrawToast(screenWidth, Message, Shown);
    }
}

/// <summary>
/// The notice of a medal won (plan 13 · V9; style guide, "Field menus and notices", "A medal won"): a panel with the
/// medal that drops in under the toasts' place, holds and lifts away; medals won together come one after another.
/// Its clock runs only while the engine lets it (the field and its text box), so it waits behind any other screen.
/// </summary>
public sealed class MedalNotice
{
    public const float SlideTime = 0.3f, HoldTime = 3.2f;

    private readonly System.Collections.Generic.Queue<Models.Medal> waiting = new();
    private float time = -1f;

    /// <summary>The medal on show, or null.</summary>
    public Models.Medal? Medal { get; private set; }

    public bool Visible => time >= 0f && Medal != null;

    /// <summary>How many medals wait behind the one on show.</summary>
    public int Waiting => waiting.Count;

    public void Enqueue(Models.Medal medal) => waiting.Enqueue(medal);

    /// <summary>Takes every notice down and forgets those waiting (a game begun or loaded).</summary>
    public void Clear()
    {
        waiting.Clear();
        Medal = null;
        time = -1f;
    }

    /// <summary>Moves the notice on by <paramref name="dt"/>; true when a medal has just begun to drop in (its fanfare's moment).</summary>
    public bool Update(float dt)
    {
        if (Visible)
        {
            time += dt;
            if (time < SlideTime * 2f + HoldTime) return false;
            Medal = null;
            time = -1f;
        }
        if (waiting.Count == 0) return false;
        Medal = waiting.Dequeue();
        time = 0f;
        return true;
    }

    public float Shown =>
        !Visible ? 0f
        : time < SlideTime ? UiMotion.EaseOut(time / SlideTime)
        : time < SlideTime + HoldTime ? 1f
        : 1f - UiMotion.EaseIn((time - SlideTime - HoldTime) / SlideTime);

    public void Draw(int screenWidth)
    {
        if (Visible) ModernUi.DrawMedalNotice(screenWidth, Medal!, Shown);
    }
}
