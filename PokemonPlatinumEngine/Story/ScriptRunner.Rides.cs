using PokemonPlatinumEngine.Models.Rides;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Plan 42 · PS7 (<c>docs/scripts.md</c>, "The rides"): the command <c>ride</c>'s words for the rides of the beaches,
/// which a beach's surf hut says (the Ride Pager's <c>ride "Tauros"</c> and <c>ride off</c> are another op).
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>ride surf "hauoli"</c>: the course ridden on the screen (the script waits): RESULT the player's place, 1 to 4,
    /// or 0 backed out of; <c>{number}</c> the Battle Points it paid. <c>ride runs "hauoli"</c>: RESULT the times it has
    /// been ridden. <c>ride best "hauoli"</c>: RESULT and <c>{number}</c> its best score.
    /// </summary>
    private void RideCommand(Instruction i)
    {
        var records = host.Rides;
        string course = i.Other ?? "";
        switch (i.Name)
        {
            case "surf":
                if (records == null || SurfCourses.Get(course) == null) { Result = 0; lastNumber = 0; break; }
                host.Open(ScriptScreen.Ride, Subject, course);
                afterBusy = () =>
                {
                    Result = host.Answer;
                    lastNumber = host.Answer > 0 ? records.LastPaid : 0;
                };
                break;
            case "race":
                // The player's own mount (RESULT -1 with none of a riding size and able): the screen is told the track as race:<id>
                if (records == null || RaceTracks.Get(course) == null) { Result = 0; lastNumber = 0; break; }
                if (host.RaceMount == null) { Result = -1; lastNumber = 0; break; }
                host.Open(ScriptScreen.Ride, Subject, RaceTracks.Prefix + course);
                afterBusy = () =>
                {
                    Result = host.Answer;
                    lastNumber = host.Answer > 0 ? records.LastPaid : 0;
                };
                break;
            case "raceruns":
                Result = records?.RunsOf(RaceTracks.Prefix + course) ?? 0;
                break;
            case "racebest":
                Result = records?.BestOf(RaceTracks.Prefix + course) ?? 0;
                lastNumber = Result;
                break;
            case "runs":
                Result = records?.RunsOf(course) ?? 0;
                break;
            case "best":
                Result = records?.BestOf(course) ?? 0;
                lastNumber = Result;
                break;
        }
    }
}
