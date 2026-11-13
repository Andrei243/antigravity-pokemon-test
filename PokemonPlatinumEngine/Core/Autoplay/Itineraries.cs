using System.Collections.Generic;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The ways through the story the playthrough bot knows (plan 16 · T13), each a list of <see cref="Leg"/>s in the
/// order a player meets them, written from the chapters' own scripts (the triggers by their scripts' names, the
/// people by their keys). Only what the story asks for is named; the way between is the bot's to find.
/// </summary>
public static partial class Itineraries
{
    /// <summary>The first chapter (plan 02 · S4): from the bedroom to the catching lesson on Route 202.</summary>
    public static IReadOnlyList<Leg> FirstChapter { get; } = new[]
    {
        Leg.Trigger("PlayerHouse2F", "RivalRushesIn"),
        Leg.Trigger("PlayerHouse", "DoorWarning"),
        Leg.Trigger("Sinnoh", "GuitaristStops", "twinleaf_town"),
        Leg.Trigger("Sinnoh", "RivalRunsOut", "twinleaf_town"),
        Leg.Talk("RivalHouse", "rival_mom"),
        Leg.Enter("RivalHouse2F"),
        Leg.Trigger("Sinnoh", "RowanAppears", "route_201"),
        // The briefcase: the choice, the rival's battle, and home to the Running Shoes
        Leg.Talk("Sinnoh", "briefcase", "route_201"),
        Leg.Trigger("Sinnoh", "OffToTheLake", "route_201"),
        Leg.Trigger("Sinnoh", "IntoTheLake", "verity_lakefront"),
        Leg.Trigger("Sinnoh", "MeetAssistant", "sandgem_town"),
        // Sent home from Route 202 first, for Mom's Journal and the parcel; then the lesson
        Leg.Trigger("Sinnoh", "CatchingLesson", "route_202"),
        Leg.Talk("PlayerHouse", "mom"),
        Leg.Trigger("Sinnoh", "CatchingLesson", "route_202"),
    };

    /// <summary>
    /// Kalos's first chapter's opening (plan 23 · Z1; plan 30 · FM9, which plays it in <c>Eight</c> and in <c>Free</c>):
    /// downstairs to Mom, out of the door past the neighbours' scene, up Route 1 to the café's table in Aquacorde Town
    /// (the first Pokémon, the Pokédex and Shauna's battle), and home to Mom for the roller skates.
    /// </summary>
    public static IReadOnlyList<Leg> KalosOpening { get; } = new[]
    {
        Leg.Enter("VanivilleTown_PlayersHouse_1F"),
        Leg.Reach("Kalos", 255, 690),
        Leg.Trigger("Kalos", "Cafe1", "aquacorde_town"),
        Leg.Talk("VanivilleTown_PlayersHouse_1F", "mom"),
    };

    /// <summary>
    /// Alola's first chapter (plan 23 · Z5; played freely with a stick since the user's switch of 2026-11-12): Mom's word
    /// in the new home, the professor at the door, the kahuna away in Iki Town, the bridge on the Mahalo Trail, the
    /// kahuna home with the first Pokémon and Hau's battle, the Pokédex in the laboratory, and Hau'oli City's marina.
    /// </summary>
    public static IReadOnlyList<Leg> AlolaFirstChapter { get; } = new[]
    {
        // (a trigger not yet armed when its map is come to counts as played, so each scene's variable is brought to it by
        // the leg before: the door's scene as the house is left, Hau'oli City's arrival before the marina)
        Leg.Trigger("AlolaRoute1_PlayersHouse", "MomCalls"),
        Leg.Reach("Melemele", 238, 160),
        Leg.Trigger("Melemele", "KahunaAway", "iki_town"),
        Leg.Trigger("Melemele", "Bridge", "mahalo_trail"),
        Leg.Trigger("Melemele", "KahunaHome", "iki_town"),
        Leg.Talk("AlolaRoute1_KukuiLab", "kukui"),
        Leg.Reach("Melemele", 290, 167),
        Leg.Trigger("Melemele", "Marina", "hauoli_city"),
    };

    /// <summary>
    /// The second chapter (plan 02 · S5) to the Coal Badge: Jubilife City, the Trainers' School, the Pokétch campaign,
    /// the rival on Route 203, Oreburgh Gate's hiker, the boy and the rival at the Gym's door, Roark down the mine, a
    /// stretch of the mine trained in, and the Gym.
    /// </summary>
    public static IReadOnlyList<Leg> ToTheCoalBadge { get; } = new[]
    {
        Leg.Trigger("Sinnoh", "FirstArrival", "jubilife_city"),
        Leg.Talk("TrainersSchool", "rival"),
        Leg.Talk("TrainersSchool", "school_kid_harrison"),
        Leg.Talk("TrainersSchool", "school_kid_christine"),
        Leg.Trigger("Sinnoh", "PoketchCampaign", "jubilife_city"),
        Leg.Talk("Sinnoh", "clown_1", "jubilife_city"),
        Leg.Talk("Sinnoh", "clown_2", "jubilife_city"),
        Leg.Talk("Sinnoh", "clown_3", "jubilife_city"),
        Leg.Talk("Sinnoh", "poketch_co_president", "jubilife_city"),
        Leg.Heal("JubilifePokemonCenter"),
        Leg.Trigger("Sinnoh", "Rival", "route_203"),
        Leg.Trigger("OreburghGate1F", "HikerStops", "oreburgh_gate_1f"),
        Leg.Trigger("Sinnoh", "ToTheGym", "oreburgh_city"),
        Leg.Talk("Sinnoh", "rival", "oreburgh_city"),
        Leg.Talk("OreburghMineB2F", "roark"),
        // Roark's Rock types against a lead of at least 14, trained in the mine's first floor if it isn't yet
        Leg.Train(14, "OreburghMineB1F", 8, 10, 16, 10, "OreburghPokemonCenter"),
        Leg.Heal("OreburghPokemonCenter"),
        Leg.Talk("OreburghGym", "youngster_darius"),
        Leg.Heal("OreburghPokemonCenter"),
        Leg.Talk("OreburghGym", "roark") with { Until = story => story.HasBadge("Sinnoh", "Coal") },
    };

    /// <summary>
    /// Kalos's middle chapters (plan 23 · Z1 to Z3, the way Z4's loose ends closed), from where <see cref="KalosOpening"/>
    /// leaves a game (Mom has given the roller skates, <c>VAR_KALOS_STORY</c> 4) to where <see cref="KalosLastChapters"/>
    /// begins (in Shalour City with the Key Stone given): the professor's lesson on Route 2, the friends in Santalune
    /// Forest and on Route 3, the neighbour and Viola, the assistants and the professor in Lumiose City, Lysandre's café,
    /// the friends on Route 5, the castle's keeper, the palace's butler and the Snorlax, the neighbour in Cyllage City and
    /// Grant, Trevor in Geosenge Town, and the Tower of Mastery's three visits round Korrina's Gym.
    /// </summary>
    public static IReadOnlyList<Leg> KalosMiddleChapters { get; } = new[]
    {
        Leg.Trigger("Kalos", "Lesson", "kalos_route_2"),
        Leg.Trigger("Kalos", "Meet", "santalune_forest"),
        Leg.Trigger("Kalos", "Friends", "kalos_route_3"),
        Leg.Trigger("Kalos", "Neighbour", "santalune_city"),
        // (Viola's web: the knot at the first ring's west end shakes the dew onto the strands between the rings, the second
        // ring's back onto those up to her floor, and the second ring's again onto the way back down: SantaluneWeb)
        Leg.Reach("SantaluneCity_Gym", 1, 10),
        Leg.Reach("SantaluneCity_Gym", 1, 7),
        Leg.Talk("SantaluneCity_Gym", "viola") with { Until = story => story.HasBadge("Kalos", "Bug") },
        Leg.Reach("SantaluneCity_Gym", 1, 7),
        Leg.Trigger("Kalos", "Assistants", "lumiose_city"),
        Leg.Talk("LumioseCity_SycamoreLab", "sycamore") with { Until = story => story.Var("VAR_KALOS_STORY") >= 11 },
        Leg.Enter("LumioseCity_LysandreCafe") with { Until = story => story.Var("VAR_KALOS_STORY") >= 12 },
        Leg.Trigger("Kalos", "Friends", "kalos_route_5"),
        Leg.Trigger("Kalos", "Castellan", "camphrier_town"),
        Leg.Talk("Kalos", "butler", "kalos_route_6"),
        Leg.Talk("Kalos", "snorlax", "kalos_route_7"),
        Leg.Trigger("Kalos", "Neighbour", "cyllage_city"),
        Leg.Talk("CyllageCity_Gym", "grant") with { Until = story => story.HasBadge("Kalos", "Cliff") },
        Leg.Trigger("Kalos", "Trevor", "geosenge_town"),
        Leg.Talk("ShalourCity_TowerOfMastery", "gurkinn") with { Until = story => story.Var("VAR_KALOS_STORY") >= 20 },
        // (Korrina's rink: the levers on the two rails' stubs throw the points on the way up, and the levers on her floor
        // and the stubs throw them back on the way down: ShalourRails)
        Leg.Reach("ShalourCity_Gym", 2, 11),
        Leg.Reach("ShalourCity_Gym", 10, 8),
        Leg.Talk("ShalourCity_Gym", "korrina") with { Until = story => story.HasBadge("Kalos", "Rumble") },
        Leg.Reach("ShalourCity_Gym", 10, 3),
        Leg.Reach("ShalourCity_Gym", 10, 8),
        Leg.Reach("ShalourCity_Gym", 2, 11),
        Leg.Talk("ShalourCity_TowerOfMastery", "gurkinn") with { Until = story => story.Var("VAR_KALOS_STORY") >= 22 },
    };

    /// <summary>
    /// Kalos's last chapters (plan 23 · Z4), from where the Tower of Mastery leaves a game (in Coumarine City with three
    /// Badges) to the Hall of Fame: Ramos, the Power Plant, the Prism Tower's lift and Clemont, the neighbour on Route 14,
    /// Valerie, Olympia, the waiter's way down to Lysandre's laboratory, the hidden door under Geosenge's stones, Lysandre
    /// and Xerneas, Wulfric, the neighbour and the guard on Route 21, the Elite Four and Diantha, whose Hall of Fame sends
    /// the player home.
    /// </summary>
    public static IReadOnlyList<Leg> KalosLastChapters { get; } = new[]
    {
        // (the team is seen to at a Pokémon Center before each Gym and the League's doors, as a player would: a long chain of
        // trainers wears even a strong team down)
        Leg.Heal("CoumarineCity_PokemonCenter_1F"),
        // (Ramos's tree: the vine at the top tier's west end lets down the pole up to his platform: CoumarinePoles; Grant's
        // wall needs nothing of the itinerary's, since nothing of it moves)
        Leg.Reach("CoumarineCity_Gym", 1, 4),
        Leg.Talk("CoumarineCity_Gym", "ramos") with { Until = story => story.HasBadge("Kalos", "Plant") },
        Leg.Talk("KalosPowerPlant", "aliana") with { Until = story => story.Var("VAR_KALOS_STORY") >= 24 },
        Leg.Heal("LumioseCity_PokemonCenter_1F"),
        Leg.Talk("Kalos", "lift_operator", "lumiose_city"),
        // (the Prism Tower's three quiz gates, each opened by its host's question: plan 23, Kalos's Gyms)
        Leg.Talk("LumioseCity_PrismTowerGym", "quiz_host_1") with { Until = story => story.Has("FLAG_KALOS_PRISM_GATE_1") },
        Leg.Talk("LumioseCity_PrismTowerGym", "quiz_host_2") with { Until = story => story.Has("FLAG_KALOS_PRISM_GATE_2") },
        Leg.Talk("LumioseCity_PrismTowerGym", "quiz_host_3") with { Until = story => story.Has("FLAG_KALOS_PRISM_GATE_3") },
        Leg.Talk("LumioseCity_PrismTowerGym", "clemont") with { Until = story => story.HasBadge("Kalos", "Voltage") },
        Leg.Trigger("Kalos", "Neighbour", "kalos_route_14"),
        Leg.Heal("LaverreCity_PokemonCenter_1F"),
        // (Valerie's warp panels: the door's room's west panel, then the west room's north-east panel to her own room)
        Leg.Reach("LaverreCity_Gym", 3, 14),
        Leg.Reach("LaverreCity_Gym", 4, 8),
        Leg.Talk("LaverreCity_Gym", "valerie") with { Until = story => story.HasBadge("Kalos", "Fairy") },
        // (and out by her own panel, which puts the player down by the door, as Olympia's and Wulfric's do)
        Leg.Reach("LaverreCity_Gym", 3, 4),
        // (Route 17's drifts are crossed on a Mamoswine from its west end: plan 23, Kalos's Mamoswine ride)
        Leg.Talk("Kalos", "mamoswine_west", "kalos_route_17"),
        Leg.Heal("AnistarCity_PokemonCenter_1F"),
        // (Olympia's pads of light: the middle pad's east panel, the north-east pad's west one, the south-west pad's east one)
        Leg.Reach("AnistarCity_Gym", 7, 9),
        Leg.Reach("AnistarCity_Gym", 10, 7),
        Leg.Reach("AnistarCity_Gym", 2, 10),
        Leg.Talk("AnistarCity_Gym", "olympia") with { Until = story => story.HasBadge("Kalos", "Psychic") },
        Leg.Reach("AnistarCity_Gym", 8, 5),
        Leg.Talk("Kalos", "mamoswine_east", "kalos_route_17"),
        Leg.Heal("LumioseCity_PokemonCenter_1F"),
        Leg.Talk("LumioseCity_LysandreCafe", "waiter"),
        Leg.Talk("LumioseCity_LysandreLabs", "lysandre") with { Until = story => story.Var("VAR_KALOS_STORY") >= 28 },
        Leg.Heal("GeosengeTown_PokemonCenter_1F"),
        Leg.Trigger("Kalos", "HiddenDoor", "geosenge_town"),
        Leg.Talk("GeosengeTown_FlareHQ", "lysandre") with { Until = story => story.Has("FLAG_KALOS_HIDE_LYSANDRE_HQ") },
        Leg.Talk("GeosengeTown_FlareHQ", "xerneas") with { Until = story => story.Var("VAR_KALOS_STORY") >= 29 },
        Leg.Talk("Kalos", "mamoswine_west", "kalos_route_17"),
        Leg.Heal("SnowbelleCity_PokemonCenter_1F"),
        // (Wulfric's turntables: the east one over the door's platform turned twice, its fellow above, the middle one
        // turned twice, by its hubs: SnowbelleTurntables)
        Leg.Reach("SnowbelleCity_Gym", 9, 12),
        Leg.Reach("SnowbelleCity_Gym", 9, 13),
        Leg.Reach("SnowbelleCity_Gym", 9, 12),
        Leg.Reach("SnowbelleCity_Gym", 9, 9),
        Leg.Reach("SnowbelleCity_Gym", 6, 9),
        Leg.Reach("SnowbelleCity_Gym", 6, 10),
        Leg.Reach("SnowbelleCity_Gym", 6, 9),
        Leg.Talk("SnowbelleCity_Gym", "wulfric") with { Until = story => story.HasBadge("Kalos", "Iceberg") },
        Leg.Reach("SnowbelleCity_Gym", 10, 4),
        Leg.Trigger("Kalos", "Neighbour", "kalos_route_21"),
        Leg.Trigger("Kalos", "Gate", "kalos_route_21"),
        Leg.Heal("KalosLeague_Hall"),
        // (the hall's nurse between the rooms, as a player would go back to her: a team worn by three of the Four in a
        // row lost to the fourth once Z4b's trainers had moved the dice, and a white-out sends the player home)
        Leg.Talk("KalosLeague_MalvaRoom", "malva") with { Until = story => story.Has("FLAG_KALOS_DEFEATED_MALVA") },
        Leg.Heal("KalosLeague_Hall"),
        Leg.Talk("KalosLeague_SieboldRoom", "siebold") with { Until = story => story.Has("FLAG_KALOS_DEFEATED_SIEBOLD") },
        Leg.Heal("KalosLeague_Hall"),
        Leg.Talk("KalosLeague_WikstromRoom", "wikstrom") with { Until = story => story.Has("FLAG_KALOS_DEFEATED_WIKSTROM") },
        Leg.Heal("KalosLeague_Hall"),
        Leg.Talk("KalosLeague_DrasnaRoom", "drasna") with { Until = story => story.Has("FLAG_KALOS_DEFEATED_DRASNA") },
        // (her script ends as it takes the player through to the Hall of Fame, whose own arrival sends the player home)
        Leg.Heal("KalosLeague_Hall"),
        Leg.Talk("KalosLeague_ChampionRoom", "diantha") with { Until = story => story.Has("FLAG_KALOS_DEFEATED_DIANTHA") || story.Has("KalosHallOfFame") },
        Leg.Enter("VanivilleTown_PlayersHouse_2F") with { Until = story => story.Has("KalosHallOfFame") },
    };

    /// <summary>A Kalos game played whole by the bot, from the bedroom in Vaniville Town to the Hall of Fame (plan 23 · Z4's "done when").</summary>
    public static IReadOnlyList<Leg> KalosWhole { get; } = Join(KalosOpening, KalosMiddleChapters, KalosLastChapters);

    /// <summary>From a new game's bedroom to the Coal Badge.</summary>
    public static IReadOnlyList<Leg> TwinleafToCoalBadge { get; } = Join(FirstChapter, ToTheCoalBadge);

    private static List<Leg> Join(params IReadOnlyList<Leg>[] parts)
    {
        var all = new List<Leg>();
        foreach (var part in parts) all.AddRange(part);
        return all;
    }
}
