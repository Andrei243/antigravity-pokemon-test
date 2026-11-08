using System.Collections.Generic;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// A story flag the clock keeps (plan 23, Kalos's last places): set for the hours from <see cref="From"/> to before
/// <see cref="To"/>, while <see cref="Requires"/> (if any) is set, and clear otherwise. Something shown only at an hour (an
/// item ball, a person) is shown by it, and the map changes as the hour turns, wherever the player stands.
/// </summary>
public sealed record ClockFlag(string Flag, int From, int To, string? Requires = null)
{
    /// <summary>Whether the flag is due at <paramref name="hour"/> of a game whose story is <paramref name="story"/>.</summary>
    public bool Holds(int hour, StoryState story) => hour >= From && hour < To && (Requires == null || story.Has(Requires));
}

/// <summary>
/// The flags the clock keeps (<see cref="ClockFlag"/>). Kalos's post-game Mega Stones glint between eight and nine in the
/// evening once the Mega Ring is upgraded, as X and Y's do (<c>docs/mechanics/rulings.md</c>, "Kalos's last places"):
/// their item balls are shown by <see cref="KalosStonesGlint"/>. A flag is written only when it changes, so the story's
/// revision moves twice an evening and never in between. GPU-free.
/// </summary>
public static class ClockFlags
{
    /// <summary>Kalos's post-game Mega Stones are to be seen: the hour of eight in the evening, with the Mega Ring upgraded.</summary>
    public const string KalosStonesGlint = "FLAG_KALOS_STONES_GLINT";

    /// <summary>The hour the stones glint, X and Y's: from 20:00 to 20:59.</summary>
    public const int StonesHour = 20;

    public static IReadOnlyList<ClockFlag> All { get; } = new[]
    {
        new ClockFlag(KalosStonesGlint, StonesHour, StonesHour + 1, "FLAG_KALOS_MEGA_RING_UPGRADED")
    };

    /// <summary>Sets or clears each flag as the hour says; true when any changed.</summary>
    public static bool Keep(StoryState story, int hour)
    {
        bool changed = false;
        foreach (var f in All)
        {
            bool due = f.Holds(hour, story);
            if (due == story.Has(f.Flag)) continue;
            if (due) story.Set(f.Flag);
            else story.Unset(f.Flag);
            changed = true;
        }
        return changed;
    }
}
