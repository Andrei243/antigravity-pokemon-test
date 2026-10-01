using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>A place on a map where the player is put down, facing a direction.</summary>
public sealed record MapSpot(string Map, int X, int Y, Direction Facing = Direction.Down);

/// <summary>How the player crosses from one region to the next.</summary>
public enum Transport
{
    /// <summary>Not chosen yet: each later link gets its own way of travelling when its region is built.</summary>
    Undecided,
    Boat
}

/// <summary>
/// One region of the world, from one generation. Regions are separate worlds with their own maps and story; the
/// player starts in the first one and reaches each of the others by finishing the story of the one before it.
/// </summary>
public sealed class Region
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Generation { get; init; }

    /// <summary>Where a new game in this region begins; null while the region has no maps yet.</summary>
    public MapSpot? Start { get; init; }

    /// <summary>Where the player lands on arriving from the previous region; the start when not set.</summary>
    public MapSpot? Arrival { get; init; }

    /// <summary>Every map (outdoor and indoor) that belongs to this region.</summary>
    public IReadOnlyList<string> Maps { get; init; } = Array.Empty<string>();

    /// <summary>The story flag set when the player enters this region's Hall of Fame, which ends its story.</summary>
    public string StoryCompleteFlag => $"{Id}HallOfFame";

    /// <summary>True once the region has maps to play in.</summary>
    public bool IsBuilt => Start != null;

    public MapSpot? ArrivalSpot => Arrival ?? Start;
}

/// <summary>The way from one region to the next, open once the first region's story is finished.</summary>
public sealed class RegionLink
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required Transport Transport { get; init; }

    /// <summary>The map in <see cref="From"/> where the attendant who takes the player across stands.</summary>
    public string? DepartureMap { get; init; }
}

/// <summary>What happens when the player asks to travel along a link.</summary>
public enum TravelCheck
{
    /// <summary>The player can go now.</summary>
    Ready,

    /// <summary>The region the link leaves from hasn't had its story finished yet.</summary>
    StoryUnfinished,

    /// <summary>The story is finished, but the next region has no maps yet.</summary>
    DestinationNotBuilt
}

/// <summary>
/// The regions in generation order and the links that chain them: Kanto, then Johto by boat, then Hoenn and the
/// rest, each reached from the one before.
/// </summary>
public static class RegionDatabase
{
    public const string Kanto = "Kanto";
    public const string Johto = "Johto";
    public const string Hoenn = "Hoenn";
    public const string Sinnoh = "Sinnoh";
    public const string Unova = "Unova";
    public const string Kalos = "Kalos";
    public const string Alola = "Alola";
    public const string Galar = "Galar";
    public const string Paldea = "Paldea";

    private static readonly Region[] regions =
    {
        new()
        {
            Id = Kanto, Name = "Kanto", Generation = 1,
            // A stand-in Pallet Town until the Kanto chapter is built
            Start = new MapSpot("PalletTown", 9, 9),
            Maps = new[] { "PalletTown", "PalletPlayerHouse" }
        },
        new() { Id = Johto, Name = "Johto", Generation = 2 },
        new() { Id = Hoenn, Name = "Hoenn", Generation = 3 },
        new()
        {
            Id = Sinnoh, Name = "Sinnoh", Generation = 4,
            Start = new MapSpot("TwinleafTown", 11, 8),
            Maps = new[] { "TwinleafTown", "Route201", "LakeVerity", "SandgemTown", "Route202", "PlayerHouse", "RivalHouse", "PokemonCenter", "PokeMart", "RowanLab",
                "JubilifeCity", "JubilifePokemonCenter", "JubilifePokeMart", "TrainersSchool", "PoketchCompany" }
        },
        new() { Id = Unova, Name = "Unova", Generation = 5 },
        new() { Id = Kalos, Name = "Kalos", Generation = 6 },
        new() { Id = Alola, Name = "Alola", Generation = 7 },
        new() { Id = Galar, Name = "Galar", Generation = 8 },
        new() { Id = Paldea, Name = "Paldea", Generation = 9 }
    };

    private static readonly RegionLink[] links =
    {
        // As in HeartGold and SoulSilver, but the other way round: the ship sails from Vermilion to Olivine.
        // Pallet Town's pier stands in for Vermilion's harbour until Vermilion exists.
        new() { From = Kanto, To = Johto, Transport = Transport.Boat, DepartureMap = "PalletTown" },
        new() { From = Johto, To = Hoenn, Transport = Transport.Undecided },
        new() { From = Hoenn, To = Sinnoh, Transport = Transport.Undecided },
        new() { From = Sinnoh, To = Unova, Transport = Transport.Undecided },
        new() { From = Unova, To = Kalos, Transport = Transport.Undecided },
        new() { From = Kalos, To = Alola, Transport = Transport.Undecided },
        new() { From = Alola, To = Galar, Transport = Transport.Undecided },
        new() { From = Galar, To = Paldea, Transport = Transport.Undecided }
    };

    /// <summary>Every region, in generation order.</summary>
    public static IReadOnlyList<Region> All => regions;

    public static IReadOnlyList<RegionLink> Links => links;

    /// <summary>The region a new game starts in.</summary>
    public static Region First => regions[0];

    public static Region? Get(string id) =>
        regions.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The region a map belongs to, or null for a map no region lists.</summary>
    public static Region? RegionOfMap(string mapName) =>
        regions.FirstOrDefault(r => r.Maps.Contains(mapName, StringComparer.OrdinalIgnoreCase));

    /// <summary>The way on from a region to the next one, or null for the last region.</summary>
    public static RegionLink? LinkFrom(string regionId) =>
        links.FirstOrDefault(l => string.Equals(l.From, regionId, StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the player may take a link now, given what they have done.</summary>
    public static TravelCheck CheckTravel(RegionLink link, StoryProgress progress)
    {
        var from = Get(link.From)!;
        if (!progress.Has(from.StoryCompleteFlag)) return TravelCheck.StoryUnfinished;
        return Get(link.To)!.IsBuilt ? TravelCheck.Ready : TravelCheck.DestinationNotBuilt;
    }

    /// <summary>What the person who takes the player across says, for each outcome of <see cref="CheckTravel"/>.</summary>
    public static List<string> AttendantLines(RegionLink link, TravelCheck check)
    {
        var from = Get(link.From)!;
        var to = Get(link.To)!;
        string vessel = link.Transport == Transport.Boat ? "ship" : "way";
        return check switch
        {
            TravelCheck.StoryUnfinished => new()
            {
                $"This {vessel} goes to {to.Name}.",
                $"Only trainers who have entered {from.Name}'s Hall of Fame may come aboard. Come back when you're Champion!"
            },
            TravelCheck.DestinationNotBuilt => new()
            {
                $"Congratulations, Champion of {from.Name}!",
                $"The {vessel} to {to.Name} isn't ready to leave yet. Check back another time."
            },
            _ => new()
            {
                $"Congratulations, Champion of {from.Name}! The {vessel} to {to.Name} is ready.",
                "All aboard!"
            }
        };
    }
}

/// <summary>
/// What the player has done in the story, as a set of named flags. Each region's story ends with its
/// <see cref="Region.StoryCompleteFlag"/>, which opens the way to the next region.
/// </summary>
public sealed class StoryProgress
{
    private readonly HashSet<string> flags = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Flags => flags;

    public bool Has(string flag) => flags.Contains(flag);

    public void Set(string flag) => flags.Add(flag);

    public void Clear() => flags.Clear();

    public void Restore(IEnumerable<string> saved)
    {
        flags.Clear();
        foreach (var f in saved) flags.Add(f);
    }

    public bool IsRegionComplete(string regionId) => Get(regionId) is { } r && Has(r.StoryCompleteFlag);

    /// <summary>Marks a region's story as finished.</summary>
    public void CompleteRegion(string regionId)
    {
        if (Get(regionId) is { } r) Set(r.StoryCompleteFlag);
    }

    private static Region? Get(string id) => RegionDatabase.Get(id);
}
