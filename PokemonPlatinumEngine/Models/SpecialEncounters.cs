using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What the game remembers of its wild Pokémon beyond the tables (plan 06 · R13), the original's
/// <c>SpecialEncounter</c> save block (<c>src/special_encounter.c</c>): the day's numbers the Great Marsh's and the
/// swarms' Pokémon are drawn from, whether swarms have begun, the Trophy Garden's two, the honey trees, the Poké
/// Radar's battery, the roaming Pokémon and the places the player was last in, and the steps poison counts. No
/// drawing or input; saved whole (<see cref="Core.SaveData.Encounters"/>).
/// </summary>
public sealed class SpecialEncounters
{
    /// <summary>
    /// The number the original keeps for the player's record-mixing group (<c>RecordMixedRNG_GetRand</c>): it moves
    /// on once a day by its own generator (<see cref="Next"/>), and Feebas's tiles, the Great Marsh's daily Pokémon
    /// and the day's swarm are all read from it. The original starts it at the number a group is founded with; here a
    /// new game draws it (there are no groups to found).
    /// </summary>
    public uint DailyNumber { get; set; }

    /// <summary>
    /// The hidden half of the trainer's number (the original's is 32 bits; the card shows the lower 16), drawn for a
    /// new game: with the card's number it picks the four Munchlax trees (<see cref="Overworld.HoneyTrees.MunchlaxTrees"/>).
    /// </summary>
    public int SecretId { get; set; }

    /// <summary>The day's number the Great Marsh's six areas draw their species from (<c>marshDaily</c>).</summary>
    public uint MarshDaily { get; set; }

    /// <summary>The day's number the swarm's place is drawn from (<c>swarmDaily</c>, <see cref="Overworld.Swarms.AreaOf"/>).</summary>
    public uint SwarmDaily { get; set; }

    /// <summary>Whether swarms have begun (<c>SpecialEncounter_EnableSwarms</c>): the assistant's sister tells of them after the Hall of Fame.</summary>
    public bool SwarmsOn { get; set; }

    /// <summary>
    /// The Trophy Garden's two (<c>TrophyGardenMons</c>), each a place in the garden's list of sixteen
    /// (<see cref="WorldEncountersFile.TrophyGarden"/>); null for a slot nobody has brought yet.
    /// </summary>
    public int? TrophyFirst { get; set; }
    public int? TrophySecond { get; set; }

    /// <summary>The 21 honey trees, in the original's order (<see cref="Overworld.HoneyTrees.Areas"/>).</summary>
    public List<HoneyTree> Trees { get; set; } = Enumerable.Range(0, Overworld.HoneyTrees.Count).Select(_ => new HoneyTree()).ToList();

    /// <summary>The tree last slathered (<c>lastSlatheredTree</c>); <see cref="Overworld.HoneyTrees.Count"/> for none.</summary>
    public int LastSlathered { get; set; } = Overworld.HoneyTrees.Count;

    /// <summary>The steps the Poké Radar's battery has charged, up to <see cref="Overworld.RadarChain.BatterySteps"/> (<c>radarCharge</c>).</summary>
    public int RadarCharge { get; set; }

    /// <summary>The roaming Pokémon, one to each of the original's slots (<c>ROAMING_SLOT_*</c>), active or not.</summary>
    public List<Roamer> Roamers { get; set; } = Overworld.Roamers.Slots.Select(s => new Roamer { Species = s.Species, Level = s.Level }).ToList();

    /// <summary>
    /// The place the player is in and the one before it (<c>PlayerRecentRoutes</c>): an area's key, or a hand-made
    /// map's name. A roamer never moves to the one the player has just left.
    /// </summary>
    public string? CurrentPlace { get; set; }
    public string? PreviousPlace { get; set; }

    /// <summary>The steps counted towards poison's next bite in the field, from 0 to 3 (<c>FieldOverworldState_GetPoisonStepCount</c>).</summary>
    public int PoisonSteps { get; set; }

    /// <summary>The minute the honey trees' clocks were last moved on from; null before they ever were.</summary>
    public DateTime? Clock { get; set; }

    /// <summary>
    /// What a new game starts with (<c>SpecialEncounter_Init</c>): the marsh's and the swarm's numbers drawn
    /// (<c>MTRNG_Next</c>), and here the day's number too, which the original leaves at nothing until a group is
    /// founded. Everything else is empty.
    /// </summary>
    public static SpecialEncounters NewGame(Random rng) => new()
    {
        SecretId = rng.Next(1 << 16),
        DailyNumber = Draw(rng),
        MarshDaily = Draw(rng),
        SwarmDaily = Draw(rng)
    };

    private static uint Draw(Random rng) => (uint)rng.NextInt64(1L << 32);

    /// <summary>The record-mixing group's generator (<c>ARNG_Next</c>): × 1,812,433,253 + 1, whole numbers of 32 bits.</summary>
    public static uint Next(uint number) => unchecked(number * 1812433253u + 1u);

    /// <summary>
    /// Days have passed (<c>FieldSystem_HandleDailyEvents</c>): the day's number moves on once for each
    /// (<c>RecordMixedRNG_AdvanceEntries</c>), and the marsh's and the swarm's numbers become it
    /// (<c>SpecialEncounter_SetMixedRecordDailies</c>).
    /// </summary>
    public void DaysPass(int days)
    {
        if (days <= 0) return;
        for (int i = 0; i < days; i++) DailyNumber = Next(DailyNumber);
        MarshDaily = SwarmDaily = DailyNumber;
    }

    /// <summary>The clock has moved on to <paramref name="now"/>: the honey trees count the whole minutes since it was last read. A clock turned back counts nothing.</summary>
    public void ClockTo(DateTime now)
    {
        if (Clock is { } before && now > before)
        {
            int minutes = (int)Math.Min(int.MaxValue, (now - before).TotalMinutes);
            if (minutes <= 0) return;
            Overworld.HoneyTrees.MinutesPass(this, minutes);
            Clock = before.AddMinutes(minutes);
            return;
        }
        Clock = now;
    }

    /// <summary>The player has come to another place (<c>SpecialEncounter_UpdateRecentRoutes</c>): the one they were in becomes the one before.</summary>
    public void Arrive(string place)
    {
        if (CurrentPlace == place) return;
        PreviousPlace = CurrentPlace;
        CurrentPlace = place;
    }
}

/// <summary>
/// One honey tree as the save keeps it (<c>HoneyTree</c>): the minutes its honey has left (a day from slathering;
/// Pokémon come once six hours have passed), which group and slot of the honey trees' tables is waiting at it, and
/// how hard the tree shakes.
/// </summary>
public sealed class HoneyTree
{
    public int MinutesLeft { get; set; }
    public int Slot { get; set; }

    /// <summary>The group drawn: 0 nothing, 1 the common table, 2 the uncommon, 3 the rare (Munchlax).</summary>
    public int Group { get; set; }

    /// <summary>How hard it shakes once Pokémon have come: 0 not at all to 3 (<c>numShakes</c>).</summary>
    public int Shakes { get; set; }
}

/// <summary>
/// A roaming Pokémon as the save keeps it (<c>Roamer</c>): its species and level, whether it roams, where (a place in
/// the roamers' list of routes, <see cref="Overworld.Roamers.Routes"/>), and the Pokémon itself as it was made when
/// it was set loose (its IVs, personality, nature and gender), with the HP and condition its last battle left it.
/// </summary>
public sealed class Roamer
{
    public string Species { get; set; } = "";
    public int Level { get; set; }
    public bool Active { get; set; }
    public int Route { get; set; }
    public Core.SavedPokemonData? Pokemon { get; set; }
}
