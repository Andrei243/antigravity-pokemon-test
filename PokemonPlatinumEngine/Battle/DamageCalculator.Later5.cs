using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The later moves of Generation 9 that hit with other numbers (plan 06 · R26b; Showdown's data/moves.ts and
// data/conditions.ts). Glaive Rush's price is read in both formulas from its user's Volatiles.GlaiveRush.
public static partial class DamageCalculator
{
    /// <summary>
    /// Hydro Steam is half as strong again in the sun, where any other Water move is half as strong (Showdown's
    /// sunnyday <c>onWeatherModifyDamage</c>): both formulas take it as a Fire move's weather.
    /// </summary>
    public static bool SteamsInTheSun(MoveData move, BattleWeather weather) =>
        move.Effect == "HydroSteam" && weather is BattleWeather.Sun or BattleWeather.HarshSun;
}
