using System;
using System.Collections.Generic;
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
}
