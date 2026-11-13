using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A row of a den's table (plan 33 · GL3): a species, its stars, how often it is in the den and the version it is of (0 both, 1 Sword, 2 Shield).</summary>
public sealed class RaidRowFile
{
    [DataRequired] public string Species { get; set; } = "";
    [DataRequired] public int Stars { get; set; }
    public int Weight { get; set; } = 10;
    public int Version { get; set; }
    /// <summary>The item the boss holds (plan 33 · GL7, part b: a Mega Max den's boss holds its Mega Stone), or none.</summary>
    public string? Item { get; set; }
}

/// <summary>A Max Raid den (plan 33 · GL3): its id (what its script names it by), the area it is in and its table.</summary>
public sealed class RaidDenFile
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public string Area { get; set; } = "";
    [DataRequired] public List<RaidRowFile> Rows { get; set; } = new();
    /// <summary>The story flag the den waits for (plan 33 · GL7, part b: <c>GalarHallOfFame</c>); quiet, unlit and unwished until it is set. None for a den open from the first.</summary>
    public string? Opens { get; set; }
    /// <summary>A Mega Max den (<see cref="Battle.Sim.CoreSetup.MegaMaxDen"/>): its boss holding its stone Mega Evolves and Mega Dynamaxes by itself.</summary>
    public bool MegaMax { get; set; }
    /// <summary>The levels its bosses are met at, least and most, in place of their stars' (<see cref="Raids.LevelsOf"/>); none for the stars'.</summary>
    public List<int>? Levels { get; set; }
}

/// <summary>
/// <c>Data/world/galar/dens.json</c>: Galar's dens and the raiders who answer a den's call (each a name, a look and the
/// species they bring), all ours (plan 33 · decision 4). Read strictly.
/// </summary>
public sealed class RaidDensFile
{
    public List<RaidDenFile> Dens { get; set; } = new();
    public List<RaiderFile> Raiders { get; set; } = new();
}

/// <summary>A raider who may join the player at a den: their name, their look and the species they may bring.</summary>
public sealed class RaiderFile
{
    [DataRequired] public string Name { get; set; } = "";
    public string? Look { get; set; }
    [DataRequired] public List<string> Species { get; set; } = new();
}

/// <summary>What a den holds today: its species, its stars, the level it is met at, and whether its beam is lit.</summary>
public readonly record struct RaidToday(string Species, int Stars, int Level, bool Lit, string? Item = null);

/// <summary>
/// Max Raid dens out of a battle (plan 33 · GL3; <c>docs/mechanics/rulings.md</c>, "Max Raid dens"): which Pokémon a den
/// holds on a day and how many stars (the stars it may have by the region's badges, a row by weight among those of the
/// version, drawn by the day's number and the den's id, never the dice), whether its beam is lit, the Watts its light
/// gives, the three raiders who answer its call, and the chance a ball takes the felled boss. GPU-free.
/// </summary>
public static class Raids
{
    private static RaidDensFile? galar;

    /// <summary>Galar's dens and raiders (the game's file, or a test's).</summary>
    public static RaidDensFile Galar
    {
        get => galar ??= GameDataFiles.Load<RaidDensFile>("world/galar/dens.json", strict: true);
        set => galar = value;
    }

    /// <summary>A den by its id, or null.</summary>
    public static RaidDenFile? Den(string id) => Galar.Dens.FirstOrDefault(d => d.Id == id);

    /// <summary>The most stars a den holds by the badges won: two with none or one, three with two or three, four with four or five, five with six or more.</summary>
    public static int MostStars(int badges) => badges >= 6 ? 5 : badges >= 4 ? 4 : badges >= 2 ? 3 : 2;

    /// <summary>The level of a boss of so many stars: a range of three from the stars' floor (ours).</summary>
    public static (int Least, int Most) LevelsOf(int stars) => stars switch
    {
        1 => (12, 14),
        2 => (18, 20),
        3 => (26, 28),
        4 => (36, 38),
        _ => (50, 52)
    };

    /// <summary>The Watts a lit den's light gives once a day: a hundred for every star of what waits in it, and two hundred more.</summary>
    public static int WattsOf(int stars) => 200 + 100 * stars;

    /// <summary>The draws a den's day is made from: the day's number, the den's id and the version.</summary>
    private static SplitMix DrawsOf(string den, uint day, int version) => new(SplitMix.Mix(SplitMix.Fnv("raid/" + den), day, (uint)version));

    /// <summary>
    /// What a den holds on a day (by <see cref="SpecialEncounters.DailyNumber"/>), for a version (1 Sword, 2 Shield) with
    /// so many of the region's badges; a den whose rows the stars allow none of holds its weakest. <paramref name="wished"/>
    /// lights its beam whatever the day says.
    /// </summary>
    public static RaidToday Today(RaidDenFile den, uint day, int version, int badges, bool wished = false)
    {
        var draws = DrawsOf(den.Id, day, version);
        bool lit = wished || draws.Int(3) == 0;
        var rows = den.Rows.Where(r => (r.Version == 0 || r.Version == version) && r.Stars <= MostStars(badges)).ToList();
        if (rows.Count == 0) rows = den.Rows.Where(r => r.Version == 0 || r.Version == version).OrderBy(r => r.Stars).Take(1).ToList();
        int total = rows.Sum(r => Math.Max(1, r.Weight));
        int pick = draws.Int(Math.Max(1, total));
        var row = rows[0];
        foreach (var r in rows)
        {
            if ((pick -= Math.Max(1, r.Weight)) < 0) { row = r; break; }
        }
        var (least, most) = den.Levels is { Count: 2 } levels ? (levels[0], levels[1]) : LevelsOf(row.Stars);
        return new RaidToday(row.Species, row.Stars, least + draws.Int(most - least + 1), lit, row.Item);
    }

    /// <summary>Whether a den is open as the story stands: one with no <see cref="RaidDenFile.Opens"/>, or whose flag is set. A closed den is quiet, unlit and takes no Wishing Piece.</summary>
    public static bool IsOpen(RaidDenFile den, Func<string, bool> has) => den.Opens == null || has(den.Opens);

    /// <summary>
    /// The three raiders who answer a den's call on a day: three of the file's raiders, each with one of their species at
    /// the boss's level, drawn as the den's day is (a test or the game, the same three). Trainer AI flags of a partner's.
    /// </summary>
    public static List<Trainer> Allies(RaidDenFile den, uint day, int version, int level)
    {
        var draws = DrawsOf(den.Id, day, version + 16);
        var pool = Galar.Raiders.ToList();
        var allies = new List<Trainer>();
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            var raider = pool[draws.Int(pool.Count)];
            pool.Remove(raider);
            string species = raider.Species[draws.Int(raider.Species.Count)];
            var party = new Party();
            var rng = new Random((int)(draws.Next() & 0x7FFFFFFF));
            party.Add(PokemonDatabase.Get(species) is { } plain ? new Pokemon(plain, level, rng)
                : Pokemon.InForm(PokemonDatabase.SpeciesOfForm(species)!, species, level, rng));
            allies.Add(new Trainer
            {
                Id = "raider_" + raider.Name.ToLowerInvariant().Replace(' ', '_'),
                Name = raider.Name,
                TrainerClass = "Pokémon Trainer",
                Party = party,
                Look = raider.Look,
                Ai = AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert
            });
        }
        return allies;
    }

    /// <summary>
    /// The den an open place's trigger stands for, by the trigger's name (<c>den_rolling_1</c> is den <c>rolling_1</c>),
    /// or null where it is no den's or names none of the file: what the picture's beams are found by (GL3's second session).
    /// </summary>
    public static RaidDenFile? DenOfTrigger(string triggerName) =>
        triggerName.StartsWith("den_", StringComparison.Ordinal) ? Den(triggerName[4..]) : null;

    /// <summary>
    /// Whether a den's beam stands today: lit by the day's draw or by a Wishing Piece thrown in, and its boss not felled
    /// today. The same question `raid den` asks, so the picture never shows a beam the den's script calls quiet.
    /// </summary>
    public static bool BeamStands(RaidDenFile den, uint day, int version, int badges, bool wished, bool felled, bool open = true) =>
        open && !felled && Today(den, day, version, badges, wished).Lit;

    /// <summary>The chance in a hundred a ball takes a felled boss of so many stars: by its stars, a Master Ball always, a Great or Ultra Ball better (ours).</summary>
    public static int CatchChance(int stars, string ball)
    {
        if (ball == "Master Ball") return 100;
        int chance = stars switch { <= 1 => 100, 2 => 90, 3 => 75, 4 => 60, _ => 45 };
        chance += ball switch { "Ultra Ball" => 15, "Great Ball" => 10, "Dream Ball" or "Beast Ball" => 20, _ => 0 };
        return Math.Min(100, chance);
    }
}
