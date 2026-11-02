using System;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Join Avenue's growth (plan 22 · U11; <c>docs/mechanics/rulings.md</c>, "Join Avenue's growth"), GPU-free. Black 2's avenue
/// fills as passers-by met over the console's wireless come to it and the player, its manager, ranks it up; with no link,
/// ours grows by days: once the player has taken the owner's job (<see cref="ManagerFlag"/>), the first coming to the
/// avenue on a day brings <see cref="VisitorsOn"/> visitors, more the higher its rank, and every <see cref="VisitorsARank"/>
/// visitors raise its rank by one, up to <see cref="TopRank"/>. The shops widen their stock at their ranks
/// (<see cref="MarketAt"/>, <see cref="FlowersAt"/>, <see cref="CafeAt"/>) and two more shops open (<see cref="StonesRank"/>,
/// <see cref="VitaminsRank"/>). Every number is ours. Nothing is saved but the story's own variables and flags
/// (<see cref="Vars"/>); a day is the game's own (<c>FLAG_DAILY_…</c>, cleared by <see cref="DailyEvents"/>), so no clock is
/// read here.
/// </summary>
public static class JoinAvenue
{
    /// <summary>The flag the owner's job sets: until then the avenue has no visitors.</summary>
    public const string ManagerFlag = "FLAG_UNOVA_JOIN_AVENUE_MANAGER";

    /// <summary>The day's flag: the day's visitors have come (cleared by a new day).</summary>
    public const string VisitedTodayFlag = "FLAG_DAILY_UNOVA_JOIN_AVENUE_VISITORS";

    /// <summary>The story's variables the avenue keeps.</summary>
    public static class Vars
    {
        /// <summary>Every visitor the avenue has had.</summary>
        public const string Visitors = "VAR_UNOVA_JOIN_AVENUE_VISITORS";
    }

    /// <summary>The visitors a rank takes, and the highest rank.</summary>
    public const int VisitorsARank = 10, TopRank = 10;

    /// <summary>The ranks the shops widen and open at.</summary>
    public const int MarketSecondRank = 3, MarketThirdRank = 6, FlowersSecondRank = 4, CafeSecondRank = 5, StonesRank = 7, VitaminsRank = 9;

    /// <summary>The avenue's rank from its visitors: 1 to <see cref="TopRank"/>.</summary>
    public static int RankOf(int visitors) => Math.Clamp(1 + Math.Max(0, visitors) / VisitorsARank, 1, TopRank);

    /// <summary>The visitors a day brings at a rank: two more than the rank.</summary>
    public static int VisitorsOn(int rank) => 2 + Math.Clamp(rank, 1, TopRank);

    /// <summary>The visitors still wanted for the next rank; 0 at the top.</summary>
    public static int ToNextRank(int visitors)
    {
        int rank = RankOf(visitors);
        return rank >= TopRank ? 0 : rank * VisitorsARank - Math.Max(0, visitors);
    }

    /// <summary>The avenue's rank as the story stands.</summary>
    public static int Rank(StoryState story) => RankOf(story.Var(Vars.Visitors));

    /// <summary>
    /// The avenue come to: on the first coming of a day, once the player manages it, the day's visitors come (the day's
    /// flag set). Returns how many came; 0 before the job is taken or once the day's have come.
    /// </summary>
    public static int Visit(StoryState story)
    {
        if (!story.Has(ManagerFlag) || story.Has(VisitedTodayFlag)) return 0;
        int came = VisitorsOn(Rank(story));
        story.SetVar(Vars.Visitors, story.Var(Vars.Visitors) + came);
        story.Set(VisitedTodayFlag);
        return came;
    }

    /// <summary>The market's stock at a rank: the first, a second from <see cref="MarketSecondRank"/>, a third from <see cref="MarketThirdRank"/>.</summary>
    public static string MarketAt(int rank) =>
        rank >= MarketThirdRank ? "join_avenue_market_3" : rank >= MarketSecondRank ? "join_avenue_market_2" : "join_avenue_market";

    /// <summary>The flower shop's stock at a rank.</summary>
    public static string FlowersAt(int rank) => rank >= FlowersSecondRank ? "join_avenue_flowers_2" : "join_avenue_flowers";

    /// <summary>The café's stock at a rank.</summary>
    public static string CafeAt(int rank) => rank >= CafeSecondRank ? "join_avenue_cafe_2" : "join_avenue_cafe";

    /// <summary>
    /// A shop's stock by its name at a rank (<c>market</c>, <c>flowers</c>, <c>cafe</c>, and the two that open later,
    /// <c>stones</c> and <c>vitamins</c>), or null where the shop isn't open yet.
    /// </summary>
    public static string? StockOf(string shop, int rank) => shop switch
    {
        "market" => MarketAt(rank),
        "flowers" => FlowersAt(rank),
        "cafe" => CafeAt(rank),
        "stones" => rank >= StonesRank ? "join_avenue_stones" : null,
        "vitamins" => rank >= VitaminsRank ? "join_avenue_vitamins" : null,
        _ => null
    };
}
