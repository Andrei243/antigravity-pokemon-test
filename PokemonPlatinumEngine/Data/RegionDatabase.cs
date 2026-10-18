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

    /// <summary>What the region's file holds (plan 18 · W1); null for a region with no file yet.</summary>
    public RegionFile? File { get; init; }

    /// <summary>The folder of the region's world and music; its lower-cased id when it has no file.</summary>
    public string Folder => File?.Folder is { Length: > 0 } f ? f : Id.ToLowerInvariant();

    /// <summary>The catalogue of models its world's props are looked up in (see <see cref="WorldModels.Catalogue"/>).</summary>
    public string Models => File?.Models is { Length: > 0 } m ? m : Folder;

    /// <summary>The region's overworld map, where Fly, Teleport and a way out noted for Dig land.</summary>
    public string? OverworldMap => File?.Map is { Length: > 0 } m ? m : null;

    /// <summary>Whether a new game here begins with the region's story rather than a Pokémon and a few items.</summary>
    public bool HasStory => File?.HasStory ?? false;

    /// <summary>The name the introduction offers for the rival; null where the region's rival is its other hero.</summary>
    public string? Rival => File?.Rival;

    /// <summary>The three Pokémon of the professor's briefcase, in the order they are shown.</summary>
    public IReadOnlyList<string> Starters => (IReadOnlyList<string>?)File?.Starters ?? Array.Empty<string>();

    /// <summary>The names of the region's badges, in the order of its Trainer Card (plan 18 · W2).</summary>
    public IReadOnlyList<string> BadgeNames => (IReadOnlyList<string>?)File?.Badges ?? Array.Empty<string>();

    /// <summary>The badge each field move asks for here, by the move's name (<c>Cut</c>, <c>RockSmash</c>) to the badge's (plan 18 · W2).</summary>
    public IReadOnlyDictionary<string, string> Gates => (IReadOnlyDictionary<string, string>?)File?.Gates ?? new Dictionary<string, string>();

    /// <summary>The regional Pokédex the region plays by, a PokeAPI identifier (<c>extended-sinnoh</c>, <c>kanto</c>); null for a region with no file (plan 18 · W3).</summary>
    public string? Dex => File?.Dex is { Length: > 0 } d ? d : null;

    /// <summary>The rule that opens the National Pokédex here, read from the file (plan 18 · W3); the Hall of Fame alone for a region that names none.</summary>
    public NationalUnlock NationalUnlock => NationalUnlock.Parse(File?.NationalUnlock);

    public RegionHeroes Heroes => File?.Heroes ?? new RegionHeroes();

    public RegionProfessor? Professor => File?.Professor;

    /// <summary>The starter the rival takes when the player takes this one: the next in the briefcase, round (grass is answered with fire).</summary>
    public string RivalStarterFor(string playerStarter)
    {
        var list = Starters;
        if (list.Count == 0) return playerStarter;
        int i = -1;
        for (int k = 0; k < list.Count; k++) if (list[k] == playerStarter) i = k;
        return list[(Math.Max(i, 0) + 1) % list.Count];
    }
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
/// The regions in the order the journey takes them (plan 18 · W10, the user's answer of 2026-10-12): Sinnoh first,
/// where every new game begins, then the others in generation order, Kanto, Johto, Hoenn, Unova and so on to Paldea,
/// each reached from the one before by finishing its story. Nothing leads back from the last, and nothing is asked
/// about where to begin.
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
            Id = Sinnoh, Name = "Sinnoh", Generation = 4, File = RegionFiles.Load(Sinnoh),
            // In the player's room upstairs at home in Twinleaf Town, facing the television, as in Platinum (plan 02 · S4)
            Start = new MapSpot("PlayerHouse2F", 4, 6, Direction.Up),
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
                "SunyshoreGym", "SunyshoreGymRoom2", "SunyshoreGymRoom3",
                // The rooms of the first chapters (made by hand until plan 01 · M11 laid them out again, but for the houses and the lab)
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
                "VeilstoneGalacticWarehouse",
                // Solaceon Town's Pokémon Day Care, rebuilt to the original's plan (plan 06 · R15)
                "PokemonDayCare",
                // Plan 06 · R17b: the house of the Seal Case in Solaceon Town, the Sunyshore Market, Julia's house and
                // Dr. Footstep's on Route 213, rebuilt to the original's plans
                "SolaceonTownEastHouse", "SunyshoreMarket", "SunyshoreCityNortheastHouse", "FootstepHouse",
                // Plan 02 · S9: the cave of Celestic Town's ruins and the Fuego Ironworks' floor (matrices of the
                // imported world), and the elder's house in Celestic Town, rebuilt to the original's plan
                "CelesticTownCave", "FuegoIronworksBuilding", "CelesticTownNorthHouse",
                // Plan 01 · M9, part 2d: the League's rooms on the original's plans, from the door behind the guard to the Hall of Fame
                "PokemonLeagueElevatorToAaronRoom", "PokemonLeagueAaronRoom", "PokemonLeagueElevatorToBerthaRoom", "PokemonLeagueBerthaRoom",
                "PokemonLeagueElevatorToFlintRoom", "PokemonLeagueFlintRoom", "PokemonLeagueElevatorToLucianRoom", "PokemonLeagueLucianRoom",
                "PokemonLeagueElevatorToChampionRoom", "PokemonLeagueChampionRoom", "PokemonLeagueHallwayToHallOfFame", "PokemonLeagueHallOfFame",
                // Plan 02 · S10: Lake Valor drained, Valor Cavern and Lake Verity as Team Galactic holds it (matrices
                // of the imported world), and the Canalave Library's three floors, rebuilt to the original's plans
                "LakeValorDrained", "ValorCavern", "LakeVerityGalactic", "CanalaveLibrary1F", "CanalaveLibrary2F", "CanalaveLibrary3F",
                // Plan 01 · M10, part a: Victory Road's back rooms (matrices of the imported world), and the Pokémon Centers
                // and Marts of the Battle Zone's three towns
                "VictoryRoad1FRoom1", "VictoryRoad1FRoom2", "VictoryRoad1FRoom3",
                "FightAreaPokemonCenter", "FightAreaPokeMart", "SurvivalAreaPokemonCenter", "SurvivalAreaPokeMart", "ResortAreaPokemonCenter",
                // Plan 02 · S11, part a: Lake Acuity as Team Galactic leaves it (a matrix of the imported world)
                "LakeAcuityGalactic",
                // Plan 02 · S11, part b: Team Galactic's headquarters in Veilstone City, floor by floor on the original's plans
                "GalacticHQ1F", "GalacticHQ2F", "GalacticHQ3F", "GalacticHQ4F", "GalacticHQB1F", "GalacticHQB2F",
                "GalacticHQHall", "GalacticHQLaboratory", "GalacticHQControlRoom",
                // Plan 06 · R16a: the Underground (a matrix of the imported world, come to by the Explorer Kit alone)
                "Underground",
                // Plan 01 · M10, part b: Stark Mountain's rooms, Snowpoint Temple's floors, the islands' forests and the
                // Battle Frontier (matrices of the imported world), and the Ribbon Syndicate's ground floor
                "StarkMountainRoom1", "StarkMountainRoom2", "StarkMountainRoom3",
                "SnowpointTemple1F", "SnowpointTempleB1F", "SnowpointTempleB2F", "SnowpointTempleB3F", "SnowpointTempleB4F", "SnowpointTempleB5F",
                "FullmoonIslandForest", "NewmoonIslandForest", "BattleFrontier", "ResortAreaRibbonSyndicate1F",
                // Plan 02 · S12: Spear Pillar as the Distortion World tears it (a matrix of the imported world)
                "SpearPillarDistorted",
                // Plan 06 · R16b: the Mining Museum, the Game Corner with its prize exchange and the Coin Case's house, the
                // Villa, and Pal Park's lobby and grounds
                "OreburghMiningMuseum", "VeilstoneGameCorner", "VeilstonePrizeExchange", "VeilstoneSoutheastHouse", "ResortAreaVilla",
                "PalParkLobby", "PalPark",
                // Plan 01 · M11, part a: the rooms of the south-west and the centre, on the original's plans
                "TwinleafTownNortheastHouse", "TwinleafTownSouthwestHouse",
                "SandgemTownCounterpartHouse1F", "SandgemTownCounterpartHouse2F", "SandgemTownHouse",
                "JubilifeCityCondominiums1F", "JubilifeCityCondominiums2F", "JubilifeCitySouthHouse1F", "JubilifeCitySouthHouse2F",
                "OreburghCityNorthwestHouse1F", "OreburghCityNorthwestHouse2F", "OreburghNorthHouse2F", "OreburghCityWestHouse",
                "OreburghCityMiddleHouse", "OreburghCityEastHouse1F", "OreburghCityEastHouse2F", "OreburghCitySouthHouse",
                "FlowerShop", "FloaromaTownSoutheastHouse", "FloaromaTownMiddleHouse", "FloaromaMeadowHouse", "Route205House",
                "EternaCitySouthHouse", "EternaCityEastHouse", "EternaCityHerbShop", "EternaCondominiums2F", "EternaCondominiums3F",
                "Route208House", "HearthomeCityNorthwestHouse", "HearthomeCitySoutheastHouse1F", "HearthomeCitySoutheastHouse2F",
                "ForeignBuilding", "HearthomeCityNortheastHouse1F", "HearthomeCityNortheastHouse2F",
                "SolaceonTownNortheastHouse", "SolaceonTownPokemonNewsPress", "SolaceonTownNorthHouse",
                "OldChateau", "OldChateauDiningArea", "OldChateauWestRoom", "OldChateauEastRoom", "OldChateauCorridor",
                "OldChateauBackWestRoom", "OldChateauBackMiddleWestRoom", "OldChateauBackMiddleRoom", "OldChateauBackMiddleEastRoom",
                "OldChateauBackEastRoom",
                // Plan 02 · S13: Verity Cavern and Acuity Cavern (matrices of the imported world), and the Vista
                // Lighthouse's gallery and lift, rebuilt to the original's plans
                "VerityCavern", "AcuityCavern", "VistaLighthouse", "VistaLighthouseElevator",
                // Plan 06 · R18a: the Battle Tower's lobby, lift, corridors, battle rooms and Battle Salon
                "BattleTower", "BattleTowerElevator", "BattleTowerCorridor", "BattleTowerBattleRoom",
                "BattleTowerCorridorMulti", "BattleTowerMultiBattleRoom", "BattleTowerBattleSalon",
                // Plan 01 · M11, part b: the rooms of the east and the sea, on the original's plans
                "CafeCabin", "VeilstoneCitySouthwestHouse", "VeilstoneCityNorthwestHouse", "VeilstoneCityNortheastHouse",
                "SevenStarsRestaurant", "GrandLakeValorLakefrontEastHouse", "GrandLakeValorLakefrontWestHouse",
                "GrandLakeRoute213Lobby", "GrandLakeRoute213EastHouse", "GrandLakeRoute213NorthwestHouse", "GrandLakeRoute213NortheastHouse",
                "PastoriaCityMiddleHouse", "PastoriaCityNorthHouse", "PastoriaCityNortheastHouse", "PastoriaCitySouthwestHouse",
                "PastoriaCityObservatoryGate1F", "PastoriaCityObservatoryGate2F",
                "PokemonMansion", "PokemonMansionMaidsRoomWest", "PokemonMansionMaidsRoomMiddle", "PokemonMansionMaidsRoomEast",
                "PokemonMansionOffice", "Route221House",
                "CanalaveCitySoutheastHouse", "CanalaveCitySailorEldritchHouse", "CanalaveCityWestHouse", "IronIslandHouse",
                // Plan 06 · R18b: the Battle Factory's lobby, corridor and battle room
                "BattleFactory", "BattleFactoryCorridor", "BattleFactoryBattleRoom",
                // Plan 01 · M11, part c: the rooms of the north and of the Battle Zone, on the original's plans
                "CelesticTownNorthwestHouse", "CelesticTownNortheastHouse", "CelesticTownSouthwestHouse",
                "Route216House", "Route217WestHouse", "Route217NortheastHouse", "Route222EastHouse", "Route222WestHouse",
                "SunyshoreCityWestHouse", "SunyshoreCityNorthwestHouse", "SunyshoreCityEastHouse",
                "FightAreaMiddleHouse", "FightAreaSouthHouse", "Route225House", "SurvivalAreaSouthHouse",
                "Route226House", "Route227House", "Route228SouthHouse", "ResortAreaHouse",
                "ResortAreaRibbonSyndicate2F", "ResortAreaRibbonSyndicateElevator",
                // Plan 06 · R18c: the Battle Hall's lobby, passage and stage, and the Battle Castle's lobby, valet's room and throne room
                "BattleHall", "BattleHallCorridor", "BattleHallStage",
                "BattleCastle", "BattleCastleCorridor", "BattleCastleBattleRoom",
                // Plan 01 · M11, part d: the gate houses, on the original's plans
                "Route218GateToJubilifeCity", "Route218GateToCanalaveCity", "Route208GateToHearthomeCity", "Route215GateToVeilstoneCity",
                "Route213GateToPastoriaCity", "Route222GateToSunyshoreCity", "Route228GateToRoute226",
                "Route212GateToHearthomeCity", "Route225GateToFightArea", "Route214GateToVeilstoneCity",
                "Route206CyclingRoadNorthGate", "Route206CyclingRoadSouthGate",
                "HearthomeCityWestGateToAmitySquare", "HearthomeCityEastGateToAmitySquare", "BattleFrontierGateToFightArea",
                // Plan 01 · M11, part d: the tutors' and the two move men's houses
                "Route210GrandmaWilmaHouse", "Route212House", "PastoriaCityEastHouse", "CanalaveCityEastHouse",
                "SnowpointCityEastHouse", "SurvivalAreaNorthHouse", "Route228NorthHouse",
                // Plan 01 · M11, part d: Veilstone's Department Store and its lift
                "VeilstoneStore1F", "VeilstoneStore2F", "VeilstoneStore3F", "VeilstoneStore4F", "VeilstoneStore5F", "VeilstoneStoreB1F",
                "VeilstoneStoreElevator",
                // Plan 01 · M11, part d: the Global Terminal's three floors
                "GlobalTerminal1F", "GlobalTerminal2F", "GlobalTerminal3F",
                // Plan 01 · M11, part d: the viewing gallery over Pal Park's lobby
                "PalParkGallery",
                // Plan 01 · M11, part d: the floor above and the floor below each Pokémon Center, on the original's plans
                "CanalavePokemonCenter2F", "CanalavePokemonCenterB1F", "CelesticPokemonCenter2F",
                "CelesticPokemonCenterB1F", "EternaPokemonCenter2F", "EternaPokemonCenterB1F", "FightAreaPokemonCenter2F",
                "FightAreaPokemonCenterB1F", "FloaromaPokemonCenter2F", "FloaromaPokemonCenterB1F",
                "HearthomePokemonCenter2F", "HearthomePokemonCenterB1F", "JubilifePokemonCenter2F",
                "JubilifePokemonCenterB1F", "OreburghPokemonCenter2F", "OreburghPokemonCenterB1F",
                "PastoriaPokemonCenter2F", "PastoriaPokemonCenterB1F", "PokemonCenter2F", "PokemonCenterB1F",
                "PokemonLeagueNorthPokemonCenter2F", "PokemonLeagueNorthPokemonCenterB1F",
                "PokemonLeagueSouthPokemonCenter2F", "PokemonLeagueSouthPokemonCenterB1F", "ResortAreaPokemonCenter2F",
                "ResortAreaPokemonCenterB1F", "SnowpointPokemonCenter2F", "SnowpointPokemonCenterB1F",
                "SolaceonPokemonCenter2F", "SolaceonPokemonCenterB1F", "SunyshorePokemonCenter2F",
                "SunyshorePokemonCenterB1F", "SurvivalAreaPokemonCenter2F", "SurvivalAreaPokemonCenterB1F",
                "VeilstonePokemonCenter2F", "VeilstonePokemonCenterB1F",
                // Plan 06 · R18d: the Battle Arcade's lobby, corridor and game room
                "BattleArcade", "BattleArcadeCorridor", "BattleArcadeBattleRoom",
                // Plan 02 · S15, part c: the Battleground in the Survival Area, on the original's plan, and Spear Pillar
                // with Dialga's rift and with Palkia's
                "Battleground", "SpearPillarDialga", "SpearPillarPalkia",
                // Plan 01 · M11, the last rooms: the floors over the Pokétch Company's, on the original's plans
                "PoketchCompany2F", "PoketchCompany3F",
            }
        },
        new()
        {
            Id = Kanto, Name = "Kanto", Generation = 1, File = RegionFiles.Load(Kanto),
            // FireRed's Kanto, read by the GBA reader (plan 19 · K1): a new game begun there stands before the player's
            // house in Pallet Town, on the region's own overworld
            Start = new MapSpot("Kanto", 66, 268, Direction.Down),
            Maps = new[]
            {
                "Kanto",
                // Plan 19 · K1: Pallet Town's rooms and Viridian City's, each a matrix of the import built from its layout
                "PalletTown_PlayersHouse_1F", "PalletTown_PlayersHouse_2F", "PalletTown_RivalsHouse", "PalletTown_ProfessorOaksLab",
                "ViridianCity_House", "ViridianCity_Gym", "ViridianCity_School", "ViridianCity_Mart",
                "ViridianCity_PokemonCenter_1F", "ViridianCity_PokemonCenter_2F",
            }
        },
        new() { Id = Johto, Name = "Johto", Generation = 2 },
        new() { Id = Hoenn, Name = "Hoenn", Generation = 3 },
        new() { Id = Unova, Name = "Unova", Generation = 5 },
        new() { Id = Kalos, Name = "Kalos", Generation = 6 },
        new() { Id = Alola, Name = "Alola", Generation = 7 },
        new() { Id = Galar, Name = "Galar", Generation = 8 },
        new() { Id = Paldea, Name = "Paldea", Generation = 9 }
    };

    private static readonly RegionLink[] links =
    {
        // Sinnoh first: the way on from its Hall of Fame, to the stand-in Kanto until plan 19 builds it. It has no
        // attendant yet; plan 18 · W5 gives the crossing its vessel and its scene.
        new() { From = Sinnoh, To = Kanto, Transport = Transport.Undecided },
        // As in HeartGold and SoulSilver, but the other way round: the ship sails from Vermilion to Olivine.
        // A sailor on Pallet Town's shore stands in for Vermilion's harbour until plan 19 · K8 opens Vermilion City.
        new() { From = Kanto, To = Johto, Transport = Transport.Boat, DepartureMap = "Kanto" },
        new() { From = Johto, To = Hoenn, Transport = Transport.Undecided },
        new() { From = Hoenn, To = Unova, Transport = Transport.Undecided },
        new() { From = Unova, To = Kalos, Transport = Transport.Undecided },
        new() { From = Kalos, To = Alola, Transport = Transport.Undecided },
        new() { From = Alola, To = Galar, Transport = Transport.Undecided },
        new() { From = Galar, To = Paldea, Transport = Transport.Undecided }
    };

    /// <summary>Every region, in the order of the journey: Sinnoh, then generation order from Kanto.</summary>
    public static IReadOnlyList<Region> All => regions;

    public static IReadOnlyList<RegionLink> Links => links;

    /// <summary>The region every new game starts in: the chain's first, Sinnoh. Only a switch or a tool names another (<c>GameEngine.NewGameRegion</c>).</summary>
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
