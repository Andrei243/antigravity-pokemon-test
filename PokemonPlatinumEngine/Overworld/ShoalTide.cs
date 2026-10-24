namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Shoal Cave's tide (plan 21 · H8, part d; <c>docs/mechanics/rulings.md</c>, "Shoal Cave and its tide"), as Emerald's
/// <c>UpdateShoalTideFlag</c> (<c>src/time_events.c</c>) has it: the tide is high from midnight to three, from nine to
/// three in the afternoon and from nine at night, and low in the six hours between; it is read only as the player comes
/// into the cave from outdoors (<c>IsMapTypeOutdoors(GetLastUsedWarpMapType())</c>), so it never turns while they are
/// inside. <see cref="Flag"/> is set at high tide, and the region's <c>world.json</c> lays the high tide's chunks over
/// the cave's two upper rooms while it is (<c>swaps</c>). The script command <c>tide</c> sets it, run by the entrance
/// room's <c>OnTransition</c> behind the warp's fade.
/// </summary>
public static class ShoalTide
{
    /// <summary>Emerald's flag, set while the tide is in.</summary>
    public const string Flag = "FLAG_SYS_SHOAL_TIDE";

    /// <summary>Emerald's table, an hour to a place: whether the tide is high.</summary>
    private static readonly bool[] High =
    {
        true, true, true, false, false, false, false, false, false, true, true, true,
        true, true, true, false, false, false, false, false, false, true, true, true
    };

    /// <summary>Whether the tide is high at an hour of the day (0 to 23; any other is taken round the clock).</summary>
    public static bool HighAt(int hour) => High[((hour % 24) + 24) % 24];
}
