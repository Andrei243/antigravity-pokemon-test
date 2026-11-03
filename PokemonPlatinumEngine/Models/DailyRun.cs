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

/// <summary>
/// The daily run (plan 13 · V14; rulings, "The daily run"): one round of the Battle Factory, the same for everyone on
/// one day. Its seed is the day's date (UTC, or the date <see cref="GameClock.FixedDate"/> fixes) mixed with the data's
/// version, never <see cref="Dice"/> or the computer's own clock, so every machine with the same data draws the same six
/// rentals and the same seven trainers, and each battle is fought on the same generator. The round is the day of the
/// week's (Monday the Factory's first round, Sunday its seventh), so the week grows harder, and the Factory Head ends
/// Wednesday's and Sunday's run as he ends the 21st and 49th battles of a streak. Everything is the Factory's own
/// (<see cref="BattleFactoryChallenge"/>): its rows, its draws, its minds, its trades, at level 50 under the Frontier's
/// battle rules; but it is played from the battle room, on records of its own, and touches no save. GPU-free.
/// </summary>
public sealed class DailyRun
{
    /// <summary>The battles of a daily run: the Factory's round of seven.</summary>
    public const int Battles = BattleFactoryChallenge.BattlesPerRound;

    // The salts a day's draws are told apart by
    private const uint RoundSalt = 0x4452u, FoeSalt = 0x4446u, BattleSalt = 0x4442u;

    /// <summary>The day the run is of (a date, midnight).</summary>
    public DateTime Date { get; }

    /// <summary>The day's seed: the date and the data's version.</summary>
    public uint Seed { get; }

    /// <summary>The Factory's challenge the run is played on.</summary>
    public BattleFactoryChallenge Factory { get; }

    /// <summary>Whether this run is the day's first, the one the records keep; a later run that day is practice.</summary>
    public bool Counts { get; }

    /// <summary>Battles won so far.</summary>
    public int Wins { get; private set; }

    /// <summary>The run ended in a loss, or was given up.</summary>
    public bool Lost { get; private set; }

    /// <summary>All seven were won.</summary>
    public bool Cleared => Wins >= Battles;

    public bool Over => Lost || Cleared;

    /// <summary>The three have been rented and the first battle stands ready.</summary>
    public bool Rented => Factory.Team.Count == BattleFactoryChallenge.TeamSize;

    /// <summary>A trade may be made: a battle has just been won and none has been made since.</summary>
    public bool CanTrade { get; private set; }

    /// <summary>The trainer to come has been drawn but not yet stepped up (the beaten three still stand to be traded for).</summary>
    private bool foePending;

    // Records the run's trades go into: its own, never the player's
    private readonly FrontierRecords scratch = new();

    private DailyRun(DateTime date, string version, bool counts)
    {
        Date = date.Date;
        Seed = SeedFor(Date, version);
        Counts = counts;
        Factory = BattleFactoryChallenge.AtRound(RoundFor(Date));
        Factory.DrawRound(Draw(RoundSalt, 0));
    }

    /// <summary>The run of a day, on the game's data; <paramref name="counts"/> says whether the records keep it.</summary>
    public static DailyRun For(DateTime date, bool counts = true, string? version = null) => new(date, version ?? GameDataFiles.Version, counts);

    /// <summary>
    /// Today as the daily run counts days: the date <see cref="GameClock.FixedDate"/> fixes (the harness's), or else the
    /// day in UTC by <see cref="WallClock"/>, so one day is one day the world over.
    /// </summary>
    public static DateTime Today() => (GameClock.FixedDate ?? WallClock.UtcNow).Date;

    /// <summary>A day's seed: its year, month and day and the data's version mixed with no draw of anyone's.</summary>
    public static uint SeedFor(DateTime date, string version) =>
        SplitMix.Mix(SplitMix.Fnv(version), (uint)(date.Year * 10000 + date.Month * 100 + date.Day), 0x44414955u);

    /// <summary>The Factory's round a day is played at: Monday its first, Sunday its seventh.</summary>
    public static int RoundFor(DateTime date) => ((int)date.DayOfWeek + 6) % 7;

    /// <summary>The key a day is kept under in <c>daily.json</c>.</summary>
    public static string KeyOf(DateTime date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    // A generator of the day's seed and a salt and a number: the same draws whatever came before
    private Func<int> Draw(uint salt, int n) => new BattleRandom(SplitMix.Mix(Seed, salt, (uint)n)).Next16;

    /// <summary>The six on offer.</summary>
    public IReadOnlyList<FactoryRental> Offer => Factory.Offer;

    /// <summary>The seven trainers of the day by the Frontier's numbers, in the order they are met.</summary>
    public IReadOnlyList<int> Trainers => Factory.Trainers.Take(Battles).ToList();

    /// <summary>The three chosen of the six, in the order chosen: the first trainer stands ready.</summary>
    public void Rent(IReadOnlyList<int> chosen)
    {
        if (Rented) throw new InvalidOperationException("The daily run's three are rented once.");
        Factory.Rent(chosen);
    }

    /// <summary>The number of the battle to come, from 1.</summary>
    public int Battle => Wins + 1;

    /// <summary>The seed the battle to come is fought on: the day's and the battle's number.</summary>
    public uint BattleSeed => SplitMix.Mix(Seed, BattleSalt, (uint)Battle);

    /// <summary>The trainer of the battle to come steps up: the three drawn for them after the last win stand ready, and no more trade is offered.</summary>
    public void Ready()
    {
        if (!Rented) throw new InvalidOperationException("Three are rented before a battle.");
        if (foePending) Factory.TakeNextFoe();
        foePending = false;
        CanTrade = false;
    }

    /// <summary>The three in hand as a team for the battle to come, whole.</summary>
    public Party PlayerParty() => Factory.TeamForBattle();

    /// <summary>The trainer of the battle to come, with their three, thinking as the round has them think.</summary>
    public Trainer NextFoe() => Factory.Opponent().AsTrainer(Factory.Mind);

    /// <summary>The trainer of the battle to come by name and class, for whoever tells of them before they step up.</summary>
    public FrontierTrainerRecord NextTrainer => FrontierDatabase.Trainer(Factory.Trainers[Math.Min(Wins, Battles - 1)]);

    /// <summary>The battle to come was won: the next trainer's three are drawn from the day's seed and the battle's number, and a trade is offered.</summary>
    public void Won()
    {
        if (Over) throw new InvalidOperationException("The daily run is over.");
        Factory.Won(scratch);
        Wins++;
        if (Cleared) return;
        Factory.DrawNextFoe(Draw(FoeSalt, Wins));
        foePending = true;
        CanTrade = true;
    }

    /// <summary>One of the three in hand given up for one of the trainer just beaten.</summary>
    public void Trade(int mine, int theirs)
    {
        if (!CanTrade) throw new InvalidOperationException("No trade is offered now.");
        Factory.Trade(scratch, mine, theirs);
        CanTrade = false;
    }

    /// <summary>The battle to come was lost, or the run given up.</summary>
    public void Lose() => Lost = true;
}

/// <summary>A day's result as <c>daily.json</c> keeps it.</summary>
public sealed class DailyResult
{
    /// <summary>The day, <c>yyyy-MM-dd</c>.</summary>
    public string Date { get; set; } = "";

    /// <summary>Battles won in the day's first run.</summary>
    public int Wins { get; set; }

    /// <summary>The run is over (lost, given up or cleared); a run left in the middle stands at its wins.</summary>
    public bool Over { get; set; }
}

/// <summary>
/// <c>daily.json</c> beside the saves (plan 13 · V14): the result of each day's first run, kept as soon as its three are
/// rented and as each battle is won, so a run left half way is kept where it stood. A later run that day changes
/// nothing. Local only: nothing is sent anywhere.
/// </summary>
public sealed class DailyRecords
{
    public const string FileName = "daily.json";

    public List<DailyResult> Days { get; set; } = new();

    /// <summary>The most battles won in one day's run.</summary>
    public int Best => Days.Count == 0 ? 0 : Days.Max(d => d.Wins);

    /// <summary>How many days' runs won all seven.</summary>
    public int Clears => Days.Count(d => d.Wins >= DailyRun.Battles);

    /// <summary>A day's result; null when it has not been played.</summary>
    public DailyResult? On(DateTime date) => Days.FirstOrDefault(d => d.Date == DailyRun.KeyOf(date));

    public bool Played(DateTime date) => On(date) != null;

    /// <summary>
    /// The run as it stands is written into its day: the first time for a counted run adds the day, every later
    /// time moves its wins on. True when the run has set a new best. A run that doesn't count keeps nothing.
    /// </summary>
    public bool Keep(DailyRun run)
    {
        if (!run.Counts) return false;
        int before = Best;
        var day = On(run.Date);
        if (day == null)
        {
            day = new DailyResult { Date = DailyRun.KeyOf(run.Date) };
            Days.Add(day);
            Days.Sort((a, b) => string.CompareOrdinal(a.Date, b.Date));
        }
        day.Wins = Math.Max(day.Wins, run.Wins);
        day.Over = run.Over;
        return run.Wins > before;
    }

    /// <summary>The records of a file; empty where there is none or it can't be read.</summary>
    public static DailyRecords Read(string path)
    {
        try
        {
            if (File.Exists(path)) return JsonSerializer.Deserialize<DailyRecords>(File.ReadAllText(path), GameDataFiles.Json) ?? new DailyRecords();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            Core.Diagnostics.Log.Warn($"The daily run's records couldn't be read: {e.Message}");
        }
        return new DailyRecords();
    }

    /// <summary>Writes the records whole (beside, then over).</summary>
    public static void Write(string path, DailyRecords records)
    {
        try
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(records, GameDataFiles.Json));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Core.Diagnostics.Log.Warn($"The daily run's records couldn't be written: {e.Message}");
        }
    }
}
