using System;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>The weather over a battle. Fog only ever comes from the field outside: no move or ability starts it.</summary>
public enum BattleWeather { None, Rain, Sandstorm, Sun, Hail, Fog }

/// <summary>What one side of the field has up: screens, veils and what lies scattered at its feet.</summary>
public sealed class SideState
{
    public int ReflectTurns, LightScreenTurns, MistTurns, SafeguardTurns, TailwindTurns, LuckyChantTurns;

    /// <summary>Layers of Spikes (up to three) and of Toxic Spikes (up to two), and whether Stealth Rock floats there.</summary>
    public int Spikes, ToxicSpikes;
    public bool StealthRock;

    public bool Reflect => ReflectTurns > 0;
    public bool LightScreen => LightScreenTurns > 0;
    public bool Mist => MistTurns > 0;
    public bool Safeguard => SafeguardTurns > 0;
    public bool Tailwind => TailwindTurns > 0;
    public bool LuckyChant => LuckyChantTurns > 0;
}

/// <summary>
/// What waits for a place on the field whoever stands there when it arrives: a Wish, and a Future Sight or Doom
/// Desire on its way.
/// </summary>
public sealed class PlaceState
{
    /// <summary>Turn ends left before the Wish comes true (0: none), and who made it.</summary>
    public int WishTurns;
    public string WishFrom = "";

    /// <summary>Turn ends left before the attack lands (0: none), with the damage it was worked out to do.</summary>
    public int DoomTurns;
    public MoveData? DoomMove;
    public Place DoomFrom;
    public int DoomDamage;
}

/// <summary>
/// The battlefield beyond the Pokémon on it (plan 06 · R3): the weather, Trick Room, Gravity, each side's screens
/// and hazards, and what is on its way to each place. Counters count turn ends still to come, as the original's.
/// </summary>
public sealed class FieldState
{
    public BattleWeather Weather { get; internal set; }

    /// <summary>Turn ends the weather has left; 0 while <see cref="WeatherLasts"/>.</summary>
    public int WeatherTurns { get; internal set; }

    /// <summary>The weather has no end of its own: brought by an ability under Platinum's rules, or by the place the battle is fought in.</summary>
    public bool WeatherLasts { get; internal set; }

    public int TrickRoomTurns { get; internal set; }
    public int GravityTurns { get; internal set; }

    public bool TrickRoom => TrickRoomTurns > 0;
    public bool Gravity => GravityTurns > 0;

    /// <summary>Indexed by <see cref="BattleSide"/>.</summary>
    public SideState[] Sides { get; } = { new(), new() };

    /// <summary>Indexed by a place's number (slot × 2, + 1 for the foe's side).</summary>
    public PlaceState[] Places { get; } = { new(), new(), new(), new() };

    public SideState Side(BattleSide side) => Sides[(int)side];
    public PlaceState At(Place place) => Places[place.Slot * 2 + (place.Side == BattleSide.Enemy ? 1 : 0)];

    /// <summary>Someone on the field has Cloud Nine or Air Lock: the weather is there, and does nothing.</summary>
    internal Func<bool> WeatherIgnored = () => false;

    /// <summary>Someone on the field has used Mud Sport (Electric moves are halved) or Water Sport (Fire moves are).</summary>
    internal Func<bool> MudSport = () => false, WaterSport = () => false;

    /// <summary>How many Pokémon of a side stand on the field and can fight.</summary>
    internal Func<BattleSide, int> Standing = _ => 1;

    /// <summary>The weather as it bears on the battle: none while Cloud Nine or Air Lock is out.</summary>
    public BattleWeather WeatherInEffect => WeatherIgnored() ? BattleWeather.None : Weather;

    /// <summary>Rain, a sandstorm, hail or fog: what weakens Solar Beam and the healing of Morning Sun.</summary>
    public bool WeatherDimsTheSun => WeatherInEffect is BattleWeather.Rain or BattleWeather.Sandstorm or BattleWeather.Hail or BattleWeather.Fog;
}
