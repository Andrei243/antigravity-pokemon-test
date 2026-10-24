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
    Ash,
    /// <summary>
    /// Emerald's drought (plan 21 · H8, part b; <c>WEATHER_DROUGHT</c>): Groudon's heat, a hard white sun and nothing
    /// in the air; a battle under it opens in the sun.
    /// </summary>
    Drought,
    /// <summary>
    /// Emerald's abnormal weather (<c>WEATHER_ABNORMAL</c>), while Groudon and Kyogre clash over Sootopolis: a downpour
    /// and a drought by turns (<see cref="Weathers.Alternating"/>). It never leaves <see cref="Map.WeatherAt(int, int)"/>,
    /// which gives the one of the two in force.
    /// </summary>
    Abnormal
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
        // Emerald's two of the titans (plan 21 · H8, part b), named by an overlay's zone
        "Drought" => FieldWeather.Drought,
        "Abnormal" => FieldWeather.Abnormal,
        _ => FieldWeather.Clear
    };

    /// <summary>How long each of the abnormal weather's two turns lasts, in seconds of the field's clock.</summary>
    public const double AbnormalTurn = 10.0;

    /// <summary>
    /// The abnormal weather at a moment of the field's clock (plan 21 · H8, part b): the downpour first, then the drought,
    /// each for <see cref="AbnormalTurn"/>, as Emerald's alternates <c>WEATHER_DOWNPOUR</c> and <c>WEATHER_DROUGHT</c>.
    /// Any other weather is itself.
    /// </summary>
    public static FieldWeather Alternating(FieldWeather weather, double clock) =>
        weather != FieldWeather.Abnormal ? weather
        : ((long)Math.Floor(Math.Max(0, clock) / AbnormalTurn) % 2 == 0 ? FieldWeather.HeavyRain : FieldWeather.Drought);

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
        // Emerald's drought opens a battle in the sun (plan 21 · H8, part b), as its field weather carries it over
        FieldWeather.Drought => Battle.Sim.BattleWeather.Sun,
        _ => Battle.Sim.BattleWeather.None
    };

    /// <summary>
    /// The way back (plan 25 · Y1): the field's weather a battle's comes from, whose light the battle's stage takes
    /// under it (<see cref="Graphics.ArtLook.InBattleWeather"/>): rain as the field's rain, hail as the field's hail,
    /// a sandstorm and fog as themselves. The harsh sun has no weather of the field's and a light of its own: clear.
    /// </summary>
    public static FieldWeather FromBattle(Battle.Sim.BattleWeather weather) => weather switch
    {
        Battle.Sim.BattleWeather.Rain => FieldWeather.Rain,
        Battle.Sim.BattleWeather.Hail => FieldWeather.Hail,
        // The later games' (plan 06 · R19b): snow and the heavy rain as the field's; the harsh sun and the winds clear
        Battle.Sim.BattleWeather.Snow => FieldWeather.Snow,
        Battle.Sim.BattleWeather.HeavyRain => FieldWeather.HeavyRain,
        Battle.Sim.BattleWeather.Sandstorm => FieldWeather.Sandstorm,
        Battle.Sim.BattleWeather.Fog => FieldWeather.Fog,
        _ => FieldWeather.Clear
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
