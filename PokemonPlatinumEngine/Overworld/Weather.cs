namespace PokemonPlatinumEngine.Overworld;

/// <summary>What falls or hangs in the air of a place in the field (style guide, "Life").</summary>
public enum FieldWeather
{
    Clear,
    Cloudy,
    Rain,
    HeavyRain,
    Thunderstorm,
    Snow,
    HeavySnow,
    Blizzard,
    Hail,
    Fog,
    Sandstorm,
    Ash
}

public static class Weathers
{
    /// <summary>
    /// The weather of an imported area, by the name its header gives it (Platinum's own list). Five places take
    /// theirs from a calendar in Platinum (<c>sYearlyWeather</c> in the original's
    /// <c>src/field_overworld_weather.c</c>), which the world's <c>calendar.json</c> holds and
    /// <see cref="MapArea.WeatherOn"/> reads; without it, each has the weather it has on most days. The values
    /// that only set a mood indoors (a cave before Flash, the forest's shade) put nothing in the air.
    /// </summary>
    public static FieldWeather Of(string? name) => name switch
    {
        "Cloudy" => FieldWeather.Cloudy,
        "Raining" or "Route212South" => FieldWeather.Rain,
        "HeavyRain" => FieldWeather.HeavyRain,
        "Thunderstorm" => FieldWeather.Thunderstorm,
        "Snowing" or "SnowpointCity" => FieldWeather.Snow,
        "HeavySnow" or "Route216" or "AcuityLakefront" => FieldWeather.HeavySnow,
        "Blizzard" => FieldWeather.Blizzard,
        "Hailing" => FieldWeather.Hail,
        "Fog" or "DeepFog" => FieldWeather.Fog,
        "Sandstorm" => FieldWeather.Sandstorm,
        "SlowAshfall" => FieldWeather.Ash,
        _ => FieldWeather.Clear
    };

    /// <summary>
    /// A day's row in Platinum's calendar (<c>FieldSystem_GetWeather</c>): the table has a row for every day of a
    /// leap year, and in any other year the days from March on are counted one further, past the 29th of February.
    /// </summary>
    public static int CalendarDay(System.DateTime date) =>
        date.DayOfYear - 1 + (date.Month > 2 && !System.DateTime.IsLeapYear(date.Year) ? 1 : 0);

    /// <summary>
    /// The weather a battle fought under this sky opens with and keeps, as the original carries it over
    /// (<c>BattleSystem_TriggerEffectOnSwitch</c>'s field weather): rain of any strength is rain, snow of any
    /// strength is hail, and a sandstorm and fog are themselves.
    /// </summary>
    public static Battle.Sim.BattleWeather InBattle(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain or FieldWeather.HeavyRain or FieldWeather.Thunderstorm => Battle.Sim.BattleWeather.Rain,
        FieldWeather.Snow or FieldWeather.HeavySnow or FieldWeather.Blizzard => Battle.Sim.BattleWeather.Hail,
        FieldWeather.Sandstorm => Battle.Sim.BattleWeather.Sandstorm,
        FieldWeather.Fog => Battle.Sim.BattleWeather.Fog,
        _ => Battle.Sim.BattleWeather.None
    };

    /// <summary>How much harder than a breeze the wind blows: grass and leaves sway that much more.</summary>
    public static float Wind(FieldWeather weather) => weather switch
    {
        FieldWeather.Hail => 0.3f,
        FieldWeather.Rain => 0.5f,
        FieldWeather.HeavyRain => 0.8f,
        FieldWeather.Thunderstorm or FieldWeather.HeavySnow or FieldWeather.Blizzard or FieldWeather.Sandstorm => 1f,
        _ => 0f
    };

    /// <summary>Whether it rains: what an evolution that waits for rain in the field asks.</summary>
    public static bool IsRain(FieldWeather weather) =>
        weather is FieldWeather.Rain or FieldWeather.HeavyRain or FieldWeather.Thunderstorm;

    /// <summary>
    /// How many drops (or hailstones) land in view at each of the thirty beats of a second
    /// (<see cref="Graphics.FieldLife.Rainfall"/>): none for weather that leaves no mark where it lands.
    /// </summary>
    public static int LandsABeat(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain => 12,
        FieldWeather.HeavyRain or FieldWeather.Thunderstorm => 24,
        FieldWeather.Hail => 6,
        _ => 0
    };
}

/// <summary>What shows in the bubble over someone's head (style guide, "Life").</summary>
public enum EmoteBubble
{
    None,
    Exclaim,
    Question,
    Dots,
    Note,
    Heart,
    Sleep,
    Sweat
}
