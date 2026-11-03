using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A mark a wild Pokémon carries from the moment it was met (plan 10 · F9; Sword and Shield's marks; not the boxes'
/// <see cref="Pokemon.Marks"/>, which are Platinum's markings). Values are added at the enum's end: a save keeps a
/// mark by its name.
/// </summary>
public enum Mark
{
    Rare,
    // The personality marks, one of which a Pokémon that passes their odds is given, by its personality
    Rowdy, AbsentMinded, Jittery, Excited, Charismatic, Calmness, Intense, ZonedOut, Joyful, Angry, Smiley, Teary,
    Upbeat, Peeved, Intellectual, Ferocious, Crafty, Scowling, Kindly, Flustered, PumpedUp, ZeroEnergy, Prideful,
    Unsure, Humble, Thorny, Vigor, Slump,
    // The time marks, each only in its hours
    Lunchtime, SleepyTime, Dusk, Dawn,
    // The weather marks, each only under its weather
    Cloudy, Rainy, Stormy, Snowy, Blizzard, Dry, Sandstorm, Misty,
    /// <summary>Only for a Pokémon reeled in on a rod.</summary>
    Fishing,
    /// <summary>An alpha's (plan 10 · F10, which gives it; nothing gives it yet).</summary>
    Alpha
}

/// <summary>When and how a wild Pokémon was met, which is all its marks are read from but its personality.</summary>
/// <param name="Time">The time of day it was met at (<see cref="GameClock.Now"/>).</param>
/// <param name="Weather">The weather where it was met (<see cref="Map.WeatherAt(int, int)"/>).</param>
/// <param name="Fished">It was reeled in on a rod.</param>
public readonly record struct MarkMoment(TimeOfDay Time, FieldWeather Weather, bool Fished);

/// <summary>
/// The marks of a catch (plan 10 · F9; docs/mechanics/rulings.md, "Marks and titles"): at most one, tried in the
/// plan's order (rare, personality, time, weather, fishing), each at Sword and Shield's odds (1 in 1000, 100, 50, 50,
/// 25), the time and weather marks only when the hour or the weather is theirs. Nothing is drawn: each try is read
/// from the catch's personality (<see cref="SplitMix.Mix"/> with the try's own salt), so the same Pokémon met at the
/// same moment carries the same mark on every run, and no roll of the game moves. A title is a mark the Pokémon
/// carries, chosen by the player (<see cref="Pokemon.Title"/>), which the send-out lines alone add to its name.
/// </summary>
public static class EncounterMarks
{
    public const int RareOdds = 1000;
    public const int PersonalityOdds = 100;
    public const int TimeOdds = 50;
    public const int WeatherOdds = 50;
    public const int FishingOdds = 25;

    /// <summary>The personality marks in their order, the one a personality picks among.</summary>
    public static IReadOnlyList<Mark> Personalities { get; } =
        Enumerable.Range((int)Mark.Rowdy, Mark.Slump - Mark.Rowdy + 1).Select(i => (Mark)i).ToArray();

    /// <summary>The time mark of a time of day.</summary>
    public static Mark TimeMarkOf(TimeOfDay time) => time switch
    {
        TimeOfDay.Morning => Mark.Dawn,
        TimeOfDay.Day => Mark.Lunchtime,
        TimeOfDay.Twilight => Mark.Dusk,
        _ => Mark.SleepyTime
    };

    /// <summary>The weather mark of a weather, or none for a clear sky (and ash, which is no weather of Sword and Shield's).</summary>
    public static Mark? WeatherMarkOf(FieldWeather weather) => weather switch
    {
        FieldWeather.Cloudy => Mark.Cloudy,
        FieldWeather.Rain or FieldWeather.HeavyRain => Mark.Rainy,
        FieldWeather.Thunderstorm => Mark.Stormy,
        FieldWeather.Snow or FieldWeather.HeavySnow => Mark.Snowy,
        FieldWeather.Blizzard or FieldWeather.Hail => Mark.Blizzard,
        FieldWeather.Drought => Mark.Dry,
        FieldWeather.Sandstorm => Mark.Sandstorm,
        FieldWeather.Fog => Mark.Misty,
        _ => null
    };

    /// <summary>Whether a try passes its odds: a number read from the personality and the try's salt.</summary>
    private static bool Passes(uint personality, uint salt, int odds) => SplitMix.Mix(personality, 0x4D41524Bu, salt) % (uint)odds == 0;

    /// <summary>The mark the encounter gives a Pokémon of this personality, or none.</summary>
    public static Mark? For(uint personality, MarkMoment moment)
    {
        if (Passes(personality, 1, RareOdds)) return Mark.Rare;
        if (Passes(personality, 2, PersonalityOdds))
            return Personalities[(int)(SplitMix.Mix(personality, 0x4D41524Bu, 3) % (uint)Personalities.Count)];
        if (Passes(personality, 4, TimeOdds)) return TimeMarkOf(moment.Time);
        if (WeatherMarkOf(moment.Weather) is { } weather && Passes(personality, 5, WeatherOdds)) return weather;
        if (moment.Fished && Passes(personality, 6, FishingOdds)) return Mark.Fishing;
        return null;
    }

    /// <summary>Gives a caught Pokémon the mark of its encounter, if it has one: as the catch becomes the player's.</summary>
    public static void Give(Pokemon p, MarkMoment moment)
    {
        if (For(p.Personality, moment) is { } mark) p.EncounterMarks.Add(mark.ToString());
    }

    /// <summary>The marks a Pokémon carries, in the enum's order (a name no longer known is passed over).</summary>
    public static IEnumerable<Mark> Of(Pokemon p) =>
        p.EncounterMarks.Select(n => Enum.TryParse<Mark>(n, out var m) ? (Mark?)m : null).OfType<Mark>().OrderBy(m => m);

    /// <summary>Makes one of its marks its title, or none with null; false for a mark it doesn't carry.</summary>
    public static bool ChooseTitle(Pokemon p, Mark? mark)
    {
        if (mark is { } m && !p.EncounterMarks.Contains(m.ToString())) return false;
        p.Title = mark?.ToString();
        return true;
    }

    /// <summary>The word a mark is known by ("Sleepy"), in the language chosen.</summary>
    public static string Word(Mark mark)
    {
        string key = Key(mark);
        return Text.Of($"marks.word.{key}");
    }

    /// <summary>A mark's name ("Sleepy-Time Mark"), in the language chosen.</summary>
    public static string Name(Mark mark) => Text.Of("marks.name", ("word", Word(mark)));

    /// <summary>
    /// The name it is sent out under: its name with its title ("Starly the Sleepy") when one is chosen, its name
    /// otherwise. Only the send-out lines ask it.
    /// </summary>
    public static string SentOutAs(Pokemon p, string name) =>
        p.Title != null && Enum.TryParse<Mark>(p.Title, out var mark) && p.EncounterMarks.Contains(p.Title)
            ? Text.Of("marks.titled", ("pokemon", name), ("word", Word(mark)))
            : name;

    /// <summary>The mark's key in <c>ui.json</c>: its name with a lower first letter.</summary>
    private static string Key(Mark mark)
    {
        string name = mark.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
