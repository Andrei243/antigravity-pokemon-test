using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>A class of <c>Data/facility-trainers.json</c>: one of the tables' classes, the look its trainer wears and eight names of ours.</summary>
public sealed class FacilityTrainerRecord
{
    [DataRequired] public string Class { get; set; } = "";
    [DataRequired] public string Look { get; set; } = "";
    [DataRequired] public List<string> Names { get; set; } = new();
}

/// <summary>The later battle places' own trainers (<c>Data/facility-trainers.json</c>, written by hand).</summary>
public sealed class FacilityTrainersFile
{
    [DataRequired] public List<FacilityTrainerRecord> Trainers { get; set; } = new();
}

/// <summary>
/// A trainer of the later places with the team drawn for one battle (plan 37 · FC1): who they are, the later sets drawn
/// and the Pokémon built from them, with what a challenge put down keeps to meet them again.
/// </summary>
public sealed class FacilityOpponent
{
    public string Id { get; init; } = "";
    public string Class { get; init; } = "";
    public string Name { get; init; } = "";
    public string Look { get; init; } = "";

    /// <summary>The sets drawn, by their ids in <see cref="FacilitySetDatabase"/>.</summary>
    public List<string> Sets { get; init; } = new();
    public List<Pokemon> Pokemon { get; init; } = new();

    /// <summary>The number its Pokémon carry as their first trainer's, and each one's personality.</summary>
    public uint OtId { get; init; }
    public List<uint> Personalities { get; init; } = new();

    /// <summary>The trainer as a battle takes one: no prize money, no items of their own, and the Frontier's three routines unless a place has its own.</summary>
    public Trainer AsTrainer(AiFlags? mind = null)
    {
        var trainer = new Trainer
        {
            Id = Id,
            Name = Name,
            TrainerClass = Class,
            PrizeMoney = 0,
            Ai = mind ?? FrontierTrainers.Mind,
            Look = Look
        };
        foreach (var p in Pokemon) trainer.Party.Add(p);
        return trainer;
    }
}

/// <summary>
/// How the later battle places draw their trainers' teams (plan 37 · FC1; <c>docs/mechanics/rulings.md</c>, "The later
/// sets"): who the trainer is, from <c>Data/facility-trainers.json</c>; so many of the later sets
/// (<see cref="FacilitySetDatabase"/>) of the tiers a room allows, no two of one species or holding the same item, and none
/// sharing either with a team to keep clear of (<see cref="FrontierTrainers.SelectSets"/>' rule, and a partner's); each set
/// built as the Frontier builds one (<see cref="FrontierTrainers.Build"/>) with a personality for its nature
/// (<see cref="FrontierTrainers.PersonalityFor"/>), at the level asked. Every draw is on the caller's sixteen-bit draw, in
/// this order: the class, the name, the sets, the first trainer's number (two draws), each personality. GPU-free.
/// </summary>
public static class FacilityTrainers
{
    public const string FileName = "facility-trainers.json";

    private static readonly Lazy<FacilityTrainersFile> file = new(() => GameDataFiles.Load<FacilityTrainersFile>(FileName, strict: true));

    public static IReadOnlyList<FacilityTrainerRecord> Trainers => file.Value.Trainers;

    /// <summary>How many draws a team may refuse before the pool is taken to be too small for the team asked.</summary>
    public const int MostRefusals = 10_000;

    /// <summary>
    /// The IV of all six stats by a room's rank (a rule of ours: 12 at rank 0, four more a rank, 31 from rank 5), so a
    /// harder room's trainers bring better Pokémon, as the Frontier's IVs rise with its trainers' numbers.
    /// </summary>
    public static int IvsFor(int rank) => rank <= 0 ? 12 : Math.Min(31, 12 + 4 * rank);

    /// <summary>
    /// Whether a set may come to a trainer of the later places: not of a species the Frontier bans, and, unless
    /// <paramref name="legendaries"/>, of no legendary or mythical species (a rule of ours: the Frontier's banlist is
    /// Platinum's eighteen, and the later legendaries are met only where a place asks for them).
    /// </summary>
    public static bool MayCome(FrontierSetRecord set, bool legendaries = false)
    {
        if (FrontierDatabase.IsBanned(set.Species)) return false;
        if (legendaries) return true;
        var species = PokemonDatabase.Get(set.Species);
        return species != null && !species.Legendary && !species.Mythical;
    }

    /// <summary>The sets a draw is made from: those of the tiers asked that may come, and that <paramref name="only"/> lets in, in the file's order.</summary>
    public static List<FrontierSetRecord> Pool(int lowestTier, int highestTier, bool legendaries = false, Func<FrontierSetRecord, bool>? only = null) =>
        FacilitySetDatabase.Sets
            .Where(s => s.Tier >= lowestTier && s.Tier <= highestTier && MayCome(s, legendaries) && (only == null || only(s)))
            .ToList();

    /// <summary>
    /// Draws <paramref name="count"/> sets of the pool, no two of one species or holding the same item, none sharing either
    /// with <paramref name="keepClearOf"/> (a partner's Pokémon, a team already drawn), a set a draw each.
    /// </summary>
    public static List<FrontierSetRecord> SelectSets(IReadOnlyList<FrontierSetRecord> pool, int count, Func<int> next16, IReadOnlyList<Pokemon>? keepClearOf = null)
    {
        var species = new HashSet<string>(keepClearOf?.Select(p => p.Species.Name) ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        var items = new HashSet<string?>(keepClearOf?.Select(p => p.HeldItem?.Name) ?? Enumerable.Empty<string?>());
        var chosen = new List<FrontierSetRecord>();
        int refusals = 0;
        while (chosen.Count < count)
        {
            if (pool.Count == 0 || refusals > MostRefusals)
                throw new InvalidOperationException($"{pool.Count} sets can't make a team of {count} with no two of one species or holding the same item");
            var set = pool[(next16() & 0xFFFF) % pool.Count];
            if (species.Contains(set.Species) || items.Contains(set.Item))
            {
                refusals++;
                continue;
            }
            chosen.Add(set);
            species.Add(set.Species);
            items.Add(set.Item);
        }
        return chosen;
    }

    /// <summary>
    /// A trainer and their team: a class and a name of ours, <paramref name="count"/> sets of the tiers asked built at
    /// <paramref name="level"/> with every IV <paramref name="ivs"/>.
    /// </summary>
    public static FacilityOpponent Draw(int count, int lowestTier, int highestTier, int ivs, Func<int> next16, int level = FrontierRules.Level,
        IReadOnlyList<Pokemon>? keepClearOf = null, bool legendaries = false, Func<FrontierSetRecord, bool>? only = null)
    {
        var who = Trainers[(next16() & 0xFFFF) % Trainers.Count];
        string name = who.Names[(next16() & 0xFFFF) % who.Names.Count];
        var sets = SelectSets(Pool(lowestTier, highestTier, legendaries, only), count, next16, keepClearOf);
        uint otId = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
        var pokemon = new List<Pokemon>();
        var personalities = new List<uint>();
        foreach (var set in sets)
        {
            uint personality = FrontierTrainers.PersonalityFor(set.Nature, otId, next16);
            personalities.Add(personality);
            pokemon.Add(FrontierTrainers.Build(set, personality, ivs, level));
        }
        return new FacilityOpponent
        {
            Id = IdOf(who, name), Class = who.Class, Name = name, Look = who.Look,
            Sets = sets.Select(s => s.Id).ToList(), Pokemon = pokemon, OtId = otId, Personalities = personalities
        };
    }

    /// <summary>
    /// The same trainer and team again from what a challenge put down keeps (the sets, the first trainer's number and the
    /// personalities), with no draw.
    /// </summary>
    public static FacilityOpponent Rebuild(FacilityOpponent kept, int ivs, int level = FrontierRules.Level) => new()
    {
        Id = kept.Id, Class = kept.Class, Name = kept.Name, Look = kept.Look, Sets = kept.Sets.ToList(), OtId = kept.OtId,
        Personalities = kept.Personalities.ToList(),
        Pokemon = kept.Sets.Zip(kept.Personalities, (id, personality) =>
            FrontierTrainers.Build(FacilitySetDatabase.Get(id) ?? throw new InvalidOperationException($"No later set {id}"), personality, ivs, level)).ToList()
    };

    /// <summary>A trainer's id for the battle: <c>facility_</c>, the class and the name (<c>facility_ace_trainer_adeline</c>).</summary>
    public static string IdOf(FacilityTrainerRecord who, string name) =>
        "facility_" + Slug(who.Class) + "_" + Slug(name);

    private static string Slug(string text)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in text.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (c == ' ' && sb.Length > 0 && sb[^1] != '_') sb.Append('_');
        }
        return sb.ToString();
    }
}
