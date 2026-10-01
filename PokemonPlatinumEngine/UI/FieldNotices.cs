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
    public bool Visible => time >= 0f;

    public void Show(string name)
    {
        Name = name;
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
        if (Visible) ModernUi.DrawLocationSign(Name, Shown);
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
