using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The medals (plan 13 · V9): judged whenever the counters or the story have moved, kept in the save whatever the
/// options say, and told by a notice with its fanfare only while the options' MEDAL NOTICES row is on.
/// </summary>
public partial class GameEngine
{
    /// <summary>The counters' and the story's revisions the medals were last judged at; -1 to judge them quietly (a game begun or loaded).</summary>
    private long medalsLookedAt = -1;

    /// <summary>What the medals are judged by now.</summary>
    internal MedalFacts MedalFactsNow() => new()
    {
        Counts = flow.Statistics,
        Seen = flow.Pokedex.SeenCount,
        Caught = flow.Pokedex.CaughtCount,
        BadgeCount = flow.Story.TotalBadges,
        ChampionCount = RegionDatabase.All.Count(r => flow.Story.IsRegionComplete(r.Id))
    };

    /// <summary>
    /// Judges the medals when anything they read may have moved, and moves the notice on while the field shows: a
    /// medal won is kept at once, and told (with the fanfare as its notice drops in) only with the option on. A game
    /// begun or loaded wins quietly what its counters already reach.
    /// </summary>
    private void KeepTheMedals(float dt)
    {
        if (!gameStarted) return;
        long now = ((long)flow.Statistics.Revision << 32) ^ (uint)flow.Story.Revision;
        if (now != medalsLookedAt)
        {
            bool quietly = medalsLookedAt == -1;
            medalsLookedAt = now;
            var won = flow.Medals.Check(MedalFactsNow());
            if (!quietly && Settings.MedalNotices)
                foreach (var medal in won) flow.MedalNotice.Enqueue(medal);
        }
        if (flow.State is GameState.Overworld or GameState.Dialogue && flow.MedalNotice.Update(dt))
            AudioManager.PlayFanfare(MusicRole.FanfareMedal);
    }
}
