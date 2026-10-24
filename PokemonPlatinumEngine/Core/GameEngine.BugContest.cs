using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The National Park's Bug-Catching Contest in the game (plan 15 · E5): its minutes counted while the field or one of
/// its battles is on screen, the park's grass giving the contest's own table, a catch handed to the contest and the
/// question of which to keep, the corner over the field, and its end when the minutes or the Sport Balls run out. The
/// rules are <see cref="BugContest"/>; the gates' counters and the judging are scripts (<c>common.txt</c>'s
/// <c>BugContest</c> scripts).
/// </summary>
public partial class GameEngine
{
    /// <summary>The map the contest is held on, and the two gate houses into it.</summary>
    public const string BugContestPark = "NationalPark";

    /// <summary>What the contest's end starts: its minutes run out, its last Sport Ball thrown, a second catch.</summary>
    public const string BugContestTimeUpScript = "common.BugContestTimeUp", BugContestOutOfBallsScript = "common.BugContestOutOfBalls",
        BugContestSwapScript = "common.BugContestSwap";

    /// <summary>A contest is under way and the player is in its park: the grass is the contest's, the balls the Sport Balls.</summary>
    private bool InBugContest => flow.BugContest.Active && !flow.BugContest.Judged && flow.Map.Name == BugContestPark;

    /// <summary>
    /// The contest's minutes, while the field is the player's or a script plays over it; once they are up in the park
    /// and nothing else is happening, the time-up script takes the player to the judging.
    /// </summary>
    private void TickBugContest(float dt)
    {
        var contest = flow.BugContest;
        if (!contest.Active || contest.Judged) return;
        contest.Tick(dt);
        if (contest.TimeUp && flow.State == GameState.Overworld && !ScriptRunning && flow.Map.Name == BugContestPark)
            StartScript(BugContestTimeUpScript);
    }

    /// <summary>
    /// A wild Pokémon met by a step (<c>BugContest_GetEncounterSlot</c>): in the park's grass during a contest the
    /// species and level are the contest's table's, drawn on the field's chance; anywhere else as the place has it.
    /// </summary>
    private void MeetInTheField(WildEncounterEntry entry)
    {
        if (InBugContest && flow.Player.Mode != TravelMode.Surfing && entry.Roamer == null)
        {
            var (species, level) = flow.BugContest.Meet(fieldRandom);
            entry = new WildEncounterEntry { SpeciesName = species, MinLevel = level, MaxLevel = level, Gender = entry.Gender, Nature = entry.Nature };
        }
        StartWildBattle(entry);
    }

    /// <summary>
    /// A battle of the contest is over (from <c>EndBattle</c>, once the field is back): the Sport Balls it didn't
    /// throw are the contest's, a catch is kept or asked about, and the last ball ends the contest.
    /// </summary>
    private void AfterContestBattle()
    {
        var contest = flow.BugContest;
        if (!contest.Active || flow.Battle?.OnlyBall == null) return;
        contest.Balls = flow.Battle.SpecialBalls;
        if (flow.Battle.Caught is { } caught && contest.Catch(caught))
        {
            StartScript(BugContestSwapScript);
            return;
        }
        if (contest.Balls <= 0) StartScript(BugContestOutOfBallsScript);
    }

    /// <summary>A battle lost in the park: the contest is given up and the team put back before the player wakes up.</summary>
    private void AbandonBugContest()
    {
        if (!flow.BugContest.Active) return;
        flow.BugContest.Abandon(flow.Party);
        foreach (string flag in Story.ScriptRunner.BugContestFlags) flow.Story.Unset(flag);
    }

    /// <summary>A contest read back from a save, or none: the team set aside stays aside.</summary>
    private void ResumeBugContest(BugContestSave? save)
    {
        if (save == null) flow.BugContest.Reset();
        else flow.BugContest.Resume(save, flow.Party);
    }

    /// <summary>The corner over the field while a contest is under way in the park (style guide, "The Bug-Catching Contest's corner").</summary>
    private void DrawBugContestCount()
    {
        var contest = flow.BugContest;
        if (!contest.Active || contest.Judged || flow.Map.Name != BugContestPark) return;
        ModernUi.BugContestCount(VirtualWidth, contest.Balls, contest.SecondsLeft, contest.Venue.Balls, contest.Length);
    }

    /// <summary>The board of results, for a script's <c>bugcontest results</c>.</summary>
    private void OpenBugContestResults()
    {
        flow.BugContestResultsScreen.Open(flow.BugContest);
        flow.State = GameState.BugContestResults;
        // The winner's catch is shown on its disc: its picture is asked for now, to be baked by the time first place is told
        if (flow.BugContest.Ranking.Count > 0 && flow.BugContest.Ranking[0].Species.Length > 0)
            Graphics.PokemonSprites.Request(flow.BugContest.Ranking[0].Species);
    }
}
