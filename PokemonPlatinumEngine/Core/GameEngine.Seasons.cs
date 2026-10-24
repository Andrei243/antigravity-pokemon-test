using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Unova's seasons on the field (plan 22 · U3; style guide, "Seasons"). The calendar's season is
/// <see cref="GameClock.Season"/>; the season the field shows is held here and moves only where the picture is dark:
/// as a game is begun or taken up, when the options fix another, and behind the next fade once the calendar has turned
/// (told on the field). Only a region whose record says so has one (<see cref="Region.HasSeasons"/>): everywhere else
/// <see cref="SeasonHere"/> is null and nothing is baked, lit or told otherwise.
/// </summary>
public partial class GameEngine
{
    // The season the field is drawn in. Set from the calendar whenever the picture may change at once.
    private Season season = GameClock.Season;

    // Whether each map's region has seasons, asked once a map (RegionOfMap goes through every region's maps)
    private readonly Dictionary<string, bool> seasonalMaps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The season of the place the player stands in, or null where the region has none (every region but Unova).</summary>
    public Season? SeasonHere => flow.Map != null ? SeasonOn(flow.Map) : null;

    /// <summary>The season a map is drawn in: the field's, on a map of a region with seasons; null on any other.</summary>
    internal Season? SeasonOn(Map map)
    {
        if (!seasonalMaps.TryGetValue(map.Name, out bool seasonal))
            seasonalMaps[map.Name] = seasonal = RegionDatabase.RegionOfMap(map.Name)?.HasSeasons == true;
        return seasonal ? season : null;
    }

    /// <summary>Takes the calendar's season (or the options') without a word: a game begun or taken up, an option changed.</summary>
    private void TakeTheSeason()
    {
        season = GameClock.Season;
        LayTheSeason();
    }

    /// <summary>
    /// Behind a fade that opens on the field: if the calendar has turned to another season, the field takes it (the
    /// renderer makes the scenes again, which is a load behind the dark) and, in a region with seasons, says so.
    /// </summary>
    private void TurnTheSeason()
    {
        var now = GameClock.Season;
        if (now == season) return;
        season = now;
        LayTheSeason();
        if (flow.After == GameState.Overworld && SeasonHere != null)
            flow.Toast.Show(SeasonArrived(now, Here.Name));
    }

    /// <summary>
    /// What the field's season changes beside its look (plan 22 · U4): each map's chunks of the season laid in place of
    /// spring's, whatever was drawn of them made again, and the coats of the team's Deerling and Sawsbuck while the
    /// player stands in a region with seasons. Laid every time, since the maps may be new.
    /// </summary>
    private void LayTheSeason()
    {
        foreach (var (map, place) in MapDatabase.ApplySeason(season))
            foreach (var (cx, cy) in place.Cells)
                world?.Forget(map, cx * Map.ChunkTiles, cy * Map.ChunkTiles, Map.ChunkTiles, Map.ChunkTiles);
        KeepTheCoats();
    }

    /// <summary>
    /// The team's Deerling and Sawsbuck in the coat of the season here (<see cref="SeasonalForms"/>, plan 22 · U4): as a
    /// game is taken up, as the season turns and as one comes out of a box. Outside a region with seasons a coat stays
    /// what it is, as the later games keep it.
    /// </summary>
    private void KeepTheCoats()
    {
        if (flow.Map == null || SeasonHere is not { } here) return;
        foreach (var pokemon in flow.Party.Members) SeasonalForms.Wear(pokemon, here);
    }

    /// <summary>A wild Pokémon met in a region with seasons wears the season's coat (Deerling and Sawsbuck, plan 22 · U4).</summary>
    private void DressForTheSeason(Pokemon wild)
    {
        if (SeasonHere is { } here) SeasonalForms.Wear(wild, here);
    }

    /// <summary>What the field's notice says as a season comes ("Spring has come to Unova.").</summary>
    internal static string SeasonArrived(Season season, string region) => Text.Of($"seasons.arrived.{season}", ("region", region));
}
