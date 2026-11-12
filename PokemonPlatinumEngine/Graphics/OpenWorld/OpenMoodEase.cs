using System;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// A place's own light eased in and out as the player comes and goes (plan 33 · GL6; style guide, "Open world", "A
/// place's own light"): Spikemuth held in the night, Wyndon under the Darkest Day while its flag is set. The light wanted
/// is the area's under the player's feet (<see cref="OpenArea.Light"/>, only while <see cref="OpenArea.LightWhile"/> is
/// set where it names a flag); the share runs to it over <see cref="Seconds"/> of the frames' time, and a light going is
/// eased out before another comes in. GPU-free; nothing of it reaches the rules.
/// </summary>
internal sealed class OpenMoodEase
{
    /// <summary>How long a place's own light takes to come in or go, as a climate's does.</summary>
    public const double Seconds = 1.0;

    /// <summary>The light eased in (kept while it eases out), or null.</summary>
    public string? Mood { get; private set; }

    /// <summary>How far it is in, nought to one.</summary>
    public double Share { get; private set; }

    /// <summary>The light an area asks for with the story as it stands: its own where it has one it may show, else none.</summary>
    public static string? Wanted(OpenArea? area, Func<string, bool> has) =>
        area is { Light: { } light } && OpenLook.IsMood(light) && (area.LightWhile == null || has(area.LightWhile)) ? light : null;

    /// <summary>At once: where the view is first placed (a warp, a load), the light is there or not with no easing.</summary>
    public void Settle(string? wanted)
    {
        Mood = wanted;
        Share = wanted != null ? 1 : 0;
    }

    /// <summary>A frame of <paramref name="seconds"/> toward the light wanted.</summary>
    public void Advance(string? wanted, double seconds)
    {
        double step = Math.Max(0, seconds) / Seconds;
        if (wanted != null && Mood != null && wanted != Mood)
        {
            // Another light first eases the one in force out
            Share = Math.Max(0, Share - step);
            if (Share <= 0) Mood = wanted;
            return;
        }
        if (wanted != null) Mood = wanted;
        Share = wanted != null ? Math.Min(1, Share + step) : Math.Max(0, Share - step);
        if (Share <= 0 && wanted == null) Mood = null;
    }
}
