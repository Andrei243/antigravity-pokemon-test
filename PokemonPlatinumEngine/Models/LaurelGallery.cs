using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>A plinth of the Laurel Gallery: whose teams it shows (a region's id, <c>earlier</c> or <c>Circuit</c>) and its tile.</summary>
public readonly record struct GalleryPlinth(string Of, int X, int Y)
{
    /// <summary>The gallery's script read on its tile.</summary>
    public string Script => "Plinth" + Of.Replace("earlier", "Earlier");
}

/// <summary>
/// The Laurel Gallery, the hub's hall of Champions (plan 41 · EG2; <c>docs/mechanics/rulings.md</c>, "The Laurel Gallery"):
/// which plinths stand in it as the story stands, worked out whenever asked, so nothing is saved. A plinth for each region
/// of the chain finished, in the chain's order on the gallery's places; one for EARLIER TEAMS where teams entered before
/// a team's region was kept and more than one region is finished; and the circuit's, once a final is won. GPU-free.
/// </summary>
public static class LaurelGallery
{
    public const string Map = "Hub_HallOfChampions";

    /// <summary>The places of the chain's regions, in its order: two columns each side of the aisle, three rows.</summary>
    public static IReadOnlyList<(int X, int Y)> RegionPlaces { get; } = new[]
    {
        (3, 5), (6, 5), (3, 7), (6, 7), (3, 9), (6, 9), (12, 5), (15, 5), (12, 7),
    };

    /// <summary>EARLIER TEAMS' place and the circuit's.</summary>
    public static readonly (int X, int Y) EarlierPlace = (12, 9), CircuitPlace = (9, 3);

    /// <summary>The plinths standing now.</summary>
    public static IReadOnlyList<GalleryPlinth> Plinths(StoryState story, HallOfFame records, CircuitState circuit)
    {
        var plinths = new List<GalleryPlinth>();
        var chain = RegionDatabase.All.Where(r => !r.Side).ToList();
        int finished = 0;
        for (int k = 0; k < chain.Count && k < RegionPlaces.Count; k++)
        {
            if (!story.Has(chain[k].StoryCompleteFlag)) continue;
            finished++;
            plinths.Add(new GalleryPlinth(chain[k].Id, RegionPlaces[k].X, RegionPlaces[k].Y));
        }
        if (records.HasEarlier && finished > 1) plinths.Add(new GalleryPlinth("earlier", EarlierPlace.X, EarlierPlace.Y));
        if (circuit.Finals.Count > 0) plinths.Add(new GalleryPlinth(ChampionsCircuit.CircuitRegion, CircuitPlace.X, CircuitPlace.Y));
        return plinths;
    }

    /// <summary>Every plinth the gallery could ever stand, for the content check and the tests: each region of the chain's place, EARLIER TEAMS' and the circuit's.</summary>
    public static IEnumerable<GalleryPlinth> EveryPlace() =>
        RegionDatabase.All.Where(r => !r.Side).Take(RegionPlaces.Count).Select((r, k) => new GalleryPlinth(r.Id, RegionPlaces[k].X, RegionPlaces[k].Y))
            .Append(new GalleryPlinth("earlier", EarlierPlace.X, EarlierPlace.Y))
            .Append(new GalleryPlinth(ChampionsCircuit.CircuitRegion, CircuitPlace.X, CircuitPlace.Y));
}
