using System;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokétch on the screen (plan 02 · S2; style guide, "The Pokétch"): shown over the field and put away with its
/// button, while the player goes on walking, and its side button going from one app to the next. Its state takes
/// no input (<see cref="Toggle"/>, <see cref="NextApp"/>), so tests and the harness drive it.
/// </summary>
public sealed class PoketchView
{
    public const float SlideSeconds = 0.2f;

    /// <summary>Whether it is out (or coming out).</summary>
    public bool Out { get; private set; }

    /// <summary>How far it has come up, 0 put away to 1 out.</summary>
    public float Shown { get; private set; }

    public void Toggle(Poketch poketch)
    {
        if (!poketch.Enabled) return;
        Out = !Out;
        AudioManager.PlaySound("poketch");
    }

    /// <summary>The side button: the next app.</summary>
    public void NextApp(Poketch poketch)
    {
        if (!Out || !poketch.Enabled) return;
        poketch.Next();
        AudioManager.PlaySound("poketch");
    }

    public void Hide()
    {
        Out = false;
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

    public void Draw(int sw, int sh, Poketch poketch, Party party)
    {
        if (Shown <= 0f || !poketch.Enabled) return;
        ModernUi.DrawPoketch(sw, sh, poketch, party, Clock(), UI.Kit.UiMotion.EaseOut(Shown));
    }
}
