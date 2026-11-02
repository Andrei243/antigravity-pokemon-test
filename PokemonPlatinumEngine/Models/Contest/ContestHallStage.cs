using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.Contest;

/// <summary>Who stands where on a contest's stage.</summary>
public enum StagePlace { Host, Judge, Contestant, Audience, Prize, Artist }

/// <summary>One of the stage's places, on Emerald's grid of tiles (x from the left, y from the top).</summary>
public readonly record struct StageSpot(StagePlace Place, int X, int Y, int Number = 0);

/// <summary>
/// The rooms a Hoenn contest is held in (plan 21 · H12, part o; H11's stage rooms): Emerald's six <c>ContestHall*</c> maps,
/// one to each quality and a plain one, fifteen tiles by eleven, which no warp reaches and where the player is never
/// walked (the contest's screen stands over them). They are a backdrop here and nothing else: this is where Emerald's
/// map files stand the host, the judge, the four contestants, the audience, the prize's ball and the painter, as
/// numbers read from <c>data/maps/ContestHall/map.json</c> (the other five share its events), which the contest's
/// screen lays its backdrop out by (<c>ModernUi.ContestHallBackdrop</c>).
/// </summary>
public static class ContestHallStage
{
    public const int Width = 15, Height = 11;

    /// <summary>The map each quality's contest is held in (Emerald's <c>MAP_CONTEST_HALL_*</c>), the plain hall for none.</summary>
    public static string MapOf(ContestStat? quality) => quality switch
    {
        ContestStat.Cool => "ContestHallCool",
        ContestStat.Beauty => "ContestHallBeauty",
        ContestStat.Cute => "ContestHallCute",
        ContestStat.Smart => "ContestHallSmart",
        ContestStat.Tough => "ContestHallTough",
        _ => "ContestHall"
    };

    /// <summary>The places of <c>ContestHall</c>'s object events, which every hall shares.</summary>
    public static IReadOnlyList<StageSpot> Spots { get; } = new StageSpot[]
    {
        new(StagePlace.Host, 6, 4),
        new(StagePlace.Judge, 7, 4),
        new(StagePlace.Contestant, 4, 7, 1),
        new(StagePlace.Contestant, 6, 7, 2),
        new(StagePlace.Contestant, 8, 7, 3),
        new(StagePlace.Contestant, 10, 7, 4),
        new(StagePlace.Audience, 2, 6, 1),
        new(StagePlace.Audience, 2, 5, 2),
        new(StagePlace.Audience, 4, 2, 3),
        new(StagePlace.Audience, 5, 2, 4),
        new(StagePlace.Audience, 9, 2, 5),
        new(StagePlace.Audience, 10, 2, 6),
        new(StagePlace.Audience, 12, 5, 7),
        new(StagePlace.Prize, 7, 5),
        new(StagePlace.Artist, 12, 6)
    };
}
