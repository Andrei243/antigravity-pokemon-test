using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What the World Tournament keeps beside the Frontier's other records (plan 14 · B11;
/// <see cref="FrontierRecords.Tournament"/>): the titles won by event, the tournaments entered and the matches won, and
/// whether a tournament was begun and not finished. Nothing else of a tournament is saved.
/// </summary>
public sealed class TournamentRecord
{
    /// <summary>The titles won, by the event's id.</summary>
    public Dictionary<string, int> Titles { get; set; } = new();

    public int Entered { get; set; }
    public int Matches { get; set; }

    /// <summary>A tournament has been begun and not finished (a game closed in the hall).</summary>
    public bool InProgress { get; set; }

    public int TitlesOf(string eventId) => Titles.TryGetValue(eventId, out int won) ? won : 0;
}

/// <summary>One of the eight in a World Tournament: the player (<see cref="Record"/> null) or a trainer of the table with their three at level 50.</summary>
public sealed class TournamentEntrant
{
    public TrainerRecord? Record { get; init; }

    /// <summary>The look they stand in on the stage and in battle; empty for the player.</summary>
    public string Look { get; init; } = "";

    /// <summary>Their three, copies at the tournament's level (<see cref="LevelScaling.At"/>): a trainer's the three of the highest level of their team, the player's the three entered.</summary>
    public IReadOnlyList<Pokemon> Three { get; init; } = Array.Empty<Pokemon>();

    public bool IsPlayer => Record == null;

    /// <summary>The class and the name ("Leader Roark"); empty for the player.</summary>
    public string Title => Record is { } r ? Core.Text.Of("battle.fullTitle", ("class", Trainer.ClassName(r.Class)), ("name", r.Name)) : "";
}

/// <summary>
/// The Pokémon World Tournament (plan 14 · B11; rulings, "The World Tournament"): an event of the file
/// (<see cref="TournamentDatabase"/>, Sinnoh's Leaders and the Champion's event first), a <see cref="Bracket"/> of
/// eight, the player's three entered under the species and item clauses (<see cref="FrontierRules.WhyNot"/>) and every
/// Pokémon of the eight brought to level 50 up or down (<see cref="LevelScaling"/>), so a party is never touched. Each
/// match is a single battle of three against three; the other matches of a round are fought headless by the battle's
/// core with the trainer AI on both sides (<see cref="FightHeadless"/>), so the bracket is a real one. Three wins take
/// the title, worth the event's Battle Points; a loss ends it. Nothing is written to the save but the record and the
/// Battle Points. Every draw is on the sixteen-bit draw the caller hands over. No drawing or input.
/// </summary>
public sealed class Tournament
{
    public const int Entrants = 8, TeamSize = 3, Level = 50, Player = 0;

    public TournamentEventRecord Event { get; }

    /// <summary>The places in the team of the three entered, in the order they were entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The eight: the player first, then the seven trainers in the order they were seated in the field (the always-in first).</summary>
    public IReadOnlyList<TournamentEntrant> Field { get; }

    public Bracket Bracket { get; }

    /// <summary>What each headless match came to: who met, who stood on the player's side, the seed and the winner.</summary>
    public List<(int A, int B, int Home, uint Seed, int Winner)> Fought { get; } = new();

    private Tournament(TournamentEventRecord ev, IReadOnlyList<int> slots, IReadOnlyList<TournamentEntrant> field, Bracket bracket)
    {
        Event = ev;
        Slots = slots;
        Field = field;
        Bracket = bracket;
    }

    /// <summary>Whether an event may be entered: it waits for no title, or for one already won.</summary>
    public static bool IsOpen(TournamentEventRecord ev, TournamentRecord record) => ev.After == null || record.TitlesOf(ev.After) > 0;

    /// <summary>Whether a Pokémon may be entered: the Frontier's rule (no Egg, none of the banned).</summary>
    public static bool MayEnter(Pokemon pokemon) => FrontierRules.MayEnter(pokemon);

    /// <summary>
    /// The three a trainer of the table brings: the three of the highest level of their team as the trainer table
    /// builds it (the later among equals), in the team's order, at the tournament's level.
    /// </summary>
    public static List<Pokemon> ThreeOf(TrainerRecord record)
    {
        var team = TrainerDatabase.PartyOf(record).Members.ToList();
        var picked = team.Select((p, i) => (p, i)).OrderByDescending(x => x.p.Level).ThenByDescending(x => x.i).Take(TeamSize).OrderBy(x => x.i);
        return picked.Select(x => LevelScaling.At(x.p, Level)).ToList();
    }

    /// <summary>
    /// A tournament begun with the three entered (<paramref name="slots"/>): one walked out on is let go first, the
    /// event's always-in are seated and the rest drawn from its pool, and the eight are drawn into the bracket.
    /// </summary>
    public static Tournament Begin(FrontierRecords records, TournamentEventRecord ev, Party party, IReadOnlyList<int> slots, Func<int> next16)
    {
        var record = records.Tournament;
        record.InProgress = true;
        record.Entered = Math.Min(FrontierRecords.Most, record.Entered + 1);

        var own = slots.Where(s => s >= 0 && s < party.Count).Select(s => LevelScaling.At(party.Members[s], Level)).ToList();
        var field = new List<TournamentEntrant> { new() { Three = own } };
        var seated = new List<TournamentEntrantRecord>(ev.Always.Take(Entrants - 1));
        var pool = ev.Pool.Where(p => seated.All(s => s.Trainer != p.Trainer)).ToList();
        while (seated.Count < Entrants - 1 && pool.Count > 0)
        {
            int i = (next16() & 0xFFFF) % pool.Count;
            seated.Add(pool[i]);
            pool.RemoveAt(i);
        }
        foreach (var entrant in seated)
        {
            var trainer = TrainerDatabase.Get(entrant.Trainer) ?? throw new InvalidOperationException($"The World Tournament names no trainer called '{entrant.Trainer}'.");
            field.Add(new TournamentEntrant { Record = trainer, Look = entrant.Look, Three = ThreeOf(trainer) });
        }
        if (field.Count != Entrants) throw new InvalidOperationException($"The event '{ev.Id}' seats {field.Count}, not {Entrants}.");
        return new Tournament(ev, slots.ToList(), field, Bracket.Draw(Entrants, next16));
    }

    // ------------------------------------------------------------------ the round

    /// <summary>The round under way (0 to 2; 2 is the final), or 3 once the tournament is over.</summary>
    public int Round => Bracket.Round;

    public bool PlayerStillIn => Bracket.StillIn(Player);
    public bool PlayerWon => Bracket.Champion == Player;

    /// <summary>The one the player meets in the round under way, or null once they are out or it is over.</summary>
    public TournamentEntrant? Opponent => PlayerStillIn && !Bracket.IsOver && Bracket.OpponentOf(Round, Player) is >= 0 and var o ? Field[o] : null;

    /// <summary>The player's three at the tournament's level, fresh copies for a battle.</summary>
    public Party TeamForBattle() => LevelScaling.TeamAt(Field[Player].Three, Level);

    /// <summary>An entrant as a trainer of a battle: their three, fresh copies, no items in the bag, nothing paid.</summary>
    public Trainer AsTrainer(int entrant)
    {
        var who = Field[entrant];
        var r = who.Record ?? throw new InvalidOperationException("The player is no trainer of the tournament's.");
        return new Trainer
        {
            Id = r.Id, Name = r.Name, TrainerClass = r.Class, PrizeMoney = 0,
            Ai = Battle.Sim.Ai.AiFlagNames.Parse(r.Ai), Items = new List<string>(), Look = who.Look,
            Party = LevelScaling.TeamAt(who.Three, Level)
        };
    }

    /// <summary>The round's opponent as a trainer of the battle.</summary>
    public Trainer OpponentAsTrainer() =>
        Opponent is { } o ? AsTrainer(Field.ToList().IndexOf(o)) : throw new InvalidOperationException("The player meets nobody in this round.");

    // ------------------------------------------------------------------ after a match

    /// <summary>The player won the round's match: the rest of the round is fought headless, top to bottom. True when that was the final.</summary>
    public bool Won(FrontierRecords records, Func<int> next16)
    {
        int round = Round;
        Bracket.Decide(round, Bracket.MatchOf(round, Player), Player);
        records.Tournament.Matches = Math.Min(FrontierRecords.Most, records.Tournament.Matches + 1);
        Bracket.DecideRound(round, (a, b) => FightHeadless(a, b, next16));
        return Bracket.IsOver;
    }

    /// <summary>The player lost the round's match: the opponent goes on and the rest of the round is fought as above.</summary>
    public void Lost(Func<int> next16)
    {
        int round = Round;
        Bracket.Decide(round, Bracket.MatchOf(round, Player), Bracket.OpponentOf(round, Player));
        Bracket.DecideRound(round, (a, b) => FightHeadless(a, b, next16));
    }

    /// <summary>
    /// A match fought by the battle's core with nobody watching, the player's too when a test asks: a draw says which of
    /// the two stands on the player's side (the trainer AI thinks for the other side with its flags and stands in as
    /// <see cref="CoreSetup.PlayerController"/> on the player's, where it draws its moves), two more the battle's seed.
    /// The winner is the side left standing.
    /// </summary>
    public int FightHeadless(int a, int b, Func<int> next16)
    {
        bool aHome = (next16() & 1) == 0;
        uint seed = (uint)(next16() & 0xFFFF) | (uint)(next16() & 0xFFFF) << 16;
        int home = aHome ? a : b, away = aHome ? b : a;
        // The player stands on the player's side whenever they fight (the AI never thinks as the player)
        if (away == Player) (home, away) = (away, home);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = LevelScaling.TeamAt(Field[home].Three, Level),
            Trainers = new List<Trainer> { AsTrainer(away) },
            Random = new BattleRandom(seed),
            Rules = Ruleset.Current,
            PlayerController = TrainerAi.Instance,
            Frontier = true,
            PlayerName = Field[home].Record?.Name ?? "Player"
        });
        core.Start();
        int winner = core.Result switch
        {
            BattleResult.PlayerVictory => home,
            BattleResult.PlayerDefeat => away,
            _ => Share(core.PlayerParty) >= Share(core.Trainers[0].Party) ? home : away
        };
        Fought.Add((a, b, home, seed, winner));
        return winner;
    }

    private static float Share(Party side) =>
        side.Members.Sum(p => (float)p.CurrentHP) / Math.Max(1, side.Members.Sum(p => p.MaxHP));

    /// <summary>
    /// The whole tournament fought headless, the player's matches too (by the trainer AI standing in for the player,
    /// <see cref="CoreSetup.PlayerController"/>): how a test plays a bracket through. True when the player won it.
    /// </summary>
    public bool PlayHeadless(FrontierRecords records, Func<int> next16)
    {
        while (!Bracket.IsOver)
        {
            if (!PlayerStillIn)
            {
                Bracket.DecideRound(Round, (a, b) => FightHeadless(a, b, next16));
                continue;
            }
            int round = Round;
            int opponent = Bracket.OpponentOf(round, Player);
            if (FightHeadless(Player, opponent, next16) == Player) Won(records, next16);
            else Lost(next16);
        }
        Finish(records, PlayerWon);
        return PlayerWon;
    }

    // ------------------------------------------------------------------ the end

    /// <summary>The tournament is over: a title is counted for a win; either way it is no longer in progress.</summary>
    public void Finish(FrontierRecords records, bool won)
    {
        var record = records.Tournament;
        record.InProgress = false;
        if (won) record.Titles[Event.Id] = Math.Min(FrontierRecords.Most, record.TitlesOf(Event.Id) + 1);
    }

    /// <summary>The Battle Points the title earns: the event's own; nothing for a loss.</summary>
    public int PointsEarned => PlayerWon ? Event.Points : 0;
}
