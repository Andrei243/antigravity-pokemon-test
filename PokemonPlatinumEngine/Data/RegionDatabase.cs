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
    Boat,
    /// <summary>By air (plan 18 · W4, part b): the later regions' links, Unova's to Kalos and on, once their plans name a departure.</summary>
    Plane
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

    /// <summary>Whether the region's places change with the seasons (plan 22 · U2: Unova's alone).</summary>
    public bool HasSeasons => File?.HasSeasons ?? false;

    /// <summary>The name the introduction offers for the rival; null where the region's rival is its other hero.</summary>
    public string? Rival => File?.Rival;

    /// <summary>
    /// The rival's name for a player of this look: the name the introduction offers, or where the rival is the hero
    /// the player isn't, that hero's (Hoenn's May for the boy, Brendan for the girl; plan 21 · H2). Null for neither.
    /// </summary>
    public string? RivalFor(Core.PlayerLook look) =>
        Rival ?? (look == Core.PlayerLook.Girl ? File?.Rivals?.Girl : File?.Rivals?.Boy);

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

    /// <summary>
    /// The region's levels for a team that crossed into it (plan 18 · W4; the file's <c>levels</c>): what its wild
    /// Pokémon, trainers and gifts are met at in a game begun in another region. Null where the file maps none.
    /// </summary>
    public LevelMapping? Levels
    {
        get
        {
            if (!levelsRead)
            {
                levels = LevelMapping.From(File?.Levels);
                levelsRead = true;
            }
            return levels;
        }
    }

    private LevelMapping? levels;
    private bool levelsRead;

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

    /// <summary>
    /// What the attendant calls the way across, our own word (plan 18 · W4, part b: <c>ship</c>, <c>ferry</c>, <c>plane</c>);
    /// when the link names none, the transport's own (a boat's ship, a plane's plane), and "way" while it is undecided.
    /// </summary>
    public string? Vessel { get; init; }

    /// <summary>The word the attendant's lines use for the way across.</summary>
    public string VesselWord => Vessel is { Length: > 0 } v ? v : Transport switch
    {
        Transport.Boat => "ship",
        Transport.Plane => "plane",
        _ => "way"
    };
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
                // Plan 14 · B8: the Battle Frontier's annex, its lobby and the Battle Dome's hall (maps of ours)
                "BattleFrontierAnnex", "BattleDome",
                // Plan 14 · B9: the Battle Palace's and the Battle Arena's halls in the annex (maps of ours)
                "BattlePalace", "BattleArena",
                // Plan 14 · B10: the Battle Pike's hall of doors and its room, and the Battle Pyramid's floor (maps of ours)
                "BattlePike", "BattlePikeRoom", "BattlePyramidFloor",
                // Plan 14 · B11: the World Tournament's hall in the annex (a map of ours)
                "WorldTournamentHall",
                // Plan 14 · B15: the Masters' Hall off the annex's lobby (a map of ours)
                "MastersHall",
            }
        },
        new()
        {
            Id = Kanto, Name = "Kanto", Generation = 1, File = RegionFiles.Load(Kanto),
            // FireRed's Kanto, read by the GBA reader (plan 19 · K1). A new game begun there wakes in the player's room
            // upstairs, as FireRed's does (plan 19 · K3: the original's warp to the bedroom's 6, 6), with nothing; a
            // Champion come across from Sinnoh steps out before the player's house in Pallet Town, on the overworld
            Start = new MapSpot("PalletTown_PlayersHouse_2F", 6, 6, Direction.Up),
            Arrival = new MapSpot("Kanto", 66, 268, Direction.Down),
            Maps = new[]
            {
                "Kanto",
                // Plan 19 · K1: Pallet Town's rooms and Viridian City's, each a matrix of the import built from its layout
                "PalletTown_PlayersHouse_1F", "PalletTown_PlayersHouse_2F", "PalletTown_RivalsHouse", "PalletTown_ProfessorOaksLab",
                "ViridianCity_House", "ViridianCity_Gym", "ViridianCity_School", "ViridianCity_Mart",
                "ViridianCity_PokemonCenter_1F", "ViridianCity_PokemonCenter_2F",
                // Plan 19 · K2: Viridian Forest, Route 2's rooms and Route 22's gate
                "ViridianForest", "Route2_ViridianForest_SouthEntrance", "Route2_House", "Route2_EastBuilding",
                "Route2_ViridianForest_NorthEntrance", "Route22_NorthEntrance",
                // Plan 19 · K3: Pewter City's rooms
                "PewterCity_Museum_1F", "PewterCity_Museum_2F", "PewterCity_Gym", "PewterCity_Mart", "PewterCity_House1",
                "PewterCity_PokemonCenter_1F", "PewterCity_PokemonCenter_2F", "PewterCity_House2",
                // Plan 19 · K4: Mt. Moon, Route 4's Pokémon Center, Cerulean City's rooms and Bill's Sea Cottage
                "MtMoon_1F", "MtMoon_B1F", "MtMoon_B2F", "Route4_PokemonCenter_1F", "Route4_PokemonCenter_2F",
                "CeruleanCity_House1", "CeruleanCity_House2", "CeruleanCity_House3", "CeruleanCity_PokemonCenter_1F",
                "CeruleanCity_PokemonCenter_2F", "CeruleanCity_Gym", "CeruleanCity_BikeShop", "CeruleanCity_Mart",
                "CeruleanCity_House4", "CeruleanCity_House5", "Route25_SeaCottage",
                // Plan 19 · K4, part b: Vermilion City's rooms, the Underground Path, Route 11's gate, the S.S. Anne
                // and Diglett's Cave
                "VermilionCity_House1", "VermilionCity_PokemonCenter_1F", "VermilionCity_PokemonCenter_2F",
                "VermilionCity_PokemonFanClub", "VermilionCity_House2", "VermilionCity_Mart", "VermilionCity_Gym",
                "VermilionCity_House3", "UndergroundPath_NorthEntrance", "UndergroundPath_NorthSouthTunnel",
                "UndergroundPath_SouthEntrance", "Route11_EastEntrance_1F", "Route11_EastEntrance_2F",
                "SSAnne_Exterior", "SSAnne_1F_Corridor", "SSAnne_2F_Corridor", "SSAnne_3F_Corridor", "SSAnne_B1F_Corridor",
                "SSAnne_Deck", "SSAnne_Kitchen", "SSAnne_CaptainsOffice", "SSAnne_1F_Room1", "SSAnne_1F_Room2",
                "SSAnne_1F_Room3", "SSAnne_1F_Room4", "SSAnne_1F_Room5", "SSAnne_1F_Room7", "SSAnne_1F_Room6",
                "SSAnne_2F_Room1", "SSAnne_2F_Room2", "SSAnne_2F_Room3", "SSAnne_2F_Room4", "SSAnne_2F_Room5",
                "SSAnne_2F_Room6", "SSAnne_B1F_Room1", "SSAnne_B1F_Room2", "SSAnne_B1F_Room3", "SSAnne_B1F_Room4",
                "SSAnne_B1F_Room5", "DiglettsCave_NorthEntrance", "DiglettsCave_B1F", "DiglettsCave_SouthEntrance",
                // Plan 19 · K5, part a: Route 10's Pokémon Center, Rock Tunnel, Lavender Town's rooms, the Pokémon Tower,
                // the Underground Path between Routes 7 and 8 and the gates of Routes 7 and 8
                "Route10_PokemonCenter_1F", "Route10_PokemonCenter_2F", "RockTunnel_1F", "RockTunnel_B1F",
                "LavenderTown_PokemonCenter_1F", "LavenderTown_PokemonCenter_2F", "LavenderTown_VolunteerPokemonHouse",
                "LavenderTown_House1", "LavenderTown_House2", "LavenderTown_Mart", "PokemonTower_1F", "PokemonTower_2F",
                "PokemonTower_3F", "PokemonTower_4F", "PokemonTower_5F", "PokemonTower_6F", "PokemonTower_7F",
                "UndergroundPath_WestEntrance", "UndergroundPath_EastWestTunnel", "UndergroundPath_EastEntrance",
                "Route7_EastEntrance", "Route8_WestEntrance",
                // Plan 19 · K5, part b: Celadon City's rooms, the Rocket Hideout, Routes 16's and 12's rooms, Saffron
                // City's rooms, Silph Co. and the gates of Routes 5 and 6
                "CeladonCity_Condominiums_1F", "CeladonCity_Condominiums_2F", "CeladonCity_Condominiums_3F",
                "CeladonCity_Condominiums_Roof", "CeladonCity_Condominiums_RoofRoom", "CeladonCity_DepartmentStore_1F",
                "CeladonCity_DepartmentStore_2F", "CeladonCity_DepartmentStore_3F", "CeladonCity_DepartmentStore_4F",
                "CeladonCity_DepartmentStore_5F", "CeladonCity_DepartmentStore_Elevator", "CeladonCity_DepartmentStore_Roof",
                "CeladonCity_GameCorner", "CeladonCity_GameCorner_PrizeRoom", "CeladonCity_Gym", "CeladonCity_Hotel",
                "CeladonCity_House1", "CeladonCity_PokemonCenter_1F", "CeladonCity_PokemonCenter_2F", "CeladonCity_Restaurant",
                "RocketHideout_B1F", "RocketHideout_B2F", "RocketHideout_B3F", "RocketHideout_B4F", "RocketHideout_Elevator",
                "Route16_House", "Route16_NorthEntrance_1F", "Route16_NorthEntrance_2F", "Route12_FishingHouse",
                "Route12_NorthEntrance_1F", "Route12_NorthEntrance_2F", "SaffronCity_CopycatsHouse_1F",
                "SaffronCity_CopycatsHouse_2F", "SaffronCity_Dojo", "SaffronCity_Gym", "SaffronCity_House", "SaffronCity_Mart",
                "SaffronCity_MrPsychicsHouse", "SaffronCity_PokemonCenter_1F", "SaffronCity_PokemonCenter_2F",
                "SaffronCity_PokemonTrainerFanClub", "SilphCo_1F", "SilphCo_2F", "SilphCo_3F", "SilphCo_4F", "SilphCo_5F",
                "SilphCo_6F", "SilphCo_7F", "SilphCo_8F", "SilphCo_9F", "SilphCo_10F", "SilphCo_11F", "SilphCo_Elevator",
                "Route5_SouthEntrance", "Route6_NorthEntrance",
                // Plan 19 · K6, part a: the gates of Routes 15 and 18, Fuchsia City's rooms and the Safari Zone
                "Route15_WestEntrance_1F", "Route15_WestEntrance_2F", "Route18_EastEntrance_1F", "Route18_EastEntrance_2F",
                "FuchsiaCity_SafariZone_Entrance", "FuchsiaCity_Mart", "FuchsiaCity_SafariZone_Office", "FuchsiaCity_Gym",
                "FuchsiaCity_House1", "FuchsiaCity_PokemonCenter_1F", "FuchsiaCity_PokemonCenter_2F",
                "FuchsiaCity_WardensHouse", "FuchsiaCity_House2", "FuchsiaCity_House3",
                "SafariZone_Center", "SafariZone_East", "SafariZone_North", "SafariZone_West", "SafariZone_Center_RestHouse",
                "SafariZone_East_RestHouse", "SafariZone_North_RestHouse", "SafariZone_West_RestHouse", "SafariZone_SecretHouse",
                // Plan 19 · K6, part b: the Seafoam Islands, the Power Plant, Cinnabar Island's rooms and the Pokémon Mansion
                "SeafoamIslands_1F", "SeafoamIslands_B1F", "SeafoamIslands_B2F", "SeafoamIslands_B3F", "SeafoamIslands_B4F",
                "PowerPlant", "CinnabarIsland_Gym", "CinnabarIsland_PokemonLab_Entrance", "CinnabarIsland_PokemonLab_Lounge",
                "CinnabarIsland_PokemonLab_ResearchRoom", "CinnabarIsland_PokemonLab_ExperimentRoom",
                "CinnabarIsland_PokemonCenter_1F", "CinnabarIsland_PokemonCenter_2F", "CinnabarIsland_Mart",
                "PokemonMansion_1F", "PokemonMansion_2F", "PokemonMansion_3F", "PokemonMansion_B1F",
                // Plan 19 · K7: Victory Road, the Indigo Plateau's Pokémon Center, the Pokémon League's rooms, and One
                // Island with its rooms
                "VictoryRoad_1F", "VictoryRoad_2F", "VictoryRoad_3F", "IndigoPlateau_PokemonCenter_1F",
                "IndigoPlateau_PokemonCenter_2F", "PokemonLeague_LoreleisRoom", "PokemonLeague_BrunosRoom",
                "PokemonLeague_AgathasRoom", "PokemonLeague_LancesRoom", "PokemonLeague_ChampionsRoom",
                "PokemonLeague_HallOfFame", "OneIsland", "OneIsland_PokemonCenter_1F", "OneIsland_PokemonCenter_2F",
                "OneIsland_House1", "OneIsland_House2", "OneIsland_Harbor",
                // Plan 19 · K9, part a: the Ember Spa, Mt. Ember, Two Island and Three Island with their rooms
                "OneIsland_KindleRoad_EmberSpa", "MtEmber_Exterior", "MtEmber_SummitPath_1F", "MtEmber_SummitPath_2F",
                "MtEmber_SummitPath_3F", "MtEmber_Summit", "MtEmber_RubyPath_1F", "MtEmber_RubyPath_B1F",
                "MtEmber_RubyPath_B2F", "MtEmber_RubyPath_B3F", "MtEmber_RubyPath_B4F", "MtEmber_RubyPath_B5F",
                "MtEmber_RubyPath_B1F_Stairs", "MtEmber_RubyPath_B2F_Stairs", "TwoIsland", "TwoIsland_JoyfulGameCorner",
                "TwoIsland_House", "TwoIsland_PokemonCenter_1F", "TwoIsland_PokemonCenter_2F", "TwoIsland_Harbor",
                "TwoIsland_CapeBrink_House", "ThreeIsland", "ThreeIsland_BerryForest", "ThreeIsland_DunsparceTunnel",
                "ThreeIsland_House1", "ThreeIsland_PokemonCenter_1F", "ThreeIsland_PokemonCenter_2F", "ThreeIsland_Mart",
                "ThreeIsland_House2", "ThreeIsland_House3", "ThreeIsland_House4", "ThreeIsland_House5", "ThreeIsland_Harbor",
                // Plan 19 · K9, part b: Four to Seven Island with their rooms and caves, and Cerulean Cave
                "FourIsland", "FourIsland_IcefallCave_Entrance", "FourIsland_IcefallCave_1F", "FourIsland_IcefallCave_B1F",
                "FourIsland_IcefallCave_Back", "FourIsland_PokemonDayCare", "FourIsland_PokemonCenter_1F", "FourIsland_PokemonCenter_2F",
                "FourIsland_House1", "FourIsland_LoreleisHouse", "FourIsland_Harbor", "FourIsland_House2", "FourIsland_Mart",
                "FiveIsland", "FiveIsland_RocketWarehouse", "FiveIsland_LostCave_Entrance", "FiveIsland_LostCave_Room1",
                "FiveIsland_LostCave_Room2", "FiveIsland_LostCave_Room3", "FiveIsland_LostCave_Room4", "FiveIsland_LostCave_Room5",
                "FiveIsland_LostCave_Room6", "FiveIsland_LostCave_Room7", "FiveIsland_LostCave_Room8", "FiveIsland_LostCave_Room9",
                "FiveIsland_LostCave_Room10", "FiveIsland_LostCave_Room11", "FiveIsland_LostCave_Room12", "FiveIsland_LostCave_Room13",
                "FiveIsland_LostCave_Room14", "FiveIsland_PokemonCenter_1F", "FiveIsland_PokemonCenter_2F", "FiveIsland_Harbor",
                "FiveIsland_House1", "FiveIsland_House2", "FiveIsland_ResortGorgeous_House", "SixIsland", "SixIsland_DottedHole_1F",
                "SixIsland_DottedHole_B1F", "SixIsland_DottedHole_B2F", "SixIsland_DottedHole_B3F", "SixIsland_DottedHole_B4F",
                "SixIsland_DottedHole_SapphireRoom", "SixIsland_PatternBush", "SixIsland_AlteringCave", "SixIsland_PokemonCenter_1F",
                "SixIsland_PokemonCenter_2F", "SixIsland_Harbor", "SixIsland_House", "SixIsland_Mart", "SixIsland_WaterPath_House1",
                "SixIsland_WaterPath_House2", "SevenIsland", "SevenIsland_TanobyRuins_MoneanChamber", "SevenIsland_TanobyRuins_LiptooChamber",
                "SevenIsland_TanobyRuins_WeepthChamber", "SevenIsland_TanobyRuins_DilfordChamber", "SevenIsland_TanobyRuins_ScufibChamber",
                "SevenIsland_TanobyRuins_RixyChamber", "SevenIsland_TanobyRuins_ViapoisChamber", "SevenIsland_SevaultCanyon_TanobyKey",
                "SevenIsland_House_Room1", "SevenIsland_Mart", "SevenIsland_PokemonCenter_1F", "SevenIsland_PokemonCenter_2F",
                "SevenIsland_Harbor", "SevenIsland_SevaultCanyon_House", "CeruleanCave_1F", "CeruleanCave_2F", "CeruleanCave_B1F",
            }
        },
        new()
        {
            Id = Johto, Name = "Johto", Generation = 2, File = RegionFiles.Load(Johto),
            // HeartGold's Johto, read by the second DS reader (plan 20 · J2). A new game begun there wakes in the
            // player's room upstairs in New Bark Town with nothing, as HeartGold's does (plan 20 · J4, the chapter); a
            // Champion of Kanto who sails on stands before the player's house (plan 20's decision 1), on the region's
            // own overworld, Johto and Kanto on one grid
            Start = new MapSpot("NewBark_PlayersHouse_2F", 4, 9, Direction.Up),
            Arrival = new MapSpot("Johto", 695, 397, Direction.Down),
            Maps = new[]
            {
                "Johto",
                // Plan 20 · J2: New Bark Town's rooms, Route 29's gate house, Cherrygrove City's rooms and Routes 30 and 31's
                "NewBark_PlayersHouse_1F", "NewBark_PlayersHouse_2F", "NewBark_RivalsHouse_1F", "NewBark_RivalsHouse_2F",
                "NewBark_ElmsLab", "NewBark_ElmsHouse", "NewBark_SouthwestHouse", "Route29_Route46Gate",
                "Cherrygrove_PokemonCenter_1F", "Cherrygrove_PokemonCenter_B1F", "Cherrygrove_Mart", "Cherrygrove_GuideGentsHouse",
                "Cherrygrove_SouthwestHouse", "Cherrygrove_SoutheastHouse",
                "Route30_ApricornHouse", "Route30_MrPokemonsHouse", "Route31_VioletGate",
                // and Violet City's
                "Violet_PokemonCenter_1F", "Violet_PokemonCenter_B1F", "Violet_Mart", "Violet_PokemonSchool",
                "Violet_NorthwestHouse", "Violet_TradeHouse", "Violet_Route36Gate",
                // Plan 20 · J2, part b: Violet's Gym, Sprout Tower, Route 32's Pokémon Center and gate house
                "Violet_Gym", "SproutTower_1F", "SproutTower_2F", "SproutTower_3F",
                "Route32_PokemonCenter_1F", "Route32_PokemonCenter_B1F", "Route32_RuinsOfAlphGate",
                // Plan 20 · J5: the Ruins of Alph's research centre, chambers and gate house to Route 36, Union Cave,
                // Azalea Town's rooms and its gate house to Ilex Forest
                "RuinsOfAlph_ResearchCenter", "RuinsOfAlph_SouthwestChamber", "RuinsOfAlph_NorthwestChamber",
                "RuinsOfAlph_NortheastChamber", "RuinsOfAlph_SoutheastChamber", "Route36_RuinsOfAlphGate",
                "UnionCave_1F", "UnionCave_B1F", "UnionCave_B2F",
                "Azalea_PokemonCenter_1F", "Azalea_PokemonCenter_B1F", "Azalea_Mart", "Azalea_KurtsHouse",
                "Azalea_CharcoalKiln", "Azalea_IlexForestGate",
                // Plan 20 · J5, part b: Ilex Forest, Route 34, Goldenrod City, Routes 35 to 37, the National Park and Ecruteak City
                "IlexForest", "Route34_IlexForestGate", "Route34_DayCare", "Goldenrod_PokemonCenter_1F",
                "Goldenrod_PokemonCenter_B1F", "Goldenrod_FlowerShop", "Goldenrod_BikeShop", "Goldenrod_BillsHouse",
                "Goldenrod_NameRatersHouse", "Goldenrod_FriendshipHouse", "Goldenrod_NortheastHouse", "Goldenrod_Gym",
                "Goldenrod_GameCorner", "Goldenrod_DeptStore_1F", "Goldenrod_DeptStore_2F", "Goldenrod_DeptStore_3F",
                "Goldenrod_DeptStore_4F", "Goldenrod_DeptStore_5F", "Goldenrod_DeptStore_6F", "Goldenrod_RadioTower_1F",
                "Goldenrod_Underground", "Route35_GoldenrodGate", "Route35_NationalParkGate", "NationalPark",
                "Route36_NationalParkGate", "Ecruteak_PokemonCenter_1F", "Ecruteak_PokemonCenter_B1F", "Ecruteak_Mart",
                "Ecruteak_DanceTheater", "Ecruteak_SouthwestHouse", "Ecruteak_DowsingHouse", "Ecruteak_BellTowerGate",
                "BurnedTower_1F", "BurnedTower_B1F",
                // Plan 20 · J7: the Slowpoke Well, the Azalea and Ecruteak Gyms and the Department Store's lift
                "SlowpokeWell_Entrance", "SlowpokeWell_B1F", "SlowpokeWell_B2F", "Azalea_GymEntrance", "Azalea_Gym",
                "Ecruteak_Gym", "Goldenrod_DeptStore_Elevator",
                // Plan 20 · J8: the west and the sea
                "Route38_EcruteakGate", "Route39_MoomooFarmHouse", "Route39_MoomooFarmStable", "Olivine_PokemonCenter_1F",
                "Olivine_PokemonCenter_B1F", "Olivine_Mart", "Olivine_Cafe", "Olivine_Gym", "Olivine_NorthHouse",
                "Olivine_NortheastHouse", "Olivine_NorthwestHouse", "Olivine_PortHall", "OlivineLighthouse_1F",
                "OlivineLighthouse_2F", "OlivineLighthouse_3F", "OlivineLighthouse_4F", "OlivineLighthouse_5F",
                "OlivineLighthouse_Ledge", "OlivineLighthouse_LightRoom", "Route40_FrontierGate", "Cianwood_PokemonCenter_1F",
                "Cianwood_PokemonCenter_B1F", "Cianwood_Gym", "Cianwood_Pharmacy", "Cianwood_KirksHouse", "Cianwood_CentralHouse",
                "Cianwood_NorthHouse", "Cianwood_PhotoStudio", "WhirlIslands_1F", "WhirlIslands_B1F", "WhirlIslands_B2F",
                // Plan 20 · J9: the lighthouse's lift
                "OlivineLighthouse_Elevator",
                // Plan 20 · J8, part b, and J9, part b: Route 47's gate, Cliff Cave and the Safari Zone with its gate
                "CliffEdgeGate", "CliffCave", "SafariZoneGate_PokemonCenter_1F", "SafariZoneGate_PokemonCenter_B1F",
                "SafariZone_Gate", "SafariZone",
                // Plan 20 · J10, part a: the north, from Route 42's gate to the Dragon's Den and the Dark Cave
                "Route42_EcruteakGate", "MtMortar_1F", "MtMortar_1F_Back", "MtMortar_2F", "MtMortar_B1F",
                "Mahogany_PokemonCenter_1F", "Mahogany_PokemonCenter_B1F", "Mahogany_SouvenirShop", "Mahogany_EastHouse",
                "Mahogany_Gym", "Mahogany_Gym_2", "Mahogany_Gym_3", "Route43_MahoganyGate", "Route43_Gate",
                "LakeOfRage_HiddenPowerHouse", "LakeOfRage_FishingGuruHouse",
                "IcePath_1F", "IcePath_B1F", "IcePath_B2F", "IcePath_B3F",
                "Blackthorn_PokemonCenter_1F", "Blackthorn_PokemonCenter_B1F", "Blackthorn_Mart", "Blackthorn_Gym",
                "Blackthorn_WestHouse", "Blackthorn_EastHouse", "Blackthorn_MoveTutorHouse",
                "DragonsDen_Entrance", "DragonsDen", "DragonsDen_Shrine", "DarkCave_Route31Side", "DarkCave_Route45Side",
                // Plan 20 · J10, part b: Routes 26 and 27, Tohjo Falls, the League's gate, Victory Road and the Plateau
                "Route26_NorthHouse", "Route26_WeekSiblingsHouse", "Route27_House", "TohjoFalls", "TohjoFalls_HiddenRoom",
                "Route22_LeagueReceptionGate", "IndigoVictoryRoad_1F", "IndigoVictoryRoad_2F", "IndigoVictoryRoad_3F",
                "IndigoLeague_PokemonCenter_1F", "IndigoLeague_PokemonCenter_B1F", "IndigoLeague_WillRoom",
                "IndigoLeague_KogaRoom", "IndigoLeague_BrunoRoom", "IndigoLeague_KarenRoom", "IndigoLeague_LanceRoom",
                "IndigoLeague_HallOfFame",
                // Plan 20 · J13: the Pokéathlon Dome's square west of Route 35's gate house, and its lobby
                "PokeathlonDome", "PokeathlonDome_1F",
                // Plan 20 · J11, part a: the Team Rocket Hideout under Mahogany's souvenir shop
                "TeamRocketHQ_B1F", "TeamRocketHQ_B2F", "TeamRocketHQ_B3F",
                // Plan 20 · J11, part b: Goldenrod's takeover, the Radio Tower's upper floors and the ways under the city
                "Goldenrod_RadioTower_2F", "Goldenrod_RadioTower_3F", "Goldenrod_RadioTower_4F", "Goldenrod_RadioTower_5F",
                "Goldenrod_RadioTower_ObservationDeck", "Goldenrod_RadioTower_Elevator", "Goldenrod_Underground_B1F",
                "Goldenrod_Underground_B2F", "Goldenrod_Underground_Warehouse", "Goldenrod_DeptStore_Basement",
                // Plan 20 · J15: Route 28's house, Mt. Silver's Pokémon Center and the cave to the summit
                "Route28_House", "MtSilver_PokemonCenter_1F", "MtSilver_PokemonCenter_B1F", "MtSilverCave_1F", "MtSilverCave_UpperMountainside", "MtSilverCave_LowerMountainside", "MtSilverCave_ExpertBeltChamber", "MtSilverCave_MoltresChamber", "MtSilverCave_2F", "MtSilverCave_3F", "MtSilverCave_Summit",
                // Plan 20 · J15, part b: the S.S. Aqua's voyage and Vermilion City's rooms
                "SSAqua_OlivinePort", "SSAqua_1F", "SSAqua_VermilionPort", "Vermilion_PortHall", "Vermilion_PokemonCenter_1F", "Vermilion_PokemonCenter_B1F", "Vermilion_Mart", "Vermilion_FishingGuruHouse", "Vermilion_FanClub", "Vermilion_CentralHouse", "Vermilion_SouthHouse", "Vermilion_Gym",
                // Plan 20 · J15, part c: the Magnet Train's stations, Route 6's gate and Saffron City's rooms
                "Goldenrod_MagnetTrainStation", "Saffron_MagnetTrainStation", "Route6_SaffronGate", "Saffron_PokemonCenter_1F", "Saffron_PokemonCenter_2F", "Saffron_Mart", "Saffron_FightingDojo", "Saffron_MrPsychicHouse", "Saffron_CopycatHouse_1F", "Saffron_CopycatHouse_2F", "Saffron_Gym",
                // Plan 20 · J15, part d: Routes 7 and 8's gates, Celadon City's rooms and Gym, Lavender Town's rooms
                "Route7_SaffronGate", "Route8_SaffronGate", "Celadon_PokemonCenter_1F", "Celadon_PokemonCenter_B1F", "Celadon_DeptStore_1F", "Celadon_Condominiums_1F", "Celadon_Restaurant", "Celadon_Gym",
                "Lavender_PokemonCenter_1F", "Lavender_PokemonCenter_B1F", "Lavender_Mart", "Lavender_NameRater", "Lavender_SouthwestHouse", "Lavender_VolunteerHouse", "Lavender_HouseOfMemories",
                // Plan 20 · J15, part e: Route 5's gate and house, Cerulean City's rooms and Gym, Route 25's Sea Cottage
                "Route5_SaffronGate", "Route5_House", "Cerulean_PokemonCenter_1F", "Cerulean_PokemonCenter_B1F", "Cerulean_Mart", "Cerulean_NorthHouse",
                "Cerulean_EastHouse", "Cerulean_WestHouse", "Cerulean_BikeManiacHouse", "Cerulean_Gym", "CeruleanCape_SeaCottage",
            }
        },
        new()
        {
            Id = Hoenn, Name = "Hoenn", Generation = 3, File = RegionFiles.Load(Hoenn),
            // Emerald's Hoenn, read by the GBA reader (plan 21 · H1). A new game begun there steps out of the moving van
            // beside the west house of Littleroot Town with nothing (plan 21 · H2: Emerald's dynamic warp out of the
            // truck, 3, 10 of the town), and the van's ride is the town's arrival; a Champion come across stands before
            // the family's door, Emerald's spot for a white-out at home
            Start = new MapSpot("Hoenn", 123, 292, Direction.Right),
            Arrival = new MapSpot("Hoenn", 125, 291, Direction.Down),
            Maps = new[]
            {
                "Hoenn",
                // Plan 21 · H1: Littleroot Town's rooms, Oldale Town's and Petalburg City's, each a matrix of the import
                // built from its layout
                "LittlerootTown_BrendansHouse_1F", "LittlerootTown_BrendansHouse_2F", "LittlerootTown_MaysHouse_1F",
                "LittlerootTown_MaysHouse_2F", "LittlerootTown_ProfessorBirchsLab",
                "OldaleTown_House1", "OldaleTown_House2", "OldaleTown_PokemonCenter_1F", "OldaleTown_PokemonCenter_2F", "OldaleTown_Mart",
                "PetalburgCity_WallysHouse", "PetalburgCity_House1", "PetalburgCity_House2",
                "PetalburgCity_PokemonCenter_1F", "PetalburgCity_PokemonCenter_2F", "PetalburgCity_Mart",
                // Plan 21 · H2: Petalburg's Gym, its hall at the door
                "PetalburgCity_Gym",
                // Plan 21 · H2, part b: Petalburg Woods, Rusturf Tunnel, and the rooms of Route 104, Rustboro City and
                // Route 116
                "PetalburgWoods", "RusturfTunnel",
                "RustboroCity_DevonCorp_1F", "RustboroCity_DevonCorp_2F", "RustboroCity_DevonCorp_3F", "RustboroCity_Gym",
                "RustboroCity_PokemonSchool", "RustboroCity_PokemonCenter_1F", "RustboroCity_PokemonCenter_2F", "RustboroCity_Mart",
                "RustboroCity_Flat1_1F", "RustboroCity_Flat1_2F", "RustboroCity_House1", "RustboroCity_CuttersHouse",
                "RustboroCity_House2", "RustboroCity_Flat2_1F", "RustboroCity_Flat2_2F", "RustboroCity_Flat2_3F",
                "RustboroCity_House3", "Route104_MrBrineysHouse", "Route104_PrettyPetalFlowerShop", "Route116_TunnelersRestHouse",
                // Plan 21 · H3: Dewford Town's rooms, Granite Cave, Route 109's seashore house and Slateport City's rooms
                "DewfordTown_House1", "DewfordTown_PokemonCenter_1F", "DewfordTown_PokemonCenter_2F", "DewfordTown_Gym",
                "DewfordTown_Hall", "DewfordTown_House2", "GraniteCave_1F", "GraniteCave_B1F", "GraniteCave_B2F",
                "GraniteCave_StevensRoom", "Route109_SeashoreHouse", "SlateportCity_SternsShipyard_1F",
                "SlateportCity_SternsShipyard_2F", "SlateportCity_BattleTentLobby", "SlateportCity_NameRatersHouse",
                "SlateportCity_PokemonFanClub", "SlateportCity_OceanicMuseum_1F", "SlateportCity_OceanicMuseum_2F",
                "SlateportCity_Harbor", "SlateportCity_House", "SlateportCity_PokemonCenter_1F",
                "SlateportCity_PokemonCenter_2F", "SlateportCity_Mart",
                // Plan 21 · H3, part b: the rooms of Route 110, Mauville City, Route 117 and Verdanturf Town
                "Route110_SeasideCyclingRoadSouthEntrance", "Route110_SeasideCyclingRoadNorthEntrance",
                "Route110_TrickHouseEntrance", "Route110_TrickHouseEnd", "Route110_TrickHousePuzzle1",
                "MauvilleCity_Gym", "MauvilleCity_BikeShop", "MauvilleCity_House1", "MauvilleCity_House2",
                "MauvilleCity_PokemonCenter_1F", "MauvilleCity_PokemonCenter_2F", "MauvilleCity_Mart",
                "Route117_PokemonDayCare", "VerdanturfTown_BattleTentLobby", "VerdanturfTown_Mart",
                "VerdanturfTown_PokemonCenter_1F", "VerdanturfTown_PokemonCenter_2F", "VerdanturfTown_WandasHouse",
                "VerdanturfTown_FriendshipRatersHouse", "VerdanturfTown_House",
                // Plan 21 · H3, part c, and H4, part a: Mauville's Game Corner, Fiery Path, Meteor Falls' first room and the rooms
                // of Routes 111 to 114 and Fallarbor Town
                "MauvilleCity_GameCorner", "FieryPath", "MeteorFalls_1F_1R", "Route111_WinstrateFamilysHouse",
                "Route111_OldLadysRestStop", "Route112_CableCarStation", "Route113_GlassWorkshop", "Route114_FossilManiacsHouse",
                "Route114_FossilManiacsTunnel", "Route114_LanettesHouse", "FallarborTown_Mart", "FallarborTown_BattleTentLobby",
                "FallarborTown_PokemonCenter_1F", "FallarborTown_PokemonCenter_2F", "FallarborTown_CozmosHouse",
                "FallarborTown_MoveRelearnersHouse",
                // Plan 21 · H4, part b: Meteor Falls' other rooms, Mt. Chimney, Jagged Pass, Lavaridge Town's rooms and
                // the Mirage Tower
                "MeteorFalls_1F_2R", "MeteorFalls_B1F_1R", "MeteorFalls_B1F_2R", "MtChimney", "MtChimney_CableCarStation",
                "JaggedPass", "LavaridgeTown_HerbShop", "LavaridgeTown_Gym_1F", "LavaridgeTown_Gym_B1F", "LavaridgeTown_House",
                "LavaridgeTown_Mart", "LavaridgeTown_PokemonCenter_1F", "LavaridgeTown_PokemonCenter_2F",
                "MirageTower_1F", "MirageTower_2F", "MirageTower_3F", "MirageTower_4F",
                // Plan 21 · H5: the sea floor under Routes 124, 126, 127 and 128 (Emerald's matrix of their underwater
                // maps), laid tile for tile under the sea it is dived to from
                "HoennUnderwater",
                // Plan 21 · H6, part a: Route 119's house and Weather Institute, Fortree City's rooms and Winona's Gym
                "Route119_House", "Route119_WeatherInstitute_1F", "Route119_WeatherInstitute_2F", "FortreeCity_House1",
                "FortreeCity_Gym", "FortreeCity_PokemonCenter_1F", "FortreeCity_PokemonCenter_2F", "FortreeCity_Mart",
                "FortreeCity_House2", "FortreeCity_House3", "FortreeCity_House4", "FortreeCity_House5",
                // Plan 21 · H6, part b: Route 121's gate, the Safari Zone and its rest house, the Ancient Tomb and the
                // Scorched Slab
                "Route121_SafariZoneEntrance", "HoennSafariZone", "HoennSafariZone_RestHouse", "AncientTomb", "ScorchedSlab",
                // Plan 21 · H7, part a: Lilycove City's rooms, Route 123's Berry Master's house and Mt. Pyre
                "LilycoveCity_PokemonCenter_1F", "LilycoveCity_PokemonCenter_2F", "LilycoveCity_DepartmentStore_1F",
                "LilycoveCity_DepartmentStore_2F", "LilycoveCity_DepartmentStore_3F", "LilycoveCity_DepartmentStore_4F",
                "LilycoveCity_DepartmentStore_5F", "LilycoveCity_DepartmentStoreRooftop", "LilycoveCity_CoveLilyMotel_1F",
                "LilycoveCity_CoveLilyMotel_2F", "LilycoveCity_LilycoveMuseum_1F", "LilycoveCity_LilycoveMuseum_2F",
                "LilycoveCity_PokemonTrainerFanClub", "LilycoveCity_MoveDeletersHouse", "LilycoveCity_House1", "LilycoveCity_House2",
                "LilycoveCity_House3", "LilycoveCity_House4", "Route123_BerryMastersHouse", "MtPyre_1F", "MtPyre_2F", "MtPyre_3F",
                "MtPyre_4F", "MtPyre_5F", "MtPyre_6F", "MtPyre_Exterior", "MtPyre_Summit",
                // Plan 21 · H7, part b: Team Magma's and Team Aqua's hideouts
                "MagmaHideout_1F", "MagmaHideout_2F_1R", "MagmaHideout_2F_2R", "MagmaHideout_2F_3R", "MagmaHideout_3F_1R",
                "MagmaHideout_3F_2R", "MagmaHideout_3F_3R", "MagmaHideout_4F", "AquaHideout_1F", "AquaHideout_B1F", "AquaHideout_B2F",
                // Plan 21 · H8, part a: Mossdeep City's rooms and the treasure hunter's house on Route 124
                "MossdeepCity_Gym", "MossdeepCity_House1", "MossdeepCity_House2", "MossdeepCity_House3", "MossdeepCity_House4",
                "MossdeepCity_PokemonCenter_1F", "MossdeepCity_PokemonCenter_2F", "MossdeepCity_Mart", "MossdeepCity_StevensHouse",
                "MossdeepCity_SpaceCenter_1F", "MossdeepCity_SpaceCenter_2F", "Route124_DivingTreasureHuntersHouse",
                // Plan 21 · H8, part b: the Seafloor Cavern with the pool under the sea that leads into it, Sootopolis
                // City with the floor under it and its rooms, and the Cave of Origin
                "Underwater_SeafloorCavern", "SeafloorCavern_Entrance", "SeafloorCavern_Room1", "SeafloorCavern_Room2",
                "SeafloorCavern_Room3", "SeafloorCavern_Room4", "SeafloorCavern_Room5", "SeafloorCavern_Room6",
                "SeafloorCavern_Room7", "SeafloorCavern_Room8", "SeafloorCavern_Room9",
                "SootopolisCity", "Underwater_SootopolisCity", "SootopolisCity_PokemonCenter_1F", "SootopolisCity_PokemonCenter_2F",
                "SootopolisCity_Mart", "SootopolisCity_House1", "SootopolisCity_House2", "SootopolisCity_House3",
                "SootopolisCity_House4", "SootopolisCity_House5", "SootopolisCity_House6", "SootopolisCity_House7",
                "SootopolisCity_LotadAndSeedotHouse", "CaveOfOrigin_Entrance", "CaveOfOrigin_1F", "CaveOfOrigin_B1F",
                // Plan 21 · H8, part c: Juan's Gym, Pacifidlog Town's rooms and Sky Pillar
                "SootopolisCity_Gym_1F", "SootopolisCity_Gym_B1F",
                "PacifidlogTown_PokemonCenter_1F", "PacifidlogTown_PokemonCenter_2F", "PacifidlogTown_House1",
                "PacifidlogTown_House2", "PacifidlogTown_House3", "PacifidlogTown_House4", "PacifidlogTown_House5",
                "SkyPillar_Outside", "SkyPillar_Entrance", "SkyPillar_1F", "SkyPillar_2F", "SkyPillar_3F", "SkyPillar_4F",
                "SkyPillar_5F", "SkyPillar_Top",
                // Plan 21 · H8, part d: Shoal Cave and its tide
                "ShoalCave_LowTideEntranceRoom", "ShoalCave_LowTideInnerRoom", "ShoalCave_LowTideStairsRoom",
                "ShoalCave_LowTideLowerRoom", "ShoalCave_LowTideIceRoom",
                // Plan 21 · H9: Ever Grande City's rooms, Victory Road and the Pokémon League
                "EverGrandeCity_PokemonCenter_1F", "EverGrandeCity_PokemonCenter_2F", "EverGrandeCity_PokemonLeague_1F",
                "EverGrandeCity_PokemonLeague_2F", "EverGrandeCity_Hall5", "EverGrandeCity_SidneysRoom", "EverGrandeCity_Hall1",
                "EverGrandeCity_PhoebesRoom", "EverGrandeCity_Hall2", "EverGrandeCity_GlaciasRoom", "EverGrandeCity_Hall3",
                "EverGrandeCity_DrakesRoom", "EverGrandeCity_Hall4", "EverGrandeCity_ChampionsRoom", "EverGrandeCity_HallOfFame",
                "HoennVictoryRoad_1F", "HoennVictoryRoad_B1F", "HoennVictoryRoad_B2F",
                // Plan 21 · H9, part b: Lilycove City's harbour, where the ship on to Unova sails from
                "LilycoveCity_Harbor",
                // Plan 21 · H10: the secret bases' twenty-four rooms (Models/SecretBase.cs)
                "SecretBase_RedCave1", "SecretBase_BrownCave1", "SecretBase_BlueCave1", "SecretBase_YellowCave1", "SecretBase_Tree1", "SecretBase_Shrub1",
                "SecretBase_RedCave2", "SecretBase_BrownCave2", "SecretBase_BlueCave2", "SecretBase_YellowCave2", "SecretBase_Tree2", "SecretBase_Shrub2",
                "SecretBase_RedCave3", "SecretBase_BrownCave3", "SecretBase_BlueCave3", "SecretBase_YellowCave3", "SecretBase_Tree3", "SecretBase_Shrub3",
                "SecretBase_RedCave4", "SecretBase_BrownCave4", "SecretBase_BlueCave4", "SecretBase_YellowCave4", "SecretBase_Tree4", "SecretBase_Shrub4",
                // Plan 21 · H11: Lilycove City's Contest Lobby and Contest Hall
                "LilycoveCity_ContestLobby", "LilycoveCity_ContestHall",
                // Plan 21 · H12, part a: the Regis and Southern Island
                "Underwater_Route134", "Underwater_SealedChamber", "SealedChamber_OuterRoom", "SealedChamber_InnerRoom", "DesertRuins", "IslandCave",
                "SouthernIsland_Exterior", "SouthernIsland_Interior",
                // Plan 21 · H12, part b: the Battle Frontier island and the rooms of its grounds
                "HoennBattleFrontier", "BattleFrontier_ReceptionGate", "BattleFrontier_PokemonCenter_1F", "BattleFrontier_PokemonCenter_2F",
                "BattleFrontier_Mart", "BattleFrontier_ExchangeServiceCorner", "BattleFrontier_ScottsHouse", "BattleFrontier_RankingHall",
                "BattleFrontier_Lounge1", "BattleFrontier_Lounge2", "BattleFrontier_Lounge3", "BattleFrontier_Lounge4", "BattleFrontier_Lounge5",
                "BattleFrontier_Lounge6", "BattleFrontier_Lounge7", "BattleFrontier_Lounge8",
                // Plan 21 · H12, part c: Steven's cave
                "MeteorFalls_StevensCave",
                // Plan 21 · H12, part d: New Mauville and the Desert Underpass
                "NewMauville_Entrance", "NewMauville_Inside", "DesertUnderpass",
                // Plan 21 · H12, part e: the Marine Cave, Terra Cave and the sea floors of Routes 105, 125 and 129
                "Underwater_Route129", "Underwater_Route105", "Underwater_Route125", "Underwater_MarineCave",
                "MarineCave_Entrance", "MarineCave_End", "TerraCave_Entrance", "TerraCave_End",
                // Plan 21 · H12, part f: Birth Island and Navel Rock
                "BirthIsland_Harbor", "BirthIsland_Exterior", "NavelRock_Harbor", "NavelRock_Exterior", "NavelRock_Entrance", "NavelRock_B1F",
                "NavelRock_Fork", "NavelRock_Up1", "NavelRock_Up2", "NavelRock_Up3", "NavelRock_Up4", "NavelRock_Top",
                "NavelRock_Down01", "NavelRock_Down02", "NavelRock_Down03", "NavelRock_Down04", "NavelRock_Down05", "NavelRock_Down06",
                "NavelRock_Down07", "NavelRock_Down08", "NavelRock_Down09", "NavelRock_Down10", "NavelRock_Down11", "NavelRock_Bottom",
                // Plan 21 · H12, part g: Faraway Island and the S.S. Tidal
                "FarawayIsland_Entrance", "FarawayIsland_Interior", "SSTidalCorridor", "SSTidalLowerDeck", "SSTidalRooms",
            }
        },
        new()
        {
            Id = Unova, Name = "Unova", Generation = 5, File = RegionFiles.Load(Unova),
            // Black 2's Unova, read by the third DS reader (plan 22 · U2). A new game begun there wakes in the player's
            // house in Aspertia City (its story is plan 22 · U4's); a Champion come across stands before the house's door
            Start = new MapSpot("AspertiaCity_PlayersHouse", 14, 5, Direction.Down),
            Arrival = new MapSpot("Unova", 47, 762, Direction.Down),
            Maps = new[]
            {
                "Unova",
                // Plan 22 · U2: Aspertia City's rooms, its gate to Route 19, its school and the Gym behind it, Floccesy
                // Town's rooms and the ranch's field, each a matrix of the import
                "AspertiaCity_PlayersHouse", "AspertiaCity_House1_1F", "AspertiaCity_House1_2F", "AspertiaCity_House2_1F",
                "AspertiaCity_House2_2F", "AspertiaCity_House3_1F", "AspertiaCity_House3_2F", "AspertiaCity_TrainerSchool",
                "AspertiaCity_Gym", "AspertiaCity_PokemonCenter", "AspertiaGate",
                "FloccesyTown_AldersHouse", "FloccesyTown_House1", "FloccesyTown_House2", "FloccesyTown_PokemonCenter",
                "FloccesyRanch",
                // Plan 22 · U5, part b: Virbank City's gate, rooms, ship terminal and club, the gate to Pokéstar Studios
                // and the Virbank Complex
                "VirbankGate", "VirbankCity_House1", "VirbankCity_House2", "VirbankCity_House3", "VirbankCity_Terminal",
                "VirbankCity_Gym", "VirbankCity_GymStage", "VirbankCity_PokemonCenter", "PokestarGate", "VirbankComplex",
                // Plan 22 · U5, part c: Castelia City, its avenue a map of ours (Data/maps/CasteliaCity.json, Black 2's
                // standing on rails), its piers, streets, plaza and rooms the import's, the gate to Route 4 and the sewers
                "CasteliaCity", "CasteliaCity_PrimePier", "CasteliaCity_ThumbPier", "CasteliaCity_CasteliaStreet",
                "CasteliaCity_ModeStreet", "CasteliaCity_NarrowStreet", "CasteliaCity_GymStreet", "CasteliaCity_Plaza",
                "CasteliaCity_NorthStreet", "CasteliaCity_PokemonCenter", "CasteliaCity_Gym",
                "CasteliaGate", "CasteliaSewers",
                // Plan 22 · U6: Route 4's houses of flats, Join Avenue, the Desert Resort's gate and the desert, and
                // Nimbasa City's Pokémon Center, amusement park and Gym
                "Route4_House1_1F", "Route4_House1_2F", "Route4_RestHouse", "Route4_House2_1F", "Route4_House2_2F",
                "Route4_House3_1F", "Route4_House3_2F", "Route4_House4_1F", "Route4_House4_2F", "JoinAvenue",
                "DesertResortGate", "DesertResort", "NimbasaCity_PokemonCenter", "NimbasaCity_AmusementPark", "NimbasaCity_Gym",
                // Plan 22 · U6, part b: Nimbasa City's other rooms, its gate to Route 5, Route 5's trailer, the Driftveil
                // Drawbridge, Driftveil City's rooms and Clay's Gym (a map of ours, Data/maps/DriftveilCity_Gym.json)
                "NimbasaCity_BigStadium", "NimbasaCity_BigStadiumStands", "NimbasaCity_SmallCourt", "NimbasaCity_SmallCourtStands",
                "NimbasaCity_House1_1F", "NimbasaCity_House1_2F", "NimbasaCity_House2_1F", "NimbasaCity_House2_2F",
                "NimbasaCity_BattleInstitute", "MusicalTheater", "NimbasaCity_RollerCoaster", "NimbasaGate_Route5", "Route5_Trailer",
                "DriftveilDrawbridge", "DriftveilCity_PokemonCenter", "DriftveilCity_House1", "DriftveilCity_House2",
                "DriftveilCity_House3", "DriftveilCity_House4", "DriftveilCity_PlasmaHouse", "DriftveilCity_Market",
                "DriftveilCity_TournamentGate", "DriftveilCity_GymLobby", "DriftveilCity_Gym",
                // Plan 22 · U6, part c: the gate to Route 16, the Lostlorn Forest, the Gear Station (a map of ours,
                // Data/maps/GearStation.json) and Anville Town at the end of its line, the Relic Passage, the Relic
                // Castle and its buried halls, and the Plasma Frigate's deck
                "NimbasaGate_Route16", "LostlornForest", "GearStation", "AnvilleTown", "AnvilleTown_House1",
                "AnvilleTown_House2", "AnvilleTown_House3", "RelicPassage_East", "RelicPassage", "RelicPassage_West",
                "RelicCastle", "RelicCastle_B1F", "RelicCastle_Ruins1", "RelicCastle_Ruins2", "RelicCastle_Ruins3",
                "RelicCastle_Ruins4", "RelicCastle_Ruins5", "RelicCastle_Depths", "PlasmaFrigate",
                // Plan 22 · U6, part d: a car of the Battle Subway (a map of ours, Data/maps/BattleSubwayTrain.json)
                "BattleSubwayTrain",
                // and the Pokémon World Tournament's hall (a map of ours, Data/maps/PokemonWorldTournament.json)
                "PokemonWorldTournament",
                // Plan 22 · U7, part a: Route 6's Season Research Lab and house, Mistralton Cave, Chargestone Cave's three
                // floors, Mistralton City's rooms and Gym, Route 7's houses and the Celestial Tower
                "Route6_SeasonLab", "Route6_House", "MistraltonCave", "ChargestoneCave",
                "ChargestoneCave_Heart", "ChargestoneCave_Depths", "MistraltonCity_PokemonCenter", "MistraltonCity_Greenhouse",
                "MistraltonCity_CargoHall", "MistraltonCity_House", "MistraltonCity_Gym", "Route7_House1", "Route7_House2", "LentimasTown_PokemonCenter", "LentimasTown_House1", "LentimasTown_House2",
                "CelestialTower_1F", "CelestialTower_2F", "CelestialTower_3F", "CelestialTower_4F", "CelestialTower_Summit",
                // Plan 22 · U7, part b: the Strange House's six rooms, Reversal Mountain's six floors, Undella Town's Pokémon
                // Center, two houses and gate, the two houses of Route 13 and Lacunosa Town's four houses and Pokémon Center
                "StrangeHouse_Hall", "StrangeHouse_Library1", "StrangeHouse_Rooms", "StrangeHouse_Library2", "StrangeHouse_Attic",
                "StrangeHouse_Cellar", "ReversalMountain_1", "ReversalMountain_2", "ReversalMountain_3", "ReversalMountain_4",
                "ReversalMountain_5", "ReversalMountain_6", "UndellaTown_PokemonCenter", "UndellaTown_House1", "UndellaTown_House2",
                "UndellaGate", "Route13_House1", "Route13_House2", "LacunosaTown_House1", "LacunosaTown_House2",
                "LacunosaTown_House3", "LacunosaTown_House4", "LacunosaTown_PokemonCenter",
                // Plan 22 · U8, part a: the gate houses either side of the Village Bridge and Opelucid City, the bridge, Route 11's hut,
                // Opelucid City's five houses of two floors, Pokémon Center and Gym, and Route 9's mall
                "VillageBridgeGate_West", "VillageBridgeGate_East", "OpelucidGate_East", "OpelucidGate_West", "VillageBridge", "Route11_Hut", "OpelucidCity_DraydensHouse_1F", "OpelucidCity_DraydensHouse_2F", "OpelucidCity_TripleHouse_1F", "OpelucidCity_TripleHouse_2F", "OpelucidCity_MusicalHouse_1F", "OpelucidCity_MusicalHouse_2F", "OpelucidCity_TradeHouse_1F", "OpelucidCity_TradeHouse_2F", "OpelucidCity_OldHouse_1F", "OpelucidCity_OldHouse_2F", "OpelucidCity_PokemonCenter", "OpelucidCity_Gym", "ShoppingMallNine_1F", "ShoppingMallNine_2F",
                // Plan 22 · U8, part b: the Marine Tube (a map of ours, Data/maps/MarineTube.json) between its gates, Humilau City's
                // rooms and Gym, the Seaside Cave and five rooms of the Plasma Frigate
                "MarineTubeGate_Undella", "MarineTube", "MarineTubeGate_Humilau", "HumilauCity_House1", "HumilauCity_House2", "HumilauCity_House3", "HumilauCity_House4", "HumilauCity_House5", "HumilauCity_PokemonCenter", "HumilauCity_Gym", "SeasideCave_1F", "SeasideCave_B1F", "PlasmaFrigate_Hold", "PlasmaFrigate_Stern", "PlasmaFrigate_Bridge", "PlasmaFrigate_Cabins", "PlasmaFrigate_Cabin",
                // Plan 22 · U9, part a: the Giant Chasm's cave, crater (twice), passage and cavern, and the Frigate's ramp hall
                "GiantChasm_Cave", "GiantChasm_Crater", "GiantChasm_CraterFrigate", "GiantChasm_Passage", "GiantChasm_Cavern", "PlasmaFrigate_Ramp",
                // Plan 22 · U9, part b: Route 23's rooms, Victory Road's foot, floors and Pokémon Center, and the League's plaza and Pokémon Center
                "Route23_House1", "Route23_House2", "UnovaVictoryRoad", "UnovaVictoryRoad_PokemonCenter", "UnovaVictoryRoad_Ruins", "UnovaVictoryRoad_Grove", "UnovaVictoryRoad_Falls", "UnovaVictoryRoad_Cave", "UnovaVictoryRoad_Pool", "UnovaVictoryRoad_Summit", "UnovaVictoryRoad_Hollow", "UnovaVictoryRoad_Tunnel", "UnovaPokemonLeague", "UnovaPokemonLeague_PokemonCenter",
                // Plan 22 · U9, part c: the League's hall, the Elite Four's rooms, the way down, the Champion's room and the Hall of Fame
                "UnovaPokemonLeague_Hall", "UnovaPokemonLeague_ShauntalsRoom", "UnovaPokemonLeague_GrimsleysRoom", "UnovaPokemonLeague_MarshalsRoom", "UnovaPokemonLeague_CaitlinsRoom", "UnovaPokemonLeague_Descent", "UnovaPokemonLeague_ChampionsRoom", "UnovaPokemonLeague_HallOfFame",
            }
        },
        new() { Id = Kalos, Name = "Kalos", Generation = 6 },
        new() { Id = Alola, Name = "Alola", Generation = 7 },
        new() { Id = Galar, Name = "Galar", Generation = 8 },
        new() { Id = Paldea, Name = "Paldea", Generation = 9 }
    };

    private static readonly RegionLink[] links =
    {
        // Sinnoh first: the way on from its Hall of Fame, by ship from Canalave's pier, where W8 makes Sinnoh's harbour.
        // Its captain stands at the end of the pier beside Sailor Eldritch once the Hall of Fame is entered (plan 18 · W4,
        // part b: Canalave's overlay, common.Attendant on the map of Sinnoh); plan 18 · W5 gives the crossing its scene.
        new() { From = Sinnoh, To = Kanto, Transport = Transport.Boat, Vessel = "ship", DepartureMap = "Sinnoh" },
        // As in HeartGold and SoulSilver, but the other way round: the ship sails from Vermilion to Olivine.
        // Its captain stands on the quay in Vermilion City (plan 19 · K8: Vermilion's overlay, common.Attendant on the map of Kanto).
        new() { From = Kanto, To = Johto, Transport = Transport.Boat, Vessel = "ship", DepartureMap = "Kanto" },
        // From Johto by sea too: its sailor stands before the quay in Olivine's port hall (plan 20 · J12: the hall's overlay,
        // common.Attendant), where HeartGold's S.S. Aqua leaves from; a ferry, as plan 18's table has it
        new() { From = Johto, To = Hoenn, Transport = Transport.Boat, Vessel = "ferry", DepartureMap = "Olivine_PortHall" },
        // From Hoenn by sea as well: the captain stands on the quay of Lilycove City's harbour once Hoenn's Hall of Fame is
        // entered (plan 21 · H9, part b: the harbour's overlay, common.Attendant), where Emerald's ferries leave from
        new() { From = Hoenn, To = Unova, Transport = Transport.Boat, Vessel = "ship", DepartureMap = "LilycoveCity_Harbor" },
        // The later regions by air, as the table proposes; their plans (23) name where they leave from
        // From Unova by air: the airline's attendant stands in the east wing of Mistralton City's cargo hall once Unova's Hall of
        // Fame is entered (plan 22 · U9, part c: the hall's overlay, common.Attendant)
        new() { From = Unova, To = Kalos, Transport = Transport.Plane, Vessel = "plane", DepartureMap = "MistraltonCity_CargoHall" },
        new() { From = Kalos, To = Alola, Transport = Transport.Plane, Vessel = "plane" },
        new() { From = Alola, To = Galar, Transport = Transport.Plane, Vessel = "plane" },
        new() { From = Galar, To = Paldea, Transport = Transport.Plane, Vessel = "plane" }
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
    /// <summary>
    /// The sound the dark of a crossing opens with (plan 18 · W5): a plane's engine, or a ship's horn for whatever sails
    /// (a ship, a ferry, and a way still undecided).
    /// </summary>
    public static string CrossingSound(RegionLink link) => link.VesselWord == "plane" ? "crossing_engine" : "crossing_horn";

    /// <summary>The one line said over the dark of a crossing (plan 18 · W5), by the vessel, naming where it goes.</summary>
    public static string VoyageLine(RegionLink link)
    {
        string word = link.VesselWord;
        return Core.Text.Of($"crossing.voyage.{word}", ("to", Get(link.To)!.Name));
    }

    public static List<string> AttendantLines(RegionLink link, TravelCheck check)
    {
        // The string table's attendant words (plan 17), the vessel's word among them
        string from = Get(link.From)!.Name;
        string to = Get(link.To)!.Name;
        string word = link.VesselWord;
        string vessel = Core.Text.Of($"attendant.vessel.{word}");
        return check switch
        {
            TravelCheck.StoryUnfinished => new()
            {
                Core.Text.Of("attendant.goes", ("vessel", vessel), ("to", to)),
                Core.Text.Of("attendant.championsOnly", ("from", from))
            },
            TravelCheck.DestinationNotBuilt => new()
            {
                Core.Text.Of("attendant.congratulations", ("from", from)),
                Core.Text.Of("attendant.notReady", ("vessel", vessel), ("to", to))
            },
            _ => new()
            {
                Core.Text.Of("attendant.ready", ("from", from), ("vessel", vessel), ("to", to)),
                // Under option B the whole team crosses (plan 18 · W4, part b), and the attendant says so before anyone agrees
                Core.Text.Of("attendant.welcome")
            }
        };
    }
}
