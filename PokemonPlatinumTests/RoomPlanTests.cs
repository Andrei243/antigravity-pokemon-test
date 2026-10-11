using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Rooms rebuilt to the original's floor plans (the houses of plan 06 · R12's trades, plan 02 · S6's Valley Windworks):
/// the original's people, ways out, triggers and things read stand on the tiles its events give them, so a scene can be
/// written from the original's coordinates as they are.
/// </summary>
[Collection("MapDatabase")]
public class RoomPlanTests
{
    private static Map Room(string name) =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>Each room, where its floor begins, and the original's exit mat (where whoever comes in stands).</summary>
    public static IEnumerable<object[]> Plans => new[]
    {
        new object[] { "OreburghNorthHouse1F", 3, 3, 11, 12 },
        new object[] { "EternaCondominiums1F", 3, 3, 11, 12 },
        new object[] { "SnowpointWestHouse", 1, 3, 4, 8 },
        new object[] { "ValleyWindworksBuilding", 1, 3, 12, 16 },
        // Eterna City's rooms of plan 02 · S6, part 2
        new object[] { "EternaCycleShop", 1, 3, 7, 11 },
        new object[] { "EternaUndergroundManHouse", 1, 3, 4, 8 },
        new object[] { "TeamGalacticEternaBuilding1F", 1, 6, 11, 15 },
        // Hearthome City's of plan 02 · S7, part 1
        new object[] { "ContestHallLobby", 1, 3, 16, 13 },
        // Route 209's Lost Tower, plan 02 · S7, part 2
        new object[] { "LostTower1F", 1, 3, 7, 14 },
        // Hearthome City's Poffin House and Pokémon Fan Club, plan 06 · R14c
        new object[] { "PoffinHouse", 1, 3, 8, 13 },
        new object[] { "HearthomeFanClub", 1, 3, 6, 17 },
        // Veilstone City's Galactic warehouse, plan 02 · S8
        new object[] { "VeilstoneGalacticWarehouse", 1, 3, 8, 11 },
        // Solaceon Town's Pokémon Day Care, plan 06 · R15
        new object[] { "PokemonDayCare", 1, 3, 9, 11 },
        // The elder's house in Celestic Town, plan 02 · S9
        new object[] { "CelesticTownNorthHouse", 1, 4, 5, 8 },
        // The League's Pokémon Center, with the door to the Elite Four, plan 01 · M9, part 2d: its nurse and clerks stand in row 1
        new object[] { "PokemonLeagueNorthPokemonCenter", 1, 1, 11, 11 },
        // The Canalave Library's first floor, plan 02 · S10
        new object[] { "CanalaveLibrary1F", 1, 4, 11, 11 },
        // The Seal Case's house, the Sunyshore Market, Julia's house and Dr. Footstep's, plan 06 · R17b
        new object[] { "SolaceonTownEastHouse", 1, 3, 4, 8 },
        new object[] { "SunyshoreMarket", 1, 3, 5, 12 },
        new object[] { "SunyshoreCityNortheastHouse", 1, 3, 4, 8 },
        new object[] { "FootstepHouse", 1, 3, 4, 8 },
        // The Ribbon Syndicate's ground floor in the Resort Area, plan 01 · M10, part b
        new object[] { "ResortAreaRibbonSyndicate1F", 1, 3, 15, 15 },
        // The Game Corner, its prize exchange, the Coin Case's house and the Villa, plan 06 · R16b (the Mining Museum's
        // door is in the west half of its front wall, and Pal Park's lobby is a flat stand-in: PastimeTests)
        new object[] { "VeilstoneGameCorner", 1, 3, 5, 11 },
        new object[] { "VeilstonePrizeExchange", 1, 3, 4, 8 },
        new object[] { "VeilstoneSoutheastHouse", 1, 3, 4, 8 },
        new object[] { "ResortAreaVilla", 1, 3, 11, 11 },
        new object[] { "PalParkLobby", 1, 7, 7, 19 },
        // The rooms of the south-west and the centre, plan 01 · M11, part a
        new object[] { "TwinleafTownNortheastHouse", 1, 3, 4, 8 },
        new object[] { "TwinleafTownSouthwestHouse", 1, 3, 4, 8 },
        new object[] { "SandgemTownCounterpartHouse1F", 1, 3, 6, 10 },
        new object[] { "SandgemTownHouse", 1, 3, 4, 8 },
        new object[] { "JubilifeCityCondominiums1F", 3, 3, 11, 12 },
        new object[] { "JubilifeCitySouthHouse1F", 3, 3, 11, 12 },
        new object[] { "OreburghCityNorthwestHouse1F", 3, 3, 11, 12 },
        new object[] { "OreburghCityWestHouse", 1, 3, 4, 8 },
        new object[] { "OreburghCityMiddleHouse", 1, 3, 4, 8 },
        new object[] { "OreburghCityEastHouse1F", 3, 3, 11, 12 },
        new object[] { "OreburghCitySouthHouse", 1, 3, 4, 8 },
        new object[] { "FlowerShop", 1, 3, 8, 11 },
        new object[] { "FloaromaTownSoutheastHouse", 1, 3, 4, 8 },
        new object[] { "FloaromaTownMiddleHouse", 1, 3, 4, 8 },
        new object[] { "Route205House", 1, 3, 4, 8 },
        new object[] { "EternaCitySouthHouse", 1, 3, 4, 8 },
        new object[] { "EternaCityEastHouse", 1, 3, 4, 8 },
        new object[] { "EternaCityHerbShop", 1, 3, 4, 8 },
        new object[] { "Route208House", 1, 3, 4, 8 },
        new object[] { "HearthomeCityNorthwestHouse", 1, 3, 5, 8 },
        new object[] { "HearthomeCitySoutheastHouse1F", 1, 3, 10, 12 },
        new object[] { "ForeignBuilding", 1, 8, 8, 20 },
        new object[] { "HearthomeCityNortheastHouse1F", 1, 3, 10, 12 },
        new object[] { "SolaceonTownNortheastHouse", 1, 3, 4, 8 },
        new object[] { "SolaceonTownPokemonNewsPress", 1, 3, 4, 8 },
        new object[] { "SolaceonTownNorthHouse", 1, 3, 4, 8 },
        // The Vista Lighthouse's lift, plan 02 · S13: its door leads out to Sunyshore City
        new object[] { "VistaLighthouseElevator", 1, 3, 3, 6 },
        // The rooms of the east and the sea, plan 01 · M11, part b
        new object[] { "CafeCabin", 1, 3, 5, 8 },
        new object[] { "VeilstoneCitySouthwestHouse", 1, 3, 4, 8 },
        new object[] { "VeilstoneCityNorthwestHouse", 1, 3, 4, 8 },
        new object[] { "VeilstoneCityNortheastHouse", 1, 3, 4, 8 },
        new object[] { "SevenStarsRestaurant", 1, 3, 9, 12 },
        new object[] { "GrandLakeValorLakefrontEastHouse", 1, 3, 6, 8 },
        new object[] { "GrandLakeValorLakefrontWestHouse", 1, 3, 6, 8 },
        new object[] { "GrandLakeRoute213EastHouse", 1, 3, 6, 8 },
        new object[] { "GrandLakeRoute213NorthwestHouse", 1, 3, 6, 8 },
        new object[] { "GrandLakeRoute213NortheastHouse", 1, 3, 6, 8 },
        new object[] { "PastoriaCityMiddleHouse", 1, 3, 4, 8 },
        new object[] { "PastoriaCityNorthHouse", 1, 3, 4, 8 },
        new object[] { "PastoriaCityNortheastHouse", 1, 3, 4, 8 },
        new object[] { "PastoriaCitySouthwestHouse", 1, 3, 4, 8 },
        new object[] { "PastoriaCityObservatoryGate1F", 1, 3, 5, 11 },
        new object[] { "PokemonMansion", 1, 3, 33, 16 },
        new object[] { "Route221House", 1, 3, 4, 8 },
        new object[] { "CanalaveCitySoutheastHouse", 1, 3, 4, 8 },
        new object[] { "CanalaveCitySailorEldritchHouse", 1, 3, 4, 8 },
        new object[] { "CanalaveCityWestHouse", 1, 3, 4, 8 },
        new object[] { "IronIslandHouse", 1, 3, 4, 8 },
        // Plan 01 · M11, part c: the rooms of the north and of the Battle Zone
        new object[] { "CelesticTownNorthwestHouse", 1, 3, 4, 8 },
        new object[] { "CelesticTownNortheastHouse", 1, 3, 4, 8 },
        new object[] { "CelesticTownSouthwestHouse", 1, 3, 4, 8 },
        new object[] { "Route216House", 1, 3, 4, 8 },
        new object[] { "Route217WestHouse", 1, 3, 4, 8 },
        new object[] { "Route217NortheastHouse", 1, 3, 4, 8 },
        new object[] { "Route222EastHouse", 1, 3, 4, 8 },
        new object[] { "Route222WestHouse", 1, 3, 4, 8 },
        new object[] { "SunyshoreCityWestHouse", 1, 3, 4, 8 },
        new object[] { "SunyshoreCityNorthwestHouse", 1, 3, 4, 8 },
        new object[] { "SunyshoreCityEastHouse", 1, 3, 4, 8 },
        new object[] { "FightAreaMiddleHouse", 1, 3, 4, 8 },
        new object[] { "FightAreaSouthHouse", 1, 3, 4, 8 },
        new object[] { "Route225House", 1, 3, 4, 8 },
        new object[] { "SurvivalAreaSouthHouse", 1, 3, 4, 8 },
        new object[] { "Route226House", 1, 3, 4, 8 },
        new object[] { "Route227House", 1, 3, 4, 8 },
        new object[] { "Route228SouthHouse", 1, 3, 4, 8 },
        new object[] { "ResortAreaHouse", 1, 3, 4, 8 },
        // Plan 01 · M11, part d: the tutors' and the two move men's houses
        new object[] { "Route210GrandmaWilmaHouse", 1, 3, 4, 8 },
        new object[] { "Route212House", 1, 3, 4, 8 },
        new object[] { "PastoriaCityEastHouse", 1, 3, 4, 8 },
        new object[] { "CanalaveCityEastHouse", 1, 3, 4, 8 },
        new object[] { "SnowpointCityEastHouse", 1, 3, 4, 8 },
        new object[] { "SurvivalAreaNorthHouse", 1, 3, 4, 8 },
        new object[] { "Route228NorthHouse", 1, 3, 4, 8 },
        // Plan 01 · M11, part d: Veilstone's Department Store, whose ground floor has the door
        new object[] { "VeilstoneStore1F", 1, 3, 10, 12 },
        // Plan 01 · M11, part d: the Global Terminal, whose ground floor has the door
        new object[] { "GlobalTerminal1F", 1, 3, 13, 18 },
        // Plan 02 · S15, part c: the Battleground in the Survival Area
        new object[] { "Battleground", 1, 3, 7, 11 },
        // The Pokémon Centers' ground floors on the original's one plan (plan 01 · M11, the last rooms)
        new object[] { "PokemonCenter", 1, 3, 8, 12 },
        new object[] { "JubilifePokemonCenter", 1, 3, 8, 12 },
        new object[] { "OreburghPokemonCenter", 1, 3, 8, 12 },
        new object[] { "FloaromaPokemonCenter", 1, 3, 8, 12 },
        new object[] { "EternaPokemonCenter", 1, 3, 8, 12 },
        new object[] { "HearthomePokemonCenter", 1, 3, 8, 12 },
        new object[] { "SolaceonPokemonCenter", 1, 3, 8, 12 },
        new object[] { "VeilstonePokemonCenter", 1, 3, 8, 12 },
        new object[] { "PastoriaPokemonCenter", 1, 3, 8, 12 },
        new object[] { "CanalavePokemonCenter", 1, 3, 8, 12 },
        new object[] { "CelesticPokemonCenter", 1, 3, 8, 12 },
        new object[] { "SnowpointPokemonCenter", 1, 3, 8, 12 },
        new object[] { "SunyshorePokemonCenter", 1, 3, 8, 12 },
        new object[] { "PokemonLeagueSouthPokemonCenter", 1, 3, 8, 12 },
        new object[] { "FightAreaPokemonCenter", 1, 3, 8, 12 },
        new object[] { "SurvivalAreaPokemonCenter", 1, 3, 8, 12 },
        new object[] { "ResortAreaPokemonCenter", 1, 3, 8, 12 },
        // The Poké Marts on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "PokeMart", 1, 3, 3, 11 },
        new object[] { "JubilifePokeMart", 1, 3, 3, 11 },
        new object[] { "OreburghPokeMart", 1, 3, 3, 11 },
        new object[] { "FloaromaPokeMart", 1, 3, 3, 11 },
        new object[] { "EternaPokeMart", 1, 3, 3, 11 },
        new object[] { "HearthomePokeMart", 1, 3, 3, 11 },
        new object[] { "SolaceonPokeMart", 1, 3, 3, 11 },
        new object[] { "PastoriaPokeMart", 1, 3, 3, 11 },
        new object[] { "CanalavePokeMart", 1, 3, 3, 11 },
        new object[] { "SnowpointPokeMart", 1, 3, 3, 11 },
        new object[] { "SunyshorePokeMart", 1, 3, 3, 11 },
        new object[] { "FightAreaPokeMart", 1, 3, 3, 11 },
        new object[] { "SurvivalAreaPokeMart", 1, 3, 3, 11 },
        // The Trainers' School on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "TrainersSchool", 1, 3, 7, 11 },
        // Jubilife TV's lobby on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "JubilifeTV1F", 1, 4, 9, 18 },
        // The rival's house on the original's plans (plan 01 · M11, the last rooms)
        new object[] { "RivalHouse", 1, 3, 6, 10 },
    };

    /// <summary>Rooms on the original's plans whose door isn't in the last row of the room (plan 06 · R16b): the Oreburgh
    /// Mining Museum is L-shaped, its door in the west half of its front wall (<c>PastimeTests</c>).</summary>
    internal static readonly string[] OddlyShaped = { "OreburghMiningMuseum" };

    /// <summary>The Battle Tower's rooms (plan 06 · R18a): its lobby leads out onto the Battle Frontier's grounds by three
    /// tiles of mat, and the lift, the corridors, the battle rooms and the Battle Salon behind it are come to and left
    /// by the challenge's own scripts (<c>FrontierTests</c>).</summary>
    internal static readonly string[] BattleTower =
    {
        "BattleTower", "BattleTowerElevator", "BattleTowerCorridor", "BattleTowerBattleRoom",
        "BattleTowerCorridorMulti", "BattleTowerMultiBattleRoom", "BattleTowerBattleSalon",
    };

    /// <summary>The Battle Factory's rooms (plan 06 · R18b): its lobby is come into from the west, at the head of its
    /// stairs, and the corridor and the battle room behind its counter are laid out from the original's scene, which is a
    /// picture and no map (<c>BattleFactoryTests</c>).</summary>
    internal static readonly string[] BattleFactory = { "BattleFactory", "BattleFactoryCorridor", "BattleFactoryBattleRoom" };

    /// <summary>The Battle Hall's and the Battle Castle's rooms (plan 06 · R18c): each lobby is come into from the east,
    /// and the rooms behind its counter are laid out from the original's scenes, which are pictures and no maps
    /// (<c>BattleHallTests</c>, <c>BattleCastleTests</c>).</summary>
    internal static readonly string[] BattleHallAndCastle =
    {
        "BattleHall", "BattleHallCorridor", "BattleHallStage", "BattleCastle", "BattleCastleCorridor", "BattleCastleBattleRoom",
    };

    /// <summary>The Battle Arcade's rooms (plan 06 · R18d): the lobby is come into from the west, and the corridor and
    /// the game room behind its attendants are laid out from the original's scenes (<c>BattleArcadeTests</c>).</summary>
    internal static readonly string[] BattleArcade = { "BattleArcade", "BattleArcadeCorridor", "BattleArcadeBattleRoom" };

    /// <summary>The Pokétch Company (plan 01 · M11, the last rooms): its ground floor has two doors onto Jubilife City,
    /// as the original's has, and the floors over it only their stairs (<c>RebuiltRoomTests</c>).</summary>
    internal static readonly string[] PoketchCompany = { "PoketchCompany", "PoketchCompany2F", "PoketchCompany3F" };

    /// <summary>The rival's room over his house (plan 01 · M11, the last rooms): no door, only the stairs, and the scene that
    /// plays as the player first comes up them (<c>RebuiltRoomTests</c>, <c>OpeningTests</c>).</summary>
    internal static readonly string[] RivalsRoom = { "RivalHouse2F" };

    /// <summary>The Canalave Library's upper floors (plan 02 · S10): no door, only the stairs.</summary>
    internal static readonly string[] LibraryUpperFloors = { "CanalaveLibrary2F", "CanalaveLibrary3F" };

    /// <summary>Gate houses rebuilt to the original's plans: a door in each side wall, both to the map of Sinnoh.</summary>
    internal static readonly string[] Gates =
    {
        "Route209GateToHearthomeCity",
        // Plan 01 · M11, part d: the seven others on the same plan (m_gatepl02)
        "Route218GateToJubilifeCity", "Route218GateToCanalaveCity", "Route208GateToHearthomeCity", "Route215GateToVeilstoneCity",
        "Route213GateToPastoriaCity", "Route222GateToSunyshoreCity", "Route228GateToRoute226",
    };

    /// <summary>
    /// Gate houses on a road that runs north and south (plan 01 · M11, part d): a way through the back wall and a door
    /// in the front wall, each to the place on its side (<c>FinalInteriorTests</c>). The Cycling Road's two have doors
    /// three tiles wide, Hearthome's two lead to Amity Square, and the Battle Frontier's is a hall of two parts.
    /// </summary>
    internal static readonly string[] NorthSouthGates =
    {
        "Route212GateToHearthomeCity", "Route225GateToFightArea", "Route214GateToVeilstoneCity",
        "Route206CyclingRoadNorthGate", "Route206CyclingRoadSouthGate",
        "HearthomeCityWestGateToAmitySquare", "HearthomeCityEastGateToAmitySquare", "BattleFrontierGateToFightArea",
    };

    /// <summary>The Team Galactic Eterna Building's upper floors: no door, only the stairs, and each cut in two (one
    /// staircase of each pair a trap that leads back down into the other part).</summary>
    internal static readonly string[] UpperFloors = { "TeamGalacticEternaBuilding2F", "TeamGalacticEternaBuilding3F", "TeamGalacticEternaBuilding4F" };

    /// <summary>The Lost Tower's floors from the ground up: the stairs up at the west end of the back wall, the stairs
    /// down at the east end (the original's <c>StairsEast</c> and <c>StairsWest</c>).</summary>
    internal static readonly string[] LostTower = { "LostTower1F", "LostTower2F", "LostTower3F", "LostTower4F", "LostTower5F" };

    /// <summary>Team Galactic's headquarters in Veilstone City (plan 02 · S11, part b): nine rooms on the original's plans,
    /// joined by stairs and warp panels; only the ground floor has doors, three of them (<c>GalacticHeadquartersTests</c>).</summary>
    internal static readonly string[] GalacticHeadquarters =
    {
        "GalacticHQ1F", "GalacticHQ2F", "GalacticHQ3F", "GalacticHQ4F", "GalacticHQB1F", "GalacticHQB2F",
        "GalacticHQHall", "GalacticHQLaboratory", "GalacticHQControlRoom",
    };

    /// <summary>
    /// The rooms of plan 01 · M11, part a, that have no door onto the map of Sinnoh: the floors over a ground floor, reached
    /// by its stairs or its lift, the house in Floaroma Meadow, and the Old Chateau, whose hall opens on Eterna Forest
    /// and whose other rooms open on the hall (<c>InteriorTests</c>). Part b's with them: the observatory's upper floor,
    /// the Pokémon Mansion's rooms off its hall, and the Hotel Grand Lake's lobby, which has a door onto the map of
    /// Sinnoh in its front wall and another in its back wall (<c>EastInteriorTests</c>).
    /// </summary>
    internal static readonly string[] InnerRooms =
    {
        "SandgemTownCounterpartHouse2F", "JubilifeCityCondominiums2F", "JubilifeCitySouthHouse2F", "OreburghCityNorthwestHouse2F",
        "OreburghNorthHouse2F", "OreburghCityEastHouse2F", "FloaromaMeadowHouse", "EternaCondominiums2F",
        "EternaCondominiums3F", "HearthomeCitySoutheastHouse2F", "HearthomeCityNortheastHouse2F", "OldChateau",
        "OldChateauDiningArea", "OldChateauWestRoom", "OldChateauEastRoom", "OldChateauCorridor",
        "OldChateauBackWestRoom", "OldChateauBackMiddleWestRoom", "OldChateauBackMiddleRoom", "OldChateauBackMiddleEastRoom",
        "OldChateauBackEastRoom",
        "GrandLakeRoute213Lobby", "PastoriaCityObservatoryGate2F", "PokemonMansionMaidsRoomWest", "PokemonMansionMaidsRoomMiddle",
        "PokemonMansionMaidsRoomEast", "PokemonMansionOffice",
    };
    /// <summary>The Ribbon Syndicate's upper floor and the car of its lift (plan 01 · M11, part c): come to by the lift's
    /// doors in the ground floor's back wall, never from the map of Sinnoh (<c>NorthInteriorTests</c>).</summary>
    internal static readonly string[] SyndicateLift = { "ResortAreaRibbonSyndicate2F", "ResortAreaRibbonSyndicateElevator" };

    /// <summary>
    /// The floors of Veilstone's Department Store but the ground floor, and the car of its lift (plan 01 · M11, part d):
    /// come to by the escalators in the middle of each floor and the lift's doors in its back wall, never from the map
    /// of Sinnoh (<c>FinalInteriorTests</c>).
    /// </summary>
    internal static readonly string[] StoreFloors =
    {
        "VeilstoneStore2F", "VeilstoneStore3F", "VeilstoneStore4F", "VeilstoneStore5F", "VeilstoneStoreB1F", "VeilstoneStoreElevator",
    };
    /// <summary>
    /// The floor above and the floor below each Pokémon Center (plan 01 · M11, part d): the original's two plans, one
    /// map of each to a Center as the original has a header of each to a town, come to by the escalators of the
    /// Center's ground floor (<c>FinalInteriorTests</c>).
    /// </summary>
    internal static readonly string[] CenterFloors =
    {
        "CanalavePokemonCenter2F", "CanalavePokemonCenterB1F", "CelesticPokemonCenter2F", "CelesticPokemonCenterB1F",
        "EternaPokemonCenter2F", "EternaPokemonCenterB1F", "FightAreaPokemonCenter2F", "FightAreaPokemonCenterB1F",
        "FloaromaPokemonCenter2F", "FloaromaPokemonCenterB1F", "HearthomePokemonCenter2F", "HearthomePokemonCenterB1F",
        "JubilifePokemonCenter2F", "JubilifePokemonCenterB1F", "OreburghPokemonCenter2F", "OreburghPokemonCenterB1F",
        "PastoriaPokemonCenter2F", "PastoriaPokemonCenterB1F", "PokemonCenter2F", "PokemonCenterB1F",
        "PokemonLeagueNorthPokemonCenter2F", "PokemonLeagueNorthPokemonCenterB1F", "PokemonLeagueSouthPokemonCenter2F",
        "PokemonLeagueSouthPokemonCenterB1F", "ResortAreaPokemonCenter2F", "ResortAreaPokemonCenterB1F",
        "SnowpointPokemonCenter2F", "SnowpointPokemonCenterB1F", "SolaceonPokemonCenter2F", "SolaceonPokemonCenterB1F",
        "SunyshorePokemonCenter2F", "SunyshorePokemonCenterB1F", "SurvivalAreaPokemonCenter2F",
        "SurvivalAreaPokemonCenterB1F", "VeilstonePokemonCenter2F", "VeilstonePokemonCenterB1F",
    };
    /// <summary>The Global Terminal's upper floors (plan 01 · M11, part d): come to by the ground floor's warp panels (<c>FinalInteriorTests</c>).</summary>
    internal static readonly string[] TerminalFloors = { "GlobalTerminal2F", "GlobalTerminal3F" };
    /// <summary>The viewing gallery over Pal Park's lobby (plan 01 · M11, part d): the original's upper level of the lobby's
    /// own map, a map of its own here, come to by the lobby's escalator (<c>FinalInteriorTests</c>).</summary>
    internal static readonly string[] ParkGallery = { "PalParkGallery" };
    /// <summary>The gallery at the top of the Vista Lighthouse (plan 02 · S13): no door, only the lift's car in the
    /// shaft at its middle (<c>SunyshoreTests</c>).</summary>
    internal static readonly string[] LighthouseGallery = { "VistaLighthouse" };

    // The original's people (res/field/events/events_<map>.json of the decompilation): local id, tile, facing
    public static IEnumerable<object[]> People => new[]
    {
        new object[] { "OreburghNorthHouse1F", "expert_f", 15, 8, Direction.Left },
        new object[] { "OreburghNorthHouse1F", "school_kid_f", 11, 7, Direction.Down },
        new object[] { "OreburghNorthHouse1F", "pokemon_breeder_m", 8, 10, Direction.Left },
        new object[] { "EternaCondominiums1F", "ninja_boy", 5, 5, Direction.Down },
        new object[] { "EternaCondominiums1F", "pokefan_m", 15, 8, Direction.Down },
        new object[] { "EternaCondominiums1F", "expert_m", 8, 10, Direction.Left },
        new object[] { "SnowpointWestHouse", "ace_trainer_m", 7, 6, Direction.Left },
        new object[] { "SnowpointWestHouse", "mindy", 5, 5, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_1", 12, 14, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "mars", 20, 7, Direction.Left },
        new object[] { "ValleyWindworksBuilding", "scientist_papa", 21, 3, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_2", 3, 8, Direction.Left },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_3", 12, 3, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "little_girl", 10, 8, Direction.Right },
        new object[] { "ValleyWindworksBuilding", "grunt_m", 18, 8, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "charon", 21, 5, Direction.Left },
        new object[] { "EternaCycleShop", "pokefan_m", 3, 5, Direction.Down },
        new object[] { "EternaCycleShop", "youngster", 5, 10, Direction.Left },
        new object[] { "EternaUndergroundManHouse", "underground_man", 6, 5, Direction.Down },
        new object[] { "EternaUndergroundManHouse", "scientist_m", 3, 5, Direction.Down },
        new object[] { "EternaUndergroundManHouse", "youngster", 8, 7, Direction.Left },
        new object[] { "EternaUndergroundManHouse", "bug_catcher", 9, 6, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_1", 3, 11, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_2", 10, 11, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding1F", "galactic_grunt_1", 16, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding1F", "galactic_grunt_2", 12, 8, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_looker", 14, 15, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding1F", "looker", 14, 15, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "grunt_m", 7, 6, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "grunt_f", 19, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding2F", "galactic_grunt_1", 3, 6, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "galactic_grunt_2", 13, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding3F", "grunt_m", 12, 5, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding3F", "galactic_grunt", 8, 7, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding3F", "scientist_travon", 18, 5, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding4F", "jupiter", 14, 6, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding4F", "pokefan_m", 14, 9, Direction.Up },
        new object[] { "ContestHallLobby", "receptionist_official", 16, 4, Direction.Down },
        new object[] { "ContestHallLobby", "mom", 16, 10, Direction.Right },
        new object[] { "ContestHallLobby", "keira", 17, 10, Direction.Left },
        new object[] { "ContestHallLobby", "fantina", 22, 9, Direction.Left },
        new object[] { "ContestHallLobby", "rich_boy", 16, 6, Direction.Up },
        new object[] { "Route209GateToHearthomeCity", "battle_girl", 9, 4, Direction.Down },
        new object[] { "Route209GateToHearthomeCity", "rival", 8, 7, Direction.Left },
        new object[] { "LostTower1F", "pokemon_breeder_f_1", 8, 9, Direction.Down },
        new object[] { "LostTower1F", "pokemon_breeder_f_2", 2, 4, Direction.Right },
        new object[] { "LostTower2F", "youngster_oliver", 8, 10, Direction.Up },
        new object[] { "LostTower5F", "old_woman_1", 7, 9, Direction.Down },
        new object[] { "LostTower5F", "old_woman_2", 8, 9, Direction.Down },
        new object[] { "PoffinHouse", "idol", 6, 5, Direction.Down },
        new object[] { "PoffinHouse", "old_woman", 11, 8, Direction.Down },
        new object[] { "PoffinHouse", "old_man", 11, 9, Direction.Up },
        new object[] { "PoffinHouse", "pokefan_f", 5, 10, Direction.Down },
        new object[] { "PoffinHouse", "school_kid_f", 3, 8, Direction.Right },
        new object[] { "HearthomeFanClub", "gentleman", 6, 7, Direction.Down },
        new object[] { "HearthomeFanClub", "beauty", 2, 4, Direction.Down },
        new object[] { "HearthomeFanClub", "psyduck", 9, 5, Direction.Down },
        new object[] { "HearthomeFanClub", "buneary", 7, 13, Direction.Down },
        new object[] { "HearthomeFanClub", "clefairy", 3, 11, Direction.Down },
        new object[] { "VeilstoneGalacticWarehouse", "looker", 12, 8, Direction.Down },
        new object[] { "PokemonDayCare", "day_care_lady", 9, 4, Direction.Down },
        new object[] { "PokemonDayCare", "gym_guide", 4, 9, Direction.Right },
        new object[] { "CelesticTownNorthHouse", "expert_m", 8, 7, Direction.Down },
        new object[] { "CelesticTownNorthHouse", "elder", 2, 6, Direction.Down },
        new object[] { "CelesticTownNorthHouse", "lass", 3, 4, Direction.Down },
        new object[] { "CanalaveLibrary1F", "receptionist", 14, 9, Direction.Left },
        new object[] { "CanalaveLibrary1F", "lass", 2, 5, Direction.Up },
        new object[] { "CanalaveLibrary2F", "school_kid_m", 11, 10, Direction.Down },
        new object[] { "CanalaveLibrary3F", "scientist_f", 15, 5, Direction.Down },
        new object[] { "CanalaveLibrary3F", "youngster", 3, 7, Direction.Down },
        new object[] { "CanalaveLibrary3F", "prof_rowan", 8, 10, Direction.Up },
        new object[] { "CanalaveLibrary3F", "counterpart", 7, 10, Direction.Up },
        new object[] { "CanalaveLibrary3F", "rival", 2, 3, Direction.Right },
        new object[] { "SolaceonTownEastHouse", "ninja_boy", 5, 5, Direction.Left },
        new object[] { "SolaceonTownEastHouse", "pokefan_f", 2, 5, Direction.Right },
        new object[] { "SunyshoreMarket", "pokemon_breeder_f", 2, 9, Direction.Right },
        new object[] { "SunyshoreMarket", "gym_guide", 1, 5, Direction.Right },
        new object[] { "SunyshoreMarket", "sailor", 8, 9, Direction.Down },
        new object[] { "SunyshoreCityNortheastHouse", "julia", 3, 5, Direction.Down },
        new object[] { "FootstepHouse", "dr_footstep", 5, 6, Direction.Down },
        // Team Galactic's headquarters and the warehouse's door, plan 02 · S11, part b
        new object[] { "VeilstoneGalacticWarehouse", "galactic_hq_door_west", 8, 7, Direction.Down },
        new object[] { "VeilstoneGalacticWarehouse", "galactic_hq_door_east", 9, 7, Direction.Down },
        new object[] { "GalacticHQ1F", "grunt_f_1", 29, 15, Direction.Down },
        new object[] { "GalacticHQ1F", "scientist_m_1", 11, 18, Direction.Down },
        new object[] { "GalacticHQ1F", "galactic_grunt", 41, 7, Direction.Down },
        new object[] { "GalacticHQ1F", "scientist_fredrick", 17, 6, Direction.Right },
        new object[] { "GalacticHQ1F", "galactic_hq_door_west", 22, 18, Direction.Down },
        new object[] { "GalacticHQ1F", "galactic_hq_door_east", 23, 18, Direction.Down },
        new object[] { "GalacticHQ1F", "grunt_f_2", 2, 19, Direction.Down },
        new object[] { "GalacticHQ1F", "grunt_m", 19, 21, Direction.Left },
        new object[] { "GalacticHQ1F", "saturn", 33, 19, Direction.Left },
        new object[] { "GalacticHQ2F", "galactic_grunt_1", 10, 6, Direction.Up },
        new object[] { "GalacticHQ2F", "galactic_grunt_2", 21, 16, Direction.Up },
        new object[] { "GalacticHQ2F", "galactic_grunt_3", 26, 16, Direction.Left },
        new object[] { "GalacticHQ2F", "scientist_darrius", 32, 6, Direction.Up },
        new object[] { "GalacticHQ3F", "grunt_m", 15, 6, Direction.Down },
        new object[] { "GalacticHQ3F", "galactic_grunt_1", 4, 6, Direction.Up },
        new object[] { "GalacticHQ3F", "galactic_grunt_2", 9, 20, Direction.Right },
        new object[] { "GalacticHQ3F", "galactic_grunt_3", 42, 7, Direction.Up },
        new object[] { "GalacticHQ3F", "galactic_grunt_4", 42, 3, Direction.Down },
        new object[] { "GalacticHQ3F", "galactic_hq_door_west", 27, 19, Direction.Down },
        new object[] { "GalacticHQ3F", "galactic_hq_door_east", 28, 19, Direction.Down },
        new object[] { "GalacticHQ4F", "cyrus", 8, 8, Direction.Up },
        new object[] { "GalacticHQ4F", "galactic_hq_door_west", 8, 14, Direction.Down },
        new object[] { "GalacticHQ4F", "galactic_hq_door_east", 9, 14, Direction.Down },
        new object[] { "GalacticHQ4F", "bollard_west", 18, 14, Direction.Down },
        new object[] { "GalacticHQ4F", "bollard_east", 19, 14, Direction.Down },
        new object[] { "GalacticHQB1F", "grunt_m", 5, 7, Direction.Down },
        new object[] { "GalacticHQB2F", "grunt_m", 26, 5, Direction.Down },
        new object[] { "GalacticHQB2F", "scientist_m", 8, 10, Direction.Down },
        new object[] { "GalacticHQB2F", "galactic_grunt_1", 40, 15, Direction.Left },
        new object[] { "GalacticHQB2F", "galactic_grunt_2", 71, 10, Direction.Down },
        new object[] { "GalacticHQB2F", "galactic_hq_door_west", 14, 8, Direction.Down },
        new object[] { "GalacticHQB2F", "galactic_hq_door_east", 15, 8, Direction.Down },
        new object[] { "GalacticHQHall", "looker", 19, 12, Direction.Left },
        new object[] { "GalacticHQHall", "cyrus", 17, 3, Direction.Down },
        new object[] { "GalacticHQLaboratory", "scientist_m", 8, 13, Direction.Left },
        new object[] { "GalacticHQLaboratory", "scientist_f", 36, 10, Direction.Left },
        new object[] { "GalacticHQControlRoom", "saturn", 8, 6, Direction.Up },
        new object[] { "GalacticHQControlRoom", "charon", 7, 6, Direction.Up },
        new object[] { "ResortAreaRibbonSyndicate1F", "beauty", 15, 14, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate1F", "cashier_f", 15, 4, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate1F", "middle_aged_woman", 23, 5, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate1F", "lady", 6, 13, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate1F", "skitty", 10, 11, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate1F", "psyduck", 25, 6, Direction.Down },
        // Plan 06 · R16b: the Mining Museum, the Game Corner, the prize exchange, the Coin Case's house, the Villa, Pal Park's lobby
        new object[] { "OreburghMiningMuseum", "scientist_m", 6, 3, Direction.Down },
        new object[] { "OreburghMiningMuseum", "ace_trainer_f", 4, 3, Direction.Down },
        new object[] { "OreburghMiningMuseum", "hiker", 8, 7, Direction.Down },
        new object[] { "OreburghMiningMuseum", "expert_m", 16, 15, Direction.Down },
        new object[] { "VeilstoneGameCorner", "receptionist", 2, 4, Direction.Down },
        new object[] { "VeilstoneGameCorner", "pokefan_m_1", 4, 8, Direction.Right },
        new object[] { "VeilstoneGameCorner", "collector", 6, 6, Direction.Right },
        new object[] { "VeilstoneGameCorner", "pokefan_m_2", 14, 8, Direction.Left },
        new object[] { "VeilstoneGameCorner", "looker", 9, 8, Direction.Left },
        new object[] { "VeilstonePrizeExchange", "receptionist", 4, 4, Direction.Down },
        new object[] { "VeilstonePrizeExchange", "guitarist", 2, 7, Direction.Up },
        new object[] { "VeilstoneSoutheastHouse", "clown", 6, 6, Direction.Down },
        new object[] { "ResortAreaVilla", "rival", 4, 3, Direction.Left },
        new object[] { "ResortAreaVilla", "prof_rowan", 17, 4, Direction.Up },
        new object[] { "ResortAreaVilla", "cynthia", 3, 5, Direction.Left },
        new object[] { "ResortAreaVilla", "crasher_wake", 19, 4, Direction.Up },
        new object[] { "ResortAreaVilla", "school_kid_m", 9, 6, Direction.Right },
        new object[] { "ResortAreaVilla", "delivery_guy", 10, 8, Direction.Up },
        new object[] { "PalParkLobby", "worker", 7, 11, Direction.Down },
        new object[] { "PalParkLobby", "lass_1", 3, 16, Direction.Down },
        new object[] { "PalParkLobby", "beauty", 12, 17, Direction.Down },
        // The rooms of the south-west and the centre, plan 01 · M11, part a
        new object[] { "TwinleafTownNortheastHouse", "guitarist", 8, 6, Direction.Down },
        new object[] { "TwinleafTownSouthwestHouse", "expert_f", 5, 5, Direction.Down },
        new object[] { "TwinleafTownSouthwestHouse", "twin", 8, 6, Direction.Down },
        new object[] { "SandgemTownCounterpartHouse1F", "expert_m", 3, 9, Direction.Up },
        new object[] { "SandgemTownCounterpartHouse1F", "twin", 5, 7, Direction.Up },
        new object[] { "SandgemTownHouse", "pokemon_breeder_m", 5, 5, Direction.Right },
        new object[] { "SandgemTownHouse", "pokemon_breeder_f", 8, 4, Direction.Left },
        new object[] { "JubilifeCityCondominiums1F", "expert_f", 15, 8, Direction.Left },
        new object[] { "JubilifeCityCondominiums1F", "pachirisu", 9, 6, Direction.Down },
        new object[] { "JubilifeCityCondominiums1F", "pikachu", 10, 6, Direction.Left },
        new object[] { "JubilifeCityCondominiums1F", "beauty", 8, 6, Direction.Down },
        new object[] { "JubilifeCityCondominiums2F", "bug_catcher", 11, 11, Direction.Up },
        new object[] { "JubilifeCityCondominiums2F", "pokemon_breeder_m", 8, 8, Direction.Left },
        new object[] { "JubilifeCitySouthHouse1F", "ace_trainer_m", 5, 9, Direction.Right },
        new object[] { "JubilifeCitySouthHouse1F", "lass", 8, 9, Direction.Left },
        new object[] { "JubilifeCitySouthHouse1F", "pachirisu", 11, 7, Direction.Right },
        new object[] { "JubilifeCitySouthHouse2F", "youngster", 8, 8, Direction.Down },
        new object[] { "JubilifeCitySouthHouse2F", "pokefan_f", 13, 9, Direction.Down },
        new object[] { "JubilifeCitySouthHouse2F", "pokemon_breeder_f", 16, 8, Direction.Left },
        new object[] { "OreburghCityNorthwestHouse1F", "pokemon_breeder_m", 14, 8, Direction.Right },
        new object[] { "OreburghCityNorthwestHouse1F", "youngster", 5, 5, Direction.Right },
        new object[] { "OreburghCityNorthwestHouse1F", "pokefan_f", 8, 10, Direction.Left },
        new object[] { "OreburghCityNorthwestHouse2F", "worker", 10, 8, Direction.Right },
        new object[] { "OreburghCityNorthwestHouse2F", "school_kid_f", 12, 8, Direction.Left },
        new object[] { "OreburghCityNorthwestHouse2F", "lass", 5, 5, Direction.Left },
        new object[] { "OreburghCityNorthwestHouse2F", "psyduck", 11, 8, Direction.Down },
        new object[] { "OreburghNorthHouse2F", "guitarist", 6, 6, Direction.Right },
        new object[] { "OreburghNorthHouse2F", "pokemon_breeder_f", 11, 10, Direction.Left },
        new object[] { "OreburghNorthHouse2F", "collector", 14, 5, Direction.Right },
        new object[] { "OreburghCityWestHouse", "youngster", 7, 6, Direction.Right },
        new object[] { "OreburghCityWestHouse", "pokefan_f", 5, 5, Direction.Left },
        new object[] { "OreburghCityWestHouse", "pikachu", 8, 6, Direction.Down },
        new object[] { "OreburghCityMiddleHouse", "school_kid_f", 8, 6, Direction.Left },
        new object[] { "OreburghCityMiddleHouse", "hiker", 2, 4, Direction.Down },
        new object[] { "OreburghCityEastHouse1F", "pokefan_m", 16, 10, Direction.Down },
        new object[] { "OreburghCityEastHouse1F", "hiker", 14, 7, Direction.Left },
        new object[] { "OreburghCityEastHouse1F", "ace_trainer_m", 10, 8, Direction.Right },
        new object[] { "OreburghCityEastHouse2F", "youngster", 5, 8, Direction.Right },
        new object[] { "OreburghCityEastHouse2F", "gentleman", 10, 11, Direction.Up },
        new object[] { "OreburghCityEastHouse2F", "scientist_m", 16, 5, Direction.Down },
        new object[] { "OreburghCitySouthHouse", "worker", 5, 5, Direction.Right },
        new object[] { "FlowerShop", "beauty", 14, 4, Direction.Down },
        new object[] { "FlowerShop", "lass", 4, 7, Direction.Down },
        new object[] { "FlowerShop", "pokemon_breeder_f", 14, 8, Direction.Down },
        new object[] { "FloaromaTownSoutheastHouse", "pokefan_f", 8, 6, Direction.Down },
        new object[] { "FloaromaTownSoutheastHouse", "pokefan_m", 5, 5, Direction.Left },
        new object[] { "FloaromaTownMiddleHouse", "pokemon_breeder_f", 8, 6, Direction.Left },
        new object[] { "FloaromaTownMiddleHouse", "twin", 2, 3, Direction.Right },
        new object[] { "FloaromaTownMiddleHouse", "clefairy", 3, 3, Direction.Left },
        new object[] { "FloaromaMeadowHouse", "hiker", 5, 3, Direction.Up },
        new object[] { "FloaromaMeadowHouse", "ninja_boy", 6, 6, Direction.Left },
        new object[] { "Route205House", "pokefan_f", 6, 5, Direction.Down },
        new object[] { "Route205House", "bug_catcher", 3, 5, Direction.Down },
        new object[] { "EternaCityEastHouse", "expert_m", 4, 5, Direction.Right },
        new object[] { "EternaCityEastHouse", "expert_f", 7, 5, Direction.Left },
        new object[] { "EternaCityEastHouse", "pokemon_breeder_m", 8, 7, Direction.Up },
        new object[] { "EternaCityHerbShop", "expert_f", 6, 6, Direction.Right },
        new object[] { "EternaCityHerbShop", "cashier_f", 3, 4, Direction.Down },
        new object[] { "EternaCityHerbShop", "expert_m", 8, 4, Direction.Up },
        new object[] { "EternaCondominiums2F", "expert_f", 17, 11, Direction.Left },
        new object[] { "EternaCondominiums2F", "twin", 6, 4, Direction.Right },
        new object[] { "EternaCondominiums2F", "expert_m", 6, 11, Direction.Up },
        new object[] { "EternaCondominiums3F", "pokefan_m", 5, 9, Direction.Left },
        new object[] { "EternaCondominiums3F", "school_kid_m", 16, 7, Direction.Down },
        new object[] { "EternaCondominiums3F", "bug_catcher", 10, 7, Direction.Down },
        new object[] { "EternaCondominiums3F", "ninja_boy", 13, 10, Direction.Up },
        new object[] { "Route208House", "old_man", 5, 5, Direction.Left },
        new object[] { "Route208House", "twin", 7, 7, Direction.Down },
        new object[] { "Route208House", "pokemon_breeder_f", 8, 5, Direction.Down },
        new object[] { "HearthomeCityNorthwestHouse", "bebe", 5, 5, Direction.Left },
        new object[] { "HearthomeCitySoutheastHouse1F", "middle_aged_man", 12, 9, Direction.Down },
        new object[] { "HearthomeCitySoutheastHouse1F", "lady", 4, 10, Direction.Right },
        new object[] { "HearthomeCitySoutheastHouse1F", "shroomish", 20, 7, Direction.Left },
        new object[] { "HearthomeCitySoutheastHouse1F", "twin", 19, 7, Direction.Right },
        new object[] { "HearthomeCitySoutheastHouse2F", "ace_trainer_f", 11, 8, Direction.Left },
        new object[] { "HearthomeCitySoutheastHouse2F", "clefairy", 10, 7, Direction.Down },
        new object[] { "ForeignBuilding", "ruin_maniac", 4, 9, Direction.Up },
        new object[] { "ForeignBuilding", "old_man", 13, 9, Direction.Left },
        new object[] { "ForeignBuilding", "ninja_boy", 2, 14, Direction.Left },
        new object[] { "ForeignBuilding", "old_woman", 5, 12, Direction.Up },
        new object[] { "ForeignBuilding", "ace_trainer_f", 6, 15, Direction.Up },
        new object[] { "ForeignBuilding", "pokefan_f", 11, 18, Direction.Up },
        new object[] { "ForeignBuilding", "lady", 8, 8, Direction.Up },
        new object[] { "ForeignBuilding", "psychic", 2, 19, Direction.Right },
        new object[] { "ForeignBuilding", "expert_m", 15, 10, Direction.Left },
        new object[] { "HearthomeCityNortheastHouse1F", "pokefan_f", 6, 10, Direction.Left },
        new object[] { "HearthomeCityNortheastHouse1F", "pikachu", 4, 11, Direction.Right },
        new object[] { "HearthomeCityNortheastHouse1F", "battle_girl", 5, 8, Direction.Down },
        new object[] { "HearthomeCityNortheastHouse1F", "ninja_boy", 12, 9, Direction.Left },
        new object[] { "HearthomeCityNortheastHouse1F", "twin", 2, 10, Direction.Down },
        new object[] { "HearthomeCityNortheastHouse1F", "lass", 15, 10, Direction.Left },
        new object[] { "HearthomeCityNortheastHouse1F", "school_kid_m", 2, 3, Direction.Right },
        new object[] { "HearthomeCityNortheastHouse2F", "expert_m", 8, 8, Direction.Right },
        new object[] { "HearthomeCityNortheastHouse2F", "expert_f", 12, 4, Direction.Up },
        new object[] { "HearthomeCityNortheastHouse2F", "old_man", 11, 8, Direction.Left },
        new object[] { "HearthomeCityNortheastHouse2F", "old_woman", 3, 5, Direction.Up },
        new object[] { "SolaceonTownNortheastHouse", "pokemon_breeder_f", 5, 5, Direction.Down },
        new object[] { "SolaceonTownNortheastHouse", "cowgirl", 7, 6, Direction.Down },
        new object[] { "SolaceonTownPokemonNewsPress", "gym_guide", 2, 5, Direction.Down },
        new object[] { "SolaceonTownPokemonNewsPress", "pokemon_breeder_m", 4, 3, Direction.Up },
        new object[] { "SolaceonTownNorthHouse", "old_woman", 5, 5, Direction.Down },
        new object[] { "SolaceonTownNorthHouse", "lass", 7, 6, Direction.Down },
        new object[] { "OldChateauDiningArea", "old_man", 19, 4, Direction.Down },
        new object[] { "OldChateauBackMiddleEastRoom", "twin", 20, 5, Direction.Left },
        new object[] { "VistaLighthouse", "sailor", 9, 3, Direction.Up },
        new object[] { "VistaLighthouse", "youngster", 8, 3, Direction.Up },
        new object[] { "VistaLighthouse", "volkner", 6, 4, Direction.Up },
        // The Battle Tower's lobby and Battle Salon, plan 06 · R18a (events_battle_tower, events_battle_tower_battle_salon)
        new object[] { "BattleTower", "single_attendant", 15, 5, Direction.Down },
        new object[] { "BattleTower", "multi_attendant", 11, 5, Direction.Down },
        new object[] { "BattleTower", "wifi_attendant", 19, 5, Direction.Down },
        new object[] { "BattleTower", "teala", 16, 6, Direction.Down },
        new object[] { "BattleTower", "artist", 2, 14, Direction.Down },
        new object[] { "BattleTower", "jogger", 13, 14, Direction.Left },
        new object[] { "BattleTower", "idol", 6, 10, Direction.Up },
        new object[] { "BattleTower", "rich_boy", 25, 13, Direction.Down },
        new object[] { "BattleTower", "pikachu_1", 25, 14, Direction.Left },
        new object[] { "BattleTower", "pikachu_2", 19, 16, Direction.Right },
        new object[] { "BattleTower", "school_kid_m_1", 20, 16, Direction.Left },
        new object[] { "BattleTower", "parasol_lady", 9, 13, Direction.Down },
        new object[] { "BattleTower", "school_kid_m_2", 20, 11, Direction.Left },
        new object[] { "BattleTower", "stats_judge", 24, 7, Direction.Down },
        new object[] { "BattleTowerBattleSalon", "cheryl", 13, 10, Direction.Left },
        new object[] { "BattleTowerBattleSalon", "mira", 4, 6, Direction.Right },
        new object[] { "BattleTowerBattleSalon", "riley", 11, 8, Direction.Down },
        new object[] { "BattleTowerBattleSalon", "marley", 2, 10, Direction.Right },
        new object[] { "BattleTowerBattleSalon", "buck", 7, 9, Direction.Up },
        new object[] { "BattleTowerElevator", "attendant", 4, 4, Direction.Down },
        new object[] { "BattleTowerBattleRoom", "attendant", 2, 6, Direction.Right },
        // The rooms of the east and the sea, plan 01 · M11, part b
        new object[] { "CafeCabin", "rancher", 7, 7, Direction.Right },
        new object[] { "CafeCabin", "waitress", 8, 4, Direction.Left },
        new object[] { "CafeCabin", "collector_fernando", 1, 4, Direction.Right },
        new object[] { "CafeCabin", "collector_edwin", 3, 4, Direction.Left },
        new object[] { "CafeCabin", "waitress_kati", 5, 3, Direction.Down },
        new object[] { "CafeCabin", "old_man", 3, 6, Direction.Left },
        new object[] { "VeilstoneCitySouthwestHouse", "beauty", 5, 5, Direction.Down },
        new object[] { "VeilstoneCitySouthwestHouse", "youngster", 3, 3, Direction.Down },
        new object[] { "VeilstoneCitySouthwestHouse", "pachirisu", 7, 6, Direction.Down },
        new object[] { "VeilstoneCityNorthwestHouse", "ace_trainer_f", 4, 3, Direction.Down },
        new object[] { "VeilstoneCityNorthwestHouse", "old_woman", 6, 6, Direction.Down },
        new object[] { "VeilstoneCityNorthwestHouse", "buneary", 3, 5, Direction.Down },
        new object[] { "VeilstoneCityNortheastHouse", "pokefan_m", 5, 5, Direction.Left },
        new object[] { "VeilstoneCityNortheastHouse", "pokefan_f", 4, 4, Direction.Down },
        new object[] { "SevenStarsRestaurant", "gentleman_leonardo", 13, 3, Direction.Right },
        new object[] { "SevenStarsRestaurant", "socialite_rebecca", 15, 3, Direction.Left },
        new object[] { "SevenStarsRestaurant", "rich_boy_roman", 8, 3, Direction.Right },
        new object[] { "SevenStarsRestaurant", "lady_kylie", 10, 3, Direction.Left },
        new object[] { "SevenStarsRestaurant", "reporter_valerie", 17, 8, Direction.Left },
        new object[] { "SevenStarsRestaurant", "cameraman_darryl", 15, 8, Direction.Right },
        new object[] { "SevenStarsRestaurant", "artist_ismael", 3, 3, Direction.Right },
        new object[] { "SevenStarsRestaurant", "pi_kendrick", 4, 11, Direction.Right },
        new object[] { "SevenStarsRestaurant", "beauty_gabriella", 6, 11, Direction.Left },
        new object[] { "SevenStarsRestaurant", "beauty_harley", 5, 3, Direction.Left },
        new object[] { "SevenStarsRestaurant", "aroma_lady_alison", 7, 6, Direction.Left },
        new object[] { "SevenStarsRestaurant", "veteran_emanuel", 1, 8, Direction.Right },
        new object[] { "SevenStarsRestaurant", "lass_blythe", 3, 8, Direction.Left },
        new object[] { "SevenStarsRestaurant", "pokefan_meredith", 13, 6, Direction.Left },
        new object[] { "SevenStarsRestaurant", "scientist_emilio", 12, 11, Direction.Right },
        new object[] { "SevenStarsRestaurant", "school_kid_esteban", 11, 6, Direction.Right },
        new object[] { "SevenStarsRestaurant", "breeder_kaylee", 14, 11, Direction.Left },
        new object[] { "SevenStarsRestaurant", "collector_eugine", 5, 6, Direction.Right },
        new object[] { "SevenStarsRestaurant", "waiter", 10, 12, Direction.Left },
        new object[] { "GrandLakeValorLakefrontEastHouse", "beauty", 5, 3, Direction.Down },
        new object[] { "GrandLakeValorLakefrontWestHouse", "game_director", 5, 3, Direction.Down },
        new object[] { "GrandLakeRoute213Lobby", "ace_trainer_f", 3, 5, Direction.Down },
        new object[] { "GrandLakeRoute213Lobby", "policeman", 11, 4, Direction.Left },
        new object[] { "GrandLakeRoute213Lobby", "looker", 9, 4, Direction.Right },
        new object[] { "GrandLakeRoute213EastHouse", "middle_aged_man", 8, 5, Direction.Right },
        new object[] { "GrandLakeRoute213NorthwestHouse", "clown", 2, 3, Direction.Left },
        new object[] { "GrandLakeRoute213NorthwestHouse", "psychic", 8, 6, Direction.Left },
        new object[] { "GrandLakeRoute213NorthwestHouse", "guitarist", 5, 5, Direction.Left },
        new object[] { "GrandLakeRoute213NortheastHouse", "rich_boy", 4, 4, Direction.Down },
        new object[] { "GrandLakeRoute213NortheastHouse", "lady", 6, 5, Direction.Left },
        new object[] { "PastoriaCityMiddleHouse", "old_man", 7, 6, Direction.Left },
        new object[] { "PastoriaCityMiddleHouse", "bug_catcher", 5, 5, Direction.Down },
        new object[] { "PastoriaCityNorthHouse", "scientist_m", 5, 6, Direction.Down },
        new object[] { "PastoriaCityNorthHouse", "psyduck", 4, 4, Direction.Down },
        new object[] { "PastoriaCityNorthHouse", "kid_with_nds", 8, 5, Direction.Left },
        new object[] { "PastoriaCityNortheastHouse", "ace_trainer_m", 5, 5, Direction.Left },
        new object[] { "PastoriaCityNortheastHouse", "battle_girl", 8, 6, Direction.Down },
        new object[] { "PastoriaCityNortheastHouse", "pachirisu", 3, 3, Direction.Right },
        new object[] { "PastoriaCitySouthwestHouse", "pokemon_breeder_f", 5, 5, Direction.Right },
        new object[] { "PastoriaCitySouthwestHouse", "twin", 7, 6, Direction.Right },
        new object[] { "PastoriaCityObservatoryGate1F", "cashier_f", 2, 7, Direction.Down },
        new object[] { "PastoriaCityObservatoryGate1F", "cashier_m", 2, 6, Direction.Right },
        new object[] { "PastoriaCityObservatoryGate1F", "cowgirl", 1, 9, Direction.Right },
        new object[] { "PastoriaCityObservatoryGate2F", "ace_trainer_f", 5, 10, Direction.Left },
        new object[] { "PastoriaCityObservatoryGate2F", "bug_catcher", 4, 5, Direction.Down },
        new object[] { "PastoriaCityObservatoryGate2F", "pokefan_m", 3, 9, Direction.Up },
        new object[] { "PastoriaCityObservatoryGate2F", "picnicker", 7, 5, Direction.Down },
        new object[] { "PokemonMansion", "maid_office", 49, 6, Direction.Down },
        new object[] { "PokemonMansion", "maid_trophy_garden", 34, 6, Direction.Down },
        new object[] { "PokemonMansion", "maid_far_west", 1, 7, Direction.Right },
        new object[] { "PokemonMansion", "maid_far_east", 56, 7, Direction.Left },
        new object[] { "PokemonMansion", "five_maid_knockout_maid", 1, 7, Direction.Right },
        new object[] { "PokemonMansion", "maid_east", 42, 6, Direction.Right },
        new object[] { "PokemonMansion", "liam", 1, 7, Direction.Right },
        new object[] { "PokemonMansion", "celeste", 1, 7, Direction.Right },
        new object[] { "PokemonMansionMaidsRoomMiddle", "maid_middle", 3, 6, Direction.Up },
        new object[] { "PokemonMansionMaidsRoomEast", "maid_east", 4, 7, Direction.Right },
        new object[] { "PokemonMansionOffice", "mr_backlot", 7, 8, Direction.Down },
        new object[] { "PokemonMansionOffice", "old_man", 6, 8, Direction.Down },
        new object[] { "PokemonMansionOffice", "policeman", 8, 4, Direction.Down },
        new object[] { "Route221House", "expert_m", 2, 5, Direction.Right },
        new object[] { "CanalaveCitySoutheastHouse", "lass", 2, 4, Direction.Down },
        new object[] { "CanalaveCitySoutheastHouse", "ace_trainer_f", 8, 7, Direction.Down },
        new object[] { "CanalaveCitySailorEldritchHouse", "little_boy", 5, 5, Direction.Up },
        new object[] { "CanalaveCitySailorEldritchHouse", "pokefan_f", 8, 4, Direction.Up },
        // The Battle Factory's lobby, plan 06 · R18b (events_battle_factory)
        new object[] { "BattleFactory", "single_attendant", 21, 6, Direction.Down },
        new object[] { "BattleFactory", "multi_attendant", 23, 6, Direction.Down },
        new object[] { "BattleFactory", "youngster", 16, 5, Direction.Right },
        new object[] { "BattleFactory", "beauty", 15, 12, Direction.Left },
        new object[] { "BattleFactory", "policeman", 25, 9, Direction.Up },
        new object[] { "BattleFactory", "sailor", 1, 12, Direction.Up },
        new object[] { "BattleFactory", "pokemon_breeder_f", 1, 11, Direction.Down },
        new object[] { "BattleFactory", "clown", 18, 9, Direction.Left },
        new object[] { "BattleFactory", "lady", 22, 11, Direction.Left },
        // Plan 01 · M11, part c: the rooms of the north and of the Battle Zone
        new object[] { "CelesticTownNorthwestHouse", "expert_m", 2, 4, Direction.Down },
        new object[] { "CelesticTownNorthwestHouse", "expert_f", 4, 4, Direction.Down },
        new object[] { "CelesticTownNorthwestHouse", "gym_guide", 7, 6, Direction.Left },
        new object[] { "CelesticTownNortheastHouse", "pokefan_m", 7, 6, Direction.Down },
        new object[] { "CelesticTownNortheastHouse", "ninja_boy", 2, 5, Direction.Left },
        new object[] { "CelesticTownSouthwestHouse", "happiny", 2, 5, Direction.Down },
        new object[] { "CelesticTownSouthwestHouse", "black_belt", 7, 6, Direction.Down },
        new object[] { "Route216House", "hiker", 3, 5, Direction.Down },
        new object[] { "Route216House", "snowpoint_npc_f", 6, 4, Direction.Left },
        new object[] { "Route217WestHouse", "hiker", 4, 5, Direction.Up },
        new object[] { "Route217NortheastHouse", "pokemon_breeder_f", 4, 5, Direction.Down },
        new object[] { "Route222EastHouse", "fisherman", 5, 5, Direction.Left },
        new object[] { "Route222WestHouse", "pikachu_1", 8, 5, Direction.Left },
        new object[] { "Route222WestHouse", "pikachu_2", 2, 7, Direction.Up },
        new object[] { "Route222WestHouse", "pikachu_3", 1, 4, Direction.Right },
        new object[] { "Route222WestHouse", "pikachu_4", 7, 7, Direction.Down },
        new object[] { "Route222WestHouse", "pikachu_5", 7, 5, Direction.Right },
        new object[] { "Route222WestHouse", "pokefan_m", 5, 5, Direction.Down },
        new object[] { "Route222WestHouse", "poke_kid_janet", 4, 3, Direction.Right },
        new object[] { "SunyshoreCityWestHouse", "battle_girl", 6, 6, Direction.Down },
        new object[] { "SunyshoreCityWestHouse", "clown", 5, 3, Direction.Up },
        new object[] { "SunyshoreCityWestHouse", "gym_guide", 2, 5, Direction.Down },
        new object[] { "SunyshoreCityWestHouse", "pokemon_breeder_f", 2, 7, Direction.Down },
        new object[] { "SunyshoreCityWestHouse", "youngster_1", 9, 5, Direction.Left },
        new object[] { "SunyshoreCityWestHouse", "youngster_2", 9, 6, Direction.Left },
        new object[] { "SunyshoreCityWestHouse", "youngster_3", 8, 7, Direction.Left },
        new object[] { "SunyshoreCityNorthwestHouse", "old_man", 3, 5, Direction.Left },
        new object[] { "SunyshoreCityNorthwestHouse", "old_woman", 1, 5, Direction.Right },
        new object[] { "SunyshoreCityEastHouse", "scientist_m", 5, 5, Direction.Up },
        new object[] { "FightAreaMiddleHouse", "pokefan_m", 5, 5, Direction.Right },
        new object[] { "FightAreaMiddleHouse", "school_kid_m", 8, 6, Direction.Left },
        new object[] { "FightAreaMiddleHouse", "youngster", 3, 7, Direction.Up },
        new object[] { "FightAreaSouthHouse", "old_man", 6, 5, Direction.Left },
        new object[] { "FightAreaSouthHouse", "torchic", 2, 6, Direction.Down },
        new object[] { "FightAreaSouthHouse", "old_woman", 2, 5, Direction.Right },
        new object[] { "FightAreaSouthHouse", "item_expert", 8, 6, Direction.Down },
        new object[] { "Route225House", "worker", 5, 6, Direction.Down },
        new object[] { "SurvivalAreaSouthHouse", "black_belt", 4, 5, Direction.Left },
        new object[] { "Route226House", "meister", 2, 5, Direction.Right },
        new object[] { "Route227House", "expert_f", 3, 5, Direction.Left },
        new object[] { "Route228SouthHouse", "gentleman", 2, 3, Direction.Left },
        new object[] { "ResortAreaHouse", "collector", 6, 4, Direction.Up },
        new object[] { "ResortAreaHouse", "old_man", 2, 5, Direction.Up },
        new object[] { "ResortAreaHouse", "ninja_boy", 8, 7, Direction.Up },
        new object[] { "ResortAreaRibbonSyndicate2F", "receptionist_1", 16, 5, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate2F", "receptionist_2", 12, 7, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicate2F", "receptionist_3", 12, 5, Direction.Left },
        new object[] { "ResortAreaRibbonSyndicate2F", "receptionist_4", 16, 12, Direction.Down },
        new object[] { "ResortAreaRibbonSyndicateElevator", "receptionist", 1, 6, Direction.Right },
        // The rooms' guests after the Hall of Fame (plan 02 · S15, part a)
        new object[] { "PalParkLobby", "prof_oak", 7, 15, Direction.Down },
        new object[] { "EternaCitySouthHouse", "prof_oak", 4, 5, Direction.Down },
        new object[] { "IronIslandHouse", "byron", 5, 5, Direction.Down },
        new object[] { "CanalaveLibrary1F", "lucian", 16, 5, Direction.Up },
        // The Battle Hall's lobby (plan 06 · R18c): the original's fifteen less its reporter (plan 08)
        new object[] { "BattleHall", "single_attendant", 6, 9, Direction.Right },
        new object[] { "BattleHall", "multi_attendant", 6, 10, Direction.Right },
        new object[] { "BattleHall", "record_keeper", 8, 8, Direction.Down },
        new object[] { "BattleHall", "maid", 2, 5, Direction.Left },
        new object[] { "BattleHall", "pokefan_f", 3, 7, Direction.Right },
        new object[] { "BattleHall", "twin", 15, 5, Direction.Down },
        new object[] { "BattleHall", "pachirisu", 15, 6, Direction.Up },
        new object[] { "BattleHall", "snowpoint_npc_f", 20, 4, Direction.Left },
        new object[] { "BattleHall", "hiker", 23, 11, Direction.Up },
        new object[] { "BattleHall", "expert_m", 10, 3, Direction.Down },
        new object[] { "BattleHall", "idol", 6, 6, Direction.Up },
        new object[] { "BattleHall", "bug_catcher", 25, 4, Direction.Left },
        new object[] { "BattleHall", "fan", 18, 11, Direction.Left },
        new object[] { "BattleHall", "visitor_counterpart", 4, 6, Direction.Down },
        new object[] { "BattleCastle", "single_attendant", 5, 9, Direction.Right },
        new object[] { "BattleCastle", "multi_attendant", 5, 10, Direction.Right },
        new object[] { "BattleCastle", "roughneck", 14, 13, Direction.Right },
        new object[] { "BattleCastle", "black_belt", 2, 14, Direction.Left },
        new object[] { "BattleCastle", "gentleman", 8, 12, Direction.Up },
        new object[] { "BattleCastle", "collector", 12, 4, Direction.Down },
        new object[] { "BattleCastle", "artist", 20, 5, Direction.Left },
        new object[] { "BattleCastle", "socialite", 14, 5, Direction.Right },
        new object[] { "BattleCastle", "waitress", 9, 7, Direction.Down },
        // Plan 01 · M11, part d: the gate houses (events_<gate>.json)
        new object[] { "Route218GateToJubilifeCity", "policeman", 7, 6, Direction.Down },
        new object[] { "Route218GateToJubilifeCity", "fisherman", 4, 8, Direction.Up },
        new object[] { "Route218GateToCanalaveCity", "policeman", 2, 5, Direction.Down },
        new object[] { "Route218GateToCanalaveCity", "scientist_m", 5, 7, Direction.Right },
        new object[] { "Route208GateToHearthomeCity", "pokemon_breeder_m", 7, 9, Direction.Down },
        new object[] { "Route208GateToHearthomeCity", "middle_aged_woman", 5, 6, Direction.Down },
        new object[] { "Route208GateToHearthomeCity", "clefairy", 4, 6, Direction.Down },
        new object[] { "Route215GateToVeilstoneCity", "ruin_maniac", 7, 8, Direction.Right },
        new object[] { "Route213GateToPastoriaCity", "pokefan_m", 8, 5, Direction.Left },
        new object[] { "Route213GateToPastoriaCity", "school_kid_f", 5, 8, Direction.Down },
        new object[] { "Route222GateToSunyshoreCity", "worker", 7, 5, Direction.Down },
        new object[] { "Route222GateToSunyshoreCity", "pokefan_m", 6, 8, Direction.Right },
        new object[] { "Route228GateToRoute226", "school_kid_f", 5, 8, Direction.Left },
        new object[] { "Route228GateToRoute226", "ace_trainer_f", 7, 6, Direction.Up },
        new object[] { "Route212GateToHearthomeCity", "collector", 4, 4, Direction.Up },
        new object[] { "Route212GateToHearthomeCity", "cowgirl", 6, 7, Direction.Up },
        new object[] { "Route225GateToFightArea", "ace_trainer_m", 4, 10, Direction.Down },
        new object[] { "Route225GateToFightArea", "rich_boy", 6, 5, Direction.Left },
        new object[] { "Route214GateToVeilstoneCity", "middle_aged_man", 6, 8, Direction.Up },
        new object[] { "Route206CyclingRoadNorthGate", "cashier_m_east", 10, 7, Direction.Left },
        new object[] { "Route206CyclingRoadNorthGate", "cashier_m_west", 4, 7, Direction.Right },
        new object[] { "Route206CyclingRoadNorthGate", "scientist_m", 6, 4, Direction.Down },
        new object[] { "Route206CyclingRoadSouthGate", "cashier_m_west", 3, 7, Direction.Right },
        new object[] { "Route206CyclingRoadSouthGate", "cashier_m_east", 11, 7, Direction.Left },
        new object[] { "Route206CyclingRoadSouthGate", "battle_girl", 9, 9, Direction.Left },
        new object[] { "HearthomeCityWestGateToAmitySquare", "receptionist", 6, 3, Direction.Down },
        new object[] { "HearthomeCityWestGateToAmitySquare", "beauty", 6, 8, Direction.Up },
        new object[] { "HearthomeCityWestGateToAmitySquare", "pikachu", 6, 7, Direction.Down },
        new object[] { "HearthomeCityEastGateToAmitySquare", "receptionist", 6, 3, Direction.Down },
        new object[] { "HearthomeCityEastGateToAmitySquare", "black_belt", 6, 7, Direction.Down },
        new object[] { "BattleFrontierGateToFightArea", "pokemon_breeder_m", 4, 6, Direction.Up },
        new object[] { "BattleFrontierGateToFightArea", "expert_m", 11, 6, Direction.Left },
        new object[] { "BattleFrontierGateToFightArea", "ruin_maniac", 3, 4, Direction.Left },
        new object[] { "BattleFrontierGateToFightArea", "attendant_battle_castle", 5, 13, Direction.Right },
        new object[] { "BattleFrontierGateToFightArea", "attendant_battle_factory", 5, 15, Direction.Right },
        new object[] { "BattleFrontierGateToFightArea", "attendant_general", 5, 17, Direction.Right },
        new object[] { "BattleFrontierGateToFightArea", "attendant_battle_arcade", 9, 13, Direction.Left },
        new object[] { "BattleFrontierGateToFightArea", "attendant_battle_hall", 9, 15, Direction.Left },
        new object[] { "BattleFrontierGateToFightArea", "attendant_battle_tower", 9, 17, Direction.Left },
        new object[] { "BattleFrontierGateToFightArea", "beauty", 11, 3, Direction.Left },
        // Plan 01 · M11, part d: the tutors' and the two move men's houses
        new object[] { "Route210GrandmaWilmaHouse", "wilma", 5, 5, Direction.Down },
        new object[] { "Route212House", "beauty", 3, 4, Direction.Right },
        new object[] { "Route212House", "hiker", 5, 6, Direction.Left },
        new object[] { "Route212House", "twin", 8, 7, Direction.Down },
        new object[] { "PastoriaCityEastHouse", "move_maniac", 8, 4, Direction.Down },
        new object[] { "PastoriaCityEastHouse", "youngster", 2, 4, Direction.Down },
        new object[] { "CanalaveCityEastHouse", "move_deleter", 2, 6, Direction.Right },
        new object[] { "SnowpointCityEastHouse", "expert_m", 5, 5, Direction.Down },
        new object[] { "SnowpointCityEastHouse", "hiker", 2, 4, Direction.Down },
        new object[] { "SurvivalAreaNorthHouse", "hiker", 3, 4, Direction.Right },
        new object[] { "SurvivalAreaNorthHouse", "psychic", 8, 5, Direction.Up },
        new object[] { "Route228NorthHouse", "expert_m", 2, 5, Direction.Down },
        // Plan 01 · M11, part d: Veilstone's Department Store (events_veilstone_store_<floor>.json)
        new object[] { "VeilstoneStore1F", "receptionist", 16, 8, Direction.Left },
        new object[] { "VeilstoneStore1F", "middle_aged_man", 17, 11, Direction.Down },
        new object[] { "VeilstoneStore1F", "lady", 2, 9, Direction.Right },
        new object[] { "VeilstoneStore1F", "socialite", 4, 11, Direction.Up },
        new object[] { "VeilstoneStore1F", "cashier_f", 3, 5, Direction.Right },
        new object[] { "VeilstoneStore1F", "cashier_m", 2, 5, Direction.Down },
        new object[] { "VeilstoneStore2F", "lady", 15, 11, Direction.Right },
        new object[] { "VeilstoneStore2F", "bug_catcher_1", 5, 11, Direction.Right },
        new object[] { "VeilstoneStore2F", "cashier_m", 2, 8, Direction.Down },
        new object[] { "VeilstoneStore2F", "receptionist", 18, 6, Direction.Left },
        new object[] { "VeilstoneStore2F", "cashier_f_up", 2, 4, Direction.Right },
        new object[] { "VeilstoneStore2F", "cashier_f_mid", 2, 6, Direction.Right },
        new object[] { "VeilstoneStore2F", "bug_catcher_2", 6, 10, Direction.Right },
        new object[] { "VeilstoneStore3F", "receptionist", 18, 6, Direction.Left },
        new object[] { "VeilstoneStore3F", "twin", 9, 10, Direction.Down },
        new object[] { "VeilstoneStore3F", "guitarist", 7, 4, Direction.Down },
        new object[] { "VeilstoneStore3F", "cashier_f", 3, 4, Direction.Down },
        new object[] { "VeilstoneStore3F", "cashier_m", 3, 11, Direction.Up },
        new object[] { "VeilstoneStore3F", "pokemon_breeder_f", 18, 10, Direction.Down },
        new object[] { "VeilstoneStore4F", "ninja_boy", 15, 10, Direction.Left },
        new object[] { "VeilstoneStore4F", "pokefan_f", 16, 11, Direction.Up },
        new object[] { "VeilstoneStore4F", "receptionist", 18, 5, Direction.Left },
        new object[] { "VeilstoneStore4F", "cashier_f", 3, 5, Direction.Right },
        new object[] { "VeilstoneStore4F", "cashier_m", 2, 8, Direction.Down },
        new object[] { "VeilstoneStore4F", "school_kid_m", 6, 11, Direction.Down },
        new object[] { "VeilstoneStore5F", "bug_catcher", 3, 6, Direction.Down },
        new object[] { "VeilstoneStore5F", "middle_aged_woman", 14, 8, Direction.Up },
        new object[] { "VeilstoneStore5F", "collector_1", 10, 11, Direction.Right },
        new object[] { "VeilstoneStore5F", "collector_2", 9, 11, Direction.Right },
        new object[] { "VeilstoneStoreB1F", "receptionist_1", 15, 6, Direction.Left },
        new object[] { "VeilstoneStoreB1F", "cashier_f_lava_cookie", 20, 11, Direction.Up },
        new object[] { "VeilstoneStoreB1F", "cashier_f_poffin", 15, 11, Direction.Down },
        new object[] { "VeilstoneStoreB1F", "socialite_1", 3, 8, Direction.Up },
        new object[] { "VeilstoneStoreB1F", "receptionist_2", 5, 6, Direction.Right },
        new object[] { "VeilstoneStoreB1F", "cashier_f_berry", 5, 11, Direction.Down },
        new object[] { "VeilstoneStoreB1F", "cashier_f_rage_candy_bar", 20, 6, Direction.Left },
        new object[] { "VeilstoneStoreB1F", "socialite_2", 16, 9, Direction.Up },
        new object[] { "VeilstoneStoreB1F", "gentleman", 21, 13, Direction.Left },
        new object[] { "VeilstoneStoreB1F", "prof_rowan", 15, 4, Direction.Down },
        new object[] { "VeilstoneStoreElevator", "receptionist", 1, 6, Direction.Right },
        // Plan 01 · M11, part d: the Global Terminal (events_global_terminal_<floor>.json)
        new object[] { "GlobalTerminal1F", "receptionist_gts", 7, 3, Direction.Down },
        new object[] { "GlobalTerminal1F", "collector", 3, 9, Direction.Right },
        new object[] { "GlobalTerminal1F", "bug_catcher", 6, 7, Direction.Right },
        new object[] { "GlobalTerminal1F", "guitarist", 3, 13, Direction.Up },
        new object[] { "GlobalTerminal1F", "expert_m", 24, 7, Direction.Right },
        new object[] { "GlobalTerminal1F", "ace_trainer_m", 6, 12, Direction.Up },
        new object[] { "GlobalTerminal1F", "beauty_1", 12, 7, Direction.Right },
        new object[] { "GlobalTerminal1F", "receptionist_entry_north", 9, 15, Direction.Right },
        new object[] { "GlobalTerminal1F", "receptionist_entry_south", 9, 16, Direction.Right },
        new object[] { "GlobalTerminal1F", "picnicker", 25, 12, Direction.Up },
        new object[] { "GlobalTerminal1F", "youngster", 16, 16, Direction.Left },
        new object[] { "GlobalTerminal1F", "pokemon_breeder_f", 13, 3, Direction.Up },
        new object[] { "GlobalTerminal1F", "pokemon_breeder_m", 12, 13, Direction.Down },
        new object[] { "GlobalTerminal1F", "beauty_2", 15, 10, Direction.Down },
        new object[] { "GlobalTerminal2F", "battle_girl", 10, 6, Direction.Left },
        new object[] { "GlobalTerminal2F", "twin", 11, 17, Direction.Down },
        new object[] { "GlobalTerminal2F", "pokefan_f", 13, 10, Direction.Left },
        new object[] { "GlobalTerminal2F", "beauty", 4, 15, Direction.Right },
        new object[] { "GlobalTerminal2F", "lass", 13, 15, Direction.Left },
        new object[] { "GlobalTerminal2F", "expert_f", 10, 14, Direction.Left },
        new object[] { "GlobalTerminal2F", "hiker", 5, 8, Direction.Right },
        new object[] { "GlobalTerminal2F", "idol", 20, 11, Direction.Right },
        new object[] { "GlobalTerminal2F", "artist", 7, 4, Direction.Down },
        new object[] { "GlobalTerminal3F", "gentleman", 8, 16, Direction.Right },
        new object[] { "GlobalTerminal3F", "hiker", 18, 10, Direction.Left },
        new object[] { "GlobalTerminal3F", "guitarist", 15, 10, Direction.Left },
        new object[] { "GlobalTerminal3F", "school_kid_f", 6, 6, Direction.Up },
        new object[] { "GlobalTerminal3F", "camper", 4, 11, Direction.Right },
        new object[] { "GlobalTerminal3F", "youngster", 6, 5, Direction.Down },
        new object[] { "GlobalTerminal3F", "pokefan_m", 4, 10, Direction.Right },
        new object[] { "GlobalTerminal3F", "clown", 11, 10, Direction.Left },
        new object[] { "GlobalTerminal3F", "ace_trainer_m", 9, 6, Direction.Left },
        // Pal Park's viewing gallery (plan 01 · M11, part d): the lobby's four spectators on the original's tiles
        new object[] { "PalParkGallery", "lass_2", 9, 4, Direction.Up },
        new object[] { "PalParkGallery", "pokefan_m", 8, 4, Direction.Up },
        new object[] { "PalParkGallery", "youngster", 3, 4, Direction.Up },
        new object[] { "PalParkGallery", "lady", 14, 4, Direction.Up },
        // The Battle Arcade's lobby (plan 06 · R18d): the original's twelve (its reporter is plan 08's)
        new object[] { "BattleArcade", "single_attendant", 14, 5, Direction.Down },
        new object[] { "BattleArcade", "multi_attendant", 15, 5, Direction.Down },
        new object[] { "BattleArcade", "psychic", 19, 9, Direction.Right },
        new object[] { "BattleArcade", "waitress", 2, 12, Direction.Down },
        new object[] { "BattleArcade", "camper", 2, 14, Direction.Right },
        new object[] { "BattleArcade", "beauty", 4, 14, Direction.Right },
        new object[] { "BattleArcade", "worker", 12, 3, Direction.Left },
        new object[] { "BattleArcade", "bug_catcher", 7, 11, Direction.Left },
        new object[] { "BattleArcade", "tuber_f", 21, 14, Direction.Right },
        new object[] { "BattleArcade", "parasol_lady", 19, 6, Direction.Right },
        new object[] { "BattleArcade", "hiker_1", 13, 8, Direction.Down },
        new object[] { "BattleArcade", "hiker_2", 13, 9, Direction.Up },
        // The Battleground (plan 02 · S15, part c): Buck's grandfather behind his bar and Buck before it. The day's four
        // guests are put on the original's four tiles as the room is come into (TheBattlegroundAndTheLastLegendsTests)
        new object[] { "Battleground", "expert_m", 13, 9, Direction.Left },
        new object[] { "Battleground", "buck", 11, 8, Direction.Down },
        // The Pokémon Centers' ground floors on the original's one plan (plan 01 · M11, the last rooms)
        new object[] { "PokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "PokemonCenter", "pokemon_breeder_m", 6, 6, Direction.Right },
        new object[] { "PokemonCenter", "expert_m", 13, 5, Direction.Right },
        new object[] { "PokemonCenter", "lass", 5, 11, Direction.Left },
        new object[] { "PokemonCenter", "teala", 13, 10, Direction.Left },
        new object[] { "JubilifePokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "JubilifePokemonCenter", "ace_trainer_f", 14, 6, Direction.Up },
        new object[] { "JubilifePokemonCenter", "school_kid_m", 4, 7, Direction.Right },
        new object[] { "JubilifePokemonCenter", "twin", 10, 8, Direction.Down },
        new object[] { "JubilifePokemonCenter", "teala", 13, 10, Direction.Left },
        new object[] { "OreburghPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "OreburghPokemonCenter", "psychic", 5, 9, Direction.Right },
        new object[] { "OreburghPokemonCenter", "ace_trainer_f", 3, 6, Direction.Down },
        new object[] { "OreburghPokemonCenter", "ninja_boy", 11, 8, Direction.Left },
        new object[] { "OreburghPokemonCenter", "gentleman", 12, 12, Direction.Left },
        new object[] { "OreburghPokemonCenter", "kid_with_nds_west", 14, 5, Direction.Right },
        new object[] { "OreburghPokemonCenter", "kid_with_nds_east", 15, 5, Direction.Left },
        new object[] { "FloaromaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "FloaromaPokemonCenter", "ace_trainer_f", 5, 9, Direction.Down },
        new object[] { "FloaromaPokemonCenter", "battle_girl", 12, 8, Direction.Left },
        new object[] { "FloaromaPokemonCenter", "pokefan_m", 1, 6, Direction.Right },
        new object[] { "EternaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "EternaPokemonCenter", "school_kid_m", 5, 8, Direction.Down },
        new object[] { "EternaPokemonCenter", "battle_girl", 14, 6, Direction.Down },
        new object[] { "EternaPokemonCenter", "buneary", 4, 8, Direction.Down },
        new object[] { "EternaPokemonCenter", "pokemon_breeder_f", 10, 9, Direction.Down },
        new object[] { "HearthomePokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "HearthomePokemonCenter", "youngster", 10, 8, Direction.Down },
        new object[] { "HearthomePokemonCenter", "socialite", 5, 9, Direction.Right },
        new object[] { "HearthomePokemonCenter", "guitarist", 2, 7, Direction.Right },
        new object[] { "SolaceonPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "SolaceonPokemonCenter", "old_man", 13, 7, Direction.Down },
        new object[] { "SolaceonPokemonCenter", "lass", 5, 9, Direction.Left },
        new object[] { "SolaceonPokemonCenter", "ruin_maniac", 11, 11, Direction.Down },
        new object[] { "VeilstonePokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "VeilstonePokemonCenter", "socialite", 3, 6, Direction.Down },
        new object[] { "VeilstonePokemonCenter", "school_kid_m", 10, 8, Direction.Down },
        new object[] { "PastoriaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "PastoriaPokemonCenter", "black_belt", 4, 7, Direction.Down },
        new object[] { "PastoriaPokemonCenter", "school_kid_f", 11, 8, Direction.Down },
        new object[] { "CanalavePokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "CanalavePokemonCenter", "lass", 11, 9, Direction.Down },
        new object[] { "CanalavePokemonCenter", "pokemon_breeder_f", 2, 6, Direction.Down },
        new object[] { "CanalavePokemonCenter", "bug_catcher", 14, 5, Direction.Down },
        new object[] { "CanalavePokemonCenter", "scientist_m", 15, 5, Direction.Down },
        new object[] { "CelesticPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "CelesticPokemonCenter", "expert_f", 11, 7, Direction.Down },
        new object[] { "CelesticPokemonCenter", "twin", 10, 10, Direction.Down },
        new object[] { "CelesticPokemonCenter", "pokefan_m", 3, 8, Direction.Right },
        new object[] { "SnowpointPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "SnowpointPokemonCenter", "lass", 3, 6, Direction.Down },
        new object[] { "SnowpointPokemonCenter", "battle_girl", 12, 11, Direction.Left },
        new object[] { "SnowpointPokemonCenter", "psyduck", 10, 8, Direction.Down },
        new object[] { "SnowpointPokemonCenter", "maylene", 7, 6, Direction.Up },
        new object[] { "SunyshorePokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "SunyshorePokemonCenter", "old_man", 6, 10, Direction.Down },
        new object[] { "SunyshorePokemonCenter", "ninja_boy", 10, 7, Direction.Left },
        new object[] { "SunyshorePokemonCenter", "middle_aged_man", 3, 6, Direction.Right },
        new object[] { "PokemonLeagueSouthPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "PokemonLeagueSouthPokemonCenter", "pokefan_m", 3, 6, Direction.Up },
        new object[] { "PokemonLeagueSouthPokemonCenter", "ace_trainer_f", 9, 8, Direction.Up },
        new object[] { "FightAreaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "FightAreaPokemonCenter", "guitarist", 10, 8, Direction.Down },
        new object[] { "FightAreaPokemonCenter", "waitress", 6, 9, Direction.Right },
        new object[] { "FightAreaPokemonCenter", "black_belt", 4, 5, Direction.Down },
        new object[] { "SurvivalAreaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "SurvivalAreaPokemonCenter", "ace_trainer_m", 1, 5, Direction.Right },
        new object[] { "SurvivalAreaPokemonCenter", "expert_m", 10, 8, Direction.Right },
        new object[] { "SurvivalAreaPokemonCenter", "psychic", 5, 9, Direction.Down },
        new object[] { "ResortAreaPokemonCenter", "nurse", 8, 4, Direction.Down },
        new object[] { "ResortAreaPokemonCenter", "rich_boy", 3, 6, Direction.Down },
        new object[] { "ResortAreaPokemonCenter", "idol", 11, 9, Direction.Left },
        // The Poké Marts on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "PokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "PokeMart", "school_kid_m", 6, 9, Direction.Right },
        new object[] { "PokeMart", "pokemon_breeder_m", 9, 6, Direction.Down },
        new object[] { "JubilifePokeMart", "guitarist", 8, 7, Direction.Left },
        new object[] { "JubilifePokeMart", "pokefan_f", 7, 3, Direction.Up },
        new object[] { "JubilifePokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "JubilifePokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "JubilifePokeMart", "beauty_5", 6, 10, Direction.Right },
        new object[] { "OreburghPokeMart", "hiker", 6, 6, Direction.Down },
        new object[] { "OreburghPokeMart", "collector", 6, 9, Direction.Left },
        new object[] { "OreburghPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "OreburghPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "FloaromaPokeMart", "beauty", 6, 8, Direction.Down },
        new object[] { "FloaromaPokeMart", "school_kid_f", 9, 6, Direction.Right },
        new object[] { "FloaromaPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "FloaromaPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "EternaPokeMart", "guitarist", 8, 3, Direction.Up },
        new object[] { "EternaPokeMart", "hiker", 6, 8, Direction.Right },
        new object[] { "EternaPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "EternaPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "HearthomePokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "HearthomePokeMart", "hiker", 5, 8, Direction.Right },
        new object[] { "HearthomePokeMart", "beauty", 8, 3, Direction.Up },
        new object[] { "HearthomePokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "SolaceonPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "SolaceonPokeMart", "ninja_boy", 8, 7, Direction.Left },
        new object[] { "SolaceonPokeMart", "pokefan_f", 5, 9, Direction.Down },
        new object[] { "SolaceonPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "PastoriaPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "PastoriaPokeMart", "bug_catcher", 5, 9, Direction.Down },
        new object[] { "PastoriaPokeMart", "school_kid_f", 8, 7, Direction.Up },
        new object[] { "PastoriaPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "CanalavePokeMart", "lady", 2, 8, Direction.Down },
        new object[] { "CanalavePokeMart", "pokefan_f", 6, 6, Direction.Down },
        new object[] { "CanalavePokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "CanalavePokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "SnowpointPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "SnowpointPokeMart", "youngster", 5, 9, Direction.Down },
        new object[] { "SnowpointPokeMart", "snowpoint_lady", 9, 8, Direction.Down },
        new object[] { "SnowpointPokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "SunyshorePokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "SunyshorePokeMart", "ruin_maniac", 6, 8, Direction.Down },
        new object[] { "SunyshorePokeMart", "sailor", 8, 7, Direction.Up },
        new object[] { "SunyshorePokeMart", "cashier_m", 2, 5, Direction.Down },
        new object[] { "FightAreaPokeMart", "clown", 7, 8, Direction.Left },
        new object[] { "FightAreaPokeMart", "socialite", 2, 9, Direction.Right },
        new object[] { "FightAreaPokeMart", "cashier_f", 3, 5, Direction.Down },
        new object[] { "SurvivalAreaPokeMart", "ace_trainer_f", 7, 7, Direction.Down },
        new object[] { "SurvivalAreaPokeMart", "hiker", 8, 3, Direction.Down },
        new object[] { "SurvivalAreaPokeMart", "cashier_f", 3, 5, Direction.Down },
        // The Trainers' School on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "TrainersSchool", "teacher", 5, 4, Direction.Down },
        new object[] { "TrainersSchool", "rival", 6, 3, Direction.Up },
        new object[] { "TrainersSchool", "youngster_1", 14, 9, Direction.Down },
        new object[] { "TrainersSchool", "lass", 2, 8, Direction.Right },
        new object[] { "TrainersSchool", "youngster_2", 5, 8, Direction.Left },
        new object[] { "TrainersSchool", "school_kid_harrison", 12, 4, Direction.Down },
        new object[] { "TrainersSchool", "school_kid_christine", 14, 4, Direction.Down },
        // Jubilife TV's lobby on the original's plan (plan 01 · M11, the last rooms)
        new object[] { "JubilifeTV1F", "beauty", 13, 13, Direction.Left },
        new object[] { "JubilifeTV1F", "gym_guide", 13, 7, Direction.Left },
        new object[] { "JubilifeTV1F", "poketch_co_president", 3, 11, Direction.Left },
        new object[] { "JubilifeTV1F", "ace_trainer_snow_f", 9, 7, Direction.Down },
        new object[] { "JubilifeTV1F", "ace_trainer_m", 7, 10, Direction.Left },
        // The Pokétch Company's three floors on the original's plans (plan 01 · M11, the last rooms)
        new object[] { "PoketchCompany", "scientist_m", 15, 8, Direction.Down },
        new object[] { "PoketchCompany", "receptionist", 16, 3, Direction.Down },
        new object[] { "PoketchCompany", "poketch_co_president", 18, 5, Direction.Down },
        new object[] { "PoketchCompany", "lass", 6, 7, Direction.Right },
        new object[] { "PoketchCompany2F", "pokefan_m", 12, 6, Direction.Right },
        new object[] { "PoketchCompany2F", "old_man", 2, 9, Direction.Right },
        new object[] { "PoketchCompany2F", "clefairy", 3, 10, Direction.Right },
        new object[] { "PoketchCompany2F", "pachirisu", 4, 11, Direction.Down },
        new object[] { "PoketchCompany3F", "ninja_boy", 7, 5, Direction.Right },
        new object[] { "PoketchCompany3F", "pokefan_f", 14, 6, Direction.Left },
        new object[] { "PoketchCompany3F", "expert_f", 3, 6, Direction.Left },
        // The rival's house on the original's plans (plan 01 · M11, the last rooms)
        new object[] { "RivalHouse", "rival_mom", 4, 6, Direction.Down },
        // The rival's room over it (plan 01 · M11, the last rooms)
        new object[] { "RivalHouse2F", "rival", 9, 5, Direction.Up },
    };

    [Theory]
    [MemberData(nameof(People))]
    public void TheOriginalsPeopleStandOnTheOriginalsTiles(string room, string id, int x, int y, Direction facing)
    {
        var map = Room(room);
        var npc = Assert.Single(map.Everyone, n => n.Key == id);
        Assert.Equal((x, y, facing), (npc.GridX, npc.GridY, npc.Facing));
        Assert.False(map.IsSolid(x, y), $"{room}: {id} stands in something solid");
    }

    [Theory]
    [MemberData(nameof(Plans))]
    public void TheFloorBeginsWhereTheOriginalsDoesAndTheWayOutIsBelowItsMat(string room, int left, int back, int matX, int matY)
    {
        var map = Room(room);
        Assert.True(map.IsIndoors);
        Assert.Equal((left, back), map.RoomCorner());
        // The door is in the front wall, the row under the original's exit mat
        Assert.Equal(map.Height - 1, matY + 1);
        Assert.Equal(TileType.Door, map.GetGroundTile(matX, matY + 1));
        var door = Assert.Single(map.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((matX, matY + 1), (door.SourceX, door.SourceY));
        Assert.True(map.IsWalkable(matX, matY));
    }

    [Fact]
    public void AGateHouseHasADoorInEachSideWallToTheMapOfSinnoh()
    {
        foreach (string name in Gates)
        {
            var map = Room(name);
            Assert.True(map.IsIndoors);
            var doors = map.Warps.Where(w => w.TargetMap == "Sinnoh").OrderBy(w => w.SourceX).ToList();
            Assert.Equal(2, doors.Count);
            Assert.Equal((0, map.Width - 1), (doors[0].SourceX, doors[1].SourceX));
            Assert.All(doors, d => Assert.Equal(TileType.Door, map.GetGroundTile(d.SourceX, d.SourceY)));
            Assert.Equal((Direction.Left, Direction.Right), (doors[0].TargetFacing, doors[1].TargetFacing));
        }
    }

    [Fact]
    public void TheLostTowersStairsJoinEachFloorToTheNextBesideTheStairs()
    {
        for (int i = 0; i < LostTower.Length - 1; i++)
        {
            Map below = Room(LostTower[i]), above = Room(LostTower[i + 1]);
            var up = Assert.Single(below.Warps, w => w.TargetMap == above.Name);
            var down = Assert.Single(above.Warps, w => w.TargetMap == below.Name);
            // Each comes out beside the other's stairs, walking away from them
            Assert.Equal((down.SourceX + 1, down.SourceY, Direction.Right), (up.TargetX, up.TargetY, up.TargetFacing));
            Assert.Equal((up.SourceX - 1, up.SourceY, Direction.Left), (down.TargetX, down.TargetY, down.TargetFacing));
            Assert.True(above.IsWalkable(up.TargetX, up.TargetY));
            Assert.True(below.IsWalkable(down.TargetX, down.TargetY));
            Assert.Contains(below.Props, p => p.Type == PropType.SideStairsUpEast && p.X == up.SourceX + 1);
            Assert.Contains(above.Props, p => p.Type == PropType.SideStairsDownWest && p.X == down.SourceX - 2);
        }
        // Wild Pokémon live on every floor, and the top is lost in fog
        Assert.All(LostTower, name => Assert.Equal(12, Room(name).WildEncounters.Count));
        Assert.All(LostTower, name => Assert.Equal(TileBehavior.OldChateauFloor, Room(name).BehaviourAt(7, 6)));
        Assert.Equal(FieldWeather.Fog, Room("LostTower5F").WeatherAt(7, 6));
        Assert.Equal(FieldWeather.Clear, Room("LostTower4F").WeatherAt(7, 6));
    }

    [Fact]
    public void EveryOtherRoomBeginsWhereTheHandMadeOnesDo()
    {
        // The rooms that are not laid out as the hand-made ones are: a list each, one to a line, so that sessions
        // appending in the same night merge by keeping both
        var rebuilt = new IEnumerable<string>[]
        {
            Plans.Select(p => (string)p[0]),
            UpperFloors,
            Gates,
            LostTower,
            LibraryUpperFloors,
            GalacticHeadquarters,
            OddlyShaped,
            InnerRooms,
            LighthouseGallery,
            BattleTower,
            BattleFactory,
            SyndicateLift,
            BattleHallAndCastle,
            NorthSouthGates,
            StoreFloors,
            CenterFloors,
            TerminalFloors,
            ParkGallery,
            BattleArcade,
            PoketchCompany,
            RivalsRoom,
        }.SelectMany(names => names).ToHashSet();
        foreach (string path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var map = Room(name);
            if (!map.IsIndoors || rebuilt.Contains(name) || map.Interior == InteriorStyle.Gym) continue; // Gyms: GymTests
            Assert.Equal((1, 2), map.RoomCorner());
            // No hand-made room has walls of its own inside it
            for (int ty = 2; ty < map.Height - 1; ty++)
                for (int tx = 1; tx < map.Width - 1; tx++)
                    Assert.NotEqual(TileType.Wall, map.GetGroundTile(tx, ty));
        }
    }

    [Fact]
    public void TheWindworksKeepsTheOriginalsTriggerTrainersAndComputers()
    {
        var map = Room("ValleyWindworksBuilding");

        // Commander Mars waits beside the original's trigger: a column two tiles deep in front of her
        var trigger = Assert.Single(map.Triggers);
        Assert.Equal((19, 6, 1, 2), (trigger.X, trigger.Y, trigger.Width, trigger.Depth));
        Assert.Equal(("Mars", "VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE", 1), (trigger.Script, trigger.Variable, trigger.Value));

        // The two grunts are Platinum's, with their teams, and see two tiles ahead
        foreach (var (id, trainer, species) in new[] { ("galactic_grunt_2", "galactic_grunt_valley_windworks_2", "Zubat"), ("galactic_grunt_3", "galactic_grunt_valley_windworks_3", "Glameow") })
        {
            var grunt = map.Everyone.Single(n => n.Key == id);
            Assert.True(grunt.IsTrainer);
            Assert.Equal((trainer, 2, species), (grunt.TrainerData!.Id, grunt.TrainerData.SightRange, grunt.TrainerData.Party.Members[0].Species.Name));
            Assert.Equal("FLAG_HIDE_VALLEY_WINDWORKS_BUILDING_TEAM_GALACTIC", grunt.HiddenBy);
        }

        // The two computers are read from the tile west of them, as the original's are
        foreach (var (x, y) in new[] { (19, 9), (22, 9) })
        {
            Assert.Equal("Notes", map.SignScripts[(x, y)]);
            Assert.True(map.IsSolid(x, y));
            Assert.True(map.IsWalkable(x - 1, y));
        }

        // Past the hall's doorway in the partition, the trigger can't be walked round: the desk and the grunt close
        // the way on either side of it
        Assert.False(map.IsWalkable(18, 5));
        Assert.Contains(map.Everyone, n => (n.GridX, n.GridY) == (18, 8));
    }

    // The original's stairs between the Team Galactic Eterna Building's floors: each pair of warps leads one into the
    // other, and whoever goes up or down is put beside the stairs at the far end, walking away from them
    public static IEnumerable<object[]> Stairs => new[]
    {
        new object[] { "TeamGalacticEternaBuilding1F", 14, 6, "TeamGalacticEternaBuilding2F", 3, 3 },
        new object[] { "TeamGalacticEternaBuilding1F", 20, 6, "TeamGalacticEternaBuilding2F", 8, 3 },
        new object[] { "TeamGalacticEternaBuilding2F", 14, 3, "TeamGalacticEternaBuilding3F", 2, 3 },
        new object[] { "TeamGalacticEternaBuilding2F", 20, 3, "TeamGalacticEternaBuilding3F", 8, 3 },
        new object[] { "TeamGalacticEternaBuilding3F", 14, 3, "TeamGalacticEternaBuilding4F", 3, 3 },
        new object[] { "TeamGalacticEternaBuilding3F", 20, 3, "TeamGalacticEternaBuilding4F", 8, 3 },
    };

    [Theory]
    [MemberData(nameof(Stairs))]
    public void TheEternaBuildingsStairsJoinItsFloorsAsTheOriginalsDo(string below, int ux, int uy, string above, int dx, int dy)
    {
        var lower = Room(below);
        var upper = Room(above);
        var up = lower.GetWarpAt(ux, uy)!;
        Assert.Equal((above, dx - 1, dy, Direction.Left), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
        var down = upper.GetWarpAt(dx, dy)!;
        Assert.Equal((below, ux + 1, uy, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.True(upper.IsWalkable(dx - 1, dy));
        Assert.True(lower.IsWalkable(ux + 1, uy));
        // The flights stand beside their warps: up to the west, down to the east
        Assert.Contains(lower.Props, p => p.Type == PropType.SideStairsUp && p.Covers(ux - 1, uy));
        Assert.Contains(upper.Props, p => p.Type == PropType.SideStairsDown && p.Covers(dx + 1, dy));
    }

    [Fact]
    public void TheCanalaveLibrarysStairsJoinItsFloorsAsTheOriginalsDo()
    {
        // The original's warps: 1F (11, 4) to 2F (4, 4), 2F (11, 4) to 3F (4, 3); a flight climbing west below, a
        // stairwell going down east above, and whoever takes them is put beside the stairs at the far end
        foreach (var (below, ux, uy, above, dx, dy) in new[] { ("CanalaveLibrary1F", 11, 4, "CanalaveLibrary2F", 4, 4), ("CanalaveLibrary2F", 11, 4, "CanalaveLibrary3F", 4, 3) })
        {
            Map lower = Room(below), upper = Room(above);
            var up = lower.GetWarpAt(ux, uy)!;
            Assert.Equal((above, dx - 1, dy, Direction.Left), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
            var down = upper.GetWarpAt(dx, dy)!;
            Assert.Equal((below, ux + 1, uy, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
            Assert.True(upper.IsWalkable(dx - 1, dy));
            Assert.True(lower.IsWalkable(ux + 1, uy));
            Assert.Contains(lower.Props, p => p.Type == PropType.SideStairsUp && p.Covers(ux - 1, uy));
            Assert.Contains(upper.Props, p => p.Type == PropType.SideStairsDown && p.Covers(dx + 1, dy));
        }
        // The upper floors begin where the original's do, and have no door
        Assert.Equal((1, 4), Room("CanalaveLibrary2F").RoomCorner());
        Assert.Equal((1, 3), Room("CanalaveLibrary3F").RoomCorner());
        Assert.All(LibraryUpperFloors, name => Assert.DoesNotContain(Room(name).Warps, w => w.TargetMap == "Sinnoh"));
        // The third floor's books are read where the original's are: seven of them, on twenty-one tiles
        var top = Room("CanalaveLibrary3F");
        Assert.Equal(7, top.SignScripts.Values.Distinct().Count());
        Assert.Equal(21, top.SignScripts.Count);
        Assert.Equal("SinnohMyth", top.SignScripts[(9, 3)]);
        Assert.Equal("SinnohFolkStories", top.SignScripts[(20, 9)]);
        // And its table stands between the tiles the meeting is held on
        Assert.Contains(top.Props, p => p.Type == PropType.Table && p.Covers(7, 8) && p.Covers(8, 9));
        Assert.True(top.IsWalkable(7, 7));
        Assert.True(top.IsWalkable(8, 7));
    }

    /// <summary>The tiles one can walk to on a floor from a tile, without its stairs, whoever stands about.</summary>
    private static HashSet<(int, int)> Reach(Map map, int x, int y)
    {
        var seen = new HashSet<(int, int)> { (x, y) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (map.GetWarpAt(cx, cy) != null && (cx, cy) != (x, y)) continue;
            foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                if (map.InBounds(nx, ny) && !map.IsSolid(nx, ny) && seen.Add((nx, ny))) queue.Enqueue((nx, ny));
        }
        return seen;
    }

    [Fact]
    public void OneOfEachFloorsTwoStaircasesIsATrap()
    {
        // Looker's warning: on each floor one way up comes out in a pocket with nothing but a grunt or two and an item,
        // whose only way on is back down, and the other comes out where the next way up is
        foreach (var (floor, trap, onward, next) in new[]
                 {
                     ("TeamGalacticEternaBuilding2F", (2, 3), (7, 3), (14, 3)),
                     ("TeamGalacticEternaBuilding3F", (7, 3), (1, 3), (14, 3)),
                     ("TeamGalacticEternaBuilding4F", (7, 3), (2, 3), (14, 7))
                 })
        {
            var map = Room(floor);
            Assert.DoesNotContain(next, Reach(map, trap.Item1, trap.Item2));
            Assert.Contains(next, Reach(map, onward.Item1, onward.Item2));
        }
        // Commander Jupiter stands where the right way leads, before the manager she holds
        var top = Room("TeamGalacticEternaBuilding4F");
        Assert.Equal((14, 6), (top.FindPerson("jupiter")!.GridX, top.FindPerson("jupiter")!.GridY));
    }

    [Fact]
    public void ARoomsInnerWallsArePaintedAsTheLowerPartOfItsWalls()
    {
        foreach (var style in new[] { InteriorStyle.House, InteriorStyle.Lab })
        {
            var whole = new PixelCanvas(32, PropModels.WallHeight);
            GroundBaker.PaintWall(whole, style);
            var inner = new PixelCanvas(32, PropModels.InnerWallHeight);
            GroundBaker.PaintWall(inner, style, PropModels.WallHeight - PropModels.InnerWallHeight);
            for (int y = 0; y < inner.Height; y++)
                for (int x = 0; x < 32; x++)
                    Assert.Equal(whole.Get(x, PropModels.WallHeight - PropModels.InnerWallHeight + y), inner.Get(x, y));
        }
    }
}
