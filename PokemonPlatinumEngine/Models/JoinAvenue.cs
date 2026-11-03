using System;
using PokemonPlatinumEngine.Data;
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
        "antiques" => rank >= AntiquesSecondRank ? "join_avenue_antiques_2" : rank >= AntiquesRank ? "join_avenue_antiques" : null,
        _ => null
    };

    // ------------------------------------------------------------------ the other shops (plan 22 · U11's leftovers)

    /// <summary>
    /// The ranks the avenue's other shops open at (rulings, "Join Avenue's other shops"): the raffle from the first, the dojo
    /// from 2, the salon from 3, the nursery from 4, the antique shop from 5 (more from 8). Each does more the higher the
    /// avenue's rank, which is what stands for Black 2's shop levels of their own.
    /// </summary>
    public const int RaffleRank = 1, DojoRank = 2, SalonRank = 3, NurseryRank = 4, AntiquesRank = 5, AntiquesSecondRank = 8;

    /// <summary>The services that take a Pokémon or the team, by name, and the rank each opens at.</summary>
    public static int OpensAt(string shop) => shop switch
    {
        "raffle" => RaffleRank,
        "dojo" => DojoRank,
        "salon" => SalonRank,
        "nursery" => NurseryRank,
        "antiques" => AntiquesRank,
        "stones" => StonesRank,
        "vitamins" => VitaminsRank,
        _ => 1
    };

    /// <summary>Whether a shop is open at the avenue's rank.</summary>
    public static bool IsOpen(string shop, int rank) => rank >= OpensAt(shop);

    /// <summary>What a service costs: the dojo's session, the salon's treatment (the nursery and the raffle are free).</summary>
    public const int DojoPrice = 1000, SalonPrice = 500;

    /// <summary>The dojo's session at a rank: so much effort in the stat trained, four at its opening and two more a rank.</summary>
    public static int DojoEffort(int rank) => 4 + 2 * Math.Max(0, Math.Clamp(rank, 1, TopRank) - DojoRank);

    /// <summary>
    /// A dojo session: the Pokémon gains <see cref="DojoEffort"/> in the stat, cut to the rules' cap of a stat and to 510 in
    /// all, and its stats are worked out again. Returns what it gained (0 when it can gain no more there, or for an Egg).
    /// </summary>
    public static int Train(Pokemon pokemon, StatType stat, int rank, Data.Ruleset? rules = null)
    {
        if (pokemon.IsEgg) return 0;
        int statCap = (rules ?? Data.Ruleset.Current).EvStatCap;
        int now = EffortRules.Get(pokemon, stat);
        int gain = Math.Min(DojoEffort(rank), Math.Min(statCap - now, EffortRules.MaxTotal - EffortRules.Total(pokemon)));
        if (gain <= 0) return 0;
        EffortRules.Set(pokemon, stat, now + gain);
        pokemon.RecalculateStats();
        return gain;
    }

    /// <summary>The salon's treatment at a rank: so much friendship, ten at its opening and three more a rank.</summary>
    public static int SalonFriendship(int rank) => 10 + 3 * Math.Max(0, Math.Clamp(rank, 1, TopRank) - SalonRank);

    /// <summary>A salon treatment: the Pokémon grows friendlier, never past 255. Returns how much (0 for an Egg or one at the top).</summary>
    public static int Groom(Pokemon pokemon, int rank)
    {
        if (pokemon.IsEgg) return 0;
        int before = pokemon.Friendship;
        pokemon.Friendship = Math.Min(255, before + SalonFriendship(rank));
        return pokemon.Friendship - before;
    }

    /// <summary>The nursery's warmth at a rank: the cycles every Egg of the team is brought on by, two at its opening and one more a rank.</summary>
    public static int NurseryCycles(int rank) => 2 + Math.Max(0, Math.Clamp(rank, 1, TopRank) - NurseryRank);

    /// <summary>The flag the nursery keeps for the day: an Egg is warmed once a day.</summary>
    public const string NurseryTodayFlag = "FLAG_DAILY_UNOVA_JOIN_AVENUE_NURSERY";

    /// <summary>
    /// The nursery warms the team's Eggs once a day: each is brought on by <see cref="NurseryCycles"/>, never below one cycle
    /// (it still hatches by walking). Returns how many Eggs were warmed (0 with none, or once the day's warmth is given).
    /// </summary>
    public static int Warm(StoryState story, Party party, int rank)
    {
        if (story.Has(NurseryTodayFlag)) return 0;
        int warmed = 0;
        foreach (var egg in party.Members)
        {
            if (!egg.IsEgg) continue;
            egg.EggCycles = Math.Max(1, egg.EggCycles - NurseryCycles(rank));
            warmed++;
        }
        if (warmed > 0) story.Set(NurseryTodayFlag);
        return warmed;
    }

    /// <summary>The flag the raffle keeps for the day: one ticket a day.</summary>
    public const string RaffleTodayFlag = "FLAG_DAILY_UNOVA_JOIN_AVENUE_RAFFLE";

    /// <summary>
    /// The raffle's prizes, the lowest first: a ticket draws a number of 0 to 99, and the prize is the row it reaches, the
    /// rank moving the draw up by three a rank (our numbers).
    /// </summary>
    public static readonly (int From, string Item)[] RafflePrizes =
    {
        (0, "Potion"),
        (35, "Great Ball"),
        (60, "Super Potion"),
        (75, "Ultra Ball"),
        (85, "Revive"),
        (93, "PP Up"),
        (99, "Rare Candy")
    };

    /// <summary>The raffle's prize for a draw of 0 to 99 at a rank.</summary>
    public static string RafflePrize(int draw, int rank)
    {
        int at = Math.Clamp(draw, 0, 99) + 3 * (Math.Clamp(rank, 1, TopRank) - 1);
        string prize = RafflePrizes[0].Item;
        foreach (var (from, item) in RafflePrizes)
            if (at >= from) prize = item;
        return prize;
    }

    // ------------------------------------------------------------------ the visitors (plan 22 · U11's leftovers)

    /// <summary>
    /// The passers-by who come to the avenue, ours, standing in for those met over the console's wireless (plan 06's decision
    /// 7 holds no link): each a name and a greeting of <c>ui.json</c>'s <c>joinAvenue.visitor.N</c>. Three stand in the street
    /// once the avenue is managed, picked by the avenue's visitors so far, so they change with each day's visitors and never by
    /// chance.
    /// </summary>
    public const int VisitorList = 16, VisitorsStanding = 3;

    /// <summary>Which of the <see cref="VisitorList"/> (0 on) stands at a place (1 to <see cref="VisitorsStanding"/>) with that many visitors so far: three different ones.</summary>
    public static int VisitorAt(int place, int visitors) =>
        (Math.Max(0, visitors) + Math.Clamp(place, 1, VisitorsStanding) - 1) % VisitorList;
}
