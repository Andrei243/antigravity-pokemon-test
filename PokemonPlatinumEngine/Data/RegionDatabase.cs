using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Story;

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
            // In the player's room upstairs at home in Twinleaf Town, facing the television, as in Platinum (plan 02 · S4)
            Start = new MapSpot("PlayerHouse2F", 4, 4, Direction.Up),
            Maps = new[]
            {
                // The imported world: the overworld, and the places that are matrices of their own
                "Sinnoh", "LakeVerity", "OreburghGate1F", "OreburghGateB1F", "OreburghMineB1F", "OreburghMineB2F", "RavagedPath", "FloaromaMeadow",
                // Plan 01 · M6: Eterna Forest, the caves of the centre, Amity Square and the Solaceon Ruins' rooms
                "EternaForest", "WaywardCave1F", "WaywardCaveB1F", "MtCoronet1FSouth", "AmitySquare",
                "SolaceonRuinsRoom1", "SolaceonRuinsRoom1NorthwestDeadEnd", "SolaceonRuinsRoom1SoutheastDeadEnd",
                "SolaceonRuinsRoom2", "SolaceonRuinsRoom2NortheastDeadEnd", "SolaceonRuinsRoom2SoutheastDeadEnd",
                "SolaceonRuinsRoom3", "SolaceonRuinsRoom3NorthwestDeadEnd", "SolaceonRuinsRoom3SouthwestDeadEnd",
                "SolaceonRuinsRoom4", "SolaceonRuinsRoom4SoutheastDeadEnd", "SolaceonRuinsRoom5",
                "SolaceonRuinsRoom5SoutheastDeadEnd", "SolaceonRuinsRoom5SouthwestDeadEnd", "SolaceonRuinsRoom6",
                "SolaceonRuinsRoom6NorthwestDeadEnd", "SolaceonRuinsRoom6SoutheastDeadEnd", "SolaceonRuinsRoom7",
                // Plan 01 · M7: the east and the sea
                "RuinManiacCave", "LakeValor", "GreatMarsh", "TrophyGarden",
                "IronIsland1F", "IronIslandB1FLeft", "IronIslandB1FRight", "IronIslandB2FRight", "IronIslandB2FLeft", "IronIslandB3F",
                // Plan 01 · M8: Mt. Coronet's north and summit, Spear Pillar and Lake Acuity
                "MtCoronet1FNorthRoom1", "MtCoronet1FNorthRoom2", "MtCoronet1FTunnelRoom", "MtCoronetB1F", "MtCoronet2F", "MtCoronet3F",
                "MtCoronetOutsideSouth", "MtCoronetOutsideNorth", "MtCoronet4FRooms1And2", "MtCoronet4FRoom3", "MtCoronet5F", "MtCoronet6F",
                "SpearPillar", "LakeAcuity",
                // Plan 01 · M8: Victory Road
                "VictoryRoad1F", "VictoryRoad2F", "VictoryRoadB1F",
                // Plan 01 · M8, part 2: Sendoff Spring and Turnback Cave
                "SendoffSpring", "TurnbackCaveEntrance", "TurnbackCavePillarRoom", "TurnbackCaveGiratinaRoom",
                "TurnbackCavePillar1Room1", "TurnbackCavePillar1Room2", "TurnbackCavePillar1Room3", "TurnbackCavePillar1Room4", "TurnbackCavePillar1Room5", "TurnbackCavePillar1Room6", "TurnbackCavePillar2Room1", "TurnbackCavePillar2Room2", "TurnbackCavePillar2Room3", "TurnbackCavePillar2Room4", "TurnbackCavePillar2Room5", "TurnbackCavePillar2Room6", "TurnbackCavePillar3Room1", "TurnbackCavePillar3Room2", "TurnbackCavePillar3Room3", "TurnbackCavePillar3Room4", "TurnbackCavePillar3Room5", "TurnbackCavePillar3Room6",
                // Plan 01 · M8, part 2: the Distortion World, a floor to a map, and the room Turnback Cave's portal leads to
                "DistortionWorld1F", "DistortionWorldB1F", "DistortionWorldB2F", "DistortionWorldB3F", "DistortionWorldB4F",
                "DistortionWorldB5F", "DistortionWorldB6F", "DistortionWorldB7F", "DistortionWorldGiratinaRoom", "DistortionWorldTurnbackCaveRoom",
                // Plan 01 · M9, part 1: the Gyms rebuilt to the original's plans, with their puzzles
                "EternaGym", "HearthomeGym", "HearthomeGymRoom1", "HearthomeGymRoom2", "HearthomeGymLeaderRoom", "VeilstoneGym", "PastoriaGym", "CanalaveGym", "SnowpointGym",
                // Rooms, still made by hand (plan 01 · M11)
                "PlayerHouse", "PlayerHouse2F", "RivalHouse", "RivalHouse2F", "PokemonCenter", "PokeMart", "RowanLab",
                "JubilifePokemonCenter", "JubilifePokeMart", "JubilifeBoutique", "JubilifeTV1F", "TrainersSchool", "PoketchCompany",
                "OreburghPokemonCenter", "OreburghPokeMart", "OreburghGym", "FloaromaPokemonCenter", "FloaromaPokeMart",
                "EternaPokemonCenter", "EternaPokeMart", "HearthomePokemonCenter", "HearthomePokeMart", "SolaceonPokemonCenter", "SolaceonPokeMart",
                "VeilstonePokemonCenter", "PastoriaPokemonCenter", "PastoriaPokeMart", "CelesticPokemonCenter", "CanalavePokemonCenter", "CanalavePokeMart",
                "SnowpointPokemonCenter", "SnowpointPokeMart",
                "SunyshorePokemonCenter", "SunyshorePokeMart", "PokemonLeagueSouthPokemonCenter", "PokemonLeagueNorthPokemonCenter",
                // Rebuilt to the original's plans, so its people stand where the original's do: three houses of the
                // in-game trades (plan 06 · R12) and the Valley Windworks' hall (plan 02 · S6)
                "OreburghNorthHouse1F", "EternaCondominiums1F", "SnowpointWestHouse", "ValleyWindworksBuilding",
                // Eterna City's rooms of plan 02 · S6: the cycle shop, the Underground Man's house and Team Galactic's building
                "EternaCycleShop", "EternaUndergroundManHouse",
                "TeamGalacticEternaBuilding1F", "TeamGalacticEternaBuilding2F", "TeamGalacticEternaBuilding3F", "TeamGalacticEternaBuilding4F",
                // Hearthome's rooms of plan 02 · S7: the Contest Hall's lobby and the gate to Route 209
                "ContestHallLobby", "Route209GateToHearthomeCity",
                // Route 209's Lost Tower, its five floors of graves (plan 02 · S7)
                "LostTower1F", "LostTower2F", "LostTower3F", "LostTower4F", "LostTower5F",
                // Hearthome's Poffin House and Pokémon Fan Club (plan 06 · R14c)
                "PoffinHouse", "HearthomeFanClub",
                // Veilstone City's Galactic warehouse, where HM02 lies (plan 02 · S8)
                "VeilstoneGalacticWarehouse"
            }
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
    public static TravelCheck CheckTravel(RegionLink link, StoryState progress)
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
