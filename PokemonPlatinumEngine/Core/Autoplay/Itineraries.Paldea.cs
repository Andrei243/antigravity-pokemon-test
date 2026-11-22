using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// Paldea's first chapter (plan 34 · PD1's "done when"): from the bedroom in Cabo Poco to the academy's door in Mesagoza,
/// the open map's triggers by their names and the rooms' scenes by their arrivals, as
/// <c>PaldeaTests.TheFirstChapterRunsFromTheBedroomToTheAcademysDoorHeadless</c> plays the scripts one by one. Each leg is
/// done when <c>VAR_PALDEA_STORY</c> (cabo_poco.txt's header has its count) says so.
/// </summary>
public static partial class Itineraries
{
    private const string PaldeaMap = "Paldea";

    private static Func<StoryState, bool> PaldeaStory(int at) => story => story.Var("VAR_PALDEA_STORY") >= at;

    /// <summary>Mom and the director, the three on the terrace, the roar, the two in the grotto, Nemona's lesson, the gate and the academy's welcome.</summary>
    public static IReadOnlyList<Leg> PaldeaToTheAcademy { get; } = new[]
    {
        Leg.Enter("CaboPoco_PlayersHouse_1F") with { Until = PaldeaStory(1) },
        Leg.OpenTrigger(PaldeaMap, "the_three") with { Until = PaldeaStory(2) },
        Leg.OpenTrigger(PaldeaMap, "a_roar") with { Until = PaldeaStory(3) },
        Leg.Trigger("InletGrotto", "TheTwo") with { Until = PaldeaStory(4) },
        Leg.OpenTrigger(PaldeaMap, "nemonas_lesson") with { Until = PaldeaStory(5) },
        Leg.OpenTrigger(PaldeaMap, "nemona_at_the_gate") with { Until = PaldeaStory(6) },
        Leg.Enter("Mesagoza_AcademyHall"),
        // (an arrival is done as the door is come through, while the welcome still plays: the director spoken to after it
        // is what holds the itinerary open until the welcome has set the count)
        Leg.Talk("Mesagoza_AcademyHall", "clavell_hall") with { Until = PaldeaStory(7) },
    };

    private static Func<StoryState, bool> PaldeaBadge(string badge) => story => story.HasBadge(RegionDatabase.Paldea, badge);

    /// <summary>
    /// Plan 34 · PD3's second session: from the academy's door the first three gyms in the academy's order, each test
    /// and then its leader, the team seen to at the town's Pokémon Center before each: Katy's olive roll in her gym's room,
    /// Brassius's hidden Sunflora about Artazon and Iono's Wattrel about Levincia on the open map.
    /// </summary>
    public static IReadOnlyList<Leg> PaldeaToTheThreeBadges { get; } = new[]
    {
        Leg.Talk("Cortondo_GymTest", "giant_olive_1") with { Until = Flag("FLAG_PALDEA_CORTONDO_TEST_1") },
        Leg.Talk("Cortondo_GymTest", "giant_olive_2") with { Until = Flag("FLAG_PALDEA_CORTONDO_TEST_2") },
        Leg.Talk("Cortondo_GymTest", "giant_olive_3") with { Until = Flag("FLAG_PALDEA_CORTONDO_TEST_3") },
        Leg.Heal("Cortondo_PokemonCenter"),
        Leg.Talk("Cortondo_GymTest", "katy") with { Until = PaldeaBadge("Bug") },
        Leg.OpenTalk(PaldeaMap, "hidden_sunflora_1") with { Until = Flag("FLAG_PALDEA_ARTAZON_TEST_1") },
        Leg.OpenTalk(PaldeaMap, "hidden_sunflora_2") with { Until = Flag("FLAG_PALDEA_ARTAZON_TEST_2") },
        Leg.OpenTalk(PaldeaMap, "hidden_sunflora_3") with { Until = Flag("FLAG_PALDEA_ARTAZON_TEST_3") },
        Leg.Heal("Artazon_PokemonCenter"),
        Leg.Talk("Artazon_GymTest", "brassius") with { Until = PaldeaBadge("Grass") },
        Leg.OpenTalk(PaldeaMap, "hidden_wattrel_1") with { Until = Flag("FLAG_PALDEA_LEVINCIA_TEST_1") },
        Leg.OpenTalk(PaldeaMap, "hidden_wattrel_2") with { Until = Flag("FLAG_PALDEA_LEVINCIA_TEST_2") },
        Leg.OpenTalk(PaldeaMap, "hidden_wattrel_3") with { Until = Flag("FLAG_PALDEA_LEVINCIA_TEST_3") },
        Leg.Heal("Levincia_PokemonCenter"),
        Leg.Talk("Levincia_GymTest", "iono") with { Until = PaldeaBadge("Electric") },
    };

    /// <summary>Where <c>VAR_PALDEA_LEGENDS</c> stands once the path is walked to its end.</summary>
    public const int PaldeaLegendsEnd = 12;

    private static Func<StoryState, bool> PaldeaLegends(int at) => story => story.Var("VAR_PALDEA_LEGENDS") >= at;

    /// <summary>
    /// Plan 34 · PD4: the Path of Legends from Arven's word at Poco Path's lighthouse, each Titan met as its place is
    /// walked into and beaten again at its den, on foot (the bot rides no link yet), and Arven's battle at the lighthouse
    /// after the fifth. Each leg is done when <c>VAR_PALDEA_LEGENDS</c> (stony_cliff.txt's header has its count) says so.
    /// </summary>
    public static IReadOnlyList<Leg> PaldeaPathOfLegends { get; } = new[]
    {
        Leg.OpenTalk(PaldeaMap, "arven_lighthouse") with { Until = PaldeaLegends(1) },
        Leg.OpenTrigger(PaldeaMap, "klawf_gully") with { Until = PaldeaLegends(2) },
        Leg.OpenTrigger(PaldeaMap, "klawf_den") with { Until = PaldeaLegends(3) },
        Leg.OpenTrigger(PaldeaMap, "bombirdier_gully") with { Until = PaldeaLegends(4) },
        Leg.OpenTrigger(PaldeaMap, "bombirdier_den") with { Until = PaldeaLegends(5) },
        Leg.OpenTrigger(PaldeaMap, "orthworm_cutting") with { Until = PaldeaLegends(6) },
        Leg.OpenTrigger(PaldeaMap, "orthworm_den") with { Until = PaldeaLegends(7) },
        Leg.OpenTrigger(PaldeaMap, "quaking_basin") with { Until = PaldeaLegends(8) },
        Leg.OpenTrigger(PaldeaMap, "quaking_den") with { Until = PaldeaLegends(9) },
        Leg.OpenTrigger(PaldeaMap, "tatsugiri_shore") with { Until = PaldeaLegends(10) },
        Leg.OpenTrigger(PaldeaMap, "dondozo_shore") with { Until = PaldeaLegends(11) },
        Leg.OpenTalk(PaldeaMap, "arven_lighthouse") with { Until = PaldeaLegends(12) },
    };

    /// <summary>From the bedroom in Cabo Poco to the Electric Badge.</summary>
    public static IReadOnlyList<Leg> PaldeaToTheThirdBadge => Join(PaldeaToTheAcademy, PaldeaToTheThreeBadges);

    /// <summary>
    /// Plan 34 · PD6 (owed by its first session, built by its second): on from the third badge to the fifth in the
    /// academy's order, Kofu's errand to Porto Marinada's auction and Larry's secret order at the Treasure Eatery. The
    /// auction is a run of questions the bot answers otherwise each time it tries again (<see cref="Leg.Vary"/>): wait,
    /// wait and raise the paddle is the fourth way it takes, within its tries.
    /// </summary>
    public static IReadOnlyList<Leg> PaldeaWestBadges { get; } = new[]
    {
        Leg.Talk("Cascarrafa_GymTest", "kofu") with { Until = Flag("FLAG_PALDEA_CASCARRAFA_ERRAND") },
        Leg.OpenTalk(PaldeaMap, "porto_auctioneer") with { Until = Flag("FLAG_PALDEA_CASCARRAFA_TEST_DONE"), Vary = true },
        Leg.Heal("Cascarrafa_PokemonCenter"),
        Leg.Talk("Cascarrafa_GymTest", "kofu") with { Until = PaldeaBadge("Water") },
        Leg.OpenTalk(PaldeaMap, "medali_regular") with { Until = Flag("FLAG_PALDEA_MEDALI_TEST_1") },
        Leg.OpenTalk(PaldeaMap, "medali_waiter") with { Until = Flag("FLAG_PALDEA_MEDALI_TEST_2") },
        Leg.OpenTalk(PaldeaMap, "medali_office_worker") with { Until = Flag("FLAG_PALDEA_MEDALI_TEST_3") },
        Leg.Talk("Medali_TreasureEatery", "eatery_waiter") with { Until = Flag("FLAG_PALDEA_MEDALI_TEST_DONE") },
        Leg.Heal("Medali_PokemonCenter"),
        Leg.Talk("Medali_GymTest", "larry") with { Until = PaldeaBadge("Normal") },
    };

    /// <summary>From the bedroom in Cabo Poco to the Normal Badge, the fifth.</summary>
    public static IReadOnlyList<Leg> PaldeaToTheFifthBadge => Join(PaldeaToTheThirdBadge, PaldeaWestBadges);

    private static Func<StoryState, bool> GlaseadoRun(int at) => story => story.Var("VAR_PALDEA_GLASEADO_RUN") >= at;

    /// <summary>
    /// Plan 34 · PD6, its second session: the last three gyms in the academy's order. Ryme's rap battle and Tulip's
    /// photographs are answered otherwise each time the MC or the photographer is spoken to again (<see cref="Leg.Vary"/>):
    /// a line or a shot answered right is kept, so each is passed within three turns; Grusha's run is the coach at the top,
    /// the three gates in order down the snowslope and the line at its foot, and back up to the gym.
    /// </summary>
    public static IReadOnlyList<Leg> PaldeaNorthBadges { get; } = new[]
    {
        Leg.Talk("Montenevera_GymTest", "montenevera_mc") with { Until = Flag("FLAG_PALDEA_MONTENEVERA_TEST_DONE"), Vary = true },
        Leg.Heal("Montenevera_PokemonCenter"),
        Leg.Talk("Montenevera_GymTest", "ryme") with { Until = PaldeaBadge("Ghost") },
        Leg.Talk("Alfornada_GymTest", "alfornada_photographer") with { Until = Flag("FLAG_PALDEA_ALFORNADA_TEST_DONE"), Vary = true },
        Leg.Heal("Alfornada_PokemonCenter"),
        Leg.Talk("Alfornada_GymTest", "tulip") with { Until = PaldeaBadge("Psychic") },
        Leg.OpenTalk(PaldeaMap, "glaseado_coach") with { Until = GlaseadoRun(1) },
        Leg.OpenTrigger(PaldeaMap, "glaseado_gate_one") with { Until = GlaseadoRun(2) },
        Leg.OpenTrigger(PaldeaMap, "glaseado_gate_two") with { Until = GlaseadoRun(3) },
        Leg.OpenTrigger(PaldeaMap, "glaseado_gate_three") with { Until = GlaseadoRun(4) },
        Leg.OpenTrigger(PaldeaMap, "glaseado_finish") with { Until = Flag("FLAG_PALDEA_GLASEADO_TEST_DONE") },
        Leg.Heal("Glaseado_PokemonCenter"),
        Leg.Talk("Glaseado_GymTest", "grusha") with { Until = PaldeaBadge("Ice") },
    };

    /// <summary>
    /// Plan 34 · PD6's done-when, the region walked and ridden: from Cabo Poco's lane north through Los Platos and
    /// Mesagoza, west to Cortondo, north through Asado Desert, east to Medali, north along the North Province's first area
    /// to Glaseado's foot and up its snowslope to the top, a marker of each place in turn.
    /// </summary>
    public static IReadOnlyList<Leg> PaldeaToGlaseadosTop { get; } = new[]
    {
        Leg.OpenReach(PaldeaMap, "los_platos_south"),
        Leg.OpenReach(PaldeaMap, "mesagoza_plaza"),
        Leg.OpenReach(PaldeaMap, "cortondo_square"),
        Leg.OpenReach(PaldeaMap, "desert_crossroads"),
        Leg.OpenReach(PaldeaMap, "medali_square"),
        Leg.OpenReach(PaldeaMap, "north_one_north"),
        Leg.OpenReach(PaldeaMap, "glaseado_foot"),
        Leg.OpenReach(PaldeaMap, "glaseado_top"),
    };
}
