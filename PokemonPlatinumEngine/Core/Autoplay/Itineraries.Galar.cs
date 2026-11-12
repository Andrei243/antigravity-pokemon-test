using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// Galar's way from the bedroom in Postwick to the Hall of Fame (plan 33 · GL6's "done when"), the first itinerary that
/// walks an open region and its grid rooms as one: the open map's triggers, people and markers by their names
/// (<see cref="Leg.OpenTrigger"/>, <see cref="Leg.OpenTalk"/>), the rooms' people by their keys, as
/// <c>GalarTests.TheFirstChapterRunsFromTheBedroomToTheTrainHeadless</c> plays the scripts one by one. Each leg is done
/// when <c>VAR_GALAR_STORY</c> (postwick.txt's header has its count), a badge or a flag says so.
/// </summary>
public static partial class Itineraries
{
    private const string GalarMap = "Galar";

    private static Func<StoryState, bool> GalarStory(int at) => story => story.Var("VAR_GALAR_STORY") >= at;
    private static Func<StoryState, bool> GalarBadge(string badge) => story => story.HasBadge(RegionDatabase.Galar, badge);
    private static Func<StoryState, bool> Flag(string flag) => story => story.Has(flag);

    /// <summary>The first chapter: Mum, Hop at the door, Leon home and the three, the Weald's shadow, the professor, the lake and the train.</summary>
    public static IReadOnlyList<Leg> GalarToTheTrain { get; } = new[]
    {
        Leg.Enter("Postwick_PlayersHouse_1F"),
        Leg.OpenTrigger(GalarMap, "hop_at_the_door"),
        Leg.OpenTrigger(GalarMap, "leon_arrives"),
        Leg.OpenTrigger(GalarMap, "weald_gate") with { Until = GalarStory(4) },
        Leg.OpenTrigger(GalarMap, "the_shadow"),
        Leg.Enter("Postwick_PlayersHouse_1F") with { Until = GalarStory(6) },
        Leg.OpenTrigger(GalarMap, "hop_waits"),
        Leg.Enter("Route2_MagnoliasHouse") with { Until = GalarStory(8) },
        Leg.OpenTrigger(GalarMap, "hops_battle"),
        Leg.OpenTrigger(GalarMap, "wishing_stars"),
        Leg.Talk("Wedgehurst_Station", "station_attendant") with { Until = GalarStory(11) },
    };

    /// <summary>The Wild Area to the Fire Badge: Motostoke's steps, the registration, the hotel, the ceremony and the first three gyms.</summary>
    public static IReadOnlyList<Leg> GalarToTheFireBadge { get; } = new[]
    {
        Leg.OpenTrigger(GalarMap, "motostoke_steps") with { Until = GalarStory(13) },
        Leg.OpenTrigger(GalarMap, "motostoke_arrival"),
        Leg.Enter("Motostoke_Stadium") with { Until = GalarStory(15) },
        Leg.Enter("Motostoke_Hotel") with { Until = GalarStory(16) },
        // (the lobby's arrival on the ceremony's morning takes the player through to the pitch, and out to the street)
        Leg.Enter("Motostoke_Stadium") with { Until = GalarStory(17) },
        Leg.Trigger("GalarMine", "BedeInTheMine"),
        Leg.Talk("Turffield_StadiumMission", "mission_wooloo_1") with { Until = Flag("FLAG_GALAR_TURFFIELD_WOOLOO_1") },
        Leg.Talk("Turffield_StadiumMission", "mission_wooloo_2") with { Until = Flag("FLAG_GALAR_TURFFIELD_WOOLOO_2") },
        Leg.Talk("Turffield_StadiumMission", "mission_wooloo_3") with { Until = Flag("FLAG_GALAR_TURFFIELD_WOOLOO_3") },
        Leg.Talk("Turffield_StadiumPitch", "milo") with { Until = GalarBadge("Grass") },
        Leg.OpenTrigger(GalarMap, "route5_hop"),
        Leg.Talk("Hulbury_StadiumMission", "mission_switch_1") with { Until = Flag("FLAG_GALAR_HULBURY_SWITCH_1") },
        Leg.Talk("Hulbury_StadiumMission", "mission_switch_2") with { Until = Flag("FLAG_GALAR_HULBURY_SWITCH_2") },
        Leg.Talk("Hulbury_StadiumMission", "mission_switch_3") with { Until = Flag("FLAG_GALAR_HULBURY_SWITCH_3") },
        Leg.Talk("Hulbury_StadiumPitch", "nessa") with { Until = GalarBadge("Water") },
        Leg.Trigger("GalarMine2", "BedeInMineTwo"),
        Leg.OpenTrigger(GalarMap, "motostoke_marnie"),
        Leg.Talk("Motostoke_StadiumMission", "mission_vulpix") with { Until = Flag("FLAG_GALAR_MOTOSTOKE_CAUGHT_1") },
        Leg.Talk("Motostoke_StadiumMission", "mission_growlithe") with { Until = Flag("FLAG_GALAR_MOTOSTOKE_CAUGHT_2") },
        Leg.Talk("Motostoke_StadiumMission", "mission_sizzlipede") with { Until = Flag("FLAG_GALAR_MOTOSTOKE_CAUGHT_3") },
        Leg.Talk("Motostoke_StadiumPitch", "kabu") with { Until = GalarBadge("Fire") },
    };

    /// <summary>Hammerlocke to the Dragon Badge (a Sword game: Bea and Gordie), the bike fitted for the water, Spikemuth and Raihan.</summary>
    public static IReadOnlyList<Leg> GalarToTheDragonBadge { get; } = new[]
    {
        Leg.OpenTrigger(GalarMap, "hammerlocke_arrival"),
        Leg.Enter("Hammerlocke_Vault") with { Until = GalarStory(26) },
        Leg.OpenTrigger(GalarMap, "route6_hop"),
        Leg.Talk("StowOnSide_StadiumMission", "mission_gate_1") with { Until = Flag("FLAG_GALAR_STOW_GATE_1") },
        Leg.Talk("StowOnSide_StadiumMission", "mission_gate_2") with { Until = Flag("FLAG_GALAR_STOW_GATE_2") },
        Leg.Talk("StowOnSide_StadiumMission", "mission_gate_3") with { Until = Flag("FLAG_GALAR_STOW_GATE_3") },
        Leg.Talk("StowOnSide_StadiumPitch", "bea") with { Until = GalarBadge("Fighting") },
        Leg.OpenTrigger(GalarMap, "stow_mural"),
        // (Opal's quiz: a wrong answer leaves the quizmaster in the way, so each is asked again with the next answer)
        Leg.Talk("Ballonlea_StadiumMission", "mission_quiz_1") with { Until = Flag("FLAG_GALAR_BALLONLEA_QUIZ_1"), Vary = true },
        Leg.Talk("Ballonlea_StadiumMission", "mission_quiz_2") with { Until = Flag("FLAG_GALAR_BALLONLEA_QUIZ_2"), Vary = true },
        Leg.Talk("Ballonlea_StadiumMission", "mission_quiz_3") with { Until = Flag("FLAG_GALAR_BALLONLEA_QUIZ_3"), Vary = true },
        Leg.Talk("Ballonlea_StadiumPitch", "opal") with { Until = GalarBadge("Fairy") },
        Leg.OpenTrigger(GalarMap, "circhester_arrival"),
        Leg.Talk("Circhester_StadiumMission", "mission_gate_1") with { Until = Flag("FLAG_GALAR_CIRCHESTER_GATE_1") },
        Leg.Talk("Circhester_StadiumMission", "mission_gate_2") with { Until = Flag("FLAG_GALAR_CIRCHESTER_GATE_2") },
        Leg.Talk("Circhester_StadiumMission", "mission_gate_3") with { Until = Flag("FLAG_GALAR_CIRCHESTER_GATE_3") },
        Leg.Talk("Circhester_StadiumPitch", "gordie") with { Until = GalarBadge("Rock") },
        Leg.OpenTalk(GalarMap, "bike_mechanic") with { Until = GalarStory(33) },
        Leg.OpenTrigger(GalarMap, "spikemuth_gate") with { Until = GalarStory(34) },
        Leg.OpenTalk(GalarMap, "piers") with { Until = GalarBadge("Dark") },
        Leg.Talk("Hammerlocke_StadiumMission", "mission_gate_1") with { Until = Flag("FLAG_GALAR_HAMMERLOCKE_GATE_1") },
        Leg.Talk("Hammerlocke_StadiumMission", "mission_gate_2") with { Until = Flag("FLAG_GALAR_HAMMERLOCKE_GATE_2") },
        Leg.Talk("Hammerlocke_StadiumMission", "mission_gate_3") with { Until = Flag("FLAG_GALAR_HAMMERLOCKE_GATE_3") },
        Leg.Talk("Hammerlocke_StadiumPitch", "raihan") with { Until = GalarBadge("Dragon") },
    };

    /// <summary>Wyndon to the Hall of Fame: the train, the Semifinals, the Darkest Day, Oleana, the Energy Plant, the Finals and Leon.</summary>
    public static IReadOnlyList<Leg> GalarToTheHallOfFame { get; } = new[]
    {
        Leg.OpenTalk(GalarMap, "hammerlocke_station_staff"),
        Leg.OpenTrigger(GalarMap, "wyndon_arrival"),
        Leg.Talk("Wyndon_StadiumPitch", "marnie") with { Until = GalarStory(38) },
        Leg.Talk("Wyndon_StadiumPitch", "hop") with { Until = GalarStory(39) },
        // (the lobby on the way out is where the Darkest Day falls, and its scene takes the player to the Weald)
        Leg.Enter("Wyndon_Stadium") with { Until = GalarStory(40) },
        Leg.OpenTrigger(GalarMap, "the_altar"),
        Leg.Talk("Wyndon_RoseTower", "oleana") with { Until = GalarStory(42) },
        Leg.Enter("Wyndon_EnergyPlant") with { Until = GalarStory(43) },
        Leg.Talk("Wyndon_StadiumPitch", "bede") with { Until = GalarStory(44) },
        Leg.Talk("Wyndon_StadiumPitch", "raihan") with { Until = GalarStory(45) },
        Leg.Talk("Wyndon_StadiumPitch", "leon") with { Until = Flag("GalarHallOfFame") },
    };

    /// <summary>A Galar game played whole by the bot, from the bedroom in Postwick to the Hall of Fame.</summary>
    public static IReadOnlyList<Leg> GalarWhole => Join(GalarToTheTrain, GalarToTheFireBadge, GalarToTheDragonBadge, GalarToTheHallOfFame);
}
