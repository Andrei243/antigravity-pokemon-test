using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch on the screen (plan 02 · S2, plan 06 · R14b; style guide, "The Pokétch"): shown over the field and
/// put away with its button, while the player goes on walking, and its side button going from one app to the next.
///
/// The original's lower screen is touched with a stylus while the player walks; here the Pokétch is taken in hand
/// (<see cref="TakeInHand"/>): the player stands still, a cursor goes from one of the app's buttons to the next
/// with the arrows (<see cref="MoveCursor"/>), and the confirm button touches the one it is on
/// (<see cref="Touch"/>), until cancel lets it go. Its state takes no input, so tests and the harness drive it.
/// </summary>
public sealed class PoketchView
{
    public const float SlideSeconds = 0.2f;

    private int cursor;
    private PoketchApp? cursorApp;

    /// <summary>Whether it is out (or coming out).</summary>
    public bool Out { get; private set; }

    /// <summary>How far it has come up, 0 put away to 1 out.</summary>
    public float Shown { get; private set; }

    /// <summary>Whether the player has it in hand, touching its screen: the field waits meanwhile.</summary>
    public bool InHand { get; private set; }

    public void Toggle(Poketch poketch)
    {
        if (!poketch.Enabled) return;
        Out = !Out;
        if (!Out) InHand = false;
        AudioManager.PlaySound("poketch");
    }

    /// <summary>The side button: the next app.</summary>
    public void NextApp(Poketch poketch)
    {
        if (!Out || !poketch.Enabled) return;
        poketch.Next();
        cursor = 0;
        AudioManager.PlaySound("poketch");
    }

    /// <summary>
    /// The Pokétch taken in hand to touch its screen, brought out first if it was away; false (and nothing changes)
    /// when there is no Pokétch, no app or nothing on it to touch.
    /// </summary>
    public bool TakeInHand(Poketch poketch, PoketchContext context)
    {
        if (!poketch.Enabled || poketch.State is not { } app || app.Buttons(context).Count == 0) return false;
        if (!Out) Toggle(poketch);
        InHand = true;
        return true;
    }

    /// <summary>Lets go of the Pokétch: it stays out, and the player walks again.</summary>
    public void LetGo() => InHand = false;

    /// <summary>The button the cursor is on, while in hand; null otherwise or when the app has none.</summary>
    public PoketchButton? Cursor(Poketch poketch, PoketchContext context)
    {
        if (!InHand || poketch.State is not { } app) return null;
        var buttons = app.Buttons(context);
        if (buttons.Count == 0) return null;
        if (cursorApp != app.App)
        {
            cursorApp = app.App;
            cursor = 0;
        }
        return buttons[Math.Clamp(cursor, 0, buttons.Count - 1)];
    }

    /// <summary>
    /// Moves the cursor to the nearest button that lies that way: the one whose middle is closest, counting a step
    /// across the way twice as far as one along it, so a row or a column is followed before anything beside it.
    /// </summary>
    public void MoveCursor(Poketch poketch, PoketchContext context, int dx, int dy)
    {
        if (Cursor(poketch, context) is not { } from || (dx == 0 && dy == 0)) return;
        var buttons = poketch.State!.Buttons(context);
        int best = -1;
        float bestScore = float.MaxValue;
        for (int i = 0; i < buttons.Count; i++)
        {
            float ox = buttons[i].CentreX - from.CentreX, oy = buttons[i].CentreY - from.CentreY;
            float along = ox * dx + oy * dy, across = MathF.Abs(ox * dy) + MathF.Abs(oy * dx);
            if (along <= 0.01f) continue;
            float score = along + 2f * across;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        if (best >= 0) cursor = best;
    }

    /// <summary>The button under the cursor touched.</summary>
    public void Touch(Poketch poketch, PoketchContext context)
    {
        if (Cursor(poketch, context) is { } on) poketch.State!.Press(on.Id, context);
    }

    public void Hide()
    {
        Out = false;
        InHand = false;
        Shown = 0f;
    }

    public void Update(float dt) => Shown = Math.Clamp(Shown + (Out ? dt : -dt) / SlideSeconds, 0f, 1f);

    /// <summary>
    /// The time the watch shows: the computer's clock, or, while the options fix the time of day, a set day at that
    /// hour (so a tool's pictures are the same every run).
    /// </summary>
    public static DateTime Clock() => GameClock.Fixed == null
        ? DateTime.Now
        : new DateTime(2009, 3, 22).AddHours(GameClock.Hour);

    public void Draw(int sw, int sh, Poketch poketch, PoketchContext context)
    {
        if (Shown <= 0f || !poketch.Enabled) return;
        ModernUi.DrawPoketch(sw, sh, poketch, context, Cursor(poketch, context), UI.Kit.UiMotion.EaseOut(Shown));
    }
}
