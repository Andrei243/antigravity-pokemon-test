using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>A cup of the stadium (<c>Data/cups.json</c>, written by hand): the Stadium games' numbers, names and prizes of ours.</summary>
public sealed class CupRecord
{
    /// <summary>The cup's word (<c>facility stadium cup crest</c>) and its record's key.</summary>
    [DataRequired] public string Id { get; set; } = "";
    /// <summary>The cup's name as the desk says it.</summary>
    [DataRequired] public string Name { get; set; } = "";
    /// <summary>Every Pokémon entered at this level or more and at <see cref="MaxLevel"/> or less.</summary>
    [DataRequired] public int MinLevel { get; set; }
    [DataRequired] public int MaxLevel { get; set; }
    /// <summary>The three entered may not total more than this (0 for no total).</summary>
    public int Total { get; set; }
    /// <summary>Only a species that is the first of its line and evolves (the cup of the unevolved).</summary>
    public bool FirstStage { get; set; }
    /// <summary>Taller (in metres) or heavier (in kilograms) is refused; 0 for no limit.</summary>
    public float MaxHeight { get; set; }
    public float MaxWeight { get; set; }
    /// <summary>The prize the first win of the cup hands over, and the Battle Points of every win.</summary>
    [DataRequired] public string Prize { get; set; } = "";
    [DataRequired] public int Points { get; set; }
}

public sealed class CupsFile
{
    [DataRequired] public List<CupRecord> Cups { get; set; } = new();
}

/// <summary>Why a team may not enter a cup (the first reason found).</summary>
public enum CupRefusal { None, Count, Egg, Banned, Level, Total, Evolved, TooTall, TooHeavy, SameSpecies, SameItem, Type }

/// <summary>
/// The cups' stadium off the Battle Frontier annex's lobby (plan 37 · FC7; <c>docs/mechanics/rulings.md</c>, "The cups, the
/// Leaders' round and the sparring room"): a cup is the Stadium games' rules as values (<see cref="CupRecord"/>), eight
/// battles in a row against trainers of the later sets that meet them (<see cref="FacilityTrainers"/>), three entered of
/// the player's own as copies at their own levels, or three rentals the stadium lends at the cup's highest level. Every
/// draw is on the caller's sixteen-bit draw. No drawing or input.
/// </summary>
public static class Cups
{
    public const string FileName = "cups.json";

    /// <summary>How many are entered, how many battles a cup is, and every rental's and foe's IVs.</summary>
    public const int Entered = 3, Battles = 8, RentalIvs = 20;

    private static readonly Lazy<CupsFile> file = new(() => GameDataFiles.Load<CupsFile>(FileName, strict: true));

    public static IReadOnlyList<CupRecord> All => file.Value.Cups;

    public static CupRecord? Get(string id) => All.FirstOrDefault(c => c.Id == id);

    /// <summary>Whether a species may enter a cup at all, whatever its level (the Frontier's banned, the first stage, height, weight).</summary>
    public static bool SpeciesMay(PokemonSpecies species, CupRecord cup)
    {
        if (FrontierDatabase.IsBanned(species.Name) || FrontierRules.IsBannedOfOurs(species)) return false;
        if (cup.FirstStage && !Unevolved(species)) return false;
        if (cup.MaxHeight > 0 && species.Height > cup.MaxHeight) return false;
        if (cup.MaxWeight > 0 && species.Weight > cup.MaxWeight) return false;
        return true;
    }

    /// <summary>
    /// The first of its line that can still evolve into a species of the games (an evolution into a species of our own past
    /// level 100 doesn't count, so a Tauros is no Pokémon of the cup of the unevolved).
    /// </summary>
    public static bool Unevolved(PokemonSpecies species) =>
        TradeBoard.IsFirstStage(species) && species.Evolutions is { } evolutions
        && evolutions.Any(e => PokemonDatabase.Get(e.TargetSpecies) is { IsOurs: false });

    /// <summary>The first reason the team of those entered may not enter the cup, or <see cref="CupRefusal.None"/>.</summary>
    public static CupRefusal WhyNot(IReadOnlyList<Pokemon> team, CupRecord cup, PokemonType? only = null)
    {
        if (team.Count != Entered) return CupRefusal.Count;
        foreach (var p in team)
        {
            if (p.IsEgg) return CupRefusal.Egg;
            if (!FrontierRules.MayEnter(p)) return CupRefusal.Banned;
            if (p.Level < cup.MinLevel || p.Level > cup.MaxLevel) return CupRefusal.Level;
            if (cup.FirstStage && !Unevolved(p.Species)) return CupRefusal.Evolved;
            if (cup.MaxHeight > 0 && p.Height > cup.MaxHeight) return CupRefusal.TooTall;
            if (cup.MaxWeight > 0 && p.Weight > cup.MaxWeight) return CupRefusal.TooHeavy;
            if (only is { } type && !p.HasType(type)) return CupRefusal.Type;
        }
        if (cup.Total > 0 && team.Sum(p => p.Level) > cup.Total) return CupRefusal.Total;
        if (team.Select(p => p.Species.Name).Distinct().Count() < team.Count) return CupRefusal.SameSpecies;
        var items = team.Where(p => p.HeldItem != null).Select(p => p.HeldItem!.Name).ToList();
        if (items.Distinct().Count() < items.Count) return CupRefusal.SameItem;
        return CupRefusal.None;
    }

    /// <summary>The level a cup's foes and rentals stand at: its highest, or less where the total would have three no higher.</summary>
    public static int FoeLevel(CupRecord cup) =>
        Math.Max(cup.MinLevel, cup.Total > 0 ? Math.Min(cup.MaxLevel, cup.Total / Entered) : cup.MaxLevel);

    /// <summary>A set of the later sets that meets the cup.</summary>
    public static bool SetMeets(FrontierSetRecord set, CupRecord cup) =>
        PokemonDatabase.Get(set.Species) is { } species && SpeciesMay(species, cup);

    /// <summary>The tiers of the cup's foe of a battle (1 to 8): the weakest two first, the strongest two from the sixth.</summary>
    public static (int Lowest, int Highest) TiersFor(int battle) => battle switch { <= 2 => (1, 2), <= 5 => (2, 3), _ => (3, 4) };

    /// <summary>Three rentals the stadium lends for a cup: three of the later sets that meet it, at its foes' level.</summary>
    public static List<Pokemon> Rentals(CupRecord cup, Func<int> next16) =>
        FacilityTrainers.Draw(Entered, 1, 4, RentalIvs, next16, FoeLevel(cup), only: s => SetMeets(s, cup)).Pokemon;

    /// <summary>The cup's foe of a battle (1 to 8): a trainer of ours with three of the later sets that meet it, at its foes' level.</summary>
    public static FacilityOpponent Foe(CupRecord cup, int battle, Func<int> next16)
    {
        var (low, high) = TiersFor(battle);
        return FacilityTrainers.Draw(Entered, low, high, FacilityTrainers.IvsFor(battle), next16, FoeLevel(cup), only: s => SetMeets(s, cup));
    }

    /// <summary>The variables a cup's run keeps: the cup (its number in the file from 1), the battles won, the seed of its draws and whether it is rented.</summary>
    public const string CupVariable = "VAR_ANNEX_CUP", WonVariable = "VAR_ANNEX_CUP_WON", SeedVariable = "VAR_ANNEX_CUP_SEED", RentedVariable = "VAR_ANNEX_CUP_RENTED";

    /// <summary>The variable a cup's first win is kept in (1 once its prize is given).</summary>
    public static string WonCupVariable(CupRecord cup) => "VAR_ANNEX_CUP_PRIZE_" + cup.Id.ToUpperInvariant();

    /// <summary>A generator for a battle of a run: Platinum's, seeded by the run's seed and the battle, so a battle drawn again is the same.</summary>
    public static Func<int> DrawsFor(int seed, int battle) => new BattleRandom(((uint)seed & 0xFFFF) | (uint)battle << 16 ^ 0x5C0F_0000).Next16;
}

/// <summary>
/// The Leaders' round (plan 37 · FC7): a region's Leaders one after another in its own order, then its Elite Four and its
/// Champion, with no bracket, every Pokémon on both sides at level 100 (<see cref="LevelScaling.At"/>), each trainer's three
/// from their team as the World Tournament picks them (<see cref="Tournament.ThreeOf"/>), from <c>tournaments.json</c>'s
/// pools (no id written twice). A region's round opens with its Hall of Fame. No drawing or input.
/// </summary>
public static class LeadersRound
{
    public const int Level = 100;

    /// <summary>The variables a round keeps: the region's number in <see cref="Regions"/> from 1, and the trainers beaten.</summary>
    public const string RegionVariable = "VAR_ANNEX_ROUND", BeatenVariable = "VAR_ANNEX_ROUND_BEATEN";

    /// <summary>The variable a region's round cleared is kept in (1 once cleared).</summary>
    public static string ClearedVariable(string region) => "VAR_ANNEX_ROUND_CLEARED_" + region.ToUpperInvariant();

    /// <summary>The regions with a round: those whose Leaders and League <c>tournaments.json</c> lists.</summary>
    public static IReadOnlyList<string> Regions =>
        TournamentDatabase.Regions.Where(r => r.Events.Any(e => e.Kind == "leaders" && e.Pool.Count > 0) && r.Events.Any(e => e.Kind == "league" && e.Always.Count > 0))
            .Select(r => r.Region).ToList();

    /// <summary>A region's round, in order: its Leaders, then its Elite Four, then its Champion (the league event's first).</summary>
    public static List<TournamentEntrantRecord> TrainersOf(string region)
    {
        var events = TournamentDatabase.Regions.First(r => r.Region == region).Events;
        var leaders = events.First(e => e.Kind == "leaders" && e.Pool.Count > 0).Pool;
        var league = events.First(e => e.Kind == "league" && e.Always.Count > 0).Always;
        var order = new List<TournamentEntrantRecord>();
        foreach (var e in leaders.Concat(league.Skip(1)).Append(league[0]))
            if (order.All(o => o.Trainer != e.Trainer)) order.Add(e);
        return order;
    }

    /// <summary>Whether a region's round is open: its Hall of Fame entered.</summary>
    public static bool Open(string region, Func<string, bool> has) => has(region + "HallOfFame");

    /// <summary>A trainer of the round as a battle takes them: their three at 100 under their own id's class and name.</summary>
    public static Trainer TrainerOf(TournamentEntrantRecord entrant)
    {
        var record = TrainerDatabase.Get(entrant.Trainer) ?? throw new InvalidOperationException($"The Leaders' round names no trainer {entrant.Trainer}");
        var trainer = new Trainer
        {
            Id = "round_" + record.Id, Name = record.Name, TrainerClass = record.Class, PrizeMoney = 0,
            Ai = Battle.Sim.Ai.AiFlagNames.Parse(record.Ai), Look = entrant.Look, Items = new List<string>(),
            KeyStone = record.KeyStone, ZRing = record.ZRing
        };
        foreach (var p in Tournament.ThreeOf(record)) trainer.Party.Add(LevelScaling.At(p, Level));
        return trainer;
    }

    /// <summary>The player's three as the round fights them: copies at 100, whole.</summary>
    public static Party TeamFor(Party party, IReadOnlyList<int> slots)
    {
        var team = new Party();
        foreach (int slot in slots)
            if (slot >= 0 && slot < party.Count) team.Add(LevelScaling.At(party.Members[slot], Level));
        return team;
    }
}

/// <summary>
/// The sparring room (plan 37 · FC7, after the Isle of Armor's dojo): a type chosen, three of it entered, an endless streak of
/// trainers whose three are of that type, at 50, with a record per type (its ways are the eighteen types). Trainers of the
/// later sets of the tiers the streak has reached; no host. No drawing or input.
/// </summary>
public static class SparringRoom
{
    public const string Place = "sparring";

    /// <summary>The room's ways: the types, as their words.</summary>
    public static readonly string[] Ways = Enum.GetValues<PokemonType>().Select(t => t.ToString().ToLowerInvariant()).ToArray();

    public static PokemonType TypeOf(string way) => Enum.Parse<PokemonType>(way, ignoreCase: true);

    /// <summary>The tiers by the streak: 1 and 2 to the tenth battle, then 2 and 3, then 3 and 4 from the 25th.</summary>
    public static (int Lowest, int Highest) TiersFor(int streak) => streak switch { < 10 => (1, 2), < 25 => (2, 3), _ => (3, 4) };

    /// <summary>The Battle Points of a win: one for the first ten of a streak, two to the 25th, three after.</summary>
    public static int PointsFor(int streak) => streak <= 10 ? 1 : streak <= 25 ? 2 : 3;

    /// <summary>A set whose species is of the type.</summary>
    public static bool OfType(FrontierSetRecord set, PokemonType type) =>
        PokemonDatabase.Get(set.Species) is { } s && (s.PrimaryType == type || s.SecondaryType == type);

    public static StreakRoom Room(string way)
    {
        var type = TypeOf(way);
        return new StreakRoom
        {
            Place = Place, Way = way, Hard = false, Entered = 3, Format = BattleFormat.Single,
            Tiers = TiersFor, Ivs = streak => FacilityTrainers.IvsFor(streak / 10), Points = PointsFor,
            Only = set => OfType(set, type), Type = type
        };
    }
}
