using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models;

/// <summary>One step of the gauntlet: a trainer of <c>trainers.json</c> by id, and the look they are drawn in.</summary>
public sealed class GauntletStep
{
    public string Id { get; set; } = "";
    public string Look { get; set; } = "";
}

/// <summary><c>Data/gauntlet.json</c>: the gauntlet's order.</summary>
public sealed class GauntletData
{
    public List<GauntletStep> Order { get; set; } = new();

    private static GauntletData? loaded;

    /// <summary>The game's own gauntlet, read once.</summary>
    public static GauntletData Default => loaded ??= JsonSerializer.Deserialize<GauntletData>(File.ReadAllText(GameDataFiles.PathOf("gauntlet.json")), GameDataFiles.Json) ?? new GauntletData();
}

/// <summary>
/// What the battle room keeps of this machine's play (plan 13 · V13, V26): the furthest a gauntlet came, the longest
/// streak and the puzzles solved. Per machine and never per save, so it lives in <c>settings.json</c>
/// (<see cref="GameSettings.BattleRoom"/>).
/// </summary>
public sealed class BattleRoomRecords
{
    /// <summary>The most of the gauntlet's battles won in one go (all of them is a gauntlet cleared).</summary>
    public int GauntletBest { get; set; }

    /// <summary>How many times the whole gauntlet has been won.</summary>
    public int GauntletClears { get; set; }

    /// <summary>The longest streak of trainers beaten.</summary>
    public int StreakBest { get; set; }

    /// <summary>The puzzles solved, by id, in the order of their ids.</summary>
    public List<string> Puzzles { get; set; } = new();

    public bool Solved(string puzzle) => Puzzles.Contains(puzzle);

    /// <summary>Marks a puzzle solved; true the first time.</summary>
    public bool Solve(string puzzle)
    {
        if (Puzzles.Contains(puzzle)) return false;
        Puzzles.Add(puzzle);
        Puzzles.Sort(StringComparer.Ordinal);
        return true;
    }
}

/// <summary>The challenges of the battle room: two are a run of battles, and a friend's trainer (plan 13 · V22) is one battle that is no run.</summary>
public enum RoomChallenge { Gauntlet, Streak, Friend }

/// <summary>
/// A run of the battle room (plan 13 · V13; rulings, "The team builder and the gauntlet"): the gauntlet, Sinnoh's Gym Leaders,
/// Elite Four and Champion one after another with their own teams at their own levels, or the endless streak, three
/// of the Battle Frontier's trainers' Pokémon at level 50 battle after battle, the trainer drawn by the Battle Tower's
/// rooms as the streak grows. The built team is made afresh, whole, for every battle, so nothing is carried from one to
/// the next and nothing is kept anywhere but the records. Everything a run draws comes from its seed: the streak's
/// trainers and teams from a <see cref="BattleRandom"/> of the seed mixed with the battle's number, and each battle's
/// own seed the same way, so a battle's <see cref="BattleRecord"/> replays. GPU-free.
/// </summary>
public sealed class RoomRun
{
    /// <summary>The level the streak is fought at, as the Frontier's.</summary>
    public const int StreakLevel = FrontierRules.Level;

    /// <summary>How many Pokémon a streak's trainer brings.</summary>
    public const int StreakTeam = 3;

    public RoomChallenge Kind { get; }
    public BuiltTeam Team { get; }
    public uint Seed { get; }
    public GauntletData Gauntlet { get; }

    /// <summary>Battles won so far.</summary>
    public int Wins { get; private set; }

    /// <summary>The run ended in a loss (or was given up).</summary>
    public bool Lost { get; private set; }

    /// <summary>The gauntlet was won to its last battle.</summary>
    public bool Cleared => Kind == RoomChallenge.Gauntlet && Wins >= Gauntlet.Order.Count;

    public bool Over => Lost || Cleared;

    public RoomRun(RoomChallenge kind, BuiltTeam team, uint seed, GauntletData? gauntlet = null)
    {
        if (BuiltTeams.WhyNot(team) is { } why) throw new ArgumentException($"A run is begun with a team that can battle: {why.Key} {why.Value}");
        Kind = kind;
        Team = team.Copy();
        Seed = seed;
        Gauntlet = gauntlet ?? GauntletData.Default;
    }

    /// <summary>The number of the battle to come, from 1.</summary>
    public int Battle => Wins + 1;

    /// <summary>How many battles the gauntlet has; none (endless) for a streak.</summary>
    public int? Length => Kind == RoomChallenge.Gauntlet ? Gauntlet.Order.Count : null;

    /// <summary>The seed of the battle to come: the run's mixed with the battle's number.</summary>
    public uint BattleSeed => SplitMix.Mix(Seed, (uint)Battle, (uint)Kind);

    /// <summary>The player's team for the battle to come: the built team made afresh, at its own levels in the gauntlet and at 50 in a streak.</summary>
    public Party PlayerParty()
    {
        var party = BuiltTeams.PartyOf(Team);
        return Kind == RoomChallenge.Streak ? LevelScaling.TeamAt(party.Members, StreakLevel) : party;
    }

    /// <summary>The trainer of the battle to come.</summary>
    public Trainer NextFoe()
    {
        if (Over) throw new InvalidOperationException("The run is over.");
        return Kind == RoomChallenge.Gauntlet ? GauntletFoe(Gauntlet.Order[Wins]) : StreakFoe(Wins, Seed);
    }

    /// <summary>A trainer of the gauntlet: the table's team, items and mind, at the levels the table gives them.</summary>
    public static Trainer GauntletFoe(GauntletStep step)
    {
        var record = TrainerDatabase.Get(step.Id) ?? throw new InvalidOperationException($"The gauntlet names no trainer called {step.Id}");
        var trainer = new Trainer { Id = record.Id, Look = step.Look };
        TrainerDatabase.Fill(trainer, record);
        trainer.PrizeMoney = 0;
        return trainer;
    }

    /// <summary>
    /// The streak's trainer for the battle after <paramref name="won"/> wins: one of the Battle Tower's room for that
    /// many (seven to a room, the last room's row from the eighth on), drawn with three of their sets as the Frontier draws
    /// them, all of it from a generator of the run's seed and the battle's number.
    /// </summary>
    public static Trainer StreakFoe(int won, uint seed)
    {
        var rng = new BattleRandom(SplitMix.Mix(seed, (uint)(won + 1), 0x5354u));
        int room = Math.Min(won / 7, FrontierDatabase.TowerRooms.Count - 1);
        int[] row = FrontierDatabase.TowerRooms[room];
        int number = row[0] + rng.Next16() % (row[1] - row[0] + 1);
        var opponent = FrontierTrainers.ForTrainers(StreakTeam, number, 0, false, rng.Next16, StreakLevel)[0];
        return opponent.AsTrainer();
    }

    /// <summary>The battle to come was won: on to the next (or the gauntlet cleared).</summary>
    public void Won()
    {
        if (Over) throw new InvalidOperationException("The run is over.");
        Wins++;
    }

    /// <summary>The battle to come was lost, or the run given up.</summary>
    public void Lose() => Lost = true;

    /// <summary>Keeps the run's best in the records; true when it is a new best.</summary>
    public bool Keep(BattleRoomRecords records)
    {
        bool best;
        if (Kind == RoomChallenge.Gauntlet)
        {
            best = Wins > records.GauntletBest;
            records.GauntletBest = Math.Max(records.GauntletBest, Wins);
            if (Cleared) records.GauntletClears++;
        }
        else
        {
            best = Wins > records.StreakBest;
            records.StreakBest = Math.Max(records.StreakBest, Wins);
        }
        return best;
    }
}
