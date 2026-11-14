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
}
