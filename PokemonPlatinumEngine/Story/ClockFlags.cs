using System.Collections.Generic;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// A story flag the clock keeps (plan 23, Kalos's last places): set for the hours from <see cref="From"/> to before
/// <see cref="To"/> (a range with <see cref="To"/> before <see cref="From"/> runs through midnight), while <see cref="Requires"/> (if any) is set, and clear otherwise. Something shown only at an hour (an
/// item ball, a person) is shown by it, and the map changes as the hour turns, wherever the player stands.
/// </summary>
public sealed record ClockFlag(string Flag, int From, int To, string? Requires = null)
{
    /// <summary>Whether the flag is due at <paramref name="hour"/> of a game whose story is <paramref name="story"/>.</summary>
    public bool Holds(int hour, StoryState story) =>
        (From <= To ? hour >= From && hour < To : hour >= From || hour < To) && (Requires == null || story.Has(Requires));
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

    /// <summary>
    /// Alola's cells that show by day or by night only (plan 23, the day and night cells; rulings, "Sharpedo's currents, Mantine
    /// Surf, ..."): a cell of the hundred an overlay shows by <see cref="Daylight"/> stands from six in the morning to six in
    /// the evening, one shown by <see cref="Nightfall"/> from then to six, and neither is there before the Zygarde Cube is
    /// had (<see cref="CubeGiven"/>), which is what the cells are for.
    /// </summary>
    public const string Daylight = "FLAG_ALOLA_DAYLIGHT", Nightfall = "FLAG_ALOLA_NIGHTFALL", CubeGiven = "FLAG_ALOLA_GOT_ZYGARDE_CUBE";

    public static IReadOnlyList<ClockFlag> All { get; } = new[]
    {
        new ClockFlag(KalosStonesGlint, StonesHour, StonesHour + 1, "FLAG_KALOS_MEGA_RING_UPGRADED"),
        new ClockFlag(Daylight, 6, 18, CubeGiven),
        new ClockFlag(Nightfall, 18, 6, CubeGiven)
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
