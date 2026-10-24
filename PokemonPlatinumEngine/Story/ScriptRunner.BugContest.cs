using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 15 · E5: <c>bugcontest</c>, the National Park's Bug-Catching Contest (<see cref="BugContest"/>),
/// each word on the lines of HeartGold's script it stands for (the gates' counters and the park's judging).
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The flag that is set while a contest is under way: the park's own people keep away (HeartGold's <c>FLAG_UNK_24E</c>).</summary>
    public const string BugContestUnderWayFlag = "FLAG_BUG_CONTEST_UNDER_WAY";

    /// <summary>The flag that shows a contestant in the park while they are entered (HeartGold's <c>FLAG_HIDE_BUG_CONTESTANT_*</c>, turned round).</summary>
    public static string BugContestantFlag(string name) => "FLAG_BUG_CONTESTANT_" + name.ToUpperInvariant();

    /// <summary>Every flag <c>bugcontest</c> sets and clears by itself, for the content check.</summary>
    public static IEnumerable<string> BugContestFlags =>
        BugContestVenue.NationalPark.Contestants.Select(c => BugContestantFlag(c.Name)).Append(BugContestUnderWayFlag);

    /// <summary>
    /// <c>open</c> is 1 on a day the contest is held, <c>on</c> 1 while one is under way and not yet judged. <c>start</c> begins one (RESULT 1; 0 with no Pokémon able to go in):
    /// the team set aside but for its first able Pokémon, the twenty Sport Balls, the five others drawn and shown in
    /// the park. <c>balls</c> is the Sport Balls left, <c>caught</c> 1 while a Pokémon is kept (<c>{bugmon}</c>),
    /// <c>keep</c> keeps the one just caught (<c>{bugnew}</c>) in its place and <c>release</c> lets it go. <c>judge</c>
    /// judges the six (RESULT the player's place, 0 first to 3 none of the three; <c>{score}</c>, <c>{prize}</c>),
    /// <c>results</c> opens the board of them, <c>prize</c> hands the prize over and <c>end</c> gives the team back with
    /// the catch (RESULT 1 on the team, 2 in the PC, 0 nothing caught) and sends the others home.
    /// </summary>
    private void BugContestCommand(Instruction i)
    {
        var contest = host.BugContest;
        switch (i.Name)
        {
            case "open":
                Result = BugContestVenue.NationalPark.OpenOn(host.Weekday) ? 1 : 0;
                break;

            // BugContest_New, from the gate's counter
            case "start":
            {
                if (!contest.Start(host.Party, (int)host.Weekday, host.NationalDex, host.Chance))
                {
                    Result = 0;
                    break;
                }
                foreach (var c in contest.Venue.Contestants) host.Story.Unset(BugContestantFlag(c.Name));
                foreach (var e in contest.Entrants) host.Story.Set(BugContestantFlag(contest.NameOf(e.Id)));
                host.Story.Set(BugContestUnderWayFlag);
                Result = 1;
                break;
            }

            case "on":
                Result = contest.Active && !contest.Judged ? 1 : 0;
                break;

            case "balls":
                Result = contest.Active ? contest.Balls : 0;
                break;

            case "caught":
                Result = contest.Caught != null ? 1 : 0;
                break;

            case "keep":
            case "release":
                contest.Keep(i.Name == "keep");
                break;

            // BugContest_Judge
            case "judge":
                // (Judged once: asked again, or with no contest under way, it is the place it came to, or none of the three)
                if (contest.Active && !contest.Judged) contest.JudgeAll(host.Chance, host.LevelHere);
                Result = contest.Placement;
                break;

            case "results":
                host.Open(ScriptScreen.BugContestResults, Subject);
                break;

            // The prize the judging gave, with the item's fanfare and the lines a gift says
            case "prize":
            {
                // (Nothing before the judging, and nothing a second time)
                if (ItemDatabase.Get(contest.Prize) is not { } item) break;
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string>
                {
                    Text.Of("found.received", ("n", 1), ("item", item.DisplayName)),
                    Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket)))
                });
                break;
            }

            // BugContest_Delete: the team back, the catch on it or in the PC, and the others gone home
            case "end":
            {
                var caught = contest.Finish(host.Party);
                contest.ForgetResults();
                Result = 0;
                if (caught != null)
                {
                    if (host.Party.Add(caught)) Result = 1;
                    else
                    {
                        host.PutInBox(caught);
                        Result = 2;
                    }
                }
                foreach (var c in contest.Venue.Contestants) host.Story.Unset(BugContestantFlag(c.Name));
                host.Story.Unset(BugContestUnderWayFlag);
                break;
            }
        }
    }
}
