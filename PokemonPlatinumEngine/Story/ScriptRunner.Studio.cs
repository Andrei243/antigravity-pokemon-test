using PokemonPlatinumEngine.Models.Studio;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The film studio's command (plan 42 · PS4; <c>docs/scripts.md</c>, "The film studio"): <c>studio shoot "id"</c> shoots
/// a take of a film on its own battle and waits for it, <c>RESULT</c> the take's stars (0 where the host has no studio
/// or the film isn't open); <c>studio open "id"</c> 1 where the studio's rank opens the film; <c>studio stars "id"</c>
/// the film's best stars; <c>studio rank</c> the studio's rank, the best stars summed.
/// </summary>
public sealed partial class ScriptRunner
{
    private void StudioCommand(Instruction i)
    {
        var records = host.Studio;
        var film = i.Other is { } id ? StudioFilms.Get(id) : null;
        switch (i.Name)
        {
            case "rank":
                Result = records?.Rank ?? 0;
                break;
            case "open":
                Result = records != null && film != null && records.Opens(film) ? 1 : 0;
                break;
            case "stars":
                Result = records != null && film != null ? records.BestOf(film.Id) : 0;
                break;
            case "shoot":
                if (records == null || film == null || !records.Opens(film))
                {
                    Result = 0;
                    break;
                }
                host.StudioShoot(film);
                afterBusy = () => Result = host.Studio?.LastTake?.Stars ?? 0;
                break;
        }
    }
}
