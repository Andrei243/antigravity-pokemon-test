using System.Collections.Generic;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The ways through the story the playthrough bot knows (plan 16 · T13), each a list of <see cref="Leg"/>s in the
/// order a player meets them, written from the chapters' own scripts (the triggers by their scripts' names, the
/// people by their keys). Only what the story asks for is named; the way between is the bot's to find.
/// </summary>
public static class Itineraries
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

    /// <summary>From a new game's bedroom to the Coal Badge.</summary>
    public static IReadOnlyList<Leg> TwinleafToCoalBadge { get; } = Join(FirstChapter, ToTheCoalBadge);

    private static List<Leg> Join(params IReadOnlyList<Leg>[] parts)
    {
        var all = new List<Leg>();
        foreach (var part in parts) all.AddRange(part);
        return all;
    }
}
