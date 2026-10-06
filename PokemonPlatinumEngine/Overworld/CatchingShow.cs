using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Pal Park's catching show (plan 06 · R9, from the original's <c>catching_show.c</c>; no drawing or input): six
/// Pokémon brought to the park hide in its grass and water, each in the corner its species keeps to; every few steps
/// one may turn up, by how common its species is; a Park Ball never misses; and when the show ends its score is the
/// species' points, a bonus for catching them so that no two in a row share a type and for every type caught, and
/// points for the time left. Where the six come from (the original moved them from older games) and the park
/// itself wait for their sessions; the show takes any six it is given.
/// </summary>
public sealed class CatchingShow
{
    public const int Size = 6;
    private const int NoEncounterWeight = 20, DifferentTypeBonus = 200, DistinctTypeBonus = 50, MaxSeconds = 1000, PointsLostPerSecond = 2;

    private readonly List<Pokemon> pokemon;
    private readonly int[] caughtOrder = new int[Size];
    private readonly Random rng;
    private int steps;

    /// <summary>The Pokémon the show has, in their order; the one met last is <see cref="Current"/>.</summary>
    public IReadOnlyList<Pokemon> Pokemon => pokemon;
    public int Current { get; private set; } = -1;

    /// <summary>The time points worked out when the show ended (<see cref="End"/>).</summary>
    public int TimePoints { get; private set; }

    public CatchingShow(IEnumerable<Pokemon> six, Random? rng = null)
    {
        pokemon = six.Take(Size).ToList();
        if (pokemon.Count != Size) throw new ArgumentException("The catching show needs six Pokémon");
        this.rng = rng ?? Core.Dice.New();
        ResetSteps();
    }

    /// <summary>How many have been caught so far.</summary>
    public int Caught => caughtOrder.Count(o => o != 0);

    /// <summary>Park Balls left: one for each Pokémon still free (<c>FieldSystem_GetParkBallCount</c>).</summary>
    public int ParkBalls => Size - Caught;

    public bool IsCaught(int index) => caughtOrder[index] != 0;

    /// <summary>
    /// The area a Pokémon hides in: its corner of the field (1 to 4), or of the water (5 to 8) for one that keeps to
    /// it (<c>InitSpeciesData</c>).
    /// </summary>
    public static int AreaOf(PokemonSpecies species)
    {
        var data = species.PalPark;
        if (data == null) return 0;
        return data.LandArea != 0 ? data.LandArea : 4 + data.WaterArea;
    }

    /// <summary>
    /// The area a tile of the park's 64×64 belongs to (<c>GetEncounterArea</c>): its corner, in the grass or on the
    /// water; 0 elsewhere.
    /// </summary>
    public static int AreaAt(int x, int y, bool tallGrass, bool water)
    {
        int quadrant = (x < 32 ? 0 : 1) + (y < 32 ? 0 : 2);
        if (tallGrass) return 1 + quadrant;
        if (water) return 5 + quadrant;
        return 0;
    }

    /// <summary>
    /// A step in the park (<c>CatchingShow_CheckWildEncounter</c>): every 5 to 14 steps a Pokémon of the area may
    /// turn up, each still free one there weighted by its rarity against a share of 20 for nothing. Returns the index
    /// of the one met, or −1.
    /// </summary>
    public int Step(int area)
    {
        if (--steps != 0) return -1;
        ResetSteps();
        if (area == 0) return -1;

        var here = Enumerable.Range(0, Size).Where(i => caughtOrder[i] == 0 && AreaOf(pokemon[i].Species) == area).ToList();
        int total = here.Sum(i => pokemon[i].Species.PalPark!.Rarity);
        if (total == 0) return -1;
        int roll = rng.Next(total + NoEncounterWeight);
        if (roll < NoEncounterWeight) return -1;
        roll -= NoEncounterWeight;
        foreach (int i in here)
        {
            int rarity = pokemon[i].Species.PalPark!.Rarity;
            if (roll < rarity)
            {
                Current = i;
                return i;
            }
            roll -= rarity;
        }
        return -1;
    }

    private void ResetSteps() => steps = rng.Next(10) + 5;

    /// <summary>The battle with <see cref="Current"/> ended in a catch: it is the next caught in order.</summary>
    public void CaughtCurrent()
    {
        if (Current >= 0 && caughtOrder[Current] == 0) caughtOrder[Current] = Caught + 1;
    }

    /// <summary>The show ends after this many seconds: two points for every second under a thousand (<c>CatchingShow_End</c>).</summary>
    public void End(int seconds) => TimePoints = seconds < MaxSeconds ? (MaxSeconds - seconds) * PointsLostPerSecond : 0;

    /// <summary><c>CalcCatchingPoints</c>: the points of all six species, caught or not.</summary>
    public int CatchingPoints => pokemon.Sum(p => p.Species.PalPark?.CatchingPoints ?? 0);

    /// <summary>
    /// <c>CalculateTypePoints</c>: 200 for each catch that shares no type with the one before it, then 50 for each
    /// type among those caught.
    /// </summary>
    public int TypePoints
    {
        get
        {
            int points = 0;
            var types = new HashSet<PokemonType>();
            PokemonType prev1 = default, prev2 = default;
            for (int order = 1; order <= Size; order++)
            {
                int i = Array.IndexOf(caughtOrder, order);
                if (i < 0) continue;
                var p = pokemon[i];
                var t1 = p.PrimaryType;
                var t2 = p.SecondaryType ?? p.PrimaryType;
                if (order != 1 && prev1 != t1 && prev1 != t2 && prev2 != t1 && prev2 != t2) points += DifferentTypeBonus;
                prev1 = t1;
                prev2 = t2;
                types.Add(t1);
                types.Add(t2);
            }
            return points + types.Count * DistinctTypeBonus;
        }
    }

    /// <summary>The show's whole score.</summary>
    public int Score => CatchingPoints + TypePoints + TimePoints;
}
