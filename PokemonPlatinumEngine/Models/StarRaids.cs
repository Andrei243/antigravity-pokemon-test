using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One of Team Star's five bases on Starfall Street (plan 34 · PD5; Scarlet and Violet's, our own words): its key (the
/// boss's, as the scripts and flags name it), the crew's type, the Pokémon its gate's yard is full of with their levels,
/// and the boss's trainer of <c>Data/world/paldea/trainers.json</c>, whose team ends with the Starmobile.
/// </summary>
public sealed record StarBase(string Key, string Crew, IReadOnlyList<string> Species, int MinLevel, int MaxLevel, string Boss)
{
    /// <summary>The marker of the yard's middle before the gate (the place's file).</summary>
    public string Yard => Key + "_yard";

    /// <summary>The marker at the gate's mouth, where a raid begins and where a raid lost puts the player back.</summary>
    public string Gate => Key + "_gate";

    /// <summary>The script the game starts once the raid is won, and once it is lost (each in <see cref="StarBases.File"/>).</summary>
    public string WonScript => Capital + "RaidWon";
    public string LostScript => Capital + "RaidLost";

    private string Capital => char.ToUpperInvariant(Key[0]) + Key[1..];
}

/// <summary>The five bases, in the order the academy suggests them.</summary>
public static class StarBases
{
    /// <summary>The scripts' file and the open area the five yards stand in (a stand-in place until plan 34 · PD6).</summary>
    public const string File = "starfall_street";

    public static readonly IReadOnlyList<StarBase> All = new[]
    {
        new StarBase("giacomo", "Dark", new[] { "Murkrow", "Houndour", "Maschiff", "Impidimp" }, 14, 18, "paldea_star_giacomo"),
        new StarBase("mela", "Fire", new[] { "Litleo", "Growlithe", "Charcadet", "Numel" }, 20, 24, "paldea_star_mela"),
        new StarBase("atticus", "Poison", new[] { "Shroodle", "Gulpin", "Grimer", "Varoom" }, 26, 30, "paldea_star_atticus"),
        new StarBase("ortega", "Fairy", new[] { "Fidough", "Flabébé", "Tinkatink", "Impidimp" }, 42, 46, "paldea_star_ortega"),
        new StarBase("eri", "Fighting", new[] { "Makuhita", "Mankey", "Riolu", "Meditite" }, 48, 52, "paldea_star_eri")
    };

    public static StarBase? Get(string key) => All.FirstOrDefault(b => string.Equals(b.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>A Pokémon of a base's yard standing on one of the ring's spots during a raid.</summary>
public sealed class StarRaidFoe
{
    public int Id { get; init; }
    public required string Species { get; init; }
    public int Level { get; init; }
    public int Spot { get; init; }
    public OpenPos Pos { get; init; }
    public int Yaw { get; init; }
}

/// <summary>
/// A raid on a Team Star base's gate (plan 34 · PD5; Scarlet and Violet's "Star Barrage", our own numbers): the player's
/// first three Pokémon able to fight are sent by Let's Go, one press at a time and each in turn, at the crew's Pokémon
/// standing on <see cref="Spots"/> spots of a ring round the yard's middle, and <see cref="Goal"/> must be felled before
/// the clock's <see cref="Seconds"/> run out. The clock is the open field's own ticks (<see cref="OpenClock"/>), never
/// the wall clock; a felled foe's spot stands empty a moment and is filled again. Which species and level fills a spot is
/// drawn from a generator of the raid's own, seeded by the base's key and how many have stood, never <c>Dice</c>.
/// Won or lost, the engine starts the base's script (<see cref="StarBase.WonScript"/>, <see cref="StarBase.LostScript"/>).
/// GPU-free.
/// </summary>
public sealed class StarRaid
{
    /// <summary>Foes to fell, the clock's length, the spots of the ring and its radius in metres, a spot's wait once emptied.</summary>
    public const int Goal = 15, Seconds = 300, Spots = 6, RefillTicks = 2 * OpenClock.TicksPerSecond;
    public const double Radius = 9.0;

    /// <summary>How near a foe must stand for Let's Go to reach it in a raid, in millimetres.</summary>
    public const int Reach = 16_000;

    public StarBase Base { get; }
    public OpenPos Middle { get; }
    public int TicksLeft { get; private set; }
    public int Felled { get; private set; }

    /// <summary>The foes standing now, by spot (null where a spot is empty).</summary>
    public IReadOnlyList<StarRaidFoe?> Standing => standing;

    public bool Won => Felled >= Goal;
    public bool Lost => !Won && TicksLeft <= 0;
    public bool Over => Won || Lost;

    /// <summary>The seconds left on the clock, rounded up (what the corner shows).</summary>
    public int SecondsLeft => (TicksLeft + OpenClock.TicksPerSecond - 1) / OpenClock.TicksPerSecond;

    private readonly StarRaidFoe?[] standing = new StarRaidFoe?[Spots];
    private readonly int[] emptyFor = new int[Spots];
    private readonly Func<double, double, double, double?> groundAt;
    private int made, lead;

    /// <summary>
    /// A raid begun at a base's yard. <paramref name="groundAt"/> gives the ground's height at a place (x, z, near a
    /// height), all in metres, or null where nothing stands: the map's own ground in the game, a flat one in a test.
    /// </summary>
    public StarRaid(StarBase @base, OpenPos middle, Func<double, double, double, double?> groundAt)
    {
        Base = @base;
        Middle = middle;
        this.groundAt = groundAt;
        TicksLeft = Seconds * OpenClock.TicksPerSecond;
        for (int spot = 0; spot < Spots; spot++) Fill(spot);
    }

    /// <summary>The raid on the open map's ground (the yard's marker its middle).</summary>
    public static StarRaid On(OpenMap map, StarBase @base, OpenPos middle) =>
        new(@base, middle, (x, z, y) => map.Ground.Footing(x, z, y + 3, y - 3, walkableOnly: true)?.Y);

    /// <summary>A tick of the field's clock: the clock runs down and an empty spot whose wait is over is filled. Nothing once the raid is over.</summary>
    public void Tick()
    {
        if (Over) return;
        TicksLeft--;
        for (int spot = 0; spot < Spots; spot++)
            if (standing[spot] == null && --emptyFor[spot] <= 0) Fill(spot);
    }

    /// <summary>The raid given up (the player gone from the street, or none of the three able to fight): lost, unless already won.</summary>
    public void GiveUp() => TicksLeft = 0;

    /// <summary>The ticks run down at once (a test's: the clock ends the raid).</summary>
    public void Run(int ticks)
    {
        for (int i = 0; i < ticks && !Over; i++) Tick();
    }

    /// <summary>The foe nearest a place within <see cref="Reach"/>, or null.</summary>
    public StarRaidFoe? Nearest(OpenPos at)
    {
        StarRaidFoe? best = null;
        long bestD = (long)Reach * Reach;
        foreach (var foe in standing)
        {
            if (foe == null) continue;
            long d = foe.Pos.GroundDistanceSquared(at);
            if (d > bestD) continue;
            bestD = d;
            best = foe;
        }
        return best;
    }

    /// <summary>A foe felled by Let's Go: its spot empties for a moment, and the count goes on.</summary>
    public void Fell(StarRaidFoe foe)
    {
        if (Over || standing[foe.Spot] != foe) return;
        standing[foe.Spot] = null;
        emptyFor[foe.Spot] = RefillTicks;
        Felled++;
    }

    /// <summary>
    /// The member of the team sent next: the first three able to fight, in turn (the one after the last sent), or -1
    /// where none of the first three can.
    /// </summary>
    public int NextLead(Party party)
    {
        var three = Enumerable.Range(0, party.Count).Where(i => !party.Members[i].IsEgg && !party.Members[i].IsFainted).Take(3).ToList();
        if (three.Count == 0) return -1;
        int pick = three[lead % three.Count];
        lead++;
        return pick;
    }

    private void Fill(int spot)
    {
        var draws = new SplitMix(SplitMix.Mix(SplitMix.Fnv("starraid/" + Base.Key), (uint)made, 0x57A4));
        string species = Base.Species[draws.Int(Base.Species.Count)];
        int level = Base.MinLevel + draws.Int(Base.MaxLevel - Base.MinLevel + 1);
        // On the ring, each spot its sixth of the turn, a little in or out
        double angle = spot * Math.Tau / Spots + draws.Range(-0.25f, 0.25f);
        double r = Radius + draws.Range(-1.5f, 1.5f);
        double x = Middle.MetresX + Math.Sin(angle) * r, z = Middle.MetresZ - Math.Cos(angle) * r;
        double y = groundAt(x, z, Middle.MetresY) ?? Middle.MetresY;
        // Facing the middle
        int yaw = OpenYaw.Toward(Middle.MetresX - x, Middle.MetresZ - z);
        standing[spot] = new StarRaidFoe { Id = made, Species = species, Level = level, Spot = spot, Pos = OpenPos.FromMetres(x, y, z), Yaw = yaw };
        made++;
    }
}
