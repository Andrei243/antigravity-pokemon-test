using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Light and motion (plan 12 · Q9; style guide, "Move effects (G8)", "Camera (G8)", "Life" and "Motion"): the two
/// options' rows as the whole process holds them, set by <c>GameEngine.ApplySettings</c> alone (like
/// <see cref="Haptics.Strength"/>). GPU-free, so every place that flashes or moves asks it the same way, and a test
/// that changes it runs in the collection "Shared clocks and dice" and puts both back.
/// </summary>
public static class Accessibility
{
    /// <summary>The most any flash over the whole picture may whiten it under <see cref="ReduceFlashes"/>: a quarter.</summary>
    public const float FlashCap = 0.25f;

    /// <summary>Every flash over the picture held to <see cref="FlashCap"/> (the options' REDUCE FLASHES row): off unless chosen.</summary>
    public static bool ReduceFlashes { get; set; }

    /// <summary>
    /// The camera's shakes and punch-ins stilled, the interface's slides snapped to their end, depth of field off, the
    /// sway of grass and leaves stilled and the picture's ripples flat (the options' REDUCE MOTION row): off unless chosen.
    /// </summary>
    public static bool ReduceMotion { get; set; }

    /// <summary>How strong a flash is shown: as asked, or at most <see cref="FlashCap"/> under <see cref="ReduceFlashes"/>.</summary>
    public static float Flash(float amount) => ReduceFlashes ? Math.Min(amount, FlashCap) : amount;

    /// <summary>The share of a motion that plays: 1, or nought under <see cref="ReduceMotion"/>.</summary>
    public static float Motion => ReduceMotion ? 0f : 1f;

    /// <summary>Both rows as the options have them, and <see cref="Graphics.BattleCamera.Motion"/> with them.</summary>
    public static void Use(bool reduceFlashes, bool reduceMotion)
    {
        ReduceFlashes = reduceFlashes;
        ReduceMotion = reduceMotion;
        Graphics.BattleCamera.Motion = reduceMotion ? 0f : 1f;
    }
}
