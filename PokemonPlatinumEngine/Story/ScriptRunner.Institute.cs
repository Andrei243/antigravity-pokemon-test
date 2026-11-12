using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// <c>facility institute &lt;word&gt;</c>, Nimbasa's Battle Institute's test and its ladder (plan 37 · FC8,
/// <see cref="InstituteTest"/>, <see cref="Ladder"/>), each played at the desk a battle at a time.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The institute's word under <c>facility</c>.</summary>
    public const string InstitutePlace = "institute";

    /// <summary>The ladder's opponent of the game to come, by rating, and the seed of their draw.</summary>
    public const string LadderOpponentVariable = "VAR_UNOVA_LADDER_OPPONENT", LadderSeedVariable = "VAR_UNOVA_LADDER_SEED";

    private IReadOnlyList<int> instituteSlots = Array.Empty<int>();

    /// <summary>
    /// The test: <c>test single|double</c> chooses the format; <c>choose</c> has those going in chosen (three, or four in a
    /// double; RESULT 1); <c>testbegin</c> begins the five; <c>testnext</c> stands the battle to come's trainer (<c>{foe}</c>,
    /// <c>{number}</c>); <c>testbattle</c> fights it and scores it (RESULT 1 won, 0 lost; <c>{number}</c> the battle's score);
    /// <c>testscore</c> is the total (<c>{number}</c>; RESULT its rank, 0 to 5); <c>testend</c> keeps the best of the format
    /// (RESULT 1 a new best; <c>{number}</c> its place on the board). The ladder: <c>season</c> brings the season to the
    /// game's month (RESULT the Battle Points the season gone paid, which it hands over; <c>{number}</c> the rating);
    /// <c>ladderformat</c> is the season's (1 single, 2 double); <c>laddernext</c> draws the next opponent about the rating
    /// and stands them (<c>{foe}</c>, <c>{number}</c> their rating); <c>ladderbattle</c> fights them (RESULT 1 won;
    /// <c>{number}</c> the rating after); <c>band</c> is the rating's band (RESULT 0 to 4, <c>{number}</c> the rating).
    /// </summary>
    private void InstituteCommand(Instruction i)
    {
        var story = host.Story;
        int format = Math.Clamp(story.Var(InstituteTest.FormatVariable), 1, 2);
        int season = Ladder.SeasonOf(host.Today);
        switch (i.Other)
        {
            case "test":
                story.SetVar(InstituteTest.FormatVariable, i.Move == "double" ? 2 : 1);
                Result = 1;
                break;
            case "choose":
            {
                int size = i.Move == "ladder" ? Ladder.EnteredIn(season) : InstituteTest.EnteredIn(format);
                Result = host.FrontierAct("enter", size, Subject);
                afterBusy = () =>
                {
                    instituteSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && instituteSlots.Count == size ? 1 : 0;
                };
                break;
            }
            case "testbegin":
                if (instituteSlots.Count != InstituteTest.EnteredIn(format)) throw Wrong(i, "'facility institute testbegin' needs those going in chosen");
                story.SetVar(InstituteTest.BattleVariable, 0);
                story.SetVar(InstituteTest.ScoreVariable, 0);
                story.SetVar(InstituteTest.SeedVariable, host.Chance.Next(0x10000));
                host.Frontier.ChallengesStarted++;
                Result = 1;
                break;
            case "testnext":
            {
                int battle = story.Var(InstituteTest.BattleVariable) + 1;
                StandFacilityFoe(InstituteTest.Foe(format, battle, story.Var(InstituteTest.SeedVariable)));
                Result = lastNumber = battle;
                break;
            }
            case "testbattle":
            {
                int battle = story.Var(InstituteTest.BattleVariable) + 1;
                var foe = InstituteTest.Foe(format, battle, story.Var(InstituteTest.SeedVariable));
                int foeTiers = foe.Sets.Sum(s => Data.FacilitySetDatabase.Get(s)?.Tier ?? 0);
                var team = FrontierRules.TeamFor(host.Party, instituteSlots);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe.AsTrainer() }, null, doubles: format == 2,
                    new FrontierConditions(Format: InstituteTest.FormatOf(format)));
                afterBusy = () =>
                {
                    bool won = Outcome() == 1;
                    int score = InstituteTest.ScoreOf(won, host.BattleTurns, InstituteTest.HpKept(team), foeTiers);
                    story.SetVar(InstituteTest.BattleVariable, battle);
                    story.SetVar(InstituteTest.ScoreVariable, Math.Min(InstituteTest.MostScore, story.Var(InstituteTest.ScoreVariable) + score));
                    lastNumber = score;
                    Result = won ? 1 : 0;
                };
                break;
            }
            case "testleft":
                Result = lastNumber = Math.Max(0, InstituteTest.Battles - story.Var(InstituteTest.BattleVariable));
                break;
            case "testscore":
                lastNumber = story.Var(InstituteTest.ScoreVariable);
                Result = InstituteTest.RankOf(lastNumber);
                break;
            case "testend":
            {
                int total = story.Var(InstituteTest.ScoreVariable);
                string best = InstituteTest.BestVariable(format);
                Result = total > story.Var(best) ? 1 : 0;
                if (Result == 1) story.SetVar(best, total);
                lastNumber = InstituteTest.PlaceOf(Math.Max(story.Var(InstituteTest.BestVariable(1)), story.Var(InstituteTest.BestVariable(2))));
                story.SetVar(InstituteTest.BattleVariable, 0);
                instituteSlots = Array.Empty<int>();
                break;
            }
            case "season":
            {
                int prize = Ladder.TurnSeason(story, season);
                if (prize > 0)
                {
                    host.Frontier.GivePoints(prize);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = prize;
                lastNumber = story.Var(Ladder.RatingVariable);
                break;
            }
            case "ladderformat":
                Result = Ladder.FormatOf(season) == Battle.BattleFormat.Double ? 2 : 1;
                break;
            case "laddernext":
            {
                int seed = host.Chance.Next(0x10000);
                var rng = new Battle.Sim.BattleRandom((uint)seed);
                int opponent = Ladder.OpponentRating(story.Var(Ladder.RatingVariable), rng.Next16);
                story.SetVar(LadderOpponentVariable, opponent);
                story.SetVar(LadderSeedVariable, seed);
                StandFacilityFoe(Ladder.Opponent(season, opponent, rng.Next16));
                Result = lastNumber = opponent;
                break;
            }
            case "ladderbattle":
            {
                if (instituteSlots.Count != Ladder.EnteredIn(season)) throw Wrong(i, "'facility institute ladderbattle' needs those going in chosen");
                int opponent = story.Var(LadderOpponentVariable);
                var rng = new Battle.Sim.BattleRandom((uint)story.Var(LadderSeedVariable));
                Ladder.OpponentRating(story.Var(Ladder.RatingVariable), rng.Next16);
                var foe = Ladder.Opponent(season, opponent, rng.Next16);
                var team = FrontierRules.TeamFor(host.Party, instituteSlots);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe.AsTrainer() }, null, doubles: Ladder.FormatOf(season) == Battle.BattleFormat.Double,
                    new FrontierConditions(Format: Ladder.FormatOf(season)));
                afterBusy = () =>
                {
                    bool won = Outcome() == 1;
                    Ladder.Played(story, opponent, won);
                    lastNumber = story.Var(Ladder.RatingVariable);
                    Result = won ? 1 : 0;
                };
                break;
            }
            case "band":
                lastNumber = story.Var(Ladder.RatingVariable);
                Result = Ladder.BandOf(lastNumber);
                break;
        }
    }
}
