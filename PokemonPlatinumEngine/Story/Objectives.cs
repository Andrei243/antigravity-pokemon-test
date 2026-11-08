using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// One thing the story asks of the player next (plan 12 · Q14): a name (the key of its line in <c>ui.json</c>, under
/// <c>objectives</c>), the region whose story it belongs to, a number that is what <c>VAR_OBJECTIVE</c> holds when a
/// script sets it, and, for a region whose chapters leave flags to follow, what says it is done.
/// </summary>
/// <param name="Done">
/// Whether the story has got past it; null for an objective a script sets by <c>objective</c> and nothing else.
/// Only ever a flag, a variable or a badge, so it never goes back to false once the story has moved on.
/// </param>
public sealed record Objective(int Id, string Name, string Region, Func<StoryState, bool>? Done)
{
    /// <summary>The key of its line in the table of words.</summary>
    public string Key => $"objectives.{Name}";
}

/// <summary>
/// "What to do next" (plan 12 · Q14; GPU-free). Each region's story is a list of objectives in the order it is played.
/// <para>
/// Sinnoh's are <i>derived</i>: the current one is the first whose <see cref="Objective.Done"/> the story has not
/// yet satisfied, worked out from the flags, variables and badges the chapters already set (S4 to S14 and the time
/// after the Hall of Fame), so the save gains no field and an older save needs no migration, as for the requests
/// of Q21. A region whose chapters set no such flags writes its objective with the script command
/// <c>objective &lt;name&gt;</c>, which stores its number in <see cref="Variable"/>; that is saved as any variable is.
/// A stored objective further along than the derived one wins, so a script can always move things on.
/// </para>
/// A new chapter adds its objectives here, in order, with their lines in <c>ui.json</c> (<c>objectives.*</c>), and
/// either gives each a <see cref="Objective.Done"/> or ends its scenes with <c>objective</c>.
/// </summary>
public static class Objectives
{
    /// <summary>The story variable a script's <c>objective</c> sets: the number of the objective (0 for none).</summary>
    public const string Variable = "VAR_OBJECTIVE";

    private static Objective S(int id, string name, Func<StoryState, bool>? done) => new(id, name, RegionDatabase.Sinnoh, done);

    private static Func<StoryState, bool> F(string flag) => s => s.Has(flag);

    private static Func<StoryState, bool> Badge(string name) => s => s.HasBadge(RegionDatabase.Sinnoh, name);

    /// <summary>Every objective of every region, in the order each region's story is played. Sinnoh's ids are 1 to 99.</summary>
    public static IReadOnlyList<Objective> All { get; } = new Objective[]
    {
        // Plan 02 · S4: the opening
        S(1, "downstairs", F("FLAG_TALKED_TO_MOM")),
        S(2, "rivalHome", F("FLAG_RIVAL_LEFT_HOME")),
        S(3, "route201", s => s.PlayerStarter != null),
        S(4, "shoes", F(StoryState.RunningShoesFlag)),
        S(5, "lake", s => s.Var("VAR_VISITED_LAKE_VERITY_WITH_RIVAL") >= 1),
        S(6, "lab", F(StoryState.PokedexFlag)),
        S(7, "parcel", F("FLAG_RECEIVED_PARCEL")),
        // S5: Jubilife and Roark
        S(8, "school", F("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL")),
        S(9, "poketch", F("FLAG_RECEIVED_POKETCH")),
        S(10, "roark", Badge("Coal")),
        // S6: the Windworks and Eterna
        S(11, "worksKey", F("FLAG_OBTAINED_FLOAROMA_MEADOW_WORKS_KEY")),
        S(12, "forest", F("FLAG_TRAVELED_WITH_CHERYL")),
        S(13, "gardenia", Badge("Forest")),
        S(14, "galacticBuilding", F("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING")),
        S(15, "bicycle", F("FLAG_RECEIVED_BICYCLE")),
        // S7 to S8: Hearthome, Veilstone and Pastoria
        S(16, "fantina", Badge("Relic")),
        S(17, "maylene", Badge("Cobble")),
        S(18, "wake", Badge("Fen")),
        // S9 to S10: Celestic Town, Canalave, Iron Island and the lakes
        S(19, "oldCharm", F("FLAG_DELIVERED_OLD_CHARM")),
        S(20, "ironIsland", F("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY")),
        S(21, "byron", Badge("Mine")),
        S(22, "lakes", F("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN")),
        // S11: Snowpoint City and Team Galactic's headquarters
        S(23, "candice", Badge("Icicle")),
        S(24, "storageKey", F("FLAG_USED_STORAGE_KEY")),
        S(25, "headquarters", F("FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN")),
        // S12: Mt. Coronet, Spear Pillar and the Distortion World
        S(26, "coronet", F("FLAG_RECEIVED_MT_CORONET_2F_BLACK_FLUTE")),
        S(27, "spearPillar", F("FLAG_GALACTIC_LEFT_LAKE_VALOR")),
        // S13: Sunyshore City
        S(28, "volkner", Badge("Beacon")),
        S(29, "beach", F("FLAG_RECEIVED_SUNYSHORE_CITY_HM07")),
        // S14: the Pokémon League
        S(30, "victoryRoad", F("FLAG_FIRST_ARRIVAL_POKEMON_LEAGUE")),
        S(31, "leagueGuard", F("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY")),
        S(32, "champion", F(StoryMigration.GameCompletedFlag)),
        // S15: after the Hall of Fame
        S(33, "afterwards", F("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN")),
        // Nothing presses any more
        S(34, "free", null),
    }
    .Concat(Kanto).Concat(Johto).Concat(Hoenn).Concat(Unova).Concat(Kalos).Concat(Alola).ToArray();

    // The later regions (plan 12 · Q14, their part). Each has a block of a hundred numbers of its own, its derived
    // conditions read from what only its own chapters set (its badges, its own flags and counts, never a flag another
    // region's game sets too, such as FLAG_HAS_POKEDEX), and its chapters' scenes end with `objective <next>` besides.
    // An objective of the opening is also done once anything after it is (a Champion who crossed in may come to a
    // region past a scene its own new game plays); a badge and a beat between badges are done by themselves alone,
    // so a Gym won out of turn leaves the one before it to do, as in Sinnoh.

    private static Func<StoryState, bool> RegionBadge(string region, string name) => s => s.HasBadge(region, name);

    private static Func<StoryState, bool> AtLeast(string variable, int value) => s => s.Var(variable) >= value;

    /// <summary>
    /// A region's list: (number, name, done, opening). An opening objective is also done once any objective after it
    /// in the region is; the last, with nothing to say it is done, is the region's "nothing presses".
    /// </summary>
    private static Objective[] Region(string region, params (int Id, string Name, Func<StoryState, bool>? Done, bool Opening)[] entries)
    {
        var own = entries.Select(e => e.Done).ToArray();
        var list = new Objective[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            Func<StoryState, bool>? done = e.Done;
            if (e.Opening && done != null)
            {
                int from = i + 1;
                var self = done;
                done = s => self(s) || own.Skip(from).Any(d => d != null && d(s));
            }
            list[i] = new Objective(e.Id, e.Name, region, done);
        }
        return list;
    }

    private static Objective[] Kanto => Region(RegionDatabase.Kanto,
        // Plan 19 · K3 to K5: Pallet Town, Oak's lab and the Parcel (FireRed's scene counts)
        (100, "kantoSetOut", AtLeast("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 1), true),
        (101, "kantoStarter", AtLeast("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 3), true),
        (102, "kantoRivalLab", AtLeast("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 4), true),
        (103, "kantoParcel", AtLeast("VAR_MAP_SCENE_VIRIDIAN_CITY_MART", 1), true),
        (104, "kantoParcelHome", AtLeast("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB", 6), true),
        // The Gyms, and what stands between them
        (105, "kantoBrock", RegionBadge(RegionDatabase.Kanto, "Boulder"), false),
        (106, "kantoMisty", RegionBadge(RegionDatabase.Kanto, "Cascade"), false),
        (107, "kantoBill", F("FLAG_GOT_SS_TICKET"), false),
        (108, "kantoSurge", RegionBadge(RegionDatabase.Kanto, "Thunder"), false),
        (109, "kantoErika", RegionBadge(RegionDatabase.Kanto, "Rainbow"), false),
        (110, "kantoHideout", F("FLAG_DEFEATED_ROCKET_HIDEOUT_GIOVANNI"), false),
        (111, "kantoTower", F("FLAG_RESCUED_MR_FUJI"), false),
        (112, "kantoKoga", RegionBadge(RegionDatabase.Kanto, "Soul"), false),
        (113, "kantoSilph", AtLeast("VAR_MAP_SCENE_SILPH_CO_11F", 1), false),
        (114, "kantoSabrina", RegionBadge(RegionDatabase.Kanto, "Marsh"), false),
        (115, "kantoBlaine", RegionBadge(RegionDatabase.Kanto, "Volcano"), false),
        (116, "kantoGiovanni", RegionBadge(RegionDatabase.Kanto, "Earth"), false),
        (117, "kantoLeague", F("KantoHallOfFame"), false),
        // Plan 19 · K9: the Sevii Islands
        (118, "kantoSevii", F("FLAG_SYS_CAN_LINK_WITH_RS"), false),
        (119, "kantoFree", null, false));

    private static Objective[] Johto => Region(RegionDatabase.Johto,
        // Plan 20 · J4: New Bark Town, Elm's errand and the Egg
        (200, "johtoMom", F("FLAG_GOT_POKEGEAR"), true),
        (201, "johtoElm", F("FLAG_GOT_STARTER"), true),
        (202, "johtoMrPokemon", F("FLAG_GOT_MYSTERY_EGG"), true),
        (203, "johtoBackToElm", F("FLAG_GAVE_MYSTERY_EGG_TO_ELM"), true),
        // J4 to J12: the Gyms, the Rockets and the League
        (204, "johtoFalkner", RegionBadge(RegionDatabase.Johto, "Zephyr"), false),
        (205, "johtoBugsy", RegionBadge(RegionDatabase.Johto, "Hive"), false),
        (206, "johtoWhitney", RegionBadge(RegionDatabase.Johto, "Plain"), false),
        (207, "johtoMorty", RegionBadge(RegionDatabase.Johto, "Fog"), false),
        (208, "johtoChuck", RegionBadge(RegionDatabase.Johto, "Storm"), false),
        (209, "johtoJasmine", RegionBadge(RegionDatabase.Johto, "Mineral"), false),
        (210, "johtoMahogany", F("FLAG_ROCKET_HIDEOUT_CLEARED"), false),
        (211, "johtoPryce", RegionBadge(RegionDatabase.Johto, "Glacier"), false),
        (212, "johtoRadioTower", AtLeast("VAR_SCENE_ROCKET_TAKEOVER", 5), false),
        (213, "johtoClair", RegionBadge(RegionDatabase.Johto, "Rising"), false),
        (214, "johtoLeague", F("JohtoHallOfFame"), false),
        // J15: HeartGold's Kanto, its Badges Kanto's own
        (215, "johtoKanto", s => s.BadgeCount(RegionDatabase.Kanto) >= 8, false),
        (216, "johtoFree", null, false));

    private static Objective[] Hoenn => Region(RegionDatabase.Hoenn,
        // Plan 21 · H2: Littleroot Town, the professor on Route 101 and the rival
        (300, "hoennNeighbours", F("FLAG_MET_RIVAL_MOM"), true),
        (301, "hoennBirch", F("FLAG_RESCUED_BIRCH"), true),
        (302, "hoennRival", F("FLAG_DEFEATED_RIVAL_ROUTE103"), true),
        (303, "hoennPokedex", AtLeast("VAR_BIRCH_LAB_STATE", 5), true),
        // H2 to H9: the Gyms and the League
        (304, "hoennRoxanne", RegionBadge(RegionDatabase.Hoenn, "Stone"), false),
        (305, "hoennDevon", F("FLAG_RECEIVED_POKENAV"), false),
        (306, "hoennBrawly", RegionBadge(RegionDatabase.Hoenn, "Knuckle"), false),
        (307, "hoennWattson", RegionBadge(RegionDatabase.Hoenn, "Dynamo"), false),
        (308, "hoennFlannery", RegionBadge(RegionDatabase.Hoenn, "Heat"), false),
        (309, "hoennNorman", RegionBadge(RegionDatabase.Hoenn, "Balance"), false),
        (310, "hoennWinona", RegionBadge(RegionDatabase.Hoenn, "Feather"), false),
        (311, "hoennTateLiza", RegionBadge(RegionDatabase.Hoenn, "Mind"), false),
        (312, "hoennJuan", RegionBadge(RegionDatabase.Hoenn, "Rain"), false),
        (313, "hoennLeague", F("HoennHallOfFame"), false),
        (314, "hoennFree", null, false));

    private static Objective[] Unova => Region(RegionDatabase.Unova,
        // Plan 22 · U5: Aspertia City (VAR_UNOVA_ASPERTIA, aspertia_city.txt's count)
        (400, "unovaHugh", AtLeast("VAR_UNOVA_ASPERTIA", 1), true),
        (401, "unovaOutlook", AtLeast("VAR_UNOVA_ASPERTIA", 5), true),
        (402, "unovaCenter", AtLeast("VAR_UNOVA_ASPERTIA", 6), true),
        // U5 to U9: the Gyms, the Giant Chasm and the League
        (403, "unovaCheren", RegionBadge(RegionDatabase.Unova, "Basic"), false),
        (404, "unovaRoxie", RegionBadge(RegionDatabase.Unova, "Toxic"), false),
        (405, "unovaBurgh", RegionBadge(RegionDatabase.Unova, "Insect"), false),
        (406, "unovaElesa", RegionBadge(RegionDatabase.Unova, "Bolt"), false),
        (407, "unovaClay", RegionBadge(RegionDatabase.Unova, "Quake"), false),
        (408, "unovaSkyla", RegionBadge(RegionDatabase.Unova, "Jet"), false),
        (409, "unovaDrayden", RegionBadge(RegionDatabase.Unova, "Legend"), false),
        (410, "unovaMarlon", RegionBadge(RegionDatabase.Unova, "Wave"), false),
        (411, "unovaChasm", AtLeast("VAR_UNOVA_CHASM", 4), false),
        (412, "unovaLeague", F("UnovaHallOfFame"), false),
        (413, "unovaFree", null, false));

    private static Objective[] Kalos => Region(RegionDatabase.Kalos,
        // Plan 23 · Z1: VAR_KALOS_STORY, vaniville_town.txt's count, to the Bug Badge
        (500, "kalosDownstairs", AtLeast("VAR_KALOS_STORY", 1), true),
        (501, "kalosDoor", AtLeast("VAR_KALOS_STORY", 2), true),
        (502, "kalosAquacorde", AtLeast("VAR_KALOS_STORY", 3), true),
        (503, "kalosHome", AtLeast("VAR_KALOS_STORY", 4), true),
        (504, "kalosRoute2", AtLeast("VAR_KALOS_STORY", 5), true),
        (505, "kalosForest", AtLeast("VAR_KALOS_STORY", 6), true),
        (506, "kalosRoute3", AtLeast("VAR_KALOS_STORY", 7), true),
        (507, "kalosSantalune", AtLeast("VAR_KALOS_STORY", 8), true),
        (508, "kalosViola", s => s.Var("VAR_KALOS_STORY") >= 9 || s.HasBadge(RegionDatabase.Kalos, "Bug"), false),
        // Plan 23 · Z2: Route 4 to Lumiose City, the professor's laboratory and Lysandre's café. 509 was "nothing presses"
        // until Z2: a save that stored it after Viola now stores the road on to Lumiose, which is what it meant
        (509, "kalosRoute4", AtLeast("VAR_KALOS_STORY", 10), false),
        (510, "kalosLab", AtLeast("VAR_KALOS_STORY", 11), false),
        (511, "kalosLysandre", AtLeast("VAR_KALOS_STORY", 12), false),
        // Plan 23 · Z2, second part: west to Camphrier Town, the palace's flute, the Snorlax on Route 7, the neighbour in
        // Cyllage City and Grant's Cliff Badge
        (512, "kalosRoute5", AtLeast("VAR_KALOS_STORY", 13), false),
        (513, "kalosCamphrier", AtLeast("VAR_KALOS_STORY", 14), false),
        (514, "kalosPalace", AtLeast("VAR_KALOS_STORY", 15), false),
        (515, "kalosSnorlax", AtLeast("VAR_KALOS_STORY", 16), false),
        (516, "kalosCyllage", AtLeast("VAR_KALOS_STORY", 17), false),
        (517, "kalosGrant", s => s.Var("VAR_KALOS_STORY") >= 18 || s.HasBadge(RegionDatabase.Kalos, "Cliff"), false),
        // Plan 23 · Z3, first part: north up Route 10 to Geosenge Town, on to Shalour City's tower, Korrina's Rumble Badge
        // and the Key Stone
        (518, "kalosRoute10", AtLeast("VAR_KALOS_STORY", 19), false),
        (519, "kalosShalour", AtLeast("VAR_KALOS_STORY", 20), false),
        (520, "kalosKorrina", s => s.Var("VAR_KALOS_STORY") >= 21 || s.HasBadge(RegionDatabase.Kalos, "Rumble"), false),
        (521, "kalosMastery", AtLeast("VAR_KALOS_STORY", 22), false),
        // Plan 23 · Z4: Ramos's Plant Badge, the Power Plant, Clemont's Voltage Badge, Valerie's Fairy Badge, Olympia's
        // Psychic Badge, Lysandre's laboratory, Team Flare's base under Geosenge Town, Wulfric's Iceberg Badge and the League
        (522, "kalosRamos", s => s.Var("VAR_KALOS_STORY") >= 23 || s.HasBadge(RegionDatabase.Kalos, "Plant"), false),
        (523, "kalosPowerPlant", AtLeast("VAR_KALOS_STORY", 24), false),
        (524, "kalosClemont", s => s.Var("VAR_KALOS_STORY") >= 25 || s.HasBadge(RegionDatabase.Kalos, "Voltage"), false),
        (525, "kalosValerie", s => s.Var("VAR_KALOS_STORY") >= 26 || s.HasBadge(RegionDatabase.Kalos, "Fairy"), false),
        (526, "kalosOlympia", s => s.Var("VAR_KALOS_STORY") >= 27 || s.HasBadge(RegionDatabase.Kalos, "Psychic"), false),
        (527, "kalosLabs", AtLeast("VAR_KALOS_STORY", 28), false),
        (528, "kalosFlareHQ", AtLeast("VAR_KALOS_STORY", 29), false),
        (529, "kalosWulfric", s => s.Var("VAR_KALOS_STORY") >= 30 || s.HasBadge(RegionDatabase.Kalos, "Iceberg"), false),
        (530, "kalosLeague", s => s.Var("VAR_KALOS_STORY") >= 31 || s.Has("KalosHallOfFame"), false),
        // Plan 23 · Z4b, after the Hall of Fame: the professor at Lumiose Station and the TMV pass, the neighbour in Kiloude
        // City, the Mega Ring upgraded by Anistar's sundial, and Looker's episode to its end (VAR_KALOS_POSTGAME,
        // VAR_KALOS_LOOKER); the legendaries, the bird and the stones ask nothing of the story
        (531, "kalosStation", AtLeast("VAR_KALOS_POSTGAME", 1), false),
        (532, "kalosKiloude", AtLeast("VAR_KALOS_POSTGAME", 2), false),
        (533, "kalosSundial", AtLeast("VAR_KALOS_POSTGAME", 3), false),
        (534, "kalosLooker", AtLeast("VAR_KALOS_LOOKER", 7), false),
        // What comes after that (the flight to Alola, the rest of the post-game) leaves nothing pressing
        (599, "kalosFree", null, false));
    private static Objective[] Alola => Region(RegionDatabase.Alola,
        // Plan 23 · Z5: VAR_ALOLA_STORY, alola_route_1.txt's count, from the new home to Hau'oli City
        (600, "alolaMom", AtLeast("VAR_ALOLA_STORY", 1), true),
        (601, "alolaDoor", AtLeast("VAR_ALOLA_STORY", 2), true),
        (602, "alolaIkiTown", AtLeast("VAR_ALOLA_STORY", 3), true),
        (603, "alolaTrail", AtLeast("VAR_ALOLA_STORY", 4), true),
        (604, "alolaKahuna", AtLeast("VAR_ALOLA_STORY", 5), true),
        (605, "alolaLab", AtLeast("VAR_ALOLA_STORY", 6), true),
        (606, "alolaHauoli", AtLeast("VAR_ALOLA_STORY", 7), true),
        // Plan 23 · Z5, second part: the marina, the festival, Ilima's trial, the meadow, Hau on Route 3, Hala's grand
        // trial and the boat to Akala
        (607, "alolaMarina", AtLeast("VAR_ALOLA_STORY", 8), false),
        (608, "alolaFestival", AtLeast("VAR_ALOLA_STORY", 9), false),
        (609, "alolaVerdant", AtLeast("VAR_ALOLA_STORY", 10), false),
        (610, "alolaMeadow", AtLeast("VAR_ALOLA_STORY", 11), false),
        (611, "alolaHau3", AtLeast("VAR_ALOLA_STORY", 12), false),
        (612, "alolaGrandTrial", s => s.Var("VAR_ALOLA_STORY") >= 13 || s.HasBadge(RegionDatabase.Alola, "Melemele"), false),
        (613, "alolaSail", AtLeast("VAR_ALOLA_STORY", 14), false),
        // Akala: Route 4 to Paniola Town, the ranch, Route 5 and Lana's trial at Brooklet Hill
        (614, "alolaPaniola", AtLeast("VAR_ALOLA_STORY", 15), false),
        (615, "alolaRanch", AtLeast("VAR_ALOLA_STORY", 16), false),
        (616, "alolaRoute5", AtLeast("VAR_ALOLA_STORY", 17), false),
        (617, "alolaBrooklet", AtLeast("VAR_ALOLA_STORY", 18), false),
        // Plan 23 · Z6: the rest of Akala, on a count of its own, VAR_ALOLA_AKALA (royal_avenue.txt's header): Royal
        // Avenue, the motel on Route 8, Kiawe's trial, Mallow's, Konikoni City, the Ruins of Life, Olivia's grand trial
        // and the Hano Grand Resort
        (618, "alolaRoyal", AtLeast("VAR_ALOLA_AKALA", 1), false),
        (619, "alolaMotel", AtLeast("VAR_ALOLA_AKALA", 2), false),
        (620, "alolaWela", AtLeast("VAR_ALOLA_AKALA", 3), false),
        (621, "alolaJungle", AtLeast("VAR_ALOLA_AKALA", 4), false),
        (622, "alolaKonikoni", AtLeast("VAR_ALOLA_AKALA", 5), false),
        (623, "alolaRuins", AtLeast("VAR_ALOLA_AKALA", 6), false),
        (624, "alolaOlivia", s => s.Var("VAR_ALOLA_AKALA") >= 7 || s.HasBadge(RegionDatabase.Alola, "Akala"), false),
        (625, "alolaHano", AtLeast("VAR_ALOLA_AKALA", 8), false),
        // Plan 23 · Z7: the Aether tour, Ula'ula's trials and grand trial, the rescue, Poni's trial and grand trial, the
        // Altar and the League (VAR_ALOLA_STORY 40 to 54)
        (650, "alolaAether", AtLeast("VAR_ALOLA_STORY", 41), false),
        (651, "alolaMalie", AtLeast("VAR_ALOLA_STORY", 42), false),
        (652, "alolaHokulani", AtLeast("VAR_ALOLA_STORY", 43), false),
        (653, "alolaTapu", AtLeast("VAR_ALOLA_STORY", 44), false),
        (654, "alolaMegamart", AtLeast("VAR_ALOLA_STORY", 45), false),
        (655, "alolaPoTown", AtLeast("VAR_ALOLA_STORY", 46), false),
        (656, "alolaNanu", s => s.Var("VAR_ALOLA_STORY") >= 47 || s.HasBadge(RegionDatabase.Alola, "UlaUla"), false),
        (657, "alolaRescue", AtLeast("VAR_ALOLA_STORY", 48), false),
        (658, "alolaPoni", AtLeast("VAR_ALOLA_STORY", 49), false),
        (659, "alolaDulse", AtLeast("VAR_ALOLA_STORY", 50), false),
        (660, "alolaHapu", s => s.Var("VAR_ALOLA_STORY") >= 51 || s.HasBadge(RegionDatabase.Alola, "Poni"), false),
        (661, "alolaAltar", AtLeast("VAR_ALOLA_STORY", 52), false),
        (662, "alolaLanakila", s => s.Var("VAR_ALOLA_STORY") >= 54 || s.Has("AlolaHallOfFame"), false),
        // Plan 23, the wave after the loose ends: after the Hall of Fame, the four guardians in their ruins and the totems
        // of the trials that came back (flags their scripts set once, never cleared)
        (663, "alolaTapuKoko", F("FLAG_ALOLA_TAPU_KOKO_DONE"), false),
        (664, "alolaTapuLele", F("FLAG_ALOLA_TAPU_LELE_DONE"), false),
        (665, "alolaTapuBulu", F("FLAG_ALOLA_TAPU_BULU_DONE"), false),
        (666, "alolaTapuFini", F("FLAG_ALOLA_TAPU_FINI_DONE"), false),
        (667, "alolaTotems", s => s.Has("FLAG_ALOLA_TOTEM_RATICATE_DONE") && s.Has("FLAG_ALOLA_TOTEM_ARAQUANID_DONE")
            && s.Has("FLAG_ALOLA_TOTEM_SALAZZLE_DONE") && s.Has("FLAG_ALOLA_TOTEM_VIKAVOLT_DONE"), false),
        (699, "alolaFree", null, false));

    private static readonly Dictionary<string, Objective> ByName = All.ToDictionary(o => o.Name, StringComparer.Ordinal);

    /// <summary>An objective by its name, as <c>objective &lt;name&gt;</c> writes it; null for a name there is none of.</summary>
    public static Objective? Find(string name) => ByName.GetValueOrDefault(name);

    /// <summary>An objective by the number a story variable holds; null for none.</summary>
    public static Objective? ById(int id) => id > 0 ? All.FirstOrDefault(o => o.Id == id) : null;

    /// <summary>
    /// The objective the story is at in a region: the first of the region's derived ones not yet done, or the one a
    /// script stored if that is further along; null in a region with neither.
    /// </summary>
    public static Objective? Current(StoryState story, string region)
    {
        Objective? derived = null;
        foreach (var o in All)
        {
            if (!string.Equals(o.Region, region, StringComparison.OrdinalIgnoreCase)) continue;
            if (o.Done == null || !o.Done(story)) { derived = o; break; }
        }
        var stored = ById(story.Var(Variable));
        if (stored != null && !string.Equals(stored.Region, region, StringComparison.OrdinalIgnoreCase)) stored = null;
        if (derived == null) return stored;
        return stored != null && stored.Id > derived.Id ? stored : derived;
    }

    /// <summary>
    /// The line that says what to do next, with the player's and the rival's names in, in the language chosen; empty
    /// where the region has none yet.
    /// </summary>
    public static string Line(StoryState story, string region) =>
        Current(story, region) is { } o ? PlayerIdentity.Fill(Text.Of(o.Key)) : "";

    /// <summary>The same for a save, in the region it was made in.</summary>
    public static string Line(SaveData save)
    {
        var story = new StoryState();
        story.Restore(save.ToStory());
        return Line(story, save.RegionId);
    }
}
